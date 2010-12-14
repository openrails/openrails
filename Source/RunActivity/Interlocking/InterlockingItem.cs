using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines the base class for interlocking objects.
   /// </summary>
   public class InterlockingItem
   {
      /// <summary>
      /// Reference to the simulator object.
      /// </summary>
      protected Simulator simulator;


      public InterlockingItem(Simulator simulator)
      {
         this.simulator = simulator;
      }

   }
}
