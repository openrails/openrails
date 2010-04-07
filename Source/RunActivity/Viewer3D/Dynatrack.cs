/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
///     

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
    #region DynatrackDrawer
    public class DynatrackDrawer
    {
        Viewer3D Viewer;
        Material dtrackMaterial;

        // Classes reqiring instantiation
        public DynatrackMesh dtrackMesh;

        #region Class variables
        WorldPosition worldPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// DynatrackDrawer constructor
        /// </summary>
        public DynatrackDrawer(Viewer3D viewer, DyntrackObj dtrack, WorldPosition position)
        {
            Viewer = viewer;
            worldPosition = position;
            dtrackMaterial = Materials.Load(Viewer.RenderProcess, "DynatrackMaterial");

            // Instantiate classes
            dtrackMesh = new DynatrackMesh(Viewer.RenderProcess, dtrack);
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

            float objectRadius = dtrackMesh.objectRadius;
            // This is hopeless! Always a problem no matter what. Wayne, please help!
            /*
            if (Viewer.Camera.InFOV(mstsLocation, objectRadius))
            {
                if (Viewer.Camera.InRange(mstsLocation, 2000))
                    frame.AddPrimitive(dtrackMaterial, dtrackMesh, ref xnaDTileTranslation);
            }
            */
            frame.AddPrimitive(dtrackMaterial, dtrackMesh, ref xnaDTileTranslation);
        }
    }
    #endregion

    #region DynatrackMesh
    public class DynatrackMesh : RenderPrimitive
    {
        VertexBuffer VertexBuffer;
        VertexDeclaration VertexDeclaration;
        IndexBuffer IndexBuffer;
        int VertexStride;  // in bytes
        public int drawIndex; // Used by Draw to determine which primitive to draw.
        float[,] section; // Contains the track section profile coordinates.
        int numSections; // Number of straight sections needed to make up a curved section.
        Matrix displacement; // For translating and rotating the starting profile vertices.
        Vector3 center; // Center coordinates of curve radius
        Vector3 radius; // Radius vector to each primitive
        Vector3 directionVector; // The direction each track segment is pointing
        public float objectRadius; // For FOV calculation

        VertexPositionNormalTexture[] vertexList;
        short[] triangleListIndices; // Trilist buffer.
        int numVertices; // Number of vertices in the track profile
        short indexCount; // Number of triangle indices
        int vertexIndex;
        short iIndex;

        // These four are used to establish the start points for each Draw call
        int railsideStartVertex;
        int railtopStartVertex;
        short railsideStartIndex;
        short railtopStartIndex;

        // This structure holds the basic geometric parameters of a DT section.
        public struct DtrackData
        {
            public int IsCurved;
            public float param1;
            public float param2;
        }
        DtrackData[] dtrackData;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DynatrackMesh(RenderProcess renderProcess, DyntrackObj dtrack)
        {
            // The track cross section (profile) vertex coordinates are hard coded.
            // The coordinates listed here are those of default MSTS "A1t" track.
            // TODO: Read this stuff from a file. Provide the ability to use alternative profiles.
            section = new float[10, 2] { { 2.5f, 0.2f }, { -2.5f, 0.2f }, // Ballast
                { 0.7175f, 0.2f }, { 0.8675f, 0.2f }, { -0.7175f, 0.2f }, { -0.8675f, 0.2f }, // Rail sides, lower
                { 0.7175f, 0.325f }, { 0.8675f, 0.325f }, { -0.7175f, 0.325f }, { -0.8675f, 0.325f }}; // Rail sides, upper and rail tops

            // Initialize the array of DtrackData objects
            // In each DT world file there are five possible track sections
            dtrackData = new DtrackData[5];
            for (int i = 0; i < 5; i++)
            {
                dtrackData[i].IsCurved = (int)dtrack.trackSections[i].isCurved;
                dtrackData[i].param1 = dtrack.trackSections[i].param1;
                dtrackData[i].param2 = dtrack.trackSections[i].param2;
            }

            numVertices = 14;
            indexCount = 0;
            vertexIndex = 0;
            iIndex = 0;
            CountVerticesAndIndices();
            // Knowing the counts, we can specify the size of the vertex and index buffers.
            vertexList = new VertexPositionNormalTexture[numVertices];
            triangleListIndices = new short[indexCount];

            // Build the mesh and then fill the vertex and triangle index buffers.
            BuildMesh();
            objectRadius = (float)Math.Pow(Math.Pow(vertexList[numVertices - 1].Position.X, 2) + Math.Pow(vertexList[numVertices - 1].Position.X, 2), 0.5) * 1.05f;
            VertexDeclaration = null;
            VertexBuffer = null;
            IndexBuffer = null;
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexStride);
            graphicsDevice.Indices = IndexBuffer;

            switch (drawIndex)
            {
                case 1: // Ballast
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        railsideStartVertex,
                        0,
                        railsideStartIndex / 3);
                    break;
                case 2: // Rail sides
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        railsideStartVertex,
                        (railtopStartVertex - railsideStartVertex),
                        railsideStartIndex,
                        (railtopStartIndex - railsideStartIndex) / 3);
                    break;
                case 3: // Rail tops
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        railtopStartVertex,
                        (numVertices - railtopStartVertex),
                        railtopStartIndex,
                        (indexCount - railtopStartIndex) / 3);
                    break;
                default:
                    break;
            }
        }

        #region Vertex and triangle index generators
        /// <summary>
        /// The DT mesh is built with three primitives. Rather than create a single, large
        /// method to build all the vertices and triangle indices, we split the process
        /// into a series of separate calls.
        /// </summary>
        public void BuildMesh()
        {
            // Create three primitives: ballast, rail sides and rail tops. Each primitive
            // has a unique texture and/or shader. Eventually, when LODs are implemented, we will shed
            // these primitives in the same way as MSTS. All primitives are visible to 700 m.
            // Between 700 m and 1200 m, the rail sides are not drawn. Beyond 1200 m
            // only the ballast plane remains.

            // Split this up for clarity, otherwise the code gets too cumbersome to read
            BallastVertices();
            railsideStartVertex = vertexIndex;
            RailSideVertices();
            railtopStartVertex = vertexIndex;
            RailTopVertices();
            BallastIndices();
            railsideStartIndex = iIndex;
            RailSideIndices();
            railtopStartIndex = iIndex;
            RailTopIndices();
        }

        // VERTICES
        //
        public void BallastVertices()
        {
            // Ballast
            // The following are hard coded to match with MSTS ballast texture mapping
            float uv_vStart = -0.280582f; // Initial V-coordinate value.
            float uv_vIncrement = 0.2088545f; // V-coordinate increment per one meter of track
            float uv_uRight = -0.153916f; // Right-side U coordinate.
            float uv_uLeft = 0.862105f; // Left-side U coordinate.
            float uv_vDisplacement = 0;
            float segmentAngle = 0;
            float segmentLength = 0;
            directionVector = new Vector3(0, 0 ,-1); // Unit vector giving the "heading" of the current segment
            // Set the origin vertices
            vertexList[vertexIndex].Position = new Vector3(section[0, 0], section[0, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, uv_vStart);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[1, 0], section[1, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, uv_vStart);
            vertexIndex++;
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    continue;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    segmentLength = dtrackData[i].param1;
                    uv_vDisplacement = uv_vIncrement * segmentLength;
                    directionVector *= segmentLength;
                    displacement = Matrix.CreateTranslation(directionVector);
                    directionVector.Normalize();

                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 2].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 2].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                    vertexIndex++;
                }
                else // Curved track section
                {
                    numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData[i].param1)); // See above for explanation (~1 degree increments).
                    if (numSections == 0) numSections++;
                    segmentAngle = (dtrackData[i].param1)/ numSections; // Angle in radians by which each successive section of the curved track is rotated.
                    segmentLength = Math.Abs(dtrackData[i].param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.
                    uv_vDisplacement = uv_vIncrement * segmentLength;

                    // Find the coordinates of the center of curvature.
                    center = dtrackData[i].param2 * Vector3.Cross(Vector3.Up, directionVector);
                    // Find the midpoint of the previous ballast face.
                    Vector3 midpoint = new Vector3(
                        (vertexList[vertexIndex - 1].Position.X + vertexList[vertexIndex - 2].Position.X)/2,
                        (vertexList[vertexIndex - 1].Position.Y + vertexList[vertexIndex - 2].Position.Y)/2,
                        (vertexList[vertexIndex - 1].Position.Z + vertexList[vertexIndex - 2].Position.Z)/2);
                    // Move the center point to align with the previous ballast face. This is where the curve begins.
                    displacement = Matrix.CreateTranslation(midpoint);
                    center = Vector3.Transform(center, displacement);

                    for (int j = 0; j < numSections; j++)
                    {
                        // Rotate the direction vector along the curve.
                        displacement = Matrix.CreateRotationY(-segmentAngle);
                        directionVector = Vector3.Transform(directionVector, displacement);
                        // Calculate the radius vector for the right edge of the ballast.
                        radius = vertexList[vertexIndex - 2].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                        vertexIndex++;
                        // Calculate the radius vector for the left edge of the ballast.
                        radius = vertexList[vertexIndex - 2].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                        vertexIndex++;
                    }
                }
            }
        } // BallastVertices

        public void RailSideVertices()
        {
            // Rail Sides
            // The following are hard coded to match MSTS rail side texture mapping
            // Note: Rail sides are mapped horizontally vs. vertically for ballast
            // Refer to comments in BallastVertices().
            float uv_uIncrement = 0.1673372f; // U-coordinate increment per one meter of track
            float uv_uStart = -0.139362f; // Initial U-coordinate value.
            float uv_vUpper = 0.003906f; // Upper edge V coordinate.
            float uv_vLower = 0.101563f; // Lower edge V coordinate.
            float uv_uDisplacement = 0;
            float segmentAngle = 0;
            float segmentLength = 0;
            directionVector = new Vector3(0, 0, -1); // Unit vector giving the "heading" of the current segment
            // Set the origin vertices
            // Right, outer side
            vertexList[vertexIndex].Position = new Vector3(section[3, 0], section[3, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vLower);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[7, 0], section[7, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vUpper);
            vertexIndex++;
            // Right, inner side
            vertexList[vertexIndex].Position = new Vector3(section[2, 0], section[2, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vLower);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[6, 0], section[6, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vUpper);
            vertexIndex++;
            // Left, inner side
            vertexList[vertexIndex].Position = new Vector3(section[4, 0], section[4, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vLower);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[8, 0], section[8, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vUpper);
            vertexIndex++;
            // Left, outer side
            vertexList[vertexIndex].Position = new Vector3(section[5, 0], section[5, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vLower);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[9, 0], section[9, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vUpper);
            vertexIndex++;
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    continue;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    segmentLength = dtrackData[i].param1;
                    uv_uDisplacement = uv_uIncrement * segmentLength;
                    directionVector *= segmentLength;
                    displacement = Matrix.CreateTranslation(directionVector);
                    directionVector.Normalize();

                    // Right, outer side
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Right, inner side
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Left, inner side
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Left, outer side
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                }
                else // Curved track section.
                {
                    numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData[i].param1)); // See above for explanation
                    if (numSections == 0) numSections++;
                    segmentAngle = (dtrackData[i].param1) / numSections; // Angle in radians by which each successive section of the curved track is rotated.
                    segmentLength = Math.Abs(dtrackData[i].param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.
                    uv_uDisplacement = uv_uIncrement * segmentLength;

                    center = dtrackData[i].param2 * Vector3.Cross(Vector3.Up, directionVector);
                    Vector3 midpoint = new Vector3(
                        (vertexList[vertexIndex - 2].Position.X + vertexList[vertexIndex - 8].Position.X) / 2,
                        (vertexList[vertexIndex - 2].Position.Y + vertexList[vertexIndex - 8].Position.Y) / 2,
                        (vertexList[vertexIndex - 2].Position.Z + vertexList[vertexIndex - 8].Position.Z) / 2);
                    displacement = Matrix.CreateTranslation(midpoint);
                    center = Vector3.Transform(center, displacement);

                    for (int j = 0; j < numSections; j++)
                    {
                        displacement = Matrix.CreateRotationY(-segmentAngle);
                        directionVector = Vector3.Transform(directionVector, displacement);
                        // Right, outer side
                        radius = vertexList[vertexIndex - 8].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                        vertexIndex++;
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Position.Y += 0.125f;
                        vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                        vertexIndex++;
                        // Right, inner side
                        radius = vertexList[vertexIndex - 8].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                        vertexIndex++;
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Position.Y += 0.125f;
                        vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                        vertexIndex++;
                        // Left, inner side
                        radius = vertexList[vertexIndex - 8].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                        vertexIndex++;
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Position.Y += 0.125f;
                        vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                        vertexIndex++;
                        // Left, outer side
                        radius = vertexList[vertexIndex - 8].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                        vertexIndex++;
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Position.Y += 0.125f;
                        vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                        vertexIndex++;
                    }
                }
            }
        } // RailSideVertices

        public void RailTopVertices()
        {
            // Rail Tops
            // The following are hard coded to match MSTS rail top texture mapping
            // Note: Rail tops are mapped horizontally vs. vertically for ballast
            float uv_uIncrement = 0.0744726f; // U-coordinate increment per one meter of track
            float uv_uStart = 0.232067f; // Initial U-coordinate value.
            float uv_vInner = 0.126953f; // Inner edge V coordinate.
            float uv_vOuter = 0.224609f; // Outer edge V coordinate.
            float uv_uDisplacement = 0;
            float segmentAngle = 0;
            float segmentLength = 0;
            directionVector = new Vector3(0, 0, -1); // Unit vector giving the "heading" of the current segment
            // Set the origin vertices
            // Right top
            vertexList[vertexIndex].Position = new Vector3(section[7, 0], section[7, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vOuter);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[6, 0], section[6, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vInner);
            vertexIndex++;
            // Left top
            vertexList[vertexIndex].Position = new Vector3(section[8, 0], section[8, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vOuter);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[9, 0], section[9, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uStart, uv_vInner);
            vertexIndex++;
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    continue;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    segmentLength = dtrackData[i].param1;
                    uv_uDisplacement = uv_uIncrement * segmentLength;
                    directionVector *= segmentLength;
                    displacement = Matrix.CreateTranslation(directionVector);
                    directionVector.Normalize();

                    // Right top
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                    vertexIndex++;
                    // Left top
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                    vertexIndex++;
                    vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                    vertexIndex++;
                }
                else // Curved track section
                {
                    numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData[i].param1)); // See above for explanation
                    if (numSections == 0) numSections++;
                    segmentAngle = dtrackData[i].param1 / numSections; // Angle in radians by which each successive section of the curved track is rotated.
                    segmentLength = Math.Abs(dtrackData[i].param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.
                    uv_uDisplacement = uv_uIncrement * segmentLength;

                    center = dtrackData[i].param2 * Vector3.Cross(Vector3.Up, directionVector);
                    Vector3 midpoint = new Vector3(
                        (vertexList[vertexIndex - 1].Position.X + vertexList[vertexIndex - 4].Position.X) / 2,
                        (vertexList[vertexIndex - 1].Position.Y + vertexList[vertexIndex - 4].Position.Y) / 2,
                        (vertexList[vertexIndex - 1].Position.Z + vertexList[vertexIndex - 4].Position.Z) / 2);
                    displacement = Matrix.CreateTranslation(midpoint);
                    center = Vector3.Transform(center, displacement);

                    for (int j = 0; j < numSections; j++)
                    {
                        displacement = Matrix.CreateRotationY(-segmentAngle);
                        directionVector = Vector3.Transform(directionVector, displacement);
                        // Right top
                        radius = vertexList[vertexIndex - 4].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                        vertexIndex++;
                        radius = vertexList[vertexIndex - 4].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                        vertexIndex++;
                        // Right top
                        radius = vertexList[vertexIndex - 4].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                        vertexIndex++;
                        radius = vertexList[vertexIndex - 4].Position - center;
                        radius = Vector3.Transform(radius, displacement);
                        vertexList[vertexIndex].Position = center + radius;
                        vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                        vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                        vertexIndex++;
                    }
                }
            }
        } // RailTopVertices

        // TRIANGLE INDICES                 |/ |
        //                                5 o--o 6
        //                                  |\ |
        // All track primitives are         | \|
        // built up with clockwise        3 o--o 2
        // winding, like this:              | /|
        //                                  |/ |
        //                                1 o--o 0
        public void BallastIndices()
        {
            for (int i = 0; i < railsideStartVertex - 2; i += 2)
            {
                if (i % 4 == 0)
                {
                    triangleListIndices[iIndex++] = (short)i;
                    triangleListIndices[iIndex++] = (short)(i + 1);
                    triangleListIndices[iIndex++] = (short)(i + 2);
                    triangleListIndices[iIndex++] = (short)(i + 1);
                    triangleListIndices[iIndex++] = (short)(i + 3);
                    triangleListIndices[iIndex++] = (short)(i + 2);
                }
                else
                {
                    triangleListIndices[iIndex++] = (short)i;
                    triangleListIndices[iIndex++] = (short)(i + 3);
                    triangleListIndices[iIndex++] = (short)(i + 2);
                    triangleListIndices[iIndex++] = (short)(i + 1);
                    triangleListIndices[iIndex++] = (short)(i + 3);
                    triangleListIndices[iIndex++] = (short)i;
                }
            }
        }// BallastIndices

        public void RailSideIndices()
        {
            for (int i = 0; i < (railtopStartVertex - railsideStartVertex - 8); i += 8)
            {
                for (int j = 0; j < 8; j += 4)
                {
                    if (i % 16 == 0)
                    {
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 8);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 9);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 8);

                        // "Left-facing" triangles must be reverse wound
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 2);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 10);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 3);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 3);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 10);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 11);
                    }
                    else
                    {
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 9);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 8);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 9);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j);

                        // "Left-facing" triangles must be reverse wound
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 2);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 10);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 11);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 3);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 2);
                        triangleListIndices[iIndex++] = (short)(railsideStartVertex + i + j + 11);
                    }
                }
            }
        }// RailSideIndices

        public void RailTopIndices()
        {
            for (int i = 0; i < numVertices - railtopStartVertex - 4; i += 4)
            {
                for (int j = 0; j < 4; j += 2)
                {
                    if (i % 8 == 0)
                    {
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 4);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 5);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 4);
                    }
                    else
                    {
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 5);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 4);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 1);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j + 5);
                        triangleListIndices[iIndex++] = (short)(railtopStartVertex + i + j);
                    }
                }
            }
        }// RailTopIndices
        #endregion

        #region Helpers
        /// <summary>
        /// Get the total vertex and index count for the current DT instance.
        /// </summary>
        public void CountVerticesAndIndices()
        {
            for (int i = 0; i < 5; i++)
            {
                // Check for unused sections
                if (dtrackData[i].param1 == 0 && dtrackData[i].param2 == 0)
                    continue;

                if (dtrackData[i].IsCurved == 0) // Straight track section
                {
                    numVertices += 14;
                    indexCount += 21 * 2;
                }
                else
                {
                    // Assume one skewed straight section per degree of curvature
                    numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData[i].param1));
                    if (numSections == 0) numSections++; // Very small radius track - zero avoidance
                    numVertices += 14 * numSections;
                    indexCount += (short)(21 * numSections * 2);
                }
            }
        }

        /// <summary>
        /// Initializes the vertex and triangle index list buffers.
        /// </summary>
        private void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            if (VertexDeclaration == null)
            {
                VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                VertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            // Initialize the vertex and index buffers, allocating memory for each vertex and index
            VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexList.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexList);
            if (IndexBuffer == null)
            {
                IndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                IndexBuffer.SetData<short>(triangleListIndices);
            }
        }
        #endregion
    }
    #endregion
}
