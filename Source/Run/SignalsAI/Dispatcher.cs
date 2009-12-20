/* Dispatcher
 * 
 * Contains code for AI train dispatcher.
 * This dispatcher reserves track nodes along an AI train's path up to the end of a passing point.
 * If all nodes can be reserved, the AI train is granted permission to move.
 * At the moment passing sections must be defined in the path.
 * In the future some code should be added to compare paths to find possible passing points.
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    public class Dispatcher
    {
        public AI AI;
        private int[] reservations;

        /// <summary>
        /// Initializes the dispatcher.
        /// Creates an array for saving track node reservations and initializes it to no reservations.
        /// </summary>
        public Dispatcher(AI ai)
        {
            AI = ai;
            reservations = new int[ai.Simulator.TDB.TrackDB.TrackNodes.Length];
            for (int i = 0; i < reservations.Length; i++)
                reservations[i] = -1;
        }

        /// <summary>
        /// Updates dispatcher information.
        /// Moves each train's rear path node forward and updates reservations.
        /// </summary>
        public void Update(double clockTime)
        {
            foreach (AITrain train in AI.AITrains)
            {
                if (train.RearNode.NextMainTVNIndex == train.RearTDBTraveller.TrackNodeIndex ||
                  train.RearNode.NextSidingTVNIndex == train.RearTDBTraveller.TrackNodeIndex ||
                  train.RearTDBTraveller.TN.TrVectorNode == null)
                    continue;
                int i = train.RearNode.NextMainTVNIndex;
                //Console.WriteLine("dispatcher update {0} {1}", i, train.UiD);
                if (i >= 0 && reservations[i] == train.UiD)
                    reservations[i] = -1;
                else
                {
                    i = train.RearNode.NextSidingTVNIndex;
                    //Console.WriteLine(" siding {0} {1}", i, train.UiD);
                    if (i >= 0 && reservations[i] == train.UiD)
                        reservations[i] = -1;
                }
                //for (int j = 0; j < reservations.Length; j++)
                //    if (reservations[j] == train.UiD)
                //        Console.WriteLine(" res {0}", j);
                train.RearNode = train.Path.FindTrackNode(train.RearNode, train.RearTDBTraveller.TrackNodeIndex);
            }
        }

        /// <summary>
        /// Requests movement authorization for the specified train.
        /// Follows the train's path from the current rear node until the path ends
        /// or a SidingEnd node is found.  Grants authorization if all of the track
        /// vector nodes can be reserved for the train.
        /// If a SidingStart node is found, the main track and siding are tested separately.
        /// Returns true if an authorization was granted, else false.
        /// The authorization is specified using the SetAuthorization method.
        /// </summary>
        public bool RequestAuth(AITrain train)
        {
            List<int> tnList = new List<int>();
            AIPathNode node = train.RearNode;
            while (node != null && (node == train.RearNode || node.Type != AIPathNodeType.SidingStart))
            {
                if (node.Type == AIPathNodeType.SidingEnd && node != train.AuthEndNode)
                    break;
                if (node.NextMainNode != null && node != train.AuthSidingNode)
                {
                    tnList.Add(node.NextMainTVNIndex);
                    node = node.NextMainNode;
                }
                else if (node.NextSidingNode != null)
                {
                    tnList.Add(node.NextSidingTVNIndex);
                    node = node.NextSidingNode;
                }
                else
                    break;
            }
            if (node == null || !CanReserve(train, tnList))
                return false;
            if (node.Type != AIPathNodeType.SidingStart)
            {
                Unreserve(train);
                Reserve(train, tnList);
                return train.SetAuthorization(node, null);
            }
            //Console.WriteLine("start siding {0}", node.ID);
            List<int> tnList1 = new List<int>();
            AIPathNode sidingNode = node;
            for (; node.Type != AIPathNodeType.SidingEnd; node = node.NextMainNode)
                tnList1.Add(node.NextMainTVNIndex);
            if (CanReserve(train, tnList1))
            {
                Unreserve(train);
                Reserve(train, tnList);
                Reserve(train, tnList1);
                //Console.WriteLine("got main {0}", node.ID);
                return train.SetAuthorization(node, null);
            }
            //Console.WriteLine("try siding {0}", node.ID);
            tnList1.Clear();
            for (node = sidingNode; node.Type != AIPathNodeType.SidingEnd; node = node.NextSidingNode)
                tnList1.Add(node.NextSidingTVNIndex);
            if (CanReserve(train, tnList1))
            {
                Unreserve(train);
                Reserve(train, tnList);
                Reserve(train, tnList1);
                //Console.WriteLine("got siding {0} {1}", node.ID, sidingNode.ID);
                return train.SetAuthorization(node, sidingNode);
            }
            return false;
        }

        /// <summary>
        /// Checks to see is the listed track nodes can be reserved for the specified train.
        /// return true if none of the nodes are already reserved for another train.
        /// </summary>
        private bool CanReserve(AITrain train, List<int> tnList)
        {
            //foreach (int i in tnList)
            //    Console.WriteLine("res {0} {1} {2}", i, reservations[i], train.UiD);
            foreach (int i in tnList)
                if (reservations[i] >= 0 && reservations[i] != train.UiD)
                    return false;
            //Console.WriteLine("can reserve");
            return true;
        }

        /// <summary>
        /// Reserves the listed track nodes for the specified train.
        /// </summary>
        private void Reserve(AITrain train, List<int> tnList)
        {
            foreach (int i in tnList)
                reservations[i] = train.UiD;
        }

        /// <summary>
        /// Clears any existing reservations for the specified train.
        /// </summary>
        private void Unreserve(AITrain train)
        {
            for (int i = 0; i < reservations.Length; i++)
                if (reservations[i] == train.UiD)
                    reservations[i] = -1;
        }

        /// <summary>
        /// Releases the specified train's movement authorization.
        /// </summary>
        public void Release(AITrain train)
        {
            train.SetAuthorization(null, null);
            Unreserve(train);
        }
    }
}
