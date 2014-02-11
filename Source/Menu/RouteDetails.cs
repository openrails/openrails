// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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

//
//  This form dipslays details for the selected route or activity
//  Author: Laurie Heath
//

using System;
using System.Drawing;
using System.Windows.Forms;
using ORTS.Menu;
using GNU.Gettext;
using GNU.Gettext.WinForms;

namespace ORTS
{
    public partial class DetailsForm : Form
    {
		string[] Seasons = { "Spring", "Summer", "Autumn", "Winter", "Unknown" };
		string[] Weathers = { "Clear", "Snow", "Rain", "Unknown" };
		string[] Difficulties = { "Easy", "Medium", "Hard", "Unknown" };

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        DetailsForm()
        {
            InitializeComponent();

            Localizer.Localize(this, catalog);

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;
		}

		public DetailsForm(Route route)
			: this()
        {
            Text = catalog.GetString("Route Details");
			groupBoxDescription.Text = route.Name;
			textDescription.Text = route.Description.Replace("\n", "\r\n");
			groupBoxDescription.Height *= 2;
			groupBoxBriefing.Visible = false;
			groupEnvironment.Visible = false;
			this.Height -= groupEnvironment.Bottom - groupBoxDescription.Bottom;
        }

		public DetailsForm(Activity activity)
			: this()
		{
            Text = catalog.GetString("Activity Details");
			groupBoxDescription.Text = activity.Name;
			textDescription.Text = activity.Description.Replace("\n", "\r\n");
			textBriefing.Text = activity.Briefing.Replace("\n", "\r\n");
			textStartTime.Text = activity.StartTime.FormattedStartTime();
			textDuration.Text = activity.Duration.FormattedDurationTime();
			textSeason.Text = Seasons[Math.Min(4, (int)activity.Season)];
			textWeather.Text = Weathers[Math.Min(3, (int)activity.Weather)];
			textDifficulty.Text = Difficulties[Math.Min(3, (int)activity.Difficulty)];
        }

    }
}
