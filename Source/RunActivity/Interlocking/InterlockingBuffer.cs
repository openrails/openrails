using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS.Interlocking
{
   public class InterlockingBuffer: InterlockingItem
   {


      public TrackNode TrackNode { get; private set; }
      
      /// <summary>
      /// Creates a new InterlockingBuffer.
      /// </summary>
      /// <param name="simulator"></param>
      /// <param name="trackNode"></param>
      public InterlockingBuffer(Simulator simulator, TrackNode trackNode) : base(simulator) 
      {
         TrackNode = trackNode;
      }
   }
}
