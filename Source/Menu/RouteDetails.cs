/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

//
//  This form dipslays details for the selected route or activity
//  Author: Laurie Heath
//

using System;
using System.Drawing;
using System.Windows.Forms;
using ORTS.Menu;

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

		public DetailsForm(Route route)
			: this()
        {
            Text = "Route Details";
			groupBoxDescription.Text = route.TRKFile.Tr_RouteFile.Name;
			textDescription.Text = route.TRKFile.Tr_RouteFile.Description.Replace("\n", "\r\n");
			groupBoxDescription.Height *= 2;
			groupBoxBriefing.Visible = false;
			groupEnvironment.Visible = false;
			this.Height -= groupEnvironment.Bottom - groupBoxDescription.Bottom;
        }

		public DetailsForm(Activity activity)
			: this()
		{
            Text = "Activity Details";
			groupBoxDescription.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Name;
			textDescription.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Description.Replace("\n", "\r\n");
			textBriefing.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Briefing.Replace("\n", "\r\n");
			textStartTime.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.StartTime.FormattedStartTime();
			textDuration.Text = activity.ACTFile.Tr_Activity.Tr_Activity_Header.Duration.FormattedDurationTime();
			textSeason.Text = Seasons[Math.Min(4, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Season)];
			textWeather.Text = Weathers[Math.Min(3, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Weather)];
			textDifficulty.Text = Difficulties[Math.Min(3, (int)activity.ACTFile.Tr_Activity.Tr_Activity_Header.Difficulty)];
        }

    }
}
