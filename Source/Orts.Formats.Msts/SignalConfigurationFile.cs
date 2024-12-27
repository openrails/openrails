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

// This module parses the sigcfg file and builds an object model based on signal details
// 
// Author: Laurie Heath
// Updates : Rob Roeterdink
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Orts.Parsers.Msts;
using ORTS.Common;
using static System.String;

namespace Orts.Formats.Msts
{
    #region SIGCFGFile
    /// <summary>
    /// Object containing a representation of everything in the MSTS sigcfg.dat file
    /// Not everythin of the representation will be used by OpenRails
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable only used in using statement, known FcCop bug")]
    public class SignalConfigurationFile
    {
        /// <summary>Name-indexed list of available signal functions</summary>
        public IDictionary<string, SignalFunction> SignalFunctions;
        /// <summary>Allocation-free MstsName-indexed list of available signal functions</summary>
        public static Dictionary<MstsSignalFunction, SignalFunction> MstsSignalFunctions;
        /// <summary>List of OR defined subtypes for Norman signals</summary>
        public IList<string> ORTSNormalSubtypes;
        /// <summary>Name-indexed list of available light textures</summary>
        public IDictionary<string, LightTexture> LightTextures;
        /// <summary>Name-indexed list of available colours for lights</summary>
        public IDictionary<string, LightTableEntry> LightsTable;
        /// <summary>Name-indexed list of available signal types</summary>
        public IDictionary<string, SignalType> SignalTypes;
        /// <summary>Name-indexed list of available signal shapes (including heads and other sub-objects)</summary>
        public IDictionary<string, SignalShape> SignalShapes;
        /// <summary>list of names of script files</summary>
        public IList<string> ScriptFiles;
        /// <summary>Full file name and path of the signal config file</summary>
        public string ScriptPath;

        /// <summary>
        /// Constructor from file
        /// </summary>
        /// <param name="filenamewithpath">Full file name of the sigcfg.dat file</param>
        /// <param name="ORTSMode">Read file in ORTSMode (set NumClearAhead_ORTS only)</param>
        public SignalConfigurationFile(string filenamewithpath, bool ORTSMode)
        {
            ScriptPath = Path.GetDirectoryName(filenamewithpath);

            // preset OR function types and related MSTS function types for default types
            SignalFunctions = new SortedDictionary<string, SignalFunction>
            {
                { SignalFunction.NORMAL.Name, SignalFunction.NORMAL },
                { SignalFunction.DISTANCE.Name, SignalFunction.DISTANCE },
                { SignalFunction.REPEATER.Name, SignalFunction.REPEATER },
                { SignalFunction.SHUNTING.Name, SignalFunction.SHUNTING },
                { SignalFunction.INFO.Name, SignalFunction.INFO },
                { SignalFunction.SPEED.Name, SignalFunction.SPEED },
                { SignalFunction.ALERT.Name, SignalFunction.ALERT },
                { SignalFunction.UNKNOWN.Name, SignalFunction.UNKNOWN }
            };

            // and the allocation-free version of the above 
            MstsSignalFunctions = new Dictionary<MstsSignalFunction, SignalFunction>
            {
                { MstsSignalFunction.NORMAL, SignalFunction.NORMAL },
                { MstsSignalFunction.DISTANCE, SignalFunction.DISTANCE },
                { MstsSignalFunction.REPEATER, SignalFunction.REPEATER },
                { MstsSignalFunction.SHUNTING, SignalFunction.SHUNTING },
                { MstsSignalFunction.INFO, SignalFunction.INFO },
                { MstsSignalFunction.SPEED, SignalFunction.SPEED },
                { MstsSignalFunction.ALERT, SignalFunction.ALERT },
                { MstsSignalFunction.UNKNOWN, SignalFunction.UNKNOWN }
            };

            // preset empty OR normal subtypes
            ORTSNormalSubtypes = new List<String>();

            if (ORTSMode)
            {
                using (STFReader stf = new STFReader(filenamewithpath, false))
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("lighttextures", ()=>{ LightTextures = ReadLightTextures(stf); }),
                    new STFReader.TokenProcessor("lightstab", ()=>{ LightsTable = ReadLightsTable(stf); }),
                    new STFReader.TokenProcessor("ortssignalfunctions", ()=>{ ReadORTSSignalFunctions(stf, ref SignalFunctions); }),
                    new STFReader.TokenProcessor("ortsnormalsubtypes", ()=>{ ReadORTSNormalSubtypes(stf, ref ORTSNormalSubtypes); }),
                    new STFReader.TokenProcessor("signaltypes", ()=>{ SignalTypes = ReadSignalTypes(stf, ORTSMode, SignalFunctions, ORTSNormalSubtypes); }),
                    new STFReader.TokenProcessor("signalshapes", ()=>{ SignalShapes = ReadSignalShapes(stf); }),
                    new STFReader.TokenProcessor("scriptfiles", ()=>{ ScriptFiles = ReadScriptFiles(stf); }),
                });
            }
            else
            {
                using (STFReader stf = new STFReader(filenamewithpath, false))
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("lighttextures", ()=>{ LightTextures = ReadLightTextures(stf); }),
                    new STFReader.TokenProcessor("lightstab", ()=>{ LightsTable = ReadLightsTable(stf); }),
                    new STFReader.TokenProcessor("signaltypes", ()=>{ SignalTypes = ReadSignalTypes(stf, ORTSMode, SignalFunctions, ORTSNormalSubtypes); }),
                    new STFReader.TokenProcessor("signalshapes", ()=>{ SignalShapes = ReadSignalShapes(stf); }),
                    new STFReader.TokenProcessor("scriptfiles", ()=>{ ScriptFiles = ReadScriptFiles(stf); }),
                });
            }

            Initialize<Dictionary<string, LightTexture>, IDictionary<string, LightTexture>>(ref LightTextures, "LightTextures", filenamewithpath);
            Initialize<Dictionary<string, LightTableEntry>, IDictionary<string, LightTableEntry>>(ref LightsTable, "LightsTab", filenamewithpath);
            Initialize<Dictionary<string, SignalType>, IDictionary<string, SignalType>>(ref SignalTypes, "SignalTypes", filenamewithpath);
            Initialize<Dictionary<string, SignalShape>, IDictionary<string, SignalShape>>(ref SignalShapes, "SignalShapes", filenamewithpath);
            Initialize<List<string>, IList<string>>(ref ScriptFiles, "ScriptFiles", filenamewithpath);
        }

        private static void Initialize<T, U>(ref U field, string name, string file) where T : U, new()
        {
            if (field == null)
            {
                field = new T();
                Trace.TraceWarning("Ignored missing {1} in {0}", file, name);
            }
        }

        private static void ReadORTSSignalFunctions(STFReader stf, ref IDictionary<string, SignalFunction> SignalFunctions)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, MstsSignalFunction> TokensRead = new Dictionary<string, MstsSignalFunction>();

            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("ortssignalfunctiontype", ()=> {
                    stf.MustMatch("(");
                    if (TokensRead.Count >= count)
                    {
                        STFException.TraceWarning(stf, "Skipped extra ORTSFunctionType");
                    }
                    else
                    {
                        string orType = stf.ReadString().ToUpper();
                        bool valid = true;

                        // check against default types
                        foreach (MstsSignalFunction function in Enum.GetValues(typeof(MstsSignalFunction)))
                        {
                            if (orType == function.ToString().ToUpper())
                            {
                                STFException.TraceWarning(stf, "Invalid definition of ORTSFunctionType, type is equal to MSTS defined type : " + orType);
                                valid = false;
                                break;
                            }
                        }

                        if (valid)
                        {
                            if (orType.Length > 3 && orType.Substring(0, 3) == "OR_"
                                || orType.Length > 4 && orType.Substring(0, 4) == "ORTS")
                            {
                                STFException.TraceWarning(stf, "Invalid definition of ORTSFunctionType, using reserved type name : " + orType);
                                valid = false;
                            }
                        }

                        string mstsTypeUnparsed = stf.ReadString().ToUpper();
                        MstsSignalFunction mstsTypeParsed;

                        if (mstsTypeUnparsed != ")")
                        {
                            // If optional MSTS type is defined and incorrect, invalidate the function.
                            if (!Enum.TryParse(mstsTypeUnparsed, out mstsTypeParsed))
                            {
                                STFException.TraceWarning(stf, "Invalid definition of ORTSFunctionType, underlying type is not equal to a MSTS function type : " + orType);
                                valid = false;
                            }
                            else if (mstsTypeParsed == MstsSignalFunction.NORMAL)
                            {
                                STFException.TraceWarning(stf, "Invalid definition of ORTSFunctionType, underlying type value NORMAL is forbidden for custom function types : " + orType);
                                valid = false;
                            }
                        }
                        else
                        {
                            // Default to INFO if no MSTS type defined
                            stf.StepBackOneItem();
                            mstsTypeParsed = MstsSignalFunction.INFO;
                        }

                        if (valid)
                        {
                            TokensRead.Add(orType, mstsTypeParsed);
                        }
                    }
                    stf.SkipRestOfBlock();
                }),
            });

            // sort defined OR functions
            foreach (KeyValuePair<string, MstsSignalFunction> element in TokensRead)
            {
                if (SignalFunctions.ContainsKey(element.Key))
                {
                    STFException.TraceWarning(stf, "Skipped duplicate ORTSSignalFunction definition : " + element.Key);
                }
                else
                {
                    SignalFunctions.Add(element.Key, new SignalFunction(element.Key, element.Value));
                }
            }
        }

        private static void ReadORTSNormalSubtypes(STFReader stf, ref IList<string> ORTSNormalSubtypes)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<string> TokensRead = new List<string>();

            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsnormalsubtype", ()=> {
                stf.MustMatch("(");
                if (TokensRead.Count >= count)
                {
                        STFException.TraceWarning(stf, "Skipped extra ORTSNormalSubtype");
                }
                else
                {
                            TokensRead.Add(stf.ReadString().ToUpper());
                }
                    stf.SkipRestOfBlock();
                }),
            });

            // sort defined OR subtypes
            for (int itoken = 0; itoken < TokensRead.Count; itoken++)
            {
                string ORTSSubtype = TokensRead[itoken];
                if (ORTSNormalSubtypes.Contains(ORTSSubtype))
                {
                    STFException.TraceWarning(stf, "Skipped duplicate ORTSNormalSubtype definition : " + ORTSSubtype);
                }
                else
                {
                    ORTSNormalSubtypes.Add(ORTSSubtype);
                }
            }
        }

        private static IDictionary<string, LightTexture> ReadLightTextures(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, LightTexture> lightTextures = new Dictionary<string, LightTexture>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("lighttex", ()=>{
                    if (lightTextures.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra LightTex");
                    else
                    {
                        LightTexture lightTexture = new LightTexture(stf);
                        if (lightTextures.ContainsKey(lightTexture.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate LightTex " + lightTexture.Name);
                        else
                            lightTextures.Add(lightTexture.Name, lightTexture);
                    }
                }),
            });
            if (lightTextures.Count < count)
                STFException.TraceWarning(stf, (count - lightTextures.Count).ToString() + " missing LightTex(s)");
            return lightTextures;
        }

        private static IDictionary<string, LightTableEntry> ReadLightsTable(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, LightTableEntry> lightsTable = new Dictionary<string, LightTableEntry>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("lightstabentry", ()=>{
                    if (lightsTable.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra LightsTabEntry");
                    else
                    {
                        LightTableEntry lightsTableEntry = new LightTableEntry(stf);
                        if (lightsTable.ContainsKey(lightsTableEntry.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate LightsTabEntry " + lightsTableEntry.Name);
                        else
                            lightsTable.Add(lightsTableEntry.Name, lightsTableEntry);
                    }
                }),
            });
            if (lightsTable.Count < count)
                STFException.TraceWarning(stf, (count - lightsTable.Count).ToString() + " missing LightsTabEntry(s)");
            return lightsTable;
        }

        private static IDictionary<string, SignalType> ReadSignalTypes(STFReader stf, bool ORTSMode, IDictionary<string, SignalFunction> ORTSFunctionTypes, IList<string> ORTSNormalSubtypes)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, SignalType> signalTypes = new Dictionary<string, SignalType>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signaltype", ()=>{
                    if (signalTypes.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalType");
                    else
                    {
                        SignalType signalType = new SignalType(stf, ORTSMode, ORTSFunctionTypes, ORTSNormalSubtypes);
                        if (signalTypes.ContainsKey(signalType.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate SignalType " + signalType.Name);
                        else
                            signalTypes.Add(signalType.Name, signalType);
                    }
                }),
            });
            if (signalTypes.Count < count)
                STFException.TraceWarning(stf, (count - signalTypes.Count).ToString() + " missing SignalType(s)");
            return signalTypes;
        }

        private static IDictionary<string, SignalShape> ReadSignalShapes(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, SignalShape> signalShapes = new Dictionary<string, SignalShape>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalshape", ()=>{
                        if (signalShapes.Count >= count)
                            STFException.TraceWarning(stf, "Skipped extra SignalShape");
                        else
                        {
                            SignalShape signalShape = new SignalShape(stf);
                            if (signalShapes.ContainsKey(signalShape.ShapeFileName))
                                STFException.TraceWarning(stf, "Skipped duplicate SignalShape " + signalShape.ShapeFileName);
                            else
                                signalShapes.Add(signalShape.ShapeFileName, signalShape);
                        }
                }),
            });
            if (signalShapes.Count < count)
                STFException.TraceWarning(stf, (count - signalShapes.Count).ToString() + " missing SignalShape(s)");
            return signalShapes;
        }

        private static IList<string> ReadScriptFiles(STFReader stf)
        {
            stf.MustMatch("(");
            List<string> scriptFiles = new List<string>();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("scriptfile", ()=>{ scriptFiles.Add(stf.ReadStringBlock(null)); }),
            });
            return scriptFiles;
        }
    }
    #endregion

    #region LightTexture
    /// <summary>
    /// Defines a single light texture, used as background to draw lit lights onto signals
    /// </summary>
    public class LightTexture
    {
        /// <summary>Name of the light texture</summary>
        public readonly string Name;
        /// <summary>Filename of the texture</summary>
        public readonly string TextureFile;
        /// <summary>Left coordinate within texture (0.0 to 1.0)</summary>
        public readonly float u0;
        /// <summary>Top coordinate within texture (0.0 to 1.0)</summary>
        public readonly float v0;
        /// <summary>Right coordinate within texture (0.0 to 1.0)</summary>
        public readonly float u1;
        /// <summary>Bottom coordinate within texture (0.0 to 1.0)</summary>
        public readonly float v1;

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public LightTexture(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            TextureFile = stf.ReadString();
            u0 = stf.ReadFloat(STFReader.UNITS.None, null);
            v0 = stf.ReadFloat(STFReader.UNITS.None, null);
            u1 = stf.ReadFloat(STFReader.UNITS.None, null);
            v1 = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
    }
    #endregion

    #region LightTableEntry
    /// <summary>
    /// Describes how to draw a light in its illuminated state
    /// </summary>
    public class LightTableEntry
    {
        /// <summary>Name of the light</summary>
        public readonly string Name;
        /// <summary>Alpha channel of the colour (255 is opaque)</summary>
        public byte a { get; private set; }
        /// <summary>Amount of red in the colour</summary>
        public byte r { get; private set; }
        /// <summary>Amount of green in the colour</summary>
        public byte g { get; private set; }
        /// <summary>Amount of blue in the colour</summary>
        public byte b { get; private set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public LightTableEntry(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("colour", ()=>{
                    stf.MustMatch("(");
                    a = (byte)stf.ReadUInt(null);
                    r = (byte)stf.ReadUInt(null);
                    g = (byte)stf.ReadUInt(null);
                    b = (byte)stf.ReadUInt(null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }
    #endregion

    #region SignalFunction
    public class SignalFunction : IEquatable<SignalFunction>
    {
        public static SignalFunction NORMAL = new SignalFunction(MstsSignalFunction.NORMAL);
        public static SignalFunction DISTANCE = new SignalFunction(MstsSignalFunction.DISTANCE);
        public static SignalFunction REPEATER = new SignalFunction(MstsSignalFunction.REPEATER);
        public static SignalFunction SHUNTING = new SignalFunction(MstsSignalFunction.SHUNTING);
        public static SignalFunction INFO = new SignalFunction(MstsSignalFunction.INFO);
        public static SignalFunction SPEED = new SignalFunction(MstsSignalFunction.SPEED);
        public static SignalFunction ALERT = new SignalFunction(MstsSignalFunction.ALERT);
        public static SignalFunction UNKNOWN = new SignalFunction(MstsSignalFunction.UNKNOWN);

        public readonly string Name;
        public readonly MstsSignalFunction MstsFunction;
        readonly int HashCode;

        public SignalFunction(MstsSignalFunction mstsFunction)
        {
            Name = mstsFunction.ToString();
            MstsFunction = mstsFunction;

            HashCode = Name.GetHashCode() ^ MstsFunction.GetHashCode();
        }

        public SignalFunction(string name, MstsSignalFunction mstsFunction)
        {
            Name = name;
            MstsFunction = mstsFunction;

            HashCode = Name.GetHashCode() ^ MstsFunction.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SignalFunction);
        }

        public bool Equals(SignalFunction other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name &&
                   MstsFunction == other.MstsFunction;
        }

        public static bool operator ==(SignalFunction a, SignalFunction b)
        {
            if (a is null)
            {
                if (b is null)
                {
                    return true;
                }

                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(SignalFunction a, SignalFunction b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
    #endregion

    #region SignalType
    /// <summary>
    /// Signal Type which defines the attributes of a type or category of signal-heads
    /// </summary>
    public class SignalType
    {
        /// <summary>
        /// Describe the function of a particular signal head.
        /// Only SIGFN_NORMAL signal heads will require a train to take action (e.g. to stop).  
        /// The other values act only as categories for signal types to belong to.
        /// Within MSTS and scripts known as SIGFN_ values.  
        /// </summary>

        /// <summary></summary>
        public readonly string Name;
        /// allocated script
        public string Script = Empty;
        /// <summary>
        /// Signal function associated to this signal type 
        /// </summary>
        public SignalFunction Function { get; protected set; }
        /// <summary>OR Additional subtype for Normal signals</summary>
        public String ORTSNormalSubtype { get; private set; }
        public int ORTSSubtypeIndex { get; private set; }
        /// <summary>Unknown, used at least in Marias Pass route</summary>
        public bool Abs { get; private set; }
        /// <summary>This signal type is not suitable for placement on a gantry</summary>
        public bool NoGantry { get; private set; }
        /// <summary>This is a semaphore signal</summary>
        public bool Semaphore { get; private set; }
        /// <summary>On duration for flashing light. (In seconds.)</summary>
        public float FlashTimeOn { get; private set; }
        /// <summary>Off duration for flashing light. (In seconds.)</summary>
        public float FlashTimeOff { get; private set; }
        /// <summary>Transition time between lit and unlit states. (In seconds.)</summary>
        public float OnOffTimeS { get; private set; }
        /// <summary>The name of the texture to use for the lights</summary>
        public string LightTextureName { get; private set; }
        /// <summary></summary>
        public IList<SignalLight> Lights { get; private set; }
        /// <summary>Name-indexed draw states</summary>
        public IDictionary<string, SignalDrawState> DrawStates { get; private set; }
        /// <summary>List of aspects this signal type can have</summary>
        public IList<SignalAspect> Aspects { get; private set; }
        /// <summary>Number of blocks ahead which need to be cleared in order to maintain a 'clear' indication
        /// in front of a train. MSTS calculation</summary>
        public int NumClearAhead_MSTS { get; private set; }
        /// <summary>Number of blocks ahead which need to be cleared in order to maintain a 'clear' indication
        /// in front of a train. ORTS calculation</summary>
        public int NumClearAhead_ORTS { get; private set; }
        /// <summary>Number of seconds to spend animating a semaphore signal.</summary>
        public float SemaphoreInfo { get; private set; }

        /// <summary> approach control details
        public ApproachControlLimits ApproachControlDetails;

        /// <summary> visibility distance for request stop pick up
        public float? ReqStopVisDistance;
        /// <summary> announce distance for request stop set down
        public float? ReqStopAnnDistance;

        /// <summary> Glow value for daytime (optional).</summary>
        public float? DayGlow = null;
        /// <summary> Glow value for nighttime (optional).</summary>
        public float? NightGlow = null;
        /// <summary> Lights switched off or on during daytime (default : on) (optional).</summary>
        public bool DayLight = true;

        /// <summary>
        /// Common initialization part for constructors
        /// </summary>
        private SignalType()
        {
            SemaphoreInfo = 1; // Default animation time for semaphore signals (1 second).
            LightTextureName = String.Empty;
            FlashTimeOn = 1.0f;
            FlashTimeOff = 1.0f;
            OnOffTimeS = 0.2f;
        }

        /// <summary>
        /// Constructor for dummy entries
        /// </summary>
        public SignalType(SignalFunction function, MstsSignalAspect reqAspect)
            : this()
        {
            Function = function;
            Name = "UNDEFINED";
            Semaphore = false;
            DrawStates = new Dictionary<string, SignalDrawState>();
            DrawStates.Add("CLEAR", new SignalDrawState("CLEAR", 1));
            Aspects = new List<SignalAspect>();
            Aspects.Add(new SignalAspect(reqAspect, "CLEAR"));
        }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        /// <param name="ortsMode">Process SignalType for ORTS mode (always set NumClearAhead_ORTS only)</param>
        public SignalType(STFReader stf, bool ortsMode, IDictionary<string, SignalFunction> signalFunctions, IList<string> ORTSNormalSubtypes)
            : this()
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            int numClearAhead = -2;
            int numdefs = 0;
            ORTSNormalSubtype = String.Empty;

            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsscript", ()=>{ Script = stf.ReadStringBlock("").ToLowerInvariant(); }),
                new STFReader.TokenProcessor("signalfntype", ()=>{ Function = ReadSignalFunction(stf, ortsMode, signalFunctions); }),
                new STFReader.TokenProcessor("signallighttex", ()=>{ LightTextureName = stf.ReadStringBlock("").ToLowerInvariant(); }),
                new STFReader.TokenProcessor("signallights", ()=>{ Lights = ReadLights(stf); }),
                new STFReader.TokenProcessor("signaldrawstates", ()=>{ DrawStates = ReadDrawStates(stf); }),
                new STFReader.TokenProcessor("signalaspects", ()=>{ Aspects = ReadAspects(stf); }),
                new STFReader.TokenProcessor("approachcontrolsettings", ()=>{ ApproachControlDetails = ReadApproachControlDetails(stf); }),
                new STFReader.TokenProcessor("ortsreqstopvisdistance", ()=>{ReqStopVisDistance = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("ortsreqstopanndistance", ()=>{ReqStopAnnDistance = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("signalnumclearahead", ()=>{ numClearAhead = numClearAhead >= -1 ? numClearAhead : stf.ReadIntBlock(null); numdefs++;}),
                new STFReader.TokenProcessor("semaphoreinfo", ()=>{ SemaphoreInfo = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("ortsdayglow", ()=>{ DayGlow = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("ortsnightglow", ()=>{ NightGlow = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),                
                new STFReader.TokenProcessor("ortsdaylight", ()=>{ DayLight = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("ortsnormalsubtype", ()=>{ ORTSNormalSubtype = ReadORTSNormalSubtype(stf, ORTSNormalSubtypes); }),
                new STFReader.TokenProcessor("ortsonofftimes", ()=>{ OnOffTimeS = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("sigflashduration", ()=>{
                    stf.MustMatch("(");
                    FlashTimeOn = stf.ReadFloat(STFReader.UNITS.None, null);
                    FlashTimeOff = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "abs": Abs = true; break;
                            case "no_gantry": NoGantry = true; break;
                            case "semaphore": Semaphore = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalType flag " + stf.ReadString()); break;
                        }
                }),
            });

            if (ortsMode)
            {
                // set index for Normal Subtype
                ORTSSubtypeIndex = String.IsNullOrEmpty(ORTSNormalSubtype) ? -1 : ORTSNormalSubtypes.IndexOf(ORTSNormalSubtype);

                // set SNCA
                NumClearAhead_MSTS = -2;
                NumClearAhead_ORTS = numClearAhead;
            }
            else
            {
                // set SNCA
                NumClearAhead_MSTS = numdefs == 1 ? numClearAhead : -2;
                NumClearAhead_ORTS = numdefs == 2 ? numClearAhead : -2;
            }
        }

        static SignalFunction ReadSignalFunction(STFReader stf, bool ortsMode, IDictionary<string, SignalFunction> signalFunctions)
        {
            string type = stf.ReadStringBlock(null).ToUpper();
            if (!ortsMode && !Enum.IsDefined(typeof(MstsSignalFunction), type))
            {
                STFException.TraceInformation(stf, "Skipped unknown SignalFnType " + type);
                return SignalFunction.INFO;
            }
            else if (signalFunctions.ContainsKey(type))
            {
                return signalFunctions[type];
            }
            else
            {
                STFException.TraceInformation(stf, "Skipped unknown ORTSSignalFnType " + type);
                return SignalFunction.INFO;
            }
        }

        static string ReadORTSNormalSubtype(STFReader stf, IList<string> ORTSNormalSubtypes)
        {
            string type = stf.ReadStringBlock(null);
            if (ORTSNormalSubtypes.Contains(type.ToUpper()))
            {
                return type.ToUpper();
            }
            else
            {
                STFException.TraceInformation(stf, "Skipped unknown ORTSNormalSubtype " + type);
                return String.Empty;
            }
        }

        static IList<SignalLight> ReadLights(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<SignalLight> lights = new List<SignalLight>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signallight", ()=>{
                    if (lights.Count >= lights.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra SignalLight");
                    else
                        lights.Add(new SignalLight(stf));
                }),
            });
            lights.Sort(SignalLight.Comparer);
            for (int i = 0; i < lights.Count; i++)
                if (lights[i].Index != i)
                    STFException.TraceWarning(stf, "Invalid SignalLight index; expected " + i + ", got " + lights[i].Index);
            return lights;
        }

        static IDictionary<string, SignalDrawState> ReadDrawStates(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            Dictionary<string, SignalDrawState> drawStates = new Dictionary<string, SignalDrawState>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signaldrawstate", ()=>{
                    if (drawStates.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalDrawState");
                    else
                    {
                        SignalDrawState drawState = new SignalDrawState(stf);
                        if (drawStates.ContainsKey(drawState.Name))
                        {
                            string TempNew = "DST";
                            TempNew = String.Concat(TempNew,drawStates.Count.ToString());
                            drawStates.Add(TempNew, drawState);
                            STFException.TraceInformation(stf, "Duplicate SignalDrawState name \'"+drawState.Name+"\', using name \'"+TempNew+"\' instead");
                        }
                        else
                        {
                            drawStates.Add(drawState.Name, drawState);
                        }
                    }
                }),
            });
            if (drawStates.Count < count)
                STFException.TraceWarning(stf, (count - drawStates.Count).ToString() + " missing SignalDrawState(s)");
            return drawStates;
        }

        static IList<SignalAspect> ReadAspects(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<SignalAspect> aspects = new List<SignalAspect>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalaspect", ()=>{
                    if (aspects.Count >= aspects.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra SignalAspect");
                    else
                    {
                        SignalAspect aspect = new SignalAspect(stf);
                        if (aspects.Any(sa => sa.Aspect == aspect.Aspect))
                            STFException.TraceWarning(stf, "Skipped duplicate SignalAspect " + aspect.Aspect);
                        else
                            aspects.Add(aspect);
                    }
                }),
            });
            return aspects;
        }

        static ApproachControlLimits ReadApproachControlDetails(STFReader stf)
        {
            stf.MustMatch("(");
            var details = new ApproachControlLimits(stf);
            return (details);
        }

        /// <summary>
        /// This method returns the default draw state for the specified aspect or -1 if none.
        /// </summary>
        public int def_draw_state(MstsSignalAspect state)
        {
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (state == Aspects[i].Aspect)
                {
                    return DrawStates[Aspects[i].DrawStateName].Index;
                }
            }
            return -1;
        }

        /// <summary>
        /// This method returns the next least restrictive aspect from the one specified.
        /// </summary>
        public MstsSignalAspect GetNextLeastRestrictiveState(MstsSignalAspect state)
        {
            MstsSignalAspect targetState = MstsSignalAspect.UNKNOWN;
            MstsSignalAspect leastState = MstsSignalAspect.STOP;

            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect > leastState) leastState = Aspects[i].Aspect;
                if (Aspects[i].Aspect > state && Aspects[i].Aspect < targetState) targetState = Aspects[i].Aspect;
            }
            if (targetState == MstsSignalAspect.UNKNOWN) return leastState; else return targetState;
        }

        /// <summary>
        /// This method returns the most restrictive aspect for this signal type.
        /// </summary>
        public MstsSignalAspect GetMostRestrictiveAspect()
        {
            MstsSignalAspect targetAspect = MstsSignalAspect.UNKNOWN;
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect < targetAspect) targetAspect = Aspects[i].Aspect;
            }
            if (targetAspect == MstsSignalAspect.UNKNOWN) return MstsSignalAspect.STOP; else return targetAspect;
        }

        /// <summary>
        /// This method returns the least restrictive aspect for this signal type.
        /// [Rob Roeterdink] added for basic signals without script
        /// </summary>
        public MstsSignalAspect GetLeastRestrictiveAspect()
        {
            MstsSignalAspect targetAspect = MstsSignalAspect.STOP;
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect > targetAspect) targetAspect = Aspects[i].Aspect;
            }
            if (targetAspect > MstsSignalAspect.CLEAR_2) return MstsSignalAspect.CLEAR_2; else return targetAspect;
        }

        /// <summary>
        /// This method returns the lowest speed limit linked to the aspect
        /// </summary>
        public float GetSpeedLimitMpS(MstsSignalAspect aspect)
        {
            for (int i = 0; i < Aspects.Count; i++)
                if (Aspects[i].Aspect == aspect)
                    return Aspects[i].SpeedMpS;
            return -1;
        }

    }
    #endregion

    #region SignalLight
    /// <summary>
    /// Describes the a light on a signal, so the location and size of a signal light,
    /// as well as a reference to a light from the lights table
    /// </summary>
    public class SignalLight
    {
        /// <summary>Index in the list of signal lights</summary>
        public readonly uint Index;
        /// <summary>Name of the reference light from the lights table</summary>
        public readonly string Name;
        /// <summary>X-offset from the sub-object origin</summary>
        public float X { get; private set; }
        /// <summary>Y-offset from the sub-object origin</summary>
        public float Y { get; private set; }
        /// <summary>Z-offset from the sub-object origin</summary>
        public float Z { get; private set; }
        /// <summary>Radius of the light</summary>
        public float Radius { get; private set; }
        /// <summary>is the SIGLIGHT flag SEMAPHORE_CHANGE set?</summary>
        public bool SemaphoreChange { get; private set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public SignalLight(STFReader stf)
        {
            stf.MustMatch("(");
            Index = stf.ReadUInt(null);
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "semaphore_change": SemaphoreChange = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }

        /// <summary>
        /// Comparator function for ordering signal lights
        /// </summary>
        /// <param name="lightA">first light to compare</param>
        /// <param name="lightB">second light to compare</param>
        /// <returns>integer describing whether first light needs to be sorted before second light (so less than 0, 0, or larger than 0)</returns>
        public static int Comparer(SignalLight lightA, SignalLight lightB)
        {
            return (int)lightA.Index - (int)lightB.Index;
        }
    }
    #endregion

    #region SignalDrawState
    /// <summary>
    /// Describes a draw state: a single combination of lights and semaphore arm positions that go together.
    /// </summary>
    public class SignalDrawState
    {
        /// <summary>Index in the list of draw states</summary>
        public readonly int Index;
        /// <summary>Name identifying the draw state</summary>
        public readonly string Name;
        /// <summary>The lights to draw in this state</summary>
        public IList<SignalDrawLight> DrawLights { get; private set; }
        /// <summary>The position of the semaphore for this draw state (as a keyframe)</summary>
        public float SemaphorePos { get; private set; }

        /// <summary>
        /// constructor for dummy entries
        /// </summary>
        /// <param name="reqName">Requested name</param>
        /// <param name="reqIndex">Requested index</param>
        public SignalDrawState(string reqName, int reqIndex)
        {
            Index = reqIndex;
            Name = reqName;
            DrawLights = null;
        }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public SignalDrawState(STFReader stf)
        {
            stf.MustMatch("(");
            Index = stf.ReadInt(null);
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("drawlights", ()=>{ DrawLights = ReadDrawLights(stf); }),
                new STFReader.TokenProcessor("semaphorepos", ()=>{ SemaphorePos = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
            });
        }

        static IList<SignalDrawLight> ReadDrawLights(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<SignalDrawLight> drawLights = new List<SignalDrawLight>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("drawlight", ()=>{
                    if (drawLights.Count >= drawLights.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra DrawLight");
                    else
                        drawLights.Add(new SignalDrawLight(stf));
                }),
            });
            return drawLights;
        }

        /// <summary>
        /// Comparator function for ordering signal draw states
        /// </summary>
        /// <param name="drawStateA">first draw state to compare</param>
        /// <param name="drawStateB">second draw state to compare</param>
        /// <returns>integer describing whether first draw state needs to be sorted before second state (so less than 0, 0, or larger than 0)</returns>
        public static int Comparer(SignalDrawState drawStateA, SignalDrawState drawStateB)
        {
            return (int)drawStateA.Index - (int)drawStateB.Index;
        }
    }
    #endregion

    #region SignalDrawLight
    /// <summary>
    /// Describes a single light to be drawn as part of a draw state
    /// </summary>
    public class SignalDrawLight
    {
        /// <summary>Index in the list of draw lights</summary>
        public readonly uint LightIndex;
        /// <summary>Is the light flashing or not</summary>
        public bool Flashing { get; private set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public SignalDrawLight(STFReader stf)
        {
            stf.MustMatch("(");
            LightIndex = stf.ReadUInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "flashing": Flashing = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown DrawLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }
    }
    #endregion

    #region SignalAspect
    /// <summary>
    /// Describes an signal aspect, a combination of a signal indication state and what it means to be in that state.
    /// </summary>
    public class SignalAspect
    {
        /// <summary>The signal aspect or rather signal indication state itself</summary>
        public readonly MstsSignalAspect Aspect;
        /// <summary>The name of the Draw State for this signal aspect</summary>
        public readonly string DrawStateName;
        /// <summary>Speed limit (meters per second) for this aspect. -1 if track speed is to be used</summary>
        public float SpeedMpS { get; private set; }
        /// <summary>Set to true if SignalFlags ASAP option specified, meaning train needs to go to speed As Soon As Possible</summary>
        public bool Asap { get; private set; }
        /// <summary>Set to true if SignalFlags RESET option specified (ORTS only)</summary>
        public bool Reset; 
        /// <summary>Set to true if no speed reduction is required for RESTRICTED or STOP_AND_PROCEED aspects (ORTS only) </summary>
        public bool NoSpeedReduction;

        /// <summary>
        /// constructor for dummy entries
        /// </summary>
        /// <param name="reqAspect">Requested aspect</param>
        /// <param name="reqName">Requested drawstate name</param>
        public SignalAspect(MstsSignalAspect reqAspect, string reqName)
        {
            Aspect = reqAspect;
            DrawStateName = reqName;
            SpeedMpS = -1;
            Asap = false;
            NoSpeedReduction = false;
        }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public SignalAspect(STFReader stf)
        {
            SpeedMpS = -1;
            stf.MustMatch("(");
            string aspectName = stf.ReadString();
            try
            {
                Aspect = (MstsSignalAspect)Enum.Parse(typeof(MstsSignalAspect), aspectName, true);
            }
            catch (ArgumentException)
            {
                STFException.TraceInformation(stf, "Skipped unknown signal aspect " + aspectName);
                Aspect = MstsSignalAspect.UNKNOWN;
            }
            DrawStateName = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("speedmph", ()=>{ SpeedMpS = MpS.FromMpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedkph", ()=>{ SpeedMpS = MpS.FromKpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "asap": Asap = true; break;
                            case "or_speedreset": Reset = true; break;
                            case "or_nospeedreduction": NoSpeedReduction = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown DrawLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }
    }
    #endregion
    #region SignalShape
    /// <summary>
    /// Describes a signal object shape and the set of signal heads and other sub-objects that are present on this.
    /// </summary>
    
    public class ApproachControlLimits
    {
        public float? ApproachControlPositionM = null;
        public float? ApproachControlSpeedMpS = null;

        /// <summary>
        /// Constructor for dummy entries
        /// </summary>
        public ApproachControlLimits()
        {
        }

        public ApproachControlLimits(STFReader stf)
        {
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("positionmiles", ()=>{ ApproachControlPositionM = Me.FromMi(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("positionkm", ()=>{ ApproachControlPositionM = (stf.ReadFloatBlock(STFReader.UNITS.None, 0) * 1000); }),
                new STFReader.TokenProcessor("positionm", ()=>{ ApproachControlPositionM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("positionyd", ()=>{ ApproachControlPositionM = Me.FromYd(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedmph", ()=>{ ApproachControlSpeedMpS = MpS.FromMpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedkph", ()=>{ ApproachControlSpeedMpS = MpS.FromKpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedmps", ()=>{ ApproachControlSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
              });
        }
    }

    public class SignalShape
    {
        /// <summary>Name (without path) of the file that contains the shape itself</summary>
        public string ShapeFileName { get; private set; }
        /// <summary>Description of the signal shape</summary>
        public string Description { get; private set; }
        /// <summary>List of sub-objects that are belong to this shape</summary>
        public IList<SignalSubObj> SignalSubObjs { get; private set; }

        /// <summary>
        /// Default constructor used during file parsing.
        /// </summary>
        /// <param name="stf">The STFreader containing the file stream</param>
        public SignalShape(STFReader stf)
        {
            stf.MustMatch("(");
            ShapeFileName = Path.GetFileName(stf.ReadString()).ToUpper();
            Description = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalsubobjs", ()=>{ SignalSubObjs = ReadSignalSubObjects(stf); }),
            });
        }

        static IList<SignalSubObj> ReadSignalSubObjects(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            List<SignalSubObj> signalSubObjects = new List<SignalSubObj>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalsubobj", ()=>{
                    if (signalSubObjects.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalSubObj");
                    else
                    {
                        SignalSubObj signalSubObject = new SignalSubObj(stf);
                        if (signalSubObject.Index != signalSubObjects.Count)
                            STFException.TraceWarning(stf, "Invalid SignalSubObj index; expected " + signalSubObjects.Count + ", got " + signalSubObject.Index);
                        signalSubObjects.Add(signalSubObject);
                    }
                }),
            });
            if (signalSubObjects.Count < count)
                STFException.TraceWarning(stf, (count - signalSubObjects.Count).ToString() + " missing SignalSubObj(s)");
            return signalSubObjects;
        }

        /// <summary>
        /// Describes a sub-object belonging to a signal shape
        /// </summary>
        public class SignalSubObj
        {
            /// <summary>
            /// List of allowed signal sub types, as defined by MSTS (SIGSUBT_ values)
            /// </summary>
            public static IList<string> SignalSubTypes =
                    new[] {"DECOR","SIGNAL_HEAD","DUMMY1","DUMMY2",
				"NUMBER_PLATE","GRADIENT_PLATE","USER1","USER2","USER3","USER4"};
            // made public for access from SIGSCR processing
            // Altered to match definition in MSTS

            /// <summary></summary>
            public readonly int Index;
            /// <summary>Name of the group within the signal shape which defines this head</summary>
            public readonly string MatrixName;
            /// <summary></summary>
            public readonly string Description;
            /// <summary>Index of the signal sub type (decor, signal_head, ...). -1 if not specified</summary>
            public int SignalSubType { get; private set; }
            /// <summary>Signal Type of the this sub-object</summary>
            public string SignalSubSignalType { get; private set; }
            /// <summary>The sub-object is optional on this signal shape</summary>
            public bool Optional { get; private set; }
            /// <summary>The sub-object will be enabled by default (when manually placed)</summary>
            public bool Default { get; private set; }
            /// <summary>The sub-object is facing backwards w.r.t. rest of object</summary>
            public bool BackFacing { get; private set; }
            /// <summary>Signal should always have a junction link</summary>
            public bool JunctionLink { get; private set; }

            // SigSubJnLinkIf is not supported 

            /// <summary>
            /// Default constructor used during file parsing.
            /// </summary>
            /// <param name="stf">The STFreader containing the file stream</param>
            public SignalSubObj(STFReader stf)
            {
                SignalSubType = -1; // not (yet) specified
                stf.MustMatch("(");
                Index = stf.ReadInt(null);
                MatrixName = stf.ReadString().ToUpper();
                Description = stf.ReadString();
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("sigsubtype", ()=>{ SignalSubType = SignalSubTypes.IndexOf(stf.ReadStringBlock(null).ToUpper()); }),
                    new STFReader.TokenProcessor("sigsubstype", ()=>{ SignalSubSignalType = stf.ReadStringBlock(null).ToLowerInvariant(); }),
                    new STFReader.TokenProcessor("signalflags", ()=>{
                        stf.MustMatch("(");
                        while (!stf.EndOfBlock())
                            switch (stf.ReadString().ToLower())
                            {
                                case "optional": Optional = true; break;
                                case "default": Default = true; break;
                                case "back_facing": BackFacing = true; break;
                                case "jn_link": JunctionLink = true; break;
                                default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalSubObj flag " + stf.ReadString()); break;
                            }
                    }),
                });
            }
        }
    }
    #endregion
}
