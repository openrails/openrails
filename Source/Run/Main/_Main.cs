/// <summary>
/// This application runs an activity.  After loading the activity, main
/// sets up the simulator engine and connects a 3D viewer to the payer locomotive.
/// 
/// The simulator engine contains all the elements that represent the operations on a route including 
/// signal conditions, switch track alignment, rolling stock location and movement, track paths, 
/// AI logic, physics calculations, essentially everything except the 3d representation of the objects.  
/// It is intended that the simulator engine could run in separate thread, or even on a separate computer.
/// 
/// There can be multiple viewers looking at the simulator - ie straight down activity editor type views,
/// or full 3D viewers or potentially viewers on a different computer.   The 3D viewer is responsible for 
/// loading and rendering all the shape files in the scene.  It also handles movement of wheels and other 
/// animations as directed by values stored in the simulator engine.
/// 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// </summary>


using System;
using System.Windows.Forms;
using System.IO;
using MSTS;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace ORTS
{
    static class Program
    {
        static string ActivityPath;
        static bool WarningsOn = true;
        public static string Version = Application.ProductVersion.Replace(".", "");
        public static string RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

        [STAThread]  // requred for use of the DirectoryBrowserDialog in the main form.
        static void Main(string[] args)
        {
            if (WarningsOn)
            {
                string warningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\ORTS Log.txt";
                File.Delete(warningLogFileName);
                Console.SetError(new ErrorLogger( warningLogFileName ));
                Console.SetOut(new Logger( warningLogFileName ));
                Console.WriteLine("ORTS V" + Version );
                Console.WriteLine();
            }


            //TSectionDatFile tsdat = new TSectionDatFile(@"c:\users\wayne\desktop\tsection.dat");
            //YFile yfile = new YFile("c:\\program files\\microsoft games\\train simulator\\ROUTES\\USA1\\TILES\\-07415a30_y.raw");
            //ACTFile actFile = new ACTFile(@"C:\personal\mststest\routes\lps\activities\ls1.act");
            //WAGFile wagFile = new WAGFile(@"C:\Personal\MSTSTEST\trains\trainset\BCER415xBoxes\BCER4155.wag");
           //Test.AllFiles();
           //return;
            
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            try
            {
                bool isFullScreen = false;

                // Restore retained settings
                RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
                if (RK != null)
                {
                    isFullScreen = (int)RK.GetValue("Fullscreen", 0) == 1 ? true : false;
                    WarningsOn = (int)RK.GetValue("Warnings", 1) == 1 ? true : false;
                }


                if (args.Length == 0)  
                {
                    // As a convenience during debugging  TODO remove before release
                    MainForm MainForm = new MainForm();
                    MainForm.Text = "ORTS V" + Version;

                    MainForm.ShowDialog();
                    if (MainForm.DialogResult != DialogResult.OK) return;
                    ActivityPath = MainForm.SelectedActivityPath;
                }
                else if (args.Length == 1 && args[0] == "-random")  
                {
                    // For debuging purposes - random activity - TODO remove before release
                    List<string> activityPaths = new List<string>();
                    foreach (string basePath in Test.AllBaseFolders)
                        foreach( string routePath in Directory.GetDirectories( basePath + @"\ROUTES" ) )
                            foreach( string activityPath in Directory.GetFiles(routePath + @"\ACTIVITIES", "*.act") )
                            {
                                if (0 != string.Compare(Path.GetFileNameWithoutExtension(activityPath), "ITR_e1_s1_w1_t1", true))  // ignore these, seems to be some sort of internal function
                                {
                                    activityPaths.Add( activityPath );
                                }
                            }
                        
                    Random random = new Random();
                    ActivityPath = activityPaths[random.Next(activityPaths.Count)];
                }
                else if (args.Length == 1 && args[0] == "-join")
                {
                    Console.WriteLine(ActivityPath);

                    using (Simulator simulator = new Simulator( "", ""))
                    {
                        using (Viewer viewer = new Viewer(simulator, isFullScreen))
                        {
                            viewer.Run();
                        }
                    }

                    return;
                }
                else
                {
                    // This is the normal behaviour
                    ActivityPath = args[0];
                }


                Console.WriteLine("Activity = " + ActivityPath);
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------");

                using (Simulator simulator = new Simulator(ActivityPath))
                {
                    using (Viewer viewer = new Viewer(simulator, isFullScreen))
                    {
                        viewer.Run();
                    }
                }
            }
            catch( System.Exception error )
            {
                Console.Error.WriteLine(error.Message);
                MessageBox.Show(error.Message);
            }
        }


    }//Program


    public class ErrorLogger: Logger
    {
        public ErrorLogger(string filename)
            : base(filename)
        {
        }

        public override void WriteLine(string value)
        {
            console.WriteLine();
            console.WriteLine( "ERROR: " + value);
            console.WriteLine();
            Warn("ERROR: " + value);
        }
    }

    public class Logger : TextWriter
    {
        static string WarningLogFileName = null;
        static protected TextWriter console = null;

        public Logger(string filename)
            : base()
        {
            if( WarningLogFileName == null )
                WarningLogFileName = filename;
            if( console == null )
                console = Console.Out;
        }

        public override void WriteLine(string value)
        {
            console.WriteLine(value);
            Warn(value);
        }

        public override void Write(string value)
        {
            console.Write( value );
        }

        public override void WriteLine()
        {
            console.WriteLine();
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.ASCII; }
        }

        public void Warn(string s)
        {
            StreamWriter f;
            if (!File.Exists(WarningLogFileName))
            {
                f = new StreamWriter(WarningLogFileName);
            }
            else
            {
                f = new StreamWriter(WarningLogFileName, true); // append
            }

            f.WriteLine(s);
            f.WriteLine();
            f.Close();
        }

    }
}

