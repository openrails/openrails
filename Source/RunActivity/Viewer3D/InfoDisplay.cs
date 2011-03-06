/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Popups;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ORTS.Common;

namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        readonly StringBuilder TextBuilder = new StringBuilder();
        readonly DataLogger Logger = new DataLogger();
        readonly TextPrimitive TextPrimitive;
        readonly Viewer3D Viewer;
		Matrix Matrix = Matrix.Identity;
		int InfoAmount = 1;
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
            Viewer = viewer;
			var material = (SpriteBatchMaterial)Materials.Load(Viewer.RenderProcess, "SpriteBatch");
			TextPrimitive = new TextPrimitive(material, new Vector2(10, 10), Color.White, 0.25f, Color.Black);
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
            {
                ++InfoAmount;
                if (InfoAmount > 5)
                    InfoAmount = 0;
            }
            if (UserInput.IsPressed(UserCommands.GameLogger))
            {
				Viewer.Settings.DataLogger = !Viewer.Settings.DataLogger;
				if (Viewer.Settings.DataLogger)
					DataLoggerStart();
				else
					DataLoggerStop();
            }
        }

		/// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
			FrameNumber++;
			ElapsedTime += elapsedTime;
			UpdateDialogs(elapsedTime);

			if (Viewer.RealTime - LastUpdateRealTime >= 0.25)
			{
				double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
				LastUpdateRealTime = Viewer.RealTime;
				Profile(elapsedRealSeconds);
				UpdateDialogsText(ElapsedTime);
				UpdateText(elapsedRealSeconds);
				ElapsedTime.Reset();
			}

            TextPrimitive.Text = TextBuilder.ToString();
            frame.AddPrimitive(TextPrimitive.Material, TextPrimitive, RenderPrimitiveGroup.Overlay, ref Matrix);

			//Here's where the logger stores the data from each frame
			if (Viewer.Settings.DataLogger)
			{
				Logger.Data(Program.Revision);
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

		public void UpdateText(double elapsedRealSeconds)
        {
            TextBuilder.Length = 0;

            if (InfoAmount > 0)
            {
                AddBasicInfo();
            }
            if (InfoAmount == 2)
            {
                AddBrakeInfo();
            }
			if (InfoAmount == 3)
			{
				AddForceInfo();
			}
            if (InfoAmount == 4)
            {
                AddDispatcherInfo();
            }
			if (InfoAmount == 5)
            {
				AddDebugInfo(elapsedRealSeconds);
            }
        }

        private void AddBasicInfo()
        {
            var playerTrain = Viewer.PlayerLocomotive.Train;
			var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100;
			var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
			var engineBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus();
			var dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
			var locomotiveStatus = Viewer.PlayerLocomotive.GetStatus();
			var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
			var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;

			TextBuilder.AppendFormat("Version = {0}", Program.Revision); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Time = {0}", FormattedTime(Viewer.Simulator.ClockTime)); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Speed = {0}", TrackMonitorWindow.FormatSpeed(Viewer.PlayerLocomotive.SpeedMpS, Viewer.MilepostUnitsMetric)); TextBuilder.AppendLine();
            TextBuilder.AppendFormat(showMUReverser ? "Direction = {1:F0} {0}" : "Direction = {0}", Viewer.PlayerLocomotive.Direction, Math.Abs(playerTrain.MUReverserPercent)); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Throttle = {0:F0}%", Viewer.PlayerLocomotive.ThrottlePercent); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Train Brake = {0}", Viewer.PlayerLocomotive.GetTrainBrakeStatus()); TextBuilder.AppendLine();
			if (showRetainers)
			{
				TextBuilder.AppendFormat("Retainers = {0}% {1}", playerTrain.RetainerPercent, playerTrain.RetainerSetting); TextBuilder.AppendLine();
			}
			if (engineBrakeStatus != null)
			{
				TextBuilder.AppendFormat("Engine Brake = {0}", engineBrakeStatus); TextBuilder.AppendLine();
			}
			if (dynamicBrakeStatus != null)
			{
				TextBuilder.AppendFormat("Dynamic Brake = {0}", dynamicBrakeStatus); TextBuilder.AppendLine();
			}
			if (locomotiveStatus != null)
			{
				TextBuilder.AppendLine(locomotiveStatus);
			}
			TextBuilder.AppendFormat("Coupler Slack = {0:F2} m ({1} pulling, {2} pushing) {3}", playerTrain.TotalCouplerSlackM, playerTrain.NPull, playerTrain.NPush, stretched ? "Stretched" : bunched ? "Bunched" : ""); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Coupler Force = {0:F0} N", playerTrain.MaximumCouplerForceN); TextBuilder.AppendLine();

            locomotiveStatus = Viewer.Simulator.AI.GetStatus();
			if (locomotiveStatus != null)
			{
				TextBuilder.Append(locomotiveStatus);
			}
            TextBuilder.AppendLine();

			TextBuilder.AppendFormat("FPS = {0:F0}", Viewer.RenderProcess.FrameRate.SmoothedValue); TextBuilder.AppendLine();

            if (Viewer.PlayerLocomotive.WheelSlip)
                TextBuilder.AppendLine("Wheel Slip");
            if (Viewer.PlayerLocomotive.GetSanderOn())
                TextBuilder.AppendLine("Sander On");
        }

		private void AddBrakeInfo()
		{
			TextBuilder.AppendLine();
			TextBuilder.AppendLine("BRAKE INFORMATION");
			Train playerTrain = Viewer.PlayerLocomotive.Train;
			TextBuilder.Append("Main Res = "); TextBuilder.AppendLine(string.Format("{0:F0}", playerTrain.BrakeLine2PressurePSI));
			int n = playerTrain.Cars.Count;
			if (n > 10)
				n = 11;
			for (int i = 0; i < n; i++)
			{
				int j = i;
				if (playerTrain.Cars.Count > 10)
					j = i * playerTrain.Cars.Count / 10 + (i == 10 ? -1 : 0);
				TextBuilder.AppendFormat("Car {0:D2}: {1}", j + 1, playerTrain.Cars[j].BrakeSystem.GetStatus(2));
				TextBuilder.AppendLine();
			}
		}

		private void AddForceInfo()
		{
			TextBuilder.AppendLine();
			TextBuilder.AppendLine("FORCE INFORMATION");
			Train playerTrain = Viewer.PlayerLocomotive.Train;
			int n = playerTrain.Cars.Count;
			if (n > 10)
				n = 11;
			for (int i = 0; i < n; i++)
			{
				int j = i;
				if (playerTrain.Cars.Count > 10)
					j = i * playerTrain.Cars.Count / 10 + (i == 10 ? -1 : 0);
				TrainCar car = playerTrain.Cars[j];
				TextBuilder.AppendFormat("Car {0:D2}: {1:F0} {2:F0} {3:F0} {4:F0} {5:F0} {6:F0} {7}", j + 1, car.TotalForceN, car.MotiveForceN, car.FrictionForceN, car.GravityForceN, car.CouplerForceU, car.MassKG, car.Flipped ? "Flipped" : "");
				TextBuilder.AppendLine();
			}
		}

		private void AddDispatcherInfo()
		{
			TextBuilder.AppendLine();
			TextBuilder.AppendLine("DISPATCHER INFORMATION");
			foreach (TrackAuthority auth in Program.Simulator.AI.Dispatcher.TrackAuthorities)
			{
				TextBuilder.AppendLine(auth.GetStatus());
			}
		}

		[Conditional("DEBUG")]
		private void AddDebugInfo(double elapsedRealSeconds)
        {
            TextBuilder.AppendLine();
			TextBuilder.AppendLine("DEBUG INFORMATION");
			TextBuilder.AppendFormat("Logging Enabled = {0}", Viewer.Settings.DataLogger); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Build = {0}", Program.Build); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Memory = {0:F0} MB (managed: {1:F0} MB, collections: {2:F0}/{3:F0}/{4:F0})", GetWorkingSetSize() / 1024 / 1024, GC.GetTotalMemory(false) / 1024 / 1024, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("CPU = {0:F0}% ({1} logical processors)", (Viewer.RenderProcess.Profiler.CPU.SmoothedValue + Viewer.UpdaterProcess.Profiler.CPU.SmoothedValue + Viewer.LoaderProcess.Profiler.CPU.SmoothedValue + Viewer.SoundProcess.Profiler.CPU.SmoothedValue) / ProcessorCount, ProcessorCount); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("GPU = {0:F0} FPS ({1:F1} \u00B1 {2:F1} ms, shader model {3})", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedValue * 1000, Viewer.RenderProcess.FrameJitter.SmoothedValue * 1000, Viewer.Settings.ShaderModel); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Adapter = {0} ({1:F0} MB)", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024); TextBuilder.AppendLine();
			if (Viewer.Settings.DynamicShadows)
			{
				TextBuilder.AppendFormat("Shadow Maps = {0} ({1}x{1})", String.Join(", ", Enumerable.Range(0, RenderProcess.ShadowMapCount).Select(i => String.Format("{0}m/{1}m", RenderProcess.ShadowMapDistance[i], RenderProcess.ShadowMapDiameter[i])).ToArray()), Viewer.Settings.ShadowMapResolution); TextBuilder.AppendLine();
				TextBuilder.AppendFormat("Shadow Primitives = {0:F0} = {1}", Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum(), String.Join(" + ", Viewer.RenderProcess.ShadowPrimitivePerFrame.Select(p => p.ToString("F0")).ToArray())); TextBuilder.AppendLine();
			}
			TextBuilder.AppendFormat("Render Primitives = {0:F0} = {1}", Viewer.RenderProcess.PrimitivePerFrame.Sum(), String.Join(" + ", Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("F0")).ToArray())); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Render Process = {0:F0}% ({1:F0}% wait)", Viewer.RenderProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Updater Process = {0:F0}% ({1:F0}% wait)", Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue, Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Loader Process = {0:F0}% ({1:F0}% wait)", Viewer.LoaderProcess.Profiler.Wall.SmoothedValue, Viewer.LoaderProcess.Profiler.Wait.SmoothedValue); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Sound Process = {0:F0}% ({1:F0}% wait)", Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.SoundProcess.Profiler.Wait.SmoothedValue); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Total Process = {0:F0}% ({1:F0}% wait)", Viewer.RenderProcess.Profiler.Wall.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue + Viewer.LoaderProcess.Profiler.Wall.SmoothedValue + Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue + Viewer.LoaderProcess.Profiler.Wait.SmoothedValue + Viewer.SoundProcess.Profiler.Wait.SmoothedValue); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Camera: TileX:{0:F0} TileZ:{1:F0} X:{2:F4} Y:{3:F4} Z:{4:F4}", Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location.X, Viewer.Camera.Location.Y, Viewer.Camera.Location.Z); TextBuilder.AppendLine();
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

    } // Class Info Display

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