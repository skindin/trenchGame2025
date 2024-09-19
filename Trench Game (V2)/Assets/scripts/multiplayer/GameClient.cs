using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.FullSerializer;
//using UnityEditor.U2D.Animation;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
using WebSocketSharp;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;
using Google.Protobuf.Collections;
using UnityEngine.TextCore.Text;
using Unity.VisualScripting.Dependencies.NCalc;
//using Google.Protobuf.Collections;
//using UnityEditor.SearchService;

public class GameClient : MonoBehaviour
{
    private WebSocket ws;
    public string serverAdress = "localhost";

    public string ServerAdress { get { return serverAdress; } set { serverAdress = value; } }

    //public string ID;
    public UnityEvent onConnect, onDisconnect;

    readonly Queue<Action> actionQueue = new Queue<Action>();

    public bool logBitRate = false;
    public int averageBitRateFrames = 20, averageBitRate = 0;

    int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    CharacterData newPlayer;
    public CharDataList newRemoteChars = new(), updateChars = new();
    public RepeatedField<int> removeChars = new(), removeItems = new();

    public RepeatedField<ItemData> newItems = new(), updateItems = new();

    public RepeatedField<BulletBunch> newBullets = new();

    float latestMsgStamp = 0;

    long startTimeTick = 0;

    //private void Start()
    //{
    //    Connect();
    //    //ID = 
    //}

    //private void Awake()
    //{
    //    startTimeTick = DateTime.UtcNow
    //}

    private void LateUpdate()
    {
        if (NetworkManager.IsServer)
            return;

        //bool sentSomeData = actionQueue.Count > 0;

        if (ws == null)// || !ws.IsAlive)
            return;

        if (startTimeTick > 0)
            NetworkManager.NetTime = LogicAndMath.TicksToSeconds(DateTime.UtcNow.Ticks - startTimeTick);

        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }

        //add remote chars for new player
        if (!CharacterManager.Manager.localPlayerCharacter && newPlayer != null)
        {
            var pos = DataManager.ConvertDataToVector(newPlayer.Pos);

            var id = SpawnManager.Manager.SpawnLocalPlayer(pos, newPlayer.CharacterID).id;

            Debug.Log($"spawned local player, character {id}");
        }

        foreach (var newItemData in newItems)
        {
            DataManager.CreateItemWithData(newItemData);
        }

        foreach (var updateItem in updateItems)
        {
            DataManager.UpdateItemWithData(updateItem);
        }

        foreach (var removeItem in removeItems)
        {
            //var item = ItemManager.Manager.active[removeItem];

            if (!ItemManager.Manager.active.TryGetValue(removeItem, out var item))
            {
                Debug.Log($"could not remove item, no item with id {removeItem} was found");
                continue;
            }

            //ItemManager.Manager.RemoveItem(item);
            item.DestroyItem();

            Debug.Log($"server said to remove item {removeItem}");
        }

        foreach (var newRemoteChar in newRemoteChars.List)
        {
            var id = newRemoteChar.CharacterID;
            var pos = DataManager.ConvertDataToVector(newRemoteChar.Pos);
            var name = newRemoteChar.Name;

            var newCharacter = SpawnManager.Manager.SpawnRemoteCharacter(pos, id);

            newCharacter.characterName = name;

            if (newRemoteChar.HasItemId)
            {
                var item = ItemManager.Manager.active[newRemoteChar.ItemId];

                newCharacter.inventory.PickupItem(item, item.transform.position);
            }

            if (newRemoteChar.HasAngle && newCharacter.inventory.ActiveItem is IDirectionalAction dirItem) //idk if there's a point in syncing the angle on the server
            {
                Vector2 direction = Quaternion.AngleAxis(newRemoteChar.Angle, Vector3.forward) * Vector2.up;
                dirItem.Aim(direction);
                //Debug.Log($"recieved angle {updateChar.Angle}")
            }

            if (newRemoteChar.HasHp)
            {
                newCharacter.SetHP(newRemoteChar.Hp);
            }

            Debug.Log($"spawned remote character {id} named {name} at {pos}{(newRemoteChar.HasItemId ? $" holding item {newRemoteChar.ItemId}" : "")}");
        }

        //Debug.Log(updateChars.List);

        foreach (var updateChar in updateChars.List)
        {
            var character = CharacterManager.Manager.active.Find(character => character.id == updateChar.CharacterID);

            if (character)
            {
                if (updateChar.Pos != null)
                {
                    var pos = DataManager.ConvertDataToVector(updateChar.Pos);
                    character.SetPos(pos, false);
                }

                if (updateChar.HasName)
                {
                    character.characterName = updateChar.Name;
                }

                if (updateChar.HasItemId)
                {
                    var item = ItemManager.Manager.active[updateChar.ItemId];

                    character.inventory.PickupItem(item, item.transform.position);

                    Debug.Log($"server told character {character.id} to pickup item {item.id}");
                }

                if (updateChar.HasHp)
                {
                    character.SetHP(updateChar.Hp);
                }

                if (updateChar.HasAngle && character.inventory.ActiveItem is IDirectionalAction dirItem) //idk if there's a point in syncing the angle on the server
                {
                    Vector2 direction = Quaternion.AngleAxis(updateChar.Angle, Vector3.forward) * Vector2.up;
                    dirItem.Aim(direction);
                    //Debug.Log($"recieved angle {updateChar.Angle}")
                }

                if (updateChar.Reserve != null)
                {
                    foreach (var ammo in updateChar.Reserve)
                    {
                        character.reserve.ammoPools[ammo.Index].rounds = ammo.Amount;
                    }
                }
            }
            else
            {
                Debug.Log($"couldn't update character, no character with id {updateChar.CharacterID}");
            }
        }

        foreach (var removeChar in removeChars)
        {
            var character = CharacterManager.Manager.active.Find(character => character.id == removeChar);

            if (character)
            {
                SpawnManager.Manager.RemoveCharacter(character);
            }
            else
            {
                Debug.Log($"couldn't remove character, no character with id {removeChar}");
            }
        }



        //remove chars
        //add new remoteChars
        //update chars

        newPlayer = null;
        newRemoteChars.List.Clear();
        updateChars.List.Clear();
        removeChars.Clear();

        newItems.Clear();
        updateItems.Clear();
        removeItems.Clear();

        //SendData(new byte[1]);
    }

    //private void LateUpdate()
    //{
    //if (logBitRate)
    //{
    //    //Debug.Log($"Sent {bytesThisFrame} bytes this frame");

    //    pastByteRecords.Add(bytesThisFrame);

    //    if (pastByteRecords.Count > averageBitRateFrames)
    //        pastByteRecords.RemoveAt(0);

    //    averageBitRate = Mathf.RoundToInt(LogicAndMath.GetListValueTotal(pastByteRecords.ToArray(), byteCount => byteCount) / pastByteRecords.Count / Time.deltaTime);

    //    if (bytesThisFrame > 0)
    //        Debug.Log($"average bit rate: {averageBitRate}");

    //    bytesThisFrame = 0;
    //}
    //}

    public void Connect()
    {
        if (NetworkManager.IsServer)
        {
            Debug.Log("this isn't a client");
            return;
        }

        if (ws != null)
            return;

        Disconnect();

        // Initialize WebSocket
        ws = new WebSocket($"ws://{serverAdress}:8080/ClientBehavior");

        // Set up message received handler
        ws.OnMessage += OnMessage;

        ws.OnOpen += (sender, e) =>
        {
            var baseMessage = new MessageForServer() { NewPlayerRequest = CharacterManager.Manager.playerName };

            //var binary = DataManager.MessageToBinary(baseMessage);

            SendData(baseMessage.ToByteArray());

            actionQueue.Enqueue(() => {
                Debug.Log("Connected to server");
                onConnect.Invoke();
            });
            //Debug.Log("connected to server");
        };

        ws.OnClose += (sender, e) => actionQueue.Enqueue(() => {
            Debug.Log("Disconnected from server");
            //UIUtils.ResetScene();
            ws = null;
            Disconnect();
        });

        //ws.OnOpen += (sender, e) => {
        //    var idData = new ConnectionId() { ID = Guid.NewGuid().ToString() };
        //    var binary = DataManager.MessageToBinary(idData);
        //    SendData(binary);
        //    };

        // Connect to WebSocket server
        ws.ConnectAsync();
    }

    //float lastMsgStamp = 0;

    void OnMessage(object sender, MessageEventArgs e)
    {


        actionQueue.Enqueue(() => OnData(e.RawData));

        //if (lastMsgStamp > 0)
        //{
        //    actionQueue.Enqueue(() => Debug.Log($"{Time.time - lastMsgStamp} seconds since last message"));
        //}
        //lastMsgStamp = Time.time;

        void OnData(byte[] rawData)
        {
            //bool uhoh = false;

            if (DataManager.IfGet<MessageForClient>(rawData, out var message))
            {
                if (message.HasStartTick)
                    startTimeTick = message.StartTick;

                switch (message.TypeCase)
                {
                    case MessageForClient.TypeOneofCase.NewPlayerGrant:
                        {
                            newPlayer = message.NewPlayerGrant.NewPlayer;

                            foreach (var charData in message.NewPlayerGrant.CurrentChars.List)
                            {
                                newRemoteChars.List.Add(charData);
                            }

                            foreach (var itemData in message.NewPlayerGrant.CurrentItems)
                            {
                                newItems.Add(itemData);
                            }

                            if (!message.HasStartTick)
                            {
                                var tickMsg = new MessageForServer { Tick = DateTime.UtcNow.Ticks };
                                SendData(tickMsg.ToByteArray());
                            }

                            break;
                        }

                    case MessageForClient.TypeOneofCase.GameState:
                        {
                            if (message.GameState.NewRemoteChars != null)
                            {
                                foreach (var newRemoteChar in message.GameState.NewRemoteChars.List)
                                {
                                    newRemoteChars.List.Add(newRemoteChar);
                                }
                            }

                            if (message.GameState.UpdateChars != null)
                            {
                                foreach (var updateChar in message.GameState.UpdateChars.List)
                                {
                                    if (updateChar.CharacterID == CharacterManager.Manager.localPlayerCharacter.id)
                                    {
                                        Debug.Log($"recieved data for local player, {updateChar}");
                                        //continue;
                                    }

                                    //var pos = DataManager.ConvertDataToVector(updateChar.Pos);

                                    //GeoUtils.MarkPoint(pos, 1, Color.red);

                                    DataManager.CombineCharDataList(updateChar, updateChars);
                                }
                            }

                            if (message.GameState.RemoveChars != null)
                            {
                                foreach (var removeChar in message.GameState.RemoveChars)
                                {
                                    removeChars.Add(removeChar);
                                }
                            }

                            foreach (var itemData in message.GameState.NewItems)
                            {
                                newItems.Add(itemData);

                                Debug.Log($"recieved new item {itemData.ItemId}");
                            }

                            foreach (var itemData in message.GameState.UpdateItems)
                            {
                                updateItems.Add(itemData);

                                Debug.Log($"recieved update for item {itemData.ItemId}");
                            }

                            foreach (var removeItem in message.GameState.RemoveItems)
                            {
                                removeItems.Add(removeItem);
                            }

                            foreach (var bunch in message.GameState.NewBullets)
                            {
                                var character = CharacterManager.Manager.active.Find(character => character.id == bunch.CharacterId);

                                if (!character) continue;

                                foreach (var bullet in bunch.Bullets)
                                {
                                    NetworkManager.Manager.DataToBullet(bullet, character,bunch.StartTime);
                                }

                                //Debug.Log(
                                //    $"was told to spawn {bunch.Bullets.Count} bullet(s) by server from character {bunch.CharacterId}"
                                //    );
                            }

                            if (message.GameState.UpdatePlayer != null)
                            {
                                updateChars.List.Add(message.GameState.UpdatePlayer);
                            }

                            break;
                        }
                }

                latestMsgStamp = Mathf.Max(latestMsgStamp, message.Time);
            }

            //try
            //{
            //    // Assuming BinaryToVector is a method to convert raw data to a Vector2 or similar
            //    var pos = DataManager.BinaryToVector(rawData);

            //    Debug.Log("Client received pos: " + pos);

            //    // Ensure this call is made on the main thread
            //    //yield return new WaitForEndOfFrame();
            //    CharacterManager.Manager.mainPlayerCharacter.SetPos(pos, false);
            //}
            //catch (Exception ex)
            //{
            //    Debug.LogError("Error in OnMessage client: " + ex.Message);
            //}
        }
    }

    public void Disconnect()
    {
        if (ws != null)
        {
            ws.CloseAsync();
            ws = null;
        }

        CharacterManager.Manager.RemoveAllCharacters();
        ItemManager.Manager.RemoveAll();
        //CharacterManager.Manager.RemoveAllCharacters();
        onDisconnect.Invoke();

        newPlayer = null;
        newRemoteChars.List.Clear();
        updateChars.List.Clear();
        removeChars.Clear();

        actionQueue.Clear();

        Debug.Log("connection ended by client");
    }

    //private void Update()
    //{
    //    // Send a message when the space bar is pressed
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        ws.Send("Space bar pressed!");
    //        Debug.Log("Message sent to server");
    //    }
    //}

    private void OnDestroy()
    {
        // Clean up WebSocket connection
        Disconnect();
    }

    public void SendData(byte[] data)
    {
        ws?.Send(data);
        bytesThisFrame += data.Length;

        if (logBitRate)
            Debug.Log($"sent {data.Length} bytes to server");
    }
}
