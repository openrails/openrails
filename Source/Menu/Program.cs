/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

namespace ORTS
{
    static class Program
    {
        const string RunActivityProgram = "runactivity.exe";
        static string WarningLogFileName;
        public static string RegistryKey = "SOFTWARE\\OpenRails\\ORTS";
        public static string Build;
        public static string Revision;


        [STAThread]  // requred for use of the DirectoryBrowserDialog in the main form.
        static void Main(string[] args)
        {
            SetBuildRevision();
            WarningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\OpenRailsLog.txt";
            File.Delete(WarningLogFileName);

            try
            {

                MainForm MainForm = new MainForm();
                if (Revision != "000")
                    MainForm.Text = "Open Rails V " + Revision;
                else
                    MainForm.Text = "Open Rails   BUILD = " +  Build;

                while (true)
                {

                    MainForm.ShowDialog();

                    string parameter;

                    switch( MainForm.DialogResult )
                    {
                        case DialogResult.OK:
                            if (MainForm.SelectedActivityPath == null)
                                parameter = "\"" + MainForm.SelectedPath + "\" \"" + MainForm.SelectedConsist + "\"" +
                                    string.Format(" {0} {1} {2}", MainForm.ExploreStartHour, MainForm.ExploreSeason, MainForm.ExploreWeather);
                            else
                                parameter = "\"" + MainForm.SelectedActivityPath + "\"";
                            break;
                        case DialogResult.Retry: parameter = "-resume"; break;
                        default: return;
                    }

                    // find the RunActivity program, normally in the startup path, 
                    //  but while debugging it will be in an adjacent directory
                    string RunActivityFolder = Application.StartupPath.ToLower();

                    System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
                    objPSI.FileName = RunActivityFolder + @"\" + RunActivityProgram ;
                    objPSI.Arguments = parameter;
                    objPSI.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal; // or Hidden, Maximized or Normal 
                    objPSI.WorkingDirectory = RunActivityFolder;

                    System.Diagnostics.Process objProcess = System.Diagnostics.Process.Start(objPSI);

                    while (objProcess.HasExited == false)
                        System.Threading.Thread.Sleep(100);

                    int retVal = objProcess.ExitCode;
                }
            }
            catch (System.Exception error)
            {
                Warn(error.Message);
                MessageBox.Show(error.Message);
            }
        }


        public static void Warn(string s)
        {
            StreamWriter f;
            if (!File.Exists(WarningLogFileName))
            {
                f = new StreamWriter(WarningLogFileName);
                f.WriteLine("ORTS WARNING LOG");
                f.WriteLine();
                f.WriteLine("Launching Menu");
                f.WriteLine();
                f.WriteLine("------------------------------------------------");
            }
            else
            {
                f = new StreamWriter(WarningLogFileName, true); // append
            }

            f.WriteLine(s);
            f.WriteLine();
            f.WriteLine("------------------------------------------------");
            f.Close();
        }

        public static void SetBuildRevision()
        {
            try
            {
                using (StreamReader f = new StreamReader("Revision.txt"))
                {
                    string line = f.ReadLine();
                    string rev = line.Substring(11);
                    int i = rev.IndexOf('$');
                    Revision = rev.Substring(0, i).Trim();

                    Build = Application.ProductVersion;  // from assembly
                    Build = Build + " " + f.ReadLine();  // date
                    Build = Build + " " + f.ReadLine(); // time
                }
            }
            catch
            {
                Revision = "000";
                Build = Application.ProductVersion;
            }
        }

    } // class Program

}

