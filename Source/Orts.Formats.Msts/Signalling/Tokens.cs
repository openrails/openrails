using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Orts.Formats.Msts.Signalling
{
    #region Script Tokens
    internal class ScriptToken
    {
        protected static int indent = 0;

        public virtual string Token { get; set; }

        public override string ToString()
        {
            return Token;
        }
    }

    internal enum OperatorType
    {
        Negator,
        Logical,
        Equality,
        Assignment,
        Operation,
        Other,
    }

    internal class OperatorToken : ScriptToken
    {
        public OperatorToken(string token, int lineNumber)
        {
            Token = token;

            switch (token)
            {
                case "NOT":
                case "!":
                    OperatorType = OperatorType.Negator;
                    break;
                case "AND":
                case "OR":
                case "||":
                case "&&":
                case "EOR":
                case "^":
                    OperatorType = OperatorType.Logical;
                    break;
                case "=":
                case "#=":
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                case "/#=":
                case "%=":
                    OperatorType = OperatorType.Assignment;
                    break;
                case "-":
                case "*":
                case "+":
                case "/":
                case "/#":
                case "%":
                case "DIV":
                case "MOD":
                    OperatorType = OperatorType.Operation;
                    break;
                case ">":
                case ">#":
                case ">=":
                case ">=#":
                case "<":
                case "<#":
                case "<=":
                case "<=#":
                case "==":
                case "==#":
                case "!=":
                case "!=#":
                    OperatorType = OperatorType.Equality;
                    break;
                default:
                    OperatorType = OperatorType.Other;
                    Trace.TraceWarning($"sigscr-file : Invalid operator token {token} in line number {lineNumber}");
                    break;
            }
        }

        public OperatorType OperatorType { get; private set; }
    }

    internal abstract class BlockBase : ScriptToken
    {

        //protected static int blockID;

        //protected int BlockId = blockID++;

        protected bool isAlternateReturn;

        public BlockBase Parent { get; private set; }

        public int LineNumber { get; set; }

        public List<ScriptToken> Tokens { get; } = new List<ScriptToken>();

        public BlockBase Current { get; protected set; }

        public BlockBase(BlockBase parent, int lineNumber)
        {
            Parent = parent;
            Parent?.AddNestedBlock(this);
            LineNumber = lineNumber;
        }

        protected virtual void AddNestedBlock(BlockBase block)
        {
            Current = block;
            Tokens.Add(block);
        }

        protected BlockBase SetAsAlternateReturn()
        {
            if (!(this as ConditionalBlock)?.SkipOnReturn ?? true)
                isAlternateReturn = true;
            return this;
        }

        protected BlockBase FindAlternateReturn()
        {
            return isAlternateReturn ? this : Parent.FindAlternateReturn();
        }

        public override string Token { get { return ToString(); } }

        public override string ToString()
        {
            if (Tokens.Count == 0)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            indent++;

            foreach (ScriptToken block in Tokens)
            {
                builder.Append(block.ToString());
                if (builder[builder.Length - 1] != '\n')
                    builder.AppendLine();
            }
            if (builder.Length > 3)
                builder.Length -= 2;
            indent--;
            return builder.ToString();
        }

        public virtual BlockBase Add(ScriptToken token)
        {
            Current = Current ?? new Statement(this, LineNumber);
            //token should be Value or Operator only added to current statement or enclosing
            Current.Tokens.Add(token);
            return Current;
        }

        public virtual BlockBase StartBlock(int lineNumber)
        { return new Block(this, lineNumber); }

        public virtual BlockBase StartEnclosure(int lineNumber)
        { return new Enclosure(this, lineNumber); }

        public virtual BlockBase StartCondition(int lineNumber)
        { return new ConditionalBlock(this, lineNumber); }

        public virtual BlockBase StartAlternate(int lineNumber)
        {
            SetAsAlternateReturn();
            //find the deepest Condition block which does not yet have an alternate (no Else, or Else If only)
            return (((Tokens.Last() as ConditionalBlock)?.RequiresAlternate()?.StartAlternate(lineNumber) as ConditionalBlock) ?? this as ConditionalBlock).SetAsAlternateReturn();
        }

        public virtual BlockBase CompleteBlock()
        {
            if (Parent is ConditionalBlock)
            {
                return Parent.CompleteBlock();
            }
            else
            {
                Current = null;
                Parent.Current = null;
                return Parent;
            }
        }
    }

    internal class Script : BlockBase
    {
        public Script(int lineNumber) : base(null, lineNumber)
        { }

        public string ScriptName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Script {ScriptName}\r\n" + base.ToString();
        }
    }

    internal class Statement : BlockBase
    {
        public Statement(BlockBase parent, int lineNumber) : base(parent, lineNumber)
        { }

        public override BlockBase Add(ScriptToken token)
        {
            Tokens.Add(token);
            return this;
        }

        public override string ToString()
        {
            if (Tokens.Count == 0)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append(' ', indent);
            foreach (ScriptToken token in Tokens)
            {
                builder.Append(token.ToString());
                builder.Append(' ');
            }
            if (builder.Length > 1)
                builder[builder.Length - 1] = ';';
            return builder.ToString();
        }
    }

    internal class Block : BlockBase
    {
        public Block(BlockBase parent, int lineNumber) : base(parent, lineNumber)
        { }

        public override string ToString()
        {
            return $"{new string(' ', indent)}{{\r\n{base.ToString()}\r\n{new string(' ', indent)}}}\r\n";
        }
    }

    internal class Enclosure : BlockBase
    {
        public Enclosure(BlockBase parent, int lineNumber) : base(parent, lineNumber)
        { }

        public override BlockBase Add(ScriptToken token)
        {
            Tokens.Add(token);
            return this;
        }

        public override string ToString()
        {
            if (Tokens.Count == 0)
                return "()";
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            foreach (ScriptToken token in Tokens)
            {
                string statement = token.ToString();
                statement.TrimEnd('\r', '\n');
                builder.Append(statement);
                builder.Append(' ');
            }
            if (builder.Length > 1)
                builder.Length--;
            builder.Append(")");
            return builder.ToString();
        }
    }

    internal class ConditionalBlock : BlockBase
    {
        private bool hasCondition;
        private bool hasStatement;
        private bool hasAlternateCondition;

        public ConditionalBlock(BlockBase parent, int lineNumber) : base(parent, lineNumber)
        { }

        internal bool IsAlternateCondition { get; private set; }

        internal bool HasAlternate { get; private set; }

        internal bool SkipOnReturn;

        public override BlockBase Add(ScriptToken token)
        {
            return base.Add(token);
        }

        public override BlockBase CompleteBlock()
        {
            BlockBase result;
            if (!hasCondition && Current is Enclosure)
            {
                hasCondition = true;
                result = this;
            }
            else if (hasCondition && !hasStatement && (Current is Statement || Current is Block || Current is ConditionalBlock))
            {
                hasStatement = true;
                result = base.CompleteBlock();
            }
            else if (hasCondition && hasStatement && hasAlternateCondition)
            {
                hasAlternateCondition = false;
                result = FindAlternateReturn().Parent;
            }
            else if (hasCondition && hasStatement && !HasAlternate && (Current is Statement || Current is Block || Current is ConditionalBlock))
            {
                HasAlternate = true;
                result = FindAlternateReturn().Parent;
            }
            else
                result = null;
            Current = null;
            return result;
        }

        public override BlockBase StartCondition(int lineNumber)
        {
            ConditionalBlock result = base.StartCondition(lineNumber) as ConditionalBlock;
            if (isAlternateReturn) //this is the If after Else for ElseIf
            {
                hasAlternateCondition = true;   //mark myself to have ElseIf
                result.IsAlternateCondition = true; //mark as ElseIf Condition
            }   //else this is just a nested Condition If () If () ;
            return result;
        }

        public BlockBase RequiresAlternate()
        {
            // return this instance if there is no Else yet, or if this is the If part of an ElseIf, else just null for simpified handling
            if (HasAlternate || IsAlternateCondition)
            {
                ConditionalBlock child = Tokens.ElementAtOrDefault(1) as ConditionalBlock;
                //only true on isAlternateCondition
                if (!child?.HasAlternate ?? false)
                {
                    //since we are going deeper one more, we want to skip this one as return block reference next time
                    child.SkipOnReturn = true;
                }
                else
                    return null;
            }
            return this;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            //builder.Append($"{BlockId}");
            builder.Append(' ', indent);
            builder.Append(IsAlternateCondition ? "ELSE IF " : "IF ");
            builder.AppendLine(Tokens.First().ToString());

            int index = 0;
            while (index++ < Tokens.Count - 1)
            {
                if (HasAlternate && index == Tokens.Count - 1)  //the last block is the alternate
                    builder.AppendLine($"{new string(' ', indent)}ELSE");
                builder.Append(AdjustIndentAndPrint(Tokens[index] as BlockBase));
                if (builder[builder.Length - 1] != '\n')
                    builder.AppendLine();
            }
            builder.Length -= 2;//remove last CRLF
            //builder.Append($"?{BlockId}");
            return builder.ToString();

            string AdjustIndentAndPrint(BlockBase block)
            {
                string result;
                if (block is Statement || (block is ConditionalBlock && !(block as ConditionalBlock).IsAlternateCondition)) //Single statements or Nested Conditions (but not ElseIf)
                {
                    indent++;
                    result = block.ToString();
                    indent--;
                }
                else
                    result = block.ToString();

                return result;
            }
        }
    }
    #endregion

}
