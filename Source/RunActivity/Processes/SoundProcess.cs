using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ORTS
{
    public class SoundProcess
    {
        Thread _SoundThread;
        Viewer3D _Viewer3D;
        Dictionary<object, List<SoundSource>> _SoundSources;

        /// <summary>
        /// Constructs SoundProcess, creates the sound thread but not start. Must create after loading ingame sounds.
        /// </summary>
        /// <param name="viewer3D">The Viewer</param>
        public SoundProcess(Viewer3D viewer3D)
        {
            _Viewer3D = viewer3D;
            ThreadStart ts = new ThreadStart(SoundUpdateLoop);
            _SoundThread = new Thread(ts);
            _SoundSources = new Dictionary<object, List<SoundSource>>();
            if (_Viewer3D.IngameSounds != null)
            {
                AddSoundSource(_Viewer3D.Simulator.RoutePath + "\\Sound\\ingame.sms", new List<SoundSource>() { _Viewer3D.IngameSounds });
            }
        }

        /// <summary>
        /// Checks the SoundDetail level, and if above 0, starts sound thread.
        /// </summary>
        public void Run()
        {
            if (_Viewer3D.SettingsInt[(int)IntSettings.SoundDetailLevel] > 0) _SoundThread.Start();
        }

        /// <summary>
        /// Stops sound thread
        /// </summary>
        public void Stop()
        {
            _SoundThread.Abort();
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
        public void SoundUpdateLoop()
        {
            while (true)
            {
                // Sleeping a while
                Thread.Sleep(200);

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
            }
        }
    }
}
