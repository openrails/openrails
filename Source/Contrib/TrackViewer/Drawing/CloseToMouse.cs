// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using ORTS.Common;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// Helper classes to store an item/junction/endnode/track that is closest to the current mouse location. 
    /// We can then use it later to highlight that item, to show its details in the statusbar, or use it to determin
    /// the active node/location for the path editor.
    /// All these classes are similar, but too different to merge in one class.
    /// During drawing of items/junctions/tracks one needs to call CheckMouseDistance with the location of the mouse
    /// as well as the location of the item, and with the item itself. The distance (squared) to from item to mouse
    /// is then calculated, and when it is closer than any previously checked item, it is stored together with its distance.
    /// Call Reset to reset the closest distance for a new run.
    /// </summary>
    public abstract class CloseToMouse
    {
        /// <summary>Distance squared from the mouse to the closest item</summary>
        protected float ClosestDistanceSquared { get; set; }
        /// <summary>description to be used in statusbar</summary>
        public string Description { get; protected set; }
        /// <summary>Distance (squared) from mouse to the closest item.</summary>
        public virtual float ClosestMouseDistanceSquared { get { return ClosestDistanceSquared; } }

        /// <summary>
        /// Reset the distance to far far away, so everything else we see will be closer. reset default description
        /// </summary>
        public virtual void Reset()
        {
            ClosestDistanceSquared = float.MaxValue;
            Description = "none";
        }

        /// <summary>
        /// Return whether this item is closer than the other item.
        /// </summary>
        /// <param name="otherItem">The other item to compare to</param>
        /// <returns></returns>
        public bool IsCloserThan(CloseToMouse otherItem)
        {
            return this.ClosestMouseDistanceSquared < otherItem.ClosestMouseDistanceSquared;
        }

    }

    /// <summary>
    /// CloseToMouse item specifically for point-like track-database items like junctions, endnodes and track items.
    /// </summary>
    public abstract class CloseToMousePoint : CloseToMouse
    {
        /// <summary>The index of the original item in whatever table it was defined</summary>
        public abstract uint Index { get; }
        /// <summary>The X-coordinate within a tile of the original item in the track database</summary>
        public abstract float X { get; }
        /// <summary>The Z-coordinate within a tile of the original item in the track database</summary>
        public abstract float Z { get; }
    }

    /// <summary>
    /// CloseToMouse item specifically for junctions and endnode. But will take any track point
    /// </summary>
    public class CloseToMouseJunctionOrEnd : CloseToMousePoint
    {
        /// <summary>Tracknode of the closest junction or end node</summary>
        public TrackNode JunctionOrEndNode { get; private set; }
        /// <summary>The index of the original item in whatever table it was defined</summary>
        public override uint Index { get { return JunctionOrEndNode.Index; } }
        /// <summary>The X-coordinate within a tile of the original item in the track database</summary>
        public override float X { get { return JunctionOrEndNode.UiD.X; } }
        /// <summary>The Z-coordinate within a tile of the original item in the track database</summary>
        public override float Z { get { return JunctionOrEndNode.UiD.Z; } }

        /// <summary>
        /// Reset the calculation of which item (junction) is closest to the mouse
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            JunctionOrEndNode = null;
        }

        /// <summary>
        /// Constructor, creating an empty object
        /// </summary>
        public CloseToMouseJunctionOrEnd()
        {
        }

        /// <summary>
        /// Constructor that immediately sets the closest item (and distance)
        /// </summary>
        /// <param name="junctionOrEndNode">Actual tracknode to store as closest item</param>
        /// <param name="description">  to use for printing out</param>
        public CloseToMouseJunctionOrEnd(TrackNode junctionOrEndNode, string description)
        {
            ClosestDistanceSquared = 0;
            this.JunctionOrEndNode = junctionOrEndNode;
            this.Description = description;
        }

        /// <summary>
        /// Check wether this trackNode (assumed to be a junction or endnode) is closest to the mouse location
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <param name="mouseLocation">Current mouse location</param>
        /// <param name="junctionOrEndNode">the trackNode that will be stored when it is indeed the closest</param>
        /// <param name="description">The type of item (needed for later printing in statusbar)</param>
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation, TrackNode junctionOrEndNode, string description)
        {
            float distanceSquared = WorldLocation.GetDistanceSquared2D(location, mouseLocation);
            if (distanceSquared < ClosestDistanceSquared)
            {
                ClosestDistanceSquared = distanceSquared;
                this.JunctionOrEndNode = junctionOrEndNode;
                this.Description = description;
            }
        }

    }

    /// <summary>
    /// CloseToMouse track item. Stores item as well as its type (name)
    /// </summary>
    public class CloseToMouseItem : CloseToMousePoint
    {
        /// <summary>Link to the item that is closest to the mouse</summary>
        public DrawableTrackItem DrawableTrackItem { get; protected set; }

        /// <summary>The index of the original item in whatever table it was defined</summary>
        public override uint Index { get { return DrawableTrackItem.Index; } }
        /// <summary>The X-coordinate within a tile of the original item in the track database</summary>
        public override float X { get { return worldLocation.Location.X; } }
        /// <summary>The Z-coordinate within a tile of the original item in the track database</summary>
        public override float Z { get { return worldLocation.Location.Z; } }

        /// <summary>The world location of the item that is closest to the mouse</summary>
        private WorldLocation worldLocation;

        /// <summary>
        /// Constructor, creating an empty object
        /// </summary>
        public CloseToMouseItem()
        {
        }

        /// <summary>
        /// Constructor that immediately sets the closest item (and distance)
        /// </summary>
        /// <param name="item">track item to store as closest item</param>
        public CloseToMouseItem(DrawableTrackItem item)
        {
            ClosestDistanceSquared = 0;
            DrawableTrackItem = item;
            Description = DrawableTrackItem.Description;
            worldLocation = DrawableTrackItem.WorldLocation;
        }

        /// <summary>
        /// To figure out which track the closest is to the mouse, we have to reset the mouse distance when we start    
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            DrawableTrackItem = null;
            worldLocation = WorldLocation.None;
        }

        /// <summary>
        /// Check wether this track Item is closest to the mouse location
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <param name="mouseLocation">Current mouse location</param>
        /// <param name="trItem">The track Item that will be stored when it is indeed the closest</param>
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation, DrawableTrackItem trItem)
        {
            float distanceSquared = WorldLocation.GetDistanceSquared2D(location, mouseLocation);

            if (distanceSquared < ClosestDistanceSquared)
            {
                ClosestDistanceSquared = distanceSquared;
                this.DrawableTrackItem = trItem;
                this.worldLocation = location;
                this.Description = trItem.Description;
            }
        }
    }

    /// <summary>
    /// CloseToMouse track. Since tracks are long, we need to do quite a bit more to find the real closest one.
    /// We initially store the distance from mouse to the startpoint of the track section. Because that is fast.
    /// Later we then calculate the real distance for a number of sections
    /// </summary>
    public class CloseToMouseTrack : CloseToMouse
    {
        //Note: 'Last' is the one with shortest distance
        /// <summary>Tracknode that is closest</summary>
        public TrackNode TrackNode { get { CalcRealDistances(); return sortedTrackCandidates.Last().Value.trackNode; } }
        /// <summary>Vectorsection within the tracnode</summary>
        public TrVectorSection VectorSection { get { CalcRealDistances(); return sortedTrackCandidates.Last().Value.vectorSection; } }
        /// <summary>Index of vector section that is closest to the mouse</summary>
        public int TrackVectorSectionIndex { get { CalcRealDistances(); return sortedTrackCandidates.Last().Value.trackVectorSectionIndex; } }
        /// <summary>Distance along the track describing precisely where the mouse is</summary>
        public float DistanceAlongTrack { get { CalcRealDistances(); return sortedTrackCandidates.Last().Value.distanceAlongSection; } }
        /// <summary>Distance (squared) between mouse and closest track location</summary>
        public override float ClosestMouseDistanceSquared { get { CalcRealDistances(); return (float)sortedTrackCandidates.Last().Key; } }

        private TrackSectionsFile tsectionDat;
        private WorldLocation storedMouseLocation;
        private bool realDistancesAreCalculated;

        /// <summary>
        /// Store a finite list of TrackCandidates, sorted by the distance to the mouse location.
        /// We only want to store the closest ones. 
        /// The 'double' we use to store the distance is (distance^2). Distance^2 is used because this makes a convenient absolute measure.
        /// The list is sorted in reverse order: the largest number still in the list is the first one. The reason for that is
        /// that to know whether a new TrackCandidate needs to be added to the list, we need to compare it to the candidate with the largest distance.
        /// The initial implementation used normal ordering and the obvious 'Last'. Profiling showed that this single statement was taking > 25% of total CPU. 
        /// </summary>
        private SortedList<double, TrackCandidate> sortedTrackCandidates;


        /// <summary>
        /// Constructor, because we need to store the TsectionDatFile
        /// </summary>
        /// <param name="tsectionDat">The track section Dat file that we can use to calculate the distance to the track</param>
        public CloseToMouseTrack(TrackSectionsFile tsectionDat)
        {
            this.tsectionDat = tsectionDat;
        }

        /// <summary>
        /// Comparer to sort doubles in reverse order.
        /// </summary>
        private class ReverseDoubleComparer : IComparer<double>
        {
            int IComparer<double>.Compare(double a, double b)
            {
                return Comparer<double>.Default.Compare(b, a);
            }
        }

        /// <summary>
        /// Constructor that immediately sets the closest item (and distance)
        /// </summary>
        /// <param name="tn">Tracknode that will be stored as closest item</param>
        /// <param name="tsectionDat">The track section Dat file that we can use to calculate the distance to the track</param>
        public CloseToMouseTrack(TrackSectionsFile tsectionDat, TrackNode tn)
        {
            this.tsectionDat = tsectionDat;
            sortedTrackCandidates = new SortedList<double, TrackCandidate>(new ReverseDoubleComparer())
            {
                { 0, new TrackCandidate(tn, null, 0, 0) }
            };
            realDistancesAreCalculated = true; // we do not want to calculate distance if we override the highlight
        }

        /// <summary>
        /// To figure out which track the closest is to the mouse, we have to reset the mouse distance when we start    
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            sortedTrackCandidates = new SortedList<double, TrackCandidate>(new ReverseDoubleComparer())
            {
                { float.MaxValue, new TrackCandidate(null, null, 0, 0) }
            };
            realDistancesAreCalculated = false;
        }

        /// <summary>
        /// Check wether this vector node is closest to the mouse location
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <param name="mouseLocation">Current mouse location</param>
        /// <param name="trackNode">The trackNode that will be stored when indeed it is the closest to the mouse location</param>
        /// <param name="vectorSection">the vectorSection that will be stored when indeed it is closest to the mouse location</param>
        /// <param name="tvsi">Current index of the trackvectorsection</param>
        /// <param name="pixelsPerMeter"></param>
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation,
            TrackNode trackNode, TrVectorSection vectorSection, int tvsi, double pixelsPerMeter)
        {
            storedMouseLocation = mouseLocation;
            float distanceSquared = WorldLocation.GetDistanceSquared2D(location, mouseLocation);
            // to make unique distances becasue they also act as Key
            double distanceSquaredIndexed = ((double)distanceSquared) * (1 + 1e-16 * trackNode.Index);
            if (distanceSquaredIndexed < sortedTrackCandidates.First().Key)
            {
                if (!sortedTrackCandidates.ContainsKey(distanceSquaredIndexed))
                {
                    sortedTrackCandidates.Add(distanceSquaredIndexed, new TrackCandidate(trackNode, vectorSection, tvsi, 0));

                    // The next one is a bit tricky. The problem is that the first culling is done based on the trackvector section location
                    // Which is only at one side of the section. So the other end might be quiet far away.
                    // Unfortunately, this means that we need to keep track of many candidates to make sure we can calculate the closest one
                    // Which is costing performance.
                    // The biggest issue is for a long track close to a region with lots junctions and hence small track segments
                    // By making this number zoom dependent, we get good results for big zooms, and not a large
                    // performance penalty for wide views
                    int maxNumberOfCandidates = 50 + (int)(100 * pixelsPerMeter);
                    while (sortedTrackCandidates.Count > maxNumberOfCandidates)
                    {
                        sortedTrackCandidates.RemoveAt(0); // First one has largest distance
                    }
                }
            }
        }

        /// <summary>
        /// Method to calculate, for each of the candidates, the real closest distance (squared) from mouse to track
        /// </summary>
        void CalcRealDistances()
        {
            if (realDistancesAreCalculated) return;
            List<double> existingKeys = sortedTrackCandidates.Keys.ToList();
            foreach (double distanceKey in existingKeys)
            {
                TrackCandidate trackCandidate = sortedTrackCandidates[distanceKey];
                if (trackCandidate.trackNode == null) continue;
                TrackSection trackSection = tsectionDat.TrackSections.Get(trackCandidate.vectorSection.SectionIndex);
                DistanceLon distanceLon = CalcRealDistanceSquared(trackCandidate.vectorSection, trackSection);
                double realDistanceSquared = (double)distanceLon.distanceSquared;

                // Add the trackCandidate to the sorted list with its new distance squared as key
                if (!sortedTrackCandidates.ContainsKey(realDistanceSquared))
                {
                    trackCandidate.distanceAlongSection = distanceLon.lengthAlongTrack;
                    sortedTrackCandidates.Add(realDistanceSquared, trackCandidate);

                }
            }
            realDistancesAreCalculated = true;
        }

        /// <summary>
        /// Calculate the closest distance to a track, as well as the 'longitude' along it.
        /// </summary>
        /// <param name="trackVectorSection">The vectorsection for which we want to know the distance</param>
        /// <param name="trackSection">The corresponding tracksection</param>
        /// <returns>Distance Squared to the track as well as length along track.</returns>
        /// <remarks>Partly the same code as in Traveller.cs, but here no culling, and we just want the distance.
        /// The math here is not perfect (it is quite difficult to calculate the distances to a curved line 
        /// for all possibilities) but good enough. The math was designed (in Traveller.cs) to work well for close distances.
        /// Math is modified to prevent NaN and to combine straight and curved tracks.</remarks>
        DistanceLon CalcRealDistanceSquared(TrVectorSection trackVectorSection, TrackSection trackSection)
        {
            //Calculate the vector from start of track to the mouse
            Vector3 vectorToMouse = new Vector3
            {
                X = storedMouseLocation.Location.X - trackVectorSection.X,
                Z = storedMouseLocation.Location.Z - trackVectorSection.Z
            };
            vectorToMouse.X = (float)(vectorToMouse.X + (storedMouseLocation.TileX - trackVectorSection.TileX) * WorldLocation.TileSize);
            vectorToMouse.Z = (float)(vectorToMouse.Z + (storedMouseLocation.TileZ - trackVectorSection.TileZ) * WorldLocation.TileSize);

            //Now rotate the vector such that a direction along the track is in a direction (x=0, z=1)
            vectorToMouse = Vector3.Transform(vectorToMouse, Matrix.CreateRotationY(-trackVectorSection.AY));

            float lon, lat;
            if (trackSection.SectionCurve == null)
            {
                //Track is straight. In this coordinate system, the distance along track (lon) and orthogonal to track (lat) are easy.
                lon = vectorToMouse.Z;
                lat = vectorToMouse.X;
            }
            else
            {
                // make sure the vector is as if the vector section turns to the left.
                // The center of the curved track is now a (x=-radius, z=0), track starting at (0,0), pointing in positive Z
                if (trackSection.SectionCurve.Angle > 0)
                    vectorToMouse.X *= -1;

                //make vector relative to center of curve. Track now starts at (radius,0)
                vectorToMouse.X += trackSection.SectionCurve.Radius;

                float radiansAlongCurve = (float)Math.Atan2(vectorToMouse.Z, vectorToMouse.X);

                //The following calculations make sense when close to the track. Otherwise they are not necessarily sensible, but at least well-defined.
                // Distance from mouse to circle through track section.
                lat = (float)Math.Sqrt(vectorToMouse.X * vectorToMouse.X + vectorToMouse.Z * vectorToMouse.Z) - trackSection.SectionCurve.Radius;
                lon = radiansAlongCurve * trackSection.SectionCurve.Radius;
            }


            float trackSectionLength = DrawTrackDB.GetLength(trackSection);
            if (lon < 0)
            {   // distance from start of track
                return new DistanceLon(lat * lat + lon * lon, 0);
            }
            if (lon > trackSectionLength)
            {   // distance from end of track
                return new DistanceLon(lat * lat + (lon - trackSectionLength) * (lon - trackSectionLength), trackSectionLength); //idem
            }
            // somewhere along track. Distance is only in lateral direction
            return new DistanceLon(lat * lat, lon);
        }

    }

    /// <summary>
    /// Struct to store a candidate for the track closest to the mouse, so we can keep an ordered list.
    /// </summary>
    struct TrackCandidate
    {
        public TrackNode trackNode;
        public TrVectorSection vectorSection;
        public int trackVectorSectionIndex;  // which section within a trackNode that is a vector node
        public float distanceAlongSection;

        public TrackCandidate(TrackNode node, TrVectorSection section, int tvsi, float lon)
        {
            trackNode = node;
            vectorSection = section;
            trackVectorSectionIndex = tvsi;
            distanceAlongSection = lon;
        }
    }

    /// <summary>
    /// Small struct storing distance to a track (squared) and length along the track (section).
    /// </summary>
    struct DistanceLon
    {
        public float distanceSquared;
        public float lengthAlongTrack;

        public DistanceLon(float distanceSquared, float lengthAlongTrack)
        {
            this.distanceSquared = distanceSquared;
            this.lengthAlongTrack = lengthAlongTrack;
        }
    }
}
