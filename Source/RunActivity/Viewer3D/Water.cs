/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS
{
    public class WaterTile: RenderPrimitive
    {
        public readonly Viewer3D Viewer;
        public readonly int TileX, TileZ;

        // these can be shared since they are the same for all patches
		static IEnumerable<KeyValuePair<float, Material>> WaterLayers;
        public static VertexDeclaration PatchVertexDeclaration = null;

        // these change per tile
        private static int PatchVertexStride;  // in bytes
        public IndexBuffer TileIndexBuffer = null;  // not constant, because some patches don't have water
        private int TriangleCount = 0;
        private VertexBuffer TileVertexBuffer;
        private WorldLocation TileWorldLocation;


		public WaterTile(Viewer3D viewer, int tileX, int tileZ)
		{
			Viewer = viewer;
			TileX = tileX;
			TileZ = tileZ;

			if (viewer.Tiles.GetTile(tileX, tileZ) == null)
				return;

            if (WaterLayers == null && Viewer.ENVFile.WaterLayers != null)
				LoadWaterMaterial();

			if (PatchVertexDeclaration == null)
				LoadPatchVertexDeclaration();

			LoadGeometry();
		}

		private void LoadWaterMaterial()
		{
            WaterLayers = Viewer.ENVFile.WaterLayers.Select(layer => new KeyValuePair<float, Material>(layer.Height, Materials.Load(Viewer.RenderProcess, "WaterMaterial", Viewer.Simulator.RoutePath + @"\envfiles\textures\" + layer.TextureName)));
		}

        private Matrix xnaMatrix = Matrix.Identity;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public void PrepareFrame(RenderFrame frame)
        {
            if (WaterLayers == null)  // if there was a problem loading the water texture
                return;

			int dTileX = TileX - Viewer.Camera.TileX;
			int dTileZ = TileZ - Viewer.Camera.TileZ;
			Vector3 mstsLocation = new Vector3(1024 + dTileX * 2048, 0, 1024 + dTileZ * 2048);

            // Distance cull
			if (Viewer.Camera.CanSee(mstsLocation, 1448f, 2000f))
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


        /// <summary>
        /// This is called when the water should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Indices = TileIndexBuffer;
            graphicsDevice.Vertices[0].SetSource(TileVertexBuffer, 0, PatchVertexStride);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 17 * 17, 0, TriangleCount);
        }


        // vertex type declaration to be shared by all tiles
        private void LoadPatchVertexDeclaration()
        {
            PatchVertexDeclaration = new VertexDeclaration(Viewer.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            PatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
        }

        private void LoadGeometry()
        {
            GraphicsDevice graphicsDevice = Viewer.GraphicsDevice;

            Tile tile = Viewer.Tiles.GetTile(TileX, TileZ);
            TFile tFile = tile.TFile;
            TileWorldLocation = new WorldLocation(TileX, TileZ, 0, 0, 0);

            ORTSMath.Matrix2x2 waterLevels = new ORTSMath.Matrix2x2(tFile.WaterSW, tFile.WaterSE, tFile.WaterNW, tFile.WaterNE);

            int indexCount = 16 * 16 * 2 * 3;  // 16 x 16 squares * 2 triangles per square * 3 indices per triangle
            short[] indexData = new short[indexCount];

            // Create index buffer representing only patches that have water
            int iIndex = 0;
            // for each 128 meter patch
            for (int iz = 0; iz < 16; ++iz)
                for (int ix = 0; ix < 16; ++ix)
                {
                    terrain_patchset_patch patch = tFile.terrain.terrain_patchsets[0].GetPatch(ix,iz);

                    if (patch.WaterEnabled)
                    {
                        short nw = (short)(ix + iz * 17);  // vertice index in the north west corner
                        short ne = (short)(nw + 1);
                        short sw = (short)(nw + 17);
                        short se = (short)(sw + 1);

                        TriangleCount += 2;

                        if (((ix & 1) == (iz & 1)))  // triangles alternate
                        {
                            indexData[iIndex++] = nw;
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = sw;
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = nw;
                            indexData[iIndex++] = ne;
                        }
                        else
                        {
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = sw;
                            indexData[iIndex++] = ne;
                            indexData[iIndex++] = nw;
                            indexData[iIndex++] = ne;
                            indexData[iIndex++] = sw;
                        }
                    }
                }
            TileIndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * iIndex, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            TileIndexBuffer.SetData<short>(indexData, 0, iIndex);

            // Set up the vertex buffer
            VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[17 * 17];
            int iV = 0;
            // for each vertex - starting in SW corner
            for (int iz = 16; iz >= 0; --iz)
                for (int ix = 0; ix < 17; ++ix)
                {
                    float xp = (float)ix / 16.0f;  // make it 0-1
                    float zp = (float)iz / 16.0f;  // make it 0-1
                    float y = ORTSMath.Interpolate2D(xp, zp, waterLevels);

                    float x = -1024 + xp * 2048f;  // make it -1024 to +1024
                    float z = -1024 + zp * 2048f;

                    vertexData[iV].Position = new Vector3(x, y, -z);
                    vertexData[iV].TextureCoordinate = new Vector2(xp, zp);
                    vertexData[iV].Normal = new Vector3(0, 1, 0);
                    iV++;
                }
            TileVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
            TileVertexBuffer.SetData(vertexData);
        }

    } // class watertile


}
