using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CatSceneEditor {
    public class CSTL {
        byte[] Script;

        public CSTL(byte[] Script) { this.Script = Script; }

        public string[] Langs { get; private set; }

        CSTLHeader Header = new CSTLHeader();
        public string[] Import() {
            StructReader Reader = new StructReader(new MemoryStream(Script), false, Encoding.UTF8);
            Reader.ReadStruct(ref Header);
            if (Header.Signature != "CSTL")
                throw new Exception("Invalid Format");

            Langs = new string[ReadNum(Reader)];
            for (long i = 0; i < Langs.Length; i++) 
                Langs[i] = ReadString(Reader);

            long Count = (ReadNum(Reader) * 2) * Langs.Length;
            string[] Strings = new string[Count];
            for (long i = 0; i < Count; i++) {
                Strings[i] = ReadString(Reader);
            }

            Reader.Close();
            return Strings;
        }

        public byte[] Export(string[] Content) {
            MemoryStream Output = new MemoryStream();
            StructWriter Writer = new StructWriter(Output, false, Encoding.UTF8);
            Writer.WriteStruct(ref Header);

            WriteNum(Writer, Langs.Length);
            foreach (string Lang in Langs)
                WriteString(Writer, Lang);

            WriteNum(Writer, (Content.Length/2)/Langs.Length);
            foreach (string str in Content)
                WriteString(Writer, str);

            byte[] Result = Output.ToArray();
            Writer.Close();
            return Result;
        }

        private void WriteString(StructWriter Stream, string Content) {
            byte[] Buffer = Encoding.UTF8.GetBytes(Content);
            WriteNum(Stream, Buffer.LongLength);

            Stream.Write(Buffer, 0, Buffer.Length);
        }

        private void WriteNum(StructWriter Stream, long Value) {
            List<byte> Rst = new List<byte>();
            Rst.Add(0x00);
            while (Value-- > 0) {
                if (Rst[Rst.Count - 1] == 0xFF)
                    Rst.Add(0x00);
                Rst[Rst.Count - 1]++;
            }

            if (Rst[Rst.Count - 1] == 0xFF)
                Rst.Add(0x00);

            byte[] Buffer = Rst.ToArray();

            Stream.Write(Buffer, 0, Buffer.Length);
        }
        private long ReadNum(StructReader Stream) {
            long Value = 0;
            while (Stream.Peek() == 0xFF)
                Value += Stream.ReadByte();
            Value += Stream.ReadByte();

            return Value;
        }
        private string ReadString(StructReader Stream) {
            byte[] Buffer = new byte[ReadNum(Stream)];
            Stream.Read(Buffer, 0, Buffer.Length);

            return Encoding.UTF8.GetString(Buffer);
        }

        struct CSTLHeader {
            [FString(Length = 4)]
            public string Signature;
            uint Unk;
        }
        struct StringEntry {
            [PString(PrefixType = Const.UINT8)]
            public string Content;
        }
    }
}
