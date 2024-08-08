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
using Orts.Simulation.Signalling;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Simulation
{
    public class SuperElevation
    {
        public List<List<TrVectorSection>> Curves;
        public Dictionary<int, List<TrVectorSection>> Sections;
        public float MaximumAllowedM;
        public Signals signalRef { get; protected set; }

        //check TDB for long curves and determine each section's position/elev in the curve
        public SuperElevation(Simulator simulator)
        {
            Curves = new List<List<TrVectorSection>>();
            Sections = new Dictionary<int, List<TrVectorSection>>();
            signalRef = simulator.Signals;

            if (simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMaxCantM <= 0) // Max allowed elevation controlled by user setting
                MaximumAllowedM = 0.07f + simulator.UseSuperElevation / 100f; 
            else // Max allowed elevation has been set in route file
                MaximumAllowedM = simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMaxCantM;

            // Superelevation MUST be limited to the track gauge or lower to avoid NaN errors
            MaximumAllowedM = MathHelper.Clamp(MaximumAllowedM, 0.0f, simulator.SuperElevationGauge);

            var SectionList = new List<TrVectorSection>();
            foreach (var node in simulator.TDB.TrackDB.TrackNodes)
            {
                if (node == null || node.TrJunctionNode != null || node.TrEndNode == true)
                    continue;
                var StartCurve = false;
                var CurveDir = 0;
                var Len = 0.0f;
                float nodeTotalLength = 0.0f;
                SectionList.Clear();
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
                        if (i == 1 || i == count)
                        {
                            // First and last curves in a series are connected to junctions or buffers
                            // If these sections are very short, undesirable superelevation can occur, so exclude these
                            if (theCurve.Radius * (float)Math.Abs(theCurve.Angle * Math.PI / 180.0f) < 15f)
                                continue; 
                        } 
                        if (StartCurve == false) // we are beginning a curve
                        {
                            StartCurve = true;
                            CurveDir = Math.Sign(sec.SectionCurve.Angle);
                            Len = 0f;
                        }
                        else if (CurveDir != Math.Sign(sec.SectionCurve.Angle)) // we are in curve, but bending different dir
                        {
                            MarkSections(simulator, SectionList, Len); // treat the sections encountered so far, then restart with other dir
                            CurveDir = Math.Sign(sec.SectionCurve.Angle);
                            SectionList.Clear();
                            Len = 0f; // StartCurve remains true as we are still in a curve
                        }
                        sectionLength = theCurve.Radius * (float)Math.Abs(theCurve.Angle * (Math.PI / 180.0f));
                        Len += sectionLength;
                        SectionList.Add(section);
                    }
                    else // meet a straight line
                    {
                        if (StartCurve == true) // we are in a curve, need to finish
                        {
                            MarkSections(simulator, SectionList, Len);
                            Len = 0f;
                            SectionList.Clear();
                        }
                        sectionLength = sec.SectionSize.Length;
                        StartCurve = false;
                    }
                    nodeTotalLength += sectionLength;

                    float nodeOffset = nodeTotalLength - sectionLength / 2.0f;
                    float routeMaxSpeed = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;

                    // Get speed limits for this section of track
                    float[] speeds = DetermineTrackSpeeds(signalRef, node, nodeOffset);
                    section.FreightSpeedMpS = Math.Min(speeds[0], routeMaxSpeed);
                    section.PassSpeedMpS = Math.Min(speeds[1], routeMaxSpeed);
                }
                if (StartCurve == true) // we are in a curve after looking at every section
                {
                    MarkSections(simulator, SectionList, Len);
                }
                SectionList.Clear();
            }
        }

        void MarkSections(Simulator simulator, List<TrVectorSection> SectionList, float Len)
        {
            if (Len < simulator.SuperElevationMinLen || SectionList.Count == 0)
                return; // Ignore curves too short or invalid data
            // Array of arc lengths for every section
            float[] lengths = new float[SectionList.Count];
            // The allowed change in superelevation per distance along curve, may change with speed
            float effectiveRunoffSlope = simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationRunoffSlope;

            // Determine proper level of superelevation for every section
            for (int i = 0; i < SectionList.Count; i++)
            {
                var sectionData = simulator.TSectionDat.TrackSections.Get(SectionList[i].SectionIndex);

                if (sectionData == null || sectionData.SectionCurve == null)
                {
                    SectionList[i].NomElevM = 0.0f;
                    continue;
                }

                lengths[i] = sectionData.SectionCurve.Radius * (float)Math.Abs(sectionData.SectionCurve.Angle * (Math.PI / 180.0f));

                // Calculate nominal superelevation amount only if it hasn't been set yet
                if (SectionList[i].NomElevM < 0.0f)
                {
                    // Support for old system with superelevation set in Route (TRK) file
                    if (simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM != null)
                    {
                        SectionList[i].NomElevM = simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM[sectionData.SectionCurve.Radius];
                    }
                    else // Newer standard for calculating superelevation
                    {
                        // TODO: Figure out how to actually get the speed limit in here
                        float paxSpeed = SectionList[i].PassSpeedMpS;
                        float freightSpeed = SectionList[i].FreightSpeedMpS;

                        // If track is too slow for superelevation, use none
                        if (Math.Max(paxSpeed, freightSpeed) < simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMinSpeedMpS)
                        {
                            SectionList[i].NomElevM = 0;
                            continue;
                        }

                        // Approximate ideal level of superelevation determined using E = (G*V^2) / (g*R), then subtract off cant deficiency
                        // For different speeds on the same curve, we can factor out speed and get a constant c = G / (g*R)
                        float elevationFactor = simulator.SuperElevationGauge / (9.81f * sectionData.SectionCurve.Radius);

                        // Calculate required superelevation for passenger and freight separately
                        float paxElevation = elevationFactor * (paxSpeed * paxSpeed) - simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMaxPaxUnderbalanceM;
                        float freightElevation = elevationFactor * (freightSpeed * freightSpeed) - simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMaxFreightUnderbalanceM;

                        SectionList[i].NomElevM = Math.Max(paxElevation, freightElevation); // Choose the highest required superelevation

                        SectionList[i].NomElevM = (float)Math.Round(SectionList[i].NomElevM / simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationPrecisionM, MidpointRounding.AwayFromZero)
                            * simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationPrecisionM; // Round superelevation amount to next higher increment of precision

                        // Runoff slope may be limited by the rate of superelevation change per time
                        float limitedRunoffSlope = simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationRunoffSpeedMpS / Math.Max(paxSpeed, freightSpeed);

                        if (limitedRunoffSlope < effectiveRunoffSlope)
                            effectiveRunoffSlope = limitedRunoffSlope;

                        SectionList[i].NomElevM = MathHelper.Clamp(SectionList[i].NomElevM, simulator.TRK.Tr_RouteFile.SuperElevation.SuperElevationMinCantM, MaximumAllowedM);
                    }
                }
            }

            Curves.Add(new List<TrVectorSection>(SectionList)); // add the curve
            MapWFiles2Sections(SectionList); // map these sections to tiles, so we can compute it quicker later
            if (SectionList.Count == 1) // only one section in the curve
            {
                SectionList[0].StartElevM = SectionList[0].EndElevM = 0f;
                // Limit rate of change of superelevation to the runoff rate value
                SectionList[0].MidElevM = Math.Min(SectionList[0].NomElevM, (lengths[0] / 2.0f) * effectiveRunoffSlope);
            }
            else // more than one section in the curve
            {
                int count = 0;
                float accumulatedLength = 0;

                foreach (var section in SectionList)
                {
                    if (count == 0)
                    {
                        section.StartElevM = 0f;
                        section.MidElevM = section.NomElevM;
                        section.EndElevM = Math.Max(section.NomElevM, SectionList[count + 1].NomElevM);
                    }
                    else if (count == SectionList.Count - 1)
                    {
                        section.StartElevM = SectionList[count - 1].EndElevM;
                        section.MidElevM = section.NomElevM;
                        section.EndElevM = 0f;
                    }
                    else
                    {
                        section.StartElevM = SectionList[count - 1].EndElevM;
                        // Attempt to limit rate of change of superelevation in middle curve segments
                        float maxChange = (lengths[count] / 2.0f) * effectiveRunoffSlope;
                        section.MidElevM = MathHelper.Clamp(section.NomElevM, section.StartElevM - maxChange, section.StartElevM + maxChange);
                        section.EndElevM = MathHelper.Clamp(Math.Max(section.NomElevM, SectionList[count + 1].NomElevM), section.MidElevM - maxChange, section.MidElevM + maxChange);
                        // Rate of change at the start and end is controlled by the next section
                    }
                    // Limit rate of change of superelevation across entire curve to the value of the runoff rate
                    float maxStart = Math.Min(accumulatedLength, Len - accumulatedLength)
                        * effectiveRunoffSlope;
                    float maxMid = Math.Min(accumulatedLength + lengths[count] / 2.0f, Len - (accumulatedLength + lengths[count] / 2.0f))
                        * effectiveRunoffSlope;
                    float maxEnd = Math.Min(accumulatedLength + lengths[count], Len - (accumulatedLength + lengths[count]))
                        * effectiveRunoffSlope;

                    section.StartElevM = MathHelper.Clamp(section.StartElevM, 0, maxStart);
                    section.MidElevM = MathHelper.Clamp(section.MidElevM, 0, maxMid);
                    section.EndElevM = MathHelper.Clamp(section.EndElevM, 0, maxEnd);

                    accumulatedLength += lengths[count];
                    count++;
                }
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
