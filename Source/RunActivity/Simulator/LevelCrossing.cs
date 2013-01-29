// COPYRIGHT 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MSTS;

namespace ORTS
{
    public class LevelCrossings
    {
        const float MaximumActivationDistance = 2000;

        readonly Simulator Simulator;
        public readonly Dictionary<int, LevelCrossingItem> TrackCrossingItems;
        public readonly Dictionary<int, LevelCrossingItem> RoadCrossingItems;
        public readonly Dictionary<LevelCrossingItem, LevelCrossingItem> RoadToTrackCrossingItems = new Dictionary<LevelCrossingItem, LevelCrossingItem>();

        public LevelCrossings(Simulator simulator)
        {
            Simulator = simulator;
            TrackCrossingItems = simulator.TDB != null && simulator.TDB.TrackDB != null ? GetLevelCrossingsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
            RoadCrossingItems = simulator.RDB != null && simulator.RDB.RoadTrackDB != null ? GetLevelCrossingsFromDB(simulator.RDB.RoadTrackDB.TrackNodes, simulator.RDB.RoadTrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
        }

        static Dictionary<int, LevelCrossingItem> GetLevelCrossingsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
        {
            return (from trackNode in trackNodes
                    where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.noItemRefs > 0
                    from itemRef in trackNode.TrVectorNode.TrItemRefs
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
                UpdateCrossings(train);
        }

        [CallOnThread("Updater")]
        void UpdateCrossings(Train train)
        {
            var speedMpS = train.SpeedMpS;
            var absSpeedMpS = Math.Abs(speedMpS);
            // We only care about crossing items which are:
            //   a) Grouped properly.
            //   b) Within the maximum activation distance of front/rear of the train.
            foreach (var crossing in TrackCrossingItems.Values.Where(ci => ci.CrossingGroup != null))
            {
                var predictedDist = crossing.CrossingGroup.WarningTime * absSpeedMpS;
                var minimumDist = crossing.CrossingGroup.MinimumDistance;
                var totalDist = predictedDist + minimumDist + 1;

                if (!WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, totalDist) && !WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, totalDist))
                    continue;

                // Distances forward from the front and rearwards from the rear.
                var frontDist = crossing.DistanceTo(train.FrontTDBTraveller, totalDist);
                if (frontDist < 0)
                {
                    frontDist = -crossing.DistanceTo(new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward), totalDist + train.Length);
                    if (frontDist > 0)
                    {
                        // Train cannot find crossing.
                        crossing.RemoveTrain(train);
                        continue;
                    }
                }
                var rearDist = -frontDist - train.Length;

                if (speedMpS < 0)
                {
                    // Train is reversing; swap distances so frontDist is always the front.
                    var temp = rearDist;
                    rearDist = frontDist;
                    frontDist = temp;
                }

                if (frontDist <= predictedDist + minimumDist && rearDist <= minimumDist)
                    crossing.AddTrain(train);
                else
                    crossing.RemoveTrain(train);
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
        internal WorldLocation Location;
        internal LevelCrossing CrossingGroup;

        public LevelCrossing Crossing { get { return CrossingGroup; } }

        public LevelCrossingItem(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
        }

        [CallOnThread("Updater")]
        public void AddTrain(Train train)
        {
            var trains = Trains;
            if (!trains.Contains(train))
            {
                var newTrains = new List<Train>(trains);
                newTrains.Add(train);
                Trains = newTrains;
            }
        }

        [CallOnThread("Updater")]
        public void RemoveTrain(Train train)
        {
            var trains = Trains;
            if (trains.Contains(train))
            {
                var newTrains = new List<Train>(trains);
                newTrains.Remove(train);
                Trains = newTrains;
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
                return Items.Any(i => i.Trains.Count > 0);
            }
        }
    }
}
