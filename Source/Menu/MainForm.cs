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

using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Common;
using ORTS.Formats;
using ORTS.Menu;
using ORTS.Settings;
using ORTS.Updater;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Path = ORTS.Menu.Path;

namespace ORTS
{
    public partial class MainForm : Form
    {
        public enum UserAction
        {
            SingleplayerNewGame,
            SingleplayerResumeSave,
            SingleplayerReplaySave,
            SingleplayerReplaySaveFromSave,
            MultiplayerServer,
            MultiplayerClient,
            SinglePlayerTimetableGame,
            SinglePlayerResumeTimetableGame,
        }

        bool Initialized;
        UserSettings Settings;
        List<Folder> Folders = new List<Folder>();
        public List<Route> Routes = new List<Route>();
        List<Activity> Activities = new List<Activity>();
        List<Consist> Consists = new List<Consist>();
        List<Path> Paths = new List<Path>();
        List<TimetableInfo> ORTimeTables = new List<TimetableInfo>();
        Task<List<Folder>> FolderLoader;
        Task<List<Route>> RouteLoader;
        Task<List<Activity>> ActivityLoader;
        Task<List<Consist>> ConsistLoader;
        Task<List<Path>> PathLoader;
        Task<List<TimetableInfo>> ORTimeTableLoader;
        readonly ResourceManager Resources = new ResourceManager("ORTS.Properties.Resources", typeof(MainForm).Assembly);
        readonly UpdateManager UpdateManager;
        readonly Image ElevationIcon;

        internal string RunActivityProgram
        {
            get
            {
                var programNormal = System.IO.Path.Combine(Application.StartupPath, "RunActivity.exe");
                var programLAA = System.IO.Path.Combine(Application.StartupPath, "RunActivityLAA.exe");
                if (Settings.UseLargeAddressAware && File.Exists(programLAA))
                    return programLAA;
                return programNormal;
            }
        }

        public Folder SelectedFolder { get { return (Folder)comboBoxFolder.SelectedItem; } }
        public Route SelectedRoute { get { return (Route)comboBoxRoute.SelectedItem; } }
        public Activity SelectedActivity { get { return (Activity)comboBoxActivity.SelectedItem; } }
        public Consist SelectedConsist { get { return (Consist)comboBoxConsist.SelectedItem; } }
        public Path SelectedPath { get { return (Path)comboBoxHeadTo.SelectedItem; } }
        public string SelectedStartTime { get { return comboBoxStartTime.Text; } }
        public int SelectedStartSeason { get { return comboBoxStartSeason.SelectedIndex; } }
        public int SelectedStartWeather { get { return comboBoxStartWeather.SelectedIndex; } }
        public string SelectedSaveFile { get; set; }
        public UserAction SelectedAction { get; set; }
        public TimetableInfo SelectedTimetable { get { return (TimetableInfo) comboBoxTimetable.SelectedItem; } }
        public String SelectedPlayerTimetable { get { return (String) comboBoxPlayerTimetable.SelectedItem; } }
        public TTPreInfo.TTTrainPreInfo SelectedTimetableTrain { get { return (TTPreInfo.TTTrainPreInfo)comboBoxPlayerTrain.SelectedItem; } }
        public Consist SelectedTimetableConsist;
        public Path SelectedTimetablePath;

        GettextResourceManager catalog = new GettextResourceManager("Menu");

        #region Main Form
        public MainForm()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            // Set title to show revision or build info.
            Text = String.Format(VersionInfo.Version.Length > 0 ? "{0} {1}" : "{0} build {2}", Application.ProductName, VersionInfo.Version, VersionInfo.Build);
#if DEBUG
            Text = Text + " (debug)";
#endif
            panelModeTimetable.Location = panelModeActivity.Location;
            ShowDetails();
            UpdateEnabled();
            UpdateManager = new UpdateManager(System.IO.Path.GetDirectoryName(Application.ExecutablePath), Application.ProductName, VersionInfo.VersionOrBuild);
            ElevationIcon = new Icon(SystemIcons.Shield, SystemInformation.SmallIconSize).ToBitmap();
        }

        void MainForm_Shown(object sender, EventArgs e)
        {
            var options = Environment.GetCommandLineArgs().Where(a => (a.StartsWith("-") || a.StartsWith("/"))).Select(a => a.Substring(1));
            Settings = new UserSettings(options);

            LoadOptions();
            LoadLanguage();

            if (!Initialized)
            {
                var Seasons = new[] {
                    catalog.GetString("Spring"),
                    catalog.GetString("Summer"),
                    catalog.GetString("Autumn"),
                    catalog.GetString("Winter"),
                };
                var Weathers = new[] {
                    catalog.GetString("Clear"),
                    catalog.GetString("Snow"),
                    catalog.GetString("Rain"),
                };
                var Difficulties = new[] {
                    catalog.GetString("Easy"),
                    catalog.GetString("Medium"),
                    catalog.GetString("Hard"),
                    "",
                };
                var Days = new[] {
                    catalog.GetString("Monday"),
                    catalog.GetString("Tuesday"),
                    catalog.GetString("Wednesday"),
                    catalog.GetString("Thursday"),
                    catalog.GetString("Friday"),
                    catalog.GetString("Saturday"),
                    catalog.GetString("Sunday"),
                };

                comboBoxStartSeason.Items.AddRange(Seasons);
                comboBoxStartWeather.Items.AddRange(Weathers);
                comboBoxDifficulty.Items.AddRange(Difficulties);

                comboBoxTimetableSeason.Items.AddRange(Seasons);
                comboBoxTimetableWeather.Items.AddRange(Weathers);
                comboBoxTimetableDay.Items.AddRange(Days);

                var coreExecutables = new[] {
                    "OpenRails.exe",
                    "Menu.exe",
                    "RunActivity.exe",
                    "RunActivityLAA.exe",
                    "Updater.exe",
                };
                var tools = new List<ToolStripItem>();
                foreach (var executable in Directory.GetFiles(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "*.exe"))
                {
                    // Don't show any of the core parts of the application.
                    if (coreExecutables.Contains(System.IO.Path.GetFileName(executable)))
                        continue;

                    var toolInfo = FileVersionInfo.GetVersionInfo(executable);

                    // Skip any executable that isn't part of this product (e.g. Visual Studio hosting files).
                    if (toolInfo.ProductName != Application.ProductName)
                        continue;

                    // Remove the product name from the tool's name and localise.
                    var toolName = catalog.GetString(toolInfo.FileDescription.Replace(Application.ProductName, "").Trim());

                    // Create menu item to execute tool.
                    tools.Add(new ToolStripMenuItem(toolName, null, (Object sender2, EventArgs e2) =>
                    {
                        var toolPath = (sender2 as ToolStripItem).Tag as string;
                        var toolIsConsole = false;
                        using (var reader = new BinaryReader(File.OpenRead(toolPath)))
                        {
                            toolIsConsole = GetImageSubsystem(reader) == ImageSubsystem.WindowsConsole;
                        }
                        if (toolIsConsole)
                            Process.Start("cmd", "/k \"" + toolPath + "\"");
                        else
                            Process.Start(toolPath);
                    }) { Tag = executable });
                }
                // Add all the tools in alphabetical order.
                contextMenuStripTools.Items.AddRange((from tool in tools
                                                      orderby tool.Text
                                                      select tool).ToArray());
            }

            ShowEnvironment();

            CheckForUpdate();

            if (!Initialized)
            {
                Initialized = true;
                LoadFolderList();
            }
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveOptions();
            if (RouteLoader != null)
                RouteLoader.Cancel();
            if (ActivityLoader != null)
                ActivityLoader.Cancel();
            if (ConsistLoader != null)
                ConsistLoader.Cancel();
            if (PathLoader != null)
                PathLoader.Cancel();
            if (ORTimeTableLoader != null)
                ORTimeTableLoader.Cancel();

            // Remove any deleted saves
			if (Directory.Exists(UserSettings.DeletedSaveFolder))
				Directory.Delete(UserSettings.DeletedSaveFolder, true);   // true removes all contents as well as folder

            // Tidy up after versions which used SAVE.BIN
			var file = UserSettings.UserDataFolder + @"\SAVE.BIN";
            if (File.Exists(file))
                File.Delete(file);
        }

        void CheckForUpdate()
        {
            // This is known directly from the chosen channel so doesn't need to wait for the update check itself.
            linkLabelChangeLog.Visible = !string.IsNullOrEmpty(UpdateManager.ChangeLogLink);

            new Task<UpdateManager>(this, () =>
            {
                UpdateManager.Check();
                return null;
            }, _ =>
            {
                if (UpdateManager.LastCheckError != null)
                    linkLabelUpdate.Text = catalog.GetString("Update check failed");
                else if (UpdateManager.LastUpdate != null && UpdateManager.LastUpdate.Version != VersionInfo.Version)
                    linkLabelUpdate.Text = catalog.GetStringFmt("Update to {0}", UpdateManager.LastUpdate.Version);
                else
                    linkLabelUpdate.Text = "";
                linkLabelUpdate.Enabled = true;
                linkLabelUpdate.Visible = linkLabelUpdate.Text.Length > 0;
                // Update link's elevation icon and size/position.
                if (UpdateManager.LastCheckError == null && UpdateManager.LastUpdate != null && UpdateManager.LastUpdate.Version != VersionInfo.Version && UpdateManager.UpdaterNeedsElevation)
                    linkLabelUpdate.Image = ElevationIcon;
                else
                    linkLabelUpdate.Image = null;
                linkLabelUpdate.AutoSize = true;
                linkLabelUpdate.Left = linkLabelChangeLog.Right - linkLabelUpdate.Width - ElevationIcon.Width;
                linkLabelUpdate.AutoSize = false;
                linkLabelUpdate.Width = linkLabelChangeLog.Right - linkLabelUpdate.Left;
            });
        }

        void LoadLanguage()
        {
            if (Settings.Language.Length > 0)
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
                }
                catch { }
            }

            Localizer.Localize(this, catalog);
        }
        #endregion

        #region Folders
        void comboBoxFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadRouteList();
            LoadLocomotiveList();
            ShowDetails();
        }

        void buttonFolderAdd_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = SelectedFolder != null ? SelectedFolder.Path : "";
                folderBrowser.Description = catalog.GetString("Select a the installation profile (MSTS folder) to add:");
                folderBrowser.ShowNewFolderButton = false;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    using (var form = new FolderForm())
                    {
                        form.Folder = new Folder(System.IO.Path.GetFileName(folderBrowser.SelectedPath), folderBrowser.SelectedPath);
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            Folders.Add(form.Folder);
                            Settings.Menu_Selection = new[] { form.Folder.Path, null, null };
                            SaveFolderList();
                            LoadFolderList();
                        }
                    }
                }
            }
        }

        void buttonFolderEdit_Click(object sender, EventArgs e)
        {
            using (var form = new FolderForm())
            {
                form.Folder = SelectedFolder;
                if (form.ShowDialog() == DialogResult.OK)
                {
                    SaveOptions();
                    Folders.Remove(SelectedFolder);
                    Folders.Add(form.Folder);
                    SaveFolderList();
                    LoadFolderList();
                }
            }
        }

        void buttonFolderRemove_Click(object sender, EventArgs e)
        {
            var folder = SelectedFolder;
            if (MessageBox.Show(catalog.GetString("Path: ") + folder.Path + catalog.GetString("\nName: ") + folder.Name + catalog.GetString("\n\nRemove this installation profile from Open Rails?"), Application.ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Folders.Remove(folder);
                SaveFolderList();
                LoadFolderList();
            }
        }
        #endregion

        #region Routes
        void comboBoxRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadActivityList();
            LoadStartAtList();
            LoadORTimeTableList();
            ShowDetails();
        }
        #endregion

        #region Mode
        void radioButtonMode_CheckedChanged(object sender, EventArgs e)
        {
            panelModeActivity.Visible = radioButtonModeActivity.Checked;
            panelModeTimetable.Visible = radioButtonModeTimetable.Checked;
            SelectedAction = radioButtonModeTimetable.Checked ? UserAction.SinglePlayerTimetableGame : UserAction.SingleplayerNewGame;
            ShowDetails();
        }
        #endregion

        #region Activities
        void comboBoxActivity_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxTimetable.SelectedItem = null;
            ShowLocomotiveList();
            ShowConsistList();
            ShowStartAtList();
            ShowEnvironment();
            ShowDetails();
        }
        #endregion

        #region Locomotives
        void comboBoxLocomotive_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowConsistList();
            ShowDetails();
        }
        #endregion

        #region Consists
        void comboBoxConsist_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExploreActivity();
            ShowDetails();
        }
        #endregion

        #region Starting from
        void comboBoxStartAt_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowHeadToList();
        }
        #endregion

        #region Heading to
        void comboBoxHeadTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExploreActivity();
        }
        #endregion

        #region Environment
        void comboBoxStartTime_TextChanged(object sender, EventArgs e)
        {
            UpdateExploreActivity();
        }

        void comboBoxStartSeason_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExploreActivity();
        }

        void comboBoxStartWeather_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExploreActivity();
        }
        #endregion

        #region Multiplayer
        void textBoxMPUser_TextChanged(object sender, EventArgs e)
        {
            UpdateEnabled();
        }

        bool CheckUserName(string text)
        {
            string tmp = text;
            if (tmp.Length < 4 || tmp.Length > 10 || tmp.Contains("\"") || tmp.Contains("\'") || tmp.Contains(" ") || tmp.Contains("-") || Char.IsDigit(tmp, 0))
            {
                MessageBox.Show(catalog.GetString("User name must be 4-10 characters long, cannot contain space, ', \" or - and must not start with a digit."), Application.ProductName);
                return false;
            }
            return true;
        }

        #endregion

        #region Misc. buttons and options
        void linkLabelUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (UpdateManager.LastCheckError != null)
            {
                MessageBox.Show(catalog.GetStringFmt("The update check failed due to an error:\n\n{0}", UpdateManager.LastCheckError), Application.ProductName);
                return;
            }

            UpdateManager.Update();

            if (UpdateManager.LastUpdateError != null)
            {
                MessageBox.Show(catalog.GetStringFmt("The update failed due to an error:\n\n{0}", UpdateManager.LastUpdateError), Application.ProductName);
                return;
            }
        }

        void linkLabelChangeLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(UpdateManager.ChangeLogLink);
        }

        void buttonTools_Click(object sender, EventArgs e)
        {
            contextMenuStripTools.Show(buttonTools, new Point(0, buttonTools.ClientSize.Height), ToolStripDropDownDirection.Default);
        }

        void testingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new TestingForm(this, Settings))
            {
                form.ShowDialog(this);
            }
        }

        void buttonOptions_Click(object sender, EventArgs e)
        {
            using (var form = new OptionsForm(Settings, UpdateManager))
            {
                if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    CheckForUpdate();
            }
        }

        void buttonStart_Click(object sender, EventArgs e)
        {
            SaveOptions();

            if (SelectedAction == UserAction.SinglePlayerTimetableGame)
            {
                if (SelectedTimetable != null)
                {
                    DialogResult = CheckAndBuildTimetableInfo();
                }
            }
            else
            {
                SelectedAction = UserAction.SingleplayerNewGame;
                if (SelectedActivity != null)
                {
                    DialogResult = DialogResult.OK;
                }
            }
        }

        void buttonResume_Click(object sender, EventArgs e)
        {
            // if timetable mode but no timetable selected - no action
            if (SelectedAction == UserAction.SinglePlayerTimetableGame && SelectedTimetable == null)
            {
                return;
            }

            using (var form = new ResumeForm(Settings, SelectedRoute, SelectedAction, SelectedActivity, SelectedTimetable, this))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SaveOptions();
                    SelectedSaveFile = form.SelectedSaveFile;
                    SelectedAction = form.SelectedAction;
                    DialogResult = DialogResult.OK;
                }
            }
        }

        void buttonMPClient_Click(object sender, EventArgs e)
        {
            if (CheckUserName(textBoxMPUser.Text) == false) return;
            SaveOptions();
            SelectedAction = UserAction.MultiplayerClient;
            DialogResult = DialogResult.OK;
        }

        void buttonMPServer_Click(object sender, EventArgs e)
        {
            if (CheckUserName(textBoxMPUser.Text) == false) return;
            SaveOptions();
            SelectedAction = UserAction.MultiplayerServer;
            DialogResult = DialogResult.OK;
        }
        #endregion

        #region Options
        void LoadOptions()
        {
            checkBoxWarnings.Checked = Settings.Logging;
            checkBoxWindowed.Checked = !Settings.FullScreen;
            textBoxMPUser.Text = Settings.Multiplayer_User;
            textBoxMPHost.Text = Settings.Multiplayer_Host + ":" + Settings.Multiplayer_Port;
        }

        void SaveOptions()
        {
            Settings.Logging = checkBoxWarnings.Checked;
            Settings.FullScreen = !checkBoxWindowed.Checked;
            Settings.Multiplayer_User = textBoxMPUser.Text;
            var mpHost = textBoxMPHost.Text.Split(':');
            Settings.Multiplayer_Host = mpHost[0];
            if (mpHost.Length > 1)
            {
                var port = Settings.Multiplayer_Port;
                if (int.TryParse(mpHost[1], out port))
                    Settings.Multiplayer_Port = port;
            }
            else
            {
                Settings.Multiplayer_Port = (int)Settings.GetDefaultValue("Multiplayer_Port");
            }
            Settings.Menu_Selection = new[] {
                comboBoxFolder.SelectedItem != null ? (comboBoxFolder.SelectedItem as Folder).Path : "",
                comboBoxRoute.SelectedItem != null ? (comboBoxRoute.SelectedItem as Route).Path : "",
                comboBoxActivity.SelectedItem != null && (comboBoxActivity.SelectedItem as Activity).FilePath != null ? (comboBoxActivity.SelectedItem as Activity).FilePath : "",
            };
            Settings.Save();
        }
        #endregion

        #region Enabled state
        void UpdateEnabled()
        {
            comboBoxFolder.Enabled = buttonFolderRemove.Enabled = comboBoxFolder.Items.Count > 0;
            comboBoxRoute.Enabled = comboBoxRoute.Items.Count > 0;
            comboBoxActivity.Enabled = comboBoxActivity.Items.Count > 0;
            comboBoxLocomotive.Enabled = comboBoxLocomotive.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxConsist.Enabled = comboBoxConsist.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxStartAt.Enabled = comboBoxStartAt.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxHeadTo.Enabled = comboBoxHeadTo.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxStartTime.Enabled = comboBoxStartSeason.Enabled = comboBoxStartWeather.Enabled = SelectedActivity is ExploreActivity;
            comboBoxStartTime.DropDownStyle = SelectedActivity is ExploreActivity ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            buttonResume.Enabled = buttonStart.Enabled = SelectedActivity != null && (!(SelectedActivity is ExploreActivity) || (comboBoxConsist.Items.Count > 0 && comboBoxHeadTo.Items.Count > 0));
            buttonMPClient.Enabled = buttonStart.Enabled && !String.IsNullOrEmpty(textBoxMPUser.Text) && !String.IsNullOrEmpty(textBoxMPHost.Text);
            buttonMPServer.Enabled = buttonStart.Enabled && !String.IsNullOrEmpty(textBoxMPUser.Text);
        }
        #endregion

        #region Folder list
        void LoadFolderList()
        {
            Folders.Clear();
            ShowFolderList();

            FolderLoader = new Task<List<Folder>>(this, () => Folder.GetFolders(Settings).OrderBy(f => f.Name).ToList(), (folders) =>
            {
                Folders = folders;
                if (Folders.Count == 0)
                    MessageBox.Show(catalog.GetString("Microsoft Train Simulator doesn't appear to be installed but is optional.\n")
                        + catalog.GetString("Click on 'Add...' to point Open Rails at a folder containing folders ROUTES, TRAINS etc.."), Application.ProductName);
                ShowFolderList();
                if (Folders.Count > 0)
                    comboBoxFolder.Focus();
            });
        }

        void ShowFolderList()
        {
            comboBoxFolder.Items.Clear();
            foreach (var folder in Folders)
                comboBoxFolder.Items.Add(folder);
            if (comboBoxFolder.Items.Count > 0)
            {
                var selectionIndex = Settings.Menu_Selection.Length > 0 ? Folders.FindIndex(f => f.Path == Settings.Menu_Selection[0]) : -1;
                comboBoxFolder.SelectedIndex = Math.Max(0, selectionIndex);
            }
            UpdateEnabled();
        }

        void SaveFolderList()
        {
            Folder.SetFolders(Settings, Folders);
        }
        #endregion

        #region Route list
        void LoadRouteList()
        {
            if (RouteLoader != null)
                RouteLoader.Cancel();

            Routes.Clear();
            Activities.Clear();
            Paths.Clear();
            ShowRouteList();
            ShowActivityList();
            ShowStartAtList();
            ShowHeadToList();

            var selectedFolder = SelectedFolder;
            RouteLoader = new Task<List<Route>>(this, () => Route.GetRoutes(selectedFolder).OrderBy(r => r.ToString()).ToList(), (routes) =>
            {
                Routes = routes;
                ShowRouteList();
            });
        }

        void ShowRouteList()
        {
            comboBoxRoute.Items.Clear();
            foreach (var route in Routes)
                comboBoxRoute.Items.Add(route);
            if (comboBoxRoute.Items.Count > 0)
            {
                var selectionIndex = Settings.Menu_Selection.Length > 1 ? Routes.FindIndex(f => f.Path == Settings.Menu_Selection[1]) : -1;
                comboBoxRoute.SelectedIndex = Math.Max(0, selectionIndex);
            }
            UpdateEnabled();
        }
        #endregion

        #region Activity list
        void LoadActivityList()
        {
            if (ActivityLoader != null)
                ActivityLoader.Cancel();

            Activities.Clear();
            ShowActivityList();

            var selectedFolder = SelectedFolder;
            var selectedRoute = SelectedRoute;
            ActivityLoader = new Task<List<Activity>>(this, () => Activity.GetActivities(selectedFolder, selectedRoute).OrderBy(a => a.ToString()).ToList(), (activities) =>
            {
                Activities = activities;
                ShowActivityList();
            });
        }

        void ShowActivityList()
        {
            comboBoxActivity.Items.Clear();
            foreach (var activity in Activities)
                comboBoxActivity.Items.Add(activity);
            if (comboBoxActivity.Items.Count > 0)
            {
                var selectionIndex = Settings.Menu_Selection.Length > 2 ? Activities.FindIndex(f => f.FilePath == Settings.Menu_Selection[2]) : -1;
                comboBoxActivity.SelectedIndex = Math.Max(0, selectionIndex);
            }
            UpdateEnabled();
        }

        void UpdateExploreActivity()
        {
            if (SelectedActivity == null || !(SelectedActivity is ExploreActivity))
                return;

            var exploreActivity = SelectedActivity as ExploreActivity;
            exploreActivity.Consist = SelectedConsist;
            exploreActivity.Path = SelectedPath;
            exploreActivity.StartTime = SelectedStartTime;
            exploreActivity.Season = (MSTS.Formats.SeasonType)SelectedStartSeason;
            exploreActivity.Weather = (MSTS.Formats.WeatherType)SelectedStartWeather;
        }
        #endregion

        #region Consist lists
        void LoadLocomotiveList()
        {
            if (ConsistLoader != null)
                ConsistLoader.Cancel();

            Consists.Clear();
            ShowLocomotiveList();
            ShowConsistList();

            var selectedFolder = SelectedFolder;
            ConsistLoader = new Task<List<Consist>>(this, () => Consist.GetConsists(selectedFolder).OrderBy(a => a.ToString()).ToList(), (consists) =>
            {
                Consists = consists;
                if (SelectedActivity == null || SelectedActivity is ExploreActivity)
                    ShowLocomotiveList();
            });
        }

        void ShowLocomotiveList()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxLocomotive.Items.Clear();
                comboBoxLocomotive.Items.Add(new Locomotive());
                foreach (var loco in Consists.Where(c => c.Locomotive != null).Select(c => c.Locomotive).Distinct().OrderBy(l => l.ToString()))
                    comboBoxLocomotive.Items.Add(loco);
                if (comboBoxLocomotive.Items.Count == 1)
                    comboBoxLocomotive.Items.Clear();
                if (comboBoxLocomotive.Items.Count > 0)
                    comboBoxLocomotive.SelectedIndex = 0;
            }
            else
            {
                var consist = SelectedActivity.Consist;
                comboBoxLocomotive.Items.Clear();
                comboBoxLocomotive.Items.Add(consist.Locomotive);
                comboBoxLocomotive.SelectedIndex = 0;
                comboBoxConsist.Items.Clear();
                comboBoxConsist.Items.Add(consist);
                comboBoxConsist.SelectedIndex = 0;
            }
            UpdateEnabled();
        }

        void ShowConsistList()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxConsist.Items.Clear();
                foreach (var consist in Consists.Where(c => comboBoxLocomotive.SelectedItem.Equals(c.Locomotive)).OrderBy(c => c.Name))
                    comboBoxConsist.Items.Add(consist);
                if (comboBoxConsist.Items.Count > 0)
                    comboBoxConsist.SelectedIndex = 0;
            }
            UpdateEnabled();
        }
        #endregion

        #region Path lists
        void LoadStartAtList()
        {
            if (PathLoader != null)
                PathLoader.Cancel();

            Paths.Clear();
            ShowStartAtList();
            ShowHeadToList();

            var selectedRoute = SelectedRoute;
            PathLoader = new Task<List<Path>>(this, () => Path.GetPaths(selectedRoute, false).OrderBy(a => a.ToString()).ToList(), (paths) =>
            {
                Paths = paths;
                if (SelectedActivity == null || SelectedActivity is ExploreActivity)
                    ShowStartAtList();
            });
        }

        void ShowStartAtList()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxStartAt.Items.Clear();
                foreach (var place in Paths.Select(p => p.Start).Distinct().OrderBy(s => s.ToString()))
                    comboBoxStartAt.Items.Add(place);
                if (comboBoxStartAt.Items.Count > 0)
                    comboBoxStartAt.SelectedIndex = 0;
            }
            else
            {
                var path = SelectedActivity.Path;
                comboBoxStartAt.Items.Clear();
                comboBoxStartAt.Items.Add(path.Start);
                comboBoxStartAt.SelectedIndex = 0;
                comboBoxHeadTo.Items.Clear();
                comboBoxHeadTo.Items.Add(path);
                comboBoxHeadTo.SelectedIndex = 0;
            }
            UpdateEnabled();
        }

        void ShowHeadToList()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxHeadTo.Items.Clear();
                foreach (var path in Paths.Where(p => p.Start == (string)comboBoxStartAt.SelectedItem))
                    comboBoxHeadTo.Items.Add(path);
                if (comboBoxHeadTo.Items.Count > 0)
                    comboBoxHeadTo.SelectedIndex = 0;
            }
            UpdateEnabled();
        }
        #endregion

        #region Environment and details
        void ShowEnvironment()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxStartTime.Items.Clear();
                foreach (var hour in Enumerable.Range(0, 24))
                    comboBoxStartTime.Items.Add(String.Format("{0}:00", hour));
                comboBoxStartTime.SelectedIndex = 12;
                comboBoxDifficulty.SelectedIndex = 3;
                comboBoxDuration.Items.Clear();
                comboBoxDuration.Items.Add("");
                comboBoxDuration.SelectedIndex = 0;
                comboBoxStartSeason.SelectedIndex = 1;
                comboBoxStartWeather.SelectedIndex = 0;
            }
            else
            {
                comboBoxStartTime.Items.Clear();
                comboBoxStartTime.Items.Add(SelectedActivity.StartTime.FormattedStartTime());
                comboBoxStartTime.SelectedIndex = 0;
                comboBoxStartSeason.SelectedIndex = (int)SelectedActivity.Season;
                comboBoxStartWeather.SelectedIndex = (int)SelectedActivity.Weather;
                comboBoxDifficulty.SelectedIndex = (int)SelectedActivity.Difficulty;
                comboBoxDuration.Items.Clear();
                comboBoxDuration.Items.Add(SelectedActivity.Duration.FormattedDurationTime());
                comboBoxDuration.SelectedIndex = 0;
            }
        }

        void ShowDetails()
        {
            Win32.LockWindowUpdate(Handle);
            ClearDetails();
            if (SelectedRoute != null && SelectedRoute.Description != null)
                ShowDetail(catalog.GetStringFmt("Route: {0}", SelectedRoute.Name), SelectedRoute.Description.Split('\n'));

            if (radioButtonModeActivity.Checked)
            {
                if (SelectedConsist != null && SelectedConsist.Locomotive != null && SelectedConsist.Locomotive.Description != null)
                    ShowDetail(catalog.GetStringFmt("Locomotive: {0}", SelectedConsist.Locomotive.Name), SelectedConsist.Locomotive.Description.Split('\n'));
                if (SelectedActivity != null && SelectedActivity.Description != null)
                {
                    ShowDetail(catalog.GetStringFmt("Activity: {0}", SelectedActivity.Name), SelectedActivity.Description.Split('\n'));
                    ShowDetail(catalog.GetString("Activity Briefing"), SelectedActivity.Briefing.Split('\n'));
                }
            }
            if (radioButtonModeTimetable.Checked)
            {
                if (SelectedTimetable != null)
                {
                    ShowDetail(catalog.GetString("Timetable"), new string[1] { SelectedTimetable.ToString() });
                }
                if (!String.IsNullOrEmpty(SelectedPlayerTimetable))
                {
                    ShowDetail(catalog.GetString("Player Timetable"), new string[1] { SelectedPlayerTimetable });
                }
                if (SelectedTimetableTrain != null)
                {
                    ShowDetail(catalog.GetString("Player Train"), SelectedTimetableTrain.ToInfo());
                    if (SelectedTimetableConsist != null)
                    {
                        ShowDetail(catalog.GetString("Consist : "), new string[1] { SelectedTimetableTrain.Consist.ToString() });
                        if (SelectedTimetableConsist.Locomotive != null && SelectedTimetableConsist.Locomotive.Description != null)
                        {
                            ShowDetail(catalog.GetStringFmt("Locomotive: {0}", SelectedTimetableConsist.Locomotive.Name), SelectedTimetableConsist.Locomotive.Description.Split('\n'));
                        }
                    }
                    if (SelectedTimetablePath != null)
                    {
                        ShowDetail(catalog.GetString("Path : "), SelectedTimetablePath.ToInfo());
                    }
                }
            }

            FlowDetails();
            Win32.LockWindowUpdate(IntPtr.Zero);
        }

        List<Detail> Details = new List<Detail>();
        class Detail
        {
            public readonly Control Title;
            public readonly Control Expander;
            public readonly Control Summary;
            public readonly Control Description;
            public bool Expanded;
            public Detail(Control title, Control expander, Control summary, Control lines)
            {
                Title = title;
                Expander = expander;
                Summary = summary;
                Description = lines;
                Expanded = false;
            }
        }

        void ClearDetails()
        {
            Details.Clear();
            while (panelDetails.Controls.Count > 0)
                panelDetails.Controls.RemoveAt(0);
        }

        void ShowDetail(string title, string[] lines)
        {
            var titleControl = new Label { Margin = new Padding(2), Text = title, UseMnemonic = false, Font = new Font(panelDetails.Font, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
            panelDetails.Controls.Add(titleControl);
            titleControl.Left = titleControl.Margin.Left;
            titleControl.Width = panelDetails.ClientSize.Width - titleControl.Margin.Horizontal - titleControl.PreferredHeight;
            titleControl.Height = titleControl.PreferredHeight;
            titleControl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            var expanderControl = new Button { Margin = new Padding(0), Text = "", FlatStyle = FlatStyle.Flat };
            panelDetails.Controls.Add(expanderControl);
            expanderControl.Left = panelDetails.ClientSize.Width - titleControl.Height - titleControl.Margin.Right;
            expanderControl.Width = expanderControl.Height = titleControl.Height;
            expanderControl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            expanderControl.FlatAppearance.BorderSize = 0;
            expanderControl.BackgroundImageLayout = ImageLayout.Center;

            var summaryControl = new Label { Margin = new Padding(2), Text = String.Join("\n", lines), AutoSize = false, UseMnemonic = false, UseCompatibleTextRendering = false };
            panelDetails.Controls.Add(summaryControl);
            summaryControl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            summaryControl.Left = summaryControl.Margin.Left;
            summaryControl.Width = panelDetails.ClientSize.Width - summaryControl.Margin.Horizontal;
            summaryControl.Height = TextRenderer.MeasureText("1\n2\n3\n4\n5", summaryControl.Font).Height;

            // Find out where we need to cut the text to make the summary 5 lines long. Uses a binaty search to find the cut point.
            var size = MeasureText(summaryControl.Text, summaryControl);
            if (size > summaryControl.Height)
            {
                var index = (float)summaryControl.Text.Length;
                var indexChunk = (float)summaryControl.Text.Length / 2;
                while (indexChunk > 0.5f || size > summaryControl.Height)
                {
                    if (size > summaryControl.Height)
                        index -= indexChunk;
                    else
                        index += indexChunk;
                    if (indexChunk > 0.5f)
                        indexChunk /= 2;
                    size = MeasureText(summaryControl.Text.Substring(0, (int)index) + "...", summaryControl);
                }
                summaryControl.Text = summaryControl.Text.Substring(0, (int)index) + "...";
            }

            var descriptionControl = new Label { Margin = new Padding(2), Text = String.Join("\n", lines), AutoSize = false, UseMnemonic = false, UseCompatibleTextRendering = false };
            panelDetails.Controls.Add(descriptionControl);
            descriptionControl.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            descriptionControl.Left = descriptionControl.Margin.Left;
            descriptionControl.Width = panelDetails.ClientSize.Width - descriptionControl.Margin.Horizontal;
            descriptionControl.Height = MeasureText(descriptionControl.Text, descriptionControl);

            // Enable the expander only if the full description is longer than the summary. Otherwise, disable the expander.
            expanderControl.Enabled = descriptionControl.Height > summaryControl.Height;
            if (expanderControl.Enabled)
            {
                expanderControl.BackgroundImage = (Image)Resources.GetObject("ExpanderClosed");
                expanderControl.Tag = Details.Count;
                expanderControl.Click += new EventHandler(expanderControl_Click);
            }
            else
            {
                expanderControl.BackgroundImage = (Image)Resources.GetObject("ExpanderClosedDisabled");
            }

            Details.Add(new Detail(titleControl, expanderControl, summaryControl, descriptionControl));
        }

        static int MeasureText(string text, Label summaryControl)
        {
            return TextRenderer.MeasureText(text, summaryControl.Font, summaryControl.ClientSize, TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
        }

        void expanderControl_Click(object sender, EventArgs e)
        {
            Win32.LockWindowUpdate(Handle);
            var index = (int)(sender as Control).Tag;
            Details[index].Expanded = !Details[index].Expanded;
            Details[index].Expander.BackgroundImage = (Image)Resources.GetObject(Details[index].Expanded ? "ExpanderOpen" : "ExpanderClosed");
            FlowDetails();
            Win32.LockWindowUpdate(IntPtr.Zero);
        }

        void FlowDetails()
        {
            var scrollPosition = panelDetails.AutoScrollPosition.Y;
            panelDetails.AutoScrollPosition = Point.Empty;
            panelDetails.AutoScrollMinSize = new Size(0, panelDetails.ClientSize.Height + 1);

            var top = 0;
            foreach (var detail in Details)
            {
                top += detail.Title.Margin.Top;
                detail.Title.Top = detail.Expander.Top = top;
                top += detail.Title.Height + detail.Title.Margin.Bottom + detail.Description.Margin.Top;
                detail.Summary.Top = detail.Description.Top = top;
                detail.Summary.Visible = !detail.Expanded && detail.Expander.Enabled;
                detail.Description.Visible = !detail.Summary.Visible;
                if (detail.Description.Visible)
                    top += detail.Description.Height + detail.Description.Margin.Bottom;
                else
                    top += detail.Summary.Height + detail.Summary.Margin.Bottom;
            }

            if (panelDetails.AutoScrollMinSize.Height < top)
                panelDetails.AutoScrollMinSize = new Size(0, top);
            panelDetails.AutoScrollPosition = new Point(0, -scrollPosition);
        }
        #endregion

        #region Utility functions
        private sealed class Win32
        {
            Win32() { }

            /// <summary>
            /// Lock ore relase the wndow for updating.
            /// </summary>
            [DllImport("user32")]
            public static extern int LockWindowUpdate(IntPtr hwnd);
        }
        #endregion

        #region ORTimeTable
        void LoadORTimeTableList()
        {
            if (ORTimeTableLoader != null)
                ORTimeTableLoader.Cancel();

            ORTimeTables.Clear();
            ShowORTimetableList();

            var selectedFolder = SelectedFolder;
            var selectedRoute = SelectedRoute;
            ORTimeTableLoader = new Task<List<TimetableInfo>>(this, () => TimetableInfo.GetTimetableInfo(selectedFolder, selectedRoute).OrderBy(a => a.ToString()).ToList(), (ortimetables) =>
            {
                ORTimeTables = ortimetables;
                ShowORTimetableList();
            });
        }
        #endregion

        #region ORTimetableList
        void ShowORTimetableList()
        {
            comboBoxTimetable.Items.Clear();
            foreach (var timetable in ORTimeTables)
                comboBoxTimetable.Items.Add(timetable);
            UpdateEnabled();
        }

        private void ComboBoxTimetable_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedAction = UserAction.SinglePlayerTimetableGame;
            ClearTrainList();
            ShowORSubTimetableList();
            PresetTimetableAdditionalInfo();
            ShowDetails();
        }

        private DialogResult CheckAndBuildTimetableInfo()
        {
            if (SelectedTimetableTrain == null)
            {
                return DialogResult.None;
            }
            SelectedTimetable.Day = comboBoxTimetableDay.SelectedIndex;
            SelectedTimetable.Season = comboBoxTimetableSeason.SelectedIndex;
            SelectedTimetable.Weather = comboBoxTimetableWeather.SelectedIndex;

            return DialogResult.OK;
        }
        #endregion

        #region ORSubTimetable
        void ShowORSubTimetableList()
        {
            comboBoxPlayerTimetable.Items.Clear();
            if (SelectedTimetable != null)
            {
                foreach (ORTS.Formats.TTPreInfo ttInfo in SelectedTimetable.ORTTList)
                {
                    comboBoxPlayerTimetable.Items.Add(ttInfo.Description);
                }
                if (comboBoxPlayerTimetable.Items.Count == 1)
                {
                    comboBoxPlayerTimetable.SelectedIndex = 0;
                }
            }
            else
            {
                comboBoxPlayerTimetable.Items.Clear();
                comboBoxPlayerTimetable.SelectedItem = null;
            }
        }
        #endregion

        #region ORSubTimetableList
        private void comboboxPlayerTimetable_selectedIndexChanged(object sender, EventArgs e)
        {
            ShowORTimetableTrainList();
            ShowDetails();
        }
        #endregion

        #region ORTimetableTrain
        void ShowORTimetableTrainList()
        {
            comboBoxPlayerTrain.Items.Clear();
            if (comboBoxTimetable.SelectedIndex >= 0)
            {
                List<TTPreInfo.TTTrainPreInfo> usedTrains = SelectedTimetable.ORTTList[comboBoxPlayerTimetable.SelectedIndex].Trains;
                usedTrains.Sort();

                foreach (TTPreInfo.TTTrainPreInfo train in usedTrains)
                {
                    comboBoxPlayerTrain.Items.Add(train);
                }
            }
        }

        void ClearTrainList()
        {
            comboBoxPlayerTrain.Items.Clear();
        }

        private void comboBoxPlayerTrain_SelectedIndexChanged(object sender, EventArgs e)
        {
            TTPreInfo.TTTrainPreInfo selectedTrain = comboBoxPlayerTrain.SelectedItem as TTPreInfo.TTTrainPreInfo;
            SelectedTimetableConsist = Consist.GetConsist(SelectedFolder, selectedTrain.LeadingConsist, selectedTrain.ReverseConsist);
            SelectedTimetablePath = Path.GetPath(SelectedRoute, selectedTrain.Path, false);
            ShowDetails();
        }
        #endregion

        #region TimetableAdditionInfo
        void PresetTimetableAdditionalInfo()
        {
            comboBoxTimetableDay.SelectedIndex = 0;
            comboBoxTimetableSeason.SelectedIndex = 1;
            comboBoxTimetableWeather.SelectedIndex = 0;
        }
        #endregion

        #region Executable utils
        enum ImageSubsystem
        {
            Unknown = 0,
            Native = 1,
            WindowsGui = 2,
            WindowsConsole = 3,
        }

        ImageSubsystem GetImageSubsystem(BinaryReader stream)
        {
            try
            {
                var baseOffset = stream.BaseStream.Position;

                // WORD IMAGE_DOS_HEADER.e_magic = 0x4D5A (MZ)
                stream.BaseStream.Seek(baseOffset + 0, SeekOrigin.Begin);
                var dosMagic = stream.ReadUInt16();
                if (dosMagic != 0x5A4D)
                    return ImageSubsystem.Unknown;

                // LONG IMAGE_DOS_HEADER.e_lfanew
                stream.BaseStream.Seek(baseOffset + 60, SeekOrigin.Begin);
                var ntHeaderOffset = stream.ReadUInt32();
                if (ntHeaderOffset == 0)
                    return ImageSubsystem.Unknown;

                // DWORD IMAGE_NT_HEADERS.Signature = 0x00004550 (PE..)
                stream.BaseStream.Seek(baseOffset + ntHeaderOffset, SeekOrigin.Begin);
                var ntMagic = stream.ReadUInt32();
                if (ntMagic != 0x00004550)
                    return ImageSubsystem.Unknown;

                // WORD IMAGE_OPTIONAL_HEADER.Magic = 0x010A (32bit header) or 0x020B (64bit header)
                stream.BaseStream.Seek(baseOffset + ntHeaderOffset + 24, SeekOrigin.Begin);
                var optionalMagic = stream.ReadUInt16();
                if (optionalMagic != 0x010B && optionalMagic != 0x020B)
                    return ImageSubsystem.Unknown;

                // WORD IMAGE_OPTIONAL_HEADER.Subsystem
                // Note: There might need to be an adjustment for ImageBase being ULONGLONG in the 64bit header though this doesn't actually seem to be true.
                stream.BaseStream.Seek(baseOffset + ntHeaderOffset + 92, SeekOrigin.Begin);
                var peSubsystem = stream.ReadUInt16();

                return (ImageSubsystem)peSubsystem;
            }
            catch (EndOfStreamException)
            {
                return ImageSubsystem.Unknown;
            }
        }
        #endregion
    }
}
