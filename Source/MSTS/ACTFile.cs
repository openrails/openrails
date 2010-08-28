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
            STFReader f = new STFReader(filenamewithpath);
            try
            {
                while (!f.EndOfBlock()) // EOF
                {
                    string token = f.ReadToken();
                    if (0 == String.Compare(token, "Tr_Activity", true)) Tr_Activity = new Tr_Activity(f, headerOnly);
                    else f.SkipUnknownBlock(token);
                    if (headerOnly && Tr_Activity.Tr_Activity_Header != null)
                    {
                        f.Close();
                        return;
                    }
                }
                if (Tr_Activity == null)
                    throw (new STFException(f, "Missing Tr_Activity statement"));
            }
            finally
            {
                f.Close();
            }
        }
	}

	public class Tr_Activity
	{
		public int Serial = 1;
		public Tr_Activity_Header Tr_Activity_Header;
		public Tr_Activity_File Tr_Activity_File;

		public Tr_Activity( STFReader f , bool headerOnly)
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "Tr_Activity_File", true)) Tr_Activity_File = new Tr_Activity_File(f);
                if (0 == String.Compare(token, "Serial", true)) Serial = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Tr_Activity_Header", true)) Tr_Activity_Header = new Tr_Activity_Header(f);
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
                if (headerOnly && Tr_Activity_Header != null)
                    return;

            }
			if( Tr_Activity_File == null )
				throw( new STFException( f, "Missing Tr_Activity_File statement" ) );
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
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "RouteID", true)) RouteID = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Name", true)) Name = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Description", true)) Description = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Briefing", true)) Briefing = f.ReadStringBlock();
                else if (0 == String.Compare(token, "CompleteActivity", true)) CompleteActivity = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Type", true)) Type = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Mode", true)) Mode = f.ReadIntBlock();
                else if (0 == String.Compare(token, "StartTime", true)) StartTime = new StartTime(f);
                else if (0 == String.Compare(token, "Season", true)) Season = (SeasonType)f.ReadIntBlock();
                else if (0 == String.Compare(token, "Weather", true)) Weather = (WeatherType)f.ReadIntBlock();
                else if (0 == String.Compare(token, "PathID", true)) PathID = f.ReadStringBlock();
                else if (0 == String.Compare(token, "StartingSpeed", true)) StartingSpeed = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Duration", true)) Duration = new Duration(f);
                else if (0 == String.Compare(token, "Difficulty", true)) Difficulty = (Difficulty)f.ReadIntBlock();
                else if (0 == String.Compare(token, "Animals", true)) Animals = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Workers", true)) Workers = f.ReadIntBlock();
                else if (0 == String.Compare(token, "FuelWater", true)) FuelWater = f.ReadIntBlock();
                else if (0 == String.Compare(token, "FuelCoal", true)) FuelCoal = f.ReadIntBlock();
                else if (0 == String.Compare(token, "FuelDiesel", true)) FuelDiesel = f.ReadIntBlock();
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			Hour = f.ReadInt();
			Minute = f.ReadInt();
			Second = f.ReadInt();
			f.VerifyEndOfBlock();
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
			f.VerifyStartOfBlock();
			Hour = f.ReadInt();
			Minute = f.ReadInt();
			f.VerifyEndOfBlock();
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
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "Player_Service_Definition", true)) Player_Service_Definition = new Player_Service_Definition(f);
                else if (0 == String.Compare(token, "NextServiceUID", true)) NextServiceUID = f.ReadIntBlock();
                else if (0 == String.Compare(token, "NextActivityObjectUID", true)) NextActivityObjectUID = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Events", true)) Events = new Events(f);
                else if (0 == String.Compare(token, "Traffic_Definition", true)) Traffic_Definition = new Traffic_Definition(f);
                else if (0 == String.Compare(token, "ActivityObjects", true)) ActivityObjects = new ActivityObjects(f);
                else if (0 == String.Compare(token, "ActivityFailedSignals", true)) ActivityFailedSignals = new ActivityFailedSignals(f);
                else if (0 == String.Compare(token, "PlatformNumPassengersWaiting", true)) f.SkipBlock();// todo complete parse
                else if (0 == String.Compare(token, "ActivityRestrictedSpeedZones", true)) f.SkipBlock();  // todo complete parse
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			Label = f.ReadToken();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "Service_Definition", true)) this.Add(new Service_Definition(f));
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			Service = f.ReadToken();
			Time = f.ReadInt();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken();
                switch (token.ToLower())
                {
                    case "uid": UiD = f.ReadIntBlock(); break;
                    case "efficiency": f.ReadFloatBlock(); break;
                    case "skipcount": f.ReadIntBlock(); break;
                    case "distancedownpath": f.ReadFloatBlock(); break;
                    case "platformstartid": f.ReadIntBlock(); break;
                    default: f.SkipUnknownBlock(token); break;
                }
            }
        }
	}

	public class Events: ArrayList
	{
		public Events( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() ) 
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "EventCategoryLocation", true)) this.Add(new EventCategoryLocation(f));
                else if (0 == String.Compare(token, "EventCategoryAction", true)) this.Add(new EventCategoryAction(f));
                else f.SkipBlock(); // TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "EventTypeLocation", true)) { f.VerifyStartOfBlock(); f.MustMatch(")"); }
                else if (0 == String.Compare(token, "ID", true)) ID = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Activation_Level", true)) Activation_Level = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Outcomes", true)) Outcomes = new Outcomes(f);
                else if (0 == String.Compare(token, "Name", true)) Name = f.ReadStringBlock();
                else if (0 == String.Compare(token, "TextToDisplayOnCompletionIfNotTriggered", true)) TextToDisplayOnCompletionIfNotTriggered = f.ReadStringBlock();
                else if (0 == String.Compare(token, "Location", true))
                {
                    f.VerifyStartOfBlock();
                    TileX = f.ReadInt();
                    TileZ = f.ReadInt();
                    X = f.ReadDouble();
                    Z = f.ReadDouble();
                    Size = f.ReadDouble();
                    f.VerifyEndOfBlock();
                }
                else if (0 == String.Compare(token, "TriggerOnStop", true)) TriggerOnStop = f.ReadBoolBlock();
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
                if (0 == String.Compare(token, "EventTypeAllStops", true)) { f.VerifyStartOfBlock(); f.MustMatch(")"); }
                else if (0 == String.Compare(token, "ID", true)) ID = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Activation_Level", true)) Activation_Level = f.ReadIntBlock();
                else if (0 == String.Compare(token, "Outcomes", true)) Outcomes = new Outcomes(f);
                else if (0 == String.Compare(token, "TextToDisplayOnCompletionIfTriggered", true)) f.ReadStringBlock(); // ignore
                else if (0 == String.Compare(token, "TextToDisplayOnCompletionIfNotTriggered", true)) f.ReadStringBlock(); // ignore
                else if (0 == String.Compare(token, "Name", true)) Name = f.ReadStringBlock();
                else f.SkipBlock(); // TODO, when we finish it should be f.ThrowUnknownToken(token);
			}
		}

	}


	public class Outcomes: ArrayList
	{
		public Outcomes( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() ) 
			{
                string token = f.ReadToken();
                // TODO, we'll have to handle other types of activity outcomes eventually
                if (0 == String.Compare(token, "ActivitySuccess", true)) this.Add(new ActivitySuccess(f));
                else f.SkipBlock(); // TODO, when finished this line should be  f.ThrowUnknownToken(token);
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
			f.VerifyStartOfBlock();
			f.VerifyEndOfBlock();
		}

		public ActivitySuccess()
		{
		}

	}

	
	public class ActivityFailedSignals: ArrayList
	{
		public ActivityFailedSignals( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() ) 
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token,"ActivityFailedSignal", true ) ) this.Add( f.ReadIntBlock() );
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() ) 
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token,"ActivityObject", true ) ) this.Add( new ActivityObject(f) );
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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

		public ActivityObject( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token, "ObjectType" ) ) { f.VerifyStartOfBlock(); f.MustMatch( "WagonsList" ); f.VerifyEndOfBlock(); }
				else if( 0 == String.Compare( token,"Train_Config", true ) ) Train_Config = new Train_Config(f);
				else if( 0 == String.Compare( token,"Direction", true ) ) Direction = f.ReadIntBlock();
				else if( 0 == String.Compare( token,"ID", true ) ) ID = f.ReadIntBlock();
				else if( 0 == String.Compare( token,"Tile", true ) ) 
				{
					f.VerifyStartOfBlock();
					TileX = f.ReadInt();
					TileZ = f.ReadInt();
					X = f.ReadFloat();
					Z = f.ReadFloat();
					f.VerifyEndOfBlock();
				}
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
            }
		}

	}

	public class Train_Config
	{
		public TrainCfg TrainCfg;

		public Train_Config( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token,"TrainCfg", true ) ) TrainCfg = new TrainCfg(f);
                else f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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
			f.VerifyStartOfBlock();
			A = f.ReadFloat();
			B = f.ReadFloat();
			f.VerifyEndOfBlock();
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

		public TrainCfg( STFReader f )
		{
			f.VerifyStartOfBlock();
			f.ReadToken();  // Discard the "" token after the braces
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token,"Name", true ) ) Name = f.ReadStringBlock();
				else if( 0 == String.Compare( token,"Serial", true ) ) Serial = f.ReadIntBlock();
				else if( 0 == String.Compare( token,"MaxVelocity", true ) ) MaxVelocity = new MaxVelocity( f );
				else if( 0 == String.Compare( token,"NextWagonUID", true ) ) NextWagonUID = f.ReadIntBlock();
 			    else if( 0 == String.Compare( token,"Durability", true ) ) Durability = (float) f.ReadDoubleBlock();
				else if( 0 == String.Compare( token,"Wagon", true ) ) Wagons.Add( new Wagon( f ) );
				else if( 0 == String.Compare( token,"Engine", true ) ) Wagons.Add( new Wagon( f ) );
                else 
                f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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

		public Wagon( STFReader f )
		{
			f.VerifyStartOfBlock();
			while( !f.EndOfBlock() )
			{
                string token = f.ReadToken();
				if( 0 == String.Compare( token,"UiD", true ) ) UiD = f.ReadIntBlock();
				else if( 0 == String.Compare( token,"Flip", true ) ) { Flip = true; f.VerifyStartOfBlock(); f.VerifyEndOfBlock(); }
				else if( 0 == String.Compare( token,"WagonData", true )
					||   0 == String.Compare( token,"EngineData", true ))
				{
					if( 0 == String.Compare( token,"EngineData", true )) IsEngine = true;
					f.VerifyStartOfBlock();
					Name = f.ReadToken();
					Folder = f.ReadToken();
					f.VerifyEndOfBlock();
				}
				else 
                   f.SkipBlock(); //TODO complete parse and replace with f.SkipUnknownBlock(token);
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

            f.VerifyStartOfBlock();
            Name = f.ReadToken();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken().ToLower();
                switch (token)
                {
                    case "distancedownpath": DistanceDownPath.Add(f.ReadFloatBlock()); break;
                    case "player_traffic_definition": Player_Traffic_Definition = new Player_Traffic_Definition(f); break;
                    default: f.SkipBlock(); break;
                }
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

            f.VerifyStartOfBlock();
            Name = f.ReadToken();
            while (!f.EndOfBlock())
            {
                string token = f.ReadToken().ToLower();
                switch (token)
                {
                    case "arrivaltime": ArrivalTime.Add(basedt.AddSeconds(f.ReadFloatBlock())); break;
                    case "departtime": DepartTime.Add(basedt.AddSeconds(f.ReadFloatBlock())); break;
                    case "distancedownpath": DistanceDownPath.Add(f.ReadFloatBlock()); break;
                    case "platformstartid": PlatformStartID.Add(f.ReadIntBlock()); break;
                    default: f.SkipBlock(); break;
                }
            }
        }
    }

}
