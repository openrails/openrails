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
using System.Collections;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using Microsoft.Xna.Framework;

namespace MSTS
{
	public class STFError: System.Exception
		// STF errors display the last few lines of the STF file when reporting errors.
	{
        public static void Report(STFReader f, string message) { Console.Error.WriteLine("STF Error in " + f.FileName + "\r\n   Line " + f.LineNumber.ToString() + ": " + message); }

        
		public STFError( STFReader f, string message ): base( "STF Error in " + f.FileName + "\r\n   Line " + f.LineNumber.ToString() + ": " + message )
		{
			ProblemFile = f;
		}
		STFReader ProblemFile;
         
	}


	public class STFReader
	{
		StreamReader f;
		public string FileName;  // only needed for error reporting purposes
        public int LineNumber = 1;    // current line number for error reporting
        public string Header;
       
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
                    throw new System.Exception("Problem peeking eof in compressed file.");
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            int c = 0;

            StringBuilder stringText = new StringBuilder("", 1000);

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
                        if (f.Peek() != '+')
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
            UpdateTree(stringText.ToString());
            return stringText.ToString();
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
                return; 
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
                        STFError.Report(this, "Unexpected " + token);
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
                STFError.Report(this, "Ignoring extra data: " + extraTokens);

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
                    STFError.Report(this, "Unexpected end of file");
                else
                    STFError.Report(this, target + " Not Found - instead found " + s);
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
				return int.Parse( token, System.Globalization.NumberStyles.HexNumber );
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message ) ;
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
            catch (System.Exception e)
            {
                STFError.Report(this, e.Message);
                return 0;
            }
        }

		public int ReadInt()
			// Note:  end of file should return FormatException.
			/* Throws:
				IOException An I/O error occurs. 
				STFError			
			*/
		{
			try
			{
                double value = ReadDouble();
				return (int) value;
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message ) ;
                return 0;
			}

		}
		public uint ReadUInt()
			// Note:  end of file should return FormatException.
			/* Throws:
				IOException An I/O error occurs. 
				STFError
			*/
		{
			try
			{
                double value = ReadDouble();
				return (uint) value;
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message );
                return 0;
			}

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
			// TODO, complete parsing of units ie, km, etc - some are done but not all
		{
			double scale = 1.0;
			string token = ReadToken();
            token = token.ToLower();
			int i;
			// Add handling of units
			i = token.IndexOf( "/2" );
			if( i != -1 )
			{
				scale /= 2;
				token = token.Substring( 0,i );
			}
			i = token.IndexOf( "cm" );
			if( i != -1 )
			{
				scale *= 0.01;
				token = token.Substring( 0,i );
			}
			i = token.IndexOf( "mm" );
			if( i != -1 )
			{
				scale *= 0.001;
				token = token.Substring( 0,i );
			}
			i = token.IndexOf( "ft" );
			if( i != -1 )
			{
				scale *= 0.3048;
				token = token.Substring( 0,i );
			}
			i = token.IndexOf( "in" );
			if( i != -1 )
			{
				scale *= 0.0254;
				token = token.Substring( 0,i );
			}
			i = token.IndexOf( "kn" );
			if( i != -1 )
			{
				scale *= 1e3;
				token = token.Substring( 0,i );
			}
            i = token.IndexOf("n");
            if (i != -1)
            {
                scale *= 1e0;
                token = token.Substring(0, i);
            }
            i = token.IndexOf("t");
			if( i != -1 )
			{
				scale *= 1e3;
				token = token.Substring( 0,i ); // return kg
			}
			i = token.IndexOf( "kg" );
			if( i != -1 )
			{
				scale *= 1;
				token = token.Substring( 0,i ); // return kg
			}
            i = token.IndexOf("lb");
            if (i != -1)
            {
                scale *= 0.00045359237;  
                token = token.Substring(0, i); // return kg
            }
            i = token.IndexOf('m');
			if( i != -1 )
				token = token.Substring(0,i );

            i = token.IndexOf(',');   // MSTS ignores a comma at the end of the number
            if (i != -1)
                token = token.Substring(0, i); 
			
			try
			{
				return double.Parse( token, new System.Globalization.CultureInfo( "en-US") ) * scale;
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message );
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
			// Reads a () enclosed int
			/* Throws
					STFError( this, "( Not Found" )
					IOException An I/O error occurs. 
			*/
		{
			try
			{
                double value = ReadDoubleBlock();                
				return (uint)value;
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message );
                return 0;
			}
		}

		public int ReadIntBlock()
			// Reads a () enclosed int
			/* Throws
					STFError( this, ") Not Found" )
					IOException An I/O error occurs. 
			*/
		{
			try
			{
                double value = ReadDoubleBlock();
                return (int)value;
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message );
                return 0;
			}
		}

        public float ReadFloatBlock()
        {
            return (float)ReadDoubleBlock();
        }

		public double ReadDoubleBlock()
			// Reads a () enclosed double
			/* Throws
					STFError - syntax or numeric format
					IOException An I/O error occurs. 
			*/
		{
            VerifyStartOfBlock();
			double result = ReadDouble();
            VerifyEndOfBlock();
			return result;
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
			if( s == ")" )
				return true;  // assume a null block is true
			int i;
			try
			{
				i = int.Parse(s);
			}
			catch( System.Exception e )
			{
				STFError.Report( this, e.Message );
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

        public string ReadDelimitedItem()  // legacy - don't use
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
        {
            string s = "";

            // Peek ahead for a (
            while (true)
            {
                int c = Peek();
                if ( EOF())
                    return s;
                if (c == '(')
                    break;
                if (c > ' ')
                    return s;
                if (c < 0)
                    return s;
                // else it must be white space
                s += (char)ReadChar();
            }

            // We have a bracket, so skip the entire hierarchy
            s += ReadDelimitedToken();  // now read the leading bracket ( 
            int depth = 1;
            while (depth > 0)
            {
                string token = this.ReadDelimitedToken();
                s += token;
                if (token.Trim() == "")
                    throw (new STFError(this, "Missing )"));
                if (token.Trim() == "(")
                    ++depth;
                if (token.Trim() == ")")
                    --depth;
            }
            return s;
        }

        public string ReadDelimitedToken()
        // Read any leading whitespace, then a token and the trailing delimiter
        // TODO - multiline tokens ending with +
        /* Throws:
            IOException An I/O error occurs. 
        */
        // Returned value may include leading and trailing whitespace and quote chars
        {
            int c = 0;

            StringBuilder tokenText = new StringBuilder("", 1000);

            // Read leading whitespace 
            while (true)
            {
                c = ReadChar();
                if ( EOF() ) // EOF
                    break;
                if (c < 0 || c > ' ')
                    break;
                tokenText.Append((char)c);
            }
            // c == -1 or first char of token

            if (c == '"')
            {
                tokenText.Append((char)c);
                // Read the rest of the string token and final delimiting "
                do
                {
                    c = ReadChar();
                    if ( EOF()) // EOF
                        break;
                    tokenText.Append((char)c);
                    if (c == '\\') // escape sequence
                    {
                        c = ReadChar();
                        tokenText.Append((char)c);
                    }
                    else if (c == '"')
                    {
                        // Check for string extender
                        if ( Peek() != '+')
                            break;

                        // Skip over white space to next string
                        tokenText.Append( (char)ReadChar());
                        do
                        {
                            c = ReadChar();
                            if (EOF()) // EOF
                                break;
                            tokenText.Append((char)c);
                        }
                        while (c >= 0 && c <= ' ');

                        // ensure we are at a quote
                        if (c != '"')
                            break;

                    }

                }
                while (true);
            }
            else if (c == '(')
            {
                tokenText.Append((char)c);
            }
            else if (c == ')')
            {
                tokenText.Append((char)c);
            }
            else if ( !EOF() )
            {
                tokenText.Append((char)c);
                // Read the rest of the token and first delimiter character
                do
                {
                    if ( EOF() ) // EOF
                        break;
                    if (Peek() == '(')
                        break;
                    if (Peek() == ')')
                        break;
                    c = ReadChar();
                    tokenText.Append((char)c);
                }
                while (c > ' ');
            }
            UpdateTree(tokenText.ToString());
            return tokenText.ToString();
        }

        /// <summary>
        /// Throw an unknown token exception
        /// </summary>
        /// <param name="token"></param>
        public void ThrowUnknownToken(string token)
        {
            throw new STFError(this, "Unknown token " + token);
        }


		// HIERARCHICAL TREE VIEW OF FILE POSITION

		private StringBuilder tree = new StringBuilder( "", 1000 );
		private int treeLevel = 0;

		public string Tree
		{
			get{ return tree.ToString(); }
		}

        private int IndexOf(StringBuilder tree, char target, int iStart)
        {
            for (int i = iStart; i < tree.Length; ++i)
                if (tree[i] == target)
                    return i;
            return -1;
        }

        private void UpdateTree(string delimitedToken)
		// A delimited token may include leading and trailing whitespace characters

		{
			string token = delimitedToken.Trim();
			if( token == "(" )
			{
				tree.Append( "(" );
				++treeLevel;
			}
			else if( token ==  ")"  )
			{
				int i = -1;
				for( int n = 0; n < treeLevel; ++n )
				{
					++i;
					i = IndexOf(tree,'(',i );
				}
				if( i < 0 )
                    i = 0;  // S/B throw new STFError(this, "Mismatched parenthesis"); but MSTS just ignores these errors so we will also.
				tree.Length = i;       // remove (
				-- treeLevel;
			}
			else
			{
				int n = treeLevel;
				int i = 0;
				while( n-- > 0 )
				{
					i = IndexOf( tree, '(',i );
					++i;
				}
				tree.Length = i;
				tree.Append( token );
			}
		}
	}


}
