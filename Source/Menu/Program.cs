/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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

                using (var MainForm = new MainForm())
                {
                    while (true)
                    {
                        var dialogResult = MainForm.ShowDialog();
                        if (dialogResult == DialogResult.Cancel)
                            break;

                        var parameters = new List<string>();
                        switch (dialogResult)
                        {
                            case DialogResult.OK:
                                switch (MainForm.Multiplayer)
                                {
                                    case MainForm.MultiplayerMode.Server:
                                        parameters.Add("-multiplayerserver");
                                        break;
                                    case MainForm.MultiplayerMode.Client:
                                        parameters.Add("-multiplayerclient");
                                        break;
                                    default:
                                        parameters.Add("-start");
                                        break;
                                }
                                var exploreActivity = MainForm.SelectedActivity as ORTS.Menu.ExploreActivity;
                                if (exploreActivity == null)
                                    parameters.Add(String.Format("\"{0}\"", MainForm.SelectedActivity.FilePath));
                                else
                                    parameters.Add(String.Format("\"{0}\" \"{1}\" {2}:{3} {4} {5}", exploreActivity.Path.FilePath, exploreActivity.Consist.FilePath, exploreActivity.StartHour, exploreActivity.StartMinute, exploreActivity.Season, exploreActivity.Weather));
                                break;
                            case DialogResult.Retry:
                                parameters.Add("-resume");
                                parameters.Add("\"" + Path.GetFileNameWithoutExtension(MainForm.SelectedSaveFile) + "\"");
                                break;
                        }

                        var processStartInfo = new System.Diagnostics.ProcessStartInfo();
                        processStartInfo.FileName = Path.Combine(Application.StartupPath, RunActivityProgram);
                        processStartInfo.Arguments = String.Join(" ", parameters.ToArray());
                        processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                        processStartInfo.WorkingDirectory = Application.StartupPath;

                        var process = Process.Start(processStartInfo);
                        process.WaitForExit();
                    }
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
