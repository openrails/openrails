/// <summary>
/// This application runs an activity.  After loading the activity, main
/// sets up the simulator engine and connects a 3D viewer 
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
using System.Diagnostics;

namespace ORTS
{
    static class Program
    {
        public static string ActivityPath;
        public static string Revision;        // ie 078
        public static string Build;           // ie "0.0.3661.19322 Sat 01/09/2010  10:44 AM"
        public static string RegistryKey;     // ie "SOFTWARE\\OpenRails\\ORTS"
        public static string UserDataFolder;  // ie "C:\\Users\\Wayne\\AppData\\Roaming\\ORTS"
        public static double RealTime = 0;    // tracks the real time in seconds for the frame we are currently processing
        public static Random Random = new Random();  // primary random number generator used throughout the program
        public static Simulator Simulator; 
        private static Viewer3D Viewer;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            SetBuildRevision();

            UserDataFolder = Path.GetDirectoryName( Path.GetDirectoryName(Application.UserAppDataPath));

            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

            if (IsWarningsOn()) EnableLogging();

            if (!ValidateArgs(args)) return;

            if (args[0] == "-runtest")

                Testing.Test();

            else if (args[0] == "-random")

                Start(Testing.GetRandomActivity());

            else if (args[0] == "-resume")

                Resume();

            else

                Start(args[0]);

        }


        /// <summary>
        /// Run the specified activity from the beginning.
        /// </summary>
        public static void Start(string parameter)
        {
            try
            {
                ActivityPath = parameter;

                Console.WriteLine("Starting Activity = " + ActivityPath);
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------");

                Simulator = new Simulator(ActivityPath);
                Simulator.Start();
                Viewer = new Viewer3D(Simulator);
                Viewer.Run();
            }
            catch (System.Exception error)
            {
                Console.Error.WriteLine(error.Message);
                MessageBox.Show(error.Message);
            }
        }


        /// <summary>
        /// Save the current game state for later resume.
        /// Currently only supports one save, in a SAVE.BIN file in 
        /// the users local program storage, 
        /// ie.  "C:\\Users\\Wayne\\AppData\\Roaming\\ORTS\\SAVE.BIN"
        /// </summary>
        public static void Save()
        {
            try
            {
                using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\SAVE.BIN", FileMode.Create, FileAccess.Write)))
                {
                    outf.Write(ActivityPath);
                    Simulator.Save(outf);
                    Viewer.Save(outf);
                    Console.WriteLine("\nSaved");
                }
            }
            catch (System.Exception error)
            {
                Console.Error.WriteLine("While Saving: " + error.Message);
            }
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
        public static void Resume()
        {
            try
            {
                using( BinaryReader inf = new BinaryReader( new FileStream( UserDataFolder + "\\SAVE.BIN", FileMode.Open, FileAccess.Read )) )
                {
                    ActivityPath = inf.ReadString();

                    Console.WriteLine("Restoring Activity = " + ActivityPath);
                    Console.WriteLine();
                    Console.WriteLine("------------------------------------------------");

                    Simulator = new Simulator(ActivityPath);
                    Simulator.Restore(inf);
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Restore(inf);
                }
                Viewer.Run();
            }
            catch (System.Exception error)
            {
                Console.Error.WriteLine("While restoring: " + error.Message);
            }
        }


        /// <summary>
        /// If the command line arguments are invalid, 
        /// display an error message and return false.
        /// </summary>
        /// <param name="args"></param>
        public static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Missing activity file name\r\n   ie RunActivity \"c:\\program files\\microsoft games\\train simulator\\routes\\usa1\\activites\\xxx.act\"\r\n\r\nOr launch the OpenRails program and select from the menu.");
                Console.ReadKey();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the registry and return true if the OpenRailsLog.TXT
        /// file should be created.
        /// </summary>
        public static bool IsWarningsOn()
        {
            // TODO Read from Registry
            return true;
        }


        /// <summary>
        /// Set up to capture all console and error I/O into a  log file.
        /// </summary>
        public static void EnableLogging()
        {
            string warningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\OpenRailsLog.txt";
            File.Delete(warningLogFileName);
            ErrorLogger errorLogger = new ErrorLogger(warningLogFileName);
            TraceListener traceListener = new System.Diagnostics.TextWriterTraceListener(errorLogger);
            System.Diagnostics.Debug.Listeners.Insert(0, traceListener);
            System.Diagnostics.Trace.Listeners.Insert(0, traceListener);
            Console.SetError(errorLogger);
            Console.SetOut(new Logger(warningLogFileName));
            Console.WriteLine("SVN V = " + Revision);
            Console.WriteLine("BUILD = " + Build);
            Console.WriteLine();
        }

        /// <summary>
        /// Set up the global Build and Revision variables
        /// from assembly data and the revision.txt file.
        /// </summary>
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
                Revision = "";
                Build = Application.ProductVersion;
            }
        }

        /// <summary>
        /// This class is for programmer's use in setting up adhoc tests.
        /// </summary>
        class Testing
        {

            static string[] BaseFolders = new string[] { @"c:\personal\msts", @"c:\personal\mststest", @"c:\program files\microsoft games\train simulator" };


            /// <summary>
            /// For testing purposes, select a random activity from the available routes.
            /// </summary>
            public static string GetRandomActivity()
            {
                List<string> activityFileNames = new List<string>();

                foreach (string baseFolder in BaseFolders)
                {
                    string[] routeFolders = Directory.GetDirectories( baseFolder + @"\routes" );
                    foreach (string routeFolder in routeFolders)
                    {
                        string[] activityFiles = Directory.GetFiles(routeFolder + @"\activities", "*.act");
                        foreach (string activityFileName in activityFiles)
                        {
                            activityFileNames.Add(activityFileName);
                        }
                    }
                }

                int i = Random.Next(activityFileNames.Count);
                return activityFileNames[i];
            }


            /// <summary>
            /// Adhoc testing for programmers
            /// </summary>
            public static void Test()
            {

                TestAll();

                Console.WriteLine("DONE");
                Console.ReadKey();
            }

            /// <summary>
            /// Test all files in all MSTS folders 
            /// used by the development team for adhoc testing - customize this for whatever you need
            /// </summary>
            public static void TestAll()
            {
                List<string> FileNames = new List<string>();

                foreach (string baseFolder in BaseFolders)
                {
                    string[] routeFolders = Directory.GetDirectories(baseFolder + @"\routes");
                    foreach (string routeFolder in routeFolders)
                    {
                        string[] filenamesinfolder = Directory.GetFiles(routeFolder + @"\world", "*.ace");
                        foreach (string filenameinfolder in filenamesinfolder)
                        {
                            FileNames.Add(filenameinfolder);
                        }
                    }
                }

                // RUN TEST HERE
                foreach( string filename in FileNames )
                    try
                    {
                        WFile file = new WFile(filename);
                    }
                    catch (System.Exception error)
                    {
                        Console.Error.WriteLine("While testing " + filename + "\r\n   " + error.Message );
                    }

            } // TestAll
        }

    }// Program

}

