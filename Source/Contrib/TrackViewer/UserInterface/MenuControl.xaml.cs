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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

//using System.Windows.Forms;
using System.Windows.Forms.Integration;

using ORTS.TrackViewer.Drawing; // for colors
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
    public sealed partial class MenuControl : System.Windows.Controls.UserControl, IDisposable, IPreferenceChanger
    {
        /// <summary>Height of the menu in pixels</summary>
        public int MenuHeight { get; set; }

        private TrackViewer trackViewer;
        private ElementHost elementHost;
        private SaveableSettingsDictionary settingsDictionary = new SaveableSettingsDictionary();
        private OtherPathsWindow otherPathsWindow;
        private bool hasMouseItself;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="trackViewer">The trackviewer object we need for callbacks</param>
        public MenuControl(TrackViewer trackViewer)
        {
            this.trackViewer = trackViewer;
            InitializeComponent();
            MenuHeight = (int)menuMain.Height;

            //ElementHost object helps us to connect a WPF User Control.
            elementHost = new ElementHost
            {
                Location = new System.Drawing.Point(0, 0),
                TabIndex = 1,
                Child = this
            };
            System.Windows.Forms.Control.FromHandle(trackViewer.Window.Handle).Controls.Add(elementHost);

            InitUserSettings();

        }

        /// <summary>
        /// set the size of the menu control (also after rescaling)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetScreenSize(int width, int height)
        {
            elementHost.Size = new System.Drawing.Size(width, height);
        }

        /// <summary>
        /// Init various settings from the stored user settings.
        /// </summary>
        public void InitUserSettings()
        {
            menuShowInset.IsChecked = Properties.Settings.Default.showInset;
            menuShowWorldTiles.IsChecked = Properties.Settings.Default.showWorldTiles;
            menuShowGridLines.IsChecked = Properties.Settings.Default.showGridLines;
            menuColorTracks.IsChecked = Properties.Settings.Default.colorTracks;
            menuHighlightTracks.IsChecked = Properties.Settings.Default.showTrackHighlights;
            menuHighlightItems.IsChecked = Properties.Settings.Default.showItemHighlights;

            menuShowJunctionNodes.IsChecked = Properties.Settings.Default.showJunctionNodes;
            menuShowEndNodes.IsChecked = Properties.Settings.Default.showEndNodes;
            menuShowCrossovers.IsChecked = Properties.Settings.Default.showCrossovers;
            menuShowSidingMarkers.IsChecked = Properties.Settings.Default.showSidingMarkers;
            menuShowSidingNames.IsChecked = Properties.Settings.Default.showSidingNames;
            menuShowPlatformMarkers.IsChecked = Properties.Settings.Default.showPlatformMarkers;
            menuShowPlatformNames.IsChecked = Properties.Settings.Default.showPlatformNames;
            menuShowStationNames.IsChecked = Properties.Settings.Default.showStationNames;
            menuShowCrossings.IsChecked = Properties.Settings.Default.showCrossings;
            menuShowSpeedLimits.IsChecked = Properties.Settings.Default.showSpeedLimits;
            menuShowMileposts.IsChecked = Properties.Settings.Default.showMileposts;
            menuShowHazards.IsChecked = Properties.Settings.Default.showHazards;
            menuShowSignals.IsChecked = Properties.Settings.Default.showSignals;
            menuShowAllSignals.IsChecked = Properties.Settings.Default.showAllSignals;
            menuShowPickups.IsChecked = Properties.Settings.Default.showPickups;
            menuShowSoundRegions.IsChecked = Properties.Settings.Default.showSoundRegions;

            menuShowPATfile.IsChecked = Properties.Settings.Default.showPATfile;
            menuShowTrainpath.IsChecked = Properties.Settings.Default.showTrainpath;
            menuHighlightLastPathSection.IsChecked = Properties.Settings.Default.highlightLastPathSection;
            menuHighlightLastPathSection2.IsChecked = Properties.Settings.Default.highlightLastPathSection;
            menuShowCurrentEditorAction.IsChecked = Properties.Settings.Default.showEditorAction;
            menuShowCurrentEditorAction2.IsChecked = Properties.Settings.Default.showEditorAction;
            menuPgupExtendsPath.IsChecked = Properties.Settings.Default.pgupExtendsPath;
            menuPgupExtendsPath2.IsChecked = Properties.Settings.Default.pgupExtendsPath;

            menuStatusShowVectorSection.IsChecked = Properties.Settings.Default.statusShowVectorSections;
            menuStatusShowPATfile.IsChecked = Properties.Settings.Default.statusShowPATfile;
            menuStatusShowTrainpath.IsChecked = Properties.Settings.Default.statusShowTrainpath;
            menuStatusShowTerrain.IsChecked = Properties.Settings.Default.statusShowTerrain;
            menuStatusShowSignal.IsChecked = Properties.Settings.Default.statusShowSignal;
            menuStatusShowNames.IsChecked = Properties.Settings.Default.statusShowNames;


            menuDrawRoads.IsChecked = Properties.Settings.Default.drawRoads;
            menuShowCarSpawners.IsChecked = Properties.Settings.Default.showCarSpawners;
            menuShowRoadCrossings.IsChecked = Properties.Settings.Default.showRoadCrossings;

            menuShowScaleRuler.IsChecked = Properties.Settings.Default.showScaleRuler;
            menuShowLonLat.IsChecked = Properties.Settings.Default.showLonLat;
            menuUseMilesNotMeters.IsChecked = Properties.Settings.Default.useMilesNotMeters;

            menuZoomIsCenteredOnMouse.IsChecked = Properties.Settings.Default.zoomIsCenteredOnMouse;

            menuShowLabels.IsChecked = Properties.Settings.Default.showLabels;

            // Terrain should be off by default. We do not want to burden people with having this load always
            menuShowTerrain.IsChecked = false;
            menuShowDMTerrain.IsChecked = false;
            menuShowPatchLines.IsChecked = false;

            reductionNone.IsChecked = (Convert.ToInt32(reductionNone.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);
            reductionAuto.IsChecked = (Convert.ToInt32(reductionAuto.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);
            reduction2.IsChecked = (Convert.ToInt32(reduction2.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);
            reduction4.IsChecked = (Convert.ToInt32(reduction4.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);
            reduction8.IsChecked = (Convert.ToInt32(reduction8.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);
            reduction16.IsChecked = (Convert.ToInt32(reduction16.Tag.ToString()) == Properties.Settings.Default.terrainReductionFactor);

            UpdateMenuSettings();  // to be sure some other settings are done correctly

            menuDoAntiAliasing.IsChecked = Properties.Settings.Default.doAntiAliasing;
        }

        /// <summary>
        /// Update the (user) settings based on menu state
        /// </summary>
        void UpdateMenuSettings()
        {
            menuShowPATfile.IsEnabled = !menuEnableEditing.IsChecked;
            if (!menuShowPATfile.IsEnabled) {
                menuShowPATfile.IsChecked = false;
            }
            if (!menuShowSignals.IsChecked)
            {   // if signals are not shown, then also all signals cannot be shown.
                menuShowAllSignals.IsChecked = false;
            }
            if (menuShowStationNames.IsChecked)
            {
                menuShowPlatformNames.IsChecked = false;
            }

            if (menuShowTerrain.IsChecked || menuShowDMTerrain.IsChecked)
            {
                menuShowWorldTiles.IsChecked = false;
                menuShowWorldTiles.IsEnabled = false;
                menuShowPatchLines.IsEnabled = true;
            }
            else
            {
                menuShowWorldTiles.IsEnabled = true;
                menuShowPatchLines.IsEnabled = false;
            }

            Properties.Settings.Default.showInset = menuShowInset.IsChecked;
            Properties.Settings.Default.showWorldTiles = menuShowWorldTiles.IsChecked;
            Properties.Settings.Default.showGridLines = menuShowGridLines.IsChecked;
            Properties.Settings.Default.colorTracks = menuColorTracks.IsChecked;
            Properties.Settings.Default.showTrackHighlights = menuHighlightTracks.IsChecked;
            Properties.Settings.Default.showItemHighlights = menuHighlightItems.IsChecked;

            Properties.Settings.Default.showJunctionNodes = menuShowJunctionNodes.IsChecked;
            Properties.Settings.Default.showEndNodes = menuShowEndNodes.IsChecked;
            Properties.Settings.Default.showCrossovers = menuShowCrossovers.IsChecked;
            Properties.Settings.Default.showSidingMarkers = menuShowSidingMarkers.IsChecked;
            Properties.Settings.Default.showSidingNames = menuShowSidingNames.IsChecked;
            Properties.Settings.Default.showPlatformMarkers = menuShowPlatformMarkers.IsChecked;
            Properties.Settings.Default.showPlatformNames = menuShowPlatformNames.IsChecked;
            Properties.Settings.Default.showStationNames = menuShowStationNames.IsChecked;
            Properties.Settings.Default.showCrossings = menuShowCrossings.IsChecked;
            Properties.Settings.Default.showSpeedLimits = menuShowSpeedLimits.IsChecked;
            Properties.Settings.Default.showMileposts = menuShowMileposts.IsChecked;
            Properties.Settings.Default.showSignals = menuShowSignals.IsChecked;
            Properties.Settings.Default.showAllSignals = menuShowAllSignals.IsChecked;
            Properties.Settings.Default.showHazards = menuShowHazards.IsChecked;
            Properties.Settings.Default.showSoundRegions = menuShowSoundRegions.IsChecked;
            Properties.Settings.Default.showPickups = menuShowPickups.IsChecked;

            Properties.Settings.Default.showPATfile = menuShowPATfile.IsChecked;
            Properties.Settings.Default.showTrainpath = menuShowTrainpath.IsChecked;
            Properties.Settings.Default.highlightLastPathSection = menuHighlightLastPathSection.IsChecked;
            Properties.Settings.Default.showEditorAction = menuShowCurrentEditorAction.IsChecked;
            Properties.Settings.Default.pgupExtendsPath = menuPgupExtendsPath.IsChecked;

            Properties.Settings.Default.statusShowVectorSections = menuStatusShowVectorSection.IsChecked;
            Properties.Settings.Default.statusShowPATfile = menuStatusShowPATfile.IsChecked && menuShowPATfile.IsChecked;
            Properties.Settings.Default.statusShowTrainpath = menuStatusShowTrainpath.IsChecked && menuShowTrainpath.IsChecked;
            Properties.Settings.Default.statusShowTerrain = menuStatusShowTerrain.IsChecked && (menuShowTerrain.IsChecked || menuShowDMTerrain.IsChecked);
            Properties.Settings.Default.statusShowSignal = menuStatusShowSignal.IsChecked && menuShowSignals.IsChecked;
            Properties.Settings.Default.statusShowNames = menuStatusShowNames.IsChecked;

            Properties.Settings.Default.drawRoads = menuDrawRoads.IsChecked;
            Properties.Settings.Default.showCarSpawners = menuShowCarSpawners.IsChecked;
            Properties.Settings.Default.showRoadCrossings = menuShowRoadCrossings.IsChecked;

            Properties.Settings.Default.showScaleRuler = menuShowScaleRuler.IsChecked;
            Properties.Settings.Default.showLonLat = menuShowLonLat.IsChecked;
            Properties.Settings.Default.useMilesNotMeters = menuUseMilesNotMeters.IsChecked;

            Properties.Settings.Default.showLabels = menuShowLabels.IsChecked;
            Properties.Settings.Default.zoomIsCenteredOnMouse = menuZoomIsCenteredOnMouse.IsChecked;

            Properties.Settings.Default.Save();

            DrawColors.SetColoursFromOptions(menuColorTracks.IsChecked, menuShowWorldTiles.IsChecked, menuShowTerrain.IsChecked || menuShowDMTerrain.IsChecked);

            menuStatusShowPATfile.IsEnabled = menuShowPATfile.IsChecked;
            menuStatusShowTrainpath.IsEnabled = menuShowTrainpath.IsChecked;
            menuStatusShowTerrain.IsEnabled = menuShowTerrain.IsChecked || menuShowDMTerrain.IsChecked;
            menuStatusShowSignal.IsEnabled = menuShowSignals.IsChecked;

            menuSelectPath.IsEnabled = (trackViewer.CurrentRoute != null);
            menuNewPath.IsEnabled = (trackViewer.CurrentRoute != null);
            menuShowOtherPaths.IsEnabled = (trackViewer.CurrentRoute != null);
            menuSavePath.IsEnabled = (trackViewer.PathEditor != null);
            menuSaveStations.IsEnabled = (trackViewer.PathEditor != null);
            menuShowChart.IsEnabled = (trackViewer.PathEditor != null);
            menuEditMetadata.IsEnabled = menuEnableEditing.IsChecked;
            menuReversePath.IsEnabled = menuEnableEditing.IsChecked;
            menuExtendPath.IsEnabled = (trackViewer.PathEditor != null) && menuEnableEditing.IsChecked;
            menuAutoFixAllNodes.IsEnabled = menuEnableEditing.IsChecked && (trackViewer.PathEditor != null);
            menuAutoFixAllPaths.IsEnabled = (trackViewer.CurrentRoute != null);

            menuEnableEditing.IsEnabled = (trackViewer.PathEditor != null);
            menuEnableEditing2.IsEnabled = menuEnableEditing.IsEnabled;
            menuHighlightLastPathSection.IsEnabled = (trackViewer.PathEditor != null);
            menuHighlightLastPathSection2.IsEnabled = menuHighlightLastPathSection.IsEnabled;
            menuShowCurrentEditorAction.IsEnabled = menuEnableEditing.IsChecked;
            menuShowCurrentEditorAction2.IsEnabled = menuShowCurrentEditorAction.IsEnabled;
            menuPgupExtendsPath.IsEnabled = menuEnableEditing.IsChecked;
            menuPgupExtendsPath2.IsEnabled = menuEnableEditing.IsChecked;
        }

        private void MenuSetAllItems(bool isChecked)
        {
            menuShowJunctionNodes.IsChecked = isChecked;
            menuShowEndNodes.IsChecked = isChecked;
            menuShowCrossovers.IsChecked = isChecked;
            menuShowSidingMarkers.IsChecked = isChecked;
            menuShowSidingNames.IsChecked = isChecked;
            menuShowPlatformMarkers.IsChecked = isChecked;
            menuShowPlatformNames.IsChecked = false; // stationNames get preference
            menuShowStationNames.IsChecked = isChecked;
            menuShowCrossings.IsChecked = isChecked;
            menuShowSpeedLimits.IsChecked = isChecked;
            menuShowMileposts.IsChecked = isChecked;
            menuShowHazards.IsChecked = isChecked;
            menuShowSignals.IsChecked = isChecked;
            menuShowAllSignals.IsChecked = isChecked;
            menuShowPickups.IsChecked = isChecked;
            menuShowSoundRegions.IsChecked = isChecked;

            menuDrawRoads.IsChecked = isChecked;
            menuShowCarSpawners.IsChecked = isChecked;
            menuShowRoadCrossings.IsChecked = isChecked;

            UpdateMenuSettings();  // to be sure some other settings are done correctly
        }

        // only so it can be called as a callback
        private void UpdateMenuSettings(object sender, RoutedEventArgs e)
        {
            UpdateMenuSettings();
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.Quit();
        }

        private void MenuZoomIn_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.ZoomCentered(-4);
        }

        private void MenuZoomOut_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.ZoomCentered(4);
        }

        private void MenuZoomReset_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.ZoomReset(trackViewer.DrawTrackDB);
        }

        private void MenuZoomToTile_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.ZoomToTileCentered();
        }

        private void MenuInstallFolder_Click(object sender, RoutedEventArgs e)
        {
            bool newFolderWasInstalled = trackViewer.SelectInstallFolder();
            if (newFolderWasInstalled)
            {
                CloseOtherPathsWindow();
                PopulateRoutes();
            }
        }

        /// <summary>
        /// Update the menu to show the available routes
        /// </summary>
        public void PopulateRoutes()
        {
            menuSelectRoute.Items.Clear();
            if (trackViewer.Routes == null) return;
            foreach (ORTS.Menu.Route route in trackViewer.Routes)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = route.Name,
                    IsCheckable = false,
                    IsChecked = false
                };
                menuItem.Click += new RoutedEventHandler(MenuSelectRoute_Click);

                menuSelectRoute.Items.Add(menuItem);
            }
            menuSelectRoute.UpdateLayout();
        }

        /// <summary>
        /// The user has selected a route, figure out which one and load it.
        /// </summary>
        private void MenuSelectRoute_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;
            foreach (ORTS.Menu.Route route in trackViewer.Routes)
            {
                if (route.Name == (string)selectedMenuItem.Header)
                {
                    CloseOtherPathsWindow();
                    trackViewer.SetRoute(route);
                    UpdateMenuSettings();
                    return;
                }
            }
        }

        /// <summary>
        /// Load the last used route
        /// </summary>
        private void MenuReloadRoute_Click(object sender, RoutedEventArgs e)
        {
            CloseOtherPathsWindow();
            trackViewer.ReloadRoute();
            UpdateMenuSettings();
        }

        /// <summary>
        /// Update the menu to show the available paths.
        /// </summary>
        public void PopulatePaths()
        {
            if (trackViewer.Paths == null) return;
            List<string> paths = new List<string>();
            foreach (ORTS.Menu.Path path in trackViewer.Paths)
            {
                paths.Add(MakePathMenyEntryName(path));
            }
            paths.Insert(0, TrackViewer.catalog.GetString("<Select path>"));
            menuSelectPathCombobox.ItemsSource = paths;
            menuSelectPathCombobox.SelectedItem = menuSelectPathCombobox.Items.GetItemAt(0).ToString();
            menuExtendPathCombobox.ItemsSource = paths;
            menuExtendPathCombobox.SelectedItem = menuExtendPathCombobox.Items.GetItemAt(0).ToString();
        }

        /// <summary>
        /// Update the menu to make sure all the stations are listed
        /// </summary>
        public void PopulateStations()
        {
            
            if (trackViewer.DrawTrackDB == null) return;
            if (trackViewer.DrawTrackDB.StationLocations == null) return;
            List<string> stations = trackViewer.DrawTrackDB.StationLocations.Keys.OrderBy(a => a.ToString()).ToList();
            stations.Insert(0, TrackViewer.catalog.GetString("<Select station>"));
            menuStationCombobox.ItemsSource = stations;
            menuStationCombobox.SelectedItem = menuStationCombobox.Items.GetItemAt(0).ToString();
        }
        
        /// <summary>
        /// Update the menu to make sure all the platforms are listed
        /// </summary>
        public void PopulatePlatforms()
        {
            if (trackViewer.DrawTrackDB == null) return;
            if (trackViewer.DrawTrackDB.PlatformLocations == null) return;
            List<string> platforms = trackViewer.DrawTrackDB.PlatformLocations.Keys.OrderBy(a => a.ToString()).ToList();
            platforms.Insert(0, TrackViewer.catalog.GetString("<Select platform>"));
            menuPlatformCombobox.ItemsSource = platforms;
            menuPlatformCombobox.SelectedItem = menuPlatformCombobox.Items.GetItemAt(0).ToString();
        }

        /// <summary>
        /// Update the menu to make sure all the sidings are listed
        /// </summary>
        public void PopulateSidings()
        {
            if (trackViewer.DrawTrackDB == null) return;
            if (trackViewer.DrawTrackDB.SidingLocations == null) return;
            List<string> sidings = trackViewer.DrawTrackDB.SidingLocations.Keys.OrderBy(a => a.ToString()).ToList();
            sidings.Insert(0, TrackViewer.catalog.GetString("<Select siding>"));
            menuSidingCombobox.ItemsSource = sidings;
            menuSidingCombobox.SelectedItem = menuSidingCombobox.Items.GetItemAt(0).ToString();
        }

        private void MenuStationCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string stationName = menuStationCombobox.SelectedItem as string;
            if (stationName == null) return;
            if (trackViewer.DrawTrackDB.StationLocations.ContainsKey(stationName))
            {
                trackViewer.DrawArea.ShiftToLocation(trackViewer.DrawTrackDB.StationLocations[stationName]);
            }
        }

        private void MenuPlatformCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string platformName = menuPlatformCombobox.SelectedItem as string;
            if (platformName == null) return;
            if (trackViewer.DrawTrackDB.PlatformLocations.ContainsKey(platformName))
            {
                trackViewer.DrawArea.ShiftToLocation(trackViewer.DrawTrackDB.PlatformLocations[platformName]);
            }
        }

        private void MenuSidingCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sidingName = menuSidingCombobox.SelectedItem as string;
            if (sidingName == null) return;
            if (trackViewer.DrawTrackDB.SidingLocations.ContainsKey(sidingName))
            {
                trackViewer.DrawArea.ShiftToLocation(trackViewer.DrawTrackDB.SidingLocations[sidingName]);
            }

        }

        private void MenuItemCenterClosed(object sender, RoutedEventArgs e)
        {
            // If we close, we select the first item (which is only a header)
            if (menuStationCombobox.Items.Count > 0)
            {
                menuStationCombobox.SelectedItem = menuStationCombobox.Items.GetItemAt(0).ToString();
            } 
            if (menuPlatformCombobox.Items.Count > 0)
            {
                menuPlatformCombobox.SelectedItem = menuPlatformCombobox.Items.GetItemAt(0).ToString();
            }
            if (menuSidingCombobox.Items.Count > 0)
            {
                menuSidingCombobox.SelectedItem = menuSidingCombobox.Items.GetItemAt(0).ToString();
            }
            MenuNeedingMouseClosed(sender, e);
        }

        /// <summary>
        /// The user has selected a path. Find out which one and load it
        /// </summary>
        private void MenuSelectPathCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedPath = menuSelectPathCombobox.SelectedItem as string;
            if (selectedPath == null) return;
            foreach (ORTS.Menu.Path path in trackViewer.Paths)
            {
                if (MakePathMenyEntryName(path) == selectedPath)
                {
                    menuPathEditor.IsSubmenuOpen = false;
                    trackViewer.SetPath(path);
                    trackViewer.PathEditor.EditingIsActive = menuEnableEditing.IsChecked;
                    if (!menuShowPATfile.IsChecked)
                    {   // make sure path is visible either raw or (preferably) processed.
                        menuShowTrainpath.IsChecked = true;
                    }
                    UpdateMenuSettings();
                    return;
                }
            }
        }

        /// <summary>
        /// The user has selected a path. Find out which one and use it to extend the path
        /// </summary>
        private void MenuExtendPathCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedPath = menuExtendPathCombobox.SelectedItem as string;
            if (selectedPath == null) return;
            foreach (ORTS.Menu.Path path in trackViewer.Paths)
            {
                if (MakePathMenyEntryName(path) == selectedPath)
                {
                    menuPathEditor.IsSubmenuOpen = false;
                    trackViewer.PathEditor.ExtendWithPath(path);
                    UpdateMenuSettings();
                    menuExtendPathCombobox.SelectedIndex = 0;  // make sure we can select again next time.
                    return;
                }
            }
        }

        /// <summary>
        /// Convert a path (based on a .pat file) to a header for the menu
        /// </summary>
        /// <param name="path">The path containing name and filepath</param>
        /// <returns>string that can be used to defined menu header</returns>
        public static string MakePathMenyEntryName(ORTS.Menu.Path path)
        {
            string[] pathArr = path.FilePath.Split('\\');
            string fileName = pathArr.Last();
            return path.Name + " ( " + fileName + " )";
        }

        private void MenuShortcuts_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder shortcuts = new StringBuilder();
            shortcuts.Append(TrackViewer.catalog.GetString("ctrl-R\tReload the route\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("Q\tQuit\n"));
            shortcuts.Append("\n");
            shortcuts.Append(TrackViewer.catalog.GetString("=\tZoom-in (and keep zooming)\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-=\tZoom-in\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("-\tZoom-out (and keep zooming)\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift--\tZoom-out\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("Z\tZoom to tile\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("M\tToggle zoom center\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("R\tZoom reset\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("L\tAdd label\n"));
            shortcuts.Append("\n");
            shortcuts.Append(TrackViewer.catalog.GetString("shift-C\t\tShift center to current mouse location\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("left arrow\tShift left\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("down arrow\tShift down\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("right arrow\tShift right\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("up arrow\t\tShift up\n"));
            shortcuts.Append("\n");
            shortcuts.Append(TrackViewer.catalog.GetString("F5\tShow speed-limits\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F5\tShow mileposts\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F6\tShow terrain\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("ctrl-F6\tShow DM terrain\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F6\tShow terrain patch lines\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F7\tshow signals\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F8\tShow platforms\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F8\tShow platform-names\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F9\tShow sidings\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F9\tShow siding-names\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F10\tHighlight tracks\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F10\tHighlight items\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("F11\tShow path\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-F11\tShow raw path\n"));
            shortcuts.Append("\n");
            shortcuts.Append(TrackViewer.catalog.GetString("C\t\tShift to center of current path node\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("PgUp\t\tShow more of the path\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("PgDn\t\tShow less of the path\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-PgUp\tShow the full path\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("shift-PgDn\tShow only start point of path\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("E\t\tPlace end-point\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("W\t\tPlace a wait-point\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("ctrl-Z\t\tUndo in path editor\n"));
            shortcuts.Append(TrackViewer.catalog.GetString("ctrl-Y\t\tRedo in path editor\n"));
            shortcuts.Append("\n");
            shortcuts.Append(TrackViewer.catalog.GetString("alt-?\t\tVarious keys to open submenus\n"));
            MessageBox.Show(shortcuts.ToString(), TrackViewer.catalog.GetString("Keyboard shortcuts"));
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder about = new StringBuilder();
            about.Append(TrackViewer.catalog.GetString("This is ORTS TrackViewer, version "));
            about.Append(TrackViewer.TrackViewerVersion);
            about.Append("\n");
            about.Append(TrackViewer.catalog.GetString("It is a 'Contribution' to ORTS, and as such\n"));
            about.Append(TrackViewer.catalog.GetString("not an integral part of ORTS."));
            MessageBox.Show(about.ToString(), "TrackViewer");
        }

        private void MenuDocumentation_Click(object sender, RoutedEventArgs e)
        {
            string website = "http://openrails.org/learn/manual-and-tutorials/";
            StringBuilder documentation = new StringBuilder();
            documentation.Append(TrackViewer.catalog.GetString("Documentation is available"));
            documentation.Append(": ");
            documentation.Append(website);
            documentation.Append(" (");
            documentation.Append(TrackViewer.catalog.GetString("Right column → Open Rails Trackviewer"));
            documentation.Append(")\n");
            documentation.Append(TrackViewer.catalog.GetString("Do you want to go to the website?"));
            MessageBoxResult result = MessageBox.Show(documentation.ToString(),
                TrackViewer.catalog.GetString("Documentation"),
                MessageBoxButton.YesNoCancel);
            if (result.ToString() == "Yes")
            {
                System.Diagnostics.Process.Start(website);
            }
        }

        private void MenuZoomSave_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.Save(trackViewer.CurrentRoute.Path);
        }

        private void MenuZoomRestore_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.DrawArea.Restore();
        }

        private void MenuDoAntiAliasing_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.doAntiAliasing = menuDoAntiAliasing.IsChecked;
            Properties.Settings.Default.Save();
            trackViewer.SetAliasing();
        }

        /// <summary>
        /// Toggle whether zooming is around mouse or centered
        /// </summary>
        public void MenuToggleZoomingAroundMouse()
        {
            menuZoomIsCenteredOnMouse.IsChecked = !menuZoomIsCenteredOnMouse.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the sidings are shown
        /// </summary>
        public void MenuToggleShowSidings()
        {
            menuShowSidingMarkers.IsChecked = !menuShowSidingMarkers.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the siding names are shown
        /// </summary>
        public void MenuToggleShowSidingNames()
        {
            menuShowSidingNames.IsChecked = !menuShowSidingNames.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the platforms are shown
        /// </summary>
        public void MenuToggleShowPlatforms()
        {
            menuShowPlatformMarkers.IsChecked = !menuShowPlatformMarkers.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Circulate from station names, to platform names to neither
        /// </summary>
        public void MenuCirculatePlatformStationNames()
        {
            if (menuShowStationNames.IsChecked)
            {
                menuShowStationNames.IsChecked = false;
                menuShowPlatformNames.IsChecked = true;
            }
            else if (menuShowPlatformNames.IsChecked)
            {
                menuShowStationNames.IsChecked = false;
                menuShowPlatformNames.IsChecked = false;
            }
            else
            {
                menuShowStationNames.IsChecked = true;
                menuShowPlatformNames.IsChecked = false;
            }
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the train path is shown
        /// </summary>
        public void MenuToggleShowTrainpath()
        {
            menuShowTrainpath.IsChecked = !menuShowTrainpath.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the PATfile (.pat file) is shown
        /// </summary>
        public void MenuToggleShowPatFile()
        {
            menuShowPATfile.IsChecked = !menuShowPATfile.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether highlighting of tracks is done
        /// </summary>
        public void MenuToggleHighlightTracks()
        {
            menuHighlightTracks.IsChecked = !menuHighlightTracks.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether highlighting of track-itemss is done
        /// </summary>
        public void MenuToggleHighlightItems()
        {
            menuHighlightItems.IsChecked = !menuHighlightItems.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the signals are shown
        /// </summary>
        public void MenuToggleShowSignals()
        {
            menuShowSignals.IsChecked = !menuShowSignals.IsChecked;
            if (!menuShowSignals.IsChecked)
            {
                menuShowAllSignals.IsChecked = false;
            }
            UpdateMenuSettings();
        }
        
        /// <summary>
        /// Toggle whether the speedlimits are shown
        /// </summary>
        public void MenuToggleShowSpeedLimits()
        {
            menuShowSpeedLimits.IsChecked = !menuShowSpeedLimits.IsChecked;
            UpdateMenuSettings();
        }

        /// <summary>
        /// Toggle whether the mile posts are shown
        /// </summary>
        public void MenuToggleShowMilePosts()
        {
            menuShowMileposts.IsChecked = !menuShowMileposts.IsChecked;
            UpdateMenuSettings();
        }

        #region Terrain
        /// <summary>
        /// Toggle whether the terrain is shown or not
        /// </summary>
        public void MenuToggleShowTerrain()
        {
            menuShowTerrain.IsChecked = !menuShowTerrain.IsChecked;
            MenuShowTerrain_Click(null, null);
        }

        /// <summary>
        /// Toggle whether the Distance Mountain terrain is shown or not
        /// </summary>
        public void MenuToggleShowDMTerrain()
        {
            menuShowDMTerrain.IsChecked = !menuShowDMTerrain.IsChecked;
            MenuShowTerrain_Click(null, null);
        }

        /// <summary>
        /// Set whether the terrain is shown or not
        /// </summary>
        /// <param name="show">Set to true if you want to show the terrain</param>
        public void MenuSetShowTerrain(bool show)
        {
            menuShowTerrain.IsChecked = show;
            if (!show)
            {
                menuShowPatchLines.IsChecked = false;
            }
            MenuShowTerrain_Click(null, null);
        }

        /// <summary>
        /// Set whether the Distance Mountain terrain is shown or not
        /// </summary>
        /// <param name="show">Set to true if you want to show the terrain</param>
        public void MenuSetShowDMTerrain(bool show)
        {
            menuShowDMTerrain.IsChecked = show;
            MenuShowTerrain_Click(null, null);
        }

        private void MenuShowTerrain_Click(object sender, RoutedEventArgs e)
        {
            UpdateMenuSettings();
            bool succeeded = trackViewer.SetTerrainVisibility(menuShowTerrain.IsChecked, menuShowDMTerrain.IsChecked);
            if (!succeeded)
            {
                if (menuShowTerrain.IsChecked == true)
                {
                    MenuSetShowTerrain(false);
                }
                if (menuShowDMTerrain.IsChecked == true)
                {
                    MenuSetShowDMTerrain(false);
                }
            }
        }

        /// <summary>
        /// Toggle whether the patchlines of the terrain are shown or not
        /// </summary>
        public void MenuToggleShowPatchLines()
        {
            menuShowPatchLines.IsChecked = !menuShowPatchLines.IsChecked;
            MenuShowPatchLines_Click(null, null);
        }

        private void MenuShowPatchLines_Click(object sender, RoutedEventArgs e)
        {
            UpdateMenuSettings();
            trackViewer.SetPatchLineVisibility(menuShowPatchLines.IsChecked);
        }

        private void TerrainReductionOption_CheckChanged(object sender, RoutedEventArgs e)
        {
            RadioButton reductionOptionButton = sender as RadioButton;
            object tag = reductionOptionButton.Tag;
            var value = Convert.ToInt32(tag.ToString());
            Properties.Settings.Default.terrainReductionFactor = value;
            UpdateMenuSettings();
            trackViewer.SetTerrainReduction();
            menuTerrain.IsSubmenuOpen = false;
        }
        #endregion

        private void MenuSearchTrackNode_Click(object sender, RoutedEventArgs e)
        {
            SearchControl searchControl = new SearchControl(trackViewer, SearchableItem.TrackNode);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void MenuSearchTrackItem_Click(object sender, RoutedEventArgs e)
        {
            SearchControl searchControl = new SearchControl(trackViewer, SearchableItem.TrackItem);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void MenuSearchTrackNodeRoad_Click(object sender, RoutedEventArgs e)
        {
            SearchControl searchControl = new SearchControl(trackViewer, SearchableItem.TrackNodeRoad);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }

        private void MenuSearchTrackItemRoad_Click(object sender, RoutedEventArgs e)
        {
            SearchControl searchControl = new SearchControl(trackViewer, SearchableItem.TrackItemRoad);
            TrackViewer.Localize(searchControl);
            searchControl.ShowDialog();
        }


        /// <summary>
        /// Add a new preference in the form of a string. The list of allowed options should also be given.
        /// The callback should be called whenever the option is changed (or possibly when the default is initialized).
        /// </summary>
        /// <param name="name">Name of the preference used for indexing within the program</param>
        /// <param name="description">Description of the preference used to present it to the user</param>
        /// <param name="options">The options a preference can take</param>
        /// <param name="defaultOption">The default option.</param>
        /// <param name="callback">The callback that will be called upon a change in the preference</param>
        public void AddStringPreference(string name, string description, string[] options, string defaultOption, StringPreferenceDelegate callback)
        {
            MenuItem preferenceItem = new MenuItem
            {
                Header = description
            };
            foreach (string option in options)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = option,
                    IsCheckable = false,
                    IsChecked = false,
                    CommandParameter = new PreferenceData { Callback = callback, Name = name }
                };
                menuItem.Click += new RoutedEventHandler(StringPreference_Click);

                preferenceItem.Items.Add(menuItem);
            }
            menuPreferences.Items.Add(preferenceItem);

            //See if there is a preference
            if (settingsDictionary.ContainsKey(name))
            {
                callback(settingsDictionary[name]);
            }
        }

        private void StringPreference_Click(object sender, RoutedEventArgs e)
        {
            MenuItem callingMenu = sender as MenuItem;
            PreferenceData preferenceData = callingMenu.CommandParameter as PreferenceData;
            StringPreferenceDelegate callback = preferenceData.Callback;
            string selectedOption = callingMenu.Header.ToString();
            callback(selectedOption);

            //Store the preference
            settingsDictionary[preferenceData.Name] = selectedOption;
            settingsDictionary.Save();
        }

        /// <summary>
        /// Enable editing and make sure this is visible in the menu as well.
        /// </summary>
        public void SetEnableEditing()
        {
            menuEnableEditing.IsChecked = true;
            MenuEnableEditing_Click(null, null);
        }

        private void MenuEnableEditing_Click(object sender, RoutedEventArgs e)
        {
            menuEnableEditing2.IsChecked = menuEnableEditing.IsChecked;
            trackViewer.PathEditor.EditingIsActive = menuEnableEditing.IsChecked;
            UpdateMenuSettings();
        }

        private void MenuEnableEditing2_Click(object sender, RoutedEventArgs e)
        {
            menuEnableEditing.IsChecked = menuEnableEditing2.IsChecked;
            MenuEnableEditing_Click(null, null);
        }

        private void MenuNewPath_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.NewPath();
            menuEnableEditing.IsChecked = true;
            menuShowTrainpath.IsChecked = true;
            UpdateMenuSettings();
        }

        private void MenuReversePath_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.ReversePath();
            UpdateMenuSettings();
        }

        private void MenuSavePath_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.PathEditor.SavePath();
        }


        private void MenuSaveStations_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.PathEditor.SaveStationNames();
        }

        private void MenuEditMetadata_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.EditMetaData();
        }

        private void MenuAutoRestorePaths_Click(object sender, RoutedEventArgs e)
        {
            trackViewer.AutoRestorePaths();
        }

        #region IDisposable
        private bool disposed;
        /// <summary>
        /// Implementing IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    disposed = true; // to prevent infinite loop. Probably elementHost should not be part of this class
                    elementHost.Dispose();
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
        #endregion

        private void MenuShowAll_Click(object sender, RoutedEventArgs e)
        {
            MenuSetAllItems(true);
        }

        private void MenuShowNone_Click(object sender, RoutedEventArgs e)
        {
            MenuSetAllItems(false);
        }

        private void MenuShowAllSignals_Click(object sender, RoutedEventArgs e)
        {
            // if someone selects all signals, also normal signals should be turned on.
            if (menuShowAllSignals.IsChecked)
            {
                menuShowSignals.IsChecked = true;
            }
            UpdateMenuSettings();
        }

        private void MenuNeedingMouseOpened(object sender, RoutedEventArgs e)
        {
            this.hasMouseItself = true;
        }

        private void MenuNeedingMouseClosed(object sender, RoutedEventArgs e)
        {
            this.hasMouseItself = false;
        }

        /// <summary>
        /// Determine whether the menu or a child window has captured the mouse for its actions
        /// </summary>
        public bool HasMouse()
        {
            bool otherPathsWindowHasMouse = (this.otherPathsWindow) != null && this.otherPathsWindow.IsActive;
            return this.hasMouseItself || otherPathsWindowHasMouse;
        }

        /// <summary>
        /// For preference that can be added from other places, we need to store both a callback and a name
        /// </summary>
        class PreferenceData
        {
            /// <summary>The callback for a preference</summary>
            public StringPreferenceDelegate Callback;
            /// <summary>The name for a preference that can be used as programming index</summary>
            public string Name;
        }

        /// <summary>
        /// Populate the combobox for languages
        /// </summary>
        public void PopulateLanguages()
        {
            comboBoxLanguage.ItemsSource = trackViewer.LanguageManager.Languages;
            comboBoxLanguage.SelectedValue = LanguageManager.CurrentLanguageCode; 
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Language selectedLanguage = comboBoxLanguage.SelectedItem as Language;
            trackViewer.SelectLanguage(selectedLanguage.Code);
        }

        private void MenuShowOtherPaths_Click(object sender, RoutedEventArgs e)
        {
            otherPathsWindow = new OtherPathsWindow(trackViewer.DrawMultiplePaths);
            TrackViewer.Localize(otherPathsWindow);
            otherPathsWindow.Show();
        }

        private void CloseOtherPathsWindow()
        {
            if (otherPathsWindow != null)
            {
                otherPathsWindow.Close();
            }
        }

        private void MenuShowChart_Click(object sender, RoutedEventArgs e)
        {
            this.trackViewer.ShowPathChart();
        }

        private void MenuAutoFixAllNodes_Click(object sender, RoutedEventArgs e)
        {
            this.trackViewer.PathEditor.AutoFixAllBrokenNodes();
        }

        private void MenuLoadLabels_Click(object sender, RoutedEventArgs e)
        {
            this.trackViewer.LoadLabels();
        }

        private void MenuSaveLabels_Click(object sender, RoutedEventArgs e)
        {
            this.trackViewer.SaveLabels();
        }

        private void MenuHighlightLastPathSection_Click(object sender, RoutedEventArgs e)
        {
            menuHighlightLastPathSection2.IsChecked = menuHighlightLastPathSection.IsChecked;
            UpdateMenuSettings();
        }

        private void MenuHighlightLastPathSection2_Click(object sender, RoutedEventArgs e)
        {
            menuHighlightLastPathSection.IsChecked = menuHighlightLastPathSection2.IsChecked;
            MenuHighlightLastPathSection_Click(null, null);
        }

        private void MenuShowCurrentEditorAction_Click(object sender, RoutedEventArgs e)
        {
            menuShowCurrentEditorAction2.IsChecked = menuShowCurrentEditorAction.IsChecked;
            UpdateMenuSettings();
        }

        private void MenuShowCurrentEditorAction2_Click(object sender, RoutedEventArgs e)
        {
            menuShowCurrentEditorAction.IsChecked = menuShowCurrentEditorAction2.IsChecked;
            MenuShowCurrentEditorAction_Click(null, null);
        }

        private void MenuPgupExtendsPathAction_Click(object sender, RoutedEventArgs e)
        {
            menuPgupExtendsPath2.IsChecked = menuPgupExtendsPath.IsChecked;
            UpdateMenuSettings();
        }

        private void MenuPgupExtendsPathAction2_Click(object sender, RoutedEventArgs e)
        {
            menuPgupExtendsPath.IsChecked = menuPgupExtendsPath2.IsChecked;
            MenuPgupExtendsPathAction_Click(null, null);
        }
    }

    #region IPreferenceChanger
    /// <summary>
    /// Delegate to enable callbacks for preference choices
    /// </summary>
    /// <param name="chosenOption">The option of the preferences that was chosen</param>
    public delegate void StringPreferenceDelegate(string chosenOption);

    /// <summary>
    /// This interface is intended to support various ways in preferences can be changed.
    /// By having a common interface implementation and definition are separated and hence more flexible
    /// </summary>
    public interface IPreferenceChanger
    {
        /// <summary>
        /// Add a new preference in the form of a string. The list of allowed options should also be given.
        /// The callback should be called whenever the option is changed (or possibly when the default is initialized).
        /// </summary>
        /// <param name="name">Name of the preference used for indexing within the program</param>
        /// <param name="description">Description of the preference used to present it to the user</param>
        /// <param name="options">The options a preference can take</param>
        /// <param name="defaultOption">The default option.</param>
        /// <param name="callback">The callback that will be called upon a change in the preference</param>
        void AddStringPreference(string name, string description, string[] options, string defaultOption, StringPreferenceDelegate callback);
    }
    #endregion 

    #region SaveableSettingsDictionary
    /// <summary>
    /// Dictionary that supports saving to stored user settings.
    /// </summary>
    class SaveableSettingsDictionary : Dictionary<string,string>
    {
        /// <summary>
        /// Constructor. Also loads the values from stored settings.
        /// </summary>
        public SaveableSettingsDictionary()
        {
            if (Properties.Settings.Default.preferences == null)
            {
                Properties.Settings.Default.preferences = new StringCollection();
            }
            Dictionary<string, string> hiddenDictionary = ToDictionary(Properties.Settings.Default.preferences);
            foreach (KeyValuePair<string, string> kvp in hiddenDictionary)
            {
                this.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Save to stored settings
        /// </summary>
        public void Save()
        {
            Properties.Settings.Default.preferences = ToStringCollection(this);
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Translate a StringCollection containing key, value pairs into a dictionary.
        /// </summary>
        /// <param name="stringCollection"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ToDictionary(StringCollection stringCollection)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < stringCollection.Count - 1; i += 2) // the -1 to prevent problems when the stringCollection has odd number of elements.
            {
                result.Add(stringCollection[i], stringCollection[i + 1]);
            }
            return result;
        }

        /// <summary>
        /// Translate from a dictionary to a StringCollection (organized in key, value pairs)
        /// </summary>
        /// <param name="dictionary">The dictionary to translate</param>
        private static StringCollection ToStringCollection(Dictionary<string, string> dictionary)
        {
            StringCollection result = new StringCollection();
            foreach (KeyValuePair<string,string> kvp in dictionary)
            {
                result.Add(kvp.Key);
                result.Add(kvp.Value);
            }
            return result;
        }
    }
    #endregion
}
