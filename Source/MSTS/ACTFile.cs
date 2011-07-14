/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MSTS
{
	public enum SeasonType { Spring=0, Summer, Autumn, Winter };
	public enum WeatherType { Clear=0, Snow, Rain };
	public enum Difficulty { Easy=0, Medium, Hard };
    public enum EventType { StopAtFinalStation=0, PickUpPassengers, MakeAPickup, ReachSpeed, PickUpWagons, DropOffWagonsAtLocation, AssembleTrain, AssembleTrainAtLocation };

	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ACTFile
	{
		public Tr_Activity Tr_Activity;

		public ACTFile( string filenamewithpath )
		{
            Read(filenamewithpath, false);
		}

        public ACTFile(string filenamewithpath, bool headerOnly )
        {
            Read(filenamewithpath, headerOnly);
        }

        public void Read(string filenamewithpath, bool headerOnly)
        {
            using (STFReader stf = new STFReader(filenamewithpath, false))
            {
                stf.ParseFile(() => headerOnly && (Tr_Activity != null) && (Tr_Activity.Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_activity", ()=>{ Tr_Activity = new Tr_Activity(stf, headerOnly); }),
                });
                //TODO This should be changed to STFException.TraceError() with defaults values created
                if (Tr_Activity == null)
                    throw new STFException(stf, "Missing Tr_Activity statement");
            }
        }
	}

    public class Tr_Activity
    {
        public int Serial = 1;
        public Tr_Activity_Header Tr_Activity_Header;
        public Tr_Activity_File Tr_Activity_File;

        public Tr_Activity(STFReader stf, bool headerOnly)
        {
            stf.MustMatch("(");
            stf.ParseBlock(() => headerOnly && (Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tr_activity_file", ()=>{ Tr_Activity_File = new Tr_Activity_File(stf); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("tr_activity_header", ()=>{ Tr_Activity_Header = new Tr_Activity_Header(stf); }),
            });
            //TODO This should be changed to STFException.TraceError() with defaults values created
            if (!headerOnly && (Tr_Activity_File == null))
                throw new STFException(stf, "Missing Tr_Activity_File statement");
        }
    }

	public class Tr_Activity_Header
	{
		public string RouteID;
		public string Name;					// AE Display Name
		public string Description = " ";
		public string Briefing = " ";
		public int CompleteActivity = 1;
		public int Type = 0;
		public int Mode = 2;
		public StartTime StartTime = new StartTime( 10,0,0 );
		public SeasonType Season = SeasonType.Summer;				
		public WeatherType Weather = WeatherType.Clear;
		public string PathID;
		public int StartingSpeed = 0;
		public Duration Duration = new Duration( 1,0 );
		public Difficulty Difficulty = Difficulty.Easy;			
		public int Animals = 100;			// Animal percent
		public int Workers = 0;			// People percent
		public int FuelWater = 100;		// Percent
		public int FuelCoal = 100;		// Percent
		public int FuelDiesel = 100;	// Percent

		public Tr_Activity_Header(STFReader stf)
		{
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
        public Tr_Activity_Header( )
        {
        }
	}

	public class StartTime
	{
		public int Hour;
		public int Minute;
		public int Second;

		public StartTime( int h, int m, int s )
		{
			Hour = h;
			Minute = m;
			Second = s;
		}

		public StartTime(STFReader stf)
		{
			stf.MustMatch("(");
            Hour = stf.ReadInt(STFReader.UNITS.None, null);
            Minute = stf.ReadInt(STFReader.UNITS.None, null);
            Second = stf.ReadInt(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
		}

        public String FormattedStartTime()
        {
            return Hour.ToString("00")+":"+Minute.ToString("00")+":"+Second.ToString("00");
        }
	}

	public class Duration
	{
		int Hour;
		int Minute;

		public Duration( int h, int m )
		{
			Hour = h;
			Minute = m;
		}

		public Duration(STFReader stf)
		{
			stf.MustMatch("(");
            Hour = stf.ReadInt(STFReader.UNITS.None, null);
            Minute = stf.ReadInt(STFReader.UNITS.None, null);
			stf.SkipRestOfBlock();
		}

        public String FormattedDurationTime()
        {
            return Hour.ToString("00") + ":" + Minute.ToString("00");
        }
	}

	public class Tr_Activity_File
	{
		public Player_Service_Definition Player_Service_Definition = null;
		public int NextServiceUID = 1;
		public int NextActivityObjectUID = 32786;
		public ActivityObjects ActivityObjects = new ActivityObjects();
		public ActivityFailedSignals ActivityFailedSignals = new ActivityFailedSignals();
		public Events Events = new Events();
		public Traffic_Definition Traffic_Definition = null;
		//string PlatformNumPassengersWaiting = null; // Commented out to eliminate warning
        //string ActivityRestrictedSpeedZones = null; // Commented out to eliminate warning

		public Tr_Activity_File(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("player_service_definition",()=>{ Player_Service_Definition = new Player_Service_Definition(stf); }),
                new STFReader.TokenProcessor("nextserviceuid",()=>{ NextServiceUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("nextactivityobjectuid",()=>{ NextActivityObjectUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("events",()=>{ Events = new Events(stf); }),
                new STFReader.TokenProcessor("traffic_definition",()=>{ Traffic_Definition = new Traffic_Definition(stf); }),
                new STFReader.TokenProcessor("activityobjects",()=>{ ActivityObjects = new ActivityObjects(stf); }),
                new STFReader.TokenProcessor("activityfailedsignals",()=>{ ActivityFailedSignals = new ActivityFailedSignals(stf); }),
                new STFReader.TokenProcessor("platformnumpassengerswaiting",()=>{ stf.SkipRestOfBlock();  }),
                new STFReader.TokenProcessor("activityrestrictedspeedzones",()=>{ stf.SkipRestOfBlock(); }),
            });
		}


		public int AddActivitySuccessEvent( int tileX, int tileZ, float x, float y, float size )
		{
			int ID = this.Events.Count;
			return this.Events.Add( new EventCategoryLocation( ID, tileX, tileZ, x, y, size ) );
		}

		public void ClearStaticConsists()
		{
			NextActivityObjectUID = 32786;
			ActivityObjects.Clear();
		}


	}

	public class Traffic_Definition: ArrayList
	{
		public string Label;

		public Traffic_Definition(STFReader stf)
		{
			stf.MustMatch("(");
			Label = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("service_definition", ()=>{ Add(new Service_Definition(stf)); }),
            });
		}
	}

	public class Service_Definition
	{
		public string Service;
		public int Time;
		public int UiD;

		public Service_Definition(STFReader stf)
		{
			stf.MustMatch("(");
			Service = stf.ReadString();
            Time = stf.ReadInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("efficiency", ()=>{ stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ stf.ReadIntBlock(STFReader.UNITS.None, null); }),
            });
        }
	}

	public class Events: ArrayList
	{
		public Events(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventcategorylocation", ()=>{ Add(new EventCategoryLocation(stf)); }),
                new STFReader.TokenProcessor("eventcategoryaction", ()=>{ Add(new EventCategoryAction(stf)); }),
            });
		}

		public Events()
		{
		}

	}


	public class EventCategoryLocation
	{
		public int ID;
		public int Activation_Level;
		public Outcomes Outcomes = new Outcomes();
		public string Name;
		public string TextToDisplayOnCompletionIfNotTriggered = null;
		public bool TriggerOnStop;
		public int TileX;
		public int TileZ;
		public double X;
		public double Z;
		public double Size;

		public EventCategoryLocation(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypelocation", ()=>{ stf.SkipBlock();  }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("outcomes", ()=>{ Outcomes = new Outcomes(stf); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnottriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("triggeronstop", ()=>{ TriggerOnStop = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("location", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(STFReader.UNITS.None, null);
                    TileZ = stf.ReadInt(STFReader.UNITS.None, null);
                    X = stf.ReadDouble(STFReader.UNITS.None, null);
                    Z = stf.ReadDouble(STFReader.UNITS.None, null);
                    Size = stf.ReadDouble(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
		}

		/// <summary>
		/// Create a new 'ActivitySuccess' LocationEvent
		/// </summary>
		public EventCategoryLocation( int id, int tileX, int tileZ, float x, float z, float size )
		{
			TileX = tileX;
			TileZ = tileZ;
			X = x;
			Z = z;
			Size = size;
			ID = id;
			Activation_Level = 1;
			Name = String.Format( "Location{0}", ID );
			TriggerOnStop = false;
			Outcomes.Add( new ActivitySuccess( ) );
		}
	}


	public class EventCategoryAction
	{
		public int ID;
		public int Activation_Level;
		public Outcomes Outcomes = new Outcomes();
		public string Name;
        public EventType EventType;
        public WagonList WagonList;
        public int SidingId;

		/// <summary>
		/// Build a default EventTypeAllStops event
		/// </summary>
		public EventCategoryAction( int id )
		{
			ID = id;
			Activation_Level = 1;
			Outcomes.Add( new ActivitySuccess() );
			Name = string.Format( "Action{0}",ID );
		}

		public EventCategoryAction(STFReader stf)
		{
        stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypeallstops", ()=>{ stf.SkipBlock(); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("outcomes", ()=>{ Outcomes = new Outcomes(stf); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnotrriggered", ()=>{ stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("eventtypedropoffwagonsatlocation", ()=>{ EventType = EventType.DropOffWagonsAtLocation; }),
                new STFReader.TokenProcessor("eventtypepickupwagons", ()=>{ EventType = EventType.PickUpWagons; }),
                new STFReader.TokenProcessor("eventtypeassembletrain", ()=>{ EventType = EventType.AssembleTrain; }),
                new STFReader.TokenProcessor("eventtypeassembletrainatlocation", ()=>{ EventType = EventType.AssembleTrainAtLocation; }),
                new STFReader.TokenProcessor("wagon_list", ()=>{ WagonList = new WagonList(stf, EventType); }),
                new STFReader.TokenProcessor("sidingitem", ()=>{ SidingId = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
            });
        }
	}

    public class WagonList {
        public ArrayList Wagons = new ArrayList();
        public uint UID;
        public uint SidingItem;  
        public string Description = "";

        public WagonList(STFReader stf, EventType eventType) {
            stf.MustMatch("(");
            switch (eventType) {
                case EventType.PickUpWagons: // "Pick Up" Wagon_Lists lack a Description attribute.
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("uid", ()=>{ UID = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("sidingitem", ()=>{ SidingItem = stf.ReadUIntBlock(STFReader.UNITS.None, null);  Wagons.Add(new WorkOrderWagon(UID, SidingItem, ""));}),
                    });
                    break;
                default:  // "Drop Off" Wagon_Lists sometimes lack a Description attribute, so create the wagon _before_ description
                          // is parsed. Bad practice. However, not very dangerous as each Description usually contains the same data.
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("uid", ()=>{ UID = stf.ReadUIntBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("sidingitem", ()=>{ SidingItem = stf.ReadUIntBlock(STFReader.UNITS.None, null); Wagons.Add(new WorkOrderWagon(UID, SidingItem, Description));}),
                        new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(""); }),
                    }); 
                    break;
            }
        }
    }

    /// <summary>
    /// Parses Wagon_List from the *.act file.
    /// Do not confuse with class Wagon below, which parses TrainCfg from the *.con file.
    /// </summary>
    public class WorkOrderWagon {
        public uint UID;
        public uint SidingItem; 
        public string Description;

        public WorkOrderWagon(uint uId, uint sidingItem, string description) {
            UID = uId;
            SidingItem = sidingItem;
            Description = description;
        }
    }

	public class Outcomes: ArrayList
	{
		public Outcomes(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activitysuccess", ()=>{ Add(new ActivitySuccess(stf)); }),
            });
		}

		public Outcomes()
		{
		}

	}


	public class ActivitySuccess // a type of outcome
	{
		public ActivitySuccess(STFReader stf)
		{
			stf.MustMatch("(");
			stf.SkipRestOfBlock();
		}

		public ActivitySuccess()
		{
		}

	}

	
	public class ActivityFailedSignals: ArrayList
	{
		public ActivityFailedSignals(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityfailedsignal", ()=>{ Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
            });
		}

		public ActivityFailedSignals()
		{
		}

	}

	public class ActivityObjects: ArrayList
	{
        public new ActivityObject this[int i]
        {
            get { return (ActivityObject)base[i]; }
            set { base[i] = value; }
        }

		public ActivityObjects(STFReader stf)
		{
			stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityobject", ()=>{ Add(new ActivityObject(stf)); }),
            });
		}

		public ActivityObjects()
		{
		}

	}

	public class ActivityObject
	{
		public Train_Config Train_Config;
		public int Direction;
		public int ID;
		public int TileX;
		public int TileZ;
		public float X;
		public float Z;

        public ActivityObject(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("objecttype", ()=>{ stf.MustMatch("("); stf.MustMatch("WagonsList"); stf.SkipRestOfBlock(); }),
                new STFReader.TokenProcessor("train_config", ()=>{ Train_Config = new Train_Config(stf); }),
                new STFReader.TokenProcessor("direction", ()=>{ Direction = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("tile", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(STFReader.UNITS.None, null);
                    TileZ = stf.ReadInt(STFReader.UNITS.None, null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
	}

	public class Train_Config
	{
		public TrainCfg TrainCfg;

        public Train_Config(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("traincfg", ()=>{ TrainCfg = new TrainCfg(stf); }),
            });
        }
	}


	public class MaxVelocity
	{
		public float A = 0;
		public float B = 0.001f;

		public MaxVelocity()
		{
		}

		public MaxVelocity(STFReader stf)
		{
			stf.MustMatch("(");
            A = stf.ReadFloat(STFReader.UNITS.Speed, null);
            B = stf.ReadFloat(STFReader.UNITS.Speed, null);
			stf.SkipRestOfBlock();
		}
	}

	public class TrainCfg
	{
		public string Name = "Loose consist.";
		int Serial = 1;
		public MaxVelocity MaxVelocity = new MaxVelocity();
		int NextWagonUID = 0;
		float Durability = 1.0f;

		public ArrayList Wagons = new ArrayList();

        public TrainCfg(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("maxvelocity", ()=>{ MaxVelocity = new MaxVelocity(stf); }),
                new STFReader.TokenProcessor("nextwagonuid", ()=>{ NextWagonUID = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("durability", ()=>{ Durability = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagon", ()=>{ Wagons.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("engine", ()=>{ Wagons.Add(new Wagon(stf)); }),
            });
        }
	}

	public class Wagon
	{
		public string Folder;
		public string Name;
		public int UiD;
		public bool IsEngine = false;
		public bool Flip = false;

        public Wagon(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("flip", ()=>{ Flip = true; stf.SkipBlock(); }),
                new STFReader.TokenProcessor("enginedata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.SkipRestOfBlock(); IsEngine = true; }),
                new STFReader.TokenProcessor("wagondata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.SkipRestOfBlock(); }),
            });
        }

		public Wagon( int uiD, string folder, string name, bool isEngine, bool flip ) 
		{
			UiD = uiD;
			Folder = folder;
			Name = name;
			IsEngine = isEngine;
			Flip = flip;
		}
        public string GetName(uint uId, ArrayList wagonList) {
            foreach (var item in wagonList) {
                var wagon = item as Wagon;
                if (wagon.UiD == uId) {
                    return wagon.Name;
                }
            }
            return "<unknown name>";
        }
    }

    public class Player_Service_Definition
    {
        public string Name;
        public List<float> DistanceDownPath = new List<float>();
        public Player_Traffic_Definition Player_Traffic_Definition;

        public Player_Service_Definition(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("distancedownpath", ()=>{ DistanceDownPath.Add(stf.ReadFloatBlock(STFReader.UNITS.Distance, null)); }),
                new STFReader.TokenProcessor("player_traffic_definition", ()=>{ Player_Traffic_Definition = new Player_Traffic_Definition(stf); }),
            });
        }

    }

    public class Player_Traffic_Definition
    {
        public List<DateTime> ArrivalTime = new List<DateTime>();
        public List<DateTime> DepartTime = new List<DateTime>();
        public List<float> DistanceDownPath = new List<float>();
        public List<int> PlatformStartID = new List<int>();

        public string Name;

        public Player_Traffic_Definition(STFReader stf)
        {
            DateTime basedt = new DateTime();
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("arrivaltime", ()=>{ ArrivalTime.Add(basedt.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.None, null))); }),
                new STFReader.TokenProcessor("departtime", ()=>{ DepartTime.Add(basedt.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.None, null))); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ DistanceDownPath.Add(stf.ReadFloatBlock(STFReader.UNITS.Distance, null)); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ PlatformStartID.Add(stf.ReadIntBlock(STFReader.UNITS.None, null)); }),
            });
        }
    }

}
