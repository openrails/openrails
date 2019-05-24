using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Orts.Formats.Msts.Signalling
{
    internal class Parser : IEnumerable<Script>
    {
        private readonly Tokenizer tokenizer;

        public int LineNumber { get { return tokenizer.LineNumber; } }

        public Parser(TextReader reader)
        {
            this.tokenizer = new Tokenizer(reader);
        }

        internal enum ScriptParserState
        {
            None,
            ScriptName,
            Remark,
        }

        public IEnumerator<Script> GetEnumerator()
        {
            bool inScript = false;
            ScriptParserState parserState = ScriptParserState.None;
            BlockBase currentBlock = null;

            foreach (Token token in tokenizer)
            {
                if (inScript)
                {
                    switch (token.Type)
                    {
                        case TokenType.StatementEnd:
                            currentBlock = currentBlock.CompleteBlock() ?? throw new InvalidDataException($"Malformed Script detected");
                            parserState = ScriptParserState.None;
                            continue;
                        case TokenType.LineEnd:
                            if (parserState == ScriptParserState.ScriptName)
                                parserState = ScriptParserState.None;
                            continue;
                        case TokenType.BlockOpen:
                            currentBlock = currentBlock.StartBlock(tokenizer.LineNumber);
                            continue;
                        case TokenType.BracketOpen:
                            currentBlock = currentBlock.StartEnclosure(tokenizer.LineNumber);
                            continue;
                        case TokenType.BlockClose:
                        case TokenType.BracketClose:
                            if ((token.Type == TokenType.BracketClose && currentBlock is Enclosure) ||
                            (token.Type == TokenType.BlockClose && currentBlock is Block))
                            {
                                currentBlock = currentBlock.CompleteBlock() ?? throw new InvalidDataException($"Malformed Script detected"); ;
                                continue;
                            }
                            else //something wrong here
                                throw new InvalidDataException($"Error in signal script data, matching element not found in line {tokenizer.LineNumber}.");
                        case TokenType.Value:
                            if (parserState == ScriptParserState.ScriptName)        //script names may include any value token or operator, only ended by line end
                            {
                                (currentBlock as Script).ScriptName += token.Value;
                                continue;
                            }

                            switch (token.Value)
                            {
                                case "REM":
                                    yield return currentBlock as Script;
                                    inScript = false;
                                    parserState = ScriptParserState.Remark;
                                    continue;
                                case "SCRIPT":
                                    if (!(currentBlock is Script))
                                        throw new InvalidDataException($"Error in signal script, matching element not found before new script in line {tokenizer.LineNumber}.");
                                    yield return currentBlock as Script;
                                    if (parserState == ScriptParserState.Remark)
                                        parserState = ScriptParserState.None;
                                    else
                                    {
                                        currentBlock = new Script(tokenizer.LineNumber);
                                        parserState = ScriptParserState.ScriptName;
                                        inScript = true;
                                    }
                                    continue;
                                case "IF":
                                    currentBlock = currentBlock.StartCondition(tokenizer.LineNumber);
                                    continue;
                                case "ELSE":
                                    currentBlock = currentBlock.StartAlternate(tokenizer.LineNumber);
                                    continue;
                                case "AND":
                                case "OR":
                                case "NOT":
                                case "MOD":
                                case "DIV":
                                    currentBlock = currentBlock.Add(new OperatorToken(token.Value, tokenizer.LineNumber), tokenizer.LineNumber);
                                    continue;
                                default:
                                    currentBlock = currentBlock.Add(new ScriptToken() { Token = token.Value }, tokenizer.LineNumber);
                                    continue;
                            }
                        case TokenType.Separator:
                        case TokenType.Tab:
                        case TokenType.Comma:
                            continue;
                        case TokenType.Operator:
                            if (parserState == ScriptParserState.ScriptName)
                            {
                                (currentBlock as Script).ScriptName += token.Value;
                            }
                            else
                            {
                                currentBlock = currentBlock.Add(new OperatorToken(token.Value, tokenizer.LineNumber), tokenizer.LineNumber);
                            }
                            continue;
                        default:
                            throw new InvalidOperationException($"Unknown token type {token.Type} containing '{token.Value}' in line {tokenizer.LineNumber}");
                    }
                }
                else if (token.Type == TokenType.Value)
                {
                    switch (token.Value)
                    {
                        case "REM":
                            parserState = ScriptParserState.Remark;
                            continue;
                        case "SCRIPT":
                            if (parserState == ScriptParserState.Remark)
                                parserState = ScriptParserState.None;
                            else // start new script
                            {
                                currentBlock = new Script(tokenizer.LineNumber);
                                parserState = ScriptParserState.ScriptName;
                                inScript = true;
                            }
                            continue;
                    }
                }
            }
            yield return currentBlock as Script;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
