using Google.Protobuf;
using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

//using UnityEngine.Android;
using WebSocketSharp;
using WebSocketSharp.Server;
using static UnityEngine.EventSystems.EventTrigger;

public class GameServer : MonoBehaviour
{
    private WebSocketServer wssv;
    public Dictionary<string, ClientBehavior> clients = new();
    public Dictionary<string, Character> playerCharacters = new();
    public Queue<string> disconnected = new();

    public Queue<Action> actionQueue = new();

    public bool logBitRate = false;
    public int averageBitRateFrames = 20, averageBitRate = 0, targetFramerate = 200;

    public int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    long startTick = 0;

    private void Awake()
    {
        if (!NetworkManager.IsServer)
            return;

        startTick = DateTime.UtcNow.Ticks;

        //|| true
        ClientBehavior.server = this;

        try
        {
            // Initialize the WebSocket server
            wssv = new WebSocketServer("ws://0.0.0.0:8080");
            // or for dual-stack (both IPv4 and IPv6)
            // wssv = new WebSocketServer("ws://[::]:8080");

            wssv.AddWebSocketService<ClientBehavior>("/ClientBehavior");

            wssv.Start();

            if (wssv.IsListening)
            {
                Debug.Log("WebSocket Server listening on port " + wssv.Port + ", and providing WebSocket services:");
                foreach (var path in wssv.WebSocketServices.Paths)
                {
                    Debug.Log("- " + path);
                }
            }
        }
        catch (TypeInitializationException ex)
        {
            Debug.Log("Type Initialization Exception: " + ex.Message);
            if (ex.InnerException != null)
            {
                Debug.Log("Inner Exception: " + ex.InnerException.Message);
                Debug.Log("Stack Trace: " + ex.InnerException.StackTrace);
            }
            else
            {
                Debug.Log("No Inner Exception.");
            }
        }
        catch (Exception ex)
        {
            Debug.Log("General Exception: " + ex.Message);
            Debug.Log("Stack Trace: " + ex.StackTrace);
        }

        Application.targetFrameRate = targetFramerate;
        Debug.Log($"Target framerate set to {targetFramerate}");
    }

    public CharDataList newPlayerData = new(), updateCharData = new(), currentCharData = new();
    public RepeatedField<int> removeCharList = new(), removeItemList = new();

    public RepeatedField<ItemData> newItems = new(), updateItems = new(), currentItems = new();

    public RepeatedField<BulletBunch> newBullets = new();

    private void LateUpdate()
    {
        if (!NetworkManager.IsServer)
            return;

        NetworkManager.Manager.NetTime = LogicAndMath.TicksToSeconds(DateTime.UtcNow.Ticks - startTick);

        //bool sentSomeData = actionQueue.Count > 0;

        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }

        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.newPlayer != null)
            {
                newPlayerData.List.Add(client.newPlayer);

                var pos = ChunkManager.Manager.GetRandomPos();
                var id = SpawnManager.Manager.NewCharId;

                var newCharacter = SpawnManager.Manager.SpawnRemoteCharacter(pos, id);

                Debug.Log($"spawned remote character {id} named {client.newPlayer.Name}");

                playerCharacters.Add(client.ID, newCharacter);
                client.character = newCharacter;
                currentCharData.List.Add(client.newPlayer);

                var posData = DataManager.VectorToData(pos);

                client.newPlayer.Pos = posData;
                client.newPlayer.CharacterID = id;
            }
            else if (client.update != null)
            {
                updateCharData.List.Add(client.update);

                var character = playerCharacters[client.ID];

                var currentData = currentCharData.List.FirstOrDefault(charData => charData.CharacterID == client.update.CharacterID);

                if (currentData != null)
                    DataManager.CombineCharData(currentData, client.update); //i hope this part works properly

                if (client.update.HasName)
                {
                    character.characterName = client.update.Name;
                }

                if (client.update.Pos != null)
                {
                    var pos = DataManager.ConvertDataToVector(client.update.Pos);
                    character.SetPos(pos, false);
                }

                foreach (var droppedItem in client.droppedItems)
                {
                    updateItems.Add(droppedItem);
                    var item = ItemManager.Manager.active[droppedItem.ItemId];
                    var pos = DataManager.ConvertDataToVector(droppedItem.Pos);
                    client.character.inventory.DropItem(item, pos);

                    foreach (var currentItem in currentItems)
                    {
                        if (currentItem.ItemId == droppedItem.ItemId)
                        {
                            currentItem.Pos = droppedItem.Pos;
                        }
                    }
                }

                if (client.droppedItems.Count > 0)
                    currentData.ClearItemId();

                if (client.update.HasItemId)
                {
                    var item = ItemManager.Manager.active[client.update.ItemId];
                    character.inventory.PickupItem(item, item.transform.position);

                    foreach (var itemData in currentItems)
                    {
                        if (itemData.ItemId == item.id)
                        {
                            itemData.Pos = null;
                        }
                    }

                    currentData.ItemId = client.update.ItemId; //i could make combinedata do this but idc rn
                    //hopefully adding the previous item to the dropped list makes the drop pos work when switching items...
                }

                if (character.inventory.ActiveWeapon != null
                    && character.inventory.ActiveWeapon is Gun gun
                    && client.bullets != null && client.bullets.Bullets.Count > 0)
                {
                    var gunModel = gun.GunModel;

                    foreach (var bullet in client.bullets.Bullets)
                    {
                        if (gun.rounds > 0)
                        NetworkManager.Manager.DataToBullet(bullet, character, client.bullets.StartTime);
                        gun.rounds--;
                    }

                    if (client.bullets.Bullets.Count > 0)
                    {
                        var gunData = new GunData { Amo = gun.rounds };

                        var itemData = new ItemData { ItemId = gun.id, Gun = gunData };

                        UpdateItemData(itemData);
                    }

                    //Debug.Log($"spawned {client.bullets.Bullets.Count} bullet(s)");

                    newBullets.Add(client.bullets);
                }

                //client.droppedItems.Clear();

                //if (!character.inventory.ActiveItem)
                //{
                //    currentData.ClearItemId();
                //}
                //else
                //{
                //    currentData.ItemId = character.inventory.ActiveItem.id;

                //}

                //Debug.Log($"updated character {character.id}");
                LogLists();
            }
            else if (client.remove != null)
            {

                var character = playerCharacters[client.ID];

                removeCharList.Add(character.id);


                SpawnManager.Manager.RemoveCharacter(character);
                foreach (var currentChar in currentCharData.List)
                {
                    //var currentChar = removeList.List[i];

                    if (currentChar.CharacterID == client.remove.CharacterID)
                    {
                        currentCharData.List.Remove(currentChar);

                        //character.inventory.DropActiveItem();

                        ItemData activeItem = null;

                        foreach (var itemData in currentItems)
                        {
                            if (itemData.ItemId == currentChar.ItemId)
                            {
                                activeItem = itemData;
                                break;
                            }
                        }

                        if (activeItem == null)
                        {
                            activeItem = new ItemData { ItemId = currentChar.ItemId };
                        }

                        activeItem.Pos = currentChar.Pos;

                        //currentChar.ClearItemId();
                        break;
                    }
                }

                Debug.Log($"removed character {character.id}, {currentCharData.List.Count} characters left");
            }
        }

        //if (updateCharData.List.Count > 1)
        //    Debug.Log($"{updateCharData}");

        foreach (var pair in clients)
        {
            var client = pair.Value;

            if (client.State != WebSocketState.Open)
                continue;

            //if (client.newPlayer == null && client.update == null && client.remove == null) //bruh im such an idiot
            //    continue; //doesn't need to reset, because they're all already null

            if (client.newPlayer != null) //if this client just joined, inform them of current characters, then continue to other clients
            {
                var currentChars = (currentCharData.List.Count > 0) ? currentCharData : null;

                var currentChar = currentChars.List.FirstOrDefault(charData => charData.CharacterID == client.newPlayer.CharacterID);

                if (true || client.character != null) //sloppy af ik, not sure why i was testing if the client already had a character, it should by this point
                {
                    currentChars.List.Remove(currentChar);
                }

                //client.newPlayer.Name = null;

                //var startTime = LogicAndMath.SecondsToTicks(Time.time);

                var grant = new NewPlayerGrant { NewPlayer = client.newPlayer, CurrentChars = currentChars };

                foreach (var item in currentItems) //might be better to make this it's own message, this readonly thing getting annoying
                {
                    grant.CurrentItems.Add(item);
                }

                var message = new MessageForClient { NewPlayerGrant = grant };
                client.SendData(message.ToByteArray());

                client.connStartTick = DateTime.UtcNow.Ticks;

                currentChars.List.Add(currentChar);

                Debug.Log($"told client to spawn their player, character {client.newPlayer.CharacterID}");

                LogLists();
                client.update = client.remove = client.newPlayer = null;
                continue;
            }
            //if this is an established player...
            {
                var gameState = new GameState();


                if (client.update != null) //this might cause a problem when the character's hp is modified when they're also moving
                {
                    var removed = updateCharData.List.Remove(client.update);

                    //if (updateCharData.List.Count > 0)
                    //    Debug.Log($"{updateCharData}");

                    //if (removed && updateCharData.List.Count > 2)
                    //    Debug.Log($"removed {client.update}");

                    //var skinned = new CharacterData { CharacterID = client.update.CharacterID, }

                    //if (client.bull)
                }

                if (client.bullets != null)
                {
                    newBullets.Remove(client.bullets);
                }

                foreach (var dropItem in client.droppedItems)
                {
                    bool found = false;
                    for (int i = 0; i < updateItems.Count; i++)
                    {
                        var updateItem = updateItems[i];

                        if (dropItem.ItemId == updateItem.ItemId)
                        {
                            updateItems.RemoveAt(i);
                            i--;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                gameState.UpdateChars = updateCharData.List.Count > 0 ? updateCharData : null;

                gameState.NewRemoteChars = newPlayerData.List.Count > 0 ? newPlayerData : null;

                foreach (var removeChar in removeCharList)
                {
                    gameState.RemoveChars.Add(removeChar);
                }

                foreach (var newItem in newItems)
                {
                    if (!client.character.inventory.ActiveItem || client.character.inventory.ActiveItem.id != newItem.ItemId)
                        gameState.NewItems.Add(newItem);
                }

                foreach (var updateItem in updateItems)
                {
                    gameState.UpdateItems.Add(updateItem);
                }

                foreach (var removeItem in removeItemList)
                {
                    gameState.RemoveItems.Add(removeItem);
                }

                foreach (var bunch in newBullets)
                {
                    gameState.NewBullets.Add(bunch);
                }

                //if (newBullets.Count > 0)
                //{
                //    Debug.Log($"told client {client.character.id} to spawn {newBullets.Count} bullet(s)");
                //}

                if (true ||
                    gameState.UpdateChars != null ||
                    gameState.NewRemoteChars != null ||
                    gameState.RemoveChars != null ||
                    gameState.NewItems.Count > 0
                    )
                {
                    var message = new MessageForClient { GameState = gameState, Time = Time.time };

                    client.SendData(message.ToByteArray());
                    LogLists();
                }
                //else
                //    Debug.Log("apparently all the lists are empty shruggin emoji");

                if (client.update != null) //readd this character back
                {
                    updateCharData.List.Add(client.update);
                }


                foreach (var dropItem in client.droppedItems)
                {
                    updateItems.Add(dropItem);
                }

                if (client.bullets != null)
                    newBullets.Add(client.bullets);

                client.droppedItems.Clear();

                client.bullets = null;

                client.update = client.remove = client.newPlayer = null;
            }
            //LogLists();
        }

        //if (updateCharData.List.Count > 1)
        //    Debug.Log($"{updateCharData}");

        //var message = 

        newPlayerData.List.Clear();
        updateCharData.List.Clear();
        removeCharList.Clear();

        newItems.Clear();
        updateItems.Clear();
        removeItemList.Clear();

        newBullets.Clear(); //bruh facepalm emoji
        //if (logBitRate)
        //{
        //    pastByteRecords.Add(bytesThisFrame);

        //    if (pastByteRecords.Count > averageBitRateFrames)
        //        pastByteRecords.RemoveAt(0);

        //    averageBitRate = Mathf.RoundToInt(LogicAndMath.GetListValueTotal(pastByteRecords.ToArray(), byteCount => byteCount) / pastByteRecords.Count / Time.deltaTime);

        //    if (bytesThisFrame > 0)
        //        Debug.Log($"average bit rate: {averageBitRate}");

        //    bytesThisFrame = 0;
        //}

        while (disconnected.Count > 0)
        {
            clients.Remove(disconnected.Dequeue());
        }

        //LogLists();

        void LogLists()
        {
            //Debug.Log($"{newPlayerList.List.Count} new players, {updateList.List.Count} character updates, {removeList.List.Count} removals, and {currentCharData.List.Count} current characters");
        }
    }

    public void UpdateCharData (CharacterData data)
    {
        DataManager.CombineCharDataList(data, currentCharData);
        //DataManager.CombineCharDataList(data, updateCharData);
        updateCharData.List.Add(data); //this is so fucking sloppy because it's adding more characters bleh
    }

    public void AddCharacter(Character character)
    {
        var pos = DataManager.VectorToData(character.transform.position);
        var data = new CharacterData { CharacterID = character.id, Pos = pos, Name = character.characterName };
        currentCharData.List.Add(data);
    }

    public void UpdateItemData (ItemData data)
    {
        DataManager.CombineItemDataList(data,currentItems);
        DataManager.CombineItemDataList(data,updateItems);
    }

    public void AddItem(Item item)
    {
        var pos = DataManager.VectorToData(item.transform.position);
        var data = new ItemData { ItemId = item.id, PrefabId = item.prefabId, Pos = pos };
        newItems.Add(data);
        currentItems.Add(data);
    }

    public void Broadcast(byte[] message)
    {
        //foreach (var session in wssv.WebSocketServices["/Echo"].Sessions) ;

        var sessions = wssv.WebSocketServices["/ClientBehavior"].Sessions;

        sessions.Broadcast(message);

        //for (var i = 0; i < sessions.Count; i++)
        //{

        //}
    }

    public void SendDataDisclude(byte[] binary, params string[] discludeIDs)
    {
        //Debug.Log($"began send data disclude. clients: {clients} discludedIds: {discludeIDs}");
        foreach (var clientBehavior in clients)
        {
            if (!discludeIDs.Contains(clientBehavior.Key))
            {
                clientBehavior.Value.SendData(binary);
            }
        }
    }

    public void SendDataToClient(byte[] binary, string id)
    {
        if (clients.TryGetValue(id, out var client))
        {
            client.SendData(binary);
        }
    }

    private void OnDestroy()
    {
        if (wssv != null)
        {
            wssv.Stop();
            wssv = null;
        }
    }

    public class ClientBehavior : WebSocketBehavior
    {
        public static GameServer server;

        public CharacterData remove, update, newPlayer;

        public List<ItemData> droppedItems = new(); //GOTTA MAKE THIS LIST THE ITEMS IT'S DROPPING

        public Character character;

        public BulletBunch bullets = new();

        public long connStartTick;

        //public ClientBehavior (GameServer server)
        //{
        //    ClientBehavior.server = server;
        //}

        protected override void OnOpen()
        {
            //Debug.Log("New clientBehavior connected.");
            server.clients.Add(ID, this);
        }

        protected override void OnMessage(MessageEventArgs e)
        {


            // Use Coroutine on the main thread
            //Debug.Log("onmessage started on server");
            server.actionQueue.Enqueue(() => UseData(e.RawData));
            //Debug.Log("started coroutine on server");

            void UseData(byte[] rawData)
            {
                if (e.IsText)
                {
                    Debug.Log("Server received message: " + e.Data);
                    //return;
                }
                else if (DataManager.IfGet<MessageForServer>(rawData, out var message))
                {
                    switch (message.TypeCase)
                    {
                        case MessageForServer.TypeOneofCase.NewPlayerRequest:
                            {
                                if (!character && newPlayer == null)
                                {
                                    //var pos = ChunkManager.Manager.GetRandomPos();

                                    var name = message.NewPlayerRequest;

                                    newPlayer = new CharacterData { Name = name };

                                    Debug.Log($"recieved new player request");
                                }
                            }
                            break;

                        case MessageForServer.TypeOneofCase.Input:
                            {
                                if (server.playerCharacters.TryGetValue(ID, out var character))
                                {
                                    var charData = new CharacterData { CharacterID = this.character.id };

                                    if (message.Input.Pos != null)
                                    {
                                        charData.Pos = message.Input.Pos;
                                    }

                                    if (message.Input.HasName)
                                    {
                                        charData.Name = message.Input.Name;
                                        //Debug.Log($"recieved new name {message.Input.Name}");
                                    }

                                    if (message.Input.HasAngle)
                                    {
                                        charData.Angle = message.Input.Angle;
                                        //Debug.Log($"recieved angle {charData.Angle} for character {character.id}");
                                    }

                                    if (update != null)
                                    {
                                        DataManager.CombineCharData(update, charData);
                                    }
                                    else
                                        update = charData;

                                    switch (message.Input.ItemCase)
                                    {
                                        //case PlayerInput.ItemOneofCase.Action:
                                        //    break;

                                        //case PlayerInput.ItemOneofCase.SecondaryAction: break;

                                        //case PlayerInput.ItemOneofCase.DirectionalAction: break;

                                        case PlayerInput.ItemOneofCase.DropItem:
                                            droppedItems.Add(new ItemData { ItemId = character.inventory.ActiveItem.id, Pos = message.Input.LookPos });
                                            break;

                                        case PlayerInput.ItemOneofCase.PickupItem:
                                            update.ItemId = message.Input.PickupItem;
                                            if (character.inventory.ActiveItem) //if the character is holding an item,
                                            {
                                                droppedItems.Add(new ItemData { ItemId = character.inventory.ActiveItem.id, Pos = message.Input.LookPos });
                                            }
                                            break;
                                    }

                                    //Debug.Log($"server recieved input");
                                }
                                else
                                {
                                    Debug.Log($"server doesn't have a currentChar associated with client {ID}");
                                }

                                if (message.Input.Bullets != null && message.Input.Bullets.Bullets.Count > 0)
                                {
                                    bullets = message.Input.Bullets;
                                    bullets.CharacterId = character.id;
                                }
                            }
                            break;
                    }

                    if (message.HasTick)
                    {
                        var delay = DateTime.UtcNow.Ticks - connStartTick; //delay is how long this message took roundTrip
                        var delayInSeconds = LogicAndMath.TicksToSeconds(delay);

                        var remoteTick = message.Tick - (delay); //remote tick is the tick that the client thought it was when the server sent the message
                        //var ticksSinceStart = LogicAndMath.SecondsToTicks(Time.unscaledTime);

                        var remoteStartTick = remoteTick - connStartTick + server.startTick;
                        var startTickDelta = LogicAndMath.TicksToSeconds(server.startTick - remoteStartTick);

                        var startTickMessage = new MessageForClient { StartTick = remoteStartTick };

                        SendData(startTickMessage.ToByteArray());
                    }
                }
            }
        }


        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log("Client disconnected. Reason: " + e.Reason);

            server.actionQueue.Enqueue(() => RemoveCharacter(ID));

            void RemoveCharacter(string ID)
            {
                if (character)
                {
                    remove = new CharacterData { CharacterID = character.id };

                    newPlayer = update = null;

                    server.disconnected.Enqueue(ID);
                }

                //server.clients.Remove(ID);

                //if (server.playerCharacters.TryGetValue(ID, out var character))
                //{
                //    server.playerCharacters.Remove(ID);

                //    CharacterManager.Manager.RemoveCharacter(character);
                //}

                //var removeMessage = new BaseMessage() { RemoveCharOfID = character.id };

                //server.SendDataDisclude(removeMessage.ToByteArray(), ID);

                //Debug.Log($"Removed character {character.id}");
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.Log("WebSocket error: " + e.Message);
        }

        public void SendData(byte[] binary)
        {
            Send(binary);

            server.bytesThisFrame += binary.Length;

            if (server.logBitRate)
                Debug.Log($"sent {binary.Length} bytes to client");
        }
    }
}
