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
    /// Track connecting two SignalGraphVertex locations
    /// </summary>
    public class SignalGraphEdge
    {
        SignalGraphVertex Vertex1 = null;
        SignalGraphVertex Vertex2 = null;
        int NodeIndex = 0;
        float LengthM = 0;                  // distance from Vertex1 to Vertex2 along track
        TrackCircuit TrackCircuit = null;   // train occupancy detector for this track
        static int nEdge = 0;
        public int id;
        public SignalGraphEdge(int nodeIndex, SignalGraphVertex v1, SignalGraphVertex v2)
        {
            id = nEdge++;
            NodeIndex = nodeIndex;
            Vertex1 = v1;
            Vertex2 = v2;
        }
        public int GetNodeIndex()
        {
            return NodeIndex;
        }
        public SignalGraphVertex V1()
        {
            return Vertex1;
        }
        public SignalGraphVertex V2()
        {
            return Vertex2;
        }
        public void SetLength(float l)
        {
            LengthM = l;
        }
        public float GetLength()
        {
            return LengthM;
        }
        public void SetTrackCircuit(TrackCircuit tc)
        {
            TrackCircuit = tc;
        }
        public TrackCircuit GetTrackCircuit()
        {
            return TrackCircuit;
        }
        public void UpdateSignals()
        {
            UpdateSignal(Vertex1);
            UpdateSignal(Vertex2);
        }
        /// <summary>
        /// Finds the closest signal that sees this edge from the v end and updates it.
        /// </summary>
        /// <param name="v"></param>
        private void UpdateSignal(SignalGraphVertex v)
        {
            //Trace.WriteLine(string.Format("eusig {0} {1} {2} {3}", id, v.id, NodeIndex, v.GetNodeIndex()));
            SignalGraphEdge e = this;
            while (!v.UpdateSignal(e))
            {
                e = v.NextEdge(e);
                if (e == null)
                    break;
                v = v == e.V1() ? e.V2() : e.V1();
                //Trace.WriteLine(string.Format(" eusig {0} {1} {2} {3}", e.id, v.id, NodeIndex, v.GetNodeIndex()));
            }
        }
    }
}
