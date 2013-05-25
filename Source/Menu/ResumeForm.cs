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

/*
This form adds the ability to save the state of the simulator (a Save) multiple times and replace the previous 
single save to the file SAVE.BIN.
 = false
Saves are made to the folder Program.UserDataFolder (e.g.
    C:\Users\Chris\AppData\Roaming\Open Rails\ 
and take the form  <activity file name> <date> <time>.save. E.g.
    yard_two 2012-03-20 22.07.36.save

As Saves for all routes are saved in the same folder and activity file names might be common, the date and time 
elements ensure that the Save file names are unique.

If the player is not running an activity but exploring a route, the filename takes the form  
<route folder name> <date> <time>.save. E.g.
    USA2 2012-03-20 22.07.36.save

The RunActivity program takes switches; one of these is -resume
The -resume switch can now take an ActivitySave file name as a parameter. E.g.
    RunActivity.exe -resume "yard_two 2012-03-20 22.07.36"
or
    RunActivity.exe -resume "yard_two 2012-03-20 22.07.36.save"

If no parameter is provided, then RunActivity uses the most recent ActivitySave.

When the RunActivity program saves a Save, it adds its own program and build values. When resuming from an 
Save, it checks that the program and build values from the file match its own and rejects an ActivitySave that 
it didn't create.

The intention is to increase reliability by preventing crashes. A newer version of RunActivity might make use of 
additional values in the Save file which will not be present if saved by a previous version. Techniques 
to maintain compatibility are possible but too onerous for a voluntary team.

Some problems remain (see <CJ comment> in the source code):
1. A screen-capture image is saved along with the Save. The intention is that this image should be a thumbnail
   but I can't find how to code this successfully. In the meantime, the screen-capture image that is saved is full-size 
   but displayed as a thumbnail.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MSTS;
using ORTS.Common;
using ORTS.Menu;
using Path = System.IO.Path;

namespace ORTS
{
    public partial class ResumeForm : Form
    {
        const string InvalidTextString = "To prevent crashes and unexpected behavior, new versions of Open Rails invalidate old saved games. {0} of {1} saves are no longer valid.";

        readonly UserSettings Settings;
        readonly Route Route;
        readonly Activity Activity;
        readonly MainForm ParentForm;

        List<Save> Saves = new List<Save>();
        Task<List<Save>> SaveLoader;

        public class Save
        {
            public string File { get; private set; }
            public string PathName { get; private set; }
            public string RouteName { get; private set; }
            public TimeSpan GameTime { get; private set; }
            public DateTime RealTime { get; private set; }
            public string CurrentTile { get; private set; }
            public string Distance { get; private set; }
            public bool Valid { get; private set; }

            public Save(string fileName, string currentBuild)
            {
                File = fileName;
                System.Threading.Thread.Sleep(10);
                using (BinaryReader inf = new BinaryReader(new FileStream(File, FileMode.Open, FileAccess.Read)))
                {
                    try
                    {
                        // Read in validation data.
                        var version = inf.ReadString().Replace("\0", "");
                        var build = inf.ReadString().Replace("\0", "");

                        // Read in route/activity/path/player data.
                        var routeName = inf.ReadString(); // Route name
                        var pathName = inf.ReadString(); // Path name
                        var gameTime = new DateTime().AddSeconds(inf.ReadInt32()).TimeOfDay; // Game time
                        var realTime = DateTime.FromBinary(inf.ReadInt64()); // Real time
                        var currentTileX = inf.ReadSingle(); // Player TileX
                        var currentTileZ = inf.ReadSingle(); // Player TileZ
                        var currentTile = String.Format("{0:F1}, {1:F1}", currentTileX, currentTileZ);
                        var initialTileX = inf.ReadSingle(); // Initial TileX
                        var initialTileZ = inf.ReadSingle(); // Initial TileZ
                        if (currentTileX < short.MinValue || currentTileX > short.MaxValue || currentTileZ < short.MinValue || currentTileZ > short.MaxValue) throw new InvalidDataException();
                        if (initialTileX < short.MinValue || initialTileX > short.MaxValue || initialTileZ < short.MinValue || initialTileZ > short.MaxValue) throw new InvalidDataException();

                        // DistanceFromInitial using Pythagoras theorem.
                        var distance = String.Format("{0:F1}", Math.Sqrt(Math.Pow(currentTileX - initialTileX, 2) + Math.Pow(currentTileZ - initialTileZ, 2)) * 2048);

                        RouteName = routeName;
                        PathName = pathName;
                        GameTime = gameTime;
                        RealTime = realTime;
                        CurrentTile = currentTile;
                        Distance = distance;
                        Valid = currentBuild == null || build.EndsWith(currentBuild) 
                            || Debugger.IsAttached; // to support testing
                    }
                    catch { }
                }
            }
        }

        public string SelectedSaveFile { get; set; }
        public MainForm.UserAction SelectedAction { get; set; }

        public ResumeForm(UserSettings settings, Route route, Activity activity, MainForm parentForm)
        {
            ParentForm = parentForm;
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Settings = settings;
            Route = route;
            Activity = activity;
            Text = String.Format("{0} - {1} - {2}", Text, route.Name, activity.FilePath != null ? activity.Name : "Explore Route");
            checkBoxReplayPauseBeforeEnd.Checked = Settings.ReplayPauseBeforeEnd;
            numericReplayPauseBeforeEnd.Value = Settings.ReplayPauseBeforeEndS;

            gridSaves_SelectionChanged(null, null);
            pathNameDataGridViewTextBoxColumn.Visible = activity.FilePath == null;
            LoadSaves();
        }

        void ResumeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SaveLoader != null)
                SaveLoader.Cancel();
        }

        void LoadSaves()
        {
            if (SaveLoader != null)
                SaveLoader.Cancel();

            var warning = "";
            SaveLoader = new Task<List<Save>>(this, () =>
            {
                var saves = new List<Save>();
                var directory = Program.UserDataFolder;
                var build = VersionInfo.Build.Contains(" ") ? VersionInfo.Build.Substring(VersionInfo.Build.IndexOf(" ") + 1) : null;
                var prefix = Activity.FilePath == null ? Path.GetFileName(Route.Path) : Path.GetFileNameWithoutExtension(Activity.FilePath);
                if (Directory.Exists(directory))
                {
                    foreach (var saveFile in Directory.GetFiles(directory, prefix + "*.save"))
                    {
                        try
                        {
                            // SavePacks are all in the same folder and activities may have the same name 
                            // (e.g. Short Passenger Run shrtpass.act) but belong to a different route,
                            // so pick only the activities for the current route.
                            var save = new Save(saveFile, build);
                            if (save.RouteName == Route.Name)
                            {
                                saves.Add(save);
                            }
                            else    // In case you receive a SavePack where the activity is recognised but the route has been renamed.
                                    // Checks the route is not in your list of routes.
                                    // If so, add it with a warning.
                            {
                                if (!ParentForm.Routes.Any(el => el.Name == save.RouteName))
                                {
                                    saves.Add(save);
                                    // Save a warning to show later.
                                    warning += String.Format("Warning: Save {0} found from a route with an unexpected name:\n{1}.\n\n", save.RealTime, save.RouteName);
                                }
                            }
                        }
                        catch { }
                    }
                }
                return saves.OrderBy(s => s.RealTime).Reverse().ToList();
            }, (saves) =>
            {
                Saves = saves;
                saveBindingSource.DataSource = Saves;
                labelInvalidSaves.Text = String.Format(InvalidTextString, Saves.Count(s => !s.Valid), Saves.Count);
                gridSaves_SelectionChanged(null, null);
                // Show warning after the list has been updated as this is more useful.
                if (warning != "")
                    MessageBox.Show(warning);
            });
        }

        void ResumeSave()
        {
            var save = saveBindingSource.Current as Save;
            if (save.Valid)
            {
                if( Found(save) )
                {
                    SelectedSaveFile = save.File;
                    SelectedAction = MainForm.UserAction.SingleplayerResumeSave;
                    DialogResult = DialogResult.OK;
                }
            }
        }

        void gridSaves_SelectionChanged(object sender, EventArgs e)
        {
            // Clean up old thumbnail.
            if (pictureBoxScreenshot.Image != null)
            {
                pictureBoxScreenshot.Image.Dispose();
                pictureBoxScreenshot.Image = null;
            }

            // Load new thumbnail.
            if (gridSaves.SelectedRows.Count > 0)
            {
                var save = saveBindingSource.Current as Save;
                if (save != null)
                {
                    var thumbFileName = Path.ChangeExtension(save.File, "png");
                    if (File.Exists(thumbFileName))
                        pictureBoxScreenshot.Image = new Bitmap(thumbFileName);

                    buttonDelete.Enabled = true;
                    buttonResume.Enabled = save.Valid;
                    var replayFileName = Path.ChangeExtension(save.File, "replay");
                    buttonReplayFromPreviousSave.Enabled = (save.Valid && File.Exists(replayFileName));
                    buttonReplayFromStart.Enabled = File.Exists(replayFileName);
                }
                else
                {
                    buttonDelete.Enabled 
                        = buttonResume.Enabled 
                        = buttonReplayFromStart.Enabled
                        = buttonReplayFromPreviousSave.Enabled = false;
                }
            }
            else
            {
                buttonDelete.Enabled = buttonResume.Enabled = false;
            }

            buttonDeleteInvalid.Enabled = Saves.Any(s => !s.Valid);
            buttonUndelete.Enabled = Directory.Exists(Program.DeletedSaveFolder) && Directory.GetFiles(Program.DeletedSaveFolder).Length > 0;
        }

        void gridSaves_DoubleClick(object sender, EventArgs e)
        {
            ResumeSave();
        }

        void pictureBoxScreenshot_Click(object sender, EventArgs e)
        {
            ResumeSave();
        }

        void buttonResume_Click(object sender, EventArgs e)
        {
            ResumeSave();
        }

        void buttonDelete_Click(object sender, EventArgs e)
        {
            var selectedRows = gridSaves.SelectedRows;
            if (selectedRows.Count > 0)
            {
                gridSaves.ClearSelection();

                if (!Directory.Exists(Program.DeletedSaveFolder))
                    Directory.CreateDirectory(Program.DeletedSaveFolder);

                for (var i = 0; i < selectedRows.Count; i++)
                {
                    var save = selectedRows[i].DataBoundItem as Save;
                    foreach (var fileName in new[] { Path.GetFileName(save.File), Path.ChangeExtension(Path.GetFileName(save.File), "png") })
                    {
                        try
                        {
                            File.Move(Path.Combine(Program.UserDataFolder, fileName), Path.Combine(Program.DeletedSaveFolder, fileName));
                        }
                        catch { }
                    }
                }

                LoadSaves();
            }
        }

        void buttonUndelete_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(Program.DeletedSaveFolder))
            {
                foreach (var filePath in Directory.GetFiles(Program.DeletedSaveFolder))
                {
                    try
                    {
                        File.Move(filePath, Path.Combine(Program.UserDataFolder, Path.GetFileName(filePath)));
                    }
                    catch { }
                }

                Directory.Delete(Program.DeletedSaveFolder);

                LoadSaves();
            }
        }

        void buttonDeleteInvalid_Click(object sender, EventArgs e)
        {
            gridSaves.ClearSelection();

            foreach (var save in Saves)
            {
                if (!save.Valid)
                {
                    foreach (var fileName in new[] { save.File, Path.ChangeExtension(save.File, "png") })
                    {
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch { }
                    }
                }
            }

            LoadSaves();
        }

        private void buttonReplayFromStart_Click(object sender, EventArgs e)
        {
            SelectedAction = MainForm.UserAction.SingleplayerReplaySave;
            InitiateReplay();
        }

        private void buttonReplayFromPreviousSave_Click(object sender, EventArgs e)
        {
            SelectedAction = MainForm.UserAction.SingleplayerReplaySaveFromSave;
            InitiateReplay();
        }

        private void InitiateReplay()
        {
            var save = saveBindingSource.Current as Save;
            if (Found(save) )
            {
                SelectedSaveFile = save.File;
                Settings.ReplayPauseBeforeEnd = checkBoxReplayPauseBeforeEnd.Checked;
                Settings.ReplayPauseBeforeEndS = (int)numericReplayPauseBeforeEnd.Value;
                DialogResult = DialogResult.OK; // Anything but DialogResult.Cancel
            }
        }

        private void buttonImportExportSaves_Click(object sender, EventArgs e)
        {
            var save = saveBindingSource.Current as Save;
            using (ImportExportSaveForm form = new ImportExportSaveForm(save))
            {
                form.ShowDialog();
            }
            LoadSaves();
        }

        /// <summary>
        /// Saves may come from other, foreign installations (i.e. not this PC). 
        /// They can be replayed or resumed on this PC but they will contain activity / path / consist filenames
        /// and these may be inappropriate for this PC, typically having a different path.
        /// This method tries to use the paths in the Save if they exist on the current PC. 
        /// If not, it prompts the user to locate a matching file from those on the current PC.
        /// 
        /// The save file is then modified to contain filename(s) from the current PC instead.
        /// </summary>
        public bool Found(Save save)
        {
            {
                try
                {
                    BinaryReader inf = new BinaryReader(new FileStream(save.File, FileMode.Open, FileAccess.Read));
                    var version = inf.ReadString();
                    var build = inf.ReadString();
                    var routeName = inf.ReadString();
                    var pathName = inf.ReadString();
                    var gameTime = inf.ReadInt32();
                    var realTime = inf.ReadInt64();
                    var currentTileX = inf.ReadSingle();
                    var currentTileZ = inf.ReadSingle();
                    var initialTileX = inf.ReadSingle();
                    var initialTileZ = inf.ReadSingle();
                    var tempInt = inf.ReadInt32();
                    var savedArgs = new string[tempInt];
                    for (var i = 0; i < savedArgs.Length; i++)
                        savedArgs[i] = inf.ReadString();

                    // Re-locate files if saved on another PC
                    var rewriteNeeded = false;
                    // savedArgs[0] contains Activity or Path filepath
                    var filePath = savedArgs[0];
                    if( !System.IO.File.Exists(filePath) )
                    {
                        // Show the dialog and get result.
                        openFileDialog1.InitialDirectory = MSTSPath.Base();
                        openFileDialog1.FileName = Path.GetFileName(filePath);
                        openFileDialog1.Title = @"Find location for file " + filePath;
                        if( openFileDialog1.ShowDialog() != DialogResult.OK )
                            return false;
                        rewriteNeeded = true;
                        savedArgs[0] = openFileDialog1.FileName;
                    }
                    if( savedArgs.Length > 1 )  // Explore, not Activity
                    {
                        // savedArgs[1] contains Consist filepath
                        filePath = savedArgs[1];
                        if( !System.IO.File.Exists(filePath) )
                        {
                            // Show the dialog and get result.
                            openFileDialog1.InitialDirectory = MSTSPath.Base();
                            openFileDialog1.FileName = Path.GetFileName(filePath);
                            openFileDialog1.Title = @"Find location for file " + filePath;
                            if( openFileDialog1.ShowDialog() != DialogResult.OK )
                                return false;
                            rewriteNeeded = true;
                            savedArgs[1] = openFileDialog1.FileName;
                        }
                    }
                    if( rewriteNeeded )
                    {
                        using( BinaryWriter outf = new BinaryWriter(new FileStream(save.File + ".tmp", FileMode.Create, FileAccess.Write)) )
                        {
                            // copy the start of the file
                            outf.Write(version);
                            outf.Write(build);
                            outf.Write(routeName);
                            outf.Write(pathName);
                            outf.Write(gameTime);
                            outf.Write(realTime);
                            outf.Write(currentTileX);
                            outf.Write(currentTileZ);
                            outf.Write(initialTileX);
                            outf.Write(initialTileZ);
                            outf.Write(savedArgs.Length);
                            // copy the pars which may have changed
                            for( var i = 0; i < savedArgs.Length; i++ )
                                outf.Write(savedArgs[i]);
                            // copy the rest of the file
                            while( inf.BaseStream.Position < inf.BaseStream.Length )
                            {
                                outf.Write(inf.ReadByte());
                            }
                        }
                        inf.Close();
                        File.Replace(save.File + ".tmp", save.File, null);
                    } 
                    else
                    {
                        inf.Close();
                    }
                }
                catch
                {
                }
            }
            return true;
        }
    }
}
