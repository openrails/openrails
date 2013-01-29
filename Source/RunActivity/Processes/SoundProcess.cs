// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
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
using System.Linq;
using System.Threading;

namespace ORTS
{
    public class SoundProcess
    {
		public readonly bool Threaded;
		public readonly Profiler Profiler = new Profiler("Sound");
        readonly Viewer3D Viewer;
		readonly Thread Thread;

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
                Thread.Sleep(200);
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
							if (at.SoundNotify != -1)
							{
								if (Viewer.World.GameSounds != null) Viewer.World.GameSounds.HandleEvent(at.SoundNotify);
								at.SoundNotify = -1;
							}
						}
					}
				}
				// Update all sound in our list
				lock (SoundSources)
				{
					List<KeyValuePair<List<SoundSourceBase>, SoundSourceBase>> remove = null;
					foreach (List<SoundSourceBase> src in SoundSources.Values)
					{
						foreach (SoundSourceBase ss in src)
						{
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
