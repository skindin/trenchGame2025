using ENet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    private Host server;
    private Address address;
    Dictionary<uint, Peer> clients = new();

    public bool isServer = false;

    private void Start()
    {
#if UNITY_SERVER && !UNITY_EDITOR
        isServer = true;
        return;
#endif

        if (!isServer)
            return;

        Library.Initialize();
        server = new Host();

        address = new Address();
        address.Port = 7777;
        server.Create(address, 10); // Maximum 10 connections

        Console.WriteLine("Server started...");
    }

    private void Update()
    {
        if (!isServer)
            return;

        ENet.Event netEvent;
        bool polled = false;

        while (!polled)
        {
            if (server.CheckEvents(out netEvent) <= 0)
            {
                if (server.Service(15, out netEvent) <= 0)
                    break;

                polled = true;
            }

            switch (netEvent.Type)
            {
                case ENet.EventType.Connect:
                    Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID);
                    //client = netEvent.Peer; // Save the connected client
                    clients.Add(netEvent.Peer.ID, netEvent.Peer);
                    break;

                case ENet.EventType.Disconnect:
                    Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID);
                    break;

                case ENet.EventType.Receive:
                    Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID);
                    HandlePacket(netEvent.Packet, netEvent.Peer); // Handle the received packet
                    netEvent.Packet.Dispose();
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (!isServer)
            return;

        server.Dispose();
        Library.Deinitialize();
    }

    private void HandlePacket(Packet packet, Peer client)
    {
        byte[] data = new byte[packet.Length];
        packet.CopyTo(data);

        // Handle the data (e.g., convert to protobuf, etc.)
        // For now, just log the data as a string
        string message = System.Text.Encoding.UTF8.GetString(data);
        Console.WriteLine("Received message: " + message);

        // Example: Echo the message back to the client
        SendMessageToClient("Echo: " + message, client);
    }

    public void SendMessageToClient(string message, uint ID)
    {
        byte[] binary = Encoding.UTF8.GetBytes(message);
        SendDataToClient(binary, ID);
    }

    public void SendMessageToClient (string message, Peer client)
    {
        byte[] binary = Encoding.UTF8.GetBytes(message);
        SendDataToClient(binary, client);
    }

    public void SendDataToClient(byte[] binary, uint ID)
    {
        if (!isServer)
            return;

        if (clients.TryGetValue(ID, out var client))
        {
            SendDataToClient(binary, client);
            return;
        }

        Console.WriteLine($"server did not have record for id {ID}");
    }

    public void SendDataToClient(byte[] binary, Peer client)
    {
        Packet packet = default;
        packet.Create(binary, PacketFlags.Reliable);
        client.Send(0, ref packet);
    }
}
