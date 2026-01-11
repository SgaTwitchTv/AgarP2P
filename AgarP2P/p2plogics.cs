using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AgarP2P
{
    public class GameNetwork : INetEventListener
    {
        public NetManager Manager { get; private set; }
        public NetPeer ConnectedPeer { get; private set; }

        public GameNetwork()
        {
            Manager = new NetManager(this)
            {
                DisconnectTimeout = 5000,
                UnsyncedEvents = true
            };

            Manager.NatPunchEnabled = true;

            if (!Manager.Start())
            {
                Console.WriteLine("Failed to start network manager.");
                return;
            }

            Console.WriteLine($"Network started on port {Manager.LocalPort}");
            Console.WriteLine("Share this with friend: localhost:" + Manager.LocalPort + " or your LAN IP");
        }

        public void Poll()
        {
            Manager.PollEvents();
        }

        public void Connect(string host, int port)
        {
            Manager.Connect(host, port, "agar_game");
        }

        public void SendPosition(float x, float y, float radius)
        {
            if (ConnectedPeer == null || ConnectedPeer.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put("POS");
            writer.Put(x);
            writer.Put(y);
            writer.Put(radius);

            ConnectedPeer.Send(writer, DeliveryMethod.Unreliable);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            ConnectedPeer = peer;
            Console.WriteLine($"Connected to: {peer}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Disconnected from: {peer}, Reason: {disconnectInfo.Reason}");
            ConnectedPeer = null;
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError errorMessage)
        {

        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            string type = reader.GetString();

            if (type == "POS")
            {
                float x = reader.GetFloat();
                float y = reader.GetFloat();
                float r = reader.GetFloat();

                Console.WriteLine($"Remote pos: {x:F2}, {y:F2} radius {r:F2}");
                // → here update remote player position + size
            }

            reader.Clear();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Not used
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Not used
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("AgarP2PGame");
        }

        public void Shutdown()
        {
            Manager?.Stop();
        }

    }
}