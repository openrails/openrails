/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
///
/// Principal Author:
///     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
/// Contributor:
///    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
/// 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using ORTS;
using Microsoft.Xna.Framework;

namespace ORTS.Interlocking
{

   /// <summary>
   /// Defines possible states of the switch. 
   /// </summary>
   public enum SwitchState
   {
      RightLeading,
      LeftLeading,
      GoingToRightLeading,
      GoingToLeftLeading,
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

         //ComputeGeometry();
      }


      /// <summary>
      /// Defines the maximum speed in m/s through the switch via the left leading position.
      /// </summary>
      public int SpeedLeftLeading { get; private set; }

      /// <summary>
      /// Defines the maximum speed in m/s through the switch via the right leading position.
      /// </summary>
      public int SpeedRightLeading { get; private set; }


      /// <summary>
      /// The base node of the switch.
      /// </summary>
      private TrackNode BaseNode;

      /// <summary>
      /// The left-leading node of the switch.
      /// </summary>
      private TrackNode LeftLeadingNode;

      /// <summary>
      /// The right-leading node of the switch.
      /// </summary>
      private TrackNode RightLeadingNode;

      
      /// <summary>
      /// True when switch is manually locked by the dispatcher.
      /// </summary>
      public bool IsManuallyLocked { get; private set; }

      /// <summary>
      /// True when the switch has been locked as part of a route.
      /// </summary>
      public bool IsRouteLocked { get; private set; }


      /// <summary>
      /// Returns true if any of the connected tracks are occupied.
      /// </summary>
      public bool IsOccupied
      {
         get
         {
            bool returnValue = false;

            if (BaseNode.InterlockingTrack != null && BaseNode.InterlockingTrack.IsOccupied)
            {
               returnValue = true;
            }

            if (LeftLeadingNode.InterlockingTrack != null && LeftLeadingNode.InterlockingTrack.IsOccupied)
            {
               returnValue = true;
            }

            if (RightLeadingNode.InterlockingTrack != null && RightLeadingNode.InterlockingTrack.IsOccupied)
            {
               returnValue = true;
            }


            return returnValue;
         }
      }


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


      /// <summary>
      /// Initialises some of the switch properties based on the underlying network topology.
      /// </summary>
      private void ComputeGeometry()
      {
         var tracknodes = simulator.TDB.TrackDB.TrackNodes;

         TrackNode[] connections= new TrackNode[3];
         
         TrackNode thisNode = tracknodes[TrJunctionNodeIndex];

         for (int i = 0; i < thisNode.Inpins + thisNode.Outpins; i++)
         {
            connections[i] = tracknodes[thisNode.TrPins[i].Link];

            if (connections[i] == null)
            {
               // in rare cases, we cannot find a connection (malformed network?)
               // in such cases we cannot compute the switch geometry (for the moment, anyways)
               return;
            }
         }
          
  
         

         
         // we now have the 3 connected nodes that are connected to this switch.

         // now, we need to try to compute the angles in this switch
         Dictionary<TrVectorNode[],float> angles = DetermineAngles(connections);


         bool isWye = true;   // is false for *most* switches - some switches, however, diverge on both branches
                              // is false when or more of the angles in the switch is ~180.

         foreach (var a in angles.Values)
         {
            if (a.AlmostEqual(180f, 0.1f))
            {
               isWye = false; // one of the angles is 180 - switch is not a wye.
               break;               
            }
         }


         if (!isWye)
         {
            // normal case - only one of the branches is diverging

            TrVectorNode baseNode = null;
            TrVectorNode leftLeadingNode = null;
            TrVectorNode rightLeadingNode = null;

            ComputeBranches(angles, ref baseNode, ref leftLeadingNode, ref rightLeadingNode);

            foreach (var c in connections)
            {
               if (c.TrVectorNode == baseNode)
               {
                  BaseNode = c;
               }

               if (c.TrVectorNode == leftLeadingNode)
               {
                  LeftLeadingNode = c;
               }

               if (c.TrVectorNode == rightLeadingNode)
               {
                  RightLeadingNode = c;
               }
            }

            if (BaseNode == null)
            {
               // TODO: error
            }

            if (LeftLeadingNode == null)
            {
               // TODO: error
            }

            if (RightLeadingNode == null)
            {
               // TODO: error
            }

         }
         else
         {
            // two of the branches are diverging
         }


       

      }


      /// <summary>
      /// Computes the branches/nodes of the switch.
      /// </summary>
      /// <param name="angles"></param>
      /// <returns></returns>
      private void ComputeBranches(Dictionary<TrVectorNode[], float> angles, ref TrVectorNode baseNode, ref TrVectorNode leftLeadingNode, ref TrVectorNode rightLeadingNode)
      {
        
         List<TrVectorNode> nodes = new List<TrVectorNode>();

         var tempDict = new Dictionary<TrVectorNode[], float>(angles); 

         foreach (var nodePair in angles)
         {
            if (nodes.Contains(nodePair.Key[0]) == false)
            {
               nodes.Add(nodePair.Key[0]);
            }

            if (nodes.Contains(nodePair.Key[1]) == false)
            {
               nodes.Add(nodePair.Key[1]);
            }
         }


         // nodes now contains all three TrVectorNodes
         // get the smallest angle

         float smallest = float.MaxValue;

         foreach (var nodePair in angles)
         {
            if (nodePair.Value < smallest)
            {
               smallest = nodePair.Value;
            }
         }

         #region Compute Base Node
         // now that we know the smallest angle, remove that entry, as the other two entries both contain the base

         var enumer = tempDict.GetEnumerator();
         while (enumer.MoveNext())
         {
            if (enumer.Current.Value == smallest)
            {
               tempDict.Remove(enumer.Current.Key);
               break;
            }
         }

         // tempDict should now have two entries - the base is the common TrVectorNode between the two

         var first = tempDict.First();
         var last = tempDict.Last();

         if (first.Key[0] == last.Key[0])
         {
            baseNode = first.Key[0];
         }
         else if (first.Key[1] == last.Key[1])
         {
            baseNode = first.Key[1];
         }
         else if (first.Key[1] == last.Key[0])
         {
            baseNode = first.Key[1];
         }
         else if (first.Key[0] == last.Key[1])
         {
            baseNode = first.Key[0];
         }

         #endregion

         #region Compute Right-Leading Node

         #endregion

      }

      /// <summary>
      /// Determines the angles of the switch. Useful for computing properties
      /// related to the geometry of a switch.
      /// </summary>
      /// <param name="connections"></param>
      /// <returns></returns>
      private Dictionary<TrVectorNode[], float> DetermineAngles(TrackNode[] connections)
      {

         Dictionary<TrVectorNode[], float> returnValue = new Dictionary<TrVectorNode[], float>();


         Vector3[] junctionVectors = new Vector3[3];

         for (int i = 0; i < 3; i++)
         {

            if (connections[i].TrEndNode == false &&
                connections[i].TrJunctionNode == null)
            {

               TDBTraveller tempTrav = new TDBTraveller(connections[i], connections[i].TrVectorNode.TrVectorSections[0], simulator.TDB, simulator.TSectionDat);


               Vector3 initialLocation = new Vector3(/*tempTrav.TileX * 2048 +*/ tempTrav.X, 0, /*tempTrav.TileZ * 2048 +*/ tempTrav.Z);

               // move a small distance from the junction
               tempTrav.Move(1);

               Vector3 finalLocation = new Vector3(/*tempTrav.TileX * 2048 +*/ tempTrav.X, 0, /*tempTrav.TileZ * 2048 +*/ tempTrav.Z);

               junctionVectors[i].X = finalLocation.X - initialLocation.X;
               junctionVectors[i].Z = finalLocation.Z - initialLocation.Z;

               junctionVectors[i].Normalize();
            }
         }

         returnValue.Add(new TrVectorNode[] { connections[0].TrVectorNode, connections[1].TrVectorNode }, MathHelper.ToDegrees((float)Math.Acos(MathHelper.Clamp(Vector3.Dot(junctionVectors[0], junctionVectors[1]), -1, 1))));
         returnValue.Add(new TrVectorNode[] { connections[1].TrVectorNode, connections[2].TrVectorNode }, MathHelper.ToDegrees((float)Math.Acos(MathHelper.Clamp(Vector3.Dot(junctionVectors[1], junctionVectors[2]), -1, 1))));
         returnValue.Add(new TrVectorNode[] { connections[0].TrVectorNode, connections[2].TrVectorNode }, MathHelper.ToDegrees((float)Math.Acos(MathHelper.Clamp(Vector3.Dot(junctionVectors[0], junctionVectors[2]), -1, 1))));

         return returnValue;
      }

      
   }
}
