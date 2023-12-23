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
// #define DEBUG_DEADLOCK
// print details of deadlock processing

using Orts.Simulation.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Simulation.Signalling
{
    public class DeadlockInfo
    {
        public enum DeadlockTrainState                                    // state of train wrt this deadlock                     
        {
            KeepClearThisDirection,
            KeepClearReverseDirection,
            Approaching,
            StoppedAheadLoop,
            InLoop,
            StoppedInLoop,
        }

        protected Signals signalRef;                                       // reference to overlaying Signals class

        public int DeadlockIndex;                                          // this deadlock unique index reference
        public List<DeadlockPathInfo> AvailablePathList;                   // list of available paths
        public Dictionary<int, List<int>> PathReferences;                  // list of paths per boundary section
        public Dictionary<int, List<int>> TrainReferences;                 // list of paths as allowed per train/subpath index
        public Dictionary<int, Dictionary<int, bool>> TrainLengthFit;      // list of length fit per train/subpath and per path
        public Dictionary<int, int> TrainOwnPath;                          // train's own path per train/subpath
        public Dictionary<int, int> InverseInfo;                           // list of paths which are each others inverse
        public Dictionary<int, Dictionary<int, int>> TrainSubpathIndex;    // unique index per train and subpath
        private int nextTrainSubpathIndex;                                 // counter for train/subpath index

        /// <summary>
        /// Constructor for emtpy struct to gain access to methods
        /// </summary>
        public DeadlockInfo(Signals signalReference)
        {
            signalRef = signalReference;
        }

        public DeadlockInfo(Signals signalReference, TrackCircuitSection startSection, TrackCircuitSection endSection)
        {
            signalRef = signalReference;

            DeadlockIndex = signalRef.deadlockIndex++;

            AvailablePathList = new List<DeadlockPathInfo>();
            PathReferences = new Dictionary<int, List<int>>();
            TrainReferences = new Dictionary<int, List<int>>();
            TrainLengthFit = new Dictionary<int, Dictionary<int, bool>>();
            TrainOwnPath = new Dictionary<int, int>();
            InverseInfo = new Dictionary<int, int>();
            TrainSubpathIndex = new Dictionary<int, Dictionary<int, int>>();
            nextTrainSubpathIndex = 0;

            signalRef.DeadlockInfoList.Add(DeadlockIndex, this);
        }

        /// <summary>
        /// Constructor for restore
        /// </summary>
        public DeadlockInfo(Signals signalReference, BinaryReader inf)
        {
            signalRef = signalReference;

            DeadlockIndex = inf.ReadInt32();
            AvailablePathList = new List<DeadlockPathInfo>();

            int totalPaths = inf.ReadInt32();
            for (int iPath = 0; iPath <= totalPaths - 1; iPath++)
            {
                DeadlockPathInfo thisPath = new DeadlockPathInfo(inf);
                AvailablePathList.Add(thisPath);
            }

            PathReferences = new Dictionary<int, List<int>>();

            int totalReferences = inf.ReadInt32();
            for (int iReference = 0; iReference <= totalReferences - 1; iReference++)
            {
                int thisReference = inf.ReadInt32();
                List<int> thisList = new List<int>();

                int totalItems = inf.ReadInt32();
                for (int iItem = 0; iItem <= totalItems - 1; iItem++)
                {
                    int thisItem = inf.ReadInt32();
                    thisList.Add(thisItem);
                }
                PathReferences.Add(thisReference, thisList);
            }

            TrainReferences = new Dictionary<int, List<int>>();

            totalReferences = inf.ReadInt32();
            for (int iReference = 0; iReference <= totalReferences - 1; iReference++)
            {
                int thisReference = inf.ReadInt32();
                List<int> thisList = new List<int>();

                int totalItems = inf.ReadInt32();
                for (int iItem = 0; iItem <= totalItems - 1; iItem++)
                {
                    int thisItem = inf.ReadInt32();
                    thisList.Add(thisItem);
                }
                TrainReferences.Add(thisReference, thisList);
            }

            TrainLengthFit = new Dictionary<int, Dictionary<int, bool>>();

            int totalFits = inf.ReadInt32();
            for (int iFits = 0; iFits <= totalFits - 1; iFits++)
            {
                int thisTrain = inf.ReadInt32();
                Dictionary<int, bool> thisLengthFit = new Dictionary<int, bool>();

                int totalItems = inf.ReadInt32();
                for (int iItem = 0; iItem <= totalItems - 1; iItem++)
                {
                    int itemRef = inf.ReadInt32();
                    bool itemValue = inf.ReadBoolean();

                    thisLengthFit.Add(itemRef, itemValue);
                }
                TrainLengthFit.Add(thisTrain, thisLengthFit);
            }

            TrainOwnPath = new Dictionary<int, int>();

            int totalOwnPath = inf.ReadInt32();
            for (int iOwnPath = 0; iOwnPath <= totalOwnPath - 1; iOwnPath++)
            {
                int trainIndex = inf.ReadInt32();
                int pathIndex = inf.ReadInt32();
                TrainOwnPath.Add(trainIndex, pathIndex);
            }

            InverseInfo = new Dictionary<int, int>();
            int totalInverseInfo = inf.ReadInt32();

            for (int iInfo = 0; iInfo <= totalInverseInfo - 1; iInfo++)
            {
                int infoKey = inf.ReadInt32();
                int infoValue = inf.ReadInt32();
                InverseInfo.Add(infoKey, infoValue);
            }

            TrainSubpathIndex = new Dictionary<int, Dictionary<int, int>>();
            int totalTrain = inf.ReadInt32();

            for (int iTrain = 0; iTrain <= totalTrain - 1; iTrain++)
            {
                int trainValue = inf.ReadInt32();
                Dictionary<int, int> subpathList = new Dictionary<int, int>();

                int totalSubpaths = inf.ReadInt32();
                for (int iSubpath = 0; iSubpath <= totalSubpaths - 1; iSubpath++)
                {
                    int subpathValue = inf.ReadInt32();
                    int indexValue = inf.ReadInt32();
                    subpathList.Add(subpathValue, indexValue);
                }
                TrainSubpathIndex.Add(trainValue, subpathList);
            }

            nextTrainSubpathIndex = inf.ReadInt32();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(DeadlockIndex);
            outf.Write(AvailablePathList.Count);

            foreach (DeadlockPathInfo thisPathInfo in AvailablePathList)
            {
                thisPathInfo.Save(outf);
            }

            outf.Write(PathReferences.Count);
            foreach (KeyValuePair<int, List<int>> thisReference in PathReferences)
            {
                outf.Write(thisReference.Key);
                outf.Write(thisReference.Value.Count);

                foreach (int thisRefValue in thisReference.Value)
                {
                    outf.Write(thisRefValue);
                }
            }

            outf.Write(TrainReferences.Count);
            foreach (KeyValuePair<int, List<int>> thisReference in TrainReferences)
            {
                outf.Write(thisReference.Key);
                outf.Write(thisReference.Value.Count);

                foreach (int thisRefValue in thisReference.Value)
                {
                    outf.Write(thisRefValue);
                }
            }

            outf.Write(TrainLengthFit.Count);
            foreach (KeyValuePair<int, Dictionary<int, bool>> thisLengthFit in TrainLengthFit)
            {
                outf.Write(thisLengthFit.Key);
                outf.Write(thisLengthFit.Value.Count);

                foreach (KeyValuePair<int, bool> thisAvailValue in thisLengthFit.Value)
                {
                    outf.Write(thisAvailValue.Key);
                    outf.Write(thisAvailValue.Value);
                }
            }

            outf.Write(TrainOwnPath.Count);
            foreach (KeyValuePair<int, int> ownTrainInfo in TrainOwnPath)
            {
                outf.Write(ownTrainInfo.Key);
                outf.Write(ownTrainInfo.Value);
            }

            outf.Write(InverseInfo.Count);
            foreach (KeyValuePair<int, int> thisInfo in InverseInfo)
            {
                outf.Write(thisInfo.Key);
                outf.Write(thisInfo.Value);
            }

            outf.Write(TrainSubpathIndex.Count);
            foreach (KeyValuePair<int, Dictionary<int, int>> trainInfo in TrainSubpathIndex)
            {
                outf.Write(trainInfo.Key);
                outf.Write(trainInfo.Value.Count);

                foreach (KeyValuePair<int, int> subpathInfo in trainInfo.Value)
                {
                    outf.Write(subpathInfo.Key);
                    outf.Write(subpathInfo.Value);
                }
            }

            outf.Write(nextTrainSubpathIndex);
        }

        /// <summary>
        /// Create deadlock info from alternative path or find related info
        /// </summary>
        public DeadlockInfo FindDeadlockInfo(ref Train.TCSubpathRoute partPath, Train.TCSubpathRoute mainPath, int startSectionIndex, int endSectionIndex)
        {
            TrackCircuitSection startSection = signalRef.TrackCircuitList[startSectionIndex];
            TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

            int usedStartSectionRouteIndex = mainPath.GetRouteIndex(startSectionIndex, 0);
            int usedEndSectionRouteIndex = mainPath.GetRouteIndex(endSectionIndex, usedStartSectionRouteIndex);

            // check if there is a deadlock info defined with these as boundaries
            int startSectionDLReference = startSection.DeadlockReference;
            int endSectionDLReference = endSection.DeadlockReference;

            DeadlockInfo newDeadlockInfo = null;

            // if either end is within a deadlock, try if end of deadlock matches train path
            if (startSection.DeadlockBoundaries != null && startSection.DeadlockBoundaries.Count > 0)
            {
                int newStartSectionRouteIndex = -1;
                foreach (KeyValuePair<int, int> startSectionInfo in startSection.DeadlockBoundaries)
                {
                    DeadlockInfo existDeadlockInfo = signalRef.DeadlockInfoList[startSectionInfo.Key];
                    Train.TCSubpathRoute existPath = existDeadlockInfo.AvailablePathList[startSectionInfo.Value].Path;
                    newStartSectionRouteIndex = mainPath.GetRouteIndexBackward(existPath[0].TCSectionIndex, usedStartSectionRouteIndex);
                    if (newStartSectionRouteIndex < 0) // may be wrong direction - try end section
                    {
                        newStartSectionRouteIndex =
                            mainPath.GetRouteIndexBackward(existDeadlockInfo.AvailablePathList[startSectionInfo.Value].EndSectionIndex, usedStartSectionRouteIndex);
                    }

                    if (newStartSectionRouteIndex >= 0)
                    {
                        newDeadlockInfo = existDeadlockInfo;
                        break; // match found, stop searching
                    }
                }

                // no match found - train path is not on existing deadlock - do not accept
                if (newStartSectionRouteIndex < 0)
                {
                    return (null);
                }
                else
                {
                    // add sections to start of temp path
                    for (int iIndex = usedStartSectionRouteIndex - 1; iIndex >= newStartSectionRouteIndex; iIndex--)
                    {
                        Train.TCRouteElement newElement = mainPath[iIndex];
                        partPath.Insert(0, newElement);
                    }
                }
            }

            if (endSection.DeadlockBoundaries != null && endSection.DeadlockBoundaries.Count > 0)
            {
                int newEndSectionRouteIndex = -1;
                foreach (KeyValuePair<int, int> endSectionInfo in endSection.DeadlockBoundaries)
                {
                    DeadlockInfo existDeadlockInfo = signalRef.DeadlockInfoList[endSectionInfo.Key];
                    Train.TCSubpathRoute existPath = existDeadlockInfo.AvailablePathList[endSectionInfo.Value].Path;
                    newEndSectionRouteIndex = mainPath.GetRouteIndex(existPath[0].TCSectionIndex, usedEndSectionRouteIndex);
                    if (newEndSectionRouteIndex < 0) // may be wrong direction - try end section
                    {
                        newEndSectionRouteIndex =
                            mainPath.GetRouteIndex(existDeadlockInfo.AvailablePathList[endSectionInfo.Value].EndSectionIndex, usedEndSectionRouteIndex);
                    }

                    if (newEndSectionRouteIndex >= 0)
                    {
                        newDeadlockInfo = existDeadlockInfo;
                        break; // match found, stop searching
                    }
                }

                // no match found - train path is not on existing deadlock - do not accept
                if (newEndSectionRouteIndex < 0)
                {
                    return (null);
                }
                else
                {
                    // add sections to end of temp path
                    for (int iIndex = usedEndSectionRouteIndex + 1; iIndex <= newEndSectionRouteIndex; iIndex++)
                    {
                        Train.TCRouteElement newElement = mainPath[iIndex];
                        partPath.Add(newElement);
                    }
                }
            }

            // if no deadlock yet found
            if (newDeadlockInfo == null)
            {
                // if both references are equal, use existing information
                if (startSectionDLReference > 0 && startSectionDLReference == endSectionDLReference)
                {
                    newDeadlockInfo = signalRef.DeadlockInfoList[startSectionDLReference];
                }
                // if both references are null, check for existing references along route
                else if (startSectionDLReference < 0 && endSectionDLReference < 0)
                {
                    if (CheckNoOverlapDeadlockPaths(partPath, signalRef))
                    {
                        newDeadlockInfo = new DeadlockInfo(signalRef, startSection, endSection);
                        signalRef.DeadlockReference.Add(startSectionIndex, newDeadlockInfo.DeadlockIndex);
                        signalRef.DeadlockReference.Add(endSectionIndex, newDeadlockInfo.DeadlockIndex);

                        startSection.DeadlockReference = newDeadlockInfo.DeadlockIndex;
                        endSection.DeadlockReference = newDeadlockInfo.DeadlockIndex;
                    }
                    // else : overlaps existing deadlocks - will sort that out later //TODO DEADLOCK
                }
            }

            return (newDeadlockInfo);
        }

        /// <summary>
        /// add unnamed path to deadlock info
        /// return : [0] index to path
        ///          [1] > 0 : existing, < 0 : new
        /// </summary>
        public int[] AddPath(Train.TCSubpathRoute thisPath, int startSectionIndex)
        {
            // check if equal to existing path
            for (int iIndex = 0; iIndex <= AvailablePathList.Count - 1; iIndex++)
            {
                DeadlockPathInfo existPathInfo = AvailablePathList[iIndex];
                if (thisPath.EqualsPath(existPathInfo.Path))
                {
                    // check if path referenced from correct start position, else add reference
                    if (PathReferences.ContainsKey(startSectionIndex))
                    {
                        if (!PathReferences[startSectionIndex].Contains(iIndex))
                        {
                            PathReferences[startSectionIndex].Add(iIndex);
                        }
                    }
                    else
                    {
                        List<int> refSectionPaths = new List<int>();
                        refSectionPaths.Add(iIndex);
                        PathReferences.Add(startSectionIndex, refSectionPaths);
                    }

                    // return path
                    return (new int[2] { iIndex, 1 });
                }
            }

            // new path
            int newPathIndex = AvailablePathList.Count;
            DeadlockPathInfo newPathInfo = new DeadlockPathInfo(thisPath, newPathIndex);
            AvailablePathList.Add(newPathInfo);

            // add path to list of paths from this section
            List<int> thisSectionPaths;

            if (PathReferences.ContainsKey(startSectionIndex))
            {
                thisSectionPaths = PathReferences[startSectionIndex];
            }
            else
            {
                thisSectionPaths = new List<int>();
                PathReferences.Add(startSectionIndex, thisSectionPaths);
            }

            thisSectionPaths.Add(newPathIndex);

            // set references for intermediate sections
            SetIntermediateReferences(thisPath, newPathIndex);

            if (AvailablePathList.Count == 1) // if only one entry, set name to MAIN (first path is MAIN path)
            {
                newPathInfo.Name = "MAIN";
            }
            else
            {
                newPathInfo.Name = String.Concat("PASS", AvailablePathList.Count.ToString("00"));
            }

            // check for reverse path (through existing paths only)
            for (int iPath = 0; iPath <= AvailablePathList.Count - 2; iPath++)
            {
                if (thisPath.EqualsReversePath(AvailablePathList[iPath].Path))
                {
                    InverseInfo.Add(newPathIndex, iPath);
                    InverseInfo.Add(iPath, newPathIndex);
                }
            }

            return (new int[2] { newPathIndex, -1 }); // set new path found
        }

        /// <summary>
        /// add named path to deadlock info
        /// return : [0] index to path
        ///          [1] > 0 : existing, < 0 : new
        /// </summary>
        public int[] AddPath(Train.TCSubpathRoute thisPath, int startSectionIndex, string thisName, string thisGroupName)
        {
            // check if equal to existing path and has same name
            for (int iIndex = 0; iIndex <= AvailablePathList.Count - 1; iIndex++)
            {
                DeadlockPathInfo existPathInfo = AvailablePathList[iIndex];
                if (thisPath.EqualsPath(existPathInfo.Path) && String.Compare(existPathInfo.Name, thisName) == 0)
                {
                    if (!String.IsNullOrEmpty(thisGroupName))
                    {
                        bool groupfound = false;
                        foreach (string groupName in existPathInfo.Groups)
                        {
                            if (String.Compare(groupName, thisGroupName) == 0)
                            {
                                groupfound = true;
                                break;
                            }
                        }

                        if (!groupfound) existPathInfo.Groups.Add(String.Copy(thisGroupName));
                    }

                    // check if path referenced from correct start position, else add reference
                    if (PathReferences.ContainsKey(startSectionIndex))
                    {
                        if (!PathReferences[startSectionIndex].Contains(iIndex))
                        {
                            PathReferences[startSectionIndex].Add(iIndex);
                        }
                    }
                    else
                    {
                        List<int> refSectionPaths = new List<int>();
                        refSectionPaths.Add(iIndex);
                        PathReferences.Add(startSectionIndex, refSectionPaths);
                    }

                    // return path
                    return (new int[2] { iIndex, 1 });
                }
            }

            // new path
            int newPathIndex = AvailablePathList.Count;
            DeadlockPathInfo newPathInfo = new DeadlockPathInfo(thisPath, newPathIndex);
            newPathInfo.Name = String.Copy(thisName);
            if (!String.IsNullOrEmpty(thisGroupName)) newPathInfo.Groups.Add(String.Copy(thisGroupName));

            AvailablePathList.Add(newPathInfo);

            // add path to list of path from this section
            List<int> thisSectionPaths;

            if (PathReferences.ContainsKey(startSectionIndex))
            {
                thisSectionPaths = PathReferences[startSectionIndex];
            }
            else
            {
                thisSectionPaths = new List<int>();
                PathReferences.Add(startSectionIndex, thisSectionPaths);
            }

            thisSectionPaths.Add(newPathIndex);

            // set references for intermediate sections
            SetIntermediateReferences(thisPath, newPathIndex);

            // check for reverse path (through existing paths only)
            for (int iPath = 0; iPath <= AvailablePathList.Count - 2; iPath++)
            {
                if (thisPath.EqualsReversePath(AvailablePathList[iPath].Path))
                {
                    InverseInfo.Add(newPathIndex, iPath);
                    InverseInfo.Add(iPath, newPathIndex);
                }
            }

            return (new int[2] { newPathIndex, -1 }); // return negative index to indicate new path
        }

        /// <summary>
        /// check if path has no conflict with overlapping deadlock paths
        /// returns false if there is an overlap
        /// </summary>
        public bool CheckNoOverlapDeadlockPaths(Train.TCSubpathRoute thisPath, Signals signalRef)
        {
            foreach (Train.TCRouteElement thisElement in thisPath)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.DeadlockReference >= 0)
                {
                    return (false);
                }
            }
            return (true);
        }

        /// <summary>
        /// check if at least one valid path is available into a deadlock area
        /// returns indices of available paths
        /// </summary>
        public List<int> CheckDeadlockPathAvailability(TrackCircuitSection startSection, Train thisTrain)
        {
            List<int> useablePaths = new List<int>();

            // get end section for this train
            int endSectionIndex = GetEndSection(thisTrain);
            TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

            // get list of paths which are available
            List<int> freePaths = GetFreePaths(thisTrain);

            // get all possible paths from train(s) in opposite direction
            List<int> usedRoutes = new List<int>();    // all routes allowed for any train
            List<int> commonRoutes = new List<int>();  // routes common to all trains
            List<int> singleRoutes = new List<int>();  // routes which are the single available route for trains which have one route only

            bool firstTrain = true;

            // loop through other trains
            foreach (int otherTrainNumber in endSection.DeadlockActives)
            {
                Train otherTrain = thisTrain.GetOtherTrainByNumber(otherTrainNumber);

                // TODO : find proper most matching path
                if (HasTrainAndSubpathIndex(otherTrain.Number, otherTrain.TCRoute.activeSubpath))
                {
                    List<int> otherFreePaths = GetFreePaths(otherTrain);
                    foreach (int iPath in otherFreePaths)
                    {
                        if (!usedRoutes.Contains(iPath)) usedRoutes.Add(iPath);
                        if (firstTrain)
                        {
                            commonRoutes.Add(iPath);
                        }
                    }

                    if (otherFreePaths.Count == 1)
                    {
                        singleRoutes.Add(otherFreePaths[0]);
                    }

                    for (int cPathIndex = commonRoutes.Count - 1; cPathIndex >= 0 && !firstTrain; cPathIndex--)
                    {
                        if (!otherFreePaths.Contains(commonRoutes[cPathIndex]))
                        {
                            commonRoutes.RemoveAt(cPathIndex);
                        }
                    }
                }
                else
                {
                    // for now : set all possible routes to used and single
                    foreach (int iroute in freePaths)
                    {
                        singleRoutes.Add(iroute);
                        usedRoutes.Add(iroute);
                    }
                }

                firstTrain = false;
            }

#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n=================\nTrain : " + thisTrain.Number.ToString() + "\n");
            File.AppendAllText(@"C:\Temp\deadlock.txt", "At Section : " + startSection.Index.ToString() + "\n");
            File.AppendAllText(@"C:\Temp\deadlock.txt", "To Section : " + endSection.Index.ToString() + "\n");
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n Available paths : \n");
            foreach (int avroute in PathReferences[startSection.Index])
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Path index : " + avroute.ToString() + " : \n");
                Train.TCSubpathRoute thisPath = AvailablePathList[avroute].Path;
                foreach (Train.TCRouteElement thisElement in thisPath)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "   - Element : " + thisElement.TCSectionIndex.ToString() + "\n");
                }
            }
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n Available Inverse paths : \n");

            if (PathReferences.ContainsKey(endSection.Index))
            {
                foreach (int avroute in PathReferences[endSection.Index])
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "Path index : " + avroute.ToString() + " : \n");
                    Train.TCSubpathRoute thisPath = AvailablePathList[avroute].Path;
                    foreach (Train.TCRouteElement thisElement in thisPath)
                    {
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "   - Element : " + thisElement.TCSectionIndex.ToString() + "\n");
                    }
                }
            }
            else
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\nNo Inverse paths available \n");
            }

            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n Inverse references : \n");
            foreach (KeyValuePair<int, int> inverseDetail in InverseInfo)
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", " Path : " + inverseDetail.Key.ToString() + " -> " + inverseDetail.Value.ToString() + "\n");
            }
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n Free paths : \n");
            foreach (int avroute in freePaths)
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Path index : " + avroute.ToString() + " : \n");
                Train.TCSubpathRoute thisPath = AvailablePathList[avroute].Path;
                foreach (Train.TCRouteElement thisElement in thisPath)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "   - Element : " + thisElement.TCSectionIndex.ToString() + "\n");
                }
            }

            File.AppendAllText(@"C:\Temp\deadlock.txt", "\nMeeting : \n");

            foreach (int otherTrainNumber in endSection.DeadlockActives)
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "   Other train : " + otherTrainNumber.ToString() + "\n");
            }

            File.AppendAllText(@"C:\Temp\deadlock.txt", "\nUsed paths : \n");
            foreach (int iRoute in usedRoutes)
            {
                if (InverseInfo.ContainsKey(iRoute))
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = " + InverseInfo[iRoute].ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = <no inverse> \n");
                }
            }

            File.AppendAllText(@"C:\Temp\deadlock.txt", "\nCommom paths : \n");
            foreach (int iRoute in commonRoutes)
            {
                if (InverseInfo.ContainsKey(iRoute))
                {
                File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = " + InverseInfo[iRoute].ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = <no inverse> \n");
                }
            }

            File.AppendAllText(@"C:\Temp\deadlock.txt", "\nSingle paths : \n");
            foreach (int iRoute in singleRoutes)
            {
                if (InverseInfo.ContainsKey(iRoute))
                {
                File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = " + InverseInfo[iRoute].ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " - route index : " + iRoute.ToString() + " = <no inverse> \n");
                }
            }
#endif
            // get inverse path indices to compare with this train's paths
            List<int> inverseUsedRoutes = new List<int>();
            List<int> inverseCommonRoutes = new List<int>();
            List<int> inverseSingleRoutes = new List<int>();

            foreach (int iPath in usedRoutes)
            {
                if (InverseInfo.ContainsKey(iPath))
                    inverseUsedRoutes.Add(InverseInfo[iPath]);
            }
            foreach (int iPath in commonRoutes)
            {
                if (InverseInfo.ContainsKey(iPath))
                    inverseCommonRoutes.Add(InverseInfo[iPath]);
            }
            foreach (int iPath in singleRoutes)
            {
                if (InverseInfo.ContainsKey(iPath))
                    inverseSingleRoutes.Add(InverseInfo[iPath]);
            }

            // if deadlock is awaited at other end : remove paths which would cause conflict
            if (endSection.CheckDeadlockAwaited(thisTrain.Number))
            {
#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\n ++ Deadlock Awaited\n");
#endif
                // check if this train has any route not used by trains from other end

                foreach (int iPath in freePaths)
                {
                    if (!inverseUsedRoutes.Contains(iPath)) useablePaths.Add(iPath);
                }

                if (useablePaths.Count > 0) return (useablePaths); // unused paths available

                // check if any path remains if common paths are excluded

                if (inverseCommonRoutes.Count >= 1) // there are common routes, so other routes may be used
                {
                    foreach (int iPath in freePaths)
                    {
                        if (!inverseCommonRoutes.Contains(iPath)) useablePaths.Add(iPath);
                    }

                    if (useablePaths.Count > 0)
                    {
#if DEBUG_DEADLOCK
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- Usable paths (checked common) : \n");
                        foreach (int iRoute in useablePaths)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                        }
#endif
                        return (useablePaths);
                    }
                }

                // check if any path remains if all required single paths are excluded
                if (inverseSingleRoutes.Count >= 1) // there are single paths
                {
                    foreach (int iPath in freePaths)
                    {
                        if (!inverseSingleRoutes.Contains(iPath)) useablePaths.Add(iPath);
                    }

                    if (useablePaths.Count > 0)
                    {
#if DEBUG_DEADLOCK
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- Usable paths (after checking single) : \n");
                        foreach (int iRoute in useablePaths)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                        }
#endif
                        return (useablePaths);
                    }
                }

                // no path available without conflict - but if deadlock also awaited on this end, proceed anyway (otherwise everything gets stuck)
                if (startSection.DeadlockAwaited.Count >= 1)
                {
#if DEBUG_DEADLOCK
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- Free paths (deadlock awaited this end) : \n");
                    foreach (int iRoute in freePaths)
                    {
                        File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                    }
#endif
                    return (freePaths); // may use any path in this situation
                }

                // no path available - return empty list

#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- No paths available) : \n");
                foreach (int iRoute in useablePaths)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                }
#endif
                return (useablePaths);
            }
            // no deadlock awaited at other end : check if there is any single path set, if so exclude those to avoid conflict
            else
            {
#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\n ++ No Deadlock Awaited\n");
#endif
                // check if any path remains if all required single paths are excluded
                if (inverseSingleRoutes.Count >= 1) // there are single paths
                {
                    foreach (int iPath in freePaths)
                    {
                        if (!inverseSingleRoutes.Contains(iPath)) useablePaths.Add(iPath);
                    }

                    if (useablePaths.Count > 0)
                    {
#if DEBUG_DEADLOCK
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- Usable paths (after checking singles) : \n");
                        foreach (int iRoute in useablePaths)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                        }
#endif
                        return (useablePaths);
                    }
                }

                // no single path conflicts - so all free paths are available
#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n ----- No single paths conflicts - all paths available : \n");
                foreach (int iRoute in freePaths)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", " Route : " + iRoute.ToString() + "\n");
                }
#endif
                return (freePaths);
            }
        }

        /// <summary>
        /// get valid list of indices related available for specific train / subpath index
        /// </summary>
        public List<int> GetValidPassingPaths(int trainNumber, int sublistRef, bool allowPublic)
        {
            List<int> foundIndices = new List<int>();

            for (int iPath = 0; iPath <= AvailablePathList.Count - 1; iPath++)
            {
                DeadlockPathInfo thisPathInfo = AvailablePathList[iPath];
                int trainSubpathIndex = GetTrainAndSubpathIndex(trainNumber, sublistRef);
                if (thisPathInfo.AllowedTrains.Contains(trainSubpathIndex) || (thisPathInfo.AllowedTrains.Contains(-1) && allowPublic))
                {
                    foundIndices.Add(iPath);
                }
            }

            return (foundIndices);
        }

        /// <summary>
        /// check availability of passing paths
        /// return list of paths which are free
        /// </summary>
        public List<int> GetFreePaths(Train thisTrain)
        {
            List<int> freePaths = new List<int>();

            int thisTrainAndSubpathIndex = GetTrainAndSubpathIndex(thisTrain.Number, thisTrain.TCRoute.activeSubpath);
            for (int iPath = 0; iPath <= TrainReferences[thisTrainAndSubpathIndex].Count - 1; iPath++)
            {
                int pathIndex = TrainReferences[thisTrainAndSubpathIndex][iPath];
                DeadlockPathInfo altPathInfo = AvailablePathList[pathIndex];
                Train.TCSubpathRoute altPath = altPathInfo.Path;

                // check all sections upto and including last used index, but do not check first junction section
                bool pathAvail = true;
                for (int iElement = 1; iElement <= altPathInfo.LastUsefullSectionIndex; iElement++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[altPath[iElement].TCSectionIndex];
                    if (!thisSection.IsAvailable(thisTrain.routedForward))
                    {
                        pathAvail = false;
                        break;
                    }
                }

                if (pathAvail) freePaths.Add(pathIndex);
            }

            return (freePaths);
        }

        /// <summary>
        /// set deadlock info references for intermediate sections
        /// </summary>
        public int SelectPath(List<int> availableRoutes, Train thisTrain, ref int endSectionIndex)
        {
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n\n**** For train " + thisTrain.Number.ToString() + " Select route from : \n");
            foreach (int iRoute in availableRoutes)
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Available route : " + iRoute.ToString() + "\n");
            }
#endif
            int selectedPathNofit = -1;
            int selectedPathFit = -1;

            int defaultPath = 0;

            bool checkedMain = false;
            bool checkedOwn = false;

            endSectionIndex = GetEndSection(thisTrain);
            TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

            bool preferMain = true;
            // if deadlock actives : main least preferred
            if (endSection.DeadlockActives.Count > 0)
            {
                preferMain = false;
                checkedMain = true; // consider main as checked
            }

            // check if own path is also main path - if so, do not check it separately
            int indexTrainAndSubroute = GetTrainAndSubpathIndex(thisTrain.Number, thisTrain.TCRoute.activeSubpath);
            int ownPathIndex = TrainOwnPath[indexTrainAndSubroute];
            defaultPath = ownPathIndex;

            if (String.Compare(AvailablePathList[ownPathIndex].Name, "MAIN") == 0)
            {
                checkedOwn = true; // do not check own path separately
            }

            // get train fit list
            Dictionary<int, bool> trainFitInfo = TrainLengthFit[indexTrainAndSubroute];

            // loop through all available paths
            for (int iPath = 0; iPath <= availableRoutes.Count - 1; iPath++)
            {
                int pathIndex = availableRoutes[iPath];
                DeadlockPathInfo pathInfo = AvailablePathList[pathIndex];
                bool trainFitsInSection = trainFitInfo[pathIndex];

                // check for OWN
                if (!checkedOwn && pathIndex == ownPathIndex)
                {
                    checkedOwn = true;
                    if (trainFitsInSection)
                    {
                        selectedPathFit = pathIndex;
                        break; // if train fits in own path, break
                    }

                    selectedPathNofit = pathIndex;
                    if (checkedMain && selectedPathFit > 0) break;  // if doesnt fit but main has been checked and train fits somewhere, break
                }

                // check for MAIN
                if (String.Compare(pathInfo.Name, "MAIN") == 0)
                {
                    checkedMain = true;
                    if (trainFitsInSection)
                    {
                        selectedPathFit = pathIndex;
                        if (checkedOwn && preferMain) break;  // if fits and own has been checked and main prefered - break
                    }
                    else
                    {
                        if (!checkedOwn || selectedPathNofit < 0 || preferMain)  // if own has not been checked
                        {
                            selectedPathNofit = pathIndex;
                        }
                    }
                }
                // check for others
                else
                {
                    if (trainFitsInSection) // if train fits
                    {
                        selectedPathFit = pathIndex;
                        if (checkedMain || checkedOwn)
                        {
                            break;  // main and own allready checked so no need to look further
                        }
                    }
                    else
                    {
                        if ((!checkedOwn && !checkedMain) || !preferMain) // set as option if own and main both not checked or main not prefered
                        {
                            selectedPathNofit = pathIndex;
                        }
                    }
                }
            }

#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", " Selected path (fit)   : " + selectedPathFit.ToString() + "\n");
            File.AppendAllText(@"C:\Temp\deadlock.txt", " Selected path (nofit) : " + selectedPathNofit.ToString() + "\n");
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n****\n\n");
#endif
            // Sometimes selectedPathFit nor selectedPathNofit gets new value, which is wrong and will induce an
            // IndexOutOfRangeException, but I can't find out why that happens, so here is a warning message when it
            // happens, to at least find out which train, and passing path that triggers this bug.
            if (selectedPathFit < 0 && selectedPathNofit < 0 && defaultPath < 0)
                Trace.TraceWarning("Path can't be selected for train {0} at end-section index {1}", thisTrain.Name, endSectionIndex);
            return (selectedPathFit >= 0 ? selectedPathFit : selectedPathNofit >= 0 ? selectedPathNofit : defaultPath); // return fit path if set else no-fit path if set else default path
        }

        /// <summary>
        /// get end section index for deadlock area for a particular train
        /// </summary>
        public int GetEndSection(Train thisTrain)
        {
            int thisTrainAndSubpathIndex = GetTrainAndSubpathIndex(thisTrain.Number, thisTrain.TCRoute.activeSubpath);
            if (!TrainReferences.ContainsKey(thisTrainAndSubpathIndex))
            {
                Trace.TraceWarning("Multiple passing paths at the same location, without common branch out, or return switch. Check the passing paths for Train name: {0} (number: {1}), and other train's paths, which have passing paths at the same locations", thisTrain.Name, thisTrain.Number);
            }
            int pathIndex = TrainReferences[thisTrainAndSubpathIndex][0];
            DeadlockPathInfo pathInfo = AvailablePathList[pathIndex];
            return (pathInfo.EndSectionIndex);
        }

        /// <summary>
        /// set deadlock info references for intermediate sections
        /// </summary>
        public void SetIntermediateReferences(Train.TCSubpathRoute thisPath, int pathIndex)
        {
            for (int iElement = 1; iElement <= thisPath.Count - 2; iElement++) // loop through path excluding first and last section
            {
                Train.TCRouteElement thisElement = thisPath[iElement];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.DeadlockBoundaries == null)
                {
                    thisSection.DeadlockBoundaries = new Dictionary<int, int>();
                }

                if (!thisSection.DeadlockBoundaries.ContainsKey(DeadlockIndex))
                {
                    thisSection.DeadlockBoundaries.Add(DeadlockIndex, pathIndex);
                }
            }
        }

        /// <summary>
        /// get index value for specific train/subpath combination
        /// if set, return value
        /// if not set, generate value, set value and return value
        /// </summary>
        public int GetTrainAndSubpathIndex(int trainNumber, int subpathIndex)
        {
            if (TrainSubpathIndex.ContainsKey(trainNumber))
            {
                Dictionary<int, int> subpathList = TrainSubpathIndex[trainNumber];
                if (subpathList.ContainsKey(subpathIndex))
                {
                    return (subpathList[subpathIndex]);
                }
            }

            int newIndex = ++nextTrainSubpathIndex;
            Dictionary<int, int> newSubpathList;
            if (TrainSubpathIndex.ContainsKey(trainNumber))
            {
                newSubpathList = TrainSubpathIndex[trainNumber];
            }
            else
            {
                newSubpathList = new Dictionary<int, int>();
                TrainSubpathIndex.Add(trainNumber, newSubpathList);
            }

            newSubpathList.Add(subpathIndex, newIndex);

            return (newIndex);
        }

        /// <summary>
        /// check index value for specific train/subpath combination
        /// if set, return value
        /// if not set, generate value, set value and return value
        /// </summary>
        public bool HasTrainAndSubpathIndex(int trainNumber, int subpathIndex)
        {
            if (TrainSubpathIndex.ContainsKey(trainNumber))
            {
                Dictionary<int, int> subpathList = TrainSubpathIndex[trainNumber];
                if (subpathList.ContainsKey(subpathIndex))
                {
                    return (true);
                }
            }
            return (false);
        }

        /// <summary>
        /// check index value for specific train/subpath combination
        /// if set, return value
        /// if not set, generate value, set value and return value
        /// </summary>
        public bool RemoveTrainAndSubpathIndex(int trainNumber, int subpathIndex)
        {
            if (TrainSubpathIndex.ContainsKey(trainNumber))
            {
                Dictionary<int, int> subpathList = TrainSubpathIndex[trainNumber];
                if (subpathList.ContainsKey(subpathIndex))
                {
                    subpathList.Remove(subpathIndex);
                }
                if (subpathList.Count <= 0)
                {
                    TrainSubpathIndex.Remove(trainNumber);
                }
            }
            return (false);
        }

        /// <summary>
        /// Insert train reference details
        /// </summary>
        public int SetTrainDetails(int trainNumber, int subpathRef, float trainLength, Train.TCSubpathRoute subpath, int elementRouteIndex)
        {
            Train.TCSubpathRoute partPath = null;  // retreived route of train through deadlock area

            // search if trains path has valid equivalent
            if (elementRouteIndex <= 0 || elementRouteIndex >= subpath.Count)
            {
                Trace.TraceWarning("Invalid route element in SetTrainDetails : value =  {0}, max. is {1}", elementRouteIndex, subpath.Count);
                return (-1);
            }

            int trainSubpathIndex = GetTrainAndSubpathIndex(trainNumber, subpathRef);
            int sectionIndex = subpath[elementRouteIndex].TCSectionIndex;
            int[] matchingPath = SearchMatchingFullPath(subpath, sectionIndex, elementRouteIndex);

            // matchingPath[0] == 1 : path runs short of all available paths - train ends within area - no alternative path available
            if (matchingPath[0] == 1)
            {
                // if no other paths for this reference, remove train/subpath reference from table
                if (!TrainReferences.ContainsKey(trainSubpathIndex))
                {
                    RemoveTrainAndSubpathIndex(trainNumber, subpathRef);
                }
                return (-1);
            }

            // matchingPath[0] == 2 : path runs through area but has no match - insert path for this train only (no inverse inserted)
            // matchingPath[1] = end section index in route
            if (matchingPath[0] == 2)
            {
                partPath = new Train.TCSubpathRoute(subpath, elementRouteIndex, matchingPath[1]);
                int[] pathReference = AddPath(partPath, sectionIndex);
                DeadlockPathInfo thisPathInfo = AvailablePathList[pathReference[0]];

                Dictionary<int, float> pathEndAndLengthInfo = partPath.GetUsefullLength(0.0f, signalRef, -1, -1);
                KeyValuePair<int, float> pathEndAndLengthValue = pathEndAndLengthInfo.ElementAt(0);
                thisPathInfo.UsefullLength = pathEndAndLengthValue.Value;
                thisPathInfo.LastUsefullSectionIndex = pathEndAndLengthValue.Key;
                thisPathInfo.EndSectionIndex = subpath[matchingPath[1]].TCSectionIndex;
                thisPathInfo.Name = String.Empty;  // path has no name

                thisPathInfo.AllowedTrains.Add(trainSubpathIndex);
                TrainOwnPath.Add(trainSubpathIndex, pathReference[0]);
            }
            // matchingPath[0] == 3 : path runs through area but no valid path available or possible - remove train index as train has no alternative paths at this location
            else if (matchingPath[0] == 3)
            {
                RemoveTrainAndSubpathIndex(trainNumber, subpathRef);
                return (matchingPath[1]);
            }
            // otherwise matchingPath [1] is matching path - add track details if not yet set
            else
            {
                DeadlockPathInfo thisPathInfo = AvailablePathList[matchingPath[1]];
                if (!thisPathInfo.AllowedTrains.Contains(trainSubpathIndex))
                {
                    thisPathInfo.AllowedTrains.Add(trainSubpathIndex);
                }
                TrainOwnPath.Add(trainSubpathIndex, matchingPath[1]);
            }

            // set cross-references to allowed track entries for easy reference
            List<int> availPathList;

            if (TrainReferences.ContainsKey(trainSubpathIndex))
            {
                availPathList = TrainReferences[trainSubpathIndex];
            }
            else
            {
                availPathList = new List<int>();
                TrainReferences.Add(trainSubpathIndex, availPathList);
            }

            Dictionary<int, bool> thisTrainFitList;
            if (TrainLengthFit.ContainsKey(trainSubpathIndex))
            {
                thisTrainFitList = TrainLengthFit[trainSubpathIndex];
            }
            else
            {
                thisTrainFitList = new Dictionary<int, bool>();
                TrainLengthFit.Add(trainSubpathIndex, thisTrainFitList);
            }

            for (int iPath = 0; iPath <= AvailablePathList.Count - 1; iPath++)
            {
                DeadlockPathInfo thisPathInfo = AvailablePathList[iPath];

                if (thisPathInfo.AllowedTrains.Contains(-1) || thisPathInfo.AllowedTrains.Contains(trainSubpathIndex))
                {
                    if (this.PathReferences[sectionIndex].Contains(iPath)) // path from correct end
                    {
                        availPathList.Add(iPath);

                        bool trainFit = (trainLength < thisPathInfo.UsefullLength);
                        thisTrainFitList.Add(iPath, trainFit);
                    }
                }
            }

            // get end section from first valid path
            partPath = new Train.TCSubpathRoute(AvailablePathList[availPathList[0]].Path);
            int lastSection = partPath[partPath.Count - 1].TCSectionIndex;
            int returnIndex = subpath.GetRouteIndex(lastSection, elementRouteIndex);
            return (returnIndex);
        }

        /// <summary>
        /// Search matching path from full route path
        ///
        /// return : [0] = 0 : matching path, [1] = matching path index
        ///          [0] = 1 : no matching path and route does not contain any of the end sections (route ends within area)
        ///          [0] = 2 : no matching path but route does run through area, [1] contains end section index
        ///          [0] = 3 : no matching path in required direction but route does run through area, [1] contains end section index
        /// </summary>
        public int[] SearchMatchingFullPath(Train.TCSubpathRoute fullPath, int startSectionIndex, int startSectionRouteIndex)
        {
            int[] matchingValue = new int[2] { 0, 0 };
            int foundMatchingEndRouteIndex = -1;
            int matchingPath = -1;

            // paths available from start section
            if (PathReferences.ContainsKey(startSectionIndex))
            {
                List<int> availablePaths = PathReferences[startSectionIndex];

                // search through paths from this section
                for (int iPath = 0; iPath <= availablePaths.Count - 1; iPath++)
                {
                    // extract path, get indices in train path
                    Train.TCSubpathRoute testPath = AvailablePathList[availablePaths[iPath]].Path;
                    int endSectionIndex = AvailablePathList[availablePaths[iPath]].EndSectionIndex;
                    int endSectionRouteIndex = fullPath.GetRouteIndex(endSectionIndex, startSectionRouteIndex);

                    // can only be matching path if endindex > 0 and endindex != startindex (if wrong way path, endindex = startindex)
                    if (endSectionRouteIndex > 0 && endSectionRouteIndex != startSectionRouteIndex)
                    {
                        Train.TCSubpathRoute partPath = new Train.TCSubpathRoute(fullPath, startSectionRouteIndex, endSectionRouteIndex);

                        // test route
                        if (partPath.EqualsPath(testPath))
                        {
                            matchingPath = availablePaths[iPath];
                            break;
                        }

                        // set end index (if not yet found)
                        if (foundMatchingEndRouteIndex < 0)
                        {
                            foundMatchingEndRouteIndex = endSectionRouteIndex;
                        }
                    }
                    // no matching end index - check train direction
                    else
                    {
                        // check direction
                        int areadirection = AvailablePathList[availablePaths[0]].Path[0].Direction;
                        int traindirection = fullPath[startSectionRouteIndex].Direction;

                        // train has same direction - check if end of path is really within the path
                        if (areadirection == traindirection)
                        {
                            int pathEndSection = fullPath[fullPath.Count - 1].TCSectionIndex;
                            if (testPath.GetRouteIndex(pathEndSection, 0) >= 0) // end point is within section
                            {
                                matchingValue[0] = 1;
                                matchingValue[1] = 0;
                                return (matchingValue);
                            }
                        }
                        else  //if wrong direction, train exits area at this location//
                        {
                            matchingValue[0] = 3;
                            matchingValue[1] = startSectionRouteIndex + 1;
                            return (matchingValue);
                        }
                    }
                }
            }
            // no paths available from start section, check if end section of paths matches start section
            else
            {
                if (startSectionIndex == AvailablePathList[0].EndSectionIndex)
                {
                    int matchingEndIndex = fullPath.GetRouteIndex(AvailablePathList[0].Path[0].TCSectionIndex, startSectionRouteIndex);
                    if (matchingEndIndex > 0)
                    {
                        matchingValue[0] = 2;
                        matchingValue[1] = matchingEndIndex;
                    }
                    else
                    {
                        matchingValue[0] = 3;
                        matchingValue[1] = startSectionRouteIndex + 1;
                    }
                    return (matchingValue);
                }
            }

            if (matchingPath >= 0)
            {
                matchingValue[0] = 0;
                matchingValue[1] = matchingPath;
            }
            else if (foundMatchingEndRouteIndex >= 0)
            {
                matchingValue[0] = 2;
                matchingValue[1] = foundMatchingEndRouteIndex;
            }
            else
            {
                matchingValue[0] = 3;
                matchingValue[1] = startSectionRouteIndex + 1;
            }

            return (matchingValue);
        }
    }
}
