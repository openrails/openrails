// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Define this to log each change of the sound sources.
//#define DEBUG_SOURCE_SOURCES

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
        readonly WatchdogToken WatchdogToken;

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
            WatchdogToken = new WatchdogToken(Thread);
        }

        public void Start()
        {
            if (Game.Settings.SoundDetailLevel > 0)
            {
                Game.WatchdogProcess.Register(WatchdogToken);
                Thread.Start();
            }
        }

        public void Stop()
        {
            if (Game.Settings.SoundDetailLevel > 0)
            {
                Game.WatchdogProcess.Unregister(WatchdogToken);
                State.SignalTerminate();
            }
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
                WatchdogToken.Ping();

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
#if DEBUG_SOURCE_SOURCES
                    Trace.TraceInformation("SoundProcess: sound source self-removal on " + Thread.CurrentThread.Name);
#endif
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

        internal Dictionary<object, List<SoundSourceBase>> GetSoundSources()
        {
            return SoundSources;
        }

        /// <summary>
        /// Adds the collection of <see cref="SoundSourceBase"/> for a particular <paramref name="owner"/> to the playable sounds.
        /// </summary>
        /// <param name="owner">The object to which the sound sources are attached.</param>
        /// <param name="sources">The sound sources to add.</param>
        public void AddSoundSources(object owner, List<SoundSourceBase> sources)
        {
#if DEBUG_SOURCE_SOURCES
            Trace.TraceInformation("SoundProcess: AddSoundSources on " + Thread.CurrentThread.Name + " by " + owner);
#endif
            // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
            // failure, we must recompute the modifications from SoundSources.
            Dictionary<object, List<SoundSourceBase>> soundSources;
            Dictionary<object, List<SoundSourceBase>> newSoundSources;
            do
            {
                soundSources = SoundSources;
                newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                newSoundSources.Add(owner, sources);
            } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
        }

        /// <summary>
        /// Adds a single <see cref="SoundSourceBase"/> to the playable sounds.
        /// </summary>
        /// <param name="owner">The object to which the sound is attached.</param>
        /// <param name="source">The sound source to add.</param>
        public void AddSoundSource(object owner, SoundSourceBase source)
        {
#if DEBUG_SOURCE_SOURCES
            Trace.TraceInformation("SoundProcess: AddSoundSource on " + Thread.CurrentThread.Name + " by " + owner);
#endif
            // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
            // failure, we must recompute the modifications from SoundSources.
            Dictionary<object, List<SoundSourceBase>> soundSources;
            Dictionary<object, List<SoundSourceBase>> newSoundSources;
            do
            {
                soundSources = SoundSources;
                newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                if (!newSoundSources.ContainsKey(owner))
                    newSoundSources.Add(owner, new List<SoundSourceBase>());
                newSoundSources[owner].Add(source);
            } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
        }

        /// <summary>
        /// Returns whether a particular sound source in the playable sounds is owned by a particular <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">The object to which the sound might be owned.</param>
        /// <param name="source">The sound source to check.</param>
        /// <returns><see cref="true"/> for a match between <paramref name="owner"/> and <paramref name="source"/>, <see cref="false"/> otherwise.</returns>
        public bool IsSoundSourceOwnedBy(object owner, SoundSourceBase source)
        {
            var soundSources = SoundSources;
            return soundSources.ContainsKey(owner) && soundSources[owner].Contains(source);
        }

        /// <summary>
        /// Removes the collection of <see cref="SoundSourceBase"/> for a particular <paramref name="owner"/> from the playable sounds.
        /// </summary>
        /// <param name="owner">The object to which the sound sources are attached.</param>
        public void RemoveSoundSources(object owner)
        {
#if DEBUG_SOURCE_SOURCES
            Trace.TraceInformation("SoundProcess: RemoveSoundSources on " + Thread.CurrentThread.Name + " by " + owner);
#endif
            // We use an interlocked compare-exchange to thread-safely update the list. Note that on each
            // failure, we must recompute the modifications from SoundSources.
            Dictionary<object, List<SoundSourceBase>> soundSources;
            Dictionary<object, List<SoundSourceBase>> newSoundSources;
            do
            {
                soundSources = SoundSources;
                newSoundSources = new Dictionary<object, List<SoundSourceBase>>(soundSources);
                if (newSoundSources.ContainsKey(owner))
                {
                    foreach (var source in newSoundSources[owner])
                        source.Uninitialize();
                    newSoundSources.Remove(owner);
                }
            } while (soundSources != Interlocked.CompareExchange(ref SoundSources, newSoundSources, soundSources));
        }
    }
}
