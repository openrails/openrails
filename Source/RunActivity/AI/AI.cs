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
using MSTS;
using ORTS.MultiPlayer;

namespace ORTS
{
    public class AI
    {
        public readonly Simulator Simulator;
        public List<AITrain> AITrains = new List<AITrain>();// active AI trains
        public Dictionary<int, AITrain> AITrainDictionary = new Dictionary<int, AITrain>();

        public StartTrains StartList = new StartTrains();
        private double clockTime; // clock time : local time before activity start, common time from simulator after start
        private bool localTime;  // if true : clockTime is local time
        public bool PreUpdate = false; // if true : running in pre-update phase
        public List<AITrain> TrainsToRemove = new List<AITrain>();

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
                    if (train == null)
                        continue;
                    AITrainDictionary.Add(sd.UiD, train);
                }
            }

            float firstAITime = StartList.GetNextTime();
            if (firstAITime > 0 && firstAITime < Simulator.ClockTime)
            {

                // perform update for AI trains upto actual start time

                clockTime = firstAITime - 1.0f;
                localTime = true;

                Trace.Write("\nRunning AI trains ...   ");
                PreUpdate = true;

                for (double runTime = firstAITime; runTime < Simulator.ClockTime; runTime += 5.0) // update with 5 secs interval
                {
                    AIUpdate((float)(runTime - clockTime), PreUpdate);
                    Simulator.Signals.Update((float)(runTime - clockTime));
                    clockTime = runTime;
                }

                Trace.Write("\n");
                PreUpdate = false;
            }

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
            }

            int totalStarting = inf.ReadInt32();

            for (int iStarting = 0; iStarting < totalStarting; iStarting++)
            {
                AITrain aiTrain = new AITrain(Simulator, inf);
                aiTrain.AI = this;
                StartList.InsertTrain(aiTrain);
            }
        }

        // save game state
        public void Save(BinaryWriter outf)
        {

            RemoveTrains();   // remove trains waiting to be removed

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
                    AddToWorld(thisTrain);
                }
            }
            foreach (AITrain train in AITrains)
            {
                if (train.Cars.Count == 0 || train.Cars[0].Train != train)
                    train.RemoveTrain();
                else
                    train.AIUpdate(elapsedClockSeconds, clockTime, preUpdate);
            }

            RemoveTrains();
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
            string consistFileName = srvFile.Train_Config;
            CONFile conFile = new CONFile(Simulator.BasePath + @"\TRAINS\CONSISTS\" + consistFileName + ".CON");
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

            PATTraveller patTraveller = new PATTraveller(pathFileName);
            Traveller tempTraveller = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes,
                patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Z);

            // figure out if the next waypoint is forward or back
            patTraveller.NextWaypoint();

            // get distance forward
            float fwdist = tempTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z);

            // reverse train, get distance backward
            tempTraveller.ReverseDirection();
            float bwdist = tempTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z);

            // check which way exists or is shorter (in case of loop)
            // remember : train is now facing backward !

            if (bwdist < 0 || (fwdist > 0 && bwdist > fwdist)) // no path backward or backward path is longer
                tempTraveller.ReverseDirection();


            //            PathDescription = patFile.Name;

            PATFile patFile = new PATFile(pathFileName);
            AIPath aiPath = new AIPath(patFile, Simulator.TDB, Simulator.TSectionDat, pathFileName);
            if (aiPath.Nodes == null)
            {
                Trace.TraceWarning("Invalid path " + pathFileName + " for AI train : " + sd.Name + " ; train not started\n");
                return null;
            }
            
            AITrain train = new AITrain(Simulator, sd.UiD, this, aiPath, sd.Time, srvFile.Efficiency, sd.Name, trfDef);
            if (consistFileName.Contains("tilted")) train.tilted = true;

            // also set Route max speed for speedpost-processing in train.cs
            train.TrainMaxSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;
            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                train.TrainMaxSpeedMpS = Math.Min(train.TrainMaxSpeedMpS, conFile.Train.TrainCfg.MaxVelocity.A);
            
            // insert in start list

            StartList.InsertTrain(train);
            train.RearTDBTraveller = new Traveller(tempTraveller);

            // add wagons
            TrainCar previousCar = null;
            foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
            {

                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag";
                ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCar car = RollingStock.Load(Simulator, wagonFilePath);
                    car.Flipped = wagon.Flip;
                    train.Cars.Add(car);
                    car.Train = train;
                    car.SignalEvent(Event.Pantograph1Up);
                    previousCar = car;
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
                    Trace.TraceWarning("Cannot place AI train {0} at time {1}", thisTrain.UiD, thisTrain.StartTime);
                }
                else
                {
                    StartList.InsertTrain(thisTrain);
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
            foreach (AITrain train in TrainsToRemove)
            {
                AITrainDictionary.Remove(train.UiD);
                AITrains.Remove(train);
                Simulator.Trains.Remove(train);
                if (train.Cars.Count > 0 && train.Cars[0].Train == train)
                {
                    foreach (TrainCar car in train.Cars)
                    {
                        car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
                    }
                }
            }

			if (TrainsToRemove.Count > 0)
			{
				List<Train> removeList = new List<Train>();
				foreach (AITrain train in TrainsToRemove)
					if (train.Cars.Count > 0 && train.Cars[0].Train != train)
						removeList.Add(train);
				if (MultiPlayer.MPManager.IsServer())
				{
					MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGRemoveTrain(removeList)).ToString());
				}
			}
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
                    if (nextTrain.StartTime > thisTrain.StartTime)
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
                return (nextTrain.StartTime);
            }
        }

        //================================================================================================//
        //
        // Get all trains with time < present time and remove these from list
        //

        public List<AITrain> GetTrains(float reqTime)
        {
            List<AITrain> itemList = new List<AITrain>();

            bool itemsCollected = false;
            LinkedListNode<AITrain> nextNode = this.First;
            LinkedListNode<AITrain> prevNode;

            while (!itemsCollected && nextNode != null)
            {
                if (nextNode.Value.StartTime <= reqTime)
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
    }
}
