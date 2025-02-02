// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Common;

namespace Orts.Formats.OR
{

    //  All these class definition are there onbly for documentation

    //================================================================================================//
    //
    // class AESignals
    //
    //================================================================================================//

    public class AESignals
    {

        //================================================================================================//
        // local data
        //================================================================================================//

        public TrackDB trackDB;
        private TrackSectionsFile tsectiondat;
        private TrackDatabaseFile tdbfile;

        private AESignalObject[] signalObjects;
        private List<AESignalWorldObject> SignalWorldList = new List<AESignalWorldObject>();
        //private Dictionary<uint, SignalRefObject> SignalRefList;
        private Dictionary<uint, AESignalObject> SignalHeadList;
        //public static SIGSCRfile scrfile;
        public readonly IDictionary<string, SignalFunction> SignalFunctions;

        public int noSignals;
        private int foundSignals;

        //private static int updatecount;

        //public List<TrackCircuitSection> TrackCircuitList;
        //private Dictionary<int, CrossOverItem> CrossoverList = new Dictionary<int, CrossOverItem>();
        //public List<PlatformDetails> PlatformDetailsList = new List<PlatformDetails>();
        //public Dictionary<int, int> PlatformXRefList = new Dictionary<int, int>();

        public int thisRef;                     // This signal's reference.

        //================================================================================================//
        ///
        /// Constructor
        ///

        public AESignals(MSTSData data, SignalConfigurationFile sigcfg)
        {

#if DEBUG_REPORTS
            File.Delete(@"C:\temp\printproc.txt");
#endif

            //SignalRefList = new Dictionary<uint, SignalRefObject>();
            SignalHeadList = new Dictionary<uint, AESignalObject>();
            Dictionary<int, int> platformList = new Dictionary<int, int>();

            SignalFunctions = sigcfg.SignalFunctions;

            trackDB = data.TDB.TrackDB;
            tsectiondat = data.TSectionDat;
            tdbfile = data.TDB;

            // read SIGSCR files

            //scrfile = new SIGSCRfile(data.RoutePath, sigcfg.ScriptFiles, sigcfg.SignalTypes);

            // build list of signal world file information

            BuildSignalWorld(data, sigcfg);

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

            //
            // Create trackcircuit database
            //

            //CreateTrackCircuits(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat);

            //
            // Process platform information
            //

            //ProcessPlatforms(platformList, trackDB.TrItemTable, trackDB.TrackNodes);

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

			var sob = new StringBuilder();
            for (var isignal = 0; isignal < signalObjects.Length - 1; isignal++)
            {
				var singleSignal = signalObjects[isignal];
                if (singleSignal == null)
                {
					sob.AppendFormat("\nInvalid entry : {0}\n", isignal);
                }
                else
                {
					sob.AppendFormat("\nSignal ref item     : {0}\n", singleSignal.thisRef);
					sob.AppendFormat("Track node + index  : {0} + {1}\n", singleSignal.trackNode, singleSignal.trRefIndex);

                    foreach (var thisHead in singleSignal.SignalHeads)
                    {
						sob.AppendFormat("Type name           : {0}\n", thisHead.signalType.Name);
						sob.AppendFormat("Type                : {0}\n", thisHead.signalType.FnType);
						sob.AppendFormat("item Index          : {0}\n", thisHead.trItemIndex);
						sob.AppendFormat("TDB  Index          : {0}\n", thisHead.TDBIndex);
						sob.AppendFormat("Junction Main Node  : {0}\n", thisHead.JunctionMainNode);
						sob.AppendFormat("Junction Path       : {0}\n", thisHead.JunctionPath);
                    }

					sob.AppendFormat("TC Reference   : {0}\n", singleSignal.TCReference);
					sob.AppendFormat("TC Direction   : {0}\n", singleSignal.TCDirection);
					sob.AppendFormat("TC Position    : {0}\n", singleSignal.TCOffset);
					sob.AppendFormat("TC TCNextTC    : {0}\n", singleSignal.TCNextTC);
                }
            }
			File.AppendAllText(@"C:\temp\SignalObjects.txt", sob.ToString());

			var ssb = new StringBuilder();
            foreach (var sshape in sigcfg.SignalShapes)
            {
				var thisshape = sshape.Value;
				ssb.Append("\n==========================================\n");
				ssb.AppendFormat("Shape key   : {0}\n", sshape.Key);
				ssb.AppendFormat("Filename    : {0}\n", thisshape.ShapeFileName);
				ssb.AppendFormat("Description : {0}\n", thisshape.Description);

                foreach (var ssobj in thisshape.SignalSubObjs)
                {
					ssb.AppendFormat("\nSubobj Index : {0}\n", ssobj.Index);
					ssb.AppendFormat("Matrix       : {0}\n", ssobj.MatrixName);
					ssb.AppendFormat("Description  : {0}\n", ssobj.Description);
					ssb.AppendFormat("Sub Type (I) : {0}\n", ssobj.SignalSubType);
                    if (ssobj.SignalSubSignalType != null)
                    {
						ssb.AppendFormat("Sub Type (C) : {0}\n", ssobj.SignalSubSignalType);
                    }
                    else
                    {
						ssb.AppendFormat("Sub Type (C) : not set \n");
                    }
					ssb.AppendFormat("Optional     : {0}\n", ssobj.Optional);
					ssb.AppendFormat("Default      : {0}\n", ssobj.Default);
					ssb.AppendFormat("BackFacing   : {0}\n", ssobj.BackFacing);
					ssb.AppendFormat("JunctionLink : {0}\n", ssobj.JunctionLink);
                }
				ssb.Append("\n==========================================\n");
            }
			File.AppendAllText(@"C:\temp\SignalShapes.txt", ssb.ToString());
#endif

            // Clear world lists to save memory

            SignalWorldList.Clear();
            //SignalRefList.Clear();
            SignalHeadList.Clear();

#if !ACTIVITY_EDITOR
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
                                Trace.TraceInformation("Signal " + thisSignal.thisRef +
                                    " ; TC : " + thisSignal.TCReference +
                                    " ; NextTC : " + thisSignal.TCNextTC +
                                    " ; TN : " + thisSignal.trackNode);
                            }

                            if (thisSignal.TCReference < 0) // signal is not on any track - remove it!
                            {
                                Trace.TraceInformation("Signal removed " + thisSignal.thisRef +
                                    " ; TC : " + thisSignal.TCReference +
                                    " ; NextTC : " + thisSignal.TCNextTC +
                                    " ; TN : " + thisSignal.trackNode);
                                SignalObjects[thisSignal.thisRef] = null;
                            }
                        }
                    }
                }
            }
#endif
        }

        //================================================================================================//
        /// 
        /// Gets an array of all the SignalObjects.
        ///

        public AESignalObject[] SignalObjects
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

        private void BuildSignalWorld(MSTSData data, SignalConfigurationFile sigcfg)
        {

            // get all filesnames in World directory

            var WFilePath = data.RoutePath + @"\WORLD\";

            var Tokens = new List<TokenID>();
            Tokens.Add(TokenID.Signal);

            // loop through files, use only extention .w, skip w+1000000+1000000.w file

            foreach (var fileName in Directory.GetFiles(WFilePath, "*.w"))
            {
                // validate file name a little bit

                if (Path.GetFileName(fileName).Length != 17)
                    continue;

                // read w-file, get SignalObjects only

                WorldFile WFile;
                try
                {
                    WFile = new WorldFile(fileName, Tokens);
                }
                catch (FileLoadException error)
                {
                    Trace.WriteLine(error);
                    continue;
                }

                // loop through all signals

                foreach (var worldObject in WFile.Tr_Worldfile)
                {
                    if (worldObject.GetType() == typeof(Orts.Formats.Msts.SignalObj))
                    {
                        var thisWorldObject = worldObject as Orts.Formats.Msts.SignalObj;
                        var SignalWorldSignal = new AESignalWorldObject(thisWorldObject, sigcfg);
                        SignalWorldList.Add(SignalWorldSignal);
                        foreach (var thisref in SignalWorldSignal.HeadReference)
                        {
                            var thisSignalCount = SignalWorldList.Count() - 1;    // Index starts at 0
                            //var thisRefObject = new SignalRefObject(thisSignalCount, thisref.Value);
                            //if (!SignalRefList.ContainsKey(thisref.Key))
                            //{
                            //    SignalRefList.Add(thisref.Key, thisRefObject);
                            //}
                        }
                    }
                }
            }

#if DEBUG_PRINT
			var srlb = new StringBuilder();
            foreach (var thisref in SignalRefList)
            {
                var TBDRef = thisref.Key;
				var signalRef = thisref.Value;
                var reffedObject = SignalWorldList[(int)signalRef.SignalWorldIndex];
                uint headref;
                if (!reffedObject.HeadReference.TryGetValue(TBDRef, out headref))
                {
                    srlb.AppendFormat("Incorrect Ref : {0}\n", TBDRef);
                    foreach (var headindex in reffedObject.HeadReference)
                    {
						srlb.AppendFormat("TDB : {0} + {1}\n", headindex.Key, headindex.Value);
                    }
                }
            }
			File.AppendAllText(@"WorldSignalList.txt", srlb.ToString());
#endif

        }  //BuildSignalWorld


        //================================================================================================//
        ///
        /// Build signal list from TDB
        ///

        private void BuildSignalList(TrItem[] TrItems, TrackNode[] trackNodes, TrackSectionsFile tsectiondat,
                TrackDatabaseFile tdbfile, Dictionary<int, int> platformList)
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
                signalObjects = new AESignalObject[noSignals];
                AESignalObject.trackNodes = trackNodes;
                AESignalObject.signalObjects = signalObjects;
                AESignalObject.trItems = TrItems;

                for (int i = 1; i < trackNodes.Length; i++)
                {
                    ScanSection(TrItems, trackNodes, i, tsectiondat, tdbfile, platformList);
                }

                // using world cross-reference list, merge heads to single signal

                //MergeHeads();

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

                for (var iSignal = 0; iSignal < SignalObjects.Length; iSignal++)
                {
                    if (SignalObjects[iSignal] != null)
                    {
                        var thisObject = SignalObjects[iSignal];
                        thisObject.thisRef = iSignal;

                        foreach (var thisHead in thisObject.SignalHeads)
                        {
                            thisHead.mainSignal = thisObject;
                            var trackItem = TrItems[thisHead.TDBIndex];
                            var sigItem = trackItem as SignalItem;
                            var speedItem = trackItem as SpeedPostItem;
                            if (sigItem != null)
                            {
                                sigItem.SigObj = thisObject.thisRef;
                            }
                            else if (speedItem != null)
                            {
                                speedItem.SigObj = thisObject.thisRef;
                            }
                        }
                    }
                }

                foundSignals = firstfree;

            }
            else
            {
                signalObjects = new AESignalObject[0];
            }

        } //BuildSignalList


        //================================================================================================//
        ///
        /// Split backfacing signals
        ///

        private void SplitBackfacing(TrItem[] TrItems, TrackNode[] TrackNodes)
        {

            List<AESignalObject> newSignals = new List<AESignalObject>();
            int newindex = foundSignals;

            //
            // Loop through all signals to check on Backfacing heads
            //

            for (int isignal = 0; isignal < signalObjects.Length - 1; isignal++)
            {
                AESignalObject singleSignal = signalObjects[isignal];
                if (singleSignal != null && singleSignal.isSignal &&
                                singleSignal.WorldObject != null && singleSignal.WorldObject.Backfacing.Count > 0)
                {

                    //
                    // create new signal - copy of existing signal
                    // use Backfacing flags and reset head indication
                    //

                    AESignalObject newSignal = new AESignalObject(singleSignal);

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
                                    AESignalHead thisHead = singleSignal.SignalHeads[ihIndex];

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
                                //singleSignal.tdbtraveller.ReverseDirection();                           // reverse //
                            }
                        }

                        SignalItem thisItemNew = TrItems[newSignal.SignalHeads[0].TDBIndex] as SignalItem;
                        if (newSignal.direction != thisItemNew.Direction)
                        {
                            newSignal.direction = (int)thisItemNew.Direction;
                            //newSignal.tdbtraveller.ReverseDirection();                           // reverse //
                        }

                        //
                        // set correct trRefIndex for this signal, and set cross-reference for all backfacing trRef items
                        //

                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.NoItemRefs; i++)
                        {
                            int TDBRef = TrackNodes[newSignal.trackNode].TrVectorNode.TrItemRefs[i];
                            if (TrItems[TDBRef] != null)
                            {
                                if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                {
                                    foreach (AESignalHead thisHead in newSignal.SignalHeads)
                                    {
                                        if (TDBRef == thisHead.TDBIndex)
                                        {
                                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                            sigItem.SigObj = newSignal.thisRef;
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

                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.NoItemRefs; i++)
                        {
                            int TDBRef = TrackNodes[newSignal.trackNode].TrVectorNode.TrItemRefs[i];
                            if (TrItems[TDBRef] != null)
                            {
                                if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                                {
                                    foreach (AESignalHead thisHead in singleSignal.SignalHeads)
                                    {
                                        if (TDBRef == thisHead.TDBIndex)
                                        {
                                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                                            sigItem.SigObj = singleSignal.thisRef;
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

            newindex = foundSignals;
            foreach (AESignalObject newSignal in newSignals)
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
                               TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile, Dictionary<int, int> platformList)
        {
            int lastSignal = -1;                // Index to last signal found in ComponentItem -1 if none

            if (trackNodes[index].TrEndNode)
                return;

            //  Is it a vector node then it may contain objects.
            if (trackNodes[index].TrVectorNode != null && trackNodes[index].TrVectorNode.NoItemRefs > 0)
            {
                // Any obects ?
                for (int i = 0; i < trackNodes[index].TrVectorNode.NoItemRefs; i++)
                {
                    if (TrItems[trackNodes[index].TrVectorNode.TrItemRefs[i]] != null)
                    {
                        int TDBRef = trackNodes[index].TrVectorNode.TrItemRefs[i];

                        // Track Item is signal
                        if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSIGNAL)
                        {
                            SignalItem sigItem = (SignalItem)TrItems[TDBRef];
                            sigItem.SigObj = foundSignals;

                            bool validSignal = true;
                            lastSignal = AddSignal(index, i, sigItem, TDBRef, tsectiondat, tdbfile, ref validSignal);

                            if (validSignal)
                            {
                                sigItem.SigObj = lastSignal;
                            }
                            else
                            {
                                sigItem.SigObj = -1;
                            }
                        }

        // Track Item is speedpost - check if really limit
                        else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trSPEEDPOST)
                        {
                            SpeedPostItem speedItem = (SpeedPostItem)TrItems[TDBRef];
                            if (speedItem.IsLimit)
                            {
                                speedItem.SigObj = foundSignals;

                                lastSignal = AddSpeed(index, i, speedItem, TDBRef, tsectiondat, tdbfile);
                                speedItem.SigObj = lastSignal;

                            }
                        }
                        else if (TrItems[TDBRef].ItemType == TrItem.trItemType.trPLATFORM)
                        {
                            if (platformList.ContainsKey(TDBRef))
                            {
                                Trace.TraceInformation("Double reference to platform ID {0} in nodes {1} and {2}\n", TDBRef, platformList[TDBRef], index);
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
                                Trace.TraceInformation("Double reference to siding ID {0} in nodes {1} and {2}\n", TDBRef, platformList[TDBRef], index);
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

#if false
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
                            Trace.TraceInformation("Signal found in Worldfile but not in TDB - TDB Index : {0}", thisReference.Key);
                            MainSignal = null;
                        }
                    }
                }
            }
        }

#endif
        //================================================================================================//
        ///
        /// This method adds a new Signal to the list
        ///

        private int AddSignal(int trackNode, int nodeIndx, SignalItem sigItem, int TDBRef, TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile, ref bool validSignal)
        {
            validSignal = true;

            signalObjects[foundSignals] = new AESignalObject();
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
                Trace.TraceInformation("Reference to invalid track node {0} for Signal {1}\n", trackNode, TDBRef);
            }
            else
            {
                //signalObjects[foundSignals].tdbtraveller =
                //new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                //        sigItem.TileX, sigItem.TileZ, sigItem.X, sigItem.Z,
                //(Traveller.TravellerDirection)(1 - sigItem.Direction));
            }

            signalObjects[foundSignals].WorldObject = null;

            if (SignalHeadList.ContainsKey((uint)TDBRef))
            {
                validSignal = false;
                Trace.TraceInformation("Invalid double TDBRef {0} in node {1}\n", TDBRef, trackNode);
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

        private int AddSpeed(int trackNode, int nodeIndx, SpeedPostItem speedItem, int TDBRef, TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile)
        {
            signalObjects[foundSignals] = new AESignalObject();
            signalObjects[foundSignals].isSignal = false;
            signalObjects[foundSignals].direction = 0;                  // preset - direction not yet known //
            signalObjects[foundSignals].trackNode = trackNode;
            signalObjects[foundSignals].trRefIndex = nodeIndx;
            signalObjects[foundSignals].AddHead(nodeIndx, TDBRef, speedItem);
            signalObjects[foundSignals].thisRef = foundSignals;
            signalObjects[foundSignals].signalRef = this;

#if DEBUG_PRINT
            File.AppendAllText(@"C:\temp\speedpost.txt",
				String.Format("\nPlaced : at : {0} {1}:{2} {3}; angle - track : {4}:{5}; delta : {6}; dir : {7}\n",
				speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z,
				speedItem.Angle, signalObjects[foundSignals].tdbtraveller.RotY,
				delta_angle,
				signalObjects[foundSignals].direction));
#endif

            signalObjects[foundSignals].WorldObject = null;
            foundSignals++;
            return foundSignals - 1;
        } // AddSpeed

        //================================================================================================//
        //
        //      AddCFG : This method adds the sigcfg reference to each signal object.
        //

        private void AddCFG(SignalConfigurationFile sigCFG)
        {
            foreach (AESignalObject signal in signalObjects)
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

            foreach (AESignalObject signal in signalObjects)
            {
                if (signal != null)
                {
                    foreach (AESignalHead head in signal.SignalHeads)
                    {

                        // get reference using TDB index from head

                        uint TDBRef = Convert.ToUInt32(head.TDBIndex);
#if false
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
#endif
                    }
                }
            }

        }//AddWorldInfo

#if false
		        //================================================================================================//
        ///
        //  FindByTrItem : find required signalObj + signalHead
        ///

        public KeyValuePair<SignalObject, SignalHead>? FindByTrItem(uint trItem)
        {
            foreach (var signal in signalObjects)
                if (signal != null)
                    foreach (var head in signal.SignalHeads)
                        if (SignalObject.trackNodes[signal.trackNode].TrVectorNode.TrItemRefs[head.trItemIndex] == (int)trItem)
                            return new KeyValuePair<SignalObject, SignalHead>(signal, head);
            return null;
        }//FindByTrItem
  
        //
        //================================================================================================//
        //
        // Create Track Circuits
        //

        private void CreateTrackCircuits(TrItem[] TrItems, TrackNode[] trackNodes, TSectionDatFile tsectiondat)
        {
        }
#endif

     }// class AESignals

    //================================================================================================//
    //
    //  class AESignalObject
    //
    //================================================================================================//

    public class AESignalObject
    {

        public enum MstsBlockState
        {
            CLEAR,         // Block ahead is clear and accesible
            OCCUPIED,      // Block ahead is occupied by one or more wagons/locos not moving in opposite direction
            JN_OBSTRUCTED, // Block ahead is impassable due to the state of a switch or occupied by moving train or not accesible
        }

        public enum InternalBlockstate
        {
            Reserved,                   // all sections reserved for requiring train       //
            Reservable,                 // all secetions clear and reservable for train    //
            OccupiedSameDirection,      // occupied by train moving in same direction      //
            ReservedOther,              // reserved for other train                        //
            OccupiedOppositeDirection,  // occupied by train moving in opposite direction  //
            Open,                       // sections are claimed and not accesible          //
            Blocked,                    // switch locked against train                     //
        }

        public enum Permission
        {
            Granted,
            Requested,
            Denied,
        }

        public enum HoldState                // signal is locked in hold
        {
            None,                            // signal is clear
            StationStop,                     // because of station stop
            ManualLock,                      // because of manual lock. 
            ManualPass,                      // Sometime you want to set a light green, especially in MP
            ManualApproach,                  // Sometime to set approach, in MP again
            //PLEASE DO NOT CHANGE THE ORDER OF THESE ENUMS
        }

        public AESignals signalRef;               // reference to overlaying Signal class
        public static AESignalObject[] signalObjects;
        public static TrackNode[] trackNodes;
        public static TrItem[] trItems;
        public AESignalWorldObject WorldObject;   // Signal World Object information

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
        public List<AESignalHead> SignalHeads = new List<AESignalHead>();

        public int SignalNumClearAhead_MSTS = -2;    // Overall maximum SignalNumClearAhead over all heads (MSTS calculation)
        public int SignalNumClearAhead_ORTS = -2;    // Overall maximum SignalNumClearAhead over all heads (ORTS calculation)
        public int SignalNumNormalHeads;             // no. of normal signal heads in signal
        public int ReqNumClearAhead;                 // Passed on value for SignalNumClearAhead

        public int draw_state;                  // actual signal state

        //public Train.TrainRouted enabledTrain;  // full train structure for which signal is enabled

        private InternalBlockstate internalBlockState = InternalBlockstate.Open;    // internal blockstate
        public Permission hasPermission = Permission.Denied;  // Permission to pass red signal
        public HoldState holdState = HoldState.None;

        public int[] sigfound = new int[(int)MstsSignalFunction.UNKNOWN];  // active next signal - used for signals with NORMAL heads only
        private int[] defaultNextSignal = new int[(int)MstsSignalFunction.UNKNOWN];  // default next signal
        //public AETraveller tdbtraveller;          // TDB traveller to determine distance between objects

        public int trainRouteDirectionIndex;    // direction index in train route array (usually 0, value 1 valid for Manual only)

        public bool hasFixedRoute;              // signal has no fixed route
        public bool StationHold = false;        // Set if signal must be held at station - processed by signal script


        public bool enabled
        {
            get
            {
                return true;
            }
        }

        public MstsBlockState blockState
        {
            get
            {
                MstsBlockState lstate = MstsBlockState.JN_OBSTRUCTED;
                switch (internalBlockState)
                {
                    case InternalBlockstate.Reserved:
                    case InternalBlockstate.Reservable:
                        lstate = MstsBlockState.CLEAR;
                        break;
                    case InternalBlockstate.OccupiedSameDirection:
                        lstate = MstsBlockState.OCCUPIED;
                        break;
                    default:
                        lstate = MstsBlockState.JN_OBSTRUCTED;
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
        //  Constructor for empty sideItem
        ///

        public AESignalObject()
        {
        }

        //================================================================================================//
        ///
        //  Constructor for Copy 
        ///

        public AESignalObject(AESignalObject copy)
        {
            signalRef = copy.signalRef;
            WorldObject = new AESignalWorldObject(copy.WorldObject);

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

            //tdbtraveller = new Traveller(copy.tdbtraveller);

            sigfound = new int[copy.sigfound.Length];
            copy.sigfound.CopyTo(sigfound, 0);
            defaultNextSignal = new int[copy.defaultNextSignal.Length];
            copy.defaultNextSignal.CopyTo(defaultNextSignal, 0);
        }

#if false
        public MstsBlockState block_state()
        {
            return (blockState);
        }

        //================================================================================================//
        //
        // return station hold state
        //

        public bool isStationHold()
        {
            return (StationHold);
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

            for (int fntype = 0; fntype < (int)MstsSignalFunction.UNKNOWN; fntype++)
            {
                defaultNextSignal[fntype] = -1;
            }

            // loop through valid sections

            while (thisSection != null && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
            {

                if (!completedFixedRoute)
                {
                    //fixedRoute.Add(new Train.TCRouteElement(thisSection.Index, direction));
                }

                // normal signal

                if (defaultNextSignal[(int)MstsSignalFunction.NORMAL] < 0)
                {
                    if (thisSection.EndSignals[direction] != null)
                    {
                        defaultNextSignal[(int)MstsSignalFunction.NORMAL] = thisSection.EndSignals[direction].thisRef;
                        completedFixedRoute = true;
                    }
                }

                // other signals

                for (int fntype = 0; fntype < (int)MstsSignalFunction.UNKNOWN; fntype++)
                {
                    if (fntype != (int)MstsSignalFunction.NORMAL && fntype != (int)MstsSignalFunction.UNKNOWN)
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

            for (int fntype = 0; fntype < (int)MstsSignalFunction.UNKNOWN; fntype++)
            {
                sigfound[fntype] = defaultNextSignal[fntype];
            }

            // Allow use of fixed route if ended on END_OF_TRACK

            if (thisSection != null && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
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
                //fixedRoute.Clear();
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
                if (sigHead.sigFunction == MstsSignalFunction.NORMAL)
                    return true;
            }
            return false;
        }

        //================================================================================================//
        ///
        // isSignalType : Returns true if at least one signal head is of required type
        ///

        public bool isSignalType(MstsSignalFunction[] reqSIGFN)
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

        public MstsSignalAspect next_sig_mr(MstsSignalFunction fn_type)
        {
                return MstsSignalAspect.STOP;
        }

        //================================================================================================//
        ///
        // next_sig_lr : returns least restrictive state of next signal of required type
        ///
        ///

        public MstsSignalAspect next_sig_lr(MstsSignalFunction fn_type)
        {
            {
                return MstsSignalAspect.STOP;
            }
        }

        //================================================================================================//
        //
        // opp_sig_mr
        //

        public MstsSignalAspect opp_sig_mr(MstsSignalFunction fn_type)
        {
            return (MstsSignalAspect.STOP);
        }//opp_sig_mr

        public MstsSignalAspect opp_sig_mr(MstsSignalFunction fn_type, ref SignalObject foundSignal) // used for debug print process
        {
            return (MstsSignalAspect.STOP);
        }//opp_sig_mr

        //================================================================================================//
        //
        // opp_sig_lr
        //

        public MstsSignalAspect opp_sig_lr(MstsSignalFunction fn_type)
        {
            return (MstsSignalAspect.STOP);
        }//opp_sig_lr

        public MstsSignalAspect opp_sig_lr(MstsSignalFunction fn_type, ref SignalObject foundSignal) // used for debug print process
        {
            return (MstsSignalAspect.STOP);
        }//opp_sig_lr

        //================================================================================================//
        //
        // this_sig_mr : Returns the most restrictive state of this signal's heads of required type
        //

        // standard version without state return
        public MstsSignalAspect this_sig_mr(MstsSignalFunction fn_type)
        {
            bool sigfound = false;
            return (this_sig_mr(fn_type, ref sigfound));
        }

        // additional version with state return
        public MstsSignalAspect this_sig_mr(MstsSignalFunction fn_type, ref bool sigfound)
        {
            MstsSignalAspect sigAsp = MstsSignalAspect.UNKNOWN;
            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction == fn_type && sigHead.state < sigAsp)
                {
                    sigAsp = sigHead.state;
                }
            }
            if (sigAsp == MstsSignalAspect.UNKNOWN)
            {
                sigfound = false;
                return MstsSignalAspect.STOP;
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
        public MstsSignalAspect this_sig_lr(MstsSignalFunction fn_type)
        {
            bool sigfound = false;
            return (this_sig_lr(fn_type, ref sigfound));
        }

        // additional version with state return
        public MstsSignalAspect this_sig_lr(MstsSignalFunction fn_type, ref bool sigfound)
        {
            MstsSignalAspect sigAsp = MstsSignalAspect.STOP;
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
            else if (fn_type == MstsSignalFunction.NORMAL)
            {
                return MstsSignalAspect.CLEAR_2;
            }
            else
            {
                return MstsSignalAspect.STOP;
            }
        }//this_sig_lr

        //================================================================================================//
        //
        // this_sig_speed : Returns the speed related to the least restrictive aspect (for normal signal)
        //

        public ObjectSpeedInfo this_sig_speed(MstsSignalFunction fn_type)
        {
            var sigAsp = MstsSignalAspect.STOP;
            var set_speed = new ObjectSpeedInfo(-1, -1, false);

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction == fn_type && sigHead.state >= sigAsp)
                {
                    sigAsp = sigHead.state;
                    set_speed = sigHead.speed_info[(int)sigAsp];
                }
            }
            return set_speed;
        }//this_sig_speed

        //================================================================================================//
        //
        // this_lim_speed : Returns the lowest allowed speed (for speedpost and speed signal)
        //

        public ObjectSpeedInfo this_lim_speed(MstsSignalFunction fn_type)
        {
            var set_speed = new ObjectSpeedInfo(9E9f, 9E9f, false);

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.sigFunction == fn_type)
                {
                    ObjectSpeedInfo this_speed = sigHead.speed_info[(int)sigHead.state];
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

#endif

        //================================================================================================//
        //
        // route_set : check if required route is set
        //

        public bool route_set(int req_mainnode, uint req_jnnode)
        {
            return false;
        }

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
            // create AESignalHead
            AESignalHead head = new AESignalHead(this, trItem, TDBRef, sigItem);

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
            // create AESignalHead
            AESignalHead head = new AESignalHead(this, trItem, TDBRef, speedItem);
            SignalHeads.Add(head);

        }//AddHead (speedpost)

        //================================================================================================//
        //
        // SetSignalType : Sets the signal type from the sigcfg file for each signal head.
        //

        public void SetSignalType(SignalConfigurationFile sigCFG)
        {
            foreach (AESignalHead sigHead in SignalHeads)
            {
                sigHead.SetSignalType(trItems, sigCFG);
            }
        }//SetSignalType

        //================================================================================================//
        //
        // TranslateTMAspect : Gets the display aspect for the track monitor.
        //

        public TrackMonitorSignalAspect TranslateTMAspect(MstsSignalAspect SigState)
        {
            switch (SigState)
            {
                case MstsSignalAspect.STOP:
                    if (hasPermission == Permission.Granted)
                        return TrackMonitorSignalAspect.Permission;
                    else
                        return TrackMonitorSignalAspect.Stop;
                case MstsSignalAspect.STOP_AND_PROCEED:
                    return TrackMonitorSignalAspect.StopAndProceed;
                case MstsSignalAspect.RESTRICTING:
                    return TrackMonitorSignalAspect.Restricted;
                case MstsSignalAspect.APPROACH_1:
                    return TrackMonitorSignalAspect.Approach_1;
                case MstsSignalAspect.APPROACH_2:
                    return TrackMonitorSignalAspect.Approach_2;
                case MstsSignalAspect.APPROACH_3:
                    return TrackMonitorSignalAspect.Approach_3;
                case MstsSignalAspect.CLEAR_1:
                    return TrackMonitorSignalAspect.Clear_1;
                case MstsSignalAspect.CLEAR_2:
                    return TrackMonitorSignalAspect.Clear_2;
                default:
                    return TrackMonitorSignalAspect.None;
            }
        } // GetMonitorAspect

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
            holdState = HoldState.None;
        }

    }  // AESignalObject


    //================================================================================================//
    //
    // class AESignalHead
    //
    //================================================================================================//

    public class AESignalHead
    {
        public SignalType signalType;           // from sigcfg file
        public MstsSignalAspect state = MstsSignalAspect.STOP;
        public int draw_state;
        public int trItemIndex;                 // Index to trItem   
        public uint TrackJunctionNode;          // Track Junction Node (= 0 if not set)
        public uint JunctionPath;               // Required Junction Path
        public int JunctionMainNode;            // Main node following junction
        public int TDBIndex;                    // Index to TDB Signal Item
        //public ObjectSpeedInfo[] speed_info;      // speed limit info (per aspect)

        public AESignalObject mainSignal;        //  This is the signal which this head forms a part.

        public MstsSignalFunction sigFunction
        {
            get
            {
                if (signalType != null)
                    return signalType.Function.MstsFunction;
                else
                    return MstsSignalFunction.UNKNOWN;
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

        public AESignalHead(AESignalObject sigOoject, int trItem, int TDBRef, SignalItem sigItem)
        {
            mainSignal = sigOoject;
            trItemIndex = trItem;
            TDBIndex = TDBRef;

            if (sigItem.NoSigDirs > 0)
            {
                TrackJunctionNode = sigItem.TrSignalDirs[0].TrackNode;
                JunctionPath = sigItem.TrSignalDirs[0].LinkLRPath;
            }

            var sigasp_values = Enum.GetValues(typeof(MstsSignalAspect));
            //speed_info = new ObjectSpeedInfo[sigasp_values.Length];
        }

        //================================================================================================//
        //
        // Constructor for speedposts
        //

        public AESignalHead(AESignalObject sigObject, int trItem, int TDBRef, SpeedPostItem speedItem)
        {
            mainSignal = sigObject;
            trItemIndex = trItem;
            TDBIndex = TDBRef;
            draw_state = 1;
            state = MstsSignalAspect.CLEAR_2;
            signalType = new SignalType(SignalFunction.SPEED, MstsSignalAspect.CLEAR_2);

            var sigasp_values = Enum.GetValues(typeof(MstsSignalAspect));
            //speed_info = new ObjectSpeedInfo[sigasp_values.Length];

            float speedMpS = MpS.ToMpS(speedItem.SpeedInd, !speedItem.IsMPH);
            if (speedItem.IsResume)
                speedMpS = 999f;

            float passSpeed = speedItem.IsPassenger ? speedMpS : -1;
            float freightSpeed = speedItem.IsFreight ? speedMpS : -1;
            //ObjectSpeedInfo speedinfo = new ObjectSpeedInfo(passSpeed, freightSpeed, false);
            //speed_info[(int)state] = speedinfo;
        }

        //================================================================================================//
        //
        // SetSignalType : This method sets the signal type object from the CIGCFG file
        //

        public void SetSignalType(TrItem[] TrItems, SignalConfigurationFile sigCFG)
        {
            SignalItem sigItem = (SignalItem)TrItems[TDBIndex];

            // set signal type
            if (sigCFG.SignalTypes.ContainsKey(sigItem.SignalType))
            {
                signalType = sigCFG.SignalTypes[sigItem.SignalType];

                // set signal speeds
                foreach (SignalAspect thisAspect in signalType.Aspects)
                {
                    int arrindex = (int)thisAspect.Aspect;
                    //speed_info[arrindex] = new ObjectSpeedInfo(thisAspect.SpeedMpS, thisAspect.SpeedMpS, thisAspect.Asap);
                }

                // update overall SignalNumClearAhead

                if (sigFunction == MstsSignalFunction.NORMAL)
                {
                    mainSignal.SignalNumClearAhead_MSTS = Math.Max(mainSignal.SignalNumClearAhead_MSTS, signalType.NumClearAhead_MSTS);
                    mainSignal.SignalNumClearAhead_ORTS = Math.Max(mainSignal.SignalNumClearAhead_ORTS, signalType.NumClearAhead_ORTS);
                }
            }
            else
            {
                Trace.TraceWarning("AESignalObject trItem={0}, trackNode={1} has AESignalHead with undefined SignalType {2}.",
                                  mainSignal.trItem, mainSignal.trackNode, sigItem.SignalType);
            }


        }//SetSignalType

#if false
        //================================================================================================//
        //
        //  Following methods used in scipting
        //

        public MstsSignalAspect next_sig_mr(MstsSignalFunction sigFN)
        {
            return mainSignal.next_sig_mr(sigFN);
        }

        public MstsSignalAspect next_sig_lr(MstsSignalFunction sigFN)
        {
            return mainSignal.next_sig_lr(sigFN);
        }

        public MstsSignalAspect this_sig_lr(MstsSignalFunction sigFN)
        {
            return mainSignal.this_sig_lr(sigFN);
        }

        public MstsSignalAspect this_sig_lr(MstsSignalFunction sigFN, ref bool sigfound)
        {
            return mainSignal.this_sig_lr(sigFN, ref sigfound);
        }

        public MstsSignalAspect this_sig_mr(MstsSignalFunction sigFN)
        {
            return mainSignal.this_sig_mr(sigFN);
        }

        public MstsSignalAspect this_sig_mr(MstsSignalFunction sigFN, ref bool sigfound)
        {
            return mainSignal.this_sig_mr(sigFN, ref sigfound);
        }

        public MstsSignalAspect opp_sig_mr(MstsSignalFunction sigFN)
        {
            return mainSignal.opp_sig_mr(sigFN);
        }

        public MstsSignalAspect opp_sig_mr(MstsSignalFunction sigFN, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_mr(sigFN, ref signalFound);
        }

        public MstsSignalAspect opp_sig_lr(MstsSignalFunction sigFN)
        {
            return mainSignal.opp_sig_lr(sigFN);
        }

        public MstsSignalAspect opp_sig_lr(MstsSignalFunction sigFN, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_lr(sigFN, ref signalFound);
        }

        //================================================================================================//
        //
        //  dist_multi_sig_mr : Returns most restrictive state of signal type A, for all type A upto type B
        //  
        //

        public MstsSignalAspect dist_multi_sig_mr(MstsSignalFunction sigFN1, MstsSignalFunction sigFN2, string dumpfile)
        {
            return (MstsSignalAspect.STOP);   // no type 2 or running out of signals before finding type 2
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

        public int def_draw_state(MstsSignalAspect state)
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
                state = MstsSignalAspect.STOP;

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
                state = MstsSignalAspect.CLEAR_2;
            def_draw_state(state);
        }//SetLeastRestrictiveAspect

        //================================================================================================//
        //
        //  route_set : check if linked route is set
        //

        public int route_set()
        {
                return 0;
        }//route_set

        //================================================================================================//
        //
        //  Default update process
        //

        public void Update()
        {
            //SIGSCRfile.SH_update(this, Signals.scrfile);
        }
#endif
    } //Update
    //================================================================================================//
    //
    // class SignalWorldInfo
    //
    //================================================================================================//

    public class AESignalWorldObject
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

        public AESignalWorldObject(SignalObj SignalWorldItem, SignalConfigurationFile sigcfg)
        {
            SignalShape thisCFGShape;

            HeadReference = new Dictionary<uint, uint>();

            // set flags with length to number of possible SubObjects type

            FlagsSet = new bool[SignalShape.SignalSubObj.SignalSubTypes.Count];
            FlagsSetBackfacing = new bool[SignalShape.SignalSubObj.SignalSubTypes.Count];
            for (uint iFlag = 0; iFlag < FlagsSet.Length; iFlag++)
            {
                FlagsSet[iFlag] = false;
                FlagsSetBackfacing[iFlag] = false;
            }

            // get filename in Uppercase

            SFileName = SignalWorldItem.FileName.ToUpperInvariant();

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
                    SignalShape.SignalSubObj thisSubObjs = thisCFGShape.SignalSubObjs[iHead];
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
                if (SignalWorldItem != null && SignalWorldItem.SignalUnits != null)
                {
                    foreach (SignalUnit signalUnitInfo in SignalWorldItem.SignalUnits.Units)
                    {
                        uint TrItemRef = signalUnitInfo.TrItem;
                        uint HeadRef = Convert.ToUInt32(signalUnitInfo.SubObj);
                        HeadReference.Add(TrItemRef, HeadRef);
                    }
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

        public AESignalWorldObject(AESignalWorldObject copy)
        {
            SFileName = copy.SFileName;
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

#if false

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
    // class ObjectItemInfo
    //
    //================================================================================================//

    public class ObjectItemInfo
    {
        public enum ObjectItemType
        {
            Any,
            Signal,
            Speedlimit,
        }

        public enum ObjectItemFindState
        {
            None = 0,
            Object = 1,
            EndOfTrack = -1,
            PassedDanger = -2,
            PassedMaximumDistance = -3,
            TdbError = -4,
            EndOfAuthority = -5,
            EndOfPath = -6,
        }

        public ObjectItemType ObjectType;                     // type information
        public ObjectItemFindState ObjectState;               // state information

        public SignalObject ObjectDetails;                    // actual object 

        public float distance_found;
        public float distance_to_train;
        public float distance_to_object;

        public MstsSignalAspect signal_state;                   // UNKNOWN if type = speedlimit
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
            ObjectState = ObjectItemFindState.Object;

            distance_found = distance;

            ObjectDetails = thisObject;

            if (thisObject.isSignal)
            {
                ObjectType = ObjectItemType.Signal;
                signal_state = MstsSignalAspect.UNKNOWN;  // set active by TRAIN
                speed_passenger = -1;                      // set active by TRAIN
                speed_freight = -1;                      // set active by TRAIN
                speed_flag = 0;                       // set active by TRAIN
            }
            else
            {
                ObjectType = ObjectItemType.Speedlimit;
                signal_state = MstsSignalAspect.UNKNOWN;
                speed_info = thisObject.this_lim_speed(MstsSignalFunction.SPEED);
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

            orgDetails.PlatformReference.CopyTo(PlatformReference, 0);
            TCOffset[0, 0] = orgDetails.TCOffset[0, 0];
            TCOffset[0, 1] = orgDetails.TCOffset[0, 1];
            TCOffset[1, 0] = orgDetails.TCOffset[1, 0];
            TCOffset[1, 1] = orgDetails.TCOffset[1, 1];
            orgDetails.nodeOffset.CopyTo(nodeOffset, 0);
            Length = orgDetails.Length;
            orgDetails.EndSignals.CopyTo(EndSignals, 0);
            orgDetails.DistanceToSignals.CopyTo(DistanceToSignals, 0);
            Name = String.Copy(orgDetails.Name);
            MinWaitingTime = orgDetails.MinWaitingTime;
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
        //public TrainOccupyState TrainOccupy;                       // trains occupying section      //
        //public Train.TrainRouted TrainReserved;                    // train reserving section       //
        public int SignalReserved;                                 // signal reserving section      //
        //public TrainQueue TrainPreReserved;                        // trains with pre-reservation   //
        //public TrainQueue TrainClaimed;                            // trains with normal claims     //
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
            //TrainOccupy = new TrainOccupyState();
            //TrainReserved = null;
            SignalReserved = -1;
            //TrainPreReserved = new TrainQueue();
            //TrainClaimed = new TrainQueue();
        }


        //================================================================================================//
        //
        // check if this train occupies track
        //

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


    public class CrossOverItem
    {
        public float[] Position = new float[2];        // position within track sections //
        public int[] SectionIndex = new int[2];          // indices of original sections   //
        public int[] ItemIndex = new int[2];             // TDB sideItem indices               //
        public uint TrackShape;
    }
#endif    

}

