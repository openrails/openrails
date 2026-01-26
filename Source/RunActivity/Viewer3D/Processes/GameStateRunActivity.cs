// COPYRIGHT 2021 by the Open Rails project.
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

// Define this to include extra data on loading performance and progress indications.
//#define DEBUG_LOADING

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation;
using Orts.Viewer3D.Debugging;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Orts.Viewer3D.Processes
{
    public class GameStateRunActivity : GameState
    {
        static string[] Arguments;
        static string Acttype;
        static Simulator Simulator { get { return Program.Simulator; } set { Program.Simulator = value; } }

        //for Multiplayer
        static Server Server { get { return MPManager.Server; } set { MPManager.Server = value; } }
        static ClientComm Client { get { return MPManager.Client; } set { MPManager.Client = value; } }
        string UserName;
        string Code;

        static Viewer Viewer { get { return Program.Viewer; } set { Program.Viewer = value; } }
        static ORTraceListener ORTraceListener { get { return Program.ORTraceListener; } set { Program.ORTraceListener = value; } }
        static string logFileName { get { return Program.logFileName; } set { Program.logFileName = value; } }
        static string EvaluationFilename { get { return Program.EvaluationFilename; } set { Program.EvaluationFilename = value; } }

        /// <summary>
        /// A set of save files all have the same filestem but a specific extension:
        /// *.save for the binary data containing the simulation values at the time of saving.
        /// *.txt for the log file, provided only for reference.
        /// *.png for the screenshot taken automatically at the moment of saving and shown in minature in the Resume menu.
        /// *.replay for binary data containing the user's commands so the simulation can be replayed if required.
        /// *.evaluation.txt for the text evaluation if an activity evaluation has been requested.
        /// </summary>
        public class SaveSet
        { 
            public string FileStem { get; }

        // Prefix with the activity filename so that, when resuming from the Menu.exe, we can quickly find those Saves 
        // that are likely to match the previously chosen route and activity.
        // Append the current date and time, so that each file is unique.
        // This is the "sortable" date format, ISO 8601, but with "." in place of the ":" which is not valid in filenames.
        public SaveSet()
            {
                FileStem = String.Format("{0} {1} {2:yyyy-MM-dd HH.mm.ss}",
                    Simulator.Activity != null
                        ? Simulator.ActivityFileName
                        : (String.IsNullOrEmpty(Simulator.TimetableFileName)
                            ? Simulator.RoutePathName
                            : Simulator.RoutePathName + " " + Simulator.TimetableFileName),
                    MPManager.IsMultiPlayer() && MPManager.IsServer()
                        ? "$Multipl$ "
                        : "",
                    DateTime.Now);
            }
        }

        struct savedValues
        {
            public string pathName;
            public float initialTileX;
            public float initialTileZ;
            public string[] args;
            public string acttype;
        }

        static MapViewer MapForm { get { return Program.MapForm; } set { Program.MapForm = value; } }
        static SoundDebugForm SoundDebugForm { get { return Program.SoundDebugForm; } set { Program.SoundDebugForm = value; } }

        LoadingPrimitive Loading;
        LoadingScreenPrimitive LoadingScreen;
        LoadingBarPrimitive LoadingBar;
        TimetableLoadingBarPrimitive TimetableLoadingBar;
        Matrix LoadingMatrix = Matrix.Identity;

        public GameStateRunActivity(string[] args)
        {
            Arguments = args;
        }

        internal override void Dispose()
        {
            Loading.Dispose();
            LoadingScreen.Dispose();
            LoadingBar.Dispose();
            TimetableLoadingBar.Dispose();
            base.Dispose();
        }

        internal override void Update(RenderFrame frame, double totalRealSeconds)
        {
            UpdateLoading();

            if (Loading != null)
            {
                frame.AddPrimitive(Loading.Material, Loading, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            if (LoadingScreen != null)
            {
                frame.AddPrimitive(LoadingScreen.Material, LoadingScreen, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            if (LoadingBar != null)
            {
                LoadingBar.Material.Shader.LoadingPercent = LoadedPercent;
                frame.AddPrimitive(LoadingBar.Material, LoadingBar, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            if (Simulator != null && Simulator.TimetableMode && TimetableLoadingBar != null 
                && Simulator.TimetableLoadedFraction < 0.99f    // 0.99 to hide loading bar at end of timetable pre-run
            )
            {
                TimetableLoadingBar.Material.Shader.LoadingPercent = Simulator.TimetableLoadedFraction;
                frame.AddPrimitive(TimetableLoadingBar.Material, TimetableLoadingBar, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            base.Update(frame, totalRealSeconds);
        }

        internal override void Load()
        {
            // Load loading image first!
            if (Loading == null)
                Loading = new LoadingPrimitive(Game);
            if (LoadingBar == null)
                LoadingBar = new LoadingBarPrimitive(Game);
            if (TimetableLoadingBar == null)
                TimetableLoadingBar = new TimetableLoadingBarPrimitive(Game);
            var args = Arguments;

            // Look for an action to perform.
            var action = "";
            var actions = new[] { "start", "resume", "replay", "replay_from_save", "test"};
            foreach (var possibleAction in actions)
                if (args.Contains("-" + possibleAction) || args.Contains("/" + possibleAction, StringComparer.OrdinalIgnoreCase))
                {
                    action = possibleAction;
                    Game.Settings.MultiplayerServer = false;
                    Game.Settings.MultiplayerClient = false;
                }

            // Look for required type of action
            var acttype = "";
            var acttypes = new[] { "activity", "explorer", "exploreactivity", "timetable" };
            foreach (var possibleActType in acttypes)
                if (args.Contains("-" + possibleActType) || args.Contains("/" + possibleActType, StringComparer.OrdinalIgnoreCase))
                    acttype = possibleActType;

            Acttype = acttype;

            // Collect all non-action options.
            var options = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")) && !actions.Contains(a.Substring(1)) && !acttype.Contains(a.Substring(1))).Select(a => a.Substring(1)).ToArray();

            // Collect all non-options as data.
            var data = args.Where(a => !a.StartsWith("-") && !a.StartsWith("/")).ToArray();

            // No action, check for data; for now assume any data is good data.
            if (action.Length == 0 && data.Length > 0)
            {
                // in multiplayer start/resume there is no "-start" or "-resume" string, so you have to discriminate
                if (Acttype.Length > 0 || options.Length == 0) action = "start";
                else action = "resume";
            }

            var settings = Game.Settings;

            Action doAction = () =>
            {
                // Do the action specified or write out some help.
                switch (action)
                {
                    case "start":
                    case "start-profile":
                        InitLogging(settings, args);
                        InitLoading(settings, args);
                        Start(settings, acttype, data);
                        break;
                    case "resume":
                        InitLogging(settings, args);
                        InitLoading(settings, args);
                        Resume(settings, data);
                        break;
                    case "replay":
                        InitLogging(settings, args);
                        InitLoading(settings, args);
                        Replay(settings, data);
                        break;
                    case "replay_from_save":
                        InitLogging(settings, args);
                        InitLoading(settings, args);
                        ReplayFromSave(settings, data);
                        break;
                    case "test":
                        InitLogging(settings, args, true);
                        InitLoading(settings, args);
                        Test(settings, data);
                        break;

                    default:
                        MessageBox.Show("To start " + Application.ProductName + ", please run 'OpenRails.exe'.\n\n"
                                + "If you are attempting to debug this component, please run 'OpenRails.exe' and execute the scenario you are interested in. "
                                + "In the log file, a line with the command-line arguments used will be listed at the top. "
                                + "You should then configure your debug environment to execute this component with those command-line arguments.",
                                Application.ProductName + " " + VersionInfo.VersionOrBuild);
                        Game.Exit();
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
                    // Turn off the watchdog since we're going down.
                    Game.WatchdogProcess.Stop();
                    Trace.WriteLine(new FatalException(error));
                    if (settings.ShowErrorDialogs)
                    {
                        // If we had a load error but the inner error is one we handle here specially, unwrap it and discard the extra file information.
                        var loadError = error as FileLoadException;
                        if (loadError != null && (error.InnerException is FileNotFoundException || error.InnerException is DirectoryNotFoundException))
                            error = error.InnerException;

                        if (error is IncompatibleSaveException)
                        {
                            MessageBox.Show(String.Format(
                                "Save file is incompatible with this version of {0}.\n\n" +
                                "    {1}\n\n" +
                                "Saved version: {2}\n" +
                                "Current version: {3}",
                                Application.ProductName,
                                ((IncompatibleSaveException)error).SaveFile,
                                ((IncompatibleSaveException)error).VersionOrBuild,
                                VersionInfo.VersionOrBuild),
                                Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (error is InvalidCommandLine)
                            MessageBox.Show(String.Format(
                                "{0} was started with an invalid command-line. {1} Arguments given:\n\n{2}",
                                Application.ProductName,
                                error.Message,
                                String.Join("\n", data.Select(d => "\u2022 " + d).ToArray())),
                                Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else if (error is Traveller.MissingTrackNodeException)
                            MessageBox.Show(String.Format("Open Rails detected a track shape index {0} which is not present in tsection.dat and cannot continue.\n\n" +
                                "The version of standard tsection.dat may be out of date, or this route requires a custom tsection.dat.\n" +
                                "Please check the route installation instructions to verify the required tsection.dat.",
                                ((Traveller.MissingTrackNodeException)error).Index));
                        else if (error is FileNotFoundException)
                        {
                            MessageBox.Show(String.Format(
                                    "An essential file is missing and {0} cannot continue.\n\n" +
                                    "    {1}",
                                    Application.ProductName, (error as FileNotFoundException).FileName),
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            var errorSummary = error.GetType().FullName + ": " + error.Message;
                            var logFile = Path.Combine(settings.LoggingPath, settings.LoggingFilename);
                            var openTracker = MessageBox.Show(String.Format(
                                    "A fatal error has occured and {0} cannot continue.\n\n" +
                                    "    {1}\n\n" +
                                    "This error may be due to bad data or a bug. You can help improve {0} by reporting this error in our bug tracker at http://launchpad.net/or and attaching the log file {2}.\n\n" +
                                    ">>> Click OK to report this error on the {0} bug tracker <<<",
                                    Application.ProductName, errorSummary, logFile),
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
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
            UninitLoading();
        }

        /// <summary>
        /// Run the specified activity from the beginning.
        /// This is the start for MSTS Activity or Explorer mode or Timetable mode
        /// </summary>
        void Start(UserSettings settings, string acttype, string[] args)
        {
            InitSimulator(settings, args, "", acttype);

            switch (acttype)
            {
                case "timetable":
                    Simulator.StartTimetable(args, Game.LoaderProcess.CancellationToken);
                    break;

                default:
                    Simulator.Start(Game.LoaderProcess.CancellationToken);
                    break;
            }

            if (Client != null)
            {
                Client.Send((new MSGPlayer(UserName, Code, Simulator.conFileName, Simulator.patFileName, Simulator.Trains[0], 0, Simulator.Settings.AvatarURL)).ToString());
                // wait 5 seconds to see if you get a reply from server with updated position/consist data, else go on
               
                System.Threading.Thread.Sleep(5000);
                if (Simulator.Trains[0].jumpRequested)
                {
                    Simulator.Trains[0].UpdateRemoteTrainPos(0);
                }
                var cancellation = Game.LoaderProcess.CancellationToken;
                if (cancellation.IsCancellationRequested) return;
            }

            Viewer = new Viewer(Simulator, Game);

            Game.ReplaceState(new GameStateViewer3D(Viewer));
        }

        /// <summary>
        /// Save the current game state for later resume.
        /// </summary>
        [CallOnThread("Updater")]
        public static void Save()
        {
            if (MPManager.IsMultiPlayer() && !MPManager.IsServer()) return; //no save for multiplayer sessions yet
            if (ContainerManager.ActiveOperationsCounter > 0)
                // don't save if performing a container load/unload
            {
                Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("Game save is not allowed during container load/unload"));
                return;
            }

            // Prefix with the activity filename so that, when resuming from the Menu.exe, we can quickly find those Saves 
            // that are likely to match the previously chosen route and activity.
            // Append the current date and time, so that each file is unique.
            // This is the "sortable" date format, ISO 8601, but with "." in place of the ":" which are not valid in filenames.
            //var fileStem = String.Format("{0} {1} {2:yyyy'-'MM'-'dd HH'.'mm'.'ss}", Simulator.Activity != null ? Simulator.ActivityFileName :
            //    (!String.IsNullOrEmpty(Simulator.TimetableFileName) ? Simulator.RoutePathName + " " + Simulator.TimetableFileName : Simulator.RoutePathName),
            //    MPManager.IsMultiPlayer() && MPManager.IsServer() ? "$Multipl$ " : "" , DateTime.Now);

            var saveSet = new SaveSet();  // Sets the filestem for the set of Save files
            using (BinaryWriter outf = new BinaryWriter(new FileStream(UserSettings.UserDataFolder + "\\" + saveSet.FileStem + ".save", FileMode.Create, FileAccess.Write)))
            {
                // Save some version identifiers so we can validate on load.
                outf.Write(VersionInfo.Version);
                outf.Write(VersionInfo.Build);

                // Save heading data used in Menu.exe
                if (MPManager.IsMultiPlayer() && MPManager.IsServer())
                    outf.Write("$Multipl$");
                outf.Write(Simulator.RouteName);
                outf.Write(Simulator.PathName);

                outf.Write((int)Simulator.GameTime);
                outf.Write(DateTime.Now.ToBinary());
                outf.Write(Simulator.Trains[0].FrontTDBTraveller.TileX + (Simulator.Trains[0].FrontTDBTraveller.X / 2048));
                outf.Write(Simulator.Trains[0].FrontTDBTraveller.TileZ + (Simulator.Trains[0].FrontTDBTraveller.Z / 2048));
                outf.Write(Simulator.InitialTileX);
                outf.Write(Simulator.InitialTileZ);

                // Now save the data used by RunActivity.exe
                outf.Write(Arguments.Length);
                foreach (var argument in Arguments)
                    outf.Write(argument);
                outf.Write(Acttype);

                Simulator.Save(outf);
                Viewer.Save(outf, saveSet.FileStem);
                // Save multiplayer parameters
                if (MPManager.IsMultiPlayer() && MPManager.IsServer())
                    MPManager.OnlineTrains.Save (outf);

                Viewer.TrainCarOperationsWebpage.Save(outf);

                SaveEvaluation(outf);

                // Write out position within file so we can check when restoring.
                outf.Write(outf.BaseStream.Position);
            }

            LogLocation();

            // Having written .save file, write other files: .replay, .txt, .evaluation.txt

            // The Save command is the only command that doesn't take any action. It just serves as a marker.
            new SaveCommand(Simulator.Log, saveSet.FileStem);
            Simulator.Log.SaveLog(Path.Combine(UserSettings.UserDataFolder, saveSet.FileStem + ".replay"));

            // Copy the logfile to the save folder
            CopyLog(Path.Combine(UserSettings.UserDataFolder, saveSet.FileStem + ".txt"));

            // Copy the evaluation file to the save folder
            if (File.Exists(Program.EvaluationFilename))
                File.Copy(Program.EvaluationFilename, Path.Combine(UserSettings.UserDataFolder, saveSet.FileStem + ".evaluation.txt"), true); 
        }


        /// <summary>
        /// Append time and location to the log for potential use in an ACT file. E.g.:
        /// "Location ( -6112 15146 78.15 -672.81 10 )"
        /// </summary>
        private static void LogLocation()
        {
            var t = Simulator.Trains[0].FrontTDBTraveller;
            var location = $"Location ( {t.TileX} {t.TileZ} {t.X:F2} {t.Z:F2}";
            location += $" 10 )"; // Matches location if within this 10 meter radius
            var clockTime = FormatStrings.FormatTime(Simulator.ClockTime);
            Console.WriteLine($"\nSave after {(int)Simulator.GameTime} secs at {clockTime}, EventCategoryLocation = {location}");
        }        

        private static void SaveEvaluation(BinaryWriter outf)
        {
            outf.Write(ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding.Count);
            for (int i = 0; i < ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding.Count; i++)
            {
                outf.Write((string)ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding[i]);
            }
            outf.Write(Popups.TrackMonitor.DbfEvalOverSpeed);
            outf.Write(Popups.TrackMonitor.DbfEvalOverSpeedTimeS);
            outf.Write(Popups.TrackMonitor.DbfEvalIniOverSpeedTimeS);
            outf.Write(RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBmoving);
            outf.Write(RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBstopped);
            outf.Write(Simulation.Physics.Train.NumOfCouplerBreaks);
            outf.Write(Simulation.RollingStocks.MSTSLocomotive.DbfEvalFullTrainBrakeUnder8kmh);
            outf.Write(Simulation.RollingStocks.SubSystems.ScriptedTrainControlSystem.DbfevalFullBrakeAbove16kmh);
            outf.Write(Simulation.RollingStocks.TrainCar.DbfEvalTrainOverturned);
            outf.Write(Simulation.RollingStocks.TrainCar.DbfEvalTravellingTooFast);
            outf.Write(Simulation.RollingStocks.TrainCar.DbfEvalTravellingTooFastSnappedBrakeHose);
            outf.Write(Simulator.DbfEvalOverSpeedCoupling);
            outf.Write(Viewer.DbfEvalAutoPilotTimeS);
            outf.Write(Viewer.DbfEvalIniAutoPilotTimeS);
            outf.Write(Simulator.PlayerLocomotive.DistanceM + Popups.HelpWindow.DbfEvalDistanceTravelled);
            outf.Write(Viewer.DbfEvalAutoPilot);
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
            var saveFile = GetSaveFile(args);
            var versionAndBuild = new[] { "", "", "" };
            using (BinaryReader inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
            {
                try // Because Restore() methods may try to read beyond the end of an out of date file.
                {
                    versionAndBuild = GetValidSaveVersionAndBuild(settings, saveFile, inf);

                    var values = GetSavedValues(inf);
                    Acttype = values.acttype;
                    InitSimulator(settings, values.args, "Resume", values.acttype);
                    Simulator.Restore(inf, values.pathName, values.initialTileX, values.initialTileZ, Game.LoaderProcess.CancellationToken);
                    Viewer = new Viewer(Simulator, Game);
                    if (Client != null || Server != null)
                        if (Acttype == "activity") Simulator.GetPathAndConsist();
                    if (Client != null)
                    {
                        Client.Send((new MSGPlayer(UserName, Code, Simulator.conFileName, Simulator.patFileName, Simulator.Trains[0], 0, Simulator.Settings.AvatarURL)).ToString());
                    }
                    Viewer.Restore(inf);

                    if (MPManager.IsMultiPlayer() && MPManager.IsServer())
                        MPManager.OnlineTrains.Restore(inf);

                    WebServices.TrainCarOperationsWebpage.Restore(inf);

                    ResumeEvaluation(inf);

                    var restorePosition = inf.BaseStream.Position;
                    var savePosition = inf.ReadInt64();
                    if (restorePosition != savePosition)
                        throw new InvalidDataException("Saved game stream position is incorrect.");
                }
                catch (Exception error)
                {
                    if (versionAndBuild[2] == VersionInfo.VersionOrBuild)
                    {
                        // If the save version is the same as the program version, we can't be an incompatible save - it's just a bug.
                        throw;
                    }
                    else
                    {
                        var parsedSaveVersion = VersionInfo.ParseVersion(versionAndBuild[0]);
                        var parsedSettingVersion = VersionInfo.ParseVersion(settings.YoungestVersionFailedToRestore);
                        if (parsedSaveVersion > parsedSettingVersion)
                        {
                            settings.YoungestVersionFailedToRestore = versionAndBuild[0];
                            settings.Save("YoungestVersionFailedToRestore");
                            Trace.TraceInformation("YoungestVersionFailedToRestore set to Save version: {0}", versionAndBuild[0]);
                        }
                        // Rethrow the existing error if it is already an IncompatibleSaveException.
                        if (error is IncompatibleSaveException)
                            throw;
                        throw new IncompatibleSaveException(saveFile, versionAndBuild[2], error);
                    }
                }

                // Reload the command log
                Simulator.Log.LoadLog(Path.ChangeExtension(saveFile, "replay"));

                Game.ReplaceState(new GameStateViewer3D(Viewer));
            }
        }

        private static void ResumeEvaluation(BinaryReader infDbfEval)
        {
            int nDepartBeforeBoarding = infDbfEval.ReadInt32();
            for (int i = 0; i < nDepartBeforeBoarding; i++)
            {
                ActivityTaskPassengerStopAt.DbfEvalDepartBeforeBoarding.Add(infDbfEval.ReadString());
            }
            Popups.TrackMonitor.DbfEvalOverSpeed = infDbfEval.ReadInt32();
            Popups.TrackMonitor.DbfEvalOverSpeedTimeS = infDbfEval.ReadDouble();
            Popups.TrackMonitor.DbfEvalIniOverSpeedTimeS = infDbfEval.ReadDouble();
            RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBmoving = infDbfEval.ReadInt32();
            RollingStock.MSTSLocomotiveViewer.DbfEvalEBPBstopped = infDbfEval.ReadInt32();
            Simulation.Physics.Train.NumOfCouplerBreaks = infDbfEval.ReadInt32();
            Simulation.RollingStocks.MSTSLocomotive.DbfEvalFullTrainBrakeUnder8kmh = infDbfEval.ReadInt32();
            Simulation.RollingStocks.SubSystems.ScriptedTrainControlSystem.DbfevalFullBrakeAbove16kmh = infDbfEval.ReadInt32();
            Simulation.RollingStocks.TrainCar.DbfEvalTrainOverturned = infDbfEval.ReadInt32();
            Simulation.RollingStocks.TrainCar.DbfEvalTravellingTooFast = infDbfEval.ReadInt32();
            Simulation.RollingStocks.TrainCar.DbfEvalTravellingTooFastSnappedBrakeHose = infDbfEval.ReadInt32();
            Simulator.DbfEvalOverSpeedCoupling = infDbfEval.ReadInt32();
            Viewer.DbfEvalAutoPilotTimeS = infDbfEval.ReadDouble();
            Viewer.DbfEvalIniAutoPilotTimeS = infDbfEval.ReadDouble();
            Popups.HelpWindow.DbfEvalDistanceTravelled = infDbfEval.ReadSingle();
            Viewer.DbfEvalAutoPilot = infDbfEval.ReadBoolean();
        }

        /// <summary>
        /// Replay a saved game.
        /// </summary>
        void Replay(UserSettings settings, string[] args)
        {
            // If "-replay" also specifies a save file then use it
            // E.g. RunActivity.exe -replay "yard_two 2012-03-20 22.07.36"
            // else use most recently changed *.save
            // E.g. RunActivity.exe -replay

            // First use the .save file to extract the route and activity.
            string saveFile = GetSaveFile(args);
            using (BinaryReader inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
            {
                inf.ReadString();    // Revision
                inf.ReadString();    // Build
                savedValues values = GetSavedValues(inf);
                Acttype = values.acttype;
                InitSimulator(settings, values.args, "Replay", values.acttype);
                Simulator.Start(Game.LoaderProcess.CancellationToken);
                Viewer = new Viewer(Simulator, Game);
            }

            // Load command log to replay
            Simulator.ReplayCommandList = new List<ICommand>();
            string replayFile = Path.ChangeExtension(saveFile, "replay");
            Simulator.Log.LoadLog(replayFile);
            foreach (var c in Simulator.Log.CommandList)
            {
                Simulator.ReplayCommandList.Add(c);
            }
            Simulator.Log.CommandList.Clear();
            CommandLog.ReportReplayCommands(Simulator.ReplayCommandList);

            Game.ReplaceState(new GameStateViewer3D(Viewer));
        }

        /// <summary>
        /// Replay the last segment of a saved game.
        /// </summary>
        void ReplayFromSave(UserSettings settings, string[] args)
        {
            // E.g. RunActivity.exe -replay_from_save "yard_two 2012-03-20 22.07.36"
            var saveFile = GetSaveFile(args);

            // Find previous save file and then move commands to be replayed into replay list.
            var log = new CommandLog(null);
            var logFile = saveFile.Replace(".save", ".replay");
            log.LoadLog(logFile);
            var replayCommandList = new List<ICommand>();

            // Scan backwards to find previous saveFile (ignore any that user has deleted).
            var count = log.CommandList.Count;
            var previousSaveFile = "";
            for (int i = count - 2; // -2 so we skip over the final save command
                    i >= 0; i--)
            {
                var c = log.CommandList[i] as SaveCommand;
                if (c != null)
                {
                    var f = Path.Combine(UserSettings.UserDataFolder, c.FileStem);
                    if (!f.EndsWith(".save"))
                        f += ".save";
                    if (File.Exists(f))
                    {
                        previousSaveFile = f;
                        // Move commands after this to the replay command list.
                        for (var j = i + 1; j < count; j++)
                        {
                            replayCommandList.Add(log.CommandList[i + 1]);
                            log.CommandList.RemoveAt(i + 1);
                        }
                        break;
                    }
                }
            }
            if (previousSaveFile == "")
            {
                // No save file found so just replay from start
                replayCommandList.AddRange(log.CommandList);    // copy the commands before deleting them.
                log.CommandList.Clear();
                // But we have no args, so have to get these from the Save
                using (var inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
                {
                    inf.ReadString();    // Revision
                    inf.ReadString();    // Build
                    savedValues values = GetSavedValues(inf);
                    InitSimulator(settings, values.args, "Replay");
                }
                Simulator.Start(Game.LoaderProcess.CancellationToken);
                Viewer = new Viewer(Simulator, Game);
            }
            else
            {
                // Resume from previous SaveFile and then replay
                using (var inf = new BinaryReader(new FileStream(previousSaveFile, FileMode.Open, FileAccess.Read)))
                {
                    GetValidSaveVersionAndBuild(settings, saveFile, inf);

                    var values = GetSavedValues(inf);
                    InitSimulator(settings, values.args, "Resume", values.acttype);
                    Simulator.Restore(inf, values.pathName, values.initialTileX, values.initialTileZ, Game.LoaderProcess.CancellationToken);
                    Viewer = new Viewer(Simulator, Game);
                    Viewer.Restore(inf);
                }
            }

            // Now Simulator exists, link the log to it in both directions
            Simulator.Log = log;
            log.Simulator = Simulator;
            Simulator.ReplayCommandList = replayCommandList;
            CommandLog.ReportReplayCommands(Simulator.ReplayCommandList);

            Game.ReplaceState(new GameStateViewer3D(Viewer));
        }

        static string[] GetValidSaveVersionAndBuild(UserSettings settings, string saveFile, BinaryReader inf)
        {
            var version = inf.ReadString().Replace("\0", ""); // e.g. "0.9.0.1648" or "X1321" or "" (if compiled locally)
            var build = inf.ReadString().Replace("\0", ""); // e.g. 0.0.5223.24629 (2014-04-20 13:40:58Z)
            var versionOrBuild = version.Length > 0 ? version : build;
            var valid = VersionInfo.GetValidity(version, build, settings.YoungestVersionFailedToRestore);
            if (valid == false) // This is usually detected in ResumeForm.cs but a Resume can also be launched from the command line.
                throw new IncompatibleSaveException(saveFile, versionOrBuild);
            if (valid == null)
            {
                //<CJComment> Cannot make this multi-language using Viewer.Catalog as Viewer is still null. </CJCOmment>
                Trace.TraceWarning("Restoring from a save made by version {1}\n"
                    + "of {0} may be incompatible with current version {2}.\n"
                    + "Please do not report any problems that may result.\n",
                    Application.ProductName, versionOrBuild, VersionInfo.VersionOrBuild);
            }
            return new[] { version, build, versionOrBuild };
        }

        /// <summary>
        /// Tests that RunActivity.exe can launch a specific activity or explore.
        /// </summary>
        void Test(UserSettings settings, string[] args)
        {
            var startTime = DateTime.Now;
            var exitGameState = new GameStateViewer3DTest(args);
            try
            {
                InitSimulator(settings, args, "Test");
                Simulator.Start(Game.LoaderProcess.CancellationToken);
                Viewer = new Viewer(Simulator, Game);
                Game.ReplaceState(exitGameState);
                Game.PushState(new GameStateViewer3D(Viewer));
                exitGameState.LoadTime = (DateTime.Now - startTime).TotalSeconds - Viewer.RealTime;
                exitGameState.Passed = true;
            }
            catch
            {
                Game.ReplaceState(exitGameState);
            }
        }

        class GameStateViewer3DTest : GameState
        {
            public bool Passed;
            public double LoadTime;

            readonly string[] Args;

            public GameStateViewer3DTest(string[] args)
            {
                Args = args;
            }

            internal override void Load()
            {
                Game.PopState();
            }

            internal override void Dispose()
            {
                ExportTestSummary(Game.Settings, Args, Passed, LoadTime);
                Environment.ExitCode = Passed ? 0 : 1;

                base.Dispose();
            }

            static void ExportTestSummary(UserSettings settings, string[] args, bool passed, double loadTime)
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
        }

        void InitLogging(UserSettings settings, string[] args)
        {
            InitLogging(settings, args, false);
        }

        void InitLogging(UserSettings settings, string[] args, bool appendLog)
        {
            if (settings.Logging && (settings.LoggingPath.Length > 0) && Directory.Exists(settings.LoggingPath))
            {
                string fileName;
                try
                {
                    fileName = string.Format(settings.LoggingFilename, Application.ProductName, VersionInfo.VersionOrBuild, VersionInfo.Version, VersionInfo.Build, DateTime.Now);
                }
                catch (FormatException)
                {
                    fileName = settings.LoggingFilename;
                }
                logFileName = GetFilePath(settings, fileName);

                // Ensure we start with an empty file.
                if (!appendLog)
                    File.Delete(logFileName);
                // Make Console.Out go to the log file AND the output stream.
                Console.SetOut(new FileTeeLogger(logFileName, Console.Out));
                // Make Console.Error go to the new Console.Out.
                Console.SetError(Console.Out);
            }

            // Captures Trace.Trace* calls and others and formats.
            ORTraceListener = new ORTraceListener(Console.Out, !settings.Logging);
            ORTraceListener.TraceOutputOptions = TraceOptions.Callstack;
            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(ORTraceListener);

            Console.WriteLine("This is a log file for {0}. Please include this file in bug reports.", Application.ProductName);
            LogSeparator();
            if (settings.Logging)
            {
                SystemInfo.WriteSystemDetails(Console.Out);
                LogSeparator();
                Console.WriteLine("Version    = {0}", VersionInfo.Version.Length > 0 ? VersionInfo.Version : "<none>");
                Console.WriteLine("Build      = {0}", VersionInfo.Build);
                if (logFileName.Length > 0)
                    Console.WriteLine("Logfile    = {0}", logFileName);
                Console.WriteLine("Executable = {0}", Path.GetFileName(ApplicationInfo.ProcessFile));
                foreach (var arg in args)
                    Console.WriteLine("Argument   = {0}", arg);

                string debugArgline = "";
                foreach (var arg in args)
                {
                    if (arg.Contains(" ")) 
                    {
                        debugArgline += "\"" + arg + "\" ";
                    } 
                    else
                    {
                        debugArgline += arg + " ";
                    }
                 }
                Console.WriteLine("Arguments  = {0}", debugArgline.TrimEnd());

                LogSeparator();
                settings.Log();
                LogSeparator();
            }
            else
            {
                Console.WriteLine("Logging is disabled, only fatal errors will appear here.");
                LogSeparator();
            }
            InitEvaluation(settings);
        }

        /// <summary>
        /// Sanitises the user's filename, adds logging path (Windows Desktop by default) and deletes any file already existing.
        /// </summary>
        /// <param name="settings"></param>
        void InitEvaluation(UserSettings settings)
        {
            EvaluationFilename = GetFilePath(settings, settings.EvaluationFilename);

            // Ensure we start with an empty file.
            File.Delete(EvaluationFilename);
        }

        /// <summary>
        /// Sanitise user's filename and combine with logging path
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetFilePath(UserSettings settings, string fileName)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(ch, '.');

            return Path.Combine(settings.LoggingPath, fileName);
        }

        #region Loading progress indication calculations

        const int LoadingSampleCount = 100;

        string LoadingDataKey;
        string LoadingDataFilePath;
        long LoadingBytesInitial;
        int LoadingTime;
        DateTime LoadingStart;
        long[] LoadingBytesExpected;
        List<long> LoadingBytesActual;
        TimeSpan LoadingBytesSampleRate;
        DateTime LoadingNextSample = DateTime.MinValue;
        float LoadedPercent = -1;

        void InitLoading(UserSettings settings, string[] args)
        {
            // Get the initial bytes; this is subtracted from all further uses of GetProcessBytesLoaded().
            LoadingBytesInitial = GetProcessBytesLoaded();

            // We hash together all the appropriate arguments to the program as the key for the loading cache file.
            // Arguments without a '.' in them and those starting '/' are ignored, since they are explore activity
            // configuration (time, season, etc.) or flags like /test which we don't want to change on.
            LoadingDataKey = String.Join(" ", args.Where(a => a.Contains('.') && !a.StartsWith("-") && !a.StartsWith("/"))).ToLowerInvariant();
            LoadingDataFilePath = settings.GetCacheFilePath("Load", LoadingDataKey);

            var loadingTime = 0;
            var bytesExpected = new long[LoadingSampleCount];
            var bytesActual = new List<long>(LoadingSampleCount);
            // The loading of the cached data doesn't matter if anything goes wrong; we'll simply have no progress bar.
            try
            {
                using (var data = File.OpenRead(LoadingDataFilePath))
                {
                    using (var reader = new BinaryReader(data))
                    {
                        reader.ReadString();
                        loadingTime = reader.ReadInt32();
                        for (var i = 0; i < LoadingSampleCount; i++)
                            bytesExpected[i] = reader.ReadInt64();
                    }
                }
            }
            catch { }

            LoadingTime = loadingTime;
            LoadingStart = DateTime.Now;
            LoadingBytesExpected = bytesExpected;
            LoadingBytesActual = bytesActual;
            // Using the cached loading time, pick a sample rate that will get us ~100 samples. Clamp to 100ms < x < 10,000ms.
            LoadingBytesSampleRate = new TimeSpan(0, 0, 0, 0, (int)MathHelper.Clamp(loadingTime / LoadingSampleCount, 100, 10000));
            LoadingNextSample = LoadingStart + LoadingBytesSampleRate;

#if DEBUG_LOADING
            Console.WriteLine("Loader: Cache key  = {0}", LoadingDataKey);
            Console.WriteLine("Loader: Cache file = {0}", LoadingDataFilePath);
            Console.WriteLine("Loader: Expected   = {0:N0} bytes", LoadingBytesExpected[LoadingSampleCount - 1]);
            Console.WriteLine("Loader: Sampler    = {0:N0} ms", LoadingBytesSampleRate);
            LogSeparator();
#endif
        }

        void UpdateLoading()
        {
            if (LoadingBytesActual == null)
                return;

            var bytes = GetProcessBytesLoaded() - LoadingBytesInitial;

            // Negative indicates no progress data; this happens if the loaded bytes exceeds the cached maximum expected bytes.
            LoadedPercent = -(float)(DateTime.Now - LoadingStart).TotalSeconds / 15;
            for (var i = 0; i < LoadingSampleCount; i++)
            {
                // Find the first expected sample with more bytes. This means we're currently in the (i - 1) to (i) range.
                if (bytes <= LoadingBytesExpected[i])
                {
                    // Calculate the position within the (i - 1) to (i) range using straight interpolation.
                    var expectedP = i == 0 ? 0 : LoadingBytesExpected[i - 1];
                    var expectedC = LoadingBytesExpected[i];
                    var index = i + (float)(bytes - expectedP) / (expectedC - expectedP);
                    LoadedPercent = index / LoadingSampleCount;
                    break;
                }
            }

            if (DateTime.Now > LoadingNextSample)
            {
                // Record a sample every time we should.
                LoadingBytesActual.Add(bytes);
                LoadingNextSample += LoadingBytesSampleRate;
            }
        }

        void UninitLoading()
        {
            if (LoadingDataKey == null)
                return;

            var loadingTime = DateTime.Now - LoadingStart;
            var bytes = GetProcessBytesLoaded() - LoadingBytesInitial;
            LoadingBytesActual.Add(bytes);

            // Convert from N samples to 100 samples.
            var bytesActual = new long[LoadingSampleCount];
            for (var i = 0; i < LoadingSampleCount; i++)
            {
                var index = (float)(i + 1) / LoadingSampleCount * (LoadingBytesActual.Count - 1);
                var indexR = index - Math.Floor(index);
                bytesActual[i] = (int)(LoadingBytesActual[(int)Math.Floor(index)] * indexR + LoadingBytesActual[(int)Math.Ceiling(index)] * (1 - indexR));
            }

            var bytesExpected = LoadingBytesExpected;
            var expected = bytesExpected[LoadingSampleCount - 1];
            var difference = bytes - expected;

            Console.WriteLine("Loader: Time       = {0:N0} ms", loadingTime.ToString());
            Console.WriteLine("Loader: Expected   = {0:N0} bytes", expected);
            Console.WriteLine("Loader: Actual     = {0:N0} bytes", bytes);
            Console.WriteLine("Loader: Difference = {0:N0} bytes ({1:P1})", difference, (float)difference / expected);
#if DEBUG_LOADING
            for (var i = 0; i < LoadingSampleCount; i++)
                Console.WriteLine("Loader: Sample {0,2}  = {1,13:N0} / {2,13:N0} ({3:N0})", i, bytesExpected[i], bytesActual[i], bytesActual[i] - bytesExpected[i]);
#endif
            Console.WriteLine();

            // Smoothly move all expected values towards actual values, by 10% each run. First run will just copy actual values.
            for (var i = 0; i < LoadingSampleCount; i++)
                bytesExpected[i] = bytesExpected[i] > 0 ? bytesExpected[i] * 9 / 10 + bytesActual[i] / 10 : bytesActual[i];

            // Like loading, saving the loading cache data doesn't matter if it fails. We'll just have no data to show progress with.
            try
            {
                using (var data = File.OpenWrite(LoadingDataFilePath))
                {
                    data.SetLength(0);
                    using (var writer = new BinaryWriter(data))
                    {
                        writer.Write(LoadingDataKey);
                        writer.Write((int)loadingTime.TotalMilliseconds);
                        for (var i = 0; i < LoadingSampleCount; i++)
                            writer.Write(bytesExpected[i]);
                    }
                }
            }
            catch { }
        }

        #endregion

        static void CopyLog(string toFile)
        {
            if (logFileName.Length == 0) return;
            File.Copy(logFileName, toFile, true);
        }

        void InitSimulator(UserSettings settings, string[] args, string mode)
        {
            InitSimulator(settings, args, mode, "");
        }

        void InitSimulator(UserSettings settings, string[] args, string mode, string acttype)
        {
            if (String.IsNullOrEmpty(acttype))
            {
                // old style processing without explicit action definition - to be removed later
                if (args.Length == 1)
                    acttype = "activity";
                else if (args.Length == 5)
                    acttype = "explorer";
            }

            Console.WriteLine(mode.Length <= 0 ? "Mode       = {1}" : acttype.Length > 0 ? "Mode       = {0}" : "Mode       = {0} {1}", mode, acttype);

            switch (acttype)
            {
                case "activity":
                    if (args.Length < 1) throw new InvalidCommandLine("Mode 'activity' needs 1 argument: activity file.");
                    Console.WriteLine("Route      = {0}", GetRouteName(args[0]));
                    Console.WriteLine("Activity   = {0} ({1})", GetActivityName(args[0]), args[0]);
                    break;

                case "explorer":
                case "exploreactivity":
                    if (args.Length < 5) throw new InvalidCommandLine("Mode 'explorer' needs 5 arguments: path file, consist file, time (hh[:mm[:ss]]), season (0-3), weather (0-2).");
                    Console.WriteLine("Route      = {0}", GetRouteName(args[0]));
                    Console.WriteLine("Path       = {0} ({1})", GetPathName(args[0]), args[0]);
                    Console.WriteLine("Consist    = {0} ({1})", GetConsistName(args[1]), args[1]);
                    Console.WriteLine("Time       = {0} ({1})", GetTime(args[2]), args[2]);
                    Console.WriteLine("Season     = {0} ({1})", GetSeason(args[3]), args[3]);
                    Console.WriteLine("Weather    = {0} ({1})", GetWeather(args[4]), args[4]);
                    break;

                case "timetable":
                    if (args.Length < 5) throw new InvalidCommandLine("Mode 'timetable' needs 5 arguments: timetable file, train name, day (???), season (0-3), weather (0-2).");
                    Console.WriteLine("File       = {0}", args[0]);
                    Console.WriteLine("Train      = {0}", args[1]);
                    Console.WriteLine("Day        = {0}", args[2]);
                    Console.WriteLine("Season     = {0} ({1})", GetSeason(args[3]), args[3]);
                    Console.WriteLine("Weather    = {0} ({1})", GetWeather(args[4]), args[4]);
                    break;

                default:
                    throw new InvalidCommandLine("Unexpected mode '" + acttype + "' with argument count " + args.Length);
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

            switch (acttype)
            {
                case "activity":
                    Simulator = new Simulator(settings, args[0], false);
                    if (LoadingScreen == null)
                        LoadingScreen = new LoadingScreenPrimitive(Game);
                    Simulator.SetActivity(args[0]);
                    break;

                case "explorer":
                    Simulator = new Simulator(settings, args[0], false);
                    if (LoadingScreen == null)
                        LoadingScreen = new LoadingScreenPrimitive(Game);
                    Simulator.SetExplore(args[0], args[1], args[2], args[3], args[4]);
                    break;

                case "exploreactivity":
                    Simulator = new Simulator(settings, args[0], false);
                    if (LoadingScreen == null)
                        LoadingScreen = new LoadingScreenPrimitive(Game);
                    Simulator.SetExploreThroughActivity(args[0], args[1], args[2], args[3], args[4]);
                    break;

                case "timetable":
                    Simulator = new Simulator(settings, args[0], true);
                    if (LoadingScreen == null)
                        LoadingScreen = new LoadingScreenPrimitive(Game);
                    if (String.Compare(mode, "start", true) != 0) // no specific action for start, handled in start_timetable
                    {
                        // for resume and replay : set timetable file and selected train info
                        Simulator.TimetableFileName = System.IO.Path.GetFileNameWithoutExtension(args[0]);
                        Simulator.PathName = args[1];
                        Simulator.IsAutopilotMode = true;
                    }
                    break;
            }

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
                    Trace.TraceWarning("Connection error - will play in single mode.");
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
                    Trace.TraceWarning("Connection error - will play in single mode.");
                    Client = null;
                }
            }
        }

        string GetRouteName(string path)
        {
            if (!HasExtension(path, ".act") && !HasExtension(path, ".pat"))
                return null;

            RouteFile trk = null;
            try
            {
                trk = new RouteFile(MSTS.MSTSPath.GetTRKFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))));
            }
            catch { }
            return trk?.Tr_RouteFile?.Name;
        }

        string GetActivityName(string path)
        {
            if (!HasExtension(path, ".act"))
                return null;
            
            ActivityFile act = null;
            try
            {
                act = new ActivityFile(path);
            }
            catch { }
            return act?.Tr_Activity?.Tr_Activity_Header?.Name;
        }

        string GetPathName(string path)
        {
            if (!HasExtension(path, ".pat"))
                return null;

            PathFile pat = null;
            try
            {
                pat = new PathFile(path);
            }
            catch { }
            return pat?.Name;
        }

        string GetConsistName(string path)
        {
            if (!HasExtension(path, ".con"))
                return null;

            ConsistFile con = null;
            try
            {
                con = new ConsistFile(path);
            }
            catch { }
            return con?.Name;
        }

        private bool HasExtension(string path, string ext) => Path.GetExtension(path).Equals(ext, StringComparison.OrdinalIgnoreCase);

        string GetTime(string timeString)
        {
            string[] time = timeString.Split(':');
            if (time.Length == 0)
                return null;

            string ts = null;
            try
            {
                ts = new TimeSpan(int.Parse(time[0]), time.Length > 1 ? int.Parse(time[1]) : 0, time.Length > 2 ? int.Parse(time[2]) : 0).ToString();
            }
            catch (ArgumentOutOfRangeException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            return ts;
        }

        string GetSeason(string season)
        {
            if (Enum.TryParse(season, out SeasonType value))
                return value.ToString();
            else
                return null;
        }

        string GetWeather(string weather)
        {
            if (Enum.TryParse(weather, out WeatherType value))
                return value.ToString();
            else
                return null;
        }

        void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }

        string GetSaveFile(string[] args)
        {
            if (args.Length == 0)
            {
                return GetMostRecentSave();
            }
            string saveFile = args[0];
            if (!saveFile.EndsWith(".save")) { saveFile += ".save"; }
            return Path.Combine(UserSettings.UserDataFolder, saveFile);
        }

        string GetMostRecentSave()
        {
            var directory = new DirectoryInfo(UserSettings.UserDataFolder);
            var file = directory.GetFiles("*.save")
             .OrderByDescending(f => f.LastWriteTime)
             .First();
            if (file == null) throw new FileNotFoundException(String.Format(
               Viewer.Catalog.GetString("Activity Save file '*.save' not found in folder {0}"), directory));
            return file.FullName;
        }

        savedValues GetSavedValues(BinaryReader inf)
        {
            savedValues values = default(savedValues);
            // Skip the heading data used in Menu.exe
            // Done so even if not elegant to be compatible with existing save files
            var routeNameOrMultipl = inf.ReadString();
            if (routeNameOrMultipl == "$Multipl$")
                inf.ReadString(); // Route name
            values.pathName = inf.ReadString();    // Path name
            inf.ReadInt32();     // Time elapsed in game (secs)
            inf.ReadInt64();     // Date and time in real world
            inf.ReadSingle();    // Current location of player train TileX
            inf.ReadSingle();    // Current location of player train TileZ

            // Read initial position and pass to Simulator so it can be written out if another save is made.
            values.initialTileX = inf.ReadSingle();  // Initial location of player train TileX
            values.initialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

            // Read in the real data...
            var savedArgs = new string[inf.ReadInt32()];
            for (var i = 0; i < savedArgs.Length; i++)
                savedArgs[i] = inf.ReadString();
            values.acttype = inf.ReadString();
            values.args = savedArgs;
            return values;
        }

        long GetProcessBytesLoaded()
        {
            NativeMathods.IO_COUNTERS counters;
            if (NativeMathods.GetProcessIoCounters(Process.GetCurrentProcess().Handle, out counters))
                return (long)counters.ReadTransferCount;

            return 0;
        }

        class LoadingPrimitive : RenderPrimitive, IDisposable
        {
            public readonly LoadingMaterial Material;
            readonly VertexBuffer VertexBuffer;

            public LoadingPrimitive(Game game)
            {
                Material = GetMaterial(game);
                var verticies = GetVerticies(game);
                VertexBuffer = new VertexBuffer(game.GraphicsDevice, typeof(VertexPositionTexture), verticies.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(verticies);
            }

            virtual protected LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingMaterial(game);
            }

            virtual protected VertexPositionTexture[] GetVerticies(Game game)
            {
                var dd = (float)Material.Texture.Width / 2;
                return new[] {
                    new VertexPositionTexture(new Vector3(-dd - 0.5f, +dd + 0.5f, -3), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(+dd - 0.5f, +dd + 0.5f, -3), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(-dd - 0.5f, -dd + 0.5f, -3), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(+dd - 0.5f, -dd + 0.5f, -3), new Vector2(1, 1)),
                };
            }

            public void Dispose()
            {
                Material.Dispose();
            }
            
            public override void Draw(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.SetVertexBuffer(VertexBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        class LoadingScreenPrimitive : LoadingPrimitive
        {
            public LoadingScreenPrimitive(Game game)
                : base(game)
            {
            }

            protected override LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingScreenMaterial(game);
            }

            protected override VertexPositionTexture[] GetVerticies(Game game)
            {
                float w, h;

                if (Material.Texture == null)
                {
                    w = h = 0;
                }
                else
                {
                    w = (float)Material.Texture.Width;
                    h = (float)Material.Texture.Height;
                    var scaleX = (float)game.RenderProcess.DisplaySize.X / w;
                    var scaleY = (float)game.RenderProcess.DisplaySize.Y / h;
                    var scale = scaleX < scaleY ? scaleX : scaleY;
                    w = w * scale / 2;
                    h = h * scale / 2;
                }
                return new[] {
                    new VertexPositionTexture(new Vector3(-w - 0.5f, +h + 0.5f, -2), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(+w - 0.5f, +h + 0.5f, -2), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(-w - 0.5f, -h + 0.5f, -2), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(+w - 0.5f, -h + 0.5f, -2), new Vector2(1, 1)),
                };
            }
        }

        class LoadingBarPrimitive : LoadingPrimitive
        {
            public LoadingBarPrimitive(Game game )
                : base(game)
            {
            }

            protected override LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingBarMaterial(game);
            }

            protected override VertexPositionTexture[] GetVerticies(Game game)
            {
                GetLoadingBarSize(game, out int w, out int h, out float x, out float y);
                return GetLoadingBarCoords(w, h, x, y);
            }

            protected VertexPositionTexture[] GetLoadingBarCoords(int w, int h, float x, float y)
            {
                return new[] {
                    new VertexPositionTexture(new Vector3(x + 0, -y - 0, -1), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(x + w, -y - 0, -1), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(x + 0, -y - h, -1), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(x + w, -y - h, -1), new Vector2(1, 1)),
                };
            }

            protected static void GetLoadingBarSize(Game game, out int w, out int h, out float x, out float y)
            {
                w = game.RenderProcess.DisplaySize.X;
                h = 10;
                x = -w / 2 - 0.5f;
                y = game.RenderProcess.DisplaySize.Y / 2 - h - 0.5f;
            }
        }

        class TimetableLoadingBarPrimitive : LoadingBarPrimitive
        {
            public TimetableLoadingBarPrimitive(Game game)
                : base(game)
            {
            }

            protected override VertexPositionTexture[] GetVerticies(Game game)
            {
                GetLoadingBarSize(game, out int w, out int h, out float x, out float y);
                y -= h + 1; // Allow for second bar and 1 pixel gap between
                return GetLoadingBarCoords(w, h, x, y);
            }
        }

        class LoadingMaterial : Material, IDisposable
        {
            public readonly LoadingShader Shader;
            public readonly Texture2D Texture;

            public LoadingMaterial(Game game)
                : base(null, null)
            {
                Shader = new LoadingShader(game.RenderProcess.GraphicsDevice);
                Texture = GetTexture(game);
            }

            public void Dispose()
            {
                Texture?.Dispose();
            }

            virtual protected Texture2D GetTexture(Game game)
            {
                return SharedTextureManager.LoadInternal(game.RenderProcess.GraphicsDevice, Path.Combine(game.ContentPath, "Loading.png"));
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                Shader.CurrentTechnique = Shader.Techniques["Loading"];
                Shader.LoadingTexture = Texture;

                graphicsDevice.BlendState = BlendState.NonPremultiplied;
            }

            public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
            {
                foreach (var item in renderItems)
                {
                    Shader.WorldViewProjection = item.XNAMatrix * XNAViewMatrix * XNAProjectionMatrix;
                    Shader.CurrentTechnique.Passes[0].Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            public override void ResetState(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.BlendState = BlendState.Opaque;
            }
        }

        class LoadingScreenMaterial : LoadingMaterial
        {
            public LoadingScreenMaterial(Game game)
                : base(game)
            {
            }

            private bool isWideScreen(Game game)
            {
                float x = game.RenderProcess.DisplaySize.X;
                float y = game.RenderProcess.DisplaySize.Y;

                return (x / y > 1.5);
            }

            protected override Texture2D GetTexture(Game game)
            {
                Texture2D texture;
                GraphicsDevice gd = game.RenderProcess.GraphicsDevice;
                string defaultScreen = "load.ace";

                string loadingScreen = Simulator.TRK.Tr_RouteFile.LoadingScreen;
                if (isWideScreen(game))
                {
                    string loadingScreenWide = Simulator.TRK.Tr_RouteFile.LoadingScreenWide;
                    loadingScreen = loadingScreenWide == null ? loadingScreen : loadingScreenWide;
                }
                loadingScreen = loadingScreen == null ? defaultScreen : loadingScreen;
                var path = Path.Combine(Simulator.RoutePath, loadingScreen);
                if (Path.GetExtension(path) == ".dds" && File.Exists(path))
                {
                    DDSLib.DDSFromFile(path, gd, true, out texture);
                }
                else if (Path.GetExtension(path) == ".ace")
                {
                    var alternativeTexture = Path.ChangeExtension(path, ".dds");

                    if (File.Exists(alternativeTexture))
                    {
                        DDSLib.DDSFromFile(alternativeTexture, gd, true, out texture);
                    }
                    else if (File.Exists(path))
                    {
                        texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(gd, path);
                    }
                    else
                    {
                        path = Path.Combine(Simulator.RoutePath, defaultScreen);
                        if (File.Exists(path))
                        {
                            texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(gd, path);
                        }
                        else
                        {
                            texture = null;
                        }
                    }

                }
                else
                {
                    texture = null;
                }
                return texture;
            }
        }

        class LoadingBarMaterial : LoadingMaterial
        {
            public LoadingBarMaterial(Game game)
                : base(game)
            {
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                base.SetState(graphicsDevice, previousMaterial);
                Shader.CurrentTechnique = Shader.Techniques["LoadingBar"];
            }
        }

        class TimetableLoadingBarMaterial : LoadingMaterial
        {
            public TimetableLoadingBarMaterial(Game game)
                : base(game)
            {
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                base.SetState(graphicsDevice, previousMaterial);
                Shader.CurrentTechnique = Shader.Techniques["LoadingBar"];
            }
        }

        class LoadingShader : Shader
        {
            readonly EffectParameter worldViewProjection;
            readonly EffectParameter loadingPercent;
            readonly EffectParameter loadingTexture;

            public Matrix WorldViewProjection { set { worldViewProjection.SetValue(value); } }

            public float LoadingPercent { set { loadingPercent.SetValue(value); } }

            public Texture2D LoadingTexture { set { loadingTexture.SetValue(value); } }

            public LoadingShader(GraphicsDevice graphicsDevice)
                : base(graphicsDevice, "Loading")
            {
                worldViewProjection = Parameters["WorldViewProjection"];
                loadingPercent = Parameters["LoadingPercent"];
                loadingTexture = Parameters["LoadingTexture"];
            }
        }

        static class NativeMathods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

            [StructLayout(LayoutKind.Sequential)]
            public struct IO_COUNTERS
            {
                public UInt64 ReadOperationCount;
                public UInt64 WriteOperationCount;
                public UInt64 OtherOperationCount;
                public UInt64 ReadTransferCount;
                public UInt64 WriteTransferCount;
                public UInt64 OtherTransferCount;
            };
        }
    }

    public sealed class IncompatibleSaveException : Exception
    {
        public readonly string SaveFile;
        public readonly string VersionOrBuild;

        public IncompatibleSaveException(string saveFile, string versionOrBuild, Exception innerException)
            : base(null, innerException)
        {
            SaveFile = saveFile;
            VersionOrBuild = versionOrBuild;
        }

        public IncompatibleSaveException(string saveFile, string versionOrBuild)
            : this(saveFile, versionOrBuild, null)
        {
        }
    }

    public sealed class InvalidCommandLine : Exception
    {
        public InvalidCommandLine(string message)
            : base(message)
        {
        }
    }
}
