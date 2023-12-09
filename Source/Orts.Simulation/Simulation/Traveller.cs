// COPYRIGHT 2012, 2013 by the Open Rails project.
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
using Orts.Simulation.AIs;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Simulation
{
    /// <summary>
    /// A traveller that represents a specific location and direction on a track node databse. Think of it like a virtual truck or bogie that can travel along the track or a virtual car that can travel along the road.
    /// </summary>
    public class Traveller
    {
        public enum TravellerDirection : byte
        {
            Backward = 0,
            Forward = 1,
        }

        // Maximum distance beyond the ends of the track we'll allow for initialization.
        const float InitErrorMargin = 0.5f;

        // If a car has some overhang, than it will be offset toward the center of curvature
        // and won't be right along the center line.  I'll have to add some allowance for this
        // and accept a hit if it is within 2.5 meters of the center line - this was determined
        // experimentally to match MSTS's 'capture range'.
        const float MaximumCenterlineOffset = 2.5f;

        TrackSectionsFile TSectionDat;
        TrackNode[] TrackNodes;
        TravellerDirection direction = TravellerDirection.Forward;
        float trackOffset; // Offset into track (vector) section; meters for straight sections, radians for curved sections.
        TrackNode trackNode;
        TrVectorSection trackVectorSection;
        TrackSection trackSection;

        // Location and directionVector are only valid if locationSet == true.
        bool locationSet;
        WorldLocation location = new WorldLocation();
        Vector3 directionVector;

        // Length and offset only valid if lengthSet = true.
        bool lengthSet;
        float trackNodeLength;
        float trackNodeOffset;

        public WorldLocation WorldLocation { get { if (!locationSet) SetLocation(); return new WorldLocation(location); } }
        public int TileX { get { if (!locationSet) SetLocation(); return location.TileX; } }
        public int TileZ { get { if (!locationSet) SetLocation(); return location.TileZ; } }
        public Vector3 Location { get { if (!locationSet) SetLocation(); return location.Location; } }
        public float X { get { if (!locationSet) SetLocation(); return location.Location.X; } }
        public float Y { get { if (!locationSet) SetLocation(); return location.Location.Y; } }
        public float Z { get { if (!locationSet) SetLocation(); return location.Location.Z; } }
        public TravellerDirection Direction
        {
            get
            {
                return direction;
            }
            set
            {
                if (value != direction)
                {
                    direction = value;
                    if (locationSet)
                    {
                        directionVector.X *= -1;
                        directionVector.Y += MathHelper.Pi;
                        directionVector.X = MathHelper.WrapAngle(directionVector.X);
                        directionVector.Y = MathHelper.WrapAngle(directionVector.Y);
                    }
                    if (lengthSet)
                        trackNodeOffset = trackNodeLength - trackNodeOffset;
                }
            }
        }
        public float RotY { get { if (!locationSet) SetLocation(); return directionVector.Y; } }
        public TrackNode TN { get { return trackNode; } }

        /// <summary>
        /// Returns the index of the current track node in the database.
        /// </summary>
        public int TrackNodeIndex { get; private set; }
        /// <summary>
        /// Returns the index of the current track vector section (individual straight or curved section of track) in the current track node.
        /// </summary>
        public int TrackVectorSectionIndex { get; private set; }
        /// <summary>
        /// Returns the length of the current track node in meters.
        /// </summary>
        public float TrackNodeLength { get { if (!lengthSet) SetLength(); return trackNodeLength; } }
        /// <summary>
        /// Returns the distance down the current track node in meters, based on direction of travel.
        /// </summary>
        public float TrackNodeOffset { get { if (!lengthSet) SetLength(); return trackNodeOffset; } }
        /// <summary>
        /// Returns whether this traveller is currently on a (section of) track node (opposed to junction, end of line).
        /// </summary>
        public bool IsTrack { get { return trackNode.TrVectorNode != null; } }
        /// <summary>
        /// Returns whether this traveller is currently on a junction node.
        /// </summary>
        public bool IsJunction { get { return trackNode.TrJunctionNode != null; } }
        /// <summary>
        /// Returns whether this traveller is currently on a end of line node.
        /// </summary>
        public bool IsEnd { get { return trackNode.TrEndNode; } }
        /// <summary>
        /// Returns whether this traveller is currently on a section of track which is curved.
        /// </summary>
        public bool IsTrackCurved { get { return IsTrack && trackSection != null && trackSection.SectionCurve != null; } }
        /// <summary>
        /// Returns whether this traveller is currently on a section of track which is straight.
        /// </summary>
        public bool IsTrackStraight { get { return IsTrack && (trackSection == null || trackSection.SectionCurve == null); } }
        /// <summary>
        /// Returns the pin index number, for the current track node, identifying the route travelled into this track node.
        /// </summary>
        public int JunctionEntryPinIndex { get; private set; }

        Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes)
        {
            if (tSectionDat == null) throw new ArgumentNullException("tSectionDat");
            if (trackNodes == null) throw new ArgumentNullException("trackNodes");
            TSectionDat = tSectionDat;
            TrackNodes = trackNodes;
        }
        /// <summary>
        /// Creates a traveller on the starting point of a path, in the direction of the path
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="aiPath">The path used to determine travellers location and direction</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, AIPath aiPath)
            : this(tSectionDat, trackNodes, aiPath.FirstNode.Location)
        {
            AIPathNode nextNode = aiPath.FirstNode.NextMainNode; // assumption is that all paths have at least two points.

            // get distance forward
            float fwdist = this.DistanceTo(nextNode.Location);

            // reverse train, get distance backward
            this.ReverseDirection();
            float bwdist = this.DistanceTo(nextNode.Location);

            // check which way exists or is shorter (in case of loop)
            // remember : train is now facing backward !

            if (bwdist < 0 || (fwdist > 0 && bwdist > fwdist)) // no path backward or backward path is longer
                this.ReverseDirection();
        }

        /// <summary>
        /// Creates a traveller starting at a specific location, facing with the track node.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="loc">Starting world location</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, WorldLocation loc)
            : this(tSectionDat, trackNodes, loc.TileX, loc.TileZ, loc.Location.X, loc.Location.Z)
        {
        }

        /// <summary>
        /// Creates a traveller starting at a specific location, facing with the track node.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="tileX">Starting tile coordinate.</param>
        /// <param name="tileZ">Starting tile coordinate.</param>
        /// <param name="x">Starting coordinate.</param>
        /// <param name="z">Starting coordinate.</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, int tileX, int tileZ, float x, float z)
            : this(tSectionDat, trackNodes)
        {
            List<TrackNodeCandidate> candidates = new List<TrackNodeCandidate>();
            WorldLocation loc = new WorldLocation(tileX, tileZ, x, 0, z);

            // first find all tracknodes that are close enough
            for (var tni = 0; tni < TrackNodes.Length; tni++)
            {
                TrackNodeCandidate candidate = TryTrackNode(tni, loc, TSectionDat, TrackNodes);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidDataException(String.Format("{0} could not be found in the track database.", new WorldLocation(tileX, tileZ, x, 0, z)));
            }

            // find the best one.
            TrackNodeCandidate bestCandidate = candidates.OrderBy(cand => cand.distanceToTrack).First();

            InitFromCandidate(bestCandidate);
        }

        /// <summary>
        /// Creates a traveller starting at a specific location, facing in the specified direction.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="tileX">Starting tile coordinate.</param>
        /// <param name="tileZ">Starting tile coordinate.</param>
        /// <param name="x">Starting coordinate.</param>
        /// <param name="z">Starting coordinate.</param>
        /// <param name="direction">Starting direction.</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, int tileX, int tileZ, float x, float z, TravellerDirection direction)
            : this(tSectionDat, trackNodes, tileX, tileZ, x, z)
        {
            Direction = direction;
        }

        /// <summary>
        /// Creates a traveller starting at the beginning of the specified track node, facing with the track node.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="startTrackNode">Starting track node.</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode)
            : this(tSectionDat, trackNodes)
        {
            if (startTrackNode == null) throw new ArgumentNullException("startTrackNode");
            var startTrackNodeIndex = Array.IndexOf(trackNodes, startTrackNode);
            if (startTrackNodeIndex == -1) throw new ArgumentException("Track node is not in track nodes array.", "startTrackNode");
            if (startTrackNode.TrVectorNode == null) throw new ArgumentException("Track node is not a vector node.", "startTrackNode");
            if (startTrackNode.TrVectorNode.TrVectorSections == null) throw new ArgumentException("Track node has no vector section data.", "startTrackNode");
            if (startTrackNode.TrVectorNode.TrVectorSections.Length == 0) throw new ArgumentException("Track node has no vector sections.", "startTrackNode");
            var tvs = startTrackNode.TrVectorNode.TrVectorSections[0];
            if (!InitTrackNode(startTrackNodeIndex, tvs.TileX, tvs.TileZ, tvs.X, tvs.Z))
            {
                if (TrackSections.MissingTrackSectionWarnings == 0)
                    throw new InvalidDataException(String.Format("Track node {0} could not be found in the track database.", startTrackNode.UiD));
                else
                {
                    throw new MissingTrackNodeException();
                }
            }
        }

        /// <summary>
        /// Creates a traveller starting at a specific location within a specified track node, facing with the track node.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="startTrackNode">Starting track node.</param>
        /// <param name="tileX">Starting tile coordinate.</param>
        /// <param name="tileZ">Starting tile coordinate.</param>
        /// <param name="x">Starting coordinate.</param>
        /// <param name="z">Starting coordinate.</param>
        Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode, int tileX, int tileZ, float x, float z)
            : this(tSectionDat, trackNodes)
        {
            if (startTrackNode == null) throw new ArgumentNullException("startTrackNode");
            var startTrackNodeIndex = Array.IndexOf(trackNodes, startTrackNode);
            if (startTrackNodeIndex == -1) throw new ArgumentException("Track node is not in track nodes array.", "startTrackNode");
            if (!InitTrackNode(startTrackNodeIndex, tileX, tileZ, x, z))
            {
                if (startTrackNode.TrVectorNode == null) throw new ArgumentException("Track node is not a vector node.", "startTrackNode");
                if (startTrackNode.TrVectorNode.TrVectorSections == null) throw new ArgumentException("Track node has no vector section data.", "startTrackNode");
                if (startTrackNode.TrVectorNode.TrVectorSections.Length == 0) throw new ArgumentException("Track node has no vector sections.", "startTrackNode");
                var tvs = startTrackNode.TrVectorNode.TrVectorSections[0];
                if (!InitTrackNode(startTrackNodeIndex, tvs.TileX, tvs.TileZ, tvs.X, tvs.Z))
                {
                    if (TrackSections.MissingTrackSectionWarnings == 0)
                        throw new InvalidDataException(String.Format("Track node {0} could not be found in the track database.", startTrackNode.UiD));
                    else
                    {
                        throw new MissingTrackNodeException();
                    }
                    
                }

                // Figure out which end of the track node is closest and use that.
                var target = new WorldLocation(tileX, tileZ, x, 0, z);
                var startDistance = WorldLocation.GetDistance2D(WorldLocation, target).Length();
                Direction = TravellerDirection.Backward;
                NextTrackVectorSection(startTrackNode.TrVectorNode.TrVectorSections.Length - 1);
                var endDistance = WorldLocation.GetDistance2D(WorldLocation, target).Length();
                if (startDistance < endDistance)
                {
                    Direction = TravellerDirection.Forward;
                    NextTrackVectorSection(0);
                }
            }
        }

        /// <summary>
        /// Creates a traveller starting at a specific location within a specified track node, facing in the specified direction.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="startTrackNode">Starting track node.</param>
        /// <param name="tileX">Starting tile coordinate.</param>
        /// <param name="tileZ">Starting tile coordinate.</param>
        /// <param name="x">Starting coordinate.</param>
        /// <param name="z">Starting coordinate.</param>
        /// <param name="direction">Starting direction.</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode, int tileX, int tileZ, float x, float z, TravellerDirection direction)
            : this(tSectionDat, trackNodes, startTrackNode, tileX, tileZ, x, z)
        {
            Direction = direction;
        }

        /// <summary>
        /// Creates a copy of another traveller, starting in the same location and with the same direction.
        /// </summary>
        /// <param name="copy">The other traveller to copy.</param>
        public Traveller(Traveller copy)
        {
            if (copy == null) throw new ArgumentNullException("copy");
            Copy(copy);
        }

        /// <summary>
        /// Creates a copy of another traveller, starting in the same location but with the specified change of direction.
        /// </summary>
        /// <param name="copy">The other traveller to copy.</param>
        /// <param name="reversed">Specifies whether to go the same direction as the <paramref name="copy"/> (Forward) or flip direction (Backward).</param>
        public Traveller(Traveller copy, TravellerDirection reversed)
            : this(copy)
        {
            if (reversed == TravellerDirection.Backward)
                Direction = Direction == TravellerDirection.Forward ? TravellerDirection.Backward : TravellerDirection.Forward;
        }

        /// <summary>
        /// Creates a traveller from persisted data.
        /// </summary>
        /// <param name="tSectionDat">Provides vector track sections.</param>
        /// <param name="trackNodes">Provides track nodes.</param>
        /// <param name="inf">Reader to read persisted data from.</param>
        public Traveller(TrackSectionsFile tSectionDat, TrackNode[] trackNodes, BinaryReader inf)
            : this(tSectionDat, trackNodes)
        {
            locationSet = lengthSet = false;
            direction = (TravellerDirection)inf.ReadByte();
            trackOffset = inf.ReadSingle();
            TrackNodeIndex = inf.ReadInt32();
            trackNode = TrackNodes[TrackNodeIndex];
            if (IsTrack)
            {
                TrackVectorSectionIndex = inf.ReadInt32();
                trackVectorSection = trackNode.TrVectorNode.TrVectorSections[TrackVectorSectionIndex];
                trackSection = TSectionDat.TrackSections[trackVectorSection.SectionIndex];
            }
        }

        /// <summary>
        /// Saves a traveller to persisted data.
        /// </summary>
        /// <param name="outf">Writer to write persisted data to.</param>
        public void Save(BinaryWriter outf)
        {
            outf.Write((byte)direction);
            outf.Write(trackOffset);
            outf.Write(TrackNodeIndex);
            if (IsTrack)
                outf.Write(TrackVectorSectionIndex);
        }

        /// <summary>
        /// Test whether the given location is indeed on (or at least close to) the tracknode given by its index.
        /// If it is, we initialize the (current) traveller such that it is placed on the correct location on the track.
        /// The current traveller will not be changed if initialization is not successfull.
        /// </summary>
        /// <param name="tni">The index of the trackNode for which we test the location</param>
        /// <returns>boolean describing whether the location is indeed on the given tracknode and initialization is done</returns>
        bool InitTrackNode(int tni, int tileX, int tileZ, float wx, float wz)
        {
            //In contrast to an earlier implementaion there are no side-effects meaning a change in the traveller
            //even though the initializatin did not succeed. In particular, 
            //      tracksection, TrackVectorSectionIndex, trackVectorSection, tracknode, tracknodeindex
            //will only be set on successfull initialization
            WorldLocation loc = new WorldLocation(tileX, tileZ, wx, 0, wz);
            TrackNodeCandidate candidate = TryTrackNode(tni, loc, TSectionDat, TrackNodes);
            if (candidate == null) return false;

            InitFromCandidate(candidate);
            return true;
        }

        /// <summary>
        /// Try whether the given location is indeed on (or at least close to) the tracknode given by its index.
        /// If it is, we return a TrackNodeCandidate object. 
        /// </summary>
        /// <param name="tni">The index of the tracknode we are testing</param>
        /// <param name="loc">The location for which we want to see if it is on the tracksection</param>
        /// <param name="TSectionDat">Database with track sections</param>
        /// <param name="TrackNodes">List of available tracknodes</param>
        /// <returns>Details on where exactly the location is on the track.</returns>
        static TrackNodeCandidate TryTrackNode(int tni, WorldLocation loc, TrackSectionsFile TSectionDat, TrackNode[] TrackNodes)
        {
            TrackNode trackNode = TrackNodes[tni];
            if (trackNode == null || trackNode.TrVectorNode == null)
                return null;
            // TODO, we could do an additional cull here by calculating a bounding sphere for each node as they are being read.
            for (var tvsi = 0; tvsi < trackNode.TrVectorNode.TrVectorSections.Length; tvsi++)
            {
                TrackNodeCandidate candidate = TryTrackVectorSection(tvsi, loc, TSectionDat, trackNode);
                if (candidate != null)
                {
                    candidate.TrackNodeIndex = tni;
                    candidate.trackNode = trackNode;
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>
        /// Try whether the given location is indeed on (or at least close to) the trackvectorsection given by its index.
        /// If it is, we return a TrackNodeCandidate object. 
        /// </summary>
        /// <param name="tvsi">The index of the trackvectorsection</param>
        /// <param name="loc">The location for which we want to see if it is on the tracksection</param>
        /// <param name="TSectionDat">Database with track sections</param></param>
        /// <param name="trackNode">The parent trackNode of the vector section</param>
        /// <returns>Details on where exactly the location is on the track.</returns>
        static TrackNodeCandidate TryTrackVectorSection(int tvsi, WorldLocation loc, TrackSectionsFile TSectionDat, TrackNode trackNode)
        {
            TrVectorSection trackVectorSection = trackNode.TrVectorNode.TrVectorSections[tvsi];
            if (trackVectorSection == null)
                return null;
            TrackNodeCandidate candidate = TryTrackSection(trackVectorSection.SectionIndex, loc, TSectionDat, trackVectorSection);
            if (candidate == null) return null;

            candidate.TrackVectorSectionIndex = tvsi;
            candidate.trackVectorSection = trackVectorSection;
            return candidate;
        }

        /// <summary>
        /// Try whether the given location is indeed on (or at least close to) the tracksection given by its index.
        /// If it is, we return a TrackNodeCandidate object. 
        /// </summary>
        /// <param name="tsi">The track section index</param>
        /// <param name="loc">The location for which we want to see if it is on the tracksection</param>
        /// <param name="TSectionDat">Database with track sections</param>
        /// <param name="trackVectorSection">The parent track vector section</param>
        /// <returns>Details on where exactly the location is on the track.</returns>
        static TrackNodeCandidate TryTrackSection(uint tsi, WorldLocation loc, TrackSectionsFile TSectionDat, TrVectorSection trackVectorSection)
        {
            TrackSection trackSection = TSectionDat.TrackSections.Get(tsi);
            if (trackSection == null)
                return null;
            TrackNodeCandidate candidate;
            if (trackSection.SectionCurve == null)
            {
                candidate = TryTrackSectionStraight(loc, trackVectorSection, trackSection);
            }
            else
            {
                candidate = TryTrackSectionCurved(loc, trackVectorSection, trackSection);
            }
            if (candidate == null) return null;

            candidate.trackSection = trackSection;
            return candidate;
        }

        /// <summary>
        /// Try whether the given location is indeed on (or at least close to) the given curved tracksection.
        /// If it is, we return a TrackNodeCandidate object 
        /// </summary>
        /// <param name="loc">The location we are looking for</param>
        /// <param name="trackVectorSection">The trackvector section that is parent of the tracksection</param>
        /// <param name="trackSection">the specific tracksection we want to try</param>
        /// <returns>Details on where exactly the location is on the track.</returns>
        static TrackNodeCandidate TryTrackSectionCurved(WorldLocation loc, TrVectorSection trackVectorSection, TrackSection trackSection)
        {// TODO: Add y component.
            var l = loc.Location;
            // We're working relative to the track section, so offset as needed.
            l.X += (loc.TileX - trackVectorSection.TileX) * 2048;
            l.Z += (loc.TileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (radius * angle + error) by (radius * angle + error) around starting coordinates but no more than 2 for angle.
            var boundingDistance = trackSection.SectionCurve.Radius * Math.Min(Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)), 2) + MaximumCenterlineOffset;
            var dx = Math.Abs(l.X - sx);
            var dz = Math.Abs(l.Z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return null;

            // To simplify the math, center around the start of the track section, rotate such that the track section starts out pointing north (+z) and flip so the track curves to the right.
            l.X -= sx;
            l.Z -= sz;
            l = Vector3.Transform(l, Matrix.CreateRotationY(-trackVectorSection.AY));
            if (trackSection.SectionCurve.Angle < 0)
                l.X *= -1;

            // Compute distance to curve's center at (radius,0) then adjust to get distance from centerline.
            dx = l.X - trackSection.SectionCurve.Radius;
            float lat = (float)Math.Sqrt(dx * dx + l.Z * l.Z) - trackSection.SectionCurve.Radius;
            if (Math.Abs(lat) > MaximumCenterlineOffset)
                return null;

            // Compute distance along curve (ensure we are in the top right quadrant, otherwise our math goes wrong).
            if (l.Z < -InitErrorMargin || l.X > trackSection.SectionCurve.Radius + InitErrorMargin || l.Z > trackSection.SectionCurve.Radius + InitErrorMargin)
                return null;
            float radiansAlongCurve;
            if (l.Z > trackSection.SectionCurve.Radius)
                radiansAlongCurve = MathHelper.PiOver2;
            else
                radiansAlongCurve = (float)Math.Asin(l.Z / trackSection.SectionCurve.Radius);
            var lon = radiansAlongCurve * trackSection.SectionCurve.Radius;
            var trackSectionLength = GetLength(trackSection);
            if (lon < -InitErrorMargin || lon > trackSectionLength + InitErrorMargin)
                return null;

            return new TrackNodeCandidate(Math.Abs(lat), lon, true);
        }

        /// <summary>
        /// Try whether the given location is indeed on (or at least close to) the given straight tracksection.
        /// If it is, we return a TrackNodeCandidate object 
        /// </summary>
        /// <param name="loc">The location we are looking for</param>
        /// <param name="trackVectorSection">The trackvector section that is parent of the tracksection</param>
        /// <param name="trackSection">the specific tracksection we want to try</param>
        /// <returns>Details on where exactly the location is on the track.</returns>
        static TrackNodeCandidate TryTrackSectionStraight(WorldLocation loc, TrVectorSection trackVectorSection, TrackSection trackSection)
        { // TODO: Add y component.
            float x = loc.Location.X;
            float z = loc.Location.Z;
            // We're working relative to the track section, so offset as needed.
            x += (loc.TileX - trackVectorSection.TileX) * 2048;
            z += (loc.TileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (length + error) by (length + error) around starting coordinates.
            if (trackSection != null && trackSection.SectionSize != null)
            {
                var boundingDistance = trackSection.SectionSize.Length + MaximumCenterlineOffset;
                var dx = Math.Abs(x - sx);
                var dz = Math.Abs(z - sz);
                if (dx > boundingDistance || dz > boundingDistance)
                    return null;
            }

            // Calculate distance along and away from the track centerline.
            float lat, lon;
            MstsUtility.Survey(sx, sz, trackVectorSection.AY, x, z, out lon, out lat);
            if (Math.Abs(lat) > MaximumCenterlineOffset)
                return null;
            if (lon < -InitErrorMargin || lon > GetLength(trackSection) + InitErrorMargin)
                return null;

            return new TrackNodeCandidate(Math.Abs(lat), lon, false);
        }

        /// <summary>
        /// Initialize the traveller on the already given tracksection, and return true if this succeeded
        /// </summary>
        /// <param name="traveller">The traveller that needs to be placed</param>
        /// <param name="location">The location where it needs to be placed</param>
        /// <returns>boolean showing whether the traveller can be placed on the section at given location</returns>
        private static bool InitTrackSectionSucceeded(Traveller traveller, WorldLocation location)
        {
            TrackNodeCandidate candidate = (traveller.IsTrackCurved)
                ? TryTrackSectionCurved(location, traveller.trackVectorSection, traveller.trackSection)
                : TryTrackSectionStraight(location, traveller.trackVectorSection, traveller.trackSection);

            if (candidate == null) return false;

            traveller.InitFromCandidate(candidate);
            return true;
        }

        public void Copy(Traveller copy)
        {
            TSectionDat = copy.TSectionDat;
            TrackNodes = copy.TrackNodes;
            locationSet = copy.locationSet;
            location.TileX = copy.location.TileX;
            location.TileZ = copy.location.TileZ;
            location.Location.X = copy.location.Location.X;
            location.Location.Y = copy.location.Location.Y;
            location.Location.Z = copy.location.Location.Z;
            direction = copy.direction;
            directionVector = copy.directionVector;
            trackOffset = copy.trackOffset;
            TrackNodeIndex = copy.TrackNodeIndex;
            trackNode = copy.trackNode;
            TrackVectorSectionIndex = copy.TrackVectorSectionIndex;
            trackVectorSection = copy.trackVectorSection;
            trackSection = copy.trackSection;
            lengthSet = copy.lengthSet;
            trackNodeLength = copy.trackNodeLength;
            trackNodeOffset = copy.trackNodeOffset;
        }

        /// <summary>
        /// Switched the direction of the traveller.
        /// </summary>
        /// <remarks>
        /// To set a known direction, use <see cref="Direction"/>.
        /// </remarks>
        public void ReverseDirection()
        {
            Direction = Direction == TravellerDirection.Forward ? TravellerDirection.Backward : TravellerDirection.Forward;
        }
        /// <summary>
        /// Returns the distance from the traveller's current lcation, in its current direction, to the location specified
        /// </summary>
        /// <param name="location">Target world location</param>
        /// <returns>f the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(WorldLocation location)
        {
            return DistanceTo(location.TileX, location.TileZ,
                location.Location.X, location.Location.Y, location.Location.Z);
        }

        /// <summary>
        /// Returns the distance from the traveller's current location, in its current direction, to the location specified.
        /// </summary>
        /// <param name="tileX">Target tile coordinate.</param>
        /// <param name="tileZ">Target tile coordinate.</param>
        /// <param name="x">Target coordinate.</param>
        /// <param name="y">Target coordinate.</param>
        /// <param name="z">Target coordinate.</param>
        /// <returns>If the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(int tileX, int tileZ, float x, float y, float z)
        {
            return DistanceTo(new Traveller(this), null, tileX, tileZ, x, y, z, float.MaxValue);
        }

        /// <summary>
        /// Returns the distance from the traveller's current location, in its current direction, to the location specified.
        /// </summary>
        /// <param name="tileX">Target tile coordinate.</param>
        /// <param name="tileZ">Target tile coordinate.</param>
        /// <param name="x">Target coordinate.</param>
        /// <param name="y">Target coordinate.</param>
        /// <param name="z">Target coordinate.</param>
        /// <param name="destination">Traveller at the destination</param>
        /// <returns>If the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(int tileX, int tileZ, float x, float y, float z, out Traveller destination)
        {
            destination = new Traveller(this);
            return DistanceTo(destination, null, tileX, tileZ, x, y, z, float.MaxValue);
        }

        /// <summary>
        /// Returns the distance from the traveller's current location, in its current direction, to the location specified.
        /// </summary>
        /// <param name="tileX">Target tile coordinate.</param>
        /// <param name="tileZ">Target tile coordinate.</param>
        /// <param name="x">Target coordinate.</param>
        /// <param name="y">Target coordinate.</param>
        /// <param name="z">Target coordinate.</param>
        /// <param name="maxDistance">MAximum distance to search for specified location.</param>
        /// <returns>If the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(int tileX, int tileZ, float x, float y, float z, float maxDistance)
        {
            return DistanceTo(new Traveller(this), null, tileX, tileZ, x, y, z, maxDistance);
        }

        /// <summary>
        /// Returns the distance from the traveller's current location, in its current direction, to the location specified.
        /// </summary>
        /// <param name="trackNode">Target track node.</param>
        /// <param name="tileX">Target tile coordinate.</param>
        /// <param name="tileZ">Target tile coordinate.</param>
        /// <param name="x">Target coordinate.</param>
        /// <param name="y">Target coordinate.</param>
        /// <param name="z">Target coordinate.</param>
        /// <returns>If the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(TrackNode trackNode, int tileX, int tileZ, float x, float y, float z)
        {
            return DistanceTo(new Traveller(this), trackNode, tileX, tileZ, x, y, z, float.MaxValue);
        }

        /// <summary>
        /// Returns the distance from the traveller's current location, in its current direction, to the location specified.
        /// </summary>
        /// <param name="trackNode">Target track node.</param>
        /// <param name="tileX">Target tile coordinate.</param>
        /// <param name="tileZ">Target tile coordinate.</param>
        /// <param name="x">Target coordinate.</param>
        /// <param name="y">Target coordinate.</param>
        /// <param name="z">Target coordinate.</param>
        /// <param name="maxDistance">MAximum distance to search for specified location.</param>
        /// <returns>If the target is found, the distance from the traveller's current location, along the track nodes, to the specified location. If the target is not found, <c>-1</c>.</returns>
        public float DistanceTo(TrackNode trackNode, int tileX, int tileZ, float x, float y, float z, float maxDistance)
        {
            return DistanceTo(new Traveller(this), trackNode, tileX, tileZ, x, y, z, maxDistance);
        }

        /// <summary>
        /// This is the actual routine that calculates the Distance To a given location along the track.
        /// </summary>
        static float DistanceTo(Traveller traveller, TrackNode trackNode, int tileX, int tileZ, float x, float y, float z, float maxDistance)
        {
            var targetLocation = new WorldLocation(tileX, tileZ, x, y, z);
            var accumulatedDistance = 0f;
            while (accumulatedDistance < maxDistance)
            {
                if (traveller.IsTrack)
                {
                    var initialOffset = traveller.trackOffset;
                    var radius = traveller.IsTrackCurved ? traveller.trackSection.SectionCurve.Radius : 1;
                    if (traveller.TN == trackNode || trackNode == null)
                    {
                        var direction = traveller.Direction == TravellerDirection.Forward ? 1 : -1;
                        if (InitTrackSectionSucceeded(traveller, targetLocation))
                        {
                            // If the new offset is EARLIER, the target is behind us!
                            if (traveller.trackOffset * direction < initialOffset * direction)
                                break;
                            // Otherwise, accumulate distance from offset change and we're done.
                            accumulatedDistance += (traveller.trackOffset - initialOffset) * direction * radius;
                            return accumulatedDistance;
                        }
                    }
                    // No sign of the target location in this track section, accumulate remaining track section length and continue.
                    var length = traveller.trackSection != null ? traveller.IsTrackCurved ? Math.Abs(MathHelper.ToRadians(traveller.trackSection.SectionCurve.Angle)) : traveller.trackSection.SectionSize.Length : 0;
                    accumulatedDistance += (traveller.Direction == TravellerDirection.Forward ? length - initialOffset : initialOffset) * radius;
                }
                // No sign of the target location yet, let's move on to the next track section.
                if (!traveller.NextSection())
                    break;
                if (traveller.IsJunction)
                {
                    // Junctions have no actual location but check the current traveller position against the target.
                    if (WorldLocation.GetDistanceSquared(traveller.WorldLocation, targetLocation) < 0.1)
                        return accumulatedDistance;
                    // No match; move past the junction node so we're on track again.
                    traveller.NextSection();
                }
                // If we've found the end of the track, the target isn't here.
                if (traveller.IsEnd)
                    break;
            }
            return -1;
        }

        public TrVectorSection GetCurrentSection()
        {
            if (TrackNodes[TrackNodeIndex].TrVectorNode != null)
                return TrackNodes[TrackNodeIndex].TrVectorNode.TrVectorSections[TrackVectorSectionIndex];
            else return null;
        }

        /// <summary>
        /// Moves the traveller on to the next section of track, whether that is another section within the current track node or a new track node.
        /// </summary>
        /// <returns><c>true</c> if the next section exists, <c>false</c> if it does not.</returns>
        public bool NextSection()
        {
            if (IsTrack && NextVectorSection())
                return true;
            return NextTrackNode();
        }

        public bool NextTrackNode()
        {
            if (IsJunction)
                Debug.Assert(trackNode.Inpins == 1 && trackNode.Outpins > 1);
            else if (IsEnd)
                Debug.Assert(trackNode.Inpins == 1 && trackNode.Outpins == 0);
            else
                Debug.Assert(trackNode.Inpins == 1 && trackNode.Outpins == 1);

            var oldTrackNodeIndex = TrackNodeIndex;
            var pin = direction == TravellerDirection.Forward ? (int)trackNode.Inpins : 0;
            if (IsJunction && direction == TravellerDirection.Forward)
                pin += trackNode.TrJunctionNode.SelectedRoute;
            if (pin < 0 || pin >= trackNode.TrPins.Length)
                return false;
            var trPin = trackNode.TrPins[pin];
            if (trPin.Link <= 0 || trPin.Link >= TrackNodes.Length)
                return false;

            direction = trPin.Direction > 0 ? TravellerDirection.Forward : TravellerDirection.Backward;
            trackOffset = 0;
            TrackNodeIndex = trPin.Link;
            trackNode = TrackNodes[TrackNodeIndex];
            TrackVectorSectionIndex = -1;
            trackVectorSection = null;
            trackSection = null;
            if (IsTrack)
            {
                if (direction == TravellerDirection.Forward)
                    NextTrackVectorSection(0);
                else
                    NextTrackVectorSection(trackNode.TrVectorNode.TrVectorSections.Length - 1);
            }
            JunctionEntryPinIndex = -1;
            for (var i = 0; i < trackNode.TrPins.Length; i++)
                if (trackNode.TrPins[i].Link == oldTrackNodeIndex)
                    JunctionEntryPinIndex = i;
            return true;
        }

        /// <summary>
        /// Moves the traveller on to the next section of the current track node only, stopping at the end of the track node.
        /// </summary>
        /// <returns><c>true</c> if the next section exists, <c>false</c> if it does not.</returns>
        public bool NextVectorSection()
        {
            if ((direction == TravellerDirection.Forward && trackVectorSection == trackNode.TrVectorNode.TrVectorSections[trackNode.TrVectorNode.TrVectorSections.Length - 1]) || (direction == TravellerDirection.Backward && trackVectorSection == trackNode.TrVectorNode.TrVectorSections[0]))
                return false;
            return NextTrackVectorSection(TrackVectorSectionIndex + (direction == TravellerDirection.Forward ? 1 : -1));
        }

        bool NextTrackVectorSection(int trackVectorSectionIndex)
        {
            TrackVectorSectionIndex = trackVectorSectionIndex;
            trackVectorSection = trackNode.TrVectorNode.TrVectorSections[TrackVectorSectionIndex];
            trackSection = TSectionDat.TrackSections.Get(trackVectorSection.SectionIndex);
            if (trackSection == null)
                return false;
            locationSet = lengthSet = false;
            trackOffset = direction == TravellerDirection.Forward ? 0 : IsTrackCurved ? Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize.Length;
            return true;
        }

        void SetLocation()
        {
            if (locationSet)
                return;

            locationSet = true;

            var tn = trackNode;
            var tvs = trackVectorSection;
            var ts = trackSection;
            var to = trackOffset;
            if (tvs == null)
            {
                // We're on a junction or end node. Use one of the links to get location and direction information.
                var pin = trackNode.TrPins[0];
                if (pin.Link <= 0 || pin.Link >= TrackNodes.Length)
                    return;
                tn = TrackNodes[pin.Link];
                tvs = tn.TrVectorNode.TrVectorSections[pin.Direction > 0 ? 0 : tn.TrVectorNode.TrVectorSections.Length - 1];
                ts = TSectionDat.TrackSections.Get(tvs.SectionIndex);
                if (ts == null)
                    return; // This is really bad and we'll have unknown data in the Traveller when the code reads the location and direction!
                to = pin.Direction > 0 ? -trackOffset : GetLength(ts) + trackOffset;
            }

            location.TileX = tvs.TileX;
            location.TileZ = tvs.TileZ;
            location.Location.X = tvs.X;
            location.Location.Y = tvs.Y;
            location.Location.Z = tvs.Z;
            directionVector.X = tvs.AX;
            directionVector.Y = tvs.AY;
            directionVector.Z = tvs.AZ;

            if (ts.SectionCurve != null)
            {
                // "Handedness" Convention: A right-hand curve (TS.SectionCurve.Angle > 0) curves 
                // to the right when moving forward.
                var sign = -Math.Sign(ts.SectionCurve.Angle);
                var vectorCurveStartToCenter = Vector3.Left * ts.SectionCurve.Radius * sign;
                var curveRotation = Matrix.CreateRotationY(to * sign);
                var XNAMatrix = Matrix.CreateFromYawPitchRoll(-tvs.AY, -tvs.AX, tvs.AZ);
                Vector3 dummy;
                var displacement = MSTSInterpolateAlongCurve(Vector3.Zero, vectorCurveStartToCenter, curveRotation, XNAMatrix, out dummy);
                location.Location.X += displacement.X;
                location.Location.Y += displacement.Y;
                location.Location.Z -= displacement.Z;
                directionVector.Y -= to * sign;
            }
            else
            {
                var XNAMatrix = Matrix.CreateFromYawPitchRoll(tvs.AY, tvs.AX, tvs.AZ);
                Vector3 dummy;
                var displacement = MSTSInterpolateAlongStraight(Vector3.Zero, Vector3.UnitZ, to, XNAMatrix, out dummy);
                location.Location.X += displacement.X;
                location.Location.Y += displacement.Y;
                location.Location.Z += displacement.Z;
            }

            if (direction == TravellerDirection.Backward)
            {
                directionVector.X *= -1;
                directionVector.Y += MathHelper.Pi;
            }
            directionVector.X = MathHelper.WrapAngle(directionVector.X);
            directionVector.Y = MathHelper.WrapAngle(directionVector.Y);

            if (trackVectorSection != null)
                location.NormalizeTo(trackVectorSection.TileX, trackVectorSection.TileZ);
        }

        void SetLength()
        {
            if (lengthSet)
                return;
            lengthSet = true;
            trackNodeLength = 0;
            trackNodeOffset = 0;
            if (trackNode == null || trackNode.TrVectorNode == null || trackNode.TrVectorNode.TrVectorSections == null)
                return;
            var tvs = trackNode.TrVectorNode.TrVectorSections;
            for (var i = 0; i < tvs.Length; i++)
            {
                var ts = TSectionDat.TrackSections.Get(tvs[i].SectionIndex);
                if (ts == null)
                    continue; // This is bad and we'll have potentially bogus data in the Traveller when the code reads the length!
                var length = GetLength(ts);
                trackNodeLength += length;
                if (i < TrackVectorSectionIndex)
                    trackNodeOffset += length;
                if (i == TrackVectorSectionIndex)
                    trackNodeOffset += trackOffset * (ts.SectionCurve != null ? ts.SectionCurve.Radius : 1);
            }
            if (Direction == TravellerDirection.Backward)
                trackNodeOffset = trackNodeLength - trackNodeOffset;
        }

        static float GetLength(TrackSection trackSection)
        {
            if (trackSection == null)
                return 0;

            return trackSection.SectionCurve != null ? trackSection.SectionCurve.Radius * Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize != null ? trackSection.SectionSize.Length : 0;
        }

        /// <summary>
        /// Current Curve Radius value. Zero if not a curve
        /// </summary>
        /// <returns>Current Curve Radius in meters</returns>
        public float GetCurveRadius()
        {
            if (trackSection == null)
                return 0;

            return trackSection.SectionCurve != null ? trackSection.SectionCurve.Radius : 0;
        }

        public float GetCurvature()
        {
            if (trackSection == null)
                return 0;

            return trackSection.SectionCurve != null ? Math.Sign(trackSection.SectionCurve.Angle) / trackSection.SectionCurve.Radius : 0;
        }

        public float GetSuperElevation()
        {
            if (trackSection == null)
                return 0;

            if (trackSection.SectionCurve == null)
                return 0;

            if (trackVectorSection == null)
                return 0;

            var trackLength = Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle));
            var sign = Math.Sign(trackSection.SectionCurve.Angle) > 0 ^ direction == TravellerDirection.Backward ? -1 : 1;
            var trackOffsetReverse = trackLength - trackOffset;

            var startingElevation = trackVectorSection.StartElev;
            var endingElevation = trackVectorSection.EndElev;
            var elevation = trackVectorSection.MaxElev * sign;

            // Check if there is no super-elevation at all.
            if (elevation.AlmostEqual(0f, 0.001f))
                return 0;

            if (trackOffset < trackLength / 2)
            {
                // Start of the curve; if there is starting super-elevation, use max super-elevation.
                if (startingElevation.AlmostEqual(0f, 0.001f))
                    return elevation * trackOffset * 2 / trackLength;

                return elevation;
            }

            // End of the curve; if there is ending super-elevation, use max super-elevation.
            if (endingElevation.AlmostEqual(0f, 0.001f))
                return elevation * trackOffsetReverse * 2 / trackLength;

            return elevation;
        }

        public float GetSuperElevation(float smoothingOffset)
        {
            var offset = new Traveller(this);
            offset.Move(smoothingOffset);
            return (GetSuperElevation() + offset.GetSuperElevation()) / 2;
        }

        public float FindTiltedZ(float speed) //will test 1 second ahead, computed will return desired elev. only
        {
            if (speed < 12) return 0;//no tilt if speed too low (<50km/h)
            var tn = trackNode;
            if (tn.TrVectorNode == null) return 0f;
            var tvs = trackVectorSection;
            var ts = trackSection;
            var desiredZ = 0f;
            if (tvs == null)
            {
                desiredZ = 0f;
            }
            else if (ts.SectionCurve != null)
            {
                float maxv = tvs.MaxElev;
                maxv = 0.14f * speed / 40f;//max 8 degree
                //maxv *= speed / 40f;
                //if (maxv.AlmostEqual(0f, 0.001f)) maxv = 0.02f; //short curve, add some effect anyway
                var sign = -Math.Sign(ts.SectionCurve.Angle);
                if ((this.direction == TravellerDirection.Forward ? 1 : -1) * sign > 0) desiredZ = 1f;
                else desiredZ = -1f;
                desiredZ *= maxv;//max elevation
            }
            else desiredZ = 0f;
            return desiredZ;
        }

        /// <summary>
        /// Finds the nearest junction node in the direction this traveller is facing.
        /// </summary>
        /// <returns>The <see cref="TrJunctionNode"/> of the found junction, or <c>null</c> if none was found.</returns>
        public TrJunctionNode JunctionNodeAhead()
        {
            return NextJunctionNode(TravellerDirection.Forward);
        }

        /// <summary>
        /// Finds the nearest junction node in the opposite direction to this traveller.
        /// </summary>
        /// <returns>The <see cref="TrJunctionNode"/> of the found junction, or <c>null</c> if none was found.</returns>
        public TrJunctionNode JunctionNodeBehind()
        {
            return NextJunctionNode(TravellerDirection.Backward);
        }

        TrJunctionNode NextJunctionNode(TravellerDirection direction)
        {
            var traveller = new Traveller(this, direction);
            while (traveller.NextSection())
                if (traveller.IsJunction)
                    return traveller.trackNode.TrJunctionNode;
            return null;
        }

        /// <summary>
        /// Move the traveller along the track by the specified distance, or until the end of the track is reached.
        /// </summary>
        /// <param name="distanceToGo">The distance to travel along the track. Positive values travel in the direction of the traveller and negative values in the opposite direction.</param>
        /// <returns>The remaining distance if the traveller reached the end of the track.</returns>
        public float Move(float distanceToGo)
        {
            // TODO - must remove the trig from these calculations
            if (float.IsNaN(distanceToGo)) distanceToGo = 0f;
            var distanceSign = Math.Sign(distanceToGo);
            distanceToGo = Math.Abs(distanceToGo);
            if (distanceSign < 0)
                ReverseDirection();
            do
            {
                distanceToGo = MoveInTrackSection(distanceToGo);
                if (distanceToGo < 0.001)
                    break;
            }
            while (NextSection());
            if (distanceSign < 0)
                ReverseDirection();
            return distanceSign * distanceToGo;
        }

        /// <summary>
        /// Move the traveller along the track by the specified distance, or until the end of the track is reached, within the current track section only.
        /// </summary>
        /// <param name="distanceToGo">The distance to travel along the track section. Positive values travel in the direction of the traveller and negative values in the opposite direction.</param>
        /// <returns>The remaining distance if the traveller reached the end of the track section.</returns>
        public float MoveInSection(float distanceToGo)
        {
            var distanceSign = Math.Sign(distanceToGo);
            distanceToGo = Math.Abs(distanceToGo);
            if (distanceSign < 0)
                ReverseDirection();
            distanceToGo = MoveInTrackSection(distanceToGo);
            if (distanceSign < 0)
                ReverseDirection();
            return distanceSign * distanceToGo;
        }

        float MoveInTrackSection(float distanceToGo)
        {
            if (IsJunction)
                return distanceToGo;
            if (!IsTrack)
                return MoveInTrackSectionInfinite(distanceToGo);
            if (IsTrackCurved)
                return MoveInTrackSectionCurved(distanceToGo);
            return MoveInTrackSectionStraight(distanceToGo);
        }

        float MoveInTrackSectionInfinite(float distanceToGo)
        {
            var scale = Direction == TravellerDirection.Forward ? 1 : -1;
            var distance = distanceToGo;
            if (Direction == TravellerDirection.Backward && distance > trackOffset)
                distance = trackOffset;
            trackOffset += scale * distance;
            trackNodeOffset += distance;
            locationSet = false;
            return distanceToGo - distance;
        }

        float MoveInTrackSectionCurved(float distanceToGo)
        {
            var scale = Direction == TravellerDirection.Forward ? 1 : -1;
            var desiredTurnRadians = distanceToGo / trackSection.SectionCurve.Radius;
            var sectionTurnRadians = Math.Abs(MathHelper.ToRadians(trackSection.SectionCurve.Angle));
            if (direction == TravellerDirection.Forward)
            {
                if (desiredTurnRadians > sectionTurnRadians - trackOffset)
                    desiredTurnRadians = sectionTurnRadians - trackOffset;
            }
            else
            {
                if (desiredTurnRadians > trackOffset)
                    desiredTurnRadians = trackOffset;
            }
            trackOffset += scale * desiredTurnRadians;
            trackNodeOffset += desiredTurnRadians * trackSection.SectionCurve.Radius;
            locationSet = false;
            return distanceToGo - desiredTurnRadians * trackSection.SectionCurve.Radius;
        }

        float MoveInTrackSectionStraight(float distanceToGo)
        {
            var scale = Direction == TravellerDirection.Forward ? 1 : -1;
            var desiredDistance = distanceToGo;
            if (direction == TravellerDirection.Forward)
            {
                if (desiredDistance > trackSection.SectionSize.Length - trackOffset)
                    desiredDistance = trackSection.SectionSize.Length - trackOffset;
            }
            else
            {
                if (desiredDistance > trackOffset)
                    desiredDistance = trackOffset;
            }
            trackOffset += scale * desiredDistance;
            trackNodeOffset += desiredDistance;
            locationSet = false;
            return distanceToGo - desiredDistance;
        }

        /// <summary>
        /// MSTSInterpolateAlongCurve interpolates position along a circular arc.
        /// (Uses MSTS rigid-body rotation method for curve on a grade.)
        /// </summary>
        /// <param name="vPC">Local position vector for Point-of-Curve (PC) in x-z plane.</param>
        /// <param name="vPC_O">Unit vector in direction from PC to arc center (O).</param>
        /// <param name="mRotY">Rotation matrix that deflects arc from PC to a point on curve (P).</param>
        /// <param name="mWorld">Transformation from local to world coordinates.</param>
        /// <param name="vP">Position vector for desired point on curve (P), returned by reference.</param>
        /// <returns>Displacement vector from PC to P in world coordinates.</returns>
        public static Vector3 MSTSInterpolateAlongCurve(Vector3 vPC, Vector3 vPC_O, Matrix mRotY, Matrix mWorld, out Vector3 vP)
        {
            // Shared method returns displacement from present world position and, by reference,
            // local position in x-z plane of end of this section
            var vO_P = Vector3.Transform(-vPC_O, mRotY); // Rotate O_PC to O_P
            vP = vPC + vPC_O + vO_P; // Position of P relative to PC
            return Vector3.Transform(vP, mWorld); // Transform to world coordinates and return as displacement.
        }

        /// <summary>
        /// MSTSInterpolateAlongStraight interpolates position along a straight stretch.
        /// </summary>
        /// <param name="vP0">Local position vector for starting point P0 in x-z plane.</param>
        /// <param name="vP0_P">Unit vector in direction from P0 to P.</param>
        /// <param name="offset">Distance from P0 to P.</param>
        /// <param name="mWorld">Transformation from local to world coordinates.</param>
        /// <param name="vP">Position vector for desired point(P), returned by reference.</param>
        /// <returns>Displacement vector from P0 to P in world coordinates.</returns>
        public static Vector3 MSTSInterpolateAlongStraight(Vector3 vP0, Vector3 vP0_P, float offset, Matrix mWorld, out Vector3 vP)
        {
            vP = vP0 + offset * vP0_P; // Position of desired point in local coordinates.
            return Vector3.Transform(vP, mWorld);
        }

        // TODO: This is a bit of a strange method that probably should be cleaned up.
        public float OverlapDistanceM(Traveller other, bool rear)
        {
            var dx = X - other.X + 2048 * (TileX - other.TileX);
            var dz = Z - other.Z + 2048 * (TileZ - other.TileZ);
            var dy = Y - other.Y;
            if (dx * dx + dz * dz > 1)
                return 1;
            if (Math.Abs(dy) > 1)
                return 1;
            var dot = dx * (float)Math.Sin(directionVector.Y) + dz * (float)Math.Cos(directionVector.Y);
            return rear ? dot : -dot;
        }

        // Checks if trains are overlapping. Used in multiplayer, where the standard method may lead to train overlapping
        public float RoughOverlapDistanceM(Traveller other, Traveller farMe, Traveller farOther, float lengthMe, float lengthOther, bool rear)
        {
            var dy = Y - other.Y;
            if (Math.Abs(dy) > 1)
                return 1;
            var dx = farMe.X - other.X + 2048 * (farMe.TileX - other.TileX);
            var dz = farMe.Z - other.Z + 2048 * (farMe.TileZ - other.TileZ);
            if (dx * dx + dz * dz > lengthMe * lengthMe) return 1;
            dx = X - farOther.X + 2048 * (TileX - farOther.TileX);
            dz = Z - farOther.Z + 2048 * (TileZ - farOther.TileZ);
            if (dx * dx + dz * dz > lengthOther * lengthOther) return 1;
            dx = X - other.X + 2048 * (TileX - other.TileX);
            dz = Z - other.Z + 2048 * (TileZ - other.TileZ);
            var diagonal = dx * dx + dz * dz;
            if (diagonal < 200 && diagonal < (lengthMe + lengthOther) * (lengthMe + lengthOther))
            {
                var dot = dx * (float)Math.Sin(directionVector.Y) + dz * (float)Math.Cos(directionVector.Y);
                return rear ? dot : -dot;
            }
            return 1;
        }

        public override string ToString()
        {
            return String.Format("{{TN={0} TS={1} O={2:F6}}}", TrackNodeIndex, TrackVectorSectionIndex, trackOffset);
        }

        /// <summary>
        /// During initialization a specific track section (candidate) needs to be found corresponding to the requested worldLocation.
        /// Once the best (or only) candidate has been found, this routine initializes the traveller from the information
        /// stored in the candidate.
        /// </summary>
        /// <param name="candidate">The candidate with all information needed to place the traveller</param>
        void InitFromCandidate(TrackNodeCandidate candidate)
        {
            // Some things only have to be set when defined. This prevents overwriting existing settings.
            // The order might be important.
            if (candidate.trackNode != null) trackNode = candidate.trackNode;
            if (candidate.TrackNodeIndex >= 0) TrackNodeIndex = candidate.TrackNodeIndex;
            if (candidate.trackVectorSection != null) trackVectorSection = candidate.trackVectorSection;
            if (candidate.TrackVectorSectionIndex >= 0) TrackVectorSectionIndex = candidate.TrackVectorSectionIndex;
            if (candidate.trackSection != null) trackSection = candidate.trackSection;

            // these are always set:
            direction = TravellerDirection.Forward;
            trackOffset = 0;
            locationSet = lengthSet = false;
            if (candidate.isCurved)
            {
                MoveInTrackSectionCurved(candidate.lon);
            }
            else
            {
                MoveInTrackSectionStraight(candidate.lon);
            }
        }

        public sealed class MissingTrackNodeException : Exception
        {
            public MissingTrackNodeException()
                : base("")
            {
            }
        }
    }

    /// <summary>
    /// Helper class to store details of a possible candidate where we can place the traveller.
    /// Used during initialization as part of constructer(s)
    /// </summary>
    class TrackNodeCandidate
    {
        public float lon;               // longitude along the section
        public float distanceToTrack;   // lateral distance to the track
        public bool isCurved;           // Whether the tracksection is curved or not.  
        public TrackNode trackNode;     // the trackNode object
        public int TrackNodeIndex = -1; // the index of the tracknode
        public TrVectorSection trackVectorSection; // the trackvectorSection within the tracknode
        public int TrackVectorSectionIndex = -1;   // the corresponding index of the trackvectorsection
        public TrackSection trackSection;          // the tracksection within the trackvectorsection

        /// <summary>
        /// Constructor will only be called deep into a section, where the actual lon(gitude) and lat(ttide) are being calculated.
        /// </summary>
        public TrackNodeCandidate(float distanceToTrack, float lon, bool isCurved)
        {
            this.lon = lon;
            this.distanceToTrack = distanceToTrack;
            this.isCurved = isCurved;
        }
    }

    public class TravellerInvalidDataException : Exception
    {
        public TravellerInvalidDataException(string format, params object[] args)
            : base(String.Format(format, args))
        {
        }
    }

    public abstract class TravellerInitializationException : Exception
    {
        public readonly int TileX;
        public readonly int TileZ;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly TrVectorSection TVS;
        public readonly float ErrorLimit;

        protected TravellerInitializationException(Exception innerException, int tileX, int tileZ, float x, float y, float z, TrVectorSection tvs, float errorLimit, string format, params object[] args)
            : base(String.Format(format, args), innerException)
        {
            TileX = tileX;
            TileZ = tileZ;
            X = x;
            Y = y;
            Z = z;
            TVS = tvs;
            ErrorLimit = errorLimit;
        }
    }

    public class TravellerOutsideBoundingAreaException : TravellerInitializationException
    {
        public readonly float DistanceX;
        public readonly float DistanceZ;

        public TravellerOutsideBoundingAreaException(int tileX, int tileZ, float x, float y, float z, TrVectorSection tvs, float errorLimit, float dx, float dz)
            : base(null, tileX, tileZ, x, y, z, tvs, errorLimit, "{0} is ({3} > {2} or {4} > {2}) outside the bounding area of track vector section {1}", new WorldLocation(tileX, tileZ, x, y, z), tvs, errorLimit, dx, dz)
        {
            DistanceX = dx;
            DistanceZ = dz;
        }
    }

    public class TravellerOutsideCenterlineException : TravellerInitializationException
    {
        public readonly float Distance;

        public TravellerOutsideCenterlineException(int tileX, int tileZ, float x, float y, float z, TrVectorSection tvs, float errorLimit, float distance)
            : base(null, tileX, tileZ, x, y, z, tvs, errorLimit, "{0} is ({2} > {3}) from the centerline of track vector section {1}", new WorldLocation(tileX, tileZ, x, y, z), tvs, distance, errorLimit)
        {
            Distance = distance;
        }
    }

    public class TravellerBeyondTrackLengthException : TravellerInitializationException
    {
        public readonly float Length;
        public readonly float Distance;

        public TravellerBeyondTrackLengthException(int tileX, int tileZ, float x, float y, float z, TrVectorSection tvs, float errorLimit, float length, float distance)
            : base(null, tileX, tileZ, x, y, z, tvs, errorLimit, "{0} is ({2} < {3} or {2} > {4}) beyond the extents of track vector section {1}", new WorldLocation(tileX, tileZ, x, y, z), tvs, distance, -errorLimit, length + errorLimit)
        {
            Length = length;
            Distance = distance;
        }
    }
}
