// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

// Prints out lots of diagnostic information about the construction of signals from shape data and their state changes.
//#define DEBUG_SIGNAL_SHAPES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
    public class SignalShape : PoseableShape
    {
        readonly uint UID;
        readonly SignalObject SignalObject;
        readonly bool[] SubObjVisible;
        readonly List<SignalShapeHead> Heads = new List<SignalShapeHead>();

        public SignalShape(Viewer3D viewer, MSTS.SignalObj mstsSignal, string path, WorldPosition position, ShapeFlags flags)
            : base(viewer, path, position, flags)
        {
#if DEBUG_SIGNAL_SHAPES
			Console.WriteLine("{0} signal {1}:", Location.ToString(), mstsSignal.UID);
#endif
            UID = mstsSignal.UID;
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

            // All sub-objects except the first are hidden by default. For each sub-object beyond the first, look up
            // its name in the hierarchy and use the visibility of that matrix. Note: parent matricies in the
            // hierarchy are not considered.
            SubObjVisible = new bool[SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length];
            SubObjVisible[0] = true;
            for (var i = 1; i < SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length; i++)
                SubObjVisible[i] = visibleMatrixNames[SharedShape.LodControls[0].DistanceLevels[0].SubObjects[i].ShapePrimitives[0].HierarchyIndex];

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
                    Trace.TraceWarning("{0} signal {1} unit {2} has invalid TrItem {3}.", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].TrItem);
                    continue;
                }
                // Get the signal sub-object for this unit (head).
                var mstsSignalSubObj = mstsSignalShape.SignalSubObjs[mstsSignal.SignalUnits.Units[i].SubObj];
                if (mstsSignalSubObj.SignalSubType != 1) // SIGNAL_HEAD
                {
                    Trace.TraceWarning("{0} signal {1} unit {2} has invalid SubObj {3}.", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].SubObj);
                    continue;
                }
                SignalObject = signalAndHead.Value.Key;
                var mstsSignalItem = (MSTS.SignalItem)(viewer.Simulator.TDB.TrackDB.TrItemTable[mstsSignal.SignalUnits.Units[i].TrItem]);
                try
                {
                    // Go create the shape head.
                    Heads.Add(new SignalShapeHead(viewer, this, i, signalAndHead.Value.Value, mstsSignalItem, mstsSignalSubObj));
                }
                catch (InvalidDataException error)
                {
                    Trace.WriteLine(error);
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
            var mstsLocation = Location.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
            var xnaTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            Matrix.Multiply(ref Location.XNAMatrix, ref xnaTileTranslation, out xnaTileTranslation);

            foreach (var head in Heads)
                head.PrepareFrame(frame, elapsedTime, xnaTileTranslation);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, SubObjVisible, Flags);
        }

        internal override void Mark()
        {
            foreach (var head in Heads)
                head.Mark();
            base.Mark();
        }

        class SignalShapeHead
        {
            static readonly Dictionary<string, SignalTypeData> SignalTypes = new Dictionary<string, SignalTypeData>();

#if DEBUG_SIGNAL_SHAPES
			readonly Viewer3D Viewer;
#endif
            readonly SignalShape SignalShape;
            readonly int Index;
            readonly SignalHead SignalHead;
            readonly int MatrixIndex;
            readonly SignalTypeData SignalTypeData;
            float CumulativeTime;
            float SemaphorePos;
            float SemaphoreTarget;
            float SemaphoreSpeed;
            float SemaphoreInfo;
            int DisplayState = -1;

            public SignalShapeHead(Viewer3D viewer, SignalShape signalShape, int index, SignalHead signalHead,
                        MSTS.SignalItem mstsSignalItem, MSTS.SignalShape.SignalSubObj mstsSignalSubObj)
            {
#if DEBUG_SIGNAL_SHAPES
				Viewer = viewer;
#endif
                SignalShape = signalShape;
                Index = index;
                SignalHead = signalHead;
                MatrixIndex = signalShape.SharedShape.MatrixNames.IndexOf(mstsSignalSubObj.MatrixName);
                if (MatrixIndex == -1)
                    throw new InvalidDataException(String.Format("{0} signal {1} unit {2} has invalid sub-object node-name {3}.", signalShape.Location, signalShape.UID, index, mstsSignalSubObj.MatrixName));

                if (!viewer.SIGCFG.SignalTypes.ContainsKey(mstsSignalSubObj.SignalSubSignalType))
                    throw new InvalidDataException(String.Format("{0} signal {1} unit {2} has invalid SigSubSType {3}.", signalShape.Location, signalShape.UID, index, mstsSignalSubObj.SignalSubSignalType));
                var mstsSignalType = viewer.SIGCFG.SignalTypes[mstsSignalSubObj.SignalSubSignalType];

                SemaphoreInfo = mstsSignalType.SemaphoreInfo;

                if (SignalTypes.ContainsKey(mstsSignalType.Name))
                    SignalTypeData = SignalTypes[mstsSignalType.Name];
                else
                    SignalTypeData = SignalTypes[mstsSignalType.Name] = new SignalTypeData(viewer, mstsSignalType);

#if DEBUG_SIGNAL_SHAPES
				Console.Write("  HEAD type={0,-8} lights={1,-2}", SignalTypeData.Type, SignalTypeData.Lights.Count);
#endif
            }

            public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, Matrix xnaTileTranslation)
            {
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
                        SemaphoreSpeed = SemaphoreTarget > SemaphorePos ? +1 : -1;
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
                    SignalShape.AnimateMatrix(MatrixIndex, 0);
                }

                for (var i = 0; i < SignalTypeData.Lights.Count; i++)
                {
                    if (SemaphorePos != SemaphoreTarget && SignalTypeData.LightsSemaphoreChange[i])
                        continue;
                    if (!SignalTypeData.DrawAspects[DisplayState].DrawLights[i])
                        continue;
                    if (SignalTypeData.DrawAspects[DisplayState].FlashLights[i] && (CumulativeTime > SignalTypeData.FlashTimeOn))
                        continue;

                    var xnaMatrix = Matrix.Identity;
                    Matrix.Multiply(ref xnaMatrix, ref SignalShape.XNAMatrices[MatrixIndex], out xnaMatrix);
                    Matrix.Multiply(ref xnaMatrix, ref xnaTileTranslation, out xnaMatrix);

                    frame.AddPrimitive(SignalTypeData.Material, SignalTypeData.Lights[i], RenderPrimitiveGroup.Lights, ref xnaMatrix);
                }

                if (SignalTypeData.Semaphore)
                {
                    // Now we update and re-animate the semaphore arm.
                    // Set arm to final position immediately if semaphoreinfo = 0

                    if (SemaphoreInfo == 0)
                    {
                        SemaphorePos = SemaphoreTarget;
                        SemaphoreSpeed = 0;
                    }
                    else
                    {
                        SemaphorePos += SemaphoreSpeed * elapsedTime.ClockSeconds;
                        if (SemaphorePos * Math.Sign(SemaphoreSpeed) > SemaphoreTarget * Math.Sign(SemaphoreSpeed))
                        {
                            SemaphorePos = SemaphoreTarget;
                            SemaphoreSpeed = 0;
                        }
                    }
                    SignalShape.AnimateMatrix(MatrixIndex, SemaphorePos);
                }
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                SignalTypeData.Material.Mark();
            }
        }

        class SignalTypeData
        {
            public readonly Material Material;
            public readonly SignalTypeDataType Type;
            public readonly List<SignalLightMesh> Lights = new List<SignalLightMesh>();
            public readonly List<bool> LightsSemaphoreChange = new List<bool>();
            public readonly Dictionary<int, SignalAspectData> DrawAspects = new Dictionary<int, SignalAspectData>();
            public readonly float FlashTimeOn;
            public readonly float FlashTimeTotal;
            public readonly bool Semaphore;

            public SignalTypeData(Viewer3D viewer, MSTS.SignalType mstsSignalType)
            {
                if (!viewer.SIGCFG.LightTextures.ContainsKey(mstsSignalType.LightTextureName))
                {
                    Trace.TraceWarning("Skipped invalid light texture {1} for signal type {0}", mstsSignalType.Name, mstsSignalType.LightTextureName);
                    Material = viewer.MaterialManager.Load("missing-signal-light");
                    Type = SignalTypeDataType.Normal;
                    FlashTimeOn = 1;
                    FlashTimeTotal = 2;
                }
                else
                {
                    var mstsLightTexture = viewer.SIGCFG.LightTextures[mstsSignalType.LightTextureName];
                    Material = viewer.MaterialManager.Load("SignalLight", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, mstsLightTexture.TextureFile));
                    Type = (SignalTypeDataType)mstsSignalType.FnType;
                    if (mstsSignalType.Lights != null)
                    {
                        foreach (var mstsSignalLight in mstsSignalType.Lights)
                        {
                            if (!viewer.SIGCFG.LightsTable.ContainsKey(mstsSignalLight.Name))
                            {
                                Trace.TraceWarning("Skipped invalid light {1} for signal type {0}", mstsSignalType.Name, mstsSignalLight.Name);
                                continue;
                            }
                            var mstsLight = viewer.SIGCFG.LightsTable[mstsSignalLight.Name];
                            Lights.Add(new SignalLightMesh(viewer, new Vector3(-mstsSignalLight.X, mstsSignalLight.Y, mstsSignalLight.Z), mstsSignalLight.Radius, new Color(mstsLight.r, mstsLight.g, mstsLight.b, mstsLight.a), mstsLightTexture.u0, mstsLightTexture.v0, mstsLightTexture.u1, mstsLightTexture.v1));
                            LightsSemaphoreChange.Add(mstsSignalLight.SemaphoreChange);
                        }
                    }

                    foreach (KeyValuePair<string, MSTS.SignalDrawState> sdrawstate in mstsSignalType.DrawStates)
                        DrawAspects.Add(sdrawstate.Value.Index, new SignalAspectData(mstsSignalType, sdrawstate.Value));
                    FlashTimeOn = mstsSignalType.FlashTimeOn;
                    FlashTimeTotal = mstsSignalType.FlashTimeOn + mstsSignalType.FlashTimeOff;
                    Semaphore = mstsSignalType.Semaphore;
                }
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

        class SignalAspectData
        {
            public readonly bool[] DrawLights;
            public readonly bool[] FlashLights;
            public readonly float SemaphorePos;

            public SignalAspectData(MSTS.SignalType mstsSignalType, MSTS.SignalDrawState drawStateData)
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
                        if (drawLight.LightIndex < 0 || drawLight.LightIndex >= DrawLights.Length)
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
    }

    public class SignalLightMesh : RenderPrimitive
    {
        readonly VertexDeclaration VertexDeclaration;
        readonly VertexBuffer VertexBuffer;

        public SignalLightMesh(Viewer3D viewer, Vector3 position, float radius, Color color, float u0, float v0, float u1, float v1)
        {
            var verticies = new[] {
				new VertexPositionColorTexture(new Vector3(position.X - radius, position.Y + radius, position.Z), color, new Vector2(u1, v0)),
				new VertexPositionColorTexture(new Vector3(position.X + radius, position.Y + radius, position.Z), color, new Vector2(u0, v0)),
				new VertexPositionColorTexture(new Vector3(position.X + radius, position.Y - radius, position.Z), color, new Vector2(u0, v1)),
				new VertexPositionColorTexture(new Vector3(position.X - radius, position.Y - radius, position.Z), color, new Vector2(u1, v1)),
			};

            VertexDeclaration = new VertexDeclaration(viewer.GraphicsDevice, VertexPositionColorTexture.VertexElements);
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, VertexPositionColorTexture.SizeInBytes * verticies.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(verticies);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionColorTexture.SizeInBytes);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
        }
    }

    public class SignalLightMaterial : Material
    {
        readonly SceneryShader SceneryShader;
        readonly Texture2D Texture;

        public SignalLightMaterial(Viewer3D viewer, string textureName)
            : base(viewer, textureName)
        {
            SceneryShader = Viewer.MaterialManager.SceneryShader;
            Texture = Viewer.TextureManager.Get(textureName);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SceneryShader.CurrentTechnique = Viewer.MaterialManager.SceneryShader.Techniques["SignalLight"];
            SceneryShader.ImageTexture = Texture;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

            // With the GPU configured, now we can draw the primitive
            SceneryShader.SetViewMatrix(ref XNAViewMatrix);
            SceneryShader.Begin();
            foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();

                foreach (RenderItem item in renderItems)
                {
                    SceneryShader.SetMatrix(ref item.XNAMatrix, ref viewProj);
                    SceneryShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }

                pass.End();
            }
            SceneryShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            base.Mark();
        }
    }
}
