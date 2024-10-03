using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System;

public abstract class WSServerBase : MonoBehaviour
{ 
    public WebSocketServer wssv;
    public Dictionary<string, ClientBehavior> clients = new();
    public Queue<Action> actionQueue = new();

    public abstract bool IsServer { get; }

    private void Awake()
    {
        if (!IsServer)
            return;

        //startTick = DateTime.UtcNow.Ticks;

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

        //Application.targetFrameRate = targetFramerate;
        //Debug.Log($"Target framerate set to {targetFramerate}");

        OnAwake();
    }

    public virtual void OnAwake ()
    {

    }

    private void Update()
    {
        if (IsServer)
            return;

        PreMsgUpdate();

        while (actionQueue.Count > 0)
        {
            Action action;
            lock (actionQueue)
            {
                action = actionQueue.Dequeue();
            }
            action?.Invoke();
        }

        PostMsgUpdate();
    }

    public virtual void PreMsgUpdate()
    {

    }

    public virtual void PostMsgUpdate()
    {

    }

    public virtual void ClientConnected (ClientBehavior client)
    {
        clients.Add(client.ID, client);
        Debug.Log($"client {client.ID} connected");
    }

    public virtual void ClientDisconnected (ClientBehavior client)
    {
        clients.Remove(client.ID);
        Debug.Log($"client {client.ID} disconnected");
    }

    public virtual void OnClientMessage (ClientBehavior client, MessageEventArgs e)
    {

    }

    public virtual void OnClientError(ClientBehavior client, ErrorEventArgs e)
    {
        Debug.LogError(e.Exception);
    }

    public class ClientBehavior : WebSocketBehavior
    {
        public static WSServerBase server;

        protected override void OnOpen()
        {
            //base.OnOpen();

            server.actionQueue.Enqueue(() => server.ClientConnected(this));
        }

        protected override void OnClose(CloseEventArgs e)
        {
            //base.OnClose(e);

            server.actionQueue.Enqueue(() => server.ClientDisconnected(this));
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            //base.OnMessage(e);
            server.actionQueue.Enqueue(() => server.OnClientMessage(this, e));
        }

        protected override void OnError(ErrorEventArgs e)
        {
            server.actionQueue.Enqueue(() => server.OnClientError(this, e));
        }

        public void SendData (byte[] data)
        {
            Send(data);
        }
    }
}
