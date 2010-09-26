/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Diagnostics;

namespace ORTS
{
	public class SignalShape : PoseableShape
	{
		readonly uint UID;
		readonly SignalObject SignalObject;
		readonly List<SignalShapeHead> Heads = new List<SignalShapeHead>();

		public SignalShape(Viewer3D viewer, MSTS.SignalObj mstsSignal, string path, WorldPosition position, ShapeFlags flags)
			: base(viewer, path, position, flags)
		{
			UID = mstsSignal.UID;
			var mstsSignalShape = viewer.Simulator.SIGCFG.SignalShapes[Path.GetFileName(path)];

			for (var i = 0; i < mstsSignal.SignalUnits.Units.Length; i++)
			{
				var signalAndHead = viewer.Simulator.Signals.FindByTrItem(mstsSignal.SignalUnits.Units[i].TrItem);
				if (!signalAndHead.HasValue)
				{
					Trace.TraceWarning("{0} signal {1} unit {2} has invalid TrItem {3}.", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].TrItem);
					continue;
				}
				var mstsSignalSubObj = mstsSignalShape.SignalSubObjs[mstsSignal.SignalUnits.Units[i].SubObj];
				if (mstsSignalSubObj.SignalSubType != 1) // SIGNAL_HEAD
				{
					Trace.TraceWarning("{0} signal {1} unit {2} has invalid SubObj {3}.", Location.ToString(), mstsSignal.UID, i, mstsSignal.SignalUnits.Units[i].SubObj);
					continue;
				}
				SignalObject = signalAndHead.Value.Key;
				try
				{
					Heads.Add(new SignalShapeHead(viewer, this, i, signalAndHead.Value.Value, mstsSignalSubObj));
				}
				catch (InvalidDataException error)
				{
					Trace.WriteLine(error);
				}
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
			base.PrepareFrame(frame, elapsedTime);
		}

		class SignalShapeHead
		{
			static readonly Dictionary<string, SignalTypeData> SignalTypes = new Dictionary<string, SignalTypeData>();

			readonly SignalShape SignalShape;
			readonly int Index;
			readonly SignalHead SignalHead;
			readonly int MatrixIndex;
			readonly SignalTypeData SignalTypeData;
			float CumulativeTime;

			SignalHead.SIGASP LastState;

			public SignalShapeHead(Viewer3D viewer, SignalShape signalShape, int index, SignalHead signalHead, MSTS.SignalShape.SignalSubObj mstsSignalSubObj)
			{
				SignalShape = signalShape;
				Index = index;
				SignalHead = signalHead;
				MatrixIndex = Array.IndexOf(signalShape.SharedShape.MatrixNames, mstsSignalSubObj.node_name.ToUpper());
				if (MatrixIndex == -1)
					throw new InvalidDataException(String.Format("{0} signal {1} unit {2} has invalid sub-object node-name {3}.", signalShape.Location.ToString(), signalShape.UID, index, mstsSignalSubObj.node_name));

				var mstsSignalType = viewer.Simulator.SIGCFG.SignalTypes[mstsSignalSubObj.SigSubSType];
				if (SignalTypes.ContainsKey(mstsSignalType.typeName))
					SignalTypeData = SignalTypes[mstsSignalType.typeName];
				else
					SignalTypeData = SignalTypes[mstsSignalType.typeName] = new SignalTypeData(viewer, mstsSignalType);
			}

			public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, Matrix xnaTileTranslation)
			{
				LastState = SignalHead.state;
				CumulativeTime += elapsedTime.ClockSeconds;

				// Uhoh, aspect isn't in our data. We'll display nothing for now.
				if (!SignalTypeData.Aspects.ContainsKey(LastState))
					return;

				for (var i = 0; i < SignalTypeData.Lights.Count; i++)
				{
					if (!SignalTypeData.Aspects[LastState].DrawLights[i])
						continue;
					if (SignalTypeData.Aspects[LastState].FlashLights[i] && (CumulativeTime % 2 < 1))
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
			public readonly List<SignalLightMesh> Lights = new List<SignalLightMesh>();
			public readonly Dictionary<SignalHead.SIGASP, SignalAspectData> Aspects = new Dictionary<SignalHead.SIGASP, SignalAspectData>();

			public SignalTypeData(Viewer3D viewer, MSTS.SignalType mstsSignalType)
			{
				var mstsLightTexture = viewer.Simulator.SIGCFG.LightTextures[mstsSignalType.SignalLightTex];
				Material = Materials.Load(viewer.RenderProcess, "SignalLightMaterial", Helpers.GetTextureFolder(viewer, 0) + @"\" + mstsLightTexture.TextureFile);
				if (mstsSignalType.SignalLights != null)
				{
					foreach (var mstsSignalLight in mstsSignalType.SignalLights)
					{
						var mstsLight = viewer.Simulator.SIGCFG.LightsTable[mstsSignalLight.Name];
						Lights.Add(new SignalLightMesh(viewer, new Vector3(mstsSignalLight.x, mstsSignalLight.y, mstsSignalLight.z), mstsSignalLight.radius, new Color(mstsLight.r, mstsLight.g, mstsLight.b, mstsLight.a), mstsLightTexture.u0, mstsLightTexture.v0, mstsLightTexture.u1, mstsLightTexture.v1));
					}
					// Only load aspects if we've got lights. Not much point otherwise.
					if (mstsSignalType.SignalAspects != null)
					{
						foreach (var mstsSignalAspect in mstsSignalType.SignalAspects)
							Aspects.Add(mstsSignalAspect.signalAspect, new SignalAspectData(mstsSignalType, mstsSignalAspect));
					}
				}
			}
		}

		class SignalAspectData
		{
			public bool[] DrawLights;
			public bool[] FlashLights;

			public SignalAspectData(MSTS.SignalType mstsSignalType, MSTS.SignalAspect mstsSignalAspect)
			{
				DrawLights = new bool[mstsSignalType.SignalLights.Length];
				FlashLights = new bool[mstsSignalType.SignalLights.Length];
				var drawState = mstsSignalType.SignalDrawStates[mstsSignalAspect.drawState];
				if (drawState.DrawLights != null)
				{
					foreach (var drawLight in drawState.DrawLights)
					{
						DrawLights[drawLight.DrawLight] = true;
						FlashLights[drawLight.DrawLight] = drawLight.Flashing;
					}
				}
			}
		}
	}

	public class SignalLightMesh : RenderPrimitive
	{
		const int CirclePoints = 32;

		readonly VertexDeclaration VertexDeclaration;
		readonly VertexBuffer VertexBuffer;

		public SignalLightMesh(Viewer3D viewer, Vector3 position, float radius, Color color, float u0, float v0, float u1, float v1)
		{
			var uvRadius = new Vector2(-(u1 - u0) / 2, -(v1 - v0) / 2);
			var uvCenter = new Vector2(u0 - uvRadius.X, v0 - uvRadius.Y);
			var verticies = new VertexPositionColorTexture[CirclePoints + 2];
			verticies[0] = new VertexPositionColorTexture(position, color, uvCenter);
			for (var i = 1; i <= CirclePoints; i++)
			{
				var x = (float)Math.Sin((float)i / CirclePoints * Math.PI * 2);
				var y = (float)Math.Cos((float)i / CirclePoints * Math.PI * 2);
				var pos = new Vector3(position.X - radius * x, position.Y - radius * y, position.Z);
				verticies[i] = new VertexPositionColorTexture(pos, color, new Vector2(uvCenter.X + uvRadius.X * x, uvCenter.Y - uvRadius.Y * y));
			}
			verticies[CirclePoints + 1] = verticies[1];

			VertexDeclaration = new VertexDeclaration(viewer.GraphicsDevice, VertexPositionColorTexture.VertexElements);
			VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, VertexPositionColorTexture.SizeInBytes * verticies.Length, BufferUsage.WriteOnly);
			VertexBuffer.SetData(verticies);
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.VertexDeclaration = VertexDeclaration;
			graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionColorTexture.SizeInBytes);
			graphicsDevice.DrawPrimitives(PrimitiveType.TriangleFan, 0, CirclePoints);
		}
	}

	public class SignalLightMaterial : Material
	{
		readonly SceneryShader SceneryShader;
		readonly Texture2D Texture;

		public SignalLightMaterial(RenderProcess renderProcess, string textureName)
		{
			SceneryShader = Materials.SceneryShader;
			Texture = SharedTextureManager.Get(renderProcess.GraphicsDevice, textureName);
		}

		public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
		{
			SceneryShader.CurrentTechnique = Materials.SceneryShader.Techniques["SignalLight"];
			SceneryShader.ImageMap_Tex = Texture;
			graphicsDevice.RenderState.AlphaBlendEnable = true;
			graphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
			graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
			graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
			graphicsDevice.RenderState.SeparateAlphaBlendEnabled = true;
			graphicsDevice.RenderState.AlphaSourceBlend = Blend.Zero;
			graphicsDevice.RenderState.AlphaDestinationBlend = Blend.One;
			graphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
		}

		public override void Render(GraphicsDevice graphicsDevice, List<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			Matrix viewProj = XNAViewMatrix * XNAProjectionMatrix;

			// With the GPU configured, now we can draw the primitive
			SceneryShader.Begin();
			foreach (EffectPass pass in SceneryShader.CurrentTechnique.Passes)
			{
				pass.Begin();

				foreach (RenderItem item in renderItems)
				{
					SceneryShader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref viewProj);
					SceneryShader.CommitChanges();
					item.RenderPrimitive.Draw(graphicsDevice);
				}

				pass.End();
			}
			SceneryShader.End();
		}

		public override void ResetState(GraphicsDevice graphicsDevice)
		{
			graphicsDevice.RenderState.AlphaBlendEnable = false;
			graphicsDevice.RenderState.SourceBlend = Blend.One;
			graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
			graphicsDevice.RenderState.SeparateAlphaBlendEnabled = false;
			graphicsDevice.RenderState.AlphaSourceBlend = Blend.One;
		}
	}
}
