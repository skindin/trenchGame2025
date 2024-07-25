using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            if (DataManager.IfGetVector(rawData, out var pos))
            {
                Debug.Log("Client received pos: " + pos);

                // Ensure this call is made on the main thread
                //yield return new WaitForEndOfFrame();
                CharacterManager.Manager.mainPlayerCharacter.SetPos(pos, false);
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
