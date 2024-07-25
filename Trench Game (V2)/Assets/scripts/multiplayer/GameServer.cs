using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class GameServer : MonoBehaviour
{
    private WebSocketServer wssv;
    public Dictionary<string, ClientBehavior> clients = new();

    public Queue<Action> actionQueue = new();

    private void Awake()
    {
#if UNITY_SERVER && !UNITY_EDITOR// || true
        //|| true
        ClientBehavior.server = this;

        try
        {
            // Initialize the WebSocket server
            wssv = new WebSocketServer("ws://localhost:8080");
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
        catch (Exception ex)
        {
            Debug.LogError("Error starting WebSocket server: " + ex.Message);
        }
#else
        Debug.Log("This is either not a server or is the Unity editor...");
#endif
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

    void BroadCast (string message)
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
        //Console.WriteLine($"began send data disclude. clients: {clients} discludedIds: {discludeIDs}");
        foreach (var clientBehavior in clients)
        {
            if (!discludeIDs.Contains(clientBehavior.Key))
            {
                clientBehavior.Value.SendData(binary);
            }
        }
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

public class ClientBehavior : WebSocketBehavior
{
    public static GameServer server;

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
        //Console.WriteLine("onmessage started on server");
        server.actionQueue.Enqueue(() => UseData(e.RawData));
        //Console.WriteLine("started coroutine on server");

        void UseData(byte[] rawData)
        {
            if (e.IsText)
            {
                Console.WriteLine("Server received message: " + e.Data);
                //return;
            }

            else if (DataManager.IfGetVector(rawData, out var pos))
            {
                Console.WriteLine("Server received pos: " + pos);

                // Ensure this call is made on the main thread
                //yield return new WaitForEndOfFrame();
                CharacterManager.Manager.mainPlayerCharacter.SetPos(pos, false);

                server.SendDataDisclude(rawData, ID);
            }
        }
    }


    protected override void OnClose(CloseEventArgs e)
    {
        Console.WriteLine("Client disconnected. Reason: " + e.Reason);
        server.clients.Remove(ID);
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Console.WriteLine("penis WebSocket error: " + e.Message);
    }

    public void SendData(byte[] binary)
    {
        Send(binary);
    }
}
