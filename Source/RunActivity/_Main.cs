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
#define DEBUG_VIEWER
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Debugging;
using ORTS.Menu;
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

        private static Viewer3D Viewer;
        public static int[] ErrorCount = new int[Enum.GetNames(typeof(TraceEventType)).Length];
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
                    InitLogging(settings, false);
                    // set Exit code to be returned to Menu.exe 
                    System.Environment.ExitCode = Test(settings, data);
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
					Client.Send((new MSGPlayer(Program.UserName, Program.Code, Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.Trains[0], 0)).ToString());
				}

#if DEBUG_VIEWER
                if (MPManager.IsMultiPlayer())
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
                if (MPManager.IsMultiPlayer()) DebugViewer.Dispose();
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
        /// e.g. "C:\\Users\\Wayne\\AppData\\Roaming\\ORTS\\<activity file name> <date_and_time>.save"
        /// or
        /// e.g. "C:\\Users\\Wayne\\AppData\\Roaming\\ORTS\\<route folder name> <date_and_time>.save"
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
                string prefix;
                // If there is an activity:
                if (Arguments.Length == 1)
                {
                    // Extract the name of the activity file
                    prefix = Path.GetFileNameWithoutExtension(Arguments[0]);
                }
                else
                {
                    // Extract the name of the route folder instead
                    Regex r1 = new Regex(@"(\\ROUTES\\)(.+)(\\PATHS)");
                    Match match = r1.Match(Arguments[0]);   // e.g. "D:\MSTS\ROUTES\USA1\PATHS\local service (traffic).pat"
                    prefix =
                        match.Success ?
                        match.Groups[2].Value  // Extract 2nd group (1)(2)(3), e.g. "USA1"
                        : "unknown route";
                }
                string fileStem = String.Format("{0} {1:yyyy'-'MM'-'dd HH'.'mm'.'ss}", prefix, System.DateTime.Now);

                using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\" + fileStem + ".save", FileMode.Create, FileAccess.Write)))
                {
                    // Save some version identifiers so we can validate on load.
                    outf.Write(Version);
                    outf.Write(Build);

                    // Save heading data used in Menu.exe
                    outf.Write(Simulator.RouteName);
                    outf.Write(Arguments.Length);
                    if (Arguments.Length < 2)
                    {     // save Activity 
                        outf.Write(Path.GetFileNameWithoutExtension(Arguments[0]));   // Activity filename
                    }
                    else
                    {                          // save Explore details
                        outf.Write(Path.GetFileNameWithoutExtension(Arguments[0]));  // Path filename
                        outf.Write(Path.GetFileNameWithoutExtension(Arguments[1]));  // Consist filename
                    }

                    if (Simulator.PathDescription == null) { Simulator.PathDescription = "<unknown>"; }
                    outf.Write(Simulator.PathDescription);
                    outf.Write((int)Simulator.GameTime);                              // Time elapsed in game (secs)
                    outf.Write(System.DateTime.Now.ToString("ddd dd-MM-yy HH:mm"));   // Date and time in real world
                    // Calculate position of player's train in fractions of a 2048 metre tile
                    float currentTileX = Simulator.Trains[0].FrontTDBTraveller.TileX + (Simulator.Trains[0].FrontTDBTraveller.X / 2048);
                    float currentTileZ = Simulator.Trains[0].FrontTDBTraveller.TileZ + (Simulator.Trains[0].FrontTDBTraveller.Z / 2048);
                    outf.Write(currentTileX);  // Current location of player train
                    outf.Write(currentTileZ);  // Current location of player train
                    outf.Write(Simulator.InitialTileX);  // Initial location of player train
                    outf.Write(Simulator.InitialTileZ);  // Initial location of player train

                    // Now save the real data used by RunActivity.exe
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
                    var temp = inf.ReadString();    // Route name
                    var argumentsCount = inf.ReadInt32();
                    if (argumentsCount < 2)
                    {
                        temp = inf.ReadString();    // Activity filename
                    }
                    else
                    {
                        temp = inf.ReadString();    // Path filename
                        temp = inf.ReadString();    // Consist filename
                    }
                    var simulatorPathDescription = inf.ReadString();
                    var tempInt = inf.ReadInt32();          // Time elapsed in game (secs)
                    temp = inf.ReadString();                // Date and time in real world
                    var tempFloat = inf.ReadSingle();       // Current location of player train TileX
                    tempFloat = inf.ReadSingle();           // Current location of player train TileZ

                    // Read initial position and pass to Simulator so it can be written out if another save is made.
                    var initialTileX = inf.ReadSingle();  // Initial location of player train TileX
                    var initialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

                    // Read in the real data...
                    var savedArgs = new string[inf.ReadInt32()];
                    for (var i = 0; i < savedArgs.Length; i++)
                        savedArgs[i] = inf.ReadString();

                    InitSimulator(settings, savedArgs, "Resume");
                    Simulator.Restore(inf, simulatorPathDescription, initialTileX, initialTileZ);
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

        /// <summary>
        /// Tests that RunActivity.exe can launch a specific activity or explore.
        /// </summary>
        public static int Test(UserSettings settings, string[] args)
        {
            int fatalErrors = 0;
            DateTime StartTime = DateTime.Now;
            DateTime EndTime = DateTime.Now;
            try
            {
                InitSimulator(settings, args);
                StartTime = DateTime.Now;
                Simulator.Start();
                Viewer = new Viewer3D(Simulator);
                Viewer.Run(null);
                Simulator.Stop();
                EndTime = DateTime.Now;
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                if (settings.ShowErrorDialogs)
                    MessageBox.Show(error.ToString(), Application.ProductName);
                // Set a positive exit code so Menu.exe can pick it up.
                fatalErrors++;
            }
            ExportTestSummary(fatalErrors, settings, args, EndTime - StartTime);
            return fatalErrors;
        }

        static void ExportTestSummary(int fatalErrors, UserSettings settings, string[] args, TimeSpan duration)
        {
            // Append to CSV file in format suitable for Excel
            string summaryFileName = Path.Combine(Program.UserDataFolder, "TestSummary.csv");
            // Could fail if already opened by Excel
            try
            {
                using (StreamWriter sw = File.AppendText(summaryFileName))
                {
                    // Pass, Activity, Errors, Warnings, Infos, Folder, Route, Activity
                    // Excel doesn't handle CSV with commas embedded in text (although Access does :{ )
                    // Simplest solution is to change embedded "," to ";" with .Replace()
                    sw.Write((fatalErrors == 0) ? "yes" : "no");
                    sw.Write(String.Format(", {0}", Simulator.Activity.Tr_Activity.Tr_Activity_Header.Name.Replace(",", ";")));  // e.g. Auto Train with Set-Out
                    sw.Write(String.Format(", {0}", (ErrorCount[0] + ErrorCount[1]).ToString()));   // critical and error
                    sw.Write(String.Format(", {0}", ErrorCount[2].ToString()));              // warning
                    sw.Write(String.Format(", {0}", ErrorCount[3].ToString()));              // information
                    sw.Write(String.Format(", {0}", Simulator.RoutePath.Replace(",", ";")));               // e.g. D:\MSTS\ROUTES\USA2
                    sw.Write(String.Format(", {0}", Simulator.TRK.Tr_RouteFile.Name.Replace(",", ";")));   // e.g. "Marias Pass"
                    sw.Write(String.Format(", {0}", Path.GetFileName(args[0]).Replace(",", ";")));        // e.g. "autotrnsetout.act"
                    sw.Write(String.Format(", {0}", duration.Seconds));
                    sw.Write(String.Format(", {0:0}", Viewer.RenderProcess.FrameRate.SmoothedValue));
                    sw.WriteLine("");
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
            InitLogging(settings, true);
        }

        static void InitLogging(UserSettings settings, bool newFile)
        {
            var logFileName = "";
            if (settings.Logging)
            {
                if ((settings.LoggingPath.Length > 0) && Directory.Exists(settings.LoggingPath))
                {
                    var fileName = settings.LoggingFilename;
                    try
                    {
                        if (newFile)
                        {
                            fileName = String.Format(fileName, Application.ProductName, Version.Length > 0 ? Version : Build, Version, Build, DateTime.Now);
                        }
                        else
                        {  // -test parameter appends all records to a single log file, so filename musn't change with time of day.
                            fileName = String.Format(fileName, Application.ProductName, Version.Length > 0 ? Version : Build, Version, Build, "-test");
                        }
                    }
                    catch { }
                    foreach (var ch in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(ch, '.');

                    logFileName = Path.Combine(settings.LoggingPath, fileName);
                    // Ensure we start with an empty file.
                    if (newFile) File.Delete(logFileName);
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

            // Multiplayer is (currently) detected from the number of arguments passed to RunActivity.exe
            //
            //No of Args   Activity  Explore
            //Single-Player   1         5
            //      Server    3         7
            //      Client    4         8
            //
            //Single-Player arguments for an activity:
            //- Activity
            //
            //Server arguments for an activity:
            //- Activity | PortNo | Username+MPCode
            //
            //Client arguments for an activity:
            //- Activity | IP | PortNo | Username+MPCode

            Arguments = args;
            Simulator = new Simulator(settings, args[0]);
            if (args.Length == 1 || args.Length == 3 || args.Length == 4)
                Simulator.SetActivity(args[0]);
            else if (args.Length >= 5)
                Simulator.SetExplore(args[0], args[1], args[2], args[3], args[4]);

            if (args.Length == 7 && args[5] != "0")
            {
				try
				{
					Server = new Server(args[6], int.Parse(args[5]));
					UserName = Server.UserName;
					Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
						"Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
					Code = Server.Code;
				}
				catch (Exception e)
				{
					System.Console.WriteLine("Connection Error: " + e.Message + ". Will play in single mode"); Server = null;
				}
            }
            if (args.Length == 3 && args[1] != "0")
            {
				try
				{
					Server = new Server(args[2], int.Parse(args[1]));
					UserName = Server.UserName;
					Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
						"Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
					Code = Server.Code;
				}
				catch (Exception e)
				{
					System.Console.WriteLine("Connection Error: " + e.Message + ". Will play in single mode"); Server = null;
				}
            }
            if (args.Length == 4)
            {
				try
				{
					Client = new ClientComm(args[1], int.Parse(args[2]), args[3]);
					UserName = Client.UserName;
					Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
						"Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
					Code = Client.Code;
				}
				catch (Exception e)
				{
					System.Console.WriteLine("Connection Error: " + e.Message + ". Will play in single mode"); Client = null;
				}

            }
            if (args.Length == 8)
            {
				try
				{
					Client = new ClientComm(args[5], int.Parse(args[6]), args[7]);
					UserName = Client.UserName;
					Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
						"Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
					Code = Client.Code;
				}
				catch (Exception e)
				{
					System.Console.WriteLine("Connection Error: " + e.Message + ". Will play in single mode"); Client = null;
				}
			}
        }

        static void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }
    }
}
