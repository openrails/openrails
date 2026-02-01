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

using Orts.Common;
using Orts.Processes;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Orts.Viewer3D.Processes
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

#if DEBUG_SOURCE_SOURCES
        private float SoundSrcCount = 0;
        private float SoundSrcBaseCount = 0;
        private float NullSoundSrcBaseCount = 0;
        private float SoundTime = 0;

        private double ConsoleWriteTime = 0;
#endif
        private int UpdateCounter = -1;
        private int SleepTime = 50;
        private double StartUpdateTime = 0;
        private int ASyncUpdatePending = 0;
        private const int FULLUPDATECYCLE = 4; // Number of frequent updates needed till a full update

        ORTSActSoundSources ORTSActSoundSourceList; // Dictionary of activity sound sources

        public SoundProcess(Game game)
        {
            Game = game;
            Thread = new Thread(SoundThread);
            WatchdogToken = new WatchdogToken(Thread);
            ORTSActSoundSourceList = new ORTSActSoundSources();
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
                Thread.Sleep(SleepTime);
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
                if (viewer.Simulator.SoundNotify != Orts.Common.Event.None)
                {
                    if (viewer.World.GameSounds != null) viewer.World.GameSounds.HandleEvent(viewer.Simulator.SoundNotify);
                    viewer.Simulator.SoundNotify = Orts.Common.Event.None;
                }

                // Update all sound in our list
                //float UpdateInterrupts = 0;
                StartUpdateTime = viewer.RealTime;
                int RetryUpdate = 0;
                int restartIndex = -1;
                
                while (RetryUpdate >= 0)
                {
                    bool updateInterrupted = false;
                    lock (SoundSources)
                    {
                        UpdateCounter++;
                        UpdateCounter %= FULLUPDATECYCLE;
                        var removals = new List<KeyValuePair<object, SoundSourceBase>>();
#if DEBUG_SOURCE_SOURCES
                        SoundSrcBaseCount += SoundSources.Count;
#endif
                        foreach (var sources in SoundSources)
                        {
                            restartIndex++;
#if DEBUG_SOURCE_SOURCES
                            SoundSrcCount += sources.Value.Count;
                            if (sources.Value.Count < 1)
                            {
                                NullSoundSrcBaseCount++;
                                //Trace.TraceInformation("Null SoundSourceBase {0}", sources.Key.ToString());
                            }
#endif
                            if (restartIndex >= RetryUpdate)
                            {
                                for (int i = 0; i < sources.Value.Count; i++)
                                {
                                    if (!sources.Value[i].NeedsFrequentUpdate && UpdateCounter > 0)
                                        continue;

                                    if (!sources.Value[i].Update())
                                    {
                                        removals.Add(new KeyValuePair<object, SoundSourceBase>(sources.Key, sources.Value[i]));
                                    }
                                }
                            }
                            // Check if Add or Remove Sound Sources is waiting to get in - allow it if so.
                            // Update can be a (relatively) long process.
                            if (ASyncUpdatePending > 0)
                            {
                                updateInterrupted = true;
                                RetryUpdate = restartIndex;
                                //Trace.TraceInformation("Sound Source Updates Interrupted: {0}, Restart Index:{1}", UpdateInterrupts, restartIndex);
                                break;
                            }

                        }
                        if (!updateInterrupted)
                            RetryUpdate = -1;
#if DEBUG_SOURCE_SOURCES
                        Trace.TraceInformation("SoundProcess: sound source self-removal on " + Thread.CurrentThread.Name);
#endif
                        // Remove Sound Sources for train no longer active.  This doesn't seem to be necessary -
                        // cleanup when a train is removed seems to do it anyway with hardly any delay.
                        foreach (var removal in removals)
                        {
                            // If either of the key or value no longer exist, we can't remove them - so skip over them.
                            if (SoundSources.ContainsKey(removal.Key) && SoundSources[removal.Key].Contains(removal.Value))
                            {
                                removal.Value.Uninitialize();
                                SoundSources[removal.Key].Remove(removal.Value);
                                if (SoundSources[removal.Key].Count == 0)
                                {
                                    SoundSources.Remove(removal.Key);
                                }
                            }
                        }
                    }

                    //Update check for activity sounds
                    if (ORTSActSoundSourceList != null)
                        ORTSActSoundSourceList.Update();
                }
                //if (UpdateInterrupts > 1)
                //    Trace.TraceInformation("Sound Source Update Interrupted more than once: {0}", UpdateInterrupts);

                // <CSComment> the block below could provide better sound response but is more demanding in terms of CPU time, especially for slow CPUs
/*              int resptime = (int)((viewer.RealTime - StartUpdateTime) * 1000);
                SleepTime = 50 - resptime;
                if (SleepTime < 5)
                    SleepTime = 5;*/
#if DEBUG_SOURCE_SOURCES
                SoundTime += (int)((viewer.RealTime - StartUpdateTime) * 1000);
                if (viewer.RealTime - ConsoleWriteTime >= 15f)
                {
                    Console.WriteLine("SoundSourceBases (Null): {0} ({1}), SoundSources: {2}, Time: {3}ms",
                        (int)(SoundSrcBaseCount/UpdateCounter), (int)(NullSoundSrcBaseCount/UpdateCounter), (int)(SoundSrcCount/UpdateCounter), (int)(SoundTime/UpdateCounter));
                    ConsoleWriteTime = viewer.RealTime;
                    SoundTime = 0;
                    UpdateCounter = 0;
                    SoundSrcCount = 0;
                    SoundSrcBaseCount = 0;
                    NullSoundSrcBaseCount = 0;
                }
#endif

            }
            finally
            {
                Profiler.Stop();
            }
        }

        /// <summary>
        /// Used by Sound Debug Form. Warning: Creates garbage.
        /// </summary>
        /// <returns></returns>
        internal Dictionary<object, List<SoundSourceBase>> GetSoundSources()
        {
            lock (SoundSources)
            {
                return new Dictionary<object, List<SoundSourceBase>>(SoundSources);
            }
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
            // We use lock to thread-safely update the list.  Interlocked compare-exchange
            // is used to interrupt the update.
            int j;
            while (ASyncUpdatePending < 1)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 1, 0);
            lock (SoundSources)
                SoundSources.Add(owner, sources);
            while (ASyncUpdatePending > 0)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 0, 1);
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
            // We use lock to thread-safely update the list.  Interlocked compare-exchange
            // is used to interrupt the update.
            int j;
            while (ASyncUpdatePending < 1)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 1, 0);
            lock (SoundSources)
            {
                if (!SoundSources.ContainsKey(owner))
                    SoundSources.Add(owner, new List<SoundSourceBase>());
                SoundSources[owner].Add(source);
            }
            while (ASyncUpdatePending > 0)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 0, 1);
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
            // We use lock to thread-safely update the list.  Interlocked compare-exchange
            // is used to interrupt the update.
            int j;
            while (ASyncUpdatePending < 1)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 1, 0);
            lock (SoundSources)
            {
                if (SoundSources.ContainsKey(owner))
                {
                    foreach (var source in SoundSources[owner])
                        source.Uninitialize();
                    SoundSources.Remove(owner);
                }
            }
            while (ASyncUpdatePending > 0)
                j = Interlocked.CompareExchange(ref ASyncUpdatePending, 0, 1);
        }

        /// <summary>
        /// Sets the stale data flag for ALL sound sources to the given bool
        /// (default true)
        /// </summary>
        public void SetAllStale(bool stale = true)
        {
            lock (SoundSources)
            {
                foreach (List<SoundSourceBase> baseSources in SoundSources.Values)
                {
                    foreach (SoundSourceBase baseSource in baseSources)
                    {
                        // A track sound source actually contains multiple sound sources, need to account for that
                        List<SoundSource> sources = new List<SoundSource>();

                        if (baseSource is SoundSource standardSource)
                        {
                            sources.Add(standardSource);
                        }
                        else if (baseSource is TrackSoundSource trackSource)
                        {
                            sources.AddRange(trackSource.InSources);
                            sources.AddRange(trackSource.OutSources);
                        }

                        foreach (SoundSource source in sources)
                            source.StaleData = stale;

                        baseSource.StaleData = stale;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the stale data flag for sound sources using any of the sound files from the given set of paths
        /// </summary>
        /// <returns>bool indicating if any sound source changed from fresh to stale</returns>
        public bool MarkStale(HashSet<string> wavPaths)
        {
            // Each entry in the dictionary of SoundSources can have multiple SoundSource objects
            // We need to iterate all of the SoundStreams in each SoundSource object to check if the referenced .wav files are used
            bool found = false;

            lock (SoundSources)
            {
                foreach (List<SoundSourceBase> baseSources in SoundSources.Values)
                {
                    foreach (SoundSourceBase baseSource in baseSources)
                    {
                        // A track sound source actually contains multiple sound sources, need to account for that
                        List<SoundSource> sources = new List<SoundSource>();

                        if (baseSource is SoundSource standardSource)
                        {
                            sources.Add(standardSource);
                        }
                        else if (baseSource is TrackSoundSource trackSource)
                        {
                            sources.AddRange(trackSource.InSources);
                            sources.AddRange(trackSource.OutSources);
                        }

                        foreach (SoundSource source in sources)
                        {
                            HashSet<string> files = new HashSet<string>();

                            // Files are stored in the sound streams, check for files there
                            string[] pathArray = {source.SMSFolder ?? "",
                                                Program.Simulator.RoutePath + @"\SOUND",
                                                Program.Simulator.BasePath + @"\SOUND"};

                            foreach (SoundStream stream in source.SoundStreams)
                                foreach (ORTSTrigger trigger in stream.Triggers)
                                    if (trigger.SoundCommand is ORTSSoundPlayCommand soundPlayCommand)
                                        foreach (string file in soundPlayCommand.Files)
                                            files.Add(ORTSPaths.GetFileFromFolders(pathArray, file)?.ToLowerInvariant()); // Full file path usually isn't given in the stream, has to be constructed

                            // Some sources are just one sound file, check for this as well
                            if (source.WavFolder != null && source.WavFileName != null)
                                files.Add(Path.GetFullPath(Path.Combine(source.WavFolder, source.WavFileName)).ToLowerInvariant());

                            string soundManager = "";
                            if (source.SMSFolder != null && source.SMSFileName != null)
                                soundManager = Path.GetFullPath(Path.Combine(source.SMSFolder, source.SMSFileName)).ToLowerInvariant();

                            foreach (string sound in files)
                            {
                                if (!source.StaleData && wavPaths.Contains(sound))
                                {
                                    source.StaleData = true;
                                    baseSource.StaleData = true;
                                    found = true;

                                    Trace.TraceInformation("Sound file {0} was updated on disk and will be reloaded.", sound);

                                    // Also mark the sound management system file as stale if it hasn't already been marked
                                    if (SharedSMSFileManager.SharedSMSFiles.ContainsKey(soundManager) && !SharedSMSFileManager.SharedSMSFiles[soundManager].StaleData)
                                        SharedSMSFileManager.SharedSMSFiles[soundManager].StaleData = true;

                                    break;
                                }
                            }

                            if (source.StaleData)
                                break;
                        }
                    }
                    // Continue scanning next set of sound sources, there may be multiple matching sources
                }
            }

            return found;
        }

        /// <summary>
        /// Determines if any of the sound sources associated with the given object are stale
        /// (returns false if there are no sound sources associated with the given object)
        /// </summary>
        /// <param name="owner">The object to which the sound sources are attached.</param>
        /// <returns>bool indicating if any of the sound sources are stale</returns>
        public bool GetStale(object owner)
        {
            bool found = false;

            lock (SoundSources)
            {
                if (SoundSources.ContainsKey(owner))
                {
                    foreach (SoundSourceBase baseSource in SoundSources[owner])
                    {
                        if (baseSource.StaleData)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Checks all sound sources for stale sound management files and sets the stale data flag if any sound managers are stale
        /// </summary>
        /// <returns>bool indicating if any sound source changed from fresh to stale</returns>
        public bool CheckStale()
        {
            // Each entry in the dictionary of SoundSources can have multiple SoundSource objects
            // We need to iterate all of the SoundSource objects to check if stale .sms files are used
            bool found = false;

            lock (SoundSources)
            {
                foreach (List<SoundSourceBase> baseSources in SoundSources.Values)
                {
                    foreach (SoundSourceBase baseSource in baseSources)
                    {
                        // A track sound source actually contains multiple sound sources, need to account for that
                        List<SoundSource> sources = new List<SoundSource>();

                        if (baseSource is SoundSource standardSource)
                        {
                            sources.Add(standardSource);
                        }
                        else if (baseSource is TrackSoundSource trackSource)
                        {
                            sources.AddRange(trackSource.InSources);
                            sources.AddRange(trackSource.OutSources);
                        }

                        foreach (SoundSource source in sources)
                        {
                            if (source.SMSFolder != null && source.SMSFileName != null)
                            {
                                string soundManager = Path.GetFullPath(Path.Combine(source.SMSFolder, source.SMSFileName)).ToLowerInvariant();

                                if (!source.StaleData && SharedSMSFileManager.SharedSMSFiles.ContainsKey(soundManager) && SharedSMSFileManager.SharedSMSFiles[soundManager].StaleData)
                                {
                                    source.StaleData = true;
                                    baseSource.StaleData = true;
                                    found = true;

                                    break;
                                }
                            }
                        }
                    }
                    // Continue scanning next set of sound sources, there may be multiple matching sources
                }
            }

            return found;
        }
    }
}
