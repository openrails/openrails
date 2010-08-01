/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MSTS;
using Microsoft.Win32;

namespace ORTS
{
    public partial class MainForm : Form
    {
        public string SelectedRoutePath { get{   return routePaths[listBoxRoutes.SelectedIndex]; } }
        public string SelectedActivityPath { get { return activityPaths[listBoxActivities.SelectedIndex]; } }
        public string SelectedPath { get { return ExplorePatFile; } }
        public string SelectedConsist { get { return ExploreConFile; } }

        List<string> folderPaths = new List<string>();
        List<string> routePaths = new List<string>();
        List<string> activityPaths;
        public string ExplorePatFile = null;
        public string ExploreConFile = null;
        public int ExploreSeason = 0;
        public int ExploreWeather = 0;
        public int ExploreStartHour = 12;

        string FolderDataFileName = "folder.dat";


        public MainForm()
        {
            string UserDataFolder = Path.GetDirectoryName( Path.GetDirectoryName(Application.UserAppDataPath));
            FolderDataFileName = UserDataFolder + @"\" + FolderDataFileName;

            InitializeComponent();

            listBoxActivities.DoubleClick += new EventHandler(listBoxActivities_DoubleClick);
            listBoxRoutes.DoubleClick += new EventHandler(listBoxRoutes_DoubleClick);

            // Handle cleanup from pre version 0021
            if( null != Registry.CurrentUser.OpenSubKey("SOFTWARE\\ORTS"))
                Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\ORTS");

            if (!File.Exists(FolderDataFileName))
            {
                // Handle name change that occured at version 0021
                string oldFolderDataFileName = UserDataFolder + @"\..\ORTS\folder.dat";
                try
                {
                    if (File.Exists(oldFolderDataFileName))
                    {
                        File.Copy(oldFolderDataFileName, FolderDataFileName);
                        Directory.Delete(Path.GetDirectoryName(oldFolderDataFileName), true);
                    }
                }
                catch
                {
                }
            }


            // Restore retained settings
            RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey, true);
            if (RK != null)
            {
                checkBoxFullScreen.Checked = (int)RK.GetValue("Fullscreen", 0) == 1 ? true : false;
                checkBoxWarnings.Checked = (int)RK.GetValue("Warnings", 1) == 1 ? true : false;
            }


            listBoxFolders.Items.Clear();


            if (File.Exists(FolderDataFileName))
            {
                try
                {
                    ReadFolderDat();
                }
                catch (System.Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
            if (folderPaths.Count == 0)
            {
                try
                {
                    folderPaths.Add(MSTSPath.Base());
                    listBoxFolders.Items.Add("- Default -");
                }
                catch (System.Exception)
                {
                    MessageBox.Show("MSTS doesn't seem to be installed.\nClick on 'Add Folder' to point ORTS at your MSTS installation folder");
                }
            }
            if (folderPaths.Count > 0)
                listBoxFolders.SelectedIndex = 0;
            else
                listBoxFolders.ClearSelected();
        }

        void listBoxRoutes_DoubleClick(object sender, EventArgs e)
        {
            DisplayRouteDetails();
        }

        void listBoxActivities_DoubleClick(object sender, EventArgs e)
        {
            DisplayActivityDetails();
        }

        private void listBoxFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBoxRoutes.Items.Clear();
            listBoxRoutes.Refresh();
            listBoxActivities.Items.Clear();
            listBoxActivities.Refresh();
            ExploreConFile = null;

            try
            {
                if (listBoxFolders.SelectedIndex < 0) return;
                string folderPath = folderPaths[listBoxFolders.SelectedIndex];
                routePaths.Clear();
                string[] directories = Directory.GetDirectories(folderPath + @"\ROUTES");


                // create a list of routes
                foreach (string routePath in directories)
                {
                    string routeFolder = Path.GetFileName(routePath);
                    try
                    {
                        TRKFile trkFile = new TRKFile(MSTSPath.GetTRKFileName(routePath));
                        listBoxRoutes.Items.Add(trkFile.Tr_RouteFile.Name);
                        routePaths.Add(routePath);
                    }
                    catch
                    {
                    }
                }

                if (listBoxRoutes.Items.Count > 0)
                    listBoxRoutes.SelectedIndex = 0;
                else
                    listBoxRoutes.ClearSelected();
            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void listBoxRoutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBoxActivities.Items.Clear();
            listBoxActivities.Refresh();

            try
            {
                if (listBoxRoutes.SelectedIndex < 0) return;

                activityPaths = new List<string>();
                string[] allActivityPaths = Directory.GetFiles(SelectedRoutePath + @"\ACTIVITIES", "*.act");

                listBoxActivities.Items.Add("Explore Route");
                activityPaths.Add(null);
                ExplorePatFile = null;

                // create a list of activities
                foreach (string activityPath in allActivityPaths)
                {
                    if (0 != string.Compare(Path.GetFileNameWithoutExtension(activityPath), "ITR_e1_s1_w1_t1", true))  // ignore these, seems to be some sort of internal function
                    {
                        try
                        {
                            ACTFile actFile = new ACTFile(activityPath, true);
                            listBoxActivities.Items.Add(actFile.Tr_Activity.Tr_Activity_Header.Name);
                            activityPaths.Add(activityPath);
                        }
                        catch 
                        {
                        }
                    }
                }
                if (listBoxActivities.Items.Count > 0)
                    listBoxActivities.SelectedIndex = 0;
                else
                    listBoxActivities.ClearSelected();
            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
			SaveOptions();
			if (listBoxActivities.SelectedIndex == 0)
            {
                if (GetExploreInfo() && ExploreConFile != null && ExplorePatFile != null)
                    DialogResult = DialogResult.OK;
            }
            else if (listBoxActivities.SelectedIndex >= 0 )
            {              
                DialogResult = DialogResult.OK;
            }
        }

		private void SaveOptions()
		{
			// Retain settings for convenience
			RegistryKey RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey);
			if (RK != null)
			{
				RK.SetValue("Fullscreen", checkBoxFullScreen.Checked ? 1 : 0);
				RK.SetValue("Warnings", checkBoxWarnings.Checked ? 1 : 0);
			}
		}

        private void buttonAddFolder_Click(object sender, EventArgs e)
        {
            string folderPath = "";
            if( listBoxFolders.SelectedIndex >= 0 )
                folderPath = folderPaths[listBoxFolders.SelectedIndex];
            FolderBrowserDialog f = new FolderBrowserDialog();
            f.SelectedPath = folderPath;
            f.Description = "Navigate to your alternate MSTS installation folder.";
            f.ShowNewFolderButton = false;
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                FormFolderName form = new FormFolderName();
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    listBoxFolders.Items.Add(form.Description);
                    folderPaths.Add(f.SelectedPath);
                    if (listBoxFolders.SelectedIndex < 0 && listBoxFolders.Items.Count > 0 )
                        listBoxFolders.SelectedIndex = 0;
                    SaveFolderDat();
                }
            }
        }

        private void SaveFolderDat()
        {
            // save the file
            using (BinaryWriter outf = new BinaryWriter(File.Open(FolderDataFileName, FileMode.Create)))
            {
                outf.Write(folderPaths.Count);

                for (int i = 0; i < folderPaths.Count; ++i)
                {
                    outf.Write(folderPaths[i]);
                    outf.Write((string)listBoxFolders.Items[i]);
                }
            }
        }

        private void ReadFolderDat()
        {
            // save the file
            using (BinaryReader inf = new BinaryReader(File.Open(FolderDataFileName, FileMode.Open)))
            {
                int count = inf.ReadInt32();

                for (int i = 0; i < count; ++i)
                {
                    folderPaths.Add(inf.ReadString());
                    listBoxFolders.Items.Add(inf.ReadString());
                }
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (listBoxFolders.SelectedIndex >= 0)
            {
                int i = listBoxFolders.SelectedIndex;
                listBoxFolders.ClearSelected();
                listBoxFolders.Items.RemoveAt(i);
                folderPaths.RemoveAt(i);
                SaveFolderDat();
                if( listBoxFolders.Items.Count > 0 )
                    listBoxFolders.SelectedIndex = 0;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
			SaveOptions();
        }

        private void buttonOptions_Click(object sender, EventArgs e)
        {
            (new OptionsForm()).ShowDialog(this);
        }

        private void DisplayRouteDetails()
        {
            if (listBoxRoutes.SelectedIndex >= 0)
            {
                DetailsForm frmDetails = new DetailsForm();
                if (frmDetails.RouteDetails(SelectedRoutePath))
                {
                    frmDetails.ShowDialog(this);
                }
            }
        }

        private void DisplayActivityDetails()
        {
            if (listBoxActivities.SelectedIndex == 0)
                GetExploreInfo();
            else if (listBoxActivities.SelectedIndex > 0)
            {
                DetailsForm frmDetails = new DetailsForm();
                if (frmDetails.ActivityDetails(SelectedActivityPath))
                {
                    frmDetails.ShowDialog(this);
                }
            }
        }

        private bool GetExploreInfo()
        {
            ExploreForm form = new ExploreForm();
            form.LoadData(folderPaths[listBoxFolders.SelectedIndex], SelectedRoutePath, ExplorePatFile, ExploreConFile, ExploreSeason, ExploreWeather, ExploreStartHour);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ExplorePatFile = form.SelectedPath;
                ExploreConFile = form.SelectedConsist;
                ExploreStartHour = form.SelectedStartHour;
                ExploreSeason = form.SelectedSeason;
                ExploreWeather = form.SelectedWeather;
                return true;
            }
            return false;
        }

        private void buttonRouteDtls_Click(object sender, EventArgs e)
        {
            DisplayRouteDetails();
        }

        private void buttonActivityDtls_Click_1(object sender, EventArgs e)
        {
            DisplayActivityDetails();
        }

        private void buttonResume_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Retry;
        }
    }
}
