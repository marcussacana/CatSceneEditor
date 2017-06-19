using AdvancedBinary;
using NPCSManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CatSceneEditor {
    public static class Int {

        public static FileEntry[] Open(Stream Input) {
            StructReader Reader = new StructReader(Input, false, Encoding.GetEncoding(932));
            IntHeader Header = new IntHeader();
            Reader.ReadStruct(ref Header);
            if (Header.Signature != "KIF\x0")
                throw new Exception("Invalid Packget");

            FileEntry[] Entries = new FileEntry[Header.FileCount];
            for (uint i = 0; i < Entries.Length; i++) {
                FileEntry Entry = new FileEntry();
                Reader.ReadStruct(ref Entry);
                Entry.Content = new VirtStream(Input, Entry.Postion, Entry.Length);
                Entry.FileName = Entry.FileName.TrimEnd('\x0');
                if (Entry.FileName == "__key__.dat")
                    throw new Exception("Encrypted Packget Not Supported.");
                Entries[i] = Entry;
            }

            return Entries;
        }

        public static void Repack(Stream Output, FileEntry[] Files, bool CloseStreams = true) {
            StructWriter Writer = new StructWriter(Output, false, Encoding.GetEncoding(932));
            IntHeader Header = new IntHeader() {
                Signature = "KIF\x0",
                FileCount = (uint)Files.LongLength
            };

            Writer.WriteStruct(ref Header);

            uint Len = 0;
            for (uint i = 0; i < Files.Length; i++) {
                FileEntry Entry = new FileEntry() {
                    FileName = Files[i].FileName,
                    Postion = (0x48 * (uint)Files.LongLength) + 0x8 + Len,
                    Length = (uint)Files[i].Content.Length
                };
                Writer.WriteStruct(ref Entry);
                Len += Entry.Length;
            }

            for (uint i = 0; i < Files.Length; i++) {
                Tools.CopyStream(Files[i].Content, Writer.BaseStream);
                if (CloseStreams)
                    Files[i].Content.Close();
            }

            if (CloseStreams)
                Output.Close();
        }
    }

    struct IntHeader {//0x8 of len
        [FString(Length = 0x4)]
        public string Signature;
        public uint FileCount;
    }
    public struct FileEntry {//0x48 of len
        [FString(Length = 0x40)]
        public string FileName;
        internal uint Postion;
        internal uint Length;

        [Ignore]
        public Stream Content;
    }
}
