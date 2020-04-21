// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using ORTS.Common.Input;
using Game = Orts.Viewer3D.Processes.Game;

namespace Orts.Viewer3D
{
    public enum RenderPrimitiveSequence
    {
        CabOpaque,
        Sky,
        WorldOpaque,
        WorldBlended,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        InteriorOpaque,
        InteriorBlended,
        Labels,
        CabBlended,
        OverlayOpaque,
        OverlayBlended,
        // This value must be last.
        Sentinel
    }

    public enum RenderPrimitiveGroup
    {
        Cab,
        Sky,
        World,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        Interior,
        Labels,
        Overlay
    }

    public abstract class RenderPrimitive
    {
        /// <summary>
        /// Mapping from <see cref="RenderPrimitiveGroup"/> to <see cref="RenderPrimitiveSequence"/> for blended
        /// materials. The number of items in the array must equal the number of values in
        /// <see cref="RenderPrimitiveGroup"/>.
        /// </summary>
        public static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
			RenderPrimitiveSequence.CabBlended,
            RenderPrimitiveSequence.Sky,
			RenderPrimitiveSequence.WorldBlended,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorBlended,
            RenderPrimitiveSequence.Labels,
			RenderPrimitiveSequence.OverlayBlended,
		};

        /// <summary>
        /// Mapping from <see cref="RenderPrimitiveGroup"/> to <see cref="RenderPrimitiveSequence"/> for opaque
        /// materials. The number of items in the array must equal the number of values in
        /// <see cref="RenderPrimitiveGroup"/>.
        /// </summary>
        public static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
			RenderPrimitiveSequence.CabOpaque,
            RenderPrimitiveSequence.Sky,
			RenderPrimitiveSequence.WorldOpaque,
			RenderPrimitiveSequence.Lights,
			RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorOpaque,
            RenderPrimitiveSequence.Labels,
			RenderPrimitiveSequence.OverlayOpaque,
		};

        protected static VertexBuffer DummyVertexBuffer;
        protected static readonly VertexDeclaration DummyVertexDeclaration = new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements);
        protected static readonly Matrix[] DummyVertexData = new Matrix[] { Matrix.Identity };

        /// <summary>
        /// This is an adjustment for the depth buffer calculation which may be used to reduce the chance of co-planar primitives from fighting each other.
        /// </summary>
        // TODO: Does this actually make any real difference?
        public float ZBias;

        /// <summary>
        /// This is a sorting adjustment for primitives with similar/the same world location. Primitives with higher SortIndex values are rendered after others. Has no effect on non-blended primitives.
        /// </summary>
        public float SortIndex;

        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public abstract void Draw(GraphicsDevice graphicsDevice);
    }

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;

        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags)
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
                // For unknown reasons, this would crash with an ArgumentException (saying Compare(x, x) != 0)
                // sometimes when calculated as two values and subtracted. Presumed cause is floating point.
                var xd = (x.XNAMatrix.Translation - XNAViewerPos).Length();
                var yd = (y.XNAMatrix.Translation - XNAViewerPos).Length();
                if (x.Material is WaterMaterial && y.Material is WaterMaterial && Math.Abs(yd - xd) < 1.0 && x.XNAMatrix.Translation.Y < XNAViewerPos.Y)
                {
                    return Math.Sign(x.XNAMatrix.Translation.Y - y.XNAMatrix.Translation.Y);
                }
                // If the absolute difference is >= 1mm use that; otherwise, they're effectively in the same
                // place so fall back to the SortIndex.
                if (Math.Abs(yd - xd) >= 0.001)
                    return Math.Sign(yd - xd);
                return Math.Sign(x.RenderPrimitive.SortIndex - y.RenderPrimitive.SortIndex);
            }

            #endregion
        }
    }

	public class RenderItemCollection : IList<RenderItem>, IEnumerator<RenderItem>
	{
		RenderItem[] Items = new RenderItem[4];
		int ItemCount;
		int EnumeratorIndex;

		public RenderItemCollection()
		{
		}

        public int Capacity
        {
            get
            {
                return Items.Length;
            }
        }

		public int Count
		{
            get
            {
                return ItemCount;
            }
		}

		public void Sort(IComparer<RenderItem> comparer)
		{
			Array.Sort(Items, 0, ItemCount, comparer);
		}

		#region IList<RenderItem> Members

		public int IndexOf(RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void Insert(int index, RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
            throw new NotSupportedException();
		}

		public RenderItem this[int index]
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
                throw new NotSupportedException();
			}
		}

		#endregion

		#region ICollection<RenderItem> Members

		public void Add(RenderItem item)
		{
			if (ItemCount == Items.Length)
			{
				var items = new RenderItem[Items.Length * 2];
				Array.Copy(Items, 0, items, 0, Items.Length);
                Items = items;
			}
			Items[ItemCount] = item;
			ItemCount++;
		}

		public void Clear()
		{
			Array.Clear(Items, 0, ItemCount);
			ItemCount = 0;
		}

		public bool Contains(RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void CopyTo(RenderItem[] array, int arrayIndex)
		{
            throw new NotSupportedException();
		}

		int ICollection<RenderItem>.Count
		{
            get
            {
                throw new NotSupportedException();
            }
		}

		public bool IsReadOnly
		{
            get
            {
                throw new NotSupportedException();
            }
		}

		public bool Remove(RenderItem item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<RenderItem> Members

		public IEnumerator<RenderItem> GetEnumerator()
		{
			Reset();
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
                return Items[EnumeratorIndex];
            }
		}

		#endregion

		#region IEnumerator Members

		object System.Collections.IEnumerator.Current
		{
            get
            {
                return Current;
            }
		}

		public bool MoveNext()
		{
			EnumeratorIndex++;
			return EnumeratorIndex < ItemCount;
		}

		public void Reset()
		{
			EnumeratorIndex = -1;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			// No op.
		}

		#endregion
    }

    public class RenderFrame
    {
        readonly Game Game;

        // Shared shadow map data.
        static RenderTarget2D[] ShadowMap;
        static RenderTarget2D[] ShadowMapRenderTarget;
        static Vector3 SteppedSolarDirection = Vector3.UnitX;

        // Local shadow map data.
        Matrix[] ShadowMapLightView;
        Matrix[] ShadowMapLightProj;
        Matrix[] ShadowMapLightViewProjShadowProj;
        Vector3 ShadowMapX;
        Vector3 ShadowMapY;
        Vector3[] ShadowMapCenter;

        readonly Material DummyBlendedMaterial;
		readonly Dictionary<Material, RenderItemCollection>[] RenderItems = new Dictionary<Material, RenderItemCollection>[(int)RenderPrimitiveSequence.Sentinel];
        readonly RenderItemCollection[] RenderShadowSceneryItems;
        readonly RenderItemCollection[] RenderShadowForestItems;
        readonly RenderItemCollection[] RenderShadowTerrainItems;
        readonly RenderItemCollection RenderItemsSequence = new RenderItemCollection();

        public bool IsScreenChanged { get; internal set; }
        ShadowMapMaterial ShadowMapMaterial;
        SceneryShader SceneryShader;
        Vector3 SolarDirection;
        Camera Camera;
        Vector3 CameraLocation;
        Vector3 XNACameraLocation;
        Matrix XNACameraView;
        Matrix XNACameraProjection;

        public RenderFrame(Game game)
        {
            Game = game;
            DummyBlendedMaterial = new EmptyMaterial(null);

            for (int i = 0; i < RenderItems.Length; i++)
				RenderItems[i] = new Dictionary<Material, RenderItemCollection>();

            if (Game.Settings.DynamicShadows)
            {
                if (ShadowMap == null)
                {
                    var shadowMapSize = Game.Settings.ShadowMapResolution;
                    ShadowMap = new RenderTarget2D[RenderProcess.ShadowMapCount];
                    ShadowMapRenderTarget = new RenderTarget2D[RenderProcess.ShadowMapCount];
                    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    {
                        ShadowMapRenderTarget[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
                        ShadowMap[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
                    }
                }

                ShadowMapLightView = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightViewProjShadowProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapCenter = new Vector3[RenderProcess.ShadowMapCount];

                RenderShadowSceneryItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowForestItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowTerrainItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowForestItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowTerrainItems[shadowMapIndex] = new RenderItemCollection();
                }
            }

            XNACameraView = Matrix.Identity;
            XNACameraProjection = Matrix.CreateOrthographic(game.RenderProcess.DisplaySize.X, game.RenderProcess.DisplaySize.Y, 1, 100);
        }

        public void Clear()
        {
            // Attempt to clean up unused materials over time (max 1 per RenderPrimitiveSequence).
            for (var i = 0; i < RenderItems.Length; i++)
            {
                foreach (var mat in RenderItems[i].Keys)
                {
                    if (RenderItems[i][mat].Count == 0)
                    {
                        RenderItems[i].Remove(mat);
                        break;
                    }
                }
            }
            
            // Clear out (reset) all of the RenderItem lists.
            for (var i = 0; i < RenderItems.Length; i++)
                foreach (var mat in RenderItems[i].Keys)
                    RenderItems[i][mat].Clear();

            // Clear out (reset) all of the shadow mapping RenderItem lists.
            if (Game.Settings.DynamicShadows)
            {
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex].Clear();
                    RenderShadowForestItems[shadowMapIndex].Clear();
                    RenderShadowTerrainItems[shadowMapIndex].Clear();
                }
            }
        }

        public void PrepareFrame(Viewer viewer)
        {
            if (viewer.Settings.UseMSTSEnv == false)
                SolarDirection = viewer.World.Sky.solarDirection;
            else
                SolarDirection = viewer.World.MSTSSky.mstsskysolarDirection;

            if (ShadowMapMaterial == null)
                ShadowMapMaterial = (ShadowMapMaterial)viewer.MaterialManager.Load("ShadowMap");
            if (SceneryShader == null)
                SceneryShader = viewer.MaterialManager.SceneryShader;
        }

        public void SetCamera(Camera camera)
        {
            Camera = camera;
            XNACameraLocation = CameraLocation = Camera.Location;
            XNACameraLocation.Z *= -1;
            XNACameraView = Camera.XnaView;
            XNACameraProjection = Camera.XnaProjection;
        }

        static bool LockShadows;
        [CallOnThread("Updater")]
        public void PrepareFrame(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommand.DebugLockShadows))
                LockShadows = !LockShadows;

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && !LockShadows)
            {
                var solarDirection = SolarDirection;
                solarDirection.Normalize();
                if (Vector3.Dot(SteppedSolarDirection, solarDirection) < 0.99999)
                    SteppedSolarDirection = solarDirection;

                var cameraDirection = new Vector3(-XNACameraView.M13, -XNACameraView.M23, -XNACameraView.M33);
                cameraDirection.Normalize();

                var shadowMapAlignAxisX = Vector3.Cross(SteppedSolarDirection, Vector3.UnitY);
                var shadowMapAlignAxisY = Vector3.Cross(shadowMapAlignAxisX, SteppedSolarDirection);
                shadowMapAlignAxisX.Normalize();
                shadowMapAlignAxisY.Normalize();
                ShadowMapX = shadowMapAlignAxisX;
                ShadowMapY = shadowMapAlignAxisY;

                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    var viewingDistance = Game.Settings.ViewingDistance;
                    var shadowMapDiameter = RenderProcess.ShadowMapDiameter[shadowMapIndex];
                    var shadowMapLocation = XNACameraLocation + RenderProcess.ShadowMapDistance[shadowMapIndex] * cameraDirection;

                    // Align shadow map location to grid so it doesn't "flutter" so much. This basically means aligning it along a
                    // grid based on the size of a shadow texel (shadowMapSize / shadowMapSize) along the axes of the sun direction
                    // and up/left.
                    var shadowMapAlignmentGrid = (float)shadowMapDiameter / Game.Settings.ShadowMapResolution;
                    var shadowMapSize = Game.Settings.ShadowMapResolution;
                    var adjustX = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisX, shadowMapLocation), shadowMapAlignmentGrid);
                    var adjustY = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisY, shadowMapLocation), shadowMapAlignmentGrid);
                    shadowMapLocation.X -= shadowMapAlignAxisX.X * adjustX;
                    shadowMapLocation.Y -= shadowMapAlignAxisX.Y * adjustX;
                    shadowMapLocation.Z -= shadowMapAlignAxisX.Z * adjustX;
                    shadowMapLocation.X -= shadowMapAlignAxisY.X * adjustY;
                    shadowMapLocation.Y -= shadowMapAlignAxisY.Y * adjustY;
                    shadowMapLocation.Z -= shadowMapAlignAxisY.Z * adjustY;

                    ShadowMapLightView[shadowMapIndex] = Matrix.CreateLookAt(shadowMapLocation + viewingDistance * SteppedSolarDirection, shadowMapLocation, Vector3.Up);
                    ShadowMapLightProj[shadowMapIndex] = Matrix.CreateOrthographic(shadowMapDiameter, shadowMapDiameter, 0, viewingDistance + shadowMapDiameter / 2);
                    ShadowMapLightViewProjShadowProj[shadowMapIndex] = ShadowMapLightView[shadowMapIndex] * ShadowMapLightProj[shadowMapIndex] * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / shadowMapSize, 0.5f + 0.5f / shadowMapSize, 0, 1);
                    ShadowMapCenter[shadowMapIndex] = shadowMapLocation;
                }
            }
        }

        /// <summary>
        /// Automatically adds or culls a <see cref="RenderPrimitive"/> based on a location, radius and max viewing distance.
        /// </summary>
        /// <param name="mstsLocation">Center location of the <see cref="RenderPrimitive"/> in MSTS coordinates.</param>
        /// <param name="objectRadius">Radius of a sphere containing the whole <see cref="RenderPrimitive"/>, centered on <paramref name="mstsLocation"/>.</param>
        /// <param name="objectViewingDistance">Maximum distance from which the <see cref="RenderPrimitive"/> should be viewable.</param>
        /// <param name="material"></param>
        /// <param name="primitive"></param>
        /// <param name="group"></param>
        /// <param name="xnaMatrix"></param>
        /// <param name="flags"></param>
        [CallOnThread("Updater")]
        public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (float.IsPositiveInfinity(objectViewingDistance) || (Camera != null && Camera.InRange(mstsLocation, objectRadius, objectViewingDistance)))
            {
                if (Camera != null && Camera.InFov(mstsLocation, objectRadius))
                    AddPrimitive(material, primitive, group, ref xnaMatrix, flags);
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ((flags & ShapeFlags.ShadowCaster) != 0))
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    if (IsInShadowMap(shadowMapIndex, mstsLocation, objectRadius, objectViewingDistance))
                        AddShadowPrimitive(shadowMapIndex, material, primitive, ref xnaMatrix, flags);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None);
        }

        static readonly bool[] PrimitiveBlendedScenery = new bool[] { true, false }; // Search for opaque pixels in alpha blended primitives, thus maintaining correct DepthBuffer
        static readonly bool[] PrimitiveBlended = new bool[] { true };
        static readonly bool[] PrimitiveNotBlended = new bool[] { false };

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            var getBlending = material.GetBlending();
            var blending = getBlending && material is SceneryMaterial ? PrimitiveBlendedScenery : getBlending ? PrimitiveBlended : PrimitiveNotBlended;

            RenderItemCollection items;
            foreach (var blended in blending)
            {
                var sortingMaterial = blended ? DummyBlendedMaterial : material;
                var sequence = RenderItems[(int)GetRenderSequence(group, blended)];

                if (!sequence.TryGetValue(sortingMaterial, out items))
                {
                    items = new RenderItemCollection();
                    sequence.Add(sortingMaterial, items);
                }
                items.Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            }
            if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
                primitive.ZBias = 1;
        }

        [CallOnThread("Updater")]
        void AddShadowPrimitive(int shadowMapIndex, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (material is SceneryMaterial)
                RenderShadowSceneryItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is ForestMaterial)
                RenderShadowForestItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is TerrainMaterial)
                RenderShadowTerrainItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else
                Debug.Fail("Only scenery, forest and terrain materials allowed in shadow map.");
        }

        [CallOnThread("Updater")]
        public void Sort()
        {
            var renderItemComparer = new RenderItem.Comparer(CameraLocation);
            foreach (var sequence in RenderItems)
            {
                foreach (var sequenceMaterial in sequence.Where(kvp => kvp.Value.Count > 0))
                {
                    if (sequenceMaterial.Key != DummyBlendedMaterial)
                        continue;
                    sequenceMaterial.Value.Sort(renderItemComparer);
                }
            }
        }

        bool IsInShadowMap(int shadowMapIndex, Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
        {
            if (ShadowMapRenderTarget == null)
                return false;

            mstsLocation.Z *= -1;
            mstsLocation.X -= ShadowMapCenter[shadowMapIndex].X;
            mstsLocation.Y -= ShadowMapCenter[shadowMapIndex].Y;
            mstsLocation.Z -= ShadowMapCenter[shadowMapIndex].Z;
            objectRadius += RenderProcess.ShadowMapDiameter[shadowMapIndex] / 2;

            // Check if object is inside the sphere.
            var length = mstsLocation.LengthSquared();
            if (length <= objectRadius * objectRadius)
                return true;

            // Check if object is inside cylinder.
            var dotX = Math.Abs(Vector3.Dot(mstsLocation, ShadowMapX));
            if (dotX > objectRadius)
                return false;

            var dotY = Math.Abs(Vector3.Dot(mstsLocation, ShadowMapY));
            if (dotY > objectRadius)
                return false;

            // Check if object is on correct side of center.
            var dotZ = Vector3.Dot(mstsLocation, SteppedSolarDirection);
            if (dotZ < 0)
                return false;

            return true;
        }

        static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
        {
            if (blended)
                return RenderPrimitive.SequenceForBlended[(int)group];
            return RenderPrimitive.SequenceForOpaque[(int)group];
        }

        [CallOnThread("Render")]
        public void Draw(GraphicsDevice graphicsDevice)
        {
            var logging = UserInput.IsPressed(UserCommand.DebugLogRenderFrame);
            if (logging)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Draw {");
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ShadowMapMaterial != null)
                DrawShadows(graphicsDevice, logging);

            DrawSimple(graphicsDevice, logging);

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
                Game.RenderProcess.PrimitiveCount[i] = RenderItems[i].Values.Sum(l => l.Count);

            if (logging)
            {
                Console.WriteLine("}");
                Console.WriteLine();
            }
        }

        void DrawShadows( GraphicsDevice graphicsDevice, bool logging )
        {
            if (logging) Console.WriteLine("  DrawShadows {");
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                DrawShadows(graphicsDevice, logging, shadowMapIndex);
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                Game.RenderProcess.ShadowPrimitiveCount[shadowMapIndex] = RenderShadowSceneryItems[shadowMapIndex].Count + RenderShadowForestItems[shadowMapIndex].Count + RenderShadowTerrainItems[shadowMapIndex].Count;
            if (logging) Console.WriteLine("  }");
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging, int shadowMapIndex)
        {
            if (logging) Console.WriteLine("    {0} {{", shadowMapIndex);

            // Prepare renderer for drawing the shadow map.
            graphicsDevice.SetRenderTarget(ShadowMapRenderTarget[shadowMapIndex]);
            graphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.White, 1, 0);

            // Prepare for normal (non-blocking) rendering of scenery.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Render non-terrain, non-forest shadow items first.
            if (logging) Console.WriteLine("      {0,-5} * SceneryMaterial (normal)", RenderShadowSceneryItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowSceneryItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for normal (non-blocking) rendering of forests.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Forest);

            // Render forest shadow items next.
            if (logging) Console.WriteLine("      {0,-5} * ForestMaterial (forest)", RenderShadowForestItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowForestItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for normal (non-blocking) rendering of terrain.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Render terrain shadow items now, with their magic.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (normal)", RenderShadowTerrainItems[shadowMapIndex].Count);
            graphicsDevice.Indices = TerrainPrimitive.SharedPatchIndexBuffer;
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare for blocking rendering of terrain.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Blocker);

            // Render terrain shadow items in blocking mode.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (blocker)", RenderShadowTerrainItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // All done.
            ShadowMapMaterial.ResetState(graphicsDevice);
            graphicsDevice.SetRenderTarget(null);

            // Blur the shadow map.
            if (Game.Settings.ShadowMapBlur)
            {
				ShadowMap[shadowMapIndex] = ShadowMapMaterial.ApplyBlur(graphicsDevice, ShadowMap[shadowMapIndex], ShadowMapRenderTarget[shadowMapIndex]);
            }
            else
                ShadowMap[shadowMapIndex] = ShadowMapRenderTarget[shadowMapIndex];

            if (logging) Console.WriteLine("    }");
        }

        /// <summary>
        /// Executed in the RenderProcess thread - simple draw
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="logging"></param>
        void DrawSimple(GraphicsDevice graphicsDevice, bool logging)
        {
            if (Game.Settings.DistantMountains)
            {
                if (logging) Console.WriteLine("  DrawSimple (Distant Mountains) {");
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                DrawSequencesDistantMountains(graphicsDevice, logging);
                if (logging) Console.WriteLine("  }");
                if (logging) Console.WriteLine("  DrawSimple {");
                graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);
                DrawSequences(graphicsDevice, logging);
                if (logging) Console.WriteLine("  }");
            }
            else
            {
                if (logging) Console.WriteLine("  DrawSimple {");
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                DrawSequences(graphicsDevice, logging);
                if (logging) Console.WriteLine("  }");
            }
        }

        void DrawSequences(GraphicsDevice graphicsDevice, bool logging)
        {
            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
                SceneryShader.SetShadowMap(ShadowMapLightViewProjShadowProj, ShadowMap, RenderProcess.ShadowMapLimit);

            var renderItems = RenderItemsSequence;
            renderItems.Clear();
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
                                    lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                                    renderItems.Clear();
                                }
                                if (lastMaterial != null)
                                    lastMaterial.ResetState(graphicsDevice);
                                renderItem.Material.SetState(graphicsDevice, lastMaterial);
                                lastMaterial = renderItem.Material;
                            }
                            renderItems.Add(renderItem);
                        }
                        if (renderItems.Count > 0)
                        {
                            if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, lastMaterial);
                            lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                            renderItems.Clear();
                        }
                        if (lastMaterial != null)
                            lastMaterial.ResetState(graphicsDevice);
                    }
                    else
                    {
                        if (Game.Settings.DistantMountains && (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial
                            || sequenceMaterial.Key is MSTSSkyMaterial))
                            continue;
                        // Opaque: single material, render in one go.
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref XNACameraProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
                    }
                }
                if (logging) Console.WriteLine("    }");
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
                SceneryShader.ClearShadowMap();
        }

        void DrawSequencesDistantMountains(GraphicsDevice graphicsDevice, bool logging)
        {
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                if (logging) Console.WriteLine("    {0} {{", (RenderPrimitiveSequence)i);
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;
                    if (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial || sequenceMaterial.Key is MSTSSkyMaterial)
                    {
                        // Opaque: single material, render in one go.
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref Camera.XnaDistantMountainProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
                    }
                }
                if (logging) Console.WriteLine("    }");
            }
        }
    }
}
