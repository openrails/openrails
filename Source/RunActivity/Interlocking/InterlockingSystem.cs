// COPYRIGHT 2010 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using ORTS.Interlocking.MovementAuthority;

namespace ORTS.Interlocking
{
   /// <summary>
   /// Encapsulates the interlocking and signalling system.
   /// </summary>
   public partial class InterlockingSystem
   {
      /// <summary>
      /// Reference to the simulator object.
      /// </summary>
      private Simulator simulator;

      /// <summary>
      /// Links the underlying TrackNode to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<TrackNode, InterlockingTrack> Tracks { get; private set; }

      /// <summary>
      /// Links the underlying TrVectorSection to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<SignalObject, InterlockingSignal> Signals { get; private set; }

      /// <summary>
      /// Links the underlying Switch to its corresponding interlocking layer counterpart.
      /// </summary>
      internal Dictionary<TrJunctionNode, InterlockingSwitch> Switches { get; private set; }

      /// <summary>
      /// Links underlying TrackNode objects representing buffer ends
      /// </summary>
      internal Dictionary<TrackNode, InterlockingBuffer> Buffers { get; private set; }

      /// <summary>
      /// Contains all routes within the interlocking system.
      /// </summary>
      internal RouteCollection Routes { get; private set; }

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
         CreateBuffers();

         CreateRoutes();
      }

      private void CreateRoutes()
      {
         Routes = new RouteCollection(this);

         foreach (var s in Signals.Values)
         {
            Routes.AddRange(DiscoverRoutesFromSignal(s));
         }
      }



      private void CreateBuffers()
      {
         Buffers = new Dictionary<TrackNode, InterlockingBuffer>();

         foreach (var n in simulator.TDB.TrackDB.TrackNodes)
         {
            if (n != null && n.TrEndNode)
            {
               Buffers.Add(n, new InterlockingBuffer(simulator, n));
            }
         }
      }


      /// <summary>
      /// Instantiates InterlockingSignal objects from the simulation's SignalObject objects.
      /// </summary>
      private void CreateSwitches()
      {
         Switches = new Dictionary<TrJunctionNode, InterlockingSwitch>();

         for (int i = 0; i < simulator.TDB.TrackDB.TrackNodes.Length; i++)
         {
            var n = simulator.TDB.TrackDB.TrackNodes[i];

            if (n != null && n.TrJunctionNode != null)
            {
               Switches.Add(n.TrJunctionNode, new InterlockingSwitch(simulator, n.TrJunctionNode, i));
            }
         }
      }

      /// <summary>
      /// Instantiates InterlockingSignal objects from the simulation's SignalObject objects.
      /// </summary>
      private void CreateSignals()
      {
         Signals = new Dictionary<SignalObject, InterlockingSignal>();


         // verify that we have signals (it's possible to have none!)
         if (simulator.Signals.SignalObjects != null)
         {  
            foreach (var sigObj in simulator.Signals.SignalObjects)
            {
               if (sigObj == null)
                  continue;

               Signals.Add(sigObj, new InterlockingSignal(simulator, sigObj));
            }
         }
      }

      /// <summary>
      /// Instantiates InterlockingTrack objects from the simulation's TrVectorSection.
      /// </summary>
      private void CreateTracks()
      {

         Tracks = new Dictionary<TrackNode, InterlockingTrack>();

         foreach (var n in simulator.TDB.TrackDB.TrackNodes)
         {
            if (n != null && 
                n.TrEndNode == false &&   // not a buffer
                n.TrJunctionNode == null) // not a switch
            {
               Tracks.Add(n, new InterlockingTrack(simulator, n));
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
