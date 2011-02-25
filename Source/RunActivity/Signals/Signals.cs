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
	public class Signals
	{


		private TrackDB trackDB;

		private int[,] visited;
		private SignalObject[] signalObjects;

		/// <summary>
		/// Gets an array of all the SignalObjects.
		/// </summary>
		internal SignalObject[] SignalObjects
		{
			get
			{
				return signalObjects;
			}
		}

		public int noSignals = 0;
		private int foundSignals = 0;



		public Signals(Simulator simulator, SIGCFGFile sigcfg)
		{
			trackDB = simulator.TDB.TrackDB;
			BuildSignalList(simulator.TDB.TrackDB.TrItemTable, simulator.TDB.TrackDB.TrackNodes);
            if (foundSignals > 0) AddCFG(sigcfg);  // Add links to the sigcfg.dat file
		}

		// Restore state to resume a saved game
		public Signals(Simulator simulator, SIGCFGFile sigcfg, BinaryReader inf)
            : this(simulator, sigcfg)
		{
		}

		// Save state to resume the game later
		public void Save(BinaryWriter outf)
		{
		}

		public void Update(float elapsedClockSeconds)
		{
			if (foundSignals > 0)
			{
				foreach (SignalObject signal in signalObjects)
				{
					if (signal != null) // to cater for orphans. RE bug!
					{
						signal.Update();
					}
				}
			}
		}  //Update

		private void BuildSignalList(TrItem[] TrItems, TrackNode[] trackNodes)
		{
			visited = new int[trackNodes.Length, 2];
			for (int i = 0; i < trackNodes.Length; i++)
			{
				visited[i, 0] = 0;
				visited[i, 1] = 0;
			}
			//
			//  Determaine the number of signals in the track Objects list
			//
			noSignals = 0;
			if (TrItems == null) return;                // No track Objects in route.
			foreach (TrItem trItem in TrItems)
			{
				if (trItem != null)
				{
					if (trItem.ItemType == TrItem.trItemType.trSIGNAL)
					{
						noSignals++;
					}
				}
			}
			//
			//  Only continue if one or more signals in route.
			//
			if (noSignals > 0)
			{
				signalObjects = new SignalObject[noSignals];
				SignalObject.trackNodes = trackNodes;
				SignalObject.signalObjects = signalObjects;
				SignalObject.trItems = TrItems;

				for (int i = 1; i < trackNodes.Length; i++)
				{
					// Using the track end node as starting point to find signals.
					if (trackNodes[i].TrEndNode)
					{
						int direction = trackNodes[i].TrPins[0].Direction;
						int nextNode = trackNodes[i].TrPins[0].Link;
						visited[i, direction] = 1;
						ScanPath(nextNode, direction, TrItems, trackNodes);
					}
				}
			}

		} //BuildSignalList

		//
		//  This method follows the track path to find any signal objects along it.
		//
		private void ScanPath(int startIndex, int startDir, TrItem[] TrItems, TrackNode[] trackNodes)
		{
			int index = startIndex;
			int direction = startDir;
			int lastSignal = -1;                // Index to last signal found in path -1 if none
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

									// Track Item is signal
									if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
									{
										SignalItem sigItem = (SignalItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
										if ((int)sigItem.revDir == direction)
										{
											sigItem.sigObj = foundSignals;
											lastSignal = AddSignal(index, i, (int)sigItem.Direction, lastSignal, TrItems, trackNodes);
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
									// Track Item is signal
									if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
									{
										SignalItem sigItem = (SignalItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
										if ((int)sigItem.revDir == direction)
										{
											sigItem.sigObj = foundSignals;
											lastSignal = AddSignal(index, i, (int)sigItem.Direction, lastSignal, TrItems, trackNodes);
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
							if (lastSignal >= 0) signalObjects[lastSignal].isJunction = true;
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
							if (lastSignal >= 0) signalObjects[lastSignal].isJunction = true;
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


		// This method adds a new Signal to the list
		private int AddSignal(int trackNode, int nodeIndx, int direction, int prevSignal, TrItem[] TrItems, TrackNode[] trackNodes)
		{
			if (prevSignal >= 0)
			{
				if (signalObjects[prevSignal].isSignalHead((SignalItem)TrItems[trackNodes[trackNode].TrVectorNode.TrItemRefs[nodeIndx]]))
				{
					signalObjects[prevSignal].AddHead(nodeIndx);
					return prevSignal;
				}
			}
			signalObjects[foundSignals] = new SignalObject();
			signalObjects[foundSignals].direction = direction;
			signalObjects[foundSignals].trackNode = trackNode;
			signalObjects[foundSignals].trRefIndex = nodeIndx;
			signalObjects[foundSignals].prevSignal = prevSignal;
			signalObjects[foundSignals].AddHead(nodeIndx);
			signalObjects[foundSignals].thisRef = foundSignals;
			//if (prevSignal >= 0) signalObjects[prevSignal].nextSignal = foundSignals;
			foundSignals++;
			return foundSignals - 1;
		} // AddSignal

		//
		//  This method returns the index of the next signal along the set path. -1 if no signal found
		//
		public int FindNextSignal(int startIndex, int startDir, TrItem[] TrItems, TrackNode[] trackNodes)
		{
			int index = startIndex;
			int direction = startDir;
			do
			{
				if (trackNodes[index].TrEndNode) return -1;
				if (trackNodes[index].TrVectorNode != null)
				{
					// Any obects ?
					if (trackNodes[index].TrVectorNode.noItemRefs > 0)
					{
						if (direction == 0)
						{
							for (int i = 0; i < trackNodes[index].TrVectorNode.noItemRefs; i++)
							{
								// Track Item is signal
								if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
								{
									SignalItem sigItem = (SignalItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
									if ((int)sigItem.revDir == direction)
									{
										return sigItem.sigObj;
									}
								}
							}
							direction = trackNodes[index].TrPins[0].Direction;
							index = trackNodes[index].TrPins[0].Link;
						}
						else
						{
							for (int i = trackNodes[index].TrVectorNode.noItemRefs - 1; i >= 0; i--)
							{
								// Track Item is signal
								if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
								{
									SignalItem sigItem = (SignalItem)TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]];
									if ((int)sigItem.revDir == direction)
									{
										return sigItem.sigObj;
									}
								}
							}
							direction = trackNodes[index].TrPins[trackNodes[index].Outpins].Direction;
							index = trackNodes[index].TrPins[trackNodes[index].Outpins].Link;
						}
					}
				}
				else if (trackNodes[index].TrJunctionNode != null)
				{
					if (direction == 0)
					{
						if (trackNodes[index].Inpins > 1)
						{
							if (trackNodes[index].TrJunctionNode.SelectedRoute == 0)
							{
								direction = trackNodes[index].TrPins[0].Direction;
								index = trackNodes[index].TrPins[0].Link;
							}
							else
							{
								direction = trackNodes[index].TrPins[1].Direction;
								index = trackNodes[index].TrPins[1].Link;
							}
						}
						else
						{
							direction = trackNodes[index].TrPins[0].Direction;
							index = trackNodes[index].TrPins[0].Link;
						}
					}
					else
					{
						if (trackNodes[index].Outpins > 1)
						{
							if (trackNodes[index].TrJunctionNode.SelectedRoute == 0)
							{
								direction = trackNodes[index].TrPins[trackNodes[index].Inpins].Direction;
								index = trackNodes[index].TrPins[trackNodes[index].Inpins].Link;
							}
							else
							{
								direction = trackNodes[index].TrPins[trackNodes[index].Inpins + 1].Direction;
								index = trackNodes[index].TrPins[trackNodes[index].Inpins + 1].Link;
							}
						}
						else
						{
							direction = trackNodes[index].TrPins[trackNodes[index].Inpins].Direction;
							index = trackNodes[index].TrPins[trackNodes[index].Inpins].Link;
						}
					}
				}
				else
				{
					if (direction == 0)
					{
						direction = trackNodes[index].TrPins[0].Direction;
						index = trackNodes[index].TrPins[0].Link;
					}
					else
					{
						direction = trackNodes[index].TrPins[trackNodes[index].Inpins].Direction;
						index = trackNodes[index].TrPins[trackNodes[index].Inpins].Link;
					}

				}
			} while (true);
		}



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

		//
		//      This method adds the sigcfg reference to each signal object.
		//
		private void AddCFG(SIGCFGFile sigCFG)
		{
			foreach (SignalObject signal in signalObjects)
			{
				if (signal != null)
				{
					signal.SetSignalType(sigCFG);
				}
			}
		}

		//
		// This method resets junction signals to indeterminate state
		// To do: make this more specific
		//
		public void ResetJunction()
		{
			foreach (SignalObject signal in signalObjects)
			{
				if (signal != null)
				{
					if (signal.isJunction) signal.nextSignal = -2;
				}
			}
		}


		//
		// Get the nearest (NORMAL)signal to the current point in the tdbtraveller
		// Returns -1 if one cannot be found.
		//

		public int FindNextSignal(TDBTraveller tdbtraveller)
		{
			return FindSignal(tdbtraveller, tdbtraveller.Direction);
		}

		public Signal FindNearestSignal(TDBTraveller tdbtraveller)
		{
			int sigRef = FindNextSignal(tdbtraveller);
			return new Signal(this, signalObjects, sigRef);
		}

		public int FindPrevSignal(TDBTraveller tdbtraveller)
		{
			TDBTraveller revTDBtraveller = new TDBTraveller(tdbtraveller);
			revTDBtraveller.ReverseDirection();
			int direction = tdbtraveller.Direction;
			return FindSignal(revTDBtraveller, direction);
		}

		public int FindSignal(TDBTraveller tdbtraveller, int Direction)
		{
			int startNode = tdbtraveller.TrackNodeIndex;
			int currenNode = startNode;
			int currDir = Direction;
			int sigIndex = -1;
			float distance = 999999.0f;
			TrackNode[] trackNodes = trackDB.TrackNodes;
			TrItem[] trItems = trackDB.TrItemTable;

			if (noSignals < 1) return -1; ;   // No Signals on route

			do
			{
				if (trackNodes[currenNode].TrEndNode) return -1;  // End of track reached no signals found.
				if (trackNodes[currenNode].TrVectorNode != null)
				{
					if (trackNodes[currenNode].TrVectorNode.noItemRefs > 0)
					{
						for (int i = 0; i < trackNodes[currenNode].TrVectorNode.noItemRefs; i++)
						{
							if (trItems[trackNodes[currenNode].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
							{
								SignalItem sigItem = (SignalItem)trItems[trackNodes[currenNode].TrVectorNode.TrItemRefs[i]];
								if (sigItem.revDir == currDir)
								{
									int sigObj = sigItem.sigObj;
									if (signalObjects[sigObj] != null) //WaltN: Fixes Sandpatch problem
										if (signalObjects[sigObj].isSignalNormal())
										{
											float dist = signalObjects[sigObj].DistanceTo(tdbtraveller);
											if (dist > 0)
											{
												if (dist < distance)
												{
													distance = dist;
													sigIndex = sigObj;
												}
											}
										}
								}
							}
						}

						if (sigIndex >= 0) return sigIndex; // Signal found in this node.
					}

				}
				NextNode(trackNodes, ref currenNode, ref currDir);
				if (currenNode == startNode) return -1; // back to where we started !
			} while (true);

		} //FindNearestSignal

		public KeyValuePair<SignalObject, SignalHead>? FindByTrItem(uint trItem)
		{
			foreach (var signal in signalObjects)
				if (signal != null)
					foreach (var head in signal.SignalHeads)
						if (SignalObject.trackNodes[signal.trackNode].TrVectorNode.TrItemRefs[head.trItemIndex] == (int)trItem)
							return new KeyValuePair<SignalObject, SignalHead>(signal, head);
			return null;
		}
	}


	public class SignalObject
	{

		public enum BLOCKSTATE
		{
			CLEAR,					// Block ahead is clear
			OCCUPIED,				// Block ahead is occupied by one or more wagons/locos
			JN_OBSTRUCTED	        // Block ahead is impassable due to the state of a switch
		}

		public static SignalObject[] signalObjects;
		public static TrackNode[] trackNodes;
		public static TrItem[] trItems;
		public List<SignalHead> SignalHeads = new List<SignalHead>();
		public int trackNode;                   // Track node which contains this signal
		public int trRefIndex;                  // Index to TrItemRef within Track Node 
		public int thisRef;                     // This signal's reference.
		public int direction;                   // Diection facing on track
		public int draw_state;
		public bool enabled = true;
		public bool isJunction = false;          // Indicates whether the signal controls a junction.
		public bool canUpdate = true;           // Signal can be updated automatically
		public bool isAuto = true;
		public bool useScript = false;
		public BLOCKSTATE blockState = BLOCKSTATE.CLEAR;
		public Signal.PERMISSION hasPermission = Signal.PERMISSION.DENIED;  // Permission to pass red signal
		public int nextSignal = -2;             // Index to next signal. -1 if none -2 indeterminate
		public int prevSignal = -2;             // Index to previous signal -1 if none -2 indeterminate

		//
		//  Needed because signal faces train!
		//
		public int revDir
		{
			get { return direction == 0 ? 1 : 0; }
		}

		public BLOCKSTATE block_state()
		{
			return blockState;
		}

		//
		//  Returns which route is set for a junction link signal head
		//  Returns False if none
		//
		public bool route_set(int iLinknode)
		{
			int currentTrackNode = trackNode;
			int currentIndex = trRefIndex;
			int currentDir = this.revDir;

			while (currentTrackNode != iLinknode)
			{
				if (trackNodes[currentTrackNode].TrEndNode) return false;  // End of track reached
				NextNode(ref currentTrackNode, ref currentDir);
			}

			if (currentTrackNode == trackNode) return false;  // Stop it going loopy
			return true;
		}

		// Returns true if at least one signal head is type normal.
		public bool isSignalNormal()
		{
			foreach (SignalHead sigHead in SignalHeads)
			{
				if (sigHead.sigFunction == SignalHead.SIGFN.NORMAL) return true;
			}
			return false;
		}

		public SignalHead.SIGASP next_sig_mr(SignalHead.SIGFN fn_type)
		{
			if (nextSignal < -1)
			{
				nextSignal = NextSignal();
			}
			if (nextSignal >= 0)
			{
				return signalObjects[nextSignal].this_sig_mr(fn_type);
			}
			else
			{
				return SignalHead.SIGASP.STOP;
			}
		}

		public SignalHead.SIGASP next_sig_lr(SignalHead.SIGFN fn_type)
		{
			if (nextSignal < -1)
			{
				nextSignal = NextSignal();
			}
			if (nextSignal >= 0)
			{
				return signalObjects[nextSignal].this_sig_lr(fn_type);
			}
			else
			{
				return SignalHead.SIGASP.STOP;
			}
		}



		//
		//  Finds the next (NORMAL) signal down the line from this one.
		//  Returns -1 if one cannot be found.
		//
		private int NextSignal()
		{
			int currentTrackNode = trackNode;
			int currentIndex = trRefIndex;
			int currentDir = this.revDir;

			// Is the next signal within the current tracknode?
			if (trackNodes[currentTrackNode].TrVectorNode != null)
			{
				// Only process if there is more than one item within track node
				if (trackNodes[currentTrackNode].TrVectorNode.noItemRefs > 1)
				{
					if (currentDir == 1)
					{
						while (++currentIndex < trackNodes[currentTrackNode].TrVectorNode.noItemRefs)
						{
							int index = trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[currentIndex];
							if (trItems[index].ItemType == TrItem.trItemType.trSIGNAL)
							{
								SignalItem signalItem = (SignalItem)trItems[index];
								int sigIndex = signalItem.sigObj;
								if ((signalObjects[sigIndex] != null) && (signalObjects[sigIndex].thisRef != thisRef)) // Not a signal head for this signal
								{
									if (signalObjects[sigIndex].isSignalNormal() && signalObjects[sigIndex].revDir == currentDir) return sigIndex;
								}
							}
						}
					}
					else
					{
						while (--currentIndex > 0)
						{
							int index = trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[currentIndex];
							if (trItems[index].ItemType == TrItem.trItemType.trSIGNAL)
							{
								SignalItem signalItem = (SignalItem)trItems[index];
								int sigIndex = signalItem.sigObj;
								if ((signalObjects[sigIndex] != null) && (signalObjects[sigIndex].thisRef != thisRef)) // Not a signal head for this signal
								{
									if (signalObjects[sigIndex].isSignalNormal() && signalObjects[sigIndex].revDir == currentDir) return sigIndex;
								}
							}
						}
					}
				}
			}

			do
			{
				// Get the next track node
				if (trackNodes[currentTrackNode].TrEndNode) return -1;  // End of track reached
				NextNode(ref currentTrackNode, ref currentDir);
				if (currentTrackNode == 0) return -1;  // End of track reached (broken track database?)

				if (currentTrackNode == trackNode) return -1;  // Back to start node again !!

				// If new track node is a vector look for signal object
				if (trackNodes[currentTrackNode].TrVectorNode != null)
				{
					if (trackNodes[currentTrackNode].TrVectorNode.noItemRefs > 0)
					{
						if (currentDir == 1)
						{
							for (int i = 0; i < trackNodes[currentTrackNode].TrVectorNode.noItemRefs; i++)
							{
								// Track Item is signal
								if (trItems[trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
								{
									SignalItem signalItem = (SignalItem)trItems[trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i]];
									if ((int)signalItem.revDir == currentDir)
									{
										int sigIndex = signalItem.sigObj;
										if (signalObjects[sigIndex].isSignalNormal()) return sigIndex;
									}
								}
							}
						}
						else
						{
							for (int i = trackNodes[currentTrackNode].TrVectorNode.noItemRefs - 1; i >= 0; i--)
							{
								// Track Item is signal
								if (trItems[trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i]].ItemType == TrItem.trItemType.trSIGNAL)
								{
									SignalItem signalItem = (SignalItem)trItems[trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i]];
									if ((int)signalItem.revDir == currentDir)
									{
										int sigIndex = signalItem.sigObj;   // Signal reference for this item
										if (signalObjects[sigIndex].isSignalNormal()) return sigIndex;
									}
								}
							}
						}
					}
				}

			} while (true);
		}

		// Returns the next node and direction in the TDB
		private void NextNode(ref int node, ref int direction)
		{
			if (trackNodes[node].TrVectorNode != null)
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
			else if (trackNodes[node].TrJunctionNode != null)
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
		}

		public SignalHead.SIGASP opp_sig_mr(SignalHead.SIGFN fn_type)
		{
			return SignalHead.SIGASP.STOP;
		}

		public SignalHead.SIGASP opp_sig_lr(SignalHead.SIGFN fn_type)
		{
			return SignalHead.SIGASP.STOP;
		}

		/// <summary>
		/// Returns the most restrictive state of this signal's heads.
		/// </summary>
		public SignalHead.SIGASP this_sig_mr(SignalHead.SIGFN fn_type)
		{
			SignalHead.SIGASP sigAsp = SignalHead.SIGASP.UNKNOWN;
			foreach (SignalHead sigHead in SignalHeads)
			{
				if (sigHead.sigFunction == fn_type)
				{
					if (sigHead.state < sigAsp)
					{
						sigAsp = sigHead.state;
					}
				}
			}
			if (sigAsp == SignalHead.SIGASP.UNKNOWN) return SignalHead.SIGASP.STOP; else return sigAsp;
		}

		/// <summary>
		/// Returns the least restrictive state of this signal's heads.
		/// </summary>
		public SignalHead.SIGASP this_sig_lr(SignalHead.SIGFN fn_type)
		{
			SignalHead.SIGASP sigAsp = SignalHead.SIGASP.STOP;
			foreach (SignalHead sigHead in SignalHeads)
			{
				if (sigHead.sigFunction == fn_type)
				{
					if (sigHead.state > sigAsp)
					{
						sigAsp = sigHead.state;
					}
				}
			}
			return sigAsp;
		}

		/// <summary>
		/// Perform the update for each head on this signal.
		/// </summary>
		public void Update()
		{
			if (canUpdate)
			{
				foreach (SignalHead sigHead in SignalHeads)
				{
					sigHead.Update();
				}
			}

		} // Update

		/// <summary>
		/// Returns the distance from the TDBtraveller to this signal. 
		/// </summary>
		public float DistanceTo(TDBTraveller tdbTraveller)
		{
			int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
			return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
		}  //DistanceTo


		/// <summary>
		/// Check Whether signal head is for this signal.
		/// </summary>
		public bool isSignalHead(SignalItem signalItem)
		{
			SignalItem thisSignalItem = (SignalItem)trItems[this.trItem];   // Tritem for this signal
			if (signalItem.TileX == thisSignalItem.TileX && signalItem.TileZ == thisSignalItem.TileZ) // Same Tile
			{
				if ((Math.Abs(signalItem.X - thisSignalItem.X) < 0.01) && (Math.Abs(signalItem.Y - thisSignalItem.Y) < 0.01) && (Math.Abs(signalItem.Z - thisSignalItem.Z) < 0.01))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Adds a head to this signal.
		/// </summary>
		public void AddHead(int trItem)
		{
			SignalHead head = new SignalHead(this, trItem);
			SignalHeads.Add(head);
		}

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

		/// <summary>
		/// Sets the signal type from the sigcfg file for each signal head.
		/// </summary>
		public void SetSignalType(SIGCFGFile sigCFG)
		{
			foreach (SignalHead sigHead in SignalHeads)
			{
				sigHead.SetSignalType(trItems, sigCFG);
			}
		}

		public void SetSignalState(Signal.SIGNALSTATE state)
		{
			switch (state)
			{
				case Signal.SIGNALSTATE.STOP:
					canUpdate = false;
					foreach (SignalHead sigHead in SignalHeads)
					{
						sigHead.state = SignalHead.SIGASP.STOP;
					}
					break;
				case Signal.SIGNALSTATE.CLEAR:
					canUpdate = true;
					break;
				case Signal.SIGNALSTATE.UNKNOWN:
					break;
				default:
					break;
			}
		} // SetSignalState

		public int GetNextSignal()
		{
			if (nextSignal < -1)
			{
				nextSignal = NextSignal();
			}
			return nextSignal;
		}

		public void TrackStateChanged()
		{
			//if(isJunction) nextSignal=-2;
			nextSignal = -2;
		}

		/// <summary>
		/// Gets the display aspect for the track monitor.
		/// </summary>
		public TrackMonitorSignalAspect GetMonitorAspect()
		{
			switch (this_sig_lr(SignalHead.SIGFN.NORMAL))
			{
				case SignalHead.SIGASP.STOP:
				case SignalHead.SIGASP.STOP_AND_PROCEED:
					if (hasPermission == Signal.PERMISSION.GRANTED)
						return TrackMonitorSignalAspect.Warning;
					else
						return TrackMonitorSignalAspect.Stop;
				case SignalHead.SIGASP.RESTRICTING:
				case SignalHead.SIGASP.APPROACH_1:
				case SignalHead.SIGASP.APPROACH_2:
				case SignalHead.SIGASP.APPROACH_3:
				case SignalHead.SIGASP.APPROACH_4:
					return TrackMonitorSignalAspect.Warning;
				case SignalHead.SIGASP.CLEAR_1:
				case SignalHead.SIGASP.CLEAR_2:
				case SignalHead.SIGASP.CLEAR_3:
				case SignalHead.SIGASP.CLEAR_4:
					return TrackMonitorSignalAspect.Clear;
				default:
					return TrackMonitorSignalAspect.None;
			}
		} // GetMonitorAspect

	}  // SignalOnbject

	public class SignalHead
	{
		public enum SIGASP
		{
			STOP,
			STOP_AND_PROCEED,
			RESTRICTING,
			APPROACH_1,
			APPROACH_2,
			APPROACH_3,
			APPROACH_4,
			CLEAR_1,
			CLEAR_2,
			CLEAR_3,
			CLEAR_4,
			UNKNOWN
		}

		public enum SIGFN
		{
			NORMAL,
			DISTANCE,
			REPEATER,
			SHUNTING,
			INFO,
			ALERT,
			UNKNOWN
		}

		public SignalType signalType = null;    // from sigcfg file
		public SIGASP state = SIGASP.STOP;
		public int draw_state;
		public int trItemIndex;                 // Index to trItem   


		private SignalObject mainSignal;         //  This is the signal which this head forms a part.

		public SignalHead(SignalObject sigOoject, int trItem)
		{
			mainSignal = sigOoject;
			trItemIndex = trItem;
		}

		// This method sets the signal type object from the CIGCFG file
		public void SetSignalType(TrItem[] TrItems, SIGCFGFile sigCFG)
		{
			SignalItem sigItem = (SignalItem)TrItems[SignalObject.trackNodes[mainSignal.trackNode].TrVectorNode.TrItemRefs[trItemIndex]];
			signalType = sigCFG.SignalTypes[sigItem.SignalType];
		}

		public SIGFN sigFunction
		{
			get
			{
				if (signalType != null) return (SIGFN)signalType.FnType; else return SIGFN.UNKNOWN;
			}
		}

		//
		//  The type name from CFG Signal type
		//
		public String SignalTypeName
		{
			get
			{
				if (signalType != null) return signalType.Name; else return "";
			}
		}

		//
		//  Following methods used in scipting
		//
		public SIGASP next_sig_mr(SIGFN sigFN)
		{
			return mainSignal.next_sig_mr(sigFN);
		}

		public SIGASP next_sig_lr(SIGFN sigFN)
		{
			return mainSignal.next_sig_mr(sigFN);
		}

		public SIGASP tnis_sig_lr(SIGFN sigFN)
		{
			return mainSignal.this_sig_lr(sigFN);
		}

		public SIGASP tnis_sig_mr(SIGFN sigFN)
		{
			return mainSignal.this_sig_mr(sigFN);
		}

		public SIGASP opp_sig_mr(SIGFN sigFN)
		{
			return mainSignal.this_sig_mr(sigFN);
		}

		public SIGASP opp_sig_lr(SIGFN sigFN)
		{
			return mainSignal.this_sig_lr(sigFN);
		}

		//
		//  Returns the default draw state for this signal head from the SIGCFG file
		//  Retruns -1 id no draw state.
		//
		public int def_draw_state(SIGASP state)
		{
			if (signalType != null) return signalType.def_draw_state(state); else return -1;
		}

		//
		//  Gets the least restrictive aspect for this head from the SIGCFG file depending on state of next signal 
		//
		public SignalHead.SIGASP def_next_state(SignalHead.SIGASP state)
		{
			if (signalType != null) return signalType.GetNextLeastRestrictiveState(state); else return SignalHead.SIGASP.STOP;
		}

		//
		//  Sets the state to the most restrictive aspect for this head.
		//
		public void SetMostRestrictiveAspect()
		{
			if (signalType != null) state = signalType.GetMostRestrictiveAspect(); else state = SignalHead.SIGASP.STOP;
		}

		public int route_set()
		{
			//SignalItem sigItem = (SignalItem)SignalObject.trItems[trItemIndex];
			//if (sigItem.noSigDirs > 0)
			//{
			//    for (int i = 0; i < sigItem.noSigDirs; i++)
			//    {
			//        if (mainSignal.route_set((int)sigItem.TrSignalDirs[i].TrackNode))
			//        {
			//            return i+1;         // route is set
			//        }
			//    }
			//    return 0;   // No links set
			//}
			//else
			//{
			//    return 1;   // Returns 1 if signal does not have link defined
			//}
			return 1;
		}

		//
		//  Default update process
		//  To do: add interface for scripting.
		//
		public void Update()
		{
			if (mainSignal.enabled)
			{
				if (route_set() == 1 && mainSignal.block_state() == SignalObject.BLOCKSTATE.CLEAR)
				{
					switch (this.sigFunction)
					{
						case SIGFN.DISTANCE:
							//
							//  If the signal head is part of a signal with a normal head then check 
							//  whether this is set to stop state, 
							//  If so then set most restrictive aspect for this head,
							//
							if (mainSignal.this_sig_lr(SIGFN.NORMAL) == SIGASP.STOP)
							{
								SetMostRestrictiveAspect();
							}
							else
							{
								state = def_next_state(mainSignal.next_sig_lr(SIGFN.NORMAL));
							}
							break;
						//case SIGFN.INFO:
						//
						//  Info signals depend on the implementation of the route_set function
						//  
						//break;
						case SIGFN.NORMAL:
							state = def_next_state(mainSignal.next_sig_lr(SIGFN.NORMAL));
							break;

						case SIGFN.REPEATER:
							//
							//  For repeater signals just copy the state of the next siganl
							//
							state = next_sig_lr(SIGFN.NORMAL);
							break;
						case SIGFN.SHUNTING:
							//
							//  Need to confirm how these should work.
							//
							state = def_next_state(mainSignal.next_sig_lr(SIGFN.NORMAL));
							break;

						default:
							SetMostRestrictiveAspect();
							break;
					}
				}
				else SetMostRestrictiveAspect();
			}
			else SetMostRestrictiveAspect();
			draw_state = def_draw_state(state);
		}
	} // SignalHead

	public class Signal
	{
		public enum SIGNALSTATE
		{
			STOP,
			CLEAR,
			UNKNOWN
		}

		public enum PERMISSION
		{
			GRANTED,
			DENIED
		}

		private static SignalObject[] signalObjects = null;
		private static Signals signals = null;
		private int nextSigRef = -1;                          // Index to next signal from front TDB. -1 if none.         
		private int rearSigRef = -2;                          // Index to next signal from rear TDB. -1 if none -2 indeterminate.
		private int prevSigRef = -2;                          // Index to Signal behind train. -1 if none -2 indeterminate.

		public Signal(Signals sigNals, SignalObject[] sigObjects, int sigRef)
		{
			nextSigRef = sigRef;
			if (signalObjects == null) signalObjects = sigObjects;
			if (signals == null) signals = sigNals;
		}

		//
		//   This method is invoked if the train has changed direction or the switch ahead has changed ('G' pressed.)
		//   Ensures that the train 'sees' the correct signal.
		//
		public void Reset(TDBTraveller tdbTraveller, bool askPermisiion)
		{
			if (signals != null)
			{
				nextSigRef = signals.FindNextSignal(tdbTraveller);
				SetSignalState(SIGNALSTATE.CLEAR);
				TrackStateChanged();
				rearSigRef = -2;
				prevSigRef = -2;
				if (nextSigRef >= 0 && askPermisiion) signalObjects[nextSigRef].hasPermission = Signal.PERMISSION.GRANTED;
			}
		}

		public void UpdateTrackOcupancy(TDBTraveller rearTDBTraveller)
		{
			if (rearSigRef < -1)
			{
				if (signals != null)
				{
					rearSigRef = signals.FindNextSignal(rearTDBTraveller);
					if ((rearSigRef >= 0) && (rearSigRef != nextSigRef))
					{
						signalObjects[rearSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;  // Train spans signal
					}
				}
			}
			if (prevSigRef < -1)
			{
				if (signals != null)
				{
					prevSigRef = signals.FindPrevSignal(rearTDBTraveller);
					if (prevSigRef >= 0) signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;
				}
			}
			if (rearSigRef >= 0)
			{
				float dist = signalObjects[rearSigRef].DistanceTo(rearTDBTraveller);
				// The rear of the train has passed this signal so set previous signal to BLOCKSTATE.CLEAR
				if (dist <= 0)
				{
					if (prevSigRef >= 0) signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.CLEAR;
					prevSigRef = rearSigRef;
					rearSigRef = signals.FindNextSignal(rearTDBTraveller);
					if ((rearSigRef >= 0) && (rearSigRef != nextSigRef))
					{
						signalObjects[rearSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;  // Train spans signal
					}
				}

			}
		}

		//
		//  Next Signal along the line. Returns -1 if no signal found
		//
		public void NextSignal()
		{
			if (nextSigRef >= 0)
			{
				// Train has entered block controled by this signal
				signalObjects[nextSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;
				signalObjects[nextSigRef].hasPermission = PERMISSION.DENIED;
				nextSigRef = signalObjects[nextSigRef].GetNextSignal();
			}
		} // NextSignal

		//
		//  Returns Distance to next signal from current TDBTraveller position.
		//
		public float DistanceToSignal(TDBTraveller tdbTraverler)
		{
			return nextSigRef >= 0 ? signalObjects[nextSigRef].DistanceTo(tdbTraverler) : 0.01F;
		}  // DistanceToSignal

		//
		//   Returns the signal aspect. Least restricting if Multiple head.
		//
		public SignalHead.SIGASP GetAspect()
		{
			return nextSigRef >= 0 ? signalObjects[nextSigRef].this_sig_lr(SignalHead.SIGFN.NORMAL) : SignalHead.SIGASP.UNKNOWN;
		}

		//
		//   Returns the signal aspect for the track monitor. Least restricting if Multiple head.
		//
		public TrackMonitorSignalAspect GetMonitorAspect()
		{
			return nextSigRef >= 0 ? signalObjects[nextSigRef].GetMonitorAspect() : TrackMonitorSignalAspect.None;
		}

		public void SetSignalState(Signal.SIGNALSTATE state)
		{
			if (nextSigRef >= 0) signalObjects[nextSigRef].SetSignalState(state);
		}

		public void TrackStateChanged()
		{
			if (nextSigRef >= 0) signalObjects[nextSigRef].TrackStateChanged();
		}

		public PERMISSION HasPermissionToProceed()
		{
			if (nextSigRef > 0) return signalObjects[nextSigRef].hasPermission; else return PERMISSION.DENIED;
		}

		//public TDBTraveller FrontTDBTraveler
		//{
		//    set { frontTDBTraveler = value; }
		//}
	}
}
