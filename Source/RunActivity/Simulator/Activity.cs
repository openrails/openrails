using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Popups;

namespace ORTS {
    public enum ActivityEventType {
        Timer,
        TrainStart,
        TrainStop,
        Couple,
        Uncouple
    }

    public class Activity {
        Simulator Simulator;
        
        // Passenger tasks
        public DateTime StartTime;
        public List<ActivityTask> Tasks = new List<ActivityTask>();
        public ActivityTask Current = null;
        double prevTrainSpeed = -1;

        // Freight events
        public List<EventWrapper> EventList = new List<EventWrapper>();
        public Boolean IsComplete;          // true once activity is completed.
        public Boolean IsSuccessful;        // status of completed activity
        public Nullable<int> StartTimeS;    // Clock time in seconds when activity was launched.
        public EventWrapper TriggeredEvent; // Indicates the currently triggered event whose data the ActivityWindow will pop up to display.
		public bool ActivityUpdated = false;

        private Activity(BinaryReader inf, Simulator simulator) {
            Simulator = simulator;
            RestoreThis(inf);
        }

        public Activity(ACTFile actFile, Simulator simulator) {
            Simulator = simulator;  // Save for future use.
            Player_Service_Definition sd;
            sd = actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition;
            if (sd != null) {
                if (sd.Player_Traffic_Definition.Player_Traffic_List.Count > 0) {
                    PlatformItem Platform = null;
                    ActivityTask task = null;
                    foreach (var i in sd.Player_Traffic_Definition.Player_Traffic_List) {
                        Platform = Simulator.TDB.TrackDB.TrItemTable[i.PlatformStartID] as PlatformItem;
                        if (Platform != null) {
                            Tasks.Add(task = new ActivityTaskPassengerStopAt(task,
                                i.ArrivalTime,
                                i.DepartTime,
                                Platform, Simulator.TDB.TrackDB.TrItemTable[Platform.LinkedPlatformItemId] as PlatformItem));
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
            foreach (var i in parsedEventList) {
                if (i is EventCategoryAction) {
                    EventList.Add(new EventCategoryActionWrapper(i, Simulator));
                }
                if (i is EventCategoryLocation) {
                    EventList.Add(new EventCategoryLocationWrapper(i, Simulator));
                }
                if (i is EventCategoryTime) {
                    EventList.Add(new EventCategoryTimeWrapper(i, Simulator));
                }
                EventWrapper eventAdded = EventList.Last();
                eventAdded.OriginalActivationLevel = i.Activation_Level;
            }
        }

        public ActivityTask Last {
            get {
                return Tasks.Count == 0 ? null : Tasks[Tasks.Count - 1];
            }
        }

        public bool IsFinished {
            get {
                return Tasks.Count == 0 ? false : Last.IsCompleted != null;
            }
        }

        public void Update() {
            // Update freight events
            // Set the clock first time through. Can't set in the Activity constructor as Simulator.ClockTime is still 0 then.
            if (!StartTimeS.HasValue) { StartTimeS = (int)Simulator.ClockTime; }
            if (this.IsComplete == false) {
                foreach (var i in EventList) {
                    // Once an event has fired, we don't respond to any more events until that has been acknowledged.
                    if (this.TriggeredEvent != null) { break; }

                    if (i != null && i.ParsedObject.Activation_Level > 0) {
                        if (i.TimesTriggered < 1 || i.ParsedObject.Reversible) {
                            if (i.Triggered(this)) {
                                if (i.IsDisabled == false) {
                                    i.TimesTriggered += 1;
                                    if (i.IsActivityEnded(this)) {
                                        IsComplete = true;
                                    }
                                    this.TriggeredEvent = i;    // Note this for Viewer and ActivityWindow to use.
                                                                // Do this after IsActivityEnded() so values are ready for ActivityWindow
                                }
                            } else {
                                if (i.ParsedObject.Reversible) {
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
            //   if (Current.IsCompleted.HasValue && Current.IsCompleted.Value)
            // (see http://stackoverflow.com/questions/56518/c-is-there-any-difference-between-bool-and-nullablebool)
            {
                Current = Current.NextTask;
            }

            if (Simulator.PlayerLocomotive.SpeedMpS == 0) {
                if (prevTrainSpeed != 0) {
                    prevTrainSpeed = 0;
                    Current.NotifyEvent(ActivityEventType.TrainStop);
                    if (Current.IsCompleted != null) {
                        Current = Current.NextTask;
                    }
                }
            } else {
                if (prevTrainSpeed == 0) {
                    prevTrainSpeed = Simulator.PlayerLocomotive.SpeedMpS;
                    Current.NotifyEvent(ActivityEventType.TrainStart);
                    if (Current.IsCompleted != null) {
                        Current = Current.NextTask;
                    }
                }
            }
        }

        public static void Save(BinaryWriter outf, Activity act) {
            Int32 noval = -1;
            if (act == null) {
                outf.Write(noval);
            } else {
                noval = 1;
                outf.Write(noval);
                act.Save(outf);
            }
        }

        public static Activity Restore(BinaryReader inf, Simulator simulator) {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == -1) {
                return null;
            } else {
                Activity act = new Activity(inf, simulator);
                return act;
            }
        }

        public void Save(BinaryWriter outf) {
            Int32 noval = -1;
            outf.Write((Int64)StartTime.Ticks);
            outf.Write((Int32)Tasks.Count);
            foreach (ActivityTask task in Tasks) {
                task.Save(outf);
            }
            if (Current == null) outf.Write(noval); else outf.Write((Int32)(Tasks.IndexOf(Current)));
            outf.Write(prevTrainSpeed);
        }

        private ActivityTask GetTask(BinaryReader inf) {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == 1)
                return new ActivityTaskPassengerStopAt();
            else
                return null;
        }

        public void RestoreThis(BinaryReader inf) {
            Int32 rdval;
            ActivityTask task;

            StartTime = new DateTime(inf.ReadInt64());
            rdval = inf.ReadInt32();
            for (int i = 0; i < rdval; i++) {
                task = GetTask(inf);
                task.Restore(inf);
                Tasks.Add(task);
            }
            rdval = inf.ReadInt32();
            Current = rdval == -1 ? null : Tasks[rdval];
            prevTrainSpeed = inf.ReadDouble();

            task = null;
            for (int i = 0; i < Tasks.Count; i++) {
                Tasks[i].PrevTask = task;
                if (task != null) task.NextTask = Tasks[i];
                task = Tasks[i];
            }
        }
    }

    public class ActivityTask {
        public bool? IsCompleted { get; internal set; }
        public ActivityTask PrevTask { get; internal set; }
        public ActivityTask NextTask { get; internal set; }
        public DateTime CompletedAt { get; internal set; }
        public string DisplayMessage { get; internal set; }
        public int SoundNotify = -1;

        public virtual void NotifyEvent(ActivityEventType EventType) {
        }

        public virtual void Save(BinaryWriter outf) {
            Int32 noval = -1;
            if (IsCompleted == null) outf.Write(noval); else outf.Write(IsCompleted.Value ? (Int32)1 : (Int32)0);
            outf.Write((Int64)CompletedAt.Ticks);
            outf.Write(DisplayMessage);
            outf.Write((Int32)SoundNotify);
        }

        public virtual void Restore(BinaryReader inf) {
            Int64 rdval;
            rdval = inf.ReadInt32();
            IsCompleted = rdval == -1 ? (bool?)null : rdval == 0 ? false : true;
            CompletedAt = new DateTime(inf.ReadInt64());
            DisplayMessage = inf.ReadString();
            SoundNotify = inf.ReadInt32();
        }
    }

    /// <summary>
    /// Helper class to calculate distances along the path
    /// </summary>
    public class TDBTravellerDistanceCalculatorHelper {
        // Result of calculation
        public enum DistanceResult {
            Valid,
            Behind,
            OffPath
        }

        // We use this traveller as the base of the calculations
        TDBTraveller refTraveller;
        float Distance;

        public TDBTravellerDistanceCalculatorHelper(TDBTraveller traveller) {
            refTraveller = traveller;
        }

        public DistanceResult CalculateToPoint(int TileX, int TileZ, float X, float Y, float Z) {
            TDBTraveller poiTraveller;
            poiTraveller = new TDBTraveller(refTraveller);

            // Find distance once
            Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, ref poiTraveller);

            // If valid
            if (Distance > 0) {
                return DistanceResult.Valid;
            } else {
                // Go to opposite direction
                poiTraveller = new TDBTraveller(refTraveller);
                poiTraveller.ReverseDirection();

                Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, ref poiTraveller);
                // If valid, it is behind us
                if (Distance > 0) {
                    return DistanceResult.Behind;
                }
            }

            // Otherwise off path
            return DistanceResult.OffPath;
        }
    }

    public class ActivityTaskPassengerStopAt : ActivityTask {
        public DateTime SchArrive;
        public DateTime SchDepart;
        public DateTime? ActArrive = null;
        public DateTime? ActDepart = null;
        public PlatformItem PlatformEnd1;
        public PlatformItem PlatformEnd2;

        double LoadUnload;
        int TimerChk = 0;
        bool arrived = false;
        bool maydepart = false;

        public ActivityTaskPassengerStopAt(ActivityTask prev, DateTime Arrive, DateTime Depart, PlatformItem Platformend1, PlatformItem Platformend2) {
            SchArrive = Arrive;
            SchDepart = Depart;
            PlatformEnd1 = Platformend1;
            PlatformEnd2 = Platformend2;
            PrevTask = prev;
            if (prev != null)
                prev.NextTask = this;
            DisplayMessage = "";
        }

        internal ActivityTaskPassengerStopAt() {
        }

        /// <summary>
        /// Determines if the train is at station.
        /// Tests for either the front or the rear of the train is within the platform.
        /// </summary>
        /// <returns></returns>
        public bool IsAtStation() {
            // Front calcs
            TDBTravellerDistanceCalculatorHelper helper =
                new TDBTravellerDistanceCalculatorHelper(Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller);
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend1;
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend2;

            distanceend1 = helper.CalculateToPoint(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
            distanceend2 = helper.CalculateToPoint(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

            // If front between the ends of the platform
            if ((distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid) || (
                distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind))
                return true;

            // Rear calcs
            helper =
                new TDBTravellerDistanceCalculatorHelper(Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller);

            distanceend1 = helper.CalculateToPoint(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
            distanceend2 = helper.CalculateToPoint(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

            // If rear between the ends of the platform
            if ((distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid) || (
                distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind))
                return true;

            // Otherwise not
            return false;
        }

        public bool IsMissedStation() {
            // Calc all distances
            TDBTravellerDistanceCalculatorHelper helper =
                new TDBTravellerDistanceCalculatorHelper(Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller);
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend1;
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend2;

            distanceend1 = helper.CalculateToPoint(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
            distanceend2 = helper.CalculateToPoint(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

            helper =
                new TDBTravellerDistanceCalculatorHelper(Program.Simulator.PlayerLocomotive.Train.RearTDBTraveller);

            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend3;
            TDBTravellerDistanceCalculatorHelper.DistanceResult distanceend4;
            distanceend3 = helper.CalculateToPoint(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
            distanceend4 = helper.CalculateToPoint(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

            // If all behind then missed
            return (distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend3 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend4 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind);
        }

        public override void NotifyEvent(ActivityEventType EventType) {

            //// Pop-up ActivityWindow
            //ORTS.Program.Viewer.ActivityWindow.Visible = true;
            ////ORTS.Program.Viewer.ActivityWindow.EventMessageBox.Add(new ORTS.Popups.TextFlow(ORTS.Program.Viewer.ActivityWindow.EventMessageBox.RemainingWidth, "Arrived at station"));
            //ORTS.Program.Viewer.ActivityWindow.Line.Add(new Label(ORTS.Program.Viewer.ActivityWindow.Line.RemainingWidth, ORTS.Program.Viewer.ActivityWindow.Line.RemainingHeight, " Message", LabelAlignment.Center));

            //ORTS.Program.Viewer.ActivityWindow = new ActivityWindow(ORTS.Program.Viewer.WindowManager, "message");
            //ORTS.Program.Viewer.ActivityWindow.Visible = true;

            // The train is stopped.
            if (EventType == ActivityEventType.TrainStop) {
                // Checking if the stopping is occuread at the scheduled platform.
                /*
                double dist1 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
                double dist2 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);
                if ( (dist1 >= 0 && dist2 <= 0) || (dist1 <= 0 && dist2 >= 0))
                */
                if (IsAtStation()) {

                    //// Pop-up ActivityWindow
                    //ORTS.Program.Viewer.ActivityWindow.Visible = true;
                    //Debug.WriteLine("ORTS.Program.Viewer.ActivityWindow.Visible = true;");
                    //ORTS.Program.Viewer.ActivityWindow.EventMessageBox.Add(new ORTS.Popups.TextFlow(ORTS.Program.Viewer.ActivityWindow.EventMessageBox.RemainingWidth, "Arrived at station"));

                    // If yes, we arrived
                    ActArrive = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));
                    arrived = true;

                    // Check if this is the last task in activity, then it is complete
                    if (NextTask == null) {
                        IsCompleted = true;
                        return;
                    }

                    // Figure out the load/unload time
                    if (SchDepart > ActArrive) {
                        // Depart as scheduled
                        LoadUnload = (SchDepart - ActArrive).Value.TotalSeconds;
                    }
                    if (LoadUnload < PlatformEnd1.PlatformMinWaitingTime) LoadUnload = PlatformEnd1.PlatformMinWaitingTime;
                    LoadUnload += Program.Simulator.ClockTime;
                }
            } else if (EventType == ActivityEventType.TrainStart) {
                // Train has started, we have things to do if we arrived before
                if (arrived) {
                    ActDepart = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));
                    CompletedAt = ActDepart.Value;
                    // Completeness is depend on the elapsed waiting time
                    IsCompleted = maydepart;
                }
            } else if (EventType == ActivityEventType.Timer) {
                // Waiting at a station
                if (arrived) {
                    double remaining = LoadUnload - Program.Simulator.ClockTime;
                    // Still have to wait
                    if (remaining > 0) {
                        DisplayMessage = string.Format("Passenger load/unload completes in {0:D2}:{1:D2}",
                            (int)(remaining / 60), (int)(remaining % 60));
                    }
                        // May depart
                    else if (!maydepart) {
                        maydepart = true;
                        DisplayMessage = "Passenger load/unload completed. You may depart now.";
                        SoundNotify = 60;
                    }
                } else {
                    // Checking missed station
                    int tmp = (int)(Program.Simulator.ClockTime % 10);
                    if (tmp != TimerChk) {
                        /*
                        double dist1 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd1.TileX,
                            PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
                        double dist2 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd2.TileX,
                            PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

                        // If both less than zero, station is missed
                        if (dist1 < 0 && dist2 < 0)
                        */
                        if (IsMissedStation()) {

                            //// Pop-up ActivityWindow
                            //ORTS.Program.Viewer.ActivityWindow.Visible = true;
                            //Debug.WriteLine("ORTS.Program.Viewer.ActivityWindow.Visible = true;"); 

                            IsCompleted = false;
                        }
                    }
                }
            }
        }

        public override void Save(BinaryWriter outf) {
            Int64 noval = -1;
            outf.Write((Int32)1);

            base.Save(outf);

            outf.Write((Int64)SchArrive.Ticks);
            outf.Write((Int64)SchDepart.Ticks);
            if (ActArrive == null) outf.Write(noval); else outf.Write((Int64)ActArrive.Value.Ticks);
            if (ActDepart == null) outf.Write(noval); else outf.Write((Int64)ActDepart.Value.Ticks);
            outf.Write((Int32)PlatformEnd1.TrItemId);
            outf.Write((Int32)PlatformEnd2.TrItemId);
            outf.Write((double)LoadUnload);
            outf.Write((Int32)TimerChk);
            outf.Write(arrived);
            outf.Write(maydepart);
        }

        public override void Restore(BinaryReader inf) {
            Int64 rdval;

            base.Restore(inf);

            SchArrive = new DateTime(inf.ReadInt64());
            SchDepart = new DateTime(inf.ReadInt64());
            rdval = inf.ReadInt64();
            ActArrive = rdval == -1 ? (DateTime?)null : new DateTime(rdval);
            rdval = inf.ReadInt64();
            ActDepart = rdval == -1 ? (DateTime?)null : new DateTime(rdval);
            PlatformEnd1 = Program.Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            PlatformEnd2 = Program.Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            LoadUnload = inf.ReadDouble();
            TimerChk = inf.ReadInt32();
            arrived = inf.ReadBoolean();
            maydepart = inf.ReadBoolean();
        }
    }

    /// <summary>
    /// This class adds attributes around the event objects parsed from the ACT file.
    /// Note: Can't add attributes to the event objects directly as ACTFile.cs is not just used by 
    /// RunActivity.exe but also by Menu.exe and MenuWPF.exe and these executables lack most of the ORTS classes.
    /// </summary>
    public class EventWrapper {
        protected Simulator Simulator;
        public MSTS.Event ParsedObject;     // Points to object parsed from file *.act
        public int OriginalActivationLevel; // Needed to reset .ActivationLevel
        public int TimesTriggered = 0;      // Needed for evaluation after activity ends
        public Boolean IsDisabled = false;  // Used for a reversible event to prevent it firing again until after it has been reset.

        public EventWrapper(MSTS.Event @event, Simulator simulator) {
            ParsedObject = @event;
            Simulator = simulator;
        }

        /// <summary>
        /// Any message is displayed independently by ActivityWindow.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public virtual Boolean Triggered(Activity activity) {  // To be overloaded by subclasses
            return false;  // Compiler insists something is returned.
        }

        /// <summary>
        /// Acts on the outcomes and then sets ActivationLevel = 0 to prevent re-use.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>true if entire activity ends here whether it succeeded or failed</returns>
        public Boolean IsActivityEnded(Activity activity) {

            if (this.ParsedObject.Reversible) {
                // Stop this event being actioned
                this.IsDisabled = true;
            } else {
                // Stop this event being monitored
                this.ParsedObject.Activation_Level = 0;
            }
            // No further action if this reversible event has been triggered before
            if (this.TimesTriggered > 1) { return false; }

            if (this.ParsedObject.Outcomes == null) { return false; }

            // Set Activation Level of each event in the Activate list to 1.
            // Uses lambda expression => for brevity.
            foreach (int eventId in ParsedObject.Outcomes.ActivateList) {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level = 1;
            }
            foreach (int eventId in ParsedObject.Outcomes.RestoreActLevelList) {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level = item.OriginalActivationLevel;
            }
            foreach (int eventId in ParsedObject.Outcomes.DecActLevelList) {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level += -1;
            }
            foreach (int eventId in ParsedObject.Outcomes.IncActLevelList) {
                foreach (var item in activity.EventList.Where(item => item.ParsedObject.ID == eventId))
                    item.ParsedObject.Activation_Level += +1;
            }

            if (this.ParsedObject.Outcomes.ActivityFail != null) {
                activity.IsSuccessful = false;
                return true;
            }
            if (this.ParsedObject.Outcomes.ActivitySuccess == true) {
                activity.IsSuccessful = true;
                return true;
            }
            return false;
        }

    }

    public class EventCategoryActionWrapper : EventWrapper {
        SidingItem SidingEnd1 = null;
        SidingItem SidingEnd2 = null;
        List<string> ChangeWagonIdList = null;   // Wagons to be assembled, picked up or dropped off.

        public EventCategoryActionWrapper(MSTS.Event @event, Simulator simulator)
            : base(@event, simulator) {
            var e = this.ParsedObject as EventCategoryAction;
            if (e.SidingId != null) {
                SidingEnd1 = Simulator.TDB.TrackDB.TrItemTable[e.SidingId.Value] as SidingItem;
                SidingEnd2 = Simulator.TDB.TrackDB.TrItemTable[SidingEnd1.Flags2] as SidingItem;
            }
        }

        override public Boolean Triggered(Activity activity) {
            Train PlayerTrain = Simulator.PlayerLocomotive.Train;
            var e = this.ParsedObject as EventCategoryAction;
            if (e.WagonList != null) {                     // only if event involves wagons
                if (ChangeWagonIdList == null) {           // populate the list only once - the first time that ActivationLevel > 0 and so this method is called.
                    ChangeWagonIdList = new List<string>();
                    foreach (var item in e.WagonList.WorkOrderWagonList) {
                        ChangeWagonIdList.Add(String.Format("{0} - {1}", ((int)item.UID & 0xFFFF0000) >> 16, (int)item.UID & 0x0000FFFF)); // form the .CarID
                    }
                }
            }
            var triggered = false;
            Train consistTrain;
            switch (e.Type) {
                case EventType.AllStops:
                    break;
                case EventType.AssembleTrain:
                    consistTrain = matchesConsist(ChangeWagonIdList);
                    if (consistTrain != null) {
                        triggered = true;
                    }
                    break;
                case EventType.AssembleTrainAtLocation:
                    consistTrain = matchesConsist(ChangeWagonIdList);
                    if (consistTrain != null) {
                        triggered = atSiding(consistTrain.FrontTDBTraveller, consistTrain.RearTDBTraveller, this.SidingEnd1, this.SidingEnd2);
                    }
                    break;

                // This is the action that should be taken by DropOffWagonsAtLocation.
                // MSTS - Marias Pass - Cutbank Grain Car Sorting - shows that, in MSTS, the event fires as soon as the train is in the siding.
                //case EventType.DropOffWagonsAtLocation:
                //    if (atSiding(Simulator.PlayerLocomotive.Train.FrontTDBTraveller, this.SidingEnd1, this.SidingEnd2)) {
                //        triggered = excludesWagons();
                //    }
                //    break;

                case EventType.DropOffWagonsAtLocation:
                    // A better name than DropOffWagonsAtLocation would be ArriveAtSidingWithWagons.
                    if (atSiding(PlayerTrain.FrontTDBTraveller, PlayerTrain.RearTDBTraveller, this.SidingEnd1, this.SidingEnd2)) {
                        triggered = includesWagons(PlayerTrain, ChangeWagonIdList);
                    }
                    break;
                case EventType.PickUpPassengers:
                    break;
                case EventType.PickUpWagons: // PickUpWagons is independent of location or siding
                    triggered = includesWagons(PlayerTrain, ChangeWagonIdList);
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
        private Train matchesConsist(List<string> wagonIdList) {
            foreach (var trainItem in Simulator.Trains) {
                if (trainItem.Cars.Count == wagonIdList.Count) {
                    // Compare two lists to make sure wagons are in expected sequence.
                    bool listsMatch = true;
                    for (int i = 0; i < trainItem.Cars.Count; i++) {
                        if (trainItem.Cars.ElementAt(i).CarID != wagonIdList.ElementAt(i)) { listsMatch = false;  break; }
                    }
                    if (listsMatch) return trainItem;
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
        private Boolean includesWagons(Train train, List<string> wagonIdList) {
            foreach (var item in wagonIdList) {
                if (train.Cars.Find(car => car.CarID == item) == null) return false;
            }
            return true;
        }

        /// <summary>
        /// Like platforms, checking that one end of the train is within the siding.
        /// </summary>
        /// <param name="frontPosition"></param>
        /// <param name="rearPosition"></param>
        /// <param name="sidingEnd1"></param>
        /// <param name="sidingEnd2"></param>
        /// <returns>true if both ends of train within siding</returns>
        private Boolean atSiding(TDBTraveller frontPosition, TDBTraveller rearPosition, SidingItem sidingEnd1, SidingItem sidingEnd2) {
            if (sidingEnd1 == null || sidingEnd2 == null) {
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
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind))) {
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
                && distanceEnd2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind))) {
                return true;
            }

            return false;
        }
    }

    public class EventCategoryLocationWrapper : EventWrapper {
        public EventCategoryLocationWrapper(MSTS.Event @event, Simulator simulator)
            : base(@event, simulator) {
        }

        override public Boolean Triggered(Activity activity) {
            var triggered = false;
            var e = this.ParsedObject as MSTS.EventCategoryLocation;
            if (e.TriggerOnStop) {
                // Is train still moving?
                if (Simulator.PlayerLocomotive.SpeedMpS != 0) {
                    return triggered;
                }
            }

            var trainFrontPosition = new TDBTraveller(Simulator.PlayerLocomotive.Train.FrontTDBTraveller);
            var distance = trainFrontPosition.DistanceTo(e.TileX, e.TileZ, e.X, trainFrontPosition.Y, e.Z);
            if (distance == -1) {
                trainFrontPosition.ReverseDirection();
                distance = trainFrontPosition.DistanceTo(e.TileX, e.TileZ, e.X, trainFrontPosition.Y, e.Z);
                if (distance == -1)
                    return triggered;
            }
            if (distance < e.RadiusM) { triggered = true; }
            return triggered;
        }
    }

    public class EventCategoryTimeWrapper : EventWrapper {
        public EventCategoryTimeWrapper(MSTS.Event @event, Simulator simulator)
            : base(@event, simulator) {
        }

        override public Boolean Triggered(Activity activity) {
            var e = this.ParsedObject as MSTS.EventCategoryTime;
            var triggered = (e.Time <= (int)Simulator.ClockTime - activity.StartTimeS);
            return triggered;
        }
    }
}
