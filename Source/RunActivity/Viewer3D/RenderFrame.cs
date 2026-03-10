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

// Define this to check every material is resetting the RenderState correctly.
// #define DEBUG_RENDER_STATE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

    public enum LightMode
    {
        Directional = 0,
        Point = 1,
        Spot = 2,
        Headlight = 3
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

        // We are required to provide all necessary data for the shader code. To avoid needing to split it up into instanced and non-instanced versions, we provide this dummy vertex buffer instead of the instance buffer where needed.
        static VertexBuffer DummyVertexBuffer;
        static internal VertexBuffer GetDummyVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (DummyVertexBuffer == null)
            {
                var vertexBuffer = new VertexBuffer(graphicsDevice, new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements), 1, BufferUsage.WriteOnly);
                vertexBuffer.SetData(new Matrix[] { Matrix.Identity });
                vertexBuffer.Name = "INSTANCE_DUMMY";
                DummyVertexBuffer = vertexBuffer;
            }
            return DummyVertexBuffer;
        }

        public StaticLight AttachedLight { get; protected set; }
    }

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;
        public object ItemData;

        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags, object itemData = null)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
            ItemData = itemData;
        }

        public class Comparer : IComparer<RenderItem>
        {
            Vector3 XNAViewerPos;

            public Comparer(Vector3 viewerPos)
            {
                SetViewerPosition(viewerPos);
            }

            public void SetViewerPosition(Vector3 viewerPos)
            {
                XNAViewerPos = viewerPos;
                XNAViewerPos.Z *= -1;
            }

            #region IComparer<RenderItem> Members

            public int Compare(RenderItem x, RenderItem y)
            {
                // For unknown reasons, this would crash with an ArgumentException (saying Compare(x, x) != 0)
                // sometimes when calculated as two values and subtracted. Presumed cause is floating point.
                var xd = (x.XNAMatrix.Translation - XNAViewerPos).LengthSquared();
                var yd = (y.XNAMatrix.Translation - XNAViewerPos).LengthSquared();
                var diff = yd - xd;
                // The following avoids water levels flashing, by forcing that higher water levels are nearer to the
                // camera, which is always true except when camera is under water level, which is quite abnormal
                if (Math.Abs(diff) < 1.0 && x.Material is WaterMaterial && y.Material is WaterMaterial && x.XNAMatrix.Translation.Y < XNAViewerPos.Y)
                {
                    return Math.Sign(x.XNAMatrix.Translation.Y - y.XNAMatrix.Translation.Y);
                }
                // If the absolute difference is >= 1mm use that; otherwise, they're effectively in the same
                // place so fall back to the SortIndex.
                if (Math.Abs(diff) >= 0.000001)
                    return diff > 0 ? 1 : -1;
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
                if (index < 0 || index >= ItemCount)
                    throw new IndexOutOfRangeException();

                return Items[index];
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
        static RenderTarget2D ShadowMap;
        static RenderTarget2D ShadowMapRenderTarget;
        static Vector3 SteppedSolarDirection = Vector3.UnitX;
        static readonly Vector3 SunColor = Vector3.One;
        static readonly Vector3 MoonGlow = new Vector3(245f / 255f, 243f / 255f, 206f / 255f);
        const float SunIntensity = 1;
        const float MoonIntensity = SunIntensity / 380000;
        //public const float HeadLightIntensity = 250000; // See some sample values: https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@5.2/manual/LightUnits.html
        public const float HeadLightIntensity = 4; // Using the old linear attenuation model

        /// <summary>
        /// In the order of <see cref="LightMode"/>, by visual inspection of PlaysetLightTest at nighttime. For Point probably should be 1 / 4π = 0.08
        /// </summary>
        static readonly float[] LightIntensityAdjustment = new float[] { 1, 0.08f, 1, 1 };
        
        float LightDayNightClampTo = 1;
        float LightDayNightMultiplier = 1;

        // Local shadow map data.
        Matrix[] ShadowMapLightView;
        Matrix[] ShadowMapLightProj;
        Matrix[] ShadowMapLightViewProjShadowProj;
        Vector3 ShadowMapX;
        Vector3 ShadowMapY;
        Vector3[] ShadowMapCenter;

        internal RenderTarget2D RenderSurface;
        SpriteBatchMaterial RenderSurfaceMaterial;

        readonly RenderItemCollection[][] RenderItems = new RenderItemCollection[(int)RenderPrimitiveSequence.Sentinel][];
        readonly ulong[][] RenderItemKeys = new ulong[(int)RenderPrimitiveSequence.Sentinel][];
        readonly int[] RenderItemCount = new int[(int)RenderPrimitiveSequence.Sentinel];

        readonly RenderItemCollection[] RenderShadowSceneryItems;
        readonly RenderItemCollection[] RenderShadowPbrNormalMapItems;
        readonly RenderItemCollection[] RenderShadowPbrSkinnedItems;
        readonly RenderItemCollection[] RenderShadowPbrMorphedItems;
        readonly RenderItemCollection[] RenderShadowForestItems;
        readonly RenderItemCollection[] RenderShadowTerrainItems;
        const ulong BlendedKey = ulong.MaxValue;
        static RenderItem.Comparer RenderItemComparer;

        static readonly Func<Material, bool> SkyDM = material => material is TerrainSharedDistantMountain || material is SkyMaterial || material is MSTSSkyMaterial;
        static readonly Func<Material, bool> NonSkyDM = material => !(material is TerrainSharedDistantMountain || material is SkyMaterial || material is MSTSSkyMaterial);

        public int NumLights;
        Texture2D LightsTexture;
        LightData[] Lights = new LightData[RenderProcess.MaxLights];

        public bool IsScreenChanged { get; internal set; }
        ShadowMapMaterial ShadowMapMaterial;
        SceneryShader SceneryShader;
        ShadowMapShader ShadowMapShader;
        Vector3 SolarDirection;
        Camera Camera;
        Vector3 CameraLocation;
        Vector3 XNACameraLocation;
        Matrix XNACameraView;
        Matrix XNACameraProjection;

        public RenderFrame(Game game)
        {
            Game = game;

            for (int i = 0; i < RenderItems.Length; i++)
            {
                RenderItemKeys[i] = new ulong[2];
                RenderItems[i] = new RenderItemCollection[2];
            }

            if (Game.Settings.DynamicShadows)
            {
                var shadowMapSize = Game.Settings.ShadowMapResolution;

                ShadowMap = ShadowMap ?? new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize,
                    false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents, false, RenderProcess.ShadowMapCount);
                ShadowMapRenderTarget = ShadowMapRenderTarget ?? (!Game.Settings.ShadowMapBlur ? ShadowMap :
                    new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize,
                    false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents, false, RenderProcess.ShadowMapCount));

                ShadowMapLightView = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightViewProjShadowProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapCenter = new Vector3[RenderProcess.ShadowMapCount];

                RenderShadowSceneryItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowPbrNormalMapItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowPbrSkinnedItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowPbrMorphedItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowForestItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowTerrainItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowPbrNormalMapItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowPbrSkinnedItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowPbrMorphedItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowForestItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowTerrainItems[shadowMapIndex] = new RenderItemCollection();
                }
            }

            XNACameraView = Matrix.Identity;
            XNACameraProjection = Matrix.CreateOrthographic(game.RenderProcess.DisplaySize.X, game.RenderProcess.DisplaySize.Y, 1, 100);

            SetLightsTexture();

            ScreenChanged();
        }

        void ScreenChanged()
        {
            RenderSurface = new RenderTarget2D(
                Game.RenderProcess.GraphicsDevice,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferFormat,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.MultiSampleCount,
                RenderTargetUsage.PreserveContents
            );
        }

        public void Clear()
        {
            // Clear out (reset) all of the RenderItem lists.
            for (var i = 0; i < RenderItems.Length; i++)
            {
                for (var j = 0; j < RenderItemCount[i]; j++)
                    RenderItems[i][j].Clear();

                RenderItemCount[i] = 0;
            }

            // Clear out (reset) all of the shadow mapping RenderItem lists.
            if (Game.Settings.DynamicShadows)
            {
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex].Clear();
                    RenderShadowPbrNormalMapItems[shadowMapIndex].Clear();
                    RenderShadowPbrSkinnedItems[shadowMapIndex].Clear();
                    RenderShadowPbrMorphedItems[shadowMapIndex].Clear();
                    RenderShadowForestItems[shadowMapIndex].Clear();
                    RenderShadowTerrainItems[shadowMapIndex].Clear();
                }
            }

            NumLights = 0;
        }

        public void PrepareFrame(Viewer viewer)
        {
            if (RenderSurfaceMaterial == null)
                RenderSurfaceMaterial = new SpriteBatchMaterial(viewer, BlendState.Opaque);

            if (viewer.Settings.UseMSTSEnv == false)
                SolarDirection = viewer.World.Sky.SolarDirection;
            else
                SolarDirection = viewer.World.MSTSSky.mstsskysolarDirection;

            if (ShadowMapMaterial == null)
                ShadowMapMaterial = (ShadowMapMaterial)viewer.MaterialManager.Load("ShadowMap");
            if (SceneryShader == null)
                SceneryShader = viewer.MaterialManager.SceneryShader;
            if (ShadowMapShader == null)
                ShadowMapShader = viewer.MaterialManager.ShadowMapShader;

            // Ensure that the first light is always the sun/moon, because the ambient and shadow effects will be calculated based on the first light.
            if (SolarDirection.Y > -0.05)
            {
                AddLight(LightMode.Directional, Vector3.Zero, SolarDirection, SunColor, SunIntensity, 0, 0, 0, 1, true);
            }
            else
            {
                var moonDirection = viewer.Settings.UseMSTSEnv ? viewer.World.MSTSSky.mstsskylunarDirection : viewer.World.Sky.LunarDirection;
                AddLight(LightMode.Directional, Vector3.Zero, moonDirection, MoonGlow, MoonIntensity, 0, 0, 0, 1, true);
            }

            if (SolarDirection.Y <= -0.05)
            {
                LightDayNightClampTo = 1; // at nighttime max light
                LightDayNightMultiplier = 1;
            }
            else if (SolarDirection.Y >= 0.15)
            {
                LightDayNightClampTo = 0.5f; // at daytime min light
                LightDayNightMultiplier = 0.1f;
            }
            else
            {
                LightDayNightClampTo = 1 - 2.5f * (SolarDirection.Y + 0.05f); // in the meantime interpolate
                LightDayNightMultiplier = 1 - 4.5f * (SolarDirection.Y + 0.05f);
            }
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
            if (xnaMatrix == new Matrix()) // invisible object
                return;

            if (float.IsPositiveInfinity(objectViewingDistance) || (Camera != null && Camera.InRange(mstsLocation, objectRadius, objectViewingDistance)))
            {
                if (Camera != null && Camera.InFov(mstsLocation, objectRadius))
                {
                    AddPrimitive(material, primitive, group, ref xnaMatrix, flags);

                    if (primitive?.AttachedLight != null && primitive.AttachedLight.Range > 0 && Camera.InRange(mstsLocation, objectRadius, primitive.AttachedLight.Range))
                    {
                        primitive.AttachedLight.WorldMatrix = xnaMatrix;
                        AddLight(primitive.AttachedLight, false);
                    }
                }
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ((flags & ShapeFlags.ShadowCaster) != 0))
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    if (IsInShadowMap(shadowMapIndex, mstsLocation, objectRadius, objectViewingDistance))
                        AddShadowPrimitive(shadowMapIndex, material, primitive, ref xnaMatrix, flags);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None, null);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, flags, null);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags, object itemData)
        {
            if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
                primitive.ZBias = 1;

            var blending = material.GetBlending();
            var sortingKey = blending ? BlendedKey : material.SortingKey;
            getSequence(blending, sortingKey).Add(new RenderItem(material, primitive, ref xnaMatrix, flags, itemData));

            // SceneryMaterial primitives may contain both opaque and blended/transparent parts, put these into both sequences
            if (blending && material is SceneryMaterial)
                getSequence(false, material.SortingKey).Add(new RenderItem(material, primitive, ref xnaMatrix, flags, itemData));

            RenderItemCollection getSequence(bool blended, ulong key)
            {
                var s = (int)(blended ? RenderPrimitive.SequenceForBlended[(int)group] : RenderPrimitive.SequenceForOpaque[(int)group]);

                var index = Array.IndexOf(RenderItemKeys[s], key, 0, RenderItemCount[s]);
                if (index == -1)
                    index = RenderItemCount[s]++;

                if (RenderItemKeys[s].Length <= index)
                {
                    Array.Resize(ref RenderItemKeys[s], RenderItemCount[s] * 2);
                    Array.Resize(ref RenderItems[s], RenderItemCount[s] * 2);
                }
                RenderItemKeys[s][index] = key;
                RenderItems[s][index] = RenderItems[s][index] ?? new RenderItemCollection();
                return RenderItems[s][index];
            }
        }

        [CallOnThread("Updater")]
        void AddShadowPrimitive(int shadowMapIndex, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (material is PbrMaterial morphedMaterial && (morphedMaterial.Options & SceneryMaterialOptions.PbrHasMorphTargets) != 0)
                RenderShadowPbrMorphedItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is PbrMaterial skinnedMaterial && (skinnedMaterial.Options & SceneryMaterialOptions.PbrHasSkin) != 0)
                RenderShadowPbrSkinnedItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is PbrMaterial pbrMaterial && (pbrMaterial.Options & SceneryMaterialOptions.PbrHasTexCoord1) != 0)
                RenderShadowPbrNormalMapItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is SceneryMaterial)
                RenderShadowSceneryItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is ForestMaterial)
                RenderShadowForestItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is TerrainMaterial)
                RenderShadowTerrainItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (!(material is EmptyMaterial))
                Debug.Fail("Only scenery, forest and terrain materials allowed in shadow map.");
        }

        /// <summary>
        /// Z-sort the blended/transparent primitives for correct rendering.
        /// </summary>
        [CallOnThread("Updater")]
        public void Sort()
        {
            if (RenderItemComparer == null)
                RenderItemComparer = new RenderItem.Comparer(XNACameraLocation);
            else
                RenderItemComparer.SetViewerPosition(XNACameraLocation);

            for (var i = 0; i < RenderItems.Length; i++)
            {
                if (RenderItemKeys[i][0] == BlendedKey)
                {
                    RenderItems[i][0].Sort(RenderItemComparer);

                    // Blended: multiple materials sorted by depth, create render batches without destroying the ordering.
                    var sortingKey = ulong.MaxValue;
                    foreach (var renderItem in RenderItems[i][0])
                    {
                        if (sortingKey != renderItem.Material.SortingKey)
                        {
                            sortingKey = renderItem.Material.SortingKey;
                            if (RenderItems[i].Length <= RenderItemCount[i])
                                Array.Resize(ref RenderItems[i], RenderItemCount[i] * 2);
                            RenderItemCount[i]++;
                        }
                        RenderItems[i][RenderItemCount[i] - 1] = RenderItems[i][RenderItemCount[i] - 1] ?? new RenderItemCollection();
                        RenderItems[i][RenderItemCount[i] - 1].Add(renderItem);
                    }
                    RenderItems[i][0].Clear();
                }
                else
                {
                    Array.Sort(RenderItemKeys[i], RenderItems[i], 0, RenderItemCount[i]);
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

        [CallOnThread("Render")]
        public void Draw(GraphicsDevice graphicsDevice)
        {
            if (RenderSurface.Width != graphicsDevice.PresentationParameters.BackBufferWidth || RenderSurface.Height != graphicsDevice.PresentationParameters.BackBufferHeight)
                ScreenChanged();

#if DEBUG_RENDER_STATE
            DebugRenderState(graphicsDevice, "RenderFrame.Draw");
#endif
            var logging = UserInput.IsPressed(UserCommand.DebugLogRenderFrame);
            if (logging)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Draw {");
            }

            SetLights();

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ShadowMapMaterial != null)
                DrawShadows(graphicsDevice, logging);

            DrawSimple(graphicsDevice, logging);

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
                Game.RenderProcess.PrimitiveCount[i] = RenderItems[i].Take(RenderItemCount[i]).Sum(l => l?.Count ?? 0);

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
                Game.RenderProcess.ShadowPrimitiveCount[shadowMapIndex] =
                    RenderShadowSceneryItems[shadowMapIndex].Count + 
                    RenderShadowPbrNormalMapItems[shadowMapIndex].Count + 
                    RenderShadowPbrSkinnedItems[shadowMapIndex].Count + 
                    RenderShadowPbrMorphedItems[shadowMapIndex].Count + 
                    RenderShadowForestItems[shadowMapIndex].Count + 
                    RenderShadowTerrainItems[shadowMapIndex].Count;
            if (logging) Console.WriteLine("  }");
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging, int shadowMapIndex)
        {
            if (logging) Console.WriteLine("    {0} {{", shadowMapIndex);

            ShadowMapShader?.SetPerShadowMap(ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepare renderer for drawing the shadow map.
            graphicsDevice.SetRenderTarget(ShadowMapRenderTarget, shadowMapIndex);
            graphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.White, 1, 0);

            // Prepare for normal (non-blocking) rendering of scenery.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Render non-terrain, non-forest shadow items first.
            if (logging) Console.WriteLine("      {0,-5} * SceneryMaterial (normal)", RenderShadowSceneryItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowSceneryItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Pbr);
            if (logging) Console.WriteLine("      {0,-5} * PbrMaterialNormalMap (normal)", RenderShadowPbrNormalMapItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowPbrNormalMapItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.PbrSkinned);
            if (logging) Console.WriteLine("      {0,-5} * PbrMaterialSkinned (normal)", RenderShadowPbrSkinnedItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowPbrSkinnedItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.PbrMorphed);
            if (logging) Console.WriteLine("      {0,-5} * PbrMaterialSkinned (normal)", RenderShadowPbrMorphedItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowPbrMorphedItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

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
#if DEBUG_RENDER_STATE
            DebugRenderState(graphicsDevice, ShadowMapMaterial.ToString());
#endif
            graphicsDevice.SetRenderTarget(null);

            // Blur the shadow map.
            if (Game.Settings.ShadowMapBlur)
            {
				ShadowMapMaterial.ApplyBlur(graphicsDevice, ShadowMap, ShadowMapRenderTarget, shadowMapIndex);
#if DEBUG_RENDER_STATE
                DebugRenderState(graphicsDevice, ShadowMapMaterial.ToString() + " ApplyBlur()");
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
            if (RenderSurfaceMaterial != null)
            {
                graphicsDevice.SetRenderTarget(RenderSurface);
            }

            graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);

            if (Game.Settings.DistantMountains)
            {
                if (logging) Console.WriteLine("  DrawSimple (Distant Mountains) {");
                DrawSequences(graphicsDevice, ref Camera.XnaDistantMountainProjection, logging, excludeMaterial: NonSkyDM);
                if (logging) Console.WriteLine("  }");

                graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);
            }

            if (Game.Settings.DynamicShadows && RenderProcess.ShadowMapCount > 0)
                SceneryShader?.SetShadowMap(ShadowMapLightViewProjShadowProj, ShadowMap, RenderProcess.ShadowMapLimit);

            var excludeMaterial = Game.Settings.DistantMountains ? SkyDM : null;
            if (logging) Console.WriteLine("  DrawSimple {");
            DrawSequences(graphicsDevice, ref XNACameraProjection, logging, excludeMaterial);
            if (logging) Console.WriteLine("  }");

            if (Game.Settings.DynamicShadows && RenderProcess.ShadowMapCount > 0)
                SceneryShader?.ClearShadowMap();

            if (RenderSurfaceMaterial != null)
            {
                graphicsDevice.SetRenderTarget(null);
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                RenderSurfaceMaterial.SetState(graphicsDevice, null);
                RenderSurfaceMaterial.SpriteBatch.Draw(RenderSurface, Vector2.Zero, Color.White);
                RenderSurfaceMaterial.ResetState(graphicsDevice);
            }
        }

        void DrawSequences(GraphicsDevice graphicsDevice, ref Matrix projection, bool logging, Func<Material, bool> excludeMaterial)
        {
            SceneryShader?.SetPerFrame(ref XNACameraView, ref projection);
            
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                if (logging) Console.WriteLine("    {0} {{", (RenderPrimitiveSequence)i);
                var sequence = RenderItems[i];

                for (var j = 0; j < sequence.Length; j++)
                {
                    var renderItems = sequence[j];

                    if (renderItems == null || renderItems.Count == 0 ||
                        !(renderItems[0].Material is Material sequenceMaterial) || excludeMaterial != null && excludeMaterial(sequenceMaterial))
                        continue;

                    if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, sequenceMaterial);
#if DEBUG_RENDER_STATE
                    DebugRenderState(graphicsDevice, sequenceMaterial.ToString());
#endif
                    var transparentPass = !RenderPrimitive.SequenceForOpaque.Contains((RenderPrimitiveSequence)i) ? sequenceMaterial : null;

                    sequenceMaterial.SetState(graphicsDevice, transparentPass);
                    sequenceMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref projection);
                    sequenceMaterial.ResetState(graphicsDevice);
                }
                if (logging) Console.WriteLine("    }");
            }
        }

        /// <summary>
        /// Add a light to the actually compiled frame.
        /// </summary>
        public void AddLight(StaticLight light, bool ignoreDayNight)
        {
            // Do not allow directional light injection. That is reserved to the sun and the moon.
            if (light != null && light.Type != LightMode.Directional)
                AddLight(light.Type, light.WorldMatrix.Translation, Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, light.WorldMatrix)),
                    light.Color * light.ColorX,
                    light.Intensity,
                    light.Range * light.RangeX,
                    light.InnerConeAngle * light.InnerConeAngleX,
                    light.OuterConeAngle * light.OuterConeAngleX,
                    light.IntensityX, // Send this separately
                    ignoreDayNight);
        }

        /// <summary>
        /// Add a light to the actually compiled frame.
        /// </summary>
        /// <param name="type">Can be Point or Spot only. Use the Directional type only for the Sun and the Moon.</param>
        /// <param name="position">The worldMatrix.Translation</param>
        /// <param name="direction">The light direction</param>
        /// <param name="color">The light color</param>
        /// <param name="intensity">Luminous intensity in candela (lm/sr)</param>
        /// <param name="range">Cutoff distance</param>
        /// <param name="innerConeCos"></param>
        /// <param name="outerConeCos"></param>
        /// <param name="fade">Fading, 0 no light, 1 full light</param>
        /// <param name="ignoreDayNight">At daytime the intensity is automatically reduced to match the sunlight. Disable this by this parameter.</param>
        public void AddLight(LightMode type, Vector3 position, Vector3 direction, Vector3 color, float intensity, float range, float innerConeAngle, float outerConeAngle, float fade, bool ignoreDayNight)
        {
            if (intensity <= 0 || fade <= 0)
                return;

            if (NumLights >= Lights.Length)
                Array.Resize(ref Lights, Lights.Length * 2);

            intensity *= ignoreDayNight
                ? MathHelper.Clamp(fade, 0, 1)
                : MathHelper.Clamp(fade * LightDayNightMultiplier * LightDayNightClampTo, 0, LightDayNightClampTo);
            intensity *= LightIntensityAdjustment[(int)type];
            range *= ignoreDayNight ? 1 : LightDayNightMultiplier;

            Lights[NumLights++] = new LightData
            {
                Position = position,
                Direction = type == LightMode.Point ? Vector3.Zero : direction,
                ColorIntensity = color * intensity,
                RangeRcp = range == 0 ? float.MaxValue : 1f / range,
                OuterConeCos = (float)Math.Cos(outerConeAngle),
                InnerConeCos = type == LightMode.Headlight ? 1.0001f : type != LightMode.Spot ? -1 : (float)Math.Cos(innerConeAngle)
                // Light type is coded into this parameter. -1: non-spot type, 1.0001: headlight. Need to keep close to 1.0 not to ruin the smoothrange() in the shader.
            };
        }

        void SetLights()
        {
            if (SceneryShader == null)
                return;

            if (Lights.Length > RenderProcess.MaxLights)
            {
                RenderProcess.MaxLights = Lights.Length;
                SetLightsTexture();
            }
            LightsTexture.SetData(MemoryMarshal.Cast<LightData, Vector4>(Lights).ToArray());

            SceneryShader.NumLights = NumLights;
            SceneryShader.LightsTexture = LightsTexture;
        }

        void SetLightsTexture()
        {
            if (LightsTexture != null)
                LightsTexture.Dispose();

            LightsTexture = new Texture2D(Game.RenderProcess.GraphicsDevice, 3, RenderProcess.MaxLights, false, SurfaceFormat.Vector4);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LightData
        {
            public Vector3 Position;
            public float RangeRcp;
            public Vector3 Direction;
            public float InnerConeCos;
            public Vector3 ColorIntensity;
            public float OuterConeCos;
        }

#if DEBUG_RENDER_STATE
        static void DebugRenderState(GraphicsDevice graphicsDevice, string location)
        {
            if (graphicsDevice.BlendState != BlendState.Opaque) throw new InvalidOperationException($"BlendState is {graphicsDevice.BlendState}; expected {BlendState.Opaque} at {location}.");
            if (graphicsDevice.DepthStencilState != DepthStencilState.Default) throw new InvalidOperationException($"DepthStencilState is {graphicsDevice.DepthStencilState}; expected {DepthStencilState.Default} at {location}.");
            if (graphicsDevice.RasterizerState != RasterizerState.CullCounterClockwise) throw new InvalidOperationException($"RasterizerState is {graphicsDevice.RasterizerState}; expected {RasterizerState.CullCounterClockwise} at {location}.");
            // TODO: Check graphicsDevice.ScissorRectangle? Tricky because we struggle to know what the default Width/Height should be (different for shadows vs normal)
        }
#endif
    }

    public static class RenderSortHelper
    {
        private static readonly Dictionary<EffectTechnique, byte> EffectIds = new Dictionary<EffectTechnique, byte>();
        private static readonly Dictionary<RasterizerState, byte> RasterIds = new Dictionary<RasterizerState, byte>();
        private static readonly Dictionary<BlendState, byte> BlendIds = new Dictionary<BlendState, byte>();
        private static readonly Dictionary<DepthStencilState, byte> DepthIds = new Dictionary<DepthStencilState, byte>();
        private static readonly Dictionary<SamplerState, byte> SamplerIds = new Dictionary<SamplerState, byte>();
        private static readonly Dictionary<string, ushort> TextureIds = new Dictionary<string, ushort>();
        private static readonly Dictionary<Material, ushort> MaterialIds = new Dictionary<Material, ushort>();

        public static byte GetEffectId(EffectTechnique effect) => GetOrCreate(EffectIds, effect);
        public static byte GetRasterizerId(RasterizerState state) => GetOrCreate(RasterIds, state);
        public static byte GetBlendId(BlendState state) => GetOrCreate(BlendIds, state);
        public static byte GetDepthStencilId(DepthStencilState state) => GetOrCreate(DepthIds, state);
        public static byte GetSamplerId(SamplerState state) => GetOrCreate(SamplerIds, state);
        public static ushort GetTextureId(string tex) => GetOrCreate(TextureIds, tex);
        public static ushort GetMaterialId(Material material) => GetOrCreate(MaterialIds, material);

        private static TId GetOrCreate<TObj, TId>(Dictionary<TObj, TId> dict, TObj obj)
            where TObj : class
            where TId : struct, IConvertible
        {
            if (obj == null) return default;
            if (dict.TryGetValue(obj, out TId id)) return id;

            TId newId = (TId)Convert.ChangeType(dict.Count, typeof(TId));
            dict[obj] = newId;
            return newId;
        }

        public static void ClearCache()
        {
            MaterialIds.Clear();
        }
    }
}
