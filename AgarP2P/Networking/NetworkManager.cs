using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Networking;

namespace Networking
{
    public class NetworkManager : INetEventListener
    {
        private NetManager? _net;
        private NetPeer? _serverPeer; // for client side
        private readonly NetDataWriter _writer = new NetDataWriter();

        public bool IsHost { get; private set; }
        public int LocalPlayerId { get; private set; } = new Random().Next(1000, 9999);

        // Events for AgarGame to hook into
        public event Action<PlayerStateMessage>? OnPlayerStateReceived;
        public event Action<int>? OnPeerConnectedEvent;
        public event Action<int>? OnPeerDisconnectedEvent;
        public event Action<int, int>? OnPlayerEatenReceived;

        public NetworkManager()
        {
            _net = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };
        }

        public void StartHost(int port = 9050)
        {
            IsHost = true;

            var listener = this;
            _net = new NetManager(listener)
            {
                IPv6Enabled = false,
                ReuseAddress = true // allow reuse of 9050
            };

            _net.Start(port);
            Console.WriteLine($"[Host] Started on port {port}");
        }

        public void Connect(string address, int port = 9050)
        {
            IsHost = false;

            var listener = this;
            _net = new NetManager(listener)
            {
                IPv6Enabled = false,
                ReuseAddress = true // allow same socket family
            };

            // Start client on a random port instead of 9050
            _net.Start();

            _serverPeer = _net.Connect(address, port, "AgarP2P");
            Console.WriteLine($"[Client] Connecting to {address}:{port}");
        }

        public void PollEvents()
        {
            _net?.PollEvents();
        }

        public void Stop()
        {
            _net?.Stop();
        }


        // --- Event callbacks ---

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"[Network] Peer connected: {peer.Address}:{peer.Port}");
            OnPeerConnectedEvent?.Invoke(peer.Id);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"[Network] Peer disconnected: {peer.Address}:{peer.Port}");
            OnPeerDisconnectedEvent?.Invoke(peer.Id);
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("AgarP2P");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"[Network] Error from {endPoint}: {socketError}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional: track latency if needed
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            // Deserialize message type
            var stream = new MemoryStream(reader.GetRemainingBytes());
            using var br = new BinaryReader(stream);
            var msgType = (MessageType)br.ReadByte();

            switch (msgType)
            {
                case MessageType.PlayerState:
                    var msg = PlayerStateMessage.Deserialize(br);
                    OnPlayerStateReceived?.Invoke(msg);
                    break;

                case MessageType.PlayerEaten:
                    {
                        var killerId = reader.GetInt();
                        var victimId = reader.GetInt();
                        OnPlayerEatenReceived?.Invoke(killerId, victimId);
                        break;
                    }
            }

            reader.Recycle();
        }

        // Sending messages

        public void SendPlayerState(float x, float y, float radius)
        {
            if (!_net.IsRunning) return;

            var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            new PlayerStateMessage
            {
                PlayerId = LocalPlayerId,
                X = x,
                Y = y,
                Radius = radius
            }.Serialize(bw);

            _writer.Reset();
            _writer.Put(ms.ToArray());

            if (IsHost)
            {
                _net.SendToAll(_writer, DeliveryMethod.Unreliable);
            }
            else if (_serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected)
            {
                _serverPeer.Send(_writer, DeliveryMethod.Unreliable);
            }
        }

        // Newer LiteNetLib adds this overload, so we implement it to satisfy INetEventListener
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            OnNetworkReceive(peer, reader, deliveryMethod);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Not used
        }
        public void SendPlayerEaten(int killerId, int victimId)
        {
            _writer.Reset();
            _writer.Put((byte)MessageType.PlayerEaten);
            _writer.Put(killerId);
            _writer.Put(victimId);
            Broadcast(_writer, DeliveryMethod.ReliableOrdered);
        }
        private void Broadcast(NetDataWriter writer, DeliveryMethod method)
        {
            if (_net == null)
                return;

            foreach (var peer in _net.ConnectedPeerList)
            {
                peer.Send(writer, method);
            }
        }

    }
}
