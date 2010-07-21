/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

//
//  This form dipslays details for the selected route or activity
//  Author: Laurie Heath
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using MSTS;

namespace ORTS
{
    public partial class DetailsForm : Form
    {
        private String[] Seasons = { "Spring", "Summer", "Autumn", "Winter", "Unknown" };
        private String[] Weathers = { "Clear", "Snow", "Rain", "Unknown" };
        private String[] Difficulties = {"Easy", "Medium", "Hard", "Unknown"};

        public DetailsForm()
        {
            InitializeComponent();
        }

        public bool RouteDetails(String strRoutePath)
        {
            this.Text = "Route Details";

            try
            {
                string[] RouteTRK = Directory.GetFiles(strRoutePath, "*.trk", SearchOption.TopDirectoryOnly);
                if (RouteTRK.Length == 0)
                {
                    Console.Error.WriteLine("No .trk file found for route.");
                    return false;       // .trk file not found.
                }
                TRKFile trkFile = new TRKFile(RouteTRK[0]);
                txtName.Text = trkFile.Tr_RouteFile.Name;
                string Description = trkFile.Tr_RouteFile.Description;
                txtDescription.Text = Description.Replace("\n", "\r\n");
                grpBriefing.Visible = false;
                grpEnvironment.Visible = false;
                this.Height -= grpEnvironment.Bottom - grpDescription.Bottom;
            }
            catch
            {
                Console.Error.WriteLine("Failed to read: .trk file");
                return false;
            }
            return true;
        }

        public bool ActivityDetails(String strActiviyPath)
        {
            this.Text = "Activity Details";
            try
            {
                int i;
                ACTFile actFile=new ACTFile(strActiviyPath,true);
                txtName.Text=actFile.Tr_Activity.Tr_Activity_Header.Name;
                string Description = actFile.Tr_Activity.Tr_Activity_Header.Description;
                txtDescription.Text=Description.Replace("\n","\r\n");
                string Briefing = actFile.Tr_Activity.Tr_Activity_Header.Briefing;
                txtBriefing.Text=Briefing.Replace("\n","\r\n");
                StartTime startTime = actFile.Tr_Activity.Tr_Activity_Header.StartTime;
                txtStartTime.Text = actFile.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime();
                txtDuration.Text = actFile.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTime();
                i=(int)actFile.Tr_Activity.Tr_Activity_Header.Season;
                if(i>3) i=4;
                txtSeason.Text = Seasons[i];
                i=(int)actFile.Tr_Activity.Tr_Activity_Header.Weather;
                if (i > 2) i = 3;
                txtWeather.Text = Weathers[i];
                if (i > 2) i = 3;
                i = (int)actFile.Tr_Activity.Tr_Activity_Header.Difficulty;
                txtDifficulty.Text = Difficulties[i];
            }
            catch
            {
                Console.Error.WriteLine("Failed to read: " + strActiviyPath);
                return false;
            }
            return true;
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
