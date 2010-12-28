using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MSTS;


namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines an abstraction of a track circuit used within the interlocking system.
   /// </summary>
   [DebuggerDisplay("{Section} Occupied: {IsOccupied}")]
   public class InterlockingTrack : InterlockingItem
   {
    
  

      /// <summary>
      /// Gets the underlying physical object.
      /// </summary>
      public TrackNode Node { get; private set; }

      /// <summary>
      /// Creates a new InterlockingTrack object.
      /// </summary>
      /// <param name="simulator">The Simulator object.</param>
      /// <param name="node">The underlying object from which to create an InterlockingTrack.</param>
      public InterlockingTrack(Simulator simulator, TrackNode node)
         : base(simulator)
      {
         Node = node;
      
         Node.InterlockingTrack = this;
      }


      public override string ToString()
      {
         return string.Format("{0} Occupied: {1}", Node, IsOccupied);
      }



      private bool isOccupied;

      /// <summary>
      /// True when the track is occupied, false otherwise.
      /// </summary>
      public bool IsOccupied 
      {
         get
         {
            return isOccupied;
         }
         private set
         {
            if (isOccupied != value)
            {
               isOccupied = value;
            }
         }
      }


      /// <summary>
      /// Used during the update process.
      /// </summary>
      private bool tempIsOccupied;

      /// <summary>
      /// Notify this track that it is occupied by a train.
      /// </summary>
      /// <returns></returns>
      public void Occupy()
      {
         tempIsOccupied = true;
      }

      /// <summary>
      /// Prepares the track for possible changes.
      /// </summary>
      public void BeginUpdate()
      {
         tempIsOccupied = false;
      }

      /// <summary>
      /// Informs the track that updating has completed.
      /// </summary>
      public void EndUpdate()
      {
         IsOccupied = tempIsOccupied;
      }

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
