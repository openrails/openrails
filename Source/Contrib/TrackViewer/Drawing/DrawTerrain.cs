using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Orts.Formats.Msts;
using ORTS.Viewer3D;

namespace ORTS.TrackViewer.Drawing
{
    class DrawTerrain
    {
        List<Tile> tiles;
        string tilesPath;
        string lotilesPath;
        string terrtexPath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routePath">Path to the route directory</param>
        /// <param name="messageDelegate">The delegate that will deal with the message we want to send to the user</param>
        /// <param name="drawWorldTiles">The object that knows which tiles are available</param>
        public DrawTerrain(string routePath, MessageDelegate messageDelegate, DrawWorldTiles drawWorldTiles)
        {
            messageDelegate(TrackViewer.catalog.GetString("Loading terrain data ..."));
            tilesPath = routePath + @"\TILES\";
            lotilesPath = routePath + @"\LO_TILES\";
            terrtexPath = routePath + @"\TERRTEX\";

            //todo possibly drawWorldTiles does not know all lo tiles.
            tiles = new List<Tile>();
            drawWorldTiles.DoForAllTiles(new TileDelegate(LoadTile));

        }

        private void LoadTile(int tileX, int tileZ)
        {
            // Note, code is similar to ORTS.Viewer3D.TileManager.Load
            // Check for 1x1 tiles.
            TileName.Snap(ref tileX, ref tileZ, TileName.Zoom.Small);

            //// we set visible to false to make sure errors are loaded
            //Tile newTile = new Tile(tilesPath, tileX, tileZ, TileName.Zoom.Small, false);
            //if (newTile.Loaded)
            //{
            //    tiles.Add(newTile);
            //}
            //else
            //{
            //    // Check for 2x2 tiles.
            //    TileName.Snap(ref tileX, ref tileZ, TileName.Zoom.Large);
            //    newTile = new Tile(tilesPath, tileX, tileZ, TileName.Zoom.Large, false);
            //    if (newTile.Loaded)
            //    {
            //        tiles.Add(newTile);
            //    }
            //}

            //todo lotiles
        }

        public void LoadContent(GraphicsDevice graphicsDevice)
        {
            //tiles[0].Shaders[0].terrain_texslots[0].Filename;
            List<string> filenames = new List<string>();
            foreach (Tile tile in tiles)
            {
                var newfilenames = tile.Shaders.Select(s => s.terrain_texslots[0].Filename).Distinct().ToList();
                filenames = filenames.Union(newfilenames).Distinct().ToList();
            }

            BasicShapes.LoadAceFiles(graphicsDevice, terrtexPath, filenames);

        }

        public void Draw(DrawArea drawArea)
        {
        }
    }
}
