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
        public ExploreForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        List<string> Paths = new List<string>();
        List<string> Consists = new List<string>();

        public string SelectedPath { get { return pathListBox.SelectedIndex>=0 ? Paths[pathListBox.SelectedIndex] : null; } }
        public string SelectedConsist { get { return consistListBox.SelectedIndex >= 0 ? Consists[consistListBox.SelectedIndex] : null; } }
        public int SelectedSeason { get { return seasonListBox.SelectedIndex; } }
        public int SelectedWeather { get { return weatherListBox.SelectedIndex; } }
        public int SelectedStartHour { get { return (int)startHourNumeric.Value; } }

        public void LoadData(string folderPath, string routePath, string path, string consist, int season, int weather, int startHour)
        {
            string[] patfiles = Directory.GetFiles(routePath + @"\paths");
            foreach (string file in patfiles)
            {
                pathListBox.Items.Add(Path.GetFileName(file));
                Paths.Add(file);
                if (file == path)
                    pathListBox.SelectedIndex = Paths.Count - 1;
            }
            string[] confiles = Directory.GetFiles(folderPath + @"\trains\consists");
            foreach (string file in confiles)
            {
                consistListBox.Items.Add(Path.GetFileName(file));
                Consists.Add(file);
                if (file == consist)
                    consistListBox.SelectedIndex = Consists.Count - 1;
            }
            seasonListBox.SelectedIndex = season;
            weatherListBox.SelectedIndex = weather;
            startHourNumeric.Value = startHour;
        }
    }
}
