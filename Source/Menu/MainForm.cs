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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Menu;
using Path = ORTS.Menu.Path;
using System.Resources;

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
        }

        bool Initialized;
        public UserSettings Settings;
        List<Folder> Folders = new List<Folder>();
        public List<Route> Routes = new List<Route>();
        List<Activity> Activities = new List<Activity>();
        List<Consist> Consists = new List<Consist>();
        List<Path> Paths = new List<Path>();
        Task<List<Route>> RouteLoader;
        Task<List<Activity>> ActivityLoader;
        Task<List<Consist>> ConsistLoader;
        Task<List<Path>> PathLoader;
        readonly ResourceManager Resources = new ResourceManager("ORTS.Properties.Resources", typeof(MainForm).Assembly);

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

            CleanupPre021();
            ShowEnvironment();
            ShowDetails();
            UpdateEnabled();
        }

        void MainForm_Shown(object sender, EventArgs e)
        {
            var options = Environment.GetCommandLineArgs().Where(a => (a.StartsWith("-") || a.StartsWith("/"))).Select(a => a.Substring(1));
            Settings = UserSettings.GetSettings(Program.RegistryKey, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "OpenRails.ini"), options);

            LoadOptions();

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

            // Remove any deleted saves
            if (Directory.Exists(Program.DeletedSaveFolder))
                Directory.Delete(Program.DeletedSaveFolder, true);   // true removes all contents as well as folder

            // Tidy up after versions which used SAVE.BIN
            var file = Program.UserDataFolder + @"\SAVE.BIN";
            if (File.Exists(file))
                File.Delete(file);
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
                folderBrowser.Description = "Select a the installation profile (MSTS folder) to add:";
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
            if (MessageBox.Show("Path: " + folder.Path + "\nName: " + folder.Name + "\n\nRemove this installation profile from Open Rails?", Application.ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            ShowDetails();
        }
        #endregion

        #region Activities
        void comboBoxActivity_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowLocomotiveList();
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
				MessageBox.Show("User name must be 4-10 characters long, cannot contain space, ', \" or - and must not start with a digit.");
				return false;
			}
			return true;
		}

        #endregion

        #region Misc. buttons and options
        void buttonTesting_Click(object sender, EventArgs e)
        {
            using (var form = new TestingForm())
            {
                form.ShowDialog(this);
            }
        }

        void buttonOptions_Click(object sender, EventArgs e)
        {
            using (var form = new OptionsForm(Settings))
            {
                form.ShowDialog(this);
            }
        }

        void buttonStart_Click(object sender, EventArgs e)
        {
            SaveOptions();
            SelectedAction = UserAction.SingleplayerNewGame;
            if (SelectedActivity != null)
                DialogResult = DialogResult.OK;
        }

        void buttonResume_Click(object sender, EventArgs e)
        {
            using (var form = new ResumeForm(Settings, SelectedRoute, SelectedActivity, this))
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

        static void CleanupPre021()
        {
            // Handle cleanup from pre version 0021
            using (var RK = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\ORTS"))
            {
                if (RK != null)
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\ORTS");
            }

            if (!File.Exists(Folder.FolderDataFile))
            {
                // Handle name change that occured at version 0021
                var oldFolderDataFileName = Program.UserDataFolder + @"\..\ORTS\folder.dat";
                try
                {
                    if (File.Exists(oldFolderDataFileName))
                    {
                        File.Copy(oldFolderDataFileName, Folder.FolderDataFile);
                        Directory.Delete(System.IO.Path.GetDirectoryName(oldFolderDataFileName), true);
                    }
                }
                catch
                {
                }
            }
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

            new Task<List<Folder>>(this, () => Folder.GetFolders().OrderBy(f => f.Name).ToList(), (folders) =>
            {
                Folders = folders;
                if (Folders.Count == 0)
                    MessageBox.Show("Microsoft Train Simulator doesn't appear to be installed.\nClick on 'Add...' to point Open Rails at your Microsoft Train Simulator folder.", Application.ProductName);
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
            Folder.SetFolders(Folders);
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
            exploreActivity.Season = (MSTS.SeasonType)SelectedStartSeason;
            exploreActivity.Weather = (MSTS.WeatherType)SelectedStartWeather;
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
                comboBoxLocomotive.Items.Add(new Locomotive(null));
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
            PathLoader = new Task<List<Path>>(this, () => Path.GetPaths(selectedRoute).OrderBy(a => a.ToString()).ToList(), (paths) =>
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
                ShowDetail(String.Format("Route: {0}", SelectedRoute.Name), SelectedRoute.Description.Split('\n'));
            if (SelectedConsist != null && SelectedConsist.Locomotive != null && SelectedConsist.Locomotive.Description != null)
                ShowDetail(String.Format("Locomotive: {0}", SelectedConsist.Locomotive.Name), SelectedConsist.Locomotive.Description.Split('\n'));
            if (SelectedActivity != null && SelectedActivity.Description != null)
            {
                ShowDetail(String.Format("Activity: {0}", SelectedActivity.Name), SelectedActivity.Description.Split('\n'));
                ShowDetail("Activity Briefing", SelectedActivity.Briefing.Split('\n'));
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
            var titleControl = new Label { Margin = new Padding(2), Text = title, Font = new Font(panelDetails.Font, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
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
    }
}
