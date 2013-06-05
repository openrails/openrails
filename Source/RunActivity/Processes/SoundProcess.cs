// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

#define DOPPLER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;

namespace ORTS
{
    public class SoundProcess
    {
		public readonly bool Threaded;
		public readonly Profiler Profiler = new Profiler("Sound");
        readonly Viewer3D Viewer;
		readonly Thread Thread;

        private int UpdateCounter = -1;
        private const int FULLUPDATECYCLE = 4; // Number of frequent updates needed till a full update

        public SoundProcess(Viewer3D viewer)
        {
            Threaded = true;
            Viewer = viewer;
            if (Viewer.Settings.SoundDetailLevel > 0)
            {
                if (Threaded)
                {
                    Thread = new Thread(SoundThread);
                    Thread.Start();
                }
                if (Viewer.World.GameSounds != null)
                    AddSoundSource(Viewer.Simulator.RoutePath + "\\Sound\\ingame.sms", new List<SoundSourceBase>() { Viewer.World.GameSounds });
            }
        }

        public void Stop()
        {
            if (Threaded && Thread != null)
                Thread.Abort();
        }

        Dictionary<object, List<SoundSourceBase>> SoundSources = new Dictionary<object, List<SoundSourceBase>>();

        /// <summary>
        /// Adds a SoundSource list attached to an object to the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object, could be anything</param>
        /// <param name="sources">List of SoundSources to play</param>
        public void AddSoundSource(object viewer, List<SoundSourceBase> sources)
        {
            lock (SoundSources)
            {
                if (!SoundSources.Keys.Contains(viewer)) 
                    SoundSources.Add(viewer, sources);
            }
        }

        /// <summary>
        /// Removes a SoundSource list attached to an object from the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object the sounds attached to</param>
        public void RemoveSoundSource(object viewer)
        {
            List<SoundSourceBase> ls = null;
            // Try to remove the given SoundSource
            lock (SoundSources)
            {
                if (SoundSources.Keys.Contains(viewer))
                {
                    ls = SoundSources[viewer];
                    SoundSources.Remove(viewer);
                }
            }
            // Uninitialize its sounds
            if (ls != null)
            {
                foreach (SoundSource ss in ls)
                    ss.Uninitialize();
            }
        }
        
		[ThreadName("Sound")]
        void SoundThread()
        {
            Profiler.SetThread();

            while (Viewer.RealTime == 0)
                Thread.Sleep(100);

            lock (SoundSources)
                foreach (List<SoundSourceBase> src in SoundSources.Values)
                    foreach (SoundSourceBase ss in src)
                        ss.InitInitials();

            while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
            {
                DoSound();
                Thread.Sleep(50);
            }
        }

        [CallOnThread("Sound")]
        bool DoSound()
        {
            if (Debugger.IsAttached)
            {
                Sound();
            }
            else
            {
                try
                {
                    Sound();
                }
                catch (Exception error)
                {
                    if (!(error is ThreadAbortException))
                    {
                        // Report error and die.
                        Viewer.ProcessReportError(error);
                        return false;
                    }
                }
            }
            return true;
        }

        [CallOnThread("Sound")]
        void Sound()
        {
            Profiler.Start();
			try
			{
				// Update activity sounds
				{
					Activity act = Viewer.Simulator.ActivityRun;
					if (act != null)
					{
						ActivityTask at = act.Current;
						if (at != null)
						{
							if (at.SoundNotify != Event.None)
							{
								if (Viewer.World.GameSounds != null) Viewer.World.GameSounds.HandleEvent(at.SoundNotify);
                                at.SoundNotify = Event.None;
							}
						}
					}
				}
#if DOPPLER
                if (Viewer != null || Viewer.Camera != null) // For sure we have a Camera
                {
                    float[] cameraPosition = new float[] {
                        Viewer.Camera.CameraWorldLocation.Location.X + 2048 * Viewer.Camera.CameraWorldLocation.TileX,
                        Viewer.Camera.CameraWorldLocation.Location.Y,
                        Viewer.Camera.CameraWorldLocation.Location.Z + 2048 * Viewer.Camera.CameraWorldLocation.TileZ};

                    float[] cameraVelocity = new float[] { 0, 0, 0 };

                    if (!(Viewer.Camera is TracksideCamera) && !(Viewer.Camera is FreeRoamCamera) && Viewer.Camera.AttachedCar != null)
                    {
                        Vector3 directionVector = Vector3.Multiply(Viewer.Camera.AttachedCar.GetXNAMatrix().Forward, Viewer.Camera.AttachedCar.SpeedMpS);
                        cameraVelocity = new float[] { directionVector.X, directionVector.Y, -directionVector.Z };
                    }

                    float[] cameraOrientation = new float[] { 
                        Viewer.Camera.XNAView.Forward.X, Viewer.Camera.XNAView.Forward.Y, Viewer.Camera.XNAView.Forward.Z,
                        Viewer.Camera.XNAView.Up.X, Viewer.Camera.XNAView.Up.Y, Viewer.Camera.XNAView.Up.Z };

                    OpenAL.alListenerfv(OpenAL.AL_POSITION, cameraPosition);
                    OpenAL.alListenerfv(OpenAL.AL_VELOCITY, cameraVelocity);
                    OpenAL.alListenerfv(OpenAL.AL_ORIENTATION, cameraOrientation);
                    OpenAL.alListenerf(OpenAL.AL_GAIN, Program.Simulator.Paused ? 0 : (float)Program.Simulator.Settings.SoundVolumePercent / 100f);
                }
#endif
                // Update all sound in our list
				lock (SoundSources)
				{
                    UpdateCounter++;
                    UpdateCounter %= FULLUPDATECYCLE;

					List<KeyValuePair<List<SoundSourceBase>, SoundSourceBase>> remove = null;
					foreach (List<SoundSourceBase> src in SoundSources.Values)
					{
						foreach (SoundSourceBase ss in src)
						{
                            if (!ss.NeedsFrequentUpdate && UpdateCounter > 0)
                                continue;

							if (!ss.Update())
							{
								if (remove == null)
									remove = new List<KeyValuePair<List<SoundSourceBase>, SoundSourceBase>>();
								remove.Add(new KeyValuePair<List<SoundSourceBase>, SoundSourceBase>(src, ss));
							}
						}
					}
					if (remove != null)
					{
						foreach (KeyValuePair<List<SoundSourceBase>, SoundSourceBase> ss in remove)
						{
							ss.Value.Dispose();
							ss.Key.Remove(ss.Value);
						}
					}
				}
			}
			catch { }
            finally
            {
                Profiler.Stop();
            }
        }

        internal void RemoveAllSources()
        {
            // TODO: Clear all and exit
        }
    }
}
