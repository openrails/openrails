using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;

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

        internal double X1, X2;
        internal double Y1, Y2;
        internal double Z1, Z2;

        public virtual void NotifyEvent(ActivityEvent EventType)
        {
        }
    }

    public class Activity
    {
        public DateTime StartTime;
        List<ActivityTask> Tasks = new List<ActivityTask>();
        public ActivityTask Current = null;
        double prevTrainSpeed = -1;
        double startPosition = -1;

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
                    throw new Exception("Invalid Player Traffice Definition in ACT file!");
                }

                PlatformItem Platform;
                ActivityTask task = null;
                for (int i = 0; i < sd.Player_Traffic_Definition.ArrivalTime.Count; i++)
                {
                    if (startPosition == -1) startPosition = sd.Player_Traffic_Definition.DistanceDownPath[i];
                    Platform = Program.Simulator.TDB.TrackDB.TrItemTable[sd.Player_Traffic_Definition.PlatformStartID[i]] as PlatformItem;
                    
                    if (Platform != null)
                    {
                        Tasks.Add(new ActivityTaskPassengerStopAt(task,
                            sd.Player_Traffic_Definition.ArrivalTime[i],
                            sd.Player_Traffic_Definition.DepartTime[i],
                            Platform, Program.Simulator.TDB.TrackDB.TrItemTable[Platform.Flags2] as PlatformItem,
                            sd.Player_Traffic_Definition.DistanceDownPath[i]));
                        task = Tasks[i];
                    }
                }

                Current = Tasks[0];
            }
        }

        public void Update()
        {
            if (Current == null) return;

            Current.NotifyEvent(ActivityEvent.Timer);

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
        double Position;
        bool arrived = false;
        bool maydepart = false;

        public ActivityTaskPassengerStopAt(ActivityTask prev, DateTime Arrive, DateTime Depart, PlatformItem Platformend1, PlatformItem Platformend2, double Position)
        {
            SchArrive = Arrive;
            SchDepart = Depart;
            PlatformEnd1 = Platformend1;
            PlatformEnd2 = Platformend2;
            PrevTask = prev;
            if (prev != null)
                prev.NextTask = this;
            this.Position = Position;
            DisplayMessage = "";
        }

        public override void NotifyEvent(ActivityEvent EventType)
        {
            if (EventType == ActivityEvent.TrainStop)
            {
                double dist1 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd1.TileX,
                    PlatformEnd1.TileZ, PlatformEnd1.X, PlatformEnd1.Y, PlatformEnd1.Z);
                double dist2 = Program.Simulator.PlayerLocomotive.Train.FrontTDBTraveller.DistanceTo(PlatformEnd2.TileX,
                    PlatformEnd2.TileZ, PlatformEnd2.X, PlatformEnd2.Y, PlatformEnd2.Z);
                if ( (dist1 >= 0 && dist2 <= 0) || (dist1 <= 0 && dist2 >= 0))
                {
                    ActArrive = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));

                    if (SchDepart > ActArrive)
                    {
                        LoadUnload = (SchDepart - ActArrive).Value.TotalSeconds;
                    }
                    
                    if (LoadUnload < PlatformEnd1.PlatformMinWaitingTime) LoadUnload = PlatformEnd1.PlatformMinWaitingTime;
                    LoadUnload += Program.Simulator.ClockTime;
                    arrived = true;
                }
            }
            else if (EventType == ActivityEvent.TrainStart)
            {
                if (arrived)
                {
                    ActDepart = new DateTime().Add(TimeSpan.FromSeconds(Program.Simulator.ClockTime));
                    CompletedAt = ActDepart.Value;
                    IsCompleted = true;
                }
            }
            else if (EventType == ActivityEvent.Timer)
            {
                if (arrived)
                {
                    double remaining = LoadUnload -  Program.Simulator.ClockTime;
                    if (remaining > 0)
                    {
                        DisplayMessage = string.Format("Passenger load/unload completes in {0:D2}:{1:D2}",
                            (int)(remaining / 60), (int)(remaining % 60));
                    }
                    else if (!maydepart)
                    {
                        maydepart = true;
                        DisplayMessage = "Passenger load/unload completed. You may depart now.";
                        SoundNotify = 60;
                    }
                }
            }
        }
    }
}
