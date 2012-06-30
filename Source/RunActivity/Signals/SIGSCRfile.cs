
/// COPYRIGHT 2011 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


// Author : Rob Roeterdink
//
//
// This file processes the MSTS SIGSCR.dat file, which contains the signal logic.
// The information is stored in a series of classes.
// This file also contains the functions to process the information when running, and as such is linked with signals.cs
//
// Debug flags :
// #define DEBUG_PRINT_IN
// prints details of the file as read from input
//
// #define DEBUG_PRINT_OUT
// prints details of the file as processed
//
// #define DEBUG_PRINT_PROCESS
// prints processing details
// set TBD_debug_ref to TDB index of required signals
//
// #define DEBUG_PRINT_ENABLED
// prints processing details of all enabled signals
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Popups;

namespace ORTS
{

  //================================================================================================//
  //
  // class scrfile
  //
  //================================================================================================//

	public class SIGSCRfile
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
			DIST_MULTI_SIG_MR,
			SIG_FEATURE,
			DEF_DRAW_STATE,
			DEBUG_HEADER,
			DEBUG_OUT,
		}

		public enum SCRExternalFloats
		{
			STATE,
			DRAW_STATE,
			ENABLED,
			BLOCK_STATE,
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
			MULTIPLY,
			PLUS,
			MINUS,
			DIVIDE,
			MODULO,
		}

		public enum SCRTermType
		{
			ExternalFloat,
			LocalFloat,
			Sigasp,
			Sigfn,
			Sigfeat,
			Block,
			Constant,
			Invalid,
		}

		private static IDictionary <string, SCRTermCondition> TranslateConditions;
		private static IDictionary <string, SCRTermOperator> TranslateOperator;
		private static IDictionary <string, SCRAndOr> TranslateAndOr;

#if DEBUG_PRINT_IN
		public static string din_fileLoc = String.Empty;    /* file path for debug files */
 //		public static string din_fileLoc = @"C:\temp\";     /* file path for debug files */
#endif

#if DEBUG_PRINT_OUT
		public static string dout_fileLoc = String.Empty;   /* file path for debug files */
 //		public static string dout_fileLoc = @"C:\temp\";    /* file path for debug files */
#endif

#if DEBUG_PRINT_PROCESS
		public static int [] TDB_debug_ref;                 /* signal TDB idents         */
 //		public static string dpr_fileLoc = String.Empty;    /* file path for debug files */
 		public static string dpr_fileLoc = @"C:\temp\";     /* file path for debug files */
#endif
#if DEBUG_PRINT_ENABLED
 //		public static string dpe_fileLoc = String.Empty;    /* file path for debug files */
 		public static string dpe_fileLoc = @"C:\temp\";     /* file path for debug files */
#endif

		public IDictionary <SignalType, SCRScripts> Scripts;
		private static string keepLine = String.Empty;

  //================================================================================================//
  // local data
  //================================================================================================//


  //================================================================================================//
  ///
  /// Constructor
  ///

		public SIGSCRfile(string RoutePath, IList<string> ScriptFiles, IDictionary <string, SignalType> SignalTypes)
		{

  // Create required translators

			Scripts = new Dictionary <SignalType, SCRScripts> ();
			TranslateConditions = new Dictionary <string, SCRTermCondition> ();
			TranslateConditions.Add(">" ,SCRTermCondition.GT);
			TranslateConditions.Add(">=",SCRTermCondition.GE);
			TranslateConditions.Add("<" ,SCRTermCondition.LT);
			TranslateConditions.Add("<=",SCRTermCondition.LE);
			TranslateConditions.Add("==",SCRTermCondition.EQ);
			TranslateConditions.Add("!=",SCRTermCondition.NE);
			TranslateConditions.Add("::",SCRTermCondition.NE);  // dummy (for no separator)

			TranslateAndOr = new Dictionary <string, SCRAndOr> ();
			TranslateAndOr.Add("&&",SCRAndOr.AND);
			TranslateAndOr.Add("||",SCRAndOr.OR);
			TranslateAndOr.Add("??",SCRAndOr.NONE);

			TranslateOperator = new Dictionary <string, SCRTermOperator> ();
			TranslateOperator.Add("?",SCRTermOperator.NONE);
			TranslateOperator.Add("*",SCRTermOperator.MULTIPLY);
			TranslateOperator.Add("+",SCRTermOperator.PLUS);
			TranslateOperator.Add("/",SCRTermOperator.DIVIDE);
			TranslateOperator.Add("%",SCRTermOperator.MODULO);


#if DEBUG_PRINT_PROCESS
			TDB_debug_ref = new int[10] {344,7526,6799,6792,6791,6790,6795,7067,7523,6784};   /* signal tdb ref.no selected for print-out */
#endif

#if DEBUG_PRINT_IN
            File.Delete(din_fileLoc+@"sigscr.txt");
#endif

#if DEBUG_PRINT_OUT
            File.Delete(dout_fileLoc+@"scriptproc.txt");
#endif

#if DEBUG_PRINT_ENABLED
            File.Delete(dpe_fileLoc+@"printproc.txt");
#endif
#if DEBUG_PRINT_PROCESS
            File.Delete(dpr_fileLoc+@"printproc.txt");
#endif

  // Process all files listed in SIGCFG

			foreach(string FileName in ScriptFiles)
			{
				string fullName = String.Concat(RoutePath,@"\",FileName);
				try
				{
					using (StreamReader scrStream = new StreamReader(fullName, true))
					{
#if DEBUG_PRINT_IN
						File.AppendAllText(din_fileLoc+@"sigscr.txt","Reading file : "+fullName+"\n\n");
#endif
						sigscrRead(scrStream, SignalTypes);
						scrStream.Close();
					}
				}
				catch (Exception error)
				{
                    Trace.TraceInformation(FileName);
                    Trace.WriteLine(error);
				}
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

		public void sigscrRead(StreamReader scrStream, IDictionary <string, SignalType> SignalTypes)
		{
			string readLine;
			bool   ScriptFound = false;
			string scriptname = String.Empty;
			SignalType thisType;
			List <string> ScriptLines = new List<string> ();

			readLine = scrReadLine(scrStream);

  // search for first SCRIPT - skip lines until first script found

			while (readLine != null && !ScriptFound)
			{

				if (readLine.StartsWith("SCRIPT "))
				{
					ScriptFound = true;
					scriptname = readLine.Substring(7);
				}
				else
				{
					readLine = scrReadLine(scrStream);
				}
			}

  // process SCRIPT

			while (readLine != null)
			{
				readLine = scrReadLine(scrStream);
				if (readLine != null)
				{

  // new SCRIPT line - process stored lines

					if (readLine.StartsWith("SCRIPT "))
					{
#if DEBUG_PRINT_IN
						File.AppendAllText(din_fileLoc+@"sigscr.txt","\n===============================\n");
						File.AppendAllText(din_fileLoc+@"sigscr.txt","\nNew Script : "+scriptname+"\n");
#endif
#if DEBUG_PRINT_OUT
						File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\n===============================\n");
						File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nNew Script : "+scriptname+"\n");
#endif
						SCRScripts newScript = new SCRScripts(ScriptLines, scriptname);

						if (SignalTypes.TryGetValue(scriptname.ToLower().Trim(), out thisType))
						{
							if (Scripts.ContainsKey(thisType))
							{
								Trace.TraceWarning("Multiple definition of signaltype {0}",scriptname);
							}
							else
							{
#if DEBUG_PRINT_IN
								File.AppendAllText(din_fileLoc+@"sigscr.txt","Adding script : "+thisType.Name+"\n");
#endif
								Scripts.Add(thisType, newScript);
							}
						}
						else
						{
#if DEBUG_PRINT_OUT
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
									"\nUnknown signal type : "+scriptname+"\n\n");
#endif
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt","\nUnknown signal type : "+scriptname+"\n\n");
#endif
						}

						scriptname = readLine.Substring(7);
					}

  // new REM SCRIPT line - process stored lines, skip until new SCRIPT found

					else if (readLine.StartsWith("REM SCRIPT "))
					{
						SCRScripts newScript = new SCRScripts(ScriptLines, scriptname);

						if (SignalTypes.TryGetValue(scriptname.ToLower(), out thisType))
						{
							if (Scripts.ContainsKey(thisType))
							{
								Trace.TraceWarning("Multiple definition of signaltype {0}",scriptname);
							}
							else
							{
								Scripts.Add(thisType, newScript);
							}
						}
						else
						{
							Trace.TraceWarning("Unknown signal type : {0}", scriptname);
						}

						while (!readLine.StartsWith("SCRIPT ") && readLine != null)
						{
							readLine = scrReadLine(scrStream);     // Skip
						}
						scriptname = readLine.Substring(7);
					}

  // store line

					else
					{
						ScriptLines.Add(readLine);
					}
				}
			}

  // process last SCRIPT if any

			if (ScriptLines.Count > 0)
			{
#if DEBUG_PRINT_IN
				File.AppendAllText(din_fileLoc+@"sigscr.txt","\n===============================\n");
				File.AppendAllText(din_fileLoc+@"sigscr.txt","\nNew Script : "+scriptname+"\n");
#endif
				SCRScripts newScript = new SCRScripts(ScriptLines, scriptname);
				if (SignalTypes.TryGetValue(scriptname.ToLower().Trim(), out thisType))
				{
					if (Scripts.ContainsKey(thisType))
					{
						Trace.TraceWarning("Multiple definition of signaltype {0}",scriptname);
					}
					else
					{
						Scripts.Add(thisType, newScript);
					}
				}
				else
				{
					Trace.TraceWarning("Unknown signal type : {0}", scriptname);
				}
			}

#if DEBUG_PRINT_OUT

  // print processed details

			foreach ( KeyValuePair <SignalType, SCRScripts> thispair in Scripts)
			{

				SCRScripts thisscript = thispair.Value;

				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","Script : "+thisscript.scriptname+"\n\n");
				printscript(thisscript.Statements);
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\n=====================\n");
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
			List <int> Sublevels = new List <int> ();

			foreach (object scriptstat in Statements)
			{

  // process statement lines

				if (scriptstat is SCRScripts.SCRStatement)
				{
					SCRScripts.SCRStatement ThisStat = (SCRScripts.SCRStatement) scriptstat;
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","Statement : \n");
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
							ThisStat.AssignType.ToString()+"["+ThisStat.AssignParameter.ToString()+"] = ");

					foreach (SCRScripts.SCRStatTerm ThisTerm in ThisStat.StatementTerms)
					{
						if (ThisTerm.issublevel > 0)
						{
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
									" <SUB"+ThisTerm.issublevel.ToString()+"> ");
						}
						function = false;
						if (ThisTerm.Function != SCRExternalFunctions.NONE)
						{
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
								ThisTerm.Function.ToString()+"(");
							function = true;
						}

						if (ThisTerm.PartParameter != null)
						{
							foreach (SCRScripts.SCRParameterType ThisParam in ThisTerm.PartParameter)
							{
								File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
									ThisParam.PartType+"["+ThisParam.PartParameter+"] ,");
							}
						}

						if (ThisTerm.sublevel != 0)
						{
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt"," SUBTERM_"+ThisTerm.sublevel.ToString());
						}

						if (function)
						{
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt",")");
						}
						File.AppendAllText(dout_fileLoc+@"scriptproc.txt"," -"+ThisTerm.TermOperator.ToString()+"- \n");
					}

					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\n\n");
				}

  // process conditions line

				if (scriptstat is SCRScripts.SCRConditionBlock)
				{
					SCRScripts.SCRConditionBlock CondBlock = (SCRScripts.SCRConditionBlock) scriptstat;
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nCondition : \n");

					printConditionArray(CondBlock.Conditions);

					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nIF Block : \n");
					printscript(CondBlock.IfBlock.Statements);

					if (CondBlock.ElseIfBlock != null)
					{
						foreach ( SCRScripts.SCRBlock TempBlock in CondBlock.ElseIfBlock)
						{
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nStatements in ELSEIF : "+
								TempBlock.Statements.Count+"\n");
							File.AppendAllText(dout_fileLoc+@"scriptproc.txt","Elseif Block : \n");
							printscript(TempBlock.Statements);
						}
					}

					if (CondBlock.ElseBlock != null)
					{
						File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nElse Block : \n");
						printscript(CondBlock.ElseBlock.Statements);
					}

					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\nEnd IF Block : \n");

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
			foreach(object ThisCond in Conditions)
			{
				if (ThisCond is SCRScripts.SCRConditions)
				{
					printcondition((SCRScripts.SCRConditions) ThisCond);
				}
				else if (ThisCond is SCRAndOr)
				{
					SCRAndOr condstring = (SCRAndOr) ThisCond;
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt",condstring.ToString()+"\n");
				}
				else if (ThisCond is SCRNegate)
				{
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","NEGATED : \n");
				}
				else
				{
					printConditionArray( (ArrayList) ThisCond);
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
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","NOT : ");
			}
			if (ThisCond.Term1.Function != SCRExternalFunctions.NONE)
			{
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt",ThisCond.Term1.Function.ToString()+"(");
				function = true;
			}

			if (ThisCond.Term1.PartParameter != null)
			{
				foreach (SCRScripts.SCRParameterType ThisParam in ThisCond.Term1.PartParameter)
				{
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt",ThisParam.PartType+"["+ThisParam.PartParameter+"] ,");
				}
			}
			else
			{
 				File.AppendAllText(dout_fileLoc+@"scriptproc.txt"," 0 , ");
			}

			if (function)
			{
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt",")");
			}

			File.AppendAllText(dout_fileLoc+@"scriptproc.txt"," -- "+ThisCond.Condition.ToString()+" --\n");

			if (ThisCond.Term2 != null)
			{
				function = false;
				if (ThisCond.negate2)
				{
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","NOT : ");
				}
				if (ThisCond.Term2.Function != SCRExternalFunctions.NONE)
				{
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt",ThisCond.Term2.Function.ToString()+"(");
					function = true;
				}

				if (ThisCond.Term2.PartParameter != null)
				{
					foreach (SCRScripts.SCRParameterType ThisParam in ThisCond.Term2.PartParameter)
					{
						File.AppendAllText(dout_fileLoc+@"scriptproc.txt",
								ThisParam.PartType+"["+ThisParam.PartParameter+"] ,");
					}
				}
				else
				{
 					File.AppendAllText(dout_fileLoc+@"scriptproc.txt"," 0 , ");
				}

				if (function)
				{
					File.AppendAllText(dout_fileLoc+@"scriptproc.txt",")");
				}
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\n");
			}
		}// printcondition
#endif

  //================================================================================================//
  //
  // read single line from file
  // skip comment and empty lines
  //
  //================================================================================================//

		public string scrReadLine(StreamReader scrStream)
		{
			string readLine;
			string procLine = String.Empty;
			bool validLine = false;
			bool compart = false;

  // check if anything still in store

			if (String.IsNullOrEmpty(keepLine))
			{
				readLine = scrStream.ReadLine();
#if DEBUG_PRINT_IN
				File.AppendAllText(din_fileLoc+@"sigfile.txt","From file : "+readLine+"\n");
#endif

			}
			else
			{
				readLine = String.Copy(keepLine);
				keepLine = String.Empty;
#if DEBUG_PRINT_IN
				File.AppendAllText(din_fileLoc+@"sigfile.txt","From store : "+readLine+"\n");
#endif
			}	

  // loop until valid line found

			while (readLine != null && !validLine)
			{

  // remove comment

				if (compart)
				{
					procLine= String.Concat(@"/*",readLine.ToUpper());  // force as comment
				}
				else
				{
					procLine = readLine.ToUpper();
				}

				procLine = procLine.Replace("\t"," ");
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
					procLine = procLine.Substring(0,comsep).Trim();
				}

				if (addsep == 0)
				{
					compart = (endsep <= 0); // No end comment
					procLine= String.Empty;
				}
					
  // check if empty, else read next line

				if (procLine.Length > 0)
				{
					validLine = true;
				}
				else
				{
					readLine=scrStream.ReadLine();
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigfile.txt","Invalid line, next from file : "+readLine+"\n");
#endif
				}
			}

  // if ';' in string, split there and keep rest

			char [] sepCheck = ";{}".ToCharArray();
			int seppos = procLine.IndexOfAny(sepCheck);
#if DEBUG_PRINT_IN
			File.AppendAllText(din_fileLoc+@"sigfile.txt","Extracted : "+procLine+"\n");
#endif
			if (seppos >= 0)
			{
				if (String.Compare(procLine.Substring(seppos,1),";") == 0)
				{
					keepLine = procLine.Substring(seppos+1);
					procLine = procLine.Substring(0,seppos+1);
				}
				else if (seppos == 0)
				{
					keepLine = procLine.Substring(seppos+1);
					procLine = procLine.Substring(0,1);
				}
				else
				{
					keepLine = procLine.Substring(seppos);
					procLine = procLine.Substring(0,seppos);
				}
#if DEBUG_PRINT_IN
				File.AppendAllText(din_fileLoc+@"sigfile.txt","To store : "+keepLine+"\n");
#endif
			}

  // if "IF(" in string, replace with "IF ("

			int ifbrack = procLine.IndexOf("IF(");
			if (ifbrack >= 0)
			{
			    procLine=procLine.Substring(0,ifbrack)+"IF ("+procLine.Substring(ifbrack+3);
			}

  // return line or null

			if (readLine == null)
			{
				return null;
			}
			else
			{
#if DEBUG_PRINT_IN
				File.AppendAllText(din_fileLoc+@"sigfile.txt","To process : "+procLine+"\n");
#endif
				return procLine;
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

			private IDictionary <string, uint> LocalFloats;
			public uint totalLocalFloats;
			public ArrayList Statements;
			public string scriptname;

  //================================================================================================//
  //
  // Constructor
  // Input is list with all lines for one signal script
  //

			public SCRScripts (List<string> ScriptLines, string scriptnameIn)
			{
				LocalFloats = new Dictionary <string, uint> ();
				totalLocalFloats = 0;
				Statements = new ArrayList();

				int lcount = 0;
				int maxcount = ScriptLines.Count;

				scriptname = scriptnameIn;

#if DEBUG_PRINT_IN
  // print inputlines

				foreach(string Line in ScriptLines)
				{
					File.AppendAllText(din_fileLoc+@"sigscr.txt",Line+"\n");
				}
				File.AppendAllText(din_fileLoc+@"sigscr.txt","\n+++++++++++++++++++++++++++++++++++\n\n");

#endif

  // Skip external floats (exist automatically)

				bool exfloat = ScriptLines[lcount].StartsWith("EXTERN FLOAT ");
				while (exfloat && lcount < maxcount)
				{
					lcount++;
					exfloat = ScriptLines[lcount].StartsWith("EXTERN FLOAT ");
				}

  // Process floats : build list with internal floats

				bool infloat = ScriptLines[lcount].StartsWith("FLOAT ");
				while (infloat && lcount < maxcount)
				{
					string floatstring = ScriptLines[lcount].Substring(6);
					floatstring = floatstring.Trim();
					int endstring = floatstring.IndexOf(";");
					floatstring = floatstring.Substring(0,endstring);

					if (!LocalFloats.ContainsKey(floatstring))
					{
						LocalFloats.Add(floatstring,totalLocalFloats);
						totalLocalFloats++;
					}

					lcount++;
					infloat = ScriptLines[lcount].StartsWith("FLOAT ");
				}

#if DEBUG_PRINT_OUT
  // print details of internal floats

				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","\n\nFloats : \n");
				foreach( KeyValuePair <string, uint> deffloat in LocalFloats)
				{
					string defstring = deffloat.Key;
					uint defindex = deffloat.Value;

					File.AppendAllText(dout_fileLoc+@"scriptproc.txt","Float : "+defstring+" = "+defindex.ToString()+"\n");
				}
				File.AppendAllText(dout_fileLoc+@"scriptproc.txt","Total : "+totalLocalFloats.ToString()+"\n\n\n");
#endif

  // Check rest of file - statements

				Statements = processScriptLines(ScriptLines, lcount, LocalFloats);
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

			public static ArrayList processScriptLines(List <string> PSLScriptLines, int index, IDictionary <string, uint> LocalFloats)
			{

				int lcount = index;
				int nextcount;
				List <int> ifblockcount;
				ArrayList localStatements = new ArrayList();

  // loop through all lines

				while (lcount < PSLScriptLines.Count)
				{

  // clear enclosing { and } if still in string

					int sepparent = PSLScriptLines[lcount].IndexOf("{");
					while (sepparent >= 0)
					{
						PSLScriptLines[lcount] = PSLScriptLines[lcount].Replace("{",String.Empty).Trim();
						sepparent = PSLScriptLines[lcount].IndexOf("{");
					}
						
					sepparent = PSLScriptLines[lcount].IndexOf("}");
					while (sepparent >= 0)
					{
						PSLScriptLines[lcount] = PSLScriptLines[lcount].Replace("}",String.Empty).Trim();
						sepparent = PSLScriptLines[lcount].IndexOf("}");
					}

  // process IF statement
  // all lines in IF (-ELSEIF) (-ELSE)  block will be handled by this function call

					if (PSLScriptLines[lcount].StartsWith("IF "))
					{
						ifblockcount = findEndIfBlock(PSLScriptLines, lcount);
						SCRConditionBlock thisCondition = new SCRConditionBlock(PSLScriptLines, lcount, ifblockcount, LocalFloats);
						nextcount = ifblockcount[ifblockcount.Count-1];
						localStatements.Add(thisCondition);
					}

  // process statement

					else if (!String.IsNullOrEmpty(PSLScriptLines[lcount]))
					{
						nextcount = FindEndStatement(PSLScriptLines,lcount);
						SCRStatement thisStatement = new SCRStatement(PSLScriptLines[lcount], LocalFloats);
						localStatements.Add(thisStatement);
					}

  // empty line (may be result of removing { and })

					else
					{
						nextcount = lcount + 1;
					}

					lcount=nextcount;
				}

				return localStatements;
			} // processScriptlines

  //================================================================================================//
  //
  // Find end of IF condition statement
  // returns index to next line
  //
  //================================================================================================//

			public static int FindEndStatement(List <string> FESScriptLines, int index)
			{
				string presentstring, addline;
				int endpos;
				int actindex;

  //================================================================================================//

				presentstring = FESScriptLines[index].Trim();
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
					addline = FESScriptLines[actindex];
					FESScriptLines.RemoveAt(actindex);
					presentstring = String.Concat(presentstring,addline);
					endpos = presentstring.IndexOf(";");
				}

  // Illegal statement - no ;

				if (endpos <= 0)
				{
                    Trace.TraceWarning("Missing ; in statement starting with {0}\n", presentstring);
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","Missing ; in statement starting with "+presentstring+"\n");
#endif

				}

  // split string at ; if anything follows after

				if (presentstring.Length > (endpos+1)  && endpos > 0)
				{
					FESScriptLines.Insert(index,presentstring.Substring(endpos+1).Trim());
					presentstring = presentstring.Substring(0,endpos+1).Trim();
				}

				FESScriptLines.Insert(index,presentstring.Trim());
				actindex = index+1;
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

			public static List <int> findEndIfBlock(List <string> FEIScriptLines, int index)
			{

				List <int> nextcount = new List <int> ();
				string nextline;
				int endIfcount, endElsecount;

				int linecount = FindEndCondition(FEIScriptLines, index);

				nextline = FEIScriptLines[linecount].Trim();

  // full block : search for matching parenthesis in next lines
  // set end after related closing }

				endIfcount = linecount;

				if (String.Compare(nextline.Substring(0,1),"{") == 0)
				{	
					endIfcount = findEndBlock(FEIScriptLines, linecount);
				}

  // next statement is another if : insert { and } to ease processing

				else if (String.Compare(nextline.Substring(0,Math.Min(3,nextline.Length)),"IF ") == 0)
				{	
					List <int> fullcount = findEndIfBlock(FEIScriptLines,linecount);
					int lastline = fullcount[fullcount.Count - 1];
					string templine = FEIScriptLines[linecount];
					FEIScriptLines.RemoveAt(linecount);
					templine = String.Concat("{ ",templine);
					FEIScriptLines.Insert(linecount,templine);
					templine = FEIScriptLines[lastline-1];
					FEIScriptLines.RemoveAt(lastline-1);
					templine = String.Concat(templine," }");
					FEIScriptLines.Insert(lastline-1,templine);
					endIfcount = lastline;
				}

  // single statement - set end after statement

				else
				{
					endIfcount = FindEndStatement(FEIScriptLines, linecount);
				}
				nextcount.Add(endIfcount);

				endElsecount=endIfcount;

  // check if next line starts with ELSE or any form of ELSEIF

				nextline = endElsecount < FEIScriptLines.Count ? FEIScriptLines[endElsecount].Trim() : String.Empty;
				bool endelse  = false;

				while (!endelse && endElsecount < FEIScriptLines.Count)
				{
					bool elsepart = false;

  // line contains ELSE only

					if (nextline.Length <= 4)
					{
						if (String.Compare(nextline,"ELSE") == 0)
						{
							elsepart = true;
							nextline = FEIScriptLines[endElsecount+1];

  // check if next line start with IF - then this is an ELSEIF

							if (nextline.StartsWith("IF "))
							{
								nextline = String.Concat("ELSEIF ",nextline.Substring(3).Trim());
								FEIScriptLines.RemoveAt(endElsecount+1);
								FEIScriptLines.RemoveAt(endElsecount);
								FEIScriptLines.Insert(endElsecount,nextline);
								endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
								nextline     = FEIScriptLines[endElsecount];
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

					else if (String.Compare(nextline.Substring(0,Math.Min(5,nextline.Length)),"ELSE ") == 0)
					{
						elsepart = true;
						nextline = nextline.Substring(5).Trim();
						if (nextline.StartsWith("IF "))
						{
							nextline = String.Concat("ELSEIF ",nextline.Substring(3).Trim());
							FEIScriptLines.RemoveAt(endElsecount);
							FEIScriptLines.Insert(endElsecount,nextline);
							endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
							nextline     = FEIScriptLines[endElsecount];
						}
						else
						{
							endelse = true;

							FEIScriptLines.RemoveAt(endElsecount);
							FEIScriptLines.Insert(endElsecount,nextline.Trim());
							FEIScriptLines.Insert(endElsecount,"ELSE");
							endElsecount++;
						}
					}

  // line starts with ELSEIF 

					else if (String.Compare(nextline.Substring(0,Math.Min(7,nextline.Length)),"ELSEIF ") == 0)
					{
						elsepart = true;
						endElsecount = FindEndCondition(FEIScriptLines, endElsecount);
						nextline     = FEIScriptLines[endElsecount];
					}

  // line starts with ELSE{
  // store ELSE on separate new line

					else if ( String.Compare(nextline.Substring(0,Math.Min(5,nextline.Length)),"ELSE{") == 0)
					{
						elsepart = true;
						endelse  = true;
						nextline = nextline.Substring(5).Trim();
						FEIScriptLines.RemoveAt(endElsecount);
						FEIScriptLines.Insert(endElsecount,nextline);
						nextline = "{";
						FEIScriptLines.Insert(endElsecount,"{");
						FEIScriptLines.Insert(endElsecount,"ELSE");
					}

  // if an ELSE or ELSEIF part is found - find end 

					if (elsepart)
					{
						if (String.Compare(nextline.Substring(0,1),"{") == 0)
						{
							endElsecount = findEndBlock(FEIScriptLines, endElsecount);
						}
						else
						{
							endElsecount = FindEndStatement(FEIScriptLines, endElsecount);
						}
						nextline = endElsecount < FEIScriptLines.Count ? FEIScriptLines[endElsecount].Trim() : String.Empty;
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
 
			public static int findEndBlock(List <string> FEBScriptLines, int index)
			{

  // Use regular expression to find all occurences of { and }
  // Keep searching through next lines until match is found

				int openparent = 0;
				int closeparent= 0;

				int openindex = 0;
				int closeindex= 0;

				Regex openparstr = new Regex ("{");
				Regex closeparstr= new Regex ("}");

				string presentline = FEBScriptLines[index];

				bool blockEnd = false;
				int splitpoint= -1;
				int checkcount= index;

  // get positions in present line

				MatchCollection opencount = openparstr.Matches(presentline);
				MatchCollection closecount= closeparstr.Matches(presentline);

  // convert to ARRAY

				int totalopen = opencount.Count;
				int totalclose= closecount.Count;

				Match [] closearray = new Match[totalclose];
				closecount.CopyTo(closearray, 0);
				Match [] openarray = new Match[totalopen];
				opencount.CopyTo(openarray, 0);

  // search until match found
				while (!blockEnd)
				{

  // get next position (continue from previous index)

					int openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
					int closepos= closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;

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
							closepos= closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;
						}
					}

  // openpos and closepos equal - both have reached end of line - get next line

					else
					{
						checkcount++;
						presentline = FEBScriptLines[checkcount];

  // get positions

						opencount = openparstr.Matches(presentline);
						closecount= closeparstr.Matches(presentline);

						totalopen = opencount.Count;
						totalclose= closecount.Count;

  // convert to array

						closearray = new Match[totalclose];
						closecount.CopyTo(closearray, 0);
						openarray = new Match[totalopen];
						opencount.CopyTo(openarray, 0);

						openindex = 0;
						closeindex= 0;

  // get next positions

						openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
						closepos= closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;
					}
				}

  // end found - check if anything follows final }

				int nextcount = checkcount + 1;
				presentline = FEBScriptLines[checkcount].Trim();

				if (splitpoint >= 0 && splitpoint < presentline.Length-1)
				{
					presentline = FEBScriptLines[checkcount];
					FEBScriptLines.RemoveAt(checkcount);

					FEBScriptLines.Insert(checkcount, presentline.Substring(splitpoint+1).Trim());
					FEBScriptLines.Insert(checkcount, presentline.Substring(0,splitpoint+1).Trim());
				}

				return nextcount;
			}//findEndBlock

  //================================================================================================//
  //
  // find end of IF condition statement
  //
  //================================================================================================//

			public static int FindEndCondition(List <string> FECScriptLines, int index)
			{
				string presentstring, addline;
				int totalopen, totalclose;
				int actindex;

  //================================================================================================//

				actindex   = index;
        
				presentstring = FECScriptLines[index];
				FECScriptLines.RemoveAt(index);

  // use regular expression to search for open and close bracket

				Regex openbrack = new Regex (@"\(");
				Regex closebrack= new Regex (@"\)");

  // search for open bracket

				MatchCollection opencount = openbrack.Matches(presentstring);
				totalopen = opencount.Count;

  // add lines until open bracket found

				while (totalopen <= 0 && actindex < FECScriptLines.Count)
				{
					addline = FECScriptLines[actindex];
					FECScriptLines.RemoveAt(actindex);
					presentstring = String.Concat(presentstring, addline);
					opencount = openbrack.Matches(presentstring);
					totalopen = opencount.Count;
				}

				if (totalopen <= 0)
				{
                    Trace.TraceWarning("If statement without ( ; starting with {0}\n", presentstring);
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","If statement without ( ; starting with {0}"+presentstring+"\n");
#endif
				}

  // in total string, search for close brackets

				MatchCollection closecount= closebrack.Matches(presentstring);
				totalclose= closecount.Count;

  // keep adding lines until open and close brackets match

				while (totalclose < totalopen && actindex < FECScriptLines.Count)
				{
					addline = FECScriptLines[actindex];
					FECScriptLines.RemoveAt(actindex);
					presentstring = String.Concat(presentstring, addline);

					opencount = openbrack.Matches(presentstring);
					totalopen = opencount.Count;
					closecount= closebrack.Matches(presentstring);
					totalclose= closecount.Count;
				}

				if (totalclose < totalopen)
				{
                    Trace.TraceWarning("Missing ) in IF statement ; starting with {0} : {1} and {2}", presentstring,
					totalopen.ToString(), totalclose.ToString());
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","If statement without ) ; starting with "+presentstring+
						" : "+totalopen.ToString()+" and "+totalclose.ToString()+"\n");
#endif
				}

  // get position of final close bracket - end of condition statement

				Match [] closearray = new Match[totalclose];
				closecount.CopyTo(closearray, 0);
				Match [] openarray = new Match[totalopen];
				opencount.CopyTo(openarray, 0);

  // match open and close brackets - when matched, that is end of condition

				int actbracks = 1;
				int actpos    = openarray[0].Index;

				int actopen = 1;
				int openpos = actopen < openarray.Length ? openarray[actopen].Index : presentstring.Length+1;

				int actclose= 0;
				int closepos= closearray[actclose].Index;

				while (actbracks > 0)
				{
					if (openpos < closepos)
					{
						actbracks++;
						actopen++;
						openpos = actopen < openarray.Length ? openarray[actopen].Index : presentstring.Length+1;
					}
					else
					{
						actbracks--;
						if (actbracks > 0)
						{
							actclose++;
							closepos = actclose < closearray.Length ? closearray[actclose].Index : presentstring.Length+1;
						}
					}
				}

  // split on end of condition

				if (closepos < (presentstring.Length-1))
				{
					FECScriptLines.Insert(index,presentstring.Substring(closepos+1).Trim());
					presentstring = presentstring.Substring(0,closepos+1);
				}
				FECScriptLines.Insert(index,presentstring.Trim());
				actindex = index+1;
				return actindex;
			}//findEndCondition

  //================================================================================================//
  //
  // process function call (in statement or in IF condition)
  //
  //================================================================================================//

			static public ArrayList process_FunctionCall (string FunctionStatement, IDictionary <string, uint> LocalFloats)
			{
				ArrayList FunctionParts = new ArrayList ();
				bool  valid_func = true;

  // split in function and parameter parts

				string[] StatementParts = FunctionStatement.Split('(');
				if (StatementParts.Length > 2)
				{
					valid_func = false;
					Trace.TraceWarning("Unexpected number of ( in function call : {0}",FunctionStatement);
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","Unexpected number of ( in function call : "+FunctionStatement+"\n");
#endif
				}

  // process function part

				try
				{
					SCRExternalFunctions exFunction =
					       	(SCRExternalFunctions)Enum.Parse(typeof(SCRExternalFunctions), StatementParts[0], true);
					FunctionParts.Add(exFunction);

				}
				catch (Exception ex)
				{
					valid_func = false;
					Trace.TraceWarning("Unknown function call : {0} : {1}",FunctionStatement, ex.ToString());
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown function call : "+FunctionStatement+"\n");
#endif
				}

  // remove closing bracket

				string ParameterPart = StatementParts[1].Replace(")",String.Empty).Trim();

  // process first parameters in case of multiple parameters

				int sepindex = ParameterPart.IndexOf(",");
				while (sepindex > 0 && valid_func)
				{
					string parmPart = ParameterPart.Substring(0,sepindex).Trim();
					SCRParameterType TempParm = process_TermPart( parmPart, LocalFloats);
					FunctionParts.Add(TempParm); 

					ParameterPart = ParameterPart.Substring(sepindex+1).Trim();
					sepindex = ParameterPart.IndexOf(",");
				}

  // process last or only parameter if set

				if ( !String.IsNullOrEmpty(ParameterPart) && valid_func)
				{
					SCRParameterType TempParm = process_TermPart( ParameterPart, LocalFloats);
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
 
			static public SCRParameterType process_TermPart(string TermString, IDictionary <string, uint> LocalFloats)
			{

				bool termset = false;
				SCRParameterType TermParts = new SCRParameterType (SCRTermType.Constant, 0);

  // check for use of #
				if (String.Compare(TermString.Substring(0,1),"#") == 0)
				{
					TermString = TermString.Substring(1).Trim();
				}


  // try constant

				try
				{
					int tmpint = int.Parse(TermString);
					TermParts = new SCRParameterType(SCRTermType.Constant, tmpint);
					termset = true;
				}
				catch (Exception Ex)
				{
					if (TermString.Length < 1) Trace.Write(Ex.ToString());   // dummy statement to avoid compiler warning
				}

  // try external float

				if (!termset)
				{
					try
					{
						SCRExternalFloats exFloat =
						       	(SCRExternalFloats)Enum.Parse(typeof(SCRExternalFloats), TermString, true);
						TermParts = new SCRParameterType(SCRTermType.ExternalFloat, (int) exFloat);
						termset = true;
					}
					catch (Exception Ex)
					{
						if (TermString.Length < 1) Trace.Write(Ex.ToString());   // dummy statement to avoid compiler warning
					}
				}

  // try local float

				if (!termset)
				{
					foreach (KeyValuePair <string, uint> intFloat in LocalFloats)
					{
						string intFloatName = intFloat.Key;
						uint intFloatDef    = intFloat.Value;

						if (String.Compare(TermString, intFloatName) == 0)
						{
							TermParts = new SCRParameterType(SCRTermType.LocalFloat, (int) intFloatDef);
							termset = true;
						}
					}
				}

  // try blockstate

				if (!termset)
				{
					if (TermString.StartsWith("BLOCK_"))
					{
						string partString = TermString.Substring(6);
						try
						{
							SignalObject.BLOCKSTATE Blockstate =
							       	(SignalObject.BLOCKSTATE)Enum.Parse(typeof(SignalObject.BLOCKSTATE), partString, true);
							TermParts = new SCRParameterType(SCRTermType.Block, (int) Blockstate);
						}
						catch (Exception Ex)
						{
							Trace.TraceWarning("Unknown Blockstate : {0} : {1}", partString, Ex.ToString() );
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown Blockstate : "+partString+"\n");
#endif
						}
						termset = true;
					}
				}


  // try SIGASP definition

				if (!termset)
				{
					if (TermString.StartsWith("SIGASP_"))
					{
						string partString = TermString.Substring(7);
						try
						{
							SignalHead.SIGASP Aspect =
							       	(SignalHead.SIGASP)Enum.Parse(typeof(SignalHead.SIGASP), partString, true);
							TermParts = new SCRParameterType(SCRTermType.Sigasp, (int) Aspect);
						}
						catch (Exception Ex)
						{
							Trace.TraceWarning("Unknown Aspect : {0} : {1}", partString, Ex.ToString() );
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown Aspect : "+partString+"\n");
#endif
						}
						termset = true;
					}
				}

  // try SIGFN definition

				if (!termset)
				{
					if (TermString.StartsWith("SIGFN_"))
					{
						string partString = TermString.Substring(6);
						try
						{
							SignalHead.SIGFN Type =
							        (SignalHead.SIGFN)Enum.Parse(typeof(SignalHead.SIGFN), partString, true);
							TermParts = new SCRParameterType(SCRTermType.Sigfn, (int) Type);
						}
						catch (Exception Ex)
						{
							Trace.TraceWarning("Unknown Type : {0} : {1}", partString, Ex.ToString() );
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown Type : "+partString+"\n");
#endif
						}
						termset = true;
					}
				}

  // try SIGFEAT definition

				if (!termset)
				{
					if (TermString.StartsWith("SIGFEAT_"))
					{
						string partString = TermString.Substring(8);
						try
						{
							int sfIndex = MSTS.SignalShape.SignalSubObj.SignalSubTypes.IndexOf(partString);
							TermParts = new SCRParameterType(SCRTermType.Sigfeat, sfIndex);
						}
						catch (Exception Ex)
						{
							Trace.TraceWarning("Unknown SubType : {0} : {1}", partString, Ex.ToString() );
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown SubType : "+partString+"\n");
#endif
						}
						termset = true;
					}
				}

  // nothing found - set error

				if (!termset)
				{
					Trace.TraceWarning("Unknown parameter in statement : {0}",TermString);
#if DEBUG_PRINT_IN
					File.AppendAllText(din_fileLoc+@"sigscr.txt","Unknown parameter : "+TermString+"\n");
#endif
				}

				return TermParts;
			}//process_TermPart

  //================================================================================================//
  //
  // process IF condition line - split into logic parts
  //
  //================================================================================================//

			public static ArrayList getIfConditions (string GICString, IDictionary <string, uint> LocalFloats)
			{
				SCRConditions ThisCondition;
				ArrayList SCRConditionList = new ArrayList ();

				SCRAndOr condAndOr;
				List <string> sublist = new List <string> ();

  // extract condition between first ( and last )

				int startpos = GICString.IndexOf("(");
				int endpos   = GICString.LastIndexOf(")");
				string presentline = GICString.Substring(startpos+1, endpos-startpos-1).Trim();

  // search for substrings
  // search for matching brackets

				Regex openparstr = new Regex ("[(]");
				Regex closeparstr= new Regex ("[)]");
				char [] AndOrCheck = "|&".ToCharArray();

  // get all brackets in string

				MatchCollection opencount = openparstr.Matches(presentline);
				MatchCollection closecount= closeparstr.Matches(presentline);

				int totalopen = opencount.Count;
				int totalclose= closecount.Count;

				if (totalopen > 0)
				{

  // convert matches to array

					Match [] closearray = new Match[totalclose];
					closecount.CopyTo(closearray, 0);
					Match [] openarray = new Match[totalopen];
					opencount.CopyTo(openarray, 0);

  // get positions, find ) which matches first (

					bool blockEnd = false;
					int bracklevel= 0;

					int openindex = 0;
					int closeindex= 0;

					int openpos = openindex < openarray.Length ? openarray[openindex].Index : presentline.Length;
					int closepos= closeindex < closearray.Length ? closearray[closeindex].Index : presentline.Length;

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
								string substring = presentline.Substring(firstopen,closepos-firstopen+1);
								if (substring.IndexOfAny(AndOrCheck) > 0)
								{
									sublist.Add(substring);
									string replacestring = "["+sublist.Count.ToString()+"]";
									replacestring = replacestring.PadRight(substring.Length, '*' );
									
									presentline = presentline.Remove(firstopen,closepos-firstopen+1);
									presentline = presentline.Insert(firstopen,replacestring);
								}
							}

							closeindex++;
							if (closeindex < closearray.Length)
							{
								closepos= closearray[closeindex].Index;
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

				int seppos = reststring.IndexOfAny(AndOrCheck);

  // process each part

				while (seppos > 0)
				{
					procstring = reststring.Substring(0, seppos).Trim();
					condstring = reststring.Substring(seppos,2);
					reststring = reststring.Substring(seppos+2).Trim();

  // process separate !

					if (procstring.Length > 0 && String.Compare(procstring.Substring(0,1),"!") == 0)
					{
						SCRNegate negated = SCRNegate.NEGATE;
						SCRConditionList.Add(negated);
						procstring = procstring.Substring(1).Trim();
					}

  // previous separated substring - process as new full IF condition

					if (procstring.StartsWith("["))
					{
						int entnum = procstring.IndexOf("]");
						int subindex = Convert.ToInt32(procstring.Substring(1,entnum-1));
						ArrayList SubCondition = getIfConditions(sublist[subindex-1], LocalFloats);
						SCRConditionList.Add(SubCondition);
					}

  // single condition

					else
					{

  // replace any enclosing ()

						if (procstring.StartsWith("("))
						{
							procstring=procstring.Substring(1,procstring.Length-2);
						}
						ThisCondition = new SCRConditions(procstring, LocalFloats);
						SCRConditionList.Add(ThisCondition);
					}

  // translate logical operator

					if (TranslateAndOr.TryGetValue(condstring, out condAndOr))
					{
						SCRConditionList.Add(condAndOr);
					}
					else
					{
						Trace.TraceWarning("Invalid condition operator in : {0}",GICString);
					}

					seppos = reststring.IndexOfAny(AndOrCheck);
				}

  // process last part or full part if no separators

				procstring = reststring;

  // process separate !

				if (procstring.Length > 0 && String.Compare(procstring.Substring(0,1),"!") == 0)
				{
					SCRNegate negated = SCRNegate.NEGATE;
					SCRConditionList.Add(negated);
					procstring = procstring.Substring(1).Trim();
				}

  // previous separated substring - process as new full IF condition

				if (procstring.StartsWith("["))
				{
					int entnum = procstring.IndexOf("]");
					int subindex = Convert.ToInt32(procstring.Substring(1,entnum-1));
					ArrayList SubCondition = getIfConditions(sublist[subindex-1], LocalFloats);
					SCRConditionList.Add(SubCondition);
				}

  // single condition

				else
				{

  // remove any enclosing ()

					if (procstring.StartsWith("("))
					{
						procstring=procstring.Substring(1,procstring.Length-2).Trim();
					}
					ThisCondition = new SCRConditions(procstring, LocalFloats);
					SCRConditionList.Add(ThisCondition);
				}

  // process logical operator if set

				if (! String.IsNullOrEmpty(condstring))
				{
					if (TranslateAndOr.TryGetValue(condstring, out condAndOr))
					{
						SCRConditionList.Add(condAndOr);
					}
					else
					{
						Trace.TraceWarning("Invalid condition operator in : {0}",GICString);
					}
				}

				return SCRConditionList;
			}//getIfConditions


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
				public SCRTermType AssignType;
				public int AssignParameter;
				public List<SCRStatTerm> StatementTerms;

				public string[] StatementParts;

  //================================================================================================//
  //
  //  Constructor
  //

				public SCRStatement(string StatementLine, IDictionary<string, uint> LocalFloats)
				{

  // check for improper use of =# or ==#

					int eqindex = StatementLine.IndexOf("=");
					if (String.Compare(StatementLine.Substring(eqindex+1,2),"=#") == 0)
					{
						StatementLine = String.Concat(StatementLine.Substring(0,eqindex+1),
										StatementLine.Substring(eqindex+3));
					}
					else if (String.Compare(StatementLine.Substring(eqindex+1,1),"#") == 0)
					{
						StatementLine = String.Concat(StatementLine.Substring(0,eqindex+1),
										StatementLine.Substring(eqindex+2));
					}
					else if (String.Compare(StatementLine.Substring(eqindex+1,1),"=") == 0)
					{
						StatementLine = String.Concat(StatementLine.Substring(0,eqindex+1),
										StatementLine.Substring(eqindex+2));
					}

  //split on =, should be only 2 parts

					StatementTerms = new List<SCRStatTerm>();
					String TermPart;

					StatementLine = StatementLine.Replace(";", String.Empty);

                    char[] splitChar = {'='};
					StatementParts = StatementLine.Split(splitChar,StringSplitOptions.RemoveEmptyEntries);
					if (StatementParts.Length > 2)
					{
                        Trace.TraceWarning("Unexpected number of = in string : {0}", StatementLine);
#if DEBUG_PRINT_IN
						File.AppendAllText(din_fileLoc+@"sigscr.txt","Unexpected number of = in string "+StatementLine+"\n");
#endif
					}

  // Assignment part - search external and local floats
  // if only 1 part, it is a single function call without assignment

					AssignType = SCRTermType.Invalid;

					if (StatementParts.Length == 2)
					{
						string assignPart = StatementParts[0].Trim();
						try
						{

							SCRExternalFloats exFloat =
							       	(SCRExternalFloats)Enum.Parse(typeof(SCRExternalFloats), assignPart, true);
							AssignParameter = (int)exFloat;
							AssignType = SCRTermType.ExternalFloat;
						}
						catch (Exception Ex)
						{
							if (StatementLine.Length < 1) Trace.Write(Ex.ToString());   // dummy statement to avoid compiler warning
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
					SCRProcess_TermPartLine(TermPart, ref sublevel, 0, LocalFloats);
				}

  //================================================================================================//
  //
  //  Process Term part line
  //  May be called recursive to process substrings
  //

				public void SCRProcess_TermPartLine(string TermLinePart, ref int sublevel, int issublevel,
					       	IDictionary<string, uint> LocalFloats)
				{

					string keepString = String.Copy(TermLinePart);
					string procString;
					string operString;

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
					Match [] operPos = new Match[totalOper];
					opertotal.CopyTo(operPos, 0);

  // get position of closing and opening brackets

					Regex openbrack = new Regex("[(]");
					MatchCollection openbrackmatch = openbrack.Matches(keepString);
					int totalOpenbrack = openbrackmatch.Count;
					Match [] openbrackpos;

					Regex closebrack = new Regex("[)]");
					MatchCollection closebrackmatch = closebrack.Matches(keepString);
					int totalClosebrack = closebrackmatch.Count;
					Match [] closebrackpos;

					if (totalClosebrack != totalOpenbrack)
					{
						Trace.TraceWarning("Unmatching brackets in : {0}", keepString);
						keepString = String.Empty;
#if DEBUG_PRINT_IN
						File.AppendAllText(din_fileLoc+@"sigscr.txt","Unmatching brackets in : "+keepString+"\n");;
#endif
					}


  // process each part - part is either separated by operator or enclosed within brackets

					int nextoper = 0;
					int nextopenbrack = 0;
					int nextoperpos;
					int nextbrackpos;

					while (!String.IsNullOrEmpty(keepString))
					{

  // if first chars is operator, copy it to operator string
  // redetermine position of next operator

						opertotal = operators.Matches(keepString);
						totalOper = opertotal.Count;
						operPos = new Match[totalOper];
						opertotal.CopyTo(operPos, 0);
						nextoper = 0;
						nextoperpos = nextoper < operPos.Length ? operPos[nextoper].Index : keepString.Length+1;
	
						if (nextoperpos == 0)
						{
							operString = keepString.Substring(0,1);
							keepString = keepString.Substring(1).Trim();

							opertotal = operators.Matches(keepString);
							totalOper = opertotal.Count;
							operPos = new Match[totalOper];
							opertotal.CopyTo(operPos, 0);
							nextoper = 0;
							nextoperpos = nextoper < operPos.Length ? operPos[nextoper].Index : keepString.Length+1;
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
						nextbrackpos  = nextopenbrack < openbrackpos.Length ?
						       	openbrackpos[nextopenbrack].Index : keepString.Length+1;

						closebrackmatch = closebrack.Matches(keepString);
						totalClosebrack = closebrackmatch.Count;
						closebrackpos = new Match[totalClosebrack];
						closebrackmatch.CopyTo(closebrackpos, 0);

  // first is bracket, but not at start so is part of function call - ignore
  // first is operator
  // operator and bracket are equal - so neither are found
  // normal term, so process

						if ( (nextbrackpos < nextoperpos && nextbrackpos > 0) || nextbrackpos >= nextoperpos)
						{
							if (nextoperpos < keepString.Length)
							{
								procString = keepString.Substring(0,nextoperpos).Trim();
								keepString = keepString.Substring(nextoperpos).Trim();
							}
							else
							{
								procString = String.Copy(keepString);
								keepString = String.Empty;
							}
							SCRStatTerm thisTerm =
						       		new SCRStatTerm(procString, operString, sublevel, issublevel, LocalFloats);
							StatementTerms.Add(thisTerm);
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
							       	openbrackpos[nextopenbrack].Index : keepString.Length+1;

							while (brackcount > 0)
							{
								if (nextbrackpos < nextclosepos)
								{
									brackcount++;
									nextopenbrack++;
                                                        		nextbrackpos =
							       			nextopenbrack < openbrackpos.Length ?
									       	openbrackpos[nextopenbrack].Index : keepString.Length+1;
								}
								else
								{
									lastclosepos = nextclosepos;
									brackcount--;
									nextclosebrack++;
									nextclosepos =
										nextclosebrack < closebrackpos.Length ?
										closebrackpos[nextclosebrack].Index : keepString.Length+1;
								}
							}

							procString = keepString.Substring(1,lastclosepos-1).Trim();
							keepString = keepString.Substring(lastclosepos+1).Trim();

  // increase sublevel, set sublevel entry in statements

							sublevel++;
							SCRStatTerm thisTerm =
						       		new SCRStatTerm("*S*", operString, sublevel, issublevel, LocalFloats);
							StatementTerms.Add(thisTerm);

  // process string as sublevel

							int nextsublevel = sublevel;
							SCRProcess_TermPartLine(procString, ref sublevel, nextsublevel, LocalFloats);
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

  //================================================================================================//
  //
  // Constructor
  //

				public SCRStatTerm(string StatementString, string StatementOperator, int sublevelIn, int issublevelIn,
					       	IDictionary<string, uint> LocalFloats)
				{

  // check if statement starts with ! - if so , set negate

					if (String.Compare(StatementString.Substring(0,1),"!") == 0)
					{
						negate = true;
						StatementString = StatementString.Substring(1).Trim();
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

					else if (String.Compare(StatementString,"*S*") == 0)
					{
						sublevel = sublevelIn;
					}

  // if contains no brackets it is a fixed parameter

					else if (StatementString.IndexOf("(") < 0)
					{
						Function = SCRExternalFunctions.NONE;

						PartParameter = new SCRParameterType[1];
						PartParameter[0] = process_TermPart(StatementString.Trim(), LocalFloats);

						TranslateOperator.TryGetValue(StatementOperator, out TermOperator);
					}

  // function

					else
					{
						ArrayList FunctionParts = process_FunctionCall(StatementString, LocalFloats);

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

  //================================================================================================//
  //
  // Constructor
  //

				public SCRParameterType (SCRTermType TypeIn, int IntIn)
				{
					PartType = TypeIn;
					PartParameter = IntIn;
				} // constructor
			} // class SCRParameterType

  //================================================================================================//
  //
  // class SCRConditionBlock
  //
  //================================================================================================//

			public class SCRConditionBlock
			{
  				public ArrayList       Conditions;
				public SCRBlock        IfBlock;
				public List <SCRBlock> ElseIfBlock;
				public SCRBlock        ElseBlock;

  //================================================================================================//
  //
  // Constructor
  // Input is the array of indices pointing to the lines following the IF - ELSEIF - IF blocks
  //

				public SCRConditionBlock (List <string> CBLScriptLines, int index, List <int> endindex, IDictionary <string, uint> LocalFloats)
				{

  // process conditions

                                        Conditions = getIfConditions(CBLScriptLines[index],LocalFloats);

  // process IF block

					int iflines = endindex[0] - index - 1;
					List <string> IfSubBlock = new List <string> ();

					for (int iline=0; iline < iflines; iline++)
					{
						IfSubBlock.Add(CBLScriptLines[iline+index+1]);
					}

					IfBlock = new SCRBlock(IfSubBlock, LocalFloats);
					ElseIfBlock = null;
					ElseBlock   = null;

  // process all ELSE blocks if available

					int blockindex = 0;
					int elseindex = endindex[blockindex];
					blockindex++;

					while (blockindex < endindex.Count)
					{
						int elselines = endindex[blockindex] - elseindex;

						List <string> ElseSubBlock = new List <string> ();

  // process ELSEIF block
  // delete ELSE to process as IF block

						if (CBLScriptLines[elseindex].StartsWith("ELSEIF"))
						{
							ElseSubBlock.Add(CBLScriptLines[elseindex].Substring(4));	// set start of line to IF
							for (int iline=1; iline < elselines; iline++)
							{
								ElseSubBlock.Add(CBLScriptLines[iline+elseindex]);
							}
							SCRBlock TempBlock = new SCRBlock(ElseSubBlock, LocalFloats);
							if (ElseIfBlock == null)
							{
								ElseIfBlock = new List <SCRBlock> ();
							}
							ElseIfBlock.Add(TempBlock);
							elseindex = endindex[blockindex];
							blockindex++;
						}

  // process ELSE block

						else
						{
							for (int iline=1; iline < elselines; iline++)
							{
								ElseSubBlock.Add(CBLScriptLines[iline+elseindex]);
							}
							ElseBlock = new SCRBlock(ElseSubBlock, LocalFloats);
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
				public SCRStatTerm      Term1;
				public bool             negate1;
				public SCRStatTerm      Term2;
				public bool             negate2;
				public SCRTermCondition Condition;

  //================================================================================================//
  //
  //  Constructor
  //

				public SCRConditions(string TermString, IDictionary <string, uint> LocalFloats)
				{

					string firststring, secondstring;
					string separator;
					string TempString = TermString;

  // check on !, if not followed by = then it is a NOT, replace by ^ to ease processing

					Regex NotSeps = new Regex("!");

					MatchCollection NotSepCount = NotSeps.Matches(TempString);
					int totalNot = NotSepCount.Count;
					Match [] NotSeparray = new Match[totalNot];
					NotSepCount.CopyTo(NotSeparray, 0);

					for (int inot=0; inot < totalNot; inot++)
					{
						int notpos = NotSeparray[inot].Index;
						if (String.Compare(TempString.Substring(notpos,2),"!=") != 0)
						{
							TempString = String.Concat(TempString.Substring(0,notpos),"^",TempString.Substring(notpos+1));
						}
					}

  // search for separators

					Regex CondSeps = new Regex("[<>!=]");

					MatchCollection CondSepCount = CondSeps.Matches(TempString);
					int totalSeps = CondSepCount.Count;
					Match [] CondSeparray = new Match[totalSeps];
					CondSepCount.CopyTo(CondSeparray, 0);

  // split on separator

					if (totalSeps == 0)
					{
						firststring = TempString.Trim();
						secondstring= String.Empty;
						separator   = String.Empty;
					}
					else
					{
						firststring = TempString.Substring(0,CondSeparray[0].Index).Trim();
						secondstring= TempString.Substring(CondSeparray[0].Index+1).Trim();
						separator   = TempString.Substring(CondSeparray[0].Index,1);
					}

  // first string
  // check for ^ (as replacement for !) as starting character

					negate1 = false;
					if (firststring.StartsWith("^"))
					{
						negate1 = true;
						firststring = firststring.Substring(1).Trim();
					}

					Term1 = new SCRStatTerm(firststring, String.Empty, 0, 0, LocalFloats);

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
							separator = String.Concat(separator,"=");
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

						Term2 = new SCRStatTerm(secondstring, String.Empty, 0, 0, LocalFloats);
					}

					if (! String.IsNullOrEmpty(separator))
					{
						SCRTermCondition setcond;
						if (TranslateConditions.TryGetValue(separator, out setcond))
						{
							Condition = setcond;
						}
						else
						{
							Trace.TraceWarning("Invalid condition operator in : {0}",TermString);
#if DEBUG_PRINT_IN
							File.AppendAllText(din_fileLoc+@"sigscr.txt",
									"Invalid condition operator in : "+TermString+"\n");;
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

				public SCRBlock(List <string> BlockStrings, IDictionary <string, uint> LocalFloats)
				{
					Statements = new ArrayList();
					Statements = processScriptLines(BlockStrings, 0, LocalFloats);
				} // constructor
			} // class SCRBlock
		} // class Scripts

  //================================================================================================//
  //
  // processing routines
  //
  //================================================================================================//
  //
  // main update routine
  //
  //================================================================================================//

		public static void SH_update( SignalHead thisHead, SIGSCRfile sigscr )
		{

			SCRScripts signalScript;
			if (thisHead.signalType == null) return;
			if (sigscr.Scripts.TryGetValue(thisHead.signalType, out signalScript))
			{
				sigscr.SH_process_script(thisHead, signalScript, sigscr);
			}
			else
			{
    				sigscr.SH_update_basic(thisHead);
			}
		}

  //================================================================================================//
  //
  // update_basic : update signal without script
  //
  //================================================================================================//

		public void SH_update_basic( SignalHead thisHead)
		{
			if (thisHead.mainSignal.block_state() == SignalObject.BLOCKSTATE.CLEAR)
			{
				thisHead.SetLeastRestrictiveAspect();
			}
			else
			{
				thisHead.SetMostRestrictiveAspect();
			}
		}

  //================================================================================================//
  //
  // process script
  //
  //================================================================================================//

		public void SH_process_script( SignalHead thisHead, SCRScripts signalScript, SIGSCRfile sigscr)
		{

			int [] localFloats = new int [signalScript.totalLocalFloats];

  // process script

#if DEBUG_PRINT_ENABLED
			if (thisHead.mainSignal.enabledTrain != null)
			{
				File.AppendAllText(dpe_fileLoc+@"printproc.txt","\n\nSIGNAL : "+thisHead.TDBIndex.ToString()+"\n");
				File.AppendAllText(dpe_fileLoc+@"printproc.txt","OBJECT : "+thisHead.mainSignal.thisRef.ToString()+"\n");
				File.AppendAllText(dpe_fileLoc+@"printproc.txt","type   : "+signalScript.scriptname+"\n");
			}
#endif
#if DEBUG_PRINT_PROCESS
			if (TDB_debug_ref.Contains(thisHead.TDBIndex))
			{
				File.AppendAllText(dpr_fileLoc+@"printproc.txt","\n\nSIGNAL : "+thisHead.TDBIndex.ToString()+"\n");
				File.AppendAllText(dpr_fileLoc+@"printproc.txt","OBJECT : "+thisHead.mainSignal.thisRef.ToString()+"\n");
				File.AppendAllText(dpr_fileLoc+@"printproc.txt","type   : "+signalScript.scriptname+"\n");
			}
#endif

			SH_process_StatementBlock(thisHead, signalScript.Statements, localFloats, sigscr);

#if DEBUG_PRINT_ENABLED
			if (thisHead.mainSignal.enabledTrain != null)
			{
				File.AppendAllText(dpe_fileLoc+@"printproc.txt","\n ------- \n");
			}
#endif
#if DEBUG_PRINT_PROCESS
			if (TDB_debug_ref.Contains(thisHead.TDBIndex))
			{
				File.AppendAllText(dpr_fileLoc+@"printproc.txt","\n ------- \n");
			}
#endif

		}
  
  //================================================================================================//
  //
  // process statement block
  // called for full script as well as for IF and ELSE blocks
  //
  //================================================================================================//

		public void SH_process_StatementBlock( SignalHead thisHead, ArrayList Statements,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

  // loop through all lines

			foreach (object scriptstat in Statements)
			{

  // process statement lines

				if (scriptstat is SCRScripts.SCRStatement)
				{

					SCRScripts.SCRStatement ThisStat = (SCRScripts.SCRStatement) scriptstat;
					SH_processAssignStatement(thisHead, ThisStat, localFloats, sigscr);

#if DEBUG_PRINT_ENABLED
					if (thisHead.mainSignal.enabledTrain != null)
					{
						File.AppendAllText(dpe_fileLoc+@"printproc.txt","Statement : \n");
						foreach (string statstring in ThisStat.StatementParts)
						{
							File.AppendAllText(dpe_fileLoc+@"printproc.txt","   "+statstring+"\n");
						}
						foreach (int lfloat in localFloats)
						{
							File.AppendAllText(dpe_fileLoc+@"printproc.txt"," local : "+lfloat.ToString()+"\n");
						}
						File.AppendAllText(dpe_fileLoc+@"printproc.txt","Externals : \n");
						File.AppendAllText(dpe_fileLoc+@"printproc.txt"," state      : "+thisHead.state.ToString()+"\n");
						File.AppendAllText(dpe_fileLoc+@"printproc.txt"," draw_state : "+thisHead.draw_state.ToString()+"\n");
						File.AppendAllText(dpe_fileLoc+@"printproc.txt"," enabled    : "+thisHead.mainSignal.enabled.ToString()+"\n");
						File.AppendAllText(dpe_fileLoc+@"printproc.txt"," blockstate : "+thisHead.mainSignal.blockState.ToString()+"\n");
						File.AppendAllText(dpe_fileLoc+@"printproc.txt","\n");
					}
#endif

#if DEBUG_PRINT_PROCESS
					if (TDB_debug_ref.Contains(thisHead.TDBIndex))
					{
						File.AppendAllText(dpr_fileLoc+@"printproc.txt","Statement : \n");
						foreach (string statstring in ThisStat.StatementParts)
						{
							File.AppendAllText(dpr_fileLoc+@"printproc.txt","   "+statstring+"\n");
						}
						foreach (int lfloat in localFloats)
						{
							File.AppendAllText(dpr_fileLoc+@"printproc.txt"," local : "+lfloat.ToString()+"\n");
						}
						File.AppendAllText(dpr_fileLoc+@"printproc.txt","Externals : \n");
						File.AppendAllText(dpr_fileLoc+@"printproc.txt"," state      : "+thisHead.state.ToString()+"\n");
						File.AppendAllText(dpr_fileLoc+@"printproc.txt"," draw_state : "+thisHead.draw_state.ToString()+"\n");
						File.AppendAllText(dpr_fileLoc+@"printproc.txt"," enabled    : "+thisHead.mainSignal.enabled.ToString()+"\n");
						File.AppendAllText(dpr_fileLoc+@"printproc.txt"," blockstate : "+thisHead.mainSignal.blockState.ToString()+"\n");
						File.AppendAllText(dpr_fileLoc+@"printproc.txt","\n");
					}
#endif

				}

				if (scriptstat is SCRScripts.SCRConditionBlock)
				{
					SCRScripts.SCRConditionBlock thisCond = (SCRScripts.SCRConditionBlock) scriptstat;
					SH_processIfCondition(thisHead, thisCond, localFloats, sigscr);
				}
			}
		}

  //================================================================================================//
  //
  // print processed script - for DEBUG purposes only
  //
  //================================================================================================//

		public void SH_processAssignStatement(SignalHead thisHead, SCRScripts.SCRStatement thisStat,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

  // get term value

			int tempvalue = 0;

			tempvalue = SH_processSubTerm(thisHead, thisStat.StatementTerms, 0, localFloats, sigscr);

  // assign value

			switch (thisStat.AssignType)
			{

  // assign value to external float
  // Possible floats :
  //			STATE
  //			DRAW_STATE
  //			ENABLED     (not allowed for write)
  //			BLOCK_STATE (not allowed for write)

				case (SCRTermType.ExternalFloat) :
					SCRExternalFloats FloatType = (SCRExternalFloats) thisStat.AssignParameter;

					switch (FloatType)
					{
						case SCRExternalFloats.STATE :
							thisHead.state = (SignalHead.SIGASP) tempvalue;
							break;

						case SCRExternalFloats.DRAW_STATE :
							thisHead.draw_state = tempvalue;
							break;

						default:
							break;
					}
					break;

  // Local float

				case (SCRTermType.LocalFloat) :
					localFloats[thisStat.AssignParameter]=tempvalue;
					break;

				default:
					break;
			}
		}

  //================================================================================================//
  //
  // get value of single term
  //
  //================================================================================================//

		public int SH_processAssignTerm(SignalHead thisHead, List <SCRScripts.SCRStatTerm> StatementTerms,
					       SCRScripts.SCRStatTerm thisTerm, int sublevel,
					       int [] localFloats, SIGSCRfile sigscr)
		{

			int termvalue = 0;

			if (thisTerm.Function != SCRExternalFunctions.NONE)
			{
				termvalue = SH_function_value(thisHead, thisTerm, localFloats, sigscr);
			}
			else if (thisTerm.PartParameter != null)
			{

  // for non-function terms only first entry is valid

		                SCRScripts.SCRParameterType thisParameter = thisTerm.PartParameter[0];
				termvalue = SH_termvalue(thisHead, thisParameter, localFloats, sigscr);
			}
			else if (thisTerm.sublevel > 0)
			{
				termvalue = SH_processSubTerm(thisHead, StatementTerms, thisTerm.sublevel, localFloats, sigscr);
			}

			return termvalue;
		}

		
  //================================================================================================//
  //
  // process subterm
  //
  //================================================================================================//

		public int SH_processSubTerm(SignalHead thisHead, List <SCRScripts.SCRStatTerm> StatementTerms,
					       int sublevel, int [] localFloats, SIGSCRfile sigscr)
		{
			int tempvalue = 0;
			int termvalue = 0;

			foreach (SCRScripts.SCRStatTerm thisTerm in StatementTerms)
			{
				SCRTermOperator thisOperator = thisTerm.TermOperator;
				if (thisTerm.issublevel == sublevel)
				{
					termvalue =
					       	SH_processAssignTerm(thisHead, StatementTerms, thisTerm, sublevel, localFloats, sigscr);
					if (thisTerm.negate)
					{
						termvalue = termvalue == 0 ? 1 : 0;
					}
	
					switch (thisOperator)
					{
						case (SCRTermOperator.MULTIPLY) :
							tempvalue *= termvalue;
							break;

						case (SCRTermOperator.PLUS) :
							tempvalue += termvalue;
							break;

						case (SCRTermOperator.MINUS) :
							tempvalue -= termvalue;
							break;

						case (SCRTermOperator.DIVIDE) :
							if (termvalue == 0)
						{
							tempvalue = 0;
						}
							else
							{
							tempvalue /= termvalue;
							}
							break;

						case (SCRTermOperator.MODULO) :
							tempvalue %= termvalue;
							break;

						default :
							tempvalue = termvalue;
							break;
					}
				}
			}

			return tempvalue;
		}

  //================================================================================================//
  //
  // get parameter term value
  //
  //================================================================================================//

		public int SH_termvalue (SignalHead thisHead, SCRScripts.SCRParameterType thisParameter,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

			int return_value = 0;

  // for non-function terms only first entry is valid

			switch (thisParameter.PartType)
			{

  // assign value to external float
  // Possible floats :
  //			STATE
  //			DRAW_STATE
  //			ENABLED     
  //			BLOCK_STATE 

				case (SCRTermType.ExternalFloat) :
					SCRExternalFloats FloatType = (SCRExternalFloats) thisParameter.PartParameter;

					switch (FloatType)
					{
						case SCRExternalFloats.STATE :
							return_value = (int) thisHead.state;
							break;

						case SCRExternalFloats.DRAW_STATE :
							return_value = thisHead.draw_state;
							break;

						case SCRExternalFloats.ENABLED :
							return_value = Convert.ToInt32(thisHead.mainSignal.enabled);
							break;

						case SCRExternalFloats.BLOCK_STATE :
							return_value = (int) thisHead.mainSignal.block_state();
							break;

						default:
							break;
					}
					break;

  // Local float

				case (SCRTermType.LocalFloat) :
					return_value = localFloats[thisParameter.PartParameter];
					break;

  // all others : constants

				default:
					return_value = thisParameter.PartParameter;
					break;

			}

			return return_value;
		}

  //================================================================================================//
  //
  // return function value
  // Possible functions :
  //			BLOCK_STATE
  //			ROUTE_SET
  //			NEXT_SIG_LR
  //			NEXT_SIG_MR
  //			THIS_SIG_LR
  //			THIS_SIG_MR
  //			OPP_SIG_LR
  //			OPP_SIG_MR
  //			DIST_MULTI_SIG_MR
  //			SIG_FEATURE
  //			DEF_DRAW_STATE
  //			DEBUG_HEADER   (does not return a value)
  //			DEBUG_OUT      (does not return a value)
  //
  //================================================================================================//

		public int SH_function_value (SignalHead thisHead, SCRScripts.SCRStatTerm thisTerm,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

			int return_value = 0;
			int parameter1_value = 0;
			int parameter2_value= 0;

  // extract parameters (max. 2)

			if (thisTerm.PartParameter != null)
			{
				if (thisTerm.PartParameter.Length >= 1)
				{
					SCRScripts.SCRParameterType thisParameter = thisTerm.PartParameter[0];
					parameter1_value = SH_termvalue(thisHead, thisParameter, 
						localFloats, sigscr);
				}

				if (thisTerm.PartParameter.Length >= 2)
				{
					SCRScripts.SCRParameterType thisParameter = thisTerm.PartParameter[1];
					parameter2_value = SH_termvalue(thisHead, thisParameter, 
						localFloats, sigscr);
				}
			}

  // switch on function

			SCRExternalFunctions thisFunction = thisTerm.Function;

			switch (thisFunction)
			{

  // BlockState

				case (SCRExternalFunctions.BLOCK_STATE) :
					return_value = (int) thisHead.mainSignal.block_state();
					break;

  // Route set

				case (SCRExternalFunctions.ROUTE_SET) :
					return_value = (int) thisHead.route_set();
					break;

  // next_sig_lr

				case(SCRExternalFunctions.NEXT_SIG_LR) :
					return_value = (int) thisHead.next_sig_lr( (SignalHead.SIGFN) parameter1_value);
#if DEBUG_PRINT_ENABLED
					if (thisHead.mainSignal.enabledTrain != null)
					{
						File.AppendAllText(dpe_fileLoc+@"printproc.txt",
								" NEXT_SIG_LR : Located signal : "+
							       	thisHead.mainSignal.sigfound[parameter1_value].ToString()+"\n");
					}
#endif
#if DEBUG_PRINT_PROCESS
					if (TDB_debug_ref.Contains(thisHead.TDBIndex))
					{
						File.AppendAllText(dpr_fileLoc+@"printproc.txt",
								" NEXT_SIG_LR : Located signal : "+
							       	thisHead.mainSignal.sigfound[parameter1_value].ToString()+"\n");
					}
#endif

					break;

  // next_sig_mr

				case(SCRExternalFunctions.NEXT_SIG_MR) :
					return_value = (int) thisHead.next_sig_mr( (SignalHead.SIGFN) parameter1_value);
					break;

  // this_sig_lr

				case(SCRExternalFunctions.THIS_SIG_LR) :
					return_value = (int) thisHead.this_sig_lr( (SignalHead.SIGFN) parameter1_value);
					break;

  // this_sig_mr

				case(SCRExternalFunctions.THIS_SIG_MR) :
					return_value = (int) thisHead.this_sig_mr( (SignalHead.SIGFN) parameter1_value);
					break;

  // opp_sig_lr

				case(SCRExternalFunctions.OPP_SIG_LR) :
					return_value = (int) thisHead.opp_sig_lr( (SignalHead.SIGFN) parameter1_value);
					break;

  // opp_sig_mr

				case(SCRExternalFunctions.OPP_SIG_MR) :
					return_value = (int) thisHead.opp_sig_mr( (SignalHead.SIGFN) parameter1_value);
					break;

  // dist_multi_sig_mr

				case(SCRExternalFunctions.DIST_MULTI_SIG_MR) :
					return_value = (int) thisHead.dist_multi_sig_mr(
							(SignalHead.SIGFN) parameter1_value,
							(SignalHead.SIGFN) parameter2_value);
					break;

  // sig_feature

				case(SCRExternalFunctions.SIG_FEATURE) :
					bool temp_value;
					temp_value = thisHead.sig_feature(parameter1_value);
					return_value = Convert.ToInt32(temp_value);
					break;

  // def_draw_state

				case(SCRExternalFunctions.DEF_DRAW_STATE) :
					return_value = thisHead.def_draw_state( (SignalHead.SIGASP) parameter1_value);
					break;

  // DEBUG routine : to be implemented later

				default:
					break;
			}

#if DEBUG_PRINT_ENABLED
			if (thisHead.mainSignal.enabledTrain != null)
			{
				File.AppendAllText(dpe_fileLoc+@"printproc.txt",
						"Function Result : "+thisFunction.ToString()+"("+parameter1_value.ToString()+") = "+
						return_value.ToString()+"\n");
			}
#endif
#if DEBUG_PRINT_PROCESS
			if (TDB_debug_ref.Contains(thisHead.TDBIndex))
			{
				File.AppendAllText(dpr_fileLoc+@"printproc.txt",
						"Function Result : "+thisFunction.ToString()+"("+parameter1_value.ToString()+") = "+
						return_value.ToString()+"\n");
			}
#endif




  // check sign

			if (thisTerm.TermOperator == SCRTermOperator.MINUS)
			{
				return_value = -(return_value);
			}

			return return_value;
		}

  //================================================================================================//
  //
  // check IF conditions
  //
  //================================================================================================//

		public void SH_processIfCondition(SignalHead thisHead, SCRScripts.SCRConditionBlock thisCond,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

  //					SCRScripts.SCRConditionBlock thisCond = (SCRScripts.SCRConditionBlock) scriptstat;
  //					SH_processIfCondition(thisHead, thisCond, localFloats, sigscr);
  //				public ArrayList       Conditions;
  //				public SCRBlock        IfBlock;
  //				public List <SCRBlock> ElseIfBlock;
  //				public SCRBlock        ElseBlock;

			bool condition = true;
			bool performed = false;

  // check condition

			condition = SH_processConditionStatement(thisHead, thisCond.Conditions, localFloats, sigscr);

  // if set : execute IF block

			if (condition)
			{
				SH_process_StatementBlock(thisHead, thisCond.IfBlock.Statements, localFloats, sigscr);
				performed = true;
			}

  // not set : check through ELSEIF

			if (!performed)
			{
				int totalElseIf;
				if (thisCond.ElseIfBlock == null)
				{
					totalElseIf = 0;
				}
				else
				{
					totalElseIf = thisCond.ElseIfBlock.Count;
				}

				for (int ielseif=0; ielseif < totalElseIf && !performed; ielseif++)
				{

  // first (and only ) entry in ELSEIF block must be IF condition - extract condition

					object elseifStat = thisCond.ElseIfBlock[ielseif].Statements[0];
					if (elseifStat is SCRScripts.SCRConditionBlock)
					{
						SCRScripts.SCRConditionBlock elseifCond =
						       	(SCRScripts.SCRConditionBlock) elseifStat;

						condition = SH_processConditionStatement(thisHead, elseifCond.Conditions,
								localFloats, sigscr);

						if (condition)
						{
							SH_process_StatementBlock(thisHead, elseifCond.IfBlock.Statements,
									localFloats, sigscr);
							performed = true;
						}
					}
				}
			}

  // ELSE block

			if (!performed && thisCond.ElseBlock != null)
			{
				SH_process_StatementBlock(thisHead, thisCond.ElseBlock.Statements, localFloats, sigscr);
			}
		}

  //================================================================================================//
  //
  // process condition statement
  //
  //================================================================================================//

		public bool SH_processConditionStatement(SignalHead thisHead, ArrayList thisCStatList,
			       	int [] localFloats, SIGSCRfile sigscr)
		{

  // loop through all conditions

			bool condition = true;
			bool newcondition = true;
			bool termnegate   = false;
			SCRAndOr condstring = SCRAndOr.NONE;

			foreach(object thisCond in thisCStatList)
			{

  // single condition : process

				if (thisCond is SCRNegate)
				{
					termnegate = true;
				}

				else if (thisCond is SCRScripts.SCRConditions)
				{
					SCRScripts.SCRConditions thisSingleCond = (SCRScripts.SCRConditions) thisCond;
					newcondition = SH_processSingleCondition(thisHead, thisSingleCond, localFloats, sigscr);

					if (termnegate)
					{
						termnegate = false;
						newcondition = newcondition ? false : true;
					}

					switch (condstring)
					{
						case (SCRAndOr.AND) :
							condition &= newcondition;
							break;

						case (SCRAndOr.OR) :
							condition |= newcondition;
							break;

						default :
							condition = newcondition;
							break;
					}
				}

  // AND or OR indication (to link previous and next part)

				else if (thisCond is SCRAndOr)
				{
					condstring = (SCRAndOr) thisCond;
				}

  // subcondition

				else
				{
					ArrayList subCond = (ArrayList) thisCond;
					newcondition = SH_processConditionStatement(thisHead, subCond, localFloats, sigscr);

					if (termnegate)
					{
						termnegate = false;
						newcondition = newcondition ? false : true;
					}

					switch (condstring)
					{
						case (SCRAndOr.AND) :
							condition &= newcondition;
							break;

						case (SCRAndOr.OR) :
							condition |= newcondition;
							break;

						default :
							condition = newcondition;
							break;
					}
				}

			}

			return condition;
		}

  //================================================================================================//
  //
  // process single condition
  //
  //================================================================================================//

		public bool SH_processSingleCondition(SignalHead thisHead, SCRScripts.SCRConditions thisCond, 
			       	int [] localFloats, SIGSCRfile sigscr)
		{

			int term1value = 0;
			int term2value = 0;
			bool condition = true;

  // get value of first term


#if DEBUG_PRINT_ENABLED
			if (thisHead.mainSignal.enabledTrain != null)
			{
				File.AppendAllText(dpe_fileLoc+@"printproc.txt","IF Condition statement (1) : \n");
			}
#endif
#if DEBUG_PRINT_PROCESS
			if (TDB_debug_ref.Contains(thisHead.TDBIndex))
			{
				File.AppendAllText(dpr_fileLoc+@"printproc.txt","IF Condition statement (1) : \n");
			}
#endif

			if (thisCond.Term1.Function != SCRExternalFunctions.NONE)
			{
				term1value = SH_function_value(thisHead, thisCond.Term1, localFloats, sigscr);
			}
			else if (thisCond.Term1.PartParameter != null)
			{
		                SCRScripts.SCRParameterType thisParameter = thisCond.Term1.PartParameter[0];

#if DEBUG_PRINT_ENABLED
				if (thisHead.mainSignal.enabledTrain != null)
				{
					File.AppendAllText(dpe_fileLoc+@"printproc.txt","Parameter : "+thisParameter.PartType.ToString()+" : "+
							thisParameter.PartParameter.ToString()+"\n");
				}
#endif
#if DEBUG_PRINT_PROCESS
				if (TDB_debug_ref.Contains(thisHead.TDBIndex))
				{
					File.AppendAllText(dpr_fileLoc+@"printproc.txt","Parameter : "+thisParameter.PartType.ToString()+" : "+
							thisParameter.PartParameter.ToString()+"\n");
				}
#endif

				SCRTermOperator thisOperator = thisCond.Term1.TermOperator;
				term1value = SH_termvalue(thisHead, thisParameter, 
						localFloats, sigscr);
				if (thisOperator == SCRTermOperator.MINUS)
				{
					term1value = -term1value;
				}
			}

  // get value of second term

			if (thisCond.Term2 == null)

  // if only one value : check for NOT

			{
				if (thisCond.negate1)
				{
					condition = !(Convert.ToBoolean(term1value));
				}
				else
				{
					condition = Convert.ToBoolean(term1value);
				}

#if DEBUG_PRINT_ENABLED
			if (thisHead.mainSignal.enabledTrain != null)
				{
					File.AppendAllText(dpe_fileLoc+@"printproc.txt","Result of single condition : "+
						" : "+condition.ToString()+" (NOT : "+thisCond.negate1.ToString()+")\n\n");
				}
#endif
#if DEBUG_PRINT_PROCESS
				if (TDB_debug_ref.Contains(thisHead.TDBIndex))
				{
					File.AppendAllText(dpr_fileLoc+@"printproc.txt","Result of single condition : "+
						" : "+condition.ToString()+" (NOT : "+thisCond.negate1.ToString()+")\n\n");
				}
#endif
			}

  // process second term

			else
			{

#if DEBUG_PRINT_ENABLED
				if (thisHead.mainSignal.enabledTrain != null)
				{
					File.AppendAllText(dpe_fileLoc+@"printproc.txt","IF Condition statement (2) : \n");
				}
#endif
#if DEBUG_PRINT_PROCESS
				if (TDB_debug_ref.Contains(thisHead.TDBIndex))
				{
					File.AppendAllText(dpr_fileLoc+@"printproc.txt","IF Condition statement (2) : \n");
				}
#endif

				if (thisCond.Term2.Function != SCRExternalFunctions.NONE)
				{
					term2value = SH_function_value(thisHead, thisCond.Term2, localFloats, sigscr);
				}
				else if (thisCond.Term2.PartParameter != null)
				{
		                	SCRScripts.SCRParameterType thisParameter = thisCond.Term2.PartParameter[0];

#if DEBUG_PRINT_ENABLED
					if (thisHead.mainSignal.enabledTrain != null)
					{
						File.AppendAllText(dpe_fileLoc+@"printproc.txt",
							"Parameter : "+thisParameter.PartType.ToString()+" : "+
							thisParameter.PartParameter.ToString()+"\n");
					}
#endif
#if DEBUG_PRINT_PROCESS
					if (TDB_debug_ref.Contains(thisHead.TDBIndex))
					{
						File.AppendAllText(dpr_fileLoc+@"printproc.txt",
							"Parameter : "+thisParameter.PartType.ToString()+" : "+
							thisParameter.PartParameter.ToString()+"\n");
					}
#endif

					SCRTermOperator thisOperator = thisCond.Term2.TermOperator;
					term2value = SH_termvalue(thisHead, thisParameter,
						localFloats, sigscr);
					if (thisOperator == SCRTermOperator.MINUS)
					{
						term2value = -term2value;
					}
				}

  // check on required condition

				switch (thisCond.Condition)
				{

  // GT

					case (SCRTermCondition.GT) :
						condition = (term1value > term2value);
						break;

  // GE

					case (SCRTermCondition.GE) :
						condition = (term1value >= term2value);
						break;

  // LT

					case (SCRTermCondition.LT) :
						condition = (term1value < term2value);
						break;

  // LE

					case (SCRTermCondition.LE) :
						condition = (term1value <= term2value);
						break;

  // EQ

					case (SCRTermCondition.EQ) :
						condition = (term1value == term2value);
						break;

  // NE

					case (SCRTermCondition.NE) :
						condition = (term1value != term2value);
						break;
				}

#if DEBUG_PRINT_ENABLED
				if (thisHead.mainSignal.enabledTrain != null)
                {
                    File.AppendAllText(dpe_fileLoc + @"printproc.txt", "Result of operation : " +
                        thisCond.Condition.ToString() + " : " + condition.ToString() + "\n\n");
                }
#endif
#if DEBUG_PRINT_PROCESS
				if (TDB_debug_ref.Contains(thisHead.TDBIndex))
				{
					File.AppendAllText(dpr_fileLoc+@"printproc.txt","Result of operation : "+
						thisCond.Condition.ToString()+" : "+condition.ToString()+"\n\n");
				}
#endif
			}


			return condition;
		}

  //================================================================================================//

	}
}


