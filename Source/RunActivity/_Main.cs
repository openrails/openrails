// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

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
/// </summary>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ORTS.Common;
using ORTS.Debugging;
using ORTS.MultiPlayer;

namespace ORTS
{
    static class Program
    {
        public static string[] Arguments;
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
        static string logFileName = "";

        private struct savedValues {
            public float initialTileX;
            public float initialTileZ;
            public string[] args;
        }

        public static Debugging.DispatchViewer DebugViewer;
        public static bool DebugViewerEnabled = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);

            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";

            // Look for an action to perform.
            var action = "";
            var actions = new[] { "start", "resume", "test", "testall", "replay", "replay_from_save" };
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

            Action doAction = () => 
            {
                // Do the action specified or write out some help.
                switch (action)
                {
                    case "start":
                    case "start-profile":
                        InitLogging(settings, args);
                        Start(settings, data);
                        break;
                    case "resume":
                        InitLogging(settings, args);
                        Resume(settings, data);
                        break;
                    case "test":
                        InitLogging(settings, args, true);
                        Test(settings, data);
                        break;
                    case "testall":
                        InitLogging(settings, args);
                        TestAll(data);
                        break;
                    case "replay":
                        InitLogging(settings, args);
                        Replay(settings, data);
                        break;
                    case "replay_from_save":
                        InitLogging(settings, args);
                        ReplayFromSave(settings, data);
                        break;
                    default:
                        Console.WriteLine("Supply missing activity file name");
                        Console.WriteLine("   i.e.: RunActivity \"C:\\Program Files\\Microsoft Games\\Train Simulator\\ROUTES\\USA1\\ACTIVITIES\\xxx.act\"");
                        Console.WriteLine();
                        Console.WriteLine("or launch the program OpenRails.exe and select from the menu.");
                        Console.ReadKey();
                        break;
                }
            };
            if (Debugger.IsAttached) // Separate code path during debugging, so IDE stops at the problem and not at the message.
            {
                doAction();
            }
            else
            {
                try
                {
                    doAction();
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FatalException(error));
                    if (settings.ShowErrorDialogs)
                    {
                        // If we had a load error but the inner error is one we handle here specially, unwrap it and discard the extra file information.
                        var loadError = error as FileLoadException;
                        if (loadError != null && (error.InnerException is FileNotFoundException || error.InnerException is DirectoryNotFoundException))
                            error = error.InnerException;

                        if (error is IncompatibleSaveException)
                        {
                            MessageBox.Show(error.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (error is FileNotFoundException)
                        {
                            MessageBox.Show(String.Format(
                                    "An essential file is missing and {0} cannot continue.\n\n" +
                                    "    {1}",
                                    Application.ProductName, (error as FileNotFoundException).FileName),
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (error is DirectoryNotFoundException)
                        {
                            // This is a hack to try and extract the actual file name from the exception message. It isn't available anywhere else.
                            var re = new Regex("'([^']+)'").Match(error.Message);
                            var fileName = re.Groups[1].Success ? re.Groups[1].Value : error.Message;
                            MessageBox.Show(String.Format(
                                    "An essential folder is missing and {0} cannot continue.\n\n" +
                                    "    {1}",
                                    Application.ProductName, fileName),
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            var errorSummary = error.GetType().FullName + ": " + error.Message;
                            var logFile = Path.Combine(settings.LoggingPath, settings.LoggingFilename);
                            var openTracker = MessageBox.Show(String.Format(
                                    "A fatal error has occured and {0} cannot continue.\n\n" +
                                    "    {1}\n\n" +
                                    "This error may be due to bad data or a bug. You can help improve {0} by reporting this error in our bug tracker at http://launchpad.net/or and attaching the log file {2}.\n\n" +
                                    ">>> Please report this error to the {0} bug tracker <<<",
                                    Application.ProductName, errorSummary, logFile),
                                    Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                            if (openTracker == DialogResult.OK)
                                Process.Start("http://launchpad.net/or");
                            // James Ross would prefer to do this:
                            //   Process.Start("http://bugs.launchpad.net/or/+filebug?field.title=" + Uri.EscapeDataString(errorSummary));
                            // but unfortunately if you need to log in (as most people might), Launchpad munges the title
                            // and leaves you with garbage. Plus, landing straight on a login page might confuse some people.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Run the specified activity from the beginning.
        /// </summary>
        static void Start(UserSettings settings, string[] args)
        {
            InitSimulator(settings, args);
            Simulator.Start();

            Viewer = new Viewer3D(Simulator);
            Viewer.Log = new CommandLog( Viewer );

			if (Client != null)
			{
                Client.Send( (new MSGPlayer( Program.UserName, Program.Code, Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.Trains[0], 0, Program.Simulator.Settings.AvatarURL )).ToString() );
			}

            if (MPManager.IsMultiPlayer() || Viewer.Settings.ViewDispatcher)
            {
                // prepare to show debug output in a separate window
                DebugViewer = new DispatchViewer(Simulator, Viewer);
                DebugViewer.Show();
                DebugViewer.Hide();
                Viewer.DebugViewerEnabled = false;
            }

            Viewer.Run(null);

            Simulator.Stop();

            if (MPManager.IsMultiPlayer()) DebugViewer.Dispose();
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
            // Prefix with the activity filename so that, when resuming from the Menu.exe, we can quickly find those Saves 
            // that are likely to match the previously chosen route and activity.
            // Append the current date and time, so that each file is unique.
            // This is the "sortable" date format, ISO 8601, but with "." in place of the ":" which are not valid in filenames.
            var fileStem = String.Format("{0} {1:yyyy'-'MM'-'dd HH'.'mm'.'ss}", Simulator.Activity != null ? Simulator.ActivityFileName : Simulator.RoutePathName, DateTime.Now);

            using (BinaryWriter outf = new BinaryWriter(new FileStream(UserDataFolder + "\\" + fileStem + ".save", FileMode.Create, FileAccess.Write)))
            {
                // Save some version identifiers so we can validate on load.
                outf.Write(VersionInfo.Version);
                outf.Write(VersionInfo.Build);

                // Save heading data used in Menu.exe
                outf.Write(Simulator.RouteName);
                outf.Write(Simulator.PathName);

                outf.Write((int)Simulator.GameTime);
                outf.Write( DateTime.Now.ToBinary() );
                outf.Write(Simulator.Trains[0].FrontTDBTraveller.TileX + (Simulator.Trains[0].FrontTDBTraveller.X / 2048));
                outf.Write( Simulator.Trains[0].FrontTDBTraveller.TileZ + (Simulator.Trains[0].FrontTDBTraveller.Z / 2048) );
                outf.Write( Simulator.InitialTileX );
                outf.Write( Simulator.InitialTileZ );

                // Now save the data used by RunActivity.exe
                outf.Write(Arguments.Length);
                foreach( var argument in Arguments )
                    outf.Write(argument);

                // The Save command is the only command that doesn't take any action. It just serves as a marker.
                new SaveCommand( Viewer.Log, fileStem );
                Viewer.Log.SaveLog( Path.Combine(UserDataFolder, fileStem + ".replay" ) );

                // Copy the logfile to the save folder
                CopyLog( Path.Combine( UserDataFolder, fileStem + ".txt" ) );

                Simulator.Save(outf);
                Viewer.Save(outf, fileStem);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
        static void Resume(UserSettings settings, string[] args)
        {
            // If "-resume" also specifies a save file then use it
            // E.g. RunActivity.exe -resume "yard_two 2012-03-20 22.07.36"
            // else use most recently changed *.save
            // E.g. RunActivity.exe -resume

            // First use the .save file to check the validity and extract the route and activity.
            string saveFile = GetSaveFile( args );
            using( BinaryReader inf = new BinaryReader(
                    new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
                ValidateSave(saveFile, inf);
                savedValues values = GetSavedValues( inf );
                InitSimulator( settings, values.args, "Resume" );
                Simulator.Restore( inf, values.initialTileX, values.initialTileZ );
                Viewer = new Viewer3D( Simulator );
                //Viewer.SetCommandReceivers();

                // Reload the command log
                Viewer.Log = new CommandLog( Viewer );
                string replayFile = Path.ChangeExtension( saveFile, "replay" );
                Viewer.Log.LoadLog( replayFile );
				if (MPManager.IsMultiPlayer() || Viewer.Settings.ViewDispatcher)
				{
					// prepare to show debug output in a separate window
					DebugViewer = new DispatchViewer(Simulator, Viewer);
					DebugViewer.Show();
					DebugViewer.Hide();
					Viewer.DebugViewerEnabled = false;
				}

				Viewer.Run(inf);
	
				if (MPManager.IsMultiPlayer() || Viewer.Settings.ViewDispatcher)
					if (DebugViewer != null && !DebugViewer.IsDisposed) DebugViewer.Dispose();
            }
        }

        /// <summary>
        /// Replay a saved game.
        /// </summary>
        static void Replay( UserSettings settings, string[] args ) {
            // If "-replay" also specifies a save file then use it
            // E.g. RunActivity.exe -replay "yard_two 2012-03-20 22.07.36"
            // else use most recently changed *.save
            // E.g. RunActivity.exe -replay

            // First use the .save file to check the validity and extract the route and activity.
            string saveFile = GetSaveFile( args );
            using( BinaryReader inf = new BinaryReader( new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
                var revision = inf.ReadString();
                var build = inf.ReadString();
                savedValues values = GetSavedValues( inf );
                InitSimulator( settings, values.args, "Replay" );
                Simulator.Start();
                Viewer = new Viewer3D( Simulator );
            }

            Viewer.Log = new CommandLog( Viewer );
            // Load command log to replay
            Viewer.ReplayCommandList = new List<ICommand>();
            string replayFile = Path.ChangeExtension( saveFile, "replay" );
            Viewer.Log.LoadLog( replayFile );
            foreach( var c in Viewer.Log.CommandList ) {
                Viewer.ReplayCommandList.Add( c );
            }
            Viewer.Log.CommandList.Clear();
            Viewer.Log.ReportReplayCommands( Viewer.ReplayCommandList );

            Viewer.Run( null );
            Simulator.Stop();
        }

        /// <summary>
        /// Replay the last segment of a saved game.
        /// </summary>
        static void ReplayFromSave( UserSettings settings, string[] args ) {
            BinaryReader inf;

            // E.g. RunActivity.exe -replay_from_save "yard_two 2012-03-20 22.07.36"
            string saveFile = GetSaveFile( args );

            // Find previous save file and move commands to be replayed into replay list.
            CommandLog log = new CommandLog();
            string logFile = saveFile.Replace( ".save", ".replay" );
            log.LoadLog( logFile );
            List<ICommand> replayCommandList = new List<ICommand>();

            // Scan backwards to find previous saveFile (ignore any that user has deleted).
            int count = log.CommandList.Count;
            string previousSaveFile = "";
            for( int i = count - 2; // -2 so we skip over the final save command
                    i >= 0; i-- ) {
                var c = log.CommandList[i];
                if( c is SaveCommand ) {
                    string f = ((SaveCommand)c).FileStem;
                    f = Path.Combine( Program.UserDataFolder, f );
                    if( !f.EndsWith( ".save" ) ) { f += ".save"; }
                    if( System.IO.File.Exists( f ) ) {
                        previousSaveFile = f;
                        // Move commands after this to the replay command list.
                        for( int j = i + 1; j < count; j++ ) {
                            replayCommandList.Add( log.CommandList[i + 1] );
                            log.CommandList.RemoveAt( i + 1 );
                        }
                        break;
                    }
                }
            }
            if( previousSaveFile == "" ) {  // No save file found so just replay from start
                replayCommandList.AddRange(log.CommandList);    // copy the commands before deleting them.
                log.CommandList.Clear();
                // But we have no args, so have to get these from the Save
                inf = new BinaryReader(
                        new FileStream( saveFile, FileMode.Open, FileAccess.Read ) );
                ValidateSave(saveFile, inf);
                savedValues values = GetSavedValues( inf );
                inf = null; // else Viewer.Initialize() will trigger Viewer.Restore()
                InitSimulator( settings, values.args, "Replay" );
                Simulator.Start();
                Viewer = new Viewer3D( Simulator );
            } else {
                // Resume from previousSaveFile
                // and then replay
                inf = new BinaryReader(
                        new FileStream( previousSaveFile, FileMode.Open, FileAccess.Read ) );
                ValidateSave(previousSaveFile, inf);
                savedValues values = GetSavedValues( inf );
                InitSimulator( settings, values.args, "Resume" );
                Simulator.Restore( inf, values.initialTileX, values.initialTileZ );
                Viewer = new Viewer3D( Simulator );
            }

            // Now Viewer exists, link the log to it in both directions
            Viewer.Log = log;
            log.Viewer = Viewer;
            // Now Simulator exists, link the viewer to it
            Viewer.Log.Simulator = Simulator;
            Viewer.ReplayCommandList = replayCommandList;
            Viewer.Log.ReportReplayCommands( Viewer.ReplayCommandList );

            Viewer.Run( inf );
            Simulator.Stop();
        }

        /// <summary>
        /// Tests OR against every activity in every route in every folder.
        /// <CJComment>
        /// From v974 (and probably much before) this method fails on the second activity, raising a fatal error InvalidOperationException in MSTSLocomotive.cs:DisassembleFrames()
        /// (Tried on just the JAPAN2 activities and on just the USA2 activities.)
        /// Superseded by the Test() method.
        /// </CJ>
        /// </summary>
        static void TestAll(string[] args)
        {
            var settings = GetSettings(new[] { "ShowErrorDialogs=no", "Profiling", "ProfilingFrameCount=0" });
            InitLogging(settings, args);
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
                Viewer.Log = new CommandLog(Viewer);
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
        }

        /// <summary>
        /// Tests that RunActivity.exe can launch a specific activity or explore.
        /// </summary>
        public static void Test(UserSettings settings, string[] args)
        {
            var passed = false;
            var startTime = DateTime.Now;
            var loadTime = 0d;
            try
            {
                InitSimulator(settings, args, "Test");
                Simulator.Start();
                Viewer = new Viewer3D(Simulator);
                Viewer.Log = new CommandLog(Viewer);
                Viewer.Run(null);
                Simulator.Stop();
                loadTime = (DateTime.Now - startTime).TotalSeconds - Viewer.RealTime;
                passed = true;
            }
            finally
            {
                ExportTestSummary(settings, args, passed, loadTime);
                Environment.ExitCode = passed ? 0 : 1;
            }
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

        static UserSettings GetSettings(IEnumerable<string> options)
        {
            return new UserSettings(RegistryKey, options);
        }

        static void InitLogging( UserSettings settings, string[] args ) {
            InitLogging( settings, args, false );
        }

        static void InitLogging( UserSettings settings, string[] args, bool appendLog ) {
            if( settings.LoggingPath == "" ) {
                settings.LoggingPath = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
            }
            if( settings.Logging ) {
                if( (settings.LoggingPath.Length > 0) && Directory.Exists( settings.LoggingPath ) ) {
                    var fileName = settings.LoggingFilename;
                    try
                    {
                        fileName = String.Format(fileName, Application.ProductName, VersionInfo.Version.Length > 0 ? VersionInfo.Version : VersionInfo.Build, VersionInfo.Version, VersionInfo.Build, DateTime.Now);
                    }
                    catch { }
                    foreach (var ch in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(ch, '.');

                    logFileName = Path.Combine( settings.LoggingPath, fileName );
                    // Ensure we start with an empty file.
                    if (!appendLog)
                        File.Delete(logFileName);
                    // Make Console.Out go to the log file AND the output stream.
                    Console.SetOut( new FileTeeLogger( logFileName, Console.Out ) );
                    // Make Console.Error go to the new Console.Out.
                    Console.SetError( Console.Out );
                }
            }

            // Captures Trace.Trace* calls and others and formats.
            ORTraceListener = new ORTraceListener(Console.Out, !settings.Logging);
            ORTraceListener.TraceOutputOptions = TraceOptions.Callstack;
            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(ORTraceListener);

            Console.WriteLine( "{0} is starting...", Application.ProductName ); { int i = 0; foreach( var a in args ) { Console.WriteLine( String.Format( "Argument {0} = {1}", i++, a ) ); } }

            Console.WriteLine("Version    = {0}", VersionInfo.Version.Length > 0 ? VersionInfo.Version : "<none>");
            Console.WriteLine("Build      = {0}", VersionInfo.Build);
            if (logFileName.Length > 0)
                Console.WriteLine("Logfile    = {0}", logFileName);
            LogSeparator();
            settings.Log();
            LogSeparator();
            if( !settings.Logging ) {
                Console.WriteLine( "Logging is disabled, only fatal errors will appear here." );
                LogSeparator();
            }
        }

        static void CopyLog( string toFile ) {
			if (logFileName.Length == 0) return;
			File.Copy(logFileName, toFile);
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
					MPManager.Instance().MPUpdateInterval = settings.Multiplayer_UpdateInterval;
					Client = new ClientComm(settings.Multiplayer_Host, settings.Multiplayer_Port, settings.Multiplayer_User + " 1234");
                    UserName = Client.UserName;
                    Debug.Assert(UserName.Length >= 4 && UserName.Length <= 10 && !UserName.Contains('\"') && !UserName.Contains('\'') && !char.IsDigit(UserName[0]),
                        "Error in the user name: should not start with digits, be 4-10 characters long and no special characters");
                    Code = Client.Code;
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
        
        private static void ValidateSave(string fileName, BinaryReader inf) {
            // Read in validation data.
            var version = "<unknown>";
            var build = "<unknown>";
            var versionOkay = false;
            try {
                version = inf.ReadString().Replace( "\0", "" );
                build = inf.ReadString().Replace( "\0", "" );
                versionOkay = (version == VersionInfo.Version) && (build == VersionInfo.Build);
            } catch { }

            if( !versionOkay ) {
                if (Debugger.IsAttached)
                {
                    // Only if debugging, then allow user to continue as
                    // resuming from saved activities is useful in debugging.
                    // (To resume from the latest save, set 
                    // RunActivity > Properties > Debug > Command line arguments = "-resume")
                    Trace.WriteLine(new IncompatibleSaveException(fileName, version, build, VersionInfo.Version, VersionInfo.Build));
                    LogSeparator();
                } else {
                    throw new IncompatibleSaveException(fileName, version, build, VersionInfo.Version, VersionInfo.Build);
                }
            }
        }

        private static string GetSaveFile( string[] args ) {
            if( args.Length == 0 ) {
                return GetMostRecentSave();
            }
            string saveFile = args[0];
            if( !saveFile.EndsWith( ".save" ) ) { saveFile += ".save"; }
            return Path.Combine( Program.UserDataFolder, saveFile );
        }

        private static string GetMostRecentSave() {
            var directory = new DirectoryInfo( UserDataFolder );
            var file = directory.GetFiles( "*.save" )
             .OrderByDescending( f => f.LastWriteTime )
             .First();
            if( file == null ) throw new FileNotFoundException( String.Format(
                "Activity Save file '*.save' not found in folder {0}", directory ) );
            return file.FullName;
        }
        
        private static string GetMostRecentReplay() {
            var directory = new DirectoryInfo( UserDataFolder );
            var file = directory.GetFiles( "*.replay" )
             .OrderByDescending( f => f.LastWriteTime )
             .First();
            if( file == null ) throw new FileNotFoundException( String.Format(
                "Activity Replay file '*.replay' found in folder {0}", directory ) );
            return file.FullName;
        }

        private static savedValues GetSavedValues( BinaryReader inf ) {
            savedValues values = default( savedValues );
            // Skip the heading data used in Menu.exe
            string temp = inf.ReadString();         // Route name
            temp = inf.ReadString();                // Path name
            int tempInt = inf.ReadInt32();          // Time elapsed in game (secs)
            Int64 tempInt64 = inf.ReadInt64();      // Date and time in real world
            float tempFloat = inf.ReadSingle();     // Current location of player train TileX
            tempFloat = inf.ReadSingle();           // Current location of player train TileZ

            // Read initial position and pass to Simulator so it can be written out if another save is made.
            values.initialTileX = inf.ReadSingle();  // Initial location of player train TileX
            values.initialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

            // Read in the real data...
            tempInt = inf.ReadInt32();
            var savedArgs = new string[tempInt];
            for( var i = 0; i < savedArgs.Length; i++ )
                savedArgs[i] = inf.ReadString();
            values.args = savedArgs;
            return values;
        }
    }

    public sealed class IncompatibleSaveException : Exception
    {
        public IncompatibleSaveException(string fileName, string version, string build, string gameVersion, string gameBuild)
            : base(version.Length > 0 && build.Length > 0 ?
                String.Format("Saved game file is not compatible with this version of {0}.\n\nFile: {1}\nSave: {4} ({5})\nGame: {2} ({3})", Application.ProductName, fileName, gameVersion, gameBuild, version, build) :
            String.Format("Saved game file is not compatible with this version of {0}.\n\nFile: {1}\nGame: {2} ({3})", Application.ProductName, fileName, gameVersion, gameBuild))
        {
        }
    }
}
