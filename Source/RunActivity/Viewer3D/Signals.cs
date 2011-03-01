/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

//#define DEBUG_SIGNAL_SHAPES

// This is a temporary implementation of signal feathers for testing.
//#define SIGNAL_SHAPES_FEATHERS

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
		readonly List<SignalShapeHead> Heads = new List<SignalShapeHead>();
#if SIGNAL_SHAPES_FEATHERS
		bool InfoHeadFound;
#endif

		public SignalShape(Viewer3D viewer, MSTS.SignalObj mstsSignal, string path, WorldPosition position, ShapeFlags flags)
			: base(viewer, path, position, flags)
		{
#if DEBUG_SIGNAL_SHAPES
			Console.WriteLine(String.Format("{0} signal {1}:", Location.ToString(), mstsSignal.UID));
#endif

			UID = mstsSignal.UID;
			var signalShape = Path.GetFileName(path).ToUpper();
			if (!viewer.SIGCFG.SignalShapes.ContainsKey(signalShape))
			{
				Trace.TraceError("{0} signal {1} has invalid shape {2}.", Location.ToString(), mstsSignal.UID, signalShape);
				return;
			}
            var mstsSignalShape = viewer.SIGCFG.SignalShapes[signalShape];

			// Move the optional signal components way off into the sky. We're
			// re-position all the ones that are visible on this signal later.
			// For some reason many optional components aren't in the shape,
			// so we need to handle that.
			foreach (var mstsSignalSubObj in mstsSignalShape.SignalSubObjs)
				if (mstsSignalSubObj.Optional && !mstsSignalSubObj.Default && SharedShape.MatrixNames.Contains(mstsSignalSubObj.MatrixName))
					XNAMatrices[SharedShape.MatrixNames.IndexOf(mstsSignalSubObj.MatrixName)].M42 += 10000;

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
				// Ensure this head is displayed if it is optional.
				if (mstsSignalSubObj.Optional && !mstsSignalSubObj.Default && SharedShape.MatrixNames.Contains(mstsSignalSubObj.MatrixName))
					XNAMatrices[SharedShape.MatrixNames.IndexOf(mstsSignalSubObj.MatrixName)].M42 -= 10000;
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

#if SIGNAL_SHAPES_FEATHERS
			InfoHeadFound = false;
#endif
			foreach (var head in Heads)
				head.PrepareFrame(frame, elapsedTime, xnaTileTranslation);

			base.PrepareFrame(frame, elapsedTime);
		}

		class SignalShapeHead
		{
			static readonly Dictionary<string, SignalTypeData> SignalTypes = new Dictionary<string, SignalTypeData>();

#if DEBUG_SIGNAL_SHAPES || SIGNAL_SHAPES_FEATHERS
			readonly Viewer3D Viewer;
#endif
			readonly SignalShape SignalShape;
			readonly int Index;
			readonly SignalHead SignalHead;
			readonly int MatrixIndex;
			readonly SignalTypeData SignalTypeData;
#if SIGNAL_SHAPES_FEATHERS
			readonly uint JunctionTrackNode;
			readonly uint JunctionLinkRoute;
#endif
			float CumulativeTime;

			SignalHead.SIGASP LastState = SignalHead.SIGASP.UNKNOWN;
			SignalHead.SIGASP DisplayState = SignalHead.SIGASP.UNKNOWN;

			public SignalShapeHead(Viewer3D viewer, SignalShape signalShape, int index, SignalHead signalHead, MSTS.SignalItem mstsSignalItem, MSTS.SignalShape.SignalSubObj mstsSignalSubObj)
			{
#if DEBUG_SIGNAL_SHAPES || SIGNAL_SHAPES_FEATHERS
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
				if (SignalTypes.ContainsKey(mstsSignalType.Name))
					SignalTypeData = SignalTypes[mstsSignalType.Name];
				else
					SignalTypeData = SignalTypes[mstsSignalType.Name] = new SignalTypeData(viewer, mstsSignalType);

#if DEBUG_SIGNAL_SHAPES
				Console.Write("  HEAD type={0,-8} lights={1,-2} aspects={2,-2}", SignalTypeData.Type, SignalTypeData.Lights.Count, SignalTypeData.Aspects.Count);
#endif

				if (SignalTypeData.Type == SignalTypeDataType.Info)
				{
					if (mstsSignalItem.TrSignalDirs == null)
					{
						Trace.TraceError("{0} signal {1} unit {2} has no TrSignalDirs.", signalShape.Location, signalShape.UID, index);
						return;
					}
					if (mstsSignalItem.TrSignalDirs.Length != 1)
					{
						Trace.TraceError("{0} signal {1} unit {2} has {3} TrSignalDirs; expected 1.", signalShape.Location, signalShape.UID, index, mstsSignalItem.TrSignalDirs.Length);
						return;
					}
#if DEBUG_SIGNAL_SHAPES
					Console.Write("  LINK node={0,-5} sd1={2,-1} path={1,-1} sd3={3,-1}", mstsSignalItem.TrSignalDirs[0].TrackNode, mstsSignalItem.TrSignalDirs[0].linkLRPath, mstsSignalItem.TrSignalDirs[0].sd1, mstsSignalItem.TrSignalDirs[0].sd3);
#endif
#if SIGNAL_SHAPES_FEATHERS
					JunctionTrackNode = mstsSignalItem.TrSignalDirs[0].TrackNode;
					JunctionLinkRoute = mstsSignalItem.TrSignalDirs[0].linkLRPath;
#endif
				}
			}

			public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, Matrix xnaTileTranslation)
			{
				if (LastState != SignalHead.state)
				{
#if DEBUG_SIGNAL_SHAPES
					Console.WriteLine(String.Format("{5} {0} signal {1} unit {2} state: {3} --> {4}", SignalShape.Location, SignalShape.UID, Index, LastState, SignalHead.state, InfoDisplay.FormattedTime(Viewer.Simulator.ClockTime)));
#endif
					LastState = SignalHead.state;
					DisplayState = LastState;
				}
				CumulativeTime += elapsedTime.ClockSeconds;
				while (CumulativeTime > SignalTypeData.FlashTimeTotal)
					CumulativeTime -= SignalTypeData.FlashTimeTotal;

#if SIGNAL_SHAPES_FEATHERS
				if (SignalTypeData.Type == SignalTypeDataType.Info)
				{
					if (JunctionTrackNode != 0)
					{
						// Use CLEAR_1 or STOP depending on selected route.
						var selectedRoute = Viewer.Simulator.TDB.TrackDB.TrackNodes[JunctionTrackNode].TrJunctionNode.SelectedRoute;
						DisplayState = !SignalShape.InfoHeadFound && selectedRoute == JunctionLinkRoute ? SignalHead.SIGASP.CLEAR_1 : SignalHead.SIGASP.STOP;
						SignalShape.InfoHeadFound |= selectedRoute == JunctionLinkRoute;
					}
				}
#endif

				if ((DisplayState == SignalHead.SIGASP.UNKNOWN) || !SignalTypeData.Aspects.ContainsKey(DisplayState))
					return;

				for (var i = 0; i < SignalTypeData.Lights.Count; i++)
				{
					if (!SignalTypeData.Aspects[DisplayState].DrawLights[i])
						continue;
					if (SignalTypeData.Aspects[DisplayState].FlashLights[i] && (CumulativeTime > SignalTypeData.FlashTimeOn))
						continue;

					var xnaMatrix = Matrix.Identity;
					Matrix.Multiply(ref xnaMatrix, ref SignalShape.XNAMatrices[MatrixIndex], out xnaMatrix);
					Matrix.Multiply(ref xnaMatrix, ref xnaTileTranslation, out xnaMatrix);

					frame.AddPrimitive(SignalTypeData.Material, SignalTypeData.Lights[i], RenderPrimitiveGroup.Lights, ref xnaMatrix);
				}
			}
		}

		class SignalTypeData
		{
			public readonly Material Material;
			public readonly SignalTypeDataType Type;
			public readonly List<SignalLightMesh> Lights = new List<SignalLightMesh>();
			public readonly Dictionary<SignalHead.SIGASP, SignalAspectData> Aspects = new Dictionary<SignalHead.SIGASP, SignalAspectData>();
			public readonly float FlashTimeOn;
			public readonly float FlashTimeTotal;

			public SignalTypeData(Viewer3D viewer, MSTS.SignalType mstsSignalType)
			{
                if (!viewer.SIGCFG.LightTextures.ContainsKey(mstsSignalType.LightTextureName))
				{
					Trace.TraceError("Signal type {0} has invalid light texture {1}.", mstsSignalType.Name, mstsSignalType.LightTextureName);
					Material = Materials.YellowMaterial;
					Type = SignalTypeDataType.Normal;
					FlashTimeOn = 1;
					FlashTimeTotal = 2;
				}
				else
				{
                    var mstsLightTexture = viewer.SIGCFG.LightTextures[mstsSignalType.LightTextureName];
					Material = Materials.Load(viewer.RenderProcess, "SignalLightMaterial", Helpers.GetTextureFolder(viewer, 0) + @"\" + mstsLightTexture.TextureFile);
					Type = (SignalTypeDataType)mstsSignalType.FnType;
					if (mstsSignalType.Lights != null)
					{
						foreach (var mstsSignalLight in mstsSignalType.Lights)
						{
                            var mstsLight = viewer.SIGCFG.LightsTable[mstsSignalLight.Name];
							Lights.Add(new SignalLightMesh(viewer, new Vector3(mstsSignalLight.X, mstsSignalLight.Y, mstsSignalLight.Z), mstsSignalLight.Radius, new Color(mstsLight.r, mstsLight.g, mstsLight.b, mstsLight.a), mstsLightTexture.u0, mstsLightTexture.v0, mstsLightTexture.u1, mstsLightTexture.v1));
						}
						// Only load aspects if we've got lights. Not much point otherwise.
						if (mstsSignalType.Aspects != null)
						{
							foreach (var mstsSignalAspect in mstsSignalType.Aspects)
								Aspects.Add(mstsSignalAspect.Aspect, new SignalAspectData(mstsSignalType, mstsSignalAspect.DrawStateName));
						}
					}
#if SIGNAL_SHAPES_FEATHERS
				// Info = feather/branch/etc. lights, linked to a junction.
				if (Type == SignalTypeDataType.Info)
				{
					if (mstsSignalType.SignalDrawStates.Length != 2)
					{
						Trace.TraceError("Signal type {0} has {1} draw states; expected 2.", mstsSignalType.typeName, mstsSignalType.SignalDrawStates.Length);
						return;
					}
					Aspects.Add(SignalHead.SIGASP.STOP, new SignalAspectData(mstsSignalType, 0));
					Aspects.Add(SignalHead.SIGASP.CLEAR_1, new SignalAspectData(mstsSignalType, 1));
				}
#endif
					FlashTimeOn = mstsSignalType.FlashTimeOn;
					FlashTimeTotal = mstsSignalType.FlashTimeOn + mstsSignalType.FlashTimeOff;
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

			public SignalAspectData(MSTS.SignalType mstsSignalType, string drawState)
			{
				DrawLights = new bool[mstsSignalType.Lights.Count];
				FlashLights = new bool[mstsSignalType.Lights.Count];
				var drawStateData = mstsSignalType.DrawStates[drawState];
				if (drawStateData.DrawLights != null)
				{
					foreach (var drawLight in drawStateData.DrawLights)
					{
						DrawLights[drawLight.LightIndex] = true;
						FlashLights[drawLight.LightIndex] = drawLight.Flashing;
					}
				}
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
				new VertexPositionColorTexture(new Vector3(position.X - radius, position.Y + radius, position.Z), color, new Vector2(u0, v0)),
				new VertexPositionColorTexture(new Vector3(position.X + radius, position.Y + radius, position.Z), color, new Vector2(u1, v0)),
				new VertexPositionColorTexture(new Vector3(position.X + radius, position.Y - radius, position.Z), color, new Vector2(u1, v1)),
				new VertexPositionColorTexture(new Vector3(position.X - radius, position.Y - radius, position.Z), color, new Vector2(u0, v1)),
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
		readonly RenderProcess RenderProcess;
		readonly SceneryShader SceneryShader;
		readonly Texture2D Texture;

		public SignalLightMaterial(RenderProcess renderProcess, string textureName)
			: base(textureName)
		{
			RenderProcess = renderProcess;
			SceneryShader = Materials.SceneryShader;
			Texture = SharedTextureManager.Get(renderProcess.GraphicsDevice, textureName);
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques["SignalLight"];
			SceneryShader.ImageMap_Tex = Texture;

            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = true;
			rs.DestinationBlend = Blend.InverseSourceAlpha;
			rs.SeparateAlphaBlendEnabled = true;
			rs.SourceBlend = Blend.SourceAlpha;
		}

		public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

			// With the GPU configured, now we can draw the primitive
			SceneryShader.Begin();
			foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
			{
				pass.Begin();

				foreach (RenderItem item in renderItems)
				{
					SceneryShader.SetMatrix(ref item.XNAMatrix, ref XNAViewMatrix, ref viewProj);
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
			rs.SeparateAlphaBlendEnabled = false;
			rs.SourceBlend = Blend.One;
		}
	}
}
