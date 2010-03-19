/*  TDBTraveller
 * 
 *  A Track Database Traveller represents a specific location and direction in the track database.
 *  Think of it like a virtual truck or bogie that can travel the track database.
 *  Constructing a TDBTraveller involves placing it on a track in the TDB.
 *  It then provides methods for moving along the track.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using MSTS;
using MSTSMath;
using System.IO;

namespace ORTS
{
    public class TDBTraveller
    {
        TDBFile TDB;
        TSectionDatFile TSectionDat;

        // World position and orientation of the TDB Traveller
        public int TileX;
        public int TileZ;
        public float X, Y, Z; // relative to Tile center
        private float AX, AY, AZ;  // radians these reverse when the direction reverses.

        public ORTS.WorldLocation WorldLocation
        {
            get
            {
                ORTS.WorldLocation worldLocation = new ORTS.WorldLocation();
                worldLocation.TileX = TileX;
                worldLocation.TileZ = TileZ;
                worldLocation.Location = new Vector3(X, Y, Z);
                return worldLocation;
            }
        }

        // TDB database location of the traveller
        // these must be updated with each change of segment
        int iTrackNode;
        int iTrVectorSection;
        TrackSection TS;
        public TrackNode TN;
        public int iEntryPIN;   // We entered this node on this PIN
        TrVectorSection TVS;
        float Offset; // Offset into section, meters for straight section, for curves, this is in radians
        private int pDirection; // 1 = forward 0 = backward

        public int Direction
        {
            get { return pDirection; }
            set
            {
                if (value != pDirection)
                {
                    pDirection = value;
                    AY += (float)Math.PI;
                    AX *= -1;
                    MSTSMath.M.NormalizeRadians(ref AY);
                    MSTSMath.M.NormalizeRadians(ref AX);
                }
            }
        }

        public int TrackNodeIndex
        {
            get { return iTrackNode; }
        }

        public void ReverseDirection()
        {
            if (Direction == 0)
                Direction = 1;
            else
                Direction = 0;
        }

        public override string ToString()   // for debug
        {
            return String.Format("TN={0} TS={1}", iTrackNode, iTrVectorSection);
        }


        public TDBTraveller(TDBTraveller copy)
        {
            TDB = copy.TDB;
            TSectionDat = copy.TSectionDat;
            TileX = copy.TileX;
            TileZ = copy.TileZ;
            X = copy.X;
            Y = copy.Y;
            Z = copy.Z;
            AX = copy.AX;
            AY = copy.AY;
            AZ = copy.AZ;
            Offset = copy.Offset;
            pDirection = copy.pDirection;
            iTrackNode = copy.iTrackNode;
            iTrVectorSection = copy.iTrVectorSection;
            TS = copy.TS;
            TN = copy.TN;
            TVS = copy.TVS;
        }

        public TDBTraveller( BinaryReader inf )
        {
            TDB = Program.Simulator.TDB;
            TSectionDat = Program.Simulator.TSectionDat;
            TileX = inf.ReadInt32();
            TileZ = inf.ReadInt32();
            X = inf.ReadSingle();
            Y = inf.ReadSingle();
            Z = inf.ReadSingle();
            AX = inf.ReadSingle();
            AY = inf.ReadSingle();
            AZ = inf.ReadSingle();
            Offset = inf.ReadSingle();
            pDirection = inf.ReadInt32();
            iTrackNode = inf.ReadInt32();
            iTrVectorSection = inf.ReadInt32();
            TS = TSectionDat.TrackSections[inf.ReadInt32()];
            TN = TDB.TrackDB.TrackNodes[inf.ReadInt32()];
            TVS = TN.TrVectorNode.TrVectorSections[inf.ReadInt32()];
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(TileX);
            outf.Write(TileZ);
            outf.Write(X);
            outf.Write(Y);
            outf.Write(Z);
            outf.Write(AX);
            outf.Write(AY);
            outf.Write(AZ);
            outf.Write(Offset);
            outf.Write(pDirection);
            outf.Write(iTrackNode);
            outf.Write(iTrVectorSection);
            outf.Write(TSectionDat.TrackSections.IndexOf(TS));
            outf.Write(TDB.TrackDB.TrackNodesIndexOf(TN));
            outf.Write(TN.TrVectorNode.TrVectorSectionsIndexOf(TVS));
        }

        /// <summary>
        /// Advance to the specified point.  If the point isn't ahead
        /// along the track, return false and don't advance.
        /// </summary>
        public bool MoveTo(int tileX, int tileZ, float wx, float wy, float wz)
        {
            TDBTraveller copy = new TDBTraveller(this);

            float distance = DistanceTo(tileX, tileZ, wx, wy, wz, ref copy);

            if (distance < -0.01)  // -ve distance means we didn't find it.
                return false;

            TDB = copy.TDB;
            TSectionDat = copy.TSectionDat;
            TileX = copy.TileX;
            TileZ = copy.TileZ;
            X = copy.X;
            Y = copy.Y;
            Z = copy.Z;
            AX = copy.AX;
            AY = copy.AY;
            AZ = copy.AZ;
            Offset = copy.Offset;
            pDirection = copy.pDirection;
            iTrackNode = copy.iTrackNode;
            iTrVectorSection = copy.iTrVectorSection;
            TS = copy.TS;
            TN = copy.TN;
            TVS = copy.TVS;
            return true;
        }

        /// <summary>
        /// Return the distance along the track to the specified point. 
        /// Observe the current settings for the switch tracks.
        /// If the point isn't located ahead along the track, then return -1;
        /// </summary>
        public float DistanceTo(int tileX, int tileZ, float wx, float wy, float wz)
        {
            TDBTraveller traveller = new TDBTraveller(this);
            return DistanceTo(tileX, tileZ, wx, wy, wz, ref traveller);
        }

        public float DistanceTo(int tileX, int tileZ, float wx, float wy, float wz, ref TDBTraveller traveller)
        {
            float accumulatedDistance = 0;

            while (true)  // we will exit this loop when we find the waypoint or run out of track
            {
                float initialOffset = traveller.Offset;
                if (traveller.Direction > 0)  // moving forward
                {
                    if (traveller.TS.SectionCurve != null) // If we are moving forward in a curve
                    {
                        if (traveller.CurvedSectionInit(tileX, tileZ, wx, wz))
                        {
                            // offset is in radians for curves
                            if (traveller.Offset < initialOffset)
                                return -1;  // the waypoint is behind us
                            float radiansTravelled = traveller.Offset - initialOffset;
                            accumulatedDistance += radiansTravelled * traveller.TS.SectionCurve.Radius;
                            return accumulatedDistance;
                        }
                        else // it wasn't in this section so just add the length of the section to the distance travelled.
                        {
                            float radiansTravelled = Math.Abs(MSTSMath.M.Radians(traveller.TS.SectionCurve.Angle)) - initialOffset;
                            accumulatedDistance += radiansTravelled * traveller.TS.SectionCurve.Radius;
                        }
                    }
                    else // We are moving forward in a straight
                    {
                        if (traveller.StraightSectionInit(tileX, tileZ, wx, wz))
                        {
                            // offset is in meters for straights
                            if (traveller.Offset < initialOffset)
                                return -1; // the waypoint is behind us
                            float metersTravelled = traveller.Offset - initialOffset;
                            accumulatedDistance += metersTravelled;
                            return accumulatedDistance;
                        }
                        else // it wasn't in this section so just add the length of the section to the distance travelled
                        {
                            float metersTravelled = traveller.TS.SectionSize.Length - initialOffset;
                            accumulatedDistance += metersTravelled;
                        }

                    }
                }
                else // we are moving backwards
                {
                    if (traveller.TS.SectionCurve != null) // If we are moving backward in a curve
                    {
                        if (traveller.CurvedSectionInit(tileX, tileZ, wx, wz))
                        {
                            // offset is in radians for curves
                            if (traveller.Offset > initialOffset)
                                return -1;  // the waypoint is behind us
                            float radiansTravelled = initialOffset - traveller.Offset;
                            accumulatedDistance += radiansTravelled * traveller.TS.SectionCurve.Radius;
                            return accumulatedDistance;
                        }
                        else // it wasn't in this section so just add the length of the section to the distance travelled.
                        {
                            float radiansTravelled = initialOffset;
                            accumulatedDistance += radiansTravelled * traveller.TS.SectionCurve.Radius;
                        }
                    }
                    else // We are moving backward in a straight
                    {
                        if (traveller.StraightSectionInit(tileX, tileZ, wx, wz))
                        {
                            // offset is in meters for straights
                            if (traveller.Offset > initialOffset)
                                return -1; // the waypoint is behind us
                            float metersTravelled = initialOffset - traveller.Offset;
                            accumulatedDistance += metersTravelled;
                            return accumulatedDistance;
                        }
                        else // it wasn't in this section so just add the length of the section to the distance travelled
                        {
                            float metersTravelled = initialOffset;
                            accumulatedDistance += metersTravelled;
                        }

                    }
                } // end else moving backwards


                // if we got to here, the waypoint isn't in the current track section
                // so move on to the next section

                if (!traveller.NextSection())
                    return -1;   // there's no more sections.

                if (traveller.TN.TrJunctionNode != null)
                {
                    // how far away are we?
                    float dx = traveller.X - wx;
                    dx += (traveller.TileX - TileX) * 2048;
                    float dz = traveller.Z - wz;
                    dz += (traveller.TileZ - TileZ) * 2048;
                    float dy = traveller.Y - wy;
                    if (Math.Abs(dx) + Math.Abs(dz) + Math.Abs(dy) < 0.1)  // we found it at this junction
                        return accumulatedDistance;
                    traveller.NextSection();  // we are at a junction - move past it
                }

                if (traveller.TN.TrEndNode != null)
                    return -1;   // we are at the end and didn't find the waypoint


            } // end while(true )

        }

        public TrJunctionNode TrJunctionNodeBehind()
        {
            TDBTraveller traveller = new TDBTraveller(this);
            traveller.ReverseDirection();
            return NextTrJunctionNode(traveller);
        }

        public TrJunctionNode TrJunctionNodeAhead()
        {
            TDBTraveller traveller = new TDBTraveller(this);
            return NextTrJunctionNode(traveller);
        }

        public TrJunctionNode NextTrJunctionNode(TDBTraveller traveller)
        {
            while (traveller.NextSection())
            {
                if (traveller.TN.TrJunctionNode != null)
                    return traveller.TN.TrJunctionNode;
            }
            return null;
        }

        public TDBTraveller(int tileX, int tileZ, float wx, float wz, int direction, TDBFile tdb, TSectionDatFile tsectiondat)
        // Initialize a traveller based on coordinates relative to the specified tile center
        // use the specified track database file
        // initial direction 1 = forward;
        {
            TDB = tdb;
            TSectionDat = tsectiondat;


            TN = null;
            TS = null;

            for (iTrackNode = 1; iTrackNode < TDB.TrackDB.TrackNodes.Length; ++iTrackNode)
            {
                TN = TDB.TrackDB.TrackNodes[iTrackNode];

                if (TN.TrVectorNode != null)
                {
                    // TODO, we could do an additional cull here by calculating a bounding sphere for each node as they are being read.

                    for (iTrVectorSection = 0; iTrVectorSection < TN.TrVectorNode.TrVectorSections.Length; ++iTrVectorSection)
                    {
                        TVS = TN.TrVectorNode.TrVectorSections[iTrVectorSection];

                        // Note dynamic track will have  TVS.SectionIndex >= 40000 
                        TS = TSectionDat.TrackSections.Get(TVS.SectionIndex);
                        if (TS == null) continue;

                        if (TS.SectionCurve != null)
                        // Its a curve
                        {
                            if (CurvedSectionInit(tileX, tileZ, wx, wz))
                            {
                                Direction = direction;
                                return;
                            }
                        }
                        else
                        // Its a straight
                        {
                            if (StraightSectionInit(tileX, tileZ, wx, wz))
                            {
                                Direction = direction;
                                return;
                            }
                        }

                    }
                }
            }
            throw (new System.Exception("The car is on a track section that could not be found in the TDB file."));
        }


        public float Move(float distanceToGo)
        // moves the traveller along the track traversing the track database
        // direction was set by the constructor
        // on success returns 0.0
        // on failure, returns distance remaining after the move, ie if it runs into an obstacle
        // TODO - must remove the trig from these calculations
        // Note- distanceToGo can be positive or negative relative to the direction of travel 
        {
            bool negative = distanceToGo < 0;
            if (negative)
            {
                ReverseDirection();
                distanceToGo *= -1;
            }

            while (true)
            {
                distanceToGo = MoveInSegment(distanceToGo);
                if (distanceToGo < 0.001) break; // traversed the required distance
                if (!NextSection())
                    break;  // no more sections
            }

            if (negative)
            {
                ReverseDirection();
                distanceToGo *= -1;
            }

            return distanceToGo;
        }

        // If wx,wz is in this curved section, init the traveller to this location
        // Initial direction is forward
        // otherwise return false
        bool CurvedSectionInit(int tileX, int tileZ, float wx, float wz)
        {
            // get wx and wz relative to the tile that the section starts on
            wx += (tileX - TVS.TileX) * 2048;
            wz += (tileZ - TVS.TileZ) * 2048;
            float sx = TVS.X;  // sx and sz are relative to the track section's home tile
            float sz = TVS.Z;

            // do a preliminary cull based on a bounding square around the track section
            // bounding square width is Radians of curvature * curve radius, but no more than 2 * the curve radius, plus a little
            // the square is actually 2 x width wide
            float boundingWidth = TS.SectionCurve.Radius * (float)Math.Min(Math.Abs(M.Radians(TS.SectionCurve.Angle)), 2f) + 1f;
            float dx = Math.Abs(wx - sx);
            float dz = Math.Abs(wz - sz);
            if (dx > boundingWidth || dz > boundingWidth) return false;

            // to simplify the math, translate coordinates such that the track section starts at 0,0
            wx -= sx;
            wz -= sz;

            // and rotate the coordinates such that the track section starts out pointing north ( +z )
            MSTSMath.M.Rotate2D(TVS.AY, ref wx, ref wz);

            // and flip them so the track curves to the right
            if (TS.SectionCurve.Angle < 0)
                wx *= -1;

            // How far off the track centerline are we?
            // First find the center of curvature, cz = 0, cx = radius 
            float cx = TS.SectionCurve.Radius;
            // Then compute the distance of this point to that center point
            dx = wx - cx;
            double lat = Math.Sqrt(dx * dx + wz * wz) - TS.SectionCurve.Radius;

            // note the wx,wz position represents the center of the car 
            // if the car has some overhang, than it will be offset toward the center of curvature
            // and won't be right along the center line.  I'll have to add some allowance for this
            // and accept a hit if it is within 2.5 meters of the center line
            // this was determined experimentally to match MSTS's 'capture range'
            if (Math.Abs(lat) > 2.5) return false; // we are not along the track centerline

            // Ensure we are in the top right quadrant, otherwise our math goes wrong
            if (wz < 0.02) return false;  // and we can't be 'behind' the start of the circle
            if (wx + 0.001 > TS.SectionCurve.Radius) return false;  // we can't be to the right of center, 90' is the limit
            if (wz + 0.001 > TS.SectionCurve.Radius) return false;  // and we can't be outside the circle
            float radiansAlongCurve = (float)Math.Asin(wz / TS.SectionCurve.Radius);

            // Ensure we are not beyond the end of the section
            float lon = radiansAlongCurve * TS.SectionCurve.Radius;
            float trackSectionLength = MSTSMath.M.Radians(Math.Abs(TS.SectionCurve.Angle)) * TS.SectionCurve.Radius;
            if (lon > trackSectionLength + 0.002) return false;
            if (lon < -0.002) return false;

            Offset = 0;
            TileX = TVS.TileX;
            TileZ = TVS.TileZ;
            X = TVS.X;
            Y = TVS.Y;
            Z = TVS.Z;
            AX = TVS.AX;
            AY = TVS.AY;
            AZ = TVS.AZ;
            pDirection = 1;
            MoveInCurvedSegment(lon, 1, TS);
            return true;
        }

        /// <summary>
        /// If wx,wz is in this straight section, init the traveller to this location
        /// otherwise return false
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wz"></param>
        /// <returns></returns>
        bool StraightSectionInit(int tileX, int tileZ, float wx, float wz)
        {
            // get wx and wz relative to the tile that the section starts on
            wx += (tileX - TVS.TileX) * 2048;
            wz += (tileZ - TVS.TileZ) * 2048;
            float sx = TVS.X;  // sx and sz are relative to the track section's home tile
            float sz = TVS.Z;

            // Do a preliminary cull based on a bounding square around the track section
            // bounding square width is equivalent to the track section length
            // the square is actually 2 x width wide + a little
            // If the point is far from this track section, return false
            float boundingWidth = TS.SectionSize.Length + 2;
            float dx = Math.Abs(wx - sx);
            float dz = Math.Abs(wz - sz);
            if (dx > boundingWidth || dz > boundingWidth) return false;

            // The point wasn't culled, so it must be close to the track section 
            // Do a detailed calculation to ensure the point wx,wz falls along this section of track
            // First compute its distance away from the centerline ( lat ) 
            // and the distance along the beginning of the track section ( lon )
            // If it is too far away, return false
            float lat, lon;
            M.Survey(sx, sz, TVS.AY, wx, wz, out lon, out lat);

            if (Math.Abs(lat) > 1.5    // near the centerline - I've found areas where its off the centerline due to car overhang
                || lon < -0.002			      // before the beginning of this track section
                || lon > TS.SectionSize.Length + 0.002)  // beyond the end of the track
                return false;

            // The point is on this track so initialize the traveller on this section
            Offset = 0;
            TileX = TVS.TileX;
            TileZ = TVS.TileZ;
            X = TVS.X;
            Y = TVS.Y;
            Z = TVS.Z;
            AX = TVS.AX;
            AY = TVS.AY;
            AZ = TVS.AZ;
            pDirection = 1; // forward

            MoveInStraightSegment(lon, 1, TS);

            return true;
        }

        float MoveInCurvedSegment(float distanceToGo, int direction, TrackSection TS)
        // code assumes distanctToGo is positive relative to specified direction.
        {
            // for curved segments, offset is always positive, 

            if (direction > 0)  // moving forwared
            {
                float desiredTurnRadians = M.TurnAngleRadians(TS.SectionCurve.Radius * 2.0f, distanceToGo);
                float sectionTurnRadians = Math.Abs(MSTSMath.M.Radians(TS.SectionCurve.Angle));

                if (Offset + desiredTurnRadians > sectionTurnRadians)
                    desiredTurnRadians = sectionTurnRadians - Offset;

                // for curves ,offset is in radians from beginning of curve.
                Offset += desiredTurnRadians;

                // TODO - consider using circumference length instead - it is a simpler calculation 
                float distanceMoved = M.CordLength(TS.SectionCurve.Radius * 2.0f, desiredTurnRadians);

                TWorldPosition P = new TWorldPosition(X, Y, Z);
                TWorldDirection D = new TWorldDirection();
                D.SetBearing(AY);

                if (TS.SectionCurve.Angle < 0)   // turning left
                    desiredTurnRadians *= -1;

                D.Rotate(desiredTurnRadians / 2.0f);
                P.Move(D, distanceMoved);
                D.Rotate(desiredTurnRadians / 2.0f);

                X = P.X;
                Y = P.Y;
                Z = P.Z;

                // Normalize 
                MSTSMath.M.NormalizeRadians(ref AX);

                Y -= AX * (float)distanceMoved;  // equivalent to sin(ax) * distance    when ax is small

                AY += desiredTurnRadians;

                return distanceToGo - distanceMoved;
            }
            else // moving backward
            {

                float desiredTurnRadians = M.TurnAngleRadians(TS.SectionCurve.Radius * 2.0f, distanceToGo);
                float sectionTurnRadians = Math.Abs(MSTSMath.M.Radians(TS.SectionCurve.Angle));

                if (desiredTurnRadians > Offset)  // can't go back past beginning
                    desiredTurnRadians = Offset;

                // for curves ,offset is in radians from beginning of curve.
                Offset -= desiredTurnRadians;

                // TODO - consider using circumference length instead - it is a simpler calculation 
                float distanceMoved = M.CordLength(TS.SectionCurve.Radius * 2.0f, desiredTurnRadians);

                TWorldPosition P = new TWorldPosition(X, Y, Z);
                TWorldDirection D = new TWorldDirection();
                D.SetBearing(AY);

                if (TS.SectionCurve.Angle < 0)   // turning left
                    desiredTurnRadians *= -1;

                D.Rotate(-desiredTurnRadians / 2.0f);
                P.Move(D, distanceMoved);
                D.Rotate(-desiredTurnRadians / 2.0f);

                X = P.X;
                Y = P.Y;
                Z = P.Z;

                // Normalize 
                MSTSMath.M.NormalizeRadians(ref AX);

                Y -= AX * (float)distanceMoved;  // equivalent to sin(ax) * distance    when ax is small

                AY -= desiredTurnRadians;

                return distanceToGo - distanceMoved;
            }
        }

        float MoveInStraightSegment(float distanceToGo, float direction, TrackSection TS)
        // code assumes distanctToGo is positive relative to current direction.
        {
            float distance = distanceToGo;

            if (direction == 0)
                distance *= -1;

            // Limit it to the segment size
            if (Offset + distance > TS.SectionSize.Length)
                distance = TS.SectionSize.Length - Offset;
            else if (Offset + distance < 0)
                distance = -Offset;

            Vector3 p = new Vector3(0f, 0f, distance);
            p = Vector3.Transform(p, Matrix.CreateFromYawPitchRoll(TVS.AY, TVS.AX, TVS.AZ));

            X += p.X;
            Y += p.Y;
            Z += p.Z;

            Offset += distance;

            if (direction == 0)
                distance *= -1;

            return distanceToGo - distance;
        }

        float MoveInInfiniteSegment(float distanceToGo)
        // temp code used as default at end of track or through dynamic track
        // code assumes distanctToGo is positive relative to current direction.
        {
            float distance = distanceToGo;

            if (Direction == 0) // reverse , ie towards origin of infinite segment
            {
                if (distance > Offset)  // we can't go past the origin
                    distance = Offset;
            }

            // TODO replace with matrix math
            TWorldPosition P = new TWorldPosition(X, Y, Z);
            TWorldDirection D = new TWorldDirection();
            D.SetAngles(AY, -AX);
            P.Move(D, distance);
            X = P.X;
            Y = P.Y;
            Z = P.Z;

            if (Direction > 0)
                Offset += distance;
            else
                Offset -= distance;

            return distanceToGo - distance;
        }

        /// <summary>
        /// To simplify the calculations, all distances must be positive
        /// </summary>
        /// <param name="distanceToGo"></param>
        /// <returns></returns>
        float MoveInSegment(float distanceToGo)
        // returns distance to go after movement
        // don't move past the end of the current sement
        {
            Debug.Assert(distanceToGo >= -.00001);
            if (TN.TrJunctionNode != null)  // if we are at a junction node
                return distanceToGo;        //    they have zero length, so transit distance is 0
            if (TS == null) // else we are at a end of track node
                return MoveInInfiniteSegment(distanceToGo);
            if (TS.SectionCurve != null)
                return MoveInCurvedSegment(distanceToGo, pDirection, TS);
            else
                return MoveInStraightSegment(distanceToGo, pDirection, TS);
        }


        /// <summary>
        /// Advance into the next section
        /// which could be a TrVectorSection in a TrVectorNode, a TrJunctionNode, or a TrEndNode.
        /// </summary>
        /// <returns></returns>
        public bool NextSection()
        {

            if (TN.TrVectorNode != null)  // we were in a track node that contains multiple sections
                if (NextTrVectorSection())  // try to advance to the next section in the node
                    return true;
            return NextTrackNode();  // otherwise we will have to move to the next node
        }

        /// <summary>
        /// Assume the TDBTraveller is traversing a TrVectorNode
        /// Advance to the next TrVectorSection or return false if there are no more sections
        /// in the current direction of travel.
        /// </summary>
        /// <returns></returns>
        bool NextTrVectorSection()
        {
            if (pDirection > 0)   // if we are moving forward
            {
                if (iTrVectorSection >= TN.TrVectorNode.TrVectorSections.Length - 1) return false; // switch to the next node

                ++iTrVectorSection;
                TVS = TN.TrVectorNode.TrVectorSections[iTrVectorSection];
                TS = TSectionDat.TrackSections.Get(TVS.SectionIndex);
                if (TS == null) return false;
                Offset = 0;

                TileX = TVS.TileX;
                TileZ = TVS.TileZ;
                X = TVS.X;
                Y = TVS.Y;
                Z = TVS.Z;
                AX = TVS.AX;
                AY = TVS.AY;
                AZ = TVS.AZ;
            }
            else  // we are moving backwards
            {
                if (TVS != null)
                {
                    // we can recalibrate our position to the beginning of this section before moving back
                    TileX = TVS.TileX;
                    TileZ = TVS.TileZ;
                    X = TVS.X;
                    Y = TVS.Y;
                    Z = TVS.Z;
                    AX = TVS.AX;
                    AY = TVS.AY;
                    AZ = TVS.AZ;
                    AY += (float)Math.PI;  // we are moving backwards
                    AX *= -1;
                }

                if (iTrVectorSection <= 0) return false;  // we'll have to go to the next node

                --iTrVectorSection;
                TVS = TN.TrVectorNode.TrVectorSections[iTrVectorSection];
                TS = TSectionDat.TrackSections.Get(TVS.SectionIndex);
                if (TS == null) return false;

                // as we transition across, we must adopt the pitch of the new section of track
                AX = -TVS.AX;
                AZ = TVS.AZ;


                if (TS.SectionCurve == null)
                {
                    AY = TVS.AY;
                    AY += (float)Math.PI;  // we are moving backwards

                    Offset = TS.SectionSize.Length;  // straight section
                }
                else
                {
                    Offset = Math.Abs(M.Radians(TS.SectionCurve.Angle));  // curve section - offsets are in radians and always positive
                }

                MSTSMath.M.NormalizeRadians(ref AY);
                MSTSMath.M.NormalizeRadians(ref AX);
            }

            // Normalize coordinates to be on the same tile as this track vector section
            while (TileX > TVS.TileX) { X += 2048; --TileX; }
            while (TileX < TVS.TileX) { X -= 2048; ++TileX; }
            while (TileZ > TVS.TileZ) { Z += 2048; --TileZ; }
            while (TileZ < TVS.TileZ) { Z -= 2048; ++TileZ; }

            return true;
        }

        /// <summary>
        /// Advance the TDBTraveller into the next track node.
        /// which could be a TrVectorNode, a TrJunctionNode, or a TrEndNode
        /// </summary>
        /// <returns></returns>
        bool NextTrackNode()
        {
            int iPreviousTrackNode = this.iTrackNode;

            if (TN.TrJunctionNode != null)  // are we coming from a junction node
            {
                // TODO add recalibration from data in UiD field
                int iPin;
                // we're at a junction, take the selected path
                if (pDirection == 0) iPin = 0; else iPin = (int)TN.Inpins + TN.TrJunctionNode.SelectedRoute;
                TrPin TrPin = TN.TrPins[iPin];  // this points to the next node ie TrPin ( 49 0 )
                iTrackNode = TrPin.Link;
                TN = TDB.TrackDB.TrackNodes[iTrackNode];  // we are at the new node
                pDirection = TrPin.Direction;
            }
            else if (TN.TrEndNode != null) // are we coming from an end node
            {
                // if we are moving forward then there is no place to go
                if (pDirection == 1) return false;

                // TODO add recalibration from data in UiD field
                TrPin TrPin = TN.TrPins[0];  // this points to the next node ie TrPin ( 49 0 )
                iTrackNode = TrPin.Link;
                TN = TDB.TrackDB.TrackNodes[iTrackNode];  // we are at the new node
                pDirection = TrPin.Direction;
            }
            else // we must be coming from a TrSectionNode ( a list of sections
            {
                int iPin;
                if (pDirection == 0) iPin = 0; else iPin = 1;
                TrPin TrPin = TN.TrPins[iPin];  // this points to the next node ie TrPin ( 49 0 )
                iTrackNode = TrPin.Link;
                TN = TDB.TrackDB.TrackNodes[iTrackNode];  // we are at the new node
                pDirection = TrPin.Direction;
            }

            // figure out which PIN we entered on
            for (iEntryPIN = 0; iEntryPIN < 5; ++iEntryPIN)
                if (TN.TrPins[iEntryPIN].Link == iPreviousTrackNode)
                    break;

            // at this point TN points to the new node, with pDirection set as required
            // now set up TVS, TS, iTrVectorSection, Offset
            if (TN.TrVectorNode != null)  // if the new node is a TrVectorSections node, we have some more work to do
            {
                if (pDirection > 0)  // if we are moving forward start with the first TrVectorSection
                {
                    iTrVectorSection = -1;
                    NextTrVectorSection();
                }
                else // moving back, start with the last TrVectorSection
                {
                    iTrVectorSection = TN.TrVectorNode.TrVectorSections.Length;
                    NextTrVectorSection();
                }

            }
            else // new section is a TrEndNodes and TrJunctionNodes so we set these to the defaults.
            {
                TVS = null;
                TS = null;
                iTrVectorSection = 0;
                Offset = 0;
            }
            return true;
        } // Next Track Node

        /// <summary>
        /// Returns directed distance between two train ends represented by this traveller and other traveller.
        /// Returns 1 if the distance is >= 1.
        /// Returns a positive value if the corresponding trains do not overlap and negative if they do.
        /// rear should be true if this traveller is the rear end of a train.
        /// </summary>
        /// <returns></returns>
        public float OverlapDistanceM(TDBTraveller other, bool rear)
        {
            float dx = X - other.X + 2048 * (TileX - other.TileX);
            float dz = Z - other.Z + 2048 * (TileZ - other.TileZ);
            if (dx * dx + dz * dz > 1)
                return 1;
            float dot = dx * (float)Math.Sin(AY) + dz * (float)Math.Cos(AY);
            //Console.WriteLine("overlap {0} {1} {2} {3} {4} {5} {6}", dot, dx, dz, AY, flip, Math.Cos(AY), Math.Sin(AY));
            return rear ? dot : -dot;
        }
    }

}
