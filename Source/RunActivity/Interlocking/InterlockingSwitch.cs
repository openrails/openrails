using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines possible states of the switch. For now, using underlying
   /// value to indicate position, instead of normal/reverse.
   /// </summary>
   public enum SwitchState
   {
      Position_0,
      Position_1,
      GoingTo_0,
      GoingTo_1,
      Trailed
   }


   /// <summary>
   /// Provides an abstraction atop an underlying switch object.
   /// </summary>
   public class InterlockingSwitch : InterlockingItem
   {


      public TrJunctionNode Switch { get; private set; }

      /// <summary>
      /// Defines the index number of the underlying TrJunctionNode.
      /// </summary>
      private int TrJunctionNodeIndex;

      public InterlockingSwitch(Simulator simulator, TrJunctionNode switchObject, int trJunctionNodeIndex)
         : base(simulator)
      {
         Switch = switchObject;
         TrJunctionNodeIndex = trJunctionNodeIndex;

         ComputeGeometry();
      }

      
      /// <summary>
      /// True when switch is manually locked by the dispatcher.
      /// </summary>
      public bool IsManuallyLocked { get; private set; }

      /// <summary>
      /// True when the switch has been locked as part of a route.
      /// </summary>
      public bool IsRouteLocked { get; private set; }


      /// <summary>
      /// True if locked for any reason.
      /// </summary>
      public bool IsLocked
      {
         get
         {
            bool returnValue = false;

            if (IsManuallyLocked)
            {
               returnValue = true;
            }

            if (IsRouteLocked)
            {
               returnValue = true;
            }


            return returnValue;
         }
      }


      /// <summary>
      /// Returns true when this switch can be thrown (commanded to change position)
      /// </summary>
      public bool CanThrow
      {
         get
         {
            bool returnValue = true;

            if (IsLocked)
            {
               returnValue = false;
            }

            return returnValue;
         }
      }


      /// <summary>
      /// Throws switch if possible.
      /// </summary>
      public void Throw()
      {
         if (CanThrow)
         {
            // TODO: switch throws instantaneously. there should be 
            // some intermediate "out of correspondence" state that matches
            // any switch animation duration

            if (Switch.SelectedRoute == 0)
            {
               Switch.SelectedRoute = 1;
            }
            else
            {
               Switch.SelectedRoute = 0;
            }
         }
      }



      private void ComputeGeometry()
      {
         var tracknodes = simulator.TDB.TrackDB.TrackNodes;



         TrackNode[] connections= new TrackNode[3];


         
         TrackNode thisNode = tracknodes[TrJunctionNodeIndex];

         for (int i = 0; i < thisNode.Inpins + thisNode.Outpins; i++)
         {
            connections[i] = tracknodes[thisNode.TrPins[i].Link];
         }
            
         
         // we now have the 3 connected nodes that are connected to this switch.

         // now, we need to try to compute the angles in this switch



         point[] junctionVectors = new point[3];

         for (int i = 0; i < 3; i++)
         {

            if (connections[i].TrEndNode)
            {
               // next thing is a buffer
            }
            else if (connections[i].TrJunctionNode != null)
            {
               // next thing is another switch
            }
            else
            {
               // just a track section
            
               TDBTraveller tempTrav = new TDBTraveller(connections[i],connections[i].TrVectorNode.TrVectorSections[0], simulator.TDB, simulator.TSectionDat);


               point initialLocation = new point(tempTrav.TileX * 2048 + tempTrav.X, 0, tempTrav.TileZ * 2048 + tempTrav.Z);

               // move a small distance from the junction
               tempTrav.Move(1);

               point finalLocation = new point(tempTrav.TileX * 2048 + tempTrav.X, 0, tempTrav.TileZ * 2048 + tempTrav.Z);


               // TODO: this does not work correctly yet
               junctionVectors[i].X = finalLocation.X - initialLocation.X;
               junctionVectors[i].Z = finalLocation.Z - initialLocation.Z;

            }
         }

      }
   }
}
