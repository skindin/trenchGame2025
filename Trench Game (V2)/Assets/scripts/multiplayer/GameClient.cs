using System;
using System.Text;
using ENet;
//using UnityEditor.PackageManager;
using UnityEngine;

public class GameClient : MonoBehaviour
{
//#if !UNITY_SERVER || UNITY_EDITOR

    private Host client;
    private Peer server;
    private bool isConnected = false;

    public bool isClient = true;

    private void Start()
    {
#if UNITY_SERVER
        isClient = false;
        return;
#endif

        if (!isClient)
            return;

        Library.Initialize();
        client = new Host();
        Address address = new ();
        address.SetHost("localhost");
        address.Port = 1234;
        client.Create();

        server = client.Connect(address);
        Debug.Log("Client started and connecting to server...");
    }

    private void Update()
    {
        if (!isClient)
            return;

        ENet.Event netEvent;

        if (!isConnected)
        {
            while (client.Service(15, out netEvent) > 0)
            {
                switch (netEvent.Type)
                {
                    case ENet.EventType.Connect:
                        Debug.Log("Connected to server - ID: " + netEvent.Peer.ID);
                        isConnected = true;
                        SendMessageToServer("Hello, server!");
                        break;

                    case ENet.EventType.Disconnect:
                        Debug.Log("Disconnected from server - ID: " + netEvent.Peer.ID);
                        isConnected = false;
                        break;

                    case ENet.EventType.Receive:
                        Debug.Log("Packet received from server - ID: " + netEvent.Peer.ID);
                        HandlePacket(netEvent.Packet);
                        netEvent.Packet.Dispose();
                        break;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (!isClient)
            return;

        client.Dispose();
        Library.Deinitialize();
    }

    private void HandlePacket(Packet packet)
    {
        byte[] data = new byte[packet.Length];
        packet.CopyTo(data);

        // Handle the data (e.g., convert to protobuf, etc.)
        // For now, just log the data as a string
        string message = Encoding.UTF8.GetString(data);
        Debug.Log("Received message: " + message);
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
    }

//#endif
}
