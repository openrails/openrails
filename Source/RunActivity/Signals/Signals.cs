// COPYRIGHT 2013 by the Open Rails project.
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

// This module covers all classes and code for signal, speed post, track occupation and track reservation control

// Debug flags :
// #define DEBUG_PRINT
// #define DEBUG_REPORTS
// #define DEBUG_DEADLOCK
// prints details of the derived signal structure

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

        public TrackDB trackDB;
        private TSectionDatFile tsectiondat;
        private TDBFile tdbfile;

        private SignalObject[] signalObjects;
        private List<SignalWorldObject> SignalWorldList = new List<SignalWorldObject>();
        private Dictionary<uint, SignalRefObject> SignalRefList;
        private Dictionary<uint, SignalObject> SignalHeadList;
        public static SIGSCRfile scrfile;

        public int noSignals = 0;
        private int foundSignals = 0;

        private static int updatecount = 0;

        public List<TrackCircuitSection> TrackCircuitList;
        private Dictionary<int, CrossOverItem> CrossOverList = new Dictionary<int, CrossOverItem>();
        public List<PlatformDetails> PlatformDetailsList = new List<PlatformDetails>();
        public Dictionary<int, int> PlatformXRefList = new Dictionary<int, int>();

        //================================================================================================//
        ///
        /// Constructor
        ///

        public Signals(Simulator simulator, SIGCFGFile sigcfg)
        {

#if DEBUG_REPORTS
            File.Delete(@"C:\temp\printproc.txt");
#endif

            SignalRefList = new Dictionary<uint, SignalRefObject>();
            SignalHeadList = new Dictionary<uint, SignalObject>();
            Dictionary<int, int> platformList = new Dictionary<int, int>();

            trackDB = simulator.TDB.TrackDB;
            tsectiondat = simulator.TSectionDat;
            tdbfile = Program.Simulator.TDB;

            // read SIGSCR files

            Trace.Write(" SIGSCR ");
            scrfile = new SIGSCRfile(simulator.RoutePath, sigcfg.ScriptFiles, sigcfg.SignalTypes);

            // build list of signal world file information

            BuildSignalWorld(simulator, sigcfg);

            // build list of signals in TDB file

            BuildSignalList(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat, tdbfile, platformList);

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

            if (SignalObjects != null)
                SetNumSignalHeads();

            //
            // Create trackcircuit database
            //

            CreateTrackCircuits(trackDB.TrItemTable, trackDB.TrackNodes,
                                       tsectiondat, tdbfile);

            //
            // Process platform information
            //

            ProcessPlatforms(platformList, trackDB.TrItemTable, trackDB.TrackNodes);

            //
            // Print all info (DEBUG only)
            //

#if DEBUG_PRINT

            PrintTCBase(trackDB.TrackNodes);

            if (File.Exists(@"C:\temp\SignalObjects.txt"))
            {
                File.Delete(@"C:\temp\SignalObjects.txt");
            }
            if (File.Exists(@"C:\temp\SignalShapes.txt"))
            {
                File.Delete(@"C:\temp\SignalShapes.txt");
            }

            for (int isignal = 0; isignal < signalObjects.Length - 1; isignal++)
            {
                SignalObject singleSignal = signalObjects[isignal];
                if (singleSignal == null)
                {
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "\nInvalid entry : " + isignal.ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "\nSignal ref item     : " + singleSignal.thisRef.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "Track node + index  : " + singleSignal.trackNode.ToString() + " + " +
                                                                                    singleSignal.trRefIndex.ToString() + "\n");

                    foreach (SignalHead thisHead in singleSignal.SignalHeads)
                    {
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "Type name           : " + thisHead.signalType.Name.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "Type                : " + thisHead.signalType.FnType.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "item Index          : " + thisHead.trItemIndex.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "TDB  Index          : " + thisHead.TDBIndex.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "Junction Main Node  : " + thisHead.JunctionMainNode.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\SignalObjects.txt", "Junction Path       : " + thisHead.JunctionPath.ToString() + "\n");
                    }

                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "TC Reference   : " + singleSignal.TCReference.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "TC Direction   : " + singleSignal.TCDirection.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "TC Position    : " + singleSignal.TCOffset.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalObjects.txt", "TC TCNextTC    : " + singleSignal.TCNextTC.ToString() + "\n");
                }
            }

            foreach (KeyValuePair<string, MSTS.SignalShape> sshape in sigcfg.SignalShapes)
            {
                File.AppendAllText(@"C:\temp\SignalShapes.txt", "\n==========================================\n");
                File.AppendAllText(@"C:\temp\SignalShapes.txt", "Shape key   : " + sshape.Key.ToString() + "\n");
                MSTS.SignalShape thisshape = sshape.Value;
                File.AppendAllText(@"C:\temp\SignalShapes.txt", "Filename    : " + thisshape.ShapeFileName.ToString() + "\n");
                File.AppendAllText(@"C:\temp\SignalShapes.txt", "Description : " + thisshape.Description.ToString() + "\n");

                foreach (MSTS.SignalShape.SignalSubObj ssobj in thisshape.SignalSubObjs)
                {
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "\nSubobj Index : " + ssobj.Index.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "Matrix       : " + ssobj.MatrixName.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "Description  : " + ssobj.Description.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "Sub Type (I) : " + ssobj.SignalSubType.ToString() + "\n");
                    if (ssobj.SignalSubSignalType != null)
                    {
                        File.AppendAllText(@"C:\temp\SignalShapes.txt", "Sub Type (C) : " + ssobj.SignalSubSignalType.ToString() + "\n");
                    }
                    else
                    {
                        File.AppendAllText(@"C:\temp\SignalShapes.txt", "Sub Type (C) : not set \n");
                    }
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "Optional     : " + ssobj.Optional.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "Default      : " + ssobj.Default.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "BackFacing   : " + ssobj.BackFacing.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\SignalShapes.txt", "JunctionLink : " + ssobj.JunctionLink.ToString() + "\n");
                }
                File.AppendAllText(@"C:\temp\SignalShapes.txt", "\n==========================================\n");
            }
#endif

            // Clear world lists to save memory

            SignalWorldList.Clear();
            SignalRefList.Clear();
            SignalHeadList.Clear();

            if (SignalObjects != null)
            {
                foreach (SignalObject thisSignal in SignalObjects)
                {
                    if (thisSignal != null)
                    {
                        if (thisSignal.isSignalNormal())
                        {
                            if (thisSignal.TCNextTC < 0)
                            {
                                Trace.TraceInformation("Signal " + thisSignal.thisRef.ToString() +
                                    " ; TC : " + thisSignal.TCReference.ToString() +
                                    " ; NextTC : " + thisSignal.TCNextTC.ToString() +
                                    " ; TN : " + thisSignal.trackNode.ToString());
                            }

                            if (thisSignal.TCReference < 0) // signal is not on any track - remove it!
                            {
                                Trace.TraceInformation("Signal removed " + thisSignal.thisRef.ToString() +
                                    " ; TC : " + thisSignal.TCReference.ToString() +
                                    " ; NextTC : " + thisSignal.TCNextTC.ToString() +
                                    " ; TN : " + thisSignal.trackNode.ToString());
                                SignalObjects[thisSignal.thisRef] = null;
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        ///
        /// Overlay constructor for restore after saved game
        ///

        public Signals(Simulator simulator, SIGCFGFile sigcfg, BinaryReader inf)
            : this(simulator, sigcfg)
        {
            int signalRef = inf.ReadInt32();
            while (signalRef >= 0)
            {
                SignalObject thisSignal = SignalObjects[signalRef];
                thisSignal.Restore(inf);
                signalRef = inf.ReadInt32();
            }

            int tcListCount = inf.ReadInt32();

            if (tcListCount != TrackCircuitList.Count)
            {
                Trace.TraceError("Mismatch between saved : {0} and existing : {1} TrackCircuits",
                        tcListCount.ToString(), TrackCircuitList.Count.ToString());
                throw new InvalidDataException("Cannot resume route due to altered data");
            }
            else
            {
                foreach (TrackCircuitSection thisSection in TrackCircuitList)
                {
                    thisSection.Restore(inf);
                }
            }
        }

        //================================================================================================//
        //
        // Restore Train links
        // Train links must be restored separately as Trains is restored later as Signals
        //

        public void RestoreTrains(List<Train> trains)
        {
            foreach (TrackCircuitSection thisSection in TrackCircuitList)
            {
                thisSection.CircuitState.RestoreTrains(this, trains);
            }

            // restore train information

            if (SignalObjects != null)
            {
                foreach (SignalObject thisSignal in SignalObjects)
                {
                    if (thisSignal != null)
                    {
                        thisSignal.RestoreTrains(trains);
                    }
                }

                // restore correct aspects
                foreach (SignalObject thisSignal in SignalObjects)
                {
                    if (thisSignal != null)
                    {
                        thisSignal.RestoreAspect();
                    }
                }
            }
        }

        //================================================================================================//
        ///
        /// Save game
        ///

        public void Save(BinaryWriter outf)
        {
            if (SignalObjects != null)
            {
                foreach (SignalObject thisSignal in SignalObjects)
                {
                    if (thisSignal != null)
                    {
                        outf.Write(thisSignal.thisRef);
                        thisSignal.Save(outf);
                    }
                }
            }
            outf.Write(-1);

            outf.Write(TrackCircuitList.Count);
            foreach (TrackCircuitSection thisSection in TrackCircuitList)
            {
                thisSection.Save(outf);
            }
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
            string[] FileEntries = Directory.GetFiles(WFilePath);

            List<TokenID> Tokens = new List<TokenID>();
            Tokens.Add(TokenID.Signal);

            // loop through files, use only extention .w, skip w+1000000+1000000.w file

            foreach (string fileName in FileEntries)
            {
                string[] fparts = fileName.Split('.');
                if (fparts.Length < 2)
                    continue;
                string[] fparts2 = fparts[fparts.Length - 2].Split('\\');

                // check if valid file

                try
                {
                    int p = fileName.ToUpper().LastIndexOf("\\WORLD\\W");
                    int TileX = int.Parse(fileName.Substring(p + 8, 7));
                    int TileZ = int.Parse(fileName.Substring(p + 15, 7));
                }
                catch (Exception)
                {
                    continue;
                }

                if (string.Compare(fparts[fparts.Length - 1], "w") == 0)
                {

                    // read w-file, get SignalObjects only

                    Trace.Write("W");
                    WFile WFile = new WFile(fileName, Tokens);

                    // loop through all signals

                    foreach (WorldObject worldObject in WFile.Tr_Worldfile)
                    {
                        if (worldObject.GetType() == typeof(MSTS.SignalObj))
                        {
                            MSTS.SignalObj thisWorldObject = worldObject as MSTS.SignalObj;
                            SignalWorldObject SignalWorldSignal = new SignalWorldObject(thisWorldObject, sigcfg);
                            SignalWorldList.Add(SignalWorldSignal);
                            foreach (KeyValuePair<uint, uint> thisref in SignalWorldSignal.HeadReference)
                            {
                                int thisSignalCount = SignalWorldList.Count() - 1;    // Index starts at 0
                                SignalRefObject thisRefObject = new SignalRefObject(thisSignalCount, thisref.Value);
                                if (SignalRefList.ContainsKey(thisref.Key))
                                {
                                    SignalRefObject DoubleObject = SignalRefList[thisref.Key];
                                }
                                else
                                {
                                    SignalRefList.Add(thisref.Key, thisRefObject);
                                }
                            }
                        }
                    }

                    // clear worldfile info

                    WFile = null;
                }
            }
            Trace.Write("\n");

#if DEBUG_PRINT
            foreach (KeyValuePair<uint, SignalRefObject> thisref in SignalRefList)
            {
                uint headref;
                uint TBDRef = thisref.Key;
                SignalRefObject signalRef = thisref.Value;

                SignalWorldObject reffedObject = SignalWorldList[(int)signalRef.SignalWorldIndex];
                if (!reffedObject.HeadReference.TryGetValue(TBDRef, out headref))
                {
                    File.AppendAllText(@"WorldSignalList.txt", "Incorrect Ref : " + TBDRef.ToString() + "\n");
                    foreach (KeyValuePair<uint, uint> headindex in reffedObject.HeadReference)
                    {
                        File.AppendAllText(@"WorldSignalList.txt", "TDB : " + headindex.Key.ToString() +
                                                " + " + headindex.Value.ToString() + "\n");
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
            if (MultiPlayer.MPManager.IsClient()) return; //in MP, client will not update

            if (foundSignals > 0)
            {

                // loop through all signals
                // update required part

                int totalSignal = signalObjects.Length - 1;
                int updatestep = (totalSignal / 20) + 1;
                for (int icount = updatecount; icount < Math.Min(totalSignal, updatecount + updatestep); icount++)
                {
                    SignalObject signal = signalObjects[icount];
                    if (signal != null) // to cater for orphans
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

        private void BuildSignalList(TrItem[] TrItems, TrackNode[] trackNodes, TSectionDatFile tsectiondat,
                TDBFile tdbfile, Dictionary<int, int> platformList)
        {

            //  Determaine the number of signals in the track Objects list

            noSignals = 0;
            if (TrItems == null)
                return;                // No track Objects in route.
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
                        SpeedPostItem Speedpost = (SpeedPostItem)trItem;
                        if (Speedpost.IsLimit)
                        {
                            noSignals++;
                        }
                    }
                }
            }

            //  Only continue if one or more signals in route.

            if (noSignals > 0)
            {
                signalObjects = new SignalObject[noSignals];
                SignalObject.trackNodes = trackNodes;
                SignalObject.signalObjects = signalObjects;
                SignalObject.trItems = TrItems;

                for (int i = 1; i < trackNodes.Length; i++)
                {
                    ScanSection(TrItems, trackNodes, i, tsectiondat, tdbfile, platformList);
                }

                // using world cross-reference list, merge heads to single signal

                MergeHeads();

                // rebuild list - clear out null elements

                int firstfree = -1;
                for (int iSignal = 0; iSignal < SignalObjects.Length; iSignal++)
                {
                    if (SignalObjects[iSignal] == null && firstfree < 0)
                    {
                        firstfree = iSignal;
                    }
                    else if (SignalObjects[iSignal] != null && firstfree >= 0)
                    {
                        SignalObjects[firstfree] = SignalObjects[iSignal];
                        SignalObjects[iSignal] = null;
                        firstfree++;
                    }
                }

                if (firstfree < 0)
                    firstfree = SignalObjects.Length - 1;

                // restore all links and indices

                for (int iSignal = 0; iSignal < SignalObjects.Length; iSignal++)
                {
                    if (SignalObjects[iSignal] != null)
                    {
                        SignalObject thisObject = SignalObjects[iSignal];
                        thisObject.thisRef = iSignal;

                        foreach (SignalHead thisHead in thisObject.SignalHeads)
                        {
                            thisHead.mainSignal = thisObject;
                            var trackItem = TrItems[thisHead.TDBIndex];
                            if (trackItem is SignalItem)
                            {
                                SignalItem sigItem = trackItem as SignalItem;
                                sigItem.sigObj = thisObject.thisRef;
                            }
                            else if (trackItem is SpeedPostItem)
                            {
                                SpeedPostItem speedItem = trackItem as SpeedPostItem;
                                speedItem.sigObj = thisObject.thisRef;
                            }
                        }
                    }
                }

                foundSignals = firstfree;

            }
            else
            {
                signalObjects = new SignalObject[0];
            }

        } //BuildSignalList


        //================================================================================================//
        ///
        /// Split backfacing signals
        ///

        private void SplitBackfacing(TrItem[] TrItems, TrackNode[] TrackNodes)
        {

            List<SignalObject> newSignals = new List<SignalObject>();
            int newindex = foundSignals + 1;

            //
            // Loop through all signals to check on Backfacing heads
            //

            for (int isignal = 0; isignal < signalObjects.Length - 1; isignal++)
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

                    newSignal.WorldObject.FlagsSet = new bool[singleSignal.WorldObject.FlagsSetBackfacing.Length];
                    singleSignal.WorldObject.FlagsSetBackfacing.CopyTo(newSignal.WorldObject.FlagsSet, 0);

                    for (int iindex = 0; iindex < newSignal.WorldObject.HeadsSet.Length; iindex++)
                    {
                        newSignal.WorldObject.HeadsSet[iindex] = false;
                    }

                    //
                    // loop through the list with headreferences, check this agains the list with backfacing heads
                    // use the TDBreference to find the actual head
                    //

                    List<int> removeHead = new List<int>();  // list to keep trace of heads which are moved //

                    foreach (KeyValuePair<uint, uint> thisHeadRef in singleSignal.WorldObject.HeadReference)
                    {
                        for (int iindex = singleSignal.WorldObject.Backfacing.Count - 1; iindex >= 0; iindex--)
                        {
                            int ihead = singleSignal.WorldObject.Backfacing[iindex];
                            if (thisHeadRef.Value == ihead)
                            {
                                for (int ihIndex = 0; ihIndex < singleSignal.SignalHeads.Count; ihIndex++)
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
                    // check if there were actually any backfacing signal heads
                    //

                    if (removeHead.Count > 0)
                    {

                        //
                        // remove moved heads from existing signal
                        //

                        for (int ihead = singleSignal.SignalHeads.Count - 1; ihead >= 0; ihead--)
                        {
                            if (removeHead.Contains(ihead))
                            {
                                singleSignal.SignalHeads.RemoveAt(ihead);
                            }
                        }

                        //
                        // Check direction of heads to set correct direction for signal
                        //

                        if (singleSignal.SignalHeads.Count > 0)
                        {
                            SignalItem thisItemOld = TrItems[singleSignal.SignalHeads[0].TDBIndex] as SignalItem;
                            if (singleSignal.direction != thisItemOld.Direction)
                            {
                                singleSignal.direction = (int)thisItemOld.Direction;
                                singleSignal.tdbtraveller.ReverseDirection();                           // reverse //
                            }
                        }

                        SignalItem thisItemNew = TrItems[newSignal.SignalHeads[0].TDBIndex] as SignalItem;
                        if (newSignal.direction != thisItemNew.Direction)
                        {
                            newSignal.direction = (int)thisItemNew.Direction;
                            newSignal.tdbtraveller.ReverseDirection();                           // reverse //
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
                                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                            sigItem.sigObj = newSignal.thisRef;
                                            newSignal.trRefIndex = i;

                                            // remove this key from the original signal //

                                            singleSignal.WorldObject.HeadReference.Remove((uint)TDBRef);
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
                                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                            sigItem.sigObj = singleSignal.thisRef;
                                            singleSignal.trRefIndex = i;

                                            // remove this key from the new signal //

                                            newSignal.WorldObject.HeadReference.Remove((uint)TDBRef);
                                        }
                                    }
                                }
                            }
                        }

                        //
                        // add new signal to signal list
                        //

                        newindex++;
                        newSignals.Add(newSignal);

                        //
                        // revert existing signal to NULL if no heads remain
                        //

                        if (singleSignal.SignalHeads.Count <= 0)
                        {
                            signalObjects[isignal] = null;
                        }
                    }
                }
            }

            //
            // add all new signals to the signalObject array
            // length of array was set to all possible signals, so there will be space to spare
            //

            newindex = foundSignals + 1;
            foreach (SignalObject newSignal in newSignals)
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
                               TSectionDatFile tsectiondat, TDBFile tdbfile, Dictionary<int, int> platformList)
        {
            int lastSignal = -1;                // Index to last signal found in path -1 if none

            if (trackNodes[index].TrEndNode)
                return;

            //  Is it a vector node then it may contain objects.
            if (trackNodes[index].TrVectorNode != null && trackNodes[index].TrVectorNode.noItemRefs > 0)
            {
                // Any obects ?
                for (int i = 0; i < trackNodes[index].TrVectorNode.noItemRefs; i++)
                {
                    if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]] != null)
                    {
                        int TDBRef = trackNodes[index].TrVectorNode.TrItemRefs[i];

                        // Track Item is signal
                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                        {
                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                            sigItem.sigObj = foundSignals;

                            if (sigItem.noSigDirs > 0)
                            {
                                SignalItem.strTrSignalDir sigTrSignalDirs = sigItem.TrSignalDirs[0];
                            }

                            bool validSignal = true;
                            lastSignal = AddSignal(index, i, sigItem, lastSignal,
                                                    TrItems, trackNodes, TDBRef, tsectiondat, tdbfile, ref validSignal);

                            if (validSignal)
                            {
                                sigItem.sigObj = lastSignal;
                            }
                            else
                            {
                                sigItem.sigObj = -1;
                            }
                        }

        // Track Item is speedpost - check if really limit
                        else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSPEEDPOST)
                        {
                            SpeedPostItem speedItem = (SpeedPostItem)TrItems[TDBRef];
                            if (speedItem.IsLimit)
                            {
                                speedItem.sigObj = foundSignals;

                                lastSignal = AddSpeed(index, i, speedItem, lastSignal,
                                                 TrItems, trackNodes, TDBRef, tsectiondat, tdbfile);
                                speedItem.sigObj = lastSignal;

                            }
                        }
                        else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trPLATFORM)
                        {
                            if (platformList.ContainsKey(TDBRef))
                            {
                                Trace.TraceInformation("Double reference to platform ID " + TDBRef.ToString() +
                                    " in nodes " + platformList[TDBRef].ToString() + " and " + index.ToString() + "\n");
                            }
                            else
                            {
                                platformList.Add(TDBRef, index);
                            }
                        }
                        else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIDING)
                        {
                            if (platformList.ContainsKey(TDBRef))
                            {
                                Trace.TraceInformation("Double reference to siding ID " + TDBRef.ToString() +
                                    " in nodes " + platformList[TDBRef].ToString() + " and " + index.ToString() + "\n");
                            }
                            else
                            {
                                platformList.Add(TDBRef, index);
                            }
                        }
                    }
                }
            }
        }   //ScanSection 

        //================================================================================================//
        //
        // Merge Heads
        //

        public void MergeHeads()
        {
            //            foreach (SignalWorldObject thisWorldObject in SignalWorldList)
            //            {
            for (int iWorldIndex = 0; iWorldIndex < SignalWorldList.Count; iWorldIndex++)
            {
                SignalWorldObject thisWorldObject = SignalWorldList[iWorldIndex];
                SignalObject MainSignal = null;

                if (thisWorldObject.HeadReference.Count > 1)
                {
                    
                    foreach (KeyValuePair<uint, uint> thisReference in thisWorldObject.HeadReference)
                    {
                        if (SignalHeadList.ContainsKey(thisReference.Key))
                        {
                            if (MainSignal == null)
                            {
                                MainSignal = SignalHeadList[thisReference.Key];
                            }
                            else
                            {
                                SignalObject AddSignal = SignalHeadList[thisReference.Key];
                                foreach (SignalHead thisHead in AddSignal.SignalHeads)
                                {
                                    MainSignal.SignalHeads.Add(thisHead);
                                    SignalObjects[AddSignal.thisRef] = null;
                                }
                            }
                        }
                        else
                        {
                            Trace.TraceInformation("Signal found in Worldfile but not in TDB - TDB Index : " +
                                thisReference.Key.ToString());
                            MainSignal = null;
                        }
                    }
                }
            }
        }

        //================================================================================================//
        ///
        /// This method adds a new Signal to the list
        ///

        private int AddSignal(int trackNode, int nodeIndx, SignalItem sigItem, int prevSignal,
                        TrItem[] TrItems, TrackNode[] trackNodes, int TDBRef, TSectionDatFile tsectiondat, TDBFile tdbfile, ref bool validSignal)
        {
            validSignal = true;

            signalObjects[foundSignals] = new SignalObject();
            signalObjects[foundSignals].isSignal = true;
            signalObjects[foundSignals].direction = (int)sigItem.Direction;
            signalObjects[foundSignals].trackNode = trackNode;
            signalObjects[foundSignals].trRefIndex = nodeIndx;
            signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, sigItem);
            signalObjects[foundSignals].thisRef = foundSignals;
            signalObjects[foundSignals].signalRef = this;

            if (tdbfile.TrackDB.TrackNodes[trackNode] == null || tdbfile.TrackDB.TrackNodes[trackNode].TrVectorNode == null)
            {
                validSignal = false;
                Trace.TraceInformation("Reference to invalid track node " + trackNode.ToString() + " for Signal " + TDBRef.ToString() + "\n");
            }
            else
            {
                signalObjects[foundSignals].tdbtraveller =
                new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                        sigItem.TileX, sigItem.TileZ, sigItem.X, sigItem.Z,
                (Traveller.TravellerDirection)(1 - sigItem.Direction));
            }

            signalObjects[foundSignals].WorldObject = null;

            if (SignalHeadList.ContainsKey((uint)TDBRef))
            {
                validSignal = false;
                Trace.TraceInformation("Invalid double TBDRef " + TDBRef.ToString() + " in node " + trackNode.ToString() + "\n");
            }

            if (!validSignal)
            {
                signalObjects[foundSignals] = null;  // reset signal, do not increase signal count
            }
            else
            {
                SignalHeadList.Add((uint)TDBRef, signalObjects[foundSignals]);
                foundSignals++;
            }

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
            signalObjects[foundSignals].isSignal = false;
            signalObjects[foundSignals].direction = 0;                  // preset - direction not yet known //
            signalObjects[foundSignals].trackNode = trackNode;
            signalObjects[foundSignals].trRefIndex = nodeIndx;
            signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, speedItem);
            signalObjects[foundSignals].thisRef = foundSignals;
            signalObjects[foundSignals].signalRef = this;

            signalObjects[foundSignals].tdbtraveller =
            new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                    speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z,
                    (Traveller.TravellerDirection)signalObjects[foundSignals].direction);

            double delta_angle = signalObjects[foundSignals].tdbtraveller.RotY - ((Math.PI / 2) - speedItem.Angle);
            float delta_float = (float)delta_angle;
            MSTSMath.M.NormalizeRadians(ref delta_float);
            if (Math.Abs(delta_float) < (Math.PI / 2))
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
            dumpstring = String.Concat(dumpstring, " at : ");
            dumpstring = String.Concat(dumpstring, speedItem.TileX.ToString(), " ");
            dumpstring = String.Concat(dumpstring, speedItem.TileZ.ToString(), ":");
            dumpstring = String.Concat(dumpstring, speedItem.X.ToString(), " ");
            dumpstring = String.Concat(dumpstring, speedItem.Z.ToString(), " ");
            dumpstring = String.Concat(dumpstring, "; angle - track : ");
            dumpstring = String.Concat(dumpstring, speedItem.Angle.ToString(), ":",
                            signalObjects[foundSignals].tdbtraveller.RotY.ToString());
            dumpstring = String.Concat(dumpstring, "; delta : ", delta_angle.ToString());
            dumpstring = String.Concat(dumpstring, "; dir : ", signalObjects[foundSignals].direction.ToString());
            File.AppendAllText(@"C:\temp\speedpost.txt", dumpstring + "\n");
#endif

            signalObjects[foundSignals].WorldObject = null;
            foundSignals++;
            return foundSignals - 1;
        } // AddSpeed

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
                                signal.WorldObject = SignalWorldList[(int)signalIndex];
                            }
                            SignalRefList.Remove(TDBRef);
                        }
                    }
                }
            }

        }//AddWorldInfo

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
        /// 
        /// Count number of normal signal heads
        ///

        public void SetNumSignalHeads()
        {
            foreach (SignalObject thisSignal in signalObjects)
            {
                if (thisSignal != null)
                {
                    foreach (SignalHead thisHead in thisSignal.SignalHeads)
                    {
                        if (thisHead.sigFunction == SignalHead.SIGFN.NORMAL)
                        {
                            thisSignal.SignalNumNormalHeads++;
                        }
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Find_Next_Object_InRoute : find next item along path of train - using Route List (only forward)
        // Objects to search for : SpeedPost, Normal Signal
        //
        // Usage :
        //   always set : RouteList, RouteNodeIndex, distance along RouteNode, fnType
        //
        //   from train :
        //     optional : maxdistance
        //
        // returned :
        //   >= 0 : signal object reference
        //   -1  : end of track 
        //   -3  : no item within required distance
        //   -5  : end of authority
        //   -6  : end of (sub)route
        //

        public TrackCircuitSignalItem Find_Next_Object_InRoute(Train.TCSubpathRoute routePath,
                int routeIndex, float routePosition, float maxDistance, SignalHead.SIGFN fn_type)
        {

            ObjectItemInfo.ObjectItemFindState locstate = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;
            // local processing state     //

            int actRouteIndex = routeIndex;      // present node               //
            Train.TCRouteElement thisElement = routePath[actRouteIndex];
            int actSection = thisElement.TCSectionIndex;
            int actDirection = thisElement.Direction;
            TrackCircuitSection thisSection = TrackCircuitList[actSection];
            float totalLength = 0;
            float lengthOffset = routePosition;

            SignalObject foundObject = null;
            TrackCircuitSignalItem thisItem = null;

            //
            // loop through trackcircuits until :
            //  - end of track or route is found
            //  - end of authorization is found
            //  - required item is found
            //  - max distance is covered
            //

            while (locstate == ObjectItemInfo.ObjectItemFindState.NONE_FOUND)
            {

                // normal signal
                if (fn_type == SignalHead.SIGFN.NORMAL)
                {
                    if (thisSection.EndSignals[actDirection] != null)
                    {
                        foundObject = thisSection.EndSignals[actDirection];
                        totalLength += (thisSection.Length - lengthOffset);
                        locstate = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                    }
                }

        // speedpost
                else if (fn_type == SignalHead.SIGFN.SPEED)
                {
                    TrackCircuitSignalList thisSpeedpostList =
                               thisSection.CircuitItems.TrackCircuitSpeedPosts[actDirection];
                    locstate = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;

                    for (int iPost = 0;
                             iPost < thisSpeedpostList.TrackCircuitItem.Count &&
                                     locstate == ObjectItemInfo.ObjectItemFindState.NONE_FOUND;
                             iPost++)
                    {
                        TrackCircuitSignalItem thisSpeedpost = thisSpeedpostList.TrackCircuitItem[iPost];
                        if (thisSpeedpost.SignalLocation > lengthOffset)
                        {
                            locstate = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
                            foundObject = thisSpeedpost.SignalRef;
                            totalLength += (thisSpeedpost.SignalLocation - lengthOffset);
                        }
                    }
                }

                // next section accessed via next route element

                if (locstate == ObjectItemInfo.ObjectItemFindState.NONE_FOUND)
                {
                    totalLength += (thisSection.Length - lengthOffset);
                    lengthOffset = 0;

                    int setSection = thisSection.ActivePins[thisElement.OutPin[0], thisElement.OutPin[1]].Link;
                    actRouteIndex++;

                    if (setSection < 0)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.END_OF_AUTHORITY;
                    }
                    else if (actRouteIndex >= routePath.Count)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.END_OF_PATH;
                    }
                    else if (maxDistance > 0 && totalLength > maxDistance)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.PASSED_MAXDISTANCE;
                    }
                    else
                    {
                        thisElement = routePath[actRouteIndex];
                        actSection = thisElement.TCSectionIndex;
                        actDirection = thisElement.Direction;
                        thisSection = TrackCircuitList[actSection];
                    }
                }
            }

            if (foundObject != null)
            {
                thisItem = new TrackCircuitSignalItem(foundObject, totalLength);
            }
            else
            {
                thisItem = new TrackCircuitSignalItem(locstate);
            }

            return (thisItem);
        }

        //================================================================================================//
        //
        // GetNextObject_InRoute : find next item along path of train - using Route List (only forward)
        //
        // Usage :
        //   always set : Train (may be null), RouteList, RouteNodeIndex, distance along RouteNode, fn_type
        //
        //   from train :
        //     optional : maxdistance
        //
        // returned :
        //   >= 0 : signal object reference
        //   -1  : end of track 
        //   -2  : passed signal at danger
        //   -3  : no item within required distance
        //   -5  : end of authority
        //   -6  : end of (sub)route
        //


        // call without position
        public ObjectItemInfo GetNextObject_InRoute(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePath,
                    int routeIndex, float routePosition, float maxDistance, ObjectItemInfo.ObjectItemType req_type)
        {

            Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

            return (GetNextObject_InRoute(thisTrain, routePath, routeIndex, routePosition, maxDistance, req_type, thisPosition));
        }

        // call with position
        public ObjectItemInfo GetNextObject_InRoute(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePath,
                    int routeIndex, float routePosition, float maxDistance, ObjectItemInfo.ObjectItemType req_type,
                    Train.TCPosition thisPosition)
        {

            TrackCircuitSignalItem foundItem = null;

            bool findSignal = false;
            bool findSpeedpost = false;

            float signalDistance = -1f;
            float speedpostDistance = -1f;

            int sigObjRef = 0;
            int speedObjRef = 0;

            if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                req_type == ObjectItemInfo.ObjectItemType.SIGNAL)
            {
                findSignal = true;
            }

            if (req_type == ObjectItemInfo.ObjectItemType.ANY ||
                req_type == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
            {
                findSpeedpost = true;
            }

            Train.TCSubpathRoute usedRoute = routePath;

            // if routeIndex is not valid, build temp route from present position to first node or signal

            if (routeIndex < 0)
            {
                bool thisIsFreight = thisTrain != null ? thisTrain.Train.IsFreight : false;

                List<int> tempSections = ScanRoute(thisTrain.Train, thisPosition.TCSectionIndex,
                    thisPosition.TCOffset, thisPosition.TCDirection,
                    true, 200f, false, true, true, false, true, false, false, true, false, thisIsFreight);


                Train.TCSubpathRoute tempRoute = new Train.TCSubpathRoute();
                int prevSection = -2;

                foreach (int sectionIndex in tempSections)
                {
                    Train.TCRouteElement thisElement =
                        new Train.TCRouteElement(TrackCircuitList[Math.Abs(sectionIndex)],
                            sectionIndex > 0 ? 0 : 1, this, prevSection);
                    tempRoute.Add(thisElement);
                    prevSection = Math.Abs(sectionIndex);
                }
                usedRoute = tempRoute;
                routeIndex = 0;
            }

            // always find signal to check for signal at danger

            ObjectItemInfo.ObjectItemFindState signalState = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;

            TrackCircuitSignalItem nextSignal =
                Find_Next_Object_InRoute(usedRoute, routeIndex, routePosition,
                        maxDistance, SignalHead.SIGFN.NORMAL);

            signalState = nextSignal.SignalState;
            if (nextSignal.SignalState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
            {
                signalDistance = nextSignal.SignalLocation;
                SignalObject foundSignal = nextSignal.SignalRef;
                sigObjRef = foundSignal.thisRef;
                if (foundSignal.this_sig_lr(SignalHead.SIGFN.NORMAL) == SignalHead.SIGASP.STOP)
                {
                    signalState = ObjectItemInfo.ObjectItemFindState.PASSED_DANGER;
                }
                else if (thisTrain != null && foundSignal.enabledTrain != thisTrain)
                {
                    signalState = ObjectItemInfo.ObjectItemFindState.PASSED_DANGER;
                    nextSignal.SignalState = signalState;  // do not return OBJECT_FOUND - signal is not valid
                }

            }

            // look for speedpost only if required

            if (findSpeedpost)
            {
                TrackCircuitSignalItem nextSpeedpost =
                    Find_Next_Object_InRoute(usedRoute, routeIndex, routePosition,
                        maxDistance, SignalHead.SIGFN.SPEED);

                if (nextSpeedpost.SignalState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                {
                    speedpostDistance = nextSpeedpost.SignalLocation;
                    SignalObject foundSignal = nextSpeedpost.SignalRef;
                    speedObjRef = foundSignal.thisRef;
                }


                if (signalDistance > 0 && speedpostDistance > 0)
                {
                    if (signalDistance < speedpostDistance)
                    {
                        if (findSignal)
                        {
                            foundItem = nextSignal;
                        }
                        else
                        {
                            foundItem = nextSpeedpost;
                            if (signalState == ObjectItemInfo.ObjectItemFindState.PASSED_DANGER)
                            {
                                foundItem.SignalState = signalState;
                            }
                        }
                    }
                    else
                    {
                        foundItem = nextSpeedpost;
                    }
                }
                else if (signalDistance > 0)
                {
                    foundItem = nextSignal;
                }
                else if (speedpostDistance > 0)
                {
                    foundItem = nextSpeedpost;
                }
            }
            else if (findSignal)
            {
                foundItem = nextSignal;
            }


            ObjectItemInfo returnItem = null;

            if (foundItem == null)
            {
                returnItem = new ObjectItemInfo(ObjectItemInfo.ObjectItemFindState.NONE_FOUND);
            }
            else if (foundItem.SignalState != ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
            {
                returnItem = new ObjectItemInfo(foundItem.SignalState);
            }
            else
            {
                returnItem = new ObjectItemInfo(foundItem.SignalRef, foundItem.SignalLocation);
            }

            return (returnItem);
        }

        //
        //================================================================================================//
        //
        // Create Track Circuits
        //

        private void CreateTrackCircuits(TrItem[] TrItems, TrackNode[] trackNodes,
            TSectionDatFile tsectiondat, TDBFile tdbfile)
        {

            //
            // Create dummy element as first to keep indexes equal
            //

            TrackCircuitList = new List<TrackCircuitSection>();
            TrackCircuitList.Add(new TrackCircuitSection(0, this));

            //
            // Create new default elements from existing base
            //

            for (int iNode = 1; iNode < trackNodes.Length; iNode++)
            {
                TrackNode trackNode = trackNodes[iNode];
                TrackCircuitSection defaultSection =
                    new TrackCircuitSection(trackNode, iNode, tsectiondat, this);
                TrackCircuitList.Add(defaultSection);
            }

            //
            // loop through original default elements
            // collect track items
            //

            int originalNodes = TrackCircuitList.Count;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                ProcessNodes(iNode, TrItems, trackNodes, tsectiondat, tdbfile);
            }

            //
            // loop through original default elements
            // split on crossover items
            //

            originalNodes = TrackCircuitList.Count;
            int nextNode = originalNodes;
            foreach (KeyValuePair<int, CrossOverItem> CrossOver in CrossOverList)
            {
                nextNode = SplitNodesCrossover(CrossOver.Value, tsectiondat, nextNode);
            }

            //
            // loop through original default elements
            // split on normal signals
            //

            originalNodes = TrackCircuitList.Count;
            nextNode = originalNodes;

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                nextNode = SplitNodesSignals(iNode, nextNode);
            }

            //
            // loop through all items
            // perform link test
            //

            originalNodes = TrackCircuitList.Count;
            nextNode = originalNodes;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                nextNode = performLinkTest(iNode, nextNode);
            }

            //
            // loop through all items
            // reset active links
            // set fixed active links for none-junction links
            // set trailing junction flags
            //

            originalNodes = TrackCircuitList.Count;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setActivePins(iNode);
            }

            //
            // Set cross-reference
            //

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setCrossReference(iNode, trackNodes);
            }
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setCrossReferenceCrossOver(iNode, trackNodes);
            }

            //
            // Set cross-reference for signals
            //

            for (int iNode = 0; iNode < TrackCircuitList.Count; iNode++)
            {
                setSignalCrossReference(iNode);
            }

            //
            // Set default next signal and fixed route information
            //

            for (int iSignal = 0; signalObjects != null && iSignal < signalObjects.Length; iSignal++)
            {
                SignalObject thisSignal = signalObjects[iSignal];
                if (thisSignal != null)
                {
                    thisSignal.setSignalDefaultNextSignal();
                }
            }
        }

        //================================================================================================//
        //
        // Print TC Information
        //


        private void PrintTCBase(TrackNode[] trackNodes)
        {

            //
            // Test : print TrackCircuitList
            //

#if DEBUG_PRINT
            if (File.Exists(@"C:\temp\TCBase.txt"))
            {
                File.Delete(@"C:\temp\TCBase.txt");
            }

            for (int iNode = 0; iNode < TrackCircuitList.Count; iNode++)
            {
                TrackCircuitSection thisSection = TrackCircuitList[iNode];
                File.AppendAllText(@"C:\temp\TCBase.txt",
                   "\nIndex : " + iNode.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                   "{\n     Section    : " + thisSection.Index.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     OrgSection : " + thisSection.OriginalIndex.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Type       : " + thisSection.CircuitType.ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Pins (0,0) : " + thisSection.Pins[0, 0].Direction.ToString() +
                               " " + thisSection.Pins[0, 0].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Pins (0,1) : " + thisSection.Pins[0, 1].Direction.ToString() +
                               " " + thisSection.Pins[0, 1].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Pins (1,0) : " + thisSection.Pins[1, 0].Direction.ToString() +
                               " " + thisSection.Pins[1, 0].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Pins (1,1) : " + thisSection.Pins[1, 1].Direction.ToString() +
                               " " + thisSection.Pins[1, 1].Link.ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Active Pins (0,0) : " + thisSection.ActivePins[0, 0].Direction.ToString() +
                               " " + thisSection.ActivePins[0, 0].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Active Pins (0,1) : " + thisSection.ActivePins[0, 1].Direction.ToString() +
                               " " + thisSection.ActivePins[0, 1].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Active Pins (1,0) : " + thisSection.ActivePins[1, 0].Direction.ToString() +
                               " " + thisSection.ActivePins[1, 0].Link.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Active Pins (1,1) : " + thisSection.ActivePins[1, 1].Direction.ToString() +
                               " " + thisSection.ActivePins[1, 1].Link.ToString() + "\n");

                if (thisSection.EndIsTrailingJunction[0])
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Trailing Junction : direction 0\n");
                }

                if (thisSection.EndIsTrailingJunction[1])
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Trailing Junction : direction 1\n");
                }

                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     Length         : " + thisSection.Length.ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     OffsetLength 0 : " + thisSection.OffsetLength[0].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt",
                      "     OffsetLength 1 : " + thisSection.OffsetLength[1].ToString() + "\n");

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL && thisSection.CircuitItems != null)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", "\nSignals : \n");
                    for (int iDirection = 0; iDirection <= 1; iDirection++)
                    {
                        if (thisSection.EndSignals[iDirection] != null)
                        {
                            File.AppendAllText(@"C:\temp\TCBase.txt",
                                  "    End Signal " + iDirection.ToString() + " : " +
                                  thisSection.EndSignals[iDirection].thisRef.ToString() + "\n");
                        }

                        for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                        {
                            SignalHead.SIGFN thisFN = (SignalHead.SIGFN)fntype;
                            File.AppendAllText(@"C:\temp\TCBase.txt",
                                  "    Direction " + iDirection.ToString() +
                                  " - Function : " + thisFN.ToString() + " : \n");
                            TrackCircuitSignalList thisSignalList =
                                    thisSection.CircuitItems.TrackCircuitSignals[iDirection, fntype];
                            foreach (TrackCircuitSignalItem thisItem in thisSignalList.TrackCircuitItem)
                            {
                                SignalObject thisSignal = thisItem.SignalRef;
                                float signalDistance = thisItem.SignalLocation;

                                if (thisSignal.WorldObject == null)
                                {
                                    File.AppendAllText(@"C:\temp\TCBase.txt", "         " +
                                        thisSignal.thisRef.ToString() + " = **UNKNOWN** at " +
                                        signalDistance.ToString() + "\n");
                                }
                                else
                                {
                                    File.AppendAllText(@"C:\temp\TCBase.txt", "         " +
                                        thisSignal.thisRef.ToString() + " = " +
                                        thisSignal.WorldObject.SFileName + " at " +
                                        signalDistance.ToString() + "\n");
                                }
                            }
                            File.AppendAllText(@"C:\temp\TCBase.txt", "\n");
                        }
                    }

                    File.AppendAllText(@"C:\temp\TCBase.txt", "\nSpeedposts : \n");
                    for (int iDirection = 0; iDirection <= 1; iDirection++)
                    {
                        File.AppendAllText(@"C:\temp\TCBase.txt", "    Direction " + iDirection.ToString() + "\n");

                        TrackCircuitSignalList thisSpeedpostList =
                                thisSection.CircuitItems.TrackCircuitSpeedPosts[iDirection];
                        foreach (TrackCircuitSignalItem thisItem in thisSpeedpostList.TrackCircuitItem)
                        {
                            SignalObject thisSpeedpost = thisItem.SignalRef;
                            float speedpostDistance = thisItem.SignalLocation;

                            ObjectItemInfo speedInfo = new ObjectItemInfo(thisSpeedpost, speedpostDistance);
                            File.AppendAllText(@"C:\temp\TCBase.txt", thisSpeedpost.thisRef.ToString() +
                              " = pass : " + speedInfo.speed_passenger.ToString() +
                                                  " ; freight : " + speedInfo.speed_freight.ToString());
                            File.AppendAllText(@"C:\temp\TCBase.txt", " - at distance " + speedpostDistance.ToString() + "\n");
                        }

                        File.AppendAllText(@"C:\temp\TCBase.txt", "\n");
                    }
                }
                else if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt",
                                    "    Overlap : " + thisSection.Overlap.ToString() + "\n");
                }

                File.AppendAllText(@"C:\temp\TCBase.txt", "}\n");
            }

            File.AppendAllText(@"C:\temp\TCBase.txt", "\n\nCROSSOVERS\n\n");
            foreach (KeyValuePair<int, CrossOverItem> CrossItem in CrossOverList)
            {
                CrossOverItem thisCross = CrossItem.Value;
                File.AppendAllText(@"C:\temp\TCBase.txt", "   Indices : " + thisCross.ItemIndex[0].ToString() + " - " +
                                                                 thisCross.ItemIndex[1].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "   Sections: " + thisCross.SectionIndex[0].ToString() + " - " +
                                                                 thisCross.SectionIndex[1].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "\n");
            }

            File.AppendAllText(@"C:\temp\TCBase.txt", "\n\nTRACK SECTIONS\n\n");
            foreach (TrackNode thisTrack in trackNodes)
            {
                if (thisTrack == null)
                {
                }
                else if (thisTrack.TCCrossReference == null)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt",
            "   ERROR : no track circuit cross-reference \n");
                    Trace.TraceWarning("ERROR : Track Node without Track Circuit cross-reference");
                }
                else
                {

                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;

                    TrackCircuitSection thisSection = TrackCircuitList[thisXRef[0].CrossRefIndex];
                    File.AppendAllText(@"C:\temp\TCBase.txt",
                        "     Original node : " + thisSection.OriginalIndex.ToString() + "\n");

                    foreach (TrackCircuitCrossReference thisReference in thisXRef)
                    {
                        File.AppendAllText(@"C:\temp\TCBase.txt",
                            "        Ref Index : " + thisReference.CrossRefIndex.ToString() + " : " +
                            "Length : " + thisReference.Length.ToString() + " at : " +
                            thisReference.Position[0] + " - " + thisReference.Position[1] + "\n");
                    }
                    File.AppendAllText(@"C:\temp\TCBase.txt", "\n");

                    if (thisXRef[thisXRef.Count - 1].Position[1] != 0)
                    {
                        File.AppendAllText(@"C:\temp\TCBASE.txt", " >>> INVALID XREF\n");
                    }
                }
            }

            File.AppendAllText(@"C:\temp\TCBase.txt", "\n\n PLATFORMS \n --------- \n\n");

            foreach (KeyValuePair<int, int> platformXRef in PlatformXRefList)
            {
                PlatformDetails thisPlatform = PlatformDetailsList[platformXRef.Value];

                File.AppendAllText(@"C:\temp\TCBase.txt", "Index " + platformXRef.Key.ToString() +
                " : Platform " + platformXRef.Value.ToString() +
                " [" + thisPlatform.PlatformReference[0].ToString() +
                " ," + thisPlatform.PlatformReference[1].ToString() + "]\n");
            }

            File.AppendAllText(@"C:\temp\TCBase.txt", "\n\n");

            for (int iPlatform = 0; iPlatform < PlatformDetailsList.Count; iPlatform++)
            {
                PlatformDetails thisPlatform = PlatformDetailsList[iPlatform];

                File.AppendAllText(@"C:\temp\TCBase.txt", "Platform : " + iPlatform.ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Name     : " + thisPlatform.Name + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "Time     : " + thisPlatform.MinWaitingTime.ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Sections : ");
                for (int iSection = 0; iSection < thisPlatform.TCSectionIndex.Count; iSection++)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", " " + thisPlatform.TCSectionIndex[iSection].ToString());
                }
                File.AppendAllText(@"C:\temp\TCBase.txt", "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Platform References    : " +
                        thisPlatform.PlatformReference[0].ToString() + " + " +
                        thisPlatform.PlatformReference[1].ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Section Offset : [0,0] : " +
                        thisPlatform.TCOffset[0, 0].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "                 [0,1] : " +
                        thisPlatform.TCOffset[0, 1].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "                 [1,0] : " +
                        thisPlatform.TCOffset[1, 0].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "                 [1,1] : " +
                        thisPlatform.TCOffset[1, 1].ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Length                 : " +
                        thisPlatform.Length.ToString() + "\n");

                File.AppendAllText(@"C:\temp\TCBase.txt", "Node Offset    : [0]   : " +
                        thisPlatform.nodeOffset[0].ToString() + "\n");
                File.AppendAllText(@"C:\temp\TCBase.txt", "Node Offset    : [1]   : " +
                        thisPlatform.nodeOffset[1].ToString() + "\n");

                if (thisPlatform.EndSignals[0] == -1)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", "End Signal     : [0]   : -None-\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", "End Signal     : [0]   : " +
                            thisPlatform.EndSignals[0].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCBase.txt", "Distance               : " +
                            thisPlatform.DistanceToSignals[0].ToString() + "\n");
                }
                if (thisPlatform.EndSignals[1] == -1)
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", "End Signal     : [1]   : -None-\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\TCBase.txt", "End Signal     : [1]   : " +
                            thisPlatform.EndSignals[1].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCBase.txt", "Distance               : " +
                            thisPlatform.DistanceToSignals[1].ToString() + "\n");
                }

                File.AppendAllText(@"C:\temp\TCBase.txt", "\n");
            }
#endif
        }

        //================================================================================================//
        //
        // ProcessNodes
        //

        public void ProcessNodes(int iNode, TrItem[] TrItems, TrackNode[] trackNodes,
                TSectionDatFile tsectiondat, TDBFile tdbfile)
        {

            //
            // Check if original tracknode had trackitems
            //

            TrackCircuitSection thisCircuit = TrackCircuitList[iNode];
            TrackNode thisNode = trackNodes[thisCircuit.OriginalIndex];

            if (thisNode.TrVectorNode != null && thisNode.TrVectorNode.noItemRefs > 0)
            {
                //
                // Create TDBtraveller at start of section to calculate distances
                //

                TrVectorSection firstSection = thisNode.TrVectorNode.TrVectorSections[0];
                Traveller TDBTrav = new Traveller(tsectiondat, trackNodes, thisNode,
                                firstSection.TileX, firstSection.TileZ,
                                firstSection.X, firstSection.Z, (Traveller.TravellerDirection)1);

                //
                // Process all items (do not split yet)
                //

                float[] lastDistance = new float[2] { -1.0f, -1.0f };
                for (int iRef = 0; iRef < thisNode.TrVectorNode.noItemRefs; iRef++)
                {
                    int TDBRef = thisNode.TrVectorNode.TrItemRefs[iRef];
                    if (TrItems[TDBRef] != null)
                    {
                        lastDistance = InsertNode(thisCircuit, TrItems[TDBRef], TDBTrav, trackNodes, lastDistance);
                    }
                }
            }
        }

        //================================================================================================//
        //
        // InsertNode
        //

        public float[] InsertNode(TrackCircuitSection thisCircuit, TrItem thisItem,
                        Traveller TDBTrav, TrackNode[] trackNodes, float[] lastDistance)
        {

            float[] newLastDistance = new float[2];
            lastDistance.CopyTo(newLastDistance, 0);

            //
            // Insert signal
            //

            if (thisItem.ItemType == TrItem.trItemType.trSIGNAL)
            {
                SignalItem sigItem = (SignalItem)thisItem;
                if (sigItem.sigObj >= 0)
                {
                    SignalObject thisSignal = SignalObjects[sigItem.sigObj];
                    float signalDistance = thisSignal.DistanceTo(TDBTrav);
                    if (thisSignal.direction == 1)
                    {
                        signalDistance = thisCircuit.Length - signalDistance;
                    }

                    for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                    {
                        SignalHead.SIGFN[] reqfntype = new SignalHead.SIGFN[1];
                        reqfntype[0] = (SignalHead.SIGFN)fntype;

                        if (thisSignal.isSignalType(reqfntype))
                        {
                            TrackCircuitSignalItem thisTCItem =
                                    new TrackCircuitSignalItem(thisSignal, signalDistance);

                            int directionList = thisSignal.direction == 0 ? 1 : 0;
                            TrackCircuitSignalList thisSignalList =
                                    thisCircuit.CircuitItems.TrackCircuitSignals[directionList, fntype];

                            bool signalset = false;
                            foreach (TrackCircuitSignalItem inItem in thisSignalList.TrackCircuitItem)
                            {
                                if (inItem.SignalRef == thisSignal)
                                {
                                    signalset = true;
                                }
                            }

                            if (!signalset)
                            {
                                if (directionList == 0)
                                {
                                    thisSignalList.TrackCircuitItem.Insert(0, thisTCItem);
                                }
                                else
                                {
                                    thisSignalList.TrackCircuitItem.Add(thisTCItem);
                                }
                            }
                        }
                    }
                    newLastDistance[thisSignal.direction] = signalDistance;
                }
            }

        //
            // Insert speedpost
            //

            else if (thisItem.ItemType == TrItem.trItemType.trSPEEDPOST)
            {
                SpeedPostItem speedItem = (SpeedPostItem)thisItem;
                if (speedItem.sigObj >= 0)
                {
                    SignalObject thisSpeedpost = SignalObjects[speedItem.sigObj];
                    float speedpostDistance = thisSpeedpost.DistanceTo(TDBTrav);
                    if (thisSpeedpost.direction == 1)
                    {
                        speedpostDistance = thisCircuit.Length - speedpostDistance;
                    }

                    if (speedpostDistance == lastDistance[thisSpeedpost.direction]) // if at same position as last item
                    {
                        speedpostDistance = speedpostDistance + 0.001f;  // shift 1 mm so it will be found
                    }

                    TrackCircuitSignalItem thisTCItem =
                            new TrackCircuitSignalItem(thisSpeedpost, speedpostDistance);

                    int directionList = thisSpeedpost.direction == 0 ? 1 : 0;
                    TrackCircuitSignalList thisSignalList =
                            thisCircuit.CircuitItems.TrackCircuitSpeedPosts[directionList];

                    if (directionList == 0)
                    {
                        thisSignalList.TrackCircuitItem.Insert(0, thisTCItem);
                    }
                    else
                    {
                        thisSignalList.TrackCircuitItem.Add(thisTCItem);
                    }

                    newLastDistance[thisSpeedpost.direction] = speedpostDistance;
                }
            }

        //
            // Insert crossover in special crossover list
            //

            else if (thisItem.ItemType == TrItem.trItemType.trCROSSOVER)
            {
                CrossoverItem crossItem = (CrossoverItem)thisItem;

                float cdist = TDBTrav.DistanceTo(trackNodes[thisCircuit.OriginalIndex],
                crossItem.TileX, crossItem.TileZ,
                                crossItem.X, crossItem.Y, crossItem.Z);

                int thisId = (int)crossItem.TrItemId;
                int crossId = (int)crossItem.TrackNode;
                CrossOverItem exItem = null;

                // search in Dictionary for combined item //

                if (CrossOverList.ContainsKey(crossId))
                {
                    exItem = CrossOverList[crossId];
                    exItem.Position[1] = cdist;
                    exItem.SectionIndex[1] = thisCircuit.Index;
                }
                else
                {
                    exItem = new CrossOverItem();
                    exItem.SectionIndex[0] = thisCircuit.Index;
                    exItem.SectionIndex[1] = -1;

                    exItem.Position[0] = cdist;
                    exItem.ItemIndex[0] = thisId;
                    exItem.ItemIndex[1] = crossId;

                    exItem.TrackShape = crossItem.CID1;

                    CrossOverList.Add(thisId, exItem);
                }
            }

            return (newLastDistance);
        }

        //================================================================================================//
        //
        // Split on Signals
        //

        private int SplitNodesSignals(int thisNode, int nextNode)
        {
            int thisIndex = thisNode;
            int newIndex = -1;
            List<int> addIndex = new List<int>();

            //
            // in direction 0, check original item only
            // keep list of added items
            //

            TrackCircuitSection thisSection = TrackCircuitList[thisIndex];

            newIndex = -1;
            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
            {
                addIndex.Add(thisNode);

                List<TrackCircuitSignalItem> sectionSignals =
                         thisSection.CircuitItems.TrackCircuitSignals[0, (int)SignalHead.SIGFN.NORMAL].TrackCircuitItem;

                while (sectionSignals.Count > 0)
                {
                    TrackCircuitSignalItem thisSignal = sectionSignals[0];
                    sectionSignals.RemoveAt(0);

                    newIndex = nextNode;
                    nextNode++;

                    splitSection(thisIndex, newIndex, thisSection.Length - thisSignal.SignalLocation);
                    TrackCircuitSection newSection = TrackCircuitList[newIndex];
                    newSection.EndSignals[0] = thisSignal.SignalRef;
                    thisSection = TrackCircuitList[thisIndex];
                    addIndex.Add(newIndex);

                    // restore list (link is lost as item is replaced)
                    sectionSignals = thisSection.CircuitItems.TrackCircuitSignals[0, (int)SignalHead.SIGFN.NORMAL].TrackCircuitItem;
                }
            }

            //
            // in direction 1, check original item and all added items
            //

            foreach (int actIndex in addIndex)
            {
                thisIndex = actIndex;

                while (thisIndex > 0)
                {
                    thisSection = TrackCircuitList[thisIndex];

                    newIndex = -1;
                    if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {

                        List<TrackCircuitSignalItem> sectionSignals =
                           thisSection.CircuitItems.TrackCircuitSignals[1, (int)SignalHead.SIGFN.NORMAL].TrackCircuitItem;

                        if (sectionSignals.Count > 0)
                        {
                            TrackCircuitSignalItem thisSignal = sectionSignals[0];
                            sectionSignals.RemoveAt(0);

                            newIndex = nextNode;
                            nextNode++;

                            splitSection(thisIndex, newIndex, thisSignal.SignalLocation);
                            TrackCircuitSection newSection = TrackCircuitList[newIndex];
                            newSection.EndSignals[0] = null;
                            thisSection = TrackCircuitList[thisIndex];
                            thisSection.EndSignals[1] = thisSignal.SignalRef;

                            // restore list (link is lost as item is replaced)
                            sectionSignals = thisSection.CircuitItems.TrackCircuitSignals[1, (int)SignalHead.SIGFN.NORMAL].TrackCircuitItem;
                        }
                    }
                    thisIndex = thisSection.CircuitItems.TrackCircuitSignals[1, (int)SignalHead.SIGFN.NORMAL].TrackCircuitItem.Count > 0 ? thisIndex : newIndex;
                }
            }

            return (nextNode);
        }

        //================================================================================================//
        //
        // Split CrossOvers
        //

        private int SplitNodesCrossover(CrossOverItem CrossOver,
                TSectionDatFile tsectiondat, int nextNode)
        {
            bool processCrossOver = true;
            int sectionIndex0 = 0;
            int sectionIndex1 = 0;

            if (CrossOver.SectionIndex[0] < 0 || CrossOver.SectionIndex[1] < 0)
            {
                Trace.TraceWarning("Incomplete crossover : indices {0} and {1}",
                            CrossOver.ItemIndex[0], CrossOver.ItemIndex[1]);
                processCrossOver = false;
            }
            if (CrossOver.SectionIndex[0] == CrossOver.SectionIndex[1])
            {
                Trace.TraceWarning("Invalid crossover : indices {0} and {1} : equal section : {2}",
                            CrossOver.ItemIndex[0], CrossOver.ItemIndex[1],
                CrossOver.SectionIndex[0]);
                processCrossOver = false;
            }

            if (processCrossOver)
            {
                sectionIndex0 = getCrossOverSectionIndex(CrossOver, 0);
                sectionIndex1 = getCrossOverSectionIndex(CrossOver, 1);

                if (sectionIndex0 < 0 || sectionIndex1 < 0)
                {
                    processCrossOver = false;
                }
            }

            if (processCrossOver)
            {
                int newSection0 = nextNode;
                nextNode++;
                int newSection1 = nextNode;
                nextNode++;
                int jnSection = nextNode;
                nextNode++;

                splitSection(sectionIndex0, newSection0, CrossOver.Position[0]);
                splitSection(sectionIndex1, newSection1, CrossOver.Position[1]);

                addCrossoverJunction(sectionIndex0, newSection0, sectionIndex1, newSection1,
                                jnSection, CrossOver, tsectiondat);
            }

            return (nextNode);
        }

        //================================================================================================//
        //
        // Get cross-over section index
        //

        private int getCrossOverSectionIndex(CrossOverItem CrossOver, int Index)
        {
            int sectionIndex = CrossOver.SectionIndex[Index];
            float position = CrossOver.Position[Index];
            TrackCircuitSection section = TrackCircuitList[sectionIndex];

            // to overcome tdb errors, check if still in original tracknode
            int firstSectionOriginalIndex = section.OriginalIndex;
            int firstSectionIndex = sectionIndex;

            while (position > 0 && position > section.Length)
            // while (position > 0 && position > section.Length && section.OriginalIndex == firstSectionOriginalIndex)
            {
                int prevSection = sectionIndex;
                position = position - section.Length;
                CrossOver.Position[Index] = position;
                sectionIndex = section.Pins[1, 0].Link;

                if (sectionIndex > 0)
                {
                    section = TrackCircuitList[sectionIndex];
                    if (section.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                    {
                        if (section.Pins[0, 0].Link == prevSection)
                        {
                            sectionIndex = section.Pins[1, 0].Link;
                        }
                        else
                        {
                            sectionIndex = section.Pins[1, 1].Link;
                        }
                        section = TrackCircuitList[sectionIndex];
                    }
                }
                else
                {
                    position = -1;  // no position found //
                }
            }

            if (position < 0)
            {
                Trace.TraceWarning("Cannot locate CrossOver {0} in Section {1}",
                                CrossOver.ItemIndex[0], CrossOver.SectionIndex[0]);
                sectionIndex = -1;
            }

            //           if (section.OriginalIndex == firstSectionOriginalIndex)  // if correct circuit found (part of original tracknode)
            //           {
            return (sectionIndex);
            //           }
            //           else                                                     // if not, return first section (is wrong but best we have)
            //           {
            //               Trace.TraceInformation("Cannot locate proper section for CrossOver {0} in Section {1}",
            //                   CrossOver.ItemIndex[0], CrossOver.SectionIndex[0]);
            //               return (firstSectionIndex);
            //           }
        }

        //================================================================================================//
        //
        // Split section
        //

        private void splitSection(int orgSectionIndex, int newSectionIndex, float position)
        {
            TrackCircuitSection orgSection = TrackCircuitList[orgSectionIndex];
            TrackCircuitSection newSection = orgSection.CopyBasic(newSectionIndex);
            TrackCircuitSection replSection = orgSection.CopyBasic(orgSectionIndex);

            replSection.OriginalIndex = newSection.OriginalIndex = orgSection.OriginalIndex;
            replSection.CircuitType = newSection.CircuitType = TrackCircuitSection.CIRCUITTYPE.NORMAL;

            replSection.Length = position;
            newSection.Length = orgSection.Length - position;

            // take care of rounding errors

            if (newSection.Length < 0 || Math.Abs(newSection.Length) < 0.01f)
            {
                newSection.Length = 0.01f;
            }
            if (replSection.Length < 0 || Math.Abs(replSection.Length) < 0.01f)
            {
                replSection.Length = 0.01f;
            }

            // check for invalid lengths - report and correct

            if (newSection.Length < 0)
            {
                Trace.TraceWarning("Invalid Length for new section {0}: length {1}, split on {2}",
                        newSection.Index, orgSection.Length, position);
                newSection.Length = 0.1f;
            }
            if (replSection.Length < 0)
            {
                Trace.TraceWarning("Invalid Length for replacement section {0}: length {1}, split on {2}",
                        newSection.Index, orgSection.Length, position);
                replSection.Length = 0.1f;
            }

            // set lengths and offset

            replSection.OffsetLength[0] = orgSection.OffsetLength[0] + newSection.Length;
            replSection.OffsetLength[1] = orgSection.OffsetLength[1];

            newSection.OffsetLength[0] = orgSection.OffsetLength[0];
            newSection.OffsetLength[1] = orgSection.OffsetLength[1] + replSection.Length;

            // set new pins

            replSection.Pins[0, 0].Direction = orgSection.Pins[0, 0].Direction;
            replSection.Pins[0, 0].Link = orgSection.Pins[0, 0].Link;
            replSection.Pins[1, 0].Direction = 1;
            replSection.Pins[1, 0].Link = newSectionIndex;

            newSection.Pins[0, 0].Direction = 0;
            newSection.Pins[0, 0].Link = orgSectionIndex;
            newSection.Pins[1, 0].Direction = orgSection.Pins[1, 0].Direction;
            newSection.Pins[1, 0].Link = orgSection.Pins[1, 0].Link;

            // update pins on adjacent sections

            int refLinkIndex = newSection.Pins[1, 0].Link;
            int refLinkDirIndex = newSection.Pins[1, 0].Direction == 0 ? 1 : 0;
            TrackCircuitSection refLink = TrackCircuitList[refLinkIndex];
            if (refLink.Pins[refLinkDirIndex, 0].Link == orgSectionIndex)
            {
                refLink.Pins[refLinkDirIndex, 0].Link = newSectionIndex;
            }
            else if (refLink.Pins[refLinkDirIndex, 1].Link == orgSectionIndex)
            {
                refLink.Pins[refLinkDirIndex, 1].Link = newSectionIndex;
            }

            // copy signal information

            for (int itype = 0; itype < orgSection.CircuitItems.TrackCircuitSignals.GetLength(1); itype++)
            {
                TrackCircuitSignalList orgSigList = orgSection.CircuitItems.TrackCircuitSignals[0, itype];
                TrackCircuitSignalList replSigList = replSection.CircuitItems.TrackCircuitSignals[0, itype];
                TrackCircuitSignalList newSigList = newSection.CircuitItems.TrackCircuitSignals[0, itype];

                foreach (TrackCircuitSignalItem thisSignal in orgSigList.TrackCircuitItem)
                {
                    float sigLocation = thisSignal.SignalLocation;
                    if (sigLocation <= newSection.Length)
                    {
                        newSigList.TrackCircuitItem.Add(thisSignal);
                    }
                    else
                    {
                        thisSignal.SignalLocation -= newSection.Length;
                        replSigList.TrackCircuitItem.Add(thisSignal);
                    }
                }
            }

            for (int itype = 0; itype < orgSection.CircuitItems.TrackCircuitSignals.GetLength(1); itype++)
            {
                TrackCircuitSignalList orgSigList = orgSection.CircuitItems.TrackCircuitSignals[1, itype];
                TrackCircuitSignalList replSigList = replSection.CircuitItems.TrackCircuitSignals[1, itype];
                TrackCircuitSignalList newSigList = newSection.CircuitItems.TrackCircuitSignals[1, itype];

                foreach (TrackCircuitSignalItem thisSignal in orgSigList.TrackCircuitItem)
                {
                    float sigLocation = thisSignal.SignalLocation;
                    if (sigLocation > replSection.Length)
                    {
                        thisSignal.SignalLocation -= replSection.Length;
                        newSigList.TrackCircuitItem.Add(thisSignal);
                    }
                    else
                    {
                        replSigList.TrackCircuitItem.Add(thisSignal);
                    }
                }
            }

            // copy speedpost information

            TrackCircuitSignalList orgSpeedList = orgSection.CircuitItems.TrackCircuitSpeedPosts[0];
            TrackCircuitSignalList replSpeedList = replSection.CircuitItems.TrackCircuitSpeedPosts[0];
            TrackCircuitSignalList newSpeedList = newSection.CircuitItems.TrackCircuitSpeedPosts[0];

            foreach (TrackCircuitSignalItem thisSpeedpost in orgSpeedList.TrackCircuitItem)
            {
                float sigLocation = thisSpeedpost.SignalLocation;
                if (sigLocation < newSection.Length)
                {
                    newSpeedList.TrackCircuitItem.Add(thisSpeedpost);
                }
                else
                {
                    thisSpeedpost.SignalLocation -= newSection.Length;
                    replSpeedList.TrackCircuitItem.Add(thisSpeedpost);
                }
            }

            orgSpeedList = orgSection.CircuitItems.TrackCircuitSpeedPosts[1];
            replSpeedList = replSection.CircuitItems.TrackCircuitSpeedPosts[1];
            newSpeedList = newSection.CircuitItems.TrackCircuitSpeedPosts[1];

            foreach (TrackCircuitSignalItem thisSpeedpost in orgSpeedList.TrackCircuitItem)
            {
                float sigLocation = thisSpeedpost.SignalLocation;
                if (sigLocation > replSection.Length)
                {
                    thisSpeedpost.SignalLocation -= replSection.Length;
                    newSpeedList.TrackCircuitItem.Add(thisSpeedpost);
                }
                else
                {
                    replSpeedList.TrackCircuitItem.Add(thisSpeedpost);
                }
            }

            // copy milepost information

            foreach (TrackCircuitMilepost thisMilePost in orgSection.CircuitItems.MilePosts)
            {
                if (thisMilePost.MilepostLocation[0] > replSection.Length)
                {
                    thisMilePost.MilepostLocation[0] -= replSection.Length;
                    newSection.CircuitItems.MilePosts.Add(thisMilePost);
                }
                else
                {
                    thisMilePost.MilepostLocation[1] -= newSection.Length;
                    replSection.CircuitItems.MilePosts.Add(thisMilePost);
                }
            }

            // update list

            TrackCircuitList.RemoveAt(orgSectionIndex);
            TrackCircuitList.Insert(orgSectionIndex, replSection);
            TrackCircuitList.Add(newSection);
        }


        //================================================================================================//
        //
        // Add junction sections for Crossover
        //

        private void addCrossoverJunction(int leadSectionIndex0, int trailSectionIndex0,
                        int leadSectionIndex1, int trailSectionIndex1, int JnIndex,
                        CrossOverItem CrossOver, TSectionDatFile tsectiondat)
        {
            TrackCircuitSection leadSection0 = TrackCircuitList[leadSectionIndex0];
            TrackCircuitSection leadSection1 = TrackCircuitList[leadSectionIndex1];
            TrackCircuitSection trailSection0 = TrackCircuitList[trailSectionIndex0];
            TrackCircuitSection trailSection1 = TrackCircuitList[trailSectionIndex1];
            TrackCircuitSection JnSection = new TrackCircuitSection(JnIndex, this);

            JnSection.OriginalIndex = leadSection0.OriginalIndex;
            JnSection.CircuitType = TrackCircuitSection.CIRCUITTYPE.CROSSOVER;
            JnSection.Length = 0;

            leadSection0.Pins[1, 0].Link = JnIndex;
            leadSection1.Pins[1, 0].Link = JnIndex;
            trailSection0.Pins[0, 0].Link = JnIndex;
            trailSection1.Pins[0, 0].Link = JnIndex;

            JnSection.Pins[0, 0].Direction = 0;
            JnSection.Pins[0, 0].Link = leadSectionIndex0;
            JnSection.Pins[0, 1].Direction = 0;
            JnSection.Pins[0, 1].Link = leadSectionIndex1;
            JnSection.Pins[1, 0].Direction = 1;
            JnSection.Pins[1, 0].Link = trailSectionIndex0;
            JnSection.Pins[1, 1].Direction = 1;
            JnSection.Pins[1, 1].Link = trailSectionIndex1;

            if (tsectiondat.TrackShapes.ContainsKey(CrossOver.TrackShape))
            {
                JnSection.Overlap = tsectiondat.TrackShapes[CrossOver.TrackShape].ClearanceDistance;
            }
            else
            {
                JnSection.Overlap = 0;
            }

            JnSection.SignalsPassingRoutes = new List<int>();

            TrackCircuitList.Add(JnSection);
        }

        //================================================================================================//
        //
        // Check pin links
        //

        private int performLinkTest(int thisNode, int nextNode)
        {

            TrackCircuitSection thisSection = TrackCircuitList[thisNode];

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int iPin = 0; iPin <= 1; iPin++)
                {
                    int linkedNode = thisSection.Pins[iDirection, iPin].Link;
                    int linkedDirection = thisSection.Pins[iDirection, iPin].Direction == 0 ? 1 : 0;

                    if (linkedNode > 0)
                    {
                        TrackCircuitSection linkedSection = TrackCircuitList[linkedNode];

                        bool linkfound = false;
                        bool doublelink = false;
                        int doublenode = -1;

                        for (int linkedPin = 0; linkedPin <= 1; linkedPin++)
                        {
                            if (linkedSection.Pins[linkedDirection, linkedPin].Link == thisNode)
                            {
                                linkfound = true;
                                if (linkedSection.ActivePins[linkedDirection, linkedPin].Link == -1)
                                {
                                    linkedSection.ActivePins[linkedDirection, linkedPin].Link = thisNode;
                                }
                                else
                                {
                                    doublelink = true;
                                    doublenode = linkedSection.ActivePins[linkedDirection, linkedPin].Link;
                                }
                            }
                        }

                        if (!linkfound)
                        {
                            Trace.TraceWarning("Invalid link in section {0} : Pin [{1},{2}] : section {3}",
                                thisNode, iDirection, iPin, linkedNode);
                            int endNode = nextNode;
                            nextNode++;
                            insertEndNode(thisNode, iDirection, iPin, endNode);
                        }

                        if (doublelink)
                        {
                            Trace.TraceWarning("Section {0}, Pin [{1},{2}] links to section {3} already linked by {4}",
                                    thisNode, iDirection, iPin, linkedNode, doublenode);
                            int endNode = nextNode;
                            nextNode++;
                            insertEndNode(thisNode, iDirection, iPin, endNode);
                        }
                    }
                    else if (linkedNode == 0)
                    {
                        Trace.TraceWarning("Section {0}, Pin [{1},{2}] is 0 reference",
                            thisNode, iDirection, iPin);
                        int endNode = nextNode;
                        nextNode++;
                        insertEndNode(thisNode, iDirection, iPin, endNode);
                    }
                }
            }

            return (nextNode);
        }

        //================================================================================================//
        //
        // insert end node to capture database break
        //

        private void insertEndNode(int thisNode, int direction, int pin, int endNode)
        {

            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            TrackCircuitSection endSection = new TrackCircuitSection(endNode, this);

            endSection.CircuitType = TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK;
            int endDirection = direction == 0 ? 1 : 0;
            int iDirection = thisSection.Pins[direction, pin].Direction == 0 ? 1 : 0;
            endSection.Pins[iDirection, 0].Direction = endDirection;
            endSection.Pins[iDirection, 0].Link = thisNode;

            thisSection.Pins[direction, pin].Link = endNode;

            TrackCircuitList.Add(endSection);
        }

        //================================================================================================//
        //
        // set active pins for non-junction links
        // set trailing link indications
        //

        private void setActivePins(int thisNode)
        {
            TrackCircuitSection thisSection = TrackCircuitList[thisNode];

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int iPin = 0; iPin <= 1; iPin++)
                {
                    if (thisSection.Pins[iDirection, iPin].Link > 0)
                    {
                        TrackCircuitSection nextSection = null;

                        if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                        {
                            int nextIndex = thisSection.Pins[iDirection, iPin].Link;
                            nextSection = TrackCircuitList[nextIndex];

                            if (thisSection.Pins[iDirection, 1].Link > 0)    // Junction end
                            {
                                thisSection.ActivePins[iDirection, iPin].Direction =
                                    thisSection.Pins[iDirection, iPin].Direction;
                                thisSection.ActivePins[iDirection, iPin].Link = -1;
                            }
                            else
                            {
                                thisSection.ActivePins[iDirection, iPin].Direction =
                                    thisSection.Pins[iDirection, iPin].Direction;
                                thisSection.ActivePins[iDirection, iPin].Link =
                                    thisSection.Pins[iDirection, iPin].Link;
                            }
                        }
                        else if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                        {
                            int nextIndex = thisSection.Pins[iDirection, iPin].Link;
                            nextSection = TrackCircuitList[nextIndex];

                            thisSection.ActivePins[iDirection, iPin].Direction =
                                thisSection.Pins[iDirection, iPin].Direction;
                            thisSection.ActivePins[iDirection, iPin].Link = -1;
                        }
                        else
                        {
                            int nextIndex = thisSection.Pins[iDirection, iPin].Link;
                            nextSection = TrackCircuitList[nextIndex];

                            thisSection.ActivePins[iDirection, iPin].Direction =
                                thisSection.Pins[iDirection, iPin].Direction;
                            thisSection.ActivePins[iDirection, iPin].Link =
                                thisSection.Pins[iDirection, iPin].Link;
                        }


                        if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                        {
                            thisSection.ActivePins[iDirection, iPin].Link = -1;
                            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
                            {
                                thisSection.EndIsTrailingJunction[iDirection] = true;
                            }
                        }
                        else if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                        {
                            int nextDirection = thisSection.Pins[iDirection, iPin].Direction == 0 ? 1 : 0;
                            //                          int nextDirection = thisSection.Pins[iDirection, iPin].Direction;
                            if (nextSection.Pins[nextDirection, 1].Link > 0)
                            {
                                thisSection.ActivePins[iDirection, iPin].Link = -1;
                                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
                                {
                                    thisSection.EndIsTrailingJunction[iDirection] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        //
        // set cross-reference to tracknodes
        //

        private void setCrossReference(int thisNode, TrackNode[] trackNodes)
        {
            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            if (thisSection.OriginalIndex > 0 && thisSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
            {
                TrackNode thisTrack = trackNodes[thisSection.OriginalIndex];
                float offset0 = thisSection.OffsetLength[0];
                float offset1 = thisSection.OffsetLength[1];

                TrackCircuitCrossReference newReference = new TrackCircuitCrossReference(thisSection);

                bool inserted = false;

                if (thisTrack.TCCrossReference == null)
                {
                    thisTrack.TCCrossReference = new TrackCircuitXRefList();
                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;
                }
                else
                {
                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;
                    for (int iPart = 0; iPart < thisXRef.Count && !inserted; iPart++)
                    {
                        TrackCircuitCrossReference thisReference = thisXRef[iPart];
                        if (offset0 < thisReference.Position[0])
                        {
                            thisXRef.Insert(iPart, newReference);
                            inserted = true;
                        }
                        else if (offset1 > thisReference.Position[1])
                        {
                            thisXRef.Insert(iPart, newReference);
                            inserted = true;
                        }
                    }
                }

                if (!inserted)
                {
                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;
                    thisXRef.Add(newReference);
                }
            }
        }

        //================================================================================================//
        //
        // set cross-reference to tracknodes for CrossOver items
        //

        private void setCrossReferenceCrossOver(int thisNode, TrackNode[] trackNodes)
        {
            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            if (thisSection.OriginalIndex > 0 && thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
            {
                for (int iPin = 0; iPin <= 1; iPin++)
                {
                    int prevIndex = thisSection.Pins[0, iPin].Link;
                    TrackCircuitSection prevSection = TrackCircuitList[prevIndex];

                    TrackCircuitCrossReference newReference = new TrackCircuitCrossReference(thisSection);
                    TrackNode thisTrack = trackNodes[prevSection.OriginalIndex];
                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;

                    bool inserted = false;
                    for (int iPart = 0; iPart < thisXRef.Count && !inserted; iPart++)
                    {
                        TrackCircuitCrossReference thisReference = thisXRef[iPart];
                        if (thisReference.CrossRefIndex == prevIndex)
                        {
                            newReference.Position[0] = thisReference.Position[0];
                            newReference.Position[1] = thisReference.Position[1] + thisReference.Length;
                            thisXRef.Insert(iPart, newReference);
                            inserted = true;
                        }
                    }

                    if (!inserted)
                    {
                        Trace.TraceWarning("ERROR : cannot find XRef for leading track to crossover {0}",
                            thisNode);
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Set trackcircuit cross reference for signal items
        //

        private void setSignalCrossReference(int thisNode)
        {

            TrackCircuitSection thisSection = TrackCircuitList[thisNode];

            // process end signals

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                SignalObject thisSignal = thisSection.EndSignals[iDirection];
                if (thisSignal != null)
                {
                    thisSignal.TCReference = thisNode;
                    thisSignal.TCOffset = thisSection.Length;
                    thisSignal.TCDirection = iDirection;

                    //                  int pinIndex = iDirection == 0 ? 1 : 0;
                    int pinIndex = iDirection;
                    thisSignal.TCNextTC = thisSection.Pins[pinIndex, 0].Link;
                    thisSignal.TCNextDirection = thisSection.Pins[pinIndex, 0].Direction;
                }
            }

            // process other signals - only set info if not already set

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                {
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[iDirection, fntype];
                    foreach (TrackCircuitSignalItem thisItem in thisList.TrackCircuitItem)
                    {
                        SignalObject thisSignal = thisItem.SignalRef;

                        if (thisSignal.TCReference <= 0)
                        {
                            thisSignal.TCReference = thisNode;
                            thisSignal.TCOffset = thisItem.SignalLocation;
                            thisSignal.TCDirection = iDirection;
                        }
                    }
                }
            }


            // process speedposts

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSpeedPosts[iDirection];
                foreach (TrackCircuitSignalItem thisItem in thisList.TrackCircuitItem)
                {
                    SignalObject thisSignal = thisItem.SignalRef;

                    if (thisSignal.TCReference <= 0)
                    {
                        thisSignal.TCReference = thisNode;
                        thisSignal.TCOffset = thisItem.SignalLocation;
                        thisSignal.TCDirection = iDirection;
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Set physical switch
        //

        public void setSwitch(int nodeIndex, int switchPos, TrackCircuitSection thisSection)
        {
            if (MultiPlayer.MPManager.NoAutoSwitch() ) return;
            TrackNode thisNode = trackDB.TrackNodes[nodeIndex];
            thisNode.TrJunctionNode.SelectedRoute = switchPos;
            thisSection.JunctionLastRoute = switchPos;
        }

        //================================================================================================//
        //
        // Node control track clearance update request
        //

        public void requestClearNode(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePart)
        {

#if DEBUG_REPORTS
            String report = "Request for clear node from train ";
            report = String.Concat(report, thisTrain.Train.Number.ToString());
            report = String.Concat(report, " at section ", thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex.ToString());
            report = String.Concat(report, " starting from ", thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex].ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Request for clear node from train ";
                reportCT = String.Concat(reportCT, thisTrain.Train.Number.ToString());
                reportCT = String.Concat(reportCT, " at section ", thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex.ToString());
                reportCT = String.Concat(reportCT, " starting from ", thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex].ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            // check if present clearance is beyond required maximum distance

            int sectionIndex = -1;
            Train.TCRouteElement thisElement = null;
            TrackCircuitSection thisSection = null;

            List <int> sectionsInRoute = new List<int>();

            float clearedDistanceM = 0.0f;
            Train.END_AUTHORITY endAuthority = Train.END_AUTHORITY.NO_PATH_RESERVED;

            int routeIndex = -1;
            float maxDistance = Math.Max(thisTrain.Train.AllowedMaxSpeedMpS * thisTrain.Train.maxTimeS, thisTrain.Train.minCheckDistanceM);

            int lastReserved = thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex];
            int endListIndex = -1;

            bool furthestRouteCleared = false;

            Train.TCSubpathRoute thisRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
            Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

            // for loop detection, set occupied sections in sectionsInRoute list - but remove present position

            foreach (TrackCircuitSection occSection in thisTrain.Train.OccupiedTrack)
            {
                sectionsInRoute.Add(occSection.Index);
            }
            sectionsInRoute.Remove(thisPosition.TCSectionIndex);

            // check if last reserved on present route

            if (lastReserved > 0)
            {

                endListIndex = thisRoute.GetRouteIndex(lastReserved, thisPosition.RouteListIndex);

                // check if backward in route - if so, route is valid and obstacle is in present section

                if (endListIndex < 0)
                {
                    int prevListIndex = -1;
                    for (int iNode = thisPosition.RouteListIndex; iNode >= 0 && prevListIndex < 0; iNode--)
                    {
                        thisElement = thisRoute[iNode];
                        if (thisElement.TCSectionIndex == lastReserved)
                        {
                            prevListIndex = iNode;
                        }
                    }

                    if (prevListIndex < 0)     // section is really off route - perform request from present position
                    {
                        BreakDownRoute(thisPosition.TCSectionIndex, thisTrain);
                    }
                }
            }

            if (thisTrain.Train.CheckTrain)
            {
                if (endListIndex >= 0)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Index in route list : " + endListIndex + " = " +
                            thisRoute[endListIndex].TCSectionIndex.ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Index in route list : " + endListIndex + "\n");
                }
            }

            // if section is (still) set, check if this is at maximum distance

            if (endListIndex >= 0)
            {
                routeIndex = endListIndex;
                clearedDistanceM = thisTrain.Train.GetDistanceToTrain(lastReserved, 0.0f);

                if (clearedDistanceM > maxDistance)
                {
                    endAuthority = Train.END_AUTHORITY.MAX_DISTANCE;
                    furthestRouteCleared = true;
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Cleared Distance : " +
                                            FormatStrings.FormatDistance(clearedDistanceM, true) +
                            " > Max Distance \n");
                    }

                }
                else
                {
                    for (int iIndex = thisPosition.RouteListIndex + 1; iIndex < routeIndex; iIndex++)
                    {
                        sectionsInRoute.Add(thisRoute[iIndex].TCSectionIndex);
                    }
                }
            }
            else
            {
                routeIndex = thisPosition.RouteListIndex;   // obstacle is in present section
            }

            if (routeIndex < 0) return;//by JTang
            
            int lastRouteIndex = routeIndex;
            float offset = 0.0f;
            if (routeIndex == thisPosition.RouteListIndex)
            {
                offset = thisPosition.TCOffset;
            }

            // if authority type is loop and loop section is still occupied by train, no need for any checks

            if (thisTrain.Train.LoopSection >= 0)
            {
                thisSection = TrackCircuitList[thisTrain.Train.LoopSection];
                if (thisSection.CircuitState.ThisTrainOccupying(thisTrain.Train) || 
                    (thisSection.CircuitState.TrainReserved != null && thisSection.CircuitState.TrainReserved.Train == thisTrain.Train))
                {
                    furthestRouteCleared = true;
                    endAuthority = Train.END_AUTHORITY.LOOP;
                }
                else
                {
                    // update trains ValidRoute to avoid continuation at wrong entry
                    int rearIndex = thisTrain.Train.PresentPosition[1].RouteListIndex;
                    int nextIndex = routePart.GetRouteIndex(thisTrain.Train.LoopSection, rearIndex);
                    int firstIndex = routePart.GetRouteIndex(thisTrain.Train.LoopSection, 0);

                    if (firstIndex != nextIndex)
                    {
                        for (int iIndex = 0; iIndex <= firstIndex; iIndex++)
                        {
                            thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][iIndex].TCSectionIndex = -1; // invalidate route upto loop point
                        }
                        routePart = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
                    }
                 
                    thisTrain.Train.LoopSection = -1;
                }
            }

            // try to clear further ahead if required

            if (!furthestRouteCleared)
            {

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Starting check from : " +
                            "Index in route list : " + routeIndex + " = " +
                            thisRoute[routeIndex].TCSectionIndex.ToString() + "\n");
                }

                // check if train ahead still in last available section

                bool routeAvailable = true;
                thisSection = TrackCircuitList[routePart[routeIndex].TCSectionIndex];

                Dictionary<Train, float> trainAhead =
                        thisSection.TestTrainAhead(thisTrain.Train, thisPosition.TCOffset, thisPosition.TCDirection);

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Train ahead in section " + thisSection.Index.ToString() + " : " +
                            trainAhead.Count.ToString() + "\n");
                }

                if (trainAhead.Count > 0)
                {
                    routeAvailable = false;
                    lastRouteIndex = routeIndex - 1;
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Set last valid section : " +
                                "Index in route list : " + lastRouteIndex + " = " +
                                thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][lastRouteIndex].TCSectionIndex.ToString() + "\n");
                    }
                }

                // train ahead has moved on, check next sections

                while (routeIndex < routePart.Count && routeAvailable && !furthestRouteCleared)
                {
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Checking : Index in route list : " + routeIndex + " = " +
                                thisRoute[routeIndex].TCSectionIndex.ToString() + "\n");
                    }

                    thisElement = routePart[routeIndex];
                    sectionIndex = thisElement.TCSectionIndex;
                    thisSection = TrackCircuitList[sectionIndex];

                    // check if section is in loop

                    if (sectionsInRoute.Contains(thisSection.Index))
                    {
                        endAuthority = Train.END_AUTHORITY.LOOP;
                        thisTrain.Train.LoopSection = thisSection.Index;
                        routeAvailable = false;

                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Section looped \n");
                        }
                    }

                    // check if section is available

                    else if (thisSection.IsAvailable(thisTrain))
                    {
                        lastReserved = thisSection.Index;
                        lastRouteIndex = routeIndex;
                        sectionsInRoute.Add(thisSection.Index);
                        clearedDistanceM += thisSection.Length - offset;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Section clear \n");
                        }

                        routeIndex++;
                        offset = 0.0f;

                        if (!thisSection.CircuitState.ThisTrainOccupying(thisTrain) &&
                            thisSection.CircuitState.TrainReserved == null)
                        {
                            thisSection.Reserve(thisTrain, routePart);
                        }

                        if (thisSection.EndSignals[thisElement.Direction] != null)
                        {
                            thisTrain.Train.SwitchToSignalControl(thisSection.EndSignals[thisElement.Direction]);
                            furthestRouteCleared = true;
                            if (thisTrain.Train.CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt",
                                     "Has end signal : " + thisSection.EndSignals[thisElement.Direction].thisRef.ToString() + "\n");
                            }
                        }

                        if (clearedDistanceM > thisTrain.Train.minCheckDistanceM &&
                                        clearedDistanceM > (thisTrain.Train.AllowedMaxSpeedMpS * thisTrain.Train.maxTimeS))
                        {
                            endAuthority = Train.END_AUTHORITY.MAX_DISTANCE;
                            furthestRouteCleared = true;
                        }


                    }
                    else
                    {
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Section blocked \n");
                        }
                        lastRouteIndex = routeIndex - 1;
                        lastReserved = lastRouteIndex >= 0 ? routePart[lastRouteIndex].TCSectionIndex : -1;
                        routeAvailable = false;
                    }
                }
            }

            // if not cleared to max distance or looped, determine reason

            if (!furthestRouteCleared && lastRouteIndex > 0 && endAuthority != Train.END_AUTHORITY.LOOP)
            {

                thisElement = routePart[lastRouteIndex];
                sectionIndex = thisElement.TCSectionIndex;
                thisSection = TrackCircuitList[sectionIndex];

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Last section cleared in route list : " + lastRouteIndex + " = " +
                            thisRoute[lastRouteIndex].TCSectionIndex.ToString() + "\n");
                }
                // end of track reached

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                {
                    endAuthority = Train.END_AUTHORITY.END_OF_TRACK;
                    furthestRouteCleared = true;
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "End od track \n");
                    }
                }

                // end of path reached

                if (!furthestRouteCleared)
                {
                    if (lastRouteIndex >= (routePart.Count - 1))
                    {
                        endAuthority = Train.END_AUTHORITY.END_OF_PATH;
                        furthestRouteCleared = true;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "End of path \n");
                        }
                    }
                }
            }

            // check if next section is switch held against train

            if (!furthestRouteCleared && lastRouteIndex < (routePart.Count - 1))
            {
                Train.TCRouteElement nextElement = routePart[lastRouteIndex + 1];
                sectionIndex = nextElement.TCSectionIndex;
                thisSection = TrackCircuitList[sectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                    thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    if (!thisSection.IsAvailable(thisTrain))
                    {
                        endAuthority = Train.END_AUTHORITY.RESERVED_SWITCH;
                        furthestRouteCleared = true;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Reserved Switch \n");
                        }
                    }
                }
            }

            // check if next section is occupied by stationary train or train moving in similar direction
            // if so calculate distance to end of train
            // only allowed for NORMAL sections and if not looped

            if (!furthestRouteCleared && lastRouteIndex < (routePart.Count - 1) && endAuthority != Train.END_AUTHORITY.LOOP)
            {
                Train.TCRouteElement nextElement = routePart[lastRouteIndex + 1];
                int reqDirection = nextElement.Direction;
                int revDirection = nextElement.Direction == 0 ? 1 : 0;

                sectionIndex = nextElement.TCSectionIndex;
                thisSection = TrackCircuitList[sectionIndex];

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL &&
                           thisSection.CircuitState.HasTrainsOccupying())
                {
                    if (thisSection.CircuitState.HasTrainsOccupying(revDirection, false))
                    {
                        endAuthority = Train.END_AUTHORITY.TRAIN_AHEAD;
                        furthestRouteCleared = true;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Train Ahead \n");
                        }
                    }
                    else
                    {
                        Dictionary<Train, float> trainAhead =
                                                thisSection.TestTrainAhead(thisTrain.Train, offset, reqDirection);

                        if (trainAhead.Count > 0)
                        {
                            foreach (KeyValuePair<Train, float> thisTrainAhead in trainAhead)  // there is only one value
                            {
                                endAuthority = Train.END_AUTHORITY.TRAIN_AHEAD;
                                clearedDistanceM += thisTrainAhead.Value;
                                furthestRouteCleared = true;
                                if (thisTrain.Train.CheckTrain)
                                {
                                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                            "Train Ahead \n");
                                }
                            }
                        }
                    }
                }
                else if (!thisSection.IsAvailable(thisTrain))
                {
                    endAuthority = Train.END_AUTHORITY.END_OF_AUTHORITY;
                    furthestRouteCleared = true;
                }
            }

            if (routeIndex >= routePart.Count)
            {
                endAuthority = Train.END_AUTHORITY.END_OF_AUTHORITY;
            }

            // update train details

            thisTrain.Train.EndAuthorityType[thisTrain.TrainRouteDirectionIndex] = endAuthority;
            thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex] = lastReserved;
            thisTrain.Train.DistanceToEndNodeAuthorityM[thisTrain.TrainRouteDirectionIndex] = clearedDistanceM;
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                        "Returned : \n" +
                        "    State : " + endAuthority.ToString() + "\n" +
                        "    Dist  : " + FormatStrings.FormatDistance(clearedDistanceM, true) + "\n" +
                        "    Sect  : " + lastReserved);

                File.AppendAllText(@"C:\temp\checktrain.txt", "\n");
            }
        }

        //================================================================================================//
        //
        // Break down reserved route
        //

        public void BreakDownRoute(int firstSectionIndex, Train.TrainRouted reqTrain)
        {
            if (firstSectionIndex < 0)
                return; // no route to break down

            TrackCircuitSection firstSection = TrackCircuitList[firstSectionIndex];
            Train.TrainRouted thisTrain = firstSection.CircuitState.TrainReserved;

            // if occupied by train - skip actions and proceed to next section

            if (firstSection.CircuitState.ThisTrainOccupying(reqTrain))
            {

                // if not reserved - no further route ahead

                if (thisTrain == null)
                {
                    return;
                }

                if (thisTrain != reqTrain)
                {
                    return;   // section reserved for other train - stop action
                }

                // unreserve first section

                firstSection.UnreserveTrain(thisTrain, true);
            }

            // check which direction to go

            TrackCircuitSection nextSection = null;
            int nextDirection = 0;

            for (int iPinLink = 0; iPinLink <= 1; iPinLink++)
            {
                for (int iPinIndex = 0; iPinIndex <= 1; iPinIndex++)
                {
                    int trySectionIndex = firstSection.Pins[iPinLink, iPinIndex].Link;
                    if (trySectionIndex > 0)
                    {
                        TrackCircuitSection trySection = TrackCircuitList[trySectionIndex];
                        if (trySection.CircuitState.TrainReserved != null && reqTrain != null && trySection.CircuitState.TrainReserved.Train == reqTrain.Train)
                        {
                            nextSection = trySection;
                            nextDirection = firstSection.Pins[iPinLink, iPinIndex].Direction;
                        }
                    }
                }
            }

            // run back through all reserved sections

            while (nextSection != null)
            {
                nextSection.UnreserveTrain(reqTrain, true);
                TrackCircuitSection thisSection = nextSection;
                nextSection = null;

                // try to find next section using active links

                TrackCircuitSection trySection = null;

                int iPinLink = nextDirection == 0 ? 1 : 0;
                for (int iPinIndex = 0; iPinIndex <= 1; iPinIndex++)
                {
                    int trySectionIndex = thisSection.ActivePins[iPinLink, iPinIndex].Link;
                    if (trySectionIndex > 0)
                    {
                        trySection = TrackCircuitList[trySectionIndex];
                        if (trySection.CircuitState.TrainReserved != null && reqTrain != null && trySection.CircuitState.TrainReserved.Train == reqTrain.Train)
                        {
                            nextSection = trySection;
                            nextDirection = thisSection.ActivePins[iPinLink, iPinIndex].Direction;
                        }
                    }
                }

                // not found, then try possible links

                for (int iPinIndex = 0; iPinIndex <= 1; iPinIndex++)
                {
                    int trySectionIndex = thisSection.Pins[iPinLink, iPinIndex].Link;
                    if (trySectionIndex > 0)
                    {
                        trySection = TrackCircuitList[trySectionIndex];
                        if (trySection.CircuitState.TrainReserved != null && reqTrain != null && trySection.CircuitState.TrainReserved.Train == reqTrain.Train)
                        {
                            nextSection = trySection;
                            nextDirection = thisSection.Pins[iPinLink, iPinIndex].Direction;
                        }
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Break down reserved route using route list
        //

        public void BreakDownRouteList(Train.TCSubpathRoute reqRoute, int firstRouteIndex, Train.TrainRouted reqTrain)
        {
            for (int iindex = reqRoute.Count - 1; iindex >= 0 && iindex >= firstRouteIndex; iindex--)
            {
                TrackCircuitSection thisSection = TrackCircuitList[reqRoute[iindex].TCSectionIndex];
                if (!thisSection.CircuitState.ThisTrainOccupying(reqTrain.Train))
                {
                    thisSection.RemoveTrain(reqTrain.Train, true);
                }
                else
                {
                    SignalObject thisSignal = thisSection.EndSignals[reqRoute[iindex].Direction];
                    if (thisSignal != null)
                    {
                        thisSignal.ResetSignal(false);
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Build temp route for train
        // Used for trains without path (eg stationary constists), manual operation
        //

        public Train.TCSubpathRoute BuildTempRoute(Train thisTrain,
                int firstSectionIndex, float firstOffset, int firstDirection,
                float routeLength, bool stopAtSignal, bool overrideManualSwitchState, bool autoAlign)
        {
            bool honourManualSwitchState = !overrideManualSwitchState;
            List<int> sectionList = ScanRoute(thisTrain, firstSectionIndex, firstOffset, firstDirection,
                    true, routeLength, honourManualSwitchState, autoAlign, false, false, true, false, false, false, false, false);
            Train.TCSubpathRoute tempRoute = new Train.TCSubpathRoute();
            int lastIndex = -1;

            foreach (int nextSectionIndex in sectionList)
            {
                int curDirection = nextSectionIndex < 0 ? 1 : 0;
                int thisSectionIndex = nextSectionIndex < 0 ? -nextSectionIndex : nextSectionIndex;
                TrackCircuitSection thisSection = TrackCircuitList[thisSectionIndex];

                Train.TCRouteElement thisElement = new Train.TCRouteElement(thisSection, curDirection, this, lastIndex);
                tempRoute.Add(thisElement);
                lastIndex = thisSectionIndex;
            }

            return (tempRoute);
        }

        //================================================================================================//
        //
        // Follow default route for train
        // Use for :
        //   - build temp list for trains without route (eg stat objects)
        //   - build list for train under Manual control
        //   - build list of sections when train slip backward
        //   - search signal or speedpost ahead or at the rear of the train (either in facing or backward direction)
        //
        // Search ends :
        //   - if required object is found
        //   - if required length is covered
        //   - if valid path only is requested and unreserved section is found (variable thisTrain required)
        //   - end of track
        //   - looped track
        //
        // Returned is list of sections, with positive no. indicating direction 0 and negative no. indicating direction 1
        // If signal or speedpost is required, list will contain index of required item (>0 facing direction, <0 backing direction)
        //

        public List<int> ScanRoute(Train thisTrain, int firstSectionIndex, float firstOffset, int firstDirection, bool forward,
                float routeLength, bool honourManualSwitch, bool autoAlign, bool stopAtFacingSignal, bool reservedOnly, bool returnSections,
                bool searchFacingSignal, bool searchBackwardSignal, bool searchFacingSpeedpost, bool searchBackwardSpeedpost,
                bool isFreight)
        {

            int sectionIndex = firstSectionIndex;

            int lastIndex = -2;   // set to values not encountered for pin links
            int thisIndex = sectionIndex;

            float offset = firstOffset;
            int curDirection = firstDirection;
            int nextDirection = curDirection;

            TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];

            float coveredLength = firstOffset;
            if (forward || (firstDirection == 1 && !forward))
            {
                coveredLength = thisSection.Length - firstOffset;
            }

            bool endOfRoute = false;
            List<int> foundItems = new List<int>();
            List<int> foundObject = new List<int>();

            while (!endOfRoute)
            {

                // check looped

                int routedIndex = curDirection == 0 ? thisIndex : -thisIndex;
                if (foundItems.Contains(routedIndex))
                {
                    break;
                }

                // add section
                foundItems.Add(routedIndex);

                // set length, pin index and opp direction

                int oppDirection = curDirection == 0 ? 1 : 0;

                int outPinIndex = forward ? curDirection : oppDirection;
                int inPinIndex = outPinIndex == 0 ? 1 : 0;

                // check all conditions and objects as required

                if (stopAtFacingSignal && thisSection.EndSignals[curDirection] != null)           // stop at facing signal
                {
                    endOfRoute = true;
                }

                if (searchFacingSignal && thisSection.EndSignals[curDirection] != null)           // search facing signal
                {
                    foundObject.Add(thisSection.EndSignals[curDirection].thisRef);
                    endOfRoute = true;
                }

                // search facing speedpost
                if (searchFacingSpeedpost && thisSection.CircuitItems.TrackCircuitSpeedPosts[curDirection].TrackCircuitItem.Count > 0)
                {
                    List<TrackCircuitSignalItem> thisItemList = thisSection.CircuitItems.TrackCircuitSpeedPosts[curDirection].TrackCircuitItem;

                    if (forward)
                    {
                        for (int iObject = 0; iObject < thisItemList.Count && !endOfRoute; iObject++)
                        {
                            TrackCircuitSignalItem thisItem = thisItemList[iObject];

                            SignalObject thisSpeedpost = thisItem.SignalRef;
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);

                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0))
                            {
                                if (thisItem.SignalLocation > offset)
                                {
                                    foundObject.Add(thisItem.SignalRef.thisRef);
                                    endOfRoute = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int iObject = thisItemList.Count - 1; iObject >= 0 && !endOfRoute; iObject--)
                        {
                            TrackCircuitSignalItem thisItem = thisItemList[iObject];

                            SignalObject thisSpeedpost = thisItem.SignalRef;
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);

                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0))
                            {
                                if (offset == 0 || thisItem.SignalLocation < offset)
                                {
                                    foundObject.Add(thisItem.SignalRef.thisRef);
                                    endOfRoute = true;
                                }
                            }
                        }
                    }
                }

                // search backward speedpost
                if (searchBackwardSpeedpost && thisSection.CircuitItems.TrackCircuitSpeedPosts[oppDirection].TrackCircuitItem.Count > 0)
                {
                    List<TrackCircuitSignalItem> thisItemList = thisSection.CircuitItems.TrackCircuitSpeedPosts[oppDirection].TrackCircuitItem;

                    if (forward)
                    {
                        for (int iObject = thisItemList.Count - 1; iObject >= 0 && !endOfRoute; iObject--)
                        {
                            TrackCircuitSignalItem thisItem = thisItemList[iObject];

                            SignalObject thisSpeedpost = thisItem.SignalRef;
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);

                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0))
                            {
                                if (thisItem.SignalLocation < thisSection.Length - offset)
                                {
                                    endOfRoute = true;
                                    foundObject.Add(-(thisItem.SignalRef.thisRef));
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int iObject = 0; iObject < thisItemList.Count - 1 && !endOfRoute; iObject++)
                        {
                            TrackCircuitSignalItem thisItem = thisItemList[iObject];

                            SignalObject thisSpeedpost = thisItem.SignalRef;
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);

                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0))
                            {
                                if (offset == 0 || thisItem.SignalLocation > thisSection.Length - offset)
                                {
                                    endOfRoute = true;
                                    foundObject.Add(-(thisItem.SignalRef.thisRef));
                                }
                            }
                        }
                    }
                }

                // move to next section
                // follow active links if set, otherwise default links (=0)

                int nextIndex = -1;
                switch (thisSection.CircuitType)
                {
                    case TrackCircuitSection.CIRCUITTYPE.CROSSOVER:
                        if (thisSection.Pins[inPinIndex, 0].Link == lastIndex)
                        {
                            nextIndex = thisSection.Pins[outPinIndex, 0].Link;
                            nextDirection = thisSection.Pins[outPinIndex, 0].Direction;
                        }
                        else if (thisSection.Pins[inPinIndex, 1].Link == lastIndex)
                        {
                            nextIndex = thisSection.Pins[outPinIndex, 1].Link;
                            nextDirection = thisSection.Pins[outPinIndex, 1].Direction;
                        }
                        break;

                    case TrackCircuitSection.CIRCUITTYPE.JUNCTION:
                        if (thisSection.ActivePins[outPinIndex, 0].Link > 0)
                        {
                            nextIndex = thisSection.ActivePins[outPinIndex, 0].Link;
                            nextDirection = thisSection.ActivePins[outPinIndex, 0].Direction;
                        }
                        else if (thisSection.ActivePins[outPinIndex, 1].Link > 0)
                        {
                            nextIndex = thisSection.ActivePins[outPinIndex, 1].Link;
                            nextDirection = thisSection.ActivePins[outPinIndex, 1].Direction;
                        }
                        else if (honourManualSwitch && thisSection.JunctionSetManual >= 0)
                        {
                            nextIndex = thisSection.Pins[outPinIndex, thisSection.JunctionSetManual].Link;
                            nextDirection = thisSection.Pins[outPinIndex, thisSection.JunctionSetManual].Direction;
                        }
                        else if (!reservedOnly)
                        {
                            nextIndex = thisSection.Pins[outPinIndex, thisSection.JunctionLastRoute].Link;
                            nextDirection = thisSection.Pins[outPinIndex, thisSection.JunctionLastRoute].Direction;
                        }
                        break;

                    case TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK:
                        break;

                    default:
                        nextIndex = thisSection.Pins[outPinIndex, 0].Link;
                        nextDirection = thisSection.Pins[outPinIndex, 0].Direction;

                        TrackCircuitSection nextSection = TrackCircuitList[nextIndex];

                        // if next section is junction : check if locked agains AI and if auto-alignment allowed
                        // switchable end of switch is always pin direction 1
                        if (nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                        {
                            int nextPinDirection = nextDirection == 0 ? 1 : 0;
                            int nextPinIndex = nextSection.Pins[(nextDirection == 0 ? 1 : 0), 0].Link == thisIndex ? 0 : 1;
                            if (nextPinDirection == 1 && nextSection.JunctionLastRoute != nextPinIndex)
                            {
                                if (nextSection.AILock && thisTrain != null && thisTrain.TrainType == Train.TRAINTYPE.AI)
                                {
                                    endOfRoute = true;
                                }

                                if (!autoAlign)
                                {
                                    endOfRoute = true;
                                }
                            }
                        }

                        break;
                }

                if (nextIndex < 0)
                {
                    endOfRoute = true;
                }
                else
                {
                    lastIndex = thisIndex;
                    thisIndex = nextIndex;
                    thisSection = TrackCircuitList[thisIndex];
                    curDirection = forward ? nextDirection : nextDirection == 0 ? 1 : 0;
                    oppDirection = curDirection == 0 ? 1 : 0;

                    if (searchBackwardSignal && thisSection.EndSignals[oppDirection] != null)
                    {
                        endOfRoute = true;
                        foundObject.Add(-(thisSection.EndSignals[oppDirection].thisRef));
                    }
                }

                if (!endOfRoute)
                {
                    offset = 0.0f;

                    if (thisTrain != null && reservedOnly)
                    {
                        TrackCircuitState thisState = thisSection.CircuitState;

                        if (!thisState.TrainOccupy.ContainsTrain(thisTrain) && 
                            (thisState.TrainReserved != null && thisState.TrainReserved.Train != thisTrain))
                        {
                            endOfRoute = true;
                        }
                    }
                }

                if (!endOfRoute && routeLength > 0)
                {
                    endOfRoute = (coveredLength > routeLength);
                    coveredLength += thisSection.Length;
                }

            }

            if (returnSections)
            {
                return (foundItems);
            }
            else
            {
                return (foundObject);
            }
        }

        //================================================================================================//
        //
        // Process Platforms
        //

        private void ProcessPlatforms(Dictionary<int, int> platformList, TrItem[] TrItems,
                TrackNode[] trackNodes)
        {
            foreach (KeyValuePair<int, int> thisPlatformIndex in platformList)
            {
                int thisPlatformDetailsIndex;

                // get platform item

                int thisIndex = thisPlatformIndex.Key;

                var thisPlatform = TrItems[thisIndex] is PlatformItem ? (PlatformItem)TrItems[thisIndex] : new PlatformItem((SidingItem)TrItems[thisIndex]);

                TrackNode thisNode = trackNodes[thisPlatformIndex.Value];

                // check if entry already created for related entry

                int relatedIndex = (int)thisPlatform.LinkedPlatformItemId;

                PlatformDetails thisDetails;
                int refIndex;
                bool splitPlatform = false;

                // get related platform details

                if (PlatformXRefList.ContainsKey(relatedIndex))
                {
                    thisPlatformDetailsIndex = PlatformXRefList[relatedIndex];
                    thisDetails = PlatformDetailsList[thisPlatformDetailsIndex];
                    PlatformXRefList.Add(thisIndex, thisPlatformDetailsIndex);
                    refIndex = 1;
                }

        // create new platform details

                else
                {
                    thisDetails = new PlatformDetails(thisIndex);
                    PlatformDetailsList.Add(thisDetails);
                    thisPlatformDetailsIndex = PlatformDetailsList.Count - 1;
                    PlatformXRefList.Add(thisIndex, thisPlatformDetailsIndex);
                    refIndex = 0;
                }

                // get tracksection

                int TCSectionIndex = -1;
                int TCXRefIndex = -1;

                for (int iXRef = thisNode.TCCrossReference.Count - 1; iXRef >= 0 && TCSectionIndex < 0; iXRef--)
                {
                    if (thisPlatform.SData1 <
                     (thisNode.TCCrossReference[iXRef].Position[1] + thisNode.TCCrossReference[iXRef].Length))
                    {
                        TCSectionIndex = thisNode.TCCrossReference[iXRef].CrossRefIndex;
                        TCXRefIndex = iXRef;
                    }
                }

                if (TCSectionIndex < 0)
                {
                    Trace.TraceInformation("Cannot locate TCSection for platform {0}", thisIndex);
                    TCSectionIndex = thisNode.TCCrossReference[0].CrossRefIndex;
                    TCXRefIndex = 0;
                }

                // if first entry, set tracksection

                if (refIndex == 0)
                {
                    thisDetails.TCSectionIndex.Add(TCSectionIndex);
                }

        // if second entry, test if equal - if not, build list

                else
                {
                    if (TCSectionIndex != thisDetails.TCSectionIndex[0])
                    {
                        int firstXRef = -1;
                        for (int iXRef = thisNode.TCCrossReference.Count - 1; iXRef >= 0 && firstXRef < 0; iXRef--)
                        {
                            if (thisNode.TCCrossReference[iXRef].CrossRefIndex == thisDetails.TCSectionIndex[0])
                            {
                                firstXRef = iXRef;
                            }
                        }

                        if (firstXRef < 0)  // platform is split by junction !!!
                        {
                            ResolveSplitPlatform(ref thisDetails, TCSectionIndex, thisPlatform, thisNode,
                                    TrItems, trackNodes);
                            splitPlatform = true;
                            Trace.TraceInformation("Platform split by junction at " + thisDetails.Name);
                        }
                        else if (TCXRefIndex < firstXRef)
                        {
                            thisDetails.TCSectionIndex.Clear();
                            for (int iXRef = TCXRefIndex; iXRef <= firstXRef; iXRef++)
                            {
                                thisDetails.TCSectionIndex.Add(thisNode.TCCrossReference[iXRef].CrossRefIndex);
                            }
                        }
                        else
                        {
                            thisDetails.TCSectionIndex.Clear();
                            for (int iXRef = firstXRef; iXRef <= TCXRefIndex; iXRef++)
                            {
                                thisDetails.TCSectionIndex.Add(thisNode.TCCrossReference[iXRef].CrossRefIndex);
                            }
                        }
                    }
                }

                // set details (if not split platform)

                if (!splitPlatform)
                {
                    TrackCircuitSection thisSection = TrackCircuitList[TCSectionIndex];

                    thisDetails.PlatformReference[refIndex] = thisIndex;
                    thisDetails.nodeOffset[refIndex] = thisPlatform.SData1;
                    thisDetails.TCOffset[refIndex, 1] = thisPlatform.SData1 - thisSection.OffsetLength[1];
                    thisDetails.TCOffset[refIndex == 1 ? 0 : 1, 0] = thisSection.Length - thisDetails.TCOffset[refIndex, 1];
                }

                if (refIndex == 0)
                {
                    thisDetails.Name = String.Copy(thisPlatform.Station);
                    thisDetails.MinWaitingTime = thisPlatform.PlatformMinWaitingTime;
                }
                else if (!splitPlatform)
                {
                    thisDetails.Length = Math.Abs(thisDetails.nodeOffset[1] - thisDetails.nodeOffset[0]);
                }

                // check if direction correct, else swap 0 - 1 entries for offsets etc.

                if (refIndex == 1 && thisDetails.nodeOffset[1] < thisDetails.nodeOffset[0] && !splitPlatform)
                {
                    float tf;
                    tf = thisDetails.nodeOffset[0];
                    thisDetails.nodeOffset[0] = thisDetails.nodeOffset[1];
                    thisDetails.nodeOffset[1] = tf;

                    for (int iDir = 0; iDir <= 1; iDir++)
                    {
                        tf = thisDetails.TCOffset[iDir, 0];
                        thisDetails.TCOffset[iDir, 0] = thisDetails.TCOffset[iDir, 1];
                        thisDetails.TCOffset[iDir, 1] = tf;
                    }
                }

                // search for end signals

                thisNode = trackNodes[TrackCircuitList[thisDetails.TCSectionIndex[0]].OriginalIndex];

                if (refIndex == 1)
                {
                    float distToSignal = 0.0f;
                    float offset = thisDetails.TCOffset[1, 0];
                    int lastSection = thisDetails.TCSectionIndex[thisDetails.TCSectionIndex.Count - 1];
                    int lastSectionXRef = -1;

                    for (int iXRef = 0; iXRef < thisNode.TCCrossReference.Count; iXRef++)
                    {
                        if (lastSection == thisNode.TCCrossReference[iXRef].CrossRefIndex)
                        {
                            lastSectionXRef = iXRef;
                            break;
                        }
                    }

                    for (int iXRef = lastSectionXRef; iXRef < thisNode.TCCrossReference.Count; iXRef++)
                    {
                        int sectionIndex = thisNode.TCCrossReference[iXRef].CrossRefIndex;
                        TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];

                        distToSignal += thisSection.Length - offset;
                        offset = 0.0f;

                        if (thisSection.EndSignals[0] != null)
                        {
                            if (!thisSection.EndSignals[0].hasFixedRoute)
                            {
                                thisDetails.EndSignals[0] = thisSection.EndSignals[0].thisRef;
                                thisDetails.DistanceToSignals[0] = distToSignal;
                            }
                            break;
                        }
                    }

                    distToSignal = 0.0f;
                    offset = thisDetails.TCOffset[1, 1];
                    int firstSection = thisDetails.TCSectionIndex[0];
                    int firstSectionXRef = lastSectionXRef;

                    if (lastSection != firstSection)
                    {
                        for (int iXRef = 0; iXRef < thisNode.TCCrossReference.Count; iXRef++)
                        {
                            if (firstSection == thisNode.TCCrossReference[iXRef].CrossRefIndex)
                            {
                                firstSectionXRef = iXRef;
                                break;
                            }
                        }
                    }

                    for (int iXRef = firstSectionXRef; iXRef >= 0; iXRef--)
                    {
                        int sectionIndex = thisNode.TCCrossReference[iXRef].CrossRefIndex;
                        TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];

                        distToSignal += thisSection.Length - offset;
                        offset = 0.0f;

                        if (thisSection.EndSignals[1] != null)
                        {
                            if (!thisSection.EndSignals[1].hasFixedRoute)
                            {
                                thisDetails.EndSignals[1] = thisSection.EndSignals[1].thisRef;
                                thisDetails.DistanceToSignals[1] = distToSignal;
                            }
                            break;
                        }

                    }
                }

                // set section crossreference


                if (refIndex == 1)
                {
                    foreach (int sectionIndex in thisDetails.TCSectionIndex)
                    {
                        TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];
                        thisSection.PlatformIndex.Add(thisPlatformDetailsIndex);
                    }
                }
            }
        }// ProcessPlatforms

        //================================================================================================//
        //
        // Resolve split platforms
        //

        public void ResolveSplitPlatform(ref PlatformDetails thisDetails, int secondSectionIndex,
                PlatformItem secondPlatform, TrackNode secondNode,
                    TrItem[] TrItems, TrackNode[] trackNodes)
        {
            // get all positions related to tile of first platform item

            PlatformItem firstPlatform = (TrItems[thisDetails.PlatformReference[0]] is PlatformItem) ?
                    (PlatformItem)TrItems[thisDetails.PlatformReference[0]] :
                    new PlatformItem((SidingItem)TrItems[thisDetails.PlatformReference[0]]);

            int firstSectionIndex = thisDetails.TCSectionIndex[0];
            TrackCircuitSection thisSection = TrackCircuitList[firstSectionIndex];
            TrackNode firstNode = trackNodes[thisSection.OriginalIndex];

            // first platform
            int TileX1 = firstPlatform.TileX;
            int TileZ1 = firstPlatform.TileZ;
            float X1 = firstPlatform.X;
            float Z1 = firstPlatform.Z;

            // start node position
            int TS1TileX = firstNode.TrVectorNode.TrVectorSections[0].TileX;
            int TS1TileZ = firstNode.TrVectorNode.TrVectorSections[0].TileZ;
            float TS1X = firstNode.TrVectorNode.TrVectorSections[0].X;
            float TS1Z = firstNode.TrVectorNode.TrVectorSections[0].Z;

            float TS1Xc = TS1X + (TS1TileX - TileX1) * 2048;
            float TS1Zc = TS1Z + (TS1TileZ - TileZ1) * 2048;

            // second platform
            int TileX2 = secondPlatform.TileX;
            int TileZ2 = secondPlatform.TileZ;
            float X2 = secondPlatform.X;
            float Z2 = secondPlatform.Z;

            float X2c = X2 + (TileX2 - TileX1) * 2048;
            float Z2c = Z2 + (TileZ2 - TileZ1) * 2048;

            int TS2TileX = secondNode.TrVectorNode.TrVectorSections[0].TileX;
            int TS2TileZ = secondNode.TrVectorNode.TrVectorSections[0].TileZ;
            float TS2X = secondNode.TrVectorNode.TrVectorSections[0].X;
            float TS2Z = secondNode.TrVectorNode.TrVectorSections[0].Z;

            float TS2Xc = TS2X + (TS2TileX - TileX1) * 2048;
            float TS2Zc = TS2Z + (TS2TileZ - TileZ1) * 2048;

            // determine if 2nd platform is towards end or begin of tracknode - use largest delta for check

            float dXplatform = X2c - X1;
            float dXnode = TS1Xc - X1;
            float dZplatform = Z2c - Z1;
            float dZnode = TS1Zc - Z1;

            float dplatform = Math.Abs(dXplatform) > Math.Abs(dZplatform) ? dXplatform : dZplatform;
            float dnode = Math.Abs(dXplatform) > Math.Abs(dXplatform) ? dXnode : dZnode;  // use same delta direction!

            // if towards begin : build list of sections from start

            List<int> PlSections1 = new List<int>();
            bool reqSectionFound = false;
            float totalLength1 = 0;
            int direction1 = 0;

            if (Math.Sign(dplatform) == Math.Sign(dnode))
            {
                for (int iXRef = firstNode.TCCrossReference.Count - 1; iXRef >= 0 && !reqSectionFound; iXRef--)
                {
                    int thisIndex = firstNode.TCCrossReference[iXRef].CrossRefIndex;
                    PlSections1.Add(thisIndex);
                    totalLength1 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = (thisIndex == firstSectionIndex);
                }
                totalLength1 -= thisDetails.TCOffset[1, 0];  // correct for offset
            }
            else
            {
                for (int iXRef = 0; iXRef < firstNode.TCCrossReference.Count && !reqSectionFound; iXRef++)
                {
                    int thisIndex = firstNode.TCCrossReference[iXRef].CrossRefIndex;
                    PlSections1.Add(thisIndex);
                    totalLength1 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = (thisIndex == firstSectionIndex);
                    direction1 = 1;
                }
                totalLength1 -= thisDetails.TCOffset[0, 1];  // correct for offset
            }

            // determine if 1st platform is towards end or begin of tracknode - use largest delta for check

            dXplatform = X1 - X2c;
            dXnode = TS2Xc - X2c;
            dZplatform = Z1 - Z2c;
            dZnode = TS2Zc - Z2c;

            dplatform = Math.Abs(dXplatform) > Math.Abs(dZplatform) ? dXplatform : dZplatform;
            dnode = Math.Abs(dXplatform) > Math.Abs(dXplatform) ? dXnode : dZnode;  // use same delta direction!

            // if towards begin : build list of sections from start

            List<int> PlSections2 = new List<int>();
            reqSectionFound = false;
            float totalLength2 = 0;
            int direction2 = 0;

            if (Math.Sign(dplatform) == Math.Sign(dnode))
            {
                for (int iXRef = secondNode.TCCrossReference.Count - 1; iXRef >= 0 && !reqSectionFound; iXRef--)
                {
                    int thisIndex = secondNode.TCCrossReference[iXRef].CrossRefIndex;
                    PlSections2.Add(thisIndex);
                    totalLength2 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = (thisIndex == secondSectionIndex);
                }
                totalLength2 -= (TrackCircuitList[secondSectionIndex].Length - secondPlatform.SData1);
            }
            else
            {
                for (int iXRef = 0; iXRef < secondNode.TCCrossReference.Count && !reqSectionFound; iXRef++)
                {
                    int thisIndex = secondNode.TCCrossReference[iXRef].CrossRefIndex;
                    PlSections2.Add(thisIndex);
                    totalLength2 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = (thisIndex == secondSectionIndex);
                    direction2 = 1;
                }
                totalLength2 -= secondPlatform.SData1; // correct for offset
            }

            // use largest part

            thisDetails.TCSectionIndex.Clear();

            if (totalLength1 > totalLength2)
            {
                foreach (int thisIndex in PlSections1)
                {
                    thisDetails.TCSectionIndex.Add(thisIndex);
                }

                thisDetails.Length = totalLength1;

                if (direction1 == 0)
                {
                    thisDetails.nodeOffset[0] = 0.0f;
                    thisDetails.nodeOffset[1] = firstPlatform.SData1;
                    thisDetails.TCOffset[0, 0] = TrackCircuitList[PlSections1[PlSections1.Count - 1]].Length - totalLength1;
                    for (int iSection = 0; iSection < PlSections1.Count - 2; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] += TrackCircuitList[PlSections1[iSection]].Length;
                    }
                    thisDetails.TCOffset[0, 1] = 0.0f;
                    thisDetails.TCOffset[1, 0] = TrackCircuitList[PlSections1[0]].Length;
                    thisDetails.TCOffset[1, 1] = firstPlatform.SData1;
                    for (int iSection = 0; iSection < PlSections1.Count - 2; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] -= TrackCircuitList[PlSections1[iSection]].Length;
                    }
                }
                else
                {
                    thisDetails.nodeOffset[0] = firstPlatform.SData1;
                    thisDetails.nodeOffset[1] = thisDetails.nodeOffset[0] + totalLength1;
                    thisDetails.TCOffset[0, 0] = 0.0f;
                    thisDetails.TCOffset[0, 1] = TrackCircuitList[PlSections1[0]].Length - totalLength1;
                    for (int iSection = 1; iSection < PlSections1.Count - 1; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] += TrackCircuitList[PlSections1[iSection]].Length;
                    }
                    thisDetails.TCOffset[1, 0] = totalLength1;
                    for (int iSection = 1; iSection < PlSections1.Count - 1; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] -= TrackCircuitList[PlSections1[iSection]].Length;
                    }
                    thisDetails.TCOffset[1, 1] = TrackCircuitList[PlSections1[PlSections1.Count - 1]].Length;
                }
            }
            else
            {
                foreach (int thisIndex in PlSections2)
                {
                    thisDetails.TCSectionIndex.Add(thisIndex);
                }

                thisDetails.Length = totalLength2;

                if (direction2 == 0)
                {
                    thisDetails.nodeOffset[0] = 0.0f;
                    thisDetails.nodeOffset[1] = secondPlatform.SData1;
                    thisDetails.TCOffset[0, 0] = TrackCircuitList[PlSections2.Count - 1].Length - totalLength2;
                    for (int iSection = 0; iSection < PlSections2.Count - 2; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] += TrackCircuitList[PlSections2[iSection]].Length;
                    }
                    thisDetails.TCOffset[0, 1] = 0.0f;
                    thisDetails.TCOffset[1, 0] = TrackCircuitList[PlSections2[0]].Length;
                    thisDetails.TCOffset[1, 1] = secondPlatform.SData1;
                    for (int iSection = 0; iSection < PlSections2.Count - 2; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] -= TrackCircuitList[PlSections2[iSection]].Length;
                    }
                }
                else
                {
                    thisDetails.nodeOffset[0] = secondPlatform.SData1;
                    thisDetails.nodeOffset[1] = thisDetails.nodeOffset[0] + totalLength2;
                    thisDetails.TCOffset[0, 0] = 0.0f;
                    thisDetails.TCOffset[0, 1] = TrackCircuitList[PlSections2[0]].Length - totalLength2;
                    for (int iSection = 1; iSection < PlSections2.Count - 1; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] += TrackCircuitList[PlSections2[iSection]].Length;
                    }
                    thisDetails.TCOffset[1, 0] = totalLength2;
                    for (int iSection = 1; iSection < PlSections2.Count - 1; iSection++)
                    {
                        thisDetails.TCOffset[0, 0] -= TrackCircuitList[PlSections2[iSection]].Length;
                    }
                    thisDetails.TCOffset[1, 1] = TrackCircuitList[PlSections2[PlSections2.Count - 1]].Length;
                }
            }
        }

        //================================================================================================//
        //
        // Find Train
        // Find train in list using number, to restore reference after restore
        //

        public Train FindTrain(int number, List<Train> trains)
        {
            foreach (Train thisTrain in trains)
            {
                if (thisTrain.Number == number)
                {
                    return (thisTrain);
                }
            }

            return (null);
        }

        //================================================================================================//
        //
        // Request set switch
        // Manual request to set switch, either from train or direct from node
        //

        public bool RequestSetSwitch(Train thisTrain, Direction direction)
        {
            if (thisTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            {
                return (thisTrain.ProcessRequestManualSetSwitch(direction));
            }
            else if (thisTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
            {
                return (thisTrain.ProcessRequestExplorerSetSwitch(direction));
            }
            return (false);
        }
        
        public bool RequestSetSwitch(TrackNode switchNode)
        {
            return RequestSetSwitch(switchNode.TCCrossReference[0].CrossRefIndex);
        }

        public bool RequestSetSwitch(int trackCircuitIndex)
        {
            TrackCircuitSection switchSection = TrackCircuitList[trackCircuitIndex];
            Train thisTrain = switchSection.CircuitState.TrainReserved == null ? null : switchSection.CircuitState.TrainReserved.Train;
            bool switchReserved = (switchSection.CircuitState.SignalReserved >= 0 || switchSection.CircuitState.TrainClaimed.Count > 0);
            bool switchSet = false;

            // set physical state

            if (switchReserved)
            {
                switchSet = false;
            }

            else if (!switchSection.CircuitState.HasTrainsOccupying() && thisTrain == null)
            {
                switchSection.JunctionSetManual = switchSection.JunctionLastRoute == 0 ? 1 : 0;
                setSwitch(switchSection.OriginalIndex, switchSection.JunctionSetManual, switchSection);
                switchSet = true;
            }

            // if switch reserved by manual train then notify train

            else if (thisTrain != null && thisTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            {
                switchSection.JunctionSetManual = switchSection.JunctionLastRoute == 0 ? 1 : 0;
                switchSet = thisTrain.ProcessRequestManualSetSwitch(switchSection.Index);
            }
            else if (thisTrain != null && thisTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
            {
                switchSection.JunctionSetManual = switchSection.JunctionLastRoute == 0 ? 1 : 0;
                switchSet = thisTrain.ProcessRequestExplorerSetSwitch(switchSection.Index);
            }

            return (switchSet);
        }

        //only used by MP to manually set a switch to a desired position
        public bool RequestSetSwitch(TrackNode switchNode, int desiredState)
        {
            TrackCircuitSection switchSection = TrackCircuitList[switchNode.TCCrossReference[0].CrossRefIndex];
            Train thisTrain = switchSection.CircuitState.TrainReserved == null ? null : switchSection.CircuitState.TrainReserved.Train;
            bool switchReserved = (switchSection.CircuitState.SignalReserved >= 0 || switchSection.CircuitState.TrainClaimed.Count > 0);
            bool switchSet = false;

            if (trackDB.TrackNodes[switchSection.OriginalIndex].TrJunctionNode.SelectedRoute == desiredState) return (false);
            // set physical state

            if (!MultiPlayer.MPManager.IsServer()) if (switchReserved) return (false);
            //this should not be enforced in MP, as a train may need to be allowed to go out of the station from the side line

            if (!switchSection.CircuitState.HasTrainsOccupying())
            {
                switchSection.JunctionSetManual = desiredState;
                trackDB.TrackNodes[switchSection.OriginalIndex].TrJunctionNode.SelectedRoute = switchSection.JunctionSetManual;
                switchSection.JunctionLastRoute = switchSection.JunctionSetManual;
                switchSet = true;
                /*if (switchSection.SignalsPassingRoutes != null)
                {
                    foreach (var thisSignalIndex in switchSection.SignalsPassingRoutes)
                    {
                        var signal = switchSection.signalRef.SignalObjects[thisSignalIndex];
                        if (signal != null) signal.ResetRoute(switchSection.Index);
                    }
                    switchSection.SignalsPassingRoutes.Clear();
                }*/
                var temptrains = Program.Simulator.Trains.ToArray();

                foreach (var t in temptrains)
                {
                    try
                    {
                        t.ProcessRequestExplorerSetSwitch(switchSection.Index);
                    }
                    catch {}
                }
            }
            return (switchSet);
        }

        //================================================================================================//

    }// class Signals

    //================================================================================================//
    //
    // class TrackCircuitSection
    //
    //================================================================================================//
    //
    // Class for track circuit and train control
    //

    public class TrackCircuitSection
    {
        public enum CIRCUITTYPE
        {
            NORMAL,
            JUNCTION,
            CROSSOVER,
            END_OF_TRACK,
            EMPTY,
        }

        public Signals signalRef;                                 // reference to Signals class //
        public int Index;                                         // section index              //
        public int OriginalIndex;                                 // original TDB section index //
        public CIRCUITTYPE CircuitType;                           // type of section            //

        public TrPin[,] Pins = new TrPin[2, 2];                   // next sections              //
        public TrPin[,] ActivePins = new TrPin[2, 2];             // active next sections       //
        public bool[] EndIsTrailingJunction = new bool[2];        // next section is trailing jn//

        public int JunctionDefaultRoute = -1;                     // jn default route, value is out-pin      //
        public int JunctionLastRoute = -1;                        // jn last route, value is out-pin         //
        public int JunctionSetManual = -1;                        // jn set manual, value is out-pin         //
        public bool AILock = false;                               // jn is locked agains AI trains           //
        public List<int> SignalsPassingRoutes;                    // list of signals reading passed junction //

        public SignalObject[] EndSignals = new SignalObject[2];   // signals at either end      //

        public float Length;                                      // full length                //
        public float[] OffsetLength = new float[2];               // offset length in orig sect //

        public double Overlap;                                    // overlap for junction nodes //
        public List<int> PlatformIndex = new List<int>();         // platforms along section    //

        public TrackCircuitItems CircuitItems;                    // all items                  //
        public TrackCircuitState CircuitState;                    // normal states              //
        public Dictionary<int, List<int>> DeadlockTraps;          // deadlock traps             //
        public List<int> DeadlockActives;                         // list of trains with active deadlock traps //
        public List<int> DeadlockAwaited;                              // train is waiting for deadlock to clear //

        //================================================================================================//
        //
        // Constructor
        //


        public TrackCircuitSection(TrackNode thisNode, int orgINode,
                        TSectionDatFile tsectiondat, Signals thisSignals)
        {

            //
            // Copy general info
            //

            signalRef = thisSignals;

            Index = orgINode;
            OriginalIndex = orgINode;

            if (thisNode.TrEndNode)
            {
                CircuitType = CIRCUITTYPE.END_OF_TRACK;
            }
            else if (thisNode.TrJunctionNode != null)
            {
                CircuitType = CIRCUITTYPE.JUNCTION;
            }
            else
            {
                CircuitType = CIRCUITTYPE.NORMAL;
            }


            //
            // Preset pins, then copy pin info
            //

            for (int direction = 0; direction < 2; direction++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[direction, pin] = new TrPin();
                    Pins[direction, pin].Direction = -1;
                    Pins[direction, pin].Link = -1;
                    ActivePins[direction, pin] = new TrPin();
                    ActivePins[direction, pin].Direction = -1;
                    ActivePins[direction, pin].Link = -1;
                }
            }

            int PinNo = 0;
            for (int pin = 0; pin < Math.Min(thisNode.Inpins, Pins.GetLength(1)); pin++)
            {
                Pins[0, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }
            if (PinNo < thisNode.Inpins) PinNo = (int) thisNode.Inpins;
            for (int pin = 0; pin < Math.Min(thisNode.Outpins, Pins.GetLength(1)); pin++)
            {
                Pins[1, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }


            //
            // preset no end signals
            // preset no trailing junction
            //

            for (int direction = 0; direction < 2; direction++)
            {
                EndSignals[direction] = null;
                EndIsTrailingJunction[direction] = false;
            }

            //
            // Preset length and offset
            // If section index not in tsectiondat, set length to 0.
            //

            float totalLength = 0.0f;

            if (thisNode.TrVectorNode != null && thisNode.TrVectorNode.TrVectorSections != null)
            {
                foreach (TrVectorSection thisSection in thisNode.TrVectorNode.TrVectorSections)
                {
                    float thisLength = 0.0f;

                    if (tsectiondat.TrackSections.ContainsKey(thisSection.SectionIndex))
                    {
                        MSTS.TrackSection TS = tsectiondat.TrackSections[thisSection.SectionIndex];

                        if (TS.SectionCurve != null)
                        {
                            thisLength =
                                    MSTSMath.M.Radians(Math.Abs(TS.SectionCurve.Angle)) *
                                    TS.SectionCurve.Radius;
                        }
                        else
                        {
                            thisLength = TS.SectionSize.Length;

                        }
                    }

                    totalLength += thisLength;
                }
            }

            Length = totalLength;

            for (int direction = 0; direction < 2; direction++)
            {
                OffsetLength[direction] = 0;
            }

            Overlap = 0;

            //
            // set signal list for junctions
            //

            if (CircuitType == CIRCUITTYPE.JUNCTION)
            {
                SignalsPassingRoutes = new List<int>();
            }
            else
            {
                SignalsPassingRoutes = null;
            }

            // for Junction nodes, obtain default route
            // set switch to default route
            // copy overlap (if set)

            if (CircuitType == CIRCUITTYPE.JUNCTION)
            {
                uint trackShapeIndex = thisNode.TrJunctionNode.ShapeIndex;
                try
                {
                    TrackShape trackShape = tsectiondat.TrackShapes[trackShapeIndex];
                    JunctionDefaultRoute = (int)trackShape.MainRoute;

                    Overlap = trackShape.ClearanceDistance;
                }
                catch (Exception)
                {
                    Trace.TraceWarning("Missing TrackShape in tsection.dat : " + trackShapeIndex);
                    JunctionDefaultRoute = 0;
                    Overlap = 0;
                }

                JunctionLastRoute = JunctionDefaultRoute;
                signalRef.setSwitch(OriginalIndex, JunctionLastRoute, this);
            }

            //
            // Create circuit items
            //

            CircuitItems = new TrackCircuitItems();
            CircuitState = new TrackCircuitState();
            DeadlockTraps = new Dictionary<int, List<int>>();
            DeadlockActives = new List<int>();
            DeadlockAwaited = new List<int>();
        }

        //================================================================================================//
        //
        // Constructor for empty entries
        //

        public TrackCircuitSection(int INode, Signals thisSignals)
        {

            signalRef = thisSignals;

            Index = INode;
            OriginalIndex = -1;
            CircuitType = CIRCUITTYPE.EMPTY;

            for (int iDir = 0; iDir < 2; iDir++)
            {
                EndIsTrailingJunction[iDir] = false;
                EndSignals[iDir] = null;
                OffsetLength[iDir] = 0;
            }

            for (int iDir = 0; iDir < 2; iDir++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[iDir, pin] = new TrPin();
                    Pins[iDir, pin].Direction = -1;
                    Pins[iDir, pin].Link = -1;
                    ActivePins[iDir, pin] = new TrPin();
                    ActivePins[iDir, pin].Direction = -1;
                    ActivePins[iDir, pin].Link = -1;
                }
            }

            Length = 0;
            Overlap = 0;

            CircuitItems = new TrackCircuitItems();
            CircuitState = new TrackCircuitState();
            DeadlockTraps = new Dictionary<int, List<int>>();
            DeadlockActives = new List<int>();
            DeadlockAwaited = new List<int>();

            SignalsPassingRoutes = null;
        }

        //================================================================================================//
        //
        // Restore
        //

        public void Restore(BinaryReader inf)
        {
            ActivePins[0, 0].Link = inf.ReadInt32();
            ActivePins[0, 0].Direction = inf.ReadInt32();
            ActivePins[1, 0].Link = inf.ReadInt32();
            ActivePins[1, 0].Direction = inf.ReadInt32();
            ActivePins[0, 1].Link = inf.ReadInt32();
            ActivePins[0, 1].Direction = inf.ReadInt32();
            ActivePins[1, 1].Link = inf.ReadInt32();
            ActivePins[1, 1].Direction = inf.ReadInt32();

            JunctionSetManual = inf.ReadInt32();
            JunctionLastRoute = inf.ReadInt32();
            AILock = inf.ReadBoolean();

            CircuitState.Restore(inf);

            // if physical junction, throw switch

            if (CircuitType == CIRCUITTYPE.JUNCTION)
            {
                signalRef.setSwitch(OriginalIndex, JunctionLastRoute, this);
            }

            int deadlockTrapsCount = inf.ReadInt32();
            for (int iDeadlock = 0; iDeadlock < deadlockTrapsCount; iDeadlock++)
            {
                int deadlockKey = inf.ReadInt32();
                int deadlockListCount = inf.ReadInt32();
                List<int> deadlockList = new List<int>();

                for (int iDeadlockInfo = 0; iDeadlockInfo < deadlockListCount; iDeadlockInfo++)
                {
                    int deadlockDetail = inf.ReadInt32();
                    deadlockList.Add(deadlockDetail);
                }
                DeadlockTraps.Add(deadlockKey, deadlockList);
            }

            int deadlockActivesCount = inf.ReadInt32();
            for (int iDeadlockActive = 0; iDeadlockActive < deadlockActivesCount; iDeadlockActive++)
            {
                int deadlockActiveDetails = inf.ReadInt32();
                DeadlockActives.Add(deadlockActiveDetails);
            }

            int deadlockWaitCount = inf.ReadInt32();
            for (int iDeadlockWait = 0; iDeadlockWait < deadlockWaitCount; iDeadlockWait++)
            {
                int deadlockWaitDetails = inf.ReadInt32();
                DeadlockAwaited.Add(deadlockWaitDetails);
            }

        }

        //================================================================================================//
        //
        // Save
        //

        public void Save(BinaryWriter outf)
        {
            outf.Write(ActivePins[0, 0].Link);
            outf.Write(ActivePins[0, 0].Direction);
            outf.Write(ActivePins[1, 0].Link);
            outf.Write(ActivePins[1, 0].Direction);
            outf.Write(ActivePins[0, 1].Link);
            outf.Write(ActivePins[0, 1].Direction);
            outf.Write(ActivePins[1, 1].Link);
            outf.Write(ActivePins[1, 1].Direction);

            outf.Write(JunctionSetManual);
            outf.Write(JunctionLastRoute);
            outf.Write(AILock);

            CircuitState.Save(outf);

            outf.Write(DeadlockTraps.Count);
            foreach (KeyValuePair<int, List<int>> thisTrap in DeadlockTraps)
            {
                outf.Write(thisTrap.Key);
                outf.Write(thisTrap.Value.Count);

                foreach (int thisDeadlockRef in thisTrap.Value)
                {
                    outf.Write(thisDeadlockRef);
                }
            }

            outf.Write(DeadlockActives.Count);
            foreach (int thisDeadlockActive in DeadlockActives)
            {
                outf.Write(thisDeadlockActive);
            }

            outf.Write(DeadlockAwaited.Count);
            foreach (int thisDeadlockWait in DeadlockAwaited)
            {
                outf.Write(thisDeadlockWait);
            }
        }

        //================================================================================================//
        //
        // Copy basic info only
        //

        public TrackCircuitSection CopyBasic(int INode)
        {
            TrackCircuitSection newSection = new TrackCircuitSection(INode, this.signalRef);

            newSection.OriginalIndex = this.OriginalIndex;
            newSection.CircuitType = this.CircuitType;

            newSection.EndSignals[0] = this.EndSignals[0];
            newSection.EndSignals[1] = this.EndSignals[1];

            newSection.Length = this.Length;

            Array.Copy(this.OffsetLength, newSection.OffsetLength, this.OffsetLength.Length);

            return (newSection);
        }

        //================================================================================================//
        //
        // Check if set for train
        //

        public bool IsSet(Train.TrainRouted thisTrain)   // using routed train
        {

            // if train in this section, return true; if other train in this section, return false

            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                return (true);
            }

            // check reservation
            
            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                return (true);
            }

            // check claim

            if (CircuitState.TrainClaimed.Count > 0)
            {
                return (CircuitState.TrainClaimed.PeekTrain() == thisTrain.Train);
            }

            // section is not yet set for this train

            return (false);
        }

        public bool IsSet(Train thisTrain)    // using unrouted train
        {
            if (IsSet(thisTrain.routedForward))
            {
                return (true);
            }
            else
            {
                return (IsSet(thisTrain.routedBackward));
            }
        }

        //================================================================================================//
        //
        // Check available state for train
        //

        public bool IsAvailable(Train.TrainRouted thisTrain)    // using routed train
        {

            // if train in this section, return true; if other train in this section, return false

            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                return (true);
            }
            if (CircuitState.HasOtherTrainsOccupying(thisTrain))
            {
                return (false);
            }

            // check reservation

            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                return (true);
            }

            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train != thisTrain.Train)
            {
                return (false);
            }

            // check signal reservation

            if (CircuitState.SignalReserved >= 0)
            {
                return (false);
            }

            // check claim

            if (CircuitState.TrainClaimed.Count > 0)
            {
                return (CircuitState.TrainClaimed.PeekTrain() == thisTrain.Train);
            }

            // check deadlock trap

            if (DeadlockTraps.ContainsKey(thisTrain.Train.Number))
            {
                if (!DeadlockAwaited.Contains(thisTrain.Train.Number))
                    DeadlockAwaited.Add(thisTrain.Train.Number); // train is waiting for deadlock to clear
                return (false);
            }

            // check deadlock is in use - only if train has valid route

            if (thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex] != null)
            {
                int routeElementIndex = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex].GetRouteIndex(Index, 0);
                if (routeElementIndex >= 0)
                {
                    Train.TCRouteElement thisElement = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][routeElementIndex];
                    if (thisElement.StartAlternativePath != null)
                    {
                        TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];
                        if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                        {
                            return (false);
                        }
                    }
                }
            }

            // section is clear

            return (true);
        }

        public bool IsAvailable(Train thisTrain)    // using unrouted train
        {
            if (IsAvailable(thisTrain.routedForward))
            {
                return (true);
            }
            else
            {
                return (IsAvailable(thisTrain.routedBackward));
            }
        }

        //================================================================================================//
        //
        // Reserve : set reserve state
        //

        public void Reserve(Train.TrainRouted thisTrain, Train.TCSubpathRoute thisRoute)
        {

#if DEBUG_REPORTS
            String report = "Reserve section ";
            report = String.Concat(report, this.Index.ToString());
            report = String.Concat(report, " for train ", thisTrain.Train.Number.ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Reserve section ";
                reportCT = String.Concat(reportCT, this.Index.ToString());
                reportCT = String.Concat(reportCT, " for train ", thisTrain.Train.Number.ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            Train.TCRouteElement thisElement;

            if (!CircuitState.ThisTrainOccupying(thisTrain.Train))
            {
                //if (!MultiPlayer.MPManager.IsMultiPlayer())
                    CircuitState.TrainReserved = thisTrain;
            }

            // remove from claim or deadlock claim

            if (CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {
                CircuitState.TrainClaimed = removeFromQueue(CircuitState.TrainClaimed, thisTrain);
            }

            // get element in routepath to find required alignment

            int thisIndex = -1;

            for (int iElement = 0; iElement < thisRoute.Count && thisIndex < 0; iElement++)
            {
                thisElement = thisRoute[iElement];
                if (thisElement.TCSectionIndex == Index)
                {
                    thisIndex = iElement;
                }
            }

            // if junction or crossover, align pins
            // also reset manual set (path will have followed setting)

            if (CircuitType == CIRCUITTYPE.JUNCTION || CircuitType == CIRCUITTYPE.CROSSOVER)
            {
                // set active pins for leading section

                JunctionSetManual = -1;  // reset manual setting (will have been honoured in route definition if applicable)

                int leadSectionIndex = -1;
                if (thisIndex > 0)
                {
                    thisElement = thisRoute[thisIndex - 1];
                    leadSectionIndex = thisElement.TCSectionIndex;

                    alignSwitchPins(leadSectionIndex);
                }

                // set active pins for trailing section

                int trailSectionIndex = -1;
                if (thisIndex <= thisRoute.Count - 2)
                {
                    thisElement = thisRoute[thisIndex + 1];
                    trailSectionIndex = thisElement.TCSectionIndex;

                    alignSwitchPins(trailSectionIndex);
                }

                // reset signals which routed through this junction

                foreach (int thisSignalIndex in SignalsPassingRoutes)
                {
                    SignalObject thisSignal = signalRef.SignalObjects[thisSignalIndex];
                    thisSignal.ResetRoute(Index);
                }
                SignalsPassingRoutes.Clear();
            }

            // enable all signals along section in direction of train
            // do not enable those signals who are part of NORMAL signal

            if (thisIndex < 0) return; //Added by JTang
            thisElement = thisRoute[thisIndex];
            int direction = thisElement.Direction;

            for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
            {
                TrackCircuitSignalList thisSignalList = CircuitItems.TrackCircuitSignals[direction, fntype];
                foreach (TrackCircuitSignalItem thisItem in thisSignalList.TrackCircuitItem)
                {
                    SignalObject thisSignal = thisItem.SignalRef;
                    if (!thisSignal.isSignalNormal())
                    {
                        thisSignal.enabledTrain = thisTrain;
                    }
                }
            }

            // set deadlock trap if required

            if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
            {
                SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
            }

            // if start of alternative route, set deadlock keys for other end

            if (thisElement != null && thisElement.StartAlternativePath != null)
            {
                TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];
                // no deadlock yet active
                if (thisTrain.Train.DeadlockInfo.ContainsKey(endSection.Index))
                {
                    endSection.SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[endSection.Index]);
                }
                else if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.DeadlockAwaited.Contains(thisTrain.Train.Number))
                {
                    endSection.DeadlockAwaited.Add(thisTrain.Train.Number);
                }
            }
        }

        //================================================================================================//
        //
        // insert Claim
        //

        public void Claim(Train.TrainRouted thisTrain)
        {
            if (!CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {
                CircuitState.TrainClaimed.Enqueue(thisTrain);
            }

            // set deadlock trap if required

            if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
            {
                SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
            }
        }

        //================================================================================================//
        //
        // insert pre-reserve
        //

        public void PreReserve(Train.TrainRouted thisTrain)
        {
            if (!CircuitState.TrainPreReserved.ContainsTrain(thisTrain))
            {
                CircuitState.TrainPreReserved.Enqueue(thisTrain);
            }
        }

        //================================================================================================//
        //
        // set track occupied
        //

        public void SetOccupied(Train.TrainRouted thisTrain)
        {

#if DEBUG_REPORTS
            String report = "Occupy section ";
            report = String.Concat(report, this.Index.ToString());
            report = String.Concat(report, " for train ", thisTrain.Train.Number.ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Occupy section ";
                reportCT = String.Concat(reportCT, this.Index.ToString());
                reportCT = String.Concat(reportCT, " for train ", thisTrain.Train.Number.ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            int direction = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCDirection;
            CircuitState.TrainOccupy.Add(thisTrain, direction);
            thisTrain.Train.OccupiedTrack.Add(this);

            // clear all reservations
            CircuitState.TrainReserved = null;
            CircuitState.SignalReserved = -1;

            if (CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {
                CircuitState.TrainClaimed = removeFromQueue(CircuitState.TrainClaimed, thisTrain);
            }

            if (CircuitState.TrainPreReserved.ContainsTrain(thisTrain))
            {
                CircuitState.TrainPreReserved = removeFromQueue(CircuitState.TrainPreReserved, thisTrain);
            }

            // add to clear list of train

            float distanceToClear = thisTrain.Train.DistanceTravelledM + Length + thisTrain.Train.standardOverlapM;

            if (CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
            {
                if (Overlap > 0)
                {
                    distanceToClear = thisTrain.Train.DistanceTravelledM + Length + Convert.ToSingle(Overlap) + thisTrain.Train.standardOverlapM;
                }
                else
                {
                    distanceToClear = thisTrain.Train.DistanceTravelledM + Length + thisTrain.Train.junctionOverlapM;
                }
            }

            Train.TCPosition presentFront = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];
            int reverseDirectionIndex = thisTrain.TrainRouteDirectionIndex == 0 ? 1 : 0;
            Train.TCPosition presentRear = thisTrain.Train.PresentPosition[reverseDirectionIndex];

            // correct offset if position direction is not equal to route direction
            float frontOffset = presentFront.TCOffset;
            if (presentFront.RouteListIndex >= 0 &&
                presentFront.TCDirection != thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][presentFront.RouteListIndex].Direction)
                frontOffset = Length - frontOffset;

            float rearOffset = presentRear.TCOffset;
            if (presentRear.RouteListIndex >= 0 &&
                presentRear.TCDirection != thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][presentRear.RouteListIndex].Direction)
                rearOffset = Length - rearOffset;

            if (presentFront.TCSectionIndex == Index)
            {
                distanceToClear += thisTrain.Train.Length - frontOffset;
            }
            else if (presentRear.TCSectionIndex == Index)
            {
                distanceToClear -= rearOffset;
            }
            else
            {
                distanceToClear += thisTrain.Train.Length;
            }
            thisTrain.Train.requiredActions.InsertAction(new Train.ClearSectionItem(distanceToClear, Index));

            // set deadlock trap if required

            if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
            {
                SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
            }

            // check for deadlock trap if taking alternative path

            if (thisTrain.Train.TCRoute != null && thisTrain.Train.TCRoute.activeAltpath >= 0)
            {
                Train.TCSubpathRoute altRoute = thisTrain.Train.TCRoute.TCAlternativePaths[thisTrain.Train.TCRoute.activeAltpath];
                Train.TCRouteElement startElement = altRoute[0];
                if (Index == startElement.TCSectionIndex)
                {
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[altRoute[altRoute.Count - 1].TCSectionIndex];

                    // set deadlock trap for next section

                    if (thisTrain.Train.DeadlockInfo.ContainsKey(endSection.Index))
                    {
                        endSection.SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[endSection.Index]);
                    }
                }
            }
        }

        //================================================================================================//
        //
        // clear track occupied
        //

        // routed train
        public void ClearOccupied(Train.TrainRouted thisTrain, bool resetEndSignal)
        {

#if DEBUG_REPORTS
            String report = "Clear section ";
            report = String.Concat(report, this.Index.ToString());
            report = String.Concat(report, " for train ", thisTrain.Train.Number.ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Clear section ";
                reportCT = String.Concat(reportCT, this.Index.ToString());
                reportCT = String.Concat(reportCT, " for train ", thisTrain.Train.Number.ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            if (CircuitState.TrainOccupy.ContainsTrain(thisTrain))
            {
                CircuitState.TrainOccupy.RemoveTrain(thisTrain);
                thisTrain.Train.OccupiedTrack.Remove(this);
            }

            RemoveTrain(thisTrain, false);   // clear occupy first to prevent loop, next clear all hanging references

            // if signal at either end is still enabled for this train, reset the signal

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                if (EndSignals[iDirection] != null)
                {
                    SignalObject endSignal = EndSignals[iDirection];
                    if (endSignal.enabledTrain == thisTrain && resetEndSignal)
                    {
                        endSignal.resetSignalEnabled();
                    }
                }

                // disable all signals along section if enabled for this train

                for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                {
                    TrackCircuitSignalList thisSignalList = CircuitItems.TrackCircuitSignals[iDirection, fntype];
                    foreach (TrackCircuitSignalItem thisItem in thisSignalList.TrackCircuitItem)
                    {
                        SignalObject thisSignal = thisItem.SignalRef;
                        if (thisSignal.enabledTrain == thisTrain)
                        {
                            thisSignal.resetSignalEnabled();
                        }
                    }
                }
            }

            // if section is Junction or Crossover, reset active pins

            if (CircuitType == CIRCUITTYPE.JUNCTION || CircuitType == CIRCUITTYPE.CROSSOVER)
            {
                deAlignSwitchPins();

                // reset signals which routed through this junction

                foreach (int thisSignalIndex in SignalsPassingRoutes)
                {
                    SignalObject thisSignal = signalRef.SignalObjects[thisSignalIndex];
                    thisSignal.ResetRoute(Index);
                }
                SignalsPassingRoutes.Clear();
            }

            // reset manual junction setting if train is in manual mode

            if (thisTrain.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL && CircuitType == CIRCUITTYPE.JUNCTION && JunctionSetManual >= 0)
            {
                JunctionSetManual = -1;
            }

            // if no longer occupied and pre-reserved not empty, promote first entry of prereserved

            if (CircuitState.TrainOccupy.Count <= 0 && CircuitState.TrainPreReserved.Count > 0)
            {
                Train.TrainRouted nextTrain = CircuitState.TrainPreReserved.Dequeue();
                Train.TCSubpathRoute RoutePart = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex];

                Reserve(nextTrain, RoutePart);
            }

        }

        // unrouted train
        public void ClearOccupied(Train thisTrain, bool resetEndSignal)
        {
            ClearOccupied(thisTrain.routedForward, resetEndSignal); // forward
            ClearOccupied(thisTrain.routedBackward, resetEndSignal);// backward
        }

        // only reset occupied state - use in case of reversal or mode change when train has not actually moved
        // routed train
        public void ResetOccupied(Train.TrainRouted thisTrain)
        {

            if (CircuitState.TrainOccupy.ContainsTrain(thisTrain))
            {
                CircuitState.TrainOccupy.RemoveTrain(thisTrain);
                thisTrain.Train.OccupiedTrack.Remove(this);
            }

        }

        // unrouted train
        public void ResetOccupied(Train thisTrain)
        {
            ResetOccupied(thisTrain.routedForward); // forward
            ResetOccupied(thisTrain.routedBackward);// backward
        }

        //================================================================================================//
        //
        // Remove train from section
        //

        // routed train
        public void RemoveTrain(Train.TrainRouted thisTrain, bool resetEndSignal)
        {
#if DEBUG_REPORTS
            String report = "Remove train from section ";
            report = String.Concat(report, this.Index.ToString());
            report = String.Concat(report, " for train ", thisTrain.Train.Number.ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Remove train from section ";
                reportCT = String.Concat(reportCT, this.Index.ToString());
                reportCT = String.Concat(reportCT, " for train ", thisTrain.Train.Number.ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                ClearOccupied(thisTrain, resetEndSignal);
            }

            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                CircuitState.TrainReserved = null;
                ClearOccupied(thisTrain, resetEndSignal);    // call clear occupy to reset signals and switches //
            }

            if (CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {
                CircuitState.TrainClaimed = removeFromQueue(CircuitState.TrainClaimed, thisTrain);
            }

            if (CircuitState.TrainPreReserved.ContainsTrain(thisTrain))
            {
                CircuitState.TrainPreReserved = removeFromQueue(CircuitState.TrainPreReserved, thisTrain);
            }

            ClearDeadlockTrap(thisTrain.Train.Number);
        }


        // unrouted train
        public void RemoveTrain(Train thisTrain, bool resetEndSignal)
        {
            RemoveTrain(thisTrain.routedForward, resetEndSignal);
            RemoveTrain(thisTrain.routedBackward, resetEndSignal);
        }

        //================================================================================================//
        //
        // Remove train reservations from section
        //

        public void UnreserveTrain(Train.TrainRouted thisTrain, bool resetEndSignal)
        {
            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                CircuitState.TrainReserved = null;
                ClearOccupied(thisTrain, resetEndSignal);    // call clear occupy to reset signals and switches //
            }

            if (CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {
                CircuitState.TrainClaimed = removeFromQueue(CircuitState.TrainClaimed, thisTrain);
            }

            if (CircuitState.TrainPreReserved.ContainsTrain(thisTrain))
            {
                CircuitState.TrainPreReserved = removeFromQueue(CircuitState.TrainPreReserved, thisTrain);
            }

            ClearDeadlockTrap(thisTrain.Train.Number);
        }

        //================================================================================================//
        //
        // Remove specified train from queue
        //

        private TrainQueue removeFromQueue(TrainQueue thisQueue, Train.TrainRouted thisTrain)
        {
            List<Train.TrainRouted> tempList = new List<Train.TrainRouted>();
            TrainQueue newQueue = new TrainQueue();

            // extract trains from queue and store in list - this will revert the order!
            // do not store train which is to be removed

            int queueCount = thisQueue.Count;
            while (queueCount > 0)
            {
                Train.TrainRouted queueTrain = thisQueue.Dequeue();
                if (thisTrain == null || queueTrain.Train != thisTrain.Train)
                {
                    tempList.Add(queueTrain);
                }
                queueCount = thisQueue.Count;
            }

            // restore the order by requeing

            foreach (Train.TrainRouted queueTrain in tempList)
            {
                newQueue.Enqueue(queueTrain);
            }

            return (newQueue);
        }

        //================================================================================================//
        //
        // align pins switch or crossover
        //

        public void alignSwitchPins(int linkedSectionIndex)
        {
            if (MultiPlayer.MPManager.NoAutoSwitch()) return;
            int alignDirection = -1;  // pin direction for leading section
            int alignLink = -1;       // link index for leading section

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int iLink = 0; iLink <= 1; iLink++)
                {
                    if (Pins[iDirection, iLink].Link == linkedSectionIndex)
                    {
                        alignDirection = iDirection;
                        alignLink = iLink;
                    }
                }
            }

            if (alignDirection >= 0)
            {
                ActivePins[alignDirection, 0].Link = -1;
                ActivePins[alignDirection, 1].Link = -1;

                ActivePins[alignDirection, alignLink].Link =
                        Pins[alignDirection, alignLink].Link;
                ActivePins[alignDirection, alignLink].Direction =
                        Pins[alignDirection, alignLink].Direction;

                TrackCircuitSection linkedSection = signalRef.TrackCircuitList[linkedSectionIndex];
                for (int iDirection = 0; iDirection <= 1; iDirection++)
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        if (linkedSection.Pins[iDirection, iLink].Link == Index)
                        {
                            linkedSection.ActivePins[iDirection, iLink].Link = Index;
                            linkedSection.ActivePins[iDirection, iLink].Direction =
                                    linkedSection.Pins[iDirection, iLink].Direction;
                        }
                    }
                }
            }

            // if junction, align physical switch

            if (CircuitType == CIRCUITTYPE.JUNCTION)
            {
                int switchPos = -1;
                if (ActivePins[1, 0].Link != -1)
                    switchPos = 0;
                if (ActivePins[1, 1].Link != -1)
                    switchPos = 1;

                if (switchPos >= 0)
                {
                    signalRef.setSwitch(OriginalIndex, switchPos, this);
                }
            }
        }

        //================================================================================================//
        //
        // de-align active switch pins
        //

        public void deAlignSwitchPins()
        {
            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                if (Pins[iDirection, 1].Link > 0)     // active switchable end
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        int activeLink = Pins[iDirection, iLink].Link;
                        int activeDirection = Pins[iDirection, iLink].Direction == 0 ? 1 : 0;
                        ActivePins[iDirection, iLink].Link = -1;

                        TrackCircuitSection linkSection = signalRef.TrackCircuitList[activeLink];
                        linkSection.ActivePins[activeDirection, 0].Link = -1;
                    }
                }
            }
        }


        //================================================================================================//
        //
        // Get state of single section
        //

        public SignalObject.INTERNAL_BLOCKSTATE getSectionState(Train.TrainRouted thisTrain, int direction,
                        SignalObject.INTERNAL_BLOCKSTATE passedBlockstate, Train.TCSubpathRoute thisRoute)
        {
            SignalObject.INTERNAL_BLOCKSTATE thisBlockstate;
            SignalObject.INTERNAL_BLOCKSTATE localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVABLE;  // default value
            bool stateSet = false;

            TrackCircuitState thisState = CircuitState;

            bool checkTrailingJunction = false;

            // track occupied - check speed and direction - only for normal sections

            if (thisTrain != null && thisState.TrainOccupy.ContainsTrain(thisTrain))
            {
                localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED;  // occupied by own train counts as reserved
                stateSet = true;
            }
            else if (thisState.HasTrainsOccupying(direction, true))
            {
                {
                    localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR;
                    stateSet = true;
                }
            }
            else
            {
                int reqDirection = direction == 0 ? 1 : 0;
                if (thisState.HasTrainsOccupying(reqDirection, false))
                {
                    localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.OCCUPIED_OPPDIR;
                    stateSet = true;
                }
            }

            // for junctions or cross-overs, check route selection

            if (CircuitType == CIRCUITTYPE.JUNCTION || CircuitType == CIRCUITTYPE.CROSSOVER)
            {
                if (thisState.HasTrainsOccupying())    // there is a train on the switch
                {
                    if (thisRoute == null)  // no route from signal - always report switch blocked
                    {
                        localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.BLOCKED;
                        stateSet = true;
                    }
                    else
                    {
                        int reqPinIndex = -1;
                        for (int iPinIndex = 0; iPinIndex <= 1 && reqPinIndex < 0; iPinIndex++)
                        {
                            if (Pins[iPinIndex, 1].Link > 0)
                                reqPinIndex = iPinIndex;  // switchable end
                        }

                        int switchEnd = -1;
                        for (int iSwitch = 0; iSwitch <= 1; iSwitch++)
                        {
                            int nextSectionIndex = Pins[reqPinIndex, iSwitch].Link;
                            int routeListIndex = thisRoute == null ? -1 : thisRoute.GetRouteIndex(nextSectionIndex, 0);
                            if (routeListIndex >= 0)
                                switchEnd = iSwitch;  // required exit
                        }
                        if (switchEnd < 0 || ActivePins[reqPinIndex, switchEnd].Link < 0) // no free exit available or switch misaligned
                        {
                            localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.BLOCKED;
                            stateSet = true;
                        }
                    }
                }
            }

            // track reserved - check direction

            if (thisState.TrainReserved != null && thisTrain != null && !stateSet)
            {
                Train.TrainRouted reservedTrain = thisState.TrainReserved;
                if (reservedTrain.Train == thisTrain.Train)
                {
                    localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED;
                    stateSet = true;
                }
                else
                {
                    if (MultiPlayer.MPManager.IsMultiPlayer())
                    {
                        var reservedTrainStillThere = false;
                        foreach (var s in this.EndSignals)
                        {
                            if (s != null && s.enabledTrain != null && s.enabledTrain.Train == reservedTrain.Train) reservedTrainStillThere = true;
                        }

                        if (reservedTrainStillThere == true && reservedTrain.Train.GetDistanceToTrain(this.Index, 0.0f) > 0)
                            localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED_OTHER;
                        else
                        {
                            //if (reservedTrain.Train.RearTDBTraveller.DistanceTo(this.
                            thisState.TrainReserved = thisTrain;
                            localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED;
                        }
                    }
                    else
                    localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED_OTHER;

                    // if end is trailing junction, set to check junction

                    checkTrailingJunction = EndIsTrailingJunction[direction];
                }
            }

            // signal reserved

            if (thisState.SignalReserved >= 0)
            {
                localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.RESERVED_OTHER;
                stateSet = true;
            }

            // track claimed

            if (!stateSet && thisTrain != null && thisState.TrainClaimed.Count > 0 && thisState.TrainClaimed.PeekTrain() != thisTrain.Train)
            {
                localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.OPEN;
            }

            // deadlock trap

            if (thisTrain != null && DeadlockTraps.ContainsKey(thisTrain.Train.Number))
            {
                localBlockstate = SignalObject.INTERNAL_BLOCKSTATE.BLOCKED;
                if (!DeadlockAwaited.Contains(thisTrain.Train.Number))
                    DeadlockAwaited.Add(thisTrain.Train.Number);
            }

            thisBlockstate = localBlockstate > passedBlockstate ? localBlockstate : passedBlockstate;
            return (thisBlockstate);
        }


        //================================================================================================//
        //
        // Test if train ahead and calculate distance to that train (front or rear depending on direction)
        //

        public Dictionary<Train, float> TestTrainAhead(Train thisTrain, float offset, int direction)
        {
            Train trainFound = null;
            float distanceTrainAheadM = Length + 1.0f; // ensure train is always within section

            List<Train.TrainRouted> trainsInSection = CircuitState.TrainsOccupying();

            // remove own train
            if (thisTrain != null)
            {
                for (int iindex = trainsInSection.Count - 1; iindex >= 0; iindex--)
                {
                    if (trainsInSection[iindex].Train == thisTrain)
                        trainsInSection.RemoveAt(iindex);
                }
            }

            // search for trains in section
            foreach (Train.TrainRouted nextTrain in trainsInSection)
            {
                int routeIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(Index, 0);
                if (routeIndex >= 0)
                {
                    Train.TCPosition nextFront = nextTrain.Train.PresentPosition[nextTrain.TrainRouteDirectionIndex];
                    int reverseDirection = nextTrain.TrainRouteDirectionIndex == 0 ? 1 : 0;
                    Train.TCPosition nextRear = nextTrain.Train.PresentPosition[reverseDirection];

                    Train.TCRouteElement thisElement = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex][routeIndex];
                    if (thisElement.Direction == direction) // same direction, so if the train is in front we're looking at the rear of the train
                    {
                        if (nextRear.TCSectionIndex == Index) // rear of train is in same section
                        {
                            float thisTrainDistanceM = nextRear.TCOffset;

                            if (thisTrainDistanceM < distanceTrainAheadM && nextRear.TCOffset >= offset) // train is nearest train and in front
                            {
                                distanceTrainAheadM = thisTrainDistanceM;
                                trainFound = nextTrain.Train;
                            }
                            else if (nextRear.TCOffset < offset && nextRear.TCOffset + nextTrain.Train.Length > offset) // our end is in the middle of the train
                            {
                                distanceTrainAheadM = 0; // set distance to 0
                                trainFound = nextTrain.Train;
                            }
                        }
                        else
                        {
                            int nextRouteFrontIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextFront.TCSectionIndex, 0);
                            int nextRouteRearIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextRear.TCSectionIndex, 0);

                            if (nextRouteRearIndex < routeIndex)
                            {
                                if (nextRouteFrontIndex > routeIndex) // train spans section, so position of train in section is 0 //
                                {
                                    distanceTrainAheadM = 0.0f;
                                    trainFound = nextTrain.Train;
                                } // otherwise train is not in front, so don't use it
                            }
                            else  // if index is greater, train has moved on - return section length minus offset
                            {
                                distanceTrainAheadM = Length - offset;
                                trainFound = nextTrain.Train;
                            }
                        }
                    }
                    else // reverse direction, so we're looking at the front - use section length - offset as position
                    {
                        float thisTrainOffset = Length - nextFront.TCOffset;
                        if (nextFront.TCSectionIndex == Index)  // front of train in section
                        {
                            float thisTrainDistanceM = thisTrainOffset;

                            if (thisTrainDistanceM < distanceTrainAheadM && thisTrainOffset >= offset) // train is nearest train and in front
                            {
                                distanceTrainAheadM = thisTrainDistanceM;
                                trainFound = nextTrain.Train;
                            }
                        }
                        else
                        {
                            int nextRouteFrontIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextFront.TCSectionIndex, 0);
                            int nextRouteRearIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextRear.TCSectionIndex, 0);

                            if (nextRouteFrontIndex < routeIndex)
                            {
                                if (nextRouteRearIndex > routeIndex)  // train spans section so offset in section is 0//
                                {
                                    distanceTrainAheadM = 0;
                                    trainFound = nextTrain.Train;
                                } // else train is not in front of us
                            }
                            else  // if index is greater, train has moved on - return section length minus offset
                            {
                                distanceTrainAheadM = Length - offset;
                                trainFound = nextTrain.Train;
                            }
                        }

                    }
                }
                else
                {
                    distanceTrainAheadM = 0; // train is off its route - assume full section occupied //
                    trainFound = nextTrain.Train;
                }
            }

            Dictionary<Train, float> result = new Dictionary<Train, float>();
            if (trainFound != null)
                result.Add(trainFound, (distanceTrainAheadM - offset));
            return (result);
        }

        //================================================================================================//
        //
        // Get next active link
        //

        public TrPin GetNextActiveLink(int direction, int lastIndex)
        {

            // Crossover

            if (CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
            {
                int inPinIndex = direction == 0 ? 1 : 0;
                if (Pins[inPinIndex, 0].Link == lastIndex)
                {
                    return (ActivePins[direction, 0]);
                }
                else if (Pins[inPinIndex, 1].Link == lastIndex)
                {
                    return (ActivePins[direction, 1]);
                }
                else
                {
                    TrPin dummyPin = new TrPin();
                    dummyPin.Direction = -1;
                    dummyPin.Link = -1;
                    return (dummyPin);
                }
            }

            // All other sections

            if (ActivePins[direction, 0].Link > 0)
            {
                return (ActivePins[direction, 0]);
            }

            return (ActivePins[direction, 1]);
        }

        //================================================================================================//
        //
        // Get distance between objects
        //

        public float GetDistanceBetweenObjects(int startSectionIndex, float startOffset, int startDirection,
            int endSectionIndex, float endOffset)
        {
            int thisSectionIndex = startSectionIndex;
            int direction = startDirection;

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            float distanceM = 0.0f;
            int lastIndex = -2;  // set to non-occuring value

            while (thisSectionIndex != endSectionIndex && thisSectionIndex > 0)
            {
                distanceM += thisSection.Length;
                TrPin nextLink = thisSection.GetNextActiveLink(direction, lastIndex);

                lastIndex = thisSectionIndex;
                thisSectionIndex = nextLink.Link;
                direction = nextLink.Direction;

                if (thisSectionIndex > 0)
                    thisSection = signalRef.TrackCircuitList[thisSectionIndex];
            }

            // use found distance, correct for begin and end offset

            if (thisSectionIndex == endSectionIndex)
            {
                distanceM += endOffset - startOffset;
                return (distanceM);
            }

            return (-1.0f);
        }

        //================================================================================================//
        //
        // Check if train can be placed in section
        //

        public bool CanPlaceTrain(Train thisTrain, float offset, float trainLength)
        {

            if (!IsAvailable(thisTrain))
            {
                if (CircuitState.TrainReserved != null ||
                CircuitState.TrainClaimed.Count > 0)
                {
                    return (false);
                }

                if (DeadlockTraps.ContainsKey(thisTrain.Number))
                {
                    return (false);  // prevent deadlock
                }

                if (CircuitType != CIRCUITTYPE.NORMAL) // other than normal and not clear - return false
                {
                    return (false);
                }

                if (offset == 0 && trainLength > Length) // train spans section
                {
                    return (false);
                }

                // get other trains in section

                Dictionary<Train, float> trainInfo = new Dictionary<Train, float>();
                float offsetFromStart = offset;

                // test train ahead of rear end (for non-placed trains, always use direction 0)

                if (thisTrain.PresentPosition[1].TCSectionIndex == Index)
                {
                    trainInfo = TestTrainAhead(thisTrain,
                            offsetFromStart, thisTrain.PresentPosition[1].TCDirection); // rear end in this section, use offset
                }
                else
                {
                    offsetFromStart = 0.0f;
                    trainInfo = TestTrainAhead(thisTrain,
                            0.0f, thisTrain.PresentPosition[1].TCDirection); // test from start
                }

                if (trainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo)
                    {
                        if (trainAhead.Value < trainLength) // train ahead not clear
                        {
                            return (false);
                        }
                    }
                }

                // test train behind of front end

                int revDirection = thisTrain.PresentPosition[0].TCDirection == 0 ? 1 : 0;
                if (thisTrain.PresentPosition[0].TCSectionIndex == Index)
                {
                    float offsetFromEnd = Length - (trainLength + offsetFromStart);
                    trainInfo = TestTrainAhead(thisTrain, offsetFromEnd, revDirection); // test remaining length
                }
                else
                {
                    trainInfo = TestTrainAhead(thisTrain, 0.0f, revDirection); // test full section
                }

                if (trainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo)
                    {
                        if (trainAhead.Value < trainLength) // train behind not clear
                        {
                            return (false);
                        }
                    }
                }

            }

            return (true);
        }

        //================================================================================================//
        //
        // Set deadlock trap for all trains which deadlock from this section at begin section
        //

        public void SetDeadlockTrap(Train thisTrain, List<Dictionary<int, int>> thisDeadlock)
        {
            foreach (Dictionary<int, int> deadlockInfo in thisDeadlock)
            {
                foreach (KeyValuePair<int, int> deadlockDetails in deadlockInfo)
                {
                    int otherTrainNumber = deadlockDetails.Key;
                    Train otherTrain = thisTrain.GetOtherTrainByNumber(deadlockDetails.Key);

                    int endSectionIndex = deadlockDetails.Value;

                    TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

                    // if other section allready set do not set deadlock
                    if (otherTrain != null && endSection.IsSet(otherTrain)) 
                        break;

                    if (DeadlockTraps.ContainsKey(thisTrain.Number))
                    {
                        List<int> thisTrap = DeadlockTraps[thisTrain.Number];
                        if (thisTrap.Contains(otherTrainNumber))
                            break;  // cannot set deadlock for train which has deadlock on this end
                    }

                    if (endSection.DeadlockTraps.ContainsKey(otherTrainNumber))
                    {
                        if (!endSection.DeadlockTraps[otherTrainNumber].Contains(thisTrain.Number))
                        {
                            endSection.DeadlockTraps[otherTrainNumber].Add(thisTrain.Number);
                        }
                    }
                    else
                    {
                        List<int> deadlockList = new List<int>();
                        deadlockList.Add(thisTrain.Number);
                        endSection.DeadlockTraps.Add(otherTrainNumber, deadlockList);
                    }

                    if (!endSection.DeadlockActives.Contains(thisTrain.Number))
                    {
                        endSection.DeadlockActives.Add(thisTrain.Number);
                    }
                }
            }
        }
        //================================================================================================//
        //
        // Set deadlock trap for individual train at end section
        //

        public void SetDeadlockTrap(int thisTrainNumber, int otherTrainNumber)
        {
            if (DeadlockTraps.ContainsKey(otherTrainNumber))
            {
                if (!DeadlockTraps[otherTrainNumber].Contains(thisTrainNumber))
                {
                    DeadlockTraps[otherTrainNumber].Add(thisTrainNumber);
                }
            }
            else
            {
                List<int> deadlockList = new List<int>();
                deadlockList.Add(thisTrainNumber);
                DeadlockTraps.Add(otherTrainNumber, deadlockList);
            }

            if (!DeadlockActives.Contains(thisTrainNumber))
            {
                DeadlockActives.Add(thisTrainNumber);
            }
        }

        //================================================================================================//
        //
        // Clear deadlock trap
        //

        public void ClearDeadlockTrap(int thisTrainNumber)
        {
            List<int> deadlocksCleared = new List<int>();

            if (DeadlockActives.Contains(thisTrainNumber))
            {
                foreach (KeyValuePair<int, List<int>> thisDeadlock in DeadlockTraps)
                {
                    if (thisDeadlock.Value.Contains(thisTrainNumber))
                    {
                        thisDeadlock.Value.Remove(thisTrainNumber);
                        if (thisDeadlock.Value.Count <= 0)
                        {
                            deadlocksCleared.Add(thisDeadlock.Key);
                        }
                    }
                }
                DeadlockActives.Remove(thisTrainNumber);
            }

            foreach (int deadlockKey in deadlocksCleared)
            {
                DeadlockTraps.Remove(deadlockKey);
            }

            DeadlockAwaited.Remove(thisTrainNumber);

        }
        //================================================================================================//
        //
        // Check if train is waiting for deadlock
        //

        public bool CheckDeadlockAwaited(int trainNumber)
        {
            int totalCount = DeadlockAwaited.Count;
            if (DeadlockAwaited.Contains(trainNumber))
                totalCount--;
            return (totalCount > 0);
        }

        //================================================================================================//

    }// class TrackCircuitSection

    //================================================================================================//
    //
    // class TrackCircuitItems
    //
    //================================================================================================//
    //
    // Class for track circuit item storage
    //

    public class TrackCircuitItems
    {
        public TrackCircuitSignalList[,]
            TrackCircuitSignals = new TrackCircuitSignalList[2, (int)SignalHead.SIGFN.UNKNOWN];
        // List of signals (per direction and per type) //
        public TrackCircuitSignalList[]
            TrackCircuitSpeedPosts = new TrackCircuitSignalList[2];
        // List of speedposts (per direction) //
        public List<TrackCircuitMilepost> MilePosts = new List<TrackCircuitMilepost>();
        // List of mileposts //

        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitItems()
        {
            TrackCircuitSignalList thisList;

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                {
                    thisList = new TrackCircuitSignalList();
                    TrackCircuitSignals[iDirection, fntype] = thisList;
                }

                thisList = new TrackCircuitSignalList();
                TrackCircuitSpeedPosts[iDirection] = thisList;
            }
        }
    }

    //================================================================================================//
    //
    // class MilepostObject
    //
    //================================================================================================//
    //
    // Class for track circuit mileposts
    //

    public class TrackCircuitMilepost
    {
        public float MilepostValue;                        // milepost value                   //
        public float[] MilepostLocation = new float[2];    // milepost location from both ends //
    }

    //================================================================================================//
    //
    // class TrackCircuitSignalList
    //
    //================================================================================================//
    //
    // Class for track circuit signal list
    //

    public class TrackCircuitSignalList
    {
        public List<TrackCircuitSignalItem> TrackCircuitItem = new List<TrackCircuitSignalItem>();
        // List of signal items //
    }

    //================================================================================================//
    //
    // class TrackCircuitSignalItem
    //
    //================================================================================================//
    //
    // Class for track circuit signal item
    //

    public class TrackCircuitSignalItem
    {
        public ObjectItemInfo.ObjectItemFindState SignalState;  // returned state // 
        public SignalObject SignalRef;            // related SignalObject     //
        public float SignalLocation;              // relative signal position //


        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitSignalItem(SignalObject thisRef, float thisLocation)
        {
            SignalState = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
            SignalRef = thisRef;
            SignalLocation = thisLocation;
        }


        public TrackCircuitSignalItem(ObjectItemInfo.ObjectItemFindState thisState)
        {
            SignalState = thisState;
            SignalRef = null;
            SignalLocation = 0.0f;
        }
    }

    //================================================================================================//
    //
    // subclass for TrackCircuitState
    //
    //================================================================================================//
    //
    // Class for track circuit state train occupied
    //

    public class TrainOccupyState : Dictionary<Train.TrainRouted, int>
    {
        // Contains
        public bool ContainsTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain == null) return (false);
            return (ContainsKey(thisTrain.Train.routedForward) || ContainsKey(thisTrain.Train.routedBackward));
        }

        public bool ContainsTrain(Train thisTrain)
        {
            if (thisTrain == null) return (false);
            return (ContainsKey(thisTrain.routedForward) || ContainsKey(thisTrain.routedBackward));
        }

        // Remove
        public void RemoveTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain != null)
            {
                if (ContainsTrain(thisTrain.Train.routedForward)) Remove(thisTrain.Train.routedForward);
                if (ContainsTrain(thisTrain.Train.routedBackward)) Remove(thisTrain.Train.routedBackward);
            }
        }
    }

    //
    // Class for track circuit state train occupied
    //

    public class TrainQueue : Queue<Train.TrainRouted>
    {
        public Train PeekTrain()
        {
            if (Count <= 0) return (null);
            Train.TrainRouted thisTrain = Peek();
            return (thisTrain.Train);
        }

        public bool ContainsTrain(Train.TrainRouted thisTrain)
        {
            if (thisTrain == null) return (false);
            return (Contains(thisTrain.Train.routedForward) || Contains(thisTrain.Train.routedBackward));
        }
    }

    //================================================================================================//
    //
    // class TrackCircuitState
    //
    //================================================================================================//
    //
    // Class for track circuit state
    //

    public class TrackCircuitState
    {
        public TrainOccupyState TrainOccupy;                       // trains occupying section      //
        public Train.TrainRouted TrainReserved;                    // train reserving section       //
        public int SignalReserved;                                 // signal reserving section      //
        public TrainQueue TrainPreReserved;                        // trains with pre-reservation   //
        public TrainQueue TrainClaimed;                            // trains with normal claims     //
        public bool RemoteAvailable;                               // remote info available         //
        public bool RemoteOccupied;                                // remote occupied state         //
        public bool RemoteSignalReserved;                          // remote signal reserved        //
        public int RemoteReserved;                                 // remote reserved (number only) //

        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitState()
        {
            TrainOccupy = new TrainOccupyState();
            TrainReserved = null;
            SignalReserved = -1;
            TrainPreReserved = new TrainQueue();
            TrainClaimed = new TrainQueue();
        }


        //================================================================================================//
        //
        // Restore
        // IMPORTANT : trains are restored to dummy value, will be restored to full contents later
        //

        public void Restore(BinaryReader inf)
        {
            int noOccupy = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noOccupy; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                int trainDirection = inf.ReadInt32();
                Train thisTrain = new Train(trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainOccupy.Add(thisRouted, trainDirection);
            }

            int trainReserved = inf.ReadInt32();
            if (trainReserved >= 0)
            {
                int trainRouteIndexR = inf.ReadInt32();
                Train thisTrain = new Train(trainReserved);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndexR);
                TrainReserved = thisRouted;
            }

            SignalReserved = inf.ReadInt32();

            int noPreReserve = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noPreReserve; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                Train thisTrain = new Train(trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainPreReserved.Enqueue(thisRouted);
            }

            int noClaimed = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noClaimed; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                Train thisTrain = new Train(trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainClaimed.Enqueue(thisRouted);
            }

        }

        //================================================================================================//
        //
        // Reset train references after restore
        //

        public void RestoreTrains(Signals signalRef, List<Train> trains)
        {

            // Occupy

            Dictionary<int[], int> tempTrains = new Dictionary<int[], int>();

            foreach (KeyValuePair<Train.TrainRouted, int> thisOccupy in TrainOccupy)
            {
                int[] trainKey = new int[2];
                trainKey[0] = thisOccupy.Key.Train.Number;
                trainKey[1] = thisOccupy.Key.TrainRouteDirectionIndex;
                int direction = thisOccupy.Value;
                tempTrains.Add(trainKey, direction);
            }

            TrainOccupy.Clear();

            foreach (KeyValuePair<int[], int> thisTemp in tempTrains)
            {
                int[] trainKey = thisTemp.Key;
                int number = trainKey[0];
                int routeIndex = trainKey[1];
                int direction = thisTemp.Value;
                Train thisTrain = signalRef.FindTrain(number, trains);
                if (thisTrain != null)
                {
                    Train.TrainRouted thisTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainOccupy.Add(thisTrainRouted, direction);
                }
            }

            // Reserved

            if (TrainReserved != null)
            {
                int number = TrainReserved.Train.Number;
                Train reservedTrain = signalRef.FindTrain(number, trains);
                if (reservedTrain != null)
                {
                    int reservedDirection = TrainReserved.TrainRouteDirectionIndex;
                    //if (!MultiPlayer.MPManager.IsMultiPlayer())
                        TrainReserved = reservedDirection == 0 ? reservedTrain.routedForward : reservedTrain.routedBackward;
                }
                else
                {
                    TrainReserved = null;
                }
            }

            // PreReserved

            Queue<Train.TrainRouted> tempQueue = new Queue<Train.TrainRouted>();

            foreach (Train.TrainRouted thisTrainRouted in TrainPreReserved)
            {
                tempQueue.Enqueue(thisTrainRouted);
            }
            TrainPreReserved.Clear();
            foreach (Train.TrainRouted thisTrainRouted in tempQueue)
            {
                Train thisTrain = signalRef.FindTrain(thisTrainRouted.Train.Number, trains);
                int routeIndex = thisTrainRouted.TrainRouteDirectionIndex;
                if (thisTrain != null)
                {
                    Train.TrainRouted foundTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainPreReserved.Enqueue(foundTrainRouted);
                }
            }

            // Claimed

            tempQueue.Clear();

            foreach (Train.TrainRouted thisTrainRouted in TrainClaimed)
            {
                tempQueue.Enqueue(thisTrainRouted);
            }
            TrainClaimed.Clear();
            foreach (Train.TrainRouted thisTrainRouted in tempQueue)
            {
                Train thisTrain = signalRef.FindTrain(thisTrainRouted.Train.Number, trains);
                int routeIndex = thisTrainRouted.TrainRouteDirectionIndex;
                if (thisTrain != null)
                {
                    Train.TrainRouted foundTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainClaimed.Enqueue(foundTrainRouted);
                }
            }

        }

        //================================================================================================//
        //
        // Save
        //

        public void Save(BinaryWriter outf)
        {
            outf.Write(TrainOccupy.Count);
            foreach (KeyValuePair<Train.TrainRouted, int> thisOccupy in TrainOccupy)
            {
                Train.TrainRouted thisTrain = thisOccupy.Key;
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
                outf.Write(thisOccupy.Value);
            }

            if (TrainReserved == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(TrainReserved.Train.Number);
                outf.Write(TrainReserved.TrainRouteDirectionIndex);
            }

            outf.Write(SignalReserved);

            outf.Write(TrainPreReserved.Count);
            foreach (Train.TrainRouted thisTrain in TrainPreReserved)
            {
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
            }

            outf.Write(TrainClaimed.Count);
            foreach (Train.TrainRouted thisTrain in TrainClaimed)
            {
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
            }

        }

        //================================================================================================//
        //
        // Get list of trains occupying track, in required direction if required
        //

        public List<Train.TrainRouted> TrainsOccupying()
        {
            List<Train.TrainRouted> reqList = new List<Train.TrainRouted>();
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                reqList.Add(thisTCT.Key);
            }
            return (reqList);
        }

        public List<Train.TrainRouted> TrainsOccupying(int reqDirection)
        {
            List<Train.TrainRouted> reqList = new List<Train.TrainRouted>();
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                if (thisTCT.Value == reqDirection)
                {
                    reqList.Add(thisTCT.Key);
                }
            }
            return (reqList);
        }

        //================================================================================================//
        //
        // check if any trains occupy track, in required direction if required
        //

        public bool HasTrainsOccupying()
        {
            return (TrainOccupy.Count > 0);
        }

        public bool HasTrainsOccupying(int reqDirection, bool stationary)
        {
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                if (thisTCT.Value == reqDirection)
                {
                    if (Math.Abs(thisTCT.Key.Train.SpeedMpS) > 0.5f)
                        return (true);   // exclude (almost) stationary trains
                }

                if ((Math.Abs(thisTCT.Key.Train.SpeedMpS) <= 0.5f) && stationary)
                    return (true);   // (almost) stationay trains
            }

            return (false);
        }

        public bool HasOtherTrainsOccupying(Train.TrainRouted thisTrain)
        {
            if (TrainOccupy.Count == 0)  // no trains
            {
                return (false);
            }

            if (TrainOccupy.Count == 1 && TrainOccupy.ContainsTrain(thisTrain))  // only one train and that one is us
            {
                return (false);
            }

            return (true);
        }

        //================================================================================================//
        //
        // check if this train occupies track
        //

        // routed train
        public bool ThisTrainOccupying(Train.TrainRouted thisTrain)
        {
            return (TrainOccupy.ContainsTrain(thisTrain));
        }

        // unrouted train
        public bool ThisTrainOccupying(Train thisTrain)
        {
            return (TrainOccupy.ContainsTrain(thisTrain));
        }

    }

    //================================================================================================//
    //
    // class CrossOverItem
    //
    //================================================================================================//
    //
    // Class for cross over items
    //

    public class CrossOverItem
    {
        public float[] Position = new float[2];        // position within track sections //
        public int[] SectionIndex = new int[2];          // indices of original sections   //
        public int[] ItemIndex = new int[2];             // TDB item indices               //
        public uint TrackShape;
    }

    //================================================================================================//
    //
    // class TrackCircuitXRefList
    //

    public class TrackCircuitXRefList : List<TrackCircuitCrossReference>
    {

        //================================================================================================//
        //
        // get XRef index
        //

        private int GetXRefIndex(float offset, int direction)
        {
            int foundSection = -1;

            if (direction == 0)
            {
                for (int TC = 1; TC < this.Count && foundSection < 0; TC++)
                {
                    TrackCircuitCrossReference thisReference = this[TC];
                    if (thisReference.Position[direction] > offset)
                    {
                        foundSection = TC - 1;
                    }
                }

                if (foundSection < 0)
                {
                    TrackCircuitCrossReference thisReference = this[this.Count - 1];
                    if (offset <= (thisReference.Position[direction] + thisReference.Length))
                    {
                        foundSection = this.Count - 1;
                    }
                }
            }
            else
            {
                for (int TC = this.Count - 2; TC >= 0 && foundSection < 0; TC--)
                {
                    TrackCircuitCrossReference thisReference = this[TC];
                    if (thisReference.Position[direction] > offset)
                    {
                        foundSection = TC + 1;
                    }
                }

                if (foundSection < 0)
                {
                    TrackCircuitCrossReference thisReference = this[0];
                    if (offset <= (thisReference.Position[direction] + thisReference.Length))
                    {
                        foundSection = 0;
                    }
                }
            }

            if (foundSection < 0)
            {
                if (direction == 0)
                {
                    foundSection = 0;
                }
                else
                {
                    foundSection = this.Count - 1;
                }
            }

            return (foundSection);
        }

        //================================================================================================//
        //
        // Get Section index
        //

        public int GetSectionIndex(float offset, int direction)
        {
            int XRefIndex = GetXRefIndex(offset, direction);

            if (XRefIndex >= 0)
            {
                TrackCircuitCrossReference thisReference = this[XRefIndex];
                return (thisReference.CrossRefIndex);
            }
            else
            {
                return (-1);
            }
        }

        //================================================================================================//
        //
        // Get TCPosition
        //

        public void GetTCPosition(float offset, int direction, ref Train.TCPosition thisPosition)
        {
            int XRefIndex = GetXRefIndex(offset, direction);

            if (XRefIndex >= 0)
            {
                TrackCircuitCrossReference thisReference = this[XRefIndex];
                thisPosition.TCSectionIndex = thisReference.CrossRefIndex;
                thisPosition.TCDirection = direction;
                thisPosition.TCOffset = offset - thisReference.Position[direction];
            }
        }

    } // class TrackCircuitXRefList

    //================================================================================================//
    //
    // class TrackCircuitCrossReference
    //
    //================================================================================================//
    //
    // Class for track circuit cross reference, added to TDB info
    //

    public class TrackCircuitCrossReference
    {
        public int CrossRefIndex;
        public float Length;
        public float[] Position = new float[2];

        //================================================================================================//
        //
        // Constructor
        //

        public TrackCircuitCrossReference(ORTS.TrackCircuitSection thisSection)
        {
            CrossRefIndex = thisSection.Index;
            Length = thisSection.Length;
            Position[0] = thisSection.OffsetLength[0];
            Position[1] = thisSection.OffsetLength[1];
        }

    }

    //================================================================================================//
    //
    //  class SignalObject
    //
    //================================================================================================//

    public class SignalObject
    {

        public enum BLOCKSTATE
        {
            CLEAR,         // Block ahead is clear and accesible
            OCCUPIED,      // Block ahead is occupied by one or more wagons/locos not moving in opposite direction
            JN_OBSTRUCTED  // Block ahead is impassable due to the state of a switch or occupied by moving train or not accesible
        }

        public enum INTERNAL_BLOCKSTATE
        {
            RESERVED,              // all sections reserved for requiring train       //
            RESERVABLE,            // all secetions clear and reservable for train    //
            OCCUPIED_SAMEDIR,      // occupied by train moving in same direction      //
            RESERVED_OTHER,        // reserved for other train                        //
            OCCUPIED_OPPDIR,       // occupied by train moving in opposite direction  //
            OPEN,                  // sections are claimed and not accesible          //
            BLOCKED                // switch locked against train                     //
        }

        public enum PERMISSION
        {
            GRANTED,
            REQUESTED,
            DENIED
        }

        public enum HOLDSTATE                   // signal is locked in hold
        {
            NONE,                           // signal is clear
            STATION_STOP,                   // because of station stop
            MANUAL_LOCK,                     // because of manual lock. 
            MANUAL_PASS,                      //Sometime you want to set a light green, especially in MP
            MANUAL_APPROACH                   //Sometime to set approach, in MP again
            //PLEASE DO NOT CHANGE THE ORDER OF THESE ENUMS
        }

        //
        // for future extention
        //
        //              public enum CONTROLSTATE
        //              {
        //                      AUTO,                           // signal is in AUTO mode
        //                      AUTO_PATHED,                    // signal is in AUTO mode but will revert to MANUAL for unpathed train
        //                      MANUAL_RESTRICT,                // signal is under MANUAL control - for restricted access
        //                      MANUAL_FULL,                    // signal is under MANUAL control - full route only
        //                      MANUAL_REPEAT,                  // signal is under MANUAL control - repeated clearance
        //                      MANUAL_PATHED                   // signal is under MANUAL control - follow pathed route if available
        //              }
        //
        //              public enum MANUALREQUESTRESPONSE
        //              {
        //                      ALLREADY_ENABLED,               // request rejected as signal is allready enabled by train
        //                      ROUTE_NOT_AVAILABLE             // request rejected as route is not available
        //              }

        public Signals signalRef;               // reference to overlaying Signal class
        public static SignalObject[] signalObjects;
        public static TrackNode[] trackNodes;
        public static TrItem[] trItems;
        public SignalWorldObject WorldObject;   // Signal World Object information

        public int trackNode;                   // Track node which contains this signal
        public int trRefIndex;                  // Index to TrItemRef within Track Node 

        public int TCReference = -1;            // Reference to TrackCircuit (index)
        public float TCOffset;                  // Position within TrackCircuit
        public int TCDirection;                 // Direction within TrackCircuit
        public int TCNextTC = -1;               // Index of next TrackCircuit (NORMAL signals only)
        public int TCNextDirection;             // Direction of next TrackCircuit 

        public List<int> JunctionsPassed = new List<int>();  // Junctions which are passed checking next signal //

        public int thisRef;                     // This signal's reference.
        public int direction;                   // Direction facing on track

        public bool isSignal = true;            // if signal, false if speedpost //
        public List<SignalHead> SignalHeads = new List<SignalHead>();

        public int SignalNumClearAhead_MSTS = -2;    // Overall maximum SignalNumClearAhead over all heads (MSTS calculation)
        public int SignalNumClearAhead_ORTS = -2;    // Overall maximum SignalNumClearAhead over all heads (ORTS calculation)
        public int SignalNumNormalHeads = 0;         // no. of normal signal heads in signal
        public int ReqNumClearAhead = 0;        // Passed on value for SignalNumClearAhead

        public int draw_state;                  // actual signal state

        public Train.TrainRouted enabledTrain = null; // full train structure for which signal is enabled

        private INTERNAL_BLOCKSTATE internalBlockState = INTERNAL_BLOCKSTATE.OPEN;    // internal blockstate
        public PERMISSION hasPermission = PERMISSION.DENIED;  // Permission to pass red signal
        public HOLDSTATE holdState = HOLDSTATE.NONE;
        //              public CONTROLSTATE controlState = CONTROLSTATE.AUTO;   // future extension
        //              public Train.TCSubpathRoute manualRoute = new Train.TCSubpathRoute();
        //              public bool manualRouteSet = false;
        //              public int manualRouteState = 0;

        public int[] sigfound = new int[(int)SignalHead.SIGFN.UNKNOWN];  // active next signal - used for signals with NORMAL heads only
        private int[] defaultNextSignal = new int[(int)SignalHead.SIGFN.UNKNOWN];  // default next signal
        public Traveller tdbtraveller;          // TDB traveller to determine distance between objects

        public Train.TCSubpathRoute signalRoute = new Train.TCSubpathRoute();  // train route from signal
        public int trainRouteDirectionIndex = 0;// direction index in train route array (usually 0, value 1 valid for Manual only)
        private int thisTrainRouteIndex;        // index of section after signal in train route list

        private Train.TCSubpathRoute fixedRoute = new Train.TCSubpathRoute();     // fixed route from signal
        public bool hasFixedRoute = false;       // signal has no fixed route
        private bool fullRoute = false;          // required route is full route to next signal or end-of-track
        private bool propagated = false;         // route request propagated to next signal
        private bool isPropagated = false;       // route request for this signal was propagated from previous signal

        public bool enabled
        {
            get
            {
                if (MultiPlayer.MPManager.IsMultiPlayer() && MultiPlayer.MPManager.PreferGreen == true) return true;
                return (enabledTrain != null);

                // future extension when manual is included : replace above with :
                //
                // if (enabledTrain != null)
                // {
                //    return (true);
                // }
                // else if (manualRouteSet)
                // {
                //    return (true);
                // {
                // return (false);
            }
        }

        public BLOCKSTATE blockState
        {
            get
            {
                BLOCKSTATE lstate = BLOCKSTATE.JN_OBSTRUCTED;
                switch (internalBlockState)
                {
                    case INTERNAL_BLOCKSTATE.RESERVED:
                    case INTERNAL_BLOCKSTATE.RESERVABLE:
                        lstate = BLOCKSTATE.CLEAR;
                        break;
                    case INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR:
                        lstate = BLOCKSTATE.OCCUPIED;
                        break;
                    default:
                        lstate = BLOCKSTATE.JN_OBSTRUCTED;
                        break;
                }

                return (lstate);
            }
        }

        public int trItem
        {
            get
            {
                return trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
            }
        }

        public int revDir                //  Needed because signal faces train!
        {
            get
            {
                return direction == 0 ? 1 : 0;
            }
        }

        //================================================================================================//
        ///
        //  Constructor for empty item
        ///

        public SignalObject()
        {
        }

        //================================================================================================//
        ///
        //  Constructor for Copy 
        ///

        public SignalObject(SignalObject copy)
        {
            signalRef = copy.signalRef;
            WorldObject = new SignalWorldObject(copy.WorldObject);

            trackNode = copy.trackNode;

            TCReference = copy.TCReference;
            TCOffset = copy.TCOffset;
            TCDirection = copy.TCDirection;
            TCNextTC = copy.TCNextTC;
            TCNextDirection = copy.TCNextDirection;

            direction = copy.direction;
            isSignal = copy.isSignal;
            SignalNumClearAhead_MSTS = copy.SignalNumClearAhead_MSTS;
            SignalNumClearAhead_ORTS = copy.SignalNumClearAhead_ORTS;
            SignalNumNormalHeads = copy.SignalNumNormalHeads;

            draw_state = copy.draw_state;
            internalBlockState = copy.internalBlockState;
            hasPermission = copy.hasPermission;

            tdbtraveller = new Traveller(copy.tdbtraveller);

            sigfound = new int[copy.sigfound.Length];
            copy.sigfound.CopyTo(sigfound, 0);
            defaultNextSignal = new int[copy.defaultNextSignal.Length];
            copy.defaultNextSignal.CopyTo(defaultNextSignal, 0);
        }

        //================================================================================================//
        //
        // Constructor for restore
        // IMPORTANT : enabled train is restore temporarily as Trains are restored later as Signals
        // Full restore of train link follows in RestoreTrains
        //

        public void Restore(BinaryReader inf)
        {
            int trainNumber = inf.ReadInt32();

            for (int iSig = 0; iSig < sigfound.Length; iSig++)
            {
                sigfound[iSig] = inf.ReadInt32();
            }

            bool validRoute = inf.ReadBoolean();

            if (validRoute)
            {
                signalRoute = new Train.TCSubpathRoute(inf);
            }

            thisTrainRouteIndex = inf.ReadInt32();
            holdState = (HOLDSTATE)inf.ReadInt32();

            int totalJnPassed = inf.ReadInt32();

            for (int iJn = 0; iJn < totalJnPassed; iJn++)
            {
                int thisJunction = inf.ReadInt32();
                JunctionsPassed.Add(thisJunction);
                signalRef.TrackCircuitList[thisJunction].SignalsPassingRoutes.Add(thisRef);
            }

            fullRoute = inf.ReadBoolean();
            propagated = inf.ReadBoolean();
            isPropagated = inf.ReadBoolean();
            ReqNumClearAhead = inf.ReadInt32();

            // set dummy train, route direction index will be set later on restore of train

            enabledTrain = null;
            if (trainNumber >= 0)
            {
                Train thisTrain = new Train(trainNumber);
                Train.TrainRouted thisTrainRouted = new Train.TrainRouted(thisTrain, 0);
                enabledTrain = thisTrainRouted;
            }

            // restore for manual settings

            // controlState = (CONTROLSTATE) inf.ReadInt32();
            // bool validManualRoute = inf.ReadBoolean();
            // if (validManualRoute)
            // {
            //     manualRoute = new Train.TCSubpathRoute(inf);
            // }
            //
            // manualRouteSet = inf.ReadBoolean();
            // manualRouteState = inf.ReadInt32();
            //

        }

        //================================================================================================//
        //
        // Restore Train Reference
        //

        public void RestoreTrains(List<Train> trains)
        {
            if (enabledTrain != null)
            {
                int number = enabledTrain.Train.Number;
                int routeIndex = enabledTrain.TrainRouteDirectionIndex;

                Train foundTrain = signalRef.FindTrain(number, trains);

                // check if this signal is next signal forward for this train

                if (foundTrain != null && foundTrain.NextSignalObject[0] != null && this.thisRef == foundTrain.NextSignalObject[0].thisRef)
                {
                    enabledTrain = foundTrain.routedForward;
                    foundTrain.NextSignalObject[0] = this;
                }

                // check if this signal is next signal backward for this train

                else if (foundTrain != null && foundTrain.NextSignalObject[1] != null && this.thisRef == foundTrain.NextSignalObject[1].thisRef)
                {
                    enabledTrain = foundTrain.routedBackward;
                    foundTrain.NextSignalObject[1] = this;
                }
                else
                {
                    // check if this section is reserved for this train

                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                    if (thisSection.CircuitState.TrainReserved != null && thisSection.CircuitState.TrainReserved.Train.Number == number)
                    {
                        enabledTrain = thisSection.CircuitState.TrainReserved;
                    }
                    else
                    {
                        enabledTrain = null; // reset - train not found
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Restore Signal Aspect based on train information
        // Process non-propagated signals only, others are updated through propagation
        //

        public void RestoreAspect()
        {
            if (enabledTrain != null && !isPropagated)
            {
                if (isSignalNormal())
                {
                    checkRouteState(false, signalRoute, enabledTrain);
                    propagateRequest();
                    StateUpdate();
                }
                else
                {
                    getBlockState_notRouted();
                    StateUpdate();
                }
            }
        }

        //================================================================================================//
        //
        // Save
        //

        public void Save(BinaryWriter outf)
        {
            if (enabledTrain == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(enabledTrain.Train.Number);
            }

            foreach (int thisSig in sigfound)
            {
                outf.Write(thisSig);
            }

            if (signalRoute == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                signalRoute.Save(outf);
            }

            outf.Write(thisTrainRouteIndex);
            outf.Write((int)holdState);

            outf.Write(JunctionsPassed.Count);
            if (JunctionsPassed.Count > 0)
            {
                foreach (int thisJunction in JunctionsPassed)
                {
                    outf.Write(thisJunction);
                }
            }

            outf.Write(fullRoute);
            outf.Write(propagated);
            outf.Write(isPropagated);
            outf.Write(ReqNumClearAhead);

            //  outf.Write((int) ControlState);
            //  if (manualRoute == null)
            //  {
            //     outf.Write(false);
            //  }
            //  else
            //  {
            //     outf.Write(true);
            //     manualRoute.Save(outf);
            //  }
            //
            //  outf.Write(manualRouteSet);
            //  outf.Write(manualRouteState);
            //
        }

        //================================================================================================//
        //
        // return blockstate
        //

        public BLOCKSTATE block_state()
        {
            return (blockState);
        }

        //================================================================================================//
        ///
        // setSignalDefaultNextSignal : set default next signal based on non-Junction tracks ahead
        // this routine also sets fixed routes for signals which do not lead onto junction or crossover
        //
        ///

        public void setSignalDefaultNextSignal()
        {
            int thisTC = TCReference;
            float position = TCOffset;
            int direction = TCDirection;
            bool setFixedRoute = false;

            // for normal signals : start at next TC

            if (TCNextTC > 0)
            {
                thisTC = TCNextTC;
                direction = TCNextDirection;
                position = 0.0f;
                setFixedRoute = true;
            }

            bool completedFixedRoute = !setFixedRoute;

            // get trackcircuit

            TrackCircuitSection thisSection = null;
            if (thisTC > 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];
            }

            // set default

            for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
            {
                defaultNextSignal[fntype] = -1;
            }

            // loop through valid sections

            while (thisSection != null && thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
            {

                if (!completedFixedRoute)
                {
                    fixedRoute.Add(new Train.TCRouteElement(thisSection.Index, direction));
                }

                // normal signal

                if (defaultNextSignal[(int)SignalHead.SIGFN.NORMAL] < 0)
                {
                    if (thisSection.EndSignals[direction] != null)
                    {
                        defaultNextSignal[(int)SignalHead.SIGFN.NORMAL] = thisSection.EndSignals[direction].thisRef;
                        completedFixedRoute = true;
                    }
                }

                // other signals

                for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                {
                    if (fntype != (int)SignalHead.SIGFN.NORMAL && fntype != (int)SignalHead.SIGFN.UNKNOWN)
                    {
                        TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction, fntype];
                        bool signalFound = defaultNextSignal[fntype] >= 0;
                        for (int iItem = 0; iItem < thisList.TrackCircuitItem.Count && !signalFound; iItem++)
                        {
                            TrackCircuitSignalItem thisItem = thisList.TrackCircuitItem[iItem];
                            if (thisItem.SignalLocation > position)
                            {
                                defaultNextSignal[fntype] = thisItem.SignalRef.thisRef;
                                signalFound = true;
                            }
                        }
                    }
                }

                int pinIndex = direction;
                direction = thisSection.Pins[pinIndex, 0].Direction;
                thisSection = signalRef.TrackCircuitList[thisSection.Pins[pinIndex, 0].Link];
            }

            // copy default as valid items

            for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
            {
                sigfound[fntype] = defaultNextSignal[fntype];
            }

            // Allow use of fixed route if ended on END_OF_TRACK

            if (thisSection != null && thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
            {
                completedFixedRoute = true;
            }

            // if valid next normal signal found, signal has fixed route

            if (setFixedRoute && completedFixedRoute)
            {
                hasFixedRoute = true;
                fullRoute = true;
            }
            else
            {
                hasFixedRoute = false;
                fixedRoute.Clear();
            }
        }

        //================================================================================================//
        ///
        // isSignalNormal : Returns true if at least one signal head is type normal.
        ///

        public bool isSignalNormal()
        {
            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction == SignalHead.SIGFN.NORMAL)
                    return true;
            }
            return false;
        }

        //================================================================================================//
        ///
        // isSignalType : Returns true if at least one signal head is of required type
        ///

        public bool isSignalType(SignalHead.SIGFN[] reqSIGFN)
        {
            foreach (SignalHead sigHead in SignalHeads)
            {
                if (reqSIGFN.Contains(sigHead.sigFunction))
                    return true;
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

            SignalHead.SIGFN[] fn_type_array = new SignalHead.SIGFN[1];

            int nextSignal = sigfound[(int)fn_type];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(fn_type);
                sigfound[(int)fn_type] = nextSignal;
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
            SignalHead.SIGFN[] fn_type_array = new SignalHead.SIGFN[1];

            int nextSignal = sigfound[(int)fn_type];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(fn_type);
                sigfound[(int)fn_type] = nextSignal;
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
        //
        // opp_sig_mr
        //

        public SignalHead.SIGASP opp_sig_mr(SignalHead.SIGFN fn_type)
        {
            int signalFound = SONextSignalOpp(fn_type);
            return (signalFound >= 0 ? signalObjects[signalFound].this_sig_mr(fn_type) : SignalHead.SIGASP.STOP);
        }//opp_sig_mr

        public SignalHead.SIGASP opp_sig_mr(SignalHead.SIGFN fn_type, ref SignalObject foundSignal) // used for debug print process
        {
            int signalFound = SONextSignalOpp(fn_type);
            foundSignal = signalFound >= 0 ? signalObjects[signalFound] : null;
            return (signalFound >= 0 ? signalObjects[signalFound].this_sig_mr(fn_type) : SignalHead.SIGASP.STOP);
        }//opp_sig_mr

        //================================================================================================//
        //
        // opp_sig_lr
        //

        public SignalHead.SIGASP opp_sig_lr(SignalHead.SIGFN fn_type)
        {
            int signalFound = SONextSignalOpp(fn_type);
            return (signalFound >= 0 ? signalObjects[signalFound].this_sig_lr(fn_type) : SignalHead.SIGASP.STOP);
        }//opp_sig_lr

        public SignalHead.SIGASP opp_sig_lr(SignalHead.SIGFN fn_type, ref SignalObject foundSignal) // used for debug print process
        {
            int signalFound = SONextSignalOpp(fn_type);
            foundSignal = signalFound >= 0 ? signalObjects[signalFound] : null;
            return (signalFound >= 0 ? signalObjects[signalFound].this_sig_lr(fn_type) : SignalHead.SIGASP.STOP);
        }//opp_sig_lr

        //================================================================================================//
        //
        // this_sig_mr : Returns the most restrictive state of this signal's heads of required type
        //

        // standard version without state return
        public SignalHead.SIGASP this_sig_mr(SignalHead.SIGFN fn_type)
        {
            bool sigfound = false;
            return (this_sig_mr(fn_type, ref sigfound));
        }

        // additional version with state return
        public SignalHead.SIGASP this_sig_mr(SignalHead.SIGFN fn_type, ref bool sigfound)
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
                sigfound = false;
                return SignalHead.SIGASP.STOP;
            }
            else
            {
                sigfound = true;
                return sigAsp;
            }
        }//this_sig_mr

        //================================================================================================//
        //
        // this_sig_lr : Returns the least restrictive state of this signal's heads of required type
        //

        // standard version without state return
        public SignalHead.SIGASP this_sig_lr(SignalHead.SIGFN fn_type)
        {
            bool sigfound = false;
            return (this_sig_lr(fn_type, ref sigfound));
        }

        // additional version with state return
        public SignalHead.SIGASP this_sig_lr(SignalHead.SIGFN fn_type, ref bool sigfound)
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

            sigfound = sigAspSet;

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

            if (set_speed.speed_pass > 1E9f)
                set_speed.speed_pass = -1;
            if (set_speed.speed_freight > 1E9f)
                set_speed.speed_freight = -1;

            return set_speed;
        }//this_lim_speed

        //================================================================================================//
        //
        // route_set : check if required route is set
        //

        public bool route_set(int req_mainnode, uint req_jnnode)
        {
            bool routeset = false;

            // if signal is enabled for a train, check if required section is in train route path

            if (enabledTrain != null && !MultiPlayer.MPManager.IsMultiPlayer())
            {
                Train.TCSubpathRoute RoutePart = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex];

                TrackNode thisNode = signalRef.trackDB.TrackNodes[req_mainnode];
                for (int iSection = 0; iSection <= thisNode.TCCrossReference.Count - 1 && !routeset; iSection++)
                {
                    int sectionIndex = thisNode.TCCrossReference[iSection].CrossRefIndex;

                    for (int iElement = 0; iElement < RoutePart.Count && !routeset; iElement++)
                    {
                        routeset = (sectionIndex == RoutePart[iElement].TCSectionIndex);
                    }
                }
            }

            // not enabled, follow set route but only if not normal signal (normal signal will not clear if not enabled)
            else if (!isSignalNormal() || MultiPlayer.MPManager.IsMultiPlayer())
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                int curDirection = TCDirection;
                int newDirection = 0;
                int sectionIndex = -1;
                bool passedTrackJn = false;

                routeset = (req_mainnode == thisSection.OriginalIndex);
                while (!routeset && thisSection != null)
                {
                    if (thisSection.ActivePins[curDirection, 0].Link >= 0)
                    {
                        newDirection = thisSection.ActivePins[curDirection, 0].Direction;
                        sectionIndex = thisSection.ActivePins[curDirection, 0].Link;
                    }
                    else
                    {
                        newDirection = thisSection.ActivePins[curDirection, 1].Direction;
                        sectionIndex = thisSection.ActivePins[curDirection, 1].Link;
                    }

                    // if Junction, if active pins not set use selected route
                    if (sectionIndex < 0 && thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                    {
                        // check if this is required junction
                        if (Convert.ToUInt32(thisSection.Index) == req_jnnode)
                        {
                            passedTrackJn = true;
                        }
                        // break if passed required junction
                        else if (passedTrackJn)
                        {
                            break;
                        }

                        if (thisSection.ActivePins[1, 0].Link == -1 && thisSection.ActivePins[1, 1].Link == -1)
                        {
                            int selectedDirection = signalRef.trackDB.TrackNodes[thisSection.OriginalIndex].TrJunctionNode.SelectedRoute;
                            newDirection = thisSection.Pins[1, selectedDirection].Direction;
                            sectionIndex = thisSection.Pins[1, selectedDirection].Link;
                        }
                    }

                    // if NORMAL, if active pins not set use default pins
                    if (sectionIndex < 0 && thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        newDirection = thisSection.Pins[curDirection, 0].Direction;
                        sectionIndex = thisSection.Pins[curDirection, 0].Link;
                    }

                    // next section
                    if (sectionIndex >= 0)
                    {
                        thisSection = signalRef.TrackCircuitList[sectionIndex];
                        curDirection = newDirection;
                        routeset = (req_mainnode == thisSection.OriginalIndex);
                    }
                    else
                    {
                        thisSection = null;
                    }
                }
            }

            return (routeset);
        }

        //================================================================================================//
        //
        // Find next signal of specified type along set sections - not for NORMAL signals
        //

        public int SONextSignal(SignalHead.SIGFN fntype)
        {
            int thisTC = TCReference;
            int direction = TCDirection;
            int signalFound = -1;
            TrackCircuitSection thisSection = null;
            bool sectionSet = false;

            // for normal signals

            if (fntype == SignalHead.SIGFN.NORMAL)
            {
                if (isSignalNormal())        // if this signal is normal : cannot be done using this route (set through sigfound variable)
                    return (-1);
                signalFound = SONextSignalNormal(TCReference);   // other types of signals (sigfound not used)
            }

        // for other signals : move to next TC (signal would have been default if within same section)

            else
            {
                thisSection = signalRef.TrackCircuitList[thisTC];
                sectionSet = enabledTrain == null ? false : thisSection.IsSet(enabledTrain);

                if (sectionSet)
                {
                    int pinIndex = direction;
                    thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                    direction = thisSection.ActivePins[pinIndex, 0].Direction;
                }
            }

            // loop through valid sections

            while (sectionSet && thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                    thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if required type of signal is along this section

                TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction, (int)fntype];
                if (thisList.TrackCircuitItem.Count > 0)
                {
                    signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                }

                // get next section if active link is set

                if (signalFound < 0)
                {
                    int pinIndex = direction;
                    sectionSet = thisSection.IsSet(enabledTrain);
                    if (sectionSet)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (thisTC == -1)
                        {
                            thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }
                    }
                }
            }

            if (signalFound < 0 && enabledTrain != null) // if signal not found following switches use signal route
            {
                for (int iSection = 0; iSection <= (signalRoute.Count - 1) && signalFound < 0; iSection++)
                {
                    thisSection = signalRef.TrackCircuitList[signalRoute[iSection].TCSectionIndex];
                    direction = signalRoute[iSection].Direction;
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction, (int)fntype];
                    if (thisList.TrackCircuitItem.Count > 0)
                    {
                        signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                    }
                }
            }

            return (signalFound);
        }

        //================================================================================================//
        //
        // Find next signal of specified type along set sections - NORMAL signals ONLY
        //

        private int SONextSignalNormal(int thisTC)
        {
            int direction = TCDirection;
            int signalFound = -1;
            TrackCircuitSection thisSection = null;

            int pinIndex = direction;

            if (thisTC < 0)
            {
                thisTC = TCReference;
                thisSection = signalRef.TrackCircuitList[thisTC];
                pinIndex = direction;
                thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                direction = thisSection.ActivePins[pinIndex, 0].Direction;
            }

            // loop through valid sections

            while (thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                    thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if normal signal is along this section

                if (thisSection.EndSignals[direction] != null)
                {
                    signalFound = thisSection.EndSignals[direction].thisRef;
                }

                // get next section if active link is set

                if (signalFound < 0)
                {
                    pinIndex = direction;
                    thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                    direction = thisSection.ActivePins[pinIndex, 0].Direction;
                    if (thisTC == -1)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                        direction = thisSection.ActivePins[pinIndex, 1].Direction;
                    }

                    // if no active link but signal has train and route allocated, use train route to find next section

                    if (thisTC == -1 && enabledTrain != null)
                    {
                        int thisIndex = signalRoute.GetRouteIndex(thisSection.Index, 0);
                        if (thisIndex >= 0 && thisIndex <= signalRoute.Count - 2)
                        {
                            thisTC = signalRoute[thisIndex + 1].TCSectionIndex;
                            direction = signalRoute[thisIndex + 1].Direction;
                        }
                    }
                }
            }

            return (signalFound);
        }

        //================================================================================================//
        //
        // SONextSignalOpp : find next signal in opp direction
        //

        public int SONextSignalOpp(SignalHead.SIGFN fntype)
        {
            int thisTC = TCReference;
            int direction = TCDirection == 0 ? 1 : 0;    // reverse direction
            int signalFound = -1;

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisTC];
            bool sectionSet = enabledTrain == null ? false : thisSection.IsSet(enabledTrain);

            // loop through valid sections

            while (sectionSet && thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                    thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if required type of signal is along this section

                if (fntype == SignalHead.SIGFN.NORMAL)
                {
                    signalFound = thisSection.EndSignals[direction] != null ? thisSection.EndSignals[direction].thisRef : -1;
                }
                else
                {
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction, (int)fntype];
                    if (thisList.TrackCircuitItem.Count > 0)
                    {
                        signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                    }
                }

                // get next section if active link is set

                if (signalFound < 0)
                {
                    int pinIndex = direction;
                    sectionSet = thisSection.IsSet(enabledTrain);
                    if (sectionSet)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (thisTC == -1)
                        {
                            thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }
                    }
                }
            }

            return (signalFound);
        }

        //================================================================================================//
        //
        // Update : perform route check and state update
        //

        public void Update()
        {
            // perform route update for normal signals if enabled

            if (isSignalNormal())
            {
                // if in hold, set to most restrictive for each head

                if (holdState != HOLDSTATE.NONE)
                {
                    foreach (SignalHead sigHead in SignalHeads)
                    {
                        if (holdState == HOLDSTATE.MANUAL_LOCK || holdState == HOLDSTATE.STATION_STOP) sigHead.SetMostRestrictiveAspect();
                    }
                    return;
                }


                // if enabled - perform full update and propagate if not yet done

                if (enabledTrain != null)
                {
                    // if internal state is not reserved (route fully claimed), perform route check

                    if (internalBlockState != INTERNAL_BLOCKSTATE.RESERVED)
                    {
                        checkRouteState(isPropagated, signalRoute, enabledTrain);
                    }

                    // propagate request

                    if (!isPropagated)
                    {
                        propagateRequest();
                    }

                    StateUpdate();

                    // propagate request if not yet done

                    if (!propagated && enabledTrain != null)
                    {
                        propagateRequest();
                    }
                }

        // fixed route - check route and update

                else if (hasFixedRoute)
                {

                    // if internal state is not reserved (route fully claimed), perform route check

                    if (internalBlockState != INTERNAL_BLOCKSTATE.RESERVED)
                    {
                        checkRouteState(true, fixedRoute, null);
                    }

                    StateUpdate();

                }

        // no route - perform update only

                else
                {
                    StateUpdate();
                }

            }

        // check blockstate for other signals

            else
            {
                getBlockState_notRouted();
                StateUpdate();
            }
        }

        //================================================================================================//
        //
        // reset signal as train has passed
        //

        public void resetSignalEnabled()
        {

            // reset train information

            enabledTrain = null;
            trainRouteDirectionIndex = 0;
            signalRoute.Clear();
            fullRoute = hasFixedRoute;
            thisTrainRouteIndex = -1;

            isPropagated = false;
            propagated = false;

            // reset block state to most restrictive

            internalBlockState = INTERNAL_BLOCKSTATE.BLOCKED;

            // reset next signal information to default

            for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
            {
                sigfound[fntype] = defaultNextSignal[fntype];
            }

            foreach (int thisSectionIndex in JunctionsPassed)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                thisSection.SignalsPassingRoutes.Remove(thisRef);
            }

            // reset permission //

            hasPermission = PERMISSION.DENIED;

            StateUpdate();

        }

        //================================================================================================//
        //
        // StateUpdate : Perform the update for each head on this signal to determine state of signal.
        //

        public void StateUpdate()
        {
            // update all normal heads first

            if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                if (MultiPlayer.MPManager.IsClient()) return; //client won't handle signal update

                //if there were hold manually, will not update
                if (holdState == HOLDSTATE.MANUAL_APPROACH || holdState == HOLDSTATE.MANUAL_LOCK || holdState == HOLDSTATE.MANUAL_PASS) return;
            }

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction == SignalHead.SIGFN.NORMAL)
                    sigHead.Update();
            }

            // next, update all other heads

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction != SignalHead.SIGFN.NORMAL)
                    sigHead.Update();
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
        // TranslateTMAspect : Gets the display aspect for the track monitor.
        //

        public TrackMonitorSignalAspect TranslateTMAspect(SignalHead.SIGASP SigState)
        {
            switch (SigState)
            {
                case SignalHead.SIGASP.STOP:
                    if (hasPermission == PERMISSION.GRANTED)
                        return TrackMonitorSignalAspect.Permission;
                    else
                        return TrackMonitorSignalAspect.Stop;
                case SignalHead.SIGASP.STOP_AND_PROCEED:
                    return TrackMonitorSignalAspect.StopAndProceed;
                case SignalHead.SIGASP.RESTRICTING:
                    return TrackMonitorSignalAspect.Restricted;
                case SignalHead.SIGASP.APPROACH_1:
                    return TrackMonitorSignalAspect.Approach_1;
                case SignalHead.SIGASP.APPROACH_2:
                    return TrackMonitorSignalAspect.Approach_2;
                case SignalHead.SIGASP.APPROACH_3:
                    return TrackMonitorSignalAspect.Approach_3;
                case SignalHead.SIGASP.CLEAR_1:
                    return TrackMonitorSignalAspect.Clear_1;
                case SignalHead.SIGASP.CLEAR_2:
                    return TrackMonitorSignalAspect.Clear_2;
                default:
                    return TrackMonitorSignalAspect.None;
            }
        } // GetMonitorAspect

        //================================================================================================//
        //
        // request to clear signal in explorer mode
        //

        public Train.TCSubpathRoute requestClearSignalExplorer(Train.TCSubpathRoute thisRoute,
            float reqDistance, Train.TrainRouted thisTrain, bool propagated, int signalNumClearAhead)
        {
            // build output route from input route
            Train.TCSubpathRoute newRoute = new Train.TCSubpathRoute(thisRoute);

            // if signal has fixed route, use that else build route
            if (fixedRoute != null && fixedRoute.Count > 0)
            {
                signalRoute = new Train.TCSubpathRoute(fixedRoute);
            }

            // build route from signal, upto next signal or max distance, take into account manual switch settings
            else
            {
                List<int> nextRoute = signalRef.ScanRoute(thisTrain.Train, TCNextTC, 0.0f, TCNextDirection, true, reqDistance, true, true, true, false,
                true, false, false, false, false, thisTrain.Train.IsFreight);

                signalRoute = new Train.TCSubpathRoute();

                foreach (int sectionIndex in nextRoute)
                {
                    Train.TCRouteElement thisElement = new Train.TCRouteElement(Math.Abs(sectionIndex), sectionIndex >= 0 ? 0 : 1);
                    signalRoute.Add(thisElement);
                }
            }

            // set full route if route ends with signal
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[signalRoute[signalRoute.Count - 1].TCSectionIndex];
            int lastDirection = signalRoute[signalRoute.Count - 1].Direction;

            if (lastSection.EndSignals[lastDirection] != null)
            {
                fullRoute = true;
                sigfound[(int)SignalHead.SIGFN.NORMAL] = lastSection.EndSignals[lastDirection].thisRef;
            }

            // try and clear signal

            enabledTrain = thisTrain;
            checkRouteState(propagated, signalRoute, thisTrain);

            // extend route if block is clear or permission is granted, even if signal is not cleared (signal state may depend on next signal)
            bool extendRoute = false;
            if (this_sig_lr(SignalHead.SIGFN.NORMAL) > SignalHead.SIGASP.STOP) extendRoute = true;
            if (internalBlockState <= INTERNAL_BLOCKSTATE.RESERVABLE) extendRoute = true;

            // if signal is cleared or permission is granted, extend route with signal route

            if (extendRoute || hasPermission == PERMISSION.GRANTED)
            {
                foreach (Train.TCRouteElement thisElement in signalRoute)
                {
                    newRoute.Add(thisElement);
                }
            }

            // if signal is cleared, propagate request if required
            if (extendRoute && fullRoute)
            {
                isPropagated = propagated;
                int ReqNumClearAhead = 0;

                if (SignalNumClearAhead_MSTS > -2)
                {
                    ReqNumClearAhead = propagated ?
                        signalNumClearAhead - SignalNumNormalHeads : SignalNumClearAhead_MSTS - SignalNumNormalHeads;
                }
                else
                {
                    if (SignalNumClearAhead_ORTS == -1)
                    {
                        ReqNumClearAhead = propagated ? signalNumClearAhead : 1;
                    }
                    else if (SignalNumClearAhead_ORTS == 0)
                    {
                        ReqNumClearAhead = 0;
                    }
                    else
                    {
                        ReqNumClearAhead = isPropagated ? signalNumClearAhead - 1 : SignalNumClearAhead_ORTS - 1;
                    }
                }


                if (ReqNumClearAhead > 0)
                {
                    int nextSignalIndex = sigfound[(int) SignalHead.SIGFN.NORMAL];
                    if (nextSignalIndex >= 0)
                    {
                        SignalObject nextSignal = signalObjects[nextSignalIndex];
                        newRoute = nextSignal.requestClearSignalExplorer(newRoute, thisTrain.Train.minCheckDistanceM, thisTrain, true, ReqNumClearAhead);
                    }
                }
            }

            return (newRoute);
        }

        //================================================================================================//
        //
        // request to clear signal
        //

        public void requestClearSignal(Train.TCSubpathRoute RoutePart, Train.TrainRouted thisTrain,
                        int clearNextSignals, bool requestIsPropagated, SignalObject lastSignal)
        {

#if DEBUG_REPORTS
            String report = "Request for clear signal from train ";
            report = String.Concat(report, thisTrain.Train.Number.ToString());
            report = String.Concat(report, " at section ", thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex.ToString());
            report = String.Concat(report, " for signal ", thisRef.ToString());
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (thisTrain.Train.CheckTrain)
            {
                String reportCT = "Request for clear signal from train ";
                reportCT = String.Concat(reportCT, thisTrain.Train.Number.ToString());
                reportCT = String.Concat(reportCT, " at section ", thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex.ToString());
                reportCT = String.Concat(reportCT, " for signal ", thisRef.ToString());
                File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
            }

            int procstate = 0;
            int foundFirstSection = -1;
            int foundLastSection = -1;
            SignalObject nextSignal = null;

            isPropagated = requestIsPropagated;
            propagated = false;   // always pass on request

            // check if signal not yet enabled - if it is, give warning and quit

            if (enabledTrain != null && enabledTrain != thisTrain)
            {
                Trace.TraceWarning("Request to clear signal {0} from train {1}, signal already enabled for train {2}",
                                       thisRef, thisTrain.Train.Number, enabledTrain.Train.Number);
                thisTrain.Train.ControlMode = Train.TRAIN_CONTROL.AUTO_NODE; // keep train in NODE control mode
                procstate = -1;
                return;
            }
            else
            {
                if (enabledTrain != thisTrain) // new allocation - reset next signals
                {
                    for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
                    {
                        sigfound[fntype] = defaultNextSignal[fntype];
                    }
                }
                enabledTrain = thisTrain;
            }

            // find section in route part which follows signal

            if (procstate == 0)
            {
                signalRoute.Clear();

                int firstIndex = -1;
                if (lastSignal != null)
                {
                    firstIndex = lastSignal.thisTrainRouteIndex;
                }
                if (firstIndex < 0)
                {
                    firstIndex = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].RouteListIndex;
                }

                if (firstIndex >= 0)
                {
                    for (int iNode = firstIndex;
                             iNode < RoutePart.Count && foundFirstSection < 0;
                             iNode++)
                    {
                        Train.TCRouteElement thisElement = RoutePart[iNode];
                        if (thisElement.TCSectionIndex == TCNextTC)
                        {
                            foundFirstSection = iNode;
                            thisTrainRouteIndex = iNode;
                        }
                    }
                }

                if (foundFirstSection < 0)
                {
                    // no route from this signal - reset enable and exit
                    enabledTrain = null;

                    // if signal on holding list, set hold state
                    if (thisTrain.Train.HoldingSignals.Contains(thisRef) && holdState == HOLDSTATE.NONE) holdState = HOLDSTATE.STATION_STOP;
                    return;
                }
            }

            // copy sections upto next normal signal
            // check for loop


            if (procstate == 0)
            {
                List<int> sectionsInRoute = new List<int>();

                for (int iNode = foundFirstSection; iNode < RoutePart.Count && foundLastSection < 0; iNode++)
                {
                    Train.TCRouteElement thisElement = RoutePart[iNode];
                    if (sectionsInRoute.Contains(thisElement.TCSectionIndex))
                    {
                        foundLastSection = iNode;  // loop
                    }
                    else
                    {
                        signalRoute.Add(thisElement);
                        sectionsInRoute.Add(thisElement.TCSectionIndex);

                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        if (thisSection.EndSignals[thisElement.Direction] != null)
                        {
                            foundLastSection = iNode;
                            nextSignal = thisSection.EndSignals[thisElement.Direction];
                        }
                    }
                }
            }

            // check if end of track reached

            Train.TCRouteElement lastSignalElement = signalRoute[signalRoute.Count - 1];
            TrackCircuitSection lastSignalSection = signalRef.TrackCircuitList[lastSignalElement.TCSectionIndex];

            fullRoute = true;

            // if end of signal route is not a signal or end-of-track it is not a full route

            if (nextSignal == null && lastSignalSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
            {
                fullRoute = false;
            }

            // if next signal is found and relevant, set reference

            if (nextSignal != null)
            {
                sigfound[(int)SignalHead.SIGFN.NORMAL] = nextSignal.thisRef;
            }
            else
            {
                sigfound[(int)SignalHead.SIGFN.NORMAL] = -1;
            }

            // set number of signals to clear ahead

            if (SignalNumClearAhead_MSTS > -2)
            {
                ReqNumClearAhead = clearNextSignals > 0 ?
                    clearNextSignals - SignalNumNormalHeads : SignalNumClearAhead_MSTS - SignalNumNormalHeads;
            }
            else
            {
                if (SignalNumClearAhead_ORTS == -1)
                {
                    ReqNumClearAhead = clearNextSignals > 0 ? clearNextSignals : 1;
                }
                else if (SignalNumClearAhead_ORTS == 0)
                {
                    ReqNumClearAhead = 0;
                }
                else
                {
                    ReqNumClearAhead = clearNextSignals > 0 ? clearNextSignals - 1 : SignalNumClearAhead_ORTS - 1;
                }
            }

            // perform route check

            checkRouteState(isPropagated, signalRoute, thisTrain);

            // propagate request

            if (!isPropagated)
            {
                propagateRequest();
            }
        }

        //================================================================================================//
        //
        // check and update Route State
        //

        public void checkRouteState(bool isPropagated, Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain)
        {

            // check if signal must be hold

            bool signalHold = (holdState != HOLDSTATE.NONE);
            if (enabledTrain != null && enabledTrain.Train.HoldingSignals.Contains(thisRef) && holdState < HOLDSTATE.MANUAL_LOCK)
            {
                holdState = HOLDSTATE.STATION_STOP;
                signalHold = true;
            }
            else if (holdState == HOLDSTATE.STATION_STOP)
            {
                if (enabledTrain == null || !enabledTrain.Train.HoldingSignals.Contains(thisRef))
                {
                    holdState = HOLDSTATE.NONE;
                    signalHold = false;
                }
            }

            // test clearance for full route section

            if (!signalHold)
            {
                if (fullRoute)
                {
                    bool newroute = getBlockState(thisRoute, thisTrain);
                    if (newroute)
                        thisRoute = this.signalRoute;
                }

                // test clearance for sections in route only if first signal ahead of train

                else if (enabledTrain != null && !isPropagated)
                {
                    getPartBlockState(thisRoute);
                }
            }

            // else consider route blocked

            else
            {
                internalBlockState = INTERNAL_BLOCKSTATE.BLOCKED;
            }

            // derive signal state

            StateUpdate();
            SignalHead.SIGASP signalState = this_sig_lr(SignalHead.SIGFN.NORMAL);

            float lengthReserved = 0.0f;

            // check for permission

            if (internalBlockState == INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR && hasPermission == PERMISSION.REQUESTED && !isPropagated)
            {
                hasPermission = PERMISSION.GRANTED;
            }
            else if (enabledTrain != null && enabledTrain.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL && signalState == SignalHead.SIGASP.STOP &&
                internalBlockState <= INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR && hasPermission == PERMISSION.REQUESTED)
            {
                hasPermission = PERMISSION.GRANTED;
            }
            else if (MultiPlayer.MPManager.IsMultiPlayer() && enabledTrain != null && enabledTrain.Train.ControlMode == Train.TRAIN_CONTROL.EXPLORER 
                && signalState == SignalHead.SIGASP.STOP && internalBlockState <= INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR && hasPermission == PERMISSION.REQUESTED)
            {//added by JTang
                hasPermission = PERMISSION.GRANTED;
            }

            // reserve full section if allowed

            if (enabledTrain != null)
            {
                if (internalBlockState == INTERNAL_BLOCKSTATE.RESERVABLE)
                {
                    foreach (Train.TCRouteElement thisElement in thisRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (thisSection.CircuitState.TrainReserved == null)
                            thisSection.Reserve(enabledTrain, thisRoute);
                        enabledTrain.Train.LastReservedSection[enabledTrain.TrainRouteDirectionIndex] = thisElement.TCSectionIndex;
                        lengthReserved += thisSection.Length;
                    }

                    internalBlockState = INTERNAL_BLOCKSTATE.RESERVED;
                    enabledTrain.Train.ClaimState = false;
                }

            // reserve partial sections if signal clears on occupied track or permission is granted

                else if ((signalState > SignalHead.SIGASP.STOP || hasPermission == PERMISSION.GRANTED) &&
                internalBlockState != INTERNAL_BLOCKSTATE.RESERVED)
                {

                    // reserve upto available section

                    int lastSectionIndex = 0;
                    bool reservable = true;

                    for (int iSection = 0; iSection < thisRoute.Count && reservable; iSection++)
                    {
                        Train.TCRouteElement thisElement = thisRoute[iSection];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        if (thisSection.IsAvailable(enabledTrain))
                        {
                            if (thisSection.CircuitState.TrainReserved == null)
                            {
                                thisSection.Reserve(enabledTrain, thisRoute);
                            }
                            enabledTrain.Train.LastReservedSection[enabledTrain.TrainRouteDirectionIndex] = thisElement.TCSectionIndex;
                            lastSectionIndex = iSection;
                            lengthReserved += thisSection.Length;
                        }
                        else
                        {
                            reservable = false;
                        }
                    }

                    // set pre-reserved or reserved for all other sections

                    for (int iSection = lastSectionIndex++; iSection < thisRoute.Count; iSection++)
                    {
                        Train.TCRouteElement thisElement = thisRoute[iSection];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        if (thisSection.IsAvailable(enabledTrain) && thisSection.CircuitState.TrainReserved == null)
                        {
                            thisSection.Reserve(enabledTrain, thisRoute);
                        }
                        else if (thisSection.CircuitState.HasOtherTrainsOccupying(enabledTrain))
                        {
                            thisSection.PreReserve(enabledTrain);
                        }
                        else if (thisSection.CircuitState.TrainReserved == null || thisSection.CircuitState.TrainReserved.Train != enabledTrain.Train)
                        {
                            thisSection.PreReserve(enabledTrain);
                        }
                    }
                    enabledTrain.Train.ClaimState = false;
                }

            // if claim allowed - reserve free sections and claim all other if first signal ahead of train

                else if (enabledTrain.Train.ClaimState && internalBlockState != INTERNAL_BLOCKSTATE.RESERVED && !isPropagated)
                {
                    foreach (Train.TCRouteElement thisElement in thisRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (thisSection.IsAvailable(enabledTrain) && thisSection.CircuitState.TrainReserved == null)
                        {
                            thisSection.Reserve(enabledTrain, thisRoute);
                        }
                        else if (thisSection.CircuitState.TrainReserved == null || thisSection.CircuitState.TrainReserved.Train != enabledTrain.Train)
                        {
                            thisSection.Claim(enabledTrain);
                        }
                    }
                }
            }
        }

        //================================================================================================//
        //
        // propagate clearance request
        //

        private void propagateRequest()
        {
            // no. of next signals to clear : as passed on -1 if signal has normal clear ahead
            // if passed on < 0, use this signals num to clear

            SignalObject nextSignal = null;
            if (sigfound[(int)SignalHead.SIGFN.NORMAL] >= 0)
            {
                nextSignal = signalObjects[sigfound[(int)SignalHead.SIGFN.NORMAL]];
            }

            Train.TCSubpathRoute RoutePart;
            if (enabledTrain != null)
            {
                RoutePart = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex];   // if known which route to use
            }
            else
            {
                RoutePart = signalRoute; // else use signal route
            }

            bool propagateState = true;  // normal propagate state

            // if section is clear but signal remains at stop - dual signal situation - do not treat as propagate
            if (internalBlockState == INTERNAL_BLOCKSTATE.RESERVED && this_sig_lr(SignalHead.SIGFN.NORMAL) == SignalHead.SIGASP.STOP && isSignalNormal())
            {
                propagateState = false;
            }

            if (ReqNumClearAhead > 0 && nextSignal != null && internalBlockState == INTERNAL_BLOCKSTATE.RESERVED)
            {
                nextSignal.requestClearSignal(RoutePart, enabledTrain, ReqNumClearAhead, propagateState, this);
                propagated = true;
            }

        } //propagateRequest

        //================================================================================================//
        //
        // get block state - not routed
        // Check blockstate for normal signal which is not enabled
        // Check blockstate for other types of signals
        //

        private void getBlockState_notRouted()
        {

            INTERNAL_BLOCKSTATE localBlockState = INTERNAL_BLOCKSTATE.RESERVED; // preset to lowest option

            // check fixed route for normal signals

            if (isSignalNormal() && hasFixedRoute)
            {
                foreach (Train.TCRouteElement thisElement in fixedRoute)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        localBlockState = INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR;
                    }
                }
            }

        // otherwise follow sections upto first non-set switch or next signal
            else
            {
                int thisTC = TCReference;
                int direction = TCDirection;
                int nextTC = -1;

                // for normal signals : start at next TC

                if (TCNextTC > 0)
                {
                    thisTC = TCNextTC;
                    direction = TCNextDirection;
                }

                // get trackcircuit

                TrackCircuitSection thisSection = null;
                if (thisTC > 0)
                {
                    thisSection = signalRef.TrackCircuitList[thisTC];
                }

                // loop through valid sections

                while (thisSection != null)
                {

                    // set blockstate

                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        if (thisSection.Index == TCReference)  // for section where signal is placed, check if train is ahead
                        {
                            Dictionary<Train, float> trainAhead =
                                                    thisSection.TestTrainAhead(null, TCOffset, TCDirection);
                            if (trainAhead.Count > 0)
                                localBlockState = INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR;
                        }
                        else
                        {
                            localBlockState = INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR;
                        }
                    }

                    // if section has signal at end stop check

                    if (thisSection.EndSignals[direction] != null)
                    {
                        thisSection = null;
                    }

        // get next section if active link is set

                    else
                    {
                        //                     int pinIndex = direction == 0 ? 1 : 0;
                        int pinIndex = direction;
                        nextTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (nextTC == -1)
                        {
                            nextTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }

                        // set state to blocked if ending at unset or unaligned switch

                        if (nextTC >= 0)
                        {
                            thisSection = signalRef.TrackCircuitList[nextTC];
                        }
                        else
                        {
                            thisSection = null;
                            localBlockState = INTERNAL_BLOCKSTATE.BLOCKED;
                        }
                    }
                }
            }

            internalBlockState = localBlockState;
        }

        //================================================================================================//
        //
        // Get block state
        // Get internal state of full block for normal enabled signal upto next signal for clear request
        // returns true if train set to use alternative route
        //

        private bool getBlockState(Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain)
        {

            bool returnvalue = false;

            INTERNAL_BLOCKSTATE blockstate = INTERNAL_BLOCKSTATE.RESERVED;  // preset to lowest possible state //

            // loop through all sections in route list

            Train.TCRouteElement lastElement = null;

            foreach (Train.TCRouteElement thisElement in thisRoute)
            {
                lastElement = thisElement;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int direction = thisElement.Direction;
                blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, thisRoute);
                if (blockstate > INTERNAL_BLOCKSTATE.RESERVABLE)
                    break;           // break on first non-reservable section //

                // if alternative path from section available but train already waiting for deadlock, set blocked
                if (thisElement.StartAlternativePath != null)
                {
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];
                    if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                    {
                        blockstate = INTERNAL_BLOCKSTATE.BLOCKED;
                        lastElement = thisElement;
                        break;
                    }
                }
            }

            // check if alternative route available

            int lastElementIndex = thisRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);

            if (blockstate > INTERNAL_BLOCKSTATE.RESERVABLE && thisTrain != null)
            {
                int startAlternativeRoute = -1;
                int altRoute = -1;

                Train.TCSubpathRoute trainRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
                Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

                for (int iElement = lastElementIndex; iElement >= 0; iElement--)
                {
                    Train.TCRouteElement prevElement = thisRoute[iElement];
                    if (prevElement.StartAlternativePath != null)
                    {
                        startAlternativeRoute =
                            trainRoute.GetRouteIndex(thisRoute[iElement].TCSectionIndex, thisPosition.RouteListIndex);
                        altRoute = prevElement.StartAlternativePath[0];
                        break;
                    }
                }

                // check if alternative path may be used

                if (startAlternativeRoute > 0)
                {
                    Train.TCRouteElement startElement = trainRoute[startAlternativeRoute];
                    int endSectionIndex = startElement.StartAlternativePath[1];
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];
                    if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                    {
                        startAlternativeRoute = -1; // reset use of alternative route
                    }
                }

                // if available, select part of route upto next signal

                if (startAlternativeRoute > 0)
                {
                    Train.TCSubpathRoute altRoutePart = thisTrain.Train.ExtractAlternativeRoute(altRoute);

                    // check availability of alternative route

                    INTERNAL_BLOCKSTATE newblockstate = INTERNAL_BLOCKSTATE.RESERVABLE;

                    foreach (Train.TCRouteElement thisElement in altRoutePart)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        int direction = thisElement.Direction;
                        newblockstate = thisSection.getSectionState(enabledTrain, direction, newblockstate, thisRoute);
                        if (newblockstate > INTERNAL_BLOCKSTATE.RESERVABLE)
                            break;           // break on first non-reservable section //
                    }

                    // if available, use alternative route

                    if (newblockstate <= INTERNAL_BLOCKSTATE.RESERVABLE)
                    {
                        blockstate = newblockstate;
                        thisTrain.Train.SetAlternativeRoute(startAlternativeRoute, altRoute, this);
                        returnvalue = true;
                    }
                }
            }

            // check if approaching deadlock part, and if alternative route must be taken - if point where alt route start is not yet reserved
            // alternative route may not be taken if there is a train already waiting for the deadlock
            else if (thisTrain != null)
            {
                int startAlternativeRoute = -1;
                int altRoute = -1;
                TrackCircuitSection startSection = null;
                TrackCircuitSection endSection = null;

                Train.TCSubpathRoute trainRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
                Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

                for (int iElement = lastElementIndex; iElement >= 0; iElement--)
                {
                    Train.TCRouteElement prevElement = thisRoute[iElement];
                    if (prevElement.StartAlternativePath != null)
                    {
                        endSection = signalRef.TrackCircuitList[prevElement.StartAlternativePath[1]];
                        if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                        {
                            altRoute = prevElement.StartAlternativePath[0];
                            startAlternativeRoute =
                                trainRoute.GetRouteIndex(prevElement.TCSectionIndex, thisPosition.RouteListIndex);
                            startSection = signalRef.TrackCircuitList[prevElement.TCSectionIndex];
                        }
                        break;
                    }
                }

                // use alternative route

                if (startAlternativeRoute > 0 && 
                    (startSection.CircuitState.TrainReserved == null || startSection.CircuitState.TrainReserved.Train != thisTrain.Train))
                {
                    Train.TCSubpathRoute altRoutePart = thisTrain.Train.ExtractAlternativeRoute(altRoute);

                    // check availability of alternative route

                    INTERNAL_BLOCKSTATE newblockstate = INTERNAL_BLOCKSTATE.RESERVABLE;

                    foreach (Train.TCRouteElement thisElement in altRoutePart)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        int direction = thisElement.Direction;
                        newblockstate = thisSection.getSectionState(enabledTrain, direction, newblockstate, thisRoute);
                        if (newblockstate > INTERNAL_BLOCKSTATE.RESERVABLE)
                            break;           // break on first non-reservable section //
                    }

                    // if available, use alternative route

                    if (newblockstate <= INTERNAL_BLOCKSTATE.RESERVABLE)
                    {
                        blockstate = newblockstate;
                        thisTrain.Train.SetAlternativeRoute(startAlternativeRoute, altRoute, this);
                        if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.DeadlockAwaited.Contains(thisTrain.Train.Number))
                            endSection.DeadlockAwaited.Add(thisTrain.Train.Number);
                        returnvalue = true;

                    }
                }
            }

            internalBlockState = blockstate;
            return (returnvalue);
        }

        //================================================================================================//
        //
        // Get part block state
        // Get internal state of part of block for normal enabled signal upto next signal for clear request
        // if there are no switches before next signal or end of track, treat as full block
        //

        private void getPartBlockState(Train.TCSubpathRoute thisRoute)
        {

            // check beyond last section for next signal or end of track 

            int listIndex = (thisRoute.Count > 0) ? (thisRoute.Count - 1) : thisTrainRouteIndex;

            Train.TCRouteElement lastElement = thisRoute[listIndex];
            int thisSectionIndex = lastElement.TCSectionIndex;
            int direction = lastElement.Direction;

            Train.TCSubpathRoute additionalElements = new Train.TCSubpathRoute();

            bool end_of_info = false;

            while (!end_of_info)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

                TrackCircuitSection.CIRCUITTYPE thisType = thisSection.CircuitType;

                switch (thisType)
                {
                    case (TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK):
                        end_of_info = true;
                        break;

                    case (TrackCircuitSection.CIRCUITTYPE.JUNCTION):
                    case (TrackCircuitSection.CIRCUITTYPE.CROSSOVER):
                        end_of_info = true;
                        break;

                    default:
                        Train.TCRouteElement newElement = new Train.TCRouteElement(thisSectionIndex, direction);
                        additionalElements.Add(newElement);

                        if (thisSection.EndSignals[direction] != null)
                        {
                            end_of_info = true;
                        }
                        break;
                }

                if (!end_of_info)
                {
                    thisSectionIndex = thisSection.Pins[direction, 0].Link;
                    direction = thisSection.Pins[direction, 0].Direction;
                }
            }

            INTERNAL_BLOCKSTATE blockstate = INTERNAL_BLOCKSTATE.RESERVED;  // preset to lowest possible state //

            int lastSectionIndex = -1;

            // check all elements in original route

            foreach (Train.TCRouteElement thisElement in thisRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                direction = thisElement.Direction;
                blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, thisRoute);
                if (blockstate > INTERNAL_BLOCKSTATE.RESERVABLE)
                    break;           // break on first non-reservable section //
                lastSectionIndex = thisSection.Index;
            }

            // check all additional elements upto signal, junction or end-of-track

            if (blockstate <= INTERNAL_BLOCKSTATE.RESERVABLE)
            {
                foreach (Train.TCRouteElement thisElement in additionalElements)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    direction = thisElement.Direction;
                    blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, additionalElements);
                    if (blockstate > INTERNAL_BLOCKSTATE.RESERVABLE)
                        break;           // break on first non-reservable section //
                }
            }

            //          if (blockstate <= INTERNAL_BLOCKSTATE.RESERVABLE && end_at_junction)
            //          {
            //              blockstate = INTERNAL_BLOCKSTATE.OCCUPIED_SAMEDIR;  // set restricted state
            //          }

            internalBlockState = blockstate;

        }

        //================================================================================================//
        //
        // Set signal default route and next signal list as switch in route is reset
        // Used in manual mode for signals which clear by default
        //

        public void SetDefaultRoute()
        {
            signalRoute = new Train.TCSubpathRoute(fixedRoute);
            for (int iSigtype = 0; iSigtype <= defaultNextSignal.Length - 1; iSigtype++)
            {
                sigfound[iSigtype] = defaultNextSignal[iSigtype];
            }
        }

        //================================================================================================//
        //
        // Reset signal and clear all train sections
        //

        public void ResetSignal(bool propagateReset)
        {
            Train.TrainRouted thisTrain = enabledTrain;

            // search for last signal enabled for this train, start reset from there //

            SignalObject thisSignal = this;
            List<SignalObject> passedSignals = new List<SignalObject>();
            int thisSignalIndex = thisSignal.thisRef;

            if (propagateReset)
            {
                while (thisSignalIndex >= 0 && thisSignal.enabledTrain == thisTrain)
                {
                    thisSignal = signalObjects[thisSignalIndex];
                    passedSignals.Add(thisSignal);
                    thisSignalIndex = thisSignal.sigfound[(int)SignalHead.SIGFN.NORMAL];
                }
            }
            else
            {
                passedSignals.Add(thisSignal);
            }

            foreach (SignalObject nextSignal in passedSignals)
            {
                if (nextSignal.signalRoute != null)
                {
                    List<TrackCircuitSection> sectionsToClear = new List<TrackCircuitSection>();
                    foreach (Train.TCRouteElement thisElement in nextSignal.signalRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        sectionsToClear.Add(thisSection);  // store in list as signalRoute is lost during remove action
                    }
                    foreach (TrackCircuitSection thisSection in sectionsToClear)
                    {
                        thisSection.RemoveTrain(thisTrain, false);
                    }
                }

                nextSignal.resetSignalEnabled();
            }

        }

        //================================================================================================//
        //
        // Reset signal route and next signal list as switch in route is reset
        //

        public void ResetRoute(int resetSectionIndex)
        {

            // remove this signal from any other junctions

            foreach (int thisSectionIndex in JunctionsPassed)
            {
                if (thisSectionIndex != resetSectionIndex)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                    thisSection.SignalsPassingRoutes.Remove(thisRef);
                }
            }

            JunctionsPassed.Clear();

            for (int fntype = 0; fntype < (int)SignalHead.SIGFN.UNKNOWN; fntype++)
            {
                sigfound[fntype] = defaultNextSignal[fntype];
            }

            // if signal is enabled, ensure next normal signal is reset

            if (enabledTrain != null && sigfound[(int)SignalHead.SIGFN.NORMAL] < 0)
            {
                sigfound[(int)SignalHead.SIGFN.NORMAL] = SONextSignalNormal(TCNextTC);
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt", "Signal " + thisRef.ToString() + " reset on Junction Change\n");

            if (enabledTrain != null)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " + enabledTrain.Train.Number.ToString() + " affected; " +
                        "new NORMAL signal : " + sigfound[(int)SignalHead.SIGFN.NORMAL].ToString() + "\n");
            }
#endif
            if (enabledTrain != null && enabledTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Signal " + thisRef.ToString() + " reset on Junction Change\n");

                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + enabledTrain.Train.Number.ToString() + " affected; " +
                        "new NORMAL signal : " + sigfound[(int)SignalHead.SIGFN.NORMAL].ToString() + "\n");
            }
        }

        //================================================================================================//
        //
        // Set HOLD state for dispatcher control
        //
        // Parameter : bool, if set signal must be reset if set (and train position allows)
        //
        // Returned : bool[], dimension 2,
        //            field [0] : if true, hold state is set
        //            field [1] : if true, signal is reset (always returns false if reset not requested)
        //

        public bool[] requestHoldSignalDispatcher(bool requestResetSignal)
        {
            bool[] returnValue = new bool[2] { false, false };
            SignalHead.SIGASP thisAspect = this_sig_lr(SignalHead.SIGFN.NORMAL);

            // signal not enabled - set lock, reset if cleared (auto signal can clear without enabling)

            if (enabledTrain == null || enabledTrain.Train == null)
            {
                holdState = HOLDSTATE.MANUAL_LOCK;
                if (thisAspect > SignalHead.SIGASP.STOP) ResetSignal(true);
                returnValue[0] = true;
            }

            // if enabled, cleared and reset not requested : no action

            else if (!requestResetSignal && thisAspect > SignalHead.SIGASP.STOP)
            {
                holdState = HOLDSTATE.MANUAL_LOCK; //just in case this one later will be set to green by the system
                returnValue[0] = true;
            }

            // if enabled and not cleared : set hold, no reset required

            else if (thisAspect == SignalHead.SIGASP.STOP)
            {
                holdState = HOLDSTATE.MANUAL_LOCK;
                returnValue[0] = true;
            }

            // enabled, cleared , reset required : check train speed
            // if train is moving : no action
            //temporarily removed by JTang, before the full revision is ready
//          else if (Math.Abs(enabledTrain.Train.SpeedMpS) > 0.1f)
//          {
//          }

            // if train is stopped : reset signal, breakdown train route, set holdstate

            else
            {
                int signalRouteIndex = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex].GetRouteIndex(TCNextTC, 0);
                if (signalRouteIndex >= 0)
                {
                    signalRef.BreakDownRoute(TCNextTC, enabledTrain);
                    ResetSignal(true);
                    holdState = HOLDSTATE.MANUAL_LOCK;
                    returnValue[0] = true;
                    returnValue[1] = true;
                }
                else //hopefully this does not happen
                {
                    holdState = HOLDSTATE.MANUAL_LOCK;
                    returnValue[0] = true;
                }
            }

            return (returnValue);
        }

        //================================================================================================//
        //
        // Reset HOLD state for dispatcher control
        //
        // Parameter : none
        //
        // Returned : void
        //

        public void clearHoldSignalDispatcher()
        {
            holdState = HOLDSTATE.NONE;
        }

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

        public SIGFN sigFunction
        {
            get
            {
                if (signalType != null)
                    return (SIGFN)signalType.FnType;
                else
                    return SIGFN.UNKNOWN;
            }
        }

        public String SignalTypeName
        {
            get
            {
                if (signalType != null)
                    return signalType.Name;
                else
                    return "";
            }
        }

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
            JunctionMainNode = 0;

            if (sigItem.noSigDirs > 0)
            {
                TrackJunctionNode = sigItem.TrSignalDirs[0].TrackNode;
                JunctionPath = sigItem.TrSignalDirs[0].linkLRPath;
            }

            Array sigasp_values = SIGASP.GetValues(typeof(SIGASP));
            speed_info = new ObjectSpeedInfo[sigasp_values.Length];
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
            state = SIGASP.CLEAR_2;
            signalType = new SignalType(SignalType.FnTypes.Speed, SIGASP.CLEAR_2);

            TrackJunctionNode = 0;
            JunctionMainNode = 0;

            Array sigasp_values = SIGASP.GetValues(typeof(SIGASP));
            speed_info = new ObjectSpeedInfo[sigasp_values.Length];

            float speedMpS = MpS.ToMpS(speedItem.SpeedInd, !speedItem.IsMPH);
            if (speedItem.IsResume)
                speedMpS = 999f;

            float passSpeed = speedItem.IsPassenger ? speedMpS : -1;
            float freightSpeed = speedItem.IsFreight ? speedMpS : -1;
            ObjectSpeedInfo speedinfo = new ObjectSpeedInfo(passSpeed, freightSpeed, false);
            speed_info[Convert.ToInt32(state)] = speedinfo;
        }

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

                if (sigFunction == SIGFN.NORMAL)
                {
                    mainSignal.SignalNumClearAhead_MSTS = Math.Max(mainSignal.SignalNumClearAhead_MSTS, signalType.NumClearAhead_MSTS);
                    mainSignal.SignalNumClearAhead_ORTS = Math.Max(mainSignal.SignalNumClearAhead_ORTS, signalType.NumClearAhead_ORTS);
                }
            }
            else
            {
                Trace.TraceWarning("SignalObject trItem={0}, trackNode={1} has SignalHead with undefined SignalType {2}.",
                                  mainSignal.trItem, mainSignal.trackNode, sigItem.SignalType);
            }


        }//SetSignalType

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

        public SIGASP this_sig_lr(SIGFN sigFN, ref bool sigfound)
        {
            return mainSignal.this_sig_lr(sigFN, ref sigfound);
        }

        public SIGASP this_sig_mr(SIGFN sigFN)
        {
            return mainSignal.this_sig_mr(sigFN);
        }

        public SIGASP this_sig_mr(SIGFN sigFN, ref bool sigfound)
        {
            return mainSignal.this_sig_mr(sigFN, ref sigfound);
        }

        public SIGASP opp_sig_mr(SIGFN sigFN)
        {
            return mainSignal.opp_sig_mr(sigFN);
        }

        public SIGASP opp_sig_mr(SIGFN sigFN, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_mr(sigFN, ref signalFound);
        }

        public SIGASP opp_sig_lr(SIGFN sigFN)
        {
            return mainSignal.opp_sig_lr(sigFN);
        }

        public SIGASP opp_sig_lr(SIGFN sigFN, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_lr(sigFN, ref signalFound);
        }

        //================================================================================================//
        //
        //  dist_multi_sig_mr : Returns most restrictive state of signal type A, for all type A upto type B
        //  
        //

        public SIGASP dist_multi_sig_mr(SIGFN sigFN1, SIGFN sigFN2, string dumpfile)
        {
            SIGASP foundState = SIGASP.CLEAR_2;
            bool foundValid = false;

            if (dumpfile.Length > 1)
                File.AppendAllText(dumpfile, "DIST_MULTI_SIG_MR for " + sigFN1.ToString() + " + upto " + sigFN2.ToString() + "\n");

            int sig2Index = mainSignal.sigfound[(int)sigFN2];
            if (sig2Index < 0)           // try renewed search with full route
            {
                sig2Index = mainSignal.SONextSignal(sigFN2);
                mainSignal.sigfound[(int)sigFN2] = sig2Index;
            }

            if (dumpfile.Length > 1)
            {
                if (sig2Index < 0)
                    File.AppendAllText(dumpfile, "  no signal type 2 found\n");
            }

            if (dumpfile.Length > 1)
                File.AppendAllText(dumpfile, "  signal type 2 : " + mainSignal.sigfound[(int)sigFN2].ToString() + "\n");
            SignalObject thisSignal = mainSignal;

            while (thisSignal.sigfound[(int)sigFN1] >= 0)
            {
                foundValid = true;
                thisSignal = thisSignal.signalRef.SignalObjects[thisSignal.sigfound[(int)sigFN1]];

                SIGASP thisState = thisSignal.this_sig_mr(sigFN1);
                foundState = foundState < thisState ? foundState : thisState;

                if (dumpfile.Length > 1)
                    File.AppendAllText(dumpfile, "  signal type 1 : " + thisSignal.thisRef.ToString() + " = " + thisState.ToString() + "\n");

                if (sig2Index >= 0 && thisSignal.sigfound[(int)sigFN2] != sig2Index)  // we are beyond type 2 signal
                {
                    return (foundState);
                }
            }

            return (foundValid ? foundState : SIGASP.STOP);   // no type 2 or running out of signals before finding type 2
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
            if (signalType != null)
                return signalType.def_draw_state(state);
            else
                return -1;
        }//def_draw_state

        //================================================================================================//
        //
        //  SetMostRestrictiveAspect : Sets the state to the most restrictive aspect for this head.
        //

        public void SetMostRestrictiveAspect()
        {
            if (signalType != null)
                state = signalType.GetMostRestrictiveAspect();
            else
                state = SignalHead.SIGASP.STOP;

            draw_state = def_draw_state(state);
        }//SetMostRestrictiveAspect

        //================================================================================================//
        //
        //  SetLeastRestrictiveAspect : Sets the state to the least restrictive aspect for this head.
        //

        public void SetLeastRestrictiveAspect()
        {
            if (signalType != null)
                state = signalType.GetLeastRestrictiveAspect();
            else
                state = SignalHead.SIGASP.CLEAR_2;
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
                juncfound = mainSignal.route_set(JunctionMainNode, TrackJunctionNode);
            }
                //added by JTang
            else if (MultiPlayer.MPManager.IsMultiPlayer())
            {
                var node = mainSignal.signalRef.trackDB.TrackNodes[mainSignal.trackNode];
                if (node.TrJunctionNode == null && node.TrPins != null && mainSignal.TCDirection < node.TrPins.Length)
                {
                    node = mainSignal.signalRef.trackDB.TrackNodes[node.TrPins[mainSignal.TCDirection].Link];
                    if (node.TrJunctionNode == null) return 0;
                    for (var pin = node.Inpins; pin < node.Inpins + node.Outpins; pin++)
                    {
                        if (node.TrPins[pin].Link == mainSignal.trackNode && pin - node.Inpins != node.TrJunctionNode.SelectedRoute)
                        {
                            juncfound = false;
                            break;
                        }
                    }
                }
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
            SIGSCRfile.SH_update(this, Signals.scrfile);
        }
    } //Update


    //================================================================================================//
    //
    // class SignalRefObject
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
            HeadIndex = HeadItemIn;
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
        public Dictionary<uint, uint> HeadReference;     // key=TDBIndex, value=headindex
        public bool[] HeadsSet;                          // Flags heads which are set
        public bool[] FlagsSet;                          // Flags signal-flags which are set
        public bool[] FlagsSetBackfacing;                // Flags signal-flags which are set
        //    for backfacing signal
        public List<int> Backfacing = new List<int>();   // Flags heads which are backfacing

        //================================================================================================//
        //
        // Constructor
        //

        public SignalWorldObject(MSTS.SignalObj SignalWorldItem, SIGCFGFile sigcfg)
        {
            MSTS.SignalShape thisCFGShape;

            HeadReference = new Dictionary<uint, uint>();

            // set flags with length to number of possible SubObjects type

            FlagsSet = new bool[MSTS.SignalShape.SignalSubObj.SignalSubTypes.Count];
            FlagsSetBackfacing = new bool[MSTS.SignalShape.SignalSubObj.SignalSubTypes.Count];
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

                HeadsSet = new bool[thisCFGShape.SignalSubObjs.Count];

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
                    uint HeadRef = Convert.ToUInt32(signalUnitInfo.SubObj);
                    HeadReference.Add(TrItemRef, HeadRef);
                }
            }
            else
            {
                Trace.TraceWarning("Signal not found : {0} n", SFileName);
            }

        }


        //================================================================================================//
        //
        // Constructor for copy
        //

        public SignalWorldObject(SignalWorldObject copy)
        {
            SFileName = String.Copy(copy.SFileName);
            Backfacing = copy.Backfacing;

            HeadsSet = new bool[copy.HeadsSet.Length];
            FlagsSet = new bool[copy.FlagsSet.Length];
            FlagsSetBackfacing = new bool[copy.FlagsSet.Length];
            copy.HeadsSet.CopyTo(HeadsSet, 0);
            copy.FlagsSet.CopyTo(FlagsSet, 0);
            copy.FlagsSetBackfacing.CopyTo(FlagsSet, 0);

            HeadReference = new Dictionary<uint, uint>();
            foreach (KeyValuePair<uint, uint> thisRef in copy.HeadReference)
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
            END_OF_AUTHORITY = -5,
            END_OF_PATH = -6,
        }

        public ObjectItemType ObjectType;                     // type information
        public ObjectItemFindState ObjectState;               // state information

        public SignalObject ObjectDetails;                    // actual object 

        public float distance_found;
        public float distance_to_train;
        public float distance_to_object;

        public SignalHead.SIGASP signal_state;                   // UNKNOWN if type = speedlimit
        // set active by TRAIN
        public float speed_passenger;                // -1 if not set
        public float speed_freight;                  // -1 if not set
        public uint speed_flag;
        public float actual_speed;                   // set active by TRAIN

        public bool processed;                       // for AI trains, set active by TRAIN

        //================================================================================================//
        //
        // Constructor
        //

        public ObjectItemInfo(SignalObject thisObject, float distance)
        {
            ObjectSpeedInfo speed_info;
            ObjectState = ObjectItemFindState.OBJECT_FOUND;

            distance_found = distance;

            ObjectDetails = thisObject;

            if (thisObject.isSignal)
            {
                ObjectType = ObjectItemType.SIGNAL;
                signal_state = SignalHead.SIGASP.UNKNOWN;  // set active by TRAIN
                speed_passenger = -1;                      // set active by TRAIN
                speed_freight = -1;                      // set active by TRAIN
                speed_flag = 0;                       // set active by TRAIN
            }
            else
            {
                ObjectType = ObjectItemType.SPEEDLIMIT;
                signal_state = SignalHead.SIGASP.UNKNOWN;
                speed_info = thisObject.this_lim_speed(SignalHead.SIGFN.SPEED);
                speed_passenger = speed_info.speed_pass;
                speed_freight = speed_info.speed_freight;
                speed_flag = speed_info.speed_flag;
            }
        }



        public ObjectItemInfo(ObjectItemFindState thisState)
        {
            ObjectState = thisState;
        }

    }

    //================================================================================================//
    //
    // class ObjectSpeedInfo
    //
    //================================================================================================//

    public class ObjectSpeedInfo
    {

        public float speed_pass;
        public float speed_freight;
        public uint speed_flag;

        //================================================================================================//
        //
        // Constructor
        //

        public ObjectSpeedInfo(float pass, float freight, bool asap)
        {
            speed_pass = pass;
            speed_freight = freight;
            if (asap)
            {
                speed_flag = 1;
            }
        }
    }

    //================================================================================================//
    //
    // Class Platform Details
    //
    //================================================================================================//

    public class PlatformDetails
    {
        public List<int> TCSectionIndex = new List<int>();
        public int[] PlatformReference = new int[2];
        public float[,] TCOffset = new float[2, 2];
        public float[] nodeOffset = new float[2];
        public float Length;
        public int[] EndSignals = new int[2] { -1, -1 };
        public float[] DistanceToSignals = new float[2];
        public string Name;
        public uint MinWaitingTime;

        //================================================================================================//
        //
        // Constructor
        //

        public PlatformDetails(int platformReference)
        {
            PlatformReference[0] = platformReference;
        }

        //================================================================================================//
        //
        // Constructor for copy
        //

        public PlatformDetails(PlatformDetails orgDetails)
        {
            foreach (int sectionIndex in orgDetails.TCSectionIndex)
            {
                TCSectionIndex.Add(sectionIndex);
            }

            orgDetails.PlatformReference.CopyTo(PlatformReference,0);
            TCOffset[0, 0] = orgDetails.TCOffset[0, 0];
            TCOffset[0, 1] = orgDetails.TCOffset[0, 1];
            TCOffset[1, 0] = orgDetails.TCOffset[1, 0];
            TCOffset[1, 1] = orgDetails.TCOffset[1, 1];
            orgDetails.nodeOffset.CopyTo(nodeOffset,0);
            Length = orgDetails.Length;
            orgDetails.EndSignals.CopyTo(EndSignals,0);
            orgDetails.DistanceToSignals.CopyTo(DistanceToSignals,0);
            Name = String.Copy(orgDetails.Name);
            MinWaitingTime = orgDetails.MinWaitingTime;
        }
    }

    //================================================================================================//

}

