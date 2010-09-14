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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using MSTS;

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
        public static bool TrainLightsEnabled = false;  // control parsing and displaying of train lights
        public static int BrakePipeChargingRatePSIpS = 21; // temporary option to control player train brakes
        public static bool GraduatedRelease = false;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
		static void Main(string[] args)
		{
			SetBuildRevision();

			UserDataFolder = Path.GetDirectoryName(Path.GetDirectoryName(Application.UserAppDataPath));

			RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

			EnableLogging();

			try
			{
				RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
				if (RK != null)
				{
					TrainLightsEnabled = (1 == (int)RK.GetValue("TrainLights", 0));
					BrakePipeChargingRatePSIpS = (int)RK.GetValue("BrakePipeChargingRate", (int)21);
					GraduatedRelease = (1 == (int)RK.GetValue("GraduatedRelease", 0));
                    if ((1 == (int)RK.GetValue("MSTSBINSound", 0)))
                        EventID.SetMSTSBINCompatible();
				}
			}
			catch (Exception error)
			{
				Trace.WriteLine(error);
			}

			// Look for an action to perform.
			var action = "";
			var actions = new[] { "start", "resume", "random", "runtest" };
			foreach (var possibleAction in actions)
				if (args.Contains("-" + possibleAction) || args.Contains("/" + possibleAction))
					action = possibleAction;

			// Collect all non-action settings.
			var settings = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")) && !actions.Contains(a.Substring(1))).Select(a => a.Substring(1));

			// Collect all non-options as data.
			var data = args.Where(a => !a.StartsWith("-") && !a.StartsWith("/")).ToArray();

			// No action, check for data; for now assume any data is good data.
			if ((action.Length == 0) && (data.Length > 0))
				action = "start";

			// Do the action specified or write out some help.
			switch (action)
			{
				case "start":
				case "start-profile":
					Start(settings, data);
					break;
				case "resume":
					Resume(settings);
					break;
				case "random":
					Start(settings, new[] { Testing.GetRandomActivity() });
					break;
				case "runtest":
					Testing.Test();
					break;
				default:
					Console.WriteLine("Missing activity file name");
					Console.WriteLine("   ie RunActivity \"c:\\program files\\microsoft games\\train simulator\\routes\\usa1\\activites\\xxx.act\"");
					Console.WriteLine();
					Console.WriteLine("Or launch the OpenRails program and select from the menu.");
					Console.ReadKey();
					break;
			}
		}


        /// <summary>
        /// Run the specified activity from the beginning.
        /// </summary>
		static void Start(IEnumerable<string> settings, string[] args)
		{
			try
			{
				ActivityPath = args[0];

				if (args.Length == 1)
					Console.WriteLine("Starting Activity = " + args[0]);
				else
					Console.WriteLine("Starting Explore = " + args[0] + " " + args[1]);
				Console.WriteLine();
				Console.WriteLine("------------------------------------------------");

				Simulator = new Simulator(args[0]);
				if (args.Length == 1)
					Simulator.SetActivity(args[0]);
				else
					Simulator.SetExplore(args[0], args[1], args[2], args[3], args[4]);
				Simulator.Start();
				Viewer = new Viewer3D(Simulator);
				if (!settings.Contains("skip-user-settings"))
					Viewer.LoadUserSettings();
				SetViewerSettings(Viewer, settings);
				Viewer.Initialize();
				Viewer.Run();
			}
			catch (Exception error)
			{
				Trace.WriteLine(error);
				MessageBox.Show(error.ToString());
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
                    outf.Write(Simulator.ExploreConFile != null);
                    if (Simulator.ExploreConFile != null)
                        outf.Write(Simulator.ExploreConFile);
                    Simulator.Save(outf);
                    Viewer.Save(outf);
                    Console.WriteLine("\nSaved");
                }
            }
            catch (Exception error)
            {
				Trace.WriteLine(error);
                MessageBox.Show(error.ToString());
            }
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
		static void Resume(IEnumerable<string> settings)
        {
            try
            {
                using( BinaryReader inf = new BinaryReader( new FileStream( UserDataFolder + "\\SAVE.BIN", FileMode.Open, FileAccess.Read )) )
                {
                    ActivityPath = inf.ReadString();
                    bool explore = inf.ReadBoolean();
                    string conFile = null;
                    if (explore)
                        conFile = inf.ReadString();

                    Console.WriteLine("Restoring Activity = " + ActivityPath);
                    Console.WriteLine();
                    Console.WriteLine("------------------------------------------------");

                    Simulator = new Simulator(ActivityPath);
                    if (explore)
                        Simulator.SetExplore(ActivityPath, conFile, "12", "0", "0");
                    else
                        Simulator.SetActivity(ActivityPath);
                    Simulator.Restore(inf);
                    Viewer = new Viewer3D(Simulator);
					if (!settings.Contains("skip-user-settings"))
						Viewer.LoadUserSettings();
					SetViewerSettings(Viewer, settings);
					Viewer.Initialize();
					Viewer.Restore(inf);
                }
                Viewer.Run();
            }
            catch (Exception error)
            {
				Trace.WriteLine(error);
                MessageBox.Show(error.ToString());
            }
        }


        /// <summary>
        /// Check the registry and return true if the OpenRailsLog.TXT
        /// file should be created.
        /// </summary>
        static bool IsWarningsOn()
        {
            // TODO Read from Registry
            return true;
        }


        /// <summary>
        /// Set up to capture all console and error I/O into a  log file.
        /// </summary>
        static void EnableLogging()
        {
			if (IsWarningsOn())
			{
				string logFileName = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\OpenRailsLog.txt";
				File.Delete(logFileName);

				// Make Console.Out go to the log file AND the output stream.
				Console.SetOut(new FileTeeLogger(logFileName, Console.Out));
				// Make Console.Error go to the new Console.Out.
				Console.SetError(Console.Out);
			}

			// Captures Trace.Trace* calls and others and formats.
			var traceListener = new ORTraceListener(Console.Out);
			traceListener.TraceOutputOptions = TraceOptions.Callstack;
			// Trace.Listeners and Debug.Listeners are the same list.
			Trace.Listeners.Add(traceListener);

			Console.WriteLine("SVN V = " + Revision);
			Console.WriteLine("BUILD = " + Build);
			Console.WriteLine();
        }

		static void SetViewerSettings(Viewer3D viewer, IEnumerable<string> settings)
		{
			foreach (var setting in settings)
			{
				var data = setting.ToLowerInvariant().Split('=', ':');
				if (Enum.GetNames(typeof(BoolSettings)).Contains(data[0], StringComparer.OrdinalIgnoreCase))
				{
					viewer.SettingsBool[(int)Enum.Parse(typeof(BoolSettings), data[0], true)] = (data.Length == 1) || new[] { "true", "yes", "on", "1" }.Contains(data[1]);
				}
				else if (Enum.GetNames(typeof(IntSettings)).Contains(data[0], StringComparer.OrdinalIgnoreCase))
				{
					if (data.Length < 2)
						Console.WriteLine("Option '" + setting + "' missing value.");
					else
						viewer.SettingsInt[(int)Enum.Parse(typeof(IntSettings), data[0], true)] = int.Parse(data[1]);
				}
				else if (data[0] != "skip-user-settings")
				{
					Console.WriteLine("Option '" + data[0] + "' is unknown.");
				}
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
                    catch (Exception error)
                    {
                        Trace.WriteLine(filename);
						Trace.WriteLine(error);
                    }

            } // TestAll
        }

    }// Program

}

