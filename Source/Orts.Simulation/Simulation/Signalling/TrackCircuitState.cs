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

using Orts.Simulation.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Simulation.Signalling
{
    public class TrackCircuitState
    {
        public TrainOccupyState TrainOccupy;                       // trains occupying section      //
        public Train.TrainRouted TrainReserved;                    // train reserving section       //
        public int SignalReserved;                                 // signal reserving section      //
        public TrainQueue TrainPreReserved;                        // trains with pre-reservation   //
        public TrainQueue TrainClaimed;                            // trains with normal claims     //
        public bool RemoteAvailable;                               // remote info available         //
        public bool RemoteOccupied;                                // remote occupied state         //
        public bool RemoteSignalReserved;                          // remote signal reserved        //
        public int RemoteReserved;                                 // remote reserved (number only) //
        public bool Forced;                                        // forced by human dispatcher    //

        public TrackCircuitState()
        {
            TrainOccupy = new TrainOccupyState();
            TrainReserved = null;
            SignalReserved = -1;
            TrainPreReserved = new TrainQueue();
            TrainClaimed = new TrainQueue();
            Forced = false;
        }

        /// <summary>
        /// Restore
        /// IMPORTANT : trains are restored to dummy value, will be restored to full contents later
        /// </summary>
        public void Restore(Simulator simulator, BinaryReader inf)
        {
            int noOccupy = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noOccupy; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                int trainDirection = inf.ReadInt32();
                Train thisTrain = new Train(simulator, trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainOccupy.Add(thisRouted, trainDirection);
            }

            int trainReserved = inf.ReadInt32();
            if (trainReserved >= 0)
            {
                int trainRouteIndexR = inf.ReadInt32();
                Train thisTrain = new Train(simulator, trainReserved);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndexR);
                TrainReserved = thisRouted;
            }

            SignalReserved = inf.ReadInt32();

            int noPreReserve = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noPreReserve; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                Train thisTrain = new Train(simulator, trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainPreReserved.Enqueue(thisRouted);
            }

            int noClaimed = inf.ReadInt32();
            for (int trainNo = 0; trainNo < noClaimed; trainNo++)
            {
                int trainNumber = inf.ReadInt32();
                int trainRouteIndex = inf.ReadInt32();
                Train thisTrain = new Train(simulator, trainNumber);
                Train.TrainRouted thisRouted = new Train.TrainRouted(thisTrain, trainRouteIndex);
                TrainClaimed.Enqueue(thisRouted);
            }
            Forced = inf.ReadBoolean();

        }

        /// <summary>
        /// Reset train references after restore
        /// </summary>
        public void RestoreTrains(List<Train> trains, int sectionIndex)
        {

            // Occupy

            Dictionary<int[], int> tempTrains = new Dictionary<int[], int>();

            foreach (KeyValuePair<Train.TrainRouted, int> thisOccupy in TrainOccupy)
            {
                int[] trainKey = new int[2];
                trainKey[0] = thisOccupy.Key.Train.Number;
                trainKey[1] = thisOccupy.Key.TrainRouteDirectionIndex;
                int direction = thisOccupy.Value;
                tempTrains.Add(trainKey, direction);
            }

            TrainOccupy.Clear();

            foreach (KeyValuePair<int[], int> thisTemp in tempTrains)
            {
                int[] trainKey = thisTemp.Key;
                int number = trainKey[0];
                int routeIndex = trainKey[1];
                int direction = thisTemp.Value;
                Train thisTrain = Signals.FindTrain(number, trains);
                if (thisTrain != null)
                {
                    Train.TrainRouted thisTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainOccupy.Add(thisTrainRouted, direction);
                }
            }

            // Reserved

            if (TrainReserved != null)
            {
                int number = TrainReserved.Train.Number;
                Train reservedTrain = Signals.FindTrain(number, trains);
                if (reservedTrain != null)
                {
                    int reservedDirection = TrainReserved.TrainRouteDirectionIndex;
                    bool validreserve = true;

                    // check if reserved section is on train's route except when train is in explorer or manual mode
                    if (reservedTrain.ValidRoute[reservedDirection].Count > 0 && reservedTrain.ControlMode != Train.TRAIN_CONTROL.EXPLORER && reservedTrain.ControlMode != Train.TRAIN_CONTROL.MANUAL)
                    {
                        int dummy = reservedTrain.ValidRoute[reservedDirection].GetRouteIndex(sectionIndex, reservedTrain.PresentPosition[0].RouteListIndex);
                        validreserve = reservedTrain.ValidRoute[reservedDirection].GetRouteIndex(sectionIndex, reservedTrain.PresentPosition[0].RouteListIndex) >= 0;
                    }

                    if (validreserve || reservedTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                    {
                        TrainReserved = reservedDirection == 0 ? reservedTrain.routedForward : reservedTrain.routedBackward;
                    }
                    else
                    {
                        Trace.TraceWarning("Invalid reservation for train : {0} [{1}], section : {2} not restored. May lead to a fatal error later.", reservedTrain.Name, reservedDirection, sectionIndex);
                    }
                }
                else
                {
                    TrainReserved = null;
                }
            }

            // PreReserved

            Queue<Train.TrainRouted> tempQueue = new Queue<Train.TrainRouted>();

            foreach (Train.TrainRouted thisTrainRouted in TrainPreReserved)
            {
                tempQueue.Enqueue(thisTrainRouted);
            }
            TrainPreReserved.Clear();
            foreach (Train.TrainRouted thisTrainRouted in tempQueue)
            {
                Train thisTrain = Signals.FindTrain(thisTrainRouted.Train.Number, trains);
                int routeIndex = thisTrainRouted.TrainRouteDirectionIndex;
                if (thisTrain != null)
                {
                    Train.TrainRouted foundTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainPreReserved.Enqueue(foundTrainRouted);
                }
            }

            // Claimed

            tempQueue.Clear();

            foreach (Train.TrainRouted thisTrainRouted in TrainClaimed)
            {
                tempQueue.Enqueue(thisTrainRouted);
            }
            TrainClaimed.Clear();
            foreach (Train.TrainRouted thisTrainRouted in tempQueue)
            {
                Train thisTrain = Signals.FindTrain(thisTrainRouted.Train.Number, trains);
                int routeIndex = thisTrainRouted.TrainRouteDirectionIndex;
                if (thisTrain != null)
                {
                    Train.TrainRouted foundTrainRouted = routeIndex == 0 ? thisTrain.routedForward : thisTrain.routedBackward;
                    TrainClaimed.Enqueue(foundTrainRouted);
                }
            }

        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(TrainOccupy.Count);
            foreach (KeyValuePair<Train.TrainRouted, int> thisOccupy in TrainOccupy)
            {
                Train.TrainRouted thisTrain = thisOccupy.Key;
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
                outf.Write(thisOccupy.Value);
            }

            if (TrainReserved == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(TrainReserved.Train.Number);
                outf.Write(TrainReserved.TrainRouteDirectionIndex);
            }

            outf.Write(SignalReserved);

            outf.Write(TrainPreReserved.Count);
            foreach (Train.TrainRouted thisTrain in TrainPreReserved)
            {
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
            }

            outf.Write(TrainClaimed.Count);
            foreach (Train.TrainRouted thisTrain in TrainClaimed)
            {
                outf.Write(thisTrain.Train.Number);
                outf.Write(thisTrain.TrainRouteDirectionIndex);
            }

            outf.Write(Forced);
        }

        /// <summary>
        /// Get list of trains occupying track
        /// Check without direction
        /// </summary>
        public List<Train.TrainRouted> TrainsOccupying()
        {
            List<Train.TrainRouted> reqList = new List<Train.TrainRouted>();
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                reqList.Add(thisTCT.Key);
            }
            return (reqList);
        }

        /// <summary>
        /// Get list of trains occupying track
        /// Check based on direction
        /// </summary>
        public List<Train.TrainRouted> TrainsOccupying(int reqDirection)
        {
            List<Train.TrainRouted> reqList = new List<Train.TrainRouted>();
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                if (thisTCT.Value == reqDirection)
                {
                    reqList.Add(thisTCT.Key);
                }
            }
            return (reqList);
        }

        /// <summary>
        /// check if any trains occupy track
        /// Check without direction
        /// </summary>
        public bool HasTrainsOccupying()
        {
            return (TrainOccupy.Count > 0);
        }

        /// <summary>
        /// check if any trains occupy track
        /// Check based on direction
        /// </summary>
        public bool HasTrainsOccupying(int reqDirection, bool stationary)
        {
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                if (thisTCT.Value == reqDirection)
                {
                    if (Math.Abs(thisTCT.Key.Train.SpeedMpS) > 0.5f)
                        return (true);   // exclude (almost) stationary trains
                }

                if ((Math.Abs(thisTCT.Key.Train.SpeedMpS) <= 0.5f) && stationary)
                    return (true);   // (almost) stationay trains
            }

            return (false);
        }

        /// <summary>
        /// check if any trains occupy track
        /// Check for other train without direction
        /// </summary>
        public bool HasOtherTrainsOccupying(Train.TrainRouted thisTrain)
        {
            if (TrainOccupy.Count == 0)  // no trains
            {
                return (false);
            }

            if (TrainOccupy.Count == 1 && TrainOccupy.ContainsTrain(thisTrain))  // only one train and that one is us
            {
                return (false);
            }

            return (true);
        }

        /// <summary>
        /// check if any trains occupy track
        /// Check for other train based on direction
        /// </summary>
        public bool HasOtherTrainsOccupying(int reqDirection, bool stationary, Train.TrainRouted thisTrain)
        {
            foreach (KeyValuePair<Train.TrainRouted, int> thisTCT in TrainOccupy)
            {
                Train.TrainRouted otherTrain = thisTCT.Key;
                if (otherTrain != thisTrain)
                {
                    if (thisTCT.Value == reqDirection)
                    {
                        if (Math.Abs(thisTCT.Key.Train.SpeedMpS) > 0.5f)
                            return (true);   // exclude (almost) stationary trains
                    }

                    if ((Math.Abs(thisTCT.Key.Train.SpeedMpS) <= 0.5f) && stationary)
                        return (true);   // (almost) stationay trains
                }
            }

            return (false);
        }

        /// <summary>
        /// check if this train occupies track
        /// routed train
        /// </summary>
        public bool ThisTrainOccupying(Train.TrainRouted thisTrain)
        {
            return (TrainOccupy.ContainsTrain(thisTrain));
        }

        /// <summary>
        /// check if this train occupies track
        /// unrouted train
        /// </summary>
        public bool ThisTrainOccupying(Train thisTrain)
        {
            return (TrainOccupy.ContainsTrain(thisTrain));
        }

    }
}
