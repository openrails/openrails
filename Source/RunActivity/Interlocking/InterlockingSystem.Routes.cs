using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS.Interlocking.MovementAuthority;
using MSTS;

namespace ORTS.Interlocking
{
   public partial class InterlockingSystem
   {



      private TrackNode[] TrackNodes
      {
         get
         {
            return simulator.TDB.TrackDB.TrackNodes;
         }
      }

      private List<Route> DiscoverRoutesFromSignal(InterlockingSignal s)
      {
         List<Route> returnValue = new List<Route>();


         SignalObject startSignal = s.SignalObject;


          //startSignal.trackNode



         return returnValue;
      }

      
   }
}
