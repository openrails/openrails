// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ORTS
{
    public class SoundProcess
    {
		public readonly bool Threaded;
		public readonly Profiler Profiler = new Profiler("Sound");
		readonly Viewer3D Viewer;
		readonly Thread Thread;
		readonly ProcessState State;

        Dictionary<object, List<SoundSource>> _SoundSources;

        /// <summary>
        /// Constructs SoundProcess, creates the sound thread but not start. Must create after loading ingame sounds.
        /// </summary>
		public SoundProcess(Viewer3D viewer)
        {
			Threaded = true;
			Viewer = viewer;
			if (Threaded)
			{
				State = new ProcessState();
				Thread = new Thread(SoundUpdateLoop);
			}
            _SoundSources = new Dictionary<object, List<SoundSource>>();
			if (Viewer.IngameSounds != null)
            {
				AddSoundSource(Viewer.Simulator.RoutePath + "\\Sound\\ingame.sms", new List<SoundSource>() { Viewer.IngameSounds });
            }
        }

        /// <summary>
        /// Checks the SoundDetail level, and if above 0, starts sound thread.
        /// </summary>
        public void Run()
        {
			if (Viewer.Settings.SoundDetailLevel > 0) Thread.Start();
        }

        /// <summary>
        /// Stops sound thread
        /// </summary>
        public void Stop()
        {
			Thread.Abort();
        }

        /// <summary>
        /// Adds a SoundSource list attached to an object to the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object, could be anything</param>
        /// <param name="sources">List of SoundSources to play</param>
        public void AddSoundSource(object viewer, List<SoundSource> sources)
        {
            lock (_SoundSources)
            {
                if (!_SoundSources.Keys.Contains(viewer)) 
                    _SoundSources.Add(viewer, sources);
            }
        }

        /// <summary>
        /// Removes a SoundSource list attached to an object from the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object the sounds attached to</param>
        public void RemoveSoundSource(object viewer)
        {
            List<SoundSource> ls = null;
            // Try to remove the given SoundSource
            lock (_SoundSources)
            {
                if (_SoundSources.Keys.Contains(viewer))
                {
                    ls = _SoundSources[viewer];
                    _SoundSources.Remove(viewer);
                }
            }
            // Uninitialize its sounds
            if (ls != null)
            {
                foreach (SoundSource ss in ls)
                    ss.Uninitialize();
            }
        }
        
        /// <summary>
        /// The loop running in the thread
        /// </summary>
		[ThreadName("Sound")]
        public void SoundUpdateLoop()
        {
			ProcessState.SetThreadName("Sound Process");

            while (Viewer.RealTime == 0)
            {
                Thread.Sleep(100);
            }

            lock (_SoundSources)
            {
                foreach (List<SoundSource> src in _SoundSources.Values)
                {
                    foreach (SoundSource ss in src)
                    {
                        ss.InitInitials();
                    }
                }
            }

            while (true)
            {
				Profiler.Start();

                // Update activity sounds
                {
                    Activity act = Viewer.Simulator.ActivityRun;
                    if (act != null)
                    {
                        ActivityTask at = act.Current;
                        if (at != null)
                        {
                            if (at.SoundNotify != -1)
                            {
                                if (Viewer.IngameSounds != null) Viewer.IngameSounds.HandleEvent(at.SoundNotify);
                                at.SoundNotify = -1;
                            }
                        }
                    }
                }

                // Update all sound in our list
                lock (_SoundSources)
                {
                    foreach (List<SoundSource> src in _SoundSources.Values)
                    {
                        foreach (SoundSource ss in src)
                        {
                            ss.Update();
                        }
                    }
                }

				Profiler.Stop();

                // Sleeping a while
                Thread.Sleep(200);
            }
        }

        internal void RemoveAllSources()
        {
            // TODO: Clear all and exit
        }
    }
}
