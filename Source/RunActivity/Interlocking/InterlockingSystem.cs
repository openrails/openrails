using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Encapsulates the interlocking and signalling system.
   /// </summary>
   public class InterlockingSystem
   {
      /// <summary>
      /// Reference to the simulator object.
      /// </summary>
      private Simulator simulator;

      /// <summary>
      /// 
      /// </summary>
      internal Dictionary<TrVectorSection, InterlockingTrack> Tracks { get; set; }

      /// <summary>
      /// Creates a new InterlockingSystem object.
      /// </summary>
      /// <param name="simulator">The simulator from which to create an InterlockingSystem.</param>
      public InterlockingSystem(Simulator simulator)
      {
         this.simulator = simulator;

         CreateTracks();
      }

      /// <summary>
      /// Instantiates InterlockingTrack objects from the simulation's TrackShapes.
      /// </summary>
      private void CreateTracks()
      {

         Tracks = new Dictionary<TrVectorSection, InterlockingTrack>();

         var nodes = simulator.TDB.TrackDB.TrackNodes;
         foreach (var n in nodes)
         {
            if (n != null && n.TrVectorNode != null)
            {
               foreach (var vNode in n.TrVectorNode.TrVectorSections)
               {
                  Tracks.Add(vNode, new InterlockingTrack(simulator, vNode));
               }
            }
         }
      }




      internal void Update(float elapsed)
      {
         
         // let the tracks know we're about to change their properties
         var enumer = Tracks.GetEnumerator();
         
         while (enumer.MoveNext())
         {
            enumer.Current.Value.BeginUpdate();
         }



         // update each train's track occupation
         foreach (Train train in simulator.Trains)
         {
            train.UpdateTrackOccupation();
         }



         // tell the tracks we're done updating
         enumer = Tracks.GetEnumerator();

         while (enumer.MoveNext())
         {
            enumer.Current.Value.EndUpdate();
         }

      }
   }
}
