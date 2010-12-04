using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines an abstraction of a track circuit used within the interlocking system.
   /// </summary>
   public class InterlockingTrack
   {
      
      /// <summary>
      /// True when the track is occupied, false otherwise.
      /// </summary>
      public bool Occupied { get; set; }


      /// <summary>
      /// Reference to the simulation object.
      /// </summary>
      private Simulator simulator;


      /// <summary>
      /// TODO: create instances of tracks from existing track objects.
      /// </summary>
      public InterlockingTrack(Simulator simulator/*TrackItem foo*/)
      {

      }

   }
}
