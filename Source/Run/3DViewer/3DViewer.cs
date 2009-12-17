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
 * 
 * 
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MSTS;
using System.Threading;
using IrrKlang;
using System.IO;
using Microsoft.Win32;

namespace ORTS
{
    /// <summary>
    /// </summary>
    public class Viewer : Microsoft.Xna.Framework.Game
    {
        // Current camera
        public Camera Camera;

        private CabCamera CabCamera;
        private TrackingCamera FrontCamera;
        private TrackingCamera BackCamera;
        private PassengerCamera PassengerCamera;
        private BrakemanCamera BrakemanCamera;

        Thread ViewerThread;
        // ie after the initial loading, Loader owns the ContentManager
        public bool ContentLoaded = false; // tells us when the background content loader has the initial content loaded

        GraphicsDeviceManager graphics;
        public int RenderState = 0;     // numeric representation of the current state of the graphicsdevice.renderstate
        // used in draw routines to minimize time consuming setup of the renderstate
        // 0 means renderstate is unknown
        // 1 set up by terrain draw
        // 2 set up by staticshape draw
        // 3 set up by sky draw
        // 4 motion blur setup
        // 5 set up for water draw

        public KeyboardInput KeyboardInput = new KeyboardInput(); // enhances Keyboard class to detect key presses

        // These variables are used by the various graphic objects created by 3dViewer
        public Simulator Simulator;                 // what simulator are we viewing

        public bool IsFullScreen = false;
        public int SoundDetailLevel = 5;             // used to select which sound scaleability group to use.
        public int WorldObjectDensity = 10;
        public float ViewingDistance = 2000f;       // used for culling  TODO, complete user input slider
        
        public string Text = "Running OK";          // a way to display debug messages etc on the screen

        // SOUND
        public ISoundEngine SoundEngine;  // IrrKlang Sound Device

        public SceneryShader SceneryShader = null;
        public TerrainDrawer TerrainDrawer = null;
        public SceneryDrawer SceneryDrawer = null;
        public TrainDrawer TrainDrawer = null;
        public MotionBlur MotionBlur = null;

        public Thread LoaderThread = null;

        public ENVFile ENVFile;
        public TTypeDatFile TTypeDatFile;
        public Tiles Tiles = null;

        /// <summary>
        /// Create a 3D view of the specified simulator.
        /// </summary>
        /// <param name="simulator"></param>
        /// <param name="fullScreen"></param>
        public Viewer(Simulator simulator, bool fullScreen)
        {
            Simulator = simulator;
            IsFullScreen = fullScreen;
            ViewerThread = Thread.CurrentThread;
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.PrimaryScreen;

            WorldObjectDensity = 10;
            SoundDetailLevel = 10;

            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey( Program.RegistryKey);
            if (RK != null)
            {
                WorldObjectDensity = (int)RK.GetValue("WorldObjectDensity", WorldObjectDensity);
                SoundDetailLevel = (int)RK.GetValue("SoundDetailLevel", SoundDetailLevel);
            }
            if (Simulator.RouteName == "LPSYARD")
                ViewingDistance = 500;

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // TODO, this may cause problems with video cards not set up to 
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false; // you get smoother animation if we pace to video card retrace setting
            TargetElapsedTime = TimeSpan.FromMilliseconds(1); // setting this a value near refresh rate, ie 16ms, causes hickups ( beating against refresh rate )
            graphics.PreferredBackBufferWidth = 1024; // screen.Bounds.Width; // 1680;
            graphics.PreferredBackBufferHeight = 768; // screen.Bounds.Height; // 1050;
            graphics.IsFullScreen = false;
            graphics.PreferMultiSampling = true;
            graphics.PreferredBackBufferFormat = SurfaceFormat.Bgr32;

            KeyboardInput.SetKeyboardState(Keyboard.GetState());

            Console.WriteLine();
            SoundEngine = new ISoundEngine();
            SoundEngine.SetListenerPosition(new IrrKlang.Vector3D(0, 0, 0), new IrrKlang.Vector3D(0, 0, 1));
            SoundEngine.SoundVolume = 0;  // while loading


            ReadENVFile();
            TTypeDatFile = new TTypeDatFile(Simulator.RoutePath + @"\TTYPE.DAT");
            Tiles = new Tiles(Simulator.RoutePath + @"\TILES\");
        }

        private void ReadENVFile()
        {
            string envFileName = Simulator.TRK.Tr_RouteFile.Environment.ENVFileName(
                                        Simulator.Activity.Tr_Activity.Tr_Activity_Header.Season, 
                                        Simulator.Activity.Tr_Activity.Tr_Activity_Header.Weather);

            ENVFile = new ENVFile(Simulator.RoutePath + @"\ENVFILES\" + envFileName);
        }

        /// <summary>
        /// Executes in a separate thread to keep the correct terrain tiles loaded
        /// as the camera moves
        /// </summary>
        private void Loader()
        {
            while ( Thread.CurrentThread.ThreadState != ThreadState.Aborted && ViewerThread.IsAlive)
            {
                if (TerrainDrawer != null) TerrainDrawer.Update(null);
                if (SceneryDrawer != null) SceneryDrawer.Update(null);
                if (TrainDrawer != null) TrainDrawer.Update(null);
                ContentLoaded = true; 
                Thread.Sleep(100);  // update 10 times per second
            }
        }


        /// <summary>
        /// Called by the base class when the graphics device is up and ready.
        /// Guaranteed to be called only once per session.
        /// </summary>
        protected override void Initialize()
        {

            ISound ambientSound = SoundEngine.Play2D(Simulator.BasePath + @"\SOUND\gen_urb1.wav", true);  // TODO temp code
            ambientSound.Volume = 0.2f;

            // And set up the surface material effect shared by all patches
            if (SceneryShader == null)
            {
                SceneryShader = new SceneryShader(GraphicsDevice, Content);
                SceneryShader.BumpTexture = ACEFile.Texture2DFromFile(GraphicsDevice, Simulator.RoutePath + @"\TERRTEX\microtex.ace");
                // TODO, pull microtex.ace from tile file
            }
         
            // Sky Z=1 so draw it first
            new SkyDrawer(this);

            // Terrain
            Console.Write(" T");
            TerrainDrawer = new TerrainDrawer(this);

            // Scenery
            Console.Write(" W");
            SceneryDrawer = new SceneryDrawer(this);

            // Trains
            TrainDrawer = new TrainDrawer(this);
            
            // Set up cameras
            CabCamera = new CabCamera(this);
            FrontCamera = new TrackingCamera(this, Tether.ToFront);
            BackCamera = new TrackingCamera(this, Tether.ToRear);
            PassengerCamera = new PassengerCamera(this);
            BrakemanCamera = new BrakemanCamera(this);

            // Set up default camera
            FrontCamera.Activate();

            // Start up the background file loader for Terrain and Scenery 
            // This must be done after all other content is loaded to avoid thread safety issues
            LoaderThread = new Thread(Loader);
            LoaderThread.Start();

            // Wait for initial content to load before continuing
            while (!ContentLoaded)
                Thread.Sleep(100);

            if (IsFullScreen)
            {
                IsFullScreen = false;
                ToggleFullscreen();
            }

            MotionBlur = new MotionBlur(this);
            MotionBlur.Enabled = false;
             
            // Frame rates, etc
            new InfoDisplay(this);
            
            base.Initialize();  // calls Initialize() on all Game.Components 
        }

        /// <summary>
        /// Undo what was done in Initialize
        /// </summary>
        private void Terminate()
        {
            SoundEngine.StopAllSounds();

            if (LoaderThread != null)
            {
                LoaderThread.Abort();
                while (LoaderThread.IsAlive) Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Hook gameengine exit so we can clean up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnExiting ( Object sender, EventArgs args )
        {
            Terminate();
        }

        /// <summary>
        /// Adjust all projection matrixes and buffer sizes
        /// </summary>
        private void ToggleFullscreen()
        {
            IsFullScreen = !IsFullScreen;
            if (IsFullScreen)
            {
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.PrimaryScreen;
                graphics.PreferredBackBufferWidth = screen.Bounds.Width; // 1680;
                graphics.PreferredBackBufferHeight = screen.Bounds.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = 1024;
                graphics.PreferredBackBufferHeight = 768;
            }
            graphics.ToggleFullScreen();
            // since each camera has its own projection matrix, they all have to be notified
            Camera.ScreenChanged();
            CabCamera.ScreenChanged();
            FrontCamera.ScreenChanged();
            BackCamera.ScreenChanged();
            PassengerCamera.ScreenChanged();
            BrakemanCamera.ScreenChanged();

            if (MotionBlur != null) MotionBlur.ScreenChanged();
        }

        /// <summary>
        /// Process keyboard and mouse input
        /// </summary>
        /// <param name="keyboard"></param>
        /// <param name="mouse"></param>
        /// <param name="gameTime"></param>
        void HandleInput(KeyboardInput keyboard, MouseState mouse, GameTime gameTime)
        {

            // Check for game control keys
            if (KeyboardInput.IsKeyDown(Keys.Escape)) { Terminate(); Exit(); return; }
            if (KeyboardInput.IsAltPressed(Keys.Enter)) { ToggleFullscreen(); }
            // MOTION BLUR TEMPORARILY DISABLED if (KeyboardInput.IsPressed(Keys.D9)) { if (MotionBlur != null) MotionBlur.Enabled = !MotionBlur.Enabled; }

            // Change view point - cab, passenger, outside, etc
            if (KeyboardInput.IsPressed(Keys.D1)) CabCamera.Activate();
            if (KeyboardInput.IsPressed(Keys.D2)) FrontCamera.Activate();
            if (KeyboardInput.IsPressed(Keys.D3)) BackCamera.Activate();
            if (KeyboardInput.IsPressed(Keys.D6)) BrakemanCamera.Activate();
            if (KeyboardInput.IsPressed(Keys.D5)) PassengerCamera.Activate();
            if (KeyboardInput.IsPressed(Keys.D4)
              || KeyboardInput.IsPressed(Keys.D7)
              || KeyboardInput.IsPressed(Keys.D8)) (new Camera(this, Camera)).Activate();

            // Uncoupling?
            if (!Simulator.Paused && KeyboardInput.IsKeyDown(Keys.U))
            {
                this.IsMouseVisible = true;
                if (mouse.LeftButton == ButtonState.Pressed)  
                    TryUncoupleAt(mouse.X, mouse.Y);
            }
            else
            {
                this.IsMouseVisible = false;
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            Simulator.Update(gameTime);   // TODO, this is temp code, Simulator should update in a separate thread

            // Mute sound when paused
            if (Simulator.Paused)
                SoundEngine.SoundVolume = 0;
            else
                SoundEngine.SoundVolume = 1;

            // Read the state of the input devices
            KeyboardInput.SetKeyboardState( Keyboard.GetState() );
            MouseState mouse = Mouse.GetState();

            HandleInput(KeyboardInput, mouse, gameTime);

            // Update Camera
            Camera.HandleInput(KeyboardInput, mouse, gameTime);
            Camera.Update(gameTime);

            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            RenderState = 0;  // since we've made some changes here
            GraphicsDevice.RenderState.DepthBias = 0f;

            GraphicsDevice.Clear(new Color(136, 138, 141));// light blue

            base.Draw(gameTime);
        }

        /// <summary>
        /// Setup the renderstate for fog
        /// </summary>
        public void SetupFog()
        {
            GraphicsDevice.RenderState.FogEnable = true;
            GraphicsDevice.RenderState.FogVertexMode = FogMode.None;  // vertex fog
            GraphicsDevice.RenderState.FogTableMode = FogMode.Linear;     // pixel fog off
            GraphicsDevice.RenderState.FogColor = new Color(162, 185, 215, 255); // new Color(128, 128, 128, 255);
            GraphicsDevice.RenderState.FogDensity = 1.0f;                      // used for exponential fog only, not linear
            GraphicsDevice.RenderState.FogEnd = ViewingDistance; // +300;
            GraphicsDevice.RenderState.FogStart = ViewingDistance / 2;
        }

        /// <summary>
        /// The user has left clicked with U pressed.   
        /// If the mouse was over a coupler, then uncouple the car.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        private void TryUncoupleAt(int mouseX, int mouseY)
        {
            Vector3 nearsource = new Vector3((float)mouseX, (float)mouseY, 0f);
            Vector3 farsource = new Vector3((float)mouseX, (float)mouseY, 1f);
            Matrix world = Matrix.CreateTranslation(0, 0, 0);
            Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(nearsource, Camera.XNAProjection, Camera.XNAView, world);
            Vector3 farPoint = GraphicsDevice.Viewport.Unproject(farsource, Camera.XNAProjection, Camera.XNAView, world);

            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);

            // check each car
            TDBTraveller traveller = new TDBTraveller( Simulator.PlayerTrain.FrontTDBTraveller );
            traveller.ReverseDirection();
            foreach (TrainCarSimulator car in Simulator.PlayerTrain.Cars)
            {
                traveller.Move(car.WagFile.Wagon.Length);

                Vector3 xnaCenter = Camera.XNALocation(traveller.WorldLocation);
                float radius = 2f;  // 2 meter click range
                BoundingSphere boundingSphere = new BoundingSphere(xnaCenter, radius);

                if ( null != pickRay.Intersects( boundingSphere ) )
                {
                    Simulator.UncoupleBehind(car);
                    break;
                }
            }
        }
    } // class Viewer

}
