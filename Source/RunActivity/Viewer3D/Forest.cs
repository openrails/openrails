// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS
{
    #region ForestDrawer
    public class ForestDrawer
    {
        readonly Viewer3D Viewer;
        readonly Material forestMaterial;

        // Classes reqiring instantiation
        public ForestMesh forestMesh;

        #region Class variables
        public readonly WorldPosition worldPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// ForestDrawer constructor
        /// </summary>
        public ForestDrawer(Viewer3D viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;
            worldPosition = position;

            forestMaterial = viewer.MaterialManager.Load("Forest", Helpers.GetForestTextureFile(viewer.Simulator, forest.TreeTexture), 0, 0);

            // Instantiate classes
            forestMesh = new ForestMesh(Viewer.RenderProcess, Viewer.Tiles, this, forest);
        }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Locate relative to the camera
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
			var xnaTranslation = worldPosition.XNAMatrix.Translation;
			Vector3 mstsLocation = new Vector3(xnaTranslation.X + dTileX * 2048, forestMesh.refElevation, -xnaTranslation.Z + dTileZ * 2048);
			Matrix xnaPatchMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            float viewingDistance = Viewer.Settings.ViewingDistance; // Arbitrary, but historically in MSTS it was only 1000.
			frame.AddAutoPrimitive(mstsLocation, forestMesh.objectRadius, viewingDistance + forestMesh.objectRadius, forestMaterial, forestMesh, 
                RenderPrimitiveGroup.World, ref xnaPatchMatrix, Viewer.Settings.ShadowAllShapes ? ShapeFlags.ShadowCaster : ShapeFlags.None);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            forestMaterial.Mark();
        }
    }
    #endregion

    #region ForestMesh
    public class ForestMesh : RenderPrimitive
    {
        // Vertex declaration
        public VertexDeclaration treeVertexDeclaration;
        public VertexBuffer Buffer;
        public int PrimitiveCount;

        // Forest variables
        Random random;
        ForestDrawer Drawer;
        Viewer3D Viewer;
        public float objectRadius;
        public float refElevation;

        // Basic geometric parameters of a forest object, from the World file.
        string treeTexture;
        float scaleRange1;
        float scaleRange2;
        float areaDim1;
        float areaDim2;
        int population;
        float treeSize1;
        float treeSize2;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ForestMesh(RenderProcess renderProcess, TileManager tiles, ForestDrawer drawer, ForestObj forest)
        {
            Drawer = drawer;
            Viewer = renderProcess.Viewer;
            // Initialize local variables from WFile data
            treeTexture = forest.TreeTexture;
            scaleRange1 = forest.scaleRange.scaleRange1;
            scaleRange2 = forest.scaleRange.scaleRange2;
            if (scaleRange1 > scaleRange2)
            {
                Trace.TraceWarning("{0} forest {1} has scale range with minimum greater than maximum", drawer.worldPosition, forest.TreeTexture);
                float scaleRangeSwap = scaleRange2;
                scaleRange2 = scaleRange1;
                scaleRange1 = scaleRangeSwap;
            }

            areaDim1 = Math.Abs(forest.forestArea.areaDim1);
            areaDim2 = Math.Abs(forest.forestArea.areaDim2);
            population = (int)(0.75f * (float)forest.Population) + 1;
            treeSize1 = forest.treeSize.treeSize1;
            treeSize2 = forest.treeSize.treeSize2;

            objectRadius = Math.Max(areaDim1, areaDim2) / 2;

            // Instantiate classes
            // to get consistent tree placement between sessions, derive the seed from the location
            int seed = (int)(1000.0*(drawer.worldPosition.Location.X + drawer.worldPosition.Location.Z + drawer.worldPosition.Location.Y));
            random = new Random(seed);
            VertexPositionNormalTexture[] trees = new VertexPositionNormalTexture[population * 6];
            treeVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPositionNormalTexture.VertexElements);

            InitForestVertices(tiles, trees);

            PrimitiveCount = trees.Length / 3;
            Buffer = new VertexBuffer(renderProcess.GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * trees.Length, BufferUsage.WriteOnly);
            Buffer.SetData(trees);
        }

        /// <summary>
        /// Forest tree array intialization. 
        /// </summary>
        private void InitForestVertices(TileManager tiles, VertexPositionNormalTexture[] trees)
        {
            // Create the tree position and size arrays.
            Vector3[] treePosition = new Vector3[population];
            Vector3[] tempPosition = new Vector3[population]; // Used only for getting the terrain Y value
            Vector3[] treeSize = new Vector3[population];
            // Find out where in the world we are.
            Matrix XNAWorldLocation = Drawer.worldPosition.XNAMatrix;
            Drawer.worldPosition.XNAMatrix = Matrix.Identity;
            Drawer.worldPosition.XNAMatrix.Translation = XNAWorldLocation.Translation;
            float YtileX, YtileZ;
            // Get the Y elevation of the base object itself. Tree elevations are referenced to this.
            YtileX = (XNAWorldLocation.M41 + 1024) / 8;
            YtileZ = (XNAWorldLocation.M43 + 1024) / 8;
            refElevation = tiles.GetElevation(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, (int)YtileX, (int)YtileZ);
            float scale;

            //we will use an inner boundary of 2 meters to plant trees, so will make sure the area is big enough
            //if (areaDim1 < treeSize1 * 1.5f + 1) areaDim1 = treeSize1 * 1.5f + 1;
            //if (areaDim2 < treeSize1 * 1.5f + 1) areaDim2 = treeSize1 * 1.5f + 1;
            var dim1 = areaDim1 - treeSize1 * 2 - 1;
            var dim2 = areaDim2 - treeSize1 * 2 - 1;
            if (dim1 < 0.5) dim1 = 0.5f;
            if (dim2 < 0.5) dim2 = 0.5f;
            for (int i = 0; i < population; i++)
            {
                // Set the XZ position of each tree at random.
                treePosition[i].X = (0.5f - (float)random.NextDouble()) * dim1;
                treePosition[i].Y = 0;
                treePosition[i].Z = (0.5f - (float)random.NextDouble()) * dim2;
                // Orient each treePosition to its final position on the tile so we can get its Y value.
                // Do this by transforming a copy of the object to its final orientation on the terrain.
                tempPosition[i] = Vector3.Transform(treePosition[i], XNAWorldLocation);
                treePosition[i] = tempPosition[i] - XNAWorldLocation.Translation;
                // Get the terrain height at each position and set Y.
				treePosition[i].Y = tiles.GetElevation(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, (tempPosition[i].X + 1024) / 8, (tempPosition[i].Z + 1024) / 8) - refElevation;
                // WVP transformation of the complete object takes place in the vertex shader.

                // Randomize the tree size
                scale = (float)random.Next((int)(scaleRange1 * 1000), (int)(scaleRange2 * 1000)) / 1000;
                treeSize[i].X = treeSize1 * scale;
                treeSize[i].Y = treeSize2 * scale;
                treeSize[i].Z = 1.0f;
            }

            // Create the tree vertex array.
            // Using the Normal property to hold the size info.
            for (int i = 0; i < population * 6; i++)
            {
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 1));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 0));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 0));

                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 1));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 1));
                trees[i] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 0));
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = treeVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(Buffer, 0, treeVertexDeclaration.GetVertexStrideSize(0));
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, PrimitiveCount);
        }

        //map sections to W tiles
        private static Dictionary<string, List<TrVectorSection>> SectionMap;
        public List<TrVectorSection> FindTracksClose(int TileX, int TileZ)
        {
            if (SectionMap == null)
            {
                SectionMap = new Dictionary<string, List<TrVectorSection>>();
                foreach (var node in Viewer.Simulator.TDB.TrackDB.TrackNodes)
                {
                    if (node == null || node.TrVectorNode == null) continue;
                    foreach (var section in node.TrVectorNode.TrVectorSections)
                    {
                        var key = "" + section.WFNameX + "." + section.WFNameZ;
                        if (!SectionMap.ContainsKey(key)) SectionMap.Add(key, new List<TrVectorSection>());
                        SectionMap[key].Add(section);
                    }
                }
            }

            var targetKey = "" + TileX + "." + TileZ;
            if (SectionMap.ContainsKey(targetKey)) return SectionMap[targetKey];
            else return null;
        }

        TrackSection trackSection;
        bool InitTrackSection(TrVectorSection section)
        {
            trackSection = Viewer.Simulator.TSectionDat.TrackSections.Get(section.SectionIndex);
            if (trackSection == null)
                return false;
            if (trackSection.SectionCurve != null)
            {
                return InitTrackSectionCurved(section.TileX, section.TileZ, section.X, section.Z, section);
            }
            return InitTrackSectionStraight(section.TileX, section.TileZ, section.X, section.Z, section);
        }

        const float MaximumCenterlineOffset = 2.5f;
        const float InitErrorMargin = 0.5f;

        bool InitTrackSectionCurved(int tileX, int tileZ, float x, float z, TrVectorSection trackVectorSection)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (radius * angle + error) by (radius * angle + error) around starting coordinates but no more than 2 for angle.
            var boundingDistance = trackSection.SectionCurve.Radius * Math.Min(Math.Abs(MSTSMath.M.Radians(trackSection.SectionCurve.Angle)), 2) + MaximumCenterlineOffset;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // To simplify the math, center around the start of the track section, rotate such that the track section starts out pointing north (+z) and flip so the track curves to the right.
            x -= sx;
            z -= sz;
            MSTSMath.M.Rotate2D(trackVectorSection.AY, ref x, ref z);
            if (trackSection.SectionCurve.Angle < 0)
                x *= -1;

            // Compute distance to curve's center at (radius,0) then adjust to get distance from centerline.
            dx = x - trackSection.SectionCurve.Radius;
            var lat = Math.Sqrt(dx * dx + z * z) - trackSection.SectionCurve.Radius;
            if (Math.Abs(lat) > MaximumCenterlineOffset)
                return false;

            // Compute distance along curve (ensure we are in the top right quadrant, otherwise our math goes wrong).
            if (z < -InitErrorMargin || x > trackSection.SectionCurve.Radius + InitErrorMargin || z > trackSection.SectionCurve.Radius + InitErrorMargin)
                return false;
            var radiansAlongCurve = (float)Math.Asin(z / trackSection.SectionCurve.Radius);
            var lon = radiansAlongCurve * trackSection.SectionCurve.Radius;
            var trackSectionLength = GetLength(trackSection);
            if (lon < -InitErrorMargin || lon > trackSectionLength + InitErrorMargin)
                return false;

            return true;
        }

        bool InitTrackSectionStraight(int tileX, int tileZ, float x, float z, TrVectorSection trackVectorSection)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (length + error) by (length + error) around starting coordinates.
            var boundingDistance = trackSection.SectionSize.Length + MaximumCenterlineOffset;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // Calculate distance along and away from the track centerline.
            float lat, lon;
            MSTSMath.M.Survey(sx, sz, trackVectorSection.AY, x, z, out lon, out lat);
            var trackSectionLength = GetLength(trackSection);
            if (Math.Abs(lat) > MaximumCenterlineOffset)
                return false;
            if (lon < -InitErrorMargin || lon > trackSectionLength + InitErrorMargin)
                return false;

            return true;
        }

        static float GetLength(TrackSection trackSection)
        {
            return trackSection.SectionCurve != null ? trackSection.SectionCurve.Radius * Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize.Length;
        }

    }
    #endregion

}
