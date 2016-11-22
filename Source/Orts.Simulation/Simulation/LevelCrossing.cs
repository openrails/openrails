// COPYRIGHT 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Orts.Formats.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Orts.Simulation
{
    public class LevelCrossings
    {
        const float MaximumActivationDistance = 2000;
        
        readonly Simulator Simulator;
        public readonly Dictionary<int, LevelCrossingItem> TrackCrossingItems;
        public readonly Dictionary<int, LevelCrossingItem> RoadCrossingItems;
        public readonly Dictionary<LevelCrossingItem, LevelCrossingItem> RoadToTrackCrossingItems = new Dictionary<LevelCrossingItem, LevelCrossingItem>();

        public object Program { get; private set; }

        public LevelCrossings(Simulator simulator)
        {
            Simulator = simulator;
            TrackCrossingItems = simulator.TDB != null && simulator.TDB.TrackDB != null && simulator.TDB.TrackDB.TrackNodes != null && simulator.TDB.TrackDB.TrItemTable != null 
                ? GetLevelCrossingsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
            RoadCrossingItems = simulator.RDB != null && simulator.RDB.RoadTrackDB != null && simulator.RDB.RoadTrackDB.TrackNodes != null && simulator.RDB.RoadTrackDB.TrItemTable != null
                ? GetLevelCrossingsFromDB(simulator.RDB.RoadTrackDB.TrackNodes, simulator.RDB.RoadTrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
        }

        static Dictionary<int, LevelCrossingItem> GetLevelCrossingsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
        {
            return (from trackNode in trackNodes
                    where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
                    from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
                    where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trXING
                    select new KeyValuePair<int, LevelCrossingItem>(itemRef, new LevelCrossingItem(trackNode, trItemTable[itemRef])))
                    .ToDictionary(_ => _.Key, _ => _.Value);
        }

        /// <summary>
        /// Creates a level crossing from its track and road component IDs.
        /// </summary>
        /// <param name="position">Position of the level crossing object for error reporting.</param>
        /// <param name="trackIDs">List of TrItem IDs (from the track database) for the track crossing items.</param>
        /// <param name="roadIDs">List of TrItem IDs (from the road database) for the road crossing items.</param>
        /// <param name="warningTime">Time that gates should be closed prior to a train arriving (seconds).</param>
        /// <param name="minimumDistance">Minimum distance from the gates that a train is allowed to stop and have the gates open (meters).</param>
        /// <returns>The level crossing object comprising of the specified track and road items plus warning and distance configuration.</returns>
        public LevelCrossing CreateLevelCrossing(WorldPosition position, IEnumerable<int> trackIDs, IEnumerable<int> roadIDs, float warningTime, float minimumDistance)
        {
            var trackItems = trackIDs.Select(id => TrackCrossingItems[id]).ToArray();
            var roadItems = roadIDs.Select(id => RoadCrossingItems[id]).ToArray();
            if (trackItems.Length != roadItems.Length)
                Trace.TraceWarning("{0} level crossing contains {1} rail and {2} road items; expected them to match.", position, trackItems.Length, roadItems.Length);
            if (trackItems.Length >= roadItems.Length)
                for (var i = 0; i < roadItems.Length; i++)
                    if (!RoadToTrackCrossingItems.ContainsKey(roadItems[i]))
                        RoadToTrackCrossingItems.Add(roadItems[i], trackItems[i]);
            return new LevelCrossing(trackItems.Union(roadItems), warningTime, minimumDistance);
        }

        [CallOnThread("Updater")]
        public void Update(float elapsedClockSeconds)
        {
            foreach (var train in Simulator.Trains)
                UpdateCrossings(train, elapsedClockSeconds);
        }

        [CallOnThread("Updater")]
        void UpdateCrossings(Train train, float elapsedTime)
        {
            var speedMpS = train.SpeedMpS;
            var absSpeedMpS = Math.Abs(speedMpS);
            var maxSpeedMpS = train.AllowedMaxSpeedMpS;
            var minCrossingActivationSpeed = 5.0f;  //5.0MpS is equalivalent to 11.1mph.  This is the estimated min speed that MSTS uses to activate the gates when in range.
            

            bool validTrain = false;
            bool validStaticConsist = false;
            //var stopTime = elapsedTime; // This has been set up, but it is not being used in the code.
            //stopTime = 0;


            // We only care about crossing items which are:
            //   a) Grouped properly.
            //   b) Within the maximum activation distance of front/rear of the train.
            // Separate tests are performed for present speed and for possible maximum speed to avoid anomolies if train accelerates.
            // Special test is also done to check on section availability to avoid closure beyond signal at danger.

            foreach (var crossing in TrackCrossingItems.Values.Where(ci => ci.CrossingGroup != null))
            {
                var predictedDist = crossing.CrossingGroup.WarningTime * absSpeedMpS;
                var maxPredictedDist = crossing.CrossingGroup.WarningTime * (maxSpeedMpS - absSpeedMpS) / 2; // added distance if train accelerates to maxspeed
                var minimumDist = crossing.CrossingGroup.MinimumDistance;
                var totalDist = predictedDist + minimumDist + 1;
                var totalMaxDist = predictedDist + maxPredictedDist + minimumDist + 1;

                var reqDist = 0f; // actual used distance
                var hornReqDist = 0f; // used distance for horn blow

                // The purpose of this test is to validate the static consist that is within vicinity of the crossing.  
                if ((train.TrainType == Train.TRAINTYPE.STATIC) && WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, (minimumDist + (train.Length/2))) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, minimumDist + (minimumDist + (train.Length / 2))))
                {
                    if (WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, minimumDist) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, minimumDist))
                    {
                        validStaticConsist = true;
                    }
                    else
                    {
                        foreach (var scar in train.Cars)
                        {
                            if (WorldLocation.Within(crossing.Location, scar.WorldPosition.WorldLocation, minimumDist))
                            {
                                validStaticConsist = true;
                            }
                        }
                    }
                }

                if ((train.TrainType != Train.TRAINTYPE.STATIC) && WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, totalDist) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, totalDist))
                {
                    validTrain = true;
                    reqDist = totalDist;
                    hornReqDist = Math.Min(totalDist, 80.0f);
                }

                else if ((train.TrainType != Train.TRAINTYPE.STATIC) && WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, totalMaxDist) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, totalMaxDist))
                {
                    validTrain = true;
                    reqDist = totalMaxDist;
                    hornReqDist = Math.Min(totalMaxDist, 80.0f);
                }

                if ((train.TrainType == Train.TRAINTYPE.STATIC) && !validStaticConsist)
                {
                    continue;
                }

                if ((train.TrainType != Train.TRAINTYPE.STATIC) && !validTrain && !crossing.Trains.Contains(train))
                {
                    continue;
                }

                // Distances forward from the front and rearwards from the rear.
                var frontDist = crossing.DistanceTo(train.FrontTDBTraveller, reqDist);
                if (frontDist < 0 && train.TrainType != Train.TRAINTYPE.STATIC)
                {
                    frontDist = -crossing.DistanceTo(new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward), reqDist + train.Length);
                    if (frontDist > 0)
                    {
                        // Train cannot find crossing.
                        crossing.RemoveTrain(train);
                        continue;
                    }
                }

                var rearDist = -frontDist - train.Length;

                if (train is AITrain && frontDist <= hornReqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    //  Add generic actions if needed
                    ((AITrain)train).AuxActionsContain.CheckGenActions(this.GetType(), crossing.Location, rearDist, frontDist, crossing.TrackIndex);
                }

                // The tests below is to allow the crossings operate like the crossings under MSTS
                // Tests as follows
                // Train speed is 0.  This was the initial issue that was found under one the MSTS activities.  Activity should start without gates being activated.
                // There are 2 tests for train speed between 0 and 5.0MpS(11.1mph).  Covering forward movement and reverse movement.  
                // The last 2 tests is for testing trains running at line speed, forward or reverse.

                // The crossing only becomes active if the train has been added to the list such as crossing.AddTrain(train).
                // Note: With the conditions below, OR's crossings operates like the crossings in MSTS, with exception to the simulation of the timout below.

                // MSTS did not simulate a timeout, I introduced a simple timout using speedMpS.

                // Depending upon future development in this area, it would probably be best to have the current operation in its own class followed by any new region specific operations. 


                // Recognizing static consists at crossings.
                if ((train.TrainType == Train.TRAINTYPE.STATIC) && frontDist <= minimumDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    if (frontDist <= minimumDist - 15f && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist - 15f)
                    {
                        crossing.AddTrain(train);
                    }
                 }
                
                // Train is stopped.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.STATIC) && speedMpS == 0 && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    // First test is to simulate a timeout if a train comes to a stop before minimumDist
                    if (frontDist > minimumDist && Simulator.Trains.Contains(train))
                    {
                        crossing.RemoveTrain(train);
                    }
                    // This test is to factor in the train sitting on the crossing at the start of the activity.
                    else
                        crossing.AddTrain(train);
                }

                // Train is travelling toward crossing below 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.STATIC) && speedMpS > 0 && speedMpS <= minCrossingActivationSpeed && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    // This will allow a slow train to approach to the crossing's minmum distance without activating the crossing.
                    if (frontDist <= minimumDist + 70f) // Not all crossing systems operate the same so adding an additional 70 meters is only an option to improve operation.
                        crossing.AddTrain(train);
                }

                // Checking for reverse movement when train is approaching crossing while travelling under 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER) && speedMpS < 0 && absSpeedMpS <= minCrossingActivationSpeed && rearDist <= reqDist && (train.ReservedTrackLengthM <= 0 || rearDist < train.ReservedTrackLengthM) && frontDist <= minimumDist)
                {
                    // This will allow a slow train to approach a crossing to a certain point without activating the system.
                    // First test covers front of train clearing crossing.
                    // Second test covers rear of train approaching crossing.
                    if (frontDist > 2.5) // The value of 2.5 which is within minimumDist is used to test against frontDist to give the best possible distance the gates should deactivate.
                        crossing.RemoveTrain(train);
                    else if (rearDist <= minimumDist + 70f) // Not all crossing systems operate the same so adding an additional 70 meters is only an option to improve operation.
                        crossing.AddTrain(train);
                }

                // Checking for reverse movement through crossing when train is travelling above 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER) && speedMpS < 0 && absSpeedMpS > minCrossingActivationSpeed && rearDist <= reqDist && (train.ReservedTrackLengthM <= 0 || rearDist < train.ReservedTrackLengthM) && frontDist <= minimumDist)
                {
                    crossing.AddTrain(train);
                }

                // Player train travelling in forward direction above 11.1mph will activate the crossing.  
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.STATIC) && speedMpS > 0 && speedMpS > minCrossingActivationSpeed && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    crossing.AddTrain(train);
                }

                else
                {
                    crossing.RemoveTrain(train);
                }
            }
        }
    }

    public class LevelCrossingItem
    {
        readonly TrackNode TrackNode;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        internal List<Train> Trains = new List<Train>();
        internal List<Train> StaticConsists = new List<Train>();
        public readonly WorldLocation Location;
        public LevelCrossing CrossingGroup { get; internal set; }
        public uint TrackIndex { get { return TrackNode.Index; } }

        public LevelCrossing Crossing { get { return CrossingGroup; } }

        public LevelCrossingItem(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
        }

        [CallOnThread("Updater")]
        public void AddTrain(Train train)
        {
            if(train.TrainType == Train.TRAINTYPE.STATIC)
            {
                var staticConsists = StaticConsists;
                if (!staticConsists.Contains(train))
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    newStaticConsists.Add(train);
                    StaticConsists = newStaticConsists;
                }

            }
            else
            {
                var trains = Trains;
                if (!trains.Contains(train))
                {
                    var newTrains = new List<Train>(trains);
                    newTrains.Add(train);
                    Trains = newTrains;
                }
            }
        }

        [CallOnThread("Updater")]
        public void RemoveTrain(Train train)
        {
            var trains = Trains;
            var staticConsists = StaticConsists;
            if (staticConsists.Count > 0)
            {
                if (staticConsists.Contains(train))
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    newStaticConsists.Remove(train);
                    StaticConsists = newStaticConsists;
                }
                // Secondary option to remove Static entry from list in case the above does not work.
                // Since the above process would not be able to remove the static consist from the list when the locomotive attaches to the consist.
                // The process below will be able to do it. 
                else
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    for (int i = 0; i < newStaticConsists.Count; i++)
                    {
                        if (newStaticConsists[i].TrainType == Train.TRAINTYPE.STATIC)
                        {
                            newStaticConsists.RemoveAt(i);
                        }
                    }
                    StaticConsists = newStaticConsists;
                }
            }
            else if(trains.Count > 0)
            {
                if (trains.Contains(train))
                {
                    var newTrains = new List<Train>(trains);
                    newTrains.Remove(train);
                    Trains = newTrains;
                }
            }
        }

        public float DistanceTo(Traveller traveller)
        {
            return DistanceTo(traveller, float.MaxValue);
        }

        public float DistanceTo(Traveller traveller, float maxDistance)
        {
            return traveller.DistanceTo(TrackNode, Location.TileX, Location.TileZ, Location.Location.X, Location.Location.Y, Location.Location.Z, maxDistance);
        }
    }

    public class LevelCrossing
    {
        internal readonly List<LevelCrossingItem> Items;
        internal readonly float WarningTime;
        internal readonly float MinimumDistance;
        
        public LevelCrossing(IEnumerable<LevelCrossingItem> items, float warningTime, float minimumDistance)
        {
            Items = new List<LevelCrossingItem>(items);
            WarningTime = warningTime;
            MinimumDistance = minimumDistance;
            foreach (var item in items)
                item.CrossingGroup = this;
        }

        public bool HasTrain
        {
            get
            {
                bool trains = Items.Any(i => i.Trains.Count > 0);
                bool staticconsists = Items.Any(i => i.StaticConsists.Count > 0);
                if (trains && staticconsists)
                    return true;
                else if (trains || staticconsists)
                    return true;
                else
                    return false;
            }
        }
    }
}
