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
using System.Linq;
using System.Text;
using ORTS;

namespace MSTS
{
    public struct LightTexture
    {
        public string Name, TextureFile;
        public float u0, v0, u1, v1;	       
    }

    public struct LightTableEntry
    {
        public string Name;
        public byte a, r, g, b;   // colour
    }

    public class SIGCFGFile
    {
		public Dictionary<string, LightTexture> LightTextures = new Dictionary<string, LightTexture>();
		public Dictionary<string, LightTableEntry> LightsTable = new Dictionary<string, LightTableEntry>();
		public Dictionary<string, SignalType> SignalTypes = new Dictionary<string, SignalType>();
		public Dictionary<string, SignalShape> SignalShapes = new Dictionary<string, SignalShape>();
        public List<string> ScriptFiles;

        public SIGCFGFile(string filenamewithpath)
        {
            STFReader f = new STFReader(filenamewithpath);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") throw (new STFException(f, "Unexpected )"));
                    else if (0 == String.Compare(token, "LightTextures", true)) ReadLightTextures(f);
                    else if (0 == String.Compare(token, "LightsTab", true)) ReadLightsTab(f);
                    else if (0 == String.Compare(token, "SignalTypes", true)) ReadSigTypes(f);
                    else if (0 == String.Compare(token, "SignalShapes", true)) ReadSigShapes(f);
                    else if (0 == String.Compare(token, "ScriptFiles", true)) ReadSignalScripts(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
            }
        }

        void ReadLightTextures(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noTextures = f.ReadUInt();
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "LightTex", true))
                {
                    if (count < noTextures)
                    {
						var lightTexture = new LightTexture();
                        f.VerifyStartOfBlock();
						lightTexture.Name = f.ReadString();
						lightTexture.TextureFile = f.ReadString();
						lightTexture.u0 = f.ReadFloat();
						lightTexture.v0 = f.ReadFloat();
						lightTexture.u1 = f.ReadFloat();
						lightTexture.v1 = f.ReadFloat();
                        f.MustMatch(")");
						LightTextures[lightTexture.Name] = lightTexture;
                        count++;
                    }
                    else
                    {
                        throw (new STFException(f, "LightTextures count mismatch"));
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noTextures) STFException.ReportError(f, "LightTextures count mismatch");
        }

        void ReadLightsTab(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noLights = f.ReadUInt();
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "LightsTabEntry", true))
                {
                    if (count < noLights)
                    {
                        var lightsTableEntry = new LightTableEntry();
                        f.VerifyStartOfBlock();
						lightsTableEntry.Name = f.ReadString();
                        string Token1 = f.ReadToken();
                        if (0 == String.Compare(Token1, "Colour", true))
                        {
                            f.VerifyStartOfBlock();
							lightsTableEntry.a = (byte)f.ReadUInt();
							lightsTableEntry.r = (byte)f.ReadUInt();
							lightsTableEntry.g = (byte)f.ReadUInt();
							lightsTableEntry.b = (byte)f.ReadUInt();
                            f.MustMatch(")");
                        }
                        else
                        {
                            STFException.ReportError(f, "'Colour' Expected");
                        }
                        f.MustMatch(")");
						LightsTable[lightsTableEntry.Name] = lightsTableEntry;
                        count++;
                    }
                    else
                    {
                        throw (new STFException(f, "LightsTab count mismatch"));
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noLights) STFException.ReportError(f, "LightsTab count mismatch");
        }

        void ReadSigTypes(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noSigTypes = f.ReadUInt();
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalType", true))
                {
                    if (count < noSigTypes)
                    {
						var signalType = new SignalType(count, f, this);
                        SignalTypes[signalType.Name] = signalType;
                        count++;
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        void ReadSigShapes(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noSigShapes = f.ReadUInt();
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalShape", true))
                {
                    if (count < noSigShapes)
                    {
						var signalShape = new SignalShape(count, f, this);
                        SignalShapes[signalShape.ShapeFileName] = signalShape;
                        count++;
                    }
                    else throw (new STFException(f, "SigShapes count mismatch"));
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noSigShapes) STFException.ReportError(f, "SigShapes count mismatch");
        }

        void ReadSignalScripts(STFReader f)
        {
            ScriptFiles = new List<string>();
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "ScrptFile", true))
                {
                    string ScriptFile = f.ReadStringBlock();
                    ScriptFiles.Add(ScriptFile);
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
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
		public readonly float FlashTimeOn, FlashTimeOff;  // On/Off duration for flashing light. (In seconds.)
		public readonly string LightTextureName;
		public readonly IList<SignalLight> Lights;
		public readonly IList<SignalDrawState> DrawStates;
		public readonly IDictionary<string, SignalDrawState> DrawStatesByName;
		public readonly IList<SignalAspect> Aspects;
		public readonly uint NumClearAhead;
		public readonly float SemaphoreInfo;

		public SignalType(uint count, STFReader f, SIGCFGFile sigcfg)
		{
			f.VerifyStartOfBlock();
			Name = f.ReadString();
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalFnType", true)) FnType = ReadFnType(f);
				else if (0 == String.Compare(token, "SignalFlags", true))
				{
					f.VerifyStartOfBlock();
					var token1 = f.ReadToken();
					while (token1 != ")")
					{
						if (token1 == "") throw new STFException(f, "Missing )");
						else if (0 == String.Compare(token1, "ABS", true)) Abs = true;
						else if (0 == String.Compare(token1, "NO_GANTRY", true)) NoGantry = true;
						else if (0 == String.Compare(token1, "SEMAPHORE", true)) Semaphore = true;
						else throw new STFException(f, "Unknown Signal Type Flag " + token1);
						token1 = f.ReadToken();
					}
				}
				else if (0 == String.Compare(token, "SigFlashDuration", true))
				{
					f.VerifyStartOfBlock();
					FlashTimeOn = f.ReadFloat();
					FlashTimeOff = f.ReadFloat();
					f.MustMatch(")");
				}
				else if (0 == String.Compare(token, "SignalLightTex", true)) LightTextureName = ReadLightTextureName(f);
				else if (0 == String.Compare(token, "SignalLights", true)) Lights = ReadLights(f);
				else if (0 == String.Compare(token, "SignalDrawStates", true)) DrawStates = ReadDrawStates(f, sigcfg);
				else if (0 == String.Compare(token, "SignalAspects", true)) Aspects = ReadAspects(f);
				else if (0 == String.Compare(token, "SignalNumClearAhead", true))
				{
					NumClearAhead = f.ReadUIntBlock();
				}
				else if (0 == String.Compare(token, "SemaphoreInfo", true))
				{
					SemaphoreInfo = f.ReadFloat();
				}
				else f.SkipBlock();
				token = f.ReadToken();
			}
			DrawStatesByName = new Dictionary<string, SignalDrawState>(DrawStates.Count);
			foreach (var drawState in DrawStates)
				DrawStatesByName.Add(drawState.Name, drawState);
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
			f.VerifyStartOfBlock();
			var count = f.ReadInt();
			var lights = new List<SignalLight>(count);
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");  // EOF
				else if (0 == String.Compare(token, "SignalLight", true))
				{
					if (lights.Count < lights.Capacity)
						lights.Add(new SignalLight(f));
				}
				else f.SkipBlock();
				token = f.ReadToken();
			}
			lights.Sort(SignalLight.Comparer);
			for (var i = 0; i < lights.Count; i++)
				if (lights[i].Index != i)
					throw new STFException(f, "SignalLight index out of range: " + lights[i].Index);
			return lights;
		}

		static IList<SignalDrawState> ReadDrawStates(STFReader f, SIGCFGFile sigcfg)
		{
			f.VerifyStartOfBlock();
			var count = f.ReadInt();
			var drawStates = new List<SignalDrawState>(count);
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalDrawState", true))
				{
					if (drawStates.Count < drawStates.Capacity)
						drawStates.Add(new SignalDrawState(f));
				}
				else f.SkipBlock();
				token = f.ReadToken();
			}
			drawStates.Sort(SignalDrawState.Comparer);
			for (var i = 0; i < drawStates.Count; i++)
				if (drawStates[i].Index != i)
					throw new STFException(f, "SignalDrawState index out of range: " + drawStates[i].Index);
			return drawStates;
		}

		static IList<SignalAspect> ReadAspects(STFReader f)
		{
			f.VerifyStartOfBlock();
			var count = f.ReadInt();
			var aspects = new List<SignalAspect>(count);
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalAspect", true))
				{
					if (aspects.Count < aspects.Capacity)
						aspects.Add(new SignalAspect(f));
				}
				else f.SkipBlock();
				token = f.ReadToken();
			}
			return aspects;
		}

		//
		//  This method returns the default draw state for the specified aspect 
		//  - 1 if none.
		//
		public int def_draw_state(SignalHead.SIGASP state)
		{
			for (int i = 0; i < Aspects.Count; i++)
			{
				if (state == Aspects[i].Aspect)
				{
					return DrawStates.IndexOf(DrawStatesByName[Aspects[i].DrawStateName]);
				}
			}
			return -1;
		}

		//
		//  This method returns the next least restrictive aspect
		//  from the one specified.
		//
		public SignalHead.SIGASP def_next_state(SignalHead.SIGASP state)
		{
			SignalHead.SIGASP next_state = SignalHead.SIGASP.UNKNOWN;
			for (int i = 0; i < Aspects.Count; i++)
			{
				//if (SignalAspects[i].signalAspect > state)
				//{
				//}
				if (Aspects[i].Aspect > state && Aspects[i].Aspect < next_state) next_state = Aspects[i].Aspect;
				//if (SignalAspects[i].signalAspect > state) next_state = SignalAspects[i].signalAspect;
			}
			if (next_state == SignalHead.SIGASP.UNKNOWN) return state; else return next_state;
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
			f.VerifyStartOfBlock();
			Index = f.ReadUInt();
			Name = f.ReadString();
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "Position", true))
				{
					f.VerifyStartOfBlock();
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
				token = f.ReadToken();
			}
		}

		public static int Comparer(SignalLight lightA, SignalLight lightB)
		{
			return (int)lightA.Index - (int)lightB.Index;
		}
	}

	public class SignalDrawState
	{
		public readonly uint Index;
		public readonly string Name;
		public readonly IList<SignalDrawLight> DrawLights;

		public SignalDrawState(STFReader f)
		{
			f.VerifyStartOfBlock();
			Index = f.ReadUInt();
			Name = f.ReadString();
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "DrawLights", true)) DrawLights = ReadDrawLights(f);
				else f.SkipBlock();
				token = f.ReadToken();
			}
		}

		static IList<SignalDrawLight> ReadDrawLights(STFReader f)
		{
			f.VerifyStartOfBlock();
			var count = f.ReadInt();
			var drawLights = new List<SignalDrawLight>(count);
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "DrawLight", true))
				{
					if (drawLights.Count < drawLights.Capacity)
						drawLights.Add(new SignalDrawLight(f));
				}
				else f.SkipBlock();
				token = f.ReadToken();
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
			f.VerifyStartOfBlock();
			LightIndex = f.ReadUInt();
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SignalFlags", true))
				{
					var flag = f.ReadStringBlock();
					if (0 == String.Compare(flag, "Flashing", true))
					{
						Flashing = true;
						token = f.ReadToken();
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
			f.VerifyStartOfBlock();
			var aspectName = f.ReadString();
			try
			{
				Aspect = (ORTS.SignalHead.SIGASP)Enum.Parse(typeof(ORTS.SignalHead.SIGASP), aspectName, true);
			}
			catch (ArgumentException)
			{
				throw new STFException(f, "Unknown Aspect " + aspectName);
			}
			DrawStateName = f.ReadString();
			var token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw new STFException(f, "Missing )");
				else if (0 == String.Compare(token, "SpeedMPH ", true))
				{
					SpeedMpS = MpH.ToMpS(f.ReadFloatBlock());
				}
				else if (0 == String.Compare(token, "SpeedKPH ", true))
				{
					SpeedMpS = KpH.ToMpS(f.ReadFloatBlock());
				}
				else if (0 == String.Compare(token, "SignalFlags ", true))
				{
					var signalFlag = f.ReadStringBlock();
					if (0 == String.Compare(signalFlag, "ASAP", true))
						Asap = true;
					else
						throw new STFException(f, "Unrecognised signal flag " + signalFlag);
				}
				else f.SkipBlock();
				token = f.ReadToken();
			}
		}
	}

	public class SignalShape
	{
		public string Description, ShapeFileName;
		public SignalSubObj[] SignalSubObjs;

		public SignalShape(uint count, STFReader f, SIGCFGFile sigcfg)
		{
			f.VerifyStartOfBlock();
			ShapeFileName = f.ReadString();
			Description = f.ReadString();
			string token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw (new STFException(f, "Missing )"));  // EOF
				else if (0 == String.Compare(token, "SignalSubObjs", true)) SigSubObjs(f, sigcfg);
				//else if (0 == String.Compare(token, "SignalFlagss", true)) SignalShapeFlags(f);
				else f.SkipBlock();
				token = f.ReadToken();
			}
		}

		private void SigSubObjs(STFReader f, SIGCFGFile sigcfg)
		{
			f.VerifyStartOfBlock();
			uint noObj = f.ReadUInt();
			SignalSubObjs = new SignalSubObj[noObj];
			uint count = 0;
			string token = f.ReadToken();
			while (token != ")")
			{
				if (token == "") throw (new STFException(f, "Missing )"));  // EOF
				else if (0 == String.Compare(token, "SignalSubObj", true))
				{
					SignalSubObjs[count] = new SignalSubObj(count, f, sigcfg);
					count++;
				}
				else f.SkipBlock();
				token = f.ReadToken();

			}

			//
			//  Found a reference to this in the documentation but no details
			//  Commented out for the time being
			//
			//private void SignalShapeFlags(STFReader f)
			//{
			//}
		}

		public class SignalSubObj
		{
			private string[] SignalSubTypes ={"DECOR","SIGNAL_HEAD","NUMBER_PLATE","GRADIENT_PLATE",
                                              "USER1","USER2","USER3","USER4"};
			public string MatrixName;        // Name of the group within the signal shape which defines this head
			public string Description;      // 
			public int SignalSubType = -1;  // Signal sub type: -1 if not specified;
			public string SigSubSType;
			public bool Optional = false;
			public bool Default = false;
			public bool BackFacing = false;
			public bool JunctionLink = false;
			public uint[] SigSubJnLinkIfs;  // indexes to linked signal heads 
			public uint SigSubJnLinkIf = 0;

			public SignalSubObj(uint seq, STFReader f, SIGCFGFile sigcfg)
			{
				f.VerifyStartOfBlock();
				uint seqNo = f.ReadUInt();
				if (seqNo == seq)
				{
					MatrixName = f.ReadString().ToUpper();
					Description = f.ReadString();
					string token = f.ReadToken();
					while (token != ")")
					{
						if (token == "") throw (new STFException(f, "Missing )"));  // EOF
						else if (0 == String.Compare(token, "SigSubType", true)) SignalSubType = SigSubType(f);
						else if (0 == String.Compare(token, "SignalFlags", true)) SigSubFlags(f);
						else if (0 == String.Compare(token, "SigSubSType", true)) SigSubSType = SigSTtype(f);
						else f.SkipBlock();
						token = f.ReadToken();
					}
				}
				else
				{
					throw (new STFException(f, "SignalSubObj Sequence Missmatch"));
				}

			}

			private int SigSubType(STFReader f)
			{
				string subType = f.ReadStringBlock();
				for (int i = 0; i < SignalSubTypes.Length; i++)
				{
					if (0 == String.Compare(subType, SignalSubTypes[i], true))
					{
						return i;
					}
				}
				return -1;
			}

			private void SigSubFlags(STFReader f)
			{
				f.VerifyStartOfBlock();
				string token = f.ReadToken();
				while (token != ")")
				{
					if (token == "") throw (new STFException(f, "Missing )"));  // EOF
					else if (0 == String.Compare(token, "OPTIONAL", true)) Optional = true;
					else if (0 == String.Compare(token, "DEFAULT", true)) Default = true;
					else if (0 == String.Compare(token, "BACK_FACING", true)) BackFacing = true;
					else if (0 == String.Compare(token, "JN_LINK", true)) JunctionLink = true;
					token = f.ReadToken();
				}
			}

			private string SigSTtype(STFReader f)
			{
				return f.ReadStringBlock();
			}

			private void SubJnLinkIf(STFReader f)
			{
				f.VerifyStartOfBlock();
				SigSubJnLinkIf = f.ReadUInt();
				SigSubJnLinkIfs = new uint[SigSubJnLinkIf];
				for (int i = 0; i < SigSubJnLinkIf; i++)
				{
					SigSubJnLinkIfs[i] = f.ReadUInt();
				}
				f.MustMatch(")");
			}
		}
	}
}
