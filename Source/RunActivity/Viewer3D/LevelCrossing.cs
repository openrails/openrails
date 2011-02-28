/// LevelCrossings/LevelCrossingObject
/// 
/// The LevelCrossings/LevelCrossingObject classes are responsible for holding and updating level crossing items.
/// LevelCrossings searches and stores a list of LevelCrossingObjects. One or Several LevelCrossingObjects work
/// together under one LevelCrossingObj. One LevelCrossingObj has one shape, which will be initializaed in WFile,
/// and unloaded when the WordFile is moved out of range.

/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;
using MSTS;
using ORTS.Popups;

namespace ORTS
{

	public class LevelCrossings
	{
		private TrackDB trackDB;
		/// <summary>
		/// determines how many levelcrossingobjects should be updated each frame, now hardcoded to be 1/20
		/// </summary>
		private int refreshCount = 0;

		private int[,] visited;
		/// <summary>
		/// A list of levelcrossingobjects, built by BuildCrossingList
		/// </summary>
		private LevelCrossingObject[] levelCrossingObjects;
		/// <summary>
		/// UpdateCrossingState requires a copy of rearTDBTraveller if the train moves backward,
		/// thus declare the variable here to have a bit cache friendness
		/// </summary>
		private TDBTraveller traveller;
		/// <summary>
		/// Gets an array of all the LevelCrossingObjects.
		/// </summary>
		internal LevelCrossingObject[] LevelCrossingObjects
		{
			get
			{
				return levelCrossingObjects;
			}
		}
		/// <summary>
		/// number of crossings
		/// </summary>
		public int noCrossing = 0;
		private int foundCrossings = 0;



		public LevelCrossings(Simulator simulator)
		{
			trackDB = simulator.TDB.TrackDB;
			BuildCrossingList(simulator.TDB.TrackDB.TrItemTable, simulator.TDB.TrackDB.TrackNodes);
		}
		/// <summary>
		/// Build and store the crossing lists, using ScanPath, adopted from Signal.cs
		/// <param name="TrItems">The TrackItems from simulator.TDB.TrackDB.TrItemTable.</param>
		/// <param name="trackNodes">The TrackNodes from simulator.TDB.TrackDB.TrackNodes.</param>
		/// </summary>
		private void BuildCrossingList(TrItem[] TrItems, TrackNode[] trackNodes)
		{
			visited = new int[trackNodes.Length, 2];
			for (int i = 0; i < trackNodes.Length; i++)
			{
				visited[i, 0] = 0;
				visited[i, 1] = 0;
			}
			//
			//  Determaine the number of crossings in the track Objects list
			//
			noCrossing = 0;
			if (TrItems == null) return;                // No track Objects in route.
			foreach (TrItem trItem in TrItems)
			{
				if (trItem != null)
				{
					if (trItem.ItemType == TrItem.trItemType.trXING)
					{
						noCrossing++;
					}
				}
			}
			//
			//  Only continue if one or more crossings in route.
			//
			if (noCrossing > 0)
			{
				levelCrossingObjects = new LevelCrossingObject[noCrossing];
				LevelCrossingObject.trackNodes = trackNodes;
				LevelCrossingObject.levelCrossingObjects = levelCrossingObjects;
				LevelCrossingObject.trItems = TrItems;

				for (int i = 1; i < trackNodes.Length; i++)
				{
					// Using the track end node as starting point to find crossings.
					if (trackNodes[i].TrEndNode)
					{
						int direction = trackNodes[i].TrPins[0].Direction;
						int nextNode = trackNodes[i].TrPins[0].Link;
						visited[i, direction] = 1;
						ScanPath(nextNode, direction, TrItems, trackNodes);
					}
				}
			}

		} //BuildCrossingList

		/// <summary>
		/// This method follows the track path to find any crossing objects along it, adopted from signals.cs.
		/// <param name="startIndex">what is the index the path starts</param>
		/// <param name="startDir">which direction to go</param>
		/// <param name="TrItems">The TrackItems from simulator.TDB.TrackDB.TrItemTable.</param>
		/// <param name="trackNodes">The TrackNodes from simulator.TDB.TrackDB.TrackNodes.</param>
		/// </summary>
		private void ScanPath(int startIndex, int startDir, TrItem[] TrItems, TrackNode[] trackNodes)
		{
			int index = startIndex;
			int direction = startDir;
			int lastCrossing = -1;                // Index to last crossing found in path -1 if none
			do
			{
				// Return if this track node has already been processed.
				if (index == 0) return;
				if (visited[index, direction] > 0) return;
				visited[index, direction] = 1;      //  Mark track node as processed

				if (trackNodes[index].TrEndNode) return;
				//  Is it a vector node then it may contain objects.
				if (trackNodes[index].TrVectorNode != null)
				{
					// Any obects ?
					if (trackNodes[index].TrVectorNode.noItemRefs > 0)
					{
						if (direction == 1)
						{
							for (int i = 0; i < trackNodes[index].TrVectorNode.noItemRefs; i++)
							{
								if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]] != null)
								{

									// Track Item is crossing
									if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trXING)
									{
										LevelCrItem sigItem = (LevelCrItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
										if ((int)sigItem.revDir == direction)
										{
											lastCrossing = AddCrossing(index, i, (int)sigItem.Direction, lastCrossing, TrItems, trackNodes);
										}
									}
								}
							}
						}
						else
						{
							for (int i = trackNodes[index].TrVectorNode.noItemRefs - 1; i >= 0; i--)
							{
								if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]] != null)
								{
									// Track Item is crossing
									if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trXING)
									{
										LevelCrItem crossItem = (LevelCrItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
										if ((int)crossItem.revDir == direction)
										{
											lastCrossing = AddCrossing(index, i, (int)crossItem.Direction, lastCrossing, TrItems, trackNodes);
										}
									}
								}
							}
						}
					}
				}
				else if (trackNodes[index].TrJunctionNode != null)
				{
					if (direction == 0)
					{
						if (trackNodes[index].Inpins > 1)
						{
							for (int i = 0; i < trackNodes[index].Inpins; i++)
							{
								ScanPath(trackNodes[index].TrPins[i].Link, trackNodes[index].TrPins[i].Direction, TrItems, trackNodes);
							}
							return;
						}
					}
					else
					{
						if (trackNodes[index].Outpins > 1)
						{
							for (int i = 0; i < trackNodes[index].Outpins; i++)
							{
								ScanPath(trackNodes[index].TrPins[i + trackNodes[index].Inpins].Link, trackNodes[index].TrPins[i + trackNodes[index].Inpins].Direction, TrItems, trackNodes);
							}
							return;
						}
					}
				}
				// Get the next node
				if (direction == 0)
				{
					direction = trackNodes[index].TrPins[0].Direction;
					index = trackNodes[index].TrPins[0].Link;
				}
				else
				{
					direction = trackNodes[index].TrPins[trackNodes[index].Outpins].Direction;
					index = trackNodes[index].TrPins[trackNodes[index].Outpins].Link;
				}
			} while (true);
		}   //ScanPath 


		// This method adds a new Crossing to the list
		private int AddCrossing(int trackNode, int nodeIndx, int direction, int prevSignal, TrItem[] TrItems, TrackNode[] trackNodes)
		{
			levelCrossingObjects[foundCrossings] = new LevelCrossingObject();
			levelCrossingObjects[foundCrossings].trackNode = trackNode;
			levelCrossingObjects[foundCrossings].trRefIndex = nodeIndx;
			levelCrossingObjects[foundCrossings].thisRef = foundCrossings;
			foundCrossings++;
			return foundCrossings - 1;
		} // AddCrossing


		private void NextNode(TrackNode[] trackNodes, ref int node, ref int direction)
		{
			if (trackNodes[node].TrJunctionNode != null)
			{
				if (direction == 0)
				{
					if (trackNodes[node].Inpins > 1)
					{
						if (trackNodes[node].TrJunctionNode.SelectedRoute == 0)
						{
							direction = trackNodes[node].TrPins[0].Direction;
							node = trackNodes[node].TrPins[0].Link;
						}
						else
						{
							direction = trackNodes[node].TrPins[1].Direction;
							node = trackNodes[node].TrPins[1].Link;
						}
					}
					else
					{
						direction = trackNodes[node].TrPins[0].Direction;
						node = trackNodes[node].TrPins[0].Link;
					}
				}
				else
				{
					if (trackNodes[node].Outpins > 1)
					{
						if (trackNodes[node].TrJunctionNode.SelectedRoute == 0)
						{
							direction = trackNodes[node].TrPins[trackNodes[node].Inpins].Direction;
							node = trackNodes[node].TrPins[trackNodes[node].Inpins].Link;
						}
						else
						{
							direction = trackNodes[node].TrPins[trackNodes[node].Inpins + 1].Direction;
							node = trackNodes[node].TrPins[trackNodes[node].Inpins + 1].Link;
						}
					}
					else
					{
						direction = trackNodes[node].TrPins[trackNodes[node].Inpins].Direction;
						node = trackNodes[node].TrPins[trackNodes[node].Inpins].Link;
					}
				}
			}
			else
			{
				if (direction == 0)
				{
					direction = trackNodes[node].TrPins[0].Direction;
					node = trackNodes[node].TrPins[0].Link;
				}
				else
				{
					direction = trackNodes[node].TrPins[trackNodes[node].Inpins].Direction;
					node = trackNodes[node].TrPins[trackNodes[node].Inpins].Link;
				}
			}
		} //NextNode

		/// <summary>
		/// This method update 1/20 of crossings in the viewing range per frame for a given train.
		/// <param name="train">the train approching or leaving a crossing</param>
		/// <param name="SpeedMpS">the speed of the train (negative means moving reversely)</param>
		/// </summary>
		public bool UpdateCrossings(Train train, float SpeedMpS)
		{
			//no crossing, go back 
			if (noCrossing == 0) return false;
			float frontDist, rearDist;
			//each frame only updates 1/20 crossings
			int eachUpdate = noCrossing / 20 + 1;
			float distance; //distance to activate the gate
			float endDist; //gate will open if >endDist from the end

			// only update the distance for 1/20 of the crossings each time.
			for (int i = eachUpdate * refreshCount; i < eachUpdate * refreshCount + eachUpdate && i < noCrossing; i++)
			{
				//if the crossing is not in viewing range, go back
				if (LevelCrossingObjects[i] == null ||
					LevelCrossingObjects[i].levelCrossingObj == null ||
					LevelCrossingObjects[i].levelCrossingObj.inrange == false) continue;

				//compute activiting distance
				distance = LevelCrossingObjects[i].levelCrossingObj.warningTime * SpeedMpS;
				endDist = LevelCrossingObjects[i].endDist;
				if (distance > 0 && distance < endDist) distance = endDist;
				else if (distance < 0 && distance > -endDist) distance = -endDist; //mini warning distance

				//if the train is moving backward
				if (SpeedMpS < 0)
				{
					//copy the rearTDBTraveller and move it 300m in the moving direction (-300), and find the distance

					//distance from the front and rear of the train to a crossing
					//even when the train is moving backward, front is still the original front of the train
					//if a crossing is not reachable, both will be -1
					//if by moving -distance, the train has positive rear distance, 
					//it means the train is within distance of a crossing
					traveller = new TDBTraveller(train.RearTDBTraveller); //the frontDist is the rearend dist since it moves backward
					traveller.Move(distance); //distance is negative
					frontDist = LevelCrossingObjects[i].DistanceTo(traveller);
					if (frontDist < 0) //not reached
					{
						LevelCrossingObjects[i].TrainLeaving(train);
						continue;
					}
					traveller = new TDBTraveller(train.FrontTDBTraveller);
					traveller.Move(endDist);
					rearDist = LevelCrossingObjects[i].DistanceTo(traveller);
					//both -1, the crossing is not reachable (or has just passed the crossing), so call trainleaving anyway

					//if train is far away, do nothing
					if (frontDist >= 0 && rearDist < 0)
					{
						LevelCrossingObjects[i].TrainApproaching(train);
					}
					else
					{
						LevelCrossingObjects[i].TrainLeaving(train);
					}
				}
				else if (SpeedMpS <= 0.1) //train is stopping and not in a gate
				{
					traveller = new TDBTraveller(train.FrontTDBTraveller);
					traveller.Move(endDist);
					frontDist = LevelCrossingObjects[i].DistanceTo(traveller);

					traveller = new TDBTraveller(train.RearTDBTraveller);
					traveller.Move(-endDist);
					rearDist = LevelCrossingObjects[i].DistanceTo(traveller);

					if (rearDist >= 0 && frontDist <= 0)//train is stopping at a crossing gate or is very close to it
					{
						LevelCrossingObjects[i].TrainApproaching(train);
					}
					else
					{
						LevelCrossingObjects[i].TrainLeaving(train);
					}
				}
				else // train moves forward
				{
					traveller = new TDBTraveller(train.FrontTDBTraveller);
					traveller.Move(distance);
					frontDist = LevelCrossingObjects[i].DistanceTo(traveller);
					if (frontDist > 10) //too far away, not reached
					{
						LevelCrossingObjects[i].TrainLeaving(train);
						continue;
					}

					traveller = new TDBTraveller(train.RearTDBTraveller);
					traveller.Move(-endDist);
					rearDist = LevelCrossingObjects[i].DistanceTo(traveller);

					//both -1, the crossing is not reachable (or has just passed the crossing), so call trainleaving anyway
					if (rearDist < 0 && frontDist < 0)
					{
						LevelCrossingObjects[i].TrainLeaving(train);
						continue;
					}
					//if train is far away, do nothing
					if (rearDist >= 0 && frontDist >= distance)
					{
					}
					else if (rearDist >= 0 && frontDist > 0) // train is within warning range
					{
						LevelCrossingObjects[i].TrainLeaving(train);
					}
					else if (rearDist >= 0 && frontDist < 0)//train is in a crossing range
					{
						LevelCrossingObjects[i].TrainApproaching(train);
					}

				}
			}
			if (refreshCount++ > 21) refreshCount = 0;
			return true;
		}//UpdateCrossings
	} //LevelCrossings


	public class LevelCrossingObject
	{
		public static LevelCrossingObject[] levelCrossingObjects;
		public static TrackNode[] trackNodes;
		public static TrItem[] trItems;
		public int trackNode;                   // Track node which contains this crossing
		public int trRefIndex;                  // Index to TrItemRef within Track Node 
		public int thisRef;                     // This crossing's reference.
		public LevelCrossingObj levelCrossingObj; //LevelCrossingObj has LevelCrossingShape
		public List<Train> trains; // trains that on the crossing
		public List<LevelCrossingObject> groups; //sister crossings working together (parrallel lines)
		public float endDist = 10;
		public CarSpawner carSpawner = null; //spawner that will cross the gate

		public List<Train> mytrain; //the train that is on the track of the crossing, not on the sister crossings


		/// <summary>
		/// This method update a crossing if a train is approaching. 
		/// If the crossing has trains, it won't do anything, otherwise, it will update the LevelCrossingObj, 
		/// which will set the animation direction of the LevelCrossingShape
		/// <param name="t">the train approching the crossing</param>
		/// </summary>
		public int TrainApproaching(Train t)
		{
			// no groups, means the crossing has no shape, no need to do anything (or its sister will handle things)
			if (groups == null)
			{
				return 0;
			}
			// no train on the crossing yet
			if (mytrain == null)
			{
				mytrain = new List<Train>();
			}
			//no train on the sisters yet
			if (trains == null)
			{
				trains = new List<Train>();
				//tell every sister the crossing has train now
				foreach (LevelCrossingObject levelObjects in groups)
				{
					levelObjects.trains = trains;
				}
			}
			//if previously no train, move animation to close the crossing
			if (trains.Count == 0)
			{
				levelCrossingObj.movingDirection = 1;
			}
			//if the train is not at the gate before, add it to the list
			if (!mytrain.Contains(t))
			{
				mytrain.Add(t);
			}
			if (!trains.Contains(t))
			{
				trains.Add(t);
			}
			return 0;
		}//trainApproaching
		/// <summary>
		/// This method update a crossing if a train is leaving. 
		/// If the crossing has no train, it won't do anything, otherwise, it will update the LevelCrossingObj, 
		/// which will set the animation direction of the LevelCrossingShape
		/// <param name="t">the train approching the crossing</param>
		/// </summary>
		public int TrainLeaving(Train t)
		{
			if (groups == null)
			{
				return 0;
			}
			//no train before, do nothing
			if (trains == null || mytrain == null) return 0;
			if (trains.Count == 0)
			{
				return 0;
			}
			//if the crossing has the train, remove it
			if (mytrain.Contains(t))
			{
				trains.Remove(t);
				mytrain.Remove(t);
			}
			//if the crossing has no train now, just move the gate open
			if (trains.Count == 0)
			{
				levelCrossingObj.movingDirection = 0;
			}
			return 0;
		}//trainLeaving

		//check if a crossing has train
		public bool HasTrain()
		{
			if (trains == null) return false;
			if (trains.Count > 0) return true;
			else return false;
		}//hasTrain

		/// <summary>
		/// Returns the distance from the TDBtraveller to this crossing. 
		/// </summary>
		public float DistanceTo(TDBTraveller tdbTraveller)
		{
			int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
			return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
		}  //DistanceTo a track

		public float DistanceTo(RDBTraveller rdbTraveller)
		{
			int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
			return rdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
		}  //DistanceTo a car

		/// <summary>
		/// Gets the correspnding TrItem from the TDB.
		/// </summary>
		public int trItem
		{
			get
			{
				return trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
			}
		}

	}  // LevelCrossingOnbject

}
