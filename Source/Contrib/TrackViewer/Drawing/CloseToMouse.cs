// COPYRIGHT 2014 by the Open Rails project.
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
using System.Text;
using ORTS.Common;
using MSTS.Formats;

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
    public class CloseToMouse
    {
        protected float closestMouseDistanceSquared;
        public string type;  // type to be used in statusbar

        // Distance (squared) from mouse to the closest item
        public virtual float ClosestMouseDistanceSquared { get { return closestMouseDistanceSquared; } }

        /// <summary>
        /// Reset the distance to far far away, so everything else we see will be closer. reset default type
        /// </summary>
        public virtual void Reset()
        {
            closestMouseDistanceSquared = float.MaxValue;
            type = "none";
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

        /// <summary>
        /// get distance between two world locations not taking the height in account
        /// </summary>
        /// <param name="location1">first location</param>
        /// <param name="location2">second location</param>
        /// <returns>Distance squared</returns>
        /// <remarks>Very similar to WordlLocation.GetDistanceSquared</remarks>
        public static float GetGroundDistanceSquared(WorldLocation location1, WorldLocation location2)
        {
            var dx = location1.Location.X - location2.Location.X;
            var dz = location1.Location.Z - location2.Location.Z;
            dx += 2048 * (location1.TileX - location2.TileX);
            dz += 2048 * (location1.TileZ - location2.TileZ);
            return dx * dx + dz * dz;
        }
 
    }

    /// <summary>
    /// CloseToMouse item specifically for junctions and endnode. But will take any track point
    /// </summary>
    public class CloseToMouseJunctionOrEnd:CloseToMouse
    {
        public TrackNode junctionOrEndNode;
        public override void Reset()
        {
            base.Reset();
            junctionOrEndNode = null;
        }

        public CloseToMouseJunctionOrEnd()
        {
        }

        public CloseToMouseJunctionOrEnd(TrackNode junctionOrEndNode, string type)
        {
            closestMouseDistanceSquared = 0;
            this.junctionOrEndNode = junctionOrEndNode;
            this.type = type;
        }

        /// <summary>
        /// Check wether this trackNode (assumed to be a junction or endnode) is closest to the mouse location
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <param name="mouseLocation">Current mouse location</param>
        /// <param name="junctionOrEndNode">the trackNode that will be stored when it is indeed the closest</param>
        /// <param name="type">The type of item (needed for later printing in statusbar)</param>
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation, TrackNode junctionOrEndNode, string type)
        {
            float distanceSquared = CloseToMouse.GetGroundDistanceSquared(location, mouseLocation);
            if (distanceSquared < closestMouseDistanceSquared)
            {
                closestMouseDistanceSquared = distanceSquared;
                this.junctionOrEndNode = junctionOrEndNode;
                this.type = type;
            }
        }

    }

    /// <summary>
    /// CloseToMouse track item. Stores item as well as its type (name)
    /// </summary>
    public class CloseToMouseItem:CloseToMouse
    {
        public TrItem trItem;

        public CloseToMouseItem()
        {
        }

        public CloseToMouseItem(TrItem item)
        {
            trItem = item;
            closestMouseDistanceSquared = 0;
            type = DrawTrackDB.itemName[item.ItemType];
        }

        /// <summary>
        /// To figure out which track the closest is to the mouse, we have to reset the mouse distance when we start    
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            trItem = null;
       }

        
        /// <summary>
        /// Check wether this track Item is closest to the mouse location
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <param name="mouseLocation">Current mouse location</param>
        /// <param name="trItem">The track Item that will be stored when it is indeed the closest</param>
        /// <param name="type">The type of item (needed for later printing in statusbar)</param>
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation, TrItem trItem)
        {
            float distanceSquared = CloseToMouse.GetGroundDistanceSquared(location, mouseLocation);

            if (distanceSquared < closestMouseDistanceSquared)
            {
                closestMouseDistanceSquared = distanceSquared;
                this.trItem = trItem;
                this.type = DrawTrackDB.itemName[trItem.ItemType];
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
        private TSectionDatFile tsectionDat;
        private SortedList<double, TrackCandidate> sortedTrackCandidates;
        private WorldLocation storedMouseLocation;

        private bool realDistancesAreCalculated = false;

        // The next one is a bit tricky. The problem is that the first culling is done based on the trackvector section location
        // Which is only at one side of the section. So the other end might be quiet far away.
        // Unfortunately, this means that we need to keep track of many candidates to make sure we can calculate the closest one
        // Which is costing performance.
        // The biggest issue is for a long track close to a region with lots junctions and hence small track segments
        private const int maxNumberOfCandidates = 50;

        //Properties
        public TrackNode TrackNode { get { calcRealDistances(); return sortedTrackCandidates.First().Value.trackNode; } }
        public TrVectorSection VectorSection { get { calcRealDistances(); return sortedTrackCandidates.First().Value.vectorSection; } }
        public int TrackVectorSectionIndex { get { calcRealDistances(); return sortedTrackCandidates.First().Value.trackVectorSectionIndex; } }
        public float DistanceAlongTrack { get { calcRealDistances(); return sortedTrackCandidates.First().Value.distanceAlongSection; } }
        public override float ClosestMouseDistanceSquared { get { calcRealDistances(); return (float)sortedTrackCandidates.First().Key; } }

        /// <summary>
        /// Constructor, because we need to store the TsectionDatFile
        /// </summary>
        /// <param name="tsectionDat">The track section Dat file that we can use to calculate the distance to the track</param>
        public CloseToMouseTrack(TSectionDatFile tsectionDat)
        {
            this.tsectionDat = tsectionDat;
        }

        public CloseToMouseTrack(TSectionDatFile tsectionDat, TrackNode tn)
        {
            this.tsectionDat = tsectionDat;
            sortedTrackCandidates = new SortedList<double, TrackCandidate>();
            sortedTrackCandidates.Add(0, new TrackCandidate(tn, null, 0, 0));
            realDistancesAreCalculated = true; // we do not want to calculate distance if we override the highlight
        }

        /// <summary>
        /// To figure out which track the closest is to the mouse, we have to reset the mouse distance when we start    
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            sortedTrackCandidates = new SortedList<double, TrackCandidate>();
            sortedTrackCandidates.Add(float.MaxValue, new TrackCandidate(null, null, 0, 0));
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
        public void CheckMouseDistance(WorldLocation location, WorldLocation mouseLocation, TrackNode trackNode, TrVectorSection vectorSection, int tvsi)
        {
            storedMouseLocation = mouseLocation;
            float distanceSquared = WorldLocation.GetDistanceSquared(location, mouseLocation);
            // to make unique distances becasue the also act as Key
            double distanceSquaredIndexed = ((double)distanceSquared) * (1 + 1e-16 * trackNode.Index);
            if (distanceSquaredIndexed < sortedTrackCandidates.Last().Key)
            {
                if (!sortedTrackCandidates.ContainsKey(distanceSquaredIndexed))
                {
                    sortedTrackCandidates.Add(distanceSquaredIndexed, new TrackCandidate(trackNode, vectorSection, tvsi, 0));
                    if (sortedTrackCandidates.Count > maxNumberOfCandidates)
                    {
                        sortedTrackCandidates.RemoveAt(maxNumberOfCandidates);
                    }
                }
            }
        }

        /// <summary>
        /// Method to calculate, for each of the candidates, the real closest distance (squared) from mouse to track
        /// </summary>
        void calcRealDistances()
        {
            if (realDistancesAreCalculated) return;
            List<double> existingKeys = sortedTrackCandidates.Keys.ToList();
            foreach (double distanceKey in existingKeys)
            {
                TrackCandidate trackCandidate = sortedTrackCandidates[distanceKey];
                if (trackCandidate.trackNode == null) continue;
                TrackSection trackSection = tsectionDat.TrackSections.Get(trackCandidate.vectorSection.SectionIndex);
                DistanceLon distanceLon = (trackSection.SectionCurve == null)
                    ? calcRealDistanceSquaredStraight(trackCandidate.vectorSection, trackSection)
                    : calcRealDistanceSquaredCurved(trackCandidate.vectorSection, trackSection);
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
        /// Calculate the closest distance to a straight track. Any direction, any distance allowed
        /// </summary>
        /// <param name="trackVectorSection">The vectorsection for which we want to know the distance</param>
        /// <param name="trackSection">The corresponding tracksection</param>
        /// <returns>Distance to the track</returns>
        /// <remarks>Partly the same code as in Traveller.cs, but here no culling, and we just want the distance.
        /// The math here is not perfect (it is quite difficult to calculate the distances to a curved line 
        /// for all possibilities) but good enough. The math was designed (in Traveller,cs) to work well for close distances</remarks>
        DistanceLon calcRealDistanceSquaredCurved(TrVectorSection trackVectorSection, TrackSection trackSection)
        {
            float x = storedMouseLocation.Location.X;
            float z = storedMouseLocation.Location.Z;
            x += (storedMouseLocation.TileX - trackVectorSection.TileX) * 2048;
            z += (storedMouseLocation.TileZ - trackVectorSection.TileZ) * 2048;
            float sx = trackVectorSection.X;
            float sz = trackVectorSection.Z;

            // Copied from traveller.cs. Not verified the math.
            // To simplify the math, center around the start of the track section, rotate such that the track section starts out pointing north (+z) and flip so the track curves to the right.
            x -= sx;
            z -= sz;
            MSTSMath.M.Rotate2D(trackVectorSection.AY, ref x, ref z);
            if (trackSection.SectionCurve.Angle < 0)
                x *= -1;

            // Compute distance to curve's center at (radius,0) then adjust to get distance from centerline.
            float dx = x - trackSection.SectionCurve.Radius;
            float lat = (float)Math.Sqrt(dx * dx + z * z) - trackSection.SectionCurve.Radius;

            float radiansAlongCurve = (float)Math.Asin(z / trackSection.SectionCurve.Radius);
            float lon = radiansAlongCurve * trackSection.SectionCurve.Radius;
            float trackSectionLength = DrawTrackDB.GetLength(trackSection);

            if (lon < 0)
            {
                return new DistanceLon(lat * lat + lon * lon, 0);  // distance from one end of the track
            }
            if (lon > trackSectionLength)
            {
                return new DistanceLon(lat * lat + (lon - trackSectionLength) * (lon - trackSectionLength), trackSectionLength); //idem
            }
            return new DistanceLon(lat * lat, lon); // Only lateral distance because to the side of the track.
        }

        /// <summary>
        /// Calculate the closest distance to a straight track. Any direction, any distance allowed
        /// </summary>
        /// <param name="trackVectorSection">The vectorsection for which we want to know the distance</param>
        /// <param name="trackSection">The corresponding tracksection</param>
        /// <returns>Distance to the track</returns>
        /// <remarks>Partly the same code as in Traveller.cs, but here no culling, and we just want the distance</remarks>
        DistanceLon calcRealDistanceSquaredStraight(TrVectorSection trackVectorSection, TrackSection trackSection)
        {
            float x = storedMouseLocation.Location.X;
            float z = storedMouseLocation.Location.Z;
            x += (storedMouseLocation.TileX - trackVectorSection.TileX) * 2048;
            z += (storedMouseLocation.TileZ - trackVectorSection.TileZ) * 2048;
            float sx = trackVectorSection.X;
            float sz = trackVectorSection.Z;

            // Calculate distance along and away from the track centerline.
            float lat, lon;
            MSTSMath.M.Survey(sx, sz, trackVectorSection.AY, x, z, out lon, out lat);
            float trackSectionLength = DrawTrackDB.GetLength(trackSection);
            if (lon < 0)
            {   // distance from one end of the track, place at beginning of section
                return new DistanceLon(lat * lat + lon * lon, 0);
            }

            if (lon > trackSectionLength)
            {   //idem, but now place at end of section
                return new DistanceLon(lat * lat + (lon - trackSectionLength) * (lon - trackSectionLength), trackSectionLength);
            }
            return new DistanceLon(lat * lat, lon); // Only lateral distance because to the side of the track.
        }

    }

    /// <summary>
    /// Struct to store a candidate for the track closest to the mouse, so we can keep an ordered list.
    /// </summary>
    struct TrackCandidate {
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
