/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///

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
        private int lastViewerTileX, lastViewerTileZ;  // have we moved since the last load call?
        private Viewer3D Viewer;

        public TerrainDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
            for (int i = 0; i < TerrainTiles.Length; ++i)
                TerrainTiles[i] = null;
        }

        /// <summary>
        /// Called 10 times per second when its safe to read volatile data
        /// from the simulator and viewer classes in preparation
        /// for the Load call.  Copy data to local storage for use 
        /// in the next load call.
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            viewerTileX = Viewer.Camera.TileX;
            viewerTileZ = Viewer.Camera.TileZ;
        }

        /// <summary>
        /// Called 10 times a second to load graphics content
        /// that comes and goes as the player and trains move.
        /// Called from background LoaderProcess Thread
        /// Do not access volatile data from the simulator 
        /// and viewer classes during the Load call ( see
        /// LoadPrep() )
        /// Executes in the LoaderProcess thread.
        /// </summary>
        public void Load( RenderProcess renderProcess )
        {
            if (viewerTileX != lastViewerTileX || viewerTileZ != lastViewerTileZ)   // if the camera has moved into a new tile
            {
                lastViewerTileX = viewerTileX;
                lastViewerTileZ = viewerTileZ;

                // remove any tiles out of range
                // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
                for (int i = 0; i < TerrainTiles.Length; ++i)
                {
                    TerrainTile tile = TerrainTiles[i];
                    if (tile != null)
                    {
                        if (Math.Abs(tile.TileX - viewerTileX) > 1
                          || Math.Abs(tile.TileZ - viewerTileZ) > 1)
                        {
                            Console.Write("t");
                            TerrainTiles[i] = null;  // make it invisible to UpdateProcess
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
            // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
            for( int i = 0; i < TerrainTiles.Length; ++i )
                if ( TerrainTiles[i] == null)  // we found one
                {
                    Console.Write("T");
                    TerrainTiles[i] = new TerrainTile( Viewer, tileX, tileZ);
                    return;
                }

            // otherwise we didn't find an available spot - this shouldn't happen
            System.Diagnostics.Debug.Assert( false, "Program Bug - didn't expect TerrainTiles array to be full.");
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // THREAD SAFETY WARNING - LoaderProcess could write to this array at any time
            // its OK to iterate through this array because LoaderProcess never changes the size
            foreach (TerrainTile tile in TerrainTiles)
                if (tile != null)
                    tile.PrepareFrame(frame, elapsedTime);
        }

    } // TerrainDrawer
    
    public class TerrainTile: IDisposable
    {
        public int TileX, TileZ;
        private TerrainPatch[,] TerrainPatches = new TerrainPatch[16, 16];
        private Viewer3D Viewer;
        private WaterTile WaterTile = null;

        public TerrainTile(Viewer3D viewer, int tileX, int tileZ)
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
                            Thread.Sleep(5);
                            TerrainPatch patch = new TerrainPatch(viewer, TFile, tile.YFile, x, z, tileX, tileZ);
                            TerrainPatches[x, z] = patch;
                        }
                        else
                        {
                           TerrainPatches[x, z] = null;
                        }
                    }
            }
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (WaterTile != null)
                WaterTile.PrepareFrame(frame);
            for (int x = 0; x < 16; ++x)
                for (int z = 0; z < 16; ++z)
                {
                    TerrainPatch patch = TerrainPatches[x, z];
                    if( patch != null)
                        patch.PrepareFrame(frame);
                }
        }

        #region IDisposable Members

        public void Dispose()
        {
            // TODO finish water
            //if (WaterTile != null)
            //    WaterTile.Dispose();

            for( int x = 0; x < 16; ++x )
                for( int z = 0; z < 16; ++z )
                {
                    TerrainPatch patch = TerrainPatches[x,z];
                    if (patch != null)
                    {
                        TerrainPatches[x, z] = null;
                        // TODO handle unload patch.Dispose();
                    }
                }
        }

        #endregion
    } // Terrain Tile

    
    public class TerrainPatch : RenderPrimitive
    {
        Viewer3D Viewer;

        private int TileX, TileZ;               
        private Vector3 XNAPatchLocation;      // in XNA world coordinates relative to the center of the tile
        private Matrix XNAPatchPosition;       // computed from above to save time in the Draw routine
        private VertexBuffer PatchVertexBuffer;  // separate vertex buffer for each patch

        public Material PatchMaterial;

        // these can be shared since they are the same for all patches
        public static VertexDeclaration PatchVertexDeclaration = null; 
        public static IndexBuffer PatchIndexBuffer = null;
        public static int PatchVertexStride;  // in bytes

        // these are only used while the contructor runs and are discarded after
        int PatchX, PatchZ;
        TFile TFile;
        YFile YFile;
        
        float X,Y, W, B, C, H;  // A 2 x 3 matrix for texture translation

        public TerrainPatch(Viewer3D viewer, TFile tFile, YFile yFile, int x, int z, int tileX, int tileZ)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;
            PatchX = x;
            PatchZ = z;
            TFile = tFile;
            YFile = yFile;
            int weather = (int)Viewer.Simulator.Weather;
            int season = (int)Viewer.Simulator.Season;

            terrain_patchset_patch patch = tFile.terrain.terrain_patchsets[0].GetPatch(x, z);
            terrain_shader terrain_shader = (terrain_shader)tFile.terrain.terrain_shaders[patch.iShader];
            string terrtexName = terrain_shader.terrain_texslots[0].Filename;

            if (weather == (int)WeatherType.Snow || season == (int)SeasonType.Winter)
            {
                // Make sure there's a "snow" counterpart to the terrtex. If not, use the regular terrtex.
                if (File.Exists(Viewer.Simulator.RoutePath + @"\terrtex\snow\" + terrtexName))
                    PatchMaterial = Materials.Load(viewer.RenderProcess, "Terrain", Viewer.Simulator.RoutePath + @"\terrtex\snow\" + terrtexName);
                else
                    PatchMaterial = Materials.Load(viewer.RenderProcess, "Terrain", Viewer.Simulator.RoutePath + @"\terrtex\" + terrtexName);
            }
            else
                PatchMaterial = Materials.Load(viewer.RenderProcess, "Terrain", Viewer.Simulator.RoutePath + @"\terrtex\" + terrtexName);

            float cx =  -1024+(int)patch.CenterX;
            float cz =  -1024-(int)patch.CenterZ;
            XNAPatchLocation = new Vector3(cx, 0, cz);
            XNAPatchPosition = Matrix.CreateTranslation(XNAPatchLocation);
            X = patch.X;
            Y = patch.Y;
            W = patch.W;
            B = patch.B;
            C = patch.C;
            H = patch.H;

            // vertex type declaration to be shared by all terrain patches
            if (PatchVertexDeclaration == null)
            {
                SetupPatchVertexDeclaration();
            }

            // Set up one indexBuffer to be shared by all terrain patches
            if (PatchIndexBuffer == null)
            {
                SetupPatchIndexBuffer();
            }

            SetupVertexBuffer();

            TFile = null;
            YFile = null;
        }

        public void PrepareFrame(  RenderFrame frame )
        {
            int dTileX = TileX - Viewer.Camera.TileX;
            int dTileZ = TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            Matrix xnaPatchMatrix = XNAPatchPosition * xnaDTileTranslation;  // defines location and pose for patch

            // Distance cull
            if (Viewer.Camera.CanSee(xnaPatchMatrix, 150f, 2000f))
            {
                frame.AddPrimitive(PatchMaterial, this, ref xnaPatchMatrix);
            }
        }



        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // TODO ADD THESE LINES USING EXPERIMENTAL FAST MATERIALS
            // graphicsDevice.VertexDeclaration = TerrainPatch.PatchVertexDeclaration;
            // graphicsDevice.Indices = TerrainPatch.PatchIndexBuffer;

            graphicsDevice.Vertices[0].SetSource(this.PatchVertexBuffer, 0, PatchVertexStride);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,0, 0, 17 * 17, 0, 16 * 16 * 2 );
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
            int hx = PatchX * 16 + x;
            int hz = PatchZ * 16 + z;
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

            float ynw = Elevation(x - 1, z - 1);
            float yw = Elevation(x - 1, z);
            float ysw = Elevation(x - 1, z + 1);
            float ys = Elevation(x, z + 1);
            float yse = Elevation(x + 1, z + 1);
            float ye = Elevation(x + 1, z);
            float yne = Elevation(x + 1, z - 1);
            float yn = Elevation(x, z - 1);

            float dyx = ynw + yw + ysw - yne - ye - yse;
            float dyz = ysw + ys + yse - ynw - yn - yne;

            return Vector3.Normalize(new Vector3(dyx, 6, -dyz));
        }

        private void SetupPatchVertexDeclaration()
        {
            PatchVertexDeclaration = new VertexDeclaration(Viewer.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            PatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
        }

        private void SetupPatchIndexBuffer()
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
            PatchIndexBuffer = new IndexBuffer(Viewer.GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            PatchIndexBuffer.SetData<short>(indexData);
        }

        private void SetupVertexBuffer()
        {

            VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[17 * 17];
            int iV = 0;
            // for each vertex
            for (int x = 0; x < 17; ++x)
                for (int z = 0; z < 17; ++z)
                {
                    float w = -64 + x * 8;
                    float n = -64 + z * 8;

                    float u = (float)x;
                    float v = (float)z;


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
            PatchVertexBuffer = new VertexBuffer(Viewer.GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Length, BufferUsage.WriteOnly);
            PatchVertexBuffer.SetData(vertexData);
        }


    } // Terrain Patch


} // namespace
