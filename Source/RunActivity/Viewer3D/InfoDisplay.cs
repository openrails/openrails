// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
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
			var material = (SpriteBatchMaterial)Materials.Load(Viewer.RenderProcess, "SpriteBatch");
            for (var i = 0; i < TextColumns.Length; i++)
            {
                TextColumns[i] = new StringBuilder();
                TextPrimitives[i] = new TextPrimitive(material, new Vector2(TextOffset + TextColumnOffsets[i], TextOffset), Color.White, 0.25f, Color.Black);
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
            if (UserInput.IsPressed(UserCommands.GameODS))
                TextPage = (TextPage + 1) % TextPages.Length;

            if (UserInput.IsPressed(UserCommands.GameLogger))
            {
				Viewer.Settings.DataLogger = !Viewer.Settings.DataLogger;
				if (Viewer.Settings.DataLogger)
					DataLoggerStart();
				else
					DataLoggerStop();
            }
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
			FrameNumber++;
			ElapsedTime += elapsedTime;
			UpdateDialogs(elapsedTime);

            if (Viewer.RealTime - LastUpdateRealTime >= 0.25)
			{
                for (var i = 0; i < TextColumns.Length; i++)
                    TextColumns[i].Length = 0;

                double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
				LastUpdateRealTime = Viewer.RealTime;
				Profile(elapsedRealSeconds);
				UpdateDialogsText(ElapsedTime);
				UpdateText(elapsedRealSeconds);
				ElapsedTime.Reset();
			}

            for (var i = 0; i < TextColumns.Length; i++)
            {
                TextPrimitives[i].Text = TextColumns[i].ToString();
                frame.AddPrimitive(TextPrimitives[i].Material, TextPrimitives[i], RenderPrimitiveGroup.Overlay, ref Matrix);
            }

			//Here's where the logger stores the data from each frame
			if (Viewer.Settings.DataLogger)
			{
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
		}

		void UpdateDialogs(ElapsedTime elapsedTime)
		{
			if (Viewer.CompassWindow.Visible)
			{
				var compassDir = new Vector2(Viewer.Camera.XNAView.M11, Viewer.Camera.XNAView.M13);
				var heading = Math.Acos(compassDir.X);
				if (compassDir.Y > 0) heading = 2 * Math.PI - heading;
				Viewer.CompassWindow.Update((float)heading);
			}

         if (Viewer.DriverAidWindow.Visible)
         {

            // update driver aid window - convert m/s to km/h, and take absolute so
            // speed is non-negative.
            float trainSpeed = Math.Abs(Viewer.PlayerTrain.SpeedMpS * 3.6f);


            // for now, use 120 = clear, 0 = anything else. 
            // TODO: get actual target speed of signal ahead. Currently, signals
            // clear automatically on their own so the by itself, the driver aid 
            // isn't showing all that much.
            int targetSpeed = 0;
            if (Viewer.PlayerTrain.TMaspect == TrackMonitorSignalAspect.Clear)
            {
               targetSpeed = 120;
            }

            // temporary: this shows what it would look like if you had to stop
            // at every signal, demonstrating stuff needed to get things working
            // inside the driver aid window
            targetSpeed = 20;

            float deceleration = 0.3f;

            

            float brakeCurveSpeed = BrakeCurves.ComputeCurve(Viewer.PlayerTrain.SpeedMpS, Viewer.PlayerTrain.distanceToSignal, targetSpeed / 3.6f, deceleration) * 3.6f;
            
            Viewer.DriverAidWindow.Update(trainSpeed, Viewer.PlayerTrain.distanceToSignal, targetSpeed, brakeCurveSpeed);
         }
		}

		void UpdateDialogsText(ElapsedTime elapsedTime)
		{
			Viewer.MessagesWindow.UpdateMessages();
            if (Viewer.HelpWindow.Visible)
            {
                Viewer.HelpWindow.UpdateText(elapsedTime);
            }
            if (Viewer.TrackMonitorWindow.Visible)
			{
				var poiDistance = 0f;
				var poiBackwards = false;
				var poiType = Viewer.Simulator.AI.Dispatcher.GetPlayerNextPOI(out poiDistance, out poiBackwards);
                Viewer.TrackMonitorWindow.UpdateText(elapsedTime, Viewer.MilepostUnitsMetric, Viewer.PlayerLocomotive.SpeedMpS, Viewer.PlayerTrain.distanceToSignal, Viewer.PlayerTrain.TMaspect, poiType, poiDistance);
			}
			else Viewer.TrackMonitorWindow.UpdateSpeed(Viewer.PlayerLocomotive.SpeedMpS); // always update last speed so that the projected speed will be right (By JTang)
			if (Viewer.SwitchWindow.Visible)
			{
				Viewer.SwitchWindow.UpdateText(elapsedTime, Viewer.PlayerTrain);
			}
			if (Viewer.TrainOperationsWindow.Visible)
			{
				Viewer.TrainOperationsWindow.UpdateText(elapsedTime, Viewer.PlayerTrain);
			}
			if (Viewer.NextStationWindow.Visible)
			{
				Viewer.NextStationWindow.UpdateText(elapsedTime, Viewer.Simulator.ClockTime, FormattedTime);
			}
            Viewer.NextStationWindow.UpdateSound();
			if (Viewer.CompassWindow.Visible)
			{
				double latitude = 0;
				double longitude = 0;
				new WorldLatLon().ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
				Viewer.CompassWindow.UpdateText((float)latitude, (float)longitude);
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
                TextColumns[(int)Columns.Labels].AppendLine(locomotiveStatus);
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
                TextColumns[(int)Columns.BasicInfo].AppendFormat("{0:F0} N\n", mstsLocomotive.LocomotiveAxle.FrictionForceN * mstsLocomotive.LocomotiveAxle.AxleSpeedMpS);
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
		public readonly Vector2 Position;
		public readonly Color Color;
		public readonly Color ShadowColor;
        public string Text;

		public TextPrimitive(SpriteBatchMaterial material, Vector2 position, Color color, float shadowStrength, Color shadowColor)
		{
			Material = material;
			Position = position;
			Color = color;
			ShadowColor = new Color(shadowColor, shadowStrength);
		}

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
			if (ShadowColor.A > 0.01f)
			{
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X - 1, Position.Y - 1), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X + 0, Position.Y - 1), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X + 1, Position.Y - 1), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X - 1, Position.Y + 0), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X + 1, Position.Y + 0), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X - 1, Position.Y + 1), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X + 0, Position.Y + 1), ShadowColor);
				Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(Position.X + 1, Position.Y + 1), ShadowColor);
			}
			Material.SpriteBatch.DrawString(Material.DefaultFont, Text, Position, Color);
        }
    }
}