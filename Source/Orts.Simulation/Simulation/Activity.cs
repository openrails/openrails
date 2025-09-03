// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation
{

    public enum ActivityEventType
    {
        Timer,
        TrainStart,
        TrainStop,
        Couple,
        Uncouple
    }

    public class Activity
    {
        Simulator Simulator;

        // Passenger tasks
        public DateTime StartTime;
        public List<ActivityTask> Tasks = new List<ActivityTask>();
        public ActivityTask Current;
        double prevTrainSpeed = -1;

        // Freight events
        public List<EventWrapper> EventList = new List<EventWrapper>();
        public Boolean IsComplete;          // true once activity is completed.
        public Boolean IsSuccessful;        // status of completed activity
        public Nullable<int> StartTimeS;    // Clock time in seconds when activity was launched.
        public EventWrapper TriggeredEvent; // Indicates the currently triggered event whose data the ActivityWindow will pop up to display.

        // The ActivityWindow may be open when the simulation is saved with F2.
        // If so, we need to remember the event and the state of the window (is the activity resumed or still paused, so we can restore it.
        public bool IsActivityWindowOpen;       // Remembers the status of the ActivityWindow [closed|opened]
        public EventWrapper LastTriggeredEvent; // Remembers the TriggeredEvent after it has been cancelled.
        public bool IsActivityResumed;            // Remembers the status of the ActivityWindow [paused|resumed]
        public bool ReopenActivityWindow;       // Set on Restore() and tested by ActivityWindow
        // Note: The variables above belong to the Activity, not the ActivityWindow because they run on different threads.
        // The Simulator must not monitor variables in the Window thread, but it's OK for the Window thread to monitor the Simulator.

        // station stop logging flags - these are saved to resume correct logging after save
        private string StationStopLogFile;   // logfile name
        private bool StationStopLogActive;   // logging is active
        public EventWrapper triggeredEventWrapper = null;        // used for exchange with Sound.cs to trigger activity sounds;
        public bool NewMsgFromNewPlayer = false; // flag to indicate to ActivityWindow that there is a new message to be shown;
        public string MsgFromNewPlayer; // string to be displayed in ActivityWindow

        public List<TempSpeedPostItem> TempSpeedPostItems;

        public int RandomizabilityPerCent = 0; // 0 -> hardly randomizable ; 100 -> well randomizable
        public bool WeatherChangesPresent; // tested in case of randomized activities to state wheter weather should be randomized

        private Activity(BinaryReader inf, Simulator simulator, List<EventWrapper> oldEventList, List<TempSpeedPostItem> tempSpeedPostItems)
        {
            TempSpeedPostItems = tempSpeedPostItems;
            Simulator = simulator;
            RestoreThis(inf, simulator, oldEventList);
        }

        public Activity(ActivityFile actFile, Simulator simulator)
        {
            Simulator = simulator;  // Save for future use.
            Player_Service_Definition sd;
            sd = actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition;
            if (sd != null)
            {
                if (sd.Player_Traffic_Definition.Player_Traffic_List.Count > 0)
                {
                    PlatformItem Platform = null;
                    ActivityTask task = null;

                    foreach (var i in sd.Player_Traffic_Definition.Player_Traffic_List)
                    {
                        if (i.PlatformStartID < Simulator.TDB.TrackDB.TrItemTable.Length && i.PlatformStartID >= 0 && 
                            Simulator.TDB.TrackDB.TrItemTable[i.PlatformStartID] is PlatformItem)
                            Platform = Simulator.TDB.TrackDB.TrItemTable[i.PlatformStartID] as PlatformItem;
                        else
                        {
                            Trace.TraceWarning("PlatformStartID {0} is not present in TDB file", i.PlatformStartID);
                            continue;
                        }
                        if (Platform != null)
                        {
                            if (Simulator.TDB.TrackDB.TrItemTable[Platform.LinkedPlatformItemId] is PlatformItem)
                            {
                                PlatformItem Platform2 = Simulator.TDB.TrackDB.TrItemTable[Platform.LinkedPlatformItemId] as PlatformItem;
                                Tasks.Add(task = new ActivityTaskPassengerStopAt(simulator,
                                    task,
                                    i.ArrivalTime,
                                    i.DepartTime,
                                    Platform, Platform2));
                            }
                        }
                    }
                    Current = Tasks[0];
                }
            }

            // Compile list of freight events, if any, from the parsed ACT file.
            if (actFile.Tr_Activity == null) { return; }
            if (actFile.Tr_Activity.Tr_Activity_File == null) { return; }
            if (actFile.Tr_Activity.Tr_Activity_File.Events == null) { return; }
            var parsedEventList = actFile.Tr_Activity.Tr_Activity_File.Events.EventList;
            foreach (var i in parsedEventList)
            {
                if (i is EventCategoryAction)
                {
                    EventList.Add(new EventCategoryActionWrapper(i, Simulator));
                }
                if (i is EventCategoryLocation)
                {
                    EventList.Add(new EventCategoryLocationWrapper(i, Simulator));
                }
                if (i is EventCategoryTime)
                {
                    EventList.Add(new EventCategoryTimeWrapper(i, Simulator));
                }
                EventWrapper eventAdded = EventList.Last();
                eventAdded.OriginalActivationLevel = i.Activation_Level;
                if (i.ORTSWeatherChange != null || i.Outcomes.ORTSWeatherChange != null) WeatherChangesPresent = true;
            }

            StationStopLogActive = false;
            StationStopLogFile = null;
        }

        public ActivityTask Last
        {
            get
            {
                return Tasks.Count == 0 ? null : Tasks[Tasks.Count - 1];
            }
        }

        public bool IsFinished
        {
            get
            {
                return Tasks.Count == 0 ? false : Last.IsCompleted != null;
            }
        }

        public void Update()
        {
            // Update freight events
            // Set the clock first time through. Can't set in the Activity constructor as Simulator.ClockTime is still 0 then.
            if (!StartTimeS.HasValue)
            {
                StartTimeS = (int)Simulator.ClockTime;
                // Initialise passenger actual arrival time
                if (Current != null)
                {
                    if (Current is ActivityTaskPassengerStopAt)
                    {
                        ActivityTaskPassengerStopAt task = Current as ActivityTaskPassengerStopAt;
                    }
                }
            }
            if (this.IsComplete == false)
            {
                foreach (var i in EventList)
                {
                    // Once an event has fired, we don't respond to any more events until that has been acknowledged.
                    // so this line needs to be inside the EventList loop.
                    if (this.TriggeredEvent != null) { break; }

                    if (i != null && i.ParsedObject.Activation_Level > 0)
                    {
                        if (i.TimesTriggered < 1 || i.ParsedObject.Reversible)
                        {
                            if (i.Triggered(this))
                            {
                                if (i.IsDisabled == false)
                                {
                                    i.TimesTriggered += 1;
                                    if (i.IsActivityEnded(this))
                                    {
                                        IsComplete = true;
                                    }
                                    this.TriggeredEvent = i;    // Note this for Viewer and ActivityWindow to use.
                                    // Do this after IsActivityEnded() so values are ready for ActivityWindow
                                    LastTriggeredEvent = TriggeredEvent;
                                }
                            }
                            else
                            {
                                if (i.ParsedObject.Reversible)
                                {
                                    // Reversible event is no longer triggered, so can re-enable it.
                                    i.IsDisabled = false;
                                }
                            }
                        }
                    }
                }
            }

            // Update passenger tasks
            if (Current == null) return;

            Current.NotifyEvent(ActivityEventType.Timer);
            if (Current.IsCompleted != null)    // Surely this doesn't test for: 
            //   Current.IsCompleted == false
            // More correct would be:
            //   if (Current.IsCompleted.HasValue && Current.IsCompleted == true)
            // (see http://stackoverflow.com/questions/56518/c-is-there-any-difference-between-bool-and-nullablebool)
            {
                Current = Current.NextTask;
            }
            if (Simulator.OriginalPlayerTrain.TrainType == Train.TRAINTYPE.PLAYER || Simulator.OriginalPlayerTrain.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN)
            {
                if (Math.Abs(Simulator.OriginalPlayerTrain.SpeedMpS) < 0.2f)
                {
                    if (Math.Abs(prevTrainSpeed) >= 0.2f)
                    {
                        prevTrainSpeed = 0;
                        Current.NotifyEvent(ActivityEventType.TrainStop);
                        if (Current.IsCompleted != null)
                        {
                            Current = Current.NextTask;
                        }
                    }
                }
                else
                {
                    if (Math.Abs(prevTrainSpeed) < 0.2f && Math.Abs(Simulator.OriginalPlayerTrain.SpeedMpS) >= 0.2f)
                    {
                        prevTrainSpeed = Simulator.OriginalPlayerTrain.SpeedMpS;
                        Current.NotifyEvent(ActivityEventType.TrainStart);
                        if (Current.IsCompleted != null)
                        {
                            Current = Current.NextTask;
                        }
                    }
                }
            }
            else
            {
                if (Math.Abs(Simulator.OriginalPlayerTrain.SpeedMpS) <= Simulator.MaxStoppedMpS)
                {
                    if (prevTrainSpeed != 0)
                    {
                        prevTrainSpeed = 0;
                        Current.NotifyEvent(ActivityEventType.TrainStop);
                        if (Current.IsCompleted != null)
                        {
                            Current = Current.NextTask;
                        }
                    }
                }
                else
                {
                    if (prevTrainSpeed == 0 && Math.Abs(Simulator.OriginalPlayerTrain.SpeedMpS) > 0.2f)
                    {
                        prevTrainSpeed = Simulator.OriginalPlayerTrain.SpeedMpS;
                        Current.NotifyEvent(ActivityEventType.TrainStart);
                        if (Current.IsCompleted != null)
                        {
                            Current = Current.NextTask;
                        }
                    }
                }
            }
        }

        // <CJComment> Use of static methods is clumsy. </CJComment>
        public static void Save(BinaryWriter outf, Activity act)
        {
            Int32 noval = -1;
            if (act == null)
            {
                outf.Write(noval);
            }
            else
            {
                noval = 1;
                outf.Write(noval);
                act.Save(outf);
            }
        }

        // <CJComment> Re-creating the activity object seems bizarre but not ready to re-write it yet. </CJComment>
        public static Activity Restore(BinaryReader inf, Simulator simulator, Activity oldActivity)
        {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == -1)
            {
                return null;
            }
            else
            {
                // Retain the old EventList. It's full of static data so save and restore is a waste of effort
                Activity act = new Activity(inf, simulator, oldActivity.EventList, oldActivity.TempSpeedPostItems);
                return act;
            }
        }

        public void Save(BinaryWriter outf)
        {
            Int32 noval = -1;

            // Save passenger activity
            outf.Write((Int64)StartTime.Ticks);
            outf.Write((Int32)Tasks.Count);
            foreach (ActivityTask task in Tasks)
            {
                task.Save(outf);
            }
            if (Current == null) outf.Write(noval); else outf.Write((Int32)(Tasks.IndexOf(Current)));
            outf.Write(prevTrainSpeed);

            // Save freight activity
            outf.Write((bool)IsComplete);
            outf.Write((bool)IsSuccessful);
            outf.Write((Int32)StartTimeS);
            foreach (EventWrapper e in EventList)
            {
                e.Save(outf);
            }
            if (TriggeredEvent == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                outf.Write(EventList.IndexOf(TriggeredEvent));
            }
            outf.Write(IsActivityWindowOpen);
            if (LastTriggeredEvent == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                outf.Write(EventList.IndexOf(LastTriggeredEvent));
            }

            // Save info for ActivityWindow coming from new player train
            outf.Write(NewMsgFromNewPlayer);
            if (NewMsgFromNewPlayer) outf.Write(MsgFromNewPlayer);

            outf.Write(IsActivityResumed);

            // write log details

            outf.Write(StationStopLogActive);
            if (StationStopLogActive)
            {
                outf.Write(StationStopLogFile);
            }
        }

        public void RestoreThis(BinaryReader inf, Simulator simulator, List<EventWrapper> oldEventList)
        {
            Int32 rdval;

            // Restore passenger activity
            ActivityTask task;
            StartTime = new DateTime(inf.ReadInt64());
            rdval = inf.ReadInt32();
            for (int i = 0; i < rdval; i++)
            {
                task = GetTask(inf, simulator);
                task.Restore(inf);
                Tasks.Add(task);
            }
            rdval = inf.ReadInt32();
            Current = rdval == -1 ? null : Tasks[rdval];
            prevTrainSpeed = inf.ReadDouble();

            task = null;
            for (int i = 0; i < Tasks.Count; i++)
            {
                Tasks[i].PrevTask = task;
                if (task != null) task.NextTask = Tasks[i];
                task = Tasks[i];
            }

            // Restore freight activity
            IsComplete = inf.ReadBoolean();
            IsSuccessful = inf.ReadBoolean();
            StartTimeS = inf.ReadInt32();

            this.EventList = oldEventList;
            foreach (var e in EventList)
            {
                e.Restore(inf);
            }

            if (inf.ReadBoolean()) TriggeredEvent = EventList[inf.ReadInt32()];

            IsActivityWindowOpen = inf.ReadBoolean();
            if (inf.ReadBoolean()) LastTriggeredEvent = EventList[inf.ReadInt32()];

            // Restore info for ActivityWindow coming from new player train
            NewMsgFromNewPlayer = inf.ReadBoolean();
            if (NewMsgFromNewPlayer) MsgFromNewPlayer = inf.ReadString();

            IsActivityResumed = inf.ReadBoolean();
            ReopenActivityWindow = IsActivityWindowOpen;

            // restore logging info
            StationStopLogActive = inf.ReadBoolean();
            if (StationStopLogActive)
            {
                StationStopLogFile = inf.ReadString();

                foreach (ActivityTask stask in Tasks)
                {
                    if (stask.GetType() == typeof(ActivityTaskPassengerStopAt))
                    {
                        ActivityTaskPassengerStopAt stoptask = stask as ActivityTaskPassengerStopAt;
                        stoptask.LogStationLogFile = StationStopLogFile;
                        stoptask.LogStationStops = true;
                    }
                }
            }
            else
            {
                StationStopLogFile = null;
            }
        }

        static ActivityTask GetTask(BinaryReader inf, Simulator simulator)
        {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == 1)
                return new ActivityTaskPassengerStopAt(simulator);
            else
                return null;
        }

        public void StartStationLogging(string stationLogFile)
        {
            StationStopLogFile = stationLogFile;
            StationStopLogActive = true;

            var stringBuild = new StringBuilder();

            char separator = (char)(DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), Simulator.Settings.DataLoggerSeparator);
            stringBuild.Append("STATION");
            stringBuild.Append(separator);
            stringBuild.Append("BOOKED ARR");
            stringBuild.Append(separator);
            stringBuild.Append("BOOKED DEP");
            stringBuild.Append(separator);
            stringBuild.Append("ACTUAL ARR");
            stringBuild.Append(separator);
            stringBuild.Append("ACTUAL DEP");
            stringBuild.Append(separator);
            stringBuild.Append("DELAY");
            stringBuild.Append(separator);
            stringBuild.Append("STATE");
            stringBuild.Append("\n");
            File.AppendAllText(StationStopLogFile, stringBuild.ToString());

            foreach (ActivityTask task in Tasks)
            {
                if (task.GetType() == typeof(ActivityTaskPassengerStopAt))
                {
                    ActivityTaskPassengerStopAt stoptask = task as ActivityTaskPassengerStopAt;
                    stoptask.LogStationLogFile = StationStopLogFile;
                    stoptask.LogStationStops = true;
                }
            }
        }

        /// <summary>
        /// Add speedposts to the track database for each Temporary Speed Restriction zone
        /// </summary>
        /// <param name="routeFile"></param>
        /// <param name="tsectionDat">track sections containing the details of the various sections</param>
        /// <param name="trackDB">The track Database that needs to be updated</param>
        /// <param name="zones">List of speed restriction zones</param>
        public void AddRestrictZones(Tr_RouteFile routeFile, TrackSectionsFile tsectionDat, TrackDB trackDB, ActivityRestrictedSpeedZones zones)
        {
            if (zones.ActivityRestrictedSpeedZoneList.Count < 1) return;

            TempSpeedPostItems = new List<TempSpeedPostItem>();

            TrItem[] newSpeedPostItems = new TempSpeedPostItem[2];

            Traveller traveller;

            const float MaxDistanceOfWarningPost = 2000;

            for (int idxZone = 0; idxZone < zones.ActivityRestrictedSpeedZoneList.Count; idxZone++)
			{
               var worldPosition1 = new WorldPosition();
                newSpeedPostItems[0]   = new TempSpeedPostItem(routeFile,
                    zones.ActivityRestrictedSpeedZoneList[idxZone].StartPosition, true, worldPosition1, false);
                var worldPosition2 = new WorldPosition();
                newSpeedPostItems[1] = new TempSpeedPostItem(routeFile,
                    zones.ActivityRestrictedSpeedZoneList[idxZone].EndPosition, false, worldPosition2, false);
			
                // Add the speedposts to the track database. This will set the TrItemId's of all speedposts
            trackDB.AddTrItems(newSpeedPostItems);

            // And now update the various (vector) tracknodes (this needs the TrItemIds.
                var endOffset = AddItemIdToTrackNode(ref zones.ActivityRestrictedSpeedZoneList[idxZone].EndPosition,
                    tsectionDat, trackDB, newSpeedPostItems[1], out traveller);
                var startOffset = AddItemIdToTrackNode(ref zones.ActivityRestrictedSpeedZoneList[idxZone].StartPosition,
                    tsectionDat, trackDB, newSpeedPostItems[0], out traveller);
                float distanceOfWarningPost = 0;
                TrackNode trackNode = trackDB.TrackNodes[traveller.TrackNodeIndex];
                if (startOffset != null && endOffset != null && startOffset > endOffset)
                {
                    FlipRestrSpeedPost((TempSpeedPostItem)newSpeedPostItems[0]);
                    FlipRestrSpeedPost((TempSpeedPostItem)newSpeedPostItems[1]);
                    distanceOfWarningPost = (float)Math.Min(MaxDistanceOfWarningPost, traveller.TrackNodeLength - (double)startOffset);
                }
                else if (startOffset != null && endOffset != null && startOffset <= endOffset)
                    distanceOfWarningPost = (float)Math.Max(-MaxDistanceOfWarningPost, -(double)startOffset);
                traveller.Move(distanceOfWarningPost);
                var worldPosition3 = new WorldPosition();
                var speedWarningPostItem = new TempSpeedPostItem(routeFile,
                    zones.ActivityRestrictedSpeedZoneList[idxZone].StartPosition, false, worldPosition3, true);
                SpeedPostPosition(speedWarningPostItem, ref traveller);
                if (startOffset != null && endOffset != null && startOffset > endOffset)
                {
                    FlipRestrSpeedPost((TempSpeedPostItem)speedWarningPostItem);
                }
                ComputeTablePosition((TempSpeedPostItem)newSpeedPostItems[0]); 
                TempSpeedPostItems.Add((TempSpeedPostItem)newSpeedPostItems[0]);
                ComputeTablePosition((TempSpeedPostItem)newSpeedPostItems[1]); 
                TempSpeedPostItems.Add((TempSpeedPostItem)newSpeedPostItems[1]);
                ComputeTablePosition((TempSpeedPostItem)speedWarningPostItem); 
                TempSpeedPostItems.Add((TempSpeedPostItem)speedWarningPostItem);
            }
        }
        
        /// <summary>
        /// Add a reference to a new TrItemId to the correct trackNode (which needs to be determined from the position)
        /// </summary>
        /// <param name="position">Position of the new </param>
        /// <param name="tsectionDat">track sections containing the details of the various sections</param>
        /// <param name="trackDB">track database to be modified</param>
        /// <param name="newTrItemRef">The Id of the new TrItem to add to the tracknode</param>
        /// <param name="traveller">The computed traveller to the speedPost position</param>
        static float? AddItemIdToTrackNode(ref Position position, TrackSectionsFile tsectionDat, TrackDB trackDB, TrItem newTrItem, out Traveller traveller)
        {
            float? offset = 0.0f;
            traveller = new Traveller(tsectionDat, trackDB.TrackNodes, position.TileX, position.TileZ, position.X, position.Z);
            TrackNode trackNode = trackDB.TrackNodes[traveller.TrackNodeIndex];//find the track node
            if (trackNode.TrVectorNode != null)
            {
                offset = traveller.TrackNodeOffset;
                SpeedPostPosition((TempSpeedPostItem)newTrItem, ref traveller);
                InsertTrItemRef(tsectionDat, trackDB, trackNode.TrVectorNode, (int)newTrItem.TrItemId, (float)offset);
            }
            return offset;
        }

        /// <summary>
        /// Determine position parameters of restricted speed Post
        /// </summary>
        /// <param name="restrSpeedPost">The Id of the new restricted speed post to position</param>
        /// <param name="traveller">The traveller to the speedPost position</param>
        /// 
        static void SpeedPostPosition(TempSpeedPostItem restrSpeedPost, ref Traveller traveller)
        {
            restrSpeedPost.Y = traveller.Y;
            restrSpeedPost.Angle = -traveller.RotY + (float)Math.PI / 2;
            restrSpeedPost.WorldPosition.XNAMatrix = Matrix.CreateFromYawPitchRoll(-traveller.RotY, 0, 0);
            restrSpeedPost.WorldPosition.XNAMatrix.M41 = traveller.X;
            restrSpeedPost.WorldPosition.XNAMatrix.M42 = traveller.Y;
            restrSpeedPost.WorldPosition.XNAMatrix.M43 = traveller.Z;
            restrSpeedPost.WorldPosition.TileX = traveller.TileX;
            restrSpeedPost.WorldPosition.TileZ = traveller.TileZ;
            //                    restrSpeedPost.WorldPosition.Normalize();
            restrSpeedPost.WorldPosition.XNAMatrix.M43 *= -1;
        }

        /// <summary>
        /// Flip restricted speedpost 
        /// </summary>
        /// <param name="restrSpeedPost">The Id of the restricted speedpost to flip</param>
        /// 
        static void FlipRestrSpeedPost(TempSpeedPostItem restrSpeedPost)
        {
            restrSpeedPost.Angle += (float)Math.PI;
            restrSpeedPost.WorldPosition.XNAMatrix.M11 *= -1;
            restrSpeedPost.WorldPosition.XNAMatrix.M13 *= -1;
            restrSpeedPost.WorldPosition.XNAMatrix.M31 *= -1;
            restrSpeedPost.WorldPosition.XNAMatrix.M33 *= -1;
        }

        /// <summary>
        /// Compute position of restricted speedpost table
        /// </summary>
        /// <param name="restrSpeedPost">The Id of the restricted speed post to flip</param>
        /// 
        static void ComputeTablePosition(TempSpeedPostItem restrSpeedPost)
        {
            var speedPostTablePosition = new Vector3(2.2f, 0, 0);
            Vector3.Transform(ref speedPostTablePosition, ref restrSpeedPost.WorldPosition.XNAMatrix, out speedPostTablePosition);
            restrSpeedPost.WorldPosition.XNAMatrix.Translation = speedPostTablePosition;
            restrSpeedPost.WorldPosition.Normalize();
        }


        /// <summary>
        /// Insert a reference to a new TrItem to the already existing TrItemRefs basing on its offset within the track node.
        /// </summary>
        /// 
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "Keeping identifier consistent to use in MSTS")]
        static void InsertTrItemRef(TrackSectionsFile tsectionDat, TrackDB trackDB, TrVectorNode thisVectorNode, int newTrItemId, float offset)
        {
            int[] newTrItemRefs = new int[thisVectorNode.NoItemRefs + 1];
            if (thisVectorNode.NoItemRefs > 0)
            {
                thisVectorNode.TrItemRefs.CopyTo(newTrItemRefs, 0);
                // insert the new TrItemRef accordingly to its offset
                for (int iTrItems = thisVectorNode.NoItemRefs - 1; iTrItems >= 0; iTrItems--)
                {
                    var currTrItemID = newTrItemRefs[iTrItems];
                    var currTrItem = trackDB.TrItemTable[currTrItemID];
                    Traveller traveller = new Traveller(tsectionDat, trackDB.TrackNodes, currTrItem.TileX, currTrItem.TileZ, currTrItem.X, currTrItem.Z);
                    if (offset >= traveller.TrackNodeOffset)
                    {
                        newTrItemRefs[iTrItems + 1] = newTrItemId;
                        break;
                    }
                    else newTrItemRefs[iTrItems + 1] = currTrItemID;
                    if (iTrItems == 0) newTrItemRefs[0] = newTrItemId;
                }
            }
            else newTrItemRefs[0] = newTrItemId;
            thisVectorNode.TrItemRefs = newTrItemRefs; //use the new item lists for the track node
            thisVectorNode.NoItemRefs++;
        }

        public void AssociateEvents(Train train)
        {
            foreach (var eventWrapper in EventList)
            {
                if (eventWrapper is EventCategoryLocationWrapper && eventWrapper.ParsedObject.TrainService != "" &&
                    train.Name.ToLower() == eventWrapper.ParsedObject.TrainService.ToLower())
                {
                    if (eventWrapper.ParsedObject.TrainStartingTime == -1 || (train as AITrain).ServiceDefinition.Time == eventWrapper.ParsedObject.TrainStartingTime)
                    {
                        eventWrapper.Train = train;
                    }
                }
            }
        }
    }

    public class ActivityTask
    {
        public bool? IsCompleted { get; internal set; }
        public ActivityTask PrevTask { get; internal set; }
        public ActivityTask NextTask { get; internal set; }
        public DateTime CompletedAt { get; internal set; }
        public string DisplayMessage { get; internal set; }
        public Color DisplayColor { get; internal set; }

        public virtual void NotifyEvent(ActivityEventType EventType)
        {
        }

        public virtual void Save(BinaryWriter outf)
        {
            Int32 noval = -1;
            if (IsCompleted == null) outf.Write(noval); else outf.Write(IsCompleted.Value ? (Int32)1 : (Int32)0);
            outf.Write((Int64)CompletedAt.Ticks);
            outf.Write(DisplayMessage);
        }

        public virtual void Restore(BinaryReader inf)
        {
            Int64 rdval;
            rdval = inf.ReadInt32();
            IsCompleted = rdval == -1 ? (bool?)null : rdval == 0 ? false : true;
            CompletedAt = new DateTime(inf.ReadInt64());
            DisplayMessage = inf.ReadString();
        }
    }

    /// <summary>
    /// Helper class to calculate distances along the path
    /// </summary>
    public class TDBTravellerDistanceCalculatorHelper
    {
        /// <summary>Maximum size of a platform or station we use for searching forward and backward</summary>
        const float maxPlatformOrStationSize = 10000f;

        // Result of calculation
        public enum DistanceResult
        {
            Valid,
            Behind,
            OffPath
        }

        // We use this traveller as the basis of the calculations.
        Traveller refTraveller;
        float Distance;

        public TDBTravellerDistanceCalculatorHelper(Traveller traveller)
        {
            refTraveller = traveller;
        }

        public DistanceResult CalculateToPoint(int TileX, int TileZ, float X, float Y, float Z)
        {
            Traveller poiTraveller;
            poiTraveller = new Traveller(refTraveller);

            // Find distance once
            Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, maxPlatformOrStationSize);

            // If valid
            if (Distance > 0)
            {
                return DistanceResult.Valid;
            }
            else
            {
                // Go to opposite direction
                poiTraveller = new Traveller(refTraveller, Traveller.TravellerDirection.Backward);

                Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, maxPlatformOrStationSize);
                // If valid, it is behind us
                if (Distance > 0)
                {
                    return DistanceResult.Behind;
                }
            }

            // Otherwise off path
            return DistanceResult.OffPath;
        }
    }

    public class ActivityTaskPassengerStopAt : ActivityTask
    {
        readonly Simulator Simulator;

        public DateTime SchArrive;
        public DateTime SchDepart;
        public DateTime? ActArrive;
        public DateTime? ActDepart;
        public PlatformItem PlatformEnd1;
        public PlatformItem PlatformEnd2;

        public double BoardingS;   // MSTS calls this the Load/Unload time. Cargo gets loaded, but passengers board the train.
        public double BoardingEndS;
        int TimerChk;
        bool arrived;
        bool maydepart;
        public bool LogStationStops;
        public string LogStationLogFile;
        public float distanceToNextSignal = -1;
        public Train MyPlayerTrain; // Shortcut to player train

        public bool ldbfevaldepartbeforeboarding = false;//Debrief Eval
        public static List<string> DbfEvalDepartBeforeBoarding = new List<string>();//Debrief Eval

        public ActivityTaskPassengerStopAt(Simulator simulator, ActivityTask prev, DateTime Arrive, DateTime Depart,
                 PlatformItem Platformend1, PlatformItem Platformend2)
        {
            Simulator = simulator;
            SchArrive = Arrive;
            SchDepart = Depart;
            PlatformEnd1 = Platformend1;
            PlatformEnd2 = Platformend2;
            PrevTask = prev;
            if (prev != null)
                prev.NextTask = this;
            DisplayMessage = "";

            LogStationStops = false;
            LogStationLogFile = null;
        }

        internal ActivityTaskPassengerStopAt(Simulator simulator)
        {
            Simulator = simulator;
        }

        /// <summary>
        /// Determines if the train is at station.
        /// Tests for either the front or the rear of the train is within the platform.
        /// </summary>
        /// <returns></returns>
        public bool IsAtStation(Train myTrain)
        {
            if (myTrain.StationStops.Count == 0) return false;
            var thisStation = myTrain.StationStops[0];
            if (myTrain.StationStops[0].SubrouteIndex != myTrain.TCRoute.activeSubpath) return false;
            return myTrain.CheckStationPosition(thisStation.PlatformItem, thisStation.Direction, thisStation.TCSectionIndex);
        }

        public bool IsMissedStation()
        {
            // Check if station is in present train path

            if (MyPlayerTrain.StationStops.Count == 0 ||
                MyPlayerTrain.TCRoute.activeSubpath != MyPlayerTrain.StationStops[0].SubrouteIndex || !(MyPlayerTrain.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || MyPlayerTrain.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL))
            {
                return (false);
            }

            return MyPlayerTrain.IsMissedPlatform(200.0f);
        }

        public override void NotifyEvent(ActivityEventType EventType)
        {

            MyPlayerTrain = Simulator.OriginalPlayerTrain;
            // The train is stopped.
            if (EventType == ActivityEventType.TrainStop)
            {
                if (MyPlayerTrain.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING && IsAtStation(MyPlayerTrain)  ||
                    MyPlayerTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING && (MyPlayerTrain as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.STATION_STOP)
                {
                    if (Simulator.TimetableMode || MyPlayerTrain.StationStops.Count == 0)
                    {
                        // If yes, we arrived
                        if (ActArrive == null)
                        {
                            ActArrive = new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime));
                        }

                        arrived = true;

                        // Figure out the boarding time
                        // <CSComment> No midnight checks here? There are some in Train.CalculateDepartTime
                        double plannedBoardingS = (SchDepart - SchArrive).TotalSeconds;
                        double punctualBoardingS = (SchDepart - ActArrive).Value.TotalSeconds;
                        double expectedBoardingS = plannedBoardingS > 0 ? plannedBoardingS : PlatformEnd1.PlatformMinWaitingTime;
                        BoardingS = punctualBoardingS;                                     // default is leave on time
                        if (punctualBoardingS < expectedBoardingS)                         // if not enough time for boarding
                        {
                            if (plannedBoardingS > 0 && plannedBoardingS < PlatformEnd1.PlatformMinWaitingTime)
                            { // and tight schedule
                                BoardingS = plannedBoardingS;                              // leave late with no recovery of time
                            }
                            else
                            {                                                       // generous schedule
                                BoardingS = Math.Max(
                                    punctualBoardingS,                                     // leave on time
                                    PlatformEnd1.PlatformMinWaitingTime);                  // leave late with some recovery
                            }
                        }
                        // ActArrive is usually same as ClockTime
                        BoardingEndS = Simulator.ClockTime + BoardingS;
                        // But not if game starts after scheduled arrival. In which case actual arrival is assumed to be same as schedule arrival.
                        double sinceActArriveS = (new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime))
                                                - ActArrive).Value.TotalSeconds;
                        BoardingEndS -= sinceActArriveS;

                        ldbfevaldepartbeforeboarding = false; // reset flag. Debrief Eval
                    }
                    else
                    {
                    // <CSComment> MSTS mode - player
                        if (Simulator.GameTime < 2)
                        {
                            // If the simulation starts with a scheduled arrive in the past, assume the train arrived on time.
                            if (SchArrive < new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime)))
                            {
                                ActArrive = SchArrive;
                            }
                        }
                        BoardingS = (double)MyPlayerTrain.StationStops[0].ComputeStationBoardingTime(Simulator.PlayerLocomotive.Train);
                        if (BoardingS > 0 || ((double)(SchDepart - SchArrive).TotalSeconds > 0 &&
                            MyPlayerTrain.PassengerCarsNumber == 1 && MyPlayerTrain.Cars.Count > 10 ))
                        {
                            // accepted station stop because either freight train or passenger train or fake passenger train with passenger car on platform or fake passenger train
                            // with Scheduled Depart > Scheduled Arrive
                            // ActArrive is usually same as ClockTime
                            BoardingEndS = Simulator.ClockTime + BoardingS;

                            if (ActArrive == null)
                            {
                                ActArrive = new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime));
                            }

                            arrived = true;
                            // But not if game starts after scheduled arrival. In which case actual arrival is assumed to be same as schedule arrival.
                            double sinceActArriveS = (new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime))
                                                    - ActArrive).Value.TotalSeconds;
                            BoardingEndS -= sinceActArriveS;
                            double SchDepartS = SchDepart.Subtract(new DateTime()).TotalSeconds;
                            BoardingEndS = CompareTimes.LatestTime((int)SchDepartS, (int)BoardingEndS);

                        }

                        ldbfevaldepartbeforeboarding = false; // reset flag. Debrief Eval
                    }
                    if  (MyPlayerTrain.NextSignalObject[0] != null)
                           distanceToNextSignal =  MyPlayerTrain.NextSignalObject[0].DistanceTo(MyPlayerTrain.FrontTDBTraveller);
                }
            }
            else if (EventType == ActivityEventType.TrainStart)
            {
                // Train has started, we have things to do if we arrived before
                if (arrived)
                {
                    ActDepart = new DateTime().Add(TimeSpan.FromSeconds(Simulator.ClockTime));
                    CompletedAt = ActDepart.Value;
                    // Completeness depends on the elapsed waiting time
                    IsCompleted = maydepart;
                    if (MyPlayerTrain.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING)
                       MyPlayerTrain.ClearStation(PlatformEnd1.LinkedPlatformItemId, PlatformEnd2.LinkedPlatformItemId, true);

                    // Debrief Eval: departure before boarding completed
                    if (!maydepart && !ldbfevaldepartbeforeboarding)
                    {
                        var train = Simulator.PlayerLocomotive.Train;
                        ldbfevaldepartbeforeboarding = true;
                        DbfEvalDepartBeforeBoarding.Add(PlatformEnd1.Station);
                        train.DbfEvalValueChanged = true;
                    }

                    if (LogStationStops)
                    {
                        StringBuilder stringBuild = new StringBuilder();
                        char separator = (char)(DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), Simulator.Settings.DataLoggerSeparator);
                        stringBuild.Append(PlatformEnd1.Station);
                        stringBuild.Append(separator);
                        stringBuild.Append(SchArrive.ToString("HH:mm:ss"));
                        stringBuild.Append(separator);
                        stringBuild.Append(SchDepart.ToString("HH:mm:ss"));
                        stringBuild.Append(separator);
                        stringBuild.Append(ActArrive.HasValue ? ActArrive.Value.ToString("HH:mm:ss") : "-");
                        stringBuild.Append(separator);
                        stringBuild.Append(ActDepart.HasValue ? ActDepart.Value.ToString("HH:mm:ss") : "-");

                        TimeSpan delay = ActDepart.HasValue ? (ActDepart - SchDepart).Value : TimeSpan.Zero;
                        stringBuild.Append(separator);
                        stringBuild.AppendFormat("{0}:{1}:{2}", delay.Hours.ToString("00"), delay.Minutes.ToString("00"), delay.Seconds.ToString("00"));
                        stringBuild.Append(separator);
                        stringBuild.Append(maydepart ? "Completed" : "NotCompleted");
                        stringBuild.Append("\n");
                        File.AppendAllText(LogStationLogFile, stringBuild.ToString());
                    }
                }
            }
            else if (EventType == ActivityEventType.Timer)
            {
                // Waiting at a station
                if (arrived)
                {
                    var remaining = (int)Math.Ceiling(BoardingEndS - Simulator.ClockTime);
                    if (remaining < 1) DisplayColor = Color.LightGreen;
                    else if (remaining < 11) DisplayColor = new Color(255, 255, 128);
                    else DisplayColor = Color.White;

                    if (remaining < 120 && (MyPlayerTrain.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING))
                    {
                        MyPlayerTrain.ClearStation(PlatformEnd1.LinkedPlatformItemId, PlatformEnd2.LinkedPlatformItemId, false);
                    }

                    // Still have to wait
                    if (remaining > 0)
                    {
                        DisplayMessage = Simulator.Catalog.GetStringFmt("Passenger boarding completes in {0:D2}:{1:D2}",
                            remaining / 60, remaining % 60);
                    }
                    // May depart
                    else if (!maydepart)
                    {
                        // check if signal ahead is cleared - if not, do not allow depart
                        if (distanceToNextSignal >= 0 && distanceToNextSignal< 300 && MyPlayerTrain.NextSignalObject[0] != null &&
                            MyPlayerTrain.NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP
                            && MyPlayerTrain.NextSignalObject[0].hasPermission != SignalObject.Permission.Granted)
                        {
                            DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. Waiting for signal ahead to clear.");
                        }
                        else
                        {
                            maydepart = true;
                            DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. You may depart now.");
                            if (MyPlayerTrain.IsActualPlayerTrain) Simulator.SoundNotify = Event.PermissionToDepart;
                        }

                        // if last task, show closure window
                        // also set times in logfile

                        if (NextTask == null)
                        {
                            if (LogStationStops)
                            {
                                StringBuilder stringBuild = new StringBuilder();
                                char separator = (char)(DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), Simulator.Settings.DataLoggerSeparator);
                                stringBuild.Append(PlatformEnd1.Station);
                                stringBuild.Append(separator);
                                stringBuild.Append(SchArrive.ToString("HH:mm:ss"));
                                stringBuild.Append(separator);
                                stringBuild.Append("-");
                                stringBuild.Append(separator);
                                stringBuild.Append(ActArrive.HasValue ? ActArrive.Value.ToString("HH:mm:ss") : "-");
                                stringBuild.Append(separator);
                                stringBuild.Append("-");
                                stringBuild.Append(separator);

                                TimeSpan delay = ActArrive.HasValue ? (ActArrive - SchArrive).Value : TimeSpan.Zero;
                                if (delay.CompareTo(TimeSpan.Zero) < 0)
                                {
                                    delay = TimeSpan.Zero - delay;
                                    stringBuild.AppendFormat("-{0}:{1}:{2}", delay.Hours.ToString("00"), delay.Minutes.ToString("00"), delay.Seconds.ToString("00"));
                                }
                                else
                                {
                                    stringBuild.AppendFormat("{0}:{1}:{2}", delay.Hours.ToString("00"), delay.Minutes.ToString("00"), delay.Seconds.ToString("00"));
                                }

                                stringBuild.Append(separator);
                                stringBuild.Append("Final stop");
                                stringBuild.Append("\n");
                                File.AppendAllText(LogStationLogFile, stringBuild.ToString());
                            }

                            IsCompleted = true;
                        }
                    }
                }
                else
                {
                    // Checking missed station
                    int tmp = (int)(Simulator.ClockTime % 10);
                    if (tmp != TimerChk)
                    {
                        if (IsMissedStation() && (MyPlayerTrain.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING))
                        {
                            MyPlayerTrain.ClearStation(PlatformEnd1.LinkedPlatformItemId, PlatformEnd2.LinkedPlatformItemId, true);
                            IsCompleted = false;

                            if (LogStationStops)
                            {
                                StringBuilder stringBuild = new StringBuilder();
                                char separator = (char)(DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), Simulator.Settings.DataLoggerSeparator);
                                stringBuild.Append(PlatformEnd1.Station);
                                stringBuild.Append(separator);
                                stringBuild.Append(SchArrive.ToString("HH:mm:ss"));
                                stringBuild.Append(separator);
                                stringBuild.Append(SchDepart.ToString("HH:mm:ss"));
                                stringBuild.Append(separator);
                                stringBuild.Append("-");
                                stringBuild.Append(separator);
                                stringBuild.Append("-");
                                stringBuild.Append(separator);
                                stringBuild.Append("-");
                                stringBuild.Append(separator);
                                stringBuild.Append("Missed");
                                stringBuild.Append("\n");
                                File.AppendAllText(LogStationLogFile, stringBuild.ToString());
                            }
                        }
                    }
                }
            }
        }

        public override void Save(BinaryWriter outf)
        {
            Int64 noval = -1;
            outf.Write((Int32)1);

            base.Save(outf);

            outf.Write((Int64)SchArrive.Ticks);
            outf.Write((Int64)SchDepart.Ticks);
            if (ActArrive == null) outf.Write(noval); else outf.Write((Int64)ActArrive.Value.Ticks);
            if (ActDepart == null) outf.Write(noval); else outf.Write((Int64)ActDepart.Value.Ticks);
            outf.Write((Int32)PlatformEnd1.TrItemId);
            outf.Write((Int32)PlatformEnd2.TrItemId);
            outf.Write((double)BoardingEndS);
            outf.Write((double)BoardingS);
            outf.Write((Int32)TimerChk);
            outf.Write(arrived);
            outf.Write(maydepart);
            outf.Write(distanceToNextSignal);
        }

        public override void Restore(BinaryReader inf)
        {
            Int64 rdval;

            base.Restore(inf);

            SchArrive = new DateTime(inf.ReadInt64());
            SchDepart = new DateTime(inf.ReadInt64());
            rdval = inf.ReadInt64();
            ActArrive = rdval == -1 ? (DateTime?)null : new DateTime(rdval);
            rdval = inf.ReadInt64();
            ActDepart = rdval == -1 ? (DateTime?)null : new DateTime(rdval);
            PlatformEnd1 = Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            PlatformEnd2 = Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            BoardingEndS = inf.ReadDouble();
            BoardingS = inf.ReadDouble();
            TimerChk = inf.ReadInt32();
            arrived = inf.ReadBoolean();
            maydepart = inf.ReadBoolean();
            distanceToNextSignal = inf.ReadSingle();
        }
    }

    /// <summary>
    /// This class adds attributes around the event objects parsed from the ACT file.
    /// Note: Can't add attributes to the event objects directly as ACTFile.cs is not just used by 
    /// RunActivity.exe but also by Menu.exe and MenuWPF.exe and these executables lack most of the ORTS classes.
    /// </summary>
    public abstract class EventWrapper
    {
        public Orts.Formats.Msts.Event ParsedObject;     // Points to object parsed from file *.act
        public int OriginalActivationLevel; // Needed to reset .ActivationLevel
        public int TimesTriggered;          // Needed for evaluation after activity ends
        public Boolean IsDisabled;          // Used for a reversible event to prevent it firing again until after it has been reset.
        protected Simulator Simulator;
        public Train Train;              // Train involved in event; if null actual or original player train

        public EventWrapper(Orts.Formats.Msts.Event @event, Simulator simulator)
        {
            ParsedObject = @event;
            Simulator = simulator;
            Train = null;
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(TimesTriggered);
            outf.Write(IsDisabled);
            outf.Write(ParsedObject.Activation_Level);
        }

        public virtual void Restore(BinaryReader inf)
        {
            TimesTriggered = inf.ReadInt32();
            IsDisabled = inf.ReadBoolean();
            ParsedObject.Activation_Level = inf.ReadInt32();
        }

        /// <summary>
        /// After an event is triggered, any message is displayed independently by ActivityWindow.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public virtual Boolean Triggered(Activity activity)
        {  // To be overloaded by subclasses
            return false;  // Compiler insists something is returned.
        }

        /// <summary>
        /// Acts on the outcomes and then sets ActivationLevel = 0 to prevent re-use.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>true if entire activity ends here whether it succeeded or failed</returns>
        public Boolean IsActivityEnded(Activity activity)
        {

            if (this.ParsedObject.Reversible)
            {
                // Stop this event being actioned
                this.IsDisabled = true;
            }
            else
            {
                // Stop this event being monitored
                this.ParsedObject.Activation_Level = 0;
            }
            // No further action if this reversible event has been triggered before
            if (this.TimesTriggered > 1) { return false; }

            if (this.ParsedObject.Outcomes == null) { return false; }

            // Set Activation Level of each event in the Activate list to 1.
            // Uses lambda expression => for brevity.
            foreach (int eventId in ParsedObject.Outcomes.ActivateList)
            {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level = 1;
            }
            foreach (int eventId in ParsedObject.Outcomes.RestoreActLevelList)
            {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level = item.OriginalActivationLevel;
            }
            foreach (int eventId in ParsedObject.Outcomes.DecActLevelList)
            {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level += -1;
            }
            foreach (int eventId in ParsedObject.Outcomes.IncActLevelList)
            {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                {
                    item.ParsedObject.Activation_Level += +1;
                }
            }

            // Activity sound management

            if (this.ParsedObject.ORTSActSoundFile != null || (this.ParsedObject.Outcomes != null && this.ParsedObject.Outcomes.ActivitySound != null))
            {
                if (activity.triggeredEventWrapper == null) activity.triggeredEventWrapper = this;
            }

            if (this.ParsedObject.ORTSWeatherChange != null || (this.ParsedObject.Outcomes != null && this.ParsedObject.Outcomes.ORTSWeatherChange != null))
            {
                if (activity.triggeredEventWrapper == null) activity.triggeredEventWrapper = this;
            }

            if (this.ParsedObject.Outcomes.ActivityFail != null)
            {
                activity.IsSuccessful = false;
                return true;
            }
            if (this.ParsedObject.Outcomes.ActivitySuccess == true)
            {
                activity.IsSuccessful = true;
                return true;
            }
            if (this.ParsedObject.Outcomes.RestartWaitingTrain != null && this.ParsedObject.Outcomes.RestartWaitingTrain.WaitingTrainToRestart != "")
            {
                var restartWaitingTrain = this.ParsedObject.Outcomes.RestartWaitingTrain;
                Simulator.RestartWaitingTrain(restartWaitingTrain);
            }
            return false;
        }
     
    }

    public class EventCategoryActionWrapper : EventWrapper
    {
        SidingItem SidingEnd1;
        SidingItem SidingEnd2;
        List<string> ChangeWagonIdList;   // Wagons to be assembled, picked up or dropped off.

        public EventCategoryActionWrapper(Orts.Formats.Msts.Event @event, Simulator simulator)
            : base(@event, simulator)
        {
            var e = this.ParsedObject as EventCategoryAction;
            if (e.SidingId != null)
            {
                var i = e.SidingId.Value;
                try
                {
                    SidingEnd1 = Simulator.TDB.TrackDB.TrItemTable[i] as SidingItem;
                    i = SidingEnd1.LinkedSidingId;
                    SidingEnd2 = Simulator.TDB.TrackDB.TrItemTable[i] as SidingItem;
                }
                catch (IndexOutOfRangeException)
                {
                    Trace.TraceWarning("Siding {0} is not in track database.", i);
                }
                catch (NullReferenceException)
                {
                    Trace.TraceWarning("Item {0} in track database is not a siding.", i);
                }
            }
        }

        override public Boolean Triggered(Activity activity)
        {
            Train OriginalPlayerTrain = Simulator.OriginalPlayerTrain;
            var e = this.ParsedObject as EventCategoryAction;
            if (e.WagonList != null)
            {                     // only if event involves wagons
                if (ChangeWagonIdList == null)
                {           // populate the list only once - the first time that ActivationLevel > 0 and so this method is called.
                    ChangeWagonIdList = new List<string>();
                    foreach (var item in e.WagonList.WorkOrderWagonList)
                    {
                        ChangeWagonIdList.Add(String.Format("{0} - {1}", ((int)item.UID & 0xFFFF0000) >> 16, (int)item.UID & 0x0000FFFF)); // form the .CarID
                    }
                }
            }
            var triggered = false;
            Train consistTrain;
            switch (e.Type)
            {
                case EventType.AllStops:
                    triggered = activity.Tasks.Count > 0 && activity.Last.IsCompleted != null;
                    break;
                case EventType.AssembleTrain:
                    consistTrain = matchesConsist(ChangeWagonIdList);
                    if (consistTrain != null)
                    {
                        triggered = true;
                    }
                    break;
                case EventType.AssembleTrainAtLocation:
                    if (atSiding(OriginalPlayerTrain.FrontTDBTraveller, OriginalPlayerTrain.RearTDBTraveller, this.SidingEnd1, this.SidingEnd2))
                    {
                        consistTrain = matchesConsist(ChangeWagonIdList);
                        triggered = consistTrain != null;
                    }
                    break;
                case EventType.DropOffWagonsAtLocation:
                        consistTrain = matchesConsistNoOrder(ChangeWagonIdList);
                    if (consistTrain != null)
                    {
                        if (consistTrain.TrainType == Train.TRAINTYPE.STATIC)
                        {
                            if (atSiding(consistTrain.FrontTDBTraveller, consistTrain.RearTDBTraveller, this.SidingEnd1, this.SidingEnd2))
                            {
                                triggered = true;
                            }
                        }
                    }
                    break;
                case EventType.PickUpPassengers:
                    break;
                case EventType.PickUpWagons: // PickUpWagons is independent of location or siding
                    triggered = includesWagons(OriginalPlayerTrain, ChangeWagonIdList);
                    break;
                case EventType.ReachSpeed:
                    triggered = (Math.Abs(Simulator.PlayerLocomotive.SpeedMpS) >= e.SpeedMpS);
                    break;
            }
            return triggered;
        }
        /// <summary>
        /// Finds the train that contains exactly the wagons (and maybe loco) in the list in the correct sequence.
        /// </summary>
        /// <param name="wagonIdList"></param>
        /// <returns>train or null</returns>
        private Train matchesConsist(List<string> wagonIdList)
        {
            foreach (var trainItem in Simulator.Trains)
            {
                if (trainItem.Cars.Count == wagonIdList.Count)
                {
                    // Compare two lists to make sure wagons are in expected sequence.
                    bool listsMatch = true;
                    //both lists with the same order
                    for (int i = 0; i < trainItem.Cars.Count; i++)
                    {
                        if (trainItem.Cars.ElementAt(i).CarID != wagonIdList.ElementAt(i)) { listsMatch = false; break; }
                    }
                    if (!listsMatch)
                    {//different order list
                        listsMatch = true;
                        for (int i = trainItem.Cars.Count; i > 0; i--)
                        {
                            if (trainItem.Cars.ElementAt(i - 1).CarID != wagonIdList.ElementAt(trainItem.Cars.Count - i)) { listsMatch = false; break; }
                        }
                    }
                    if (listsMatch) return trainItem;
                }
            }
            return null;
        }
        /// <summary>
        /// Finds the train that contains the wagons in the list. 
        /// Exact order is not required.
        /// Some lists may only contain the first and last wagon. Check that first and last wagon match with those two wagons in the activity list.
        /// </summary>
        /// <param name="wagonIdList"></param>
        /// <returns>train or null</returns>
        private Train matchesConsistNoOrder(List<string> wagonIdList)
        {
            foreach (var trainItem in Simulator.Trains)
            {
                int nWagonListCars = 0;
                foreach (var item in trainItem.Cars)
                {
                    if (wagonIdList.Contains(item.CarID))
                {
                        nWagonListCars++;
                    }
                    if (nWagonListCars == wagonIdList.Count)
                    {
                        return trainItem;
                    }
                }
                }
               
            return null;
        }
        /// <summary>
        /// Like MSTS, do not check for unlisted wagons as the wagon list may be shortened for convenience to contain
        /// only the first and last wagon or even just the first wagon.
        /// </summary>
        /// <param name="train"></param>
        /// <param name="wagonIdList"></param>
        /// <returns>True if all listed wagons are part of the given train.</returns>
        static bool includesWagons(Train train, List<string> wagonIdList)
        {
            foreach (var item in wagonIdList)
            {
                if (train.Cars.Find(car => car.CarID == item) == null) return false;
            }
            // train speed < 1
            return (Math.Abs(train.SpeedMpS) <= 1 ? true : false);
        }
        /// <summary>
        /// Like MSTS, do not check for unlisted wagons as the wagon list may be shortened for convenience to contain
        /// only the first and last wagon or even just the first wagon.
        /// </summary>
        /// <param name="train"></param>
        /// <param name="wagonIdList"></param>
        /// <returns>True if all listed wagons are not part of the given train.</returns>
        static bool excludesWagons(Train train, List<string> wagonIdList)
        {
            // The Cars list is a global list that includes STATIC cars.  We need to make sure that the active train/car is processed only.
            if (train.TrainType == Train.TRAINTYPE.STATIC)
                return true;

            bool lNotFound = false;
            foreach (var item in wagonIdList)
            {
                //take in count each item in wagonIdList 
                if (train.Cars.Find(car => car.CarID == item) == null)
                {
                    lNotFound = true; //wagon not part of the train
                }
                else
                {
                    lNotFound = false; break;//wagon still part of the train
                }
            }
            return lNotFound;
        }
        /// <summary>
        /// Like platforms, checking that one end of the train is within the siding.
        /// </summary>
        /// <param name="frontPosition"></param>
        /// <param name="rearPosition"></param>
        /// <param name="sidingEnd1"></param>
        /// <param name="sidingEnd2"></param>
        /// <returns>true if both ends of train within siding</returns>
        static bool atSiding(Traveller frontPosition, Traveller rearPosition, SidingItem sidingEnd1, SidingItem sidingEnd2)
        {
            if (sidingEnd1 == null || sidingEnd2 == null)
            {
                return true;
            }

            TDBTravellerDistanceCalculatorHelper helper;
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceEnd1;
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceEnd2;

            // Front calcs
            helper = new TDBTravellerDistanceCalculatorHelper(frontPosition);

            distanceEnd1 = helper.CalculateToPoint(sidingEnd1.TileX,
                    sidingEnd1.TileZ, sidingEnd1.X, sidingEnd1.Y, sidingEnd1.Z);
            distanceEnd2 = helper.CalculateToPoint(sidingEnd2.TileX,
                    sidingEnd2.TileZ, sidingEnd2.X, sidingEnd2.Y, sidingEnd2.Z);

            // If front between the ends of the siding
            if (((distanceEnd1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid)
                || (distanceEnd1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind)))
            {
                return true;
            }

            // Rear calcs
            helper = new TDBTravellerDistanceCalculatorHelper(rearPosition);

            distanceEnd1 = helper.CalculateToPoint(sidingEnd1.TileX,
                    sidingEnd1.TileZ, sidingEnd1.X, sidingEnd1.Y, sidingEnd1.Z);
            distanceEnd2 = helper.CalculateToPoint(sidingEnd2.TileX,
                    sidingEnd2.TileZ, sidingEnd2.X, sidingEnd2.Y, sidingEnd2.Z);

            // If rear between the ends of the siding
            if (((distanceEnd1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid)
                || (distanceEnd1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind)))
            {
                return true;
            }

            return false;
        }
    }

    public class EventCategoryLocationWrapper : EventWrapper
    {
        public EventCategoryLocationWrapper(Orts.Formats.Msts.Event @event, Simulator simulator)
            : base(@event, simulator)
        {
        }

        override public Boolean Triggered(Activity activity)
        {
            var triggered = false;
            var e = this.ParsedObject as Orts.Formats.Msts.EventCategoryLocation;
            var train = Simulator.PlayerLocomotive.Train;
            if (ParsedObject.TrainService != "" && Train != null)
            {
                if (Train.FrontTDBTraveller == null) return triggered;
                train = Train;
            }
            Train = train;
            if (e.TriggerOnStop)
            {
                // Is train still moving?
                if (Math.Abs(train.SpeedMpS) > 0.032f)
                {
                    return triggered;
                }
            }
            var trainFrontPosition = new Traveller(train.nextRouteReady && train.TCRoute.activeSubpath > 0 && train.TCRoute.ReversalInfo[train.TCRoute.activeSubpath - 1].Valid ?
                train.RearTDBTraveller : train.FrontTDBTraveller); // just after reversal the old train front position must be considered
            var distance = trainFrontPosition.DistanceTo(e.TileX, e.TileZ, e.X, trainFrontPosition.Y, e.Z, e.RadiusM);
            if (distance == -1)
            {
                trainFrontPosition.ReverseDirection();
                distance = trainFrontPosition.DistanceTo(e.TileX, e.TileZ, e.X, trainFrontPosition.Y, e.Z, e.RadiusM);
                if (distance == -1)
                    return triggered;
            }
            if (distance < e.RadiusM) { triggered = true; }
            return triggered;
        }
    }

    public class EventCategoryTimeWrapper : EventWrapper
    {

        public EventCategoryTimeWrapper(Orts.Formats.Msts.Event @event, Simulator simulator)
            : base(@event, simulator)
        {
        }

        override public Boolean Triggered(Activity activity)
        {
            var e = this.ParsedObject as Orts.Formats.Msts.EventCategoryTime;
            if (e == null) return false;
            Train = Simulator.PlayerLocomotive.Train;
            var triggered = (e.Time <= (int)Simulator.ClockTime - activity.StartTimeS);
            return triggered;
        }
    }
}
