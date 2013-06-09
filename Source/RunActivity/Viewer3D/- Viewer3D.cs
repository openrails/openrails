// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

/* 3D Viewer

    /// This a 3D viewer.  It connects to a simulator engine, rendering the route content and
    /// rolling stock.
    /// 
    /// When the 3D viewer is constructed its passed a reference to the simulator engine, and a flag
    /// indicating if it should operate in fullscreen mode or windowed mode.   After construction, 
    /// LookAt attaches the viewer a TrainCar in the simulator.
    /// 
 *  
 *  The Viewer class actually represents the screen window on which the camera is rendered.
 * 
 * TODO, add note re abandoning Viewer.Components
 *      - control over render order - ie sorting by material to minimize state changes
 *      - multitasking issues
 *      - multipass techniques, such as shadow mapping 
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;
using ORTS.MultiPlayer;
namespace ORTS
{
    public class Viewer3D
    {
        // User setups.
        public readonly UserSettings Settings;
        // Multi-threaded processes
        public LoaderProcess LoaderProcess;
        public UpdaterProcess UpdaterProcess;
        public RenderProcess RenderProcess;
        public SoundProcess SoundProcess;
        // Access to the XNA Game class
        public GraphicsDeviceManager GDM;
        public GraphicsDevice GraphicsDevice;
        public SharedTextureManager TextureManager;
        public SharedMaterialManager MaterialManager;
        public SharedShapeManager ShapeManager;
        public Point DisplaySize;
        // Components
        public readonly Simulator Simulator;
        public World World;
        /// <summary>
        /// Monotonically increasing time value (in seconds) for the game/viewer. Starts at 0 and only ever increases, at real-time.
        /// </summary>
        public double RealTime = 0;
        InfoDisplay InfoDisplay;
        public WindowManager WindowManager = null;
        public QuitWindow QuitWindow; // Escape window
        public MessagesWindow MessagesWindow; // Game message window (special, always visible)
        public PauseWindow PauseWindow; // Game paused window (special)
        public HelpWindow HelpWindow; // F1 window
        public TrackMonitorWindow TrackMonitorWindow; // F4 window
        public HUDWindow HUDWindow; // F5 hud
        public SwitchWindow SwitchWindow; // F8 window
        public TrainOperationsWindow TrainOperationsWindow; // F9 window
        public NextStationWindow NextStationWindow; // F10 window
        public CompassWindow CompassWindow; // 0 window
        public ActivityWindow ActivityWindow; // pop-up window
		public TracksDebugWindow TracksDebugWindow; // Control-Alt-F6
		public SignallingDebugWindow SignallingDebugWindow; // Control-Alt-F11 window
		public ComposeMessage ComposeMessageWindow; // Control-Alt-F11 window
		// Route Information
        public TileManager Tiles;
        public TileManager LOTiles;
        public ENVFile ENVFile;
        public SIGCFGFile SIGCFG;
        public TTypeDatFile TTypeDatFile;
        public bool MilepostUnitsMetric;
        // Cameras
        public Camera Camera; // Current camera
        public Camera AboveGroundCamera; // Previous camera for when automatically switching to cab.
        public CabCamera CabCamera; // Camera 1
        public HeadOutCamera HeadOutForwardCamera; // Camera 1+Up
        public HeadOutCamera HeadOutBackCamera; // Camera 2+Down
        public TrackingCamera FrontCamera; // Camera 2
        public TrackingCamera BackCamera; // Camera 3
        public TracksideCamera TracksideCamera; // Camera 4
        public PassengerCamera PassengerCamera; // Camera 5
        public BrakemanCamera BrakemanCamera; // Camera 6
        public List<FreeRoamCamera> FreeRoamCameraList = new List<FreeRoamCamera>();
        public FreeRoamCamera FreeRoamCamera // Camera 8
        {
            get
            { return FreeRoamCameraList[0]; }
            set {}
        }
        List<Camera> WellKnownCameras; // Providing Camera save functionality by GeorgeS

        public TrainCarViewer PlayerLocomotiveViewer = null;  // we are controlling this loco, or null if we aren't controlling any
        private MouseState originalMouseState;      // Current mouse coordinates.

        // This is the train we are controlling
        public TrainCar PlayerLocomotive { get { return Simulator.PlayerLocomotive; } set { Simulator.PlayerLocomotive = value; } }
        public Train PlayerTrain { get { if (PlayerLocomotive == null) return null; else return PlayerLocomotive.Train; } }

        // This is the train we are viewing
        public Train SelectedTrain { get; private set; }
        void CameraActivate()
        {
            if (Camera == null || !Camera.IsAvailable) //passenger camera may jump to a train without passenger view
                FrontCamera.Activate();
            else
                Camera.Activate();
        }

        // Mouse visibility by timer - GeorgeS
        private bool isMouseShouldVisible = false;
        private bool isMouseTimerVisible = false;
        private double MouseShownAtRealTime = 0;

        public bool SaveScreenshot;
        public bool SaveActivityThumbnail;
        public string SaveActivityFileStem;
        private BinaryReader inf;   // (In File) = Null indicates not resuming from a save.

		public bool DebugViewerEnabled = false;

        // MSTS cab views are images with aspect ratio 4:3.
        // OR can use cab views with other aspect ratios where these are available.
        // On screen with other aspect ratios (e.g. 16:9), two approaches are possible:
        //   1) stretch the width to fit the screen. This gives flattened controls, most noticeable with round dials.
        //   2) clip the image losing a slice off top and bottom.
        // Setting.Cab2DStretch controls the amount of stretch and clip. 0 is entirely clipped and 100 is entirely stretched.
        // No difference is seen on screens with 4:3 aspect ratio.
        // This adjustment assumes that the cab view is 4:3. Where the cab view matches the aspect ratio of the screen, use an adjustment of 100.
        public int CabHeightPixels;
        public int CabYOffsetPixels; // Note: Always -ve. Without it, the cab view is fixed to the top of the screen. -ve values pull it up the screen.

        public CommandLog Log { get; set; }
        public List<ICommand> ReplayCommandList = null;
        public bool CameraReplaySuspended { get; set; }
        public Camera SuspendedCamera { get; set; }

        /// <summary>
        /// True if a replay is in progress.
        /// Used to show some confirmations which are only valuable during replay (e.g. uncouple or resume activity).
        /// Also used to show the replay countdown in the HUD.
        /// </summary>
        public bool IsReplaying {
            get {
                if( ReplayCommandList != null ) {
                    return (ReplayCommandList.Count > 0);
                }
                return false;
            }
        }

        /// <summary>
        /// Finds time of last entry to set ReplayEndsAt and provide the Replay started message.
        /// </summary>
        void InitReplay() {
            if( ReplayCommandList != null ) {
                // Get time of last entry
                int lastEntry = ReplayCommandList.Count - 1;
                if( lastEntry >= 0 ) {
                    double lastTime = ReplayCommandList[lastEntry].Time;
                    Log.ReplayEndsAt = lastTime;
                    double duration = lastTime - Simulator.ClockTime;
                    MessagesWindow.AddMessage( String.Format( "Replay started: ending at {0} after {1}",
                        InfoDisplay.FormattedApproxTime( lastTime ),
                        InfoDisplay.FormattedTime( duration ) ),
                        3.0 );
                }
            }
        }

        /// <summary>
        /// Construct a viewer.  At this time background processes are not running
        /// and the graphics device is not ready to accept content.
        /// </summary>
        /// <param name="simulator"></param>
        [CallOnThread("Render")]
        public Viewer3D(Simulator simulator)
        {
            Simulator = simulator;
            Settings = simulator.Settings;

            WellKnownCameras = new List<Camera>();
            WellKnownCameras.Add(CabCamera = new CabCamera(this));
			WellKnownCameras.Add(FrontCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Front));
			WellKnownCameras.Add(BackCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Rear));
            WellKnownCameras.Add(PassengerCamera = new PassengerCamera(this));
            WellKnownCameras.Add(BrakemanCamera = new BrakemanCamera(this));
            WellKnownCameras.Add(HeadOutForwardCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Forward));
            WellKnownCameras.Add(HeadOutBackCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Backward));
            WellKnownCameras.Add(TracksideCamera = new TracksideCamera(this));
            WellKnownCameras.Add(FreeRoamCamera = new FreeRoamCamera( this, FrontCamera ) ); // Any existing camera will suffice to satisfy .Save() and .Restore()

            SharedMaterialManager.ViewingDistance = Settings.ViewingDistance = (int)Math.Min(Simulator.TRK.ORTRKData.MaxViewingDistance, Settings.ViewingDistance);

            Trace.Write(" ENV");
            ENVFile = new ENVFile(Simulator.RoutePath + @"\ENVFILES\" + Simulator.TRK.Tr_RouteFile.Environment.ENVFileName(Simulator.Season, Simulator.Weather));

            Trace.Write(" SIGCFG");
            SIGCFG = new SIGCFGFile(Simulator.RoutePath + @"\sigcfg.dat");

            Trace.Write(" TTYPE");
            TTypeDatFile = new TTypeDatFile(Simulator.RoutePath + @"\TTYPE.DAT");

            Tiles = new TileManager(Simulator.RoutePath + @"\TILES\");
            LOTiles = new TileManager(Simulator.RoutePath + @"\LO_TILES\");
            MilepostUnitsMetric = Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric;
        }

        public void Save(BinaryWriter outf, string fileStem)
        {
            outf.Write(Simulator.Trains.IndexOf(PlayerTrain));
            outf.Write(PlayerTrain.Cars.IndexOf(PlayerLocomotive));
            outf.Write(Simulator.Trains.IndexOf(SelectedTrain));

            WindowManager.Save(outf);

            outf.Write( WellKnownCameras.IndexOf( Camera ) );
            foreach( var camera in WellKnownCameras )
                camera.Save(outf);
            Camera.Save(outf);
            outf.Write( CabYOffsetPixels );

            // Set these so RenderFrame can use them when its thread gets control.
            SaveActivityFileStem = fileStem;
            SaveActivityThumbnail = true;
        }

        public void Restore(BinaryReader inf)
        {
            Train playerTrain = Simulator.Trains[inf.ReadInt32()];
            PlayerLocomotive = playerTrain.Cars[inf.ReadInt32()];
            SelectedTrain = Simulator.Trains[inf.ReadInt32()];

            WindowManager.Restore(inf);

            var cameraToRestore = inf.ReadInt32();
            foreach (var camera in WellKnownCameras)
                camera.Restore(inf);
            if (cameraToRestore == -1)
                new FreeRoamCamera(this, Camera).Activate();
            else
                WellKnownCameras[cameraToRestore].Activate();
            Camera.Restore(inf);
            CabYOffsetPixels = inf.ReadInt32();

        }

        [ThreadName( "Render" )]
        public void Run( BinaryReader inf )
        {
            this.inf = inf;
            LoaderProcess = new LoaderProcess( this );
            UpdaterProcess = new UpdaterProcess( this );
            RenderProcess = new RenderProcess( this );
            RenderProcess.Run();
        }

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background 
        /// processes haven't started yet.
        /// </summary>
        [CallOnThread("Render")]
        internal void Initialize()
        {
            GraphicsDevice = RenderProcess.GraphicsDevice;
            DisplaySize.X = GraphicsDevice.Viewport.Width;
            DisplaySize.Y = GraphicsDevice.Viewport.Height;

            AdjustCabHeight( DisplaySize.X, DisplaySize.Y );

            if (Settings.ShaderModel == 0)
                Settings.ShaderModel = GraphicsDevice.GraphicsDeviceCapabilities.PixelShaderVersion.Major;
            else if (Settings.ShaderModel < 2)
                Settings.ShaderModel = 2;
            else if (Settings.ShaderModel > 3)
                Settings.ShaderModel = 3;
            if (Settings.ShadowMapDistance == 0)
                Settings.ShadowMapDistance = Settings.ViewingDistance / 2;

            if (PlayerLocomotive == null) PlayerLocomotive = Simulator.InitialPlayerLocomotive();
            SelectedTrain = PlayerTrain;

            TextureManager = new SharedTextureManager(GraphicsDevice);
            MaterialManager = new SharedMaterialManager(this);
            ShapeManager = new SharedShapeManager(this);
            InfoDisplay = new InfoDisplay(this);

            WindowManager = new WindowManager(this);
            QuitWindow = new QuitWindow(WindowManager);
            MessagesWindow = new MessagesWindow(WindowManager);
            PauseWindow = new PauseWindow(WindowManager);
            HelpWindow = new HelpWindow(WindowManager);
            TrackMonitorWindow = new TrackMonitorWindow(WindowManager);
            HUDWindow = new HUDWindow(WindowManager);
            SwitchWindow = new SwitchWindow(WindowManager);
            TrainOperationsWindow = new TrainOperationsWindow(WindowManager);
            NextStationWindow = new NextStationWindow(WindowManager);
            CompassWindow = new CompassWindow(WindowManager);
            ActivityWindow = new ActivityWindow(WindowManager);
            TracksDebugWindow = new TracksDebugWindow(WindowManager);
            SignallingDebugWindow = new SignallingDebugWindow(WindowManager);
			ComposeMessageWindow = new ComposeMessage(WindowManager);
            WindowManager.Initialize();

            World = new World(this);

            SoundProcess = new SoundProcess(this);
            Simulator.Confirmer = new Confirmer(this, 1.5);

            // Now everything is ready to use, changed to saved values if available. 
            if (inf != null)
                Restore(inf);

            CameraActivate();

            // Prepare the world to be loaded and then load it from the correct thread for debugging/tracing purposes.
            // This ensures that a) we have all the required objects loaded when the 3D view first appears and b) that
            // all loading is performed on a single thread that we can handle in debugging and tracing.
            World.LoadPrep();
            LoaderProcess.StartLoad();
            LoaderProcess.WaitTillFinished();

            // MUST be after loading is done! (Or we try and load shapes on the main thread.)
            PlayerLocomotiveViewer = World.Trains.GetViewer(PlayerLocomotive);

            if (Settings.FullScreen)
                RenderProcess.ToggleFullScreen();

            SetCommandReceivers();
            InitReplay();
        }

        /// <summary>
        /// Each Command needs to know its Receiver so it can call a method of the Receiver to action the command. 
        /// The Receiver is a static property as all commands of the same class share the same Receiver
        /// and it needs to be set before the command is used.
        /// </summary>
        public void SetCommandReceivers() {
            ReverserCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            NotchedThrottleCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ContinuousThrottleCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            TrainBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            EngineBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            DynamicBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            InitializeBrakesCommand.Receiver = PlayerLocomotive.Train;
            EmergencyBrakesCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HandbrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BailOffCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            RetainersCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BrakeHoseConnectCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            if( PlayerLocomotive is MSTSSteamLocomotive ) {
                ContinuousReverserCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousInjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleInjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousBlowerCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousDamperCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousFiringRateCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleManualFiringCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleCylinderCocksCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                FireShovelfullCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
            }
            if( PlayerLocomotive is MSTSElectricLocomotive ) {
                PantographCommand.Receiver = (MSTSElectricLocomotive)PlayerLocomotive;
            }
            SanderCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            AlerterCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HornCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BellCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleCabLightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleWipersCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HeadlightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ChangeCabCommand.Receiver = this;
            ToggleDoorsLeftCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleDoorsRightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleMirrorsCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleSwitchAheadCommand.Receiver = this;
            ToggleSwitchBehindCommand.Receiver = this;
            ToggleAnySwitchCommand.Receiver = this;
            UncoupleCommand.Receiver = this;
            SaveScreenshotCommand.Receiver = this;
            ActivityCommand.Receiver = ActivityWindow;  // and therefore shared by all sub-classes
            UseCameraCommand.Receiver = this;
            MoveCameraCommand.Receiver = this;
            SaveCommand.Receiver = this;
        }

        public void ChangeToPreviousFreeRoamCamera()
        {
            if (Camera == FreeRoamCamera)
            {
                // If 8 is the current camera, rotate the list and then activate a different camera.
                RotateFreeRoamCameraList();
                FreeRoamCamera.Activate();
            }
            else
            {
                FreeRoamCamera.Activate();
                RotateFreeRoamCameraList();
            }
        }

        void RotateFreeRoamCameraList()
        {
            // Rotate list moving 1 to 0 etc. (by adding 0 to end, then removing 0)
            FreeRoamCameraList.Add(FreeRoamCamera);
            FreeRoamCameraList.RemoveAt(0);
        }

        public void AdjustCabHeight(int windowWidth, int windowHeight) {
            int MSTSCabHeightPixels = windowWidth * 3 / 4; // MSTS cab views are designed for 4:3 aspect ratio.
            // For wider screens (e.g. 16:9), the height of the cab view before adjustment exceeds the height of the display.
            // The user can decide how much of this excess to keep. Setting of 0 keeps all the excess and 100 keeps none.

            // <CJComment> This scheme treats all cab views the same and assumes that they have a 4:3 aspect ratio.
            // For a cab view with a different aspect ratio (e.g. designed for a 16:9 screen), use a setting of 100 which
            // will disable this feature. A smarter scheme would discover the aspect ratio of the cab view and adjust
            // appropriately. </CJComment>

            int CabExceedsDisplay;
            if( ((float)windowWidth / windowHeight) > (4.0 / 3) ) {
                // screen is wide-screen, so can choose between scroll or stretch
                CabExceedsDisplay = (int)((MSTSCabHeightPixels - windowHeight) * ((100 - Settings.Cab2DStretch) / 100f));
            } else {
                // scroll not practical, so stretch instead
                CabExceedsDisplay = 0;
            }
            CabHeightPixels = windowHeight + CabExceedsDisplay;
            CabYOffsetPixels = -CabExceedsDisplay / 2; // Initial value is halfway. User can adjust with arrow keys.
            if (CabCamera.IsAvailable)
                CabCamera.InitialiseRotation(Simulator.PlayerLocomotive);
        }

        string adapterDescription;
        public string AdapterDescription { get { return adapterDescription; } }

        uint adapterMemory = 0;
        public uint AdapterMemory { get { return adapterMemory; } }

        [CallOnThread("Updater")]
        internal void UpdateAdapterInformation(GraphicsAdapter graphicsAdapter)
        {
            adapterDescription = graphicsAdapter.Description;
            try
            {
                // Note that we might find multiple adapters with the same
                // description; however, the chance of such adapters not having
                // the same amount of video memory is very slim.
                foreach (ManagementObject videoController in new ManagementClass("Win32_VideoController").GetInstances())
                    if (((string)videoController["Description"] == adapterDescription) && (videoController["AdapterRAM"] != null))
                        adapterMemory = (uint)videoController["AdapterRAM"];
            }
            catch (ManagementException error)
            {
                Trace.WriteLine(error);
            }
        }

        internal void ProcessReportError(Exception error)
        {
            // Log the error first in case we're burning.
            Trace.WriteLine(new FatalException(error));
            // Stop the world!
            RenderProcess.Exit();
            // Show the user that it's all gone horribly wrong.
            if (Settings.ShowErrorDialogs)
                System.Windows.Forms.MessageBox.Show(error.ToString());
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            World.Load();
            WindowManager.Load();
        }

        [CallOnThread("Updater")]
        public void Update(float elapsedRealTime, RenderFrame frame)
        {
            RealTime += elapsedRealTime;
            var elapsedTime = new ElapsedTime(Simulator.GetElapsedClockSeconds(elapsedRealTime), elapsedRealTime);

            Simulator.Update(elapsedTime.ClockSeconds);
            HandleUserInput(elapsedTime);
            UserInput.Handled();

            if( ReplayCommandList != null ) {
                Log.Update( ReplayCommandList );

                if( Log.PauseState == ReplayPauseState.Due ) {
                    if( Simulator.Settings.ReplayPauseBeforeEnd ) {
                        // Reveal Quit Menu
                        QuitWindow.Visible = Simulator.Paused = !QuitWindow.Visible;
                        Log.PauseState = ReplayPauseState.During;
                    } else {
                        Log.PauseState = ReplayPauseState.Done;
                    }
                }
            }
            if( Log.ReplayComplete ) {
                MessagesWindow.AddMessage( "Replay complete", 2 );
                Log.ReplayComplete = false;
            }

            if (ScreenHasChanged())
            {
                Camera.ScreenChanged();
                RenderProcess.InitializeShadowMapLocations(RenderProcess.Viewer);
            }

            // Update camera first...
            Camera.Update(elapsedTime);

            // No above camera means we're allowed to auto-switch to cab view.
            if ((AboveGroundCamera == null) && Camera.IsUnderground)
            {
                AboveGroundCamera = Camera;
                bool ViewingPlayer = true;
                
                if (Camera.AttachedCar!=null) ViewingPlayer = Camera.AttachedCar.Train == Simulator.PlayerLocomotive.Train;

                if (Simulator.PlayerLocomotive.HasFrontCab && ViewingPlayer)
                {
                    CabCamera.Activate();
                }
                else
                {
                    Simulator.Confirmer.Warning("Cab view not available");
                }
            }
			else if (AboveGroundCamera != null 
                && Camera.AttachedCar != null 
                && Camera.AttachedCar.Train == Simulator.PlayerLocomotive.Train)
			{
				// Make sure to keep the old camera updated...
				AboveGroundCamera.Update(elapsedTime);
				// ...so we can tell when to come back to it.
				if (!AboveGroundCamera.IsUnderground)
				{
					// But only if the user hasn't selected another camera!
					if (Camera == CabCamera)
						AboveGroundCamera.Activate();
					AboveGroundCamera = null;
				}
			}

            World.Update(elapsedTime);

            // Every 250ms, check for new things to load and kick off the loader.
            if (LastLoadRealTime + 0.25 < RealTime && LoaderProcess.Finished)
            {
                LastLoadRealTime = RealTime;
                World.LoadPrep();
                LoaderProcess.StartLoad();
            }

            Camera.PrepareFrame(frame, elapsedTime);
            frame.PrepareFrame(elapsedTime);
            World.PrepareFrame(frame, elapsedTime);
            InfoDisplay.PrepareFrame(frame, elapsedTime);
            // TODO: This is not correct. The ActivityWindow's PrepareFrame is already called by the WindowManager!
            if (Simulator.ActivityRun != null) ActivityWindow.PrepareFrame(elapsedTime, true);

            WindowManager.PrepareFrame(frame, elapsedTime);
        }
        double LastLoadRealTime = 0;

        [CallOnThread("Updater")]
        void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommands.CameraReset))
                Camera.Reset();

            Camera.HandleUserInput(elapsedTime);

            if (PlayerLocomotiveViewer != null)
                PlayerLocomotiveViewer.HandleUserInput(elapsedTime);

            InfoDisplay.HandleUserInput(elapsedTime);
            WindowManager.HandleUserInput(elapsedTime);

            // Check for game control keys
			if (MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.GameMultiPlayerTexting))
			{
				if (ComposeMessageWindow == null) ComposeMessageWindow = new ComposeMessage(WindowManager);
				ComposeMessageWindow.InitMessage();
			}
            if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.GamePauseMenu)) { QuitWindow.Visible = Simulator.Paused = !QuitWindow.Visible; }
			if (MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.GamePauseMenu)) { if (Simulator.Confirmer != null) Simulator.Confirmer.Information("In MP, use Alt-F4 to quit directly"); }

            if (UserInput.IsPressed(UserCommands.GameFullscreen)) { RenderProcess.ToggleFullScreen(); }
			if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.GamePause)) Simulator.Paused = !Simulator.Paused;
			if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.DebugSpeedUp))
			{
                Simulator.GameSpeed *= 1.5f;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Increase, Simulator.GameSpeed * 100 );
            }
			if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommands.DebugSpeedDown))
			{
                Simulator.GameSpeed /= 1.5f;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Decrease, Simulator.GameSpeed * 100 );
            }
            if( UserInput.IsPressed( UserCommands.DebugSpeedReset ) ) {
                Simulator.GameSpeed = 1;
                Simulator.Confirmer.ConfirmWithPerCent( CabControl.SimulationSpeed, CabSetting.Off, Simulator.GameSpeed * 100 );
            }
            if (UserInput.IsPressed(UserCommands.GameSave)) { Program.Save(); }
            if (UserInput.IsPressed(UserCommands.DisplayHelpWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) HelpWindow.TabAction(); else HelpWindow.Visible = !HelpWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayTrackMonitorWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TrackMonitorWindow.TabAction(); else TrackMonitorWindow.Visible = !TrackMonitorWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayHUD)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) HUDWindow.TabAction(); else HUDWindow.Visible = !HUDWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplaySwitchWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) SwitchWindow.TabAction(); else SwitchWindow.Visible = !SwitchWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayTrainOperationsWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TrainOperationsWindow.TabAction(); else TrainOperationsWindow.Visible = !TrainOperationsWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayNextStationWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) NextStationWindow.TabAction(); else NextStationWindow.Visible = !NextStationWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DisplayCompassWindow)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) CompassWindow.TabAction(); else CompassWindow.Visible = !CompassWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DebugTracks)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) TracksDebugWindow.TabAction(); else TracksDebugWindow.Visible = !TracksDebugWindow.Visible;
            if (UserInput.IsPressed(UserCommands.DebugSignalling)) if (UserInput.IsDown(UserCommands.DisplayNextWindowTab)) SignallingDebugWindow.TabAction(); else SignallingDebugWindow.Visible = !SignallingDebugWindow.Visible;

            if (UserInput.IsPressed(UserCommands.GameChangeCab))
            {
                if (PlayerLocomotive.ThrottlePercent >= 1 
                    || Math.Abs(PlayerLocomotive.SpeedMpS) > 1
                    || !IsReverserInNeutral(PlayerLocomotive))
                {
                    Simulator.Confirmer.Warning(CabControl.ChangeCab, CabSetting.Warn2);
                } 
                else
                {
                    new ChangeCabCommand(Log);
                }
            }

            if (UserInput.IsPressed(UserCommands.CameraCab))
            {
                if (CabCamera.IsAvailable)
                {
                    new UseCabCameraCommand(Log);
                }
                else
                {
                    Simulator.Confirmer.Warning("Cab view not available");
                }
            }
            if( UserInput.IsPressed( UserCommands.CameraOutsideFront ) ) {
                CheckReplaying();
                new UseFrontCameraCommand( Log );
            }
            if( UserInput.IsPressed( UserCommands.CameraOutsideRear ) ) {
                CheckReplaying();
                new UseBackCameraCommand( Log );
            }
            if( UserInput.IsPressed( UserCommands.CameraJumpingTrains ) ) RandomSelectTrain(); //hit Alt-9 key, random selected train to have 2 and 3 camera attached to

            if (UserInput.IsPressed(UserCommands.CameraVibrate))
            {
                Program.Simulator.CarVibrating = (Program.Simulator.CarVibrating + 1) % 4;
                Simulator.Confirmer.Message(ConfirmLevel.Information, "Vibrating at level " + Program.Simulator.CarVibrating);
                Settings.CarVibratingLevel = Program.Simulator.CarVibrating;
                Settings.Save("CarVibratingLevel");
            }

            //hit 9 key, get back to player train
            if( UserInput.IsPressed( UserCommands.CameraJumpBackPlayer ) ) {
                SelectedTrain = PlayerTrain;
                CameraActivate();
            }
            if( UserInput.IsPressed( UserCommands.CameraTrackside ) ) {
                CheckReplaying();
                new UseTracksideCameraCommand( Log );
            }
            // Could add warning if PassengerCamera not available.
            if( UserInput.IsPressed( UserCommands.CameraPassenger ) && PassengerCamera.IsAvailable ) {
                CheckReplaying();
                new UsePassengerCameraCommand( Log );
            }
            if( UserInput.IsPressed( UserCommands.CameraBrakeman ) ) {
                CheckReplaying();
                new UseBrakemanCameraCommand( Log );
            }
            if( UserInput.IsPressed( UserCommands.CameraFree ) ) {
                CheckReplaying();
                new UseFreeRoamCameraCommand( Log );
                Simulator.Confirmer.Message(ConfirmLevel.None, String.Format(
                    "{0} viewpoints stored. Use Shift+8 to restore viewpoints.", FreeRoamCameraList.Count));
            }
            if (UserInput.IsPressed(UserCommands.CameraPreviousFree))
            {
                if (FreeRoamCameraList.Count > 0)
                {
                    CheckReplaying();
                    new UsePreviousFreeRoamCameraCommand(Log);
                }
            } 
            if (UserInput.IsPressed(UserCommands.CameraHeadOutForward) && HeadOutForwardCamera.IsAvailable)
            {
                CheckReplaying();
                new UseHeadOutForwardCameraCommand( Log );
            }
            if( UserInput.IsPressed( UserCommands.CameraHeadOutBackward ) && HeadOutBackCamera.IsAvailable ) {
                CheckReplaying();
                new UseHeadOutBackCameraCommand( Log );
            }
#if !NEW_SIGNALLING
            if( UserInput.IsPressed( UserCommands.GameSwitchAhead ) )
                if( Simulator.SwitchTrackAhead( PlayerTrain ) )
                    new ToggleSwitchAheadCommand( Log );
            if( UserInput.IsPressed( UserCommands.GameSwitchBehind ) )
                if( Simulator.SwitchTrackBehind( PlayerTrain ) )
                    new ToggleSwitchBehindCommand( Log );
#else
            if (UserInput.IsPressed(UserCommands.GameSwitchAhead))
            {
                if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL || PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                    new ToggleSwitchAheadCommand(Log);
            }
            if (UserInput.IsPressed(UserCommands.GameSwitchBehind))
            {
                if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL || PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                    new ToggleSwitchBehindCommand(Log);
            }
#endif

#if !NEW_SIGNALLING
			if (UserInput.IsPressed(UserCommands.DebugResetSignal))
			{
				if (MPManager.IsMultiPlayer() && !MPManager.IsServer()) MPManager.Instance().RequestSignalReset();
				else PlayerTrain.ResetSignal(true);
			}

#else
            if (UserInput.IsPressed(UserCommands.GameResetSignalForward)) PlayerTrain.RequestSignalPermission(Direction.Forward);
			if (UserInput.IsPressed(UserCommands.GameResetSignalBackward)) PlayerTrain.RequestSignalPermission(Direction.Reverse);
			if (UserInput.IsPressed(UserCommands.GameSwitchManualMode)) PlayerTrain.RequestToggleManualMode();
			if (UserInput.IsPressed(UserCommands.GameShuntLockAOn)) PlayerTrain.RequestShuntLock(1,1);
			if (UserInput.IsPressed(UserCommands.GameShuntLockAOff)) PlayerTrain.RequestShuntLock(1,0);
			if (UserInput.IsPressed(UserCommands.GameShuntLockBOn)) PlayerTrain.RequestShuntLock(2,1);
			if (UserInput.IsPressed(UserCommands.GameShuntLockBOff)) PlayerTrain.RequestShuntLock(2,0);
#endif

			if (UserInput.IsPressed(UserCommands.GameMultiPlayerDispatcher)) { DebugViewerEnabled = !DebugViewerEnabled; return; }
#if !NEW_SIGNALLING
			if (UserInput.IsPressed(UserCommands.GameSwitchPicked) && (!MultiPlayer.MPManager.IsMultiPlayer() || MultiPlayer.MPManager.IsServer()))
			{
				if (Program.DebugViewer != null && Program.DebugViewer.Enabled && Program.DebugViewer.switchPickedItem != null)
				{

					TrJunctionNode nextSwitchTrack = Program.DebugViewer.switchPickedItem.Item.TrJunctionNode;
					if (nextSwitchTrack != null && !Simulator.SwitchIsOccupied(nextSwitchTrack))
					{
						if (nextSwitchTrack.SelectedRoute == 0)
							nextSwitchTrack.SelectedRoute = 1;
						else
							nextSwitchTrack.SelectedRoute = 0;
						//if (MPManager.IsMultiPlayer() && MPManager.IsServer()) MPManager.BroadCast((new MultiPlayer.MSGSwitchStatus()).ToString());

					}
				}
				//Program.DebugViewer.switchPickedItemHandled = true;
            }

			if (UserInput.IsPressed(UserCommands.GameSignalPicked) && (!MultiPlayer.MPManager.IsMultiPlayer() || MultiPlayer.MPManager.IsServer()))
			{
				if (Program.DebugViewer != null && Program.DebugViewer.Enabled && Program.DebugViewer.signalPickedItem != null)
				{

					var signal = Program.DebugViewer.signalPickedItem.Signal;
					if (signal != null)
					{
						if (signal.forcedTime > 0)
						{//a signal was set by the dispatcher before, so will need to reset it so that system will control it.
							signal.canUpdate = true;
							signal.forcedTime = 0;
						}
						else if (Program.DebugViewer.signalPickedItem.IsProceed <= 1)
						{
							signal.enabled = false;
							signal.canUpdate = false;
							foreach (var head in signal.SignalHeads)
							{
								head.SetMostRestrictiveAspect();
								head.Update();
							}
							signal.forcedTime = Simulator.GameTime;
						}
						else
						{
							signal.canUpdate = false;
							signal.enabled = true; //force it to be green,
							//signal.
							foreach (var head in signal.SignalHeads)
							{
								//first try to set as approach, if not defined, set as the least
								var drawstate1 = head.def_draw_state(SignalHead.SIGASP.APPROACH_1);
								var drawstate2 = head.def_draw_state(SignalHead.SIGASP.APPROACH_2);
								var drawstate3 = head.def_draw_state(SignalHead.SIGASP.APPROACH_3);
								if (drawstate1 > 0) { head.state = SignalHead.SIGASP.APPROACH_1; }
								else if (drawstate2 > 0) { head.state = SignalHead.SIGASP.APPROACH_2; }
								else if (drawstate3 > 0) { head.state = SignalHead.SIGASP.APPROACH_3; }
								else head.SetLeastRestrictiveAspect();
								//head.state = SignalHead.SIGASP.CLEAR_2;
								head.draw_state = head.def_draw_state(head.state);
								//head.Update();
							}
							signal.forcedTime = Simulator.GameTime;
						}
						//if (MPManager.IsMultiPlayer() && MPManager.IsServer()) MPManager.BroadCast((new MultiPlayer.MSGSignalStatus()).ToString());

					}
				}
				//Program.DebugViewer.signalPickedItemHandled = true;
			}
#endif

			if (UserInput.IsPressed(UserCommands.CameraJumpSeeSwitch))
			{
				if (Program.DebugViewer != null && Program.DebugViewer.Enabled && (Program.DebugViewer.switchPickedItem != null || Program.DebugViewer.signalPickedItem != null))
				{
					WorldLocation wos = null;
					try
					{
						if (Program.DebugViewer.switchPickedItem != null)
						{
							TrJunctionNode nextSwitchTrack = Program.DebugViewer.switchPickedItem.Item.TrJunctionNode;
							wos = new WorldLocation(nextSwitchTrack.TN.UiD.TileX, nextSwitchTrack.TN.UiD.TileZ, nextSwitchTrack.TN.UiD.X, nextSwitchTrack.TN.UiD.Y + 8, nextSwitchTrack.TN.UiD.Z);
						}
						else
						{
							var s = Program.DebugViewer.signalPickedItem.Item;
							wos = new WorldLocation(s.TileX, s.TileZ, s.X, s.Y + 8, s.Z);
						}
                        if (FreeRoamCameraList.Count == 0)
                        {
                            new UseFreeRoamCameraCommand(Log);
                        }
						FreeRoamCamera.SetLocation(wos);
						//FreeRoamCamera
						FreeRoamCamera.Activate();
					}
					catch { }


				}
			}

			//in the dispatcher window, when one clicks a train and "See in Game", will jump to see that train
			if (Program.DebugViewer != null && Program.DebugViewer.ClickedTrain == true)
			{
				Program.DebugViewer.ClickedTrain = false;
				Train old = SelectedTrain;
				if (SelectedTrain != Program.DebugViewer.PickedTrain)
				{
					SelectedTrain = Program.DebugViewer.PickedTrain;

					if (SelectedTrain.Cars == null || SelectedTrain.Cars.Count == 0) SelectedTrain = PlayerTrain;

                    CameraActivate();
                }
			}

            if (!Simulator.Paused && UserInput.IsDown(UserCommands.GameSwitchWithMouse))
            {
                isMouseShouldVisible = true;
                if (UserInput.MouseState.LeftButton == ButtonState.Pressed && UserInput.Changed)
                {
                    TryThrowSwitchAt();
                    UserInput.Handled();
                }
            }
            else if (!Simulator.Paused && UserInput.IsDown(UserCommands.GameUncoupleWithMouse))
            {
                isMouseShouldVisible = true;
                if (UserInput.MouseState.LeftButton == ButtonState.Pressed && UserInput.Changed)
                {
                    TryUncoupleAt();
                    UserInput.Handled();
                }
            }
            else
            {
                isMouseShouldVisible = false;
            }

            RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;

            if (UserInput.RDState != null)
                UserInput.RDState.Handled();

            MouseState currentMouseState = Mouse.GetState();

            // Handling mouse movement and timing - GeorgeS
            if (currentMouseState.X != originalMouseState.X ||
                currentMouseState.Y != originalMouseState.Y)
            {
                isMouseTimerVisible = true;
                MouseShownAtRealTime = RealTime;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }
            else if (isMouseTimerVisible && MouseShownAtRealTime + .5 < RealTime)
            {
                isMouseTimerVisible = false;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }

            originalMouseState = currentMouseState;
        }
       
        private bool IsReverserInNeutral(TrainCar car)
        {
            // Diesel and electric locos have a Reverser lever and,
            // in the neutral position, direction == N
            return car.Direction == Direction.N
            // Steam locos never have direction == N, so check for setting close to zero.
            || Math.Abs(car.Train.MUReverserPercent) <= 1;
        }
        /// <summary>
        /// If the player changes the camera during replay, then further replay of the camera is suspended.
        /// The player's camera commands will be recorded instead of the replay camera commands.
        /// Replay and recording of non-camera commands such as controls continues.
        /// </summary>
        public void CheckReplaying() {
            if( IsReplaying ) {
                if( !CameraReplaySuspended ) {
                    CameraReplaySuspended = true;
                    SuspendedCamera = Camera;
                    Simulator.Confirmer.Confirm( CabControl.Replay, CabSetting.Warn1 );
                }
            }
        }

        /// <summary>
        /// Replay of the camera is not resumed until the player opens the Quit Menu and then presses Esc to unpause the simulator.
        /// </summary>
        public void ResumeReplaying() {
            CameraReplaySuspended = false;
            if (SuspendedCamera != null)
                SuspendedCamera.Activate();
        }

        public void ChangeCab() {
            if (!Simulator.PlayerLocomotive.Train.IsChangeCabAvailable()) return;

            Simulator.PlayerLocomotive = Simulator.PlayerLocomotive.Train.GetNextCab();
            PlayerLocomotiveViewer = World.Trains.GetViewer(Simulator.PlayerLocomotive);
            Camera.Activate(); // If you need anything else here the cameras should check for it.
            SetCommandReceivers();
            if (MPManager.IsMultiPlayer()) 
                MPManager.LocoChange(Simulator.PlayerLocomotive.Train, Simulator.PlayerLocomotive);
            Simulator.Confirmer.Confirm(CabControl.ChangeCab, CabSetting.On);
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            InfoDisplay.Mark();
            WindowManager.Mark();
        }

        /// <summary>
        /// Call this method to request a normal shutdown of the game.
        /// </summary>
        public void Stop()
        {
            // Do not put shutdown code in here! Use Viewer3D.Terminate() instead.
            RenderProcess.Stop();
        }

        [CallOnThread("Render")]
        internal void Terminate()
        {
            InfoDisplay.Terminate();
            SoundProcess.RemoveAllSources();
        }

        /// <summary>
        /// Return true if the screen has changed dimensions
        /// </summary>
        /// <returns></returns>
        bool ScreenHasChanged()
        {
            if (RenderProcess.GraphicsDeviceManager.IsFullScreen != isFullScreen)
            {
                isFullScreen = RenderProcess.GraphicsDeviceManager.IsFullScreen;
                return true;
            }
            return false;
        }

		private int trainCount = 0;
		void RandomSelectTrain()
		{
			Train old = SelectedTrain;
			try
			{
				SortedList<double, Train> users = new SortedList<double, Train>();
				foreach (var t in Simulator.Trains)
				{
					if (t == null || t.Cars == null || t.Cars.Count == 0) continue;
					var d = WorldLocation.GetDistanceSquared(t.RearTDBTraveller.WorldLocation, PlayerTrain.RearTDBTraveller.WorldLocation);
					users.Add(d + Program.Random.NextDouble(), t);
				}
				trainCount++;
				if (trainCount >= users.Count) trainCount = 0;

				SelectedTrain = users.ElementAt(trainCount).Value;
				if (SelectedTrain.Cars == null || SelectedTrain.Cars.Count == 0) SelectedTrain = PlayerTrain;

				//if (SelectedTrain.LeadLocomotive == null) SelectedTrain.LeadNextLocomotive();
				//if (SelectedTrain.LeadLocomotive != null) { PlayerLocomotive = SelectedTrain.LeadLocomotive; PlayerLocomotiveViewer = World.Trains.GetViewer(Simulator.PlayerLocomotive); }
				
			}
			catch 
			{
				SelectedTrain = PlayerTrain;
			}
            CameraActivate();
		}
        bool isFullScreen = false;

        /// <summary>
        /// The user has left-clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        void TryUncoupleAt()
        {
            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = UserInput.FarPoint - UserInput.NearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(UserInput.NearPoint, direction);

            // check each car
            Traveller traveller = new Traveller(PlayerTrain.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
            int carNo = 0;
            foreach (TrainCar car in PlayerTrain.Cars)
            {
                float d = (car.CouplerSlackM + car.GetCouplerZeroLengthM()) / 2;
                traveller.Move(car.Length + d);

                Vector3 xnaCenter = Camera.XNALocation(traveller.WorldLocation);
                float radius = 2f;  // 2 meter click range
                BoundingSphere boundingSphere = new BoundingSphere(xnaCenter, radius);

                if (null != pickRay.Intersects(boundingSphere))
                {
                    new UncoupleCommand( Log, carNo );
                    break;
                }
                traveller.Move(d);
                carNo++;
            }
        }

        /// <summary>
        /// The user has left-clicked with Alt key pressed.   
        /// If the mouse was over a switch, then toggle the switch.
        /// No action if toggling blocks the player loco's path.
        /// </summary>
        void TryThrowSwitchAt()
        {
#if !NEW_SIGNALLING
            TrJunctionNode bestNode = null;
            int index = 0;
#else
            TrackNode bestTn = null;
#endif
            float bestD = 10;
            // check each switch
            for (int j = 0; j < Simulator.TDB.TrackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = Simulator.TDB.TrackDB.TrackNodes[j];
                if (tn != null && tn.TrJunctionNode != null)
                {

                    Vector3 xnaCenter = Camera.XNALocation(new WorldLocation(tn.UiD.TileX, tn.UiD.TileZ, tn.UiD.X, tn.UiD.Y, tn.UiD.Z));
                    float d = ORTSMath.LineSegmentDistanceSq(xnaCenter, UserInput.NearPoint, UserInput.FarPoint);

#if NEW_SIGNALLING
                    if (bestD > d)
                    {
                        bestTn = tn;
                        bestD = d;
                    }
        	    }
            }
            if (bestTn != null)
            {
                new ToggleAnySwitchCommand(Log, bestTn.TCCrossReference[0].CrossRefIndex);
            }
#else
                    if (bestD > d && !Simulator.SwitchIsOccupied(j))
                    {
                        bestNode = tn.TrJunctionNode;
                        bestD = d;
                        index = j;
                    }
                }
            }
            if (bestNode != null)
            {
                new ToggleAnySwitchCommand(Log, index);
            }
#endif
        }

#if NEW_SIGNALLING
        public void ToggleAnySwitch(int index)
        {
            Simulator.Signals.RequestSetSwitch(index);
        }
#else
        public void ToggleAnySwitch(int index)
        {
            TrackNode tn = Simulator.TDB.TrackDB.TrackNodes[index];
            TrJunctionNode bestNode = tn.TrJunctionNode;
            bestNode.SelectedRoute = 1 - bestNode.SelectedRoute;
        }
#endif
        public void ToggleSwitchAhead()
        {
#if NEW_SIGNALLING
            if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            {
                PlayerTrain.ProcessRequestManualSetSwitch(Direction.Forward);
            }
            else if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
            {
                PlayerTrain.ProcessRequestExplorerSetSwitch(Direction.Forward);
            }
#else
            Simulator.ToggleSwitchTrackAhead(PlayerTrain);
#endif
        }

        public void ToggleSwitchBehind()
        {
#if NEW_SIGNALLING
            if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            {
                PlayerTrain.ProcessRequestManualSetSwitch(Direction.Reverse);
            }
            else if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
            {
                PlayerTrain.ProcessRequestExplorerSetSwitch(Direction.Reverse);
            }
#else
            Simulator.ToggleSwitchTrackBehind(PlayerTrain);
#endif
        }
        
        internal void UncoupleBehind(int carPosition)
        {
            Simulator.UncoupleBehind(carPosition);
            //make the camera train to be the player train
            if (PlayerLocomotive != null && PlayerLocomotive.Train != null) this.SelectedTrain = PlayerLocomotive.Train;
            CameraActivate();
        }
    }
}
