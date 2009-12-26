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

        static Simulator Simulator; 

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
                string warningLogFileName = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\ORTS Log.txt";
                File.Delete(warningLogFileName);
                ErrorLogger errorLogger = new ErrorLogger(warningLogFileName);
                TraceListener traceListener = new System.Diagnostics.TextWriterTraceListener(errorLogger);
                System.Diagnostics.Debug.Listeners.Insert(0, traceListener);
                System.Diagnostics.Trace.Listeners.Insert(0, traceListener);
                Console.SetError(errorLogger);
                Console.SetOut(new Logger(warningLogFileName));
                Console.WriteLine("ORTS V " + Version);
                Console.WriteLine();
                Console.Error.WriteLine("Build = " + Application.ProductVersion);
                Console.WriteLine();
            }

            try
            {
                ActivityPath = args[0];
                Console.WriteLine("Activity = " + ActivityPath);
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------");

                Simulator = new Simulator(ActivityPath);
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

    }
}

