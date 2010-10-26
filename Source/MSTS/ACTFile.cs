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
            using (STFReader f = new STFReader(filenamewithpath))
            {
                while (!f.EOF)
                {
                    switch (f.ReadItem().ToLower())
                    {
                        case "tr_activity": Tr_Activity = new Tr_Activity(f, headerOnly); break;
                        case "(": f.SkipRestOfBlock(); break;
                    }
                    if (headerOnly && Tr_Activity.Tr_Activity_Header != null)
                        return;
                }
                if (Tr_Activity == null)
                    throw new STFException(f, "Missing Tr_Activity statement");
            }
        }
	}

    public class Tr_Activity
    {
        public int Serial = 1;
        public Tr_Activity_Header Tr_Activity_Header;
        public Tr_Activity_File Tr_Activity_File;

        public Tr_Activity(STFReader f, bool headerOnly)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                switch (f.ReadItem().ToLower())
                {
                    case "tr_activity_file": Tr_Activity_File = new Tr_Activity_File(f); break;
                    case "serial": Serial = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "tr_activity_header": Tr_Activity_Header = new Tr_Activity_Header(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
                if (headerOnly && Tr_Activity_Header != null)
                    return;
            }
            if (Tr_Activity_File == null)
                throw new STFException(f, "Missing Tr_Activity_File statement");
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

		public Tr_Activity_Header( STFReader f )
		{
			f.MustMatch("(");
			while( !f.EndOfBlock() )
                switch(f.ReadItem().ToLower())
                {
                    case "routeid": RouteID = f.ReadItemBlock(null); break;
                    case "name": Name = f.ReadItemBlock(null); break;
                    case "description": Description = f.ReadItemBlock(null); break;
                    case "briefing": Briefing = f.ReadItemBlock(null); break;
                    case "completeactivity": CompleteActivity = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "type": Type = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "mode": Mode = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "starttime": StartTime = new StartTime(f); break;
                    case "season": Season = (SeasonType)f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "weather": Weather = (WeatherType)f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "pathid": PathID = f.ReadItemBlock(null); break;
                    case "startingspeed": StartingSpeed = f.ReadIntBlock(STFReader.UNITS.Speed, null); break;
                    case "duration": Duration = new Duration(f); break;
                    case "difficulty": Difficulty = (Difficulty)f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "animals": Animals = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "workers": Workers = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "fuelwater": FuelWater = f.ReadIntBlock(STFReader.UNITS.Any, null); break;
                    case "fuelcoal": FuelCoal = f.ReadIntBlock(STFReader.UNITS.Any, null); break;
                    case "fueldiesel": FuelDiesel = f.ReadIntBlock(STFReader.UNITS.Any, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

		public StartTime( STFReader f )
		{
			f.MustMatch("(");
            Hour = f.ReadInt(STFReader.UNITS.None, null);
            Minute = f.ReadInt(STFReader.UNITS.None, null);
            Second = f.ReadInt(STFReader.UNITS.None, null);
			f.SkipRestOfBlock();
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

		public Duration( STFReader f )
		{
			f.MustMatch("(");
            Hour = f.ReadInt(STFReader.UNITS.None, null);
            Minute = f.ReadInt(STFReader.UNITS.None, null);
			f.SkipRestOfBlock();
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

		public Tr_Activity_File( STFReader f )
		{
			f.MustMatch("(");
			while( !f.EndOfBlock() )
                switch(f.ReadItem().ToLower())
                {
                    case "player_service_definition": Player_Service_Definition = new Player_Service_Definition(f); break;
                    case "nextserviceuid": NextServiceUID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "nextactivityobjectuid": NextActivityObjectUID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "events": Events = new Events(f); break;
                    case "traffic_definition": Traffic_Definition = new Traffic_Definition(f); break;
                    case "activityobjects": ActivityObjects = new ActivityObjects(f); break;
                    case "activityfailedsignals": ActivityFailedSignals = new ActivityFailedSignals(f); break;
                    case "platformnumpassengerswaiting": f.SkipBlock(); break;
                    case "activityrestrictedspeedzones": f.SkipBlock(); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

		public Traffic_Definition( STFReader f )
		{
			f.MustMatch("(");
			Label = f.ReadItem();
            while (!f.EndOfBlock())
                switch(f.ReadItem().ToLower())
                {
                    case "service_definition": this.Add(new Service_Definition(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}
	}

	public class Service_Definition
	{
		public string Service;
		public int Time;
		public int UiD;

		public Service_Definition( STFReader f )
		{
			f.MustMatch("(");
			Service = f.ReadItem();
            Time = f.ReadInt(STFReader.UNITS.None, null);
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "uid": UiD = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "efficiency": f.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                    case "skipcount": f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "distancedownpath": f.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "platformstartid": f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
	}

	public class Events: ArrayList
	{
		public Events( STFReader f )
		{
			f.MustMatch("(");
            while (!f.EndOfBlock()) 
                switch (f.ReadItem().ToLower())
                {
                    case "eventcategorylocation": this.Add(new EventCategoryLocation(f)); break;
                    case "eventcategoryaction": this.Add(new EventCategoryAction(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

		public EventCategoryLocation( STFReader f )
		{
			f.MustMatch("(");
			while( !f.EndOfBlock() )
                switch (f.ReadItem().ToLower())
                {
                    case "eventtypelocation": f.MustMatch("("); f.MustMatch(")"); break;
                    case "id": ID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "activation_level": Activation_Level = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "outcomes": Outcomes = new Outcomes(f); break;
                    case "name": Name = f.ReadItemBlock(null); break;
                    case "texttodisplayoncompletionifnottriggered": TextToDisplayOnCompletionIfNotTriggered = f.ReadItemBlock(null); break;
                    case "triggeronstop": TriggerOnStop = f.ReadBoolBlock(true); break;
                    case "location":
                        f.MustMatch("(");
                        TileX = f.ReadInt(STFReader.UNITS.None, null);
                        TileZ = f.ReadInt(STFReader.UNITS.None, null);
                        X = f.ReadDouble(STFReader.UNITS.None, null);
                        Z = f.ReadDouble(STFReader.UNITS.None, null);
                        Size = f.ReadDouble(STFReader.UNITS.None, null);
                        f.SkipRestOfBlock();
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

		public EventCategoryAction( STFReader f )
		{
			f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "eventtypeallstops": f.MustMatch("("); f.MustMatch(")"); break;
                    case "id": ID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "activation_level": Activation_Level = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "outcomes": Outcomes = new Outcomes(f); break;
                    case "texttodisplayoncompletioniftriggered": f.ReadItemBlock(""); break;
                    case "texttodisplayoncompletionifnotrriggered": f.ReadItemBlock(""); break;
                    case "name": Name = f.ReadItemBlock(""); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}
	}


	public class Outcomes: ArrayList
	{
		public Outcomes( STFReader f )
		{
			f.MustMatch("(");
            // TODO, we'll have to handle other types of activity outcomes eventually
            while (!f.EndOfBlock()) 
                switch (f.ReadItem().ToLower())
                {

                    case "activitysuccess": this.Add(new ActivitySuccess(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
		}

		public Outcomes()
		{
		}

	}


	public class ActivitySuccess // a type of outcome
	{
		public ActivitySuccess( STFReader f )
		{
			f.MustMatch("(");
			f.SkipRestOfBlock();
		}

		public ActivitySuccess()
		{
		}

	}

	
	public class ActivityFailedSignals: ArrayList
	{
		public ActivityFailedSignals( STFReader f )
		{
			f.MustMatch("(");
			while( !f.EndOfBlock() ) 
                switch(f.ReadItem().ToLower())
                {
                    case"activityfailedsignal": this.Add(f.ReadIntBlock(STFReader.UNITS.None, null)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

		public ActivityObjects( STFReader f )
		{
			f.MustMatch("(");
			while( !f.EndOfBlock() ) 
                switch(f.ReadItem().ToLower())
                {
                    case "activityobject": this.Add(new ActivityObject(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
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

        public ActivityObject(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "objecttype": f.MustMatch("("); f.MustMatch("WagonsList"); f.SkipRestOfBlock(); break;
                    case "train_config": Train_Config = new Train_Config(f); break;
                    case "direction": Direction = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "id": ID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "tile":
                        f.MustMatch("(");
                        TileX = f.ReadInt(STFReader.UNITS.None, null);
                        TileZ = f.ReadInt(STFReader.UNITS.None, null);
                        X = f.ReadFloat(STFReader.UNITS.None, null);
                        Z = f.ReadFloat(STFReader.UNITS.None, null);
                        f.SkipRestOfBlock();
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
	}

	public class Train_Config
	{
		public TrainCfg TrainCfg;

        public Train_Config(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "traincfg": TrainCfg = new TrainCfg(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
	}


	public class MaxVelocity
	{
		public float A = 0;
		public float B = 0.001f;

		public MaxVelocity()
		{
		}

		public MaxVelocity( STFReader f )
		{
			f.MustMatch("(");
            A = f.ReadFloat(STFReader.UNITS.Speed, null);
            B = f.ReadFloat(STFReader.UNITS.Speed, null);
			f.SkipRestOfBlock();
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

        public TrainCfg(STFReader f)
        {
            f.MustMatch("(");
            f.ReadItem();  // Discard the "" lowertoken after the braces
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "name": Name = f.ReadItemBlock(null); break;
                    case "serial": Serial = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "maxvelocity": MaxVelocity = new MaxVelocity(f); break;
                    case "nextwagonuid": NextWagonUID = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "durability": Durability = (float)f.ReadDoubleBlock(STFReader.UNITS.None, null); break;
                    case "wagon": Wagons.Add(new Wagon(f)); break;
                    case "engine": Wagons.Add(new Wagon(f)); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
	}

	public class Wagon
	{
		public string Folder;
		public string Name;
		public int UiD;
		public bool IsEngine = false;
		public bool Flip = false;

        public Wagon(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "uid": UiD = f.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "flip": Flip = true; f.SkipBlock(); break;
                    case "enginedata": f.MustMatch("("); Name = f.ReadItem(); Folder = f.ReadItem(); f.SkipRestOfBlock(); IsEngine = true; break;
                    case "wagondata": f.MustMatch("("); Name = f.ReadItem(); Folder = f.ReadItem(); f.SkipRestOfBlock(); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

		public Wagon( int uiD, string folder, string name, bool isEngine, bool flip ) 
		{
			UiD = uiD;
			Folder = folder;
			Name = name;
			IsEngine = isEngine;
			Flip = flip;
		}

	}

    public class Player_Service_Definition
    {
        public string Name;
        public List<float> DistanceDownPath = new List<float>();
        public Player_Traffic_Definition Player_Traffic_Definition;

        public Player_Service_Definition(STFReader f)
        {
            StringBuilder s = new StringBuilder();

            f.MustMatch("(");
            Name = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "distancedownpath": DistanceDownPath.Add(f.ReadFloatBlock(STFReader.UNITS.Distance, null)); break;
                    case "player_traffic_definition": Player_Traffic_Definition = new Player_Traffic_Definition(f); break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }

    }

    public class Player_Traffic_Definition
    {
        public List<DateTime> ArrivalTime = new List<DateTime>();
        public List<DateTime> DepartTime = new List<DateTime>();
        public List<float> DistanceDownPath = new List<float>();
        public List<int> PlatformStartID = new List<int>();

        public string Name;

        public Player_Traffic_Definition(STFReader f)
        {
            StringBuilder s = new StringBuilder();
            DateTime basedt = new DateTime();

            f.MustMatch("(");
            Name = f.ReadItem();
            while (!f.EndOfBlock())
                switch (f.ReadItem().ToLower())
                {
                    case "arrivaltime": ArrivalTime.Add(basedt.AddSeconds(f.ReadFloatBlock(STFReader.UNITS.None, null))); break;
                    case "departtime": DepartTime.Add(basedt.AddSeconds(f.ReadFloatBlock(STFReader.UNITS.None, null))); break;
                    case "distancedownpath": DistanceDownPath.Add(f.ReadFloatBlock(STFReader.UNITS.Distance, null)); break;
                    case "platformstartid": PlatformStartID.Add(f.ReadIntBlock(STFReader.UNITS.None, null)); break;
                    case "(": f.SkipRestOfBlock(); break;
            }
        }
    }

}
