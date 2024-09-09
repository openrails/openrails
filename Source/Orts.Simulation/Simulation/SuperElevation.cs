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
using ORTS.Common;
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
            foreach (var node in simulator.TDB.TrackDB.TrackNodes)
            {
                if (node == null || node.TrJunctionNode != null || node.TrEndNode == true)
                    continue;
                bool startCurve = false;
                int curveDir = 0;
                float curveLen = 0.0f;
                List<float> sectionLengths = new List<float>();
                float nodeTotalLength = 0.0f;
                sectionList.Clear();
                SectionCurve theCurve;
                int i = 0;
                int count = node.TrVectorNode.TrVectorSections.Length;

                foreach (var section in node.TrVectorNode.TrVectorSections) // loop all curves
                {
                    i++;
                    float sectionLength = 0.0f;
                    var sec = simulator.TSectionDat.TrackSections.Get(section.SectionIndex);
                    if (sec == null)
                        continue;
                    theCurve = sec.SectionCurve;
                    if (sec.SectionSize != null && Math.Abs(sec.SectionSize.Width - simulator.SuperElevationGauge) > 0.2f)
                        continue; // the main route has a gauge different than mine

                    if (theCurve != null && !theCurve.Angle.AlmostEqual(0f, 0.01f)) // Check for valid curves
                    {
                        sectionLength = theCurve.Radius * (float)Math.Abs(theCurve.Angle * (Math.PI / 180.0f));
                        if (i == 1 || i == count)
                        {
                            // First and last curves in a series are connected to junctions or buffers
                            // If these sections are short, undesirable superelevation can occur, so exclude these
                            if (sectionLength < 15f)
                                continue; 
                        } 
                        if (startCurve == false) // we are beginning a curve
                        {
                            startCurve = true;
                            curveDir = Math.Sign(sec.SectionCurve.Angle);
                            curveLen = 0f;
                        }
                        else if (curveDir != Math.Sign(sec.SectionCurve.Angle)) // we are in curve, but bending different dir
                        {
                            MarkSections(simulator, sectionList, curveLen, sectionLengths, curveDir); // treat the sections encountered so far, then restart with other dir
                            curveDir = Math.Sign(sec.SectionCurve.Angle);
                            sectionList.Clear();
                            sectionLengths.Clear();
                            curveLen = 0f; // startCurve remains true as we are still in a curve
                        }
                        curveLen += sectionLength;
                        sectionLengths.Add(sectionLength);
                        sectionList.Add(section);
                    }
                    else // meet a straight line
                    {
                        if (startCurve == true) // we are in a curve, need to finish
                        {
                            MarkSections(simulator, sectionList, curveLen, sectionLengths, curveDir);
                            curveLen = 0f;
                            sectionList.Clear();
                            sectionLengths.Clear();
                        }
                        sectionLength = sec.SectionSize.Length;
                        startCurve = false;
                    }
                    nodeTotalLength += sectionLength;

                    float nodeOffset = nodeTotalLength - sectionLength / 2.0f;

                    // Get speed limits for this section of track if they aren't known
                    if (section.PassSpeedMpS < 0.0f || section.FreightSpeedMpS < 0.0f)
                    {
                        float[] speeds = DetermineTrackSpeeds(signalRef, node, nodeOffset);
                        section.FreightSpeedMpS = Math.Min(speeds[0], routeMaxSpeed);
                        section.PassSpeedMpS = Math.Min(speeds[1], routeMaxSpeed);
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

            for (int s = 0; s < simulator.TRK.Tr_RouteFile.SuperElevation.Count; s++)
            {
                SuperElevationStandard checkStandard = simulator.TRK.Tr_RouteFile.SuperElevation[s];
                // If curve speed is within the appropriate speed range for the superelevation standard, use it
                // Otherwise, check the next one
                if (maxCurveSpeedMpS < checkStandard.MaxSpeedMpS + 0.05f && maxCurveSpeedMpS > checkStandard.MinSpeedMpS - 0.05f)
                {
                    standard = checkStandard;

                    // Calculate the allowed change in superelevation per distance along curve, may change with speed
                    effectiveRunoffSlope = Math.Min(standard.RunoffSlope, standard.RunoffSpeedMpS / maxCurveSpeedMpS);

                    // Ensure superelevation is limited to the track gauge no matter what to avoid NaN errors
                    maxElev = MathHelper.Clamp(maxElev, 0.0f, simulator.SuperElevationGauge);

                    break;
                }
            }

            if (standard == null)
            {
                foreach (TrVectorSection s in SectionList)
                    s.NomElevM = 0;
                return; // No superelevation needed, stop processing here
            }
            if ((standard.MinCantM / effectiveRunoffSlope) * 2.0f > totLen * 0.75f)
            {
                foreach (TrVectorSection s in SectionList)
                    s.NomElevM = 0;
                return; // Curve is so short that no meaningful superelevation can be applied
            }

            // Determine proper level of superelevation for every section
            for (int i = 0; i < SectionList.Count; i++)
            {
                // Superelevation has not been calculated for this section yet
                if (SectionList[i].NomElevM < 0.0f)
                {
                    var sectionData = simulator.TSectionDat.TrackSections.Get(SectionList[i].SectionIndex);

                    if (sectionData == null || sectionData.SectionCurve == null)
                    {
                        SectionList[i].NomElevM = 0.0f;
                        continue;
                    }
                    else
                    {
                        float superElevation;

                        // Support for old system with superelevation set directly in Route (TRK) file
                        if (standard.UseLegacyCalculation && simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM != null)
                        {
                            superElevation = simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM[sectionData.SectionCurve.Radius];
                        }
                        else // Newer standard for calculating superelevation
                        {
                            if (standard != null)
                            {
                                float paxSpeed = SectionList[i].PassSpeedMpS;
                                float freightSpeed = SectionList[i].FreightSpeedMpS;

                                // Approximate ideal level of superelevation determined using E = (G*V^2) / (g*R), then subtract off cant deficiency
                                // For different speeds on the same curve, we can factor out speed and get a constant c = G / (g*R)
                                float elevationFactor = simulator.SuperElevationGauge / (9.81f * sectionData.SectionCurve.Radius);
                                // Calculate required superelevation for passenger and freight separately
                                float paxElevation = elevationFactor * (paxSpeed * paxSpeed) - standard.MaxPaxUnderbalanceM;
                                float freightElevation = elevationFactor * (freightSpeed * freightSpeed) - standard.MaxFreightUnderbalanceM;

                                superElevation = Math.Max(paxElevation, freightElevation); // Choose the highest required superelevation
                            }
                            else // No superelevation needed (shouldn't reach this point, this is a failsafe)
                                superElevation = 0.0f;
                        }
                        superElevation = (float)Math.Round(superElevation / standard.PrecisionM, MidpointRounding.AwayFromZero)
                            * standard.PrecisionM; // Round superelevation amount to next higher increment of precision

                        superElevation = MathHelper.Clamp(superElevation, standard.MinCantM, maxElev);

                        SectionList[i].NomElevM = superElevation;
                    }
                }
            }

            Curves.Add(new List<TrVectorSection>(SectionList)); // add the curve
            MapWFiles2Sections(SectionList); // map these sections to tiles, so we can find them quicker later

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
                    endElevs[count] = Math.Min((SectionList[count].NomElevM + SectionList[count + 1].NomElevM) / 2.0f,
                        maxEnd);

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

                // Visual superelevation is stored in terms of angle in radians rather than meters
                float[] angles = elevations.Select(e => (float)Math.Asin(e / simulator.SuperElevationGauge)).ToArray();
                SectionList[count].VisElevTable = new Interpolator(SectionList[count].PhysElevTable.X, angles);
                // Invert visual elevation values based on curve direction
                // direction is negated for consistency of direction sense in other places
                SectionList[count].VisElevTable.ScaleY(-direction);

                accumulatedLength += lengths[count];
                count++;
            }
        }

        //find all sections in a tile, save the info to a look-up table
        void MapWFiles2Sections(List<TrVectorSection> sections)
        {
            foreach (var section in sections)
            {
                var key = (int)(Math.Abs(section.WFNameX) + Math.Abs(section.WFNameZ));
                if (Sections.ContainsKey(key)) Sections[key].Add(section);
                else
                {
                    List<TrVectorSection> tmpSections = new List<TrVectorSection>();
                    tmpSections.Add(section);
                    Sections.Add(key, tmpSections);
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
