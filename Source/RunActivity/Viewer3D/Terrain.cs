// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

//#define SUPERSMOOTHNORMALS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public void Load(RenderProcess renderProcess)
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
                            Trace.Write("t");
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
            foreach (TerrainTile tile in TerrainTiles)   // check every tile
                if (tile != null)
                    if (tile.TileX == tileX && tile.TileZ == tileZ)  // return if its the one we want
                        return;

            // find an available spot in the TerrainTiles array
            // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
            for (int i = 0; i < TerrainTiles.Length; ++i)
                if (TerrainTiles[i] == null)  // we found one
                {
                    Trace.Write("T");
                    TerrainTiles[i] = new TerrainTile(Viewer, tileX, tileZ);
                    return;
                }

            // otherwise we didn't find an available spot - this shouldn't happen
            System.Diagnostics.Debug.Assert(false, "Program Bug - didn't expect TerrainTiles array to be full.");
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // THREAD SAFETY WARNING - LoaderProcess could write to this array at any time
            // its OK to iterate through this array because LoaderProcess never changes the size
            foreach (TerrainTile tile in TerrainTiles)
                if (tile != null)
                    if (Viewer.Camera.InFOV(new Vector3((tile.TileX - Viewer.Camera.TileX) * 2048, 0, (tile.TileZ - Viewer.Camera.TileZ) * 2048), 1448))
                        tile.PrepareFrame(frame, elapsedTime);
        }

    } // TerrainDrawer

    public class TerrainTile : IDisposable
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
                if (tile.TFile.ContainsWater)
                    WaterTile = new WaterTile(viewer, TileX, TileZ);


                for (int x = 0; x < 16; ++x)
                    for (int z = 0; z < 16; ++z)
                    {
                        if (!tile.IsEmpty && tile.TFile.terrain.terrain_patchsets[0].GetPatch(x, z).DrawingEnabled)
                        {
                            TerrainPatch patch = new TerrainPatch(viewer, tile, x, z, tileX, tileZ);
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
                    if (patch != null)
                        patch.PrepareFrame(frame);
                }
        }

        #region IDisposable Members

        public void Dispose()
        {
            // TODO finish water
            //if (WaterTile != null)
            //    WaterTile.Dispose();

            for (int x = 0; x < 16; ++x)
                for (int z = 0; z < 16; ++z)
                {
                    TerrainPatch patch = TerrainPatches[x, z];
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
        readonly Viewer3D Viewer;

        readonly int TileX, TileZ;
        readonly Vector3 XNAPatchLocation;        // in XNA world coordinates relative to the center of the tile
        readonly VertexBuffer PatchVertexBuffer;  // separate vertex buffer for each patch
        readonly IndexBuffer PatchIndexBuffer;    // separate index buffer for each patch if there are tunnels
        readonly int PatchPrimitiveCount;
        readonly float AverageElevation;

        public readonly Material PatchMaterial;

        // these can be shared since they are the same for all patches
        public static VertexDeclaration SharedPatchVertexDeclaration;
        public static int SharedPatchVertexStride;  // in bytes

        // these are only used while the contructor runs and are discarded after
        int PatchX, PatchZ;
        Tile Tile;

        float X, Y, W, B, C, H;  // A 2 x 3 matrix for texture translation

        public TerrainPatch(Viewer3D viewer, Tile tile, int x, int z, int tileX, int tileZ)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;
            PatchX = x;
            PatchZ = z;
            Tile = tile;

            var patch = Tile.TFile.terrain.terrain_patchsets[0].GetPatch(x, z);
            var ts = ((terrain_shader)Tile.TFile.terrain.terrain_shaders[patch.iShader]).terrain_texslots;
            if (ts.Length > 1)
                PatchMaterial = Materials.Load(viewer.RenderProcess, "Terrain", Helpers.GetTerrainTextureFile(viewer.Simulator, ts[0].Filename) + "\0" + Helpers.GetTerrainTextureFile(viewer.Simulator, ts[1].Filename));
            else
                PatchMaterial = Materials.Load(viewer.RenderProcess, "Terrain", Helpers.GetTerrainTextureFile(viewer.Simulator, ts[0].Filename));

            float cx = -1024 + (int)patch.CenterX;
            float cz = -1024 - (int)patch.CenterZ;
            XNAPatchLocation = new Vector3(cx, Tile.TFile.Floor, cz);
            X = patch.X;
            Y = patch.Y;
            W = patch.W;
            B = patch.B;
            C = patch.C;
            H = patch.H;

            // vertex type declaration to be shared by all terrain patches
            if (SharedPatchVertexDeclaration == null)
                SetupPatchVertexDeclaration(Viewer.GraphicsDevice);

            PatchIndexBuffer = GetIndexBuffer(out PatchPrimitiveCount);
            PatchVertexBuffer = GetVertexBuffer(out AverageElevation);

            Tile = null;
        }

        public void PrepareFrame(RenderFrame frame)
        {
            int dTileX = TileX - Viewer.Camera.TileX;
            int dTileZ = TileZ - Viewer.Camera.TileZ;
            Vector3 mstsLocation = new Vector3(XNAPatchLocation.X + dTileX * 2048, XNAPatchLocation.Y, -XNAPatchLocation.Z + dTileZ * 2048);
            Matrix xnaPatchMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            mstsLocation.Y += AverageElevation; // Try to keep testing point somewhere useful within the patch's altitude.
            frame.AddAutoPrimitive(mstsLocation, 180f, 2000f, PatchMaterial, this, RenderPrimitiveGroup.World, ref xnaPatchMatrix, ShapeFlags.ShadowCaster);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = TerrainPatch.SharedPatchVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(PatchVertexBuffer, 0, SharedPatchVertexStride);
            graphicsDevice.Indices = PatchIndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 17 * 17, 0, PatchPrimitiveCount);
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

            uint e = Tile.YFile.GetElevationIndex(hx, hz);
            return (float)e * Tile.TFile.Resolution + Tile.TFile.Floor;
        }

        bool IsVertexHidden(int x, int z)
        {
            int hx = PatchX * 16 + x;
            int hz = PatchZ * 16 + z;
            if (hx > 255 || hx < 0 || hz > 255 || hz < 0)
                // its outside this tile, so we will have to look it up
                return Viewer.Tiles.IsVertexHidden(TileX, TileZ, hx, hz);

            return Tile.FFile.IsVertexHidden(hx, hz);
        }

        private Vector3 TerrainNormal(int x, int z)
        {
            Vector3 ourNormal = SpecificTerrainNormal(x, z);

#if !SUPERSMOOTHNORMALS
            return ourNormal;
#else           
            float centerWeight = 0.4f;

            Vector3 n = SpecificTerrainNormal(x, z - 1);
            Vector3 e = SpecificTerrainNormal(x + 1, z);
            Vector3 s = SpecificTerrainNormal(x, z + 1);
            Vector3 w = SpecificTerrainNormal(x - 1, z);
            
            if (x % 2 == z % 2)
            {                
                Vector3 ne = SpecificTerrainNormal(x + 1, z - 1);                
                Vector3 se = SpecificTerrainNormal(x + 1, z + 1);                
                Vector3 sw = SpecificTerrainNormal(x - 1, z + 1);                
                Vector3 nw = SpecificTerrainNormal(x - 1, z - 1);

                float restWeight = 1 - centerWeight;
                float neswWeight = restWeight * 0.66f;

                Vector3 neswAverage = Vector3.Normalize(n + e + s + w) * neswWeight;
                Vector3 othersAverage = Vector3.Normalize(ne + se + sw + nw) * (restWeight - neswWeight);
                Vector3 weighted = Vector3.Normalize((ourNormal * centerWeight) + neswAverage + othersAverage);
                return weighted;
            }
            else
            {
                float restWeight = 1 - centerWeight;
                Vector3 neswAverage = Vector3.Normalize(n + e + s + w) * restWeight;
                Vector3 weighted = Vector3.Normalize((ourNormal * centerWeight) + neswAverage);
                return weighted;
            }
#endif
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
        private Vector3 SpecificTerrainNormal(int x, int z)
        {
            // TODO, decode this from the _N.RAW TILE
            // until I figure out this file, I'll compute normals from the terrain

            const float t = 8;

            float vx = x;
            float vz = z;

            Vector3 center = new Vector3(vx, Elevation(x, z), vz);

            Vector3 n = new Vector3(vx, Elevation(x, z - 1), vz - t); Vector3 toN = Vector3.Normalize(n - center);
            Vector3 e = new Vector3(vx + t, Elevation(x + 1, z), vz); Vector3 toE = Vector3.Normalize(e - center);
            Vector3 s = new Vector3(vx, Elevation(x, z + 1), vz + t); Vector3 toS = Vector3.Normalize(s - center);
            Vector3 w = new Vector3(vx - t, Elevation(x - 1, z), vz); Vector3 toW = Vector3.Normalize(w - center);

            if (x % 2 == z % 2)
            {
                Vector3 ne = new Vector3(vx + t, Elevation(x + 1, z - 1), vz - t); Vector3 toNE = Vector3.Normalize(ne - center);
                Vector3 se = new Vector3(vx + t, Elevation(x + 1, z + 1), vz + t); Vector3 toSE = Vector3.Normalize(se - center);
                Vector3 sw = new Vector3(vx - t, Elevation(x - 1, z + 1), vz + t); Vector3 toSW = Vector3.Normalize(sw - center);
                Vector3 nw = new Vector3(vx - t, Elevation(x - 1, z - 1), vz - t); Vector3 toNW = Vector3.Normalize(nw - center);

                Vector3 nneFaceNormal = Vector3.Normalize(Vector3.Cross(toNE, toN));
                Vector3 eneFaceNormal = Vector3.Normalize(Vector3.Cross(toE, toNE));
                Vector3 eseFaceNormal = Vector3.Normalize(Vector3.Cross(toSE, toE));
                Vector3 sseFaceNormal = Vector3.Normalize(Vector3.Cross(toS, toSE));
                Vector3 sswFaceNormal = Vector3.Normalize(Vector3.Cross(toSW, toS));
                Vector3 wswFaceNormal = Vector3.Normalize(Vector3.Cross(toW, toSW));
                Vector3 wnwFaceNormal = Vector3.Normalize(Vector3.Cross(toNW, toW));
                Vector3 nnwFaceNormal = Vector3.Normalize(Vector3.Cross(toN, toNW));

                Vector3 normal = Vector3.Normalize((nneFaceNormal + eneFaceNormal + eseFaceNormal + sseFaceNormal + sswFaceNormal + wswFaceNormal + wnwFaceNormal + nnwFaceNormal));
                return normal;
            }
            else
            {
                Vector3 neFaceNormal = Vector3.Normalize(Vector3.Cross(toE, toN));
                Vector3 seFaceNormal = Vector3.Normalize(Vector3.Cross(toS, toE));
                Vector3 swFaceNormal = Vector3.Normalize(Vector3.Cross(toW, toS));
                Vector3 nwFaceNormal = Vector3.Normalize(Vector3.Cross(toN, toW));

                return Vector3.Normalize((neFaceNormal + seFaceNormal + swFaceNormal + nwFaceNormal));
            }
        }

        static void SetupPatchVertexDeclaration(GraphicsDevice graphicsDevice)
        {
            SharedPatchVertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
            SharedPatchVertexStride = VertexPositionNormalTexture.SizeInBytes;
        }

        IndexBuffer GetIndexBuffer(out int primitiveCount)
        {
            // 16 x 16 squares * 2 triangles per square * 3 indices per triangle
            var indexData = new List<short>(16 * 16 * 2 * 3);

            // for each 8 meter rectangle
            for (var z = 0; z < 16; ++z)
                for (var x = 0; x < 16; ++x)
                {
                    var nw = (short)(z * 17 + x);  // vertice index in the north west corner
                    var ne = (short)(nw + 1);
                    var sw = (short)(nw + 17);
                    var se = (short)(sw + 1);

                    if (((z & 1) == (x & 1)))  // triangles alternate
                    {
                        if (!IsVertexHidden(x, z) && !IsVertexHidden(x + 1, z + 1) && !IsVertexHidden(x, z + 1))
                        {
                            indexData.Add(nw);
                            indexData.Add(se);
                            indexData.Add(sw);
                        }
                        if (!IsVertexHidden(x, z) && !IsVertexHidden(x + 1, z) && !IsVertexHidden(x + 1, z + 1))
                        {
                            indexData.Add(nw);
                            indexData.Add(ne);
                            indexData.Add(se);
                        }
                    }
                    else
                    {
                        if (!IsVertexHidden(x + 1, z) && !IsVertexHidden(x + 1, z + 1) && !IsVertexHidden(x, z + 1))
                        {
                            indexData.Add(ne);
                            indexData.Add(se);
                            indexData.Add(sw);
                        }
                        if (!IsVertexHidden(x, z) && !IsVertexHidden(x + 1, z) && !IsVertexHidden(x, z + 1))
                        {
                            indexData.Add(nw);
                            indexData.Add(ne);
                            indexData.Add(sw);
                        }
                    }
                }

            primitiveCount = indexData.Count / 3;
            var indexBuffer = new IndexBuffer(Viewer.GraphicsDevice, sizeof(short) * indexData.Count, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData(indexData.ToArray());
            return indexBuffer;
        }

        VertexBuffer GetVertexBuffer(out float averageElevation)
        {
            var totalElevation = 0f;
            var vertexData = new List<VertexPositionNormalTexture>(17 * 17);

            for (int z = 0; z < 17; ++z)
                for (int x = 0; x < 17; ++x)
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

                    float y = Elevation(x, z) - Tile.TFile.Floor;
                    totalElevation += y;

                    vertexData.Add(new VertexPositionNormalTexture(new Vector3(w, y, n), TerrainNormal(x, z), new Vector2(U, V)));
                }

            averageElevation = totalElevation / vertexData.Count;
            var patchVertexBuffer = new VertexBuffer(Viewer.GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexData.Count, BufferUsage.WriteOnly);
            patchVertexBuffer.SetData(vertexData.ToArray());
            return patchVertexBuffer;
        }
    } // Terrain Patch
} // namespace
