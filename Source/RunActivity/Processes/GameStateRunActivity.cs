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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Debugging;
using ORTS.MultiPlayer;

namespace ORTS.Processes
{
    public class GameStateRunActivity : GameState
    {
        static string[] Arguments;
        static Random Random { get { return Program.Random; } set { Program.Random = value; } }  // primary random number generator used throughout the program
        static Simulator Simulator { get { return Program.Simulator; } set { Program.Simulator = value; } }

		//for Multiplayer
		static Server Server { get { return Program.Server; } set { Program.Server = value; } }
		static ClientComm Client { get { return Program.Client; } set { Program.Client = value; } }
		static string UserName { get { return Program.UserName; } set { Program.UserName = value; } }
		static string Code { get { return Program.Code; } set { Program.Code = value; } }

        static Viewer3D Viewer { get { return Program.Viewer; } set { Program.Viewer = value; } }
        static ORTraceListener ORTraceListener { get { return Program.ORTraceListener; } set { Program.ORTraceListener = value; } }
        static string logFileName { get { return Program.logFileName; } set { Program.logFileName = value; } }

        struct savedValues {
            public float initialTileX;
            public float initialTileZ;
            public string[] args;
        }

        static Debugging.DispatchViewer DebugViewer { get { return Program.DebugViewer; } set { Program.DebugViewer = value; } }
        static Debugging.SoundDebugForm SoundDebugForm { get { return Program.SoundDebugForm; } set { Program.SoundDebugForm = value; } }

        LoadingPrimitive Loading;
        Matrix LoadingMatrix = Matrix.Identity;

        public GameStateRunActivity(string[] args)
        {
            Arguments = args;
        }

        internal override void Update(RenderFrame frame, double totalRealSeconds)
        {
            if (Loading != null)
                frame.AddPrimitive(Loading.Material, Loading, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);

            base.Update(frame, totalRealSeconds);
        }

        internal override void Load()
        {
            // Load loading image first!
            if (Loading == null)
                Loading = new LoadingPrimitive(Game);

            var args = Arguments;

            // Look for an action to perform.
            var action = "";
            var actions = new[] { "start", "resume", "replay", "replay_from_save" };
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

            var settings = Game.Settings;

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
                    case "replay":
                        InitLogging(settings, args);
                        Replay(settings, data);
                        break;
                    case "replay_from_save":
                        InitLogging(settings, args);
                        ReplayFromSave(settings, data);
                        break;
                    default:
                        MessageBox.Show("Supply missing activity file name\n"
                            + "   i.e.: RunActivity \"C:\\Program Files\\Microsoft Games\\Train Simulator\\ROUTES\\USA1\\ACTIVITIES\\xxx.act\"\n"
                            + "\n"
                            + "or launch the program OpenRails.exe and select from the menu.");
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
                    // Make sure we quit after handling an error.
                    Game.Exit();
                }
            }
        }

        /// <summary>
        /// Run the specified activity from the beginning.
        /// </summary>
        void Start(UserSettings settings, string[] args)
        {
            InitSimulator(settings, args);
            Simulator.Start();

            Viewer = new Viewer3D(Simulator, Game);
            Viewer.Log = new CommandLog( Viewer );

			if (Client != null)
			{
                Client.Send( (new MSGPlayer( Program.UserName, Program.Code, Program.Simulator.conFileName, Program.Simulator.patFileName, Program.Simulator.Trains[0], 0, Program.Simulator.Settings.AvatarURL )).ToString() );
			}

            Game.ReplaceState(new GameStateRunActivityEnd());
            Game.PushState(new GameStateViewer3D(Viewer));
        }

        /// <summary>
        /// Save the current game state for later resume.
        /// Save files are stored in the user's local program storage:
        /// e.g. "C:\Users\Wayne\AppData\Roaming\ORTS\<activity file name> <date_and_time>.save"
        /// or
        /// e.g. "C:\Users\Wayne\AppData\Roaming\ORTS\<route folder name> <date_and_time>.save"
        /// </summary>
        [CallOnThread("Updater")]
        public static void Save()
        {
            if (MPManager.IsMultiPlayer()) return; //no save for multiplayer sessions yet
            // Prefix with the activity filename so that, when resuming from the Menu.exe, we can quickly find those Saves 
            // that are likely to match the previously chosen route and activity.
            // Append the current date and time, so that each file is unique.
            // This is the "sortable" date format, ISO 8601, but with "." in place of the ":" which are not valid in filenames.
            var fileStem = String.Format("{0} {1:yyyy'-'MM'-'dd HH'.'mm'.'ss}", Simulator.Activity != null ? Simulator.ActivityFileName : Simulator.RoutePathName, DateTime.Now);

            using (BinaryWriter outf = new BinaryWriter(new FileStream(UserSettings.UserDataFolder + "\\" + fileStem + ".save", FileMode.Create, FileAccess.Write)))
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
				Viewer.Log.SaveLog(Path.Combine(UserSettings.UserDataFolder, fileStem + ".replay"));

                // Copy the logfile to the save folder
				CopyLog(Path.Combine(UserSettings.UserDataFolder, fileStem + ".txt"));

                Simulator.Save(outf);
                Viewer.Save(outf, fileStem);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Resume a saved game.
        /// </summary>
        void Resume(UserSettings settings, string[] args)
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
                Viewer = new Viewer3D(Simulator, Game);
                //Viewer.SetCommandReceivers();

                // Reload the command log
                Viewer.Log = new CommandLog( Viewer );
                string replayFile = Path.ChangeExtension( saveFile, "replay" );
                Viewer.Log.LoadLog( replayFile );

                Viewer.inf = inf;
                Game.ReplaceState(new GameStateRunActivityEnd());
                Game.PushState(new GameStateViewer3D(Viewer));
            }
        }

        /// <summary>
        /// Replay a saved game.
        /// </summary>
        void Replay( UserSettings settings, string[] args ) {
            // If "-replay" also specifies a save file then use it
            // E.g. RunActivity.exe -replay "yard_two 2012-03-20 22.07.36"
            // else use most recently changed *.save
            // E.g. RunActivity.exe -replay

            // First use the .save file to check the validity and extract the route and activity.
            string saveFile = GetSaveFile( args );
            using( BinaryReader inf = new BinaryReader( new FileStream( saveFile, FileMode.Open, FileAccess.Read ) ) ) {
                inf.ReadString();    // Revision
                inf.ReadString();    // Build
                savedValues values = GetSavedValues( inf );
                InitSimulator( settings, values.args, "Replay" );
                Simulator.Start();
                Viewer = new Viewer3D(Simulator, Game);
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
            CommandLog.ReportReplayCommands( Viewer.ReplayCommandList );

            Game.ReplaceState(new GameStateRunActivityEnd());
            Game.PushState(new GameStateViewer3D(Viewer));
        }

        /// <summary>
        /// Replay the last segment of a saved game.
        /// </summary>
        void ReplayFromSave( UserSettings settings, string[] args ) {
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
					f = Path.Combine(UserSettings.UserDataFolder, f);
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
                Viewer = new Viewer3D(Simulator, Game);
            } else {
                // Resume from previousSaveFile
                // and then replay
                inf = new BinaryReader(
                        new FileStream( previousSaveFile, FileMode.Open, FileAccess.Read ) );
                ValidateSave(previousSaveFile, inf);
                savedValues values = GetSavedValues( inf );
                InitSimulator( settings, values.args, "Resume" );
                Simulator.Restore( inf, values.initialTileX, values.initialTileZ );
                Viewer = new Viewer3D(Simulator, Game);
            }

            // Now Viewer exists, link the log to it in both directions
            Viewer.Log = log;
            log.Viewer = Viewer;
            // Now Simulator exists, link the viewer to it
            Viewer.Log.Simulator = Simulator;
            Viewer.ReplayCommandList = replayCommandList;
            CommandLog.ReportReplayCommands( Viewer.ReplayCommandList );

            Viewer.inf = inf;
            Game.ReplaceState(new GameStateRunActivityEnd());
            Game.PushState(new GameStateViewer3D(Viewer));
        }

        /// <summary>
        /// Tests that RunActivity.exe can launch a specific activity or explore.
        /// </summary>
        void Test(UserSettings settings, string[] args)
        {
            var passed = false;
            var startTime = DateTime.Now;
            var loadTime = 0d;
            try
            {
                InitSimulator(settings, args, "Test");
                Simulator.Start();
                Viewer = new Viewer3D(Simulator, Game);
                Viewer.Log = new CommandLog(Viewer);
                Game.ReplaceState(new GameStateRunActivityEnd());
                Game.PushState(new GameStateViewer3D(Viewer));
                loadTime = (DateTime.Now - startTime).TotalSeconds - Viewer.RealTime;
                passed = true;
            }
            finally
            {
                ExportTestSummary(settings, args, passed, loadTime);
                Environment.ExitCode = passed ? 0 : 1;
            }
        }

        void ExportTestSummary(UserSettings settings, string[] args, bool passed, double loadTime)
        {
            // Append to CSV file in format suitable for Excel
			var summaryFileName = Path.Combine(UserSettings.UserDataFolder, "TestingSummary.csv");
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

        void InitLogging( UserSettings settings, string[] args ) {
            InitLogging( settings, args, false );
        }

        void InitLogging( UserSettings settings, string[] args, bool appendLog ) {
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
			File.Copy(logFileName, toFile, true);
        }

        void InitSimulator(UserSettings settings, string[] args)
        {
            InitSimulator(settings, args, "");
        }

        void InitSimulator(UserSettings settings, string[] args, string mode)
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

        void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }
        
        void ValidateSave(string fileName, BinaryReader inf) {
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

        string GetSaveFile( string[] args ) {
            if( args.Length == 0 ) {
                return GetMostRecentSave();
            }
            string saveFile = args[0];
            if( !saveFile.EndsWith( ".save" ) ) { saveFile += ".save"; }
			return Path.Combine(UserSettings.UserDataFolder, saveFile);
        }

        string GetMostRecentSave() {
			var directory = new DirectoryInfo(UserSettings.UserDataFolder);
            var file = directory.GetFiles( "*.save" )
             .OrderByDescending( f => f.LastWriteTime )
             .First();
            if( file == null ) throw new FileNotFoundException( String.Format(
                "Activity Save file '*.save' not found in folder {0}", directory ) );
            return file.FullName;
        }
        
        savedValues GetSavedValues( BinaryReader inf ) {
            savedValues values = default( savedValues );
            // Skip the heading data used in Menu.exe
            inf.ReadString();    // Route name
            inf.ReadString();    // Path name
            inf.ReadInt32();     // Time elapsed in game (secs)
            inf.ReadInt64();     // Date and time in real world
            inf.ReadSingle();    // Current location of player train TileX
            inf.ReadSingle();    // Current location of player train TileZ

            // Read initial position and pass to Simulator so it can be written out if another save is made.
            values.initialTileX = inf.ReadSingle();  // Initial location of player train TileX
            values.initialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

            // Read in the real data...
            var savedArgs = new string[inf.ReadInt32()];
            for( var i = 0; i < savedArgs.Length; i++ )
                savedArgs[i] = inf.ReadString();
            values.args = savedArgs;
            return values;
        }

        class LoadingPrimitive : RenderPrimitive
        {
            public readonly LoadingMaterial Material;
            readonly VertexDeclaration VertexDeclaration;
            readonly VertexBuffer VertexBuffer;

            public LoadingPrimitive(Game game)
            {
                Material = new LoadingMaterial(game);
                var dd = (float)Material.Texture.Width / 2 + 0.5f;
                var verticies = new[] {
				    new VertexPositionTexture(new Vector3(-dd, +dd, -1), new Vector2(0, 0)),
				    new VertexPositionTexture(new Vector3(+dd, +dd, -1), new Vector2(1, 0)),
				    new VertexPositionTexture(new Vector3(-dd, -dd, -1), new Vector2(0, 1)),
				    new VertexPositionTexture(new Vector3(+dd, -dd, -1), new Vector2(1, 1)),
			    };

                VertexDeclaration = new VertexDeclaration(game.GraphicsDevice, VertexPositionTexture.VertexElements);
                VertexBuffer = new VertexBuffer(game.GraphicsDevice, VertexPositionTexture.SizeInBytes * verticies.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(verticies);
            }

            public override void Draw(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.VertexDeclaration = VertexDeclaration;
                graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        class LoadingMaterial : Material
        {
            public readonly LoadingShader Shader;
            public readonly Texture2D Texture;

            public LoadingMaterial(Game game)
                : base(null, null)
            {
                Shader = new LoadingShader(game.RenderProcess.GraphicsDevice);
                Texture = Texture2D.FromFile(game.RenderProcess.GraphicsDevice, Path.Combine(game.ContentPath, "Loading.png"));
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                Shader.CurrentTechnique = Shader.Techniques["Loading"];
                Shader.LoadingTexture = Texture;

                graphicsDevice.RenderState.AlphaBlendEnable = true;
                graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            }

            public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
            {
                Shader.Begin();
                Shader.CurrentTechnique.Passes[0].Begin();
                foreach (var item in renderItems)
                {
                    Shader.WorldViewProjection = item.XNAMatrix * XNAViewMatrix * XNAProjectionMatrix;
                    Shader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                Shader.CurrentTechnique.Passes[0].End();
                Shader.End();
            }

            public override void ResetState(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.RenderState.AlphaBlendEnable = false;
                graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
                graphicsDevice.RenderState.SourceBlend = Blend.One;
            }
        }

        class LoadingShader : Shader
        {
            readonly EffectParameter worldViewProjection;
            readonly EffectParameter loadingTexture;

            public Matrix WorldViewProjection { set { worldViewProjection.SetValue(value); } }

            public Texture2D LoadingTexture { set { loadingTexture.SetValue(value); } }

            public LoadingShader(GraphicsDevice graphicsDevice)
                : base(graphicsDevice, "Loading")
            {
                worldViewProjection = Parameters["WorldViewProjection"];
                loadingTexture = Parameters["LoadingTexture"];
            }
        }
    }

    public class GameStateRunActivityEnd : GameState
    {
        internal override void Load()
        {
            if (Program.Simulator != null)
                Program.Simulator.Stop();
            if (Program.DebugViewer != null)
                Program.DebugViewer.Dispose();
            if (Program.SoundDebugForm != null)
                Program.SoundDebugForm.Dispose();
            Game.PopState();
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
