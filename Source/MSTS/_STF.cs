/// This file contains code to read MSTS structured unicode text files
/// through the class  STFReader.   
/// 
/// Note:  the SBR classes are more general in that they are capable of reading
///        both unicode and binary compressed data files.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Additional Contributions
/// Copyright (c) Robert Hodgson 2010
/// Licensed to OpenRails under the Open Software License (OSL) v3.0

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
    /// <listItem><para>comment &amp; skip - must be followed by a block which will not be processed in OR</para></listItem>
    /// </list>&#160;<para>
    /// Finally any token which begins with a '#' character will be ignored, and then the next {data_item} (constant or block) will not be processed.</para><para>
    /// &#160;</para>
    /// <alert class="important"><para>NB!!! If a comment/skip/#*/_* is the last {item} in a block, rather than being totally consumed a dummy "#\u00b6" is returned, so if EndOFBlock() returns false, you always get an {item} (which can then just be ignored).</para></alert>
    /// </remarks>
    /// <example><code lang="C#" title="STF parsing using Parse...() and delegate/lambda functions in C#">
    ///     using (STFReader stf = new STFReader(filename, false))
    ///         stf.ParseFile(new STFReader.TokenProcessor[] {
    ///             new STFReader.TokenProcessor("item_single_constant", ()=>{ float isc = stf.ReadFloat(STFReader.UNITS.None, 0); }),
    ///             new STFReader.TokenProcessor("item_single_speed", ()=>{ float iss_mps = stf.ReadFloat(STFReader.UNITS.Speed, 0); }),
    ///             new STFReader.TokenProcessor("block_single_constant", ()=>{ float bsc = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
    ///             new STFReader.TokenProcessor("block_fixed_format", ()=>{
    ///                 stf.MustMatch("(");
    ///                 int bff1 = stf.ReadInt(STFReader.UNITS.None, 0);
    ///                 string bff2 = stf.ReadString();
    ///                 stf.SkipRestOfBlock();
    ///             }),
    ///             new STFReader.TokenProcessor("block_variable_contents", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
    ///                 new STFReader.TokenProcessor("subitem", ()=>{ string si = stf.ReadString(); }),
    ///                 new STFReader.TokenProcessor("subblock", ()=>{ string sb = stf.ReadStringBlock(""); }),
    ///             });}),
    ///         });
    /// </code></example>
    /// <example><code lang="C#" title="Alternate functional method to parse STF using C#">
    ///        using (STFReader stf = new STFReader(filename, false))
    ///            while (!stf.Eof)
    ///                switch (stf.ReadItem().ToLower())
    ///                {
    ///                    case "item_single_constant": float isc = stf.ReadFloat(STFReader.UNITS.None, 0); break;
    ///                    case "item_single_speed": float iss_mps = stf.ReadFloat(STFReader.UNITS.Speed, 0); break;
    ///                    case "block_single_constant": float bsc = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
    ///                    case "block_fixed_format":
    ///                        stf.MustMatch("(");
    ///                        int bff1 = stf.ReadInt(STFReader.UNITS.None, 0);
    ///                        string bff2 = stf.ReadString();
    ///                        stf.SkipRestOfBlock();
    ///                        break;
    ///                    case "block_variable_contents":
    ///                        stf.MustMatch("(");
    ///                        while (!stf.EndOfBlock())
    ///                            switch (stf.ReadItem().ToLower())
    ///                            {
    ///                                case "subitem": string si = stf.ReadString(); break;
    ///                                case "subblock": string sb = stf.ReadStringBlock(""); break;
    ///                                case "(": stf.SkipRestOfBlock();
    ///                            }
    ///                        break;
    ///                    case "(": stf.SkipRestOfBlock(); break;
    ///                }
    /// </code></example>
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
        /// <param name="useTree"><para>true - if the consumer is going to use the Tree Property as it's parsing method (MSTS wagons &amp; engines)</para>
        /// <para>false - if Tree is not used which signicantly reduces GC</para></param>
		public STFReader(string filename, bool useTree)
        {
            streamSTF = new StreamReader(filename, true); // was System.Text.Encoding.Unicode ); but I found some ASCII files, ie GLOBAL\SHAPES\milemarker.s
            FileName = filename;
            SimisSignature = streamSTF.ReadLine();
            LineNumber = 2;
            if (useTree) tree = new List<string>();
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
            Debug.Assert(inputStream.CanSeek);
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
#if DEBUG
                if (!IsEof(PeekPastWhitespace()))
                    STFException.TraceWarning(this, "Some of this STF file was not parsed.");
#endif
                streamSTF.Close(); streamSTF = null;
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
                string s = ReadItem();
                if (!s.Equals(target, StringComparison.OrdinalIgnoreCase))
                    throw new STFException(this, target + " Not Found - instead found " + s);
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
            if (includeReader != null)
            {
                bool eob = includeReader.EndOfBlock();
                if (includeReader.Eof)
                {
                    if (tree.Count != 0)
                        STFException.TraceWarning(includeReader, "Included file did not have a properly matched number of blocks.  It is unlikely the parent STF file will work properly.");
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
                UpdateTreeAndStepBack(")");
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
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file, with the suffix normalized to OR units.</returns>
        public int ReadInt(UNITS validUnits, int? defaultValue)
		{
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            int val;
            double scale = ParseUnitSuffix(ref item, validUnits);
            if (item.Length == 0) return 0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (int.TryParse(item, parseNum, parseNFI, out val)) return (scale == 1) ? val : (int)(scale * val);

            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an unsigned integer {constant_item}
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file, with the suffix normalized to OR units.</returns>
        public uint ReadUInt(UNITS validUnits, uint? defaultValue)
		{
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            uint val;
            double scale = ParseUnitSuffix(ref item, validUnits);
            if (item.Length == 0) return 0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (uint.TryParse(item, parseNum, parseNFI, out val)) return (scale == 1) ? val : (uint)(scale * val);

            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an single precision floating point number {constant_item}
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
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
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if an unexpected ')' token is found</param>
        /// <returns>The next {constant_item} from the STF file, with the suffix normalized to OR units.</returns>
        public double ReadDouble(UNITS validUnits, double? defaultValue)
		{
            string item = ReadItem();

            if ((defaultValue.HasValue) && (item == ")"))
            {
                STFException.TraceWarning(this, "When expecting a number, we found a ) marker. Using the default " + defaultValue.ToString());
                StepBackOneItem();
                return defaultValue.Value;
            }

            double val;
            double scale = ParseUnitSuffix(ref item, validUnits);
            if (item.Length == 0) return 0.0;
            if (item[item.Length - 1] == ',') item = item.TrimEnd(',');
            if (double.TryParse(item, parseNum, parseNFI, out val)) return scale * val;

            STFException.TraceWarning(this, "Cannot parse the constant number " + item);
            if (item == ")") StepBackOneItem();
            return defaultValue.GetValueOrDefault(0);
		}
        /// <summary>Enumeration limiting which units are valid when parsing a numeric constant.
        /// </summary>
        [Flags]
        public enum UNITS
        {
            /// <summary>No unit parsing is done on the {constant_item} - which is obviously fastest
            /// </summary>
            None = 0,
            /// <summary>Combined using an | with other UNITS if the unit is compulsary (compulsary units will slow parsing)
            /// </summary>
            Compulsary = 1 << 0,
            /// <summary>Valid Units: m, cm, mm, km, ft, ', in, "
            /// <para>Scaled to meters.</para>
            /// </summary>
            Distance = 1 << 1,
            /// <summary>Valid Units: m/s, mph, kph, kmh, km/h
            /// <para>Scaled to meters/second.</para>
            /// </summary>
            Speed = 1 << 2,
            /// <summary>Valid Units: kg, t, lb
            /// <para>Scaled to kilograms.</para>
            /// </summary>
            Mass = 1 << 3,
            /// <summary>Valid Units: n, kn, lbf
            /// <para>Scaled to newtons.</para>
            /// </summary>
            Force = 1 << 4,
            /// <summary>Valid Units: w, kw, hp
            /// <para>Scaled to watts.</para>
            /// </summary>
            Power = 1 << 5,
            /// <summary>Valid Units: n/m
            /// <para>Scaled to newtons/metre.</para>
            /// </summary>
            Stiffness = 1 << 6,
            /// <summary>Valid Units: n/m/s (+ '/m/s' in case the newtons is missed) 
            /// <para>Scaled to newtons/speed(m/s)</para>
            /// </summary>
            Resistance = 1 << 7,
            /// <summary>Valid Units: lb/h
            /// <para>Scaled to pounds per hour.</para>
            /// </summary>
            MassRate = 1 << 8,
            /// <summary>Valid Units: *(ft^3)
            /// <para>Scaled to cubic feet.</para>
            /// </summary>
            Volume = 1 << 9,
            /// <summary>Valid Units: psi
            /// <para>Scaled to pounds per square inch.</para>
            /// </summary>
            Pressure = 1 << 10,
            /// <summary>Valid Units: *(ft^2)
            /// <para>Scaled to square meters.</para>
            /// </summary>
            Area = 1 << 11,
            /// <summary>Valid Units: kj/kg, j/g, btu/lb
            /// <para>Scaled to kj/kg.</para>
            /// </summary>
            EnergyDensity = 1 << 12,
            /// <summary>
            /// Valid Units: gal, l
            /// <para>Scaled to litres.</para>
            /// </summary>
            Diesel = 1 << 13,
            /// <summary>This is only provided for backwards compatibility - all new users should limit the units to appropriate types
            /// </summary>
            Any = -2
        }

        /// <summary>This function removes known unit suffixes, and returns a scaler to bring the constant into the standard OR units.
        /// </summary>
        /// <remarks>This function is marked internal so it can be used to support arithmetic processing once the elements are seperated (eg. 5*2m)
        /// </remarks>
        /// <param name="constant">string with suffix (ie "23 mph"), after the function call the suffix is removed.</param>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <returns>The scaler that should be used to modify the constant to standard OR units.</returns>
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
            if (i == constant.Length)
            {
                if ((validUnits & UNITS.Compulsary) > 0)
                    STFException.TraceWarning(this, "Missing a suffix for data expecting " + validUnits.ToString() + " units");
                else
                    return 1; // There is no suffix, it's all numeric
            }
            while ((i < constant.Length) && (constant[i] == ' ')) ++i; // skip the spaces

            // Enclose the unit suffix
            int suffixStart = i;
            int suffixLength = constant.Length - suffixStart;  

            // Check for an embedded comment in the unit suffix string, ( ie "220kN#est" used in acela.eng ) 
            int commentStart = constant.IndexOf('#', suffixStart);
            if( commentStart != -1 ) 
                suffixLength = commentStart - suffixStart;

            // Extract the unit suffix
            string suffix = constant.Substring(suffixStart, suffixLength).ToLowerInvariant();
            suffix = suffix.Trim();

            // Extract the prefixed numeric string
            constant = constant.Substring(beg, end - beg);

            // Select and return the scalar value
            if ((validUnits & UNITS.Distance) > 0)
                switch (suffix)
                {
                    case "m": return 1;
                    case "cm": return 0.01;
                    case "mm": return 0.001;
                    case "km": return 1e3;
                    case "ft": return 0.3048;
                    case "'": return 0.3048;
                    case "in": return 0.0254;
                    case "\"": return 0.0254;
                    case "in/2": return 0.0127; // This is a strange unit used to measure radius
                }
            if ((validUnits & UNITS.Speed) > 0)
                switch (suffix)
                {
                    case "m/s": return 1;
                    case "mph": return 0.44704;
                    case "kph": return 0.27778;
                    case "kmh": return 0.27778;
                    case "km/h": return 0.27778;
                    default: return 0.44704;
                }
            if ((validUnits & UNITS.Mass) > 0)
                switch (suffix)
                {
                    case "kg": return 1;
                    case "t": return 1e3;
                    case "lb": return 0.45359237;
                }
            if ((validUnits & UNITS.MassRate) > 0)
                switch (suffix)
                {
                    case "lb/h": return 1;
                }
            if ((validUnits & UNITS.Force) > 0)
                switch (suffix)
                {
                    case "n": return 1;
                    case "kn": return 1e3;
                    case "lbf": return 4.44822162;
                }
            if ((validUnits & UNITS.Power) > 0)
                switch (suffix)
                {
                    case "w": return 1;
                    case "kw": return 1e3;
                    case "hp": return 745.7;
                }
            if ((validUnits & UNITS.Stiffness) > 0)
                switch (suffix)
                {
                    case "n/m": return 1;
                }
            if ((validUnits & UNITS.Resistance) > 0)
                switch (suffix)
                {
                    case "n/m/s": return 1;
                    case "/m/s": return 1;
                }
            if ((validUnits & UNITS.Pressure) > 0)
                switch (suffix)
                {
                    case "psi": return 1;
                }
            if ((validUnits & UNITS.Volume) > 0)
                switch (suffix)
                {
                    case "*(ft^3)": return 1;
                }
            if ((validUnits & UNITS.Diesel) > 0)
                switch (suffix)
                {
                    case "gal": return 3.785f;
                    case "l": return 1;
                }
            if ((validUnits & UNITS.Area) > 0)
                switch (suffix)
                {
                    case "*(ft^2)": return .09290304f;
                }
            if ((validUnits & UNITS.EnergyDensity) > 0)
                switch (suffix)
                {
                    case "kj/kg": return 1;
                    case "j/g": return 1;
                    case "btu/lb": return 1 / 2.326f;
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
                SkipRestOfBlock();
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
                SkipRestOfBlock();
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }
		/// <summary>Read an integer constant from the STF format '( {int_constant} ... )'
		/// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
		/// <returns>The STF block with the first {item} converted to a integer constant.</returns>
		public int ReadIntBlock(UNITS validUnits, int? defaultValue)
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
				int result = ReadInt(validUnits, defaultValue);
				SkipRestOfBlock();
				return result;
			}
			STFException.TraceWarning(this, "Block Not Found - instead found " + s);
			return defaultValue.GetValueOrDefault(0);
		}
		/// <summary>Read an unsigned integer constant from the STF format '( {uint_constant} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a unsigned integer constant.</returns>
        public uint ReadUIntBlock(UNITS validUnits, uint? defaultValue)
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
                uint result = ReadUInt(validUnits, defaultValue);
                SkipRestOfBlock();
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an single precision constant from the STF format '( {float_constant} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
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
                SkipRestOfBlock();
                return result;
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue.GetValueOrDefault(0);
        }
        /// <summary>Read an double precision constant from the STF format '( {double_constant} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
        /// <param name="defaultValue">the default value if the constant is not found in the block.</param>
        /// <returns>The STF block with the first {item} converted to a double precision constant.</returns>
        public double ReadDoubleBlock(UNITS validUnits, double? defaultValue)
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
                double result = ReadDouble(validUnits, defaultValue);
                SkipRestOfBlock();
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
                        SkipRestOfBlock();
                        return defaultValue;
                }
            }
            STFException.TraceWarning(this, "Block Not Found - instead found " + s);
            return defaultValue;
        }
        /// <summary>Read a Vector3 object in the STF format '( {X} {Y} {Z} ... )'
        /// </summary>
        /// <param name="validUnits">Any combination of the UNITS enumeration, to limit the availale suffixes to reasonable values.</param>
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
                SkipRestOfBlock();
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
                if (tree.Count != 0)
                    STFException.TraceWarning(includeReader, "Included file did not have a properly matched number of blocks.  It is unlikely the parent STF file will work properly.");
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
            #region Handle Open and Close Block markers - parenthisis
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
                if (comment == ")") { StepBackOneItem(); return "#\u00b6"; }
                if (comment == "(") SkipRestOfBlock();
                #endregion
                string item = ReadItem( skip_mode, string_mode);
                if (item == ")") { StepBackOneItem(); return "#\u00b6"; }
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
            #region Build Normal Items - whitespace delimitered
            else if (c != -1)
            {
                for (; ; )
                {
                    itemBuilder.Append((char)c);
                    c = PeekChar();
                    if ((c == '(') || (c == ')')) break;
                    c = ReadChar();
                    if (IsEof(c)) break;
                    if (IsWhiteSpace(c)) break;
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
                        string filename = ReadItem(skip_mode, string_mode);
                        if (filename == "(")
                        {
                            filename = ReadItem(skip_mode, string_mode);
                            SkipRestOfBlock();
                        }
                        if (tree.Count == 0)
                        {
                            includeReader = new STFReader(Path.GetDirectoryName(FileName) + @"\" + filename, false);
                            return ReadItem(skip_mode, string_mode); // Which will recurse down when includeReader is tested
                        }
                        else
                            STFException.TraceWarning(this, "Found an include directive, but it was enclosed inside block parenthesis which is illegal.");
                        break;
                    #endregion
                    #region Process special token - skip and comment
                    case "skip":
                    case "comment":
                        {
                            #region Skip the comment item or block
                            string comment = ReadItem(skip_mode, string_mode);
                            if (comment == "(") SkipRestOfBlock();
                            #endregion
                            string item = ReadItem(skip_mode, string_mode);
                            if (item == ")") { StepBackOneItem(); return "#\u00b6"; }
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
        public static void TraceInformation(STFReader stf, string message)
        {
            Trace.TraceInformation("{2} in {0}:line {1}", stf.FileName, stf.LineNumber, message);
        }

        public static void TraceWarning(STFReader stf, string message)
        {
            Trace.TraceWarning("{2} in {0}:line {1}", stf.FileName, stf.LineNumber, message);
        }

        public STFException(STFReader stf, string message)
            : base(String.Format("{2} in {0}:line {1}", stf.FileName, stf.LineNumber, message))
        {
        }
    }
}
