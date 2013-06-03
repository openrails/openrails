// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

/* OVERHEAD WIRE
 * 
 * Overhead wire is generated procedurally from data in the track database.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;

namespace ORTS
{
    public class Wire
    {
        /// <summary>
        /// Decompose and add a wire on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static int DecomposeStaticWire(Viewer3D viewer, List<DynatrackDrawer> dTrackList, TrackObj dTrackObj,
            WorldPosition worldMatrixInput)
        {
            // The following vectors represent local positioning relative to root of original (5-part) section:
            Vector3 localV = Vector3.Zero; // Local position (in x-z plane)
            Vector3 localProjectedV; // Local next position (in x-z plane)
            Vector3 displacement;  // Local displacement (from y=0 plane)
            Vector3 heading = Vector3.Forward; // Local heading (unit vector)

            WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Make a copy so it will not be messed

            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root

            WorldPosition wcopy = new WorldPosition(nextRoot);
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Save root position
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only
            try
            {
                if (viewer.Simulator.TSectionDat.TrackShapes.Get(dTrackObj.SectionIdx).RoadShape == true) return 1;
            }
            catch (Exception)
            {
                return 0;
            }
            SectionIdx[] SectionIdxs = viewer.Simulator.TSectionDat.TrackShapes.Get(dTrackObj.SectionIdx).SectionIdxs;

            foreach (SectionIdx id in SectionIdxs)
            {
                nextRoot = new WorldPosition(wcopy); // Will become initial root
                sectionOrigin = nextRoot.XNAMatrix.Translation;

                heading = Vector3.Forward; // Local heading (unit vector)
                localV = Vector3.Zero; // Local position (in x-z plane)


                Vector3 trackLoc = new Vector3((float)id.X, (float)id.Y, (float)id.Z);// +new Vector3(3, 0, 0);
                Matrix trackRot = Matrix.CreateRotationY(-(float)id.A * 3.14f / 180);

                //heading = Vector3.Transform(heading, trackRot); // Heading change
                nextRoot.XNAMatrix = trackRot * nextRoot.XNAMatrix;
                uint[] sections = id.TrackSections;

                for (int i = 0; i < sections.Length; i++)
                {
                    float length, radius;
                    uint sid = id.TrackSections[i];
                    TrackSection section = viewer.Simulator.TSectionDat.TrackSections[sid];
                    WorldPosition root = new WorldPosition(nextRoot);
                    nextRoot.XNAMatrix.Translation = Vector3.Zero;

                    if (section.SectionCurve == null)
                    {
                        length = section.SectionSize.Length;
                        radius = -1;
                        localProjectedV = localV + length * heading;
                        displacement = Traveller.MSTSInterpolateAlongStraight(localV, heading, length,
                                                                worldMatrix.XNAMatrix, out localProjectedV);
                    }
                    else
                    {
                        length = section.SectionCurve.Angle * 3.14f / 180;
                        radius = section.SectionCurve.Radius; // meters

                        Vector3 left;
                        if (section.SectionCurve.Angle > 0) left = radius * Vector3.Cross(Vector3.Down, heading); // Vector from PC to O
                        else left = radius * Vector3.Cross(Vector3.Up, heading); // Vector from PC to O
                        Matrix rot = Matrix.CreateRotationY(-section.SectionCurve.Angle * 3.14f / 180); // Heading change (rotation about O)

                        Matrix rot2 = Matrix.CreateRotationY(-(90 - section.SectionCurve.Angle) * 3.14f / 180); // Heading change (rotation about O)
                        displacement = Traveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                                worldMatrix.XNAMatrix, out localProjectedV);

                        heading = Vector3.Transform(heading, rot); // Heading change
                        nextRoot.XNAMatrix = trackRot * rot * nextRoot.XNAMatrix; // Store heading change

                    }
                    nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;
                    root.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                    //nextRoot.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                    dTrackList.Add(new WireDrawer(viewer, root, nextRoot, radius, length));
                    localV = localProjectedV; // Next subsection
                }
            }
            return 1;
        } // end DecomposeStaticWire

        /// <summary>
        /// Decompose and add a wire on top of MSTS track section converted from dynamic tracks
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeConvertedDynamicWire(Viewer3D viewer, List<DynatrackDrawer> dTrackList, TrackObj dTrackObj,
            WorldPosition worldMatrixInput)
        {
            // The following vectors represent local positioning relative to root of original (5-part) section:
            Vector3 localV = Vector3.Zero; // Local position (in x-z plane)
            Vector3 localProjectedV; // Local next position (in x-z plane)
            Vector3 displacement;  // Local displacement (from y=0 plane)
            Vector3 heading = Vector3.Forward; // Local heading (unit vector)

            WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Make a copy so it will not be messed

            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root

            WorldPosition wcopy = new WorldPosition(nextRoot);
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Save root position
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

            TrackPath path;

            try
            {
                path = viewer.Simulator.TSectionDat.TSectionIdx.TrackPaths[dTrackObj.SectionIdx];
            }
            catch (Exception)
            {
                return; //cannot find the path for the dynamic track
            }

            nextRoot = new WorldPosition(wcopy); // Will become initial root
            sectionOrigin = nextRoot.XNAMatrix.Translation;

            heading = Vector3.Forward; // Local heading (unit vector)
            localV = Vector3.Zero; // Local position (in x-z plane)


            Vector3 trackLoc = new Vector3(0, 0, 0);// +new Vector3(3, 0, 0);
            Matrix trackRot = Matrix.CreateRotationY(0);

            //heading = Vector3.Transform(heading, trackRot); // Heading change
            nextRoot.XNAMatrix = trackRot * nextRoot.XNAMatrix;
            uint[] sections = path.TrackSections;

            for (int i = 0; i < sections.Length; i++)
            {
                float length, radius;
                uint sid = path.TrackSections[i];
                TrackSection section = viewer.Simulator.TSectionDat.TrackSections[sid];
                WorldPosition root = new WorldPosition(nextRoot);
                nextRoot.XNAMatrix.Translation = Vector3.Zero;

                if (section.SectionCurve == null)
                {
                    length = section.SectionSize.Length;
                    radius = -1;
                    localProjectedV = localV + length * heading;
                    displacement = Traveller.MSTSInterpolateAlongStraight(localV, heading, length,
                                                            worldMatrix.XNAMatrix, out localProjectedV);
                }
                else
                {
                    length = section.SectionCurve.Angle * 3.14f / 180;
                    radius = section.SectionCurve.Radius; // meters

                    Vector3 left;
                    if (section.SectionCurve.Angle > 0) left = radius * Vector3.Cross(Vector3.Down, heading); // Vector from PC to O
                    else left = radius * Vector3.Cross(Vector3.Up, heading); // Vector from PC to O
                    Matrix rot = Matrix.CreateRotationY(-section.SectionCurve.Angle * 3.14f / 180); // Heading change (rotation about O)

                    Matrix rot2 = Matrix.CreateRotationY(-(90 - section.SectionCurve.Angle) * 3.14f / 180); // Heading change (rotation about O)
                    displacement = Traveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                            worldMatrix.XNAMatrix, out localProjectedV);

                    heading = Vector3.Transform(heading, rot); // Heading change
                    nextRoot.XNAMatrix = trackRot * rot * nextRoot.XNAMatrix; // Store heading change

                }
                nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;
                root.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                nextRoot.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                dTrackList.Add(new WireDrawer(viewer, root, nextRoot, radius, length));
                localV = localProjectedV; // Next subsection
            }
        } // end DecomposeStaticWire
        /// <summary>
        /// Decompose and add a wire on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeDynamicWire(Viewer3D viewer, List<DynatrackDrawer> dTrackList, DyntrackObj dTrackObj,
            WorldPosition worldMatrixInput)
        {
            // DYNAMIC TRACK
            // =============
            // Objectives:
            // 1-Decompose multi-subsection DT into individual sections.  
            // 2-Create updated transformation objects (instances of WorldPosition) to reflect 
            //   root of next subsection.
            // 3-Distribute elevation change for total section through subsections. (ABANDONED)
            // 4-For each meaningful subsection of dtrack, build a separate DynatrackMesh.
            //
            // Method: Iterate through each subsection, updating WorldPosition for the root of
            // each subsection.  The rotation component changes only in heading.  The translation 
            // component steps along the path to reflect the root of each subsection.

            // The following vectors represent local positioning relative to root of original (5-part) section:
            Vector3 localV = Vector3.Zero; // Local position (in x-z plane)
            Vector3 localProjectedV; // Local next position (in x-z plane)
            Vector3 displacement;  // Local displacement (from y=0 plane)
            Vector3 heading = Vector3.Forward; // Local heading (unit vector)

            WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Make a copy so it will not be messed

            float realRun; // Actual run for subsection based on path


            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Save root position
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

            // Iterate through all subsections
            for (int iTkSection = 0; iTkSection < dTrackObj.trackSections.Count; iTkSection++)
            {
                float length = 0, radius = -1;

                length = dTrackObj.trackSections[iTkSection].param1; // meters if straight; radians if curved
                if (length == 0.0) continue; // Consider zero-length subsections vacuous

                // Create new DT object copy; has only one meaningful subsection
                DyntrackObj subsection = new DyntrackObj(dTrackObj, iTkSection);

                // Create a new WorldPosition for this subsection, initialized to nextRoot,
                // which is the WorldPosition for the end of the last subsection.
                // In other words, beginning of present subsection is end of previous subsection.
                WorldPosition root = new WorldPosition(nextRoot);

                // Now we need to compute the position of the end (nextRoot) of this subsection,
                // which will become root for the next subsection.

                // Clear nextRoot's translation vector so that nextRoot matrix contains rotation only
                nextRoot.XNAMatrix.Translation = Vector3.Zero;

                // Straight or curved subsection?
                if (subsection.trackSections[0].isCurved == 0) // Straight section
                {   // Heading stays the same; translation changes in the direction oriented
                    // Rotate Vector3.Forward to orient the displacement vector
                    localProjectedV = localV + length * heading;
                    displacement = Traveller.MSTSInterpolateAlongStraight(localV, heading, length,
                                                            worldMatrix.XNAMatrix, out localProjectedV);
                    realRun = length;
                }
                else // Curved section
                {   // Both heading and translation change 
                    // nextRoot is found by moving from Point-of-Curve (PC) to
                    // center (O)to Point-of-Tangent (PT).
                    radius = subsection.trackSections[0].param2; // meters
                    Vector3 left = radius * Vector3.Cross(Vector3.Up, heading); // Vector from PC to O
                    Matrix rot = Matrix.CreateRotationY(-length); // Heading change (rotation about O)
                    // Shared method returns displacement from present world position and, by reference,
                    // local position in x-z plane of end of this section
                    displacement = Traveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                            worldMatrix.XNAMatrix, out localProjectedV);

                    heading = Vector3.Transform(heading, rot); // Heading change
                    nextRoot.XNAMatrix = rot * nextRoot.XNAMatrix; // Store heading change
                    realRun = radius * ((length > 0) ? length : -length); // Actual run (meters)
                }

                // Update nextRoot with new translation component
                nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;


                // Create a new DynatrackDrawer for the subsection
                dTrackList.Add(new WireDrawer(viewer, root, nextRoot, radius, length));
                localV = localProjectedV; // Next subsection
            }
        } // end DecomposeDynamicWire

    } // end class Wires

    public class WireDrawer : DynatrackDrawer
    {
        public WireDrawer(Viewer3D viewer, WorldPosition position, WorldPosition endPosition, float radius, float angle)
            : base(viewer, position, endPosition)
        {

            // Instantiate classes
            dtrackMesh = new WireMesh(viewer.RenderProcess, position, endPosition, radius, angle);
        } // end DynatrackDrawer constructor


    } // end WireDrawer

    public class LODWire : LOD
    {
        public LODWire(float cutoffRadius)
            : base(cutoffRadius)
        {
        }
    }

    public class LODItemWire : LODItem
    {
        // NumVertices and NumSegments used for sizing vertex and index buffers
        public uint VerticalNumVertices = 0;                     // Total independent vertices in LOD
        public uint VerticalNumSegments = 0;                     // Total line segment count in LOD
        public ArrayList VerticalPolylines = new ArrayList();  // Array of arrays of vertices 

        /// <summary>
        /// LODItemWire constructor (default &amp; XML)
        /// </summary>
        public LODItemWire(string name)
            : base(name)
        {
        } // end LODItem() constructor

        public void VerticalAccum(int count)
        {
            // Accumulates total independent vertices and total line segments
            // Used for sizing of vertex and index buffers
            VerticalNumVertices += (uint)count;
            VerticalNumSegments += (uint)count - 1;
        } // end Accum

    } // end class LODItemWire


    // Dynamic Wire profile class
    public class WireProfile : TrProfile
    {
        public float expectedSegmentLength;

        /// <summary>
        /// WireProfile constructor (default - builds from self-contained data)
        /// </summary>
        public WireProfile(RenderProcess RenderProcess) // Nasty: void return type is not allowed. (See MSDN for compiler error CS0542.)
            : base(RenderProcess, 0)//call the dummy base constructor so that no data is pre-populated
        {
            LODMethod = LODMethods.ComponentAdditive;
            LODWire lod; // Local LOD instance 
            LODItemWire lodItem; // Local LODItem instance
            Polyline pl; // Local polyline instance
            Polyline vertical;

            expectedSegmentLength = 40; //segment of wire is expected to be 40 meters

            lod = new LODWire(800.0f); // Create LOD for railsides with specified CutoffRadius
            lodItem = new LODItemWire("Wire");
            if (File.Exists(RenderProcess.Viewer.Simulator.RoutePath + "\\Textures\\overheadwire.ace"))
                lodItem.TexName = "overheadwire.ace";
            else
            {
                Trace.TraceInformation("Ignored missing overheadwire.ace, using default. You can copy the overheadwire.ace from OR\'s AddOns folder to {0}\\Textures", RenderProcess.Viewer.Simulator.RoutePath);
                lodItem.TexName = "..\\..\\..\\global\\textures\\dieselsmoke.ace";
            }
            lodItem.ShaderName = "TexDiff";
            lodItem.LightModelName = "OptSpecular0";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = 0;
            lodItem.MipMapLevelOfDetailBias = 0;
            lodItem.LoadMaterial(RenderProcess, lodItem);

            float topHeight = (float)RenderProcess.Viewer.Simulator.TRK.Tr_RouteFile.OverheadWireHeight;

            float u1 = 0.25f, v1 = 0.25f;
            pl = new Polyline(this, "TopWire", 5);
            pl.DeltaTexCoord = new Vector2(0.00f, 0.00f);

            pl.Vertices.Add(new Vertex(-0.01f, topHeight + 0.02f, 0.0f, -1f, 0f, 0f, u1, v1));
            pl.Vertices.Add(new Vertex(0.01f, topHeight + 0.02f, 0.0f, 0f, 1f, 0f, u1, v1));
            pl.Vertices.Add(new Vertex(0.01f, topHeight, 0.0f, 1f, 0f, 0f, u1, v1));
            pl.Vertices.Add(new Vertex(-0.01f, topHeight, 0.0f, 0f, -1f, 0f, u1, v1));
            pl.Vertices.Add(new Vertex(-0.01f, topHeight + 0.02f, 0.0f, -1f, 0f, 0f, u1, v1));
            lodItem.Polylines.Add(pl); 
            lodItem.Accum(pl.Vertices.Count);

            if (RenderProcess.Viewer.Settings.DoubleWire)
            {
                pl = new Polyline(this, "TopWire1", 5);
                pl.DeltaTexCoord = new Vector2(0.00f, 0.00f);
                topHeight += 1.0f;

                pl.Vertices.Add(new Vertex(-0.01f, topHeight + 0.02f, 0.0f, -1f, 0f, 0f, u1, v1));
                pl.Vertices.Add(new Vertex(0.01f, topHeight + 0.02f, 0.0f, 0f, 1f, 0f, u1, v1));
                pl.Vertices.Add(new Vertex(0.01f, topHeight, 0.0f, 1f, 0f, 0f, u1, v1));
                pl.Vertices.Add(new Vertex(-0.01f, topHeight, 0.0f, 0f, -1f, 0f, u1, v1));
                pl.Vertices.Add(new Vertex(-0.01f, topHeight + 0.04f, 0.0f, -1f, 0f, 0f, u1, v1));
                lodItem.Polylines.Add(pl);
                lodItem.Accum(pl.Vertices.Count);

                vertical = new Polyline(this, "TopWireVertical", 5);
                vertical.DeltaTexCoord = new Vector2(0.00f, 0.00f);

                vertical.Vertices.Add(new Vertex(-0.008f, topHeight, 0.008f, 0f, 0f, 1f, u1, v1));
                vertical.Vertices.Add(new Vertex(-.008f, topHeight, -.008f, -1f, 0f, 0f, u1, v1));
                vertical.Vertices.Add(new Vertex(.008f, topHeight, -.008f, 0f, 0f, -1f, u1, v1));
                vertical.Vertices.Add(new Vertex(.008f, topHeight, .008f, 1f, 0f, 0f, u1, v1));
                vertical.Vertices.Add(new Vertex(-.008f, topHeight, .008f, 1f, 0f, 0f, u1, v1));
                lodItem.VerticalPolylines = new ArrayList();
                lodItem.VerticalPolylines.Add(vertical);
                lodItem.VerticalAccum(vertical.Vertices.Count);
            }


            lod.LODItems.Add(lodItem); // Append to LODItems array 
            base.LODs.Add(lod); // Append this lod to LODs array

        } // end WireProfile() constructor


    } // end class WireProfile




    public class WireMesh : DynatrackMesh
    {
        static WireProfile WireProfile;
        public WireMesh(RenderProcess renderProcess, WorldPosition worldPosition,
        WorldPosition endPosition, float radius, float angle)
            : base()
        {
            // DynatrackMesh is responsible for creating a mesh for a section with a single subsection.
            // It also must update worldPosition to reflect the end of this subsection, subsequently to
            // serve as the beginning of the next subsection.


            // The track cross section (profile) vertex coordinates are hard coded.
            // The coordinates listed here are those of default MSTS "A1t" track.
            // TODO: Read this stuff from a file. Provide the ability to use alternative profiles.

            // Initialize a scalar DtrackData object
            DTrackData = new DtrackData();
            if (radius < 0)
            {
                DTrackData.IsCurved = 0;
                DTrackData.param1 = angle;
                DTrackData.param2 = 0;

            }
            else
            {
                DTrackData.IsCurved = 1;
                DTrackData.param1 = angle;
                DTrackData.param2 = radius;
            }
            DTrackData.deltaY = 0;

            if (WireProfile == null)
            {
                WireProfile = new WireProfile(renderProcess);
            }
            TrProfile = WireProfile;

            XNAEnd = endPosition.XNAMatrix.Translation;

            // Count all of the LODItems in all the LODs
            int count = 0;
            for (int i = 0; i < TrProfile.LODs.Count; i++)
            {
                LOD lod = (LOD)TrProfile.LODs[i];
                count += lod.LODItems.Count;
            }
            // Allocate ShapePrimitives array for the LOD count
            ShapePrimitives = new ShapePrimitive[count];

            // Build the meshes for all the LODs, filling the vertex and triangle index buffers.
            int primIndex = 0;
            for (int iLOD = 0; iLOD < TrProfile.LODs.Count; iLOD++)
            {
                LOD lod = (LOD)TrProfile.LODs[iLOD];
                lod.PrimIndexStart = primIndex; // Store start index for this LOD
                for (int iLODItem = 0; iLODItem < lod.LODItems.Count; iLODItem++)
                {
                    // Build vertexList and triangleListIndices
                    ShapePrimitives[primIndex] = BuildMesh(renderProcess.Viewer, worldPosition, iLOD, iLODItem);
                    primIndex++;
                }
                lod.PrimIndexStop = primIndex; // 1 above last index for this LOD
            }


            if (DTrackData.IsCurved == 0) ObjectRadius = 0.5f * DTrackData.param1; // half-length
            else ObjectRadius = DTrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(DTrackData.param1)); // half chord length


        } // end WireMesh constructor


        /// <summary>
        /// Builds a Wire LOD to WireProfile specifications as one vertex buffer and one index buffer.
        /// The order in which the buffers are built reflects the nesting in the TrProfile.  The nesting order is:
        /// (Polylines (Vertices)).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="worldPosition">WorldPosition.</param>
        /// <param name="iLOD">Index of LOD mesh to be generated from profile.</param>
        /// <param name="iLODItem">Index of LOD mesh to be generated from profile.</param>
        public new ShapePrimitive BuildMesh(Viewer3D viewer, WorldPosition worldPosition, int iLOD, int iLODItem)
        {
            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0) LinearGen();
            else CircArcGen();

            // Count vertices and indices
            LODWire lod = (LODWire)TrProfile.LODs[iLOD];
            LODItemWire lodItem = (LODItemWire)lod.LODItems[iLODItem];
            NumVertices = (int)(lodItem.NumVertices * (NumSections + 1) + 2 * lodItem.VerticalNumVertices * NumSections);
            NumIndices = (short)(lodItem.NumSegments * NumSections * 6 + lodItem.VerticalNumSegments * NumSections * 6);
            // (Cells x 2 triangles/cell x 3 indices/triangle)

            // Allocate memory for vertices and indices
            VertexList = new VertexPositionNormalTexture[NumVertices]; // numVertices is now aggregate
            TriangleListIndices = new short[NumIndices]; // as is NumIndices

            // Build the mesh for lod
            VertexIndex = 0;
            IndexIndex = 0;
            // Initial load of baseline cross section polylines for this LOD only:
            foreach (Polyline pl in lodItem.Polylines)
            {
                foreach (Vertex v in pl.Vertices)
                {
                    VertexList[VertexIndex].Position = v.Position;
                    VertexList[VertexIndex].Normal = v.Normal;
                    VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                    VertexIndex++;
                }
            }
            // Initial load of base cross section complete

            // Now generate and load subsequent cross sections
            OldRadius = -center;
            uint stride = VertexIndex;
            for (uint i = 0; i < NumSections; i++)
            {
                foreach (Polyline pl in lodItem.Polylines)
                {
                    uint plv = 0; // Polyline vertex index
                    foreach (Vertex v in pl.Vertices)
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

            if (lodItem.VerticalPolylines != null && lodItem.VerticalPolylines.Count > 0)
            {

                // Now generate and load subsequent cross sections
                OldRadius = -center;
                float coveredLength = SegmentLength;

                for (uint i = 0; i < NumSections; i++)
                {
                    stride = 0;
                    radius = Vector3.Transform(OldRadius, sectionRotation);
                    Vector3 p;
                    // Initial load of baseline cross section polylines for this LOD only:
                    if (i == 0)
                    {
                        foreach (Polyline pl in lodItem.VerticalPolylines)
                        {
                            foreach (Vertex v in pl.Vertices)
                            {
                                VertexList[VertexIndex].Position = v.Position;
                                VertexList[VertexIndex].Normal = v.Normal;
                                VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                                VertexIndex++;
                                stride++;
                            }
                        }
                    }
                    else
                    {
                        foreach (Polyline pl in lodItem.VerticalPolylines)
                        {
                            foreach (Vertex v in pl.Vertices)
                            {
                                if (DTrackData.IsCurved != 0)
                                {

                                    OldV = v.Position - center - OldRadius;
                                    // Rotate the point about local origin and reposition it (including elevation change)
                                    p = DDY + center + radius + v.Position;// +Vector3.Transform(OldV, sectionRotation);
                                    VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);

                                }
                                else
                                {
                                    VertexList[VertexIndex].Position = v.Position + new Vector3(0, 0, -coveredLength);
                                }

                                VertexList[VertexIndex].Normal = v.Normal;
                                VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                                VertexIndex++;
                                stride++;
                            }
                        }
                    }

                    foreach (Polyline pl in lodItem.VerticalPolylines)
                    {
                        uint plv = 0; // Polyline vertex index
                        foreach (Vertex v in pl.Vertices)
                        {
                            LinearVerticalGen(stride, pl); // Generation call

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

                    if (i != 0)
                    {
                        OldRadius = radius; // Get ready for next segment
                        coveredLength += SegmentLength;
                    }
                } // end for i
            }

            // Create and populate a new ShapePrimitive
            var indexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), NumIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(TriangleListIndices);
            return new ShapePrimitive(lodItem.LODMaterial, new SharedShape.VertexBufferSet(VertexList, viewer.GraphicsDevice), indexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);
        } // end BuildMesh

        /// <summary>
        /// Initializes member variables for straight track sections.
        /// </summary>
        void LinearGen()
        {
            NumSections = 1;

            // Cute the lines to have vertical stuff if needed
            if (WireProfile.expectedSegmentLength > 1)
            {
                NumSections = (int)(DTrackData.param1 / WireProfile.expectedSegmentLength);
            }

            if (NumSections < 1) NumSections = 1;

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (meters)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change
        } // end LinearGen

        /// <summary>
        /// Initializes member variables for circular arc track sections.
        /// </summary>
        void CircArcGen()
        {
            float arcLength = Math.Abs(DTrackData.param2 * DTrackData.param1);
            // Define the number of track cross sections in addition to the base.
            // Assume one skewed straight section per degree of curvature
            // Define the number of track cross sections in addition to the base.
            if (WireProfile.expectedSegmentLength > 1)
            {
                if (arcLength > 2 * WireProfile.expectedSegmentLength)
                {
                    NumSections = (int)(arcLength / WireProfile.expectedSegmentLength);
                }
                else if (arcLength > WireProfile.expectedSegmentLength)
                {
                    NumSections = (int)(2 * arcLength / WireProfile.expectedSegmentLength);
                }
                else NumSections = (int)Math.Abs(MathHelper.ToDegrees(DTrackData.param1 / 4));
            }
            else NumSections = (int)Math.Abs(MathHelper.ToDegrees(DTrackData.param1 / 3));

            if (NumSections < 1) NumSections = 1; // Very small radius track - zero avoidance
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
        /// Generates vertices for a vertical section (straight track).
        /// </summary>
        /// <param name="stride">Index increment between section-to-section vertices.</param>
        /// <param name="pl"></param>
        void LinearVerticalGen(uint stride, Polyline pl)
        {
            Vector3 displacement = new Vector3(0, -1.0f, 0) + DDY;
            float wrapLength = displacement.Length();
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            Vector3 p = VertexList[VertexIndex - stride].Position + displacement;
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement;

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

    }
}

//code from previous implementation

#if false
namespace ORTS
{
    /// <summary>
    /// Created by a Viewer
    /// </summary>
    public class WireDrawer
    {
        private Viewer3D Viewer;  // the viewer that we are tracking
        int viewerTileX, viewerTileZ;  // position of the viewer updated once per frame
        List<WirePrimitive> WirePrimitives = new List<WirePrimitive>();  // the currently loaded wire geometry
        //WireMaterial WireMaterial;

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background 
        /// processes haven't started yet.
        /// Executes in the RenderProcess thread.
        /// </summary>
        public WireDrawer(Viewer3D viewer)
        {
            Viewer = viewer;

            // load any static content, ie spritebatches, textures, etc

            // if all wire uses the same material,  create your material here.
            WireMaterial WireMaterial = new WireMaterial(viewer.RenderProcess);
        }

        /// <summary>
        /// Called 10 times per second when its safe to read volatile data
        /// from the simulator and viewer classes in preparation
        /// for the Load call.  Copy data to local storage for use 
        /// in the next load call.
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            viewerTileX = Viewer.Camera.TileX;
            viewerTileZ = Viewer.Camera.TileZ;

            // read any other volatile data that will be needed by Load
        }

        /// <summary>
        /// Called 10 times a second to load graphics content
        /// that comes and goes as the player and trains move.
        /// Called from background LoaderProcess Thread
        /// Do not access volatile data from the simulator 
        /// and viewer classes during the Load call ( see
        /// LoadPrep() )
        /// Executes in the LoaderProcess thread.
        /// Do not read volatile data managed by the UpdaterProcess
        /// </summary>
        public void Load(RenderProcess renderProcess)
        {
            // This is where the wire primitives list is updated and procedural geometry (perhaps line lists ) is created
            // Unload from WirePrimitives, any wire geometry that is out of viewing range from the 
            // Add to WirePrimitives, geometry for wire coming into viewing range
            // ie for each track segment (TDB) that is in range
            //        WirePrimitives.Add(new WirePrimitive(....));

        }


        /// <summary>
        /// Called every frame to update animations and load the frame contents .
        /// Note:  this doesn't actually draw on the screen surface, but 
        /// instead prepares a list of drawing primitives that will be rendered
        /// later in RenderFrame.Draw() by the RenderProcess thread.
        /// elapsedTime represents the the time since the last call to PrepareFrame
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // THREAD SAFETY WARNING - LoaderProcess could write to WirePrimitives list at any time
            // its probably not OK to iterate through this list because LoaderProcess could change its size at any time
            foreach ( WirePrimitive wirePrimitive in WirePrimitives)
            {
                // if ( wire primitive is in field of view of camera )
                //    frame.AddPrimitive( WireMaterial, wireprimitive, wireprimitive.xnaMatrix );
            }
        }
    } // SceneryDrawer


    /// <summary>
    /// This encapsulates any shaders, sprites, etc needed by the material.
    /// </summary>
	public class WireMaterial : Material
	{
		public WireMaterial(RenderProcess renderProcess)
			: base(null)
		{
			// create a shader if necessary
			// load any static textures etc
		}

		/// <summary>
		/// Called by RenderFrame.Draw() in the RenderProcess thread for each primitive
		/// that was loaded by PrepareFrame
		/// </summary>
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			foreach(var item in renderItems)
			    item.RenderPrimitive.Draw(graphicsDevice);
		}
	}

    public class WirePrimitive : RenderPrimitive
    {
        //Matrix xnaMatrix;
        // LineLists etc

        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread called from the Render method of the material class
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // do any draw calls on the graphics device 
            // ie graphicsDevice.Draw( .... )
        }
    }




}
#endif
