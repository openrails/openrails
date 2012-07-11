/* AI
 * 
 * Contains code to initialize and control AI trains.
 * Currently, AI trains are created at startup and moved down 1000 meters to make them
 * invisible.  This is done so the rendering code can discover the model it needs to draw.
 * 
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
	public class AITrain : Train
	{
		public int UiD;
		public AIPathNode NextStopNode = null;  // next path node train should stop at
		public float AuthUpdateDistanceM = 0;  // distance to next stop node to ask for longer authorization
		public float NextStopDistanceM = 0;  // distance to next stop node
		public float NextStopTimeS = 0;      // seconds to next stop
		public bool CoupleOnNextStop = false;   // true if cars at next stop to couple onto
		public float MaxDecelMpSS = 1;  // maximum decelleration
		public float MaxAccelMpSS = .5f;// maximum accelleration
		public float MaxSpeedMpS = 10;  // maximum speed
		public double WaitUntil = 0;    // clock time to wait for before next update
		public int StartTime;        // starting time, may be modified by dispatcher
		public int Priority = 0;        // train priority, smaller value is higher priority
		public AI AI;
		public List<AISwitchInfo> SwitchList = new List<AISwitchInfo>();

		public AITrain(Simulator simulator, int uid, AI ai, AIPath path, int start)
            : base(simulator)
		{
			UiD = uid;
			AI = ai;
			Path = path;
			NextStopNode = Path.FirstNode;
			StartTime = start;
			Priority = start % 10;
			TrainType = TRAINTYPE.AI;
		}

		// restore game state
		public AITrain(Simulator simulator, BinaryReader inf)
			: base(simulator, inf)
		{
			UiD = inf.ReadInt32();
			AuthUpdateDistanceM = inf.ReadSingle();
			NextStopDistanceM = inf.ReadSingle();
			NextStopTimeS = inf.ReadSingle();
			CoupleOnNextStop = inf.ReadBoolean();
			MaxDecelMpSS = inf.ReadSingle();
			MaxAccelMpSS = inf.ReadSingle();
			MaxSpeedMpS = inf.ReadSingle();
			WaitUntil = inf.ReadDouble();
			StartTime = inf.ReadInt32();
			Priority = inf.ReadInt32();
			Path = new AIPath(inf);
			NextStopNode = Path.ReadNode(inf);
		}

		// save game state
		public override void Save(BinaryWriter outf)
		{
			base.Save(outf);
			outf.Write(UiD);
			outf.Write(AuthUpdateDistanceM);
			outf.Write(NextStopDistanceM);
			outf.Write(NextStopTimeS);
			outf.Write(CoupleOnNextStop);
			outf.Write(MaxDecelMpSS);
			outf.Write(MaxAccelMpSS);
			outf.Write(MaxSpeedMpS);
			outf.Write(WaitUntil);
			outf.Write(StartTime);
			outf.Write(Priority);
			Path.Save(outf);
			Path.WriteNode(outf, NextStopNode);
		}

		/// <summary>
		/// Update function for a single AI train.
		/// Performs stop processing if a planned stop is made.
		/// Then moves the train, calculates target accelleration and adjusts the controls.
		/// </summary>
		public void AIUpdate(float elapsedClockSeconds, double clockTime)
		{
			if (WaitUntil > clockTime)
				return;
			if ((SpeedMpS <= 0 && NextStopDistanceM < .3) || NextStopDistanceM < 0)
			{
                SpeedMpS = 0;
                foreach (TrainCar tc in Cars) tc.SpeedMpS = 0;
                if (NextStopDistanceM < 0)
				{
					CalculatePositionOfCars(NextStopDistanceM);
					NextStopDistanceM = 0;
				}
				Update(0);   // stop the wheels from moving etc
				AITrainThrottlePercent = 0;
				AITrainBrakePercent = 100;
				if (WaitUntil == 0 && HandleNodeAction(NextStopNode, clockTime))
					return;
				if (NextStopNode.NextMainNode == null && NextStopNode.NextSidingNode == null)
				{
					NextStopNode = null;
					return;
				}
				//if ((TrackAuthority == null || NextStopNode == TrackAuthority.EndNode) && !AI.Dispatcher.RequestAuth(this, false))
				if (TrackAuthority == null || NextStopNode == TrackAuthority.EndNode)
                {
                    AI.Dispatcher.RequestAuth(this, false);
                    //WaitUntil = clockTime + 10;
					//return;
				}
				if (NextStopNode.IsFacingPoint)
					Path.AlignSwitch(NextStopNode.JunctionIndex, GetTVNIndex(NextStopNode));
				else
				{
					AIPathNode prevNode = FindPrevNode(NextStopNode);
					Path.AlignSwitch(NextStopNode.JunctionIndex, GetTVNIndex(prevNode));
				}
				NextStopNode = FindStopNode(NextStopNode, 50);
				if (NextStopNode == null)
					return;
				if (!CalcNextStopDistance(clockTime))
					return;
			}
            else if (SpeedMpS <= 0 && NextStopDistanceM <= 10 && NextStopNode.NextMainNode == null && NextStopNode.NextSidingNode == null)
            {
                NextStopNode = null;
                return;
            }
            /*
			if ( (NextStopDistanceM < AuthUpdateDistanceM || AuthUpdateDistanceM == -1) && 
                (NextStopNode.NextMainNode != null || NextStopNode.NextSidingNode != null) )
			{
				AuthUpdateDistanceM = -1;
				if (AI.Dispatcher.RequestAuth(this, true))
				{
					if (Path.SwitchIsAligned(NextStopNode.JunctionIndex, NextStopNode.IsFacingPoint ? GetTVNIndex(NextStopNode) : FrontTDBTraveller.TrackNodeIndex))
					{
						AIPathNode node = FindStopNode(NextStopNode, 50);
						if (node != null && NextStopNode != node)
						{
							NextStopNode = node;
							CalcNextStopDistance(clockTime);
						}
					}
				}
                else
                {
                    AIPathNode node = FindStopNode(NextStopNode, 50);
                    //Console.WriteLine("authupdate {0} {1} {2} {3}", NextStopNode.ID, node.ID, NextStopNode.Type, node.Type);
                    if (node != null && NextStopNode != node)
                    {
                        NextStopNode = node;
                    }
                    CalcNextStopDistance(clockTime);
                }
				//Console.WriteLine("new next stop distance {0} {1} {2}", UiD, NextStopDistanceM, AuthUpdateDistanceM);
			}
            */
			WaitUntil = 0;
			float prevSpeedMpS = SpeedMpS;
			base.Update(elapsedClockSeconds);
			float dir = AITrainDirectionForward ? 1 : -1;
			float distanceM = dir * SpeedMpS * elapsedClockSeconds;
			NextStopDistanceM -= distanceM;
			float targetMpSS = CalcAccelMpSS();
			//if (NextStopDistanceM < 100)
			//    Console.WriteLine("update {0} {1} {2} {3} {4}", NextStopDistanceM, SpeedMpS, targetMpSS, AITrainThrottlePercent, AITrainBrakePercent);
			if (elapsedClockSeconds > 0)
				AdjustControls(targetMpSS, dir * (SpeedMpS - prevSpeedMpS) / elapsedClockSeconds, dir * elapsedClockSeconds);
		}

        public void TryAdvanceStopNode(double clockTime)
        {
            if (NextStopNode.Type == AIPathNodeType.Other || NextStopNode.Type == AIPathNodeType.SidingEnd ||
                NextStopNode.Type == AIPathNodeType.SidingStart)
            {
                AIPathNode node = FindStopNode(NextStopNode, 50);
                //Console.WriteLine("authupdate {0} {1} {2} {3}", NextStopNode.ID, node.ID, NextStopNode.Type, node.Type);
                if (node != null && NextStopNode != node)
                {
                    NextStopNode = node;
                }
                CalcNextStopDistance(clockTime);
            }
        }

		/// <summary>
		/// Computes the NextStopDistanceM value, i.e. the distance from one end of the train to the NextStopNode.
		/// Returns false and performs the NextStopNode action if its past the train end.
		/// Also checks for possible coupling and sets CoupleOnNextStop and adjusts the distance if any.
		/// </summary>
		private bool CalcNextStopDistance(double clockTime)
		{
			CoupleOnNextStop = false;
            // By GeorgeS
            if (NextStopNode.Type == AIPathNodeType.Other && (NextStopNode.NextMainNode != null || NextStopNode.NextSidingNode != null))
            {
                NextStopDistanceM = 30000;
                AuthUpdateDistanceM = 29000;
                return false;
            }
			WorldLocation wl = NextStopNode.Location;
			Traveller traveller = FrontTDBTraveller;
			if (!AITrainDirectionForward)
				traveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward);

			NextStopDistanceM = traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
			if (NextStopDistanceM < 0 && HandleNodeAction(NextStopNode, clockTime))
				return false;
			foreach (AISwitchInfo sw in SwitchList)
			{
				wl = sw.PathNode.Location;
				sw.DistanceM = NextStopDistanceM - traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
			}
			if (NextStopNode.Type == AIPathNodeType.Stop || NextStopNode.Type == AIPathNodeType.Reverse || NextStopNode.Type == AIPathNodeType.Uncouple)
			{
				int index = NextStopNode.NextMainTVNIndex;
				if (index < 0)
					index = NextStopNode.NextSidingTVNIndex;
				foreach (Train train in AI.Simulator.Trains)
				{
					if (train == this)
						continue;
					if (train.FrontTDBTraveller.TrackNodeIndex == index)
					{
						wl = train.FrontTDBTraveller.WorldLocation;
						float d = traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
						if (d > 0 && d < NextStopDistanceM)
						{
							NextStopDistanceM = d;
							CoupleOnNextStop = true;
						}
					}
					if (train.RearTDBTraveller.TrackNodeIndex == index)
					{
						wl = train.RearTDBTraveller.WorldLocation;
						float d = traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
						if (d > 0 && d < NextStopDistanceM)
						{
							NextStopDistanceM = d;
							CoupleOnNextStop = true;
						}
					}
				}
				if (CoupleOnNextStop == false && NextStopNode.Type == AIPathNodeType.Reverse && NextStopNode.WaitUntil == 0 && NextStopNode.WaitTimeS == 0)
				{
					AIPathNode node = FindStopNode(NextStopNode, 0);
					if (node != null && node.JunctionIndex >= 0)
					{
						wl = node.Location;
						Traveller rtraveller = RearTDBTraveller;
						if (!AITrainDirectionForward)
							rtraveller = new Traveller(FrontTDBTraveller, Traveller.TravellerDirection.Backward);

                        float d = rtraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
						if (d > 0 && d + 1 < NextStopDistanceM && d + 100 > NextStopDistanceM)
							NextStopDistanceM = d + 1;
					}
				}
				return true;
			}
			NextStopDistanceM -= 1;
			if (NextStopNode.IsFacingPoint == false && NextStopNode.JunctionIndex >= 0)
			{
				float clearance = 10;
				if (NextStopNode == TrackAuthority.EndNode)
				{
					TrackNode tn = Path.TrackDB.TrackNodes[NextStopNode.JunctionIndex];
					clearance = 40;
					if (tn != null && tn.TrJunctionNode != null)
					{
						TrackShape shape = Path.TSectionDat.TrackShapes.Get(tn.TrJunctionNode.ShapeIndex);
						if (shape != null && shape.ClearanceDistance > 30)
							clearance = 1.5f * (float)shape.ClearanceDistance;
					}
				}
				NextStopDistanceM -= clearance;
			}
			if (NextStopDistanceM < 0)
				NextStopDistanceM = 0;
			AuthUpdateDistanceM = -1;
			if (AITrainDirectionForward && (NextStopNode == TrackAuthority.EndNode || NextStopNode == TrackAuthority.SidingNode))
			{
                //AuthUpdateDistanceM = .5f * MaxSpeedMpS * MaxSpeedMpS / MaxDecelMpSS;
                //if (AuthUpdateDistanceM > .5f * NextStopDistanceM)
                //    AuthUpdateDistanceM = .5f * NextStopDistanceM;
                // By GeorgeS
                AuthUpdateDistanceM = .5f * MaxSpeedMpS * MaxSpeedMpS / MaxDecelMpSS;
				if (AuthUpdateDistanceM > NextStopDistanceM + 1500)
					AuthUpdateDistanceM = NextStopDistanceM + 1500;
			}
			return true;
		}

		/// <summary>
		/// Finds the next path node the train should stop at.
		/// </summary>
		private AIPathNode FindStopNode(AIPathNode node, float throwDistance)
		{
			SwitchList.Clear();
			if (node.NextMainNode == null && node.NextSidingNode == null)
				return null;
			while (node != TrackAuthority.EndNode)
			{
				AIPathNode prevNode = node;
				node = GetNextNode(node);
				if (node == null)
					return node;
				switch (node.Type)
				{
					case AIPathNodeType.Stop:
					case AIPathNodeType.Reverse:
					case AIPathNodeType.Uncouple:
						return node;
					default:
						break;
				}
				if (!Path.SwitchIsAligned(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode)))
				{
					Traveller traveller = AITrainDirectionForward ? FrontTDBTraveller : RearTDBTraveller;
					float d = WorldLocation.GetDistanceSquared(traveller.WorldLocation, node.Location);
					//if (d > throwDistance * throwDistance || AI.Simulator.SwitchIsOccupied(node.JunctionIndex))
                    // By GeorgeS
                    if (AI.Simulator.SwitchIsOccupied(node.JunctionIndex))
						return node;
                    if (node != TrackAuthority.EndNode || (node.NextMainNode == null && node.NextSidingNode == null))
					    Path.AlignSwitch(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode));
				}
				if (node.JunctionIndex >= 0 && node != TrackAuthority.EndNode)
					SwitchList.Add(new AISwitchInfo(Path, node));
			}
			return node;
		}

		/// <summary>
		/// Finds the path node before target.
		/// </summary>
		private AIPathNode FindPrevNode(AIPathNode target)
		{
			AIPathNode node1 = TrackAuthority.StartNode;
			while (node1 != target)
			{
				AIPathNode node2 = GetNextNode(node1);
				if (node2 == null)
					return TrackAuthority.StartNode;
				if (node2 == target)
					return node1;
				node1 = node2;
			}
			return node1;
		}

		/// <summary>
		/// Performs any special processing based on the type of node.
		/// Returns true if an action was performed.
		/// </summary>
		private bool HandleNodeAction(AIPathNode node, double clockTime)
		{
			if (CoupleOnNextStop)
			{
				CoupleOnNextStop = false;
				if (AITrainDirectionForward)
					SpeedMpS = 20; // speed controls distance threshold which must be larger than our stop threshold
				else
					SpeedMpS = -20;
				AI.Simulator.CheckForCoupling(this, .1f);
				SpeedMpS = 0;
				CalculatePositionOfCars(0);
			}
			switch (node.Type)
			{
				case AIPathNodeType.Stop:
					WaitUntil = clockTime + node.WaitTimeS;
					if (WaitUntil < node.WaitUntil)
						WaitUntil = node.WaitUntil;
					return true;
				case AIPathNodeType.Reverse:
					AITrainDirectionForward = !AITrainDirectionForward;
					WaitUntil = clockTime + (node.WaitTimeS > 0 ? node.WaitTimeS : 5);
					if (WaitUntil < node.WaitUntil)
						WaitUntil = node.WaitUntil;
					if (node.NCars != 0)
						Uncouple(node.NCars);
					return true;
				case AIPathNodeType.Uncouple:
					Uncouple(node.NCars);
					WaitUntil = clockTime + node.WaitTimeS;
					if (WaitUntil < node.WaitUntil)
						WaitUntil = node.WaitUntil;
					return true;
				default:
					break;
			}
			return false;
		}

		/// <summary>
		/// Uncouples cars from the AI train and keeps the specified number of cars.
		/// If nCars is negative, -nCars are counted from the rear and the rear is kept.
		/// If nCars if zero, the entire train is uncoupled and the AI train will be deleted.
		/// Returns true if an action was performed.
		/// </summary>
		public void Uncouple(int nCars)
		{
			int n1 = nCars;
			int n2 = Cars.Count;
			if (nCars < 0)
			{
				n1 = 0;
				n2 = Cars.Count + nCars;
			}
			if (n1 > n2 || n2 > Cars.Count)
				return;
			// move rest of cars to the new train
			Train train2 = new Train(Simulator);
			train2.TrainType = Train.TRAINTYPE.AI;
			for (int k = n1; k < n2; ++k)
			{
				TrainCar newcar = Cars[k];
				train2.Cars.Add(newcar);
				newcar.Train = train2;
			}
			// and drop them from the old train
			for (int k = n2 - 1; k >= n1; --k)
			{
				Cars.RemoveAt(k);
			}
			// and fix up the travellers
			if (nCars < 0)
			{
				train2.FrontTDBTraveller = new Traveller(FrontTDBTraveller);
				train2.RepositionRearTraveller();
				CalculatePositionOfCars(0);
			}
			else
			{
				train2.RearTDBTraveller = new Traveller(RearTDBTraveller);
				train2.CalculatePositionOfCars(0);  // fix the front traveller
				RepositionRearTraveller();    // fix the rear traveller
			}

			// check if trains are freight or passenger - for both trains

			CheckFreight();
			train2.CheckFreight();

			train2.InitializeSignals(false);  // Initialize signals and speedposts without active speed information [R.Roeterdink]
			AI.Simulator.Trains.Add(train2);
			
			if (nCars != 0)
				Update(0);   // stop the wheels from moving etc
			train2.Update(0);  // stop the wheels from moving etc
			if (nCars > 0)
				Cars[nCars - 1].SignalEvent(EventID.Uncouple);
			else if (nCars < 0)
				Cars[0].SignalEvent(EventID.Uncouple);
		}

		public AIPathNode GetNextNode(AIPathNode node)
		{
			if ((TrackAuthority != null && node == TrackAuthority.SidingNode) || node.NextMainNode == null)
				return node.NextSidingNode;
			else
				return node.NextMainNode;
		}
		public int GetTVNIndex(AIPathNode node)
		{
			if ((TrackAuthority != null && node == TrackAuthority.SidingNode) || node.NextMainNode == null)
				return node.NextSidingTVNIndex;
			else
				return node.NextMainTVNIndex;
		}

		/// <summary>
		/// Calculated the desired acceleration given the distance to the next stop.
		/// </summary>
		private float CalcAccelMpSS()
		{
            float targetMpS = MaxSpeedMpS < base.AllowedMaxSpeedMpS ? MaxSpeedMpS : base.AllowedMaxSpeedMpS;
			if (!AITrainDirectionForward || CoupleOnNextStop)
				targetMpS *= .75f;
			float stopDistanceM = NextStopDistanceM;
			float minStopDistance = 2 * SpeedMpS * SpeedMpS / MaxDecelMpSS + 40;
			foreach (AISwitchInfo sw in SwitchList)
			{
				float d = NextStopDistanceM - sw.DistanceM;
				if (d > minStopDistance)
					break;
				if (d < 0)
				{
					SwitchList.Remove(sw);
					break;
				}
				if (sw.JunctionNode.SelectedRoute != sw.SelectedRoute)
				{
					stopDistanceM = d - 40;
					if (stopDistanceM < 0)
						stopDistanceM = 0;
					if (d < 40.5f && !AI.Simulator.SwitchIsOccupied(sw.JunctionNode))
						sw.JunctionNode.SelectedRoute = sw.SelectedRoute;
					break;
				}
			}
			if (AI.Dispatcher.PlayerOverlaps(this, true))
			{
				Traveller traveller = FrontTDBTraveller;
				if (!AITrainDirectionForward)
					traveller = RearTDBTraveller;
				float d = (float)Math.Sqrt(WorldLocation.GetDistanceSquared(traveller.WorldLocation, AI.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.WorldLocation));
				d -= SpeedMpS == 0 ? 500 : 50;
				if (d < 0)
					d = 0;
				if (stopDistanceM > d)
					stopDistanceM = d;
			}
			if (AI.Dispatcher.PlayerOverlaps(this, false))
			{
				Traveller traveller = AITrainDirectionForward ? FrontTDBTraveller : RearTDBTraveller;
				float d = (float)Math.Sqrt(WorldLocation.GetDistanceSquared(traveller.WorldLocation, AI.Simulator.PlayerLocomotive.Train.RearTDBTraveller.WorldLocation));
				d -= SpeedMpS == 0 ? 500 : 50;
				if (d < 0)
					d = 0;
				if (stopDistanceM > d)
					stopDistanceM = d;
			}
			if (AITrainDirectionForward)
			{
				switch (GetNextSignalAspect())
				{
					case SignalHead.SIGASP.STOP:
                        targetMpS = 30 > targetMpS ? targetMpS : 30;
                        targetMpS = distanceToSignal < 1200 && 20 > targetMpS ? targetMpS : 20;
                        targetMpS = distanceToSignal < 500 && 10 > targetMpS ? targetMpS : 10;
						//targetMpS *= .5f;
						if (stopDistanceM > distanceToSignal - 1)
							stopDistanceM = distanceToSignal - 5;
						break;
					case SignalHead.SIGASP.STOP_AND_PROCEED:
                        targetMpS = 30 > targetMpS ? targetMpS : 30;
                        targetMpS = distanceToSignal < 1200 && 20 > targetMpS ? targetMpS : 20;
                        targetMpS = distanceToSignal < 500 && 10 > targetMpS ? targetMpS : 10;
                        //targetMpS *= .5f;
						if (distanceToSignal > 3 && stopDistanceM > distanceToSignal - 1)
							stopDistanceM = distanceToSignal - 1;
						break;
				}
			}
			if (stopDistanceM < 0)
				stopDistanceM = 0;
			// adjust stopDistanceM to account for signals etc.
			float maxSpeedSq = targetMpS * targetMpS;
			if (maxSpeedSq > 2 * stopDistanceM * MaxDecelMpSS)
				maxSpeedSq = 2 * stopDistanceM * MaxDecelMpSS;
			float minSpeedSq = .96f * maxSpeedSq;
			if (NextStopTimeS > 0)
			{
				float ssq = (stopDistanceM + maxSpeedSq / (2 * MaxDecelMpSS)) / NextStopTimeS;
				ssq = ssq * ssq;
				if (minSpeedSq > ssq)
				{
					minSpeedSq = ssq;
					if (maxSpeedSq > 1.4f * ssq)
						maxSpeedSq = 1.4f * ssq;
				}
			}
			if (minSpeedSq > (stopDistanceM - .2f) * MaxDecelMpSS)
				minSpeedSq = (stopDistanceM - .2f) * MaxDecelMpSS;
			if (minSpeedSq < 0)
				minSpeedSq = 0;
			float speedSq = SpeedMpS * SpeedMpS;
			if (speedSq > maxSpeedSq && stopDistanceM > 0 && 2 * stopDistanceM * MaxDecelMpSS < speedSq)
				return -.5f * speedSq / stopDistanceM;
			else if (speedSq > maxSpeedSq)
				return -MaxDecelMpSS;
			else if (speedSq < minSpeedSq)
				return MaxAccelMpSS;
			else
				return 0;
		}

		/// <summary>
		/// Adjusts the train's throttle and brake controls to try to achieve the
		/// desired acceleration.  If the desired acceleration cannot be achieved
		/// using the controls, it is simply added to the speed.
		/// Adjusting the controls is the easiest way to get sound effects etc. to work right.
		/// </summary>
		private void AdjustControls(float targetMpSS, float measMpSS, float timeS)
		{
			if (NextStopDistanceM < 0)
			{
				AITrainThrottlePercent = 0;
				AITrainBrakePercent = 100;
			}
			if (targetMpSS < 0 && measMpSS > targetMpSS)
			{
				if (AITrainThrottlePercent > 0)
				{
					AITrainThrottlePercent -= 10;
					if (AITrainThrottlePercent < 0)
						AITrainThrottlePercent = 0;
				}
				else if (AITrainBrakePercent < 100)
				{
					AITrainBrakePercent += 10;
					if (AITrainBrakePercent > 100)
						AITrainBrakePercent = 100;
				}
				else
				{
                   
					float ds = timeS * (targetMpSS - measMpSS);
					SpeedMpS = Math.Max (SpeedMpS+ds, 0); // avoid negative speeds
					foreach (TrainCar car in Cars)
						if (car.Flipped)
							car.SpeedMpS = -SpeedMpS;
						else
							car.SpeedMpS = SpeedMpS;
				}
			}
			if (targetMpSS > 0 && measMpSS < targetMpSS)
			{
				if (AITrainBrakePercent > 0)
				{
					AITrainBrakePercent -= 10;
					if (AITrainBrakePercent < 0)
						AITrainBrakePercent = 0;
				}
				else if (AITrainThrottlePercent < 100)
				{

                    if (AITrainThrottlePercent == 25 && targetMpSS > measMpSS * 500)
                    {
                        float ds = timeS * (targetMpSS - measMpSS);
                        SpeedMpS += ds;
                        foreach (TrainCar car in Cars)
                            if (car.Flipped)
                                car.SpeedMpS -= ds;
                            else
                                car.SpeedMpS += ds;
                    }
                    else
                    {
                        AITrainThrottlePercent += 2;
                        if (SpeedMpS > 0 && AITrainThrottlePercent > SpeedMpS * 10)
                            AITrainThrottlePercent = SpeedMpS * 10;

                        if (AITrainThrottlePercent < 25)
                            AITrainThrottlePercent = 25;

                        if (AITrainThrottlePercent > 100)
                            AITrainThrottlePercent = 100;
                    }

				}
				else
				{
                    
					float ds = timeS * (targetMpSS - measMpSS);
					SpeedMpS += ds;
					foreach (TrainCar car in Cars)
						if (car.Flipped)
							car.SpeedMpS -= ds;
						else
							car.SpeedMpS += ds;
				}
			}
            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }
		}

        public void Release()
        {
            nextSignal.Clear();
            foreach (TrainCar car in Cars)
                car.IsPartOfActiveTrain = false;
        }

		public float PassTime()
		{
			return Length / MaxSpeedMpS;
		}
		public float StopStartTime()
		{
			return .5f * MaxSpeedMpS * (1 / MaxDecelMpSS + 1 / MaxAccelMpSS);
		}
	}

	public class AISwitchInfo
	{
		public AIPathNode PathNode;
		public TrJunctionNode JunctionNode;
		public int SelectedRoute;
		public float DistanceM;
		public AISwitchInfo(AIPath path, AIPathNode node)
		{
			PathNode = node;
			JunctionNode = path.TrackDB.TrackNodes[node.JunctionIndex].TrJunctionNode;
			SelectedRoute = JunctionNode.SelectedRoute;
			DistanceM = 0;
		}
	}
}
