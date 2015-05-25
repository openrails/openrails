// COPYRIGHT 2014, 2015 by the Open Rails project.
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

// These classes are about rendering the terrain textures in a 2D fashion below the tracks
// Rendering terrain in this way is very different from rendering terrain in the simulator in a number of ways.
// In the simulator normally only a few patches are being drawn. And for each patch 16x16 different height points are being used.
// Each height point is a different vertex. The texture is then wrapped over these vertices.
// Here, in contrast, we do not care about height. So each texture only needs to be drawn over a single quad (two triangles).
// Although the 4 vertices at the corners can be reused in principle for the other patches at the same corner, the texture UV
// values can not. Normally vertices can be reused because both the vertex location and the location in the texture are the same.
// Here the location in the texture (and actually even the texture) is unique. So there is not re-using of vertices.
// Because we are not re-using vertices over different textures, we also do not care about reusing them for a single texture.
// Therefore, we actually use 6 vertices (2 triangles) without reuse.
//
// Another big difference is that we must have in memory all textures in the visible region (which might be the whole route!).
// The good thing is that if there is a lot of re-using of textures (e.g. in the case no photo-textures are used) drawing the whole
// route is not very difficult: many vertices using the same tertex, which the GPU is capable of doing quite well.
//
// The terrain textures are drawn using normal3D techniques. All terrain is taken to be in the X-Z plane (so Y=0). The camera
// is exacly above the middle of the 'screen' area at a height and view frustrum such that the 3D view exactly matches the grid
// lines (that are drawn in 2D).
//
// Because of the large memory of all textures only those tiles and their needed textures are loaded that are in the visible range.
// Once the tiles are loaded, they stay in memory (and will therefore be drawn). 

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.Formats.Msts;
using ORTS.Viewer3D;
using ORTS.Common;


namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// Main class to draw the terrain as from far above (so in an effective 2D fashion.
    /// Drawing lines around the patches (each tile is divided in a number, typically 16x16, patches) is also supported
    /// </summary>
    public class DrawTerrain
    {
        #region Properties

        //injection dependency properties
        private MessageDelegate messageDelegate;

        //Directory paths of various formats and files
        string tilesPath;
        string lotilesPath;
        string terrtexPath;

        //basic graphics
        private GraphicsDevice device;
        private BasicEffect basicEffect;
        private VertexDeclaration vertexDeclaration;

        //Managin textures
        private TerrainTextureManager textureManager;

        // Storing our own generated data for viewing
        private Dictionary<uint, TerrainTile2D> terrainTiles;
        private Dictionary<uint, TerrainTile2D> terrainTiles2;  // those with zoom level 2;
        private HashSet<uint> emptyTerrainTiles;
        private Dictionary<string, VertexBuffer> vertexBuffers;
        private Dictionary<string, int> vertexBufferCounts;

        //visibility of terrain and patchlines
        private bool showPatchLines;

        //drawArea
        private Translate3Dto2D locationTranslator;
        private int visibleTileXmin;
        private int visibleTileXmax;
        private int visibleTileZmin;
        private int visibleTileZmax;
        #endregion

        #region Public methods
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routePath">Path to the route directory</param>
        /// <param name="messageDelegate">The delegate that will present with the message we want to send to the user during long loading times</param>
        /// <param name="drawWorldTiles">The object that knows which tiles are available</param>
        public DrawTerrain(string routePath, MessageDelegate messageDelegate, DrawWorldTiles drawWorldTiles)
        {
            this.messageDelegate = messageDelegate;
            this.tilesPath = routePath + @"\TILES\";
            this.lotilesPath = routePath + @"\LO_TILES\";
            this.terrtexPath = routePath + @"\TERRTEX\";

            locationTranslator = new Translate3Dto2D(drawWorldTiles);
            terrainTiles = new Dictionary<uint, TerrainTile2D>();
            terrainTiles2 = new Dictionary<uint, TerrainTile2D>();
            emptyTerrainTiles = new HashSet<uint>();
        }

        /// <summary>
        /// Load some basic content (regular XNA method), mainly just initialization.
        /// This does not load tiles nor textures (because those are only loaded on demand, depending on visual area). 
        /// </summary>
        /// <param name="graphicsDevice">The graphsics device used for XNA drawing, and also texture importing</param>
        public void LoadContent(GraphicsDevice graphicsDevice)
        {
            this.device = graphicsDevice;
            basicEffect = new BasicEffect(this.device, null)
            {
                TextureEnabled = true,
                World = Matrix.Identity
            };
            this.vertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            textureManager = new TerrainTextureManager(this.terrtexPath, this.device, this.messageDelegate);

            DiscardVertexBuffers();
        }

        /// <summary>
        /// Set whether the terrain is visible (drawn) or not.
        /// </summary>
        /// <param name="isVisible">Terrain will be drawn if this is set to yes</param>
        /// <param name="drawArea">The area currently visible and for which all tiles and textures need to be loaded</param>
        public void SetTerrainVisibility(bool isVisible, DrawArea drawArea)
        {
            DiscardVertexBuffers();
            if (isVisible)
            {
                if (drawArea == null)
                {
                    return;
                }
                DetermineVisibleArea(drawArea);
                EnsureAllTilesAreLoaded();
                CreateVertexBuffers(this.showPatchLines);
            }
        }

        /// <summary>
        /// Set whether the lines between the patches are visible or not
        /// </summary>
        public void SetPatchLineVisibility(bool isVisible)
        {
            this.showPatchLines = isVisible;
            DiscardVertexBuffers();
            CreateVertexBuffers(isVisible);
        }
        #endregion

        #region Area and visibility handling
        /// <summary>
        /// Find and store the current visible area
        /// </summary>
        void DetermineVisibleArea(DrawArea drawArea)
        {
            WorldLocation upperLeft = drawArea.LocationUpperLeft;
            WorldLocation lowerRight = drawArea.LocationLowerRight;
            visibleTileXmin = upperLeft.TileX;
            visibleTileXmax = lowerRight.TileX;
            visibleTileZmin = lowerRight.TileZ;
            visibleTileZmax = upperLeft.TileZ;
        }

        /// <summary>
        /// Translate a tile location given by tileX and tileZ into a single uint index
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        uint TileIndex(int tileX, int tileZ)
        {
            return ((uint)tileX << 16) + (uint)tileZ;
        }
        #endregion

        #region Loading
        /// <summary>
        /// Make sure all tiles that had not been loaded are loaded now.
        /// </summary>
        public void EnsureAllTilesAreLoaded()
        {
            for (int tileX = visibleTileXmin; tileX <= visibleTileXmax; tileX++)
            {
                for (int tileZ = visibleTileZmin; tileZ <= visibleTileZmax; tileZ++)
                {
                    EnsureTileIsLoaded(tileX, tileZ);
                }
            }
        }

        /// <summary>
        /// Make sure the Tile and its needed textures are loaded. This means checking if it is already loaded, 
        /// and if it is not loaded, load it.
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        void EnsureTileIsLoaded(int tileX, int tileZ)
        {
            uint index = TileIndex(tileX, tileZ);
            if (emptyTerrainTiles.Contains(index) || terrainTiles.ContainsKey(index) || terrainTiles2.ContainsKey(index))
            {
                return;
            }
            Tile newTile = LoadTile(tileX, tileZ);
            if (newTile == null)
            {
                emptyTerrainTiles.Add(index);
                return;
            }

            var newTerrainTile = new TerrainTile2D(newTile, textureManager, locationTranslator);
            if (newTile.Size == 1)
            {
                terrainTiles.Add(index, newTerrainTile);
            }
            else
            {
                terrainTiles2.Add(index, newTerrainTile);
            }
        }

        /// <summary>
        /// Load the information for a single tile (as in parse the needed .t -file.
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        /// <returns>The tile information as a 'Tile' object</returns>
        private Tile LoadTile(int tileX, int tileZ)
        {
            // Note, code is similar to ORTS.Viewer3D.TileManager.Load
            // Check for 1x1 tiles.
            TileName.Snap(ref tileX, ref tileZ, TileName.Zoom.Small);

            // we set visible to false to make sure errors are loaded
            Tile newTile = new Tile(tilesPath, tileX, tileZ, TileName.Zoom.Small, false);
            if (newTile.Loaded)
            {
                return newTile;
            }
            else
            {
                // Check for 2x2 tiles.
                TileName.Snap(ref tileX, ref tileZ, TileName.Zoom.Large);
                newTile = new Tile(tilesPath, tileX, tileZ, TileName.Zoom.Large, false);
                if (newTile.Loaded)
                {
                    return newTile;
                }
            }

            return null;

        }

        /// <summary>
        /// Create the texture-name-indexed vertex buffers that the Graphics card will need.
        /// The vertex buffers are created from information pre-calculated in stored 'TerrainTile2D' objects.
        /// </summary>
        /// <param name="showPatchLines">If patchLines need to be shown, we need to load different vertices</param>
        void CreateVertexBuffers(bool showPatchLines)
        {
            foreach (string textureName in textureManager.Keys)
            {
                //It seems that this implementation has quite some copying of data of vertices, but I am not sure how to prevent this.
                var vertices = new List<VertexPositionTexture>();
                
                //make sure we first draw those tiles with zoom level 2
                foreach (TerrainTile2D terrainTile in terrainTiles2.Values)
                {
                    var additionalVertices = terrainTile.GetVertices(textureName, showPatchLines);
                    vertices.AddRange(additionalVertices);
                }

                foreach (TerrainTile2D terrainTile in terrainTiles.Values)
                {
                    var additionalVertices = terrainTile.GetVertices(textureName, showPatchLines);
                    vertices.AddRange(additionalVertices);
                }
                VertexBuffer buffer = new VertexBuffer(this.device, VertexPositionTexture.SizeInBytes * vertices.Count(), BufferUsage.WriteOnly);
                int vertexCount = vertices.Count();
                buffer.SetData(vertices.ToArray(), 0, vertexCount);
                vertexBuffers.Add(textureName, buffer);
                vertexBufferCounts.Add(textureName, vertexCount);
            }

        }

        /// <summary>
        /// Discard the vertexbuffers. No active signal is sent to the Graphics card to discard, but since we are not using the buffers anymore, 
        /// this should be save enough (we are not re-using the buffers until the user decides to draw terrain again).
        /// </summary>
        void DiscardVertexBuffers()
        {
            vertexBuffers = new Dictionary<string, VertexBuffer>();
            vertexBufferCounts = new Dictionary<string, int>();
        }        
        #endregion 

        #region Drawing
        /// <summary>
        /// This is the method that does the actual drawing of the terrain
        /// </summary>
        /// <param name="drawArea">The area to draw upon (with physical location to pixel transformations)</param>
        public void Draw(DrawArea drawArea)
        {
            UpdateCamera(drawArea);

            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

            foreach (string textureName in vertexBuffers.Keys)
            {
                basicEffect.Texture = textureManager[textureName];
                basicEffect.Begin();
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.VertexDeclaration = this.vertexDeclaration;
                    device.Vertices[0].SetSource(vertexBuffers[textureName], 0, VertexPositionTexture.SizeInBytes);
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBufferCounts[textureName]);
                    pass.End();
                }
                basicEffect.End();
            }
        }

        /// <summary>
        /// Place the (3D) camera such that the 3D terrain is drawn in exactly the same location as the other 2D elements
        /// </summary>
        /// <param name="drawArea">The area to draw upon (with physical location to pixel transformations)</param>
        void UpdateCamera(DrawArea drawArea)
        {
            // We create a view and projection matrix to allow viewing the world/terrain from the top.
            // All Vertices will be in real-world locations relative to a certain reference location

            //Using Pi/2 for projection, the distance from camera to plane is the same as the half the distance from top to bottom in the screen.
            //
            // So if the vertex positions are real world-locations, the camera-target should be at (worldCenterX, 0, worldCenterZ).
            // The Cameraposition itself is (world-center-X, cam-height, world-centerZ)
            //      where camheight is (worldHeight/2 = worldWidth/aspectRatio/2).
            // The distance of camera can be very large, so the backplane has to be set accordingly: cam-height/2 and cam-height*2.

            WorldLocation upperLeft = drawArea.LocationUpperLeft;
            WorldLocation lowerRight = drawArea.LocationLowerRight;
            Vector3 groundUpperLeft = locationTranslator.VertexPosition(upperLeft);
            Vector3 groundLowerRight = locationTranslator.VertexPosition(lowerRight);
            Vector3 cameraTarget = (groundUpperLeft + groundLowerRight) / 2;
            float width = groundLowerRight.X - groundUpperLeft.X;
            float camHeight = width / device.Viewport.AspectRatio / 2;
            Vector3 cameraPosition = cameraTarget;
            cameraPosition.Y = -camHeight;
            basicEffect.View = Matrix.CreateLookAt(cameraPosition, cameraTarget, new Vector3(0, 0, 1));
            basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, device.Viewport.AspectRatio, camHeight / 2, camHeight * 2);

        }
        #endregion
    }

    #region class Translate3Dto2D
    /// <summary>
    /// This class translates a 3D WorldLocation (tile-based), into a 2D location (without tile reference) for the flat terrain (in X-Z plane) 
    /// </summary>
    class Translate3Dto2D
    {
        private int referenceTileX;
        private int referenceTileZ;

        /// <summary>
        /// Constructor. The main thing done in this constructor is determining the reference tile used for calculating from 3D tile-based to effectively 2D without tiles.
        /// </summary>
        /// <param name="worldTiles">The object that know what tiles are in principle available, so a reference location can be chosen nicely in the middle</param>
        public Translate3Dto2D(DrawWorldTiles worldTiles)
        {
            int TileXmin = int.MaxValue;
            int TileXmax = int.MinValue;
            int TileZmin = int.MaxValue;
            int TileZmax = int.MinValue;
            worldTiles.DoForAllTiles(new TileDelegate((tileX, tileZ) =>
            {
                if (tileX > TileXmax) { TileXmax = tileX; }
                if (tileX < TileXmin) { TileXmin = tileX; }
                if (tileZ > TileZmax) { TileZmax = tileZ; }
                if (tileZ < TileZmin) { TileZmin = tileZ; }
            }));
            referenceTileX = (TileXmax + TileXmin) / 2;
            referenceTileZ = (TileZmax + TileZmin) / 2;
        }

        /// <summary>
        /// Translate a World-Location to a Vertex position, without taking height into account.
        /// This means that we take the (cornerIndexX,0,cornerIndexZ) vector between the location and some (center of) a reference tile.
        /// </summary>
        /// <param name="location">Source World location</param>
        public Vector3 VertexPosition(WorldLocation location)
        {
            WorldLocation normalizedLocation = new WorldLocation(location);
            normalizedLocation.NormalizeTo(referenceTileX, referenceTileZ);
            return new Vector3(normalizedLocation.Location.X, 0, normalizedLocation.Location.Z);
        }
    }
    #endregion

    #region class TerrainTextureManager
    /// <summary>
    /// The manager that loads and stores the various terrain textures. Since normally the textures are shared over multiple tiles,
    /// we want to store them only once.
    /// </summary>
    class TerrainTextureManager : Dictionary<string, Texture2D>
    {
        private HashSet<string> unloadableTerrainTextures;
        private string terrtexPath;
        private MessageDelegate messageDelegate;
        private GraphicsDevice device;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="terrtexPath">Directory path where the textures are located</param>
        /// <param name="device">The graphics device used to import textures into Texture2D</param>
        /// <param name="messageDelegate">The delegate used to draw an on-screen message to the user during longer loading sessions</param>
        public TerrainTextureManager(string terrtexPath, GraphicsDevice device, MessageDelegate messageDelegate) : base() {
            this.unloadableTerrainTextures = new HashSet<string>();
            this.device = device;
            this.messageDelegate = messageDelegate;
            this.terrtexPath = terrtexPath;
        }

        /// <summary>
        /// Return whether a texture is loaded or not.
        /// </summary>
        /// <param name="filename">The filename (without path) of the texture</param>
        public bool TextureIsLoaded(string filename)
        {
            if (this.ContainsKey(filename))
            {
                return true;
            }

            if (unloadableTerrainTextures.Contains(filename))
            {
                return false;
            }

            string path = terrtexPath + filename;
            if (System.IO.File.Exists(path))
            {
                messageDelegate(TrackViewer.catalog.GetString("Loading terrain data ...") + filename);
                this[filename] = Orts.Formats.Msts.ACEFile.Texture2DFromFile(this.device, path);
                return true;
            }

            unloadableTerrainTextures.Add(filename);
            return false;
        }
    }
    #endregion

    #region class TerrainTile2D
    /// <summary>
    /// Class to store information of a single tile needed for drawing the terrain
    /// This will not actually do the drawing itself, because it is more efficient to combine all vertices of all tiles that use
    /// the same texture (only one draw call is then needed).
    /// </summary>
    class TerrainTile2D
    {
        /// <summary>Storing the list of pre-calculated vertices when the textures are drawn fully</summary>
        private Dictionary<string, VertexPositionTexture[]> verticesFull;
        ///<summary>Storing the list of pre-calculated vertices when the textures are drawn with a border to indicate patch lines</summary>
        private Dictionary<string, VertexPositionTexture[]> verticesLine;
        
        //Injection dependencies
        private TerrainTextureManager textureManager;
        private Translate3Dto2D locationTranslator;

        //used during construction:
        private Dictionary<string, List<VertexPositionTexture>> newVertices;
        float squaresPerPatch;

        /// <summary>
        /// Constructor. This will pre-calculate all needed vertices
        /// </summary>
        /// <param name="tile">The tile (parsed .t-file)</param>
        /// <param name="textureManager">The manager for the textures</param>
        /// <param name="locationTranslator">The translator for mapping 3D tile-based coordinates to quasi-2D locations</param>
        public TerrainTile2D(Tile tile, TerrainTextureManager textureManager, Translate3Dto2D locationTranslator)
        {
            this.textureManager = textureManager;
            this.locationTranslator = locationTranslator;
            verticesFull = CreateVerticesFromTile(tile, false);
            verticesLine = CreateVerticesFromTile(tile, true);
        }

        /// <summary>
        /// Return an array of the vertices needed to draw the terrain of this tile.
        /// </summary>
        /// <param name="textureName">The texture name for which the vertices need to be returned</param>
        /// <param name="showPatchLines">Whether or not patch lines need to be drawn.</param>
        public VertexPositionTexture[] GetVertices(string textureName, bool showPatchLines)
        {
            Dictionary<string, VertexPositionTexture[]> vertices = showPatchLines ? verticesLine : verticesFull;
            if (vertices.ContainsKey(textureName))
            {
                return vertices[textureName];
            }
            else
            {
                return new VertexPositionTexture[0];
            }
        }

        /// <summary>
        /// Calculate the vertices for the whole tile
        /// </summary>
        /// <param name="tile">The tile (parsed .t-file)</param>
        /// <param name="showPatchLines">Whether or not patch lines need to be drawn.</param>
        /// <return>A dictionary with texture-name indexed arrays of vertices</return>
        private Dictionary<string, VertexPositionTexture[]> CreateVerticesFromTile(Tile tile, bool showPatchLines)
        {
            squaresPerPatch = showPatchLines ? 15.8f : 16;

            newVertices = new Dictionary<string, List<VertexPositionTexture>>();

            for (int x = 0; x < tile.PatchCount; ++x)
            {
                for (int z = 0; z < tile.PatchCount; ++z)
                {
                    var patch = tile.GetPatch(x, z);
                    CreateVerticesFromPatch(tile, patch);
                }
            }

            var vertices = new Dictionary<string, VertexPositionTexture[]>();
            foreach (string textureName in newVertices.Keys)
            {
                vertices.Add(textureName, newVertices[textureName].ToArray());
            }
            newVertices = null;

            return vertices;
        }

        /// <summary>
        /// Calculate the vertices for a single patch in the tile
        /// </summary>
        /// <param name="tile">The tile (parsed .t-file)</param>
        /// <param name="patch">The terrain patch (one of the patches in the tile)</param>
        private void CreateVerticesFromPatch(Tile tile, terrain_patchset_patch patch)
        {
            var ts = tile.Shaders[patch.ShaderIndex].terrain_texslots;
            string textureName = ts[0].Filename;

            if (!textureManager.TextureIsLoaded(textureName))
            {   // apparently the texture was not found earlier on, so no use bothering about vertices
                return;
            }

            if (!newVertices.ContainsKey(textureName))
            {   // in case this is the first time we use this particular texture
                newVertices.Add(textureName, new List<VertexPositionTexture>());
            }

            // 1 square or quad is two triangles
            CreateSingleCornerVertex(tile, patch, 0, 0, textureName);
            CreateSingleCornerVertex(tile, patch, 1, 0, textureName);
            CreateSingleCornerVertex(tile, patch, 0, 1, textureName);

            CreateSingleCornerVertex(tile, patch, 0, 1, textureName);
            CreateSingleCornerVertex(tile, patch, 1, 0, textureName);
            CreateSingleCornerVertex(tile, patch, 1, 1, textureName);

        }

        /// <summary>
        /// Create a single vertex for a corner in the patch
        /// </summary>
        /// <param name="tile">The tile (parsed .t-file)</param>
        /// <param name="patch">The terrain patch (one of the patches in the tile)</param>
        /// <param name="cornerIndexX">Defines the x-value of the corner (0 or 1)</param>
        /// <param name="cornerIndexZ">Defines the z-value of the corner (0 or 1)</param>
        /// <param name="textureName">The name of the texture</param>
        private void CreateSingleCornerVertex(Tile tile, terrain_patchset_patch patch, float cornerIndexX, float cornerIndexZ, string textureName)
        {
            cornerIndexX *= squaresPerPatch;
            cornerIndexZ *= squaresPerPatch;

            var step = tile.SampleSize;
            float cornerX = patch.CenterX - 1024;
            float cornerZ = patch.CenterZ - 1024 + 2048 * tile.Size;
            cornerX += -patch.RadiusM + cornerIndexX * step;
            cornerZ += -patch.RadiusM + (squaresPerPatch - cornerIndexZ) * step;
            var location = new WorldLocation(tile.TileX, tile.TileZ, cornerX, 0, cornerZ);

            // Rotate, Flip, and stretch the texture using the matrix coordinates stored in terrain_patchset_patch 
            // transform uv by the 2x3 matrix made up of X,Y  W,B  C,H
            var U = cornerIndexX * patch.W + cornerIndexZ * patch.B + patch.X;
            var V = cornerIndexX * patch.C + cornerIndexZ * patch.H + patch.Y;

            newVertices[textureName].Add(new VertexPositionTexture(locationTranslator.VertexPosition(location), new Vector2(U, V)));
        }
    }
#endregion

    
   
}
