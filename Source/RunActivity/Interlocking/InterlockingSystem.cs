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
      /// Links the underlying TrVectorSection to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<TrVectorSection, InterlockingTrack> Tracks { get; set; }

      /// <summary>
      /// Links the underlying TrVectorSection to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<SignalObject, InterlockingSignal> Signals { get; set; }

      /// <summary>
      /// Links the underlying Switch to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<TrJunctionNode, InterlockingSwitch> Switches { get; set; }


      /// <summary>
      /// Creates a new InterlockingSystem object.
      /// </summary>
      /// <param name="simulator">The simulator from which to create an InterlockingSystem.</param>
      public InterlockingSystem(Simulator simulator)
      {
         this.simulator = simulator;

         CreateTracks();
         CreateSwitches();
         CreateSignals();
      }

      /// <summary>
      /// Instantiates InterlockingSignal objects from the simulation's SignalObject objects.
      /// </summary>
      private void CreateSwitches()
      {
         Switches = new Dictionary<TrJunctionNode, InterlockingSwitch>();
         
         foreach (var n in simulator.TDB.TrackDB.TrackNodes)
         {
            if (n != null && n.TrJunctionNode != null)
            {
               Switches.Add(n.TrJunctionNode, new InterlockingSwitch(simulator, n.TrJunctionNode));
            }
         }
      }

      /// <summary>
      /// Instantiates InterlockingSignal objects from the simulation's SignalObject objects.
      /// </summary>
      private void CreateSignals()
      {
         Signals = new Dictionary<SignalObject, InterlockingSignal>();
         
         foreach (var sigObj in simulator.Signals.SignalObjects)
         {
            Signals.Add(sigObj, new InterlockingSignal(simulator, sigObj));
         }
      }

      /// <summary>
      /// Instantiates InterlockingTrack objects from the simulation's TrVectorSection.
      /// </summary>
      private void CreateTracks()
      {

         Tracks = new Dictionary<TrVectorSection, InterlockingTrack>();

         foreach (var n in simulator.TDB.TrackDB.TrackNodes)
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
