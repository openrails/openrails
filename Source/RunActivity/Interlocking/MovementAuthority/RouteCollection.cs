using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS.Interlocking.MovementAuthority
{
   internal class RouteCollection
   {

      internal Dictionary<InterlockingSignal, List<AtomicRoute>> AtomicRoutes { get; private set; }


      private InterlockingSystem InterLockingSystem;


      /// <summary>
      /// Creates a new collection of routes.
      /// </summary>
      /// <param name="interlockingSystem">The InterlockingSystem object from which
      /// to create routes.</param>
      internal RouteCollection(InterlockingSystem interlockingSystem)
      {
         InterLockingSystem = interlockingSystem;

         AtomicRoutes = new Dictionary<InterlockingSignal, List<AtomicRoute>>();
      }


      /// <summary>
      /// Adds a list of routes to the collection.
      /// </summary>
      /// <param name="items">The routes to add to the collection.</param>
      internal void AddRange(List<AtomicRoute> items)
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
      internal void Add(AtomicRoute route)
      {
         if (!AtomicRoutes.ContainsKey(route.StartSignal))
         {
            AtomicRoutes.Add(route.StartSignal, new List<AtomicRoute>());
         }

         AtomicRoutes[route.StartSignal].Add(route);
      }


      /// <summary>
      /// Returns all routes beginning with the given signal.
      /// </summary>
      /// <param name="StartSignal"></param>
      /// <returns></returns>
      internal List<AtomicRoute> FindExistingRoutes(InterlockingSignal StartSignal)
      {
         List<AtomicRoute> returnValue = new List<AtomicRoute>();
         
         if (AtomicRoutes.ContainsKey(StartSignal)) 
         {
            returnValue.AddRange(AtomicRoutes[StartSignal]);
         }

         return returnValue;
      }

      /// <summary>
      /// Returns all routees starting with the given signal and ending with the given terminator.
      /// </summary>
      /// <param name="StartSignal"></param>
      /// <param name="Terminator"></param>
      /// <returns></returns>
      internal List<AtomicRoute> FindExistingRoutes(InterlockingSignal StartSignal, InterlockingTerminator Terminator)
      {
         return FindExistingRoutes(StartSignal).FindAll(route => route.Terminator == Terminator);
      }


   }
}
