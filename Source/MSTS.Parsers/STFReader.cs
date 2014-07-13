// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

// This file contains code to read MSTS structured unicode text files
// through the class  STFReader.   
// 
// Note:  the SBR classes are more general in that they are capable of reading
//        both unicode and binary compressed data files.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace MSTS.Parsers
{
    // Used for reading data from Structured Text Format (MSTS-style) files.
    // 
    // An STF file is a whitespace delimitered file, taking the format - {item}{whitespace}[repeated].
    //  
    // At its most simple an STF file has the format - {token_item}{whitespace}{data_item}{whitespace}(repeated)
    // Even, more simplisitically every {data_item} can be a {constant_item}
    // 
    //     Example:
    //     name SimpleSTFfile
    //     weight 100
    //     speed 50.25
    //  
    // STF also has a block methodology where a {data_item} following a {token_item} can start with '(' 
    // followed by any number of {data_item}s and closed with a ')'.
    // The STF parser is schema-neutral so the contents of the block are defined in the code that uses STF, not in the STF parser.
    // The STF definition allows that inside a pair of parentheses may be a single {constant_item}, 
    // multiple whitespace delimitered {constant_item}s, or a nested {token_item}{data_item} pair (which could 
    // contain a further nested block recursively).
    // 
    //     Example:
    //     name BlockedSTFfile
    //     root_constant 100
    //     root_block_1
    //     (
    //           nested_block_1_1
    //           (
    //                 1
    //           )
    //           nested_block_1_2 ( 5 )
    //     )
    //     root_block_2
    //     (
    //           1 2 3
    //     )
    //     root_block_3 ( a b c )
    //  
    // Numeric {constant_item}s can include a 'unit' suffix, which is handled in the ReadFloat() function.
    // Within ReadFloat these units are then converted to the standards used throughout OR - meters, newtons, 
    // kilograms, etc..
    // 
    //     Example:
    //     name STFfileWithUnits
    //     weight 100kg
    //     speed 50mph
    //  
    // Whitespaces can be included within any {item} using a double quotation notation.
    // Quoted values also support a trailing addition operator to indicate an append operation of multiple quoted 
    // strings.
    // Although append operations are technically allowed for {token_item}'s this practice is *strongly* 
    // discouraged for readability.
    // 
    //     Example:
    //     simple_token "Data Item with" + " whitespace"
    //     block_token ( "Data " + "Item 1" "Data Item 2" )
    //     "discouraged_" + "token" -1
    //     Error Example:
    //     error1 "You cannot use append suffix to non quoted " + items
    //  
    // The STF format also supports 3 special {token_item}s - include, comment & skip.
    // include - directive contains a filename relative to the current file to include.
    // The include token has the effect of in-lining the defined file into the current document.
    // comment & skip - must be followed by a block which will not be processed in OR.
    //  
    // Finally any token which begins with a '#' character will be ignored, and then the next {data_item} 
    // (constant or block) will not be processed.
    //  
    // NB!!! If a comment/skip/#*/_* is the last {item} in a block, rather than being totally consumed a dummy 
    // "#\u00b6" is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be 
    // ignored).
    // 
    // Here are two examples which use different techniques to read the same STF file:
    // Example 1:
    //
    //     using (STFReader stf = new STFReader(filename, false))
    //         stf.ParseFile(new STFReader.TokenProcessor[] {
    //             new STFReader.TokenProcessor("item_single_constant", ()=>{ float isc = stf.ReadFloat(STFReader.UNITS.None, 0); }),
    //             new STFReader.TokenProcessor("item_single_speed", ()=>{ float iss_mps = stf.ReadFloat(STFReader.UNITS.Speed, 0); }),
    //             new STFReader.TokenProcessor("block_single_constant", ()=>{ float bsc = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
    //             new STFReader.TokenProcessor("block_fixed_format", ()=>{
    //                 stf.MustMatch("(");
    //                 int bff1 = stf.ReadInt(0);
    //                 string bff2 = stf.ReadString();
    //                 stf.SkipRestOfBlock();
    //             }),
    //             new STFReader.TokenProcessor("block_variable_contents", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
    //                 new STFReader.TokenProcessor("subitem", ()=>{ string si = stf.ReadString(); }),
    //                 new STFReader.TokenProcessor("subblock", ()=>{ string sb = stf.ReadStringBlock(""); }),
    //             });}),
    //         });
    //
    //
    // Example 2:
    //
    //        using (STFReader stf = new STFReader(filename, false))
    //            while (!stf.Eof)
    //                switch (stf.ReadItem().ToLower())
    //                {
    //                    case "item_single_constant": float isc = stf.ReadFloat(STFReader.UNITS.None, 0); break;
    //                    case "item_single_speed": float iss_mps = stf.ReadFloat(STFReader.UNITS.Speed, 0); break;
    //                    case "block_single_constant": float bsc = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
    //                    case "block_fixed_format":
    //                        stf.MustMatch("(");
    //                        int bff1 = stf.ReadInt(0);
    //                        string bff2 = stf.ReadString();
    //                        stf.SkipRestOfBlock();
    //                        break;
    //                    case "block_variable_contents":
    //                        stf.MustMatch("(");
    //                        while (!stf.EndOfBlock())
    //                            switch (stf.ReadItem().ToLower())
    //                            {
    //                                case "subitem": string si = stf.ReadString(); break;
    //                                case "subblock": string sb = stf.ReadStringBlock(""); break;
    //                                case "(": stf.SkipRestOfBlock();
    //                            }
    //                        break;
    //                    case "(": stf.SkipRestOfBlock(); break;
    //                }

    /// <exception cref="STFException"><para>
    /// STF reports errors using the exception static members</para><para>
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
        /// <param name="useTree"><para>true - if the consumer is going to use the Tree Property as it's parsing method (MSTS wagons &amp; engines)</para>
        /// <para>false - if Tree is not used which signicantly reduces GC</para></param>
		public STFReader(string filename, bool useTree)
        {
            var path = Path.GetDirectoryName(filename);
            if (Directory.Exists(path))
            {
                streamSTF = new StreamReader(filename, true); // was System.Text.Encoding.Unicode ); but I found some ASCII files, ie GLOBAL\SHAPES\milemarker.s
                FileName = filename;
                SimisSignature = streamSTF.ReadLine();
                LineNumber = 2;
                if (useTree) tree = new List<string>();
            }
            else
            {
                throw new DirectoryNotFoundException(path);
            }
        }
        /// <summary>Use an open stream for STF parsing, this constructor assumes that the SIMIS signature has already been gathered (or there isn't one)
        /// </summary>
        /// <param name="inputStream">Stream that will be parsed.</param>
        /// <param name="fileName">Is only used for error reporting.</param>
        /// <param name="encoding">One of the Encoding formats, defined as static members in Encoding which return an Encoding type.  Eg. Encoding.ASCII or Encoding.Unicode</param>
        /// <param name="useTree"><para>true - if the consumer is going to use the Tree Property as it's parsing method (MSTS wagons &amp; engines)</para>
        /// <para>false - if Tree is not used which signicantly reduces GC</para></param>
        public STFReader(Stream inputStream, string fileName, Encoding encoding, bool useTree)
        {
            // TODO RESTORE Debug.Assert(inputStream.CanSeek);
            FileName = fileName;
            streamSTF = new StreamReader(inputStream , encoding);
            LineNumber = 1;
            if (useTree) tree = new List<string>();
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
                if (!IsEof(PeekPastWhitespace()))
                    STFException.TraceWarning(this, "Expected end of file");
                streamSTF.Close(); streamSTF = null;
                if (includeReader != null)
                    includeReader.Dispose();
                itemBuilder.Length = 0;
                itemBuilder.Capacity = 0;
            }
        }

        /// <summary>Property that returns true when the EOF has been reached
        /// </summary>
        public bool Eof { get { return PeekChar() == -1; } }
        /// <summary>Filename property for the file being parsed - for reporting purposes
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>Line Number property for the file being parsed - for reporting purposes
        /// </summary>
        public int LineNumber { get; private set; }
        /// <summary>SIMIS header read from the first line of the file being parsed
        /// </summary>
        public string SimisSignature { get; private set; }
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
                Debug.Assert(tree != null);
                if ((tree_cache != null) && (!stepbackoneitemFlag))
                    return tree_cache + previousItem;
                else
                {
                    StringBuilder sb = new StringBuilder(256);
                    foreach (string t in (stepbackoneitemFlag) ? stepback.Tree : tree) sb.Append(t);
                    tree_cache = sb.ToString();
                    sb.Append(previousItem);
                    return sb.ToString();
                }
            }
        }

        /// <summary>Returns the next whitespace delimited {item} from the STF file skipping comments, etc.
        /// </summary>
        /// <remarks>
        /// <alert class="important">If a comment/skip/#*/_* ignore block is the last {item} in a block, rather than being totally consumed a dummy '#' is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be ignored).</alert>
        /// </remarks>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadItem()
        {
            return ReadItem(false);
        }

        /// <summary>This is an internal function in STFReader, it returns the next whitespace delimited {item} from the STF file.
        /// </summary>
        /// <remarks>
        /// <alert class="important">If a comment/skip/#*/_* ignore block is the last {item} in a block, rather than being totally consumed a dummy '#' is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be ignored).</alert>
        /// </remarks>
        /// <param name="string_mode">When true normal comment processing is disabled.</param>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadItem( bool string_mode)
        {
            #region If StepBackOneItem() has been called then return the previous output from ReadItem() rather than reading a new token
            if (stepbackoneitemFlag)
            {
                Debug.Assert(stepback.Item != null, "You must called at least one ReadItem() between StepBackOneItem() calls", "The current step back functionality only allows for a single step");
                string item = stepback.Item;
                previousItem = stepback.PrevItem;
                block_depth = stepback.BlockDepth;
                if (stepback.Tree != null) { tree = stepback.Tree; tree_cache = null; }
                stepbackoneitemFlag = false;
                return UpdateTreeAndStepBack(item);
            }
            #endregion

            return ReadItem(false, string_mode);
        }

        /// <summary>Calling this function causes ReadItem() to repeat the last {item} that was read from the STF file
        /// </summary>
        /// <remarks>
        /// <para>The current implementation of StepBackOneItem() only allows for one "step back".</para>
        /// <para>This means that there each call to StepBackOneItem() must have an intervening call to ReadItem().</para>
        /// </remarks>
        public void StepBackOneItem()
        {
            Debug.Assert(!stepbackoneitemFlag, "You must called at least one ReadItem() between StepBackOneItem() calls", "The current step back functionality only allows for a single step");
            stepbackoneitemFlag = true;
        }

        /// <summary>Reports a critical error if the next {item} does not match the target.
        /// </summary>
        /// <param name="target">The next {item} contents we are expecting in the STF file.</param>
        /// <returns>The {item} read from the STF file</returns>
        public void MustMatch(string target)
        {
            if (Eof)
                STFException.TraceWarning(this, "Unexpected end of file instead of " + target);
            else
            {
                string s1 = ReadItem();
                // A single unexpected token leads to a warning; two leads to a fatal error.
                if (!s1.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    STFException.TraceWarning(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
                    string s2 = ReadItem();
                    if (!s2.Equals(target, StringComparison.OrdinalIgnoreCase))
                        throw new STFException(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
                }
            }
        }

        /// <summary>Reports a critical error if the next {item} does not match the target.
        /// Same as MustMatch() but uses ReadItemFromBlock()
        /// </summary>
        /// <param name="target">The next {item} contents we are expecting in the STF file.</param>
        /// <returns>The {item} read from the STF file</returns>
        private void MustMatchFromBlock(string target)
        {
            if (Eof)
                STFException.TraceWarning(this, "Unexpected end of file instead of \"" + target + "\"");
            else
            {
                string s1 = ReadItemFromBlock();
                // A single unexpected token leads to a warning; two leads to a fatal error.
                if (!s1.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    STFException.TraceWarning(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
                    string s2 = ReadItemFromBlock();
                    if (!s2.Equals(target, StringComparison.OrdinalIgnoreCase))
                        throw new STFException(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
                }
            }
        }

        /// <summary>
        /// Shortened version of ReadItem() just to parse rest of block allowing for comment to end of block. 
        /// E.g.:
        ///   Sanding ( 20mph #sanding system is switched off when faster than this speed )
        /// </summary>
        /// <returns></returns>
        private string ReadItemFromBlock()
        {
            int c;
            #region Skip past any leading whitespace characters
            for (; ; )
            {
                c = ReadChar();
                if (IsEof(c)) return UpdateTreeAndStepBack("");
                if (!IsWhiteSpace(c)) break;
            }
            #endregion

            #region Handle # marker
            if (c == '#')
            {
                #region Consume # comment to end of block
                for ( ; ; )
                {
                    c = PeekChar();
                    if (c == ')') break;
                    c = ReadChar();
                    if (IsEof(c))
                    {
                        STFException.TraceWarning(this, "Found a # marker immediately followed by an unexpected EOF.");
                    }
                }
                #endregion
            } 
            #endregion
            
            #region Parse next item
            string item = "";
            for (; ; )
            {
                item += (char)c;
                if (c == ')')
                {
                    UpdateTreeAndStepBack(")");
                    break;
                }
                c = ReadChar();
                if (IsEof(c)) break;
                if (IsWhiteSpace(c)) break;
            }
            #endregion

            return item;
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
            if (includeReader != null)
            {
                var eob = includeReader.EndOfBlock();
                if (includeReader.Eof)
                {
                    includeReader.Dispose();
                    includeReader = null;
                }
                else
                {
                    if (eob) UpdateTreeAndStepBack(")");
                    return eob;
                }
            }
            #region If StepBackOneItem() has been called and that token was a ")" then consume it, and return true;
            if (stepbackoneitemFlag && (stepback.Item == ")"))
            {
                // Consume the step-back end-of-block
                stepbackoneitemFlag = false;
                UpdateTreeAndStepBack(")");
                return true;
            }
            #endregion
            int c = PeekPastWhitespace();
            if (c == ')')
            {
                c = streamSTF.Read(); // consume the end block
                UpdateTreeAndStepBack(")");
            }
            return c == ')' || c == -1;
        }
        /// <summary>Read a block open (, and then consume the rest of the block without processing.
        /// If we find an immediate close ), then produce a warning, and return without consuming the parenthesis.
        /// </summary>
        public void SkipBlock()
		{
			string token = ReadItem(true, false);  // read the leading bracket ( 
            if (token == ")")   // just in case we are not where we think we are
            {
                STFException.TraceWarning(this, "Found a close parenthesis, rather than the expected block of data");
                StepBackOneItem();
                return;
            }
            else if (token != "(")
                throw new STFException(this, "SkipBlock() expected an open block but found a token instead: " + token);
            SkipRestOfBlock();
		}
        /// <summary>Skip to the end of this block, ignoring any nested blocks
        /// </summary>
        public void SkipRestOfBlock()
        {
            if (stepbackoneitemFlag && (stepback.Item == ")"))
            {
                // Consume the step-back end-of-block
                stepbackoneitemFlag = false;
                
                //<CJComment> Following statement commented out until we know why it might be needed.
                // E.g. "engine ( numwheels ( )" warns of missing number correctly and replaces with default.
                // However UpdateTreeAndStepBack() then skips the rest of the "engine (" block which is not the required behaviour. </CJComment>
                
                //UpdateTreeAndStepBack(")");
                return;
            }
            // We are inside a pair of brackets, skip the entire hierarchy to past the end bracket
            int depth = 1;
            while (!Eof && depth > 0)
            {
                string token = ReadItem(true, false);
                if (token == "(")
                    ++depth;
                if (token == ")")
                    --depth;
            }
        }

        /// <summary>Return next whitespace delimited string from the STF file.
        /// </summary>
        /// <remarks>
        /// <alert class="important">This differs from ReadInt in that normal comment processing is disabled.  ie an item that starts with _ is returned and not skipped.</alert>
        /// </remarks>
        /// <returns>The next {string_item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadString()
        {
            return ReadItem(true);
        }

        /// <summary>Read an hexidecimal encoded number {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public uint ReadHex(uint? defaultValue)
        {
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a hex string, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            uint val;
            if (uint.TryParse(item, parseHex, parseNFI, out val)) return val;
            STFException.TraceWarning(this, "Cannot parse the constant hex string " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an signed integer {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public int ReadInt(int? defaultValue)
        {
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }
            int val;
            if (item.Length == 0) return 0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (int.TryParse(item, parseNum, parseNFI, out val)) return val;
            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an unsigned integer {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public uint ReadUInt(uint? defaultValue)
		{
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            uint val;
            if (item.Length == 0) return 0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (uint.TryParse(item, parseNum, parseNFI, out val)) return val;

            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an single precision floating point number {constant_item}
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file, with the suffix normalized to OR units.</returns>
        public float ReadFloat(UNITS validUnits, float? defaultValue)
		{
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            // <CJComment> Considered reading ahead to accommodate units written with a space such as "60 kph" as well as "60kph".
            // However, some values (mostly "time" ones) may be followed by text. Therefore that approach cannot be used consistently 
            // and has been abandoned. </CJComment> 
            
            float val;
            double scale = ParseUnitSuffix(ref item, validUnits);
            if (item.Length == 0) return 0.0f;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (float.TryParse(item, parseNum, parseNFI, out val)) return (scale == 1) ? val : (float)(scale * val);
            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }

        /// <summary>Read an double precision floating point number {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public double ReadDouble(double? defaultValue)
        {
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            double val;
            if (item.Length == 0) return 0.0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (double.TryParse(item, parseNum, parseNFI, out val)) return val;
            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
		}

        /// <summary>Enumeration specifying which units are valid when parsing a numeric constant.
        /// </summary>
        // Additional entries because MSTS has multiple default units, e.g. some speeds in metres/sec and others in miles/hr
        [Flags]
        public enum UNITS
        {
            /// <summary>No unit parsing is done on the {constant_item} - which is obviously fastest
            /// </summary>
            None = 0,

            /// <summary>Combined using an | with other UNITS if the unit is compulsory (compulsory units will slow parsing)
            /// </summary>
            Compulsory = 1 << 0,

            /// <summary>Valid Units: kg, t, lb
            /// <para>Scaled to kilograms.</para>
            /// </summary>
            Mass = 1 << 1,

            /// <summary>Valid Units: m, cm, mm, km, ft, ', in, "
            /// <para>Scaled to meters.</para>
            /// </summary>
            Distance = 1 << 2,

            /// <summary>Valid Units: *(ft^2)
            /// <para>Scaled to square meters.</para>
            /// </summary>
            AreaDefaultFT2 = 1 << 3,

            /// <summary>
            /// Valid Units: gal, l
            /// <para>Scaled to litres.</para>
            /// </summary>
            Volume = 1 << 4,

            /// <summary>Valid Units: *(ft^3)
            /// <para>Scaled to cubic feet.</para>
            /// </summary>
            VolumeDefaultFT3 = 1 << 5,

            /// <summary>
            /// Valid Units: s, m, h
            /// <para>Scaled to secs.</para>
            /// </summary>            
            Time = 1 << 6,

            /// <summary>
            /// Valid Units: s, m, h
            /// <para>Scaled to secs.</para>
            /// </summary>            
            TimeDefaultM = 1 << 7,

            /// <summary>
            /// Valid Units: s, m, h
            /// <para>Scaled to secs.</para>
            /// </summary>            
            TimeDefaultH = 1 << 8,

            /// <summary>
            /// Valid Units: a, amps
            /// <para>Scaled to amps.</para>
            /// </summary>            
            Current = 1 << 9,

            /// <summary>
            /// Valid Units: v, kv
            /// <para>Scaled to v.</para>
            /// </summary>            
            Voltage = 1 << 10,

            /// <summary>Valid Units: lb/h
            /// <para>Scaled to pounds per hour.</para>
            /// </summary>
            MassRateDefaultLBpH = 1 << 11,

            /// <summary>Valid Units: m/s, mph, kph, kmh, km/h
            /// <para>Scaled to meters/second.
            /// See also SpeedMPH </para>
            /// </summary>
            Speed = 1 << 12,

            /// <summary>Valid Units: m/s, mph, kph, kmh, km/h
            /// <para>Scaled to miles/hour.</para>
            /// Similar to UNITS.Speed except default unit is mph.
            /// </summary>
            SpeedDefaultMPH = 1 << 13,

            /// <summary>
            /// Valid Units: Hz, rps, rpm
            /// <para>Scaled to Hz.</para>
            /// </summary>            
            Frequency = 1 << 14,

            /// <summary>Valid Units: n, kn, lbf
            /// <para>Scaled to newtons.</para>
            /// </summary>
            Force = 1 << 15,

            /// <summary>Valid Units: w, kw, hp
            /// <para>Scaled to watts.</para>
            /// </summary>
            Power = 1 << 16,

            /// <summary>Valid Units: n/m
            /// <para>Scaled to newtons/metre.</para>
            /// </summary>
            Stiffness = 1 << 17,

            /// <summary>Valid Units: n/m/s (+ '/m/s' in case the newtons is missed), lbf/mph 
            /// <para>Scaled to newtons/speed(m/s)</para>
            /// </summary>
            Resistance = 1 << 18,

            /// <summary>Valid Units: psi, bar, inhg, kpa
            /// <para>Scaled to pounds per square inch.</para>
            /// </summary>
            PressureDefaultPSI = 1 << 19,

            /// <summary>Valid Units: psi, bar, inhg, kpa
            /// <para>Scaled to pounds per square inch.</para>
            /// Similar to UNITS.Pressure except default unit is inHg.
            /// </summary>
            PressureDefaultInHg = 1 << 20,

            /// <summary>
            /// Valid Units: psi/s, bar/s, inhg/s, kpa/s
            /// <para>Scaled to psi/s.</para>
            /// </summary>            
            PressureRateDefaultPSIpS = 1 << 21,

            /// <summary>
            /// Valid Units: psi/s, bar/s, inhg/s, kpa/s
            /// <para>Scaled to psi/s.</para>
            /// Similar to UNITS.PressureRate except default unit is inHg/s.
            /// </summary>            
            PressureRateDefaultInHgpS = 1 << 22,

            /// <summary>Valid Units: kj/kg, j/g, btu/lb
            /// <para>Scaled to kj/kg.</para>
            /// </summary>
            EnergyDensity = 1 << 23,

            /// <summary>
            /// Valid Units: degc, degf
            /// <para>Scaled to Deg Celsius</para>
            /// </summary>            
            TemperatureDifference = 1 << 24,    // "TemperatureDifference" not "Temperature" as 0'C <> 0'F

            /// <summary>
            /// Valid Units: kgm^2
            /// <para>Scaled to kgm^2.</para>
            /// </summary>            
            RotationalInertia = 1 << 25,

              /// <summary>
            /// Valid Units: N/m/s^2, lbf/mph^2
            /// <para>Scaled to N/m/s^2.</para>
            /// </summary>            
            ResistanceDavisC = 1 << 26,

            // "Any" is used where units cannot easily be specified, such as generic routines for interpolating continuous data from point values.
            // or interpreting locomotive cab attributes from the ORTSExtendedCVF experimental mechanism.
            // "Any" should not be used where the dimensions of a unit are predictable.
            Any = ~Compulsory // All bits set except the Compulsory bit
        }

        /// <summary>This function removes known unit suffixes, and returns a scaler to bring the constant into the standard OR units.
        /// </summary>
        /// <remarks>This function is marked internal so it can be used to support arithmetic processing once the elements are seperated (eg. 5*2m)
        /// </remarks>
        /// <param name="constant">string with suffix (ie "23 mph"), after the function call the suffix is removed.</param>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <returns>The scaler that should be used to multiply the constant to convert into standard OR units.</returns>
        internal double ParseUnitSuffix(ref string constant, UNITS validUnits)
        {
            if (validUnits == UNITS.None)
                return 1;

            // Enclose the prefixed numeric string with beg,end
            int beg, end, i;
            for (beg = end = i = 0; i < constant.Length; end = ++i)
            {
                char c = constant[i];
                if ((i == 0) && (c == '+')) { ++beg; continue; }
                if ((i == 0) && (c == '-')) continue;
                if ((c == '.') || (c == ',')) continue;
                if ((c == 'e') || (c == 'E') && (i < constant.Length - 1))
                {
                    c = constant[i + 1];
                    if ((c == '+') || (c == '-')) { ++i; continue; }
                }
                if ((c < '0') || (c > '9')) break;
            }
            string suffix = "";
            if (i == constant.Length)
            {
                if ((validUnits & UNITS.Compulsory) > 0)
                    STFException.TraceWarning(this, "Missing a suffix for data expecting " + validUnits.ToString() + " units");
            }
            else
            {
                while ((i < constant.Length) && (constant[i] == ' ')) ++i; // skip the spaces

                // Enclose the unit suffix
                int suffixStart = i;
                int suffixLength = constant.Length - suffixStart;

                // Check for an embedded comment in the unit suffix string, ( ie "220kN#est" used in acela.eng ) 
                // This style of comment doesn't work across spaces, so
                //  MaxReleaseRate( 20#Passenger Service )
                // will lead to a warning: 
                //  ")" not found - "Service" found instead. 
                // Should re-write the comment with a space as
                //  MaxReleaseRate( 20 #Passenger Service )
                int commentStart = constant.IndexOf('#', suffixStart);
                if (commentStart != -1)
                    suffixLength = commentStart - suffixStart;
                // Extract the unit suffix
                suffix = constant.Substring(suffixStart, suffixLength).ToLowerInvariant();
                suffix = suffix.Trim();

                // Extract the prefixed numeric string
                constant = constant.Substring(beg, end - beg);
            }
            // Select and return the scalar value
            if ((validUnits & UNITS.Mass) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "kg": return 1;
                    case "lb": return 0.45359237;
                    case "t": return 1e3;   // metric tonne
                    case "t-uk": return 1016.05;
                    case "t-us": return 907.18474;
                }
            if ((validUnits & UNITS.Distance) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "m": return 1;
                    case "cm": return 0.01;
                    case "mm": return 0.001;
                    case "km": return 1e3;
                    case "ft": return 0.3048;
                    case "in": return 0.0254;
                    case "in/2": return 0.0127; // Used to measure wheel radius in half-inches, as sometimes the practice in the tyre industry
                                                // - see trainset\KIHA31\KIHA31a.eng and others
                }
            if ((validUnits & UNITS.AreaDefaultFT2) > 0)
                switch (suffix)
                {
                    case "": return 0.09290304f;
                    case "*(m^2)": return 1.0f;
                    case "m^2": return 1.0f;
                    case "*(ft^2)": return 0.09290304f;
                    case "ft^2": return 0.09290304f;
                }
            if ((validUnits & UNITS.Volume) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "*(ft^3)": return 28.3168;
                    case "ft^3": return 28.3168;
                    case "*(in^3)": return 0.0163871;
                    case "in^3": return 0.0163871;
                    case "*(m^3)": return 1000;
                    case "m^3": return 1000;
                    case "l": return 1;
                    case "g-uk": return 4.54609f;
                    case "g-us": return 3.78541f;
                    case "gal": return 3.78541f;  // US gallons
                    case "gals": return 3.78541f; // US gallons
                }
            if ((validUnits & UNITS.VolumeDefaultFT3) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "*(ft^3)": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "ft^3": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "*(in^3)": return 0.000578703704;
                    case "in^3": return 0.000578703704;
                    case "*(m^3)": return 35.3146667;
                    case "m^3": return 35.3146667;
                    case "l": return 0.0353146667;
                    case "g-uk": return 0.16054372f;
                    case "g-us": return 0.133680556f;
                    case "gal": return 0.133680556f;  // US gallons
                    case "gals": return 0.133680556f; // US gallons
                 }
            if ((validUnits & UNITS.Time) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "s": return 1;
                    case "m": return 60;    // If validUnits == UNITS.Any then "m" for meters will be returned instead of "m" for minutes.
                                            // Use of UNITS.Any is not good practice.
                    case "h": return 3600;
                }
            if ((validUnits & UNITS.TimeDefaultM) > 0)
                switch (suffix)
                {
                    case "": return 60.0;
                    case "s": return 1;
                    case "m": return 60; 
                    case "h": return 3600;
                }
            if ((validUnits & UNITS.TimeDefaultH) > 0)
                switch (suffix)
                {
                    case "": return 3600.0;
                    case "s": return 1;
                    case "m": return 60;
                    case "h": return 3600;
                } 
            if ((validUnits & UNITS.Current) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "amps": return 1;
                    case "a": return 1;
                }
            if ((validUnits & UNITS.Voltage) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "v": return 1;
                    case "kv": return 1000;
                } 
            if ((validUnits & UNITS.MassRateDefaultLBpH) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "lb/h": return 1;  // <CJComment> To be revised when non-metric internal units removed. </CJComment>
                    case "kg/h": return 2.20462;
                    case "g/h": return 0.00220462;
                }
            if ((validUnits & UNITS.Speed) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "m/s": return 1.0;
                    case "mph": return 0.44704;
                    case "kph": return 0.27777778;
                    case "km/h": return 0.27777778;
                    case "kmph": return 0.27777778;
                    case "kmh": return 0.27777778; // Misspelled unit accepted by MSTS, documented in Richter-Realmuto's 
                    // "Manual for .eng- and .wag-files of the MS Train Simulator 1.0". and used in Bernina
                }
            if ((validUnits & UNITS.SpeedDefaultMPH) > 0)
                switch (suffix)
                {
                    case "": return 0.44704;
                    case "m/s": return 1.0;
                    case "mph": return 0.44704;
                    case "kph": return 0.27777778;
                    case "km/h": return 0.27777778;
                    case "kmph": return 0.27777778;
                    case "kmh": return 0.27777778; // Misspelled unit accepted by MSTS, documented in Richter-Realmuto's 
                    // "Manual for .eng- and .wag-files of the MS Train Simulator 1.0". and used in Bernina
                }
            if ((validUnits & UNITS.Frequency) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "hz": return 1;
                    case "rps": return 1;
                    case "rpm": return 1.0 / 60;
                }
            if ((validUnits & UNITS.Force) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "n": return 1;
                    case "kn": return 1e3;
                    case "lbf": return 4.44822162;
                    case "lb": return 4.44822162;
                }
            if ((validUnits & UNITS.Power) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "w": return 1;
                    case "kw": return 1e3;
                    case "hp": return 745.699872;
                }
            if ((validUnits & UNITS.Stiffness) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "n/m": return 1;
                }
            if ((validUnits & UNITS.Resistance) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "n/m/s": return 1;
                    case "ns/m": return 1;
                    case "lbf/mph": return 10.0264321;  // 1 lbf = 4.4822162, 1 mph = 0.44704 mps => 4.4822162 / 0.44704 = 10.0264321
                }
            if ((validUnits & UNITS.PressureDefaultPSI) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "psi": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "bar": return 14.5037738;
                    case "inhg": return 0.4911542;
                    case "kpa": return 0.145037738;
                }
            if ((validUnits & UNITS.PressureDefaultInHg) > 0)
                switch (suffix)
                {
                    case "": return 0.4911542;
                    case "psi": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "bar": return 14.5037738;
                    case "inhg": return 0.4911542;
                    case "kpa": return 0.145037738;
                }
            if ((validUnits & UNITS.PressureRateDefaultPSIpS) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "psi/s": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "inhg/s": return 0.4911542;
                    case "bar/s": return 14.5037738;
                    case "kpa/s": return 0.145;
                }
            if ((validUnits & UNITS.PressureRateDefaultInHgpS) > 0)
                switch (suffix)
                {
                    case "": return 0.4911542;
                    case "psi/s": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "inhg/s": return 0.4911542;
                    case "bar/s": return 14.5037738;
                    case "kpa/s": return 0.145;
                }
            if ((validUnits & UNITS.EnergyDensity) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "kj/kg": return 1;
                    case "j/g": return 1;
                    case "btu/lb": return 2.326f;
                }
            if ((validUnits & UNITS.TemperatureDifference) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "degc": return 1;
                    case "degf": return 100.0/180;
                }
            if ((validUnits & UNITS.RotationalInertia) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                }
            if ((validUnits & UNITS.ResistanceDavisC) > 0)
                switch (suffix)
                {
                    case "": return 1.0;        
                    case "N/m/s^2": return 1;
                    case "lbf/mph^2": return 22.42849;  // 1 lbf = 4.4822162, 1 mph = 0.44704 mps +> 4.4822162 / (0.44704 * 0.44704) = 22.42849
                }            

            STFException.TraceWarning(this, "Found a suffix '" + suffix + "' which could not be parsed as a " + validUnits.ToString() + " unit");
            return 1;
        }

        /// <summary>Read an string constant from the STF format '( {string_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the item is not found in the block.</param>
        /// <returns>The first item inside the STF block.</returns>
        public string ReadStringBlock(string defaultValue)
		{
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue;
            }
            string s = ReadItem();
            if (s == ")" && (defaultValue != null))
            {
                StepBackOneItem();
                return defaultValue;
            }
            if (s == "(")
            {
                string result = ReadString();
                if (result == ")")
                {
                    STFException.TraceWarning(this, "Expected string block; got empty block");
                    return (defaultValue != null) ? defaultValue : "";
                }
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                if (result == "#\u00b6")
                {
                    STFException.TraceWarning(this, "Found a comment when an {constant item} was expected.");
                    return (defaultValue != null) ? defaultValue : result;
                }
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
		}

		/// <summary>Read an hexidecimal encoded number from the STF format '( {int_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a integer constant.</returns>
        public uint ReadHexBlock(uint? defaultValue)
		{
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue.GetValueOrDefault(0);
            }
            string s = ReadItem();
            if (s == ")" && defaultValue.HasValue)
            {
                StepBackOneItem();
                return defaultValue.Value;
            }
            if (s == "(")
            {
                uint result = ReadHex(defaultValue);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }

		/// <summary>Read an integer constant from the STF format '( {int_constant} ... )'
		/// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
		/// <returns>The STF block with the first {item} converted to a integer.</returns>
        public int ReadIntBlock(int? defaultValue)
		{
			if (Eof)
			{
				STFException.TraceWarning(this, "Unexpected end of file");
				return defaultValue.GetValueOrDefault(0);
			}
			string s = ReadItem();
			if (s == ")" && defaultValue.HasValue)
			{
				StepBackOneItem();
				return defaultValue.Value;
			}
			if (s == "(")
			{
                int result = ReadInt(defaultValue);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
				return result;
			}
			STFException.TraceWarning(this, "Block Not Found - instead found " + s);
			return defaultValue.GetValueOrDefault(0);
		}

		/// <summary>Read an unsigned integer constant from the STF format '( {uint_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a unsigned integer.</returns>
        public uint ReadUIntBlock(uint? defaultValue)
        {
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue.GetValueOrDefault(0);
            }
            string s = ReadItem();
            if (s == ")" && defaultValue.HasValue)
            {
                StepBackOneItem();
                return defaultValue.Value;
            }
            if (s == "(")
            {
                uint result = ReadUInt(defaultValue);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }

        /// <summary>Read an single precision constant from the STF format '( {float_constant} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a single precision constant.</returns>
        public float ReadFloatBlock(UNITS validUnits, float? defaultValue)
        {
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue.GetValueOrDefault(0);
            }
            string s = ReadItem();
            if (s == ")" && defaultValue.HasValue)
            {
                StepBackOneItem();
                return defaultValue.Value;
            }
            if (s == "(")
            {
                float result = ReadFloat(validUnits, defaultValue);
                SkipRestOfBlock(); // e.g. to ignore everything after the "30" in
                // SignalAspect ( APPROACH_1 "Approach" SpeedMPH ( 30 SignalFlags ( ASAP ) ) )
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }

        /// <summary>Read an double precision constant from the STF format '( {double_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a double precision value.</returns>
        public double ReadDoubleBlock(double? defaultValue)
		{
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue.GetValueOrDefault(0);
            }
            string s = ReadItem();
            if (s == ")" && defaultValue.HasValue)
            {
                StepBackOneItem();
                return defaultValue.Value;
            }
            if (s == "(")
            {
                double result = ReadDouble(defaultValue);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }

        /// <summary>Reads the first item from a block in the STF format '( {double_constant} ... )' and return true if is not-zero or 'true'
        /// </summary>
        /// <param name="defaultValue">the default value if a item is not found in the block.</param>
        /// <returns><para>true - If the first {item} in the block is non-zero or 'true'.</para>
        /// <para>false - If the first {item} in the block is zero or 'false'.</para></returns>
        public bool ReadBoolBlock(bool defaultValue)
        {
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue;
            }
            string s = ReadItem();
            if (s == ")")
            {
                StepBackOneItem();
                return defaultValue;
            }
            if (s == "(")
            {
                switch (s = ReadItem().ToLower())
                {
                    case "true": SkipRestOfBlock(); return true;
                    case "false": SkipRestOfBlock(); return false;
                    case ")": return defaultValue;
                    default:
                        int v;
                        if (int.TryParse(s, NumberStyles.Any, parseNFI, out v)) defaultValue = (v != 0);
                        SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                        return defaultValue;
                }
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }

        /// <summary>Read a Vector3 object in the STF format '( {X} {Y} {Z} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector3</returns>
        public Vector3 ReadVector3Block(UNITS validUnits, Vector3 defaultValue)
        {
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue;
            }
            string s = ReadItem();
            if (s == ")")
            {
                StepBackOneItem();
                return defaultValue;
            }
            if (s == "(")
            {
                defaultValue.X = ReadFloat(validUnits, defaultValue.X);
                defaultValue.Y = ReadFloat(validUnits, defaultValue.Y);
                defaultValue.Z = ReadFloat(validUnits, defaultValue.Z);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                return defaultValue;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }

		/// <summary>Read a Vector3 object in the STF format '( {X} {Y} ... )'
		/// </summary>
		/// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
		/// <param name="defaultValue">The default vector if any of the values are not specified</param>
		/// <returns>The STF block as a Vector2</returns>
		public Vector2 ReadVector2Block(UNITS validUnits, Vector2 defaultValue)
		{
			if (Eof)
			{
				STFException.TraceWarning(this, "Unexpected end of file");
				return defaultValue;
			}
			string s = ReadItem();
			if (s == ")")
			{
				StepBackOneItem();
				return defaultValue;
			}
			if (s == "(")
			{
				defaultValue.X = ReadFloat(validUnits, defaultValue.X);
				defaultValue.Y = ReadFloat(validUnits, defaultValue.Y);
				SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
				return defaultValue;
			}
			STFException.TraceWarning(this, "Block Not Found - instead found " + s);
			return defaultValue;
		}

		/// <summary>Read a Vector4 object in the STF format '( {X} {Y} {Z} {W} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector4</returns>
        public Vector4 ReadVector4Block(UNITS validUnits, Vector4 defaultValue)
        {
            if (Eof)
            {
                STFException.TraceWarning(this, "Unexpected end of file");
                return defaultValue;
            }
            string s = ReadItem();
            if (s == ")")
            {
                StepBackOneItem();
                return defaultValue;
            }
            if (s == "(")
            {
                defaultValue.X = ReadFloat(validUnits, defaultValue.X);
                defaultValue.Y = ReadFloat(validUnits, defaultValue.Y);
                defaultValue.Z = ReadFloat(validUnits, defaultValue.Z);
                defaultValue.W = ReadFloat(validUnits, defaultValue.W);
                SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                return defaultValue;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }

        /// <summary>Parse an STF file until the EOF, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseFile(TokenProcessor[] processors)
        { // Press F10 'Step Over' to jump to the next token
#line hidden
            while (!Eof)
            {
                string token = ReadItem().ToLower();
                if (token == "(") { SkipRestOfBlock(); continue; }
                foreach (TokenProcessor tp in processors)
                    if (tp.token == token)
#line default
                        tp.processor(); // Press F11 'Step Into' to debug the Processor delegate
            } // Press F10 'Step Over' to jump to the next token
        }
        /// <summary>Parse an STF file until the EOF, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="breakout">A delegate that returns true, if the processing should be halted prematurely</param>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseFile(ParsingBreak breakout, TokenProcessor[] processors)
        { // Press F10 'Step Over' to jump to the next token
#line hidden
            while (!Eof)
            {
#line default
                if (breakout()) break; // Press F11 'Step Into' to debug the Breakout delegate
#if DEBUG
                else { } // Press F10 'Step Over' to jump to the next token
#endif
#line hidden
                string token = ReadItem().ToLower();
                if (token == "(") { SkipRestOfBlock(); continue; }
                foreach (TokenProcessor tp in processors)
                    if (tp.token == token)
#line default
                        tp.processor(); // Press F11 'Step Into' to debug the Processor delegate
            } // Press F10 'Step Over' to jump to the next token
        }
        /// <summary>Parse an STF file until the end of block ')' marker, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseBlock(TokenProcessor[] processors)
        { // Press F10 'Step Over' to jump to the next token
#line hidden
            while (!EndOfBlock())
            {
                string token = ReadItem().ToLower();
                if (token == "(") { SkipRestOfBlock(); continue; }
                foreach (TokenProcessor tp in processors)
                    if (tp.token == token)
#line default
                        tp.processor(); // Press F11 'Step Into' to debug the Processor delegate
            } // Press F10 'Step Over' to jump to the next token
        }
        /// <summary>Parse an STF file until the end of block ')' marker, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="breakout">A delegate that returns true, if the processing should be halted prematurely</param>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseBlock(ParsingBreak breakout, TokenProcessor[] processors)
        { // Press F10 'Step Over' to jump to the next token
#line hidden
            while (!EndOfBlock())
            {
#line default
                if (breakout()) { SkipRestOfBlock(); break; } // Press F11 'Step Into' to debug the Breakout delegate
#if DEBUG
                else { } // Press F10 'Step Over' to jump to the next token
#endif
#line hidden
                string token = ReadItem().ToLower();
                if (token == "(") { SkipRestOfBlock(); continue; }
                foreach (TokenProcessor tp in processors)
                    if (tp.token == token)
#line default
                        tp.processor(); // Press F11 'Step Into' to debug the Processor delegate
            } // Press F10 'Step Over' to jump to the next token
        }
        #region *** Delegate and Structure definitions used by the Parse...() methods.
        /// <summary>This delegate definition is used by the ParseFile and ParseBlock methods, and is called when an associated matching token is found.
        /// </summary>
        public delegate void Processor();
        /// <summary>This delegate definition is used by the ParseFile and ParseBlock methods, and is used to break out of the processing loop prematurely.
        /// </summary>
        /// <returns>true - if the parsing should be aborted prematurely</returns>
        public delegate bool ParsingBreak();
        /// <summary>A structure used to index lambda functions to a lower cased token.
        /// </summary>
        public struct TokenProcessor
        {
            /// <summary>This constructor is used for the arguments to ParseFile and ParseBlock.
            /// </summary>
            /// <param name="t">The lower case token.</param>
            /// <param name="p">A lambda function or delegate that will be called from the Parse...() method.</param>
            [DebuggerStepThrough]
            public TokenProcessor(string t, Processor p) { token = t; processor = p; }
            public string token; public Processor processor;
        }
        #endregion

        /// <summary>The I/O stream for the STF file we are processing
        /// </summary>
        private StreamReader streamSTF;
        /// <summary>includeReader is used recursively in ReadItem() to handle the 'include' token, file include mechanism
        /// </summary>
        private STFReader includeReader;
        /// <summary>Remembers the last returned ReadItem().  If the next {item] is a '(', this is the block name used in the tree.
        /// </summary>
        private string previousItem = "";
        /// <summary>How deep in nested blocks the current parser is
        /// </summary>
        private int block_depth;
        /// <summary>A list describing the hierachy of nested block tokens
        /// </summary>
        private List<string> tree;
        /// <summary>The tree cache is used to minimize the calls to StringBuilder when Tree is called repetively for the same hierachy.
        /// </summary>
        private string tree_cache;

        private static NumberStyles parseHex = NumberStyles.AllowLeadingWhite|NumberStyles.AllowHexSpecifier|NumberStyles.AllowTrailingWhite;
        private static NumberStyles parseNum = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowTrailingWhite;
        private static IFormatProvider parseNFI = NumberFormatInfo.InvariantInfo;
        #region *** StepBack Variables - It is important that all state variables in this STFReader class have a equivalent in the STEPBACK structure
        /// <summary>This flag is set in StepBackOneItem(), and causes ReadItem(), to use the stepback* variables to do an item repeat
        /// </summary>
        private bool stepbackoneitemFlag;
        /// <summary>Internal Structure used to group together the variables used to implement step back functionality.
        /// </summary>
        private struct STEPBACK
        {
            //streamSTF - is not needed for this stepback implementation
            //includeReader - is not needed for this stepback implementation
            /// <summary>The stepback* variables store the previous state, so StepBackOneItem() can jump back on {item}. stepbackItem « ReadItem() return
            /// </summary>
            public string Item;
            /// <summary>The stepback* variables store the previous state, so StepBackOneItem() can jump back on {item}. stepbackCurrItem « previousItem
            /// </summary>
            public string PrevItem;
            /// <summary>The stepback* variables store the previous state, so StepBackOneItem() can jump back on {item}. stepbackTree « tree
            /// <para>This item, is optimized, so when value is null it means stepbackTree was the same as Tree, so we don't create unneccessary memory duplicates of lists.</para>
            /// </summary>
            public List<string> Tree;
            /// <summary>The stepback* variables store the previous state, so StepBackOneItem() can jump back on {item}. BlockDepth « block_depth
            /// </summary>
            public int BlockDepth;
            //tree_cache can just be set to null, so it is re-evaluated from the stepback'd tree state variable if Tree is called
        };
        private STEPBACK stepback = new STEPBACK();
        #endregion

        #region *** Private Class Implementation
        private static bool IsWhiteSpace(int c) { return c >= 0 && c <= ' '; }
        private static bool IsEof(int c) { return c == -1; }
        private int PeekChar()
        {
            return streamSTF.Peek();
        }
        public int PeekPastWhitespace()
        {
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
        
        public void VerifyStartOfBlock()
        {
            MustMatch("(");
        }

        public bool EOF() { return PeekChar() == -1; }


        /// <summary>This is really a local variable in the function ReadItem(...) but it is a class member to stop unnecessary memory re-allocations.
        /// </summary>
        private StringBuilder itemBuilder = new StringBuilder(256);

        /// <summary>Internal Implementation - This is the main function that reads an item from the STF stream.
        /// </summary>
        /// <param name="skip_mode">True - we are in a skip function, and so we don't want to do any special token processing.</param>
        /// <param name="string_mode">True - we are expecting a string, so don't skip comments.</param>
        /// <returns>The next item from the STF file</returns>
        private string ReadItem(bool skip_mode, bool string_mode)
        {
            #region If includeReader exists, then recurse down to get the next token from the included STF file
            if (includeReader != null)
            {
                string item = includeReader.ReadItem( skip_mode, string_mode);
                UpdateTreeAndStepBack(item);
                if ((!includeReader.Eof) || (item.Length > 0)) return item;
                includeReader.Dispose();
                includeReader = null;
            }
            #endregion

            int c;
            #region Skip past any leading whitespace characters
            for (; ; )
            {
                c = ReadChar();
                if (IsEof(c)) return UpdateTreeAndStepBack("");
                if (!IsWhiteSpace(c)) break;
            }
            #endregion

            itemBuilder.Length = 0;
            #region Handle Open and Close Block markers - parentheses
            if (c == '(')
            {
                return UpdateTreeAndStepBack("(");
            }
            else if (c == ')')
            {
                return UpdateTreeAndStepBack(")");
            }
            #endregion
            #region Handle #&_ markers
            else if ((!skip_mode && !string_mode) && ((c == '#') || (c == '_')))
            {
                #region Move on to a whitespace so we can pick up any token starting with a #
                for (; ; )
                {
                    c = PeekChar();
                    if ((c == '(') || (c == ')')) break;
                    c = ReadChar();
                    if (IsEof(c))
                    {
                        STFException.TraceWarning(this, "Found a # marker immediately followed by an unexpected EOF.");
                        return UpdateTreeAndStepBack("");
                    }
                    if (IsWhiteSpace(c)) break;
                }
                #endregion
                #region Skip the comment item or block
                string comment = ReadItem( skip_mode, string_mode);
                if (comment == ")") return comment;
                if (comment == "(") SkipRestOfBlock();
                #endregion

                //If the next thing is end-of-block, we cannot just read it, because StepBackOneItem is not handling
                //this correctly when using 'tree'
                int c2 = PeekPastWhitespace();
                if (c2 == ')')
                {
                    return "#\u00b6";
                }
                string item = ReadItem( skip_mode, string_mode);
                return item; // Now move on to the next token after the commented area
            }
            #endregion
            #region Build Quoted Items - including append operations
            else if (c == '"')
            {
                for (; ; )
                {
                    c = ReadChar();
                    if (IsEof(c))
                    {
                        STFException.TraceWarning(this, "Found an unexpected EOF, while reading an item started with a double-quote character.");
                        return UpdateTreeAndStepBack(itemBuilder.ToString());
                    }
                    if (c == '\\') // escape sequence
                    {
                        c = ReadChar();
                        if (c == 'n') itemBuilder.Append('\n');
                        else if (c == 't') itemBuilder.Append('\t');
                        else itemBuilder.Append((char)c);  // ie \, " etc
                    }
                    else if (c != '"')
                    {
                        itemBuilder.Append((char)c);
                    }
                    else //  end of quotation
                    {
                        // Anything other than a string extender now, means we have finished reading the item
                        if (PeekPastWhitespace() != '+') break;
                        ReadChar(); // Read the '+' character

                        #region Skip past any leading whitespace characters
                        c = (char)PeekPastWhitespace();
                        if (IsEof(c))
                        {
                            STFException.TraceWarning(this, "Found an unexpected EOF, while reading an item started with a double-quote character and followed by the + operator.");
                            return UpdateTreeAndStepBack(itemBuilder.ToString());
                        }
                        #endregion

                        if (c != '"')
                        {
                            if (skip_mode) return "";
                            STFException.TraceWarning(this, "Reading an item started with a double-quote character and followed by the + operator but then the next item must also be double-quoted.");
                            return UpdateTreeAndStepBack(itemBuilder.ToString());
                        }
                        c = ReadChar(); // Read the open quote
                    }
                }
            }
            #endregion
            #region Build Normal Items - delimitered by whitespace, ( and )
            else if (c != -1)
            {
                itemBuilder.Append((char)c);
                for (; ; )
                {
                    c = PeekChar();
                    if ((c == '(') || (c == ')')) break;
                    if (c == '"') // Also delimit by a trailing " in case the leading " is missing. 
                    {
                        c = ReadChar();
                        break;
                    }
                    c = ReadChar();
                    if (IsEof(c)) break;
                    if (IsWhiteSpace(c)) break;
                    if (c == '\\') // escape sequence
                    {
                        c = ReadChar();
                        if (c == 'n') itemBuilder.Append('\n');
                        else if (c == 't') itemBuilder.Append('\t');
                        else itemBuilder.Append((char)c);  // ie \, " etc
                    }
                    else
                    {
                        itemBuilder.Append((char)c);
                    }
                }
            }
            #endregion

            if (skip_mode) return "";
            string result = itemBuilder.ToString();
            if (!string_mode)  // in string mode we don't exclude comments
            {
                switch (result.ToLower())
                {
                    #region Process special token - include
                    case "include":
                        var filename = ReadItem(skip_mode, string_mode);
                        if (filename == "(")
                        {
                            filename = ReadItem(skip_mode, string_mode);
                            SkipRestOfBlock();
                        }
                        includeReader = new STFReader(Path.GetDirectoryName(FileName) + @"\" + filename, false);
                        return ReadItem(skip_mode, string_mode); // Which will recurse down when includeReader is tested
                    #endregion
                    #region Process special token - skip and comment
                    case "skip":
                    case "comment":
                        {
                            #region Skip the comment item or block
                            string comment = ReadItem(skip_mode, string_mode);
                            if (comment == "(") SkipRestOfBlock();
                            #endregion
                            //If the next thing is end-of-block, we cannot just read it, because StepBackOneItem is not handling
                            //this correctly when using 'tree'
                            int c2 = PeekPastWhitespace();
                            if (c2 == ')')
                            {
                                return "#\u00b6";
                            }
                            string item = ReadItem(skip_mode, string_mode);
                            return item; // Now move on to the next token after the commented area
                        }
                    #endregion
                }
            }

            return UpdateTreeAndStepBack(result);
        }
        /// <summary>Internal Implementation
        /// <para>This function is called by ReadItem() for every item read from the STF file (and Included files).</para>
        /// <para>If a block instuction is found, then tree list is updated.</para>
        /// <para>As this function is called once per ReadItem() is stores the previous value in stepback* variables (there is additional optimization that we only copy stepbackTree if the tree has changed.</para>
        /// <para>Now when the stepbackoneitemFlag flag is set, we use the stepback* copies, to move back exactly one item.</para>
        /// </summary>
        /// <param name="token"></param>
        private string UpdateTreeAndStepBack(string token)
        {
            stepback.Item = token;
            token = token.Trim();
            if (token == "(")
            {
                if (tree != null)
                {
                    stepback.Tree = new List<string>(tree);
                    tree.Add(previousItem + "(");
                    tree_cache = null; // The tree has changed, so we need to empty the cache which will be rebuilt if the property 'Tree' is used
                }
                stepback.BlockDepth = block_depth++;
                stepback.PrevItem = previousItem;
                previousItem = "";
            }
            else if (token == ")")
            {
                if (tree != null)
                    {
                    stepback.Tree = new List<string>(tree);
                    if (tree.Count > 0)
                    {
                        tree.RemoveAt(tree.Count - 1);
                        tree_cache = null; // The tree has changed, so we need to empty the cache which will be rebuilt if the property 'Tree' is used
                    }
                }
                stepback.BlockDepth = (block_depth > 0) ? block_depth-- : 0;
                stepback.PrevItem = previousItem;
                previousItem = token;
            }
            else
            {
                stepback.Tree = null; // The tree has not changed so stepback doesn't need any data
                stepback.PrevItem = previousItem;
                previousItem = token;
            }
            return stepback.Item;
        }
        #endregion
	}

    public class STFException : Exception
    {
        public static void TraceWarning(STFReader stf, string message)
        {
            Trace.TraceWarning("{2} in {0}:line {1}", stf.FileName, stf.LineNumber, message);
        }

        public static void TraceInformation(STFReader stf, string message)
        {
            Trace.TraceInformation("{2} in {0}:line {1}", stf.FileName, stf.LineNumber, message);
        }
        
        public STFException(STFReader stf, string message)
            : base(String.Format("{2} in {0}:line {1}\n", stf.FileName, stf.LineNumber, message))
        {
        }
    }
}
