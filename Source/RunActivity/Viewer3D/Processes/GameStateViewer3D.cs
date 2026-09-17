// COPYRIGHT 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Orts.MultiPlayer;
using Orts.Viewer3D.Debugging;
using System;

namespace Orts.Viewer3D.Processes
{
    public class GameStateViewer3D : GameState
    {
        internal readonly Viewer Viewer;

        bool FirstFrame = true;
        int ProfileFrames = 0;

        public GameStateViewer3D(Viewer viewer)
        {
            Viewer = viewer;
            Viewer.Simulator.Paused = true;
            Viewer.QuitWindow.Visible = true;
        }

        internal override void BeginRender(RenderFrame frame)
        {
            // Do this here (instead of RenderProcess) because we only want to measure/time the running game.
            if (Game.Settings.Profiling)
                if ((Game.Settings.ProfilingFrameCount > 0 && ++ProfileFrames > Game.Settings.ProfilingFrameCount) || (Game.Settings.ProfilingTime > 0 && Viewer != null && Viewer.RealTime >= Game.Settings.ProfilingTime))
                    Game.PopState();

            if (FirstFrame)
            {
                // Turn off the 10FPS fixed-time-step and return to running as fast as we can.
                Game.IsFixedTimeStep = false;
                Game.InactiveSleepTime = TimeSpan.Zero;

                // We must create these forms on the main thread (Render) or they won't pump events correctly.
                Program.MapForm = new MapViewer(Viewer.Simulator, Viewer);
                Program.MapForm.Hide();

                Program.SoundDebugForm = new SoundDebugForm(Viewer);
                Program.SoundDebugForm.Hide();
                Viewer.SoundDebugFormEnabled = false;

                FirstFrame = false;
            }
            Viewer.BeginRender(frame);
        }

        internal override void EndRender(RenderFrame frame)
        {
            Viewer.EndRender(frame);
        }

        double LastLoadRealTime;
        double LastTotalRealSeconds = -1;
        double[] AverageElapsedRealTime = new double[10];
        int AverageElapsedRealTimeIndex;

        internal override void Update(RenderFrame frame, double totalRealSeconds)
        {
            // Every 250ms, check for new things to load and kick off the loader.
            if (LastLoadRealTime + 0.25 < totalRealSeconds && Game.LoaderProcess.Finished)
            {
                LastLoadRealTime = totalRealSeconds;
                Viewer.World.LoadPrep();
                Game.LoaderProcess.StartLoad();
            }

            // The first time we update, the TotalRealSeconds will be ~time
            // taken to load everything. We'd rather not skip that far through
            // the simulation so the first time we deliberately have an
            // elapsed real and clock time of 0.0s.
            if (LastTotalRealSeconds == -1)
                LastTotalRealSeconds = totalRealSeconds;
            // We would like to avoid any large jumps in the simulation, so
            // this is a 4FPS minimum, 250ms maximum update time.
            else if (totalRealSeconds - LastTotalRealSeconds > 0.25f)
                LastTotalRealSeconds = totalRealSeconds;

            var elapsedRealTime = totalRealSeconds - LastTotalRealSeconds;
            LastTotalRealSeconds = totalRealSeconds;

            if (elapsedRealTime > 0)
            {
                // Store the elapsed real time, but also loop through overwriting any blank entries.
                do
                {
                    AverageElapsedRealTime[AverageElapsedRealTimeIndex] = elapsedRealTime;
                    AverageElapsedRealTimeIndex = (AverageElapsedRealTimeIndex + 1) % AverageElapsedRealTime.Length;
                } while (AverageElapsedRealTime[AverageElapsedRealTimeIndex] == 0);

                // Elapsed real time is now the average.
                elapsedRealTime = 0;
                for (var i = 0; i < AverageElapsedRealTime.Length; i++)
                    elapsedRealTime += AverageElapsedRealTime[i] / AverageElapsedRealTime.Length;
            }

            // TODO: ComputeFPS should be called in UpdaterProcess.Update() but needs delta time.
            Game.RenderProcess.ComputeFPS((float)elapsedRealTime);
            Viewer.Update(frame, (float)elapsedRealTime);
        }

        internal override void Load()
        {
            Viewer.Load();
        }

        internal override void Dispose()
        {
            Viewer.Terminate();
           if (MPManager.Server != null)
                MPManager.Server.Stop();
            if (MPManager.Client != null)
                MPManager.Client.Stop();
            if (Program.Simulator != null)
                Program.Simulator.Stop();
            // MapForm and SoundDebugForm run in the Renderer thread and need to be disposed there
            base.Dispose();
        }

    }
}
