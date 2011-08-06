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
        private int[] Reservations;
        public float[] TrackLength;
        private TimeTable TimeTable = null;
        public int PlayerPriority = 0;
        public List<TrackAuthority> TrackAuthorities = new List<TrackAuthority>();
        private float UpdateTimerS = 60;

        /// <summary>
        /// Initializes the dispatcher.
        /// Creates an array for saving track node reservations and initializes it to no reservations.
        /// </summary>
        public Dispatcher(AI ai)
        {
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
            RequestAuth(auth, true, true);
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

        /// <summary>
        /// Updates dispatcher information.
        /// Moves each train's rear path node forward and updates reservations.
        /// </summary>
        public void Update(double clockTime, float elapsedClockSeconds)
        {
            UpdateTimerS -= elapsedClockSeconds;
            foreach (TrackAuthority auth in TrackAuthorities)
            {
                if (auth.EndNode == null || auth.StartNode == null)
                    continue;
                if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
                {
                    auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                    if (auth.NReverseNodes % 2 == 0)
                        auth.DistanceDownPathM += elapsedClockSeconds * auth.Train.SpeedMpS;
                    else
                        auth.DistanceDownPathM -= elapsedClockSeconds * auth.Train.SpeedMpS;
                    if (auth.StopNode == auth.StartNode)
                    {
                        auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    else if (auth.EndNode == auth.StopNode && UpdateTimerS < 0 && auth.StopDistanceM < 4*auth.Train.SpeedMpS*auth.Train.SpeedMpS + 500)
                    {
                        RequestAuth(auth, true, auth.NReverseNodes % 2 == 0);
                        if (auth.EndNode != auth.StopNode)
                            auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    else if (auth.NReverseNodes % 2 == 1)
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
                        auth.DistanceDownPathM += 2 * auth.PathDistReverseAdjustmentM;
                        auth.StartNode = auth.StopNode;
                        auth.NReverseNodes--;
                        Rereserve(auth);
                        auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                        if (auth.NReverseNodes == 0)
                            UpdateTimerS = 0;
                    }
                    else if (auth.StopDistanceM < 0 && auth.StopNode != auth.EndNode)
                    {
                        auth.AdvanceStopNode(true);
                        auth.CalcStopDistance();
                    }
                    if (auth.NReverseNodes % 2 == 1 && auth.StartNode.Type == AIPathNodeType.Reverse)
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
                            //Console.WriteLine("new rev r {0}", train.RearNode.ID);
                        }
                        continue;
                    }
                    if (auth.NReverseNodes > 0 && auth.NReverseNodes % 2 == 1)
                    {
                        auth.NReverseNodes--;
                        auth.StartNode = FindNextReverseNode(auth);
                        ///Console.WriteLine("new rev f {0}", train.RearNode.ID);
                        if (auth.NReverseNodes == 0)
                            Rereserve(auth);
                    }
                    if (auth.NReverseNodes > 0 && auth.StartNode.Type == AIPathNodeType.Reverse)
                        continue;
                }
                if (auth.StartNode.NextMainTVNIndex == auth.Train.RearTDBTraveller.TrackNodeIndex ||
                  auth.StartNode.NextSidingTVNIndex == auth.Train.RearTDBTraveller.TrackNodeIndex ||
                  auth.Train.RearTDBTraveller.TN.TrVectorNode == null)
                    continue;
                if (auth.TrainID == 0 && Reservations[auth.Train.RearTDBTraveller.TrackNodeIndex] != auth.TrainID)
                    continue;
                AIPathNode nextNode = auth.Path.FindTrackNode(auth.StartNode, auth.Train.RearTDBTraveller.TrackNodeIndex);
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
                    float d = WorldLocation.DistanceSquared(nextNode.Location, auth.Train.RearTDBTraveller.WorldLocation);
                    //Console.WriteLine("{0} {1}", d, clearance);
                    if (d < clearance * clearance)
                        continue;
                }
                int i = auth.StartNode.NextMainTVNIndex;
                //Console.WriteLine("dispatcher update {0} {1} {2}", auth.TrainID, i, auth.Train.RearTDBTraveller.TrackNodeIndex);
                if (i >= 0 && Reservations[i] == auth.TrainID)
                    Reservations[i] = -1;
                else
                {
                    i = auth.StartNode.NextSidingTVNIndex;
                    //Console.WriteLine(" siding {0} {1}", i, train.UiD);
                    if (i >= 0 && Reservations[i] == auth.TrainID)
                        Reservations[i] = -1;
                }
                //int n = 0;
                //for (int j = 0; j < Reservations.Length; j++)
                //    if (Reservations[j] == auth.TrainID)
                //        n++;
                //Console.WriteLine(" nres {0}", n);
                if (auth.StartNode.IsLastSwitchUse)
                    auth.Path.RestoreSwitch(auth.StartNode.JunctionIndex);
                auth.StartNode = nextNode;
            }
            if (UpdateTimerS < 0)
                UpdateTimerS = 10;
        }
        public void ExtendPlayerAuthorization()
        {
            if (TrackAuthorities.Count == 0)
                return;            

            TrackAuthority auth = TrackAuthorities[0];
            if (auth.TrainID == 0 && AI.Simulator.PlayerLocomotive != null)
            {
                auth.Train = AI.Simulator.PlayerLocomotive.Train;// this can change due to uncoupling
                RequestAuth(auth, true, auth.NReverseNodes % 2 == 0);
                if (auth.EndNode != auth.StopNode)
                    auth.AdvanceStopNode(true);
                auth.CalcStopDistance();
            }
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
            return RequestAuth(train.TrackAuthority, update, train.AITrainDirectionForward);
        }
        private bool RequestAuth(TrackAuthority auth, bool update, bool movingForward)
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
            AIPathNode node = auth.StartNode;
            int nRev = 0;
            if (!movingForward)
                nRev++;
            //Console.WriteLine("reqa {0} {1}", train.UiD, update);
            while (node != null)
            {
                //Console.WriteLine(" node {0} {1}", node.ID, node.Type);
                if (movingForward && node != auth.StartNode && node.Type == AIPathNodeType.SidingStart)
                    break;
                if (movingForward && node.Type == AIPathNodeType.SidingEnd && node != auth.EndNode && Reservations[node.NextMainTVNIndex] != auth.TrainID)
                    break;
                if (node != auth.StartNode && node.Type == AIPathNodeType.Reverse)
                {
                    movingForward = !movingForward;
                    nRev++;
                    //Console.WriteLine("rev node {0}", node.ID);
                }
                if (node.NextMainNode != null && node != auth.SidingNode)
                {
                    tnList.Add(node.NextMainTVNIndex);
                    node = node.NextMainNode;
                }
                else if (node.NextSidingNode != null)
                {
                    tnList.Add(node.NextSidingTVNIndex);
                    node = node.NextSidingNode;
                }
                else
                    break;
            }
            if (node == null || !CanReserve(auth.TrainID, auth.Priority, tnList))
                return false;
            if (node.Type != AIPathNodeType.SidingStart)
            {
                Unreserve(auth.TrainID);
                Reserve(auth.TrainID, tnList);
                return SetAuthorization(auth, node, null, nRev);
            }
            //Console.WriteLine("start siding {0}", node.ID);
            List<int> tnList1 = new List<int>();
            AIPathNode sidingNode = node;
            int nReverse = nRev;
            bool forward = movingForward;
            bool sidingFirst = !update;
            if (sidingFirst)
            {
                WorldLocation wl = sidingNode.Location;
                if (auth.Train.FrontTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 10)
                    sidingFirst = false;
            }
            for (int i = 0; i < 2; i++)
            {
                tnList1.Clear();
                nRev = nReverse;
                movingForward = forward;
                if (sidingFirst ? i == 1 : i == 0)
                {
                    //Console.WriteLine("try main {0}", node.ID);
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
                        Unreserve(auth.TrainID);
                        Reserve(auth.TrainID, tnList);
                        Reserve(auth.TrainID, tnList1);
                        //Console.WriteLine("got main {0}", node.ID);
                        return SetAuthorization(auth, node, null, nRev);
                    }
                }
                else
                {
                    //Console.WriteLine("try siding {0}", node.ID);
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
                        Unreserve(auth.TrainID);
                        Reserve(auth.TrainID, tnList);
                        Reserve(auth.TrainID, tnList1);
                        //Console.WriteLine("got siding {0} {1}", node.ID, sidingNode.ID);
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
            int n = 0;
            for (int j = 0; j < Reservations.Length; j++)
                if (Reservations[j] == auth.TrainID)
                    n++;
            //Console.WriteLine("setauth {0} {1} {2} {3}", auth.TrainID, result, n, nRev);
            //for (int j = 0; j < Reservations.Length; j++)
            //    if (Reservations[j] == auth.TrainID)
            //        Console.WriteLine(" res {0}", j);
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
            //foreach (int i in tnList)
            //    Console.WriteLine("res {0} {1} {2}", i, Reservations[i], train.UiD);
            foreach (int i in tnList)
                if (Reservations[i] >= 0 && Reservations[i] != trainID)
                    return false;
            if (trainID != 0 && PlayerPriority <= priority && AI.Simulator.PlayerLocomotive != null)
            {
                Train playerTrain = AI.Simulator.PlayerLocomotive.Train;
                foreach (int j in tnList)
                    if (Reservations[j] != trainID && (j == playerTrain.FrontTDBTraveller.TrackNodeIndex || j == playerTrain.RearTDBTraveller.TrackNodeIndex))
                    {
                        //Console.WriteLine("player on track {0} {1}", j, Reservations[j]);
                        return false;
                    }
            }
            //Console.WriteLine("can reserve");
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
        private void Unreserve(int trainID)
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
            //Console.WriteLine("release ai {0}", train.UiD);
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
            Unreserve(auth.TrainID);
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
            //Console.WriteLine("rereserve {0}", train.UiD);
            //for (int j = 0; j < Reservations.Length; j++)
            //    if (Reservations[j] == train.UiD)
            //        Console.WriteLine(" res {0}", j);
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
                        //Console.WriteLine("junction {0} {1} {2} {3}", train.UiD, node.JunctionIndex, f, node.Type);
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
                        //Console.WriteLine("junction {0} {1} {2} {3}", train.UiD, node.JunctionIndex, f, node.Type);
                    }
                    //if (node.Type == AIPathNodeType.SidingEnd)
                    //    Console.WriteLine("meet point {0} {1} {2}", train.UiD, node.JunctionIndex, node.Type);
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
                    //if (ts == null)
                    //    Console.WriteLine("no tracksection {0} {1} {2}", i, j, k);
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
                //Console.WriteLine("TrackLength {0} {1}", i, TrackLength[i]);
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
			if (auth.StopNode == null || ptrain.FrontTDBTraveller.TrackNodeIndex < 0 || Reservations[ptrain.FrontTDBTraveller.TrackNodeIndex] != 0 || ptrain.RearTDBTraveller.TrackNodeIndex < 0 || Reservations[ptrain.RearTDBTraveller.TrackNodeIndex] != 0)
				return DispatcherPOIType.OffPath;

			distance = auth.StopDistanceM;
			backwards = auth.NReverseNodes % 2 == 1;
			if (auth.StationStop)
				return DispatcherPOIType.StationStop;
			if (auth.EndNode == auth.StopNode)
				return DispatcherPOIType.EndOfAuthorization;
			if (auth.StopNode.Type == AIPathNodeType.Reverse)
				return DispatcherPOIType.ReversePoint;
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
