
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Popups;


// updated 12-2011 by Rob Roeterdink
//
// set flag for environment where SignalGraphs is availabe
// #define SIGNALGRAPHS
//

namespace ORTS
{

  //================================================================================================//
  //
  // class Signals
  //
  //================================================================================================//

        public class Signals
        {

  //================================================================================================//
  // local data
  //================================================================================================//

                private TrackDB trackDB;

                private int[,] visited;
                private SignalObject[] signalObjects;
                private List<SignalWorldObject> SignalWorldList = new List<SignalWorldObject>();
                private Dictionary<uint, SignalRefObject> SignalRefList; 
                public static SIGSCRfile scrfile;

                public int noSignals = 0;
                private int foundSignals = 0;
#if SIGNALGRAPHS
                public SignalGraph SignalGraph = null;
#endif

                private static int updatecount=0;

  //================================================================================================//
  ///
  /// Constructor
  ///

                public Signals(Simulator simulator, SIGCFGFile sigcfg)
                {
                        SignalRefList = new Dictionary <uint, SignalRefObject> ();

                        trackDB = simulator.TDB.TrackDB;

  // read SIGSCR files

                        Trace.Write(" SIGSCR ");
                        scrfile = new SIGSCRfile(simulator.RoutePath, sigcfg.ScriptFiles, sigcfg.SignalTypes);

  // build list of signal world file information

                        BuildSignalWorld(simulator, sigcfg); 

  // build list of signals in TDB file

                        BuildSignalList(simulator.TDB.TrackDB.TrItemTable, simulator.TDB.TrackDB.TrackNodes);

                        if (foundSignals > 0)
                        {

  // Add CFG info

                                AddCFG(sigcfg);

  // Add World info

                                AddWorldInfo();

  // Build Signal Graph

#if SIGNALGRAPHS
                                SignalGraph = new SignalGraph(simulator, this);
#endif
                        }


  //                    for (int isignal=0; isignal < signalObjects.Length-1; isignal++)
  //                    {
  //                            SignalObject singleSignal = signalObjects[isignal];
  //                            if (singleSignal == null)
  //                            {
  //                                    File.AppendAllText(@"SignalObjects.txt","\nInvalid entry : "+isignal.ToString()+"\n");
  //                            }
  //                            else
  //                            {
  //                                    File.AppendAllText(@"SignalObjects.txt","\nSignal ref item     : "+singleSignal.thisRef.ToString()+"\n");
  //                                    File.AppendAllText(@"SignalObjects.txt","Track node + index  : "+singleSignal.trackNode.ToString()+" + "+
  //                                                                                                    singleSignal.trRefIndex.ToString()+"\n");

  //                                    foreach (SignalHead thisHead in singleSignal.SignalHeads)
  //                                    {
  //                                       File.AppendAllText(@"SignalObjects.txt","Type name           : "+thisHead.signalType.Name.ToString()+"\n");
  //                                       File.AppendAllText(@"SignalObjects.txt","Type                : "+thisHead.signalType.FnType.ToString()+"\n");
  //                                       File.AppendAllText(@"SignalObjects.txt","item Index          : "+thisHead.trItemIndex.ToString()+"\n");
  //                                       File.AppendAllText(@"SignalObjects.txt","TDB  Index          : "+thisHead.TDBIndex.ToString()+"\n");
  //                                    }
  //                            }
  //                    }

  //                    foreach (KeyValuePair <string, MSTS.SignalShape> sshape in sigcfg.SignalShapes)
  //                    {
  //                            File.AppendAllText(@"SignalShapes.txt","\n==========================================\n");
  //                            File.AppendAllText(@"SignalShapes.txt","Shape key   : "+sshape.Key.ToString()+"\n");
  //                            MSTS.SignalShape thisshape = sshape.Value;
  //                            File.AppendAllText(@"SignalShapes.txt","Filename    : "+thisshape.ShapeFileName.ToString()+"\n");
  //                            File.AppendAllText(@"SignalShapes.txt","Description : "+thisshape.Description.ToString()+"\n");

  //                            foreach (MSTS.SignalShape.SignalSubObj ssobj in thisshape.SignalSubObjs)
  //                            {
  //                               File.AppendAllText(@"SignalShapes.txt","\nSubobj Index : "+ssobj.Index.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","Matrix       : "+ssobj.MatrixName.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","Description  : "+ssobj.Description.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","Sub Type (I) : "+ssobj.SignalSubType.ToString()+"\n");
  //                               if (ssobj.SignalSubSignalType != null)
  //                               {
  //                                  File.AppendAllText(@"SignalShapes.txt","Sub Type (C) : "+ssobj.SignalSubSignalType.ToString()+"\n");
  //                               }
  //                               else
  //                               {
  //                                  File.AppendAllText(@"SignalShapes.txt","Sub Type (C) : not set \n");
  //                               }
  //                               File.AppendAllText(@"SignalShapes.txt","Optional     : "+ssobj.Optional.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","Default      : "+ssobj.Default.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","BackFacing   : "+ssobj.BackFacing.ToString()+"\n");
  //                               File.AppendAllText(@"SignalShapes.txt","JunctionLink : "+ssobj.JunctionLink.ToString()+"\n");
  //                            }
  //                            File.AppendAllText(@"SignalShapes.txt","\n==========================================\n");
  //                    }

  // Clear world lists to save memory

                        SignalWorldList.Clear();
                        SignalRefList.Clear();

                }

  //================================================================================================//
  ///
  /// Overlay constructor for restore after saved game (empty)
  ///

                public Signals(Simulator simulator, SIGCFGFile sigcfg, BinaryReader inf)
                     : this(simulator, sigcfg)
                {
                }

  //================================================================================================//
  ///
  /// Save game (empty)
  ///

                public void Save(BinaryWriter outf)
                {
                }

  //================================================================================================//
  /// 
  /// Gets an array of all the SignalObjects.
  ///

                internal SignalObject[] SignalObjects
                {
                        get
                        {
                                return signalObjects;
                        }
                }

  //================================================================================================//
  ///
  /// Read all world files to get signal flags
  ///

                private void BuildSignalWorld(Simulator simulator, SIGCFGFile sigcfg)
                {

  // get all filesnames in World directory

                        Trace.Write("\n");
                        string WFilePath = simulator.RoutePath + @"\WORLD\";
                        string [] FileEntries = Directory.GetFiles(WFilePath);

			List<TokenID> Tokens = new List<TokenID> ();
			Tokens.Add(TokenID.Signal);

  // loop through files, use only extention .w, skip w+1000000+1000000.w file

                        foreach(string fileName in FileEntries)
                        {
                                string [] fparts = fileName.Split('.');
                                string [] fparts2= fparts[fparts.Length-2].Split('\\');

  // check if valid file

				bool validFile = true;

				try
				{
		    			int p = fileName.ToUpper().LastIndexOf("\\WORLD\\W");
		    			int TileX = int.Parse(fileName.Substring(p + 8, 7));
					int TileZ = int.Parse(fileName.Substring(p + 15, 7));
				}
				catch (Exception)
				{
					validFile = false;
				}

                                if (string.Compare(fparts[fparts.Length-1], "w") == 0 && validFile)
                                {

  // read w-file, get SignalObjects only

                                        Trace.Write("W");
                                        WFile WFile = new WFile(fileName, Tokens);

  // loop through all signals

                                        foreach (WorldObject worldObject in WFile.Tr_Worldfile)
                                        {
                                                if (worldObject.GetType() == typeof(MSTS.SignalObj))
                                                {
                                                        MSTS.SignalObj thisWorldObject = (MSTS.SignalObj) worldObject;
                                                        SignalWorldObject SignalWorldSignal = new SignalWorldObject(thisWorldObject,sigcfg);
                                                        SignalWorldList.Add(SignalWorldSignal);
                                                        foreach ( KeyValuePair <uint, uint> thisref in SignalWorldSignal.HeadReference)
                                                        {
                                                                int thisSignalCount = SignalWorldList.Count()-1;    // Index starts at 0
                                                                SignalRefObject thisRefObject = new SignalRefObject(thisSignalCount,thisref.Value);
                                                                if (SignalRefList.ContainsKey(thisref.Key))
                                                                {
                                                                        SignalRefObject DoubleObject = SignalRefList[thisref.Key];
                                                                        Trace.TraceWarning("Double key : {0} for heads {1} and {2} in {3}-{4}",
                                                                                        thisref.Key,thisRefObject.HeadIndex,DoubleObject.HeadIndex,
                                                                                        WFile.TileX.ToString(),WFile.TileZ.ToString());
                                                                }
                                                                else
                                                                {
                                                                        SignalRefList.Add(thisref.Key,thisRefObject);
                                                                }
                                                        }
                                                }
                                        }

  // clear worldfile info
 
                                        WFile=null;
                                }
                        }
                        Trace.Write("\n");

  //                    foreach ( KeyValuePair <uint, SignalRefObject> thisref in SignalRefList)
  //                    {
  //                        uint headref;
  //                            uint TBDRef = thisref.Key;
  //                            SignalRefObject signalRef = thisref.Value;

  //                            SignalWorldObject reffedObject = SignalWorldList[(int) signalRef.SignalWorldIndex];
  //                            if ( !reffedObject.HeadReference.TryGetValue(TBDRef, out headref))
  //                            {
  //                                    File.AppendAllText(@"WorldSignalList.txt","Incorrect Ref : "+TBDRef.ToString()+"\n");
  //                                    foreach ( KeyValuePair <uint, uint> headindex in reffedObject.HeadReference)
  //                                    {
  //                                            File.AppendAllText(@"WorldSignalList.txt","TDB : "+headindex.Key.ToString()+
  //                                                            " + "+headindex.Value.ToString()+"\n");
  //                                    }
  //                            }
  //                    }

                }  //BuildSignalWorld


  //================================================================================================//
  /// 
  /// Update : perform signal updates
  /// 

                public void Update(float elapsedClockSeconds)
                {
                        if (foundSignals > 0)
                        {
#if SIGNALGRAPHS
                                SignalGraph.UpdateJunctionSignals();
#endif

  // loop through all signals
  // update required part

                                int totalSignal = signalObjects.Length - 1;
                                int updatestep  = (totalSignal/20)+1;
                                for (int icount = updatecount; icount < Math.Min(totalSignal, updatecount+updatestep); icount++)
                                {
                                        SignalObject signal = signalObjects[icount];
                                        if (signal != null) // to cater for orphans. RE bug!
                                        {
                                                signal.Update();
                                        }
                                }

                                updatecount += updatestep;
                                updatecount = updatecount > totalSignal ? 0 : updatecount;
                        }
                }  //Update

  //================================================================================================//
  ///
  /// Build signal list from TDB
  ///

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

  //================================================================================================//
  //
  //  ScanPath : This method follows the track path to find any signal objects along it.
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
                                                                        int TDBRef = trackNodes[index].TrVectorNode.TrItemRefs[i];
                                                                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                                                        {
                                                                                SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                                                                if ((int)sigItem.revDir == direction)
                                                                                {
                                                                                        sigItem.sigObj = foundSignals;

                                                                                        if (sigItem.noSigDirs > 0)
                                                                                        {
                                                                                        SignalItem.strTrSignalDir sigTrSignalDirs = sigItem.TrSignalDirs[0];
                                                                                        }

                                                                                        lastSignal = AddSignal(index, i, sigItem, lastSignal, TrItems, trackNodes, TDBRef);
                                                                                        sigItem.sigObj = lastSignal;
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
                                                                        int TDBRef = trackNodes[index].TrVectorNode.TrItemRefs[i];
                                                                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                                                        {
                                                                                SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                                                                if ((int)sigItem.revDir == direction)
                                                                                {
                                                                                        sigItem.sigObj = foundSignals;

                                                                                        if (sigItem.noSigDirs > 0)
                                                                                        {
                                                                                        SignalItem.strTrSignalDir sigTrSignalDirs = sigItem.TrSignalDirs[0];
                                                                                        }

                                                                                        lastSignal = AddSignal(index, i, sigItem, lastSignal, TrItems, trackNodes, TDBRef);
                                                                                        sigItem.sigObj = lastSignal;
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
                                                                ScanPath(trackNodes[index].TrPins[i + trackNodes[index].Inpins].Link,
                                                                         trackNodes[index].TrPins[i + trackNodes[index].Inpins].Direction, TrItems, trackNodes);
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

  //================================================================================================//
  ///
  /// This method adds a new Signal to the list
  ///

                private int AddSignal(int trackNode, int nodeIndx, SignalItem sigItem, int prevSignal, TrItem[] TrItems, TrackNode[] trackNodes, int TDBRef)
                {
                        if (prevSignal >= 0)
                        {
                                if (signalObjects[prevSignal].isSignalHead((SignalItem)TrItems[trackNodes[trackNode].TrVectorNode.TrItemRefs[nodeIndx]]))
                                {
                                        signalObjects[prevSignal].AddHead(nodeIndx, TDBRef, sigItem);
                                        return prevSignal;
                                }
                        }
                        signalObjects[foundSignals] = new SignalObject();
                        signalObjects[foundSignals].direction = (int) sigItem.Direction;
                        signalObjects[foundSignals].trackNode = trackNode;
                        signalObjects[foundSignals].trRefIndex = nodeIndx;
                        signalObjects[foundSignals].prevSignal = prevSignal;
                        signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, sigItem);
                        signalObjects[foundSignals].thisRef = foundSignals;

                        signalObjects[foundSignals].WorldObject = null;
  //if (prevSignal >= 0) signalObjects[prevSignal].nextSignal = foundSignals;
                        foundSignals++;
                        return foundSignals - 1;
                } // AddSignal

  //================================================================================================//
  /// 
  ///  This method returns the index of the next signal along the set path. -1 if no signal found
  /// 

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
                } //FindNextSignal

  //================================================================================================//
  //
  // NextNode : find next junction node in path
  //

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

  //================================================================================================//
  //
  //      AddCFG : This method adds the sigcfg reference to each signal object.
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
                }//AddCFG

  //================================================================================================//
  //
  //     AddWorldInfo : add info from signal world objects to signal
  //

                private void AddWorldInfo()
                {

  // loop through all signal and all heads

                        foreach (SignalObject signal in signalObjects)
                        {
                                if (signal != null)
                                {
                                        foreach (SignalHead head in signal.SignalHeads)
                                        {

  // get reference using TDB index from head

                                                uint TDBRef = Convert.ToUInt32(head.TDBIndex);
                                                SignalRefObject thisRef;

                                                if (SignalRefList.TryGetValue(TDBRef, out thisRef))
                                                {
                                                        uint signalIndex = thisRef.SignalWorldIndex;
                                                        if (signal.WorldObject == null)
                                                        {
                                                                 signal.WorldObject = SignalWorldList[(int) signalIndex];
                                                        }
                                                        SignalRefList.Remove(TDBRef);
                                                }
                                        }
                                }
                        }

 // check if any signals have been missed

                }//AddWorldInfo


  //================================================================================================//
  /// 
  /// This method resets junction signals to indeterminate state
  /// 
  // #TODO# : make this more specific
  
                public void ResetJunction()
                {
                        foreach (SignalObject signal in signalObjects)
                        {
                                if (signal != null)
                                {
                                        if (signal.isJunction) signal.nextSignal = -2;
                                }
                        }
                }//ResetJunction

  //================================================================================================//
  /// 
  /// Get index of next (NORMAL)signal to the current point in the tdbtraveller
  /// Returns -1 if one cannot be found.
  ///
  //

                public int FindNextSignal(TDBTraveller tdbtraveller)
                {
                        return FindSignal(tdbtraveller, tdbtraveller.Direction);
                }//FindNextSignal

  //================================================================================================//
  ///
  // Get signal object of nearest signal in direction of travel
  ///
  /// 
                public Signal FindNearestSignal(TDBTraveller tdbtraveller)
                {
                        int sigRef = FindNextSignal(tdbtraveller);
                        return new Signal(this, signalObjects, sigRef);
                }//FindNearestSignal

  //================================================================================================//
  ///
  //  Get index of previous signal in direction of travel
  ///

                public int FindPrevSignal(TDBTraveller tdbtraveller)
                {
                        TDBTraveller revTDBtraveller = new TDBTraveller(tdbtraveller);
                        revTDBtraveller.ReverseDirection();
                        int direction = tdbtraveller.Direction;
                        return FindSignal(revTDBtraveller, direction);
                }//FindPrevSignal

  //================================================================================================//
  ///
  ///  Find signal index
  ///

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
                                                                        {
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
                                                }

                                                if (sigIndex >= 0) return sigIndex; // Signal found in this node.
                                        }

                                }
                                NextNode(trackNodes, ref currenNode, ref currDir);
                                if (currenNode == startNode) return -1; // back to where we started !
                        } while (true);

                } //FindSignal

  //================================================================================================//
  ///
  //  FindByTrItem : find required signalObj + signalHead
  ///

                public KeyValuePair<SignalObject, SignalHead>? FindByTrItem(uint trItem)
                {
                        foreach (var signal in signalObjects)
                                if (signal != null)
                                        foreach (var head in signal.SignalHeads)
                                        {
                                                int tempint = SignalObject.trackNodes[signal.trackNode].TrVectorNode.TrItemRefs[head.trItemIndex];
                                                if (SignalObject.trackNodes[signal.trackNode].TrVectorNode.TrItemRefs[head.trItemIndex] == (int)trItem)
                                                        return new KeyValuePair<SignalObject, SignalHead>(signal, head);
                                        }
                        return null;
                }
        }//FindByTrItem

  //================================================================================================//
  //
  //  class SignalObject
  //
  //================================================================================================//

        public class SignalObject
        {

                public enum BLOCKSTATE
                {
                        CLEAR,                                        // Block ahead is clear
                        OCCUPIED,                                // Block ahead is occupied by one or more wagons/locos
                        JN_OBSTRUCTED                // Block ahead is impassable due to the state of a switch
                }

                public static SignalObject[] signalObjects;
                public static TrackNode[] trackNodes;
                public static TrItem[] trItems;
                public List<SignalHead> SignalHeads = new List<SignalHead>();
                public int trackNode;                   // Track node which contains this signal
                public int nextNode;                    // Next Track node which follows this signal
                public int trRefIndex;                  // Index to TrItemRef within Track Node 
                public int thisRef;                     // This signal's reference.
                public int direction;                   // Direction facing on track
                public int draw_state;
                public bool enabled = true;
                public bool isJunction = false;         // Indicates whether the signal controls a junction.
                public bool canUpdate = true;           // Signal can be updated automatically
                public bool isAuto = true;
                public bool useScript = false;
                public BLOCKSTATE blockState = BLOCKSTATE.CLEAR;
                public Signal.PERMISSION hasPermission = Signal.PERMISSION.DENIED;  // Permission to pass red signal
                public int nextSignal = -2;             // Index to next signal. -1 if none -2 indeterminate
                public int prevSignal = -2;             // Index to previous signal -1 if none -2 indeterminate
                public SignalWorldObject WorldObject;   // Signal World Object information
                public int [] sigfound = new int [(int) SignalHead.SIGFN.UNKNOWN];

  //================================================================================================//
  ///
  //  revDir : reverse direction
  //  Needed because signal faces train!
  ///
                public int revDir
                {
                        get { return direction == 0 ? 1 : 0; }
                }//revDir

  //================================================================================================//
  ///
  // BLOCKSTATE : get blockstate
  ///

                public BLOCKSTATE block_state()
                {
                        return blockState;
                }//BLOCKSTATE

  //================================================================================================//
  ///
  // isSignalNormal : Returns true if at least one signal head is type normal.
  ///

                public bool isSignalNormal()
                {
                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (sigHead.sigFunction == SignalHead.SIGFN.NORMAL) return true;
                        }
                        return false;
                }

  //================================================================================================//
  ///
  // isSignalType : Returns true if at least one signal head is of required type
  ///

                public bool isSignalType(SignalHead.SIGFN [] reqSIGFN)
                {
                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (reqSIGFN.Contains(sigHead.sigFunction)) return true;
                        }
                        return false;
                }

  //================================================================================================//
  ///
  // next_sig_mr : returns most restrictive state of next signal of required type
  ///
  ///

                public SignalHead.SIGASP next_sig_mr(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = fn_type;

                        nextSignal = sigfound[(int) fn_type];
                        if (nextSignal < 0)
                        {
                                nextSignal = SONextSignal(fn_type_array);
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

  //================================================================================================//
  ///
  // next_sig_lr : returns least restrictive state of next signal of required type
  ///
  ///

                public SignalHead.SIGASP next_sig_lr(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = fn_type;

                        nextSignal = sigfound[(int) fn_type];
                        if (nextSignal < 0)
                        {
                                nextSignal = SONextSignal(fn_type_array);
                                sigfound[(int) fn_type] = nextSignal;
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

  //================================================================================================//
  ///
  //  NextSignal : Finds the next signal down the line from this one.
  //  Returns -1 if one cannot be found.
  ///
  /// #TODO# : limit search to reserved section

                private int SONextSignal(SignalHead.SIGFN [] fn_type)
                {
                        int currentTrackNode = trackNode;
                        int currentIndex = trRefIndex;
                        int currentDir = this.revDir;
			SignalObject thisSignal;

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
  // Not a signal head for this signal
                                                                if ((signalObjects[sigIndex] != null) && (signalObjects[sigIndex].thisRef != thisRef))
                                                                {
                                                                        if (signalObjects[sigIndex].isSignalType(fn_type) &&
                                                                            signalObjects[sigIndex].revDir == currentDir)
                                                                            return sigIndex;
                                                                }
                                                        }
                                                }
                                        }
                                        else
                                        {
                                                while (--currentIndex >= 0)
                                                {
                                                        int index = trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[currentIndex];
                                                        if (trItems[index].ItemType == TrItem.trItemType.trSIGNAL)
                                                        {
                                                                SignalItem signalItem = (SignalItem)trItems[index];
                                                                int sigIndex = signalItem.sigObj;

								thisSignal = null;
								if (sigIndex > 0 && sigIndex < signalObjects.Length)
								{
									thisSignal = signalObjects[sigIndex];
								}
  // Not a signal head for this signal
                                                                if ((thisSignal != null) && (thisSignal.thisRef != thisRef))
                                                                {
                                                                        if (thisSignal.isSignalType(fn_type) &&
                                                                            thisSignal.revDir == currentDir)
                                                                            return sigIndex;
                                                                }
                                                        }
                                                }
                                        }
                                }
                        }

  // Look for signal in next tracknodes
  // #TODO# : limit to end of reservation (perhaps as parameter)
  //          this routine may look 'through' signal for other train

                        do
                        {
  // Get the next track node
  // End of track reached
                                if (trackNodes[currentTrackNode].TrEndNode) return -1;  

                                NextNode(ref currentTrackNode, ref currentDir);

  // check for broken database or looped route
                                if (currentTrackNode == 0) return -1;  // End of track reached (broken track database?)
                                if (currentTrackNode == trackNode) 
                                {
                                        Trace.TraceWarning("Next signal looped : "+currentTrackNode.ToString()+"+"+currentDir.ToString()+"\n");
                                        return -1;  // Back to start node again !!
                                }


  // If new track node is a vector look for signal object
                                if (trackNodes[currentTrackNode].TrVectorNode != null)
                                {
                                        if (trackNodes[currentTrackNode].TrVectorNode.noItemRefs > 0)
                                        {
                                                if (currentDir == 1)
                                                {
                                                        for (int i = 0; i < trackNodes[currentTrackNode].TrVectorNode.noItemRefs; i++)
                                                        {
                                                                int trItemref = trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i];
  // Track Item is signal
                                                                if (trItems[trItemref].ItemType == TrItem.trItemType.trSIGNAL)
                                                                {
                                                                        SignalItem signalItem = (SignalItem)trItems[trItemref];
                                                                        int sigIndex = signalItem.sigObj;
                                                                        if ((int)signalItem.revDir == currentDir)
                                                                        {
                                                                                if (signalObjects[sigIndex].isSignalType(fn_type)) return sigIndex;
                                                                        }
                                                                }
                                                        }
                                                }
                                                else
                                                {
                                                        for (int i = trackNodes[currentTrackNode].TrVectorNode.noItemRefs - 1; i >= 0; i--)
                                                        {
                                                                int trItemref = trackNodes[currentTrackNode].TrVectorNode.TrItemRefs[i];
  // Track Item is signal
                                                                if (trItems[trItemref].ItemType == TrItem.trItemType.trSIGNAL)
                                                                {
                                                                        SignalItem signalItem = (SignalItem)trItems[trItemref];
                                                                        int sigIndex = signalItem.sigObj;
                                                                        if ((int)signalItem.revDir == currentDir)
                                                                        {
                                                                                if (signalObjects[sigIndex].isSignalType(fn_type)) return sigIndex;
                                                                        }
                                                                }
                                                        }
                                                }
                                        }
                                }

                        } while (true);
                }//NextSignal

  //================================================================================================//
  //
  // NextNode : Returns the next node and direction in the TDB
  //

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
                }//NextNode

  //================================================================================================//
  //
  // opp_sig_mr : not yet implemented
  //

                public SignalHead.SIGASP opp_sig_mr(SignalHead.SIGFN fn_type)
                {
                        return SignalHead.SIGASP.STOP;
                }//opp_sig_mr

  //================================================================================================//
  //
  // opp_sig_lr : not yet implemented
  //

                public SignalHead.SIGASP opp_sig_lr(SignalHead.SIGFN fn_type)
                {
                        return SignalHead.SIGASP.STOP;
                }//opp_sig_lr

  //================================================================================================//
  //
  // this_sig_mr : Returns the most restrictive state of this signal's heads of required type
  //

                public SignalHead.SIGASP this_sig_mr(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGASP sigAsp = SignalHead.SIGASP.UNKNOWN;
                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (sigHead.sigFunction == fn_type && sigHead.state < sigAsp)
                                {
                                        sigAsp = sigHead.state;
                                }
                        }
                        if (sigAsp == SignalHead.SIGASP.UNKNOWN)
			{
				return SignalHead.SIGASP.STOP;
			}
			else
			{
				return sigAsp;
			}
                }//this_sig_mr

  //================================================================================================//
  //
  // this_sig_lr : Returns the least restrictive state of this signal's heads of required type
  //

                public SignalHead.SIGASP this_sig_lr(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGASP sigAsp = SignalHead.SIGASP.STOP;
			bool sigAspSet = false;
                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (sigHead.sigFunction == fn_type && sigHead.state >= sigAsp)
                                {
                                        sigAsp = sigHead.state;
					sigAspSet = true;
                                }
                        }
                        if (sigAspSet)
			{
				return sigAsp;
			}
			else if (fn_type == SignalHead.SIGFN.NORMAL)
			{
				return SignalHead.SIGASP.CLEAR_2;
			}
			else
			{
				return SignalHead.SIGASP.STOP;
			}
                }//this_sig_lr

  //================================================================================================//
  //
  // route_set : check if required route is set
  //

                public bool route_set (int req_mainnode)
                {
                        int thisreservid = Dispatcher.Reservations[trackNode];
                        int nextreservid = Dispatcher.Reservations[req_mainnode];

                        return (thisreservid >= 0 && thisreservid == nextreservid);
                }

  //================================================================================================//
  //
  // Update : Perform the update for each head on this signal.
  //

                public void Update()
                {
                        if (canUpdate)
                        {

  // clear next signal flags

                                for (int isig = 0 ; isig < sigfound.Length; isig++)
                                {
                                        sigfound[isig] = -1;
                                }

  // get next normal signal

				int nextreservid = -2;
                                int nextSignal = GetNextSignal();

				if (nextSignal > 0)
				{
					SignalObject nextSignalObject = signalObjects[nextSignal];
					sigfound[(int) SignalHead.SIGFN.NORMAL]=nextSignal;

					int nextNode   = nextSignalObject.trackNode;
					nextreservid = Dispatcher.Reservations[nextNode];
				}

  // set enabled

                        	int thisreservid = Dispatcher.Reservations[trackNode];
				if (thisreservid >= 0)
				{
					enabled = ( (thisreservid == nextreservid) || (nextreservid < 0) );
				}
				else
				{
					enabled = false;
				}

  // update all heads

                                foreach (SignalHead sigHead in SignalHeads)
                                {
                                        sigHead.Update();
                                }
                        }

                } // Update

  //================================================================================================//
  //
  // DistanceTo : Returns the distance from the TDBtraveller to this signal. 
  //

                public float DistanceTo(TDBTraveller tdbTraveller)
                {
                        int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
                        return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
                }//DistanceTo

  //================================================================================================//
  //
  // isSignalHead : Check Whether signal head is for this signal.
  // #TODO# : check if world ItemRef can be used - safer option
  //

                public bool isSignalHead(SignalItem signalItem)
                {
  // Tritem for this signal
                        SignalItem thisSignalItem = (SignalItem)trItems[this.trItem];
  // Same Tile
                        if (signalItem.TileX == thisSignalItem.TileX && signalItem.TileZ == thisSignalItem.TileZ)
                        {
  // Same position
                                if ((Math.Abs(signalItem.X - thisSignalItem.X) < 0.01) &&
                                    (Math.Abs(signalItem.Y - thisSignalItem.Y) < 0.01) &&
                                    (Math.Abs(signalItem.Z - thisSignalItem.Z) < 0.01))
                                {
                                        return true;
                                }
                        }
                        return false;
                }//isSignalHead

  //================================================================================================//
  //
  // AddHead : Adds a head to this signal.
  //

                public void AddHead(int trItem, int TDBRef, SignalItem sigItem)
                {
                        SignalHead head = new SignalHead(this, trItem, TDBRef, sigItem);
			if (head.TrackJunctionNode != 0)
			{
                                if (head.JunctionPath == 0)
                                {
                                    head.JunctionMainNode =
                                       trackNodes[head.TrackJunctionNode].TrPins[trackNodes[head.TrackJunctionNode].Inpins].Link;
                                }
                                else
                                {
                                    head.JunctionMainNode =
                                       trackNodes[head.TrackJunctionNode].TrPins[trackNodes[head.TrackJunctionNode].Inpins + 1].Link;
                                }
			}
                        SignalHeads.Add(head);
                }//AddHead

  //================================================================================================//
  //
  // trItem : Gets the correspnding TrItem from the TDB.
  //

                public int trItem
                {
                        get
                        {
                                return trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
                        }
                }//trItem

  //================================================================================================//
  //
  // SetSignalType : Sets the signal type from the sigcfg file for each signal head.
  //

                public void SetSignalType(SIGCFGFile sigCFG)
                {
                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                sigHead.SetSignalType(trItems, sigCFG);
                        }
                }//SetSignalType

  //================================================================================================//
  //
  // SetSignalState : set state 
  //

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

  //================================================================================================//
  //
  // GetNextSignal : get next NORMAL signal (for train aspect)
  //

                public int GetNextSignal()
                {
                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = SignalHead.SIGFN.NORMAL;
                        nextSignal = SONextSignal(fn_type_array);
                        return nextSignal;
                }//GetNextSignal

  //================================================================================================//
  //
  // ??????
  // #TODO# : check intention of this routine !!!
  //

                public void TrackStateChanged()
                {
                        //if(isJunction) nextSignal=-2;
                        nextSignal = -2;
                }//TrackStateChanged

  //================================================================================================//
  //
  // TrackMonitorSignalAspect : Gets the display aspect for the track monitor.
  //

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
                                        return TrackMonitorSignalAspect.Warning;
                                case SignalHead.SIGASP.CLEAR_1:
                                case SignalHead.SIGASP.CLEAR_2:
                                        return TrackMonitorSignalAspect.Clear;
                                default:
                                        return TrackMonitorSignalAspect.None;
                        }
                } // GetMonitorAspect

        }  // SignalObject

  //================================================================================================//
  //
  // class SignalHead
  //
  //================================================================================================//

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
                        CLEAR_1,
                        CLEAR_2,
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
                public uint TrackJunctionNode;          // Track Junction Node (= 0 if not set)
                public uint JunctionPath;               // Required Junction Path
                public int JunctionMainNode;            // Main node following junction
                public int TDBIndex;                    // Index to TDB Signal Item


                public SignalObject mainSignal;        //  This is the signal which this head forms a part.

  //================================================================================================//
  //
  // Constructor
  //

                public SignalHead(SignalObject sigOoject, int trItem, int TDBRef, SignalItem sigItem)
                {
                        mainSignal = sigOoject;
                        trItemIndex = trItem;
                        TDBIndex = TDBRef;
			draw_state = 0;

                        TrackJunctionNode = 0;
			JunctionMainNode  = 0;

                        if (sigItem.noSigDirs > 0)
                        {
                                TrackJunctionNode = sigItem.TrSignalDirs[0].TrackNode;
                                JunctionPath = sigItem.TrSignalDirs[0].linkLRPath;
                        }
                }

  //================================================================================================//
  //
  // SetSignalType : This method sets the signal type object from the CIGCFG file
  //

                public void SetSignalType(TrItem[] TrItems, SIGCFGFile sigCFG)
                {
                        SignalItem sigItem = (SignalItem)TrItems[TDBIndex];

                        if (sigCFG.SignalTypes.ContainsKey(sigItem.SignalType))
                        {
                                signalType = sigCFG.SignalTypes[sigItem.SignalType];
                        }
                        else
                        {
                                Trace.TraceWarning("SignalObject trItem={0}, trackNode={1} has SignalHead with undefined SignalType {2}.",
                                                  mainSignal.trItem, mainSignal.trackNode, sigItem.SignalType);
                        }
                }//SetSignalType

  //================================================================================================//
  //
  // sigfunction : returns signal type
  //

                public SIGFN sigFunction
                {
                        get
                        {
                                if (signalType != null) return (SIGFN)signalType.FnType; else return SIGFN.UNKNOWN;
                        }
                }//sigfunction

  //================================================================================================//
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

  //================================================================================================//
  //
  //  Following methods used in scipting
  //

                public SIGASP next_sig_mr(SIGFN sigFN)
                {
                        return mainSignal.next_sig_mr(sigFN);
                }

                public SIGASP next_sig_lr(SIGFN sigFN)
                {
                        return mainSignal.next_sig_lr(sigFN);
                }

                public SIGASP this_sig_lr(SIGFN sigFN)
                {
                        return mainSignal.this_sig_lr(sigFN);
                }

                public SIGASP this_sig_mr(SIGFN sigFN)
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

  //================================================================================================//
  //
  //  dist_multi_sig_mr : Returns most restrictive state of signal type A, for all type A upto type B
  //  #TODO# : write code
  //  
  //

                public SIGASP dist_multi_sig_mr(SIGFN sigFN1, SIGFN sigFN2)
                {
                        return SIGASP.CLEAR_2;
                }

  //================================================================================================//
  //
  //  sig_feature : return state of requested feature through signal head flags
  //  
  //

                public bool sig_feature(int feature)
                {
                        bool flag_value = true;

                        if (mainSignal.WorldObject != null)
                        {
                                if (feature < mainSignal.WorldObject.FlagsSet.Length)
                                {
                                        flag_value = mainSignal.WorldObject.FlagsSet[feature];
                                }
                        }

                        return flag_value;
                }

  //================================================================================================//
  //
  //  def_draw_state : Returns the default draw state for this signal head from the SIGCFG file
  //  Retruns -1 id no draw state.
  //

                public int def_draw_state(SIGASP state)
                {
                        if (signalType != null) return signalType.def_draw_state(state); else return -1;
                }//def_draw_state

  //================================================================================================//
  //
  //  SetMostRestrictiveAspect : Sets the state to the most restrictive aspect for this head.
  //

                public void SetMostRestrictiveAspect()
                {
                        if (signalType != null) state = signalType.GetMostRestrictiveAspect(); else state = SignalHead.SIGASP.STOP;
			def_draw_state(state);
                }//SetMostRestrictiveAspect

  //================================================================================================//
  //
  //  SetLeastRestrictiveAspect : Sets the state to the least restrictive aspect for this head.
  //

                public void SetLeastRestrictiveAspect()
                {
                        if (signalType != null) state = signalType.GetLeastRestrictiveAspect(); else state = SignalHead.SIGASP.CLEAR_2;
			def_draw_state(state);
                }//SetLeastRestrictiveAspect

  //================================================================================================//
  //
  //  route_set : check if linked route is set
  //

                public int route_set()
                {
                        bool juncfound = true;

  // call route_set routine from main signal

                        if (TrackJunctionNode > 0)
                        {
                                juncfound = mainSignal.route_set(JunctionMainNode);
                        }

                        if (juncfound)
                        {
                                return 1;
                        }
                        else
                        {
                                return 0;
                        }
                }//route_set

  //================================================================================================//
  //
  //  Default update process
  //

                public void Update()
                {
                        SIGASP oldstate;
                        SignalObject.BLOCKSTATE block;
                        StringBuilder s = new StringBuilder();
                        
                        oldstate = state;

                        SIGSCRfile.SH_update(this, Signals.scrfile);

                        if (state != oldstate && state == SIGASP.STOP)
                        {
                                block=mainSignal.block_state();
                        }
                }
        } //Update

  //================================================================================================//
  //
  // class Signal
  //
  //================================================================================================//

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

                public static SignalObject[] signalObjects = null;
                public static Signals signals = null;
                public int nextSigRef = -1;                          // Index to next signal from front TDB. -1 if none.         
                public int rearSigRef = -2;                          // Index to next signal from rear TDB. -1 if none -2 indeterminate.
                public int prevSigRef = -2;                          // Index to Signal behind train. -1 if none -2 indeterminate.

  //================================================================================================//
  //
  // Constructor
  //

                public Signal(Signals sigNals, SignalObject[] sigObjects, int sigRef)
                {
                        nextSigRef = sigRef;
                        if (signalObjects == null) signalObjects = sigObjects;
                        if (signals == null) signals = sigNals;
                }

  //================================================================================================//
  //
  //   Reset : This method is invoked if the train has changed direction or the switch ahead has changed ('G' pressed.)
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
#if SIGNALGRAPHS
                                signals.SignalGraph.UpdateSignals();
#endif
                        }
                }//Reset

  //================================================================================================//
  //
  // UpdateTrackOccupancy : update track state
  //

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
                }//UpdateTrackOcupancy

  //================================================================================================//
  //
  //  NextSignal : Next NORMAL Signal along the line. Returns -1 if no signal found
  //

                public void SNextSignal()
                {
                        if (nextSigRef >= 0)
                        {
                                int TDBRef = signalObjects[nextSigRef].SignalHeads[0].TDBIndex;
                                signalObjects[nextSigRef].hasPermission = PERMISSION.DENIED;
                                nextSigRef = signalObjects[nextSigRef].GetNextSignal();
                        }
                } // NextSignal

  //================================================================================================//
  //
  //  DistanceToSignal: Returns Distance to next NORMAL signal from current TDBTraveller position.
  //

                public float DistanceToSignal(TDBTraveller tdbTraverler)
                {
                        return nextSigRef >= 0 ? signalObjects[nextSigRef].DistanceTo(tdbTraverler) : 0.01F;
                }  // DistanceToSignal

  //================================================================================================//
  //
  //   GetAspect : Returns the signal aspect. Least restricting if Multiple head.
  //

                public SignalHead.SIGASP GetAspect()
                {
                        return nextSigRef >= 0 ? signalObjects[nextSigRef].this_sig_lr(SignalHead.SIGFN.NORMAL) : SignalHead.SIGASP.UNKNOWN;
                }//GetAspect

  //================================================================================================//
  //
  //   GetMonitorAspect : Returns the signal aspect for the track monitor. Least restricting if Multiple head.
  //
                public TrackMonitorSignalAspect GetMonitorAspect()
                {
                        return nextSigRef >= 0 ? signalObjects[nextSigRef].GetMonitorAspect() : TrackMonitorSignalAspect.None;
                }//GetMonitorAspect

  //================================================================================================//
  //
  //   SetSignalState : set state of signal
  //

                public void SetSignalState(Signal.SIGNALSTATE state)
                {
                        if (nextSigRef >= 0) signalObjects[nextSigRef].SetSignalState(state);
                }

  //================================================================================================//
  //
  //  TrackStateChanged : set action on track state change
  //

                public void TrackStateChanged()
                {
                        if (nextSigRef >= 0) signalObjects[nextSigRef].TrackStateChanged();
                }

  //================================================================================================//
  //
  //  HasPermissionToProceed : manual permission allowed
  //

                public PERMISSION HasPermissionToProceed()
                {
                        if (nextSigRef > 0) return signalObjects[nextSigRef].hasPermission; else return PERMISSION.DENIED;
                }

#if SIGNALGRAPHS
  //================================================================================================//
  // Methods for SignalGraph
  //

                private Train Train = null;
                private SignalGraphLocation FrontSGL = null;
                private SignalGraphLocation RearSGL = null;
                private float TrainLength = 0;

  //================================================================================================//
  //
  // SetTrackOccupied : Initiallizes the signal system track occupancy information for the specified train.
  //                    Used for SignalGraph
  //
  // <param name="train"></param>
  //

                public void SetTrackOccupied(Train train)
                {
                    if (signals == null || signals.SignalGraph == null) return;
  
                    Train = train;
                    RearSGL = signals.SignalGraph.FindLocation(train.RearTDBTraveller);
                    FrontSGL = new SignalGraphLocation(RearSGL);
                    FrontSGL.ChangeOccupancy(1);
                    FrontSGL.Move(train.Length, 1);
                    TrainLength = train.Length;
                }

  //================================================================================================//
  //
  // ClearTrackOccupied : Removes a train from the signal system track occupancy information.
  //

                public void ClearTrackOccupied()
                {
                    if (Train == null)
                    return;
                    RearSGL.Move(TrainLength, -1);
                    RearSGL.ChangeOccupancy(-1);
                    Train = null;
                }

  //================================================================================================//
  //
  // UpdateTrackOccupance : Updates the signal system track occupancy information for a train whose rear traveller has moved distanceM meters forward.
  // The train length change is used to determine the distance the forward end of the train has moved.
  //
  // <param name="distanceM"></param>

                public void UpdateTrackOccupancy(float distanceM)
                {
                    if (Train == null || distanceM == 0)
                    return;
                    float dl = Train.Length - TrainLength;
                    TrainLength = Train.Length;
                    FrontSGL.Move(distanceM + dl, 1);
                    RearSGL.Move(distanceM, -1);
                }

#endif

        }

  //================================================================================================//
  //
  // class SignalInfo
  //
  //================================================================================================//

        public class SignalRefObject
        {
                public uint SignalWorldIndex;
                public uint HeadIndex;

  //================================================================================================//
  //
  // Constructor
  //

                public SignalRefObject(int WorldIndexIn, uint HeadItemIn)
                {
                        SignalWorldIndex = Convert.ToUInt32(WorldIndexIn);
                        HeadIndex        = HeadItemIn;
                }
        }
        
  //================================================================================================//
  //
  // class SignalWorldInfo
  //
  //================================================================================================//

        public class SignalWorldObject
        {
                public string SFileName;
                public Dictionary <uint, uint> HeadReference;     // key=TDBIndex, value=headindex
                public bool [] HeadsSet;                          // Flags heads which are set
                public bool [] FlagsSet;                          // Flags signal-flags which are set

  //================================================================================================//
  //
  // Constructor
  //

                public SignalWorldObject(MSTS.SignalObj SignalWorldItem,SIGCFGFile sigcfg)
                {
                        MSTS.SignalShape thisCFGShape;

                        HeadReference = new Dictionary <uint, uint>();

  // set flags with length to number of possible SubObjects type

                        FlagsSet = new bool [MSTS.SignalShape.SignalSubObj.SignalSubTypes.Count];
                        for (uint iFlag = 0; iFlag < FlagsSet.Length; iFlag++)
                        {
                                FlagsSet[iFlag] = false;
                        }

  // get filename in Uppercase

                        SFileName = SignalWorldItem.FileName.ToUpper();

  // search defined shapes in SIGCFG to find signal definition

                        if (sigcfg.SignalShapes.TryGetValue(SFileName, out thisCFGShape))
                        {

  // set array length to actual no. of heade

                                foreach (MSTS.SignalUnit signalUnitInfo in SignalWorldItem.SignalUnits.Units)
                                {
                                        uint TrItemRef = signalUnitInfo.TrItem;
                                        if (TrItemRef == 1511)
                                        {
                                                TrItemRef = 1511;
                                        }
                                }
 
                                HeadsSet = new bool [thisCFGShape.SignalSubObjs.Count];

  // loop through all heads and check SubObj flag per bit to check if head is set

                                uint iMask = 1;

                                for (int iHead = 0; iHead < thisCFGShape.SignalSubObjs.Count; iHead++)
                                {
                                        HeadsSet[iHead] = false;
                                        uint headSet = SignalWorldItem.SignalSubObj & iMask;
                                        MSTS.SignalShape.SignalSubObj thisSubObjs = thisCFGShape.SignalSubObjs[iHead];
                                        if (headSet != 0)
                                        {

  // set head, and if head is flag, also set flag

                                                HeadsSet[iHead] = true;
                                                
                                                if (thisSubObjs.SignalSubType >= 1)
                                                {
                                                         FlagsSet[thisSubObjs.SignalSubType] = true;
                                                }
                                        }
                                        iMask = iMask << 1;
                                }

  // get TDB and head reference from World file

                                foreach (MSTS.SignalUnit signalUnitInfo in SignalWorldItem.SignalUnits.Units)
                                {
                                        uint TrItemRef = signalUnitInfo.TrItem;
                                        uint HeadRef   = Convert.ToUInt32(signalUnitInfo.SubObj);
                                        HeadReference.Add(TrItemRef, HeadRef);
                                }
                        }
                        else
                        {
                                Trace.TraceWarning("Signal not found : {0} n", SFileName);
                        }
                                
                }

        }
}


