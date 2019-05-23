using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Orts.Formats.Msts.Signalling
{
    internal readonly struct Token
    {
        public Token(TokenType type, string value)
        {
            Value = value;
            Type = type;
        }
        public Token(TokenType type, char value)
        {
            Value = value.ToString();
            Type = type;
        }

        public readonly string Value;
        public readonly TokenType Type;
    }

    internal static class OperatorTokenExtension
    {
        public static bool ValidateOperator(string value, char c)
        {
            if (value.Length > 3)
                return false;
            switch (value)
            {
                case "|":
                    return (c == '|');
                case "&":
                    return (c == '&');
                case "^":
                    return false;
                case "!":
                case "*":
                case "%":
                case "=":
                case "/#":
                    return (c == '=');
                case "+":
                case "-":
                    return (c == '=' || c == value[0]);
                case "/":
                case "<":
                case ">":
                    return (c == '=' || c == '#');
                case "#":
                    return (c == '='); ;
                case "==":
                case "!=":
                case "<=":
                case ">=":
                    return (c == '#');
            }
            return false;
        }
    }

    internal class Tokenizer : IEnumerable<Token>
    {
        private TextReader reader;

        internal int LineNumber { get; private set; }

        public Tokenizer(TextReader reader) : this(reader, 0)
        {
        }

        public Tokenizer(TextReader reader, int lineNumberOffset)
        {
            this.reader = reader;
            this.LineNumber = lineNumberOffset;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            string line;
            CommentParserState state = CommentParserState.None;
            StringBuilder value = new StringBuilder();
            bool lineContent = false;

            while ((line = reader.ReadLine()) != null)
            {
                LineNumber++;
                lineContent = false;

                foreach (char c in line)
                {
                    switch (c)
                    {
                        case '/':
                            switch (state)
                            {
                                case CommentParserState.None:
                                    if (value.Length > 0)
                                    {
                                        yield return new Token(TokenType.Value, value.ToString());
                                        value.Length = 0;
                                    }
                                    value.Append(c);
                                    state = CommentParserState.Operator;
                                    continue;
                                case CommentParserState.Operator:
                                    if (value.Length == 1 && value.ToString() == "/")
                                    {
                                        state = CommentParserState.None;
                                        value.Length = value.Length - 1;
                                        goto SkipLineComment;
                                    }
                                    else
                                    {
                                        if (!OperatorTokenExtension.ValidateOperator(value.ToString(), c))
                                        {
                                            yield return new Token(TokenType.Operator, value.ToString());
                                            value.Length = 0;
                                        }
                                        value.Append(c);
                                        continue;
                                    }
                                case CommentParserState.EndComment:
                                    state = CommentParserState.None;
                                    continue;
                                case CommentParserState.OpenComment:
                                    continue;
                                default:
                                    value.Append(c);
                                    continue;
                            }
                        case '*':
                            switch (state)
                            {
                                case CommentParserState.OpenComment:
                                    state = CommentParserState.EndComment;
                                    continue;
                                case CommentParserState.Operator:
                                    if (value.Length == 1 && value.ToString() == "/")
                                    {
                                        value.Length = value.Length - 1;
                                        state = CommentParserState.OpenComment;
                                        continue;
                                    }
                                    if (!OperatorTokenExtension.ValidateOperator(value.ToString(), c))
                                    {
                                        yield return new Token(TokenType.Operator, value.ToString());
                                        value.Length = 0;
                                    }
                                    value.Append(c);
                                    continue;
                                default:
                                    if (value.Length > 0)
                                    {
                                        yield return new Token(TokenType.Value, value.ToString());
                                        value.Length = 0;
                                    }
                                    value.Append(c);
                                    state = CommentParserState.Operator;
                                    continue;
                            }
                        case ';':
                        case '{':
                        case '}':
                        case '(':
                        case ')':
                        case '\t':
                        case ' ':
                        case ',':
                            switch (state)
                            {
                                case CommentParserState.OpenComment:
                                    continue;
                                default:
                                    if (value.Length > 0)
                                    {
                                        yield return new Token((state == CommentParserState.Operator ? TokenType.Operator : TokenType.Value), value.ToString());
                                        value.Length = 0;
                                    }
                                    lineContent = true;
                                    state = CommentParserState.None;
                                    yield return new Token((TokenType)c, c);
                                    continue;
                            }
                        case '|':
                        case '&':
                        case '^':
                        case '!':
                        case '+':
                        case '-':
                        case '%':
                        case '#':
                        case '<':
                        case '>':
                        case '=':
                            switch (state)
                            {
                                case CommentParserState.OpenComment:
                                    continue;
                                case CommentParserState.Operator:
                                    if (!OperatorTokenExtension.ValidateOperator(value.ToString(), c))
                                    {
                                        yield return new Token(TokenType.Operator, value.ToString());
                                        value.Length = 0;
                                    }
                                    value.Append(c);
                                    continue;
                                default:
                                    if (value.Length > 0)
                                    {
                                        yield return new Token(TokenType.Value, value.ToString());
                                        value.Length = 0;
                                    }
                                    value.Append(c);
                                    state = CommentParserState.Operator;
                                    continue;
                            }
                        default:
                            switch (state)
                            {
                                case CommentParserState.OpenComment:
                                    continue;
                                case CommentParserState.Operator:
                                    if (value.Length > 0)
                                    {
                                        yield return new Token(TokenType.Operator, value.ToString());
                                        value.Length = 0;
                                    }
                                    state = CommentParserState.None;
                                    value.Append(char.ToUpper(c));
                                    continue;
                                default:
                                    state = CommentParserState.None;
                                    value.Append(char.ToUpper(c));
                                    continue;
                            }
                    }
                }
                SkipLineComment:
                if (state != CommentParserState.OpenComment)
                {
                    if (value.Length > 0)
                    {
                        lineContent = true;
                        yield return new Token((state == CommentParserState.Operator ? TokenType.Operator : TokenType.Value), value.ToString());
                        value.Length = 0;
                    }
                    if (lineContent)
                        yield return new Token(TokenType.LineEnd, '\n');
                    state = CommentParserState.None;
                }
            }
            if (value.Length > 0)
            {
                yield return new Token(TokenType.Value, value.ToString());
                value.Length = 0;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
