using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSyntaxHighlighter;

namespace Example
{
    public partial class Form1 : Form
    {
        bool preventUpdate = false;
        CSSyntaxHighlighter hl = new CSSyntaxHighlighter();
        public Form1()
        {
            InitializeComponent();
            preventUpdate = true;
            hl.Apply(rtb1);
            preventUpdate = false;

        }

        private void rtb1_TextChanged(object sender, EventArgs e)
        {
            if (preventUpdate)
                return;
            preventUpdate = true;
            hl.Apply(rtb1);
            preventUpdate = false;
        }

        private void rtb1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                preventUpdate = true;
                hl.HandleNewLine(rtb1);
                hl.Apply(rtb1);
                preventUpdate = false;
            }
        }

        private void autoIndentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            preventUpdate = true;
            rtb1.Text = hl.AutoIndent(rtb1.Text);
            hl.Apply(rtb1);
            preventUpdate = false;
        }
    }
}
