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

    //private void Start()
    //{
    //    Connect();
    //    //ID = 
    //}

    private void Update()
    {
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
    }

    private void LateUpdate()
    {
        if (logBitRate)
        {
            //Debug.Log($"Sent {bytesThisFrame} bytes this frame");

            pastByteRecords.Add(bytesThisFrame);

            if (pastByteRecords.Count > averageBitRateFrames)
                pastByteRecords.RemoveAt(0);

            averageBitRate = Mathf.RoundToInt(LogicAndMath.GetListValueTotal(pastByteRecords.ToArray(), byteCount => byteCount) / averageBitRateFrames / Time.deltaTime);

            if (bytesThisFrame > 0)
                Debug.Log($"average bit rate: {averageBitRate}");

            bytesThisFrame = 0;
        }
    }

    public void Connect()
    {
        // Initialize WebSocket
        ws = new WebSocket($"ws://{serverAdress}:8080/ClientBehavior");

        // Set up message received handler
        ws.OnMessage += OnMessage;

        ws.OnOpen += (sender, e) =>
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

        ws.OnClose += (sender, e) => actionQueue.Enqueue(() => {
            Debug.Log("Disconnected from server");
            //UIUtils.ResetScene();
            CharacterManager.Manager.RemoveAllCharacters();
            //CharacterManager.Manager.RemoveAllCharacters();
            onDisconnect.Invoke();
        });

        //ws.OnOpen += (sender, e) => {
        //    var idData = new ConnectionId() { ID = Guid.NewGuid().ToString() };
        //    var binary = DataManager.MessageToBinary(idData);
        //    SendData(binary);
        //    };

        // Connect to WebSocket server
        ws.ConnectAsync();
    }

    void OnMessage(object sender, MessageEventArgs e)
    {
        actionQueue.Enqueue(() => OnData(e.RawData));

        void OnData(byte[] rawData)
        {
            //bool uhoh = false;

            if (DataManager.IfGet<BaseMessage>(rawData, out var baseMessage))
            {
                switch (baseMessage.TypeCase)
                {
                    case BaseMessage.TypeOneofCase.NewPlayerGrant:
                        {
                            var grant = baseMessage.NewPlayerGrant;

                            if (!CharacterManager.Manager.localPlayerCharacter)
                            {
                                var id = grant.CharacterID;
                                var pos = DataManager.ConvertDataToVector(grant.Pos);
                                SpawnManager.Manager.SpawnLocalPlayer(pos, id);

                                Debug.Log($"spawned local player {id} at {pos}");
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.NewRemoteChar:
                        {
                            var newRemoteData = baseMessage.NewRemoteChar;

                            var id = newRemoteData.CharacterID;
                            var pos = DataManager.ConvertDataToVector(newRemoteData.Pos);
                            var name = newRemoteData.Name;

                            SpawnManager.Manager.SpawnRemoteCharacter(pos, id).characterName = name;

                            Debug.Log($"spawned remote character {id} named {name} at {pos}");
                        }
                        break;

                    case BaseMessage.TypeOneofCase.UpdateCharData:
                        {
                            var updateData = baseMessage.UpdateCharData;

                            var id = updateData.CharacterID;

                            // Ensure this call is made on the main thread
                            //yield return new WaitForEndOfFrame();
                            var character = CharacterManager.Manager.active.Find(character => character.id == id);

                            if (character)
                            {
                                if (updateData.Pos != null)
                                {
                                    var pos = DataManager.ConvertDataToVector(updateData.Pos);
                                    character.SetPos(pos, false);
                                    Debug.Log($"updated pos of character {updateData.CharacterID} to {pos}");
                                }

                                if (updateData.HasName)
                                {
                                    character.characterName = updateData.Name;

                                    Debug.Log($"updated characterName of character {updateData.CharacterID} to {character.characterName}");
                                }
                            }
                            else
                            {
                                Debug.Log($"this client doesn't have a character with id {id}");
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.RemoveCharOfID:
                        {
                            var removeId = baseMessage.RemoveCharOfID;

                            var removeChar = CharacterManager.Manager.active.Find(character => character.id == removeId);

                            if (removeChar)
                            {
                                CharacterManager.Manager.RemoveCharacter(removeChar);
                            }

                            Debug.Log($"character {removeId} was removed");
                        }
                        break;
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
    }
#endif
}
