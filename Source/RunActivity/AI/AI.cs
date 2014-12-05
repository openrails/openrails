// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* AI
 * 
 * Contains code to initialize and control AI trains.
 * Currently, AI trains are created at startup and moved down 1000 meters to make them
 * invisible.  This is done so the rendering code can discover the model it needs to draw.
 * 
 * 
 */

// 
// Flag to print deadlock info
// #define DEBUG_DEADLOCK
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTS.Formats;
using ORTS.MultiPlayer;
using ORTS.Scripting.Api;
using ORTS.Processes;

namespace ORTS
{
    public class AI
    {
        public readonly Simulator Simulator;
        public List<AITrain> AITrains = new List<AITrain>();// active AI trains

        public StartTrains StartList = new StartTrains(); // trains yet to be started
        public List<AITrain> AutoGenTrains = new List<AITrain>(); // auto-generated trains
        private double clockTime; // clock time : local time before activity start, common time from simulator after start
        private bool localTime;  // if true : clockTime is local time
        public bool PreUpdate; // if true : running in pre-update phase
        public List<AITrain> TrainsToRemove = new List<AITrain>();
        public List<AITrain> TrainsToAdd = new List<AITrain>();

        /// <summary>
        /// Loads AI train information from activity file.
        /// Creates a queue of AI trains in the order they should appear.
        /// At the moment AI trains are also created off scene so the rendering code will know about them.
        /// </summary>
        public AI(Simulator simulator, LoaderProcess loader, double activityStartTime)
        {
            Simulator = simulator;
#if WITH_PATH_DEBUG

            if (File.Exists(@"C:\temp\checkpath.txt"))
            {
                File.Delete(@"C:\temp\checkpath.txt");
            }


#endif
            if (simulator.Activity != null && simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition != null)
            {
                foreach (var sd in simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList)
                {
                    AITrain train = CreateAITrain(sd,
                    simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition.TrafficFile.TrafficDefinition);
                    if (loader.Terminated) // ping loader watchdog
                        return;
                }
            }

            // prerun trains
            PrerunAI(-1, null, loader);

            clockTime = Simulator.ClockTime;
            localTime = false;
        }

        // constructor for Timetable trains
        // trains allready have a number - must not be changed!
        public AI(Simulator simulator, List<AITrain> allTrains, double ClockTime, int playerTrainOriginalTrain, Train playerTrain, LoaderProcess loader)
        {
            Simulator = simulator;

            foreach (AITrain train in allTrains)
            {
                if (train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE)
                {
                    train.AI = this;
                    train.BrakeLine3PressurePSI = 0;
                    AutoGenTrains.Add(train);
                    Simulator.AutoGenDictionary.Add(train.Number, train);
                }

                // set train details
                else
                {
                    train.TrainType = Train.TRAINTYPE.AI_NOTSTARTED;
                    train.AI = this;

                    if (train.Cars.Count > 0) train.Cars[0].Headlight = 2;//AI train always has light on
                    train.BrakeLine3PressurePSI = 0;

                    // insert in start list
 //                   if (train.InitialSpeed > 0) train.InitializeMoving();
                    StartList.InsertTrain(train);
                    Simulator.StartReference.Add(train.Number);
                }
            }

            // clear dictionary (no trains yet exist)
            Simulator.TrainDictionary.Clear();
            Simulator.NameDictionary.Clear();

            // prerun trains
            PrerunAI(playerTrainOriginalTrain, playerTrain, loader);

            clockTime = Simulator.ClockTime;
            localTime = false;
        }

        // restore game state
        public AI(Simulator simulator, BinaryReader inf)
        {
            Debug.Assert(simulator.Trains != null, "Cannot restore AI without Simulator.Trains.");
            Simulator = simulator;

            int totalAITrains = inf.ReadInt32();

            for (int iTrain = 0; iTrain < totalAITrains; iTrain++)
            {
                AITrain aiTrain = new AITrain(Simulator, inf);
                aiTrain.AI = this;
                AITrains.Add(aiTrain);
                Simulator.Trains.Add(aiTrain);
                simulator.TrainDictionary.Add(aiTrain.Number, aiTrain);
                if (!Simulator.NameDictionary.ContainsKey(aiTrain.Name.ToLower()))
                    Simulator.NameDictionary.Add(aiTrain.Name.ToLower(), aiTrain);
            }

            int totalStarting = inf.ReadInt32();

            for (int iStarting = 0; iStarting < totalStarting; iStarting++)
            {
                AITrain aiTrain = new AITrain(Simulator, inf);
                aiTrain.AI = this;
                StartList.InsertTrain(aiTrain);
                Simulator.StartReference.Add(aiTrain.Number);
            }

            int totalAutoGen = inf.ReadInt32();

            for (int iAutoGen = 0; iAutoGen < totalAutoGen; iAutoGen++)
            {
                AITrain aiTrain = new AITrain(Simulator, inf);
                aiTrain.AI = this;
                AutoGenTrains.Add(aiTrain);
                Simulator.AutoGenDictionary.Add(aiTrain.Number, aiTrain);
            }
            if (Simulator.PlayerLocomotive.Train is AITrain) ((AITrain)Simulator.PlayerLocomotive.Train).AI = this;
        }

        // Restore in autopilot mode

        public AI (Simulator simulator, BinaryReader inf, bool autopilot)
        {
            Debug.Assert(simulator.Trains != null, "Cannot restore AI without Simulator.Trains.");
            Simulator = simulator;
            AITrain aiTrain = new AITrain(Simulator, inf);
            int PlayerLocomotiveIndex = inf.ReadInt32();
            if (PlayerLocomotiveIndex >=0) Simulator.PlayerLocomotive = aiTrain.Cars[PlayerLocomotiveIndex];
            Simulator.Trains.Add(aiTrain);
        }

        public AI(Simulator simulator)
        {
            Simulator = simulator;
        }

        // save game state
        public void Save(BinaryWriter outf)
        {

            RemoveTrains();   // remove trains waiting to be removed
            AddTrains();      // add trains waiting to be added
            outf.Write(AITrains.Count);
            foreach (AITrain train in AITrains)
            {
                train.Save(outf);
            }

            outf.Write(StartList.Count);
            foreach (AITrain thisStartTrain in StartList)
            {
                thisStartTrain.Save(outf);
            }

            outf.Write(AutoGenTrains.Count);
            foreach (AITrain train in AutoGenTrains)
            {
                train.Save(outf);
            }
        }

        // Saves train in autopilot mode
        public void SaveAutopil(Train train, BinaryWriter outf)
        {
            ((AITrain)train).Save(outf);
            if (Simulator.PlayerLocomotive != null)
            {
                var j = 0;
                int PlayerLocomotiveIndex = -1;
                foreach (TrainCar car in train.Cars)
                {
                    if (car == Simulator.PlayerLocomotive) { PlayerLocomotiveIndex = j; break; }
                    j++;
                }
                outf.Write(PlayerLocomotiveIndex);
            }
            else outf.Write (-1);
        }

        private void PrerunAI(int playerTrainOriginalTrain, Train playerTrain, LoaderProcess loader)
        {
            float firstAITime = StartList.GetNextTime();
            if (firstAITime > 0 && firstAITime < Simulator.ClockTime)
            {
                Trace.Write("\n Run AI : " + StartList.Count.ToString() + " ");

                // perform update for AI trains upto actual start time

                clockTime = firstAITime - 1.0f;
                localTime = true;
                PreUpdate = true;

                for (double runTime = firstAITime; runTime < Simulator.ClockTime; runTime += 5.0) // update with 5 secs interval
                {
                    int fullsec = Convert.ToInt32(runTime);
                    if (fullsec % 3600 == 0) Trace.Write(" " + (fullsec / 3600).ToString("00") + ":00 ");

                    AIUpdate((float)(runTime - clockTime), PreUpdate);
                    Simulator.Signals.Update(true);
                    clockTime = runTime;
                    if (loader.Terminated) return; // ping watchdog process
                }

                // prerun finished - check if train from which player train originates has run and is finished
                bool delayedrun = false;
                bool OrgTrainNotStarted = false;
                AITrain OrgTrain = null;

                if (playerTrainOriginalTrain > 0)
                {
                    OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                    OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain);
                    if (OrgTrainNotStarted)
                    {
                        Trace.TraceInformation("Player train start delayed as incoming train has yet to start");
                        delayedrun = true;
                    }
                    else if (OrgTrain != null)
                    {
                        Trace.TraceInformation("Player train start delayed as incoming train {0} has not yet arrived", OrgTrain.Name);
                        if (OrgTrain.Delay.HasValue)
                        {
                            Trace.TraceInformation("Estimated delay : {0}", OrgTrain.Delay.Value.ToString());
                        }
                        delayedrun = true;
                    }
                }
                else if (playerTrain != null && !playerTrain.InitialTrainPlacement()) // if player train exists but cannot be placed
                {
                    delayedrun = true;
                    Trace.TraceInformation("Player train start delayed as track is not clear");
                }

                // continue prerun until player train can be started
                if (delayedrun)
                {
                    float deltaTime = 5.0f; // update with 1 sec. interval
                    double runTime = Simulator.ClockTime - (double)deltaTime;
                    bool playerTrainStarted = false;

                    while (!playerTrainStarted)
                    {
                        AIUpdate((float)(runTime - clockTime), PreUpdate);
                        Simulator.Signals.Update(true);
                        clockTime = runTime;
                        runTime += deltaTime;

                        int fullsec = Convert.ToInt32(runTime);
                        if (fullsec % 3600 == 0) Trace.Write(" " + (fullsec / 3600).ToString("00") + ":00 ");

                        if (runTime >= 24 * 3600) // end of day reached
                        {
                            if (playerTrainOriginalTrain > 0)
                            {
                                if (OrgTrainNotStarted)
                                {
                                    throw new InvalidDataException("Session aborted - incoming train has not run at all");
                                }
                                else
                                {
                                    throw new InvalidDataException("Session aborted - incoming train has not arrived before midnight");
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("Session aborted - track for player train not cleared before midnight");
                            }
                        }

                        if (playerTrainOriginalTrain > 0)
                        {
                            OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                            OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain);
                            playerTrainStarted = (!OrgTrainNotStarted && OrgTrain == null);
                        }
                        else
                        {
                            playerTrainStarted = playerTrain.InitialTrainPlacement();
                        }
                    }

                    TimeSpan delayedStart = new TimeSpan((long)(Math.Pow(10, 7) * (clockTime - Simulator.ClockTime)));
                    Trace.TraceInformation("Start delayed by : {0}", delayedStart.ToString());
                    Simulator.ClockTime = runTime;
                }
                else
                {
                    Trace.TraceInformation("Player train started on time");
                }
            }

            Trace.Write("\n");
            PreUpdate = false;

        }

        /// <summary>
        /// Updates AI train information.
        /// Creates any AI trains that are scheduled to appear.
        /// Moves all active AI trains by calling their Update method.
        /// And finally, removes any AI trains that have reached the end of their path.
        /// </summary>
        public void Update(float elapsedClockSeconds)
        {
            AIUpdate(elapsedClockSeconds, false);
        }

        public void AIUpdate(float elapsedClockSeconds, bool preUpdate)
        {
            // update clock

            if (!localTime)
            {
                clockTime = Simulator.ClockTime;
            }

            // check to see if any train to be added

            float nextTrainTime = StartList.GetNextTime();
            if (nextTrainTime > 0 && nextTrainTime < clockTime)
            {
                List<AITrain> newTrains = StartList.GetTrains((float)clockTime);
                foreach (AITrain thisTrain in newTrains)
                {
                    Simulator.StartReference.Remove(thisTrain.Number);
                    AddToWorld(thisTrain);
                    if (thisTrain.InitialSpeed > 0)
                        // Add extra run to allow setting signals
                    {
                       thisTrain.AIPreUpdate(0);
                    }
                }
            }
            foreach (AITrain train in AITrains)
            {
                if (train.Cars.Count == 0 || train.Cars[0].Train != train)
                    TrainsToRemove.Add(train);
                else
                    train.AIUpdate(elapsedClockSeconds, clockTime, preUpdate);
            }

            RemoveTrains();
            AddTrains();
        }

        /// <summary>
        /// Creates an AI train
        /// </summary>
        private AITrain CreateAITrain(Service_Definition sd, Traffic_Traffic_Definition trd)
        {
            // set up a new AI train
            // first extract the service definition from the activity file
            // this gives the consist and path

            // find related traffic definition

            Traffic_Service_Definition trfDef = null;
            foreach (Traffic_Service_Definition thisDef in trd.TrafficItems)
            {
                if (String.Compare(thisDef.Service_Definition, sd.Name) == 0 &&
                thisDef.Time == sd.Time)
                {
                    trfDef = thisDef;
                    break;
                }
            }
            AITrain train = CreateAITrainDetail (sd, trfDef, false);
            if (train != null)
            {
                // insert in start list

                StartList.InsertTrain(train);
                Simulator.StartReference.Add(train.Number);
            }
            return train;
        }

        /// <summary>
        /// Creates the detail of an AI train
        /// Moves the models down 1000M to make them invisible.
        /// called also in case of autopilot mode
        /// </summary>
        public AITrain CreateAITrainDetail(Service_Definition sd, Traffic_Service_Definition trfDef, bool isInitialPlayerTrain)
        {
            // read service and consist file

            SRVFile srvFile = new SRVFile(Simulator.RoutePath + @"\SERVICES\" + sd.Name + ".SRV");
            string consistFileName = Simulator.BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON";
            CONFile conFile = new CONFile(consistFileName);
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

            // Patch Placingproblem - JeroenP
            // 
#if ACTIVITY_EDITOR
            AIPath aiPath = new AIPath(Simulator.TDB, Simulator.TSectionDat, pathFileName, Simulator.orRouteConfig);
#else
            AIPath aiPath = new AIPath(Simulator.TDB, Simulator.TSectionDat, pathFileName);
#endif
            // End patch

            if (aiPath.Nodes == null)
            {
                Trace.TraceWarning("Invalid path " + pathFileName + " for AI train : " + srvFile.Name + " ; train not started\n");
                return null;
            }

            float maxVelocityA = conFile.Train.TrainCfg.MaxVelocity.A;
            // sd.Name is the name of the service file.
            // srvFile.Name points to the name of the service within the Name() category such as Name ( "Eastbound Freight Train" ) in the service file.
            AITrain train = new AITrain(Simulator, sd, this, aiPath, srvFile.Efficiency, srvFile.Name, trfDef, maxVelocityA);
            Simulator.TrainDictionary.Add(train.Number, train);

            if (!Simulator.NameDictionary.ContainsKey(train.Name.ToLower()))
                Simulator.NameDictionary.Add(train.Name.ToLower(), train);

            if (consistFileName.Contains("tilted")) train.tilted = true;

            // also set Route max speed for speedpost-processing in train.cs
            train.TrainMaxSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;

            train.InitialSpeed = srvFile.TimeTable.InitialSpeed;

            if (maxVelocityA > 0 && srvFile.Efficiency > 0)
            {
                if (!Program.Simulator.Settings.EnhancedActCompatibility || Program.Simulator.TimetableMode) 
                    train.TrainMaxSpeedMpS = Math.Min(train.TrainMaxSpeedMpS, maxVelocityA);
                    // <CScomment> this is overridden if there are station stops
                else train.TrainMaxSpeedMpS = Math.Min(train.TrainMaxSpeedMpS, maxVelocityA * srvFile.Efficiency);
                }

            // add wagons
            train.Length = 0.0f;
            foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
            {

                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag";
                ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                if (!File.Exists(wagonFilePath))
                {
                    Trace.TraceWarning("Ignored missing wagon {0} in consist {1}", wagonFilePath, consistFileName);
                    continue;
                }

                try
                {
                    TrainCar car = RollingStock.Load(Simulator, wagonFilePath);
                    car.Flipped = wagon.Flip;
                    train.Cars.Add(car);
                    car.Train = train;
                    car.SignalEvent(PowerSupplyEvent.RaisePantograph, 1);
                    train.Length += car.LengthM;
                    if (isInitialPlayerTrain) car.CarID = "0 - " + wagon.UiD;
                    else car.CarID = "AI" + train.Number.ToString() + " - " + (train.Cars.Count - 1).ToString();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(wagonFilePath, error));
                }

            }// for each rail car

            if (train.Cars.Count <= 0)
            {
                Trace.TraceInformation("Empty consists for AI train - train removed");
                return null;
            }

            train.Cars[0].Headlight = 2;//AI train always has light on

            // Patch placingproblem JeroenP (1 line)
            train.RearTDBTraveller = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, aiPath); // create traveller
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "-----  New AI Train  -----\n");
#endif
            train.CreateRoute(false);  // create route without use of FrontTDBtraveller
            train.CheckFreight(); // check if train is freight or passenger
            train.AITrainDirectionForward = true;
            train.BrakeLine3PressurePSI = 0;


            return train;
        }

        /// <summary>
        /// Add train to world : 
        /// place train on required position
        /// initialize signals and movement
        /// </summary>

        private void AddToWorld(AITrain thisTrain)
        {
            // clear track and align switches - check state

            bool validPosition = true;
            Train.TCSubpathRoute tempRoute = thisTrain.CalculateInitialTrainPosition(ref validPosition);

            if (validPosition)
            {
                thisTrain.SetInitialTrainRoute(tempRoute);
                thisTrain.CalculatePositionOfCars(0);
                for (int i = 0; i < thisTrain.Cars.Count; i++)
                    thisTrain.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
                thisTrain.ResetInitialTrainRoute(tempRoute);
                validPosition = thisTrain.PostInit();
            }

            if (validPosition)
            {
                thisTrain.actualWaitTimeS = 0; // reset wait counter //
                thisTrain.TrainType = Train.TRAINTYPE.AI;
                AITrains.Add(thisTrain);
                Simulator.Trains.Add(thisTrain);
                if (Simulator.TrainDictionary.ContainsKey(thisTrain.Number)) Simulator.TrainDictionary.Remove(thisTrain.Number); // clear existing entry
                Simulator.TrainDictionary.Add(thisTrain.Number, thisTrain);
                if (Simulator.NameDictionary.ContainsKey(thisTrain.Name.ToLower())) Simulator.NameDictionary.Remove(thisTrain.Name.ToLower());
                Simulator.NameDictionary.Add(thisTrain.Name.ToLower(), thisTrain);
                if (thisTrain.InitialSpeed > 0 && thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.STATION_STOP)
                {
                    thisTrain.InitializeMoving();
                    thisTrain.MovementState = AITrain.AI_MOVEMENT_STATE.BRAKING;
                }
                else if (thisTrain.InitialSpeed == 0)
                    thisTrain.InitializeBrakes();

                if (MPManager.IsServer())
                {
                    MPManager.BroadCast((new MSGTrain(thisTrain, thisTrain.Number)).ToString());
                }
            }
            else
            {
                thisTrain.StartTime += 30;    // try again in half a minute
                thisTrain.actualWaitTimeS += 30;
                if (thisTrain.actualWaitTimeS > 900)   // tried for 15 mins
                {
                    TimeSpan timeStart = new TimeSpan((long)(Math.Pow(10, 7) * thisTrain.StartTime.Value));
                    Trace.TraceWarning("Cannot place AI train {0} at time {1}", thisTrain.Name, timeStart.ToString());
                }
                else
                {
                    StartList.InsertTrain(thisTrain);
                    Simulator.StartReference.Add(thisTrain.Number);
                }
            }
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "Added Train : " + thisTrain.Number.ToString() + " , accepted : " + validPosition.ToString()+"\n");

            foreach (TrackCircuitSection thisSection in Simulator.Signals.TrackCircuitList)
            {
                if (thisSection.DeadlockTraps.Count > 0)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "Section : " + thisSection.Index.ToString() + "\n");
                    foreach (KeyValuePair<int, List<int>> thisDeadlock in thisSection.DeadlockTraps)
                    {
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "    Train : " + thisDeadlock.Key.ToString() + "\n");
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "       With : " + "\n");
                        foreach (int otherTrain in thisDeadlock.Value)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "          " + otherTrain.ToString() + "\n");
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Removes AI trains that have reached the end of their path or
        /// have been coupled onto by the player train.
        /// Moves the models down 1000M to make them invisible.
        /// </summary>
        private void RemoveTrains()
        {
            List<Train> removeList = new List<Train>();

            foreach (AITrain train in TrainsToRemove)
            {
                Simulator.TrainDictionary.Remove(train.Number);
                Simulator.NameDictionary.Remove(train.Name.ToLower());
                AITrains.Remove(train);
                Simulator.Trains.Remove(train);
                removeList.Add(train);

                if (train.Cars.Count > 0 && train.Cars[0].Train == train)
                {
                    foreach (TrainCar car in train.Cars)
                    {
                        car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
                        car.IsPartOfActiveTrain = false;  // to stop sounds
                    }
                }
            }

            if (MultiPlayer.MPManager.IsServer() && removeList.Count > 0)
            {
                MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGRemoveTrain(removeList)).ToString());
            }

            TrainsToRemove.Clear();
        }

        private void AddTrains()
        {
            foreach (AITrain train in TrainsToAdd)
            {
                if (Simulator.TrainDictionary.ContainsKey(train.Number)) Simulator.TrainDictionary.Remove(train.Number); // clear existing entry
                Simulator.TrainDictionary.Add(train.Number, train);
                if (Simulator.NameDictionary.ContainsKey(train.Name.ToLower())) Simulator.NameDictionary.Remove(train.Name.ToLower());
                Simulator.NameDictionary.Add(train.Name.ToLower(), train);
                AITrains.Add(train);
                Simulator.Trains.Add(train);
            }
            TrainsToAdd.Clear();
        }

    }

    public class StartTrains : LinkedList<AITrain>
    {

        //================================================================================================//
        //
        // Insert item on correct time
        //

        public void InsertTrain(AITrain thisTrain)
        {
            if (this.Count == 0)
            {
                this.AddFirst(thisTrain);
            }
            else
            {
                LinkedListNode<AITrain> nextNode = this.First;
                AITrain nextTrain = nextNode.Value;
                bool inserted = false;
                while (!inserted)
                {
                    if (nextTrain.StartTime.Value > thisTrain.StartTime.Value)
                    {
                        this.AddBefore(nextNode, thisTrain);
                        inserted = true;
                    }
                    else if (nextNode.Next == null)
                    {
                        this.AddAfter(nextNode, thisTrain);
                        inserted = true;
                    }
                    else
                    {
                        nextNode = nextNode.Next;
                        nextTrain = nextNode.Value;
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Get next time
        //

        public float GetNextTime()
        {
            if (this.Count == 0)
            {
                return (-1.0f);
            }
            else
            {
                LinkedListNode<AITrain> nextNode = this.First;
                AITrain nextTrain = nextNode.Value;
                return (nextTrain.StartTime.Value);
            }
        }

        //================================================================================================//
        //
        // Get all trains with time < present time and remove these from list
        // Skip trains which have a valid 'formedof' set
        //

        public List<AITrain> GetTrains(float reqTime)
        {
            List<AITrain> itemList = new List<AITrain>();

            bool itemsCollected = false;
            LinkedListNode<AITrain> nextNode = this.First;
            LinkedListNode<AITrain> prevNode;

            while (!itemsCollected && nextNode != null)
            {
                if (nextNode.Value.StartTime.Value <= reqTime)
                {
                    if (nextNode.Value.FormedOf < 0)
                    {
                        itemList.Add(nextNode.Value);
                        prevNode = nextNode;
                        nextNode = prevNode.Next;
                        this.Remove(prevNode);
                    }
                    else
                    {
                        nextNode = nextNode.Next;
                    }
                }
                else
                {
                    itemsCollected = true;
                }
            }

            return (itemList);
        }

        //================================================================================================//
        //
        // Get unstarted train by number and remove it from startlist if required
        //

        public AITrain GetNotStartedTrainByNumber(int reqNumber, bool remove)
        {
            LinkedListNode<AITrain> AITrainNode = First;
            while (AITrainNode != null)
            {
                if (AITrainNode.Value.Number == reqNumber)
                {
                    AITrain reqTrain = AITrainNode.Value;
                    if (remove) Remove(AITrainNode);
                    return (reqTrain);
                }
                else
                {
                    AITrainNode = AITrainNode.Next;
                }
            }
            return (null);
        }
    }
}
