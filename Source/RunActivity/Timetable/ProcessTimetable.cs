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
    public class TimetableInfo
    {
        private Simulator simulator;

        private enum columnType
        {
            stationInfo,
            addStationInfo,
            comment,
            trainDefinition,
            trainAddInfo,
            invalid,
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
            invalid,
        }

        Dictionary<string, AIPath> Paths = new Dictionary<string, AIPath>();                 // original path referenced by path name
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
                indexcount = ConvertFileContents(fileContents, simulator.Signals, ref trainInfoList, indexcount, filePath);
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
                reqPlayerTrain = InitializePlayerTrain(playerTrain, ref Paths);
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
        private int ConvertFileContents(TTContents fileContents, Signals signalRef, ref List<TTTrainInfo> trainInfoList, int indexcount, string filePath)
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
            Dictionary<int, StationInfo> stationNames = new Dictionary<int, StationInfo>();          // key int = row no, value string = station name

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

                // oheck for invalid command definition
                else if (columnDef.Substring(0, 1).Equals("#"))
                {
                    Trace.TraceWarning("Invalid column definition in {0} : column {1} : {2}", filePath, iColumn, columnDef);
                    ColInfo[iColumn] = columnType.invalid;
                }

                // otherwise it is a train definition
                else
                {
                    ColInfo[iColumn] = columnType.trainDefinition;
                    trainHeaders.Add(iColumn, String.Copy(columnDef));
                    trainInfo.Add(iColumn, new TTTrainInfo(iColumn, columnDef, simulator, indexcount, this));
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
                            if (rowDef.Substring(0, 1).Equals("#"))
                            {
                                Trace.TraceWarning("Invalid row definition in {0} : {1}", filePath, rowDef);
                                RowInfo[iRow] = rowType.invalid;
                            }
                            else
                            {
                                RowInfo[iRow] = rowType.stationInfo;
                                stationNames.Add(iRow, new StationInfo(String.Copy(rowDef)));
                            }
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

            bool validFile = true;

            // check if all required row definitions are available
            if (consistRow < 0)
            {
                Trace.TraceWarning("File : {0} - Consist definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (pathRow < 0)
            {
                Trace.TraceWarning("File : {0} - Path definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (startRow < 0)
            {
                Trace.TraceWarning("File : {0} - Start definition row missing, file cannot be processed", filePath);
                validFile = false;
            }

            if (!validFile) return (indexcount); // abandone processing

            // extract description

            string description = (firstCommentRow >= 0 && firstCommentColumn >= 0) ?
                fileContents.trainStrings[firstCommentRow][firstCommentColumn] : Path.GetFileNameWithoutExtension(fileContents.TTfilename);

            // extract additional station info

            for (int iRow = 1; iRow <= fileContents.trainStrings.Count - 1; iRow++)
            {
                if (RowInfo[iRow] == rowType.stationInfo)
                {
                    string[] columnStrings = fileContents.trainStrings[iRow];
                    for (int iColumn = 1; iColumn <= ColInfo.Length - 1; iColumn++)
                    {
                        if (ColInfo[iColumn] == columnType.addStationInfo)
                        {
                            string[] stationCommands = columnStrings[iColumn].Split('$');
                            stationNames[iRow].ProcessStationCommands(stationCommands);
                        }
                    }
                }
            }

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
                    reqTrain.ProcessDisposeInfo(ref trainList, playerTrain, simulator);
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
        private Train InitializePlayerTrain(TTTrainInfo reqTrain, ref Dictionary<string, AIPath> paths)
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

            int icar = 1;
            foreach (TrainCar car in playerTrain.Cars)
            {
                car.Train = playerTrain;
                car.CarID = icar.ToString();
                icar++;
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
#if ACTIVITY_EDITOR
                AIPath newPath = new AIPath(simulator.TDB, simulator.TSectionDat, thisRoute, simulator.orRouteConfig);
#else
                AIPath newPath = new AIPath(simulator.TDB, simulator.TSectionDat, thisRoute);
#endif
                Paths.Add(thisRoute, newPath);
            }
        }

        public AIPath LoadPath(string pathstring)
        {
            string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
            string formedpathFilefull = Path.Combine(pathDirectory, pathstring);
            string pathExtension = Path.GetExtension(formedpathFilefull);
            if (String.IsNullOrEmpty(pathExtension))
                formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

            AIPath outPath = null;
            if (Paths.ContainsKey(formedpathFilefull))
            {
                outPath = new AIPath(Paths[formedpathFilefull]);
            }
            else
            {
#if ACTIVITY_EDITOR
                outPath = new AIPath(simulator.TDB, simulator.TSectionDat, formedpathFilefull, simulator.orRouteConfig);
#else
                outPath = new AIPath(simulator.TDB, simulator.TSectionDat, formedpathFilefull);
#endif
                Paths.Add(formedpathFilefull, new AIPath(outPath));
            }

            return (outPath);
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

            public readonly TimetableInfo parentInfo;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="trainName"></param>
            /// <param name="simulator"></param>
            /// <param name="ttfilename"></param>
            public TTTrainInfo(int icolumn, string trainName, Simulator simulator, int index, TimetableInfo thisParent)
            {
                parentInfo = thisParent;
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
                Dictionary<int, StationInfo> stationNames, TimetableInfo ttInfo)
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
                    StartTime = AITrain.StartTime.Value;
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
                        TTTrainCommands disposeCommands = new TTTrainCommands(disposeString);

                        if (String.Compare(disposeCommands.CommandToken, "$forms") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeCommands, Train.FormCommand.TerminationFormed);
                        }
                        else if (String.Compare(disposeCommands.CommandToken, "$triggers") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeCommands, Train.FormCommand.TerminationTriggered);
                        }
                        else if (String.Compare(disposeCommands.CommandToken, "$static") == 0)
                        {
                            DisposeDetails = new DisposeInfo();
                        }
                        else if (String.Compare(disposeCommands.CommandToken, "$stable") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeCommands);
                        }
                        else
                        {
                            Trace.TraceWarning("Invalid dispose string defined for train {0} : {1}",
                                AITrain.Name, disposeCommands.CommandToken);
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
                            StationInfo stationDetails = stationNames[iRow];
                            if (!String.IsNullOrEmpty(fileStrings[iRow][columnIndex]))
                            {
                                Stops.Add(stationDetails.StationName, ProcessStopInfo(fileStrings[iRow][columnIndex], stationDetails));
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
                        car.CarID = AITrain.Name.Split(':')[0];
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
            public StopInfo ProcessStopInfo(string stationInfo, StationInfo stationDetails)
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

                StopInfo newStop = new StopInfo(stationDetails.StationName, arr_dep[0], arr_dep[1], parentInfo);
                newStop.holdState = stationDetails.HoldState == StationInfo.HoldInfo.Hold ? StopInfo.SignalHoldType.Normal : StopInfo.SignalHoldType.None;

                if (!String.IsNullOrEmpty(fullCommandString))
                {
                    newStop.Commands = new List<TTTrainCommands>();
                    string[] commandStrings = fullCommandString.Split('$');
                    foreach (string thisCommand in commandStrings)
                    {
                        newStop.Commands.Add(new TTTrainCommands(thisCommand));
                    }
                }

                // process forced stop through station commands
                if (stationDetails.HoldState == StationInfo.HoldInfo.ForceHold)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("forcehold"));
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
                    actTrain.ProcessTimetableStopCommands(thisCommand, 0, -1, -1, parentInfo);
                }
            }

            public void ProcessDisposeInfo(ref List<AITrain> trainList, TTTrainInfo playerTrain, Simulator simulator)
            {
                var formedTrain = new Train(simulator);
                Train.FormCommand formtype = Train.FormCommand.None;
                bool trainFound = false;

                // train forms other train
                if (DisposeDetails.FormType == Train.FormCommand.TerminationFormed || DisposeDetails.FormType == Train.FormCommand.TerminationTriggered)
                {
                    formtype = DisposeDetails.FormType;
                    string[] otherTrainName = null;

                    // extract name
                    if (DisposeDetails.FormedTrain.Contains('='))
                    {
                        otherTrainName = DisposeDetails.FormedTrain.Split('='); // extract train name
                    }
                    else
                    {
                        otherTrainName = new string[2];
                        otherTrainName[1] = String.Copy(DisposeDetails.FormedTrain);
                    }

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
                    foreach (AITrain otherTrain in trainList)
                    {
                        if (String.Compare(otherTrain.Name, otherTrainName[1], true) == 0)
                        {
                            if (otherTrain.FormedOf >= 0)
                            {
                                Trace.TraceWarning("Train : {0} : dispose details : formed train {1} already formed out of another train",
                                    AITrain.Name, otherTrain.Name);
                                break;
                            }

                            AITrain.Forms = otherTrain.Number;
                            AITrain.SetStop = DisposeDetails.SetStop;
                            otherTrain.FormedOf = AITrain.Number;
                            otherTrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = otherTrain;
                            break;
                        }
                    }

                    // if train not found, check for player train
                    if (!trainFound && String.Compare(playerTrain.Train.Name, otherTrainName[1], true) == 0)
                    {
                        if (playerTrain.Train.FormedOf >= 0)
                        {
                            Trace.TraceWarning("Train : {0} : dispose details : formed train {1} already formed out of another train",
                                AITrain.Name, playerTrain.Train.Name);
                        }
                        else
                        {
                            AITrain.SetStop = DisposeDetails.SetStop;
                            playerTrain.Train.FormedOf = AITrain.Number;
                            playerTrain.Train.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = playerTrain.Train;
                        }
                    }

                    if (!trainFound)
                    {
                        Trace.TraceWarning("Train :  {0} : Dispose details : formed train {1} not found",
                            AITrain.Name, otherTrainName[1]);
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

                AITrain outTrain = null;
                AITrain inTrain = null;

                // check if train must be stabled
                if (DisposeDetails.Stable && (trainFound || DisposeDetails.FormStatic))
                {
                    // save final train
                    int finalForms = AITrain.Forms;

                    // create outbound train (note : train is defined WITHOUT consist as it is formed of incoming train)
                    outTrain = new AITrain(simulator);

                    AIPath outPath = parentInfo.LoadPath(DisposeDetails.Stable_outpath);

                    outTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, outPath);
                    outTrain.Path = outPath;
                    outTrain.CreateRoute(false);
                    outTrain.ValidRoute[0] = new Train.TCSubpathRoute(outTrain.TCRoute.TCRouteSubpaths[0]);
                    outTrain.AITrainDirectionForward = true;
                    outTrain.StartTime = DisposeDetails.Stable_outtime;
                    outTrain.Name = String.Concat("SO_", AITrain.Number.ToString("0000"));
                    outTrain.FormedOf = AITrain.Number;
                    outTrain.FormedOfType = Train.FormCommand.TerminationFormed;
                    outTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                    trainList.Add(outTrain);

                    AITrain.Forms = outTrain.Number;

                    // if stable to static
                    if (DisposeDetails.FormStatic)
                    {
                        outTrain.FormsStatic = true;
                    }
                    else
                    {
                        outTrain.FormsStatic = false;

                        // create inbound train
                        inTrain = new AITrain(simulator);

                        AIPath inPath = parentInfo.LoadPath(DisposeDetails.Stable_inpath);

                        inTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, inPath);
                        inTrain.Path = inPath;
                        inTrain.CreateRoute(false);
                        inTrain.ValidRoute[0] = new Train.TCSubpathRoute(inTrain.TCRoute.TCRouteSubpaths[0]);
                        inTrain.AITrainDirectionForward = true;
                        inTrain.StartTime = DisposeDetails.Stable_intime;
                        inTrain.Name = String.Concat("SI_", finalForms.ToString("0000"));
                        inTrain.FormedOf = outTrain.Number;
                        inTrain.FormedOfType = DisposeDetails.FormType; // set forms or triggered as defined in stable
                        inTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                        inTrain.Forms = finalForms;
                        inTrain.SetStop = DisposeDetails.SetStop;
                        inTrain.FormsStatic = false;
                        inTrain.Stable_CallOn = DisposeDetails.CallOn;

                        trainList.Add(inTrain);

                        outTrain.Forms = inTrain.Number;

                        formtype = inTrain.FormedOfType;

                        // set back reference from final train

                        formedTrain.FormedOf = inTrain.Number;
                        formedTrain.FormedOfType = Train.FormCommand.TerminationFormed;

                        Train.TCSubpathRoute lastSubpath = inTrain.TCRoute.TCRouteSubpaths[inTrain.TCRoute.TCRouteSubpaths.Count - 1];
                        if (inTrain.FormedOfType == Train.FormCommand.TerminationTriggered && formedTrain.Number != 0) // no need to set consist for player train
                        {
                            bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);
                            BuildStabledConsist(ref inTrain, formedTrain.Cars, formedTrain.TCRoute.TCRouteSubpaths[0], reverseTrain);
                        }
                    }
                }
                // if run round required, build runround (except if formed train is player train)

                if (formtype == Train.FormCommand.TerminationFormed && trainFound && DisposeDetails.RunRound && formedTrain.Number != 0)
                {
                    Train usedTrain;
                    bool atStart = false;  // indicates if run-round is to be performed before start of move or forms, or at end of move
                    int attachTo;

                    if (DisposeDetails.Stable)
                    {
                        switch (DisposeDetails.RunRoundPos)
                        {
                            case DisposeInfo.RunRoundPosition.outposition:
                                usedTrain = outTrain;
                                attachTo = usedTrain.Number;
                                atStart = true;
                                break;

                            case DisposeInfo.RunRoundPosition.inposition:
                                usedTrain = inTrain;
                                attachTo = formedTrain.Number;
                                atStart = false;
                                break;

                            default:
                                usedTrain = inTrain;
                                attachTo = inTrain.Number;
                                atStart = true;
                                break;
                        }
                    }
                    else
                    {
                        usedTrain = formedTrain;
                        attachTo = usedTrain.Number;
                        atStart = true;
                    }

                    BuildRunRound(ref usedTrain, attachTo, atStart, DisposeDetails, simulator, ref trainList);
                }

                if (DisposeDetails.FormStatic)
                {
                    AITrain.FormsStatic = true;
                }
            }

            /// <summary>
            /// Build run round details and train
            /// </summary>
            /// <param name="rrtrain"></param>
            /// <param name="atStart"></param>
            /// <param name="disposeDetails"></param>
            /// <param name="simulator"></param>
            /// <param name="trainList"></param>
            /// <param name="paths"></param>
            public void BuildRunRound(ref Train rrtrain, int attachTo, bool atStart, DisposeInfo disposeDetails, Simulator simulator, ref List<AITrain> trainList)
            {
                AITrain formedTrain = new AITrain(simulator);

                string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
                string formedpathFilefull = Path.Combine(pathDirectory, DisposeDetails.RunRoundPath);
                string pathExtension = Path.GetExtension(formedpathFilefull);
                if (String.IsNullOrEmpty(pathExtension))
                    formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

                AIPath formedPath = parentInfo.LoadPath(formedpathFilefull);

                formedTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, formedPath);
                formedTrain.Path = formedPath;
                formedTrain.CreateRoute(false);
                formedTrain.ValidRoute[0] = new Train.TCSubpathRoute(formedTrain.TCRoute.TCRouteSubpaths[0]);
                formedTrain.AITrainDirectionForward = true;
                formedTrain.Name = String.Concat("RR_", rrtrain.Number.ToString("0000"));
                formedTrain.FormedOf = rrtrain.Number;
                formedTrain.FormedOfType = Train.FormCommand.Detached;
                formedTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                formedTrain.AttachTo = attachTo;
                trainList.Add(formedTrain);

                Train.TCSubpathRoute lastSubpath = rrtrain.TCRoute.TCRouteSubpaths[rrtrain.TCRoute.TCRouteSubpaths.Count - 1];
                bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);

                if (atStart)
                {
                    int? rrtime = disposeDetails.RunRoundTime;
                    Train.DetachInfo detachDetails = new Train.DetachInfo(true, false, false, 0, false, false, true, -1, rrtime, formedTrain.Number, reverseTrain);
                    rrtrain.DetachDetails.Add(detachDetails);
                }
                else
                {
                    Train.DetachInfo detachDetails = new Train.DetachInfo(false, true, false, 0, false, false, true, -1, null, formedTrain.Number, reverseTrain);
                    rrtrain.DetachDetails.Add(detachDetails);
                }

            }

            /// <summary>
            /// Build consist for stabled train from final train
            /// </summary>
            /// <param name="stabledTrain"></param>
            /// <param name="cars"></param>
            /// <param name="trainRoute"></param>
            private void BuildStabledConsist(ref AITrain stabledTrain, List<TrainCar> cars, Train.TCSubpathRoute trainRoute, bool reverseTrain)
            {
                int totalreverse = 0;

                // check no. of reversals
                foreach (Train.TCReversalInfo reversalInfo in stabledTrain.TCRoute.ReversalInfo)
                {
                    if (reversalInfo.Valid) totalreverse++;
                }

                if (reverseTrain) totalreverse++;

                // copy consist in same or reverse direction
                if ((totalreverse % 2) == 0) // even number, so same direction
                {
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = stabledTrain.Name.Split(':')[0];
                        stabledTrain.Cars.Add(car);
                    }
                }
                else
                {
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = stabledTrain.Name.Split(':')[0];
                        car.Flipped = !car.Flipped;
                        stabledTrain.Cars.Insert(0, car);
                    }
                }
            }

            public bool CheckFormedReverse(Train.TCSubpathRoute thisTrainRoute, Train.TCSubpathRoute formedTrainRoute)
            {
                // get matching route sections to check on direction
                int lastElementIndex = thisTrainRoute.Count - 1;
                Train.TCRouteElement lastElement = thisTrainRoute[lastElementIndex];

                int firstElementIndex = formedTrainRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);

                while (firstElementIndex < 0 && lastElementIndex > 0)
                {
                    lastElementIndex--;
                    lastElement = thisTrainRoute[lastElementIndex];
                    firstElementIndex = formedTrainRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);
                }

                // if no matching sections found leave train without consist
                if (firstElementIndex < 0)
                {
                    return false;
                }

                Train.TCRouteElement firstElement = formedTrainRoute[firstElementIndex];

                // reverse required
                return (firstElement.Direction != lastElement.Direction);
            }
        }

        /// <summary>
        /// Class to hold stop info
        /// </summary>
        private class StopInfo
        {
            public enum SignalHoldType
            {
                None,
                Normal,
                Forced,
            }

            public string StopName;
            public int arrivalTime;
            public int departureTime;
            public DateTime arrivalDT;
            public DateTime departureDT;
            public bool arrdepvalid;
            public SignalHoldType holdState;
            //          public int passageTime;   // not yet implemented
            //          public bool passvalid;    // not yet implemented
            public List<TTTrainCommands> Commands;

            public TimetableInfo refTTInfo;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name"></param>
            /// <param name="arrTime"></param>
            /// <param name="depTime"></param>
            public StopInfo(string name, string arrTime, string depTime, TimetableInfo ttinfo)
            {
                refTTInfo = ttinfo;
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
                    int activeSubroute = 0;
                    int activeSubrouteNodeIndex = 0;
                    // create station stop info
                    validStop = actTrain.CreateStationStop(actPlatformID, arrivalTime, departureTime, arrivalDT, departureDT, 15.0f,
                        ref activeSubroute, ref activeSubrouteNodeIndex);

                    // override holdstate using stop info - but only if exit signal is defined

                    int exitSignal = actTrain.StationStops[actTrain.StationStops.Count - 1].ExitSignal;
                    bool holdSignal = holdState != SignalHoldType.None && (exitSignal >= 0);
                    actTrain.StationStops[actTrain.StationStops.Count - 1].HoldSignal = holdSignal;

                    // process additional commands
                    if (Commands != null && validStop)
                    {
                        int sectionIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].TCSectionIndex;
                        int subrouteIndex = actTrain.StationStops[actTrain.StationStops.Count - 1].SubrouteIndex;

                        foreach (TTTrainCommands thisCommand in Commands)
                        {
                            actTrain.ProcessTimetableStopCommands(thisCommand, subrouteIndex, sectionIndex, (actTrain.StationStops.Count - 1), refTTInfo);
                        }

                        holdSignal = actTrain.StationStops[actTrain.StationStops.Count - 1].HoldSignal;
                    }

                    // check holdsignal list

                    if (holdSignal)
                    {
                        if (!actTrain.HoldingSignals.Contains(exitSignal))
                        {
                            actTrain.HoldingSignals.Add(exitSignal);
                        }
                    }
                    else
                    {
                        if (actTrain.HoldingSignals.Contains(exitSignal))
                        {
                            actTrain.HoldingSignals.Remove(exitSignal);
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
                                actTrain.ProcessTimetableStopCommands(thisCommand, actSubpath, sectionIndex, -1, refTTInfo);
                            }
                        }
                    }
                }

                return (validStop);
            } // end buildStopInfo

        } // end class stopInfo

        private class StationInfo
        {
            public enum HoldInfo
            {
                Hold,
                NoHold,
                ForceHold,
                HoldConditional_DwellTime,
            }

            public string StationName;       // Station Name
            public HoldInfo HoldState;       // Hold State
            public int? MinDwellTimeMins;    // Min Dwell time for Conditional Holdstate

            /// <summary>
            /// Constructor from String
            /// </summary>
            /// <param name="stationName"></param>
            public StationInfo(string stationString)
            {
                // default settings
                HoldState = HoldInfo.NoHold;
                MinDwellTimeMins = null;

                // if string contains commands : split name and commands
                if (stationString.Contains("$"))
                {
                    string[] stationDetails = stationString.Split('$');
                    StationName = String.Copy(stationDetails[0]).ToLower().Trim();
                    ProcessStationCommands(stationDetails);
                }
                else
                // string contains name only
                {
                    StationName = String.Copy(stationString).ToLower().Trim();
                }
            }

            /// <summary>
            /// Process Station Commands : add command info to stationInfo class
            /// </summary>
            /// <param name="commands"></param>
            public void ProcessStationCommands(string[] commands)
            {
                // start at 1 as 0 is station name
                for (int iString = 1; iString <= commands.Length - 1; iString++)
                {
                    string commandFull = commands[iString];
                    TTTrainCommands thisCommand = new TTTrainCommands(commandFull);

                    switch (thisCommand.CommandToken)
                    {
                        case "hold":
                            HoldState = HoldInfo.Hold;
                            break;

                        case "nohold":
                            HoldState = HoldInfo.NoHold;
                            break;

                        case "forcehold":
                            HoldState = HoldInfo.ForceHold;
                            break;

                        // other commands not yet implemented
                        default:
                            break;
                    }
                }

            }

        }

        private class DisposeInfo
        {
            public string FormedTrain;
            public Train.FormCommand FormType;
            public bool FormTrain;
            public bool FormStatic;
            public bool SetStop;

            public bool Stable;
            public string Stable_outpath;
            public int? Stable_outtime;
            public string Stable_inpath;
            public int? Stable_intime;

            public bool RunRound;
            public string RunRoundPath;
            public int? RunRoundTime;

            public enum RunRoundPosition
            {
                inposition,
                stableposition,
                outposition,
            }

            public RunRoundPosition RunRoundPos;

            public bool CallOn;

            /// <summary>
            /// Constructor for FORMS and TRIGGERS
            /// </summary>
            /// <param name="formedTrain"></param>
            /// <param name="formType"></param>
            public DisposeInfo(TTTrainCommands formedTrainCommands, Train.FormCommand formType)
            {
                FormedTrain = String.Copy(formedTrainCommands.CommandValues[0]);
                FormType = formType;
                FormTrain = true;
                FormStatic = false;
                Stable = false;
                RunRound = false;
                SetStop = false;

                if (formedTrainCommands.CommandQualifiers != null && formType == Train.FormCommand.TerminationFormed)
                {
                    foreach (TTTrainCommands.TTTrainComQualifiers formedTrainQualifiers in formedTrainCommands.CommandQualifiers)
                    {
                        if (String.Compare(formedTrainQualifiers.QualifierName, "runround") == 0)
                        {
                            RunRound = true;
                            RunRoundPath = String.Copy(formedTrainQualifiers.QualifierValues[0]);
                            RunRoundTime = -1;
                        }

                        if (String.Compare(formedTrainQualifiers.QualifierName, "rrtime") == 0)
                        {
                            TimeSpan RRSpan;
                            TimeSpan.TryParse(formedTrainQualifiers.QualifierValues[0], out RRSpan);
                            RunRoundTime = Convert.ToInt32(RRSpan.TotalSeconds);
                        }

                        if (String.Compare(formedTrainQualifiers.QualifierName, "setstop") == 0)
                        {
                            SetStop = true;
                        }
                    }
                }
            }

            /// <summary>
            /// Constructor for STATIC
            /// </summary>
            public DisposeInfo()
            {
                FormTrain = false;
                FormStatic = true;
                Stable = false;
                RunRound = false;
                FormType = Train.FormCommand.None;
                SetStop = false;
            }

            /// <summary>
            /// Constructor for STABLE
            /// </summary>
            /// <param name="disposeString"></param>
            public DisposeInfo(TTTrainCommands stableCommands)
            {
                FormTrain = false;
                FormStatic = false;
                Stable = true;
                RunRound = false;
                CallOn = false;
                SetStop = true;

                foreach (TTTrainCommands.TTTrainComQualifiers stableQualifier in stableCommands.CommandQualifiers)
                {
                    switch (stableQualifier.QualifierName)
                    {
                        case "out_path":
                            Stable_outpath = String.Copy(stableQualifier.QualifierValues[0]);
                            break;

                        case "out_time":
                            TimeSpan outtime;
                            TimeSpan.TryParse(stableQualifier.QualifierValues[0], out outtime);
                            Stable_outtime = Convert.ToInt32(outtime.TotalSeconds);
                            break;

                        case "in_path":
                            Stable_inpath = String.Copy(stableQualifier.QualifierValues[0]);
                            break;

                        case "in_time":
                            TimeSpan intime;
                            TimeSpan.TryParse(stableQualifier.QualifierValues[0], out intime);
                            Stable_intime = Convert.ToInt32(intime.TotalSeconds);
                            break;

                        case "forms":
                            FormTrain = true;
                            FormedTrain = String.Copy(stableQualifier.QualifierValues[0]);
                            FormStatic = false;
                            FormType = Train.FormCommand.TerminationFormed;
                            break;

                        case "triggers":
                            FormTrain = true;
                            FormedTrain = String.Copy(stableQualifier.QualifierValues[0]);
                            FormStatic = false;
                            FormType = Train.FormCommand.TerminationTriggered;
                            break;

                        case "static":
                            FormTrain = false;
                            FormStatic = true;
                            FormType = Train.FormCommand.None;
                            break;

                        case "runround":
                            RunRound = true;
                            RunRoundPath = String.Copy(stableQualifier.QualifierValues[0]);
                            RunRoundTime = -1;
                            RunRoundPos = RunRoundPosition.stableposition;
                            break;

                        case "rrtime":
                            TimeSpan RRSpan;
                            TimeSpan.TryParse(stableQualifier.QualifierValues[0], out RRSpan);
                            RunRoundTime = Convert.ToInt32(RRSpan.TotalSeconds);
                            break;

                        case "callon":
                            CallOn = true;
                            break;

                        case "rrpos":
                            switch (stableQualifier.QualifierValues[0])
                            {
                                case "in":
                                    RunRoundPos = RunRoundPosition.inposition;
                                    break;

                                case "out":
                                    RunRoundPos = RunRoundPosition.outposition;
                                    break;

                                case "stable":
                                    RunRoundPos = RunRoundPosition.stableposition;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }
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
            string workString = String.Copy(CommandString).ToLower().Trim();
            string restString = String.Empty;
            string commandValueString = String.Empty;

            // check for qualifiers

            if (workString.Contains('/'))
            {
                string[] tempStrings = workString.Split('/');  // first string is token plus value, rest is qualifiers
                restString = String.Copy(tempStrings[0]);

                if (CommandQualifiers == null) CommandQualifiers = new List<TTTrainComQualifiers>();

                for (int iQual = 1; iQual < tempStrings.Length; iQual++)
                {
                    CommandQualifiers.Add(new TTTrainComQualifiers(tempStrings[iQual]));
                }
            }
            else
            {
                restString = String.Copy(workString);
            }

            // extract command token and values
            if (restString.Contains('='))
            {
                int splitPosition = restString.IndexOf('=');
                CommandToken = restString.Substring(0, splitPosition);
                commandValueString = restString.Substring(splitPosition + 1);
            }
            else
            {
                CommandToken = String.Copy(restString.Trim());
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
                if (qualifier.Contains('='))
                {
                    qualparts = qualifier.Split('=');
                }
                else
                {
                    qualparts = new string[1] { qualifier };
                }

                QualifierName = String.Copy(qualparts[0].Trim());

                for (int iQualValue = 1; iQualValue < qualparts.Length; iQualValue++)
                {
                    QualifierValues.Add(String.Copy(qualparts[iQualValue]).Trim());
                }
            }

        } // end class TTTrainComQualifiers
    } // end class TTTrainCommands
}

