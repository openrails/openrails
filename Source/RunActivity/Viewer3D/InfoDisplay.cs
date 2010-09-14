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

namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        readonly StringBuilder TextBuilder = new StringBuilder();
        readonly DataLogger Logger = new DataLogger();
        readonly TextPrimitive TextPrimitive = new TextPrimitive();
        readonly SpriteBatchMaterial Material;
        readonly Viewer3D Viewer;
		Matrix Matrix = Matrix.Identity;
		int InfoAmount = 1;
        int frameNum = 0;
        bool LoggerEnabled = false;
        double lastUpdateTime = 0;   // update text message only 10 times per second
		ElapsedTime ElapsedTime = new ElapsedTime();

        int processors = System.Environment.ProcessorCount;

        public InfoDisplay( Viewer3D viewer )
        {
            Viewer = viewer;
            // Create a new SpriteBatch, which can be used to draw text.
            Material = (SpriteBatchMaterial) Materials.Load( Viewer.RenderProcess, "SpriteBatch" );
            TextPrimitive.Material = Material;
            TextPrimitive.Color = Color.Yellow;
            TextPrimitive.Location = new Vector2(10, 10);
            //TextPrimitive.Sequence = RenderPrimitiveSequence.TextOverlay;
        }

		public void Stop()
		{
			if (LoggerEnabled)
			{
				Logger.Flush();
			}
		}

        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                ++InfoAmount;
                if (InfoAmount > 5)
                    InfoAmount = 0;
            }
            if (UserInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.F12))
            {
                LoggerEnabled = !LoggerEnabled;
				if (LoggerEnabled == false)
				{
					Logger.Flush();
				}
				else
				{
					using (StreamWriter file = File.AppendText("dump.csv"))
					{
						file.WriteLine("SVN,Frame,FPS,Frame Time,Frame Jitter,Primitives,State Changes,Image Changes,Processors,Render Process,Updater Process,Loader Process,Camera TileX, Camera TileZ, Camera Location");
						file.Close();
					}
				}
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
			frameNum++;
			ElapsedTime += elapsedTime;
			UpdateDialogs(elapsedTime);

            if (Program.RealTime - lastUpdateTime >= 0.25)
            {
                double elapsedRealSeconds = Program.RealTime - lastUpdateTime;
                lastUpdateTime = Program.RealTime;
                Profile(elapsedRealSeconds);
				UpdateDialogsText(ElapsedTime);
                UpdateText();

				ElapsedTime.Reset();
            }

            TextPrimitive.Text = TextBuilder.ToString();
            frame.AddPrimitive(Material, TextPrimitive, RenderPrimitiveGroup.Overlay, ref Matrix);

			//Here's where the logger stores the data from each frame
			if (LoggerEnabled)
			{
				Logger.Data(Program.Revision); //SVN Revision
				Logger.Data(frameNum.ToString()); //Frame Number
				Logger.Data(Viewer.RenderProcess.FrameRate.ToString("F0")); //FPS
				Logger.Data(Viewer.RenderProcess.FrameTime.ToString("F4")); //Frame Time
				Logger.Data(Viewer.RenderProcess.FrameJitter.ToString("F4")); //Frame Jitter
				Logger.Data(Viewer.RenderProcess.PrimitivePerFrame.Sum().ToString()); //Primitives
				Logger.Data(Viewer.RenderProcess.RenderStateChangesPerFrame.ToString()); //State Changes
				Logger.Data(Viewer.RenderProcess.ImageChangesPerFrame.ToString()); //Image Changes
				Logger.Data(processors.ToString()); //Processors
				Logger.Data(Viewer.RenderProfiler.Wall.ToString("F0")); //Render Process %
				Logger.Data(Viewer.UpdaterProfiler.Wall.ToString("F0")); //Updater Process %
				Logger.Data(Viewer.LoaderProfiler.Wall.ToString("F0")); //Loader Process %
				Logger.Data(Viewer.Camera.TileX.ToString());     //
				Logger.Data(Viewer.Camera.TileZ.ToString());     // Camera coordinates
				Logger.Data(Viewer.Camera.Location.ToString());  //
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
		}

		void UpdateDialogsText(ElapsedTime elapsedTime)
		{
			if (Viewer.TrackMonitorWindow.Visible)
			{
				var poiDistance = 0f;
				var poiBackwards = false;
				var poiType = Viewer.Simulator.AI.Dispatcher.GetPlayerNextPOI(out poiDistance, out poiBackwards);
				//Viewer.TrackMonitor.UpdateText(elapsedTime, Viewer.MilepostUnitsMetric, Viewer.PlayerLocomotive.SpeedMpS, 0, TrackMonitorSignalAspect.None, poiType, poiDistance);
                Viewer.TrackMonitorWindow.UpdateText(elapsedTime, Viewer.MilepostUnitsMetric, Viewer.PlayerLocomotive.SpeedMpS, Viewer.PlayerTrain.distanceToSignal, Viewer.PlayerTrain.TMaspect, poiType, poiDistance);
			}
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

        public void UpdateText()
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
                AddDispatcherInfo();
            }
            if (InfoAmount == 4)
            {
                AddDebugInfo();
            }
            if (InfoAmount == 5)
            {
                AddForceInfo();
            }
        }

        private void AddBasicInfo()
        {
            string clockTimeString = FormattedTime(Viewer.Simulator.ClockTime);
            Train playerTrain = Viewer.PlayerLocomotive.Train;

            TextBuilder.Append("Version = "); TextBuilder.AppendLine(Program.Revision);//this DONE
            TextBuilder.Append("Time = "); TextBuilder.AppendLine(clockTimeString);
            TextBuilder.Append("Direction = ");
            if (Math.Abs(Viewer.PlayerLocomotive.Train.MUReverserPercent) != 100)
                TextBuilder.Append(string.Format("{0:F0}% ", Math.Abs(Viewer.PlayerLocomotive.Train.MUReverserPercent)));
            TextBuilder.AppendLine(Viewer.PlayerLocomotive.Direction.ToString());
            TextBuilder.Append("Throttle = "); TextBuilder.AppendLine(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
            TextBuilder.Append("Train Brake = "); TextBuilder.AppendLine(Viewer.PlayerLocomotive.GetTrainBrakeStatus());
            if (playerTrain.RetainerSetting != RetainerSetting.Exhaust)
            {
                TextBuilder.Append("Retainers = "); TextBuilder.AppendLine(string.Format("{0}% {1}", playerTrain.RetainerPercent, playerTrain.RetainerSetting));
            }
            string engBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus();
            if (engBrakeStatus != null)
            {
                TextBuilder.Append("Engine Brake = "); TextBuilder.AppendLine(engBrakeStatus);
            }
            string dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
            if (dynamicBrakeStatus != null)
            {
                TextBuilder.Append("Dynamic Brake = "); TextBuilder.AppendLine(dynamicBrakeStatus);
            }
            TextBuilder.Append("Speed = "); TextBuilder.AppendLine(MpH.FromMpS(Math.Abs(Viewer.PlayerLocomotive.SpeedMpS)).ToString("F1"));
            string status = Viewer.PlayerLocomotive.GetStatus();
            if (status != null)
                TextBuilder.AppendLine(status);
            TextBuilder.Append("Slack = "); TextBuilder.Append(playerTrain.TotalCouplerSlackM.ToString("F2")+" "+playerTrain.NPull.ToString()+" "+playerTrain.NPush.ToString());
            if (playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1)
                TextBuilder.AppendLine(" Stretched");
            else if (playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1)
                TextBuilder.AppendLine(" Bunched");
            else
                TextBuilder.AppendLine();
            TextBuilder.Append("CForce = "); TextBuilder.AppendLine(playerTrain.MaximumCouplerForceN.ToString("F0"));

            // Added by rvg....
            // Compass
            string sTemp;
            Vector2 compassDir;
            compassDir.X = Viewer.Camera.XNAView.M11;
            compassDir.Y = Viewer.Camera.XNAView.M13;
            float direction = MathHelper.ToDegrees((float)Math.Acos(compassDir.X));
            if (compassDir.Y > 0) direction = 360-direction;
            sTemp = direction.ToString("N0");
            sTemp += Convert.ToChar(176);
            TextBuilder.Append("Compass Hdg: "); TextBuilder.AppendLine(sTemp);
            // Latitude/Longitude
            WorldLatLon worldLatLon = new WorldLatLon();
            double latitude = 0;
            double longitude = 0; ;
            worldLatLon.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
            sTemp = MathHelper.ToDegrees((float)latitude).ToString("F6");
            sTemp += ", ";
            sTemp += MathHelper.ToDegrees((float)longitude).ToString("F6");
            TextBuilder.Append("Lat/Lon: "); TextBuilder.AppendLine(sTemp);

            status = Viewer.Simulator.AI.GetStatus();
            if (status != null)
                TextBuilder.Append(status);

            TextBuilder.AppendLine();
            TextBuilder.AppendLine(string.Format("FPS = {0:F0}", Viewer.RenderProcess.SmoothedFrameRate)); //this DONE
/*
            //WHN:
            status = Viewer.PlayerTrain.RearTDBTraveller.TNToString();
            TextBuilder.Append("TN: "); TextBuilder.AppendLine(status);
*/
        }

        [Conditional("DEBUG")]
        private void AddDebugInfo()
        {
            // Memory Useage
            long memory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            TextBuilder.AppendLine(); //notepad pls.
			TextBuilder.AppendLine("DEBUG INFORMATION");
            TextBuilder.AppendFormat("Logging Enabled = {0}", LoggerEnabled); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Build = {0}", Program.Build); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Memory = {0:N0} MB", memory / 1024 / 1024); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Adapter = {0} ({1:N0} MB)", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Frame Time = {0:F1} ms", Viewer.RenderProcess.SmoothedFrameTime * 1000); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Frame Jitter = {0:F1} ms", Viewer.RenderProcess.SmoothedFrameJitter * 1000); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Render Primitives = {0:N0} ({1})", Viewer.RenderProcess.PrimitivePerFrame.Sum(), String.Join(" + ", Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("N0")).ToArray())); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Render State Changes = {0:N0}", Viewer.RenderProcess.RenderStateChangesPerFrame); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Render Image Changes = {0:N0}", Viewer.RenderProcess.ImageChangesPerFrame); TextBuilder.AppendLine();
            TextBuilder.AppendFormat("Processors = {0}", processors); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Render Process = {0:F0}% ({1:F0}% wait)", Viewer.RenderProfiler.SmoothedWall, Viewer.RenderProfiler.SmoothedWait); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Update Process = {0:F0}% ({1:F0}% wait)", Viewer.UpdaterProfiler.SmoothedWall, Viewer.UpdaterProfiler.SmoothedWait); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Loader Process = {0:F0}% ({1:F0}% wait)", Viewer.LoaderProfiler.SmoothedWall, Viewer.LoaderProfiler.SmoothedWait); TextBuilder.AppendLine();
			TextBuilder.AppendFormat("Total Process = {0:F0}% ({1:F0}% wait)", Viewer.RenderProfiler.SmoothedWall + Viewer.UpdaterProfiler.SmoothedWall + Viewer.LoaderProfiler.SmoothedWall, Viewer.RenderProfiler.SmoothedWait + Viewer.UpdaterProfiler.SmoothedWait + Viewer.LoaderProfiler.SmoothedWait); TextBuilder.AppendLine();
            // Added by rvg....
            TextBuilder.Append("Tile: "); TextBuilder.Append(Viewer.Camera.TileX.ToString()); // Camera coordinates
            TextBuilder.Append(" ");
            TextBuilder.Append(Viewer.Camera.TileZ.ToString());//this DONE
            TextBuilder.Append(" ");
            TextBuilder.AppendLine(Viewer.Camera.Location.ToString()); //this DONE
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
                TextBuilder.Append(string.Format("Car {0:D2} ", j + 1));
                TextBuilder.AppendLine(playerTrain.Cars[j].BrakeSystem.GetStatus(2));
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
                TrainCar car= playerTrain.Cars[j];
                TextBuilder.Append(string.Format("Car {0:D2} ", j + 1));
                TextBuilder.AppendLine(string.Format("{0:F0} {1:F0} {2:F0} {3:F0} {4:F0} {5:F0} {6}", car.TotalForceN, car.MotiveForceN, car.FrictionForceN, car.GravityForceN, car.CouplerForceU, car.MassKG, car.Flipped));
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

        string FormattedTime(double clockTimeSeconds) //some measure of time so it can be sorted.  Good enuf for now. Might add more later. Okay
        {
            int hour = (int)(clockTimeSeconds / (60.0 * 60.0));
            clockTimeSeconds -= hour * 60.0 * 60.0;
            int minute = (int)(clockTimeSeconds / 60.0);
            clockTimeSeconds -= minute * 60.0;
            int seconds = (int)clockTimeSeconds;
            // Reset clock before and after midnight
            if (hour >= 24)
                hour -= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

        public void Profile(double elapsedRealSeconds) // should be called every 100mS
        {
            if (elapsedRealSeconds < 0.01)  // just in case
				return;

			Viewer.RenderProfiler.Mark();
			Viewer.UpdaterProfiler.Mark();
			Viewer.LoaderProfiler.Mark();
        }

    } // Class Info Display

    public class TextPrimitive : RenderPrimitive
    {
        public SpriteBatchMaterial Material;
        public string Text;
        public Color Color;
        public Vector2 Location;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            Material.SpriteBatch.DrawString(Material.DefaultFont, Text, Location, Color );
        }
    }



}