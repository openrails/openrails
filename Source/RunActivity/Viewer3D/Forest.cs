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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS.Viewer3D
{
    [CallOnThread("Loader")]
    public class ForestDrawer
    {
        readonly Viewer Viewer;
        readonly WorldPosition Position;
        readonly Material Material;
        readonly ForestMesh Mesh;

        public ForestDrawer(Viewer viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;
            Position = position;
            Material = viewer.MaterialManager.Load("Forest", Helpers.GetForestTextureFile(viewer.Simulator, forest.TreeTexture));
            Mesh = new ForestMesh(Viewer, forest, position);
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var dTileX = Position.TileX - Viewer.Camera.TileX;
            var dTileZ = Position.TileZ - Viewer.Camera.TileZ;
            var mstsLocation = Position.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
            var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            frame.AddAutoPrimitive(mstsLocation, Mesh.ObjectRadius, float.MaxValue, Material, Mesh, RenderPrimitiveGroup.World, ref xnaMatrix, Viewer.Settings.ShadowAllShapes ? ShapeFlags.ShadowCaster : ShapeFlags.None);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }
    }

    [CallOnThread("Loader")]
    public class ForestMesh : RenderPrimitive
    {
        readonly Viewer Viewer;
        readonly VertexDeclaration VertexDeclaration;
        readonly int VertexStride;
        readonly VertexBuffer VertexBuffer;
        readonly int PrimitiveCount;

        public readonly float ObjectRadius;

        public ForestMesh(Viewer viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;

            var trees = CalculateTrees(viewer.Tiles, forest, position, out ObjectRadius);

            VertexDeclaration = new VertexDeclaration(viewer.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            VertexStride = VertexDeclaration.GetVertexStrideSize(0);
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionNormalTexture), trees.Count, BufferUsage.WriteOnly);
            VertexBuffer.SetData(trees.ToArray());
            PrimitiveCount = trees.Count / 3;
        }

        static List<VertexPositionNormalTexture> CalculateTrees(TileManager tiles, ForestObj forest, WorldPosition position, out float objectRadius)
        {
            if (forest.scaleRange.scaleRange1 > forest.scaleRange.scaleRange2)
                Trace.TraceWarning("{0} forest {1}: tree size minimum greater than maximum, using values backwards", position, forest.TreeTexture);

            // To get consistent tree placement between sessions, derive the seed from the location.
            var random = new Random((int)(1000.0 * (position.Location.X + position.Location.Z + position.Location.Y)));
            var area = new Vector3(Math.Abs(forest.forestArea.areaDim1), 0, Math.Abs(forest.forestArea.areaDim2));
            var population = (int)(0.75f * (float)forest.Population) + 1;
            var size = new Vector3(forest.treeSize.treeSize1, forest.treeSize.treeSize2, 0);
            var scaleMin = Math.Min(forest.scaleRange.scaleRange1, forest.scaleRange.scaleRange2);
            var scaleMax = Math.Max(forest.scaleRange.scaleRange1, forest.scaleRange.scaleRange2);
            var plantingArea = area - new Vector3(size.X * 2 + 1, 0, size.X * 2 + 1);
            if (plantingArea.X < 0.5) plantingArea.X = 0.5f;
            if (plantingArea.Z < 0.5) plantingArea.Z = 0.5f;

            objectRadius = (float)Math.Sqrt(area.X * area.X + area.Z * area.Z) / 2;

            var trees = new List<VertexPositionNormalTexture>(population * 6);
            for (var i = 0; i < population; i++)
            {
                var xnaTreePosition = new Vector3((0.5f - (float)random.NextDouble()) * plantingArea.X, 0, (0.5f - (float)random.NextDouble()) * plantingArea.Z);
                xnaTreePosition = Vector3.Transform(xnaTreePosition, position.XNAMatrix);
                xnaTreePosition.Y = tiles.GetElevation(position.TileX, position.TileZ, xnaTreePosition.X, -xnaTreePosition.Z);
                xnaTreePosition -= position.XNAMatrix.Translation;

                var scale = MathHelper.Lerp(scaleMin, scaleMax, (float)random.NextDouble());
                var treeSize = new Vector3(size.X * scale, size.Y * scale, 1);

                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 1)));
                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 0)));
                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 0)));
                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(1, 1)));
                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 1)));
                trees.Add(new VertexPositionNormalTexture(xnaTreePosition, treeSize, new Vector2(0, 0)));
            }

            return trees;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexStride);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, PrimitiveCount);
        }
    }
}
