// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using MSTS;
using MSTSMath;

namespace ORTS
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
        const float InitErrorMargin = 0.01f;

        // If a car has some overhang, than it will be offset toward the center of curvature
        // and won't be right along the center line.  I'll have to add some allowance for this
        // and accept a hit if it is within 2.5 meters of the center line - this was determined
        // experimentally to match MSTS's 'capture range'.
        const float MaximumCenterlineOffset = 2.5f;

        readonly TSectionDatFile TSectionDat;
        readonly TrackNode[] TrackNodes;
        TravellerDirection direction = TravellerDirection.Forward;
        float trackOffset; // Offset into section; meters for straight sections, radians for curved sections.
        TrackNode trackNode;
        TrVectorSection trackVectorSection;
        TrackSection trackSection;

        // Location and directionVector are only valid if locationSet == true.
        bool locationSet = false;
        WorldLocation location = new WorldLocation();
        Vector3 directionVector;

        public WorldLocation WorldLocation { get { if (!locationSet) SetLocation(); return new WorldLocation(location); } }
        public int TileX { get { if (!locationSet) SetLocation(); return location.TileX; } }
        public int TileZ { get { if (!locationSet) SetLocation(); return location.TileZ; } }
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
                    if (!locationSet) SetLocation();
                    direction = value;
                    directionVector.X *= -1;
                    directionVector.Y += (float)Math.PI;
                    M.NormalizeRadians(ref directionVector.X);
                    M.NormalizeRadians(ref directionVector.Y);
                }
            }
        }
        public float RotY { get { if (!locationSet) SetLocation(); return directionVector.Y; } }
        public TrackNode TN { get { return trackNode; } }
        public int TrackNodeIndex { get; private set; }
        public int TrackVectorSectionIndex { get; private set; }

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
        public bool IsTrackCurved { get { return IsTrack && trackSection.SectionCurve != null; } }
        /// <summary>
        /// Returns whether this traveller is currently on a section of track which is straight.
        /// </summary>
        public bool IsTrackStraight { get { return IsTrack && trackSection.SectionCurve == null; } }
        /// <summary>
        /// Returns the pin index number, for the current track node, identifying the route travelled into this track node.
        /// </summary>
        public int JunctionEntryPinIndex { get; private set; }

        Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes)
        {
            if (tSectionDat == null) throw new ArgumentNullException("tSectionDat");
            if (trackNodes == null) throw new ArgumentNullException("trackNodes");
            TSectionDat = tSectionDat;
            TrackNodes = trackNodes;
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
        public Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, int tileX, int tileZ, float x, float z)
            : this(tSectionDat, trackNodes)
        {
            for (var tni = 0; tni < TrackNodes.Length; tni++)
                if (InitTrackNode(tni, tileX, tileZ, x, z))
                    return;
            throw new InvalidDataException(String.Format("{0} could not be found in the track database.", new WorldLocation(tileX, tileZ, x, 0, z)));
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
        public Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, int tileX, int tileZ, float x, float z, TravellerDirection direction)
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
        public Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode)
            : this(tSectionDat, trackNodes)
        {
            if (startTrackNode == null) throw new ArgumentNullException("startTrackNode");
            var startTrackNodeIndex = Array.IndexOf(trackNodes, startTrackNode);
            if (startTrackNodeIndex == -1) throw new ArgumentException("Track node is not in track nodes array.", "startTrackNode");
            if (startTrackNode.TrVectorNode == null) throw new ArgumentException("Track node is not a vector node.", "startTrackNode");
            if (startTrackNode.TrVectorNode.TrVectorSections == null) throw new ArgumentException("Track node has no vector section data.", "startTrackNode");
            if (startTrackNode.TrVectorNode.TrVectorSections.Length == 0) throw new ArgumentException("Track node has no vector sections.", "startTrackNode");
            var tvs = startTrackNode.TrVectorNode.TrVectorSections[0];
            if (InitTrackNode(startTrackNodeIndex, tvs.TileX, tvs.TileZ, tvs.X, tvs.Z))
                return;
            throw new InvalidDataException(String.Format("Failed to initialize Traveller at track node {0}.", Array.IndexOf(trackNodes, trackNode)));
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
        Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode, int tileX, int tileZ, float x, float z)
            : this(tSectionDat, trackNodes)
        {
            if (startTrackNode == null) throw new ArgumentNullException("startTrackNode");
            var startTrackNodeIndex = Array.IndexOf(trackNodes, startTrackNode);
            if (startTrackNodeIndex == -1) throw new ArgumentException("Track node is not in track nodes array.", "startTrackNode");
            if (InitTrackNode(startTrackNodeIndex, tileX, tileZ, x, z))
                return;
            // TODO: Probably should check that we're actually somewhere near one end or the other.
            throw new InvalidDataException(String.Format("{0} failed to initialize Traveller at track node {1}.", new WorldLocation(tileX, tileZ, x, 0, z), Array.IndexOf(trackNodes, startTrackNode)));
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
        public Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, TrackNode startTrackNode, int tileX, int tileZ, float x, float z, TravellerDirection direction)
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
            TSectionDat = copy.TSectionDat;
            TrackNodes = copy.TrackNodes;
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
        public Traveller(TSectionDatFile tSectionDat, TrackNode[] trackNodes, BinaryReader inf)
            : this(tSectionDat, trackNodes)
        {
            locationSet = inf.ReadBoolean();
            location.Restore(inf);
            direction = (TravellerDirection)inf.ReadByte();
            directionVector.X = inf.ReadSingle();
            directionVector.Y = inf.ReadSingle();
            directionVector.Z = inf.ReadSingle();
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
            outf.Write(locationSet);
            location.Save(outf);
            outf.Write((byte)direction);
            outf.Write(directionVector.X);
            outf.Write(directionVector.Y);
            outf.Write(directionVector.Z);
            outf.Write(trackOffset);
            outf.Write(TrackNodeIndex);
            if (IsTrack)
                outf.Write(TrackVectorSectionIndex);
        }

        bool InitTrackNode(int tni, int tileX, int tileZ, float wx, float wz)
        {
            TrackNodeIndex = tni;
            trackNode = TrackNodes[TrackNodeIndex];
            if (trackNode == null || trackNode.TrVectorNode == null)
                return false;
            // TODO, we could do an additional cull here by calculating a bounding sphere for each node as they are being read.
            for (var tvsi = 0; tvsi < trackNode.TrVectorNode.TrVectorSections.Length; tvsi++)
                if (InitTrackVectorSection(tvsi, tileX, tileZ, wx, wz))
                    return true;
            return false;
        }

        bool InitTrackVectorSection(int tvsi, int tileX, int tileZ, float x, float z)
        {
            TrackVectorSectionIndex = tvsi;
            trackVectorSection = trackNode.TrVectorNode.TrVectorSections[TrackVectorSectionIndex];
            if (trackVectorSection == null)
                return false;
            if (InitTrackSection(trackVectorSection.SectionIndex, tileX, tileZ, x, z))
                return true;
            return false;
        }

        bool InitTrackSection(uint tsi, int tileX, int tileZ, float x, float z)
        {
            trackSection = TSectionDat.TrackSections.Get(tsi);
            if (trackSection == null)
                return false;
            if ((trackSection.SectionCurve != null && InitTrackSectionCurved(tileX, tileZ, x, z)) || (trackSection.SectionCurve == null && InitTrackSectionStraight(tileX, tileZ, x, z)))
                return true;
            return false;
        }

        // TODO: Add y component.
        bool InitTrackSectionCurved(int tileX, int tileZ, float x, float z)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (radius * angle + error) by (radius * angle + error) around starting coordinates but no more than 2 for angle.
            var boundingDistance = trackSection.SectionCurve.Radius * Math.Min(Math.Abs(M.Radians(trackSection.SectionCurve.Angle)), 2) + MaximumCenterlineOffset;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // To simplify the math, center around the start of the track section, rotate such that the track section starts out pointing north (+z) and flip so the track curves to the right.
            x -= sx;
            z -= sz;
            M.Rotate2D(trackVectorSection.AY, ref x, ref z);
            if (trackSection.SectionCurve.Angle < 0)
                x *= -1;

            // Compute distance to curve's center at (radius,0) then adjust to get distance from centerline.
            dx = x - trackSection.SectionCurve.Radius;
            var lat = Math.Sqrt(dx * dx + z * z) - trackSection.SectionCurve.Radius;
            if (Math.Abs(lat) > MaximumCenterlineOffset)
                return false;

            // Compute distance along curve (ensure we are in the top right quadrant, otherwise our math goes wrong).
            if (z < -InitErrorMargin || x > trackSection.SectionCurve.Radius + InitErrorMargin || z > trackSection.SectionCurve.Radius + InitErrorMargin)
                return false;
            var radiansAlongCurve = (float)Math.Asin(z / trackSection.SectionCurve.Radius);
            var lon = radiansAlongCurve * trackSection.SectionCurve.Radius;
            var trackSectionLength = M.Radians(Math.Abs(trackSection.SectionCurve.Angle)) * trackSection.SectionCurve.Radius;
            if (lon < -InitErrorMargin || lon > trackSectionLength + InitErrorMargin)
                return false;

            direction = TravellerDirection.Forward;
            trackOffset = 0;
            locationSet = false;
            MoveInTrackSectionCurved(lon);
            return true;
        }

        // TODO: Add y component.
        bool InitTrackSectionStraight(int tileX, int tileZ, float x, float z)
        {
            // We're working relative to the track section, so offset as needed.
            x += (tileX - trackVectorSection.TileX) * 2048;
            z += (tileZ - trackVectorSection.TileZ) * 2048;
            var sx = trackVectorSection.X;
            var sz = trackVectorSection.Z;

            // Do a preliminary cull based on a bounding square around the track section.
            // Bounding distance is (length + error) by (length + error) around starting coordinates.
            var boundingDistance = trackSection.SectionSize.Length + MaximumCenterlineOffset;
            var dx = Math.Abs(x - sx);
            var dz = Math.Abs(z - sz);
            if (dx > boundingDistance || dz > boundingDistance)
                return false;

            // Calculate distance along and away from the track centerline.
            float lat, lon;
            M.Survey(sx, sz, trackVectorSection.AY, x, z, out lon, out lat);
            if (Math.Abs(lat) > MaximumCenterlineOffset || lon < -InitErrorMargin || lon > trackSection.SectionSize.Length + InitErrorMargin)
                return false;

            direction = TravellerDirection.Forward;
            trackOffset = 0;
            locationSet = false;
            MoveInTrackSectionStraight(lon);
            return true;
        }

        void Copy(Traveller copy)
        {
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
                        if ((traveller.IsTrackCurved && traveller.InitTrackSectionCurved(tileX, tileZ, x, z)) || (!traveller.IsTrackCurved && traveller.InitTrackSectionStraight(tileX, tileZ, x, z)))
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
                    var length = traveller.IsTrackCurved ? Math.Abs(M.Radians(traveller.trackSection.SectionCurve.Angle)) : traveller.trackSection.SectionSize.Length;
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

        bool NextTrackNode()
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
            if (trPin.Link < 0 || trPin.Link >= TrackNodes.Length)
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
            JunctionEntryPinIndex = Array.FindIndex(trackNode.TrPins, tp => tp.Link == oldTrackNodeIndex);
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
            locationSet = false;
            trackOffset = direction == TravellerDirection.Forward ? 0 : IsTrackCurved ? Math.Abs(M.Radians(trackSection.SectionCurve.Angle)) : trackSection.SectionSize.Length;
            return true;
        }

        void SetLocation()
        {
            if (locationSet)
                return;

            locationSet = true;

            if (trackVectorSection == null)
            {
                location = new WorldLocation();
                directionVector = new Vector3();
            }
            else
            {
                location.TileX = trackVectorSection.TileX;
                location.TileZ = trackVectorSection.TileZ;
                location.Location.X = trackVectorSection.X;
                location.Location.Y = trackVectorSection.Y;
                location.Location.Z = trackVectorSection.Z;
                directionVector.X = trackVectorSection.AX;
                directionVector.Y = trackVectorSection.AY;
                directionVector.Z = trackVectorSection.AZ;
            }

            if (IsTrackCurved)
            {
                // "Handedness" Convention: A right-hand curve (TS.SectionCurve.Angle > 0) curves 
                // to the right when moving forward.
                var sign = -Math.Sign(trackSection.SectionCurve.Angle);
                var vectorCurveStartToCenter = Vector3.Left * trackSection.SectionCurve.Radius * sign;
                var curveRotation = Matrix.CreateRotationY(trackOffset * sign);
                var XNAMatrix = Matrix.CreateFromYawPitchRoll(-trackVectorSection.AY, -trackVectorSection.AX, trackVectorSection.AZ);
                Vector3 dummy;
                var displacement = MSTSInterpolateAlongCurve(Vector3.Zero, vectorCurveStartToCenter, curveRotation, XNAMatrix, out dummy);
                location.Location.X = trackVectorSection.X + displacement.X;
                location.Location.Y = trackVectorSection.Y + displacement.Y;
                location.Location.Z = trackVectorSection.Z - displacement.Z;
                directionVector.Y -= trackOffset * sign;
            }
            else if (IsTrackStraight)
            {
                var XNAMatrix = Matrix.CreateFromYawPitchRoll(trackVectorSection.AY, trackVectorSection.AX, trackVectorSection.AZ);
                Vector3 dummy;
                var displacement = MSTSInterpolateAlongStraight(Vector3.Zero, Vector3.UnitZ, trackOffset, XNAMatrix, out dummy);
                location.Location.X = trackVectorSection.X + displacement.X;
                location.Location.Y = trackVectorSection.Y + displacement.Y;
                location.Location.Z = trackVectorSection.Z + displacement.Z;
            }
            else
            {
                // TODO replace with matrix math
                var P = new TWorldPosition(X, Y, Z);
                var D = new TWorldDirection();
                D.SetAngles(directionVector.Y, -directionVector.X);
                P.Move(D, trackOffset);
                location.Location.X = P.X;
                location.Location.Y = P.Y;
                location.Location.Z = P.Z;
            }

            if (direction == TravellerDirection.Backward)
            {
                directionVector.X *= -1;
                directionVector.Y += (float)Math.PI;
            }
            M.NormalizeRadians(ref directionVector.X);
            M.NormalizeRadians(ref directionVector.Y);

            if (trackVectorSection != null)
            {
                while (location.TileX > trackVectorSection.TileX) { location.Location.X += 2048; --location.TileX; }
                while (location.TileX < trackVectorSection.TileX) { location.Location.X -= 2048; ++location.TileX; }
                while (location.TileZ > trackVectorSection.TileZ) { location.Location.Z += 2048; --location.TileZ; }
                while (location.TileZ < trackVectorSection.TileZ) { location.Location.Z -= 2048; ++location.TileZ; }
            }
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
            var distance = distanceToGo;
            if (Direction == TravellerDirection.Backward && distance > trackOffset)
                distance = trackOffset;

            if (Direction == TravellerDirection.Forward)
                trackOffset += distance;
            else
                trackOffset -= distance;

            locationSet = false;
            return distanceToGo - distance;
        }

        float MoveInTrackSectionCurved(float distanceToGo)
        {
            var desiredTurnRadians = distanceToGo / trackSection.SectionCurve.Radius;
            var sectionTurnRadians = Math.Abs(trackSection.SectionCurve.Angle * (float)(Math.PI / 180.0));
            if (direction == TravellerDirection.Forward)
            {
                if (desiredTurnRadians > sectionTurnRadians - trackOffset)
                    desiredTurnRadians = sectionTurnRadians - trackOffset;
                trackOffset += desiredTurnRadians;
            }
            else
            {
                if (desiredTurnRadians > trackOffset)
                    desiredTurnRadians = trackOffset;
                trackOffset -= desiredTurnRadians;
            }
            locationSet = false;
            return distanceToGo - desiredTurnRadians * trackSection.SectionCurve.Radius;
        }

        float MoveInTrackSectionStraight(float distanceToGo)
        {
            var desiredDistance = distanceToGo;
            if (direction == TravellerDirection.Forward)
            {
                if (desiredDistance > trackSection.SectionSize.Length - trackOffset)
                    desiredDistance = trackSection.SectionSize.Length - trackOffset;
                trackOffset += desiredDistance;
            }
            else
            {
                if (desiredDistance > trackOffset)
                    desiredDistance = trackOffset;
                trackOffset -= desiredDistance;
            }
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
            if (dx * dx + dz * dz > 1)
                return 1;
            var dot = dx * (float)Math.Sin(directionVector.Y) + dz * (float)Math.Cos(directionVector.Y);
            return rear ? dot : -dot;
        }

        public override string ToString()
        {
            return String.Format("TN={0} TS={1} O={2:F6}", TrackNodeIndex, TrackVectorSectionIndex, trackOffset);
        }
    }
}
