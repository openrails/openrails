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

    public struct SignalLight
    {
        public string Name;
        public float x,y,z;      // position
        public float radius;
    }

    public struct SignalAspect
    {
        public ORTS.SignalHead.SIGASP signalAspect;   // Display aspect
        public int drawState;      // Index to drawstate table
        public float speed;        // Speed limit for this aspect. -1 if track speed is to be used
        public bool speedMPH;      // Set true if speed limit is MPH else KPH
        public bool asap;          // Set to true if SignalFlags ASAP option specified
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
                        SignalTypes[signalType.typeName] = signalType;
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
        private static string[] SigFunctionTypes = { "NORMAL", "DISTANCE", "REPEATER", "SHUNTING","INFO" };
        private static string[] SigAspectNames ={"STOP","STOP_AND_PROCEED","RESTRICTING","APPROACH_1","APPROACH_2",
                                          "APPROACH_3","APPROACH_4","CLEAR_1","CLEAR_2","CLEAR_3","CLEAR_4"};
        public string typeName;
        public uint SignalFnType,SignalNumClearAhead;
		public string SignalLightTex;
        public SignalLight[] SignalLights;
        public SignalAspect[] SignalAspects;
        public SignalDrawState[] SignalDrawStates;
        public bool semaphore = false;
        public bool noGantry = false;
        public bool abs = false;                    // Don't know what this is for but found in Marias Pass route
        public float time_on = 0f, time_off = 0f;   // On/Off duration for flashing light. (In seconds.)

        public SignalType(uint count, STFReader f,SIGCFGFile sigcfg)
        {
            f.VerifyStartOfBlock();
            typeName = f.ReadString();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalFnType", true)) SigFnType(f);
                else if (0 == String.Compare(token, "SignalFlags", true)) SignalFlags(f);
                else if (0 == String.Compare(token, "SigFlashDuration", true))
                {
                    f.VerifyStartOfBlock();
                    time_on = f.ReadFloat();
                    time_off = f.ReadFloat();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "SignalLightTex", true)) SignalLightTex = SigLightTex(f);
                else if (0 == String.Compare(token, "SignalLights", true)) SigLights(f);
                else if (0 == String.Compare(token, "SignalDrawStates", true)) DrawStates(f,sigcfg);
                else if (0 == String.Compare(token, "SignalAspects", true)) SigAspects(f);
                else if (0 == String.Compare(token, "SignalNumClearAhead", true))
                {
                    SignalNumClearAhead=f.ReadUIntBlock();
                }

                else f.SkipBlock();
                token = f.ReadToken();

            }

        }

        private void SigFnType(STFReader f)
        {
            string fnType = f.ReadStringBlock();
            for (uint i = 0; i < SigFunctionTypes.Length; i++)
            {
                if(0==String.Compare(fnType,SigFunctionTypes[i],true))
                {
                    SignalFnType=i;
                    return;
                }
            }
            throw new STFException(f,"Unknown SignalFnType " + fnType);
        }

        //
        //  Scans the light texture table for the light texture name and return an index.
        //
        private string SigLightTex(STFReader f)
        {
            return f.ReadStringBlock();
        }

        private void SignalFlags(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SEMAPHORE", true)) semaphore = true;
                else if (0 == String.Compare(token, "NO_GANTRY", true)) noGantry = true;
                else if (0 == String.Compare(token, "ABS", true)) abs = true;
                else throw new STFException(f, "Unknown Signal Type Flag " + token);
                token = f.ReadToken();
            }
        }

        private void SigLights(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noLights = f.ReadUInt();
            SignalLights = new SignalLight[noLights];
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SignalLight", true))
                {
                    f.VerifyStartOfBlock();
                    uint lightindex = f.ReadUInt();
                    if(lightindex >= noLights)
                    {
                        throw new STFException(f, "SignalLight index out of range: " + lightindex.ToString());
                    }
                    SignalLights[lightindex]=new SignalLight();
                    SignalLights[lightindex].Name=f.ReadString();
                    string token1 = f.ReadToken();
                    while (token1 != ")")
                    {
                        if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                        else if (0 == String.Compare(token1, "Position", true))
                        {
                            f.VerifyStartOfBlock();
                            SignalLights[lightindex].x = f.ReadFloat();
                            SignalLights[lightindex].y = f.ReadFloat();
                            SignalLights[lightindex].z = f.ReadFloat();
                            f.MustMatch(")");
                        }
                        else if (0 == String.Compare(token1, "Radius", true))
                        {
                            SignalLights[lightindex].radius = f.ReadFloatBlock();
                        }
                        else f.SkipBlock();
                        token1 = f.ReadToken();
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void DrawStates(STFReader f,SIGCFGFile sigcfg)
        {
            f.VerifyStartOfBlock();
            uint noStates = f.ReadUInt();
            SignalDrawStates=new SignalDrawState[noStates];
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SignalDrawState", true))
                {
                    f.VerifyStartOfBlock();
                    uint index = f.ReadUInt();
                    if (index < noStates)
                    {
                        SignalDrawStates[index] = new SignalDrawState(f,sigcfg);
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void SigAspects(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noAspects = f.ReadUInt();
            SignalAspects = new SignalAspect[noAspects];
            string token = f.ReadToken();
            uint count=0;
            while(token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SignalAspect", true))
                {
                    if (count < noAspects)
                    {
                        SigAspect(count, f);
                        count++;
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
			if (count < noAspects)
			{
				noAspects = count;
				SignalAspects = SignalAspects.Take((int)noAspects).ToArray();
			}
        }

        private void SigAspect(uint count,STFReader f)
        {
            SignalAspects[count] = new SignalAspect();
            SignalAspects[count].speed = -1;
            SignalAspects[count].asap = false;
            f.VerifyStartOfBlock();
            string aspectName = f.ReadString();
            if ((SignalAspects[count].signalAspect = (SignalHead.SIGASP) GetAspect(aspectName)) < 0)
            {
                throw (new STFException(f, "Unknown Aspect " + aspectName));
            }
            string drawState = f.ReadString();
            if ((SignalAspects[count].drawState = GetDrawState(drawState)) < 0)
            {
                throw (new STFException(f, "Undefined draw state " + drawState));
            }
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SpeedMPH ", true))
                {
                    SignalAspects[count].speedMPH = true;
                    SignalAspects[count].speed = f.ReadFloatBlock();
                }
                else if (0 == String.Compare(token, "SpeedKPH ", true))
                {
                    SignalAspects[count].speedMPH = false;
                    SignalAspects[count].speed = f.ReadFloatBlock();
                }
                else if (0 == String.Compare(token, "SignalFlags ", true))
                {
                    string signalFlag = f.ReadStringBlock();
                    if (0 == String.Compare(signalFlag, "ASAP", true))
                    {
                        SignalAspects[count].asap = true;
                    }
                    else
                    {
                        STFException.ReportError(f, "Unrecognised signal flag " + signalFlag);
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private int GetAspect(string aspectName)
        {
            for (int i = 0; i < SigAspectNames.Length; i++)
            {
                if (0 == String.Compare(aspectName, SigAspectNames[i], true)) return i;
            }
            return -1;
        }

        private int GetDrawState(string drawState)
        {
            for (int i = 0; i < SignalDrawStates.Length; i++)
            {
                if (0 == String.Compare(drawState, SignalDrawStates[i].DrawStateName, true)) return i;
            }
            return -1;
        }

        //
        //  This method returns the default draw state for the specified aspect 
        //  - 1 if none.
        //
        public int def_draw_state( SignalHead.SIGASP state)
        {
            for (int i = 0; i < SignalAspects.Length;i++ )
            {
                if (state == SignalAspects[i].signalAspect)
                {
                    return SignalAspects[i].drawState;
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
            SignalHead.SIGASP next_state=SignalHead.SIGASP.UNKNOWN;
            for (int i=0;i< SignalAspects.Length;i++)
            {
                //if (SignalAspects[i].signalAspect > state)
                //{
                //}
                if (SignalAspects[i].signalAspect > state && SignalAspects[i].signalAspect < next_state) next_state = SignalAspects[i].signalAspect;
                //if (SignalAspects[i].signalAspect > state) next_state = SignalAspects[i].signalAspect;
            }
            if (next_state == SignalHead.SIGASP.UNKNOWN) return state; else return next_state;
        }
    }

    public class SignalDrawState
    {
        public struct strDrawLights
        {
            public uint DrawLight;
            public bool Flashing;
        }
        public strDrawLights[] DrawLights;
        public String DrawStateName;
        public uint index;

        public SignalDrawState(STFReader f, SIGCFGFile sigcfg)
        {
            //f.VerifyStartOfBlock();
            //index = f.ReadUInt();
            DrawStateName = f.ReadString();
            string token = f.ReadToken();
            ///uint count=0;
            while(token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF  
                else if (0 == String.Compare(token, "DrawLights", true)) Lights(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void Lights(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noDrawLights = f.ReadUInt();
            DrawLights = new strDrawLights[noDrawLights];
            string token = f.ReadToken();
            uint count = 0;
            while (token != ")")
            {
                if (token == "") throw (new STFException(f, "Missing )"));  // EOF 
                else if (0 == String.Compare(token, "DrawLight", true))
                {
                    if (count < noDrawLights)
                    {
                        DrawLights[count] = new strDrawLights();
                        f.VerifyStartOfBlock();
                        DrawLights[count].DrawLight = f.ReadUInt();
                        DrawLights[count].Flashing = false;
                        token = f.ReadToken();
                        while (token != ")")
                        {
                            if (token == "") throw (new STFException(f, "Missing )"));  // EOF 
                            else if (0 == String.Compare(token, "SignalFlags", true))
                            {
                                string trLightFlags = f.ReadStringBlock();
                                if (0 == String.Compare(trLightFlags, "FLASHING", true))
                                {
                                    DrawLights[count].Flashing = true;
                                    token = f.ReadToken();
                                }
                                else
                                {
                                    STFException.ReportError(f, "Unrecognised TRLIGHT flag " + trLightFlags);
                                }
                            }
                        }
                        count++;
                    }
                    else throw (new STFException(f, "DrawLights count mismatch"));

                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if(count!=noDrawLights) STFException.ReportError(f, "DrawLights count mismatch");
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
                else if (0 == String.Compare(token, "SignalSubObjs", true)) SigSubObjs(f,sigcfg);
                //else if (0 == String.Compare(token, "SignalFlagss", true)) SignalShapeFlags(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void SigSubObjs(STFReader f,SIGCFGFile sigcfg)
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
                    SignalSubObjs[count]=new SignalSubObj(count,f,sigcfg);
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
                SigSubJnLinkIf=f.ReadUInt();
                SigSubJnLinkIfs=new uint[SigSubJnLinkIf];
                for (int i = 0; i < SigSubJnLinkIf; i++)
                {
                    SigSubJnLinkIfs[i] = f.ReadUInt();
                }
                f.MustMatch(")");
            }
        }
    }

}
