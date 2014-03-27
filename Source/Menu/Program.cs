// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Updater;

namespace ORTS
{
    static class Program
    {
        [STAThread]  // requred for use of the DirectoryBrowserDialog in the main form.
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            if (Debugger.IsAttached)
            {
                MainForm();
            }
            else
            {
                try
                {
                    MainForm();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString(), Application.ProductName + " " + VersionInfo.VersionOrBuild);
                }
            }
        }

        static void MainForm()
        {
            var updateManager = new UpdateManager(Path.GetDirectoryName(Application.ExecutablePath));

            // We must do this before localisation gets its grubby mitts on the satellite assemblies.
            if (updateManager.Apply())
            {
                Process.Start(System.IO.Path.Combine(updateManager.BasePath, "OpenRails.exe"));
                return;
            }

            using (var MainForm = new MainForm(updateManager))
            {
                while (MainForm.ShowDialog() == DialogResult.OK)
                {
                    var parameters = new List<string>();
                    switch (MainForm.SelectedAction)
                    {
                        case MainForm.UserAction.SingleplayerNewGame:
                            parameters.Add("-start");
                            break;
                        case MainForm.UserAction.SingleplayerResumeSave:
                            parameters.Add("-resume");
                            break;
                        case MainForm.UserAction.SingleplayerReplaySave:
                            parameters.Add("-replay");
                            break;
                        case MainForm.UserAction.SingleplayerReplaySaveFromSave:
                            parameters.Add("-replay_from_save");
                            break;
                        case MainForm.UserAction.MultiplayerClient:
                            parameters.Add("-multiplayerclient");
                            break;
                        case MainForm.UserAction.MultiplayerServer:
                            parameters.Add("-multiplayerserver");
                            break;
                        case MainForm.UserAction.SinglePlayerTimetableGame:
                            parameters.Add("-start");
                            break;
                        case MainForm.UserAction.SinglePlayerResumeTimetableGame:
                            parameters.Add("-resume");
                            break;
                    }
                    switch (MainForm.SelectedAction)
                    {
                        case MainForm.UserAction.SingleplayerNewGame:
                        case MainForm.UserAction.MultiplayerClient:
                        case MainForm.UserAction.MultiplayerServer:
                            if (MainForm.SelectedActivity is ORTS.Menu.ExploreActivity)
                            {
                                var exploreActivity = MainForm.SelectedActivity as ORTS.Menu.ExploreActivity;
                                parameters.Add(String.Format("-explorer \"{0}\" \"{1}\" {2} {3} {4}",
                                    exploreActivity.Path.FilePath,
                                    exploreActivity.Consist.FilePath,
                                    exploreActivity.StartTime,
                                    (int)exploreActivity.Season,
                                    (int)exploreActivity.Weather));
                            }
                            else
                            {
                                parameters.Add(String.Format("-activity \"{0}\"", MainForm.SelectedActivity.FilePath));
                            }
                            break;
                        case MainForm.UserAction.SingleplayerResumeSave:
                        case MainForm.UserAction.SingleplayerReplaySave:
                        case MainForm.UserAction.SingleplayerReplaySaveFromSave:
                            parameters.Add("\"" + MainForm.SelectedSaveFile + "\"");
                            break;
                        case MainForm.UserAction.SinglePlayerTimetableGame:
                            parameters.Add(String.Format("-timetable \"{0}\" \"{1}:{2}\" \"{3}:{4}\" {5} {6} {7} {8} {9}",
                                MainForm.SelectedTimetable.fileName,
                                MainForm.SelectedPlayerTimetable,
                                MainForm.SelectedTimetableTrain,
                                MainForm.SelectedTimetable.AITimeHrs.ToString("00"),
                                MainForm.SelectedTimetable.AITimeMins.ToString("00"),
                                MainForm.SelectedTimetable.AITimeRelative ? String.Copy("R") : String.Copy("A"),
                                MainForm.SelectedTimetable.AIInPlayerDirection.ToString(),
                                MainForm.SelectedTimetable.Day,
                                MainForm.SelectedTimetable.Season,
                                MainForm.SelectedTimetable.Weather));
                            break;
                        case MainForm.UserAction.SinglePlayerResumeTimetableGame:
                            parameters.Add("\"" + MainForm.SelectedSaveFile + "\"");
                            break;
                    }

                    var processStartInfo = new System.Diagnostics.ProcessStartInfo();
                    processStartInfo.FileName = MainForm.RunActivityProgram;
                    processStartInfo.Arguments = String.Join(" ", parameters.ToArray());
                    processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    processStartInfo.WorkingDirectory = Application.StartupPath;

                    var process = Process.Start(processStartInfo);
                    process.WaitForExit();
                }
            }
        }
    }
}
