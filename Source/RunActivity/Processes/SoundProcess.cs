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

using ORTS.Common;
using ORTS.Processes;
using ORTS.Viewer3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ORTS
{
    public class SoundProcess
    {
        public readonly Profiler Profiler = new Profiler("Sound");
        readonly ProcessState State = new ProcessState("Sound");
        readonly Game Game;
        readonly Thread Thread;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        Dictionary<object, List<SoundSourceBase>> SoundSources = new Dictionary<object, List<SoundSourceBase>>();

        private int UpdateCounter = -1;
        private const int FULLUPDATECYCLE = 4; // Number of frequent updates needed till a full update

        public SoundProcess(Game game)
        {
            Game = game;
            Thread = new Thread(SoundThread);
        }

        public void Start()
        {
            if (Game.Settings.SoundDetailLevel > 0)
                Thread.Start();
        }

        public void Stop()
        {
            State.SignalTerminate();
        }

        [ThreadName("Sound")]
        void SoundThread()
        {
            Profiler.SetThread();

            OpenAL.Initialize();

            while (true)
            {
                Thread.Sleep(50);
                if (State.Terminated)
                    break;
                if (!DoSound())
                    return;
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
                    // Unblock anyone waiting for us, report error and die.
                    State.SignalTerminate();
                    Game.ProcessReportError(error);
                    return false;
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
                var viewer = Game.RenderProcess.Viewer;
                if (viewer == null)
                    return;

                OpenAL.alListenerf(OpenAL.AL_GAIN, Program.Simulator.Paused ? 0 : (float)Game.Settings.SoundVolumePercent / 100f);

                // Update activity sounds
                if (viewer.Simulator.SoundNotify != Event.None)
                {
                    if (viewer.World.GameSounds != null) viewer.World.GameSounds.HandleEvent(viewer.Simulator.SoundNotify);
                    viewer.Simulator.SoundNotify = Event.None;
                }

                // Update all sound in our list
                UpdateCounter++;
                UpdateCounter %= FULLUPDATECYCLE;

                var soundSources = SoundSources;
                List<KeyValuePair<object, SoundSourceBase>> removals = null;
                foreach (var sources in soundSources)
                {
                    foreach (var source in sources.Value)
                    {
                        if (!source.NeedsFrequentUpdate && UpdateCounter > 0)
                            continue;

                        if (!source.Update())
                        {
                            if (removals == null)
                                removals = new List<KeyValuePair<object, SoundSourceBase>>();
                            removals.Add(new KeyValuePair<object, SoundSourceBase>(sources.Key, source));
                        }
                    }
                }
                if (removals != null)
                {
                    // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
                    // failure, we must recompute the modifications from SoundSources.
                    Dictionary<object, List<SoundSourceBase>> newSoundSources;
                    do
                    {
                        soundSources = SoundSources;
                        newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                        foreach (var removal in removals)
                        {
                            // If either of the key or value no longer exist, we can't remove them - so skip over them.
                            if (newSoundSources.ContainsKey(removal.Key) && newSoundSources[removal.Key].Contains(removal.Value))
                            {
                                removal.Value.Dispose();
                                newSoundSources[removal.Key] = new List<SoundSourceBase>(newSoundSources[removal.Key]);
                                newSoundSources[removal.Key].Remove(removal.Value);
                            }
                        }
                    } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
                }
            }
            finally
            {
                Profiler.Stop();
            }
        }

        public void GetSoundSources(ref Dictionary<object, List<SoundSourceBase>> soundSources)
        {
            soundSources = new Dictionary<object, List<SoundSourceBase>>(SoundSources);
        }

        public List<SoundSourceBase> GetSoundSources(object viewer)
        {
            return SoundSources[viewer];
        }

        /// <summary>
        /// Adds a SoundSource list attached to an object to the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object, could be anything</param>
        /// <param name="sources">List of SoundSources to play</param>
        public void AddSoundSource(object viewer, List<SoundSourceBase> sources)
        {
            // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
            // failure, we must recompute the modifications from SoundSources.
            Dictionary<object, List<SoundSourceBase>> soundSources;
            Dictionary<object, List<SoundSourceBase>> newSoundSources;
            do
            {
                soundSources = SoundSources;
                newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                if (!newSoundSources.ContainsKey(viewer))
                    newSoundSources.Add(viewer, sources);
            } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
        }

        /// <summary>
        /// Removes a SoundSource list attached to an object from the playable sounds.
        /// </summary>
        /// <param name="viewer">The viewer object the sounds attached to</param>
        public void RemoveSoundSource(object viewer)
        {
            // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
            // failure, we must recompute the modifications from SoundSources.
            Dictionary<object, List<SoundSourceBase>> soundSources;
            Dictionary<object, List<SoundSourceBase>> newSoundSources;
            do
            {
                soundSources = SoundSources;
                newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                if (newSoundSources.ContainsKey(viewer))
                {
                    foreach (var source in newSoundSources[viewer])
                        source.Uninitialize();
                    newSoundSources.Remove(viewer);
                }
            } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
        }
    }
}
