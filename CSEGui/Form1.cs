using CatSceneEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CSEGui {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            MessageBox.Show("This is a demo DLL GUI", "CSEGUI");
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
            catch { }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\n' || e.KeyChar == '\r') {
                try {
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                }
                catch {

                }
            }
        }

        CSTHelper Editor;
        CSTLHelper Editor2;
        private bool CSTMode = true;

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "ALL CST Files|*.cst|All CSTL Files|*.cstl";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            byte[] Script = System.IO.File.ReadAllBytes(fd.FileName);
            CSTMode = fd.FilterIndex == 1;

            if (CSTMode) {
                Editor = new CSTHelper(Script, true, true);

                listBox1.Items.Clear();
                foreach (string str in Editor.Import())
                    listBox1.Items.Add(str);
            } else {
                Editor2 = new CSTLHelper(Script);

                listBox1.Items.Clear();
                foreach (string str in Editor2.Import())
                    listBox1.Items.Add(str);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "ALL CST Files|*.cst|All CSTL Files|*.cstl";
            fd.FilterIndex = (CSTMode ? 0 : 1);
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Strings = new string[listBox1.Items.Count];
            listBox1.Items.CopyTo(Strings, 0);

            System.IO.File.WriteAllBytes(fd.FileName, CSTMode ? Editor.Export(Strings) : Editor2.Export(Strings));
            MessageBox.Show("File Saved", "CSEGui", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void createToolStripMenuItem_Click(object sender, EventArgs e) {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = "Select folder with directories or files to repack";
            if (fb.ShowDialog() != DialogResult.OK)
                return;
            if (!fb.SelectedPath.EndsWith("\\"))
                fb.SelectedPath += '\\';
            bool SubDirMode = Directory.GetDirectories(fb.SelectedPath).Length > 0;
            if (SubDirMode) {
                foreach (string dir in Directory.GetDirectories(fb.SelectedPath))
                    PackDir(dir);
            } else
                PackDir(fb.SelectedPath);

            MessageBox.Show("Repacked", "CSEGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PackDir(string Dir) {
            if (!Dir.EndsWith("\\"))
                Dir += '\\';
            string OutPath = Dir.TrimEnd('\\', ' ', '~') + ".int";

            string[] Files = Directory.GetFiles(Dir, "*.*");
            FileEntry[] Entries = new FileEntry[Files.Length];
            for (uint i = 0; i < Files.Length; i++) {
                string FN = Path.GetFileName(Files[i]);
                Entries[i] = new FileEntry() {
                    FileName = FN,
                    Content = new StreamReader(Files[i]).BaseStream
                };
            }

            Stream Output = new StreamWriter(OutPath).BaseStream;

            Int.Repack(Output, Entries, true);
        }

        private void extractToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Int Packgets|*.int";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            int i = 2;
            string OutDir = fd.FileName + "~\\";
            while (Directory.Exists(OutDir))
                OutDir = fd.FileName + string.Format("~{0}\\", i++);
            Directory.CreateDirectory(OutDir);

            Stream Reader = new StreamReader(fd.FileName).BaseStream;
            FileEntry[] Entries = Int.Open(Reader);

            foreach (FileEntry Entry in Entries) {
                Stream Output = new StreamWriter(OutDir + Entry.FileName).BaseStream;
                CopyStream(Entry.Content, Output);
                Entry.Content.Close();
                Output.Close();
            }

            Reader.Close();
        }

        private void CopyStream(Stream Input, Stream Output) {
            int Readed = 0;
            byte[] Buffer = new byte[1024];
            do {
                Readed = Input.Read(Buffer, 0, Buffer.Length);
                Output.Write(Buffer, 0, Readed);
            } while (Readed > 0);
        }

        private void escapeToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Multiselect = true;
            if (fd.ShowDialog() != DialogResult.OK)
                return;
            
            foreach (string file in fd.FileNames) {
                byte[] Scr = File.ReadAllBytes(file);
                var Editor = new CSTHelper(Scr);
                string[] Dialogues = Editor.Import();

            }
        }
    }
}
