// COPYRIGHT 2014 by the Open Rails project.
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
//
//
//
//TO-DO list for Trackviewer      
// Ideas from others
//      Be able to list the issues directly without going through the ORTS logfile
//      Make it XNA independent.
//      Import & export.
//          via JSON? Might be a good way to learn JSON.
//          if we are going to write own routines, then use stringbuilder
//
// Release issues 
//      Always: 1. Update SVN. 
//              2. look at all to-dos and remove temporary changes. 
//              3. update version. 
//              4. remove debug. 
//              5. Set xml compiler version on, check all xml warnings, and turn it off again.
//              6. test
//
// Little things
//      Add y to statusbar, but perhaps only for items?
//      
// Looks and usability
//      drawTrains: add y, add direction=angle. Add option to (re-)connect to ORTS. Remove http variant. train replaces mouselocation
//
// Code improvements
//      Still dependency on Runactivity because of TDBfile. Once that is removed, we can remove dependency
//      remove dependency on ORTS.Settings. Even though it means a bit of code duplication
//      re-enable trace warnings (e.g. in Trainpath).
//      colors should not be string based, but enum.
//
// MSTS trackviewer features perhaps to take over:
//      different color for switches
//      track width option
//      add slope and height
//      Save and restore? But that is like writing/reading tsection.dat, .tdb, .rdb., and .pat files.
// 
// ORTS specific items to add
//      new signalling TrackCircuitSection number. Cumbersome because of dependence on Simulator.
//      Add milepost and speedpost texture?
//
// Further ideas
//      add crossover?
//
// Performance improvements
//      How can I measure performance. I do not want FPS, but it might help measuring improvement.
//      Instead of creating arcs from lines, create arc textures depending on need
//      Once we use more textures, let draw sort them itself. But this needs that we specify z-depth for all textures
//      Split basicshapes into 'static' and 'mouse-dependent'
//          update the static part only when needed.
//          update the mouse-dependent only when needed.
//      Perhaps even create a texture from tracks, one for current items, and draw these, and only draw hightlights directly
//          This might already save some time related to the inset (because it can re-use the texture if we make it big enough)
//          We can also make it more advanced, to support lots of shifting/zooming without generating a new track texture
//          But might take up memory? 
//
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using System.Windows.Forms;
using System.Drawing;
using ORTS.Menu;
using ORTS.Common;
using ORTS.TrackViewer.Drawing;
using ORTS.TrackViewer.UserInterface;
using ORTS.TrackViewer.Editing;

namespace ORTS.TrackViewer
{
    
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TrackViewer : Microsoft.Xna.Framework.Game
    {
        /// <summary>String showing the version of the program</summary>
        public readonly static string TrackViewerVersion = "2014/03/13";
        /// <summary>Path where the content (like .png files) is stored</summary>
        public string ContentPath { get; private set; }
             
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        /// <summary>Folder where MSTS is installed (or at least, where the files needed for tracks, routes and paths are stored)</summary>
        public Folder InstallFolder { get; private set; }
        /// <summary>List of available routes (in the install directory)</summary>
        public List<Route> Routes { get; private set; }
        /// <summary>List of available paths in the current route</summary>
        public List<Path> Paths { get; private set; }
        /// <summary>Route, ie with a path c:\program files\microsoft games\train simulator\routes\usa1  - may be different on different pc's</summary>
        public Route CurrentRoute { get; private set; } 
        /// <summary>Route that was used last time</summary>
        private Route DefaultRoute;
        /// <summary>Width of the drawing screen in pixels</summary>
        public int ScreenW { get; private set; }
        /// <summary>Height of the drawing screen in pixels</summary>
        public int ScreenH { get; private set; }

        /// <summary>(Draw)trackDB, that also contains the track data base and the track section data</summary>
        public DrawTrackDB drawTrackDB { get; private set; }

        /// <summary>Main draw area</summary>
        public DrawArea drawArea { get; private set; }
        /// <summary>Draw area for the inset</summary>
        ShadowDrawArea drawAreaInset;
        /// <summary>The scale ruler to draw on screen</summary>
        DrawScaleRuler drawScaleRuler;
        /// <summary>The routines to draw trains from runactivy</summary>
        DrawTrains drawTrains;
        /// <summary>The routines to draw the world tiles</summary>
        DrawWorldTiles drawWorldTiles;

        /// <summary>The Path editor</summary>
        public PathEditor pathEditor { get; private set; }
        /// <summary>The routines to draw the .pat file</summary>
        public DrawPATfile drawPATfile;

        /// <summary>The menu at the top</summary>
        MenuControl menuControl;
        /// <summary>The status bar at the bottom</summary>
        StatusBarControl statusBarControl;
        /// <summary>The frame rate</summary>
        public SmoothedData FrameRate { get; private set; }

        /// <summary></summary>
        private bool lostFocus;  //when we have lost focus, we do not want to enable shifting with mouse
        /// <summary></summary>
        private int skipDrawAmount = 0; // number of times we want to skip draw because nothing happened
        /// <summary></summary>
        private const int maxSkipDrawAmount = 10;

        /// <summary>
        /// Constructor. This is where it all starts.
        /// </summary>
        public TrackViewer()
        {
            graphics = new GraphicsDeviceManager(this);
            ContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Content");
           
            Content.RootDirectory = "Content";
            //graphics.PreferredBackBufferHeight = screenH;
            //graphics.PreferredBackBufferWidth  = screenW;
            ScreenH = graphics.PreferredBackBufferHeight;
            ScreenW = graphics.PreferredBackBufferWidth;
            setAliasing();
            graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new System.EventHandler(Window_ClientSizeChanged);
        

            //we do not a very fast behaviour, but we do need to get all key presses
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(0.05);
            FrameRate = new SmoothedData(0.5f);
        }

        /// <summary>
        /// Set aliasing depending on the settings (set in the menu)
        /// </summary>
        public void setAliasing()
        {
            // Personally, I do not think anti-aliasing looks crisp at all
            graphics.PreferMultiSampling = Properties.Settings.Default.doAntiAliasing;
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            ScreenW = Window.ClientBounds.Width;
            ScreenH = Window.ClientBounds.Height;
            setSubwindowSizes();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// relation ontent.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            TVInputSettings.SetDefaults();

            statusBarControl = new StatusBarControl(this);
            DrawColors.Initialize(menuControl);
            menuControl = new MenuControl(this);
            
            drawWorldTiles = new DrawWorldTiles();
            drawTrains = new DrawTrains();
            drawScaleRuler = new DrawScaleRuler();
            drawArea = new DrawArea(drawScaleRuler);
            drawAreaInset = new ShadowDrawArea(null);
            drawAreaInset.strictChecking = true;
            setSubwindowSizes();
            

            this.IsMouseVisible = true;

            // install folder
            if (Properties.Settings.Default.installDirectory == "")
            {
                try
                {
                    Properties.Settings.Default.installDirectory = MSTS.MSTSPath.Base();
                }
                catch {}
            }
            InstallFolder = new Folder("default", Properties.Settings.Default.installDirectory);
            try
            {
                findRoutes(InstallFolder);
            }
            catch
            {
            }
            
            base.Initialize();
        }

        /// <summary>
        /// Set the sizes of the various subwindows that they can use to draw upon. 
        /// </summary>
        void setSubwindowSizes()
        {
            int insetRatio = 10;
            int menuHeight = menuControl.menuHeight;
            int statusbarHeight = statusBarControl.statusbarHeight;
            menuControl.setScreenSize(ScreenW, menuHeight);
            statusBarControl.setScreenSize(ScreenW, statusbarHeight, ScreenH);

            drawArea.SetScreenSize(0, menuHeight, ScreenW, ScreenH - statusbarHeight - menuHeight);
            drawAreaInset.SetScreenSize(ScreenW - ScreenW / insetRatio, menuHeight + 1, ScreenW / insetRatio, ScreenH / insetRatio);
            drawScaleRuler.SetLowerLeftPoint(10, ScreenH - statusbarHeight - 10);

        }
 
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            BasicShapes.LoadContent(GraphicsDevice, spriteBatch, ContentPath);
            drawAreaInset.LoadContent(GraphicsDevice, spriteBatch, 2, 2, 2);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Even if not active, still do update from trains
            if (drawTrains.Update(drawArea)) skipDrawAmount = 0; 

            if (!this.IsActive)
            {
                lostFocus = true;
                return;
            }
 
            TVUserInput.Update();
            if (lostFocus)
            {
                // if the previous call was in inactive mode, we do want TVUserIut to be updated, but we will only
                // act on it the next round.
                lostFocus = false;
                return;
            }

            BasicShapes.Update(GraphicsDevice);
            DrawTrackDB.ClearHighlightOverrides(); // when update is called, we are not searching via menu

            // First check all the buttons that can be kept down.
            if (TVUserInput.IsDown(TVUserCommands.ShiftLeft)) { drawArea.ShiftLeft(); skipDrawAmount = 0; }
            if (TVUserInput.IsDown(TVUserCommands.ShiftRight)) { drawArea.ShiftRight(); skipDrawAmount = 0; }
            if (TVUserInput.IsDown(TVUserCommands.ShiftUp)) {drawArea.ShiftUp(); skipDrawAmount=0;}
            if (TVUserInput.IsDown(TVUserCommands.ShiftDown)) { drawArea.ShiftDown(); skipDrawAmount = 0; }

            if (TVUserInput.IsDown(TVUserCommands.ZoomIn)) { drawArea.zoomIn(); skipDrawAmount = 0; }
            if (TVUserInput.IsDown(TVUserCommands.ZoomOut)) {drawArea.zoomOut(); skipDrawAmount = 0;}

            if (TVUserInput.Changed)
            {
                skipDrawAmount = 0;
            }

            if (TVUserInput.IsPressed(TVUserCommands.Quit)) this.Quit();

            if (TVUserInput.IsPressed(TVUserCommands.ZoomReset))
            {
                drawArea.zoomReset(drawTrackDB);
                drawAreaInset.zoomReset(drawTrackDB);  // needed in case window was resized
            }
            if (TVUserInput.IsPressed(TVUserCommands.ZoomToTile)) drawArea.zoomToTile();

            if (drawPATfile != null && Properties.Settings.Default.showPATfile)
            {
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPath))     drawPATfile.extendPath();
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPathFull)) drawPATfile.extendPathFull();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePath))     drawPATfile.reducePath();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePathFull)) drawPATfile.reducePathFull();
                if (TVUserInput.IsDown(TVUserCommands.ShiftToLocation)) drawArea.ShiftToLocation(drawPATfile.CurrentLocation);
            }

            if (pathEditor != null && Properties.Settings.Default.showTrainpath)
            {
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPath))     pathEditor.ExtendPath();
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPathFull)) pathEditor.ExtendPathFull();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePath))     pathEditor.ReducePath();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePathFull)) pathEditor.ReducePathFull();
                if (TVUserInput.IsDown(TVUserCommands.ShiftToLocation)) drawArea.ShiftToLocation(pathEditor.CurrentLocation);

                if (TVUserInput.IsPressed(TVUserCommands.EditorUndo)) pathEditor.Undo();
                if (TVUserInput.IsPressed(TVUserCommands.EditorRedo)) pathEditor.Redo();
                if (TVUserInput.IsMouseXButton1Pressed()) pathEditor.Undo();
                if (TVUserInput.IsMouseXButton2Pressed()) pathEditor.Redo();
            }


            if (TVUserInput.IsMouseMoved() && TVUserInput.IsMouseLeftButtonDown())
            {
                drawArea.ShiftArea(TVUserInput.MouseMoveX(), TVUserInput.MouseMoveY());
            }
            if (TVUserInput.IsMouseWheelChanged())
            {
                int mouseWheelChange = TVUserInput.MouseWheelChange();
                if (TVUserInput.IsDown(TVUserCommands.MouseZoomSlow))
                {
                    drawArea.zoomAroundMouse(mouseWheelChange > 0 ? -1 : 1);  
                }
                else
                {
                    drawArea.zoomAroundMouse(-mouseWheelChange / 40);
                }
            }

            if (TVUserInput.IsMouseRightButtonPressed() && pathEditor!= null && pathEditor.EditingIsActive)
            {
                pathEditor.PopupContextMenu(TVUserInput.MouseLocationX, TVUserInput.MouseLocationY);
            }

            drawArea.update();
            drawAreaInset.update();
            drawAreaInset.Follow(drawArea, 10f);

            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSignals)) menuControl.menuToggleShowSignals();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSidings)) menuControl.menuToggleShowSidings();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSidingNames)) menuControl.menuToggleShowSidingNames();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPlatforms)) menuControl.menuToggleShowPlatforms();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPlatformNames)) menuControl.menuToggleShowPlatformNames();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSpeedLimits)) menuControl.menuToggleShowSpeedLimits();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowMilePosts)) menuControl.menuToggleShowMilePosts();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowTrainpath)) menuControl.menuToggleShowTrainpath();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPATFile)) menuControl.menuToggleShowPATFile();


            if (TVUserInput.IsPressed(TVUserCommands.Debug)) runDebug();

            base.Update(gameTime);
            
        }

        /// <summary>
        /// Delegate that can be called by routines such that we can draw it to the screen
        /// </summary>
        /// <param name="message">Message to draw</param>
        public delegate void messageDelegate(string message);
        
        /// <summary>
        /// Simplified Draw routine that only shows background and a message. 
        /// </summary>
        /// <param name="message">The message you want to show</param>
        private void DrawLoadingMessage(string message)
        {
            // This is not really a game State, because it is not used interactively. In fact, Draw itself is
            // probably not called because the program is doing other things
            BeginDraw();
            GraphicsDevice.Clear(DrawColors.colorsNormal["clearwindow"]);
            spriteBatch.Begin();
            // it is better to have integer locations, otherwise text is difficult to read
            Vector2 messageLocation = new Vector2((float) Math.Round(ScreenW / 2f), (float) Math.Round(ScreenH / 2f));
            BasicShapes.DrawStringLoading(messageLocation, Color.Black, message);

            // we have to redo the, because we now first have to load the characters into textures.
            BasicShapes.Update(GraphicsDevice);
            BasicShapes.DrawStringLoading(messageLocation, Color.Black, message);
            spriteBatch.End();
            EndDraw();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            // Even if there is nothing new to draw for main window, we might still need to draw for the shadow textures.
            if (drawTrackDB != null)
            {
                drawAreaInset.DrawShadowTextures(drawTrackDB.DrawTracks, DrawColors.colorsNormal["clearwindowinset"]);
            }
            
            // if there is nothing to draw, be done.
            if (--skipDrawAmount > 0)
            {
                return;
            }

            GraphicsDevice.Clear(DrawColors.colorsNormal["clearwindow"]);
            if (drawTrackDB == null) return;

            spriteBatch.Begin();
            drawWorldTiles.Draw(drawArea);
            drawArea.DrawTileGrid();
            
            drawTrackDB.DrawRoads(drawArea);
            drawTrackDB.DrawTracks(drawArea);
            drawTrackDB.DrawJunctionAndEndNodes(drawArea);
            drawTrackDB.DrawTrackItems(drawArea);
            drawTrackDB.DrawRoadTrackItems(drawArea);
            drawTrackDB.DrawHighlights(drawArea, true);

            if (Properties.Settings.Default.showInset)
            {
                drawAreaInset.DrawBackground(DrawColors.colorsNormal["clearwindowinset"]);
                //drawTrackDB.DrawTracks(drawAreaInset); //replaced by next line
                drawAreaInset.DrawShadowedTextures(); 
                drawTrackDB.DrawHighlights(drawAreaInset, false);
                drawAreaInset.DrawBorder(Color.Red, drawArea);
                drawAreaInset.DrawBorder(Color.Black);
            }

            if (drawPATfile != null && Properties.Settings.Default.showPATfile) drawPATfile.Draw(drawArea);
            if (pathEditor != null && Properties.Settings.Default.showTrainpath) pathEditor.Draw(drawArea);

            CalculateFPS(gameTime);
            
            statusBarControl.Update(this, drawArea.mouseLocation);

            drawScaleRuler.Draw();

            drawTrains.Draw(drawArea);

            spriteBatch.End();

            base.Draw(gameTime);
            skipDrawAmount = maxSkipDrawAmount;
        }

 
        void CalculateFPS(GameTime gameTime)
        {
            float elapsedRealTime = (float)gameTime.ElapsedRealTime.TotalSeconds;
            FrameRate.Update(elapsedRealTime, 1f / elapsedRealTime);
        }

        /// <summary>
        /// Ask the user if we really want to quit or not, and if yes, well, quit.
        /// </summary>
        public void Quit()
        {

            if (MessageBox.Show("Do you really want to Quit?", "Question", MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                this.Exit();
            }
        }
 
        /// <summary>
        /// Open up a dialog so the user can select the install directory 
        /// (which should contain a sub-directory called ROUTES).
        /// </summary>
        public void selectInstallFolder ()
        {
            string folderPath = "";

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (InstallFolder != null)
            {
                folderBrowserDialog.SelectedPath = InstallFolder.Path;
            }
            folderBrowserDialog.ShowNewFolderButton = false;
            DialogResult dialogResult = folderBrowserDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                folderPath = folderBrowserDialog.SelectedPath;
            }

            if (folderPath != "")
            {
                try
                {
                    Folder newInstallFolder = new Folder("installFolder", folderPath);
                    findRoutes(newInstallFolder);
                    InstallFolder = newInstallFolder;
             
                    // make sure the current route is disabled,
                    CurrentRoute = null;
                    drawTrackDB = null;

                    Properties.Settings.Default.installDirectory = folderPath;
                    Properties.Settings.Default.Save();
                }
                catch
                {
                    MessageBox.Show("Directory is not a valid install directory.\nThe install directory needs to contain ROUTES, GLOBAL, ...");
                }
            }
        }

        /// <summary>
        /// Find the available routes, and if possible load the first one.
        /// </summary>
        private void findRoutes(Folder newInstallFolder)
        {
            if (newInstallFolder == null) return;
            List<Route> newRoutes = Route.GetRoutes(newInstallFolder).OrderBy(r => r.ToString()).ToList();


            if (newRoutes.Count > 0)
            {
                // set default route
                DefaultRoute = newRoutes[0];
                foreach (Route tryRoute in newRoutes)
                {
                    string dirName = tryRoute.Path.Split('\\').Last();
                    if (dirName == Properties.Settings.Default.defaultRoute)
                    {
                        DefaultRoute = tryRoute;
                    }
                }
                //setRoute(defaultRoute);

                Routes = newRoutes;
                menuControl.populateRoutes();
            }
            else
            {
                throw new Exception("No routes found");
            }
            
        }

        /// <summary>
        /// Load the default route. This would be either the route used last time, the current route, or else the first available route.
        /// </summary>
        public void setDefaultRoute()
        {
            setRoute(DefaultRoute);
        }

        /// <summary>
        /// Set and load a new route
        /// </summary>
        /// <param name="newRoute">The route to load, containing amongst other the directory name of the route</param>
        public void setRoute(Route newRoute)
        {
            if (newRoute == null) return;

            DrawLoadingMessage("Loading route...");

            try
            {
                messageDelegate messageHandler = new messageDelegate(DrawLoadingMessage);
                drawTrackDB = new DrawTrackDB(newRoute.Path, messageHandler);
                CurrentRoute = newRoute;

                Properties.Settings.Default.defaultRoute = CurrentRoute.Path.Split('\\').Last();
                if (Properties.Settings.Default.zoomRoutePath != CurrentRoute.Path)
                {
                    Properties.Settings.Default.zoomScale = -1; // To disable the use of zoom reset
                }
                Properties.Settings.Default.Save();
                drawArea.zoomReset(drawTrackDB);
                drawAreaInset.zoomReset(drawTrackDB);
                Window.Title = "TrackViewer: " + drawTrackDB.RouteName;
            }
            catch
            {
                MessageBox.Show("Route cannot be loaded. Sorry");
            }

            if (CurrentRoute == null) return;

            try
            {
                findPaths();
            }
            catch { }

            try
            {
                drawWorldTiles.SetRoute(CurrentRoute.Path);
            }
            catch { }

            menuControl.populatePlatforms();
            menuControl.populateSidings();
        }

        /// <summary>
        /// Find the paths (.pat files) belonging to the current route, and update the menu
        /// </summary>
        private void findPaths()
        {
            List<Path> newPaths = Path.GetPaths(CurrentRoute).OrderBy(r => r.Name).ToList();
            Paths = newPaths;
            menuControl.populatePaths();
            setPath(null);   
        }

        /// <summary>
        /// Once a path has been selected, do the necessary loading.
        /// </summary>
        /// <param name="path">Path (with FilePath) that has to be loaded</param>
        internal void setPath(Path path)
        {
            if (path == null)
            {
                drawPATfile = null;
                pathEditor = null;
            }
            else
            {
                DrawLoadingMessage("Loading .pat file ...");
                drawPATfile = new DrawPATfile(path);

                DrawLoadingMessage("Processing .pat file ...");
                pathEditor = new PathEditor(drawTrackDB, path);
                DrawLoadingMessage(" ...");
            }   
        }

        /// <summary>
        /// Find a track node, center around it and highlight it
        /// </summary>
        /// <param name="TrackNumberIndex">Index of the track node</param>
        public void CenterAroundTrackNode(int TrackNumberIndex)
        {
            CenterAround(drawTrackDB.TrackNodeHighlightOverride(TrackNumberIndex));
        }

        /// <summary>
        /// Find a Road track node, center around it and highlight it
        /// </summary>
        /// <param name="TrackNumberIndex">Index of the track node</param>
        public void CenterAroundTrackNodeRoad(int TrackNumberIndex)
        {
            CenterAround(drawTrackDB.TrackNodeHighlightOverrideRoad(TrackNumberIndex));
        }

        /// <summary>
        /// Find a trackItem and center around it and highlight it
        /// </summary>
        /// <param name="TrackItemIndex">Index of the track item</param>
        public void CenterAroundTrackItem(int TrackItemIndex)
        {
            WorldLocation itemLocation = drawTrackDB.TrackItemHighlightOverride(TrackItemIndex);
            if (itemLocation == null) return;
            CenterAround(itemLocation);
        }

        /// <summary>
        /// Find a road trackItem and center around it and highlight it
        /// </summary>
        /// <param name="TrackItemIndex">Index of the track item</param>
        public void CenterAroundTrackItemRoad(int TrackItemIndex)
        {
            WorldLocation itemLocation = drawTrackDB.TrackItemHighlightOverrideRoad(TrackItemIndex);
            if (itemLocation == null) return;
            CenterAround(itemLocation);
        }

        /// <summary>
        /// Center around a certain world-location. In particular, outside the normal Draw/Update loop. So it does a draw itself
        /// To be used from additional windows (like search).
        /// </summary>
        /// <param name="centerLocation">Location to center the view window around</param>
        public void CenterAround(WorldLocation centerLocation)
        {
            if (centerLocation == null) return;

            drawArea.ShiftToLocation(centerLocation);
            drawArea.mouseLocation = centerLocation;
            drawAreaInset.Follow(drawArea, 10f);
            BeginDraw();
            skipDrawAmount = 0; // make sure the draw is really done.
            Draw(new GameTime());
            EndDraw();

        }

        void runDebug()
        {
            //Properties.Settings.Default.statusShowFPS = false;
            //setDefaultRoute();
            //setPath(Paths[0]);
            //drawArea.zoomToTile();
            //drawArea.zoomCentered(-15);
            //////CenterAroundTrackNode(200);
            //drawArea.ShiftToLocation(pathEditor.CurrentLocation);
            ////drawArea.ShiftToLocation(pathEditor.trainpath.FirstNode.location);

            //pathEditor.EditingIsActive = true;
            //pathEditor.ExtendPathFull();
            ////Exit();
        }
    }
}

/*
 * Layer depth of various items (1.0 is back, 0.0 is front)
 *      tiles               lines
 *      grid                lines
 *      track               lines (but perhaps later arcs as well)
 *      track high          idem
 *      track hot           idem
 *      inset background    lines
 *      inset track         lines
 *      inset track high    lines
 *      inset border (or perhaps same as inset track high) lines
 *      items               various textures, and especially here we want to be able to sort.
 *                          Possible alternative is to use a big texture, and use only part of it.
 *      items high          idem
 *      path                lines, textures
 *      ruler               lines
 *      text                text
*/