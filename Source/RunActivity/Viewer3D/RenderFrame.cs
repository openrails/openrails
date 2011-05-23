// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

// Define this to check every material is resetting the RenderState correctly.
//#define DEBUG_RENDER_STATE

// Define this to use the experimental collection that reduces reallocation
// and copying of RenderItems. This reduces the number/frequency of Gen 0 and
// Gen 1 garbage collections but has little overall performance effect. It
// also leaks memory currently.
//#define RENDER_ITEM_COLLECTION

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
        CabOpaque,
        WorldOpaque,
        WorldBlended,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        CabBlended,
        TextOverlayOpaque,
        TextOverlayBlended,
        // This value must be last.
        Sentinel
    }

    public enum RenderPrimitiveGroup
    {
        Cab,
        World,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        Overlay
    }

    public abstract class RenderPrimitive
    {
        public static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
			RenderPrimitiveSequence.CabBlended,
			RenderPrimitiveSequence.WorldBlended,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
			RenderPrimitiveSequence.TextOverlayBlended,
		};
        public static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
			RenderPrimitiveSequence.CabOpaque,
			RenderPrimitiveSequence.WorldOpaque,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
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

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
#if RENDER_ITEM_COLLECTION
    public struct RenderItem
#else
    public class RenderItem
#endif
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;

#if !RENDER_ITEM_COLLECTION
        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
        }
#endif

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

#if RENDER_ITEM_COLLECTION
	public class RenderItemCollection : IEnumerable<RenderItem>, IEnumerator<RenderItem>
	{
		RenderItem[] RenderItems;
		int count;
		bool Enumerating;
		int EnumeratingIndex;

		public RenderItemCollection()
		{
			// 128 is a reasonable size, i.e. few instances need to resize up
			// but isn't too big. Further tuning certainly possible.
			RenderItems = new RenderItem[128];
			count = 0;
		}

		public RenderItem this[int index]
		{
			get
			{
				return RenderItems[index];
			}
		}

		public int Count
		{
			get
			{
				return count;
			}
		}

		public void Add(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags)
		{
			if (count >= RenderItems.Length)
			{
				var newRenderItems = new RenderItem[RenderItems.Length * 2];
				Array.Copy(RenderItems, newRenderItems, RenderItems.Length);
				RenderItems = newRenderItems;
			}
			RenderItems[count].Material = material;
			RenderItems[count].RenderPrimitive = renderPrimitive;
			RenderItems[count].XNAMatrix = xnaMatrix;
			RenderItems[count].Flags = flags;
			count++;
		}

		public void Clear()
		{
			count = 0;
		}

    #region IEnumerable<RenderItem> Members

		public IEnumerator<RenderItem> GetEnumerator()
		{
			//return new RenderItemCollectionEnumerator(this);
			if (Enumerating) throw new InvalidOperationException("RenderItemCollection can only have one enumerator.");
			Enumerating = true;
			EnumeratingIndex = -1;
			return this;
		}

    #endregion

    #region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

    #endregion

    #region IEnumerator<RenderItem> Members

		public RenderItem Current
		{
			get
			{
				return RenderItems[EnumeratingIndex];
			}
		}

    #endregion

    #region IDisposable Members

		public void Dispose()
		{
			if (!Enumerating) throw new InvalidOperationException("RenderItemCollection is not enumerating.");
			Enumerating = false;
		}

    #endregion

    #region IEnumerator Members

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return RenderItems[EnumeratingIndex];
			}
		}

		public bool MoveNext()
		{
			EnumeratingIndex++;
			return EnumeratingIndex < count;
		}

		public void Reset()
		{
			EnumeratingIndex = -1;
		}

    #endregion
	}
#else
    public class RenderItemCollection : List<RenderItem>
    {
    }
#endif

    public class RenderFrame
    {
        // Shared shadow map data.
        static Texture2D[] ShadowMap;
        static RenderTarget2D[] ShadowMapRenderTarget;
        static DepthStencilBuffer ShadowMapStencilBuffer;
        static DepthStencilBuffer NormalStencilBuffer;
        static Vector3 SteppedSolarDirection = Vector3.UnitX;

        // Local shadow map data.
        Matrix[] ShadowMapLightView;
        Matrix[] ShadowMapLightProj;
        Matrix[] ShadowMapLightViewProjShadowProj;
        BoundingFrustum[] ShadowMapBound;

        static readonly Material DummyBlendedMaterial = new EmptyMaterial();

        readonly RenderProcess RenderProcess;
        readonly Dictionary<Material, RenderItemCollection>[] RenderItems = new Dictionary<Material, RenderItemCollection>[(int)RenderPrimitiveSequence.Sentinel];
        readonly RenderItemCollection[] RenderShadowItems;

        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;

        public RenderFrame(RenderProcess owner)
        {
            RenderProcess = owner;

            for (int i = 0; i < RenderItems.Length; i++)
                RenderItems[i] = new Dictionary<Material, RenderItemCollection>();

            if (RenderProcess.Viewer.Settings.DynamicShadows)
            {
                if (ShadowMap == null)
                {
                    var shadowMapSize = RenderProcess.Viewer.Settings.ShadowMapResolution;
                    ShadowMap = new Texture2D[RenderProcess.ShadowMapCount];
                    ShadowMapRenderTarget = new RenderTarget2D[RenderProcess.ShadowMapCount];
                    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                        ShadowMapRenderTarget[shadowMapIndex] = new RenderTarget2D(RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, RenderProcess.ShadowMapMipCount, SurfaceFormat.Rg32, RenderTargetUsage.PreserveContents);
                    ShadowMapStencilBuffer = new DepthStencilBuffer(RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, DepthFormat.Depth16);
                    NormalStencilBuffer = RenderProcess.GraphicsDevice.DepthStencilBuffer;
                }

                ShadowMapLightView = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightViewProjShadowProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapBound = new BoundingFrustum[RenderProcess.ShadowMapCount];

                RenderShadowItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    RenderShadowItems[shadowMapIndex] = new RenderItemCollection();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < RenderItems.Length; i++)
                foreach (Material mat in RenderItems[i].Keys)
                    RenderItems[i][mat].Clear();
            if (RenderProcess.Viewer.Settings.DynamicShadows)
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    RenderShadowItems[shadowMapIndex].Clear();
        }

        public void SetCamera(ref Matrix xnaViewMatrix, ref Matrix xnaProjectionMatrix)
        {
            XNAViewMatrix = xnaViewMatrix;
            XNAProjectionMatrix = xnaProjectionMatrix;
        }

        static bool LockShadows;
        public void PrepareFrame(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommands.DebugLockShadows))
                LockShadows = !LockShadows;

            if (RenderProcess.Viewer.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && !LockShadows)
            {
                var solarDirection = RenderProcess.Viewer.SkyDrawer.solarDirection;
                solarDirection.Normalize();
                if (Vector3.Dot(SteppedSolarDirection, solarDirection) < 0.99999)
                    SteppedSolarDirection = solarDirection;

                var cameraLocation = RenderProcess.Viewer.Camera.Location;
                cameraLocation.Z *= -1;

                var xnaCameraView = RenderProcess.Viewer.Camera.XNAView;
                var cameraDirection = new Vector3(-xnaCameraView.M13, -xnaCameraView.M23, -xnaCameraView.M33);
                cameraDirection.Normalize();

                var shadowMapAlignAxisX = Vector3.Cross(SteppedSolarDirection, Vector3.UnitY);
                var shadowMapAlignAxisY = Vector3.Cross(shadowMapAlignAxisX, SteppedSolarDirection);
                shadowMapAlignAxisX.Normalize();
                shadowMapAlignAxisY.Normalize();

                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    var viewingDistance = RenderProcess.Viewer.Settings.ViewingDistance;
                    var shadowMapDiameter = RenderProcess.ShadowMapDiameter[shadowMapIndex];
                    var shadowMapLocation = cameraLocation + RenderProcess.ShadowMapDistance[shadowMapIndex] * cameraDirection;

                    // Align shadow map location to grid so it doesn't "flutter" so much. This basically means aligning it along a
                    // grid based on the size of a shadow texel (shadowMapSize / shadowMapSize) along the axes of the sun direction
                    // and up/left.
                    var shadowMapAlignmentGrid = (float)shadowMapDiameter / RenderProcess.Viewer.Settings.ShadowMapResolution;
                    var adjustX = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisX, shadowMapLocation), shadowMapAlignmentGrid);
                    var adjustY = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisY, shadowMapLocation), shadowMapAlignmentGrid);
                    shadowMapLocation.X -= shadowMapAlignAxisX.X * adjustX;
                    shadowMapLocation.Y -= shadowMapAlignAxisX.Y * adjustX;
                    shadowMapLocation.Z -= shadowMapAlignAxisX.Z * adjustX;
                    shadowMapLocation.X -= shadowMapAlignAxisY.X * adjustY;
                    shadowMapLocation.Y -= shadowMapAlignAxisY.Y * adjustY;
                    shadowMapLocation.Z -= shadowMapAlignAxisY.Z * adjustY;

                    ShadowMapLightView[shadowMapIndex] = Matrix.CreateLookAt(shadowMapLocation + (viewingDistance + shadowMapDiameter / 2) * SteppedSolarDirection, shadowMapLocation, Vector3.Up);
                    ShadowMapLightProj[shadowMapIndex] = Matrix.CreateOrthographic(shadowMapDiameter, shadowMapDiameter, viewingDistance, viewingDistance + shadowMapDiameter);
                    ShadowMapLightViewProjShadowProj[shadowMapIndex] = ShadowMapLightView[shadowMapIndex] * ShadowMapLightProj[shadowMapIndex] * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / ShadowMapStencilBuffer.Width, 0.5f + 0.5f / ShadowMapStencilBuffer.Height, 0, 1);
                    ShadowMapBound[shadowMapIndex] = new BoundingFrustum(ShadowMapLightView[shadowMapIndex] * ShadowMapLightProj[shadowMapIndex]);
                }
            }
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
        /// <param name="group"></param>
        /// <param name="xnaMatrix"></param>
        /// <param name="flags"></param>
        public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (RenderProcess.Viewer.Camera.InRange(mstsLocation, objectRadius, objectViewingDistance))
            {
                if (RenderProcess.Viewer.Camera.InFOV(mstsLocation, objectRadius))
                    AddPrimitive(material, primitive, group, ref xnaMatrix, flags);

                if (RenderProcess.Viewer.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ((flags & ShapeFlags.ShadowCaster) != 0))
                    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                        if (IsInShadowMap(shadowMapIndex, mstsLocation, objectRadius, objectViewingDistance))
                            AddShadowPrimitive(shadowMapIndex, material, primitive, ref xnaMatrix, flags);
            }
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

#if RENDER_ITEM_COLLECTION
			if (!sequence.ContainsKey(sortingMaterial))
				sequence.Add(sortingMaterial, new RenderItemCollection());

			sequence[sortingMaterial].Add(material, primitive, ref xnaMatrix, flags);
#else
            RenderItemCollection items;
            if (!sequence.TryGetValue(sortingMaterial, out items))
            {
                items = new RenderItemCollection();
                sequence.Add(sortingMaterial, items);
            }
            items.Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
#endif

            if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
                primitive.ZBias = 1;
        }

        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        void AddShadowPrimitive(int shadowMapIndex, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
#if RENDER_ITEM_COLLECTION
			RenderShadowItems[shadowMapIndex].Add(material, primitive, ref xnaMatrix, flags);
#else
            RenderShadowItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
#endif
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

        bool IsInShadowMap(int shadowMapIndex, Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
        {
            if (ShadowMapRenderTarget == null)
                return false;

            mstsLocation.Z *= -1;
            return ShadowMapBound[shadowMapIndex].Intersects(new BoundingSphere(mstsLocation, objectRadius));
        }

        static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
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
            var logging = UserInput.IsPressed(UserCommands.DebugLogRenderFrame);
            if (logging)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Draw {");
            }

            Materials.UpdateShaders(RenderProcess, graphicsDevice);

            if (RenderProcess.Viewer.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0))
                DrawShadows(graphicsDevice, logging);

            DrawSimple(graphicsDevice, logging);

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
                RenderProcess.PrimitiveCount[i] = RenderItems[i].Values.Sum(l => l.Count);

            if (logging)
            {
                Console.WriteLine("}");
                Console.WriteLine();
            }
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging)
        {
            if (logging) Console.WriteLine("  DrawShadows {");
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                DrawShadows(graphicsDevice, logging, shadowMapIndex);
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                RenderProcess.ShadowPrimitiveCount[shadowMapIndex] = RenderShadowItems[shadowMapIndex].Count;
            if (logging) Console.WriteLine("  }");
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging, int shadowMapIndex)
        {
            if (logging) Console.WriteLine("    {0} {{", shadowMapIndex);

            // Prepare renderer for drawing the shadow map.
            graphicsDevice.SetRenderTarget(0, ShadowMapRenderTarget[shadowMapIndex]);
            graphicsDevice.DepthStencilBuffer = ShadowMapStencilBuffer;
            graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1, 0);

            // Prepare for normal (non-blocking) rendering of scenery.
            Materials.ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Render non-terrain, non-forest shadow items first.
            if (logging) Console.WriteLine("      {0,-5} * SceneryMaterial (normal)", RenderShadowItems[shadowMapIndex].Count(ri => ri.Material is SceneryMaterial));
            Materials.ShadowMapMaterial.Render(graphicsDevice, RenderShadowItems[shadowMapIndex].Where(ri => ri.Material is SceneryMaterial), ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for normal (non-blocking) rendering of forests.
            Materials.ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Forest);

            // Render forest shadow items next.
            if (logging) Console.WriteLine("      {0,-5} * ForestMaterial (forest)", RenderShadowItems[shadowMapIndex].Count(ri => ri.Material is ForestMaterial));
            Materials.ShadowMapMaterial.Render(graphicsDevice, RenderShadowItems[shadowMapIndex].Where(ri => ri.Material is ForestMaterial), ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for normal (non-blocking) rendering of terrain.
            Materials.ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Render terrain shadow items now, with their magic.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (normal)", RenderShadowItems[shadowMapIndex].Count(ri => ri.Material is TerrainMaterial));
            Materials.ShadowMapMaterial.Render(graphicsDevice, RenderShadowItems[shadowMapIndex].Where(ri => ri.Material is TerrainMaterial), ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for blocking rendering of terrain.
            Materials.ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Blocker);

            // Render terrain shadow items in blocking mode.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (blocker)", RenderShadowItems[shadowMapIndex].Count(ri => ri.Material is TerrainMaterial));
            Materials.ShadowMapMaterial.Render(graphicsDevice, RenderShadowItems[shadowMapIndex].Where(ri => ri.Material is TerrainMaterial), ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // All done.
            Materials.ShadowMapMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
			DebugRenderState(graphicsDevice.RenderState, Materials.ShadowMapMaterial.ToString());
#endif
            graphicsDevice.DepthStencilBuffer = NormalStencilBuffer;
            graphicsDevice.SetRenderTarget(0, null);
            ShadowMap[shadowMapIndex] = ShadowMapRenderTarget[shadowMapIndex].GetTexture();

            // Blur the shadow map.
            if (RenderProcess.Viewer.Settings.ShadowMapBlur)
            {
                ShadowMap[shadowMapIndex] = Materials.ShadowMapMaterial.ApplyBlur(graphicsDevice, ShadowMap[shadowMapIndex], ShadowMapRenderTarget[shadowMapIndex], ShadowMapStencilBuffer, NormalStencilBuffer);
#if DEBUG_RENDER_STATE
				DebugRenderState(graphicsDevice.RenderState, Materials.ShadowMapMaterial.ToString() + " ApplyBlur()");
#endif
            }

            if (logging) Console.WriteLine("    }");
        }

        /// <summary>
        /// Executed in the RenderProcess thread - simple draw
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="logging"></param>
        void DrawSimple(GraphicsDevice graphicsDevice, bool logging)
        {
            if (logging) Console.WriteLine("  DrawSimple {");
            graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Materials.FogColor, 1, 0);
            DrawSequences(graphicsDevice, logging);
            if (logging) Console.WriteLine("  }");
        }

        void DrawSequences(GraphicsDevice graphicsDevice, bool logging)
        {
            if (RenderProcess.Viewer.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0))
            {
                Materials.SceneryShader.SetShadowMap(ShadowMapLightViewProjShadowProj, ShadowMap, RenderProcess.ShadowMapLimit);
            }

            var renderItems = new List<RenderItem>();
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                if (logging) Console.WriteLine("    {0} {{", (RenderPrimitiveSequence)i);
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;
                    if (sequenceMaterial.Key == DummyBlendedMaterial)
                    {
                        // Blended: multiple materials, group by material as much as possible without destroying ordering.
                        Material lastMaterial = null;
                        foreach (var renderItem in sequenceMaterial.Value)
                        {
                            if (lastMaterial != renderItem.Material)
                            {
                                if (renderItems.Count > 0)
                                {
                                    if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, lastMaterial);
                                    lastMaterial.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
                                    renderItems.Clear();
                                }
                                if (lastMaterial != null)
                                    lastMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
								if (lastMaterial != null)
									DebugRenderState(graphicsDevice.RenderState, lastMaterial.ToString());
#endif
                                renderItem.Material.SetState(graphicsDevice, lastMaterial);
                                lastMaterial = renderItem.Material;
                            }
                            renderItems.Add(renderItem);
                        }
                        if (renderItems.Count > 0)
                        {
                            if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, lastMaterial);
                            lastMaterial.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
                            renderItems.Clear();
                        }
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
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNAViewMatrix, ref XNAProjectionMatrix);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
						DebugRenderState(graphicsDevice.RenderState, sequenceMaterial.Key.ToString());
#endif
                    }
                }
                if (logging) Console.WriteLine("    }");
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
