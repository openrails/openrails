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
// #DEBUG_POOLINFO
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Parsers.OR;
using ORTS.Common;

namespace Orts.Simulation.Timetables
{
    /// <summary>
    /// class Poolholder
    /// Interface class for access from Simulator
    /// </summary>
    public class Poolholder
    {
        public Dictionary<string, TimetablePool> Pools;

        /// <summary>
        /// loader for timetable mode
        /// </summary>
        public Poolholder(Simulator simulatorref, string[] arguments, CancellationToken cancellation)
        {
            PoolInfo TTPool = new PoolInfo(simulatorref);
            Pools = TTPool.ProcessPools(arguments, cancellation);
        }

        //================================================================================================//
        /// <summary>
        /// loader for activity mode (dummy)
        /// </summary>
        public Poolholder()
        {
            Pools = null;
        }

        //================================================================================================//
        /// <summary>
        /// loader for restore
        /// </summary>
        public Poolholder(BinaryReader inf, Simulator simulatorref)
        {
            int nopools = inf.ReadInt32();
            if (nopools < 0)
            {
                Pools = null;
            }
            else
            {
                Pools = new Dictionary<string, TimetablePool>();
                for (int iPool = 0; iPool < nopools; iPool++)
                {
                    string newKey = inf.ReadString();
                    TimetablePool newPool = new TimetablePool(inf, simulatorref);
                    Pools.Add(newKey, newPool);
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
                    outf.Write(thisPool.Key);
                    thisPool.Value.Save(outf);
                }
            }
        }
    }

    //================================================================================================//
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

        public string PoolName = String.Empty;
        public bool ForceCreation;

        public struct PoolDetails
        {
            public Train.TCSubpathRoute StoragePath;          // path defined as storage location
            public Traveller StoragePathTraveller;            // traveller used to get path position and direction
            public string StorageName;                        // storage name
            public List<Train.TCSubpathRoute> AccessPaths;    // access paths defined for storage location
            public float StorageLength;                       // available length
            public float StorageCorrection;                   // length correction (e.g. due to switch overlap safety) - difference between length of sections in path and actual storage length

            public List<int> StoredUnits;                     // stored no. of units
            public float RemLength;                           // remaining storage length
        }

        public List<PoolDetails> StoragePool = new List<PoolDetails>();

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

            // loop through definitions
            while (lineindex < fileContents.Strings.Count && !newName)
            {
                string[] inputLine = fileContents.Strings[lineindex];

                // switch through definitions
                switch (inputLine[0].ToLower().Trim())
                {
                    // comment : do not process
                    case "#comment":
                        lineindex++;
                        break;

                    // name : set as name
                    case "#name":
                        newName = firstName;
                        if (!firstName)
                        {
                            lineindex++;
                            firstName = true;
                            PoolName = String.Copy(inputLine[1].ToLower().Trim());
                        }
                        break;

                    // storage : read path, add to path list
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
                            PoolDetails thisPool = ExtractStorage(fileContents, simulatorref, ref lineindex, out validStorage);
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

            // reset poolname if not valid
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
                PoolDetails newPool = new PoolDetails();
                newPool.StoragePath = new Train.TCSubpathRoute(inf);
                newPool.StoragePathTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes, inf);
                newPool.StorageName = inf.ReadString();

                newPool.AccessPaths = new List<Train.TCSubpathRoute>();
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

                newPool.StorageLength = inf.ReadSingle();
                newPool.StorageCorrection = inf.ReadSingle();
                newPool.RemLength = inf.ReadSingle();
                StoragePool.Add(newPool);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Method to save pool
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(PoolName);
            outf.Write(ForceCreation);

            outf.Write(StoragePool.Count);

            foreach (PoolDetails thisStorage in StoragePool)
            {
                thisStorage.StoragePath.Save(outf);
                thisStorage.StoragePathTraveller.Save(outf);
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

                outf.Write(thisStorage.StorageLength);
                outf.Write(thisStorage.StorageCorrection);
                outf.Write(thisStorage.RemLength);
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
        public PoolDetails ExtractStorage(TimetableReader fileContents, Simulator simulatorref, ref int lineindex, out bool validStorage)
        {
            PoolDetails newPool = new PoolDetails();
            List<string> accessPathNames = new List<string>();

            string[] inputLine = fileContents.Strings[lineindex];
            string storagePathName = String.Copy(inputLine[1]);

            lineindex++;
            inputLine = fileContents.Strings[lineindex];

            bool endOfStorage = false;
            validStorage = true;

            // extract access paths
            while (lineindex < fileContents.Strings.Count && !endOfStorage)
            {
                inputLine = fileContents.Strings[lineindex];
                switch (inputLine[0].ToLower().Trim())
                {
                    // skip comment
                    case "#comment":
                        lineindex++;
                        break;

                    // exit on next name
                    case "#name":
                        endOfStorage = true;
                        break;

                    // storage : next storage area
                    case "#storage":
                        endOfStorage = true;
                        break;

                    // access paths : process
                    case "#access":
                        int nextfield = 1;

                        while (nextfield < inputLine.Length && !String.IsNullOrEmpty(inputLine[nextfield]))
                        {
                            accessPathNames.Add(String.Copy(inputLine[nextfield]));
                            nextfield++;
                        }

                        lineindex++;
                        break;

                    // settings : check setting
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
                                nextfield++;
                            }
                        }
                        lineindex++;
                        break;

                    default:
                        Trace.TraceInformation("Pool : " + fileContents.FilePath + " : line : " + lineindex + " : unknown definition : " + inputLine[0] + " ; line ignored \n");
                        lineindex++;
                        break;
                }
            }

            // check if access paths defined
            if (accessPathNames.Count <= 0)
            {
                Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage : " + storagePathName + " : no access paths defined \n");
                validStorage = false;
                return (newPool);
            }

            // process storage paths
            newPool.AccessPaths = new List<Train.TCSubpathRoute>();
            newPool.StoredUnits = new List<int>();
            newPool.StorageLength = 0.0f;
            newPool.RemLength = 0.0f;

            bool pathValid = true;
            TimetableInfo TTInfo = new TimetableInfo(simulatorref);
            AIPath newPath = TTInfo.LoadPath(storagePathName, out pathValid);

            if (pathValid)
            {
                Train.TCRoutePath fullRoute = new Train.TCRoutePath(newPath, -2, 1, simulatorref.Signals, -1, simulatorref.Settings);

                newPool.StoragePath = new Train.TCSubpathRoute(fullRoute.TCRouteSubpaths[0]);
                newPool.StoragePathTraveller = new Traveller(simulatorref.TSectionDat, simulatorref.TDB.TrackDB.TrackNodes, newPath);
                newPool.StorageName = String.Copy(storagePathName);

                // if last element is end of track, remove it from path
                int lastSectionIndex = newPool.StoragePath[newPool.StoragePath.Count - 1].TCSectionIndex;
                if (simulatorref.Signals.TrackCircuitList[lastSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                {
                    newPool.StoragePath.RemoveAt(newPool.StoragePath.Count - 1);
                }

                // check for multiple subpaths - not allowed for storage area
                if (fullRoute.TCRouteSubpaths.Count > 1)
                {
                    Trace.TraceInformation("Pool : " + fileContents.FilePath + " : storage area : " + storagePathName + " : storage path may not contain multiple subpaths\n");
                }
            }
            else
            {
                Trace.TraceWarning("Pool : " + fileContents.FilePath + " : error while processing storege area path : " + storagePathName + "\n");
                validStorage = false;
                return (newPool);
            }

            // process access paths
            foreach (string accessPath in accessPathNames)
            {
                pathValid = true;
                newPath = TTInfo.LoadPath(accessPath, out pathValid);

                if (pathValid)
                {
                    Train.TCRoutePath fullRoute = new Train.TCRoutePath(newPath, -2, 1, simulatorref.Signals, -1, simulatorref.Settings);
                    // if last element is end of track, remove it from path
                    Train.TCSubpathRoute usedRoute = fullRoute.TCRouteSubpaths[0];
                    int lastIndex = usedRoute.Count - 1;
                    int lastSectionIndex = usedRoute[lastIndex].TCSectionIndex;
                    if (simulatorref.Signals.TrackCircuitList[lastSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                    {
                        lastIndex = usedRoute.Count - 2;
                    }
                    newPool.AccessPaths.Add(new Train.TCSubpathRoute(usedRoute, 0, lastIndex));

                    // check for multiple subpaths - not allowed for storage area
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

            // verify proper access route definition

            if (!validStorage)
            {
                return (newPool);
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
                    // check storage path direction, reverse path if required
                    // path may be in wrong direction due to path conversion problems
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

                    // remove elements from access path which are part of storage path
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

            // calculate storage length
            if (!validStorage)
            {
                return (newPool);
            }
            float storeLength = 0;
            foreach (Train.TCRouteElement thisElement in newPool.StoragePath)
            {
                storeLength += simulatorref.Signals.TrackCircuitList[thisElement.TCSectionIndex].Length;
            }

            // if storage ends at switch, deduct switch safety distance

            float addedLength = 0;

            foreach (Train.TCRouteElement thisElement in newPool.AccessPaths[0])
            {
                TrackCircuitSection thisSection = simulatorref.Signals.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                {
                    addedLength -= (float)thisSection.Overlap;
                    break;
                }
                else
                {
                    // count length only if not part of storage path itself
                    if (newPool.StoragePath.GetRouteIndex(thisSection.Index, 0) < 0)
                    {
                        addedLength += thisSection.Length;
                    }
                }
            }

            // if switch overlap exceeds distance between end of storage and switch, deduct from storage length
            if (addedLength < 0)
            {
                storeLength += addedLength;
            }

            newPool.StorageLength = storeLength;
            newPool.StorageCorrection = addedLength;
            newPool.RemLength = storeLength;

            return (newPool);
        }

        //================================================================================================//
        /// <summary>
        /// TestPoolExit : test if end of route is access to required pool
        /// </summary>
        /// <param name="train"></param>
        public bool TestPoolExit(TTTrain train)
        {

            bool validPool = false;

            // set dispose states
            train.FormsStatic = true;
            train.Closeup = true;

            // find relevant access path
            int lastSectionIndex = train.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
            int lastSectionDirection = train.TCRoute.TCRouteSubpaths.Last().Last().Direction;

            // use first storage path to get pool access path

            PoolDetails thisStorage = StoragePool[0];
            int reqPath = -1;
            int reqPathIndex = -1;

            // find relevant access path
            for (int iPath = 0; iPath < thisStorage.AccessPaths.Count && reqPath < 0; iPath++)
            {
                Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[iPath];
                reqPathIndex = accessPath.GetRouteIndex(lastSectionIndex, 0);

                // path is defined outbound, so directions must be opposite
                if (reqPathIndex >= 0 && accessPath[reqPathIndex].Direction != lastSectionDirection)
                {
                    reqPath = iPath;
                }
            }

            // none found
            if (reqPath < 0)
            {
                Trace.TraceWarning("Train : " + train.Name + " : no valid path found to access pool storage " + PoolName + "\n");
                train.FormsStatic = false;
                train.Closeup = false;
            }
            // path found : extend train path with access and storage paths
            else
            {
                train.PoolAccessSection = lastSectionIndex;
                validPool = true;
            }

            return (validPool);
        }

        //================================================================================================//
        /// <summary>
        /// SetPoolExit : adjust train dispose details and path to required pool exit
        /// </summary>
        /// <param name="train"></param>
        public Train.TCSubpathRoute SetPoolExit(TTTrain train, out int poolStorageIndex, bool checkAccessPath)
        {
            // new route
            Train.TCSubpathRoute newRoute = null;
            poolStorageIndex = -1;

            // set dispose states
            train.FormsStatic = true;
            train.Closeup = true;

            // find relevant access path
            int lastSectionIndex = train.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
            int lastSectionDirection = train.TCRoute.TCRouteSubpaths.Last().Last().Direction;

            // find storage path with enough space to store train

            int reqPool = -1;
            for (int iPool = 0; iPool < StoragePool.Count && reqPool < 0; iPool++)
            {
                PoolDetails thisStorage = StoragePool[iPool];

                if (thisStorage.StoredUnits.Contains(train.Number))
                {
#if DEBUG_POOLINFO
                    var sob = new StringBuilder();
                    sob.AppendFormat("Pool {0} : error : train {1} ({2}) allready stored in pool \n", PoolName, train.Number, train.Name);
                    sob.AppendFormat("           stored units : {0}", thisStorage.StoredUnits.Count);
                    File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                }
                else if (thisStorage.RemLength > train.Length)
                {
                    reqPool = iPool;
                    poolStorageIndex = iPool;
                }
            }

            // no storage space found : pool overflow
            if (reqPool < 0)
            {
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

                // train will be abandoned when reaching end of path
                train.FormsStatic = false;
                train.Closeup = false;
            }
            else
            {
                PoolDetails thisStorage = StoragePool[reqPool];

                if (checkAccessPath)
                {
                    int reqPath = -1;
                    int reqPathIndex = -1;

                    // find relevant access path
                    for (int iPath = 0; iPath < thisStorage.AccessPaths.Count && reqPath < 0; iPath++)
                    {
                        Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[iPath];
                        reqPathIndex = accessPath.GetRouteIndex(lastSectionIndex, 0);

                        // path is defined outbound, so directions must be opposite
                        if (reqPathIndex >= 0 && accessPath[reqPathIndex].Direction != lastSectionDirection)
                        {
                            reqPath = iPath;
                        }
                    }

                    // none found
                    if (reqPath < 0)
                    {
                        Trace.TraceWarning("Train : " + train.Name + " : no valid path found to access pool storage " + PoolName + "\n");
                        train.FormsStatic = false;
                        train.Closeup = false;
                        poolStorageIndex = -1;
                    }
                    // path found : extend train path with access and storage paths
                    else
                    {
                        Train.TCSubpathRoute accessPath = thisStorage.AccessPaths[reqPath];
                        newRoute = new Train.TCSubpathRoute(train.TCRoute.TCRouteSubpaths.Last());

                        // add elements from access route except those allready on the path
                        // add in reverse order and reverse direction as path is defined outbound
                        for (int iElement = reqPathIndex; iElement >= 0; iElement--)
                        {
                            if (newRoute.GetRouteIndex(accessPath[iElement].TCSectionIndex, 0) < 0)
                            {
                                Train.TCRouteElement newElement = new Train.TCRouteElement(accessPath[iElement]);
                                newElement.Direction = newElement.Direction == 1 ? 0 : 1;
                                newRoute.Add(newElement);
                            }
                        }
                        // add elements from storage
                        for (int iElement = thisStorage.StoragePath.Count - 1; iElement >= 0; iElement--)
                        {
                            if (newRoute.GetRouteIndex(thisStorage.StoragePath[iElement].TCSectionIndex, 0) < 0)
                            {
                                Train.TCRouteElement newElement = new Train.TCRouteElement(thisStorage.StoragePath[iElement]);
                                newElement.Direction = newElement.Direction == 1 ? 0 : 1;
                                newRoute.Add(newElement);
                            }
                        }
                    }
                }
                // create new route from storage and access track only
                else
                {
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

            return (newRoute);
        }

        //================================================================================================//
        /// <summary>
        /// AddUnit : add unit to pool, update remaining length
        /// </summary>
        /// <param name="train"></param>
        public void AddUnit(TTTrain train)
        {
            // check if unit allready in pool (error occurs during prerun)
            // foreach (TimetablePool thisPool in )
            // add train to pool
            PoolDetails thisPool = StoragePool[train.PoolIndex];
            thisPool.StoredUnits.Add(train.Number);

            thisPool.RemLength = CalculateStorageLength(thisPool, train);
            StoragePool[train.PoolIndex] = thisPool;

#if DEBUG_POOLINFO
            var sob = new StringBuilder();
            sob.AppendFormat("Pool {0} : train {1} ({2}) added\n", PoolName, train.Number, train.Name);
            sob.AppendFormat("           stored units : {0}\n", thisPool.StoredUnits.Count);
            File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif

#if DEBUG_TRACEINFO
            Trace.TraceInformation("Pool {0} : added unit : {1} ; stored units : {2} ( last = {3} )", PoolName, train.Name, thisPool.StoredUnits.Count, thisPool.StoredUnits.Last());
#endif

            // clear track behind engine, only keep actual occupied sections
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
            // no trains in storage
            if (reqStorage.StoredUnits.Count <= 0)
            {
                return (reqStorage.StorageLength);
            }

            // calculate remaining length
            int occSectionIndex = train.PresentPosition[0].TCSectionIndex;
            int occSectionDirection = train.PresentPosition[0].TCDirection;
            int storageSectionIndex = reqStorage.StoragePath.GetRouteIndex(occSectionIndex, 0);

            // if train not stopped in pool, return remaining length = 0
            if (storageSectionIndex < 0)
            {
                return (0);
            }

            int storageSectionDirection = reqStorage.StoragePath[storageSectionIndex].Direction;
            // if directions of paths are equal, use front section, section.length - position.offset, and use front of train position

            float remLength = 0;

            // same direction : use rear of train position
            if (occSectionDirection == storageSectionDirection)
            {
                occSectionIndex = train.PresentPosition[1].TCSectionIndex;
                TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                remLength = occSection.Length - train.PresentPosition[1].TCOffset;
            }
            else
            // opposite direction : use front of train position
            {
                TrackCircuitSection occSection = train.signalRef.TrackCircuitList[occSectionIndex];
                remLength = train.PresentPosition[0].TCOffset;
            }

            for (int iSection = reqStorage.StoragePath.Count - 1; iSection >= 0 && reqStorage.StoragePath[iSection].TCSectionIndex != occSectionIndex; iSection--)
            {
                remLength += train.signalRef.TrackCircuitList[reqStorage.StoragePath[iSection].TCSectionIndex].Length;
            }

            // position was furthest down the storage area, so take off train length
            remLength -= train.Length;

            // correct for overlap etc.
            remLength += reqStorage.StorageCorrection;  // storage correction is negative!

            return (remLength);
        }

        //================================================================================================//
        /// <summary>
        /// Extract train from pool
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        public TrainFromPool ExtractTrain(ref TTTrain train, int presentTime)
        {
#if DEBUG_POOLINFO
            var sob = new StringBuilder();
            sob.AppendFormat("Pool {0} : request for train {1} ({2})", PoolName, train.Number, train.Name);
            File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
            // check if any engines available
            int selectedTrainNumber = -1;
            int selectedStorage = -1;

            for (int iStorage = 0; iStorage < StoragePool.Count; iStorage++)
            {
                PoolDetails thisStorage = StoragePool[iStorage];
                if (thisStorage.StoredUnits.Count > 0)
                {
                    selectedTrainNumber = thisStorage.StoredUnits[thisStorage.StoredUnits.Count - 1];
                    selectedStorage = iStorage;
                    break;
                }
            }

            // pool underflow : create engine from scratch
            if (selectedTrainNumber < 0)
            {
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
                    return (TrainFromPool.ForceCreated);
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
                    return (TrainFromPool.NotCreated);
                }
            }

            // find required access path
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

            // no valid path found
            if (reqAccessPath < 0)
            {
                Trace.TraceInformation("Train request : " + train.Name + " from pool " + PoolName + " : no valid access path found \n");
                return (TrainFromPool.Failed);
            }

            // if valid path found : build new path from storage area

            Train.TCSubpathRoute newRoute = new Train.TCSubpathRoute(train.TCRoute.TCRouteSubpaths[0]);

            int addedSections = 0;

            // add sections from access path at front
            // add in reverse order as path is defined outbound
            for (int iSection = reqStorage.AccessPaths[reqAccessPath].Count - 1; iSection >= 0; iSection--)
            {
                Train.TCRouteElement thisElement = reqStorage.AccessPaths[reqAccessPath][iSection];
                if (newRoute.GetRouteIndex(thisElement.TCSectionIndex, 0) < 0)
                {
                    Train.TCRouteElement newElement = new Train.TCRouteElement(thisElement);
                    newRoute.Insert(0, newElement);
                    addedSections++;
                }
            }

            // add sections from storage path
            // add in reverse order as path is defined outbound
            for (int iSection = reqStorage.StoragePath.Count - 1; iSection >= 0; iSection--)
            {
                Train.TCRouteElement thisElement = reqStorage.StoragePath[iSection];
                if (newRoute.GetRouteIndex(thisElement.TCSectionIndex, 0) < 0)
                {
                    Train.TCRouteElement newElement = new Train.TCRouteElement(thisElement);
                    newRoute.Insert(0, newElement);
                    addedSections++;
                }
            }

            // add number of added sections to reversal info
            if (train.TCRoute.ReversalInfo[0].Valid)
            {
                train.TCRoute.ReversalInfo[0].FirstDivergeIndex += addedSections;
                train.TCRoute.ReversalInfo[0].FirstSignalIndex += addedSections;
                train.TCRoute.ReversalInfo[0].LastDivergeIndex += addedSections;
                train.TCRoute.ReversalInfo[0].LastSignalIndex += addedSections;
            }

            // add number of sections to station stops
            if (train.StationStops != null && train.StationStops.Count > 0)
            {
                for (int iStop = 0; iStop < train.StationStops.Count; iStop++)
                {
                    Train.StationStop thisStop = train.StationStops[iStop];
                    if (thisStop.SubrouteIndex == 0)
                    {
                        thisStop.RouteIndex += addedSections;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // check all sections in route for engine heading for pool
            // if found, do not create engine as this may result in deadlock

            bool incomingEngine = false;
            foreach (Train.TCRouteElement thisElement in newRoute)
            {
                TrackCircuitSection thisSection = train.signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                // check reserved
                if (thisSection.CircuitState.TrainReserved != null)
                {
                    TTTrain otherTTTrain = thisSection.CircuitState.TrainReserved.Train as TTTrain;
                    if (String.Equals(otherTTTrain.ExitPool, PoolName))
                    {
                        incomingEngine = true;
                        break;
                    }
                }

                // check claimed
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

                // check occupied
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
                        sob.AppendFormat("           stored units : {0}", reqStorage.StoredUnits.Count);
                        File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                        break;
                    }
                }
                if (incomingEngine) break;
            }

            // if incoming engine is approach, do not create train
            if (incomingEngine)
            {
#if DEBUG_TRACEINFO
                Trace.TraceInformation("Pool {0} : train {1} : delayed through incoming engine\n", PoolName, train.Name);
#endif
                return (TrainFromPool.Delayed);
            }

            // valid engine found - start train from found engine

            TTTrain selectedTrain = train.GetOtherTTTrainByNumber(selectedTrainNumber);
            if (selectedTrain == null)
            {
#if DEBUG_POOLINFO
                sob = new StringBuilder();
                sob.AppendFormat("Pool {0} : cannot find train {1} for {2} ({3}) \n", PoolName, selectedTrainNumber, train.Number, train.Name);
                sob.AppendFormat("           stored units : {0}", reqStorage.StoredUnits.Count);
                File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif
                return (TrainFromPool.Delayed);
            }

            TrackCircuitSection[] occupiedSections = new TrackCircuitSection[selectedTrain.OccupiedTrack.Count];
            selectedTrain.OccupiedTrack.CopyTo(occupiedSections);

            selectedTrain.Forms = -1;
            selectedTrain.RemoveTrain();
            train.FormedOfType = TTTrain.FormCommand.TerminationFormed;
            train.TCRoute.TCRouteSubpaths[0] = new Train.TCSubpathRoute(newRoute);
            train.ValidRoute[0] = new Train.TCSubpathRoute(newRoute);

#if DEBUG_POOLINFO
            sob = new StringBuilder();
            sob.AppendFormat("Pool {0} : train {1} ({2}) extracted as {3} ({4}) \n", PoolName, selectedTrain.Number, selectedTrain.Name, train.Number, train.Name);
            sob.AppendFormat("           stored units : {0}", reqStorage.StoredUnits.Count);
            File.AppendAllText(@"C:\temp\PoolAnal.csv", sob.ToString() + "\n");
#endif

#if DEBUG_TRACEINFO
            Trace.TraceInformation("Pool : {0} : train {1} extracted as {2}", PoolName, selectedTrain.Name, train.Name);
#endif
            // set details for new train from existing train
            bool validFormed = train.StartFromAITrain(selectedTrain, presentTime, occupiedSections);

            if (validFormed)
            {
                train.InitializeSignals(true);

                // start new train
                if (train.AI.Simulator.StartReference.Contains(train.Number))
                {
                    train.AI.Simulator.StartReference.Remove(train.Number);
                }

                // existing train is player, so continue as player
                if (selectedTrain.TrainType == Train.TRAINTYPE.PLAYER)
                {
                    train.AI.TrainsToRemoveFromAI.Add(train);

                    // set proper details for new formed train
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

                    // inform viewer about player train switch
                    train.Simulator.PlayerLocomotive = train.LeadLocomotive;
                    train.Simulator.OnPlayerLocomotiveChanged();

                    train.Simulator.OnPlayerTrainChanged(selectedTrain, train);
                    train.Simulator.PlayerLocomotive.Train = train;

                    train.SetupStationStopHandling();

                    // clear replay commands
                    train.Simulator.Log.CommandList.Clear();

                    // display messages
                    if (train.Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        train.Simulator.Confirmer.Information("Player switched to train : " + train.Name);
                }

                // new train is intended as player
                else if (train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    train.TrainType = Train.TRAINTYPE.PLAYER;
                    train.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                    train.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;

                    train.AI.TrainsToAdd.Add(train);

                    // set player locomotive
                    // first test first and last cars - if either is drivable, use it as player locomotive
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
                            if (car.IsDriveable)  // first loco is the one the player drives
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

                // normal AI train
                else
                {
                    // set delay
                    float randDelay = (float)Simulator.Random.Next((train.DelayedStartSettings.newStart.randomPartS * 10));
                    train.RestdelayS = train.DelayedStartSettings.newStart.fixedPartS + (randDelay / 10f);
                    train.DelayedStart = true;
                    train.DelayedStartState = TTTrain.AI_START_MOVEMENT.NEW;

                    train.TrainType = Train.TRAINTYPE.AI;
                    train.AI.TrainsToAdd.Add(train);
                }

                train.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                train.SetFormedOccupied();

                // update any outstanding required actions adding the added length
                train.ResetActions(true);

                // set forced consist name if required
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
                return (TrainFromPool.Failed);
            }

            // update pool data
            reqStorage.StoredUnits.Remove(selectedTrainNumber);

#if DEBUG_TRACEINFO
            Trace.TraceInformation("Pool {0} : remaining units : {1}", PoolName, reqStorage.StoredUnits.Count);
            if (reqStorage.StoredUnits.Count > 0)
            {
                Trace.TraceInformation("Pool {0} : last stored unit : {1}", PoolName, reqStorage.StoredUnits.Last());
            }
#endif

            // get last train in storage
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
            return (TrainFromPool.Formed);
        }
    }
}
