using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Provides an abstraction atop an underlying switch object.
   /// </summary>
   public class InterlockingSwitch : InterlockingItem
   {


      public TrJunctionNode Switch { get; private set; }

      public InterlockingSwitch(Simulator simulator, TrJunctionNode switchObject)
         : base(simulator)
      {
         Switch = switchObject;
      }
   }
}
