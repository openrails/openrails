/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// Contributor:
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
        readonly Viewer3D Viewer;
        readonly Material forestMaterial;

        // Classes reqiring instantiation
        public ForestMesh forestMesh;

        #region Class variables
        public readonly WorldPosition worldPosition;
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
			var xnaTranslation = worldPosition.XNAMatrix.Translation;
			Vector3 mstsLocation = new Vector3(xnaTranslation.X + dTileX * 2048, forestMesh.refElevation, -xnaTranslation.Z + dTileZ * 2048);
			Matrix xnaPatchMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            float viewingDistance = 2000; // Arbitrary, but historically in MSTS it was only 1000.
			frame.AddAutoPrimitive(mstsLocation, forestMesh.objectRadius, viewingDistance + forestMesh.objectRadius, forestMaterial, forestMesh, ref xnaPatchMatrix, ShapeFlags.None);
        }
    }
    #endregion

    #region ForestMesh
    public class ForestMesh : RenderPrimitive
    {
        // Vertex declaration
        public VertexDeclaration treeVertexDeclaration;
        public VertexBuffer Buffer;
        public int PrimitiveCount;

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
            if (scaleRange1 > scaleRange2)
            {
                Console.Error.Write("\nForest " + forest.TreeTexture + " in tile " + drawer.worldPosition.TileX + "," + drawer.worldPosition.TileZ + " has scale range with minimum greater than maximum\n");
                float scaleRangeSwap = scaleRange2;
                scaleRange2 = scaleRange1;
                scaleRange1 = scaleRangeSwap;
            }

            areaDim1 = Math.Abs(forest.forestArea.areaDim1);
            areaDim2 = Math.Abs(forest.forestArea.areaDim2);
            population = (int)(0.75f * (float)forest.Population) + 1;
            treeSize1 = forest.treeSize.treeSize1;
            treeSize2 = forest.treeSize.treeSize2;

            objectRadius = Math.Max(areaDim1, areaDim2) / 2;

            // Instantiate classes
            // to get consistent tree placement between sessions, derive the seed from the location
            int seed = (int)(1000.0*(drawer.worldPosition.Location.X + drawer.worldPosition.Location.Z + drawer.worldPosition.Location.Y));
            random = new Random(seed);
            VertexPositionNormalTexture[] trees = new VertexPositionNormalTexture[population * 6];
            treeVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, VertexPositionNormalTexture.VertexElements);

            InitForestVertices(trees);

            PrimitiveCount = trees.Length / 3;
            Buffer = new VertexBuffer(renderProcess.GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * trees.Length, BufferUsage.WriteOnly);
            Buffer.SetData(trees);
        }

        /// <summary>
        /// Forest tree array intialization. 
        /// </summary>
        private void InitForestVertices( VertexPositionNormalTexture[] trees)
        {
            // Create the tree position and size arrays.
            Vector3[] treePosition = new Vector3[population];
            Vector3[] tempPosition = new Vector3[population]; // Used only for getting the terrain Y value
            Vector3[] treeSize = new Vector3[population];
            // Find out where in the world we are.
            Matrix XNAWorldLocation = Drawer.worldPosition.XNAMatrix;
            Drawer.worldPosition.XNAMatrix = Matrix.Identity;
            Drawer.worldPosition.XNAMatrix.Translation = XNAWorldLocation.Translation;
            float YtileX, YtileZ;
            Tile tile = new Tile(Drawer.worldPosition.TileX, Drawer.worldPosition.TileZ, tileFolderNameSlash);
            // Get the Y elevation of the base object itself. Tree elevations are referenced to this.
            YtileX = (XNAWorldLocation.M41 + 1024) / 8;
            YtileZ = (XNAWorldLocation.M43 + 1024) / 8;
            refElevation = tile.GetElevation((int)YtileX, (int)YtileZ);
            float scale;
            for (int i = 0; i < population; i++)
            {
                // Set the XZ position of each tree at random.
                treePosition[i].X = random.Next(-(int)areaDim1 / 2, (int)areaDim1 / 2);
                treePosition[i].Y = 0;
                treePosition[i].Z = random.Next(-(int)areaDim2 / 2, (int)areaDim2 / 2);
                // Orient each treePosition to its final position on the tile so we can get its Y value.
                // Do this by transforming a copy of the object to its final orientation on the terrain.
                tempPosition[i] = Vector3.Transform(treePosition[i], XNAWorldLocation);
                treePosition[i] = tempPosition[i] - XNAWorldLocation.Translation;
                // Get the terrain height at each position and set Y.
                // First convert to Y file metrics
                YtileX = MathHelper.Clamp((tempPosition[i].X + 1024) / 8, 0, 254.9999f);
                YtileZ = MathHelper.Clamp((tempPosition[i].Z + 1024) / 8, 0, 254.9999f);
				// TODO: What is this -0.8 here for?
				treePosition[i].Y = tile.GetElevation(YtileX, YtileZ) - refElevation - 0.8f;
                // WVP transformation of the complete object takes place in the vertex shader.

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

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = treeVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(Buffer, 0, treeVertexDeclaration.GetVertexStrideSize(0));
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, PrimitiveCount);
        }
    }
    #endregion

}
