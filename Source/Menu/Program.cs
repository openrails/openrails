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
        const string RunActivityProgram = "run.exe";

        static string WarningLogFileName;
        public static string RegistryKey = "SOFTWARE\\OpenRails\\ORTS";


        [STAThread]  // requred for use of the DirectoryBrowserDialog in the main form.
        static void Main(string[] args)
        {

            WarningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\ORTS Warning Log.txt";
            File.Delete(WarningLogFileName);

            try
            {

                MainForm MainForm = new MainForm();
                MainForm.Text = "ORTS Launcher V" + Application.ProductVersion.Replace(".", "");

                while (true)
                {

                    MainForm.ShowDialog();
                    if (MainForm.DialogResult != DialogResult.OK) return;

                    // find the RunActivity program, normally in the startup path, 
                    //  but while debugging it will be in an adjacent directory
                    string RunActivityFolder = Application.StartupPath.ToLower();

                    System.Diagnostics.ProcessStartInfo objPSI = new System.Diagnostics.ProcessStartInfo();
                    objPSI.FileName = RunActivityFolder + @"\" + RunActivityProgram ;
                    objPSI.Arguments = "\"" + MainForm.SelectedActivityPath + "\"";
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

    } // class Program

}

