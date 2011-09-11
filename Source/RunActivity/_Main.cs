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
        public static string Version;         // ie "0.6.1"
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
            InitBuildRevision();

            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);

            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

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

            var settings = GetSettings(options);
            InitLogging(settings);

            // Do the action specified or write out some help.
            switch (action)
            {
                case "start":
                case "start-profile":
                    Start(settings, data);
                    break;
                case "resume":
                    Resume(settings, data);
                    break;
                case "testall":
                    TestAll(data);
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
        static void Start(UserSettings settings, string[] args)
        {
            Action start = () =>
            {
                InitSimulator(settings, args);
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
            };
            if (Debugger.IsAttached)
            {
                start();
            }
            else
            {
                try
                {
                    start();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    if (settings.ShowErrorDialogs)
                        MessageBox.Show(error.ToString(), Application.ProductName);
                }
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
            Action save = () =>
            {
                using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\SAVE.BIN", FileMode.Create, FileAccess.Write)))
                {
                    // Save some version identifiers so we can validate on load.
                    outf.Write(Version);
                    outf.Write(Build);
                    // Now save the real data...
                    outf.Write(Arguments.Length);
                    foreach (var argument in Arguments)
                        outf.Write(argument);
                    Simulator.Save(outf);
                    Viewer.Save(outf);
                    Console.WriteLine();
                    Console.WriteLine("Saved");
                    Console.WriteLine();
                }
            };
            if (Debugger.IsAttached)
            {
                save();
            }
            else
            {
                try
                {
                    save();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    if (Simulator.Settings.ShowErrorDialogs)
                        MessageBox.Show(error.ToString(), Application.ProductName);
                }
            }
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
        static void Resume(UserSettings settings, string[] args)
        {
            Action resume = () =>
            {
                var saveFile = args.Length == 0 ? UserDataFolder + "\\SAVE.BIN" : args[0];
                using (BinaryReader inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
                {
                    // Read in validation data.
                    var revision = "<unknown>";
                    var build = "<unknown>";
                    var versionOkay = false;
                    try
                    {
                        revision = inf.ReadString().Replace("\0", "");
                        build = inf.ReadString().Replace("\0", "");
                        versionOkay = (revision == Version) && (build == Build);
                    }
                    catch { }
                    if (!versionOkay)
                    {
                        if (revision.Length + build.Length > 0)
                            throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}); it was probably created by V{3} ({4}). Save files must be created by the same version of {0}.", Application.ProductName, Version, Build, revision, build));
                        throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}). Save files must be created by the same version of {0}.", Application.ProductName, Version, Build));
                    }

                    // Read in the real data...
                    var savedArgs = new string[inf.ReadInt32()];
                    for (var i = 0; i < savedArgs.Length; i++)
                        savedArgs[i] = inf.ReadString();

                    InitSimulator(settings, savedArgs, "Resume");
                    Simulator.Restore(inf);
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Initialize();
                    Viewer.Restore(inf);
                }
                Viewer.Run();
            };
            if (Debugger.IsAttached)
            {
                resume();
            }
            else
            {
                try
                {
                    resume();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    if (settings.ShowErrorDialogs)
                        MessageBox.Show(error.ToString(), Application.ProductName);
                }
            }
        }

        /// <summary>
        /// Tests OR against every activity in every route in every folder.
        /// </summary>
        static void TestAll(string[] args)
        {
            var settings = GetSettings(new[] { "ShowErrorDialogs=no", "Profiling", "ProfilingFrameCount=0" });
            InitLogging(settings);
            Action testAll = () =>
            {
                var activities = (args.Length == 0 ? Folder.GetFolders() : args.Select(a => new Folder(Path.GetFileName(a), a)))
                    .SelectMany(f => Route.GetRoutes(f))
                    .SelectMany(r => ORTS.Menu.Activity.GetActivities(r))
                    .Where(a => !(a is ExploreActivity))
                    .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var results = new bool[activities.Count];
                Action<int> run = (i) =>
                {
                    InitSimulator(settings, new[] { activities[i].FileName }, "");
                    Simulator.Start();
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Initialize();
                    Viewer.Run();
                    results[i] = true;
                    Simulator.Stop();
                };
                for (var i = 0; i < activities.Count; i++)
                {
                    if (Debugger.IsAttached)
                    {
                        run(i);
                    }
                    else
                    {
                        try
                        {
                            run(i);
                        }
                        catch (Exception error)
                        {
                            Trace.WriteLine(error);
                        }
                    }
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
            };
            if (Debugger.IsAttached)
            {
                testAll();
            }
            else
            {
                try
                {
                    testAll();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    if (settings.ShowErrorDialogs)
                        MessageBox.Show(error.ToString(), Application.ProductName);
                }
            }
        }

        static void InitBuildRevision()
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

        static UserSettings GetSettings(IEnumerable<string> options)
        {
            return new UserSettings(RegistryKey, options);
        }

        static void InitLogging(UserSettings settings)
        {
            var logFileName = "";
            if (settings.Logging)
            {
                if ((settings.LoggingPath.Length > 0) && Directory.Exists(settings.LoggingPath))
                {
                    var fileName = settings.LoggingFilename;
                    try
                    {
                        fileName = String.Format(fileName, Application.ProductName, Version.Length > 0 ? Version : Build, Version, Build, DateTime.Now);
                    }
                    catch { }
                    foreach (var ch in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(ch, '.');

                    logFileName = Path.Combine(settings.LoggingPath, fileName);
                    // Ensure we start with an empty file.
                    File.Delete(logFileName);
                    // Make Console.Out go to the log file AND the output stream.
                    Console.SetOut(new FileTeeLogger(logFileName, Console.Out));
                    // Make Console.Error go to the new Console.Out.
                    Console.SetError(Console.Out);
                }
            }

            // Captures Trace.Trace* calls and others and formats.
            var traceListener = new ORTraceListener(Console.Out, !settings.Logging);
            traceListener.TraceOutputOptions = TraceOptions.Callstack;
            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(traceListener);

            Console.WriteLine("{0} is starting...", Application.ProductName);
            Console.WriteLine();
            Console.WriteLine("Version    = {0}", Version.Length > 0 ? Version : "<none>");
            Console.WriteLine("Build      = {0}", Build);
            if (logFileName.Length > 0)
                Console.WriteLine("Logfile    = {0}", logFileName);
            LogSeparator();
            settings.Log();
            LogSeparator();
            if (!settings.Logging)
            {
                Console.WriteLine("Logging is disabled, only fatal errors will appear here.");
                LogSeparator();
            }
        }

        static void InitSimulator(UserSettings settings, string[] args)
        {
            InitSimulator(settings, args, "");
        }

        static void InitSimulator(UserSettings settings, string[] args, string mode)
        {
            Console.WriteLine(mode.Length > 0 ? "Mode       = {0} {1}" : "Mode       = {1}", mode, args.Length == 1 ? "Activity" : "Explore");
            if (args.Length == 1)
            {
                Console.WriteLine("Activity   = {0}", args[0]);
            }
            else
            {
                Console.WriteLine("Path       = {0}", args[0]);
                Console.WriteLine("Consist    = {0}", args[1]);
                Console.WriteLine("Time       = {0}", args[2]);
                Console.WriteLine("Season     = {0}", args[3]);
                Console.WriteLine("Weather    = {0}", args[4]);
            }
            LogSeparator();

            Arguments = args;
            Simulator = new Simulator(settings, args[0]);
            if (args.Length == 1)
                Simulator.SetActivity(args[0]);
            else
                Simulator.SetExplore(args[0], args[1], args[2], args[3], args[4]);
        }

        static void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }
    }
}
