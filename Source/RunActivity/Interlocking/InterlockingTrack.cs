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
      /// Reference to the simulation object.
      /// </summary>
      private Simulator simulator;


      /// <summary>
      /// TODO: create instances of tracks from existing track objects.
      /// </summary>
      public InterlockingTrack(Simulator simulator/*TrackItem foo*/)
      {

      }



      /// <summary>
      /// True when the track is occupied, false otherwise.
      /// </summary>
      public bool Occupied { get; set; }


      /// <summary>
      /// Track reference used to detect opposing routes. 
      /// </summary>
      public InterlockingTrack CascadeToRight { get; set; }

      /// <summary>
      /// Track reference used to detect opposing routes. 
      /// </summary>
      public InterlockingTrack CascadeToLeft { get; set; }

      /// <summary>
      /// Track reference used to detect opposing routes. 
      /// </summary>
      public InterlockingTrack CascadeFromRight { get; set; }

      /// <summary>
      /// Track reference used to detect opposing routes. 
      /// </summary>
      public InterlockingTrack CascadeFromLeft { get; set; }


      /// <summary>
      /// Returns true when this track has any cascade references.
      /// </summary>
      public bool HasCascadeReference
      {
         get
         {
            bool returnValue = false;

            if (CascadeToRight != null)
            {
               returnValue = true;
            }

            if (CascadeFromRight != null)
            {
               returnValue = true;
            }

            if (CascadeToLeft != null)
            {
               returnValue = true;
            }

            if (CascadeFromLeft != null)
            {
               returnValue = true;
            }

            return returnValue;
         }
      }

   }
}
