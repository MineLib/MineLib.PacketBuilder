//#define SAVE_ALL_TYPES

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using HtmlAgilityPack;

using MineLib.Core.Data;
using MineLib.Core.Data.Anvil;
using MineLib.Core.Data.Structs;

using MineLib.PacketBuilder.Extensions;
using MineLib.PacketBuilder.WrapperInstances;

using PCLStorage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace MineLib.PacketBuilder
{
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
            var generatorList = new List<ProtobufPacketGenerator>();


            // Those ID's are not supported and should be parsed manually.
            var bannedIDs = new int[] { 0x20, 0x22, 0x26, 0x34, 0x37, 0x38, 0x44, 0x45 };

            var protocols = GetProtocolTables();
            foreach (var protocol in protocols)
            {
                var className = $"{protocol.Name.Replace("-", "").Replace("_", "")}Packet";
                var id = new VarInt(int.Parse(protocol.GetAt(0, 0).Remove(0, 2), NumberStyles.AllowHexSpecifier));
                if (bannedIDs.Contains(id))
                    continue;

                var state = (State) Enum.Parse(typeof(State), protocol.GetAt(0, 1).ExceptBlanks());
                var boundTo = (BoundTo) Enum.Parse(typeof(BoundTo), protocol.GetAt(0, 2).ExceptBlanks());

                var builder = new ProtobufPacketGenerator(className, id, boundTo, state);

                for (int ix = 0; ix < protocol.Height; ix++)
                {
                    var fieldName = protocol.GetAt(ix, 3).Replace("-", "").ExceptBlanks();
                    if (fieldName == "nofields")
                        continue;

                    var fieldType = protocol.GetAt(ix, 4).Replace("-", "").ExceptBlanks();
                    var note = protocol.GetAt(ix, 5);

                    builder.AddField(fieldName, ReplaceTypes(fieldType));

#if SAVE_ALL_TYPES
                    if (!fieldTypes.Contains(fieldType))
                        fieldTypes.Add(fieldType);
#endif
                }

                generatorList.Add(builder);
            }

#if SAVE_ALL_TYPES
            var fieldTypesBuilder = new StringBuilder();
            foreach (string str in fieldTypes)
                fieldTypesBuilder.AppendLine(str);

            var fileTypeFile = FileSystemWrapper.OutputFolder.CreateFileAsync("FileTypes.txt", CreationCollisionOption.ReplaceExisting).Result;
            using (StreamWriter stream = new StreamWriter(fileTypeFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result))
                stream.Write(fieldTypesBuilder.ToString());
#endif

            foreach (var generator in generatorList)
            {
                var folder = FileSystemWrapper.OutputFolder.CreateFolderAsync("Generated", CreationCollisionOption.OpenIfExists).Result;

                if (generator.BoundTo != BoundTo.NONE)
                    folder = folder.CreateFolderAsync(generator.BoundTo.ToString(), CreationCollisionOption.OpenIfExists).Result;
                if (generator.State != State.NONE)
                    folder = folder.CreateFolderAsync(generator.State.ToString(), CreationCollisionOption.OpenIfExists).Result;

                var classFile = folder.CreateFileAsync($"{generator.ClassName}.cs", CreationCollisionOption.ReplaceExisting).Result;
                using (StreamWriter stream = new StreamWriter(classFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite).Result))
                    stream.Write(generator.GenerateClass());
            }
        }

        public static List<WikiTable> GetProtocolTables()
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

            var tables = new List<WikiTable>();
            foreach (var htmlTable in doc.DocumentNode.Descendants("table"))
            {
                if (htmlTable.GetAttributeValue("class", string.Empty).Equals("wikitable"))
                {
                    var table = new WikiTable();

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

                //case "UUID":
                //    return "NOT_SUPPORTED";

                case "Angle":
                    return typeof(byte).Name;

                case "Metadata":
                    return typeof(EntityMetadataList).Name;

                //case "ObjectData":
                //    return "NOT_SUPPORTED";

                case "ArrayofVarInt":
                    return typeof(VarInt[]).Name;

                case "Chunk":
                    return typeof(Chunk).Name;

                case "Arrayof(Byte,Byte,Byte)":
                    return typeof(byte[]).Name;

                //case "OptionalInt":
                //    return "NOT_SUPPORTED";

                case "ArrayofSlot":
                    return typeof(ItemStack[]).Name;

                //case "OptionalNBTTag":
                //    return "NOT_SUPPORTED";

                case "ArrayofString":
                    return typeof(string[]).Name;

                //case "OptionalString":
                //    return "NOT_SUPPORTED";

                //case "OptionalVarInt":
                //    return "NOT_SUPPORTED";

                //case "OptionalByte":
                //    return "NOT_SUPPORTED";

                //case "OptionalArrayofString":
                //    return "NOT_SUPPORTED";

                case "ByteArray":
                    return typeof(byte[]).Name;

                case "NBTTag":

                //case "OptionalFloat":
                //    return "NOT_SUPPORTED";

                //case "OptionalPosition":
                //    return "NOT_SUPPORTED";

                default:
                    return str;
            }
        }
    }
}
