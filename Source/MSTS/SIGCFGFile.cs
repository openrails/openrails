/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// This module parses the sigcfg file and builds an object model based on signal details
/// 
/// Author: Laurie Heath
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ORTS;

namespace MSTS
{
    public class SIGCFGFile
    {
		public readonly IDictionary<string, LightTexture> LightTextures;
		public readonly IDictionary<string, LightTableEntry> LightsTable;
		public readonly IDictionary<string, SignalType> SignalTypes;
		public readonly IDictionary<string, SignalShape> SignalShapes;
		public readonly IList<string> ScriptFiles;

        public SIGCFGFile(string filenamewithpath)
        {
            using (STFReader f = new STFReader(filenamewithpath))
                while (!f.EOF)
                    switch (f.ReadItem().ToLower())
                    {
                        case "lighttextures": LightTextures = ReadLightTextures(f); break;
                        case "lightstab": LightsTable = ReadLightsTable(f); break;
                        case "signaltypes": SignalTypes = ReadSignalTypes(f); break;
                        case "signalshapes": SignalShapes = ReadSignalShapes(f); break;
                        case "scriptfiles": ScriptFiles = ReadScriptFiles(f); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
        }

        static IDictionary<string, LightTexture> ReadLightTextures(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var lightTextures = new Dictionary<string, LightTexture>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "lighttex":
                        if (lightTextures.Count >= count)
                            Trace.TraceWarning("Skipped extra LightTex in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var lightTexture = new LightTexture(f);
                            if (lightTextures.ContainsKey(lightTexture.Name))
                                Trace.TraceWarning("Skipped duplicate LightTex '{2}' in {0}:line {1}", f.FileName, f.LineNumber, lightTexture.Name);
                            else
                                lightTextures.Add(lightTexture.Name, lightTexture);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (lightTextures.Count < count)
                Trace.TraceWarning("{2} missing LightTex in {0}:line {1}", f.FileName, f.LineNumber, count - lightTextures.Count);
            return lightTextures;
        }

        static IDictionary<string, LightTableEntry> ReadLightsTable(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var lightsTable = new Dictionary<string, LightTableEntry>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "lightstabentry":
                        if (lightsTable.Count >= count)
                            Trace.TraceWarning("Skipped extra LightsTabEntry in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var lightsTableEntry = new LightTableEntry(f);
                            if (lightsTable.ContainsKey(lightsTableEntry.Name))
                                Trace.TraceWarning("Skipped duplicate LightsTabEntry '{2}' in {0}:line {1}", f.FileName, f.LineNumber, lightsTableEntry.Name);
                            else
                                lightsTable.Add(lightsTableEntry.Name, lightsTableEntry);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (lightsTable.Count < count)
                Trace.TraceWarning("{2} missing LightsTabEntry in {0}:line {1}", f.FileName, f.LineNumber, count - lightsTable.Count);
            return lightsTable;
		}

        static IDictionary<string, SignalType> ReadSignalTypes(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var signalTypes = new Dictionary<string, SignalType>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signaltype":
                        if (signalTypes.Count >= count)
                            Trace.TraceWarning("Skipped extra SignalType in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var signalType = new SignalType(f);
                            if (signalTypes.ContainsKey(signalType.Name))
                                Trace.TraceWarning("Skipped duplicate SignalType '{2}' in {0}:line {1}", f.FileName, f.LineNumber, signalType.Name);
                            else
                                signalTypes.Add(signalType.Name, signalType);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (signalTypes.Count < count)
                Trace.TraceWarning("{2} missing SignalType in {0}:line {1}", f.FileName, f.LineNumber, count - signalTypes.Count);
            return signalTypes;
        }

        static IDictionary<string, SignalShape> ReadSignalShapes(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var signalShapes = new Dictionary<string, SignalShape>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalshape":
                        if (signalShapes.Count >= count)
                            Trace.TraceWarning("Skipped extra SignalShape in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var signalShape = new SignalShape(f);
                            if (signalShapes.ContainsKey(signalShape.ShapeFileName))
                                Trace.TraceWarning("Skipped duplicate SignalShape '{2}' in {0}:line {1}", f.FileName, f.LineNumber, signalShape.ShapeFileName);
                            else
                                signalShapes.Add(signalShape.ShapeFileName, signalShape);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (signalShapes.Count < count)
                Trace.TraceWarning("{2} missing SignalShape(s) in {0}:line {1}", f.FileName, f.LineNumber, count - signalShapes.Count);
            return signalShapes;
        }

        static IList<string> ReadScriptFiles(STFReader f)
        {
            f.MustMatch("(");
            var scriptFiles = new List<string>();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "scriptfile": scriptFiles.Add(f.ReadItemBlock(null)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            return scriptFiles;
        }
    }

	public class LightTexture
	{
		public readonly string Name, TextureFile;
		public readonly float u0, v0, u1, v1;

		public LightTexture(STFReader f)
		{
			f.MustMatch("(");
			Name = f.ReadItem();
			TextureFile = f.ReadItem();
            u0 = f.ReadFloat(STFReader.UNITS.None, null);
            v0 = f.ReadFloat(STFReader.UNITS.None, null);
            u1 = f.ReadFloat(STFReader.UNITS.None, null);
            v1 = f.ReadFloat(STFReader.UNITS.None, null);
			f.MustMatch(")");
		}
	}

	public class LightTableEntry
	{
		public readonly string Name;
		public readonly byte a, r, g, b;   // colour

		public LightTableEntry(STFReader f)
		{
			f.MustMatch("(");
			Name = f.ReadItem();
			if (f.ReadItem().ToLower() == "colour")
			{
				f.MustMatch("(");
                a = (byte)f.ReadUInt(STFReader.UNITS.None, null);
                r = (byte)f.ReadUInt(STFReader.UNITS.None, null);
                g = (byte)f.ReadUInt(STFReader.UNITS.None, null);
                b = (byte)f.ReadUInt(STFReader.UNITS.None, null);
				f.MustMatch(")");
			}
			else
			{
				STFException.ReportError(f, "'Colour' Expected");
			}
			f.MustMatch(")");
		}
	}

	public class SignalType
	{
		public enum FnTypes
		{
			Normal,
			Distance,
			Repeater,
			Shunting,
			Info,
		}

		public readonly string Name;
		public readonly FnTypes FnType;
		public readonly bool Abs, NoGantry, Semaphore;  // Don't know what Abs is for but found in Marias Pass route
		public readonly float FlashTimeOn = 1, FlashTimeOff = 1;  // On/Off duration for flashing light. (In seconds.)
		public readonly string LightTextureName;
		public readonly IList<SignalLight> Lights;
		public readonly IDictionary<string, SignalDrawState> DrawStates;
		public readonly IList<SignalAspect> Aspects;
		public readonly uint NumClearAhead;
		public readonly float SemaphoreInfo;

        public SignalType(STFReader f)
        {
            f.MustMatch("(");
            Name = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalfntype": FnType = ReadFnType(f); break;
                    case "signalflags":
                        f.MustMatch("(");
                        while (!f.EndOfBlock())
                            switch (f.ReadItem().ToLower())
                            {
                                case "abs": Abs = true; break;
                                case "no_gantry": NoGantry = true; break;
                                case "semaphore": Semaphore = true; break;
                                default: f.StepBackOneItem(); STFException.ReportWarning(f, "Unknown Signal Type Flag " + f.ReadItem()); break;
                            }
                        break;
                    case "sigflashduration":
                        f.MustMatch("(");
                        FlashTimeOn = f.ReadFloat(STFReader.UNITS.None, null);
                        FlashTimeOff = f.ReadFloat(STFReader.UNITS.None, null);
                        f.SkipRestOfBlock();
                        break;
                    case "signallighttex": LightTextureName = ReadLightTextureName(f); break;
                    case "signallights": Lights = ReadLights(f); break;
                    case "signaldrawstates": DrawStates = ReadDrawStates(f); break;
                    case "signalaspects": Aspects = ReadAspects(f); break;
                    case "signalnumclearahead": NumClearAhead = f.ReadUIntBlock(STFReader.UNITS.None, null); break;
                    case "semaphoreinfo": SemaphoreInfo = f.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

		static FnTypes ReadFnType(STFReader f)
		{
			try
			{
                return (FnTypes)Enum.Parse(typeof(FnTypes), f.ReadItemBlock(null), true);
			}
			catch (ArgumentException error)
			{
                throw new STFException(f, "Unknown SignalFnType: " + error.Message);
			}
		}

		static string ReadLightTextureName(STFReader f)
		{
            return f.ReadItemBlock(null);
		}

        static IList<SignalLight> ReadLights(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var lights = new List<SignalLight>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signallight":
                        if (lights.Count >= lights.Capacity)
                            Trace.TraceWarning("Skipped extra SignalLight in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                            lights.Add(new SignalLight(f));
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            lights.Sort(SignalLight.Comparer);
            for (var i = 0; i < lights.Count; i++)
                if (lights[i].Index != i)
                    throw new STFException(f, "SignalLight index out of range: " + lights[i].Index);
            return lights;
		}

        static IDictionary<string, SignalDrawState> ReadDrawStates(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var drawStates = new Dictionary<string, SignalDrawState>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signaldrawstate":
                        if (drawStates.Count >= count)
                            Trace.TraceWarning("Skipped extra SignalDrawState in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var drawState = new SignalDrawState(f);
                            if (drawStates.ContainsKey(drawState.Name))
                                Trace.TraceWarning("Skipped duplicate SignalDrawState '{2}' in {0}:line {1}", f.FileName, f.LineNumber, drawState.Name);
                            else
                                drawStates.Add(drawState.Name, drawState);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (drawStates.Count < count)
                Trace.TraceWarning("{2} missing SignalDrawState in {0}:line {1}", f.FileName, f.LineNumber, count - drawStates.Count);
            return drawStates;
		}

        static IList<SignalAspect> ReadAspects(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var aspects = new List<SignalAspect>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalaspect":
                        if (aspects.Count >= aspects.Capacity)
                            Trace.TraceWarning("Skipped extra SignalAspect in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                            aspects.Add(new SignalAspect(f));
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            return aspects;
		}

		/// <summary>
		/// This method returns the default draw state for the specified aspect or -1 if none.
		/// </summary>
		public int def_draw_state(SignalHead.SIGASP state)
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
        public SignalHead.SIGASP GetNextLeastRestrictiveState(SignalHead.SIGASP state)
        {
            SignalHead.SIGASP targetState = SignalHead.SIGASP.UNKNOWN;
            SignalHead.SIGASP leastState = SignalHead.SIGASP.STOP;

            for (int i=0;i< Aspects.Count;i++)
            {
                if (Aspects[i].Aspect > leastState) leastState = Aspects[i].Aspect;
                if (Aspects[i].Aspect > state && Aspects[i].Aspect < targetState) targetState = Aspects[i].Aspect;
            }
            if (targetState == SignalHead.SIGASP.UNKNOWN) return leastState; else return targetState;
        }

        /// <summary>
		/// This method returns the most restrictive aspect for this signal type.
        /// </summary>
        public SignalHead.SIGASP GetMostRestrictiveAspect()
        {
            SignalHead.SIGASP targetAspect = SignalHead.SIGASP.UNKNOWN;
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect < targetAspect) targetAspect = Aspects[i].Aspect;
            }
            if (targetAspect == SignalHead.SIGASP.UNKNOWN) return SignalHead.SIGASP.STOP; else return targetAspect;
        }
	}

	public class SignalLight
	{
		public readonly uint Index;
		public readonly string Name;
		public readonly float X, Y, Z;      // position
		public readonly float Radius;

        public SignalLight(STFReader f)
        {
            f.MustMatch("(");
            Index = f.ReadUInt(STFReader.UNITS.None, null);
            Name = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "position":
                        f.MustMatch("(");
                        X = f.ReadFloat(STFReader.UNITS.None, null);
                        Y = f.ReadFloat(STFReader.UNITS.None, null);
                        Z = f.ReadFloat(STFReader.UNITS.None, null);
                        f.MustMatch(")");
                        break;
                    case "radius":
                        Radius = f.ReadFloatBlock(STFReader.UNITS.None, null);
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

		public static int Comparer(SignalLight lightA, SignalLight lightB)
		{
			return (int)lightA.Index - (int)lightB.Index;
		}
	}

	public class SignalDrawState
	{
		public readonly int Index;
		public readonly string Name;
		public readonly IList<SignalDrawLight> DrawLights;

        public SignalDrawState(STFReader f)
        {
            f.MustMatch("(");
            Index = f.ReadInt(STFReader.UNITS.None, null);
            Name = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "drawlights": DrawLights = ReadDrawLights(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

        static IList<SignalDrawLight> ReadDrawLights(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var drawLights = new List<SignalDrawLight>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "drawlight":
                        if (drawLights.Count >= drawLights.Capacity)
                            Trace.TraceWarning("Skipped extra DrawLight in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                            drawLights.Add(new SignalDrawLight(f));
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            return drawLights;
		}

		public static int Comparer(SignalDrawState drawStateA, SignalDrawState drawStateB)
		{
			return (int)drawStateA.Index - (int)drawStateB.Index;
		}
	}

	public class SignalDrawLight
	{
		public readonly uint LightIndex;
		public readonly bool Flashing;

		public SignalDrawLight(STFReader f)
		{
            f.MustMatch("(");
            LightIndex = f.ReadUInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalflags":
                        f.MustMatch("(");
                        while (!f.EndOfBlock())
                            switch (f.ReadItem().ToLower())
                            {
                                case "flashing": Flashing = true; break;
                                default: f.StepBackOneItem(); STFException.ReportWarning(f, "Unknown DrawLight Flag " + f.ReadItem()); break;
                            }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}
	}

	public class SignalAspect
	{
		public readonly ORTS.SignalHead.SIGASP Aspect;
		public readonly string DrawStateName;
		public readonly float SpeedMpS;  // Speed limit for this aspect. -1 if track speed is to be used
		public readonly bool Asap;  // Set to true if SignalFlags ASAP option specified

        public SignalAspect(STFReader f)
        {
            SpeedMpS = -1;
            f.MustMatch("(");
            var aspectName = f.ReadItem();
            try
            {
                Aspect = (ORTS.SignalHead.SIGASP)Enum.Parse(typeof(ORTS.SignalHead.SIGASP), aspectName, true);
            }
            catch (ArgumentException)
            {
                throw new STFException(f, "Unknown Aspect " + aspectName);
            }
            DrawStateName = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "speedmph": SpeedMpS = MpH.ToMpS(f.ReadFloatBlock(STFReader.UNITS.None, 0)); break;
                    case "speedkph": SpeedMpS = KpH.ToMpS(f.ReadFloatBlock(STFReader.UNITS.None, 0)); break;
                    case "signalflags":
                        f.MustMatch("(");
                        while (!f.EndOfBlock())
                            switch (f.ReadItem().ToLower())
                            {
                                case "asap": Asap = true; break;
                                default: f.StepBackOneItem(); STFException.ReportWarning(f, "Unknown DrawLight Flag " + f.ReadItem()); break;
                            }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
	}

	public class SignalShape
	{
		public string Description, ShapeFileName;
		public readonly IList<SignalSubObj> SignalSubObjs;

        public SignalShape(STFReader f)
        {
            f.MustMatch("(");
            ShapeFileName = f.ReadItem().ToUpper();
            Description = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalsubobjs": SignalSubObjs = ReadSignalSubObjects(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

        static IList<SignalSubObj> ReadSignalSubObjects(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt(STFReader.UNITS.None, null);
            var signalSubObjects = new List<SignalSubObj>(count);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "signalsubobj":
                        if (signalSubObjects.Count >= count)
                            Trace.TraceWarning("Skipped extra SignalSubObj in {0}:line {1}", f.FileName, f.LineNumber);
                        else
                        {
                            var signalSubObject = new SignalSubObj(f);
                            if (signalSubObject.Index != signalSubObjects.Count)
                                Trace.TraceWarning("Index of SignalSubObj is {2}; expected {3} in {0}:line {1}", f.FileName, f.LineNumber, signalSubObject.Index, signalSubObjects.Count);
                            signalSubObjects.Add(signalSubObject);
                        }
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
            if (signalSubObjects.Count < count)
                Trace.TraceWarning("{2} missing SignalSubObj in {0}:line {1}", f.FileName, f.LineNumber, count - signalSubObjects.Count);
            return signalSubObjects;
		}

		public class SignalSubObj
		{
			static IList<string> SignalSubTypes = new[] {"DECOR","SIGNAL_HEAD","NUMBER_PLATE","GRADIENT_PLATE","USER1","USER2","USER3","USER4"};

			public readonly int Index;
			public readonly string MatrixName;        // Name of the group within the signal shape which defines this head
			public readonly string Description;      // 
			public readonly int SignalSubType = -1;  // Signal sub type: -1 if not specified;
			public readonly string SignalSubSignalType;
			public readonly bool Optional = false;
			public readonly bool Default = false;
			public readonly bool BackFacing = false;
			public readonly bool JunctionLink = false;

            public SignalSubObj(STFReader f)
            {
                f.MustMatch("(");
                Index = f.ReadInt(STFReader.UNITS.None, null);
                MatrixName = f.ReadItem().ToUpper();
                Description = f.ReadItem();
                while (!f.EndOfBlock())
                    switch (f.ReadItem().ToLower())
                    {
                        case "sigsubtype": SignalSubType = ReadSignalSubType(f); break;
                        case "signalflags":
                            f.MustMatch("(");
                            while (!f.EndOfBlock())
                                switch (f.ReadItem().ToLower())
                                {
                                    case "optional": Optional = true; break;
                                    case "default": Default = true; break;
                                    case "back_facing": BackFacing = true; break;
                                    case "jn_link": JunctionLink = true; break;
                                    default: f.StepBackOneItem(); STFException.ReportWarning(f, "Unknown SignalSubObj flag " + f.ReadItem()); break;
                                }
                            break;
                        case "sigsubstype": SignalSubSignalType = ReadSignalSubSignalType(f); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
            }

			static int ReadSignalSubType(STFReader f)
			{
                return SignalSubTypes.IndexOf(f.ReadItemBlock(null).ToUpper());
			}

			static string ReadSignalSubSignalType(STFReader f)
			{
                return f.ReadItemBlock(null);
			}
		}
	}
}
