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
// #define DEBUG_TRACEINFO
//

using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Timetables;
using Orts.Simulation.Signalling;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Simulation.AIs
{
    public class AI
    {
        public readonly Simulator Simulator;
        public List<AITrain> AITrains = new List<AITrain>();// active AI trains

        public StartTrains StartList = new StartTrains(); // trains yet to be started
        public List<AITrain> AutoGenTrains = new List<AITrain>(); // auto-generated trains
        public double clockTime; // clock time : local time before activity start, common time from simulator after start
        private bool localTime;  // if true : clockTime is local time
        public bool PreUpdate; // if true : running in pre-update phase
        public List<AITrain> TrainsToRemove = new List<AITrain>();
        public List<AITrain> TrainsToAdd = new List<AITrain>();
        public List<AITrain> TrainsToRemoveFromAI = new List<AITrain>();
        public bool aiListChanged = true; // To indicate to TrainListWindow that the list has changed;

        /// <summary>
        /// Loads AI train information from activity file.
        /// Creates a queue of AI trains in the order they should appear.
        /// At the moment AI trains are also created off scene so the rendering code will know about them.
        /// </summary>
        public AI(Simulator simulator, CancellationToken cancellation, double activityStartTime)
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
                var activityFile = simulator.Activity.Tr_Activity.Tr_Activity_File;
                foreach (var sd in activityFile.Traffic_Definition.ServiceDefinitionList)
                {
                    AITrain train = CreateAITrain(sd, activityFile.Traffic_Definition.TrafficFile.TrafficDefinition, simulator.TimetableMode);
                    if (cancellation.IsCancellationRequested) // ping loader watchdog
                        return;
                }
            }

            // prerun trains
            PrerunAI(cancellation);

            clockTime = Simulator.ClockTime;
            localTime = false;
        }

        // constructor for Timetable trains
        // trains allready have a number - must not be changed!
        public AI(Simulator simulator, List<TTTrain> allTrains, ref double ClockTime, int playerTrainOriginalTrain, TTTrain.FormCommand playerTrainFormedOfType, TTTrain playerTrain, CancellationToken cancellation)
        {
            Simulator = simulator;

            foreach (var train in allTrains)
            {
                if (train.TrainType == Train.TRAINTYPE.PLAYER)
                {
                    train.AI = this;
                    AITrains.Add(train);
                    aiListChanged = true;
                }
                else if (train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    train.AI = this;
                    StartList.InsertTrain(train);
                    Simulator.StartReference.Add(train.Number);
                }
                else if (train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE)
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
            PrerunAI(playerTrainOriginalTrain, playerTrainFormedOfType, playerTrain, cancellation);

            ClockTime = clockTime;
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
                string trainType = inf.ReadString();

                // activity mode trains
                if (String.Equals(trainType, "AI"))
                {
                    AITrain aiTrain = new AITrain(Simulator, inf, this);
                    AITrains.Add(aiTrain);
                    Simulator.Trains.Add(aiTrain);
                    simulator.TrainDictionary.Add(aiTrain.Number, aiTrain);
                    if (!Simulator.NameDictionary.ContainsKey(aiTrain.Name.ToLower()))
                        Simulator.NameDictionary.Add(aiTrain.Name.ToLower(), aiTrain);
                }

                // timetable mode trains
                else {
                    TTTrain aiTrain = new TTTrain(Simulator, inf, this);
                    if (aiTrain.TrainType != Train.TRAINTYPE.PLAYER) // add to AITrains except when it is player train
                    {
                        AITrains.Add(aiTrain);
                    }

                    if (!Simulator.TrainDictionary.ContainsKey(aiTrain.Number))
                    {
                        Simulator.Trains.Add(aiTrain);
                        simulator.TrainDictionary.Add(aiTrain.Number, aiTrain);
                    }
                    if (!Simulator.NameDictionary.ContainsKey(aiTrain.Name.ToLower()))
                        Simulator.NameDictionary.Add(aiTrain.Name.ToLower(), aiTrain);
                }
            }

            int totalStarting = inf.ReadInt32();

            for (int iStarting = 0; iStarting < totalStarting; iStarting++)
            {
                string trainType = inf.ReadString();
                if (String.Equals(trainType, "AI"))
                {
                    AITrain aiTrain = new AITrain(Simulator, inf, this);
                    StartList.InsertTrain(aiTrain);
                    Simulator.StartReference.Add(aiTrain.Number);
                }
                else
                {
                    TTTrain aiTrain = new TTTrain(Simulator, inf, this);
                    StartList.InsertTrain(aiTrain);
                    Simulator.StartReference.Add(aiTrain.Number);
                }
            }

            int totalAutoGen = inf.ReadInt32();

            for (int iAutoGen = 0; iAutoGen < totalAutoGen; iAutoGen++)
            {
                string trainType = inf.ReadString();
                if (String.Equals(trainType, "AI"))
                {
                    AITrain aiTrain = new AITrain(Simulator, inf, this);
                    AutoGenTrains.Add(aiTrain);
                    Simulator.AutoGenDictionary.Add(aiTrain.Number, aiTrain);
                }
                else
                {
                    TTTrain aiTrain = new TTTrain(Simulator, inf, this);
                    AutoGenTrains.Add(aiTrain);
                    Simulator.AutoGenDictionary.Add(aiTrain.Number, aiTrain);
                }
            }

            if (Simulator.PlayerLocomotive != null)
            {
                if (Simulator.PlayerLocomotive.Train is AITrain) ((AITrain)Simulator.PlayerLocomotive.Train).AI = this;
            }

            // in timetable mode : find player train and place it in Simulator.Trains on position 0.
            if (Simulator.TimetableMode)
            {
                int playerindex = -1;
                for (int tindex = 0; tindex < Simulator.Trains.Count && playerindex < 0; tindex++)
                {
                    if (Simulator.Trains[tindex].Number == 0)
                    {
                        playerindex = tindex;
                    }
                }

                if (playerindex > 0)
                {
                    var tmptrain = Simulator.Trains[playerindex];
                    Simulator.Trains[playerindex] = Simulator.Trains[0];
                    Simulator.Trains[0] = tmptrain;
                }

                Simulator.PlayerLocomotive = Simulator.Trains[0].LeadLocomotive;
            }
        }

        // Restore in autopilot mode

        public AI(Simulator simulator, BinaryReader inf, bool autopilot)
        {
            Debug.Assert(simulator.Trains != null, "Cannot restore AI without Simulator.Trains.");
            Simulator = simulator;
            string trainType = inf.ReadString(); // may be ignored, can be AI only
            AITrain aiTrain = new AITrain(Simulator, inf, this);
            int PlayerLocomotiveIndex = inf.ReadInt32();
            if (PlayerLocomotiveIndex >= 0) Simulator.PlayerLocomotive = aiTrain.Cars[PlayerLocomotiveIndex];
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

            // in timetable mode, include player train train[0]
            if (Simulator.TimetableMode)
            {
                outf.Write(AITrains.Count + 1);
                Simulator.Trains[0].Save(outf);
            }
            else
            {
                outf.Write(AITrains.Count);
            }

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
            else outf.Write(-1);
        }

        // prerun for activity mode
        private void PrerunAI(CancellationToken cancellation)
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
                    if (cancellation.IsCancellationRequested) return; // ping watchdog process
                }
            }
        }


        // prerun for timetable mode
        private void PrerunAI(int playerTrainOriginalTrain, TTTrain.FormCommand playerTrainFormedOfType, TTTrain playerTrain, CancellationToken cancellation)
        {
            bool endPreRun = false;

            float firstAITime = StartList.GetNextTime();
            if (firstAITime > 0 && firstAITime < Simulator.ClockTime)
            {
                Trace.Write("\n Run AI : " + StartList.Count.ToString() + " ");

                // perform update for AI trains upto actual start time

                clockTime = firstAITime - 1.0f;
                localTime = true;
                PreUpdate = true;
                bool activeTrains = false;
                for (double runTime = firstAITime; runTime < Simulator.ClockTime && !endPreRun; runTime += 5.0) // update with 5 secs interval
                {
                    int fullsec = Convert.ToInt32(runTime);
                    if (fullsec % 3600 < 5) Trace.Write(" " + (fullsec / 3600).ToString("00") + ":00 ");

                    endPreRun = AITTUpdate((float)(runTime - clockTime), PreUpdate, ref activeTrains);

                    if (activeTrains)
                    {
                        Simulator.Signals.Update(true);
                    }

                    clockTime = runTime;
                    if (cancellation.IsCancellationRequested) return; // ping watchdog process
                }

                // prerun finished - check if train from which player train originates has run and is finished
                bool delayedrun = false;
                bool OrgTrainNotStarted = false;
                TTTrain OrgTrain = null;
                TTTrain PlayTrain = null;

                // player train is pre-created - check if it exists already
                if (playerTrainFormedOfType == TTTrain.FormCommand.Created)
                {
                    PlayTrain = Simulator.Trains.GetAITrainByNumber(0) as TTTrain;
                    // train exists - set as player train
                    if (PlayTrain != null)
                    {
                        PlayTrain.TrainType = Train.TRAINTYPE.PLAYER;
                        PlayTrain.MovementState = AITrain.AI_MOVEMENT_STATE.INIT;
                    }
                    else
                    {
                        Trace.TraceInformation("Player train start delayed as track is not clear");
                        delayedrun = true;

                        // reset formed state as train can now be started directly
                        playerTrainOriginalTrain = -1;
                    }
                }

                // player train is extracted from pool
                else if (!String.IsNullOrEmpty(playerTrain.CreateFromPool))
                {
                    TimetablePool.TrainFromPool extractResult = TimetablePool.TrainFromPool.Failed;
                    if (Simulator.PoolHolder.Pools.ContainsKey(playerTrain.CreateFromPool))
                    {
                        TimetablePool thisPool = Simulator.PoolHolder.Pools[playerTrain.CreateFromPool];
                        int presentTime = Convert.ToInt32(Math.Floor(clockTime));
                        extractResult = thisPool.ExtractTrain(ref playerTrain, presentTime);
                    }
                    else
                    {
                        Trace.TraceError("Player train {0} : Invalid pool definition : {1} : pool not found", playerTrain.Name, playerTrain.CreateFromPool);
                    }

                    // switch on outcome
                    switch (extractResult)
                    {
                        // train formed : no further action
                        case TimetablePool.TrainFromPool.Formed:
                            break;

                        // train creation failed : no need to try again (will fail again), so abandone
                        case TimetablePool.TrainFromPool.Failed:
                            throw new InvalidDataException("Session aborted - cannot extract required engine from pool");

                        // not created but not failed : hold, try again in 30 secs.
                        case TimetablePool.TrainFromPool.NotCreated:
                            delayedrun = true;
                            break;

                        // forced created away from pool : create train
                        case TimetablePool.TrainFromPool.ForceCreated:
                            break;
                    }
                }

                // player train is formed out of other train
                else if (playerTrainOriginalTrain > 0 && (playerTrainFormedOfType == TTTrain.FormCommand.TerminationFormed || playerTrainFormedOfType == TTTrain.FormCommand.TerminationTriggered))
                {
                    OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                    OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain) as TTTrain;
                    if (OrgTrainNotStarted)
                    {
                        Trace.TraceInformation("Player train start delayed as incoming train has yet to start");
                        delayedrun = true;
                    }
                    else if (OrgTrain != null)
                    {
                        Trace.TraceInformation("Player train start delayed as incoming train {0} [{1}] has not yet arrived", OrgTrain.Name, OrgTrain.Number);
                        if (OrgTrain.Delay.HasValue)
                        {
                            Trace.TraceInformation("Last Reported delay : {0}", OrgTrain.Delay.Value.ToString());
                        }
                        delayedrun = true;
                    }
                }

                // if player train is detached from other train
                else if (playerTrainOriginalTrain > 0 && playerTrainFormedOfType == TTTrain.FormCommand.Detached)
                {
                    PlayTrain = Simulator.Trains.GetAITrainByNumber(0) as TTTrain;
                    // train exists - set as player train
                    if (PlayTrain != null)
                    {
                        PlayTrain.TrainType = Train.TRAINTYPE.PLAYER;
                        PlayTrain.MovementState = AITrain.AI_MOVEMENT_STATE.INIT;
                    }
                    else
                    {
                        OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                        PlayTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain) as TTTrain;

                        if (OrgTrainNotStarted || OrgTrain == null)
                        {
                            Trace.TraceInformation("Player train start delayed as original train has yet to start");
                            delayedrun = true;
                        }
                        else
                        {
                            Trace.TraceInformation("Player train start delayed as original train has not yet arrived");
                            delayedrun = true;
                        }
                    }
                }

                // if player train exists but cannot be placed or has no power 
                else if (playerTrain != null)
                {
                    bool playerHasValidPosition = false;
                    playerTrain.CalculateInitialTrainPosition(ref playerHasValidPosition);

                    if (!playerHasValidPosition)
                    {
                        delayedrun = true;
                        Trace.TraceInformation("Player train start delayed as track is not clear");
                    }

                    if (playerTrain.LeadLocomotive == null)
                    {
                        delayedrun = true;
                        Trace.TraceInformation("Player train start delayed as train has no power");
                    }
                }

                // if player train exists, also check if it has an engine
                if (!delayedrun && playerTrain != null)
                {
                    if (Simulator.PlayerLocomotive == null)
                    {
                        delayedrun = true;
                        Trace.TraceInformation("Player train start delayed as train has no power");
                    }
                }

                // continue prerun until player train can be started
                if (delayedrun)
                {
                    bool dummy = true;  // dummy boolead for ActiveTrains

                    float deltaTime = 5.0f; // update with 1 sec. interval
                    double runTime = Simulator.ClockTime - (double)deltaTime;
                    bool playerTrainStarted = false;

                    while (!playerTrainStarted)
                    {
                        endPreRun = AITTUpdate((float)(runTime - clockTime), PreUpdate, ref dummy);
                        Simulator.Signals.Update(true);
                        clockTime = runTime;
                        runTime += deltaTime;
                        if (cancellation.IsCancellationRequested) // ping loader watchdog
                            return;

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

                        if (playerTrainOriginalTrain > 0 && (playerTrainFormedOfType == TTTrain.FormCommand.TerminationFormed || playerTrainFormedOfType == TTTrain.FormCommand.TerminationTriggered))
                        {
                            OrgTrainNotStarted = Simulator.Trains.CheckTrainNotStartedByNumber(playerTrainOriginalTrain);
                            OrgTrain = Simulator.Trains.GetAITrainByNumber(playerTrainOriginalTrain) as TTTrain;
                            playerTrainStarted = (!OrgTrainNotStarted && OrgTrain == null);
                        }
                        else if (playerTrainOriginalTrain > 0 && playerTrainFormedOfType == TTTrain.FormCommand.Detached)
                        {
                            PlayTrain = Simulator.Trains.GetAITrainByNumber(0) as TTTrain;
                            // train exists - set as player train
                            playerTrainStarted = (PlayTrain != null);

                            if (playerTrain.LeadLocomotive == null)
                            {
                                playerTrainStarted = false;
                            }
                        }
                        else
                        {
                            playerTrain.CalculateInitialTrainPosition(ref playerTrainStarted);
                        }
                    }

                    TimeSpan delayedStart = new TimeSpan((long)(Math.Pow(10, 7) * (clockTime - Simulator.ClockTime)));
                    Trace.TraceInformation("Start delayed by : {0}", delayedStart.ToString());
                    StartList.RemovePlayerTrain();
                    TTTrain playerTTTrain = playerTrain as TTTrain;
                    playerTTTrain.InitalizePlayerTrain();
                    Simulator.ClockTime = runTime;
                }
                else
                {
                    // remove player train from start list
                    StartList.RemovePlayerTrain();

                    Trace.TraceInformation("Player train started on time");
                    TTTrain playerTTTrain = playerTrain as TTTrain;
                    playerTTTrain.InitalizePlayerTrain();
                }
            }

            // no AI trains ahead of player train 
            else
            {
                // remove player train from start list
                StartList.RemovePlayerTrain();

                Trace.TraceInformation("Player train started on time");
                TTTrain playerTTTrain = playerTrain as TTTrain;
                playerTTTrain.InitalizePlayerTrain();
                
                clockTime = Simulator.ClockTime = playerTTTrain.StartTime.Value;
            }

            Trace.Write("\n");
            PreUpdate = false;

        }

        /// <summary>
        /// Updates AI train information - activity mode
        /// Creates any AI trains that are scheduled to appear.
        /// Moves all active AI trains by calling their Update method.
        /// And finally, removes any AI trains that have reached the end of their path.
        /// </summary>
        public void ActivityUpdate(float elapsedClockSeconds)
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
                    var validPosition = AddToWorld(thisTrain);
                    if (thisTrain.InitialSpeed > 0 && validPosition)
                    // Add extra run to allow setting signals
                    {
                        thisTrain.AIPreUpdate(0);
                    }
                }
            }
            foreach (AITrain train in AITrains)
            {
                if (train.TrainType != Train.TRAINTYPE.AI_INCORPORATED && (train.Cars.Count == 0 && train.TrainType != Train.TRAINTYPE.AI_INCORPORATED || train.Cars[0].Train != train))
                    TrainsToRemove.Add(train);
                else
                    train.AIUpdate(elapsedClockSeconds, clockTime, preUpdate);
            }

            RemoveTrains();
            RemoveFromAITrains();
            AddTrains();
        }

        // used in timetable mode
        public void TimetableUpdate(float elapsedClockSeconds)
        {
            bool dummy = true; // dummy for activeTrains boolean
            AITTUpdate(elapsedClockSeconds, false, ref dummy);
        }

        public bool AITTUpdate(float elapsedClockSeconds, bool preUpdate, ref bool activeTrains)
        {
            bool endPreRun = false;
            // update clock

            if (!localTime)
            {
                clockTime = Simulator.ClockTime;
            }

            // check to see if any train to be added

            float nextTrainTime = StartList.GetNextTime();
            if (nextTrainTime > 0 && nextTrainTime < clockTime)
            {
                List<TTTrain> newTrains = StartList.GetTTTrains((float)clockTime);

                foreach (TTTrain thisTrain in newTrains)
                {
                    Simulator.StartReference.Remove(thisTrain.Number);
                    if (thisTrain.TrainType == Train.TRAINTYPE.AI_NOTSTARTED) thisTrain.TrainType = Train.TRAINTYPE.AI;
                    endPreRun = AddToWorldTT(thisTrain, newTrains);
                    if (endPreRun) break;
                }
            }

            // check if any active trains

            if (!activeTrains)
            {
                foreach (AITrain acttrain in AITrains)
                {
                    TTTrain actTTTrain = acttrain as TTTrain;
                    if (acttrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC && acttrain.TrainType != Train.TRAINTYPE.PLAYER)
                    {
                        activeTrains = true;
                        break;
                    }
                    else if (acttrain.MovementState == AITrain.AI_MOVEMENT_STATE.AI_STATIC && actTTTrain.ActivateTime < clockTime)
                    {
                        activeTrains = true;
                        break;
                    }
                }
            }

            if (activeTrains || TrainsToAdd.Count > 0)
            {
                if (preUpdate)
                {
                    float intervalTime = 0.5f;

                    for (float trainUpdateTime = 0; trainUpdateTime < elapsedClockSeconds && !endPreRun; trainUpdateTime += intervalTime)
                    {
                        clockTime += intervalTime;
                        nextTrainTime = StartList.GetNextTime();
                        if (nextTrainTime > 0 && nextTrainTime < clockTime)
                        {
                            List<TTTrain> newTrains = StartList.GetTTTrains((float)clockTime);

                            foreach (TTTrain thisTrain in newTrains)
                            {
                                Simulator.StartReference.Remove(thisTrain.Number);
                                if (thisTrain.TrainType == Train.TRAINTYPE.AI_NOTSTARTED) thisTrain.TrainType = Train.TRAINTYPE.AI;
                                endPreRun = AddToWorldTT(thisTrain, newTrains);
                                if (endPreRun) break;
                            }
                        }

                        foreach (var train in AITrains)
                        {
                            if (train.TrainType != Train.TRAINTYPE.PLAYER && train.TrainType != Train.TRAINTYPE.INTENDED_PLAYER)
                            {
                                if (train.Cars.Count == 0 || train.Cars[0].Train != train)
                                {
                                    TrainsToRemove.Add(train);
                                }
                                else
                                {
                                    train.AIUpdate(intervalTime, clockTime, preUpdate);
                                }
                            }
                            else if (train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER && train.MovementState == AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                            {
                                TTTrain trainTT = train as TTTrain;
                                int presentTime = Convert.ToInt32(Math.Floor(clockTime));
                                trainTT.UpdateAIStaticState(presentTime);
                            }
                        }

                        RemoveTrains();
                        RemoveFromAITrains();
                        AddTTTrains();
                    }
                }
                else
                {
                    foreach (var train in AITrains)
                    {
                        if (train.TrainType != Train.TRAINTYPE.PLAYER)
                        {
                            if (train.Cars.Count == 0 || train.Cars[0].Train != train)
                            {
                                TrainsToRemove.Add(train);
                            }
                            else
                            {
                                train.AIUpdate(elapsedClockSeconds, clockTime, preUpdate);
                            }
                        }
                    }
                    RemoveTrains();
                    RemoveFromAITrains();
                    AddTTTrains();
                }
            }
            return (endPreRun);
        }

        /// <summary>
        /// Creates an AI train
        /// </summary>
        private AITrain CreateAITrain(Service_Definition sd, Traffic_Traffic_Definition trd, bool isTimetableMode)
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
            ServiceFile srvFile = new ServiceFile(Simulator.RoutePath + @"\SERVICES\" + sd.Name + ".SRV");  // read service file
            AITrain train = CreateAITrainDetail(sd, trfDef, srvFile, isTimetableMode, false);
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
        public AITrain CreateAITrainDetail(Service_Definition sd, Traffic_Service_Definition trfDef, ServiceFile srvFile, bool isTimetableMode, bool isInitialPlayerTrain)
        {
            // read consist file

            string consistFileName = Simulator.BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON";
            ConsistFile conFile = new ConsistFile(consistFileName);
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

            // Patch Placingproblem - JeroenP
            // 
#if ACTIVITY_EDITOR
            AIPath aiPath = new AIPath(Simulator.TDB, Simulator.TSectionDat, pathFileName, isTimetableMode, Simulator.orRouteConfig);
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

            if (consistFileName.Contains("tilted")) train.IsTilting = true;

            // also set Route max speed for speedpost-processing in train.cs
            train.TrainMaxSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;

            train.InitialSpeed = srvFile.TimeTable.InitialSpeed;

            if (maxVelocityA > 0 && train.Efficiency > 0)
            {
                // <CScomment> this is overridden if there are station stops
                train.TrainMaxSpeedMpS = Math.Min(train.TrainMaxSpeedMpS, maxVelocityA * train.Efficiency);
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
                    Trace.TraceWarning($"Ignored missing {(wagon.IsEngine ? "engine" : "wagon")} {wagonFilePath} in consist {consistFileName}");
                    continue;
                }

                try
                {
                    TrainCar car = RollingStock.Load(Simulator, wagonFilePath);
                    car.Flipped = wagon.Flip;
                    train.Cars.Add(car);
                    car.Train = train;
                    train.Length += car.CarLengthM;
                    car.UiD = wagon.UiD;
                    if (isInitialPlayerTrain)
                    {
                        Simulator.PathName = aiPath.pathName;
                        if (MPManager.IsMultiPlayer()) car.CarID = MPManager.GetUserName() + " - " + car.UiD; //player's train is always named train 0.
                        else car.CarID = "0 - " + car.UiD; //player's train is always named train 0.
                        var mstsDieselLocomotive = car as MSTSDieselLocomotive;
                        if (Simulator.Activity != null && mstsDieselLocomotive != null)
                            mstsDieselLocomotive.DieselLevelL = mstsDieselLocomotive.MaxDieselLevelL * Simulator.Activity.Tr_Activity.Tr_Activity_Header.FuelDiesel / 100.0f;

                        var mstsSteamLocomotive = car as MSTSSteamLocomotive;
                        if (Simulator.Activity != null && mstsSteamLocomotive != null)
                        {
                            mstsSteamLocomotive.CombinedTenderWaterVolumeUKG = (ORTS.Common.Kg.ToLb(mstsSteamLocomotive.MaxLocoTenderWaterMassKG) / 10.0f) * Simulator.Activity.Tr_Activity.Tr_Activity_Header.FuelWater / 100.0f;
                            mstsSteamLocomotive.TenderCoalMassKG = mstsSteamLocomotive.MaxTenderCoalMassKG * Simulator.Activity.Tr_Activity.Tr_Activity_Header.FuelCoal / 100.0f;
                        }
                        if (train.InitialSpeed != 0)
                            car.SignalEvent(PowerSupplyEvent.RaisePantograph, 1);
                    }
                    else
                    {
                        car.SignalEvent(PowerSupplyEvent.RaisePantograph, 1);
                        car.CarID = "AI" + train.Number.ToString() + " - " + (train.Cars.Count - 1).ToString();
                    }

                    // associate location events
                    Simulator.ActivityRun.AssociateEvents(train);

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
            if (!isInitialPlayerTrain || train.InitialSpeed != 0) train.AITrainDirectionForward = true;
            train.BrakeLine3PressurePSI = 0;

            // Compute length of path to evaluate possibility of activity randomization
            if (Simulator.Settings.ActRandomizationLevel > 0 && Simulator.ActivityRun != null)
                train.PathLength = train.ComputePathLength();

            return train;
        }

        /// <summary>
        /// Add train to world : 
        /// place train on required position
        /// initialize signals and movement
        /// </summary>

        private bool AddToWorld(AITrain thisTrain)
        {
            // clear track and align switches - check state

            bool validPosition = true;
            Train.TCSubpathRoute tempRoute = thisTrain.CalculateInitialTrainPosition(ref validPosition);

            if (validPosition)
            {
                thisTrain.SetInitialTrainRoute(tempRoute);
                thisTrain.CalculatePositionOfCars();
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
                aiListChanged = true;
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
                {
                    thisTrain.InitializeBrakes();
                    thisTrain.AdjustControlsBrakeFull();
                }

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
                    Trace.TraceWarning("Cannot place AI train {0} ({1}) at time {2}", thisTrain.Name, thisTrain.Number, timeStart.ToString());
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

            return validPosition;
        }

        private bool AddToWorldTT(TTTrain thisTrain, List<TTTrain> nextTrains)
        {
            bool endPreRun = false;
            bool validPosition = true;
            Train.TCSubpathRoute tempRoute = null;

            // create from pool
            if (!String.IsNullOrEmpty(thisTrain.CreateFromPool))
            {
                if (!Simulator.PoolHolder.Pools.ContainsKey(thisTrain.CreateFromPool))
                {
                    Trace.TraceInformation("Train {0} : unknown pool defininion : {1} \n", thisTrain.Name, thisTrain.CreateFromPool);
                    thisTrain.CreateFromPool = String.Empty;
                }
            }

            if (!String.IsNullOrEmpty(thisTrain.CreateFromPool))
            {
                int presentTime = Convert.ToInt32(Math.Floor(clockTime));

                TimetablePool thisPool = Simulator.PoolHolder.Pools[thisTrain.CreateFromPool];
                TimetablePool.TrainFromPool poolResult = thisPool.ExtractTrain(ref thisTrain, presentTime);

                bool actionCompleted = false;

                // switch on outcome
                switch (poolResult)
                {
                    // train formed : no further action
                    case TimetablePool.TrainFromPool.Formed:
                        actionCompleted = true;
                        break;

                    // train creation delayed through incoming train
                    case TimetablePool.TrainFromPool.Delayed:
                        thisTrain.StartTime += 30;    // try again in half a minute
                        StartList.InsertTrain(thisTrain);
                        Simulator.StartReference.Add(thisTrain.Number);
                        actionCompleted = true;
                        break;

                    // train creation failed : no need to try again (will fail again), so abandone
                    case TimetablePool.TrainFromPool.Failed:
                        actionCompleted = true;
                        break;

                    // not created but not failed : hold, try again in 30 secs.
                    case TimetablePool.TrainFromPool.NotCreated:
                        validPosition = false;
                        actionCompleted = false;
                        break;

                    // forced created away from pool : create train
                    case TimetablePool.TrainFromPool.ForceCreated:
                        validPosition = true;
                        actionCompleted = false;
                        tempRoute = thisTrain.CalculateInitialTTTrainPosition(ref validPosition, nextTrains);
                        break;
                }

                //Trace.TraceInformation("Train {0} from pool {1} : {2} \n", thisTrain.Name, thisPool.PoolName, poolResult.ToString());

                if (actionCompleted) return (endPreRun);
            }

            // create in pool
            // note : train is placed in pool in outbound direction

            else if (!String.IsNullOrEmpty(thisTrain.CreateInPool))
            {
                // find place in pool
                int presentTime = Convert.ToInt32(Math.Floor(clockTime));
                TimetablePool thisPool = Simulator.PoolHolder.Pools[thisTrain.CreateInPool];

                int PoolStorageState = (int)TTTrain.PoolAccessState.PoolInvalid;
                thisTrain.TCRoute.TCRouteSubpaths[0] = thisPool.CreateInPool(thisTrain, out PoolStorageState, false);
                thisTrain.ValidRoute[0] = new Train.TCSubpathRoute(thisTrain.TCRoute.TCRouteSubpaths[0]);
                thisTrain.TCRoute.activeSubpath = 0;

                // if no storage available - abondone train
                if (PoolStorageState < 0)
                {
                    Trace.TraceInformation("Train : " + thisTrain.Name + " : no storage room available in pool : " + thisPool.PoolName + " ; engine not created");
                    return (endPreRun);
                }

                // use stored traveller
                thisTrain.PoolStorageIndex = PoolStorageState;
                thisTrain.RearTDBTraveller = new Traveller(thisPool.StoragePool[thisTrain.PoolStorageIndex].StoragePathTraveller);

                // if storage available check for other engines on storage track
                if (thisPool.StoragePool[thisTrain.PoolStorageIndex].StoredUnits.Count > 0)
                {
                    int lastTrainNumber = thisPool.StoragePool[thisTrain.PoolStorageIndex].StoredUnits[thisPool.StoragePool[thisTrain.PoolStorageIndex].StoredUnits.Count - 1];
                    TTTrain lastTrain = thisTrain.GetOtherTTTrainByNumber(lastTrainNumber);
                    if (lastTrain == null)
                    {
                        lastTrain = thisTrain.Simulator.GetAutoGenTTTrainByNumber(lastTrainNumber);
                    }
                    if (lastTrain != null)
                    {
                        thisTrain.CreateAhead = String.Copy(lastTrain.Name).ToLower();
                    }
                }

                tempRoute = thisTrain.CalculateInitialTTTrainPosition(ref validPosition, nextTrains);

                if (validPosition)
                {
                    thisTrain.SetInitialTrainRoute(tempRoute);
                    thisTrain.CalculatePositionOfCars();
                    for (int i = 0; i < thisTrain.Cars.Count; i++)
                        thisTrain.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
                    thisTrain.ResetInitialTrainRoute(tempRoute);

                    // set train route and position so proper position in pool can be calculated
                    thisTrain.UpdateTrainPosition();

                    // add unit to pool
                    thisPool.AddUnit(thisTrain, false);
                    validPosition = thisTrain.PostInit(false); // post init train but do not activate
                }
            }

            // clear track and align switches - check state

            else
            {
                tempRoute = thisTrain.CalculateInitialTTTrainPosition(ref validPosition, nextTrains);
                if (validPosition)
                {
                    thisTrain.SetInitialTrainRoute(tempRoute);
                    thisTrain.CalculatePositionOfCars();
                    for (int i = 0; i < thisTrain.Cars.Count; i++)
                        thisTrain.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
                    thisTrain.ResetInitialTrainRoute(tempRoute);

                    validPosition = thisTrain.PostInit(false); // post init train but do not activate
                }
            }


            if (validPosition)
            {
                if (thisTrain.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train added to world : " + thisTrain.Number + " ; type = " + thisTrain.TrainType + "\n");
                }

                thisTrain.actualWaitTimeS = 0; // reset wait counter //

                if (!AITrains.Contains(thisTrain))
                {
                    AITrains.Add(thisTrain);
                }

                if (thisTrain.TrainType != Train.TRAINTYPE.INTENDED_PLAYER && thisTrain.TrainType != Train.TRAINTYPE.PLAYER) // player train allready exists
                {
                    Simulator.Trains.Add(thisTrain);
                }

                if (thisTrain.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    thisTrain.TrainType = Train.TRAINTYPE.PLAYER;
                }

                if (Simulator.TrainDictionary.ContainsKey(thisTrain.Number)) Simulator.TrainDictionary.Remove(thisTrain.Number); // clear existing entry
                Simulator.TrainDictionary.Add(thisTrain.Number, thisTrain);
                if (Simulator.NameDictionary.ContainsKey(thisTrain.Name.ToLower())) Simulator.NameDictionary.Remove(thisTrain.Name.ToLower());
                Simulator.NameDictionary.Add(thisTrain.Name.ToLower(), thisTrain);
            }
            else
            {
                TimeSpan timeStart = new TimeSpan((long)(Math.Pow(10, 7) * thisTrain.StartTime.Value));
#if DEBUG_TRACEINFO
                Trace.TraceWarning("Delay to placing AI train {0} ({1}) at time {2}", thisTrain.Number, thisTrain.Name, timeStart.ToString());
#endif
                thisTrain.StartTime += 30;    // try again in half a minute
                thisTrain.actualWaitTimeS += 30;
                if (thisTrain.actualWaitTimeS > 900)   // tried for 15 mins
                {
                    timeStart = new TimeSpan((long)(Math.Pow(10, 7) * thisTrain.StartTime.Value));
                    Trace.TraceWarning("Cannot place AI train {0} ({1}) at time {2}", thisTrain.Number, thisTrain.Name, timeStart.ToString());
                }
                else
                {
                    StartList.InsertTrain(thisTrain);
                    Simulator.StartReference.Add(thisTrain.Number);
                }
            }
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "Added Train : " + thisTrain.Number.ToString() + " , accepted : " + validPosition.ToString() + "\n");

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
            return (endPreRun);
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
                // check if train is actually still waiting to be added
                if (TrainsToAdd.Contains(train))
                {
                    TrainsToAdd.Remove(train);
                }
                else
                {
                    Simulator.TrainDictionary.Remove(train.Number);
                    Simulator.NameDictionary.Remove(train.Name.ToLower());
                    AITrains.Remove(train);
                    Simulator.Trains.Remove(train);
                    removeList.Add(train);
                    aiListChanged = true;
                    if (train.Cars.Count > 0 && train.Cars[0].Train == train)
                    {
                        foreach (TrainCar car in train.Cars)
                        {
                            car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
                            car.IsPartOfActiveTrain = false;  // to stop sounds
                        }
                    }
                }
            }

            if (MPManager.IsServer() && removeList.Count > 0)
            {
                MPManager.BroadCast((new MSGRemoveTrain(removeList)).ToString());
            }

            TrainsToRemove.Clear();
        }

        /// <summary>
        /// Removes an AI train only from the AI train list, but leaves it in the train lists

        /// </summary>
        private void RemoveFromAITrains()
        {

            foreach (AITrain train in TrainsToRemoveFromAI)
            {
                AITrains.Remove(train);
                aiListChanged = true;
            }

            TrainsToRemoveFromAI.Clear();
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
                aiListChanged = true;
                if (train.TrainType != Train.TRAINTYPE.INTENDED_PLAYER && train.TrainType != Train.TRAINTYPE.PLAYER) Simulator.Trains.Add(train);
            }
            TrainsToAdd.Clear();
        }

        private void AddTTTrains()
        {
            foreach (TTTrain train in TrainsToAdd)
            {
                if (Simulator.TrainDictionary.ContainsKey(train.Number)) Simulator.TrainDictionary.Remove(train.Number); // clear existing entry
                Simulator.TrainDictionary.Add(train.Number, train);
                if (Simulator.NameDictionary.ContainsKey(train.Name.ToLower())) Simulator.NameDictionary.Remove(train.Name.ToLower());
                Simulator.NameDictionary.Add(train.Name.ToLower(), train);
                if (train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    AITrains.Insert(0, train);
                }
                else
                {
                    AITrains.Add(train);
                    Simulator.Trains.Add(train);
                }
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
                    itemList.Add(nextNode.Value);
                    prevNode = nextNode;
                    nextNode = prevNode.Next;
                    this.Remove(prevNode);
                }
                else
                {
                    itemsCollected = true;
                }
            }
            return (itemList);
        }

        public List<TTTrain> GetTTTrains(float reqTime)
        {
            List<TTTrain> itemList = new List<TTTrain>();

            bool itemsCollected = false;
            LinkedListNode<AITrain> nextNode = this.First;
            LinkedListNode<AITrain> prevNode;

            while (!itemsCollected && nextNode != null)
            {
                if (nextNode.Value.StartTime.Value <= reqTime)
                {
                    TTTrain nextTrain = nextNode.Value as TTTrain;
                    if (nextTrain.FormedOf < 0 && nextTrain.TrainType != Train.TRAINTYPE.PLAYER)
                    {
                        itemList.Add(nextTrain);
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

        public void RemovePlayerTrain()
        {
            LinkedListNode<AITrain> nextNode = this.First;
            LinkedListNode<AITrain> prevNode;
            bool playerFound = false;

            while (nextNode != null && !playerFound)
            {
                if (nextNode.Value.TrainType == Train.TRAINTYPE.PLAYER || nextNode.Value.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    prevNode = nextNode;
                    nextNode = prevNode.Next;
                    this.Remove(prevNode);
                    playerFound = true;
                }
                else
                {
                    nextNode = nextNode.Next;
                }
            }
        }

        //================================================================================================//
        //
        // Get unstarted train by number and remove it from startlist if required
        //

        public TTTrain GetNotStartedTTTrainByNumber(int reqNumber, bool remove)
        {
            LinkedListNode<AITrain> AITrainNode = First;
            while (AITrainNode != null)
            {
                if (AITrainNode.Value.Number == reqNumber)
                {
                    TTTrain reqTrain = AITrainNode.Value as TTTrain;
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

        //================================================================================================//
        //
        // Get unstarted train by number and remove it from startlist if required
        //

        public TTTrain GetNotStartedTTTrainByName(string reqName, bool remove)
        {
            LinkedListNode<AITrain> AITrainNode = First;
            while (AITrainNode != null)
            {
                if (String.Equals(AITrainNode.Value.Name.ToLower(), reqName))
                {
                    TTTrain reqTrain = AITrainNode.Value as TTTrain;
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
