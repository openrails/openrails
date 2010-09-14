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
			//Materials.ShadowMapMaterial.ResetState(graphicsDevice);
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
					}
					else
					{
						// Opaque: single material, render in one go.
						sequenceMaterial.Key.SetState(graphicsDevice, null);
						sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNAViewMatrix, ref XNAProjectionMatrix);
						sequenceMaterial.Key.ResetState(graphicsDevice);
					}
				}
			}
        }
    }
}
