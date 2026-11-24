// COPYRIGHT 2009 - 2026 by the Open Rails project.
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
using System.Linq;

namespace Menu
{
    /// <summary>
    /// Turns what was chosen in the <see cref="MainForm"/> into RunActivity's command line. The menu
    /// program starts RunActivity.exe with it; RunActivity itself uses it when it hosts the menu window
    /// (the headset's in-game menu) and carries on in the same process.
    /// </summary>
    public static class MenuLauncher
    {
        /// <summary>The arguments as RunActivity receives them in Main(string[]), one per entry and unquoted.</summary>
        public static List<string> GetRunActivityArguments(MainForm form)
        {
            var parameters = new List<string>();
            switch (form.SelectedAction)
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
                case MainForm.UserAction.MultiplayerClientResumeSave:
                    parameters.Add("-multiplayerclient");
                    break;
                case MainForm.UserAction.MultiplayerServerResumeSave:
                    parameters.Add("-multiplayerserver");
                    break;
            }
            switch (form.SelectedAction)
            {
                case MainForm.UserAction.SingleplayerNewGame:
                case MainForm.UserAction.MultiplayerClient:
                case MainForm.UserAction.MultiplayerServer:
                    if (form.SelectedActivity is ORTS.Menu.DefaultExploreActivity)
                    {
                        var exploreActivity = form.SelectedActivity as ORTS.Menu.DefaultExploreActivity;
                        parameters.Add("-explorer");
                        parameters.Add(exploreActivity.Path.FilePath);
                        parameters.Add(exploreActivity.Consist.FilePath);
                        parameters.Add(exploreActivity.StartTime);
                        parameters.Add(((int)exploreActivity.Season).ToString());
                        parameters.Add(((int)exploreActivity.Weather).ToString());
                    }
                    else if (form.SelectedActivity is ORTS.Menu.ExploreThroughActivity)
                    {
                        var exploreActivity = form.SelectedActivity as ORTS.Menu.ExploreThroughActivity;
                        parameters.Add("-exploreactivity");
                        parameters.Add(exploreActivity.Path.FilePath);
                        parameters.Add(exploreActivity.Consist.FilePath);
                        parameters.Add(exploreActivity.StartTime);
                        parameters.Add(((int)exploreActivity.Season).ToString());
                        parameters.Add(((int)exploreActivity.Weather).ToString());
                    }
                    else
                    {
                        parameters.Add("-activity");
                        parameters.Add(form.SelectedActivity.FilePath);
                    }
                    break;
                case MainForm.UserAction.SingleplayerResumeSave:
                case MainForm.UserAction.SingleplayerReplaySave:
                case MainForm.UserAction.SingleplayerReplaySaveFromSave:
                case MainForm.UserAction.MultiplayerClientResumeSave:
                case MainForm.UserAction.MultiplayerServerResumeSave:
                    parameters.Add(form.SelectedSaveFile);
                    break;
                case MainForm.UserAction.SinglePlayerTimetableGame:
                    parameters.Add("-timetable");
                    parameters.Add(form.SelectedTimetableSet.fileName);
                    parameters.Add(String.Format("{0}:{1}", form.SelectedTimetable, form.SelectedTimetableTrain));
                    parameters.Add(form.SelectedTimetableSet.Day.ToString());
                    parameters.Add(form.SelectedTimetableSet.Season.ToString());
                    parameters.Add(form.SelectedTimetableSet.Weather.ToString());
                    if (!String.IsNullOrEmpty(form.SelectedTimetableSet.WeatherFile))
                        parameters.Add(form.SelectedTimetableSet.WeatherFile);
                    break;
                case MainForm.UserAction.SinglePlayerResumeTimetableGame:
                    parameters.Add(form.SelectedSaveFile);
                    break;
            }
            return parameters;
        }

        /// <summary>The same arguments as one command line for Process.Start, quoting what needs it.</summary>
        public static string JoinArguments(IEnumerable<string> arguments)
        {
            return String.Join(" ", arguments.Select(a => a.Length == 0 || a.Contains(' ') || a.Contains('"') ? "\"" + a.Replace("\"", "\\\"") + "\"" : a));
        }
    }
}
