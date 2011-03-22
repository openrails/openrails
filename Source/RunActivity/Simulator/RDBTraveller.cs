/*  RDBTraveller
 * 
 *  A Road Track Database Traveller represents a specific location and direction in the track database.
 *  Think of it like a virtual truck or bogie that can travel the track database.
 *  Constructing a RDBTraveller involves placing it on a road in the RDB.
 *  It then provides methods for moving along the road track.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
///
/// Principal Author:
///    ???
/// Contributors:
///    Copied from TDBTraveller by Jijun Tang
///     
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
	public class RDBTraveller
	{
		RDBFile RDB;
		TSectionDatFile TSectionDat;

		// World position and orientation of the RDB Traveller
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


		// RDB database location of the traveller
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

		public RDBTraveller(RDBTraveller copy)
		{
			RDB = copy.RDB;
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

		public void Copy(RDBTraveller copy)
		{
			RDB = copy.RDB;
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
		public RDBTraveller(BinaryReader inf)
		{
			RDB = Program.Simulator.RDB;
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
			TS = TSectionDat.TrackSections[inf.ReadUInt32()];
			TN = RDB.RoadTrackDB.TrackNodes[inf.ReadInt32()];
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
			outf.Write(TS.SectionIndex);
			outf.Write(RDB.RoadTrackDB.TrackNodesIndexOf(TN));
			outf.Write(TN.TrVectorNode.TrVectorSectionsIndexOf(TVS));
		}

		/// <summary>
		/// Advance to the specified point.  If the point isn't ahead
		/// along the track, return false and don't advance.
		/// </summary>
		public bool MoveTo(int tileX, int tileZ, float wx, float wy, float wz)
		{
			RDBTraveller copy = new RDBTraveller(this);

			float distance = DistanceTo(tileX, tileZ, wx, wy, wz, ref copy);

			if (distance < -0.01)  // -ve distance means we didn't find it.
				return false;

			RDB = copy.RDB;
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
            return DistanceTo(tileX, tileZ, wx, wy, wz, float.MaxValue);
        }

        public float DistanceTo(int tileX, int tileZ, float wx, float wy, float wz, float maxDistance)
        {
            var traveller = new RDBTraveller(this);
            return DistanceTo(tileX, tileZ, wx, wy, wz, ref traveller, maxDistance);
        }

        public float DistanceTo(int tileX, int tileZ, float wx, float wy, float wz, ref RDBTraveller traveller)
        {
            return DistanceTo(tileX, tileZ, wx, wy, wz, ref traveller, float.MaxValue);
        }

        public float DistanceTo(int tileX, int tileZ, float wx, float wy, float wz, ref RDBTraveller traveller, float maxDistance)
        {
            float accumulatedDistance = 0;

            while (accumulatedDistance < maxDistance)  // we will exit this loop when we find the waypoint or run out of track
            {
				float initialOffset = traveller.Offset;
				if (traveller.TS != null && traveller.Direction > 0)  // moving forward
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
				else if (traveller.TS != null) // we are moving backwards
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

				if (traveller.TN.TrEndNode)
					return -1;   // we are at the end and didn't find the waypoint
			}

            return -1;
		}

		public TrJunctionNode TrJunctionNodeBehind()
		{
			RDBTraveller traveller = new RDBTraveller(this);
			traveller.ReverseDirection();
			return NextTrJunctionNode(traveller);
		}

		public TrJunctionNode TrJunctionNodeAhead()
		{
			RDBTraveller traveller = new RDBTraveller(this);
			return NextTrJunctionNode(traveller);
		}

		public TrJunctionNode NextTrJunctionNode(RDBTraveller traveller)
		{
			while (traveller.NextSection())
			{
				if (traveller.TN.TrJunctionNode != null)
					return traveller.TN.TrJunctionNode;
			}
			return null;
		}

		public RDBTraveller(int tileX, int tileZ, float wx, float wz, int direction, RDBFile rdb, TSectionDatFile tsectiondat)
		// Initialize a traveller based on coordinates relative to the specified tile center
		// use the specified track database file
		// initial direction 1 = forward;
		{
			RDB = rdb;
			TSectionDat = tsectiondat;


			TN = null;
			TS = null;

			for (iTrackNode = 1; iTrackNode < RDB.RoadTrackDB.TrackNodes.Length; ++iTrackNode)
			{
				TN = RDB.RoadTrackDB.TrackNodes[iTrackNode];

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
			throw new InvalidDataException("The car is on a track section that could not be found in the RDB file.");
		}


		/// <summary>
		/// Creates a forward-travelling RDBTraveller.
		/// </summary>
		/// <param name="trackNode">The TrackNode from which to create the RDBTraveller.</param>
		/// <param name="vectorSection">The TrackSection from which to create the RDBTraveller.</param>
		/// <param name="rdb"></param>
		/// <param name="tsectiondat"></param>
		public RDBTraveller(TrackNode trackNode, TrVectorSection vectorSection, RDBFile rdb, TSectionDatFile tsectiondat)
		{
			RDB = rdb;
			TSectionDat = tsectiondat;

			Direction = 1; // forward

			TN = trackNode;
			TVS = vectorSection;
			TS = TSectionDat.TrackSections.Get(TVS.SectionIndex);

			if (TS.SectionCurve != null)
			{
				CurvedSectionInit(TVS.TileX, TVS.TileZ, TVS.X, TVS.Z);
			}
			else
			{
				StraightSectionInit(TVS.TileX, TVS.TileZ, TVS.X, TVS.Z);
			}

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

		/// <summary>
		/// MoveInCurvedSegment attempts to move traveler through track section.
		/// </summary>
		/// <param name="distanceToGo">Target distance (>0) to move along curve.</param>
		/// <param name="direction">Forward (1) or reverse (0).</param>
		/// <param name="TS">Track section object.</param>
		/// <returns>Remaining distance to go.</returns>
		float MoveInCurvedSegment(float distanceToGo, int direction, TrackSection TS)
		// Code assumes distanceToGo is positive relative to specified direction.
		// This implementation uses the same interpolation methodology as dynamic track visualization.
		{
			float desiredTurnRadians = distanceToGo / TS.SectionCurve.Radius;
			float sectionTurnRadians = Math.Abs(TS.SectionCurve.Angle *
												(float)(Math.PI / 180.0));

			bool fwd = direction > 0; // Moving forward
			// "Handedness" Convention: A right-hand curve (TS.SectionCurve.Angle > 0) curves 
			// to the right when moving forward.
			bool rh = TS.SectionCurve.Angle > 0;

			if (fwd) // Moving forward
			{
				// Resolve how far (radians) to progress around the curve.
				// If the total progression is more than sectionTurnRadians, 
				// back off to end of curve.
				// Offset, radians from beginning of curve, is positive and increasing. 
				if (Offset + desiredTurnRadians > sectionTurnRadians)
					desiredTurnRadians = sectionTurnRadians - Offset;
				Offset += desiredTurnRadians;   // Update Offset
			}
			else // Moving backwards
			{
				// Offset, radians from beginning of curve, is positive and decreasing.
				if (desiredTurnRadians > Offset)  // Can't go back past beginning
					desiredTurnRadians = Offset;
				Offset -= desiredTurnRadians; // Update Offset
			}

			float sgn = rh ? -1.0f : 1.0f; // sgn = -1.0f right-hand curve; sgn = 1.0f left-hand curve
			float radius = TS.SectionCurve.Radius;
			Vector3 vPC_O = sgn * radius * Vector3.Left; // Vector from PC to O
			Matrix rot = Matrix.CreateRotationY(sgn * Offset); // Rotate by Offset
			Matrix XNAMatrix = Matrix.CreateFromYawPitchRoll(-TVS.AY, -TVS.AX, TVS.AZ); // World transform
			Vector3 dummy; // Not used here
			// Shared method returns displacement from present world position and, by reference,
			// local position in x-z plane of end of this section
			Vector3 displacement = MSTSInterpolateAlongCurve(Vector3.Zero, vPC_O, rot, XNAMatrix,
																out dummy);
			X = TVS.X + displacement.X;
			Y = TVS.Y + displacement.Y;
			Z = TVS.Z - displacement.Z;

			float distanceMoved = TS.SectionCurve.Radius * desiredTurnRadians; // Along arc

			if (fwd)  // moving forward
			{
				AY += desiredTurnRadians; // Update orientation of tangent at (X, Y, Z)
			}
			else // moving backward
			{
				AY -= desiredTurnRadians; // Update orientation of tangent at (X, Y, Z)
			}

			return distanceToGo - distanceMoved; // Return remainder as new distanceToGo
		} // end MoveInCurvedSegment

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
		public static Vector3 MSTSInterpolateAlongCurve(Vector3 vPC, Vector3 vPC_O, Matrix mRotY, Matrix mWorld,
									out Vector3 vP)
		{
			// Shared method returns displacement from present world position and, by reference,
			// local position in x-z plane of end of this section
			Vector3 vO_P = Vector3.Transform(-vPC_O, mRotY); // Rotate O_PC to O_P
			vP = vPC + vPC_O + vO_P; // Position of P relative to PC
			return Vector3.Transform(vP, mWorld); // Transform to world coordinates and return as displacement.
		} // end MSTSInterpolateAlongCurve

		float MoveInStraightSegment(float distanceToGo, float direction, TrackSection TS)
		// Code assumes distanctToGo is positive relative to current direction.
		// This implementation uses the same interpolation methodology as dynamic track visualization.
		{
			float distance = distanceToGo;
			if (direction == 0)
				distance *= -1;

			// Limit it to the segment size
			if (Offset + distance > TS.SectionSize.Length)
				distance = TS.SectionSize.Length - Offset;
			else if (Offset + distance < 0)
				distance = -Offset;

			Matrix XNAMatrix = Matrix.CreateFromYawPitchRoll(TVS.AY, TVS.AX, TVS.AZ);
			Vector3 dummy;
			Vector3 displacement = MSTSInterpolateAlongStraight(Vector3.Zero, Vector3.UnitZ, distance, XNAMatrix,
																	out dummy);
			X += displacement.X;
			Y += displacement.Y;
			Z += displacement.Z;

			Offset += distance;

			if (direction == 0)
				distance *= -1;

			return distanceToGo - distance;
		} // end MoveInStraightSegment

		/// <summary>
		/// MSTSInterpolateAlongStraight interpolates position along a straight stretch.
		/// </summary>
		/// <param name="vP0">Local position vector for starting point P0 in x-z plane.</param>
		/// <param name="vP0_P">Unit vector in direction from P0 to P.</param>
		/// <param name="offset">Distance from P0 to P.</param>
		/// <param name="mWorld">Transformation from local to world coordinates.</param>
		/// <param name="vP">Position vector for desired point(P), returned by reference.</param>
		/// <returns>Displacement vector from P0 to P in world coordinates.</returns>
		public static Vector3 MSTSInterpolateAlongStraight(Vector3 vP0, Vector3 vP0_P, float offset, Matrix mWorld,
															out Vector3 vP)
		{
			vP = vP0 + offset * vP0_P; // Position of desired point in local coordinates.
			return Vector3.Transform(vP, mWorld);
		} // end MSTSInterpolateAlongStraight

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
			if (TN.TrEndNode)
			{
			}
			if (TN.TrVectorNode != null)  // we were in a track node that contains multiple sections
				if (NextTrVectorSection())  // try to advance to the next section in the node
					return true;
			return NextTrackNode();  // otherwise we will have to move to the next node
		}

		/// <summary>
		/// Assume the RDBTraveller is traversing a TrVectorNode
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
		/// Advance the RDBTraveller into the next track node.
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
				TN = RDB.RoadTrackDB.TrackNodes[iTrackNode];  // we are at the new node
				pDirection = TrPin.Direction;
			}
			else if (TN.TrEndNode) // are we coming from an end node
			{
				// if we are moving forward then there is no place to go
				if (pDirection == 1) return false;

				// TODO add recalibration from data in UiD field
				TrPin TrPin = TN.TrPins[0];  // this points to the next node ie TrPin ( 49 0 )
				iTrackNode = TrPin.Link;
				TN = RDB.RoadTrackDB.TrackNodes[iTrackNode];  // we are at the new node
				pDirection = TrPin.Direction;
			}
			else // we must be coming from a TrSectionNode ( a list of sections
			{
				int iPin;
				if (pDirection == 0) iPin = 0; else iPin = 1;
				TrPin TrPin = TN.TrPins[iPin];  // this points to the next node ie TrPin ( 49 0 )
				iTrackNode = TrPin.Link;
				TN = RDB.RoadTrackDB.TrackNodes[iTrackNode];  // we are at the new node
				pDirection = TrPin.Direction;
			}

			// figure out which PIN we entered on
			for (iEntryPIN = 0; iEntryPIN < 5 && iEntryPIN < TN.TrPins.Length; ++iEntryPIN)
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

	}

}
