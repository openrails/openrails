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
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using IrrKlang;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;

namespace ORTS
{
	public enum BoolSettings
	{
		DynamicShadows,
		FullScreen,
		Precipitation,
		Profiling,
		VerticalSync,
		WindowGlass,
		Wire,
	}

	public enum IntSettings
	{
		WorldObjectDensity,
		SoundDetailLevel,
		ViewingDistance,
	}

	public class Viewer3D
    {
        // User setups.
		public readonly bool[] SettingsBool = InitialiseSettingsBool();
		private static bool[] InitialiseSettingsBool()
		{
			var rv = new bool[Enum.GetNames(typeof(BoolSettings)).Length];
			rv[(int)BoolSettings.DynamicShadows] = false;
			rv[(int)BoolSettings.FullScreen] = false;
			rv[(int)BoolSettings.Precipitation] = false;
			rv[(int)BoolSettings.Profiling] = false;
			rv[(int)BoolSettings.VerticalSync] = false;
			rv[(int)BoolSettings.WindowGlass] = false;
			rv[(int)BoolSettings.Wire] = false;
			return rv;
		}
		public readonly int[] SettingsInt = InitialiseSettingsInt();
		private static int[] InitialiseSettingsInt()
		{
			var rv = new int[Enum.GetNames(typeof(IntSettings)).Length];
			rv[(int)IntSettings.WorldObjectDensity] = 10;
			rv[(int)IntSettings.SoundDetailLevel] = 5;
			rv[(int)IntSettings.ViewingDistance] = 2000;
			return rv;
		}
        public Vector2 WindowSize = new Vector2(1024, 768);
		// Multi-threaded processes
        public UpdaterProcess UpdaterProcess = null;
        public LoaderProcess LoaderProcess;
        public RenderProcess RenderProcess;
        // Access to the XNA Game class
        public GraphicsDeviceManager GDM;  
        public GraphicsDevice GraphicsDevice;
		public Vector2 DisplaySize;
        // Components
        public Simulator Simulator;
        InfoDisplay InfoDisplay;
		public WindowManager WindowManager = null;
		public TrackMonitorWindow TrackMonitorWindow; // F4 window
		public SwitchWindow SwitchWindow; // F8 window
		public TrainOperationsWindow TrainOperationsWindow; // F9 window
		public NextStationWindow NextStationWindow; // F10 window
		public CompassWindow CompassWindow; // 0 window
        public SkyDrawer SkyDrawer;
        public PrecipDrawer PrecipDrawer = null;
        public WireDrawer WireDrawer = null;
        public LightGlowDrawer LightGlowDrawer;
        public WeatherControl weatherControl;
        TerrainDrawer TerrainDrawer;
        public SceneryDrawer SceneryDrawer;
        public TrainDrawer TrainDrawer;
        public ISoundEngine SoundEngine = null;  // IrrKlang Sound Device
        public SoundSource IngameSounds = null;  // By GeorgeS
        public WorldSounds WorldSounds = null;   // By GeorgeS
        // Route Information
        public Tiles Tiles = null;
        public ENVFile ENVFile;
        public TTypeDatFile TTypeDatFile;
		public bool MilepostUnitsMetric;
        // Cameras
        public Camera Camera; // Current camera
		Camera AboveGroundCamera; // Previous camera for when automatically switching to cab.
		private CabCamera CabCamera; // Camera 1
		private HeadOutCamera HeadOutFwdCamera; // Camera 1+Up
		private HeadOutCamera HeadOutBackCamera; // Camera 2+Down
		private TrackingCamera FrontCamera; // Camera 2
		private TrackingCamera BackCamera; // Camera 3
		private TracksideCamera TracksideCamera; // Camera 4
		private PassengerCamera PassengerCamera; // Camera 5
		private BrakemanCamera BrakemanCamera; // Camera 6
        private List<Camera> WellKnownCameras; // Providing Camera save functionality by GeorgeS
        private int CameraToRestore = 1; // Providing Camera save functionality by GeorgeS
        public TrainCarViewer PlayerLocomotiveViewer = null;  // we are controlling this loco, or null if we aren't controlling any
        private MouseState originalMouseState;      // Current mouse coordinates.

        // This is the train we are controlling
        public TrainCar PlayerLocomotive { get { return Simulator.PlayerLocomotive; } set { Simulator.PlayerLocomotive = value; } }
        public Train PlayerTrain { get { if (PlayerLocomotive == null) return null; else return PlayerLocomotive.Train; } }

        // Mouse visibility by timer - GeorgeS
        private bool isMouseShouldVisible = false;
        private bool isMouseTimerVisible = false;
        private double MouseShownAt = 0;

		public Profiler RenderProfiler;
		public Profiler UpdaterProfiler;
		public Profiler LoaderProfiler;

		/// <summary>
        /// Construct a viewer.  At this time background processes are not running
        /// and the graphics device is not ready to accept content.
        /// </summary>
        /// <param name="simulator"></param>
		public Viewer3D(Simulator simulator)
		{
			Simulator = simulator;
		}

        /// <summary>
        /// Save game
        /// </summary>
		public void Save(BinaryWriter outf)
		{
			outf.Write(Simulator.Trains.IndexOf(PlayerTrain));
			outf.Write(PlayerTrain.Cars.IndexOf(PlayerLocomotive));
			// Saving Camera by GeorgeS
			CameraToRestore = WellKnownCameras.IndexOf(Camera);
			outf.Write(CameraToRestore);
		}

        /// <summary>
        /// Restore after game resumes
        /// </summary>
		public void Restore(BinaryReader inf)
		{
			Train playerTrain = Simulator.Trains[inf.ReadInt32()];
			PlayerLocomotive = playerTrain.Cars[inf.ReadInt32()];
			// Restoring Camera part I by GeorgeS
			CameraToRestore = inf.ReadInt32();
		}

        /// <summary>
        /// Setup the game settings provided by the user in the main menu screen.
        /// </summary>
        public void LoadUserSettings()
        {
            // Restore retained settings
            string strWindowSize = "1024x768";

			try
			{
				RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
				if (RK != null)
				{
					foreach (int key in Enum.GetValues(typeof(BoolSettings)))
						SettingsBool[key] = (1 == (int)RK.GetValue(Enum.GetName(typeof(BoolSettings), key), SettingsBool[key] ? 1 : 0));
					foreach (int key in Enum.GetValues(typeof(IntSettings)))
						SettingsInt[key] = (int)RK.GetValue(Enum.GetName(typeof(IntSettings), key), SettingsInt[key]);

					strWindowSize = (string)RK.GetValue("WindowSize", (string)strWindowSize);
					// Parse the screen dimensions text
					char[] delimiterChars = { 'x' };
					string[] words = strWindowSize.Split(delimiterChars);
					WindowSize.X = Convert.ToInt32(words[0]);
					WindowSize.Y = Convert.ToInt32(words[1]);
				}
			}
			catch (Exception error)
			{
				Trace.WriteLine(error);
			}
        }

		public void Initialize()
		{
			Console.WriteLine();
			Materials.ViewingDistance = SettingsInt[(int)IntSettings.ViewingDistance] = (int)Math.Min(Simulator.TRK.ORTRKData.MaxViewingDistance, SettingsInt[(int)IntSettings.ViewingDistance]);
			if (SettingsInt[(int)IntSettings.SoundDetailLevel] > 0)
			{
				SoundEngine = new ISoundEngine();
				SoundEngine.SetListenerPosition(new IrrKlang.Vector3D(0, 0, 0), new IrrKlang.Vector3D(0, 0, 1));
				SoundEngine.SoundVolume = 0;  // while loading
				// Swap out original file factory to support loops - by GeorgeS
				SoundEngine.AddFileFactory(new WAVIrrKlangFileFactory());
				IngameSounds = new SoundSource(this, Simulator.RoutePath + "\\Sound\\ingame.sms");
			}
			// By GeorgeS
			WorldSounds = new WorldSounds(this);
			ReadENVFile();
			TTypeDatFile = new TTypeDatFile(Simulator.RoutePath + @"\TTYPE.DAT");
			Tiles = new Tiles(Simulator.RoutePath + @"\TILES\");
			MilepostUnitsMetric = Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric;
			SetupBackgroundProcesses();
		}

		public void Run()
		{
			RenderProcess.Run();
		}

		/// <summary>
        /// Called once before the graphics device is started to configure the 
        /// graphics card and XNA game engine.
        /// Executes in the RenderProcess thread.
        /// </summary>
        public void Configure(RenderProcess renderProcess)
        {
            RenderProcess = renderProcess;
            renderProcess.Window.Title = "Open Rails";

            GDM = renderProcess.GraphicsDeviceManager;

            renderProcess.Content.RootDirectory = "Content";

            // TODO, this may cause problems with video cards not set up to handle these settings
            // do we need to check device capabilities first?
            //
            // No. XNA automatically checks capabilities. For example, if the user selects a screen
            // resolution that is greater than what the hardware can support, XNA adjusts the
            // resolution to the actual capability. "...the XNA framework automatically selects the 
            // highest resolution supported by the output device." rvg
			GDM.SynchronizeWithVerticalRetrace = SettingsBool[(int)BoolSettings.VerticalSync];
            renderProcess.IsFixedTimeStep = false; // you get smoother animation if we pace to video card retrace setting
            renderProcess.TargetElapsedTime = TimeSpan.FromMilliseconds(1); // setting this a value near refresh rate, ie 16ms, causes hiccups ( beating against refresh rate )
            GDM.PreferredBackBufferWidth = (int)WindowSize.X; // screen.Bounds.Width; // 1680;
            GDM.PreferredBackBufferHeight = (int)WindowSize.Y; // screen.Bounds.Height; // 1050;
            GDM.IsFullScreen = isFullScreen;
            GDM.PreferMultiSampling = true;
            //GDM.PreferredBackBufferFormat = SurfaceFormat.Bgr32;
            //GDM.PreferredDepthStencilFormat = DepthFormat.Depth32;
			GDM.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(GDM_PreparingDeviceSettings);
        }

		void GDM_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
		{
			// This stops ResolveBackBuffer() clearing the back buffer.
			e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
			UpdateAdapterInformation(e.GraphicsDeviceInformation.Adapter);
		}

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background 
        /// processes haven't started yet.
        /// Executes in the RenderProcess thread.
        /// </summary>
        public void Initialize(RenderProcess renderProcess)
        {
            GraphicsDevice = renderProcess.GraphicsDevice;
			DisplaySize.X = GraphicsDevice.Viewport.Width;
			DisplaySize.Y = GraphicsDevice.Viewport.Height;

            PlayerLocomotive = Simulator.InitialPlayerLocomotive();

			if (SettingsInt[(int)IntSettings.SoundDetailLevel] > 0)
            {
                ISound ambientSound = SoundEngine.Play2D(Simulator.BasePath + @"\SOUND\gen_urb1.wav", true);  // TODO temp code
                if (ambientSound != null)
                    ambientSound.Volume = 0.2f;
            }

            InfoDisplay = new InfoDisplay(this);
            
            // Initialse popup windows.
			WindowManager = new WindowManager(this);
			TrackMonitorWindow = new TrackMonitorWindow(WindowManager);
			SwitchWindow = new SwitchWindow(WindowManager);
			TrainOperationsWindow = new TrainOperationsWindow(WindowManager);
			NextStationWindow = new NextStationWindow(WindowManager);
			CompassWindow = new CompassWindow(WindowManager);

            SkyDrawer = new SkyDrawer(this);
            TerrainDrawer = new TerrainDrawer(this);
            SceneryDrawer = new SceneryDrawer(this);
			if (SettingsBool[(int)BoolSettings.Precipitation]) PrecipDrawer = new PrecipDrawer(this);
			if (SettingsBool[(int)BoolSettings.Wire]) WireDrawer = new WireDrawer(this);
            TrainDrawer = new TrainDrawer(this);
			weatherControl = new WeatherControl(this);

            PlayerLocomotiveViewer =  GetPlayerLocomotiveViewer();

			// Set up cameras.
			WellKnownCameras = new List<Camera>();
			WellKnownCameras.Add(CabCamera = new CabCamera(this));
			WellKnownCameras.Add(FrontCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Front));
			WellKnownCameras.Add(BackCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Rear));
			WellKnownCameras.Add(PassengerCamera = new PassengerCamera(this));
			WellKnownCameras.Add(BrakemanCamera = new BrakemanCamera(this));
			WellKnownCameras.Add(HeadOutFwdCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Forward));
			WellKnownCameras.Add(HeadOutBackCamera = new HeadOutCamera(this, HeadOutCamera.HeadDirection.Backward));
			WellKnownCameras.Add(TracksideCamera = new TracksideCamera(this));
		
			if (CameraToRestore != -1)
                WellKnownCameras[CameraToRestore].Activate();
            else
               new FreeRoamCamera(this, Camera).Activate();

			if (SettingsBool[(int)BoolSettings.FullScreen])
				ToggleFullscreen();
        }

        /// <summary>
        /// Called 10 times per second when its safe to read volatile data
        /// from the simulator and viewer classes in preparation
        /// for the Load call.  Copy data to local storage for use 
        /// in the next load call.
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            TerrainDrawer.LoadPrep();
            SceneryDrawer.LoadPrep();
            TrainDrawer.LoadPrep();
            if (WireDrawer != null) WireDrawer.LoadPrep();
        }

        /// <summary>
        /// Called 10 times a second to load graphics content
        /// that comes and goes as the player and trains move.
        /// Called from background LoaderProcess Thread
        /// Do not access volatile data from the simulator 
        /// and viewer classes during the Load call ( see
        /// LoadPrep() )
        /// Executes in the LoaderProcess thread.
        /// </summary>
        public void Load( RenderProcess renderProcess )
        {
            TerrainDrawer.Load(renderProcess);
            SceneryDrawer.Load(renderProcess);
            TrainDrawer.Load(renderProcess);
            if (WireDrawer != null) WireDrawer.Load(renderProcess);
        }

		string adapterDescription;
		public string AdapterDescription { get { return adapterDescription; } }

		uint adapterMemory = 0;
		public uint AdapterMemory { get { return adapterMemory; } }

		public void UpdateAdapterInformation(GraphicsAdapter graphicsAdapter)
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

        /// <summary>
        /// Called whenever a key or mouse buttin is pressed for handling user input
        /// elapsedTime represents the the time since the last call to HandleUserInput
        /// Examine the static class UserInput for mouse and keyboard status
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            Camera.HandleUserInput(elapsedTime);

			if (PlayerLocomotiveViewer != null)
				PlayerLocomotiveViewer.HandleUserInput(elapsedTime);

            InfoDisplay.HandleUserInput(elapsedTime);
			WindowManager.HandleUserInput();

            // Check for game control keys
            if (UserInput.IsKeyDown(Keys.Escape)) {  Stop(); return; }
            if (UserInput.IsAltPressed(Keys.Enter)) { ToggleFullscreen(); }
            if (UserInput.IsPressed(Keys.Pause) ) Simulator.Paused = !Simulator.Paused;
            if (UserInput.IsPressed(Keys.PageUp)) { Simulator.Paused = false; Simulator.GameSpeed = Simulator.GameSpeed * 1.5f; }
            if (UserInput.IsPressed(Keys.PageDown)) Simulator.GameSpeed = 1; 
            if (UserInput.IsPressed(Keys.F2)) { Program.Save(); }
			if (UserInput.IsPressed(Keys.F4)) TrackMonitorWindow.Visible = !TrackMonitorWindow.Visible;
			if (UserInput.IsPressed(Keys.F8)) SwitchWindow.Visible = !SwitchWindow.Visible;
			if (UserInput.IsPressed(Keys.F9)) TrainOperationsWindow.Visible = !TrainOperationsWindow.Visible;
			if (UserInput.IsPressed(Keys.F10)) NextStationWindow.Visible = !NextStationWindow.Visible;
			if (UserInput.IsPressed(Keys.D0)) CompassWindow.Visible = !CompassWindow.Visible;

            if (UserInput.IsPressed(Keys.E) && UserInput.IsCtrlKeyDown())
            {
                Simulator.PlayerLocomotive.Train.LeadNextLocomotive();
                Simulator.PlayerLocomotive = Simulator.PlayerLocomotive.Train.LeadLocomotive;
                Simulator.PlayerLocomotive.Train.CalculatePositionOfCars(0);  // fix the front traveller
                Simulator.PlayerLocomotive.Train.RepositionRearTraveller();    // fix the rear traveller
                PlayerLocomotiveViewer = Simulator.PlayerLocomotive.GetViewer(this);
                Camera.Activate();
            }

            // Change view point - cab, passenger, outside, etc
            if (UserInput.IsPressed(Keys.D1)) { if (CabCamera.HasCABViews) CabCamera.Activate(); }
            if (UserInput.IsPressed(Keys.D2)) FrontCamera.Activate();
			if (UserInput.IsPressed(Keys.D3)) BackCamera.Activate();
			if (UserInput.IsPressed(Keys.D4)) TracksideCamera.Activate();
            if (UserInput.IsPressed(Keys.D5)) { if (PassengerCamera.HasPassengerCamera) PassengerCamera.Activate(); }
			if (UserInput.IsPressed(Keys.D6)) BrakemanCamera.Activate();
            if (UserInput.IsPressed(Keys.D7) || UserInput.IsPressed(Keys.D8)) new FreeRoamCamera(this, Camera).Activate();

            bool mayheadout = (Camera == CabCamera) || (Camera == HeadOutFwdCamera) || (Camera == HeadOutBackCamera);
            if (UserInput.IsPressed(Keys.Up) && mayheadout) HeadOutFwdCamera.Activate();
            if (UserInput.IsPressed(Keys.Down) && mayheadout) HeadOutBackCamera.Activate();

            if (UserInput.IsPressed(Keys.G) && !UserInput.IsShiftDown()) Simulator.SwitchTrackAhead( PlayerTrain );
            if (UserInput.IsPressed(Keys.G) && UserInput.IsShiftDown()) Simulator.SwitchTrackBehind( PlayerTrain );
            if (UserInput.IsPressed(Keys.F) && UserInput.IsShiftDown() && UserInput.IsCtrlKeyDown()) { Simulator.PlayerLocomotive.Flipped = !Simulator.PlayerLocomotive.Flipped; Simulator.PlayerLocomotive.SpeedMpS *= -1; }
            if (!Simulator.Paused && UserInput.IsAltKeyDown())
            {
                isMouseShouldVisible = true;
                if (UserInput.MouseState.LeftButton == ButtonState.Pressed && UserInput.Changed)
                {
                    TryThrowSwitchAt();
                    UserInput.Handled();
                }
            }
            else if (!Simulator.Paused && UserInput.IsKeyDown(Keys.U))
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
                isMouseShouldVisible = WindowManager.HasVisiblePopupWindows();
            }

            RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
		}


        //
        //  This is to enable the user to move popup windows
        //  Coded as a separate routine as HandleUserInput does not cater for mouse movemenmt.
        //
        public void HandleMouseMovement()
        {
            MouseState currentMouseState = Mouse.GetState();

            // Handling mouse movement and timing - GeorgeS
            if (currentMouseState.X != originalMouseState.X ||
                currentMouseState.Y != originalMouseState.Y)
            {
                isMouseTimerVisible = true;
                MouseShownAt = Program.RealTime;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }
            else if (isMouseTimerVisible && MouseShownAt + .5 < Program.RealTime)
            {
                isMouseTimerVisible = false;
                RenderProcess.IsMouseVisible = isMouseShouldVisible || isMouseTimerVisible;
            }

            originalMouseState = currentMouseState;
        }

        /// <summary>
        /// Called every frame to update animations and load the frame contents .
        /// Note:  this doesn't actually draw on the screen surface, but 
        /// instead prepares a list of drawing primitives that will be rendered
        /// later in RenderFrame.Draw() by the RenderProcess thread.
        /// elapsedTime represents the the time since the last call to PrepareFrame
        /// Executes in the UpdaterProcess thread.
        /// </summary>
		public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
		{
			// Mute sound when paused
			if (SoundEngine != null)
			{
				if (Simulator.Paused)
					SoundEngine.SoundVolume = 0;
				else
					SoundEngine.SoundVolume = 1;
			}

			if (ScreenHasChanged())
				Camera.ScreenChanged();

			// Update camera first...
			Camera.Update(elapsedTime);
			// No above camera means we're allowed to auto-switch to cab view.
			if ((AboveGroundCamera == null) && Camera.IsUnderground)
			{
				AboveGroundCamera = Camera;
				CabCamera.Activate();
			}
			else if (AboveGroundCamera != null)
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
			// We're now ready to prepare frame for the camera.
			Camera.PrepareFrame(frame, elapsedTime);

			frame.PrepareFrame(elapsedTime);
			SkyDrawer.PrepareFrame(frame, elapsedTime);
			TerrainDrawer.PrepareFrame(frame, elapsedTime);
			SceneryDrawer.PrepareFrame(frame, elapsedTime);
			TrainDrawer.PrepareFrame(frame, elapsedTime);
			// By GeorgeS
			WorldSounds.Update(elapsedTime);
			if (PrecipDrawer != null) PrecipDrawer.PrepareFrame(frame, elapsedTime);
			if (WireDrawer != null) WireDrawer.PrepareFrame(frame, elapsedTime);
			InfoDisplay.PrepareFrame(frame, elapsedTime);
			// By GeorgeS
			IngameSounds.Update(elapsedTime);
		}


        /// <summary>
        /// Unload all graphical content and restore memory
        /// Executes in the RenderProcess thread.
        /// </summary>
        public void Unload(RenderProcess renderProcess)
        {
            if( SoundEngine != null )
                SoundEngine.StopAllSounds();
        }

        public void Stop()
        {
			InfoDisplay.Stop();
            RenderProcess.Stop();
        }

		/// <summary>
		/// Report an Exception from a background process (e.g. loader).
		/// </summary>
		/// <param name="error"></param>
		public void ProcessReportError(Exception error)
		{
			// Log the error first in case we're burning.
			Trace.WriteLine(error);
			// Stop the world!
			Stop();
			// Show the user that it's all gone horribly wrong.
			System.Windows.Forms.MessageBox.Show(error.ToString());
		}

        /// <summary>
        /// Determine the correct environment files for this activity and read it in.
        /// </summary>
        private void ReadENVFile()
        {
            string envFileName = Simulator.TRK.Tr_RouteFile.Environment.ENVFileName(Simulator.Season, Simulator.Weather);

            ENVFile = new ENVFile(Simulator.RoutePath + @"\ENVFILES\" + envFileName);
        }

        /// <summary>
        /// Adjust all projection matrixes and buffer sizes
        /// </summary>
        private void ToggleFullscreen()
        {
            bool IsFullScreen = !GDM.IsFullScreen;
            if (IsFullScreen)
            {
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.PrimaryScreen;
                GDM.PreferredBackBufferWidth = screen.Bounds.Width;
                GDM.PreferredBackBufferHeight = screen.Bounds.Height;
                GDM.PreferredBackBufferFormat = SurfaceFormat.Color;
                GDM.PreferredDepthStencilFormat = DepthFormat.Depth32;
            }
            else
            {
                GDM.PreferredBackBufferWidth = (int)WindowSize.X;
                GDM.PreferredBackBufferHeight = (int)WindowSize.Y;
            }
            RenderProcess.ToggleFullScreen();
        }

        /// <summary>
        /// Return true if the screen has changed dimensions
        /// </summary>
        /// <returns></returns>
        private bool ScreenHasChanged()
        {
            if (RenderProcess.GraphicsDeviceManager.IsFullScreen != isFullScreen)
            {
                isFullScreen = RenderProcess.GraphicsDeviceManager.IsFullScreen;
                return true;
            }
            return false;
        }
        private bool isFullScreen = false;

        private TrainCarViewer GetPlayerLocomotiveViewer()
        {
            return TrainDrawer.GetViewer(PlayerLocomotive);
        }

        /// <summary>
        /// The user has left clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        private void TryUncoupleAt()
        {
            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = UserInput.FarPoint - UserInput.NearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(UserInput.NearPoint, direction);

            // check each car
            TDBTraveller traveller = new TDBTraveller(PlayerTrain.FrontTDBTraveller);
            traveller.ReverseDirection();
            foreach (TrainCar car in PlayerTrain.Cars)
            {
                float d = (car.CouplerSlackM + car.GetCouplerZeroLengthM()) / 2;
                traveller.Move(car.Length + d);

                Vector3 xnaCenter = Camera.XNALocation(traveller.WorldLocation);
                float radius = 2f;  // 2 meter click range
                BoundingSphere boundingSphere = new BoundingSphere(xnaCenter, radius);

                if (null != pickRay.Intersects(boundingSphere))
                {
                    Simulator.UncoupleBehind(car);
                    break;
                }
                traveller.Move(d);
            }
        }
        /// <summary>
        /// The user has left clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        private void TryThrowSwitchAt()
        {
            TrJunctionNode bestNode = null;
            float bestD = 10;
            // check each switch
            for (int j = 0; j < Simulator.TDB.TrackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = Simulator.TDB.TrackDB.TrackNodes[j];
                if (tn != null && tn.TrJunctionNode != null)
                {

                    Vector3 xnaCenter = Camera.XNALocation(new WorldLocation(tn.UiD.TileX, tn.UiD.TileZ, tn.UiD.X, tn.UiD.Y, tn.UiD.Z));
                    float d = ORTSMath.LineSegmentDistanceSq(xnaCenter, UserInput.NearPoint, UserInput.FarPoint);
                    if (bestD > d && !Simulator.SwitchIsOccupied(j))
                    {
                        bestNode = tn.TrJunctionNode;
                        bestD = d;
                    }
                }
            }
            if (bestNode != null)
                bestNode.SelectedRoute = 1 - bestNode.SelectedRoute;
        }

        public void SetupBackgroundProcesses()
        {
            int processors =  System.Environment.ProcessorCount;
            RenderProcess = new RenderProcess( this);   // the order is important, since one process depends on the next
            LoaderProcess = new LoaderProcess( this);
            if (processors > 1)
                UpdaterProcess = new UpdaterProcess( this);
        }


    } // Viewer3D
} // namespace ORTS
