using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DynatrackMesh //: RenderPrimitive
    {
        private VertexBuffer VertexBuffer;
        private static VertexDeclaration VertexDeclaration = null;
        private static IndexBuffer IndexBuffer = null;
        private static int VertexStride;  // in bytes
        public int drawIndex;
        public float[,] section;

        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist buffer.
        private static int numVertices = 10;
        private static short indexCount = 21;

        public struct DtrackData
        {
            public int IsCurved;
            public float param1;
            public float param2;
        }
        DtrackData[] dtrackData;

        public DynatrackMesh(DyntrackObj dtrack)
        {
            // Initialize the array of DtrackData objects
            dtrackData = new DtrackData[5];
            for (int i = 0; i < 5; i++)
            {
                dtrackData[i].IsCurved = dtrack.trackSections[i].isCurved;
                dtrackData[i].param1 = dtrack.trackSections[i].param1;
                dtrackData[i].param2 = dtrack.trackSections[i].param2;
            }

            // Define (technically, "hard code") the track cross section vertex coordinates
            section = new float[10, 2] { { 2.5f, 0.2f }, { -2.5f, 0.2f }, // Ballast
                { 0.7175f, 0.2f }, { 0.8675f, 0.2f }, { -0.7175f, 0.2f }, { -0.8675f, 0.2f }, // Rail sides
                { 0.7175f, 0.325f }, { 0.8675f, 0.325f }, { -0.7175f, 0.325f }, { -0.8675f, 0.325f }}; // Rail tops

        }

        public void VertexCount()
        {
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    return;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    numVertices += 10;
                    indexCount += 21;
                }
                else
                {
                    // Assume one skewed straight section per degree of curvature
                    int numSections = (int)MathHelper.ToDegrees(dtrackData[i].param1);
                    numVertices += 10 * numSections;
                    indexCount += (short)(21 * numSections);
                }
            }
        }

        public void ConstructMesh()
        {
            int vertexIndex = -1;
            // Create three primitives: ballast, rail sides and rail tops. Each primitive
            // has a unique texture and/or shader. When LODs are implemented, we will shed
            // these primitives in the same way as MSTS. All primitives are visible to 700 m.
            // Between 700 m and 1200 m, the rail sides are not drawn. Beyond 1200 m
            // only the ballast plane remains.

            // Ballast
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    return;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    vertexList[vertexIndex].Position = new Vector3(section[0, 0], section[0, 1], 0);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(-0.138916f, 1.34543f);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = new Vector3(section[1, 0], section[1, 1], 0);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(0.862105f, 1.34543f);
                    vertexIndex++;
                    Vector3 displacement = new Vector3(0, 0, dtrackData[i].param1);
                    vertexList[vertexIndex].Position = vertexList[vertexIndex - 2].Position * displacement;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(-0.138916f, -0.280582f);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = vertexList[vertexIndex - 2].Position * displacement;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(0.862106f, - 0.280581f);
                }
            }
        }
    }
}
