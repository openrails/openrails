/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// Contributors:
///    Walt Niehoff
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
        public DynatrackDrawer(Viewer3D viewer, DyntrackObj dtrack, WorldPosition position, WorldPosition endPosition)
        {
            Viewer = viewer;
            worldPosition = position;
            dtrackMaterial = Materials.Load(Viewer.RenderProcess, "DynatrackMaterial");

            // Instantiate classes
            dtrackMesh = new DynatrackMesh(Viewer.RenderProcess, dtrack, worldPosition, endPosition);
       }
        #endregion

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Offset relative to the camera-tile origin
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
            Vector3 tileOffsetWrtCamera = new Vector3(dTileX * 2048, 0, -dTileZ * 2048);

            // Find midpoint between auxpoint and track section root
            Vector3 xnaLODCenter = 0.5f * (dtrackMesh.XNAEnd + worldPosition.XNAMatrix.Translation +
                                            2 * tileOffsetWrtCamera);
            dtrackMesh.MSTSLODCenter = new Vector3(xnaLODCenter.X, xnaLODCenter.Y, -xnaLODCenter.Z);

            if (Viewer.Camera.CanSee(dtrackMesh.MSTSLODCenter, dtrackMesh.objectRadius, 500))
            {
                // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
                Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
                xnaXfmWrtCamTile = worldPosition.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
                // (Transformation is now with respect to camera-tile origin)

                frame.AddPrimitive(dtrackMaterial, dtrackMesh, ref xnaXfmWrtCamTile);
            }
        }
    }
    #endregion

    #region DynatrackMesh
    public class DynatrackMesh : RenderPrimitive
    {
        VertexBuffer VertexBuffer;
        VertexDeclaration VertexDeclaration;
        IndexBuffer IndexBuffer;
        int VertexStride;           // in bytes
        public int drawIndex;       // Used by Draw to determine which primitive to draw.
        float[,] section;           // Contains the track section profile coordinates.
        int numSections;            // Number of straight sections needed to make up a curved section.
        Matrix sectionRotation;     // Rotates previous profile into next profile position on curve.
        //Matrix displacement;      // For translating and rotating the starting profile vertices.
        Vector3 center;             // Center coordinates of curve radius
        Vector3 radius;             // Radius vector to cross section on curve centerline
        Vector3 directionVector;    // The direction each track segment is pointing

        public Vector3 XNAEnd;        // Location of termination-of-section (as opposed to root)
        public float objectRadius;    // Radius of bounding sphere
        public Vector3 MSTSLODCenter; // Center of bounding sphere

        VertexPositionNormalTexture[] vertexList;
        short[] triangleListIndices;    // Trilist buffer.
        int numVertices;                // Number of vertices in the track profile
        short indexCount;               // Number of triangle indices
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
            public float deltaY;
        }
        DtrackData dtrackData; // Was: DtrackData[] dtrackData;

        public uint UiD; // Used for debugging only
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public DynatrackMesh(RenderProcess renderProcess, DyntrackObj dtrack, WorldPosition worldPosition, 
                                WorldPosition endPosition)
        {
            // DynatrackMesh is responsible for creating a mesh for a section with a single subsection.
            // It also must update worldPosition to reflect the end of this subsection, subsequently to
            // serve as the beginning of the next subsection.

            UiD = dtrack.trackSections[0].UiD; // Used for debugging only

            // The track cross section (profile) vertex coordinates are hard coded.
            // The coordinates listed here are those of default MSTS "A1t" track.
            // TODO: Read this stuff from a file. Provide the ability to use alternative profiles.
            section = new float[10, 2] { { 2.5f, 0.2f }, { -2.5f, 0.2f }, // Ballast
                { 0.7175f, 0.2f }, { 0.8675f, 0.2f }, { -0.7175f, 0.2f }, { -0.8675f, 0.2f }, // Rail sides, lower
                { 0.7175f, 0.325f }, { 0.8675f, 0.325f }, { -0.7175f, 0.325f }, { -0.8675f, 0.325f }}; // Rail sides, upper and rail tops

            // Initialize a DtrackData object
            // In this implementation dtrack has only 1 DT subsection.
            if (dtrack.trackSections.Count != 1)
            {
                throw new ApplicationException(
                    "DynatrackMesh Constructor detected a multiple-subsection dynamic track section. " +
                    "(SectionIdx = " + dtrack.SectionIdx + ")");
            }
            dtrackData = new DtrackData();
            dtrackData.IsCurved = (int)dtrack.trackSections[0].isCurved;
            dtrackData.param1 = dtrack.trackSections[0].param1;
            dtrackData.param2 = dtrack.trackSections[0].param2;
            dtrackData.deltaY = dtrack.trackSections[0].deltaY;
            XNAEnd = endPosition.XNAMatrix.Translation;

            numVertices = 14;
            indexCount = 0;
            vertexIndex = 0;
            iIndex = 0;
            CountVerticesAndIndices(); // Could be simplified a little
            // Knowing the counts, we can specify the size of the vertex and index buffers.
            vertexList = new VertexPositionNormalTexture[numVertices]; // numVertices is now aggregate
            triangleListIndices = new short[indexCount]; // as is indexCount

            // Build the mesh and then fill the vertex and triangle index buffers.
            BuildMesh(worldPosition);

            // This was the old method, which used the final point in the mesh:
            //objectRadius = (float)Math.Pow(Math.Pow(vertexList[numVertices - 1].Position.X, 2) + Math.Pow(vertexList[numVertices - 1].Position.Z, 2), 0.5) * 1.05f;
            // The new method is more straightforward because of single-subsection dynamic track
            if (dtrackData.IsCurved == 0) objectRadius = 0.5f * dtrackData.param1; // half-length
            else objectRadius = dtrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(dtrackData.param1)); // half chord length

            VertexDeclaration = null;
            VertexBuffer = null;
            IndexBuffer = null;
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        } // end DynatrackMesh

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
        public void BuildMesh(WorldPosition worldPosition)
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
        // I'VE LEFT MOST (IF NOT ALL) OF RICK'S STATEMENTS IN THE VERTEX GENERATION METHODS BELOW. (WHN)
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

            // Vertices for the starting cross section
            vertexList[vertexIndex].Position = new Vector3(section[0, 0], section[0, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, uv_vStart);
            vertexIndex++;
            vertexList[vertexIndex].Position = new Vector3(section[1, 0], section[1, 1], 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, uv_vStart);
            vertexIndex++;

            if (dtrackData.IsCurved == 0) // Straight track section
            {
                segmentLength = dtrackData.param1;
                uv_vDisplacement = uv_vIncrement * segmentLength;
                directionVector = new Vector3(0, dtrackData.deltaY, -segmentLength);
                //displacement = Matrix.CreateTranslation(directionVector);
                //directionVector.Normalize();
                // Vertices for the end cross section
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 2].Position, displacement) + directionVector;
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 2].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 2].Position, displacement) + directionVector;
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 2].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                vertexIndex++;
            } // end if straight
            else // Curved track section
            {
                numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData.param1)); // See above for explanation (~1 degree increments).
                if (numSections == 0) numSections++;
                segmentAngle = (dtrackData.param1) / numSections; // Angle in radians by which each successive section of the curved track is rotated.
                segmentLength = Math.Abs(dtrackData.param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.               
                Vector3 ddy = new Vector3(0, dtrackData.deltaY / numSections, 0); // Incremental elevation change

                uv_vDisplacement = uv_vIncrement * segmentLength;

                /*
                // Find the coordinates of the center of curvature.
                center = dtrackData.param2 * Vector3.Cross(Vector3.Up, directionVector);
                // Find the midpoint of the previous ballast face.
                Vector3 midpoint = new Vector3(
                    (vertexList[vertexIndex - 1].Position.X + vertexList[vertexIndex - 2].Position.X)/2,
                    (vertexList[vertexIndex - 1].Position.Y + vertexList[vertexIndex - 2].Position.Y)/2,
                    (vertexList[vertexIndex - 1].Position.Z + vertexList[vertexIndex - 2].Position.Z)/2);
                // Move the center point to align with the previous ballast face. This is where the curve begins.
                displacement = Matrix.CreateTranslation(midpoint);
                center = Vector3.Transform(center, displacement);
                */

                // The approach here is to replicate the previous cross section, 
                // rotated into its position on the curve and vertically displaced if on grade.

                // The local center for the curve lies to the left or right of the local origin and ON THE BASE PLANE
                center = dtrackData.param2 * (dtrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
                // Unit vector giving the "heading" of the current segment (no vertical deflection)
                //directionVector = Vector3.Forward; // Points along the centerline, initially forward
                sectionRotation = Matrix.CreateRotationY(-segmentAngle); // Rotation per iteration (constant)
                Vector3 oldRadius = -center;
                Vector3 oldV; // Vector between new radius vector and old radius vector

                // Generate vertices for the numSections cross sections
                for (int j = 0; j < numSections; j++)
                {
                    // Rotate the direction vector along the curve.
                    //directionVector = Vector3.Transform(directionVector, displacement); // Update orientation
                    radius = Vector3.Transform(oldRadius, sectionRotation);

                    // Calculate the radius vector for the right edge of the ballast.
                    // Get the previous vertex about the local coordinate system
                    oldV = vertexList[vertexIndex - 2].Position - center - oldRadius;
                    // Rotate the point about local origin and reposition it (including elevation change)
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 2].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uLeft, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                    vertexIndex++;

                    // Calculate the radius vector for the left edge of the ballast.
                    oldV = vertexList[vertexIndex - 2].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 2].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;

                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_uRight, vertexList[vertexIndex - 2].TextureCoordinate.Y + uv_vDisplacement);
                    vertexIndex++;

                    oldRadius = radius; // Get ready for next iteration
                }
            } // end else curved
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


            if (dtrackData.IsCurved == 0) // Straight track section
            {
                segmentLength = dtrackData.param1;
                uv_uDisplacement = uv_uIncrement * segmentLength;
                directionVector = new Vector3(0, dtrackData.deltaY, -segmentLength);
                //displacement = Matrix.CreateTranslation(directionVector);
                //directionVector.Normalize();

                // Right, outer side
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                vertexIndex++;
                // Right, inner side
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                vertexIndex++;
                // Left, inner side
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                vertexIndex++;
                // Left, outer side
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 8].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 8].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                vertexIndex++;
            } // end if it's straight
            else // Curved track section.
            {
                numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData.param1)); // See above for explanation
                if (numSections == 0) numSections++;
                segmentAngle = (dtrackData.param1) / numSections; // Angle in radians by which each successive section of the curved track is rotated.
                segmentLength = Math.Abs(dtrackData.param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.
                Vector3 ddy = new Vector3(0, dtrackData.deltaY / numSections, 0);
                uv_uDisplacement = uv_uIncrement * segmentLength;
                /*
                center = dtrackData.param2 * Vector3.Cross(Vector3.Up, directionVector);
                Vector3 midpoint = new Vector3(
                    (vertexList[vertexIndex - 2].Position.X + vertexList[vertexIndex - 8].Position.X) / 2,
                    (vertexList[vertexIndex - 2].Position.Y + vertexList[vertexIndex - 8].Position.Y) / 2,
                    (vertexList[vertexIndex - 2].Position.Z + vertexList[vertexIndex - 8].Position.Z) / 2);
                displacement = Matrix.CreateTranslation(midpoint);
                center = Vector3.Transform(center, displacement);
                */
                // The approach here is to replicate the previous cross section, but rotating it and displacing
                // it to its new position
                // The local center for the curve lies to the left or right of the local origin
                // and ON THE BASE PLANE.
                center = dtrackData.param2 * (dtrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
                // Create rotation matrix (about Y) for per-cross section incremental angle
                sectionRotation = Matrix.CreateRotationY(-segmentAngle); // Rotation per iteration (constant)
                Vector3 oldRadius = -center; // Starting radius vector in x-z plane
                Vector3 oldV; // Vector between new radius vector and old radius vector

                // Generate vertices for the numSections cross sections
                for (int j = 0; j < numSections; j++)
                {
                    //displacement = Matrix.CreateRotationY(-segmentAngle);
                    //directionVector = Vector3.Transform(directionVector, displacement);
                    radius = Vector3.Transform(oldRadius, sectionRotation); // Rotate oldRadius vector to get new

                    // Right, outer side
                    //radius = vertexList[vertexIndex - 8].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //vertexList[vertexIndex].Position = center + radius;
                    //vertexList[vertexIndex].Position.Y += 0.125f; // WHAT IS THIS FOR ?????
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Right, inner side
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 8].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //vertexList[vertexIndex].Position = center + radius;
                    //vertexList[vertexIndex].Position.Y += 0.125f;
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Left, inner side
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 8].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //vertexList[vertexIndex].Position = center + radius;
                    //vertexList[vertexIndex].Position.Y += 0.125f;
                    vertexList[vertexIndex].Normal = new Vector3(1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;
                    // Left, outer side
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 8].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vLower);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 8].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //vertexList[vertexIndex].Position = center + radius;
                    //vertexList[vertexIndex].Position.Y += 0.125f;
                    vertexList[vertexIndex].Normal = new Vector3(-1, 0, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 8].TextureCoordinate.X + uv_uDisplacement, uv_vUpper);
                    vertexIndex++;

                    oldRadius = radius; // Get ready for next iteration
                } // end for
            } // end else it's a curve
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
            //directionVector = Vector3.Forward; // Unit vector giving the "heading" of the current segment
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

            if (dtrackData.IsCurved == 0) // Straight track section
            {
                segmentLength = dtrackData.param1;
                uv_uDisplacement = uv_uIncrement * segmentLength;
                directionVector = new Vector3(0, dtrackData.deltaY, -segmentLength);
                //displacement = Matrix.CreateTranslation(directionVector);
                //directionVector.Normalize();

                // Right top
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 4].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 4].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                vertexIndex++;
                // Left top
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 4].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                vertexIndex++;
                //vertexList[vertexIndex].Position = Vector3.Transform(vertexList[vertexIndex - 4].Position, displacement);
                vertexList[vertexIndex].Position = vertexList[vertexIndex - 4].Position + directionVector;
                vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                vertexIndex++;
            }
            else // Curved track section
            {
                numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData.param1)); // See above for explanation
                if (numSections == 0) numSections++;
                segmentAngle = dtrackData.param1 / numSections; // Angle in radians by which each successive section of the curved track is rotated.
                segmentLength = Math.Abs(dtrackData.param2 * 2 * (float)Math.Sin(segmentAngle / 2)); // Formula: Chord of a circle.
                Vector3 ddy = new Vector3(0, dtrackData.deltaY / numSections, 0); // Incremental elevation change
                uv_uDisplacement = uv_uIncrement * segmentLength;

                /*
                center = dtrackData.param2 * Vector3.Cross(Vector3.Up, directionVector);
                Vector3 midpoint = new Vector3(
                    (vertexList[vertexIndex - 1].Position.X + vertexList[vertexIndex - 4].Position.X) / 2,
                    (vertexList[vertexIndex - 1].Position.Y + vertexList[vertexIndex - 4].Position.Y) / 2,
                    (vertexList[vertexIndex - 1].Position.Z + vertexList[vertexIndex - 4].Position.Z) / 2);
                displacement = Matrix.CreateTranslation(midpoint);
                center = Vector3.Transform(center, displacement);
                */
                // The approach here is to replicate the previous cross section, but rotated and positioned to the next

                // The local center for the curve lies to the left or right of the local origin
                // and ON THE BASE PLANE.
                center = dtrackData.param2 * (dtrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
                // Unit vector giving the "heading" of the current segment (no vertical deflection)
                // directionVector = Vector3.Forward; // Points along the centerline, initially forward
                sectionRotation = Matrix.CreateRotationY(-segmentAngle); // Rotation per iteration (constant)
                Vector3 oldRadius = -center; // Initial old radius vector
                Vector3 oldV; // Vector between new radius vector and old

                // Generate vertices for the numSections cross sections
                for (int j = 0; j < numSections; j++)
                {
                    //displacement = Matrix.CreateRotationY(-segmentAngle);
                    //directionVector = Vector3.Transform(directionVector, displacement);
                    radius = Vector3.Transform(oldRadius, sectionRotation);

                    // Right top
                    // Get the local position vector on the centerline for the previous cross section;
                    // then rotate it about the local origin and reposition it (including elevation change).
                    oldV = vertexList[vertexIndex - 4].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 4].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 4].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 4].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                    vertexIndex++;
                    // Right top
                    oldV = vertexList[vertexIndex - 4].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 4].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vOuter);
                    vertexIndex++;
                    oldV = vertexList[vertexIndex - 4].Position - center - oldRadius;
                    vertexList[vertexIndex].Position = ddy + center + radius + Vector3.Transform(oldV, sectionRotation);
                    //radius = vertexList[vertexIndex - 4].Position - center;
                    //radius = Vector3.Transform(radius, displacement);
                    //vertexList[vertexIndex].Position = center + radius;
                    vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(vertexList[vertexIndex - 4].TextureCoordinate.X + uv_uDisplacement, uv_vInner);
                    vertexIndex++;

                    oldRadius = radius; // Get ready for next iteration
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
            // Check for unused sections
            if (dtrackData.param1 == 0 && dtrackData.param2 == 0)
                return;

            if (dtrackData.IsCurved == 0) // Straight track section
            {
                numVertices += 14;
                indexCount += 21 * 2;
            }
            else
            {
                // Assume one skewed straight section per degree of curvature
                numSections = (int)Math.Abs(MathHelper.ToDegrees(dtrackData.param1));
                if (numSections == 0) numSections++; // Very small radius track - zero avoidance
                numVertices += 14 * numSections; // 14 vertices per cross section
                indexCount += (short)(21 * numSections * 2); // 7 line seg. x 3 seg./tri. x 2 tri.
            }
        } // end CountVerticesAndIndices

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
