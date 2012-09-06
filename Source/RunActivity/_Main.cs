/// <summary>
/// This application runs an activity.  After loading the activity, main
/// sets up the simulator engine and connects a 3D viewer.
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

#define DEBUG_VIEWER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Debugging;
using ORTS.MultiPlayer;

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

		//for Multiplayer
		public static Server Server;
		public static ClientComm Client;
		public static string UserName;
		public static string Code;
		public static int NumOfTrains = 0;

        static Viewer3D Viewer;
        static ORTraceListener ORTraceListener;
#if DEBUG_VIEWER
        public static Debugging.DispatchViewer DebugViewer;
        public static bool DebugViewerEnabled = false;
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
            var actions = new[] { "start", "resume", "test", "testall" };
            foreach (var possibleAction in actions)
                if (args.Contains("-" + possibleAction) || args.Contains("/" + possibleAction, StringComparer.OrdinalIgnoreCase))
                    action = possibleAction;

            // Collect all non-action options.
            var options = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")) && !actions.Contains(a.Substring(1))).Select(a => a.Substring(1));

            // Collect all non-options as data.
            var data = args.Where(a => !a.StartsWith("-") && !a.StartsWith("/")).ToArray();

            // No action, check for data; for now assume any data is good data.
            if ((action.Length == 0) && (data.Length > 0))
                action = "start";

            var settings = GetSettings(options);
            InputSettings.Initialize(options);

            // Do the action specified or write out some help.
            switch (action)
            {
                case "start":
                case "start-profile":
                    InitLogging(settings);
                    Start(settings, data);
                    break;
                case "resume":
                    InitLogging(settings);
                    Resume(settings, data);
                    break;
                case "test":
                    // Any log file is deleted by Menu.exe
                    InitLogging(settings, true);
                    // set Exit code to be returned to Menu.exe 
                    Environment.ExitCode = Test(settings, data);
                    break;
                case "testall":
                    InitLogging(settings);
                    TestAll(data);
                    break;
                default:
                    Console.WriteLine("Supply missing activity file name");
                    Console.WriteLine("   i.e.: RunActivity \"C:\\Program Files\\Microsoft Games\\Train Simulator\\ROUTES\\USA1\\ACTIVITIES\\xxx.act\"");
                    Console.WriteLine();
                    Console.WriteLine("or launch the program OpenRails.exe and select from the menu.");
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

				if (Client != null)
				{
					Client.Send((new MSGPlayer(Program.UserName, Program.Code, Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.Trains[0], 0, Program.Simulator.Settings.AvatarURL)).ToString());
				}

#if DEBUG_VIEWER
                if (MPManager.IsMultiPlayer() || Viewer.Settings.ViewDispatcher)
                {
                    // prepare to show debug output in a separate window
                    DebugViewer = new DispatchViewer(Simulator, Viewer);
                    DebugViewer.Show();
                    DebugViewer.Hide();
                    Viewer.DebugViewerEnabled = false;
                }
#endif

                Viewer.Run(null);

                Simulator.Stop();

#if DEBUG_VIEWER
				if (MPManager.IsMultiPlayer() || Viewer.Settings.ViewDispatcher) DebugViewer.Dispose();
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
        /// Save files are stored in the user's local program storage:
        /// e.g. "C:\Users\Wayne\AppData\Roaming\ORTS\<activity file name> <date_and_time>.save"
        /// or
        /// e.g. "C:\Users\Wayne\AppData\Roaming\ORTS\<route folder name> <date_and_time>.save"
        /// </summary>
        public static void Save()
        {
            if (MPManager.IsMultiPlayer()) return; //no save for multiplayer sessions yet
            Action save = () =>
            {
                // Prefix with the activity filename so that, when resuming from the Menu.exe, we can quickly find those Saves 
                // that are likely to match the previously chosen route and activity.
                // Append the current date and time, so that each file is unique.
                // This is the "sortable" date format, ISO 8601, but with "." in place of the ":" which are not valid in filenames.
                var fileStem = String.Format("{0} {1:yyyy'-'MM'-'dd HH'.'mm'.'ss}", Simulator.Activity != null ? Simulator.ActivityFileName : Simulator.RoutePathName, DateTime.Now);

                using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\" + fileStem + ".save", FileMode.Create, FileAccess.Write)))
                {
                    // Save some version identifiers so we can validate on load.
                    outf.Write(Version);
                    outf.Write(Build);

                    // Save heading data used in Menu.exe
                    outf.Write(Simulator.RouteName);
                    outf.Write(Simulator.PathName);
                    outf.Write((int)Simulator.GameTime);
                    outf.Write(DateTime.Now.ToBinary());
                    // TODO: This needs to be the player's train and/or viewer's selected train.
                    outf.Write(Simulator.Trains[0].FrontTDBTraveller.TileX + (Simulator.Trains[0].FrontTDBTraveller.X / 2048));
                    outf.Write(Simulator.Trains[0].FrontTDBTraveller.TileZ + (Simulator.Trains[0].FrontTDBTraveller.Z / 2048));
                    outf.Write(Simulator.InitialTileX);
                    outf.Write(Simulator.InitialTileZ);

                    // Now save the data used by RunActivity.exe
                    outf.Write(Arguments.Length);
                    foreach (var argument in Arguments)
                        outf.Write(argument);
                    Simulator.Save(outf);
                    Viewer.Save(outf, fileStem);
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
                // If "-resume" also specifies a save file then use it else use most recently changed *.save E.g.:
                // RunActivity.exe -resume "yard_two 2012-03-20 22.07.36"
                var saveFile = "";
                if (args.Length > 0)
                {
                    saveFile = args[0];
                    if (!saveFile.EndsWith(".save"))
                        saveFile += ".save";
                    if (!Path.IsPathRooted(saveFile))
                        saveFile = Path.Combine(Program.UserDataFolder, saveFile);
                }
                else
                {
                    saveFile = GetMostRecentSave();
                }
                Console.WriteLine("Save File  = {0}", saveFile);
                using (var inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
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
                        if (Debugger.IsAttached)
                        {
                            // Only if debugging, then allow user to continue.
                            // Resuming from saved activities is useful in debugging.
                            // (To resume from the latest save, set RunActivity > Properties > Debug > Command line arguments = "-resume")
                            Trace.Assert(versionOkay, String.Format("{0} save file is not compatible with V{1} ({2}). Save files must be created by the same version of {0}. Continue at your own risk!", Application.ProductName, Version, Build));
                        }
                        else
                        {
                            if (revision.Length + build.Length > 0)
                                throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}); it was probably created by V{3} ({4}). Save files must be created by the same version of {0}.", Application.ProductName, Version, Build, revision, build));
                            throw new InvalidDataException(String.Format("{0} save file is not compatible with V{1} ({2}). Save files must be created by the same version of {0}.", Application.ProductName, Version, Build));
                        }
                    }

                    // Skip the heading data used in Menu.exe
                    inf.ReadString(); // Route name
                    var pathName = inf.ReadString(); // Path name
                    inf.ReadInt32(); // Game time
                    inf.ReadInt64(); // Real time
                    inf.ReadSingle(); // Player TileX
                    inf.ReadSingle(); // Player TileZ
                    var initialTileX = inf.ReadSingle(); // Initial TileX
                    var initialTileZ = inf.ReadSingle(); // Initial TileZ

                    // Read in the real data...
                    var savedArgs = new string[inf.ReadInt32()];
                    for (var i = 0; i < savedArgs.Length; i++)
                        savedArgs[i] = inf.ReadString();

                    InitSimulator(settings, savedArgs, "Resume");
                    Simulator.Restore(inf, pathName, initialTileX, initialTileZ);
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Run(inf);
                }
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

        static string GetMostRecentSave()
        {
            var directory = new DirectoryInfo(UserDataFolder);
            var file = directory.GetFiles("*.save")
             .OrderByDescending(f => f.LastWriteTime)
             .First();
            if (file == null)
            {
                return "resume not found";
            }
            else
            {
                return file.FullName;
            }
        }

        /// <summary>
        /// Tests OR against every activity in every route in every folder.
        /// <CJ comment>
        /// From v974 (and probably much before) this method fails on the second activity, raising a fatal error InvalidOperationException in MSTSLocomotive.cs:DisassembleFrames()
        /// (Tried on just the JAPAN2 activities and on just the USA2 activities.)
        /// Superseded by the Test() method.
        /// </CJ>
        /// </summary>
        static void TestAll(string[] args)
        {
            var settings = GetSettings(new[] { "ShowErrorDialogs=no", "Profiling", "ProfilingFrameCount=0" });
            InitLogging(settings);
            Action testAll = () =>
            {
                var activities = (args.Length == 0 ? ORTS.Menu.Folder.GetFolders() : args.Select(a => new ORTS.Menu.Folder(Path.GetFileName(a), a)))
                    .SelectMany(f => ORTS.Menu.Route.GetRoutes(f))
                    .SelectMany(r => ORTS.Menu.Activity.GetActivities(r))
                    .Where(a => !(a is ORTS.Menu.ExploreActivity))
                    .OrderBy(a => a.FilePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var results = new bool[activities.Count];
                Action<int> run = (i) =>
                {
                    InitSimulator(settings, new[] { activities[i].FilePath}, "");
                    Simulator.Start();
                    Viewer = new Viewer3D(Simulator);
                    Viewer.Run(null);

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
                    Console.WriteLine("{0,-4}  {1}", results[i] ? "PASS" : "fail", activities[i].FilePath);
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

        /// <summary>
        /// Tests that RunActivity.exe can launch a specific activity or explore.
        /// </summary>
        public static int Test(UserSettings settings, string[] args)
        {
            var passed = false;
            var startTime = DateTime.Now;
            var loadTime = 0d;
            try
            {
                InitSimulator(settings, args, "Test");
                Simulator.Start();
                Viewer = new Viewer3D(Simulator);
                Viewer.Run(null);
                Simulator.Stop();
                loadTime = (DateTime.Now - startTime).TotalSeconds - Viewer.RealTime;
                passed = true;
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (settings.ShowErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
            }
            ExportTestSummary(settings, args, passed, loadTime);
            return passed ? 0 : 1;
        }

        static void ExportTestSummary(UserSettings settings, string[] args, bool passed, double loadTime)
        {
            // Append to CSV file in format suitable for Excel
            var summaryFileName = Path.Combine(Program.UserDataFolder, "TestingSummary.csv");
            // Could fail if already opened by Excel
            try
            {
                using (var writer = File.AppendText(summaryFileName))
                {
                    // Route, Activity, Passed, Errors, Warnings, Infos, Load Time, Frame Rate
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6:F1},{7:F1}",
                        Simulator != null && Simulator.TRK != null && Simulator.TRK.Tr_RouteFile != null ? Simulator.TRK.Tr_RouteFile.Name.Replace(",", ";") : "",
                        Simulator != null && Simulator.Activity != null && Simulator.Activity.Tr_Activity != null && Simulator.Activity.Tr_Activity.Tr_Activity_Header != null ? Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name.Replace(",", ";") : "",
                        passed ? "Yes" : "No",
                        ORTraceListener != null ? ORTraceListener.Counts[0] + ORTraceListener.Counts[1] : 0,
                        ORTraceListener != null ? ORTraceListener.Counts[2] : 0,
                        ORTraceListener != null ? ORTraceListener.Counts[3] : 0,
                        loadTime,
                        Viewer != null && Viewer.RenderProcess != null ? Viewer.RenderProcess.FrameRate.SmoothedValue : 0);
                }
            }
            catch { } // Ignore any errors
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

                    Build = Application.ProductVersion; // from assembly
                    Build = Build + " " + f.ReadLine(); // date
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
            InitLogging(settings, false);
        }

        static void InitLogging(UserSettings settings, bool appendLog)
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
                    if (!appendLog)
                        File.Delete(logFileName);
                    // Make Console.Out go to the log file AND the output stream.
                    Console.SetOut(new FileTeeLogger(logFileName, Console.Out));
                    // Make Console.Error go to the new Console.Out.
                    Console.SetError(Console.Out);
                }
            }

            // Captures Trace.Trace* calls and others and formats.
            ORTraceListener = new ORTraceListener(Console.Out, !settings.Logging);
            ORTraceListener.TraceOutputOptions = TraceOptions.Callstack;
            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(ORTraceListener);

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
            else if (args.Length == 3)
            {
                Console.WriteLine("Activity   = {0}", args[0]);
            }
            else if (args.Length == 4)
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
            if (settings.MultiplayerServer || settings.MultiplayerClient)
            {
                if (settings.MultiplayerServer)
                    Console.WriteLine("Multiplayer Server");
                else
                    Console.WriteLine("Multiplayer Client");
                Console.WriteLine("User       = {0}", settings.Multiplayer_User);
                if (settings.MultiplayerClient)
                    Console.WriteLine("Host       = {0}", settings.Multiplayer_Host);
                Console.WriteLine("Port       = {0}", settings.Multiplayer_Port);
                LogSeparator();
            }

            Arguments = args;
            Simulator = new Simulator(settings, args[0]);
            if (args.Length == 1)
                Simulator.SetActivity(args[0]);
            else if (args.Length == 5)
                Simulator.SetExplore(args[0], args[1], args[2], args[3], args[4]);

            if (settings.MultiplayerServer)
            {
                try
                {
                    Server = new Server(settings.Multiplayer_User + " 1234", settings.Multiplayer_Port);
                    UserName = Server.UserName;
                    Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
                        "Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
                    Code = Server.Code;
                    MPManager.Instance().MPUpdateInterval = settings.Multiplayer_UpdateInterval;
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    Console.WriteLine("Connection error - will play in single mode.");
                    Server = null;
                }
            }

            if (settings.MultiplayerClient)
            {
                try
                {
                    Client = new ClientComm(settings.Multiplayer_Host, settings.Multiplayer_Port, settings.Multiplayer_User + " 1234");
                    UserName = Client.UserName;
                    Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
                        "Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
                    Code = Client.Code;
                    MPManager.Instance().MPUpdateInterval = settings.Multiplayer_UpdateInterval;
				}
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    Console.WriteLine("Connection error - will play in single mode.");
                    Client = null;
                }
            }
        }

        static void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }
    }
}
