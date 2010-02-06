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

namespace MSTS
{
    public struct strLightTexture
    {
        public string TextureName, TextureFile;
        public float u0, v0, u1, v1;	       
    }

    public struct strLightTabEntry
    {
        public string LightName;
        public uint a, r, g, b;   // colour
    }

    public struct strSignalLight
    {
        public string LightName;
        public float x,y,z;      // position
        public float radius;
    }

    public struct strSignalAspect
    {
        public int signalAspect;   // Display aspect
        public int drawState;      // Index to drawstate table
        public float speed;        // Speed limit for this aspect. -1 if track speed is to be used
        public bool speedMPH;      // Set true if speed limit is MPH else KPH
        public bool asap;          // Set to true if SignalFlags ASAP option specified
    }

    public class SIGCFGFile
    {
        public strLightTexture[] lightTextures;
        public strLightTabEntry[] lightsTable;
        public SignalType[] SignalTypes;
        public SignalShape[] SignalShapes;
        public List<string> ScriptFiles;

        public SIGCFGFile(string filenamewithpath)
        {
            STFReader f = new STFReader(filenamewithpath);
            try
            {
                string token = f.ReadToken();
                while (token != "") // EOF
                {
                    if (token == ")") throw (new STFError(f, "Unexpected )"));
                    else if (0 == String.Compare(token, "LightTextures", true)) LightTextures(f);
                    else if (0 == String.Compare(token, "LightsTab", true)) LightsTab(f);
                    else if (0 == String.Compare(token, "SignalTypes", true)) SigTypes(f);
                    else if (0 == String.Compare(token, "SignalShapes", true)) SigShapes(f);
                    else if (0 == String.Compare(token, "ScriptFiles", true)) SignalScripts(f);
                    else f.SkipBlock();
                    token = f.ReadToken();
                }
            }
            finally
            {
                f.Close();
            }
        }

        private void LightTextures(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noTextures = f.ReadUInt();
            lightTextures = new strLightTexture[noTextures];
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "LightTex", true))
                {
                    if (count < noTextures)
                    {
                        lightTextures[count] = new strLightTexture();
                        f.VerifyStartOfBlock();
                        lightTextures[count].TextureName = f.ReadString();
                        lightTextures[count].TextureFile = f.ReadString();
                        lightTextures[count].u0 = f.ReadFloat();
                        lightTextures[count].v0 = f.ReadFloat();
                        lightTextures[count].u1 = f.ReadFloat();
                        lightTextures[count].v1 = f.ReadFloat();
                        f.MustMatch(")");
                        count++;
                    }
                    else
                    {
                        throw (new STFError(f, "LightTextures count mismatch"));
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noTextures) STFError.Report(f, "LightTextures count mismatch");
        }

        private void LightsTab(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noLights = f.ReadUInt();
            lightsTable = new strLightTabEntry[noLights];
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "LightsTabEntry", true))
                {
                    if (count < noLights)
                    {
                        lightsTable[count] = new strLightTabEntry();
                        f.VerifyStartOfBlock();
                        lightsTable[count].LightName = f.ReadString();
                        string Token1 = f.ReadToken();
                        if (0 == String.Compare(Token1, "Colour", true))
                        {
                            f.VerifyStartOfBlock();
                            lightsTable[count].a = f.ReadUInt();
                            lightsTable[count].r = f.ReadUInt();
                            lightsTable[count].g = f.ReadUInt();
                            lightsTable[count].b = f.ReadUInt();
                            f.MustMatch(")");
                        }
                        else
                        {
                            STFError.Report(f, "'Colour' Expected");
                        }
                        f.MustMatch(")");
                        count++;
                    }
                    else
                    {
                        throw (new STFError(f, "LightsTab count mismatch"));
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noLights) STFError.Report(f, "LightsTab count mismatch");
        }

        private void SigTypes(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noSigTypes = f.ReadUInt();
            SignalTypes = new SignalType[noSigTypes];
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalType", true))
                {
                    if (count < noSigTypes)
                    {
                        SignalTypes[count] = new SignalType(count, f, this);
                        count++;
                    }
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void SigShapes(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noSigShapes = f.ReadUInt();
            SignalShapes = new SignalShape[noSigShapes];
            uint count = 0;
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalShape", true))
                {
                    if (count < noSigShapes)
                    {
                        SignalShapes[count] = new SignalShape(count, f, this);
                        count++;
                    }
                    else throw (new STFError(f, "SigShapes count mismatch"));
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if (count != noSigShapes) STFError.Report(f, "SigShapes count mismatch");
        }

        private void SignalScripts(STFReader f)
        {
            ScriptFiles = new List<string>();
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "ScrptFile", true))
                {
                    string ScriptFile = f.ReadStringBlock();
                    ScriptFiles.Add(ScriptFile);
                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        //
        // This method returns the index to the sigtype array for the specified sigtype
        // Returns -1 if this cannot be found.
        //
        public int GetSignalTypeIndex(string sSigType)
        {
            for (int i =0;i<SignalTypes.Length;i++)
            {
                if(0==String.Compare(SignalTypes[i].typeName,sSigType,true))
                {
                    return i;
                }
            }
            return - 1;
        }
    }

    public class SignalType
    {
        private string[] SigFunctionTypes = { "NORMAL", "DISTANCE", "REPEATER", "SHUNTING","INFO" };
        private string[] SigAspectNames ={"STOP","STOP_AND_PROCEED","RESTRICTING","APPROACH_1","APPROACH_2",
                                          "APPROACH_3","APPROACH_4","CLEAR_1","CLEAR2","CLEAR_3","CLEAR_4"};
        public string typeName;
        public uint SignalFnType,SignalNumClearAhead;
        public int SignalLightTex; // Points to entry in light testure table
        public strSignalLight[] SignalLights;
        public strSignalAspect[] SignalAspects;
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
                if (token == "") throw (new STFError(f, "Missing )"));
                else if (0 == String.Compare(token, "SignalFnType", true)) SigFnType(f);
                else if (0 == String.Compare(token, "SignalFlags", true)) SignalFlags(f);
                else if (0 == String.Compare(token, "SigFlashDuration", true))
                {
                    f.VerifyStartOfBlock();
                    time_on = f.ReadFloat();
                    time_off = f.ReadFloat();
                    f.MustMatch(")");
                }
                else if (0 == String.Compare(token, "SignalLightTex", true))
                {
                    // Get index to light texture table.
                    if ((SignalLightTex = SigLightTex(f, sigcfg)) < 0)
                    {
                        throw new STFError(f, "Unknown SignalLightTex " + SignalLightTex);
                    }
                }
                else if (0 == String.Compare(token, "SignalLights", true)) SigLights(f);
                else if (0 == String.Compare(token, "SignalDrawStates ", true)) DrawStates(f,sigcfg);
                else if (0 == String.Compare(token, "SignalAspects ", true)) SigAspects(f);
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
            throw new STFError(f,"Unknown SignalFnType " + fnType);
        }

        //
        //  Scans the light texture table for the light texture name and return an index.
        //
        private int SigLightTex(STFReader f,SIGCFGFile sigcfg)
        {
            string slTex = f.ReadStringBlock();
            for (int i = 0;i< sigcfg.lightTextures.Length; i++)
            {
                if(0==String.Compare(slTex,sigcfg.lightTextures[i].TextureName,true))
                {
                    return i;   // return the index
                }
            }
            return -1;    // light texture not found.
        }

        private void SignalFlags(STFReader f)
        {
            f.VerifyStartOfBlock();
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SEMAPHORE", true)) semaphore = true;
                else if (0 == String.Compare(token, "NO_GANTRY", true)) noGantry = true;
                else if (0 == String.Compare(token, "ABS", true)) abs = true;
                else throw new STFError(f, "Unknown Signal Type Flag " + token);
                token = f.ReadToken();
            }
        }

        private void SigLights(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noLights = f.ReadUInt();
            SignalLights = new strSignalLight[noLights];
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SignalLight", true))
                {
                    f.VerifyStartOfBlock();
                    uint lightindex = f.ReadUInt();
                    if(lightindex >= noLights)
                    {
                        throw new STFError(f, "SignalLight index out of range: " + lightindex.ToString());
                    }
                    SignalLights[lightindex]=new strSignalLight();
                    SignalLights[lightindex].LightName=f.ReadString();
                    string token1 = f.ReadToken();
                    while (token1 != ")")
                    {
                        if (token == "") throw (new STFError(f, "Missing )"));  // EOF
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
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
                else if (0 == String.Compare(token, "SignalDrawState", true))
                {
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
            SignalAspects = new strSignalAspect[noAspects];
            string token = f.ReadToken();
            uint count=0;
            while(token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
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
        }

        private void SigAspect(uint count,STFReader f)
        {
            SignalAspects[count] = new strSignalAspect();
            SignalAspects[count].speed = -1;
            SignalAspects[count].asap = false;
            f.VerifyStartOfBlock();
            string aspectName = f.ReadString();
            if ((SignalAspects[count].signalAspect = GetAspect(aspectName)) < 0)
            {
                throw (new STFError(f, "Unknown Aspect " + aspectName));
            }
            string drawState = f.ReadString();
            if ((SignalAspects[count].drawState = GetDrawState(drawState)) < 0)
            {
                throw (new STFError(f, "Undefined draw state " + drawState));
            }
            string token = f.ReadToken();
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
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
                        STFError.Report(f, "Unrecognised signal flag " + signalFlag);
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
    }

    public class SignalDrawState
    {
        public struct strDrwaLights
        {
            public uint DrawLight;
            public bool flashing;
        }
        public strDrwaLights[] DrawLights;
        public String DrawStateName;
        public uint index;

        public SignalDrawState(STFReader f, SIGCFGFile sigcfg)
        {
            f.VerifyStartOfBlock();
            index = f.ReadUInt();
            DrawStateName = f.ReadString();
            string token = f.ReadToken();
            ///uint count=0;
            while(token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF  
                else if (0 == String.Compare(token, "DrawLight", true)) Lights(f);
                else f.SkipBlock();
                token = f.ReadToken();
            }
        }

        private void Lights(STFReader f)
        {
            f.VerifyStartOfBlock();
            uint noDrawLights = f.ReadUInt();
            DrawLights = new strDrwaLights[noDrawLights];
            string token = f.ReadToken();
            uint count = 0;
            while (token != ")")
            {
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF 
                else if (0 == String.Compare(token, "DrawLight", true))
                {
                    if (count < noDrawLights)
                    {
                        DrawLights[count] = new strDrwaLights();

                        DrawLights[count].DrawLight = f.ReadUIntBlock();
                        DrawLights[count].flashing = false;
                        token = f.ReadToken();
                        if (token == "") throw (new STFError(f, "Missing )"));  // EOF 
                        else if (0 == String.Compare(token, "SignalFlags", true))
                        {
                            string trLightFlags = f.ReadStringBlock();
                            if (0 == String.Compare(trLightFlags, "FLASHING", true))
                            {
                                DrawLights[count].flashing = true;
                                token = f.ReadToken();
                            }
                            else
                            {
                                STFError.Report(f, "Unrecognised TRLIGHT flag " + trLightFlags);
                            }
                        }
                        count++;
                    }
                    else throw (new STFError(f, "DrawLights count mismatch"));

                }
                else f.SkipBlock();
                token = f.ReadToken();
            }
            if(count!=noDrawLights) STFError.Report(f, "DrawLights count mismatch");
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
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
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
                if (token == "") throw (new STFError(f, "Missing )"));  // EOF
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
            public string node_name;        // Name of the group within the signal shape which defines this head
            public string description;      // 
            public int SignalSubType = -1;  // Signal sub type: -1 if not specified;
            public int SigSubSType = -1;    // index to Signal Type table. -1 if not used. 
            public bool optional = false;
            public bool bDefault = false;
            public bool back_facing = false;
            public bool jn_link = false;
            public uint[] SigSubJnLinkIfs;  // indexes to linked signal heads 
            public uint SigSubJnLinkIf = 0;

            public SignalSubObj(uint seq, STFReader f, SIGCFGFile sigcfg)
            {
                f.VerifyStartOfBlock();
                uint seqNo = f.ReadUInt();
                if (seqNo == seq)
                {
                    node_name = f.ReadString();
                    description = f.ReadString();
                    string token = f.ReadToken();
                    while (token != ")")
                    {
                        if (token == "") throw (new STFError(f, "Missing )"));  // EOF
                        else if (0 == String.Compare(token, "SigSubType", true)) SignalSubType = SigSubType(f);
                        else if (0 == String.Compare(token, "SignalFlags", true)) SigSubFlags(f);
                        else if (0 == String.Compare(token, "SigSubSType", true)) SigSubSType = SigSTtype(f, sigcfg);
                        else f.SkipBlock();
                        token = f.ReadToken();
                    }
                }
                else
                {
                    throw (new STFError(f, "SignalSubObj Sequence Missmatch"));
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
                    if (token == "") throw (new STFError(f, "Missing )"));  // EOF
                    else if (0 == String.Compare(token, "OPTIONAL", true)) optional=true;
                    else if (0 == String.Compare(token, "DEFAULT", true)) bDefault = true;
                    else if (0 == String.Compare(token, "BACK_FACING", true)) back_facing = true;
                    else if (0 == String.Compare(token, "JN_LINK", true)) jn_link = true;
                    token = f.ReadToken();
                }
            }

            private int SigSTtype(STFReader f, SIGCFGFile sigcfg)
            {
                string stType = f.ReadStringBlock();
                for(int i=0;i<sigcfg.SignalTypes.Length;i++)
                {
                    if (0 == String.Compare(stType, sigcfg.SignalTypes[i].typeName, true)) return i;   
                }
                throw (new STFError(f, "Unknown Signal Type "+stType));
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
