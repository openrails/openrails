// COPYRIGHT 2013, 2014, 2015, 2016 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    public class SuperElevationManager
    {
        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="trackList">DynamicTrackViewer list.</param>
        /// <param name="trackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        /// <param name="TileX">TileX coordinates.</param>
        /// <param name="TileZ">TileZ coordinates.</param>
        /// <param name="shapeFilePath">Path to the shape file.</param>
        public static bool DecomposeStaticSuperElevation(Viewer viewer, List<DynamicTrackViewer> trackList, TrackObj trackObj, WorldPosition worldMatrixInput, int TileX, int TileZ, string shapeFilePath)
        {
            TrackShape shape = null;
            try
            {
                shape = viewer.Simulator.TSectionDat.TrackShapes.Get(trackObj.SectionIdx);

                if (shape.RoadShape == true)
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
            SectionIdx[] SectionIdxs = shape.SectionIdxs;

            int count = -1;
            int drawn = 0;
            bool isTunnel = shape.TunnelShape;

            List<TrVectorSection> sectionsinShape = new List<TrVectorSection>();
            //List<DynamicTrackViewer> tmpTrackList = new List<DynamicTrackViewer>();
            foreach (SectionIdx id in SectionIdxs)
            {
                uint[] sections = id.TrackSections;

                for (int i = 0; i < sections.Length; i++)
                {
                    count++;
                    uint sid = id.TrackSections[i];
                    TrackSection section = viewer.Simulator.TSectionDat.TrackSections.Get(sid);

                    if (section.SectionCurve == null)
                    {
                        continue;
                        //with strait track, will remove all related sections later
                    }
                    TrVectorSection tmp = null;
                    if (section.SectionCurve != null)
                        tmp = FindSectionValue(shape, viewer.Simulator, section, TileX, TileZ, trackObj.UID);

                    if (tmp == null) //cannot find the track for super elevation, will return 0;
                    {
                        continue;
                    }
                    sectionsinShape.Add(tmp);

                    // Determine the track profile to use for this section
                    // It's possible that the tsection ID used by the track section points to the wrong shape
                    // Instead, send the shape file path to ensure the correct shape is used in calculations
                    DynamicTrackViewer.GetBestTrackProfile(viewer, tmp, shapeFilePath);

                    drawn++;
                }
            }

            if (drawn <= count || isTunnel) // tunnel or not every section is in SE, will remove all sections in the shape out
            {
                if (sectionsinShape.Count > 0)
                    RemoveTracks(viewer.Simulator, sectionsinShape);
                return false;
            }
            return true;
        }

        //remove sections from future consideration
        static void RemoveTracks(Simulator simulator, List<TrVectorSection> sectionsinShape)
        {
            foreach (var tmpSec in sectionsinShape)
            {
                List<TrVectorSection> curve = null;
                var pos = -1;
                foreach (var c in simulator.SuperElevation.Curves)
                {
                    if (c.Contains(tmpSec))
                    {
                        curve = c;
                        break;
                    }
                } // find which curve has the section
                if (curve != null)
                {
                    // Disable visual superelevation on affected sections to prevent graphical issues
                    pos = curve.IndexOf(tmpSec);
                    if (pos > 0)
                        curve[pos - 1].VisElevTable.Y[curve[pos - 1].VisElevTable.GetSize() - 1] = 0.0f;
                    if (pos < curve.Count - 1)
                        curve[pos + 1].VisElevTable.Y[0] = 0.0f;

                    curve[pos].VisElevTable.ScaleY(0.0f);

                    RemoveSectionsFromMap(simulator, tmpSec); // remove all sections in the curve from future consideration
                    curve.Remove(tmpSec);
                }
            }

        }

        //no use anymore
        public static int DecomposeStaticSuperElevationOneSection(Viewer viewer, List<DynamicTrackViewer> dTrackList, int TileX, int TileZ, TrVectorSection ts)
        {
            if (ts == null)
                return 0;

            var tss = viewer.Simulator.TSectionDat.TrackSections.Get(ts.SectionIndex);

            if (tss == null || tss.SectionCurve == null)
                return 0;

            WorldPosition root = new WorldPosition();
            root.TileX = ts.TileX;
            root.TileZ = ts.TileZ;
            root.XNAMatrix = Matrix.CreateFromYawPitchRoll(-ts.AY, -ts.AX, ts.AZ);
            root.XNAMatrix.Translation = new Vector3(ts.X, ts.Y, -ts.Z);

            float angleRad = MathHelper.ToRadians(tss.SectionCurve.Angle);
            var sign = -Math.Sign(angleRad);
            var vectorCurveStartToCenter = Vector3.Left * tss.SectionCurve.Radius * sign;
            var curveRotation = Matrix.CreateRotationY(-angleRad);
            var displacement = Traveller.MSTSInterpolateAlongCurve(Vector3.Zero, vectorCurveStartToCenter, curveRotation, root.XNAMatrix, out Vector3 dummy);

            WorldPosition nextRoot = new WorldPosition(root);
            nextRoot.XNAMatrix.Translation = displacement;

            int trpIndex = ts.TRPIndex < 0 ? DynamicTrackViewer.GetBestTrackProfile(viewer, ts) : ts.TRPIndex;

            TrProfile trProfile = viewer.TRPs[trpIndex].TrackProfile;

            // If track profile is set to have no superelevation, set visual superelevation to 0
            if (trProfile.ElevationType == TrProfile.SuperelevationMethod.None)
                ts.VisElevTable.ScaleY(0.0f);

            // Determine the centerline offset for superelevation roll
            // 0 = centered, positive = centerline moves to inside of curve, negative = centerline moves to outside of curve
            switch (trProfile.ElevationType)
            {
                case TrProfile.SuperelevationMethod.Outside: // Only outside rail should elevate
                    ts.ElevOffsetM = trProfile.TrackGaugeM / 2.0f;
                    break;
                case TrProfile.SuperelevationMethod.Inside: // Only inside rail should elevate
                    ts.ElevOffsetM = -trProfile.TrackGaugeM / 2.0f;
                    break;
                case TrProfile.SuperelevationMethod.Both: // Both rails should elevate
                default:
                    ts.ElevOffsetM = 0.0f;
                    break;
            }

            dir = 1f;

            dTrackList.Add(new SuperElevationViewer(viewer, root, nextRoot, tss.SectionCurve.Radius, angleRad, ts.VisElevTable, dir, ts.ElevOffsetM, trProfile));
            return 1;
        }

        public static int DecomposeStaticSuperElevation(Viewer viewer, List<DynamicTrackViewer> dTrackList, int TileX, int TileZ)
        {
            var key = (int)(Math.Abs(TileX) + Math.Abs(TileZ));
            if (!viewer.Simulator.SuperElevation.Sections.ContainsKey(key))
                return 0;//cannot find sections associated with this tile
            var sections = viewer.Simulator.SuperElevation.Sections[key];
            if (sections == null)
                return 0;

            foreach (var ts in sections)
            {
                var tss = viewer.Simulator.TSectionDat.TrackSections.Get(ts.SectionIndex);
                if (tss == null || tss.SectionCurve == null || ts.WFNameX != TileX || ts.WFNameZ != TileZ)
                    continue;
                WorldPosition root = new WorldPosition();
                root.TileX = ts.TileX;
                root.TileZ = ts.TileZ;
                root.XNAMatrix = Matrix.CreateFromYawPitchRoll(-ts.AY, -ts.AX, ts.AZ);
                root.XNAMatrix.Translation = new Vector3(ts.X, ts.Y, -ts.Z);

                float angleRad = MathHelper.ToRadians(tss.SectionCurve.Angle);
                var sign = -Math.Sign(angleRad);
                var vectorCurveStartToCenter = Vector3.Left * tss.SectionCurve.Radius * sign;
                var curveRotation = Matrix.CreateRotationY(-angleRad);
                var displacement = Traveller.MSTSInterpolateAlongCurve(Vector3.Zero, vectorCurveStartToCenter, curveRotation, root.XNAMatrix, out Vector3 dummy);

                WorldPosition nextRoot = new WorldPosition(root);
                nextRoot.XNAMatrix.Translation = displacement;

                int trpIndex = ts.TRPIndex < 0 ? DynamicTrackViewer.GetBestTrackProfile(viewer, ts) : ts.TRPIndex;

                TrProfile trProfile = viewer.TRPs[trpIndex].TrackProfile;

                // If track profile is set to have no superelevation, set visual superelevation to 0
                if (trProfile.ElevationType == TrProfile.SuperelevationMethod.None)
                    ts.VisElevTable.ScaleY(0.0f);

                // Determine the centerline offset for superelevation roll
                // 0 = centered, positive = centerline moves to inside of curve, negative = centerline moves to outside of curve
                switch (trProfile.ElevationType)
                {
                    case TrProfile.SuperelevationMethod.Outside: // Only outside rail should elevate
                        ts.ElevOffsetM = trProfile.TrackGaugeM / 2.0f;
                        break;
                    case TrProfile.SuperelevationMethod.Inside: // Only inside rail should elevate
                        ts.ElevOffsetM = -trProfile.TrackGaugeM / 2.0f;
                        break;
                    case TrProfile.SuperelevationMethod.Both: // Both rails should elevate
                    default:
                        ts.ElevOffsetM = 0.0f;
                        break;
                }

                dir = 1f;

                dTrackList.Add(new SuperElevationViewer(viewer, root, nextRoot, tss.SectionCurve.Radius, angleRad, ts.VisElevTable, dir, ts.ElevOffsetM, trProfile));
            }
            return 1;
        }

        static float dir;
        //a function to find the elevation of a section ,by searching the TDB database
        public static TrVectorSection FindSectionValue(TrackShape shape, Simulator simulator, TrackSection section, int TileX, int TileZ, uint UID)
        {
            if (section.SectionCurve == null) return null;
            dir = 1f;
            var key = (int)(Math.Abs(TileX) + Math.Abs(TileZ));
            if (!simulator.SuperElevation.Sections.ContainsKey(key)) return null;//we do not have the maps of sections on the given tile, will not bother to search
            var tileSections = simulator.SuperElevation.Sections[key];
            foreach (var s in tileSections)
            {
                if (s.WFNameX == TileX && s.WFNameZ == TileZ && s.WorldFileUiD == UID && section.SectionIndex == s.SectionIndex)
                {
                    return s;
                }
            }

            //not found, will do again to find reversed
            foreach (var s in tileSections)
            {
                var sec = simulator.TSectionDat.TrackSections.Get(s.SectionIndex);
                if (s.WFNameX == TileX && s.WFNameZ == TileZ && s.WorldFileUiD == UID && section.SectionCurve.Radius == sec.SectionCurve.Radius
                    && section.SectionCurve.Angle == -sec.SectionCurve.Angle)
                {
                    return s;
                }
            }
            return null;
        }

        //remove a section from the tile-section map
        public static void RemoveSectionsFromMap(Simulator simulator, TrVectorSection section)
        {
            var key = (int)(Math.Abs(section.WFNameX) + Math.Abs(section.WFNameZ));
            if (simulator.SuperElevation.Sections.ContainsKey(key)) simulator.SuperElevation.Sections[key].Remove(section);
        }

        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section converted from dynamic tracks
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynamicTrackViewer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeConvertedDynamicSuperElevation(Viewer viewer, List<DynamicTrackViewer> dTrackList, TrackObj dTrackObj,
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

            int count = -1;
            for (int i = 0; i < sections.Length; i++)
            {
                count++;
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

                    displacement = Traveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                            worldMatrix.XNAMatrix, out localProjectedV);

                    heading = Vector3.Transform(heading, rot); // Heading change
                    nextRoot.XNAMatrix = trackRot * rot * nextRoot.XNAMatrix; // Store heading change

                }
                nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;
                root.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                nextRoot.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);

                // TODO: Find a way to determine superelevation on dynamic track sections?
                // Might be feasible to use the same method as MarkSections does
                // For now, generate with 0 superelevation interpolator.
                Interpolator elevAngles = new Interpolator(new float[] { 0, 1 }, new float[] { 0, 0 });

                dir = 1f;
                //if (section.SectionCurve != null) FindSectionValue(shape, root, nextRoot, viewer.Simulator, section, TileX, TileZ, dTrackObj.UID);

                //nextRoot.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                dTrackList.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, elevAngles, dir));
                localV = localProjectedV; // Next subsection
            }
        }

        /// <summary>
        /// Decompose and add a SuperElevation on top of MSTS track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynamicTrackViewer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeDynamicSuperElevation(Viewer viewer, List<DynamicTrackViewer> dTrackList, DyntrackObj dTrackObj,
            WorldPosition worldMatrixInput)
        {
            // DYNAMIC TRACK
            // =============
            // Objectives:
            // 1-Decompose multi-subsection DT into individual sections.  
            // 2-Create updated transformation objects (instances of WorldPosition) to reflect 
            //   root of next subsection.
            // 3-Distribute elevation change for total section through subsections. (ABANDONED)
            // 4-For each meaningful subsection of dtrack, build a separate SuperElevationPrimitive.
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

            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Save root position
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

            // Iterate through all subsections
            int count = -1;
            for (int iTkSection = 0; iTkSection < dTrackObj.trackSections.Count; iTkSection++)
            {
                count++;
                float length = 0, radius = -1;

                length = dTrackObj.trackSections[iTkSection].param1; // meters if straight; radians if curved
                if (length == 0.0 || dTrackObj.trackSections[iTkSection].UiD == UInt32.MaxValue)
                    continue; // Consider zero-length subsections vacuous

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
                }
                else // Curved section
                {   // Both heading and translation change 
                    // nextRoot is found by moving from Point-of-Curve (PC) to
                    // center (O)to Point-of-Tangent (PT).
                    radius = subsection.trackSections[0].param2; // meters
                    Vector3 left = radius * Vector3.Cross(Vector3.Up, heading) * Math.Sign(-subsection.trackSections[0].param1); // Vector from PC to O
                    Matrix rot = Matrix.CreateRotationY(-length); // Heading change (rotation about O)
                    // Shared method returns displacement from present world position and, by reference,
                    // local position in x-z plane of end of this section
                    displacement = Traveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                            worldMatrix.XNAMatrix, out localProjectedV);

                    heading = Vector3.Transform(heading, rot); // Heading change
                    nextRoot.XNAMatrix = rot * nextRoot.XNAMatrix; // Store heading change
                }

                // Update nextRoot with new translation component
                nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;

                // TODO: Find a way to determine superelevation on dynamic track sections?
                // Might be feasible to use the same method as MarkSections does
                // For now, generate with 0 superelevation interpolator.
                Interpolator elevAngles = new Interpolator(new float[] { 0, 1 }, new float[] { 0, 0 });

                //                if (section.SectionCurve != null) FindSectionValue(shape, root, nextRoot, viewer.Simulator, section, TileX, TileZ, dTrackObj.UID);

                //nextRoot.XNAMatrix.Translation += Vector3.Transform(trackLoc, worldMatrix.XNAMatrix);
                dTrackList.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, elevAngles, dir));

                localV = localProjectedV; // Next subsection
            }
        }

        public static bool UseSuperElevationDyn(Viewer viewer, List<DynamicTrackViewer> dTrackList, DyntrackObj dTrackObj,
    WorldPosition worldMatrixInput)
        {
            bool withCurves = false;
            for (int iTkSection = 0; iTkSection < dTrackObj.trackSections.Count; iTkSection++)
            {
                float length = 0;

                length = dTrackObj.trackSections[iTkSection].param1; // meters if straight; radians if curved
                if (length == 0.0 || dTrackObj.trackSections[iTkSection].UiD == UInt32.MaxValue) continue; // Consider zero-length subsections vacuous

                // Create new DT object copy; has only one meaningful subsection
                DyntrackObj subsection = new DyntrackObj(dTrackObj, iTkSection);

                // Straight or curved subsection?
                if (subsection.trackSections[0].isCurved == 0) // Straight section
                {   // Heading stays the same; translation changes in the direction oriented
                }
                else // Curved section
                {   // Both heading and translation change 
                    //if (Math.Abs(radius * length) < Program.Simulator.SuperElevationMinLen) return false;
                    withCurves = true;
                }

            }
            return withCurves; //if no curve, will not draw using super elevation
        }
    }

    public class SuperElevationViewer : DynamicTrackViewer
    {
        public SuperElevationViewer(Viewer viewer, WorldPosition position, WorldPosition endPosition, float radius, float angle,
            Interpolator elevs, float dir, float rollOffset = 0, TrProfile trProfile = null)
            : base(viewer, position, endPosition)
        {
            // Instantiate classes
            Primitive = new SuperElevationPrimitive(viewer, position, endPosition, radius, angle, elevs, dir, rollOffset, trProfile);
        }
    }

    public class SuperElevationPrimitive : DynamicTrackPrimitive
    {
        Interpolator ElevAngles;
        public SuperElevationPrimitive(Viewer viewer, WorldPosition worldPosition,
            WorldPosition endPosition, float radius, float angle, Interpolator elevs, float dir, float rollOffset = 0, TrProfile trProfile = null)
            : base()
        {
            ElevAngles = elevs;

            // Use default (0th) track profile if none was given
            TrProfile = trProfile ?? viewer.TRPs[0].TrackProfile;

            // SuperElevationPrimitive is responsible for creating a mesh for a section with a single subsection.
            // It also must update worldPosition to reflect the end of this subsection, subsequently to
            // serve as the beginning of the next subsection.

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
            DTrackData.deltaY = 0; // No change in height-we construct the track level and then rotate the entire model into position later
            // TODO: Change track generation to actually respect the change in height
            //DTrackData.deltaY = endPosition.Location.Y - worldPosition.Location.Y;

            if (DTrackData.IsCurved == 0)
                ObjectRadius = 0.5f * DTrackData.param1; // half-length
            else
                ObjectRadius = DTrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(DTrackData.param1)); // half chord length

            // Correct offset for direction of curvature
            RollOffset = new Vector3(rollOffset * Math.Sign(DTrackData.param1), 0, 0);

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
                    ShapePrimitives[primIndex] = BuildPrimitive(viewer, worldPosition, iLOD, iLODItem);
                    primIndex++;
                }
                lod.PrimIndexStop = primIndex; // 1 above last index for this LOD
            }
        }

        int Offset = 0;

        float Elevated;

        // Offset from centerline for superelevation rotation
        Vector3 RollOffset;
        /// <summary>
        /// Builds a SuperElevation LOD to SuperElevationProfile specifications as one vertex buffer and one index buffer.
        /// The order in which the buffers are built reflects the nesting in the TrProfile.  The nesting order is:
        /// (Polylines (Vertices)).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="worldPosition">WorldPosition.</param>
        /// <param name="iLOD">Index of LOD mesh to be generated from profile.</param>
        /// <param name="iLODItem">Index of LOD mesh to be generated from profile.</param>
        public new ShapePrimitive BuildPrimitive(Viewer viewer, WorldPosition worldPosition, int iLOD, int iLODItem)
        {
            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0)
                LinearGen();
            else
                CircArcGen();

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

            PrevRotation = 0f;

            Matrix PreRotation = Matrix.Identity;
            Elevated = ElevAngles[0];
            if (Elevated > 0.0f) // Section starts in a curve; first cross section needs to be rotated
            {
                PreRotation = Matrix.CreateRotationZ(-Elevated * Math.Sign(DTrackData.param1));
                PrevRotation = Elevated;
            }

            Vector3 tmp;
            // Initial load of baseline cross section polylines for this LOD only:
            foreach (Polyline pl in lodItem.Polylines)
            {
                foreach (Vertex v in pl.Vertices)
                {
                    tmp = new Vector3(v.Position.X, v.Position.Y, v.Position.Z);

                    if (Elevated > 0.0f)
                    {
                        tmp -= RollOffset;
                        tmp = Vector3.Transform(tmp, PreRotation);
                        tmp += RollOffset;
                    }
                    VertexList[VertexIndex].Position = tmp;
                    VertexList[VertexIndex].Normal = v.Normal;
                    VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                    VertexIndex++;
                }
            }
            // Initial load of base cross section complete

            // Now generate and load subsequent cross sections
            OldRadius = -center;
            uint stride = VertexIndex;
            Offset = 0;

            for (uint i = 0; i < NumSections; i++)
            {
                CurrentRotation = DetermineRotation();
                Elevated = CurrentRotation - PrevRotation;
                PrevRotation = CurrentRotation;
                if (DTrackData.param1 > 0)
                    Elevated *= -1;

                foreach (Polyline pl in lodItem.Polylines)
                {
                    uint plv = 0; // Polyline vertex index
                    foreach (Vertex v in pl.Vertices)
                    {
                        if (DTrackData.IsCurved == 0)
                            LinearGen(stride, pl); // Generation call
                        else
                            CircArcGen(stride, pl);

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
                    }
                }
                OldRadius = radius; // Get ready for next segment
                Offset++;
            }

            // Create and populate a new ShapePrimitive
            var indexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), NumIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(TriangleListIndices);
            return new ShapePrimitive(lodItem.LODMaterial, new SharedShape.VertexBufferSet(VertexList, viewer.GraphicsDevice), indexBuffer, NumIndices / 3, new[] { -1 }, 0);
        }

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
        }

        /// <summary>
        /// Initializes member variables for circular arc track sections.
        /// </summary>
        void CircArcGen()
        {
            // Define the number of track cross sections in addition to the base.
            NumSections = (int)(Math.Abs(MathHelper.ToDegrees(DTrackData.param1)) / TrProfile.ChordSpan);
            if (NumSections == 0)
                NumSections = 2; // Very small radius track - zero avoidance

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
            if (NumSections % 2 == 1)
                NumSections++; // make it even number

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (radians)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change

            // The approach here is to replicate the previous cross section, 
            // rotated into its position on the curve and vertically displaced if on grade.
            // The local center for the curve lies to the left or right of the local origin and ON THE BASE PLANE
            center = DTrackData.param2 * (DTrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
            sectionRotation = Matrix.CreateRotationY(-SegmentLength); // Rotation per iteration (constant)
        }

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
            OldV = VertexList[VertexIndex - stride].Position - center - OldRadius - RollOffset - DDY * Offset;
            // Rotate the old radius vector to become the new radius vector
            radius = Vector3.Transform(OldRadius, sectionRotation);
            // Rotate the roll offset
            Vector3 newOffset = Vector3.Transform(RollOffset, sectionRotation);
            float wrapLength = (radius - OldRadius).Length(); // Wrap length is centerline chord
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            Vector3 p = DDY * (Offset + 1) + newOffset + radius + center + Vector3.Transform(OldV, Matrix.CreateRotationZ(Elevated) * sectionRotation);

            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement;

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

        public float PrevRotation;
        public float CurrentRotation;
        public float DetermineRotation()
        {
            float to = (Offset + 1f) / NumSections;

            return ElevAngles[to];
        }
    }
}
