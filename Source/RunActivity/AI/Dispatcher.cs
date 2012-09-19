/* Dispatcher
 * 
 * Contains code for AI train dispatcher.
 * This dispatcher reserves track nodes along an AI train's path up to the end of a passing point.
 * If all nodes can be reserved, the AI train is granted permission to move.
 * At the moment passing sections must be defined in the path.
 * In the future some code should be added to compare paths to find possible passing points.
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
	public enum DispatcherPOIType
	{
		Unknown,
		OffPath, // also kind of "unknown"
		StationStop, // timetabled stop at station
		ReversePoint, // change of train direction point
		EndOfAuthorization, // end of the reserved path
		Stop, // non-station timetabled stop
	}

    public class Dispatcher
    {
        public AI AI;
        public static int[] Reservations;  //[Rob Roeterdink] made public for acces from signal processing
        public float[] TrackLength;
        private TimeTable TimeTable = null;
        public int PlayerPriority = 0;
        public List<TrackAuthority> TrackAuthorities = new List<TrackAuthority>();
        private float UpdateTimerS = 10;

        /// <summary>
        /// Initializes the dispatcher.
        /// Creates an array for saving track node reservations and initializes it to no reservations.
        /// </summary>
        public Dispatcher(AI ai)
        {
#if DUMP_DISPATCHER
            File.Delete(".\\dispatcher.txt");
#endif
            AI = ai;
            Reservations = new int[ai.Simulator.TDB.TrackDB.TrackNodes.Length];
            for (int i = 0; i < Reservations.Length; i++)
                Reservations[i] = -1;
            CalcTrackLength();
            if (ai.Simulator.Activity == null)
                return;
            // make a temporary AITrain for the player
            string playerServiceFileName = AI.Simulator.Activity.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name;
            SRVFile srvFile = new SRVFile(AI.Simulator.RoutePath + @"\SERVICES\" + playerServiceFileName + ".SRV");
            CONFile conFile = new CONFile(AI.Simulator.BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON");
            string patFilename = AI.Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";
            PATFile patFile = new PATFile(patFilename);
            AIPath playerPath = new AIPath(patFile, AI.Simulator.TDB, AI.Simulator.TSectionDat,patFilename);
            AITrain playerTrain = new AITrain(ai.Simulator, 0, AI, playerPath, (int)AI.Simulator.ClockTime);
            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                playerTrain.MaxSpeedMpS = conFile.Train.TrainCfg.MaxVelocity.A * srvFile.Efficiency;
            AI.AITrainDictionary.Add(0, playerTrain);
            FindDoubleTrack();
            PlayerPriority = AI.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.Second % 10;
            if (AI.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing.Contains("OR Dispatcher: Priority"))
                TimeTable = new TimeTable(this);
            AI.AITrainDictionary.Remove(0);
            TrackAuthority auth = new TrackAuthority(playerTrain, 0, PlayerPriority, playerPath);
            TrackAuthorities.Add(auth);
            // By GeorgeS
            //RequestAuth(auth, true, true);
            Player_Service_Definition psd = AI.Simulator.Activity.Tr_Activity.Tr_Activity_File.Player_Service_Definition;
            auth.StationDistanceM = new List<float>();
            foreach (var i in psd.Player_Traffic_Definition.Player_Traffic_List) {
                auth.StationDistanceM.Add(i.DistanceDownPath);
            }
#if false
            if (AI.Simulator.TDB.TrackDB.TrItemTable != null)
            foreach (TrItem item in AI.Simulator.TDB.TrackDB.TrItemTable)
            {
                if (item.ItemType == TrItem.trItemType.trPLATFORM)
                {
                    PlatformItem platform = (PlatformItem)item;
                    Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", item.TrItemId, platform.Station, platform.TileX, platform.TileZ,
                        platform.X, platform.Y,platform.Z);
                }
            }
#endif
        }

        // restore game state
        public Dispatcher(AI ai, BinaryReader inf)
        {
#if DUMP_DISPATCHER
            File.Delete(".\\dispatcher.txt");
#endif
            AI = ai;
            PlayerPriority = inf.ReadInt32();
            int n = inf.ReadInt32();
            Reservations = new int[n];
            for (int i = 0; i < n; i++)
                Reservations[i] = inf.ReadInt32();
            n = inf.ReadInt32();
            TrackLength = new float[n];
            for (int i = 0; i < n; i++)
                TrackLength[i] = inf.ReadSingle();
            n = inf.ReadInt32();
            if (n > 0)
                TimeTable = new TimeTable(this, n, inf);
            n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
                TrackAuthorities.Add(new TrackAuthority(inf,AI));
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(PlayerPriority);
            outf.Write(Reservations.Length);
            for (int i = 0; i < Reservations.Length; i++)
                outf.Write(Reservations[i]);
            outf.Write(TrackLength.Length);
            for (int i = 0; i < TrackLength.Length; i++)
                outf.Write(TrackLength[i]);
            if (TimeTable == null)
                outf.Write((int)0);
            else
                TimeTable.Save(outf);
            outf.Write(TrackAuthorities.Count);
            for (int i = 0; i < TrackAuthorities.Count; i++)
                TrackAuthorities[i].Save(outf);
        }

#if DUMP_DISPATCHER
        public void Dump()
        {
            foreach (TrackAuthority ta in TrackAuthorities)
            {
                if (Program.Simulator.Trains.Contains(ta.Train))
                {
                    ta.Dump(sta =>
                    {
                        if (ta.Train != null) ta.Train.DumpSignals(sta);
                    });
                }
            }
        }
#endif

        /// <summary>
        /// Updates dispatcher information.
        /// Moves each train's rear path node forward and updates reservations.
        /// </summary>
        public void Update(double clockTime, float elapsedClockSeconds)
        {
            UpdateTimerS -= elapsedClockSeconds;
            foreach (TrackAuthority auth in TrackAuthorities)
            {
                if (auth.EndNode == null || auth.StartNode == null || !Program.Simulator.Trains.Contains(auth.Train))
                    continue;

            // By GeorgeS
                if (UpdateTimerS <= 0)
                {
                    //RequestAuth(auth, true, auth.NReverseNodes % 2 == 0);
                    ExtendAuthorization(auth, clockTime);
                }

                // Train length is for couple / uncouple
                auth.UpdateTrainLength();

                auth.Train.TrackAuthority = auth;

                if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
                {
                    auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                    float distMoved = 0;
                    if (auth.Train.Reverse)
                        distMoved -= elapsedClockSeconds * auth.Train.SpeedMpS;
                    else
                        distMoved += elapsedClockSeconds * auth.Train.SpeedMpS;

                    if (auth.StopNode.Type != AIPathNodeType.Reverse || auth.StopDistanceM > 0)
                    {
                        auth.DistanceDownPathM += distMoved;
                        if (auth.PathDistReverseAdjustmentM != 0)
                        {
                            auth.DistanceDownPathM -= auth.PathDistReverseAdjustmentM;
                            auth.PathDistReverseAdjustmentM = 0;
                        }
                    }
                    else
                    {
                        if (auth.PathDistReverseAdjustmentM == 0)
                            auth.PathDistReverseAdjustmentM -= auth.Train.Length;
                        auth.PathDistReverseAdjustmentM += distMoved;
                    }

                    if (auth.StopNode == auth.StartNode)
                    {
                        auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    else if (auth.EndNode == auth.StopNode && UpdateTimerS < 0 && auth.StopDistanceM < 4*auth.Train.SpeedMpS*auth.Train.SpeedMpS + 500)
                    {
                        //RequestAuth(auth, true, auth.NReverseNodes % 2 == 0);
                        RequestAuth(auth, true, !auth.Train.Reverse);
                        if (auth.EndNode != auth.StopNode)
                            auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    else if (auth.Train.Reverse)
                    {
                        auth.StopDistanceM += elapsedClockSeconds * auth.Train.SpeedMpS;
                    }
                    else
                    {
                        auth.StopDistanceM -= elapsedClockSeconds * auth.Train.SpeedMpS;
                    }
                    if (auth.StopDistanceM < 0 && auth.StationStop)
                    {
                        auth.CalcStopDistance();
                    }
                    else if (auth.StopNode.Type == AIPathNodeType.Reverse && auth.NReverseNodes>0 && (auth.StopDistanceM < 0 || (auth.Train.SpeedMpS==0 && auth.StopDistanceM-auth.PathDistReverseAdjustmentM<0)))
                    {
                        if (auth.StartNode != auth.StopNode)
                        {
                            //auth.DistanceDownPathM += 2 * auth.PathDistReverseAdjustmentM;
                            auth.NReverseNodes--;
                        }

                        auth.Path.SetVisitedNode(auth.StopNode, auth.Train.RearTDBTraveller.TrackNodeIndex);
                        auth.StartNode = auth.Path.LastVisitedNode;
                        auth.InBetweenStartNode = auth.StartNode;
                        //auth.StartNode = auth.StopNode;
                        // By GeorgeS
                        //Rereserve(auth);
                        //if (RequestAuth(auth, true, auth.NReverseNodes % 2 == 0))
                        if (RequestAuth(auth, true, !auth.Train.Reverse))
                        {
                            auth.AdvanceStopNode(true);
                            auth.CalcStopDistance();
                        }
                        if (auth.NReverseNodes == 0)
                            UpdateTimerS = 0;
                    }
                    else if (auth.StopDistanceM < 0 && auth.StopNode != auth.EndNode)
                    {
                        auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    if (auth.NReverseNodes % 2 == 1 && auth.StartNode != null && 
                        auth.StartNode.Type == AIPathNodeType.Reverse)
                        continue;
                }
                else
                {
                    auth.DistanceDownPathM += elapsedClockSeconds * auth.Train.SpeedMpS;
                    if (!auth.Train.AITrainDirectionForward)
                    {
                        if (auth.NReverseNodes > 0 && auth.NReverseNodes % 2 == 0)
                        {
                            auth.NReverseNodes--;
                            auth.StartNode = FindNextReverseNode(auth);
                        }
                        continue;
                    }
                    if (auth.NReverseNodes > 0 && auth.NReverseNodes % 2 == 1)
                    {
                        auth.NReverseNodes--;
                        auth.StartNode = FindNextReverseNode(auth);
                        if (auth.NReverseNodes == 0)
                            Rereserve(auth);
                    }
                    if (auth.NReverseNodes > 0 && auth.StartNode != null && 
                        auth.StartNode.Type == AIPathNodeType.Reverse)
                        continue;
                }
                if (auth.StartNode == null)
                    continue;

                int jnidx = -1;
                TrJunctionNode n = auth.Train.dRearTDBTraveller.JunctionNodeAhead();
                if (n != null)
                    jnidx = n.Idx;

                if (auth.PrevJunctionIndex != jnidx)
                {
                    if (auth.PrevJunctionIndex != -1)
                        Reservations[auth.PrevJunctionIndex] = -1;
                    auth.PrevJunctionIndex = jnidx;
                }

                if (auth.StartNode.NextMainTVNIndex == auth.Train.dRearTDBTraveller.TrackNodeIndex ||
                  auth.StartNode.NextSidingTVNIndex == auth.Train.dRearTDBTraveller.TrackNodeIndex ||
                  !auth.Train.dRearTDBTraveller.IsTrack)
                    continue;
                if (auth.TrainID == 0 && Reservations[auth.Train.dRearTDBTraveller.TrackNodeIndex] != auth.TrainID)
                    continue;
                AIPathNode nextNode = auth.Path.FindTrackNode(auth.StartNode, auth.Train.dRearTDBTraveller.TrackNodeIndex);
                if (nextNode != null && nextNode.IsFacingPoint == true && nextNode.JunctionIndex >= 0)
                {
                    float clearance = 40;
                    TrackNode tn = auth.Path.TrackDB.TrackNodes[nextNode.JunctionIndex];
                    if (tn != null && tn.TrJunctionNode != null)
                    {
                        TrackShape shape = auth.Path.TSectionDat.TrackShapes.Get(tn.TrJunctionNode.ShapeIndex);
                        if (shape != null)
                            clearance = 1.5f * (float)shape.ClearanceDistance;
                    }
                    float d = WorldLocation.GetDistanceSquared(nextNode.Location, auth.Train.dRearTDBTraveller.WorldLocation);
#if CLEARANCE
                    if (d < clearance * clearance)
                        continue;
#endif
                }
                int i = auth.StartNode.NextMainTVNIndex;
                // See bellow
                /*
                if (i >= 0 && Reservations[i] == auth.TrainID)
                    Reservations[i] = -1;
                else
                {
                    i = auth.StartNode.NextSidingTVNIndex;
                    if (i >= 0 && Reservations[i] == auth.TrainID)
                        Reservations[i] = -1;
                }
                bool hasUnReserve = true;
                */
                // By GeorgeS
                AIPathNode stepnode = auth.Path.FindTrackNode(auth.InBetweenStartNode, auth.Train.dRearTDBTraveller.TrackNodeIndex);
                bool hasUnReserve = false;
                while (stepnode != null)
                {
                    stepnode = auth.Path.PrevNode(stepnode);
                    if (stepnode != null)
                    {
                        i = stepnode.JunctionIndex;
                        if (i >= 0)
                        {
                            if (Reservations[i] == auth.TrainID)
                            {
                                Reservations[i] = -1;
                                hasUnReserve = true;
                            }
                        }
                        
                        i = stepnode.NextMainTVNIndex;
                        if (i >= 0)
                        {
                            if (Reservations[i] == auth.TrainID)
                            {
                                Reservations[i] = -1;
                                hasUnReserve = true;
                            }
                        }
                        else
                        {
                            i = stepnode.NextSidingTVNIndex;
                            if (i >= 0)
                            {
                                if (Reservations[i] == auth.TrainID)
                                {
                                    Reservations[i] = -1;
                                    hasUnReserve = true;
                                }
                            }
                            else
                            {
                                stepnode = null;
                            }
                        }
                        // Must check and break this way, because must free first, and step back if it is not a reverse point
                        if (stepnode.Type == AIPathNodeType.Reverse)
                            break;
                    }
                }
                //int n = 0;
                //for (int j = 0; j < Reservations.Length; j++)
                //    if (Reservations[j] == auth.TrainID)
                //        n++;
                //Console.WriteLine(" nres {0}", n);
                if (auth.StartNode.IsLastSwitchUse)
                    auth.Path.RestoreSwitch(auth.StartNode.JunctionIndex);

                auth.Path.SetVisitedNode(auth.StartNode, auth.Train.dRearTDBTraveller.TrackNodeIndex);
                auth.StartNode = auth.Path.LastVisitedNode;
                //auth.StartNode = nextNode;

                if (hasUnReserve)
                {
                    ExtendPlayerAuthorization(false);
                }
            }

            foreach (TrackAuthority auth in TrackAuthorities)
            {
                if (Program.Simulator.Trains.Contains(auth.Train) && auth.Train.dFrontTDBTraveller != null &&
                    Reservations[auth.Train.dFrontTDBTraveller.TrackNodeIndex] == -1 && auth.Train.MUDirection != Direction.N)
                {
                    ExtendAuthorization(auth, clockTime);
                }
            }

            if (UpdateTimerS < 0)
                UpdateTimerS = 10;
        }

        private void ExtendAuthorization(TrackAuthority auth, double clockTime)
        {
            bool success = RequestAuth(auth, true, !auth.Train.Reverse);
            if (success)
            {
                auth.AdvanceStopNode(true);
                auth.CalcStopDistance();
                AITrain ait = auth.Train as AITrain;
                if (ait != null)
                {
                    ait.TryAdvanceStopNode(clockTime);
                }
                auth.Train.InitializeSignals(true);
            }
        }

        public void ExtendPlayerAuthorization(bool force)
        {
            if (TrackAuthorities.Count == 0)
                return;            

            TrackAuthority auth = TrackAuthorities[0];
            if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
            {
                auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                //if (!RequestAuth(auth, true, auth.NReverseNodes % 2 == 0))
                if (!RequestAuth(auth, true, !auth.Train.Reverse, force))
                    return;
                // By GeorgeS
                //RequestAuth(auth, true, auth.NReverseNodes % 2 == 0)
                if (auth.EndNode != auth.StopNode)
                    auth.AdvanceStopNode(true);
                auth.CalcStopDistance();
            }
        }

		public void ExtendTrainAuthorization(Train t, bool force)
		{
			if (TrackAuthorities.Count == 0)
				return;

			TrackAuthority auth = null;
			foreach(var a in TrackAuthorities) {
				if (a.TrainID == t.Number + 100000) { auth = a; break; }
			}
			if (auth == null) return;

			auth.Train = t;
			RequestAuth(auth, true, !auth.Train.Reverse);
			t.InitializeSignals(true);

		}
		
		public void ReleasePlayerAuthorization()
        {
            if (TrackAuthorities.Count == 0)
                return;

            TrackAuthority auth = TrackAuthorities[0];
            if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
            {
                auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                SetAuthorization(auth, null, null, 0);
                Unreserve(0);
            }
        }
        public void ReversePlayerAuthorization()
        {
            if (TrackAuthorities.Count == 0)
                return;
            TrackAuthority auth = TrackAuthorities[0];
            if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
            {
                auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                auth.NReverseNodes++;
            }
        }

        public void CountSignals(int node, Action<SignalObject> action)
        {
            TrackNode[] trackNodes = Program.Simulator.TDB.TrackDB.TrackNodes;
            TrItem[] trItems = Program.Simulator.TDB.TrackDB.TrItemTable;
            List<int> siglist = new List<int>();
            //int dir = (int)t.Direction;

            if (node == -1) return;
            if (Signal.signalObjects == null) return;
            if (trackNodes[node].TrEndNode) return;  // End of track reached no signals found.
            if (trackNodes[node].TrVectorNode != null)
            {
                if (trackNodes[node].TrVectorNode.noItemRefs > 0)
                {
                    for (int i = 0; i < trackNodes[node].TrVectorNode.noItemRefs; i++)
                    {
                        if (trItems[trackNodes[node].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
                        {
                            SignalItem sigItem = (SignalItem)trItems[trackNodes[node].TrVectorNode.TrItemRefs[i]];
                            //if ( sigItem.revDir == Direction)
                            {
                                int sigObj = sigItem.sigObj;
                                if (Signal.signalObjects[sigObj] != null && 
                                    !siglist.Contains(Signal.signalObjects[sigObj].thisRef))
                                {
                                    siglist.Add(Signal.signalObjects[sigObj].thisRef);
                                    action(Signal.signalObjects[sigObj]);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Requests movement authorization for the specified train.
        /// Follows the train's path from the current rear node until the path ends
        /// or a SidingEnd node is found.  Grants authorization if all of the track
        /// vector nodes can be reserved for the train.
        /// If a SidingStart node is found, the main track and siding are tested separately.
        /// Returns true if an authorization was granted, else false.
        /// The authorization is specified using the SetAuthorization method.
        /// </summary>
        public bool RequestAuth(AITrain train, bool update)
        {
            if (train.TrackAuthority == null)
            {
                train.TrackAuthority = new TrackAuthority(train, train.UiD, train.Priority, train.Path);
                TrackAuthorities.Add(train.TrackAuthority);
            }
            bool success = RequestAuth(train.TrackAuthority, update, train.AITrainDirectionForward);

            if (success)
            {
                train.TryAdvanceStopNode(Program.Simulator.ClockTime);
            }

            return success;
        }

		public bool RequestAuth(Train train, bool update, int x)
		{
			if (train.TrackAuthority == null)
			{
				//train.TrackAuthority = new TrackAuthority(train, train.Number, 10, train.Path);
				//TrackAuthorities.Add(train.TrackAuthority);
				return false;
			}
			bool success = RequestAuth(train.TrackAuthority, update, train.AITrainDirectionForward);

			return success;
		}
        private bool RequestAuth(TrackAuthority auth, bool update, bool movingForward)
        {
            return RequestAuth(auth, update, movingForward, false);
        }
        private bool RequestAuth(TrackAuthority auth, bool update, bool movingForward, bool force)
        {
            TTTrainTimes ttTimes = null;
            if (TimeTable != null)
            {
                if (!TimeTable.ContainsKey(auth.TrainID))
                    return false;
                ttTimes = TimeTable[auth.TrainID];
                if (auth.Train.GetType() == typeof(AITrain))
                {
                    AITrain aiTrain = (AITrain)auth.Train;
                    if (aiTrain.NextStopNode == auth.EndNode)
                    {
                        int ji = aiTrain.NextStopNode.JunctionIndex;
                        if (!ttTimes.ContainsKey(ji) || ttTimes[ji].Arrive > AI.Simulator.ClockTime)
                            return false;
                    }
                }
            }
            List<int> tnList = new List<int>();
            AIPathNode node = auth.Path.FindTrackNode(auth.StartNode, auth.Train.dFrontTDBTraveller.TrackNodeIndex);
            if (node == null)
            {
                node = auth.Path.FindTrackNode(auth.LastValidNode, auth.Train.dFrontTDBTraveller.TrackNodeIndex);
            }
            else
            {
                auth.LastValidNode = node;
            }
            auth.StartNode = node;
            //AIPathNode node = auth.StartNode;
            bool fstNode = true;
            int nRev = 0;
            if (!movingForward)
                nRev++;

            Traveller traveller = auth.Train.FrontTDBTraveller;
            if (!auth.Train.AITrainDirectionForward)
            {
                traveller = new Traveller(auth.Train.RearTDBTraveller);
                traveller.ReverseDirection();
            }

            int jctNodeToAdd = -1;
            Traveller t = new Traveller(traveller);
            if (t.IsJunction || (t.NextTrackNode() && t.IsJunction))
            {
                TrJunctionNode n = t.TN.TrJunctionNode;
                jctNodeToAdd = n.Idx;
                if (force)
                    Reservations[n.Idx] = -1;
            }

            int nodeidx = 0;
            int sigcou = 0;

            bool firstIsNeg = false;
            while (node != null)
            {
                // By GeorgeS
                //if (movingForward && node != auth.StartNode && node.Type == AIPathNodeType.SidingStart)
                //    break;
                if (movingForward && node.Type == AIPathNodeType.SidingEnd && node != auth.EndNode && 
                    Reservations[node.NextMainTVNIndex] != -1 && Reservations[node.NextMainTVNIndex] != auth.TrainID)
                    break;
                if (node != auth.StartNode && node.Type == AIPathNodeType.Reverse)
                {
                    movingForward = !movingForward;
                    nRev++;
                    break;
                    //Console.WriteLine("rev node {0}", node.ID);
                }

                if (node.NextMainNode != null && node != auth.SidingNode)
                {
                    if (force)
                        Reservations[node.NextMainTVNIndex] = -1;

                    if (Reservations[node.NextMainTVNIndex] >= 0 && Reservations[node.NextMainTVNIndex] != auth.TrainID)
                        break;
                    nodeidx = node.NextMainTVNIndex;
                }
                else if (node.NextSidingNode != null)
                {
                    if (force)
                        Reservations[node.NextSidingTVNIndex] = -1;

                    if (Reservations[node.NextSidingTVNIndex] >= 0 && Reservations[node.NextSidingTVNIndex] != auth.TrainID)
                        break;
                    nodeidx = node.NextSidingTVNIndex;
                }

                if (node.JunctionIndex != -1)
                {
                    TrackNode tn = Program.Simulator.TDB.TrackDB.TrackNodes[node.JunctionIndex];
                    float dtj = traveller.DistanceTo(tn.UiD.TileX, tn.UiD.TileZ, tn.UiD.X, tn.UiD.Y, tn.UiD.Z);
                    if (dtj > 0)
                    {
                        if (force && Reservations[node.JunctionIndex] != auth.TrainID)
                            Reservations[node.JunctionIndex] = -1;

                        if (tnList.Contains(node.JunctionIndex))
                            tnList.Remove(node.JunctionIndex);

                        if (Reservations[node.JunctionIndex] == -1)
                            tnList.Add(node.JunctionIndex);
                    }
                }

                auth.Path.AlignSwitchesTo(node);

                float dist = traveller.DistanceTo(node.Location.TileX, node.Location.TileZ, node.Location.Location.X, node.Location.Location.Y, node.Location.Location.Z);
                if (node.NextMainTVNIndex == traveller.TrackNodeIndex || node.NextSidingTVNIndex == traveller.TrackNodeIndex)
                {
                    firstIsNeg = dist == -1;
                    dist = 0;
                }

                if (node != auth.StartNode)
                {
                    //sigcou += CountSignals(nodeidx, traveller);

                    CountSignals(nodeidx, s =>
                    {
                        Traveller tt = traveller;
                        if (s.isSignalNormal() && s.DistanceToRef(ref tt) > 0 && s.revDir == (int)tt.Direction)
                        {
                            sigcou++;
                        }
                    });
                }

                //if (dist > 4000 && !firstIsNeg)
                //    break;

                // By GeorgeS
                // Wrong direction
                if (fstNode && dist == -1)
                    return false;

                if (dist != 0)
                {
                    fstNode = false;
                    firstIsNeg = false;
                }

                if (node.NextMainNode != null && node != auth.SidingNode)
                {
                    tnList.Add(node.NextMainTVNIndex);

                    if (jctNodeToAdd != -1)
                    {
                        tnList.Add(jctNodeToAdd);
                        jctNodeToAdd = -1;
                    }

                    if (sigcou >= 4 && dist > 3000)
                        break;

                    node = node.NextMainNode;
                }
                else if (node.NextSidingNode != null)
                {
                    tnList.Add(node.NextSidingTVNIndex);

                    if (jctNodeToAdd != -1)
                    {
                        tnList.Add(jctNodeToAdd);
                        jctNodeToAdd = -1;
                    }

                    if (sigcou >= 4 && dist > 3000)
                        break;

                    node = node.NextSidingNode;
                }
                else
                    break;
            }
            if (node == null || tnList.Count == 0 || !CanReserve(auth.TrainID, auth.Priority, tnList))
                return false;
            if (node.Type != AIPathNodeType.SidingStart)
            {
                //Unreserve(auth.TrainID);
                Reserve(auth.TrainID, tnList);
                return SetAuthorization(auth, node, null, nRev);
            }
            List<int> tnList1 = new List<int>();
            AIPathNode sidingNode = node;
            int nReverse = nRev;
            bool forward = movingForward;
            bool sidingFirst = !update;
            if (sidingFirst)
            {
                WorldLocation wl = sidingNode.Location;
                if (auth.Train.dFrontTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 10)
                    sidingFirst = false;
            }
            for (int i = 0; i < 2; i++)
            {
                tnList1.Clear();
                nRev = nReverse;
                movingForward = forward;
                if (sidingFirst ? i == 1 : i == 0)
                {
                    if (ttTimes != null && !ttTimes.ContainsKey(sidingNode.NextMainTVNIndex))
                        continue;
                    for (node = sidingNode; !movingForward || node.Type != AIPathNodeType.SidingEnd; node = node.NextMainNode)
                    {
                        tnList1.Add(node.NextMainTVNIndex);
                        if (node.Type == AIPathNodeType.Reverse)
                        {
                            movingForward = !movingForward;
                            nRev++;
                        }
                    }
                    if (CanReserve(auth.TrainID, auth.Priority, tnList1))
                    {
                        //Unreserve(auth.TrainID);
                        Reserve(auth.TrainID, tnList);
                        Reserve(auth.TrainID, tnList1);
                        return SetAuthorization(auth, node, null, nRev);
                    }
                }
                else
                {
                    if (ttTimes != null && !ttTimes.ContainsKey(sidingNode.NextSidingTVNIndex))
                        continue;
                    tnList1.Clear();
                    for (node = sidingNode; node.Type != AIPathNodeType.SidingEnd; node = node.NextSidingNode)
                    {
                        tnList1.Add(node.NextSidingTVNIndex);
                        if (node.Type == AIPathNodeType.Reverse)
                        {
                            movingForward = !movingForward;
                            nRev++;
                        }
                    }
                    if (CanReserve(auth.TrainID, auth.Priority, tnList1))
                    {
                        //Unreserve(auth.TrainID);
                        Reserve(auth.TrainID, tnList);
                        Reserve(auth.TrainID, tnList1);
                        return SetAuthorization(auth, node, sidingNode, nRev);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// end is the path node the train is allowed to move just short of.
        /// if siding is not null the train should enter the siding at the specified node.
        /// The dispatcher might change the authorization while a train is moving.
        /// </summary>
        public bool SetAuthorization(TrackAuthority auth, AIPathNode end, AIPathNode siding, int nRev)
        {
            bool result = auth.EndNode != end || auth.SidingNode != siding;
            auth.EndNode = end;
            auth.SidingNode = siding;
            auth.NReverseNodes = nRev % 2 == 1 ? nRev + 1 : nRev;

            if (!(auth.Train is AITrain))
            {
                int res;
                AIPathNode node;
                for (node = end; node != null; )
                {
                    if (node.Type == AIPathNodeType.Reverse)
                        break;

                    if (node != auth.SidingNode && node.NextMainNode != null)
                    {
                        if (Reservations[node.NextMainTVNIndex] < 0)
                            break;
                        node = node.NextMainNode;
                    }
                    else if (node.NextSidingNode != null)
                    {
                        if (Reservations[node.NextSidingTVNIndex] < 0)
                            break;
                        node = node.NextSidingNode;
                    }
                    else
                        break;
                }

                end = node;
                auth.EndNode = node;
            }

            int n = 0;
            if (end != null)
            {
                auth.AdvanceStopNode(true);
                auth.CalcStopDistance();
            }
            //for (int j = 0; j < Reservations.Length; j++)
            //    if (Reservations[j] == auth.TrainID)
            //        n++;
            return result;
        }

        public int GetReservation(int tvnIndex)
        {
            if (tvnIndex < 0 || tvnIndex >= Reservations.Length)
                return -1;
            return Reservations[tvnIndex];
        }

        /// <summary>
        /// Checks to see is the listed track nodes can be reserved for the specified train.
        /// return true if none of the nodes are already reserved for another train.
        /// </summary>
        private bool CanReserve(int trainID, int priority, List<int> tnList)
        {
            foreach (int i in tnList)
                if (Reservations[i] >= 0 && Reservations[i] != trainID)
                    return false;
            // By GeorgeS - dont check occupied track because player trainis first always
            return true;
            if (trainID != 0 && PlayerPriority <= priority && AI.Simulator.PlayerLocomotive != null)
            {
                Train playerTrain = AI.Simulator.PlayerLocomotive.Train;
                foreach (int j in tnList)
                    if (Reservations[j] != trainID && (j == playerTrain.dFrontTDBTraveller.TrackNodeIndex || j == playerTrain.RearTDBTraveller.TrackNodeIndex))
                    {
                        return false;
                    }
            }
            return true;
        }

        /// <summary>
        /// Reserves the listed track nodes for the specified train.
        /// </summary>
        private void Reserve(int trainID, List<int> tnList)
        {
            foreach (int i in tnList)
                Reservations[i] = trainID;
        }

        /// <summary>
        /// Clears any existing Reservations for the specified train.
        /// </summary>
        public void Unreserve(int trainID)
        {
            for (int i = 0; i < Reservations.Length; i++)
                if (Reservations[i] == trainID)
                    Reservations[i] = -1;
        }

        /// <summary>
        /// Releases the specified train's movement authorization.
        /// </summary>
        public void Release(AITrain train)
        {
            if (train.TrackAuthority == null)
                return;
            SetAuthorization(train.TrackAuthority, null, null, 0);
            Unreserve(train.UiD);
            TrackAuthorities.Remove(train.TrackAuthority);
            train.TrackAuthority = null;
        }

        /// <summary>
        /// Releases the specified train's movement authorization.
        /// </summary>
        private void Rereserve(TrackAuthority auth)
        {
            //Unreserve(auth.TrainID);
            for (AIPathNode node = auth.StartNode; node != null && node != auth.EndNode; )
            {
                if (node != auth.SidingNode && node.NextMainNode != null)
                {
                    Reservations[node.NextMainTVNIndex] = auth.TrainID;
                    node = node.NextMainNode;
                }
                else if (node.NextSidingNode != null)
                {
                    Reservations[node.NextSidingTVNIndex] = auth.TrainID;
                    node = node.NextSidingNode;
                }
                else
                    break;
            }
        }
        AIPathNode FindNextReverseNode(TrackAuthority auth)
        {
            for (AIPathNode node = auth.StartNode; node != null && node != auth.EndNode; )
            {
                if (node != auth.StartNode && node.Type == AIPathNodeType.Reverse)
                    return node;
                if (node.IsLastSwitchUse)
                    auth.Path.RestoreSwitch(node.JunctionIndex);
                if (node != auth.SidingNode && node.NextMainNode != null)
                    node = node.NextMainNode;
                else if (node.NextSidingNode != null)
                    node = node.NextSidingNode;
                else
                    break;
            }
            return auth.StartNode;
        }

        /// <summary>
        /// Scans all AI paths to identify double track passing possibilities.
        /// Changes the path node type to SidingEnd if its the end of double track.
        /// </summary>
        private void FindDoubleTrack()
        {
            int[] flags  = new int[AI.Simulator.TDB.TrackDB.TrackNodes.Length];
            foreach (KeyValuePair<int, AITrain> kvp in AI.AITrainDictionary)
            {
                AITrain train = kvp.Value;
                int prevIndex = -1;
                bool forward = true;
                for (AIPathNode node = train.Path.FirstNode; node != null; node = node.NextMainNode)
                {
                    if (node.Type == AIPathNodeType.Reverse)
                        forward = !forward;
                    if (forward && node.JunctionIndex >= 0)
                    {
                        int f = 0;
                        bool aligned = train.Path.SwitchIsAligned(node.JunctionIndex, node.NextMainTVNIndex);
                        if (node.Type == AIPathNodeType.SidingStart)
                            f = 0x3;
                        else if (node.Type == AIPathNodeType.SidingEnd)
                            f = 0xc;
                        else if (node.IsFacingPoint && train.Path.SwitchIsAligned(node.JunctionIndex, node.NextMainTVNIndex))
                            f = 0x1;
                        else if (node.IsFacingPoint)
                            f = 0x2;
                        else if (!node.IsFacingPoint && train.Path.SwitchIsAligned(node.JunctionIndex, prevIndex))
                            f = 0x4;
                        else
                            f = 0x8;
                        flags[node.JunctionIndex] |= f;
                    }
                    prevIndex = node.NextMainTVNIndex;
                }
            }
            foreach (KeyValuePair<int, AITrain> kvp in AI.AITrainDictionary)
            {
                AITrain train = kvp.Value;
                for (AIPathNode node = train.Path.FirstNode; node != null; node = node.NextMainNode)
                {
                    if (node.Type == AIPathNodeType.Other && node.JunctionIndex >= 0 && !node.IsFacingPoint)
                    {
                        int f = flags[node.JunctionIndex];
                        if ((f & 0x9) == 0x9 || (f & 0x6) == 0x6 || (f & 0xc) == 0xc)
                            node.Type = AIPathNodeType.SidingEnd;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the length of all track vector nodes and saves it in the TrackLength array.
        /// This should probably be moved elsewhere if others need this information.
        /// </summary>
        private void CalcTrackLength()
        {
            TrackLength = new float[AI.Simulator.TDB.TrackDB.TrackNodes.Length];
            for (int i = 0; i < TrackLength.Length; i++)
            {
                TrackNode tn = AI.Simulator.TDB.TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                for (int j = 0; j < tn.TrVectorNode.TrVectorSections.Length; j++)
                {
                    uint k = tn.TrVectorNode.TrVectorSections[j].SectionIndex;
                    TrackSection ts = AI.Simulator.TSectionDat.TrackSections.Get(k);
                    if (ts == null)
                        continue;
                    if (ts.SectionCurve == null)
                        TrackLength[i] += ts.SectionSize.Length;
                    else
                    {
                        float len = ts.SectionCurve.Radius * MSTSMath.M.Radians(ts.SectionCurve.Angle);
                        if (len < 0)
                            len = -len;
                        TrackLength[i] += len;
                    }
                }
            }
        }

        public bool PlayerOverlaps(AITrain train, bool front)
        {
            if (AI.Simulator.PlayerLocomotive == null)
                return false;
            Train ptrain = AI.Simulator.PlayerLocomotive.Train;
            int i = front ? ptrain.FrontTDBTraveller.TrackNodeIndex : ptrain.RearTDBTraveller.TrackNodeIndex;
            return Reservations[i] == train.UiD;
        }

		public DispatcherPOIType GetPlayerNextPOI(out float distance, out bool backwards)
		{
			distance = 0;
			backwards = false;

			if (AI.Simulator.PlayerLocomotive == null)
				return DispatcherPOIType.Unknown;

			Train ptrain = AI.Simulator.PlayerLocomotive.Train;
			bool reserved = Reservations[ptrain.FrontTDBTraveller.TrackNodeIndex] > 0 || Reservations[ptrain.RearTDBTraveller.TrackNodeIndex] > 0;
			if (TrackAuthorities.Count == 0)
				return DispatcherPOIType.Unknown;

			TrackAuthority auth = TrackAuthorities[0];
			//if (auth.StopNode == null || ptrain.FrontTDBTraveller.TrackNodeIndex < 0 || Reservations[ptrain.FrontTDBTraveller.TrackNodeIndex] != 0 || ptrain.RearTDBTraveller.TrackNodeIndex < 0 || Reservations[ptrain.RearTDBTraveller.TrackNodeIndex] != 0)
            if (auth.Path.FindTrackNode(auth.StartNode, ptrain.dFrontTDBTraveller.TrackNodeIndex) == null)
				return DispatcherPOIType.OffPath;

			distance = auth.StopDistanceM;
			backwards = auth.NReverseNodes % 2 == 1;
            if (auth.StationStop)
                return DispatcherPOIType.StationStop;
            if (auth.StopNode.Type == AIPathNodeType.Reverse)
                return DispatcherPOIType.ReversePoint;
			if (auth.EndNode == auth.StopNode)
				return DispatcherPOIType.EndOfAuthorization;
			return DispatcherPOIType.Stop;
		}

        public string PlayerStatus()
        {
            if (AI.Simulator.PlayerLocomotive == null)
                return null;
            Train ptrain = AI.Simulator.PlayerLocomotive.Train;
            bool reserved = Reservations[ptrain.FrontTDBTraveller.TrackNodeIndex] > 0 || Reservations[ptrain.RearTDBTraveller.TrackNodeIndex] > 0;
            if (!reserved && TimeTable == null)
                return null;
            string result = "";
            if (reserved)
                result+= "Warning: track reserved for AI\n";
            if (TimeTable != null && TimeTable.ContainsKey(0))
            {
                TTTrainTimes playerTT= TimeTable[0];
                if (playerTT == null || !playerTT.ContainsKey(ptrain.FrontTDBTraveller.TrackNodeIndex))
                    return result;
                TimeTableTime ttt= playerTT[ptrain.FrontTDBTraveller.TrackNodeIndex];
                result+= String.Format("Track Time: {0:D2}:{1:D2} to {2:D2}:{3:D2}\n",ttt.Arrive/3600,ttt.Arrive/60%60,ttt.Leave/3600,ttt.Leave/60%60);
            }
            return result;
        }
    }
}
