using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace CSharpSyntaxHighlighter
{
    public static class CSSyntaxHighlighter
    {
        public static void Apply(RichTextBox box)
        {
            int pos = box.SelectionStart;
            List<Token> result = FindComments(box.Text);
            result = FindAllQuotes(result);
            result = FindAllKeyWords(result);
            box.Rtf = MakeRTF(result);
            box.SelectionStart = pos;
        }

        public static void HandleNewLine(RichTextBox box)
        {
            int selpos = box.SelectionStart;
            int lnr = box.GetLineFromCharIndex(selpos);
            if (lnr < 2)
                return;
            StringReader sr = new StringReader(box.Text);
            List<string> lines = new List<string>();
            string line = "";
            while ((line = sr.ReadLine()) != null)
                lines.Add(line);
            StringBuilder sb = new StringBuilder();
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
            lines[lnr] = sb.ToString() + lines[lnr];
            sb = new StringBuilder();
            foreach (string s in lines)
                sb.AppendLine(s);
            box.Text = sb.ToString();
            box.SelectionStart = selpos + pos;
        }

        private static List<Token> FindComments(string s)
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
                        if (s[pos + 1] == '*')
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
        private static List<Token> FindAllQuotes(List<Token> list)
        {
            List<Token> result = new List<Token>();
            foreach (Token t in list)
                if (t.type == Token.TokenType.block)
                    result.AddRange(FindQuotes(t.text));
                else
                    result.Add(t);
            return result;
        }
        private static List<Token> FindAllKeyWords(List<Token> list)
        {
            List<Token> result = new List<Token>();
            foreach (Token t in list)
                if (t.type == Token.TokenType.block)
                    result.AddRange(FindKeyWords(t.text));
                else
                    result.Add(t);
            return result;
        }
        private static List<Token> FindQuotes(string s)
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
                    while (pos < s.Length && s[pos++] != '"') ;
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
        private static List<Token> FindKeyWords(string s)
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
        private static string MakeRTF(List<Token> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1031{\fonttbl{\f0\fnil Consolas;}{\f1\fnil\fcharset0 Consolas;}}");
            sb.AppendLine(@"{\colortbl ;\red0\green0\blue0;\red255\green0\blue0;\red0\green128\blue0;\red0\green0\blue255;}");
            sb.AppendLine(@"\viewkind4\uc1\pard\f0\fs20");
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
            return sb.ToString();
        }
        private static string EscapeRTF(string s)
        {
            return s.Replace("\n", "\\par\n")
                    .Replace("{", "\\{")
                    .Replace("}", "\\}")
                    .Replace("\r","");
        }
        private static string[] keywords = new string[] 
        { 
            "abstract", "add", "alias", "as", "ascending", "async", "await", "base", "bool", "break", "by", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "equals", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "nameof", "namespace", "new", "null", "object", "on", "operator", "orderby", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "ref", "remove", "return", "sbyte", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "when", "where", "while", "yield"
        };
    }
}
