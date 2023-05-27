// COPYRIGHT 2009, 2010, 2011, 2013, 2014, 2015 by the Open Rails project.
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

//EBNF
//Usage follows http://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_Form

//Usage 			Notation
//================ ========
//definition 		 =
//concatenation 	 ,
//termination 	 ;
//alternation 	 |
//option 			 [ ... ]
//repetition 		 { ... }
//grouping 		 ( ... )
//terminal string  " ... "
//terminal string  ' ... '
//comment 		 (* ... *)
//special sequence ? ... ?
//exception 		 -

//(* MSTS Activity syntax in EBNF *)
//(* Note inconsistent use of "_" in names *)
//(* Note very similar names for different elements: ID v UiD, WagonsList v Wagon_List v Wagon, Train_Config v TrainCfg *)
//(* Note some percentages as integers 0-100 and some as float, e.g. 0.75 *)
//(* Note some times as 3*Integer for hr, min, sec and some as Integer seconds since some reference. *)
//(* As with many things Microsoft, text containing spaces must be enclosed in "" and text with no spaces needs no delimiter. *)

//Tr_Activity =
//    "(", Serial, Tr_Activity_Header, Tr_Activity_File, ")" ;

//    Serial = "Serial", "(", Integer, ")" ;

//    Tr_Activity_Header = "Tr_Activity_Header",
//        "(", *[ RouteID | Name | Description | Briefing | CompleteActivity
//        | Type | Mode | StartTime | Season | Weather | PathID | StartingSpeed | Duration | Difficulty
//        | Animals | Workers | FuelWater | FuelCoal | FuelDiesel ] ")" ; 
//        (* 1 or more options. Sequence is probably not significant. 
//           No information about which options are required or checking for duplicates. 
//         *)

//        RouteID = "RouteID", "(", Text, ")" ;  (* file name *)

//        Name = "Name", "(", Text, ")" ;

//        Description = "Description", "(", Text, ")" ;

//        Briefing = "Briefing", "(", ParagraphText, ")" ;

//            ParagraphText = Text, *( "+", Text ) ;

//        CompleteActivity = "CompleteActivity", "(", Integer, ")" ;	(* 1 for true (to be checked) *)

//        Type = "Type", "(", Integer, ")" ;	(* 0 (default) for ??? (to be checked) *)

//        Mode = "Mode", "(", Integer, ")" ;	(* 2 (default) for ??? (to be checked) *)

//        StartTime = "StartTime", "(", 3*Integer, ")" ;  (* Hour, Minute, Second (default is 10am) *)

//        Season = "Season", "(", Integer, ")" ;	(* Spring=0, Summer (default), Autumn, Winter *)

//        Weather = "Weather", "(", Integer, ")" ;	(* Clear=0 (default), Snow, Rain *)

//        PathID = "PathID", "(", Text , ")" ; 

//        StartingSpeed = "StartingSpeed", "(", Integer, ")" ;	(* 0 (default) for meters/second *) (* Why integer? *)

//        Duration = "Duration", "(", 2*Integer, ")" ;  (* Hour , Minute (default is 1 hour) *)

//        Difficulty = "Difficulty", "(", Integer, ")" ;	(* Easy=0 (default), Medium, Hard *)

//        Animals = "Animals", "(", Integer, ")" ;	(* 0-100 for % (default is 100) *)

//        Workers = "Workers", "(", Integer, ")" ;	(* 0-100 for % (default is 0) *)

//        FuelWater = "FuelWater", "(", Integer, ")";	(* 0-100 for % (default is 100) *)

//        FuelCoal = "FuelCoal", "(", Integer, ")";	(* 0-100 for % (default is 100) *)

//        FuelDiesel = "FuelDiesel", "(", Integer, ")";	(* 0-100 for % (default is 100) *)

//    Tr_Activity_File = "Tr_Activity_File", 
//        "(", *[ Player_Service_Definition | NextServiceUID | NextActivityObjectUID
//        | Traffic_Definition | Events | ActivityObjects | ActivityFailedSignals | PlatformNumPassengersWaiting | ActivityRestrictedSpeedZones ] ")" ;

//        Player_Service_Definition = "Player_Service_Definition",	(* Text is linked to PathID somehow. *)
//            "(", Text, [ Player_Traffic_Definition | UiD | *Player_Service_Item ], ")" ;    (* Code suggests just one Player_Traffic_Definition *)

//                Player_Traffic_Definition = "Player_Traffic_Definition", 
//                    "(", Integer, *( Player_Traffic_Item ), ")" ;

//                    Player_Traffic_Item =	(* Note lack of separator between Player_Traffic_Items. 
//                                               For simplicity, parser creates a new object whenever PlatformStartID is parsed. *)
//                        *[ "ArrivalTime", "(", Integer, ")"
//                         | "DepartTime", "(", Integer, ")"
//                         | "SkipCount", "(", Integer, ")"
//                         | "DistanceDownPath", "(", Float, ")" ],
//                        "PlatformStartID", "(", Integer, ")" ;

//                UiD = "UiD", "(", Integer, ")" ;

//                Player_Service_Item =	(* Note lack of separator between Player_Service_Items *)
//                                           For simplicity, parser creates a new object whenever PlatformStartID is parsed. *)
//                    *[ "Efficiency", "(", Float, ")"   (* e.g. 0.75 for 75% efficient? *)
//                     | "SkipCount", "(", Integer, ")"
//                     | "DistanceDownPath", "(", Float, ")" ],
//                    "PlatformStartID", "(", Integer, ")" ;

//        NextServiceUID = "NextServiceUID", "(", Integer, ")" ;

//        NextActivityObjectUID = "NextActivityObjectUID", "(", Integer, ")" ;

//        Traffic_Definition = "Traffic_Definition", "(", Text, *Service_Definition, ")" ;

//            Service_Definition = "Service_Definition",
//                "(", Text, Integer, UiD, *Player_Service_Item, ")" ;  (* Integer is time in seconds *)

//        Events = "Events", 
//            "(", *[ EventCategoryLocation | EventCategoryAction | EventCategoryTime ], ")" ;  (* CategoryTime *)

//            EventCategoryLocation = "EventCategoryLocation", 
//                "(", *[ EventTypeLocation | ID | Activation_Level | Outcomes
//                | Name | Location | TriggerOnStop ], ")" ;  (* ID and Name defined above *)	

//                EventTypeLocation = "EventTypeLocation", "(", ")" ;

//                ID = "ID", "(", Integer, ")" ;

//                Activation_Level = "Activation_Level", "(", Integer, ")" ;

//                Outcomes = "Outcomes",
//                    "(", *[ ActivitySuccess | ActivityFail | ActivateEvent | RestoreActLevel | DecActLevel | IncActLevel | DisplayMessage ], ")" ;

//                    ActivitySuccess = "ActivitySuccess", "(", ")" ;   (* No text parameter *)

//                    ActivityFail = "ActivityFail", "(", Text, ")" ;

//                    ActivateEvent = "ActivateEvent", "(", Integer, ")" ;

//                    RestoreActLevel = "RestoreActLevel", "(", Integer, ")" ;

//                    DecActLevel = "DecActLevel", "(", Integer, ")" ;

//                    IncActLevel = "IncActLevel", "(", Integer, ")" ;  (* Some MSTS samples have more than a single IncActLevel *)

//                    DisplayMessage = "DisplayMessage", "(", Text, ")" ;

//                Location = "Location", "(", 5*Integer, ")" ;

//                TriggerOnStop = "TriggerOnStop", "(", Integer, ")" ;  (* 0 for ?? *)

//                TextToDisplayOnCompletionIfTriggered = "TextToDisplayOnCompletionIfTriggered", "(", ParagraphText, ")" ;

//                TextToDisplayOnCompletionIfNotTriggered = "TextToDisplayOnCompletionIfNotTriggered", "(", ParagraphText, ")" ;

//            EventCategoryAction = "EventCategoryAction", 
//                "(", *[ EventType | ID | Activation_Level
//                | Outcomes | Reversable_Event | Name | Wagon_List | SidingItem | StationStop | Speed ] ;  (* ID, Activation_Level, Outcomes and Name defined above *)					

//                EventType =
//                    [ EventTypeAllStops | EventTypeAssembleTrain
//                    | EventTypeAssembleTrainAtLocation | EventTypeDropOffWagonsAtLocation 
//                    | EventTypePickUpPassengers | EventTypePickUpWagons 
//                    | EventTypeReachSpeed ] ;

//                    EventTypeAllStops = "EventTypeAllStops", "(", ")" ;

//                    EventTypeAssembleTrain = "EventTypeAssembleTrain", "(", ")" ;

//                    EventTypeAssembleTrainAtLocation = "EventTypeAssembleTrainAtLocation", "(", ")" ;

//                    EventTypeDropOffWagonsAtLocation = "EventTypeDropOffWagonsAtLocation", "(", ")" ;

//                    EventTypePickUpPassengers = "EventTypePickUpPassengers", "(", ")" ;

//                    EventTypePickUpWagons = "EventTypePickUpWagons", "(", ")" ;

//                    EventTypeReachSpeed = "EventTypeReachSpeed", "(", ")" ;

//                Reversable_Event = [ "Reversable_Event" | "Reversible_Event" ],  (* Reversable is not listed at www.learnersdictionary.com *) 
//                    "(", ")" ;

//                SidingItem =  "(", Integer, ")" ;

//                Wagon_List = "Wagon_List", "(", *WagonListItem, ")" ;

//                    WagonListItem = (* Description omitted from PickUpWagons and sometimes from DropOffWagonsAtLocation *)
//                        UiD, SidingItem, [ "Description", "(", Text, ")" ] ;  (" MSTS uses SidingItem inside the Wagon_List and also at the same level *)

//                StationStop = 

//                Speed = "(", Integer, ")" ;

//            EventCategoryTime = "EventCategoryTime", "(",  (* single instance of each alternative *)
//                [ EventTypeTime | ID | Activation_Level | Outcomes | TextToDisplayOnCompletionIfTriggered 
//                | TextToDisplayOnCompletionIfNotTriggered | Name | Time ], ")" ;  (* Outcomes may have empty parameters *)

//                EventTypeTime = "EventTypeTime", "(", ")" ;

//                Time = "Time", "(", Integer, ")" ;

//        ActivityObjects	= "ActivityObjects", "(", *ActivityObject, ")" ;

//            ActivityObject = "ActivityObject", 
//                "(", *[ ObjectType | Train_Config | Direction | ID | Tile ], ")" ;  (* ID defined above *)

//                ObjectType = "ObjectType", 
//                    "(", [ "WagonsList" | ?? ], ")" ;

//                Train_Config = "Train_Config", "(", TrainCfg, ")" ;

//                    TrainCfg = "TrainCfg", 
//                        "(", [ Name | Serial | MaxVelocity | NextWagonUID | Durability | Wagon | Engine ], ")" ;

//                        Serial = "Serial", "(", Integer, ")" ;

//                        MaxVelocity = "MaxVelocity", "(", 2*Float, ")" ;

//                        NextWagonUID = "NextWagonUID", "(", Integer, ")" ;

//                        Durability = "Durability", "(", Float, ")" ;

//                        Wagon = "Wagon", 
//                            "(", *[ WagonData | UiD ], ")" ;  (* UiD defined above *)

//                            WagonData = "WagonData", "(", 2*Text, ")" ;

//                        Engine = "Engine", "(", *[ UiD | EngineData ], ")" ;  (* UiD defined above *)

//                            EngineData = "EngineData", 
//                                "(", 2*Text, ")" ;

//                Direction = "Direction", "(", Integer, ")" ;  (* 0 for ??, 1 for ?? *)

//                Tile = "Tile", "(", 2*Integer, 2*Float, ")" ;

//        ActivityFailedSignals = "ActivityFailedSignals", "(", *ActivityFailedSignal, ")" ;

//            ActivityFailedSignal = "ActivityFailedSignal", "(", Integer, ")" ;

//        PlatformNumPassengersWaiting = "PlatformNumPassengersWaiting", "(", *PlatformData, ")" ;

//            PlatformData = "PlatformData", "(", 2*Integer, ")" ;

//        ActivityRestrictedSpeedZones = "ActivityRestrictedSpeedZones", "(", *ActivityRestrictedSpeedZone, ")" ;

//            ActivityRestrictedSpeedZone = "ActivityRestrictedSpeedZone",
//                "(", StartPosition, EndPosition, ")" ;

//                StartPosition = "StartPosition, "(", 4*Integer, ")" ;

//                EndPosition = "EndPosition", "(", 4*Integer, ")" ;


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Parsers.Msts; // For class S (seconds)
using ORTS.Common;

namespace Orts.Formats.Msts
{
    public enum SeasonType { Spring = 0, Summer, Autumn, Winter }
    public enum WeatherType { Clear = 0, Snow, Rain }
    public enum Difficulty { Easy = 0, Medium, Hard }
    public enum EventType
    {
        AllStops = 0, AssembleTrain, AssembleTrainAtLocation, DropOffWagonsAtLocation, PickUpPassengers,
        PickUpWagons, ReachSpeed
    }
    public enum ActivityMode
    {
        IntroductoryTrainRide = 0,
        Player = 2,
        Tutorial = 3,
    }

    public struct LoadData
    {
        public string Name;
        public string Folder;
        public LoadPosition LoadPosition;
        public LoadState LoadState;
    }

    /// <summary>
    /// Parse and *.act file.
    /// Naming for classes matches the terms in the *.act file.
    /// </summary>
    public class ActivityFile
    {
        public Tr_Activity Tr_Activity;

        public ActivityFile(string filenamewithpath)
        {
            Read(filenamewithpath, false);
        }

        public ActivityFile(string filenamewithpath, bool headerOnly)
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
                if (Tr_Activity == null)
                    STFException.TraceWarning(stf, "Missing Tr_Activity statement");
            }
        }

        public void InsertORSpecificData(string filenamewithpath)
        {
            using (STFReader stf = new STFReader(filenamewithpath, false))
            {
                var tr_activityTokenPresent = false;
                stf.ParseFile(() => false && (Tr_Activity.Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_activity", ()=>{ tr_activityTokenPresent = true;  Tr_Activity.InsertORSpecificData (stf); }),
                    });
                if (!tr_activityTokenPresent)
                    STFException.TraceWarning(stf, "Missing Tr_Activity statement");
            }
        }

        // Used for explore in activity mode
        public ActivityFile()
        {
            Tr_Activity = new Tr_Activity();
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
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("tr_activity_header", ()=>{ Tr_Activity_Header = new Tr_Activity_Header(stf); }),
            });
            if (!headerOnly && (Tr_Activity_File == null))
                STFException.TraceWarning(stf, "Missing Tr_Activity_File statement");
        }

        public void InsertORSpecificData(STFReader stf)
        {
            stf.MustMatch("(");
            var tr_activity_fileTokenPresent = false;
            stf.ParseBlock(() => false && (Tr_Activity_Header != null), new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("tr_activity_file", ()=>{ tr_activity_fileTokenPresent = true;  Tr_Activity_File.InsertORSpecificData (stf); }),
            });
            if (!tr_activity_fileTokenPresent)
                STFException.TraceWarning(stf, "Missing Tr_Activity_File statement");
        }

        // Used for explore in activity mode
        public Tr_Activity()
        {
            Serial = -1;
            Tr_Activity_Header = new Tr_Activity_Header();
            Tr_Activity_File = new Tr_Activity_File();
        }
    }

    public class Tr_Activity_Header
    {
        public string RouteID;
        public string Name;					// AE Display Name
        public string Description = " ";
        public string Briefing = " ";
        public int CompleteActivity = 1;    // <CJComment> Should be boolean </CJComment>
        public int Type;
        public ActivityMode Mode = ActivityMode.Player;
        public StartTime StartTime = new StartTime(10, 0, 0);
        public SeasonType Season = SeasonType.Summer;
        public WeatherType Weather = WeatherType.Clear;
        public string PathID;
        public int StartingSpeed;       // <CJComment> Should be float </CJComment>
        public Duration Duration = new Duration(1, 0);
        public Difficulty Difficulty = Difficulty.Easy;
        public int Animals = 100;		// percent
        public int Workers; 			// percent
        public int FuelWater = 100;		// percent
        public int FuelCoal = 100;		// percent
        public int FuelDiesel = 100;	// percent
        public string LoadStationsPopulationFile;

        public Tr_Activity_Header(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("routeid", ()=>{ RouteID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(Description); }),
                new STFReader.TokenProcessor("briefing", ()=>{ Briefing = stf.ReadStringBlock(Briefing); }),
                new STFReader.TokenProcessor("completeactivity", ()=>{ CompleteActivity = stf.ReadIntBlock(CompleteActivity); }),
                new STFReader.TokenProcessor("type", ()=>{ Type = stf.ReadIntBlock(Type); }),
                new STFReader.TokenProcessor("mode", ()=>{ Mode = (ActivityMode)stf.ReadIntBlock((int)Mode); }),
                new STFReader.TokenProcessor("starttime", ()=>{ StartTime = new StartTime(stf); }),
                new STFReader.TokenProcessor("season", ()=>{ Season = (SeasonType)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("weather", ()=>{ Weather = (WeatherType)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("pathid", ()=>{ PathID = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("startingspeed", ()=>{ StartingSpeed = (int)stf.ReadFloatBlock(STFReader.UNITS.Speed, (float)StartingSpeed); }),
                new STFReader.TokenProcessor("duration", ()=>{ Duration = new Duration(stf); }),
                new STFReader.TokenProcessor("difficulty", ()=>{ Difficulty = (Difficulty)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("animals", ()=>{ Animals = stf.ReadIntBlock(Animals); }),
                new STFReader.TokenProcessor("workers", ()=>{ Workers = stf.ReadIntBlock(Workers); }),
                new STFReader.TokenProcessor("fuelwater", ()=>{ FuelWater = stf.ReadIntBlock(FuelWater); }),
                new STFReader.TokenProcessor("fuelcoal", ()=>{ FuelCoal = stf.ReadIntBlock(FuelCoal); }),
                new STFReader.TokenProcessor("fueldiesel", ()=>{ FuelDiesel = stf.ReadIntBlock(FuelDiesel); }),
                new STFReader.TokenProcessor("ortsloadstationspopulation", ()=>{ LoadStationsPopulationFile = stf.ReadStringBlock(null); }),
            });
        }

        // Used for explore in activity mode
        public Tr_Activity_Header()
        {
        }
    }

    public class StartTime
    {
        public int Hour;
        public int Minute;
        public int Second;

        public StartTime(int h, int m, int s)
        {
            Hour = h;
            Minute = m;
            Second = s;
        }

        public StartTime(STFReader stf)
        {
            stf.MustMatch("(");
            Hour = stf.ReadInt(null);
            Minute = stf.ReadInt(null);
            Second = stf.ReadInt(null);
            stf.MustMatch(")");
        }

        public String FormattedStartTime()
        {
            return Hour.ToString("00") + ":" + Minute.ToString("00") + ":" + Second.ToString("00");
        }
    }

    public class Duration
    {
        int Hour;
        int Minute;
        int Second;

        public Duration(int h, int m)
        {
            Hour = h;
            Minute = m;
            Second = 0;
        }

        public Duration(STFReader stf)
        {
            stf.MustMatch("(");
            Hour = stf.ReadInt(null);
            Minute = stf.ReadInt(null);
            stf.MustMatch(")");
        }

        public int ActivityDuration()
        {
            return Hour * 3600 + Minute * 60 + Second; // Convert time to seconds
        }

        public String FormattedDurationTime()
        {
            return Hour.ToString("00") + ":" + Minute.ToString("00");
        }

        public String FormattedDurationTimeHMS()
        {
            return Hour.ToString("00") + ":" + Minute.ToString("00") + ":" + Second.ToString("00");
        }

    }

    public class Tr_Activity_File
    {
        public Player_Service_Definition Player_Service_Definition;
        public int NextServiceUID = 1;
        public int NextActivityObjectUID = 32786;
        public ActivityObjects ActivityObjects;
        public ActivityFailedSignals ActivityFailedSignals;
        public Events Events;
        public Traffic_Definition Traffic_Definition;
        public PlatformNumPassengersWaiting PlatformNumPassengersWaiting;
        public ActivityRestrictedSpeedZones ActivityRestrictedSpeedZones;
        public bool AIBlowsHornAtLevelCrossings { get; private set; } = false;
        public LevelCrossingHornPattern AILevelCrossingHornPattern { get; private set; } = LevelCrossingHornPattern.Single;


        public Tr_Activity_File(STFReader stf)
        {
            stf.MustMatch("(");
            var parser = new List<STFReader.TokenProcessor> {
                new STFReader.TokenProcessor("player_service_definition",()=>{ Player_Service_Definition = new Player_Service_Definition(stf); }),
                new STFReader.TokenProcessor("nextserviceuid",()=>{ NextServiceUID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("nextactivityobjectuid",()=>{ NextActivityObjectUID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("events",()=>{ Events = new Events(stf); }),
                new STFReader.TokenProcessor("traffic_definition",()=>{ Traffic_Definition = new Traffic_Definition(stf); }),
                new STFReader.TokenProcessor("activityobjects",()=>{ ActivityObjects = new ActivityObjects(stf); }),
                new STFReader.TokenProcessor("platformnumpassengerswaiting",()=>{ PlatformNumPassengersWaiting = new PlatformNumPassengersWaiting(stf); }),  // 35 files. To test, use EUROPE1\ACTIVITIES\aftstorm.act
                new STFReader.TokenProcessor("activityfailedsignals",()=>{ ActivityFailedSignals = new ActivityFailedSignals(stf); }),
                new STFReader.TokenProcessor("activityrestrictedspeedzones",()=>{ ActivityRestrictedSpeedZones = new ActivityRestrictedSpeedZones(stf); }),   // 27 files. To test, use EUROPE1\ACTIVITIES\lclsrvce.act
            };
            parser.AddRange(ORSpecificDataTokenProcessors(stf));
            stf.ParseBlock(parser);
        }

        // Used for explore in activity mode
        public Tr_Activity_File()
        {
            Player_Service_Definition = new Player_Service_Definition();
        }

        //public void ClearStaticConsists()
        //{
        //    NextActivityObjectUID = 32786;
        //    ActivityObjects.Clear();
        //}
        public void InsertORSpecificData(STFReader stf)
        {
            stf.MustMatch("(");
            var parser = new List<STFReader.TokenProcessor> {
                new STFReader.TokenProcessor("events",()=>
                {
                    if ( Events == null) Events = new Events(stf);
                    else Events.InsertORSpecificData (stf);
                }
                ),
            };
            parser.AddRange(ORSpecificDataTokenProcessors(stf));
            stf.ParseBlock(parser);
        }

        private IEnumerable<STFReader.TokenProcessor> ORSpecificDataTokenProcessors(STFReader stf)
        {
            yield return new STFReader.TokenProcessor("ortsaihornatcrossings", () =>
            {
                AIBlowsHornAtLevelCrossings = stf.ReadIntBlock(Convert.ToInt32(AIBlowsHornAtLevelCrossings)) > 0;
            });
            yield return new STFReader.TokenProcessor("ortsaicrossinghornpattern", () =>
            {
                if (Enum.TryParse<LevelCrossingHornPattern>(stf.ReadStringBlock(""), ignoreCase: true, out var value))
                    AILevelCrossingHornPattern = value;
            });
        }
    }

    public class Player_Service_Definition
    {
        public string Name;
        public Player_Traffic_Definition Player_Traffic_Definition;

        public Player_Service_Definition(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("player_traffic_definition", ()=>{ Player_Traffic_Definition = new Player_Traffic_Definition(stf); }),
            });
        }

        // Used for explore in activity mode
        public Player_Service_Definition()
        {
            Player_Traffic_Definition = new Player_Traffic_Definition();
        }
    }

    public class Player_Traffic_Definition
    {
        public int Time;
        public List<Player_Traffic_Item> Player_Traffic_List = new List<Player_Traffic_Item>();

        public Player_Traffic_Definition(STFReader stf)
        {
            DateTime baseDT = new DateTime();
            DateTime arrivalTime = new DateTime();
            DateTime departTime = new DateTime();
            int skipCount = 0;
            float distanceDownPath = new float();
            int platformStartID = 0;
            stf.MustMatch("(");
            Time = (int)stf.ReadFloat(STFReader.UNITS.Time, null);
            // Clumsy parsing. You only get a new Player_Traffic_Item in the list after a PlatformStartId is met.
            // Blame lies with Microsoft for poor design of syntax.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("arrivaltime", ()=>{ arrivalTime = baseDT.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.Time, null)); }),
                new STFReader.TokenProcessor("departtime", ()=>{ departTime = baseDT.AddSeconds(stf.ReadFloatBlock(STFReader.UNITS.Time, null)); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ skipCount = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ distanceDownPath = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ platformStartID = stf.ReadIntBlock(null);
                    Player_Traffic_List.Add(new Player_Traffic_Item(arrivalTime, departTime, skipCount, distanceDownPath, platformStartID)); }),
            });
        }

        // Used for explore in activity mode
        public Player_Traffic_Definition()
        {
        }
    }

    public class Player_Traffic_Item
    {
        public DateTime ArrivalTime;
        public DateTime DepartTime;
        public float DistanceDownPath;
        public int PlatformStartID;

        public Player_Traffic_Item(DateTime arrivalTime, DateTime departTime, int skipCount, float distanceDownPath, int platformStartID)
        {
            ArrivalTime = arrivalTime;
            DepartTime = departTime;
            DistanceDownPath = distanceDownPath;
            PlatformStartID = platformStartID;
        }
    }

    public class Service_Definition
    {
        public string Name;
        public int Time;
        public int UiD;
        public List<Service_Item> ServiceList = new List<Service_Item>();
        float efficiency;
        int skipCount;
        float distanceDownPath = new float();
        int platformStartID;

        public Service_Definition(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            Time = (int)stf.ReadFloat(STFReader.UNITS.Time, null);
            stf.MustMatch("uid");
            UiD = stf.ReadIntBlock(null);
            // Clumsy parsing. You only get a new Service_Item in the list after a PlatformStartId is met.
            // Blame lies with Microsoft for poor design of syntax.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("efficiency", ()=>{ efficiency = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ skipCount = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ distanceDownPath = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ platformStartID = stf.ReadIntBlock(null);
                    ServiceList.Add(new Service_Item(efficiency, skipCount, distanceDownPath, platformStartID)); }),
            });
        }

        // This is used to convert the player traffic definition into an AI train service definition for autopilot mode
        public Service_Definition(string service_Definition, Player_Traffic_Definition player_Traffic_Definition)
        {
            Name = service_Definition;
            Time = player_Traffic_Definition.Time;
            UiD = 0;
            foreach (Player_Traffic_Item player_Traffic_Item in player_Traffic_Definition.Player_Traffic_List)
            {
                efficiency = 0.95f; // Not present in player traffic definition
                distanceDownPath = player_Traffic_Item.DistanceDownPath;
                platformStartID = player_Traffic_Item.PlatformStartID;
                skipCount = 0;
                ServiceList.Add(new Service_Item(efficiency, skipCount, distanceDownPath, platformStartID));
            }
        }

        //================================================================================================//
        /// <summary>
        /// For restore
        /// <\summary>
        /// 

        public Service_Definition()
        { }

        //================================================================================================//
        /// <summary>
        /// Save of useful Service Items parameters
        /// <\summary>
        /// 

        public void Save(BinaryWriter outf)
        {
            if (ServiceList == null || ServiceList.Count == 0)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(ServiceList.Count);
                foreach (Service_Item thisServiceItem in ServiceList)
                {
                    outf.Write(thisServiceItem.Efficiency);
                    outf.Write(thisServiceItem.PlatformStartID);
                }
            }
        }
    }

    public class Service_Item
    {
        public float Efficiency = new float();
        public int SkipCount;
        public float DistanceDownPath = new float();
        public int PlatformStartID;

        public Service_Item(float efficiency, int skipCount, float distanceDownPath, int platformStartID)
        {
            Efficiency = efficiency;
            SkipCount = skipCount;
            DistanceDownPath = distanceDownPath;
            PlatformStartID = platformStartID;
        }
    }

    /// <summary>
    /// Parses Service_Definition objects and saves them in ServiceDefinitionList.
    /// </summary>
    public class Traffic_Definition
    {
        public string Name;
        public TrafficFile TrafficFile;
        public List<Service_Definition> ServiceDefinitionList = new List<Service_Definition>();

        public Traffic_Definition(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("service_definition", ()=>{ ServiceDefinitionList.Add(new Service_Definition(stf)); }),
            });

            TrafficFile = new TrafficFile(Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(stf.FileName)), "Traffic"), Name + ".trf"));

        }
    }

    /// <summary>
    /// Parses Event objects and saves them in EventList.
    /// </summary>
    public class Events
    {
        public List<Event> EventList = new List<Event>();

        public Events(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventcategorylocation", ()=>{ EventList.Add(new EventCategoryLocation(stf)); }),
                new STFReader.TokenProcessor("eventcategoryaction", ()=>{ EventList.Add(new EventCategoryAction(stf)); }),
                new STFReader.TokenProcessor("eventcategorytime", ()=>{ EventList.Add(new EventCategoryTime(stf)); }),
            });
        }

        public void InsertORSpecificData(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventcategorylocation", ()=>{ TryModify(0, stf); }),
                new STFReader.TokenProcessor("eventcategoryaction", ()=>{ TryModify(1, stf); }),
                new STFReader.TokenProcessor("eventcategorytime", ()=>{ TryModify(2, stf); }),
            });
        }

        public void TryModify(int Category, STFReader stf)
        {
            Event origEvent;
            bool wrongEventID = false;
            int modifiedID = -1;
            try
            {
                stf.MustMatch("(");
                stf.MustMatch("id");
                stf.MustMatch("(");
                modifiedID = stf.ReadInt(null);
                stf.MustMatch(")");
                origEvent = EventList.Find(x => x.ID == modifiedID);
                if (origEvent == null)
                {
                    wrongEventID = true;
                    Trace.TraceWarning("Skipped event {0} not present in base activity file", modifiedID);
                    stf.SkipRestOfBlock();
                }
                else
                {
                    wrongEventID = !TestMatch(Category, origEvent);
                    if (!wrongEventID)
                    {
                        origEvent.AddOrModifyEvent(stf, Path.GetDirectoryName(stf.FileName));
                    }
                    else
                    {
                        Trace.TraceWarning("Skipped event {0} of event category not matching with base activity file", modifiedID);
                        stf.SkipRestOfBlock();
                    }
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException("Error in additional activity file", error));
            }
        }

        private bool TestMatch(int category, Event origEvent)
        {
            if (category == 0 && origEvent is EventCategoryLocation) return true;
            if (category == 1 && origEvent is EventCategoryAction) return true;
            if (category == 2 && origEvent is EventCategoryTime) return true;
            return false;
        }
    }

    public enum ORTSActSoundFileTypes
    {
        None,
        Everywhere,
        Cab,
        Pass,
        Ground,
        Location
    }

    /// <summary>
    /// The 3 types of event are inherited from the abstract Event class.
    /// </summary>
    public abstract class Event
    {
        public int ID;
        public string Name;
        public int Activation_Level;
        public Outcomes Outcomes;
        public string TextToDisplayOnCompletionIfTriggered = "";
        public string TextToDisplayOnCompletionIfNotTriggered = "";
        public Boolean Reversible;
        public int ORTSContinue = -1;
        public string ORTSActSoundFile;
        public ORTSActSoundFileTypes ORTSActSoundFileType;
        public ORTSWeatherChange ORTSWeatherChange;
        public string TrainService = "";
        public int TrainStartingTime = -1;

        public virtual void AddOrModifyEvent(STFReader stf, string fileName)
        { }
    }

    public class EventCategoryLocation : Event
    {
        public bool TriggerOnStop;  // Value assumed if property not found.
        public int TileX;
        public int TileZ;
        public float X;
        public float Z;
        public float RadiusM;

        public EventCategoryLocation(STFReader stf)
        {
            stf.MustMatch("(");
            AddOrModifyEvent(stf, stf.FileName);
        }

        public override void AddOrModifyEvent(STFReader stf, string fileName)
        {
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypelocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("ortstriggeringtrain", ()=>{ ParseTrain(stf); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("outcomes", ()=>
                {
                    if (Outcomes == null)
                        Outcomes = new Outcomes(stf, fileName);
                    else
                        Outcomes.CreateOrModifyOutcomes(stf, fileName); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnottriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("triggeronstop", ()=>{ TriggerOnStop = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("location", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(null);
                    TileZ = stf.ReadInt(null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    RadiusM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("ortscontinue", ()=>{ ORTSContinue = stf.ReadIntBlock(0); }),
                new STFReader.TokenProcessor("ortsactsoundfile", ()=>
                {
                    stf.MustMatch("(");
                    var tempString = stf.ReadString();
                    ORTSActSoundFile =Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)), "SOUND"), tempString);
                    try
                    {
                    ORTSActSoundFileType = (ORTSActSoundFileTypes)Enum.Parse(typeof(ORTSActSoundFileTypes), stf.ReadString());
                    }
                    catch(ArgumentException)
                    {
                        stf.StepBackOneItem();
                        STFException.TraceInformation(stf, "Skipped unknown activity sound file type " + stf.ReadString());
                        ORTSActSoundFileType = ORTSActSoundFileTypes.None;
                    }
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("ortsweatherchange", ()=>{ ORTSWeatherChange = new ORTSWeatherChange(stf);}),
            });
        }

        protected void ParseTrain(STFReader stf)
        {
            stf.MustMatch("(");
            TrainService = stf.ReadString();
            TrainStartingTime = stf.ReadInt(-1);
            stf.SkipRestOfBlock();
        }
    }

    /// <summary>
    /// Parses all types of action events.
    /// Save type of action event in Type. MSTS syntax isn't fully hierarchical, so using inheritance here instead of Type would be awkward. 
    /// </summary>
    public class EventCategoryAction : Event
    {
        public EventType Type;
        public WagonList WagonList;
        public Nullable<uint> SidingId;  // May be specified inside the Wagon_List instead. Nullable as can't use -1 to indicate not set.
        public float SpeedMpS;
        //private const float MilespHourToMeterpSecond = 0.44704f;

        public EventCategoryAction(STFReader stf)
        {
            stf.MustMatch("(");
            AddOrModifyEvent(stf, stf.FileName);
        }

        public override void AddOrModifyEvent(STFReader stf, string fileName)
        {
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("eventtypeallstops", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AllStops; }),
                new STFReader.TokenProcessor("eventtypeassembletrain", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AssembleTrain; }),
                new STFReader.TokenProcessor("eventtypeassembletrainatlocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.AssembleTrainAtLocation; }),
                new STFReader.TokenProcessor("eventtypedropoffwagonsatlocation", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.DropOffWagonsAtLocation; }),
                new STFReader.TokenProcessor("eventtypepickuppassengers", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.PickUpPassengers; }),
                new STFReader.TokenProcessor("eventtypepickupwagons", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.PickUpWagons; }),
                new STFReader.TokenProcessor("eventtypereachspeed", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Type = EventType.ReachSpeed; }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("outcomes", ()=>
                {
                    if (Outcomes == null)
                        Outcomes = new Outcomes(stf, fileName);
                    else
                        Outcomes.CreateOrModifyOutcomes(stf, fileName); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnottriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("wagon_list", ()=>{ WagonList = new WagonList(stf, Type); }),
                new STFReader.TokenProcessor("sidingitem", ()=>{ SidingId = (uint)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("speed", ()=>{ SpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                new STFReader.TokenProcessor("reversable_event", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Reversible = true; }),
                // Also support the correct spelling !
                new STFReader.TokenProcessor("reversible_event", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Reversible = true; }),
                new STFReader.TokenProcessor("ortscontinue", ()=>{ ORTSContinue = stf.ReadIntBlock(0); }),
                new STFReader.TokenProcessor("ortsactsoundfile", ()=>
                {
                    stf.MustMatch("(");
                    var tempString = stf.ReadString();
                    ORTSActSoundFile =Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)), "SOUND"), tempString);
                    try
                    {
                    ORTSActSoundFileType = (ORTSActSoundFileTypes)Enum.Parse(typeof(ORTSActSoundFileTypes), stf.ReadString());
                    }
                    catch(ArgumentException)
                    {
                        stf.StepBackOneItem();
                        STFException.TraceInformation(stf, "Skipped unknown activity sound file type " + stf.ReadString());
                        ORTSActSoundFileType = ORTSActSoundFileTypes.None;
                    }
                    stf.MustMatch(")");
                }),
            });
        }
    }


    public class WagonList
    {
        public List<WorkOrderWagon> WorkOrderWagonList = new List<WorkOrderWagon>();
        Nullable<uint> uID;        // Nullable as can't use -1 to indicate not set.  
        Nullable<uint> sidingId;   // May be specified outside the Wagon_List instead.
        string description = "";   // Value assumed if property not found.

        public WagonList(STFReader stf, EventType eventType)
        {
            stf.MustMatch("(");
            // "Drop Off" Wagon_List sometimes lacks a Description attribute, so we create the wagon _before_ description
            // is parsed. Bad practice, but not very dangerous as each Description usually repeats the same data.
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ uID = stf.ReadUIntBlock(null); }),
                new STFReader.TokenProcessor("sidingitem", ()=>{ sidingId = stf.ReadUIntBlock(null);
                    WorkOrderWagonList.Add(new WorkOrderWagon(uID.Value, sidingId.Value, description));}),
                new STFReader.TokenProcessor("description", ()=>{ description = stf.ReadStringBlock(""); }),
            });
        }
    }

    /// <summary>
    /// Parses a wagon from the WagonList.
    /// Do not confuse with older class Wagon below, which parses TrainCfg from the *.con file.
    /// </summary>
    public class WorkOrderWagon
    {
        public Nullable<uint> UID;        // Nullable as can't use -1 to indicate not set.  
        public Nullable<uint> SidingId;   // May be specified outside the Wagon_List.
        public string Description = "";   // Value assumed if property not found.

        public WorkOrderWagon(uint uId, uint sidingId, string description)
        {
            UID = uId;
            SidingId = sidingId;
            Description = description;
        }
    }

    public class EventCategoryTime : Event
    {  // E.g. Hisatsu route and Short Passenger Run shrtpass.act
        public int Time;

        public EventCategoryTime(STFReader stf)
        {
            stf.MustMatch("(");
            AddOrModifyEvent(stf, stf.FileName);
        }

        public override void AddOrModifyEvent(STFReader stf, string fileName)
        {
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("activation_level", ()=>{ Activation_Level = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("outcomes", ()=>
                {
                    if (Outcomes == null)
                        Outcomes = new Outcomes(stf, fileName);
                    else
                        Outcomes.CreateOrModifyOutcomes(stf, fileName); }),
                new STFReader.TokenProcessor("texttodisplayoncompletioniftriggered", ()=>{ TextToDisplayOnCompletionIfTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("texttodisplayoncompletionifnottriggered", ()=>{ TextToDisplayOnCompletionIfNotTriggered = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("time", ()=>{ Time = (int)stf.ReadFloatBlock(STFReader.UNITS.Time, null); }),
                new STFReader.TokenProcessor("ortscontinue", ()=>{ ORTSContinue = stf.ReadIntBlock(0); }),
                new STFReader.TokenProcessor("ortsactsoundfile", ()=>
                {
                    stf.MustMatch("(");
                    var tempString = stf.ReadString();
                    ORTSActSoundFile = Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)), "SOUND"), tempString);
                    try
                    {
                    ORTSActSoundFileType = (ORTSActSoundFileTypes)Enum.Parse(typeof(ORTSActSoundFileTypes), stf.ReadString());
                    }
                    catch(ArgumentException)
                    {
                        stf.StepBackOneItem();
                        STFException.TraceInformation(stf, "Skipped unknown activity sound file type " + stf.ReadString());
                        ORTSActSoundFileType = ORTSActSoundFileTypes.None;
                    }
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("ortsweatherchange", ()=>{ ORTSWeatherChange = new ORTSWeatherChange(stf);}),
            });
        }
    }

    public class Outcomes
    {
        public bool ActivitySuccess;
        public string ActivityFail;
        // MSTS Activity Editor limits model to 4 outcomes of any type. We use lists so there is no restriction.
        public List<int> ActivateList = new List<int>();
        public List<int> RestoreActLevelList = new List<int>();
        public List<int> DecActLevelList = new List<int>();
        public List<int> IncActLevelList = new List<int>();
        public string DisplayMessage;
        //       public string WaitingTrainToRestart;
        public RestartWaitingTrain RestartWaitingTrain;
        public ORTSWeatherChange ORTSWeatherChange;
        public ActivitySound ActivitySound;

        public Outcomes(STFReader stf, string fileName)
        {
            CreateOrModifyOutcomes(stf, fileName);
        }

        public void CreateOrModifyOutcomes(STFReader stf, string fileName)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activitysuccess", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); ActivitySuccess = true; }),
                new STFReader.TokenProcessor("activityfail", ()=>{ ActivityFail = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("activateevent", ()=>{ ActivateList.Add(stf.ReadIntBlock(null)); }),
                new STFReader.TokenProcessor("restoreactlevel", ()=>{ RestoreActLevelList.Add(stf.ReadIntBlock(null)); }),
                new STFReader.TokenProcessor("decactlevel", ()=>{ DecActLevelList.Add(stf.ReadIntBlock(null)); }),
                new STFReader.TokenProcessor("incactlevel", ()=>{ IncActLevelList.Add(stf.ReadIntBlock(null)); }),
                new STFReader.TokenProcessor("displaymessage", ()=>{
                    DisplayMessage = stf.ReadStringBlock(""); }),
 //               new STFReader.TokenProcessor("ortswaitingtraintorestart", ()=>{ WaitingTrainToRestart = stf.ReadStringBlock(""); }),
                new STFReader.TokenProcessor("ortsrestartwaitingtrain", ()=>{ RestartWaitingTrain = new RestartWaitingTrain(stf); }),
                new STFReader.TokenProcessor("ortsweatherchange", ()=>{ ORTSWeatherChange = new ORTSWeatherChange(stf);}),
                new STFReader.TokenProcessor("ortsactivitysound", ()=>{ ActivitySound = new ActivitySound(stf, fileName);}),
            });
        }
    }

    public class RestartWaitingTrain
    {
        public string WaitingTrainToRestart = "";
        public int WaitingTrainStartingTime = -1;
        public int DelayToRestart;
        public int MatchingWPDelay;

        public RestartWaitingTrain(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortswaitingtraintorestart", ()=>{ ParseTrain(stf); }),
                new STFReader.TokenProcessor("ortsdelaytorestart", ()=>{ DelayToRestart = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("ortsmatchingwpdelay", ()=>{ MatchingWPDelay = stf.ReadIntBlock(null); }),
            });
        }

        protected void ParseTrain(STFReader stf)
        {
            stf.MustMatch("(");
            WaitingTrainToRestart = stf.ReadString();
            WaitingTrainStartingTime = stf.ReadInt(-1);
            stf.SkipRestOfBlock();
        }

    }

    public class ORTSWeatherChange
    {
        public float ORTSOvercast = -1;
        public int ORTSOvercastTransitionTimeS = -1;
        public float ORTSFog = -1;
        public int ORTSFogTransitionTimeS = -1;
        public float ORTSPrecipitationIntensity = -1;
        public int ORTSPrecipitationIntensityTransitionTimeS = -1;
        public float ORTSPrecipitationLiquidity = -1;
        public int ORTSPrecipitationLiquidityTransitionTimeS = -1;

        public ORTSWeatherChange(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsovercast", ()=>
                {
                    stf.MustMatch("(");
                    ORTSOvercast = stf.ReadFloat(0, -1);
                    ORTSOvercastTransitionTimeS = stf.ReadInt(-1);
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("ortsfog", ()=>
                {
                    stf.MustMatch("(");
                    ORTSFog = stf.ReadFloat(0, -1);
                    ORTSFogTransitionTimeS = stf.ReadInt(-1);
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("ortsprecipitationintensity", ()=>
                {
                    stf.MustMatch("(");
                    ORTSPrecipitationIntensity = stf.ReadFloat(0, -1);
                    ORTSPrecipitationIntensityTransitionTimeS = stf.ReadInt(-1);
                    stf.MustMatch(")");
                }),
                               new STFReader.TokenProcessor("ortsprecipitationliquidity", ()=>
                {
                    stf.MustMatch("(");
                    ORTSPrecipitationLiquidity = stf.ReadFloat(0, -1);
                    ORTSPrecipitationLiquidityTransitionTimeS = stf.ReadInt(-1);
                    stf.MustMatch(")");
                })
            });
        }
    }

    public class ActivitySound
    {
        public string ORTSActSoundFile;
        public ORTSActSoundFileTypes ORTSActSoundFileType;
        public int TileX;
        public int TileZ;
        public float X;
        public float Y;
        public float Z;
        public ActivitySound(STFReader stf, string fileName)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsactsoundfile", ()=>
                {
                    stf.MustMatch("(");
                    var tempString = stf.ReadString();
                    ORTSActSoundFile =Path.Combine(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)), "SOUND"), tempString);
                    try
                    {
                    ORTSActSoundFileType = (ORTSActSoundFileTypes)Enum.Parse(typeof(ORTSActSoundFileTypes), stf.ReadString());
                    }
                    catch(ArgumentException)
                    {
                        stf.StepBackOneItem();
                        STFException.TraceInformation(stf, "Skipped unknown activity sound file type " + stf.ReadString());
                        ORTSActSoundFileType = ORTSActSoundFileTypes.None;
                    }
                    stf.MustMatch(")");
                }),
            new STFReader.TokenProcessor("ortssoundlocation", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(null);
                    TileZ = stf.ReadInt(null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.MustMatch(")");
                }),
            });
        }
    }


    /// <summary>
    /// Parses ActivityObject objects and saves them in ActivityObjectList.
    /// </summary>
    public class ActivityObjects
    {
        public List<ActivityObject> ActivityObjectList = new List<ActivityObject>();

        //public new ActivityObject this[int i]
        //{
        //    get { return (ActivityObject)base[i]; }
        //    set { base[i] = value; }
        //}

        public ActivityObjects(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityobject", ()=>{ ActivityObjectList.Add(new ActivityObject(stf)); }),
            });
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
                new STFReader.TokenProcessor("objecttype", ()=>{ stf.MustMatch("("); stf.MustMatch("WagonsList"); stf.MustMatch(")"); }),
                new STFReader.TokenProcessor("train_config", ()=>{ Train_Config = new Train_Config(stf); }),
                new STFReader.TokenProcessor("direction", ()=>{ Direction = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("id", ()=>{ ID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("tile", ()=>{
                    stf.MustMatch("(");
                    TileX = stf.ReadInt(null);
                    TileZ = stf.ReadInt(null);
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.MustMatch(")");
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
        public float A;
        public float B = 0.001f;

        public MaxVelocity(STFReader stf)
        {
            stf.MustMatch("(");
            A = stf.ReadFloat(STFReader.UNITS.Speed, null);
            B = stf.ReadFloat(STFReader.UNITS.Speed, null);
            stf.MustMatch(")");
        }
    }

    public class TrainCfg
    {
        public string Name = "Loose consist.";
        int Serial = 1;
        public MaxVelocity MaxVelocity;
        int NextWagonUID;
        public float Durability = 1.0f;   // Value assumed if attribute not found.
        public string TcsParametersFileName = string.Empty;

        public List<Wagon> WagonList = new List<Wagon>();

        public TrainCfg(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("maxvelocity", ()=>{ MaxVelocity = new MaxVelocity(stf); }),
                new STFReader.TokenProcessor("nextwagonuid", ()=>{ NextWagonUID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("durability", ()=>{ Durability = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagon", ()=>{ WagonList.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("engine", ()=>{ WagonList.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("ortseot", ()=>{ WagonList.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("ortstraincontrolsystemparameters", () => TcsParametersFileName = stf.ReadStringBlock(null)),
            });
        }
    }

    public class Wagon
    {
        public string Folder;
        public string Name;
        public int UiD;
        public bool IsEngine;
        public bool IsEOT;
        public bool Flip;
        public List<LoadData> LoadDataList;

        public Wagon(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("flip", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Flip = true; }),
                new STFReader.TokenProcessor("enginedata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); IsEngine = true; }),
                new STFReader.TokenProcessor("wagondata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); }),
                new STFReader.TokenProcessor("eotdata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); IsEOT = true;  }),
                new STFReader.TokenProcessor("loaddata", ()=>
                {
                    stf.MustMatch("(");
                    if (LoadDataList == null) LoadDataList = new List<LoadData>();
                    LoadData loadData = new LoadData();
                    loadData.Name = stf.ReadString();
                    loadData.Folder = stf.ReadString();
                    var positionString = stf.ReadString();
                    Enum.TryParse(positionString, out loadData.LoadPosition);
                    var state = stf.ReadString();
                    if (state != ")")
                    {
                        Enum.TryParse(state, out loadData.LoadState);
                        LoadDataList.Add(loadData);
                        stf.MustMatch(")");
                    }
                    else
                        LoadDataList.Add(loadData);
                }),
            });
        }

        public string GetName(uint uId, List<Wagon> wagonList)
        {
            foreach (var item in wagonList)
            {
                var wagon = item as Wagon;
                if (wagon.UiD == uId)
                {
                    return wagon.Name;
                }
            }
            return "<unknown name>";
        }
    }

    public class PlatformNumPassengersWaiting
    {  // For use, see file EUROPE1\ACTIVITIES\aftstorm.act
        public List<PlatformData> PlatformDataList = new List<PlatformData>();

        public PlatformNumPassengersWaiting(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("platformdata", ()=>{ PlatformDataList.Add(new PlatformData(stf)); }),
            });
        }
    }

    public class PlatformData
    { // e.g. "PlatformData ( 41 20 )" 
        public int Id;
        public int PassengerCount;

        public PlatformData(int id, int passengerCount)
        {
            Id = id;
            PassengerCount = passengerCount;
        }

        public PlatformData(STFReader stf)
        {
            stf.MustMatch("(");
            Id = stf.ReadInt(null);
            PassengerCount = stf.ReadInt(null);
            stf.MustMatch(")");
        }
    }

    public class ActivityFailedSignals
    { // e.g. ActivityFailedSignals ( ActivityFailedSignal ( 50 ) )
        public List<int> FailedSignalList = new List<int>();
        public ActivityFailedSignals(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityfailedsignal", ()=>{ FailedSignalList.Add(stf.ReadIntBlock(null)); }),
            });
        }
    }

    public class ActivityRestrictedSpeedZones
    {  // For use, see file EUROPE1\ACTIVITIES\aftstorm.act
        public List<ActivityRestrictedSpeedZone> ActivityRestrictedSpeedZoneList = new List<ActivityRestrictedSpeedZone>();

        public ActivityRestrictedSpeedZones(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activityrestrictedspeedzone", ()=>{ ActivityRestrictedSpeedZoneList.Add(new ActivityRestrictedSpeedZone(stf)); }),
            });
        }
    }

    public class ActivityRestrictedSpeedZone
    {
        public Position StartPosition;
        public Position EndPosition;

        public ActivityRestrictedSpeedZone(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("startposition", ()=>{ StartPosition = new Position(stf); }),
                new STFReader.TokenProcessor("endposition", ()=>{ EndPosition = new Position(stf); }),
            });
        }
    }

    public class Position
    {
        public int TileX;
        public int TileZ;
        public float X;
        public float Z;
        public float Y;

        public Position(int tileX, int tileZ, int x, int z)
        {
            TileX = tileX;
            TileZ = tileZ;
            X = x;
            Z = z;
        }

        public Position(STFReader stf)
        {
            stf.MustMatch("(");
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.MustMatch(")");
        }
    }
}
