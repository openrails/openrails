// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

/// TrackAuthority
///
/// Used by Dispatcher to keep track of trains issued authorization to occupy track

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
        public AIPathNode LastValidNode;
        public AIPathNode InBetweenStartNode;
        public int NReverseNodes;
        public int Priority;
        public float StopDistanceM;
        public float DistanceDownPathM = 0;
        public float PathDistReverseAdjustmentM = 0;
        public bool StationStop = false;
        public List<float> StationDistanceM = null;
        public int PrevJunctionIndex = -1;
        private float prevTrainLength = -1;

        public TrackAuthority(Train train, int id, int priority, AIPath path)
        {
            Train = train;
            TrainID = id;
            Path = path;
            StartNode = path.FirstNode;
            StopNode = path.FirstNode;
            Priority = priority;
            InBetweenStartNode = StartNode;
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
                Train = Program.Simulator.Trains[0];
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
            LastValidNode = Path.ReadNode(inf);
            InBetweenStartNode = Path.ReadNode(inf);
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
            Path.WriteNode(outf, LastValidNode);
            Path.WriteNode(outf, InBetweenStartNode);
         }

        public struct Status
        {
            public int TrainID;
            public Train Train;
            public string Path;
        }

        public Status GetStatus()
        {
            StringBuilder s = new StringBuilder();
            int tvnIndex = -1;
            // By GeorgeS
            if (Train.FrontTDBTraveller == null)
                return new Status { TrainID = TrainID, Train = Train, Path = s.ToString() };
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
                        s.Append("#");
		    else if (sres >= 0)
			s.Append("-"+sres.ToString());
//                  else if (sres >= 0 && sres < 9)
//                      s.Append((char)('0' + sres));
//                  else if (sres >= 10 && sres < 36)
//                      s.Append((char)('A' + sres - 10));
                    else if (tvnIndex >= 0)
                        s.Append("_");

                }
                if (node.NextMainTVNIndex == tvnIndex)
                    continue;
                tvnIndex = node.NextMainTVNIndex;
                int res = Program.Simulator.AI.Dispatcher.GetReservation(tvnIndex);
                if (Train.FrontTDBTraveller.TrackNodeIndex == tvnIndex || Train.RearTDBTraveller.TrackNodeIndex == tvnIndex)
                    s.Append("@");
		else if (res >= 0)
			s.Append("+"+res.ToString());
//              else if (res >= 0 && res < 9)
//                  s.Append((char)('0' + res));
//              else if (res >= 10 && res < 36)
//                  s.Append((char)('A' + res - 10));
                else if (tvnIndex >= 0)
                    s.Append("=");
            }
            return new Status { TrainID = TrainID, Train = Train, Path = s.ToString() };
        }

#if DUMP_DISPATCHER
        public void Dump(Action<StringBuilder> dmpaction)
        {
            StringBuilder reservations = new StringBuilder();
            StringBuilder nodeids = new StringBuilder();
            StringBuilder travellers = new StringBuilder();
            StringBuilder signals = new StringBuilder();
            int tvnIndex = -1;
            int prevtvn = -1;

            reservations.Append("|Reservations||");
            nodeids.Append("|Node IDs||");
            travellers.Append("|Travellers, etc||");
            signals.Append("|Signals||");

            List<SignalObject> ls = new List<SignalObject>();

            if (Train.FrontTDBTraveller != null)
            {
                for (AIPathNode node = Path.FirstNode; node != null; node = node.NextMainNode)
                {
                    switch (node.Type)
                    {
                        case AIPathNodeType.Reverse: travellers.Append("?"); break;
                        case AIPathNodeType.SidingStart: travellers.Append("\\"); break;
                        case AIPathNodeType.SidingEnd: travellers.Append("/"); break;
                    }
                    for (AIPathNode snode = node.NextSidingNode; snode != null; snode = snode.NextSidingNode)
                    {
                        if (snode.NextSidingTVNIndex == tvnIndex)
                            continue;

                        prevtvn = tvnIndex;
                        tvnIndex = snode.NextSidingTVNIndex;

                        if (snode.JunctionIndex > 0)
                        {
                            //travellers.Append("-<");
                            travellers.Append(GetJunction(snode.JunctionIndex, prevtvn, tvnIndex));
                            nodeids.Append(snode.JunctionIndex);
                            int jres = Program.Simulator.AI.Dispatcher.GetReservation(snode.JunctionIndex);
                            if (jres > -1) reservations.Append(jres);
                            ls.Clear();
                            Program.Simulator.AI.Dispatcher.CountSignals(snode.JunctionIndex, s => { ls.Add(s); } );
                            signals.Append(string.Join(":", ls.Select<SignalObject, string>(s => string.Format("{0}[{1}]", s.thisRef, s.revDir)).ToArray()));
                            reservations.Append("|");
                            nodeids.Append("|");
                            travellers.Append("|");
                            signals.Append("|");
                        }

                        nodeids.Append(tvnIndex);
                        int sres = Program.Simulator.AI.Dispatcher.GetReservation(tvnIndex);
                        if (sres > -1) reservations.Append(sres);
                        if (Train.FrontTDBTraveller.TrackNodeIndex == tvnIndex)
                            travellers.Append("#");
                        if (Train.RearTDBTraveller.TrackNodeIndex == tvnIndex)
                            travellers.Append("#");
                        if (tvnIndex >= 0)
                            travellers.Append("_");
                        ls.Clear();
                        Program.Simulator.AI.Dispatcher.CountSignals(tvnIndex, s => { ls.Add(s); } );
                        signals.Append(string.Join(":", ls.Select<SignalObject, string>(s => string.Format("{0}[{1}]", s.thisRef, s.revDir)).ToArray()));

                        reservations.Append("|");
                        nodeids.Append("|");
                        travellers.Append("|");
                        signals.Append("|");
                    }
                    if (node.NextMainTVNIndex == tvnIndex)
                        continue;
                    prevtvn = tvnIndex;
                    tvnIndex = node.NextMainTVNIndex;
                    if (node.JunctionIndex > 0)
                    {
                        //travellers.Append("-<");
                        travellers.Append(GetJunction(node.JunctionIndex, prevtvn, tvnIndex));
                        nodeids.Append(node.JunctionIndex);
                        int jres = Program.Simulator.AI.Dispatcher.GetReservation(node.JunctionIndex);
                        if (jres > -1) reservations.Append(jres);
                        ls.Clear();
                        Program.Simulator.AI.Dispatcher.CountSignals(node.JunctionIndex, s => { ls.Add(s); } );
                        signals.Append(string.Join(":", ls.Select<SignalObject, string>(s => string.Format("{0}[{1}]", s.thisRef, s.revDir)).ToArray()));
                        reservations.Append("|");
                        nodeids.Append("|");
                        travellers.Append("|");
                        signals.Append("|");
                    }
                    nodeids.Append(tvnIndex);
                    int res = Program.Simulator.AI.Dispatcher.GetReservation(tvnIndex);
                    if (res > -1) reservations.Append(res);
                    if (Train.FrontTDBTraveller.TrackNodeIndex == tvnIndex)
                        travellers.Append("@");
                    if (Train.RearTDBTraveller.TrackNodeIndex == tvnIndex)
                        travellers.Append("@");
                    if (tvnIndex >= 0)
                        travellers.Append("=");
                    ls.Clear();
                    Program.Simulator.AI.Dispatcher.CountSignals(tvnIndex, s => { ls.Add(s); } );
                    signals.Append(string.Join(":", ls.Select<SignalObject, string>(s => string.Format("{0}[{1}]", s.thisRef, s.revDir)).ToArray()));

                    reservations.Append("|");
                    nodeids.Append("|");
                    travellers.Append("|");
                    signals.Append("|");
                }
            }

            StringBuilder result = new StringBuilder();
            result.AppendFormat("TrainID|{0}|Time|{1:00000}\r\n", TrainID, Program.Simulator.ClockTime);
            result.Append(reservations);
            result.AppendLine();
            result.Append(nodeids);
            result.AppendLine();
            result.Append(travellers);
            result.AppendLine();
            result.Append(signals);
            result.AppendLine();

            dmpaction(result);

            using (FileStream fs = File.Open(".\\dispatcher.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(result);
                    sw.Flush();
                }
            }
        }
#endif

        private string GetJunction(int junctionIndex, int link1, int link2)
        {
            string retval;
            TrackNode tn = Program.Simulator.TDB.TrackDB.TrackNodes[junctionIndex];
            if (tn.TrJunctionNode == null)
                return "<>";

            int selected = tn.TrPins[tn.TrJunctionNode.SelectedRoute + 1].Link;

            if (tn.TrPins[0].Link == link1 && selected == link2)
                retval = "'=_/¯";
            else if (tn.TrPins[0].Link == link2 && selected == link1)
                retval = "¯\\_=";
            else if (selected == link2)
                retval = "<X>";
            else if (tn.TrPins[0].Link == link1)
                retval = "'=X";
            else if (tn.TrPins[0].Link == link2)
                retval = "X=";
            else
                retval = "<???>";

            return retval;
        }

        public void UpdateTrainLength()
        {
            float newLen = Train.Length;
            if (prevTrainLength == -1)
            {
                prevTrainLength = newLen;
            }
            else if (prevTrainLength != newLen)
            {
                DistanceDownPathM += prevTrainLength;
                prevTrainLength = newLen;
                DistanceDownPathM -= prevTrainLength;
            }
        }

        /// <summary>
        /// Computes the StopDistanceM value, i.e. the distance from one end of the train to the StopNode.
        /// </summary>
        public void CalcStopDistance()
        {
            WorldLocation wl = StopNode.Location;
            Traveller traveller = Train.FrontTDBTraveller;
            if (Train.Reverse)
                traveller = new Traveller(Train.RearTDBTraveller, Traveller.TravellerDirection.Backward);

            StopDistanceM = traveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
            StationStop = false;
            //PathDistReverseAdjustmentM = 0;
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
            // By GeorgeS
            return;
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
                        Traveller rtraveller = Train.RearTDBTraveller;
                        if (NReverseNodes % 2 == 1)
                            rtraveller = new Traveller(Train.FrontTDBTraveller, Traveller.TravellerDirection.Backward);

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
            //AIPathNode node = StopNode;
            if (StartNode == null)
                return;
            AIPathNode node = GetNextNode(StartNode);
            if (node.NextMainNode == null && node.NextSidingNode == null)
                return;

            AIPathNode prevNode = Path.FirstNode;
            if (prevNode != StopNode)
                while (GetNextNode(prevNode) != StopNode) prevNode = GetNextNode(prevNode);

            while (true)
            {
                // GeorgeS bugfix
                // If endnode, it could throw a switch belonging to another reservation
                if (throwSwitches && prevNode != null)
                    Path.AlignSwitch(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode));

                if (node == EndNode)
                    break;

                if (Train.dFrontTDBTraveller.DistanceTo(node.Location.TileX, node.Location.TileZ,
                    node.Location.Location.X, node.Location.Location.Y, node.Location.Location.Z) == -1)
                    return;

                prevNode = node;
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
                // GeorgeS bugfix
                // If endnode, it could throw a switch belonging to another reservation
                //if (throwSwitches)
                //    Path.AlignSwitch(node.JunctionIndex, node.IsFacingPoint ? GetTVNIndex(node) : GetTVNIndex(prevNode));
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
