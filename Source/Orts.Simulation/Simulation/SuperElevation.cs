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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Signalling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Simulation
{
    public class SuperElevation
    {
        public List<List<TrVectorSection>> Curves;
        public Dictionary<int, List<TrVectorSection>> Sections;
        public Signals signalRef { get; protected set; }

        //check TDB for long curves and determine each section's position/elev in the curve
        public SuperElevation(Simulator simulator)
        {
            Curves = new List<List<TrVectorSection>>();
            Sections = new Dictionary<int, List<TrVectorSection>>();
            signalRef = simulator.Signals;

            float routeMaxSpeed = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;

            var sectionList = new List<TrVectorSection>();
            // Check all track nodes
            foreach (var node in simulator.TDB.TrackDB.TrackNodes)
            {
                if (node == null || node.TrJunctionNode != null || node.TrEndNode == true)
                    continue;

                // Run through the vector section once to get the total length
                float nodeTotLength = 0;
                foreach (TrVectorSection vectorSec in node.TrVectorNode.TrVectorSections)
                {
                    TrackSection sec = simulator.TSectionDat.TrackSections.Get(vectorSec.SectionIndex);
                    if (sec == null)
                        continue;
                    SectionCurve theCurve = sec.SectionCurve;

                    if (theCurve != null) // Curved section
                        nodeTotLength += theCurve.Radius * (float)Math.Abs(MathHelper.ToRadians(theCurve.Angle));
                    else // Straight section
                        nodeTotLength += sec.SectionSize.Length;
                }

                bool startCurve = false;
                int curveDir = 0;
                float curveLen = 0.0f;
                List<float> sectionLengths = new List<float>();
                float nodeCumLength = 0.0f;
                sectionList.Clear();
                int count = node.TrVectorNode.TrVectorSections.Length;

                // Check all sections in vector nodes
                for (int i = 0; i < count; i++)
                {
                    float sectionLength = 0.0f;
                    TrackSection sec = simulator.TSectionDat.TrackSections.Get(node.TrVectorNode.TrVectorSections[i].SectionIndex);
                    if (sec == null)
                        continue;
                    SectionCurve theCurve = sec.SectionCurve;

                    TrackSection nextSec = null;
                    SectionCurve nextCurve = null;

                    if (i < count - 1)
                    {
                        nextSec = simulator.TSectionDat.TrackSections.Get(node.TrVectorNode.TrVectorSections[i + 1].SectionIndex);
                        if (nextSec != null)
                            nextCurve = nextSec.SectionCurve;
                    }

                    // This section is a curve
                    if (theCurve != null)
                    {
                        sectionLength = theCurve.Radius * (float)Math.Abs(MathHelper.ToRadians(theCurve.Angle));

                        // First and last sections in a node are connected to junctions or buffers
                        // Don't add superelevation too close to the beginning or end
                        if (!(nodeCumLength + sectionLength < 20f || nodeTotLength - nodeCumLength < 20f))
                        {
                            // Weren't in a curve but now are
                            if (startCurve == false)
                            {
                                startCurve = true;
                                curveDir = Math.Sign(theCurve.Angle);
                                curveLen = 0f;
                            }
                            else if (curveDir != Math.Sign(theCurve.Angle)) // we are in curve, but bending different dir
                            {
                                MarkSections(simulator, sectionList, curveLen, sectionLengths, curveDir); // treat the sections encountered so far, then restart with other dir
                                curveDir = Math.Sign(theCurve.Angle);
                                sectionList.Clear();
                                sectionLengths.Clear();
                                curveLen = 0f; // startCurve remains true as we are still in a curve
                            }
                            curveLen += sectionLength;
                            sectionLengths.Add(sectionLength);
                            sectionList.Add(node.TrVectorNode.TrVectorSections[i]);
                        }
                    }
                    else // This section is straight
                    {
                        // NOTE: We may add superelevation to straight sections in order to smooth out transition to/from curve superelevation
                        sectionLength = sec.SectionSize.Length;

                        // First and last sections in a node are connected to junctions or buffers
                        // Don't add superelevation too close to the beginning or end
                        if (!(nodeCumLength + sectionLength < 20f || nodeTotLength - nodeCumLength < 20f))
                        {
                            // Previous section was in a curve, may need to end curve
                            if (startCurve == true)
                            {
                                // Include this straight section in the curve so long as the next section
                                // doesn't curve in the opposite direction (or is straight)
                                if (nextSec != null && !(nextCurve != null && curveDir != Math.Sign(nextCurve.Angle)))
                                {
                                    curveLen += sectionLength;
                                    sectionLengths.Add(sectionLength);
                                    sectionList.Add(node.TrVectorNode.TrVectorSections[i]);
                                }
                                // End curve if the next section isn't a continuation of this curve
                                if (!(nextCurve != null && curveDir == Math.Sign(nextCurve.Angle)))
                                {
                                    MarkSections(simulator, sectionList, curveLen, sectionLengths, curveDir);
                                    curveLen = 0f;
                                    sectionList.Clear();
                                    sectionLengths.Clear();
                                    startCurve = false;
                                }
                            }
                            else if (nextSec != null && nextCurve != null) // Not in a curve, but next section is a curve, start superelevation on this section
                            {
                                startCurve = true;
                                curveDir = Math.Sign(nextCurve.Angle);
                                curveLen = 0;

                                curveLen += sectionLength;
                                sectionLengths.Add(sectionLength);
                                sectionList.Add(node.TrVectorNode.TrVectorSections[i]);
                            }
                        }
                    }
                    nodeCumLength += sectionLength;

                    float nodeOffset = nodeCumLength - sectionLength / 2.0f;

                    // Get speed limits for this section of track if they aren't known
                    if (node.TrVectorNode.TrVectorSections[i].PassSpeedMpS < 0.0f || node.TrVectorNode.TrVectorSections[i].FreightSpeedMpS < 0.0f)
                    {
                        float[] speeds = DetermineTrackSpeeds(signalRef, node, nodeOffset);
                        node.TrVectorNode.TrVectorSections[i].FreightSpeedMpS = Math.Min(speeds[0], routeMaxSpeed);
                        node.TrVectorNode.TrVectorSections[i].PassSpeedMpS = Math.Min(speeds[1], routeMaxSpeed);
                    }
                }
                if (startCurve == true) // we are in a curve after looking at every section
                {
                    MarkSections(simulator, sectionList, curveLen, sectionLengths, curveDir);
                }
                sectionList.Clear();
            }
        }

        void MarkSections(Simulator simulator, List<TrVectorSection> SectionList, float totLen, List<float> lengths, int direction)
        {
            if (SectionList.Count <= 0)
                return; // Avoid errors with invalid section lists

            // The superelevation standard we will use. null means no superelevation
            SuperElevationStandard standard = null;
            // Get the maximum speed limit along the curve to determine which superelevation standard to use
            float maxCurveSpeedMpS = SectionList.Max(sec => Math.Max(sec.PassSpeedMpS, sec.FreightSpeedMpS));
            // Also determine the superelevation limits for this curve
            float effectiveRunoffSlope = 0;
            float maxElev = 0;

            foreach (SuperElevationStandard checkStandard in simulator.TRK.Tr_RouteFile.SuperElevation)
            {
                // If curve speed is within the appropriate speed range for the superelevation standard, use it
                // Otherwise, check the next one
                if (maxCurveSpeedMpS < checkStandard.MaxSpeedMpS + 0.05f && maxCurveSpeedMpS > checkStandard.MinSpeedMpS - 0.05f)
                {
                    standard = checkStandard;

                    // Calculate the allowed change in superelevation per distance along curve, may change with speed
                    effectiveRunoffSlope = Math.Min(standard.RunoffSlope, standard.RunoffSpeedMpS / maxCurveSpeedMpS);

                    // Ensure superelevation is limited to the track gauge no matter what to avoid NaN errors
                    maxElev = MathHelper.Clamp(standard.MaxCantM, 0.0f, simulator.SuperElevationGauge);
                    // Futher limit superelevation to something that can be achieved by the curve
                    // This limit gives enough distance for superelevation to smoothly build up
                    maxElev = Math.Min(totLen / (2.0f * 1.3f) * effectiveRunoffSlope, maxElev);
                    // NOTE: If maxElev is less than the minimum cant, that indicates the curve is too short for superelevation to fit

                    break;
                }
            }

            if ((standard == null || maxElev < standard.MinCantM) && !SectionList.Any(s => s.NomElevM > 0.0f))
            {
                foreach (TrVectorSection s in SectionList)
                {
                    s.NomElevM = 0; // No superelevation needed, or curve is so short that no meaningful superelevation can be applied
                    return;
                }
            }
            else
            {
                // Superelevation can be applied, run calculations
                foreach (TrVectorSection s in SectionList)
                {
                    // Superelevation has not been calculated for this section yet
                    // FUTURE: Superelevation NomElevM may be specified externally, eg: by a route editor
                    if (s.NomElevM < 0.0f)
                    {
                        var sectionData = simulator.TSectionDat.TrackSections.Get(s.SectionIndex);

                        if (sectionData == null || sectionData.SectionCurve == null)
                        {
                            // Invalid data, or this is a segment of straight track
                            s.NomElevM = 0.0f;
                            continue;
                        }
                        else
                        {
                            float superElevation = 0.0f;

                            // Support for old system with superelevation set directly in Route (TRK) file
                            if (standard.UseLegacyCalculation && simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM != null)
                            {
                                superElevation = simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM[sectionData.SectionCurve.Radius];
                            }
                            else if (standard != null) // Newer standard for calculating superelevation
                            {
                                float paxSpeed = s.PassSpeedMpS;
                                float freightSpeed = s.FreightSpeedMpS;

                                // Approximate ideal level of superelevation determined using E = (G*V^2) / (g*R), then subtract off cant deficiency
                                // For different speeds on the same curve, we can factor out speed and get a constant c = G / (g*R)
                                float elevationFactor = simulator.SuperElevationGauge / (9.81f * sectionData.SectionCurve.Radius);
                                // Calculate required superelevation for passenger and freight separately
                                float paxElevation = elevationFactor * (paxSpeed * paxSpeed) - standard.MaxPaxUnderbalanceM;
                                float freightElevation = elevationFactor * (freightSpeed * freightSpeed) - standard.MaxFreightUnderbalanceM;

                                superElevation = Math.Max(paxElevation, freightElevation); // Choose the highest required superelevation
                            }
                            superElevation = (float)Math.Round(superElevation / standard.PrecisionM, MidpointRounding.AwayFromZero)
                                * standard.PrecisionM; // Round superelevation amount to next higher increment of precision

                            superElevation = MathHelper.Clamp(superElevation, standard.MinCantM, maxElev);

                            s.NomElevM = superElevation;
                        }
                    }
                }
            }

            Curves.Add(new List<TrVectorSection>(SectionList)); // Add this curve to the global list of superelevation curves
            MapWFiles2Sections(SectionList); // Add all superelevation sections to the tile dictionary for checking later

            // Calculate the amount of superelevation as a function of curve length for all curve segments
            int count = 0;
            float accumulatedLength = 0;

            float[] startElevs = new float[SectionList.Count];
            float[] endElevs = new float[SectionList.Count];

            foreach (var section in SectionList)
            {
                // Determine target starting and ending superelevation of this section
                if (count <= 0)
                    startElevs[count] = 0;
                else
                    startElevs[count] = endElevs[count - 1];

                // Limit rate of change of superelevation across entire curve to the value of the runoff rate
                float maxEnd = Math.Min(accumulatedLength + lengths[count], totLen - (accumulatedLength + lengths[count]))
                    * effectiveRunoffSlope;

                if (count >= SectionList.Count - 1)
                    endElevs[count] = 0;
                else
                {
                    // Special case: Next section is a short straight, but isn't the end of the curve
                    // In this case, we force the straight section to maintain nonzero superelevation
                    if (count < SectionList.Count - 2 && SectionList[count + 1].NomElevM == 0.0f
                        && lengths[count + 1] < (SectionList[count].NomElevM / effectiveRunoffSlope) * 1.5f)
                        SectionList[count + 1].NomElevM = (SectionList[count].NomElevM + SectionList[count + 2].NomElevM) / 2.0f;

                    endElevs[count] = Math.Min((SectionList[count].NomElevM + SectionList[count + 1].NomElevM) / 2.0f,
                        maxEnd);
                }

                // Initialize superelevation profile with linear change in superelevation as fallback
                float[] elevations = new float[] { startElevs[count], endElevs[count] };
                float[] positions = new float[] { 0, 1.0f };

                // Curve length required to change superelevation
                float startRunoffLengthM = Math.Abs(SectionList[count].NomElevM - startElevs[count]) / effectiveRunoffSlope;
                float endRunoffLengthM = Math.Abs(endElevs[count] - SectionList[count].NomElevM) / effectiveRunoffSlope;
                float totalRunoffLengthM = Math.Abs(endElevs[count] - startElevs[count]) / effectiveRunoffSlope;

                // Section is too short for ideal superelevation profile
                if (startRunoffLengthM + endRunoffLengthM >= lengths[count]) 
                {
                    // Section isn't purely increasing or decreasing (the fallback case works otherwise)
                    if (totalRunoffLengthM < (lengths[count] * 0.75f))
                    {
                        float midLength;
                        float midElev;
                        if (SectionList[count].NomElevM > startElevs[count] && SectionList[count].NomElevM > endElevs[count])
                        {
                            // Nominal elevation is the largest - need upward cusp
                            // Calculate the size and location of the cusp based on theoretical vs actual change in elevation
                            float maxEndElev = startElevs[count] + effectiveRunoffSlope * lengths[count];

                            midLength = lengths[count] - ((maxEndElev - endElevs[count]) / effectiveRunoffSlope) / 2.0f;
                            midElev = startElevs[count] + effectiveRunoffSlope * midLength;
                        }
                        else if (SectionList[count].NomElevM == 0.0f)
                        {
                            // Special case: Downward cusp required on a section with zero design superelevation (straight track)
                            // In this case, avoid the cusp entirely
                            midLength = lengths[count] / 2.0f;
                            midElev = (startElevs[count] + endElevs[count]) / 2.0f;
                        }
                        else
                        {
                            // Need downward cusp
                            // Calculate the size and location of the cusp based on theoretical vs actual change in elevation
                            float minEndElev = startElevs[count] - effectiveRunoffSlope * lengths[count];

                            midLength = lengths[count] - ((endElevs[count] - minEndElev) / effectiveRunoffSlope) / 2.0f;
                            midElev = startElevs[count] + effectiveRunoffSlope * midLength;
                        }

                        midElev = Math.Max(0, midElev);

                        elevations = new float[] { startElevs[count], midElev, endElevs[count] };
                        positions = new float[] { 0.0f, midLength / lengths[count], 1.0f };
                    }
                }
                else // We can use the ideal superelevation profile
                {
                    // Ideal profile gradually changes from start elevation to the nominal elevation,
                    // then holds the nominal elevation before changing from the nominal elevation to the end elevation.

                    // Extra calculations only needed if superelevation is not constant across whole section
                    if (!(startElevs[count] == SectionList[count].NomElevM && SectionList[count].NomElevM == endElevs[count]))
                    {
                        if (startElevs[count] != SectionList[count].NomElevM && endElevs[count] != SectionList[count].NomElevM)
                        {
                            // Most complex case: Superelevation is different all over the section
                            elevations = new float[] { startElevs[count], SectionList[count].NomElevM, SectionList[count].NomElevM, endElevs[count] };
                            positions = new float[] { 0.0f, (startRunoffLengthM) / lengths[count], (lengths[count] - endRunoffLengthM) / lengths[count], 1.0f };
                        }
                        else
                        {
                            // Nominal superelevation is the same as the start or end superelevation
                            float midLength = startElevs[count] == SectionList[count].NomElevM ? lengths[count] - endRunoffLengthM : startRunoffLengthM;

                            elevations = new float[] { startElevs[count], SectionList[count].NomElevM, endElevs[count] };
                            positions = new float[] { 0.0f, midLength / lengths[count], 1.0f };
                        }
                    }
                }

                SectionList[count].PhysElevTable = new Interpolator(positions, elevations);

                // Only generate visual superelevation if option is enabled
                if (simulator.UseSuperElevation)
                {
                    // Visual superelevation is stored in terms of angle in radians rather than meters
                    float[] angles = elevations.Select(e => (float)Math.Asin(e / simulator.SuperElevationGauge)).ToArray();

                    SectionList[count].VisElevTable = new Interpolator(SectionList[count].PhysElevTable.X, angles);
                    // Invert visual elevation values based on curve direction
                    // direction is negated for consistency of direction sense in other places
                    SectionList[count].VisElevTable.ScaleY(-direction);
                }

                accumulatedLength += lengths[count];
                count++;
            }
        }

        // Add sections to a dictionary separated by the tiles in which those sections are contained
        void MapWFiles2Sections(List<TrVectorSection> sections)
        {
            foreach (var section in sections)
            {
                // Need to consider both the WFName tile and the 'actual' tile
                // These may differ if a section crosses a tile boundary, in which case it should be added to both tiles
                // NOTE: The WFName seems to indicate where the start of the section is, while the Tile indicates the end(?)
                int key = (int)(Math.Abs(section.TileX) + Math.Abs(section.TileZ));
                int wfKey = (int)(Math.Abs(section.WFNameX) + Math.Abs(section.WFNameZ));

                if (Sections.ContainsKey(key))
                    Sections[key].Add(section);
                else
                    Sections.Add(key, new List<TrVectorSection> { section });
                // Section crosses a tile boundary, add it to the other tile as well
                if (wfKey != key)
                {
                    if (Sections.ContainsKey(wfKey))
                        Sections[wfKey].Add(section);
                    else
                        Sections.Add(wfKey, new List<TrVectorSection> { section });
                }
            }
        }

        /// <summary>
        /// Gets the speed limit data for a track section when given a reference to the route signals,
        /// the track node containing the track section, and the offset distance from the start of the
        /// node to the point of interest.
        /// </summary>
        /// <returns>Returns an array of 2 floats where the first element is the freight speed
        /// and the second element is the passenger speed.</returns>
        float[] DetermineTrackSpeeds(Signals signalRef, TrackNode node, float offset)
        {
            // Note: Direction is always set to 1 (forward) but this will still check both forward and reverse directions
            // to determine speed limits
            int tcNodeIndex = node.TCCrossReference.GetXRefIndex(offset, 1);
            TrackCircuitSectionXref circuitSection = node.TCCrossReference[tcNodeIndex];
            int tcIndex = circuitSection.Index;
            float circuitOffset = offset - circuitSection.OffsetLength[1];

            // Temporary storage for resulting speed computations
            float frtSpeed, frtSpeedBack, paxSpeed, paxSpeedBack;

            frtSpeed = ScanSpeed(signalRef, tcIndex, circuitOffset, true, true);
            paxSpeed = ScanSpeed(signalRef, tcIndex, circuitOffset, true, false);
            frtSpeedBack = ScanSpeed(signalRef, tcIndex, circuitOffset, false, true);
            paxSpeedBack = ScanSpeed(signalRef, tcIndex, circuitOffset, false, false);

            // Select MAXIMUM speed in either direction
            frtSpeed = Math.Max(frtSpeed, frtSpeedBack);
            paxSpeed = Math.Max(paxSpeed, paxSpeedBack);

            // If no speed limit was determined, fallback to defaults
            if (frtSpeed <= 0 && paxSpeed > 0)
                frtSpeed = paxSpeed;
            if (paxSpeed <= 0 && frtSpeed > 0)
                paxSpeed = frtSpeed;
            // Could not determine speed limit, set to infinity to be safe
            if (frtSpeed <= 0)
                frtSpeed = float.PositiveInfinity;
            if (paxSpeed <= 0)
                paxSpeed = float.PositiveInfinity;

            return new float[] { frtSpeed, paxSpeed };
        }

        /// <summary>
        /// Gets a single speed limit value when given a reference to the signal system, the index of the 
        /// track circuit section to scan, the offset along that circuit (in the forward direction), a
        /// bool to indicate if the search is forwards (t) or reverse (f), and a bool to indicate if freight (t)
        /// or passenger (f) speed signs should be searched.
        /// </summary>
        /// <returns>Returns the speed limit at the given track location, or -infinity if none could be found.</returns>
        float ScanSpeed(Signals signalRef, int tcIndex, float circuitOffset, bool forwards, bool freight)
        {
            float speed = float.NegativeInfinity;

            // Always scans routes facing forward, but scans for facing speed signs if scanning backwards
            // This has the effect of searching for backing speed signs regardless of the direction given
            // No limit on search distance, assumes default switch orientation, ignores signals, ignores reservation
            List<int> speedPostRef = signalRef.ScanRoute(null, tcIndex, circuitOffset, 1, forwards, -1,
                false, true, false, false, false, false, false, !forwards, forwards, freight);

            if (speedPostRef.Count > 0)
            {
                var speedPost = signalRef.SignalObjects[Math.Abs(speedPostRef[0])];
                var speeds = speedPost.this_lim_speed(SignalFunction.SPEED);
                if (freight)
                    speed = speeds.speed_freight;
                else
                    speed = speeds.speed_pass;
            }

            return speed;
        }
    }
}
