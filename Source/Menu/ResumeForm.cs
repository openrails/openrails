//This form adds the ability to save the state of the simulator (an ActivitySave) multiple times and replace the previous 
//single save to the file SAVE.BIN.

//ActivitySaves are made to the folder Program.UserDataFolder (e.g.
//    C:\Users\Chris\AppData\Roaming\Open Rails\ 
//and take the form  <activity file name> <date> <time>.save. E.g.
//    yard_two 2012-03-20 22.07.36.save

//As ActivitySaves for all routes are saved in the same folder and activity file names might be common, the date and time 
//elements ensure that the ActivitySave file names are unique.

//If the player is not running an activity but exploring a route, the filename takes the form  
//<route folder name> <date> <time>.save. E.g.
//    USA2 2012-03-20 22.07.36.save

//The RunActivity program takes switches; one of these is -resume
//The -resume switch can now take an ActivitySave file name as a parameter. E.g.
//    RunActivity.exe -resume "yard_two 2012-03-20 22.07.36"
//or
//    RunActivity.exe -resume "yard_two 2012-03-20 22.07.36.save"

//If no parameter is provided, then RunActivity uses the most recent ActivitySave.

//When the RunActivity program saves an ActivitySave, it adds its own program and build values. When resuming from an 
//ActivitySave, it checks that the program and build values from the file match its own and rejects an ActivitySave that 
//it didn't create.

//The intention is to increase reliability by preventing crashes. A newer version of RunActivity might make use of 
//additional values in the ActivitySave file which will not be present if saved by a previous version. Techniques 
//to maintain compatibility are possible but too onerous for a voluntary team.

//Some problems remain (see <CJ comment> in the source code):
//1. A screen-capture image is saved along with the ActivitySave. The intention is that this image should be a thumbnail
//   but I can't find how to code this successfully. In the meantime, the screen-capture image that is saved is full-size 
//   but displayed as a thumbnail. This wastes time and disk space.
//2. In common with the older parts of the Menu program, the new form was populated using the Task class, which works in 
//   the background to load the controls with data. It didn't work reliably and sometimes the list of ActivitySaves or 
//   count of invalid saves remained empty. It has been temporarily replaced by foreground processing.
//3. The MenuWPF program has not been changed and the menu ORTS > Resume continues to resume from the most recent 
//   ActivitySave just as it did with SAVE.BIN

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Menu;
using Path = System.IO.Path;

namespace ORTS
{
    public partial class ResumeForm : Form
    {
        const string InvalidTextString = "To prevent crashes and unexpected behavior, new versions of Open Rails invalidate old saved games. {0} / {1} saves are no longer valid.";

        readonly Route Route;
        readonly Activity Activity;
        readonly string Build;
        readonly string DeletedSavesPath;

        List<Save> Saves = new List<Save>();
        Task<List<Save>> SaveLoader;

        public class Save
        {
            public string File { get; private set; }
            public string PathName { get; private set; }
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

                        PathName = pathName;
                        GameTime = gameTime;
                        RealTime = realTime;
                        CurrentTile = currentTile;
                        Distance = distance;
                        Valid = currentBuild == null || build.EndsWith(currentBuild);
                    }
                    catch { }
                }
            }
        }

        public string SelectedSaveFile { get; set; }

        public ResumeForm(Route route, Activity activity)
        {
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Route = route;
            Activity = activity;
            Build = GetBuild();
            DeletedSavesPath = Path.Combine(Program.UserDataFolder, "deleted_saves");
            Text = String.Format("{0} - {1} - {2}", Text, route.Name, activity.FilePath != null ? activity.Name : "Explore Route");

            gridSaves_SelectionChanged(null, null);
            pathNameDataGridViewTextBoxColumn.Visible = activity.FilePath == null;
            LoadSaves();
        }

        void ResumeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SaveLoader != null)
                SaveLoader.Cancel();
        }

        string GetBuild()
        {
            try
            {
                using (var f = new StreamReader("Revision.txt"))
                {
                    f.ReadLine();
                    return f.ReadLine() + " " + f.ReadLine(); // date, time
                }
            }
            catch
            {
                return null;
            }
        }

        void LoadSaves()
        {
            if (SaveLoader != null)
                SaveLoader.Cancel();

            SaveLoader = new Task<List<Save>>(this, () =>
            {
                var saves = new List<Save>();
                var directory = Program.UserDataFolder;
                var prefix = Activity.FilePath == null ? Path.GetFileName(Route.Path) : Path.GetFileNameWithoutExtension(Activity.FilePath);
                if (Directory.Exists(directory))
                {
                    foreach (var saveFile in Directory.GetFiles(directory, prefix + "*.save"))
                    {
                        try
                        {
                            saves.Add(new Save(saveFile, Build));
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
            });
        }

        void ResumeSave()
        {
            var save = saveBindingSource.Current as Save;
            if (save.Valid)
            {
                SelectedSaveFile = save.File;
                DialogResult = DialogResult.OK;
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
                }
                else
                {
                    buttonDelete.Enabled = buttonResume.Enabled = false;
                }
            }
            else
            {
                buttonDelete.Enabled = buttonResume.Enabled = false;
            }

            buttonDeleteInvalid.Enabled = Saves.Any(s => !s.Valid);
            buttonUndelete.Enabled = Directory.Exists(DeletedSavesPath) && Directory.GetFiles(DeletedSavesPath).Length > 0;
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

                if (!Directory.Exists(DeletedSavesPath))
                    Directory.CreateDirectory(DeletedSavesPath);

                for (var i = 0; i < selectedRows.Count; i++)
                {
                    var save=selectedRows[i].DataBoundItem as Save;
                    foreach (var fileName in new[] { Path.GetFileName(save.File), Path.ChangeExtension(Path.GetFileName(save.File), "png") })
                    {
                        try
                        {
                            File.Move(Path.Combine(Program.UserDataFolder, fileName), Path.Combine(DeletedSavesPath, fileName));
                        }
                        catch { }
                    }
                }

                LoadSaves();
            }
        }

        void buttonUndelete_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(DeletedSavesPath))
            {
                foreach (var filePath in Directory.GetFiles(DeletedSavesPath))
                {
                    try
                    {
                        File.Move(filePath, Path.Combine(Program.UserDataFolder, Path.GetFileName(filePath)));
                    }
                    catch { }
                }

                Directory.Delete(DeletedSavesPath);

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
    }
}
