// Define this to check every material is resetting the RenderState correctly.
//#define DEBUG_RENDER_STATE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
	public enum RenderPrimitiveSequence
	{
        Shadows,
        CabOpaque,
        WorldOpaque,
        WorldBlended,
		Lights, // TODO: May not be needed once alpha sorting works.
		Precipitation, // TODO: May not be needed once alpha sorting works.
        CabBlended,
        TextOverlayOpaque,
		TextOverlayBlended,
		// This value must be last.
		Sentinel
	}

    public enum RenderPrimitiveGroup
    {
        Shadows,
        Cab,
        World,
		Lights, // TODO: May not be needed once alpha sorting works.
		Precipitation, // TODO: May not be needed once alpha sorting works.
        Overlay
    }

    public abstract class RenderPrimitive
    {
		public static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
			RenderPrimitiveSequence.Shadows,
			RenderPrimitiveSequence.CabBlended,
			RenderPrimitiveSequence.WorldBlended,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
			RenderPrimitiveSequence.TextOverlayBlended,
		};
		public static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
			RenderPrimitiveSequence.Shadows,
			RenderPrimitiveSequence.CabOpaque,
			RenderPrimitiveSequence.WorldOpaque,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
			RenderPrimitiveSequence.TextOverlayOpaque,
		};
		
		public float ZBias = 0f;

        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public abstract void Draw(GraphicsDevice graphicsDevice);
    }

    public struct RenderItem
    {
        public readonly Material Material;
        public readonly RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
		public readonly ShapeFlags Flags;

		public RenderItem(Material material, RenderPrimitive renderPrimitive, Matrix xnaMatrix, ShapeFlags flags)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
        }

		public class Comparer : IComparer<RenderItem>
		{
			readonly Vector3 XNAViewerPos;

			public Comparer(Vector3 viewerPos)
			{
				XNAViewerPos = viewerPos;
				XNAViewerPos.Z *= -1;
			}

			#region IComparer<RenderItem> Members

			public int Compare(RenderItem x, RenderItem y)
			{
				var xd = (x.XNAMatrix.Translation - XNAViewerPos).Length();
				var yd = (y.XNAMatrix.Translation - XNAViewerPos).Length();
				return Math.Sign(xd - yd);
			}

			#endregion
		}
    }

    public class RenderFrame
    {
		const int ShadowMapSunDistance = 1000; // distance from shadow map center to put camera
		const int ShadowMapViewMin = 256; // minimum width/height of shadow map projection
		const int ShadowMapViewMax = 2048; // maximum width/height of shadow map projection
		const int ShadowMapViewStep = 16; // step size for shadow view to stop it fluctuating too much
		const int ShadowMapTexelSize = 4; // number of screen pixel to scale 1 shadow map texel to
		const int ShadowMapSize = 4096; // shadow map texture width/height
		const SurfaceFormat ShadowMapFormat = SurfaceFormat.Rg32; // shadow map texture format
		const float ShadowMapViewNear = 0f; // near plane for shadow map camera
		const float ShadowMapViewFar = 2000f; // far plane for shadow map camera

		static readonly Material DummyBlendedMaterial = new EmptyMaterial();

		readonly RenderProcess RenderProcess;
        readonly Dictionary<Material, List<RenderItem>>[] RenderItems = new Dictionary<Material, List<RenderItem>>[(int)RenderPrimitiveSequence.Sentinel];

        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;

        public RenderFrame(RenderProcess owner)
        {
			RenderProcess = owner;

            for (int i = 0; i < RenderItems.Length; i++)
            {
                RenderItems[i] = new Dictionary<Material, List<RenderItem>>();
            }
        }

        public void Clear() 
        {
            for (int i = 0; i < RenderItems.Length; i++)
            {
                foreach (Material mat in RenderItems[i].Keys)
                {
                    RenderItems[i][mat].Clear();
                }
            }
        }

        public void SetCamera(ref Matrix xnaViewMatrix, ref Matrix xnaProjectionMatrix)
        {
            XNAViewMatrix = xnaViewMatrix;
            XNAProjectionMatrix = xnaProjectionMatrix;
        }

		public void PrepareFrame(ElapsedTime elapsedTime)
		{
			var sunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
			var cameraLocation = new Vector3(RenderProcess.Viewer.Camera.Location.X, RenderProcess.Viewer.Camera.Location.Y, -RenderProcess.Viewer.Camera.Location.Z);
			var terrainAltitude = RenderProcess.Viewer.Tiles.GetElevation(RenderProcess.Viewer.Camera.TileX, RenderProcess.Viewer.Camera.TileZ, (cameraLocation.X + 1024) / 8, (cameraLocation.Z + 1024) / 8);

			// Project the center-bottom of the screen onto the world.
			var reverseCameraProjection = Matrix.Invert(RenderProcess.Viewer.Camera.XNAView * RenderProcess.Viewer.Camera.XNAProjection);
			var cameraBottomVector = Vector3.Transform(-Vector3.UnitY, reverseCameraProjection) / (-reverseCameraProjection.M24 + reverseCameraProjection.M44);
			var cameraBottomRay = new Ray(cameraLocation, cameraBottomVector - cameraLocation);

			// Find the place where the center-bottom of the screen intersects with the terrain.
			var terrain = new Plane(-Vector3.UnitY, terrainAltitude);
			var terrainDistance = cameraBottomRay.Intersects(terrain);
			var terrainIntersection = cameraBottomRay.Position + terrainDistance.GetValueOrDefault() * cameraBottomRay.Direction;

			// Calculate the size of the bottom of the screen in world units.
			var cameraBottomWidthAtTerrain = RenderProcess.Viewer.Camera.RightFrustrumA * terrainDistance.GetValueOrDefault() * 2;
			// Shadow map is scaled so that one shadow map texel is ShadowMapTexelSize pixels at the bottom of the screen.
			var shadowMapSize = (float)Math.Round(MathHelper.Clamp(cameraBottomWidthAtTerrain * ShadowMapTexelSize * ShadowMapSize / RenderProcess.Viewer.DisplaySize.X, ShadowMapViewMin, ShadowMapViewMax) / ShadowMapViewStep) * ShadowMapViewStep;
			// Get vector pointing directly across the ground from camera,
			var cameraFront = new Vector3(cameraBottomRay.Direction.X, 0, cameraBottomRay.Direction.Z);
			// and shift shadow map as far forward as we can (just under half its size) to get the most in front of the camera.
			var shadowMapLocation = terrainIntersection + shadowMapSize / 2.1f * cameraFront / cameraFront.Length();
			// Align shadow map location to grid so it doesn't "flutter" so much. this basically means aligning it along a
			// grid based on the size of a shadow texel (shadowMapSize / shadowMapSize) along the axes of the sun direction
			// and up/left.
			var shadowMapAlignmentGrid = shadowMapSize / shadowMapSize;
			var shadowMapAlignAxisX = Vector3.Cross(sunDirection, Vector3.UnitY);
			var shadowMapAlignAxisY = Vector3.Cross(shadowMapAlignAxisX, sunDirection);
			var adjustX = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisX, shadowMapLocation), shadowMapAlignmentGrid);
			var adjustY = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisY, shadowMapLocation), shadowMapAlignmentGrid);
			var adjustZ = (float)Math.IEEERemainder(Vector3.Dot(sunDirection, shadowMapLocation), shadowMapAlignmentGrid);
			shadowMapLocation.X -= shadowMapAlignAxisX.X * adjustX;
			shadowMapLocation.Y -= shadowMapAlignAxisX.Y * adjustX;
			shadowMapLocation.Z -= shadowMapAlignAxisX.Z * adjustX;
			shadowMapLocation.X -= shadowMapAlignAxisY.X * adjustY;
			shadowMapLocation.Y -= shadowMapAlignAxisY.Y * adjustY;
			shadowMapLocation.Z -= shadowMapAlignAxisY.Z * adjustY;
			shadowMapLocation.X -= sunDirection.X * adjustZ;
			shadowMapLocation.Y -= sunDirection.Y * adjustZ;
			shadowMapLocation.Z -= sunDirection.Z * adjustZ;

			ShadowMapLightView = Matrix.CreateLookAt(shadowMapLocation + ShadowMapSunDistance * sunDirection, shadowMapLocation, Vector3.Up);
			ShadowMapLightProj = Matrix.CreateOrthographic(shadowMapSize, shadowMapSize, ShadowMapViewNear, ShadowMapViewFar);
			ShadowMapBound = new BoundingFrustum(ShadowMapLightView * ShadowMapLightProj);
		}

		/// <summary>
		/// Automatically adds or culls a <see cref="RenderPrimitive"/> based on a location, radius and max viewing distance.
		/// </summary>
		/// <remarks>
		/// Must be called from the UpdateProcess thread.
		/// </remarks>
		/// <param name="mstsLocation">Center location of the <see cref="RenderPrimitive"/> in MSTS coordinates.</param>
		/// <param name="objectRadius">Radius of a sphere containing the whole <see cref="RenderPrimitive"/>, centered on <paramref name="mstsLocation"/>.</param>
		/// <param name="objectViewingDistance">Maximum distance from which the <see cref="RenderPrimitive"/> should be viewable.</param>
		/// <param name="material"></param>
		/// <param name="primitive"></param>
		/// <param name="xnaMatrix"></param>
		/// <param name="flags"></param>
		public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			if (RenderProcess.Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
                AddPrimitive(material, primitive, group, ref xnaMatrix, flags);

			if (((flags & ShapeFlags.ShadowCaster) != 0) && IsInShadowMap(mstsLocation, objectRadius, objectViewingDistance))
				AddShadowPrimitive(material, primitive, ref xnaMatrix, flags);
		}

		/// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
		{
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None);
		}

		/// <summary>
		/// Executed in the UpdateProcess thread
		/// </summary>
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			var blended = material.GetBlending(primitive);
			// TODO: Alpha sorting code
			var sortingMaterial = blended ? DummyBlendedMaterial : material;
			//var sortingMaterial = material;
			var sequence = RenderItems[(int)GetRenderSequence(group, blended)];

			if (!sequence.ContainsKey(sortingMaterial))
				sequence.Add(sortingMaterial, new List<RenderItem>());

			sequence[sortingMaterial].Add(new RenderItem(material, primitive, xnaMatrix, flags));
            
			if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
				primitive.ZBias = 1;
		}

		/// <summary>
		/// Executed in the UpdateProcess thread
		/// </summary>
		public void AddShadowPrimitive(Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
		{
            AddPrimitive(material, primitive, RenderPrimitiveGroup.Shadows, ref xnaMatrix, flags);
		}

        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void Sort()
        {
			// TODO: Alpha sorting code
			//var renderItemComparer = new RenderItem.Comparer(RenderProcess.Viewer.Camera.Location);
			//foreach (var sequence in RenderItems.Where((d, i) => i != (int)RenderPrimitiveSequence.Shadows))
			//{
			//    foreach (var sequenceMaterial in sequence.Where(kvp => kvp.Value.Count > 0))
			//    {
			//        if (sequenceMaterial.Key != DummyBlendedMaterial)
			//            continue;
			//        sequenceMaterial.Value.Sort(renderItemComparer);
			//    }
			//}
        }

		public bool IsInShadowMap(Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
		{
			if (ShadowMapRenderTarget == null)
				return false;

			var xnaLocation = new Vector3(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
			return ShadowMapBound.Intersects(new BoundingSphere(xnaLocation, objectRadius));
		}

		public static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
		{
			if (blended)
				return RenderPrimitive.SequenceForBlended[(int)group];
			return RenderPrimitive.SequenceForOpaque[(int)group];
		}

        /// <summary>
        /// Draw 
        /// Executed in the RenderProcess thread 
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void Draw(GraphicsDevice graphicsDevice)
        {
#if DEBUG_RENDER_STATE
			DebugRenderState(graphicsDevice.RenderState, "RenderFrame.Draw");
#endif

			Materials.UpdateShaders(RenderProcess, graphicsDevice);

            if (RenderProcess.Viewer.SettingsBool[(int)BoolSettings.DynamicShadows])
                DrawShadows(graphicsDevice);

            DrawSimple(graphicsDevice);

			for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
				RenderProcess.PrimitiveCount[i] = RenderItems[i].Values.Sum(l => l.Count);
		}

        static RenderTarget2D ShadowMapRenderTarget;
        static DepthStencilBuffer ShadowMapStencilBuffer;
		static DepthStencilBuffer NormalStencilBuffer;
        static Texture2D ShadowMap;
        Matrix ShadowMapLightView;
        Matrix ShadowMapLightProj;
		BoundingFrustum ShadowMapBound;
		public void DrawShadows(GraphicsDevice graphicsDevice)
		{
			if (ShadowMapRenderTarget == null)
			{
				ShadowMapRenderTarget = new RenderTarget2D(graphicsDevice, ShadowMapSize, ShadowMapSize, 1, ShadowMapFormat, RenderTargetUsage.PreserveContents);
				ShadowMapStencilBuffer = new DepthStencilBuffer(graphicsDevice, ShadowMapSize, ShadowMapSize, DepthFormat.Depth16);
				NormalStencilBuffer = graphicsDevice.DepthStencilBuffer;
			}

			// Prepare renderer for drawing the shadow map.
			graphicsDevice.SetRenderTarget(0, ShadowMapRenderTarget);
			graphicsDevice.DepthStencilBuffer = ShadowMapStencilBuffer;
			graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1, 0);

			// Prepare for normal (non-blocking) rendering of scenery and terrain.
			Materials.ShadowMapMaterial.SetState(graphicsDevice, false);

			// Render non-terrain shadow items first.
            foreach (var pair in RenderItems[(int)RenderPrimitiveSequence.Shadows])
            {
                if (!(pair.Key is TerrainMaterial))
                    Materials.ShadowMapMaterial.Render(graphicsDevice, pair.Value, ref ShadowMapLightView, ref ShadowMapLightProj);
            }

			// Render terrain shadow items now, with their magic.
			graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
			graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;

            foreach (var pair in RenderItems[(int)RenderPrimitiveSequence.Shadows])
            {
                if (pair.Key is TerrainMaterial)
                    Materials.ShadowMapMaterial.Render(graphicsDevice, pair.Value, ref ShadowMapLightView, ref ShadowMapLightProj);
            }

			// Prepare for blocking rendering of terrain.
			Materials.ShadowMapMaterial.SetState(graphicsDevice, true);

			// Render terrain shadow items in blocking mode.
            foreach (var pair in RenderItems[(int)RenderPrimitiveSequence.Shadows])
            {
                if (pair.Key is TerrainMaterial)
                    Materials.ShadowMapMaterial.Render(graphicsDevice, pair.Value, ref ShadowMapLightView, ref ShadowMapLightProj);
            }

			// All done.
			Materials.ShadowMapMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
			DebugRenderState(graphicsDevice.RenderState, Materials.ShadowMapMaterial.ToString());
#endif
			graphicsDevice.DepthStencilBuffer = NormalStencilBuffer;
			graphicsDevice.SetRenderTarget(0, null);
			ShadowMap = ShadowMapRenderTarget.GetTexture();
		}

        /// <summary>
        /// Executed in the RenderProcess thread - simple draw
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void DrawSimple(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Materials.FogColor);

            DrawSequences(graphicsDevice);
        }

        public void DrawSequences(GraphicsDevice graphicsDevice)
        {
            if (RenderProcess.Viewer.SettingsBool[(int)BoolSettings.DynamicShadows])
            {
				var shadowMapMatrix = ShadowMapLightView * ShadowMapLightProj * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / ShadowMapStencilBuffer.Width, 0.5f + 0.5f / ShadowMapStencilBuffer.Height, 0, 1);
				Materials.SceneryShader.SetShadowMap(ref shadowMapMatrix, ShadowMap);
            }

			foreach (var sequence in RenderItems.Where((d, i) => i != (int)RenderPrimitiveSequence.Shadows))
			{
				foreach (var sequenceMaterial in sequence.Where(kvp => kvp.Value.Count > 0))
				{
					if (sequenceMaterial.Key == DummyBlendedMaterial)
					{
						// Blended: multiple materials, group by material as much as possible without destroying ordering.
					    Material lastMaterial = null;
					    var renderItems = new List<RenderItem>();
					    foreach (var renderItem in sequenceMaterial.Value)
					    {
					        if (lastMaterial != renderItem.Material)
					        {
					            if (renderItems.Count > 0)
					                lastMaterial.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
					            if (lastMaterial != null)
					                lastMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
								if (lastMaterial != null)
									DebugRenderState(graphicsDevice.RenderState, lastMaterial.ToString());
#endif
								renderItems.Clear();
					            renderItem.Material.SetState(graphicsDevice, lastMaterial);
					            lastMaterial = renderItem.Material;
					        }
					        renderItems.Add(renderItem);
					    }
					    if (renderItems.Count > 0)
					        lastMaterial.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
					    if (lastMaterial != null)
					        lastMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
						if (lastMaterial != null)
							DebugRenderState(graphicsDevice.RenderState, lastMaterial.ToString());
#endif
					}
					else
					{
						// Opaque: single material, render in one go.
						sequenceMaterial.Key.SetState(graphicsDevice, null);
						sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNAViewMatrix, ref XNAProjectionMatrix);
						sequenceMaterial.Key.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
						DebugRenderState(graphicsDevice.RenderState, sequenceMaterial.Key.ToString());
#endif
					}
				}
			}
        }

		static void DebugRenderState(RenderState renderState, string location)
		{
			if (renderState.AlphaBlendEnable != false) throw new InvalidOperationException(String.Format("RenderState.AlphaBlendEnable is {0}; expected {1} in {2}.", renderState.AlphaBlendEnable, false, location));
			if (renderState.AlphaBlendOperation != BlendFunction.Add) throw new InvalidOperationException(String.Format("RenderState.AlphaBlendOperation is {0}; expected {1} in {2}.", renderState.AlphaBlendOperation, BlendFunction.Add, location));
			// DOCUMENTATION IS WRONG, it says Blend.One:
			if (renderState.AlphaDestinationBlend != Blend.Zero) throw new InvalidOperationException(String.Format("RenderState.AlphaDestinationBlend is {0}; expected {1} in {2}.", renderState.AlphaDestinationBlend, Blend.Zero, location));
			if (renderState.AlphaFunction != CompareFunction.Always) throw new InvalidOperationException(String.Format("RenderState.AlphaFunction is {0}; expected {1} in {2}.", renderState.AlphaFunction, CompareFunction.Always, location));
			if (renderState.AlphaSourceBlend != Blend.One) throw new InvalidOperationException(String.Format("RenderState.AlphaSourceBlend is {0}; expected {1} in {2}.", renderState.AlphaSourceBlend, Blend.One, location));
			if (renderState.AlphaTestEnable != false) throw new InvalidOperationException(String.Format("RenderState.AlphaTestEnable is {0}; expected {1} in {2}.", renderState.AlphaTestEnable, false, location));
			if (renderState.BlendFactor != Color.White) throw new InvalidOperationException(String.Format("RenderState.BlendFactor is {0}; expected {1} in {2}.", renderState.BlendFactor, Color.White, location));
			if (renderState.BlendFunction != BlendFunction.Add) throw new InvalidOperationException(String.Format("RenderState.BlendFunction is {0}; expected {1} in {2}.", renderState.BlendFunction, BlendFunction.Add, location));
			// DOCUMENTATION IS WRONG, it says ColorWriteChannels.None:
			if (renderState.ColorWriteChannels != ColorWriteChannels.All) throw new InvalidOperationException(String.Format("RenderState.ColorWriteChannels is {0}; expected {1} in {2}.", renderState.ColorWriteChannels, ColorWriteChannels.All, location));
			// DOCUMENTATION IS WRONG, it says ColorWriteChannels.None:
			if (renderState.ColorWriteChannels1 != ColorWriteChannels.All) throw new InvalidOperationException(String.Format("RenderState.ColorWriteChannels1 is {0}; expected {1} in {2}.", renderState.ColorWriteChannels1, ColorWriteChannels.All, location));
			// DOCUMENTATION IS WRONG, it says ColorWriteChannels.None:
			if (renderState.ColorWriteChannels2 != ColorWriteChannels.All) throw new InvalidOperationException(String.Format("RenderState.ColorWriteChannels2 is {0}; expected {1} in {2}.", renderState.ColorWriteChannels2, ColorWriteChannels.All, location));
			// DOCUMENTATION IS WRONG, it says ColorWriteChannels.None:
			if (renderState.ColorWriteChannels3 != ColorWriteChannels.All) throw new InvalidOperationException(String.Format("RenderState.ColorWriteChannels3 is {0}; expected {1} in {2}.", renderState.ColorWriteChannels3, ColorWriteChannels.All, location));
			if (renderState.CounterClockwiseStencilDepthBufferFail != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.CounterClockwiseStencilDepthBufferFail is {0}; expected {1} in {2}.", renderState.CounterClockwiseStencilDepthBufferFail, StencilOperation.Keep, location));
			if (renderState.CounterClockwiseStencilFail != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.CounterClockwiseStencilFail is {0}; expected {1} in {2}.", renderState.CounterClockwiseStencilFail, StencilOperation.Keep, location));
			if (renderState.CounterClockwiseStencilFunction != CompareFunction.Always) throw new InvalidOperationException(String.Format("RenderState.CounterClockwiseStencilFunction is {0}; expected {1} in {2}.", renderState.CounterClockwiseStencilFunction, CompareFunction.Always, location));
			if (renderState.CounterClockwiseStencilPass != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.CounterClockwiseStencilPass is {0}; expected {1} in {2}.", renderState.CounterClockwiseStencilPass, StencilOperation.Keep, location));
			if (renderState.CullMode != CullMode.CullCounterClockwiseFace) throw new InvalidOperationException(String.Format("RenderState.CullMode is {0}; expected {1} in {2}.", renderState.CullMode, CullMode.CullCounterClockwiseFace, location));
			if (renderState.DepthBias != 0.0f) throw new InvalidOperationException(String.Format("RenderState.DepthBias is {0}; expected {1} in {2}.", renderState.DepthBias, 0.0f, location));
			if (renderState.DepthBufferEnable != true) throw new InvalidOperationException(String.Format("RenderState.DepthBufferEnable is {0}; expected {1} in {2}.", renderState.DepthBufferEnable, true, location));
			if (renderState.DepthBufferFunction != CompareFunction.LessEqual) throw new InvalidOperationException(String.Format("RenderState.DepthBufferFunction is {0}; expected {1} in {2}.", renderState.DepthBufferFunction, CompareFunction.LessEqual, location));
			if (renderState.DepthBufferWriteEnable != true) throw new InvalidOperationException(String.Format("RenderState.DepthBufferWriteEnable is {0}; expected {1} in {2}.", renderState.DepthBufferWriteEnable, true, location));
			if (renderState.DestinationBlend != Blend.Zero) throw new InvalidOperationException(String.Format("RenderState.DestinationBlend is {0}; expected {1} in {2}.", renderState.DestinationBlend, Blend.Zero, location));
			if (renderState.FillMode != FillMode.Solid) throw new InvalidOperationException(String.Format("RenderState.FillMode is {0}; expected {1} in {2}.", renderState.FillMode, FillMode.Solid, location));
			if (renderState.FogColor != Color.TransparentBlack) throw new InvalidOperationException(String.Format("RenderState.FogColor is {0}; expected {1} in {2}.", renderState.FogColor, Color.TransparentBlack, location));
			if (renderState.FogDensity != 1.0f) throw new InvalidOperationException(String.Format("RenderState.FogDensity is {0}; expected {1} in {2}.", renderState.FogDensity, 1.0f, location));
			if (renderState.FogEnable != false) throw new InvalidOperationException(String.Format("RenderState.FogEnable is {0}; expected {1} in {2}.", renderState.FogEnable, false, location));
			if (renderState.FogEnd != 1.0f) throw new InvalidOperationException(String.Format("RenderState.FogEnd is {0}; expected {1} in {2}.", renderState.FogEnd, 1.0f, location));
			if (renderState.FogStart != 0.0f) throw new InvalidOperationException(String.Format("RenderState.FogStart is {0}; expected {1} in {2}.", renderState.FogStart, 0.0f, location));
			if (renderState.FogTableMode != FogMode.None) throw new InvalidOperationException(String.Format("RenderState.FogTableMode is {0}; expected {1} in {2}.", renderState.FogTableMode, FogMode.None, location));
			if (renderState.FogVertexMode != FogMode.None) throw new InvalidOperationException(String.Format("RenderState.FogVertexMode is {0}; expected {1} in {2}.", renderState.FogVertexMode, FogMode.None, location));
			if (renderState.MultiSampleAntiAlias != true) throw new InvalidOperationException(String.Format("RenderState.MultiSampleAntiAlias is {0}; expected {1} in {2}.", renderState.MultiSampleAntiAlias, true, location));
			if (renderState.MultiSampleMask != -1) throw new InvalidOperationException(String.Format("RenderState.MultiSampleMask is {0}; expected {1} in {2}.", renderState.MultiSampleMask, -1, location));
			//if (renderState.PointSize != 64) throw new InvalidOperationException(String.Format("RenderState.e.PointSize is {0}; expected {1} in {2}.", renderState.e.PointSize, 64, location));
			//if (renderState.PointSizeMax != 64.0f) throw new InvalidOperationException(String.Format("RenderState.PointSizeMax is {0}; expected {1} in {2}.", renderState.PointSizeMax, 64.0f, location));
			//if (renderState.PointSizeMin != 1.0f) throw new InvalidOperationException(String.Format("RenderState.PointSizeMin is {0}; expected {1} in {2}.", renderState.PointSizeMin, 1.0f, location));
			if (renderState.PointSpriteEnable != false) throw new InvalidOperationException(String.Format("RenderState.PointSpriteEnable is {0}; expected {1} in {2}.", renderState.PointSpriteEnable, false, location));
			if (renderState.RangeFogEnable != false) throw new InvalidOperationException(String.Format("RenderState.RangeFogEnable is {0}; expected {1} in {2}.", renderState.RangeFogEnable, false, location));
			if (renderState.ReferenceAlpha != 0) throw new InvalidOperationException(String.Format("RenderState.ReferenceAlpha is {0}; expected {1} in {2}.", renderState.ReferenceAlpha, 0, location));
			if (renderState.ReferenceStencil != 0) throw new InvalidOperationException(String.Format("RenderState.ReferenceStencil is {0}; expected {1} in {2}.", renderState.ReferenceStencil, 0, location));
			if (renderState.ScissorTestEnable != false) throw new InvalidOperationException(String.Format("RenderState.ScissorTestEnable is {0}; expected {1} in {2}.", renderState.ScissorTestEnable, false, location));
			if (renderState.SeparateAlphaBlendEnabled != false) throw new InvalidOperationException(String.Format("RenderState.SeparateAlphaBlendEnabled is {0}; expected {1} in {2}.", renderState.SeparateAlphaBlendEnabled, false, location));
			if (renderState.SlopeScaleDepthBias != 0) throw new InvalidOperationException(String.Format("RenderState.SlopeScaleDepthBias is {0}; expected {1} in {2}.", renderState.SlopeScaleDepthBias, 0, location));
			if (renderState.SourceBlend != Blend.One) throw new InvalidOperationException(String.Format("RenderState.SourceBlend is {0}; expected {1} in {2}.", renderState.SourceBlend, Blend.One, location));
			if (renderState.StencilDepthBufferFail != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.StencilDepthBufferFail is {0}; expected {1} in {2}.", renderState.StencilDepthBufferFail, StencilOperation.Keep, location));
			if (renderState.StencilEnable != false) throw new InvalidOperationException(String.Format("RenderState.StencilEnable is {0}; expected {1} in {2}.", renderState.StencilEnable, false, location));
			if (renderState.StencilFail != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.StencilFail is {0}; expected {1} in {2}.", renderState.StencilFail, StencilOperation.Keep, location));
			if (renderState.StencilFunction != CompareFunction.Always) throw new InvalidOperationException(String.Format("RenderState.StencilFunction is {0}; expected {1} in {2}.", renderState.StencilFunction, CompareFunction.Always, location));
			// DOCUMENTATION IS WRONG, it says Int32.MaxValue:
			if (renderState.StencilMask != -1) throw new InvalidOperationException(String.Format("RenderState.StencilMask is {0}; expected {1} in {2}.", renderState.StencilMask, -1, location));
			if (renderState.StencilPass != StencilOperation.Keep) throw new InvalidOperationException(String.Format("RenderState.StencilPass is {0}; expected {1} in {2}.", renderState.StencilPass, StencilOperation.Keep, location));
			// DOCUMENTATION IS WRONG, it says Int32.MaxValue:
			if (renderState.StencilWriteMask != -1) throw new InvalidOperationException(String.Format("RenderState.StencilWriteMask is {0}; expected {1} in {2}.", renderState.StencilWriteMask, -1, location));
			if (renderState.TwoSidedStencilMode != false) throw new InvalidOperationException(String.Format("RenderState.TwoSidedStencilMode is {0}; expected {1} in {2}.", renderState.TwoSidedStencilMode, false, location));
		}
    }
}
