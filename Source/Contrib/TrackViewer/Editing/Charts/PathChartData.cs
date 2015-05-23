// COPYRIGHT 2015 by the Open Rails project.
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
using System.Text;
using System.Windows;
using System.Windows.Media;

using ORTS.Common;
using Orts.Formats.Msts;

namespace ORTS.TrackViewer.Editing.Charts
{
    public class PathChartData
    {
        public IEnumerable<PathChartPoint> PathChartPoints { get; private set; }

        private TSectionDatFile tsectionDat;
        private TrackDB trackDB;

        public PathChartData(ORTS.TrackViewer.Drawing.DrawTrackDB drawTrackDB)
        {
            this.trackDB = drawTrackDB.TrackDB;
            this.tsectionDat = drawTrackDB.TsectionDat;
        }

        public void Update(Trainpath trainpath)
        {
            var localPathChartPoints = new List<PathChartPoint>();

            TrainpathNode node = trainpath.FirstNode;
            var firstPoint = new PathChartPoint(node);
            localPathChartPoints.Add(firstPoint);

            while (node.NextMainNode != null)
            {
                IEnumerable<PathChartPoint> additionalPoints = DetermineChartPoints(node);

                float lastDistance = localPathChartPoints.Last().DistanceAlongPath; 
                foreach (PathChartPoint relativePoint in additionalPoints)
                {
                    PathChartPoint absolutePoint = new PathChartPoint(relativePoint, lastDistance);
                    localPathChartPoints.Add(absolutePoint);
                }
                
                node = node.NextMainNode;
            }

            PathChartPoints = localPathChartPoints;

        }

        /// <summary>
        /// Determine the ChartPoints from the startNode (not included) until and including the endNode=startNode.NextMainNode
        /// Each tracksection-end should be a new point
        /// </summary>
        /// <param name="currentNode">The node to start with</param>
        /// <remarks>The assumption is that the two trainpath nodes only have a single tracknode connecting them</remarks>
        /// <returns>At least one new chart point</returns>
        IEnumerable<PathChartPoint> DetermineChartPoints(TrainpathNode currentNode)
        {
            // The track consists of a number of sections. These sections might be along the direction we are going in (isForward) or not
            // The first point (belonging to currentNode we do not return)
            // The final point is always belonging to the next main node and will always be in the list
            // Any new intermediate points we are going to add are all at the boundaries of sections
            // But we get the (height) data only at start of a section. 
            // If we are moving forward this is information from the section just after the boundary
            // If we are moving reverse then this is information from the section just before the boundary;
            var newPoints = new List<PathChartPoint>();
            TrainpathNode nextNode = currentNode.NextMainNode;

            TrackNode tn = trackDB.TrackNodes[currentNode.NextMainTvnIndex];
            TrVectorNode vectorNode = tn.TrVectorNode;

            bool isForward;
            bool isReverse; // only dummy
            int tvsiStart;
            int tvsiStop;
            float sectionOffsetStart;
            float sectionOffsetStop;

            DetermineSectionDetails(currentNode, nextNode, tn, out isForward, out tvsiStart, out sectionOffsetStart);
            DetermineSectionDetails(nextNode, currentNode, tn, out isReverse, out tvsiStop,  out sectionOffsetStop);

            float distance = 0;
            float previousDistance = distance;
            //float previousHeight = currentNode.Location.Location.Y;
            PathChartPoint newPoint;
            float gradeFromPitch;
            if (isForward)
            {
                float sectionOffset = sectionOffsetStart;
                for (int tvsi = tvsiStart; tvsi < tvsiStop; tvsi++)
                {   
                    //from next section:
                    float height = vectorNode.TrVectorSections[tvsi + 1].Y;

                    //from this section:
                    distance += SectionLengthAlongTrack(tn, tvsi) - sectionOffset;
                    gradeFromPitch = -vectorNode.TrVectorSections[tvsi].AX; // not a percentage. We can safely assume the pitch is small enough so we do not to take tan(pitch)
                    newPoint = new PathChartPoint(height, distance, GetCurvature(vectorNode, tvsi, isForward), gradeFromPitch);
                    //var newPoint = new PathChartPoint(height, distance, GetCurvature(vectorNode, tvsi), previousHeight, previousDistance);
                    newPoints.Add(newPoint);
                    
                    //previousHeight = height;
                    previousDistance = distance;
                    sectionOffset = 0; // the new section is completely present, so it starts at 0 (forward).
                }

                distance += sectionOffsetStop;
                gradeFromPitch = -vectorNode.TrVectorSections[tvsiStop].AX; // not a percentage. We can safely assume the pitch is small enough so we do not to take tan(pitch)
                newPoint = new PathChartPoint(nextNode.Location.Location.Y, distance, GetCurvature(vectorNode, tvsiStop, isForward), gradeFromPitch);
                //newPoint = new PathChartPoint(nextNode.Location.Location.Y, distance, GetCurvature(vectorNode, tvsiStop), previousHeight, previousDistance);
                newPoints.Add(newPoint);

            }
            else
            {   //reverse
                float sectionOffset = sectionOffsetStart;
                for (int tvsi = tvsiStart; tvsi > tvsiStop; tvsi--)
                {
                    //from next section:
                    
                    //from this section:
                    float height = vectorNode.TrVectorSections[tvsi].Y;
                    distance += sectionOffset;
                    gradeFromPitch = +vectorNode.TrVectorSections[tvsi].AX; // not a percentage. We can safely assume the pitch is small enough so we do not to take tan(pitch)
                    newPoint = new PathChartPoint(height, distance, GetCurvature(vectorNode, tvsi, isForward), gradeFromPitch);
                    //var newPoint = new PathChartPoint(height, distance, GetCurvature(vectorNode, tvsi), previousHeight, previousDistance);
                    newPoints.Add(newPoint);

                    //previousHeight = height;
                    previousDistance = distance;
                    sectionOffset = SectionLengthAlongTrack(tn, tvsi); // the new section is completely present, but reverse, so calculating from the end
                }

                distance += SectionLengthAlongTrack(tn, tvsiStop) - sectionOffsetStop;
                gradeFromPitch = +vectorNode.TrVectorSections[tvsiStop].AX; 
                newPoint = new PathChartPoint(nextNode.Location.Location.Y, distance, GetCurvature(vectorNode, tvsiStop, isForward), gradeFromPitch);
                //newPoint = new PathChartPoint(nextNode.Location.Location.Y, distance, GetCurvature(vectorNode, tvsiStop), previousHeight, previousDistance);
                newPoints.Add(newPoint);


            }
            return newPoints;

            //todo usage model for showing or not showing is not OK yet
        }

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

        private void DetermineSectionDetails(TrainpathNode currentNode, TrainpathNode nextNode, TrackNode tn, out bool isForward, out int tvsiStart, out float sectionOffsetStart)
        {
            TrainpathVectorNode currentNodeAsVector = currentNode as TrainpathVectorNode;
            TrainpathJunctionNode currentNodeAsJunction = currentNode as TrainpathJunctionNode;
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
            TrainpathVectorNode nodeAsVector =  node as TrainpathVectorNode;
            if (nodeAsVector != null)
            {
                return nodeAsVector.TrackVectorSectionIndex;
            }
            else
            {
                TrainpathJunctionNode currentNodeAsJunction = node as TrainpathJunctionNode;
                if (currentNodeAsJunction.JunctionIndex == trackDB.TrackNodes[node.NextMainTvnIndex].JunctionIndexAtStart()) {
                    return 0;
                }
                else{
                    return trackDB.TrackNodes[node.NextMainTvnIndex].TrVectorNode.TrVectorSections.Count() - 1;
                }
            }
        }

    }




    /// <summary>
    /// Struct to store charting information for a single point along a path
    /// For information that does not belong to a single point (like the grade), it describes the value for 
    /// the small track part preceding the point.
    /// </summary>
    public struct PathChartPoint
    {
        /// <summary>The distance along the path from a (not-in-this-class specified reference along the path (e.g. real path begin)</summary>
        public float DistanceAlongPath;
        /// <summary>Height of the point (in meters)</summary>
        public float HeightM;
        /// <summary>Curvature of the track (0 for straight, otherwise 1/radius)</summary>
        public float Curvature;
        /// <summary>Average grade in the previous part of the path</summary>
        public float GradePercent;

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
        }

        /// <summary>
        /// Constructor where both the details of this point, as well as some information of the previous point are given
        /// </summary>
        /// <param name="curvature">The curvature to store</param>
        /// <param name="distance">The distance along the path to store</param>
        /// <param name="height">The height to store</param>
        /// <param name="previousDistance">The distance along the path of the previous point used to calculate the grade</param>
        /// <param name="previousHeight">The height of the previous point used to calculate the grade</param>
        public PathChartPoint(float height, float distance, float curvature, float previousHeight, float previousDistance)
        {
            HeightM = height;
            DistanceAlongPath = distance;
            Curvature = curvature;
            GradePercent = 100 * (this.HeightM - previousHeight) / (this.DistanceAlongPath - previousDistance);
        }

        public PathChartPoint(float height, float distance, float curvature, float grade)
        {
            HeightM = height;
            DistanceAlongPath = distance;
            Curvature = curvature;
            GradePercent = grade*100;
        }


        /// <summary>
        /// Constructor from another ChartPoint, only shifted in distance along the path
        /// </summary>
        /// <param name="sourcePoint">The point to copy from</param>
        /// <param name="distanceShift">Extra distance along the path</param>
        public PathChartPoint(PathChartPoint sourcePoint, float distanceShift)
        {
            HeightM = sourcePoint.HeightM;
            DistanceAlongPath = sourcePoint.DistanceAlongPath + distanceShift;
            Curvature = sourcePoint.Curvature;
            GradePercent = sourcePoint.GradePercent;
        }

        /// <summary>
        /// Overriding for easy debugging
        /// </summary>
        public override string ToString()
        {
            return string.Format("pathChartPoint {0:F1} {1:F1} {2:F1} {3:F3}", this.DistanceAlongPath, this.HeightM, this.GradePercent, this.Curvature);
        }
    }

}
