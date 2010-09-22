using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ORTS
{
    public partial class ExploreForm : Form
    {
		List<string> Paths = new List<string>();
		List<string> Consists = new List<string>();

		public ExploreForm(MainForm.Folder folder, MainForm.Route route, MainForm.ExploreActivity exploreActivity)
        {
            InitializeComponent();

			// Windows 2000 and XP should use 8.25pt Tahoma, while Windows
			// Vista and later should use 9pt "Segoe UI". We'll use the
			// Message Box font to allow for user-customizations, though.
			Font = SystemFonts.MessageBoxFont;

			string[] patfiles = Directory.GetFiles(route.Path + @"\paths");
			foreach (string file in patfiles)
			{
				pathListBox.Items.Add(Path.GetFileName(file));
				Paths.Add(file);
				if (file == exploreActivity.Path)
					pathListBox.SelectedIndex = Paths.Count - 1;
				else if (pathListBox.Items.Count == 1)
					pathListBox.SelectedIndex = 0;
			}
			string[] confiles = Directory.GetFiles(folder.Path + @"\trains\consists");
			foreach (string file in confiles)
			{
				consistListBox.Items.Add(Path.GetFileName(file));
				Consists.Add(file);
				if (file == exploreActivity.Consist)
					consistListBox.SelectedIndex = Consists.Count - 1;
				else if (consistListBox.Items.Count == 1)
					consistListBox.SelectedIndex = 0;
			}
			seasonListBox.SelectedIndex = exploreActivity.Season;
			weatherListBox.SelectedIndex = exploreActivity.Weather;
			startHourNumeric.Value = exploreActivity.StartHour;
		}

		public MainForm.ExploreActivity ExploreActivity
		{
			get
			{
				return new MainForm.ExploreActivity(Paths[pathListBox.SelectedIndex], Consists[consistListBox.SelectedIndex], seasonListBox.SelectedIndex, weatherListBox.SelectedIndex, (int)startHourNumeric.Value);
			}
		}
    }
}
