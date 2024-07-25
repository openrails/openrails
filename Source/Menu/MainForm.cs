// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using Orts.Formats.OR;
using ORTS.Common;
using ORTS.Menu;
using ORTS.Settings;
using ORTS.Updater;
using Activity = ORTS.Menu.Activity;
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
            MultiplayerServerResumeSave,
            MultiplayerClientResumeSave
        }

        bool Initialized;
        UserSettings Settings;
        List<Folder> Folders = new List<Folder>();
        public List<Route> Routes = new List<Route>();
        List<Activity> Activities = new List<Activity>();
        public List<Consist> Consists = new List<Consist>();
        List<Path> Paths = new List<Path>();
        List<TimetableInfo> TimetableSets = new List<TimetableInfo>();
        List<WeatherFileInfo> TimetableWeatherFileSet = new List<WeatherFileInfo>();
        Task<List<Folder>> FolderLoader;
        Task<List<Route>> RouteLoader;
        Task<List<Activity>> ActivityLoader;
        Task<List<Consist>> ConsistLoader;
        Task<List<Path>> PathLoader;
        Task<List<TimetableInfo>> TimetableSetLoader;
        Task<List<WeatherFileInfo>> TimetableWeatherFileLoader;
        readonly ResourceManager Resources = new ResourceManager("ORTS.Properties.Resources", typeof(MainForm).Assembly);
        readonly UpdateManager UpdateManager;
        readonly Image ElevationIcon;
        NotificationManager NotificationManager;

        internal string RunActivityProgram
        {
            get
            {
                return System.IO.Path.Combine(Application.StartupPath, "RunActivity.exe");
            }
        }

        // Base items
        public Folder SelectedFolder { get { return (Folder)comboBoxFolder.SelectedItem; } }
        public Route SelectedRoute { get { return (Route)comboBoxRoute.SelectedItem; } }

        // Activity mode items
        public Activity SelectedActivity { get { return (Activity)comboBoxActivity.SelectedItem; } }
        public Consist SelectedConsist { get { return (Consist)comboBoxConsist.SelectedItem; } }
        public Path SelectedPath { get { return (Path)comboBoxHeadTo.SelectedItem; } }
        public string SelectedStartTime { get { return comboBoxStartTime.Text; } }

        // Timetable mode items
        public TimetableInfo SelectedTimetableSet { get { return (TimetableInfo)comboBoxTimetableSet.SelectedItem; } }
        public TimetableFileLite SelectedTimetable { get { return (TimetableFileLite)comboBoxTimetable.SelectedItem; } }
        public TimetableFileLite.TrainInformation SelectedTimetableTrain { get { return (TimetableFileLite.TrainInformation)comboBoxTimetableTrain.SelectedItem; } }
        public int SelectedTimetableDay { get { return (comboBoxTimetableDay.SelectedItem as KeyedComboBoxItem).Key; } }
        public WeatherFileInfo SelectedWeatherFile { get { return (WeatherFileInfo)comboBoxTimetableWeatherFile.SelectedItem; } }
        public Consist SelectedTimetableConsist;
        public Path SelectedTimetablePath;

        // Shared items
        public int SelectedStartSeason { get { return radioButtonModeActivity.Checked ? (comboBoxStartSeason.SelectedItem as KeyedComboBoxItem).Key : (comboBoxTimetableSeason.SelectedItem as KeyedComboBoxItem).Key; } }
        public int SelectedStartWeather { get { return radioButtonModeActivity.Checked ? (comboBoxStartWeather.SelectedItem as KeyedComboBoxItem).Key : (comboBoxTimetableWeather.SelectedItem as KeyedComboBoxItem).Key; } }

        public string SelectedSaveFile { get; set; }
        public UserAction SelectedAction { get; set; }

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
            NotificationManager = new NotificationManager(this, UpdateManager, Settings);

            LoadOptions();
            LoadLanguage();

            if (!Initialized)
            {
                var Seasons = new[] {
                    new KeyedComboBoxItem(0, catalog.GetString("Spring")),
                    new KeyedComboBoxItem(1, catalog.GetString("Summer")),
                    new KeyedComboBoxItem(2, catalog.GetString("Autumn")),
                    new KeyedComboBoxItem(3, catalog.GetString("Winter")),
                };
                var Weathers = new[] {
                    new KeyedComboBoxItem(0, catalog.GetString("Clear")),
                    new KeyedComboBoxItem(1, catalog.GetString("Snow")),
                    new KeyedComboBoxItem(2, catalog.GetString("Rain")),
                };
                var Difficulties = new[] {
                    catalog.GetString("Easy"),
                    catalog.GetString("Medium"),
                    catalog.GetString("Hard"),
                    "",
                };
                var Days = new[] {
                    new KeyedComboBoxItem(0, catalog.GetString("Monday")),
                    new KeyedComboBoxItem(1, catalog.GetString("Tuesday")),
                    new KeyedComboBoxItem(2, catalog.GetString("Wednesday")),
                    new KeyedComboBoxItem(3, catalog.GetString("Thursday")),
                    new KeyedComboBoxItem(4, catalog.GetString("Friday")),
                    new KeyedComboBoxItem(5, catalog.GetString("Saturday")),
                    new KeyedComboBoxItem(6, catalog.GetString("Sunday")),
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
                    })
                    { Tag = executable });
                }
                // Add all the tools in alphabetical order.
                contextMenuStripTools.Items.AddRange((from tool in tools
                                                      orderby tool.Text
                                                      select tool).ToArray());

                // Just like above, buttonDocuments is a button that is treated like a menu.  The result is a button that acts like a combobox.
                // Populate buttonDocuments.
                // Documents button will be disabled if Documentation folder is not present.
                var docs = new List<ToolStripItem>();
                var dir = Directory.GetCurrentDirectory();
                var path = dir + @"\Documentation\";
                if (Directory.Exists(path))
                {
                    // Load English documents
                    LoadDocuments(docs, path);

                    // Find any non-English documents by looking in \Documentation\<language code>\, e.g. \Documentation\es\
                    foreach (var codePath in Directory.GetDirectories(path))
                    {
                        // Extract the last folder in the path - the language code, e.g. "es"
                        var code = System.IO.Path.GetFileName(codePath);

                        // include any non-English documents that match the chosen language
                        if (code == Settings.Language)
                            LoadDocuments(docs, codePath, code);
                    }
                }
                else
                    buttonDocuments.Enabled = false;
            }

            ShowEnvironment();
            ShowTimetableEnvironment();

            CheckForUpdate();

            if (!Initialized)
            {
                LoadFolderList();
                Initialized = true;
            }
        }

        private void LoadDocuments(List<ToolStripItem> docs, string folderPath, string code = null)
        {
            foreach (var filePath in Directory.GetFiles(folderPath))
            {
                var ext = System.IO.Path.GetExtension(filePath);
                var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                // These are the formats that can be selected.
                if (new[] { ".pdf", ".doc", ".docx", ".pptx", ".txt" }.Contains(ext.ToLowerInvariant()))
                {
                    var codeLabel = string.IsNullOrEmpty(code) ? "" : $" [{code}]";
                    docs.Add(new ToolStripMenuItem($"{name}{ext}{codeLabel}", null, (object sender2, EventArgs e2) =>
                    {
                        var docPath = (sender2 as ToolStripItem).Tag as string;
                        Process.Start(docPath);
                    })
                    { Tag = filePath });
                }
                contextMenuStripDocuments.Items.AddRange((from tool in docs
                                                          orderby tool.Text
                                                          select tool).ToArray());
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
            if (TimetableSetLoader != null)
                TimetableSetLoader.Cancel();
            if (TimetableWeatherFileLoader != null)
                TimetableWeatherFileLoader.Cancel();

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
            // Uses a custom Task class which pre-dates the System.Threading.Task but provides much same features.
            new Task<UpdateManager>(this, () =>
            {
                UpdateManager.Check();
                return null;
            }, _ =>
            {
                NotificationManager.CheckNotifications();
            });
        }

        // Event raised by Retry button in NotificationPages so user can retry updates following an error notification.
        public event EventHandler CheckUpdatesAgain;

        public virtual void OnCheckUpdatesAgain(EventArgs e)
        {
            CheckForUpdate();
        }

        void LoadLanguage()
        {
            if (Settings.Language.Length > 0)
            {
                try
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Language);
                }
                catch { }
            }

            Localizer.Localize(this, catalog);
        }

        void RestartMenu()
        {
            Process.Start(Application.ExecutablePath);
            Close();
        }
        #endregion

        #region Folders
        void comboBoxFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadRouteList();
            LoadLocomotiveList();
            ShowDetails();
        }
        #endregion

        #region Routes
        void comboBoxRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadActivityList();
            LoadStartAtList();
            LoadTimetableSetList();
            ShowDetails();
        }
        #endregion

        #region Mode
        void radioButtonMode_CheckedChanged(object sender, EventArgs e)
        {
            panelModeActivity.Visible = radioButtonModeActivity.Checked;
            panelModeTimetable.Visible = radioButtonModeTimetable.Checked;
            UpdateEnabled();
            ShowDetails();
        }
        #endregion

        #region Activities
        void comboBoxActivity_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            ShowDetails();
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

        #region Timetable Sets
        void comboBoxTimetableSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimetableSet();
            ShowTimetableList();
            ShowDetails();
        }
        #endregion

        #region Timetables
        void comboBoxTimetable_selectedIndexChanged(object sender, EventArgs e)
        {
            ShowTimetableTrainList();
            ShowDetails();
        }
        #endregion

        #region Timetable Trains
        void comboBoxTimetableTrain_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTrain = comboBoxTimetableTrain.SelectedItem as TimetableFileLite.TrainInformation;
            SelectedTimetableConsist = Consist.GetConsist(SelectedFolder, selectedTrain.LeadingConsist, selectedTrain.ReverseConsist);
            SelectedTimetablePath = Path.GetPath(SelectedRoute, selectedTrain.Path, false);
            ShowDetails();
        }
        #endregion

        #region Timetable environment
        void comboBoxTimetableDay_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimetableSet();
        }

        void comboBoxTimetableSeason_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimetableSet();
        }

        void comboBoxTimetableWeather_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimetableSet();
        }

        void comboBoxTimetableWeatherFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimetableWeatherSet();
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

        void buttonTools_Click(object sender, EventArgs e)
        {
            contextMenuStripTools.Show(buttonTools, new Point(0, buttonTools.ClientSize.Height), ToolStripDropDownDirection.Default);
        }

        void buttonDocuments_Click(object sender, EventArgs e)
        {
            contextMenuStripDocuments.Show(buttonDocuments, new Point(0, buttonDocuments.ClientSize.Height), ToolStripDropDownDirection.Default);
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
            SaveOptions();

            using (var form = new OptionsForm(Settings, UpdateManager, false))
            {
                switch (form.ShowDialog(this))
                {
                    case DialogResult.OK:
                        LoadFolderList();
                        CheckForUpdate();
                        //Notifications.CheckNotifications();
                        break;
                    case DialogResult.Retry:
                        RestartMenu();
                        break;
                }
            }
        }

        void buttonStart_Click(object sender, EventArgs e)
        {
            SaveOptions();

            if (radioButtonModeActivity.Checked)
            {
                SelectedAction = UserAction.SingleplayerNewGame;
                if (SelectedActivity != null)
                    DialogResult = DialogResult.OK;
            }
            else
            {
                SelectedAction = UserAction.SinglePlayerTimetableGame;
                if (SelectedTimetableTrain != null)
                    DialogResult = DialogResult.OK;
            }
        }

        void buttonResume_Click(object sender, EventArgs e)
        {
            OpenResumeForm(false);
        }

        void buttonResumeMP_Click(object sender, EventArgs e)
        {
            OpenResumeForm(true);
        }

        void OpenResumeForm(bool multiplayer)
        {
            if (radioButtonModeTimetable.Checked)
            {
                SelectedAction = UserAction.SinglePlayerTimetableGame;
            }
            else if (!multiplayer)
            {
                SelectedAction = UserAction.SingleplayerNewGame;
            }
            else if (radioButtonMPClient.Checked)
            {
                SelectedAction = UserAction.MultiplayerClient;
            }
            else
                SelectedAction = UserAction.MultiplayerServer;

            // if timetable mode but no timetable selected - no action
            if (SelectedAction == UserAction.SinglePlayerTimetableGame && (SelectedTimetableSet == null || multiplayer))
            {
                return;
            }

            using (var form = new ResumeForm(Settings, SelectedRoute, SelectedAction, SelectedActivity, SelectedTimetableSet, this))
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

        void buttonStartMP_Click(object sender, EventArgs e)
        {
            if (CheckUserName(textBoxMPUser.Text) == false) return;
            SaveOptions();
            SelectedAction = radioButtonMPClient.Checked ? UserAction.MultiplayerClient : UserAction.MultiplayerServer;
            DialogResult = DialogResult.OK;
        }

        #endregion

        #region Options
        void LoadOptions()
        {
            checkBoxWarnings.Checked = Settings.Logging;
            radioButtonModeActivity.Checked = Settings.IsModeActivity;
            radioButtonModeTimetable.Checked = !Settings.IsModeActivity;

            textBoxMPUser.Text = Settings.Multiplayer_User;
            textBoxMPHost.Text = Settings.Multiplayer_Host + ":" + Settings.Multiplayer_Port;
        }

        void SaveOptions()
        {
            Settings.Logging = checkBoxWarnings.Checked;
            Settings.Multiplayer_User = textBoxMPUser.Text;
            Settings.IsModeActivity = radioButtonModeActivity.Checked;

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
                // Base items
                SelectedFolder != null ? SelectedFolder.Path : "",
                SelectedRoute != null ? SelectedRoute.Path : "",
                // Activity mode items / Explore mode items
                radioButtonModeActivity.Checked ?
                    SelectedActivity != null && SelectedActivity.FilePath != null ? SelectedActivity.FilePath : SelectedActivity != null? SelectedActivity.Name : "" :
                    SelectedTimetableSet != null ? SelectedTimetableSet.fileName : "",
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity && comboBoxLocomotive.SelectedItem != null && (comboBoxLocomotive.SelectedItem as Locomotive).FilePath != null ? (comboBoxLocomotive.SelectedItem as Locomotive).FilePath : "" :
                    SelectedTimetable != null ? SelectedTimetable.Description : "",
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity && SelectedConsist != null ? SelectedConsist.FilePath : "" :
                    SelectedTimetableTrain != null ? SelectedTimetableTrain.Column.ToString() : "",
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity && SelectedPath != null ? SelectedPath.FilePath : "" :
                    SelectedTimetableDay.ToString(),
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity ? SelectedStartTime : "" :
                    "",
                // Shared items
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity ? SelectedStartSeason.ToString() : "" :
                    SelectedStartSeason.ToString(),
                radioButtonModeActivity.Checked ?
                    SelectedActivity is ExploreActivity ? SelectedStartWeather.ToString() : "" :
                    SelectedStartWeather.ToString(),
            };
            Settings.Save();
        }
        #endregion

        #region Enabled state
        void UpdateEnabled()
        {
            comboBoxFolder.Enabled = comboBoxFolder.Items.Count > 0;
            comboBoxRoute.Enabled = comboBoxRoute.Items.Count > 0;
            comboBoxActivity.Enabled = comboBoxActivity.Items.Count > 0;
            comboBoxLocomotive.Enabled = comboBoxLocomotive.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxConsist.Enabled = comboBoxConsist.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxStartAt.Enabled = comboBoxStartAt.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxHeadTo.Enabled = comboBoxHeadTo.Items.Count > 0 && SelectedActivity is ExploreActivity;
            comboBoxStartTime.Enabled = comboBoxStartSeason.Enabled = comboBoxStartWeather.Enabled = SelectedActivity is ExploreActivity;
            comboBoxStartTime.DropDownStyle = SelectedActivity is ExploreActivity ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            comboBoxTimetable.Enabled = comboBoxTimetableSet.Items.Count > 0;
            comboBoxTimetableTrain.Enabled = comboBoxTimetable.Items.Count > 0;
            comboBoxTimetableWeatherFile.Enabled = comboBoxTimetableWeatherFile.Items.Count > 0;
            //Avoid to Start with a non valid Activity/Locomotive/Consist.
            buttonResume.Enabled = buttonStart.Enabled = radioButtonModeActivity.Checked && !comboBoxActivity.Text.StartsWith("<") && !comboBoxLocomotive.Text.StartsWith("<") ?
                SelectedActivity != null && (!(SelectedActivity is ExploreActivity) || (comboBoxConsist.Items.Count > 0 && comboBoxHeadTo.Items.Count > 0)) :
                SelectedTimetableTrain != null;
            buttonResumeMP.Enabled = buttonStartMP.Enabled = buttonStart.Enabled && !String.IsNullOrEmpty(textBoxMPUser.Text) && !String.IsNullOrEmpty(textBoxMPHost.Text);
        }
        #endregion

        #region Folder list
        void LoadFolderList()
        {
            var initialized = Initialized;
            Folders.Clear();
            ShowFolderList();

            FolderLoader = new Task<List<Folder>>(this, () => Folder.GetFolders(Settings).OrderBy(f => f.Name).ToList(), (folders) =>
            {
                Folders = folders;
                ShowFolderList();
                if (Folders.Count > 0)
                    comboBoxFolder.Focus();

                if (!initialized && Folders.Count == 0)
                {
                    using (var form = new OptionsForm(Settings, UpdateManager, true))
                    {
                        switch (form.ShowDialog(this))
                        {
                            case DialogResult.OK:
                                LoadFolderList();
                                break;
                            case DialogResult.Retry:
                                RestartMenu();
                                break;
                        }
                    }
                }
            });
        }

        void ShowFolderList()
        {
            comboBoxFolder.Items.Clear();
            foreach (var folder in Folders)
                comboBoxFolder.Items.Add(folder);
            UpdateFromMenuSelection<Folder>(comboBoxFolder, UserSettings.Menu_SelectionIndex.Folder, f => f.Path);
            UpdateEnabled();
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
            UpdateFromMenuSelection<Route>(comboBoxRoute, UserSettings.Menu_SelectionIndex.Route, r => r.Path);
            if (Settings.Menu_Selection.Length > (int)UserSettings.Menu_SelectionIndex.Activity)
            {
                var path = Settings.Menu_Selection[(int)UserSettings.Menu_SelectionIndex.Activity]; // Activity or Timetable
                var extension = System.IO.Path.GetExtension(path).ToLower();
                if (extension == ".act")
                    radioButtonModeActivity.Checked = true;
                else if (extension == ".timetable_or" || extension == ".timetable-or")
                    radioButtonModeTimetable.Checked = true;
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
            UpdateFromMenuSelection<Activity>(comboBoxActivity, UserSettings.Menu_SelectionIndex.Activity, a => a.FilePath);
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
            exploreActivity.Season = (Orts.Formats.Msts.SeasonType)SelectedStartSeason;
            exploreActivity.Weather = (Orts.Formats.Msts.WeatherType)SelectedStartWeather;
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
                UpdateFromMenuSelection<Locomotive>(comboBoxLocomotive, UserSettings.Menu_SelectionIndex.Locomotive, l => l.FilePath);
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
                UpdateFromMenuSelection<Consist>(comboBoxConsist, UserSettings.Menu_SelectionIndex.Consist, c => c.FilePath);
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
                // Because this list is unique names, we have to do some extra work to select it.
                if (Settings.Menu_Selection.Length >= (int)UserSettings.Menu_SelectionIndex.Path)
                {
                    var pathFilePath = Settings.Menu_Selection[(int)UserSettings.Menu_SelectionIndex.Path];
                    var path = Paths.FirstOrDefault(p => p.FilePath == pathFilePath);
                    if (path != null)
                        SelectComboBoxItem<string>(comboBoxStartAt, s => s == path.Start);
                    else if (comboBoxStartAt.Items.Count > 0)
                        comboBoxStartAt.SelectedIndex = 0;
                }
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
                UpdateFromMenuSelection<Path>(comboBoxHeadTo, UserSettings.Menu_SelectionIndex.Path, c => c.FilePath);
            }
            UpdateEnabled();
        }
        #endregion

        #region Environment
        void ShowEnvironment()
        {
            if (SelectedActivity == null || SelectedActivity is ExploreActivity)
            {
                comboBoxStartTime.Items.Clear();
                foreach (var hour in Enumerable.Range(0, 24))
                    comboBoxStartTime.Items.Add(String.Format("{0}:00", hour));

                UpdateFromMenuSelection<string>(comboBoxStartTime, UserSettings.Menu_SelectionIndex.Time, "12:00");
                UpdateFromMenuSelection<KeyedComboBoxItem>(comboBoxStartSeason, UserSettings.Menu_SelectionIndex.Season, s => s.Key.ToString(), new KeyedComboBoxItem(1, ""));
                UpdateFromMenuSelection<KeyedComboBoxItem>(comboBoxStartWeather, UserSettings.Menu_SelectionIndex.Weather, w => w.Key.ToString(), new KeyedComboBoxItem(0, ""));
                comboBoxDifficulty.SelectedIndex = 3;
                comboBoxDuration.Items.Clear();
                comboBoxDuration.Items.Add("");
                comboBoxDuration.SelectedIndex = 0;
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
        #endregion

        #region Timetable Set list
        void LoadTimetableSetList()
        {
            if (TimetableSetLoader != null)
                TimetableSetLoader.Cancel();
            if (TimetableWeatherFileLoader != null)
                TimetableWeatherFileLoader.Cancel();

            TimetableSets.Clear();
            ShowTimetableSetList();
            var selectedFolder = SelectedFolder;
            var selectedRoute = SelectedRoute;
            TimetableSetLoader = new Task<List<TimetableInfo>>(this, () => TimetableInfo.GetTimetableInfo(selectedFolder, selectedRoute).OrderBy(a => a.ToString()).ToList(), (timetableSets) =>
            {
                TimetableSets = timetableSets;
                ShowTimetableSetList();
            });

            TimetableWeatherFileLoader = new Task<List<WeatherFileInfo>>(this, () => WeatherFileInfo.GetTimetableWeatherFiles(selectedFolder, selectedRoute).OrderBy(a => a.ToString()).ToList(), (timetableWeatherFileSet) =>
            {
                TimetableWeatherFileSet = timetableWeatherFileSet;
                ShowTimetableWeatherSet();
            });
        }

        void ShowTimetableSetList()
        {
            comboBoxTimetableSet.Items.Clear();
            foreach (var timetableSet in TimetableSets)
                comboBoxTimetableSet.Items.Add(timetableSet);
            UpdateFromMenuSelection<TimetableInfo>(comboBoxTimetableSet, UserSettings.Menu_SelectionIndex.TimetableSet, t => t.fileName);
            UpdateEnabled();
        }

        void UpdateTimetableSet()
        {
            if (SelectedTimetableSet != null)
            {
                SelectedTimetableSet.Day = SelectedTimetableDay;
                SelectedTimetableSet.Season = SelectedStartSeason;
                SelectedTimetableSet.Weather = SelectedStartWeather;
            }
        }

        void ShowTimetableWeatherSet()
        {
            comboBoxTimetableWeatherFile.Items.Clear();
            foreach (var weatherFile in TimetableWeatherFileSet)
            {
                comboBoxTimetableWeatherFile.Items.Add(weatherFile);
                UpdateEnabled();
            }
        }

        void UpdateTimetableWeatherSet()
        {
            SelectedTimetableSet.WeatherFile = SelectedWeatherFile.GetFullName();
        }

        #endregion

        #region Timetable list
        void ShowTimetableList()
        {
            comboBoxTimetable.Items.Clear();
            if (SelectedTimetableSet != null)
            {
                foreach (var timetable in SelectedTimetableSet.ORTTList)
                    comboBoxTimetable.Items.Add(timetable);
                UpdateFromMenuSelection<TimetableFileLite>(comboBoxTimetable, UserSettings.Menu_SelectionIndex.Timetable, t => t.Description);
            }
            UpdateEnabled();
        }
        #endregion

        #region Timetable Train list
        void ShowTimetableTrainList()
        {
            comboBoxTimetableTrain.Items.Clear();
            if (SelectedTimetable != null)
            {
                var trains = SelectedTimetableSet.ORTTList[comboBoxTimetable.SelectedIndex].Trains;
                trains.Sort();
                foreach (var train in trains)
                    comboBoxTimetableTrain.Items.Add(train);
                UpdateFromMenuSelection<TimetableFileLite.TrainInformation>(comboBoxTimetableTrain, UserSettings.Menu_SelectionIndex.Train, t => t.Column.ToString());
            }
            UpdateEnabled();
        }
        #endregion

        #region Timetable environment
        void ShowTimetableEnvironment()
        {
            UpdateFromMenuSelection<KeyedComboBoxItem>(comboBoxTimetableDay, UserSettings.Menu_SelectionIndex.Day, d => d.Key.ToString(), new KeyedComboBoxItem(0, ""));
            UpdateFromMenuSelection<KeyedComboBoxItem>(comboBoxTimetableSeason, UserSettings.Menu_SelectionIndex.Season, s => s.Key.ToString(), new KeyedComboBoxItem(1, ""));
            UpdateFromMenuSelection<KeyedComboBoxItem>(comboBoxTimetableWeather, UserSettings.Menu_SelectionIndex.Weather, w => w.Key.ToString(), new KeyedComboBoxItem(0, ""));
        }
        #endregion

        #region Details
        void ShowDetails()
        {
            Win32.LockWindowUpdate(Handle);
            ClearPanel();
            AddDetails();
            FlowDetails();
            Win32.LockWindowUpdate(IntPtr.Zero);
        }

        private void AddDetails()
        {
            if (SelectedRoute != null && SelectedRoute.Description != null)
                AddDetail(catalog.GetStringFmt("Route: {0}", SelectedRoute.Name), SelectedRoute.Description.Split('\n'));

            if (radioButtonModeActivity.Checked)
            {
                if (SelectedConsist != null && SelectedConsist.Locomotive != null && SelectedConsist.Locomotive.Description != null)
                {
                    AddDetail(catalog.GetStringFmt("Locomotive: {0}", SelectedConsist.Locomotive.Name), SelectedConsist.Locomotive.Description.Split('\n'));
                }
                if (SelectedActivity != null && SelectedActivity.Description != null)
                {
                    AddDetail(catalog.GetStringFmt("Activity: {0}", SelectedActivity.Name), SelectedActivity.Description.Split('\n'));
                    AddDetail(catalog.GetString("Activity Briefing"), SelectedActivity.Briefing.Split('\n'));
                }
                else if (SelectedPath != null)
                {
                    AddDetail(catalog.GetStringFmt("Path: {0}", SelectedPath.Name), new[] {
                        catalog.GetStringFmt("Starting at: {0}", SelectedPath.Start),
                        catalog.GetStringFmt("Heading to: {0}", SelectedPath.End)
                    });
                }
            }
            if (radioButtonModeTimetable.Checked)
            {
                if (SelectedTimetableSet != null)
                    AddDetail(catalog.GetStringFmt("Timetable set: {0}", SelectedTimetableSet), new string[0]);
                // Description not shown as no description is available for a timetable set.

                if (SelectedTimetable != null)
                    AddDetail(catalog.GetStringFmt("Timetable: {0}", SelectedTimetable), SelectedTimetable.Briefing.Split('\n'));

                if (SelectedTimetableTrain != null)
                {
                    AddDetail(catalog.GetStringFmt("Train: {0}", SelectedTimetableTrain), HideStartParameters(SelectedTimetableTrain.ToInfo()));

                    if (SelectedTimetableConsist != null)
                    {
                        AddDetail(catalog.GetStringFmt("Consist: {0}", SelectedTimetableConsist.Name), new string[0]);
                        if (SelectedTimetableConsist.Locomotive != null && SelectedTimetableConsist.Locomotive.Description != null)
                            AddDetail(catalog.GetStringFmt("Locomotive: {0}", SelectedTimetableConsist.Locomotive.Name), SelectedTimetableConsist.Locomotive.Description.Split('\n'));
                    }
                    if (SelectedTimetablePath != null)
                        AddDetail(catalog.GetStringFmt("Path: {0}", SelectedTimetablePath.Name), SelectedTimetablePath.ToInfo());
                }
            }
        }

        /// <summary>
        /// Change
        ///     "Start time: 10:30$$create=00:04/ahead=0040ElghLE70F363U"
        /// to
        ///     "Start time: 10:30"
        /// for higher-level presentation
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private string[] HideStartParameters(string[] info)
        {
            var fullStartTime = info[0].TrimStart();
            var startTimeArray = fullStartTime.Split('$');
            var shortStartTime = startTimeArray[0];
            info[0] = shortStartTime;
            return info;
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

        void ClearPanel()
        {
            Details.Clear();
            while (panelDetails.Controls.Count > 0)
                panelDetails.Controls.RemoveAt(0);
        }

        void AddDetail(string title, string[] lines)
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

            // Find out where we need to cut the text to make the summary 5 lines long. Uses a binary search to find the cut point.
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
        void UpdateFromMenuSelection<T>(ComboBox comboBox, UserSettings.Menu_SelectionIndex index, T defaultValue)
        {
            UpdateFromMenuSelection<T>(comboBox, index, _ => _.ToString(), defaultValue);
        }

        void UpdateFromMenuSelection<T>(ComboBox comboBox, UserSettings.Menu_SelectionIndex index, Func<T, string> map)
        {
            UpdateFromMenuSelection<T>(comboBox, index, map, default(T));
        }

        void UpdateFromMenuSelection<T>(ComboBox comboBox, UserSettings.Menu_SelectionIndex index, Func<T, string> map, T defaultValue)
        {
            if (Settings.Menu_Selection.Length > (int)index && Settings.Menu_Selection[(int)index] != "")
            {
                if (comboBox.DropDownStyle == ComboBoxStyle.DropDown)
                    comboBox.Text = Settings.Menu_Selection[(int)index];
                else
                    SelectComboBoxItem<T>(comboBox, item => map(item) == Settings.Menu_Selection[(int)index]);
            }
            else
            {
                if (comboBox.DropDownStyle == ComboBoxStyle.DropDown)
                    comboBox.Text = map(defaultValue);
                else if (defaultValue != null)
                    SelectComboBoxItem<T>(comboBox, item => map(item) == map(defaultValue));
                else if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;
            }
        }

        void SelectComboBoxItem<T>(ComboBox comboBox, Func<T, bool> predicate)
        {
            if (comboBox.Items.Count == 0)
                return;

            var index = (int)UserSettings.Menu_SelectionIndex.Activity;
            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is T && predicate((T)comboBox.Items[i]) || (Settings.Menu_Selection.Length > i && comboBox.Items[i].ToString() == Settings.Menu_Selection[index]))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }

        private class KeyedComboBoxItem
        {
            public readonly int Key;
            public readonly string Value;

            public override string ToString()
            {
                return Value;
            }

            public KeyedComboBoxItem(int key, string value)
            {
                Key = key;
                Value = value;
            }
        }

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

        #region Documentation
        void CheckForDocumentation()
        {

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

        void comboBoxTimetable_EnabledChanged(object sender, EventArgs e)
        {
            //Debrief Eval TTActivity.
            if (!comboBoxTimetable.Enabled)
            {
                //comboBoxTimetable.Enabled == false then we erase comboBoxTimetable and comboBoxTimetableTrain data.
                if (comboBoxTimetable.Items.Count > 0)
                {
                    comboBoxTimetable.Items.Clear();
                    comboBoxTimetableTrain.Items.Clear();
                    buttonStart.Enabled = false;
                }
            }
            //TO DO: Debrief Eval TTActivity
        }

        #region NotificationPages
        public int CurrentNotificationNo = 0;
        private bool firstVisible = true;
        private bool previousVisible = false;
        private bool lastVisible = false;
        private bool nextVisible = true;


        private void pbNotificationsNone_Click(object sender, EventArgs e)
        {
            ToggleNotificationPages();
        }
        private void pbNotificationsSome_Click(object sender, EventArgs e)
        {
            ToggleNotificationPages();
        }
        private void lblNotificationCount_Click(object sender, EventArgs e)
        {
            ToggleNotificationPages();
        }

        private void ToggleNotificationPages()
        {
            if (NotificationManager.ArePagesVisible == false)
            {
                NotificationManager.ArePagesVisible = true; // Set before calling ShowNotifcations()
                ShowNotificationPages();
                FiddleNewNotificationPageCount();
            }
            else
            {
                NotificationManager.ArePagesVisible = false;
                ShowDetails();
            }
        }

        private void FiddleNewNotificationPageCount()
        {
            //NotificationManager.LastPageViewed = 1;
            UpdateNotificationPageAlert();
        }

        public void UpdateNotificationPageAlert()
        {
            if (NotificationManager.LastPageViewed >= NotificationManager.NewPageCount)
            {
                pbNotificationsSome.Visible = false;
                lblNotificationCount.Visible = false;
            }
        }

        void ShowNotificationPages()
        {
            Win32.LockWindowUpdate(Handle);
            ClearPanel();
            NotificationManager.PopulatePage();
            var notificationPage = GetCurrentNotificationPage();
            notificationPage.FlowNDetails();
            Win32.LockWindowUpdate(IntPtr.Zero);
        }

        /// <summary>
        ///  INCOMPLETE
        /// </summary>
        /// <returns></returns>
        NotificationPage GetCurrentNotificationPage()
        {
            return NotificationManager.Page;
        }

        /// <summary>
        /// Returns a new notificationPage with default images and label
        /// </summary>
        /// <returns></returns>
        public NotificationPage CreateNotificationPage()
        // Located in MainForm to get access to MainForm.Resources
        {
            var previousImage = (Image)Resources.GetObject("Notification_previous");
            var nextImage = (Image)Resources.GetObject("Notification_next");
            var firstImage = (Image)Resources.GetObject("Notification_first");
            var lastImage = (Image)Resources.GetObject("Notification_last");
            var pageCount = $"{CurrentNotificationNo + 1}/{NotificationManager.Notifications.NotificationList.Count}";
            return new NotificationPage(this, panelDetails, nextImage, previousImage, firstImage, lastImage, pageCount,
                previousVisible, firstVisible, nextVisible, lastVisible);
        }

        // 3 should be enough, but is there a way to get unlimited buttons?
        public void Button0_Click(object sender, EventArgs e)
        {
            GetCurrentNotificationPage().DoButton(UpdateManager, 0);
        }
        public void Button1_Click(object sender, EventArgs e)
        {
            GetCurrentNotificationPage().DoButton(UpdateManager, 1);
        }
        public void Button2_Click(object sender, EventArgs e)
        {
            GetCurrentNotificationPage().DoButton(UpdateManager, 2);
        }

        public void Next_Click(object sender, EventArgs e)
        {
            ChangePage(1);
            // GetCurrentNotificationPage().DoNext(1);
        }

        public void Previous_Click(object sender, EventArgs e)
        {
            ChangePage(-1);
            //GetCurrentNotificationPage().DoNext(-1);
        }

        private void ChangePage(int step)
        {
            SetVisibility(step);
            CurrentNotificationNo += step;
            ShowNotificationPages();
        }

        private void SetVisibility(int step)
        {
            if (step < 0)
            {
                if (CurrentNotificationNo + step <= 0)
                {
                    previousVisible = false;
                    firstVisible = true;
                    return;
                }
            }
            else
            {
                if (CurrentNotificationNo + step >= NotificationManager.Notifications.NotificationList.Count - 1)
                {
                    nextVisible = false;
                    lastVisible = true;
                    return;
                }
            }
            nextVisible = true;
            lastVisible = false;
            previousVisible = true;
            firstVisible = false;
        }

        #endregion NotificationPages
    }
}
