using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.FullSerializer;
//using UnityEditor.U2D.Animation;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
//using WebSocketSharp;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;
using NativeWebSocket;
//using System.Net.WebSockets;
//using WebSocketSharp;
//using Google.Protobuf.Collections;
//using UnityEditor.SearchService;

public class GameClient : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR

    private WebSocket ws;
    public string serverAdress = "localhost";
    //public string ID;
    public UnityEvent onConnect, onDisconnect;

    readonly Queue<Action> actionQueue = new Queue<Action>();

    public bool logBitRate = false;
    public int averageBitRateFrames = 20, averageBitRate = 0;

    int bytesThisFrame { get; set; }
    List<int> pastByteRecords = new();

    CharacterData newPlayer;
    public CharDataList newRemoteChars = new(), updateChars = new(), removeChars = new();

    //private void Start()
    //{
    //    Connect();
    //    //ID = 
    //}

    private void Update()
    {
        //bool sentSomeData = actionQueue.Count > 0;

        if (ws == null)
            return;

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

        foreach (var remoteChar in newRemoteChars.List)
        {
            var id = remoteChar.CharacterID;
            var pos = DataManager.ConvertDataToVector(remoteChar.Pos);
            var name = remoteChar.Name;

            SpawnManager.Manager.SpawnRemoteCharacter(pos, id).characterName = name;

            Debug.Log($"spawned remote character {id} named {name} at {pos}");
        }

        foreach (var updateChar in  updateChars.List)
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
            }
            else
            {
                Debug.Log($"couldn't update character, no character with id {updateChar.CharacterID}");
            }
        }

        foreach (var removeChar in removeChars.List)
        {
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
        newRemoteChars.List.Clear();
        updateChars.List.Clear();
        removeChars.List.Clear();
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
        Disconnect();

        // Initialize WebSocket
        ws = new WebSocket($"ws://{serverAdress}:8080/ClientBehavior");

        // Set up message received handler
        ws.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            //Debug.Log(bytes);

            OnMessage(bytes);

            // getting the message as a string
            // var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("OnMessage! " + message);
        };

        ws.OnOpen += () =>
        {
            var baseMessage = new BaseMessage() { NewPlayerRequest = CharacterManager.Manager.playerName };

            //var binary = DataManager.MessageToBinary(baseMessage);

            SendData(baseMessage.ToByteArray());

            actionQueue.Enqueue(() => {
                Debug.Log("Connected to server");
                onConnect.Invoke();
                });
            //Debug.Log("connected to server");
        };

        ws.OnClose += (e) => actionQueue.Enqueue(() => {
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
        ws.Connect();
    }

    void OnMessage(byte[] data)
    {
        actionQueue.Enqueue(() => OnData(data)); //THERE'S ERRORS HERE BUT I GOTTA SLEEP

        void OnData(byte[] rawData)
        {
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
                                newRemoteChars.List.Add(charData);
                            }

                            break;
                        }

                    case BaseMessage.TypeOneofCase.GameState:
                        {
                            if (message.GameState.NewRemoteChars != null)
                            {
                                foreach (var removeChar in message.GameState.NewRemoteChars.List)
                                {
                                    newRemoteChars.List.Add(removeChar);
                                }
                            }

                            if (message.GameState.UpdateChars != null)
                            {
                                foreach (var removeChar in message.GameState.UpdateChars.List)
                                {
                                    updateChars.List.Add(removeChar);
                                }
                            }

                            if (message.GameState.RemoveChars != null)
                            {
                                foreach (var removeChar in message.GameState.RemoveChars.List)
                                {
                                    removeChars.List.Add(removeChar);
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

    public void Disconnect ()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }

        CharacterManager.Manager.RemoveAllCharacters();
        //CharacterManager.Manager.RemoveAllCharacters();
        onDisconnect.Invoke();

        newPlayer = null;
        newRemoteChars.List.Clear();
        updateChars.List.Clear();
        removeChars.List.Clear();

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
        ws?.Send(data);
        bytesThisFrame += data.Length;

        if (logBitRate)
            Console.WriteLine($"sent {data.Length} bytes to server");
    }
#endif
}
