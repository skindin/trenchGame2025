using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.FullSerializer;
//using UnityEditor.U2D.Animation;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
using WebSocketSharp;

public class GameClient : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR

    private WebSocket ws;
    public string ID;

    Queue<Action> actionQueue = new Queue<Action>();

    private void Start()
    {
        RunWebSocketClient();
        //ID = 
    }

    private void Update()
    {
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

    private void RunWebSocketClient()
    {
        // Initialize WebSocket
        ws = new WebSocket("ws://localhost:8080/ClientBehavior");

        // Set up message received handler
        ws.OnMessage += OnMessage;

        ws.OnOpen += (sender, e) =>
        {
            var baseMessage = new BaseMessage() { NewPlayerRequest = true };

            //var binary = DataManager.MessageToBinary(baseMessage);

            ws.Send(baseMessage.ToByteArray());
            //Debug.Log("connected to server");
        };

        //ws.OnOpen += (sender, e) => {
        //    var idData = new ConnectionId() { ID = Guid.NewGuid().ToString() };
        //    var binary = DataManager.MessageToBinary(idData);
        //    SendData(binary);
        //    };

        // Connect to WebSocket server
        ws.Connect();
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
                                CharacterManager.Manager.NewLocalPlayer(pos, id);

                                Debug.Log($"spawned local player {id} at {pos}");
                            }
                        }
                        break;

                    case BaseMessage.TypeOneofCase.NewRemoteChar:
                        {
                            var newRemoteData = baseMessage.NewRemoteChar;

                            var id = newRemoteData.CharacterID;
                            var pos = DataManager.ConvertDataToVector(newRemoteData.Pos);

                            CharacterManager.Manager.NewRemoteCharacter(pos, id);

                            Debug.Log($"spawned remote character {id} at {pos}");
                        }
                        break;

                    case BaseMessage.TypeOneofCase.UpdateCharData:
                        {
                            var updateData = baseMessage.UpdateCharData;

                            var id = updateData.CharacterID;
                            var pos = DataManager.ConvertDataToVector(updateData.Pos);

                            // Ensure this call is made on the main thread
                            //yield return new WaitForEndOfFrame();
                            var character = CharacterManager.Manager.active.Find(character => character.id == id);

                            if (character)
                            {
                                character.SetPos(pos, false);
                                Debug.Log($"updated pos of character {updateData.CharacterID} to {pos}");
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
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    public void SendData (byte[] data)
    {
        ws?.Send(data);
    }
#endif
}
