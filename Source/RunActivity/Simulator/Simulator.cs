// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

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
/// </summary>

// Uncommenting the following will enable the experimental route-editing sandbox:
//#define RE_ENABLED //WaltN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms; // Needed for MessageBox
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Interlocking;
using ORTS.MultiPlayer;

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
		public string RoutePathName;    // ie LPS, USA1  represents the folder name
        public string RouteName;
        public string ActivityFileName;
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
		public string patFileName;
		public string conFileName;
        public AIPath PlayerPath;
        public LevelCrossings LevelCrossings;
        public RDBFile RDB;
		public CarSpawnerFile CarSpawnerFile;
        public bool UseAdvancedAdhesion;
        public bool BreakCouplers;
        // Used in save and restore form
        public string PathName = "<unknown>";
        public float InitialTileX;
        public float InitialTileZ;

		public bool InControl = true;//For multiplayer, a player may not control his/her own train (as helper)
		/// <summary>
		/// Reference to the InterlockingSystem object, responsible for
		/// managing signalling and interlocking.
		/// </summary>
		public InterlockingSystem InterlockingSystem;


		public TrainCar PlayerLocomotive = null;    // Set by the Viewer - TODO there could be more than one player so eliminate this.
        public Confirmer Confirmer;                 // Set by the Viewer

		public Simulator(UserSettings settings, string activityPath)
		{
			Settings = settings;
            UseAdvancedAdhesion = Settings.UseAdvancedAdhesion;
            BreakCouplers = Settings.BreakCouplers;
			RoutePath = Path.GetDirectoryName(Path.GetDirectoryName(activityPath));
			RoutePathName = Path.GetFileName(RoutePath);
			BasePath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));

			Trace.Write("Loading ");

			Trace.Write(" TRK");
			TRK = new TRKFile(MSTSPath.GetTRKFileName(RoutePath));
            RouteName = TRK.Tr_RouteFile.Name;

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

			var carSpawnFile = RoutePath + @"\carspawn.dat";
			if (File.Exists(carSpawnFile))
			{
				Trace.Write(" CARSPAWN");
				CarSpawnerFile = new CarSpawnerFile(RoutePath + @"\carspawn.dat", RoutePath + @"\shapes\");
			}

		}
		public void SetActivity(string activityPath)
		{
            ActivityFileName = Path.GetFileNameWithoutExtension(activityPath);
            Activity = new ACTFile(activityPath);
			ActivityRun = new Activity(Activity, this);
            if (ActivityRun.Current == null && ActivityRun.EventList.Count == 0)
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
			if (MPManager.IsServer())
			{
				if (Activity == null)
				{
					this.Trains[0].TrackAuthority = new TrackAuthority(this.Trains[0], 0, 10, this.Trains[0].Path);
					AI.Dispatcher.TrackAuthorities.Add(this.Trains[0].TrackAuthority);
					AI.Dispatcher.RequestAuth(this.Trains[0], true, 0);
				}
				MPManager.Instance().RememberOriginalSwitchState();
			}

		}

		public void Stop()
		{
			if (RailDriver != null)
				RailDriver.Shutdown();
			if (MultiPlayer.MPManager.IsMultiPlayer()) MultiPlayer.MPManager.Stop();
		}

        public void Restore( BinaryReader inf, string simulatorPathDescription, float initialTileX, float initialTileZ )
		{
            ClockTime = inf.ReadDouble();
            Season = (SeasonType)inf.ReadInt32();
            Weather = (WeatherType)inf.ReadInt32();
            PathName = simulatorPathDescription; // Needed for Resume header data
            InitialTileX = initialTileX;
            InitialTileZ = initialTileZ;

            RestoreSwitchSettings(inf);
            Signals = new Signals(this, SIGCFG, inf);
            RestoreTrains(inf);
            LevelCrossings = new LevelCrossings(this);
            InterlockingSystem = new InterlockingSystem(this);
            AI = new AI(this, inf);
            ActivityRun = ORTS.Activity.Restore( inf, this, ActivityRun );
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
            PlayerLocomotive = InitialPlayerLocomotive();
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
		/// elapsedClockSeconds represents the time since the last call to Simulator.Update
		/// Executes in the UpdaterProcess thread.
		/// </summary>
        [CallOnThread("Updater")]
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
                /*
				if (MPManager.IsMultiPlayer())
				{
					if (MultiPlayer.MPManager.IsServer()) 
                        AlignTrailingPointSwitches(train, train.MUDirection == Direction.Forward);
			    }
				else 
                   AlignTrailingPointSwitches(train, train.MUDirection == Direction.Forward);
				}
                */
				if (Activity == null || MultiPlayer.MPManager.IsMultiPlayer())
                {
                    AlignTrailingPointSwitches(train, train.MUDirection == Direction.Forward);
                }
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

            LevelCrossings.Update(elapsedClockSeconds);
           
            if (ActivityRun != null)
			{
				ActivityRun.Update();
			}

			if (RailDriver != null)
			{
				RailDriver.Update(PlayerLocomotive);
			}

			InterlockingSystem.Update(elapsedClockSeconds);

			if (MultiPlayer.MPManager.IsMultiPlayer()) MultiPlayer.MPManager.Instance().Update(GameTime);

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

			drivenTrain.CheckFreight();  // check if train in new consist is freight or passenger
		}

		private void UpdateUncoupled(Train drivenTrain, Train train, float d1, float d2, bool rear)
		{
			if (train == drivenTrain.UncoupledFrom && d1 > .5 && d2 > .5)
			{
				Traveller traveller = rear ? drivenTrain.RearTDBTraveller : drivenTrain.FrontTDBTraveller;
				float d3 = traveller.OverlapDistanceM(train.FrontTDBTraveller, rear);
				float d4 = traveller.OverlapDistanceM(train.RearTDBTraveller, rear);
				if (d3 > .5 && d4 > .5)
				{
					train.UncoupledFrom = null;
					drivenTrain.UncoupledFrom = null;
				}
			}
		}

		/// <summary>
		/// Scan other trains
		/// </summary>
		public void CheckForCoupling(Train drivenTrain, float elapsedClockSeconds)
		{
			if (MPManager.IsMultiPlayer() && !MPManager.IsServer()) return; //in MultiPlayer mode, server will check coupling, client will get message and do things
			if (drivenTrain.SpeedMpS < 0)
			{
				foreach (Train train in Trains)
					if (train != drivenTrain)
					{
						//avoid coupling of player train with other players train
						if (MPManager.IsMultiPlayer() && !MPManager.Instance().TrainOK2Couple(drivenTrain, train)) continue;
						
						float d1 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, true);
						if (d1 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
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
							if (MPManager.IsMultiPlayer()) MPManager.BroadCast((new MSGCouple(drivenTrain, train)).ToString());
							return;
						}
						float d2 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, true);
						if (d2 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
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
							if (MPManager.IsMultiPlayer()) MPManager.BroadCast((new MSGCouple(drivenTrain, train)).ToString());
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
						//avoid coupling of player train with other players train if it is too short alived (e.g, when a train is just spawned, it may overlap with another train)
						if (MPManager.IsMultiPlayer() && !MPManager.Instance().TrainOK2Couple(drivenTrain, train)) continue;
					//	{
					//		if ((MPManager.Instance().FindPlayerTrain(train) && drivenTrain == PlayerLocomotive.Train) || (MPManager.Instance().FindPlayerTrain(drivenTrain) && train == PlayerLocomotive.Train)) continue;
					//		if ((MPManager.Instance().FindPlayerTrain(train) && MPManager.Instance().FindPlayerTrain(drivenTrain))) continue; //if both are player-controlled trains
					//	}
						float d1 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, false);
						if (d1 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
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
							if (MPManager.IsMultiPlayer()) MPManager.BroadCast((new MSGCouple(drivenTrain, train)).ToString());
							return;
						}
						float d2 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, false);
						if (d2 < 0)
						{
							if (train == drivenTrain.UncoupledFrom)
							{
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
							if (MPManager.IsMultiPlayer()) MPManager.BroadCast((new MSGCouple(drivenTrain, train)).ToString());
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
            Traveller traveller = forward ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);

			// to save computing power, skip this if we haven't changed nodes or direction
			if (traveller.TN == lastAlignedAtTrackNode && forward == lastAlignedMovingForward) return;
			lastAlignedAtTrackNode = traveller.TN;
			lastAlignedMovingForward = forward;

			// find the next switch by scanning ahead for a TrJunctionNode
			while (!traveller.IsJunction)
			{
				if (!traveller.NextSection())
					return;   // no more switches
			}
			TrJunctionNode nextSwitchTrack = traveller.TN.TrJunctionNode;
			if (SwitchIsOccupied(nextSwitchTrack))
				return;

			// if we are facing the points of the switch we don't do anything
			if (traveller.JunctionEntryPinIndex == 0) return;

			// otherwise we are coming in on the trailing side of the switch
			// so line it up for the correct route
			nextSwitchTrack.SelectedRoute = traveller.JunctionEntryPinIndex - 1;
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
            TrJunctionNode nextSwitchTrack = train.LeadLocomotive.Flipped ? train.FrontTDBTraveller.JunctionNodeAhead() : train.RearTDBTraveller.JunctionNodeBehind();
			if (SwitchIsOccupied(nextSwitchTrack))
				return;

			if (nextSwitchTrack != null)
			{
				if (!MPManager.IsMultiPlayer()||MPManager.IsServer()) //single mode, or server
				{
					if (nextSwitchTrack.SelectedRoute == 0)
						nextSwitchTrack.SelectedRoute = 1;
					else
						nextSwitchTrack.SelectedRoute = 0;

					//multiplayer mode will do some message
					if (MPManager.IsMultiPlayer())
						MPManager.Notify((new MultiPlayer.MSGSwitch(MultiPlayer.MPManager.GetUserName(),
							nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldID, nextSwitchTrack.SelectedRoute, true)).ToString());
					Confirmer.Confirm(CabControl.SwitchBehind, CabSetting.On);

				}
				else
				{
					var Selected = 0;
					if (nextSwitchTrack.SelectedRoute == 0)
						Selected = 1;

					//notify the server
					MPManager.Notify((new MultiPlayer.MSGSwitch(MultiPlayer.MPManager.GetUserName(),
							nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldID, Selected, true)).ToString());
					Confirmer.Information("Switching Request Sent to the Server");
				}
            }
		}

		/// <summary>
		/// Align the switchtrack ahead of the players train to the opposite position
		/// </summary>
		public void SwitchTrackAhead(Train train)
		{
            TrJunctionNode nextSwitchTrack = train.LeadLocomotive.Flipped ? train.RearTDBTraveller.JunctionNodeBehind() : train.FrontTDBTraveller.JunctionNodeAhead();
            if (SwitchIsOccupied(nextSwitchTrack))
				return;

			if (nextSwitchTrack != null)
			{
				if (!MPManager.IsMultiPlayer() || MPManager.IsServer()) //single mode, or server
				{
					if (nextSwitchTrack.SelectedRoute == 0)
						nextSwitchTrack.SelectedRoute = 1;
					else
						nextSwitchTrack.SelectedRoute = 0;

					//multiplayer mode will do some message
					if (MPManager.IsMultiPlayer())
						MPManager.Notify((new MultiPlayer.MSGSwitch(MultiPlayer.MPManager.GetUserName(),
							nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldID, nextSwitchTrack.SelectedRoute, true)).ToString());
					train.ResetSignal(false);
					Confirmer.Confirm(CabControl.SwitchAhead, CabSetting.On);

				}
				else
				{
					var Selected = 0;
					if (nextSwitchTrack.SelectedRoute == 0)
						Selected = 1;

					//notify the server
					MPManager.Notify((new MultiPlayer.MSGSwitch(MultiPlayer.MPManager.GetUserName(),
							nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldID, Selected, true)).ToString());
					Confirmer.Information("Switching Request Sent to the Server");
				}
			}
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
				Traveller traveller = new Traveller(train.RearTDBTraveller);
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
			train.TrainType = Train.TRAINTYPE.PLAYER;

//WaltN: Temporary facility for track-laying experiments
#if RE_ENABLED
            train.EditTrain = new TrackLayer(TDB, TSectionDat); // Creates a TrackLayer for the player train
#endif

            PATFile patFile = new PATFile(patFileName);
            PathName = patFile.Name;
            // This is the position of the back end of the train in the database.
			PATTraveller patTraveller = new PATTraveller(patFileName);
            AIPath aiPath = new AIPath(patFile, TDB, TSectionDat, patFileName);
            PlayerPath = aiPath;
            train.RearTDBTraveller = new Traveller(TSectionDat, TDB.TrackDB.TrackNodes, patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Z);

			train.Path = aiPath;

			aiPath.AlignInitSwitches(train.RearTDBTraveller, -1, 500);
            //aiPath.AlignAllSwitches();

			// figure out if the next waypoint is forward or back
			patTraveller.NextWaypoint();
			if (train.RearTDBTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z) < 0)
				train.RearTDBTraveller.ReverseDirection();

			CONFile conFile = new CONFile(conFileName);

			// add wagons
			TrainCar previousCar = null;
			foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
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
                    if (MPManager.IsMultiPlayer()) car.CarID = MPManager.GetUserName() + " - " + car.UiD; //player's train is always named train 0.
                    else car.CarID = "0 - " + car.UiD; //player's train is always named train 0.
                    train.Cars.Add(car);
                    car.Train = train;
                    previousCar = car;
                    if ((Activity != null) && (car.GetType() == typeof(MSTSDieselLocomotive)))
                    {
                        ((MSTSDieselLocomotive)car).DieselLevelL = ((MSTSDieselLocomotive)car).MaxDieselLevelL * Activity.Tr_Activity.Tr_Activity_Header.FuelDiesel / 100.0f;
                    }
                }
                catch (Exception error)
                {
                    Trace.TraceInformation(wagonFilePath);
                    // First wagon is the player's loco and required, so issue a fatal error message
                    if (wagon == conFile.Train.TrainCfg.WagonList[0])
                        throw new InvalidDataException("Error loading the player locomotive.", error);
                    else
                        Trace.WriteLine(error);
                }

			}// for each rail car

			if (train.Cars.Count == 0) return;  // Shouldn't we issue an error message if the player's train is empty?

			train.CheckFreight();
			train.CalculatePositionOfCars(0);

			Trains.Add(train);
			train.AITrainBrakePercent = 100;
			train.RouteMaxSpeedMpS = (float) TRK.Tr_RouteFile.SpeedLimit;
            train.InitializeSignals(false); // initialize without existing speed limits
            // Note the initial position to be stored by a Save and used in Menu.exe to calculate DistanceFromStartM 
            InitialTileX = Trains[0].FrontTDBTraveller.TileX + (Trains[0].FrontTDBTraveller.X / 2048);
            InitialTileZ = Trains[0].FrontTDBTraveller.TileZ + (Trains[0].FrontTDBTraveller.Z / 2048);

        }


		/// <summary>
		/// Set up trains based on info in the static consists listed in the activity file.
		/// </summary>
		private void InitializeStaticConsists()
		{
			if (Activity == null) return;
            if (Activity.Tr_Activity == null) return;
            if (Activity.Tr_Activity.Tr_Activity_File == null) return;
            if (Activity.Tr_Activity.Tr_Activity_File.ActivityObjects == null) return;
            if (Activity.Tr_Activity.Tr_Activity_File.ActivityObjects.ActivityObjectList == null) return;
            // for each static consist
			foreach (ActivityObject activityObject in Activity.Tr_Activity.Tr_Activity_File.ActivityObjects.ActivityObjectList)
			{
				try
				{
					// construct train data
					Train train = new Train(this);
					train.TrainType = Train.TRAINTYPE.STATIC;
					int consistDirection;
					switch (activityObject.Direction)  // TODO, we don't really understand this
					{
						case 0: consistDirection = 0; break;  // reversed ( confirmed on L&PS route )
						case 18: consistDirection = 1; break;  // forward ( confirmed on ON route )
						case 131: consistDirection = 1; break; // forward ( confirmed on L&PS route )
						default: consistDirection = 1; break;  // forward ( confirmed on L&PS route )
					}
                    // FIXME: Where are TSectionDat and TDB from?
                    train.RearTDBTraveller = new Traveller(TSectionDat, TDB.TrackDB.TrackNodes, activityObject.TileX, activityObject.TileZ, activityObject.X, activityObject.Z);
					if (consistDirection != 1)
						train.RearTDBTraveller.ReverseDirection();
					// add wagons in reverse order - ie first wagon is at back of train
					// static consists are listed back to front in the activities, so we have to reverse the order, and flip the cars
					// when we add them to ORTS
					TrainCar previousCar = null;
					for (int iWagon = activityObject.Train_Config.TrainCfg.WagonList.Count - 1; iWagon >= 0; --iWagon)
					{
						Wagon wagon = (Wagon)activityObject.Train_Config.TrainCfg.WagonList[iWagon];
						string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
						string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
						if (wagon.IsEngine)
							wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
						try
						{
							TrainCar car = RollingStock.Load(this, wagonFilePath, previousCar);
							car.Flipped = !wagon.Flip;
							car.UiD = wagon.UiD;
							car.CarID = activityObject.ID + " - " + car.UiD;
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
                    train.InitializeSignals(false);  // initialize without existing speed limits

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
                    throw new InvalidDataException(String.Format("Unable to save a train of type {0}", train.GetType()));
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
                    Trace.TraceWarning("Ignored unknown train type {0} during restore", trainType);
					Debug.Fail("Don't know how to restore train type: " + trainType.ToString());  // in debug mode, halt on this error
                    Trains.Add(new Train(this, inf)); // for release version, we'll try to press on anyway
				}
			}
 // REMOVED from this location to restore of Train itself [R.Roeterdink]
 //           foreach (var train in Trains)
 //               train.InitializeSignals();
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

			if (MPManager.IsMultiPlayer() && !MPManager.Instance().TrainOK2Decouple(train)) return;
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
			train2.RearTDBTraveller = new Traveller(train.RearTDBTraveller);
			train2.CalculatePositionOfCars(0);  // fix the front traveller
			train.RepositionRearTraveller();    // fix the rear traveller
            train2.InitializeSignals(false);  // initialize signals without existing speed information

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
				train2.TrainType = Train.TRAINTYPE.PLAYER;
				train.TrainType = Train.TRAINTYPE.STATIC;
			}
			else
			{
				train2.TrainType = Train.TRAINTYPE.STATIC;
			}

			if (MPManager.IsMultiPlayer() && !MPManager.IsServer())
			{
				//add the new train to a list of uncoupled trains, handled specially
				if (PlayerLocomotive != null && PlayerLocomotive.Train == train2) MPManager.Instance().AddUncoupledTrains(train);
				else if (PlayerLocomotive != null && PlayerLocomotive.Train == train) MPManager.Instance().AddUncoupledTrains(train2);
			}


			train.CheckFreight();
			train2.CheckFreight();

			train.Update(0);   // stop the wheels from moving etc
			train2.Update(0);  // stop the wheels from moving etc

			car.SignalEvent(EventID.Uncouple);
			// TODO which event should we fire
			//car.CreateEvent(62);  these are listed as alternate events
			//car.CreateEvent(63);
			if (MPManager.IsMultiPlayer())
				MPManager.Notify((new MultiPlayer.MSGUncouple(train, train2, MultiPlayer.MPManager.GetUserName(), car.CarID, PlayerLocomotive)).ToString());
		}
	} // Simulator
}
