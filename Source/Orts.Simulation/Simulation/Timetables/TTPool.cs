// COPYRIGHT 2014 by the Open Rails project.
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

// This code processes the Timetable definition and converts it into playable train information
//
// #define DEBUG_POOLINFO

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Orts.Parsers.OR;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using ORTS.Common;

namespace Orts.Simulation.Timetables
{
    /// <summary>
    /// Class Poolholder
    /// Interface class for access from Simulator
    /// </summary>
    public class Poolholder
    {
        public Dictionary<string, TimetablePool> Pools;

        /// <summary>
        /// Loader for timetable mode
        /// </summary>
        public Poolholder(Simulator simulatorref, string[] arguments, CancellationToken cancellation)
        {
            // Process pools
            PoolInfo TTPool = new PoolInfo(simulatorref);
            Pools = TTPool.ProcessPools(arguments, cancellation);

            // Process turntables
            TurntableInfo TTTurntable = new TurntableInfo(simulatorref);
            Dictionary<string, TimetableTurntablePool> TTTurntables = new Dictionary<string, TimetableTurntablePool>();
            TTTurntables = TTTurntable.ProcessTurntables(arguments, cancellation);

            // Add turntables to poolholder
            foreach (KeyValuePair<string, TimetableTurntablePool> thisTTTurntable in TTTurntables)
            {
                Pools.Add(thisTTTurntable.Key, thisTTTurntable.Value);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Loader for activity mode (dummy)
        /// </summary>
        public Poolholder()
        {
            Pools = null;
        }

        //================================================================================================//
        /// <summary>
        /// Loader for restore
        /// </summary>
        public Poolholder(BinaryReader inf, Simulator simulatorref)
        {
            Pools = null;

            int nopools = inf.ReadInt32();
            if (nopools > 0)
            {
                Pools = new Dictionary<string, TimetablePool>();
                for (int iPool = 0; iPool < nopools; iPool++)
                {
                    string type = inf.ReadString();

                    switch (type)
                    {
                        case "TimetablePool":
                            string poolKey = inf.ReadString();
                            TimetablePool newPool = new TimetablePool(inf, simulatorref);
                            Pools.Add(poolKey, newPool);
                            break;

                        case "TimetableTurntablePool":
                            string turntableKey = inf.ReadString();
                            TimetableTurntablePool newTurntable = new TimetableTurntablePool(inf, simulatorref);
                            Pools.Add(turntableKey, newTurntable);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Save
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            if (Pools == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(Pools.Count);
                foreach (KeyValuePair<string, TimetablePool> thisPool in Pools)
                {
                    if (thisPool.Value.GetType() == typeof(TimetableTurntablePool))
                    {
                        outf.Write("TimetableTurntablePool");
                        outf.Write(thisPool.Key);
                        TimetableTurntablePool thisTurntable = thisPool.Value as TimetableTurntablePool;
                        thisTurntable.Save(outf);
                    }
                    else
                    {
                        outf.Write("TimetablePool");
                        outf.Write(thisPool.Key);
                        thisPool.Value.Save(outf);
                    }
                }
            }
        }
    }

    //================================================================================================//
    /// <summary>
    /// Class TimetablePool
    /// Class holding all pool details
    /// </summary>
    public class TimetablePool
    {
        public enum TrainFromPool
        {
            NotCreated,
            Delayed,
            Formed,
            ForceCreated,
            Failed,
        }

        public enum PoolExitDirectionEnum
        {
            Backward,
            Forward,
            Undefined,
        }

        public string PoolName = String.Empty;
        public bool ForceCreation;

        public struct PoolDetails
        {
            public Train.TCSubpathRoute StoragePath;       // Path defined as storage location
            public Traveller StoragePathTraveller;         // Traveller used to get path position and direction
            public Traveller StoragePathReverseTraveller;  // Traveller used if path must be reversed
            public string StorageName;                     // Storage name
            public List<Train.TCSubpathRoute> AccessPaths; // Access paths defined for storage location
            public float StorageLength;                    // Available length
            public float StorageCorrection;                // Length correction (e.g. due to switch overlap safety) - difference between length of sections in path and actual storage length

            public int TableExitIndex;                     // Index in table exit list for this exit
            public int TableVectorIndex;                   // Index in VectorList of tracknode which is the table
            public float TableMiddleEntry;                 // Offset of middle of moving table when approaching table (for turntable and transfertable)
            public float TableMiddleExit;                  // Offset of middle of moving table when exiting table (for turntable and transfertable)

            public List<int> StoredUnits;                  // Stored no. of units
            public List<int> ClaimUnits;                   // Units which have claimed storage but not yet in pool
            public int? maxStoredUnits;                    // Max. no of stored units for storage track
            public float RemLength;                        // Remaining storage length
        }

        public List<PoolDetails> StoragePool = new List<PoolDetails>();

        //================================================================================================//
        /// <summary>
        /// Empty constructor for use by children
        /// </summary>
        /// <param name="filePath"></param>
        public TimetablePool()
        {
        }

        //================================================================================================//
        /// <summary>
        /// Constructor to read pool info from csv file
        /// </summary>
        /// <param name="filePath"></param>
        public TimetablePool(TimetableReader fileContents, ref int lineindex, Simulator simulatorref)
        {
            bool validpool = true;
            bool newName = false;
            bool firstName = false;

            ForceCreation = simulatorref.Settings.TTCreateTrainOnPoolUnderflow;

            // Loop through definitions
            while (lineindex < fileContents.Strings.Count && !newName)
            {
                string[] inputLine = fileContents.Strings[lineindex];

                // Switch through definitions
                switch (inputLine[0].ToLower().Trim())
                {
                    // Comment: do not process
                    case "#comment":
                        lineindex++;
                        break;

                    // Name: set as name
                    case "#name":
                        newName = firstName;
                        if (!firstName)
                        {
                            lineindex++;
                            firstName = true;
                            PoolName = String.Copy(inputLine[1].ToLower().Trim());
                        }
                        break;

                    // Storage: read path, add to path list
                    case "#storage":
                        if (String.IsNullOrEmpty(PoolName))
                        {
                            Trace.TraceInformation("Pool : " + fileContents.FilePath + " : missing pool name \n");
                            validpool = false;
                            lineindex++;
                        }
                        else
                        {
                            bool validStorage = true;
                            PoolDetails thisPool = ExtractStorage(fileContents, simulatorref, ref lineindex, out validStorage, true);
                            if (validStorage)
                            {
                                StoragePool.Add(thisPool);
                            }
                            else
                            {
                                validpool = false;
                            }
                        }
                        break;

                    default:
                        Trace.TraceInformation("Pool : " + fileContents.FilePath + " : line : " + (lineindex - 1) + " : unexpected line defitinion : " + inputLine[0] + "\n");
                        lineindex++;
                        break;
                }
            }

            // Reset poolname if not valid
            if (!validpool)
            {
                PoolName = String.Empty;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Constructor for restore
        /// </summary>
        /// <param name="inf"></param>
        public TimetablePool(BinaryReader inf, Simulator simulatorref)
        {
            PoolName = inf.ReadString();
            ForceCreation = inf.ReadBoolean();

            int noPools = inf.ReadInt32();
            for (int iPool = 0; iPool < noPools; iPool++)
            {
                int maxStorage = 0;

                PoolDetails newPool = new PoolDetails
                {
                    StoragePath = new Train.TCSubpathRoute(inf),
                    StoragePathTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes, inf),
                    StoragePathReverseTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes, inf),
                    StorageName = inf.ReadString(),
                    AccessPaths = new List<Train.TCSubpathRoute>()
                };
                int noAccessPaths = inf.ReadInt32();

                for (int iPath = 0; iPath < noAccessPaths; iPath++)
                {
                    newPool.AccessPaths.Add(new Train.TCSubpathRoute(inf));
                }

                newPool.StoredUnits = new List<int>();
                int noStoredUnits = inf.ReadInt32();

                for (int iUnits = 0; iUnits < noStoredUnits; iUnits++)
                {
                    newPool.StoredUnits.Add(inf.ReadInt32());
                }

                newPool.ClaimUnits = new List<int>();
                int noClaimUnits = inf.ReadInt32();

                for (int iUnits = 0; iUnits < noClaimUnits; iUnits++)
                {
                    newPool.ClaimUnits.Add(inf.ReadInt32());
                }

                newPool.StorageLength = inf.ReadSingle();
                newPool.StorageCorrection = inf.ReadSingle();

                newPool.TableExitIndex = inf.ReadInt32();
                newPool.TableVectorIndex = inf.ReadInt32();
                newPool.TableMiddleEntry = inf.ReadSingle();
                newPool.TableMiddleExit = inf.ReadSingle();

                newPool.RemLength = inf.ReadSingle();

                maxStorage = inf.ReadInt32();
                newPool.maxStoredUnits = maxStorage <= 0 ? null : (int?)maxStorage;

                StoragePool.Add(newPool);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Method to save pool
        /// </summary>
        /// <param name="outf"></param>
        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(PoolName);
            outf.Write(ForceCreation);

            outf.Write(StoragePool.Count);

            foreach (PoolDetails thisStorage in StoragePool)
            {
                thisStorage.StoragePath.Save(outf);
                thisStorage.StoragePathTraveller.Save(outf);
                thisStorage.StoragePathReverseTraveller.Save(outf);
                outf.Write(thisStorage.StorageName);

                outf.Write(thisStorage.AccessPaths.Count);
                foreach (Train.TCSubpathRoute thisPath in thisStorage.AccessPaths)
                {
                    thisPath.Save(outf);
                }

                outf.Write(thisStorage.StoredUnits.Count);
                foreach (int storedUnit in thisStorage.StoredUnits)
                {
                    outf.Write(storedUnit);
                }

                outf.Write(thisStorage.ClaimUnits.Count);
                foreach (int claimUnit in thisStorage.ClaimUnits)
                {
                    outf.Write(claimUnit);
                }

                outf.Write(thisStorage.StorageLength);
                outf.Write(thisStorage.StorageCorrection);

                outf.Write(thisStorage.TableExitIndex);
                outf.Write(thisStorage.TableVectorIndex);
                outf.Write(thisStorage.TableMiddleEntry);
                outf.Write(thisStorage.TableMiddleExit);

                outf.Write(thisStorage.RemLength);

                if (thisStorage.maxStoredUnits.HasValue)
                {
                    outf.Write(thisStorage.maxStoredUnits.Value);
                }
                else
                {
                    outf.Write(-1);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Extract details for storage area
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="lineindex"></param>
        /// <param name="simulatorref"></param>
        /// <param name="validStorage"></param>
        /// <returns></returns>
        public PoolDetails ExtractStorage(TimetableReader fileContents, Simulator simulatorref, ref int lineindex, out bool validStorage, bool reqAccess)
        {
            PoolDetails newPool = new PoolDetails();
            List<string> accessPathNames = new List<string>();

            string[] inputLine = fileContents.Strings[lineindex];
            string storagePathName = String.Copy(inputLine[1]);

            int? maxStoredUnits = null;

            lineindex++;
            inputLine = fileContents.Strings[lineindex];

            bool endOfStorage = false;
            validStorage = true;

            // Extract access paths
            while (lineindex < fileContents.Strings.Count && !endOfStorage)
            {
                inputLine = fileContents.Strings[lineindex];
                switch (inputLine[0].ToLower().Trim())
                {
                    // Skip comment
                    case "#comment":
                        lineindex++;
                        break;

                    // Exit on next name
                    case "#name":
                        endOfStorage = true;
                        break;

                    // Storage: next storage area
                    case "#storage":
                        endOfStorage = true;
                        break;

                    // Maxstorage: set max storage for this storage track
                    case "#maxunits":
                        try
                        {
                            maxStoredUnits = Convert.ToInt32(inputLine[1]);
                        }
                        catch
                        {
                            Trace.TraceInformation("Invalid value for maxunits : {0} for storage {1} in pool {2} ; definition ignored", inputLine, storagePathName, PoolName);
                            maxStoredUnits = null;
                        }
                        lineindex++;
                        break;

                    // Access paths: process
                    case "#access":
                        int nextfield = 1;

                        while (nextfield < inputLine.Length && !String.IsNullOrEmpty(inputLine[nextfield]))
                        {
                            accessPathNames.Add(String.Copy(inputLine[nextfield]));
                            nextfield++;
                        }

                        lineindex++;
                        break;

                    // Settings: check setting
                    case "#settings":
                        nextfield = 1;
                        while (nextfield < inputLine.Length)
                        {
                            if (!String.IsNullOrEmpty(inputLine[nextfield]))
                            {
                                switch (inputLine[nextfield].ToLower().Trim())
                                {
                                    default:
                                        break;
                                }
                            }
                            nextfield++;
                        }
                        lineindex++;
                        break;

                    default:
                        Trace.TraceInformation("Pool : " + fileContents.FilePath + " : line : " + lineindex + " : unknown definition : " + inputLine[0] + " ; line ignored \n");
                        lineindex++;
                        break;
                }
            }

            // Check if access paths defined
            if (reqAccess && accessPathNames.Count <= 0)
            {
                Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage : " + storagePathName + " : no access paths defined \n");
                validStorage = false;
                return newPool;
            }

            // Process storage paths
            newPool.AccessPaths = new List<Train.TCSubpathRoute>();
            newPool.StoredUnits = new List<int>();
            newPool.ClaimUnits = new List<int>();
            newPool.StorageLength = 0.0f;
            newPool.RemLength = 0.0f;
            newPool.maxStoredUnits = maxStoredUnits;

            bool pathValid = true;
            TimetableInfo TTInfo = new TimetableInfo(simulatorref);
            AIPath newPath = TTInfo.LoadPath(storagePathName, out pathValid);

            if (pathValid)
            {
                Train.TCRoutePath fullRoute = new Train.TCRoutePath(newPath, -2, 1, simulatorref.Signals, -1, simulatorref.Settings);

                // Front traveller
                newPool.StoragePath = new Train.TCSubpathRoute(fullRoute.TCRouteSubpaths[0]);
                newPool.StoragePathTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes, newPath);

                // Rear traveller (for moving tables)
                AIPathNode lastNode = newPath.Nodes.Last();
                Traveller.TravellerDirection newDirection = newPool.StoragePathTraveller.Direction == Traveller.TravellerDirection.Forward ? Traveller.TravellerDirection.Backward : Traveller.TravellerDirection.Forward;
                newPool.StoragePathReverseTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes,
                    lastNode.Location.TileX, lastNode.Location.TileZ, lastNode.Location.Location.X, lastNode.Location.Location.Z, newDirection);

                newPool.StorageName = String.Copy(storagePathName);

                // If last element is end of track, remove it from path
                int lastSectionIndex = newPool.StoragePath[newPool.StoragePath.Count - 1].TCSectionIndex;
                if (simulatorref.Signals.TrackCircuitList[lastSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                {
                    newPool.StoragePath.RemoveAt(newPool.StoragePath.Count - 1);
                }

                // Check for multiple subpaths - not allowed for storage area
                if (fullRoute.TCRouteSubpaths.Count > 1)
                {
                    Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage area : " + storagePathName + " : storage path may not contain multiple subpaths\n");
                }
            }
            else
            {
                Trace.TraceWarning("Pool : " + fileContents.FilePath + " : error while processing storege area path : " + storagePathName + "\n");
                validStorage = false;
                return newPool;
            }

            // Process access paths
            foreach (string accessPath in accessPathNames)
            {
                pathValid = true;
                newPath = TTInfo.LoadPath(accessPath, out pathValid);

                if (pathValid)
                {
                    Train.TCRoutePath fullRoute = new Train.TCRoutePath(newPath, -2, 1, simulatorref.Signals, -1, simulatorref.Settings);
                    // If last element is end of track, remove it from path
                    Train.TCSubpathRoute usedRoute = fullRoute.TCRouteSubpaths[0];
                    int lastIndex = usedRoute.Count - 1;
                    int lastSectionIndex = usedRoute[lastIndex].TCSectionIndex;
                    if (simulatorref.Signals.TrackCircuitList[lastSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                    {
                        lastIndex = usedRoute.Count - 2;
                    }
                    newPool.AccessPaths.Add(new Train.TCSubpathRoute(usedRoute, 0, lastIndex));

                    // Check for multiple subpaths - not allowed for storage area
                    if (fullRoute.TCRouteSubpaths.Count > 1)
                    {
                        Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage area : " + accessPath + " : access path may not contain multiple subpaths\n");
                    }
                }
                else
                {
                    Trace.TraceWarning("Pool : " + fileContents.FilePath + " : error while processing access path : " + accessPath + "\n");
                    validStorage = false;
                }
            }

            // Verify proper access route definition
            if (!validStorage)
            {
                return newPool;
            }

            for (int iPath = 0; iPath < newPool.AccessPaths.Count; iPath++)
            {
                Train.TCSubpathRoute accessPath = newPool.AccessPaths[iPath];
                int firstAccessSection = accessPath[0].TCSectionIndex;
                int firstAccessDirection = accessPath[0].Direction;
                string accessName = accessPathNames[iPath];

                int reqElementIndex = newPool.StoragePath.GetRouteIndex(firstAccessSection, 0);

                if (reqElementIndex < 0)
                {
                    Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage area : " + newPool.StorageName +
                        " : access path : " + accessName + " does not start within storage area\n");
                    validStorage = false;
                }
                else
                {
                    // Check storage path direction, reverse path if required
                    // Path may be in wrong direction due to path conversion problems
                    if (firstAccessDirection != newPool.StoragePath[reqElementIndex].Direction)
                    {
                        Train.TCSubpathRoute newRoute = new Train.TCSubpathRoute();
                        for (int iElement = newPool.StoragePath.Count - 1; iElement >= 0; iElement--)
                        {
                            Train.TCRouteElement thisElement = newPool.StoragePath[iElement];
                            thisElement.Direction = thisElement.Direction == 0 ? 1 : 0;
                            newRoute.Add(thisElement);
                        }
                        newPool.StoragePath = new Train.TCSubpathRoute(newRoute);
                    }

                    // Remove elements from access path which are part of storage path
                    int lastReqElement = accessPath.Count - 1;
                    int storageRouteIndex = newPool.StoragePath.GetRouteIndex(accessPath[lastReqElement].TCSectionIndex, 0);

                    while (storageRouteIndex >= 0 && lastReqElement > 0)
                    {
                        lastReqElement--;
                        storageRouteIndex = newPool.StoragePath.GetRouteIndex(accessPath[lastReqElement].TCSectionIndex, 0);
                    }

                    newPool.AccessPaths[iPath] = new Train.TCSubpathRoute(accessPath, 0, lastReqElement);
                }
            }

            // Calculate storage length
            if (!validStorage)
            {
                return newPool;
            }
            float storeLength = 0;
            foreach (Train.TCRouteElement thisElement in newPool.StoragePath)
            {
                storeLength += simulatorref.Signals.TrackCircuitList[thisElement.TCSectionIndex].Length;
            }

            // If storage ends at switch, deduct switch safety distance
            float addedLength = 0;
            foreach (Train.TCRouteElement thisElement in newPool.StoragePath)
            {
                TrackCircuitSection thisSection = simulatorref.Signals.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                {
                    addedLength -= (float)thisSection.Overlap;
                    break;
                }
                else
                {
                    // Count length only if not part of storage path itself
                    if (newPool.StoragePath.GetRouteIndex(thisSection.Index, 0) < 0)
                    {
                        addedLength += thisSection.Length;
                    }
                }
            }

            // If switch overlap exceeds distance between end of storage and switch, deduct from storage length
            if (addedLength < 0)
            {
                storeLength += addedLength;
            }

            newPool.StorageLength = storeLength;
            newPool.StorageCorrection = addedLength;
            newPool.RemLength = storeLength;

            return newPool;
        }

        //================================================================================================//
        /// <summary>
        /// TestPoolExit: test if end of route is access to required pool
        /// </summary>
        /// <param name="train"></param>
        public virtual bool TestPoolExit(TTTrain train)
        {
            bool validPool = false;

            // Set dispose states
            train.FormsStatic = true;
            train.Closeup = true;

            // Find relevant access path
            int lastSectionIndex = train.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
            int lastSectionDirection = train.TCRoute.TCRouteSubpaths.Last().Last().Direction;

            // Use first storage path to get pool access path
            PoolDetails thisStorage = StoragePool[0];
            int reqPath = -1;
            int reqPathIndex = -1;

            // Find relevant access path
            for (int iPath = 0; iPath < thisStorage.AccessPaths.Count && reqPath < 0; iPath++)
            {
                Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[iPath];
                reqPathIndex = accessPath.GetRouteIndex(lastSectionIndex, 0);

                // Path is defined outbound, so directions must be opposite
                if (reqPathIndex >= 0 && accessPath[reqPathIndex].Direction != lastSectionDirection)
                {
                    reqPath = iPath;
                }
            }

            // None found
            if (reqPath < 0)
            {
                Trace.TraceWarning("Train : " + train.Name + " : no valid path found to access pool storage " + PoolName + "\n");
                train.FormsStatic = false;
                train.Closeup = false;
            }
            else
            {
                // Path found: extend train path with access and storage paths
                train.PoolAccessSection = lastSectionIndex;
                validPool = true;
            }

            return validPool;
        }

        //================================================================================================//
        /// <summary>
        /// Create in pool: create train in pool
        /// </summary>
        /// <param name="train"></param>
        public virtual int CreateInPool(TTTrain train, List<TTTrain> nextTrains)
        {
            int PoolStorageState = (int)TTTrain.PoolAccessState.PoolInvalid;
            train.TCRoute.TCRouteSubpaths[0] = PlaceInPool(train, out PoolStorageState, false);
            train.ValidRoute[0] = new Train.TCSubpathRoute(train.TCRoute.TCRouteSubpaths[0]);
            train.TCRoute.activeSubpath = 0;

            // If no storage available - abondone train
            if (PoolStorageState < 0)
            {
                return PoolStorageState;
            }

            // Use stored traveller
            train.PoolStorageIndex = PoolStorageState;
            train.RearTDBTraveller = new Traveller(StoragePool[train.PoolStorageIndex].StoragePathTraveller);

            // If storage available check for other engines on storage track
            if (StoragePool[train.PoolStorageIndex].StoredUnits.Count > 0)
            {
                int lastTrainNumber = StoragePool[train.PoolStorageIndex].StoredUnits[StoragePool[train.PoolStorageIndex].StoredUnits.Count - 1];
                TTTrain lastTrain = train.GetOtherTTTrainByNumber(lastTrainNumber) ?? train.Simulator.GetAutoGenTTTrainByNumber(lastTrainNumber);
                if (lastTrain != null)
                {
                    train.CreateAhead = String.Copy(lastTrain.Name).ToLower();
                }
            }

            bool validPosition = false;
            Train.TCSubpathRoute tempRoute = train.CalculateInitialTTTrainPosition(ref validPosition, nextTrains);

            if (validPosition)
            {
                train.SetInitialTrainRoute(tempRoute);
                train.CalculatePositionOfCars();
                for (int i = 0; i < train.Cars.Count; i++)
                    train.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
                train.ResetInitialTrainRoute(tempRoute);

                // Set train route and position so proper position in pool can be calculated
                train.UpdateTrainPosition();

                // Add unit to pool
                AddUnit(train, false);
                validPosition = train.PostInit(false); // Post init train but do not activate
            }

            return PoolStorageState;
        }

        //================================================================================================//
        /// <summary>
        /// Place in pool: place train in pool
        /// </summary>
        /// <param name="train"></param>
        public virtual Train.TCSubpathRoute PlaceInPool(TTTrain train, out int poolStorageIndex, bool checkAccessPath)
        {
            int tempIndex;
            Train.TCSubpathRoute newRoute = SetPoolExit(train, out tempIndex, checkAccessPath);
            poolStorageIndex = tempIndex;
            return newRoute;
        }

        //================================================================================================//
        /// <summary>
        /// SetPoolExit: adjust train dispose details and path to required pool exit
        /// Returned poolStorageState : <0 : state (enum TTTrain.PoolAccessState); >0 : poolIndex
        /// </summary>
        /// <param name="train"></param>
        public virtual Train.TCSubpathRoute SetPoolExit(TTTrain train, out int poolStorageState, bool checkAccessPath)
        {
            // New route
            Train.TCSubpathRoute newRoute = null;
            poolStorageState = (int)TTTrain.PoolAccessState.PoolInvalid;

            // Set dispose states
            train.FormsStatic = true;
            train.Closeup = true;

            // Find relevant access path
            int lastSectionIndex = train.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
            int lastSectionDirection = train.TCRoute.TCRouteSubpaths.Last().Last().Direction;

            // Find storage path with enough space to store train
            poolStorageState = GetPoolExitIndex(train);

            if (poolStorageState == (int)TTTrain.PoolAccessState.PoolOverflow)
            {
                // Pool overflow
                Trace.TraceWarning("Pool : " + PoolName + " : overflow : cannot place train : " + train.Name + "\n");

                if (train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Required Pool Exit : " + PoolName + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Pool overflow : train length : " + train.Length + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "                pool lengths : \n");
                    foreach (PoolDetails thisStorage in StoragePool)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "                  path : " + thisStorage.StorageName + " ; stored units : " +
                            thisStorage.StoredUnits.Count + " ; rem length : " + thisStorage.RemLength + "\n");
                    }
                }

                // Train will be abandoned when reaching end of path
                train.FormsStatic = false;
                train.Closeup = false;
            }
            else if (poolStorageState == (int)TTTrain.PoolAccessState.PoolInvalid)
            {
                // Pool invalid
                Trace.TraceWarning("Pool : " + PoolName + " : no valid pool found : " + train.Name + "\n");

                if (train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Required Pool Exit : " + PoolName + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "No valid pool found \n");
                }

                // Train will be abandoned when reaching end of path
                train.FormsStatic = false;
                train.Closeup = false;
            } // No action if state is poolClaimed - state will resolve as train ahead is stabled in pool
            else if (poolStorageState >= 0)
            {
                // valid pool
                PoolDetails thisStorage = StoragePool[poolStorageState];
                train.PoolStorageIndex = poolStorageState;

                if (checkAccessPath)
                {
                    int reqPath = -1;
                    int reqPathIndex = -1;

                    // Find relevant access path
                    for (int iPath = 0; iPath < thisStorage.AccessPaths.Count && reqPath < 0; iPath++)
                    {
                        Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[iPath];
                        reqPathIndex = accessPath.GetRouteIndex(lastSectionIndex, 0);

                        // Path is defined outbound, so directions must be opposite
                        if (reqPathIndex >= 0 && accessPath[reqPathIndex].Direction != lastSectionDirection)
                        {
                            reqPath = iPath;
                        }
                    }

                    // None found
                    if (reqPath < 0)
                    {
                        Trace.TraceWarning("Train : " + train.Name + " : no valid path found to access pool storage " + PoolName + "\n");
                        train.FormsStatic = false;
                        train.Closeup = false;
                        poolStorageState = -1;
                    }
                    // Path found: extend train path with access and storage paths
                    else
                    {
                        Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[reqPath];
                        newRoute = new Train.TCSubpathRoute(train.TCRoute.TCRouteSubpaths.Last());

                        // Add elements from access route except those allready on the path
                        // Add in reverse order and reverse direction as path is defined outbound
                        for (int iElement = reqPathIndex; iElement >= 0; iElement--)
                        {
                            if (newRoute.GetRouteIndex(accessPath[iElement].TCSectionIndex, 0) < 0)
                            {
                                Train.TCRouteElement newElement = new Train.TCRouteElement(accessPath[iElement]);
                                newElement.Direction = newElement.Direction == 1 ? 0 : 1;
                                newRoute.Add(newElement);
                            }
                        }
                        // Add elements from storage
                        for (int iElement = thisStorage.StoragePath.Count - 1; iElement >= 0; iElement--)
                        {
                            if (newRoute.GetRouteIndex(thisStorage.StoragePath[iElement].TCSectionIndex, 0) < 0)
                            {
                                Train.TCRouteElement newElement = new Train.TCRouteElement(thisStorage.StoragePath[iElement]);
                                newElement.Direction = newElement.Direction == 1 ? 0 : 1;
                                newRoute.Add(newElement);
                            }
                        }
                        // Set pool claim
                        AddUnit(train, true);
                        thisStorage.ClaimUnits.Add(train.Number);
                    }
                }
                else
                {
                    // Create new route from storage and access track only
                    newRoute = new Train.TCSubpathRoute(thisStorage.AccessPaths[0]);

                    foreach (Train.TCRouteElement thisElement in thisStorage.StoragePath)
                    {
                        if (newRoute.GetRouteIndex(thisElement.TCSectionIndex, 0) < 0)
                        {
                            Train.TCRouteElement newElement = new Train.TCRouteElement(thisElement);
                            newRoute.Add(newElement);
                        }
                    }
                }
            }

            return newRoute;
        }

        //================================================================================================//
        /// <summary>
        /// Base class to allow override for moving table classes
        /// </summary>
        public virtual float GetEndOfRouteDistance(Train.TCSubpathRoute thisRoute, Train.TCPosition frontPosition, int pathIndex, Signals signalRef)
        {
            return 0;
        }

        //================================================================================================//
        /// <summary>
        /// GetPoolExitIndex: get pool index for train exiting to pool
        /// Returned poolStorageState: <0: state (enum TTTrain.PoolAccessState); >0 : poolIndex
        /// </summary>
        /// <param name="train"></param>
        public int GetPoolExitIndex(TTTrain train)
        {
            // Find storage path with enough space to store train
            int reqPool = (int)TTTrain.PoolAccessState.PoolInvalid;
            for (int iPool = 0; iPool < StoragePool.Count && reqPool < 0; iPool++)
            {
                PoolDetails thisStorage = StoragePool[iPool];

                // Check on max units on storage track
                bool maxUnitsReached = false;
                if (thisStorage.maxStoredUnits.HasValue)
                {
                    maxUnitsReached = thisStorage.StoredUnits.Count >= thisStorage.maxStoredUnits.Value;
                }

                // Train already has claimed space
                if (thisStorage.ClaimUnits.Contains(train.Number))
                {
                    reqPool = iPool;
                }
                else if (thisStorage.StoredUnits.Contains(train.Number))
                {
#if DEBUG_POOLINFO
                    var sob = new StringBuilder();
                    DateTime baseDT = new DateTime();
                    DateTime actTime = baseDT.AddSeconds(train.AI.clockTime);

                    sob.AppendFormat("{0} : Pool {1} : error : train {2} ({3}) allready stored in pool \n", actTime.ToString("HH:mm:ss"),PoolName, train.Number, train.Name);

                    int totalno = 0;
                    foreach (PoolDetails selStorage in StoragePool)
                    {
                        totalno += selStorage.StoredUnits.Count;
                    }
                    sob.AppendFormat("           stored units {0} : {1} (total {2})", PoolName, thisStorage.StoredUnits.Count, totalno);
                    File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                }
                else if (thisStorage.RemLength > train.Length && !maxUnitsReached)
                {
                    reqPool = iPool;
                }
            }

            // If no valid pool found, check if any paths have a claimed train
            // Else state is pool overflow
            if (reqPool < 0)
            {
                reqPool = (int)TTTrain.PoolAccessState.PoolOverflow;

                foreach (PoolDetails thisPool in StoragePool)
                {
                    if (thisPool.ClaimUnits.Count > 0)
                    {
                        reqPool = (int)TTTrain.PoolAccessState.PoolClaimed;
                        break;
                    }
                }
            }

            return reqPool;
        }

        //================================================================================================//
        /// <summary>
        /// Test if route leads to pool
        /// </summary>
        public bool TestRouteLeadingToPool(Train.TCSubpathRoute testedRoute, int poolIndex, string dumpfile, string trainName)
        {
            Train.TCSubpathRoute poolStorage = StoragePool[poolIndex].StoragePath;

            // Check if signal route leads to pool
            foreach (Train.TCRouteElement routeElement in poolStorage)
            {
                if (testedRoute.GetRouteIndex(routeElement.TCSectionIndex, 0) > 0)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("CALL ON : Train {0} : valid - train is going into pool {1} \n", trainName, PoolName);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }
                    return true;
                }
            }

            return false;
        }

        //================================================================================================//
        /// <summary>
        /// AddUnit: add unit to pool, update remaining length
        /// </summary>
        /// <param name="train"></param>
        public void AddUnit(TTTrain train, bool claimOnly)
        {
            PoolDetails thisPool = StoragePool[train.PoolStorageIndex];

            // If train has already claimed position, remove claim
            if (thisPool.ClaimUnits.Contains(train.Number))
            {
                thisPool.ClaimUnits.Remove(train.Number);
                thisPool.RemLength = CalculateStorageLength(thisPool, train);
            }
            else
            {
                // Add train to pool
                thisPool.StoredUnits.Add(train.Number);

                thisPool.RemLength = CalculateStorageLength(thisPool, train);
                StoragePool[train.PoolStorageIndex] = thisPool;

#if DEBUG_POOLINFO
                var sob = new StringBuilder();
                DateTime baseDT = new DateTime();
                DateTime actTime = baseDT.AddSeconds(train.AI.clockTime);

                sob.AppendFormat("{0} : Pool {1} : train {2} ({3}) added\n", actTime.ToString("HH:mm:ss"),PoolName, train.Number, train.Name);

                int totalno = 0;
                foreach (PoolDetails selStorage in StoragePool)
                {
                    totalno += selStorage.StoredUnits.Count;
                }

                sob.AppendFormat("           stored units {0} : {1} (total {2})", PoolName, thisPool.StoredUnits.Count, totalno);
                File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif

#if DEBUG_TRACEINFO
                Trace.TraceInformation("Pool {0} : added unit : {1} ; stored units : {2} ( last = {3} )", PoolName, train.Name, thisPool.StoredUnits.Count, thisPool.StoredUnits.Last());
#endif
            }

            // Update altered pool
            StoragePool[train.PoolStorageIndex] = thisPool;

            // If claim only, do not reset track section states
            if (claimOnly)
            {
                return;
            }

            // Clear track behind engine, only keep actual occupied sections
            Train.TCSubpathRoute tempRoute = train.signalRef.BuildTempRoute(train, train.PresentPosition[1].TCSectionIndex, train.PresentPosition[1].TCOffset,
                train.PresentPosition[1].TCDirection, train.Length, true, true, false);
            train.OccupiedTrack.Clear();

            foreach (Train.TCRouteElement thisElement in tempRoute)
            {
                train.OccupiedTrack.Add(train.signalRef.TrackCircuitList[thisElement.TCSectionIndex]);
            }

            train.ClearActiveSectionItems();
        }

        //================================================================================================//
        /// <summary>
        /// Calculate remaining storage length
        /// </summary>
        /// <param name="reqStorage"></param>
        /// <param name="train"></param> is last train in storage (one just added, or one remaining as previous last stored unit), = null if storage is empty
        /// <returns></returns>
        public float CalculateStorageLength(PoolDetails reqStorage, TTTrain train)
        {
            // No trains in storage
            if (reqStorage.StoredUnits.Count <= 0)
            {
                return reqStorage.StorageLength;
            }

            // Calculate remaining length
            int occSectionIndex = train.PresentPosition[0].TCSectionIndex;
            int occSectionDirection = train.PresentPosition[0].TCDirection;
            int storageSectionIndex = reqStorage.StoragePath.GetRouteIndex(occSectionIndex, 0);

            // If train not stopped in pool, return remaining length = 0
            if (storageSectionIndex < 0)
            {
                return 0;
            }

            int storageSectionDirection = reqStorage.StoragePath[storageSectionIndex].Direction;
            // If directions of paths are equal, use front section, section.length - position.offset, and use front of train position

            float remLength = 0;

            // Same direction : use rear of train position
            // For turntable pools, path is defined in opposite direction
            if (GetType() == typeof(TimetableTurntablePool))
            {
                if (occSectionDirection == storageSectionDirection)
                {
                    // Use rear of train position
                    occSectionIndex = train.PresentPosition[1].TCSectionIndex;
                    TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                    remLength = train.PresentPosition[1].TCOffset;
                }
                else
                {
                    // Use front of train position
                    TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                    remLength = occSection.Length - train.PresentPosition[0].TCOffset;
                }
            }
            else
            {
                if (occSectionDirection == storageSectionDirection)
                {
                    // Use rear of train position
                    occSectionIndex = train.PresentPosition[1].TCSectionIndex;
                    TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                    remLength = occSection.Length - train.PresentPosition[1].TCOffset;
                }
                else
                {
                    // Use front of train position
                    TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                    remLength = train.PresentPosition[0].TCOffset;
                }
            }

            for (int iSection = reqStorage.StoragePath.Count - 1; iSection >= 0 && reqStorage.StoragePath[iSection].TCSectionIndex != occSectionIndex; iSection--)
            {
                remLength += train.signalRef.TrackCircuitList[reqStorage.StoragePath[iSection].TCSectionIndex].Length;
            }

            // Position was furthest down the storage area, so take off train length
            remLength -= train.Length;

            // Correct for overlap etc.
            remLength += reqStorage.StorageCorrection; // Storage correction is negative!

            return remLength;
        }

        //================================================================================================//
        /// <summary>
        /// Extract train from pool
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        public virtual TrainFromPool ExtractTrain(ref TTTrain train, int presentTime)
        {
#if DEBUG_POOLINFO
            var sob = new StringBuilder();
            DateTime baseDT = new DateTime();
            DateTime actTime = baseDT.AddSeconds(train.AI.clockTime);

            sob.AppendFormat("{0} : Pool {1} : request for train {2} ({3})", actTime.ToString("HH:mm:ss"), PoolName, train.Number, train.Name);
            File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
            // Check if any engines available
            int selectedTrainNumber = -1;
            int selectedStorage = -1;
            bool claimActive = false;

            for (int iStorage = 0; iStorage < StoragePool.Count; iStorage++)
            {
                PoolDetails thisStorage = StoragePool[iStorage];
                // Engine has claimed access - this storage cannot be used for exit right now
                if (thisStorage.ClaimUnits.Count > 0)
                {
                    claimActive = true;
                }
                else if (thisStorage.StoredUnits.Count > 0)
                {
                    selectedTrainNumber = thisStorage.StoredUnits[thisStorage.StoredUnits.Count - 1];
                    selectedStorage = iStorage;
                    break;
                }
            }

            if (selectedTrainNumber < 0)
            {
                // No train found but claim is active - create engine is delayed
                if (claimActive)
                {
#if DEBUG_TRACEINFO
                Trace.TraceInformation("Pool {0} : train {1} : delayed through claimed access\n", PoolName, train.Name);
#endif
#if DEBUG_POOLINFO
                Trace.TraceInformation("Pool {0} : train {1} : delayed through claimed access\n", PoolName, train.Name);
#endif
                    return TrainFromPool.Delayed;
                }

                // Pool underflow: create engine from scratch
                DateTime baseDTA = new DateTime();
                DateTime moveTimeA = baseDTA.AddSeconds(train.AI.clockTime);

                if (ForceCreation)
                {
                    Trace.TraceInformation("Train request : " + train.Name + " from pool " + PoolName +
                        " : no engines available in pool, engine is created, at " + moveTimeA.ToString("HH:mm:ss") + "\n");
#if DEBUG_POOLINFO
                    sob = new StringBuilder();
                    sob.AppendFormat("Pool {0} : train {1} ({2}) : no units available, engine force created", PoolName, train.Number, train.Name);
                    File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                    return TrainFromPool.ForceCreated;
                }
                else
                {
                    Trace.TraceInformation("Train request : " + train.Name + " from pool " + PoolName +
                        " : no engines available in pool, engine is not created , at " + moveTimeA.ToString("HH:mm:ss") + "\n");
#if DEBUG_POOLINFO
                    sob = new StringBuilder();
                    sob.AppendFormat("Pool {0} : train {1} ({2}) : no units available, enigne not created", PoolName, train.Number, train.Name);
                    File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                    return TrainFromPool.NotCreated;
                }
            }

            // Find required access path
            int firstSectionIndex = train.TCRoute.TCRouteSubpaths[0][0].TCSectionIndex;
            PoolDetails reqStorage = StoragePool[selectedStorage];

            int reqAccessPath = -1;
            for (int iPath = 0; iPath < reqStorage.AccessPaths.Count; iPath++)
            {
                Train.TCSubpathRoute thisPath = reqStorage.AccessPaths[iPath];
                if (thisPath.GetRouteIndex(firstSectionIndex, 0) >= 0)
                {
                    reqAccessPath = iPath;
                    break;
                }
            }

            // No valid path found
            if (reqAccessPath < 0)
            {
                Trace.TraceInformation("Train request : " + train.Name + " from pool " + PoolName + " : no valid access path found \n");
                return TrainFromPool.Failed;
            }

            // If valid path found: build new path from storage area
            train.TCRoute.AddSectionsAtStart(reqStorage.AccessPaths[reqAccessPath], train, false);
            train.TCRoute.AddSectionsAtStart(reqStorage.StoragePath, train, false);

            // Check all sections in route for engine heading for pool
            // If found, do not create engine as this may result in deadlock
            bool incomingEngine = false;
            foreach (Train.TCRouteElement thisElement in train.TCRoute.TCRouteSubpaths[0])
            {
                TrackCircuitSection thisSection = train.signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                // Check reserved
                if (thisSection.CircuitState.TrainReserved != null)
                {
                    TTTrain otherTTTrain = thisSection.CircuitState.TrainReserved.Train as TTTrain;
                    if (String.Equals(otherTTTrain.ExitPool, PoolName))
                    {
                        incomingEngine = true;
                        break;
                    }
                }

                // Check claimed
                if (thisSection.CircuitState.TrainClaimed.Count > 0)
                {
                    foreach (Train.TrainRouted otherTrain in thisSection.CircuitState.TrainClaimed)
                    {
                        TTTrain otherTTTrain = otherTrain.Train as TTTrain;
                        if (String.Equals(otherTTTrain.ExitPool, PoolName))
                        {
                            incomingEngine = true;
                            break;
                        }
                    }
                }
                if (incomingEngine) break;

                // Check occupied
                List<Train.TrainRouted> otherTrains = thisSection.CircuitState.TrainsOccupying();
                foreach (Train.TrainRouted otherTrain in otherTrains)
                {
                    TTTrain otherTTTrain = otherTrain.Train as TTTrain;
                    if (String.Equals(otherTTTrain.ExitPool, PoolName) && otherTTTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                    {
                        incomingEngine = true;
#if DEBUG_POOLINFO
                        sob = new StringBuilder();
                        sob.AppendFormat("Pool {0} : train {1} ({2}) waiting for incoming train {3} ({4})\n", PoolName, train.Number, train.Name, otherTTTrain.Number, otherTTTrain.Name);

                        int totalno1 = 0;
                        foreach (PoolDetails selStorage in StoragePool)
                        {
                            totalno1 += selStorage.StoredUnits.Count;
                        }
                        sob.AppendFormat("           stored units {0} : {1} (total {2})", PoolName, reqStorage.StoredUnits.Count, totalno1);
                        File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                        break;
                    }
                }
                if (incomingEngine) break;
            }

            // If incoming engine is approach, do not create train
            if (incomingEngine)
            {
#if DEBUG_TRACEINFO
                Trace.TraceInformation("Pool {0} : train {1} : delayed through incoming engine\n", PoolName, train.Name);
#endif
                return TrainFromPool.Delayed;
            }

            // Valid engine found - start train from found engine
            TTTrain selectedTrain = train.GetOtherTTTrainByNumber(selectedTrainNumber);
            if (selectedTrain == null)
            {
#if DEBUG_POOLINFO
                sob = new StringBuilder();

                sob.AppendFormat("Pool {1} : cannot find train {2} for {3} ({4}) \n", PoolName, selectedTrainNumber, train.Number, train.Name);

                int totalno2 = 0;
                foreach (PoolDetails selStorage in StoragePool)
                {
                    totalno2 += selStorage.StoredUnits.Count;
                }
                sob.AppendFormat("           stored units {0} : {1} (total {2})", PoolName, reqStorage.StoredUnits.Count, totalno2);
                File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                return TrainFromPool.Delayed;
            }

            TrackCircuitSection[] occupiedSections = new TrackCircuitSection[selectedTrain.OccupiedTrack.Count];
            selectedTrain.OccupiedTrack.CopyTo(occupiedSections);

            selectedTrain.Forms = -1;
            selectedTrain.RemoveTrain();
            train.FormedOfType = TTTrain.FormCommand.TerminationFormed;
            train.ValidRoute[0] = new Train.TCSubpathRoute(train.TCRoute.TCRouteSubpaths[0]);

#if DEBUG_POOLINFO
            sob = new StringBuilder();

            sob.AppendFormat("Pool {0} : train {1} ({2}) extracted as {3} ({4}) \n", PoolName, selectedTrain.Number, selectedTrain.Name, train.Number, train.Name);

            int totalno = 0;
            foreach (PoolDetails selStorage in StoragePool)
            {
                totalno += selStorage.StoredUnits.Count;
            }
            sob.AppendFormat("           stored units {0} : {1} (total {2})", PoolName, reqStorage.StoredUnits.Count, totalno);
            File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif

#if DEBUG_TRACEINFO
            Trace.TraceInformation("Pool : {0} : train {1} extracted as {2}", PoolName, selectedTrain.Name, train.Name);
#endif
            // Set details for new train from existing train
            bool validFormed = train.StartFromAITrain(selectedTrain, presentTime, occupiedSections);

            if (validFormed)
            {
                train.InitializeSignals(true);

                // Start new train
                if (train.AI.Simulator.StartReference.Contains(train.Number))
                {
                    train.AI.Simulator.StartReference.Remove(train.Number);
                }

                if (selectedTrain.TrainType == Train.TRAINTYPE.PLAYER)
                {
                    // Existing train is player, so continue as player
                    train.AI.TrainsToRemoveFromAI.Add(train);

                    // Set proper details for new formed train
                    train.OrgAINumber = train.Number;
                    train.Number = 0;
                    train.LeadLocomotiveIndex = selectedTrain.LeadLocomotiveIndex;
                    for (int carid = 0; carid < train.Cars.Count; carid++)
                    {
                        train.Cars[carid].CarID = selectedTrain.Cars[carid].CarID;
                    }
                    train.AI.TrainsToAdd.Add(train);
                    train.Simulator.Trains.Add(train);

                    train.SetFormedOccupied();
                    train.TrainType = Train.TRAINTYPE.PLAYER;
                    train.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                    train.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;

                    // IInform viewer about player train switch
                    train.Simulator.PlayerLocomotive = train.LeadLocomotive;
                    train.Simulator.OnPlayerLocomotiveChanged();

                    train.Simulator.OnPlayerTrainChanged(selectedTrain, train);
                    train.Simulator.PlayerLocomotive.Train = train;

                    train.SetupStationStopHandling();

                    // Clear replay commands
                    train.Simulator.Log.CommandList.Clear();

                    // Display messages
                    train.Simulator.Confirmer?.Information("Player switched to train : " + train.Name);
                }
                else if (train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    // New train is intended as player
                    train.TrainType = Train.TRAINTYPE.PLAYER;
                    train.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                    train.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                    train.AI.TrainsToAdd.Add(train);

                    // Set player locomotive
                    // First test first and last cars - if either is drivable, use it as player locomotive
                    int lastIndex = train.Cars.Count - 1;

                    if (train.Cars[0].IsDriveable)
                    {
                        train.AI.Simulator.PlayerLocomotive = train.LeadLocomotive = train.Cars[0];
                    }
                    else if (train.Cars[lastIndex].IsDriveable)
                    {
                        train.AI.Simulator.PlayerLocomotive = train.LeadLocomotive = train.Cars[lastIndex];
                    }
                    else
                    {
                        foreach (TrainCar car in train.Cars)
                        {
                            if (car.IsDriveable) // First loco is the one the player drives
                            {
                                train.AI.Simulator.PlayerLocomotive = train.LeadLocomotive = car;
                                break;
                            }
                        }
                    }

                    train.InitializeBrakes();

                    if (train.AI.Simulator.PlayerLocomotive == null)
                    {
                        throw new InvalidDataException("Can't find player locomotive in " + train.Name);
                    }
                    else
                    {
                        foreach (TrainCar car in train.Cars)
                        {
                            if (car.WagonType == TrainCar.WagonTypes.Engine)
                            {
                                MSTSLocomotive loco = car as MSTSLocomotive;
                                loco.AntiSlip = train.leadLocoAntiSlip;
                            }
                        }
                    }
                }
                else
                {
                    // Normal AI train
                    // Set delay
                    float randDelay = Simulator.Random.Next(train.DelayedStartSettings.newStart.randomPartS * 10);
                    train.RestdelayS = train.DelayedStartSettings.newStart.fixedPartS + (randDelay / 10f);
                    train.DelayedStart = true;
                    train.DelayedStartState = TTTrain.AI_START_MOVEMENT.NEW;

                    train.TrainType = Train.TRAINTYPE.AI;
                    train.AI.TrainsToAdd.Add(train);
                }

                train.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                train.SetFormedOccupied();

                // Update any outstanding required actions adding the added length
                train.ResetActions(true);

                // Set forced consist name if required
                if (!String.IsNullOrEmpty(train.ForcedConsistName))
                {
                    foreach (var car in train.Cars)
                    {
                        car.OrgConsist = String.Copy(train.ForcedConsistName);
                    }
                }
            }
            else
            {
#if DEBUG_TRACEINFO
                Trace.TraceWarning("Failed to extract required train " + train.Name + " from pool " + PoolName + "\n");
#endif
#if DEBUG_POOLINFO
                Trace.TraceWarning("Failed to extract required train " + train.Name + " from pool " + PoolName + "\n");
#endif
                return TrainFromPool.Failed;
            }

            // Update pool data
            reqStorage.StoredUnits.Remove(selectedTrainNumber);

#if DEBUG_TRACEINFO
            Trace.TraceInformation("Pool {0} : remaining units : {1}", PoolName, reqStorage.StoredUnits.Count);
            if (reqStorage.StoredUnits.Count > 0)
            {
                Trace.TraceInformation("Pool {0} : last stored unit : {1}", PoolName, reqStorage.StoredUnits.Last());
            }
#endif

            // Get last train in storage
            TTTrain storedTrain = null;

            if (reqStorage.StoredUnits.Count > 0)
            {
                int trainNumber = reqStorage.StoredUnits.Last();
                storedTrain = train.GetOtherTTTrainByNumber(trainNumber);

                if (storedTrain != null)
                {
                    reqStorage.RemLength = CalculateStorageLength(reqStorage, storedTrain);
                }
                else
                {
                    Trace.TraceWarning("Error in pool {0} : stored units : {1} : train no. {2} not found\n", PoolName, reqStorage.StoredUnits.Count, trainNumber);
                    reqStorage.StoredUnits.RemoveAt(reqStorage.StoredUnits.Count - 1);

                    trainNumber = reqStorage.StoredUnits.Last();
                    storedTrain = train.GetOtherTTTrainByNumber(trainNumber);

                    if (storedTrain != null)
                    {
                        reqStorage.RemLength = CalculateStorageLength(reqStorage, storedTrain);
                    }
                }
            }
            else
            {
                reqStorage.RemLength = reqStorage.StorageLength;
            }

            StoragePool[selectedStorage] = reqStorage;
            return TrainFromPool.Formed;
        }
    }
}
