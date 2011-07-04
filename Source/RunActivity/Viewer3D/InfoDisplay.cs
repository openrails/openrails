// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Popups;

namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        // Set this to the maximum number of columns that'll be used.
        const int ColumnCount = 9;

        // Set to distance from top-left corner to place text.
        const int TextOffset = 10;

        // Set to the distance from the above offset to place each column. Length must equal ColumnCount.
        static readonly int[] TextColumnOffsets = new[] {
            0,
            120,
            1 * 60,
            2 * 60,
            3 * 60,
            4 * 60,
            5 * 60,
            6 * 60,
            7 * 60,
        };

        // Name each column and use to access the Text and TextPosition arrays.
        enum Columns
        {
            Labels,
            BasicInfo,
            CarColumn1,
            CarColumn2,
            CarColumn3,
            CarColumn4,
            CarColumn5,
            CarColumn6,
            CarColumn7,
        }

        readonly Viewer3D Viewer;
        readonly StringBuilder[] TextColumns = new StringBuilder[ColumnCount];
        readonly TextPrimitive[] TextPrimitives = new TextPrimitive[ColumnCount];
        readonly Action[] TextPages;
        readonly DataLogger Logger = new DataLogger();

        bool DrawCarNumber = false;
        // F6 reveals labels for both sidings and platforms.
        // Booleans for both so they can also be used independently.
        bool DrawSiding = false;
        bool DrawPlatform = false;

		SpriteBatchMaterial TextMaterial; 
		ActivityInforMaterial DrawInforMaterial;

		int TextPage = 1;

		Matrix Matrix = Matrix.Identity;
        int FrameNumber = 0;
        double LastUpdateRealTime = 0;   // update text message only 10 times per second
		ElapsedTime ElapsedTime = new ElapsedTime();

        readonly int ProcessorCount = System.Environment.ProcessorCount;

		[StructLayout(LayoutKind.Sequential, Size = 40)]
		struct PROCESS_MEMORY_COUNTERS
		{
			public int cb;
			public int PageFaultCount;
			public int PeakWorkingSetSize;
			public int WorkingSetSize;
			public int QuotaPeakPagedPoolUsage;
			public int QuotaPagedPoolUsage;
			public int QuotaPeakNonPagedPoolUsage;
			public int QuotaNonPagedPoolUsage;
			public int PagefileUsage;
			public int PeakPagefileUsage;
		}

		[DllImport("psapi.dll", SetLastError = true)]
		static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, int size);

		[DllImport("kernel32.dll")]
		static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

		readonly IntPtr ProcessHandle;
		PROCESS_MEMORY_COUNTERS ProcessMemoryCounters;

		public InfoDisplay(Viewer3D viewer)
        {
			Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");
            Debug.Assert(TextColumnOffsets.Length == ColumnCount, "TextColumnOffsets must have ColumnCount entries.");
            Viewer = viewer;
			TextMaterial = (SpriteBatchMaterial)Materials.Load(Viewer.RenderProcess, "SpriteBatch");
			DrawInforMaterial = (ActivityInforMaterial)Materials.Load(Viewer.RenderProcess, "DrawInforMaterial");

            for (var i = 0; i < TextColumns.Length; i++)
            {
                TextColumns[i] = new StringBuilder();
                TextPrimitives[i] = new TextPrimitive(TextMaterial, new Point(TextOffset + TextColumnOffsets[i], TextOffset), Color.White, viewer.WindowManager.TextFontDefaultOutlined);
            }

            TextPages = new Action[] {
                TextPageCommon,
                TextPageEmpty,
                TextPageBrakeInfo,
				TextPageForceInfo,
                TextPageDispatcherInfo,
#if DEBUG
				TextPageDebugInfo,
#endif
            };

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
			ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { cb = 40 };

			if (Viewer.Settings.DataLogger)
				DataLoggerStart();
		}

		public void Stop()
		{
			if (Viewer.Settings.DataLogger)
				DataLoggerStop();
		}

        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommands.DisplayHUD))
                TextPage = (TextPage + 1) % TextPages.Length;

            if (UserInput.IsPressed(UserCommands.DebugLogger))
            {
				Viewer.Settings.DataLogger = !Viewer.Settings.DataLogger;
				if (Viewer.Settings.DataLogger)
					DataLoggerStart();
				else
					DataLoggerStop();
            }
			if (UserInput.IsPressed(UserCommands.DisplayCarLabels))
				DrawCarNumber = !DrawCarNumber;
            if (UserInput.IsPressed(UserCommands.DisplayStationLabels)) {
                    // Cycles round 4 states
                    // none > both > sidings only > platforms only > none
                    // MSTS users will first see the 2 states they expect and then discover the extra two. 
                    if (DrawSiding == false && DrawPlatform == false) {
                        DrawSiding = true;
                        DrawPlatform = true;
                    } else {
                        if (DrawSiding == true && DrawPlatform == true) {
                        DrawSiding = false;
                        DrawPlatform = true;
                    } else {
                        if (DrawSiding == false && DrawPlatform == true) {
                        DrawSiding = true;
                        DrawPlatform = false;
                    } else {
                        DrawSiding = false;
                        DrawPlatform = false;
                    }
                    }
                }
            }
		}

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime) {
        FrameNumber++;
        ElapsedTime += elapsedTime;

        if (Viewer.RealTime - LastUpdateRealTime >= 0.25) {
        for (var i = 0; i < TextColumns.Length; i++)
            TextColumns[i].Length = 0;

        double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
        LastUpdateRealTime = Viewer.RealTime;
        Profile(elapsedRealSeconds);
        UpdateText(elapsedRealSeconds);
        ElapsedTime.Reset();
        }

        for (var i = 0; i < TextColumns.Length; i++) {
        TextPrimitives[i].Text = TextColumns[i].ToString();
        frame.AddPrimitive(TextPrimitives[i].Material, TextPrimitives[i], RenderPrimitiveGroup.Overlay, ref Matrix);
        }

        //Here's where the logger stores the data from each frame
        if (Viewer.Settings.DataLogger) {
        Logger.Data(Program.Version);
        Logger.Data(FrameNumber.ToString("F0"));
        Logger.Data(GetWorkingSetSize().ToString("F0"));
        Logger.Data(GC.GetTotalMemory(false).ToString("F0"));
        Logger.Data(GC.CollectionCount(0).ToString("F0"));
        Logger.Data(GC.CollectionCount(1).ToString("F0"));
        Logger.Data(GC.CollectionCount(2).ToString("F0"));
        Logger.Data(ProcessorCount.ToString("F0"));
        Logger.Data(Viewer.RenderProcess.FrameRate.Value.ToString("F0"));
        Logger.Data(Viewer.RenderProcess.FrameTime.Value.ToString("F4"));
        Logger.Data(Viewer.RenderProcess.FrameJitter.Value.ToString("F4"));
        Logger.Data(Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum().ToString("F0"));
        Logger.Data(Viewer.RenderProcess.PrimitivePerFrame.Sum().ToString("F0"));
        Logger.Data(Viewer.RenderProcess.Profiler.Wall.Value.ToString("F0"));
        Logger.Data(Viewer.UpdaterProcess.Profiler.Wall.Value.ToString("F0"));
        Logger.Data(Viewer.LoaderProcess.Profiler.Wall.Value.ToString("F0"));
        Logger.Data(Viewer.SoundProcess.Profiler.Wall.Value.ToString("F0"));
        Logger.Data(Viewer.Camera.TileX.ToString("F0"));
        Logger.Data(Viewer.Camera.TileZ.ToString("F0"));
        Logger.Data(Viewer.Camera.Location.X.ToString("F4"));
        Logger.Data(Viewer.Camera.Location.Y.ToString("F4"));
        Logger.Data(Viewer.Camera.Location.Z.ToString("F4"));
        Logger.End();
        }
        if (DrawCarNumber == true) {
        foreach (TrainCar tcar in Viewer.TrainDrawer.ViewableCars) {
        frame.AddPrimitive(DrawInforMaterial,
            new ActivityInforPrimitive(DrawInforMaterial, tcar),
                RenderPrimitiveGroup.World, ref Matrix);
        }

        //	UpdateCarNumberText(frame, elapsedTime);
        }
        if (DrawSiding == true || DrawPlatform == true) {
            foreach (WorldFile w in Viewer.SceneryDrawer.WorldFiles) {
                if (DrawSiding == true && w != null && w.sidings != null) {
                    foreach (SidingLabel sd in w.sidings) {
                        if (sd != null) frame.AddPrimitive(DrawInforMaterial,
                            new ActivityInforPrimitive(DrawInforMaterial, sd, Color.Coral),
                            RenderPrimitiveGroup.World, ref Matrix);
                    }
                }
                if (DrawPlatform == true && w != null && w.platforms != null) {
                    foreach (PlatformLabel pd in w.platforms) {
                        if (pd != null) frame.AddPrimitive(DrawInforMaterial,
                            new ActivityInforPrimitive(DrawInforMaterial, pd, Color.CornflowerBlue),
                            RenderPrimitiveGroup.World, ref Matrix);
                        }
                    }
                }
            }
        }

		void UpdateText(double elapsedRealSeconds)
        {
            if (TextPage > 0)
                TextPages[0]();
            if (TextPage > 1)
                TextPages[TextPage]();
        }

		void TextPageCommon()
        {
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            var playerTrain = Viewer.PlayerLocomotive.Train;
			var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100;
			var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
			var engineBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus();
			var dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
			var locomotiveStatus = Viewer.PlayerLocomotive.GetStatus();
			var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
			var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;

            TextColumns[(int)Columns.Labels].AppendLine("Version");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", Program.Version.Length > 0 ? Program.Version : Program.Build);
            TextColumns[(int)Columns.Labels].AppendLine("Time");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", FormattedTime(Viewer.Simulator.ClockTime));
            TextColumns[(int)Columns.Labels].AppendLine("Speed");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", TrackMonitorWindow.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric));
            TextColumns[(int)Columns.Labels].AppendLine("Direction");
            TextColumns[(int)Columns.BasicInfo].AppendFormat(showMUReverser ? "{1:F0} {0}\n" : "{0}\n", Viewer.PlayerLocomotive.Direction, Math.Abs(playerTrain.MUReverserPercent));
            TextColumns[(int)Columns.Labels].AppendLine("Throttle");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}%\n", Viewer.PlayerLocomotive.ThrottlePercent);
            TextColumns[(int)Columns.Labels].AppendLine("Train Brake");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", Viewer.PlayerLocomotive.GetTrainBrakeStatus());
			if (showRetainers)
			{
                TextColumns[(int)Columns.Labels].AppendLine("Retainers");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}% {1}\n", playerTrain.RetainerPercent, playerTrain.RetainerSetting);
			}
			if (engineBrakeStatus != null)
			{
                TextColumns[(int)Columns.Labels].AppendLine("Engine Brake");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", engineBrakeStatus);
			}
			if (dynamicBrakeStatus != null)
			{
                TextColumns[(int)Columns.Labels].AppendLine("Dynamic Brake");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", dynamicBrakeStatus);
			}
			if (locomotiveStatus != null)
			{
                var lines = locomotiveStatus.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { " = " }, 2, StringSplitOptions.None);
                    TextColumns[(int)Columns.Labels].AppendLine(parts[0]);
                    TextColumns[(int)Columns.BasicInfo].AppendLine(parts.Length > 1 ? parts[1] : "");
                }
			}
            TextColumns[(int)Columns.Labels].AppendLine("Coupler Slack");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F2} m ({1} pulling, {2} pushing) {3}\n", playerTrain.TotalCouplerSlackM, playerTrain.NPull, playerTrain.NPush, stretched ? "Stretched" : bunched ? "Bunched" : "");
            TextColumns[(int)Columns.Labels].AppendLine("Coupler Force");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", playerTrain.MaximumCouplerForceN);

            locomotiveStatus = Viewer.Simulator.AI.GetStatus();
			if (locomotiveStatus != null)
			{
                TextColumns[(int)Columns.Labels].Append(locomotiveStatus);
			}

            TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();

            TextColumns[(int)Columns.Labels].AppendLine("FPS");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}\n", Viewer.RenderProcess.FrameRate.SmoothedValue);

            TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();

            if (Viewer.PlayerLocomotive.WheelSlip)
                TextColumns[(int)Columns.Labels].AppendLine("Wheel Slip");
            else
                TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();

            if ((mstsLocomotive != null) && mstsLocomotive.LocomotiveAxle.IsWheelSlipWarning)
                TextColumns[(int)Columns.Labels].AppendLine("Wheel Slip Warning");
            else
                TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();

            if (Viewer.PlayerLocomotive.GetSanderOn())
                TextColumns[(int)Columns.Labels].AppendLine("Sander On");
            else
                TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();
        }

        void TextPageEmpty()
        {
        }

		void TextPageBrakeInfo()
		{
            TextPageHeading("BRAKE INFORMATION");

            var train = Viewer.PlayerLocomotive.Train;
            TextColumns[(int)Columns.Labels].AppendLine("Main Reservoir");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} psi\n", train.BrakeLine2PressurePSI);
            for (var col = Columns.CarColumn1; col <= Columns.CarColumn7; col++)
                TextColumns[(int)col].AppendLine();

            var n = Math.Min(10, train.Cars.Count);
            for (var i = 0; i < n; i++)
            {
                var j = i == 0 ? 0 : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TextColumns[(int)Columns.Labels].AppendFormat("{0}\n", j + 1);
                var cols = car.BrakeSystem.GetDebugStatus();
                for (var col = Columns.CarColumn1; col <= Columns.CarColumn7; col++)
                    if ((int)(col - Columns.CarColumn1) < cols.Length)
                        TextColumns[(int)col].AppendLine(cols[(int)(col - Columns.CarColumn1)]);
                    else
                        TextColumns[(int)col].AppendLine();
            }
		}

		void TextPageForceInfo()
		{
            TextPageHeading("FORCE INFORMATION");

			var train = Viewer.PlayerLocomotive.Train;
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            if (mstsLocomotive != null)
            {
                TextColumns[(int)Columns.Labels].AppendLine("Wheel slip");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}%/s)\n", mstsLocomotive.LocomotiveAxle.SlipSpeedPercent, mstsLocomotive.LocomotiveAxle.SlipDerivationPercentpS);
                TextColumns[(int)Columns.Labels].AppendLine("Axle drive force");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", mstsLocomotive.LocomotiveAxle.DriveForceN);
                TextColumns[(int)Columns.Labels].AppendLine("Axle brake force");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", mstsLocomotive.LocomotiveAxle.BrakeForceN);
                TextColumns[(int)Columns.Labels].AppendLine("Axle friction force");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", mstsLocomotive.LocomotiveAxle.DampingNs * mstsLocomotive.LocomotiveAxle.SlipDerivationMpSS);
                TextColumns[(int)Columns.Labels].AppendLine("Axle out force");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", mstsLocomotive.LocomotiveAxle.AxleForceN);
                TextColumns[(int)Columns.Labels].AppendLine();
                TextColumns[(int)Columns.BasicInfo].AppendLine();
                for (var i = 0; i < 6; i++)
                    for (var col = Columns.CarColumn1; col <= Columns.CarColumn7; col++)
                        TextColumns[(int)col].AppendLine();
            }

            TextColumns[(int)Columns.Labels].AppendLine("Car");
            TextColumns[(int)Columns.CarColumn1].AppendLine("Total");
            TextColumns[(int)Columns.CarColumn2].AppendLine("Motive");
            TextColumns[(int)Columns.CarColumn3].AppendLine("Friction");
            TextColumns[(int)Columns.CarColumn4].AppendLine("Gravity");
            TextColumns[(int)Columns.CarColumn5].AppendLine("Coupler");
            TextColumns[(int)Columns.CarColumn6].AppendLine("Mass");
            TextColumns[(int)Columns.CarColumn7].AppendLine("Notes");

            var n = Math.Min(10, train.Cars.Count);
            for (var i = 0; i < n; i++)
			{
                var j = i == 0 ? 0 : i * (train.Cars.Count - 1) / (n - 1);
                var car = train.Cars[j];
                TextColumns[(int)Columns.Labels].AppendFormat("{0}\n", j + 1);
                TextColumns[(int)Columns.CarColumn1].AppendFormat("{0:F0}\n", car.TotalForceN);
                TextColumns[(int)Columns.CarColumn2].AppendFormat("{0:F0}\n", car.MotiveForceN);
                TextColumns[(int)Columns.CarColumn3].AppendFormat("{0:F0}\n", car.FrictionForceN);
                TextColumns[(int)Columns.CarColumn4].AppendFormat("{0:F0}\n", car.GravityForceN);
                TextColumns[(int)Columns.CarColumn5].AppendFormat("{0:F0}\n", car.CouplerForceU);
                TextColumns[(int)Columns.CarColumn6].AppendFormat("{0:F0}\n", car.MassKG);
                TextColumns[(int)Columns.CarColumn7].AppendFormat("{0}\n", car.Flipped ? "Flipped" : "");
			}
		}

		void TextPageDispatcherInfo()
		{
            TextPageHeading("DISPATCHER INFORMATION");

            TextColumns[(int)Columns.Labels].AppendLine("Train");
            TextColumns[(int)Columns.CarColumn1].AppendLine("Speed");
            TextColumns[(int)Columns.CarColumn2].AppendLine("Signal Aspect");
            TextColumns[(int)Columns.CarColumn4].AppendLine("Distance");
            TextColumns[(int)Columns.CarColumn5].AppendLine("Path");

            foreach (TrackAuthority auth in Program.Simulator.AI.Dispatcher.TrackAuthorities)
			{
                var status = auth.GetStatus();
                TextColumns[(int)Columns.Labels].AppendLine(status.TrainID.ToString());
                TextColumns[(int)Columns.CarColumn1].AppendLine(TrackMonitorWindow.FormatSpeed(status.Train.SpeedMpS, Viewer.MilepostUnitsMetric));
                TextColumns[(int)Columns.CarColumn2].AppendLine(status.Train.GetNextSignalAspect().ToString());
                TextColumns[(int)Columns.CarColumn4].AppendLine(TrackMonitorWindow.FormatDistance(status.Train.distanceToSignal, Viewer.MilepostUnitsMetric));
                TextColumns[(int)Columns.CarColumn5].AppendLine(status.Path);
            }
		}

		void TextPageDebugInfo()
        {
            TextPageHeading("DEBUG INFORMATION");

            TextColumns[(int)Columns.Labels].AppendLine("Logging Enabled");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", Viewer.Settings.DataLogger);
            TextColumns[(int)Columns.Labels].AppendLine("Build");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0}\n", Program.Build);
            TextColumns[(int)Columns.Labels].AppendLine("Memory");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} MB (managed: {1:F0} MB, collections: {2:F0}/{3:F0}/{4:F0})\n", GetWorkingSetSize() / 1024 / 1024, GC.GetTotalMemory(false) / 1024 / 1024, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
            TextColumns[(int)Columns.Labels].AppendLine("CPU");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1} logical processors)\n", (Viewer.RenderProcess.Profiler.CPU.SmoothedValue + Viewer.UpdaterProcess.Profiler.CPU.SmoothedValue + Viewer.LoaderProcess.Profiler.CPU.SmoothedValue + Viewer.SoundProcess.Profiler.CPU.SmoothedValue) / ProcessorCount, ProcessorCount);
            TextColumns[(int)Columns.Labels].AppendLine("GPU");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} FPS ({1:F1} \u00B1 {2:F1} ms, shader model {3})\n", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedValue * 1000, Viewer.RenderProcess.FrameJitter.SmoothedValue * 1000, Viewer.Settings.ShaderModel);
            TextColumns[(int)Columns.Labels].AppendLine("Adapter");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0} ({1:F0} MB)\n", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024);
			if (Viewer.Settings.DynamicShadows)
			{
                TextColumns[(int)Columns.Labels].AppendLine("Shadow Maps");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0} ({1}x{1})\n", String.Join(", ", Enumerable.Range(0, RenderProcess.ShadowMapCount).Select(i => String.Format("{0}m/{1}m", RenderProcess.ShadowMapDistance[i], RenderProcess.ShadowMapDiameter[i])).ToArray()), Viewer.Settings.ShadowMapResolution);
                TextColumns[(int)Columns.Labels].AppendLine("Shadow Primitives");
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} = {1}\n", Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum(), String.Join(" + ", Viewer.RenderProcess.ShadowPrimitivePerFrame.Select(p => p.ToString("F0")).ToArray()));
			}
            TextColumns[(int)Columns.Labels].AppendLine("Render Primitives");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} = {1}\n", Viewer.RenderProcess.PrimitivePerFrame.Sum(), String.Join(" + ", Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("F0")).ToArray()));
            TextColumns[(int)Columns.Labels].AppendLine("Render Process");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}% wait)\n", Viewer.RenderProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue);
            TextColumns[(int)Columns.Labels].AppendLine("Updater Process");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}% wait)\n", Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue, Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue);
            TextColumns[(int)Columns.Labels].AppendLine("Loader Process");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}% wait)\n", Viewer.LoaderProcess.Profiler.Wall.SmoothedValue, Viewer.LoaderProcess.Profiler.Wait.SmoothedValue);
            TextColumns[(int)Columns.Labels].AppendLine("Sound Process");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}% wait)\n", Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.SoundProcess.Profiler.Wait.SmoothedValue);
            TextColumns[(int)Columns.Labels].AppendLine("Total Process");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0}% ({1:F0}% wait)\n", Viewer.RenderProcess.Profiler.Wall.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue + Viewer.LoaderProcess.Profiler.Wall.SmoothedValue + Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue + Viewer.LoaderProcess.Profiler.Wait.SmoothedValue + Viewer.SoundProcess.Profiler.Wait.SmoothedValue);
            TextColumns[(int)Columns.Labels].AppendLine("Camera");
            TextColumns[(int)Columns.BasicInfo].AppendFormat("TileX:{0:F0} TileZ:{1:F0} X:{2:F4} Y:{3:F4} Z:{4:F4}\n", Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location.X, Viewer.Camera.Location.Y, Viewer.Camera.Location.Z);
        }

        void TextPageHeading(string name)
        {
            TextColumns[(int)Columns.Labels].AppendLine();
            TextColumns[(int)Columns.BasicInfo].AppendLine();
            TextColumns[(int)Columns.Labels].AppendLine(name);
            TextColumns[(int)Columns.BasicInfo].AppendLine();

            var lines = TextColumns[(int)Columns.Labels].ToString().Split('\n').Length;
            for (var col = Columns.CarColumn1; col <= Columns.CarColumn7; col++)
                for (var i = 1; i < lines; i++)
                    TextColumns[(int)col].AppendLine();
        }

		int GetWorkingSetSize()
		{
			// Get memory usage (working set).
			GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.cb);
			var memory = ProcessMemoryCounters.WorkingSetSize;
			return memory;
		}

        public static string FormattedTime(double clockTimeSeconds) //some measure of time so it can be sorted.  Good enuf for now. Might add more later. Okay
        {
            int hour = (int)(clockTimeSeconds / (60.0 * 60.0));
            clockTimeSeconds -= hour * 60.0 * 60.0;
            int minute = (int)(clockTimeSeconds / 60.0);
            clockTimeSeconds -= minute * 60.0;
            int seconds = (int)clockTimeSeconds;
            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

		static void DataLoggerStart()
		{
			using (StreamWriter file = File.AppendText("dump.csv"))
			{
				file.WriteLine(String.Join(",", new[] {
							"SVN",
							"Frame",
							"Memory",
							"Memory (Managed)",
							"Gen 0 GC",
							"Gen 1 GC",
							"Gen 2 GC",
							"Processors",
							"Frame Rate",
							"Frame Time",
							"Frame Jitter",
							"Shadow Primitives",
							"Render Primitives",
							"Render Process",
							"Updater Process",
							"Loader Process",
							"Sound Process",
							"Camera TileX",
							"Camera TileZ",
							"Camera X",
							"Camera Y",
							"Camera Z",
						}));
				file.Close();
			}
		}

		void DataLoggerStop()
		{
			Logger.Flush();
		}

		public void Profile(double elapsedRealSeconds) // should be called every 100mS
        {
            if (elapsedRealSeconds < 0.01)  // just in case
				return;

			Viewer.RenderProcess.Profiler.Mark();
			Viewer.UpdaterProcess.Profiler.Mark();
			Viewer.LoaderProcess.Profiler.Mark();
			Viewer.SoundProcess.Profiler.Mark();
        }
    }

    public class TextPrimitive : RenderPrimitive
    {
        public readonly SpriteBatchMaterial Material;
        public Point Position;
        public readonly Color Color;
        public readonly WindowTextFont Font;
        public string Text;

        public TextPrimitive(SpriteBatchMaterial material, Point position, Color color, WindowTextFont font)
        {
            Material = material;
            Position = position;
            Color = color;
            Font = font;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            Font.Draw(Material.SpriteBatch, Position, Text, Color);
        }
    }

	//2D straight lines
	public class LinePrimitive : RenderPrimitive
	{
		public readonly SpriteBatchLineMaterial Material;
		public Vector2 PositionStart; 
		public Vector2 PositionEnd;
		public readonly Color Color;
		public float Depth; //z buffer value: 0 always show, 1 always not show, in between, depends
		public int Width; //line width

		//constructor: startX, startY: X,Y of the start point, endXY the end point, depth: z buffer value (between 0, 1)
		public LinePrimitive(SpriteBatchLineMaterial material, float startX, float startY, float endX, float endY, Color color, float depth, int width)
		{
			Material = material;
			Color = color;
			Depth = depth;
			Width = width;
			PositionEnd = new Vector2(endX, endY);
			PositionStart = new Vector2(startX, startY);
		}
		public void UpdateLocation(float startX, float startY, float endX, float endY)
		{
			PositionEnd.X = endX;
			PositionEnd.Y = endY;
			PositionStart.X = startX;
			PositionStart.Y = startY;
		}

		//draw 2D straight lines
		public void DrawLine(SpriteBatch batch, Color color, Vector2 point1,
									Vector2 point2, float Layer)
		{
			float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
			float length = (point2 - point1).Length();

			batch.Draw(Material.Texture, point1, null, color,
					   angle, Vector2.Zero, new Vector2(length, Width),
					   SpriteEffects.None, Layer);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		public override void Draw(GraphicsDevice graphicsDevice)
		{
			DrawLine(Material.SpriteBatch, Color, PositionStart, PositionEnd, Depth);
		}

	}

	//2D straight lines
    public class ActivityInforPrimitive : RenderPrimitive {
        public readonly ActivityInforMaterial Material;
        public SpriteFont Font;
        public Viewer3D Viewer;
        TrainCar TrainCar = null;
        TrItemLabel TrItemLabel = null;
        Color LabelColor;
        float LineSpacing;

        //constructor: create one that draw car numbers
        public ActivityInforPrimitive(ActivityInforMaterial material, TrainCar tcar) {
            Material = material;
            Font = material.Font;
            Viewer = material.RenderProcess.Viewer;
            TrainCar = tcar;
            LineSpacing = Material.LineSpacing;
        }

        /// <summary>
        /// Information for showing labels of track items such as sidings and platforms
        /// </summary>
        public ActivityInforPrimitive(ActivityInforMaterial material, TrItemLabel pd, Color labelColor) {
            Material = material;
            Font = material.Font;
            Viewer = material.RenderProcess.Viewer;
            TrItemLabel = pd;
            LineSpacing = Material.LineSpacing;
            LabelColor = labelColor;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice) {
            if (TrainCar != null) UpdateCarNumberText();
            if (TrItemLabel != null) UpdateTrItemNameText();
        }

		//draw car numbers above train cars when F7 is hit
		void UpdateCarNumberText()
		{
			float X, BottomY, TopY;

			//find car location vs. camera
			Vector3 Location = TrainCar.WorldPosition.XNAMatrix.Translation +
					new Vector3((TrainCar.WorldPosition.TileX - Viewer.Camera.TileX) * 2048, 0, (-TrainCar.WorldPosition.TileZ + Viewer.Camera.TileZ) * 2048);

			//project 3D space to 2D (for the top of the line)
			Vector3 cameraVector = Viewer.GraphicsDevice.Viewport.Project(
				Location + new Vector3(0, TrainCar.Height, 0),
				Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);
			if (cameraVector.Z > 1 || cameraVector.Z < 0) return; //out of range or behind the camera
			X = cameraVector.X;
			BottomY = cameraVector.Y;//remember them

			////project for the top of the line
			cameraVector = Viewer.GraphicsDevice.Viewport.Project(
				Location + new Vector3(0, 10, 0),
				Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);

			//want to draw the train car name at cameraVector.Y, but need to check if it overlap other texts in Material.AlignedTextB
			//and determine the new location if conflict occurs
			TopY = AlignVertical(cameraVector.Y, X, X + Font.MeasureString(TrainCar.CarID).X, LineSpacing, Material.AlignedTextA);

			//draw the car number with blue and white color 
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY - 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 0, TopY - 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY - 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY + 0), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY + 0), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X - 1, TopY + 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 0, TopY + 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X + 1, TopY + 1), Color.White);
			Material.SpriteBatch.DrawString(Font, TrainCar.CarID, new Vector2(X, TopY), Color.Blue);

			//draw the vertical line with length Math.Abs(cameraVector.Y + LineSpacing - BottomY)
			//the term LineSpacing is used so that the text is above the line head
			Material.SpriteBatch.Draw(Material.Texture, new Vector2(X, BottomY), null, Color.Blue,
					   (float)-Math.PI / 2, Vector2.Zero, new Vector2(Math.Abs(cameraVector.Y + LineSpacing - BottomY), 2),
					   SpriteEffects.None, cameraVector.Z);
		}

        /// <summary>
        /// When F6 is pressed, draws names above track items such as sidings and platforms.
        /// </summary>
        void UpdateTrItemNameText() {
        float X, BottomY, TopY;

        //loop through all wfile and each platform to draw platform names and lines

        //the location w.r.t. the camera
        Vector3 locationWRTCamera = TrItemLabel.Location.WorldLocation.Location + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 0, (TrItemLabel.Location.TileZ - Viewer.Camera.TileZ) * 2048);

        //if the platform is out of viewing range
        if (!Viewer.Camera.InFOV(locationWRTCamera, 10)) return;

        //project 3D space to 2D (for the bottom of the line)
        Vector3 cameraVector = Viewer.GraphicsDevice.Viewport.Project(
            TrItemLabel.Location.XNAMatrix.Translation + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 0, (-TrItemLabel.Location.TileZ + Viewer.Camera.TileZ) * 2048),
            Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);
        if (cameraVector.Z > 1 || cameraVector.Z < 0) return; //out of range or behind the camera
        X = cameraVector.X;
        BottomY = cameraVector.Y;//remember them

        ////project for the top of the line
        cameraVector = Viewer.GraphicsDevice.Viewport.Project(
            TrItemLabel.Location.XNAMatrix.Translation + new Vector3((TrItemLabel.Location.TileX - Viewer.Camera.TileX) * 2048, 20, (-TrItemLabel.Location.TileZ + Viewer.Camera.TileZ) * 2048),
            Viewer.Camera.XNAProjection, Viewer.Camera.XNAView, Matrix.Identity);

        //want to draw the text at cameraVector.Y, but need to check if it overlap other texts in Material.AlignedTextB
        //and determine the new location if conflict occurs
        TopY = AlignVertical(cameraVector.Y, X, X + Font.MeasureString(TrItemLabel.ItemName).X, LineSpacing, Material.AlignedTextB);

        //outline the siding/platform name in white by pre-drawing all 8 points of compass
        //Isn't this a clumsy way to do it?
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 0, TopY + 1), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY + 1), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY + 0), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 1, TopY - 1), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X + 0, TopY - 1), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY - 1), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY - 0), Color.White);
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X - 1, TopY + 1), Color.White);
        //draw the siding/platform name in colour
        Material.SpriteBatch.DrawString(Font, TrItemLabel.ItemName, new Vector2(X, TopY), LabelColor);

        //draw a vertical line with length TopY + LineSpacing - BottomY
        //the term LineSpacing is used so that the text is above the line head
        Material.SpriteBatch.Draw(Material.Texture, new Vector2(X, BottomY), null, LabelColor,
                   -(float)Math.PI / 2, Vector2.Zero, new Vector2(Math.Abs(TopY + LineSpacing - BottomY), 2),
                   SpriteEffects.None, cameraVector.Z);

        }
        
        //helper function to make the train car and siding name align nicely on the screen
		//the basic idea is to space the screen vertically as table cell, each cell holds a list of text assigned.
		//new text in will check its destinated cell, if it overlap with a text in the cell, it will move up a cell and continue
		//once it is determined in a cell, it will be pushed in the list of text of that cell, and the new Y will be returned.
		float AlignVertical(float wantY, float startX, float endX, float spacing, List<Vector2>[] alignedTextY)
		{
			if (alignedTextY == null || wantY < 0) return wantY; //data checking
			int position = (int)(wantY / spacing);//the cell of the text it wants in
			if (position > alignedTextY.Length) return wantY;//position is larger than the number of cells
			int desiredPosition = position;
			while (position < alignedTextY.Length && position >= 0)
			{
				if (alignedTextY[position].Count == 0)
				{
					alignedTextY[position].Add(new Vector2(startX, endX));//add info for the text (i.e. start and end location)
					if (position == desiredPosition) return wantY; //if it can be in the desired cell, use the desired Y instead of the cell Y, so the text won't jump up-down
					else return position * spacing;//the cell location is the new Y
				}
				bool conflict = false;
				//check if it is intersect any one in the cell
				foreach (Vector2 v in alignedTextY[position])
				{
					//check conflict with a text, v.x is the start of the text, v.y is the end of the text
					if ((startX > v.X && startX < v.Y) || (endX > v.X && endX < v.Y) || (v.X > startX && v.X < endX) || (v.Y > startX && v.Y < endX))
					{
						conflict = true;
						break;
					}
				}
				if (conflict == false) //no conflict
				{
					alignedTextY[position].Add(new Vector2(startX, endX));//add info for the text (i.e. start and end location)
					if (position == desiredPosition) return wantY;
					else return position * spacing;//the cell location is the new Y
				}
				position--;
			}
			if (position == desiredPosition) return wantY;
			else return position * spacing;//the cell location is the new Y
		}

	}
}