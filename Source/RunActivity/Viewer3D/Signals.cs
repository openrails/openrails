// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

// Prints out lots of diagnostic information about the construction of signals from shape data and their state changes.
//#define DEBUG_SIGNAL_SHAPES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.Signalling;
using Orts.Viewer3D.Common;
using ORTS.Common;
using Event = Orts.Common.Event;
using Events = Orts.Common.Events;

namespace Orts.Viewer3D
{
    public class SignalShape : PoseableShape
    {
#if DEBUG_SIGNAL_SHAPES
        readonly uint UID;
#endif
        readonly bool[] SubObjVisible;
        readonly List<SignalShapeHead> Heads = new List<SignalShapeHead>();

        public SignalShape(Viewer viewer, SignalObj mstsSignal, string path, WorldPosition position, ShapeFlags flags)
            : base(viewer, path, position, flags)
        {
#if DEBUG_SIGNAL_SHAPES
            Console.WriteLine("{0} signal {1}:", Location.ToString(), mstsSignal.UID);
            UID = mstsSignal.UID;
#endif
            var signalShape = Path.GetFileName(path).ToUpper();
            if (!viewer.SIGCFG.SignalShapes.ContainsKey(signalShape))
            {
                Trace.TraceWarning("{0} signal {1} has invalid shape {2}.", Location.ToString(), mstsSignal.UID, signalShape);
                return;
            }
            var mstsSignalShape = viewer.SIGCFG.SignalShapes[signalShape];
#if DEBUG_SIGNAL_SHAPES
            Console.WriteLine("  Shape={0} SubObjs={1,-2} {2}", Path.GetFileNameWithoutExtension(path).ToUpper(), mstsSignalShape.SignalSubObjs.Count, mstsSignalShape.Description);
#endif

            // The matrix names are used as the sub-object names. The sub-object visibility comes from
            // mstsSignal.SignalSubObj, which is mapped to names through mstsSignalShape.SignalSubObjs.
            var visibleMatrixNames = new bool[SharedShape.MatrixNames.Count];
            for (var i = 0; i < mstsSignalShape.SignalSubObjs.Count; i++)
                if ((((mstsSignal.SignalSubObj >> i) & 0x1) == 1) && (SharedShape.MatrixNames.Contains(mstsSignalShape.SignalSubObjs[i].MatrixName)))
                    visibleMatrixNames[SharedShape.MatrixNames.IndexOf(mstsSignalShape.SignalSubObjs[i].MatrixName)] = true;

            // All sub-objects except the one pointing to the first matrix (99.00% times it is the first one, but not always, see Protrain) are hidden by default.
            //For each other sub-object, look up its name in the hierarchy and use the visibility of that matrix. 
            visibleMatrixNames[0] = true;
            SubObjVisible = new bool[SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length];
            SubObjVisible[0] = true;
            for (var i = 1; i < SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length; i++)
            {
                if (i == SharedShape.RootSubObjectIndex) SubObjVisible[i] = true;
                else
                {
                    var subObj = SharedShape.LodControls[0].DistanceLevels[0].SubObjects[i];
                    int minHiLevIndex = 0;
                    if (subObj.ShapePrimitives[0].Hierarchy[subObj.ShapePrimitives[0].HierarchyIndex] > 0)
                    // Search for ShapePrimitive with lowest Hierarchy Value and check visibility with it
                    {
                        var minHiLev = 999;
                        for (var j = 0; j < subObj.ShapePrimitives.Length; j++)
                        {
                            if (subObj.ShapePrimitives[0].Hierarchy[subObj.ShapePrimitives[j].HierarchyIndex] < minHiLev)
                            {
                                minHiLevIndex = j;
                                minHiLev = subObj.ShapePrimitives[0].Hierarchy[subObj.ShapePrimitives[j].HierarchyIndex];
                            }
                        }
                    }
                    SubObjVisible[i] = visibleMatrixNames[SharedShape.LodControls[0].DistanceLevels[0].SubObjects[i].ShapePrimitives[minHiLevIndex].HierarchyIndex];
                }
            }

#if DEBUG_SIGNAL_SHAPES
            for (var i = 0; i < mstsSignalShape.SignalSubObjs.Count; i++)
                Console.WriteLine("  SUBOBJ {1,-12} {0,-7} {2,3} {3,3} {4,2} {5,2} {6,-14} {8} ({7})", ((mstsSignal.SignalSubObj >> i) & 0x1) != 0 ? "VISIBLE" : "hidden", mstsSignalShape.SignalSubObjs[i].MatrixName, mstsSignalShape.SignalSubObjs[i].Optional ? "Opt" : "", mstsSignalShape.SignalSubObjs[i].Default ? "Def" : "", mstsSignalShape.SignalSubObjs[i].JunctionLink ? "JL" : "", mstsSignalShape.SignalSubObjs[i].BackFacing ? "BF" : "", mstsSignalShape.SignalSubObjs[i].SignalSubType == -1 ? "<none>" : MSTS.SignalShape.SignalSubObj.SignalSubTypes[mstsSignalShape.SignalSubObjs[i].SignalSubType], mstsSignalShape.SignalSubObjs[i].SignalSubSignalType, mstsSignalShape.SignalSubObjs[i].Description);
            for (var i = 0; i < SubObjVisible.Length; i++)
                Console.WriteLine("  SUBOBJ {0,-2} {1,-7}", i, SubObjVisible[i] ? "VISIBLE" : "hidden");
#endif

            if (mstsSignal.SignalUnits == null)
            {
                Trace.TraceWarning("{0} signal {1} has no SignalUnits.", Location.ToString(), mstsSignal.UID);
                return;
            }

            for (var i = 0; i < mstsSignal.SignalUnits.Units.Length; i++)
            {
#if DEBUG_SIGNAL_SHAPES
                Console.Write("  UNIT {0}: TrItem={1,-5} SubObj={2,-2}", i, mstsSignal.SignalUnits.Units[i].TrItem, mstsSignal.SignalUnits.Units[i].SubObj);
#endif
                // Find the simulation SignalObject for this shape.
                var signalAndHead = viewer.Simulator.Signals.FindByTrItem(mstsSignal.SignalUnits.Units[i].TrItem);
                if (!signalAndHead.HasValue)
                {
                    Trace.TraceWarning("Skipped {0} signal {1} unit {2} with invalid TrItem {3}", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].TrItem);
                    continue;
                }
                // Get the signal sub-object for this unit (head).
                var mstsSignalSubObj = mstsSignalShape.SignalSubObjs[mstsSignal.SignalUnits.Units[i].SubObj];
                if (mstsSignalSubObj.SignalSubType != 1) // SIGNAL_HEAD
                {
                    Trace.TraceWarning("Skipped {0} signal {1} unit {2} with invalid SubObj {3}", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].SubObj);
                    continue;
                }
                var mstsSignalItem = (SignalItem)(viewer.Simulator.TDB.TrackDB.TrItemTable[mstsSignal.SignalUnits.Units[i].TrItem]);
                try
                {
                    // Go create the shape head.
                    Heads.Add(new SignalShapeHead(viewer, this, i, signalAndHead.Value.Value, mstsSignalItem, mstsSignalSubObj));
                }
                catch (InvalidDataException error)
                {
                    Trace.TraceWarning(error.Message);
                }
#if DEBUG_SIGNAL_SHAPES
                Console.WriteLine();
#endif
            }
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Locate relative to the camera
            var dTileX = Location.TileX - Viewer.Camera.TileX;
            var dTileZ = Location.TileZ - Viewer.Camera.TileZ;
            var xnaTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            Matrix.Multiply(ref Location.XNAMatrix, ref xnaTileTranslation, out xnaTileTranslation);

            foreach (var head in Heads)
                head.PrepareFrame(frame, elapsedTime, xnaTileTranslation);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, SubObjVisible, Flags);
        }

        public override void Unload()
        {
            foreach (var head in Heads)
                head.Unload();
            base.Unload();
        }

        internal override void Mark()
        {
            foreach (var head in Heads)
                head.Mark();
            base.Mark();
        }

        class SignalShapeHead
        {
            readonly Viewer Viewer;
            readonly SignalShape SignalShape;
#if DEBUG_SIGNAL_SHAPES
            readonly int Index;
#endif
            readonly SignalHead SignalHead;
            readonly List<int> MatrixIndices = new List<int>();
            readonly SignalTypeData SignalTypeData;
            readonly SoundSource Sound;
            float CumulativeTime;
            float SemaphorePos;
            float SemaphoreTarget;
            float SemaphoreSpeed;
            List<AnimatedPart> SemaphoreParts = new List<AnimatedPart>();
            int DisplayState = -1;

            private readonly SignalLightState[] lightStates;

            public SignalShapeHead(Viewer viewer, SignalShape signalShape, int index, SignalHead signalHead,
                        SignalItem mstsSignalItem, Formats.Msts.SignalShape.SignalSubObj mstsSignalSubObj)
            {
                Viewer = viewer;
                SignalShape = signalShape;
#if DEBUG_SIGNAL_SHAPES
                Index = index;
#endif
                SignalHead = signalHead;
                for (int mindex = 0; mindex <= signalShape.SharedShape.MatrixNames.Count - 1; mindex++)
                {
                    string MatrixName = signalShape.SharedShape.MatrixNames[mindex];
                    if (String.Equals(MatrixName, mstsSignalSubObj.MatrixName))
                        MatrixIndices.Add(mindex);
                }


                if (!viewer.SIGCFG.SignalTypes.ContainsKey(mstsSignalSubObj.SignalSubSignalType))
                    return;

                var mstsSignalType = viewer.SIGCFG.SignalTypes[mstsSignalSubObj.SignalSubSignalType];

                SignalTypeData = viewer.SignalTypeDataManager.Get(mstsSignalType);

                if (SignalTypeData.Semaphore)
                {
                    // Check whether we have to correct the Semaphore position indexes following the strange rule of MSTS
                    // Such strange rule is that, if there are only two animation steps in the related .s file, MSTS behaves as follows:
                    // a SemaphorePos (2) in sigcfg.dat is executed as SemaphorePos (1)
                    // a SemaphorePos (1) in sigcfg.dat is executed as SemaphorePos (0)
                    // a SemaphorePos (0) in sigcfg.dat is executed as SemaphorePos (0)
                    // First we check if there are only two animation steps
                    if (signalShape.SharedShape.Animations != null && signalShape.SharedShape.Animations.Count != 0 && MatrixIndices.Count > 0 &&
                            signalShape.SharedShape.Animations[0].anim_nodes[MatrixIndices[0]].controllers.Count != 0 &&
                            signalShape.SharedShape.Animations[0].anim_nodes[MatrixIndices[0]].controllers[0].Count == 2)
                    {

                        // OK, now we check if maximum SemaphorePos is 2 (we won't correct if there are only SemaphorePos 1 and 0,
                        // because they would both be executed as SemaphorePos (0) accordingly to above law, therefore leading to a static semaphore)
                        float maxIndex = float.MinValue;
                        foreach (SignalAspectData drAsp in SignalTypeData.DrawAspects.Values)
                        {
                            if (drAsp.SemaphorePos > maxIndex) maxIndex = drAsp.SemaphorePos;
                        }
                        if (maxIndex == 2)
                        {
                            // in this case we modify the SemaphorePositions for compatibility with MSTS.
                            foreach (SignalAspectData drAsp in SignalTypeData.DrawAspects.Values)
                            {
                                switch ((int)drAsp.SemaphorePos)
                                {
                                    case 2:
                                        drAsp.SemaphorePos = 1;
                                        break;
                                    case 1:
                                        drAsp.SemaphorePos = 0;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (!SignalTypeData.AreSemaphoresReindexed)
                            {
                                Trace.TraceInformation("Reindexing semaphore entries of signal type {0} for compatibility with MSTS", mstsSignalType.Name);
                                SignalTypeData.AreSemaphoresReindexed = true;
                            }
                        }
                    }

                    foreach (int mindex in MatrixIndices)
                    {
                        if (mindex == 0 && (signalShape.SharedShape.Animations == null || signalShape.SharedShape.Animations.Count == 0 ||
                            signalShape.SharedShape.Animations[0].anim_nodes[mindex].controllers.Count == 0))
                            continue;
                        AnimatedPart SemaphorePart = new AnimatedPart(signalShape);
                        SemaphorePart.AddMatrix(mindex);
                        SemaphoreParts.Add(SemaphorePart);
                    }

                    if (Viewer.Simulator.TRK.Tr_RouteFile.DefaultSignalSMS != null)
                    {
                        var soundPath = Viewer.Simulator.RoutePath + @"\\sound\\" + Viewer.Simulator.TRK.Tr_RouteFile.DefaultSignalSMS;
                        try
                        {
                            Sound = new SoundSource(Viewer, SignalShape.Location.WorldLocation, Events.Source.MSTSSignal, soundPath);
                            Viewer.SoundProcess.AddSoundSources(this, new List<SoundSourceBase>() { Sound });
                        }
                        catch (Exception error)
                        {
                            Trace.WriteLine(new FileLoadException(soundPath, error));
                        }
                    }
                }

                lightStates = new SignalLightState[SignalTypeData.Lights.Count];
                for (var i = 0; i < SignalTypeData.Lights.Count; i++)
                    lightStates[i] = new SignalLightState(SignalTypeData.OnOffTimeS);

#if DEBUG_SIGNAL_SHAPES
                Console.Write("  HEAD type={0,-8} lights={1,-2} sem={2}", SignalTypeData.Type, SignalTypeData.Lights.Count, SignalTypeData.Semaphore);
#endif
            }
            [CallOnThread("Loader")]
            public void Unload()
            {
                if (Sound != null)
                {
                    Viewer.SoundProcess.RemoveSoundSources(this);
                    Sound.Dispose();
                }
            }

            public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, Matrix xnaTileTranslation)
            {
                var initialise = DisplayState == -1;
                if (DisplayState != SignalHead.draw_state)
                {
#if DEBUG_SIGNAL_SHAPES
                    Console.WriteLine("{5} {0} signal {1} unit {2} state: {3} --> {4}",
                        SignalShape.Location, SignalShape.UID, Index, DisplayState,
                        SignalHead.draw_state, InfoDisplay.FormattedTime(Viewer.Simulator.ClockTime));
#endif
                    DisplayState = SignalHead.draw_state;
                    if (SignalTypeData.DrawAspects.ContainsKey(DisplayState))
                    {
                        SemaphoreTarget = SignalTypeData.DrawAspects[DisplayState].SemaphorePos;
                        SemaphoreSpeed = SignalTypeData.SemaphoreAnimationTime <= 0 ? 0 : (SemaphoreTarget > SemaphorePos ? +1 : -1) / SignalTypeData.SemaphoreAnimationTime;
                        if (Sound != null) Sound.HandleEvent(Event.SemaphoreArm);
                    }
                }

                CumulativeTime += elapsedTime.ClockSeconds;
                while (CumulativeTime > SignalTypeData.FlashTimeTotal)
                    CumulativeTime -= SignalTypeData.FlashTimeTotal;

                if (DisplayState < 0 || !SignalTypeData.DrawAspects.ContainsKey(DisplayState))
                    return;

                if (SignalTypeData.Semaphore)
                {
                    // We reset the animation matrix before preparing the lights, because they need to be positioned
                    // based on the original matrix only.
                    foreach (AnimatedPart SemaphorePart in SemaphoreParts)
                    {
                        SemaphorePart.SetFrameWrap(0);
                    }
                }

                for (var i = 0; i < SignalTypeData.Lights.Count; i++)
                {
                    SignalLightState state = lightStates[i];
                    bool semaphoreDark = SemaphorePos != SemaphoreTarget && SignalTypeData.LightsSemaphoreChange[i];
                    bool constantDark = !SignalTypeData.DrawAspects[DisplayState].DrawLights[i];
                    bool flashingDark = SignalTypeData.DrawAspects[DisplayState].FlashLights[i] && (CumulativeTime > SignalTypeData.FlashTimeOn);
                    state.UpdateIntensity(semaphoreDark || constantDark || flashingDark ? 0 : 1, elapsedTime);
                    if (!state.IsIlluminated())
                        continue;

                    bool isDay;
                    if (Viewer.Settings.UseMSTSEnv == false)
                        isDay = Viewer.World.Sky.SolarDirection.Y > 0;
                    else
                        isDay = Viewer.World.MSTSSky.mstsskysolarDirection.Y > 0;
                    bool isPoorVisibility = Viewer.Simulator.Weather.VisibilityM < 200;
                    if (!SignalTypeData.DayLight && isDay && !isPoorVisibility)
                        continue;

                    var slp = SignalTypeData.Lights[i];
                    var xnaMatrix = Matrix.CreateTranslation(slp.Position);

                    foreach (int MatrixIndex in MatrixIndices)
                    {
                        Matrix.Multiply(ref xnaMatrix, ref SignalShape.XNAMatrices[MatrixIndex], out xnaMatrix);
                    }
                    Matrix.Multiply(ref xnaMatrix, ref xnaTileTranslation, out xnaMatrix);

                    void renderEffect(Material material)
                    {
                        frame.AddPrimitive(material, slp, RenderPrimitiveGroup.Lights, ref xnaMatrix, ShapeFlags.None, state);
                    }
                    renderEffect(slp.Material);
                    if (Viewer.Settings.SignalLightGlow)
                        renderEffect(SignalTypeData.GlowMaterial);
                }

                if (SignalTypeData.Semaphore)
                {
                    // Now we update and re-animate the semaphore arm.
                    if (SignalTypeData.SemaphoreAnimationTime <= 0 || initialise)
                    {
                        // No timing (so instant switch) or we're initialising.
                        SemaphorePos = SemaphoreTarget;
                        SemaphoreSpeed = 0;
                    }
                    else
                    {
                        // Animate slowly to target position.
                        SemaphorePos += SemaphoreSpeed * elapsedTime.ClockSeconds;
                        if (SemaphorePos * Math.Sign(SemaphoreSpeed) > SemaphoreTarget * Math.Sign(SemaphoreSpeed))
                        {
                            SemaphorePos = SemaphoreTarget;
                            SemaphoreSpeed = 0;
                        }
                    }
                    foreach (AnimatedPart SemaphorePart in SemaphoreParts)
                    {
                        SemaphorePart.SetFrameCycle(SemaphorePos);
                    }
                }
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                SignalTypeData.Mark();
            }
        }
    }

    public class SignalTypeDataManager
    {
        readonly Viewer Viewer;

        Dictionary<string, SignalTypeData> SignalTypes = new Dictionary<string, SignalTypeData>();
        Dictionary<string, bool> SignalTypesMarks = new Dictionary<string, bool>();

        public SignalTypeDataManager(Viewer viewer)
        {
            Viewer = viewer;
        }

        public SignalTypeData Get(SignalType mstsSignalType)
        {
            if (!SignalTypes.ContainsKey(mstsSignalType.Name))
            {
                SignalTypes[mstsSignalType.Name] = new SignalTypeData(Viewer, mstsSignalType);
            }

            return SignalTypes[mstsSignalType.Name];
        }

        public void Mark()
        {
            SignalTypesMarks.Clear();
            foreach (string signalTypeName in SignalTypes.Keys)
            {
                SignalTypesMarks.Add(signalTypeName, false);
            }
        }

        public void Mark(SignalTypeData signalType)
        {
            if (SignalTypes.ContainsValue(signalType))
            {
                SignalTypesMarks[SignalTypes.First(x => x.Value == signalType).Key] = true;
            }
        }

        public void Sweep()
        {
            foreach (var signalTypeName in SignalTypesMarks.Where(x => !x.Value).Select(x => x.Key))
            {
                SignalTypes.Remove(signalTypeName);
            }
        }
    }

    public class SignalTypeData
    {
        readonly Viewer Viewer;

        public readonly Material GlowMaterial;
#if DEBUG_SIGNAL_SHAPES
            public readonly SignalTypeDataType Type;
#endif
        public readonly List<SignalLightPrimitive> Lights = new List<SignalLightPrimitive>();
        public readonly List<bool> LightsSemaphoreChange = new List<bool>();
        public readonly Dictionary<int, SignalAspectData> DrawAspects = new Dictionary<int, SignalAspectData>();
        public readonly float FlashTimeOn;
        public readonly float FlashTimeTotal;
        public readonly float OnOffTimeS;
        public readonly bool Semaphore;
        public readonly bool DayLight = true;
        public readonly float SemaphoreAnimationTime;
        public bool AreSemaphoresReindexed;

        public SignalTypeData(Viewer viewer, SignalType mstsSignalType)
        {
            Viewer = viewer;

            if (!viewer.SIGCFG.LightTextures.ContainsKey(mstsSignalType.LightTextureName))
            {
                Trace.TraceWarning("Skipped invalid light texture {1} for signal type {0}", mstsSignalType.Name, mstsSignalType.LightTextureName);
#if DEBUG_SIGNAL_SHAPES
                    Type = SignalTypeDataType.Normal;
#endif
                FlashTimeOn = 1;
                FlashTimeTotal = 2;
            }
            else
            {
                var mstsLightTexture = viewer.SIGCFG.LightTextures[mstsSignalType.LightTextureName];
                var defaultMaterial = viewer.MaterialManager.Load("SignalLight", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, mstsLightTexture.TextureFile));
                GlowMaterial = viewer.MaterialManager.Load("SignalLightGlow");
#if DEBUG_SIGNAL_SHAPES
                    Type = (SignalTypeDataType)mstsSignalType.FnType;
#endif
                if (mstsSignalType.Lights != null)
                {
                    // Set up some heuristic glow values from the available data:
                    //   Typical electric light is 3.0/5.0
                    //   Semaphore is 0.0/5.0
                    //   Theatre box is 0.0/0.0
                    var glowDay = 3.0f;
                    var glowNight = 5.0f;

                    if (mstsSignalType.Semaphore)
                        glowDay = 0.0f;
                    if (mstsSignalType.Function == SignalFunction.INFO || mstsSignalType.Function == SignalFunction.SHUNTING) // These are good at identifying theatre boxes.
                        glowDay = glowNight = 0.0f;

                    // use values from signal if defined
                    if (mstsSignalType.DayGlow.HasValue)
                    {
                        glowDay = mstsSignalType.DayGlow.Value;
                    }
                    if (mstsSignalType.NightGlow.HasValue)
                    {
                        glowNight = mstsSignalType.NightGlow.Value;
                    }

                    foreach (var mstsSignalLight in mstsSignalType.Lights)
                    {
                        if (!viewer.SIGCFG.LightsTable.ContainsKey(mstsSignalLight.Name))
                        {
                            Trace.TraceWarning("Skipped invalid light {1} for signal type {0}", mstsSignalType.Name, mstsSignalLight.Name);
                            continue;
                        }
                        var mstsLight = viewer.SIGCFG.LightsTable[mstsSignalLight.Name];
                        var material = defaultMaterial;
                        if (!string.IsNullOrEmpty(mstsSignalLight.LightTextureName))
                        {
                            if (!viewer.SIGCFG.LightTextures.ContainsKey(mstsSignalLight.LightTextureName))
                            {
                                Trace.TraceWarning("Skipped invalid light texture {0} for signal light {1} in signal type {2}", mstsSignalLight.LightTextureName, mstsSignalLight.Name, mstsSignalType.Name);
                            }
                            else
                            {
                                var texture = viewer.SIGCFG.LightTextures[mstsSignalLight.LightTextureName];
                                material = viewer.MaterialManager.Load("SignalLight", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, texture.TextureFile));
                            }
                        }
                        Lights.Add(new SignalLightPrimitive(viewer, new Vector3(-mstsSignalLight.X, mstsSignalLight.Y, mstsSignalLight.Z), mstsSignalLight.Radius, new Color(mstsLight.r, mstsLight.g, mstsLight.b, mstsLight.a), glowDay, glowNight, mstsLightTexture.u0, mstsLightTexture.v0, mstsLightTexture.u1, mstsLightTexture.v1, material));
                        LightsSemaphoreChange.Add(mstsSignalLight.SemaphoreChange);
                    }
                }

                foreach (KeyValuePair<string, SignalDrawState> sdrawstate in mstsSignalType.DrawStates)
                    DrawAspects.Add(sdrawstate.Value.Index, new SignalAspectData(mstsSignalType, sdrawstate.Value));
                FlashTimeOn = mstsSignalType.FlashTimeOn;
                FlashTimeTotal = mstsSignalType.FlashTimeOn + mstsSignalType.FlashTimeOff;
                Semaphore = mstsSignalType.Semaphore;
                SemaphoreAnimationTime = mstsSignalType.SemaphoreInfo;
                DayLight = mstsSignalType.DayLight;
            }

            OnOffTimeS = mstsSignalType.OnOffTimeS;
        }

        public void Mark()
        {
            Viewer.SignalTypeDataManager.Mark(this);
            foreach (var light in Lights)
            {
                light.Mark();
            }
            GlowMaterial?.Mark();
        }
    }

    enum SignalTypeDataType
    {
        Normal,
        Distance,
        Repeater,
        Shunting,
        Info,
    }

    public class SignalAspectData
    {
        public readonly bool[] DrawLights;
        public readonly bool[] FlashLights;
        public float SemaphorePos;

        public SignalAspectData(SignalType mstsSignalType, SignalDrawState drawStateData)
        {
            if (mstsSignalType.Lights != null)
            {
                DrawLights = new bool[mstsSignalType.Lights.Count];
                FlashLights = new bool[mstsSignalType.Lights.Count];
            }
            else
            {
                DrawLights = null;
                FlashLights = null;
            }

            if (drawStateData.DrawLights != null)
            {
                foreach (var drawLight in drawStateData.DrawLights)
                {
                    if (drawLight.LightIndex < 0 || DrawLights == null || drawLight.LightIndex >= DrawLights.Length)
                        Trace.TraceWarning("Skipped extra draw light {0}", drawLight.LightIndex);
                    else
                    {
                        DrawLights[drawLight.LightIndex] = true;
                        FlashLights[drawLight.LightIndex] = drawLight.Flashing;
                    }
                }
            }
            SemaphorePos = drawStateData.SemaphorePos;
        }
    }

    /// <summary>
    /// Tracks state for individual signal head lamps, with smooth lit/unlit transitions.
    /// </summary>
    internal class SignalLightState
    {
        private readonly float onOffTime;
        private float intensity = 0;
        private bool firstUpdate = true;

        public SignalLightState(float onOffTime)
        {
            this.onOffTime = onOffTime;
        }

        public float GetIntensity()
        {
            return intensity;
        }

        public void UpdateIntensity(float target, ElapsedTime et)
        {
            if (firstUpdate || onOffTime == 0)
                intensity = target;
            else if (target > intensity)
                intensity = Math.Min(intensity + et.ClockSeconds / onOffTime, target);
            else if (target < intensity)
                intensity = Math.Max(intensity - et.ClockSeconds / onOffTime, target);
            firstUpdate = false;
        }

        public bool IsIlluminated()
        {
            return intensity > 0;
        }
    }

    public class SignalLightPrimitive : RenderPrimitive
    {
        internal readonly Vector3 Position;
        internal readonly float GlowIntensityDay;
        internal readonly float GlowIntensityNight;
        readonly VertexBuffer VertexBuffer;
        public readonly Material Material;

        public SignalLightPrimitive(Viewer viewer, Vector3 position, float radius, Color color, float glowDay, float glowNight, float u0, float v0, float u1, float v1, Material material)
        {
            Position = position;
            GlowIntensityDay = glowDay;
            GlowIntensityNight = glowNight;

            var verticies = new[] {
				new VertexPositionColorTexture(new Vector3(-radius, +radius, 0), color, new Vector2(u1, v0)),
				new VertexPositionColorTexture(new Vector3(+radius, +radius, 0), color, new Vector2(u0, v0)),
				new VertexPositionColorTexture(new Vector3(-radius, -radius, 0), color, new Vector2(u1, v1)),
				new VertexPositionColorTexture(new Vector3(+radius, -radius, 0), color, new Vector2(u0, v1)),
			};

            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColorTexture), verticies.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(verticies);

            Material = material;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        }

        public void Mark()
        {
            Material.Mark();
        }
    }

    public class SignalLightMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D Texture;

        public SignalLightMaterial(Viewer viewer, string textureName)
            : base(viewer, textureName)
        {
            SceneryShader = Viewer.MaterialManager.SceneryShader;
            Texture = Viewer.TextureManager.Get(textureName, true);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SceneryShader.CurrentTechnique = Viewer.MaterialManager.SceneryShader.Techniques["SignalLight"];
            SceneryShader.ImageTexture = Texture;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var pass in SceneryShader.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    SceneryShader.SignalLightIntensity = (item.ItemData as SignalLightState).GetIntensity();
                    SceneryShader.SetMatrix(item.XNAMatrix);
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            base.Mark();
        }
    }

    public class SignalLightGlowMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D Texture;

        float NightEffect;

        public SignalLightGlowMaterial(Viewer viewer)
            : base(viewer, null)
        {
            SceneryShader = Viewer.MaterialManager.SceneryShader;
            Texture = SharedTextureManager.LoadInternal(Viewer.GraphicsDevice, Path.Combine(Viewer.ContentPath, "SignalLightGlow.png"));
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SceneryShader.CurrentTechnique = Viewer.MaterialManager.SceneryShader.Techniques["SignalLightGlow"];
            SceneryShader.ImageTexture = Texture;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;

            // The following constants define the beginning and the end conditions of
            // the day-night transition. Values refer to the Y postion of LightVector.
            const float startNightTrans = 0.1f;
            const float finishNightTrans = -0.1f;

            var sunDirection = Viewer.Settings.UseMSTSEnv ? Viewer.World.MSTSSky.mstsskysolarDirection : Viewer.World.Sky.SolarDirection;
            NightEffect = 1 - MathHelper.Clamp((sunDirection.Y - finishNightTrans) / (startNightTrans - finishNightTrans), 0, 1);
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var pass in SceneryShader.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    var slp = item.RenderPrimitive as SignalLightPrimitive;
                    SceneryShader.ZBias = MathHelper.Lerp(slp.GlowIntensityDay, slp.GlowIntensityNight, NightEffect);
                    SceneryShader.SignalLightIntensity = (item.ItemData as SignalLightState).GetIntensity();
                    SceneryShader.SetMatrix(item.XNAMatrix);
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            base.Mark();
        }
    }
}
