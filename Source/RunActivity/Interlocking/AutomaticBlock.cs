/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
///
/// Principal Author:
///    Charlie Salts / Signalsoft Rail Consultancy Ltd.
/// Contributor:
///    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
/// 
#region description
/// notes about AutomaticBlock in ORTS:
/// 
/// this is a system specifically written to fit ORTS purposes. This is a GENERIC system.
/// in no way this Automatic Block can be compared with the real Automatic Block defined in a real world system.
/// An Automatic Block for THIS particular interlocking system is defined as:
/// !! a series of tracks after each other without switches, with AT LEAST 2 signals in the same direction !!
/// this enables the interlocking system to manage directionality
/// The tracks will be linked together in this automatic block

#endregion


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
