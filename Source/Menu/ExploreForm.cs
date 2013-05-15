// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MSTS;
using ORTS.Menu;
using Path = ORTS.Menu.Path;

namespace ORTS
{
	public partial class ExploreForm : Form
	{
		readonly Folder Folder;
		readonly Route Route;
		readonly ExploreActivity ExploreActivity;

        List<Path> Paths = new List<Path>();
		List<Consist> Consists = new List<Consist>();
        Task<List<Path>> PathLoader;
		Task<List<Consist>> ConsistLoader;

		public ExploreForm(Folder folder, Route route, ExploreActivity exploreActivity)
		{
			InitializeComponent();

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;

			Folder = folder;
			Route = route;
			ExploreActivity = exploreActivity;

			LoadPaths();

			LoadConsists();

			listSeason.SelectedIndex = exploreActivity.Season;
			listWeather.SelectedIndex = exploreActivity.Weather;
			numericHour.Value = exploreActivity.StartHour;
			numericMinute.Value = exploreActivity.StartMinute;
		}

		public ExploreActivity NewExploreActivity
		{
			get
			{
                return new ExploreActivity(listPaths.SelectedIndex >= 0 ? Paths[listPaths.SelectedIndex] : null, listConsists.SelectedIndex >= 0 ? Consists[listConsists.SelectedIndex] : null, listSeason.SelectedIndex, listWeather.SelectedIndex, (int)numericHour.Value, (int)numericMinute.Value);
			}
		}

		void LoadPaths()
		{
			if (PathLoader != null)
				PathLoader.Cancel();

			listPaths.Items.Clear();
            buttonOk.Enabled = false;
			var route = Route;
			var exploreActivity = ExploreActivity;
            PathLoader = new Task<List<Path>>(this, () => Path.GetPaths(route).OrderBy(p => p.ToString()).ToList(), (paths) =>
			{
				Paths = paths;
				foreach (var path in Paths)
					listPaths.Items.Add(path);
                var selectionIndex = exploreActivity.Path != null ? Paths.FindIndex(p => p.FilePath == exploreActivity.Path.FilePath) : -1;
                if (selectionIndex >= 0)
                    listPaths.SelectedIndex = selectionIndex;
                else if (Paths.Count > 0)
                    listPaths.SelectedIndex = 0;
                else
                    listPaths.ClearSelected();
                buttonOk.Enabled = listPaths.Items.Count > 0 && listConsists.Items.Count > 0;
            });
		}

		void LoadConsists()
		{
			if (ConsistLoader != null)
				ConsistLoader.Cancel();

			listConsists.Items.Clear();
            buttonOk.Enabled = false;
            var folder = Folder;
			var exploreActivity = ExploreActivity;
			ConsistLoader = new Task<List<Consist>>(this, () => Consist.GetConsists(folder).OrderBy(c => c.ToString()).ToList(), (consists) =>
			{
				Consists = consists;
				foreach (var consist in Consists)
					listConsists.Items.Add(consist.ToString());
                var selectionIndex = exploreActivity.Consist != null ? Consists.FindIndex(c => c.FilePath == exploreActivity.Consist.FilePath) : -1;
                if (selectionIndex >= 0)
                    listConsists.SelectedIndex = selectionIndex;
                else if (Consists.Count > 0)
                    listConsists.SelectedIndex = 0;
                else
                    listConsists.ClearSelected();
                buttonOk.Enabled = listPaths.Items.Count > 0 && listConsists.Items.Count > 0;
            });
		}

		void ExploreForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (PathLoader != null)
				PathLoader.Cancel();
			if (ConsistLoader != null)
				ConsistLoader.Cancel();
		}
	}
}
