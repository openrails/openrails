///// <summary>
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

#if DEBUG
//#define DEBUG_VIEWER
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ORTS.Menu;

namespace ORTS
{
    static class Program
    {
        public static string[] Arguments;
        public static string Revision;        // ie 078
        public static string Build;           // ie "0.0.3661.19322 Sat 01/09/2010  10:44 AM"
        public static string RegistryKey;     // ie @"SOFTWARE\OpenRails\ORTS"
        public static string UserDataFolder;  // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails"
        public static Random Random = new Random();  // primary random number generator used throughout the program
        public static Simulator Simulator;
        private static Viewer3D Viewer;
#if DEBUG_VIEWER
		private static DebugViewerForm DebugViewer;
#endif

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            SetBuildRevision();

            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);

            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

            EnableLogging();

            // Look for an action to perform.
            var action = "";
            var actions = new[] { "start", "resume", "testall" };
            foreach (var possibleAction in actions)
                if (args.Contains("-" + possibleAction) || args.Contains("/" + possibleAction))
                    action = possibleAction;

            // Collect all non-action options.
            var options = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")) && !actions.Contains(a.Substring(1))).Select(a => a.Substring(1));

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
                    Start(options, data);
                    break;
                case "resume":
                    Resume(options);
                    break;
                case "testall":
                    TestAll(options, data);
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
        static void Start(IEnumerable<string> options, string[] args)
        {
            var showErrorDialogs = true;
#if !CRASH_ON_ERRORS
            try
            {
#endif
                var settings = GetSettings(false, options, args, ref showErrorDialogs);
                CreateSimulator(settings);
                Simulator.Start();
                Viewer = new Viewer3D(Simulator);
                Viewer.Initialize();

#if DEBUG_VIEWER
				// prepare to show debug output in a separate window
				DebugViewer = new DebugViewerForm(Simulator, Viewer);
				DebugViewer.Show();
#endif

                Viewer.Run();

                Simulator.Stop();

#if DEBUG_VIEWER
				DebugViewer.Dispose();
#endif
#if !CRASH_ON_ERRORS
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (showErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
            }
#endif
        }

        /// <summary>
        /// Save the current game state for later resume.
        /// Currently only supports one save, in a SAVE.BIN file in 
        /// the users local program storage, 
        /// ie.  "C:\\Users\\Wayne\\AppData\\Roaming\\ORTS\\SAVE.BIN"
        /// </summary>
        public static void Save()
        {
            var showErrorDialogs = Simulator.Settings.ShowErrorDialogs;
#if !CRASH_ON_ERRORS
            try
            {
#endif
                using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\SAVE.BIN", FileMode.Create, FileAccess.Write)))
                {
                    // Save some version identifiers so we can validate on load.
                    outf.Write(Revision);
                    outf.Write(Build);
                    // Now save the real data...
                    outf.Write(Arguments.Length);
                    foreach (var argument in Arguments)
                        outf.Write(argument);
                    Simulator.Save(outf);
                    Viewer.Save(outf);
                    Console.WriteLine("\nSaved");
                }
#if !CRASH_ON_ERRORS
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (showErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
            }
#endif
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
        static void Resume(IEnumerable<string> options)
        {
            var showErrorDialogs = true;
#if !CRASH_ON_ERRORS
            try
            {
#endif
                using (BinaryReader inf = new BinaryReader(new FileStream(UserDataFolder + "\\SAVE.BIN", FileMode.Open, FileAccess.Read)))
                {
                    // Read in validation data.
                    var revision = "<unknown>";
                    var build = "<unknown>";
                    var versionOkay = false;
                    try
                    {
                        revision = inf.ReadString().Replace("\0", "");
                        build = inf.ReadString().Replace("\0", "");
                        versionOkay = (revision == Revision) && (build == Build);
                    }
                    catch { }
                    if (!versionOkay)
                    {
                        if (revision.Length + build.Length > 0)
                            throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}); it was probably created by V{3} ({4}). Save files must be created by the same version of {0}.", Application.ProductName, Revision, Build, revision, build));
                        throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}). Save files must be created by the same version of {0}.", Application.ProductName, Revision, Build));
                    }

                    // Read in the real data...
                    var args = new string[inf.ReadInt32()];
                    for (var i = 0; i < args.Length; i++)
                        args[i] = inf.ReadString();

                    var settings = GetSettings(true, options, args, ref showErrorDialogs);
                    CreateSimulator(settings);
                    Simulator.Restore(inf);
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Initialize();
                    Viewer.Restore(inf);
                }
                Viewer.Run();
#if !CRASH_ON_ERRORS
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (showErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
            }
#endif
        }

        /// <summary>
        /// Tests OR against every activity in every route in every folder.
        /// </summary>
        static void TestAll(IEnumerable<string> options, string[] args)
        {
            var showErrorDialogs = true;
#if !CRASH_ON_ERRORS
            try
            {
#endif
                options = new List<string>(options) { "ShowErrorDialogs=no", "Profiling", "ProfilingFrameCount=0" };
                var activities = (args.Length == 0 ? Folder.GetFolders() : args.Select(a => new Folder(Path.GetFileName(a), a)))
                    .SelectMany(f => Route.GetRoutes(f))
                    .SelectMany(r => ORTS.Menu.Activity.GetActivities(r))
                    .Where(a => !(a is ExploreActivity))
                    .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var results = new bool[activities.Count];
                for (var i = 0; i < activities.Count; i++)
                {
#if !CRASH_ON_ERRORS
                    try
                    {
#endif
                        var settings = GetSettings(false, options, new[] { activities[i].FileName }, ref showErrorDialogs);
                        CreateSimulator(settings);
                        Simulator.Start();
                        Viewer = new Viewer3D(Simulator);
                        Viewer.Initialize();
                        Viewer.Run();
                        results[i] = true;
                        Simulator.Stop();
#if !CRASH_ON_ERRORS
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(error);
                    }
#endif
                    Console.WriteLine();
                    Console.WriteLine();

                    // Force a cleanup.
                    Viewer = null;
                    Simulator = null;
                    GC.Collect();
                }

                Console.WriteLine();
                for (var i = 0; i < activities.Count; i++)
                {
                    Console.WriteLine("{0,-4}  {1}", results[i] ? "PASS" : "fail", activities[i].FileName);
                }
                Console.WriteLine();
                Console.WriteLine("Tested {0} activities; {1} passed, {2} failed.", results.Length, results.Count(r => r), results.Count(r => !r));
#if !CRASH_ON_ERRORS
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (showErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
            }
#endif
        }

        static UserSettings GetSettings(bool resume, IEnumerable<string> options, string[] args, ref bool showErrorDialogs)
        {
            Arguments = args;

            Console.WriteLine("Mode:     {0}{1}", resume ? "Resume " : "", args.Length == 1 ? "Activity" : "Explore");
            if (args.Length == 1)
            {
                Console.WriteLine("Activity: {0}", args[0]);
            }
            else
            {
                Console.WriteLine("Path:     {0}", args[0]);
                Console.WriteLine("Consist:  {0}", args[1]);
                Console.WriteLine("Time:     {0}", args[2]);
                Console.WriteLine("Season:   {0}", args[3]);
                Console.WriteLine("Weather:  {0}", args[4]);
            }
            Console.WriteLine();
            var settings = new UserSettings(RegistryKey, options);
            if (settings.MSTSBINSound)
                EventID.SetMSTSBINCompatible();
            showErrorDialogs = settings.ShowErrorDialogs;
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine();
            return settings;
        }

        static void CreateSimulator(UserSettings settings)
        {
            Simulator = new Simulator(settings, Arguments[0]);
            if (Arguments.Length == 1)
                Simulator.SetActivity(Arguments[0]);
            else
                Simulator.SetExplore(Arguments[0], Arguments[1], Arguments[2], Arguments[3], Arguments[4]);
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

            Console.WriteLine("{0} is starting...", Application.ProductName);
            Console.WriteLine();
            Console.WriteLine("Version: {0}", Revision);
            Console.WriteLine("Build:   {0}", Build);
            Console.WriteLine();
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
    }
}
