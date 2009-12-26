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
        Material SkyMaterial;
        SkyMesh SkyMesh;
        Viewer3D Viewer;

        public SkyDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
            SkyMaterial = Materials.Load(Viewer.RenderProcess, "SkyMaterial");
            SkyMesh = new SkyMesh( Viewer.RenderProcess);
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Vector3 ViewerXNAPosition = new Vector3(Viewer.Camera.Location.X, Viewer.Camera.Location.Y - 400, -Viewer.Camera.Location.Z);
            Matrix XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

            frame.AddPrimitive(SkyMaterial, SkyMesh, ref XNASkyWorldLocation);
        }
    }

    public class SkyMesh: RenderPrimitive 
    {
        private VertexBuffer SkyVertexBuffer;
        private static VertexDeclaration SkyVertexDeclaration = null;
        private static IndexBuffer SkyIndexBuffer = null;
        private static int SkyVertexStride;  // in bytes

        // Sky dome geometry is based on two global variables: the radius and the number of sides
        private const int skyRadius = SkyConstants.skyRadius;
        private const int skySides = SkyConstants.skySides;
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private const int skyLevels = ((SkyConstants.skySides / 4) - 1);
        // Number of vertices in the sky hemisphere. (= 325 with 36 sides)
        private static int numVertices = (int)((Math.Pow(SkyConstants.skySides, 2) / 4) + 1);
        // Number of point indices (= 1836 for 36 sides: 8 levels of 36 triangle pairs each + 36 triangles at the zenith)
        private const int indexCount = (SkyConstants.skySides * 2 * 3 * ((SkyConstants.skySides / 4) - 1)) + 3 * SkyConstants.skySides;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SkyMesh(RenderProcess renderProcess)
        {
            InitializeVertexBuffer( renderProcess.GraphicsDevice);
            InitializeIndexBuffer( renderProcess.GraphicsDevice);
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = SkyVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(SkyVertexBuffer, 0, SkyVertexStride);
            graphicsDevice.Indices = SkyIndexBuffer;

            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                numVertices,
                0,
                indexCount / 3);
        }

        /// <summary>
        /// Initializes the hemisphere's vertex list.
        /// </summary>
        private void InitializeVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (SkyVertexDeclaration == null)
            {
                SkyVertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                SkyVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            VertexPositionNormalTexture[] vertexList = new VertexPositionNormalTexture[numVertices];

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
            SkyVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexList.Length, BufferUsage.WriteOnly);
            SkyVertexBuffer.SetData(vertexList);
        }

        /// <summary>
        /// Initializes the hemisphere's triangle index list.
        /// </summary>
        private void InitializeIndexBuffer(GraphicsDevice graphicsDevice)
        {
            if (SkyIndexBuffer == null)
            {
                short[] triangleListIndices; // Trilist array.

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

                SkyIndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                SkyIndexBuffer.SetData<short>(triangleListIndices);
            }
        }

    } // SkyMesh

}
