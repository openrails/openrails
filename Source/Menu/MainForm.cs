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
using MSTS;

namespace ORTS
{
	public partial class MainForm : Form
	{
		public const string FolderDataFileName = "folder.dat";

		string FolderDataFile;
		List<Folder> Folders = new List<Folder>();
		List<Route> Routes = new List<Route>();
		List<Activity> Activities = new List<Activity>();
		Task<List<Route>> RouteLoader;
		Task<List<Activity>> ActivityLoader;

		public Folder SelectedFolder { get { return listBoxFolders.SelectedIndex < 0 ? null : Folders[listBoxFolders.SelectedIndex]; } }
		public Route SelectedRoute { get { return listBoxRoutes.SelectedIndex < 0 ? null : Routes[listBoxRoutes.SelectedIndex]; } }
		public Activity SelectedActivity { get { return listBoxActivities.SelectedIndex < 0 ? null : Activities[listBoxActivities.SelectedIndex]; } set { if (listBoxActivities.SelectedIndex >= 0) Activities[listBoxActivities.SelectedIndex] = value; } }

		public class Folder
		{
			public readonly string Name;
			public readonly string Path;

			public Folder(string name, string path)
			{
				Name = name;
				Path = path;
			}
		}

		public class Route
		{
			public readonly string Name;
			public readonly string Path;
			public readonly TRKFile TRKFile;

			public Route(string name, string path, TRKFile trkFile)
			{
				Name = name;
				Path = path;
				TRKFile = trkFile;
			}
		}

		public class Activity
		{
			public readonly string Name;
			public readonly string FileName;
			public readonly ACTFile ACTFile;

			public Activity(string name, string fileName, ACTFile actFile)
			{
				Name = name;
				FileName = fileName;
				ACTFile = actFile;
			}
		}

		public class ExploreActivity : Activity
		{
			public readonly string Path;
			public readonly string Consist;
			public readonly int StartHour;
			public readonly int StartMinute;
			public readonly int Season;
			public readonly int Weather;

			public ExploreActivity(string path, string consist, int season, int weather, int startHour, int startMinute)
				: base("- Explore Route -", null, null)
			{
				Path = path;
				Consist = consist;
				Season = season;
				Weather = weather;
				StartHour = startHour;
				StartMinute = startMinute;
			}

			public ExploreActivity()
				: this("", "", 0, 0, 12, 0)
			{
			}
		}

		#region Main Form
		public MainForm()
		{
			InitializeComponent();

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;

			// Set title to show revision or build info.
			Text = String.Format(Program.Revision == "000" ? "{0} BUILD {2}" : "{0} V{1}", Application.ProductName, Program.Revision, Program.Build);

			FolderDataFile = Program.UserDataFolder + @"\" + FolderDataFileName;

			CleanupPre021();

			LoadFolders();
		}

		void MainForm_Shown(object sender, EventArgs e)
		{
			LoadOptions();
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
							var folder = new Folder(form.textBoxDescription.Text, folderBrowser.SelectedPath);
							Folders.Add(folder);
							listBoxFolders.Items.Add(folder.Name);
							if (listBoxFolders.SelectedIndex < 0 && listBoxFolders.Items.Count > 0)
								listBoxFolders.SelectedIndex = 0;
							SaveFolders();
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

			if (!File.Exists(FolderDataFile))
			{
				// Handle name change that occured at version 0021
				string oldFolderDataFileName = Program.UserDataFolder + @"\..\ORTS\folder.dat";
				try
				{
					if (File.Exists(oldFolderDataFileName))
					{
						File.Copy(oldFolderDataFileName, FolderDataFile);
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
					checkBoxWarnings.Checked = (int)RK.GetValue("Warnings", 1) == 1 ? true : false;
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
					RK.SetValue("Warnings", checkBoxWarnings.Checked ? 1 : 0);
				}
			}
		}

		void LoadFolders()
		{
			Folders = new List<Folder>();

			if (File.Exists(FolderDataFile))
			{
				try
				{
					using (var inf = new BinaryReader(File.Open(FolderDataFile, FileMode.Open)))
					{
						var count = inf.ReadInt32();
						for (var i = 0; i < count; ++i)
						{
							var path = inf.ReadString();
							var name = inf.ReadString();
							Folders.Add(new Folder(name, path));
						}
					}
				}
				catch (Exception error)
				{
					MessageBox.Show(error.ToString());
				}
			}

			if (Folders.Count == 0)
			{
				try
				{
					Folders.Add(new Folder("- Default -", MSTSPath.Base()));
				}
				catch (Exception)
				{
					MessageBox.Show("Microsoft Train Simulator doesn't appear to be installed.\nClick on 'Add...' to point Open Rails at your Microsoft Train Simulator folder.", Application.ProductName);
				}
			}

			Folders = Folders.OrderBy(f => f.Name).ToList();

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
			using (BinaryWriter outf = new BinaryWriter(File.Open(FolderDataFile, FileMode.Create)))
			{
				outf.Write(Folders.Count);
				foreach (var folder in Folders)
				{
					outf.Write(folder.Path);
					outf.Write(folder.Name);
				}
			}
		}

		void LoadRoutes()
		{
			if (RouteLoader != null)
				RouteLoader.Cancel();

			listBoxRoutes.Items.Clear();
			var selectedFolder = SelectedFolder;
			RouteLoader = new Task<List<Route>>(this, () =>
			{
				var routes = new List<Route>();
				if (selectedFolder != null)
				{
					var directory = Path.Combine(selectedFolder.Path, "ROUTES");
					if (Directory.Exists(directory))
					{
						foreach (var routeDirectory in Directory.GetDirectories(directory))
						{
							try
							{
								var trkFile = new TRKFile(MSTSPath.GetTRKFileName(routeDirectory));
								routes.Add(new Route(trkFile.Tr_RouteFile.Name, routeDirectory, trkFile));
							}
							catch { }
						}
					}
				}
				return routes.OrderBy(r => r.Name).ToList();
			}, (routes) =>
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
			ActivityLoader = new Task<List<Activity>>(this, () =>
			{
				var activities = new List<Activity>();
				if (selectedRoute != null)
				{
					activities.Add(new ExploreActivity());
					var directory = Path.Combine(selectedRoute.Path, "ACTIVITIES");
					if (Directory.Exists(directory))
					{
						foreach (var activityFile in Directory.GetFiles(directory, "*.act"))
						{
							if (Path.GetFileName(activityFile).StartsWith("ITR_e1_s1_w1_t1", StringComparison.OrdinalIgnoreCase))
								continue;
							try
							{
								var actFile = new ACTFile(activityFile, true);
								activities.Add(new Activity(actFile.Tr_Activity.Tr_Activity_Header.Name, activityFile, actFile));
							}
							catch { }
						}
					}
				}
				return activities.OrderBy(a => a.Name).ToList();
			}, (activities) =>
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
