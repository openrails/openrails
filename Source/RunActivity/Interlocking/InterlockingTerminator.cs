using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Defines an object that can be the start or end of route.
   /// </summary>
   public class InterlockingTerminator : InterlockingItem
   {
      /// <summary>
      /// Creates a new InterlockingTerminator.
      /// </summary>
      /// <param name="simulator"></param>
      public InterlockingTerminator(Simulator simulator) : base(simulator) { }
   }

}
