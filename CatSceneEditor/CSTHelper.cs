using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CatSceneEditor {
    public class CSTHelper {

        CatScene Editor;
        bool Wordwrap = true;
        bool Decode = true;

        public CSTHelper(byte[] Script, bool Wordwrap, bool Decode) {
            Editor = new CatScene(Script);
            this.Wordwrap = Wordwrap;
            this.Decode = Decode;
        }

        public CSTHelper(byte[] Script) { Editor = new CatScene(Script); }

        StringEntry[] Entries;

        private Dictionary<uint, string> Prefix;
        private Dictionary<uint, string> Sufix;
        private Dictionary<uint, string> Prefix2;
        private Dictionary<uint, string> Sufix2;

        private const string FN = "\\fn";

        public string[] Import() {
            Prefix = new Dictionary<uint, string>();
            Sufix = new Dictionary<uint, string>();
            Prefix2 = new Dictionary<uint, string>();
            Sufix2 = new Dictionary<uint, string>();
            Entries = Editor.Import();
            string[] Strings = (from e in Entries where e.Type == 8193 || e.Type == 8449 select e.Content).ToArray();
            bool InChoice = false;
            foreach (var Entry in Entries) {
                if (Entry.Type != 12289)
                    continue;
                if (Entry.Content == "fselect") {
                    InChoice = true;
                    continue;
                }
                if (!InChoice)
                    continue;
                string[] Parts = Entry.Content.Split(' ');
                if (Parts.Length != 3) {
                    InChoice = false;
                    continue;
                }
                if (!int.TryParse(Parts.First(), out int tmp)) {
                    InChoice = false;
                    continue;
                }
                Strings = Strings.Concat(new string[] { Parts[2].Replace("_", " ") }).ToArray();
            }

            if (Wordwrap) {
                for (uint i = 0; i < Strings.LongLength; i++) {
                    string String = Strings[i];
                    CutString(ref String, i, false);
                    FakeDecode(ref String, true);
                    String = WordwrapUnescape(String);
                    CutString(ref String, i, true);
                    Strings[i] = String;
                }
            }

            return Strings;
        }

        public byte[] Export(string[] Strings) {
            bool InChoice = false;
            uint x = 0;
            for (uint i = 0; i < Entries.LongLength; i++) {
                if (Entries[i].Type == 8193 || Entries[i].Type == 8449) {
                    string String = Prefix[x] + Strings[x].Trim() + Sufix[x];
                    if (Wordwrap && String.Contains(" ")) {
                        String = WordwrapEscape(String);
                    }
                    FakeDecode(ref String, false);
                    Entries[i].Content = Prefix2[x] + String + Sufix2[x];
                    x++;
                }
            }
            for (uint i = 0; i < Entries.LongLength; i++) {
                if (Entries[i].Type == 12289) {
                    if (Entries[i].Content == "fselect") {
                        InChoice = true;
                        continue;
                    }
                    if (!InChoice)
                        continue;
                    string[] Parts = Entries[i].Content.Split(' ');
                    if (Parts.Length != 3) {
                        InChoice = false;
                        continue;
                    }
                    if (!int.TryParse(Parts.First(), out int tmp)) {
                        InChoice = false;
                        continue;
                    }
                    Entries[i].Content = Parts[0] + ' ' + Parts[1] + ' ' + Strings[x++].Replace(" ", "_");
                }
            }

            return Editor.Export(Entries);
        }
        private string WordwrapUnescape(string String) {
            String = String.Replace("[", "");
            String = String.Replace("]", "");
            return String;
        }
        private string WordwrapEscape(string String) {
            string[] Words = String.Split(' ');
            String = string.Empty;
            for (int z = 0; z < Words.Length; z++) {
                string Word = Words[z];
                if (z == 0) {
                    String += Word + ' ';
                    continue;
                }
                if (Word.Contains("\\n")) {
                    string tmp = Word.Replace("\\n", "\n");
                    foreach (string str in tmp.Split('\n'))
                        String += string.Format("[{0}]\\n", str);
                    String = String.Substring(0, String.Length - 1);
                    String += ' ';
                } else if (Word.Contains(":")) {
                    string[] Split = Word.Split(':');
                    String += string.Format("[{0}]:", Split[0]);
                    for (int a = 1; a < Split.Length; a++) {
                        String += Split[a] + ':';
                    }
                    String = String.Substring(0, String.Length - 1);
                    String += ' ';
                } else
                    String += string.Format("[{0}] ", Word);
            }
            String = String.Substring(0, String.Length - 1);
            return String;
        }

        //\fnThe [interval] [between] [classes,] [or] ["recess,"] [is] [fundamentally] [a] [time] [for] [lazing] [around.]\fn
        
        private void FakeDecode(ref string Line, bool Decode) {
            if (!Decode)
                return;

            string[] Source = new string[] { "\\'", "\\\"", "\\n" };
            string[] Target = new string[] { "'",   "\"",   "\n"  };

            for (int i = 0; i < Source.Length; i++) {
                Line = Line.Replace(Decode ? Source[i]: Target[i], !Decode ? Source[i] : Target[i]);
            }
        }

        List<string> Prefixs = new List<string>(new string[] { "\\n", "\\@", "\\r", "\\pc", "\\pl", "\\pr", "\\wf", "\\w", "\\fr", "\\fnl","\\fss", "\\fnn", "\\fll", "\\fn", "\\f", " ", "-" });
        private void CutString(ref string String, uint ID, bool Cutted) {
            string Prefix = string.Empty;
            while (GetPrefix(String) != null) {
                string Rst = GetPrefix(String);
                Prefix += String.Substring(0, Rst.Length);
                String = String.Substring(Rst.Length, String.Length - Rst.Length);
            }

            if (Cutted)
                this.Prefix[ID] = Prefix;
            else
                Prefix2[ID] = Prefix;

            string Sufix = string.Empty;
            while (GetSufix(String) != null) {
                string Rst = GetSufix(String);
                Sufix = String.Substring(String.Length - Rst.Length, Rst.Length) + Sufix;
                String = String.Substring(0, String.Length - Rst.Length);
            }

            if (Cutted)
                this.Sufix[ID] = Sufix;
            else
                Sufix2[ID] = Sufix;
        }

        private string GetPrefix(string String) {
            foreach (string str in Prefixs)
                if (String.ToLower().StartsWith(str)) 
                    return str;                
            return null;
        }
        private string GetSufix(string String) {
            foreach (string str in Prefixs)
                if (String.ToLower().EndsWith(str))
                    return str;
            return null;
        }
       
    }
}
