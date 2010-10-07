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
	public class STFException : Exception
	// STF errors display the last few lines of the STF file when reporting errors.
	{
		public static void Report(STFReader reader, Exception error)
		{
			Trace.TraceError("STF error in {0}:line {1}", reader.FileName, reader.LineNumber);
			Trace.WriteLine(error);
		}

		public static void ReportError(STFReader reader, string message)
		{
			Trace.TraceError("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message);
		}

		public static void ReportWarning(STFReader reader, string message)
		{
			Trace.TraceWarning("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message);
		}

		public STFException(STFReader reader, string message)
			: base(String.Format("{2} in {0}:line {1}", reader.FileName, reader.LineNumber, message))
		{
		}
	}


	public class STFReader
	{
		StreamReader f;
		public string FileName;  // only needed for error reporting purposes
        public int LineNumber = 1;    // current line number for error reporting
        public string Header;
        private STFReader IncludeReader = null;
       
		public STFReader( string filename )
        {
            f = new StreamReader(filename, true); // TODO,was  System.Text.Encoding.Unicode ); but I found some ASCII files, ie GLOBAL\SHAPES\milemarker.s
            FileName = filename;
            Header = f.ReadLine();
            ++LineNumber;
        }

        /// <summary>
        /// Create from an input stream, 
        /// Assumes header has already been read
        /// Filename is just for error reporting purposes.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="fileName"></param>
        public STFReader(Stream inputStream, string fileName, Encoding encoding )
        {
            FileName = fileName;
            f = new StreamReader( inputStream , encoding ); 
        }

		public void Close(){ f.Close(); }
		 
		private int Peek()
        {
            int c = f.Peek();
            if (c == -1) // I've seen a problem with compressed input streams with a false -1 on peek
            {
                c = f.Read();
                if( c != -1 )
                    throw new InvalidDataException("Problem peeking eof in compressed file.");
            }
            return c; 
        }


        public bool EOF() { return Peek() == -1; }

        /// <summary>
        /// A block is enclosed in brackets, ie ( block data )
        /// Returns true if the next character is the end of block, or end of file
        /// Consumes the closing ")"
        /// </summary>
        /// <returns></returns>
        public bool EndOfBlock()
        {
            int c = PeekPastWhitespace();
            if( c == ')' )
                c = f.Read();
            return c == ')' || c == -1;
        }

        /// <summary>
        /// Read a character, -1 if at end of stream
        /// </summary>
        /// <returns></returns>
		private int ReadChar(){ int c = f.Read(); if( c == '\n' ) ++LineNumber; return c;}

        /// <summary>
        /// Peek ahead to the next non-whitespace character
        /// Returns -1 if end of file is reached
        /// </summary>
        /// <returns></returns>
        public int PeekPastWhitespace()
        {
            // scan ahead and see if the next character is a bracket )
            int c = f.Peek();
            while ( IsEof(c) || IsWhiteSpace(c) ) // skip over eof and white space
            {
                c = ReadChar();
                if (IsEof(c))
                    break;   // break on reading eof 
                c = f.Peek();
            }

            return c;
        }


        private bool IsWhiteSpace(int c)
        {
            return c >= 0 && c <= ' ';
        }

        private bool IsEof(int c)
        {
            return c == -1;
        }

        public string ReadToken()
        {
            return ReadString();
        }

        public string ReadTokenNoComment()
        {
            for (; ; )
            {
                string token = ReadString();
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

        public void RewindToken()
        {
            Debug.Assert(rewindToken != null, "You must called at least one ReadString() between RewindToken() calls", "The current rewind functionality only allows for a single rewind");
            rewindNextStringRead = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            if (rewindNextStringRead)
            {
                Debug.Assert(rewindToken != null, "You must called at least one ReadString() between RewindToken() calls", "The current rewind functionality only allows for a single rewind");
                string token = rewindToken;
                currentToken = rewindCurrToken;
                if (rewindTree != null) tree = rewindTree;
                rewindNextStringRead = false;
                rewindToken = rewindCurrToken = null;
                rewindTree = null;
                return token;
            }

            if (IncludeReader != null)
            {
                string s = IncludeReader.ReadString();
                UpdateTreeAndRewindBuffer(s);
                if (s != "" || !IncludeReader.EOF())
                    return s;
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
                    if (f.Peek() == '(')
                        break;
                    if (f.Peek() == ')')
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
                return ReadString();
            }

            UpdateTreeAndRewindBuffer(result);
            return result;
        }


        /// <summary>
        /// Skip to the end of this block
        /// </summary>
        /// <returns></returns>
        public void SkipRestOfBlock()
        {
            // We are inside a pair of brackets, skip the entire hierarchy to past the end bracket
            int depth = 1;
            while (!EOF() &&  depth > 0)
            {
                string token = ReadToken();
                if (token == "(")
                    ++depth;
                if (token == ")")
                    --depth;
            }
        }


        // We are processing a line like this:
        //            token ( parameters .. )
        // the token has been read.  Now read bracket and up to final bracket.
        // if there isn't a leading bracket, just point to the start of the next token, ie:
        //			  token
        //            token
        // if we reach end of file before finding parameters or another token, leave
        // pointer pointing to end of file char

        /* Throws
            IOException An I/O error occurs. 
        */
        public void SkipBlock()
		{
			string token = ReadToken();  // read the leading bracket ( 
            if (token == ")")   // just in case we are not where we think we are
            {
                RewindToken();
                return;
            }
            // note, even if this isn't a leading bracket, we'll carry on anyway                            
            SkipRestOfBlock();
		}


        /// <summary>
        /// We weren't expecting this block
        /// If its not a comment, issue a warning
        /// and skip the entire block
        /// </summary>
        /// <param name="token"></param>
        public void SkipUnknownBlock(string token)
        {
            if( token.StartsWith( "_" ) )
                SkipBlock();
            else switch( token.ToLower() )
            {
                case "skip":
                case "comment": SkipBlock(); break;
                default: 
                    if (!token.StartsWith("#"))
                        STFException.ReportError(this, "Unexpected " + token);
                    SkipBlock();
                    break;
            }
        }

        public void VerifyStartOfBlock()
        {
            MustMatch("(");
        }

        /// <summary>
        /// We are inside ( a b c )
        /// We expect the next token is ), but if there are more params, skip them and report
        /// </summary>
        public void VerifyEndOfBlock()
        {
            string extraTokens = "";

            // We are inside a pair of brackets, skip the entire hierarchy to past the end bracket
            int depth = 1;
            while (depth > 0)
            {
                string token = ReadToken();
                if (token == "")
                    return;

                if (token == "(")
                    ++depth;
                if (token == ")")
                    --depth;

                if (depth > 0)
                    extraTokens = extraTokens + " " + token;
            }

            if (extraTokens != "" 
                && !extraTokens.StartsWith( "#" ) 
                && !extraTokens.StartsWith( "comment", StringComparison.OrdinalIgnoreCase ) 
                && !extraTokens.StartsWith( "skip", StringComparison.OrdinalIgnoreCase ) 
                )
                STFException.ReportWarning(this, "Ignoring extra data: " + extraTokens);

        }
		

		/// <summary>
		/// Reports error if not a match then continues
		/// </summary>
		/// <param name="target"></param>
		public void MustMatch( string target )
		{
            string s = ReadToken();
            if (s != target)
            {
                if (s == "")
                    STFException.ReportError(this, "Unexpected end of file");
                else
                    STFException.ReportError(this, target + " Not Found - instead found " + s);
            }
		}



		public int ReadHex()
		// Note:  end of file should return FormatException.
		/* Throws:
			IOException An I/O error occurs. 
			STFError			
		*/
		{
			string token = ReadToken();
			try
			{
				return int.Parse(token, System.Globalization.NumberStyles.HexNumber);
			}
			catch (Exception e)
			{
				STFException.Report(this, e);
				return 0;
			}
		}

		public uint ReadFlags()
		{
			string token = ReadToken();
			try
			{
				return uint.Parse(token, System.Globalization.NumberStyles.HexNumber);
			}
			catch (Exception e)
			{
				STFException.Report(this, e);
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

		/// <summary>
		/// Return double, scaled to meters, grams, newtons if needed
		/// </summary>
		/// <returns></returns>
		public double ReadDouble()
		// Note:  end of file should return FormatException.
		/* Throws:
			IOException An I/O error occurs. 
			STFError
		*/
		{
			double scale = 1.0;
			string token = ReadToken();

            if (token == ")")
            {
                STFException.ReportWarning(this, "When expecting a number, we found a ) marker");
                RewindToken();
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
				STFException.Report(this, e);
				return 0;
			}
		}

		public string ReadStringBlock()
			// Reads a () enclosed string
			/* Throws
					STFError( this, "( Not Found" )
					STFError( this, ") Not Found" )
					IOException An I/O error occurs. 
			*/
		{
            VerifyStartOfBlock();
			string s = ReadString();
            VerifyEndOfBlock();
			return s;
		}

        public uint ReadUIntBlock()
        {
            return ReadUIntBlock(false);
        }
        public uint ReadUIntBlock(bool optionalblock)
// Reads a () enclosed int
		/* Throws
				STFError( this, "( Not Found" )
				IOException An I/O error occurs. 
		*/
		{
			try
			{
                double value = ReadDoubleBlock(optionalblock);
				return (uint)value;
			}
			catch (Exception e)
			{
				STFException.Report(this, e);
				return 0;
			}
		}

        public int ReadIntBlock()
        {
            return ReadIntBlock(false);
        }
        public int ReadIntBlock(bool optionalblock)
		// Reads a () enclosed int
		/* Throws
				STFError( this, ") Not Found" )
				IOException An I/O error occurs. 
		*/
		{
			try
			{
				double value = ReadDoubleBlock(optionalblock);
				return (int)value;
			}
			catch (Exception e)
			{
				STFException.Report(this, e);
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
			// Reads a () enclosed double
			/* Throws
					STFError - syntax or numeric format
					IOException An I/O error occurs. 
			*/
		{
            string s = ReadToken();
            if (s == "")
                STFException.ReportError(this, "Unexpected end of file");
            else if (s == ")" && optionalblock)
                RewindToken();
            else if (s == "(")
            {
                double result = ReadDouble();
                VerifyEndOfBlock();
                return result;
            }
            else
                STFException.ReportError(this, "Block Not Found - instead found " + s);

            return 0;
		}

		public bool ReadBoolBlock()
		// Reads a () enclosed bool block
		/* Throws
				STFError - syntax or numeric conversion
				IOException An I/O error occurs. 
		*/
		{
			VerifyStartOfBlock();
			string s = ReadToken();
			if (s == ")")
				return true;  // assume a null block is true
			int i;
			try
			{
				i = int.Parse(s);
			}
			catch (Exception e)
			{
				STFException.Report(this, e);
				return false;
			}
			VerifyEndOfBlock();
			return i != 0;
		}

        public Vector3 ReadVector3Block()
        {
            Vector3 vector = new Vector3();
            VerifyStartOfBlock();
            vector.X = ReadFloat();
            vector.Y = ReadFloat();
            vector.Z = ReadFloat();
            VerifyEndOfBlock();
            return vector;
        }

        /// <summary>
        /// Throw an unknown token exception
        /// </summary>
        /// <param name="token"></param>
        public void ThrowUnknownToken(string token)
        {
            throw new STFException(this, "Unknown token " + token);
        }


		// HIERARCHICAL TREE VIEW OF FILE POSITION AND BUFFER TO ALLOW OF A REWIND OF A SINGLE TOKEN

        private Stack<string> tree = new Stack<string>();
        private string currentToken = "";
        private bool rewindNextStringRead = false;
        private string rewindToken;
        private Stack<string> rewindTree;
        private string rewindCurrToken;

		public string Tree
		{
			get { return String.Join("", tree.ToArray()) + currentToken; }
		}

		private void UpdateTreeAndRewindBuffer(string token)
		{
            rewindToken = token;

            // I am fairly certain that the replacement function ReadString() unlike the obsolete ReadDelimitedItem() does not send leading/trailing whitespace
            // If after testing this assumption is correct, then the line after the assertion can be removed, if I am wrong then remove the assertion
            Debug.Assert(token == token.Trim());
			token = token.Trim();

			if (token == "(")
			{
                rewindTree = new Stack<string>(tree);
                rewindCurrToken = currentToken;
                tree.Push(currentToken + "(");
                currentToken = "";
            }
			else if (token == ")")
			{
                rewindTree = new Stack<string>(tree);
                rewindCurrToken = currentToken;
                Debug.Assert(tree.Count > 0);
                if(tree.Count > 0) tree.Pop();
                currentToken = "";
            }
			else
			{
                rewindTree = null;
                rewindCurrToken = currentToken;
                currentToken = token;
			}
		}
	}
}
