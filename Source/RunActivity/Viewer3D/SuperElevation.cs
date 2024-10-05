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
using System.Linq;

namespace Orts.Viewer3D
{
    public class SuperElevationManager
    {
        /// <summary>
        /// Generate SuperElevationViewers for a static track object
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="trackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        /// <param name="dTrackList">DynamicTrackViewer list.</param>
        /// <param name="shapeFilePath">Path to the shape file.</param>
        /// <returns>Bool value indicating true if the given track object
        /// should be rendered as dynamic track rather than a static shape.</returns>
        public static bool DecomposeStaticSuperElevation(Viewer viewer, TrackObj trackObj, WorldPosition worldMatrixInput, List<DynamicTrackViewer> dTrackList, string shapeFilePath)
        {
            // STATIC TRACK
            // =============
            // Objectives:
            // 1-Decompose multi-subsection track shape into individual sections.  
            // 2-Create updated transformation objects (instances of WorldPosition) to reflect 
            //   root of next subsection.
            // 3-Identify which subsections should and shouldn't have superelevation.
            // 4-Generate viewers for all subsections, making sure to respect presence or lack
            //   of superelevation.
            // 5-If no sections have superelevation, return false to inform WorldFile that
            //   this section should be generated the traditional way.
            //
            // Method: Iterate through each subsection, updating WorldPosition for the root of
            // each subsection.  The rotation component changes only in heading.  The translation 
            // component steps along the path to reflect the root of each subsection.

            TrackShape shape;

            try
            {
                shape = viewer.Simulator.TSectionDat.TrackShapes.Get(trackObj.SectionIdx);

                if (shape.RoadShape == true)
                    return false; // Roads don't use superelevation, no use in processing them.
            }
            catch (Exception)
            {
                return false; // Won't be able to render with superelevation
            }

            SectionIdx[] SectionIdxs = shape.SectionIdxs;

            // Can't render superelevation on tunnel shapes
            // Still need to run through processing to remove superelevation from track sections
            bool dontRender = shape.TunnelShape;
            // Sometimes junctions get caught here, physics superelevation should be removed for those
            bool removePhys = false;

            // Determine the track profile to use for this section based on the shape file
            int trpIndex = DynamicTrackViewer.GetBestTrackProfile(viewer, shapeFilePath);

            TrProfile trProfile = viewer.TRPs[trpIndex].TrackProfile;

            // Determine the centerline offset for superelevation roll
            // 0 = centered, positive = centerline moves to inside of curve, negative = centerline moves to outside of curve
            float rollOffset;

            switch (trProfile.ElevationType)
            {
                case TrProfile.SuperelevationMethod.Outside: // Only outside rail should elevate
                    rollOffset = trProfile.TrackGaugeM / 2.0f;
                    break;
                case TrProfile.SuperelevationMethod.Inside: // Only inside rail should elevate
                    rollOffset = -trProfile.TrackGaugeM / 2.0f;
                    break;
                case TrProfile.SuperelevationMethod.Both: // Both rails should elevate
                default:
                    rollOffset = 0.0f;
                    break;
            }

            // Right now it's not confirmed if superelevation viewers are actually needed, so they are added to a temporary list
            List<DynamicTrackViewer> tempViewers = new List<DynamicTrackViewer>();
            // Also keep track of which sections are superelevated
            List<TrVectorSection> superElevationSections = new List<TrVectorSection>();

            // Iterate through all subsections
            foreach (SectionIdx id in SectionIdxs)
            {
                // If section angle offset is not zero, that means we have a complicated track shape (eg: junction)
                // If any sections have identical starting conditions, that means we have a junction
                // These should not be rendered using superelevation
                if (!dontRender && id.A != 0.0f)
                    dontRender = true;
                if (!dontRender && !removePhys
                    && SectionIdxs.Any(idx => idx != id && idx.X == id.X && idx.Y == id.Y && idx.Z == id.Z))
                {
                    dontRender = true;
                    removePhys = true;
                }

                // The following vectors represent local positioning relative to root of original section:
                Vector3 offset = new Vector3((float)id.X, (float)id.Y, (float)id.Z); // Offset from section origin for this series of sections
                Vector3 localV = Vector3.Zero; // Local position of subsection (in x-z plane)
                Vector3 heading = Vector3.Forward; // Local heading (unit vector)

                WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Copy origin location

                worldMatrix.XNAMatrix.Translation = Vector3.Transform(offset, worldMatrix.XNAMatrix);

                WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
                Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Original position for entire section
                worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

                foreach (uint sid in id.TrackSections)
                {
                    TrackSection section = viewer.Simulator.TSectionDat.TrackSections.Get(sid);

                    // Set the start of this section to the end of the previous section
                    WorldPosition root = new WorldPosition(nextRoot);

                    nextRoot = CalculateNextRoot(section, worldMatrix, sectionOrigin, nextRoot, localV,
                        ref heading, out Vector3 localProjectedV, out float length, out float radius);

                    localV = localProjectedV; // Move position to next subsection

                    // To determine if this section needs superelevation, search for it in the global superelevation dictionary
                    TrVectorSection tmp = FindSuperElevationSection(viewer, section, trackObj.UID, root, nextRoot, out bool reversed);

                    if (tmp != null) // Section does have superelevation, prepare to generate it with superelevation
                    {
                        superElevationSections.Add(tmp);
                        tmp.TRPIndex = trpIndex;
                        tmp.ElevOffsetM = rollOffset;

                        if (!dontRender)
                        {
                            // If track profile is set to have no superelevation, set visual superelevation to 0
                            if (trProfile.ElevationType == TrProfile.SuperelevationMethod.None)
                                tmp.VisElevTable.ScaleY(0.0f);

                            // Processing done, prepare to generate section with superelevation
                            tempViewers.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, trProfile, tmp.VisElevTable, tmp.ElevOffsetM, reversed));
                        }
                    }
                    else if (!dontRender) // Section doesn't have superelevation, prepare to generate it without superelevation
                        tempViewers.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, trProfile));
                }
            }

            // If we shouldn't render superelevation for whatever reason,
            // remove sections from the superelevation dictionary and render with static shapes instead
            if (dontRender)
            {
                if (superElevationSections.Count > 0)
                    RemoveSuperElevation(viewer.Simulator, superElevationSections, removePhys);
                return false;
            }
            // We are rendering superelevation, add all superelevation viewers to the global list to be rendered
            else if (superElevationSections.Count > 0 && tempViewers.Count > 0)
                dTrackList.AddRange(tempViewers);

            // If no sections with superelevation were found, WorldFile should render with static shapes
            return superElevationSections.Count > 0;
        }

        /// <summary>
        /// Removes superelevation from sections associated with a particular junction TrackObj
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="trackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void ClearJunctionSuperElevation(Viewer viewer, TrackObj trackObj, WorldPosition worldMatrixInput)
        {
            TrackShape shape;
            try
            {
                shape = viewer.Simulator.TSectionDat.TrackShapes.Get(trackObj.SectionIdx);

                if (shape.RoadShape == true)
                    return;
            }
            catch (Exception)
            {
                return;
            }
            SectionIdx[] SectionIdxs = shape.SectionIdxs;

            List<TrVectorSection> sectionsInShape = new List<TrVectorSection>();

            foreach (SectionIdx id in SectionIdxs)
            {
                // The following vectors represent local positioning relative to root of original section:
                Vector3 offset = new Vector3((float)id.X, (float)id.Y, (float)id.Z); // Offset from section origin for this series of sections
                Vector3 localV = Vector3.Zero; // Local position of subsection (in x-z plane)
                Vector3 heading = Vector3.Forward; // Local heading (unit vector)

                WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Copy origin location

                worldMatrix.XNAMatrix.Translation = Vector3.Transform(offset, worldMatrix.XNAMatrix);

                WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
                Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Original position for entire section
                worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

                foreach (uint sid in id.TrackSections)
                {
                    TrackSection section = viewer.Simulator.TSectionDat.TrackSections.Get(sid);

                    // Set the start of this section to the end of the previous section
                    WorldPosition root = new WorldPosition(nextRoot);

                    nextRoot = CalculateNextRoot(section, worldMatrix, sectionOrigin, nextRoot, localV,
                        ref heading, out Vector3 localProjectedV, out float length, out float radius);

                    localV = localProjectedV; // Move position to next subsection

                    // To determine if this section needs superelevation, search for it in the global superelevation dictionary
                    TrVectorSection tmp = FindSuperElevationSection(viewer, section, trackObj.UID, root, nextRoot, out bool reversed);

                    if (tmp != null) // Section does have superelevation, add it to be removed
                        sectionsInShape.Add(tmp);
                }
            }

            if (sectionsInShape.Count > 0)
                RemoveSuperElevation(viewer.Simulator, sectionsInShape, true);
        }

        /// <summary>
        /// Returns the TrVectorSection in the SuperElevation dictionary associated with a particular TrackSection
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="section">Track section to match against.</param>
        /// <param name="UID">UID of the track object containing section.</param>
        /// <param name="startPos">WorldPosition for the beginning of the track section.</param>
        /// <param name="endPos">WorldPosition for the end of the track section.</param>
        /// <param name="reversed">Output bool indicating if the track section faces the opposite direction to the TrVectorSection.</param>
        /// <returns>Superelevated TrVectorSection which matches the given track section, or null if none found.</returns>
        public static TrVectorSection FindSuperElevationSection(Viewer viewer, TrackSection section, uint UID,
            WorldPosition startPos, WorldPosition endPos, out bool reversed)
        {
            reversed = false; // Is the track shape reversed relative to the vector section?

            // Duplicate world positions to avoid modifying originals, normalize to get correct tiles
            WorldPosition shapeStart = new WorldPosition(startPos);
            shapeStart.Normalize();
            WorldPosition shapeEnd = new WorldPosition(endPos);
            shapeEnd.Normalize();

            int key = (int)(Math.Abs(shapeStart.TileX) + Math.Abs(shapeStart.TileZ));
            if (!viewer.Simulator.SuperElevation.Sections.ContainsKey(key))
                return null; // No superelevation sections present on the given tile, skip searching
            var tileSections = viewer.Simulator.SuperElevation.Sections[key];

            // Search for section with the same section index
            foreach (var s in tileSections)
            {
                // Gradually get more specific with section matching to find corresponding section
                // Note: Both the WFNameX/Z and TileX/Z are checked to handle sections which cross a tile boundary
                if (((s.WFNameX == shapeStart.TileX && s.WFNameZ == shapeStart.TileZ)
                    || (s.TileX == shapeStart.TileX && s.TileZ == shapeStart.TileZ))
                    && s.WorldFileUiD == UID)
                {
                    // Vector section is in the same tile and has the same object UiD, check against track object
                    // If the track sections corresponding to each aren't the same, then skip to the next vector section
                    TrackSection elevSec = viewer.Simulator.TSectionDat.TrackSections.Get(s.SectionIndex);

                    if (elevSec.SectionCurve == null && section.SectionCurve == null && elevSec.SectionSize.Length == section.SectionSize.Length)
                    {
                        // Both sections are straight with the same size, compare direction
                        Vector3 sDir = Matrix.CreateFromYawPitchRoll(-s.AY, -s.AX, s.AZ).Forward;

                        if (Vector3.Dot(shapeStart.XNAMatrix.Forward, sDir) < 0.0f)
                            reversed = true; // Dot product is negative, sections are facing opposite directions
                        else
                            reversed = false;
                    }
                    else if (elevSec.SectionCurve != null && section.SectionCurve != null
                        && elevSec.SectionCurve.Radius == section.SectionCurve.Radius)
                    {
                        // Both sections are curves with the same radius
                        if (Math.Abs(elevSec.SectionCurve.Angle) == Math.Abs(section.SectionCurve.Angle))
                        {
                            if (Math.Sign(elevSec.SectionCurve.Angle) != Math.Sign(section.SectionCurve.Angle))
                                reversed = true; // Curves have same angle, but opposite direction
                            else
                                reversed = false;
                        }
                        else
                            continue; // Curves have different angles
                    }
                    else // Sections aren't equivalent for some other reason
                        continue;

                    // We now have a vector section with an equivalent track section,
                    // but it may not be in the same position, so check positions
                    Vector3 sPos = new Vector3(s.X, s.Y, s.Z);
                    // If the section is reversed, the vector section start position should align with the section end
                    if (reversed)
                    {
                        if (Vector3.DistanceSquared(sPos, shapeEnd.Location) < 0.25f)
                            return s;
                    }
                    else // If the section is forwards, the vector section start position should align with the section start
                    {
                        if (Vector3.DistanceSquared(sPos, shapeStart.Location) < 0.25f)
                            return s;
                    }
                }
            }
            // Couldn't find a matching section
            return null;
        }

        /// <summary>
        /// Removes visual superelevation (optionally physics superelevation as well) from track vector sections,
        /// also removing those sections from future consideration in superelevation calculations.
        /// </summary>
        /// <param name="simulator">Simulator reference.</param>
        /// <param name="sectionsinShape">List of TrVectorSections to remove superelevation from.</param>
        /// <param name="removePhys">Bool to indicate if physics superelevation should be removed as well.</param>
        static void RemoveSuperElevation(Simulator simulator, List<TrVectorSection> sectionsinShape, bool removePhys = false)
        {
            foreach (var tmpSec in sectionsinShape)
            {
                List<TrVectorSection> curve = null;
                int pos;
                // Find which curve has the section
                foreach (var c in simulator.SuperElevation.Curves)
                {
                    if (c.Contains(tmpSec))
                    {
                        curve = c;
                        break;
                    }
                }
                // If curve was found, clean up superelevation
                if (curve != null)
                {
                    // Disable visual (optionally physical) superelevation on affected sections to prevent graphical issues
                    pos = curve.IndexOf(tmpSec);
                    if (pos > 0) // Adjust superelevation of previous section as well
                    {
                        curve[pos - 1].VisElevTable.Y[curve[pos - 1].VisElevTable.GetSize() - 1] = 0.0f;
                        if (removePhys)
                            curve[pos - 1].PhysElevTable.Y[curve[pos - 1].PhysElevTable.GetSize() - 1] = 0.0f;
                    }
                    if (pos < curve.Count - 1) // Adjust superelevation of next section as well
                    {
                        curve[pos + 1].VisElevTable.Y[0] = 0.0f;
                        if (removePhys)
                            curve[pos + 1].PhysElevTable.Y[0] = 0.0f;
                    }

                    curve[pos].VisElevTable.ScaleY(0.0f);
                    if (removePhys)
                    {
                        curve[pos].PhysElevTable.ScaleY(0.0f);
                        curve[pos].NomElevM = 0.0f;
                    }

                    // Remove affected section from superelevation system
                    RemoveSectionsFromMap(simulator, tmpSec);
                    curve.Remove(tmpSec);
                }
            }

        }

        /// <summary>
        /// Removes a track vector section from the global list of sections with superelevation,
        /// preventing the section from being considered for superelevation in the future.
        /// </summary>
        /// <param name="simulator">Simulator reference.</param>
        /// <param name="section">The TrVectorSections to remove from the superelevation list.</param>
        public static void RemoveSectionsFromMap(Simulator simulator, TrVectorSection section)
        {
            // Need to consider both the WFName tile and the 'actual' tile
            // These may differ if a section crosses a tile boundary, in which case it should be removed from both tiles
            // NOTE: The WFName seems to indicate where the start of the section is, while the Tile indicates the end(?)
            int key = (int)(Math.Abs(section.TileX) + Math.Abs(section.TileZ));
            int wfKey = (int)(Math.Abs(section.WFNameX) + Math.Abs(section.WFNameZ));

            if (simulator.SuperElevation.Sections.ContainsKey(wfKey))
                simulator.SuperElevation.Sections[wfKey].Remove(section);

            if (key != wfKey)
            {
                if (simulator.SuperElevation.Sections.ContainsKey(key))
                    simulator.SuperElevation.Sections[key].Remove(section);
            }
        }

        /// <summary>
        /// Determines the location of the end of a track section when given information
        /// about the track section and the starting position of the track section.
        /// </summary>
        /// <param name="section">The TrackSection we are travelling along.</param>
        /// <param name="orientation">Rotation matrix giving the rotation of the origin in world space.</param>
        /// <param name="origin">Vector giving the position of the origin in world space.</param>
        /// <param name="root">WorldPosition for the starting point of this track section.</param>
        /// <param name="localStart">The location of the starting point of this track section in local coordinates.</param>
        /// <param name="heading">Reference value giving the rotation at the beginning of this track section in local coordinates.</param>
        /// <param name="localEnd">Output giving the ending point of this track section in local coordinates.</param>
        /// <param name="length">Output for the length of this track section (or angle if a curved section).</param>
        /// <param name="radius">Output giving the radius of this track section (or -1 if the section is straight).</param>
        /// <returns>WorldPosition object giving the position of the end of this track section.</returns>
        public static WorldPosition CalculateNextRoot(TrackSection section, WorldPosition orientation, Vector3 origin,
            WorldPosition root, Vector3 localStart, ref Vector3 heading, out Vector3 localEnd, out float length, out float radius)
        {
            radius = -1;
            Vector3 displacement;  // Local displacement to next subsection (from y=0 plane)

            // Set the start of this section to the end of the previous section
            WorldPosition nextRoot = new WorldPosition(root);

            // Now we need to compute the position of the end (nextRoot) of this subsection,
            // which will become root for the next subsection.

            // Clear nextRoot's translation vector so that nextRoot matrix contains rotation only
            nextRoot.XNAMatrix.Translation = Vector3.Zero;

            if (section.SectionCurve == null) // Straight section
            {
                // No change to heading, displacement is linear
                length = section.SectionSize.Length;
                displacement = Traveller.MSTSInterpolateAlongStraight(localStart, heading, length, orientation.XNAMatrix, out localEnd);
            }
            else // Curved section
            {
                radius = section.SectionCurve.Radius;
                length = MathHelper.ToRadians(section.SectionCurve.Angle);
                // Vector pointing from the curve position to the curve center point
                Vector3 left = Vector3.Cross(Vector3.Up, heading) * radius * Math.Sign(-length);
                // Consider rotation caused by the curve
                Matrix rot = Matrix.CreateRotationY(-length);

                displacement = Traveller.MSTSInterpolateAlongCurve(localStart, left, rot, orientation.XNAMatrix, out localEnd);

                // Handle change in rotation
                heading = Vector3.Transform(heading, rot);
                nextRoot.XNAMatrix = rot * nextRoot.XNAMatrix;
            }

            // Update nextRoot with new translation component
            nextRoot.XNAMatrix.Translation = origin + displacement;

            return nextRoot;
        }

        /// <summary>
        /// Generate superelevation viewers for a given dynamic track section
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynamicTrackViewer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrixInput">Position matrix.</param>
        public static void DecomposeDynamicSuperElevation(Viewer viewer, List<DynamicTrackViewer> dTrackList, DyntrackObj dTrackObj, WorldPosition worldMatrixInput)
        {
            // DYNAMIC TRACK
            // =============
            // Objectives:
            // 1-Decompose multi-subsection DT into individual sections.  
            // 2-Create updated transformation objects (instances of WorldPosition) to reflect 
            //   root of next subsection.
            // 3-Identify which subsections should and shouldn't have superelevation.
            // 4-Generate viewers for all subsections, making sure to respect presence or lack
            //   of superelevation.
            // 5-If no sections have superelevation, return false to inform WorldFile that
            //   this section should be generated the traditional way.
            //
            // Method: Iterate through each subsection, updating WorldPosition for the root of
            // each subsection.  The rotation component changes only in heading.  The translation 
            // component steps along the path to reflect the root of each subsection.

            // The following vectors represent local positioning relative to root of original section:
            Vector3 localV = Vector3.Zero; // Local position of subsection (in x-z plane)
            Vector3 heading = Vector3.Forward; // Local heading (unit vector)

            WorldPosition worldMatrix = new WorldPosition(worldMatrixInput); // Copy origin location

            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Original position for entire section
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

            // Right now it's not confirmed if superelevation viewers are actually needed, so they are added to a temporary list
            List<DynamicTrackViewer> tempViewers = new List<DynamicTrackViewer>();

            // Iterate through all subsections
            foreach (DyntrackObj.TrackSection dSection in dTrackObj.trackSections)
            {
                if (dSection.param1 == 0.0f || dSection.UiD == UInt32.MaxValue)
                    continue; // dTrackObj will contain unused sections sometimes, skip these

                // Convert dynamic track section into regular track section for compatibility with other methods
                TrackSection section = new TrackSection();
                // Only adds the bare minimum data needed, this TrackSection won't be fully defined
                if (dSection.isCurved == 1)
                {
                    section.SectionCurve = new SectionCurve
                    {
                        Radius = dSection.param2,
                        Angle = MathHelper.ToDegrees(dSection.param1)
                    };
                }
                else
                {
                    section.SectionSize = new SectionSize
                    {
                        Length = dSection.param1
                    };
                }

                // Set the start of this section to the end of the previous section
                WorldPosition root = new WorldPosition(nextRoot);

                nextRoot = CalculateNextRoot(section, worldMatrix, sectionOrigin, nextRoot, localV,
                    ref heading, out Vector3 localProjectedV, out float length, out float radius);

                localV = localProjectedV; // Move position to next subsection

                // To determine if this section needs superelevation, search for it in the global superelevation dictionary
                TrVectorSection tmp = FindSuperElevationSection(viewer, section, dTrackObj.UID, root, nextRoot, out bool reversed);

                if (tmp != null) // Section does have superelevation
                {
                    // If track profile isn't assigned, use default
                    if (tmp.TRPIndex < 0)
                        tmp.TRPIndex = 0;

                    TrProfile trProfile = viewer.TRPs[tmp.TRPIndex].TrackProfile;
                    
                    // Superelevation is enabled, generate track section with superelevation
                    if (viewer.Simulator.UseSuperElevation)
                    {
                        // Determine the centerline offset for superelevation roll
                        // 0 = centered, positive = centerline moves to inside of curve, negative = centerline moves to outside of curve

                        switch (trProfile.ElevationType)
                        {
                            case TrProfile.SuperelevationMethod.Outside: // Only outside rail should elevate
                                tmp.ElevOffsetM = trProfile.TrackGaugeM / 2.0f;
                                break;
                            case TrProfile.SuperelevationMethod.Inside: // Only inside rail should elevate
                                tmp.ElevOffsetM = -trProfile.TrackGaugeM / 2.0f;
                                break;
                            case TrProfile.SuperelevationMethod.Both: // Both rails should elevate
                            default:
                                tmp.ElevOffsetM = 0.0f;
                                break;
                        }

                        // If track profile is set to have no superelevation, set visual superelevation to 0
                        if (trProfile.ElevationType == TrProfile.SuperelevationMethod.None)
                            tmp.VisElevTable.ScaleY(0.0f);

                        // Processing done, prepare to generate section with superelevation
                        tempViewers.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, trProfile, tmp.VisElevTable, tmp.ElevOffsetM, reversed));
                    }
                    else // Superelevation disabled, generate without superelevation
                        tempViewers.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length, trProfile));
                }
                else // Section doesn't have superelevation, prepare to generate it without superelevation
                    tempViewers.Add(new SuperElevationViewer(viewer, root, nextRoot, radius, length));
            }

            // Add all the generated sections to the main list
            dTrackList.AddRange(tempViewers);
        }
    }

    public class SuperElevationViewer : DynamicTrackViewer
    {
        public SuperElevationViewer(Viewer viewer, WorldPosition position, WorldPosition endPosition, float radius, float angle,
            TrProfile trProfile = null, Interpolator elevs = null, float rollOffset = 0, bool reversed = false)
            : base(viewer, position)
        {
            // Instantiate classes
            Primitive = new SuperElevationPrimitive(viewer, position, endPosition, radius, angle, trProfile, elevs, rollOffset, reversed);
        }
    }

    public class SuperElevationPrimitive : DynamicTrackPrimitive
    {
        // Direction of applied superelevation
        readonly int Direction = 1;
        // Table giving superelevation angle in radians (Y) vs distance along track section from 0-1 (X)
        readonly Interpolator ElevAngles;
        // Offset from centerline for superelevation rotation
        Vector3 RollOffset;
        // Rotation matrix describing the current amount of superelevation roll
        Matrix SuperElevationRoll;
        // Is this section being generated in the opposite direction to that implied by the track vector section?
        readonly bool Reversed;

        public SuperElevationPrimitive(Viewer viewer, WorldPosition worldPosition, WorldPosition endPosition, float radius,
            float angle, TrProfile trProfile = null, Interpolator elevs = null, float rollOffset = 0, bool reversed = false)
            : base()
        {Reversed = reversed;

            // Set up orientation matrix, which describes the initial heading of the section in local coordinates
            Orientation = worldPosition.XNAMatrix;

            // Convert to local coordinates by setting position to 0 and removing yaw angle from rotation
            Orientation.Translation = Vector3.Zero;

            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Orientation);
            // Remove X and Z components of rotation to isolate for yaw angle (compass heading) only
            rotation.X = 0.0f;
            rotation.Z = 0.0f;
            // Renormalize the quaternion after deleting X and Z to get the Y component of rotation
            rotation.Normalize();
            // Cancel out the Y rotation of the orientation matrix to translate it to local coordinates
            Orientation = Orientation * Matrix.Invert(Matrix.CreateFromQuaternion(rotation));

            // Use the given angles of superelevation, or zero if none was given
            ElevAngles = elevs ?? new Interpolator(new float[] { 0, 1 }, new float[] { 0, 0 });

            // Direction of curvature inferred from the direction of roll, will be 0 if superelevation is 0
            // This works for both straight sections and curved sections
            Direction = Math.Sign(ElevAngles.Y.Max() + ElevAngles.Y.Min()) * (reversed ? 1 : -1);

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
            DTrackData.deltaY = 0; // Not used, change in height determined from the Orientation matrix

            if (DTrackData.IsCurved == 0)
                ObjectRadius = 0.5f * DTrackData.param1; // half-length
            else
                ObjectRadius = DTrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(DTrackData.param1)); // half chord length

            // Correct offset for direction of curvature
            // Rail height is assumed to be 275 mm above the origin of a track section
            RollOffset = new Vector3(rollOffset * Direction, 0.275f, 0);

            XNAEnd = endPosition.XNAMatrix.Translation;
        }

        /// <summary>
        /// Builds a SuperElevation LOD to SuperElevationProfile specifications as one vertex buffer and one index buffer.
        /// The order in which the buffers are built reflects the nesting in the TrProfile.  The nesting order is:
        /// (Polylines (Vertices)).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="iLOD">Index of LOD mesh to be generated from profile.</param>
        /// <param name="iLODItem">Index of LOD mesh to be generated from profile.</param>
        /// <returns>The ShapePrimitive generated fro this LOD.</returns>
        public override ShapePrimitive BuildPrimitive(Viewer viewer, int iLOD, int iLODItem)
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

            // stride is the number of vertices per section, used to connect equivalent verticies between segments
            uint stride = lodItem.NumVertices;

            // Allocate memory for vertices and indices
            VertexList = new VertexPositionNormalTexture[NumVertices]; // numVertices is now aggregate
            TriangleListIndices = new short[NumIndices]; // as is NumIndices

            // Build the mesh for lod
            VertexIndex = 0;
            IndexIndex = 0;
            Offset = 0;

            for (uint i = 0; i <= NumSections; i++)
            {
                SuperElevationRoll = DetermineRotation();

                Matrix displacement;
                float totLength;

                if (DTrackData.IsCurved == 0)
                    displacement = LinearGen(out totLength);
                else
                    displacement = CircArcGen(out totLength);

                foreach (Polyline pl in lodItem.Polylines)
                {
                    uint plv = 0; // Polyline vertex index
                    foreach (Vertex v in pl.Vertices)
                    {
                        // Generate vertex positions
                        Vector3 p = v.Position;

                        // Rotate the vertex position based on superelevation
                        if (Direction != 0)
                        {
                            p -= RollOffset;
                            p = Vector3.Transform(p, SuperElevationRoll);
                            p += RollOffset;
                        }

                        // In some extreme cases, track may have been rotated so much it's upside down
                        // Rotate 180 degrees to restore right side up
                        if (displacement.Up.Y < 0.0f)
                            p = Vector3.Transform(p, Matrix.CreateRotationZ((float)Math.PI));

                        // Move vertex to proper location in 3D space
                        VertexList[VertexIndex].Position = Vector3.Transform(p, displacement);
                        VertexList[VertexIndex].TextureCoordinate = v.TexCoord + pl.DeltaTexCoord * totLength;
                        VertexList[VertexIndex].Normal = v.Normal;

                        if (plv > 0 && VertexIndex > stride)
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
        // Overwrites the default method as superelevation may mean straight sections
        // require more than one section.
        public override void LinearGen()
        {
            // Define the number of track cross sections in addition to the base.
            // Track profile parameters are intended for curved sections, estimate their effect on straight sections
            if (Direction == 0)
            {
                NumSections = 1; // If there's no superelevation, we only need one section
            }
            else
            {
                NumSections = (int)(Math.Abs(DTrackData.param1 / (TrProfile.ChordSpan * 10.0f)));

                if (TrProfile.PitchControl == TrProfile.PitchControls.ChordLength)
                {
                    // Use chord length as segment length if it is more precise than initial estimate
                    if (TrProfile.PitchControlScalar < TrProfile.ChordSpan * 10.0f)
                        NumSections = (int)(Math.Abs(DTrackData.param1 / (TrProfile.PitchControlScalar)));
                }

                // Very short length track, use minimum of two sections
                if (NumSections == 0)
                    NumSections = 2;
                // Ensure an even number of sections
                if (NumSections % 2 == 1)
                    NumSections++;
            }

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (meters)
        }

        /// <summary>
        /// Determines the appropriate superelevation rotation matrix at the current position.
        /// </summary>
        Matrix DetermineRotation()
        {
            float to = (float)Offset / NumSections;
            float angle;

            if (Reversed)
                angle = -ElevAngles[1 - to];
            else
                angle = ElevAngles[to];
            
            return Matrix.CreateRotationZ(angle);
        }
    }
}
