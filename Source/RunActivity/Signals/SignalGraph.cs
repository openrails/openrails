/// COPYRIGHT 2011 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using MSTS;

namespace ORTS
{
    /// <summary>
    /// Undirected graph for managing signal and track circuit interconnections
    /// Used to track train/track occupancy and update signal block state
    /// </summary>
    public class SignalGraph
    {
        List<SignalGraphVertex> Vertices = new List<SignalGraphVertex>();
        List<SignalGraphEdge> Edges = new List<SignalGraphEdge>();
        SignalGraphVertex[] VertexMap = null;
        private TrackDB TrackDB;
        private Signals Signals;
        bool dirty = true;  // true if all signals need to be updated to track circuit status
        /// <summary>
        /// Creates the signal graph from the TDB.
        /// Should be created after SignalObjects are created.
        /// </summary>
        /// <param name="sim"></param>
        /// <param name="signals"></param>
        public SignalGraph(Simulator sim, Signals signals)
        {
            Signals = signals;
            TrackDB = sim.TDB.TrackDB;
            VertexMap = new SignalGraphVertex[TrackDB.TrackNodes.Length];
            for (int i = 0; i < TrackDB.TrackNodes.Length; i++)
            {
                TrackNode tn = TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode != null)
                    continue;
                AddVertex(i, tn.TrJunctionNode);
            }
            SignalGraphEdge[] edgeMap = new SignalGraphEdge[TrackDB.TrackNodes.Length];
            for (int i = 0; i < TrackDB.TrackNodes.Length; i++) 
            {
                TrackNode tn = TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                float length = 0;
                for (int j = 0; j < tn.TrVectorNode.TrVectorSections.Length; j++)
                {
                    uint k = tn.TrVectorNode.TrVectorSections[j].SectionIndex;
                    TrackSection ts = sim.TSectionDat.TrackSections.Get(k);
                    //if (ts == null)
                    //    Console.WriteLine("no tracksection {0} {1} {2}", i, j, k);
                    if (ts == null)
                        continue;
                    if (ts.SectionCurve == null)
                        length += ts.SectionSize.Length;
                    else
                    {
                        float len = ts.SectionCurve.Radius * MSTSMath.M.Radians(ts.SectionCurve.Angle);
                        if (len < 0)
                            len = -len;
                        length += len;
                    }
                }
                //Trace.WriteLine(string.Format("edge {0} {1} {2} {3}", i, length, tn.TrPins[0].Link, tn.TrPins[1].Link));
                SignalGraphVertex v1 = VertexMap[tn.TrPins[0].Link];
                int p1 = FindPinIndex(v1.GetNodeIndex(), i, 1);
                float offset = 0;
                for (int j = 0; j < tn.TrVectorNode.noItemRefs; j++)
                {
                    TrItem trItem= TrackDB.TrItemTable[tn.TrVectorNode.TrItemRefs[j]];
                    if (trItem.ItemType != TrItem.trItemType.trSIGNAL)
                        continue;
                    SignalItem sigItem= (SignalItem)trItem;
                    SignalObject sig = Signals.SignalObjects[sigItem.sigObj];
                    float x = sigItem.SData1 - offset;
                    if (x > 0)
                    {
                        SignalGraphVertex v = AddVertex(i, null);
                        SignalGraphEdge e1 = AddEdge(i, v1, p1, v, 0);
                        e1.SetLength(x);
                        length -= x;
                        offset = sigItem.SData1;
                        v1 = v;
                        p1 = 1;
                    }
                    v1.SetSignal((int)sigItem.Direction, sig);
                    //Trace.WriteLine(string.Format("signal {0} {1} {2} {3} {4} {5} {6}", tn.TrVectorNode.TrItemRefs[j], sigItem.Direction, sigItem.noSigDirs, sigItem.SData1, sigItem.sigObj, sig.trackNode, sig.trRefIndex));
                }
                SignalGraphVertex v2 = VertexMap[tn.TrPins[1].Link];
                int p2 = FindPinIndex(v2.GetNodeIndex(), i, 0);
                SignalGraphEdge e = AddEdge(i, v1, p1, v2, p2);
                e.SetLength(length);
                edgeMap[i] = e;
            }
            //UpdateSignals();
        }
        /// <summary>
        /// Returns the TrPins index in node1 that equals node2.
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        int FindPinIndex(int node1, int node2, int node2dir)
        {
            TrackNode tn = TrackDB.TrackNodes[node1];
            for (int i = 0; i < tn.TrPins.Length; i++)
                if (tn.TrPins[i].Link == node2 && tn.TrPins[i].Direction==node2dir)
                    return i;
            return -1;
        }
        /// <summary>
        /// Adds a vertex to the signal graph.
        /// </summary>
        /// <param name="nodeIndex">track node this vertex corresponds to</param>
        /// <param name="jNode">junction node if junction</param>
        /// <returns></returns>
        SignalGraphVertex AddVertex(int nodeIndex, TrJunctionNode jNode)
        {
            SignalGraphVertex v = new SignalGraphVertex(nodeIndex, jNode);
            Vertices.Add(v);
            VertexMap[nodeIndex] = v;
            return v;
        }
        /// <summary>
        /// Adds an edge to the signal graph connecting v1 and v2.
        /// </summary>
        /// <param name="nodeIndex">vector node this corresponds to</param>
        /// <param name="v1">vertex at one end</param>
        /// <param name="p1">pin index for v1</param>
        /// <param name="v2">vertex at other end</param>
        /// <param name="p2">pin index for v2</param>
        /// <returns></returns>
        SignalGraphEdge AddEdge(int nodeIndex, SignalGraphVertex v1, int p1, SignalGraphVertex v2, int p2)
        {
            SignalGraphEdge e = new SignalGraphEdge(nodeIndex, v1, v2);
            v1.SetEdge(p1, e);
            v2.SetEdge(p2, e);
            e.SetTrackCircuit(new TrackCircuit()); // one track circuit per edge initially
            return e;
        }
        /// <summary>
        /// Finds the signal graph location that corresponds to a TDBTraveller value.
        /// </summary>
        /// <param name="traveller"></param>
        /// <returns></returns>
        public SignalGraphLocation FindLocation(TDBTraveller traveller)
        {
            SignalGraphLocation loc = new SignalGraphLocation();
            if (traveller.TN.TrVectorNode == null)
            {
                SignalGraphVertex v = VertexMap[traveller.TrackNodeIndex];
                SignalGraphEdge e = v.GetEdge(0);
                loc.Set(e, v == e.V1() ? 0 : e.GetLength(), false);//this isn't right
            }
            else
            {
                int i = traveller.TN.TrPins[traveller.Direction].Link;
                //Trace.WriteLine(string.Format("findloc {0} {1} {2} {3}", traveller.TrackNodeIndex, traveller.Direction, traveller.TN.TrPins[0].Link, traveller.TN.TrPins[1].Link));
                TrackNode tn= TrackDB.TrackNodes[i];
                SignalGraphVertex v = VertexMap[i];
                SignalGraphEdge e = v.FindEdge(traveller.TrackNodeIndex);
                float dist = traveller.DistanceTo(tn.UiD.TileX, tn.UiD.TileZ, tn.UiD.X, tn.UiD.Y, tn.UiD.Z);
                //Trace.WriteLine(string.Format("findloc {0} {1} {2} {3} {4} {5}", i, dist, e.GetLength(), v.GetNodeIndex(), e.V1().GetNodeIndex(), e.V2().GetNodeIndex()));
                loc.Set(e, v == e.V1() ? 0 : e.GetLength(), v == e.V1());
                loc.Move(-dist, 0);
            }
            return loc;
        }
        /// <summary>
        /// Updates all the signals to match the current track circuit state.
        /// </summary>
        public void UpdateSignals()
        {
            //foreach (SignalGraphVertex v in Vertices)
            //    if (v.GetEdge(1) == null)
            //        TracePath(v, v.GetEdge(0));
            foreach (SignalGraphVertex v in Vertices)
                v.UpdateSignals();
//            foreach (SignalGraphVertex v in Vertices)
//              if (v.GetEdge(1) == null)
//                TracePath(v, v.GetEdge(0));
        }
        /// <summary>
        /// Updates all signals for junctions whose seleted route has changed.
        /// Updates all signals the first time called.
        /// </summary>
        public void UpdateJunctionSignals()
        {
            if (dirty)
            {
                UpdateSignals();
                dirty = false;
            }
            foreach (SignalGraphVertex v in Vertices)
                v.UpdateJunctionSignals();
        }
        /// <summary>
        /// Debugging function to print path from v heading toward e.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="e"></param>
        public void TracePath(SignalGraphVertex v, SignalGraphEdge e)
        {
            Trace.WriteLine("tracepath");
            SignalGraphEdge pe= null;
            for (; ; )
            {
                Trace.Write(string.Format(" {0} {1}",v.GetNodeIndex(),v.id));
                SignalObject sig= v.GetSignal(pe);
                if (sig != null)
                    Trace.Write(string.Format(" {0} {1} {2}",sig.trackNode,sig.trRefIndex,sig.blockState));
                else
                    Trace.Write(" - - -");
                sig= v.GetSignal(e);
                if (sig != null)
                    Trace.Write(string.Format(" {0} {1} {2}",sig.trackNode,sig.trRefIndex,sig.blockState));
                else
                    Trace.Write(" - - -");
                Trace.WriteLine("");
                if (e == null)
                    break;
                Trace.Write(string.Format(" {0} {1}", e.GetNodeIndex(),e.id));
                TrackCircuit tc= e.GetTrackCircuit();
                if (tc != null)
                    Trace.Write(string.Format(" {0}",tc.IsOccupied()));
                Trace.WriteLine("");
                v= e.V1()==v ? e.V2() : e.V1();
                pe= e;
                e= v.NextEdge(e);
            }
        }
    }
}
