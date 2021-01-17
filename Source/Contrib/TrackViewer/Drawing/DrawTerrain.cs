// COPYRIGHT 2014, 2018 by the Open Rails project.
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

// Dealing with 2x2, 8x8 and 16x16 tiles:
// Foreach tile there might be a corresponding terrainTile. However, in case of 2x2 and larger terrainTiles, multiple normal tiles share
// a common terrainTile. For the simulator that is not so relevant, you just make sure the correct terrainTiles is used.
// There is another complication. Any normal tile can be part of a larger terrainTile, even when it also has a 1x1 tile present.
// When you load a terrainTile for that normal tile, the smallest possible zoom-level is loaded 
// We want to make sure that each tile is only loaded once, and that all bigger tiles are only stored once.
// For this first we have keep track of which normal tiles have their corresponding terrainTile (whatever its zoom) is loaded.
// Then, to make sure each tile is stored only once we have to have a unique identifier for the tile.
// This unique identifier is based on three things:
//  * the snapped tileX (for 1x1 tile this is just the tileX, for 2x2 tiles it is snapped to multiples of 2, for 8x8 tiles snapped to multiples of 8, ...)
//  * the snapped tileZ
//  * the zoom size (1, 2, 8, 16)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.Formats.Msts;
using Orts.Viewer3D;
using ORTS.Common;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// Main class to draw the terrain as from far above (so in an effective 2D fashion)
    /// Drawing lines around the patches (each tile is divided in a number, typically 16x16, patches) is also supported
    /// </summary>
    public class DrawTerrain
    {
        #region Properties
        /// <summary>The information to be used in the statusbar</summary>
        public string StatusInformation { get; private set; }

        //injection dependency properties
        private MessageDelegate messageDelegate;

        //Directory paths of various formats and files
        string tilesPath;
        string lotilesPath;
        string terrtexPath;

        //basic graphics
        private GraphicsDevice device;
        private BasicEffect basicEffect;

        //Managin textures
        private TerrainTextureManager textureManager;

        // Storing our own generated data for viewing
        private Dictionary<uint, TerrainTile2D> terrainTiles;
        private HashSet<uint> loadedTerrainTiles;
        private Dictionary<string, VertexBuffer> vertexBuffers;
        private Dictionary<string, int> vertexBufferCounts;

        //visibility of terrain and patchlines
        private bool terrainIsVisible;
        private bool terrainDMIsVisible;
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
            loadedTerrainTiles = new HashSet<uint>();
        }

        /// <summary>
        /// Load some basic content (regular XNA method), mainly just initialization.
        /// This does not load tiles nor textures (because those are only loaded on demand, depending on visual area). 
        /// </summary>
        /// <param name="graphicsDevice">The graphsics device used for XNA drawing, and also texture importing</param>
        public void LoadContent(GraphicsDevice graphicsDevice)
        {
            this.device = graphicsDevice;
            basicEffect = new BasicEffect(this.device)
            {
                TextureEnabled = true,
                World = Matrix.Identity
            };
            Clear();
        }

        /// <summary>
        /// Clear all stored data for this drawTerrain, getting rid of all textures and other loaded things.
        /// </summary>
        public void Clear()
        {
            textureManager?.Dispose();
            loadedTerrainTiles.Clear();
            terrainTiles.Clear();

            textureManager = new TerrainTextureManager(terrtexPath, device, messageDelegate);
            SetTerrainReduction();
            DiscardVertexBuffers();
        }

        /// <summary>
        /// Set whether the terrain is visible (drawn) or not.
        /// </summary>
        /// <param name="isVisible">Terrain will be drawn if this is set to yes</param>
        /// <param name="isVisibleDM">Distant mountain terrain will be drawn if this is set to yes</param>
        /// <param name="drawArea">The area currently visible and for which all tiles and textures need to be loaded</param>
        public void SetTerrainVisibility(bool isVisible, bool isVisibleDM, DrawArea drawArea)
        {
            DiscardVertexBuffers();
            bool eitherOneTurnedOn = ((isVisible && !this.terrainIsVisible) || (isVisibleDM && !this.terrainDMIsVisible));
            this.terrainIsVisible = isVisible;
            this.terrainDMIsVisible = isVisibleDM;
            if (isVisible || isVisibleDM)
            {
                if (drawArea == null)
                {
                    return;
                }
                DetermineVisibleArea(drawArea);
                if (eitherOneTurnedOn)
                {   // do not load something if user only wants to draw less
                    EnsureAllTilesAreLoaded();
                }
                CreateVertexBuffers();
            }
        }

        /// <summary>
        /// Set whether the lines between the patches are visible or not
        /// </summary>
        public void SetPatchLineVisibility(bool isVisible)
        {
            this.showPatchLines = isVisible;
        }

        /// <summary>
        /// Set the reduction for all the textures that are loaded. The input is a user setting, not an argument
        /// </summary>
        public void SetTerrainReduction()
        {
            int wantedScaleFactor = Properties.Settings.Default.terrainReductionFactor;
            int newScaleFactor = wantedScaleFactor;
            if (wantedScaleFactor == 0)
            {
                newScaleFactor = 1;
                //Some way to determine the scaling automatically. We use the loaded .ace files
                //The scaling below keeps the memory more or less constant
                if (textureManager.Count() > 100) { newScaleFactor = 2; }
                if (textureManager.Count() > 400) { newScaleFactor = 4; }
                if (textureManager.Count() > 1600) { newScaleFactor = 8; }
                if (textureManager.Count() > 6400) { newScaleFactor = 16; }
            }
            if (newScaleFactor < textureManager.CurrentScaleFactor)
            {
                //The wanted scale factor is less than what it was. We do not have the original data anymore, though.
                //So we need to reload everything
                this.Clear();
                EnsureAllTilesAreLoaded();
                CreateVertexBuffers();
            }
            textureManager.SetCurrentScaleFactor(newScaleFactor);
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
        /// Make sure the Tile and its needed textures are loaded. This means checking if it is already loaded, (or at least tried to load)
        /// and if it is not loaded, load it.
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        void EnsureTileIsLoaded(int tileX, int tileZ)
        {
            uint index = this.locationTranslator.TileIndex(tileX, tileZ, 1);
            if (loadedTerrainTiles.Contains(index))
            {
                return;
            }
            loadedTerrainTiles.Add(index); // whatever comes next, there is not need to reload this tile ever again.

            Tile newTile = LoadTile(tileX, tileZ, false);
            if (newTile == null)
            {
                newTile = LoadTile(tileX, tileZ, true);
                if (newTile == null)
                {
                    return;
                }
            }

            uint storeIndex = this.locationTranslator.TileIndex(tileX, tileZ, newTile.Size);
            if (terrainTiles.ContainsKey(storeIndex))
            {
                // this larger than 1x1 tile has already been loaded from a different tileX and tileZ
                return;
            }

            var newTerrainTile = new TerrainTile2D(newTile, textureManager, locationTranslator);
            terrainTiles.Add(storeIndex, newTerrainTile);
            SetTerrainReduction();
        }

        /// <summary>
        /// Load the information for a single tile (as in parse the needed .t -file.
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        /// <param name="loTiles">Loading LO tile (Distant Mountain) or not</param>
        /// <returns>The tile information as a 'Tile' object</returns>
        private Tile LoadTile(int tileX, int tileZ, bool loTiles)
        {
            TileName.Zoom zoom = loTiles ? TileName.Zoom.DMSmall : TileName.Zoom.Small;
            string path = loTiles ? this.lotilesPath : this.tilesPath;

            // Note, code is similar to ORTS.Viewer3D.TileManager.Load
            // Check for 1x1 or 8x8 tiles.
            TileName.Snap(ref tileX, ref tileZ, zoom);

            // we set visible to false to make sure errors are loaded
            Tile newTile = new Tile(path, tileX, tileZ, zoom, false);
            if (newTile.Loaded)
            {
                return newTile;
            }
            else
            {
                // Check for 2x2 or 16x16 tiles.
                TileName.Snap(ref tileX, ref tileZ, zoom - 1);
                newTile = new Tile(tilesPath, tileX, tileZ, zoom - 1, false);
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
        void CreateVertexBuffers()
        {
            foreach (string textureName in textureManager.Keys)
            {
                //It seems that this implementation has quite some copying of data of vertices, but I am not sure how to prevent this.
                var vertices = new List<VertexPositionTexture>();

                foreach (int zoomSize in GetZoomSizesToShow(false))
                {
                    foreach (TerrainTile2D terrainTile in terrainTiles.Values)
                    {
                        if (terrainTile.TileSize == zoomSize)
                        {
                            var additionalVertices = terrainTile.GetVertices(textureName);
                            vertices.AddRange(additionalVertices);
                        }
                    }
                }

                if (vertices.Count() > 0)
                {
                    VertexBuffer buffer = new VertexBuffer(device, typeof(VertexPositionTexture), vertices.Count(), BufferUsage.WriteOnly);
                    int vertexCount = vertices.Count();
                    buffer.SetData(vertices.ToArray(), 0, vertexCount);
                    vertexBuffers.Add(textureName, buffer);
                    vertexBufferCounts.Add(textureName, vertexCount);
                }
            }

        }

        private List<int> GetZoomSizesToShow(bool lowToHigh)
        {
            List<int> zoomSizesToShow = new List<int>();
            if (this.terrainDMIsVisible)
            {
                zoomSizesToShow.Add(16);
                zoomSizesToShow.Add(8);
            }
            if (this.terrainIsVisible)
            {
                zoomSizesToShow.Add(2);
                zoomSizesToShow.Add(1);
            }
            if (lowToHigh)
            {
                zoomSizesToShow.Reverse();
            }
            return zoomSizesToShow;
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

            device.SamplerStates[0] = SamplerState.LinearWrap;

            foreach (string textureName in vertexBuffers.Keys)
            {
                basicEffect.Texture = textureManager[textureName].Texture;
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    device.SetVertexBuffer(vertexBuffers[textureName]);
                    pass.Apply();
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBufferCounts[textureName]);
                }
            }

            UpdateStatusInformation(drawArea.MouseLocation);
        }

        /// <summary>
        /// This is the method that draws the patchlines (if this is enabled, and if the zooming level is enough).
        /// </summary>
        /// <param name="drawArea">The area to draw upon (with physical location to pixel transformations)</param>
        public void DrawPatchLines(DrawArea drawArea)
        {
            DetermineVisibleArea(drawArea);
            // We want at least 20 pixels/patch to start drawing, each patch = 2048m/16 = 256m, so at least 20pixels/256m or 0.08 pixel/meter
            if (!this.showPatchLines || drawArea.Scale < 0.08)
            {
                return;
            }

            foreach (int zoomSize in GetZoomSizesToShow(false))
            {
                for (int tileX = visibleTileXmin; tileX <= visibleTileXmax; tileX++)
                {
                    for (int tileZ = visibleTileZmin; tileZ <= visibleTileZmax; tileZ++)
                    {
                        uint index = this.locationTranslator.TileIndex(tileX, tileZ, zoomSize);
                        if (terrainTiles.ContainsKey(index))
                        {
                            terrainTiles[index].DrawPatchLines(drawArea);
                        }
                    }
                }
            }

        }

        private void UpdateStatusInformation(WorldLocation location)
        {
            this.StatusInformation = "unknown";
            foreach (int zoomSize in GetZoomSizesToShow(true))
            {
                uint storeIndex = this.locationTranslator.TileIndex(location.TileX, location.TileZ, zoomSize);
                if (terrainTiles.ContainsKey(storeIndex))
                {
                    this.StatusInformation = terrainTiles[storeIndex].GetStatusInformation(location);
                    return;
                }
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

        static Dictionary<int, TileName.Zoom> zoomFromInt = new Dictionary<int, TileName.Zoom> {
            {1, TileName.Zoom.Small}, {2, TileName.Zoom.Large}, {8, TileName.Zoom.DMSmall}, {16, TileName.Zoom.DMLarge}
        };

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

        /// <summary>
        /// Translate a tile location given by tileX and tileZ, together with a zoomSize, into a single uint index
        /// </summary>
        /// <param name="tileX">The cornerIndexX-value of the tile number</param>
        /// <param name="tileZ">The cornerIndexZ-value of the tile number</param>
        /// <param name="zoomSize">The zoom size (1, 2, 8, or 16) </param>
        public uint TileIndex(int tileX, int tileZ, int zoomSize)
        {
            // tileX,Z raw data can be up to 15 bit, possibly 16. But we do not need that for a single route.
            // We also need room for zoom size, which can be 1, 2, 8, 16. To make things easy, we use 6 bits for this.
            // This leaves (32-6)/2 = 13 bits to encode the tileX,Z, each
            // To make sure they are never negative, we add 2^12 = 4096, so they are never negative.
            // Basically, this means routes that can have relative tiles ranging from about -4000 to + 4000, or 8000 * 2km = 16000 km. More than enough.

            // We also snap the tileX and tileZ to multiples of the size.
            TileName.Snap(ref tileX, ref tileZ, zoomFromInt[zoomSize]);

            uint index = ((uint)zoomSize << 26) + ((uint)(tileX - referenceTileX + 4096) << 13) + (uint)(tileZ - this.referenceTileZ + 4096);

            //debug (I guess a unit test would have been better)
            //uint indexX = (uint)(tileX - referenceTileX + 4096);
            //uint indexZ = (uint)(tileZ - referenceTileZ + 4096);
            //string debugX = Convert.ToString(indexX, 2);
            //string debugZ = Convert.ToString(indexZ, 2);
            //string debugString = Convert.ToString(index, 2);
            //if (tileX > referenceTileX)

            return index;
        }
    }
    #endregion

    #region class TerrainTextureManager
    /// <summary>
    /// The manager that loads and stores the various terrain textures. Since normally the textures are shared over multiple tiles,
    /// we want to store them only once.
    /// </summary>
    class TerrainTextureManager : Dictionary<string, ReducableTexture2D>, IDisposable
    {
        public int CurrentScaleFactor { get; private set; } = 1;

        private int loadedAceFilesCounter = 0;
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
        public TerrainTextureManager(string terrtexPath, GraphicsDevice device, MessageDelegate messageDelegate) : base()
        {
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
                //The message delegate has quite some overhead, so print it only so often to keep the user informed
                if (loadedAceFilesCounter % 100 == 0)
                {
                    messageDelegate(String.Format(TrackViewer.catalog.GetString("Loading terrain ace-files {0}-{1} (scaled down with a factor {2})"), loadedAceFilesCounter, loadedAceFilesCounter + 99, CurrentScaleFactor));
                }
                loadedAceFilesCounter++;
                var originalTexture = Orts.Formats.Msts.AceFile.Texture2DFromFile(this.device, path);
                var reducableTexture = new ReducableTexture2D(device, originalTexture);
                reducableTexture.ReduceToFactor(CurrentScaleFactor);
                this[filename] = reducableTexture;
                return true;
            }

            unloadableTerrainTextures.Add(filename);
            return false;
        }

        /// <summary>
        /// Set a new scale factor. Since already loaded textures will be rescaled as needed, this can actually take some time
        /// Hence this is more then just the change of a property
        /// </summary>
        /// <param name="newScaleFactor">The new scale factor to be used</param>
        public void SetCurrentScaleFactor(int newScaleFactor)
        {
            int oldScaleFactor = CurrentScaleFactor;
            CurrentScaleFactor = newScaleFactor;
            if (CurrentScaleFactor <= oldScaleFactor) { return; }

            //We need to rescale all already loaded ace files
            messageDelegate(String.Format(TrackViewer.catalog.GetString("Rescaling previously loaded ace-files")));
            foreach (string filename in this.Keys)
            {
                this[filename].ReduceToFactor(newScaleFactor);
            }
        }

        #region IDisposable
        private bool disposed;

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TerrainTextureManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Implementing IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) { return; }
            disposed = true;
            if (!disposing) { return; }
            unloadableTerrainTextures = null;
            foreach (string filename in this.Keys)
            {
                this[filename].Dispose();
            }
            this.Clear();
        }
        #endregion
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
        /// <summary>The size of the tile (1x1, 2x2, 8x8, 16x16</summary>
        public int TileSize { get; private set; }
        /// <summary>Storing the list of pre-calculated vertices when the textures are drawn fully</summary>
        private Dictionary<string, VertexPositionTexture[]> verticesFull;

        private string[,] textureNames;
        private int snappedTileX;
        private int snappedTileZ;

        //Injection dependencies
        private TerrainTextureManager textureManager;
        private Translate3Dto2D locationTranslator;

        //used during construction:
        private Dictionary<string, List<VertexPositionTexture>> newVertices;

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
            this.TileSize = tile.Size;

            this.snappedTileX = tile.TileX;
            this.snappedTileZ = tile.TileZ;
            TileName.Snap(ref snappedTileX, ref snappedTileZ, zoomFromInt[this.TileSize]);

            verticesFull = CreateVerticesFromTile(tile);
        }

        /// <summary>
        /// Return an array of the vertices needed to draw the terrain of this tile.
        /// </summary>
        /// <param name="textureName">The texture name for which the vertices need to be returned</param>
        public VertexPositionTexture[] GetVertices(string textureName)
        {
            if (this.verticesFull.ContainsKey(textureName))
            {
                return this.verticesFull[textureName];
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
        /// <return>A dictionary with texture-name indexed arrays of vertices</return>
        private Dictionary<string, VertexPositionTexture[]> CreateVerticesFromTile(Tile tile)
        {
            newVertices = new Dictionary<string, List<VertexPositionTexture>>();

            this.textureNames = new string[tile.PatchCount, tile.PatchCount];
            for (int x = 0; x < tile.PatchCount; ++x)
            {
                for (int z = 0; z < tile.PatchCount; ++z)
                {
                    var patch = tile.GetPatch(x, z);
                    this.textureNames[x, z] = CreateVerticesFromPatch(tile, patch);
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
        /// <returns>The texture name</returns>
        private string CreateVerticesFromPatch(Tile tile, terrain_patchset_patch patch)
        {
            var ts = tile.Shaders[patch.ShaderIndex].terrain_texslots;
            string textureName = ts[0].Filename;

            if (!textureManager.TextureIsLoaded(textureName))
            {   // apparently the texture was not found earlier on, so no use bothering about vertices
                return textureName;
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

            return textureName;
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
            int squaresPerPatch = 16;
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

        public string GetStatusInformation(WorldLocation location)
        {
            // first make sure we normalize to the snapped tile
            WorldLocation snappedLocation = new WorldLocation(location);
            snappedLocation.NormalizeTo(this.snappedTileX, this.snappedTileZ);

            float totalSize = 2048 * this.TileSize;
            int patchIndexX = (int)((snappedLocation.Location.X + 1024) / totalSize * this.textureNames.GetLength(0));
            int patchIndexZ = (int)((snappedLocation.Location.Z + 1024) / totalSize * this.textureNames.GetLength(1));
            patchIndexZ = this.textureNames.GetLength(1) - patchIndexZ - 1;

            return String.Format(System.Globalization.CultureInfo.CurrentCulture,
                "({1}, {2}) for {3}x{3} tile: {0}", textureNames[patchIndexX, patchIndexZ], patchIndexX, patchIndexZ, this.TileSize);
        }

        static Dictionary<int, TileName.Zoom> zoomFromInt = new Dictionary<int, TileName.Zoom> {
            {1, TileName.Zoom.Small}, {2, TileName.Zoom.Large}, {8, TileName.Zoom.DMSmall}, {16, TileName.Zoom.DMLarge}
        };

        public void DrawPatchLines(DrawArea drawArea)
        {
            float totalSize = 2048 * this.TileSize;
            int iMax = this.textureNames.GetLength(0);
            float tileL = totalSize / iMax;
            for (int i = 0; i <= iMax; i++)
            {
                drawArea.DrawLine(1, Color.NavajoWhite, new WorldLocation(snappedTileX, snappedTileZ, i * tileL - 1024, 0, -1024), totalSize, 0, 0);
                drawArea.DrawLine(1, Color.NavajoWhite, new WorldLocation(snappedTileX, snappedTileZ, -1024, 0, i * tileL - 1024), totalSize, MathHelper.PiOver2, 0);
            }
        }
    }
    #endregion

    #region ReducableTexture2D
    /// <summary>
    /// Wrapper class around a Texture2D that allows reduction of memory consumption, by reducing the size of the texture
    /// When asked for a reduced version of a texture will be used, in place of the original texture.
    /// Multiple reductions are possible
    /// </summary>
    class ReducableTexture2D : IDisposable
    {
        /// <summary>The actual texture that this is a wrapper for</summary>
        public Texture2D Texture { get; private set; }
        /// <summary>The scaling factor that this texture already has had</summary>
        public int ScaledBy { get; private set; }

        private static GraphicsDevice device;
        private static SpriteBatch spriteBatch;

        //we keep a cache of renderTargets. So we do not have to create them over and over again
        private static Dictionary<int, Dictionary<int, RenderTarget2D>> renderTargets = new Dictionary<int, Dictionary<int, RenderTarget2D>>();
        //We keep a cache of available arrays for color data.
        //This saves quite a bit of creating and destroying temporary arrays.
        private static Dictionary<int, Color[]> colorData = new Dictionary<int, Color[]>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="graphicsDevice">The graphics device that can be used for texture reduction</param>
        /// <param name="originalTexture">The orignal texture that we wrap around (the ScaledBy=1 texture)</param>
        public ReducableTexture2D(GraphicsDevice graphicsDevice, Texture2D originalTexture)
        {
            if (device == null)
            {
                device = graphicsDevice;
                spriteBatch = new SpriteBatch(device);
            }

            Texture = originalTexture;
            ScaledBy = 1;
        }

        /// <summary>
        /// Reduce the size of the texture
        /// </summary>
        /// <param name="requestedScaleFactor">linear reduction scale, scaling from the original texture.</param>
        public void ReduceToFactor(int requestedScaleFactor)
        {
            if (!IsPowerOfTwo(requestedScaleFactor))
            {
                throw new InvalidDataException("Only scaling with a power of 2 is supported to make sure multiple reductions keep making sense");
            }

            int additionalScaleNeeded = DetermineAdditionalScaleNeeded(requestedScaleFactor);
            if (additionalScaleNeeded <= 1)
            {
                // We already have the requested scale
                return;
            }

            int newWidth = Texture.Width / additionalScaleNeeded;
            int newHeight = Texture.Height / additionalScaleNeeded;
            try
            {
                var renderTarget = GetRenderTarget(newWidth, newHeight);
                device.SetRenderTarget(renderTarget);
                device.Clear(Color.White);
                spriteBatch.Begin();
                var fullTarget = new Rectangle(0, 0, newWidth, newHeight);
                spriteBatch.Draw(Texture, fullTarget, Color.White);
                spriteBatch.End();
                device.SetRenderTarget(null);

                //The rendered texture is not very stable: it is in memory of the renderTarget which depends on the video buffer
                //Rescaling the screen or so makes it invalid. So we really copy out the data and put it in a new texture.
                var scaledTexture = GetStableTextureFromRenderTarget(renderTarget);
                Texture.Dispose();
                Texture = scaledTexture;
            }
            catch { }

            //Note, even if the scaling did not work, we do report that the texture has been scaled
            //Otherwise users might be trying over and over again on the not-yet scaled textures
            ScaledBy *= additionalScaleNeeded;
        }

        public int DetermineAdditionalScaleNeeded(int requestedScaleFactor)
        {
            if (requestedScaleFactor < ScaledBy) { return 1; }

            int additionalScaleNeeded = requestedScaleFactor / ScaledBy;
            var pp = device.PresentationParameters;
            //We must make sure the backbuffer can handle it.
            while (Texture.Width / additionalScaleNeeded > pp.BackBufferWidth || Texture.Height / additionalScaleNeeded > pp.BackBufferHeight)
            {
                additionalScaleNeeded *= 2;
            }
            return additionalScaleNeeded;
        }

        private static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        private Texture2D GetStableTextureFromRenderTarget(RenderTarget2D renderTarget)
        {
            int width = renderTarget.Width;
            int height = renderTarget.Height;
            var scaledTexture = new Texture2D(device, width, height, mipmap: false, SurfaceFormat.Color);
            Color[] data = GetColorDataArray(width * height);
            renderTarget.GetData<Color>(data);
            scaledTexture.SetData(data);
            return scaledTexture;
        }

        private static Color[] GetColorDataArray(int totalSize)
        {
            if (!colorData.ContainsKey(totalSize))
            {
                colorData[totalSize] = new Color[totalSize];
            }
            return colorData[totalSize];
        }

        private static RenderTarget2D GetRenderTarget(int width, int height)
        {
            if (renderTargets.ContainsKey(width) && renderTargets[width].ContainsKey(height))
            {
                return renderTargets[width][height];
            }
            var newTarget = GetNewRenderTarget(width, height);
            if (!renderTargets.ContainsKey(width))
            {
                renderTargets[width] = new Dictionary<int, RenderTarget2D>();
            }
            renderTargets[width][height] = newTarget;
            return newTarget;
        }

        private static RenderTarget2D GetNewRenderTarget(int width, int height)
        {
            //todo Handle situations where backbuffer is not large enough to support width and height
            var renderTarget = new RenderTarget2D(
                device,
                width,
                height,
                true,
                SurfaceFormat.Color,
                device.PresentationParameters.DepthStencilFormat,
                device.PresentationParameters.MultiSampleCount,
                device.PresentationParameters.RenderTargetUsage
            );
            return renderTarget;

        }

        #region IDisposable
        private bool disposed;

        /// <summary>
        /// Finalizer
        /// </summary>
        ~ReducableTexture2D()
        {
            Dispose(false);
        }

        /// <summary>
        /// Implementing IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) { return; }
            disposed = true;
            if (!disposing) { return; }
            Texture?.Dispose();
            Texture = null;
            // There are no unmanaged resources to release,
        }
        #endregion
    }
    #endregion
}
