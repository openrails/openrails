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
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MSTS;


namespace ORTS
{
    public class Simulator
    {
        public bool Paused = true;          // start off paused, set to true once the viewer is fully loaded and initialized
        public float GameSpeed = 1;
        public double ClockTime = 0;         // relative to 00:00:00 on the day the activity starts 
                                                    // while Simulator.Update() is running, objects are adjusted to this target time 
                                                    // after Simulator.Update() is complete, the simulator state matches this time

        public string BasePath;     // ie c:\program files\microsoft games\train simulator
        public string RoutePath;    // ie c:\program files\microsoft games\train simulator\routes\usa1  - may be different on different pc's

        // Primary Simulator Data 
        // These items represent the current state of the simulator 
        // In multiplayer games, these items must be kept in sync across all players
        // These items are what are saved and loaded in a game save.
        public string RouteName;    // ie LPS, USA1  represents the folder name
        public ACTFile Activity;
        public TDBFile TDB;
        public TRKFile TRK;
        public TSectionDatFile TSectionDat;
        public List<Train> Trains = new List<Train>();
        public Signals Signals = null;
        public AI AI = null;
        public SeasonType Season;
        public WeatherType Weather;

        
        public TrainCar PlayerLocomotive = null;  // Set by the Viewer - TODO there could be more than one player so eliminate this.

        public Simulator(string activityPath)
        {
            RoutePath = Path.GetDirectoryName(Path.GetDirectoryName(activityPath));
            RouteName = Path.GetFileName(RoutePath);
            BasePath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));

            Console.Write("Loading ");

            Console.Write(" TRK");
            TRK = new TRKFile(MSTSPath.GetTRKFileName(RoutePath));

            Console.Write(" TDB");
            TDB = new TDBFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");

            Console.Write(" DAT");
            if (Directory.Exists(RoutePath + @"\GLOBAL") && File.Exists(RoutePath + @"\GLOBAL\TSECTION.DAT"))
                TSectionDat = new TSectionDatFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            else
                TSectionDat = new TSectionDatFile(BasePath + @"\GLOBAL\TSECTION.DAT");
            if (File.Exists(RoutePath + @"\TSECTION.DAT"))
                TSectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");

            Console.Write(" ACT");
            Activity = new ACTFile(activityPath);
        }

        // restart the simulator the the beginning of the activity
        public void Start()
        {
            // Clock time
            StartTime st = Activity.Tr_Activity.Tr_Activity_Header.StartTime;
            TimeSpan StartTime = new TimeSpan(st.Hour, st.Minute, st.Second);
            ClockTime = StartTime.TotalSeconds;
            // Switches
            AlignSwitchesToDefault();  // ie straight through routing
            // Trains
            Console.Write(" CON");
            Trains.Clear();
            InitializePlayerTrain();
            InitializeStaticConsists();
            // Get season and weather
            Season = Activity.Tr_Activity.Tr_Activity_Header.Season;
            Weather = Activity.Tr_Activity.Tr_Activity_Header.Weather;

            Signals = new Signals(this);
            AI = new AI(this);
        }

        // resume game after a save
        public void Restore(BinaryReader inf)
        {
            ClockTime = inf.ReadDouble();
            RestoreSwitchSettings(inf);
            RestoreTrains(inf);
            Signals = new Signals(this, inf);
            AI = new AI(this, inf);

        }

        // save game state so we can resume later
        public void Save(BinaryWriter outf)
        {
            outf.Write(ClockTime);
            SaveSwitchSettings(outf);
            SaveTrains(outf);
            Signals.Save(outf);
            AI.Save(outf);
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
                    break;
                }
            if (PlayerLocomotive == null)
                throw new System.Exception("Can't find player locomotive in activity");
            return PlayerLocomotive;
        }


        /// <summary>
        /// Convert and elapsed real time into clock time based on simulator
        /// running speed and paused state.
        /// </summary>
        /// <param name="elapsedRealTimeSeconds"></param>
        /// <returns></returns>
        public float GetElapsedClockSeconds( float elapsedRealSeconds )
        {
            return elapsedRealSeconds * (Paused ? 0 : GameSpeed);
        }

        /// <summary>
        /// Update the simulator state 
        /// elapsedClockSeconds represents the the time since the last call to Simulator.Update
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update( float elapsedClockSeconds )
        {
            // Advance the Clock
            ClockTime += elapsedClockSeconds;

            // Represent conditions at the specified clock time.
            List<Train> movingTrains = new List<Train>();
            if (PlayerLocomotive != null)
                movingTrains.Add(PlayerLocomotive.Train);
            foreach (Train train in Trains)
                if (train.SpeedMpS != 0 && train.GetType() != typeof(AITrain) && (PlayerLocomotive == null || train != PlayerLocomotive.Train))
                    movingTrains.Add(train);
            foreach (Train train in movingTrains)
            {
                train.Update(elapsedClockSeconds);
                AlignTrailingPointSwitches(train, train.MUDirection == Direction.Forward);
            }
            foreach (Train train in movingTrains)
                CheckForCoupling(train, elapsedClockSeconds);

            if( Signals != null ) Signals.Update(elapsedClockSeconds);
            if( AI != null ) AI.Update( elapsedClockSeconds );

        }

        /// <summary>
        /// Scan other trains
        /// </summary>
        /// <param name="train"></param>
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
                            drivenTrain.SetCoupleSpeed(train, 1);
                            foreach (TrainCar car in train.Cars)
                            {
                                drivenTrain.Cars.Add(car);
                                car.Train = drivenTrain;
                            }
                            drivenTrain.RepositionRearTraveller();
                            Trains.Remove(train);
                            if (train.UncoupledFrom != null)
                                train.UncoupledFrom.UncoupledFrom = null;
                            if (PlayerLocomotive != null && PlayerLocomotive.Train == train)
                            {
                                drivenTrain.AITrainThrottlePercent = train.AITrainThrottlePercent;
                                drivenTrain.AITrainBrakePercent = train.AITrainBrakePercent;
                            }
                            drivenTrain.LastCar.SignalEvent(EventID.Couple);
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
                            drivenTrain.SetCoupleSpeed(train, -1);
                            for (int i = train.Cars.Count - 1; i >= 0; --i)
                            {
                                TrainCar car = train.Cars[i];
                                drivenTrain.Cars.Add(car);
                                car.Train = drivenTrain;
                                car.Flipped = !car.Flipped;
                            }
                            drivenTrain.RepositionRearTraveller();
                            Trains.Remove(train);
                            if (train.UncoupledFrom != null)
                                train.UncoupledFrom.UncoupledFrom = null;
                            if (PlayerLocomotive != null && PlayerLocomotive.Train == train)
                            {
                                drivenTrain.AITrainThrottlePercent = train.AITrainThrottlePercent;
                                drivenTrain.AITrainBrakePercent = train.AITrainBrakePercent;
                            }
                            drivenTrain.LastCar.SignalEvent(EventID.Couple);
                            //Console.WriteLine("couple rr {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
                            return;
                        }
                        if (train == drivenTrain.UncoupledFrom && d1 > .5 && d2 > .5)
                        {
                            float d3 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, false);
                            float d4 = drivenTrain.FrontTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, false);
                            if (d3 > .5 && d4 > .5)
                            {
                                train.UncoupledFrom = null;
                                drivenTrain.UncoupledFrom = null;
                                //Console.WriteLine("release uncoupledfrom r {0} {1} {2} {3}", d1, d2, d3, d4);
                            }
                        }
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
                            drivenTrain.SetCoupleSpeed(train, 1);
                            for (int i = 0; i < train.Cars.Count; ++i)
                            {
                                TrainCar car = train.Cars[i];
                                drivenTrain.Cars.Insert(i, car);
                                car.Train = drivenTrain;
                            }
                            drivenTrain.CalculatePositionOfCars(0);
                            Trains.Remove(train);
                            if (train.UncoupledFrom != null)
                                train.UncoupledFrom.UncoupledFrom = null;
                            if (PlayerLocomotive != null && PlayerLocomotive.Train == train)
                            {
                                drivenTrain.AITrainThrottlePercent = train.AITrainThrottlePercent;
                                drivenTrain.AITrainBrakePercent = train.AITrainBrakePercent;
                            }
                            drivenTrain.FirstCar.SignalEvent(EventID.Couple);
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
                            drivenTrain.SetCoupleSpeed(train, -1);
                            for (int i = 0; i < train.Cars.Count; ++i)
                            {
                                TrainCar car = train.Cars[i];
                                drivenTrain.Cars.Insert(0, car);
                                car.Train = drivenTrain;
                                car.Flipped = !car.Flipped;
                            }
                            drivenTrain.CalculatePositionOfCars(0);
                            Trains.Remove(train);
                            if (train.UncoupledFrom != null)
                                train.UncoupledFrom.UncoupledFrom = null;
                            if (PlayerLocomotive != null && PlayerLocomotive.Train == train)
                            {
                                drivenTrain.AITrainThrottlePercent = train.AITrainThrottlePercent;
                                drivenTrain.AITrainBrakePercent = train.AITrainBrakePercent;
                            }
                            drivenTrain.FirstCar.SignalEvent(EventID.Couple);
                            //Console.WriteLine("couple ff {0} {1} {2}", elapsedClockSeconds, captureDistance, drivenTrain.SpeedMpS);
                            return;
                        }
                        if (train == drivenTrain.UncoupledFrom && d1 > .5 && d2 > .5)
                        {
                            float d3 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.FrontTDBTraveller, true);
                            float d4 = drivenTrain.RearTDBTraveller.OverlapDistanceM(train.RearTDBTraveller, true);
                            if (d3 > .5 && d4 > .5)
                            {
                                train.UncoupledFrom = null;
                                drivenTrain.UncoupledFrom = null;
                                //Console.WriteLine("release uncoupledfrom f {0} {1} {2} {3}",d1,d2,d3,d4);
                            }
                        }
                    }
            }
        }

        /// <summary>
        /// Sets the trailing point switches ahead of the train
        /// </summary>
        /// <param name="train"></param>
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
        public void SwitchTrackBehind( Train train)
        {
            TrJunctionNode nextSwitchTrack;
            nextSwitchTrack = train.RearTDBTraveller.TrJunctionNodeBehind();
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
        public void SwitchTrackAhead( Train train)
        {
            TrJunctionNode nextSwitchTrack;
            nextSwitchTrack = train.FrontTDBTraveller.TrJunctionNodeAhead();
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
                TDBTraveller traveller= new TDBTraveller(train.RearTDBTraveller);
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
            // set up the player locomotive
            // first extract the player service definition from the activity file
            // this gives the consist and path
            string playerServiceFileName = Activity.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name;
            SRVFile srvFile = new SRVFile(RoutePath + @"\SERVICES\" + playerServiceFileName + ".SRV");
            string playerConsistFileName = srvFile.Train_Config;
            CONFile conFile = new CONFile(BasePath + @"\TRAINS\CONSISTS\" + playerConsistFileName + ".CON");
            string playerPathFileName = srvFile.PathID;

            Train train = new Train();

            // This is the position of the back end of the train in the database.
            PATTraveller patTraveller = new PATTraveller(RoutePath + @"\PATHS\" + playerPathFileName + ".PAT");
            train.RearTDBTraveller = new TDBTraveller(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Z, 0, TDB, TSectionDat);
            // figure out if the next waypoint is forward or back
            patTraveller.NextWaypoint();
            if (train.RearTDBTraveller.DistanceTo(patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z) < 0)
                train.RearTDBTraveller.ReverseDirection();

            // add wagons
            foreach (Wagon wagon in conFile.Train.TrainCfg.Wagons)
            {

                string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCar car = RollingStock.Load(wagonFilePath);
                    car.Flipped = wagon.Flip;
                    train.Cars.Add(car);
                    car.Train = train;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("Couldn't open " + wagonFilePath + "\n" + error.Message);
                }

            }// for each rail car

            if (train.Cars.Count == 0) return;

            train.CalculatePositionOfCars(0);

            Trains.Add(train);
            train.AITrainBrakePercent = 100;


        }


        /// <summary>
        /// Set up trains based on info in the static consists listed in the activity file.
        /// </summary>
        private void InitializeStaticConsists()
        {
            // for each static consist
            foreach (ActivityObject activityObject in Activity.Tr_Activity.Tr_Activity_File.ActivityObjects)
            {
                try
                {
                    // construct train data
                    Train train = new Train();
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
                    for (int iWagon = activityObject.Train_Config.TrainCfg.Wagons.Count - 1; iWagon >= 0; --iWagon)
                    {
                        Wagon wagon = (Wagon)activityObject.Train_Config.TrainCfg.Wagons[iWagon];
                        string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
                        string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                        if (wagon.IsEngine)
                            wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
                        try
                        {
                            TrainCar car = RollingStock.Load(wagonFilePath);
                            car.Flipped = !wagon.Flip;
                            train.Cars.Add(car);
                            car.Train = train;
                        }
                        catch (System.Exception error)
                        {
                            Console.Error.WriteLine("Couldn't open " + wagonFilePath + "\n" + error.Message);
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

                    Trains.Add(train);

                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine(error);
                }
            }// for each train

        }

        private void SaveTrains(BinaryWriter outf)
        {
            outf.Write(Trains.Count);
            foreach (Train train in Trains)
            {
                if (train.GetType() == typeof(Train))
                    outf.Write(0);
                else if (train.GetType() == typeof(AITrain))
                    outf.Write(1);
                else
                {
                    Console.Error.WriteLine( "Don't know how to save train type: " + train.GetType().ToString() );
                    System.Diagnostics.Debug.Assert( false );  // in debug mode, halt on this error
                    outf.Write(1);  // for release version, we'll try to press on anyway
                }
                train.Save(outf);
            }
        }

        private void RestoreTrains(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            Trains.Clear();
            for (int i = 0; i < count; ++i)
            {
                int trainType = inf.ReadInt32();
                if (trainType == 0)
                    Trains.Add(new Train(inf));
                else if (trainType == 1)
                    Trains.Add(new AITrain(inf));
                else
                {
                    Console.Error.WriteLine("Don't know how to restore train type: " + trainType.ToString());
                    System.Diagnostics.Debug.Assert(false);  // in debug mode, halt on this error
                    Trains.Add(new Train(inf)); // for release version, we'll try to press on anyway
                }
            }
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
            //Console.WriteLine("uncouple {0}", i);

            // move rest of cars to the new train
            Train train2 = new Train();
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

            Trains.Add(train2);
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

            train.Update( 0 );   // stop the wheels from moving etc
            train2.Update( 0 );  // stop the wheels from moving etc

            car.SignalEvent(EventID.Uncouple);
            // TODO which event should we fire
            //car.CreateEvent(62);  these are listed as alternate events
            //car.CreateEvent(63);

        }

    } // Simulator

}
