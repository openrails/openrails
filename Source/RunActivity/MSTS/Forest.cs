/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// 
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
using MSTS;

namespace ORTS
{
    #region ForestDrawer
    public class ForestDrawer
    {
        Viewer3D Viewer;
        Material forestMaterial;

        // Classes reqiring instantiation
        public ForestMesh forestMesh;

        #region Class variables
        public WorldPosition worldPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// ForestDrawer constructor
        /// </summary>
        public ForestDrawer(Viewer3D viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;
            worldPosition = position;

            // Check the SD file for alternative texture specification
            int altTex = 252; // Trees and vegetation
            Helpers helper = new Helpers();
            string texturePath = helper.GetTextureFolder(Viewer, altTex);

            //string texturePath = viewer.Simulator.RoutePath;
            //texturePath += @"\TEXTURES\";
            texturePath += @"\";
            texturePath += forest.TreeTexture;
            forestMaterial = Materials.Load(Viewer.RenderProcess, "ForestMaterial", texturePath, 0, 0);

            // Instantiate classes
            forestMesh = new ForestMesh(Viewer.RenderProcess, this, forest);
        }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Matrix XNAWorldLocation = worldPosition.XNAMatrix;
            // Locate relative to the camera
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = worldPosition.XNAMatrix * xnaDTileTranslation;
            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            // TODO: Calculate ViewSphere and LOD distances
            if (Viewer.Camera.InFOV(mstsLocation, 500))
            {
                if (Viewer.Camera.InRange(mstsLocation, 2000 + 500))
                    frame.AddPrimitive(forestMaterial, forestMesh, ref xnaDTileTranslation);
            }
        }
    }
    #endregion

    #region ForestMesh
    public class ForestMesh : RenderPrimitive
    {
        // Vertex declaration
        public VertexDeclaration treeVertexDeclaration;
        private VertexPositionNormalTexture[] trees;

        // Local variables
        Random random;
        ForestDrawer Drawer;
        string tileFolderNameSlash;

        // Basic geometric parameters of a forest object, from the World file.
        string treeTexture;
        float scaleRange1;
        float scaleRange2;
        float areaDim1;
        float areaDim2;
        int population;
        float treeSize1;
        float treeSize2;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ForestMesh(RenderProcess renderProcess, ForestDrawer drawer, ForestObj forest)
        {
            Drawer = drawer;
            string path = renderProcess.Viewer.Simulator.RoutePath;
            tileFolderNameSlash = path + @"\tiles\";

            // Initialize local variables from WFile data
            treeTexture = forest.TreeTexture;
            scaleRange1 = forest.scaleRange.scaleRange1;
            scaleRange2 = forest.scaleRange.scaleRange2;
            areaDim1 = Math.Abs(forest.forestArea.areaDim1);
            areaDim2 = Math.Abs(forest.forestArea.areaDim2);
            population = forest.Population;
            treeSize1 = forest.treeSize.treeSize1;
            treeSize2 = forest.treeSize.treeSize2;

            // Instantiate classes
            random = new Random();
            trees = new VertexPositionNormalTexture[population * 6];
            treeVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPositionNormalTexture.VertexElements);

            InitForestVertices();
        }

        /// <summary>
        /// Forest tree array intialization. 
        /// </summary>
        private void InitForestVertices()
        {
            // Create the tree position and size arrays.
            Vector3[] treePosition = new Vector3[population];
            Vector3[] treeSize = new Vector3[population];
            int YtileX, YtileZ;
            // Find out where in the world we are.
            Matrix XNAWorldLocation = Drawer.worldPosition.XNAMatrix;
            Tile tile = new Tile(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, tileFolderNameSlash);
            float scale;
            for (int i = 0; i < population; i++)
            {
                // Set the XZ position of each tree at random.
                treePosition[i].X = random.Next(-(int)areaDim1 / 2, (int)areaDim1 / 2);
                treePosition[i].Y = 0;
                treePosition[i].Z = random.Next(-(int)areaDim2 / 2, (int)areaDim2 / 2);
                // Orient each treePosition to its final position on the tile.
                treePosition[i] = Vector3.Transform(treePosition[i], XNAWorldLocation);
                // Get the terrain height at each position and set Y.
                // First convert to Y file metrics
                YtileX = (int)MathHelper.Clamp((((int)treePosition[i].X * 255 / 2048) + 127), 0.0f, 255.0f);
                YtileZ = (int)MathHelper.Clamp((((int)treePosition[i].Z * 255 / 2048) + 127), 0.0f, 255.0f);
                treePosition[i].Y = tile.GetElevation(YtileX, YtileZ);

                // Randomize the tree size
                scale = (float)random.Next((int)(scaleRange1 * 1000), (int)(scaleRange2 * 1000)) / 1000;
                treeSize[i].X = treeSize1 * scale;
                treeSize[i].Y = treeSize2 * scale;
                treeSize[i].Z = 1.0f;
            }

            // Create the tree vertex array.
            // Using the Normal property to hold the size info.
            for (int i = 0; i < population * 6; i++)
            {
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 0));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6], 
                    treeSize[i / 6], new Vector2(1, 0));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6], 
                    treeSize[i / 6], new Vector2(1, 1));

                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6], 
                    treeSize[i / 6], new Vector2(0, 0));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6], 
                    treeSize[i / 6], new Vector2(1, 1));
                trees[i] = new VertexPositionNormalTexture(treePosition[i / 6], 
                    treeSize[i / 6], new Vector2(0, 1));
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = treeVertexDeclaration;

            graphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, trees, 0, trees.Length / 3);
        }
    }
    #endregion

}
