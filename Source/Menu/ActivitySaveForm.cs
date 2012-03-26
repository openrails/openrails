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
//3. The Menu program also tries to check the program and build values and tags an ActivitySave as valid, but it can't be 
//   rigorous as it doesn't have access to the program and build values of the RunActivity program. Instead, it checks the 
//   ActivitySave against its own values and passes it if the timestamp is within 5 minutes.
//4. The MenuWPF program has not been changed and the menu ORTS > Resume continues to resume from the most recent 
//   ActivitySave just as it did with SAVE.BIN

using System.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;   // needed for StringCollection
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;    // needed for class Directory
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Menu;    // needed for Activity

namespace ORTS {
    public partial class ActivitySaveForm : Form {

        List<ActivitySave> ActivitySaveList = new List<ActivitySave>();
        MainForm parentForm;
        Route selectedRoute;
        Activity selectedActivity;
        Bitmap currentImage;    // Used by 2 events

        public struct SaveCounts {
            public int invalid;
            public int total;
        }
        public SaveCounts savesFound;

        public ActivitySaveForm( MainForm parentForm, Route selectedRoute, Activity selectedActivity ) {
            this.parentForm = parentForm;
            this.selectedRoute = selectedRoute;
            this.selectedActivity = selectedActivity;

            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;
            LoadActivitySaves( selectedRoute, selectedActivity );
            if( selectedActivity.FileName == null ) {
                tbActivity.Text = String.Format("Explore route: {0}", selectedRoute.Name); 
            } else {
                tbActivity.Text = selectedActivity.Name;
            }
        }

        bool IsSaveValid(string saveBuild) {
            // Compare the build time in the Save from RunActivity.exe with the build time of Menu.exe
            // They are likely to be within a minute of each other.

            // Extract the datetime from the Build string 
            DateTime saveDate = Convert.ToDateTime( saveBuild.Substring( 15 ) );
            DateTime programDate = Convert.ToDateTime( Program.Build.Substring( 15 ) );
            TimeSpan dateDifference = saveDate - programDate;
            double minutes = Math.Abs(dateDifference.TotalMinutes);
            return minutes < 5 ? true : false;  // True if within 5 mins
        }
    
        public class ActivitySave {

            public string SaveFileName { get; set; }
            public bool Valid { get; set; }
            public string DateTimeSaved { get; set; }       // DDD dd-mm-yy HH:mm:ss to be displayed
            public string DateTimeSavedISO { get; set; }    // yyyy-mm-dd HH:mm:ss used for sorting
            public string PathDescription { get; set; }
            public string GameTimeElapsed { get; set; }
            public string DistanceFromStartM { get; set; }  // Not distance travelled but "as crow flies"
            public string TileXZ { get; set; }
            string Revision;
            string Build;
            string ActivityName;
            string ActivityPath;
            string Consist;
            // float as holds the tile number combined with the fractional distance across the tile. Tiles are 2048 metres across.
            float CurrentTileX;
            float CurrentTileZ;
            float InitialTileX;
            float InitialTileZ;

            public ActivitySave( ActivitySaveForm form, string filename ) {
                SaveFileName = Path.GetFileName( filename );
                using( BinaryReader inf = new BinaryReader( new FileStream( filename, FileMode.Open, FileAccess.Read ) ) ) {
                    
                    // Read in validation data.
                    Revision = "<unknown>";
                    Build = "<unknown>";
                    try {
                        Revision = inf.ReadString().Replace( "\0", "" );
                        Build = inf.ReadString().Replace( "\0", "" );
                    } catch { }
                    Valid = form.IsSaveValid( Build );

                    string RouteName = inf.ReadString(); // Route name
                    int argumentsCount = inf.ReadInt32();
                    if( argumentsCount == 1 ) {
                        ActivityName = Path.GetFileNameWithoutExtension( inf.ReadString() );    // Activity filename
                    } else {
                        ActivityPath = Path.GetFileNameWithoutExtension( inf.ReadString() );    // Path filename
                        Consist = Path.GetFileNameWithoutExtension( inf.ReadString() );         // Consist filename
                        ActivityName = ActivityPath;
                    }
                    PathDescription = inf.ReadString();
                    int secs = inf.ReadInt32();           // Time elapsed in game (secs)
                    GameTimeElapsed = new DateTime().AddSeconds( secs ).ToString( "HH:mm:ss" );
                    DateTimeSaved = inf.ReadString();     // Date and time in real world
                    string stem = Path.GetFileNameWithoutExtension( filename );
                    DateTimeSavedISO = stem.Substring( stem.Length - 19 );
                    
                    // DistanceFromStartM using Pythagoras theorem.
                    CurrentTileX = inf.ReadSingle();      // Location of player train TileX
                    CurrentTileZ = inf.ReadSingle();      // Location of player train TileZ
                    InitialTileX = inf.ReadSingle();      // Location of player train TileX
                    InitialTileZ = inf.ReadSingle();      // Location of player train TileZ
                    double distance = Math.Sqrt(Math.Pow(CurrentTileX - InitialTileX, 2) + Math.Pow(CurrentTileZ - InitialTileZ, 2) );
                    DistanceFromStartM = String.Format("{0:N0}", (int)((distance * 2048) + 0.5));
                    TileXZ = String.Format( "{0:0.0}, {1:0.0}", CurrentTileX, CurrentTileZ );
                }
            }
        }

        // <CJ comment>
        // In common with the older parts of the Menu program, this new form is populated using the Task class, which works in
        // the background to load the controls with data. It doesn't seem to work reliably and sometimes the list of 
        // ActivitySaves or count of invalid saves remains empty.
        // Currently replaced with a foreground version below.
        // </CJ comment>

        // Task<List<ActivitySave>> ActivitySaveLoader;
        // Task<SaveCounts> savesFoundLoader;
        
        ///// <summary>
        ///// Populates the list of ActivitySaves using a background task to keep user interface responsive.  
        ///// </summary>
        //void LoadActivitySaves( Route selectedRoute, Activity selectedActivity ) {

        //    if( ActivitySaveLoader != null ) ActivitySaveLoader.Cancel();   // Cancel any background task still running.

        //    string prefix;
        //    if( selectedActivity.FileName == null ) {
        //        Regex r1 = new Regex( @"(\\ROUTES\\)(.+)\z" );  // \ROUTES\*  \z indicates the end of text
        //        Match match = r1.Match( selectedRoute.Path );   // e.g. "D:\MSTS\ROUTES\USA1"
        //        prefix =
        //            match.Success ?
        //            match.Groups[2].Value  // Extract the 2nd group (1)(2)
        //            : "unknown route";
        //    } else {
        //        prefix = Path.GetFileNameWithoutExtension( selectedActivity.FileName );
        //    }
        //    ActivitySaveLoader = new Task<List<ActivitySave>>( this,    // Fire off a new background task linked to the current form.
        //        () => {                                                 // With this anonymous task
        //            var saves = new List<ActivitySave>();
        //            var directory = Program.UserDataFolder;
        //            if( Directory.Exists( directory ) ) {
        //                foreach( var saveFile in Directory.GetFiles( directory, prefix + "*.save" ) ) {
        //                    try {
        //                        saves.Add( new ActivitySave( this, saveFile ) );
        //                    } catch { } // Ignore any errors
        //                }
        //            }
        //            return saves;
        //        },
        //        ( saves ) => {                                          // When task complete, do this
        //            // Sort by inverse date
        //            var savesSorted = saves.OrderBy( s => s.DateTimeSavedISO );
        //            bSActivitySave.DataSource = savesSorted.Reverse();

        //            // Set button states
        //            bResume.Enabled = ( saves.Count != 0 );
        //            bDeleteSave.Enabled = ( saves.Count != 0 );
        //            string fromFolder = Program.UserDataFolder + @"\deleted_saves";
        //            string[] deletedSaves = Directory.GetFiles( fromFolder, prefix + "*.save" );
        //            bUndeleteSave.Enabled = ( deletedSaves.Length != 0 );
        //            // Problem: button is always enabled
        //            bDeleteInvalidSaves.Enabled = (savesFound.invalid > 0);
        //        }
        //        );

        //    if( savesFoundLoader != null ) savesFoundLoader.Cancel();   // Cancel any background task still running.

        //    savesFoundLoader = new Task<SaveCounts>( this,  // Fire off a new background task linked to the current form.
        //        () => {                                     // With this anonymous task
        //            // Count total saves and valid saves
        //            savesFound.total = 0;
        //            savesFound.invalid = 0;
        //            var directory = Program.UserDataFolder;
        //            if( Directory.Exists( directory ) ) {
        //                foreach( var saveFile in Directory.GetFiles( directory, "*.save" ) ) {
        //                    try {
        //                        savesFound.total++;
        //                        using( BinaryReader inf = new BinaryReader( new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
        //                            // Read in validation data.
        //                            string revision = inf.ReadString().Replace( "\0", "" );
        //                            string build = inf.ReadString().Replace( "\0", "" );
        //                            if( !IsSaveValid( build ) ) {
        //                                savesFound.invalid++;
        //                            }
        //                        }
        //                    } catch { } // Ignore any errors
        //                }
        //            }
        //            return savesFound;
        //        },
        //        ( savesFound ) => {                         // When task complete, do this
        //            lSaveTotals.Text = String.Format( "{0} / {1}", savesFound.invalid, savesFound.total );
        //        }
        //        );
        //}

        /// <summary>
        /// Populates the list of ActivitySaves using a background task to keep user interface responsive.  
        /// </summary>
        void LoadActivitySaves( Route selectedRoute, Activity selectedActivity ) {

            var saves = new List<ActivitySave>();
            string prefix;
            if( selectedActivity.FileName == null ) {
                // If exploring route (so no activity), extract the route folder name
                Regex r1 = new Regex( @"(\\ROUTES\\)(.+)\z" );  // \ROUTES\*  \z indicates the end of text
                Match match = r1.Match( selectedRoute.Path );   // e.g. "D:\MSTS\ROUTES\USA1"
                prefix =
                    match.Success ?
                    match.Groups[2].Value  // Extract the 2nd group (1)(2)
                    : "unknown route";
            } else { // extract the activity file stem
                prefix = Path.GetFileNameWithoutExtension( selectedActivity.FileName );
            }
            var directory = Program.UserDataFolder;
            if( Directory.Exists( directory ) ) {
                foreach( var saveFile in Directory.GetFiles( directory, prefix + "*.save" ) ) {
                    try {
                        saves.Add( new ActivitySave( this, saveFile ) );
                    } catch { } // Ignore any errors
                }
            }
            // Sort by inverse date
            var savesSorted = saves.OrderBy( s => s.DateTimeSavedISO );
            bSActivitySave.DataSource = savesSorted.Reverse();

            // Count total saves and valid saves
            savesFound.total = 0;
            savesFound.invalid = 0;
            if( Directory.Exists( directory ) ) {
                foreach( var saveFile in Directory.GetFiles( directory, "*.save" ) ) {
                    try {
                        savesFound.total++;
                        using( BinaryReader inf = new BinaryReader( new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
                            // Read in validation data.
                            string revision = inf.ReadString().Replace( "\0", "" );
                            string build = inf.ReadString().Replace( "\0", "" );
                            if( !IsSaveValid( build ) ) {
                                savesFound.invalid++;
                            }
                        }
                    } catch { } // Ignore any errors
                }
            }
            lSaveTotals.Text = String.Format( "{0} / {1}", savesFound.invalid, savesFound.total );

            // Set button states
            bResume.Enabled = (saves.Count != 0);
            bDeleteSave.Enabled = (saves.Count != 0);
            string fromFolder = Program.UserDataFolder + @"\deleted_saves";
            if( Directory.Exists( fromFolder ) ) {
                string[] deletedSaves = Directory.GetFiles( fromFolder, prefix + "*.save" );
                bUndeleteSave.Enabled = (deletedSaves.Length != 0);
            } else {
                bUndeleteSave.Enabled = false;
            }
            bDeleteInvalidSaves.Enabled = (savesFound.invalid > 0);
        }

        private void bResume_Click( object sender, EventArgs e ) {
            ResumeActivitySave();
        }

        private void pictureBox1_Click( object sender, EventArgs e ) {
            ResumeActivitySave();
        }

        private void dGVActivitySave_DoubleClick( object sender, EventArgs e ) {
            ResumeActivitySave();
        }

        void ResumeActivitySave() {
            var save = bSActivitySave.Current as ActivitySave;
            if( save.Valid ) {
                parentForm.ActivitySaveFilename = save.SaveFileName;
                parentForm.ResumeFromSavePressed = true;
                this.Close();
                parentForm.Close();
            }
        }

        private void bDeleteSave_Click( object sender, EventArgs e ) {
            // Use .SelectedCells not the simpler .SelectedRows so we can hide the row selector.
            var deleteList = dGVActivitySave.SelectedCells;
            if( deleteList.Count > 0 ) {
                string fromFolder = Program.UserDataFolder;
                string toFolder = Program.UserDataFolder + @"\deleted_saves";
                if( !Directory.Exists( toFolder ) ) {
                    System.IO.Directory.CreateDirectory( toFolder );
                }
                for( int i = 0; i < deleteList.Count; i++ ) {
                    var item = deleteList[i].RowIndex;
                    var save = bSActivitySave[item] as ActivitySave;
                    var filename = Path.GetFileNameWithoutExtension( save.SaveFileName );
                    // Move both .save and .png files. Using ".*" leads to an error.
                    try {   // Ignore any missing files
                        System.IO.File.Move( fromFolder + @"\" + filename + @".save", toFolder + @"\" + filename + @".save" );
                        System.IO.File.Move( fromFolder + @"\" + filename + @".png", toFolder + @"\" + filename + @".png" );
                    } 
                    catch { }
                }
                // Refresh the list of saves
                LoadActivitySaves( selectedRoute, this.selectedActivity );
            }
        }

        private void bUndeleteSave_Click( object sender, EventArgs e ) {
            var activityFileStem = Path.GetFileNameWithoutExtension( selectedActivity.FileName );
            string fromFolder = Program.UserDataFolder + @"\deleted_saves";
            string toFolder = Program.UserDataFolder;
            if( Directory.Exists( fromFolder ) ) {
                foreach( var fullFilename in Directory.GetFiles( fromFolder, activityFileStem + "*.*" ) ) {
                    var filename = Path.GetFileName( fullFilename );
                    System.IO.File.Move( fromFolder + @"\" + filename, toFolder + @"\" + filename );
                }
                // Refresh the list of saves
                LoadActivitySaves( selectedRoute, this.selectedActivity );
            }
        }

        private void dGVActivitySave_SelectionChanged( object sender, EventArgs e ) {
            // Load a fresh thumbnail
            // Use .SelectedCells not the simpler .SelectedRows so we can hide the row selector.
            var list = dGVActivitySave.SelectedCells;
            if( list.Count > 0 ) {
                string fromFolder = Program.UserDataFolder;
                var item = list[0].RowIndex;
                var save = bSActivitySave[item] as ActivitySave;
                var filename = Path.GetFileNameWithoutExtension( save.SaveFileName );
                var imageFile = fromFolder + @"\" + filename + ".png";
                if( System.IO.File.Exists( imageFile ) ) {
                    // Stretches the image to fit the pictureBox.
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    currentImage = new Bitmap( imageFile );
                    pictureBox1.Image = (Image)currentImage;
                } else {
                    pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                    pictureBox1.Image = (Image)SystemIcons.Error.ToBitmap();
                }
                bResume.Enabled = save.Valid;
            } else {
                bResume.Enabled = false;
            }
        }

        private void bDeleteInvalidSaves_Click( object sender, EventArgs e ) {
            var directory = Program.UserDataFolder;
            if( Directory.Exists( directory ) ) {
                foreach( var saveFile in Directory.GetFiles( directory, "*.save" ) ) {
                    try {
                        string revision = "<unknown>";
                        string build = "<unknown>";
                        using( BinaryReader inf = new BinaryReader( new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
                            // Read in validation data.
                            revision = inf.ReadString().Replace( "\0", "" );
                            build = inf.ReadString().Replace( "\0", "" );
                        }
                        if( ! IsSaveValid( build ) ) {
                            System.IO.File.Delete( saveFile );
                            var filename = Path.GetFileNameWithoutExtension( saveFile );
                            var imageFile = Program.UserDataFolder + @"\" + filename + @".png";
                            // PROBLEM: Cannot delete the currently shown image as that is locked.
                            // Setting to null below does not release the lock.
                            pictureBox1.Image = null;
                            currentImage = null;
                            System.IO.File.Delete( imageFile );
                        }
                    } catch { } // Ignore any errors
                }
            }
            // Refresh the list of saves
            LoadActivitySaves( selectedRoute, this.selectedActivity );
        }

        private void bClose_Click( object sender, EventArgs e ) {
            this.Close();
        }
    }
}
