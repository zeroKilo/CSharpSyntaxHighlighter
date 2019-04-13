using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSyntaxHighlighter
{
    public class Token
    {
        public enum TokenType
        {
            block,
            linecomment,
            blockcomment,
            quote,
            keyword
        }

        public string text;
        public TokenType type;
        public Token(TokenType t)
        {
            type = t;
        }
    }
}
