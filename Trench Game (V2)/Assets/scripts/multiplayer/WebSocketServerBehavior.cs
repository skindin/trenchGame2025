using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WebSocketServerBehaviour : MonoBehaviour
{
    private WebSocketServer wssv;

    private void Awake()
    {
#if UNITY_SERVER && !UNITY_EDITOR 
//|| true
        try
        {
            // Initialize the WebSocket server
            wssv = new WebSocketServer("ws://localhost:8080");
            wssv.AddWebSocketService<Echo>("/Echo");
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
        catch (Exception ex)
        {
            Debug.LogError("Error starting WebSocket server: " + ex.Message);
        }
#else
        Debug.Log("This is either not a server or is the Unity editor...");
#endif
    }

    void BroadCast (string message)
    {
        //foreach (var session in wssv.WebSocketServices["/Echo"].Sessions) ;

        var sessions = wssv.WebSocketServices["/Echo"].Sessions;

        sessions.Broadcast(message);

        //for (var i = 0; i < sessions.Count; i++)
        //{

        //}
    }

//    private void Update()
//    {
//#if UNITY_SERVER && !UNITY_EDITOR 
////||true
//        BroadCast("server spamming client");
//#endif
//    }



    private void OnDestroy()
    {
        if (wssv != null)
        {
            wssv.Stop();
            wssv = null;
        }
    }
}

public class Echo : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("New client connected.");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            Debug.Log("Received message: " + e.Data);

            // Echo the message back to the client
            Send(e.Data);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error handling message: " + ex.Message);
            //var idk = Sessions.SendTo;
        }
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("Client disconnected. Reason: " + e.Reason);
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError("WebSocket error: " + e.Message);
    }

    public void SendData (string message)
    {
        Send(message);
    }
}
