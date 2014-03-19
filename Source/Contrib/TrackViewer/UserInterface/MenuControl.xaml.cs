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
// Possibly it is nicer to use 'commands' instead of Click_items. But this might conflict very much with howe we
// currently act on key commands
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//using System.Windows.Forms;
using System.Windows.Forms.Integration;

using System.Collections.ObjectModel;
using ORTS.TrackViewer.Properties;
using ORTS.TrackViewer.Drawing; // for colors
using ORTS.TrackViewer.Editing; // for context menu
namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// Interaction logic for MenuControl.xaml
    /// This part of the class contains all the callbacks related to the menu.
    /// Many parts of the menu are checkable items. These get initialized from stored property settings.
    /// When updated (by the user) also the stored property settings are updated.
    /// 
    /// A few menu items contain lists (e.g. of routes, platforms, sidings, paths). The list of menuitems
    /// are created by dedicated methods that receive the list of items from the rest of the program.
    /// 
    /// By being so central in the userinterface, it is sometimes difficult to make nice and clean interactions
    /// with the rest of the program. There are many calls to trackviewer and items within trackviewer.
    /// </summary>
    public partial class MenuControl : System.Windows.Controls.UserControl
    {
        /// <summary>Height of the menu in pixels</summary>
        public int menuHeight;
        private TrackViewer trackViewer;
        private ElementHost elementHost;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="trackViewer">The trackviewer object we need for callbacks</param>
        public MenuControl(TrackViewer trackViewer)
        {
            this.trackViewer = trackViewer;
            InitializeComponent();
            menuHeight = (int)menu1.Height;

            //ElementHost object helps us to connect a WPF User Control.
            elementHost = new ElementHost();
            elementHost.Location = new System.Drawing.Point(0, 0);
            elementHost.Name = "elementHost";
            elementHost.TabIndex = 1;
            elementHost.Text = "elementHost";
            elementHost.Child = this;
            System.Windows.Forms.Control.FromHandle(trackViewer.Window.Handle).Controls.Add(elementHost);

            initUserSettings();

        }

        /// <summary>
        /// set the size of the menu control (also after rescaling)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void setScreenSize(int width, int height)
        {
            elementHost.Size = new System.Drawing.Size(width, height);
        }

        /// <summary>
        /// Init various settings from the stored user settings.
        /// </summary>
        public void initUserSettings()
        {
            PopulateColors(Properties.Settings.Default.backgroundColorName);

            menuShowInset.IsChecked = Properties.Settings.Default.showInset;
            menuShowWorldTiles.IsChecked = Properties.Settings.Default.showWorldTiles;
            menuColorTracks.IsChecked = Properties.Settings.Default.colorTracks;

            menuShowJunctionNodes.IsChecked = Properties.Settings.Default.showJunctionNodes;
            menuShowEndNodes.IsChecked = Properties.Settings.Default.showEndNodes;
            menuShowSidingMarkers.IsChecked = Properties.Settings.Default.showSidingMarkers;
            menuShowSidingNames.IsChecked = Properties.Settings.Default.showSidingNames;
            menuShowPlatformMarkers.IsChecked = Properties.Settings.Default.showPlatformMarkers;
            menuShowPlatformNames.IsChecked = Properties.Settings.Default.showPlatformNames;
            menuShowCrossings.IsChecked = Properties.Settings.Default.showCrossings;
            menuShowSpeedLimits.IsChecked = Properties.Settings.Default.showSpeedLimits;
            menuShowMileposts.IsChecked = Properties.Settings.Default.showMileposts;
            menuShowHazards.IsChecked = Properties.Settings.Default.showHazards;
            menuShowSignals.IsChecked = Properties.Settings.Default.showSignals;
            menuShowPickups.IsChecked = Properties.Settings.Default.showPickups;
            menuShowSoundRegions.IsChecked = Properties.Settings.Default.showSoundRegions;

            menuShowPATfile.IsChecked = Properties.Settings.Default.showPATfile;
            menuShowTrainpath.IsChecked = Properties.Settings.Default.showTrainpath;

            menuStatusShowVectorSection.IsChecked = Properties.Settings.Default.statusShowVectorSections;
            menuStatusShowPATfile.IsChecked = Properties.Settings.Default.statusShowPATfile;
            menuStatusShowTrainpath.IsChecked = Properties.Settings.Default.statusShowTrainpath;

            menuDrawRoads.IsChecked = Properties.Settings.Default.drawRoads;
            menuShowCarSpawners.IsChecked = Properties.Settings.Default.showCarSpawners;
            menuShowRoadCrossings.IsChecked = Properties.Settings.Default.showRoadCrossings;

            menuShowScaleRuler.IsChecked = Properties.Settings.Default.showScaleRuler;
            menuUseMilesNotMeters.IsChecked = Properties.Settings.Default.useMilesNotMeters;

            UpdateMenuSettings();  // to be sure some other settings are done correctly

            menuDoAntiAliasing.IsChecked = Properties.Settings.Default.doAntiAliasing;

        }

        /// <summary>
        /// Update the (user) settings based on menu state
        /// </summary>
        void UpdateMenuSettings()
        {
            Properties.Settings.Default.showInset = menuShowInset.IsChecked;
            Properties.Settings.Default.showWorldTiles = menuShowWorldTiles.IsChecked;
            Properties.Settings.Default.colorTracks = menuColorTracks.IsChecked;
            Properties.Settings.Default.showJunctionNodes = menuShowJunctionNodes.IsChecked;
            Properties.Settings.Default.showEndNodes = menuShowEndNodes.IsChecked;
            Properties.Settings.Default.showSidingMarkers = menuShowSidingMarkers.IsChecked;
            Properties.Settings.Default.showSidingNames = menuShowSidingNames.IsChecked;
            Properties.Settings.Default.showPlatformMarkers = menuShowPlatformMarkers.IsChecked;
            Properties.Settings.Default.showPlatformNames = menuShowPlatformNames.IsChecked;
            Properties.Settings.Default.showCrossings = menuShowCrossings.IsChecked;
            Properties.Settings.Default.showSpeedLimits = menuShowSpeedLimits.IsChecked;
            Properties.Settings.Default.showMileposts = menuShowMileposts.IsChecked;
            Properties.Settings.Default.showSignals = menuShowSignals.IsChecked;
            Properties.Settings.Default.showHazards = menuShowHazards.IsChecked;
            Properties.Settings.Default.showSoundRegions = menuShowSoundRegions.IsChecked;
            Properties.Settings.Default.showPickups = menuShowPickups.IsChecked;

            Properties.Settings.Default.showPATfile = menuShowPATfile.IsChecked;
            Properties.Settings.Default.showTrainpath = menuShowTrainpath.IsChecked;

            Properties.Settings.Default.statusShowVectorSections = menuStatusShowVectorSection.IsChecked;
            Properties.Settings.Default.statusShowPATfile = menuStatusShowPATfile.IsChecked && menuShowPATfile.IsChecked;
            Properties.Settings.Default.statusShowTrainpath = menuStatusShowTrainpath.IsChecked && menuShowTrainpath.IsChecked;

            Properties.Settings.Default.drawRoads = menuDrawRoads.IsChecked;
            Properties.Settings.Default.showCarSpawners = menuShowCarSpawners.IsChecked;
            Properties.Settings.Default.showRoadCrossings = menuShowRoadCrossings.IsChecked;

            Properties.Settings.Default.showScaleRuler = menuShowScaleRuler.IsChecked;
            Properties.Settings.Default.useMilesNotMeters = menuUseMilesNotMeters.IsChecked;

            Properties.Settings.Default.Save();

            DrawColors.setTrackColors(menuColorTracks.IsChecked, menuShowWorldTiles.IsChecked);

            menuStatusShowPATfile.IsEnabled = menuShowPATfile.IsChecked;
            menuStatusShowTrainpath.IsEnabled = menuShowTrainpath.IsChecked;

            menuSavePath.IsEnabled = (trackViewer.pathEditor != null);
            menuNewPath.IsEnabled = (trackViewer.pathEditor != null
                                  && menuEnableEditing.IsChecked);
            menuEnableEditing.IsEnabled = (trackViewer.pathEditor != null);
            menuEditMetadata.IsEnabled = menuEnableEditing.IsChecked;

        }

        // only so it can be called as a callback
        private void UpdateMenuSettings(object sender, RoutedEventArgs e)
        {
            UpdateMenuSettings();
        }

        private void menuQuit_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.Quit();
        }

        private void menuZoomIn_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.zoomCentered(-4);
        }

        private void menuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.zoomCentered(4);
        }

        private void menuZoomReset_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.zoomReset(trackViewer.drawTrackDB);
        }

        private void menuZoomToTile_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.ZoomToTileCentered();
        }

        private void menuInstallFolder_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.selectInstallFolder();
            populateRoutes();
        }

        /// <summary>
        /// Update the menu to show the available routes
        /// </summary>
        public void populateRoutes()
        {
            menuSelectRoute.Items.Clear();
            if (trackViewer.Routes == null) return;
            foreach (ORTS.Menu.Route route in trackViewer.Routes)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = route.Name;
                menuItem.IsCheckable = false;
                menuItem.IsChecked = false;
                menuItem.Click += new RoutedEventHandler(menuSelectRoute_Click);

                menuSelectRoute.Items.Add(menuItem);
            }
            menuSelectRoute.UpdateLayout();
        }

        /// <summary>
        /// The user has selected a route, figure out which one and load it.
        /// </summary>
        private void menuSelectRoute_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;
            foreach (ORTS.Menu.Route route in trackViewer.Routes)
            {
                if (route.Name == (string)selectedMenuItem.Header)
                {
                    trackViewer.setRoute(route);
                    return;
                }
            }
        }

        /// <summary>
        /// Load the last used route
        /// </summary>
        private void menuReloadRoute_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.setDefaultRoute();
        }

        /// <summary>
        /// Update the menu to show the available paths.
        /// </summary>
        public void populatePaths()
        {
            menuSelectPath.Items.Clear();
            if (trackViewer.Paths == null) return;
            foreach (ORTS.Menu.Path path in trackViewer.Paths)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = makeHeader(path);
                menuItem.IsCheckable = false;
                menuItem.IsChecked = false;
                menuItem.Click += new RoutedEventHandler(menuSelectPath_Click);

                menuSelectPath.Items.Add(menuItem);
            }
            menuSelectPath.UpdateLayout();
        }


        /// <summary>
        /// Update the menu to make sure all the platforms are listed
        /// </summary>
        public void populatePlatforms()
        {
            if (trackViewer.drawTrackDB == null) return;
            if (trackViewer.drawTrackDB.platformLocations == null) return;
            menuPlatformCombobox.ItemsSource = trackViewer.drawTrackDB.platformLocations.Keys.OrderBy(a => a.ToString());
        }

        /// <summary>
        /// Update the menu to make sure all the sidings are listed
        /// </summary>
        public void populateSidings()
        {
            if (trackViewer.drawTrackDB == null) return;
            if (trackViewer.drawTrackDB.sidingLocations == null) return;
            menuSidingCombobox.ItemsSource = trackViewer.drawTrackDB.sidingLocations.Keys.OrderBy(a => a.ToString());
        }

        /// <summary>
        /// The user has selected a path. Find out which one and load it
        /// </summary>
        private void menuSelectPath_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;

            foreach (ORTS.Menu.Path path in trackViewer.Paths)
            {
                if (makeHeader(path) == (string)selectedMenuItem.Header)
                {
                    trackViewer.setPath(path);
                    trackViewer.pathEditor.EditingIsActive = menuEnableEditing.IsChecked;
                    UpdateMenuSettings();
                    return;
                }
            }
        }

        /// <summary>
        /// Convert a path (based on a .pat file) to a header for the menu
        /// </summary>
        /// <param name="path">The path containing name and filepath</param>
        /// <returns>string that can be used to defined menu header</returns>
        private static string makeHeader(ORTS.Menu.Path path)
        {
            string[] pathArr = path.FilePath.Split('\\');
            string fileName = pathArr.Last();
            return path.Name + " ( " + fileName + " )";
        }

        private void menuShortcuts_Click(object sender, RoutedEventArgs e)
        {
            string shortcuts = String.Empty;
            shortcuts += "A:  Shift left\n";
            shortcuts += "S:  Shift down\n";
            shortcuts += "D:  Shift right\n";
            shortcuts += "W:  Shift up\n";
            shortcuts += "C:  Shift to center of current path node\n";
            shortcuts += "PgUp:  Goto next path node\n";
            shortcuts += "PgDn:  Goto previous path node\n";
            shortcuts += "shift-PgUp:  Goto last path node\n";
            shortcuts += "shift-PgDn:  Goto first path node\n";
            shortcuts += "ctrl-Z: Undo in path editor\n";
            shortcuts += "ctrl-Y: Redo in path editor\n";
            MessageBox.Show(shortcuts);
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            string shortcuts = String.Empty;
            shortcuts += "This is ORTS TrackViewer, version " + TrackViewer.TrackViewerVersion + "\n";
            shortcuts += "It is a 'Contribution' to ORTS, and as such\n";
            shortcuts += "not an integral part of ORTS.";
            MessageBox.Show(shortcuts);
        }

        private void menuZoomSave_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.Save(trackViewer.CurrentRoute.Path);
        }

        private void menuZoomRestore_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.drawArea.Restore();
        }

        private void menuDoAntiAliasing_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.doAntiAliasing = menuDoAntiAliasing.IsChecked;
            Properties.Settings.Default.Save();
            trackViewer.setAliasing();
        }

        /// <summary>
        /// Toggle whether the sidings are shown
        /// </summary>
        public void menuToggleShowSidings()
        {
            menuShowSidingMarkers.IsChecked = !menuShowSidingMarkers.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the siding names are shown
        /// </summary>
        public void menuToggleShowSidingNames()
        {
            menuShowSidingNames.IsChecked = !menuShowSidingNames.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the platforms are shown
        /// </summary>
        public void menuToggleShowPlatforms()
        {
            menuShowPlatformMarkers.IsChecked = !menuShowPlatformMarkers.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the platform names are shown
        /// </summary>
        public void menuToggleShowPlatformNames()
        {
            menuShowPlatformNames.IsChecked = !menuShowPlatformNames.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the train path is shown
        /// </summary>
        public void menuToggleShowTrainpath()
        {
            menuShowTrainpath.IsChecked = !menuShowTrainpath.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the PATfile (.pat file) is shown
        /// </summary>
        public void menuToggleShowPATFile()
        {
            menuShowPATfile.IsChecked = !menuShowPATfile.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the signals are shown
        /// </summary>
        public void menuToggleShowSignals()
        {
            menuShowSignals.IsChecked = !menuShowSignals.IsChecked;
            UpdateMenuSettings();
        }
        
        /// <summary>
        /// Toggle whether the speedlimits are shown
        /// </summary>
        public void menuToggleShowSpeedLimits()
        {
            menuShowSpeedLimits.IsChecked = !menuShowSpeedLimits.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the mile posts are shown
        /// </summary>
        public void menuToggleShowMilePosts()
        {
            menuShowMileposts.IsChecked = !menuShowMileposts.IsChecked;
            UpdateMenuSettings();
        }

        private void menuPlatformCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string platformName = menuPlatformCombobox.SelectedItem as string;
            if (platformName == null) return;
            if (trackViewer.drawTrackDB.platformLocations.ContainsKey(platformName))
                trackViewer.drawArea.ShiftToLocation(trackViewer.drawTrackDB.platformLocations[platformName]);
        }

        private void menuSidingCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sidingName = menuSidingCombobox.SelectedItem as string;
            if (sidingName == null) return;
            if (trackViewer.drawTrackDB.sidingLocations.ContainsKey(sidingName))
                trackViewer.drawArea.ShiftToLocation(trackViewer.drawTrackDB.sidingLocations[sidingName]);

        }

        private void menuSearchTrackNode_Click(object sender, RoutedEventArgs e)
        {
            var searchControl = new SearchControl(trackViewer, SearchableItem.TrackNode);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void menuSearchTrackItem_Click(object sender, RoutedEventArgs e)
        {
            var searchControl = new SearchControl(trackViewer, SearchableItem.TrackItem);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void menuSearchTrackNodeRoad_Click(object sender, RoutedEventArgs e)
        {
            var searchControl = new SearchControl(trackViewer, SearchableItem.TrackNodeRoad);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void menuSearchTrackItemRoad_Click(object sender, RoutedEventArgs e)
        {
            var searchControl = new SearchControl(trackViewer, SearchableItem.TrackItemRoad);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }


        void PopulateColors(string preferenceBackgroundColor)
        {
            menuSelectColor.Items.Clear();

            foreach (string colorName in DrawColors.GetColorNames(preferenceBackgroundColor))
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = colorName;
                menuItem.IsCheckable = false;
                menuItem.IsChecked = false;
                menuItem.Click += new RoutedEventHandler(menuSelectColor_Click);

                menuSelectColor.Items.Add(menuItem);
            }
        }

        void menuSelectColor_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;
            string colorName = (string)selectedMenuItem.Header;
            if (DrawColors.SetBackGroundColor(colorName))
            {
                Properties.Settings.Default.backgroundColorName = colorName;
                UpdateMenuSettings();
            }

        }

 
        /// <summary>
        /// A certain item in the context menu has been clicked. Execute the corresponding action
        /// </summary>
        private void contextExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            trackViewer.pathEditor.ExecuteAction(menuItem.CommandParameter);
        }

        private void menuEnableEditing_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.pathEditor.EditingIsActive = menuEnableEditing.IsChecked;
            UpdateMenuSettings();
        }

        private void menuNewPath_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.pathEditor.NewPath();
            trackViewer.pathEditor.EditingIsActive = menuEnableEditing.IsChecked;
            UpdateMenuSettings();
        }

        private void menuSavePath_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.pathEditor.SavePath();
        }

        private void menuEditMetadata_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.pathEditor.EditMetaData();
        }

        private void menuKnownLimitations_Click(object sender, RoutedEventArgs e)
        {
            string limitations = String.Empty;
            limitations += "At least the following items are missing or incomplete\n";
            limitations += "* Handling broken points\n";
            limitations += "* Handling broken paths\n";
            limitations += "* Documentation\n";
            limitations += "\n";
            limitations += "The following items are planned additional functionalities not yet implemented\n";
            limitations += "* Give warning when discarding an edited path\n";
            limitations += "* Moving wait/uncouple/reverse/end/start points with mouse\n";
            limitations += "* Using mouse clicks for 'take other exit', 'remove siding', 'edit point'...\n";
            limitations += "\n";
            limitations += "Testing the save-paths with MSTS has not been done.\n";
            limitations += "Possible still open issues are (feedback would be nice)\n";
            limitations += "* Height of points (y-value) not saved/stored\n";
            limitations += "* Details of wait and (un)couple points might not make complete sense\n"; 
            limitations += "\n";
            MessageBox.Show(limitations);
        }

    }

}
