using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking.MovementAuthority
{
   /// <summary>
   /// Defines a single path between either  a signal
   /// and a termination object (another signal or a buffer). 
   /// </summary>
   public class Route
   {

      /// <summary>
      /// The signal defining the beginning of the route.
      /// </summary>
      public InterlockingSignal StartSignal { get; private set; }


      public InterlockingTerminator Terminator { get; private set; }
      

   }
}
