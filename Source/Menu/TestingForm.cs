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
using ORTS.Menu;    // needed for Activity
using Path = System.IO.Path;

namespace ORTS {

    public partial class TestingForm : Form {

        // Integers so we can reference cells in DataViewGrid by name instead of index.
        // Would use enums but that's too cumbersome.
        int cToTest = 0;
        //int cActivityName = 1;  // unused. Commented out to avoid compile warning.
        int cRoutePath = 2;
        int cActivityFileName = 3;
        int cTested = 4;
        int cPassed = 5;
        
        MainForm parentForm;
        Route selectedRoute;
        Activity selectedActivity;
        // Use SortableSearchableList instead of List so we can sort each column of the DataGrid by clicking on the column header.
        // For the path, it seems we need to click once for each "/" !
        SortableSearchableList<TestLoadActivity> tests = new SortableSearchableList<TestLoadActivity>();

        // We use a loader so the testing can take place in the background and the testing form, being in the foreground, can
        // be moved, re-sized and the buttons (especially Cancel Tests) will work at once.
        Task<List<DataGridViewRow>> DataGridViewRowLoader;
        List<DataGridViewRow> dataGridViewRowList = new List<DataGridViewRow>();

        string summaryFileName = Path.Combine( Program.UserDataFolder, "TestSummary.csv" );
        string logFileName = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Desktop ), "OpenRailsLog.txt" );
        bool isLoopInterrupted;

        public TestingForm(MainForm parentForm, Route selectedRoute, Activity selectedActivity ) {
            this.parentForm = parentForm;
            this.selectedRoute = selectedRoute;
            this.selectedActivity = selectedActivity;

            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            var activities = (Folder.GetFolders())
                .SelectMany( f => Route.GetRoutes( f ) )
                .SelectMany( r => ORTS.Menu.Activity.GetActivities( r ) )
                .Where( a => !(a is ExploreActivity) )
                .OrderBy( a => a.FilePath, StringComparer.OrdinalIgnoreCase )
                .ToList();

            string activityName = selectedActivity.Name;
            string routePath = "routePath";
            string routeName = "routeName";
            string activityFileName = selectedActivity.FilePath;
            for( var i = 0; i < activities.Count; i++ ) {
                activityName = activities[i].ACTFile.Tr_Activity.Tr_Activity_Header.Name;
                routePath = Path.GetDirectoryName( activities[i].FilePath );
                routePath = routePath.Substring( 0, routePath.Length - @"\ACTIVITIES".Length ); 
                activityFileName = Path.GetFileName( activities[i].FilePath );
                tests.Add( new TestLoadActivity( activityName, routePath, routeName, activityFileName ) );
            }
            bsTestLoadActivities.DataSource = tests;
            bCancelTest.Enabled = false;
            bViewSummary.Enabled = System.IO.File.Exists( summaryFileName );
            bViewDetails.Enabled = System.IO.File.Exists( logFileName );
        }

        private void bClose_Click( object sender, EventArgs e ) {
            this.Close();
            if( DataGridViewRowLoader != null )
                DataGridViewRowLoader.Cancel();
        }

        private void bTestLoadingOfAllActivities_Click( object sender, EventArgs e ) {
            foreach( DataGridViewRow rw in this.dgvTestLoadActivities.Rows ) {
                rw.Cells[cToTest].Value = true;
            }
            TestLoadingOfActivities();
        }

        private void bTestLoadingOfSelectedActivities_Click( object sender, EventArgs e ) {
            foreach( DataGridViewRow rw in this.dgvTestLoadActivities.Rows ) {
                rw.Cells[cToTest].Value = false;
            }
            // Use .SelectedCells not the simpler .SelectedRows so we can hide the row selector.
            foreach( DataGridViewCell cell in dgvTestLoadActivities.SelectedCells ) {
                DataGridViewRow rw = cell.OwningRow;
                rw.Cells[cToTest].Value = true;
            }
            TestLoadingOfActivities();
        }
        
        private void TestLoadingOfActivities() {
            bCancelTest.Enabled = true;
            bViewSummary.Enabled = false;
            bViewDetails.Enabled = false;
            // find the RunActivity program in the same folder as Menu.exe
            string RunActivityFolder = Application.StartupPath;

            System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
            objPSI.FileName = RunActivityFolder + @"\" + Program.RunActivityProgram;
            objPSI.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal; // or Hidden, Maximized or Normal 
            objPSI.WorkingDirectory = RunActivityFolder;

            // Delete any existing OpenRailsLog.txt file
            string logFileName = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Desktop )
                                            , "OpenRailsLog.txt" );
            File.Delete( logFileName );

            // Recreate any existing CSV file
            try {   // Could fail if already opened by Excel, so ignore errors
                string summaryFileName = Path.Combine( Program.UserDataFolder, "TestSummary.csv" );
                using( StreamWriter sw = File.CreateText( summaryFileName ) ) {
                    // Pass, Activity Name, Errors, Warnings, Infos, Folder, Route, Activity Filename
                    // Enclose strings in quotes in case they contain commas.
                    sw.Write( "Pass Test" );
                    sw.Write( String.Format( ", Activity" ) );      // e.g. Auto Train with Set-Out
                    sw.Write( String.Format( ", Errors" ) );        // critical and error
                    sw.Write( String.Format( ", Warnings" ) );      // warning
                    sw.Write( String.Format( ", Info Msgs" ) );     // information
                    sw.Write( String.Format( ", Route Path" ) );    // e.g. D:\MSTS\ROUTES\USA2
                    sw.Write( String.Format( ", Route Name" ) );    // e.g. "Marias Pass"
                    sw.Write( String.Format( ", Activity File" ) ); // e.g. "autotrnsetout.act"
                    sw.Write( String.Format( ", Loading Time (s)" ) );  // e.g. 45 secs
                    sw.Write( String.Format( ", Rate (frames/s)" ) );   // e.g. 20 frames/sec
                    sw.WriteLine( "" );
                }
            } catch { } // Ignore any errors

            if( DataGridViewRowLoader != null )
                DataGridViewRowLoader.Cancel();
            DataGridViewRowLoader = new Task<List<DataGridViewRow>>( this,
                () => {
                    foreach( DataGridViewRow rw in this.dgvTestLoadActivities.Rows ) {
                        if( (bool)rw.Cells[cToTest].Value ) {
                            // <CJ comment> Would like to scroll to row rw but can't make that work </CJ comment>
                            rw.Selected = true;
                            string parameter = " -test " + "\"" + Path.Combine( rw.Cells[cRoutePath].Value.ToString() + @"\ACTIVITIES",
                                                rw.Cells[cActivityFileName].Value.ToString() ) + "\"";
                            // Tests show that frame rates rise initially and stabilise after about 50 frames.
                            objPSI.Arguments = parameter + " /Profiling /ProfilingFrameCount=50 /ShowErrorDialogs=False"; // 10 frames enough to show that the graphics is working

                            // Start the test of the current activity, then wait for it to end
                            System.Diagnostics.Process objProcess = System.Diagnostics.Process.Start( objPSI );
                            while( objProcess.HasExited == false ) {
                                System.Threading.Thread.Sleep( 100 );
                            }

                            rw.Cells[cTested].Value = true;
                            rw.Cells[cPassed].Value = (objProcess.ExitCode == 0);
                            rw.Selected = false;
                        }
                        if( isLoopInterrupted ) break;
                    }
                    return new List<DataGridViewRow>(); // an empty list just to provide a parameter of the expected type.
                },
                ( rows ) => { // a dummy parameter
                        bCancelTest.Enabled = false;
                        bViewSummary.Enabled = System.IO.File.Exists( summaryFileName );
                        bViewDetails.Enabled = System.IO.File.Exists( logFileName );
                }
                );        
        }

        private void bCancelTest_Click( object sender, EventArgs e ) {
            isLoopInterrupted = true;
        }

        private void bViewSummary_Click( object sender, EventArgs e ) {
            // Not opening Excel directly as that requires a reference and a user may not have Excel installed.
            //ApplicationClass excelApp = new ApplicationClass();
            //excelApp.Workbooks.Open( summaryFileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing );
            //excelApp.Visible = true;
            System.Diagnostics.Process.Start( summaryFileName );
        }

        private void bViewDetails_Click( object sender, EventArgs e ) {
             System.Diagnostics.Process.Start( logFileName );
        }

        private void bsTestLoadActivities_CurrentChanged( object sender, EventArgs e ) {

        }

        private void bCancelSort_Click( object sender, EventArgs e ) {
            tests.RemoveSort();
        }
    }

    public class TestLoadActivity {

        public bool ToTest { get; set; }
        public bool Tested { get; set; }
        public bool Passed { get; set; }
        public string Activity { get; set; }
        public string RoutePath { get; set; }
        public string RouteName { get; set; }
        public string ActivityFileName { get; set; }

        public TestLoadActivity( string activity, string routePath, string routeName, string activityFileName ) {
            Activity = activity;
            RoutePath = routePath;
            RouteName = routeName;
            ActivityFileName = activityFileName;
        }
    }
}
