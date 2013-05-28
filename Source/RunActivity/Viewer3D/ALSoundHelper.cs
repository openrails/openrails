// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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

//#define ENUMERATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ORTS
{
    public enum PlayMode
    {
        OneShot,            // Start playing the whole sound stream once, then stop
        Loop,               // Start looping the whole stream, release it only at the end
        LoopRelease,        // Start by playing the first part, then start looping the sustain part of the stream
        Release,            // Release the sound by playing the looped sustain part till its end, then play the last part
        ReleaseWithJump     // Release the sound by playing the looped sustain part till the next cue point, then jump to the last part and play that
    };

    public enum PlayState
    {
        NOP,
        New,
        Playing,
    }

    /// <summary>
    /// Represents a piece of sound => an opened wave file, separated by the CUE points
    /// </summary>
    public class SoundPiece : IDisposable
    {
        public string Name { get; private set; }
        public int Frequency { get; private set; }
        public int BitsPerSample { get; private set; }
        public int Channels { get; private set; }
        public bool IsExternal {get; private set; }

        private int[] BufferIDs;
        private int[] BufferLens;

        private float CheckPointS = 0.15f; // In seconds. Should not be set to less than total Thread.Sleep() / 1000
        private float CheckFactor; // In bytes, without considering pitch

        private bool _isValid;
        private bool _isSingle;
        private int _length;

        public int NextBuffer = 0;

        /// <summary>
        /// Constructs a Sound Piece
        /// </summary>
        /// <param name="Name">Name of the wave file to open</param>
        /// <param name="IsExternal">True if external sound, must be converted to mono</param>
        public SoundPiece(string Name, bool IsExternal)
        {
            this.Name = Name;
            this.IsExternal = IsExternal;
            WaveFileData wfd = new WaveFileData();
            if (!wfd.OpenWavFile(Name, ref BufferIDs, ref BufferLens, IsExternal))
            {
                BufferIDs = new int[1];
                BufferIDs[0] = 0;
                _isValid = false;
                Trace.TraceWarning("Skipped unopenable wave file {0}", Name);
            }
            else
            {
                _isValid = true;
                _isSingle = BufferIDs.Length == 1;
                _length = BufferLens.Sum();

                int tmp;
                int bid = BufferIDs[0];

                foreach (int i in BufferIDs)
                    if (i != 0)
                    {
                        bid = i;
                        break;
                    }

                OpenAL.alGetBufferi(bid, OpenAL.AL_FREQUENCY, out tmp);
                Frequency = tmp;

                OpenAL.alGetBufferi(bid, OpenAL.AL_BITS, out tmp);
                BitsPerSample = tmp;

                OpenAL.alGetBufferi(bid, OpenAL.AL_CHANNELS, out tmp);
                Channels = tmp;

                CheckFactor = (CheckPointS * (float)(Frequency * Channels * BitsPerSample / 8));
            }
        }

        public bool isSingle
        {
            get
            {
                return _isSingle;
            }
        }

        public bool isValid
        {
            get
            {
                return _isValid;
            }
        }

        public int Length
        {
            get
            {
                return _length;
            }
        }

        public bool isMine(int bufferID)
        {
            return (bufferID != 0 && BufferIDs.Any(value => bufferID == value));
        }
        
        public bool isLast(int soundSourceID)
        {
            if (_isSingle)
                return true;

            int bufferID;
            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFER, out bufferID);
            return (bufferID == 0 || bufferID == BufferIDs.LastOrDefault(value => value > 0));
        }

        public bool isFirst(int bufferID)
        {
            return bufferID == BufferIDs[0];
        }

        public bool isSecond(int bufferID)
        {
            return bufferID != BufferIDs.Last() && bufferID == BufferIDs.LastOrDefault(value => value > 0);
        }

        public void QueueAll(int soundSourceID)
        {
            for (int i = 0; i < BufferIDs.Length; i++)
                if (BufferIDs[i] != 0)
                    OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[i]);
        }

        public void Queue2(int soundSourceID)
        {
            if (!isValid)
                return;
            if (BufferIDs[NextBuffer] != 0)
                OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[NextBuffer]);
            NextBuffer++;
            NextBuffer %= BufferIDs.Length - 1;
            if (NextBuffer == 0)
                NextBuffer++;
        }

        public void Queue3(int soundSourceID)
        {
            if (isValid && !isSingle && BufferIDs[BufferIDs.Length - 1] != 0)
                OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[BufferIDs.Length - 1]);
            NextBuffer = 0;
        }

        /// <summary>
        /// Checkpoint when the buffer near exhausting.
        /// </summary>
        /// <param name="soundSourceID">ID of the AL sound source</param>
        /// <param name="bufferID">ID of the buffer</param>
        /// <param name="pitch">Current playback pitch</param>
        /// <returns>True if near exhausting</returns>
        public bool IsCheckpoint(int soundSourceID, int bufferID, float pitch)
        {
            int bid = -1;
            for (int i = 0; i < BufferIDs.Length; i++)
                if (bufferID == BufferIDs[i])
                {
                    bid = i;
                    break;
                }
            if (bid == -1)
                return false;
            
            int len = (int)(CheckFactor * pitch);
            if (BufferLens[bid] < len)
                return true;

            int pos;
            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BYTE_OFFSET, out pos);

            return BufferLens[bid] - len < pos && pos < BufferLens[bid];
        }

        public void Dispose()
        {
            for (int i = 0; i < BufferIDs.Length; i++)
            {
                if (BufferIDs[i] != 0) OpenAL.alDeleteBuffers(1, ref BufferIDs[i]);
            }
        }
    }

    /// <summary>
    /// The SoundItem represents a playable item: the sound to play, the play mode, the pitch. 
    /// A Sound Piece may used by multiple Sound Items
    /// </summary>
    public struct SoundItem
    {
        public SoundPiece SoundPiece;
        public PlayMode PlayMode;
        public PlayState PlayState;
        public float Pitch;

        // Caching of Pieces
        private static Dictionary<string, SoundPiece> AllPieces = new Dictionary<string, SoundPiece>();
        public static void DisposeAll()
        {
            foreach (SoundPiece sp in AllPieces.Values)
            {
                sp.Dispose();
            }

            AllPieces.Clear();
        }

        /// <summary>
        /// Sets the Item's piece by its name
        /// Tries to open if not found in cache
        /// </summary>
        /// <param name="Name">Name of the file</param>
        /// <param name="IsExternal">True if external sound</param>
        public void SetPiece(string name, bool IsExternal)
        {
            if (string.IsNullOrEmpty(name))
                return;

            string n = name;
            if (IsExternal)
                n += ".x";

            if (AllPieces.ContainsKey(n))
            {
                SoundPiece = AllPieces[n];
            }
            else
            {
                SoundPiece = new SoundPiece(name, IsExternal);
                AllPieces.Add(n, SoundPiece);
            }
        }

        public bool IsCheckpoint(int soundSourceID, int bufferID)
        {
            return SoundPiece.IsCheckpoint(soundSourceID, bufferID, Pitch);
        }

        /// <summary>
        /// Updates queue of Sound Piece sustain part for looping or quick releasing
        /// </summary>
        /// <param name="soundSourceID">ID of the AL Sound Source</param>
        /// <param name="pitch">The current pitch of the sound</param>
        /// <returns>False if finished queueing the last chunk in sustain part, True if needs further calling for full Release</returns>
        public bool Update(int soundSourceID, float pitch)
        {
            Pitch = pitch;

            if (PlayMode == PlayMode.Release && SoundPiece.NextBuffer < 2 || PlayMode == PlayMode.ReleaseWithJump)
                return false;

            int bufferID;
            int buffersQueued;
            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFER, out bufferID);
            if (bufferID == 0)
            {
                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFERS_QUEUED, out buffersQueued);
                if (buffersQueued == 0)
                    SoundPiece.Queue2(soundSourceID);
                OpenAL.alSourcePlay(soundSourceID);
            }
            else if (IsCheckpoint(soundSourceID, bufferID))
            {
                OpenAL.alSourceUnqueueBuffers(soundSourceID, 1, ref bufferID);
                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFERS_QUEUED, out buffersQueued);
                if (buffersQueued < 2)
                    SoundPiece.Queue2(soundSourceID);
            }

            if (PlayMode == PlayMode.Release && SoundPiece.NextBuffer < 2)
                return false;

            return true;
        }

        /// <summary>
        /// Initializes the playing of the item by its mode
        /// </summary>
        /// <param name="soundSourceID">ID of the AL sound source</param>
        public void InitItemPlay(int soundSourceID)
        {
            // Put initial buffers into play
            switch (PlayMode)
            {
                case PlayMode.OneShot:
                case PlayMode.Loop:
                    {
                        SoundPiece.QueueAll(soundSourceID);
                        PlayState = PlayState.Playing;
                        break;
                    }
                case PlayMode.LoopRelease:
                    {
                        SoundPiece.NextBuffer = 0;
                        SoundPiece.Queue2(soundSourceID);
                        PlayState = PlayState.Playing;
                        break;
                    }
                default:
                    {
                        PlayState = PlayState.NOP;
                        break;
                    }
            }
        }

        /// <summary>
        /// Finishes the playing cycle, in case of the ReleaseLoopReleaseWithJump
        /// </summary>
        /// <param name="soundSourceID">ID of the AL sound source</param>
        internal void LeaveItemPlay(int soundSourceID)
        {
            if (PlayMode == PlayMode.ReleaseWithJump || PlayMode == PlayMode.Release)
            {
                SoundPiece.Queue3(soundSourceID);
                PlayState = PlayState.NOP;
            }
        }
    }

    /// <summary>
    /// Represents an OpenAL sound source -- 
    /// One MSTS Sound Stream contains one AL Sound Source
    /// </summary>
    public class ALSoundSource : IDisposable
    {
        private static int refCount = 0;
        private const int QUEUELENGHT = 16;

        int SoundSourceID = -1;
        bool _isEnv = false;
        bool _isSlowRolloff = false;
        bool _isLooping = false;
        float _distanceFactor = 10;

        private SoundItem[] SoundQueue = new SoundItem[QUEUELENGHT];
        private int QueueHeader = 0;
        private int QueueTail = 0;

        public bool NeedsFrequentUpdate = false;

        private static void Initialize()
        {
            if (refCount == 0)
            {
#if ENUMERATION
                if (OpenAL.alcIsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATION_EXT") == OpenAL.AL_TRUE)
                {
                    string deviceList = OpenAL.alcGetString(IntPtr.Zero, OpenAL.ALC_DEVICE_SPECIFIER);
                    string[] split = deviceList.Split('\0');
                    Trace.TraceInformation("___devlist {0}",deviceList);
                }
#endif
                int[] attribs = new int[0];
                IntPtr device = OpenAL.alcOpenDevice(null);
                IntPtr context = OpenAL.alcCreateContext(device, attribs);
                OpenAL.alcMakeContextCurrent(context);

                Trace.TraceInformation("OpenAL " + OpenAL.alGetString(OpenAL.AL_VERSION) 
                    + " using device " + OpenAL.alGetString(OpenAL.AL_RENDERER)
                    + " by " + OpenAL.alGetString(OpenAL.AL_VENDOR));
            }

            refCount++;
        }

        private static void Uninitialize()
        {
            refCount--;
            if (refCount == 0)
            {
                SoundItem.DisposeAll();
            }
        }

        /// <summary>
        /// Constructs a new AL sound source
        /// </summary>
        /// <param name="isEnv">True if environment sound</param>
        /// <param name="isSlowRolloff">True if stationary, slow roll off sound</param>
        /// <param name="distanceFactor">The number indicating the fade speed by the distance</param>
        public ALSoundSource(bool isEnv, bool isSlowRolloff, float distanceFactor)
        {
            Initialize();
            SoundSourceID = -1;
            SoundQueue[QueueTail].PlayState = PlayState.NOP;
            _isEnv = isEnv;
            _isSlowRolloff = isSlowRolloff;
            if (distanceFactor != 10000)
                _distanceFactor = 1 / (distanceFactor / 350);
        }

        private bool _nxtUpdate = false;
        private bool _MustActivate = false;

        public static int _ActiveCount = 0;
        private static bool _MustWarn = true;
        private void TryActivate()
        {
            if (_MustActivate)
            {
                if (SoundSourceID == -1)
                {
                    OpenAL.alGenSources(1, out SoundSourceID);
                    // Reset state
                    if (SoundSourceID != -1)
                    {
                        _ActiveCount++;
                        _MustActivate = false;
                        _MustWarn = true;

                        //OpenAL.alSourcef(SoundSourceID, OpenAL.AL_MAX_DISTANCE, _distanceFactor);
                        OpenAL.alSourcef(SoundSourceID, OpenAL.AL_REFERENCE_DISTANCE, 5f); // meter - below is no attenuation
                        OpenAL.alSourcef(SoundSourceID, OpenAL.AL_MAX_GAIN, 1);
                        if (_isSlowRolloff)
                        {
                            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_ROLLOFF_FACTOR, .4f);
                        }
                        else
                        {
                            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_ROLLOFF_FACTOR, _distanceFactor);
                        }

                        if (_Active)
                            SetVolume(_Volume);
                        else
                            SetVolume(0);

                        OpenAL.alSourcef(SoundSourceID, OpenAL.AL_PITCH, _PlaybackSpeed);
                        OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, _isLooping ? 1 : 0);
                    }
                    else if (_MustWarn)
                    {
                        Trace.TraceWarning("Sound stream activation failed at number {0}", _ActiveCount);
                        _MustWarn = false;
                    }
                }
            }
        }

        private void SetVolume(float volume)
        {
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_GAIN, volume);
        }

        public void SetPosition(float[] position)
        {
            OpenAL.alSourcefv(SoundSourceID, OpenAL.AL_POSITION, position);
        }

        public void SetVelocity(float[] velocity)
        {
            OpenAL.alSourcefv(SoundSourceID, OpenAL.AL_VELOCITY, velocity);
        }

        public void Set2D(bool sound2D)
        {
            if (sound2D)
            {
                OpenAL.alSourcei(SoundSourceID, OpenAL.AL_SOURCE_RELATIVE, OpenAL.AL_TRUE);
                OpenAL.alSourcef(SoundSourceID, OpenAL.AL_DOPPLER_FACTOR, 0);
                OpenAL.alSource3f(SoundSourceID, OpenAL.AL_POSITION, 0, 0, 0);
                OpenAL.alSource3f(SoundSourceID, OpenAL.AL_VELOCITY, 0, 0, 0);
            }
            else
            {
                OpenAL.alSourcei(SoundSourceID, OpenAL.AL_SOURCE_RELATIVE, OpenAL.AL_FALSE);
                OpenAL.alSourcef(SoundSourceID, OpenAL.AL_DOPPLER_FACTOR, 1);
            }
        }

        public bool HardActive
        {
            get
            {
                return SoundSourceID != -1;
            }
            set
            {
                if (SoundSourceID == -1 && value)
                {
                    _MustActivate = true;
                    TryActivate();
                }
                else if (SoundSourceID != -1 && !value)
                {
                    Stop();
                    OpenAL.alDeleteSources(1, ref SoundSourceID);
                    SoundSourceID = -1;
                    _ActiveCount--;
                }

                if (SoundSourceID == -1)
                    HardCleanQueue();
            }
        }

        private bool _Active = false;
        public bool Active
        {
            get
            {
                return _Active;
            }
            set
            {
                _Active = value;
                if (_Active)
                    SetVolume(_Volume);
                else
                    SetVolume(0);
            }
        }

        private float _Volume = 1f;
        public float Volume
        {
            get
            {
                return _Volume;
            }
            set
            {
                float newval = value < 0 ? 0 : value;

                if (_Volume != newval)
                {
                    _Volume = newval;
                    if (_Active)
                    {
                        SetVolume(_Volume);
                    }
                    XCheckVolumeAndState();
                }
            }
        }

        private float _PlaybackSpeed = 1;
        public float PlaybackSpeed
        {
            set
            {
                if (_PlaybackSpeed != value)
                {
                    if (!float.IsNaN(value) && value != 0 && !float.IsInfinity(value))
                    {
                        _PlaybackSpeed = value;
                        OpenAL.alSourcef(SoundSourceID, OpenAL.AL_PITCH, _PlaybackSpeed);
                    }
                }
            }
        }

        public float SampleRate { get; private set; }
        public bool isPlaying { get; private set; }

        /// <summary>
        /// Updates Items state and Queue
        /// </summary>
        public void Update()
        {
            lock (SoundQueue)
            {
                if (SoundSourceID == -1)
                {
                    TryActivate();

                    if (_nxtUpdate)
                    {
                        _nxtUpdate = false;
                    }
                    else
                    {
                        NeedsFrequentUpdate = false;
                        return;
                    }
                }

                SkipProcessed();

                if (QueueHeader == QueueTail)
                {
                    NeedsFrequentUpdate = false;
                    return;
                }

                // Find the number of processed buffers and unqueue them
                int p;
                OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_BUFFERS_PROCESSED, out p);
                while (p > 0)
                {
                    int rb = OpenAL.alSourceUnqueueBuffer(SoundSourceID);
                    p--;
                }

                switch (SoundQueue[QueueTail % QUEUELENGHT].PlayState)
                {
                    case PlayState.Playing:
                        {
                            switch (SoundQueue[QueueTail % QUEUELENGHT].PlayMode)
                            {
                                // Determine next action if available
                                case PlayMode.LoopRelease:
                                case PlayMode.Release:
                                case PlayMode.ReleaseWithJump:
                                    {
                                        if (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState == PlayState.New
                                            && (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.Release
                                            || SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
                                        {
                                            SoundQueue[QueueTail % QUEUELENGHT].PlayMode = SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode;
                                        }

                                        if (SoundQueue[QueueTail % QUEUELENGHT].Update(SoundSourceID, _PlaybackSpeed))
                                        {
                                            Start(); // Restart if buffers had been exhausted because of large update time
                                            NeedsFrequentUpdate = true; // We still queueing fast
                                        }
                                        else
                                        {
                                            LeaveLoop();
                                            SoundQueue[QueueTail % QUEUELENGHT].LeaveItemPlay(SoundSourceID);
                                            Start(); // Restart if buffers had been exhausted because of large update time
                                            NeedsFrequentUpdate = false; // Queued the last chunk, get rest
                                        }

                                        break;
                                    }
                                case PlayMode.Loop:
                                case PlayMode.OneShot:
                                    {
                                        if (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState == PlayState.New
                                            && (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.Release
                                            || SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
                                        {
                                            SoundQueue[QueueTail % QUEUELENGHT].PlayMode = SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode;
                                            SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState = PlayState.NOP;
                                            LeaveLoop();
                                        }
                                        else if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.Loop)
                                        {
                                            // The reason of the following is that at Loop type of playing we mustn't EnterLoop() immediately after
                                            // InitItemPlay(), because an other buffer might be playing at that time, and we don't want to loop
                                            // that one. We have to be sure the current loop's buffer is being played already, and all the previous
                                            // ones had been unqueued. This often happens at e.g. Variable2 frequency curves with multiple Loops and
                                            // Releases following each other when increasing throttle.
                                            int bufferID;
                                            OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_BUFFER, out bufferID);
                                            if (SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.isMine(bufferID))
                                            {
                                                EnterLoop();
                                                NeedsFrequentUpdate = false; // Start unattended looping by OpenAL
                                            }
                                            else
                                            {
                                                LeaveLoop(); // Just in case. Wait one more cycle for our buffer,
                                                NeedsFrequentUpdate = true; // and watch carefully
                                            }
                                        }
                                        else if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.OneShot)
                                        {
                                            NeedsFrequentUpdate = false;
                                            if (SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.isLast(SoundSourceID))
                                                SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    // Found a playable item, play it
                    case PlayState.New:
                        {
                            // Only if it is a Play command
                            if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode != PlayMode.Release
                                && SoundQueue[QueueTail % QUEUELENGHT].PlayMode != PlayMode.ReleaseWithJump)
                            {
                                FlushBuffers();
                                SoundQueue[QueueTail % QUEUELENGHT].InitItemPlay(SoundSourceID);
                                SampleRate = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.Frequency;

                                Start();
                                NeedsFrequentUpdate = true;
                            }
                            // Otherwise mark as done
                            else
                            {
                                SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                                NeedsFrequentUpdate = false;
                            }
                            break;
                        }
                    case PlayState.NOP:
                        {
                            NeedsFrequentUpdate = false;
                            break;
                        }
                }

                XCheckVolumeAndState();
            }
        }

        // Skip processed commands
        private void SkipProcessed()
        {
            while (SoundQueue[QueueTail % QUEUELENGHT].PlayState == PlayState.NOP && QueueHeader != QueueTail)
                QueueTail++;
        }

        // This is because: different sound items may appear on the same sound source,
        //  with different activation conditions. If the previous sound is not stopped
        //  it would be audible while the new sound must be playing already.
        // So if the volume is set to 0 by the triggers and the sound itself is released
        //  it will be stopped completely.
        private void XCheckVolumeAndState()
        {
            if (_Volume == 0 && (
                SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.Release ||
                SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
            {
                SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                Stop();
                NeedsFrequentUpdate = false;
            }
        }

        /// <summary>
        /// Puts a command with filename into Play Queue. 
        /// Tries to optimize by Name, Mode
        /// </summary>
        /// <param name="Name">Name of the wave to play</param>
        /// <param name="Mode">Mode of the play</param>
        /// <param name="isExternal">Indicator of external sound</param>
        public void Queue(string Name, PlayMode Mode, bool isExternal)
        {
            lock (SoundQueue)
            {
                if (SoundSourceID == -1)
                {
                    if (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump)
                    {
                        QueueHeader = QueueTail;
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayState = PlayState.NOP;
                    }
                    else if (Mode == PlayMode.Loop || Mode == PlayMode.LoopRelease)
                    {
                        // Cannot optimize, put into Queue
                        SoundQueue[QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal);
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayState = PlayState.New;

                        // If single, it can't LoopReleased, play one shot instead
                        if (SoundQueue[QueueHeader % QUEUELENGHT].SoundPiece.isSingle && Mode == PlayMode.LoopRelease)
                            SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = PlayMode.Loop;
                        else
                            SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = Mode;

                        QueueHeader++;
                    }

                    return;
                }

                if (QueueHeader != QueueTail)
                {
                    SoundItem prev = SoundQueue[(QueueHeader - 1) % QUEUELENGHT];

                    PlayMode prevMode = prev.PlayMode;

                    // Ignore repeated Loop commands
                    if (prevMode == Mode && Mode != PlayMode.OneShot)
                    {
                        if (prev.SoundPiece != null && prev.SoundPiece.Name == Name)
                            return;
                    }

                    if (prev.PlayState == PlayState.New)
                    {
                        // Optimize play modes
                        switch (prev.PlayMode)
                        {
                            // Whole loop
                            case PlayMode.Loop:
                                {
                                    // If interrupted, becomes OneShot
                                    if (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump)
                                    {
                                        prevMode = PlayMode.OneShot;
                                    }
                                    break;
                                }
                            case PlayMode.LoopRelease:
                            case PlayMode.OneShot:
                                {
                                    if (prev.SoundPiece.Name == Name && Mode == PlayMode.Loop)
                                    {
                                        prevMode = Mode;
                                    }
                                    break;
                                }
                        }

                        if (prevMode != SoundQueue[(QueueHeader - 1) % QUEUELENGHT].PlayMode)
                        {
                            SoundQueue[(QueueHeader - 1) % QUEUELENGHT].PlayMode = prevMode;
                            return;
                        }
                    }
                }

                // Cannot optimize, put into Queue
                SoundQueue[QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal);
                SoundQueue[QueueHeader % QUEUELENGHT].PlayState = PlayState.New;

                // If single, it can't LoopReleased, play loop instead
                if (Mode != PlayMode.Release && Mode != PlayMode.ReleaseWithJump)
                {
                    if (SoundQueue[QueueHeader % QUEUELENGHT].SoundPiece.isSingle && Mode == PlayMode.LoopRelease)
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = PlayMode.Loop;
                    else
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = Mode;
                }
                else
                {
                    SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = Mode;
                }

                QueueHeader++;
            }
        }

        private void HardCleanQueue()
        {
            if (QueueHeader == QueueTail)
                return;

            int h = QueueHeader;
            while (h >= QueueTail)
            {
                if (SoundQueue[h % QUEUELENGHT].PlayState == PlayState.NOP)
                {
                    h--;
                    continue;
                }

                if ((SoundQueue[h % QUEUELENGHT].PlayMode == PlayMode.Loop ||
                    SoundQueue[h % QUEUELENGHT].PlayMode == PlayMode.LoopRelease ||
                    (SoundQueue[h % QUEUELENGHT].PlayMode == PlayMode.OneShot && SoundQueue[h % QUEUELENGHT].SoundPiece.Length > 50000)
                    ) &&
                    (SoundQueue[h % QUEUELENGHT].PlayState == PlayState.New ||
                    SoundQueue[h % QUEUELENGHT].PlayState == PlayState.Playing))
                    break;

                if (SoundQueue[h % QUEUELENGHT].PlayMode == PlayMode.Release ||
                    SoundQueue[h % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump)
                {
                    h = QueueTail - 1;
                }

                h--;
            }

            if (h >= QueueTail)
            {
                int i;
                for (i = h-1; i >= QueueTail; i--)
                {
                    SoundQueue[i % QUEUELENGHT].PlayState = PlayState.NOP;
                }

                for (i = QueueHeader; i > h; i--)
                {
                    SoundQueue[i % QUEUELENGHT].PlayState = PlayState.NOP;
                }

                SoundQueue[h % QUEUELENGHT].PlayState = PlayState.New;
                SoundQueue[(h + 1) % QUEUELENGHT].PlayState = PlayState.NOP;

                QueueHeader = h + 1;
                QueueTail = h;
            }
            else
            {
                for (int i = QueueTail; i <= QueueHeader; i++)
                    SoundQueue[i % QUEUELENGHT].PlayState = PlayState.NOP;

                QueueHeader = QueueTail;
            }
        }

        private void Start()
        {
            int state;

            OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_SOURCE_STATE, out state);
            if (state != OpenAL.AL_PLAYING)
                OpenAL.alSourcePlay(SoundSourceID);
            isPlaying = true;
        }

        public void Stop()
        {
            OpenAL.alSourceStop(SoundSourceID);
            FlushBuffers();
            SkipProcessed();
            isPlaying = false;
        }

        public void FlushBuffers()
        {
            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_BUFFER, 0);
        }

        private void EnterLoop()
        {
            if (_isLooping)
                return;

            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, 1);
            _isLooping = true;
        }

        private void LeaveLoop()
        {
            if (!_isLooping)
                return;

            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, 0);
            _isLooping = false;
        }

        private static bool _Muted = false;
        public static void UnMuteAll()
        {
            if (refCount == 0)
                Initialize();

            if (_Muted)
            {
                OpenAL.alListenerf(OpenAL.AL_GAIN, 1);
                _Muted = false;
            }
        }

        public static void MuteAll()
        {
            if (refCount == 0)
                Initialize();

            if (!_Muted)
            {
                OpenAL.alListenerf(OpenAL.AL_GAIN, 0);
                _Muted = true;
            }
        }

        public void Dispose()
        {
            if (SoundSourceID != -1) OpenAL.alDeleteSources(1, ref SoundSourceID);
            Uninitialize();
        }
    }
}
