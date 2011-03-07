/// <summary>
/// This contains all the essential code to operate trains along paths as defined
/// in the activity.   It is meant to operate in a separate thread it handles the
/// following:
///    track paths
///    switch track positions
///    signal indications
///    calculating positions and velocities of trains
///    
/// Update is called regularly to
///     do physics calculations for train movement
///     compute new signal indications
///     operate ai trains
///     
/// All keyboard input comes from the viewer class as calls on simulator's methods.
///     
/// 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// </summary>
/// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Interlocking;


namespace ORTS
{
	public class Simulator
	{
		public bool Paused = true;          // start off paused, set to true once the viewer is fully loaded and initialized
		public float GameSpeed = 1;
		/// <summary>
		/// Monotonically increasing time value (in seconds) for the simulation. Starts at 0 and only ever increases, at <see cref="GameSpeed"/>.
		/// Does not change if game is <see cref="Paused"/>.
		/// </summary>
		public double GameTime = 0;
		/// <summary>
		/// "Time of day" clock value (in seconds) for the simulation. Starts at activity start time and may increase, at <see cref="GameSpeed"/>,
		/// or jump forwards or jump backwards.
		/// </summary>
		public double ClockTime = 0;
		// while Simulator.Update() is running, objects are adjusted to this target time 
		// after Simulator.Update() is complete, the simulator state matches this time

		public readonly UserSettings Settings;

		public string BasePath;     // ie c:\program files\microsoft games\train simulator
		public string RoutePath;    // ie c:\program files\microsoft games\train simulator\routes\usa1  - may be different on different pc's

		// Primary Simulator Data 
		// These items represent the current state of the simulator 
		// In multiplayer games, these items must be kept in sync across all players
		// These items are what are saved and loaded in a game save.
		public string RouteName;    // ie LPS, USA1  represents the folder name
		public ACTFile Activity;
		public Activity ActivityRun;
		public TDBFile TDB;
		public TRKFile TRK;
		public TRPFile TRP; // Track profile file
		public TSectionDatFile TSectionDat;
		public List<Train> Trains;
		public Signals Signals;
		public AI AI;
		public RailDriverHandler RailDriver;
		public SeasonType Season;
		public WeatherType Weather;
		SIGCFGFile SIGCFG;
		public string ExplorePathFile;
		public string ExploreConFile;
		public LevelCrossings LevelCrossings;
		public RDBFile RDB;
		public CarSpawnerFile CarSpawnerFile;

		/// <summary>
		/// Reference to the InterlockingSystem object, responsible for
		/// managing signalling and interlocking.
		/// </summary>
		public InterlockingSystem InterlockingSystem;


		public TrainCar PlayerLocomotive = null;  // Set by the Viewer - TODO there could be more than one player so eliminate this.

		public Simulator(UserSettings settings, string activityPath)
		{
			Settings = settings;
			RoutePath = Path.GetDirectoryName(Path.GetDirectoryName(activityPath));
			RouteName = Path.GetFileName(RoutePath);
			BasePath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));

			Trace.Write("Loading ");

			Trace.Write(" TRK");
			TRK = new TRKFile(MSTSPath.GetTRKFileName(RoutePath));

			//Establish default track profile
			Trace.Write(" TRP");
			if (Directory.Exists(RoutePath) && File.Exists(RoutePath + @"\TrProfile.xml"))
			{
				// XML-style
				TRP = new TRPFile(RoutePath + @"\TrProfile.xml");
			}
			else if (Directory.Exists(RoutePath) && File.Exists(RoutePath + @"\TrProfile.dat"))
			{
				// MSTS-style
				TRP = new TRPFile(RoutePath + @"\TrProfile.dat");
			}
			else
			{
				// default
				TRP = new TRPFile("");
			}
			// FOR DEBUGGING: Writes XML file from current TRP
			//TRP.TrackProfile.SaveAsXML(@"C:/Users/Walt/Desktop/TrProfile.xml");

			Trace.Write(" TDB");
			TDB = new TDBFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");

			Trace.Write(" SIGCFG");
			SIGCFG = new SIGCFGFile(RoutePath + @"\sigcfg.dat");

			Trace.Write(" DAT");
			if (Directory.Exists(RoutePath + @"\GLOBAL") && File.Exists(RoutePath + @"\GLOBAL\TSECTION.DAT"))
				TSectionDat = new TSectionDatFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
			else
				TSectionDat = new TSectionDatFile(BasePath + @"\GLOBAL\TSECTION.DAT");
			if (File.Exists(RoutePath + @"\TSECTION.DAT"))
				TSectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");

			RailDriver = new RailDriverHandler(BasePath);

			Trace.Write(" ACT");

            var rdbFile = RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".rdb";
            if (File.Exists(rdbFile))
            {
                Trace.Write(" RDB");
                RDB = new RDBFile(rdbFile);
            }

			Trace.Write(" CARSPAWN");
			CarSpawnerFile = new CarSpawnerFile(RoutePath + @"\carspawn.dat", RoutePath + @"\shapes\"); 

		}
		public void SetActivity(string activityPath)
		{
			Activity = new ACTFile(activityPath);
			ActivityRun = new Activity(Activity);
			if (ActivityRun.Current == null)
				ActivityRun = null;

			StartTime st = Activity.Tr_Activity.Tr_Activity_Header.StartTime;
			TimeSpan StartTime = new TimeSpan(st.Hour, st.Minute, st.Second);
			ClockTime = StartTime.TotalSeconds;
			Season = Activity.Tr_Activity.Tr_Activity_Header.Season;
			Weather = Activity.Tr_Activity.Tr_Activity_Header.Weather;
		}
		public void SetExplore(string path, string consist, string start, string season, string weather)
		{
			ExplorePathFile = path;
			ExploreConFile = consist;
			var time = start.Split(':');
			TimeSpan StartTime = new TimeSpan(int.Parse(time[0]), time.Length > 1 ? int.Parse(time[1]) : 0, time.Length > 2 ? int.Parse(time[2]) : 0);
			ClockTime = StartTime.TotalSeconds;
			Season = (SeasonType)int.Parse(season);
			Weather = (WeatherType)int.Parse(weather);
		}

		public void Start()
		{
            AlignSwitchesToDefault();
            Signals = new Signals(this, SIGCFG);
            InitializeTrains();
            LevelCrossings = new LevelCrossings(this);
            InterlockingSystem = new InterlockingSystem(this);
            AI = new AI(this);
		}

		public void Stop()
		{
			if (RailDriver != null)
				RailDriver.Shutdown();
		}

		public void Restore(BinaryReader inf)
		{
            ClockTime = inf.ReadDouble();
            Season = (SeasonType)inf.ReadInt32();
            Weather = (WeatherType)inf.ReadInt32();

            RestoreSwitchSettings(inf);
            Signals = new Signals(this, SIGCFG, inf);
            RestoreTrains(inf);
            LevelCrossings = new LevelCrossings(this);
            InterlockingSystem = new InterlockingSystem(this);
            AI = new AI(this, inf);

            ActivityRun = ORTS.Activity.Restore(inf);
		}

		public void Save(BinaryWriter outf)
		{
			outf.Write(ClockTime);
			outf.Write((int)Season);
			outf.Write((int)Weather);

            SaveSwitchSettings(outf);
			Signals.Save(outf);
            SaveTrains(outf);
            // LevelCrossings
            // InterlockingSystem
			AI.Save(outf);

            ORTS.Activity.Save(outf, ActivityRun);
		}

        void InitializeTrains()
        {
            Trains = new List<Train>();
            InitializePlayerTrain();
            InitializeStaticConsists();
        }

        /// <summary>
		/// Which locomotive does the activity specify for the player.
		/// </summary>
		public TrainCar InitialPlayerLocomotive()
		{
			Train playerTrain = Trains[0];    // we install the player train first
			TrainCar PlayerLocomotive = null;
			foreach (TrainCar car in playerTrain.Cars)
				if (car.IsDriveable)  // first loco is the one the player drives
				{
					PlayerLocomotive = car;
					playerTrain.LeadLocomotive = car;
					playerTrain.InitializeBrakes();
					break;
				}
			if (PlayerLocomotive == null)
				throw new InvalidDataException("Can't find player locomotive in activity");
			return PlayerLocomotive;
		}


		/// <summary>
		/// Convert and elapsed real time into clock time based on simulator
		/// running speed and paused state.
		/// </summary>
		public float GetElapsedClockSeconds(float elapsedRealSeconds)
		{
			return elapsedRealSeconds * (Paused ? 0 : GameSpeed);
		}

		/// <summary>
		/// Update the simulator state 
		/// elapsedClockSeconds represents the the time since the last call to Simulator.Update
		/// Executes in the UpdaterProcess thread.
		/// </summary>
		public void Update(float elapsedClockSeconds)
		{
			// Advance the times.
			GameTime += elapsedClockSeconds;
			ClockTime += elapsedClockSeconds;

			// Represent conditions at the specified clock time.
			List<Train> movingTrains = new List<Train>();

			if (PlayerLocomotive != null)
			{
				movingTrains.Add(PlayerLocomotive.Train);
			}

			foreach (Train train in Trains)
			{
				if (train.SpeedMpS != 0 &&
					train.GetType() != typeof(AITrain) &&
					(PlayerLocomotive == null || train != PlayerLocomotive.Train))
				{
					movingTrains.Add(train);
				}
			}

			foreach (Train train in movingTrains)
			{
				train.Update(elapsedClockSeconds);
				AlignTrailingPointSwitches(train, train.MUDirection == Direction.Forward);
			}

			foreach (Train train in movingTrains)
			{
				CheckForCoupling(train, elapsedClockSeconds);
			}

			if (Signals != null)
			{
				Signals.Update(elapsedClockSeconds);
			}

			if (AI != null)
			{
				AI.Update(elapsedClockSeconds);
			}

			if (ActivityRun != null)
			{
				ActivityRun.Update();
			}

			if (RailDriver != null)
			{
				RailDriver.Update(PlayerLocomotive);
			}

			InterlockingSystem.Update(elapsedClockSeconds);
		}

		private void FinishFrontCoupling(Train drivenTrain, Train train, TrainCar lead)
		{
			drivenTrain.LeadLocomotive = lead;
			drivenTrain.CalculatePositionOfCars(0);

			FinishCoupling(drivenTrain, train);

			drivenTrain.FirstCar.SignalEvent(EventID.Couple);
		}

		private void FinishRearCoupling(Train drivenTrain, Train train)
		{
			drivenTrain.RepositionRearTraveller();
			FinishCoupling(drivenTrain, train);
			drivenTrain.LastCar.SignalEvent(EventID.Couple);
		}

		private void FinishCoupling(Train drivenTrain, Train train)
		{
			Trains.Remove(train);

			if (train.UncoupledFrom != null)
				train.UncoupledFrom.UncoupledFrom = null;

			if (PlayerLocomotive != null && PlayerLocomotive.Train == train)
			{
				drivenTrain.AITrainThrottlePercent = train.AITrainThrottlePercent;
				drivenTrain.AITrainBrakePercent = train.AITrainBrakePercent;
				drivenTrain.LeadLocomotive = PlayerLocomotive;
			}
		}



		private void UpdateUncoupled(Train drivenTrain, Train train, float d1, float d2, bool rear)
		{
			if (train == drivenTrain.UncoupledFrom && d1 > .5 && d2 > .5)
			{
				TDBTraveller traveller = rear ? drivenTrain.RearTDBTraveller : drivenTrain.FrontTDBTraveller;
				float d3 = traveller.OverlapDistanceM(train.FrontTDBTraveller, rear);
				float d4 = traveller.OverlapDistanceM(train.RearTDBTraveller, rear);
				if (d3 > .5 && d4 > .5)
				{
					train.UncoupledFrom = null;
					drivenTrain.UncoupledFrom = null;
					//Console.WriteLine("release uncoupledfrom f {0} {1} {2} {3}",d1,d2,d3,d4);
				}
			}
		}

		/// <summary>
		/// Scan other trains
		/// </summary>
		public void CheckForCoupling(Train drivenTrain, float elapsedClockSeconds)
		{
			if (drivenTrain.SpeedMpS < 0)
			{
				foreach (Train train in Trains)
					if (train != drivenTrain)
					{
						float d1 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, true);
						if (d1 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
								//Console.WriteLine("contact rf {0} {1} {2}", d1, drivenTrain.SpeedMpS, train.SpeedMpS);
								if (drivenTrain.SpeedMpS < train.SpeedMpS)
									drivenTrain.SetCoupleSpeed(train, 1);
								drivenTrain.CalculatePositionOfCars(-d1);
								return;
							}
							// couple my rear to front of train
							//drivenTrain.SetCoupleSpeed(train, 1);
							foreach (TrainCar car in train.Cars)
							{
								drivenTrain.Cars.Add(car);
								car.Train = drivenTrain;
							}
							FinishRearCoupling(drivenTrain, train);
							//Console.WriteLine("couple rf {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
							return;
						}
						float d2 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, true);
						if (d2 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
								//Console.WriteLine("contact rr {0} {1} {2}", d2, drivenTrain.SpeedMpS, train.SpeedMpS);
								if (drivenTrain.SpeedMpS < -train.SpeedMpS)
									drivenTrain.SetCoupleSpeed(train, 11);
								drivenTrain.CalculatePositionOfCars(-d2);
								return;
							}
							// couple my rear to rear of train
							//drivenTrain.SetCoupleSpeed(train, -1);
							for (int i = train.Cars.Count - 1; i >= 0; --i)
							{
								TrainCar car = train.Cars[i];
								drivenTrain.Cars.Add(car);
								car.Train = drivenTrain;
								car.Flipped = !car.Flipped;
							}
							FinishRearCoupling(drivenTrain, train);
							//Console.WriteLine("couple rr {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
							return;
						}
						UpdateUncoupled(drivenTrain, train, d1, d2, false);
					}
			}
			else if (drivenTrain.SpeedMpS > 0)
			{
				foreach (Train train in Trains)
					if (train != drivenTrain)
					{
						float d1 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, false);
						if (d1 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
								//Console.WriteLine("contact fr {0} {1} {2} {3}", d1, drivenTrain.SpeedMpS, train.SpeedMpS);
								if (drivenTrain.SpeedMpS > train.SpeedMpS)
									drivenTrain.SetCoupleSpeed(train, 1);
								drivenTrain.CalculatePositionOfCars(d1);
								return;
							}
							// couple my front to rear of train
							//drivenTrain.SetCoupleSpeed(train, 1);
							TrainCar lead = drivenTrain.LeadLocomotive;
							for (int i = 0; i < train.Cars.Count; ++i)
							{
								TrainCar car = train.Cars[i];
								drivenTrain.Cars.Insert(i, car);
								car.Train = drivenTrain;
							}
							FinishFrontCoupling(drivenTrain, train, lead);
							//Console.WriteLine("couple fr {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
							return;
						}
						float d2 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, false);
						if (d2 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
								//Console.WriteLine("contact ff {0} {1} {2}", d2, drivenTrain.SpeedMpS, train.SpeedMpS);
								if (drivenTrain.SpeedMpS > -train.SpeedMpS)
									drivenTrain.SetCoupleSpeed(train, -1);
								drivenTrain.CalculatePositionOfCars(d2);
								return;
							}
							// couple my front to front of train
							//drivenTrain.SetCoupleSpeed(train, -1);
							TrainCar lead = drivenTrain.LeadLocomotive;
							for (int i = 0; i < train.Cars.Count; ++i)
							{
								TrainCar car = train.Cars[i];
								drivenTrain.Cars.Insert(0, car);
								car.Train = drivenTrain;
								car.Flipped = !car.Flipped;
							}
							FinishFrontCoupling(drivenTrain, train, lead);

							//Console.WriteLine("couple ff {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
							return;
						}

						UpdateUncoupled(drivenTrain, train, d1, d2, true);
					}
			}
		}

		/// <summary>
		/// Sets the trailing point switches ahead of the train
		/// </summary>
		public void AlignTrailingPointSwitches(Train train, bool forward)
		{
			// figure out which direction we are going
			TDBTraveller traveller;
			if (forward)
			{
				traveller = new TDBTraveller(train.FrontTDBTraveller);
			}
			else
			{
				traveller = new TDBTraveller(train.RearTDBTraveller);
				traveller.ReverseDirection();
			}

			// to save computing power, skip this if we haven't changed nodes or direction
			if (traveller.TN == lastAlignedAtTrackNode && forward == lastAlignedMovingForward) return;
			lastAlignedAtTrackNode = traveller.TN;
			lastAlignedMovingForward = forward;

			// find the next switch by scanning ahead for a TrJunctionNode
			while (traveller.TN.TrJunctionNode == null)
			{
				if (!traveller.NextSection())
					return;   // no more switches
			}
			TrJunctionNode nextSwitchTrack = traveller.TN.TrJunctionNode;
			if (SwitchIsOccupied(nextSwitchTrack))
				return;

			// if we are facing the points of the switch we don't do anything
			if (traveller.iEntryPIN == 0) return;

			// otherwise we are coming in on the trailing side of the switch
			// so line it up for the correct route
			nextSwitchTrack.SelectedRoute = traveller.iEntryPIN - 1;
		}

		TrackNode lastAlignedAtTrackNode = null;  // optimization skips trailing point
		bool lastAlignedMovingForward = false;    //    alignment if we haven't moved 


		/// <summary>
		/// The TSECTION.DAT specifies which path through a switch is considered the main route
		/// For most switches the main route is the straight-through route, vs taking the curved branch
		/// All the switch tracks in a route are stored in the TDB 
		/// This method scans the route's TDB, aligning each switch to the main route.
		/// </summary>
		private void AlignSwitchesToDefault()
		{
			foreach (TrackNode TN in TDB.TrackDB.TrackNodes) // for each run of track in the database
			{
				if (TN != null && TN.TrJunctionNode != null)  // if this is a switch track 
				{
					TrackShape TS = TSectionDat.TrackShapes.Get(TN.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route
					TN.TrJunctionNode.SelectedRoute = (int)TS.MainRoute;  // align the switch
				}
			}
		}

		private void SaveSwitchSettings(BinaryWriter outf)
		{
			foreach (TrackNode TN in TDB.TrackDB.TrackNodes) // for each run of track in the database
				if (TN != null && TN.TrJunctionNode != null)  // if this is a switch track 
					outf.Write(TN.TrJunctionNode.SelectedRoute);
		}

		private void RestoreSwitchSettings(BinaryReader inf)
		{
			foreach (TrackNode TN in TDB.TrackDB.TrackNodes) // for each run of track in the database
				if (TN != null && TN.TrJunctionNode != null)  // if this is a switch track 
					TN.TrJunctionNode.SelectedRoute = inf.ReadInt32();
		}

		/// <summary>
		/// Align the switchtrack behind the players train to the opposite position
		/// </summary>
		public void SwitchTrackBehind(Train train)
		{
            TrJunctionNode nextSwitchTrack = train.LeadLocomotive.Flipped ? train.FrontTDBTraveller.TrJunctionNodeAhead() : train.RearTDBTraveller.TrJunctionNodeBehind();
			if (SwitchIsOccupied(nextSwitchTrack))
				return;

			if (nextSwitchTrack != null)
			{
				if (nextSwitchTrack.SelectedRoute == 0)
					nextSwitchTrack.SelectedRoute = 1;
				else
					nextSwitchTrack.SelectedRoute = 0;
			}
		}

		/// <summary>
		/// Align the switchtrack ahead of the players train to the opposite position
		/// </summary>
		public void SwitchTrackAhead(Train train)
		{
            TrJunctionNode nextSwitchTrack = train.LeadLocomotive.Flipped ? train.RearTDBTraveller.TrJunctionNodeBehind() : train.FrontTDBTraveller.TrJunctionNodeAhead();
            if (SwitchIsOccupied(nextSwitchTrack))
				return;

			if (nextSwitchTrack != null)
			{
				if (nextSwitchTrack.SelectedRoute == 0)
					nextSwitchTrack.SelectedRoute = 1;
				else
					nextSwitchTrack.SelectedRoute = 0;
			}
			train.ResetSignal(false);
		}

		public bool SwitchIsOccupied(int junctionIndex)
		{
			if (junctionIndex < 0 || TDB.TrackDB.TrackNodes[junctionIndex] == null)
				return false;
			return SwitchIsOccupied(TDB.TrackDB.TrackNodes[junctionIndex].TrJunctionNode);
		}

		public bool SwitchIsOccupied(TrJunctionNode junctionNode)
		{
			foreach (Train train in Trains)
			{
				if (train.FrontTDBTraveller.TrackNodeIndex == train.RearTDBTraveller.TrackNodeIndex)
					continue;
				TDBTraveller traveller = new TDBTraveller(train.RearTDBTraveller);
				while (traveller.NextSection())
				{
					if (traveller.TrackNodeIndex == train.FrontTDBTraveller.TrackNodeIndex)
						break;
					if (traveller.TN.TrJunctionNode == junctionNode)
						return true;
				}
			}
			return false;
		}

		private void InitializePlayerTrain()
		{
            Debug.Assert(Trains != null, "Cannot InitializePlayerTrain() without Simulator.Trains.");
           
            // set up the player locomotive
			// first extract the player service definition from the activity file
			// this gives the consist and path
			string patFileName;
			string conFileName;
			if (Activity == null)
			{
				patFileName = ExplorePathFile;
				conFileName = ExploreConFile;
			}
			else
			{
				string playerServiceFileName = Activity.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name;
				SRVFile srvFile = new SRVFile(RoutePath + @"\SERVICES\" + playerServiceFileName + ".SRV");
				conFileName = BasePath + @"\TRAINS\CONSISTS\" + srvFile.Train_Config + ".CON";
				patFileName = RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";
			}

			Train train = new Train(this);

			// This is the position of the back end of the train in the database.
			PATTraveller patTraveller = new PATTraveller(patFileName);
			train.RearTDBTraveller = new TDBTraveller(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Z, 0, TDB, TSectionDat);

			// figure out if the next waypoint is forward or back
			patTraveller.NextWaypoint();
			if (train.RearTDBTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z) < 0)
				train.RearTDBTraveller.ReverseDirection();
			PATFile patFile = new PATFile(patFileName);
			AIPath aiPath = new AIPath(patFile, TDB, TSectionDat, patFileName);
			aiPath.AlignAllSwitches();
			CONFile conFile = new CONFile(conFileName);

			// add wagons
			TrainCar previousCar = null;
			foreach (Wagon wagon in conFile.Train.TrainCfg.Wagons)
			{

				string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
				string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
				if (wagon.IsEngine)
					wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

				try
				{
					TrainCar car = RollingStock.Load(this, wagonFilePath, previousCar);
					car.Flipped = wagon.Flip;
					car.UiD = wagon.UiD;
					train.Cars.Add(car);
					car.Train = train;
					previousCar = car;
				}
				catch (Exception error)
				{
					Trace.TraceInformation(wagonFilePath);
					Trace.WriteLine(error);
				}

			}// for each rail car

			if (train.Cars.Count == 0) return;

			train.CalculatePositionOfCars(0);

			Trains.Add(train);
			train.AITrainBrakePercent = 100;
            train.InitializeSignals();
		}


		/// <summary>
		/// Set up trains based on info in the static consists listed in the activity file.
		/// </summary>
		private void InitializeStaticConsists()
		{
			if (Activity == null)
				return;
			// for each static consist
			foreach (ActivityObject activityObject in Activity.Tr_Activity.Tr_Activity_File.ActivityObjects)
			{
				try
				{
					// construct train data
					Train train = new Train(this);
					int consistDirection;
					switch (activityObject.Direction)  // TODO, we don't really understand this
					{
						case 0: consistDirection = 0; break;  // reversed ( confirmed on L&PS route )
						case 18: consistDirection = 1; break;  // forward ( confirmed on ON route )
						case 131: consistDirection = 1; break; // forward ( confirmed on L&PS route )
						default: consistDirection = 1; break;  // forward ( confirmed on L&PS route )
					}
					train.RearTDBTraveller = new TDBTraveller(activityObject.TileX, activityObject.TileZ, activityObject.X, activityObject.Z, 1, TDB, TSectionDat);
					if (consistDirection != 1)
						train.RearTDBTraveller.ReverseDirection();
					// add wagons in reverse order - ie first wagon is at back of train
					// static consists are listed back to front in the activities, so we have to reverse the order, and flip the cars
					// when we add them to ORTS
					TrainCar previousCar = null;
					for (int iWagon = activityObject.Train_Config.TrainCfg.Wagons.Count - 1; iWagon >= 0; --iWagon)
					{
						Wagon wagon = (Wagon)activityObject.Train_Config.TrainCfg.Wagons[iWagon];
						string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
						string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
						if (wagon.IsEngine)
							wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
						try
						{
							TrainCar car = RollingStock.Load(this, wagonFilePath, previousCar);
							car.Flipped = !wagon.Flip;
							car.UiD = wagon.UiD;
							train.Cars.Add(car);
							car.Train = train;
							previousCar = car;
						}
						catch (Exception error)
						{
							Trace.TraceInformation(wagonFilePath);
							Trace.WriteLine(error);
						}

					}// for each rail car

					if (train.Cars.Count == 0) return;

					// in static consists, the specified location represents the middle of the last car, 
					// our TDB traveller is always at the back of the last car so it needs to be repositioned
					TrainCar lastCar = train.LastCar;
					train.RearTDBTraveller.ReverseDirection();
					train.RearTDBTraveller.Move(lastCar.Length / 2f);
					train.RearTDBTraveller.ReverseDirection();

					train.CalculatePositionOfCars(0);
					train.InitializeBrakes();
                    train.InitializeSignals();

					Trains.Add(train);

				}
				catch (Exception error)
				{
					Trace.WriteLine(error);
				}
			}// for each train

		}

		private void SaveTrains(BinaryWriter outf)
		{
			if (PlayerLocomotive.Train != Trains[0])
			{
				for (int i = 1; i < Trains.Count; i++)
				{
					if (PlayerLocomotive.Train == Trains[i])
					{
						Trains[i] = Trains[0];
						Trains[0] = PlayerLocomotive.Train;
						break;
					}
				}
			}
			outf.Write(Trains.Count);
			foreach (Train train in Trains)
			{
				if (train.GetType() == typeof(Train))
					outf.Write(0);
				else if (train.GetType() == typeof(AITrain))
					outf.Write(1);
				else
				{
					Trace.TraceError("Don't know how to save train type: " + train.GetType().ToString());
					Debug.Fail("Don't know how to save train type: " + train.GetType().ToString());  // in debug mode, halt on this error
					outf.Write(1);  // for release version, we'll try to press on anyway
				}
				train.Save(outf);
			}
		}

		private void RestoreTrains(BinaryReader inf)
		{
			int count = inf.ReadInt32();
			Trains = new List<Train>();
			for (int i = 0; i < count; ++i)
			{
				int trainType = inf.ReadInt32();
				if (trainType == 0)
					Trains.Add(new Train(this, inf));
				else if (trainType == 1)
					Trains.Add(new AITrain(this, inf));
				else
				{
					Trace.TraceWarning("Don't know how to restore train type: " + trainType.ToString());
					Debug.Fail("Don't know how to restore train type: " + trainType.ToString());  // in debug mode, halt on this error
                    Trains.Add(new Train(this, inf)); // for release version, we'll try to press on anyway
				}
			}
            foreach (var train in Trains)
                train.InitializeSignals();
        }

		/// <summary>
		/// The front end of a railcar is at MSTS world coordinates x1,y1,z1
		/// The other end is at x2,y2,z2
		/// Return a rotation and translation matrix for the center of the railcar.
		/// </summary>
		public static Matrix XNAMatrixFromMSTSCoordinates(float x1, float y1, float z1, float x2, float y2, float z2)
		{
			// translate 1st coordinate to be relative to 0,0,0
			float dx = (float)(x1 - x2);
			float dy = (float)(y1 - y2);
			float dz = (float)(z1 - z2);

			// compute the rotational matrix  
			float length = (float)Math.Sqrt(dx * dx + dz * dz + dy * dy);
			float run = (float)Math.Sqrt(dx * dx + dz * dz);
			// normalize to coordinate to a length of one, ie dx is change in x for a run of 1
			dx /= length;
			dy /= length;   // ie if it is tilted back 5 degrees, this is sin 5 = 0.087
			run /= length;  //                              and   this is cos 5 = 0.996
			dz /= length;
			// setup matrix values

			Matrix xnaTilt = new Matrix(1, 0, 0, 0,
									 0, run, dy, 0,
									 0, -dy, run, 0,
									 0, 0, 0, 1);

			Matrix xnaRotation = new Matrix(dz, 0, dx, 0,
											0, 1, 0, 0,
											-dx, 0, dz, 0,
											0, 0, 0, 1);

			Matrix xnaLocation = Matrix.CreateTranslation((x1 + x2) / 2f, (y1 + y2) / 2f, -(z1 + z2) / 2f);
			return xnaTilt * xnaRotation * xnaLocation;
		}


		public void UncoupleBehind(TrainCar car)
		{
			Train train = car.Train;

			int i = 0;
			while (train.Cars[i] != car) ++i;  // it can't happen that car isn't in car.Train
			if (i == train.Cars.Count - 1) return;  // can't uncouple behind last car
			++i;

			TrainCar lead = train.LeadLocomotive;
			// move rest of cars to the new train
			Train train2 = new Train(this);
			for (int k = i; k < train.Cars.Count; ++k)
			{
				TrainCar newcar = train.Cars[k];
				train2.Cars.Add(newcar);
				newcar.Train = train2;
			}

			// and drop them from the old train
			for (int k = train.Cars.Count - 1; k >= i; --k)
			{
				train.Cars.RemoveAt(k);
			}

			train.LastCar.CouplerSlackM = 0;

			// and fix up the travellers
			train2.RearTDBTraveller = new TDBTraveller(train.RearTDBTraveller);
			train2.CalculatePositionOfCars(0);  // fix the front traveller
			train.RepositionRearTraveller();    // fix the rear traveller
            train2.InitializeSignals();

			Trains.Add(train2);
			train2.LeadLocomotive = lead;
			train.LeadLocomotive = lead;
			train.UncoupledFrom = train2;
			train2.UncoupledFrom = train;
			train2.SpeedMpS = train.SpeedMpS;
			train2.AITrainBrakePercent = train.AITrainBrakePercent;
			train2.AITrainDirectionForward = train.AITrainDirectionForward;
			if (PlayerLocomotive != null && PlayerLocomotive.Train == train2)
			{
				train2.AITrainThrottlePercent = train.AITrainThrottlePercent;
				train.AITrainThrottlePercent = 0;
			}

			train.Update(0);   // stop the wheels from moving etc
			train2.Update(0);  // stop the wheels from moving etc

			car.SignalEvent(EventID.Uncouple);
			// TODO which event should we fire
			//car.CreateEvent(62);  these are listed as alternate events
			//car.CreateEvent(63);
		}
	} // Simulator
}
