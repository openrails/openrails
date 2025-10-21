// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#region Original STFreader
#if !NEW_READER
namespace Orts.Parsers.Msts
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
    // {STFReader.EndBlockCommentSentinel}, is returned, so if EndOFBlock() returns false, you always get an
    // {item} (which can then just be ignored).
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
        /// <summary>
        /// Returned in lieu of an item for a comment that is the last item in a block.
        /// </summary>
        public const string EndBlockCommentSentinel = "#\u00b6";

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
            streamSTF = new StreamReader(inputStream, encoding);
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
                else if (block_depth != 0)
                    STFException.TraceWarning(this, string.Format("Expected depth 0; got depth {0} at end of file (missing ')'?)", block_depth));
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
        /// <alert class="important">If a comment/skip/#*/_* ignore block is the last {item} in a block, rather than being totally consumed a dummy <see cref="EndBlockCommentSentinel"/> is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be ignored).</alert>
        /// </remarks>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadItem()
        {
            return ReadItem(false);
        }

        /// <summary>This is an internal function in STFReader, it returns the next whitespace delimited {item} from the STF file.
        /// </summary>
        /// <remarks>
        /// <alert class="important">If a comment/skip/#*/_* ignore block is the last {item} in a block, rather than being totally consumed a dummy <see cref="EndBlockCommentSentinel"/> is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be ignored).</alert>
        /// </remarks>
        /// <param name="string_mode">When true normal comment processing is disabled.</param>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        private string ReadItem(bool string_mode)
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

        ///// <summary>Reports a critical error if the next {item} does not match the target.
        ///// Same as MustMatch() but uses ReadItemFromBlock()
        ///// </summary>
        ///// <param name="target">The next {item} contents we are expecting in the STF file.</param>
        ///// <returns>The {item} read from the STF file</returns>
        //private void MustMatchFromBlock(string target)
        //{
        //    if (Eof)
        //        STFException.TraceWarning(this, "Unexpected end of file instead of \"" + target + "\"");
        //    else
        //    {
        //        string s1 = ReadItemFromBlock();
        //        // A single unexpected token leads to a warning; two leads to a fatal error.
        //        if (!s1.Equals(target, StringComparison.OrdinalIgnoreCase))
        //        {
        //            STFException.TraceWarning(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
        //            string s2 = ReadItemFromBlock();
        //            if (!s2.Equals(target, StringComparison.OrdinalIgnoreCase))
        //                throw new STFException(this, "\"" + target + "\" not found - instead found \"" + s1 + "\"");
        //        }
        //    }
        //}

        ///// <summary>
        ///// Shortened version of ReadItem() just to parse rest of block allowing for comment to end of block. 
        ///// E.g.:
        /////   Sanding ( 20mph #sanding system is switched off when faster than this speed )
        ///// </summary>
        ///// <returns></returns>
        //private string ReadItemFromBlock()
        //{
        //    int c;
        //    #region Skip past any leading whitespace characters
        //    for (; ; )
        //    {
        //        c = ReadChar();
        //        if (IsEof(c)) return UpdateTreeAndStepBack("");
        //        if (!IsWhiteSpace(c)) break;
        //    }
        //    #endregion

        //    #region Handle # marker
        //    if (c == '#')
        //    {
        //        #region Consume # comment to end of block
        //        for ( ; ; )
        //        {
        //            c = PeekChar();
        //            if (c == ')') break;
        //            c = ReadChar();
        //            if (IsEof(c))
        //            {
        //                STFException.TraceWarning(this, "Found a # marker immediately followed by an unexpected EOF.");
        //            }
        //        }
        //        #endregion
        //    } 
        //    #endregion

        //    #region Parse next item
        //    string item = "";
        //    for (; ; )
        //    {
        //        item += (char)c;
        //        if (c == ')')
        //        {
        //            UpdateTreeAndStepBack(")");
        //            break;
        //        }
        //        c = ReadChar();
        //        if (IsEof(c)) break;
        //        if (IsWhiteSpace(c)) break;
        //    }
        //    #endregion

        //    return item;
        //}

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
                if (eob) UpdateTreeAndStepBack(")");
                if (includeReader.Eof)
                {
                    includeReader.Dispose();
                    includeReader = null;
                }
                else
                {
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

            if (item.Length == 0)
                return 0x0;
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
        public enum UNITS : ulong
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

            /// <summary>Valid Units: psi, bar, inhg, cmhg, kpa
            /// <para>Scaled to pounds per square inch.</para>
            /// </summary>
            PressureDefaultPSI = 1 << 19,

            /// <summary>Valid Units: psi, bar, inhg, kpa
            /// <para>Scaled to pounds per square inch.</para>
            /// Similar to UNITS.Pressure except default unit is inHg.
            /// </summary>
            PressureDefaultInHg = 1 << 20,

            /// <summary>
            /// Valid Units: psi/s, psi/min, bar/s, bar/min, inhg/s, kpa/s
            /// <para>Scaled to psi/s.</para>
            /// </summary>            
            PressureRateDefaultPSIpS = 1 << 21,

            /// <summary>
            /// Valid Units: psi/s, psi/min, bar/s, bar/min, inhg/s, kpa/s
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
            /// Valid Units: Nm/s^2, lbf/mph^2
            /// <para>Scaled to N/m/s^2.</para>
            /// </summary>            
            ResistanceDavisC = 1 << 26,

            /// <summary>
            /// Valid Units: degc, degf
            /// <para>Scaled to Deg Celsius</para>
            /// </summary>            
            Temperature = 1 << 27,    // "Temperature", note above TemperatureDifference, is different

            /// <summary>
            /// Valid Units: deg, rad
            /// <para>Scaled to Radians</para>
            /// </summary>            
            Angle = 1 << 28,

            /// <summary>
            /// Valid Units: J, kJ, MJ, Wh, kWh
            /// <para>Scaled to J.</para>
            /// </summary>            
            Energy = 1 << 29,

            /// <summary>Valid Units: n/s, kn/s, lbf/s
            /// <para>Scaled to newtons per second.</para>
            /// </summary>
            ForceRate = 1 << 30,

            /// <summary>Valid Units: w/s, kw/s, hp/s
            /// <para>Scaled to watts per second.</para>
            /// </summary>
            PowerRate = 1ul << 31,

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
                    case "cm/s": return 0.01;
                    case "mm/s": return 0.001;
                    case "mph": return 0.44704;
                    case "ft/s": return 0.3048;
                    case "in/s": return 0.0254;
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
                    case "cm/s": return 0.01;
                    case "mm/s": return 0.001;
                    case "mph": return 0.44704;
                    case "ft/s": return 0.3048;
                    case "in/s": return 0.0254;
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
                    case "lbf/mph": return 9.9503884;  // 1 lbf = 4.4482216, 1 mph = 0.44704 mps => 4.4482216 / 0.44704 = 9.9503884
                }
            if ((validUnits & UNITS.PressureDefaultPSI) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "psi": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "bar": return 14.5037738;
                    case "inhg": return 0.4911542;
                    case "cmhg": return 0.1933672;
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
                    case "psi/min": return 1.0 / 60.0;
                    case "inhg/s": return 0.4911542;
                    case "cmhg/s": return 0.1933672;
                    case "bar/s": return 14.5037738;
                    case "bar/min": return 14.5037738 / 60.0;
                    case "kpa/s": return 0.145;
                }
            if ((validUnits & UNITS.PressureRateDefaultInHgpS) > 0)
                switch (suffix)
                {
                    case "": return 0.4911542; // <PNComment> Is this correct? - It appears to hold inHg values, yet it does no conversion on psi values, and a conversion on inHg values 
                    case "psi/s": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "psi/min": return 1.0 / 60.0;
                    case "inhg/s": return 0.4911542;
                    case "cmhg/s": return 0.1933672;
                    case "bar/s": return 14.5037738;
                    case "bar/min": return 14.5037738 / 60.0;
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
                    case "degf": return 100.0 / 180;
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
                    case "Nm/s^2": return 1;
                    case "lbf/mph^2": return 22.2583849;  // 1 lbf = 4.4482216, 1 mph = 0.44704 mps +> 4.4482216 / (0.44704 * 0.44704) = 22.2583849
                }
            if ((validUnits & UNITS.Temperature) > 0)
            {
                switch (suffix)
                {

                    case "": return 1.0;
                    case "degc": return 1;
                    case "degf":  // For degF we have a complex calculation process that require conversion from a string, calculation of equivalent degC, and then conversion back to a string
                        float TempConstant = Convert.ToSingle(constant);
                        float Temperature = (TempConstant - 32f) * (100f / 180f);
                        constant = Convert.ToString(Temperature);
                        return 1;
                }
            }
            if ((validUnits & UNITS.Angle) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "rad": return 1;
                    case "deg": return 0.0174533;  // 1 deg = 0.0174533 radians
                }
            
            if ((validUnits & UNITS.Energy) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "j": return 1;
                    case "kj": return 1000;
                    case "mj": return 1e6f;
                    case "wh": return 3.6e3f;
                    case "kwh": return 3.6e6f;
                }
            if ((validUnits & UNITS.ForceRate) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "n/s": return 1;
                    case "kn/s": return 1e3;
                    case "lbf/s": return 4.44822162;
                    case "lb/s": return 4.44822162;
                }
            if ((validUnits & UNITS.PowerRate) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "w/s": return 1;
                    case "kw/s": return 1e3;
                    case "hp/s": return 745.699872;
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
                if (result == EndBlockCommentSentinel)
                {
                    STFException.TraceWarning(this, "Found a comment when an {constant item} was expected.");
                    return (defaultValue != null) ? defaultValue : result;
                }
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }

        /// <summary>Read a hexadecimal encoded color from the STF format '( {color_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a color constant.</returns>
        public Color ReadColorBlock(Color? defaultValue)
        {
            var hex = this.ReadHexBlock(STFReader.SwapColorBytes(defaultValue.GetValueOrDefault(Color.Black).PackedValue));
            return new Color() { PackedValue = STFReader.SwapColorBytes(hex) };
        }

        static uint SwapColorBytes(uint color) {
            return (color & 0xFF00FF00) + (byte)(color >> 16) + (uint)((byte)color << 16);
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
                        if (int.TryParse(s, NumberStyles.Any, parseNFI, out v))
                        {
                            defaultValue = (v != 0);
                        }
                        SkipRestOfBlock(); // <CJComment> This call seems poor practice as it discards any tokens _including mistakes_ up to the matching ")". </CJComment>  
                        return defaultValue;
                }
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }

        /// <summary>Read a Vector3 object in the STF format '... {X} {Y} {Z} ...'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector3</returns>
        public Vector3 ReadVector3(UNITS validUnits, Vector3 defaultValue)
        {
            defaultValue.X = ReadFloat(validUnits, defaultValue.X);
            defaultValue.Y = ReadFloat(validUnits, defaultValue.Y);
            defaultValue.Z = ReadFloat(validUnits, defaultValue.Z);
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
        public void ParseBlock(IEnumerable<TokenProcessor> processors)
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

        /// <summary>Parse an entire STF block from a '(' until the end of block ')' marker, using the array of lower case tokens, with a processor delegate/lambda</summary>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseWholeBlock(TokenProcessor[] processors)
        {
            VerifyStartOfBlock();
            ParseBlock(processors);
        }

        /// <summary>
        /// Parse an entire STF block containing a count (int) and repeated sub-blocks of name <param name="blockName" />.
        /// </summary>
        /// <param name="list">A list to receive the sub-blocks</param>
        /// <param name="blockName">The name of the repeated sub-blocks</param>
        /// <param name="constructor">A function which constructs an object for the list</param>
        public void ParseBlockList<T>(ref List<T> list, string blockName, Func<STFReader, T> constructor)
        {
            MustMatch("(");
            var count = ReadInt(null);
            var listForLambda = list = new List<T>(count);
            ParseBlock(new[]
            {
                new TokenProcessor(blockName, () =>
                {
                    if (count-- > 0)
                    {
                        listForLambda.Add(constructor(this));
                    }
                    else
                    {
                        STFException.TraceWarning(this, $"Skipped extra {blockName}");
                    }
                }),
            });
            if (count > 0)
            {
                STFException.TraceWarning(this, $"{count} missing {blockName}");
            }
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

        private static NumberStyles parseHex = NumberStyles.AllowLeadingWhite | NumberStyles.AllowHexSpecifier | NumberStyles.AllowTrailingWhite;
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
                string item = includeReader.ReadItem(skip_mode, string_mode);
                UpdateTreeAndStepBack(item);
                if ((!includeReader.Eof) || (item.Length > 0)) return item;
                includeReader.Dispose();
                includeReader = null;
            }
            #endregion

            int c;
            #region Skip past any leading whitespace characters
            for (;;)
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
                for (;;)
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
                string comment = ReadItem(skip_mode, string_mode);
                if (comment == ")") return comment;
                if (comment == "(") SkipRestOfBlock();
                #endregion

                //If the next thing is end-of-block, we cannot just read it, because StepBackOneItem is not handling
                //this correctly when using 'tree'
                int c2 = PeekPastWhitespace();
                if (c2 == ')')
                    return EndBlockCommentSentinel;
                string item = ReadItem(skip_mode, string_mode);
                return item; // Now move on to the next token after the commented area
            }
            #endregion
            #region Build Quoted Items - including append operations
            else if (c == '"')
            {
                for (;;)
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
                for (;;)
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
                        var purefilename = Path.GetFileName(filename).ToLower();
                        if (purefilename == "[[samename]]")
                            filename = Path.GetDirectoryName(filename) + @"\" + Path.GetFileName(FileName);
                        var includeFileName = Path.GetDirectoryName(FileName) + @"\" + filename;
                        if (!File.Exists(includeFileName))
                            STFException.TraceWarning(this, string.Format("'{0}' not found", includeFileName));
                        includeReader = new STFReader(includeFileName, false);
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
                                return EndBlockCommentSentinel;
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
#endif
#endregion

#region new STF reader
#if NEW_READER
namespace Orts.Parsers.Msts
{
    // known differences with previous STF reader:
    // Some differences are mentioned in the tests: some tests fail on the old reader, but do pass on the new one.
    // If the tests do not describe the requirements correctly, the tests should be updated, and subsequently the implementation.
    //
    // Other differences:
    // * ParseUnitSuffix now returns a float, not a double, but the rest of unit parsing is not changed.
    // * We demand a correct syntax even in comments and includes, so also not possible to skipa
    //      a syntactically wrong section by putting it in comments

    #region Interfaces
    /// <summary>
    /// Interface for classes that have a 'stream' of StfTokens as output
    /// </summary>
    internal interface IStfTokenStream : IDisposable
    {
        /// <summary>
        /// Return the next token from whatever the source it uses.
        /// When the source is empty, it should return and keep on returning an StfToken that is EOF
        /// </summary>
        StfToken NextToken();
    }

    /// <summary>
    /// Interface to enable the mapping of a name (often the filename) to a StreadReader
    /// By having an interface it is possible read real files during normal usage, but use mockups during testing
    /// </summary>
    public interface IStreamReaderFactory
    {
        /// <summary>
        /// This will return a StreamReader. The source of the stream is depending on the given name.
        /// Often this will be a filename. But the interface allows other possibilities
        /// </summary>
        /// <param name="name">The name of the source used for creating a stream reader, often a filename. </param>
        /// <param name="simisSignature">This will contain the value of the simisSignature. Whether a simissignature is being read or not
        /// depends is an implementation choice. If no simisSignature is read, the value should be null
        /// If the simisSignature has been read, it is assumed the stream reader is advanced by 1 line</param>
        StreamReader GetStreamReader(string name, out string simisSignature);
    }

    #endregion

    #region StfToken
    /// <summary>
    /// Single token from the MSTS STF format (Structured Text Format).
    /// Token contains obviously the string, but also the name and lineNumber (at the start of the token)
    /// of the source it came from.
    /// </summary>
    struct StfToken
    {
        /// <summary>The (string) contents of the token</summary>
        public readonly string Contents;
        /// <summary>Name (normally filename) of the source of the token</summary>
        public readonly string SourceName;
        /// <summary>Linenumber in the source of the token, starting at 1. </summary>
        public readonly int SourceLineNumber;
        /// <summary>Is this an end-of-File token?</summary>
        public readonly bool IsEOF;

        /// <summary>
        /// Constructor
        /// </summary>
        public StfToken(string tokenContents, string Name, int lineNumber)
            : this(tokenContents, Name, lineNumber, false)
        { }

        private StfToken(string tokenContents, string Name, int lineNumber, bool atEof)
        {
            Contents = tokenContents;
            SourceName = Name;
            SourceLineNumber = lineNumber;
            IsEOF = atEof;
        }

        /// <summary>Is the token the start of a block?</summary>
        public bool IsBlockStart { get { return String.Equals(Contents, "("); } }
        /// <summary>Is the token the end of a block?</summary>
        public bool IsBlockEnd { get { return String.Equals(Contents, ")"); } }

        /// <summary> Token that describes End-Of-File.
        /// </summary>
        /// <param name="sourceName">Name of the source of the token (which still makes sense at EOF)</param>
        public static StfToken EofToken(string sourceName, int lineNumber)
        {
            return new StfToken(String.Empty, sourceName, lineNumber, true);
        }

        public override string ToString()
        {
            return "token: " + this.Contents;
        }
    }
    #endregion

    #region StfTokenizer
    /// <summary>
    /// Class to tokenize a stream. Tokenize means splitting into tokens, according to the rules of STF format
    /// Most tokens are just strings without white space.
    /// A few tokens are single-character tokens with special meaning: '(', ')', '+'.
    /// Some tokens can consist of literal strings, which are created by surrounding them by quotes '"'
    ///     To be precise, tokens that start with '"' will be taken as literal strings, until the next un-escaped '"'.
    /// There is no further interpretation of the tokens (no semantic analysis).
    /// </summary>
    sealed class StfTokenizer : IStfTokenStream
    {
        /// <summary>The I/O stream for the STF file we are processing
        /// </summary>
        private StreamReader sourceStream;

        /// <summary>This is almost a local variable in the function NextToken(...) but it is a class member to stop unnecessary memory re-allocations.
        /// </summary>
        private StringBuilder itemBuilder = new StringBuilder(256);

        /// <summary>Keeps count of the line number. Line numbers start at 1, for debug</summary>
        private int lineNumber;
        /// <summary>Contains the stream name, normally a fileName, for debug.</summary>
        private string storedStreamName;

        /// <summary>Constructor.
        /// </summary>
        /// <param name="inputReader">The input (reader) stream that has to be split into tokens.
        /// Assumption is that a possible SIMISline has already been read.</param>
        /// <param name="streamName">The name of the stream (normally the filename)</param>
        public StfTokenizer(StreamReader inputReader, string streamName, int currentLineInStream)
        {
            this.sourceStream = inputReader;
            this.lineNumber = currentLineInStream;
            this.storedStreamName = streamName;
            SkipWhiteSpace();
        }

        /// <summary>Return the next token. Obviously this is the main functionality of this class
        /// At EOF the eof token will be returned, repeatedly if so requested.
        /// </summary>
        public StfToken NextToken()
        {
            int lineNumberAtStart = lineNumber;
            itemBuilder.Length = 0;

            int peekChar = sourceStream.Peek();

            if (IsEof(peekChar))
            {
                return StfToken.EofToken(this.storedStreamName, lineNumberAtStart);
            }

            //Check for special char at the beginning
            if (IsSingleCharToken(peekChar))
            {
                return NextTokenSingleChar(lineNumberAtStart);
            }

            if (IsQuoteChar(peekChar))
            {
                return NextTokenQuotedString(lineNumberAtStart);
            }

            return NextTokenNormal(lineNumberAtStart);
        }

        private StfToken NextTokenNormal(int lineNumberAtStart)
        {
            for (; ; )
            {
                int c = ReadChar();
                if (IsEof(c)) break;
                if (IsWhiteSpace(c))
                {
                    SkipWhiteSpace();
                    break;
                }
                if (IsQuoteChar(c))
                {
                    SkipWhiteSpace();
                    break;
                }
                itemBuilder.Append((char)c);

                if (IsSingleCharToken(sourceStream.Peek())) break;
            }
            return CreateToken(lineNumberAtStart);
        }

        private StfToken NextTokenQuotedString(int lineNumberAtStart)
        {
            AppendQuotedStream(lineNumberAtStart);
            while (IsConcatChar(sourceStream.Peek()))
            {
                ReadChar(); // read the '+'
                SkipWhiteSpace();
                int peekChar = sourceStream.Peek();
                if (!IsQuoteChar(peekChar))
                {
                    STFException.TraceWarning(this.storedStreamName, this.lineNumber, "Reading an item started with a double-quote character and followed by the + operator but then the next item must also be double-quoted.");
                    break;
                }
                AppendQuotedStream(this.lineNumber);
            }

            return CreateToken(lineNumberAtStart);
        }

        private void AppendQuotedStream(int lineNumberAtStart)
        {
            ReadChar(); // initial quote
            for (; ; )
            {
                int c = ReadChar();
                if (IsEof(c))
                {
                    STFException.TraceWarning(this.storedStreamName, lineNumberAtStart, "Found an unexpected EOF, while reading an item started with a double-quote character.");
                    break;
                }

                if (IsQuoteChar(c))
                {
                    SkipWhiteSpace();
                    break;
                }

                if (IsEscapeChar(c))
                {
                    HandleEscape();
                }
                else
                {
                    itemBuilder.Append((char)c);
                }
            }

        }

        private void HandleEscape()
        {
            int c = ReadChar();
            if (c == 'n')
            {
                itemBuilder.Append('\n');
            }
            else if (c == 't')
            {
                itemBuilder.Append('\t');
            }
            else
            {
                itemBuilder.Append((char)c);
            }
        }

        private StfToken NextTokenSingleChar(int lineNumberAtStart)
        {
            int c = ReadChar();
            itemBuilder.Append((char)c);
            SkipWhiteSpace();
            return CreateToken(lineNumberAtStart);
        }

        private int ReadChar()
        {
            int c = sourceStream.Read();
            if (c == '\n') lineNumber++;
            return c;
        }

        private void SkipWhiteSpace()
        {
            for (; ; )
            {
                int c = sourceStream.Peek();
                if (IsEof(c)) break;
                if (!IsWhiteSpace(c)) break;
                ReadChar();
            }
        }

        private static bool IsEof(int c) { return c == -1; }
        /// <summary>All characters that have ASCII code equal to or below space are deemed whitespace./// </summary>
        private static bool IsWhiteSpace(int c) { return c >= 0 && c <= ' '; }

        private static bool IsSingleCharToken(int c)
        {
            return c == '(' || c == ')';
        }

        private static bool IsQuoteChar(int c)
        {
            return c == '"';
        }

        private static bool IsEscapeChar(int c)
        {
            return c == '\\';
        }

        private static bool IsConcatChar(int c)
        {
            return c == '+';
        }

        private StfToken CreateToken(int lineNumberAtStart)
        {
            return new StfToken(itemBuilder.ToString(), storedStreamName, lineNumberAtStart);
        }

        #region IDisposable
        bool hasBeenDisposed;
        /// <summary>
        /// Implements the IDisposable interface so this class can be implemented with the 
        /// 'using(var r = new StfTokenier(...)) {...}' C# statement.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~StfTokenizer()
        {
            this.Dispose(false);
        }

        /// <summary>Releases the resources used by the StfTokenizer
        /// </summary>
        /// <param name="disposing">
        /// <para>true - release managed and unmanaged resources.</para>
        /// <para>false - release only unmanaged resources.</para>
        /// </param>
        void Dispose(bool disposing)
        {
            if (hasBeenDisposed) return;
            if (disposing)
            {
                sourceStream.Dispose();
                sourceStream = null;
            }
            // Free your own state (unmanaged objects).
            // Set large fields to null. 
            itemBuilder.Length = 0;
            itemBuilder.Capacity = 0;
            hasBeenDisposed = true;
        }
        #endregion
    }
    #endregion

    #region Buffering of a token
    public abstract class StfTokenStreamBuffer
    {
        internal StfToken UpcomingTokenFromSource;
        internal StfToken CurrentTokenFromSource;
        internal IStfTokenStream SourceTokenStream;
        
        #region stepback legacy
        private StfToken bufferedTokenFromSource;
        private bool didAStepBack;

        /// <summary>
        /// Get the next token from the source, and buffer it.
        /// </summary>
        internal virtual void MoveToNextSourceToken()
        {
            if (didAStepBack)
            {   // legacy, should be refactored, see below
                this.CurrentTokenFromSource = this.UpcomingTokenFromSource;
                this.UpcomingTokenFromSource = this.bufferedTokenFromSource;
                didAStepBack = false;
                return;
            }
            this.CurrentTokenFromSource = this.UpcomingTokenFromSource;
            this.UpcomingTokenFromSource = this.SourceTokenStream.NextToken();
        }

        public void StepBackOneItem()
        {
            //TODO we do not want to support this.
            //Current usage is one of two things
            // 1: A problem has occurred, but the item that was read was not stored for the error/warning/...
            //      This is simple to refactor: just store the string.
            // 2: There is a check on ')', and the close bracket is not supposed to be read'
            //      This is very similar to what PeekPastWhiteSpace is used for
            //      Probably this means a new method like EndOfBlock(bool consumeToken) should be defined instead.

            // both here as in the former implementation of stepbackoneitem, it is not possible to do a completely safe stepback
            // in the sense that the currentTokenFromSource is now not the correct one.
            // For instance, it is not possible to do stepBackItem twice.
            // It is also not nice to have both stepback and peek methods. 

            // no unit tests for this!

            if (didAStepBack)
            {
                throw new InvalidOperationException("cannot call StepBackOneItem twice without calling some readItem in between");
            }
            bufferedTokenFromSource = UpcomingTokenFromSource;
            UpcomingTokenFromSource = CurrentTokenFromSource;
            didAStepBack = true;
        }
        #endregion

        /// <summary>
        /// Return the next token from whatever the source it uses, and, importantly, after possible pre-processing
        /// When the source is empty, it should return and keep on returning an StfToken that is EOF
        /// </summary>
        internal abstract StfToken NextToken();
    }
    #endregion

    #region Block handling and mustmatch
    public abstract class StfTokenStreamBlockProcessor : StfTokenStreamBuffer 
    {
        /// <summary>Skip to the end of this block, ignoring any nested blocks
        /// </summary>
        public void SkipRestOfBlock()
        {
            int depth = 0;
            StfToken token;
            for (; ; )
            {
                token = NextToken();
                if (token.IsEOF)
                {
                    break;
                }

                if (token.IsBlockEnd)
                {
                    if (depth == 0)
                    {
                        break;
                    }
                    depth--;
                }
                if (token.IsBlockStart)
                {
                    depth++;
                }
            }
        }

        /// <summary>Read a block open (, and then consume the rest of the block without processing.
        /// If we find an immediate close ), then produce a warning, and return without consuming the parenthesis.
        /// </summary>
        public void SkipBlock()
        {
            if (UpcomingTokenFromSource.IsBlockEnd)
            {
                STFException.TraceWarning(UpcomingTokenFromSource, "Found a close parenthesis, rather than the expected block of data");
                return;
            }
            StfToken token = NextToken();
            if (!token.IsBlockStart)
            {
                throw new STFException(token, "SkipBlock() expected an open block but found a token instead: '" + token.Contents + "'");
            }
            SkipRestOfBlock();
        }

        /// <summary>Reports a critical error if the next {item} does not match the target.
        /// This will consume the target
        /// </summary>
        /// <param name="target">The next {item} contents we are expecting in the STF file.</param>
        public void MustMatch(string target)
        {
            for (int matchAttempt = 1; matchAttempt <= 2; matchAttempt++)
            {
                StfToken token = NextToken();
                if (token.IsEOF)
                {
                    STFException.TraceWarning(token, "Unexpected end of file instead of " + target);
                    return;
                }

                if (String.Equals(token.Contents, target, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // A single unexpected token leads to a warning; two leads to a fatal error.
                if (matchAttempt == 1)
                {
                    STFException.TraceWarning(token, "\"" + target + "\" not found - instead found \"" + token.Contents + "\"");
                }
                else
                {
                    throw new STFException(token, "\"" + target + "\" not found - instead found \"" + token.Contents + "\"");
                }
            }

        }

        internal IStreamReaderFactory streamReaderFactory;
        /// <summary>
        /// This is the common method to create the objects to go from a reader into a token stream.
        /// </summary>
        /// <param name="fileName">The fileName of the reader</param>
        /// <param name="inputReader">The stream reader that is to be used</param>
        /// <param name="currentLineInStream">The current line in the stream (in case simis signature has been read)</param>
        /// <returns>The tokenStream to be used to get tokens from the stream</returns>
        internal IStfTokenStream TokenStreamFromReader(string fileName, StreamReader inputReader, int currentLineInStream)
        {
            var tokenizer = new StfTokenizer(inputReader, fileName, currentLineInStream);
            var preprocessor = new StfTokenStreamPreprocessor(tokenizer, fileName, streamReaderFactory);
            return preprocessor;
        }

    }
    #endregion

    #region Preprocessing of comments, include, ...
    class StfTokenStreamPreprocessor : StfTokenStreamBlockProcessor, IStfTokenStream
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceTokenStream">The source stream that needs to be pre-processed</param>
        /// <param name="factory">The factory to be used for getting a streamreader for included files</param>
        public StfTokenStreamPreprocessor(IStfTokenStream sourceTokenStream, string fileName, IStreamReaderFactory factory)
        {
            if (Path.IsPathRooted(fileName))
            {
                this.directoryName = Path.GetDirectoryName(fileName) + @"\";
            }
            this.streamReaderFactory = factory;
            this.SourceTokenStream = sourceTokenStream;
            this.UpcomingTokenFromSource = this.SourceTokenStream.NextToken();
            inSkipMode = false;
        }

        /// <summary>Return the next token. Obviously this is the main functionality of this class
        /// At EOF the eof token will be returned, repeatedly if so requested.
        /// </summary>
        internal override StfToken NextToken()
        {
            // todo I have not found a way to implement IstfTokenStream.NextToken and override StfTokenStreamBlockProcessor.NextToken
            // and at the same time prevent NextToken and IStfTokenStream to be public, other then implementing both separately
            return (this as IStfTokenStream).NextToken();
        }

        StfToken IStfTokenStream.NextToken()
        {
            if (sourceInclude != null)
            {
                StfToken tokenFromInclude = sourceInclude.NextToken();
                if (!tokenFromInclude.IsEOF)
                {
                    return tokenFromInclude;
                }
                DisposeIncludeStreams();
            }

            MoveToNextSourceToken();
            if (CurrentTokenFromSource.IsEOF)
            {
                return CurrentTokenFromSource;
            }

            if (!inSkipMode && IsCommentOrSkip())
            {
                inSkipMode = true;
                SkipBlock();
                inSkipMode = false;
                return NextToken(); // restart parsing
            }

            if (!inSkipMode && StartsWithHashOrUnderscore())
            {
                if (UpcomingTokenFromSource.IsBlockStart)
                {
                    inSkipMode = true;
                    SkipBlock();
                    inSkipMode = false;
                }
                else
                {
                    if (UpcomingTokenFromSource.IsEOF)
                    {
                        STFException.TraceWarning(CurrentTokenFromSource, "Found a # marker immediately followed by an unexpected EOF.");
                    }
                    MoveToNextSourceToken();
                }
                return NextToken(); // restart parsing
            }

            if (!inSkipMode && IsInclude())
            {
                HandleInclude();
                return NextToken(); // restart parsing, which will then use the tokens from the include
            }

            return CurrentTokenFromSource;
        }

        void HandleInclude()
        {
            MustMatch("(");
            StfToken token = NextToken();
            MustMatch(")");
            if (CurrentTokenFromSource.IsEOF)
            {
                throw new STFException(token, "Unexpected end of file during include statement");
            }

            string nameOfIncludeFile = token.Contents;
            string fullNameOfIncludeFile = this.directoryName + nameOfIncludeFile;
            string simisSignature;
            var streamReader = this.streamReaderFactory.GetStreamReader(fullNameOfIncludeFile, out simisSignature);
            int currentLineInStream = (simisSignature == null) ? 1 : 2;
            this.sourceInclude = TokenStreamFromReader(fullNameOfIncludeFile, streamReader, currentLineInStream);
        }

        bool IsCommentOrSkip()
        {
            if (String.Equals("skip", CurrentTokenFromSource.Contents, StringComparison.OrdinalIgnoreCase) ||
                String.Equals("comment", CurrentTokenFromSource.Contents, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        bool StartsWithHashOrUnderscore()
        {
            string currentItem = CurrentTokenFromSource.Contents;
            if (String.IsNullOrEmpty(currentItem))
            {
                return false;
            }
            Char firstChar = currentItem[0];
            if (firstChar == '#' || firstChar == '_')
            {
                return true;
            }
            return false;
        }

        bool IsInclude()
        {
            return String.Equals("include", CurrentTokenFromSource.Contents, StringComparison.OrdinalIgnoreCase);
        }

        #region IDisposable
        bool hasBeenDisposed;
        /// <summary>
        /// Implements the IDisposable interface so this class can be implemented with the 
        /// 'using(var r = new _classname_(...)) {...}' C# statement.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~StfTokenStreamPreprocessor()
        {
            this.Dispose(false);
        }

        /// <summary>Releases the resources used by this class
        /// </summary>
        /// <param name="disposing">
        /// <para>true - release managed and unmanaged resources.</para>
        /// <para>false - release only unmanaged resources.</para>
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (hasBeenDisposed) return;
            if (disposing)
            {
                DisposeIncludeStreams();
                SourceTokenStream.Dispose(); // this is an injection dependency that is not stored anywhere else
                SourceTokenStream = null;
            }
            hasBeenDisposed = true;
        }

        void DisposeIncludeStreams()
        {
            // we own sourceInclude and its raw stream, so we should dispose of them
            if (sourceInclude != null)
            {
                sourceInclude.Dispose();
                sourceInclude = null;
            }
            if (sourceIncludeRaw != null)
            {
                sourceIncludeRaw.Dispose();
                sourceIncludeRaw = null;
            }
        }

        #endregion

        private string directoryName = String.Empty;
        private IStfTokenStream sourceInclude;
        private IStfTokenStream sourceIncludeRaw;
        private bool inSkipMode;

    }
    #endregion

    /// <exception cref="STFException"><para>
    /// STF reports errors using the exception static members</para><para>
    /// There are three broad categories of error</para><list class="bullet">
    /// <listItem><para>Failure - Something which prevents loading from continuing, this throws an unhandled exception and drops out of Open Rails.</para></listItem>
    /// <listItem><para>Error - The data read does not have logical meaning - STFReader does not generate these errors, this is only appropriate STFReader consumers who understand the context of the data being processed</para></listItem>
    /// <listItem><para>Warning - When an error which can be programatically recovered from should be reported back to the user</para></listItem>
    /// </list>
    /// </exception>
    public sealed class STFReader : StfTokenStreamBlockProcessor, IDisposable
    {
        #region Properties
        /// <summary>Property that returns true when the EOF (End-of-file) has been reached </summary>
        public bool Eof { get { return this.UpcomingTokenFromSource.IsEOF; } }
        /// <summary>Property that returns the line number in the file we are reading, at the beginning of the last item that was read
        /// </summary>
        public int LineNumber { get { return CurrentTokenFromSource.SourceLineNumber; } }
        /// <summary>Filename property for the file being parsed - for reporting purposes</summary>
        public string FileName { get { return this.UpcomingTokenFromSource.SourceName; } }
        /// <summary>For some sources (the actual Stf files), the first line is a SIMIS Signature.</summary>
        public string SimisSignature { get; private set; }

        /// <summary>
        /// The next factory for getting a stream reader from a name
        /// </summary>
        /// <remarks>Normally this should be part of the constructor, as a dependency injection. However, we do not
        /// want to change the constructor (legacy, and because in normal use there is only 1 factory), hence this solution.</remarks>
        public static IStreamReaderFactory NextStreamReaderFactory
        {
            get
            {
                if (storedNextStreamReaderFactory == null)
                {
                    return new StreamReaderFactoryFromFile();
                }
                var result = storedNextStreamReaderFactory;
                storedNextStreamReaderFactory = null;
                return result;
            }
            set
            {
                storedNextStreamReaderFactory = value;
            }
        }
        static IStreamReaderFactory storedNextStreamReaderFactory;

        #endregion

        private STFReader()
        {
            // workaround since the factory is not an injection dependence
            this.streamReaderFactory = STFReader.NextStreamReaderFactory;
        }

        #region Constructor
        /// <summary>Open a file, reader the header line, and prepare for STF parsing
        /// </summary>
        /// <param name="fileName">Filename of the STF file to be opened and parsed.</param>
        /// <param name="useTree"><para>true - if the consumer is going to use the Tree Property as it's parsing method (MSTS wagons &amp; engines)</para>
        /// <para>false - if Tree is not used which signicantly reduces GC</para></param>
        public STFReader(string fileName, bool useTree) : this()
        {
            string simisSignatureAsRead;
            var inputReader = this.streamReaderFactory.GetStreamReader(fileName, out simisSignatureAsRead);
            this.SimisSignature = simisSignatureAsRead;
            InitReader(fileName, useTree, inputReader);
        }

        /// <summary>Use an open stream for STF parsing, this constructor assumes that the SIMIS signature has already been gathered (or there isn't one)
        /// </summary>
        /// <param name="inputStream">Stream that will be parsed.</param>
        /// <param name="fileName">Is only used for error reporting.</param>
        /// <param name="encoding">One of the Encoding formats, defined as static members in Encoding which return an Encoding type.  Eg. Encoding.ASCII or Encoding.Unicode</param>
        /// <param name="useTree"><para>true - if the consumer is going to use the Tree Property as it's parsing method (MSTS wagons &amp; engines)</para>
        /// <para>false - if Tree is not used which signicantly reduces GC</para></param>
        public STFReader(Stream inputStream, string fileName, Encoding encoding, bool useTree) : this()
        {
            var inputReader = new StreamReader(inputStream, encoding);
            InitReader(fileName, useTree, inputReader);
        }

        private void InitReader(string fileName, bool useTree, StreamReader inputReader)
        {
            int currentLineInStream = (this.SimisSignature == null) ? 1 : 2;
            var tokenStream = TokenStreamFromReader(fileName, inputReader, currentLineInStream);
            this.SourceTokenStream = tokenStream;

            InitBuffer();
            InitTree(useTree);
        }
        #endregion

        #region IDisposable
        bool hasBeenDisposed;
        /// <summary>
        /// Implements the IDisposable interface so this class can be implemented with the 
        /// 'using(var r = new _classname_(...)) {...}' C# statement.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~STFReader()
        {
            this.Dispose(false);
        }

        /// <summary>Releases the resources used by this class
        /// </summary>
        /// <param name="disposing">
        /// <para>true - release managed and unmanaged resources.</para>
        /// <para>false - release only unmanaged resources.</para>
        /// </param>
        void Dispose(bool disposing)
        {
            if (hasBeenDisposed) return;
            hasBeenDisposed = true;
            if (disposing)
            {
                bool earlyEOF = ! this.Eof;
                this.SourceTokenStream.Dispose();
                this.SourceTokenStream = null;
                if (earlyEOF)
                {   // first we make sure we dispose actions, then we do the warning.
                    STFException.TraceWarning(UpcomingTokenFromSource, "Expected end of file");
                }
            }
        }
        #endregion

        #region Get tokens and buffer it.
        /// <summary>
        /// Initialize the single-token buffer, so that the upcoming token is known
        /// </summary>
        void InitBuffer()
        {
            this.UpcomingTokenFromSource = this.SourceTokenStream.NextToken();
            this.CurrentTokenFromSource = this.UpcomingTokenFromSource; // just a copy to make sure LineNumber is right from the beginning
        }

        internal override void MoveToNextSourceToken()
        {
            base.MoveToNextSourceToken();
            UpdateTree();
        }
        /// <summary>
        /// Get the next token from the source, and buffer it.
        /// </summary>
        internal override StfToken NextToken()
        {
            MoveToNextSourceToken();
            return CurrentTokenFromSource;
        }


        /// <summary>Returns the next whitespace delimited {item} from the STF file skipping comments, etc.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>The next {item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadItem()
        {
            MoveToNextSourceToken();
            return CurrentTokenFromSource.Contents;
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
            if (this.Eof) { return true; }
            if (this.UpcomingTokenFromSource.IsBlockEnd)
            {
                this.ReadItem();
                return true;
            }
            return false;
        }

        #endregion

        #region Tree
        private Stack<string> treeTokens;
        private bool treeIsMaintained;

        private void InitTree(bool useTree)
        {
            this.treeIsMaintained = useTree;
            if (treeIsMaintained)
            {
                this.treeTokens = new Stack<string>();
                this.treeTokens.Push(String.Empty);
            }
        }

        private void UpdateTree()
        {
            // We store the items of a tree in a stack. The stack contains for instance "wagon", "lights"
            // If an open block has been parsed, there might be an empty string on the stack ("wagon", "lights", "")
            // If a close block has been parsed, then the last item on the stack is a ")"
            // Stack should never be empty (to prevent an extra check in the normal case)
            if (!treeIsMaintained) return;

            if (CurrentTokenFromSource.IsBlockStart)
            {
                treeTokens.Push(string.Empty);
            }
            else if (CurrentTokenFromSource.IsBlockEnd)
            {
                if (treeTokens.Count > 0)
                {
                    treeTokens.Pop();
                }
                if (treeTokens.Count > 0)
                {
                    treeTokens.Pop();
                }
                treeTokens.Push(")");
            }
            else if (CurrentTokenFromSource.IsEOF)
            {
                treeTokens.Clear();
                treeTokens.Push(String.Empty);
            }
            else
            {
                treeTokens.Pop();
                treeTokens.Push(CurrentTokenFromSource.Contents);
            }
        }

        /// <summary>Property returning the last a string describing the nested block hierachy.
        /// <para>The string returned is formatted 'rootnode(nestednode(childnode(current_item'.</para>
        /// </summary>
        /// <remarks>
        /// Tree might be an expensive method of reading STF files (especially for the GC) and should be avoided if possible.
        /// However, the amount of items stored just a bit longer on the stack is pretty limited, and it seems unlikely that more
        /// than two of them survive long enough to make it to GC generation 1. Hardest on the GC is simply the fact that the 
        /// whole file needs to be put in a string (one token at a time) anyway.
        /// </remarks>
        public string Tree
        {
            // No caching, in contrast to previous implementation. In practice the tree is only called once anyway.
            // String.Join helps in preventing the need to create a stringbuilder
            get
            {
                System.Diagnostics.Debug.Assert(treeIsMaintained);
                return String.Join("(", treeTokens.Reverse().ToArray());
            }
        }
        #endregion

        #region Units
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
            /// Valid Units: psi/s, psi/min, bar/s, bar/min, inhg/s, kpa/s
            /// <para>Scaled to psi/s.</para>
            /// </summary>            
            PressureRateDefaultPSIpS = 1 << 21,

            /// <summary>
            /// Valid Units: psi/s, psi/min, bar/s, bar/min, inhg/s, kpa/s
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
            /// Valid Units: Nm/s^2, lbf/mph^2
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
        /// <param name="constant">string with suffix (ie "23 mph"), after the function call the suffix is removed.</param>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <returns>The scaler that should be used to multiply the constant to convert into standard OR units.</returns>
        static float ParseUnitSuffix(ref string constant, UNITS validUnits, StfToken tokenForWarnings)
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
                    STFException.TraceWarning(tokenForWarnings, "Missing a suffix for data expecting " + validUnits.ToString() + " units");
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
                    case "": return 1.0f;
                    case "kg": return 1;
                    case "lb": return 0.45359237f;
                    case "t": return 1e3f;   // metric tonne
                    case "t-uk": return 1016.05f;
                    case "t-us": return 907.18474f;
                }
            if ((validUnits & UNITS.Distance) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "m": return 1;
                    case "cm": return 0.01f;
                    case "mm": return 0.001f;
                    case "km": return 1e3f;
                    case "ft": return 0.3048f;
                    case "in": return 0.0254f;
                    case "in/2": return 0.0127f; // Used to measure wheel radius in half-inches, as sometimes the practice in the tyre industry
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
                    case "": return 1.0f;
                    case "*(ft^3)": return 28.3168f;
                    case "ft^3": return 28.3168f;
                    case "*(in^3)": return 0.0163871f;
                    case "in^3": return 0.0163871f;
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
                    case "": return 1.0f;
                    case "*(ft^3)": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "ft^3": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "*(in^3)": return 0.000578703704f;
                    case "in^3": return 0.000578703704f;
                    case "*(m^3)": return 35.3146667f;
                    case "m^3": return 35.3146667f;
                    case "l": return 0.0353146667f;
                    case "g-uk": return 0.16054372f;
                    case "g-us": return 0.133680556f;
                    case "gal": return 0.133680556f;  // US gallons
                    case "gals": return 0.133680556f; // US gallons
                }
            if ((validUnits & UNITS.Time) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "s": return 1;
                    case "m": return 60;    // If validUnits == UNITS.Any then "m" for meters will be returned instead of "m" for minutes.
                    // Use of UNITS.Any is not good practice.
                    case "h": return 3600;
                }
            if ((validUnits & UNITS.TimeDefaultM) > 0)
                switch (suffix)
                {
                    case "": return 60.0f;
                    case "s": return 1;
                    case "m": return 60;
                    case "h": return 3600;
                }
            if ((validUnits & UNITS.TimeDefaultH) > 0)
                switch (suffix)
                {
                    case "": return 3600.0f;
                    case "s": return 1f;
                    case "m": return 60f;
                    case "h": return 3600f;
                }
            if ((validUnits & UNITS.Current) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "amps": return 1;
                    case "a": return 1;
                }
            if ((validUnits & UNITS.Voltage) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "v": return 1;
                    case "kv": return 1000;
                }
            if ((validUnits & UNITS.MassRateDefaultLBpH) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "lb/h": return 1;  // <CJComment> To be revised when non-metric internal units removed. </CJComment>
                    case "kg/h": return 2.20462f;
                    case "g/h": return 0.00220462f;
                }
            if ((validUnits & UNITS.Speed) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "m/s": return 1.0f;
                    case "mph": return 0.44704f;
                    case "kph": return 0.27777778f;
                    case "km/h": return 0.27777778f;
                    case "kmph": return 0.27777778f;
                    case "kmh": return 0.27777778f; // Misspelled unit accepted by MSTS, documented in Richter-Realmuto's 
                    // "Manual for .eng- and .wag-files of the MS Train Simulator 1.0". and used in Bernina
                }
            if ((validUnits & UNITS.SpeedDefaultMPH) > 0)
                switch (suffix)
                {
                    case "": return 0.44704f;
                    case "m/s": return 1.0f;
                    case "mph": return 0.44704f;
                    case "kph": return 0.27777778f;
                    case "km/h": return 0.27777778f;
                    case "kmph": return 0.27777778f;
                    case "kmh": return 0.27777778f; // Misspelled unit accepted by MSTS, documented in Richter-Realmuto's 
                    // "Manual for .eng- and .wag-files of the MS Train Simulator 1.0". and used in Bernina
                }
            if ((validUnits & UNITS.Frequency) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "hz": return 1;
                    case "rps": return 1;
                    case "rpm": return 1.0f / 60f;
                }
            if ((validUnits & UNITS.Force) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "n": return 1;
                    case "kn": return 1e3f;
                    case "lbf": return 4.44822162f;
                    case "lb": return 4.44822162f;
                }
            if ((validUnits & UNITS.Power) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "w": return 1;
                    case "kw": return 1e3f;
                    case "hp": return 745.699872f;
                }
            if ((validUnits & UNITS.Stiffness) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "n/m": return 1;
                }
            if ((validUnits & UNITS.Resistance) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "n/m/s": return 1;
                    case "ns/m": return 1;
                    case "lbf/mph": return 9.9503884;  // 1 lbf = 4.4482216, 1 mph = 0.44704 mps => 4.4482216 / 0.44704 = 9.9503884
                }
            if ((validUnits & UNITS.PressureDefaultPSI) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "psi": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "bar": return 14.5037738f;
                    case "inhg": return 0.4911542f;
                    case "kpa": return 0.145037738f;
                }
            if ((validUnits & UNITS.PressureDefaultInHg) > 0)
                switch (suffix)
                {
                    case "": return 0.4911542f;
                    case "psi": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "bar": return 14.5037738f;
                    case "inhg": return 0.4911542f;
                    case "kpa": return 0.145037738f;
                }
            if ((validUnits & UNITS.PressureRateDefaultPSIpS) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "psi/s": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "psi/min": return 1.0 / 60.0;
                    case "inhg/s": return 0.4911542f;
                    case "bar/s": return 14.5037738f;
                    case "bar/min": return 14.5037738 / 60.0;
                    case "kpa/s": return 0.145f;
                }
            if ((validUnits & UNITS.PressureRateDefaultInHgpS) > 0)
                switch (suffix)
                {
                    case "": return 0.4911542f;
                    case "psi/s": return 1;  // <CJComment> Factors to be revised when non-metric internal units removed. </CJComment>
                    case "psi/min": return 1.0 / 60.0;
                    case "inhg/s": return 0.4911542f;
                    case "bar/s": return 14.5037738f;
                    case "bar/min": return 14.5037738 / 60.0;
                    case "kpa/s": return 0.145f;
                }
            if ((validUnits & UNITS.EnergyDensity) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "kj/kg": return 1;
                    case "j/g": return 1;
                    case "btu/lb": return 2.326f;
                }
            if ((validUnits & UNITS.TemperatureDifference) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                    case "degc": return 1;
                    case "degf": return 100.0f / 180f;
                }
            if ((validUnits & UNITS.RotationalInertia) > 0)
                switch (suffix)
                {
                    case "": return 1.0f;
                }
            if ((validUnits & UNITS.ResistanceDavisC) > 0)
                switch (suffix)
                {
                    case "": return 1.0;
                    case "Nm/s^2": return 1;
                    case "lbf/mph^2": return 22.2583849;  // 1 lbf = 4.4482216, 1 mph = 0.44704 mps +> 4.4482216 / (0.44704 * 0.44704) = 22.2583849
                }

            STFException.TraceWarning(tokenForWarnings, "Found a suffix '" + suffix + "' which could not be parsed as a " + validUnits.ToString() + " unit");
            return 1;
        }

        #endregion

        #region Reading values, numbers, ...
        #region Private methods for common functionality. Here all the real work goes on
        private static NumberStyles parseNum = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowTrailingWhite;
        private static NumberStyles parseHex = NumberStyles.AllowLeadingWhite | NumberStyles.AllowHexSpecifier | NumberStyles.AllowTrailingWhite;
        private static IFormatProvider parseNFI = NumberFormatInfo.InvariantInfo;

        delegate bool TryParseCode<T>(string item, out T parsedValue) where T : struct;
        delegate T ReadValueCode<T, U>(U defaultValue);

        /// <summary>Shared method to read a single value, , and return it as type T
        /// Actual parsing is deferred to parseCode, so here only getting thes string, the giving the proper warnings/errors
        /// in case something is wrong.
        /// </summary>
        /// <typeparam name="T">The type of the item that needs to be read</typeparam>
        /// <param name="description">description of the type, to be used in warnings to the user</param>
        /// <param name="defaultValue">The default value as given by the user (might be null)</param>
        /// <param name="parseCode">The code that actual parses the string that was read and generate a wanted value</param>
        /// <returns>The value in the block as a T</returns>
        private T ReadValueStruct<T>(string description, T? defaultValue, TryParseCode<T> parseCode) where T : struct
        {
            if (UpcomingTokenFromSource.IsBlockEnd)
            {
                STFException.TraceWarning(UpcomingTokenFromSource,
                    String.Format(System.Globalization.CultureInfo.CurrentCulture,
                                    "When expecting a {0}, we found a ) marker. Using the default {1}", description, defaultValue));
                return defaultValue.GetValueOrDefault(default(T));
            }
            MoveToNextSourceToken();
            string item = CurrentTokenFromSource.Contents;
            if (String.IsNullOrEmpty(item))
            {
                return default(T);
            }
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            T parsedValue;
            if (parseCode.Invoke(item, out parsedValue)) return parsedValue;

            STFException.TraceWarning(CurrentTokenFromSource, "Cannot parse the constant number " + item);

            return defaultValue.GetValueOrDefault(default(T));
        }

        /// <summary>Shared method to read a block and give the proper warnings/errors.
        /// The actual reading in the block is deferred to codeDoingReading, so here just the handling of the block around it 
        /// </summary>
        /// <typeparam name="T">The type of the item that needs to be read</typeparam>
        /// <param name="defaultValue">The default value as given by the user (might be null)</param>
        /// <param name="codeDoingReading">The code that actual reads the value in the block</param>
        /// <returns>The value in the block as a T</returns>
        private T ReadBlockStruct<T>(T? defaultValue, ReadValueCode<T, T?> codeDoingReading) where T : struct
        {
            return ReadBlock<T, T?>(
                defaultValue,
                defaultValue.GetValueOrDefault(default(T)),
                codeDoingReading);
        }

        /// <summary>Shared method to read a block and give the proper warnings/errors.
        /// The actual reading in the block is deferred to codeDoingReading, so here just the handling of the block around it
        /// </summary>
        /// <typeparam name="T">The type of the item that needs to be read</typeparam>
        /// <typeparam name="NullableT">T? for structs, T itself for references</typeparam>
        /// <param name="defaultValue">The default value as given by the user (might be null)</param>
        /// <param name="valueOfDefault">Either the given defaultValue or a type default (so never null)</param>
        /// <param name="codeDoingReading">The code that actual reads the value in the block</param>
        /// <returns>The value in the block as a T</returns>
        private T ReadBlock<T, NullableT>(NullableT defaultValue, T valueOfDefault, ReadValueCode<T, NullableT> codeDoingReading)
        {
            if (UpcomingTokenFromSource.IsEOF)
            {
                STFException.TraceWarning(CurrentTokenFromSource, "Unexpected end of file");
                return valueOfDefault;
            }
            if (UpcomingTokenFromSource.IsBlockEnd && (defaultValue != null))
            {
                return valueOfDefault;
            }
            MoveToNextSourceToken();
            if (CurrentTokenFromSource.IsBlockStart)
            {
                T result = codeDoingReading.Invoke(defaultValue);
                this.SkipRestOfBlock();
                return result;
            }
            
            STFException.TraceWarning(CurrentTokenFromSource, "Block not found - instead found " + CurrentTokenFromSource.Contents);
            return valueOfDefault;
        }
        #endregion

        #region Reading values itself
        /// <summary>Return next whitespace delimited string from the STF file.
        /// </summary>
        /// <remarks>
        /// <alert class="important">This differs from ReadInt in that normal comment processing is disabled.  ie an item that starts with _ is returned and not skipped.</alert>
        /// </remarks>
        /// <returns>The next {string_item} from the STF file, any surrounding quotations will be not be returned.</returns>
        public string ReadString()
        {
            MoveToNextSourceToken();
            return CurrentTokenFromSource.Contents;
        }

        /// <summary>Read an signed integer {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public int ReadInt(int? defaultValue)
        {
            return ReadValueStruct<int>("int", defaultValue, (string item, out int parsedValue) => int.TryParse(item, parseNum, parseNFI, out parsedValue));
        }

        /// <summary>Read an unsigned integer {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public uint ReadUInt(uint? defaultValue)
        {
            return ReadValueStruct<uint>("uint", defaultValue, (string item, out uint parsedValue) => uint.TryParse(item, parseNum, parseNFI, out parsedValue));
        }

        /// <summary>Read an hexidecimal encoded number {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public uint ReadHex(uint? defaultValue)
        {
            return ReadValueStruct<uint>("hex", defaultValue, (string item, out uint parsedValue) => uint.TryParse(item, parseHex, parseNFI, out parsedValue));
        }

        /// <summary>Read an double precision floating point number {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        public double ReadDouble(double? defaultValue)
        {
            return ReadValueStruct<double>("double", defaultValue, (string item, out double parsedValue) => double.TryParse(item, parseNum, parseNFI, out parsedValue));
        }

        /// <summary>Read a boolean {constant_item}
        /// </summary>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file.</returns>
        private bool ReadBool(bool? defaultValue)
        {
            // ReadBool is different compared to other ReadRoutines:
            // * We do not want as many warnings, empty boolblocks are used heavily, e.g. global tsection.dat
            // * We want to parse some ints as bool as well.
            // * It is not public
            // Therefore, manual implementation, even though parts are similar to readValueStruct
            bool actualDefault = defaultValue ?? false;

            if (UpcomingTokenFromSource.IsBlockEnd)
            {
                return defaultValue.GetValueOrDefault();
            }

            MoveToNextSourceToken();
            string item = CurrentTokenFromSource.Contents;
            if (String.IsNullOrEmpty(item))
            {
                return actualDefault;
            }

            switch (item)
            {
                case "true":
                    {
                        return true;
                    }
                case "false":
                    {
                        return false;
                    }
                default:
                    int parsedInt;
                    if (int.TryParse(item, NumberStyles.Any, parseNFI, out parsedInt))
                    {
                        return (parsedInt != 0);
                    }
                    return actualDefault;
            }
        }

        /// <summary>Read a single precision floating point number {constant_item}
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file, with the suffix normalized to OR units.</returns>
        public float ReadFloat(UNITS validUnits, float? defaultValue)
        {
            return ReadValueStruct<float>("float", defaultValue, (string item, out float returnValue) =>
            {
                float scale = ParseUnitSuffix(ref item, validUnits, new StfToken());
                float parsedValue;
                bool succeeded = float.TryParse(item, parseNum, parseNFI, out parsedValue);
                returnValue = succeeded ? scale * parsedValue : 1f;
                return succeeded;
            });
        }

        #endregion

        #region Reading blocks with values
        /// <summary>Read an string constant from the STF format '( {string_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the item is not found in the block.</param>
        /// <returns>The first item inside the STF block.</returns>
        public string ReadStringBlock(string defaultValue)
        {
            return ReadBlock<string, string>(defaultValue, defaultValue, x => ReadString());
        }

        /// <summary>Read an integer constant from the STF format '( {int_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a integer.</returns>
        public int ReadIntBlock(int? defaultValue)
        {
            return ReadBlockStruct<int>(defaultValue, x => ReadInt(x));
        }

        /// <summary>Read an unsigned integer constant from the STF format '( {uint_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a unsigned integer.</returns>
        public uint ReadUIntBlock(uint? defaultValue)
        {
            return ReadBlockStruct<uint>(defaultValue, x => ReadUInt(x));
        }

        /// <summary>Read a hexidecimal encoded number from the STF format '( {int_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to an unsigned integer constant.</returns>
        public uint ReadHexBlock(uint? defaultValue)
        {
            return ReadBlockStruct<uint>(defaultValue, x => ReadHex(x));
        }

        /// <summary>Read a double precision constant from the STF format '( {double_constant} ... )'
        /// </summary>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a double precision value.</returns>
        public double ReadDoubleBlock(double? defaultValue)
        {
            return ReadBlockStruct<double>(defaultValue, x => ReadDouble(x));
        }

        /// <summary>Reads the first item from a block in the STF format '( {bool_constant} ... )' and
        /// return true if is a non-zero integer or 'true'
        /// </summary>
        /// <param name="defaultValue">the default value if a item is not found in the block.</param>
        /// <returns><para>true - If the first {item} in the block is non-zero or 'true'.</para>
        /// <para>false - If the first {item} in the block is zero or 'false'.</para></returns>
        public bool ReadBoolBlock(bool defaultValue)
        {
            return ReadBlockStruct<bool>(defaultValue, x => ReadBool(x));
        }

        /// <summary>Read an single precision constant from the STF format '( {float_constant} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a single precision constant.</returns>
        public float ReadFloatBlock(UNITS validUnits, float? defaultValue)
        {
            return ReadBlockStruct<float>(defaultValue, x => ReadFloat(validUnits, x));
        }

        /// <summary>Read a Vector2 object in the STF format '( {X} {Y} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector2</returns>
        public Vector2 ReadVector2Block(UNITS validUnits, Vector2 defaultValue)
        {
            return ReadBlockStruct<Vector2>(defaultValue, x =>
            {
                var result = new Vector2(ReadFloat(validUnits, x.Value.X), ReadFloat(validUnits, x.Value.Y));
                return result;
            });
        }

        /// <summary>Read a Vector3 object in the STF format '( {X} {Y} {Z} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector3</returns>
        public Vector3 ReadVector3Block(UNITS validUnits, Vector3 defaultValue)
        {
            return ReadBlockStruct<Vector3>(defaultValue, x =>
            {
                var result = new Vector3(
                    ReadFloat(validUnits, x.Value.X),
                    ReadFloat(validUnits, x.Value.Y),
                    ReadFloat(validUnits, x.Value.Z));
                return result;
            });
        }

        /// <summary>Read a Vector4 object in the STF format '( {X} {Y} {Z} {W} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the available suffixes to reasonable values.</param>
        /// <param name="defaultValue">The default vector if any of the values are not specified</param>
        /// <returns>The STF block as a Vector4</returns>
        public Vector4 ReadVector4Block(UNITS validUnits, Vector4 defaultValue)
        {
            return ReadBlockStruct<Vector4>(defaultValue, x =>
            {
                var result = new Vector4(
                    ReadFloat(validUnits, x.Value.X),
                    ReadFloat(validUnits, x.Value.Y),
                    ReadFloat(validUnits, x.Value.Z),
                    ReadFloat(validUnits, x.Value.W)
                    );
                return result;
            });
        }
        #endregion
        #endregion

        #region Tokenprocessor, Parseblock
        /// <summary>Parse an STF file until the EOF, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseFile(TokenProcessor[] processors)
        {
            ParseFileOrBlock(() => Eof, () => false, processors);
        }

        /// <summary>Parse an STF file until the EOF, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="breakout">A delegate that returns true, if the processing should be halted prematurely</param>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseFile(ParsingBreak breakout, TokenProcessor[] processors)
        {
            ParseFileOrBlock(() => Eof, breakout, processors);
        }

        /// <summary>Parse an STF file until the end of block ')' marker, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseBlock(TokenProcessor[] processors)
        {
            ParseFileOrBlock(() => EndOfBlock(), () => false, processors);
        }

        /// <summary>Parse an STF file until the end of block ')' marker, using the array of lower case tokens, with a processor delegate/lambda
        /// </summary>
        /// <param name="breakout">A delegate that returns true, if the processing should be halted prematurely</param>
        /// <param name="processors">Array of lower case token, and the delegate/lambda to call when matched.</param>
        public void ParseBlock(ParsingBreak breakout, TokenProcessor[] processors)
        {
            ParseFileOrBlock(() => EndOfBlock(), breakout, processors);
        }

        private void ParseFileOrBlock(ParsingBreak endCondition, ParsingBreak breakout, TokenProcessor[] processors)
        { // Press F10 'Step Over' to jump to the next token
#line hidden
            while (!endCondition())
            {
#line default
                if (breakout()) { SkipRestOfBlock(); break; } // Press F11 'Step Into' to debug the Breakout delegate
#if DEBUG
                else { } // Press F10 'Step Over' to jump to the next token
#endif
#line hidden
                string token = ReadItem().ToLower(System.Globalization.CultureInfo.InvariantCulture);
                if (token == "(") { SkipRestOfBlock(); continue; }
                foreach (TokenProcessor tp in processors)
                    if (tp.token == token)
#line default
                        tp.processor(); // Press F11 'Step Into' to debug the Processor delegate
            } // Press F10 'Step Over' to jump to the next token
        }

        /// <summary>This delegate definition is used by the ParseFile and ParseBlock methods, and is called when an associated matching token is found.</summary>
        public delegate void Processor();

        /// <summary>This delegate definition is used by the ParseFile and ParseBlock methods, and is used to break out of the processing loop prematurely.
        /// </summary>
        /// <returns>true - if the parsing should be aborted prematurely</returns>
        public delegate bool ParsingBreak();

        /// <summary>A structure used to index lambda functions by a lower cased token.</summary>
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

        #region Legacy (todo: refactor away)
        /// <summary>Property that returns true when the EOF has been reached</summary>
        public bool EOF() { return Eof; }
        public void VerifyStartOfBlock()
        {
            MustMatch("(");
        }

        public int PeekPastWhitespace()
        {   // very limited implementation!

            // this use should be refactored to .IsEOF (since whitespace is skipped anyway)
            if (UpcomingTokenFromSource.IsEOF) return -1;

            // this should use same solution as for refactoring of stepBackOneItem.
            if (UpcomingTokenFromSource.IsBlockEnd) return 41; // ')'

            return 0;
        }

        #endregion
    }

    #region StreamReaderFactory
    /// <summary>
    /// Factory class that creates a StreamReader from a file.
    /// Reading from an STF file also means that the first lines is interpreted as a SIMIS signature
    /// </summary>
    internal class StreamReaderFactoryFromFile : IStreamReaderFactory
    {
        /// <summary>
        /// Create a streamreader from a file. Read the first line as simis signature
        /// </summary>
        /// <param name="fileName">The (full) name of the file to be read</param>
        /// <param name="simisSignature">will contain the first line of the file</param>
        /// <returns>The created streamreader</returns>
        public StreamReader GetStreamReader(string fileName, out string simisSignature)
        {
            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            var stream = new StreamReader(fileName, true); // was System.Text.Encoding.Unicode ); but I found some ASCII files, ie GLOBAL\SHAPES\milemarker.s
            simisSignature = stream.ReadLine();
            return stream;
        }
    }
    #endregion

    #region StfExceptions
    /// <summary>
    /// Exceptions to be called during reading of STF files
    /// </summary>
    public class STFException : Exception
    {
        // Note: we do not like a dependency on STFReader itself: this gives circular coupling.
        // However, that is what we need to support.

        /// <summary> Create a trace warning</summary>
        internal static void TraceWarning(StfToken token, string message)
        {
            TraceWarning(token.SourceName, token.SourceLineNumber, message);
        }

        /// <summary> Create a trace warning</summary>
        public static void TraceWarning(string fileName, int lineNumber, string message)
        {
            System.Diagnostics.Trace.TraceWarning("{2} in {0}:line {1}", fileName, lineNumber, message);
        }

        /// <summary> Create a trace warning</summary>
        public static void TraceWarning(STFReader reader, string message) 
        {
            TraceWarning(reader.FileName, reader.LineNumber, message);
        }

        /// <summary> Create trace information</summary>
        public static void TraceInformation(string fileName, int lineNumber, string message)
        {
            System.Diagnostics.Trace.TraceInformation("{2} in {0}:line {1}", fileName, lineNumber, message);
        }

        /// <summary> Create trace information</summary>
        public static void TraceInformation(STFReader reader, string message)
        {
            TraceInformation(reader.FileName, reader.LineNumber, message);
        }

        /// <summary>Constructor</summary>
        internal STFException(StfToken token, string message)
            : this(token.SourceName, token.SourceLineNumber, message) { }

        /// <summary>Constructor</summary>
        public STFException(STFReader reader, string message)
            : this(reader.FileName, reader.LineNumber, message) { }

        /// <summary>Constructor</summary>
        public STFException(string fileName, int lineNumber, string message)
            : base(String.Format(System.Globalization.CultureInfo.CurrentCulture,
                                    "{2} in {0}:line {1}\n", fileName, lineNumber, message)) { }        
    }

    #endregion

}
#endif
#endregion
