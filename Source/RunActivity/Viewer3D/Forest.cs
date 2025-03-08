// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Viewer3D.Common;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    [CallOnThread("Loader")]
    public class ForestViewer
    {
        readonly Viewer Viewer;
        readonly WorldPosition Position;
        readonly Material Material;
        readonly ForestPrimitive Primitive;

        public float MaximumCenterlineOffset = 0.0f;
        public bool CheckRoadsToo = false;

        public ForestViewer(Viewer viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;
            Position = position;
            MaximumCenterlineOffset = Viewer.Simulator.TRK.Tr_RouteFile.ForestClearDistance;
            CheckRoadsToo = Viewer.Simulator.TRK.Tr_RouteFile.RemoveForestTreesFromRoads;

            Material = viewer.MaterialManager.Load("Forest", Helpers.GetForestTextureFile(viewer.Simulator, forest.TreeTexture));
            Primitive = new ForestPrimitive(Viewer, forest, position, MaximumCenterlineOffset, CheckRoadsToo);
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if ((Primitive as ForestPrimitive).PrimitiveCount > 0)
            {
                var dTileX = Position.TileX - Viewer.Camera.TileX;
                var dTileZ = Position.TileZ - Viewer.Camera.TileZ;
                var mstsLocation = Position.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
                var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
                frame.AddAutoPrimitive(mstsLocation, Primitive.ObjectRadius, float.MaxValue, Material, Primitive, RenderPrimitiveGroup.World, ref xnaMatrix, Viewer.Settings.ShadowAllShapes ? ShapeFlags.ShadowCaster : ShapeFlags.None);
            }
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }
    }

    [CallOnThread("Loader")]
    public class ForestPrimitive : RenderPrimitive
    {
        readonly Viewer Viewer;
        readonly VertexBuffer VertexBuffer;
        public readonly int PrimitiveCount;
        public float MaximumCenterlineOffset;
        public bool CheckRoadsToo;

        public readonly float ObjectRadius;

        public ForestPrimitive(Viewer viewer, ForestObj forest, WorldPosition position, float maximumCenterlineOffset, bool checkRoadsToo)
        {
            Viewer = viewer;
            MaximumCenterlineOffset = maximumCenterlineOffset;
            CheckRoadsToo = checkRoadsToo;

            var trees = CalculateTrees(viewer.Tiles, forest, position, out ObjectRadius);

            if (trees.Count > 0)
            {
                VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionNormalTexture), trees.Count, BufferUsage.WriteOnly);
                VertexBuffer.SetData(trees.ToArray());
            }

            PrimitiveCount = trees.Count / 3;
        }

        private List<VertexPositionNormalTexture> CalculateTrees(TileManager tiles, ForestObj forest, WorldPosition position, out float objectRadius)
        {
            // To get consistent tree placement between sessions, derive the seed from the location.
            var random = new Random((int)(1000.0 * (position.Location.X + position.Location.Z + position.Location.Y)));
            List<TrVectorSection> sections = new List<TrVectorSection>();
            objectRadius = (float)Math.Sqrt(forest.forestArea.X * forest.forestArea.X + forest.forestArea.Z * forest.forestArea.Z) / 2;

            if (MaximumCenterlineOffset > 0)
            {
                Matrix InvForestXNAMatrix = Matrix.Invert(position.XNAMatrix);
                var addList = FindTracksAndRoadsClose(position.TileX, position.TileZ);
                FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);


                // Check for cross-tile forests

                List<Vector3> forestVertices = new List<Vector3>();
                var forestVertex = new Vector3(-forest.forestArea.X / 2, 0, -forest.forestArea.Z / 2);
                Vector3.Transform(ref forestVertex, ref position.XNAMatrix, out forestVertex);
                forestVertices.Add(forestVertex);
                forestVertex = new Vector3(forest.forestArea.X / 2, 0, -forest.forestArea.Z / 2);
                Vector3.Transform(ref forestVertex, ref position.XNAMatrix, out forestVertex);
                forestVertices.Add(forestVertex);
                forestVertex = new Vector3(-forest.forestArea.X / 2, 0, forest.forestArea.Z / 2);
                Vector3.Transform(ref forestVertex, ref position.XNAMatrix, out forestVertex);
                forestVertices.Add(forestVertex);
                forestVertex = new Vector3(forest.forestArea.X / 2, 0, forest.forestArea.Z / 2);
                Vector3.Transform(ref forestVertex, ref position.XNAMatrix, out forestVertex);
                forestVertices.Add(forestVertex);
                bool[] considerTile = new bool [4] {false, false, false, false};
                foreach (var fVertex in forestVertices)
                {
                    if (fVertex.X > 1024) considerTile[0] = true;
                    if (fVertex.X < -1024) considerTile[1] = true;
                    if (fVertex.Z > 1024) considerTile[3] = true;
                    if (fVertex.Z < -1024) considerTile[2] = true;
                }

                // add sections in nearby tiles for cross-tile forests
                if (considerTile[0])
                {
                    addList = FindTracksAndRoadsClose(position.TileX + 1, position.TileZ);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[1])
                {
                    addList = FindTracksAndRoadsClose(position.TileX - 1, position.TileZ);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[2])
                {
                    addList = FindTracksAndRoadsClose(position.TileX, position.TileZ + 1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[3])
                {
                    addList = FindTracksAndRoadsClose(position.TileX, position.TileZ - 1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[0] && considerTile[2])
                {
                    addList = FindTracksAndRoadsClose(position.TileX + 1, position.TileZ +1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[0] && considerTile[3])
                {
                    addList = FindTracksAndRoadsClose(position.TileX + 1, position.TileZ -1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[1] && considerTile[2])
                {
                    addList = FindTracksAndRoadsClose(position.TileX-1, position.TileZ + 1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
                if (considerTile[1] && considerTile[3])
                {
                    addList = FindTracksAndRoadsClose(position.TileX-1, position.TileZ - 1);
                    FindTracksAndRoadsMoreClose(ref sections, addList, forest, position, InvForestXNAMatrix);
                }
            }


            var trees = new List<VertexPositionNormalTexture>(forest.Population * 6);
            for (var i = 0; i < forest.Population; i++)
            {
                var xnaTreePosition = new Vector3((0.5f - (float)random.NextDouble()) * forest.forestArea.X, 0, (0.5f - (float)random.NextDouble()) * forest.forestArea.Z);
                Vector3.Transform(ref xnaTreePosition, ref position.XNAMatrix, out xnaTreePosition);

                bool onTrack = false;
                var scale = MathHelper.Lerp(forest.scaleRange.Minimum, forest.scaleRange.Maximum, (float)random.NextDouble());
                var treeSize = new Vector3(forest.treeSize.Width * scale, forest.treeSize.Height * scale, 1);
                var heightComputed = false;
                if (MaximumCenterlineOffset > 0 && sections != null && sections.Count > 0)
                {
                    foreach (var section in sections)
                    {
                        onTrack = InitTrackSection(section, xnaTreePosition, position.TileX, position.TileZ, treeSize.X / 2);
                        if (onTrack)
                        {
                            try
                            {
                                var trackShape = Viewer.Simulator.TSectionDat.TrackShapes.Get((uint)section.ShapeIndex);
                                if (trackShape != null && trackShape.TunnelShape)
                                {
                                    xnaTreePosition.Y = tiles.LoadAndGetElevation(position.TileX, position.TileZ, xnaTreePosition.X, -xnaTreePosition.Z, false);
                                    heightComputed = true;
                                    if (xnaTreePosition.Y > section.Y + 10)
                                    {
                                        onTrack = false;
                                        continue;
                                    }
                                }
                            }
                            catch
                            {

                            }
                            break;
                        }
                    }
                }
                if (!onTrack)
                {
                    if (!heightComputed) xnaTreePosition.Y = tiles.LoadAndGetElevation(position.TileX, position.TileZ, xnaTreePosition.X, -xnaTreePosition.Z, false);
                    xnaTreePosition -= position.XNAMatrix.Translation;

                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 1)));
                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 0)));
                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 0)));
                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 1)));
                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 1)));
                    trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 0)));
                }
            }
            return trees;
        }

        //map sections to W tiles
        private static Dictionary<string, List<TrVectorSection>> SectionMap;
        public List<TrVectorSection> FindTracksAndRoadsClose(int TileX, int TileZ)
        {
            if (SectionMap == null)
            {
                SectionMap = new Dictionary<string, List<TrVectorSection>>();
                if (MaximumCenterlineOffset > 0)
                {
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
                if (CheckRoadsToo)
                {
                    if (Viewer.Simulator.RDB != null && Viewer.Simulator.RDB.RoadTrackDB.TrackNodes != null)
                    {
                        foreach (var node in Viewer.Simulator.RDB.RoadTrackDB.TrackNodes)
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
                }
            }

            var targetKey = "" + TileX + "." + TileZ;
            if (SectionMap.ContainsKey(targetKey)) return SectionMap[targetKey];
            else return null;
        }

        TrackSection trackSection;
        bool InitTrackSection(TrVectorSection section, Vector3 xnaTreePosition, int tileX, int tileZ, float treeWidth)
        {
            trackSection = Viewer.Simulator.TSectionDat.TrackSections.Get(section.SectionIndex);
            if (trackSection == null)
                return false;
            if (trackSection.SectionCurve != null)
            {
                return InitTrackSectionCurved(tileX, tileZ, xnaTreePosition.X, -xnaTreePosition.Z, section, treeWidth);
            }
            return InitTrackSectionStraight(tileX, tileZ, xnaTreePosition.X, -xnaTreePosition.Z, section, treeWidth);
        }

        // don't consider track sections outside the forest boundaries
        public void FindTracksAndRoadsMoreClose(ref List<TrVectorSection> sections, List<TrVectorSection> allSections, ForestObj forest, WorldPosition position, Matrix invForestXNAMatrix)
        {
            if (allSections != null && allSections.Count > 0)
            {
                var toAddX = MaximumCenterlineOffset + forest.forestArea.X / 2 + forest.scaleRange.Maximum * forest.treeSize.Width;
                var toAddZ = MaximumCenterlineOffset + forest.forestArea.Z / 2 + forest.scaleRange.Maximum * forest.treeSize.Width;
                foreach (TrVectorSection section in allSections)
                {
                    Vector3 sectPosition;
                    Vector3 sectPosToForest;
                    sectPosition.X = section.X;
                    sectPosition.Z = section.Z;
                    sectPosition.Y = section.Y;
                    sectPosition.X += (section.TileX - position.TileX) * 2048;
                    sectPosition.Z += (section.TileZ - position.TileZ) * 2048;
                    sectPosition.Z = -sectPosition.Z;
                    sectPosToForest = Vector3.Transform(sectPosition, invForestXNAMatrix);
                    sectPosToForest.Z = -sectPosToForest.Z;
                    trackSection = Viewer.Simulator.TSectionDat.TrackSections.Get(section.SectionIndex);
                    if (trackSection == null) continue;
                    var trackSectionLength = GetLength(trackSection);
                    if (Math.Abs(sectPosToForest.X) > trackSectionLength + toAddX) continue;
                    if (Math.Abs(sectPosToForest.Z) > trackSectionLength + toAddZ) continue;
                    sections.Add(section);
                }
            }
            return;
        }

        const float InitErrorMargin = 0.5f;

        bool InitTrackSectionCurved(int tileX, int tileZ, float x, float z, TrVectorSection trackVectorSection, float treeWidth)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (radius * angle + error) by (radius * angle + error) around starting coordinates but no more than 2 for angle.
            var boundingDistance = trackSection.SectionCurve.Radius * Math.Min(Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)), 2) + MaximumCenterlineOffset+treeWidth;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // To simplify the math, center around the start of the track section, rotate such that the track section starts out pointing north (+z) and flip so the track curves to the right.
            x -= sx;
            z -= sz;
            MstsUtility.Rotate2D(trackVectorSection.AY, ref x, ref z);
            if (trackSection.SectionCurve.Angle < 0)
                x *= -1;

            // Compute distance to curve's center at (radius,0) then adjust to get distance from centerline.
            dx = x - trackSection.SectionCurve.Radius;
            var lat = Math.Sqrt(dx * dx + z * z) - trackSection.SectionCurve.Radius;
            if (Math.Abs(lat) > MaximumCenterlineOffset + treeWidth)
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

        bool InitTrackSectionStraight(int tileX, int tileZ, float x, float z, TrVectorSection trackVectorSection, float treeWidth)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (length + error) by (length + error) around starting coordinates.
            var boundingDistance = trackSection.SectionSize.Length + MaximumCenterlineOffset + treeWidth;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // Calculate distance along and away from the track centerline.
            float lat, lon;
            MstsUtility.Survey(sx, sz, trackVectorSection.AY, x, z, out lon, out lat);
            var trackSectionLength = GetLength(trackSection);
            if (Math.Abs(lat) > MaximumCenterlineOffset + treeWidth)
                return false;
            if (lon < -InitErrorMargin || lon > trackSectionLength + InitErrorMargin)
                return false;

            return true;
        }

        static float GetLength(TrackSection trackSection)
        {
            return trackSection.SectionCurve != null ? trackSection.SectionCurve.Radius * Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize.Length;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, PrimitiveCount);
        }
    }

    [CallOnThread("Render")]
    public class ForestMaterial : Material
    {
        readonly Texture2D TreeTexture;
        IEnumerator<EffectPass> ShaderPasses;

        [CallOnThread("Loader")]
        public ForestMaterial(Viewer viewer, string treeTexture)
            : base(viewer, treeTexture)
        {
            TreeTexture = Viewer.TextureManager.Get(treeTexture, true);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.CurrentTechnique = shader.Techniques["Forest"];
            if (ShaderPasses == null) ShaderPasses = shader.CurrentTechnique.Passes.GetEnumerator();
            shader.ImageTexture = TreeTexture;
            shader.ReferenceAlpha = 200;

            // Enable alpha blending for everything: this allows distance scenery to appear smoothly.
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            shader.SetViewMatrix(ref XNAViewMatrix);
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    ShaderPasses.Current.Apply();

                    // SamplerStates can only be set after the ShaderPasses.Current.Apply().
                    graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            Viewer.MaterialManager.SceneryShader.ReferenceAlpha = 0;
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        public override Texture2D GetShadowTexture()
        {
            return TreeTexture;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(TreeTexture);
            base.Mark();
        }
    }
}
