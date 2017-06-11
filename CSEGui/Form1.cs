using CatSceneEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        CSTL Editor;
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "ALL CST Files|*.cst";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            byte[] Script = System.IO.File.ReadAllBytes(fd.FileName);

            Editor = new CSTL(Script, true);

            listBox1.Items.Clear();
            foreach (string str in Editor.Import())
                listBox1.Items.Add(str);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "ALL CST Files|*.cst";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Strings = new string[listBox1.Items.Count];
            listBox1.Items.CopyTo(Strings, 0);

            System.IO.File.WriteAllBytes(fd.FileName, Editor.Export(Strings));
            MessageBox.Show("File Saved", "CSEGui", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
