/// This file contains code to read MSTS structured unicode text files
/// through the class  STFReader.   
/// 
/// Note:  the SBR classes are more general in that they are capable of reading
///        both unicode and binary compressed data files.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace MSTS
{
    /// <summary>Used for reading data from Structured Text Format (MSTS1 style) files.
    /// </summary><remarks><para>
    /// An STF file is whitespace delimitered file, taking the format - {item}{whitespace}[repeated].</para><para>
    /// &#160;</para><para>
    /// At it's most simple an STF file has the format - {token_item}{whitespace}{data_item}{whitespace}(repeated)</para><para>
    /// Even, more simplisitically every {data_item} can be a {constant_item}</para>
    /// <code lang="STF" title="STF Example"><para>
    ///     Example:</para><para>
    ///     name SimpleSTFfile</para><para>
    ///     weight 100</para><para>
    ///     speed 50.25</para>
    /// </code>&#160;<para>
    /// STF also has a block methodology where a {data_item} following a {token_item} can start with '(' followed by any number of {data_item}s and closed with a ')'.
    /// The contents of the block are defined in the specific file schema, and not in the STF definition.
    /// The STF defintion allows that inside a pair of parentheses may be a single {constant_item}, multiple whitespace delimitered {constant_item}s, or a nested {token_item}{data_item} pair (which could contain a further nested block recursively).</para>
    /// <code lang="STF" title="STF Example"><para>
    ///     Example:</para><para>
    ///     name BlockedSTFfile</para><para>
    ///     root_constant 100</para><para>
    ///     root_block_1</para><para>
    ///     (</para><para>
    ///         &#160;&#160;nested_block_1_1</para><para>
    ///         &#160;&#160;(</para><para>
    ///             &#160;&#160;&#160;&#160;1</para><para>
    ///         &#160;&#160;)</para><para>
    ///         &#160;&#160;nested_block_1_2 ( 5 )</para><para>
    ///     )</para><para>
    ///     root_block_2</para><para>
    ///     (</para><para>
    ///         &#160;&#160;1 2 3</para><para>
    ///     )</para><para>
    ///     root_block_3 ( a b c )</para>
    /// </code>&#160;<para>
    /// Numeric {constan_item}s can include a 'unit' suffix, which is handled in the ReadDouble() function.</para><para>
    /// Within ReadDouble these units are then converted to the standards used throughout OR - meters, newtons, kilograms.</para>
    /// <code lang="STF" title="STF Example"><para>
    ///     Example:</para><para>
    ///     name STFfileWithUnits</para><para>
    ///     weight 100kg</para><para>
    ///     speed 50mph</para>
    /// </code>&#160;<para>
    /// Whitespaces can be included within any {item} using a double quotation notation.
    /// Quoted values also support a trailing addition operator to indicate an append operation of multiple quoted strings.</para><para>
    /// Although append operations are technically allowed for {token_item}'s this practice is *strongly* discouraged for readability.</para>
    /// <code lang="STF" title="STF Example"><para>
    ///     Example:</para><para>
    ///     simple_token "Data Item with" + " whitespace"</para><para>
    ///     block_token ( "Data " + "Item 1" "Data Item 2" )</para><para>
    ///     "discouraged_" + "token" -1</para><para>
    ///     Error Example:</para><para>
    ///     error1 "You cannot use append suffix to non quoted " + items</para>
    /// </code>&#160;<para>
    /// The STF format also supports 3 special {token_item}s - include, comment &amp; skip.</para><list class="bullet">
    /// <listItem><para>include - must be at the root level (that is to say it cannot be included within a block).
    /// After an include directive the {constant_item} is a filename relative to the current processing STF file.
    /// The include token has the effect of in-lining the defined file into the current document.</para></listItem>
    /// <listItem><para>comment &amp; skip - must be followed by a {data_item} which will not be processed in OR</para></listItem>
    /// </list>
    /// </remarks>
    /// <example>
    /// !!!TODO!!!
    /// </example>
    /// <exception cref="STFException"><para>
    /// STF reports errors using the  exception static members</para><para>
    /// There are three broad categories of error</para><list class="bullet">
    /// <listItem><para>Failure - Something which prevents loading from continuing, this throws an unhandled exception and drops out of Open Rails.</para></listItem>
    /// <listItem><para>Error - The data read does not have logical meaning - STFReader does not generate these errors, this is only appropriate STFReader consumers who understand the context of the data being processed</para></listItem>
    /// <listItem><para>Warning - When an error which can be programatically recovered from should be reported back to the user</para></listItem>
    /// </list>
    /// </exception>
    public class STFReader : IDisposable
	{
        /// <summary>Open a file, reader the header line, and prepare for STF parsing
        /// </summary>
        /// <param name="filename">Filename of the STF file to be opened and parsed.</param>
		public STFReader(string filename)
        {
            streamSTF = new StreamReader(filename, true); // was System.Text.Encoding.Unicode ); but I found some ASCII files, ie GLOBAL\SHAPES\milemarker.s
            FileName = filename;
            SIMISsignature = streamSTF.ReadLine();
            LineNumber = 2;
        }
        /// <summary>Use an open stream for STF parsing, this constructor assumes that the SIMIS signature has already been gathered (or there isn't one)
        /// </summary>
        /// <param name="inputStream">Stream that will be parsed.</param>
        /// <param name="fileName">Is only used for error reporting.</param>
        /// <param name="encoding">One of the Encoding formats, defined as static members in Encoding which return an Encoding type.  Eg. Encoding.ASCII or Encoding.Unicode</param>
        public STFReader(Stream inputStream, string fileName, Encoding encoding)
        {
            Debug.Assert(inputStream.CanSeek);
            FileName = fileName;
            streamSTF = new StreamReader(inputStream , encoding);
            LineNumber = 1;
        }

        /// <summary>Implements the IDisposable interface so this class can be implemented with the 'using(STFReader r = new STFReader(...)) {...}' C# statement.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~STFReader()
        {
            Dispose(false);
        }
        /// <summary>Releases the resources used by the STFReader.
        /// </summary>
        /// <param name="disposing">
        /// <para>true - release managed and unmanaged resources.</para>
        /// <para>false - release only unmanaged resources.</para>
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                streamSTF.Close(); streamSTF = null;
            }
        }

        /// <summary>Property that returns true when the EOF has been reached
        /// </summary>
        public bool EOF { get { return Peek() == -1; } }
        /// <summary>Filename property for the file being parsed - for reporting purposes
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>Line Number property for the file being parsed - for reporting purposes
        /// </summary>
        public int LineNumber { get; private set; }
        /// <summary>SIMIS header read from the first line of the file being parsed
        /// </summary>
        public string SIMISsignature { get; private set; }
        /// <summary>Property returning the last {item} read using ReadItem() prefixed with string describing the nested block hierachy.
        /// <para>The string returned is formatted 'rootnode(nestednode(childnode(previous_item'.</para>
        /// </summary>
        /// <remarks>
        /// Tree is expensive method of reading STF files (especially for the GC) and should be avoided if possible.
        /// </remarks>
        public string Tree
        {
            get
            {
                StringBuilder sb = new StringBuilder(256);
                foreach (string t in tree) sb.Append(t);
                sb.Append(previousItem);
                return sb.ToString();
            }
        }


        /// <summary>This is the main function in STFReader, it returns the next whitespace delimited {item} from the STF file.
        /// </summary>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadItem()
        {
            if (rewindNextReadItemFlag)
            {
                Debug.Assert(rewindItem != null, "You must called at least one ReadItem() between RewindItem() calls", "The current rewind functionality only allows for a single rewind");
                string token = rewindItem;
                previousItem = rewindCurrItem;
                if (rewindTree != null) tree = rewindTree;
                rewindNextReadItemFlag = false;
                rewindItem = rewindCurrItem = null;
                rewindTree = null;
                return token;
            }

            if (IncludeReader != null)
            {
                string s = IncludeReader.ReadItem();
                UpdateTreeAndRewindBuffer(s);
                if (!IncludeReader.EOF)
                    return s;
                if (tree.Count != 0)
                    STFException.ReportWarning(IncludeReader, "Included file did not have a properly matched number of blocks.  It is unlikely the parent STF file will work properly.");
                IncludeReader.Dispose();
                IncludeReader = null;
            }

            int c = 0;

            var stringText = new StringBuilder();

            // Read leading whitespace 
            while (true)
            {
                c = ReadChar();
                if (IsEof(c)) 
                    break;
                if ( !IsWhiteSpace(c))
                    break;
            }
            // c == -1 or first char of token

            if (c == '"')
            {
                // Read the rest of the string token and final delimiting "
                do
                {
                    c = ReadChar();
                    if (IsEof(c))
                        break;
                    if (c == '\\') // escape sequence
                    {
                        c = ReadChar();
                        if (c == 'n')
                            stringText.Append('\n');
                        else
                            stringText.Append((char)c);  // ie \, " etc
                    }
                    else if (c != '"')
                    {
                        stringText.Append((char)c);
                    }
                    else //  c == '"'
                    {
                        // Check for string extender
                        if (PeekPastWhitespace() != '+')
                            break;   // we found final " to terminate the string
                        
                        // This is an extended string
                        // Skip over white space to next string
                        int cs = ReadChar();
                        do
                        {
                            c = ReadChar();
                            if (IsEof(c))
                                break;
                        }
                        while ( IsWhiteSpace(c));

                        // ensure we are at a quote
                        if (c != '"')
                            break;

                    }
                }
                while (true);
            }
            else if (c == '(')
            {
                stringText.Append((char)c);
            }
            else if (c == ')')
            {
                stringText.Append((char)c);
            }
            else if (c != -1)
            {
                // Read the rest of the token and first delimiter character
                do
                {
                    stringText.Append((char)c);
                    if (streamSTF.Peek() == '(')
                        break;
                    if (streamSTF.Peek() == ')')
                        break;
                    c = ReadChar();
                    if (IsEof(c))
                        break;
                }
                while ( !IsWhiteSpace(c) );
            }

            string result= stringText.ToString();
            if (tree.Count == 0 && result == "include")
            {
                string filename = ReadStringBlock();
                IncludeReader = new STFReader(Path.GetDirectoryName(FileName) + @"\" + filename);
                return ReadItem();
            }

            UpdateTreeAndRewindBuffer(result);
            return result;
        }
        /// <summary>Calling this function causes ReadItem() to repeat the last {item} that was read from the STF file
        /// </summary>
        /// <remarks>
        /// <para>The current implementation of RewindItem() only allows for "rewind".</para>
        /// <para>This means that there each call to RewindItem() must have an intervening call to ReadItem().</para>
        /// </remarks>
        public void RewindItem()
        {
            Debug.Assert(rewindItem != null, "You must called at least one ReadItem() between RewindItem() calls", "The current rewind functionality only allows for a single rewind");
            rewindNextReadItemFlag = true;
        }

        /// <summary>Reports a critical error if the next {item} does not match the target.
        /// </summary>
        /// <param name="target">The next {item} contents we are expecting in the STF file.</param>
        /// <returns>The {item} read from the STF file</returns>
        public void MustMatch(string target)
        {
            if (EOF)
                throw new STFException(this, "Unexpected end of file");
            string s = ReadItem();
            if (s != target)
                throw new STFException(this, target + " Not Found - instead found " + s);
        }

        /// <summary>Read the next {token_item} skipping past any 'comment', 'skip', '#*' or '_*' tokens.
        /// </summary>
        /// <remarks>
        /// <para>This cursor should be called when placed at a {token_item} and not at a {data_item}.</para>
        /// </remarks>
        /// <returns></returns>
        public string ReadTokenNoComment()
        {
            for (; ; )
            {
                string token = ReadItem();
                if (token.StartsWith("_") || token.StartsWith("#"))
                    SkipBlock();
                else
                {
                    string lower = token.ToLower();
                    if (lower == "skip" || lower == "comment")
                        SkipBlock();
                    else
                        return token;
                }
            }
        }

        /// <summary>Returns true if the next character is the end of block, or end of file. Consuming the closing ")" all other values are not consumed.
        /// </summary>
        /// <remarks>
        /// <para>An STF block should be enclosed in parenthesis, ie ( {data_item} {data_item} )</para>
        /// </remarks>
        /// <returns>
        /// <para>true - An EOF, or closing parenthesis was found and consumed.</para>
        /// <para>false - Another type of {item} was found but not consumed.</para>
        /// </returns>
        public bool EndOfBlock()
        {
            int c = PeekPastWhitespace();
            if (c == ')')
                c = streamSTF.Read();
            return c == ')' || c == -1;
        }
        /// <summary>Read a block open (, and then consume the rest of the block without processing.
        /// If we find an immediate close ), then produce a warning, and return without consuming the parenthesis.
        /// </summary>
        public void SkipBlock()
		{
			string token = ReadItem();  // read the leading bracket ( 
            if (token == ")")   // just in case we are not where we think we are
            {
                STFException.ReportWarning(this, "Found a close parenthesis, rather than the expected block of data");
                RewindItem();
                return;
            }
            // note, even if this isn't a leading bracket, we'll carry on anyway                            
            SkipRestOfBlock();
		}
        /// <summary>Skip to the end of this block, ignoring any nested blocks
        /// </summary>
        public void SkipRestOfBlock()
        {
            // We are inside a pair of brackets, skip the entire hierarchy to past the end bracket
            int depth = 1;
            while (!EOF && depth > 0)
            {
                string token = ReadItem();
                if (token == "(")
                    ++depth;
                if (token == ")")
                    --depth;
            }
        }


		public int ReadHex()
		{
			string token = ReadItem();
			try
			{
				return int.Parse(token, System.Globalization.NumberStyles.HexNumber);
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return 0;
			}
		}

		public uint ReadFlags()
		{
			string token = ReadItem();
			try
			{
				return uint.Parse(token, System.Globalization.NumberStyles.HexNumber);
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return 0;
			}
		}

		public int ReadInt()
		{
			return (int)ReadDouble();
		}

		public uint ReadUInt()
		{
			return (uint)ReadDouble();
		}

        public float ReadFloat()
		{
			return (float)ReadDouble();
		}

		public double ReadDouble()
		{
			double scale = 1.0;
			string token = ReadItem();

            if (token == ")")
            {
                STFException.ReportWarning(this, "When expecting a number, we found a ) marker");
                RewindItem();
                return 0;
            }

			// TODO complete parsing of units ie, km, etc - some are done but not all.
			token = token.ToLower();
			int i;
			// Add handling of units
			i = token.IndexOf("/2", StringComparison.Ordinal);
			if (i != -1)
			{
				scale /= 2;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("cm", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 0.01;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("mm", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 0.001;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("ft", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 0.3048;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("in", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 0.0254;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("kn", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 1e3;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("n", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 1e0;
				token = token.Substring(0, i);
			}
			i = token.IndexOf("t", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 1e3;
				token = token.Substring(0, i); // return kg
			}
			i = token.IndexOf("kg", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 1;
				token = token.Substring(0, i); // return kg
			}
			i = token.IndexOf("lb", StringComparison.Ordinal);
			if (i != -1)
			{
				scale *= 0.00045359237;
				token = token.Substring(0, i); // return kg
			}
			i = token.IndexOf("m", StringComparison.Ordinal);
			if (i != -1)
				token = token.Substring(0, i);

			i = token.IndexOf(",", StringComparison.Ordinal);   // MSTS ignores a comma at the end of the number
			if (i != -1)
				token = token.Substring(0, i);

			try
			{
				return double.Parse(token, CultureInfo.InvariantCulture) * scale;
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return 0;
			}
		}

		public string ReadStringBlock()
		{
            MustMatch("(");
			string s = ReadItem();
            SkipRestOfBlock();
			return s;
		}

        public uint ReadUIntBlock()
        {
            return ReadUIntBlock(false);
        }
        public uint ReadUIntBlock(bool optionalblock)
		{
			try
			{
                double value = ReadDoubleBlock(optionalblock);
				return (uint)value;
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return 0;
			}
		}

        public int ReadIntBlock()
        {
            return ReadIntBlock(false);
        }
        public int ReadIntBlock(bool optionalblock)
		{
			try
			{
				double value = ReadDoubleBlock(optionalblock);
				return (int)value;
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return 0;
			}
		}

        public float ReadFloatBlock()
        {
            return ReadFloatBlock(false);
        }
        public float ReadFloatBlock(bool optionalblock)
        {
            return (float)ReadDoubleBlock(optionalblock);
        }

        public double ReadDoubleBlock()
        {
            return ReadDoubleBlock(false);
        }
        public double ReadDoubleBlock(bool optionalblock)
		{
            if (EOF)
                STFException.ReportError(this, "Unexpected end of file");
            string s = ReadItem();
            if (s == ")" && optionalblock)
                RewindItem();
            else if (s == "(")
            {
                double result = ReadDouble();
                SkipRestOfBlock();
                return result;
            }
            else
                STFException.ReportError(this, "Block Not Found - instead found " + s);

            return 0;
		}

		public bool ReadBoolBlock()
		{
            MustMatch("(");
			string s = ReadItem();
			if (s == ")")
				return true;  // assume a null block is true
			int i;
			try
			{
				i = int.Parse(s);
			}
			catch (Exception e)
			{
				STFException.ReportInformation(this, e);
				return false;
			}
			SkipRestOfBlock();
			return i != 0;
		}

        public Vector3 ReadVector3Block()
        {
            Vector3 vector = new Vector3();
            MustMatch("(");
            vector.X = ReadFloat();
            vector.Y = ReadFloat();
            vector.Z = ReadFloat();
            SkipRestOfBlock();
            return vector;
        }

        private StreamReader streamSTF;
        /// <summary>IncludeReader is used recursively in ReadItem() to handle the 'include' token, file include mechanism
        /// </summary>
        private STFReader IncludeReader = null;
        /// <summary>Remembers the last returned ReadItem().  If the next {item] is a '(', this is the block name used in the tree.
        /// </summary>
        private string previousItem = "";
        /// <summary>A list describing the hierachy of nested block tokens
        /// </summary>
        private List<string> tree = new List<string>();
        #region *** Rewind Variables - It is important that all state variables in this class have a rewind equivalent
        /// <summary>This flag is set in RewindItem(), and causes ReadItem(), to use the rewind* variables to do an item repeat
        /// </summary>
        private bool rewindNextReadItemFlag = false;
        /// <summary>The rewind* variables store the previous state, so RewindItem() can jump back on {item}. rewindTree « tree
        /// <para>This item, is optimized, so when value is null it means rewindTree was the same as Tree, so we don't create unneccessary memory duplicates of lists.</para>
        /// </summary>
        private List<string> rewindTree;
        /// <summary>The rewind* variables store the previous state, so RewindItem() can jump back on {item}. rewindCurrItem « previousItem
        /// </summary>
        private string rewindCurrItem;
        /// <summary>The rewind* variables store the previous state, so RewindItem() can jump back on {item}. rewindItem « ReadItem() return
        /// </summary>
        private string rewindItem;
        #endregion

        #region *** Private Class Implementation
        private bool IsWhiteSpace(int c) { return c >= 0 && c <= ' '; }
        private bool IsEof(int c) { return c == -1; }
        private int Peek()
        {
            int c = streamSTF.Peek();
            if (IsEof(c))
            {
                // I've seen a problem with compressed input streams with a false -1 on peek
                c = streamSTF.Read();
                if (c != -1)
                    throw new InvalidDataException("Problem peeking eof in compressed file.");
            }
            return c;
        }
        private int PeekPastWhitespace()
        {
            // scan ahead and see if the next character is a bracket )
            int c = streamSTF.Peek();
            while (IsEof(c) || IsWhiteSpace(c)) // skip over eof and white space
            {
                c = ReadChar();
                if (IsEof(c))
                    break;   // break on reading eof 
                c = streamSTF.Peek();
            }
            return c;
        }
        private int ReadChar()
        {
            int c = streamSTF.Read();
            if (c == '\n') ++LineNumber;
            return c;
        }
        /// <summary>Internal Implementation
        /// <para>This function is called by ReadItem() for every item read from the STF file (and Included files).</para>
        /// <para>If a block instuction is found, then tree list is updated.</para>
        /// <para>As this function is called once per ReadItem() is stores the previous value in rewind* variables (there is additional optimization that we only copy rewindTree if the tree has changed.</para>
        /// <para>Now when the rewind flag is set, we use the rewind* copies, to move back exactly one item.</para>
        /// </summary>
        /// <param name="token"></param>
        private void UpdateTreeAndRewindBuffer(string token)
        {
            rewindItem = token;
            token = token.Trim();

            if (token == "(")
            {
                rewindTree = new List<string>(tree);
                rewindCurrItem = previousItem;
                tree.Add(previousItem + "(");
                previousItem = "";
            }
            else if (token == ")")
            {
                rewindTree = new List<string>(tree);
                rewindCurrItem = previousItem;
                if (tree.Count > 0) tree.RemoveAt(tree.Count - 1);
                previousItem = token;
            }
            else
            {
                rewindTree = null;
                rewindCurrItem = previousItem;
                previousItem = token;
            }
        }
        #endregion
	}

    public class STFException : Exception
    // STF errors display the last few lines of the STF file when reporting errors.
    {
        public static void ReportError(STFReader reader, string message)
        {
            Trace.TraceError("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message);
        }
        public static void ReportWarning(STFReader reader, string message)
        {
            Trace.TraceWarning("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message);
        }
        public static void ReportInformation(STFReader reader, Exception error)
        {
            Trace.TraceError("STF error in {0}:line {1}", reader.FileName, reader.LineNumber);
            Trace.WriteLine(error);
        }

        public STFException(STFReader reader, string message)
            : base(String.Format("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message))
        {
        }
    }
}
