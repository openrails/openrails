// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS
{
    public class WaterTile : RenderPrimitive
    {
        readonly Viewer3D Viewer;
        readonly int TileX;
        readonly int TileZ;

        static KeyValuePair<float, Material>[] WaterLayers;
        public static VertexDeclaration PatchVertexDeclaration;
        static int PatchVertexStride;

        VertexBuffer TileVertexBuffer;
        IndexBuffer TileIndexBuffer;
        int TriangleCount;
        Matrix xnaMatrix = Matrix.Identity;

        public WaterTile(Viewer3D viewer, int tileX, int tileZ)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;

            if (viewer.Tiles.GetTile(tileX, tileZ) == null)
                return;

            if (PatchVertexDeclaration == null)
                LoadStaticData();

            LoadGeometry(Viewer.GraphicsDevice);
        }

        void LoadStaticData()
        {
            if (Viewer.ENVFile.WaterLayers != null)
                WaterLayers = Viewer.ENVFile.WaterLayers.Select(layer => new KeyValuePair<float, Material>(layer.Height, Viewer.MaterialManager.Load("Water", Viewer.Simulator.RoutePath + @"\envfiles\textures\" + layer.TextureName))).ToArray();

            PatchVertexDeclaration = new VertexDeclaration(Viewer.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            PatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame)
        {
            if (WaterLayers == null)  // if there was a problem loading the water texture
                return;

            var dTileX = TileX - Viewer.Camera.TileX;
            var dTileZ = TileZ - Viewer.Camera.TileZ;
            var mstsLocation = new Vector3(1024 + dTileX * 2048, 0, 1024 + dTileZ * 2048);

            if (Viewer.Camera.CanSee(mstsLocation, 1448f, Viewer.Settings.ViewingDistance))
            {
                xnaMatrix.M41 = mstsLocation.X - 1024;
                xnaMatrix.M43 = 1024 - mstsLocation.Z;
                foreach (var waterLayer in WaterLayers)
                {
                    xnaMatrix.M42 = mstsLocation.Y + waterLayer.Key;
                    frame.AddPrimitive(waterLayer.Value, this, RenderPrimitiveGroup.World, ref xnaMatrix);
                }
            }
        }

        [CallOnThread("Render")]
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Indices = TileIndexBuffer;
            graphicsDevice.Vertices[0].SetSource(TileVertexBuffer, 0, PatchVertexStride);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 17 * 17, 0, TriangleCount);
        }

        void LoadGeometry(GraphicsDevice graphicsDevice)
        {
            var tFile = Viewer.Tiles.GetTile(TileX, TileZ).TFile;
            var waterLevels = new ORTSMath.Matrix2x2(tFile.WaterNW, tFile.WaterNE, tFile.WaterSW, tFile.WaterSE);

            var indexData = new List<short>(16 * 16 * 2 * 3);
            for (var z = 0; z < 16; ++z)
                for (var x = 0; x < 16; ++x)
                {
                    var patch = tFile.terrain.terrain_patchsets[0].GetPatch(x, z);

                    if (!patch.WaterEnabled)
                        continue;

                    var nw = (short)(z * 17 + x);  // vertice index in the north west corner
                    var ne = (short)(nw + 1);
                    var sw = (short)(nw + 17);
                    var se = (short)(sw + 1);

                    TriangleCount += 2;

                    if (((z & 1) == (x & 1)))  // triangles alternate
                    {
                        indexData.Add(nw);
                        indexData.Add(se);
                        indexData.Add(sw);
                        indexData.Add(nw);
                        indexData.Add(ne);
                        indexData.Add(se);
                    }
                    else
                    {
                        indexData.Add(ne);
                        indexData.Add(se);
                        indexData.Add(sw);
                        indexData.Add(nw);
                        indexData.Add(ne);
                        indexData.Add(sw);
                    }
                }
            TileIndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Count, BufferUsage.WriteOnly);
            TileIndexBuffer.SetData(indexData.ToArray());

            var vertexData = new List<VertexPositionNormalTexture>(17 * 17);
            for (var z = 0; z < 17; ++z)
                for (var x = 0; x < 17; ++x)
                {
                    var U = (float)x / 16;
                    var V = (float)z / 16;

                    var w = -1024 + x * 128;
                    var n = -1024 + z * 128;
                    var y = ORTSMath.Interpolate2D(U, V, waterLevels);

                    vertexData.Add(new VertexPositionNormalTexture(new Vector3(w, y, n), Vector3.UnitY, new Vector2(U, V)));
                }
            TileVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertexData.Count, BufferUsage.WriteOnly);
            TileVertexBuffer.SetData(vertexData.ToArray());
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            foreach (var material in WaterLayers.Select(kvp => kvp.Value))
                material.Mark();
        }
    }
}
