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
            {
                string token = f.ReadItem();
                while (token != "") // EOF
                {
                    if (token == ")") throw new STFException(f, "Unexpected )");
					else if (0 == String.Compare(token, "LightTextures", true)) LightTextures = ReadLightTextures(f);
                    else if (0 == String.Compare(token, "LightsTab", true)) LightsTable = ReadLightsTable(f);
                    else if (0 == String.Compare(token, "SignalTypes", true)) SignalTypes = ReadSignalTypes(f);
                    else if (0 == String.Compare(token, "SignalShapes", true)) SignalShapes = ReadSignalShapes(f);
                    else if (0 == String.Compare(token, "ScriptFiles", true)) ScriptFiles = ReadScriptFiles(f);
                    else f.SkipBlock();
                    token = f.ReadItem();
                }
            }
        }

        static IDictionary<string, LightTexture> ReadLightTextures(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt();
			var lightTextures = new Dictionary<string, LightTexture>(count);
			var token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "LightTex", true))
                {
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
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
			if (lightTextures.Count < count)
				Trace.TraceWarning("{2} missing LightTex in {0}:line {1}", f.FileName, f.LineNumber, count - lightTextures.Count);
			return lightTextures;
		}

		static IDictionary<string, LightTableEntry> ReadLightsTable(STFReader f)
        {
            f.MustMatch("(");
            var count = f.ReadInt();
			var lightsTable = new Dictionary<string, LightTableEntry>(count);
            var token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "LightsTabEntry", true))
                {
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
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
			if (lightsTable.Count < count)
				Trace.TraceWarning("{2} missing LightsTabEntry in {0}:line {1}", f.FileName, f.LineNumber, count - lightsTable.Count);
			return lightsTable;
		}

		static IDictionary<string, SignalType> ReadSignalTypes(STFReader f)
        {
            f.MustMatch("(");
			var count = f.ReadInt();
			var signalTypes = new Dictionary<string, SignalType>(count);
			var token = f.ReadItem();
			while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                else if (0 == String.Compare(token, "SignalType", true))
                {
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
                }
                else f.SkipBlock();
                token = f.ReadItem();
            }
			if (signalTypes.Count < count)
				Trace.TraceWarning("{2} missing SignalType in {0}:line {1}", f.FileName, f.LineNumber, count - signalTypes.Count);
			return signalTypes;
		}

        static IDictionary<string, SignalShape> ReadSignalShapes(STFReader f)
        {
            f.MustMatch("(");
			var count = f.ReadInt();
			var signalShapes = new Dictionary<string, SignalShape>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalShape", true))
				{
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
				}
				else f.SkipBlock();
				token = f.ReadItem();
			}
			if (signalShapes.Count < count)
				Trace.TraceWarning("{2} missing SignalShape in {0}:line {1}", f.FileName, f.LineNumber, count - signalShapes.Count);
			return signalShapes;
        }

        static IList<string> ReadScriptFiles(STFReader f)
        {
            f.MustMatch("(");
			var scriptFiles = new List<string>();
            var token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "ScriptFile", true))
                {
					scriptFiles.Add(f.ReadStringBlock());
                }
                else f.SkipBlock();
                token = f.ReadItem();
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
			u0 = f.ReadFloat();
			v0 = f.ReadFloat();
			u1 = f.ReadFloat();
			v1 = f.ReadFloat();
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
			var token = f.ReadItem();
			if (0 == String.Compare(token, "Colour", true))
			{
				f.MustMatch("(");
				a = (byte)f.ReadUInt();
				r = (byte)f.ReadUInt();
				g = (byte)f.ReadUInt();
				b = (byte)f.ReadUInt();
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
			var token = f.ReadItem();
			while (token != ")")
			{
                if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalFnType", true)) FnType = ReadFnType(f);
				else if (0 == String.Compare(token, "SignalFlags", true))
				{
					f.MustMatch("(");
					var token1 = f.ReadItem();
					while (token1 != ")")
					{
                        if (token1 == "") throw new STFException(f, "Missing )");
						else if (0 == String.Compare(token1, "ABS", true)) Abs = true;
						else if (0 == String.Compare(token1, "NO_GANTRY", true)) NoGantry = true;
						else if (0 == String.Compare(token1, "SEMAPHORE", true)) Semaphore = true;
						else throw new STFException(f, "Unknown Signal Type Flag " + token1);
						token1 = f.ReadItem();
					}
				}
				else if (0 == String.Compare(token, "SigFlashDuration", true))
				{
					f.MustMatch("(");
					FlashTimeOn = f.ReadFloat();
					FlashTimeOff = f.ReadFloat();
					f.MustMatch(")");
				}
				else if (0 == String.Compare(token, "SignalLightTex", true)) LightTextureName = ReadLightTextureName(f);
				else if (0 == String.Compare(token, "SignalLights", true)) Lights = ReadLights(f);
				else if (0 == String.Compare(token, "SignalDrawStates", true)) DrawStates = ReadDrawStates(f);
				else if (0 == String.Compare(token, "SignalAspects", true)) Aspects = ReadAspects(f);
				else if (0 == String.Compare(token, "SignalNumClearAhead", true))
				{
					NumClearAhead = f.ReadUIntBlock();
				}
				else if (0 == String.Compare(token, "SemaphoreInfo", true))
				{
					SemaphoreInfo = f.ReadFloatBlock();
				}
				else f.SkipBlock();
				token = f.ReadItem();
			}
		}

		static FnTypes ReadFnType(STFReader f)
		{
			try
			{
				return (FnTypes)Enum.Parse(typeof(FnTypes), f.ReadStringBlock(), true);
			}
			catch (ArgumentException error)
			{
                throw new STFException(f, "Unknown SignalFnType: " + error.Message);
			}
		}

		static string ReadLightTextureName(STFReader f)
		{
			return f.ReadStringBlock();
		}

		static IList<SignalLight> ReadLights(STFReader f)
		{
			f.MustMatch("(");
			var count = f.ReadInt();
			var lights = new List<SignalLight>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
                if (token == "") throw new STFException(f, "Missing )");  // EOF
				else if (0 == String.Compare(token, "SignalLight", true))
				{
					if (lights.Count >= lights.Capacity)
						Trace.TraceWarning("Skipped extra SignalLight in {0}:line {1}", f.FileName, f.LineNumber);
					else
						lights.Add(new SignalLight(f));
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
			var count = f.ReadInt();
			var drawStates = new Dictionary<string, SignalDrawState>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
                if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalDrawState", true))
				{
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
				}
				else f.SkipBlock();
				token = f.ReadItem();
			}
			if (drawStates.Count < count)
				Trace.TraceWarning("{2} missing SignalDrawState in {0}:line {1}", f.FileName, f.LineNumber, count - drawStates.Count);
			return drawStates;
		}

		static IList<SignalAspect> ReadAspects(STFReader f)
		{
			f.MustMatch("(");
			var count = f.ReadInt();
			var aspects = new List<SignalAspect>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalAspect", true))
				{
					if (aspects.Count >= aspects.Capacity)
						Trace.TraceWarning("Skipped extra SignalAspect in {0}:line {1}", f.FileName, f.LineNumber);
					else
						aspects.Add(new SignalAspect(f));
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
			Index = f.ReadUInt();
			Name = f.ReadItem();
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "Position", true))
				{
					f.MustMatch("(");
					X = f.ReadFloat();
					Y = f.ReadFloat();
					Z = f.ReadFloat();
					f.MustMatch(")");
				}
				else if (0 == String.Compare(token, "Radius", true))
				{
					Radius = f.ReadFloatBlock();
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
			Index = f.ReadInt();
			Name = f.ReadItem();
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "DrawLights", true)) DrawLights = ReadDrawLights(f);
				else f.SkipBlock();
				token = f.ReadItem();
			}
		}

		static IList<SignalDrawLight> ReadDrawLights(STFReader f)
		{
			f.MustMatch("(");
			var count = f.ReadInt();
			var drawLights = new List<SignalDrawLight>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "DrawLight", true))
				{
					if (drawLights.Count >= drawLights.Capacity)
						Trace.TraceWarning("Skipped extra DrawLight in {0}:line {1}", f.FileName, f.LineNumber);
					else
						drawLights.Add(new SignalDrawLight(f));
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
			LightIndex = f.ReadUInt();
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalFlags", true))
				{
					var flag = f.ReadStringBlock();
					if (0 == String.Compare(flag, "Flashing", true))
					{
						Flashing = true;
						token = f.ReadItem();
					}
					else
					{
						STFException.ReportError(f, "Unrecognised DrawLight flag " + flag);
					}
				}
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
			var token = f.ReadItem();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SpeedMPH", true))
				{
					SpeedMpS = MpH.ToMpS(f.ReadFloatBlock(true));
				}
				else if (0 == String.Compare(token, "SpeedKPH", true))
				{
					SpeedMpS = KpH.ToMpS(f.ReadFloatBlock(true));
				}
				else if (0 == String.Compare(token, "SignalFlags", true))
				{
					var signalFlag = f.ReadStringBlock();
					if (0 == String.Compare(signalFlag, "ASAP", true))
						Asap = true;
					else
						throw new STFException(f, "Unrecognised signal flag " + signalFlag);
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
			string token = f.ReadItem();
			while (token != ")")
			{
                if (token == "") throw new STFException(f, "Missing )");  // EOF
				else if (0 == String.Compare(token, "SignalSubObjs", true)) SignalSubObjs = ReadSignalSubObjects(f);
				//else if (0 == String.Compare(token, "SignalFlagss", true)) SignalShapeFlags(f);
				else f.SkipBlock();
				token = f.ReadItem();
			}
		}

		static IList<SignalSubObj> ReadSignalSubObjects(STFReader f)
		{
			f.MustMatch("(");
			var count = f.ReadInt();
			var signalSubObjects = new List<SignalSubObj>(count);
			var token = f.ReadItem();
			while (token != ")")
			{
                if (token == "") throw new STFException(f, "Missing )");  // EOF
				else if (0 == String.Compare(token, "SignalSubObj", true))
				{
					if (signalSubObjects.Count >= count)
						Trace.TraceWarning("Skipped extra SignalSubObj in {0}:line {1}", f.FileName, f.LineNumber);
					else
					{
						var signalSubObject = new SignalSubObj(f);
						if (signalSubObject.Index != signalSubObjects.Count)
							Trace.TraceWarning("Index of SignalSubObj is {2}; expected {3} in {0}:line {1}", f.FileName, f.LineNumber, signalSubObject.Index, signalSubObjects.Count);
						signalSubObjects.Add(signalSubObject);
					}
				}
				else f.SkipBlock();
				token = f.ReadItem();
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
				Index = f.ReadInt();
				MatrixName = f.ReadItem().ToUpper();
				Description = f.ReadItem();
				var token = f.ReadItem();
				while (token != ")")
				{
					if (token == "") throw new STFException(f, "Missing )");  // EOF
					else if (0 == String.Compare(token, "SigSubType", true)) SignalSubType = ReadSignalSubType(f);
					else if (0 == String.Compare(token, "SignalFlags", true))
					{
						f.MustMatch("(");
						var token1 = f.ReadItem();
						while (token1 != ")")
						{
							if (token1 == "") throw new STFException(f, "Missing )");
							else if (0 == String.Compare(token1, "OPTIONAL", true)) Optional = true;
							else if (0 == String.Compare(token1, "DEFAULT", true)) Default = true;
							else if (0 == String.Compare(token1, "BACK_FACING", true)) BackFacing = true;
							else if (0 == String.Compare(token1, "JN_LINK", true)) JunctionLink = true;
							else throw new STFException(f, "Unknown SignalSubObj flag " + token1);
							token1 = f.ReadItem();
						}
					}
					else if (0 == String.Compare(token, "SigSubSType", true)) SignalSubSignalType = ReadSignalSubSignalType(f);
					else f.SkipBlock();
					token = f.ReadItem();
				}
			}

			static int ReadSignalSubType(STFReader f)
			{
				return SignalSubTypes.IndexOf(f.ReadStringBlock().ToUpper());
			}

			static string ReadSignalSubSignalType(STFReader f)
			{
				return f.ReadStringBlock();
			}
		}
	}
}
