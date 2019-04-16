using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpSyntaxHighlighter
{
    public abstract class SyntaxHighlighter
    {
        public abstract void Apply(RichTextBox box, int start = -1, int end = -1);
        public abstract void HandleNewLine(RichTextBox box);
        public abstract string AutoIndent(string text);
    }
}
