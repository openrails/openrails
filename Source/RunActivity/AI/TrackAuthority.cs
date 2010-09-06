/// TrackAuthority
///
/// Used by Dispatcher to keep track of trains issued authorization to occupy track
/// 
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    public class TrackAuthority
    {
        public int TrainID;
        public Train Train;
        public AIPath Path;
        public AIPathNode StartNode;
        public AIPathNode EndNode;
        public AIPathNode SidingNode;
        public AIPathNode StopNode;
        public int NReverseNodes;
        public int Priority;
        public float StopDistanceM;
        public float DistanceDownPathM = 0;
        public float PathDistReverseAdjustmentM = 0;
        public bool StationStop = false;
        public List<float> StationDistanceM = null;

        public TrackAuthority(Train train, int id, int priority, AIPath path)
        {
            Train = train;
            TrainID = id;
            Path = path;
            StartNode = path.FirstNode;
            StopNode = path.FirstNode;
            Priority = priority;
        }

        // restore game state
        public TrackAuthority(BinaryReader inf, AI ai)
        {
            TrainID = inf.ReadInt32();
            Priority = inf.ReadInt32();
            NReverseNodes = inf.ReadInt32();
            StopDistanceM = inf.ReadSingle();
            DistanceDownPathM = inf.ReadSingle();
            StationStop = inf.ReadBoolean();
            int n = inf.ReadInt32();
            if (n > 0)
            {
                StationDistanceM = new List<float>();
                for (int i = 0; i < n; i++)
                    StationDistanceM.Add(inf.ReadSingle());
            }
            if (TrainID == 0)
            {
                //Train set on first update
                Path = new AIPath(inf);
                Path.TrackDB = Program.Simulator.TDB.TrackDB;
                Path.TSectionDat = Program.Simulator.TSectionDat;
            }
            else
            {
                AITrain aiTrain = ai.AITrainDictionary[TrainID];
                Train = aiTrain;
                Path = aiTrain.Path;
                aiTrain.TrackAuthority = this;
            }
            StartNode = Path.ReadNode(inf);
            EndNode = Path.ReadNode(inf);
            SidingNode = Path.ReadNode(inf);
            StopNode = Path.ReadNode(inf);
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(TrainID);
            outf.Write(Priority);
            outf.Write(NReverseNodes);
            outf.Write(StopDistanceM);
            outf.Write(DistanceDownPathM);
            outf.Write(StationStop);
            int n = StationDistanceM != null ? StationDistanceM.Count : 0;
            outf.Write(n);
            for (int i = 0; i < n; i++)
                outf.Write(StationDistanceM[i]);
            if (TrainID == 0)
                Path.Save(outf);
            Path.WriteNode(outf,StartNode);
            Path.WriteNode(outf,EndNode);
            Path.WriteNode(outf,SidingNode);
            Path.WriteNode(outf, StopNode);
         }

        public string GetStatus()
        {
            StringBuilder s = new StringBuilder();
            s.Append(string.Format("{0} {1:F1} ", TrainID, Train.SpeedMpS));
            int tvnIndex = -1;
            for (AIPathNode node = Path.FirstNode; node != null; node = node.NextMainNode)
            {
                switch (node.Type)
                {
                    case AIPathNodeType.Reverse: s.Append("?"); break;
                    case AIPathNodeType.SidingStart: s.Append("\\"); break;
                    case AIPathNodeType.SidingEnd: s.Append("/"); break;
                }
                for (AIPathNode snode = node.NextSidingNode; snode != null; snode = snode.NextSidingNode)
                {
                    if (snode.NextSidingTVNIndex == tvnIndex)
                        continue;
                    tvnIndex = snode.NextSidingTVNIndex;
                    int sres= Program.Simulator.AI.Dispatcher.GetReservation(tvnIndex);
                    if (Train.FrontTDBTraveller.TrackNodeIndex == tvnIndex || Train.RearTDBTraveller.TrackNodeIndex == tvnIndex)
                        s.Append("@");
                    else if (sres >= 0 && sres < 9)
                        s.Append((char)('0' + sres));
                    else if (sres >= 10 && sres < 36)
                        s.Append((char)('A' + sres - 10));
                    else if (tvnIndex >= 0)
                        s.Append("_");

                }
                if (node.NextMainTVNIndex == tvnIndex)
                    continue;
                tvnIndex = node.NextMainTVNIndex;
                int res = Program.Simulator.AI.Dispatcher.GetReservation(tvnIndex);
                if (Train.FrontTDBTraveller.TrackNodeIndex == tvnIndex || Train.RearTDBTraveller.TrackNodeIndex == tvnIndex)
                    s.Append("@");
                else if (res >= 0 && res < 9)
                    s.Append((char)('0' + res));
                else if (res >= 10 && res < 36)
                    s.Append((char)('A' + res - 10));
                else if (tvnIndex >= 0)
                    s.Append("=");
            }
            return s.ToString();
        }

        /// <summary>
        /// Computes the StopDistanceM value, i.e. the distance from one end of the train to the StopNode.
        /// </summary>
        public void CalcStopDistance()
        {
            WorldLocation wl = StopNode.Location;
            TDBTraveller traveller = Train.FrontTDBTraveller;
            if (NReverseNodes%2 == 1)
            {
                traveller = new TDBTraveller(Train.RearTDBTraveller);
                traveller.ReverseDirection();
            }
            StopDistanceM = traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
            StationStop = false;
            PathDistReverseAdjustmentM = 0;
            float len = 0;
            foreach (TrainCar car in Train.Cars)
                len+= car.Length;
            if (StationDistanceM != null && StationDistanceM.Count > 0)
            {
                while (StationDistanceM.Count > 0)
                {
                    float d = StationDistanceM[0] - DistanceDownPathM - len;
                    if (d < 0)
                    {
                        StationDistanceM.RemoveAt(0);
                        continue;
                    }
                    if (StopDistanceM < d)
                        break;
                    StationStop = true;
                    StopDistanceM = d;
                    break;
                }
            }
            //Console.WriteLine("nextstopdist {0} {1} {2} {3}", StopDistanceM, FrontTDBTraveller.Direction, RearTDBTraveller.Direction,
            //    Math.Sqrt(WorldLocation.DistanceSquared(wl,FrontTDBTraveller.WorldLocation)));
            if (StopNode.Type == AIPathNodeType.Reverse)
            {
                for (AIPathNode node = StopNode; node != EndNode; )
                {
                    AIPathNode prevNode = node;
                    node = GetNextNode(node);
                    if (node.Type == AIPathNodeType.Reverse || node.Type == AIPathNodeType.Stop)
                        break;
                    if (!Path.SwitchIsAligned(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode)))
                    {
                        wl = node.Location;
                        TDBTraveller rtraveller = Train.RearTDBTraveller;
                        if (NReverseNodes % 2 == 1)
                        {
                            rtraveller = new TDBTraveller(Train.FrontTDBTraveller);
                            rtraveller.ReverseDirection();
                        }
                        float d = rtraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
                        if (d > 0 && d + 1 < StopDistanceM)
                            PathDistReverseAdjustmentM = StopDistanceM - d - 1;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Moves the StopNode to the next path node the train should stop at.
        /// </summary>
        public void AdvanceStopNode(bool throwSwitches)
        {
            AIPathNode node = StopNode;
            if (node.NextMainNode == null && node.NextSidingNode == null)
                return;
            while (node != EndNode)
            {
                AIPathNode prevNode = node;
                node = GetNextNode(node);
                if (node == null)
                    return;
                switch (node.Type)
                {
                    case AIPathNodeType.Stop:
                    case AIPathNodeType.Reverse:
                    case AIPathNodeType.Uncouple:
                        StopNode = node;
                        return;
                    default:
                        break;

                }
                if (throwSwitches)
                    Path.AlignSwitch(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode));
            }
            StopNode = node;
        }
        public AIPathNode GetNextNode(AIPathNode node)
        {
            if (node == SidingNode || node.NextMainNode == null)
                return node.NextSidingNode;
            else
                return node.NextMainNode;
        }
        public int GetTVNIndex(AIPathNode node)
        {
            if (node == SidingNode || node.NextMainNode == null)
                return node.NextSidingTVNIndex;
            else
                return node.NextMainTVNIndex;
        }

    }
}
