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
            texturePath += @"\";
            texturePath += forest.TreeTexture;
            forestMaterial = Materials.Load(Viewer.RenderProcess, "ForestMaterial", texturePath, 0, 0);

            // Instantiate classes
            forestMesh = new ForestMesh(Viewer.RenderProcess, this, forest);
        }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Locate relative to the camera
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = worldPosition.XNAMatrix * xnaDTileTranslation;
            xnaDTileTranslation.M42 = forestMesh.refElevation;
            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);
            
            float objectRadius = forestMesh.objectRadius;
            float viewingDistance = 2000; // Arbitrary, but historically in MSTS it was only 1000.
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
            {
                if (Viewer.Camera.InRange(mstsLocation, viewingDistance + objectRadius))
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

        // Forest variables
        Random random;
        ForestDrawer Drawer;
        string tileFolderNameSlash;
        public float objectRadius;
        public float refElevation;

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
            population = (int)(0.75f * (float)forest.Population) + 1;
            treeSize1 = forest.treeSize.treeSize1;
            treeSize2 = forest.treeSize.treeSize2;

            objectRadius = Math.Max(areaDim1, areaDim2) / 2;

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
            Vector3[] tempPosition = new Vector3[population]; // Used only for getting the terrain Y value
            Vector3[] treeSize = new Vector3[population];
            // Find out where in the world we are.
            Matrix XNAWorldLocation = Drawer.worldPosition.XNAMatrix;
            Drawer.worldPosition.XNAMatrix = Matrix.Identity;
            Drawer.worldPosition.XNAMatrix.Translation = XNAWorldLocation.Translation;
            int YtileX, YtileZ;
            Tile tile = new Tile(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, tileFolderNameSlash);
            // Get the Y elevation of the base object itself. Tree elevations are referenced to this.
            YtileX = (int)MathHelper.Clamp((((int)XNAWorldLocation.M41 * 0.125f) + 127.5f), 0, 255);
            YtileZ = (int)MathHelper.Clamp((((int)XNAWorldLocation.M43 * 0.125f) + 127.5f), 0, 255);
            refElevation = tile.GetElevation(YtileX, YtileZ);
            //refElevation = GetTerrainHeight(XNAWorldLocation.M41, XNAWorldLocation.M43);
            float scale;
            for (int i = 0; i < population; i++)
            {
                // Set the XZ position of each tree at random.
                treePosition[i].X = random.Next(-(int)areaDim1 / 4, (int)areaDim1 / 4);
                treePosition[i].Y = 0;
                treePosition[i].Z = random.Next(-(int)areaDim2 / 4, (int)areaDim2 / 4);
                // Orient each treePosition to its final position on the tile so we can get its Y value.
                // Do this by transforming a a copy of the object to its final orientation on the terrain.
                tempPosition[i] = Vector3.Transform(treePosition[i], XNAWorldLocation);
                treePosition[i] = tempPosition[i] - XNAWorldLocation.Translation;
                // Get the terrain height at each position and set Y.
                // First convert to Y file metrics
                YtileX = (int)MathHelper.Clamp((((int)tempPosition[i].X * 0.125f) + 127.5f), 0, 255);
                YtileZ = (int)MathHelper.Clamp((((int)tempPosition[i].Z * 0.125f) + 127.5f), 0, 255);
                treePosition[i].Y = tile.GetElevation(YtileX, YtileZ) - refElevation;
                //treePosition[i].Y = GetTerrainHeight(tempPosition[i].X, tempPosition[i].Z) -refElevation;
                // WVP transformation of the complete object takes place in the vertex shader.

                // Randomize the tree size
                scale = 0.8f * (float)random.Next((int)(scaleRange1 * 1000), (int)(scaleRange2 * 1000)) / 1000;
                treeSize[i].X = treeSize1 * scale;
                treeSize[i].Y = treeSize2 * scale;
                treeSize[i].Z = 1.0f;
            }

            // Create the tree vertex array.
            // Using the Normal property to hold the size info.
            for (int i = 0; i < population * 6; i++)
            {
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 1));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 0));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 0));

                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(1, 1));
                trees[i++] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 1));
                trees[i] = new VertexPositionNormalTexture(treePosition[i / 6],
                    treeSize[i / 6], new Vector2(0, 0));
            }
        }

        public float GetTerrainHeight(float X, float Z)
        {
            int YtileX, YtileZ;
            Tile tile = new Tile(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, tileFolderNameSlash);

            X = X * 0.125f + 127.5f; // 256 / 2048 = 0.125
            Z = Z * 0.125f + 127.5f;

            int xLower = (int)X;
            int xHigher = xLower + 1;
            float xRelative = X - xLower;

            int zLower = (int)Z;
            int zHigher = zLower + 1;
            float zRelative = Z - zLower;

            YtileX = xLower;
            YtileZ = zLower;
            float heightLxLz = tile.GetElevation(YtileX, YtileZ);

            YtileX = xLower;
            YtileZ = zHigher;
            float heightLxHz = tile.GetElevation(YtileX, YtileZ);

            YtileX = xHigher;
            YtileZ = zLower;
            float heightHxLz = tile.GetElevation(YtileX, YtileZ);

            YtileX = xHigher;
            YtileZ = zHigher;
            float heightHxHz = tile.GetElevation(YtileX, YtileZ);

            bool isAboveLowerTriangle = (xRelative + zRelative < 1);

            float finalHeight;
            if (isAboveLowerTriangle)
            {
                finalHeight = heightLxLz;
                finalHeight += zRelative * (heightLxHz - heightLxLz);
                finalHeight += xRelative * (heightHxLz - heightLxLz);
            }
            else
            {
                finalHeight = heightHxHz;
                finalHeight += (1.0f - zRelative) * (heightHxLz - heightHxHz);
                finalHeight += (1.0f - xRelative) * (heightLxHz - heightHxHz);
            }

            return finalHeight;
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
