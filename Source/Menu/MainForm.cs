// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using ORTS.Menu;

namespace ORTS
{
	public partial class MainForm : Form
	{
        bool Initialized;
		List<Folder> Folders = new List<Folder>();
		List<Route> Routes = new List<Route>();
		List<Activity> Activities = new List<Activity>();
		Task<List<Route>> RouteLoader;
		Task<List<Activity>> ActivityLoader;

		public Folder SelectedFolder { get { return listBoxFolders.SelectedIndex < 0 ? null : Folders[listBoxFolders.SelectedIndex]; } }
		public Route SelectedRoute { get { return listBoxRoutes.SelectedIndex < 0 ? null : Routes[listBoxRoutes.SelectedIndex]; } }
		public Activity SelectedActivity { get { return listBoxActivities.SelectedIndex < 0 ? null : Activities[listBoxActivities.SelectedIndex]; } set { if (listBoxActivities.SelectedIndex >= 0) Activities[listBoxActivities.SelectedIndex] = value; } }

        #region Main Form
		public MainForm()
		{
			InitializeComponent();

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;

			// Set title to show revision or build info.
			Text = String.Format(Program.Version.Length > 0 ? "{0} {1}" : "{0} BUILD {2}", Application.ProductName, Program.Version, Program.Build);

			CleanupPre021();
		}

		void MainForm_Shown(object sender, EventArgs e)
		{
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
		}
		#endregion

		#region Folders
		void listBoxFolder_SelectedIndexChanged(object sender, EventArgs e)
		{
			LoadRoutes();
		}

		void buttonFolderAdd_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
			{
				folderBrowser.SelectedPath = SelectedFolder != null ? SelectedFolder.Path : "";
				folderBrowser.Description = "Navigate to your alternate MSTS installation folder.";
				folderBrowser.ShowNewFolderButton = false;
				if (folderBrowser.ShowDialog(this) == DialogResult.OK)
				{
					using (FormFolderName form = new FormFolderName())
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
			int index = listBoxFolders.SelectedIndex;
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
		void buttonSwitchStyle_Click(object sender, EventArgs e)
		{
			using (RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey, true))
			{
				if (RK != null)
				{
					RK.SetValue("LauncherMenu", 2);
				}
			}
			Process.Start(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "MenuWPF.exe"));
			Close();
		}

		void buttonOptions_Click(object sender, EventArgs e)
		{
			using (var form = new OptionsForm())
			{
				form.ShowDialog(this);
			}
		}

		void buttonStart_Click(object sender, EventArgs e)
		{
			SaveOptions();
			if (SelectedActivity != null && SelectedActivity.FileName != null)
			{
				DialogResult = DialogResult.OK;
			}
			else if (SelectedActivity != null && SelectedActivity.FileName == null)
			{
				if (GetExploreInfo())
					DialogResult = DialogResult.OK;
			}
		}
		#endregion

		void CleanupPre021()
		{
			// Handle cleanup from pre version 0021
			if (null != Registry.CurrentUser.OpenSubKey("SOFTWARE\\ORTS"))
				Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\ORTS");

			if (!File.Exists(Folder.FolderDataFile))
			{
				// Handle name change that occured at version 0021
				string oldFolderDataFileName = Program.UserDataFolder + @"\..\ORTS\folder.dat";
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
			// Restore retained settings
			using (RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey, true))
			{
				if (RK != null)
				{
					checkBoxWindowed.Checked = (int)RK.GetValue("Fullscreen", 0) == 1 ? false : true;
					checkBoxWarnings.Checked = (int)RK.GetValue("Logging", 1) == 1 ? true : false;
				}
			}

			var savedGameFile = Path.Combine(Program.UserDataFolder, "save.bin");
			buttonResume.Enabled = File.Exists(savedGameFile);
		}

		void SaveOptions()
		{
			// Retain settings for convenience
			using (RegistryKey RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey))
			{
				if (RK != null)
				{
					RK.SetValue("Fullscreen", checkBoxWindowed.Checked ? 0 : 1);
                    RK.SetValue("Logging", checkBoxWarnings.Checked ? 1 : 0);
				}
			}
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
				listBoxFolders.Items.Add(folder.Name);

			if (Folders.Count > 0)
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
			var selectedFolder = SelectedFolder;
			RouteLoader = new Task<List<Route>>(this, () => Route.GetRoutes(selectedFolder).OrderBy(r => r.Name).ToList(), (routes) =>
			{
				Routes = routes;
				labelRoutes.Visible = Routes.Count == 0;
				foreach (var route in Routes)
					listBoxRoutes.Items.Add(route.Name);
				if (Routes.Count > 0)
					listBoxRoutes.SelectedIndex = 0;
				else
					LoadActivities();
			});
		}

		void LoadActivities()
		{
			if (ActivityLoader != null)
				ActivityLoader.Cancel();

			listBoxActivities.Items.Clear();
			var selectedRoute = SelectedRoute;
            ActivityLoader = new Task<List<Activity>>(this, () => Activity.GetActivities(selectedRoute).OrderBy(a => a.Name).ToList(), (activities) =>
            {
                Activities = activities;
                labelActivities.Visible = Activities.Count == 0;
                foreach (var activity in Activities)
                    listBoxActivities.Items.Add(activity.Name);
                if (Activities.Count > 0)
                    listBoxActivities.SelectedIndex = 0;
            });
		}

		void DisplayRouteDetails()
		{
			if (listBoxRoutes.SelectedIndex >= 0)
			{
				using (DetailsForm form = new DetailsForm(SelectedRoute))
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
				using (DetailsForm form = new DetailsForm(SelectedActivity))
				{
					form.ShowDialog(this);
				}
			}
		}

		bool GetExploreInfo()
		{
			using (ExploreForm form = new ExploreForm(SelectedFolder, SelectedRoute, (ExploreActivity)SelectedActivity))
			{
				if (form.ShowDialog(this) == DialogResult.OK)
				{
					SelectedActivity = form.NewExploreActivity;
					return true;
				}
				return false;
			}
		}
	}
}
