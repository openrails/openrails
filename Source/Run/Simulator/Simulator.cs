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
///     scan the keyboard for locomotive control input
///     do physics calculations for train movement
///     scan keyboard for switch track change commands
///     compute new signal indications
///     operate ai trains
///     
/// Note: for now Update is called by the game engine in 3dViewer
/// but ultimately it was intended that Simulator.Update would operate
/// in its own thread independant of any viewers.
/// 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// </summary>


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS;
using System.IO;
using Microsoft.Xna.Framework.Input;
using System.Net;
using System.Net.Sockets;

namespace ORTS
{
    public class Simulator: IDisposable
    {
        // Primary Simulator Data 
        // These items represent the current state of the simulator 
        // In multiplayer games, these items must be kept in sync across all players
        // These items are what is saved and loaded in a game save.
        public string RouteName;    // ie LPS, USA1  represents the folder name
        public ACTFile Activity;
        public string RoutePath;    // ie c:\program files\microsoft games\train simulator\routes\usa1  - may be different on different pc's
        public TDBFile TDB;
        public TRKFile TRK;
        public TSectionDatFile TSectionDat;
        public List<Train> Trains = new List<Train>();

        public Train PlayerTrain { get { if (PlayerLocomotive != null) return PlayerLocomotive.Train; else return null; } }
        public LocomotiveSimulator PlayerLocomotive = null;  // TODO, make this a 'generic locomotive'

        public string BasePath;     // ie c:\program files\microsoft games\train simulator

        public double SecondsRunning = 0;   // the number of game time seconds the program has been running - stops when paused, etc

        public static Random Random = new Random();   // for use by the entire program

        KeyboardInput keyboard = new KeyboardInput();  // TODO this is clumsy Simulator tracks its own keypresses since its running in a different thread

        public NetworkHandler NetworkHandler = null;

        public bool Paused = false;

        public Signals Signals = null;
        public AI AI = null;

        public Simulator(string basePath, string destinationIP)
        {
            //NetworkHandler.Join(this, basePath, destinationIP);
        }

        /// <summary>
        /// Initialize the simulator to starting conditions in the specified activity
        /// </summary>
        /// <param name="activityPath"></param>
        public Simulator( string activityPath )
        {
            //NetworkHandler = new NetworkHandler(this);

            RoutePath = Path.GetDirectoryName( Path.GetDirectoryName(activityPath) );
            RouteName = Path.GetFileName(RoutePath);
            BasePath = Path.GetDirectoryName( Path.GetDirectoryName(RoutePath) );

            Console.Write("Loading ");

            Console.Write(" TRK");
            TRK = new TRKFile(MSTSPath.GetTRKFileName( RoutePath ));

            Console.Write(" TDB");
            TDB = new TDBFile(RoutePath + @"\" + TRK.Tr_RouteFile.FileName + ".tdb");

            Console.Write(" DAT");
            if( Directory.Exists( RoutePath + @"\GLOBAL" ) && File.Exists( RoutePath + @"\GLOBAL\TSECTION.DAT" ) )
                TSectionDat = new TSectionDatFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            else
                TSectionDat = new TSectionDatFile( BasePath + @"\GLOBAL\TSECTION.DAT");
            if( File.Exists( RoutePath + @"\TSECTION.DAT" ) )
                TSectionDat.AddRouteTSectionDatFile( RoutePath + @"\TSECTION.DAT");

            AlignSwitchesToDefault();  // ie straight through routing

            Console.Write(" ACT");
            Activity = new ACTFile(activityPath);

            Console.Write(" CON");
            InitializePlayerTrain();
            InitializeStaticConsists();

            Train playerTrain = Trains[0]; // TODO< temp code for now
            PlayerLocomotive = null;
            foreach( TrainCarSimulator car in playerTrain.Cars )
                if (car.GetType().IsSubclassOf( typeof(LocomotiveSimulator) ))  // first loco is the one the player drives
                {
                    PlayerLocomotive = (LocomotiveSimulator)car;
                    break;
                }
            if (PlayerLocomotive == null)
                throw new System.Exception("Can't find player locomotive in activity");

            Signals = new Signals(this);
            AI = new AI(this);

            keyboard.SetKeyboardState(Keyboard.GetState());
            
        }

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
            if( train.RearTDBTraveller.DistanceTo( patTraveller.TileX, patTraveller.TileZ, patTraveller.X, patTraveller.Y, patTraveller.Z ) < 0 )
                train.RearTDBTraveller.ReverseDirection();
            train.PATTraveller = patTraveller;

            // add wagons
            foreach (ConsistTrainset wagon in conFile)
            {
                
                string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.File + ".wag"; ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCarSimulator car = TrainCarSimulator.Create( wagonFilePath);  
                    train.Cars.Add(car);
                    car.Train = train;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("Couldn't open " + wagonFilePath + "\n" + error.Message);
                }
              
            }// for each rail car

            train.CalculatePositionOfCars( 0 );

            Trains.Add(train);


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
                    int consistDirection = (activityObject.Direction & 1);  // 1 = forward
                    train.RearTDBTraveller = new TDBTraveller(activityObject.TileX, activityObject.TileZ, activityObject.X, activityObject.Z, 1, TDB, TSectionDat);
                    if (consistDirection != 1)
                        train.RearTDBTraveller.ReverseDirection();
                    // add wagons in reverse order - ie first wagon is at back of train
                    // static consists are listed back to front in the activities, so we have to reverse the order, and flip the cars
                    // when we add them to ORTS
                    for ( int iWagon = activityObject.Train_Config.TrainCfg.Wagons.Count -1; iWagon >= 0; --iWagon)
                    {
                        Wagon wagon = (Wagon) activityObject.Train_Config.TrainCfg.Wagons[iWagon];
                        string wagonFolder = BasePath + @"\trains\trainset\" + wagon.Folder;
                        string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                        if (wagon.IsEngine)
                            wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
                        try
                        {
                            TrainCarSimulator car = TrainCarSimulator.Create(wagonFilePath);
                            car.Flipped = !wagon.Flip;
                            train.Cars.Add(car);
                            car.Train = train;
                        }
                        catch (System.Exception error)
                        {
                            Console.Error.WriteLine("Couldn't open " + wagonFilePath + "\n" + error.Message);
                        }

                    }// for each rail car

                // in static consists, the specified location represents the middle of the last car, 
                // our TDB traveller is always at the back of the last car so it needs to be repositioned
                TrainCarSimulator lastCar = train.LastCar;
                train.RearTDBTraveller.ReverseDirection();
                train.RearTDBTraveller.Move(lastCar.WagFile.Wagon.Length / 2f);
                train.RearTDBTraveller.ReverseDirection();

                train.CalculatePositionOfCars( 0 );

                Trains.Add(train);

                }
                catch( System.Exception error )
                {
                    Console.Error.WriteLine(error);
                }
            }// for each train

        }

        /// <summary>
        /// Update is called regularly to
        ///     scan the keyboard for locomotive control input
        ///     do physics calculations for train movement
        ///     scan keyboard for switch track change commands
        ///     compute new signal indications
        ///     operate ai trains
        ///     
        /// Note: for now Update is called by the game engine in 3dViewer
        /// but ultimately it was intended that Simulator.Update would operate
        /// in its own thread independant of any viewers.
        /// </summary>
        public void Update( GameTime gameTime )
        {
            keyboard.SetKeyboardState(Keyboard.GetState());

            // Pause
            if (keyboard.IsPressed(Keys.Pause) && keyboard.IsShiftDown()) { Paused = !Paused; } 

            if (Paused)
                return;

            SecondsRunning += gameTime.ElapsedGameTime.TotalSeconds;

            // Debug Stop
            if (keyboard.IsKeyDown(Keys.Pause) && !keyboard.IsShiftDown() ) { 
                int a = 1; }  

            // Switch Track Control
            if (keyboard.IsPressed(Keys.G)) // Switch Track?
            {
                TrJunctionNode nextSwitchTrack;
                if( keyboard.IsKeyDown( Keys.LeftShift ) || keyboard.IsKeyDown( Keys.RightShift) )
                    nextSwitchTrack = PlayerTrain.RearTDBTraveller.TrJunctionNodeBehind();
                else
                    nextSwitchTrack = PlayerTrain.FrontTDBTraveller.TrJunctionNodeAhead();

                if (nextSwitchTrack != null)
                {
                    if (nextSwitchTrack.SelectedRoute == 0)
                        nextSwitchTrack.SelectedRoute = 1;
                    else
                        nextSwitchTrack.SelectedRoute = 0;
                }
            }


            PlayerLocomotive.HandleKeyboard(keyboard, gameTime);  // TODO clumsy - we do this here so that it is handled in the simulator thread, vs the viewer thread

            PlayerTrain.Update(gameTime);
            Signals.Update(gameTime);
            AI.Update(gameTime);

            AlignTrailingPointSwitches(PlayerTrain, PlayerLocomotive.Forward);

            CheckForCoupling( PlayerTrain );

        }

        /// <summary>
        /// Scan other trains
        /// </summary>
        /// <param name="train"></param>
        public void CheckForCoupling(Train playerTrain)
        {
            float captureDistance = 0.1f * playerTrain.SpeedMpS / 5.0f;
            float captureDistanceSquared = captureDistance * captureDistance;

            if (playerTrain.SpeedMpS < 0.01)
            {
                foreach (Train train in Trains)
                    if (train != playerTrain)
                    {
                        if (WorldLocation.DistanceSquared(playerTrain.RearTDBTraveller.WorldLocation, train.FrontTDBTraveller.WorldLocation) < captureDistanceSquared)
                        {
                            // couple my rear to front of train
                            if( playerTrain.SpeedMpS < -0.1f )
                                playerTrain.SpeedMpS = -0.1f;  // TODO, make this depend on mass and brake settings on cars coupled to
                            foreach (TrainCarSimulator car in train.Cars)
                            {
                                playerTrain.Cars.Add(car);
                                car.Train = playerTrain;
                            }
                            playerTrain.RepositionRearTraveller();
                            Trains.Remove(train);
                            PlayerTrain.LastCar.CreateEvent(58);
                            return;
                        }
                        if (WorldLocation.DistanceSquared(playerTrain.RearTDBTraveller.WorldLocation, train.RearTDBTraveller.WorldLocation) < captureDistanceSquared)
                        {
                            // couple my rear to rear of train
                            if (playerTrain.SpeedMpS < -0.1f)
                                playerTrain.SpeedMpS = -0.1f;
                            for (int i = train.Cars.Count - 1; i >= 0; --i)
                            {
                                TrainCarSimulator car = train.Cars[i];
                                playerTrain.Cars.Add(car);
                                car.Train = playerTrain;
                                car.Flipped = !car.Flipped;
                            }
                            playerTrain.RepositionRearTraveller();
                            Trains.Remove(train);
                            PlayerTrain.LastCar.CreateEvent(58);
                            return;
                        }
                    }
            }
            else if (playerTrain.SpeedMpS > 0.01 )
            {
                foreach (Train train in Trains)
                    if (train != playerTrain)
                    {
                        if (WorldLocation.DistanceSquared(playerTrain.FrontTDBTraveller.WorldLocation, train.RearTDBTraveller.WorldLocation) < captureDistanceSquared)
                        {
                            // couple my front to rear of train
                            if (playerTrain.SpeedMpS > 0.1f)
                                playerTrain.SpeedMpS = 0.1f;
                            for (int i = 0; i < train.Cars.Count; ++i)
                            {
                                TrainCarSimulator car = train.Cars[i];
                                playerTrain.Cars.Insert( i , car); 
                                car.Train = playerTrain;
                            }
                            playerTrain.CalculatePositionOfCars(0);
                            Trains.Remove(train);
                            PlayerTrain.FirstCar.CreateEvent(58);
                            return;
                        }
                        if (WorldLocation.DistanceSquared(playerTrain.FrontTDBTraveller.WorldLocation, train.FrontTDBTraveller.WorldLocation) < captureDistanceSquared)
                        {
                            // couple my front to front of train
                            if (playerTrain.SpeedMpS > 0.1f)
                                playerTrain.SpeedMpS = 0.1f;
                            for (int i = 0; i < train.Cars.Count; ++i)
                            {
                                TrainCarSimulator car = train.Cars[i];
                                playerTrain.Cars.Insert(0, car);
                                car.Train = playerTrain;
                                car.Flipped = !car.Flipped;
                            }
                            playerTrain.CalculatePositionOfCars(0);
                            Trains.Remove(train);
                            PlayerTrain.FirstCar.CreateEvent(58);
                            return;
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

            // find the next switch
            while (traveller.TN.TrJunctionNode == null)
            {
                if( !traveller.NextSection())
                    return;   // no more switches
            }
            TrJunctionNode nextSwitchTrack = traveller.TN.TrJunctionNode;

            // if we are facing the points of the switch we don't do anything
            if (traveller.iEntryPIN == 0) return;   

            // otherwise we are coming in on the trailing side of the switch
            // so line it up for the correct route
            nextSwitchTrack.SelectedRoute = traveller.iEntryPIN - 1;
        }

        TrackNode lastAlignedAtTrackNode = null;
        bool lastAlignedMovingForward = false;

        /// <summary>
        /// Return true if the train is within a meter of the waypoint
        /// </summary>
        /// <param name="patTraveller"></param>
        /// <param name="tdbTraveller"></param>
        /// <returns></returns>
        public bool TrainIsAtWaypoint(PATTraveller patTraveller, TDBTraveller tdbTraveller)
        {
            float distance = 0;

            distance += Math.Abs((patTraveller.TileX - tdbTraveller.TileX) * 2048 + (patTraveller.X - tdbTraveller.X));
            distance += Math.Abs((patTraveller.TileZ - tdbTraveller.TileZ) * 2048 + (patTraveller.Z - tdbTraveller.Z));
            distance += Math.Abs(patTraveller.Y - tdbTraveller.Y);

            return (distance < 1); // TODO, arbitrary
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

            Matrix xnaTilt = new Matrix(1,   0,  0, 0,
                                     0,  run, dy, 0,
                                     0, -dy, run, 0,
                                     0,   0,  0, 1);

            Matrix xnaRotation = new Matrix(dz, 0, dx, 0,
                                            0, 1, 0, 0,
                                            -dx, 0, dz, 0,
                                            0, 0, 0, 1);

            Matrix xnaLocation = Matrix.CreateTranslation((x1 + x2) / 2f, (y1 + y2) / 2f, -(z1 + z2) / 2f);
            return xnaTilt * xnaRotation  * xnaLocation;
        }


        public void UncoupleBehind( TrainCarSimulator car )
        {
            Train train = car.Train;

            int i = 0;
            while (train.Cars[i] != car) ++i;  // it can't happen that car isn't in car.Train
            if (i == train.Cars.Count - 1) return;  // can't uncouple behind last car
            ++i;

            // move rest of cars to the new train
            Train train2 = new Train();
            for( int k = i; k < train.Cars.Count; ++k )
            {
                TrainCarSimulator newcar = train.Cars[k];
                train2.Cars.Add(newcar);
                newcar.Train = train2;
            }

            // and drop them from the old train
            for (int k = train.Cars.Count - 1; k >= i ; --k)
            {
                train.Cars.RemoveAt(k);
            }

            // and fix up the travellers
            train2.RearTDBTraveller = new TDBTraveller(train.RearTDBTraveller);
            train2.CalculatePositionOfCars(0);  // fix the front traveller
            train.RepositionRearTraveller();    // fix the rear traveller

            Trains.Add(train2);

            train.Update( null);   // stop the wheels from moving etc
            train2.Update( null );  // stop the wheels from moving etc

            car.CreateEvent(61);
            // TODO which event should we fire
            //car.CreateEvent(62);  these are listed as alternate events
            //car.CreateEvent(63);

        }


        #region IDisposable Members

        public void Dispose()
        {
            if( NetworkHandler != null )
                NetworkHandler.Dispose();
        }

        #endregion
    } // Simulator

    public interface CarEventHandler
    {
        void HandleCarEvent(int eventID);
    }


}
