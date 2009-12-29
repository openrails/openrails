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
        public static string Version = Application.ProductVersion;
        public static string RegistryKey = "SOFTWARE\\OpenRails\\ORTS";
        public static double RealTime = 0;  // tracks the real time for the frame we are currently processing
                                                   // only update process may 

        public static Simulator Simulator; 

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Version = SVNRevision();

            // TODO, read warnings on/off from the registry
            bool WarningsOn = true;

            if (WarningsOn)
            {
                string warningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\OpenRailsLog.txt";
                File.Delete(warningLogFileName);
                ErrorLogger errorLogger = new ErrorLogger(warningLogFileName);
                TraceListener traceListener = new System.Diagnostics.TextWriterTraceListener(errorLogger);
                System.Diagnostics.Debug.Listeners.Insert(0, traceListener);
                System.Diagnostics.Trace.Listeners.Insert(0, traceListener);
                Console.SetError(errorLogger);
                Console.SetOut(new Logger(warningLogFileName));
                Console.WriteLine("ORTS V " + Version);
                Console.WriteLine();
                Console.WriteLine("Build = " + Application.ProductVersion);
                Console.WriteLine();
            }

            if (args.Length == 0)
            {
                Console.WriteLine( "Missing activity file name\r\n   ie RunActivity \"c:\\program files\\microsoft games\\train simulator\\routes\\usa1\\activites\\xxx.act\"\r\n\r\nOr launch the OpenRails program and select from the menu." );
                Console.ReadKey();
                return;
            }

            try
            {
                if (args[0] == "-random")
                    ActivityPath = Testing.GetRandomActivity();
                else
                    ActivityPath = args[0];
                Console.WriteLine("Activity = " + ActivityPath);
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------");

                Program.Simulator = new Simulator(ActivityPath);
                Viewer3D viewer = new Viewer3D(Simulator);
                viewer.Run();

            }
            catch (System.Exception error)
            {
                Console.Error.WriteLine(error.Message);
                MessageBox.Show(error.Message);
            }

        }


        public static string SVNRevision()
        {
            try
            {
                using (StreamReader f = new StreamReader("Revision.txt"))
                {
                    string line = f.ReadLine();
                    string rev = line.Substring(11);
                    int i = rev.IndexOf('$');
                    return rev.Substring(0, i);
                }
            }
            catch
            {
                return "XX";
            }
        }

        // TODO REMOVE EXPERIMENT RUNNING IN NEW PROCESS
        class ViewerProcess
        {
            public ViewerProcess()
            {
                Thread thread = new Thread(Run);
                thread.Priority = ThreadPriority.Highest;
                thread.Start();
                while (thread.ThreadState == System.Threading.ThreadState.Running)
                    Thread.Sleep(100);
            }

            public void Run()
            {
                try
                {
                    Program.Simulator = new Simulator(ActivityPath);
                    Viewer3D viewer = new Viewer3D(Simulator);
                    viewer.Run();
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine(error.Message);
                    MessageBox.Show(error.Message);
                }
            }
        }


        class Testing
        {
            static Random Random = new Random();

            static string[] BaseFolders = new string[] { @"c:\personal\msts", @"c:\personal\mststest", @"c:\program files\microsoft games\train simulator" };
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
        }

    }// Program

}

