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

using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Parsers.OR;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;

namespace Orts.Simulation.Timetables
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

        Dictionary<string, AIPath> Paths = new Dictionary<string, AIPath>();                                  // original path referenced by path name
        Dictionary<int, string> TrainRouteXRef = new Dictionary<int, string>();                               // path name referenced from train index    

        public bool BinaryPaths = false;

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
        public List<TTTrain> ProcessTimetable(string[] arguments, CancellationToken cancellation)
        {
            TTTrain reqPlayerTrain;

            bool loadPathNoFailure = true;
            List<TTTrain> trainList = new List<TTTrain>();
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
                var fileContents = new TimetableReader(filePath);

#if DEBUG_TIMETABLE
                File.AppendAllText(@"C:\temp\timetableproc.txt", "\nProcessing file : " + filePath + "\n");
#endif

                // convert to train info
                indexcount = ConvertFileContents(fileContents, simulator.Signals, ref trainInfoList, indexcount, filePath);
            }

            // read and pre-process routes

            Trace.Write(" TTROUTES:" + Paths.Count.ToString() + " ");

            loadPathNoFailure = PreProcessRoutes(cancellation);

            Trace.Write(" TTTRAINS:" + trainInfoList.Count.ToString() + " ");

            // get startinfo for player train
            playerTrain = GetPlayerTrain(ref trainInfoList, arguments);

            // pre-init player train to abstract alternative paths if set
            if (playerTrain != null)
            {
                PreInitPlayerTrain(playerTrain);
            }

            // reduce trainlist using player train info and parameters
            bool addPathNoLoadFailure;
            trainList = ReduceAITrains(trainInfoList, playerTrain, arguments, out addPathNoLoadFailure);
            if (!addPathNoLoadFailure) loadPathNoFailure = false;

            // set references (required to process commands)
            foreach (Train thisTrain in trainList)
            {
                if (simulator.NameDictionary.ContainsKey(thisTrain.Name.ToLower()))
                {
                    Trace.TraceWarning("Train : " + thisTrain.Name + " : duplicate name");
                }
                else
                {
                    simulator.TrainDictionary.Add(thisTrain.Number, thisTrain);
                    simulator.NameDictionary.Add(thisTrain.Name.ToLower(), thisTrain);
                }
            }

            // set player train
            reqPlayerTrain = null;
            if (playerTrain != null)
            {
                if (playerTrain.DisposeDetails != null)
                {
                    addPathNoLoadFailure = playerTrain.ProcessDisposeInfo(ref trainList, null, simulator);
                    if (!addPathNoLoadFailure) loadPathNoFailure = false;
                }

                reqPlayerTrain = InitializePlayerTrain(playerTrain, ref Paths, ref trainList);
                simulator.TrainDictionary.Add(reqPlayerTrain.Number, reqPlayerTrain);
                simulator.NameDictionary.Add(reqPlayerTrain.Name.ToLower(), reqPlayerTrain);
            }

            // process additional commands for all extracted trains
            reqPlayerTrain.FinalizeTimetableCommands();
            reqPlayerTrain.StationStops.Sort();

            foreach (TTTrain thisTrain in trainList)
            {
                thisTrain.FinalizeTimetableCommands();
                thisTrain.StationStops.Sort();
            }

            // set timetable identification for simulator for saves etc.
            simulator.TimetableFileName = Path.GetFileNameWithoutExtension(arguments[0]);

            if (!loadPathNoFailure)
            {
                Trace.TraceError("Load path failures");
            }

            trainList.Insert(0, reqPlayerTrain);
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
                    TimetableGroupFile multiInfo = new TimetableGroupFile(filePath, fileDirectory);
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
        private int ConvertFileContents(TimetableReader fileContents, Signals signalRef, ref List<TTTrainInfo> trainInfoList, int indexcount, string filePath)
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

            rowType[] RowInfo = new rowType[fileContents.Strings.Count];
            columnType[] ColInfo = new columnType[fileContents.Strings[0].Length];

            // process first row separately

            ColInfo[0] = columnType.stationInfo;

            for (int iColumn = 1; iColumn <= fileContents.Strings[0].Length - 1; iColumn++)
            {
                string columnDef = fileContents.Strings[0][iColumn];

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

            for (int iRow = 1; iRow <= fileContents.Strings.Count - 1; iRow++)
            {

                string rowDef = fileContents.Strings[iRow][0];

                string[] rowCommands = null;
                if (rowDef.Contains('/'))
                {
                    rowCommands = rowDef.Split('/');
                    rowDef = rowCommands[0].Trim();
                }

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
                            if (rowCommands != null && rowCommands.Length >= 2 && String.Equals(rowCommands[1], "binary")) BinaryPaths = true;
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
                fileContents.Strings[firstCommentRow][firstCommentColumn] : Path.GetFileNameWithoutExtension(fileContents.FilePath);

            // extract additional station info

            for (int iRow = 1; iRow <= fileContents.Strings.Count - 1; iRow++)
            {
                if (RowInfo[iRow] == rowType.stationInfo)
                {
                    string[] columnStrings = fileContents.Strings[iRow];
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

            bool allCorrectBuild = true;

            for (int iColumn = 1; iColumn <= ColInfo.Length - 1; iColumn++)
            {
                if (ColInfo[iColumn] == columnType.trainDefinition)
                {
                    List<int> addColumns = null;
                    addTrainColumns.TryGetValue(iColumn, out addColumns);

                    if (addColumns != null)
                    {
                        ConcatTrainStrings(fileContents.Strings, iColumn, addColumns);
                    }

                    if (!trainInfo[iColumn].BuildTrain(fileContents.Strings, RowInfo, pathRow, consistRow, startRow, disposeRow, description, stationNames, this))
                    {
                        allCorrectBuild = false;
                    }
                }
            }

            if (!allCorrectBuild)
            {
                Trace.TraceError("Failed to build trains");
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
        private List<TTTrain> ReduceAITrains(List<TTTrainInfo> allTrains, TTTrainInfo playerTrain, string[] arguments, out bool allPathsLoaded)
        {
            allPathsLoaded = true;
            List<TTTrain> trainList = new List<TTTrain>();

            foreach (TTTrainInfo reqTrain in allTrains)
            {
                // create train route
                if (TrainRouteXRef.ContainsKey(reqTrain.Index) && Paths.ContainsKey(TrainRouteXRef[reqTrain.Index]))
                {
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
            }

            // process dispose commands
            foreach (TTTrainInfo reqTrain in allTrains)
            {
                if (reqTrain.DisposeDetails != null)
                {
                    bool pathsNoLoadFailure = reqTrain.ProcessDisposeInfo(ref trainList, playerTrain, simulator);
                    if (!pathsNoLoadFailure) allPathsLoaded = false;
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
            TTTrain playerTrain = reqTrain.AITrain;
            reqTrain.playerTrain = true;

            playerTrain.TrainType = Train.TRAINTYPE.INTENDED_PLAYER;
            playerTrain.Number = 0;
            playerTrain.ControlMode = Train.TRAIN_CONTROL.AUTO_NODE;

            // define style of passing path
            simulator.Signals.UseLocationPassingPaths = true;

            // create traveller
            AIPath usedPath = Paths[TrainRouteXRef[reqTrain.Index]];
            playerTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, usedPath);

            // extract train path
            playerTrain.SetRoutePath(usedPath, simulator.Signals);
            playerTrain.ValidRoute[0] = new Train.TCSubpathRoute(playerTrain.TCRoute.TCRouteSubpaths[0]);
        }

        /// <summary>
        /// Extract and initialize player train
        /// contains extracted train plus additional info for identification and selection
        /// </summary>
        private TTTrain InitializePlayerTrain(TTTrainInfo reqTrain, ref Dictionary<string, AIPath> paths, ref List<TTTrain> trainList)
        {
            // set player train idents
            TTTrain playerTrain = reqTrain.AITrain;

            simulator.Trains.Add(playerTrain);

            // reset train for each car

            int icar = 1;
            foreach (TrainCar car in playerTrain.Cars)
            {
                car.Train = playerTrain;
                car.CarID = String.Concat(playerTrain.Number.ToString("0###"), "_", icar.ToString("0##"));
                icar++;
            }

            // set player locomotive
            // first test first and last cars - if either is drivable, use it as player locomotive
            int lastIndex = playerTrain.Cars.Count - 1;

            if (playerTrain.Cars[0].IsDriveable)
            {
                simulator.PlayerLocomotive = playerTrain.LeadLocomotive = playerTrain.Cars[0];
            }
            else if (playerTrain.Cars[lastIndex].IsDriveable)
            {
                simulator.PlayerLocomotive = playerTrain.LeadLocomotive = playerTrain.Cars[lastIndex];
            }
            else
            {
                foreach (TrainCar car in playerTrain.Cars)
                {
                    if (car.IsDriveable)  // first loco is the one the player drives
                    {
                        simulator.PlayerLocomotive = playerTrain.LeadLocomotive = car;
                        playerTrain.leadLocoAntiSlip = ((MSTSLocomotive)car).AntiSlip;
                        break;
                    }
                }
            }

            if (simulator.PlayerLocomotive == null)
                throw new InvalidDataException("Can't find player locomotive in " + reqTrain.Name);

            // initialize brakes
            playerTrain.AITrainBrakePercent = 100;
            playerTrain.InitializeBrakes();

            // set stops
            reqTrain.ConvertStops(simulator, playerTrain, reqTrain.Name);

            // process commands
            if (reqTrain.TrainCommands.Count > 0)
            {
                reqTrain.ProcessCommands(simulator, reqTrain.AITrain);
            }

            // set activity details
            simulator.ClockTime = reqTrain.StartTime;
            simulator.ActivityFileName = reqTrain.TTDescription + "_" + reqTrain.Name;

            // if train is created before start time, create train as intended player train
            if (playerTrain.StartTime != playerTrain.ActivateTime)
            {
                playerTrain.TrainType = Train.TRAINTYPE.INTENDED_PLAYER;
                playerTrain.FormedOf = -1;
                playerTrain.FormedOfType = TTTrain.FormCommand.Created;
            }

            return (playerTrain);
        }

        /// <summary>
        /// Pre-process all routes : read routes and convert to AIPath structure
        /// </summary>
        public bool PreProcessRoutes(CancellationToken cancellation)
        {

            // extract names
            List<string> routeNames = new List<string>();

            foreach (KeyValuePair<string, AIPath> thisRoute in Paths)
            {
                routeNames.Add(thisRoute.Key);
            }

            // clear routes - will be refilled
            Paths.Clear();
            bool allPathsLoaded = true;

            // create routes
            foreach (string thisRoute in routeNames)
            {
                // read route
                bool pathValid = true;
                AIPath newPath = LoadPath(thisRoute, out pathValid);
                if (!pathValid) allPathsLoaded = false;
                if (cancellation.IsCancellationRequested)
                    return (false);
            }

            return (allPathsLoaded);
        }

        public AIPath LoadPath(string pathstring, out bool validPath)
        {
            validPath = true;

            string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
            string formedpathFilefull = Path.Combine(pathDirectory, pathstring);
            string pathExtension = Path.GetExtension(formedpathFilefull);

            if (String.IsNullOrEmpty(pathExtension))
                formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

            // try to load binary path if required
            bool binaryloaded = false;
            AIPath outPath = null;

            if (Paths.ContainsKey(formedpathFilefull))
            {
                outPath = new AIPath(Paths[formedpathFilefull]);
            }
            else
            {
                string formedpathFilefullBinary = Path.Combine(Path.GetDirectoryName(formedpathFilefull), "OpenRails");
                formedpathFilefullBinary = Path.Combine(formedpathFilefullBinary, Path.GetFileNameWithoutExtension(formedpathFilefull));
                formedpathFilefullBinary = Path.ChangeExtension(formedpathFilefullBinary, "or-binpat");

                if (BinaryPaths)
                {
                    if (File.Exists(formedpathFilefullBinary))
                    {
                        try
                        {
                            var infpath = new BinaryReader(new FileStream(formedpathFilefullBinary, FileMode.Open, FileAccess.Read));
                            outPath = new AIPath(simulator.TDB, simulator.TSectionDat, infpath);
                            infpath.Close();
                            Paths.Add(formedpathFilefull, new AIPath(outPath));
                            binaryloaded = true;
                        }
                        catch
                        {
                            binaryloaded = false;
                        }
                    }
                }

                if (!binaryloaded)
                {
                    {
                        try
                        {
                            outPath = new AIPath(simulator.TDB, simulator.TSectionDat, formedpathFilefull, simulator.TimetableMode, simulator.orRouteConfig);
                        }
                        catch (Exception e)
                        {
                            validPath = false;
                            Trace.TraceInformation(new FileLoadException(formedpathFilefull, e).ToString());
                        }

                        if (validPath)
                        {
                            if (!binaryloaded && BinaryPaths)
                            {
                                var outfpath = new BinaryWriter(new FileStream(formedpathFilefullBinary, FileMode.Create));
                                outPath.Save(outfpath);
                                outfpath.Close();
                            }
                            Paths.Add(formedpathFilefull, new AIPath(outPath));
                        }
                    }
                }
            }

            return (outPath);
        }

        /// <summary>
        /// class TTTrainInfo
        /// contains extracted train plus additional info for identification and selection
        /// </summary>

        private class TTTrainInfo
        {
            public TTTrain AITrain;
            public string Name;
            public int StartTime;
            public string TTDescription;
            public int columnIndex;
            public string Direction;
            public bool validTrain = true;
            public bool playerTrain = false;
            public Dictionary<string, StopInfo> Stops = new Dictionary<string, StopInfo>();
            public int Index;
            public List<TTTrainCommands> TrainCommands = new List<TTTrainCommands>();
            public DisposeInfo DisposeDetails = null;

            public readonly TimetableInfo parentInfo;

            public struct consistInfo
            {
                public string consistFile;
                public bool reversed;
            }


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
                AITrain = new TTTrain(simulator);
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
            public bool BuildTrain(List<string[]> fileStrings, rowType[] RowInfo, int pathRow, int consistRow, int startRow, int disposeRow, string description,
                Dictionary<int, StationInfo> stationNames, TimetableInfo ttInfo)
            {
                TTDescription = string.Copy(description);

                // set name

                // if $static, set starttime row to $static and create unique name
                if (Name.ToLower().Contains("$static"))
                {
                    fileStrings[startRow][columnIndex] = String.Copy("$static");

                    if (String.Equals(Name.Trim().Substring(0, 1), "$"))
                    {
                        string trainName = "S" + columnIndex.ToString().Trim();
                        AITrain.Name = trainName + ":" + TTDescription;
                    }
                    else
                    {
                        string[] nameParts = Name.Split('$');
                        AITrain.Name = nameParts[0];
                    }
                }
                else
                {
                    AITrain.Name = Name + ":" + TTDescription;
                }

                AITrain.MovementState = AIs.AITrain.AI_MOVEMENT_STATE.AI_STATIC;

                // derive various directory paths
                string pathDirectory = Path.Combine(ttInfo.simulator.RoutePath, "Paths");
                string pathFilefull = Path.Combine(pathDirectory, fileStrings[pathRow][columnIndex]);

                string trainsDirectory = Path.Combine(ttInfo.simulator.BasePath, "Trains");
                string consistDirectory = Path.Combine(trainsDirectory, "Consists");

                string consistdef = fileStrings[consistRow][columnIndex];
                List<consistInfo> consistdetails = ProcessConsistInfo(consistdef);

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

                bool returnValue = true;

                // build consist
                returnValue = BuildConsist(consistdetails, trainsetDirectory, consistDirectory, ttInfo.simulator);

                // return if consist could not be loaded
                if (!returnValue) return (returnValue);

                // derive starttime
                string startString = fileStrings[startRow][columnIndex].ToLower().Trim();

                // if static, set starttime to 1 second and activate time to null (train is never activated)
                if (String.Equals(startString, "$static"))
                {
                    AITrain.StartTime = 1;
                    AITrain.ActivateTime = null;
                }
                // extract starttime
                else
                {
                    string[] startparts;
                    string startTimeString = String.Empty;
                    string activateTimeString = String.Empty;
                    bool created = false;
                    string createAhead = String.Empty;
                    bool startNextNight = false;

                    // process qualifier if set
                    if (startString.Contains('$'))
                    {
                        startparts = startString.Split('$');
                        activateTimeString = startparts[0].Trim();

                        if (startparts.Length > 1)
                        {
                            string command = startparts[1].Trim();
                            if (command.Contains('/'))
                            {
                                command = command.Substring(0, command.IndexOf('/')).Trim();
                            }
                            if (command.Contains('='))
                            {
                                command = command.Substring(0, command.IndexOf('=')).Trim();
                            }

                            switch (command)
                            {
                                // check for create - syntax : $create [=starttime] [/aheadof = train]
                                case "create":
                                    created = true;
                                    string[] timeparts = null;

                                    if (startparts[1].Contains('/'))
                                    {
                                        timeparts = startparts[1].Trim().Split('/');
                                    }
                                    else
                                    {
                                        timeparts = new string[1];
                                        timeparts[0] = startparts[1];
                                    }

                                    // check starttime
                                    if (timeparts[0].Contains('='))
                                    {
                                        string[] defparts = timeparts[0].Trim().Split('=');
                                        startTimeString = defparts[1].Trim();
                                    }
                                    else
                                    {
                                        startTimeString = "00:00:00";
                                    }

                                    // check train ahead
                                    if (timeparts.Length > 1 && timeparts[1].Trim().Length > 5 && String.Equals(timeparts[1].Trim().Substring(0, 5), "ahead") && timeparts[1].Contains('='))
                                    {
                                        string[] aheadparts = timeparts[1].Split('=');
                                        createAhead = String.Copy(aheadparts[1].Trim());
                                    }
                                    break;

                                // check for $previous : set special flag to start after midnight
                                case "next":
                                    startNextNight = true;
                                    startTimeString = startparts[0].Trim();
                                    activateTimeString = startparts[0].Trim();
                                    break;

                                // invalid command
                                default:
                                    Trace.TraceInformation("Train : " + Name + " invalid command for start value : " + command + "\n");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        startTimeString = startString;
                        activateTimeString = startString;
                    }

                    TimeSpan startingTime;
                    bool validSTime = TimeSpan.TryParse(startTimeString, out startingTime);
                    TimeSpan activateTime;
                    bool validATime = TimeSpan.TryParse(activateTimeString, out activateTime);

                    if (validSTime && validATime)
                    {
                        AITrain.StartTime = Math.Max(Convert.ToInt32(startingTime.TotalSeconds), 1);
                        AITrain.ActivateTime = Math.Max(Convert.ToInt32(activateTime.TotalSeconds), 1);
                        AITrain.Created = created;

                        // trains starting after midnight
                        if (startNextNight && AITrain.StartTime.HasValue)
                        {
                            AITrain.StartTime = AITrain.StartTime.Value + (24 * 3600);
                        }

                        if (created && !String.IsNullOrEmpty(createAhead))
                        {
                            if (!createAhead.Contains(':'))
                            {
                                AITrain.CreateAhead = createAhead + ":" + TTDescription;
                            }
                            else
                            {
                                AITrain.CreateAhead = createAhead;
                            }
                            AITrain.CreateAhead = AITrain.CreateAhead.ToLower();
                        }
                        StartTime = AITrain.ActivateTime.Value;
                    }
                    else
                    {
                        Trace.TraceInformation("Invalid starttime {0} for train {1}, train not included", startString, AITrain.Name);
                        validTrain = false;
                    }
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
                            DisposeDetails = new DisposeInfo(disposeCommands, TTTrain.FormCommand.TerminationFormed);
                        }
                        else if (String.Compare(disposeCommands.CommandToken, "$triggers") == 0)
                        {
                            DisposeDetails = new DisposeInfo(disposeCommands, TTTrain.FormCommand.TerminationTriggered);
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
                                if (Stops.ContainsKey(stationDetails.StationName))
                                {
                                    Trace.TraceInformation("Double station reference : train " + Name + " ; station : " + stationDetails.StationName);
                                }
                                else
                                {
                                    Stops.Add(stationDetails.StationName, ProcessStopInfo(fileStrings[iRow][columnIndex], stationDetails));
                                }
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

                return (true);
            }

            public List<consistInfo> ProcessConsistInfo(string consistDef)
            {
                List<consistInfo> consistDetails = new List<consistInfo>();
                string consistProc = String.Copy(consistDef).Trim();

                while (!String.IsNullOrEmpty(consistProc))
                {
                    if (consistProc.Substring(0, 1).Equals("<"))
                    {
                        int endIndex = consistProc.IndexOf('>');
                        if (endIndex < 0)
                        {
                            Trace.TraceWarning("Incomplete consist definition : \">\" character missing : {0}", consistProc);
                            consistInfo thisConsist = new consistInfo();
                            thisConsist.consistFile = String.Copy(consistProc.Substring(1));
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                            consistProc = String.Empty;
                        }
                        else
                        {
                            consistInfo thisConsist = new consistInfo();
                            thisConsist.consistFile = String.Copy(consistProc.Substring(1, endIndex - 1));
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                            consistProc = consistProc.Substring(endIndex + 1).Trim();
                        }
                    }
                    else if (consistProc.Substring(0, 1).Equals("$"))
                    {
                        if (consistProc.Substring(1, 7).Equals("reverse"))
                        {
                            if (consistDetails.Count > 0)
                            {
                                consistInfo thisConsist = consistDetails[consistDetails.Count - 1];
                                consistDetails.RemoveAt(consistDetails.Count - 1);
                                thisConsist.reversed = true;
                                consistDetails.Add(thisConsist);
                            }
                            else
                            {
                                Trace.TraceInformation("Invalid conmand at start of consist string {0}, command ingored", consistProc);
                            }
                            consistProc = consistProc.Substring(8).Trim();
                        }
                        else
                        {
                            Trace.TraceWarning("Invalid command in consist string : {0}", consistProc);
                            consistProc = String.Empty;
                        }
                    }
                    else
                    {
                        int plusIndex = consistProc.IndexOf('+');
                        if (plusIndex == 0)
                        {
                            consistProc = consistProc.Substring(1).Trim();
                        }
                        else if (plusIndex > 0)
                        {
                            consistInfo thisConsist = new consistInfo();
                            thisConsist.consistFile = String.Copy(consistProc.Substring(0, plusIndex - 1).Trim());

                            int sepIndex = thisConsist.consistFile.IndexOf('$');
                            if (sepIndex > 0)
                            {
                                consistProc = String.Concat(thisConsist.consistFile.Substring(sepIndex).Trim(), consistProc.Substring(plusIndex).Trim());
                                thisConsist.consistFile = thisConsist.consistFile.Substring(0, sepIndex - 1).Trim();
                            }
                            else
                            {
                                consistProc = consistProc.Substring(plusIndex + 1).Trim();
                            }
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                        }
                        else
                        {
                            consistInfo thisConsist = new consistInfo();
                            thisConsist.consistFile = String.Copy(consistProc);

                            int sepIndex = consistProc.IndexOf('$');
                            if (sepIndex > 0)
                            {
                                thisConsist.consistFile = consistProc.Substring(0, sepIndex - 1).Trim();
                                consistProc = consistProc.Substring(sepIndex).Trim();
                            }
                            else
                            {
                                consistProc = String.Empty;
                            }
                            thisConsist.reversed = false;
                            consistDetails.Add(thisConsist);
                        }
                    }
                }

                return (consistDetails);
            }

            /// <summary>
            /// Build train consist
            /// </summary>
            /// <param name="consistFile">Defined consist file</param>
            /// <param name="trainsetDirectory">Consist directory</param>
            /// <param name="simulator">Simulator</param>

            public bool BuildConsist(List<consistInfo> consistSets, string trainsetDirectory, string consistDirectory, Simulator simulator)
            {
                AITrain.IsTilting = true;

                float? confMaxSpeed = null;

                foreach (consistInfo consistDetails in consistSets)
                {
                    bool consistReverse = consistDetails.reversed;
                    string consistFile = Path.Combine(consistDirectory, consistDetails.consistFile);

                    string pathExtension = Path.GetExtension(consistFile);
                    if (String.IsNullOrEmpty(pathExtension))
                        consistFile = Path.ChangeExtension(consistFile, "con");

                    if (!consistFile.Contains("tilted"))
                    {
                        AITrain.IsTilting = false;
                    }

                    ConsistFile conFile = null;

                    // try to load config file, exit if failed
                    try
                    {
                        conFile = new ConsistFile(consistFile);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceInformation("Reading " + consistFile.ToString() + " : " + e.ToString());
                        return (false);
                    }

                    // add wagons
                    List<TrainCar> cars = AddWagons(conFile, trainsetDirectory, simulator, consistReverse);

                    // add wagons
                    AITrain.Length = 0.0f;
                    int carId = 0;

                    foreach (TrainCar car in cars)
                    {
                        AITrain.Cars.Add(car);
                        car.Train = AITrain;
                        car.CarID = String.Concat(AITrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                        carId++;
                        car.SignalEvent(Event.Pantograph1Up);
                        AITrain.Length += car.CarLengthM;
                    }

                    // derive speed

                    if (conFile.Train.TrainCfg.MaxVelocity != null && conFile.Train.TrainCfg.MaxVelocity.A > 0)
                    {
                        if (confMaxSpeed.HasValue)
                        {
                            confMaxSpeed = Math.Min(confMaxSpeed.Value, conFile.Train.TrainCfg.MaxVelocity.A);
                        }
                        else
                        {
                            confMaxSpeed = Math.Min((float)simulator.TRK.Tr_RouteFile.SpeedLimit, conFile.Train.TrainCfg.MaxVelocity.A);
                        }
                    }
                }

                if (AITrain.Cars.Count <= 0)
                {
                    Trace.TraceInformation("Empty consists for AI train - train removed");
                    validTrain = false;
                }

                // set train details
                AITrain.CheckFreight();

                if (!confMaxSpeed.HasValue || confMaxSpeed.Value <= 0f)
                {
                    AITrain.TrainMaxSpeedMpS = (float)simulator.TRK.Tr_RouteFile.SpeedLimit;

                    float tempMaxSpeedMpS = AITrain.TrainMaxSpeedMpS;

                    foreach (TrainCar car in AITrain.Cars)
                    {
                        float engineMaxSpeedMpS = 0;
                        if (car is MSTSLocomotive)
                            engineMaxSpeedMpS = (car as MSTSLocomotive).MaxSpeedMpS;
                        if (car is MSTSElectricLocomotive)
                            engineMaxSpeedMpS = (car as MSTSElectricLocomotive).MaxSpeedMpS;
                        if (car is MSTSDieselLocomotive)
                            engineMaxSpeedMpS = (car as MSTSDieselLocomotive).MaxSpeedMpS;
                        if (car is MSTSSteamLocomotive)
                            engineMaxSpeedMpS = (car as MSTSSteamLocomotive).MaxSpeedMpS;

                        if (engineMaxSpeedMpS > 0)
                        {
                            tempMaxSpeedMpS = Math.Min(tempMaxSpeedMpS, engineMaxSpeedMpS);
                        }
                    }

                    AITrain.TrainMaxSpeedMpS = tempMaxSpeedMpS;
                }
                else
                {
                    AITrain.TrainMaxSpeedMpS = confMaxSpeed.Value;
                }

                return (true);
            }

            /// <summary>
            /// Add wagons from consist file to traincar list
            /// </summary>
            /// <param name="consistFile">Processed consist File</param>
            /// <param name="trainsetDirectory">Consist Directory</param>
            /// <param name="simulator">Simulator</param>
            /// <returns>Generated TrainCar list</returns>

            public List<TrainCar> AddWagons(Orts.Formats.Msts.ConsistFile consistFile, string trainsDirectory, Simulator simulator, bool consistReverse)
            {
                List<TrainCar> cars = new List<TrainCar>();

                // add wagons
                AITrain.Length = 0.0f;

                foreach (Wagon wagon in consistFile.Train.TrainCfg.WagonList)
                {
                    string wagonFolder = Path.Combine(trainsDirectory, wagon.Folder);
                    string wagonFilePath = Path.Combine(wagonFolder, wagon.Name + ".wag");

                    TrainCar car = null;

                    if (wagon.IsEngine)
                        wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                    if (!File.Exists(wagonFilePath))
                    {
                        Trace.TraceWarning("Ignored missing wagon {0} in consist {1}", wagonFilePath, consistFile);
                        continue;
                    }

                    //try
                    //{
                    car = RollingStock.Load(simulator, wagonFilePath);
                    car.Flipped = wagon.Flip;

                    if (consistReverse)
                    {
                        car.Flipped = !car.Flipped;
                        cars.Insert(0, car);
                    }
                    else
                    {
                        cars.Add(car);
                    }
                    //}
                    //catch (Exception error)
                    //{
                    //    Trace.WriteLine(new FileLoadException(wagonFilePath, error));
                    //}

                }// for each rail car

                return (cars);
            }

            /// <summary>
            /// Process station stop info cell including possible commands
            /// Info may consist of :
            /// one or two time values (arr / dep time or pass time)
            /// commands
            /// time values and commands
            /// </summary>
            /// <param name="stationInfo">Reference to station string</param>
            /// <param name="stationName">Station Details class</param>
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
                newStop.noWaitSignal = stationDetails.NoWaitSignal;

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

                // process terminal through station commands
                if (stationDetails.IsTerminal)
                {
                    if (newStop.Commands == null)
                    {
                        newStop.Commands = new List<TTTrainCommands>();
                    }

                    newStop.Commands.Add(new TTTrainCommands("terminal"));
                }

                return (newStop);
            }

            /// <summary>
            /// Convert station stops to train stationStop info
            /// </summary>
            /// <param name="simulator"></param>
            /// <param name="actTrain"></param>
            /// <param name="name"></param>
            public void ConvertStops(Simulator simulator, TTTrain actTrain, string name)
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
            public void ProcessCommands(Simulator simulator, TTTrain actAITrain)
            {
                foreach (TTTrainCommands thisCommand in TrainCommands)
                {
                    switch (thisCommand.CommandToken)
                    {
                        case "acc":
                            actAITrain.MaxAccelMpSSP = actAITrain.DefMaxAccelMpSSP * Convert.ToSingle(thisCommand.CommandValues[0]);
                            actAITrain.MaxAccelMpSSF = actAITrain.DefMaxAccelMpSSF * Convert.ToSingle(thisCommand.CommandValues[0]);
                            break;

                        case "dec":
                            actAITrain.MaxDecelMpSSP = actAITrain.DefMaxDecelMpSSP * Convert.ToSingle(thisCommand.CommandValues[0]);
                            actAITrain.MaxDecelMpSSF = actAITrain.DefMaxDecelMpSSF * Convert.ToSingle(thisCommand.CommandValues[0]);
                            break;

                        default:
                            actAITrain.ProcessTimetableStopCommands(thisCommand, 0, -1, -1, parentInfo);
                            break;
                    }
                }
            }

            public bool ProcessDisposeInfo(ref List<TTTrain> trainList, TTTrainInfo playerTrain, Simulator simulator)
            {
                bool loadPathNoFailure = true;
                TTTrain formedTrain = new TTTrain(simulator);
                TTTrain.FormCommand formtype = TTTrain.FormCommand.None;
                bool trainFound = false;

                // train forms other train
                if (DisposeDetails.FormType == TTTrain.FormCommand.TerminationFormed || DisposeDetails.FormType == TTTrain.FormCommand.TerminationTriggered)
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
                    foreach (TTTrain otherTrain in trainList)
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
                            AITrain.FormsAtStation = DisposeDetails.FormsAtStation;
                            otherTrain.FormedOf = AITrain.Number;
                            otherTrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = otherTrain;
                            break;
                        }
                    }

                    // if not found, try player train
                    if (!trainFound)
                    {
                        if (playerTrain != null && String.Compare(playerTrain.AITrain.Name, otherTrainName[1], true) == 0)
                        {
                            if (playerTrain.AITrain.FormedOf >= 0)
                            {
                                Trace.TraceWarning("Train : {0} : dispose details : formed train {1} already formed out of another train",
                                    AITrain.Name, playerTrain.Name);
                            }

                            AITrain.Forms = playerTrain.AITrain.Number;
                            AITrain.SetStop = DisposeDetails.SetStop;
                            AITrain.FormsAtStation = DisposeDetails.FormsAtStation;
                            playerTrain.AITrain.FormedOf = AITrain.Number;
                            playerTrain.AITrain.FormedOfType = DisposeDetails.FormType;
                            trainFound = true;
                            formedTrain = playerTrain.AITrain;
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

                TTTrain outTrain = null;
                TTTrain inTrain = null;

                // check if train must be stabled
                if (DisposeDetails.Stable && (trainFound || DisposeDetails.FormStatic))
                {
                    // save final train
                    int finalForms = AITrain.Forms;

                    // create outbound train (note : train is defined WITHOUT consist as it is formed of incoming train)
                    outTrain = new TTTrain(simulator);

                    bool addPathNoLoadFailure;
                    AIPath outPath = parentInfo.LoadPath(DisposeDetails.Stable_outpath, out addPathNoLoadFailure);
                    if (!addPathNoLoadFailure)
                    {
                        loadPathNoFailure = false;
                    }
                    else
                    {
                        outTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, outPath);
                        outTrain.Path = outPath;
                        outTrain.CreateRoute(false);
                        outTrain.ValidRoute[0] = new Train.TCSubpathRoute(outTrain.TCRoute.TCRouteSubpaths[0]);
                        outTrain.AITrainDirectionForward = true;
                        outTrain.StartTime = DisposeDetails.Stable_outtime;
                        outTrain.ActivateTime = DisposeDetails.Stable_outtime;
                        outTrain.Name = String.Concat("SO_", AITrain.Number.ToString("0000"));
                        outTrain.FormedOf = AITrain.Number;
                        outTrain.FormedOfType = TTTrain.FormCommand.TerminationFormed;
                        outTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                        trainList.Add(outTrain);

                        AITrain.Forms = outTrain.Number;
                    }

                    // if stable to static
                    if (DisposeDetails.FormStatic)
                    {
                        outTrain.FormsStatic = true;
                    }
                    else
                    {
                        outTrain.FormsStatic = false;

                        // create inbound train
                        inTrain = new TTTrain(simulator);

                        AIPath inPath = parentInfo.LoadPath(DisposeDetails.Stable_inpath, out addPathNoLoadFailure);
                        if (!addPathNoLoadFailure)
                        {
                            loadPathNoFailure = false;
                        }
                        else
                        {
                            inTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, inPath);
                            inTrain.Path = inPath;
                            inTrain.CreateRoute(false);
                            inTrain.ValidRoute[0] = new Train.TCSubpathRoute(inTrain.TCRoute.TCRouteSubpaths[0]);
                            inTrain.AITrainDirectionForward = true;
                            inTrain.StartTime = DisposeDetails.Stable_intime;
                            inTrain.ActivateTime = DisposeDetails.Stable_intime;
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
                            formedTrain.FormedOfType = TTTrain.FormCommand.TerminationFormed;

                            Train.TCSubpathRoute lastSubpath = inTrain.TCRoute.TCRouteSubpaths[inTrain.TCRoute.TCRouteSubpaths.Count - 1];
                            if (inTrain.FormedOfType == TTTrain.FormCommand.TerminationTriggered && formedTrain.Number != 0) // no need to set consist for player train
                            {
                                bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);
                                BuildStabledConsist(ref inTrain, formedTrain.Cars, formedTrain.TCRoute.TCRouteSubpaths[0], reverseTrain);
                            }
                        }
                    }
                }
                // if run round required, build runround

                if (formtype == TTTrain.FormCommand.TerminationFormed && trainFound && DisposeDetails.RunRound)
                {
                    TTTrain usedTrain;
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

                    bool addPathNoLoadFailure = BuildRunRound(ref usedTrain, attachTo, atStart, DisposeDetails, simulator, ref trainList);
                    if (!addPathNoLoadFailure) loadPathNoFailure = false;
                }

                if (DisposeDetails.FormStatic)
                {
                    AITrain.FormsStatic = true;
                }

                return (loadPathNoFailure);
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
            public bool BuildRunRound(ref TTTrain rrtrain, int attachTo, bool atStart, DisposeInfo disposeDetails, Simulator simulator, ref List<TTTrain> trainList)
            {
                bool loadPathNoFailure = true;
                TTTrain formedTrain = new TTTrain(simulator);

                string pathDirectory = Path.Combine(simulator.RoutePath, "Paths");
                string formedpathFilefull = Path.Combine(pathDirectory, DisposeDetails.RunRoundPath);
                string pathExtension = Path.GetExtension(formedpathFilefull);
                if (String.IsNullOrEmpty(pathExtension))
                    formedpathFilefull = Path.ChangeExtension(formedpathFilefull, "pat");

                bool addPathNoLoadFailure;
                AIPath formedPath = parentInfo.LoadPath(formedpathFilefull, out addPathNoLoadFailure);
                if (!addPathNoLoadFailure)
                {
                    loadPathNoFailure = false;
                }
                else
                {
                    formedTrain.RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, formedPath);
                    formedTrain.Path = formedPath;
                    formedTrain.CreateRoute(false);
                    formedTrain.ValidRoute[0] = new Train.TCSubpathRoute(formedTrain.TCRoute.TCRouteSubpaths[0]);
                    formedTrain.AITrainDirectionForward = true;
                    formedTrain.Name = String.Concat("RR_", rrtrain.Number.ToString("0000"));
                    formedTrain.FormedOf = rrtrain.Number;
                    formedTrain.FormedOfType = TTTrain.FormCommand.Detached;
                    formedTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                    formedTrain.AttachTo = attachTo;
                    trainList.Add(formedTrain);

                    Train.TCSubpathRoute lastSubpath = rrtrain.TCRoute.TCRouteSubpaths[rrtrain.TCRoute.TCRouteSubpaths.Count - 1];
                    if (atStart) lastSubpath = rrtrain.TCRoute.TCRouteSubpaths[0]; // if runround at start use first subpath

                    bool reverseTrain = CheckFormedReverse(lastSubpath, formedTrain.TCRoute.TCRouteSubpaths[0]);

                    if (atStart)
                    {
                        int? rrtime = disposeDetails.RunRoundTime;
                        DetachInfo detachDetails = new DetachInfo(true, false, false, 0, false, false, true, -1, rrtime, formedTrain.Number, reverseTrain);
                        rrtrain.DetachDetails.Add(detachDetails);
                    }
                    else
                    {
                        DetachInfo detachDetails = new DetachInfo(false, true, false, 0, false, false, true, -1, null, formedTrain.Number, reverseTrain);
                        rrtrain.DetachDetails.Add(detachDetails);
                    }
                }

                return (loadPathNoFailure);
            }

            /// <summary>
            /// Build consist for stabled train from final train
            /// </summary>
            /// <param name="stabledTrain"></param>
            /// <param name="cars"></param>
            /// <param name="trainRoute"></param>
            private void BuildStabledConsist(ref TTTrain stabledTrain, List<TrainCar> cars, Train.TCSubpathRoute trainRoute, bool reverseTrain)
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
                    int carId = 0;
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = String.Concat(stabledTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                        carId++;
                        stabledTrain.Cars.Add(car);
                    }
                }
                else
                {
                    int carId = 0;
                    foreach (TrainCar car in cars)
                    {
                        car.Train = stabledTrain;
                        car.CarID = String.Concat(stabledTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                        carId++;
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
            public bool noWaitSignal;
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
            public bool BuildStopInfo(TTTrain actTrain, int actPlatformID, Signals signalRef)
            {
                bool validStop = false;

                // valid stop
                if (arrdepvalid)
                {
                    // check if terminal flag is set
                    bool terminal = false;

                    if (Commands != null)
                    {
                        foreach (TTTrainCommands thisCommand in Commands)
                        {
                            if (thisCommand.CommandToken.Equals("terminal"))
                            {
                                terminal = true;
                            }
                        }
                    }

                    // create station stop info
                    validStop = actTrain.CreateStationStop(actPlatformID, arrivalTime, departureTime, arrivalDT, departureDT, 15.0f, terminal);

                    // override holdstate using stop info - but only if exit signal is defined

                    int exitSignal = actTrain.StationStops[actTrain.StationStops.Count - 1].ExitSignal;
                    bool holdSignal = holdState != SignalHoldType.None && (exitSignal >= 0);
                    actTrain.StationStops[actTrain.StationStops.Count - 1].HoldSignal = holdSignal;

                    // override nosignalwait using stop info

                    actTrain.StationStops[actTrain.StationStops.Count - 1].NoWaitSignal = noWaitSignal;

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
            public bool NoWaitSignal;        // Train will run up to signal and not wait in platform
            public int? MinDwellTimeMins;    // Min Dwell time for Conditional Holdstate
            public bool IsTerminal;            // Station is terminal

            /// <summary>
            /// Constructor from String
            /// </summary>
            /// <param name="stationName"></param>
            public StationInfo(string stationString)
            {
                // default settings
                HoldState = HoldInfo.NoHold;
                NoWaitSignal = false;
                MinDwellTimeMins = null;
                IsTerminal = false;

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

                        case "nowaitsignal":
                            NoWaitSignal = true;
                            break;

                        case "terminal":
                            IsTerminal = true;
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
            public TTTrain.FormCommand FormType;
            public bool FormTrain;
            public bool FormStatic;
            public bool SetStop;
            public bool FormsAtStation;

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
            public DisposeInfo(TTTrainCommands formedTrainCommands, TTTrain.FormCommand formType)
            {
                FormedTrain = String.Copy(formedTrainCommands.CommandValues[0]);
                FormType = formType;
                FormTrain = true;
                FormStatic = false;
                Stable = false;
                RunRound = false;
                SetStop = false;
                FormsAtStation = false;

                if (formedTrainCommands.CommandQualifiers != null && formType == TTTrain.FormCommand.TerminationFormed)
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

                        if (String.Compare(formedTrainQualifiers.QualifierName, "atstation") == 0)
                        {
                            FormsAtStation = true;
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
                FormType = TTTrain.FormCommand.None;
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
                            FormType = TTTrain.FormCommand.TerminationFormed;
                            break;

                        case "triggers":
                            FormTrain = true;
                            FormedTrain = String.Copy(stableQualifier.QualifierValues[0]);
                            FormStatic = false;
                            FormType = TTTrain.FormCommand.TerminationTriggered;
                            break;

                        case "static":
                            FormTrain = false;
                            FormStatic = true;
                            FormType = TTTrain.FormCommand.None;
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
