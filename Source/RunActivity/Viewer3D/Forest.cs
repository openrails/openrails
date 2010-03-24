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
    /// <summary>
    /// Forest render primitive
    /// </summary>
    public class ForestDrawer
    {
        Viewer3D Viewer;
        Material forestMaterial;

        // Classes reqiring instantiation
        public ForestMesh forestMesh;

        #region Class variables
        // Forest parameters
        WorldPosition worldPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// ForestDrawer constructor
        /// </summary>
        public ForestDrawer(Viewer3D viewer, ForestObj forest, WorldPosition position)
        {
            Viewer = viewer;
            worldPosition = position;
            //forestMaterial = Materials.Load(Viewer.RenderProcess, "ForestMaterial");
            // Instantiate classes
            forestMesh = new ForestMesh(Viewer.RenderProcess, forest);

            // Set default values and pass to ForestMesh as applicable

        }
        #endregion

        /// <summary>
        /// Define the location of the forest object
        /// </summary>
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
    public class ForestMesh: RenderPrimitive 
    {
        // Vertex declaration
        private VertexDeclaration treeVertexDeclaration;
        private TreeVertex[] trees;

        Random random;

        // This structure holds the basic geometric parameters of a forest object.
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
        public ForestMesh(RenderProcess renderProcess, ForestObj forest)
        {
            // Instantiate classes
            random = new Random();
            treeVertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, TreeVertex.VertexElements);

            // Initialize local variables from WFile data
            treeTexture = forest.TreeTexture;
            scaleRange1 = forest.scaleRange.scaleRange1;
            scaleRange2 = forest.scaleRange.scaleRange2;
            areaDim1 = forest.forestArea.areaDim1;
            areaDim2 = forest.forestArea.areaDim2;
            population = (int)forest.Population;
            treeSize1 = forest.treeSize.treeSize1;
            treeSize2 = forest.treeSize.treeSize2;

        }

        /// <summary>
        /// Forest tree array intialization. 
        /// </summary>
        private void InitForestVertices(double currentTime)
        {
            // Create the precipitation particles
            trees = new TreeVertex[population];
            
            // Initialize particles
            for (int i = 0; i < trees.Length; i++)
            {
                trees[i] = new TreeVertex(new Vector3(
                        random.Next((int)width * 1000) / 1000f - width / 2f,
                        width / 2,
                        random.Next((int)width * 1000) / 1000f - width / 2f),
                    particleSize,
                    // Particles are uniformly diffused in time
                    (float)currentTime - (drops.Length - i)*timeStep,
                    windStrength * windDir);
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Place the vertex declaration on the graphics device
            graphicsDevice.VertexDeclaration = treeVertexDeclaration;

            graphicsDevice.DrawUserPrimitives(PrimitiveType.PointList, trees, 0, trees.Length);
        }

        #region TreeVertex definition
        /// <summary>
        /// Custom precipitation sprite vertex format.
        /// </summary>
        private struct TreeVertex
        {
            public Vector3 position;
            public float pointSize;
            public float time;
            public Vector2 wind;

            /// <summary>
            /// Precipitaiton vertex constructor.
            /// </summary>
            /// <param name="position">particle position</param>
            /// <param name="pointSize">particle size</param>
            /// <param name="time">time of particle initialization</param>
            /// <param name="wind">wind direction</param>
            //public VertexPointSprite(Vector3 position, float pointSize, float time, Vector3 random, Vector2 wind)
            public TreeVertex(Vector3 position, float pointSize, float time, Vector2 wind)
            {
                this.position = position;
                this.pointSize = pointSize;
                this.time = time;
                this.wind = wind;
            }

            // Vertex elements definition
            public static readonly VertexElement[] VertexElements = 
            {
                new VertexElement(0, 0, 
                    VertexElementFormat.Vector3, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.Position, 0),
                new VertexElement(0, sizeof(float) * 3, 
                    VertexElementFormat.Single, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.PointSize, 0),
                new VertexElement(0, sizeof(float) * (3 + 1), 
                    VertexElementFormat.Single, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, sizeof(float) * (3 + 1 + 1), 
                    VertexElementFormat.Vector2, 
                    VertexElementMethod.Default, 
                    VertexElementUsage.TextureCoordinate, 1),
           };

            // Size of one vertex in bytes
            public static int SizeInBytes = sizeof(float) * (3 + 1 + 1 + 2);
        }
        #endregion
    }
    #endregion
}
