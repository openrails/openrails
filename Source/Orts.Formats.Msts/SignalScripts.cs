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

// removes catch and allows program to crash on error statement
// #define DEBUG_ALLOWCRASH

// prints details of the file as read from input
// #define DEBUG_PRINT_IN

// prints details of the file as processed
// #define DEBUG_PRINT_OUT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Orts.Formats.Msts
{
    //================================================================================================//
    //
    // class scrReadinfo
    //
    //================================================================================================//

    public class scrReadInfo
    {
        public string Readline;
        public int Linenumber;
        public string Scriptname;

        //================================================================================================//
        ///
        /// Constructor
        ///

        public scrReadInfo(string thisString, int thisInt, string thisScript)
        {
            Readline = thisString;
            Linenumber = thisInt;
            Scriptname = thisScript;
        }
    }// class scrReadInfo

    public class SignalScripts
    {
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
            DIST_MULTI_SIG_MR_OF_LR,
            NEXT_SIG_ID,
            NEXT_NSIG_ID,
            OPP_SIG_ID,
            OPP_SIG_ID_TRAINPATH,
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
            TRAINHASCALLON_ADVANCED,
            TRAINHASCALLON_RESTRICTED_ADVANCED,
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
            SWITCHSTAND,
            DEBUG_HEADER,
            DEBUG_OUT,
            RETURN,
        }

        public enum SCRExternalFloats
        {
            STATE,
            DRAW_STATE,
            ENABLED,                         // read only
            BLOCK_STATE,                     // read only
            APPROACH_CONTROL_REQ_POSITION,   // read only
            APPROACH_CONTROL_REQ_SPEED,      // read only
        }

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

        public enum SCRAndOr
        {
            AND,
            OR,
            NONE,
        }

        public enum SCRNegate
        {
            NEGATE,
        }

        public enum SCRTermOperator
        {
            NONE,        // used for first term
            MINUS,       // needs to come first to avoid it being interpreted as range separator
            MULTIPLY,
            PLUS,
            DIVIDE,
            MODULO,
        }

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

        private static IDictionary<string, SCRTermCondition> TranslateConditions;
        private static IDictionary<string, SCRTermOperator> TranslateOperator;
        private static IDictionary<string, SCRAndOr> TranslateAndOr;

#if DEBUG_PRINT_IN
        public static string din_fileLoc = @"C:\temp\";     /* file path for debug files */
#endif

#if DEBUG_PRINT_OUT
        public static string dout_fileLoc = @"C:\temp\";    /* file path for debug files */
#endif

        public IDictionary<SignalType, SCRScripts> Scripts;
        private static string keepLine = String.Empty;

        public readonly IDictionary<string, SignalFunction> SignalFunctions;

        //================================================================================================//
        //
        // Constructor
        //
        //================================================================================================//

        public SignalScripts(string RoutePath, IList<string> ScriptFiles, IDictionary<string, SignalType> SignalTypes, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
        {

            // Create required translators

            Scripts = new Dictionary<SignalType, SCRScripts>();
            TranslateConditions = new Dictionary<string, SCRTermCondition>();
            TranslateConditions.Add(">", SCRTermCondition.GT);
            TranslateConditions.Add(">=", SCRTermCondition.GE);
            TranslateConditions.Add("<", SCRTermCondition.LT);
            TranslateConditions.Add("<=", SCRTermCondition.LE);
            TranslateConditions.Add("==", SCRTermCondition.EQ);
            TranslateConditions.Add("!=", SCRTermCondition.NE);
            TranslateConditions.Add("::", SCRTermCondition.NE);  // dummy (for no separator)

            TranslateAndOr = new Dictionary<string, SCRAndOr>();
            TranslateAndOr.Add("&&", SCRAndOr.AND);
            TranslateAndOr.Add("||", SCRAndOr.OR);
            TranslateAndOr.Add("AND", SCRAndOr.AND);
            TranslateAndOr.Add("OR", SCRAndOr.OR);
            TranslateAndOr.Add("??", SCRAndOr.NONE);

            TranslateOperator = new Dictionary<string, SCRTermOperator>();
            TranslateOperator.Add("?", SCRTermOperator.NONE);
            TranslateOperator.Add("-", SCRTermOperator.MINUS);  // needs to come first to avoid it being interpreted as range separator
            TranslateOperator.Add("*", SCRTermOperator.MULTIPLY);
            TranslateOperator.Add("+", SCRTermOperator.PLUS);
            TranslateOperator.Add("/", SCRTermOperator.DIVIDE);
            TranslateOperator.Add("%", SCRTermOperator.MODULO);

            SignalFunctions = signalFunctions;

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

            foreach (string FileName in ScriptFiles)
            {
                string fullName = String.Concat(RoutePath, @"\", FileName);
                int readLineNumber = 0;

#if !DEBUG_ALLOWCRASH
                try
                {
                    using (StreamReader scrStream = new StreamReader(fullName, true))
                    {
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt", "Reading file : " + fullName + "\n\n");
#endif
                        sigscrRead(fullName, scrStream, SignalTypes, ref readLineNumber, signalFunctions, ORNormalSubtypes);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error while reading signal script - {0} at line {1} : {2}", fullName, readLineNumber, ex.ToString());
                }
#else
                // for test purposes : without exception catch
                StreamReader scrStream = new StreamReader(fullName, true);
                sigscrRead(fullName, scrStream, SignalTypes, ref readLineNumber, signalFunctions, ORNormalSubtypes);
                scrStream.Close();
#endif
            }
        }// Constructor

        //================================================================================================//
        //
        // overall script file routines
        //
        //================================================================================================//
        //
        //  Read script from file
        //
        //================================================================================================//

        public void sigscrRead(string scrFileName, StreamReader scrStream, IDictionary<string, SignalType> SignalTypes, ref int readLineNumber, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
        {
            scrReadInfo readInfo;
            string readLine;
            bool ScriptFound = false;
            string scriptname = String.Empty;
            int scriptline = -1;
            int readnumber = 0;
            List<scrReadInfo> ScriptLines = new List<scrReadInfo>();

            readInfo = scrReadLine(scrStream, readnumber);
            readLine = readInfo == null ? null : readInfo.Readline;
            readnumber = readInfo == null ? readnumber : readInfo.Linenumber;
            readLineNumber = readnumber;

            // search for first SCRIPT - skip lines until first script found

            while (readLine != null && !ScriptFound)
            {
                if (readLine.StartsWith("SCRIPT "))
                {
                    ScriptFound = true;
                    scriptname = readLine.Substring(7);
                    scriptline = readInfo.Linenumber;
                }
                else
                {
                    readInfo = scrReadLine(scrStream, readnumber);
                    readLine = readInfo == null ? null : readInfo.Readline;
                    readnumber = readInfo == null ? readnumber : readInfo.Linenumber;
                    readLineNumber = readnumber;
                }
            }

            // process SCRIPT

            while (readLine != null)
            {
                readInfo = scrReadLine(scrStream, readnumber);
                readLine = readInfo == null ? null : readInfo.Readline;
                readnumber = readInfo == null ? readnumber : readInfo.Linenumber;
                readLineNumber = readnumber;

                if (readLine != null)
                {

                    // new SCRIPT line - process stored lines

                    if (readLine.StartsWith("SCRIPT "))
                    {
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt", "\n===============================\n");
                        File.AppendAllText(din_fileLoc + @"sigscr.txt", "\nNew Script : " + scriptname + "\n");
#endif
#if DEBUG_PRINT_OUT
                        File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n===============================\n");
                        File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nNew Script : " + scriptname + "\n");
#endif
                        SCRScripts newScript = new SCRScripts(ScriptLines, scriptname, signalFunctions, ORNormalSubtypes);
                        bool validScript = AllocateScriptToSignalType(newScript, SignalTypes, scriptname, readnumber, scrFileName, ref Scripts);

#if DEBUG_PRINT_OUT
                        if (!validScript)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                            "\nUnknown signal type : " + scriptname + "\n\n");
                        }
#endif
#if DEBUG_PRINT_IN
                        if (!validScript)
                        {
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", "\nUnknown signal type : " + scriptname + "\n\n");
                        }
#endif

                        scriptname = readLine.Substring(7);
                        scriptline = readInfo.Linenumber;
                    }

                    // new REM SCRIPT line - process stored lines, skip until new SCRIPT found

                    else if (readLine.StartsWith("REM SCRIPT "))
                    {
                        SCRScripts newScript = new SCRScripts(ScriptLines, scriptname, signalFunctions, ORNormalSubtypes);
                        bool validScript = AllocateScriptToSignalType(newScript, SignalTypes, scriptname, readnumber, scrFileName, ref Scripts);

                        while (!readLine.StartsWith("SCRIPT ") && readLine != null)
                        {
                            readInfo = scrReadLine(scrStream, readnumber);
                            readLine = readInfo == null ? null : readInfo.Readline;
                            readnumber = readInfo == null ? readnumber : readInfo.Linenumber;
                            readLineNumber = readnumber;
                        }
                        scriptname = readLine.Substring(7);
                        scriptline = readInfo.Linenumber;
                    }

                    // store line

                    else
                    {
                        readInfo.Scriptname = scriptname;
                        ScriptLines.Add(readInfo);

                        if (readInfo.Linenumber % 1000 == 1)
                        {
                            Trace.Write("s");
                        }
                    }
                }
            }

            // process last SCRIPT if any

            if (ScriptLines.Count > 0)
            {
#if DEBUG_PRINT_IN
                File.AppendAllText(din_fileLoc + @"sigscr.txt", "\n===============================\n");
                File.AppendAllText(din_fileLoc + @"sigscr.txt", "\nNew Script : " + scriptname + "\n");
#endif
                SCRScripts newScript = new SCRScripts(ScriptLines, scriptname, signalFunctions, ORNormalSubtypes);
                bool validScript = AllocateScriptToSignalType(newScript, SignalTypes, scriptname, readnumber, scrFileName, ref Scripts);
            }

#if DEBUG_PRINT_OUT

            // print processed details

            foreach (KeyValuePair<SignalType, SCRScripts> thispair in Scripts)
            {

                SCRScripts thisscript = thispair.Value;

                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Script : " + thisscript.scriptname + "\n\n");
                printscript(thisscript.Statements);
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n=====================\n");
            }
#endif

        }// SigscrRead


#if DEBUG_PRINT_OUT
        //================================================================================================//
        //
        // print processed script - for DEBUG purposes only
        //
        //================================================================================================//

        public void printscript(ArrayList Statements)
        {
            bool function = false;
            List<int> Sublevels = new List<int>();

            foreach (object scriptstat in Statements)
            {

                // process statement lines

                if (scriptstat is SCRScripts.SCRStatement)
                {
                    SCRScripts.SCRStatement ThisStat = (SCRScripts.SCRStatement)scriptstat;
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Statement : \n");
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                    ThisStat.AssignType.ToString() + "[" + ThisStat.AssignParameter.ToString() + "] = ");

                    foreach (SCRScripts.SCRStatTerm ThisTerm in ThisStat.StatementTerms)
                    {
                        if (ThisTerm.issublevel > 0)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                            " <SUB" + ThisTerm.issublevel.ToString() + "> ");
                        }
                        function = false;
                        if (ThisTerm.Function != SCRExternalFunctions.NONE)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                    ThisTerm.Function.ToString() + "(");
                            function = true;
                        }

                        if (ThisTerm.PartParameter != null)
                        {
                            foreach (SCRScripts.SCRParameterType ThisParam in ThisTerm.PartParameter)
                            {
                                File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                        ThisParam.PartType + "[" + ThisParam.PartParameter + "] ,");
                            }
                        }

                        if (ThisTerm.sublevel != 0)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", " SUBTERM_" + ThisTerm.sublevel.ToString());
                        }

                        if (function)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ")");
                        }
                        File.AppendAllText(dout_fileLoc + @"scriptproc.txt", " -" + ThisTerm.TermOperator.ToString() + "- \n");
                    }

                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n\n");
                }

                // process conditions line

                if (scriptstat is SCRScripts.SCRConditionBlock)
                {
                    SCRScripts.SCRConditionBlock CondBlock = (SCRScripts.SCRConditionBlock)scriptstat;
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nCondition : \n");

                    printConditionArray(CondBlock.Conditions);

                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nIF Block : \n");
                    printscript(CondBlock.IfBlock.Statements);

                    if (CondBlock.ElseIfBlock != null)
                    {
                        foreach (SCRScripts.SCRBlock TempBlock in CondBlock.ElseIfBlock)
                        {
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nStatements in ELSEIF : " +
                                    TempBlock.Statements.Count + "\n");
                            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Elseif Block : \n");
                            printscript(TempBlock.Statements);
                        }
                    }

                    if (CondBlock.ElseBlock != null)
                    {
                        File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nElse Block : \n");
                        printscript(CondBlock.ElseBlock.Statements);
                    }

                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\nEnd IF Block : \n");

                }
            }
        }// printscript

        //================================================================================================//
        //
        // print condition info - for DEBUG purposes only
        //
        //================================================================================================//

        public void printConditionArray(ArrayList Conditions)
        {
            foreach (object ThisCond in Conditions)
            {
                if (ThisCond is SCRScripts.SCRConditions)
                {
                    printcondition((SCRScripts.SCRConditions)ThisCond);
                }
                else if (ThisCond is SCRAndOr)
                {
                    SCRAndOr condstring = (SCRAndOr)ThisCond;
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", condstring.ToString() + "\n");
                }
                else if (ThisCond is SCRNegate)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "NEGATED : \n");
                }
                else
                {
                    printConditionArray((ArrayList)ThisCond);
                }
            }
        }// printConditionArray

        //================================================================================================//
        //
        // print condition statement - for DEBUG purposes only
        //
        //================================================================================================//

        public void printcondition(SCRScripts.SCRConditions ThisCond)
        {

            bool function = false;
            if (ThisCond.negate1)
            {
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "NOT : ");
            }
            if (ThisCond.Term1.Function != SCRExternalFunctions.NONE)
            {
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ThisCond.Term1.Function.ToString() + "(");
                function = true;
            }

            if (ThisCond.Term1.PartParameter != null)
            {
                foreach (SCRScripts.SCRParameterType ThisParam in ThisCond.Term1.PartParameter)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ThisParam.PartType + "[" + ThisParam.PartParameter + "] ,");
                }
            }
            else
            {
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", " 0 , ");
            }

            if (function)
            {
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ")");
            }

            File.AppendAllText(dout_fileLoc + @"scriptproc.txt", " -- " + ThisCond.Condition.ToString() + " --\n");

            if (ThisCond.Term2 != null)
            {
                function = false;
                if (ThisCond.negate2)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "NOT : ");
                }
                if (ThisCond.Term2.Function != SCRExternalFunctions.NONE)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ThisCond.Term2.Function.ToString() + "(");
                    function = true;
                }

                if (ThisCond.Term2.PartParameter != null)
                {
                    foreach (SCRScripts.SCRParameterType ThisParam in ThisCond.Term2.PartParameter)
                    {
                        File.AppendAllText(dout_fileLoc + @"scriptproc.txt",
                                        ThisParam.PartType + "[" + ThisParam.PartParameter + "] ,");
                    }
                }
                else
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", " 0 , ");
                }

                if (function)
                {
                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", ")");
                }
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n");
            }
        }// printcondition
#endif

        //================================================================================================//
        //
        // allocate script to required signal type
        //
        //================================================================================================//

        public bool AllocateScriptToSignalType(SCRScripts newScript, IDictionary<string, SignalType> SignalTypes, string scriptname, int readnumber, string scrFileName,
            ref IDictionary<SignalType, SCRScripts> Scripts)
        {
            bool validType = false;
            SignalType thisType;

            // try and find signal type with same name as script
            if (SignalTypes.TryGetValue(scriptname.ToLower().Trim(), out thisType))
            {
                if (Scripts.ContainsKey(thisType))
                {
                    Trace.TraceWarning("Ignored duplicate SignalType script {2} in {0}:line {1}", scrFileName, readnumber, scriptname);
                }
                else
                {
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "Adding script : " + thisType.Name + "\n");
#endif
                    Scripts.Add(thisType, newScript);
                    validType = true;
                }
            }

            // try and find any other signal types which reference this script
            foreach (KeyValuePair<string, SignalType> SelectedSignal in SignalTypes)
            {
                SignalType SelectedSignalType = SelectedSignal.Value;
                if (!String.IsNullOrEmpty(SelectedSignalType.Script))
                {
                    if (String.Equals(scriptname, SelectedSignalType.Script, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (Scripts.ContainsKey(SelectedSignalType))
                        {
                            Trace.TraceWarning("Ignored duplicate SignalType script {2} in {0}:line {1}", scrFileName, readnumber, scriptname);
                        }
                        else
                        {
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt", "Adding script : " + SelectedSignalType.Script + " to " + SelectedSignalType.Name + "\n");
#endif
                            Scripts.Add(SelectedSignalType, newScript);
                            validType = true;
                        }
                    }
                }
            }
            return (validType);
        }

        //================================================================================================//
        //
        // read single line from file
        // skip comment and empty lines
        //
        //================================================================================================//

        public static scrReadInfo scrReadLine(StreamReader scrStream, int lastline)
        {
            string readLine;
            string procLine = String.Empty;
            bool validLine = false;
            bool compart = false;
            int linenumber = lastline;

            // check if anything still in store

            if (String.IsNullOrEmpty(keepLine))
            {
                readLine = scrStream.ReadLine();
                linenumber++;
#if DEBUG_PRINT_IN
                File.AppendAllText(din_fileLoc + @"sigfile.txt", "From file : (" + linenumber.ToString() + ") : " + readLine + "\n");
#endif

            }
            else
            {
                readLine = keepLine;
                keepLine = String.Empty;
#if DEBUG_PRINT_IN
                File.AppendAllText(din_fileLoc + @"sigfile.txt", "From store : " + readLine + "\n");
#endif
            }

            // loop until valid line found

            while (readLine != null && !validLine)
            {

                // remove comment

                if (compart)
                {
                    procLine = String.Concat(@"/*", readLine.ToUpper());  // force as comment
                }
                else
                {
                    procLine = readLine.ToUpper();
                }

                procLine = procLine.Replace("\t", " ");
                procLine = procLine.Trim();

                int comsep = procLine.IndexOf(@"//");
                int addsep = procLine.IndexOf(@"/*");
                int endsep = procLine.IndexOf(@"*/");

                if (comsep == 0)
                {
                    procLine = String.Empty;
                }
                else if (comsep > 0)
                {
                    procLine = procLine.Substring(0, comsep).Trim();
                }

                if (addsep == 0)
                {
                    compart = (endsep <= 0); // No end comment
                    procLine = String.Empty;
                }

                // check if empty, else read next line

                if (procLine.Length > 0)
                {
                    validLine = true;
                }
                else
                {
                    readLine = scrStream.ReadLine();
                    linenumber++;
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigfile.txt",
                                    "Invalid line, next from file :(" + linenumber.ToString() + ") : " + readLine + "\n");
#endif
                }
            }

            // if ';' in string, split there and keep rest

            char[] sepCheck = ";{}".ToCharArray();
            int seppos = procLine.IndexOfAny(sepCheck);
#if DEBUG_PRINT_IN
            File.AppendAllText(din_fileLoc + @"sigfile.txt", "Extracted : " + procLine + "\n");
#endif
            if (seppos >= 0)
            {
                if (String.Compare(procLine.Substring(seppos, 1), ";") == 0)
                {
                    keepLine = procLine.Substring(seppos + 1);
                    procLine = procLine.Substring(0, seppos + 1);
                }
                else if (seppos == 0)
                {
                    keepLine = procLine.Substring(seppos + 1);
                    procLine = procLine.Substring(0, 1);
                }
                else
                {
                    keepLine = procLine.Substring(seppos);
                    procLine = procLine.Substring(0, seppos);
                }
#if DEBUG_PRINT_IN
                File.AppendAllText(din_fileLoc + @"sigfile.txt", "To store : " + keepLine + "\n");
#endif
            }

            // if "IF(" in string, replace with "IF ("

            int ifbrack = procLine.IndexOf("IF(");
            if (ifbrack >= 0)
            {
                procLine = procLine.Substring(0, ifbrack) + "IF (" + procLine.Substring(ifbrack + 3);
            }

            // return line or null

            if (readLine == null)
            {
                return null;
            }
            else
            {
#if DEBUG_PRINT_IN
                File.AppendAllText(din_fileLoc + @"sigfile.txt", "To process : " + procLine + "\n");
#endif
                scrReadInfo procInfo = new scrReadInfo(procLine, linenumber, String.Empty);
                return procInfo;
            }
        }// scrReadLine

        //================================================================================================//
        //
        // script parsing class - handles a single script from the script file
        //
        //================================================================================================//
        //
        // class SCRScripts
        //
        //================================================================================================//

        public class SCRScripts
        {

            private IDictionary<string, uint> LocalFloats;
            public uint totalLocalFloats;
            public ArrayList Statements;
            public string scriptname;

            //================================================================================================//
            //
            // Constructor
            // Input is list with all lines for one signal script
            //

            public SCRScripts(List<scrReadInfo> ScriptLines, string scriptnameIn, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
            {
                LocalFloats = new Dictionary<string, uint>();
                totalLocalFloats = 0;
                Statements = new ArrayList();

                int lcount = 0;
                int maxcount = ScriptLines.Count;

                scriptname = scriptnameIn;

#if DEBUG_PRINT_IN
                // print inputlines

                foreach (scrReadInfo InfoLine in ScriptLines)
                {
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", InfoLine.Readline + "\n");
                }
                File.AppendAllText(din_fileLoc + @"sigscr.txt", "\n+++++++++++++++++++++++++++++++++++\n\n");

#endif

                // Skip external floats (exist automatically)

                bool exfloat = ScriptLines[lcount].Readline.StartsWith("EXTERN FLOAT ");
                while (exfloat && lcount < maxcount)
                {
                    lcount++;
                    exfloat = ScriptLines[lcount].Readline.StartsWith("EXTERN FLOAT ");
                }

                // Process floats : build list with internal floats

                bool infloat = ScriptLines[lcount].Readline.StartsWith("FLOAT ");
                while (infloat && lcount < maxcount)
                {
                    string floatstring = ScriptLines[lcount].Readline.Substring(6);
                    floatstring = floatstring.Trim();
                    int endstring = floatstring.IndexOf(";");
                    floatstring = floatstring.Substring(0, endstring);
                    floatstring = floatstring.Trim();

                    if (!LocalFloats.ContainsKey(floatstring))
                    {
                        LocalFloats.Add(floatstring, totalLocalFloats);
                        totalLocalFloats++;
                    }

                    lcount++;
                    infloat = ScriptLines[lcount].Readline.StartsWith("FLOAT ");
                }

#if DEBUG_PRINT_OUT
                // print details of internal floats

                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "\n\nFloats : \n");
                foreach (KeyValuePair<string, uint> deffloat in LocalFloats)
                {
                    string defstring = deffloat.Key;
                    uint defindex = deffloat.Value;

                    File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Float : " + defstring + " = " + defindex.ToString() + "\n");
                }
                File.AppendAllText(dout_fileLoc + @"scriptproc.txt", "Total : " + totalLocalFloats.ToString() + "\n\n\n");
#endif

                // Check rest of file - statements

                Statements = processScriptLines(ScriptLines, lcount, LocalFloats, signalFunctions, ORNormalSubtypes);
                ScriptLines.Clear();

            }// constructor


            //================================================================================================//
            //
            // parsing routines
            //
            //================================================================================================//
            //
            // process all process lines
            // this function is also called recursively to process separate lower level IF and ELSE blocks
            //
            //================================================================================================//

            public static ArrayList processScriptLines(List<scrReadInfo> PSLScriptLines, int index, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
            {

                int lcount = index;
                int nextcount;
                List<int> ifblockcount;
                ArrayList localStatements = new ArrayList();

                // loop through all lines

                while (lcount < PSLScriptLines.Count)
                {

                    // clear enclosing { and } if still in string

                    int sepparent = PSLScriptLines[lcount].Readline.IndexOf("{");
                    while (sepparent >= 0)
                    {
                        PSLScriptLines[lcount].Readline = PSLScriptLines[lcount].Readline.Replace("{", String.Empty).Trim();
                        sepparent = PSLScriptLines[lcount].Readline.IndexOf("{");
                    }

                    sepparent = PSLScriptLines[lcount].Readline.IndexOf("}");
                    while (sepparent >= 0)
                    {
                        PSLScriptLines[lcount].Readline = PSLScriptLines[lcount].Readline.Replace("}", String.Empty).Trim();
                        sepparent = PSLScriptLines[lcount].Readline.IndexOf("}");
                    }

                    // process IF statement
                    // all lines in IF (-ELSEIF) (-ELSE)  block will be handled by this function call

                    if (PSLScriptLines[lcount].Readline.StartsWith("IF "))
                    {
                        ifblockcount = findEndIfBlock(PSLScriptLines, lcount);
                        SCRConditionBlock thisCondition = new SCRConditionBlock(PSLScriptLines, lcount, ifblockcount, LocalFloats, signalFunctions, ORNormalSubtypes);
                        nextcount = ifblockcount[ifblockcount.Count - 1];
                        localStatements.Add(thisCondition);
                    }

                    // process statement

                    else if (PSLScriptLines[lcount] != null && !String.IsNullOrEmpty(PSLScriptLines[lcount].Readline))
                    {
                        nextcount = FindEndStatement(PSLScriptLines, lcount);
                        SCRStatement thisStatement = new SCRStatement(PSLScriptLines[lcount], LocalFloats, signalFunctions, ORNormalSubtypes);
                        if (thisStatement.valid) localStatements.Add(thisStatement);
                    }

                    // empty line (may be result of removing { and })

                    else
                    {
                        nextcount = lcount + 1;
                    }

                    lcount = nextcount;
                }

                return localStatements;
            } // processScriptlines

            //================================================================================================//
            //
            // Find end of IF condition statement
            // returns index to next line
            //
            //================================================================================================//

            public static int FindEndStatement(List<scrReadInfo> FESScriptLines, int index)
            {
                string presentstring, addline;
                int endpos;
                int actindex;

                //================================================================================================//

                scrReadInfo presentInfo = FESScriptLines[index];
                presentstring = presentInfo.Readline.Trim();
                FESScriptLines.RemoveAt(index);
                endpos = presentstring.IndexOf(";");
                actindex = index;

                // empty string - exit and set index

                if (presentstring.Length < 1)
                {
                    return actindex;
                }

                // search for ; - keep reading until found

                while (endpos <= 0 && actindex < FESScriptLines.Count)
                {
                    addline = FESScriptLines[actindex].Readline;
                    FESScriptLines.RemoveAt(actindex);
                    presentstring = String.Concat(presentstring, addline);
                    endpos = presentstring.IndexOf(";");
                }

                // Illegal statement - no ;

                if (endpos <= 0)
                {
                    Trace.TraceWarning("sigscr-file line {1} : Missing ; in statement starting with {0}", presentstring, presentInfo.Linenumber.ToString());
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "Missing ; in statement starting with " + presentstring + " (" +
                                    presentInfo.Linenumber.ToString() + ")\n");
#endif

                }

                // split string at ; if anything follows after

                if (presentstring.Length > (endpos + 1) && endpos > 0)
                {
                    scrReadInfo splitInfo = new scrReadInfo(presentstring.Substring(endpos + 1).Trim(), presentInfo.Linenumber, presentInfo.Scriptname);
                    FESScriptLines.Insert(index, splitInfo);
                    presentstring = presentstring.Substring(0, endpos + 1).Trim();
                }

                scrReadInfo newInfo = new scrReadInfo(presentstring.Trim(), presentInfo.Linenumber, presentInfo.Scriptname);
                FESScriptLines.Insert(index, newInfo);
                actindex = index + 1;
                return actindex;
            }// FindEndStatement

            //================================================================================================//
            //
            // find end of full IF blocks
            // returns indices to lines following IF part, all (if any) ELSEIF part and (if available) last ELSE part
            // final index is next line after full IF - ELSEIF - ELSE sequence
            // any nested IF blocks are included but not indexed
            //
            // this function is call recursively for nester IF blocks
            //
            //================================================================================================//

            public static List<int> findEndIfBlock(List<scrReadInfo> FEIScriptLines, int index)
            {

                List<int> nextcount = new List<int>();
                scrReadInfo nextinfo;
                scrReadInfo tempinfo;
                string nextline;
                int tempnumber;
                int endIfcount, endElsecount;

                int linecount = FindEndCondition(FEIScriptLines, index);

                nextinfo = FEIScriptLines[linecount];
                nextline = nextinfo.Readline;

                // full block : search for matching parenthesis in next lines
                // set end after related closing }

                endIfcount = linecount;

                if (nextline.Length > 0 && String.Compare(nextline.Substring(0, 1), "{") == 0)
                {
                    endIfcount = findEndBlock(FEIScriptLines, linecount);
                }

                // next statement is another if : insert { and } to ease processing

                else if (String.Compare(nextline.Substring(0, Math.Min(3, nextline.Length)), "IF ") == 0)
                {
                    List<int> fullcount = findEndIfBlock(FEIScriptLines, linecount);
                    int lastline = fullcount[fullcount.Count - 1];
                    string templine = FEIScriptLines[linecount].Readline;
                    FEIScriptLines.RemoveAt(linecount);
                    templine = String.Concat("{ ", templine);
                    tempinfo = new scrReadInfo(templine, nextinfo.Linenumber, nextinfo.Scriptname);
                    FEIScriptLines.Insert(linecount, tempinfo);
                    templine = FEIScriptLines[lastline - 1].Readline;
                    tempnumber = FEIScriptLines[lastline - 1].Linenumber;
                    FEIScriptLines.RemoveAt(lastline - 1);
                    templine = String.Concat(templine, " }");
                    tempinfo = new scrReadInfo(templine, tempnumber, nextinfo.Scriptname);
                    FEIScriptLines.Insert(lastline - 1, tempinfo);
                    endIfcount = lastline;
                }

                // single statement - set end after statement

                else
                {
                    endIfcount = FindEndStatement(FEIScriptLines, linecount);
                }
                nextcount.Add(endIfcount);

                endElsecount = endIfcount;

                // check if next line starts with ELSE or any form of ELSEIF

                nextline = endElsecount < FEIScriptLines.Count ? FEIScriptLines[endElsecount].Readline.Trim() : String.Empty;
                bool endelse = false;

                while (!endelse && endElsecount < FEIScriptLines.Count)
                {
                    bool elsepart = false;

                    // line contains ELSE only

                    if (nextline.Length <= 4)
                    {
                        if (String.Compare(nextline, "ELSE") == 0)
                        {
                            elsepart = true;
                            nextinfo = FEIScriptLines[endElsecount + 1];
                            nextline = nextinfo.Readline;

                            // check if next line start with IF - then this is an ELSEIF

                            if (nextline.StartsWith("IF "))
                            {
                                nextline = String.Concat("ELSEIF ", nextline.Substring(3).Trim());
                                FEIScriptLines.RemoveAt(endElsecount + 1);
                                FEIScriptLines.RemoveAt(endElsecount);
                                tempinfo = new scrReadInfo(nextline, nextinfo.Linenumber, nextinfo.Scriptname);
                                FEIScriptLines.Insert(endElsecount, tempinfo);
                                endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
                                nextinfo = FEIScriptLines[endElsecount];
                                nextline = nextinfo.Readline;
                            }
                            else
                            {
                                endelse = true;
                                endElsecount++;
                            }

                        }
                    }

                    // line starts with ELSE - check if followed by IF
                    // if ELSEIF, store with rest of line
                    // if ELSE, store on separate new line

                    else if (String.Compare(nextline.Substring(0, Math.Min(5, nextline.Length)), "ELSE ") == 0)
                    {
                        elsepart = true;
                        nextline = nextline.Substring(5).Trim();
                        if (nextline.StartsWith("IF "))
                        {
                            nextline = String.Concat("ELSEIF ", nextline.Substring(3).Trim());
                            FEIScriptLines.RemoveAt(endElsecount);
                            tempinfo = new scrReadInfo(nextline, nextinfo.Linenumber, nextinfo.Scriptname);
                            FEIScriptLines.Insert(endElsecount, tempinfo);
                            endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
                            nextinfo = FEIScriptLines[endElsecount];
                            nextline = nextinfo.Readline;
                        }
                        else
                        {
                            endelse = true;

                            FEIScriptLines.RemoveAt(endElsecount);
                            tempinfo = new scrReadInfo(nextline.Trim(), nextinfo.Linenumber, nextinfo.Scriptname);
                            FEIScriptLines.Insert(endElsecount, tempinfo);
                            tempinfo = new scrReadInfo("ELSE", nextinfo.Linenumber, nextinfo.Scriptname);
                            FEIScriptLines.Insert(endElsecount, tempinfo);
                            endElsecount++;
                        }
                    }

                    // line starts with ELSEIF 

                    else if (String.Compare(nextline.Substring(0, Math.Min(7, nextline.Length)), "ELSEIF ") == 0)
                    {
                        elsepart = true;
                        endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
                        nextinfo = FEIScriptLines[endElsecount];
                        nextline = nextinfo.Readline;
                    }

                    // line starts with ELSE{
                    // store ELSE on separate new line

                    else if (String.Compare(nextline.Substring(0, Math.Min(5, nextline.Length)), "ELSE{") == 0)
                    {
                        elsepart = true;
                        endelse = true;
                        nextline = nextline.Substring(5).Trim();
                        FEIScriptLines.RemoveAt(endElsecount);
                        tempinfo = new scrReadInfo(nextline, nextinfo.Linenumber, nextinfo.Scriptname);
                        FEIScriptLines.Insert(endElsecount, tempinfo);
                        nextline = "{";
                        tempinfo = new scrReadInfo("{", nextinfo.Linenumber, nextinfo.Scriptname);
                        FEIScriptLines.Insert(endElsecount, tempinfo);
                        tempinfo = new scrReadInfo("ELSE", nextinfo.Linenumber, nextinfo.Scriptname);
                        FEIScriptLines.Insert(endElsecount, tempinfo);
                    }

                    // if an ELSE or ELSEIF part is found - find end 

                    if (elsepart)
                    {
                        if (String.Compare(nextline.Substring(0, 1), "{") == 0)
                        {
                            endElsecount = findEndBlock(FEIScriptLines, endElsecount);
                        }
                        else if (String.Compare(nextline.Substring(0, Math.Min(3, nextline.Length)), "IF ") == 0)
                        {
                            List<int> fullcount = findEndIfBlock(FEIScriptLines, endElsecount);
                            int lastline = fullcount[fullcount.Count - 1];
                            string templine = FEIScriptLines[endElsecount].Readline;
                            FEIScriptLines.RemoveAt(endElsecount);
                            templine = String.Concat("{ ", templine);
                            tempinfo = new scrReadInfo(templine, nextinfo.Linenumber, nextinfo.Scriptname);
                            FEIScriptLines.Insert(endElsecount, tempinfo);
                            templine = FEIScriptLines[lastline - 1].Readline;
                            tempnumber = FEIScriptLines[lastline - 1].Linenumber;
                            FEIScriptLines.RemoveAt(lastline - 1);
                            templine = String.Concat(templine, " }");
                            tempinfo = new scrReadInfo(templine, tempnumber, nextinfo.Scriptname);
                            FEIScriptLines.Insert(lastline - 1, tempinfo);
                            endElsecount = lastline;
                        }
                        else
                        {
                            endElsecount = FindEndStatement(FEIScriptLines, endElsecount);
                        }
                        nextline = endElsecount < FEIScriptLines.Count ? FEIScriptLines[endElsecount].Readline.Trim() : String.Empty;
                        nextcount.Add(endElsecount);
                    }
                    else
                    {
                        endelse = true;
                    }
                }

                return nextcount;
            }// findEndIfBlock

            //================================================================================================//
            //
            // find end of IF block enclosed by { and }
            //
            //================================================================================================//

            public static int findEndBlock(List<scrReadInfo> FEBScriptLines, int index)
            {

                scrReadInfo firstinfo, thisinfo, tempinfo;

                // Use regular expression to find all occurences of { and }
                // Keep searching through next lines until match is found

                int openparent = 0;
                int closeparent = 0;

                int openindex = 0;
                int closeindex = 0;

                Regex openparstr = new Regex("{");
                Regex closeparstr = new Regex("}");

                firstinfo = FEBScriptLines[index];
                string presentline = firstinfo.Readline;

                bool blockEnd = false;
                int splitpoint = -1;
                int checkcount = index;

                // get positions in present line

                MatchCollection opencount = openparstr.Matches(presentline);
                MatchCollection closecount = closeparstr.Matches(presentline);

                // convert to ARRAY

                int totalopen = opencount.Count;
                int totalclose = closecount.Count;

                Match[] closearray = new Match[totalclose];
                closecount.CopyTo(closearray, 0);
                Match[] openarray = new Match[totalopen];
                opencount.CopyTo(openarray, 0);

                // search until match found
                while (!blockEnd)
                {

                    // get next position (continue from previous index)

                    int openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
                    int closepos = closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;

                    // next is open {

                    if (openpos < closepos)
                    {
                        openparent++;
                        openindex++;
                        openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
                    }

                    // next is close }

                    else if (closepos < openpos)
                    {
                        closeparent++;

                        // check for match - if found, end of block is found

                        if (closeparent == openparent)
                        {
                            blockEnd = true;
                            splitpoint = closepos;
                        }
                        else
                        {
                            closeindex++;
                            closepos = closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;
                        }
                    }

                    // openpos and closepos equal - both have reached end of line - get next line

                    else
                    {
                        checkcount++;
                        if (checkcount >= FEBScriptLines.Count)
                        {
                            Trace.TraceWarning("sigscr-file line {0} : unbalanced curly brackets : ", index.ToString());
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt",
                                            "unbalanced curly brackets at " + index.ToString() + "\n");
#endif
                            return (FEBScriptLines.Count - 1);
                        }

                        thisinfo = FEBScriptLines[checkcount];
                        presentline = thisinfo.Readline;

                        // get positions

                        opencount = openparstr.Matches(presentline);
                        closecount = closeparstr.Matches(presentline);

                        totalopen = opencount.Count;
                        totalclose = closecount.Count;

                        // convert to array

                        closearray = new Match[totalclose];
                        closecount.CopyTo(closearray, 0);
                        openarray = new Match[totalopen];
                        opencount.CopyTo(openarray, 0);

                        openindex = 0;
                        closeindex = 0;

                        // get next positions

                        openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
                        closepos = closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;
                    }
                }

                // end found - check if anything follows final }

                int nextcount = checkcount + 1;
                thisinfo = FEBScriptLines[checkcount];
                presentline = thisinfo.Readline.Trim();

                if (splitpoint >= 0 && splitpoint < presentline.Length - 1)
                {
                    thisinfo = FEBScriptLines[checkcount];
                    presentline = thisinfo.Readline;
                    FEBScriptLines.RemoveAt(checkcount);

                    tempinfo = new scrReadInfo(presentline.Substring(splitpoint + 1).Trim(), firstinfo.Linenumber, firstinfo.Scriptname);
                    FEBScriptLines.Insert(checkcount, tempinfo);
                    tempinfo = new scrReadInfo(presentline.Substring(0, splitpoint + 1).Trim(), firstinfo.Linenumber, firstinfo.Scriptname);
                    FEBScriptLines.Insert(checkcount, tempinfo);
                }

                return nextcount;
            }//findEndBlock

            //================================================================================================//
            //
            // find end of IF condition statement
            //
            //================================================================================================//

            public static int FindEndCondition(List<scrReadInfo> FECScriptLines, int index)
            {
                string presentstring, addline;
                int totalopen, totalclose;
                int actindex;

                scrReadInfo thisinfo, addinfo, tempinfo;

                //================================================================================================//

                actindex = index;

                thisinfo = FECScriptLines[index];
                presentstring = thisinfo.Readline;
                FECScriptLines.RemoveAt(index);

                // use regular expression to search for open and close bracket

                Regex openbrack = new Regex(@"\(");
                Regex closebrack = new Regex(@"\)");

                // search for open bracket

                MatchCollection opencount = openbrack.Matches(presentstring);
                totalopen = opencount.Count;

                // add lines until open bracket found

                while (totalopen <= 0 && actindex < FECScriptLines.Count)
                {
                    addinfo = FECScriptLines[actindex];
                    addline = addinfo.Readline;
                    FECScriptLines.RemoveAt(actindex);
                    presentstring = String.Concat(presentstring, addline);
                    opencount = openbrack.Matches(presentstring);
                    totalopen = opencount.Count;
                }

                if (totalopen <= 0)
                {
                    Trace.TraceWarning("sigscr-file line {1} : If statement without ( ; starting with {0}", presentstring, thisinfo.Linenumber.ToString());
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt",
                                    "If statement without ( ; starting with {0}" + presentstring + " (" + thisinfo.Linenumber.ToString() + ")\n");
#endif
                }

                // in total string, search for close brackets

                MatchCollection closecount = closebrack.Matches(presentstring);
                totalclose = closecount.Count;

                // keep adding lines until open and close brackets match

                while (totalclose < totalopen && actindex < FECScriptLines.Count)
                {
                    addinfo = FECScriptLines[actindex];
                    addline = addinfo.Readline;
                    FECScriptLines.RemoveAt(actindex);
                    presentstring = String.Concat(presentstring, addline);

                    opencount = openbrack.Matches(presentstring);
                    totalopen = opencount.Count;
                    closecount = closebrack.Matches(presentstring);
                    totalclose = closecount.Count;
                }

                actindex = index;

                if (totalclose < totalopen)
                {

                    // locate first "{" - assume this to be the end of the IF statement

                    int possibleEnd = presentstring.IndexOf("{");

                    Trace.TraceWarning("sigscr-file line {1} : Missing ) in IF statement ; starting with {0}",
                    presentstring, thisinfo.Linenumber.ToString());

                    string reportString = presentstring;
                    if (possibleEnd > 0)
                    {
                        reportString = presentstring.Substring(0, possibleEnd);

                        Trace.TraceWarning("IF statement set to : {0}", reportString + ")");

                        tempinfo = new scrReadInfo(presentstring.Substring(possibleEnd).Trim(),
                            thisinfo.Linenumber, thisinfo.Scriptname);
                        FECScriptLines.Insert(index, tempinfo);
                        presentstring = String.Concat(presentstring.Substring(0, possibleEnd), ")");
                        actindex = index + 1;
                    }

#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "If statement without ) ; starting with " + reportString +
                                   " (" + thisinfo.Linenumber.ToString() + ")\n");
#endif
                }
                else
                {

                    // get position of final close bracket - end of condition statement

                    Match[] closearray = new Match[totalclose];
                    closecount.CopyTo(closearray, 0);
                    Match[] openarray = new Match[totalopen];
                    opencount.CopyTo(openarray, 0);

                    // match open and close brackets - when matched, that is end of condition

                    int actbracks = 1;

                    int actopen = 1;
                    int openpos = actopen < openarray.Length ? openarray[actopen].Index : presentstring.Length + 1;

                    int actclose = 0;
                    int closepos = closearray[actclose].Index;

                    while (actbracks > 0)
                    {
                        if (openpos < closepos)
                        {
                            actbracks++;
                            actopen++;
                            openpos = actopen < openarray.Length ? openarray[actopen].Index : presentstring.Length + 1;
                        }
                        else
                        {
                            actbracks--;
                            if (actbracks > 0)
                            {
                                actclose++;
                                closepos = actclose < closearray.Length ? closearray[actclose].Index : presentstring.Length + 1;
                            }
                        }
                    }

                    // split on end of condition

                    if (closepos < (presentstring.Length - 1))
                    {
                        tempinfo = new scrReadInfo(presentstring.Substring(closepos + 1).Trim(), thisinfo.Linenumber, thisinfo.Scriptname);
                        FECScriptLines.Insert(index, tempinfo);
                        presentstring = presentstring.Substring(0, closepos + 1);
                    }
                    actindex = index + 1;
                }

                tempinfo = new scrReadInfo(presentstring.Trim(), thisinfo.Linenumber, thisinfo.Scriptname);
                FECScriptLines.Insert(index, tempinfo);
                return actindex;
            }//findEndCondition

            //================================================================================================//
            //
            // process function call (in statement or in IF condition)
            //
            //================================================================================================//

            static public ArrayList process_FunctionCall(string FunctionStatement, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes, int linenumber)
            {
                ArrayList FunctionParts = new ArrayList();
                bool valid_func = true;

                // split in function and parameter parts

                string[] StatementParts = FunctionStatement.Split('(');
                if (StatementParts.Length > 2)
                {
                    valid_func = false;
                    Trace.TraceWarning("sigscr-file line {1} : Unexpected number of ( in function call : {0}", FunctionStatement, linenumber.ToString());
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "Unexpected number of ( in function call : " + FunctionStatement + "\n");
#endif
                }

                // process function part
                if (Enum.TryParse(StatementParts[0], true, out SCRExternalFunctions exFunction))
                {
                    FunctionParts.Add(exFunction);
                }
                else
                {
                    valid_func = false;
                    Trace.TraceWarning("sigscr-file line {1} : Unknown function call : {0}\n",
                        FunctionStatement, linenumber.ToString());
#if DEBUG_PRINT_IN
                    File.AppendAllText(din_fileLoc + @"sigscr.txt", "Unknown function call : " + FunctionStatement + "\n");
#endif
                }


                // remove closing bracket

                string ParameterPart = StatementParts[1].Replace(")", String.Empty).Trim();

                // process first parameters in case of multiple parameters

                int sepindex = ParameterPart.IndexOf(",");
                while (sepindex > 0 && valid_func)
                {
                    string parmPart = ParameterPart.Substring(0, sepindex).Trim();
                    SCRParameterType TempParm = process_TermPart(parmPart, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);
                    FunctionParts.Add(TempParm);

                    ParameterPart = ParameterPart.Substring(sepindex + 1).Trim();
                    sepindex = ParameterPart.IndexOf(",");
                }

                // process last or only parameter if set

                if (!String.IsNullOrEmpty(ParameterPart) && valid_func)
                {
                    SCRParameterType TempParm = process_TermPart(ParameterPart, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);
                    FunctionParts.Add(TempParm);
                }

                // return null in case of error

                if (!valid_func)
                {
                    FunctionParts = null;
                }

                return FunctionParts;
            }//process_FunctionCall

            //================================================================================================//
            //
            // process term part of statement (right-hand side)
            //
            //================================================================================================//

            public static SCRParameterType process_TermPart(string TermString, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes, int linenumber)
            {
                TermString = TermString.ToUpper();

                // Skip over any leading "#"
                if (TermString.StartsWith("#"))
                    TermString = TermString.Substring(1);

                // try integer literal
                if (int.TryParse(TermString, out int tmpint))
                    return new SCRParameterType(SCRTermType.Constant, tmpint);

                // try external float, e.g. BLOCK_STATE
                else if (Enum.TryParse(TermString, true, out SCRExternalFloats exFloat))
                    return new SCRParameterType(SCRTermType.ExternalFloat, (int)exFloat);

                // try local float
                else if (LocalFloats.TryGetValue(TermString, out uint def))
                    return new SCRParameterType(SCRTermType.LocalFloat, (int)def);

                int? ParseEnum<TEnum>(string part) where TEnum : struct
                {
                    if (Enum.TryParse(part, ignoreCase: true, out TEnum value))
                        return (int)(object)value;
                    else
                        return null;
                }
                int? ParseList(IList<string> list, string part)
                {
                    var idx = list.IndexOf(part);
                    if (idx > -1)
                        return idx;
                    else
                        return null;
                }
                (string, SCRTermType, Func<string, int?>)[] parameterParsers = {
                    // try BLOCK_CLEAR etc
                    ("BLOCK_", SCRTermType.Block, ParseEnum<MstsBlockState>),
                    // try SIGASP definition
                    ("SIGASP_", SCRTermType.Sigasp, ParseEnum<MstsSignalAspect>),
                    // try SIGFN definition
                    ("SIGFN_", SCRTermType.Sigfn, part => null),
                    // try ORSubtype definition
                    ("ORSUBTYPE_", SCRTermType.ORNormalSubtype, part => ParseList(ORNormalSubtypes, part)),
                    // try SIGFEAT definition
                    ("SIGFEAT_", SCRTermType.Sigfeat, part => ParseList(SignalShape.SignalSubObj.SignalSubTypes, part)),
                };

                foreach (var (tokenType, termType, Parse) in parameterParsers)
                {
                    if (TermString.StartsWith(tokenType))
                    {
                        var part = TermString.Substring(tokenType.Length);
                        var parsed = Parse(part);
                        if (parsed.HasValue)
                        {
                            return new SCRParameterType(termType, parsed.Value);
                        }
                        else if (termType == SCRTermType.Sigfn)
                        {
                            if (signalFunctions.ContainsKey(part.ToUpper()))
                            {
                                return new SCRParameterType(termType, signalFunctions[part.ToUpper()]);
                            }
                            else
                            {
                                TraceError(linenumber, tokenType, TermString);
                            }
                        }
                        else
                        {
                            TraceError(linenumber, tokenType, TermString);
                            break;
                        }
                    }
                }

                return new SCRParameterType(SCRTermType.Constant, 0);
            }//process_TermPart

            private static void Debugprocess_FunctionCall(SCRParameterType TermParts)
            {
                Trace.WriteLine($"SignalScript: {TermParts.PartParameter}, {TermParts.PartType}");
            }

            private static void TraceError(int lineNumber, string tokenType, string termString )
            {
#if DEBUG_PRINT_IN
                                File.AppendAllText(din_fileLoc + @"sigscr.txt", $"Unknown {tokenType} : {termString}\n");
#endif
                Trace.TraceWarning($"sigscr-file line {lineNumber} : Unknown {tokenType} : {termString}");
            }

            //================================================================================================//
            //
            // process IF condition line - split into logic parts
            //
            //================================================================================================//

            public static ArrayList getIfConditions(scrReadInfo GICInfo, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
            {
                SCRConditions ThisCondition;
                ArrayList SCRConditionList = new ArrayList();

                SCRAndOr condAndOr;
                List<string> sublist = new List<string>();

                string GICString = GICInfo.Readline;

                // extract condition between first ( and last )

                int startpos = GICString.IndexOf("(");
                int endpos = GICString.LastIndexOf(")");
                string presentline = GICString.Substring(startpos + 1, endpos - startpos - 1).Trim();

                // search for substrings
                // search for matching brackets

                Regex openparstr = new Regex("[(]");
                Regex closeparstr = new Regex("[)]");

                // get all brackets in string

                MatchCollection opencount = openparstr.Matches(presentline);
                MatchCollection closecount = closeparstr.Matches(presentline);

                int totalopen = opencount.Count;
                int totalclose = closecount.Count;

                if (totalopen > 0)
                {

                    // convert matches to array

                    Match[] closearray = new Match[totalclose];
                    closecount.CopyTo(closearray, 0);
                    Match[] openarray = new Match[totalopen];
                    opencount.CopyTo(openarray, 0);

                    // get positions, find ) which matches first (

                    bool blockEnd = false;
                    int bracklevel = 0;

                    int openindex = 0;
                    int closeindex = 0;

                    int openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
                    int closepos = closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;

                    int firstopen = 0;

                    while (!blockEnd)
                    {
                        if (bracklevel == 0)
                        {
                            firstopen = openpos;
                        }

                        if (openpos < closepos)
                        {
                            bracklevel++;
                            openindex++;
                            openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
                        }
                        else
                        {
                            bracklevel--;

                            // match found, check if any | or & in between
                            // if so, condition is enclosed and must be processed separately
                            // store string in special array
                            // replace string with substitute pointer reference stored string position

                            if (bracklevel == 0)
                            {
                                string substring = presentline.Substring(firstopen, closepos - firstopen + 1);
                                if (CheckCondition(substring) > 0)
                                {
                                    sublist.Add(substring);
                                    string replacestring = "[" + sublist.Count.ToString() + "]";
                                    replacestring = replacestring.PadRight(substring.Length, '*');

                                    presentline = presentline.Remove(firstopen, closepos - firstopen + 1);
                                    presentline = presentline.Insert(firstopen, replacestring);
                                }
                            }

                            closeindex++;
                            if (closeindex < closearray.Length)
                            {
                                closepos = closearray[closeindex].Index;
                            }
                            else
                            {
                                blockEnd = true;
                            }

                        }
                    }
                }

                // process main string
                // check for separators (OR or AND)

                string reststring = presentline;
                string condstring = String.Empty;
                string procstring = String.Empty;
                string tempstring = String.Empty;

                int seppos = CheckCondition(reststring);

                // process each part

                while (seppos > 0)
                {
                    procstring = reststring.Substring(0, seppos).Trim();
                    condstring = reststring.Substring(seppos, 2);
                    tempstring = reststring.Substring(seppos + 2);

                    bool validCondition = false;
                    while (!validCondition && tempstring.Length > 0 && tempstring[0] != ' ')
                    {
                        if (TranslateAndOr.ContainsKey(condstring))
                        {
                            validCondition = true;
                        }
                        else
                        {
                            condstring = String.Concat(condstring, tempstring.Substring(0, 1));
                            tempstring = tempstring.Substring(1);
                        }
                    }

                    reststring = tempstring.Trim();

                    // process separate !

                    if (procstring.Length > 0 && String.Compare(procstring.Substring(0, 1), "!") == 0)
                    {
                        SCRNegate negated = SCRNegate.NEGATE;
                        SCRConditionList.Add(negated);
                        procstring = procstring.Substring(1).Trim();
                    }

                    // process separate NOT

                    if (procstring.Length > 4 && String.Compare(procstring.Substring(0, 4), "NOT ") == 0)
                    {
                        SCRNegate negated = SCRNegate.NEGATE;
                        SCRConditionList.Add(negated);
                        procstring = procstring.Substring(4).Trim();
                    }

                    // previous separated substring - process as new full IF condition

                    if (procstring.StartsWith("["))
                    {
                        int entnum = procstring.IndexOf("]");
                        int subindex = Convert.ToInt32(procstring.Substring(1, entnum - 1));
                        scrReadInfo subinfo = new scrReadInfo(sublist[subindex - 1], GICInfo.Linenumber, GICInfo.Scriptname);
                        ArrayList SubCondition = getIfConditions(subinfo, LocalFloats, signalFunctions, ORNormalSubtypes);
                        SCRConditionList.Add(SubCondition);
                    }

                    // single condition

                    else
                    {
                        // remove any superflouos brackets ()
                        while (procstring.StartsWith("(") && procstring.EndsWith(")"))
                        {
                            procstring = procstring.Substring(1, procstring.Length - 2);
                        }

                        ThisCondition = new SCRConditions(procstring, LocalFloats, signalFunctions, ORNormalSubtypes, GICInfo.Linenumber);
                        SCRConditionList.Add(ThisCondition);
                    }

                    // translate logical operator

                    if (TranslateAndOr.TryGetValue(condstring, out condAndOr))
                    {
                        SCRConditionList.Add(condAndOr);
                    }
                    else
                    {
                        Trace.TraceWarning("sigscr-file line {1} : Invalid condition operator in : {0}", GICString, GICInfo.Linenumber.ToString());
                    }

                    seppos = CheckCondition(reststring);
                }

                // process last part or full part if no separators

                procstring = reststring;

                // process separate !

                if (procstring.Length > 0 && String.Compare(procstring.Substring(0, 1), "!") == 0)
                {
                    SCRNegate negated = SCRNegate.NEGATE;
                    SCRConditionList.Add(negated);
                    procstring = procstring.Substring(1).Trim();
                }

                if (procstring.Length > 4 && String.Compare(procstring.Substring(0, 4), "NOT ") == 0)
                {
                    SCRNegate negated = SCRNegate.NEGATE;
                    SCRConditionList.Add(negated);
                    procstring = procstring.Substring(4).Trim();
                }

                // previous separated substring - process as new full IF condition

                if (procstring.StartsWith("["))
                {
                    int entnum = procstring.IndexOf("]");
                    int subindex = Convert.ToInt32(procstring.Substring(1, entnum - 1));
                    scrReadInfo subinfo = new scrReadInfo(sublist[subindex - 1], GICInfo.Linenumber, GICInfo.Scriptname);
                    ArrayList SubCondition = getIfConditions(subinfo, LocalFloats, signalFunctions, ORNormalSubtypes);
                    SCRConditionList.Add(SubCondition);
                }

                // single condition

                else
                {

                    // remove any enclosing ()

                    if (procstring.StartsWith("("))
                    {
                        procstring = procstring.Substring(1, procstring.Length - 2).Trim();
                    }
                    ThisCondition = new SCRConditions(procstring, LocalFloats, signalFunctions, ORNormalSubtypes, GICInfo.Linenumber);
                    SCRConditionList.Add(ThisCondition);
                }

                // process logical operator if set

                if (!String.IsNullOrEmpty(condstring))
                {
                    if (TranslateAndOr.TryGetValue(condstring, out condAndOr))
                    {
                        SCRConditionList.Add(condAndOr);
                    }
                    else
                    {
                        Trace.TraceWarning("sigscr-file line {1} : Invalid condition operator in : {0}", GICString, GICInfo.Linenumber.ToString());
                    }
                }

                return SCRConditionList;
            }//getIfConditions

            //================================================================================================//
            //
            // check for condition in statement
            //
            //================================================================================================//

            public static int CheckCondition(String teststring)
            {
                char[] AndOrCheck = "|&".ToCharArray();
                int returnvalue = 0;

                returnvalue = teststring.IndexOfAny(AndOrCheck);
                if (returnvalue > 0)
                    return returnvalue;

                returnvalue = teststring.IndexOf(" AND ");
                if (returnvalue > 0)
                    return returnvalue + 1;

                returnvalue = teststring.IndexOf(" OR ");
                if (returnvalue > 0)
                    return returnvalue + 1;

                return returnvalue;
            }// CheckCondition

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
                public bool valid;
                public SCRTermType AssignType;
                public int AssignParameter;
                public List<SCRStatTerm> StatementTerms;

                public string[] StatementParts;
                public scrReadInfo StatementInfo;

                //================================================================================================//
                //
                //  Constructor
                //

                public SCRStatement(scrReadInfo Statement, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
                {

                    valid = true;
                    StatementInfo = new scrReadInfo(Statement.Readline, Statement.Linenumber, Statement.Scriptname);
                    string StatementLine = Statement.Readline;

                    // check for improper use of =# or ==#

                    int eqindex = StatementLine.IndexOf("=");
                    if (String.Compare(StatementLine.Substring(eqindex + 1, 2), "=#") == 0)
                    {
                        StatementLine = String.Concat(StatementLine.Substring(0, eqindex + 1),
                                        StatementLine.Substring(eqindex + 3));
                    }
                    else if (String.Compare(StatementLine.Substring(eqindex + 1, 1), "#") == 0)
                    {
                        StatementLine = String.Concat(StatementLine.Substring(0, eqindex + 1),
                                        StatementLine.Substring(eqindex + 2));
                    }
                    else if (String.Compare(StatementLine.Substring(eqindex + 1, 1), "=") == 0)
                    {
                        StatementLine = String.Concat(StatementLine.Substring(0, eqindex + 1),
                                        StatementLine.Substring(eqindex + 2));
                    }

                    //split on =, should be only 2 parts

                    StatementTerms = new List<SCRStatTerm>();
                    String TermPart;

                    StatementLine = StatementLine.Replace(";", String.Empty);

                    char[] splitChar = { '=' };
                    StatementParts = StatementLine.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
                    if (StatementParts.Length > 2)
                    {
                        valid = false;
                        Trace.TraceWarning("sigscr-file line {1} : Unexpected number of = in string : {0}", StatementLine, StatementInfo.Linenumber.ToString());
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt",
                                        "Unexpected number of = in string " + StatementLine + " (" + StatementInfo.Linenumber.ToString() + ")\n");
#endif
                    }

                    // Assignment part - search external and local floats
                    // if only 1 part, it is a single function call without assignment

                    AssignType = SCRTermType.Invalid;
                    AssignParameter = -1;    // preset assignparameter is not found

                    if (StatementParts.Length == 2)
                    {
                        string assignPart = StatementParts[0].Trim();
                        if (Enum.TryParse(assignPart, true, out SCRExternalFloats exFloat))  // try external float, e.g. BLOCK_STATE
                        {
                            AssignParameter = (int)exFloat;
                            AssignType = SCRTermType.ExternalFloat;
                        }

                        foreach (KeyValuePair<string, uint> intFloat in LocalFloats)
                        {
                            string intKey = intFloat.Key;
                            if (String.Compare(intKey, assignPart) == 0)
                            {
                                AssignType = SCRTermType.LocalFloat;
                                AssignParameter = (int)intFloat.Value;
                            }
                        }

                        // check if parameter has been defined
                        if (AssignParameter < 0)
                        {
                            Trace.TraceInformation("Local variable {0} not defined for script {1}", assignPart, Statement.Scriptname);
                            AssignParameter = 0;
                        }

                        // Term part
                        // get positions of allowed operators

                        TermPart = StatementParts[1].Trim();
                    }
                    else
                    {
                        // Term part
                        // get positions of allowed operators

                        TermPart = StatementParts[0].Trim();
                    }

                    // process term string

                    int sublevel = 0;
                    SCRProcess_TermPartLine(TermPart, ref sublevel, 0, LocalFloats, signalFunctions, ORNormalSubtypes, StatementInfo.Linenumber);

                    if (StatementTerms.Count <= 0) valid = false;
                }

                //================================================================================================//
                //
                //  Process Term part line
                //  May be called recursive to process substrings
                //

                public void SCRProcess_TermPartLine(string TermLinePart, ref int sublevel, int issublevel,
                            IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes, int linenumber)
                {

                    string keepString = TermLinePart;
                    string procString;
                    string operString;
                    bool syntaxerror = false;

                    string AllowedOperators = "[";

                    foreach (KeyValuePair<string, SCRTermOperator> PosOperator in TranslateOperator)
                    {
                        string ActOperator = PosOperator.Key;
                        if (String.Compare(ActOperator, "?") != 0)
                        {
                            AllowedOperators = String.Concat(AllowedOperators, ActOperator);
                        }
                    }

                    AllowedOperators = String.Concat(AllowedOperators, "]");
                    Regex operators = new Regex(AllowedOperators);
                    MatchCollection opertotal = operators.Matches(keepString);

                    int totalOper = opertotal.Count;
                    Match[] operPos = new Match[totalOper];
                    opertotal.CopyTo(operPos, 0);

                    // get position of closing and opening brackets

                    Regex openbrack = new Regex("[(]");
                    MatchCollection openbrackmatch = openbrack.Matches(keepString);
                    int totalOpenbrack = openbrackmatch.Count;
                    Match[] openbrackpos;

                    Regex closebrack = new Regex("[)]");
                    MatchCollection closebrackmatch = closebrack.Matches(keepString);
                    int totalClosebrack = closebrackmatch.Count;
                    Match[] closebrackpos;

                    if (totalClosebrack != totalOpenbrack)
                    {
                        Trace.TraceWarning("sigscr-file line {1} : Unmatching brackets in : {0}", keepString, StatementInfo.Linenumber.ToString());
                        keepString = String.Empty;
#if DEBUG_PRINT_IN
                        File.AppendAllText(din_fileLoc + @"sigscr.txt",
                                        "Unmatching brackets in : " + keepString + " (" + StatementInfo.Linenumber.ToString() + "\n");
#endif
                    }


                    // process each part - part is either separated by operator or enclosed within brackets

                    int nextoper = 0;
                    int nextopenbrack = 0;
                    int nextoperpos;
                    int nextbrackpos;

                    while (!String.IsNullOrEmpty(keepString) && !syntaxerror)
                    {

                        // if first chars is operator, copy it to operator string
                        // redetermine position of next operator

                        opertotal = operators.Matches(keepString);
                        totalOper = opertotal.Count;
                        operPos = new Match[totalOper];
                        opertotal.CopyTo(operPos, 0);
                        nextoper = 0;
                        nextoperpos = nextoper < operPos.Length ? operPos[nextoper].Index : keepString.Length + 1;

                        if (nextoperpos == 0)
                        {
                            operString = keepString.Substring(0, 1);
                            keepString = keepString.Substring(1).Trim();

                            opertotal = operators.Matches(keepString);
                            totalOper = opertotal.Count;
                            operPos = new Match[totalOper];
                            opertotal.CopyTo(operPos, 0);
                            nextoper = 0;
                            nextoperpos = nextoper < operPos.Length ? operPos[nextoper].Index : keepString.Length + 1;
                        }
                        else
                        {
                            operString = String.Empty;
                        }

                        // redetermine positions of operators and brackets

                        openbrackmatch = openbrack.Matches(keepString);
                        totalOpenbrack = openbrackmatch.Count;
                        openbrackpos = new Match[totalOpenbrack];
                        openbrackmatch.CopyTo(openbrackpos, 0);
                        nextopenbrack = 0;
                        nextbrackpos = nextopenbrack < openbrackpos.Length ?
                                openbrackpos[nextopenbrack].Index : keepString.Length + 1;

                        closebrackmatch = closebrack.Matches(keepString);
                        totalClosebrack = closebrackmatch.Count;
                        closebrackpos = new Match[totalClosebrack];
                        closebrackmatch.CopyTo(closebrackpos, 0);

                        // first is bracket, but not at start so is part of function call - ignore
                        // first is operator
                        // operator and bracket are equal - so neither are found
                        // normal term, so process

                        if ((nextbrackpos < nextoperpos && nextbrackpos > 0) || nextbrackpos >= nextoperpos)
                        {
                            if (nextoperpos < keepString.Length)
                            {
                                procString = keepString.Substring(0, nextoperpos).Trim();
                                keepString = keepString.Substring(nextoperpos).Trim();
                            }
                            else
                            {
                                procString = keepString;
                                keepString = String.Empty;
                            }

                            if (procString.IndexOf(")") > 0)
                            {
                                procString = procString.Replace(")", String.Empty).Trim();
                            }

                            if (String.IsNullOrEmpty(procString))
                            {
                                Trace.TraceWarning("sigscr-file line {1} : Invalid statement syntax : {0}", TermLinePart, linenumber.ToString());
                                syntaxerror = true;
                                StatementTerms.Clear();
                            }
                            else
                            {
                                SCRStatTerm thisTerm =
                                        new SCRStatTerm(procString, operString, sublevel, issublevel, LocalFloats, signalFunctions, ORNormalSubtypes, StatementInfo.Linenumber);
                                StatementTerms.Add(thisTerm);
                            }
                        }

                        // enclosed term - process as substring

                        else
                        {

                            // find matching end bracket

                            nextopenbrack++;
                            int brackcount = 1;
                            int nextclosebrack = 0;

                            int nextclosepos = closebrackpos[nextclosebrack].Index;
                            while (nextclosepos < nextbrackpos)
                            {
                                nextclosebrack++;
                                nextclosepos = closebrackpos[nextclosebrack].Index;
                            }
                            int lastclosepos = nextclosepos;

                            nextbrackpos =
                                  nextopenbrack < openbrackpos.Length ?
                                  openbrackpos[nextopenbrack].Index : keepString.Length + 1;

                            while (brackcount > 0)
                            {
                                if (nextbrackpos < nextclosepos)
                                {
                                    brackcount++;
                                    nextopenbrack++;
                                    nextbrackpos =
                                        nextopenbrack < openbrackpos.Length ?
                                        openbrackpos[nextopenbrack].Index : keepString.Length + 1;
                                }
                                else
                                {
                                    lastclosepos = nextclosepos;
                                    brackcount--;
                                    nextclosebrack++;
                                    nextclosepos =
                                        nextclosebrack < closebrackpos.Length ?
                                        closebrackpos[nextclosebrack].Index : keepString.Length + 1;
                                }
                            }

                            procString = keepString.Substring(1, lastclosepos - 1).Trim();
                            keepString = keepString.Substring(lastclosepos + 1).Trim();

                            // increase sublevel, set sublevel entry in statements

                            sublevel++;
                            SCRStatTerm thisTerm =
                                    new SCRStatTerm("*S*", operString, sublevel, issublevel, LocalFloats, signalFunctions, ORNormalSubtypes, StatementInfo.Linenumber);
                            StatementTerms.Add(thisTerm);

                            // process string as sublevel

                            int nextsublevel = sublevel;
                            SCRProcess_TermPartLine(procString, ref sublevel, nextsublevel, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);
                        }
                    }
                }//SCRProcess_TermPartLine

                //================================================================================================//

            }// class SCRStatement


            //================================================================================================//
            //
            // class SCRStatTerm
            //
            //================================================================================================//

            public class SCRStatTerm
            {
                public SCRExternalFunctions Function;
                public SCRParameterType[] PartParameter;
                public SCRTermOperator TermOperator;
                public bool negate;
                public int sublevel;
                public int issublevel;
                public int linenumber;

                //================================================================================================//
                //
                // Constructor
                //

                public SCRStatTerm(string StatementString, string StatementOperator, int sublevelIn, int issublevelIn,
                            IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes, int thisLine)
                {

                    linenumber = thisLine;

                    // check if statement starts with ! - if so , set negate

                    if (String.Compare(StatementString.Substring(0, 1), "!") == 0)
                    {
                        negate = true;
                        StatementString = StatementString.Substring(1).Trim();
                    }
                    else if (StatementString.Length >= 5 && String.Compare(StatementString.Substring(0, 4), "NOT ") == 0)
                    {
                        negate = true;
                        StatementString = StatementString.Substring(4).Trim();
                    }
                    else
                    {
                        negate = false;
                    }

                    sublevel = 0;

                    List<SCRParameterType> TempParameter = new List<SCRParameterType>();

                    // empty string - no parameter (can occur incase of allocation to negative number)

                    if (String.IsNullOrEmpty(StatementString))
                    {
                        TempParameter.Add(null);
                    }

                    // sublevel definition

                    else if (String.Compare(StatementString, "*S*") == 0)
                    {
                        sublevel = sublevelIn;
                    }

                    // if contains no brackets it is a fixed parameter

                    else if (StatementString.IndexOf("(") < 0)
                    {
                        if (String.Compare(StatementString, "RETURN") == 0)
                        {
                            Function = SCRExternalFunctions.RETURN;
                        }
                        else
                        {
                            Function = SCRExternalFunctions.NONE;

                            PartParameter = new SCRParameterType[1];
                            PartParameter[0] = process_TermPart(StatementString.Trim(), LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);

                            TranslateOperator.TryGetValue(StatementOperator, out TermOperator);
                        }
                    }


                    // function

                    else
                    {
                        ArrayList FunctionParts = process_FunctionCall(StatementString, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);

                        if (FunctionParts == null)
                        {
                            Function = SCRExternalFunctions.NONE;
                        }
                        else
                        {
                            Function = (SCRExternalFunctions)FunctionParts[0];

                            if (FunctionParts.Count > 1)
                            {
                                PartParameter = new SCRParameterType[FunctionParts.Count - 1];
                                for (int iparm = 1; iparm < FunctionParts.Count; iparm++)
                                {
                                    PartParameter[iparm - 1] = (SCRParameterType)FunctionParts[iparm];
                                }
                            }
                            else
                            {
                                PartParameter = null;
                            }
                        }
                    }

                    // process operator

                    if (!TranslateOperator.TryGetValue(StatementOperator, out TermOperator))
                    {
                        TermOperator = SCRTermOperator.NONE;
                    }

                    // issublevel

                    issublevel = issublevelIn;

                } // constructor
            } // class SCRStatTerm

            //================================================================================================//
            //
            // class SCRParameterType
            //
            //================================================================================================//

            public class SCRParameterType
            {
                public SCRTermType PartType;
                public int PartParameter;
                public SignalFunction SignalFunction;

                // <summary>
                // Constructor for generic parameter
                // </summary>
                public SCRParameterType(SCRTermType TypeIn, int IntIn)
                {
                    PartType = TypeIn;
                    PartParameter = IntIn;
                    if (Enum.IsDefined(typeof(MstsSignalFunction), IntIn)) SignalFunction = new SignalFunction((MstsSignalFunction)IntIn);
                }

                // <summary>
                // Constructor for signal function parameter
                // </summary>
                public SCRParameterType(SCRTermType TypeIn, SignalFunction signalFunction)
                {
                    PartType = TypeIn;
                    PartParameter = -1;
                    SignalFunction = signalFunction;
                }
            }

            //================================================================================================//
            //
            // class SCRConditionBlock
            //
            //================================================================================================//

            public class SCRConditionBlock
            {
                public ArrayList Conditions;
                public SCRBlock IfBlock;
                public List<SCRBlock> ElseIfBlock;
                public SCRBlock ElseBlock;

                //================================================================================================//
                //
                // Constructor
                // Input is the array of indices pointing to the lines following the IF - ELSEIF - IF blocks
                //

                public SCRConditionBlock(List<scrReadInfo> CBLScriptLines, int index, List<int> endindex, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
                {

                    scrReadInfo thisinfo, tempinfo;

                    // process conditions

                    Conditions = getIfConditions(CBLScriptLines[index], LocalFloats, signalFunctions, ORNormalSubtypes);

                    // process IF block

                    int iflines = endindex[0] - index - 1;
                    List<scrReadInfo> IfSubBlock = new List<scrReadInfo>();

                    for (int iline = 0; iline < iflines; iline++)
                    {
                        IfSubBlock.Add(CBLScriptLines[iline + index + 1]);
                    }

                    IfBlock = new SCRBlock(IfSubBlock, LocalFloats, signalFunctions, ORNormalSubtypes);
                    ElseIfBlock = null;
                    ElseBlock = null;

                    // process all ELSE blocks if available

                    int blockindex = 0;
                    int elseindex = endindex[blockindex];
                    blockindex++;

                    while (blockindex < endindex.Count)
                    {
                        int elselines = endindex[blockindex] - elseindex;

                        List<scrReadInfo> ElseSubBlock = new List<scrReadInfo>();

                        // process ELSEIF block
                        // delete ELSE to process as IF block

                        if (CBLScriptLines[elseindex].Readline.StartsWith("ELSEIF"))
                        {
                            thisinfo = CBLScriptLines[elseindex];
                            tempinfo = new scrReadInfo(thisinfo.Readline.Substring(4), thisinfo.Linenumber, thisinfo.Scriptname); // set start of line to IF
                            ElseSubBlock.Add(tempinfo);

                            for (int iline = 1; iline < elselines; iline++)
                            {
                                ElseSubBlock.Add(CBLScriptLines[iline + elseindex]);
                            }
                            SCRBlock TempBlock = new SCRBlock(ElseSubBlock, LocalFloats, signalFunctions, ORNormalSubtypes);
                            if (ElseIfBlock == null)
                            {
                                ElseIfBlock = new List<SCRBlock>();
                            }
                            ElseIfBlock.Add(TempBlock);
                            elseindex = endindex[blockindex];
                            blockindex++;
                        }

                        // process ELSE block

                        else
                        {
                            for (int iline = 1; iline < elselines; iline++)
                            {
                                ElseSubBlock.Add(CBLScriptLines[iline + elseindex]);
                            }
                            ElseBlock = new SCRBlock(ElseSubBlock, LocalFloats, signalFunctions, ORNormalSubtypes);
                            blockindex++;
                        }

                        ElseSubBlock.Clear();

                    }
                } // constructor
            } // class SCRConditionBlock

            //================================================================================================//
            //
            // class SCRConditions
            //
            //================================================================================================//

            public class SCRConditions
            {
                public SCRStatTerm Term1;
                public bool negate1;
                public SCRStatTerm Term2;
                public bool negate2;
                public SCRTermCondition Condition;
                int linenumber;

                //================================================================================================//
                //
                //  Constructor
                //

                public SCRConditions(string TermString, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes, int thisLine)
                {

                    string firststring, secondstring;
                    string separator;
                    string TempString = TermString;

                    linenumber = thisLine;

                    // check on !, if not followed by = then it is a NOT, replace by ^ to ease processing

                    Regex NotSeps = new Regex("!");

                    MatchCollection NotSepCount = NotSeps.Matches(TempString);
                    int totalNot = NotSepCount.Count;
                    Match[] NotSeparray = new Match[totalNot];
                    NotSepCount.CopyTo(NotSeparray, 0);

                    for (int inot = 0; inot < totalNot; inot++)
                    {
                        int notpos = NotSeparray[inot].Index;
                        if (String.Compare(TempString.Substring(notpos, 2), "!=") != 0)
                        {
                            TempString = String.Concat(TempString.Substring(0, notpos), "^", TempString.Substring(notpos + 1));
                        }
                    }

                    // search for separators

                    Regex CondSeps = new Regex("[<>!=]");

                    MatchCollection CondSepCount = CondSeps.Matches(TempString);
                    int totalSeps = CondSepCount.Count;
                    Match[] CondSeparray = new Match[totalSeps];
                    CondSepCount.CopyTo(CondSeparray, 0);

                    // split on separator

                    if (totalSeps == 0)
                    {
                        firststring = TempString.Trim();
                        secondstring = String.Empty;
                        separator = String.Empty;
                    }
                    else
                    {
                        firststring = TempString.Substring(0, CondSeparray[0].Index).Trim();
                        secondstring = TempString.Substring(CondSeparray[0].Index + 1).Trim();
                        separator = TempString.Substring(CondSeparray[0].Index, 1);
                    }

                    // first string
                    // check for ^ (as replacement for !) as starting character

                    negate1 = false;
                    if (firststring.StartsWith("^"))
                    {
                        negate1 = true;
                        firststring = firststring.Substring(1).Trim();
                    }

                    Term1 = new SCRStatTerm(firststring, String.Empty, 0, 0, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);

                    // second string (if it exists)
                    // check of first char, if =, add this to separator
                    // check on next char, if #, remove
                    // check for ^ (as replacement for !) as next character

                    negate2 = false;
                    if (String.IsNullOrEmpty(secondstring))
                    {
                        Term2 = null;
                    }
                    else
                    {
                        if (secondstring.StartsWith("="))
                        {
                            separator = String.Concat(separator, "=");
                            secondstring = secondstring.Substring(1).Trim();
                        }

                        if (secondstring.StartsWith("#"))
                        {
                            secondstring = secondstring.Substring(1).Trim();
                        }

                        if (secondstring.StartsWith("^"))
                        {
                            negate2 = true;
                            secondstring = secondstring.Substring(1).Trim();
                        }

                        Term2 = new SCRStatTerm(secondstring, String.Empty, 0, 0, LocalFloats, signalFunctions, ORNormalSubtypes, linenumber);
                    }

                    if (!String.IsNullOrEmpty(separator))
                    {
                        SCRTermCondition setcond;
                        if (TranslateConditions.TryGetValue(separator, out setcond))
                        {
                            Condition = setcond;
                        }
                        else
                        {
                            Trace.TraceWarning("sigscr-file line {1} : Invalid condition operator in : {0}", TermString, linenumber.ToString());
#if DEBUG_PRINT_IN
                            File.AppendAllText(din_fileLoc + @"sigscr.txt",
                                            "Invalid condition operator in : " + TermString + "\n"); ;
#endif
                        }
                    }
                } // constructor
            } // class SCRConditions

            //================================================================================================//
            //
            // class SCRBlock
            //
            //================================================================================================//

            public class SCRBlock
            {
                public ArrayList Statements;

                //================================================================================================//
                //
                //  Constructor
                //

                public SCRBlock(List<scrReadInfo> BlockStrings, IDictionary<string, uint> LocalFloats, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORNormalSubtypes)
                {
                    Statements = new ArrayList();
                    Statements = processScriptLines(BlockStrings, 0, LocalFloats, signalFunctions, ORNormalSubtypes);
                } // constructor
            } // class SCRBlock
        } // class Scripts
    }
}
