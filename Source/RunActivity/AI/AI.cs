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
        public AI(Simulator simulator, double activityStartTime)
        {
            Simulator = simulator;
            if (simulator.Activity != null && simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition != null)
            {
                foreach (var sd in simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList)
                {
                    AITrain train = CreateAITrain(sd,
                    simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition.TrafficFile.TrafficDefinition);
                }
            }

            // prerun trains
            PrerunAI(-1);

            clockTime = Simulator.ClockTime;
            localTime = false;
        }

        // constructor for Timetable trains
        // trains allready have a number - must not be changed!
        public AI(Simulator simulator, List<AITrain> allTrains, double ClockTime, int playerTrainOriginalTrain)
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

                    StartList.InsertTrain(train);
                    Simulator.StartReference.Add(train.Number);
                }
            }

            // clear dictionary (no trains yet exist)
            Simulator.TrainDictionary.Clear();
            Simulator.NameDictionary.Clear();

            // prerun trains
            PrerunAI(playerTrainOriginalTrain);

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

        private void PrerunAI(int playerTrainOriginalTrain)
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
                }

                // prerun finished - check if train from which player train originates has run and is finished
                if (playerTrainOriginalTrain > 0)
                {
                    bool OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                    AITrain OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain);

                    bool delayedrun = false;

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

                    if (delayedrun)
                    {
                        float deltaTime = 1.0f; // update with 1 sec. interval
                        double runTime = Simulator.ClockTime - (double)deltaTime;

                        while (OrgTrainNotStarted || OrgTrain != null)
                        {
                            AIUpdate((float)(runTime - clockTime), PreUpdate);
                            Simulator.Signals.Update(true);
                            clockTime = runTime;
                            runTime += deltaTime;

                            if (runTime >= 24 * 3600) // end of day reached
                            {
                                if (OrgTrainNotStarted)
                                {
                                    throw new InvalidDataException("Session aborted - incoming train has not run at all");
                                }
                                else
                                {
                                    throw new InvalidDataException("Session aborted - incoming train has not run at all");
                                }
                            }

                            OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                            OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain);
                        }

                        TimeSpan delayedStart = new TimeSpan((long)(Math.Pow(10, 7) * (clockTime - Simulator.ClockTime)));
                        Trace.TraceInformation("Start delayed by : {0}", delayedStart.ToString());
                        Simulator.ClockTime = runTime;
                    }
                    else
                    {
                        Trace.TraceInformation("Player train formed on time");
                    }
                }

                Trace.Write("\n");
                PreUpdate = false;

            }
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
        /// Moves the models down 1000M to make them invisible.
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

            // read service and consist file

            SRVFile srvFile = new SRVFile(Simulator.RoutePath + @"\SERVICES\" + sd.Name + ".SRV");
            string consistFileName = Simulator.BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON";
            CONFile conFile = new CONFile(consistFileName);
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

	    // Patch Placingproblem - JeroenP
	    // 

            AIPath aiPath = new AIPath(Simulator.TDB, Simulator.TSectionDat, pathFileName);
            // End patch
       	    
            if (aiPath.Nodes == null)
            {
                Trace.TraceWarning("Invalid path " + pathFileName + " for AI train : " + sd.Name + " ; train not started\n");
                return null;
            }
            
            AITrain train = new AITrain(Simulator, sd.UiD, this, aiPath, sd.Time, srvFile.Efficiency, sd.Name, trfDef);
            Simulator.TrainDictionary.Add(train.Number, train);
           
            if (!Simulator.NameDictionary.ContainsKey(train.Name.ToLower()))
                Simulator.NameDictionary.Add(train.Name.ToLower(), train);

            if (consistFileName.Contains("tilted")) train.tilted = true;

            // also set Route max speed for speedpost-processing in train.cs
            train.TrainMaxSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;
            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                train.TrainMaxSpeedMpS = Math.Min(train.TrainMaxSpeedMpS, conFile.Train.TrainCfg.MaxVelocity.A);
            
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
                    car.SignalEvent(Event.Pantograph1Up);
                    train.Length += car.LengthM;
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

            train.CreateRoute(false);  // create route without use of FrontTDBtraveller
            train.CheckFreight(); // check if train is freight or passenger
            train.AITrainDirectionForward = true;
            train.BrakeLine3PressurePSI = 0;

            // insert in start list

            StartList.InsertTrain(train);
            Simulator.StartReference.Add(train.Number);

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
                    Trace.TraceWarning("Cannot place AI train {0} at time {1}", thisTrain.Name, thisTrain.StartTime.Value.ToString());
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
            TrainsToRemove.Clear();

            if (MultiPlayer.MPManager.IsServer())
            {
                MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGRemoveTrain(removeList)).ToString());
            }
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
        // Get unstarted train by number and remove it from startlist
        //

        public AITrain GetNotStartedTrainByNumber(int reqNumber)
        {
            LinkedListNode<AITrain> AITrainNode = First;
            while (AITrainNode != null)
            {
                if (AITrainNode.Value.Number == reqNumber)
                {
                    AITrain reqTrain = AITrainNode.Value;
                    Remove(AITrainNode);
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
