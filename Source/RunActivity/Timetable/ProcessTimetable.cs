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

// Set debug flag to extract additional info
// Info is printed to C:\temp\timetableproc.txt
// #define DEBUG_TIMETABLE
// #define DEBUG_TRACEINFO

//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.MultiPlayer;
using ORTS.Viewer3D;
using ORTS.Viewer3D.Popups;
using ORTS.Formats;
using ORTS;

namespace ORTS
{
    class TimetableInfo
    {
        private Simulator simulator;

        private enum columnType
        {
            stationInfo,
            addStationInfo,
            comment,
            trainDefinition,
            trainAddInfo,
        }

        private enum rowType
        {
            trainInfo,
            stationInfo,
            addStationInfo,
            consistInfo,
            pathInfo,
            startInfo,
            disposeInfo,
            directionInfo,
            trainNotesInfo,
            comment,
        }

        Dictionary<string, AIPath> Paths = new Dictionary<string, AIPath>();                        // original path referenced by path name
        Dictionary<int, string> TrainRouteXRef = new Dictionary<int, string>();                     // path name referenced from train index    

        /// <summary>
        ///  Constructor - empty constructor
        /// </summary>
        public TimetableInfo(Simulator simulatorref)
        {
            simulator = simulatorref;
        }

        /// <summary>
        /// Process timetable file
        /// Convert info into list of trains
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>List of extracted Trains</returns>
        public List<AITrain> ProcessTimetable(string[] arguments, ref Train reqPlayerTrain)
        {
            List<AITrain> trainList = new List<AITrain>();
            List<TTTrainInfo> trainInfoList = new List<TTTrainInfo>();
            TTTrainInfo playerTrain = null;
            List<string> filenames;
            int indexcount = 0;

            // get filenames to process
            filenames = GetFilenames(arguments[0]);

            // get file contents as strings
            Trace.Write("\n");
            foreach (string filePath in filenames)
            {
                // get contents as strings
                Trace.Write("TT File : " + filePath + "\n");
                TTContents fileContents = new TTContents(filePath);

#if DEBUG_TIMETABLE
                File.AppendAllText(@"C:\temp\timetableproc.txt", "\nProcessing file : " + filePath + "\n");
#endif

                // convert to train info
                indexcount = ConvertFileContents(fileContents, simulator.Signals, ref trainInfoList, indexcount);
            }

            // read and pre-process routes

            Trace.Write(" TTROUTES:" + Paths.Count.ToString() + " ");

            PreProcessRoutes();

            Trace.Write(" TTTRAINS:" + trainInfoList.Count.ToString() + " ");

            // get startinfo for player train
            playerTrain = GetPlayerTrain(ref trainInfoList, arguments);

            // pre-init player train to abstract alternative paths if set
            if (playerTrain != null)
            {
                PreInitPlayerTrain(playerTrain);
            }

            // reduce trainlist using player train info and parameters
            trainList = ReduceAITrains(trainInfoList, playerTrain, arguments);

            // set references (required to process commands)
            foreach (Train thisTrain in trainList)
            {
                simulator.TrainDictionary.Add(thisTrain.Number, thisTrain);
                simulator.NameDictionary.Add(thisTrain.Name.ToLower(), thisTrain);
            }

            // set player train
            reqPlayerTrain = null;
            if (playerTrain != null)
            {
                reqPlayerTrain = InitializePlayerTrain(playerTrain);
                simulator.TrainDictionary.Add(reqPlayerTrain.Number, reqPlayerTrain);
                simulator.NameDictionary.Add(reqPlayerTrain.Name.ToLower(), reqPlayerTrain);
            }

            // process additional commands for all extracted trains
            reqPlayerTrain.FinalizeTimetableCommands();
            foreach (Train thisTrain in trainList)
            {
                thisTrain.FinalizeTimetableCommands();
            }

            // set timetable identification for simulator for saves etc.
            simulator.TimetableFileName = Path.GetFileNameWithoutExtension(arguments[0]);
            return (trainList);
        }

        /// <summary>
        /// Get filenames of TTfiles to process
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> GetFilenames(string filePath)
        {
            List<string> filenames = new List<string>();

            // check type of timetable file - list or single
            string fileExtension = Path.GetExtension(filePath);
            string fileDirectory = Path.GetDirectoryName(filePath);
            switch (fileExtension)
            {
                case ".timetable_or":
                    filenames.Add(filePath);
                    break;

                case ".timetablelist_or":
                    MultiTTInfo multiInfo = new MultiTTInfo(filePath, fileDirectory);
                    filenames = multiInfo.TTFiles;
                    break;

                default:
                    throw new InvalidDataException("Invalid type of file passed to timetable info : " + filePath);
            }

#if DEBUG_TIMETABLE
            if (File.Exists(@"C:\temp\timetableproc.txt"))
            {
                File.Delete(@"C:\temp\timetableproc.txt");
            }

            File.AppendAllText(@"C:\temp\timetableproc.txt", "Files : \n");
            foreach (string ttfile in filenames)
            {
                File.AppendAllText(@"C:\temp\timetableproc.txt", ttfile + "\n");
            }
#endif
            return (filenames);
        }

        /// <summary>
        /// Extract information and convert to traininfo
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="signalRef"></param>
        /// <param name="TDB"></param>
        /// <param name="trainInfoList"></param>
        private int ConvertFileContents(TTContents fileContents, Signals signalRef, ref List<TTTrainInfo> trainInfoList, int indexcount)
        {
            int consistRow = -1;
            int pathRow = -1;
            int startRow = -1;
            int disposeRow = -1;

            int firstCommentRow = -1;
            int firstCommentColumn = -1;

            Dictionary<int, string> trainHeaders = new Dictionary<int, string>();          // key int = column no, value string = train header
            Dictionary<int, TTTrainInfo> trainInfo = new Dictionary<int, TTTrainInfo>();   // key int = column no, value = train info class
            Dictionary<int, int> addTrainInfo = new Dictionary<int, int>();                // key int = column no, value int = main train column
            Dictionary<int, List<int>> addTrainColumns = new Dictionary<int, List<int>>(); // key int = main train column, value = add columns
            Dictionary<int, string> stationNames = new Dictionary<int, string>();          // key int = row no, value string = station name

            rowType[] RowInfo = new rowType[fileContents.trainStrings.Count];
            columnType[] ColInfo = new columnType[fileContents.trainStrings[0].Length];

            // process first row separately

            ColInfo[0] = columnType.stationInfo;

            for (int iColumn = 1; iColumn <= fileContents.trainStrings[0].Length - 1; iColumn++)
            {
                string columnDef = fileContents.trainStrings[0][iColumn];

                // empty : continuation column
                if (String.IsNullOrEmpty(columnDef))
                {
                    switch (ColInfo[iColumn - 1])
                    {
                        case columnType.stationInfo:
                        case columnType.addStationInfo:
                            ColInfo[iColumn] = columnType.addStationInfo;
                            break;

                        case columnType.comment:
                            ColInfo[iColumn] = columnType.comment;
                            break;

                        case columnType.trainDefinition:
                            ColInfo[iColumn] = columnType.trainAddInfo;
                            addTrainInfo.Add(iColumn, iColumn - 1);
                            break;

                        case columnType.trainAddInfo:
                            ColInfo[iColumn] = columnType.trainAddInfo;
                            addTrainInfo.Add(iColumn, addTrainInfo[iColumn - 1]);
                            break;
                    }
                }

                // comment
                else if (String.Compare(columnDef, "#comment", true) == 0)
                {
                    ColInfo[iColumn] = columnType.comment;
                    if (firstCommentColumn < 0) firstCommentColumn = iColumn;
                }

                // otherwise it is a train definition
                else
                {
                    ColInfo[iColumn] = columnType.trainDefinition;
                    trainHeaders.Add(iColumn, String.Copy(columnDef));
                    trainInfo.Add(iColumn, new TTTrainInfo(iColumn, columnDef, simulator, indexcount));
                    indexcount++;
                }
            }

            // get row information
            RowInfo[0] = rowType.trainInfo;

            for (int iRow = 1; iRow <= fileContents.trainStrings.Count - 1; iRow++)
            {
                string rowDef = fileContents.trainStrings[iRow][0];

                // emtpy : continuation
                if (String.IsNullOrEmpty(rowDef))
                {
                    switch (RowInfo[iRow - 1])
                    {
                        case rowType.stationInfo:
                            RowInfo[iRow] = rowType.addStationInfo;
                            break;

                        default:  // continuation of other types not allowed, treat line as comment
                            RowInfo[iRow] = rowType.comment;
                            break;
                    }
                }

                // switch on actual string

                else
                {
                    switch (rowDef)
                    {
                        case "#consist":
                            RowInfo[iRow] = rowType.consistInfo;
                            consistRow = iRow;
                            break;

                        case "#path":
                            RowInfo[iRow] = rowType.pathInfo;
                            pathRow = iRow;
                            break;

                        case "#start":
                            RowInfo[iRow] = rowType.startInfo;
                            startRow = iRow;
                            break;

                        case "#dispose":
                            RowInfo[iRow] = rowType.disposeInfo;
                            disposeRow = iRow;
                            break;

                        case "#direction":
                            RowInfo[iRow] = rowType.directionInfo;
                            break;

                        case "#note":
                            RowInfo[iRow] = rowType.trainNotesInfo;
                            break;

                        case "#comment":
                            RowInfo[iRow] = rowType.comment;
                            if (firstCommentRow < 0) firstCommentRow = iRow;
                            break;

                        default:  // default is station definition
                            RowInfo[iRow] = rowType.stationInfo;
                            stationNames.Add(iRow, String.Copy(rowDef));
                            break;
                    }
                }
            }

#if DEBUG_TIMETABLE
            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Row and Column details : \n");

            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Columns : \n");
            for (int iColumn = 0; iColumn <= ColInfo.Length - 1; iColumn++)
            {
                columnType ctype = ColInfo[iColumn];

                var stbuild = new StringBuilder();
                stbuild.AppendFormat("Column : {0} = {1}", iColumn, ctype.ToString());
                if (ctype == columnType.trainDefinition)
                {
                    stbuild.AppendFormat(" = train : {0}", trainHeaders[iColumn]);
                }
                stbuild.Append("\n");
                File.AppendAllText(@"C:\temp\timetableproc.txt", stbuild.ToString());
            }

            File.AppendAllText(@"C:\temp\timetableproc.txt", "\n Rows : \n");
            for (int iRow = 0; iRow <= RowInfo.Length - 1; iRow++)
            {
                rowType rtype = RowInfo[iRow];

                var stbuild = new StringBuilder();
                stbuild.AppendFormat("Row    : {0} = {1}", iRow, rtype.ToString());
                if (rtype == rowType.stationInfo)
                {
                    stbuild.AppendFormat(" = station {0}", stationNames[iRow]);
                }
                stbuild.Append("\n");
                File.AppendAllText(@"C:\temp\timetableproc.txt", stbuild.ToString());
            }
#endif

            // extract description

            string description = (firstCommentRow >= 0 && firstCommentColumn >= 0) ?
                fileContents.trainStrings[firstCommentRow][firstCommentColumn] : Path.GetFileNameWithoutExtension(fileContents.TTfilename);

            // build list of additional train columns

            foreach (KeyValuePair<int, int> addColumn in addTrainInfo)
            {
                if (addTrainColumns.ContainsKey(addColumn.Value))
                {
                    addTrainColumns[addColumn.Value].Add(addColumn.Key);
                }
                else
                {
                    List<int> addTrainColumn = new List<int>();
                    addTrainColumn.Add(addColumn.Key);
                    addTrainColumns.Add(addColumn.Value, addTrainColumn);
                }
            }

            // build actual trains

            for (int iColumn = 1; iColumn <= ColInfo.Length - 1; iColumn++)
            {
                if (ColInfo[iColumn] == columnType.trainDefinition)
                {
                    List<int> addColumns = null;
                    addTrainColumns.TryGetValue(iColumn, out addColumns);

                    if (addColumns != null)
                    {
                        ConcatTrainStrings(fileContents.trainStrings, iColumn, addColumns);
                    }

                    trainInfo[iColumn].BuildTrain(fileContents.trainStrings, RowInfo, pathRow, consistRow, startRow, disposeRow, description, stationNames, this);
                }
            }

            // extract valid trains
            foreach (KeyValuePair<int, TTTrainInfo> train in trainInfo)
            {
                if (train.Value.validTrain)
                {
                    trainInfoList.Add(train.Value);
                }
            }

            return (indexcount);
        }

        /// <summary>
        /// Concatinate train string with info from additional columns
        /// </summary>
        /// <param name="fileStrings"></param>
        /// <param name="iColumn"></param>
        /// <param name="addColumns"></param>
        private void ConcatTrainStrings(List<string[]> fileStrings, int iColumn, List<int> addColumns)
        {
            for (int iRow = 1; iRow < fileStrings.Count - 1; iRow++)
            {
                string[] columnStrings = fileStrings[iRow];
                foreach (int addCol in addColumns)
                {
                    string addCols = columnStrings[addCol];
                    if (!String.IsNullOrEmpty(addCols))
                    {
                        columnStrings[iColumn] = String.Concat(columnStrings[iColumn], " ", addCols);
                    }
                }
            }
        }

        /// <summary>
        /// GetPlayerTrain : extract player train from list of all available trains
        /// </summary>
        /// <param name="allTrains"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TTTrainInfo GetPlayerTrain(ref List<TTTrainInfo> allTrains, string[] arguments)
        {
            TTTrainInfo reqTrain = null;

            string[] playerTrainDetails = arguments[1].Split(':');

            // loop through all trains to find player train
            int playerIndex = -1;

            for (int iTrain = 0; iTrain <= allTrains.Count - 1 && playerIndex < 0; iTrain++)
            {
                if (String.Compare(allTrains[iTrain].Name, playerTrainDetails[1]) == 0 &&
                    String.Compare(allTrains[iTrain].TTDescription, playerTrainDetails[0]) == 0)
                {
                    playerIndex = iTrain;
                }
            }

            if (playerIndex >= 0)
            {
                reqTrain = allTrains[playerIndex];
                allTrains.RemoveAt(playerIndex);
            }
            else
            {
                throw new InvalidDataException("Player train : " + arguments[1] + " not found in timetables");
            }

            return (reqTrain);
        }

        /// <summary>
        /// Reduce AI trains : reduce set of AI trains based on player train and arguments
        /// </summary>
        /// <param name="allTrains"></param>
        /// <param name="playerTrain"></param>
        /// <param name="arguments"></param>
        private List<AITrain> ReduceAITrains(List<TTTrainInfo> allTrains, TTTrainInfo playerTrain, string[] arguments)
        {
            List<AITrain> trainList = new List<AITrain>();

            foreach (TTTrainInfo reqTrain in allTrains)
            {
                // create train route
                AIPath usedPath = new AIPath(Paths[TrainRouteXRef[reqTrain.Index]]);
                reqTrain.AITrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);
                reqTrain.AITrain.Path = usedPath;
                reqTrain.AITrain.CreateRoute(false);  // create route without use of FrontTDBtraveller
                reqTrain.AITrain.ValidRoute[0] = new Train.TCSubpathRoute(reqTrain.AITrain.TCRoute.TCRouteSubpaths[0]);
                reqTrain.AITrain.AITrainDirectionForward = true;

                // process stops
                reqTrain.ConvertStops(simulator, reqTrain.AITrain, reqTrain.Name);

                // process commands
                if (reqTrain.TrainCommands.Count > 0)
                {
                    reqTrain.ProcessCommands(simulator, reqTrain.AITrain);
                }

                // add AI train to output list
                trainList.Add(reqTrain.AITrain);
            }

            // process dispose commands
            foreach (TTTrainInfo reqTrain in allTrains)
            {
                if (reqTrain.DisposeDetails != null)
                {
                    reqTrain.ProcessDisposeInfo(trainList, playerTrain);
                }
            }

            return (trainList);
        }

        /// <summary>
        /// Extract and initialize player train
        /// contains extracted train plus additional info for identification and selection
        /// </summary>
        private void PreInitPlayerTrain(TTTrainInfo reqTrain)
        {
            // set player train idents
            Train playerTrain = reqTrain.Train;

            playerTrain.TrainType = Train.TRAINTYPE.PLAYER;
            playerTrain.Number = 0;
            playerTrain.ControlMode = Train.TRAIN_CONTROL.AUTO_NODE;

            // create traveller
            AIPath usedPath = Paths[TrainRouteXRef[reqTrain.Index]];
            playerTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);

            // define style of passing path
            simulator.Signals.UseLocationPassingPaths = true;
        }

        /// <summary>
        /// Extract and initialize player train
        /// contains extracted train plus additional info for identification and selection
        /// </summary>
        private Train InitializePlayerTrain(TTTrainInfo reqTrain)
        {
            // set player train idents
            Train playerTrain = reqTrain.Train;

            playerTrain.TrainType = Train.TRAINTYPE.PLAYER;
            playerTrain.ControlMode = Train.TRAIN_CONTROL.AUTO_NODE;

            // create traveller
            AIPath usedPath = Paths[TrainRouteXRef[reqTrain.Index]];
            playerTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);

            // extract train path
            playerTrain.SetRoutePath(usedPath, simulator.Signals);
            playerTrain.ValidRoute[0] = new Train.TCSubpathRoute(playerTrain.TCRoute.TCRouteSubpaths[0]);

            bool canPlace = true;
            Train.TCSubpathRoute tempRoute = playerTrain.CalculateInitialTrainPosition(ref canPlace);
            if (tempRoute.Count == 0 || !canPlace)
            {
                throw new InvalidDataException("Player train original position not clear");
            }

            // initate train position
            playerTrain.SetInitialTrainRoute(tempRoute);
            playerTrain.CalculatePositionOfCars(0);
            playerTrain.ResetInitialTrainRoute(tempRoute);

            playerTrain.CalculatePositionOfCars(0);
            simulator.Trains.Add(playerTrain);

            // set player locomotive
            foreach (TrainCar car in playerTrain.Cars)
            {
                if (car.IsDriveable)  // first loco is the one the player drives
                {
                    simulator.PlayerLocomotive = car;
                    playerTrain.LeadLocomotive = car;
                    break;
                }
            }

            // reset train for each car
            foreach (TrainCar car in playerTrain.Cars)
            {
                car.Train = playerTrain;
            }

            if (simulator.PlayerLocomotive == null)
                throw new InvalidDataException("Can't find player locomotive in " + reqTrain.Name);

            // initialize brakes
            playerTrain.AITrainBrakePercent = 100;
            playerTrain.InitializeBrakes();

            // Note the initial position to be stored by a Save and used in Menu.exe to calculate DistanceFromStartM 
            simulator.InitialTileX = playerTrain.FrontTDBTraveller.TileX + (playerTrain.FrontTDBTraveller.X / 2048);
            simulator.InitialTileZ = playerTrain.FrontTDBTraveller.TileZ + (playerTrain.FrontTDBTraveller.Z / 2048);

            // set stops
            reqTrain.ConvertStops(simulator, playerTrain, reqTrain.Name);
            playerTrain.StationStops.Sort();

            // process commands
            if (reqTrain.TrainCommands.Count > 0)
            {
                reqTrain.ProcessCommands(simulator, reqTrain.Train);
            }

            // set activity details
            simulator.ClockTime = reqTrain.StartTime;
            simulator.ActivityFileName = reqTrain.TTDescription + "_" + reqTrain.Name;

            return (playerTrain);
        }

        /// <summary>
        /// Pre-process all routes : read routes and convert to AIPath structure
        /// </summary>
        public void PreProcessRoutes()
        {

            // extract names
            List<string> routeNames = new List<string>();

            foreach (KeyValuePair<string, AIPath> thisRoute in Paths)
            {
                routeNames.Add(thisRoute.Key);
            }

            // clear routes - will be refilled
            Paths.Clear();

            // create routes
            foreach (string thisRoute in routeNames)
            {
                // read route
                AIPath newPath = new AIPath(simulator.TDB, simulator.TSectionDat, thisRoute);
                Paths.Add(thisRoute, newPath);
            }
        }

        /// <summary>
        /// class TTTrainInfo
        /// contains extracted train plus additional info for identification and selection
        /// </summary>

        private class TTTrainInfo
        {
            public AITrain AITrain;     // build both TRAIN and AITRAIN as it is not yet known if train is AI, Player or Static
            public Train Train;
            public string Name;
            public int StartTime;
            public string TTDescription;
            public int columnIndex;
            public string Direction;
            public bool validTrain = true;
            public Dictionary<string, StopInfo> Stops = new Dictionary<string, StopInfo>();
            public int Index;
            public List<TTTrainCommands> TrainCommands = new List<TTTrainCommands>();
            public DisposeInfo DisposeDetails = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="trainName"></param>
            /// <param name="simulator"></param>
            /// <param name="ttfilename"></param>
            public TTTrainInfo(int icolumn, string trainName, Simulator simulator, int index)
            {
                Name = String.Copy(trainName);
                AITrain = new AITrain(simulator);
                Train = new Train(simulator);
                columnIndex = icolumn;
                Index = index;
            }

            /// <summary>
            /// Build train from info in single column
            /// </summary>
            /// <param name="fileStrings"></param>
            /// <param name="RowInfo"></param>
            /// <param name="pathRow"></param>
            /// <param name="consistRow"></param>
            /// <param name="startRow"></param>
            /// <param name="description"></param>
            /// <param name="stationNames"></param>
            /// <param name="ttInfo"></param>
            public void BuildTrain(List<string[]> fileStrings, rowType[] RowInfo, int pathRow, int consistRow, int startRow, int disposeRow, string description,
                Dictionary<int, string> stationNames, TimetableInfo ttInfo)
            {
                TTDescription = string.Copy(description);

                // set name
                AITrain.Name = Name + ":" + TTDescription;
                Train.Name = Name + ":" + TTDescription;

                // derive various directory paths
                string pathDirectory = Path.Combine(ttInfo.simulator.RoutePath, "Paths");
                string pathFilefull = Path.Combine(pathDirectory, fileStrings[pathRow][columnIndex]);

                string trainsDirectory = Path.Combine(ttInfo.simulator.BasePath, "Trains");
                string consistDirectory = Path.Combine(trainsDirectory, "Consists");
                string consistFilefull = Path.Combine(consistDirectory, fileStrings[consistRow][columnIndex]);

                string trainsetDirectory = Path.Combine(trainsDirectory, "trainset");

                // extract path
                string pathExtension = Path.GetExtension(pathFilefull);
                if (String.IsNullOrEmpty(pathExtension))
                    pathFilefull = Path.ChangeExtension(pathFilefull, "pat");
                ttInfo.TrainRouteXRef.Add(Index, pathFilefull);    // set reference to path

                if (!ttInfo.Paths.ContainsKey(pathFilefull))
                {
                    ttInfo.Paths.Add(pathFilefull, null);  // insert name in dictionary, path will be loaded later
                }

                // build consist
                BuildConsist(consistFilefull, trainsetDirectory, ttInfo.simulator);

                // derive starttime
                string startString = fileStrings[startRow][columnIndex];

                TimeSpan startingTime;
                bool validTime = TimeSpan.TryParse(startString, out startingTime);

                if (validTime)
                {
                    AITrain.StartTime = Convert.ToInt32(startingTime.TotalSeconds);
                    StartTime = AITrain.StartTime;
                }
                else
                {
                    Trace.TraceInformation("Invalid starttime {0} for train {1}, train not included", startString, AITrain.Name);
                    validTrain = false;
                }

                // process dispose info
                if (disposeRow > 0)
                {
                    string disposeString = fileStrings[disposeRow][columnIndex].ToLower().Trim();
                    if (!String.IsNullOrEmpty(disposeString))
                    {
                        if (String.Compare(disposeString.Substring(0, 6), "$forms") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeString, Train.FormCommand.TerminationFormed);
                        }
                        else if (String.Compare(disposeString.Substring(0, 9), "$triggers") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeString, Train.FormCommand.TerminationTriggered);
                        }
                        else if (String.Compare(disposeString.Substring(0, 6), "$static") == 0)
                        {
                            DisposeDetails = new DisposeInfo();
                        }
                    }
                }

                // derive station stops and other info

                for (int iRow = 1; iRow <= fileStrings.Count - 1; iRow++)
                {
                    switch (RowInfo[iRow])
                    {
                        case rowType.directionInfo:
                            Direction = fileStrings[iRow][columnIndex];
                            break;

                        case rowType.stationInfo:
                            string stationName = stationNames[iRow].ToLower();
                            if (!String.IsNullOrEmpty(fileStrings[iRow][columnIndex]))
                            {
                                Stops.Add(stationName, ProcessStopInfo(fileStrings[iRow][columnIndex], stationName));
                            }
                            break;

                        case rowType.trainNotesInfo:
                            if (!String.IsNullOrEmpty(fileStrings[iRow][columnIndex]))
                            {
                                string[] commandStrings = fileStrings[iRow][columnIndex].Split('$');
                                foreach (string thisCommand in commandStrings)
                                {
                                    if (!String.IsNullOrEmpty(thisCommand))
                                    {
                                        TrainCommands.Add(new TTTrainCommands(thisCommand));
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            /// <summary>
            /// Build train consist
            /// </summary>
            /// <param name="consistFile"></param>
            /// <param name="trainsetDirectory"></param>
            /// <param name="simulator"></param>
            public void BuildConsist(string consistFile, string trainsetDirectory, Simulator simulator)
            {
                string pathExtension = Path.GetExtension(consistFile);
                if (String.IsNullOrEmpty(pathExtension))
                    consistFile = Path.ChangeExtension(consistFile, "con");

                if (consistFile.Contains("tilted"))
                {
                    AITrain.tilted = true;
                    Train.tilted = true;
                }

                CONFile conFile = new CONFile(consistFile);

                // add wagons
                AITrain.Length = 0.0f;
                Train.Length = 0.0f;

                foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
                {
                    string wagonFolder = Path.Combine(trainsetDirectory, wagon.Folder);
                    string wagonFilePath = Path.Combine(wagonFolder, wagon.Name + ".wag");

                    TrainCar car = null;

                    if (wagon.IsEngine)
                        wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                    if (!File.Exists(wagonFilePath))
                    {
                        Trace.TraceWarning("Ignored missing wagon {0} in consist {1}", wagonFilePath, consistFile);
                        continue;
                    }

                    try
                    {
                        car = RollingStock.Load(simulator, wagonFilePath);
                        car.Flipped = wagon.Flip;
                        AITrain.Cars.Add(car);
                        Train.Cars.Add(car);
                        car.Train = AITrain;
                        car.SignalEvent(Event.Pantograph1Up);
                        AITrain.Length += car.LengthM;
                        Train.Length += car.LengthM;
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(wagonFilePath, error));
                    }

                }// for each rail car

                if (AITrain.Cars.Count <= 0)
                {
                    Trace.TraceInformation("Empty consists for AI train - train removed");
                    validTrain = false;
                }

                // set train details
                AITrain.CheckFreight();
                Train.CheckFreight();

                if (conFile.Train.TrainCfg.MaxVelocity == null || conFile.Train.TrainCfg.MaxVelocity.A <= 0f)
                {
                    AITrain.TrainMaxSpeedMpS = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;
                    Train.TrainMaxSpeedMpS = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;
                }
                else
                {
                    AITrain.TrainMaxSpeedMpS = Math.Min((float)simulator.TRK.Tr_RouteFile.SpeedLimit, conFile.Train.TrainCfg.MaxVelocity.A);
                    Train.TrainMaxSpeedMpS = Math.Min((float)simulator.TRK.Tr_RouteFile.SpeedLimit, conFile.Train.TrainCfg.MaxVelocity.A);
                }
            }

            /// <summary>
            /// Process station stop info cell including possible commans
            /// Info may consist of :
            /// one or two time values (arr / dep time or pass time)
            /// commands
            /// time values and commands
            /// </summary>
            /// <param name="stationInfo"></param>
            /// <param name="stationName"></param>
            /// <returns> StopInfo structure</returns>
            public StopInfo ProcessStopInfo(string stationInfo, string stationName)
            {
                string[] arr_dep = new string[2] { String.Empty, String.Empty };
                string[] pass = new string[1] { String.Empty };

                string fullCommandString = String.Empty;

                if (stationInfo.Contains('$'))
                {
                    int commandseparator = stationInfo.IndexOf('$');
                    fullCommandString = stationInfo.Substring(commandseparator + 1);
                    stationInfo = stationInfo.Substring(0, commandseparator);
                }

                if (!String.IsNullOrEmpty(stationInfo))
                {
                    if (stationInfo.Contains('-'))
                    {
                        arr_dep = stationInfo.Split(new char[1] { '-' }, 2);
                    }
                    else
                    {
                        arr_dep[0] = String.Copy(stationInfo);
                        arr_dep[1] = String.Copy(stationInfo);
                    }
                }

                StopInfo newStop = new StopInfo(stationName, arr_dep[0], arr_dep[1]);

                if (!String.IsNullOrEmpty(fullCommandString))
                {
                    newStop.Commands = new List<TTTrainCommands>();
                    string[] commandStrings = fullCommandString.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        newStop.Commands.Add(new TTTrainCommands(thisCommand));
                    }
                }

                return (newStop);
            }

            /// <summary>
            /// Convert station stops to train stationStop info
            /// </summary>
            /// <param name="simulator"></param>
            /// <param name="actTrain"></param>
            /// <param name="name"></param>
            public void ConvertStops(Simulator simulator, Train actTrain, string name)
            {
                foreach (KeyValuePair<string, StopInfo> stationStop in Stops)
                {
                    if (actTrain.TCRoute.StationXRef.ContainsKey(stationStop.Key))
                    {
                        StopInfo stationInfo = stationStop.Value;
                        int[] platformInfo = actTrain.TCRoute.StationXRef[stationStop.Key];
                        bool ValidStop = stationInfo.BuildStopInfo(actTrain, platformInfo[2], simulator.Signals);
                        if (!ValidStop)
                        {
                            Trace.TraceInformation("Station {0} not found for train {1} ", stationStop.Key, Name);
                        }
                        actTrain.TCRoute.StationXRef.Remove(stationStop.Key);
                    }
                }
                actTrain.TCRoute.StationXRef.Clear();  // info no longer required
            }

            /// <summary>
            /// Process Timetable commands entered as general notes
            /// All commands are valid from start of route
            /// </summary>
            /// <param name="simulator"></param>
            /// <param name="actTrain"></param>
            public void ProcessCommands(Simulator simulator, Train actTrain)
            {
                foreach (TTTrainCommands thisCommand in TrainCommands)
                {
                    actTrain.ProcessTimetableStopCommands(thisCommand, 0, -1);
                }
            }

            public void ProcessDisposeInfo(List<AITrain> trainList, TTTrainInfo playerTrain)
            {
                // train forms other train
                if (DisposeDetails.FormType == Train.FormCommand.TerminationFormed || DisposeDetails.FormType == Train.FormCommand.TerminationTriggered)
                {
                    // extract name
                    string[] otherTrainName = DisposeDetails.FormedTrain.Split('='); // extract train name
                    if (otherTrainName[1].Contains('/'))
                    {
                        int splitPosition = otherTrainName[1].IndexOf('/');
                        otherTrainName[1] = otherTrainName[1].Substring(0, splitPosition);
                    }

                    if (!otherTrainName[1].Contains(':'))
                    {
                        string[] timetableName = AITrain.Name.Split(':');
                        otherTrainName[1] = String.Concat(otherTrainName[1], ":", timetableName[1]);
                    }

                    // search train
                    bool trainFound = false;

                    foreach (AITrain otherTrain in trainList)
                    {
                        if (String.Compare(otherTrain.Name, otherTrainName[1], true) == 0)
                        {
                            AITrain.Forms = otherTrain.Number;
                            otherTrain.FormedOf = AITrain.Number;
                            otherTrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            break;
                        }
                    }

                    // if train not found, check for player train
                    if (!trainFound && String.Compare(playerTrain.Train.Name, otherTrainName[1], true) == 0)
                    {
                        playerTrain.Train.FormedOf = AITrain.Number;
                        playerTrain.Train.FormedOfType = DisposeDetails.FormType;
                        trainFound = true;
                    }

#if DEBUG_TRACEINFO
                    if (trainFound)
                    {
                        Trace.TraceInformation("Dispose : {0} {1} {2} ", AITrain.Name, DisposeDetails.FormType.ToString(), otherTrainName[1]);
                    }
                    else
                    {
                        Trace.TraceInformation("Dispose : {0} : cannot find {1} ", AITrain.Name, otherTrainName[1]);
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Class to hold stop info
        /// </summary>
        private class StopInfo
        {
            public string StopName;
            public int arrivalTime;
            public int departureTime;
            public DateTime arrivalDT;
            public DateTime departureDT;
            public bool arrdepvalid;
            //          public int passageTime;   // not yet implemented
            //          public bool passvalid;    // not yet implemented
            public List<TTTrainCommands> Commands;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name"></param>
            /// <param name="arrTime"></param>
            /// <param name="depTime"></param>
            public StopInfo(string name, string arrTime, string depTime)
            {
                arrivalTime = -1;
                departureTime = -1;
                Commands = null;

                TimeSpan atime;
                bool validArrTime = false;
                bool validDepTime = false;

                validArrTime = TimeSpan.TryParse(arrTime, out atime);
                if (validArrTime)
                {
                    arrivalTime = Convert.ToInt32(atime.TotalSeconds);
                    arrivalDT = new DateTime(atime.Ticks);
                }

                validDepTime = TimeSpan.TryParse(depTime, out atime);
                if (validDepTime)
                {
                    departureTime = Convert.ToInt32(atime.TotalSeconds);
                    departureDT = new DateTime(atime.Ticks);
                }

                arrdepvalid = (validArrTime || validDepTime);

                StopName = String.Copy(name.ToLower());
            }

            /// <summary>
            /// Build station stop info
            /// </summary>
            /// <param name="actTrain"></param>
            /// <param name="TDB"></param>
            /// <param name="signalRef"></param>
            /// <returns>bool (indicating stop is found on route)</returns>
            public bool BuildStopInfo(Train actTrain, int actPlatformID, Signals signalRef)
            {
                bool validStop = false;

                // valid stop
                if (arrdepvalid)
                {
                    // create station stop info
                    validStop = actTrain.CreateStationStop(actPlatformID, arrivalTime, departureTime, arrivalDT, departureDT, 15.0f);

                    // process additional commands
                    if (Commands != null && validStop)
                    {
                        int sectionIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].TCSectionIndex;
                        int subrouteIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].SubrouteIndex;

                        foreach (TTTrainCommands thisCommand in Commands)
                        {
                            actTrain.ProcessTimetableStopCommands(thisCommand, subrouteIndex, sectionIndex);
                        }
                    }
                }

                // stop used to define command only - find related section in route
                else if (Commands != null)
                {
                    // get platform details
                    int platformIndex;
                    int actSubpath = 0;

                    if (signalRef.PlatformXRefList.TryGetValue(actPlatformID, out platformIndex))
                    {
                        PlatformDetails thisPlatform = signalRef.PlatformDetailsList[platformIndex];
                        int sectionIndex = thisPlatform.TCSectionIndex[0];
                        int routeIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath].GetRouteIndex(sectionIndex, 0);

                        // if first section not found in route, try last

                        if (routeIndex < 0)
                        {
                            sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                            routeIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath].GetRouteIndex(sectionIndex, 0);
                        }

                        // if neither section found - try next subroute - keep trying till found or out of subroutes

                        while (routeIndex < 0 && actSubpath < (actTrain.TCRoute.TCRouteSubpaths.Count - 1))
                        {
                            actSubpath++;
                            Train.TCSubpathRoute thisRoute = actTrain.TCRoute.TCRouteSubpaths[actSubpath];
                            routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);

                            // if first section not found in route, try last

                            if (routeIndex < 0)
                            {
                                sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                                routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);
                            }
                        }

                        // if section found : process stop
                        if (routeIndex >= 0)
                        {
                            validStop = true;

                            sectionIndex = actTrain.TCRoute.TCRouteSubpaths[actSubpath][routeIndex].TCSectionIndex;
                            foreach (TTTrainCommands thisCommand in Commands)
                            {
                                actTrain.ProcessTimetableStopCommands(thisCommand, actSubpath, sectionIndex);
                            }
                        }
                    }
                }

                return (validStop);
            } // end buildStopInfo

        } // end class stopInfo

        private class DisposeInfo
        {
            public string FormedTrain;
            public Train.FormCommand FormType;
            public bool FormTrain;
            public bool FormStatic;

            public DisposeInfo(string formedTrain, Train.FormCommand formType)
            {
                FormedTrain = String.Copy(formedTrain);
                FormType = formType;
                FormTrain = true;
                FormStatic = false;
            }

            public DisposeInfo()
            {
                FormTrain = false;
                FormStatic = true;
            }
        }// end class DisposeInfo

    } // end class TimetableInfo

    /// <summary>
    /// Class to hold all additional commands in unprocessed form
    /// </summary>
    /// 
    public class TTTrainCommands
    {
        public string CommandToken;
        public List<string> CommandValues;
        public List<TTTrainComQualifiers> CommandQualifiers;

        /// <summary>
        /// Constructor from string (excludes leading '$')
        /// </summary>
        /// <param name="CommandString"></param>
        public TTTrainCommands(string CommandString)
        {
            string workString = String.Copy(CommandString.ToLower());
            string restString = String.Empty;

            // extract command token and values
            if (workString.Contains('='))
            {
                int splitPosition = workString.IndexOf('=');
                CommandToken = workString.Substring(0, splitPosition);
                restString = workString.Substring(splitPosition + 1);
            }
            else
            {
                CommandToken = String.Copy(workString.Trim());
            }

            // check for qualifiers

            string commandValueString;
            if (restString.Contains('/'))
            {
                string[] tempStrings = restString.Split('/');  // first string is value, rest is qualifiers
                commandValueString = String.Copy(tempStrings[0]);
                if (CommandQualifiers == null) CommandQualifiers = new List<TTTrainComQualifiers>();

                for (int iQual = 1; iQual < tempStrings.Length; iQual++)
                {
                    CommandQualifiers.Add(new TTTrainComQualifiers(tempStrings[iQual]));
                }
            }
            else
            {
                commandValueString = String.Copy(restString);
            }

            // process values
            // split on "+" sign (multiple values)
            string[] valueStrings = null;

            if (String.IsNullOrEmpty(commandValueString))
            {
                CommandValues = null;
            }
            else
            {
                CommandValues = new List<string>();

                if (commandValueString.Contains('+'))
                {
                    valueStrings = commandValueString.Split('+');
                }
                else
                {
                    valueStrings = new string[1] { commandValueString };
                }

                foreach (string thisValue in valueStrings)
                {
                    CommandValues.Add(thisValue.Trim());
                }
            }
        }

        /// <summary>
        /// Class for command qualifiers
        /// </summary>
        public class TTTrainComQualifiers
        {
            public string QualifierName;
            public List<string> QualifierValues = new List<string>();

            /// <summary>
            /// Constructor (string is without leading '/')
            /// </summary>
            /// <param name="qualifier"></param>
            public TTTrainComQualifiers(string qualifier)
            {
                string[] qualparts = null;
                if (qualifier.Contains(':'))
                {
                    qualparts = qualifier.Split(':');
                }
                else
                {
                    qualparts = new string[1] { qualifier };
                }

                QualifierName = String.Copy(qualparts[0]);

                for (int iQualValue = 1; iQualValue < qualparts.Length; iQualValue++)
                {
                    QualifierValues.Add(String.Copy(qualparts[iQualValue]).Trim());
                }
            }

        } // end class TTTrainComQualifiers
    } // end class TTTrainCommands
}

