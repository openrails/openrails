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
       } // end DynatrackDrawer constructor
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

            if (Viewer.Camera.CanSee(dtrackMesh.MSTSLODCenter, dtrackMesh.ObjectRadius, 500))
            {
                // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
                Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
                xnaXfmWrtCamTile = worldPosition.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
                // (Transformation is now with respect to camera-tile origin)

                frame.AddPrimitive(dtrackMaterial, dtrackMesh, RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.AutoZBias);
            }
        } // end PrepareFrame
    } // end DynatrackDrawer
    #endregion

    #region DynatrackProfile
    // A track profile consists of a number of groups used for LOD considerations.  Here, these groups
    // are called "TrProfileLODItems."  Each group consists of one of more "polylines".  A polyline is a 
    // chain of line segments successively interconnected. A polyline of n segments is defined by n+1 vertices.
    // (Use of a polyline allows for use of more than single segments.  For example, ballast could be defined
    // as left slope, level, right slope - a total of four vertices.)
    public class TrProfile
    {
        public string Name;                            // e.g., "Default track profile"
        public uint NumLODItems;                       // e.g., 4 for embankment, ballast, railtops, railsides
        public uint NumVertices;                       // Total independent vertices in profile
        public uint NumSegments;                       // Total line segment count in profile
        public TrProfileLODItem[] TrProfileLODItems;   // Array of profile items corresponding to levels-of-detail

        public string Image1Name = ""; // For primary texture image file name
        public string Image1sName = "";// For wintertime alternate
        public string Image2Name = ""; // For secondary texture image file name

        /// <summary>
        /// TrProfile constructor
        /// </summary>
        public TrProfile() // Nasty: void return type is not allowed. (See MSDN for compiler error CS0542.)
        {
            // Default TrProfile constructor (possibly temporary)
            TrProfileVertex[] v;
            // We're going to be counting vertices and segments as we create them; so intialize:
            NumVertices = 0;
            NumSegments = 0;

            Name = "Default Dynatrack profile";
            Image1Name = "acleantrack1.ace";
            Image1sName = "acleantrack1.ace";
            Image2Name = "acleantrack2.ace";
            NumLODItems = 3; // Ballast, railtops, railsides
            TrProfileLODItems = new TrProfileLODItem[NumLODItems];

            // Make ballast
            TrProfileLODItems[0] = new TrProfileLODItem("Ballast", 1);
            TrProfileLODItems[0].CutoffRadius = 2000.0f;
            TrProfileLODItems[0].MipMapLevelOfDetailBias = -1;
            TrProfileLODItems[0].AlphaBlendEnable = true;
            TrProfileLODItems[0].AlphaTestEnable = false;

            TrProfileLODItems[0].Polylines[0] = new Polyline(this, "ballast", 2, out v);
            TrProfileLODItems[0].Polylines[0].DeltaTexCoord = new Vector2(0.0f, 0.2088545f);
            v[0] = new TrProfileVertex(-2.5f, 0.2f, 0.0f, 0f, 1f, 0f, -.153916f, -.280582f);
            v[1] = new TrProfileVertex(2.5f, 0.2f, 0.0f, 0f, 1f, 0f, .862105f, -.280582f);
            TrProfileLODItems[0].Polylines[0].TrProfileVertices = v;
            
            // make railtops
            TrProfileLODItems[1] = new TrProfileLODItem("Railtops", 2);
            TrProfileLODItems[1].CutoffRadius = 1200.0f;
            TrProfileLODItems[1].MipMapLevelOfDetailBias = 0;
            TrProfileLODItems[1].AlphaBlendEnable = false;
            TrProfileLODItems[1].AlphaTestEnable = false;

            TrProfileLODItems[1].Polylines[0] = new Polyline(this, "right", 2, out v);
            TrProfileLODItems[1].Polylines[0].DeltaTexCoord = new Vector2(.0744726f, 0f);
            v[0] = new TrProfileVertex(-.8675f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f);
            v[1] = new TrProfileVertex(-.7175f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f);
            TrProfileLODItems[1].Polylines[0].TrProfileVertices = v; 
   
            TrProfileLODItems[1].Polylines[1] = new Polyline(this, "left", 2, out v);
            TrProfileLODItems[1].Polylines[1].DeltaTexCoord = new Vector2(.0744726f, 0f);
            v[0] = new TrProfileVertex(.7175f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f);
            v[1] = new TrProfileVertex(.8675f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f);
            TrProfileLODItems[1].Polylines[1].TrProfileVertices = v;

            // make railsides
            TrProfileLODItems[2] = new TrProfileLODItem("Railsides", 4);
            TrProfileLODItems[2].CutoffRadius = 700.0f;
            TrProfileLODItems[2].MipMapLevelOfDetailBias = 0;
            TrProfileLODItems[2].AlphaBlendEnable = false;
            TrProfileLODItems[2].AlphaTestEnable = false;

            TrProfileLODItems[2].Polylines[0] = new Polyline(this, "left_outer", 2, out v);
            TrProfileLODItems[2].Polylines[0].DeltaTexCoord = new Vector2(.1673372f, 0f);
            v[0] = new TrProfileVertex(-.8675f, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f);
            v[1] = new TrProfileVertex(-.8675f, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f);
            TrProfileLODItems[2].Polylines[0].TrProfileVertices = v;

            TrProfileLODItems[2].Polylines[1] = new Polyline(this, "left_inner", 2, out v);
            TrProfileLODItems[2].Polylines[1].DeltaTexCoord = new Vector2(.1673372f, 0f);
            v[1] = new TrProfileVertex(-.7175f, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f);
            v[0] = new TrProfileVertex(-.7175f, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f);
            TrProfileLODItems[2].Polylines[1].TrProfileVertices = v;

            TrProfileLODItems[2].Polylines[2] = new Polyline(this, "right_inner", 2, out v);
            TrProfileLODItems[2].Polylines[2].DeltaTexCoord = new Vector2(.1673372f, 0f);
            v[0] = new TrProfileVertex(.7175f, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f);
            v[1] = new TrProfileVertex(.7175f, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f);
            TrProfileLODItems[2].Polylines[2].TrProfileVertices = v;
            
            TrProfileLODItems[2].Polylines[3] = new Polyline(this, "right_outer", 2, out v);
            TrProfileLODItems[2].Polylines[3].DeltaTexCoord = new Vector2(.1673372f, 0f);
            v[1] = new TrProfileVertex(.8675f, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f);
            v[0] = new TrProfileVertex(.8675f, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f);
            TrProfileLODItems[2].Polylines[3].TrProfileVertices = v;
        } // end TrProfile() constructor
    } // end TrProfile

    public class TrProfileLODItem
    {
        public string Name;                            // e.g., "Rail sides"
        public uint NumPolylines;                      // e.g., 4 for left-outer, left-inner, right-inner, right-outer
        public Polyline[] Polylines;                   // Array of arrays of vertices
        public float CutoffRadius;                     // Distance beyond which LOD is not seen

        public float MipMapLevelOfDetailBias;
        public bool AlphaBlendEnable;
        public bool AlphaTestEnable;

        /// <summary>
        /// TrProfileLODITem constructor
        /// </summary>
        public TrProfileLODItem(string name, uint num)
        {
            Name = name;
            NumPolylines = num;
            Polylines = new Polyline[NumPolylines];
        } // end TrProfileLODItem() constructor
    } // end TrProfileLODItem

    public class Polyline
    {
        public string Name;                            // e.g., "1:1 embankment"
        private uint NumVertices;                      // e.g., 4 for left-bottom, left-top, right-top, right-bottom

        public TrProfileVertex[] TrProfileVertices;     // Array of vertices
        public Vector2 DeltaTexCoord;                   // Incremental change in (u, v) from one cross section to the next

        /// <summary>
        /// Polyline constructor
        /// </summary>
        public Polyline(TrProfile parent, string name, uint num, out TrProfileVertex[] vertices)
        {
            Name = name;
            this.NumVertices = num;
            parent.NumVertices += num;
            parent.NumSegments += num - 1;
            TrProfileVertices = new TrProfileVertex[num];
            vertices = TrProfileVertices;
        } // end Polyline() constructor
    } // end Polyline

    public struct TrProfileVertex
    {
        public Vector3 Position;                           // Position vector (x, y, z)
        public Vector3 Normal;                             // Normal vector (nx, ny, nz)
        public Vector2 TexCoord;                           // Texture coordinate (u, v)

        public TrProfileVertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
            Position = new Vector3(x, y, z);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
        } // end TrProfileVertex() constructor
    } // end TrProfileVertex
    #endregion

    #region DynatrackMesh
    public class DynatrackMesh : RenderPrimitive
    {
        VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        IndexBuffer IndexBuffer;

        VertexPositionNormalTexture[] VertexList; // Array of vertices
        short[] TriangleListIndices;// Array of indices to vertices for triangles
        uint VertexIndex = 0;       // Index of current position in VertexList
        uint IndexIndex = 0;        // Index of current position in TriangleListIndices
        int VertexStride;           // in bytes
        int NumVertices;            // Number of vertices in the track profile
        short NumIndices;           // Number of triangle indices

        // LOD member variables:
        public int DrawIndex;       // Used by Draw to determine which primitive to draw.
        public Vector3 XNAEnd;      // Location of termination-of-section (as opposed to root)
        public float ObjectRadius;  // Radius of bounding sphere
        public Vector3 MSTSLODCenter; // Center of bounding sphere
        public struct GridItem
        {
            public uint VertexOrigin;// Start index for first vertex in LOD
            public uint VertexLength;// Number of vertices in LOD
            public uint IndexOrigin; // Start index for first triangle in LOD
            public uint IndexLength; // Number of triangle vertex indicies in LOD
            //public float CutoffRadius; // Distance beyond which LOD is not seen
        }
        public GridItem[] LODGrid;   // Grid matrix

        // Geometry member variables:
        int NumSections;            // Number of cross sections needed to make up a track section.
        float SegmentLength;        // meters if straight; radians if circular arc
        Vector3 DDY;                // Elevation (y) change from one cross section to next
        Vector3 OldV;               // Deviation from centerline for previous cross section
        Vector3 OldRadius;          // Radius vector to centerline for previous cross section

        //TODO: Candidates for re-packaging:
        Matrix sectionRotation;     // Rotates previous profile into next profile position on curve.
        Vector3 center;             // Center coordinates of curve radius
        Vector3 radius;             // Radius vector to cross section on curve centerline

        // This structure holds the basic geometric parameters of a DT section.
        public struct DtrackData
        {
            public int IsCurved;    // Straight (0) or circular arc (1)
            public float param1;    // Length in meters (straight) or radians (circular arc)
            public float param2;    // Radius for circular arc
            public float deltaY;    // Change in elevation (y) from beginning to end of section
        }
        DtrackData DTrackData;      // Was: DtrackData[] dtrackData;

        public uint UiD; // Used for debugging only

        public TrProfile TrProfile;

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

            // In this implementation dtrack has only 1 DT subsection.
            if (dtrack.trackSections.Count != 1)
            {
                throw new ApplicationException(
                    "DynatrackMesh Constructor detected a multiple-subsection dynamic track section. " +
                    "(SectionIdx = " + dtrack.SectionIdx + ")");
            }
            // Initialize a scalar DtrackData object
            DTrackData = new DtrackData();
            DTrackData.IsCurved = (int)dtrack.trackSections[0].isCurved;
            DTrackData.param1 = dtrack.trackSections[0].param1;
            DTrackData.param2 = dtrack.trackSections[0].param2;
            DTrackData.deltaY = dtrack.trackSections[0].deltaY;
            XNAEnd = endPosition.XNAMatrix.Translation;

            TrProfile = renderProcess.Viewer.Simulator.TrackProfile;

            // Build the mesh, filling the vertex and triangle index buffers.
            BuildMesh(worldPosition); // Build vertexList and triangleListIndices

            if (DTrackData.IsCurved == 0) ObjectRadius = 0.5f * DTrackData.param1; // half-length
            else ObjectRadius = DTrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(DTrackData.param1)); // half chord length

            VertexDeclaration = null;
            VertexBuffer = null;
            IndexBuffer = null;
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        } // end DynatrackMesh constructor

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (DrawIndex < 0 || DrawIndex >= TrProfile.NumLODItems) return;

            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexStride);
            graphicsDevice.Indices = IndexBuffer;

            graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        (int)LODGrid[DrawIndex].VertexOrigin,
                        (int)LODGrid[DrawIndex].VertexLength,
                        (int)LODGrid[DrawIndex].IndexOrigin,
                        (int)LODGrid[DrawIndex].IndexLength / 3);
        } // end Draw

        #region Vertex and triangle index generators
        /// <summary>
        /// Builds a section of Dynatrack to TrProfile specifications as one vertex buffer and one index buffer.
        /// The order the buffers are built in reflects the nesting in the TrProfile.  The nesting order is:
        /// (LOD items (Polylines (Vertices))).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        public void BuildMesh(WorldPosition worldPosition)
        {
            LODGrid = new GridItem[TrProfile.TrProfileLODItems.Length];

            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0) LinearGen(); else CircArcGen();
            // Count vertices and indices
            NumVertices = (int)(TrProfile.NumVertices * NumSections + TrProfile.NumVertices);
            NumIndices = (short)(TrProfile.NumSegments * NumSections * 6);
            // (Cells x 2 triangles/cell x 3 indices/triangle)

            // Allocate memory for vertices and indices
            VertexList = new VertexPositionNormalTexture[NumVertices]; // numVertices is now aggregate
            TriangleListIndices = new short[NumIndices]; // as is NumIndices

            uint iLOD = 0;
            foreach (TrProfileLODItem lod in TrProfile.TrProfileLODItems) 
            {
                LODGrid[iLOD].VertexOrigin = VertexIndex;   // Initial vertex index for this LOD
                LODGrid[iLOD].IndexOrigin = IndexIndex;     // Initial index index for this LOD
                //LODGrid[iLOD].CutoffRadius = TrProfile.TrProfileLODItems[iLOD].CutoffRadius;

                // Initial load of baseline cross section polylines for this LOD only:
                foreach (Polyline pl in lod.Polylines)
                {
                    foreach (TrProfileVertex v in pl.TrProfileVertices)
                    {
                        VertexList[VertexIndex].Position = v.Position;
                        VertexList[VertexIndex].Normal = v.Normal;
                        VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                        VertexIndex++;
                    }
                }
                // Number of vertices and indicies for this LOD only
                LODGrid[iLOD].VertexLength = VertexIndex - LODGrid[iLOD].VertexOrigin;  
                LODGrid[iLOD].IndexLength = IndexIndex - LODGrid[iLOD].IndexOrigin;
                // Initial load of base cross section complete

                // Now generate and load subsequent cross sections
                OldRadius = -center;
                uint stride = LODGrid[iLOD].VertexLength;
                for (uint i = 0; i < NumSections; i++)
                {
                    foreach (Polyline pl in lod.Polylines)
                    {
                        uint plv = 0; // Polyline vertex index
                        foreach (TrProfileVertex v in pl.TrProfileVertices)
                        {
                            if (DTrackData.IsCurved == 0) LinearGen(stride, pl); // Generation call
                            else CircArcGen(stride, pl);

                            if (plv > 0)
                            {
                                // Sense for triangles is clockwise
                                // First triangle:
                                TriangleListIndices[IndexIndex++] = (short)VertexIndex;
                                TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1 - stride);
                                TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1);
                                // Second triangle:
                                TriangleListIndices[IndexIndex++] = (short)VertexIndex;
                                TriangleListIndices[IndexIndex++] = (short)(VertexIndex - stride);
                                TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1 - stride);
                            }
                            VertexIndex++;
                            plv++;
                        } // end foreach v  
                    } // end foreach pl
                    OldRadius = radius; // Get ready for next segment
                } // end for i
                LODGrid[iLOD].VertexLength = VertexIndex - LODGrid[iLOD].VertexOrigin;
                LODGrid[iLOD].IndexLength = IndexIndex - LODGrid[iLOD].IndexOrigin;
                iLOD++; // Step LOD index
            } // end foreach lod
        } // end BuildMesh

        /// <summary>
        /// Initializes member variables for straight track sections.
        /// </summary>
        void LinearGen()
        {
            // Define the number of track cross sections in addition to the base.
            NumSections = 1;
            //numSections = 10; //TESTING
            // TODO: Generalize count to profile file specification

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (meters)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change
        } // end LinearGen

        /// <summary>
        /// Initializes member variables for circular arc track sections.
        /// </summary>
        void CircArcGen()
        {
            // Define the number of track cross sections in addition to the base.
            // Assume one skewed straight section per degree of curvature
            NumSections = (int)Math.Abs(MathHelper.ToDegrees(DTrackData.param1));
            if (NumSections == 0) NumSections++; // Very small radius track - zero avoidance
            //numSections = 10; //TESTING
            // TODO: Generalize count to profile file specification

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (radians)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change

            // The approach here is to replicate the previous cross section, 
            // rotated into its position on the curve and vertically displaced if on grade.
            // The local center for the curve lies to the left or right of the local origin and ON THE BASE PLANE
            center = DTrackData.param2 * (DTrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
            sectionRotation = Matrix.CreateRotationY(-SegmentLength); // Rotation per iteration (constant)
        } // end CircArcGen

        /// <summary>
        /// Generates vertices for a succeeding cross section (straight track).
        /// </summary>
        /// <param name="stride">Index increment between section-to-section vertices.</param>
        void LinearGen(uint stride, Polyline pl)
        {
            Vector3 displacement = new Vector3(0, 0, -SegmentLength) + DDY;
            float wrapLength = displacement.Length();
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            Vector3 p = VertexList[VertexIndex - stride].Position + displacement;
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement; 

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

        /// <summary>
        /// /// Generates vertices for a succeeding cross section (circular arc track).
        /// </summary>
        /// <param name="stride">Index increment between section-to-section vertices.</param>
        void CircArcGen(uint stride, Polyline pl)
        {
            // Get the previous vertex about the local coordinate system
            OldV = VertexList[VertexIndex - stride].Position - center - OldRadius;
            // Rotate the old radius vector to become the new radius vector
            radius = Vector3.Transform(OldRadius, sectionRotation);
            float wrapLength = (radius - OldRadius).Length(); // Wrap length is centerline chord
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            // Rotate the point about local origin and reposition it (including elevation change)
            Vector3 p = DDY + center + radius + Vector3.Transform(OldV, sectionRotation);
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement; 

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

        #endregion

        #region Helpers

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
            VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * VertexList.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(VertexList);
            if (IndexBuffer == null)
            {
                IndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * NumIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                IndexBuffer.SetData<short>(TriangleListIndices);
            }
        }
        #endregion
    }
    #endregion
}
