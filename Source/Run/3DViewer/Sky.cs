/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Contributors:
///     2009-10-28  Rick Grout
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

namespace ORTS
{
    class SkyConstants   // WAC  consolidated sky related elements into one source file
    {
        // Sky dome constants
        public const int skyRadius = 3000;
        // skySides: Use minimum 36 to avoid wavy distortion at quad edges
        // Optimum number seems to be 72. Many numbers will introduce distortion.
        public const int skySides = 72;
        public const int cloudSphereRadius = 5998; // Future
    }

    public class SkyDrawer
    {
        public SkyDrawer(Viewer viewer)
        {
            SkyMesh mesh = new SkyMesh(viewer);
        }
    }
    public class SkyMesh : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Viewer Viewer;

        private Vector3 XNASkyPosition;    // WAC - designated coordinate space
        private Matrix XNASkyWorldLocation;  // WAC - designated coordinate space
        private VertexBuffer SkyVertexBuffer;
        private static VertexDeclaration SkyVertexDeclaration = null;
        private static IndexBuffer SkyIndexBuffer = null;
        private static int SkyVertexStride;  // in bytes

        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist array.

        // Sky dome geometry is based on two global variables: the radius and the number of sides
        private static int skyRadius = SkyConstants.skyRadius;
        private static int skySides = SkyConstants.skySides;
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private static int skyLevels = ((SkyConstants.skySides / 4) - 1);
        // Number of vertices in the sky hemisphere. (= 325 with 36 sides)
        private static int numVertices = (int)((Math.Pow(SkyConstants.skySides, 2) / 4) + 1);
        // Number of point indices (= 1836 for 36 sides: 8 levels of 36 triangle pairs each + 36 triangles at the zenith)
        private static int indexCount = (SkyConstants.skySides * 2 * 3 * ((SkyConstants.skySides / 4) - 1)) + 3 * SkyConstants.skySides;

        private Texture2D skyTexture;  // WAC moved from Viewer file
 
        /// <summary>
        /// Constructor.
        /// </summary>
        public SkyMesh(Viewer viewer)
            : base(viewer)
        {
            Viewer = viewer;

            XNASkyPosition = new Vector3(0f);
            XNASkyWorldLocation = Matrix.CreateTranslation(XNASkyPosition);

            viewer.Components.Add(this);
        }

        /// <summary>
        /// Load the procedurally generated hemisphere mesh geometry
        /// </summary>
        protected override void LoadContent()
        {
            InitializeVertexList();
            InitializeTriangleList();
            skyTexture = Viewer.Content.Load<Texture2D>("sky");

            base.LoadContent();
        }

        /// <summary>
        /// Initializes the hemisphere's vertex list.
        /// </summary>
        private void InitializeVertexList()
        {
            if (SkyVertexDeclaration == null)
            {
                SkyVertexDeclaration = new VertexDeclaration(this.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
                SkyVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            vertexList = new VertexPositionNormalTexture[numVertices];

            int vertexIndex = 0;
            // for each vertex
            for (int i = 0; i < (skySides / 4); i++) // (=9 for 36 sides)
                for (int j = 0; j < skySides; j++) // (=36 for 36 sides)
                {
                    float y = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * i)) * skyRadius;
                    float yRadius = skyRadius * (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * i));
                    float x = (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;
                    float z = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;

                    // Space the concentric UV circles for each layer equally to avoid texel smearing near the horizon.
                    float uvRadius = 0.5f - (0.5f * i) / (skySides / 4);
                    float uv_x = 0.5f - ((float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);
                    float uv_z = 0.5f - ((float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);

                    vertexList[vertexIndex].Position = new Vector3(x, y, z);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_x, uv_z);
                    vertexList[vertexIndex].Normal = new Vector3(0f);
                    vertexIndex++;
                }
            // Single vertex at zenith
            vertexList[vertexIndex].Position = new Vector3(0, skyRadius, 0);
            vertexList[vertexIndex].Normal = new Vector3(0f);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(0.5f, 0.5f);

            // Initialize the vertex buffer, allocating memory for each vertex,
            // and set the vertex buffer data to the array of vertices.
            SkyVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexList.Length, BufferUsage.WriteOnly);
            SkyVertexBuffer.SetData(vertexList);
        }

        /// <summary>
        /// Initializes the hemisphere's triangle index list.
        /// </summary>
        private void InitializeTriangleList()
        {
            if (SkyIndexBuffer == null)
            {
                triangleListIndices = new short[indexCount];
                int iIndex = 0;
                // ----------------------------------------------------------------------
                // 36-sided sky dome mesh is built like this:        72 73 74
                // Triangles are wound couterclockwise          87 o--o--o--o
                // because we're looking at the inner              | /|\ | /|
                // side of the hemisphere. Each time               |/ | \|/ |
                // we circle around to the start point          71 o--o--o--o 38
                // on the mesh we have to reset the                |\ | /|\ |
                // vertex number back to the beginning.            | \|/ | \|
                // Using Wayne's sw,se,nw,ne coordinate  nw ne  35 o--o--o--o 
                // convention.-->                        sw se        0  1  2
                // ----------------------------------------------------------------------
                for (int i = 0; i < skyLevels; i++) // (=8 for 36 sides)
                    for (int j = 0; j < skySides; j++) // (=36 for 36 sides)
                    {
                        // Vertex indices, beginning in the southwest corner
                        short sw = (short)(j + i * skySides);
                        short nw = (short)(sw + skySides);
                        short ne = (short)(nw + 1);
                        short se = (short)(sw + 1);

                        if (((i & 1) == (j & 1)))  // triangles alternate
                        {
                            triangleListIndices[iIndex++] = sw;
                            triangleListIndices[iIndex++] = (ne % skySides == 0) ? (short)(ne - skySides) : ne;
                            triangleListIndices[iIndex++] = nw;
                            triangleListIndices[iIndex++] = sw;
                            triangleListIndices[iIndex++] = (se % skySides == 0) ? (short)(se - skySides) : se;
                            triangleListIndices[iIndex++] = (ne % skySides == 0) ? (short)(ne - skySides) : ne;
                        }
                        else
                        {
                            triangleListIndices[iIndex++] = sw;
                            triangleListIndices[iIndex++] = (se % skySides == 0) ? (short)(se - skySides) : se;
                            triangleListIndices[iIndex++] = nw;
                            triangleListIndices[iIndex++] = (se % skySides == 0) ? (short)(se - skySides) : se;
                            triangleListIndices[iIndex++] = (ne % skySides == 0) ? (short)(ne - skySides) : ne;
                            triangleListIndices[iIndex++] = nw;
                        }
                    }
                //Zenith triangles (=36 for 36 sides)
                for (int i = 0; i < skySides; i++)
                {
                    short sw = (short)((skySides * skyLevels) + i);
                    short se = (short)((skySides * skyLevels) + i + 1);

                    triangleListIndices[iIndex++] = sw;
                    triangleListIndices[iIndex++] = (se % skySides == 0) ? (short)(se - skySides) : se;
                    triangleListIndices[iIndex++] = (short)(numVertices - 1); // The zenith
                }

                SkyIndexBuffer = new IndexBuffer(GraphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                SkyIndexBuffer.SetData<short>(triangleListIndices);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Lower the sky so bottom rim of hemisphere is not normally visible.
            // WAC - see COMMENTS.TXT - notes on MSTS coordinate space vs XNA coordinate space
            Vector3 ViewerXNAPosition = new Vector3(Viewer.Camera.Location.X, Viewer.Camera.Location.Y - 400, -Viewer.Camera.Location.Z); 
            XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);
            Viewer.SceneryShader.SetMatrix(XNASkyWorldLocation, Viewer.Camera.XNAView, Viewer.Camera.XNAProjection);
            Viewer.SceneryShader.CurrentTechnique = Viewer.SceneryShader.Techniques[3];
            Viewer.SceneryShader.Texture = skyTexture;
            // These parameter changes have no effect
            Viewer.SceneryShader.Brightness = 1.0f;
            Viewer.SceneryShader.Ambient = 1.0f;
            Viewer.SceneryShader.Saturation = 1.0f;
            // Maybe sky needs its own shader.
            Viewer.RenderState = 3;

            GraphicsDevice.RenderState.CullMode = CullMode.None;
            GraphicsDevice.RenderState.FillMode = FillMode.Solid;
            GraphicsDevice.VertexDeclaration = SkyVertexDeclaration;
            GraphicsDevice.Vertices[0].SetSource(SkyVertexBuffer, 0, SkyVertexStride);
            GraphicsDevice.Indices = SkyIndexBuffer;

            GraphicsDevice.RenderState.FogVertexMode = FogMode.None;  // vertex fog
            GraphicsDevice.RenderState.FogTableMode = FogMode.Linear;     // pixel fog off
            GraphicsDevice.RenderState.FogColor = new Color(128, 128, 128, 255);
            GraphicsDevice.RenderState.FogDensity = 1.0f;                      // used for exponential fog only, not linear
            GraphicsDevice.RenderState.FogEnd = SkyConstants.skyRadius+ 100;
            GraphicsDevice.RenderState.FogStart = 1000f;
            GraphicsDevice.RenderState.FogEnable = false;

            Viewer.SceneryShader.Begin();
            foreach (EffectPass pass in Viewer.SceneryShader.CurrentTechnique.Passes)
            {
                pass.Begin();
                GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 
                    0, 
                    0, 
                    numVertices, 
                    0, 
                    indexCount / 3);
                pass.End();
            }
            Viewer.SceneryShader.End();

            base.Draw(gameTime);
        }
    }
}
