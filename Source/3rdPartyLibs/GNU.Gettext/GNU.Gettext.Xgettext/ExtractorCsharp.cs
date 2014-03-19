using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Resources;
using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace GNU.Gettext.Xgettext
{
    public enum ExtractMode
    {
        Msgid,
        MsgidConcat, // Like "Str 1 " + "Str 2" + "Str 3"
        MsgidFromResx, // For forms/controls that have property "Localizable" = true
        MsgidPlural,
        ContextMsgid,
    }

    public class ExtractorCsharp
    {
        const string CsharpStringPatternExplained = @"
			(\w+)\s*=\s*    # key =
			(               # Capturing group for the string
			    @""               # verbatim string - match literal at-sign and a quote
			    (?:
			        [^""]|""""    # match a non-quote character, or two quotes
			    )*                # zero times or more
			    ""                #literal quote
			|               #OR - regular string
			    ""              # string literal - opening quote
			    (?:
			        \\.         # match an escaped character,
			        |[^\\""]    # or a character that isn't a quote or a backslash
			    )*              # a few times
			    ""              # string literal - closing quote
			)";

        const string CsharpStringPattern = @"(@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*"")";
        const string ConcatenatedStringsPattern = @"((@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*"")\s*(?:\+|;|,|\))\s*){2,}";
        const string TwoStringsArgumentsPattern = CsharpStringPattern + @"\s*,\s*" + CsharpStringPattern;
        const string ThreeStringsArgumentsPattern = TwoStringsArgumentsPattern + @"\s*,\s*" + CsharpStringPattern;

        public const string CsharpStringPatternMacro = "%CsharpString%";

        public Catalog Catalog { get; private set; }
        public Options Options { get; private set; }

        #region Constructors
        public ExtractorCsharp(Options options)
        {
            this.Options = options;
            this.Catalog = new Catalog();
            if (!Options.Overwrite && File.Exists(Options.OutFile))
            {
                Catalog.Load(Options.OutFile);
                foreach (CatalogEntry entry in Catalog)
                    entry.ClearReferences();
            }
            else
            {
                Catalog.Project = "PACKAGE VERSION";
            }

            this.Options.OutFile = Path.GetFullPath(this.Options.OutFile);
        }
        #endregion

        public void GetMessages()
        {
            // Create input files list
            Dictionary<string, string> inputFiles = new Dictionary<string, string>();
            foreach (string dir in Options.InputDirs)
            {
                foreach (string fileNameOrMask in Options.InputFiles)
                {
                    string[] filesInDir = Directory.GetFiles(
                        dir,
                        fileNameOrMask,
                        Options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    foreach (string fileName in filesInDir)
                    {
                        string fullFileName = Path.GetFullPath(fileName);
                        if (!inputFiles.ContainsKey(fullFileName))
                            inputFiles.Add(fullFileName, fullFileName);
                    }
                }
            }

            foreach (string inputFile in inputFiles.Values)
            {
                GetMessagesFromFile(inputFile);
            }
        }

        private void GetMessagesFromFile(string inputFile)
        {
            inputFile = Path.GetFullPath(inputFile);
            StreamReader input = new StreamReader(inputFile, Options.InputEncoding, Options.DetectEncoding);
            string text = input.ReadToEnd();
            input.Close();
            GetMessages(text, inputFile);
        }


        public void GetMessages(string text, string inputFile)
        {
            text = RemoveComments(text);

            // Gettext functions patterns
            ProcessPattern(ExtractMode.Msgid, @"GetString\s*\(\s*" + CsharpStringPattern, text, inputFile);
            ProcessPattern(ExtractMode.Msgid, @"GetStringFmt\s*\(\s*" + CsharpStringPattern, text, inputFile);
            ProcessPattern(ExtractMode.MsgidPlural, @"GetPluralString\s*\(\s*" + TwoStringsArgumentsPattern, text, inputFile);
            ProcessPattern(ExtractMode.MsgidPlural, @"GetPluralStringFmt\s*\(\s*" + TwoStringsArgumentsPattern, text, inputFile);
            ProcessPattern(ExtractMode.ContextMsgid, @"GetParticularString\s*\(\s*" + TwoStringsArgumentsPattern, text, inputFile);
            ProcessPattern(ExtractMode.ContextMsgid, @"GetParticularPluralString\s*\(\s*" + ThreeStringsArgumentsPattern, text, inputFile);


            // Winforms patterns
			ProcessPattern(ExtractMode.Msgid, @"\.\s*Text\s*=\s*" + CsharpStringPattern + @"\s*;", text, inputFile);
            ProcessPattern(ExtractMode.MsgidConcat, @"\.\s*Text\s*=\s*" + ConcatenatedStringsPattern, text, inputFile);

            ProcessPattern(ExtractMode.Msgid, @"\.\s*HeaderText\s*=\s*" + CsharpStringPattern + @"\s*;", text, inputFile);
            ProcessPattern(ExtractMode.MsgidConcat, @"\.\s*HeaderText\s*=\s*" + ConcatenatedStringsPattern, text, inputFile);

            ProcessPattern(ExtractMode.Msgid, @"\.\s*ToolTipText\s*=\s*" + CsharpStringPattern + @"\s*;", text, inputFile);
            ProcessPattern(ExtractMode.MsgidConcat, @"\.\s*ToolTipText\s*=\s*" + ConcatenatedStringsPattern, text, inputFile);

            ProcessPattern(ExtractMode.Msgid, @"\.\s*SetToolTip\s*\([^\\""]*\s*,\s*" + CsharpStringPattern + @"\s*\)\s*;", text, inputFile);
            ProcessPattern(ExtractMode.MsgidConcat, @"\.\s*SetToolTip\s*\([^\\""]*\s*,\s*" + ConcatenatedStringsPattern, text, inputFile);

            if (ReadResources(inputFile))
                ProcessPattern(ExtractMode.MsgidFromResx, @"\.\s*ApplyResources\s*\([^\\""]*\s*,\s*" + CsharpStringPattern + @"\s*\)\s*;", text, inputFile);

            ReadXaml(inputFile);

            // Custom patterns
            foreach (string pattern in Options.SearchPatterns)
            {
                ProcessPattern(ExtractMode.Msgid, pattern.Replace(CsharpStringPatternMacro, CsharpStringPattern), text, inputFile);
            }
        }


        public void Save()
        {
            if (File.Exists(Options.OutFile))
            {
                string bakFileName = Options.OutFile + ".bak";
                if (File.Exists(bakFileName))
                    File.Delete(bakFileName);
                File.Copy(Options.OutFile, bakFileName);
                File.Delete(Options.OutFile);
            }
            Catalog.Save(Options.OutFile);
        }

        public static string RemoveComments(string input)
        {
            string blockComments = @"/\*(.*?)\*/";
            string lineComments = @"//(.*?)(\r?\n|$)";

            return Regex.Replace(
                input,
                blockComments + "|" + lineComments + "|" + CsharpStringPattern,
				m => {
                    if (m.Value.StartsWith("/*") || m.Value.StartsWith("//"))
                    {
                        // Replace the comments with empty, i.e. remove them
                        return m.Value.StartsWith("//") ? m.Groups[3].Value : "";
                    }
                    // Keep the literal strings
                    return m.Value;
                },
            RegexOptions.Singleline);
        }

        private void ProcessPattern(ExtractMode mode, string pattern, string text, string inputFile)
        {
            Regex r = new Regex(pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            MatchCollection matches = r.Matches(text);
            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;
                if (groups.Count < 2)
                    throw new Exception(String.Format(
                        "Invalid pattern '{0}'.\nTwo groups are required at least.\nSource: {1}",
                        pattern, match.Value));

                // Initialisation
                string context = String.Empty;
                string msgid = String.Empty;
                string msgidPlural = String.Empty;
                switch (mode)
                {
                    case ExtractMode.Msgid:
                        msgid = Unescape(groups[1].Value);
                        break;
                    case ExtractMode.MsgidConcat:
                        MatchCollection matches2 = Regex.Matches(groups[0].Value, CsharpStringPattern);
                        StringBuilder sb = new StringBuilder();
                        foreach (Match match2 in matches2)
                        {
                            sb.Append(Unescape(match2.Value));
                        }
                        msgid = sb.ToString();
                        break;
                    case ExtractMode.MsgidFromResx:
                        string controlId = Unescape(groups[1].Value);
                        msgid = ExtractResourceString(controlId);
                        if (String.IsNullOrEmpty(msgid))
                        {
                            if (Options.Verbose)
                                Trace.WriteLine(String.Format(
                                    "Warning: cannot extract string for control '{0}' ({1})",
                                    controlId, inputFile));
                            continue;
                        }
                        if (controlId == msgid)
                            continue; // Text property was initialized by controlId and was not changed so this text is not usable in application
                        break;
                    case ExtractMode.MsgidPlural:
                        if (groups.Count < 3)
                            throw new Exception(String.Format("Invalid 'GetPluralString' call.\nSource: {0}", match.Value));
                        msgid = Unescape(groups[1].Value);
                        msgidPlural = Unescape(groups[2].Value);
                        break;
                    case ExtractMode.ContextMsgid:
                        if (groups.Count < 3)
                            throw new Exception(String.Format("Invalid get context message call.\nSource: {0}", match.Value));
                        context = Unescape(groups[1].Value);
                        msgid = Unescape(groups[2].Value);
                        if (groups.Count == 4)
                            msgidPlural = Unescape(groups[3].Value);
                        break;
                }

                if (String.IsNullOrEmpty(msgid))
                {
					if (Options.Verbose)
                    	Trace.Write(String.Format("WARN: msgid is empty in {0}\r\n", inputFile));
                }
				else
				{
					MergeWithEntry(context, msgid, msgidPlural, inputFile, CalcLineNumber(text, match.Index));
				}
            }
        }

        private void MergeWithEntry(
            string context,
            string msgid,
            string msgidPlural,
            string inputFile,
            int line)
        {
            // Processing
            CatalogEntry entry = Catalog.FindItem(msgid, context);
            bool entryFound = entry != null;
            if (!entryFound)
                entry = new CatalogEntry(Catalog, msgid, msgidPlural);

            // Add source reference if it not exists yet
            // Each reference is in the form "path_name:line_number"
            string sourceRef = String.Format("{0}:{1}",
                                             Utils.FileUtils.GetRelativeUri(Path.GetFullPath(inputFile), Path.GetFullPath(Options.OutFile)),
                                             line);
            entry.AddReference(sourceRef); // Wont be added if exists

            if (FormatValidator.IsFormatString(msgid) || FormatValidator.IsFormatString(msgidPlural))
            {
                if (!entry.IsInFormat("csharp"))
                    entry.Flags += ", csharp-format";
                Trace.WriteLineIf(
                    !FormatValidator.IsValidFormatString(msgid),
                    String.Format("Warning: string format may be invalid: '{0}'\nSource: {1}", msgid, sourceRef));
                Trace.WriteLineIf(
                    !FormatValidator.IsValidFormatString(msgidPlural),
                    String.Format("Warning: plural string format may be invalid: '{0}'\nSource: {1}", msgidPlural, sourceRef));
            }

            if (!String.IsNullOrEmpty(msgidPlural))
            {
                if (!entryFound)
                {
                    AddPluralsTranslations(entry);
                }
                else
                    UpdatePluralEntry(entry, msgidPlural);
            }
            if (!String.IsNullOrEmpty(context))
            {
                entry.Context = context;
                entry.AddAutoComment(String.Format("Context: {0}", context), true);
            }

            if (!entryFound)
                Catalog.AddItem(entry);
        }


        private Dictionary<string, string> resources = new Dictionary<string, string>();

        private bool ReadResources(string inputFile)
        {
            resources.Clear();
            string resxFileName = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));
            if (resxFileName.EndsWith(".Designer"))
                resxFileName = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(resxFileName));
            resxFileName += ".resx";

            if (!File.Exists(resxFileName))
                return false;
            else
            {
                if (Options.Verbose)
                    Debug.WriteLine(String.Format("Extracting from resource file: {0} (Input file: {1})",
                                                  resxFileName, inputFile));
            }
            ResXResourceReader rsxr = new ResXResourceReader(resxFileName);
            // stephane matamontero: the following line was needed when I was debugging
            // Xgettext where I passed commandline parameters: 
            // It tried to search the "Resources.resx" of the project where we wanted to extract
            // the messages in a subdirectory of *this* project. Possibly a VS2008 bug !
            rsxr.BasePath = Path.GetDirectoryName(resxFileName);
            foreach (DictionaryEntry entry in rsxr)
            {
                if (entry.Value is string)
                {
                    resources.Add(entry.Key.ToString(), entry.Value.ToString());
                    //Debug.WriteLine(String.Format("{0}: {1}", entry.Key.ToString(), entry.Value.ToString()));
                }
            }
            return true;
        }
        
        private void ReadXaml(string inputFile)
        {
            // The method is based on xaml2po.py script
            if (Path.GetExtension(inputFile) != ".xaml")
                return;
            
            Dictionary<string, XAttribute> attributes;
            var attributeTags = "Title Content Text Header ToolTip".ToLower().Split(' ');
            var nameTags = "TextBlock Button".ToLower().Split(' ');
            var xaml = XDocument.Load(inputFile, LoadOptions.SetLineInfo);

            foreach (var node in xaml.Descendants())
            {
                if (nameTags.Contains(node.Name.LocalName.ToLower()) && (string)node != String.Empty)
                    MergeWithEntry(String.Empty, (string)node, String.Empty, inputFile, ((IXmlLineInfo)node).LineNumber);
                
                attributes = new Dictionary<string, XAttribute>();
                foreach (var attribute in node.Attributes())
                    attributes.Add(attribute.Name.LocalName.ToLower(), attribute);
                
                foreach (var tag in attributeTags)
                    if (attributes.Keys.Contains(tag))
                        MergeWithEntry(String.Empty, (string)attributes[tag], String.Empty, inputFile, ((IXmlLineInfo)attributes[tag]).LineNumber);
            }
        }

        private string ExtractResourceString(string controlId)
        {
            string msgid = null;
            if (!resources.TryGetValue(controlId + ".Text", out msgid))
                if (!resources.TryGetValue(controlId + ".TooTipText", out msgid))
                    if (!resources.TryGetValue(controlId + ".HeaderText", out msgid))
                        return null;
            return msgid;
        }

        private int CalcLineNumber(string text, int pos)
        {
            if (pos >= text.Length)
                pos = text.Length - 1;
            int line = 0;
            for (int i = 0; i < pos; i++)
                if (text[i] == '\n')
                    line++;
            return line + 1;
        }

        private void UpdatePluralEntry(CatalogEntry entry, string msgidPlural)
        {
            if (!entry.HasPlural)
            {
                AddPluralsTranslations(entry);
                entry.SetPluralString(msgidPlural);
            }
            else if (entry.HasPlural && entry.PluralString != msgidPlural)
            {
                entry.SetPluralString(msgidPlural);
            }
        }

        private void AddPluralsTranslations(CatalogEntry entry)
        {
            // Creating 2 plurals forms by default
            // Translator should change it using expression for it own country
            // http://translate.sourceforge.net/wiki/l10n/pluralforms
            List<string> translations = new List<string>();
            for (int i = 0; i < Catalog.PluralFormsCount; i++)
                translations.Add("");
            entry.SetTranslations(translations.ToArray());
        }

        private static string Unescape(string msgid)
        {
            StringEscaping.EscapeMode mode = StringEscaping.EscapeMode.CSharp;
            if (msgid.StartsWith("@"))
                mode = StringEscaping.EscapeMode.CSharpVerbatim;
            return StringEscaping.UnEscape(mode, msgid.Trim(new char[] { '@', '"' }));
        }
    }
}

