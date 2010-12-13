using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
      internal Dictionary<uint, InterlockingTrack> Tracks { get; set; }

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

         Tracks = new Dictionary<uint, InterlockingTrack>();

         var enumer = simulator.TSectionDat.TrackSections.GetEnumerator();
         
         while (enumer.MoveNext())
         {
            Tracks.Add(enumer.Current.Key, new InterlockingTrack(simulator, enumer.Current.Value));
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
