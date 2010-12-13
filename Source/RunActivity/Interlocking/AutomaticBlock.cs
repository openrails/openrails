using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines an automatic block for interlocking purposes.
   /// </summary>
   public class AutomaticBlock
   {


      private List<InterlockingTrack> tracks;

      /// <summary>
      /// Contains references to tracks within the AutomaticBlock.
      /// </summary>
      public List<InterlockingTrack> Tracks
      {
         get
         {
            return tracks;
         }
      }


      /// <summary>
      /// Creates a new automatic block from a series of tracks.
      /// </summary>
      /// <param name="constituentTracks"></param>
      public AutomaticBlock(List<InterlockingTrack> constituentTracks)
      {
         tracks = new List<InterlockingTrack>(constituentTracks);
      }



      /// <summary>
      /// Returns true if ANY of the tracks in the AutomaticBlock are occupied.
      /// </summary>
      public bool AnyTracksOccupied
      {
         get
         {
            bool returnValue = false;

            foreach (var track in Tracks)
            {
               if (track.IsOccupied)
               {
                  returnValue = true;
                  break;
               }
            }

            return returnValue;
         }
      }
   }
}
