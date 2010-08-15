using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ORTS
{
    public abstract class RenderPrimitive
    {
		public float ZBias = 0f;
		public int Sequence = 0;

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
        public readonly Matrix XNAMatrix;
		public readonly ShapeFlags Flags;

		public RenderItem(Material material, RenderPrimitive renderPrimitive, Matrix xnaMatrix, ShapeFlags flags)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
        }
    }

    public class RenderFrame
    {
		const int ShadowMapSunDistance = 1000; // distance from shadow map center to put camera
		const int ShadowMapViewMin = 256; // minimum width/height of shadow map projection
		const int ShadowMapViewMax = 2048; // maximum width/height of shadow map projection
		const int ShadowMapTexelSize = 4; // number of screen pixel to scale 1 shadow map texel to
		const int ShadowMapSize = 4096; // shadow map texture width/height
		const float ShadowMapViewNear = 0f; // near plane for shadow map camera
		const float ShadowMapViewFar = 2000f; // far plane for shadow map camera

		readonly RenderProcess RenderProcess;
        readonly List<RenderItem> RenderItems;
		readonly List<RenderItem> RenderShadowItems;
        int RenderMaxSequence = 0;
        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;

        public RenderFrame(RenderProcess owner)
        {
            RenderProcess = owner;
            RenderItems = new List<RenderItem>();
			RenderShadowItems = new List<RenderItem>();
        }

        public void Clear() 
        {
            RenderItems.Clear();
			RenderShadowItems.Clear();
        }

        public void SetCamera(ref Matrix xnaViewMatrix, ref Matrix xnaProjectionMatrix)
        {
            XNAViewMatrix = xnaViewMatrix;
            XNAProjectionMatrix = xnaProjectionMatrix;
        }

		public void PrepareFrame(ElapsedTime elapsedTime)
		{
			var sunDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
			var cameraLocation = RenderProcess.Viewer.Camera.Location * new Vector3(1, 1, -1);
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
			var shadowMapSize = MathHelper.Clamp(cameraBottomWidthAtTerrain * ShadowMapTexelSize * ShadowMapSize / RenderProcess.Viewer.DisplaySize.X, ShadowMapViewMin, ShadowMapViewMax);
			// Get vector pointing directly across the ground from camera,
			var cameraFront = new Vector3(cameraBottomRay.Direction.X, 0, cameraBottomRay.Direction.Z);
			// and shift shadow map as far forward as we can (just under half its size) to get the most in front of the camera.
			var shadowMapLocation = terrainIntersection + shadowMapSize / 2.1f * cameraFront / cameraFront.Length();

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
		public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			if (RenderProcess.Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
				AddPrimitive(material, primitive, ref xnaMatrix, flags);
			if (((flags & ShapeFlags.ShadowCaster) != 0) && IsInShadowMap(mstsLocation, objectRadius, objectViewingDistance))
				AddShadowPrimitive(material, primitive, ref xnaMatrix, flags);
		}

		/// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
		public void AddPrimitive(Material material, RenderPrimitive primitive, ref Matrix xnaMatrix)
		{
			AddPrimitive(material, primitive, ref xnaMatrix, ShapeFlags.None);
		}

		/// <summary>
		/// Executed in the UpdateProcess thread
		/// </summary>
		public void AddPrimitive(Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			RenderItems.Add(new RenderItem(material, primitive, xnaMatrix, flags));

			if (RenderMaxSequence < primitive.Sequence)
				RenderMaxSequence = primitive.Sequence;

			if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
				primitive.ZBias = 1;
		}

		/// <summary>
		/// Executed in the UpdateProcess thread
		/// </summary>
		public void AddShadowPrimitive(Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			RenderShadowItems.Add(new RenderItem(material, primitive, xnaMatrix, flags));
		}

        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void Sort()
        {
            // TODO, enhance this:
            //   - to sort translucent primitives
            //   - and to minimize render state changes ( sorting was taking too long! for this )
        }

		public bool IsInShadowMap(Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
		{
			if (ShadowMapRenderTarget == null)
				return false;

			var xnaLocation = mstsLocation * new Vector3(1, 1, -1);
			return ShadowMapBound.Intersects(new BoundingSphere(xnaLocation, objectRadius));
		}

        /// <summary>
        /// Draw 
        /// Executed in the RenderProcess thread 
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void Draw(GraphicsDevice graphicsDevice)
        {
            Materials.UpdateShaders(RenderProcess, graphicsDevice);
            if (RenderProcess.Viewer.DynamicShadows)
                DrawShadows(graphicsDevice);
            DrawSimple(graphicsDevice);
        }

        RenderTarget2D ShadowMapRenderTarget;
        DepthStencilBuffer ShadowMapStencilBuffer;
        Texture2D ShadowMap;
        Matrix ShadowMapLightView;
        Matrix ShadowMapLightProj;
		BoundingFrustum ShadowMapBound;
		public void DrawShadows(GraphicsDevice graphicsDevice)
		{
			if (ShadowMapRenderTarget == null)
			{
				ShadowMapRenderTarget = new RenderTarget2D(graphicsDevice, ShadowMapSize, ShadowMapSize, 1, SurfaceFormat.Rg32);
				ShadowMapStencilBuffer = new DepthStencilBuffer(graphicsDevice, ShadowMapSize, ShadowMapSize, DepthFormat.Depth16);
			}

			// Prepare renderer for drawing the shadow map.
			var oldStencilDepthBuffer = graphicsDevice.DepthStencilBuffer;
			graphicsDevice.SetRenderTarget(0, ShadowMapRenderTarget);
			graphicsDevice.DepthStencilBuffer = ShadowMapStencilBuffer;
			graphicsDevice.Clear(Color.Black);

			// Prepare for normal (non-blocking) rendering of scenery and terrain.
			Materials.ShadowMapMaterial.SetState(graphicsDevice, false);

			// Render non-terrain shadow items first.
			foreach (var renderItem in RenderShadowItems)
			{
				var riMatrix = renderItem.XNAMatrix;
				if (!(renderItem.Material is TerrainMaterial))
					Materials.ShadowMapMaterial.Render(graphicsDevice, renderItem.Material, renderItem.RenderPrimitive, ref riMatrix, ref ShadowMapLightView, ref ShadowMapLightProj);
			}

			// Render terrain shadow items now, with their magic.
			graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
			graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;
			foreach (var renderItem in RenderShadowItems)
			{
				var riMatrix = renderItem.XNAMatrix;
				if (renderItem.Material is TerrainMaterial)
					Materials.ShadowMapMaterial.Render(graphicsDevice, renderItem.Material, renderItem.RenderPrimitive, ref riMatrix, ref ShadowMapLightView, ref ShadowMapLightProj);
			}

			// Prepare for blocking rendering of terrain.
			Materials.ShadowMapMaterial.SetState(graphicsDevice, true);

			// Render terrain shadow items in blocking mode.
			foreach (var renderItem in RenderShadowItems)
			{
				var riMatrix = renderItem.XNAMatrix;
				if (renderItem.Material is TerrainMaterial)
					Materials.ShadowMapMaterial.Render(graphicsDevice, renderItem.Material, renderItem.RenderPrimitive, ref riMatrix, ref ShadowMapLightView, ref ShadowMapLightProj);
			}

			// All done.
			Materials.ShadowMapMaterial.ResetState(graphicsDevice, null);
			graphicsDevice.DepthStencilBuffer = oldStencilDepthBuffer;
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

            for (int iSequence = 0; iSequence <= RenderMaxSequence; ++iSequence)
                DrawSequence(graphicsDevice, iSequence);

        }

        public void DrawSequence(GraphicsDevice graphicsDevice, int sequence)
        {
            if (RenderProcess.Viewer.DynamicShadows)
            {
                Materials.SceneryShader.ShadowMapTexture = ShadowMap;
				Materials.SceneryShader.LightViewProjectionShadowProjection = ShadowMapLightView * ShadowMapLightProj * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / ShadowMapStencilBuffer.Width, 0.5f + 0.5f / ShadowMapStencilBuffer.Height, 0, 1);
            }

            // Render each material on the specified primitive
            // To minimize renderstate changes, the material is
            // told what material was used previously so it can
            // make a decision on what renderstates need to be
            // changed.
            Material prevMaterial = null;
            foreach (var renderItem in RenderItems)
            {
                Material currentMaterial = renderItem.Material;
                if (renderItem.RenderPrimitive.Sequence == sequence)
                {
                    if (prevMaterial != null)
                        prevMaterial.ResetState(graphicsDevice, currentMaterial);
					var riMatrix = renderItem.XNAMatrix;
					currentMaterial.Render(graphicsDevice, prevMaterial, renderItem.RenderPrimitive, ref riMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    prevMaterial = currentMaterial;
                }
            }
            if (prevMaterial != null)
                prevMaterial.ResetState(graphicsDevice, null);
        }
    }
}
