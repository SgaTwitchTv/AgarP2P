using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Networking
{
    public enum MessageType : byte
    {
        PlayerState = 1,
        PelletEaten = 2,
        PlayerEaten = 3,
        Join = 4,
        Levae = 5
    }

    public struct PlayerStateMessage
    {
        public int PlayerId;
        public float X;
        public float Y;
        public float Radius;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)MessageType.PlayerState);
            writer.Write(PlayerId);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Radius);
        }

        public static PlayerStateMessage Deserialize(BinaryReader reader)
        {
            PlayerStateMessage msg;
            msg.PlayerId = reader.ReadInt32();
            msg.X = reader.ReadSingle();
            msg.Y = reader.ReadSingle();
            msg.Radius = reader.ReadSingle();
            return msg;
        }
    }
    public struct PlayerEatenMessage
    {
        public int KillerId;
        public int VictimId;
    }
}
