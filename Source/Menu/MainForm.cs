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
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Menu;
using Path = System.IO.Path;

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
        UserSettings Settings;
        List<Folder> Folders = new List<Folder>();
        
        public List<Route> Routes = new List<Route>();  // So can be used for checking in ResumeForm 
        
        List<Activity> Activities = new List<Activity>();
        Task<List<Route>> RouteLoader;
        Task<List<Activity>> ActivityLoader;

        public Folder SelectedFolder { get { return listBoxFolders.SelectedIndex < 0 ? null : Folders[listBoxFolders.SelectedIndex]; } }
        public Route SelectedRoute { get { return listBoxRoutes.SelectedIndex < 0 ? null : Routes[listBoxRoutes.SelectedIndex]; } }
        public Activity SelectedActivity { get { return listBoxActivities.SelectedIndex < 0 ? null : Activities[listBoxActivities.SelectedIndex]; } set { if (listBoxActivities.SelectedIndex >= 0) Activities[listBoxActivities.SelectedIndex] = value; } }
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
        }

        void MainForm_Shown(object sender, EventArgs e)
        {
            var options = Environment.GetCommandLineArgs().Where(a => (a.StartsWith("-") || a.StartsWith("/"))).Select(a => a.Substring(1));
            Settings = new UserSettings(Program.RegistryKey, options);

            LoadOptions();

            if (!Initialized)
            {
                Initialized = true;

                LoadFolders();

                if (Folders.Count == 0)
                    MessageBox.Show("Microsoft Train Simulator doesn't appear to be installed.\nClick on 'Add...' to point Open Rails at your Microsoft Train Simulator folder.", Application.ProductName);
            }
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveOptions();
            if (RouteLoader != null)
                RouteLoader.Cancel();
            if (ActivityLoader != null)
                ActivityLoader.Cancel();

            // Empty the deleted_saves folder
            if (Directory.Exists(Program.DeletedSaveFolder))
                Directory.Delete(Program.DeletedSaveFolder, true);   // true removes all contents as well as folder

            // Tidy up after versions which used SAVE.BIN
            var file = Program.UserDataFolder + @"\SAVE.BIN";
            if (File.Exists(file))
                File.Delete(file);
        }
        #endregion

        #region Folders
        void listBoxFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadRoutes();
        }

        void buttonFolderAdd_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = SelectedFolder != null ? SelectedFolder.Path : "";
                folderBrowser.Description = "Navigate to your alternate MSTS installation folder.";
                folderBrowser.ShowNewFolderButton = false;
                if (folderBrowser.ShowDialog(this) == DialogResult.OK)
                {
                    using (var form = new FormFolderName())
                    {
                        form.textBoxDescription.Text = Path.GetFileName(folderBrowser.SelectedPath);
                        if (form.ShowDialog(this) == DialogResult.OK)
                        {
                            Folders.Add(new Folder(form.textBoxDescription.Text, folderBrowser.SelectedPath));
                            SaveFolders();
                            LoadFolders();
                        }
                    }
                }
            }
        }

        void buttonFolderRemove_Click(object sender, EventArgs e)
        {
            var index = listBoxFolders.SelectedIndex;
            if (index >= 0)
            {
                listBoxFolders.ClearSelected();
                listBoxFolders.Items.RemoveAt(index);
                Folders.RemoveAt(index);
                SaveFolders();
                if (listBoxFolders.Items.Count > 0)
                    listBoxFolders.SelectedIndex = 0;
            }
        }
        #endregion

        #region Routes
        void listBoxRoutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadActivities();
        }

        void listBoxRoutes_DoubleClick(object sender, EventArgs e)
        {
            DisplayRouteDetails();
        }

        void buttonRouteDetails_Click(object sender, EventArgs e)
        {
            DisplayRouteDetails();
        }
        #endregion

        #region Activities
        void listBoxActivities_DoubleClick(object sender, EventArgs e)
        {
            DisplayActivityDetails();
        }

        void buttonActivityDetails_Click(object sender, EventArgs e)
        {
            DisplayActivityDetails();
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

        void buttonResume_Click(object sender, EventArgs e)
        {
            using (var form = new ResumeForm(Settings, SelectedRoute, SelectedActivity, this))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SelectedSaveFile = form.SelectedSaveFile;
                    SelectedAction = form.SelectedAction;
                    DialogResult = DialogResult.OK;
                }
            }
        }

        void buttonStart_Click(object sender, EventArgs e)
        {
            SaveOptions();

            SelectedAction = UserAction.SingleplayerNewGame;

            // GetMultiplayerInfo() overrides SelectedAction.
            if (checkBoxMultiplayer.Checked && !GetMultiplayerInfo())
                return;

            if (SelectedActivity != null && SelectedActivity.FilePath != null)
            {
                DialogResult = DialogResult.OK;
            }
            else if (SelectedActivity != null && SelectedActivity.FilePath == null)
            {
                if (GetExploreInfo())
                    DialogResult = DialogResult.OK;
            }
        }
        #endregion

        void CleanupPre021()
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
                        Directory.Delete(Path.GetDirectoryName(oldFolderDataFileName), true);
                    }
                }
                catch
                {
                }
            }
        }

        void LoadOptions()
        {
            checkBoxWarnings.Checked = Settings.Logging;
            checkBoxWindowed.Checked = !Settings.FullScreen;
            checkBoxMultiplayer.Checked = Settings.Multiplayer;
        }

        void SaveOptions()
        {
            Settings.Logging = checkBoxWarnings.Checked;
            Settings.FullScreen = !checkBoxWindowed.Checked;
            Settings.Multiplayer = checkBoxMultiplayer.Checked;
            Settings.Menu_Selection = new[] {
                listBoxFolders.SelectedItem != null ? (listBoxFolders.SelectedItem as Folder).Path : "",
                listBoxRoutes.SelectedItem != null ? (listBoxRoutes.SelectedItem as Route).Path : "",
                listBoxActivities.SelectedItem != null && (listBoxActivities.SelectedItem as Activity).FilePath != null ? (listBoxActivities.SelectedItem as Activity).FilePath : "",
            };
            Settings.Save();
        }

        void LoadFolders()
        {
            try
            {
                Folders = Folder.GetFolders().OrderBy(f => f.Name).ToList();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
            }
            listBoxFolders.Items.Clear();
            foreach (var folder in Folders)
                listBoxFolders.Items.Add(folder);
            var selectionIndex = Settings.Menu_Selection.Length > 0 ? Folders.FindIndex(f => f.Path == Settings.Menu_Selection[0]) : -1;
            if (selectionIndex >= 0)
                listBoxFolders.SelectedIndex = selectionIndex;
            else if (Folders.Count > 0)
                listBoxFolders.SelectedIndex = 0;
            else
                listBoxFolders.ClearSelected();
        }

        void SaveFolders()
        {
            Folder.SetFolders(Folders);
        }

        void LoadRoutes()
        {
            if (RouteLoader != null)
                RouteLoader.Cancel();

            listBoxRoutes.Items.Clear();
            buttonRouteDetails.Enabled = buttonActivityDetails.Enabled = buttonResume.Enabled = buttonStart.Enabled = false;
            var selectedFolder = SelectedFolder;
            RouteLoader = new Task<List<Route>>(this, () => Route.GetRoutes(selectedFolder).OrderBy(r => r.ToString()).ToList(), (routes) =>
            {
                Routes = routes;
                labelRoutes.Visible = Routes.Count == 0;
                foreach (var route in Routes)
                    listBoxRoutes.Items.Add(route);
                var selectionIndex = Settings.Menu_Selection.Length > 1 ? routes.FindIndex(f => f.Path == Settings.Menu_Selection[1]) : -1;
                if (selectionIndex >= 0)
                    listBoxRoutes.SelectedIndex = selectionIndex;
                else if (routes.Count > 0)
                    listBoxRoutes.SelectedIndex = 0;
                else
                    listBoxRoutes.ClearSelected();
                buttonRouteDetails.Enabled = listBoxRoutes.Items.Count > 0;
            });
        }

        void LoadActivities()
        {
            if (ActivityLoader != null)
                ActivityLoader.Cancel();

            listBoxActivities.Items.Clear();
            buttonActivityDetails.Enabled = buttonResume.Enabled = buttonStart.Enabled = false;
            var selectedRoute = SelectedRoute;
            ActivityLoader = new Task<List<Activity>>(this, () => Activity.GetActivities(selectedRoute).OrderBy(a => a.ToString()).ToList(), (activities) =>
            {
                Activities = activities;
                labelActivities.Visible = Activities.Count == 0;
                foreach (var activity in Activities)
                    listBoxActivities.Items.Add(activity);
                var selectionIndex = Settings.Menu_Selection.Length > 2 ? activities.FindIndex(f => f.FilePath == Settings.Menu_Selection[2]) : -1;
                if (selectionIndex >= 0)
                    listBoxActivities.SelectedIndex = selectionIndex;
                else if (activities.Count > 0)
                    listBoxActivities.SelectedIndex = 0;
                else
                    listBoxActivities.ClearSelected();
                buttonActivityDetails.Enabled = buttonResume.Enabled = buttonStart.Enabled = listBoxActivities.Items.Count > 0;
            });
        }

        void DisplayRouteDetails()
        {
            if (listBoxRoutes.SelectedIndex >= 0)
            {
                using (var form = new DetailsForm(SelectedRoute))
                {
                    form.ShowDialog(this);
                }
            }
        }

        void DisplayActivityDetails()
        {
            if (listBoxActivities.SelectedIndex == 0)
                GetExploreInfo();
            else if (listBoxActivities.SelectedIndex > 0)
            {
                using (var form = new DetailsForm(SelectedActivity))
                {
                    form.ShowDialog(this);
                }
            }
        }

        bool GetExploreInfo()
        {
            using (var form = new ExploreForm(SelectedFolder, SelectedRoute, (ExploreActivity)SelectedActivity))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SelectedActivity = form.NewExploreActivity;
                    return true;
                }
                return false;
            }
        }

        bool GetMultiplayerInfo()
        {
            using (var form = new MultiplayerForm(Settings))
            {
                switch (form.ShowDialog(this))
                {
                    case DialogResult.Yes:
                        SelectedAction = UserAction.MultiplayerServer;
                        return true;
                    case DialogResult.No:
                        SelectedAction = UserAction.MultiplayerClient;
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
