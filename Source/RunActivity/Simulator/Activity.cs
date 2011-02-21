using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;
using System.Reflection;

namespace ORTS
{
    public enum ActivityEvent
    {
        Timer,
        TrainStart,
        TrainStop,
        Couple,
        Uncouple
    }
    
    public class ActivityTask
    {
        public bool? IsCompleted { get; internal set; }
        public ActivityTask PrevTask { get; internal set; }
        public ActivityTask NextTask { get; internal set; }
        public DateTime CompletedAt { get; internal set; }
        public string DisplayMessage { get; internal set; }
        public int SoundNotify = -1;

        public virtual void NotifyEvent(ActivityEvent EventType)
        {
        }

        public virtual void Save(BinaryWriter outf)
        {
            Int32 noval = -1;
            if (IsCompleted == null) outf.Write(noval); else outf.Write(IsCompleted.Value ? (Int32)1 : (Int32)0);
            outf.Write((Int64)CompletedAt.Ticks);
            outf.Write(DisplayMessage);
            outf.Write((Int32)SoundNotify);
        }

        public virtual void Restore(BinaryReader inf)
        {
            Int64 rdval;
            rdval = inf.ReadInt32();
            IsCompleted = rdval == -1 ? (bool?)null : rdval == 0 ? false : true;
            CompletedAt = new DateTime(inf.ReadInt64());
            DisplayMessage = inf.ReadString();
            SoundNotify = inf.ReadInt32();
        }
    }

    public class Activity
    {
        public DateTime StartTime;
        public List<ActivityTask> Tasks = new List<ActivityTask>();
        public ActivityTask Current = null;
        double prevTrainSpeed = -1;

        private Activity(BinaryReader inf)
        {
            RestoreThis(inf);
        }
        
        public Activity(ACTFile actFile)
        {
            Player_Service_Definition sd;
            sd = actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition;
            if (sd != null)
            {
                if (sd.Player_Traffic_Definition.ArrivalTime.Count != sd.Player_Traffic_Definition.DepartTime.Count ||
                    sd.Player_Traffic_Definition.DepartTime.Count != sd.Player_Traffic_Definition.DistanceDownPath.Count ||
                    sd.Player_Traffic_Definition.DistanceDownPath.Count != sd.Player_Traffic_Definition.PlatformStartID.Count)
                {
					throw new InvalidDataException("Invalid Player Traffice Definition in ACT file!");
                }

                if (sd.Player_Traffic_Definition.ArrivalTime.Count > 0)
                {
                    PlatformItem Platform;
                    ActivityTask task = null;
                    for (int i = 0; i < sd.Player_Traffic_Definition.ArrivalTime.Count; i++)
                    {
                        Platform = Program.Simulator.TDB.TrackDB.TrItemTable[sd.Player_Traffic_Definition.PlatformStartID[i]] as PlatformItem;

                        if (Platform != null)
                        {
                            Tasks.Add(new ActivityTaskPassengerStopAt(task,
                                sd.Player_Traffic_Definition.ArrivalTime[i],
                                sd.Player_Traffic_Definition.DepartTime[i],
                                Platform, Program.Simulator.TDB.TrackDB.TrItemTable[Platform.LinkedPlatformItemId] as PlatformItem));
                            task = Tasks[i];
                        }
                    }

                    Current = Tasks[0];
                }
            }
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
            if (Current == null) return;

            Current.NotifyEvent(ActivityEvent.Timer);
            if (Current.IsCompleted != null)
            {
                Current = Current.NextTask;
            }

            if (Program.Simulator.PlayerLocomotive.SpeedMpS == 0)
            {
                if (prevTrainSpeed != 0)
                {
                    prevTrainSpeed = 0;
                    Current.NotifyEvent(ActivityEvent.TrainStop);
                    if (Current.IsCompleted != null)
                    {
                        Current = Current.NextTask;
                    }
                }
            }
            else
            {
                if (prevTrainSpeed == 0)
                {
                    prevTrainSpeed = Program.Simulator.PlayerLocomotive.SpeedMpS;
                    Current.NotifyEvent(ActivityEvent.TrainStart);
                    if (Current.IsCompleted != null)
                    {
                        Current = Current.NextTask;
                    }
                }
            }
        }

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

        public static Activity Restore(BinaryReader inf)
        {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == -1)
            {
                return null;
            }
            else
            {
                Activity act = new Activity(inf);
                return act;
            }
        }
        
        public void Save(BinaryWriter outf)
        {
            Int32 noval = -1;
            outf.Write((Int64)StartTime.Ticks);
            outf.Write((Int32)Tasks.Count);
            foreach(ActivityTask task in Tasks)
            {
                task.Save(outf);
            }
            if (Current == null) outf.Write(noval); else outf.Write((Int32)(Tasks.IndexOf(Current)));
            outf.Write(prevTrainSpeed);
        }

        private ActivityTask GetTask(BinaryReader inf)
        {
            Int32 rdval;
            rdval = inf.ReadInt32();
            if (rdval == 1)
                return new ActivityTaskPassengerStopAt();
            else
                return null;
        }
        
        public void RestoreThis(BinaryReader inf)
        {
            Int32 rdval;
            ActivityTask task;

            StartTime = new DateTime(inf.ReadInt64());
            rdval = inf.ReadInt32();
            for (int i = 0; i < rdval; i++)
            {
                task = GetTask(inf);
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
        }
    }

    /// <summary>
    /// Helper class to calculate distances along the path
    /// </summary>
    public class TDBTravellerDistanceCalculatorHelper
    {
        // Result of calculation
        public enum DistanceResult
        {
            Valid,
            Behind,
            OffPath
        }

        // We use this traveller as the base of the calulations
        TDBTraveller refTraveller;
        float Distance;

        public TDBTravellerDistanceCalculatorHelper(TDBTraveller traveller)
        {
            refTraveller = traveller;
        }

        public DistanceResult CalculateToPoint (int TileX, int TileZ, float X, float Y, float Z)
        {
            TDBTraveller poiTraveller;
            poiTraveller = new TDBTraveller(refTraveller);

            // Find distance once
            Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, ref poiTraveller);

            // If valid
            if (Distance > 0)
            {
                return DistanceResult.Valid;
            }
            else
            {
                // Go to opposite direction
                poiTraveller = new TDBTraveller(refTraveller);
                poiTraveller.ReverseDirection();

                Distance = poiTraveller.DistanceTo(TileX, TileZ, X, Y, Z, ref poiTraveller);
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

        public ActivityTaskPassengerStopAt(ActivityTask prev, DateTime Arrive, DateTime Depart, PlatformItem Platformend1, PlatformItem Platformend2)
        {
            SchArrive = Arrive;
            SchDepart = Depart;
            PlatformEnd1 = Platformend1;
            PlatformEnd2 = Platformend2;
            PrevTask = prev;
            if (prev != null)
                prev.NextTask = this;
            DisplayMessage = "";
        }

        internal ActivityTaskPassengerStopAt()
        {
        }

        /// <summary>
        /// Determines if the train is at station
        /// </summary>
        /// <returns></returns>
        public bool IsAtStation()
        {
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
            if ( (distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid) || (
                distanceend1 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Valid &&
                distanceend2 == TDBTravellerDistanceCalculatorHelper.DistanceResult.Behind) )
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

        public bool IsMissedStation()
        {
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

        public override void NotifyEvent(ActivityEvent EventType)
        {
            // The train is stopped.
            if (EventType == ActivityEvent.TrainStop)
            {
                // Checking if the stopping is occuread at the scheduled platform.
                /*
                double dist1 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
                double dist2 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);
                if ( (dist1 >= 0 && dist2 <= 0) || (dist1 <= 0 && dist2 >= 0))
                */
                if (IsAtStation())
                {
                    // If yes, we arrived
                    ActArrive = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));
                    arrived = true;

                    // Chekck if this is the last task in activity, then it is complete
                    if (NextTask == null)
                    {
                        IsCompleted = true;
                        return;
                    }

                    // Figure out the load/unload time
                    if (SchDepart > ActArrive)
                    {
                        LoadUnload = (SchDepart - ActArrive).Value.TotalSeconds;
                    }
                    
                    if (LoadUnload < PlatformEnd1.PlatformMinWaitingTime) LoadUnload = PlatformEnd1.PlatformMinWaitingTime;
                    LoadUnload += Program.Simulator.ClockTime;
                }
            }
            else if (EventType == ActivityEvent.TrainStart)
            {
                // Train has started, we have things to do if we arrived before
                if (arrived)
                {
                    ActDepart = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));
                    CompletedAt = ActDepart.Value;
                    // Completeness is depend on the elapsed waiting time
                    IsCompleted = maydepart;
                }
            }
            else if (EventType == ActivityEvent.Timer)
            {
                // Waiting at a station
                if (arrived)
                {
                    double remaining = LoadUnload - Program.Simulator.ClockTime;
                    // Still have to wait
                    if (remaining > 0)
                    {
                        DisplayMessage = string.Format("Passenger load/unload completes in {0:D2}:{1:D2}",
                            (int)(remaining / 60), (int)(remaining % 60));
                    }
                    // May depart
                    else if (!maydepart)
                    {
                        maydepart = true;
                        DisplayMessage = "Passenger load/unload completed. You may depart now.";
                        SoundNotify = 60;
                    }
                }
                else
                {
                    // Checking missed station
                    int tmp = (int)(Program.Simulator.ClockTime % 10);
                    if (tmp != TimerChk)
                    {
                        /*
                        double dist1 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd1.TileX,
                            PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
                        double dist2 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd2.TileX,
                            PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);

                        // If both less than zero, station is missed
                        if (dist1 < 0 && dist2 < 0)
                        */
                        if (IsMissedStation())
                        {
                            IsCompleted = false;
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
            outf.Write((double)LoadUnload);
            outf.Write((Int32)TimerChk);
            outf.Write(arrived);
            outf.Write(maydepart);
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
            PlatformEnd1 = Program.Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            PlatformEnd2 = Program.Simulator.TDB.TrackDB.TrItemTable[inf.ReadInt32()] as PlatformItem;
            LoadUnload = inf.ReadDouble();
            TimerChk = inf.ReadInt32();
            arrived = inf.ReadBoolean();
            maydepart = inf.ReadBoolean();
        }
    }
}
