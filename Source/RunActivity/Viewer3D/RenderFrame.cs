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
			var cameraLocation = RenderProcess.Viewer.Camera.Location * new Vector3(1, 1, -1);
			ShadowMapLightView = Matrix.CreateLookAt(cameraLocation + 1000 * RenderProcess.Viewer.SkyDrawer.solarDirection, cameraLocation, Vector3.Up);
			ShadowMapLightProj = Matrix.CreateOrthographic(ShadowMapViewSize, ShadowMapViewSize, ShadowMapViewNear, ShadowMapViewFar);
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

        const int ShadowMapSize = 4096;
		const int ShadowMapViewSize = 512;
		const float ShadowMapViewNear = 0.01f;
		const float ShadowMapViewFar = 2500.0f;
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
				ShadowMapRenderTarget = new RenderTarget2D(graphicsDevice, ShadowMapSize, ShadowMapSize, 1, SurfaceFormat.Single);
				ShadowMapStencilBuffer = new DepthStencilBuffer(graphicsDevice, ShadowMapSize, ShadowMapSize, DepthFormat.Depth16);
			}

			var oldStencilDepthBuffer = graphicsDevice.DepthStencilBuffer;
			graphicsDevice.SetRenderTarget(0, ShadowMapRenderTarget);
			graphicsDevice.DepthStencilBuffer = ShadowMapStencilBuffer;
			graphicsDevice.Clear(Color.Black);
			Materials.ShadowMapMaterial.SetState(graphicsDevice, ShadowMapLightView, ShadowMapLightProj);

			foreach (var renderItem in RenderShadowItems)
			{
				var riMatrix = renderItem.XNAMatrix;
				if (!(renderItem.Material is TerrainMaterial))
					Materials.ShadowMapMaterial.Render(graphicsDevice, null, renderItem.RenderPrimitive, ref riMatrix, ref ShadowMapLightView, ref ShadowMapLightProj);
			}

			graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
			graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;
			foreach (var renderItem in RenderShadowItems)
			{
				var riMatrix = renderItem.XNAMatrix;
				if (renderItem.Material is TerrainMaterial)
					Materials.ShadowMapMaterial.Render(graphicsDevice, null, renderItem.RenderPrimitive, ref riMatrix, ref ShadowMapLightView, ref ShadowMapLightProj);
			}

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
