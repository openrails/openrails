// Debug flags :
// #define DEBUG_PRINT
// prints details of the derived signal structure
#define CHECKED_ROUTE_SET
// Checks route to next signal

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Popups;


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
                private TSectionDatFile tsectiondat;
                private TDBFile tdbfile;

                private int[,] visited;
                private SignalObject[] signalObjects;
                private List<SignalWorldObject> SignalWorldList = new List<SignalWorldObject>();
                private Dictionary<uint, SignalRefObject> SignalRefList; 
                public static SIGSCRfile scrfile;

                public int noSignals = 0;
                private int foundSignals = 0;

                private static int updatecount=0;

  //================================================================================================//
  ///
  /// Constructor
  ///

                public Signals(Simulator simulator, SIGCFGFile sigcfg)
                {

                        SignalRefList = new Dictionary <uint, SignalRefObject> ();

                        trackDB = simulator.TDB.TrackDB;
                        tsectiondat = simulator.TSectionDat;
                        tdbfile = Program.Simulator.TDB;

  // read SIGSCR files

                        Trace.Write(" SIGSCR ");
                        scrfile = new SIGSCRfile(simulator.RoutePath, sigcfg.ScriptFiles, sigcfg.SignalTypes);

  // build list of signal world file information

                        BuildSignalWorld(simulator, sigcfg); 

  // build list of signals in TDB file

                        BuildSignalList(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat, tdbfile);

                        if (foundSignals > 0)
                        {
  // Add CFG info

                                AddCFG(sigcfg);

  // Add World info

                                AddWorldInfo();

  // check for any backfacing heads in signals
  // if found, split signal

                                SplitBackfacing(trackDB.TrItemTable, trackDB.TrackNodes);
                        }

#if DEBUG_PRINT
                        for (int isignal=0; isignal < signalObjects.Length-1; isignal++)
                        {
                                SignalObject singleSignal = signalObjects[isignal];
                                if (singleSignal == null)
                                {
                                        File.AppendAllText(@"SignalObjects.txt","\nInvalid entry : "+isignal.ToString()+"\n");
                        }
                                else
                                {
                                        File.AppendAllText(@"SignalObjects.txt","\nSignal ref item     : "+singleSignal.thisRef.ToString()+"\n");
                                        File.AppendAllText(@"SignalObjects.txt","Track node + index  : "+singleSignal.trackNode.ToString()+" + "+
                                                                                                        singleSignal.trRefIndex.ToString()+"\n");

                                        foreach (SignalHead thisHead in singleSignal.SignalHeads)
                                        {
                                           File.AppendAllText(@"SignalObjects.txt","Type name           : "+thisHead.signalType.Name.ToString()+"\n");
                                           File.AppendAllText(@"SignalObjects.txt","Type                : "+thisHead.signalType.FnType.ToString()+"\n");
                                           File.AppendAllText(@"SignalObjects.txt","item Index          : "+thisHead.trItemIndex.ToString()+"\n");
                                           File.AppendAllText(@"SignalObjects.txt","TDB  Index          : "+thisHead.TDBIndex.ToString()+"\n");
                                        }
                                }
                        }

                        foreach (KeyValuePair <string, MSTS.SignalShape> sshape in sigcfg.SignalShapes)
                        {
                                File.AppendAllText(@"SignalShapes.txt","\n==========================================\n");
                                File.AppendAllText(@"SignalShapes.txt","Shape key   : "+sshape.Key.ToString()+"\n");
                                MSTS.SignalShape thisshape = sshape.Value;
                                File.AppendAllText(@"SignalShapes.txt","Filename    : "+thisshape.ShapeFileName.ToString()+"\n");
                                File.AppendAllText(@"SignalShapes.txt","Description : "+thisshape.Description.ToString()+"\n");

                                foreach (MSTS.SignalShape.SignalSubObj ssobj in thisshape.SignalSubObjs)
                                {
                                   File.AppendAllText(@"SignalShapes.txt","\nSubobj Index : "+ssobj.Index.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","Matrix       : "+ssobj.MatrixName.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","Description  : "+ssobj.Description.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","Sub Type (I) : "+ssobj.SignalSubType.ToString()+"\n");
                                   if (ssobj.SignalSubSignalType != null)
                                   {
                                      File.AppendAllText(@"SignalShapes.txt","Sub Type (C) : "+ssobj.SignalSubSignalType.ToString()+"\n");
                                   }
                                   else
                                   {
                                      File.AppendAllText(@"SignalShapes.txt","Sub Type (C) : not set \n");
                                   }
                                   File.AppendAllText(@"SignalShapes.txt","Optional     : "+ssobj.Optional.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","Default      : "+ssobj.Default.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","BackFacing   : "+ssobj.BackFacing.ToString()+"\n");
                                   File.AppendAllText(@"SignalShapes.txt","JunctionLink : "+ssobj.JunctionLink.ToString()+"\n");
                                }
                                File.AppendAllText(@"SignalShapes.txt","\n==========================================\n");
                        }
#endif

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

                        Trace.WriteLine("");
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
                        Trace.WriteLine("");

#if DEBUG_PRINT
                        foreach ( KeyValuePair <uint, SignalRefObject> thisref in SignalRefList)
                        {
                            uint headref;
                                uint TBDRef = thisref.Key;
                                SignalRefObject signalRef = thisref.Value;

                                SignalWorldObject reffedObject = SignalWorldList[(int) signalRef.SignalWorldIndex];
                                if ( !reffedObject.HeadReference.TryGetValue(TBDRef, out headref))
                                {
                                        File.AppendAllText(@"WorldSignalList.txt","Incorrect Ref : "+TBDRef.ToString()+"\n");
                                        foreach ( KeyValuePair <uint, uint> headindex in reffedObject.HeadReference)
                                        {
                                                File.AppendAllText(@"WorldSignalList.txt","TDB : "+headindex.Key.ToString()+
                                                                " + "+headindex.Value.ToString()+"\n");
                                        }
                                }
                        }
#endif

                }  //BuildSignalWorld


  //================================================================================================//
  /// 
  /// Update : perform signal updates
  /// 

                public void Update(float elapsedClockSeconds)
                {
					if (MultiPlayer.MPManager.IsMultiPlayer() && !MultiPlayer.MPManager.IsServer()) return;
                        if (foundSignals > 0)
                        {

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

                private void BuildSignalList(TrItem[] TrItems, TrackNode[] trackNodes, TSectionDatFile tsectiondat, TDBFile tdbfile)
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
                                        else if (trItem.ItemType == TrItem.trItemType.trSPEEDPOST)
                                        {
                                                SpeedPostItem Speedpost = (SpeedPostItem) trItem;
                                                if (Speedpost.IsLimit)
                                                {
                                                        noSignals++;
                                }
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
                                        ScanSection(TrItems, trackNodes, i, tsectiondat, tdbfile);
                                        }
                                }

                } //BuildSignalList

                
  //================================================================================================//
  ///
  /// Split backfacing signals
  ///

                private void SplitBackfacing(TrItem[] TrItems, TrackNode[] TrackNodes)
                {
                
                        List <SignalObject> newSignals = new List<SignalObject> ();
                        int newindex = foundSignals+1;

  //
  // Loop through all signals to check on Backfacing heads
  //

                        for (int isignal=0; isignal < signalObjects.Length-1; isignal++)
                        {
                                SignalObject singleSignal = signalObjects[isignal];
                                if (singleSignal != null && singleSignal.isSignal && 
                                                singleSignal.WorldObject != null && singleSignal.WorldObject.Backfacing.Count > 0)
                                {

  //
  // create new signal - copy of existing signal
  // use Backfacing flags and reset head indication
  //

                                        SignalObject newSignal = new SignalObject(singleSignal);

                                        newSignal.thisRef = newindex;
                                        newSignal.signalRef = this;
                                        newSignal.trRefIndex = 0;

                                        newSignal.WorldObject.FlagsSet = new bool [singleSignal.WorldObject.FlagsSetBackfacing.Length];
                                        singleSignal.WorldObject.FlagsSetBackfacing.CopyTo (newSignal.WorldObject.FlagsSet, 0);

                                        for (int iindex = 0; iindex < newSignal.WorldObject.HeadsSet.Length; iindex++)
                                        {
                                                newSignal.WorldObject.HeadsSet[iindex] = false;
                                        }

  //
  // Somehow, the original and not the new signal must be reversed
  //

                                        singleSignal.direction = singleSignal.direction == 0 ? 1 : 0;           // reverse //
                                        singleSignal.tdbtraveller.ReverseDirection();                           // reverse //


  //
  // loop through the list with headreferences, check this agains the list with backfacing heads
  // use the TDBreference to find the actual head
  //

                                        List<int> removeHead = new List<int> ();  // list to keep trace of heads which are moved //

                                        foreach (KeyValuePair <uint, uint> thisHeadRef in singleSignal.WorldObject.HeadReference)
                                        {
                                                for (int iindex = singleSignal.WorldObject.Backfacing.Count - 1; iindex >= 0; iindex --)
                                                {
                                                        int ihead = singleSignal.WorldObject.Backfacing[iindex];
                                                        if (thisHeadRef.Value == ihead)
                                                        {
                                                                for (int ihIndex=0; ihIndex < singleSignal.SignalHeads.Count; ihIndex++)
                                                                {
                                                                        SignalHead thisHead = singleSignal.SignalHeads[ihIndex];

  //
  // backfacing head found - add to new signal, set to remove from exising signal
  //

                                                                        if (thisHead.TDBIndex == thisHeadRef.Key)
                                                                        {
                                                                                removeHead.Add(ihIndex);

                                                                                thisHead.mainSignal = newSignal;
                                                                                newSignal.SignalHeads.Add(thisHead);
                                                                        }
                                                                }
                                                        }

  //
  // update flags for available heads
  //

                                                        newSignal.WorldObject.HeadsSet[ihead] = true;
                                                        singleSignal.WorldObject.HeadsSet[ihead] = false;
                                                }
                                        }

  //
  // remove moved heads from existing signal
  //

                                        for (int ihead = singleSignal.SignalHeads.Count-1; ihead >= 0; ihead--)
                                        {
                                                if (removeHead.Contains(ihead))
                                                {
                                                        singleSignal.SignalHeads.RemoveAt(ihead);
                                                }
                                        }


  //
  // set correct trRefIndex for this signal, and set cross-reference for all backfacing trRef items
  //

                                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.noItemRefs; i++)
                                        {
                                                int TDBRef = TrackNodes[newSignal.trackNode].TrVectorNode.TrItemRefs[i];
                                                if (TrItems[TDBRef] != null)
                                                {
                                                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                                        {
                                                                foreach (SignalHead thisHead in newSignal.SignalHeads)
                                                                {
                                                                        if (TDBRef == thisHead.TDBIndex)
                                                                        {
                                                                                SignalItem sigItem = (SignalItem) TrItems[TDBRef];
                                                                                sigItem.sigObj = newSignal.thisRef;
                                                                                newSignal.trRefIndex = i;

                                                                                // remove this key from the original signal //
                                                                                singleSignal.WorldObject.HeadReference.Remove((uint) TDBRef);
                                                                        }
                                                                }
                                                        }
                                                }
                                        }

  //
  // reset cross-references for original signal (it may have been set for a backfacing head)
  //

                                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.noItemRefs; i++)
                                        {
                                                int TDBRef = TrackNodes[newSignal.trackNode].TrVectorNode.TrItemRefs[i];
                                                if (TrItems[TDBRef] != null)
                                                {
                                                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                                        {
                                                                foreach (SignalHead thisHead in singleSignal.SignalHeads)
                                                                {
                                                                        if (TDBRef == thisHead.TDBIndex)
                                                                        {
                                                                                SignalItem sigItem = (SignalItem) TrItems[TDBRef];
                                                                                sigItem.sigObj = singleSignal.thisRef;
                                                                                singleSignal.trRefIndex = i;

                                                                                // remove this key from the new signal //
                                                                                newSignal.WorldObject.HeadReference.Remove((uint) TDBRef);
                                                                        }
                                                                }
                                                        }
                                                }
                                        }

                                        newindex++;
                                        newSignals.Add(newSignal);
                                }
                        }

  //
  // add all new signals to the signalObject array
  // length of array was set to all possible signals, so there will be space to spare
  //

                        newindex = foundSignals+1;
                        foreach(SignalObject newSignal in newSignals)
                        {
                                signalObjects[newindex] = newSignal;
                                newindex++;
                        }

                        foundSignals = newindex;
                }

  //================================================================================================//
  //
  //  ScanSection : This method checks a section in the TDB for signals or speedposts
  //

                private void ScanSection(TrItem[] TrItems, TrackNode[] trackNodes, int index, 
                                       TSectionDatFile tsectiondat, TDBFile tdbfile)
                {
                        int lastSignal = -1;                // Index to last signal found in path -1 if none

                        if (trackNodes[index].TrEndNode) return;

  //  Is it a vector node then it may contain objects.
                        if (trackNodes[index].TrVectorNode != null && trackNodes[index].TrVectorNode.noItemRefs > 0)
                                {
  // Any obects ?
                                                        for (int i = 0; i < trackNodes[index].TrVectorNode.noItemRefs; i++)
                                                        {
                                                                if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]] != null)
                                                                {

  // Track Item is signal
                                                                        int TDBRef = trackNodes[index].TrVectorNode.TrItemRefs[i];
                                                                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                                                        {
                                                                                SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                                                                        sigItem.sigObj = foundSignals;

                                                                                        if (sigItem.noSigDirs > 0)
                                                                                        {
                                                                                        SignalItem.strTrSignalDir sigTrSignalDirs = sigItem.TrSignalDirs[0];
                                                                                        }

                                                                                        lastSignal = AddSignal(index, i, sigItem, lastSignal,
                                                                                                               TrItems, trackNodes, TDBRef, tsectiondat, tdbfile);
                                                                                        sigItem.sigObj = lastSignal;
                                                                                }
                                                else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSPEEDPOST)
                                                {
                                                        SpeedPostItem speedItem = (SpeedPostItem) TrItems[TDBRef];
                                                        if (speedItem.IsLimit)
                                                        {
                                                                speedItem.sigObj = foundSignals;

                                                                lastSignal = AddSpeed(index, i, speedItem, lastSignal,
                                                                                                               TrItems, trackNodes, TDBRef, tsectiondat, tdbfile);
                                                                speedItem.sigObj = lastSignal;
                                                                                }
                                                                        }
                                                                }
                                                        }
                                                }
                }   //ScanSection 

  //================================================================================================//
  ///
  /// This method adds a new Signal to the list
  ///

                private int AddSignal(int trackNode, int nodeIndx, SignalItem sigItem, int prevSignal, 
                                TrItem[] TrItems, TrackNode[] trackNodes, int TDBRef, TSectionDatFile tsectiondat, TDBFile tdbfile)
                {
                        if (prevSignal >= 0)
                        {
                                if (signalObjects[prevSignal].isSignal)
                                {
                                if (signalObjects[prevSignal].isSignalHead((SignalItem)TrItems[trackNodes[trackNode].TrVectorNode.TrItemRefs[nodeIndx]]))
                                {
                                        signalObjects[prevSignal].AddHead(nodeIndx, TDBRef, sigItem);
                                        return prevSignal;
                                }
                        }
                        }
                        signalObjects[foundSignals] = new SignalObject();
                        signalObjects[foundSignals].isSignal  = true;
                        signalObjects[foundSignals].direction = (int) sigItem.Direction;
                        signalObjects[foundSignals].trackNode = trackNode;
                        signalObjects[foundSignals].trRefIndex = nodeIndx;
                        signalObjects[foundSignals].prevSignal = prevSignal;
                        signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, sigItem);
                        signalObjects[foundSignals].thisRef = foundSignals;
                        signalObjects[foundSignals].signalRef = this;

                        signalObjects[foundSignals].tdbtraveller = new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                            sigItem.TileX, sigItem.TileZ, sigItem.X, sigItem.Z, (Traveller.TravellerDirection)(1 - sigItem.Direction));

                        signalObjects[foundSignals].WorldObject = null;
                        foundSignals++;
                        return foundSignals - 1;
                } // AddSignal

                
  //================================================================================================//
  ///
  /// This method adds a new Speedpost to the list
  ///

                private int AddSpeed(int trackNode, int nodeIndx, SpeedPostItem speedItem, int prevSignal, 
                                TrItem[] TrItems, TrackNode[] trackNodes, int TDBRef, TSectionDatFile tsectiondat, TDBFile tdbfile)
                {
                        signalObjects[foundSignals] = new SignalObject();
                        signalObjects[foundSignals].isSignal  = false;
                        signalObjects[foundSignals].direction = 0;                  // preset - direction not yet known //
                        signalObjects[foundSignals].trackNode = trackNode;
                        signalObjects[foundSignals].trRefIndex = nodeIndx;
                        signalObjects[foundSignals].prevSignal = prevSignal;
                        signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, speedItem);
                        signalObjects[foundSignals].thisRef = foundSignals;
                        signalObjects[foundSignals].signalRef = this;

                        signalObjects[foundSignals].tdbtraveller = new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                            speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z, (Traveller.TravellerDirection)signalObjects[foundSignals].direction);

                        double delta_angle = signalObjects[foundSignals].tdbtraveller.RotY - ((Math.PI/2) - speedItem.Angle);
                        float delta_float = (float)delta_angle;
                        MSTSMath.M.NormalizeRadians( ref delta_float);
                        if (Math.Abs(delta_float) < (Math.PI/2))
                        {
                            signalObjects[foundSignals].direction = signalObjects[foundSignals].tdbtraveller.Direction == 0 ? 1 : 0;
                        }
                        else
                        {
                            signalObjects[foundSignals].direction = (int)signalObjects[foundSignals].tdbtraveller.Direction;
                            signalObjects[foundSignals].tdbtraveller.ReverseDirection();
                        }

#if DEBUG_PRINT
                        string dumpstring = "\nPlaced : ";
                        dumpstring = String.Concat(dumpstring," at : ");
                        dumpstring = String.Concat(dumpstring,speedItem.TileX.ToString()," ");
                        dumpstring = String.Concat(dumpstring,speedItem.TileZ.ToString(),":");
                        dumpstring = String.Concat(dumpstring,speedItem.X.ToString()," ");
                        dumpstring = String.Concat(dumpstring,speedItem.Z.ToString()," ");
                        dumpstring = String.Concat(dumpstring,"; angle - track : ");
                        dumpstring = String.Concat(dumpstring,speedItem.Angle.ToString(),":",
                                        signalObjects[foundSignals].tdbtraveller.Roty.ToString());
                        dumpstring = String.Concat(dumpstring,"; delta : ",delta_angle.ToString());
                        dumpstring = String.Concat(dumpstring,"; dir : ",signalObjects[foundSignals].direction.ToString());
                        Trace.Write(dumpstring);
#endif

                        signalObjects[foundSignals].WorldObject = null;
                        foundSignals++;
                        return foundSignals - 1;
                } // AddSpeed

  //================================================================================================//
  /// 
  ///  This method returns the index of the next signal along the set path. -1 if no signal found
  /// 

                public int FindNextSignal(int startIndex, int startDir, TrItem[] TrItems, TrackNode[] trackNodes)
                {
                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = SignalHead.SIGFN.NORMAL;
                        int newindex = Find_Next_Object(null, startIndex, startDir, true, null, false, -1, TrItems, trackNodes, fn_type_array);

                        return newindex<0 ? -1 : newindex;
                } //FindNextSignal

  //================================================================================================//
  //
  // NextNode : find next junction node in path
  //

                private void NextNode(TrackNode[] trackNodes, ref int node, ref int direction, ref int prevNode)
                {
                    
                    SignalObject.NextNode(ref node, ref direction, ref prevNode);
                    if (node == prevNode)
                        node = 0;
                    return;
                    
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
                                        if (signal.isSignal)
                                        {
                                        signal.SetSignalType(sigCFG);
                                }
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
                public int FindNextSignal(Traveller tdbtraveller)
                {
                        int startNode = tdbtraveller.TrackNodeIndex;
                        int currDir = (int)tdbtraveller.Direction;

                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = SignalHead.SIGFN.NORMAL;

                        int newindex = Find_Next_Object(null, startNode, currDir, true, tdbtraveller, true, -1,
                                        trackDB.TrItemTable, trackDB.TrackNodes, fn_type_array);

                        return newindex<0 ? -1 : newindex;
                }//FindNextSignal

  //================================================================================================//
  ///
  // Get signal object of nearest signal in direction of travel
  ///
  /// 
                public Signal FindNearestSignal(Traveller tdbtraveller)
                {
                        int sigRef = FindNextSignal(tdbtraveller);
                        return new Signal(this, signalObjects, sigRef);
                }//FindNearestSignal

  //================================================================================================//
  ///
  // Initialize Signal object (for track occupancy)
  ///
  /// 
                public Signal InitSignalItem(int sigRef)
                {
                        return new Signal(this, signalObjects, sigRef);
                }//FindNearestSignal


  //================================================================================================//
  ///
  //  Get index of previous signal in direction of travel
  ///
                public int FindPrevSignal(Traveller tdbtraveller)
                {
                        Traveller revTDBtraveller = new Traveller(tdbtraveller, Traveller.TravellerDirection.Backward);

                        int startNode = tdbtraveller.TrackNodeIndex;
                        int currDir = (int)tdbtraveller.Direction;

                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [1];
                        fn_type_array [0] = SignalHead.SIGFN.NORMAL;

                        int newindex = Find_Next_Object(null, startNode, currDir, false, revTDBtraveller, true, -1,
                                        trackDB.TrItemTable, trackDB.TrackNodes, fn_type_array);

                        return newindex<0 ? -1 : newindex;

                }//FindPrevSignal
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
                }//FindByTrItem

  //================================================================================================//
  //
  // Find_Next_Object : find next item along path of train
  //
  // Usage :
  //   always set : Tritems, trackNodes, fn_type, in_direction_of_travel
  //
  //   from existing object :
  //     set startObjRef
  //
  //   from train :
  //     set nodestartindex, startDirection, tdbtraveller, min_distance_check,
  //     optional : maxdistance
  //
  // returned :
  //   > 0 : signal object reference
  //   -1  : end if track
  //   -2  : no item within required distance
  //   -3  : trackref. 0 found - error in tdb
  //   -4  : track looped - error in tdb
  //

                public int Find_Next_Object(SignalObject startObj, int nodestartindex,
                                int startDirection, bool in_direction_of_travel,
                                Traveller tdbtraveller, bool min_distance_check, float maxdistance,
                                TrItem[] Tritems, TrackNode[] trackNodes,
                                       SignalHead.SIGFN[] fn_type)
                {

                    int locstate = 0;                                                                // local processing state     //
                    int actindex = 0;                                                                // present node               //
                    int actrefindex = -1;                                                            // first index to check       //
                    int lastrefindex = 0;                                                            // next index for loop        //
                    int direction = 0;                                                               // travel direction           //
                    int prevnodeindex = -1;
                    TrackNode thisTrackNode = null;
                    TrItem thisTrItem = null;

                    // check if search from object or train

                    if (startObj == null)
                    {
                        actindex = nodestartindex;
                        direction = startDirection;
                    }
                    else
                    {
                        actindex = startObj.trackNode;
                        actrefindex = startObj.trRefIndex;
                        direction = startObj.revDir;
                    }

                    int sigObjRef = -99;                                                                 // ref to signalObject index //

                    if (!in_direction_of_travel)
                    {
                        direction = direction == 0 ? 1 : 0;
                    }

                    //
                    // loop through nodes until :
                    //  - end of track is found
                    //  - required item is found
                    //  - max distance is covered
                    //  - broken or looped tdb is found
                    //

                    while (locstate == 0)
                    {
                        if (trackNodes[actindex].TrEndNode)
                        {
                            locstate = -2;
                        }

                        if (trackNodes[actindex].TrVectorNode != null)
                        {
                            if (direction == 1)
                            {
                                lastrefindex = actrefindex == -1 ? 0 : ++actrefindex;
                            }
                            else
                            {
                                lastrefindex = actrefindex == -1 ? trackNodes[actindex].TrVectorNode.noItemRefs - 1 : --actrefindex;
                            }
                        }

                        while (locstate == 0 && trackNodes[actindex].TrVectorNode != null)
                        {
                            // find next item within node

                            if (direction == 1)
                            {
                                for (int refindex = lastrefindex; refindex < trackNodes[actindex].TrVectorNode.noItemRefs && locstate == 0; refindex++)
                                {
                                    if (Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]].ItemType ==
                                                    TrItem.trItemType.trSIGNAL)
                                    {
                                        thisTrackNode = trackNodes[actindex];
                                        thisTrItem = Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]];
                                        SignalItem sigitem = (SignalItem)thisTrItem;
                                        sigObjRef = sigitem.sigObj;
                                        locstate = 1;
                                        lastrefindex = ++refindex;
                                    }
                                    else if (Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]].ItemType ==
                                                    TrItem.trItemType.trSPEEDPOST)
                                    {
                                        thisTrackNode = trackNodes[actindex];
                                        thisTrItem = Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]];
                                        SpeedPostItem spditem = (SpeedPostItem)thisTrItem;
                                        sigObjRef = spditem.sigObj;
                                        locstate = 1;
                                        lastrefindex = ++refindex;
                                    }
                                }
                            }
                            else
                            {
                                for (int refindex = lastrefindex; refindex >= 0 && locstate == 0; refindex--)
                                {
                                    if (Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]].ItemType ==
                                                    TrItem.trItemType.trSIGNAL)
                                    {
                                        thisTrackNode = trackNodes[actindex];
                                        thisTrItem = Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]];
                                        SignalItem sigitem = (SignalItem)thisTrItem;
                                        sigObjRef = sigitem.sigObj;
                                        locstate = 1;
                                        lastrefindex = --refindex;
                                    }
                                    else if (Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]].ItemType ==
                                                    TrItem.trItemType.trSPEEDPOST)
                                    {
                                        thisTrackNode = trackNodes[actindex];
                                        thisTrItem = Tritems[trackNodes[actindex].TrVectorNode.TrItemRefs[refindex]];
                                        SpeedPostItem spditem = (SpeedPostItem)thisTrItem;
                                        sigObjRef = spditem.sigObj;
                                        locstate = 1;
                                        lastrefindex = --refindex;
                                    }
                                }
                            }

                            // check if any item found

                            if (sigObjRef == -99)
                            {
                                locstate = -10;
                            }

                            // check if valid item

                            if (locstate > 0 && (sigObjRef < 0 || signalObjects[sigObjRef] == null))
                            {
                                locstate = 0;
                                sigObjRef = -99;
                            }

                            // check if not head of same signal

                            if (locstate > 0 && startObj != null)
                            {
                                if (sigObjRef == startObj.thisRef)
                                {
                                    locstate = 0;
                                    sigObjRef = -99;
                                }
                            }

                            // check if item has correct direction

                            if (locstate > 0)
                            {
                                int sigdirection = signalObjects[sigObjRef].revDir;

                                if (in_direction_of_travel)
                                {
                                    if (sigdirection != direction)
                                    {
                                        locstate = 0;
                                        sigObjRef = -99;
                                    }
                                }
                                else
                                {
                                    if (sigdirection == direction)
                                    {
                                        locstate = 0;
                                        sigObjRef = -99;
                                    }
                                }
                            }

                            // check if item is of correct type

                            if (locstate > 0 && !signalObjects[sigObjRef].isSignalType(fn_type))
                            {
                                locstate = 0;
                                sigObjRef = -99;
                            }

                            // check if ahead of position

                            if (locstate > 0 && min_distance_check && tdbtraveller != null)
                            {
                                float mindistance =
                                        tdbtraveller.DistanceTo(thisTrackNode, thisTrItem.TileX, thisTrItem.TileZ,
                                                                thisTrItem.X, thisTrItem.Y, thisTrItem.Z);
                                if (mindistance < 0)
                                {
                                    locstate = 0;
                                    sigObjRef = -99;
                                }
                            }

                            // check if not beyond maximum distance

                            if (locstate > 0 && tdbtraveller != null && maxdistance > 0)
                            {
                                float actdistance =
                                        tdbtraveller.DistanceTo(thisTrackNode, thisTrItem.TileX, thisTrItem.TileZ,
                                                                thisTrItem.X, thisTrItem.Y, thisTrItem.Z);
                                if (actdistance > maxdistance)
                                {
                                    locstate = -2;
                                    sigObjRef = -99;
                                }
                            }
                        }

                        // no items in currect node - go to next node

                        if (locstate == -10)
                        {
                            locstate = 0;    // set to valid again for next node //
                        }

                        int nextvalidnode = 0;
                        while (locstate == 0 && nextvalidnode == 0)
                        {
                            NextNode(trackNodes, ref actindex, ref direction, ref prevnodeindex);

                            if (actindex == 0 || actindex > trackNodes.Length || prevnodeindex == actindex)
                            {
                                locstate = -3;
                            }

                            else if (actindex == nodestartindex)
                            {
                                locstate = -4;
                            }

                            else if (trackNodes[actindex].TrEndNode)
                            {
                                locstate = -1;
                            }

                            else if (locstate == 0 && trackNodes[actindex].TrVectorNode != null)
                            {
                                nextvalidnode = 1;
                                actrefindex = -1;
                            }
                        }
                    }

                    return locstate == 1 ? sigObjRef : locstate;
                }//Find_Next_Object

  //================================================================================================//
  ///
  //  Find item from train
  ///
  /// <summary>
  /// getNextObject : to get next object from forward or backward from train
  /// Parameters :
  /// TDBTraveller : tdbtraveller linked with train
  /// ObjectItemInfo.ObjectItemType : required type of object
  /// bool : forward indication (true = forward, false = backward)
  /// float : required max. distance; set to -1 if no check required
  /// ref ObjectItemInfo.ObjectItemFindState : returned state; > 0 : Object Reference; <= 0 : error or warning according to ObjectItemFindState
  /// Returned parameter :
  /// ObjectItemInfo : class holding required info on found object; only valid if returned state > 0
  /// </summary>

                public ObjectItemInfo getNextObject(Traveller tdbtraveller, ObjectItemInfo.ObjectItemType req_type,
                                bool forward, float maxdistance, ref ObjectItemInfo.ObjectItemFindState return_state)
                {
                        int startNode = tdbtraveller.TrackNodeIndex;
                        int currDir = (int)tdbtraveller.Direction;
                        SignalObject last_object = null;

                        ObjectItemInfo return_item = null;

 //
 // preset search info
 //

                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [2];
                        fn_type_array[0] = SignalHead.SIGFN.NORMAL;
                        fn_type_array[1] = SignalHead.SIGFN.SPEED;

                        SignalHead.SIGFN [] fn_type_signal  = new SignalHead.SIGFN [1];
                        fn_type_signal[0] = SignalHead.SIGFN.NORMAL;
                        SignalHead.SIGFN [] fn_type_speed   = new SignalHead.SIGFN [1];
                        fn_type_speed[0]  = SignalHead.SIGFN.SPEED;

                        float total_distance = 0.00F;

                        ObjectItemInfo.ObjectItemFindState find_state = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;
 //
 // loop until item found or search ended
 //

                        while (find_state == ObjectItemInfo.ObjectItemFindState.NONE_FOUND)
                        {
                                int newindex = Find_Next_Object(last_object, startNode, currDir, forward, tdbtraveller, true, maxdistance,
                                        trackDB.TrItemTable, trackDB.TrackNodes, fn_type_array);

                                if (newindex == -1)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.END_OF_TRACK;
                                }
                                else if (newindex == -2)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.PASSED_MAXDISTANCE;
                                }
                                else if (newindex < 0)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.TDB_ERROR;
                                }
                                else
                                {

 //
 // check on item found
 // set info according to type
 //

                                        SignalObject found_object = SignalObjects[newindex];
                                        last_object = found_object;
                                        bool found_signal = found_object.isSignalType(fn_type_signal);
                                        bool found_speed  = found_object.isSignalType(fn_type_speed);
                                        total_distance = found_object.DistanceTo(tdbtraveller);

                                        if (found_signal)
                                        {
                                                SignalHead.SIGASP sigaspect = found_object.this_sig_lr(SignalHead.SIGFN.NORMAL);
                                                if (req_type == ObjectItemInfo.ObjectItemType.SPEEDLIMIT && sigaspect == SignalHead.SIGASP.STOP)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.PASSED_DANGER;
                                                }
                                                else if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                                                        req_type == ObjectItemInfo.ObjectItemType.SIGNAL)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                                                        return_item = new ObjectItemInfo('T', 'S', found_object, total_distance);
                                                }
                                        }
                                        else if (found_speed)
                                        {
                                                if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                                                req_type == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                                                        return_item = new ObjectItemInfo('T', 'L', found_object, total_distance);
                                                }
                                        }
                                }
                        }

                        return_state = find_state;
                        return return_item;
                }//getNextObject(1)
  //================================================================================================//
  ///
  //  Find item from object
  ///
  /// <summary>
  /// getNextObject : to get next object forward from another object
  /// Parameters :
  /// SignalObject : object from which to search
  /// ObjectItemInfo.ObjectItemType : required type of object
  /// TDBTraveller : tdbtraveller of train linked with request; optional, but required if max. distance is set
  /// float : required max. distance; set to -1 if no check required
  /// ref ObjectItemInfo.ObjectItemFindState : returned state; > 0 : Object Reference; <= 0 : error or warning according to ObjectItemFindState
  /// Returned parameter :
  /// ObjectItemInfo : class holding required info on found object; only valid if returned state > 0
  /// </summary>

                public ObjectItemInfo getNextObject(SignalObject SignalObj, ObjectItemInfo.ObjectItemType req_type,
                                Traveller tdbtraveller, float maxdistance, ref ObjectItemInfo.ObjectItemFindState return_state)
                {
                        ObjectItemInfo return_item = null;
                        SignalObject last_object = SignalObj;

 //
 // preset search info
 //

                        SignalHead.SIGFN [] fn_type_array  = new SignalHead.SIGFN [2];
                        fn_type_array[0] = SignalHead.SIGFN.NORMAL;
                        fn_type_array[1] = SignalHead.SIGFN.SPEED;

                        SignalHead.SIGFN [] fn_type_signal  = new SignalHead.SIGFN [1];
                        fn_type_signal[0] = SignalHead.SIGFN.NORMAL;
                        SignalHead.SIGFN [] fn_type_speed   = new SignalHead.SIGFN [1];
                        fn_type_speed[0]  = SignalHead.SIGFN.SPEED;

                        float total_distance = 0.00F;

                        ObjectItemInfo.ObjectItemFindState find_state = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;
                        bool maxdist_req = (tdbtraveller != null);

 //
 // if item to search from is signal and state is stop, abandone search
 //

                        if (last_object.isSignal)
                        {
                                SignalHead.SIGASP lastState = last_object.this_sig_lr(SignalHead.SIGFN.NORMAL);
                                if (lastState == SignalHead.SIGASP.STOP)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.PASSED_DANGER;
                                }
                        }
 //
 // loop until object found or search stopped
 //

                        while (find_state == ObjectItemInfo.ObjectItemFindState.NONE_FOUND)
                        {

                                int newindex = Find_Next_Object(last_object, 0, 0, true, tdbtraveller, maxdist_req, maxdistance,
                                        trackDB.TrItemTable, trackDB.TrackNodes, fn_type_array);

                                if (newindex == -1)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.END_OF_TRACK;
                                }
                                else if (newindex == -2)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.PASSED_MAXDISTANCE;
                                }
                                else if (newindex < 0)
                                {
                                        find_state = ObjectItemInfo.ObjectItemFindState.TDB_ERROR;
                                }
                                else
                                {

 //
 // check on item found
 //

                                        SignalObject found_object = SignalObjects[newindex];
                                        last_object=found_object;
                                        bool found_signal = found_object.isSignalType(fn_type_signal);
                                        bool found_speed  = found_object.isSignalType(fn_type_speed);
                                        total_distance = found_object.DistanceTo(SignalObj.tdbtraveller);

 //
 // if signal is found at danger while searching for speedlimit, set to invalid
 //

                                        if (found_signal)
                                        {
                                                SignalHead.SIGASP sigaspect = found_object.this_sig_lr(SignalHead.SIGFN.NORMAL);
                                                if (req_type == ObjectItemInfo.ObjectItemType.SPEEDLIMIT && sigaspect == SignalHead.SIGASP.STOP)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.PASSED_DANGER;
                                                }
                                                else if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                                                        req_type == ObjectItemInfo.ObjectItemType.SIGNAL)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                                                        return_item = new ObjectItemInfo('O', 'S', found_object, total_distance);
                                                }
                                        }
                                        else if (found_speed)
                                        {
                                                if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                                                req_type == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                                                {
                                                        find_state = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                                                        return_item = new ObjectItemInfo('O', 'L', found_object, total_distance);
                                                }
                                        }
                                }
                        }

                        return_state = find_state;
                        return return_item;
                }//getNextObject(2)

  //================================================================================================//

        }//EndofClass

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

                public Signals signalRef;               // reference to overlaying Signal class
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
                public bool isSignal = true;            // if signal, false if speedpost //
                public bool useScript = false;
                public BLOCKSTATE blockState = BLOCKSTATE.CLEAR;
                public Signal.PERMISSION hasPermission = Signal.PERMISSION.DENIED;  // Permission to pass red signal
                public int nextSignal = -2;             // Index to next signal. -1 if none -2 indeterminate
                public int prevSignal = -2;             // Index to previous signal -1 if none -2 indeterminate
                public SignalWorldObject WorldObject;   // Signal World Object information
                public int [] sigfound = new int [(int) SignalHead.SIGFN.UNKNOWN];
                public Traveller tdbtraveller;       // TDB traveller to determine distance between objects
                public uint SignalNumClearAhead = 0;    // Overall maximum SignalNumClearAhead over all heads

  //================================================================================================//
  ///
  //  Constructor for empty item
  ///
  
                public SignalObject()
                {
                }

#if DUMP_DISPATCHER
                public void Dump(StringBuilder sta, Traveller t)
                {
                    sta.AppendFormat("|trackNode|{0}\r\n", trackNode);
                    if (t != null) sta.AppendFormat("|DistanceTo|{0}\r\n", DistanceTo(t));
                    sta.AppendFormat("|enabled|{0}\r\n", enabled);
                    if (WorldObject != null && WorldObject.FlagsSet != null)
                        sta.AppendFormat("|sigfeat|{0}\r\n", string.Join(":", WorldObject.FlagsSet.Select<bool, string>(f => f.ToString()).ToArray()) );
                    sta.AppendFormat("|block_state|{0}\r\n", blockState);
                    sta.AppendFormat("|block_state()|{0}\r\n", block_state());
                    sta.AppendFormat("|this_sig_lr()|{0}\r\n", this_sig_lr(SignalHead.SIGFN.NORMAL));
                    sta.AppendFormat("|nextSignal|{0}\r\n", nextSignal);

                    foreach (SignalHead sh in SignalHeads)
                    {
                        sh.Dump(sta);
                    }
                }
#endif
  //================================================================================================//
  ///
  //  Constructor for Copy 
  ///
  
                public SignalObject(SignalObject copy)
                {
                         signalRef            = copy.signalRef;
                         trackNode            = copy.trackNode;
                         nextNode             = copy.nextNode;
                         direction            = copy.direction;
                         draw_state           = copy.draw_state;
                         enabled              = copy.enabled;
                         isJunction           = copy.isJunction;
                         canUpdate            = copy.canUpdate;
                         isAuto               = copy.isAuto;
                         isSignal             = copy.isSignal;
                         useScript            = copy.useScript;
                         blockState           = copy.blockState;
                         hasPermission        = copy.hasPermission;
                         nextSignal           = copy.nextSignal;
                         prevSignal           = copy.prevSignal;
                         WorldObject          = new SignalWorldObject(copy.WorldObject);
                         tdbtraveller         = new Traveller(copy.tdbtraveller);
                         SignalNumClearAhead  = copy.SignalNumClearAhead;

                         sigfound = new int [copy.sigfound.Length];
                         copy.sigfound.CopyTo(sigfound, 0);
                }

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
                    int trainId = Dispatcher.Reservations[trackNode];
                    
                    if (Program.Simulator.Activity == null && nextSignal == -1)
                    {
                        return BLOCKSTATE.JN_OBSTRUCTED;
                    }
                    
                    if (trainId >= 0)
                    {
                        
                        Traveller traveller=null;
						if (trainId == 0)
							traveller = new Traveller(Program.Simulator.PlayerLocomotive.Train.dFrontTDBTraveller);
						else if (trainId > 100000)
						{
							foreach (var t in Program.Simulator.Trains)
							{
								if (t.Number == trainId - 100000) { traveller = new Traveller(t.dFrontTDBTraveller); break; }
							}
						}
						else 
                            traveller = new Traveller(Program.Simulator.AI.AITrainDictionary[trainId].dFrontTDBTraveller);

						if (traveller == null) return BLOCKSTATE.OCCUPIED;//hopefully this will not be the case
                        while (traveller.TrackNodeIndex != trackNode && traveller.NextSection()) ;
                        if (traveller.TrackNodeIndex != trackNode)
                        {
                            if (!Train.IsUnderObserving(thisRef))
                                return BLOCKSTATE.OCCUPIED;
                        }
                        else
                        {
                            if (this.revDir != (int)traveller.Direction)
                                return BLOCKSTATE.OCCUPIED;
                        }

                        // More logic to allow enter into straight area
                        int nextreservid = -2;
                        int nextSig = nextSignal;
                        int nextNode = -2;
                        int nextnextNode = -2;
                        int nextheads = 0;

                        if (nextSig > 0)
                        {
                            SignalObject nextSignalObject = signalObjects[nextSig];
                            nextheads = nextSignalObject.SignalHeads.Count;

                            nextNode = nextSignalObject.trackNode;
                            nextreservid = Dispatcher.Reservations[nextNode];

                            nextSig = nextSignalObject.GetNextSignal();
                            if (nextSig > 0)
                            {
                                nextSignalObject = signalObjects[nextSig];
                                nextnextNode = nextSignalObject.trackNode;
                            }
                        }
                        
                        if (trainId != nextreservid && nextreservid > -2)
                        {
                            if (nextNode != -2 && nextNode != nextnextNode)
                                return BLOCKSTATE.OCCUPIED;
                        }
                    }   
                    return blockState;
                    //return BLOCKSTATE.OCCUPIED;
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
                        int sigIndex = this.signalRef.Find_Next_Object(this, -1, -1,
                                               true, null, false, -1, trItems, trackNodes, fn_type);

                        return sigIndex<0 ? -1 : sigIndex;
                }//NextSignal

  //================================================================================================//
  //
  // NextNode : Returns the next node and direction in the TDB
  //

                public static void NextNode(ref int node, ref int direction, ref int prevnode)
                {
                    try
                    {
                        if (trackNodes[node].TrVectorNode != null)
                        {
                            if (direction == 0)
                            {
                                prevnode = node;
                                direction = trackNodes[node].TrPins[0].Direction;
                                node = trackNodes[node].TrPins[0].Link;
                            }
                            else
                            {
                                prevnode = node;
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
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[0].Direction;
                                        node = trackNodes[node].TrPins[0].Link;
                                    }
                                    else
                                    {
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[1].Direction;
                                        node = trackNodes[node].TrPins[1].Link;
                                    }
                                }
                                else
                                {
                                    if (prevnode == -1 ||
                                        (trackNodes[node].TrJunctionNode.SelectedRoute == 0 &&
                                        trackNodes[node].TrPins[trackNodes[node].Inpins].Link == prevnode) ||
                                        (trackNodes[node].TrJunctionNode.SelectedRoute == 1 &&
                                        trackNodes[node].TrPins[trackNodes[node].Inpins + 1].Link == prevnode))
                                    {
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[0].Direction;
                                        node = trackNodes[node].TrPins[0].Link;
                                    }
                                    else
                                    {
                                        prevnode = node;
                                    }
                                }
                            }
                            else
                            {
                                if (trackNodes[node].Outpins > 1)
                                {
                                    if (trackNodes[node].TrJunctionNode.SelectedRoute == 0)
                                    {
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[trackNodes[node].Inpins].Direction;
                                        node = trackNodes[node].TrPins[trackNodes[node].Inpins].Link;
                                    }
                                    else
                                    {
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[trackNodes[node].Inpins + 1].Direction;
                                        node = trackNodes[node].TrPins[trackNodes[node].Inpins + 1].Link;
                                    }
                                }
                                else
                                {
                                    if (prevnode == -1 ||
                                        (trackNodes[node].TrJunctionNode.SelectedRoute == 0 &&
                                        trackNodes[node].TrPins[0].Link == prevnode) ||
                                        (trackNodes[node].TrJunctionNode.SelectedRoute == 1 &&
                                        trackNodes[node].TrPins[1].Link == prevnode))
                                    {
                                        prevnode = node;
                                        direction = trackNodes[node].TrPins[trackNodes[node].Inpins].Direction;
                                        node = trackNodes[node].TrPins[trackNodes[node].Inpins].Link;
                                    }
                                    else
                                    {
                                        prevnode = node;
                                    }
                                }
                            }
                        }
                        else
                        {
                            prevnode = node = 0;
                        }
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(error);
                        prevnode = node;
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
  // this_sig_speed : Returns the speed related to the least restrictive aspect (for normal signal)
  //

                public ObjectSpeedInfo this_sig_speed(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGASP sigAsp = SignalHead.SIGASP.STOP;
                        ObjectSpeedInfo set_speed = new ObjectSpeedInfo(-1, -1, false);

                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (sigHead.sigFunction == fn_type && sigHead.state >= sigAsp)
                                {
                                        sigAsp = sigHead.state;
                                        int AspIndex = Convert.ToInt32(sigAsp);
                                        set_speed = sigHead.speed_info[AspIndex];
                                }
                        }
                        return set_speed;
                }//this_sig_speed

  //================================================================================================//
  //
  // this_lim_speed : Returns the lowest allowed speed (for speedpost and speed signal)
  //

                public ObjectSpeedInfo this_lim_speed(SignalHead.SIGFN fn_type)
                {
                        SignalHead.SIGASP sigAsp = SignalHead.SIGASP.STOP;
                        ObjectSpeedInfo set_speed = new ObjectSpeedInfo(9E9f, 9E9f, false);

                        foreach (SignalHead sigHead in SignalHeads)
                        {
                                if (sigHead.sigFunction == fn_type)
                                {
                                        sigAsp = sigHead.state;
                                        int AspIndex = Convert.ToInt32(sigAsp);
                                        ObjectSpeedInfo this_speed = sigHead.speed_info[AspIndex];
                                        if (this_speed != null)
                                        {
                                        if (this_speed.speed_pass > 0 && this_speed.speed_pass < set_speed.speed_pass)
                                        {
                                                set_speed.speed_pass = this_speed.speed_pass;
                                                set_speed.speed_flag = 0;
                                        }

                                        if (this_speed.speed_freight > 0 && this_speed.speed_freight < set_speed.speed_freight)
                                        {
                                                set_speed.speed_freight = this_speed.speed_freight;
                                                set_speed.speed_flag = 0;
                                        }
                                        }

                                }
                        }

                        if (set_speed.speed_pass    > 1E9f) set_speed.speed_pass    = -1;
                        if (set_speed.speed_freight > 1E9f) set_speed.speed_freight = -1;

                        return set_speed;
                }//this_lim_speed
//================================================================================================//
  //
  // route_set : check if required route is set
  //

                public bool route_set (int req_mainnode)
                {
                    bool rs;

                        int thisreservid = Dispatcher.Reservations[trackNode];
                        int nextreservid = Dispatcher.Reservations[req_mainnode];

                        rs = (thisreservid >= 0 && thisreservid == nextreservid);

                        int ctn = this.trackNode;
                        int ptn = -1;
                        int dir = revDir;
                        while (ctn != ptn && ctn != req_mainnode)
                        {
                            NextNode(ref ctn, ref dir, ref ptn);
                        }
                        rs |= (ctn == req_mainnode);

                        return rs;
                }

#if CHECKED_ROUTE_SET
                //================================================================================================//
  //
  // route_set : check if required route is set
  //

                public bool route_set ()
                {
                    return nextSignal != -1;
                }
#endif

                //================================================================================================//
  //
  // Update : Perform the update for each head on this signal.
  //

			//a signal maybe forced by the dispatcher, need to release it if it is 300 seconds ago, or a train has passed.
				private bool ReleaseLock()
				{
					if (Program.Simulator.GameTime > forcedTime + 300) { canUpdate = true; forcedTime = 0; return true; }
					try
					{
						var minimumDist = 1000f;
						var predicted = 2f;
						var totalDist = minimumDist;

						var item = Program.Simulator.TDB.TrackDB.TrItemTable[this.trItem];
						var sigLoc = new WorldLocation(item.TileX, item.TileZ, item.X, item.Y, item.Z);
						foreach (var train in Program.Simulator.Trains)
						{
							if (!WorldLocation.Within(sigLoc, train.FrontTDBTraveller.WorldLocation, totalDist) && !WorldLocation.Within(sigLoc, train.RearTDBTraveller.WorldLocation, totalDist))
								continue;

							var speedMpS = train.SpeedMpS;
							// Distances forward from the front and rearwards from the rear.
							var frontDist = this.DistanceTo(train.FrontTDBTraveller);
							if (frontDist < 0)
							{
								frontDist = -this.DistanceTo(new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward));
								if (frontDist > 0)
								{
									// Train cannot find crossing.
									continue;
								}
							}
							var rearDist = -frontDist - train.Length;

							if (speedMpS < 0)
							{
								// Train is reversing; swap distances so frontDist is always the front.
								var temp = rearDist;
								rearDist = frontDist;
								frontDist = temp;
							}

							if (frontDist <= 1 && rearDist <= predicted)
							{
								this.canUpdate = true; forcedTime = 0; return true;
							}
						}
					}
					catch { }
					return false;
				}

				public double forcedTime = 0;

                public void Update()
                {
					if (forcedTime > 1) ReleaseLock();//forced by the dispatcher, will try to release the lock. forcedTime will only be set to be > 0 in MP mode
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
                                nextNode = -2;

                                if (nextSignal > 0)
                                {
                                        SignalObject nextSignalObject = signalObjects[nextSignal];
                                        sigfound[(int) SignalHead.SIGFN.NORMAL]=nextSignal;

                                        nextNode   = nextSignalObject.trackNode;
                                        nextreservid = Dispatcher.Reservations[nextNode];
                                }

  // set enabled

                                if (Program.Simulator.Activity == null && !MultiPlayer.MPManager.IsServer())
                                {
                                    enabled = true;
                                }
                                else
                                {
                                    int thisreservid = Dispatcher.Reservations[trackNode];
                                    if (thisreservid >= 0)
                                    {
                                        // By GeorgeS
                                        if (nextreservid < -1)
                                        {
                                            enabled = true;
                                        }
                                        else
                                        {
                                            enabled = (thisreservid == nextreservid || trackNode == signalObjects[nextSignal].trackNode);

                                            if (!enabled)
                                            {
                                                int trainId = Dispatcher.Reservations[trackNode];

                                                if (trainId >= 0)
                                                {
                                                    int ctn = this.trackNode;
                                                    int ptn = -1;
                                                    int dir = revDir;
                                                    while (ctn != ptn && ctn != nextNode)
                                                    {
                                                        NextNode(ref ctn, ref dir, ref ptn);
                                                    }
                                                    enabled = ctn == nextNode;

                                                    //if (!enabled)
                                                    //{
                                                    //    enabled = Train.IsUnderObserving(thisRef);
                                                    //}

                                                }
                                            }

                                        }
                                    }
                                    else
                                    {
										if (MultiPlayer.MPManager.IsMultiPlayer()) enabled = true;
										else enabled = false;
                                    }
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

                public float DistanceTo(Traveller tdbTraveller)
                {
                        int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
                        return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
                }//DistanceTo

  //================================================================================================//
  //
  // DistanceToRef : Returns the distance from the TDBtraveller to this signal and sets the Traveller at the signal. 
  //

                public float DistanceToRef(ref Traveller tdbTraveller)
                {
                        int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
                        return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z, out tdbTraveller);
                }//DistanceTo

//================================================================================================//
  //
  // ObjectDistance : Returns the distance from this object to the next object
  //

                public float ObjectDistance(SignalObject nextObject)
                {
                        int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
                        int nextTrItem = trackNodes[nextObject.trackNode].TrVectorNode.TrItemRefs[nextObject.trRefIndex];
                        return this.tdbtraveller.DistanceTo(
                                                trItems[nextTrItem].TileX, trItems[nextTrItem].TileZ, 
                                                trItems[nextTrItem].X, trItems[nextTrItem].Y, trItems[nextTrItem].Z);
                }//ObjectDistance

  //================================================================================================//
  //
  // isSignalHead : Check Whether signal head is for this signal.
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
  // create SignalHead
                        SignalHead head = new SignalHead(this, trItem, TDBRef, sigItem);

  // set junction link
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

                }//AddHead (signal)

  //================================================================================================//
  //
  // AddHead : Adds a head to this signal (for speedpost).
  //

                public void AddHead(int trItem, int TDBRef, SpeedPostItem speedItem)
                {
  // create SignalHead
                        SignalHead head = new SignalHead(this, trItem, TDBRef, speedItem);
                        SignalHeads.Add(head);

                }//AddHead (speedpost)

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
  // TranslateTMAspect : Gets the display aspect for the track monitor.
  //

                public TrackMonitorSignalAspect TranslateTMAspect(SignalHead.SIGASP SigState)
                {
                        switch (SigState)
                        {
                                case SignalHead.SIGASP.STOP:
                                        if (hasPermission == Signal.PERMISSION.GRANTED)
                                                return TrackMonitorSignalAspect.Warning;
                                        else
                                                return TrackMonitorSignalAspect.Stop;
                                case SignalHead.SIGASP.STOP_AND_PROCEED:
                                case SignalHead.SIGASP.RESTRICTING:
                                                return TrackMonitorSignalAspect.Warning;
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
                        SPEED,
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
                public ObjectSpeedInfo[] speed_info;      // speed limit info (per aspect)

                public SignalObject mainSignal;        //  This is the signal which this head forms a part.

  //================================================================================================//
  //
  // Constructor for signals
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

                        Array sigasp_values = SIGASP.GetValues(typeof (SIGASP));
                        speed_info    = new ObjectSpeedInfo[sigasp_values.Length];
                }

  //================================================================================================//
  //
  // Constructor for speedposts
  //

                public SignalHead(SignalObject sigOoject, int trItem, int TDBRef, SpeedPostItem speedItem)
                {
                        mainSignal = sigOoject;
                        trItemIndex = trItem;
                        TDBIndex = TDBRef;
                        draw_state = 1;
                        state      = SIGASP.CLEAR_2;
                        signalType = new SignalType(SignalType.FnTypes.Speed, SIGASP.CLEAR_2);

                        TrackJunctionNode = 0;
                        JunctionMainNode  = 0;

                        Array sigasp_values = SIGASP.GetValues(typeof (SIGASP));
                        speed_info    = new ObjectSpeedInfo[sigasp_values.Length];

                        float speedMpS = MpS.ToMpS(speedItem.SpeedInd, !speedItem.IsMPH);
                        if (speedItem.IsResume) speedMpS = 999f;

                        float passSpeed = speedItem.IsPassenger ? speedMpS : -1;
                        float freightSpeed = speedItem.IsFreight ? speedMpS : -1;
                        ObjectSpeedInfo speedinfo = new ObjectSpeedInfo(passSpeed, freightSpeed, false);
                        speed_info[Convert.ToInt32(state)] = speedinfo;
                }

#if DUMP_DISPATCHER
                public void Dump(StringBuilder sta)
                {
                    sta.AppendFormat("||SignalType.Name|{0}\r\n", signalType.Name);
                    sta.AppendFormat("||SignalType.SigFn|{0}\r\n", signalType.FnType);
                    sta.AppendFormat("||TrackJunctionNode|{0}\r\n", TrackJunctionNode);
                    sta.AppendFormat("||JunctionMainNode|{0}\r\n", JunctionMainNode);
                    sta.AppendFormat("||JunctionPath|{0}\r\n", JunctionPath);
                    sta.AppendFormat("||route_set()|{0}\r\n", route_set());
                    sta.AppendFormat("||next_sig_lr()|{0}\r\n", next_sig_lr(SIGFN.NORMAL));
                    sta.AppendFormat("||state|{0}\r\n", this.state);
                    sta.AppendLine();
                }
#endif
  //================================================================================================//
  //
  // SetSignalType : This method sets the signal type object from the CIGCFG file
  //

                public void SetSignalType(TrItem[] TrItems, SIGCFGFile sigCFG)
                {
                        SignalItem sigItem = (SignalItem)TrItems[TDBIndex];

  // set signal type
                        if (sigCFG.SignalTypes.ContainsKey(sigItem.SignalType))
                        {
                                signalType = sigCFG.SignalTypes[sigItem.SignalType];

  // set signal speeds
                                foreach (SignalAspect thisAspect in signalType.Aspects)
                                {
                                        int arrindex = Convert.ToInt32(thisAspect.Aspect);
                                        speed_info[arrindex] = new ObjectSpeedInfo(thisAspect.SpeedMpS, thisAspect.SpeedMpS, thisAspect.Asap);
                                }

  // update overall SignalNumClearAhead
                                mainSignal.SignalNumClearAhead = Math.Max(mainSignal.SignalNumClearAhead, signalType.NumClearAhead);

                        }
                        else
                        {
                            Trace.TraceWarning("Signal {0} at track node {1} has head with invalid type {2}", mainSignal.trItem, mainSignal.trackNode, sigItem.SignalType);
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
#if CHECKED_ROUTE_SET
                        else
                        {
                            juncfound = mainSignal.route_set();
                        }
#endif
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

                public void Reset(Traveller tdbTraveller, bool askPermission)
                {
                        if (signals != null)
                        {
                                nextSigRef = signals.FindNextSignal(tdbTraveller);
                                SetSignalState(SIGNALSTATE.CLEAR);
                                TrackStateChanged();
                                rearSigRef = -2;
                                prevSigRef = -2;
                                if (nextSigRef >= 0 && askPermission) signalObjects[nextSigRef].hasPermission = Signal.PERMISSION.GRANTED;
                        }
                }//Reset

                //================================================================================================//
                //
                //   Reset : This method is invoked if the train has been removed.
                //   Ensures that occupancy is updated to disappear.
                //
                //================================================================================================//
  //
  //   Clear : This method is invoked if the train has been removed.
  //   Ensures that occupancy is updated to disappear.
  //

                public void Clear()
                {
                    if (prevSigRef >= 0) signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.CLEAR;
                }//Clear

  //================================================================================================//
  //
  // UpdateTrackOccupancy : update track state
  //

                public void UpdateTrackOcupancy(Traveller rearTDBTraveller)
                {
                        if (rearSigRef < -1)
                        {
                                if (signals != null)
                                {
                                        rearSigRef = signals.FindNextSignal(rearTDBTraveller);
                                        if ((rearSigRef >= 0) && (rearSigRef != nextSigRef))
                                        {
                                                signalObjects[rearSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;  // Train spans signal
                                                signalObjects[rearSigRef].Update();
                                        }
                                }
                        }
                        if (prevSigRef < -1)
                        {
                                if (signals != null)
                                {
                                        prevSigRef = signals.FindPrevSignal(rearTDBTraveller);
                                        if (prevSigRef >= 0)
                                        {
                                            signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;
                                            signalObjects[prevSigRef].Update();
                                        }
                                }
                        }
                    // By GeorgeS
                        else
                        {
                                if (signals != null)
                                {
                                        int newprevSigRef = signals.FindPrevSignal(rearTDBTraveller);
                                        if (newprevSigRef != prevSigRef)
                                        {
                                                if (prevSigRef > 0)
                                                {
                                                    signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.CLEAR;
                                                }
                                                prevSigRef = newprevSigRef;
                                                if (prevSigRef > 0)
                                                {
                                                    signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;
                                                    signalObjects[prevSigRef].Update();
                                                }
                                        }
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
                                    // By GeorgeS    
                                        if (prevSigRef >= 0)
                                        {
                                            signalObjects[prevSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;
                                            signalObjects[prevSigRef].Update();
                                        }

                                        rearSigRef = signals.FindNextSignal(rearTDBTraveller);
                                        if ((rearSigRef >= 0) && (rearSigRef != nextSigRef))
                                        {
                                                signalObjects[rearSigRef].blockState = SignalObject.BLOCKSTATE.OCCUPIED;  // Train spans signal
                                                signalObjects[rearSigRef].Update();
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

                public float DistanceToSignal(Traveller tdbTraverler)
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
                public bool [] FlagsSetBackfacing;                // Flags signal-flags which are set
                                                                         //    for backfacing signal
                public List<int> Backfacing = new List<int> ();   // Flags heads which are backfacing

				public STFPositionItem Position;
  //================================================================================================//
  //
  // Constructor
  //

                public SignalWorldObject(MSTS.SignalObj SignalWorldItem,SIGCFGFile sigcfg)
                {
                        MSTS.SignalShape thisCFGShape;

						Position = SignalWorldItem.Position;
                        HeadReference = new Dictionary <uint, uint>();

  // set flags with length to number of possible SubObjects type

                        FlagsSet           = new bool [MSTS.SignalShape.SignalSubObj.SignalSubTypes.Count];
                        FlagsSetBackfacing = new bool [MSTS.SignalShape.SignalSubObj.SignalSubTypes.Count];
                        for (uint iFlag = 0; iFlag < FlagsSet.Length; iFlag++)
                        {
                                FlagsSet[iFlag] = false;
                                FlagsSetBackfacing[iFlag] = false;
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
                                                
                                                if (thisSubObjs.BackFacing)
                                                {
                                                        Backfacing.Add(iHead);
                                                        if (thisSubObjs.SignalSubType >= 1)
                                                        {
                                                                 FlagsSetBackfacing[thisSubObjs.SignalSubType] = true;
                                                        }
                                                }
                                                else if (thisSubObjs.SignalSubType >= 1)
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
                                Trace.TraceWarning("Signal {0} not found in SIGCFG.DAT", SFileName);
                        }
                                
                }


  //================================================================================================//
  //
  // Constructor for copy
  //

                public SignalWorldObject(SignalWorldObject copy)
                {
                        SFileName  = String.Copy(copy.SFileName);
                        Backfacing = copy.Backfacing;

                        HeadsSet = new bool [copy.HeadsSet.Length];
                        FlagsSet = new bool [copy.FlagsSet.Length];
                        FlagsSetBackfacing = new bool [copy.FlagsSet.Length];
                        copy.HeadsSet.CopyTo(HeadsSet, 0);
                        copy.FlagsSet.CopyTo(FlagsSet, 0);
                        copy.FlagsSetBackfacing.CopyTo(FlagsSet, 0);

                        HeadReference = new Dictionary <uint, uint> ();
                        foreach ( KeyValuePair <uint, uint> thisRef in copy.HeadReference)
                        {
                                HeadReference.Add(thisRef.Key, thisRef.Value);
                        }
                }

        }

  //================================================================================================//
  //
  // class ObjectItemInfo
  //
  //================================================================================================//

        public class ObjectItemInfo
        {
                public enum ObjectItemType
                {
                        ANY,
                        SIGNAL,
                        SPEEDLIMIT,
                }

                public enum ObjectItemFindState
                {
                        NONE_FOUND = 0,
                        OBJECT_FOUND = 1,
                        END_OF_TRACK = -1,
                        PASSED_DANGER = -2,
                        PASSED_MAXDISTANCE = -3,
                        TDB_ERROR = -4,
                }

                public ObjectItemType               ObjectType;                     // type information

                public SignalObject                 ObjectDetails;                  // actual object 

                public float                        distance_to_train;
                public float                        distance_to_object;

                public SignalHead.SIGASP            signal_state;                   // UNKNOWN if type = speedlimit
                                                                                    // set active by TRAIN
                public float                        speed_passenger;                // -1 if not set
                public float                        speed_freight;                  // -1 if not set
                public uint                         speed_flag;
                public float                        actual_speed;                   // set active by TRAIN

  //================================================================================================//
  //
  // Constructor
  //

                public ObjectItemInfo(char reference, char found_type, SignalObject thisObject, float distance)
                {
                        ObjectSpeedInfo speed_info;

                        if (reference == 'T')
                        {
                                distance_to_train = distance;
                                distance_to_object = -1;
                        }
                        else
                        {
                                distance_to_train = -1;
                                distance_to_object = distance;
                        }

                        ObjectDetails = thisObject;

                        if (found_type == 'S')
                        {
                                ObjectType = ObjectItemType.SIGNAL;
                                signal_state = SignalHead.SIGASP.UNKNOWN;  // set active by TRAIN
                                speed_passenger = -1;                      // set active by TRAIN
                                speed_freight   = -1;                      // set active by TRAIN
                                speed_flag      = 0;                       // set active by TRAIN
                                }
                        else
                        {
                                ObjectType = ObjectItemType.SPEEDLIMIT;
                                signal_state = SignalHead.SIGASP.UNKNOWN;
                                speed_info = thisObject.this_lim_speed(SignalHead.SIGFN.SPEED);
                        speed_passenger = speed_info.speed_pass;
                        speed_freight   = speed_info.speed_freight;
                        speed_flag      = speed_info.speed_flag;
                }
        }
        }

  //================================================================================================//
  //
  // class ObjectSpeedInfo
  //
  //================================================================================================//

        public class ObjectSpeedInfo
        {

                public float         speed_pass;
                public float         speed_freight;
                public uint          speed_flag;

  //================================================================================================//
  //
  // Constructor
  //

                public ObjectSpeedInfo(float pass, float freight, bool asap)
                {
                        speed_pass    = pass;
                        speed_freight = freight;
                        if (asap)
                        {
                                 speed_flag = 1;
                        }
                }
        }

  //================================================================================================//

}

