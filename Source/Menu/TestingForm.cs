// COPYRIGHT 2012, 2013 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Menu;
using ORTS.Settings;
using Path = System.IO.Path;

namespace ORTS
{
    public partial class TestingForm : Form
    {
        public class TestActivity
        {
            public string DefaultSort { get; set; }
            public string Route { get; set; }
            public string Activity { get; set; }
            public string ActivityFilePath { get; set; }
            public bool ToTest { get; set; }
            public bool Tested { get; set; }
            public bool Passed { get; set; }
            public string Errors { get; set; }
            public string Load { get; set; }
            public string FPS { get; set; }

            public TestActivity(Folder folder, Route route, Activity activity)
            {
                DefaultSort = folder.Name + "/" + route.Name + "/" + activity.Name;
                Route = route.Name;
                Activity = activity.Name;
                ActivityFilePath = activity.FilePath;
            }
        }

        Task<SortableBindingList<TestActivity>> TestActivityLoader;

        Task<int> TestActivitiesRunner;
        bool ClearedLogs;

        readonly MainForm MainForm;
        readonly UserSettings Settings;
		readonly string SummaryFilePath = Path.Combine(UserSettings.UserDataFolder, "TestingSummary.csv");
		readonly string LogFilePath = Path.Combine(UserSettings.UserDataFolder, "TestingLog.txt");

        public TestingForm(MainForm mainForm, UserSettings settings)
        {
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            GettextResourceManager catalog = new GettextResourceManager("Menu");
            Localizer.Localize(this, catalog);

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            MainForm = mainForm;
            Settings = settings;

            UpdateButtons();

            LoadActivities();
        }

        void TestingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TestActivityLoader != null)
                TestActivityLoader.Cancel();
            if (TestActivitiesRunner != null)
                TestActivitiesRunner.Cancel();
        }

        void UpdateButtons()
        {
            buttonTestAll.Enabled = TestActivitiesRunner == null && gridTestActivities.RowCount > 0;
            buttonTest.Enabled = TestActivitiesRunner == null && gridTestActivities.SelectedRows.Count > 0;
            buttonCancel.Enabled = TestActivitiesRunner != null && !TestActivitiesRunner.Cancelled;
            buttonSummary.Enabled = TestActivitiesRunner == null && File.Exists(SummaryFilePath);
            buttonDetails.Enabled = TestActivitiesRunner == null && File.Exists(LogFilePath);
        }

        void LoadActivities()
        {
            if (TestActivityLoader != null)
                TestActivityLoader.Cancel();

            TestActivityLoader = new Task<SortableBindingList<TestActivity>>(this, () =>
            {
                return new SortableBindingList<TestActivity>((from f in Folder.GetFolders(Settings)
                                                              from r in Route.GetRoutes(f)
                                                              from a in Activity.GetActivities(f, r)
                                                              where !(a is ORTS.Menu.ExploreActivity)
                                                              orderby a.Name
                                                              select new TestActivity(f, r, a)).ToList());
            }, (testActivities) =>
            {
                testBindingSource.DataSource = testActivities;
                testBindingSource.Sort = "DefaultSort";
                UpdateButtons();
            });
        }

        void buttonTestAll_Click(object sender, EventArgs e)
        {
            TestMarkedActivities(from DataGridViewRow r in gridTestActivities.Rows
                                 select r);
        }

        void buttonTest_Click(object sender, EventArgs e)
        {
            TestMarkedActivities(from DataGridViewRow r in gridTestActivities.Rows
                                 where r.Selected
                                 select r);
        }

        void buttonCancel_Click(object sender, EventArgs e)
        {
            TestActivitiesRunner.Cancel();
            UpdateButtons();
        }

        void buttonNoSort_Click(object sender, EventArgs e)
        {
            gridTestActivities.Sort(defaultSortDataGridViewTextBoxColumn, ListSortDirection.Ascending);
        }

        void buttonSummary_Click(object sender, EventArgs e)
        {
            Process.Start(SummaryFilePath);
        }

        void buttonDetails_Click(object sender, EventArgs e)
        {
            Process.Start(LogFilePath);
        }

        void TestMarkedActivities(IEnumerable<DataGridViewRow> rows)
        {
            if (TestActivitiesRunner != null)
                TestActivitiesRunner.Cancel();

            // Force the enumeration to be evaluated so that when we run the code in the background it doesn't matter if the grid changes.
            var items = from r in rows
                        select new { Index = r.Index, Activity = (TestActivity)r.DataBoundItem };
            var overrideSettings = checkBoxOverride.Checked;

            Task<int> runner = null;
            runner = TestActivitiesRunner = new Task<int>(this, () =>
            {
                var parameters = String.Join(" ", new[] {
                    "/Test",
                    "/Logging",
                    "/LoggingFilename=\"" + Path.GetFileName(LogFilePath) + "\"",
                    "/LoggingPath=\"" + Path.GetDirectoryName(LogFilePath) + "\"",
                    "/Profiling",
                    "/ProfilingTime=10",
                    "/ShowErrorDialogs=False",
                });
                if (overrideSettings)
                    parameters += " /Skip-User-Settings";

                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = MainForm.RunActivityProgram;
                processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
                processStartInfo.WorkingDirectory = Application.StartupPath;

                if (!ClearedLogs)
                {
                    using (var writer = File.CreateText(SummaryFilePath))
                        writer.WriteLine("Route,Activity,Passed,Errors,Warnings,Infos,Loading,FPS");
                    using (var writer = File.CreateText(LogFilePath))
                        writer.Flush();
                    ClearedLogs = true;
                }

                var summaryFilePosition = 0L;
                using (var reader = File.OpenText(SummaryFilePath))
                    summaryFilePosition = reader.BaseStream.Length;

                foreach (var item in items)
                {
                    processStartInfo.Arguments = String.Format("{0} \"{1}\"", parameters, item.Activity.ActivityFilePath);
                    var process = Process.Start(processStartInfo);
                    process.WaitForExit();

                    item.Activity.Tested = true;
                    item.Activity.Passed = process.ExitCode == 0;
                    using (var reader = File.OpenText(SummaryFilePath))
                    {
                        reader.BaseStream.Seek(summaryFilePosition, SeekOrigin.Begin);
                        var line = reader.ReadLine();
                        if (!String.IsNullOrEmpty(line) && reader.EndOfStream)
                        {
                            var csv = line.Split(',');
                            item.Activity.Errors = String.Format("{0}/{1}/{2}", int.Parse(csv[3]), int.Parse(csv[4]), int.Parse(csv[5]));
                            item.Activity.Load = String.Format("{0,6:F1}s", float.Parse(csv[6]));
                            item.Activity.FPS = String.Format("{0,6:F1}", float.Parse(csv[7]));
                        }
                        else
                        {
                            reader.ReadToEnd();
                            item.Activity.Passed = false;
                        }
                        summaryFilePosition = reader.BaseStream.Position;
                    }
                    if (runner.Cancelled)
                        break;

                    Invoke((Action)(() => ShowGridRow(gridTestActivities, item.Index)));
                }
                return 0;
            }, () =>
            {
                TestActivitiesRunner = null;
                UpdateButtons();
            });
            UpdateButtons();
        }

        static void ShowGridRow(DataGridView grid, int rowIndex)
        {
            var displayedRowCount = grid.DisplayedRowCount(false);
            if (grid.FirstDisplayedScrollingRowIndex > rowIndex)
                grid.FirstDisplayedScrollingRowIndex = rowIndex;
            else if (grid.FirstDisplayedScrollingRowIndex < rowIndex - displayedRowCount + 1)
                grid.FirstDisplayedScrollingRowIndex = rowIndex - displayedRowCount + 1;
            grid.InvalidateRow(rowIndex);
        }
    }
}
