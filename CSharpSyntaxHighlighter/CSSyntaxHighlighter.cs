using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CSharpSyntaxHighlighter
{
    public class CSSyntaxHighlighter : SyntaxHighlighter
    {
        [DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(IntPtr hWndLock);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("User32.dll")]
        private extern static int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private StringBuilder sb = new StringBuilder();
        private StringReader sr;
        public override void Apply(RichTextBox box, int start = -1, int end = -1)
        {
            LockWindowUpdate(box.Handle);
            int scrollY = GetScrollPos((IntPtr)box.Handle, 1) << 16;
            int pos = box.SelectionStart;
            string s = box.Text;
            string before = "";
            string after = "";
            if (start >= 0 && end >= 0)
            {
                if (start > 0)
                    before = s.Substring(0, start);
                if (end < s.Length)
                    after = s.Substring(end, s.Length - end);
                s = s.Substring(start, end - start);
            }
            List<Token> result = FindComments(s);
            result = FindAllQuotes(result);
            result = FindAllKeyWords(result);
            box.Rtf = MakeRTF(result, before, after);
            box.SelectionStart = pos;
            uint wParam = 0x4 | (uint)scrollY;
            SendMessage(box.Handle, 0x0115, new IntPtr(wParam), new IntPtr(0));
            LockWindowUpdate(IntPtr.Zero);
        }
        public override void HandleNewLine(RichTextBox box)
        {
            LockWindowUpdate(box.Handle);
            int scrollY = GetScrollPos((IntPtr)box.Handle, 1) << 16;
            int selpos = box.SelectionStart;
            int lnr = box.GetLineFromCharIndex(selpos);
            if (lnr < 1)
                return;
            sr = new StringReader(box.Text);
            List<string> lines = new List<string>();
            string line = "";
            while ((line = sr.ReadLine()) != null)
                lines.Add(line);
            sb.Clear();
            int pos = 0;
            line = lines[lnr - 1];
            while (pos < line.Length)
            {
                if (line[pos] == ' ' || line[pos] == '\t')
                    sb.Append(line[pos]);
                else
                    break;
                pos++;
            }
            if (lnr == lines.Count)
                lines.Add(sb.ToString());
            else
                lines[lnr] = sb.ToString() + lines[lnr];
            sb.Clear();
            foreach (string s in lines)
                sb.AppendLine(s);
            box.Text = sb.ToString();
            box.SelectionStart = selpos + pos;
            uint wParam = 0x4 | (uint)scrollY;
            SendMessage(box.Handle, 0x0115, new IntPtr(wParam), new IntPtr(0));
            LockWindowUpdate(IntPtr.Zero);
        }
        public override string AutoIndent(string text)
        {
            sb.Clear();
            List<Token> result = FindComments(text);
            result = FindAllQuotes(result);
            int depth = 0;
            string line = "";
            bool skipNext = false;
            foreach (Token t in result)
                if (t.type == Token.TokenType.block)
                {
                    string s = t.text;
                    int pos = 0;
                    if (skipNext)
                    {
                        skipNext = false;
                        pos = SkipEmpty(s, pos);
                    }
                    while (pos < s.Length)
                    {
                        switch (s[pos])
                        {
                            case '\n':
                                sb.Append(s[pos]);
                                pos = SkipEmpty(s, pos + 1);
                                sb.Append(MakeTabs(depth));
                                break;
                            case '{':
                                sb.Append("\n" + MakeTabs(depth) + s[pos] + "\n");
                                pos = SkipEmpty(s, pos + 1);
                                depth++;
                                sb.Append(MakeTabs(depth));
                                break;
                            case '}':
                                if (depth > 0)
                                    depth--;
                                sb.Append("\n" + MakeTabs(depth) + s[pos] + "\n");
                                pos = SkipEmpty(s, pos + 1);
                                sb.Append(MakeTabs(depth));
                                break;
                            default:
                                sb.Append(s[pos]);
                                pos++;
                                break;
                        }
                    }
                }
                else if (t.type == Token.TokenType.linecomment)
                {
                    sb.Append(t.text + MakeTabs(depth));
                    skipNext = true;
                }
                else if (t.type == Token.TokenType.blockcomment)
                {
                    sb.Append("\n");
                    sr = new StringReader(t.text);
                    while ((line = sr.ReadLine()) != null)
                        sb.Append(MakeTabs(depth) + line.Trim() + "\n");
                    skipNext = true;
                }
                else
                    sb.Append(t.text);
            sr = new StringReader(sb.ToString());
            sb.Clear();
            while ((line = sr.ReadLine()) != null)
                if (line.Trim() != "")
                    sb.Append(line + "\n");
            return sb.ToString();
        }

        private List<Token> FindComments(string s)
        {
            List<Token> result = new List<Token>();
            int pos = 0, last = 0;
            while (pos < s.Length)
            {
                if (s[pos] == '/')
                {
                    if (pos >= s.Length - 1 || (s[pos + 1] != '/' && s[pos + 1] != '*'))
                        pos++;
                    else
                    {
                        Token t = new Token(Token.TokenType.block);
                        t.text = s.Substring(last, pos - last);
                        result.Add(t);
                        last = pos;
                        if (s[pos + 1] == '/')
                        {
                            while (pos < s.Length && s[pos++] != '\n') ;
                            t = new Token(Token.TokenType.linecomment);
                            t.text = s.Substring(last, pos - last);
                            result.Add(t);
                        }
                        else if (s[pos + 1] == '*')
                        {
                            while (pos++ < s.Length - 1)
                                if (s[pos] == '*' && s[pos + 1] == '/')
                                {
                                    pos += 2;
                                    break;
                                }
                            t = new Token(Token.TokenType.blockcomment);
                            t.text = s.Substring(last, pos - last);
                            result.Add(t);
                            last = pos;
                        }
                        last = pos;
                    }
                }
                else
                    pos++;
            }
            if (last != pos)
            {
                Token t = new Token(Token.TokenType.block);
                t.text = s.Substring(last, pos - last);
                result.Add(t);
            }
            return result;
        }
        private List<Token> FindAllQuotes(List<Token> list)
        {
            List<Token> result = new List<Token>();
            foreach (Token t in list)
                if (t.type == Token.TokenType.block)
                    result.AddRange(FindQuotes(t.text));
                else
                    result.Add(t);
            return result;
        }
        private List<Token> FindAllKeyWords(List<Token> list)
        {
            List<Token> result = new List<Token>();
            foreach (Token t in list)
                if (t.type == Token.TokenType.block)
                    result.AddRange(FindKeyWords(t.text));
                else
                    result.Add(t);
            return result;
        }
        private List<Token> FindQuotes(string s)
        {
            List<Token> result = new List<Token>();
            int pos = 0, last = 0;
            while (pos < s.Length)
            {
                if (s[pos] == '"')
                {
                    Token t = new Token(Token.TokenType.block);
                    t.text = s.Substring(last, pos - last);
                    result.Add(t);
                    last = pos;
                    pos++;
                    while (pos < s.Length - 1)
                    {
                        if (s[pos - 1] != '\\' && s[pos] == '"')
                            break;
                        pos++;
                    }
                    pos++;
                    t = new Token(Token.TokenType.quote);
                    t.text = s.Substring(last, pos - last);
                    result.Add(t);
                    last = pos;
                }
                else
                    pos++;
            }
            if (last != pos)
            {
                Token t = new Token(Token.TokenType.block);
                t.text = s.Substring(last, pos - last);
                result.Add(t);
            }
            return result;
        }
        private List<Token> FindKeyWords(string s)
        {
            List<Token> result = new List<Token>();
            List<string> temp = new List<string>();
            string start = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
            string allowed = start + "0123456789";
            int pos = 0, last = 0;
            while (pos < s.Length)
            {
                if (start.Contains(s[pos]))
                {
                    if(last != pos)
                        temp.Add(s.Substring(last, pos - last));
                    last = pos;
                    while (pos < s.Length && allowed.Contains(s[pos])) 
                        pos++;
                    temp.Add(s.Substring(last, pos - last));
                    last = pos;
                }
                else
                    pos++;
            }
            if (last != pos)
                temp.Add(s.Substring(last, pos - last));
            foreach(string word in temp)
                if (keywords.Contains(word.ToLower()))
                {
                    Token t = new Token(Token.TokenType.keyword);
                    t.text = word;
                    result.Add(t);
                }
                else
                {
                    Token t = new Token(Token.TokenType.block);
                    t.text = word;
                    result.Add(t);
                }
            return result;
        }
        private string MakeRTF(List<Token> list, string before = "", string after = "")
        {
            sb.Clear();
            sb.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1031{\fonttbl{\f0\fnil Consolas;}{\f1\fnil\fcharset0 Consolas;}}");
            sb.Append(@"{\colortbl ;");
            foreach (Color c in tokenColors)
                sb.AppendFormat("\\red{0}\\green{1}\\blue{2};", c.R, c.G, c.B);
            sb.AppendLine("}");
            sb.AppendLine(@"\viewkind4\uc1\pard\f0\fs20\cf1 " + EscapeRTF(before));
            foreach(Token t in list)
                switch (t.type)
                {
                    case Token.TokenType.keyword:
                        sb.Append("\\cf4\\b " + EscapeRTF(t.text) + "\\b0 ");
                        break;
                    case Token.TokenType.linecomment:
                    case Token.TokenType.blockcomment:
                        sb.Append("\\cf3 " + EscapeRTF(t.text));
                        break;
                    case Token.TokenType.quote:
                        sb.Append("\\cf2 " + EscapeRTF(t.text));
                        break;
                    default:
                        sb.Append("\\cf1 " + EscapeRTF(t.text));
                        break;
                }
            sb.Append("\\cf1 " + EscapeRTF(after));
            return sb.ToString();
        }
        private string EscapeRTF(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\n", "\\par\n")
                    .Replace("{", "\\{")
                    .Replace("}", "\\}")
                    .Replace("\r", "");
        }
        private string MakeTabs(int count)
        {
            sb.Clear();
            for (int i = 0; i < count; i++)
                sb.Append('\t');
            return sb.ToString();
        }
        private int SkipEmpty(string s, int start)
        {
            int pos = start;
            while (pos < s.Length)
            {
                if (s[pos] != ' ' && s[pos] != '\t')
                    break;
                pos++;
            }
            return pos;
        }
        public string[] keywords = new string[] 
        { 
            "abstract", "add", "alias", "as", "ascending", "async", "await", "base", "bool", "break", "by", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "equals", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "nameof", "namespace", "new", "null", "object", "on", "operator", "orderby", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "when", "where", "while", "yield"
        };
        public Color[] tokenColors = new Color[]
        {
            Color.Black,        //generic
            Color.Red,          //string/quote
            Color.DarkGreen,    //comment
            Color.Blue          //keyword
        };
    }
}
