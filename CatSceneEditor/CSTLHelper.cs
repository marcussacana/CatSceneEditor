using System.Collections.Generic;

namespace CatSceneEditor {
    public class CSTLHelper {
        CSTL Editor;
        string Lang = string.Empty;
        int LangID;
        public CSTLHelper(byte[] Script, string Lang) {
            Editor = new CSTL(Script);
            this.Lang = Lang;
        }
        string[] Strings;
        public CSTLHelper(byte[] Script) {
            Editor = new CSTL(Script);
            Lang = "en";
        }

        Dictionary<int, int> StrMap = new Dictionary<int, int>();
       
        public string[] Import() {
            StrMap = new Dictionary<int, int>();
            Strings = Editor.Import();
            for (int i = 0; i < Editor.Langs.Length; i++) {
                if (Editor.Langs[i] == Lang) {
                    LangID = i;
                    break;
                }
            }
            List<string> Result = new List<string>();
            for (int i = 0; i < Strings.LongLength; i++) {
                bool IsTarget = ((i / 2) % Editor.Langs.Length) == LangID;
                if (IsTarget) {
                    StrMap[Result.Count] = i;
                    Result.Add(Strings[i]);                    
                }
            }

            return Result.ToArray();
        }

        public byte[] Export(string[] Content) {
            for (int i = 0; i < Content.Length; i++)
                Strings[StrMap[i]] = Content[i];

            return Editor.Export(Strings);
        }
    }
}
