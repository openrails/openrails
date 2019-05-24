// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.
#if DEBUG
// prints details of the file as read from input
// #define DEBUG_PRINT_IN

// prints details of the file as processed
// #define DEBUG_PRINT_OUT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Orts.Formats.Msts.Signalling;

namespace Orts.Formats.Msts
{
    public class SignalScripts
    {
        #region SCRExternalFunctions
        public enum SCRExternalFunctions
        {
            NONE,
            BLOCK_STATE,
            ROUTE_SET,
            NEXT_SIG_LR,
            NEXT_SIG_MR,
            THIS_SIG_LR,
            THIS_SIG_MR,
            OPP_SIG_LR,
            OPP_SIG_MR,
            NEXT_NSIG_LR,
            DIST_MULTI_SIG_MR,
            NEXT_SIG_ID,
            NEXT_NSIG_ID,
            OPP_SIG_ID,
            ID_SIG_ENABLED,
            ID_SIG_LR,
            SIG_FEATURE,
            DEF_DRAW_STATE,
            ALLOW_CLEAR_TO_PARTIAL_ROUTE,
            APPROACH_CONTROL_POSITION,
            APPROACH_CONTROL_POSITION_FORCED,
            APPROACH_CONTROL_SPEED,
            APPROACH_CONTROL_LOCK_CLAIM,
            APPROACH_CONTROL_NEXT_STOP,
            ACTIVATE_TIMING_TRIGGER,
            CHECK_TIMING_TRIGGER,
            TRAINHASCALLON,
            TRAINHASCALLON_RESTRICTED,
            TRAIN_REQUIRES_NEXT_SIGNAL,
            FIND_REQ_NORMAL_SIGNAL,
            ROUTE_CLEARED_TO_SIGNAL,
            ROUTE_CLEARED_TO_SIGNAL_CALLON,
            HASHEAD,
            INCREASE_SIGNALNUMCLEARAHEAD,
            DECREASE_SIGNALNUMCLEARAHEAD,
            SET_SIGNALNUMCLEARAHEAD,
            RESET_SIGNALNUMCLEARAHEAD,
            STORE_LVAR,
            THIS_SIG_LVAR,
            NEXT_SIG_LVAR,
            ID_SIG_LVAR,
            THIS_SIG_NOUPDATE,
            THIS_SIG_HASNORMALSUBTYPE,
            NEXT_SIG_HASNORMALSUBTYPE,
            ID_SIG_HASNORMALSUBTYPE,
            DEBUG_HEADER,
            DEBUG_OUT,
            RETURN,
        }
        #endregion

        #region SCRExternalFloats
        public enum SCRExternalFloats
        {
            STATE,
            DRAW_STATE,
            ENABLED,                         // read only
            BLOCK_STATE,                     // read only
            APPROACH_CONTROL_REQ_POSITION,   // read only
            APPROACH_CONTROL_REQ_SPEED,      // read only
        }
        #endregion

        #region SCRTermCondition
        public enum SCRTermCondition
        {
            GT,
            GE,
            LT,
            LE,
            EQ,
            NE,
            NONE,
        }
        #endregion

        #region SCRAndOr
        public enum SCRAndOr
        {
            AND,
            OR,
            NONE,
        }
        #endregion

        #region SCRNegate
        public enum SCRNegate
        {
            NEGATE,
        }
        #endregion

        #region SCRTermOperator
        public enum SCRTermOperator
        {
            NONE,        // used for first term
            MINUS,       // needs to come first to avoid it being interpreted as range separator
            MULTIPLY,
            PLUS,
            DIVIDE,
            MODULO,
        }
        #endregion

        #region SCRTermType
        public enum SCRTermType
        {
            ExternalFloat,
            LocalFloat,
            Sigasp,
            Sigfn,
            ORNormalSubtype,
            Sigfeat,
            Block,
            Constant,
            Invalid,
        }
        #endregion

        private static readonly IDictionary<string, SCRTermCondition> TranslateConditions = new Dictionary<string, SCRTermCondition>
            {
                { ">", SCRTermCondition.GT },
                { ">#", SCRTermCondition.GT },
                { ">=", SCRTermCondition.GE },
                { ">=#", SCRTermCondition.GE },
                { "<", SCRTermCondition.LT },
                { "<#", SCRTermCondition.LT },
                { "<=", SCRTermCondition.LE },
                { "<=#", SCRTermCondition.LE },
                { "==", SCRTermCondition.EQ },
                { "==#", SCRTermCondition.EQ },
                { "!=", SCRTermCondition.NE },
                { "!=#", SCRTermCondition.NE },
            };

        private static readonly IDictionary<string, SCRTermOperator> TranslateOperator = new Dictionary<string, SCRTermOperator>
            {
                { "?", SCRTermOperator.NONE },
                { "-", SCRTermOperator.MINUS },  // needs to come first to avoid it being interpreted as range separator
                { "*", SCRTermOperator.MULTIPLY },
                { "+", SCRTermOperator.PLUS },
                { "/", SCRTermOperator.DIVIDE },
                { "%", SCRTermOperator.MODULO }
            };

        private static readonly IDictionary<string, SCRAndOr> TranslateAndOr = new Dictionary<string, SCRAndOr>
            {
                { "&&", SCRAndOr.AND },
                { "||", SCRAndOr.OR },
                { "AND", SCRAndOr.AND },
                { "OR", SCRAndOr.OR },
                { "??", SCRAndOr.NONE }
            };

#if DEBUG_PRINT_IN
        public static string din_fileLoc = @"C:\temp\";     /* file path for debug files */
#endif

#if DEBUG_PRINT_OUT
        public static string dout_fileLoc = @"C:\temp\";    /* file path for debug files */
#endif

        public IDictionary<SignalType, SCRScripts> Scripts { get; private set; }

        //================================================================================================//
        //
        // Constructor
        //
        //================================================================================================//
        public SignalScripts(string routePath, IList<string> scriptFiles, IDictionary<string, SignalType> signalTypes, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
        {
            Scripts = new Dictionary<SignalType, SCRScripts>();

#if DEBUG_PRINT_PROCESS
            TDB_debug_ref = new int[5] { 7305, 7307, 7308, 7309, 7310 };   /* signal tdb ref.no selected for print-out */
#endif
#if DEBUG_PRINT_IN
            File.Delete(din_fileLoc + @"sigscr.txt");
#endif        
#if DEBUG_PRINT_OUT            
            File.Delete(dout_fileLoc + @"scriptproc.txt");
#endif
#if DEBUG_PRINT_ENABLED
            File.Delete(dpe_fileLoc + @"printproc.txt");
#endif
#if DEBUG_PRINT_PROCESS
            File.Delete(dpr_fileLoc + @"printproc.txt");
#endif

            // Process all files listed in SIGCFG
            foreach (string fileName in scriptFiles)
            {
                string fullName = Path.Combine(routePath, fileName);
                try
                {
                    using (StreamReader stream = new StreamReader(fullName, true))
                    {
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt", "Reading file : " + fullName + "\n\n");
#endif
                        Parser parser = new Parser(stream);
                        foreach (Script script in parser)
                        {
                            #region DEBUG
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", "\n===============================\n");
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", "\nNew Script : " + script.ScriptName + "\n");
#endif
#if DEBUG_PRINT_OUT
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n===============================\n");
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nNew Script : " + script.ScriptName + "\n");
#endif
                            #endregion
                            AssignScriptToSignalType(new SCRScripts(script, orSignalTypes, orNormalSubtypes),
                                signalTypes, parser.LineNumber, fileName);

                            Trace.Write("s");
                        }
                        #region DEBUG
#if DEBUG_PRINT_OUT
                        // print processed details 
                        foreach (KeyValuePair<SignalType, SCRScripts> item in Scripts)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Script : " + item.Value.ScriptName + "\n\n");
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", PrintScript(item.Value.Statements));
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n=====================\n");
                        }
#endif
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Error reading signal script - {fullName} : {ex.ToString()}");
                }
            }
        }// Constructor

        //================================================================================================//
        //
        // overall script file routines
        //
        //================================================================================================//
        #region DEBUG_PRINT_OUT
#if DEBUG_PRINT_OUT
        //================================================================================================//
        //
        // print processed script - for DEBUG purposes only
        //
        //================================================================================================//

        private string PrintScript(ArrayList statements)
        {
            bool function = false;
            List<int> Sublevels = new List<int>();
            StringBuilder builder = new StringBuilder();

            foreach (object statement in statements)
            {

                // process statement lines

                if (statement is SCRScripts.SCRStatement scrStatement)
                {
                    builder.Append("Statement : \n");
                    builder.Append(scrStatement.AssignType.ToString() + "[" + scrStatement.AssignParameter.ToString() + "] = ");

                    foreach (SCRScripts.SCRStatTerm scrTerm in scrStatement.StatementTerms)
                    {
                        if (scrTerm.TermLevel > 0)
                        {
                            builder.Append(" <SUB" + scrTerm.TermLevel.ToString() + "> ");
                        }
                        function = false;
                        if (scrTerm.Function != SCRExternalFunctions.NONE)
                        {
                            builder.Append(scrTerm.Function.ToString() + "(");
                            function = true;
                        }

                        if (scrTerm.PartParameter != null)
                        {
                            foreach (SCRScripts.SCRParameterType scrParam in scrTerm.PartParameter)
                            {
                                builder.Append(scrParam.PartType + "[" + scrParam.PartParameter + "] ,");
                            }
                        }

                        if (scrTerm.TermNumber != 0)
                        {
                            builder.Append(" SUBTERM_" + scrTerm.TermNumber.ToString());
                        }

                        if (function)
                        {
                            builder.Append(")");
                        }
                        builder.Append(" -" + scrTerm.TermOperator.ToString() + "- \n");
                    }

                    builder.Append("\n\n");
                }

                // process conditions line

                if (statement is SCRScripts.SCRConditionBlock scrCondBlock)
                {
                    builder.Append("\nCondition : \n");

                    builder.Append(PrintConditionArray(scrCondBlock.Conditions));

                    builder.Append("\nIF Block : \n");
                    builder.Append(PrintScript(scrCondBlock.IfBlock.Statements));

                    if (scrCondBlock.ElseIfBlock != null)
                    {
                        foreach (SCRScripts.SCRBlock tempBlock in scrCondBlock.ElseIfBlock)
                        {
                            builder.Append("\nStatements in ELSEIF : " + tempBlock.Statements.Count + "\n");
                            builder.Append("Elseif Block : \n");
                            builder.Append(PrintScript(tempBlock.Statements));
                        }
                    }
                    if (scrCondBlock.ElseBlock != null)
                    {
                        builder.Append("\nElse Block : \n");
                        builder.Append(PrintScript(scrCondBlock.ElseBlock.Statements));
                    }
                    builder.Append("\nEnd IF Block : \n");
                }
            }
            return builder.ToString();
        }// printscript

        //================================================================================================//
        //
        // print condition info - for DEBUG purposes only
        //
        //================================================================================================//

        private string PrintConditionArray(ArrayList Conditions)
        {
            StringBuilder builder = new StringBuilder();

            foreach (object condition in Conditions)
            {
                if (condition is SCRScripts.SCRConditions)
                {
                    builder.Append(PrintCondition((SCRScripts.SCRConditions)condition));
                }
                else if (condition is SCRAndOr andor)
                {
                    builder.Append(andor.ToString() + "\n");
                }
                else if (condition is SCRNegate)
                {
                    builder.Append("NEGATED : \n");
                }
                else
                {
                    builder.Append(PrintConditionArray((ArrayList)condition));
                }
            }
            return builder.ToString();
        }// printConditionArray

        //================================================================================================//
        //
        // print condition statement - for DEBUG purposes only
        //
        //================================================================================================//

        private string PrintCondition(SCRScripts.SCRConditions condition)
        {
            StringBuilder builder = new StringBuilder();

            bool function = false;
            if (condition.Term1.Negated)
            {
                builder.Append("NOT : ");
            }
            if (condition.Term1.Function != SCRExternalFunctions.NONE)
            {
                builder.Append(condition.Term1.Function.ToString() + "(");
                function = true;
            }

            if (condition.Term1.PartParameter != null)
            {
                foreach (SCRScripts.SCRParameterType scrParam in condition.Term1.PartParameter)
                {
                    builder.Append(scrParam.PartType + "[" + scrParam.PartParameter + "] ,");
                }
            }
            else
            {
                builder.Append(" 0 , ");
            }

            if (function)
            {
                builder.Append(")");
            }

            builder.Append(" -- " + condition.Condition.ToString() + " --\n");

            if (condition.Term2 != null)
            {
                function = false;
                if (condition.Term2.Negated)
                {
                    builder.Append("NOT : ");
                }
                if (condition.Term2.Function != SCRExternalFunctions.NONE)
                {
                    builder.Append(condition.Term2.Function.ToString() + "(");
                    function = true;
                }

                if (condition.Term2.PartParameter != null)
                {
                    foreach (SCRScripts.SCRParameterType scrParam in condition.Term2.PartParameter)
                    {
                        builder.Append(scrParam.PartType + "[" + scrParam.PartParameter + "] ,");
                    }
                }
                else
                {
                    builder.Append(" 0 , ");
                }

                if (function)
                {
                    builder.Append(")");
                }
                builder.Append("\n");
            }
            return builder.ToString();
        }// printcondition
#endif
        #endregion

        /// <summary>
        /// Links the script to the required signal type
        /// </summary>
        private void AssignScriptToSignalType(SCRScripts script, IDictionary<string, SignalType> signalTypes, int currentLine, string fileName)
        {
#pragma warning disable 219     //variable only used for DEBUG output using DEBUG_PRINT_OUT or DEBUG_PRINT_IN
            bool isValid = false;
#pragma warning restore 210
            string scriptName = script.ScriptName;
            // try and find signal type with same name as script
            if (signalTypes.TryGetValue(script.ScriptName.ToLower(), out SignalType signalType))
            {
                if (Scripts.ContainsKey(signalType))
                {
                    Trace.TraceWarning($"Ignored duplicate SignalType script {scriptName} in {0} {fileName} before {currentLine}");
                }
                else
                {
                    #region DEBUG
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "Adding script : " + signalType.Name + "\n");
#endif
                    #endregion
                    Scripts.Add(signalType, script);
                    isValid = true;
                }
            }

            // try and find any other signal types which reference this script
            foreach (KeyValuePair<string, SignalType> currentSignal in signalTypes)
            {
                if (scriptName.Equals(currentSignal.Value.Script, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Scripts.ContainsKey(currentSignal.Value))
                    {
                        Trace.TraceWarning($"Ignored duplicate SignalType script {scriptName} in {fileName} before {currentLine}");
                    }
                    else
                    {
                        #region DEBUG
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt", "Adding script : " + currentSignal.Value.Script + " to " + currentSignal.Value.Name + "\n");
#endif
                        #endregion
                        Scripts.Add(currentSignal.Value, script);
                        isValid = true;
                    }
                }
            }
            #region DEBUG
#if DEBUG_PRINT_OUT
            if (!isValid)
            {
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", $"\nUnknown signal type : {scriptName}\n\n");
            }
#endif
#if DEBUG_PRINT_IN
            if (!isValid)
            {
                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"\nUnknown signal type : {scriptName}\n\n");
            }
#endif
            #endregion
        }

        public class SCRScripts
        {
            private IDictionary<string, int> localFloats;

            public int TotalLocalFloats { get { return localFloats.Count; } }

            public ArrayList Statements { get; private set; }
            //public List<Statements> { get; private set; }

            public string ScriptName { get; private set; }

            internal SCRScripts(Script script, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
            {
                localFloats = new Dictionary<string, int>();
                Statements = new ArrayList();
                ScriptName = script.ScriptName;
                int statementLine = 0;
                int maxCount = script.Tokens.Count;
                #region DEBUG_PRINT_IN
#if DEBUG_PRINT_IN
                // print inputlines
                File.AppendAllText(din_fileLoc + @"sigscr.txt", script.ToString() + '\n');
                File.AppendAllText(din_fileLoc + @"sigscr.txt", "\n+++++++++++++++++++++++++++++++++++\n\n");

#endif
                #endregion
                // Skip external floats (exist automatically)
                while ((script.Tokens[statementLine] as Statement)?.Tokens[0].Token == "EXTERN" && (script.Tokens[statementLine] as Statement)?.Tokens[1].Token == "FLOAT" && statementLine++ < maxCount) ;

                //// Process floats : build list with internal floats
                while (((script.Tokens[statementLine] as Statement)?.Tokens[0].Token == "FLOAT") && statementLine < maxCount)
                {
                    string floatString = (script.Tokens[statementLine] as Statement)?.Tokens[1].Token;
                    if (!localFloats.ContainsKey(floatString))
                    {
                        localFloats.Add(floatString, localFloats.Count);
                    }
                    statementLine++;
                }

                #region DEBUG_PRINT_OUT
#if DEBUG_PRINT_OUT
                // print details of internal floats

                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n\nFloats : \n");
                foreach (KeyValuePair<string, int> item in localFloats)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", $"Float : {item.Key} = {item.Value}\n");
                }
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Total : " + localFloats.Count.ToString() + "\n\n\n");
#endif
                #endregion
                script.Tokens.RemoveRange(0, statementLine);

                foreach (BlockBase statementBlock in script.Tokens)
                {
                    if (statementBlock is ConditionalBlock)
                    {
                        SCRConditionBlock condition = new SCRConditionBlock(statementBlock as ConditionalBlock, localFloats, orSignalTypes, orNormalSubtypes);
                        Statements.Add(condition);
                    }
                    else
                    {
                        SCRStatement scrStatement = new SCRStatement(statementBlock, localFloats, orSignalTypes, orNormalSubtypes);
                        Statements.Add(scrStatement);
                    }
                }
            }// constructor

            internal static SCRParameterType ParameterFromToken(ScriptToken token, int lineNumber, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
            {

                int index;

                // try constant
                if (int.TryParse(token.Token, out int constantInt))
                {
                    return new SCRParameterType(SCRTermType.Constant, constantInt);
                }
                // try external float
                else if (Enum.TryParse(token.Token, true, out SCRExternalFloats externalFloat))
                {
                    return new SCRParameterType(SCRTermType.ExternalFloat, (int)externalFloat);
                }
                // try local float
                else if (localFloats.TryGetValue(token.Token, out int localFloat))
                {
                    return new SCRParameterType(SCRTermType.LocalFloat, localFloat);
                }
                string[] definitions = token.Token.Split(new char[] { '_' }, 2);
                if (definitions.Length == 2)
                    switch (definitions[0])
                    {
                        // try blockstate
                        case "BLOCK":
                            if (Enum.TryParse(definitions[1], out MstsBlockState blockstate))
                            {
                                return new SCRParameterType(SCRTermType.Block, (int)blockstate);
                            }
                            else
                            {
                                Trace.TraceWarning($"sigscr-file line {lineNumber.ToString()} : Unknown Blockstate : {definitions[1]} \n");
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown Blockstate : {token.Token} \n");
#endif
                            }
                            break;
                        // try SIGASP definition
                        case "SIGASP":
                            if (Enum.TryParse(definitions[1], out MstsSignalAspect aspect))
                            {
                                return new SCRParameterType(SCRTermType.Sigasp, (int)aspect);
                            }
                            else
                            {
                                Trace.TraceWarning($"sigscr-file line {lineNumber.ToString()} : Unknown Aspect : {definitions[1]} \n");
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown Aspect : {token.Token} \n");
#endif
                            }
                            break;
                        // try SIGFN definition
                        case "SIGFN":
                            index = orSignalTypes.IndexOf(definitions[1]);
                            if (index != -1)
                            {
                                return new SCRParameterType(SCRTermType.Sigfn, index);
                            }
                            else
                            {
                                Trace.TraceWarning($"sigscr-file line {lineNumber.ToString()} : Unknown SIGFN Type : {definitions[1]} \n");
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown Type : {token.Token} \n");
#endif
                            }
                            break;
                        // try ORSubtype definition
                        case "ORSUBTYPE":
                            index = orNormalSubtypes.IndexOf(definitions[1]);
                            if (index != -1)
                            {
                                return new SCRParameterType(SCRTermType.ORNormalSubtype, index);
                            }
                            else
                            {
                                Trace.TraceWarning($"sigscr-file line {lineNumber} : Unknown ORSUBTYPE : {definitions[1]} \n");
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown Type : {token.Token} \n");
#endif
                            }
                            break;
                        // try SIGFEAT definition
                        case "SIGFEAT":
                            index = SignalShape.SignalSubObj.SignalSubTypes.IndexOf(definitions[1]);
                            if (index != -1)
                            {
                                return new SCRParameterType(SCRTermType.Sigfeat, index);
                            }
                            else
                            {
                                Trace.TraceWarning($"sigscr-file line {lineNumber} : Unknown SubType : {definitions[1]} \n");
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown SubType : {token.Token} \n");
#endif
                            }
                            break;
                        default:
                            // nothing found - set error
                            Trace.TraceWarning($"sigscr-file line {lineNumber} : Unknown parameter in statement : {token.Token}");
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown parameter : {token.Token} \n");
#endif
                            break;
                    }
                return new SCRParameterType(SCRTermType.Constant, 0);
            }//process_TermPart

            //================================================================================================//
            //
            // process IF condition line - split into logic parts
            //
            //================================================================================================//
            internal static ArrayList ParseConditions(Enclosure condition, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
            {
                ArrayList result = new ArrayList();
                SCRAndOr logicalOperator = SCRAndOr.NONE;
                while (condition.Tokens.Count > 0)
                {
                    if ((condition.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Logical)
                    {
                        if (TranslateAndOr.TryGetValue(condition.Tokens[0].Token, out logicalOperator))
                        {
                            result.Add(logicalOperator);
                        }
                        else
                        {
                            Trace.TraceWarning($"sigscr-file line {condition.LineNumber} : Invalid logical operator in : {condition.Token[0]}");
                        }
                        condition.Tokens.RemoveAt(0);
                    }
                    else if ((condition.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Negator && (condition.Tokens[1] is Enclosure || condition.Tokens[1] is ScriptToken))
                    {
                        result.Add(SCRNegate.NEGATE);
                        condition.Tokens.RemoveAt(0);
                    }
                    //Conditions are dedicated blocks, but always separated by logical operators
                    else if (condition.Tokens[0] is Enclosure) //process sub block
                    {
                        result.AddRange(ParseConditions((condition.Tokens[0] as Enclosure), localFloats, orSignalTypes, orNormalSubtypes));
                        //recurse in the block
                        condition.Tokens.RemoveAt(0);
                    }
                    else //single term
                    {
                        result.Add(new SCRConditions(condition, localFloats, orSignalTypes, orNormalSubtypes));
                    }
                }
                // TODO: This may be removed, only for debug output compatibility
                if (logicalOperator != SCRAndOr.NONE)
                    result.Add(logicalOperator);
                return result;
            }

            //================================================================================================//
            //
            // sub classes
            //
            //================================================================================================//
            //
            // class SCRStatement
            //
            //================================================================================================//

            public class SCRStatement
            {
                public List<SCRStatTerm> StatementTerms { get; private set; } = new List<SCRStatTerm>();

                public SCRTermType AssignType { get; private set; }

                public int AssignParameter { get; private set; }

                internal SCRStatement(BlockBase statementBlock, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    AssignType = SCRTermType.Invalid;

                    //TODO: may want to process other Assignment Operations as well (+=, -= etc)
                    Statement statement = statementBlock as Statement;
                    //there are some scripts misusing equality operators (==, ==#) for assignment (=, #=), so we are warning them and trying to correct adhoc
                    if (statement?.Tokens.Count > 1 && (statement?.Tokens[1] as OperatorToken)?.OperatorType == OperatorType.Equality && statement?.Tokens[1].Token[0] == '=')
                    {
                        Trace.TraceWarning($"Invalid equality operation {statement?.Tokens[1].Token} in line {statementBlock.LineNumber} - processing as {new OperatorToken(statement?.Tokens[1].Token.Substring(1).Replace("==", "=").Replace("=#", "#="), statement.LineNumber)} assignment operation.");
                        statement.Tokens[1] = new OperatorToken(statement?.Tokens[1].Token.Substring(1).Replace("==", "=").Replace("=#", "#="), statement.LineNumber);
                    }
                    if (statement?.Tokens.Count > 1 && (statement?.Tokens[1] as OperatorToken)?.OperatorType == OperatorType.Assignment)
                    {
                        if (Enum.TryParse(statement.Tokens[0].Token, out SCRExternalFloats result))
                        {
                            AssignParameter = (int)result;
                            AssignType = SCRTermType.ExternalFloat;
                        }
                        else if (localFloats.TryGetValue(statement.Tokens[0].Token, out int value))
                        {
                            AssignParameter = value;
                            AssignType = SCRTermType.LocalFloat;
                        }
                        else
                        {
                            Trace.TraceWarning($"Invalid Assignment target in line {statementBlock.LineNumber} - could not find {statement.Tokens[0].Token} as local or external float");
                        }
                        // Assignment term
                        statement.Tokens.RemoveRange(0, 2);
                    }
                    ProcessScriptStatement(statementBlock, 0, localFloats, orSignalTypes, orNormalSubtypes);
                }

                private void ProcessScriptStatement(BlockBase statementBlock, int level, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    string operatorString = string.Empty;
                    int termNumber = level;
                    bool negated = false;

                    while (statementBlock.Tokens.Count > 0)
                    {
                        negated = false;
                        if ((statementBlock.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Operation)
                        {
                            operatorString = statementBlock.Tokens[0].Token;
                            statementBlock.Tokens.RemoveAt(0);
                            if (statementBlock.Tokens.Count == 0)
                            {
                                Trace.TraceWarning($"sigscr-file line {statementBlock.LineNumber} : Invalid statement syntax : {statementBlock.ToString()}");
                                StatementTerms.Clear();
                                return;
                            }
                        }
                        else
                            operatorString = string.Empty;

                        if (statementBlock.Tokens[0] is Enclosure)
                        {
                            termNumber++;
                            //recurse through inner statemement
                            SCRStatTerm term = new SCRStatTerm(termNumber, level, operatorString);
                            StatementTerms.Add(term);

                            ProcessScriptStatement(statementBlock.Tokens[0] as Enclosure, termNumber, localFloats, orSignalTypes, orNormalSubtypes);
                        }
                        else
                        {
                            if ((statementBlock.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Negator)
                            {
                                statementBlock.Tokens.RemoveAt(0);
                                negated = true;
                            }
                            if (statementBlock.Tokens.Count > 1 && Enum.TryParse(statementBlock.Tokens[0].Token, out SCRExternalFunctions externalFunctionsResult) && statementBlock.Tokens[1] is Enclosure)   //check if it is a Sub Function ()
                            {
                                StatementTerms.Add(
                                    new SCRStatTerm(externalFunctionsResult, statementBlock.Tokens[1] as Enclosure, termNumber, operatorString, negated, localFloats, orSignalTypes, orNormalSubtypes));
                                statementBlock.Tokens.RemoveAt(0);
                            }
                            else
                            {
                                StatementTerms.Add(
                                    new SCRStatTerm(statementBlock.Tokens[0], termNumber, operatorString, statementBlock.LineNumber, negated, localFloats, orSignalTypes, orNormalSubtypes));
                            }

                        }
                        statementBlock.Tokens.RemoveAt(0);
                    }
                }
            }

            //================================================================================================//
            //
            // class SCRStatTerm
            //
            //================================================================================================//

            public class SCRStatTerm
            {
                public SCRExternalFunctions Function { get; private set; }

                public SCRParameterType[] PartParameter { get; private set; }

                public SCRTermOperator TermOperator { get; private set; }

                public bool Negated { get; private set; }

                public int TermNumber { get; private set; }

                public int TermLevel { get; private set; }

                // SubLevel term
                internal SCRStatTerm(int termNumber, int level, string operatorTerm)
                {
                    // sublevel definition
                    this.TermNumber = termNumber;
                    this.TermLevel = level;

                    TermOperator = TranslateOperator.TryGetValue(operatorTerm, out SCRTermOperator termOperator) ? termOperator : SCRTermOperator.NONE;
                } // constructor

                // Function term
                internal SCRStatTerm(SCRExternalFunctions externalFunction, BlockBase block, int subLevel, string operatorTerm, bool negated, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    Negated = negated;
                    TermLevel = subLevel;
                    Function = externalFunction;
                    TermOperator = TranslateOperator.TryGetValue(operatorTerm, out SCRTermOperator tempOperator) ? tempOperator : SCRTermOperator.NONE;

                    List<SCRParameterType> result = new List<SCRParameterType>();

                    while (block.Tokens.Count > 0)
                    {
                        if (block.Tokens.Count > 1 && Enum.TryParse(block.Tokens[0].Token, out SCRExternalFunctions externalFunctionsResult) && block.Tokens[1] is Enclosure)   //check if it is a Function ()
                        {
                            // TODO Nested Function Call in Parameter not supported
                            throw new NotImplementedException($"Nested function call in parameter {block.Token} not supported at line {block.LineNumber}");
                            //SCRParameterType parameter = ParameterFromToken(statement.Tokens[0], lineNumber, localFloats, orSignalTypes, orNormalSubtypes);
                            //StatementTerms.Add(
                            //    new SCRStatTerm(externalFunctionsResult, statement.Tokens[1] as ScriptBlockBase, termNumber, operatorString, statement.LineNumber, localFloats, orSignalTypes, orNormalSubtypes));
                            //statement.Tokens.RemoveAt(0);
                        }
                        else
                        {
                            //substitute a trailing + or - operator token to become part of the (numeric) parameter 
                            if (block.Tokens.Count > 1 && ((block.Tokens[0] as OperatorToken)?.Token == "-" || (block.Tokens[0] as OperatorToken)?.Token == "+"))
                            {
                                block.Tokens[1].Token = block.Tokens[0].Token + block.Tokens[1].Token;
                                block.Tokens.RemoveAt(0);
                            }
                            SCRParameterType parameter = ParameterFromToken(block.Tokens[0], block.LineNumber, localFloats, orSignalTypes, orNormalSubtypes);
                            result.Add(parameter);
                        }
                        block.Tokens.RemoveAt(0);
                    }
                    PartParameter = result.Count > 0 ? result.ToArray() : null;
                }

                internal SCRStatTerm(ScriptToken token, int subLevel, string operatorTerm, int lineNumber, bool negated, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    TermLevel = subLevel;
                    Negated = negated;

                    if (token.Token == "RETURN")
                    {
                        Function = SCRExternalFunctions.RETURN;
                    }
                    else
                    {
                        Function = SCRExternalFunctions.NONE;
                        PartParameter = new SCRParameterType[1];
                        PartParameter[0] = ParameterFromToken(token, lineNumber, localFloats, orSignalTypes, orNormalSubtypes);
                        TermOperator = TranslateOperator.TryGetValue(operatorTerm, out SCRTermOperator tempOperator) ? tempOperator : SCRTermOperator.NONE;
                    }
                } // constructor
            } // class SCRStatTerm

            //================================================================================================//
            //
            // class SCRParameterType
            //
            //================================================================================================//        
            public class SCRParameterType
            {
                public SCRTermType PartType { get; private set; }

                public int PartParameter { get; private set; }

                public SCRParameterType(SCRTermType type, int value)
                {
                    PartType = type;
                    PartParameter = value;
                }
            }

            //================================================================================================//
            //
            // class SCRConditionBlock
            //
            //================================================================================================//
            public class SCRConditionBlock
            {
                public ArrayList Conditions { get; private set; }

                public SCRBlock IfBlock { get; private set; }

                public List<SCRBlock> ElseIfBlock { get; private set; }

                public SCRBlock ElseBlock { get; private set; }

                internal SCRConditionBlock(ConditionalBlock conditionalBlock, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    //IF-Term
                    Conditions = ParseConditions(conditionalBlock.Tokens[0] as Enclosure, localFloats, orSignalTypes, orNormalSubtypes);
                    IfBlock = new SCRBlock(conditionalBlock.Tokens[1] as BlockBase, localFloats, orSignalTypes, orNormalSubtypes);
                    conditionalBlock.Tokens.RemoveRange(0, 2);

                    //ElseIf-Term
                    while ((conditionalBlock.Tokens.FirstOrDefault() as ConditionalBlock)?.IsAlternateCondition ?? false)
                    {
                        if (ElseIfBlock == null)
                            ElseIfBlock = new List<SCRBlock>();
                        ElseIfBlock.Add(new SCRBlock(conditionalBlock.Tokens[0] as ConditionalBlock, localFloats, orSignalTypes, orNormalSubtypes));
                        conditionalBlock.Tokens.RemoveAt(0);
                    }

                    // Else-Block
                    if (conditionalBlock.Tokens.Count > 0 && conditionalBlock.HasAlternate)
                    {
                        ElseBlock = new SCRBlock(conditionalBlock.Tokens[0] as BlockBase, localFloats, orSignalTypes, orNormalSubtypes);
                        conditionalBlock.Tokens.RemoveAt(0);
                    }
                }
            } // class SCRConditionBlock

            //================================================================================================//
            //
            // class SCRConditions
            //
            //================================================================================================//
            public class SCRConditions
            {
                public SCRStatTerm Term1 { get; private set; }

                public SCRStatTerm Term2 { get; private set; }

                public SCRTermCondition Condition { get; private set; }

                internal SCRConditions(Enclosure statement, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    bool negated = false;

                    if ((statement.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Negator)
                    {
                        statement.Tokens.RemoveAt(0);
                        negated = true;
                    }
                    //substitute a trailing + or - operator token to become part of the (numeric) term 
                    if (statement.Tokens.Count > 1 && ((statement.Tokens[0] as OperatorToken)?.Token == "-" || (statement.Tokens[0] as OperatorToken)?.Token == "+"))
                    {
                        statement.Tokens[1].Token = statement.Tokens[0].Token + statement.Tokens[1].Token;
                        statement.Tokens.RemoveAt(0);
                    }
                    if (statement.Tokens.Count > 1 && Enum.TryParse(statement.Tokens[0].Token, out SCRExternalFunctions externalFunctionsResult) && statement.Tokens[1] is Enclosure)   //check if it is a Sub Function ()
                    {
                        Term1 = new SCRStatTerm(externalFunctionsResult, statement.Tokens[1] as Enclosure, 0, string.Empty, negated, localFloats, orSignalTypes, orNormalSubtypes);
                        statement.Tokens.RemoveAt(0);
                    }
                    else
                    {
                        Term1 = new SCRStatTerm(statement.Tokens[0], 0, string.Empty, statement.LineNumber, negated, localFloats, orSignalTypes, orNormalSubtypes);
                    }
                    statement.Tokens.RemoveAt(0);

                    if (statement.Tokens.Count > 0)
                    {
                        if ((statement.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Logical)
                        {
                            // if this is a unary (boolean)comparison
                            return;
                        }
                        //Comparison Operator
                        else if (TranslateConditions.TryGetValue(statement.Tokens[0].Token, out SCRTermCondition comparison))
                        {
                            Condition = comparison;
                        }
                        else
                        {
                            Trace.TraceWarning($"sigscr-file line {statement.LineNumber} : Invalid comparison operator in : {statement}");
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Invalid comparison operator in : {statement}\n"); ;
#endif
                        }
                        statement.Tokens.RemoveAt(0);

                        if (statement.Tokens.Count > 0)
                        {
                            //Term 2
                            if ((statement.Tokens[0] as OperatorToken)?.OperatorType == OperatorType.Negator)
                            {
                                statement.Tokens.RemoveAt(0);
                                negated = true;
                            }
                            //substitute a trailing + or - operator token to become part of the (numeric) term 
                            if (statement.Tokens.Count > 1 && ((statement.Tokens[0] as OperatorToken)?.Token == "-" || (statement.Tokens[0] as OperatorToken)?.Token == "+"))
                            {
                                statement.Tokens[1].Token = statement.Tokens[0].Token + statement.Tokens[1].Token;
                                statement.Tokens.RemoveAt(0);
                            }
                            if (statement.Tokens.Count > 1 && Enum.TryParse(statement.Tokens[0].Token, out SCRExternalFunctions externalFunctionsResult2) && statement.Tokens[1] is Enclosure)   //check if it is a Sub Function ()
                            {
                                Term2 = new SCRStatTerm(externalFunctionsResult2, statement.Tokens[1] as Enclosure, 0, string.Empty, negated, localFloats, orSignalTypes, orNormalSubtypes);
                                statement.Tokens.RemoveAt(0);
                            }
                            else
                            {
                                Term2 = new SCRStatTerm(statement.Tokens[0], 0, string.Empty, statement.LineNumber, negated, localFloats, orSignalTypes, orNormalSubtypes);
                            }
                            statement.Tokens.RemoveAt(0);
                        }
                        else
                        {
                            Trace.TraceWarning($"Invalid statement in line {statement.LineNumber}");
                        }
                    }
                }
            } // class SCRConditions

            //================================================================================================//
            //
            // class SCRBlock
            //
            //================================================================================================//
            public class SCRBlock
            {
                public ArrayList Statements { get; private set; }

                internal SCRBlock(BlockBase block, IDictionary<string, int> localFloats, IList<string> orSignalTypes, IList<string> orNormalSubtypes)
                {
                    List<ScriptToken> statements;

                    if (block is ConditionalBlock || block is Statement)
                        block = new Block(null, block.LineNumber) { Tokens = { block } };      //if this is a single If-Statement or Statement, encapsulate as block

                    while ((statements = block.Tokens)?.Count == 1 && statements[0] is Block)    //remove nested empty blocks, primarily for legacy compatiblity
                        block = statements[0] as Block;

                    Statements = new ArrayList();

                    foreach (BlockBase statementBlock in block.Tokens)
                    {
                        if (statementBlock is ConditionalBlock)
                        {
                            SCRConditionBlock condition = new SCRConditionBlock(statementBlock as ConditionalBlock, localFloats, orSignalTypes, orNormalSubtypes);
                            Statements.Add(condition);
                        }
                        else
                        {
                            SCRStatement scrStatement = new SCRStatement(statementBlock, localFloats, orSignalTypes, orNormalSubtypes);
                            Statements.Add(scrStatement);
                        }
                    }
                }
            } // class SCRBlock
        } // class Scripts
    }
}
