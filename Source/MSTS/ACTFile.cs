/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MSTS {
    public enum SeasonType { Spring = 0, Summer, Autumn, Winter };
    public enum WeatherType { Clear = 0, Snow, Rain };
    public enum Difficulty { Easy = 0, Medium, Hard };
    public enum EventType {
        AllStops = 0, AssembleTrain, AssembleTrainAtLocation, DropOffWagonsAtLocation, PickUpPassengers,
        PickUpWagons, ReachSpeed
    };

    /// <summary>
    /// Parse and *.act file.
    /// Naming for classes matches the terms in the *.act file.
    /// </summary>
    public class ACTFile {
        public Tr_Activity Tr_Activity;

        public ACTFile(string filenamewithpath) {
            Read(filenamewithpath, false);
        }

        public ACTFile(string filenamewithpath, bool headerOnly) {
            Read(filenamewithpath, headerOnly);
        }

        public void Read(string filenamewithpath, bool headerOnly) {
            using (STFReader stf = new STFReader(filenamewithpath, false)) {
                stf.ParseFile(() => headerOnly && (Tr_Activity != null) && (Tr_Activity.Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_activity", ()=>{ Tr_Activity = new Tr_Activity(stf, headerOnly); }),
                });
                if (Tr_Activity == null)
                    STFException.TraceError(stf, "Missing Tr_Activity statement");
            }
        }
    }

    public class Tr_Activity {
        public int Serial = 1;
        public Tr_Activity_Header Tr_Activity_Header;
        public Tr_Activity_File Tr_Activity_File;

        public Tr_Activity(STFReader stf, bool headerOnly) {
            stf.MustMatch("(");
            stf.ParseBlock(() => headerOnly && (Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tr_activity_file", ()=>{ Tr_Activity_File = new Tr_Activity_File(stf); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("tr_activity_header", ()=>{ Tr_Activity_Header = new Tr_Activity_Header(stf); }),
            });
            if (!headerOnly && (Tr_Activity_File == null))
                STFException.TraceError(stf, "Missing Tr_Activity_File statement");
        }
    }

    public class Tr_Activity_Header {
        public string RouteID;
        public string Name;					// AE Display Name
        public string Description = " ";
        public string Briefing = " ";
        public int CompleteActivity = 1;
        public int Type = 0;
        public int Mode = 2;
        public StartTime StartTime = new StartTime(10, 0, 0);
        public SeasonType Season = SeasonType.Summer;
        public WeatherType Weather = WeatherType.Clear;
        public string PathID;
        public int StartingSpeed = 0;
        public Duration Duration = new Duration(1, 0);
        public Difficulty Difficulty = Difficulty.Easy;
        public int Animals = 100;		// percent
        public int Workers = 0;			// percent
        public int FuelWater = 100;		// percent
        public int FuelCoal = 100;		// percent
        public int FuelDiesel = 100;	// percent

        public Tr_Activity_Header(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("routeid", ()=>{ RouteID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(Description); }),
                new STFReader.TokenProcessor("briefing", ()=>{ Briefing = stf.ReadStringBlock(Briefing); }),
                new STFReader.TokenProcessor("completeactivity", ()=>{ CompleteActivity = stf.ReadIntBlock(STFReader.UNITS.None, CompleteActivity); }),
                new STFReader.TokenProcessor("type", ()=>{ Type = stf.ReadIntBlock(STFReader.UNITS.None, Type); }),
                new STFReader.TokenProcessor("mode", ()=>{ Mode = stf.ReadIntBlock(STFReader.UNITS.None, Mode); }),
                new STFReader.TokenProcessor("starttime", ()=>{ StartTime = new StartTime(stf); }),
                new STFReader.TokenProcessor("season", ()=>{ Season = (SeasonType)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("weather", ()=>{ Weather = (WeatherType)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("pathid", ()=>{ PathID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("startingspeed", ()=>{ StartingSpeed = stf.ReadIntBlock(STFReader.UNITS.Speed, StartingSpeed); }),
                new STFReader.TokenProcessor("duration", ()=>{ Duration = new Duration(stf); }),
                new STFReader.TokenProcessor("difficulty", ()=>{ Difficulty = (Difficulty)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("animals", ()=>{ Animals = stf.ReadIntBlock(STFReader.UNITS.None, Animals); }),
                new STFReader.TokenProcessor("workers", ()=>{ Workers = stf.ReadIntBlock(STFReader.UNITS.None, Workers); }),
                new STFReader.TokenProcessor("fuelwater", ()=>{ FuelWater = stf.ReadIntBlock(STFReader.UNITS.Any, FuelWater); }),
                new STFReader.TokenProcessor("fuelcoal", ()=>{ FuelCoal = stf.ReadIntBlock(STFReader.UNITS.Any, FuelCoal); }),
                new STFReader.TokenProcessor("fueldiesel", ()=>{ FuelDiesel = stf.ReadIntBlock(STFReader.UNITS.Any, FuelDiesel); }),
            });
        }
    }

    public class StartTime {
        public int Hour;
        public int Minute;
        public int Second;

        public StartTime(int h, int m, int s) {
            Hour = h;
            Minute = m;
            Second = s;
        }

        public StartTime(STFReader stf) {
            stf.MustMatch("(");
            Hour = stf.ReadInt(STFReader.UNITS.None, null);
            Minute = stf.ReadInt(STFReader.UNITS.None, null);
            Second = stf.ReadInt(STFReader.UNITS.None, null);
            stf.MustMatch(")");
        }

        public String FormattedStartTime() {
            return Hour.ToString("00") + ":" + Minute.ToString("00") + ":" + Second.ToString("00");
        }
    }

    public class Duration {
        int Hour;
        int Minute;

        public Duration(int h, int m) {
            Hour = h;
            Minute = m;
        }

        public Duration(STFReader stf) {
            stf.MustMatch("(");
            Hour = stf.ReadInt(STFReader.UNITS.None, null);
            Minute = stf.ReadInt(STFReader.UNITS.None, null);
            stf.MustMatch(")");
        }

        public String FormattedDurationTime() {
            return Hour.ToString("00") + ":" + Minute.ToString("00");
        }
    }

    public class Tr_Activity_File {
        public Player_Service_Definition Player_Service_Definition = null;
        public int NextServiceUID = 1;
        public int NextActivityObjectUID = 32786;
        public ActivityObjects ActivityObjects = null;
        public ActivityFailedSignals ActivityFailedSignals = null;
        public Events Events = null;
        public Traffic_Definition Traffic_Definition = null;
        public PlatformNumPassengersWaiting PlatformNumPassengersWaiting = null;
        public ActivityRestrictedSpeedZones ActivityRestrictedSpeedZones = null;

        public Tr_Activity_File(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("player_service_definition",()=>{ Player_Service_Definition = new Player_Service_Definition(stf); }),
                new STFReader.TokenProcessor("nextserviceuid",()=>{ NextServiceUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("nextactivityobjectuid",()=>{ NextActivityObjectUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("events",()=>{ Events = new Events(stf); }),
                new STFReader.TokenProcessor("traffic_definition",()=>{ Traffic_Definition = new Traffic_Definition(stf); }),
                new STFReader.TokenProcessor("activityobjects",()=>{ ActivityObjects = new ActivityObjects(stf); }),
                new STFReader.TokenProcessor("platformnumpassengerswaiting",()=>{ PlatformNumPassengersWaiting = new PlatformNumPassengersWaiting(stf); }),  // 35 files. To test, use EUROPE1\ACTIVITIES\aftstorm.act
                new STFReader.TokenProcessor("activityfailedsignals",()=>{ ActivityFailedSignals = new ActivityFailedSignals(stf); }),
                new STFReader.TokenProcessor("activityrestrictedspeedzones",()=>{ ActivityRestrictedSpeedZones = new ActivityRestrictedSpeedZones(stf); }),   // 27 files. To test, use EUROPE1\ACTIVITIES\lclsrvce.act
            });
        }

        //public void ClearStaticConsists()
        //{
        //    NextActivityObjectUID = 32786;
        //    ActivityObjects.Clear();
        //}
    }

    public class Player_Service_Definition {
        public string Name;
        public Player_Traffic_Definition Player_Traffic_Definition;

        public Player_Service_Definition(STFReader stf) {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("player_traffic_definition", ()=>{ Player_Traffic_Definition = new Player_Traffic_Definition(stf); }),
            });
        }
    }

    public class Player_Traffic_Definition {
        public int Time;
        public List<Player_Traffic_Item> Player_Traffic_List = new List<Player_Traffic_Item>();

        public Player_Traffic_Definition(STFReader stf) {
            DateTime baseDT = new DateTime();
            DateTime arrivalTime = new DateTime();
            DateTime departTime = new DateTime();
            int skipCount = 0;
            float distanceDownPath = new float();
            int platformStartID = 0;
            stf.MustMatch("(");
            Time = stf.ReadInt(STFReader.UNITS.None, null);
            // Clumsy parsing. You only get a new Player_Traffic_Item in the list after a PlatformStartId is met.
            // Blame lies with Microsoft for poor design of syntax.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("arrivaltime", ()=>{ arrivalTime = baseDT.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("departtime", ()=>{ departTime = baseDT.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ skipCount = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ distanceDownPath = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ platformStartID = stf.ReadIntBlock(STFReader.UNITS.None, null); 
                    Player_Traffic_List.Add(new Player_Traffic_Item(arrivalTime, departTime, skipCount, distanceDownPath, platformStartID)); }),
            });
        }
    }

    public class Player_Traffic_Item {
        public DateTime ArrivalTime;
        public DateTime DepartTime;
        public float DistanceDownPath;
        public int PlatformStartID = 0;

        public Player_Traffic_Item(DateTime arrivalTime, DateTime departTime, int skipCount, float distanceDownPath, int platformStartID) {
            ArrivalTime = arrivalTime;
            DepartTime = departTime;
            DistanceDownPath = distanceDownPath;
            PlatformStartID = platformStartID;
        }
    }

    public class Service_Definition {
        public string Name;
        public int Time;
        public int UiD;
        public List<Service_Item> ServiceList = new List<Service_Item>();
        float efficiency;
        int skipCount = 0;
        float distanceDownPath = new float();
        int platformStartID = 0;

        public Service_Definition(STFReader stf) {
            stf.MustMatch("(");
            Name = stf.ReadString();
            Time = stf.ReadInt(STFReader.UNITS.None, null);
            stf.MustMatch("uid");
            UiD = stf.ReadIntBlock(STFReader.UNITS.None, null);
            // Clumsy parsing. You only get a new Player_Traffic_Item in the list after a PlatformStartId is met.
            // Blame lies with Microsoft for poor design of syntax.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("efficiency", ()=>{ efficiency = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ skipCount = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ distanceDownPath = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ platformStartID = stf.ReadIntBlock(STFReader.UNITS.None, null); 
                    ServiceList.Add(new Service_Item(efficiency, skipCount, distanceDownPath, platformStartID)); }),
            });
        }
    }

    public class Service_Item {
        public float Efficiency = new float();
        public int SkipCount = 0;
        public float DistanceDownPath = new float();
        public int PlatformStartID = 0;

        public Service_Item(float efficiency, int skipCount, float distanceDownPath, int platformStartID) {
            Efficiency = efficiency;
            SkipCount = skipCount;
            DistanceDownPath = distanceDownPath;
            PlatformStartID = platformStartID;
        }
    }

    /// <summary>
    /// Parses Service_Definition objects and saves them in ServiceDefinitionList.
    /// </summary>
    public class Traffic_Definition {
        public string Name;
        public List<Service_Definition> ServiceDefinitionList = new List<Service_Definition>();

        public Traffic_Definition(STFReader stf) {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("service_definition", ()=>{ ServiceDefinitionList.Add(new Service_Definition(stf)); }),
            });
        }
    }

    /// <summary>
    /// Parses Event objects and saves them in EventList.
    /// </summary>
    public class Events {
        public List<Event> EventList = new List<Event>();

        public Events(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventcategorylocation", ()=>{ EventList.Add(new EventCategoryLocation(stf)); }),
                new STFReader.TokenProcessor("eventcategoryaction", ()=>{ EventList.Add(new EventCategoryAction(stf)); }),
                new STFReader.TokenProcessor("eventcategorytime", ()=>{ EventList.Add(new EventCategoryTime(stf)); }),
            });
        }
    }

    /// <summary>
    /// The 3 types of event are inherited from the abstract Event class.
    /// </summary>
    public abstract class Event {
        public int ID;
        public string Name;
        public int Activation_Level = 0;
        public Outcomes Outcomes = null;
        public string TextToDisplayOnCompletionIfTriggered = "";
        public string TextToDisplayOnCompletionIfNotTriggered = "";
        public Boolean Reversible = false;
    }

    public class EventCategoryLocation : Event {
        public bool TriggerOnStop = false;  // Value assumed if property not found.
        public int TileX;
        public int TileZ;
        public float X;
        public float Z;
        public float RadiusM;

        public EventCategoryLocation(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypelocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("outcomes", ()=>{ Outcomes = new Outcomes(stf); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnottriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("triggeronstop", ()=>{ TriggerOnStop = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("location", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(STFReader.UNITS.None, null);
                    TileZ = stf.ReadInt(STFReader.UNITS.None, null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    RadiusM = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.MustMatch(")");
                }),
            });
        }
    }

    /// <summary>
    /// Parses all types of action events.
    /// Save type of action event in Type. MSTS syntax isn't fully hierarchical, so using inheritance here instead of Type would be awkward. 
    /// </summary>
    public class EventCategoryAction : Event {
        public EventType Type;
        public WagonList WagonList;
        public Nullable<uint> SidingId;  // May be specified inside the Wagon_List instead. Nullable as can't use -1 to indicate not set.
        public int SpeedMpS;
        private const float MilespHourToMeterpSecond = 0.44704f;

        public EventCategoryAction(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypeallstops", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AllStops; }),
                new STFReader.TokenProcessor("eventtypeassembletrain", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AssembleTrain; }),
                new STFReader.TokenProcessor("eventtypeassembletrainatlocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AssembleTrainAtLocation; }),
                new STFReader.TokenProcessor("eventtypedropoffwagonsatlocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.DropOffWagonsAtLocation; }),
                new STFReader.TokenProcessor("eventtypepickuppassengers", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.PickUpPassengers; }),
                new STFReader.TokenProcessor("eventtypepickupwagons", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.PickUpWagons; }),
                new STFReader.TokenProcessor("eventtypereachspeed", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.ReachSpeed; }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("outcomes", ()=>{ Outcomes = new Outcomes(stf); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnotrriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("wagon_list", ()=>{ WagonList = new WagonList(stf, Type); }),
                new STFReader.TokenProcessor("sidingitem", ()=>{ SidingId = (uint)stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("speed", ()=>{ SpeedMpS = (int)(MilespHourToMeterpSecond * stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("reversable", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Reversible = true; }),
                // Also support the correct spelling !
                new STFReader.TokenProcessor("reversible", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Reversible = true; }),
            });
        }
    }

    public class WagonList {
        public List<WorkOrderWagon> WorkOrderWagonList = new List<WorkOrderWagon>();
        public Nullable<uint> UID;        // Nullable as can't use -1 to indicate not set.  
        public Nullable<uint> SidingId;   // May be specified outside the Wagon_List instead.
        public string Description = "";   // Value assumed if property not found.

        public WagonList(STFReader stf, EventType eventType) {
            stf.MustMatch("(");
            // "Drop Off" Wagon_List sometimes lacks a Description attribute, so we create the wagon _before_ description
            // is parsed. Bad practice, but not very dangerous as each Description usually repeats the same data.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UID = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("sidingitem", ()=>{ SidingId = stf.ReadUIntBlock(STFReader.UNITS.None, null); 
                    WorkOrderWagonList.Add(new WorkOrderWagon(UID.Value, SidingId.Value, Description));}),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(""); }),
            });
        }
    }

    /// <summary>
    /// Parses a wagon from the WagonList.
    /// Do not confuse with older class Wagon below, which parses TrainCfg from the *.con file.
    /// </summary>
    public class WorkOrderWagon {
        public Nullable<uint> UID;        // Nullable as can't use -1 to indicate not set.  
        public Nullable<uint> SidingId;   // May be specified outside the Wagon_List.
        public string Description = "";   // Value assumed if property not found.

        public WorkOrderWagon(uint uId, uint sidingId, string description) {
            UID = uId;
            SidingId = sidingId;
            Description = description;
        }
    }

    public class EventCategoryTime : Event {  // E.g. Hisatsu route and Short Passenger Run shrtpass.act
        public int Time;

        public EventCategoryTime(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("outcomes", ()=>{ Outcomes = new Outcomes(stf); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnotrriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("time", ()=>{ Time = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
            });
        }
    }

    public class Outcomes {
        public bool ActivitySuccess = false;
        public string ActivityFail = null;
        // MSTS Activity Editor limits model to 4 outcomes of any type. We use lists so there is no restriction.
        public List<int> ActivateList = new List<int>();
        public List<int> RestoreActLevelList = new List<int>();
        public List<int> DecActLevelList = new List<int>();
        public List<int> IncActLevelList = new List<int>();
        public string DisplayMessage;

        public Outcomes(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activitysuccess", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); ActivitySuccess = true; }),
                new STFReader.TokenProcessor("activityfail", ()=>{ ActivityFail = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("activateevent", ()=>{ ActivateList.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("restoreactlevel", ()=>{ RestoreActLevelList.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("decactlevel", ()=>{ DecActLevelList.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("incactlevel", ()=>{ IncActLevelList.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
                new STFReader.TokenProcessor("displaymessage", ()=>{ DisplayMessage = stf.ReadStringBlock(""); }),
            });
        }
    }

    /// <summary>
    /// Parses ActivityObject objects and saves them in ActivityObjectList.
    /// </summary>
    public class ActivityObjects {
        public List<ActivityObject> ActivityObjectList = new List<ActivityObject>();

        //public new ActivityObject this[int i]
        //{
        //    get { return (ActivityObject)base[i]; }
        //    set { base[i] = value; }
        //}

        public ActivityObjects(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityobject", ()=>{ ActivityObjectList.Add(new ActivityObject(stf)); }),
            });
        }
    }

    public class ActivityObject {
        public Train_Config Train_Config = null;
        public int Direction;
        public int ID;
        public int TileX;
        public int TileZ;
        public float X;
        public float Z;

        public ActivityObject(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("objecttype", ()=>{ stf.MustMatch("("); stf.MustMatch("WagonsList"); stf.MustMatch(")"); }),
                new STFReader.TokenProcessor("train_config", ()=>{ Train_Config = new Train_Config(stf); }),
                new STFReader.TokenProcessor("direction", ()=>{ Direction = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("tile", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(STFReader.UNITS.None, null);
                    TileZ = stf.ReadInt(STFReader.UNITS.None, null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.MustMatch(")");
                }),
            });
        }
    }

    public class Train_Config {
        public TrainCfg TrainCfg;

        public Train_Config(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("traincfg", ()=>{ TrainCfg = new TrainCfg(stf); }),
            });
        }
    }


    public class MaxVelocity {
        public float A = 0;
        public float B = 0.001f;

        public MaxVelocity(STFReader stf) {
            stf.MustMatch("(");
            A = stf.ReadFloat(STFReader.UNITS.Speed, null);
            B = stf.ReadFloat(STFReader.UNITS.Speed, null);
            stf.MustMatch(")");
        }
    }

    public class TrainCfg {
        public string Name = "Loose consist.";
        int Serial = 1;
        public MaxVelocity MaxVelocity = null;
        int NextWagonUID = 0;
        float Durability = 1.0f;   // Value assumed if attribute not found.

        public List<Wagon> WagonList = new List<Wagon>();

        public TrainCfg(STFReader stf) {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("maxvelocity", ()=>{ MaxVelocity = new MaxVelocity(stf); }),
                new STFReader.TokenProcessor("nextwagonuid", ()=>{ NextWagonUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("durability", ()=>{ Durability = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagon", ()=>{ WagonList.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("engine", ()=>{ WagonList.Add(new Wagon(stf)); }),
            });
        }
    }

    public class Wagon {
        public string Folder;
        public string Name;
        public int UiD;
        public bool IsEngine = false;
        public bool Flip = false;

        public Wagon(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("flip", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Flip = true; }),
                new STFReader.TokenProcessor("enginedata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); IsEngine = true; }),
                new STFReader.TokenProcessor("wagondata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); }),
            });
        }

        public string GetName(uint uId, List<Wagon> wagonList) {
            foreach (var item in wagonList) {
                var wagon = item as Wagon;
                if (wagon.UiD == uId) {
                    return wagon.Name;
                }
            }
            return "<unknown name>";
        }
    }

    public class PlatformNumPassengersWaiting {  // For use, see file EUROPE1\ACTIVITIES\aftstorm.act
        public List<PlatformData> PlatformDataList = new List<PlatformData>();

        public PlatformNumPassengersWaiting(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("platformdata", ()=>{ PlatformDataList.Add(new PlatformData(stf)); }),
            });
        }
    }

    public class PlatformData { // e.g. "PlatformData ( 41 20 )" 
        int Id;
        int PassengerCount;

        public PlatformData(int id, int passengerCount) {
            Id = id;
            PassengerCount = passengerCount;
        }

        public PlatformData(STFReader stf) {
            stf.MustMatch("(");
            Id = stf.ReadInt(STFReader.UNITS.None, null);
            PassengerCount = stf.ReadInt(STFReader.UNITS.None, null);
            stf.MustMatch(")");
        }
    }

    public class ActivityFailedSignals { // e.g. ActivityFailedSignals ( ActivityFailedSignal ( 50 ) )
        public List<int> FailedSignalList = new List<int>();
        public ActivityFailedSignals(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityfailedsignal", ()=>{ FailedSignalList.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
            });
        }
    }

    public class ActivityRestrictedSpeedZones {  // For use, see file EUROPE1\ACTIVITIES\aftstorm.act
        public List<ActivityRestrictedSpeedZone> ActivityRestrictedSpeedZoneList = new List<ActivityRestrictedSpeedZone>();

        public ActivityRestrictedSpeedZones(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityrestrictedspeedzone", ()=>{ ActivityRestrictedSpeedZoneList.Add(new ActivityRestrictedSpeedZone(stf)); }),
            });
        }
    }

    public class ActivityRestrictedSpeedZone {
        public Position StartPosition = null;
        public Position EndPosition = null;

        public ActivityRestrictedSpeedZone(STFReader stf) {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("startposition", ()=>{ StartPosition = new Position(stf); }),
                new STFReader.TokenProcessor("endposition", ()=>{ EndPosition = new Position(stf); }),
            });
        }
    }

    public class Position {
        int TileX;
        int TileZ;
        int X;
        int Z;

        public Position(int tileX, int tileZ, int x, int z) {
            TileX = tileX;
            TileZ = tileZ;
            X = x;
            Z = z;
        }

        public Position(STFReader stf) {
            stf.MustMatch("(");
            TileX = stf.ReadInt(STFReader.UNITS.None, null);
            TileZ = stf.ReadInt(STFReader.UNITS.None, null);
            X = stf.ReadInt(STFReader.UNITS.None, null);
            Z = stf.ReadInt(STFReader.UNITS.None, null);
            stf.MustMatch(")");
        }
    }
}
