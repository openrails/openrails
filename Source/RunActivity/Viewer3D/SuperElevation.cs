// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

/* OVERHEAD SuperElevation
 * 
 * Overhead SuperElevation is generated procedurally from data in the track database.
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
    public class SuperElevation
    {
        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static int DecomposeStaticSuperElevation(Viewer3D viewer, List<DynatrackDrawer> dTrackList, TrackObj dTrackObj,
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
                    dTrackList.Add(new SuperElevationDrawer(viewer, root, nextRoot, radius, length));
                    localV = localProjectedV; // Next subsection
                }
            }
            return 1;
        } // end DecomposeStaticSuperElevation

        public static bool UseSuperElevation(Viewer3D viewer, List<DynatrackDrawer> dTrackList, TrackObj dTrackObj,
            WorldPosition worldMatrixInput)
        {

            try
            {
                if (viewer.Simulator.TSectionDat.TrackShapes.Get(dTrackObj.SectionIdx).RoadShape == true) return false;
            }
            catch (Exception)
            {
                return false;
            }
            SectionIdx[] SectionIdxs = viewer.Simulator.TSectionDat.TrackShapes.Get(dTrackObj.SectionIdx).SectionIdxs;

            bool withCurves = false;
            foreach (SectionIdx id in SectionIdxs)
            {
                uint[] sections = id.TrackSections;

                for (int i = 0; i < sections.Length; i++)
                {
                    float length, radius;
                    uint sid = id.TrackSections[i];
                    TrackSection section = viewer.Simulator.TSectionDat.TrackSections[sid];

                    if (section.SectionCurve == null)
                    {
                    }
                    else
                    {
                        length = section.SectionCurve.Angle * 3.14f / 180;
                        radius = section.SectionCurve.Radius; // meters
                        if (Math.Abs(length * radius) < Program.Simulator.SuperElevationMinLen) return false; //smaller than 100 meters, will not do superelevation
                        withCurves = true;
                    }
                }
            }
            return withCurves; //without curve then do not draw superelevations
        } // end UseSuperElevation

        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section converted from dynamic tracks
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeConvertedDynamicSuperElevation(Viewer3D viewer, List<DynatrackDrawer> dTrackList, TrackObj dTrackObj,
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
                dTrackList.Add(new SuperElevationDrawer(viewer, root, nextRoot, radius, length));
                localV = localProjectedV; // Next subsection
            }
        } // end DecomposeStaticSuperElevation
        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeDynamicSuperElevation(Viewer3D viewer, List<DynatrackDrawer> dTrackList, DyntrackObj dTrackObj,
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
                dTrackList.Add(new SuperElevationDrawer(viewer, root, nextRoot, radius, length));
                localV = localProjectedV; // Next subsection
            }
        } // end DecomposeDynamicSuperElevation

        public static bool UseSuperElevationDyn(Viewer3D viewer, List<DynatrackDrawer> dTrackList, DyntrackObj dTrackObj,
    WorldPosition worldMatrixInput)
        {
            bool withCurves = false;
            for (int iTkSection = 0; iTkSection < dTrackObj.trackSections.Count; iTkSection++)
            {
                float length = 0, radius = -1;

                length = dTrackObj.trackSections[iTkSection].param1; // meters if straight; radians if curved
                if (length == 0.0) continue; // Consider zero-length subsections vacuous

                // Create new DT object copy; has only one meaningful subsection
                DyntrackObj subsection = new DyntrackObj(dTrackObj, iTkSection);

                // Straight or curved subsection?
                if (subsection.trackSections[0].isCurved == 0) // Straight section
                {   // Heading stays the same; translation changes in the direction oriented
                }
                else // Curved section
                {   // Both heading and translation change 
                    // nextRoot is found by moving from Point-of-Curve (PC) to
                    // center (O)to Point-of-Tangent (PT).
                    radius = subsection.trackSections[0].param2; // meters
                    if (Math.Abs(radius * length) < Program.Simulator.SuperElevationMinLen) return false;
                    withCurves = true;
                }

            }
            return withCurves; //if no curve, will not draw using super elevation
        } // end UseSuperElevationDyn

    } // end class SuperElevations

    public class SuperElevationDrawer : DynatrackDrawer
    {

        public SuperElevationDrawer(Viewer3D viewer, WorldPosition position, WorldPosition endPosition, float radius, float angle)
            : base(viewer, position, endPosition)
        {
            // Instantiate classes
            dtrackMesh = new SuperElevationMesh(viewer.RenderProcess, position, endPosition, radius, angle);
        } // end DynatrackDrawer constructor


    } // end SuperElevationDrawer

    public class SuperElevationMesh : DynatrackMesh
    {
        public SuperElevationMesh(RenderProcess renderProcess, WorldPosition worldPosition,
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
            XNAEnd = endPosition.XNAMatrix.Translation;

            if (renderProcess.Viewer.Simulator.TRP == null)
            {
                // First to need a track profile creates it
                Trace.Write(" TRP");
                // Creates profile and loads materials into SceneryMaterials
                TRPFile.CreateTrackProfile(renderProcess, renderProcess.Viewer.Simulator.RoutePath, out renderProcess.Viewer.Simulator.TRP);
            }
            TrProfile = renderProcess.Viewer.Simulator.TRP.TrackProfile;

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


        } // end SuperElevationMesh constructor


        int offSet = 0;
        int elevationLevel = 0;
        /// <summary>
        /// Builds a SuperElevation LOD to SuperElevationProfile specifications as one vertex buffer and one index buffer.
        /// The order in which the buffers are built reflects the nesting in the TrProfile.  The nesting order is:
        /// (Polylines (Vertices)).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="worldPosition">WorldPosition.</param>
        /// <param name="iLOD">Index of LOD mesh to be generated from profile.</param>
        /// <param name="iLODItem">Index of LOD mesh to be generated from profile.</param>
        public new ShapePrimitive BuildMesh(Viewer3D viewer, WorldPosition worldPosition, int iLOD, int iLODItem)
        {
            elevationLevel = viewer.Simulator.UseSuperElevation;
            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0) LinearGen();
            else CircArcGen();

            // Count vertices and indices
            LOD lod = (LOD)TrProfile.LODs[iLOD];
            LODItem lodItem = (LODItem)lod.LODItems[iLODItem];
            NumVertices = (int)(lodItem.NumVertices * (NumSections + 1));
            NumIndices = (short)(lodItem.NumSegments * NumSections * 6);
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
            offSet = 0;
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
                offSet ++;
            } // end for i

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
            NumSections = (int)(Math.Abs(MathHelper.ToDegrees(DTrackData.param1)) / TrProfile.ChordSpan);
            if (NumSections == 0) NumSections++; // Very small radius track - zero avoidance

            // Use pitch control methods
            switch (TrProfile.PitchControl)
            {
                case TrProfile.PitchControls.None:
                    break; // Good enough
                case TrProfile.PitchControls.ChordLength:
                    // Calculate chord length for NumSections
                    float l = 2.0f * DTrackData.param2 * (float)Math.Sin(0.5f * Math.Abs(DTrackData.param1) / NumSections);
                    if (l > TrProfile.PitchControlScalar)
                    {
                        // Number of sections determined by chord length of PitchControlScalar meters
                        float chordAngle = 2.0f * (float)Math.Asin(0.5f * TrProfile.PitchControlScalar / DTrackData.param2);
                        NumSections = (int)Math.Abs((DTrackData.param1 / chordAngle));
                    }
                    break;
                case TrProfile.PitchControls.ChordDisplacement:
                    // Calculate chord displacement for NumSections
                    float d = DTrackData.param2 * (float)(1.0f - Math.Cos(0.5f * Math.Abs(DTrackData.param1) / NumSections));
                    if (d > TrProfile.PitchControlScalar)
                    {
                        // Number of sections determined by chord displacement of PitchControlScalar meters
                        float chordAngle = 2.0f * (float)Math.Acos(1.0f - TrProfile.PitchControlScalar / DTrackData.param2);
                        NumSections = (int)Math.Abs((DTrackData.param1 / chordAngle));
                    }
                    break;
            }

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
        /// <param name="pl">Polyline.</param>
        public new void LinearGen(uint stride, Polyline pl)
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
        /// <param name="pl">Polyline.</param>
        public new void CircArcGen(uint stride, Polyline pl)
        {
            // Get the previous vertex about the local coordinate system
            OldV = VertexList[VertexIndex - stride].Position - center - OldRadius;
            // Rotate the old radius vector to become the new radius vector
            radius = Vector3.Transform(OldRadius, sectionRotation);
            float wrapLength = (radius - OldRadius).Length(); // Wrap length is centerline chord
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;
            float elevated = 0.0f;
            elevated =  elevationLevel*0.1f /NumSections; //amount of rotation per segment, vs. previous segment

            //first half, will rotate more, second half, will rotate back
            if (DTrackData.param1 > 0) elevated *= -1;
            if (NumSections % 2 == 0)
            {
                if (offSet == NumSections / 2) elevated = 0;
                else if (offSet > NumSections / 2) elevated *= -1;
            }
            else
            {
                if (offSet == NumSections / 2) elevated = 0;
                else if (offSet > NumSections / 2) elevated *= -1;
            }
            // Rotate the point about local origin and reposition it (including elevation change)
            Vector3 p;
            if (NumSections > 1) p = DDY + center + radius + Vector3.Transform(OldV, Matrix.CreateRotationZ(elevated) * sectionRotation);
            else p = DDY + center + radius + Vector3.Transform(OldV, sectionRotation);
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement;

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

    }
}
