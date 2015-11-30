using System;
using System.Collections.Generic;
using System.Text;

namespace MineLib.PacketBuilder
{
    public enum BoundTo { NONE, Client, Server, Share }
    public enum State { NONE, Handshaking, Play, Status, Login }

    /*
public class ChatMessagePacket : ProtobufPacket
{
    public string Message { get; set; }

    public override VarInt ID { get { return 0x01; } }

    public override ProtobufPacket ReadPacket(IPacketDataReader reader)
    {
        Message = reader.ReadString();
        return this;
    }

    public override ProtobufPacket WritePacket(IPacketStream stream)
    {
        stream.Write(Message);

        return this;
    }
}
*/

    public class ProtobufPacketGenerator
    {
        class FieldInfo
        {
            public string Name;
            public string Type;

            public FieldInfo(string fieldName, string fieldType)
            {
                Name = fieldName;
                Type = fieldType;
            }

            public override string ToString() { return $"{Type} {Name}"; }
        }

        public string ClassName { get; set; }
        public int PacketID { get; set; }
        public BoundTo BoundTo { get; set; }
        public State State { get; set; }

        List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();

        public ProtobufPacketGenerator(string className, int packetID)
        {
            ClassName = className;
            PacketID = packetID;
        }

        public ProtobufPacketGenerator(string className, int packetID, BoundTo boundTo, State state)
        {
            ClassName = className;
            PacketID = packetID;
            BoundTo = boundTo;
            State = state;
        }

        public void AddField(string name, Type type) { Fields.Add(new FieldInfo(name, type.Name)); }
        public void AddField(string name, string type) { Fields.Add(new FieldInfo(name, type)); }

        private string GenerateFields()
        {
            var builder = new StringBuilder();
            foreach (var field in Fields)
                builder.Append("\t\t").AppendLine($"public {field.Type} {field.Name};");

            return builder.ToString();
        }
        private string GenerateReadPacket()
        {
            var builder = new StringBuilder();
            foreach (var field in Fields)
                builder.Append("\t\t\t").AppendLine($"{field.Name} = reader.Read({field.Name});"); ;

            return builder.ToString();
        }
        private string GenerateWritePacket()
        {
            var builder = new StringBuilder();
            foreach (var field in Fields)
                builder.Append("\t\t\t").AppendLine($"stream.Write({field.Name});");

            return builder.ToString();
        }

        public string GenerateClass()
        {
            return $@"
using Aragas.Core.Data;
using Aragas.Core.Extensions;
using Aragas.Core.Interfaces;
using Aragas.Core.Packets;

using MineLib.Core.Data;
using MineLib.Core.Data.Anvil;
using MineLib.Core.Data.Structs;

using System;

namespace MineLib.PacketBuilder
{{
    public class {ClassName} : ProtobufPacket
    {{
{GenerateFields()}
        public override VarInt ID {{ get {{ return {PacketID}; }} }}

        public override ProtobufPacket ReadPacket(IPacketDataReader reader)
        {{
{GenerateReadPacket()}
            return this;
        }}

        public override ProtobufPacket WritePacket(IPacketStream stream)
        {{
{GenerateWritePacket()}          
            return this;
        }}

    }}
}}";
        }
    }
}
