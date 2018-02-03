// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using ORTS.TrackViewer.Drawing;
using ORTS.Menu;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Interaction logic for AutoFixAllPaths.xaml
    /// </summary>
    public partial class AutoFixAllPaths : Window
    {
        RouteData routeData;
        DrawTrackDB drawTrackDB;

        Dictionary<string, List<string>> pathsThatAre = new Dictionary<string, List<string>>
            {
                ["UnmodifiedFine"] = new List<string>(),
                ["UnmodifiedBroken"] = new List<string>(),
                ["ModifiedFine"] = new List<string>(),
                ["ModifiedBroken"] = new List<string>(),
            };
        List<PathEditor> modifiedPaths;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="routeData">The route information that contains track data base and track section data</param>
        /// <param name="drawTrackDB">The drawn tracks to know about where the mouse is</param>
        public AutoFixAllPaths(RouteData routeData, DrawTrackDB drawTrackDB)
        {
            this.routeData = routeData;
            this.drawTrackDB = drawTrackDB;
        }

        /// <summary>
        /// Load all the paths, fix them, and ask the user what to do whit the fixed paths
        /// </summary>
        /// <param name="Paths">The list of paths that are availabel and that need to be checked and possibly fixed</param>
        /// <param name="callback">Callback that will be called showing the current processing that is being done</param>
        public void FixallAndShowResults(Collection<Path> Paths, Action<string> callback )
        {
            _Fixall(Paths, callback);
            _ShowResults();
        }

        private void _Fixall(Collection<Path> Paths, Action<string> callback)
        {
            modifiedPaths = new List<PathEditor>();

            // Loop through all available paths and fix each of them
            foreach (ORTS.Menu.Path path in Paths)
            {
                callback(TrackViewer.catalog.GetString("Processing .pat file ") + path.FilePath);
                string pathName = ORTS.TrackViewer.UserInterface.MenuControl.MakePathMenyEntryName(path);
                PathEditor pathFixer = new PathEditor(this.routeData, this.drawTrackDB, path);
                bool fixSucceeded = pathFixer.AutoFixAllBrokenNodes();
                if (pathFixer.HasModifiedPath)
                {
                    if (pathFixer.HasBrokenPath)
                    {
                        pathsThatAre["ModifiedBroken"].Add(pathName);
                    }
                    else
                    {
                        pathsThatAre["ModifiedFine"].Add(pathName);
                    }
                    modifiedPaths.Add(pathFixer);
                }
                else
                {
                    if (pathFixer.HasBrokenPath)
                    {
                        pathsThatAre["UnmodifiedBroken"].Add(pathName);
                    }
                    else
                    {
                        pathsThatAre["UnmodifiedFine"].Add(pathName);
                    }
                }
            }
        }

        private void _ShowResults()
        {
            InitializeComponent();

            if (pathsThatAre["ModifiedBroken"].Count > 0)
            {
                ModifiedBrokenLabel.Visibility = Visibility.Visible;
                ModifiedBrokenList.Visibility = Visibility.Visible;
                foreach (string pathName in pathsThatAre["ModifiedBroken"])
                {
                    var item = new ListViewItem { Content = pathName };
                    ModifiedBrokenList.Items.Add(item);
                }
                SaveDirect.IsEnabled = true;
                SaveAndConfirm.IsEnabled = true;
            }

            if (pathsThatAre["ModifiedFine"].Count > 0)
            {
                ModifiedFineLabel.Visibility = Visibility.Visible;
                ModifiedFineList.Visibility = Visibility.Visible;
                foreach (string pathName in pathsThatAre["ModifiedFine"])
                {
                    var item = new ListViewItem { Content = pathName };
                    ModifiedFineList.Items.Add(item);
                }
                SaveDirect.IsEnabled = true;
                SaveAndConfirm.IsEnabled = true;
            }

            if (pathsThatAre["UnmodifiedBroken"].Count > 0)
            {
                UnmodifiedBrokenLabel.Visibility = Visibility.Visible;
                UnmodifiedBrokenList.Visibility = Visibility.Visible;
                foreach (string pathName in pathsThatAre["UnmodifiedBroken"])
                {
                    var item = new ListViewItem { Content = pathName };
                    UnmodifiedBrokenList.Items.Add(item);
                }
            }

            if (pathsThatAre["UnmodifiedFine"].Count > 0)
            {
                UnmodifiedFineLabel.Visibility = Visibility.Visible;
                UnmodifiedFineList.Visibility = Visibility.Visible;
                foreach (string pathName in pathsThatAre["UnmodifiedFine"])
                {
                    var item = new ListViewItem { Content = pathName };
                    UnmodifiedFineList.Items.Add(item);
                }
            }

            this.ShowDialog();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveDirect_Click(object sender, RoutedEventArgs e)
        {
            foreach (PathEditor modifiedPath in modifiedPaths)
            {
                SavePatFile.WritePatFileDirect(modifiedPath.CurrentTrainPath);
            }
            this.Close();
        }

        private void SaveAndConfirm_Click(object sender, RoutedEventArgs e)
        {
            foreach (PathEditor modifiedPath in modifiedPaths)
            {
                SavePatFile.WritePatFile(modifiedPath.CurrentTrainPath);
            }
            this.Close();
        }

        private void SaveOverview_Click(object sender, RoutedEventArgs e)
        {

            string fullFilePath = GetFileName();
            if (String.IsNullOrEmpty(fullFilePath)) return;

            System.IO.StreamWriter file = new System.IO.StreamWriter(fullFilePath, false, System.Text.Encoding.Unicode);
            string[] statusses = { "ModifiedBroken", "ModifiedFine", "UnmodifiedBroken", "UnmodifiedFine"};
            foreach (string status in statusses)
            {
                file.WriteLine("");
                file.WriteLine(status + ":");
                foreach (string pathName in pathsThatAre[status])
                {
                    file.WriteLine(pathName);
                }
            }
            file.Close();
        }

        static string GetFileName()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                OverwritePrompt = true,
                //dlg.InitialDirectory = Path.GetDirectoryName(trainpath.FilePath);
                FileName = "modifiedpaths.txt",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                return dlg.FileName;
            }
            return String.Empty;
        }
    }
}
