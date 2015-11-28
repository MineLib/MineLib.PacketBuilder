//#define SAVE_ALL_TYPES

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using HtmlAgilityPack;

using MineLib.Core.Data;
using MineLib.Core.Data.Anvil;
using MineLib.Core.Data.Structs;
using MineLib.PacketBuilder.WrapperInstances;

using PCLStorage;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MineLib.PacketBuilder
{
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
    public static class CSharpGenerator
    {
        public static string Generate(string className, int id, params FieldInfo[] fields)
        {
            StringBuilder fieldsBuilder = new StringBuilder();
            foreach (FieldInfo field in fields)
            {
                fieldsBuilder.Append("\t\t");
                fieldsBuilder.AppendLine(CreateField(field.FieldType, field.FieldName));
            }

            StringBuilder readPacketBuilder = new StringBuilder();
            foreach (FieldInfo field in fields)
            {
                readPacketBuilder.Append("\t\t\t");
                readPacketBuilder.AppendLine(CreateReadPacket(field.FieldName));
            }

            StringBuilder writePacketBuilder = new StringBuilder();
            foreach (FieldInfo field in fields)
            {
                writePacketBuilder.Append("\t\t\t");
                writePacketBuilder.AppendLine(CreateWritePacket(field.FieldName));
            }


            return string.Format(@"
    public class {0} : ProtobufPacket
    {{
{1}
        public override VarInt ID {{ get {{ return {2}; }} }}

        public override ProtobufPacket ReadPacket(IPacketDataReader reader)
        {{
{3}
            return this;
        }}

        public override ProtobufPacket WritePacket(IPacketStream stream)
        {{
{4}          
            return this;
        }}

    }}", className, fieldsBuilder.ToString(), id, readPacketBuilder.ToString(), writePacketBuilder.ToString());

        }

        public static string CreateField(string fieldType, string fieldName)
        {
            return string.Format(@"public {0} {1} {{ get; set; }}", fieldType, fieldName);
        }

        public static string CreateReadPacket(string fieldName)
        {
            return string.Format(@"{0} = reader.Read({0});", fieldName);
        }

        public static string CreateWritePacket(string fieldName)
        {
            return string.Format(@"stream.Write({0});", fieldName);
        }
    }

    public static class StringExtensions
    {
        public static string ExceptBlanks(this string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

    }

    public class Cell
    {
        public int Collumn;
        public int CollumnLength;
        public int Row;

        public string Value;

        public Cell(int collumn, int row, int collumnLength, string value)
        {
            Collumn = collumn;
            CollumnLength = collumnLength;
            Row = row;

            Value = value;
        }

        public override string ToString() { return string.Format("{0}:{1}", Collumn, Row); }
    }
    public struct RepeatCell
    {
        public int Collumn;
        public int Row;
        public int Count;

        public RepeatCell(int collumn, int row, int count)
        {
            Collumn = collumn;
            Row = row;
            Count = count;
        }
    }
    public class Table
    {
        public string Name;

        public int Width => Headers.Count - 1;
        public int Height { get; set; }

        int CellRow = 0;

        public List<string> Headers = new List<string>();
        public List<Cell> Cells = new List<Cell>();

        List<RepeatCell> RepeatX = new List<RepeatCell>();
        List<RepeatCell> RepeatY = new List<RepeatCell>();

        public void AddCell(int CellCollumn, string value, bool hasRowspan)
        {
            int CellLength = 1;

            if (!hasRowspan)
            {
                foreach (var repeat in RepeatY)
                {
                    if (repeat.Row != CellRow)
                        continue;

                    if (Enumerable.Range(repeat.Collumn, repeat.Count).Contains(CellCollumn))
                        CellRow++;
                    else
                        continue;
                }

                foreach (var repeat in RepeatX)
                {
                    if (repeat.Row != CellRow)
                        continue;

                    if (Enumerable.Range(repeat.Collumn, repeat.Count).Contains(CellCollumn))
                        CellLength = repeat.Count;
                    else
                        continue;
                }
            }

            Cells.Add(new Cell(CellCollumn, CellRow, CellLength, value));


            CellRow += CellLength;
            if (CellRow > Width)
                CellRow = 0;
        }

        public void RepeatAtY(int collumn, int row, int count) { RepeatY.Add(new RepeatCell(collumn, row, count)); }
        public void RepeatAtX(int collumn, int row, int count) { RepeatX.Add(new RepeatCell(collumn, row, count)); }

        public string GetAt(int collumn, int row)
        {
            foreach (Cell cell in Cells)
                if (cell.Collumn == collumn && cell.Row == row)
                    return cell.Value;

            return null;
        }
    }


    public struct FieldInfo
    {
        public string FieldName;
        public string FieldType;

        public FieldInfo(string fieldName)
        {
            FieldName = fieldName;
            FieldType = string.Empty;
        }
        public FieldInfo(string fieldName, string fieldType)
        {
            FieldName = fieldName;
            FieldType = fieldType;
        }

        public override string ToString() { return FieldName; }
    }
    public class PacketTable
    {
        public VarInt ID;
        public List<FieldInfo> FieldInfos = new List<FieldInfo>();

        public override string ToString() { return ID.ToString(); }
    }

    public class Program
    {
        static Program()
        {
            FileSystemWrapper.Instance = new FileSystemWrapperInstance();
        }

        public static void Main(string[] args)
        {
#if SAVE_ALL_TYPES
            List<string> fieldTypes = new List<string>();
#endif
            var list = new List<PacketTable>();

            var classBuilder = new StringBuilder();
            classBuilder.Append(@"
using Aragas.Core.Data;
using Aragas.Core.Extensions;
using Aragas.Core.Interfaces;
using Aragas.Core.Packets;

using MineLib.Core.Data;
using MineLib.Core.Data.Anvil;
using MineLib.Core.Data.Structs;

using System;

namespace MineLib.PacketBuilder
{");

            // Those ID's are not supported and should be parsed manually.
            var bannedIDs = new int[] { 0x20, 0x22, 0x26, 0x34, 0x37, 0x38, 0x44, 0x45 };

            var protocols = GetProtocolTables();
            foreach (var protocol in protocols)
            {
                var infos = new List<FieldInfo>();

                var ID = new VarInt(int.Parse(protocol.GetAt(0, 0).Remove(0, 2), NumberStyles.AllowHexSpecifier));
                if (bannedIDs.Contains(ID))
                    continue;

                for (int ix = 0; ix < protocol.Height; ix++)
                {
                    var fieldName = protocol.GetAt(ix, 3).Replace("-", "").ExceptBlanks();
                    if (fieldName == "nofields")
                        continue;

                    var fieldType = protocol.GetAt(ix, 4).Replace("-", "").ExceptBlanks();
                    var note = protocol.GetAt(ix, 5);

                    infos.Add(new FieldInfo(fieldName, ReplaceTypes(fieldType)));

#if SAVE_ALL_TYPES
                    if (!fieldTypes.Contains(fieldType))
                        fieldTypes.Add(fieldType);
#endif
                }

                list.Add(new PacketTable() { ID = ID, FieldInfos = infos });

                classBuilder.AppendLine(CSharpGenerator.Generate($"{protocol.Name.Replace("-", "").Replace("_", "")}Packet", ID, infos.ToArray()));
            }

#if SAVE_ALL_TYPES
            var fieldTypesBuilder = new StringBuilder();
            foreach (string str in fieldTypes)
                fieldTypesBuilder.AppendLine(str);

            var fileTypeFile = FileSystemWrapper.LogFolder.CreateFileAsync("FileTypes.txt", CreationCollisionOption.ReplaceExisting).Result;
            using (StreamWriter stream = new StreamWriter(fileTypeFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result))
                stream.Write(fieldTypesBuilder.ToString());
#endif
            classBuilder.AppendLine("}");

            var classFile = FileSystemWrapper.LogFolder.CreateFileAsync("Generated.cs", CreationCollisionOption.ReplaceExisting).Result;
            using (StreamWriter stream = new StreamWriter(classFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result))
                stream.Write(classBuilder.ToString());
        }

        public static List<Table> GetProtocolTables()
        {
            var webClient = new WebClient();
            var html = webClient.DownloadString("http://wiki.vg/Protocol");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var ids = new List<string>();
            foreach (var htmlTable in doc.DocumentNode.Descendants("h4"))
            {
                var span = htmlTable.Element("span");
                if (span.GetAttributeValue("class", string.Empty).Equals("mw-headline"))
                    ids.Add(span.GetAttributeValue("id", "NONE"));
            }

            var tables = new List<Table>();
            foreach (var htmlTable in doc.DocumentNode.Descendants("table"))
            {
                if (htmlTable.GetAttributeValue("class", string.Empty).Equals("wikitable"))
                {
                    var table = new Table();

                    // HEADERS
                    var elements = htmlTable.Elements("tr");
                    {
                        var first_tr = elements.ElementAt(0);

                        foreach (var th in first_tr.Elements("th"))
                            table.Headers.Add(th.InnerText);

                        table.Height = elements.Count() - 1;
                    }

                    // VALUES
                    int x = 0;
                    int y = 0;
                    for (int i = 1; i < elements.Count(); i++)
                    {
                        var tr = elements.ElementAt(i);

                        y = 0;
                        foreach (var td in tr.Elements("td"))
                        {
                            string rowspan;
                            if ((rowspan = td.GetAttributeValue("rowspan", string.Empty)) != string.Empty)
                                table.RepeatAtY(x, y, int.Parse(rowspan));

                            string colspan;
                            if ((colspan = td.GetAttributeValue("colspan", string.Empty)) != string.Empty)
                                table.RepeatAtX(x, y, int.Parse(colspan));

                            table.AddCell(x, td.InnerText.Trim().Replace("\n", string.Empty), rowspan != string.Empty);

                            y++;
                        }

                        x++;
                    }

                    tables.Add(table);
                }
            }

            var index = 0;
            return tables.Where(t =>
            {
                var rValue = t.Headers.Any(h => h.Contains("Packet ID"));
                if(rValue)
                {
                    t.Name = ids[index];
                    index++;
                }
                return rValue;
            }).ToList();
        }

        public static string ReplaceTypes(string str)
        {
            switch(str)
            {
                case "VarInt":
                    return typeof(VarInt).Name;

                case "String":
                    return typeof(string).Name;

                case "UnsignedShort":
                    return typeof(ushort).Name;

                case "UnsignedByte":
                    return typeof(byte).Name;

                case "Int":
                    return typeof(int).Name;

                case "Byte":
                    return typeof(sbyte).Name;

                case "Boolean":
                    return typeof(bool).Name;

                case "Chat":
                    return typeof(string).Name;

                case "Long":
                    return typeof(long).Name;

                case "Short":
                    return typeof(short).Name;

                case "Slot":
                    return typeof(ItemStack).Name;

                case "Position":
                    return typeof(Position).Name;

                case "Float":
                    return typeof(float).Name;

                case "Double":
                    return typeof(double).Name;

                case "UUID":
                    return "NOT_SUPPORTED";

                case "Angle":
                    return typeof(byte).Name;

                case "Metadata":
                    return typeof(EntityMetadataList).Name;

                case "ObjectData":
                    return "NOT_SUPPORTED";

                case "ArrayofVarInt":
                    return typeof(VarInt[]).Name;

                case "Chunk":
                    return typeof(Chunk).Name;

                case "Arrayof(Byte,Byte,Byte)":
                    return typeof(byte[]).Name;

                case "OptionalInt":
                    return "NOT_SUPPORTED";

                case "ArrayofSlot":
                    return typeof(ItemStack[]).Name;

                case "OptionalNBTTag":
                    return "NOT_SUPPORTED";

                case "ArrayofString":
                    return typeof(string[]).Name;

                case "OptionalString":
                    return "NOT_SUPPORTED";

                case "OptionalVarInt":
                    return "NOT_SUPPORTED";

                case "OptionalByte":
                    return "NOT_SUPPORTED";

                case "OptionalArrayofString":
                    return "NOT_SUPPORTED";

                case "ByteArray":
                    return typeof(byte[]).Name;

                case "NBTTag":

                case "OptionalFloat":
                    return "NOT_SUPPORTED";

                case "OptionalPosition":
                    return "NOT_SUPPORTED";

                default:
                    return "NOT_SUPPORTED";
            }
        }
    }
}
