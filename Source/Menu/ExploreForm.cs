// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MSTS;
using ORTS.Menu;

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

		public class Path
		{
			public readonly string FileName;
			public readonly string Name;
			public readonly string Start;
			public readonly string End;

			public Path(string fileName, string name, string start, string end)
			{
				FileName = fileName;
				Name = name;
				Start = start;
				End = end;
			}

			public override string ToString()
			{
				return Start + " - " + End;
			}
		}

		public class Consist
		{
			public readonly string FileName;
			public readonly string Name;

			public Consist(string fileName, string name)
			{
				FileName = fileName;
				Name = name;
			}

			public override string ToString()
			{
				return Name;
			}
		}

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
				return new ExploreActivity(listPaths.SelectedIndex >= 0 ? Paths[listPaths.SelectedIndex].FileName : "", listConsists.SelectedIndex >= 0 ? Consists[listConsists.SelectedIndex].FileName : "", listSeason.SelectedIndex, listWeather.SelectedIndex, (int)numericHour.Value, (int)numericMinute.Value);
			}
		}

		void LoadPaths()
		{
			if (PathLoader != null)
				PathLoader.Cancel();

			listPaths.Items.Clear();
			var route = Route;
			var exploreActivity = ExploreActivity;
			PathLoader = new Task<List<Path>>(this, () =>
			{
				var paths = new List<Path>();
				var directory = System.IO.Path.Combine(route.Path, "PATHS");
				if (Directory.Exists(directory))
				{
					foreach (var pathFile in Directory.GetFiles(directory, "*.pat"))
					{
						try
						{
							var patFile = new PATFile(pathFile);
							if (patFile.IsPlayerPath)
								paths.Add(new Path(pathFile, patFile.Name, patFile.Start, patFile.End));
						}
						catch { }
					}
				}
				return paths.OrderBy(p => p.ToString()).ToList();
			}, (paths) =>
			{
				Paths = paths;
				foreach (var path in Paths)
					listPaths.Items.Add(path.ToString());
				var index = Paths.FindIndex(p => p.FileName == exploreActivity.Path);
				if (index >= 0)
					listPaths.SelectedIndex = index;
				else if (Paths.Count > 0)
					listPaths.SelectedIndex = 0;
			});
		}

		void LoadConsists()
		{
			if (ConsistLoader != null)
				ConsistLoader.Cancel();

			listConsists.Items.Clear();
			var folder = Folder;
			var exploreActivity = ExploreActivity;
			ConsistLoader = new Task<List<Consist>>(this, () =>
			{
				var consists = new List<Consist>();
				var directory = System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS");
				if (Directory.Exists(directory))
				{
					foreach (var consistFile in Directory.GetFiles(directory, "*.con"))
					{
						try
						{
							var conFile = new CONFile(consistFile);
							if (conFile.Train.TrainCfg.Name != "Loose consist.")
								consists.Add(new Consist(consistFile, conFile.Train.TrainCfg.Name));
						}
						catch { }
					}
				}
				return consists.OrderBy(p => p.ToString()).ToList();
			}, (consists) =>
			{
				Consists = consists;
				foreach (var consist in Consists)
					listConsists.Items.Add(consist.ToString());
				var index = Consists.FindIndex(c => c.FileName == exploreActivity.Consist);
				if (index >= 0)
					listConsists.SelectedIndex = index;
				else if (Consists.Count > 0)
					listConsists.SelectedIndex = 0;
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
