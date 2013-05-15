// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

namespace ORTS.Interlocking.MovementAuthority
{
   internal class RouteCollection
   {

      internal Dictionary<InterlockingSignal, List<Route>> AtomicRoutes { get; private set; }


      private InterlockingSystem InterLockingSystem;


      /// <summary>
      /// Creates a new collection of routes.
      /// </summary>
      /// <param name="interlockingSystem">The InterlockingSystem object from which
      /// to create routes.</param>
      internal RouteCollection(InterlockingSystem interlockingSystem)
      {
         InterLockingSystem = interlockingSystem;

         AtomicRoutes = new Dictionary<InterlockingSignal, List<Route>>();
      }


      /// <summary>
      /// Adds a list of routes to the collection.
      /// </summary>
      /// <param name="items">The routes to add to the collection.</param>
      internal void AddRange(List<Route> items)
      {
         foreach (var route in items)
         {
            Add(route);
         }
      }


      /// <summary>
      /// Adds a route to the collection.
      /// </summary>
      /// <param name="route">The route to add to the collection.</param>
      internal void Add(Route route)
      {
         if (!AtomicRoutes.ContainsKey(route.StartSignal))
         {
            AtomicRoutes.Add(route.StartSignal, new List<Route>());
         }

         AtomicRoutes[route.StartSignal].Add(route);
      }


      /// <summary>
      /// Returns all routes beginning with the given signal.
      /// </summary>
      /// <param name="StartSignal"></param>
      /// <returns></returns>
      internal List<Route> FindExistingRoutes(InterlockingSignal StartSignal)
      {
         List<Route> returnValue = new List<Route>();
         
         if (AtomicRoutes.ContainsKey(StartSignal)) 
         {
            returnValue.AddRange(AtomicRoutes[StartSignal]);
         }

         return returnValue;
      }

      /// <summary>
      /// Returns all routes starting with the given signal and ending with the given terminator.
      /// </summary>
      /// <param name="StartSignal"></param>
      /// <param name="Terminator"></param>
      /// <returns></returns>
      internal List<Route> FindExistingRoutes(InterlockingSignal StartSignal, InterlockingTerminator Terminator)
      {
         return FindExistingRoutes(StartSignal).FindAll(route => route.Terminator == Terminator);
      }


   }
}
