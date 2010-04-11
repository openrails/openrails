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
    public class Dispatcher
    {
        public AI AI;
        private int[] Reservations;
        public float[] TrackLength;
        private TimeTable TimeTable = null;
        public int PlayerPriority = 0;

        /// <summary>
        /// Initializes the dispatcher.
        /// Creates an array for saving track node reservations and initializes it to no reservations.
        /// </summary>
        public Dispatcher(AI ai)
        {
            AI = ai;
            // make a temporary AITrain for the player
            string playerServiceFileName = AI.Simulator.Activity.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name;
            SRVFile srvFile = new SRVFile(AI.Simulator.RoutePath + @"\SERVICES\" + playerServiceFileName + ".SRV");
            CONFile conFile = new CONFile(AI.Simulator.BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON");
            PATFile patFile = new PATFile(AI.Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT");
            AIPath playerPath = new AIPath(patFile, AI.Simulator.TDB, AI.Simulator.TSectionDat);
            AITrain playerTrain = new AITrain(0, AI, playerPath, (int)AI.Simulator.ClockTime);
            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                playerTrain.MaxSpeedMpS = conFile.Train.TrainCfg.MaxVelocity.A * srvFile.Efficiency;
            AI.AITrainDictionary.Add(0, playerTrain);
            Reservations = new int[ai.Simulator.TDB.TrackDB.TrackNodes.Length];
            for (int i = 0; i < Reservations.Length; i++)
                Reservations[i] = -1;
            FindDoubleTrack();
            CalcTrackLength();
            PlayerPriority = AI.Simulator.Activity.Tr_Activity.Tr_Activity_Header.StartTime.Second % 10;
            if (AI.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing.Contains("OR Dispatcher: Priority"))
                TimeTable = new TimeTable(this);
            AI.AITrainDictionary.Remove(0);
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
         }

        /// <summary>
        /// Updates dispatcher information.
        /// Moves each train's rear path node forward and updates reservations.
        /// </summary>
        public void Update(double clockTime)
        {
            foreach (AITrain train in AI.AITrains)
            {
                if (!train.AITrainDirectionForward)
                {
                    if (train.NReverseNodes > 0 && train.NReverseNodes % 2 == 0)
                    {
                        train.NReverseNodes--;
                        train.RearNode = FindNextReverseNode(train);
                        //Console.WriteLine("new rev r {0}", train.RearNode.ID);
                    }
                    continue;
                }
                if (train.NReverseNodes > 0 && train.NReverseNodes % 2 == 1)
                {
                    train.NReverseNodes--;
                    train.RearNode = FindNextReverseNode(train);
                    ///Console.WriteLine("new rev f {0}", train.RearNode.ID);
                    if (train.NReverseNodes == 0)
                        Rereserve(train);
                }
                if (train.NReverseNodes > 0 && train.RearNode.Type == AIPathNodeType.Reverse)
                    continue;
                if (train.RearNode.NextMainTVNIndex == train.RearTDBTraveller.TrackNodeIndex ||
                  train.RearNode.NextSidingTVNIndex == train.RearTDBTraveller.TrackNodeIndex ||
                  train.RearTDBTraveller.TN.TrVectorNode == null)
                    continue;
                int i = train.RearNode.NextMainTVNIndex;
                //Console.WriteLine("dispatcher update {0} {1}", i, train.UiD);
                if (i >= 0 && Reservations[i] == train.UiD)
                    Reservations[i] = -1;
                else
                {
                    i = train.RearNode.NextSidingTVNIndex;
                    //Console.WriteLine(" siding {0} {1}", i, train.UiD);
                    if (i >= 0 && Reservations[i] == train.UiD)
                        Reservations[i] = -1;
                }
                //for (int j = 0; j < Reservations.Length; j++)
                //    if (Reservations[j] == train.UiD)
                //        Console.WriteLine(" res {0}", j);
                if (train.RearNode.IsLastSwitchUse)
                    train.Path.RestoreSwitch(train.RearNode.JunctionIndex);
                train.RearNode = train.Path.FindTrackNode(train.RearNode, train.RearTDBTraveller.TrackNodeIndex);
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
            TTTrainTimes ttTimes = null;
            if (TimeTable != null)
            {
                if (!TimeTable.ContainsKey(train.UiD))
                    return false;
                ttTimes= TimeTable[train.UiD];
                if (train.NextStopNode == train.AuthEndNode)
                {
                    int ji = train.NextStopNode.JunctionIndex;
                    if (!ttTimes.ContainsKey(ji) || ttTimes[ji].Arrive > AI.Simulator.ClockTime)
                        return false;
                }
            }
            List<int> tnList = new List<int>();
            AIPathNode node = train.RearNode;
            bool movingForward = train.AITrainDirectionForward;
            int nRev = 0;
            if (!movingForward)
                nRev++;
            //Console.WriteLine("reqa {0} {1}", train.UiD, update);
            while (node != null)
            {
                //Console.WriteLine(" node {0} {1}", node.ID, node.Type);
                if (movingForward && node != train.RearNode && node.Type == AIPathNodeType.SidingStart)
                    break;
                if (movingForward && node.Type == AIPathNodeType.SidingEnd && node != train.AuthEndNode && Reservations[node.NextMainTVNIndex] != train.UiD)
                    break;
                if (node != train.RearNode && node.Type == AIPathNodeType.Reverse)
                {
                    movingForward = !movingForward;
                    nRev++;
                    //Console.WriteLine("rev node {0}", node.ID);
                }
                if (node.NextMainNode != null && node != train.AuthSidingNode)
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
            if (node == null || !CanReserve(train, tnList))
                return false;
            if (node.Type != AIPathNodeType.SidingStart)
            {
                Unreserve(train);
                Reserve(train, tnList);
                return train.SetAuthorization(node, null, nRev);
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
                if (train.FrontTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 10)
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
                    if (CanReserve(train, tnList1))
                    {
                        Unreserve(train);
                        Reserve(train, tnList);
                        Reserve(train, tnList1);
                        //Console.WriteLine("got main {0}", node.ID);
                        return train.SetAuthorization(node, null, nRev);
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
                    if (CanReserve(train, tnList1))
                    {
                        Unreserve(train);
                        Reserve(train, tnList);
                        Reserve(train, tnList1);
                        //Console.WriteLine("got siding {0} {1}", node.ID, sidingNode.ID);
                        return train.SetAuthorization(node, sidingNode, nRev);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see is the listed track nodes can be reserved for the specified train.
        /// return true if none of the nodes are already reserved for another train.
        /// </summary>
        private bool CanReserve(AITrain train, List<int> tnList)
        {
            //foreach (int i in tnList)
            //    Console.WriteLine("res {0} {1} {2}", i, Reservations[i], train.UiD);
            foreach (int i in tnList)
                if (Reservations[i] >= 0 && Reservations[i] != train.UiD)
                    return false;
            if (PlayerPriority <= train.Priority && AI.Simulator.PlayerLocomotive != null)
            {
                Train playerTrain = AI.Simulator.PlayerLocomotive.Train;
                foreach (int j in tnList)
                    if (Reservations[j] != train.UiD && (j == playerTrain.FrontTDBTraveller.TrackNodeIndex || j == playerTrain.RearTDBTraveller.TrackNodeIndex))
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
        private void Reserve(AITrain train, List<int> tnList)
        {
            foreach (int i in tnList)
                Reservations[i] = train.UiD;
        }

        /// <summary>
        /// Clears any existing Reservations for the specified train.
        /// </summary>
        private void Unreserve(AITrain train)
        {
            for (int i = 0; i < Reservations.Length; i++)
                if (Reservations[i] == train.UiD)
                    Reservations[i] = -1;
        }

        /// <summary>
        /// Releases the specified train's movement authorization.
        /// </summary>
        public void Release(AITrain train)
        {
            //Console.WriteLine("release ai {0}", train.UiD);
            train.SetAuthorization(null, null, 0);
            Unreserve(train);
        }

        /// <summary>
        /// Releases the specified train's movement authorization.
        /// </summary>
        public void Rereserve(AITrain train)
        {
            Unreserve(train);
            for (AIPathNode node = train.RearNode; node != null && node != train.AuthEndNode; )
            {
                if (node != train.AuthSidingNode && node.NextMainNode != null)
                {
                    Reservations[node.NextMainTVNIndex] = train.UiD;
                    node = node.NextMainNode;
                }
                else if (node.NextSidingNode != null)
                {
                    Reservations[node.NextSidingTVNIndex] = train.UiD;
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
        AIPathNode FindNextReverseNode(AITrain train)
        {
            for (AIPathNode node = train.RearNode; node != null && node != train.AuthEndNode; )
            {
                if (node != train.RearNode && node.Type == AIPathNodeType.Reverse)
                    return node;
                if (node.IsLastSwitchUse)
                    train.Path.RestoreSwitch(node.JunctionIndex);
                if (node != train.AuthSidingNode && node.NextMainNode != null)
                    node = node.NextMainNode;
                else if (node.NextSidingNode != null)
                    node = node.NextSidingNode;
                else
                    break;
            }
            return train.RearNode;
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
                        if ((f & 0x9) == 0x9 || (f & 0x6) == 0x6)
                            node.Type = AIPathNodeType.SidingEnd;
                        //Console.WriteLine("junction {0} {1} {2} {3}", train.UiD, node.JunctionIndex, f, node.Type);
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
                result+= "Warning: track reserved for AI";
            if (TimeTable != null && TimeTable.ContainsKey(0))
            {
                TTTrainTimes playerTT= TimeTable[0];
                if (playerTT == null || !playerTT.ContainsKey(ptrain.FrontTDBTraveller.TrackNodeIndex))
                    return result;
                TimeTableTime ttt= playerTT[ptrain.FrontTDBTraveller.TrackNodeIndex];
                if (reserved)
                    result+= "\n";
                result+= String.Format("Track Time: {0:D2}:{1:D2} to {2:D2}:{3:D2}",ttt.Arrive/3600,ttt.Arrive/60%60,ttt.Leave/3600,ttt.Leave/60%60);
            }
            return result;
        }
    }
}
