/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.IO;
using System.Windows.Forms;
using ORTS.Menu;

namespace ORTS
{
	static class Program
	{
        public const string RunActivityProgram = "runactivity.exe";

		public static string Version;         // ie "0.6.1"
		public static string Build;           // ie "0.0.3661.19322 Sat 01/09/2010  10:44 AM"
		public static string RegistryKey;     // ie @"SOFTWARE\OpenRails\ORTS"
		public static string UserDataFolder;  // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails"

		[STAThread]  // requred for use of the DirectoryBrowserDialog in the main form.
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();

			SetBuildRevision();

			UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);

			RegistryKey = @"SOFTWARE\OpenRails\ORTS";

			try
			{
				var MainForm = new MainForm();

				while (true)
				{
					string parameter;
                    
                    MainForm.ResumeFromSavePressed = false;
                    var result = MainForm.ShowDialog();

                    if( MainForm.ResumeFromSavePressed ) {
                        parameter = "-resume" + String.Format( " \"{0}\"", Path.GetFileNameWithoutExtension(MainForm.ActivitySaveFilename ));
                    } else {

                        switch( result ) {
                            case DialogResult.OK:
                                var exploreActivity = MainForm.SelectedActivity as ExploreActivity;
                                string activityArguments = ( exploreActivity == null ) ?
                                    String.Format( "\"{0}\"", MainForm.SelectedActivity.FileName ) :
                                    String.Format( "\"{0}\" \"{1}\" {2}:{3} {4} {5}", exploreActivity.Path, exploreActivity.Consist, exploreActivity.StartHour, exploreActivity.StartMinute, exploreActivity.Season, exploreActivity.Weather );

                                // Multiplayer is (currently) detected from the number of arguments passed to RunActivity.exe
                                //
                                //No of Args   Activity  Explore
                                //Single-Player   1         5
                                //      Server    3         7
                                //      Client    4         8
                                //
                                //Single-Player arguments for an activity:
                                //- Activity
                                //
                                //Server arguments for an activity:
                                //- Activity | PortNo | Username+MPCode
                                //
                                //Client arguments for an activity:
                                //- Activity | IP | PortNo | Username+MPCode
                                string multiPlayerArguments = "";
                                if( MainForm.IsMultiPlayer ) {
                                    string codedUsername = String.Format("\"{0} {1}\"", MainForm.Username, MainForm.MPCode);
                                    multiPlayerArguments = ( MainForm.IsClient ) ?
                                        String.Format( " {0} {1} {2}", MainForm.IP, MainForm.PortNo, codedUsername ) :
                                        String.Format( " {0} {1}",                  MainForm.PortNo, codedUsername );
                                }
                                parameter = activityArguments + multiPlayerArguments;
                                break;
                            case DialogResult.Retry:
                                parameter = "-resume";
                                break;
                            default:
                                return;
                        }
                    }

					// find the RunActivity program, normally in the startup path, 
					//  but while debugging it will be in an adjacent directory
					string RunActivityFolder = Application.StartupPath.ToLower();

					System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
					objPSI.FileName = RunActivityFolder + @"\" + RunActivityProgram;
					objPSI.Arguments = parameter;
					objPSI.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal; // or Hidden, Maximized or Normal 
					objPSI.WorkingDirectory = RunActivityFolder;

					System.Diagnostics.Process objProcess = System.Diagnostics.Process.Start(objPSI);

					while (objProcess.HasExited == false)
						System.Threading.Thread.Sleep(100);

					int retVal = objProcess.ExitCode;
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.ToString());
			}
		}

		/// <summary>
		/// Set up the global Build and Revision variables
		/// from assembly data and the revision.txt file.
		/// </summary>
		static void SetBuildRevision()
		{
			try
			{
                using (StreamReader f = new StreamReader("Version.txt"))
                {
                    Version = f.ReadLine();
                }

                using (StreamReader f = new StreamReader("Revision.txt"))
                {
					var line = f.ReadLine();
                    var revision = line.Substring(11, line.IndexOf('$', 11) - 11).Trim();
                    if (revision != "000")
                        Version += "." + revision;
                    else
                        Version = "";

					Build = Application.ProductVersion;  // from assembly
					Build = Build + " " + f.ReadLine();  // date
					Build = Build + " " + f.ReadLine(); // time
				}
			}
			catch
			{
				Version = "";
				Build = Application.ProductVersion;
			}
		}
	} // class Program
}
