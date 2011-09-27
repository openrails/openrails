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
    /// Location of significance to the signal system
    /// Could be: end of track, joint between track circuits, signal location or switch (junction node)
    /// </summary>
    public class SignalGraphVertex
    {
        SignalGraphEdge[] Edges = null;
        SignalObject[] Signals = null;
        int NodeIndex = -1;
        TrJunctionNode JunctionNode = null;
        int SelectedRoute = -1; // junction selected route on last update
        static int nVert = 0;
        public int id;
        /// <summary>
        /// Creates a vertex.
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <param name="jNode">Junction node for a switch</param>
        public SignalGraphVertex(int nodeIndex, TrJunctionNode jNode)
        {
            id = nVert++;
            NodeIndex = nodeIndex;
            JunctionNode = jNode;
            Edges = new SignalGraphEdge[jNode != null ? 3 : 2];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetNodeIndex()
        {
            return NodeIndex;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TrJunctionNode GetJunctionNode()
        {
            return JunctionNode;
        }
        /// <summary>
        /// Saves edge e at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="e"></param>
        public void SetEdge(int index, SignalGraphEdge e)
        {
            Edges[index] = e;
        }
        public SignalGraphEdge GetEdge(int index)
        {
            return Edges[index];
        }
        /// <summary>
        /// Returns the next edge opposite edge e or null if the switch is not aligned properly.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SignalGraphEdge NextEdge(SignalGraphEdge e)
        {
            int i = 1;
            if (JunctionNode != null)
                i= JunctionNode.SelectedRoute + 1;
            if (Edges[0] == e)
                return Edges[i];
            if (Edges[i] == e)
                return Edges[0];
            return null;
        }
        /// <summary>
        /// Saves signal sig at the specified index.
        /// The signal at index 0 is seen by a train on Edges[0].
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sig"></param>
        public void SetSignal(int index, SignalObject sig)
        {
            if (Signals == null)
                Signals = new SignalObject[2];
            if (Signals[index] == null || sig.SignalHeads.Count > 0)
                Signals[index] = sig;
        }
        /// <summary>
        /// Returns the signal seen from edge e.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SignalObject GetSignal(SignalGraphEdge e)
        {
            if (Signals == null)
                return null;
            for (int i = 0; i < Signals.Length; i++)
                if (e == Edges[i])
                    return Signals[i];
            return null;
        }
        /// <summary>
        /// Returns the edge with the specified node index.
        /// </summary>
        /// <param name="nodeIndex"></param>
        /// <returns></returns>
        public SignalGraphEdge FindEdge(int nodeIndex)
        {
            for (int i = 0; i < Edges.Length; i++)
                if (Edges[i].GetNodeIndex() == nodeIndex)
                    return Edges[i];
            return null;
        }
        /// <summary>
        /// Updates the signal at this vertex that protects edge e.
        /// Returns false if there is no such signal and otherwise true.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool UpdateSignal(SignalGraphEdge e)
        {
            if (Signals == null)
                return false;
            SignalObject sig = Signals[Edges[0] == e ? 1 : 0];
            if (sig == null || !sig.isSignalNormal())
                return false;
            sig.blockState = SignalObject.BLOCKSTATE.CLEAR;
            SignalGraphVertex v = this;
            //Trace.WriteLine(string.Format("vusig {0} {1} {2} {3}", e.id, v.id, e.GetNodeIndex(), v.GetNodeIndex()));
            for (; ; )
            {
                TrackCircuit tc = e.GetTrackCircuit();
                if (tc != null && tc.IsOccupied())
                {
                    sig.blockState = SignalObject.BLOCKSTATE.OCCUPIED;
                    break;
                }
                v = v == e.V1() ? e.V2() : e.V1();
                if (v.Signals != null && v.Signals[v.Edges[0] == e ? 0 : 1] != null)
                    break;
                e = v.NextEdge(e);
                if (e == null)
                {
                    if (v.Edges.Length > 2)
                        sig.blockState = SignalObject.BLOCKSTATE.JN_OBSTRUCTED;
                    break;
                }
                //Trace.WriteLine(string.Format(" vusig {0} {1} {2} {3}", e.id, v.id, e.GetNodeIndex(), v.GetNodeIndex()));
            }
            sig.TrackStateChanged();
            //Trace.WriteLine(string.Format("updatesig {0} {1} {2}", id, sig.trItem, sig.blockState));
            return true;
        }
        /// <summary>
        /// Updates all signals at this vertex.
        /// </summary>
        public void UpdateSignals()
        {
            for (int i = 0; i < Edges.Length; i++)
                UpdateSignal(Edges[i]);
        }
        /// <summary>
        /// Updates the signals prtecting a junction node if the selected route has changed.
        /// </summary>
        public void UpdateJunctionSignals()
        {
            if (JunctionNode != null && SelectedRoute != JunctionNode.SelectedRoute)
            {
                SelectedRoute = JunctionNode.SelectedRoute;
                for (int i = 1; i < Edges.Length; i++)
                    Edges[i].UpdateSignals();
            }
        }
    }
}
