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

namespace ORTS
{
    /// <summary>
    /// Location on SignalGraph
    /// Used to track train movement and update track circuits
    /// </summary>
    public class SignalGraphLocation
    {
        SignalGraphEdge Edge;
        float OffsetM;          // distance from Edge.V1()
        bool Reverse;               // true if movement is toward Edge.V1()
        public SignalGraphLocation()
        {
        }
        public SignalGraphLocation(SignalGraphLocation other)
        {
            Edge = other.Edge;
            OffsetM = other.OffsetM;
            Reverse = other.Reverse;
        }
        /// <summary>
        /// Sets this location to the specified edge, offset and reverse setting.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="o"></param>
        /// <param name="r"></param>
        public void Set(SignalGraphEdge e, float o, bool r)
        {
            Edge = e;
            OffsetM = o;
            Reverse = r;
            //Trace.WriteLine(string.Format("set {0} {1} {2} {3}", e.id, e.GetNodeIndex(), o, r));
        }
        /// <summary>
        /// Adds or removes a train from the track circuit at this location.
        /// </summary>
        /// <param name="dNumTrains">1 for add, -1 for remove</param>
        public void ChangeOccupancy(int dNumTrains)
        {
            if (Edge == null || Edge.GetTrackCircuit() == null)
                return;
            if (dNumTrains < 0)
                Edge.GetTrackCircuit().DecTrains();
            if (dNumTrains > 0)
                Edge.GetTrackCircuit().IncTrains();
        }
        /// <summary>
        /// Moves this location the specified distance and updates track circuit occupancy and signals.
        /// </summary>
        /// <param name="distM"></param>
        /// <param name="dNumTrains">1 for head end, -1 for tail end, 0 for no change</param>
        /// <returns></returns>
        public bool Move(float distM, int dNumTrains)
        {
            bool rev = distM < 0;
            if (rev)
            {
                distM = -distM;
                dNumTrains = -dNumTrains;
            }
            //Trace.WriteLine(string.Format("move {0} {1} {2} {3} {4} {5}", Edge.id,Edge.GetNodeIndex(), OffsetM, Reverse, distM, dNumTrains));
            //Trace.WriteLine(string.Format("edge {0} {1} {2} {3}", Edge.id, Edge.GetNodeIndex(), Edge.V1().id, Edge.V2().id));
            for (; ; )
            {
                float max = Reverse == rev ? Edge.GetLength() - OffsetM : OffsetM;
                if (distM < max)
                {
                    if (Reverse == rev)
                        OffsetM += distM;
                    else
                        OffsetM -= distM;
                    //Trace.WriteLine(string.Format("done {0} {1} {2}", OffsetM, Reverse, rev));
                    return true;
                }
                distM -= max;
                SignalGraphVertex v = Reverse == rev ? Edge.V2() : Edge.V1();
                SignalGraphEdge e = v.NextEdge(Edge);
                //Trace.WriteLine(string.Format("v {0} {1}", v.id, distM));
                if (e == null)
                {
                    if (v == Edge.V1())
                        OffsetM = 0;
                    else
                        OffsetM = Edge.GetLength();
                    //Trace.WriteLine(string.Format("null {0} {1}", OffsetM, distM));
                    return false;
                }
                if (dNumTrains < 0)
                {
                    TrackCircuit tc = Edge.GetTrackCircuit();
                    if (tc != null && tc != e.GetTrackCircuit())
                    {
                        tc.DecTrains();
                        Edge.UpdateSignals();
                    }
                }
                else if (dNumTrains > 0)
                {
                    TrackCircuit tc = e.GetTrackCircuit();
                    if (tc != null && tc != Edge.GetTrackCircuit())
                    {
                        tc.IncTrains();
                        e.UpdateSignals();
                    }
                }
                Edge = e;
                if (v == Edge.V1())
                {
                    OffsetM = 0;
                    Reverse = rev;
                }
                else
                {
                    OffsetM = Edge.GetLength();
                    Reverse = !rev;
                }
                //Trace.WriteLine(string.Format("newedge {0} {1} {2} {3}", Edge.id, Edge.GetNodeIndex(), OffsetM, Reverse));
                //Trace.WriteLine(string.Format("edge {0} {1} {2} {3}", Edge.id, Edge.GetNodeIndex(), Edge.V1().id, Edge.V2().id));
            }
        }
    }
}
