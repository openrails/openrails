// COPYRIGHT 2022 by the Open Rails project.
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
using System.Linq;
using Orts.Simulation;
using ORTS.Common;
using System.IO;
using Orts.Common;

namespace SimulatorTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = args.Where(a => a.StartsWith("-") || a.StartsWith("/")).Select(a => a.Substring(1));
            var files = args.Where(a => !a.StartsWith("-") && !a.StartsWith("/")).ToList();
            var settings = new UserSettings(options);

            if (files.Count != 1 || options.Contains("help", StringComparer.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("{0} {1}", ApplicationInfo.ApplicationName, VersionInfo.VersionOrBuild);
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  {0} [options] <SAVE_FILE>", Path.GetFileNameWithoutExtension(ApplicationInfo.ProcessFile));
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("  <SAVE_FILE>  {0} save file to use", ApplicationInfo.ProductName);
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  /quiet       Do not show summary of simulation (only exit code is set)");
                Console.WriteLine("  /verbose     Show version and settings (similar to a {0} log)", ApplicationInfo.ProductName);
                Console.WriteLine("  /fps <FPS>   Set the simulation frame-rate [default: 10]");
                Console.WriteLine("  /help        Show help and usage information");
                Console.WriteLine("  ...and any standard {0} option", ApplicationInfo.ProductName);
                Console.WriteLine();
                Console.WriteLine("The {0} takes a save file and:", ApplicationInfo.ApplicationName);
                Console.WriteLine("  - Loads the same activity as contained in the save file");
                Console.WriteLine("  - Runs the simulation at the specified FPS for the same duration as the save file");
                Console.WriteLine("  - Compares the final position with that contained in the save file");
                Console.WriteLine();
                Console.WriteLine("The exit code is set to the distance from the target in meters");
                Console.WriteLine();
                return;
            }

            if (settings.Verbose)
            {
                Console.WriteLine("This is a log file for {0}. Please include this file in bug reports.", ApplicationInfo.ProductName);
                LogSeparator();

                SystemInfo.WriteSystemDetails(Console.Out);
                LogSeparator();

                Console.WriteLine("Version      = {0}", VersionInfo.Version.Length > 0 ? VersionInfo.Version : "<none>");
                Console.WriteLine("Build        = {0}", VersionInfo.Build);
                Console.WriteLine("Executable   = {0}", Path.GetFileName(ApplicationInfo.ProcessFile));
                foreach (var arg in args)
                    Console.WriteLine("Argument     = {0}", arg);
                LogSeparator();

                settings.Log();
                LogSeparator();
            }

            var saveFile = files[0];
            using (BinaryReader inf = new BinaryReader(new FileStream(saveFile, FileMode.Open, FileAccess.Read)))
            {
                var cts = new CancellationTokenSource(() => { });
                var data = GetSaveData(inf);
                var activityFile = data.Args[0];

                if (!settings.Quiet)
                {
                    foreach (var arg in data.Args)
                        Console.WriteLine("Argument     = {0}", arg);
                    Console.WriteLine("Initial Pos  = {0}, {1}", data.InitialTileX, data.InitialTileZ);
                    Console.WriteLine("Expected Pos = {0}, {1}", data.ExpectedTileX, data.ExpectedTileZ);
                    Console.Write("Loading...   ");
                }

                var startTime = DateTimeOffset.Now;
                var simulator = new Simulator(settings, activityFile, useOpenRailsDirectory: false, deterministic: true);
                simulator.SetActivity(activityFile);
                simulator.Start(cts.Token);
                simulator.SetCommandReceivers();
                simulator.Log.LoadLog(Path.ChangeExtension(saveFile, "replay"));
                simulator.ReplayCommandList = new List<ICommand>();
                simulator.ReplayCommandList.AddRange(simulator.Log.CommandList);
                simulator.Log.CommandList.Clear();

                var loadTime = DateTimeOffset.Now;
                if (!settings.Quiet)
                {
                    Console.WriteLine("{0:N1} seconds", (loadTime - startTime).TotalSeconds);
                    Console.Write("Replaying... ");
                }

                var step = 1f / settings.FPS;
                for (var tick = 0f; tick < data.TimeElapsed; tick += step)
                {
                    simulator.Update(step);
                    simulator.Log.Update(simulator.ReplayCommandList);
                }

                var endTime = DateTimeOffset.Now;
                var actualTileX = simulator.Trains[0].FrontTDBTraveller.TileX + (simulator.Trains[0].FrontTDBTraveller.X / WorldPosition.TileSize);
                var actualTileZ = simulator.Trains[0].FrontTDBTraveller.TileZ + (simulator.Trains[0].FrontTDBTraveller.Z / WorldPosition.TileSize);
                var initialToExpectedM = Math.Sqrt(Math.Pow(data.ExpectedTileX - data.InitialTileX, 2) + Math.Pow(data.ExpectedTileZ - data.InitialTileZ, 2)) * WorldPosition.TileSize;
                var expectedToActualM = Math.Sqrt(Math.Pow(actualTileX - data.ExpectedTileX, 2) + Math.Pow(actualTileZ - data.ExpectedTileZ, 2)) * WorldPosition.TileSize;

                if (!settings.Quiet)
                {
                    Console.WriteLine("{0:N1} seconds ({1:F0}x speed-up)", (endTime - loadTime).TotalSeconds, data.TimeElapsed / (endTime - loadTime).TotalSeconds);
                    Console.WriteLine("Actual Pos   = {0}, {1}", actualTileX, actualTileZ);
                    Console.WriteLine("Distance     = {0:N3} m ({1:P1})", expectedToActualM, 1 - expectedToActualM / initialToExpectedM);
                }

                Environment.ExitCode = (int)expectedToActualM;
            }
        }

        static void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }

        static SaveData GetSaveData(BinaryReader inf)
        {
            var values = new SaveData();

            inf.ReadString();  // Version
            inf.ReadString();  // Build

            var routeName = inf.ReadString();
            if (routeName == "$Multipl$")
            {
                inf.ReadString();  // Route name
            }

            inf.ReadString();  // Path name
            values.TimeElapsed = inf.ReadInt32();  // Time elapsed in game (secs)
            inf.ReadInt64();  // Date and time in real world

            values.ExpectedTileX = inf.ReadSingle();  // Current location of player train TileX
            values.ExpectedTileZ = inf.ReadSingle();  // Current location of player train TileZ

            values.InitialTileX = inf.ReadSingle();  // Initial location of player train TileX
            values.InitialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

            values.Args = new string[inf.ReadInt32()];
            for (var i = 0; i < values.Args.Length; i++)
                values.Args[i] = inf.ReadString();

            inf.ReadString();  // Activity type

            return values;
        }

        struct SaveData
        {
            public int TimeElapsed;
            public float InitialTileX;
            public float InitialTileZ;
            public float ExpectedTileX;
            public float ExpectedTileZ;
            public string[] Args;
        }
    }
}
