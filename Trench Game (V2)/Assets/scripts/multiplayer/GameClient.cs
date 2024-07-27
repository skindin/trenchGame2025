using System;
using System.Text;
using ENet;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    private Host client;
    private Peer server;
    private bool isConnected = false;

    public bool isClient = true;

    private void Start()
    {
#if UNITY_SERVER && !UNITY_EDITOR
        isClient = false;
#endif

        if (!isClient)
            return;

        Logger.Log("Initializing ENet Library...");
        Library.Initialize();
        client = new Host();
        Address address = new Address();
        address.SetHost("localhost");
        address.Port = 1234;
        Logger.Log("Creating client host...");
        client.Create();

        Logger.Log("Attempting to connect to server at localhost:1234...");
        server = client.Connect(address);
        Logger.Log("Client started and attempting to connect to server...");
    }

    private void Update()
    {
        if (!isClient)
            return;

        ENet.Event netEvent;

        while (client.Service(15, out netEvent) > 0)
        {
            switch (netEvent.Type)
            {
                case ENet.EventType.Connect:
                    Logger.Log("Connected to server - ID: " + netEvent.Peer.ID);
                    isConnected = true;
                    SendMessageToServer("Hello, server!");
                    break;

                case ENet.EventType.Disconnect:
                    Logger.Log("Disconnected from server - ID: " + netEvent.Peer.ID);
                    isConnected = false;
                    break;

                case ENet.EventType.Receive:
                    Logger.Log("Packet received from server - ID: " + netEvent.Peer.ID);
                    HandlePacket(netEvent.Packet);
                    netEvent.Packet.Dispose();
                    break;
            }
        }

        // Additional logging to confirm Update is running
        if (!isConnected)
        {
            Logger.Log("Client not connected yet.");
        }
    }

    private void OnDestroy()
    {
        if (!isClient)
            return;

        client.Dispose();
        Library.Deinitialize();
        Logger.Log("Client and ENet library deinitialized.");
    }

    private void HandlePacket(Packet packet)
    {
        byte[] data = new byte[packet.Length];
        packet.CopyTo(data);

        // Handle the data (e.g., convert to protobuf, etc.)
        // For now, just log the data as a string
        string message = Encoding.UTF8.GetString(data);
        Logger.Log("Client Received message: " + message);
    }

    public void SendMessageToServer(string message)
    {
        byte[] binary = Encoding.UTF8.GetBytes(message);
        SendDataToServer(binary);
    }

    public void SendDataToServer(byte[] binary)
    {
        if (!isClient)
            return;

        Packet packet = default;
        packet.Create(binary, PacketFlags.Reliable);
        server.Send(0, ref packet);
        Logger.Log("Sent message to server: " + Encoding.UTF8.GetString(binary));
    }
}
