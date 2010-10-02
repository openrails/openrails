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
		string[] Seasons = { "Spring", "Summer", "Autumn", "Winter", "Unknown" };
		string[] Weathers = { "Clear", "Snow", "Rain", "Unknown" };
		string[] Difficulties = { "Easy", "Medium", "Hard", "Unknown" };

        DetailsForm()
        {
            InitializeComponent();

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;
		}

		public DetailsForm(MainForm.Route route)
			: this()
        {
            Text = "Route Details";
			grpDescription.Text = route.TRKFile.Tr_RouteFile.Name;
			txtDescription.Text = route.TRKFile.Tr_RouteFile.Description.Replace("\n", "\r\n");
			grpBriefing.Visible = false;
			grpEnvironment.Visible = false;
			this.Height -= grpEnvironment.Bottom - grpDescription.Bottom;
        }

		public DetailsForm(MainForm.Activity activity)
			: this()
		{
            Text = "Activity Details";
			grpDescription.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Name;
			txtDescription.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Description.Replace("\n", "\r\n");
			txtBriefing.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Briefing.Replace("\n", "\r\n");
			txtStartTime.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime();
			txtDuration.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTime();
			txtSeason.Text = Seasons[Math.Min(4, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Season)];
			txtWeather.Text = Weathers[Math.Min(3, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Weather)];
			txtDifficulty.Text = Difficulties[Math.Min(3, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Difficulty)];
        }

    }
}
