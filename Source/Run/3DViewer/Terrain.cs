/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Threading;
using MSTS;

/// The Terrain consists of TerrainTiles 2km square each subdivided 16 x 16 into TerrainPatch's
/// The TerrainTile class

namespace ORTS
{

    public class TerrainDrawer
    {
        public TerrainTile[] TerrainTiles = new TerrainTile[9]; // surrounding tiles, not in any particular order
        private int viewerTileX, viewerTileZ;  // the center of the currently loaded set of tiles
        private Viewer Viewer;


        public TerrainDrawer(Viewer viewer)
        {
            Viewer = viewer;
            for (int i = 0; i < TerrainTiles.Length; ++i)
                TerrainTiles[i] = null;
        }

        public void Update(GameTime gameTime)
        {
            // TODO, reading camera's location should be locked to ensure its atomic

            if (viewerTileX != Viewer.Camera.TileX || viewerTileZ != Viewer.Camera.TileZ)   // if the camera has moved into a new tile
            {
                viewerTileX = Viewer.Camera.TileX;
                viewerTileZ = Viewer.Camera.TileZ;

                // remove any tiles out of range
                for (int i = 0; i < TerrainTiles.Length; ++i)
                {
                    TerrainTile tile = TerrainTiles[i];
                    if (tile != null)
                    {
                        if (Math.Abs(tile.TileX - viewerTileX) > 1
                          || Math.Abs(tile.TileZ - viewerTileZ) > 1)
                        {
                            Console.Write("t");
                            TerrainTiles[i].Dispose();
                            TerrainTiles[i] = null;
                        }
                    }
                }

                // add in tiles in range 
                // by starting in the se corner we eliminate seams 
                // by ensuring the tiles to the right and below are 
                // loaded.
                LoadAt(viewerTileX + 1, viewerTileZ - 1);
                LoadAt(viewerTileX, viewerTileZ + 1);
                LoadAt(viewerTileX - 1, viewerTileZ - 1);
                LoadAt(viewerTileX + 1, viewerTileZ);
                LoadAt(viewerTileX, viewerTileZ);
                LoadAt(viewerTileX - 1, viewerTileZ);
                LoadAt(viewerTileX + 1, viewerTileZ + 1);
                LoadAt(viewerTileX, viewerTileZ - 1);
                LoadAt(viewerTileX - 1, viewerTileZ + 1);
            }

        }

           /// <summary>
        /// If the specified tile isn't already loaded, then
        /// load it into any available location in the 
        /// TerrainTiles array.
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileZ"></param>
        private void LoadAt(int tileX, int tileZ)
        {
            // return if this tile is already loaded
            foreach( TerrainTile tile in TerrainTiles )   // check every tile
                if (tile != null)
                    if (tile.TileX == tileX && tile.TileZ == tileZ)  // return if its the one we want
                        return;

            // find an available spot in the TerrainTiles array
            for( int i = 0; i < TerrainTiles.Length; ++i )
                if ( TerrainTiles[i] == null)  // we found one
                {
                    Console.Write("T");
                    TerrainTiles[i] = new TerrainTile( Viewer, tileX, tileZ);
                    return;
                }

            // otherwise we didn't find an available spot - this shouldn't happen
            throw new System.Exception("Program Bug - didn't expect TerrainTiles array to be full.");
        }   
}

    public class TerrainTile: IDisposable
    {
        public int TileX, TileZ;
        public Viewer Viewer;
        private TerrainPatch[,] TerrainPatches = new TerrainPatch[16, 16];
        private WaterTile WaterTile = null;

        public TerrainTile(Viewer viewer, int tileX, int tileZ)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;
            Tile tile = viewer.Tiles.GetTile(tileX, tileZ);
            if (!tile.IsEmpty)
            {
                if( tile.TFile.ContainsWater )
                    WaterTile = new WaterTile(viewer, TileX, TileZ);

                TFile TFile = tile.TFile;
                for (int x = 0; x < 16; ++x)
                    for (int z = 0; z < 16; ++z)
                    {
                        if (!tile.IsEmpty && TFile.terrain.terrain_patchsets[0].GetPatch(x, z).DrawingEnabled)
                        {
                            TerrainPatch patch = new TerrainPatch(viewer, TFile, tile.YFile, x, z, tileX, tileZ);
                            TerrainPatches[x, z] = patch;
                            viewer.Components.Add(patch);
                        }
                        else
                        {
                            TerrainPatches[x, z] = null;
                        }
                    }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (WaterTile != null)
                WaterTile.Dispose();

            for( int x = 0; x < 16; ++x )
                for( int z = 0; z < 16; ++z )
                {
                    TerrainPatch patch = TerrainPatches[x,z];
                    if (patch != null)
                    {
                        TerrainPatches[x, z] = null;
                        Viewer.Components.Remove(patch);
                        patch.Dispose();
                    }
                }
        }

        #endregion
    }

    public class TerrainPatch : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Viewer Viewer;

        private Vector3 XNAPatchLocation;      // in XNA world coordinates relative to the center of the tile
        private Matrix XNAPatchWorldPosition;  // computed from above to save time in the Draw routine
        private VertexBuffer PatchVertexBuffer;
        private Texture2D PatchTexture;

        // these can be shared since they are the same for all patches
        static VertexDeclaration PatchVertexDeclaration = null; 
        static IndexBuffer PatchIndexBuffer = null;
        static int PatchVertexStride;  // in bytes

        // These convey information from the constructor, to LoadContent
        // After LoadContent is called, these become invalid
        int PatchX, PatchZ;  // ie 0,0 is NW patch to 15,15 in the SE 
        TFile TFile;
        YFile YFile;
        string terrtexName;
        int TileX, TileZ;
        
        float X,Y, W, B, C, H;  // A 2 x 3 matrix for texture translation

        public TerrainPatch(Viewer viewer, TFile tFile, YFile yFile, int x, int z, int tileX, int tileZ)
            : base(viewer)
        {
            Viewer = viewer;

            TFile = tFile;
            YFile = yFile;

            TileX = tileX;
            TileZ = tileZ;

            PatchX = x;
            PatchZ = z;

            terrain_patchset_patch patch = tFile.terrain.terrain_patchsets[0].GetPatch(x, z);
            terrain_shader terrain_shader = (terrain_shader)TFile.terrain.terrain_shaders[patch.iShader];
            terrtexName = terrain_shader.terrain_texslots[0].Filename;

            float cx =  -1024+(int)patch.CenterX;
            float cz =  -1024-(int)patch.CenterZ;
            XNAPatchLocation = new Vector3(cx, 0, cz);
            XNAPatchWorldPosition = Matrix.CreateTranslation(XNAPatchLocation);
            X = patch.X;
            Y = patch.Y;
            W = patch.W;
            B = patch.B;
            C = patch.C;
            H = patch.H;
        }


        /// <summary>
        /// Return the terrain elevation in meters above sea level 
        /// from the specified vertex indices
        /// x = 0 to 255 from w to e
        /// z = 0 to 255 from n to s 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private float Elevation(int x, int z)
        {
            int hx = PatchX*16 + x;
            int hz = PatchZ*16 + z;
            if (hx > 255 || hx < 0 || hz > 255 || hz < 0)
                // its outside this tile, so we will have to look it up
                return Viewer.Tiles.GetElevation(TileX, TileZ, hx, hz);

            uint e = YFile.GetElevationIndex(hx, hz);
            return (float)e * TFile.Resolution + TFile.Floor;
        }

        /// <summary>
        /// Return the vertex normal at the specified 
        /// terrain vertex indices
        /// x = 0 to 255 from w to e
        /// z = 0 to 255 from n to s 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private Vector3 TerrainNormal(int x, int z)
        {
            // TODO, decode this from the _N.RAW TILE
            // until I figure out this file, I'll compute normals from the terrain

            float y = Elevation(x, z);

            float ynw = Elevation( x - 1, z  - 1 );
            float yw = Elevation(x - 1, z );
            float ysw = Elevation(x - 1, z + 1);
            float ys = Elevation(x , z + 1);
            float yse = Elevation(x + 1, z + 1);
            float ye = Elevation(x + 1, z);
            float yne = Elevation(x + 1, z - 1);
            float yn = Elevation( x, z - 1 );

            float dyx = ynw + yw + ysw - yne - ye - yse;
            float dyz = ysw + ys + yse - ynw - yn - yne;

            return Vector3.Normalize( new Vector3(dyx, 6, -dyz));
        }

        protected override void LoadContent()
        {
            // vertex type declaration to be shared by all terrain patches
            if (PatchVertexDeclaration == null)
            {
                PatchVertexDeclaration = new VertexDeclaration(this.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
                PatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }

            // Set up one indexBuffer to be shared by all terrain patches
            if (PatchIndexBuffer == null)
            {
                int indexCount = 16 * 16 * 2 * 3;  // 16 x 16 squares * 2 triangles per square * 3 indices per triangle
                short[] indexData = new short[indexCount];

                int iIndex = 0;
                // for each 8 meter rectangle
                for (int x = 0; x < 16; ++x)
                    for (int z = 0; z < 16; ++z)
                    {
                        short nw = (short)(x + z * 17);  // vertice index in the north west corner
                        short ne = (short)(nw + 1);
                        short sw = (short)(nw + 17);
                        short se = (short)(sw + 1);

                        if (((x & 1) == (z & 1)))  // triangles alternate
                        {
                            indexData[iIndex++] = nw;
                            indexData[iIndex++] = sw;
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = ne;
                            indexData[iIndex++] = nw;
                        }
                        else
                        {
                            indexData[iIndex++] = sw;
                            indexData[iIndex++] = se;
                            indexData[iIndex++] = ne;
                            indexData[iIndex++] = ne;
                            indexData[iIndex++] = nw;
                            indexData[iIndex++] = sw;
                        }
                    }
                PatchIndexBuffer = new IndexBuffer(GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                PatchIndexBuffer.SetData<short>(indexData);
            }

            PatchTexture = SharedTextureManager.Get(GraphicsDevice, Viewer.Simulator.RoutePath + @"\terrtex\" + terrtexName);

            // Set up the vertex buffer
            VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[17 * 17];
            int iV = 0;
            // for each vertex
            for (int x = 0; x < 17; ++x)
                for (int z = 0; z < 17; ++z)
                {
                    float w = -64 + x * 8;
                    float n = -64 + z * 8;

                    float u = (float)x ; 
                    float v = (float)z ; 


                    // Rotate, Flip, and stretch the texture using the matrix coordinates stored in terrain_patchset_patch 
                    // transform uv by the 2x3 matrix made up of X,Y  W,B  C,H
                    float U = u * W + v * B + X;
                    float V = u * C + v * H + Y;

                     // V represents the north/south shift

                    float y = Elevation(x, z);

                    vertexData[iV].Position = new Vector3(w, y, n);
                    vertexData[iV].TextureCoordinate = new Vector2(U, V);
                    vertexData[iV].Normal = TerrainNormal(x, z);
                    iV++;
                }
            PatchVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
            PatchVertexBuffer.SetData(vertexData);

            TFile = null;  // release
            YFile = null;  // release
            terrtexName = null; // release

            base.LoadContent();
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            int dTileX = TileX - Viewer.Camera.TileX;
            int dTileZ = TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            Matrix xnaPatchMatrix = XNAPatchWorldPosition * xnaDTileTranslation;  // defines location and pose for patch

            // Distance cull
            if (Viewer.Camera.CanSee(xnaPatchMatrix, 150f, 2000f))
            {
                Viewer.SceneryShader.SetMatrix(xnaPatchMatrix, Viewer.Camera.XNAView, Viewer.Camera.XNAProjection);

                // These settings are constant for all patches so we set up only when needed
                // TODO, there is something wrong here- StaticShape must be called before
                // this works.
                if (Viewer.RenderState != 1)  
                {

                    Viewer.RenderState = 1;
                    GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                    GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexDeclaration = PatchVertexDeclaration;
                    GraphicsDevice.RenderState.DepthBias = 0f;

                    GraphicsDevice.VertexSamplerStates[0].AddressU = TextureAddressMode.Wrap;
                    GraphicsDevice.VertexSamplerStates[0].AddressV = TextureAddressMode.Wrap;
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.Always;


                    GraphicsDevice.VertexDeclaration = PatchVertexDeclaration;
                    GraphicsDevice.Indices = PatchIndexBuffer;

                    Viewer.SceneryShader.CurrentTechnique = Viewer.SceneryShader.Techniques[2];

                    GraphicsDevice.RenderState.AlphaTestEnable = true;
                    GraphicsDevice.RenderState.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become transparent
                    GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;        // if alpha > reference, then skip processing this pixel

                    Viewer.SetupFog();
                }

                // These items change per Draw call
                Viewer.SceneryShader.Texture = PatchTexture;

                GraphicsDevice.Vertices[0].SetSource(this.PatchVertexBuffer, 0, PatchVertexStride);

                
                Viewer.SceneryShader.Begin();
                foreach (EffectPass pass in Viewer.SceneryShader.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,0, 0, 17 * 17, 0, 16 * 16 * 2 );
                    pass.End();
                }
                Viewer.SceneryShader.End();
                 
            }
            base.Draw(gameTime);
        }

    } // Terrain Patch





}
