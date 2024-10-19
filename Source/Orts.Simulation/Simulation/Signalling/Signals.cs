// COPYRIGHT 2021 by the Open Rails project.
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

// Debug flags :
// #define DEBUG_PRINT
// prints details of the derived signal structure
// #define DEBUG_REPORTS
// print details of train behaviour

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.Simulation.Signalling
{
    public class Signals
    {
        //================================================================================================//
        // local data
        //================================================================================================//

        internal readonly Simulator Simulator;

        public TrackDB trackDB;
        private TrackSectionsFile tsectiondat;
        private TrackDatabaseFile tdbfile;

        public SignalObject[] SignalObjects { get; private set; }
        private List<SignalWorldObject> SignalWorldList = new List<SignalWorldObject>();
        private Dictionary<uint, SignalRefObject> SignalRefList;
        private Dictionary<uint, SignalObject> SignalHeadList;
        private List<SpeedPostWorldObject> SpeedPostWorldList = new List<SpeedPostWorldObject>();
        private Dictionary<int, int> SpeedPostRefList = new Dictionary<int, int>();
        public static SIGSCRfile scrfile;
        public static CsSignalScripts CsSignalScripts;
        public readonly Dictionary<string, SignalFunction> SignalFunctions;
        public IList<string> ORTSNormalsubtypes;

        public int noSignals;
        private int foundSignals;

        private static int UpdateIndex;

        public List<TrackCircuitSection> TrackCircuitList;
        private Dictionary<int, CrossOverItem> CrossoverList = new Dictionary<int, CrossOverItem>();
        public List<PlatformDetails> PlatformDetailsList = new List<PlatformDetails>();
        public Dictionary<int, int> PlatformXRefList = new Dictionary<int, int>();
        private Dictionary<int, uint> PlatformSidesList = new Dictionary<int, uint>();
        public Dictionary<string, List<int>> StationXRefList = new Dictionary<string, List<int>>();

        public bool UseLocationPassingPaths;                    // Use location-based style processing of passing paths (set by Simulator)
        public Dictionary<int, DeadlockInfo> DeadlockInfoList;  // each deadlock info has unique reference
        public int deadlockIndex;                               // last used reference index
        public Dictionary<int, int> DeadlockReference;          // cross-reference between trackcircuitsection (key) and deadlockinforeference (value)

        public List<Milepost> MilepostList = new List<Milepost>();                     // list of mileposts
        private int foundMileposts;

        public Signals(Simulator simulator, SignalConfigurationFile sigcfg, CancellationToken cancellation)
        {
            Simulator = simulator;

#if DEBUG_REPORTS
            File.Delete(@"C:\temp\printproc.txt");
#endif

            SignalRefList = new Dictionary<uint, SignalRefObject>();
            SignalHeadList = new Dictionary<uint, SignalObject>();
            Dictionary<int, int> platformList = new Dictionary<int, int>();

            SignalFunctions = new Dictionary<string, SignalFunction>(sigcfg.SignalFunctions);
            ORTSNormalsubtypes = sigcfg.ORTSNormalSubtypes;

            trackDB = simulator.TDB.TrackDB;
            tsectiondat = simulator.TSectionDat;
            tdbfile = Simulator.TDB;

            // read SIGSCR files
            Trace.Write(" SIGSCR ");
            scrfile = new SIGSCRfile(new SignalScripts(sigcfg.ScriptPath, sigcfg.ScriptFiles, sigcfg.SignalTypes, sigcfg.SignalFunctions, sigcfg.ORTSNormalSubtypes));
            CsSignalScripts = new CsSignalScripts(Simulator);

            // build list of signal world file information
            BuildSignalWorld(simulator, sigcfg, cancellation);

            // build list of signals in TDB file
            BuildSignalList(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat, tdbfile, platformList, MilepostList);

            if (foundSignals > 0)
            {
                // Add CFG info
                AddCFG(sigcfg);

                // Add World info
                AddWorldInfo();

                InitializeSignals();

                // check for any backfacing heads in signals
                // if found, split signal
                SplitBackfacing(trackDB.TrItemTable, trackDB.TrackNodes);
            }

            if (SignalObjects != null)
                SetNumSignalHeads();

            //
            // Create trackcircuit database
            //
#if ACTIVITY_EDITOR
            CreateTrackCircuits(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat, simulator.orRouteConfig);
#else
            CreateTrackCircuits(trackDB.TrItemTable, trackDB.TrackNodes, tsectiondat);
#endif

            // Process platform information
            ProcessPlatforms(platformList, trackDB.TrItemTable, trackDB.TrackNodes, PlatformSidesList);

            // Process tunnel information
            ProcessTunnels();

            // Process trough information
            ProcessTroughs();

            // Print all info (DEBUG only)
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
            for (var isignal = 0; isignal < SignalObjects.Length - 1; isignal++)
            {
                var singleSignal = SignalObjects[isignal];
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
                        sob.AppendFormat("Type                : {0}\n", thisHead.signalType.Function.MstsFunction.ToString());
                        sob.AppendFormat("OR Type             : {0}\n", thisHead.signalType.Function.Name);
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
                                Trace.TraceInformation("Signal " + thisSignal.thisRef +
                                    " ; TC : " + thisSignal.TCReference +
                                    " ; NextTC : " + thisSignal.TCNextTC +
                                    " ; TN : " + thisSignal.trackNode + 
                                    " ; TDB (0) : " + thisSignal.SignalHeads[0].TDBIndex);
                            }

                            if (thisSignal.TCReference < 0) // signal is not on any track - remove it!
                            {
                                Trace.TraceInformation("Signal removed " + thisSignal.thisRef +
                                    " ; TC : " + thisSignal.TCReference +
                                    " ; NextTC : " + thisSignal.TCNextTC +
                                    " ; TN : " + thisSignal.trackNode +
                                    " ; TDB (0) : " + thisSignal.SignalHeads[0].TDBIndex);
                                SignalObjects[thisSignal.thisRef] = null;
                            }
                        }
                    }
                }
            }

            DeadlockInfoList = new Dictionary<int, DeadlockInfo>();
            deadlockIndex = 1;
            DeadlockReference = new Dictionary<int, int>();
        }

        /// <summary>
        /// Overlay constructor for restore after saved game
        /// </summary>
        public Signals(Simulator simulator, SignalConfigurationFile sigcfg, BinaryReader inf, CancellationToken cancellation)
            : this(simulator, sigcfg, cancellation)
        {
            int signalIndex = inf.ReadInt32();
            while (signalIndex >= 0)
            {
                SignalObject thisSignal = SignalObjects[signalIndex];
                thisSignal.Restore(simulator, inf);
                signalIndex = inf.ReadInt32();
            }

            int tcListCount = inf.ReadInt32();

            if (tcListCount != TrackCircuitList.Count)
            {
                Trace.TraceError("Mismatch between saved : {0} and existing : {1} TrackCircuits", tcListCount, TrackCircuitList.Count);
                throw new InvalidDataException("Cannot resume route due to altered data");
            }
            else
            {
                foreach (TrackCircuitSection thisSection in TrackCircuitList)
                {
                    thisSection.Restore(simulator, inf);
                }
            }

            UseLocationPassingPaths = inf.ReadBoolean();

            DeadlockInfoList = new Dictionary<int, DeadlockInfo>();
            int totalDeadlocks = inf.ReadInt32();
            for (int iDeadlock = 0; iDeadlock <= totalDeadlocks - 1; iDeadlock++)
            {
                int thisDeadlockIndex = inf.ReadInt32();
                DeadlockInfo thisInfo = new DeadlockInfo(this, inf);
                DeadlockInfoList.Add(thisDeadlockIndex, thisInfo);
            }

            deadlockIndex = inf.ReadInt32();

            DeadlockReference = new Dictionary<int, int>();
            int totalReferences = inf.ReadInt32();
            for (int iReference = 0; iReference <= totalReferences - 1; iReference++)
            {
                int thisSectionIndex = inf.ReadInt32();
                int thisDeadlockIndex = inf.ReadInt32();
                DeadlockReference.Add(thisSectionIndex, thisDeadlockIndex);
            }
        }

        /// <summary>
        /// Restore Train links
        /// Train links must be restored separately as Trains is restored later as Signals
        /// </summary>
        public void RestoreTrains(List<Train> trains)
        {
            foreach (TrackCircuitSection thisSection in TrackCircuitList)
            {
                thisSection.CircuitState.RestoreTrains(trains, thisSection.Index);
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

            outf.Write(UseLocationPassingPaths);

            outf.Write(DeadlockInfoList.Count);
            foreach (KeyValuePair<int, DeadlockInfo> deadlockDetails in DeadlockInfoList)
            {
                outf.Write(deadlockDetails.Key);
                deadlockDetails.Value.Save(outf);
            }

            outf.Write(deadlockIndex);

            outf.Write(DeadlockReference.Count);
            foreach (KeyValuePair<int, int> referenceDetails in DeadlockReference)
            {
                outf.Write(referenceDetails.Key);
                outf.Write(referenceDetails.Value);
            }

        }

        /// <summary>
        /// Read all world files to get signal flags
        /// </summary>
        private void BuildSignalWorld(Simulator simulator, SignalConfigurationFile sigcfg, CancellationToken cancellation)
        {
            // get all filesnames in World directory

            var WFilePath = simulator.RoutePath + @"\WORLD\";

            var Tokens = new List<TokenID>
            {
                TokenID.Signal,
                TokenID.Speedpost,
                TokenID.Platform,
                TokenID.Pickup
            };

            // loop through files, use only extention .w, skip w+1000000+1000000.w file

            foreach (var fileName in Directory.GetFiles(WFilePath, "*.w"))
            {
                if (cancellation.IsCancellationRequested) return; // ping loader watchdog
                // validate file name a little bit

                if (Path.GetFileName(fileName).Length != 17)
                    continue;

                // read w-file, get SignalObjects only

                Trace.Write("W");
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
                var extendedWFileRead = false;
                foreach (var worldObject in WFile.Tr_Worldfile)
                {
                    if (worldObject.GetType() == typeof(SignalObj))
                    {
                        var thisWorldObject = worldObject as SignalObj;
                        if (thisWorldObject.SignalUnits == null) continue; //this has no unit, will ignore it and treat it as static in scenary.cs

                        //check if signalheads are on same or adjacent tile as signal itself - otherwise there is an invalid match
                        uint? BadSignal = null;
                        foreach (var si in thisWorldObject.SignalUnits.Units)
                        {
                            if (this.trackDB.TrItemTable == null || si.TrItem >= this.trackDB.TrItemTable.Count())
                            {
                                BadSignal = si.TrItem;
                                break;
                            }
                            var item = this.trackDB.TrItemTable[si.TrItem];
                            if (Math.Abs(item.TileX - WFile.TileX) > 1 || Math.Abs(item.TileZ - WFile.TileZ) > 1)
                            {
                                BadSignal = si.TrItem;
                                break;
                            }
                        }
                        if (BadSignal.HasValue)
                        {
                            Trace.TraceWarning("Signal referenced in .w file {0} {1} as TrItem {2} not present in .tdb file ", WFile.TileX, WFile.TileZ, BadSignal.Value);
                            continue;
                        }

                        // if valid, add signal

                        var SignalWorldSignal = new SignalWorldObject(thisWorldObject, sigcfg);
                        SignalWorldList.Add(SignalWorldSignal);
                        foreach (var thisref in SignalWorldSignal.HeadReference)
                        {
                            var thisSignalCount = SignalWorldList.Count() - 1;    // Index starts at 0
                            var thisRefObject = new SignalRefObject(thisSignalCount, thisref.Value);
                            if (!SignalRefList.ContainsKey(thisref.Key))
                            {
                                SignalRefList.Add(thisref.Key, thisRefObject);
                            }
                        }
                    }
                    else if (worldObject is SpeedPostObj speedPostObj)
                    {
                        SpeedPostWorldList.Add(new SpeedPostWorldObject(speedPostObj));
                        int thisSpeedPostId = SpeedPostWorldList.Count() - 1;
                        foreach(TrItemId trItemId in speedPostObj.trItemIDList)
                        {
                            if (!SpeedPostRefList.ContainsKey(trItemId.dbID))
                            {
                                SpeedPostRefList.Add(trItemId.dbID, thisSpeedPostId);
                            }
                        }
                    }
                    else if (worldObject.GetType() == typeof(PlatformObj))
                    {
                        var thisWorldObj = worldObject as PlatformObj;
                        if (!PlatformSidesList.ContainsKey(thisWorldObj.trItemIDList[0].dbID)) PlatformSidesList.Add(thisWorldObj.trItemIDList[0].dbID, thisWorldObj.PlatformData);
                        if (!PlatformSidesList.ContainsKey(thisWorldObj.trItemIDList[0].dbID)) PlatformSidesList.Add(thisWorldObj.trItemIDList[1].dbID, thisWorldObj.PlatformData);
                    }
                    else if (worldObject.GetType() == typeof(PickupObj))
                    {
                        var thisWorldObj = worldObject as PickupObj;
                        if (thisWorldObj.PickupType == (uint)MSTSWagon.PickupType.Container)
                        {
                            if (!extendedWFileRead)
                            {
                                WFilePath = Simulator.RoutePath + @"\World\Openrails\" + Path.GetFileName(fileName);
                                if (File.Exists(WFilePath))
                                {
                                    // We have an OR-specific addition to world file
                                    WFile.InsertORSpecificData(WFilePath, Tokens);
                                    extendedWFileRead = true;
                                }
                            }
                            if (worldObject.QDirection != null && worldObject.Position != null)
                            {
                                var MSTSPosition = worldObject.Position;
                                var MSTSQuaternion = worldObject.QDirection;
                                var XNAQuaternion = new Quaternion((float)MSTSQuaternion.A, (float)MSTSQuaternion.B, -(float)MSTSQuaternion.C, (float)MSTSQuaternion.D);
                                var XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
                                var worldMatrix = new WorldPosition(WFile.TileX, WFile.TileZ, XNAPosition, XNAQuaternion);
                                var containerStation = Simulator.ContainerManager.CreateContainerStation(worldMatrix, from tid in thisWorldObj.TrItemIDList where tid.db == 0 select tid.dbID, thisWorldObj);
                                Simulator.ContainerManager.ContainerHandlingItems.Add(thisWorldObj.TrItemIDList[0].dbID, containerStation);
                            }
                            else
                            {
                                Trace.TraceWarning("Container station {0} within .w file {1} {2} is missing Matrix3x3 and QDirection", worldObject.UID, WFile.TileX, WFile.TileZ);
                            }
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
                if (!reffedObject.HeadReference.TryGetValue(TBDRef, out uint headref))
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

        Stopwatch UpdateTimer = new Stopwatch();
        long UpdateCounter = 0;
        long UpdateTickTarget = 10000;
        // long DebugUpdateCounter = 0;

        /// <summary>
        /// Update : perform signal updates
        /// </summary>
        public void Update(bool preUpdate)
        {
            if (MPManager.IsClient()) return; //in MP, client will not update

            if (foundSignals > 0)
            {
                // loop through all the signals, but only one batch of signals with every call to this method.
                // update one batch of signals. Batch ends when time taken exceeds 1/20th of time for all signals.
                // Processing 1/20th of signals in each batch gave a jerky result as processing time varies greatly.
                // Smoother results now that equal time is given to each batch and let the batch size vary.
                var updates = 0;
                var updateStep = 0;
                var targetTicks = Stopwatch.GetTimestamp() + UpdateTickTarget;
                UpdateTimer.Start();
                while (updateStep < foundSignals)
                {
                    var signal = SignalObjects[(UpdateIndex + updateStep) % foundSignals];
                    if (signal != null && !signal.noupdate) // to cater for orphans, and skip signals which do not require updates
                    {
                        signal.Update();
                        updates++;
                    }
                    updateStep++;

                    // in preupdate, process all
                    if (!preUpdate && updates % 10 == 0 && Stopwatch.GetTimestamp() >= targetTicks) break;
                }
                UpdateCounter += updates;
                UpdateTimer.Stop();

                if (UpdateIndex + updateStep >= foundSignals)
                {
                    // Calculate how long it takes to update all signals and target 1/20th of that
                    // Slow adjustment using clamp stops it jumping around too much
                    var ticksPerSignal = (double)UpdateTimer.ElapsedTicks / UpdateCounter;
                    UpdateTickTarget = (long)MathHelper.Clamp((float)(ticksPerSignal * foundSignals / 20), UpdateTickTarget - 100, UpdateTickTarget + 100);
                    // if (++DebugUpdateCounter % 10 == 0) Trace.WriteLine($"Signal update for {UpdateCounter,5} signals took {(double)UpdateTimer.ElapsedTicks * 1000 / Stopwatch.Frequency,9:F6} ms ({ticksPerSignal * 1000 / Stopwatch.Frequency,9:F6} ms/signal); new {(double)UpdateTickTarget * 1000 / Stopwatch.Frequency,6:F6} ms target");
                    UpdateTimer.Reset();
                    UpdateCounter = 0;
                }
                UpdateIndex = (UpdateIndex + updateStep) % foundSignals;
            }
        }

        /// <summary></summary>
        /// Build signal list from TDB
        /// </summary>
        private void BuildSignalList(TrItem[] TrItems, TrackNode[] trackNodes, TrackSectionsFile tsectiondat,
                TrackDatabaseFile tdbfile, Dictionary<int, int> platformList, List<Milepost> milepostList)
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
                        if (!Speedpost.IsMilePost)
                        {
                            noSignals++;
                        }
                    }
                }
            }

            // set general items and create sections
            if (noSignals > 0)
            {
                SignalObjects = new SignalObject[noSignals];
                SignalObject.signalObjects = SignalObjects;
            }

            SignalObject.trackNodes = trackNodes;
            SignalObject.trItems = TrItems;

            for (int i = 1; i < trackNodes.Length; i++)
            {
                ScanSection(TrItems, trackNodes, i, tsectiondat, tdbfile, platformList, milepostList);
            }

            //  Only continue if one or more signals in route.

            if (noSignals > 0)
            {
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
                            if (trackItem is SignalItem sigItem)
                            {
                                sigItem.SigObj = thisObject.thisRef;
                            }
                            else if (trackItem is SpeedPostItem speedItem)
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
                SignalObjects = new SignalObject[0];
            }
        }

        /// <summary>
        /// Split backfacing signals
        /// </summary>
        private void SplitBackfacing(TrItem[] TrItems, TrackNode[] TrackNodes)
        {
            List<SignalObject> newSignals = new List<SignalObject>();
            int newindex = foundSignals; //the last was placed into foundSignals-1, thus the new ones need to start from foundSignals

            // Loop through all signals to check on Backfacing heads

            for (int isignal = 0; isignal < SignalObjects.Length - 1; isignal++)
            {
                SignalObject singleSignal = SignalObjects[isignal];
                if (singleSignal != null && singleSignal.Type == SignalObjectType.Signal &&
                                singleSignal.WorldObject != null && singleSignal.WorldObject.Backfacing.Count > 0)
                {
                    // create new signal - copy of existing signal
                    // use Backfacing flags and reset head indication

                    SignalObject newSignal = new SignalObject(singleSignal);

                    newSignal.thisRef = newindex;
                    newSignal.trRefIndex = 0;

                    newSignal.WorldObject.FlagsSet = new bool[singleSignal.WorldObject.FlagsSetBackfacing.Length];
                    singleSignal.WorldObject.FlagsSetBackfacing.CopyTo(newSignal.WorldObject.FlagsSet, 0);

                    for (int iindex = 0; iindex < newSignal.WorldObject.HeadsSet.Length; iindex++)
                    {
                        newSignal.WorldObject.HeadsSet[iindex] = false;
                    }

                    // loop through the list with headreferences, check this agains the list with backfacing heads
                    // use the TDBreference to find the actual head

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

                                    // backfacing head found - add to new signal, set to remove from exising signal

                                    if (thisHead.TDBIndex == thisHeadRef.Key)
                                    {
                                        removeHead.Add(ihIndex);

                                        thisHead.mainSignal = newSignal;
                                        newSignal.SignalHeads.Add(thisHead);
                                    }
                                }
                            }

                            // update flags for available heads

                            newSignal.WorldObject.HeadsSet[ihead] = true;
                            singleSignal.WorldObject.HeadsSet[ihead] = false;
                        }
                    }

                    // check if there were actually any backfacing signal heads
                    if (removeHead.Count > 0)
                    {
                        // remove moved heads from existing signal
                        for (int ihead = singleSignal.SignalHeads.Count - 1; ihead >= 0; ihead--)
                        {
                            if (removeHead.Contains(ihead))
                            {
                                singleSignal.SignalHeads.RemoveAt(ihead);
                            }
                        }

                        // Check direction of heads to set correct direction for signal
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

                        // set correct trRefIndex for this signal, and set cross-reference for all backfacing trRef items
                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.NoItemRefs; i++)
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
                                            sigItem.SigObj = newSignal.thisRef;
                                            newSignal.trRefIndex = i;

                                            // remove this key from the original signal //

                                            singleSignal.WorldObject.HeadReference.Remove((uint)TDBRef);
                                        }
                                    }
                                }
                            }
                        }

                        // reset cross-references for original signal (it may have been set for a backfacing head)
                        for (int i = 0; i < TrackNodes[newSignal.trackNode].TrVectorNode.NoItemRefs; i++)
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
                                            sigItem.SigObj = singleSignal.thisRef;
                                            singleSignal.trRefIndex = i;

                                            // remove this key from the new signal //

                                            newSignal.WorldObject.HeadReference.Remove((uint)TDBRef);
                                        }
                                    }
                                }
                            }
                        }

                        // add new signal to signal list
                        newindex++;
                        newSignals.Add(newSignal);

                        // revert existing signal to NULL if no heads remain
                        if (singleSignal.SignalHeads.Count <= 0)
                        {
                            SignalObjects[isignal] = null;
                        }
                    }
                }
            }

            // add all new signals to the signalObject array
            // length of array was set to all possible signals, so there will be space to spare
            newindex = foundSignals;
            foreach (SignalObject newSignal in newSignals)
            {
                SignalObjects[newindex] = newSignal;
                newindex++;
            }

            foundSignals = newindex;
        }

        /// <summary>
        /// ScanSection : This method checks a section in the TDB for signals or speedposts
        /// </summary>
        private void ScanSection(TrItem[] TrItems, TrackNode[] trackNodes, int index,
                               TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile, Dictionary<int, int> platformList, List <Milepost> milepostList)
        {
            int lastSignal = -1;                // Index to last signal found in path; -1 if none
            int lastMilepost = -1;                // Index to last milepost found in path; -1 if none

            if (trackNodes[index].TrEndNode)
                return;

            //  Is it a vector node then it may contain objects.
            if (trackNodes[index].TrVectorNode != null && trackNodes[index].TrVectorNode.NoItemRefs > 0)
            {
                // Any objects ?
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
                            if (!speedItem.IsMilePost)
                            {
                                speedItem.SigObj = foundSignals;

                                lastSignal = AddSpeed(index, i, speedItem, TDBRef, tsectiondat, tdbfile);
                                speedItem.SigObj = lastSignal;
                            }
                            else
                            {
                                speedItem.SigObj = foundMileposts;
                                lastMilepost = AddMilepost(index, i, speedItem, TDBRef, tsectiondat, tdbfile);
                                speedItem.SigObj = lastMilepost;
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
        } 

        /// <summary>
        /// Merge Heads
        /// </summary>
        public void MergeHeads()
        {
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
                                if (MainSignal.trackNode != AddSignal.trackNode)
                                {
                                    Trace.TraceWarning("Signal head {0} in different track node than signal head {1} of same signal", MainSignal.trItem, thisReference.Key);
                                    MainSignal = null;
                                    break;
                                }
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

        /// <summary>
        /// This method adds a new Signal to the list
        /// </summary>
        private int AddSignal(int trackNode, int nodeIndx, SignalItem sigItem, int TDBRef, TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile, ref bool validSignal)
        {
            validSignal = true;

            SignalObjects[foundSignals] = new SignalObject(this, SignalObjectType.Signal);
            SignalObjects[foundSignals].direction = (int)sigItem.Direction;
            SignalObjects[foundSignals].trackNode = trackNode;
            SignalObjects[foundSignals].trRefIndex = nodeIndx;
            SignalObjects[foundSignals].AddHead(nodeIndx, TDBRef, sigItem);
            SignalObjects[foundSignals].thisRef = foundSignals;

            if (tdbfile.TrackDB.TrackNodes[trackNode] == null || tdbfile.TrackDB.TrackNodes[trackNode].TrVectorNode == null)
            {
                validSignal = false;
                Trace.TraceInformation("Reference to invalid track node {0} for Signal {1}\n", trackNode, TDBRef);
            }
            else
            {
                SignalObjects[foundSignals].tdbtraveller =
                new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                        sigItem.TileX, sigItem.TileZ, sigItem.X, sigItem.Z,
                (Traveller.TravellerDirection)(1 - sigItem.Direction));
            }

            SignalObjects[foundSignals].WorldObject = null;

            if (SignalHeadList.ContainsKey((uint)TDBRef))
            {
                validSignal = false;
                Trace.TraceInformation("Invalid double TDBRef {0} in node {1}\n", TDBRef, trackNode);
            }

            if (!validSignal)
            {
                SignalObjects[foundSignals] = null;  // reset signal, do not increase signal count
            }
            else
            {
                SignalHeadList.Add((uint)TDBRef, SignalObjects[foundSignals]);
                foundSignals++;
            }

            return foundSignals - 1;
        }

        /// <summary>
        /// This method adds a new Speedpost to the list
        /// </summary>
        private int AddSpeed(int trackNode, int nodeIndx, SpeedPostItem speedItem, int TDBRef, TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile)
        {
            SignalObjects[foundSignals] = new SignalObject(this, SignalObjectType.SpeedPost);
            SignalObjects[foundSignals].direction = 0;                  // preset - direction not yet known //
            SignalObjects[foundSignals].trackNode = trackNode;
            SignalObjects[foundSignals].trRefIndex = nodeIndx;
            SignalObjects[foundSignals].AddHead(nodeIndx, TDBRef, speedItem);
            SignalObjects[foundSignals].thisRef = foundSignals;

            SignalObjects[foundSignals].tdbtraveller =
            new Traveller(tsectiondat, tdbfile.TrackDB.TrackNodes, tdbfile.TrackDB.TrackNodes[trackNode],
                    speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z,
                    (Traveller.TravellerDirection)SignalObjects[foundSignals].direction);

            double delta_angle = SignalObjects[foundSignals].tdbtraveller.RotY - ((Math.PI / 2) - speedItem.Angle);
            float delta_float = MathHelper.WrapAngle((float)delta_angle);
            if (Math.Abs(delta_float) < (Math.PI / 2))
            {
                SignalObjects[foundSignals].direction = SignalObjects[foundSignals].tdbtraveller.Direction == 0 ? 1 : 0;
            }
            else
            {
                SignalObjects[foundSignals].direction = (int)SignalObjects[foundSignals].tdbtraveller.Direction;
                SignalObjects[foundSignals].tdbtraveller.ReverseDirection();
            }

#if DEBUG_PRINT
            File.AppendAllText(@"C:\temp\speedpost.txt",
                string.Format("\nPlaced : at : {0} {1}:{2} {3}; angle - track : {4}:{5}; delta : {6}; dir : {7}\n",
                speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z,
                speedItem.Angle, SignalObjects[foundSignals].tdbtraveller.RotY,
                delta_angle,
                SignalObjects[foundSignals].direction));
#endif

            SignalObjects[foundSignals].WorldObject = null;
            foundSignals++;
            return foundSignals - 1;
        }

        /// <summary>
        /// This method adds a new Milepost to the list
        /// </summary>
        private int AddMilepost(int trackNode, int nodeIndx, SpeedPostItem speedItem, int TDBRef, TrackSectionsFile tsectiondat, TrackDatabaseFile tdbfile)
        {
            Milepost milepost = new Milepost();
            milepost.TrItemId = (uint)TDBRef;
            milepost.MilepostValue = speedItem.SpeedInd;
            MilepostList.Add(milepost);
 
#if DEBUG_PRINT
            File.AppendAllText(@"C:\temp\speedpost.txt",
                string.Format("\nMilepost placed : at : {0} {1}:{2} {3}. String: {4}\n",
                speedItem.TileX, speedItem.TileZ, speedItem.X, speedItem.Z, speedItem.SpeedInd));
#endif

            foundMileposts = MilepostList.Count;
            return foundMileposts - 1;
        }

        /// <summary>
        /// Add the sigcfg reference to each signal object.
        /// </summary>
        private void AddCFG(SignalConfigurationFile sigCFG)
        {
            foreach (SignalObject signal in SignalObjects)
            {
                signal?.SetSignalType(sigCFG);
            }
        }

        /// <summary>
        /// Add info from signal world objects to signal
        /// </summary>
        private void AddWorldInfo()
        {
            // loop through all signal and all heads
            foreach (SignalObject signal in SignalObjects)
            {
                if (signal != null)
                {
                    if (signal.Type == SignalObjectType.Signal || signal.Type == SignalObjectType.SpeedSignal)
                    {
                        foreach (SignalHead head in signal.SignalHeads)
                        {
                            // get reference using TDB index from head
                            uint TDBRef = Convert.ToUInt32(head.TDBIndex);

                            if (SignalRefList.TryGetValue(TDBRef, out SignalRefObject thisRef))
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
                    else
                    {
                        SignalHead head = signal.SignalHeads[0];

                        if (SpeedPostRefList.TryGetValue(head.TDBIndex, out int speedPostIndex))
                        {
                            if (signal.SpeedPostWorldObject == null)
                            {
                                signal.SpeedPostWorldObject = SpeedPostWorldList[speedPostIndex];
                            }
                            SpeedPostRefList.Remove(head.TDBIndex);
                        }
                    }
                }
            }
        }
        
        private void InitializeSignals()
        {
            foreach (SignalObject signal in SignalObjects)
            {
                if (signal != null)
                {
                    if (signal.Type == SignalObjectType.Signal || signal.Type == SignalObjectType.SpeedSignal)
                    {
                        signal.Initialize();
                    }
                }
            }
        }

        /// <summary>
        /// FindByTrItem : find required signalObj + signalHead
        /// </summary>
        public KeyValuePair<SignalObject, SignalHead>? FindByTrItem(uint trItem)
        {
            foreach (var signal in SignalObjects)
                if (signal != null)
                    foreach (var head in signal.SignalHeads)
                        if (SignalObject.trackNodes[signal.trackNode].TrVectorNode.TrItemRefs[head.trItemIndex] == (int)trItem)
                            return new KeyValuePair<SignalObject, SignalHead>(signal, head);
            return null;
        }

        /// <summary>
        /// Count number of normal signal heads
        /// </summary>
        public void SetNumSignalHeads()
        {
            foreach (SignalObject thisSignal in SignalObjects)
            {
                if (thisSignal != null)
                {
                    foreach (SignalHead thisHead in thisSignal.SignalHeads)
                    {
                        if (thisHead.Function == SignalFunction.NORMAL)
                        {
                            thisSignal.SignalNumNormalHeads++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find_Next_Object_InRoute : find next item along path of train - using Route List (only forward)
        /// Objects to search for : SpeedPost, Signal
        ///
        /// Usage :
        ///   always set : RouteList, RouteNodeIndex, distance along RouteNode, fnType
        ///
        ///   from train :
        ///     optional : maxdistance
        ///
        /// returned :
        ///   >= 0 : signal object reference
        ///   -1  : end of track 
        ///   -3  : no item within required distance
        ///   -5  : end of authority
        ///   -6  : end of (sub)route
        /// </summary>
        public TrackCircuitSignalItem Find_Next_Object_InRoute(Train.TCSubpathRoute routePath,
                int routeIndex, float routePosition, float maxDistance, SignalFunction function, Train.TrainRouted thisTrain)
        {
            ObjectItemInfo.ObjectItemFindState locstate = ObjectItemInfo.ObjectItemFindState.None;
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

            // loop through trackcircuits until :
            //  - end of track or route is found
            //  - end of authorization is found
            //  - required item is found
            //  - max distance is covered
            while (locstate == ObjectItemInfo.ObjectItemFindState.None)
            {
                // normal signal
                if (function == SignalFunction.NORMAL)
                {
                    if (thisSection.EndSignals[actDirection] != null)
                    {
                        foundObject = thisSection.EndSignals[actDirection];
                        totalLength += thisSection.Length - lengthOffset;
                        locstate = ObjectItemInfo.ObjectItemFindState.Object;
                    }
                }
                // speedpost
                else if (function == SignalFunction.SPEED)
                {
                    TrackCircuitSignalList thisSpeedpostList =
                               thisSection.CircuitItems.TrackCircuitSpeedPosts[actDirection];
                    locstate = ObjectItemInfo.ObjectItemFindState.None;

                    for (int iPost = 0;
                             iPost < thisSpeedpostList.TrackCircuitItem.Count &&
                                     locstate == ObjectItemInfo.ObjectItemFindState.None;
                             iPost++)
                    {
                        TrackCircuitSignalItem thisSpeedpost = thisSpeedpostList.TrackCircuitItem[iPost];
                        if (thisSpeedpost.SignalLocation > lengthOffset)
                        {
                            ObjectSpeedInfo thisSpeed = thisSpeedpost.SignalRef.this_sig_speed(SignalFunction.SPEED);

                            // set signal in list if there is no train or if signal has active speed
                            if (thisTrain == null ||
                                (thisSpeed != null &&
                                (thisSpeed.speed_flag == 1 || thisSpeed.speed_reset == 1 ||
                                (thisTrain.Train.IsFreight && thisSpeed.speed_freight != -1) || (!thisTrain.Train.IsFreight && thisSpeed.speed_pass != -1))))
                            {
                                locstate = ObjectItemInfo.ObjectItemFindState.Object;
                                foundObject = thisSpeedpost.SignalRef;
                                totalLength += thisSpeedpost.SignalLocation - lengthOffset;
                            }
                            // also set signal in list if it is a speed signal as state of speed signal may change
                            else if (thisSpeedpost.SignalRef.Type == SignalObjectType.SpeedSignal)
                            {
                                locstate = ObjectItemInfo.ObjectItemFindState.Object;
                                foundObject = thisSpeedpost.SignalRef;
                                totalLength += thisSpeedpost.SignalLocation - lengthOffset;
                            }
                        }
                    }
                }
                // all function types
                else if (function == null)
                {
                    List<TrackCircuitSignalItem> signalList = new List<TrackCircuitSignalItem>();

                    signalList = thisSection.CircuitItems.TrackCircuitSignals[actDirection].Select(x => x.Value.TrackCircuitItem).Aggregate((acc, list) => acc.Concat(list).ToList());

                    signalList.Sort((a, b) => a.SignalLocation < b.SignalLocation ? -1 : a.SignalLocation > b.SignalLocation ? 1 : 0);

                    locstate = ObjectItemInfo.ObjectItemFindState.None;

                    foreach (TrackCircuitSignalItem thisSignal in signalList)
                    {
                        if (thisSignal.SignalLocation > lengthOffset)
                        {
                            locstate = ObjectItemInfo.ObjectItemFindState.Object;
                            foundObject = thisSignal.SignalRef;
                            totalLength += thisSignal.SignalLocation - lengthOffset;
                            break;
                        }
                    }
                }
                // other fn_types
                else
                {
                    TrackCircuitSignalList thisSignalList =
                        thisSection.CircuitItems.TrackCircuitSignals[actDirection][function];
                    locstate = ObjectItemInfo.ObjectItemFindState.None;

                    foreach (TrackCircuitSignalItem thisSignal in thisSignalList.TrackCircuitItem)
                    {
                        if (thisSignal.SignalLocation > lengthOffset)
                        {
                            locstate = ObjectItemInfo.ObjectItemFindState.Object;
                            foundObject = thisSignal.SignalRef;
                            totalLength += thisSignal.SignalLocation - lengthOffset;
                            break;
                        }
                    }
                }

                // next section accessed via next route element
                if (locstate == ObjectItemInfo.ObjectItemFindState.None)
                {
                    totalLength += thisSection.Length - lengthOffset;
                    lengthOffset = 0;

                    int setSection = thisSection.ActivePins[thisElement.OutPin[0], thisElement.OutPin[1]].Link;
                    actRouteIndex++;

                    if (setSection < 0)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.EndOfAuthority;
                    }
                    else if (actRouteIndex >= routePath.Count)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.EndOfPath;
                    }
                    else if (maxDistance > 0 && totalLength > maxDistance)
                    {
                        locstate = ObjectItemInfo.ObjectItemFindState.PassedMaximumDistance;
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

            return thisItem;
        }

        /// <summary>
        /// GetNextObject_InRoute : find next item along path of train - using Route List (only forward)
        ///
        /// Usage :
        ///   always set : Train (may be null), RouteList, RouteNodeIndex, distance along RouteNode, fn_type
        ///
        ///   from train :
        ///     optional : maxdistance
        ///
        /// returned :
        ///   >= 0 : signal object reference
        ///   -1  : end of track 
        ///   -2  : passed signal at danger
        ///   -3  : no item within required distance
        ///   -5  : end of authority
        ///   -6  : end of (sub)route
        /// </summary>
        // call without position
        public ObjectItemInfo GetNextObject_InRoute(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePath,
                    int routeIndex, float routePosition, float maxDistance, ObjectItemInfo.ObjectItemType req_type)
        {

            Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

            return GetNextObject_InRoute(thisTrain, routePath, routeIndex, routePosition, maxDistance, req_type, thisPosition);
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

            if (req_type == ObjectItemInfo.ObjectItemType.Any ||
                req_type == ObjectItemInfo.ObjectItemType.Signal)
            {
                findSignal = true;
            }

            if (req_type == ObjectItemInfo.ObjectItemType.Any ||
                req_type == ObjectItemInfo.ObjectItemType.Speedlimit)
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
            ObjectItemInfo.ObjectItemFindState signalState = ObjectItemInfo.ObjectItemFindState.None;

            TrackCircuitSignalItem nextSignal =
                Find_Next_Object_InRoute(usedRoute, routeIndex, routePosition,
                        maxDistance, SignalFunction.NORMAL, thisTrain);

            signalState = nextSignal.SignalState;
            if (nextSignal.SignalState == ObjectItemInfo.ObjectItemFindState.Object)
            {
                signalDistance = nextSignal.SignalLocation;
                SignalObject foundSignal = nextSignal.SignalRef;
                if (foundSignal.this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP)
                {
                    signalState = ObjectItemInfo.ObjectItemFindState.PassedDanger;
                }
                else if (thisTrain != null && foundSignal.enabledTrain != thisTrain)
                {
                    signalState = ObjectItemInfo.ObjectItemFindState.PassedDanger;
                    nextSignal.SignalState = signalState;  // do not return OBJECT_FOUND - signal is not valid
                }
            }

            // look for speedpost only if required
            if (findSpeedpost)
            {
                TrackCircuitSignalItem nextSpeedpost =
                    Find_Next_Object_InRoute(usedRoute, routeIndex, routePosition,
                        maxDistance, SignalFunction.SPEED, thisTrain);

                if (nextSpeedpost.SignalState == ObjectItemInfo.ObjectItemFindState.Object)
                {
                    speedpostDistance = nextSpeedpost.SignalLocation;
                    SignalObject foundSignal = nextSpeedpost.SignalRef;
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
                            if (signalState == ObjectItemInfo.ObjectItemFindState.PassedDanger)
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
                returnItem = new ObjectItemInfo(ObjectItemInfo.ObjectItemFindState.None);
            }
            else if (foundItem.SignalState != ObjectItemInfo.ObjectItemFindState.Object)
            {
                returnItem = new ObjectItemInfo(foundItem.SignalState);
            }
            else
            {
                returnItem = new ObjectItemInfo(foundItem.SignalRef, foundItem.SignalLocation);
            }

            return returnItem;
        }

        public ObjectItemInfo GetNextSignal_InRoute(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePath,
            int routeIndex, float routePosition, float maxDistance, Train.TCPosition thisPosition, SignalFunction requiredSignalFunction = null)
        {
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

            TrackCircuitSignalItem nextSignal = Find_Next_Object_InRoute(usedRoute, routeIndex, routePosition, maxDistance, requiredSignalFunction, thisTrain);

            if (nextSignal.SignalState == ObjectItemInfo.ObjectItemFindState.Object)
            {
                if (thisTrain != null && nextSignal.SignalRef.enabledTrain != thisTrain)
                {
                    nextSignal.SignalState = ObjectItemInfo.ObjectItemFindState.PassedDanger;  // do not return OBJECT_FOUND - signal is not valid
                }
            }

            ObjectItemInfo returnItem;
            if (nextSignal == null)
            {
                returnItem = new ObjectItemInfo(ObjectItemInfo.ObjectItemFindState.None);
            }
            else if (nextSignal.SignalState != ObjectItemInfo.ObjectItemFindState.Object)
            {
                returnItem = new ObjectItemInfo(nextSignal.SignalState);
            }
            else
            {
                returnItem = new ObjectItemInfo(nextSignal.SignalRef, nextSignal.SignalLocation);
            }

            return returnItem;
        }

        /// <summary>
        /// Gets the Track Monitor Aspect from the MSTS aspect (for the TCS) 
        /// </summary>
        public TrackMonitorSignalAspect TranslateToTCSAspect(MstsSignalAspect SigState)
        {
            switch (SigState)
            {
                case MstsSignalAspect.STOP:
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
        }

        /// <summary>
        /// Create Track Circuits
        /// <summary>
#if ACTIVITY_EDITOR
        private void CreateTrackCircuits(TrItem[] TrItems, TrackNode[] trackNodes, TrackSectionsFile tsectiondat, ORRouteConfig orRouteConfig)
#else
        private void CreateTrackCircuits(TrItem[] TrItems, TrackNode[] trackNodes, TSectionDatFile tsectiondat)
#endif
        {
            // Create dummy element as first to keep indexes equal

            TrackCircuitList = new List<TrackCircuitSection>
            {
                new TrackCircuitSection(0, this)
            };

            // Create new default elements from existing base

            for (int iNode = 1; iNode < trackNodes.Length; iNode++)
            {
                TrackNode trackNode = trackNodes[iNode];
                TrackCircuitSection defaultSection =
                    new TrackCircuitSection(trackNode, iNode, tsectiondat, this);
                TrackCircuitList.Add(defaultSection);
            }

            // loop through original default elements
            // collect track items

            int originalNodes = TrackCircuitList.Count;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                ProcessNodes(iNode, TrItems, trackNodes, tsectiondat);
            }

            // Delete MilepostList as it is no more needed
            MilepostList.Clear();
            foundMileposts = -1;
            MilepostList = null;

#if ACTIVITY_EDITOR
            //  Loop through original default elements to complete the track items with the OR ones

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                List<TrackCircuitElement> elements = orRouteConfig.GetORItemForNode(iNode, trackNodes, tsectiondat);
                TrackCircuitList[iNode].CircuitItems.TrackCircuitElements = elements;
            }
#endif

            // loop through original default elements
            // split on crossover items

            originalNodes = TrackCircuitList.Count;
            int nextNode = originalNodes;
            foreach (KeyValuePair<int, CrossOverItem> CrossOver in CrossoverList)
            {
                nextNode = SplitNodesCrossover(CrossOver.Value, tsectiondat, nextNode);
            }

            // loop through original default elements
            // split on normal signals

            originalNodes = TrackCircuitList.Count;
            nextNode = originalNodes;

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                nextNode = SplitNodesSignals(iNode, nextNode);
            }

#if ACTIVITY_EDITOR
            // loop through original default elements
            // split on OR Elements

            originalNodes = TrackCircuitList.Count;
            nextNode = originalNodes;

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                nextNode = SplitNodesElements(iNode, nextNode);
            }
#endif
            // loop through all items
            // perform link test

            originalNodes = TrackCircuitList.Count;
            nextNode = originalNodes;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                nextNode = performLinkTest(iNode, nextNode);
            }

            // loop through all items
            // reset active links
            // set fixed active links for none-junction links
            // set trailing junction flags

            originalNodes = TrackCircuitList.Count;
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setActivePins(iNode);
            }

            // Set cross-reference

            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setCrossReference(iNode, trackNodes);
            }
            for (int iNode = 1; iNode < originalNodes; iNode++)
            {
                setCrossReferenceCrossOver(iNode, trackNodes);
            }

            // Set cross-reference for signals

            for (int iNode = 0; iNode < TrackCircuitList.Count; iNode++)
            {
                setSignalCrossReference(iNode);
            }

            // Set default next signal and fixed route information

            for (int iSignal = 0; SignalObjects != null && iSignal < SignalObjects.Length; iSignal++)
            {
                SignalObject thisSignal = SignalObjects[iSignal];
                if (thisSignal != null)
                {
                    thisSignal.setSignalDefaultNextSignal();
                }
            }
        }

        /// <summary>
        /// Print TC Information
        /// </summary>
        void PrintTCBase(TrackNode[] trackNodes)
        {
            // Test : print TrackCircuitList

#if DEBUG_PRINT
            if (File.Exists(@"C:\temp\TCBase.txt"))
            {
                File.Delete(@"C:\temp\TCBase.txt");
            }

            var tcbb = new StringBuilder();
            for (var iNode = 0; iNode < TrackCircuitList.Count; iNode++)
            {
                var thisSection = TrackCircuitList[iNode];
                tcbb.AppendFormat("\nIndex : {0}\n", iNode);
                tcbb.Append("{\n");
                tcbb.AppendFormat("     Section    : {0}\n", thisSection.Index);
                tcbb.AppendFormat("     OrgSection : {0}\n", thisSection.OriginalIndex);
                tcbb.AppendFormat("     Type       : {0}\n", thisSection.CircuitType);

                tcbb.AppendFormat("     Pins (0,0) : {0} {1}\n", thisSection.Pins[0, 0].Direction, thisSection.Pins[0, 0].Link);
                tcbb.AppendFormat("     Pins (0,1) : {0} {1}\n", thisSection.Pins[0, 1].Direction, thisSection.Pins[0, 1].Link);
                tcbb.AppendFormat("     Pins (1,0) : {0} {1}\n", thisSection.Pins[1, 0].Direction, thisSection.Pins[1, 0].Link);
                tcbb.AppendFormat("     Pins (1,1) : {0} {1}\n", thisSection.Pins[1, 1].Direction, thisSection.Pins[1, 1].Link);

                tcbb.AppendFormat("     Active Pins (0,0) : {0} {1}\n", thisSection.ActivePins[0, 0].Direction, thisSection.ActivePins[0, 0].Link);
                tcbb.AppendFormat("     Active Pins (0,1) : {0} {1}\n", thisSection.ActivePins[0, 1].Direction, thisSection.ActivePins[0, 1].Link);
                tcbb.AppendFormat("     Active Pins (1,0) : {0} {1}\n", thisSection.ActivePins[1, 0].Direction, thisSection.ActivePins[1, 0].Link);
                tcbb.AppendFormat("     Active Pins (1,1) : {0} {1}\n", thisSection.ActivePins[1, 1].Direction, thisSection.ActivePins[1, 1].Link);

                if (thisSection.EndIsTrailingJunction[0])
                {
                    tcbb.Append("     Trailing Junction : direction 0\n");
                }

                if (thisSection.EndIsTrailingJunction[1])
                {
                    tcbb.Append("     Trailing Junction : direction 1\n");
                }

                tcbb.AppendFormat("     Length         : {0}\n", thisSection.Length);
                tcbb.AppendFormat("     OffsetLength 0 : {0}\n", thisSection.OffsetLength[0]);
                tcbb.AppendFormat("     OffsetLength 1 : {0}\n", thisSection.OffsetLength[1]);

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal && thisSection.CircuitItems != null)
                {
                    tcbb.Append("\nSignals : \n");
                    for (var iDirection = 0; iDirection <= 1; iDirection++)
                    {
                        if (thisSection.EndSignals[iDirection] != null)
                        {
                            tcbb.AppendFormat("    End Signal {0} : {1}\n", iDirection, thisSection.EndSignals[iDirection].thisRef);
                        }

                        foreach (SignalFunction function in SignalFunctions.Values)
                        {
                            tcbb.AppendFormat("    Direction {0} - Function : {1} : \n", iDirection, function);
                            var thisSignalList = thisSection.CircuitItems.TrackCircuitSignals[iDirection][function];
                            foreach (var thisItem in thisSignalList.TrackCircuitItem)
                            {
                                var thisSignal = thisItem.SignalRef;
                                var signalDistance = thisItem.SignalLocation;

                                if (thisSignal.WorldObject == null)
                                {
                                    tcbb.AppendFormat("         {0} = **UNKNOWN** at {1}\n", thisSignal.thisRef, signalDistance);
                                }
                                else
                                {
                                    tcbb.AppendFormat("         {0} = {1} at {2}\n", thisSignal.thisRef, thisSignal.WorldObject.SFileName, signalDistance);
                                }
                            }
                            tcbb.Append("\n");
                        }
                    }

                    tcbb.Append("\nSpeedposts : \n");
                    for (var iDirection = 0; iDirection <= 1; iDirection++)
                    {
                        tcbb.AppendFormat("    Direction {0}\n", iDirection);

                        var thisSpeedpostList = thisSection.CircuitItems.TrackCircuitSpeedPosts[iDirection];
                        foreach (var thisItem in thisSpeedpostList.TrackCircuitItem)
                        {
                            var thisSpeedpost = thisItem.SignalRef;
                            var speedpostDistance = thisItem.SignalLocation;

                            var speedInfo = new ObjectItemInfo(thisSpeedpost, speedpostDistance);
                            tcbb.AppendFormat("{0} = pass : {1} ; freight : {2} - at distance {3}\n", thisSpeedpost.thisRef, speedInfo.speed_passenger, speedInfo.speed_freight, speedpostDistance);
                        }

                        tcbb.Append("\n");
                    }
                }
                else if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                {
                    tcbb.AppendFormat("    Overlap : {0}\n", thisSection.Overlap);
                }

                if (thisSection.TunnelInfo != null && thisSection.TunnelInfo.Count > 0)
                {
                    tcbb.Append("\nTunnel Info : \n");
                    foreach (TrackCircuitSection.tunnelInfoData[] thisTunnelInfo in thisSection.TunnelInfo)
                    {
                        tcbb.AppendFormat("\nDirection 0 : Start : {0} ; End : {1} ; Length in TCS : {2} ; Overall length : {3} ; Tunnel offset : {4} \n",
                            thisTunnelInfo[0].TunnelStart, thisTunnelInfo[0].TunnelEnd, thisTunnelInfo[0].LengthInTCS, thisTunnelInfo[0].TotalLength, thisTunnelInfo[0].TCSStartOffset);
                        tcbb.AppendFormat("\nDirection 1 : Start : {0} ; End : {1} ; Length in TCS : {2} ; Overall length : {3} ; Tunnel offset : {4} \n",
                            thisTunnelInfo[1].TunnelStart, thisTunnelInfo[1].TunnelEnd, thisTunnelInfo[1].LengthInTCS, thisTunnelInfo[1].TotalLength, thisTunnelInfo[1].TCSStartOffset);
                    }
                }

                tcbb.Append("}\n");
            }

            tcbb.Append("\n\nCROSSOVERS\n\n");
            foreach (var CrossItem in CrossoverList)
            {
                var thisCross = CrossItem.Value;
                tcbb.AppendFormat("   Indices : {0} - {1}\n", thisCross.ItemIndex[0], thisCross.ItemIndex[1]);
                tcbb.AppendFormat("   Sections: {0} - {1}\n", thisCross.SectionIndex[0], thisCross.SectionIndex[1]);
                tcbb.Append("\n");
            }

            tcbb.Append("\n\nTRACK SECTIONS\n\n");
            foreach (var thisTrack in trackNodes)
            {
                if (thisTrack == null)
                {
                }
                else if (thisTrack.TCCrossReference == null)
                {
                    tcbb.Append("   ERROR : no track circuit cross-reference \n");
                    Trace.TraceWarning("ERROR : Track Node without Track Circuit cross-reference");
                }
                else
                {
                    var thisXRef = thisTrack.TCCrossReference;
                    var thisSection = TrackCircuitList[thisXRef[0].Index];
                    tcbb.AppendFormat("     Original node : {0}\n", thisSection.OriginalIndex);

                    foreach (var thisReference in thisXRef)
                    {
                        tcbb.AppendFormat("        Ref Index : {0} : " + "Length : {1} at : {2} - {3}\n", thisReference.Index, thisReference.Length, thisReference.OffsetLength[0], thisReference.OffsetLength[1]);
                    }
                    tcbb.Append("\n");

                    if (thisXRef[thisXRef.Count - 1].OffsetLength[1] != 0)
                    {
                        tcbb.Append(" >>> INVALID XREF\n");
                    }
                }
            }

            tcbb.Append("\n\n PLATFORMS \n --------- \n\n");

            foreach (var platformXRef in PlatformXRefList)
            {
                var thisPlatform = PlatformDetailsList[platformXRef.Value];

                tcbb.AppendFormat("Index {0} : Platform {1} [{2} ,{3}]\n", platformXRef.Key, platformXRef.Value, thisPlatform.PlatformReference[0], thisPlatform.PlatformReference[1]);
            }

            tcbb.Append("\n\n");

            for (var iPlatform = 0; iPlatform < PlatformDetailsList.Count; iPlatform++)
            {
                var thisPlatform = PlatformDetailsList[iPlatform];

                tcbb.AppendFormat("Platform : {0}\n", iPlatform);

                tcbb.AppendFormat("Name     : {0}\n", thisPlatform.Name);
                tcbb.AppendFormat("Time     : {0}\n", thisPlatform.MinWaitingTime);

                tcbb.AppendFormat("Sections : ");
                for (var iSection = 0; iSection < thisPlatform.TCSectionIndex.Count; iSection++)
                {
                    tcbb.AppendFormat(" " + thisPlatform.TCSectionIndex[iSection]);
                }
                tcbb.AppendFormat("\n");

                tcbb.AppendFormat("Platform References    : {0} + {1}\n", thisPlatform.PlatformReference[0], thisPlatform.PlatformReference[1]);

                tcbb.AppendFormat("Section Offset : [0,0] : {0}\n", thisPlatform.TCOffset[0, 0]);
                tcbb.AppendFormat("                 [0,1] : {0}\n", thisPlatform.TCOffset[0, 1]);
                tcbb.AppendFormat("                 [1,0] : {0}\n", thisPlatform.TCOffset[1, 0]);
                tcbb.AppendFormat("                 [1,1] : {0}\n", thisPlatform.TCOffset[1, 1]);

                tcbb.AppendFormat("Length                 : {0}\n", thisPlatform.Length);

                tcbb.AppendFormat("Node Offset    : [0]   : {0}\n", thisPlatform.nodeOffset[0]);
                tcbb.AppendFormat("Node Offset    : [1]   : {0}\n", thisPlatform.nodeOffset[1]);

                if (thisPlatform.EndSignals[0] == -1)
                {
                    tcbb.AppendFormat("End Signal     : [0]   : -None-\n");
                }
                else
                {
                    tcbb.AppendFormat("End Signal     : [0]   : {0}\n", thisPlatform.EndSignals[0]);
                    tcbb.AppendFormat("Distance               : {0}\n", thisPlatform.DistanceToSignals[0]);
                }
                if (thisPlatform.EndSignals[1] == -1)
                {
                    tcbb.AppendFormat("End Signal     : [1]   : -None-\n");
                }
                else
                {
                    tcbb.AppendFormat("End Signal     : [1]   : {0}\n", thisPlatform.EndSignals[1]);
                    tcbb.AppendFormat("Distance               : {0}\n", thisPlatform.DistanceToSignals[1]);
                }

                tcbb.Append("\n");
            }
            File.AppendAllText(@"C:\temp\TCBase.txt", tcbb.ToString());
#endif
        }

        /// <summary>
        /// ProcessNodes
        /// </summary>
        public void ProcessNodes(int iNode, TrItem[] TrItems, TrackNode[] trackNodes, TrackSectionsFile tsectiondat)
        {
            // Check if original tracknode had trackitems

            TrackCircuitSection thisCircuit = TrackCircuitList[iNode];
            TrackNode thisNode = trackNodes[thisCircuit.OriginalIndex];

            if (thisNode.TrVectorNode != null && thisNode.TrVectorNode.NoItemRefs > 0)
            {
                // Create TDBtraveller at start of section to calculate distances

                TrVectorSection firstSection = thisNode.TrVectorNode.TrVectorSections[0];
                Traveller TDBTrav = new Traveller(tsectiondat, trackNodes, thisNode,
                                firstSection.TileX, firstSection.TileZ,
                                firstSection.X, firstSection.Z, (Traveller.TravellerDirection)1);

                // Process all items (do not split yet)

                float[] lastDistance = new float[2] { -1.0f, -1.0f };
                for (int iRef = 0; iRef < thisNode.TrVectorNode.NoItemRefs; iRef++)
                {
                    int TDBRef = thisNode.TrVectorNode.TrItemRefs[iRef];
                    if (TrItems[TDBRef] != null)
                    {
                        lastDistance = InsertNode(thisCircuit, TrItems[TDBRef], TDBTrav, trackNodes, lastDistance);
                    }
                }
            }
        }

        /// <summary>
        /// InsertNode
        /// </summary>
        public float[] InsertNode(TrackCircuitSection thisCircuit, TrItem thisItem,
                        Traveller TDBTrav, TrackNode[] trackNodes, float[] lastDistance)
        {
            float[] newLastDistance = new float[2];
            lastDistance.CopyTo(newLastDistance, 0);

            // Insert signal
            if (thisItem.ItemType == TrItem.trItemType.trSIGNAL)
            {
                try
                {
                    SignalItem tryItem = (SignalItem)thisItem;
                }
                catch (Exception error)
                {
                    Trace.TraceWarning(error.Message);
                    Trace.TraceWarning("Signal item not consistent with signal database");
                    return newLastDistance;
                }

                SignalItem sigItem = (SignalItem)thisItem;
                if (sigItem.SigObj >= 0)
                {
                    SignalObject thisSignal = SignalObjects[sigItem.SigObj];
                    if (thisSignal == null)
                    {
                        Trace.TraceWarning("Signal item with TrItemID = {0} not consistent with signal database", sigItem.TrItemId);
                        return newLastDistance;
                    }
                    float signalDistance = thisSignal.DistanceTo(TDBTrav);
                    if (thisSignal.direction == 1)
                    {
                        signalDistance = thisCircuit.Length - signalDistance;
                    }

                    foreach (SignalFunction function in SignalFunctions.Values)
                    {
                        if (thisSignal.isORTSSignalType(function))
                        {
                            TrackCircuitSignalItem thisTCItem =
                                    new TrackCircuitSignalItem(thisSignal, signalDistance);

                            int directionList = thisSignal.direction == 0 ? 1 : 0;
                            TrackCircuitSignalList thisSignalList =
                                    thisCircuit.CircuitItems.TrackCircuitSignals[directionList][function];

                            // if signal is SPEED type, insert in speedpost list
                            if (function == SignalFunction.SPEED)
                            {
                                thisSignalList = thisCircuit.CircuitItems.TrackCircuitSpeedPosts[directionList];
                            }

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
            // Insert speedpost
            else if (thisItem.ItemType == TrItem.trItemType.trSPEEDPOST)
            {
                SpeedPostItem speedItem = (SpeedPostItem)thisItem;
                if (speedItem.SigObj >= 0)
                {
                    if (!speedItem.IsMilePost)
                    { 
                        SignalObject thisSpeedpost = SignalObjects[speedItem.SigObj];
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

                    // Milepost
                    else if (speedItem.IsMilePost)
                    {
                        Milepost thisMilepost = MilepostList[speedItem.SigObj];
                        TrItem milepostTrItem = Simulator.TDB.TrackDB.TrItemTable[thisMilepost.TrItemId];
                        float milepostDistance = TDBTrav.DistanceTo(milepostTrItem.TileX, milepostTrItem.TileZ, milepostTrItem.X, milepostTrItem.Y, milepostTrItem.Z);

                        TrackCircuitMilepost thisTCItem =
                                new TrackCircuitMilepost(thisMilepost, milepostDistance, thisCircuit.Length - milepostDistance);

                        List<TrackCircuitMilepost> thisMilepostList =
                                thisCircuit.CircuitItems.TrackCircuitMileposts;
                        thisMilepostList.Add(thisTCItem);
                    }
                }
            }
            // Insert crossover in special crossover list
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

                if (CrossoverList.ContainsKey(crossId))
                {
                    exItem = CrossoverList[crossId];
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

                    exItem.TrackShape = crossItem.ShapeId;

                    CrossoverList.Add(thisId, exItem);
                }
            }

            return newLastDistance;
        }

        /// <summary>
        /// Split on Signals
        /// </summary>
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
            if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
            {
                addIndex.Add(thisNode);

                List<TrackCircuitSignalItem> sectionSignals =
                         thisSection.CircuitItems.TrackCircuitSignals[0][SignalFunction.NORMAL].TrackCircuitItem;

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
                    sectionSignals = thisSection.CircuitItems.TrackCircuitSignals[0][SignalFunction.NORMAL].TrackCircuitItem;
                }
            }

            // in direction 1, check original item and all added items
            foreach (int actIndex in addIndex)
            {
                thisIndex = actIndex;

                while (thisIndex > 0)
                {
                    thisSection = TrackCircuitList[thisIndex];

                    newIndex = -1;
                    if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
                    {

                        List<TrackCircuitSignalItem> sectionSignals =
                           thisSection.CircuitItems.TrackCircuitSignals[1][SignalFunction.NORMAL].TrackCircuitItem;

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
                            sectionSignals = thisSection.CircuitItems.TrackCircuitSignals[1][SignalFunction.NORMAL].TrackCircuitItem;
                        }
                    }
                    thisIndex = thisSection.CircuitItems.TrackCircuitSignals[1][SignalFunction.NORMAL].TrackCircuitItem.Count > 0 ? thisIndex : newIndex;
                }
            }

            return nextNode;
        }

        /// <summary>
        /// Split CrossOvers
        /// </summary>
        private int SplitNodesCrossover(CrossOverItem CrossOver,
                TrackSectionsFile tsectiondat, int nextNode)
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

            return nextNode;
        }

#if ACTIVITY_EDITOR
        /// <summary>
        /// Split on OR Elements
        /// </summary>
        private int SplitNodesElements(int thisNode, int nextNode)
        {
            int thisIndex = thisNode;
            int newIndex = -1;
            List<int> addIndex = new List<int>();

            // in direction 0, check original item only
            // keep list of added items

            TrackCircuitSection thisSection = TrackCircuitList[thisIndex];

            newIndex = -1;
            if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
            {
                addIndex.Add(thisNode);

                List<TrackCircuitElement> elements =
                         thisSection.CircuitItems.TrackCircuitElements;

                for (int idx = 0; idx < elements.Count; idx++)
                {
                    newIndex = nextNode;
                    nextNode++;

                    splitSection(thisIndex, newIndex, thisSection.Length - elements[idx].ElementLocation);
                    TrackCircuitSection newSection = TrackCircuitList[newIndex];
                    newSection.EndSignals[0] = null;
                    thisSection = TrackCircuitList[thisIndex];
                    addIndex.Add(newIndex);
                }
            }

            return nextNode;
        }
#endif

        /// <summary>
        /// Get cross-over section index
        /// </summary>
        private int getCrossOverSectionIndex(CrossOverItem CrossOver, int Index)
        {
            int sectionIndex = CrossOver.SectionIndex[Index];
            float position = CrossOver.Position[Index];
            TrackCircuitSection section = TrackCircuitList[sectionIndex];

            while (position > 0 && position > section.Length)
            {
                int prevSection = sectionIndex;
                position -= section.Length;
                CrossOver.Position[Index] = position;
                sectionIndex = section.Pins[1, 0].Link;

                if (sectionIndex > 0)
                {
                    section = TrackCircuitList[sectionIndex];
                    if (section.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
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
                Trace.TraceWarning("Cannot locate CrossOver {0} in Section {1}", CrossOver.ItemIndex[0], CrossOver.SectionIndex[0]);
                sectionIndex = -1;
            }

            return sectionIndex;
        }

        /// <summary>
        /// Split section
        /// </summary>
        private void splitSection(int orgSectionIndex, int newSectionIndex, float position)
        {
            TrackCircuitSection orgSection = TrackCircuitList[orgSectionIndex];
            TrackCircuitSection newSection = orgSection.CopyBasic(newSectionIndex);
            TrackCircuitSection replSection = orgSection.CopyBasic(orgSectionIndex);

            replSection.OriginalIndex = newSection.OriginalIndex = orgSection.OriginalIndex;
            replSection.CircuitType = newSection.CircuitType = TrackCircuitSection.TrackCircuitType.Normal;

            replSection.Length = position;
            newSection.Length = orgSection.Length - position;

#if DEBUG_REPORTS
            // check for invalid lengths - report and correct

            if (newSection.Length < 0)
            {
                Trace.TraceWarning("Invalid Length for new section {0}: length {1}, split on {2}",
                        newSection.Index, orgSection.Length, position);
                newSection.Length = 0.1f;
                replSection.Length -= 0.01f;  // take length off other part
            }
            if (replSection.Length < 0)
            {
                Trace.TraceWarning("Invalid Length for replacement section {0}: length {1}, split on {2}",
                        newSection.Index, orgSection.Length, position);
                replSection.Length = 0.1f;
                newSection.Length -= 0.01f;  // take length off other part
            }
#endif

            // take care of rounding errors

            if (newSection.Length < 0 || Math.Abs(newSection.Length) < 0.01f)
            {
                newSection.Length = 0.01f;
                replSection.Length -= 0.01f;  // take length off other part
            }
            if (replSection.Length < 0 || Math.Abs(replSection.Length) < 0.01f)
            {
                replSection.Length = 0.01f;
                newSection.Length -= 0.01f;  // take length off other part
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

            foreach (SignalFunction function in SignalFunctions.Values)
            {
                TrackCircuitSignalList orgSigList = orgSection.CircuitItems.TrackCircuitSignals[0][function];
                TrackCircuitSignalList replSigList = replSection.CircuitItems.TrackCircuitSignals[0][function];
                TrackCircuitSignalList newSigList = newSection.CircuitItems.TrackCircuitSignals[0][function];

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

            foreach (SignalFunction function in SignalFunctions.Values)
            {
                TrackCircuitSignalList orgSigList = orgSection.CircuitItems.TrackCircuitSignals[1][function];
                TrackCircuitSignalList replSigList = replSection.CircuitItems.TrackCircuitSignals[1][function];
                TrackCircuitSignalList newSigList = newSection.CircuitItems.TrackCircuitSignals[1][function];

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

            foreach (TrackCircuitMilepost thisMilepost in orgSection.CircuitItems.TrackCircuitMileposts)
            {
                if (thisMilepost.MilepostLocation[0] > replSection.Length)
                {
                    thisMilepost.MilepostLocation[0] -= replSection.Length;
                    newSection.CircuitItems.TrackCircuitMileposts.Add(thisMilepost);
                }
                else
                {
                    thisMilepost.MilepostLocation[1] -= newSection.Length;
                    replSection.CircuitItems.TrackCircuitMileposts.Add(thisMilepost);
                }
            }

#if ACTIVITY_EDITOR
            //  copy TrackCircuitElements 

            foreach (TrackCircuitElement element in orgSection.CircuitItems.TrackCircuitElements)
            {
                if (element.ElementLocation > replSection.Length)
                {
                    element.ElementLocation -= replSection.Length;
                    newSection.CircuitItems.TrackCircuitElements.Add(element);
                }
                else
                {
                    element.ElementLocation -= newSection.Length;
                    replSection.CircuitItems.TrackCircuitElements.Add(element);
                }
            }
#endif

            // update list
            TrackCircuitList.RemoveAt(orgSectionIndex);
            TrackCircuitList.Insert(orgSectionIndex, replSection);
            TrackCircuitList.Add(newSection);
        }

        /// <summary>
        /// Add junction sections for Crossover
        /// </summary>
        private void addCrossoverJunction(int leadSectionIndex0, int trailSectionIndex0,
                        int leadSectionIndex1, int trailSectionIndex1, int JnIndex,
                        CrossOverItem CrossOver, TrackSectionsFile tsectiondat)
        {
            TrackCircuitSection leadSection0 = TrackCircuitList[leadSectionIndex0];
            TrackCircuitSection leadSection1 = TrackCircuitList[leadSectionIndex1];
            TrackCircuitSection trailSection0 = TrackCircuitList[trailSectionIndex0];
            TrackCircuitSection trailSection1 = TrackCircuitList[trailSectionIndex1];
            TrackCircuitSection JnSection = new TrackCircuitSection(JnIndex, this);

            JnSection.OriginalIndex = leadSection0.OriginalIndex;
            JnSection.CircuitType = TrackCircuitSection.TrackCircuitType.Crossover;
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

        /// <summary>
        /// Check pin links
        /// </summary>
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
                            Trace.TraceWarning("Ignored invalid track node {0} pin [{1},{2}] link to track node {3}", thisNode, iDirection, iPin, linkedNode);
                            int endNode = nextNode;
                            nextNode++;
                            insertEndNode(thisNode, iDirection, iPin, endNode);
                        }

                        if (doublelink)
                        {
                            Trace.TraceWarning("Ignored invalid track node {0} pin [{1},{2}] link to track node {3}; already linked to track node {4}", thisNode, iDirection, iPin, linkedNode, doublenode);
                            int endNode = nextNode;
                            nextNode++;
                            insertEndNode(thisNode, iDirection, iPin, endNode);
                        }
                    }
                    else if (linkedNode == 0)
                    {
                        Trace.TraceWarning("Ignored invalid track node {0} pin [{1},{2}] link to track node {3}", thisNode, iDirection, iPin, linkedNode);
                        int endNode = nextNode;
                        nextNode++;
                        insertEndNode(thisNode, iDirection, iPin, endNode);
                    }
                }
            }

            return nextNode;
        }

        /// <summary>
        /// insert end node to capture database break
        /// </summary>
        private void insertEndNode(int thisNode, int direction, int pin, int endNode)
        {

            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            TrackCircuitSection endSection = new TrackCircuitSection(endNode, this);

            endSection.CircuitType = TrackCircuitSection.TrackCircuitType.EndOfTrack;
            int endDirection = direction == 0 ? 1 : 0;
            int iDirection = thisSection.Pins[direction, pin].Direction == 0 ? 1 : 0;
            endSection.Pins[iDirection, 0].Direction = endDirection;
            endSection.Pins[iDirection, 0].Link = thisNode;

            thisSection.Pins[direction, pin].Link = endNode;

            TrackCircuitList.Add(endSection);
        }

        /// <summary>
        /// set active pins for non-junction links
        /// </summary>
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

                        if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
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
                        else if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
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


                        if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                        {
                            thisSection.ActivePins[iDirection, iPin].Link = -1;
                            if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
                            {
                                thisSection.EndIsTrailingJunction[iDirection] = true;
                            }
                        }
                        else if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                        {
                            int nextDirection = thisSection.Pins[iDirection, iPin].Direction == 0 ? 1 : 0;
                            if (nextSection.Pins[nextDirection, 1].Link > 0)
                            {
                                thisSection.ActivePins[iDirection, iPin].Link = -1;
                                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
                                {
                                    thisSection.EndIsTrailingJunction[iDirection] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// set cross-reference to tracknodes
        /// </summary>
        private void setCrossReference(int thisNode, TrackNode[] trackNodes)
        {
            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            if (thisSection.OriginalIndex > 0 && thisSection.CircuitType != TrackCircuitSection.TrackCircuitType.Crossover)
            {
                TrackNode thisTrack = trackNodes[thisSection.OriginalIndex];
                float offset0 = thisSection.OffsetLength[0];
                float offset1 = thisSection.OffsetLength[1];

                TrackCircuitSectionXref newReference = new TrackCircuitSectionXref(thisSection.Index, thisSection.Length, thisSection.OffsetLength);

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
                        TrackCircuitSectionXref thisReference = thisXRef[iPart];
                        if (offset0 < thisReference.OffsetLength[0])
                        {
                            thisXRef.Insert(iPart, newReference);
                            inserted = true;
                        }
                        else if (offset1 > thisReference.OffsetLength[1])
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

        /// <summary>
        /// set cross-reference to tracknodes for CrossOver items
        /// </summary>
        private void setCrossReferenceCrossOver(int thisNode, TrackNode[] trackNodes)
        {
            TrackCircuitSection thisSection = TrackCircuitList[thisNode];
            if (thisSection.OriginalIndex > 0 && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
            {
                for (int iPin = 0; iPin <= 1; iPin++)
                {
                    int prevIndex = thisSection.Pins[0, iPin].Link;
                    TrackCircuitSection prevSection = TrackCircuitList[prevIndex];

                    TrackCircuitSectionXref newReference = new TrackCircuitSectionXref(thisSection.Index, thisSection.Length, thisSection.OffsetLength);
                    TrackNode thisTrack = trackNodes[prevSection.OriginalIndex];
                    TrackCircuitXRefList thisXRef = thisTrack.TCCrossReference;

                    bool inserted = false;
                    for (int iPart = 0; iPart < thisXRef.Count && !inserted; iPart++)
                    {
                        TrackCircuitSectionXref thisReference = thisXRef[iPart];
                        if (thisReference.Index == prevIndex)
                        {
                            newReference.OffsetLength[0] = thisReference.OffsetLength[0];
                            newReference.OffsetLength[1] = thisReference.OffsetLength[1] + thisReference.Length;
                            thisXRef.Insert(iPart, newReference);
                            inserted = true;
                        }
                    }

                    if (!inserted)
                    {
                        Trace.TraceWarning("ERROR : cannot find XRef for leading track to crossover {0}", thisNode);
                    }
                }
            }
        }

        /// <summary>
        /// Set trackcircuit cross reference for signal items and speedposts
        /// </summary>
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

                    int pinIndex = iDirection;
                    thisSignal.TCNextTC = thisSection.Pins[pinIndex, 0].Link;
                    thisSignal.TCNextDirection = thisSection.Pins[pinIndex, 0].Direction;
                }
            }

            // process other signals - only set info if not already set
            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                foreach (SignalFunction function in SignalFunctions.Values)
                {
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[iDirection][function];
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

            // process mileposts
            foreach (TrackCircuitMilepost thisItem in thisSection.CircuitItems.TrackCircuitMileposts)
            {
                Milepost thisMilepost = thisItem.MilepostRef;

                if (thisMilepost.TCReference <= 0)
                {
                    thisMilepost.TCReference = thisNode;
                    thisMilepost.TCOffset = thisItem.MilepostLocation[0];
                }
            }
        }

        /// <summary>
        /// Set physical switch
        /// </summary>
        public void setSwitch(int nodeIndex, int switchPos, TrackCircuitSection thisSection)
        {
            if (MPManager.NoAutoSwitch()) return;
            TrackNode thisNode = trackDB.TrackNodes[nodeIndex];
            thisNode.TrJunctionNode.SelectedRoute = switchPos;
            thisSection.JunctionLastRoute = switchPos;

            // update any linked signals - perform state update only (to avoid problems with route setting)
            if (thisSection.LinkedSignals != null)
            {
                foreach (int thisSignalIndex in thisSection.LinkedSignals)
                {
                    SignalObject thisSignal = SignalObjects[thisSignalIndex];
                    thisSignal.StateUpdate();
                }
            }
        }

        /// <summary>
        /// Node control track clearance update request
        /// </summary>
        public void requestClearNode(Train.TrainRouted thisTrain, Train.TCSubpathRoute routePart)
        {
#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                String.Format("Request for clear node from train {0} at section {1} starting from {2}\n",
                thisTrain.Train.Number,
                thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex,
                thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex]));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    string.Format("Request for clear node from train {0} at section {1} starting from {2}\n",
                    thisTrain.Train.Number,
                    thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex,
                    thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex]));
            }

            // check if present clearance is beyond required maximum distance

            int sectionIndex = -1;
            Train.TCRouteElement thisElement = null;
            TrackCircuitSection thisSection = null;

            List<int> sectionsInRoute = new List<int>();

            float clearedDistanceM = 0.0f;
            Train.END_AUTHORITY endAuthority = Train.END_AUTHORITY.NO_PATH_RESERVED;
            int routeIndex = -1;
            float maxDistance = Math.Max((thisTrain.Train.IsActualPlayerTrain ? (float)Simulator.TRK.Tr_RouteFile.SpeedLimit : thisTrain.Train.AllowedMaxSpeedMpS)
                * thisTrain.Train.maxTimeS, thisTrain.Train.minCheckDistanceM);

            int lastReserved = thisTrain.Train.LastReservedSection[thisTrain.TrainRouteDirectionIndex];
            int endListIndex = -1;

            bool furthestRouteCleared = false;

            Train.TCSubpathRoute thisRoute = new Train.TCSubpathRoute(thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex]);
            Train.TCPosition thisPosition = new Train.TCPosition();
            thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].CopyTo(ref thisPosition);

            // for loop detection, set occupied sections in sectionsInRoute list - but remove present position
            foreach (TrackCircuitSection occSection in thisTrain.Train.OccupiedTrack)
            {
                sectionsInRoute.Add(occSection.Index);
            }

            // correct for invalid combination of present position and occupied sections
            if (sectionsInRoute.Count > 0 && thisPosition.TCSectionIndex != sectionsInRoute.First() && thisPosition.TCSectionIndex != sectionsInRoute.Last())
            {
                if (thisTrain.Train.PresentPosition[1].TCSectionIndex == sectionsInRoute.First())
                {
                    bool remove = true;
                    for (int iindex = sectionsInRoute.Count - 1; iindex >= 0 && remove; iindex--)
                    {
                        if (sectionsInRoute[iindex] == thisPosition.TCSectionIndex)
                        {
                            remove = false;
                        }
                        else
                        {
                            sectionsInRoute.RemoveAt(iindex);
                        }
                    }
                }
                else if (thisTrain.Train.PresentPosition[1].TCSectionIndex == sectionsInRoute.Last())
                {
                    bool remove = true;
                    for (int iindex = 0; iindex < sectionsInRoute.Count && remove; iindex++)
                    {
                        if (sectionsInRoute[iindex] == thisPosition.TCSectionIndex)
                        {
                            remove = false;
                        }
                        else
                        {
                            sectionsInRoute.RemoveAt(iindex);
                        }
                    }
                }
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
                        string.Format("Index in route list : {0} = {1}\n",
                        endListIndex, thisRoute[endListIndex].TCSectionIndex));
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        string.Format("Index in route list : {0}\n",
                        endListIndex));
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
                            string.Format("Cleared Distance : {0} > Max Distance \n",
                            FormatStrings.FormatDistance(clearedDistanceM, true)));
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

            if (routeIndex < 0) return;

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

                // test if train is really occupying this section
                Train.TCSubpathRoute tempRoute = BuildTempRoute(thisTrain.Train, thisTrain.Train.PresentPosition[1].TCSectionIndex, thisTrain.Train.PresentPosition[1].TCOffset,
                    thisTrain.Train.PresentPosition[1].TCDirection, thisTrain.Train.Length, true, true, false);

                if (tempRoute.GetRouteIndex(thisSection.Index, 0) < 0)
                {
                    thisTrain.Train.OccupiedTrack.Clear();
                    foreach (Train.TCRouteElement thisOccupyElement in tempRoute)
                    {
                        thisTrain.Train.OccupiedTrack.Add(TrackCircuitList[thisOccupyElement.TCSectionIndex]);
                    }
                }

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
                        for (int iIndex = 0; iIndex < rearIndex; iIndex++)
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
                        string.Format("Starting check from : Index in route list : {0} = {1}\n",
                        routeIndex, thisRoute[routeIndex].TCSectionIndex));
                }

                // check if train ahead still in last available section

                bool routeAvailable = true;
                thisSection = TrackCircuitList[routePart[routeIndex].TCSectionIndex];

                float posOffset = thisPosition.TCOffset;
                int posDirection = thisPosition.TCDirection;

                if (routeIndex > thisPosition.RouteListIndex)
                {
                    posOffset = 0;
                    posDirection = routePart[routeIndex].Direction;
                }

                Dictionary<Train, float> trainAhead =
                        thisSection.TestTrainAhead(thisTrain.Train, posOffset, posDirection);

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        string.Format("Train ahead in section {0} : {1}\n",
                        thisSection.Index, trainAhead.Count));
                }

                if (trainAhead.Count > 0)
                {
                    routeAvailable = false;

                    // if section is junction or crossover, use next section as last, otherwise use this section as last
                    if (thisSection.CircuitType != TrackCircuitSection.TrackCircuitType.Junction && thisSection.CircuitType != TrackCircuitSection.TrackCircuitType.Crossover)
                    {
                        lastRouteIndex = routeIndex - 1;
                    }

                    if (thisTrain.Train.CheckTrain)
                    {
                        if (lastRouteIndex >= 0)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                            string.Format("Set last valid section : Index in route list : {0} = {1}\n",
                            lastRouteIndex, thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][lastRouteIndex].TCSectionIndex));
                        }
                        else
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "First Section in Route\n");
                        }
                    }
                }

                // train ahead has moved on, check next sections

                int startRouteIndex = routeIndex;

                while (routeIndex < routePart.Count && routeAvailable && !furthestRouteCleared)
                {
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                            string.Format("Checking : Index in route list : {0} = {1}\n",
                            routeIndex, thisRoute[routeIndex].TCSectionIndex));
                    }

                    thisElement = routePart[routeIndex];
                    sectionIndex = thisElement.TCSectionIndex;
                    thisSection = TrackCircuitList[sectionIndex];

                    // check if section is in loop
                    if (sectionsInRoute.Contains(thisSection.Index) ||
                        (routeIndex > startRouteIndex && sectionIndex == thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex))
                    {
                        endAuthority = Train.END_AUTHORITY.LOOP;
                        thisTrain.Train.LoopSection = thisSection.Index;
                        routeAvailable = false;

                        Trace.TraceInformation("Train {0} ({1}) : Looped at {2}", thisTrain.Train.Name, thisTrain.Train.Number, thisSection.Index);

                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Section looped \n");
                        }
                    }
                    // check if section is access to pool
                    else if (thisTrain.Train.CheckPoolAccess(thisSection.Index))
                    {
                        routeAvailable = false;
                        furthestRouteCleared = true;
                    }
                    // check if section is available
                    else if (thisSection.GetSectionStateClearNode(thisTrain, thisElement.Direction, routePart))
                    {
                        lastReserved = thisSection.Index;
                        lastRouteIndex = routeIndex;
                        sectionsInRoute.Add(thisSection.Index);
                        clearedDistanceM += thisSection.Length - offset;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Section clear \n");
                        }

                        if (thisSection.CircuitState.HasOtherTrainsOccupying(thisTrain))
                        {
                            bool trainIsAhead = false;

                            // section is still ahead
                            if (thisSection.Index != thisPosition.TCSectionIndex)
                            {
                                trainIsAhead = true;
                            }
                            // same section
                            else
                            {
                                trainAhead = thisSection.TestTrainAhead(thisTrain.Train, thisPosition.TCOffset, thisPosition.TCDirection);
                                if (trainAhead.Count > 0 && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal) // do not end path on junction
                                {
                                    trainIsAhead = true;
                                }
                            }

                            if (trainIsAhead)
                            {
                                if (thisTrain.Train.CheckTrain)
                                {
                                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                        string.Format("Train ahead in section {0} : {1}\n",
                                        thisSection.Index, trainAhead.Count));
                                }
                                lastRouteIndex = routeIndex - 1;
                                lastReserved = lastRouteIndex >= 0 ? routePart[lastRouteIndex].TCSectionIndex : -1;
                                routeAvailable = false;
                                clearedDistanceM -= thisSection.Length + offset; // correct length as this section was already added to total length
                            }
                        }

                        if (routeAvailable)
                        {
                            routeIndex++;
                            offset = 0.0f;

                            if (!thisSection.CircuitState.ThisTrainOccupying(thisTrain) &&
                                thisSection.CircuitState.TrainReserved == null)
                            {
                                thisSection.Reserve(thisTrain, routePart);
                            }

                            if (!furthestRouteCleared && thisSection.EndSignals[thisElement.Direction] != null)
                            {
                                SignalObject endSignal = thisSection.EndSignals[thisElement.Direction];
                                // check if signal enabled for other train - if so, keep in node control
                                if (endSignal.enabledTrain == null || endSignal.enabledTrain == thisTrain)
                                {
                                    if (routeIndex < routePart.Count)
                                    {
                                        thisTrain.Train.SwitchToSignalControl(thisSection.EndSignals[thisElement.Direction]);
                                    }
                                }
                                furthestRouteCleared = true;
                                if (thisTrain.Train.CheckTrain)
                                {
                                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                        string.Format("Has end signal : {0}\n",
                                        thisSection.EndSignals[thisElement.Direction].thisRef));
                                }
                            }

                            if (clearedDistanceM > thisTrain.Train.minCheckDistanceM && clearedDistanceM > (
                                (thisTrain.Train.IsActualPlayerTrain ? (float)Simulator.TRK.Tr_RouteFile.SpeedLimit : thisTrain.Train.AllowedMaxSpeedMpS) * thisTrain.Train.maxTimeS))
                            {
                                endAuthority = Train.END_AUTHORITY.MAX_DISTANCE;
                                furthestRouteCleared = true;
                            }
                        }
                    }
                    // section is not available
                    else
                    {
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Section blocked \n");
                        }
                        lastRouteIndex = routeIndex - 1;
                        lastReserved = lastRouteIndex >= 0 ? routePart[lastRouteIndex].TCSectionIndex : -1;
                        routeAvailable = false;
                    }
                }
            }

            // if not cleared to max distance or looped, determine reason
            if (!furthestRouteCleared && lastRouteIndex > 0 && routePart[lastRouteIndex].TCSectionIndex >= 0  && endAuthority != Train.END_AUTHORITY.LOOP)
            {

                thisElement = routePart[lastRouteIndex];
                sectionIndex = thisElement.TCSectionIndex;
                thisSection = TrackCircuitList[sectionIndex];

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        string.Format("Last section cleared in route list : {0} = {1}\n",
                        lastRouteIndex, thisRoute[lastRouteIndex].TCSectionIndex));
                }

                // end of track reached
                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                {
                    endAuthority = Train.END_AUTHORITY.END_OF_TRACK;
                    furthestRouteCleared = true;
                    if (thisTrain.Train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "End of track \n");
                    }
                }

                // end of path reached
                if (!furthestRouteCleared)
                {
                    if (lastRouteIndex > (routePart.Count - 1))
                    {
                        endAuthority = Train.END_AUTHORITY.END_OF_PATH;
                        furthestRouteCleared = true;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "End of path \n");
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
                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                    thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                {
                    if (!thisSection.IsAvailable(thisTrain))
                    {
                        // check if switch is set to required path - if so, do not classify as reserved switch even if it is reserved by another train

                        int jnIndex = routePart.GetRouteIndex(sectionIndex, 0);
                        bool jnAligned = false;
                        if (jnIndex < routePart.Count - 1)
                        {
                            if (routePart[jnIndex + 1].TCSectionIndex == thisSection.ActivePins[0, 0].Link || routePart[jnIndex + 1].TCSectionIndex == thisSection.ActivePins[0, 1].Link)
                            {
                                if (routePart[jnIndex - 1].TCSectionIndex == thisSection.ActivePins[1, 0].Link || routePart[jnIndex - 1].TCSectionIndex == thisSection.ActivePins[1, 1].Link)
                                {
                                    jnAligned = true;
                                }
                            }
                            else if (routePart[jnIndex + 1].TCSectionIndex == thisSection.ActivePins[1, 0].Link || routePart[jnIndex + 1].TCSectionIndex == thisSection.ActivePins[1, 1].Link)
                            {
                                if (routePart[jnIndex - 1].TCSectionIndex == thisSection.ActivePins[0, 0].Link || routePart[jnIndex - 1].TCSectionIndex == thisSection.ActivePins[0, 1].Link)
                                {
                                    jnAligned = true;
                                }
                            }
                        }

                        // switch is not properly set, so it blocks the path
                        if (!jnAligned)
                        {
                            endAuthority = Train.END_AUTHORITY.RESERVED_SWITCH;
                            furthestRouteCleared = true;
                            if (thisTrain.Train.CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Reserved Switch \n");
                            }
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

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal &&
                           thisSection.CircuitState.HasOtherTrainsOccupying(thisTrain))
                {
                    if (thisSection.CircuitState.HasOtherTrainsOccupying(revDirection, false, thisTrain))
                    {
                        endAuthority = Train.END_AUTHORITY.TRAIN_AHEAD;
                        furthestRouteCleared = true;
                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train Ahead \n");
                        }
                    }
                    // check for train further ahead and determine distance to train
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
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train Ahead \n");
                            }
                        }
                    }
                }
                else if (thisSection.GetSectionStateClearNode(thisTrain, thisElement.Direction, routePart))
                {
                    endAuthority = Train.END_AUTHORITY.END_OF_AUTHORITY;
                    furthestRouteCleared = true;
                }
                else if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover || thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                {
                    // first not-available section is crossover or junction - treat as reserved switch
                    endAuthority = Train.END_AUTHORITY.RESERVED_SWITCH;
                }
            }

            else if (routeIndex >= routePart.Count)
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
                    string.Format("Returned : \n    State : {0}\n    Dist  : {1}\n    Sect  : {2}\n",
                    endAuthority, FormatStrings.FormatDistance(clearedDistanceM, true), lastReserved));
            }
        }

        /// <summary>
        /// Break down reserved route
        /// </summary>
        public void BreakDownRoute(int firstSectionIndex, Train.TrainRouted reqTrain)
        {
            if (firstSectionIndex < 0)
                return; // no route to break down

            TrackCircuitSection firstSection = TrackCircuitList[firstSectionIndex];
            Train.TrainRouted thisTrain = firstSection.CircuitState.TrainReserved;

            // if occupied by train - skip actions and proceed to next section
            if (!firstSection.CircuitState.ThisTrainOccupying(reqTrain))
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

                int iPinLink = nextDirection;
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

        /// <summary>
        /// Break down reserved route using route list
        /// </summary>
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

        /// Build temp route for train
        /// <summary>
        /// Used for trains without path (eg stationary constists), manual operation
        /// </summary>
        public Train.TCSubpathRoute BuildTempRoute(Train thisTrain,
                int firstSectionIndex, float firstOffset, int firstDirection,
                float routeLength, bool overrideManualSwitchState, bool autoAlign, bool stopAtFacingSignal)
        {
            bool honourManualSwitchState = !overrideManualSwitchState;
            List<int> sectionList = ScanRoute(thisTrain, firstSectionIndex, firstOffset, firstDirection,
                    true, routeLength, honourManualSwitchState, autoAlign, stopAtFacingSignal, false, true, false, false, false, false, false);
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

            // set pin references for junction sections
            for (int iElement = 0; iElement < tempRoute.Count - 1; iElement++) // do not process last element as next element is required
            {
                Train.TCRouteElement thisElement = tempRoute[iElement];
                TrackCircuitSection thisSection = TrackCircuitList[thisElement.TCSectionIndex];

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                {
                    if (thisElement.OutPin[0] == 1) // facing switch
                    {
                        thisElement.OutPin[1] = thisSection.Pins[1, 0].Link == tempRoute[iElement + 1].TCSectionIndex ? 0 : 1;
                    }
                }
            }

            return tempRoute;
        }

        /// <summary>
        /// Follow default route for train
        /// Use for :
        ///   - build temp list for trains without route (eg stat objects)
        ///   - build list for train under Manual control
        ///   - build list of sections when train slip backward
        ///   - search signal or speedpost ahead or at the rear of the train (either in facing or backward direction)
        ///
        /// Search ends :
        ///   - if required object is found
        ///   - if required length is covered
        ///   - if valid path only is requested and unreserved section is found (variable thisTrain required)
        ///   - end of track
        ///   - looped track
        ///   - re-enter in original route (for manual re-routing)
        ///
        /// Returned is list of sections, with positive no. indicating direction 0 and negative no. indicating direction 1
        /// If signal or speedpost is required, list will contain index of required item (>0 facing direction, <0 backing direction)
        /// </summary>
        public List<int> ScanRoute(Train thisTrain, int firstSectionIndex, float firstOffset, int firstDirection, bool forward,
                float routeLength, bool honourManualSwitch, bool autoAlign, bool stopAtFacingSignal, bool reservedOnly, bool returnSections,
                bool searchFacingSignal, bool searchBackwardSignal, bool searchFacingSpeedpost, bool searchBackwardSpeedpost,
                bool isFreight, bool considerSpeedReset = false, bool checkReenterOriginalRoute = false)
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
                if (foundItems.Contains(thisIndex) || foundItems.Contains(-thisIndex))
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
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalFunction.SPEED);

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
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalFunction.SPEED);

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

                if (searchFacingSignal && thisSection.EndSignals[curDirection] != null)           // search facing signal
                {
                    foundObject.Add(thisSection.EndSignals[curDirection].thisRef);
                    endOfRoute = true;
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
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalFunction.SPEED);
                            if (considerSpeedReset)
                            {
                                var speed_infoR = thisSpeedpost.this_sig_speed(SignalFunction.SPEED);
                                if (speed_infoR != null) speed_info.speed_reset = speed_infoR.speed_reset;
                            }
                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0) || speed_info.speed_reset == 1)
                            {
                                if (thisItem.SignalLocation < thisSection.Length - offset)
                                {
                                    endOfRoute = true;
                                    foundObject.Add(-thisItem.SignalRef.thisRef);
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
                            ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalFunction.SPEED);

                            if ((isFreight && speed_info.speed_freight > 0) || (!isFreight && speed_info.speed_pass > 0))
                            {
                                if (offset == 0 || thisItem.SignalLocation > thisSection.Length - offset)
                                {
                                    endOfRoute = true;
                                    foundObject.Add(-thisItem.SignalRef.thisRef);
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
                    case TrackCircuitSection.TrackCircuitType.Crossover:
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

                    case TrackCircuitSection.TrackCircuitType.Junction:
                        if (checkReenterOriginalRoute)
                        {
                            Train.TCSubpathRoute originalSubpath = thisTrain.TCRoute.TCRouteSubpaths[thisTrain.TCRoute.OriginalSubpath];
                            if (outPinIndex == 0)
                            {
                                // loop on original route to check if we are re-entering it
                                for (int routeIndex = 0; routeIndex < originalSubpath.Count; routeIndex++)
                                {
                                    if (thisIndex == originalSubpath[routeIndex].TCSectionIndex)
                                    // nice, we are returning into the original route
                                    {
                                        endOfRoute = true;
                                        break;
                                    }
                                }
                            }
                        }

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

                    case TrackCircuitSection.TrackCircuitType.EndOfTrack:
                        break;

                    default:
                        nextIndex = thisSection.Pins[outPinIndex, 0].Link;
                        nextDirection = thisSection.Pins[outPinIndex, 0].Direction;

                        TrackCircuitSection nextSection = TrackCircuitList[nextIndex];

                        // if next section is junction : check if locked against AI and if auto-alignment allowed
                        // switchable end of switch is always pin direction 1
                        if (nextSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                        {
                            int nextPinDirection = nextDirection == 0 ? 1 : 0;
                            int nextPinIndex = nextSection.Pins[nextDirection == 0 ? 1 : 0, 0].Link == thisIndex ? 0 : 1;
                            if (nextPinDirection == 1 && nextSection.JunctionLastRoute != nextPinIndex)
                            {
                                if (nextSection.AILock && thisTrain != null && (thisTrain.TrainType == Train.TRAINTYPE.AI
                                    || thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING))
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
                        foundObject.Add(-thisSection.EndSignals[oppDirection].thisRef);
                    }
                }

                if (!endOfRoute)
                {
                    offset = 0.0f;

                    if (thisTrain != null && reservedOnly)
                    {
                        TrackCircuitState thisState = thisSection.CircuitState;

                        if (!thisState.TrainOccupy.ContainsTrain(thisTrain) &&
                            thisState.TrainReserved != null && thisState.TrainReserved.Train != thisTrain)
                        {
                            endOfRoute = true;
                        }
                    }
                }

                if (!endOfRoute && routeLength > 0)
                {
                    endOfRoute = coveredLength > routeLength;
                    coveredLength += thisSection.Length;
                }
            }

            if (returnSections)
            {
                return foundItems;
            }
            else
            {
                return foundObject;
            }
        }

        /// <summary>
        /// Process Platforms
        /// </summary>
        private void ProcessPlatforms(Dictionary<int, int> platformList, TrItem[] TrItems,
                TrackNode[] trackNodes, Dictionary<int, uint> platformSidesList)
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

                // set station reference
                if (StationXRefList.ContainsKey(thisPlatform.Station))
                {
                    List<int> XRefList = StationXRefList[thisPlatform.Station];
                    XRefList.Add(thisPlatformDetailsIndex);
                }
                else
                {
                    List<int> XRefList = new List<int>();
                    XRefList.Add(thisPlatformDetailsIndex);
                    StationXRefList.Add(thisPlatform.Station, XRefList);
                }

                // get tracksection

                int TCSectionIndex = -1;
                int TCXRefIndex = -1;

                for (int iXRef = thisNode.TCCrossReference.Count - 1; iXRef >= 0 && TCSectionIndex < 0; iXRef--)
                {
                    if (thisPlatform.SData1 <
                        (thisNode.TCCrossReference[iXRef].OffsetLength[1] + thisNode.TCCrossReference[iXRef].Length))
                    {
                        TCSectionIndex = thisNode.TCCrossReference[iXRef].Index;
                        TCXRefIndex = iXRef;
                    }
                }

                if (TCSectionIndex < 0)
                {
                    Trace.TraceInformation("Cannot locate TCSection for platform {0}", thisIndex);
                    TCSectionIndex = thisNode.TCCrossReference[0].Index;
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
                            if (thisNode.TCCrossReference[iXRef].Index == thisDetails.TCSectionIndex[0])
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
                                thisDetails.TCSectionIndex.Add(thisNode.TCCrossReference[iXRef].Index);
                            }
                        }
                        else
                        {
                            thisDetails.TCSectionIndex.Clear();
                            for (int iXRef = firstXRef; iXRef <= TCXRefIndex; iXRef++)
                            {
                                thisDetails.TCSectionIndex.Add(thisNode.TCCrossReference[iXRef].Index);
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
                    if (thisPlatform.Flags1 == "ffff0000" || thisPlatform.Flags1 == "FFFF0000") thisDetails.PlatformFrontUiD = thisIndex;        // used to define 
                }

                if (refIndex == 0)
                {
                    thisDetails.Name = string.Copy(thisPlatform.Station);
                    thisDetails.MinWaitingTime = thisPlatform.PlatformMinWaitingTime;
                    thisDetails.NumPassengersWaiting = (int)thisPlatform.PlatformNumPassengersWaiting;
                }
                else if (!splitPlatform)
                {
                    thisDetails.Length = Math.Abs(thisDetails.nodeOffset[1] - thisDetails.nodeOffset[0]);
                }

                if (platformSidesList.TryGetValue(thisIndex, out uint thisPlatformData))
                {
                    if (((uint)PlatformDataFlag.PlatformLeft & thisPlatformData) != 0) thisDetails.PlatformSide[0] = true;
                    if (((uint)PlatformDataFlag.PlatformRight & thisPlatformData) != 0) thisDetails.PlatformSide[1] = true;
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
                        if (lastSection == thisNode.TCCrossReference[iXRef].Index)
                        {
                            lastSectionXRef = iXRef;
                            break;
                        }
                    }

                    for (int iXRef = lastSectionXRef; iXRef < thisNode.TCCrossReference.Count; iXRef++)
                    {
                        int sectionIndex = thisNode.TCCrossReference[iXRef].Index;
                        TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];

                        distToSignal += thisSection.Length - offset;
                        offset = 0.0f;

                        if (thisSection.EndSignals[0] != null)
                        {
                            // end signal is always valid in timetable mode
                            if (Simulator.TimetableMode || distToSignal <= 150)
                            {
                                thisDetails.EndSignals[0] = thisSection.EndSignals[0].thisRef;
                                thisDetails.DistanceToSignals[0] = distToSignal;
                            }
                            // end signal is only valid if it has no fixed route in activity mode
                            else
                            {
                                float? approachControlLimitPositionM = null;
                                if (distToSignal > 150)
                                {
                                    foreach (SignalHead signalHead in thisSection.EndSignals[0].SignalHeads)
                                    {
                                        if (signalHead.ApproachControlLimitPositionM != null) approachControlLimitPositionM = signalHead.ApproachControlLimitPositionM;
                                    }
                                }
                                if (!thisSection.EndSignals[0].hasFixedRoute && !(approachControlLimitPositionM != null && (float)approachControlLimitPositionM < distToSignal + 100))
                                {
                                    thisDetails.EndSignals[0] = thisSection.EndSignals[0].thisRef;
                                    thisDetails.DistanceToSignals[0] = distToSignal;
                                }
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
                            if (firstSection == thisNode.TCCrossReference[iXRef].Index)
                            {
                                firstSectionXRef = iXRef;
                                break;
                            }
                        }
                    }

                    for (int iXRef = firstSectionXRef; iXRef >= 0; iXRef--)
                    {
                        int sectionIndex = thisNode.TCCrossReference[iXRef].Index;
                        TrackCircuitSection thisSection = TrackCircuitList[sectionIndex];

                        distToSignal += thisSection.Length - offset;
                        offset = 0.0f;

                        if (thisSection.EndSignals[1] != null)
                        {
                            if (Simulator.TimetableMode || distToSignal <= 150)
                            {
                                thisDetails.EndSignals[1] = thisSection.EndSignals[1].thisRef;
                                thisDetails.DistanceToSignals[1] = distToSignal;
                            }
                            else
                            {
                                float? approachControlLimitPositionM = null;
                                if (distToSignal > 150)
                                {
                                    foreach (SignalHead signalHead in thisSection.EndSignals[1].SignalHeads)
                                    {
                                        if (signalHead.ApproachControlLimitPositionM != null) approachControlLimitPositionM = signalHead.ApproachControlLimitPositionM;
                                    }
                                }
                                if (!thisSection.EndSignals[1].hasFixedRoute && !(approachControlLimitPositionM != null && (float)approachControlLimitPositionM < distToSignal + 100))
                                {
                                    thisDetails.EndSignals[1] = thisSection.EndSignals[1].thisRef;
                                    thisDetails.DistanceToSignals[1] = distToSignal;
                                }
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

            if (Simulator.Activity != null &&
                Simulator.Activity.Tr_Activity.Tr_Activity_File.PlatformNumPassengersWaiting != null)

            // Override .tdb NumPassengersWaiting info with .act NumPassengersWaiting info if any available
            {
                int overriddenPlatformDetailsIndex;
                foreach (PlatformData platformData in Simulator.Activity.Tr_Activity.Tr_Activity_File.PlatformNumPassengersWaiting.PlatformDataList)
                {
                    overriddenPlatformDetailsIndex = PlatformDetailsList.FindIndex(platformDetails => (platformDetails.PlatformReference[0] == platformData.Id) || (platformDetails.PlatformReference[1] == platformData.Id));
                    if (overriddenPlatformDetailsIndex >= 0) PlatformDetailsList[overriddenPlatformDetailsIndex].NumPassengersWaiting = platformData.PassengerCount;
                    else Trace.TraceWarning("Platform referenced in .act file with TrItemId {0} not present in .tdb file ", platformData.Id);
                }
            }
        }

        /// <summary>
        /// Resolve split platforms
        /// </summary>
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
                    int thisIndex = firstNode.TCCrossReference[iXRef].Index;
                    PlSections1.Add(thisIndex);
                    totalLength1 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = thisIndex == firstSectionIndex;
                }
                totalLength1 -= thisDetails.TCOffset[1, 0];  // correct for offset
            }
            else
            {
                for (int iXRef = 0; iXRef < firstNode.TCCrossReference.Count && !reqSectionFound; iXRef++)
                {
                    int thisIndex = firstNode.TCCrossReference[iXRef].Index;
                    PlSections1.Add(thisIndex);
                    totalLength1 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = thisIndex == firstSectionIndex;
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
                    int thisIndex = secondNode.TCCrossReference[iXRef].Index;
                    PlSections2.Add(thisIndex);
                    totalLength2 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = thisIndex == secondSectionIndex;
                }
                totalLength2 -= TrackCircuitList[secondSectionIndex].Length - secondPlatform.SData1;
            }
            else
            {
                for (int iXRef = 0; iXRef < secondNode.TCCrossReference.Count && !reqSectionFound; iXRef++)
                {
                    int thisIndex = secondNode.TCCrossReference[iXRef].Index;
                    PlSections2.Add(thisIndex);
                    totalLength2 += TrackCircuitList[thisIndex].Length;
                    reqSectionFound = thisIndex == secondSectionIndex;
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

        /// <summary>
        /// Remove all deadlock path references for specified train
        /// </summary>
        public void RemoveDeadlockPathReferences(int trainnumber)
        {
            foreach (KeyValuePair<int, DeadlockInfo> deadlockElement in DeadlockInfoList)
            {
                DeadlockInfo deadlockInfo = deadlockElement.Value;
                if (deadlockInfo.TrainSubpathIndex.ContainsKey(trainnumber))
                {
                    Dictionary<int, int> subpathRef = deadlockInfo.TrainSubpathIndex[trainnumber];
                    foreach (KeyValuePair<int, int> pathRef in subpathRef)
                    {
                        int routeIndex = pathRef.Value;
                        List<int> pathReferences = deadlockInfo.TrainReferences[routeIndex];
                        foreach (int pathReference in pathReferences)
                        {
                            deadlockInfo.AvailablePathList[pathReference].AllowedTrains.Remove(trainnumber);
                        }
                        deadlockInfo.TrainReferences.Remove(routeIndex);
                        deadlockInfo.TrainOwnPath.Remove(routeIndex);
                        deadlockInfo.TrainLengthFit.Remove(routeIndex);
                    }
                    deadlockInfo.TrainSubpathIndex.Remove(trainnumber);
                }
            }
        }

        /// <summary>
        /// Reallocate all deadlock path references for specified train when train forms new train
        /// </summary>
        public void ReallocateDeadlockPathReferences(int oldnumber, int newnumber)
        {
            foreach (KeyValuePair<int, DeadlockInfo> deadlockElement in DeadlockInfoList)
            {
                DeadlockInfo deadlockInfo = deadlockElement.Value;
                if (deadlockInfo.TrainSubpathIndex.ContainsKey(oldnumber))
                {
                    Dictionary<int, int> subpathRef = deadlockInfo.TrainSubpathIndex[oldnumber];
                    foreach (KeyValuePair<int, int> pathRef in subpathRef)
                    {
                        int routeIndex = pathRef.Value;
                        List<int> pathReferences = deadlockInfo.TrainReferences[routeIndex];
                        foreach (int pathReference in pathReferences)
                        {
                            deadlockInfo.AvailablePathList[pathReference].AllowedTrains.Remove(oldnumber);
                            deadlockInfo.AvailablePathList[pathReference].AllowedTrains.Add(newnumber);
                        }
                    }
                    deadlockInfo.TrainSubpathIndex.Add(newnumber, subpathRef);
                    deadlockInfo.TrainSubpathIndex.Remove(oldnumber);
                }
            }
        }

        /// <summary>
        /// ProcessTunnels
        /// Process tunnel sections and add info to TrackCircuitSections
        /// </summary>
        public void ProcessTunnels()
        {
            // loop through tracknodes
            foreach (TrackNode thisNode in trackDB.TrackNodes)
            {
                if (thisNode != null && thisNode.TrVectorNode != null)
                {
                    bool inTunnel = false;
                    List<float[]> tunnelInfo = new List<float[]>();
                    List<int> tunnelPaths = new List<int>();
                    float[] lastTunnel = null;
                    float totalLength = 0f;
                    int numPaths = -1;

                    // loop through all sections in node
                    TrVectorNode thisVNode = thisNode.TrVectorNode;
                    foreach (TrVectorSection thisSection in thisVNode.TrVectorSections)
                    {
                        if (!tsectiondat.TrackSections.ContainsKey(thisSection.SectionIndex))
                        {
                            continue;  // missing track section
                        }

                        float thisLength = 0f;
                        Orts.Formats.Msts.TrackSection TS = tsectiondat.TrackSections[thisSection.SectionIndex];

                        // determine length
                        if (TS.SectionCurve != null)
                        {
                            thisLength =
                                    MathHelper.ToRadians(Math.Abs(TS.SectionCurve.Angle)) * TS.SectionCurve.Radius;
                        }
                        else
                        {
                            thisLength = TS.SectionSize.Length;

                        }

                        // check tunnel shape

                        bool tunnelShape = false;
                        int shapePaths = 0;

                        if (tsectiondat.TrackShapes.ContainsKey(thisSection.ShapeIndex))
                        {
                            TrackShape thisShape = tsectiondat.TrackShapes[thisSection.ShapeIndex];
                            tunnelShape = thisShape.TunnelShape;
                            shapePaths = Convert.ToInt32(thisShape.NumPaths);
                        }

                        if (tunnelShape)
                        {
                            numPaths = numPaths < 0 ? shapePaths : Math.Min(numPaths, shapePaths);
                            if (inTunnel)
                            {
                                lastTunnel[1] += thisLength;
                            }
                            else
                            {
                                lastTunnel = new float[2];
                                lastTunnel[0] = totalLength;
                                lastTunnel[1] = thisLength;
                                inTunnel = true;
                            }
                        }
                        else if (inTunnel)
                        {
                            tunnelInfo.Add(lastTunnel);
                            tunnelPaths.Add(numPaths);
                            inTunnel = false;
                            numPaths = -1;
                        }
                        totalLength += thisLength;
                    }

                    // add last tunnel item
                    if (inTunnel)
                    {
                        tunnelInfo.Add(lastTunnel);
                        tunnelPaths.Add(numPaths);
                    }

                    // add tunnel info to TrackCircuitSections
                    if (tunnelInfo.Count > 0)
                    {
                        bool TCSInTunnel = false;
                        float[] tunnelData = tunnelInfo[0];
                        float processedLength = 0;

                        for (int iXRef = thisNode.TCCrossReference.Count - 1; iXRef >= 0; iXRef--)
                        {
                            TrackCircuitSectionXref TCSXRef = thisNode.TCCrossReference[iXRef];
                            // forward direction
                            float TCSStartOffset = TCSXRef.OffsetLength[1];
                            float TCSLength = TCSXRef.Length;
                            TrackCircuitSection thisTCS = TrackCircuitList[TCSXRef.Index];

                            // if tunnel starts in TCS
                            while (tunnelData != null && tunnelData[0] <= (TCSStartOffset + TCSLength))
                            {
                                TrackCircuitSection.tunnelInfoData[] TCSTunnelData = new TrackCircuitSection.tunnelInfoData[2];
                                float tunnelStart = 0;
                                TCSTunnelData[0].numTunnelPaths = tunnelPaths[0];
                                TCSTunnelData[1].numTunnelPaths = tunnelPaths[0];

                                // if in tunnel, set start in tunnel and check end
                                if (TCSInTunnel)
                                {
                                    TCSTunnelData[1].TunnelStart = -1;
                                    TCSTunnelData[1].TCSStartOffset = processedLength;
                                }
                                else
                                // else start new tunnel
                                {
                                    TCSTunnelData[1].TunnelStart = tunnelData[0] - TCSStartOffset;
                                    tunnelStart = TCSTunnelData[1].TunnelStart;
                                    TCSTunnelData[1].TCSStartOffset = -1;
                                }

                                if ((TCSStartOffset + TCSLength) >= (tunnelData[0] + tunnelData[1]))  // tunnel end is in this section
                                {
                                    TCSInTunnel = false;
                                    TCSTunnelData[1].TunnelEnd = tunnelStart + tunnelData[1] - processedLength;

                                    TCSTunnelData[1].LengthInTCS = TCSTunnelData[1].TunnelEnd - tunnelStart;
                                    TCSTunnelData[1].TotalLength = tunnelData[1];

                                    processedLength = 0;

                                    if (thisTCS.TunnelInfo == null) thisTCS.TunnelInfo = new List<TrackCircuitSection.tunnelInfoData[]>();
                                    thisTCS.TunnelInfo.Add(TCSTunnelData);

                                    if (tunnelInfo.Count >= 2)
                                    {
                                        tunnelInfo.RemoveAt(0);
                                        tunnelData = tunnelInfo[0];
                                        tunnelPaths.RemoveAt(0);
                                    }
                                    else
                                    {
                                        tunnelData = null;
                                        break;  // no more tunnels to process
                                    }
                                }
                                else
                                {
                                    TCSInTunnel = true;

                                    TCSTunnelData[1].TunnelEnd = -1;
                                    TCSTunnelData[1].LengthInTCS = TCSLength - tunnelStart;
                                    TCSTunnelData[1].TotalLength = tunnelData[1];

                                    processedLength += TCSLength - tunnelStart;

                                    if (thisTCS.TunnelInfo == null) thisTCS.TunnelInfo = new List<TrackCircuitSection.tunnelInfoData[]>();
                                    thisTCS.TunnelInfo.Add(TCSTunnelData);
                                    break;  // cannot add more tunnels to section
                                }
                            }
                            // derive tunnel data for other direction
                            if (thisTCS.TunnelInfo != null)
                            {
                                foreach (TrackCircuitSection.tunnelInfoData[] thisTunnelInfo in thisTCS.TunnelInfo)
                                {
                                    thisTunnelInfo[0].TunnelStart = thisTunnelInfo[1].TunnelEnd < 0 ? -1 : thisTCS.Length - thisTunnelInfo[1].TunnelEnd;
                                    thisTunnelInfo[0].TunnelEnd = thisTunnelInfo[1].TunnelStart < 0 ? -1 : thisTCS.Length - thisTunnelInfo[1].TunnelStart;
                                    thisTunnelInfo[0].LengthInTCS = thisTunnelInfo[1].LengthInTCS;
                                    thisTunnelInfo[0].TotalLength = thisTunnelInfo[1].TotalLength;

                                    if (thisTunnelInfo[0].TunnelStart >= 0)
                                    {
                                        thisTunnelInfo[0].TCSStartOffset = -1;
                                    }
                                    else if (thisTunnelInfo[1].TCSStartOffset < 0)
                                    {
                                        thisTunnelInfo[0].TCSStartOffset = thisTunnelInfo[0].TotalLength - thisTunnelInfo[0].LengthInTCS;
                                    }
                                    else
                                    {
                                        thisTunnelInfo[0].TCSStartOffset = thisTunnelInfo[0].TotalLength - thisTunnelInfo[1].TCSStartOffset - thisTCS.Length;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ProcessTroughs
        /// Process trough sections and add info to TrackCircuitSections
        /// </summary>
        public void ProcessTroughs()
        {
            // loop through tracknodes
            foreach (TrackNode thisNode in trackDB.TrackNodes)
            {
                if (thisNode != null && thisNode.TrVectorNode != null)
                {
                    bool overTrough = false;
                    List<float[]> troughInfo = new List<float[]>();
                    List<int> troughPaths = new List<int>();
                    float[] lastTrough = null;
                    float totalLength = 0f;
                    int numPaths = -1;

                    // loop through all sections in node
                    TrVectorNode thisVNode = thisNode.TrVectorNode;
                    foreach (TrVectorSection thisSection in thisVNode.TrVectorSections)
                    {
                        if (!tsectiondat.TrackSections.ContainsKey(thisSection.SectionIndex))
                        {
                            continue;  // missing track section
                        }

                        float thisLength = 0f;
                        Orts.Formats.Msts.TrackSection TS = tsectiondat.TrackSections[thisSection.SectionIndex];

                        // determine length
                        if (TS.SectionCurve != null)
                        {
                            thisLength =
                                    MathHelper.ToRadians(Math.Abs(TS.SectionCurve.Angle)) * TS.SectionCurve.Radius;
                        }
                        else
                        {
                            thisLength = TS.SectionSize.Length;

                        }

                        // check trough shape

                        bool troughShape = false;
                        int shapePaths = 0;

                        if (tsectiondat.TrackShapes.ContainsKey(thisSection.ShapeIndex))
                        {
                            TrackShape thisShape = tsectiondat.TrackShapes[thisSection.ShapeIndex];
                            if (thisShape.FileName != null)
                            {
                                troughShape = thisShape.FileName.EndsWith("Wtr.s") || thisShape.FileName.EndsWith("wtr.s");
                                shapePaths = Convert.ToInt32(thisShape.NumPaths);
                            }
                        }

                        if (troughShape)
                        {
                            numPaths = numPaths < 0 ? shapePaths : Math.Min(numPaths, shapePaths);
                            if (overTrough)
                            {
                                lastTrough[1] += thisLength;
                            }
                            else
                            {
                                lastTrough = new float[2];
                                lastTrough[0] = totalLength;
                                lastTrough[1] = thisLength;
                                overTrough = true;
                            }
                        }
                        else if (overTrough)
                        {
                            troughInfo.Add(lastTrough);
                            troughPaths.Add(numPaths);
                            overTrough = false;
                            numPaths = -1;
                        }
                        totalLength += thisLength;
                    }

                    // add last tunnel item
                    if (overTrough)
                    {
                        troughInfo.Add(lastTrough);
                        troughPaths.Add(numPaths);
                    }

                    // add tunnel info to TrackCircuitSections
                    if (troughInfo.Count > 0)
                    {
                        bool TCSOverTrough = false;
                        float[] troughData = troughInfo[0];
                        float processedLength = 0;

                        for (int iXRef = thisNode.TCCrossReference.Count - 1; iXRef >= 0; iXRef--)
                        {
                            TrackCircuitSectionXref TCSXRef = thisNode.TCCrossReference[iXRef];
                            // forward direction
                            float TCSStartOffset = TCSXRef.OffsetLength[1];
                            float TCSLength = TCSXRef.Length;
                            TrackCircuitSection thisTCS = TrackCircuitList[TCSXRef.Index];

                            // if trough starts in TCS
                            while (troughData != null && troughData[0] <= (TCSStartOffset + TCSLength))
                            {
                                TrackCircuitSection.troughInfoData[] TCSTroughData = new TrackCircuitSection.troughInfoData[2];
                                float troughStart = 0;

                                // if in trough, set start in trough and check end
                                if (TCSOverTrough)
                                {
                                    TCSTroughData[1].TroughStart = -1;
                                    TCSTroughData[1].TCSStartOffset = processedLength;
                                }
                                else
                                // else start new trough
                                {
                                    TCSTroughData[1].TroughStart = troughData[0] - TCSStartOffset;
                                    troughStart = TCSTroughData[1].TroughStart;
                                    TCSTroughData[1].TCSStartOffset = -1;
                                }

                                if ((TCSStartOffset + TCSLength) >= (troughData[0] + troughData[1]))  // trough end is in this section
                                {
                                    TCSOverTrough = false;
                                    TCSTroughData[1].TroughEnd = troughStart + troughData[1] - processedLength;

                                    TCSTroughData[1].LengthInTCS = TCSTroughData[1].TroughEnd - troughStart;
                                    TCSTroughData[1].TotalLength = troughData[1];

                                    processedLength = 0;

                                    if (thisTCS.TroughInfo == null) thisTCS.TroughInfo = new List<TrackCircuitSection.troughInfoData[]>();
                                    thisTCS.TroughInfo.Add(TCSTroughData);

                                    if (troughInfo.Count >= 2)
                                    {
                                        troughInfo.RemoveAt(0);
                                        troughData = troughInfo[0];
                                        troughPaths.RemoveAt(0);
                                    }
                                    else
                                    {
                                        troughData = null;
                                        break;  // no more troughs to process
                                    }
                                }
                                else
                                {
                                    TCSOverTrough = true;

                                    TCSTroughData[1].TroughEnd = -1;
                                    TCSTroughData[1].LengthInTCS = TCSLength - troughStart;
                                    TCSTroughData[1].TotalLength = troughData[1];

                                    processedLength += TCSLength - troughStart;

                                    if (thisTCS.TroughInfo == null) thisTCS.TroughInfo = new List<TrackCircuitSection.troughInfoData[]>();
                                    thisTCS.TroughInfo.Add(TCSTroughData);
                                    break;  // cannot add more troughs to section
                                }
                            }
                            // derive trough data for other direction
                            if (thisTCS.TroughInfo != null)
                            {
                                foreach (TrackCircuitSection.troughInfoData[] thisTroughInfo in thisTCS.TroughInfo)
                                {
                                    thisTroughInfo[0].TroughStart = thisTroughInfo[1].TroughEnd < 0 ? -1 : thisTCS.Length - thisTroughInfo[1].TroughEnd;
                                    thisTroughInfo[0].TroughEnd = thisTroughInfo[1].TroughStart < 0 ? -1 : thisTCS.Length - thisTroughInfo[1].TroughStart;
                                    thisTroughInfo[0].LengthInTCS = thisTroughInfo[1].LengthInTCS;
                                    thisTroughInfo[0].TotalLength = thisTroughInfo[1].TotalLength;

                                    if (thisTroughInfo[0].TroughStart >= 0)
                                    {
                                        thisTroughInfo[0].TCSStartOffset = -1;
                                    }
                                    else if (thisTroughInfo[1].TCSStartOffset < 0)
                                    {
                                        thisTroughInfo[0].TCSStartOffset = thisTroughInfo[0].TotalLength - thisTroughInfo[0].LengthInTCS;
                                    }
                                    else
                                    {
                                        thisTroughInfo[0].TCSStartOffset = thisTroughInfo[0].TotalLength - thisTroughInfo[1].TCSStartOffset - thisTCS.Length;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find Train
        /// Find train in list using number, to restore reference after restore
        /// </summary>
        public static Train FindTrain(int number, List<Train> trains)
        {
            foreach (Train thisTrain in trains)
            {
                if (thisTrain.Number == number)
                {
                    return thisTrain;
                }
            }

            return null;
        }

        /// <summary>
        /// Request set switch
        /// Manual request to set switch, either from train or direct from node
        /// </summary>
        public static bool RequestSetSwitch(Train thisTrain, Direction direction)
        {
            if (thisTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            {
                return thisTrain.ProcessRequestManualSetSwitch(direction);
            }
            else if (thisTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
            {
                return thisTrain.ProcessRequestExplorerSetSwitch(direction);
            }
            return false;
        }

        public bool RequestSetSwitch(TrackNode switchNode)
        {
            return RequestSetSwitch(switchNode.TCCrossReference[0].Index);
        }

        public bool RequestSetSwitch(int trackCircuitIndex)
        {
            TrackCircuitSection switchSection = TrackCircuitList[trackCircuitIndex];
            Train thisTrain = switchSection.CircuitState.TrainReserved == null ? null : switchSection.CircuitState.TrainReserved.Train;
            bool switchReserved = switchSection.CircuitState.SignalReserved >= 0 || switchSection.CircuitState.TrainClaimed.Count > 0;
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

            return switchSet;
        }

        //only used by MP to manually set a switch to a desired position
        public bool RequestSetSwitch(TrackNode switchNode, int desiredState)
        {
            TrackCircuitSection switchSection = TrackCircuitList[switchNode.TCCrossReference[0].Index];
            bool switchReserved = switchSection.CircuitState.SignalReserved >= 0 || switchSection.CircuitState.TrainClaimed.Count > 0;
            bool switchSet = false;

            // It must be possible to force a switch also in its present state, not only in the opposite state
            if (!MPManager.IsServer())
                if (switchReserved) return false;
            //this should not be enforced in MP, as a train may need to be allowed to go out of the station from the side line

            if (!switchSection.CircuitState.HasTrainsOccupying())
            {
                switchSection.JunctionSetManual = desiredState;
                trackDB.TrackNodes[switchSection.OriginalIndex].TrJunctionNode.SelectedRoute = switchSection.JunctionSetManual;
                switchSection.JunctionLastRoute = switchSection.JunctionSetManual;
                switchSet = true;

                if (!Simulator.TimetableMode) switchSection.CircuitState.Forced = true;

                if (switchSection.LinkedSignals != null)
                {
                    foreach (int thisSignalIndex in switchSection.LinkedSignals)
                    {
                        SignalObject thisSignal = SignalObjects[thisSignalIndex];
                        thisSignal.Update();
                    }
                }

                var temptrains = Simulator.Trains.ToArray();

                foreach (var t in temptrains)
                {
                    if (t.TrainType != Train.TRAINTYPE.STATIC)
                    {
                        try
                        {
                            if (t.ControlMode != Train.TRAIN_CONTROL.AUTO_NODE && t.ControlMode != Train.TRAIN_CONTROL.AUTO_SIGNAL)
                                t.ProcessRequestExplorerSetSwitch(switchSection.Index);
                            else
                                t.ProcessRequestAutoSetSwitch(switchSection.Index);
                        }

                        catch
                        {
                        }
                    }
                }
            }
            return switchSet;
        }
    }
}
