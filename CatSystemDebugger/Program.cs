using AsmResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CatSystemDebugger {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("CatSystem2 Debug Enabler - By Marcussacana");
            if (args.Length == 0) {
                Console.WriteLine("Drag&Drop the game executable or directory");
                Console.ReadKey();
                return;
            }
            string minified = args[0].Trim('"').ToLower().TrimEnd();
            string BaseDir = string.Empty;
            if (minified.EndsWith(".exe"))
                BaseDir = Path.GetDirectoryName(args[0].Trim('"'));
            else
                BaseDir = args[0].Trim('"');

            if (!BaseDir.EndsWith("\\"))
                BaseDir += "\\";


            string EXE = string.Empty;
            if (minified.EndsWith(".exe")) {
                EXE = args[0].Trim('"', ' ');
            } else { 
                string[] Config = File.ReadAllLines(BaseDir + "boot.dfn");
                EXE = BaseDir + LoadConfig(Config, "boot");
            }
            string CFG = BaseDir + "config\\startup.xml";
            string KP = BaseDir + "cs2_debug_key.dat";
            DebugPatch(EXE, CFG, KP);
            Console.WriteLine("Press a Key to Exit");
            Console.ReadKey();
            return;
        }

        static readonly byte[] VCODE = new byte[]
        { 0x85, 0x83, 0xD7, 0x8A, 0x8B, 0x88, 0x04, 0xCB, 0xC3, 0x78, 0xCF, 0xD0, 0xB1, 0xA4, 0xE5, 0x9A };

        static readonly byte[] KEYCODE = new byte[]
        { 0xBA, 0xA4, 0xA3, 0xA9, 0xA0, 0xA4, 0xA1, 0xA1 };

        static readonly byte[] DBGKEY = new byte[]
        { 0xE2, 0x2A, 0xA8, 0x65, 0xC5, 0xCE, 0x04, 0x55, 0xDA, 0xE4, 0xCD, 0x9A, 0x96, 0xF2, 0x15,
          0x1E, 0x1B, 0x0E, 0x13, 0xD0, 0xAB, 0x8F, 0xF8, 0x3D, 0x3F, 0xEA, 0x46, 0x73, 0x37, 0xFA,
          0x8D, 0x90, 0x48, 0x4B, 0x83, 0xFF, 0x39, 0x21, 0xD0, 0x50, 0x2D, 0x12, 0x36, 0xB3, 0xF0,
          0xC3, 0xD5, 0x8C, 0xD7, 0xB3, 0xDD, 0xF5, 0xF1, 0x8A, 0x77, 0xE2, 0xBD, 0x3F, 0xD2, 0x4F,
          0xC9, 0x66, 0x0E, 0xFA };

        static readonly byte[] Validation = new byte[] { 0x75, 0x75, 0x8D, 0x4C };
        private static void DebugPatch(string EXE, string CFG, string KeyPath) {
            File.Move(EXE, EXE + ".bak");
            byte[] Executable = File.ReadAllBytes(EXE + ".bak");
            WindowsAssembly Assembly = WindowsAssembly.FromFile(EXE + ".bak");
            var Entries = Assembly.RootResourceDirectory.Entries;
            try {
                UpdateResourceByName(ref Executable, "V_CODE", VCODE, ref Entries);
                UpdateResourceByName(ref Executable, "V_CODE2", VCODE, ref Entries);
                UpdateResourceByName(ref Executable, "KEY_CODE", KEYCODE, ref Entries);
            }
            catch {
                File.Move(EXE + ".bak", EXE);
                Log("Failed to Patch, This is a CT2 Game?");
                return;
            }
            Log("All Resources Patched...");
            File.WriteAllBytes(KeyPath, DBGKEY);
            Log("Debug Key Generated...");
            
            if (!Patch(ref Executable, Validation, new byte[] { 0xEB }, "BYPASS")) {
                File.Move(EXE + ".bak", EXE);
                File.Delete(EXE + ".tmp");
                Log("Failed to Patch, Unsupported Engine Version");
                return;
            }
            File.WriteAllBytes(EXE, Executable);

            string[] XML = File.ReadAllLines(CFG, Encoding.UTF8);
            for (int i = 0; i < XML.Length; i++) {
                string Line = XML[i].Trim().ToLower();
                if (Line.Contains("v_code") && !Line.StartsWith("<!--"))
                    XML[i] = "<!-- " + XML[i] + " -->";
            }
            File.WriteAllLines(CFG, XML);
            Log("Successfully patched");

            ConsoleColor bk = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Atention: To works you need decrypt all Int's Packget and delete the \"startup.xml\" into game save directory.");
            Console.ForegroundColor = bk;
        }

        private static bool Patch(ref byte[] Data, byte[] Ori, byte[] Patch, string Name) {
            Log(string.Format("Patching Resource: {0}", Name));
            Log(string.Format("Original Data: {0}", ByteArrToStr(Ori)));
            Log(string.Format("Patch Data: {0}", ByteArrToStr(Patch)));

            bool Patched = false;
            for (uint i = 0; i < Data.LongLength; i++) {
                if (EqualsAt(Data, Ori, i) && !Patched) {
                    Patched = true;
                    for (uint x = 0; x < Patch.LongLength; x++)
                        Data[i + x] = Patch[x];
                    break;
                } else if (EqualsAt(Data, Ori, i)) {
                    Patched = false;
                }
            }
            return Patched;
        }

        private static bool EqualsAt(byte[] Data, byte[] DataToCompare, uint Pos) {
            if (DataToCompare.Length + Pos > Data.Length)
                return false;
            for (uint i = 0; i < DataToCompare.Length; i++)
                if (DataToCompare[i] != Data[i + Pos])
                    return false;
            return true;
        }

        private static void UpdateResourceByName(ref byte[] Executable, string Name, byte[] Content, ref IList<ImageResourceDirectoryEntry> Entries) {
            var Resource = (from e in Entries where e.Name == Name select e).Single();
            byte[] OriContent = Resource.SubDirectory.Entries[0].SubDirectory.Entries[0].DataEntry.Data;

            if (!Patch(ref Executable, OriContent, Content, Name))
                throw new Exception("Failed to Patch.");
        }


        private static string ByteArrToStr(byte[] Arr) {
            string Rst = "0x";
            foreach (byte b in Arr)
                Rst += b.ToString("X2");
            return Rst;
        }
        private static void Log(string Content) {
            Console.WriteLine("{0} at {1}: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), Content);
#if DEBUG
            System.Threading.Thread.Sleep(300);
#endif
        }
        private static string LoadConfig(string[] config, string CfgName) {
            string Config = (from s in config where s.ToLower().TrimStart().StartsWith(CfgName.ToLower()) select s).Single();
            string[] Cnt = Config.Split('"');
            return Cnt[1].Trim();
        }
    }
}
