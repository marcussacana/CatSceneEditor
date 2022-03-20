using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CatSceneEditor
{
    public class CatScene
    {
        
        ScriptHeader Header;
        byte[] Script;
        bool Decompressed = false;

        uint EntryCount { get { return (Header.StringTable - Header.OffsetTable)/4; } }
        uint HeaderLen { get { return (uint)Tools.GetStructLength(new ScriptHeader()); } }

        public Encoding Encoding = Encoding.GetEncoding(932);

        public CatScene(byte[] Script) { this.Script = Script; }

        public StringEntry[] Import() {
            if (!Decompressed)
                Decompress();

            Header = new ScriptHeader();
            StructReader Reader = new StructReader(new MemoryStream(Script), false, Encoding);

            Reader.ReadStruct(ref Header);

            if (Header.ScriptLength + 0x10 != Script.Length)
                throw new Exception("Corrupted Script");

            List<StringEntry> Strings = new List<StringEntry>();
            for (uint i = 0; i < EntryCount; i++) {
                Reader.Seek(Header.OffsetTable + HeaderLen + (i*4), SeekOrigin.Begin);
                uint Offset = Reader.ReadUInt32() + Header.StringTable + HeaderLen;

                Reader.Seek(Offset, SeekOrigin.Begin);
                StringEntry Entry = new StringEntry();
                Reader.ReadStruct(ref Entry);
                Strings.Add(Entry);
            }

            Reader.Close();
            return Strings.ToArray();
        }

        public byte[] Export(StringEntry[] Strings) {
            ScriptHeader NewHeader = new ScriptHeader();
            Tools.CopyStruct(Header, ref NewHeader);

            MemoryStream UnkData = new MemoryStream();
            MemoryStream OffsetData = new MemoryStream();
            MemoryStream StringData = new MemoryStream();

            MemoryStream Reader = new MemoryStream(Script);
            Reader.Seek(0x10, SeekOrigin.Begin);

            Algo.CopyStream(Reader, UnkData, NewHeader.UnkCount * 8);
            Reader.Close();

            StructWriter OffsetWriter = new StructWriter(OffsetData, false, Encoding);
            StructWriter StringWriter = new StructWriter(StringData, false, Encoding);


            for (uint i = 0; i < EntryCount; i++) {
                OffsetWriter.Write((uint)StringWriter.BaseStream.Length);
                StringWriter.WriteStruct(ref Strings[i]);
            }
            OffsetWriter.Seek(0, SeekOrigin.Begin);
            StringWriter.Seek(0, SeekOrigin.Begin);

            NewHeader.ScriptLength = (uint)(OffsetWriter.BaseStream.Length + StringWriter.BaseStream.Length + UnkData.Length);
            NewHeader.OffsetTable = (uint)UnkData.Length;
            NewHeader.StringTable = (uint)(UnkData.Length + OffsetData.Length);

            byte[] Output = new byte[0x10 + UnkData.Length + OffsetData.Length + StringData.Length];

            Tools.BuildStruct(ref NewHeader, false, Encoding).CopyTo(Output, 0);

            UnkData.ToArray().CopyTo(Output, 0x10);
            OffsetData.ToArray().CopyTo(Output, 0x10 + UnkData.Length);
            StringData.ToArray().CopyTo(Output, 0x10 + UnkData.Length + OffsetData.Length);

            UnkData.Close();
            StringWriter.Close();
            OffsetWriter.Close();

            return Compress(Output);
        }

        private byte[] Compress(byte[] Input) {
			byte[] Compressed;
            Algo.CompressData(Input, out Compressed);
            CatHeader Header = new CatHeader() {
                Singnature = "CatScene",
                CompressedSize = (uint)Compressed.LongLength,
                DecompressedSize = (uint)Input.LongLength
            };

            byte[] Output = new byte[0x10 + Header.CompressedSize];
            Tools.BuildStruct(ref Header, false, Encoding).CopyTo(Output, 0);
            Compressed.CopyTo(Output, 0x10);

            Input = new byte[0];
            Compressed = new byte[0];

            return Output;
        }

        private void Decompress() {
            CatHeader CompHeader = new CatHeader();
            StructReader Reader = new StructReader(new MemoryStream(Script), false, Encoding);

            Reader.ReadStruct(ref CompHeader);

            if (CompHeader.Singnature != "CatScene")
                throw new Exception("This isn't a valid CatSystem2 Script");
            MemoryStream Decompressed = new MemoryStream();
            Algo.DecompressData(Reader.BaseStream, Decompressed);

            if (CompHeader.DecompressedSize != Decompressed.Length)
                throw new Exception("Corrupted Script");

            Reader.Close();
            Script = Decompressed.ToArray();
            Decompressed.Close();
            this.Decompressed = true;
        }
    }


#pragma warning disable 649
    struct CatHeader {
        [FString(Length = 8)]
        internal string Singnature;
        internal uint CompressedSize;
        internal uint DecompressedSize;
    }

    struct ScriptHeader {
        internal uint ScriptLength;

        //Bytecode? String map? Anyway, is the count of the first part of the script after the header... 
        //every single entry 2 uint... in other words 1 entry = 8 bytes
        internal uint UnkCount;

        internal uint OffsetTable;
        internal uint StringTable;
    }

    [DebuggerDisplay("{Type}: {Content}")]
    public struct StringEntry {
        public ushort Type;

        [CString]
        public string Content;        
    }
#pragma warning restore 649
}
