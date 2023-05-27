// COPYRIGHT 2015, 2018 by the Open Rails project.
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
//
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Orts.Formats.Msts;
using Orts.Simulation;
using ORTS.Common;

namespace ORTS.TrackViewer.Editing.Charts
{
    /// <summary>
    /// Class to calculate and store the data needed for showing a chart with altitude, grade and other information for a certain path.
    /// </summary>
    [Serializable()]
    public class PathChartData
    {
        #region public members
        /// <summary>List of individual points with path data along the path.</summary>
        [JsonProperty("PathChartPoints")]
        public IEnumerable<PathChartPoint> PathChartPoints { get; private set; }

        /// <summary>point for which all of the data (apart from distance along section) are the maxima seen in all PathChartPoints</summary>
        [JsonProperty("PointWithMaxima")]
        public PathChartPoint PointWithMaxima { get; private set; }
        /// <summary>point for which all of the data (apart from distance along section) are the minima seen in all PathChartPoints</summary>
        [JsonProperty("PointWithMinima")]
        public PathChartPoint PointWithMinima { get; private set; }
        /// <summary>The distance along the path for each path-node</summary>
        [JsonIgnore]
        public IDictionary<TrainpathNode, double> DistanceAlongPath;
        /// <summary>Is there actually a path loaded with one or more points</summary>
        [JsonIgnore]
        public bool HasPath { get { return PathChartPoints != null && PathChartPoints.Count() > 0; } }

        [JsonProperty("PathName")]
        private string PathName { get; set; }
        #endregion

        #region private members
        /// <summary>Minimum of all DistanceAlongPath in PathChartPoints</summary>
        private float MinDistanceAlongPath;
        /// <summary>Maximum of all DistanceAlongPath in PathChartPoints</summary>
        private float MaxDistanceAlongPath;
        /// <summary>Minimum of all HeightM in PathChartPoints</summary>
        private float MinHeightM;
        /// <summary>Maximum of all HeightM in PathChartPoints</summary>
        private float MaxHeightM;
        /// <summary>Minimum of all GradePercent in PathChartPoints</summary>
        private float MinGradePercent;
        /// <summary>Maximum of all GradePercent in PathChartPoints</summary>
        private float MaxGradePercent;
        /// <summary>Minimum of all Curvature in PathChartPoints</summary>
        private float MinCurvature;
        /// <summary>Maximum of all Curvature in PathChartPoints</summary>
        private float MaxCurvature;


        private TrackSectionsFile tsectionDat;
        private TrackDB trackDB;
        private TrackItemManager trackItems;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routeData">The data of the route (track database, track section information, ...)</param>
        public PathChartData(ORTS.TrackViewer.Drawing.RouteData routeData)
        {
            this.trackDB = routeData.TrackDB;
            this.tsectionDat = routeData.TsectionDat;
            trackItems = new TrackItemManager(routeData);
        }

        #region Update the whole path
        /// <summary>
        /// Update (or fully recalculate) the data for charting the path
        /// </summary>
        /// <param name="trainpath">The train path for which to store chart data</param>
        public void Update(Trainpath trainpath)
        {
            this.PathName = trainpath.PathName;
            var localPathChartPoints = new List<PathChartPoint>();
            DistanceAlongPath = new Dictionary<TrainpathNode, double>();
            ResetAllMinMax();

            TrainpathNode node = trainpath.FirstNode;
            float lastDistance = 0;

            while (node != null)
            {
                DistanceAlongPath[node] = lastDistance;
                IEnumerable<PathChartPoint> additionalPoints = DetermineChartPoints(node);

                foreach (PathChartPoint relativePoint in additionalPoints)
                {
                    PathChartPoint absolutePoint = new PathChartPoint(relativePoint, lastDistance);
                    lastDistance += relativePoint.DistanceAlongNextSection;
                    AddPoint(localPathChartPoints, absolutePoint);
                }

                node = node.NextMainNode;
            }

            //todo possibly we need to change the information on the last node, and copy e.g. the grade from the last-but-one node

            PathChartPoints = localPathChartPoints;
            StoreAllMinMax();

        }

        /// <summary>
        /// Reset all min and max values so we can update it during the creation of the list of points
        /// </summary>
        private void ResetAllMinMax()
        {
            this.MinCurvature = float.MaxValue;
            this.MinDistanceAlongPath = float.MaxValue;
            this.MinGradePercent = float.MaxValue;
            this.MinHeightM = float.MaxValue;
            this.MaxCurvature = float.MinValue;
            this.MaxDistanceAlongPath = float.MinValue;
            this.MaxGradePercent = float.MinValue;
            this.MaxHeightM = float.MinValue;
        }

        /// <summary>
        /// Add a point to the list and update all Min/Max values
        /// </summary>
        private void AddPoint(List<PathChartPoint> localPathChartPoints, PathChartPoint newPoint)
        {
            this.MinDistanceAlongPath = Math.Min(this.MinDistanceAlongPath, newPoint.DistanceAlongPath);
            this.MaxDistanceAlongPath = Math.Max(this.MaxDistanceAlongPath, newPoint.DistanceAlongPath);
            this.MinHeightM = Math.Min(this.MinHeightM, newPoint.HeightM);
            this.MaxHeightM = Math.Max(this.MaxHeightM, newPoint.HeightM);
            this.MinGradePercent = Math.Min(this.MinGradePercent, newPoint.GradePercent);
            this.MaxGradePercent = Math.Max(this.MaxGradePercent, newPoint.GradePercent);
            this.MinCurvature = Math.Min(this.MinCurvature, newPoint.Curvature);
            this.MaxCurvature = Math.Max(this.MaxCurvature, newPoint.Curvature);

            localPathChartPoints.Add(newPoint);
        }

        /// <summary>
        /// Store all the maxima and minima that we found in the dedicated points
        /// </summary>
        private void StoreAllMinMax()
        {
            this.PointWithMaxima = new PathChartPoint(new PathChartPoint(this.MaxHeightM, this.MaxCurvature, this.MaxGradePercent / 100, 0), this.MaxDistanceAlongPath);
            this.PointWithMinima = new PathChartPoint(new PathChartPoint(this.MinHeightM, this.MinCurvature, this.MinGradePercent / 100, 0), this.MinDistanceAlongPath);
        }
        #endregion

        #region Update from one trainpath node to the next
        /// <summary>
        /// Determine the ChartPoints from the startNode (included) until but not including the endNode=startNode.NextMainNode
        /// Each tracksection-begin should be a new point
        /// </summary>
        /// <param name="thisNode">The node to start with</param>
        /// <remarks>The assumption is that the two trainpath nodes only have a single tracknode connecting them</remarks>
        /// <returns>At least one new chart point</returns>
        private IEnumerable<PathChartPoint> DetermineChartPoints(TrainpathNode thisNode)
        {
            // The track consists of a number of sections. These sections might be along the direction we are going in (isForward) or not
            // The first point (belonging to currentNode) is the first we return, and possibly the only one.
            // Any new  points we are going to add are all at the boundaries of sections
            // From the track database we get the (height) data only at start of a section. 
            // If we are moving forward the height at the section boundary is coming from the section just after the boundary
            // If we are moving reverse the height at the section boundary is coming from the section just before the boundary;
            var newPoints = new List<PathChartPoint>();
            TrainpathNode nextNode = thisNode.NextMainNode;

            if (nextNode == null)
            {
                PathChartPoint singlePoint = new PathChartPoint(thisNode);
                newPoints.Add(singlePoint);
                return newPoints;
            }

            if (thisNode.IsBroken || nextNode.IsBroken || thisNode.NextMainTvnIndex == -1)
            {
                PathChartPoint singlePoint = CreateBrokenChartPoint(thisNode, nextNode);
                newPoints.Add(singlePoint);
                return newPoints;
            }

            TrackNode tn = trackDB.TrackNodes[thisNode.NextMainTvnIndex];

            TrVectorNode vectorNode = tn.TrVectorNode;
            var trackItemsInTracknode = trackItems.GetItemsInTracknode(tn);


            bool isForward;
            bool isReverse; // only dummy out argument
            int tvsiStart;
            int tvsiStop;
            float sectionOffsetStart;
            float sectionOffsetStop;

            DetermineSectionDetails(thisNode, nextNode, tn, out isForward, out tvsiStart, out sectionOffsetStart);
            DetermineSectionDetails(nextNode, thisNode, tn, out isReverse, out tvsiStop, out sectionOffsetStop);

            float height;
            if (isForward)
            {
                // We add points in reverse order, so starting at the last section and its index
                float sectionOffsetNext = sectionOffsetStop;
                for (int tvsi = tvsiStop; tvsi > tvsiStart; tvsi--)
                {
                    height = vectorNode.TrVectorSections[tvsi].Y;
                    AddPointAndTrackItems(newPoints, vectorNode, trackItemsInTracknode, isForward, height, tvsi, 0, sectionOffsetNext);

                    sectionOffsetNext = SectionLengthAlongTrack(tn, tvsi - 1);
                }

                //Also works in case this is the only point we are adding
                height = thisNode.Location.Location.Y;
                AddPointAndTrackItems(newPoints, vectorNode, trackItemsInTracknode, isForward, height, tvsiStart, sectionOffsetStart, sectionOffsetNext);
            }
            else
            {   //reverse
                // We add points in reverse order, so starting at the first section and its index
                float sectionOffsetNext = sectionOffsetStop;
                for (int tvsi = tvsiStop; tvsi < tvsiStart; tvsi++)
                {
                    // The height needs to come from the end of the section, so the where the next section starts. And we only know the height at the start.
                    height = vectorNode.TrVectorSections[tvsi + 1].Y;
                    AddPointAndTrackItems(newPoints, vectorNode, trackItemsInTracknode, isForward, height, tvsi, sectionOffsetNext, SectionLengthAlongTrack(tn, tvsi));

                    sectionOffsetNext = 0;
                }

                //Also works in case this is the only point we are adding
                height = thisNode.Location.Location.Y;
                AddPointAndTrackItems(newPoints, vectorNode, trackItemsInTracknode, isForward, height, tvsiStart, sectionOffsetNext, sectionOffsetStart);
            }
            newPoints.Reverse();
            return newPoints;
        }

        private PathChartPoint CreateBrokenChartPoint(TrainpathNode thisNode, TrainpathNode nextNode)
        {
            float height = thisNode.Location.Location.Y;
            float distance = (float)Math.Sqrt(WorldLocation.GetDistanceSquared(thisNode.Location, nextNode.Location));
            float heightOther = nextNode.Location.Location.Y;
            float grade = (heightOther - height) / distance;
            float curvature = 0;
            PathChartPoint brokenPoint = new PathChartPoint(height, curvature, grade, distance);
            return brokenPoint;
        }

        /// <summary>
        /// From section information create a point for charting the path, and add it to newPoints.
        /// In case there are track items in this particular section, add those also (starting with the last item as seen from the direction of the path)
        /// </summary>
        /// <param name="newPoints">The list to which to add the point</param>
        /// <param name="vectorNode">The vectorNode to use for curvature and grade</param>
        /// <param name="trackItems">A list of track items in this vector tracknode</param>
        /// <param name="isForward">Is the path in the same direction as the tracknode</param>
        /// <param name="height">Height to store in the point</param>
        /// <param name="tvsi">The section index in the track vector node</param>
        /// <param name="sectionOffsetStart">Offset of the start of this section (in forward direction of track, not of path)</param>
        /// <param name="sectionOffsetEnd">Offset of the end of this section (in forward direction of track, not of path)</param>
        private void AddPointAndTrackItems(List<PathChartPoint> newPoints, TrVectorNode vectorNode, IEnumerable<ChartableTrackItem> trackItems,
            bool isForward, float height, int tvsi, float sectionOffsetStart, float sectionOffsetEnd)
        {
            //Note, we are adding points in in reverse direction

            var additionalPoints = new List<PathChartPoint>();

            // not a percentage. We can safely assume the pitch is small enough so we do not to take tan(pitch)
            float gradeFromPitch = -vectorNode.TrVectorSections[tvsi].AX * (isForward ? 1 : -1);
            float curvature = GetCurvature(vectorNode, tvsi, isForward);

            List<ChartableTrackItem> items_local = trackItems.ToList();
            if (isForward)
            {
                items_local.Reverse();
            }

            PathChartPoint newPoint;
            foreach (ChartableTrackItem chartableItem in items_local)
            {
                if (chartableItem.TrackVectorSectionIndex == tvsi && sectionOffsetStart <= chartableItem.TrackVectorSectionOffset && chartableItem.TrackVectorSectionOffset < sectionOffsetEnd)
                {
                    if (isForward)
                    {
                        //For forward, we start at the last item in the track
                        newPoint = new PathChartPoint(chartableItem.Height, curvature, gradeFromPitch, sectionOffsetEnd - chartableItem.TrackVectorSectionOffset, chartableItem.ItemText, chartableItem.ItemType);
                        sectionOffsetEnd = chartableItem.TrackVectorSectionOffset;
                    }
                    else
                    {
                        //For reverse, we have to swap forward and reverse speed limits
                        ChartableTrackItemType itemType =
                            chartableItem.ItemType == ChartableTrackItemType.SpeedLimitForward ? ChartableTrackItemType.SpeedLimitReverse :
                            chartableItem.ItemType == ChartableTrackItemType.SpeedLimitReverse ? ChartableTrackItemType.SpeedLimitForward :
                            chartableItem.ItemType;
                        //For reverse, we start at the first item in the track
                        newPoint = new PathChartPoint(chartableItem.Height, curvature, gradeFromPitch, chartableItem.TrackVectorSectionOffset - sectionOffsetStart, chartableItem.ItemText, itemType);
                        sectionOffsetStart = chartableItem.TrackVectorSectionOffset;
                    }
                    additionalPoints.Add(newPoint);
                }
            }

            newPoint = new PathChartPoint(height, curvature, gradeFromPitch, sectionOffsetEnd - sectionOffsetStart);
            additionalPoints.Add(newPoint);

            newPoints.AddRange(additionalPoints);
        }

        /// <summary>
        /// Get the curvature for the current section index in a vector track node.
        /// </summary>
        /// <param name="vectorNode">The vector track node</param>
        /// <param name="tvsi">The tracknode vector section index in the given verctor track node</param>
        /// <param name="isForward">Is the path in the same direction as the vector track node?</param>
        private float GetCurvature(TrVectorNode vectorNode, int tvsi, bool isForward)
        {
            TrVectorSection tvs = vectorNode.TrVectorSections[tvsi];
            TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);

            float curvature = 0;
            if (trackSection != null) // if it is null, something is wrong but we do not want to crash
            {
                SectionCurve thisCurve = trackSection.SectionCurve;

                if (thisCurve != null)
                {
                    curvature = Math.Sign(thisCurve.Angle) / thisCurve.Radius;
                    if (!isForward)
                    {
                        curvature *= -1;
                    }
                }
            }

            return curvature;
        }

        /// <summary>
        /// Determine where exactly the current trainpath node is on the track node
        /// </summary>
        /// <param name="startNode">The start node</param>
        /// <param name="nextNode">The next node (so also the direction can be understood)</param>
        /// <param name="tn">The tracknode connecting the startNode and nextNode</param>
        /// <param name="isForward">Output: whether going from startNode to nextNode is in the forward direction of the track</param>
        /// <param name="tvsiStart">Output: the track vector section index of where the startNode is</param>
        /// <param name="sectionOffsetStart">Output: the offset in the section (in the direction of the tracknode, not necessarily in the direction from startNode to nextNode)</param>
        private void DetermineSectionDetails(TrainpathNode startNode, TrainpathNode nextNode, TrackNode tn, out bool isForward, out int tvsiStart, out float sectionOffsetStart)
        {
            TrainpathVectorNode currentNodeAsVector = startNode as TrainpathVectorNode;
            TrainpathJunctionNode currentNodeAsJunction = startNode as TrainpathJunctionNode;
            if (currentNodeAsJunction != null)
            {   // we start at a junction node
                isForward = (currentNodeAsJunction.JunctionIndex == tn.JunctionIndexAtStart());
                if (isForward)
                {
                    tvsiStart = 0;
                    sectionOffsetStart = 0;
                }
                else
                {
                    tvsiStart = tn.TrVectorNode.TrVectorSections.Count() - 1;
                    sectionOffsetStart = SectionLengthAlongTrack(tn, tvsiStart);
                }
            }
            else
            {   // we start at a vector node
                isForward = currentNodeAsVector.IsEarlierOnTrackThan(nextNode);
                tvsiStart = currentNodeAsVector.TrackVectorSectionIndex;
                sectionOffsetStart = currentNodeAsVector.TrackSectionOffset;
            }
        }

        /// <summary>
        /// Determine the length of the section along the track.
        /// </summary>
        /// <param name="tn">The current tracknode, which needs to be a vector node</param>
        /// <param name="tvsi">The track vector section index</param>
        private float SectionLengthAlongTrack(TrackNode tn, int tvsi)
        {
            float fullSectionLength;
            TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
            TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);
            if (trackSection == null)
            {
                return 100;  // need to return something. Not easy to recover
            }

            if (trackSection.SectionCurve != null)
            {
                fullSectionLength = trackSection.SectionCurve.Radius * Math.Abs(Microsoft.Xna.Framework.MathHelper.ToRadians(trackSection.SectionCurve.Angle));
            }
            else
            {
                fullSectionLength = trackSection.SectionSize.Length;
            }
            return fullSectionLength;
        }

        /// <summary>
        /// Determine if the path from currentNode to nextNode is in the forward direction of the track (along main path)
        /// </summary>
        private bool DetermineIfForward(TrainpathNode currentNode, TrainpathNode nextNode)
        {   // It would be nice if we could separate this into different classes for vector and junction, but this would mean creating three additional classes for only a few methods
            TrainpathVectorNode currentNodeAsVector = currentNode as TrainpathVectorNode;
            if (currentNodeAsVector != null)
            {
                return currentNodeAsVector.IsEarlierOnTrackThan(nextNode);
            }
            else
            {
                TrainpathJunctionNode currentNodeAsJunction = currentNode as TrainpathJunctionNode;
                return currentNodeAsJunction.JunctionIndex == trackDB.TrackNodes[currentNode.NextMainTvnIndex].JunctionIndexAtStart();
            }
        }

        /// <summary>
        /// Determine the index of the trackvectorsection of the node in the track defined by the track vector node
        /// </summary>
        /// <param name="node">The node for which to determine the track vector section index</param>
        /// <param name="tvn">Track vector index of which we want to find the section</param>
        /// <returns></returns>
        private int DetermineTrackVectorSection(TrainpathNode node, int tvn)
        { // It would be nice if we could separate this into different classes for vector and junction, but this would mean creating three additional classes for only a few methods
            TrainpathVectorNode nodeAsVector = node as TrainpathVectorNode;
            if (nodeAsVector != null)
            {
                return nodeAsVector.TrackVectorSectionIndex;
            }
            else
            {
                TrainpathJunctionNode currentNodeAsJunction = node as TrainpathJunctionNode;
                if (currentNodeAsJunction.JunctionIndex == trackDB.TrackNodes[node.NextMainTvnIndex].JunctionIndexAtStart())
                {
                    return 0;
                }
                else
                {
                    return trackDB.TrackNodes[node.NextMainTvnIndex].TrVectorNode.TrVectorSections.Count() - 1;
                }
            }
        }
        #endregion
    }

    #region PathChartPoint
    /// <summary>
    /// Struct to store charting information for a single point along a path
    /// For information that does not belong to a single point (like the grade), it describes the value for 
    /// the small track part following the point.
    /// </summary>
    [Serializable()]
    public struct PathChartPoint
    {
        /// <summary>The distance along the path from a (not-in-this-class specified) reference along the path (e.g. real path begin)</summary>
        [JsonProperty("DistanceAlongPath")]
        public float DistanceAlongPath;
        /// <summary>The distance along the path from a (not-in-this-class specified) reference along the path (e.g. real path begin)</summary>
        [JsonProperty("DistanceAlongNextSection")]
        public float DistanceAlongNextSection;
        /// <summary>Height of the point (in meters)</summary>
        [JsonProperty("HeightM")]
        public float HeightM;
        /// <summary>Curvature of the upcoming track (0 for straight, otherwise 1/radius with a sign describing which direction it curves)</summary>
        [JsonProperty("Curvature")]
        public float Curvature;
        /// <summary>Average grade in the upcoming part of the path</summary>
        [JsonProperty("GradePercent")]
        public float GradePercent;
        /// <summary>The text of the track item (e.g. name of the station) at this location</summary>
        [JsonProperty("TrackItemText")]
        public string TrackItemText;
        /// <summary>The type of the trackItem</summary>
        [JsonProperty("TrackItemType")]
        public ChartableTrackItemType TrackItemType;

        /// <summary>
        /// Constructor for a first point
        /// </summary>
        /// <param name="node">The node describing where the location of the point is</param>
        public PathChartPoint(TrainpathNode node)
        {
            HeightM = node.Location.Location.Y;
            DistanceAlongPath = 0;
            Curvature = 0;
            GradePercent = 0;
            DistanceAlongNextSection = 0;
            TrackItemText = String.Empty;
            TrackItemType = ChartableTrackItemType.None;
        }

        /// <summary>
        /// Constructor where all information is given externally
        /// </summary>
        /// <param name="curvature">The curvature to store</param>
        /// <param name="height">The height to store</param>
        /// <param name="grade">The grade along the path (raw, so not in percent)</param>
        /// <param name="distanceAlongSection">The distance along the section to store</param>
        /// <param name="itemText">The text to show on an item when drawing</param>
        /// <param name="type">The type of trackitem (if any) at this point</param>
        public PathChartPoint(float height, float curvature, float grade, float distanceAlongSection, string itemText = "", ChartableTrackItemType type = ChartableTrackItemType.None)
        {
            HeightM = height;
            DistanceAlongPath = 0;
            Curvature = curvature;
            GradePercent = grade * 100;
            DistanceAlongNextSection = distanceAlongSection;
            TrackItemText = itemText;
            TrackItemType = type;
        }


        /// <summary>
        /// Constructor from another PathChartPoint, only shifted in distance along the path
        /// </summary>
        /// <param name="sourcePoint">The point to copy from</param>
        /// <param name="distanceShift">Extra distance along the path</param>
        public PathChartPoint(PathChartPoint sourcePoint, float distanceShift)
        {
            HeightM = sourcePoint.HeightM;
            DistanceAlongPath = sourcePoint.DistanceAlongPath + distanceShift;
            Curvature = sourcePoint.Curvature;
            DistanceAlongNextSection = sourcePoint.DistanceAlongNextSection;
            GradePercent = sourcePoint.GradePercent;
            TrackItemText = sourcePoint.TrackItemText;
            TrackItemType = sourcePoint.TrackItemType;
        }

        /// <summary>
        /// Overriding for easy debugging
        /// </summary>
        public override string ToString()
        {
            string basicInfo = string.Format("pathChartPoint {0:F1} {1:F1} {2:F1} {3:F1}% {4:F3} ", this.DistanceAlongPath, this.DistanceAlongNextSection, this.HeightM, this.GradePercent, this.Curvature);
            if (this.TrackItemText == string.Empty)
            {
                return basicInfo;
            }
            return basicInfo + " (" + this.TrackItemText + ")";
        }
    }
    #endregion

    #region TrackItemManager
    /// <summary>
    /// The type of what originally was a track item so we can use it for charting
    /// </summary>
    public enum ChartableTrackItemType
    {
        /// <summary>No item given at all</summary>
        None,
        /// <summary>TrackItem type is a station</summary>
        Station,
        /// <summary>TrackItem type is a milepost (or kilometer type)</summary>
        MilePost,
        /// <summary>TrackItem type is a speedlimit in forward direction</summary>
        SpeedLimitForward,
        /// <summary>TrackItem type is a speedlimit in reverse direction</summary>
        SpeedLimitReverse
    }

    /// <summary>
    /// For each requested tracknode find the track items we want to keep (stations, speed, mile markers, ...) and their location and store this information
    /// </summary>
    class TrackItemManager
    {
        private TrackDB trackDB;
        private TrackSectionsFile tsectionDat;
        private Dictionary<TrackNode, IEnumerable<ChartableTrackItem>> cachedItems;

        private HashSet<TrItem.trItemType> supportedTrackTypes;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routeData">The data needed for the route</param>
        public TrackItemManager(ORTS.TrackViewer.Drawing.RouteData routeData)
        {
            this.trackDB = routeData.TrackDB;
            this.tsectionDat = routeData.TsectionDat;

            cachedItems = new Dictionary<TrackNode, IEnumerable<ChartableTrackItem>>();

            supportedTrackTypes = new HashSet<TrItem.trItemType> {
                TrItem.trItemType.trPLATFORM,
                TrItem.trItemType.trSPEEDPOST,
            };
        }

        /// <summary>
        /// Determine the trackItems and their location in the given tracknode.
        /// </summary>
        /// <param name="tn">The tracknode in which to search for track items</param>
        /// <returns>The list/set of track itemss together with their position information</returns>
        public IEnumerable<ChartableTrackItem> GetItemsInTracknode(TrackNode tn)
        {
            if (cachedItems.ContainsKey(tn))
            {
                return cachedItems[tn];
            }

            List<ChartableTrackItem> tracknodeItems = new List<ChartableTrackItem>();
            TrVectorNode vectorNode = tn.TrVectorNode;
            if (vectorNode.TrItemRefs == null) return tracknodeItems;

            foreach (int trackItemIndex in vectorNode.TrItemRefs)
            {
                TrItem trItem = trackDB.TrItemTable[trackItemIndex];
                if (supportedTrackTypes.Contains(trItem.ItemType))
                {
                    var travellerAtItem = new Traveller(tsectionDat, trackDB.TrackNodes, tn,
                        trItem.TileX, trItem.TileZ, trItem.X, trItem.Z, Traveller.TravellerDirection.Forward);

                    if (travellerAtItem != null)
                    {
                        tracknodeItems.Add(new ChartableTrackItem(trItem, travellerAtItem));
                    }

                }
            }
            tracknodeItems.Sort(new AlongTrackComparer());
            cachedItems[tn] = tracknodeItems;
            return tracknodeItems;
        }

        /// <summary>
        /// Comparer to sort doubles in reverse order.
        /// </summary>
        private class AlongTrackComparer : IComparer<ChartableTrackItem>
        {
            int IComparer<ChartableTrackItem>.Compare(ChartableTrackItem a, ChartableTrackItem b)
            {
                int tvsCompare = a.TrackVectorSectionIndex.CompareTo(b.TrackVectorSectionIndex);
                if (tvsCompare != 0)
                {
                    return tvsCompare;
                }
                else
                {
                    return a.TrackVectorSectionOffset.CompareTo(b.TrackVectorSectionOffset);
                }
            }
        }
    }
    #endregion

    #region ChartableTrackItem
    /// <summary>
    /// Store the text of a trackItem (e.g. station name) as well as its type and its location inside a tracknode
    /// </summary>
    struct ChartableTrackItem
    {
        /// <summary>The text of this item that needs to be shown in a chart</summary>
        public string ItemText;
        /// <summary>The height of the item</summary>
        public float Height;
        /// <summary>The index of the section in the vector tracknode</summary>
        public int TrackVectorSectionIndex;
        /// <summary>The offset (in the forward direction of the tracknode) in the section where the item (marker) is</summary>
        public float TrackVectorSectionOffset;
        /// <summary>The type of item</summary>
        public ChartableTrackItemType ItemType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">The original track item</param>
        /// <param name="travellerAtItem">The traveller located at the location of the track item</param>
        public ChartableTrackItem(TrItem item, Traveller travellerAtItem)
        {
            this.Height = item.Y;
            this.ItemText = string.Empty;
            this.ItemType = ChartableTrackItemType.Station;
            switch (item.ItemType)
            {
                case TrItem.trItemType.trEMPTY:
                    break;
                case TrItem.trItemType.trCROSSOVER:
                    break;
                case TrItem.trItemType.trSIGNAL:
                    break;
                case TrItem.trItemType.trSPEEDPOST:
                    SpeedPostItem speedPost = item as SpeedPostItem;
                    this.ItemText = speedPost.SpeedInd.ToString(System.Globalization.CultureInfo.CurrentCulture);
                    if (speedPost.IsMilePost)
                    {
                        this.ItemType = ChartableTrackItemType.MilePost;
                    }
                    if (speedPost.IsLimit)
                    {
                        float relativeAngle = Microsoft.Xna.Framework.MathHelper.WrapAngle(travellerAtItem.RotY + speedPost.Angle - (float)Math.PI / 2);
                        bool inSameDirection = Math.Abs(relativeAngle) < Math.PI / 2;
                        if (inSameDirection)
                        {
                            this.ItemType = ChartableTrackItemType.SpeedLimitForward;
                        }
                        else
                        {
                            this.ItemType = ChartableTrackItemType.SpeedLimitReverse;
                        }
                    }
                    break;
                case TrItem.trItemType.trPLATFORM:
                    this.ItemText = (item as PlatformItem).Station;
                    this.ItemType = ChartableTrackItemType.Station;
                    break;
                case TrItem.trItemType.trSOUNDREGION:
                    break;
                case TrItem.trItemType.trXING:
                    break;
                case TrItem.trItemType.trSIDING:
                    break;
                case TrItem.trItemType.trHAZZARD:
                    break;
                case TrItem.trItemType.trPICKUP:
                    break;
                case TrItem.trItemType.trCARSPAWNER:
                    break;
                default:
                    break;
            }


            this.TrackVectorSectionIndex = travellerAtItem.TrackVectorSectionIndex;
            var travellerAtSectionStart = new Traveller(travellerAtItem);
            travellerAtSectionStart.MoveInSection(float.MinValue); // Move to begin of section
            this.TrackVectorSectionOffset = travellerAtItem.TrackNodeOffset - travellerAtSectionStart.TrackNodeOffset;

        }
    }
    #endregion
}
