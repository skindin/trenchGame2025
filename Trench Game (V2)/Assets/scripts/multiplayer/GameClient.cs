using Google.Protobuf;
using System;
//using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.FullSerializer;
//using UnityEditor.U2D.Animation;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
using WebSocketSharp;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;
//using UnityEngine.Rendering.PostProcessing;
//using Google.Protobuf.Collections;
//using UnityEditor.SearchService;

public class GameClient : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR

    static GameClient instance;
    public static GameClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameClient>();
                //if (manager == null)
                //{
                //    GameObject go = new GameObject("Bullet");
                //    manager = go.AddComponent<BulletManager>();
                //    DontDestroyOnLoad(go);
                //}
            }
            return instance;
        }
    }

    private WebSocket ws;
    public string serverAdress = "localhost";
    //public string ID;
    public UnityEvent onConnect, onDisconnect;

    readonly Queue<Action> actionQueue = new Queue<Action>();

    public bool logBitRate = false, logMessagesPerFrame = false;
    public int averageBitRateFrames = 20, averageBitRate = 0;//, maxMessagesPerFrame = 5;

    int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    CharacterData newPlayer;
    public Dictionary<int,CharacterData> newRemoteChars = new(), updateChars = new(), removeChars = new();

    //private void Start()
    //{
    //    Connect();
    //    //ID = 
    //}

    private void LateUpdate()
    {
        //


        //private void Lol()
        //{
        //bool sentSomeData = actionQueue.Count > 0;

        if (ws == null)// || !ws.IsAlive) //bro don't use isalive. it causes huge drop in framerate
            return;

        //SendData(new byte[] { 1 }); //this actually stopped the jittering like what the hell

        //ws.


        lock (actionQueue)
        {
            if (actionQueue.Count > 0)
            {
                if (logMessagesPerFrame)
                    Debug.Log($"recieved {actionQueue.Count} messages this frame at {Mathf.Round(1/Time.deltaTime)} FPS");
            }

            while (actionQueue.Count > 0)
            {
                Action action = actionQueue.Dequeue();
                action?.Invoke();
            }
        }

        //add remote chars for new player
        if (!CharacterManager.Manager.localPlayerCharacter && newPlayer != null)
            {
                var pos = DataManager.ConvertDataToVector(newPlayer.Pos);

                var id = SpawnManager.Manager.SpawnLocalPlayer(pos, newPlayer.CharacterID).id;

                Debug.Log($"spawned local player, character {id}");
            }

        foreach (var pair in newRemoteChars)
        {
            var remoteChar = pair.Value;

            var id = remoteChar.CharacterID;
            var pos = DataManager.ConvertDataToVector(remoteChar.Pos);
            var name = remoteChar.Name;

            SpawnManager.Manager.SpawnRemoteCharacter(pos, id).characterName = name;

            Debug.Log($"spawned remote character {id} named {name} at {pos}");
        }

        //for (var i = 0; i < updateChars.Count; i++)
        //{
        //    var pair = updateChars.

        //}

        var updateTrash = new Queue<int>();

        foreach (var pair in updateChars)
        {
            var updateChar = pair.Value;

            var character = CharacterManager.Manager.active.Find(character => character.id == updateChar.CharacterID);

            if (character)
            {
                if (updateChar.Pos != null)
                {
                    var targetPos = DataManager.ConvertDataToVector(updateChar.Pos);
                    var nextPos = Vector2.MoveTowards(character.transform.position,targetPos,character.MoveSpeed);
                    //interpolating is good but it didn't fix the current problem

                    character.SetPos(nextPos, false);

                    if (nextPos == targetPos)
                    {
                        updateTrash.Enqueue(pair.Key);
                    }
                }

                if (updateChar.HasName)
                {
                    character.characterName = updateChar.Name;
                }
            }
            else
            {
                Debug.Log($"couldn't update character, no character with id {updateChar.CharacterID}");
            }
        }

        while (updateTrash.Count > 0)
        {
            var id = updateTrash.Dequeue();
            updateChars.Remove(id);
        }

        foreach (var pair in removeChars)
        {
            var removeChar = pair.Value;

            var character = CharacterManager.Manager.active.Find(character => character.id == removeChar.CharacterID);

            if (character)
            {
                SpawnManager.Manager.RemoveCharacter(character);
            }
            else
            {
                Debug.Log($"couldn't remove character, no character with id {removeChar.CharacterID}");
            }
        }

        //remove chars
        //add new remoteChars
        //update chars

        newPlayer = null;
        newRemoteChars.Clear();
        //updateChars.Clear();
        removeChars.Clear();
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

    public string Url { get { return $"ws://{serverAdress}:8080/ClientBehavior"; } }

    public void Connect()
    {
        Disconnect();

#if UNITY_WEBGL && !UNITY_EDITOR//||true
        WebGLConnect();
#else
        WSSharpConnect();
#endif
    }

    void WSSharpConnect ()
    {
        // Initialize WebSocket
        ws = new WebSocket(Url);

        // Set up message received handler
        ws.OnMessage += (sender, e) =>
        {
            OnMessage(e.RawData);
        };

        ws.OnOpen += (sender, e) =>
        {
            OnOpen();
        };

        ws.OnClose += (sender, e) =>
        {
            OnClose();
        };

        //ws.OnOpen += (sender, e) => {
        //    var idData = new ConnectionId() { ID = Guid.NewGuid().ToString() };
        //    var binary = DataManager.MessageToBinary(idData);
        //    SendData(binary);
        //    };

        //ws.buff(1024 * 64, 1024 * 64); // Set buffer size to 64KB

        ws.EmitOnPing = true;
        //ws.Ping = TimeSpan.FromSeconds(10); // Send a ping every 10 seconds



        // Connect to WebSocket server
        ws.ConnectAsync();
    }

    void WebGLConnect ()
    {
        WebGLSocket.onOpen += OnOpen;

        WebGLSocket.onMessage += OnMessage;

        WebGLSocket.onError += OnError;

        WebGLSocket.onClose += OnClose;

        //Console.WriteLine($"told javascript to connect to {Url}");

        WebGLSocket.Connect(Url);
    }

    void OnMessage(byte[] data)
    {
        actionQueue.Enqueue(() => OnData(data));

        void OnData(byte[] rawData)
        {
            //messagesThisFrame++;
            //bool uhoh = false;

            if (DataManager.IfGet<BaseMessage>(rawData, out var message))
            {

                switch (message.TypeCase)
                {
                    case BaseMessage.TypeOneofCase.NewPlayerGrant:
                        {
                            newPlayer = message.NewPlayerGrant.NewPlayer;

                            foreach (var charData in message.NewPlayerGrant.CurrentChars.List)
                            {
                                newRemoteChars.Add(charData.CharacterID, charData);
                            }

                            break;
                        }

                    case BaseMessage.TypeOneofCase.GameState:
                        {
                            if (message.GameState.NewRemoteChars != null)
                            {
                                foreach (var removeChar in message.GameState.NewRemoteChars.List)
                                {
                                    newRemoteChars.Add(removeChar.CharacterID, removeChar);
                                }
                            }

                            if (message.GameState.UpdateChars != null)
                            {
                                foreach (var updateChar in message.GameState.UpdateChars.List)
                                {
                                    if (updateChars.TryGetValue(updateChar.CharacterID, out var prevChar))
                                    {
                                        DataManager.CombineCharData(prevChar, updateChar);
                                    }
                                    else
                                    {
                                        updateChars.Add(updateChar.CharacterID, updateChar);
                                    }
                                }
                            }

                            if (message.GameState.RemoveChars != null)
                            {
                                foreach (var removeChar in message.GameState.RemoveChars.List)
                                {
                                    removeChars.Add(removeChar.CharacterID, removeChar);
                                }
                            }
                            break;
                        }
                }

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

    void OnOpen()
    {
        var baseMessage = new BaseMessage() { NewPlayerRequest = CharacterManager.Manager.playerName };

        //var binary = DataManager.MessageToBinary(baseMessage);

        SendData(baseMessage.ToByteArray());

        actionQueue.Enqueue(() => {
            Debug.Log("Connected to server");
            onConnect.Invoke();
        });
        //Debug.Log("connected to server");
    }

    void OnClose ()
    {
        actionQueue.Enqueue(
            () =>
            {
                Debug.Log("Disconnected from server: ");
                //UIUtils.ResetScene();
                ws = null;
                Disconnect();
            }
        );
    }

    void OnError (string reason)
    {
        Debug.Log("Connection error: " + reason);
    }

    public void Disconnect ()
    {
        if (ws != null)
        {
            ws.CloseAsync();
            ws = null;
        }

        CharacterManager.Manager.RemoveAllCharacters();
        //CharacterManager.Manager.RemoveAllCharacters();
        onDisconnect.Invoke();

        newPlayer = null;
        newRemoteChars.Clear();
        updateChars.Clear();
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

    public void SendData (byte[] data)
    {
        //ws.SendAsync(data, null);

        WebGLSocket.Send(data);

        bytesThisFrame += data.Length;

        if (logBitRate)
            Console.WriteLine($"sent {data.Length} bytes to server");
    }
#endif
    }
