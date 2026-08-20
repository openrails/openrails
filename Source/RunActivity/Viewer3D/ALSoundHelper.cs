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

using Orts.Simulation.RollingStocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Orts.Viewer3D
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
        public bool IsExternal { get; private set; }
        public bool IsReleasedWithJump { get; private set; }
        public readonly bool MstsMonoTreatment;
        /// <summary>
        /// How many SoundItems use this. When it falls back to 0, SoundPiece can be disposed
        /// </summary>
        public int RefCount = 1;

        private int[] BufferIDs;
        private int[] BufferLens;

        private float CheckPointS = 0.2f; // In seconds. Should not be set to less than total Thread.Sleep() / 1000
        private float CheckFactor; // In bytes, without considering pitch

        private bool _isValid;
        private bool _isSingle;
        private int _length;

        /// <summary>
        /// Next buffer to queue when streaming
        /// </summary>
        public int NextBuffer;
        /// <summary>
        /// Number of CUE points displayed by Sound Debug Form
        /// </summary>
        public int NumCuePoints;

        /// <summary>
        /// Constructs a Sound Piece
        /// </summary>
        /// <param name="name">Name of the wave file to open</param>
        /// <param name="isExternal">True if external sound, must be converted to mono</param>
        /// <param name="isReleasedWithJump">True if sound possibly be released with jump</param>
        public SoundPiece(string name, bool isExternal, bool isReleasedWithJump)
        {
            Name = name;
            IsExternal = isExternal;
            IsReleasedWithJump = isReleasedWithJump;
            if (!WaveFileData.OpenWavFile(Name, ref BufferIDs, ref BufferLens, IsExternal, isReleasedWithJump, ref NumCuePoints, ref MstsMonoTreatment))
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

        /// <summary>
        /// Has no CUE points
        /// </summary>
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

        /// <summary>
        /// Check if buffer belongs to this sound piece
        /// </summary>
        /// <param name="bufferID">ID of the buffer to check</param>
        /// <returns>True if buffer belongs here</returns>
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

        /// <summary>
        /// Queue all buffers as AL_STREAMING
        /// </summary>
        /// <param name="soundSourceID"></param>
        public void QueueAll(int soundSourceID)
        {
            for (int i = 0; i < BufferIDs.Length; i++)
                if (BufferIDs[i] != 0)
                    OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[i]);
        }

        /// <summary>
        /// Queue only the next buffer as AL_STREAMING
        /// </summary>
        /// <param name="soundSourceID"></param>
        public void Queue2(int soundSourceID)
        {
            if (!isValid)
                return;
            if (BufferIDs[NextBuffer] != 0)
                OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[NextBuffer]);
            if (BufferIDs.Length > 1)
            {
                NextBuffer++;
                NextBuffer %= BufferIDs.Length - 1;
                if (NextBuffer == 0)
                    NextBuffer++;
            }
        }

        /// <summary>
        /// Queue only the final buffer as AL_STREAMING
        /// </summary>
        /// <param name="soundSourceID"></param>
        public void Queue3(int soundSourceID)
        {
            if (isValid && !isSingle && BufferIDs[BufferIDs.Length - 1] != 0)
                OpenAL.alSourceQueueBuffers(soundSourceID, 1, ref BufferIDs[BufferIDs.Length - 1]);
            NextBuffer = 0;
        }

        /// <summary>
        /// Assign buffer to OpenAL sound source as AL_STATIC type for soft looping
        /// </summary>
        /// <param name="soundSourceID"></param>
        public void SetBuffer(int soundSourceID)
        {
            OpenAL.alSourcei(soundSourceID, OpenAL.AL_BUFFER, BufferIDs[0]);
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
        /// <summary>
        /// Wave data to use. A Sound Piece may used by multiple Sound Items
        /// </summary>
        public SoundPiece SoundPiece;
        /// <summary>
        /// Currently executing sound command
        /// </summary>
        public PlayMode PlayMode;
        /// <summary>
        /// Frequency
        /// </summary>
        public float Pitch;
        /// <summary>
        /// Whether can utilize OpenAL Soft Loop Points extension.
        /// Can be publicly set to false in order to disable using the extension for allowing smooth transition.
        /// </summary>
        public bool SoftLoopPoints;

        private PlayState _playState;
        public PlayState PlayState
        {
            get { return _playState; }
            set
            {
                if (value == PlayState.NOP && _playState != PlayState.NOP && SoundPiece != null)
                    SoundPiece.RefCount--;
                _playState = value;
            }
        }

        /// <summary>
        /// Cache containing all wave data
        /// </summary>
        public static Dictionary<string, SoundPiece> AllPieces = new Dictionary<string, SoundPiece>();

        /// <summary>
        /// Sets the Item's piece by its name.
        /// Tries to load the file if not found in cache
        /// </summary>
        /// <param name="Name">Name of the file</param>
        /// <param name="IsExternal">True if external sound</param>
        /// <param name="isReleasedWithJump">True if sound possibly be released with jump</param>
        public void SetPiece(string name, bool IsExternal, bool isReleasedWithJump)
        {
            if (string.IsNullOrEmpty(name))
                return;

            string n = GetKey(name, IsExternal, isReleasedWithJump);

            if (AllPieces.ContainsKey(n))
            {
                SoundPiece = AllPieces[n];
                SoundPiece.RefCount++;
                if (SoundPiece.RefCount < 1)
                    SoundPiece.RefCount = 1;
            }
            else
            {
                SoundPiece = new SoundPiece(name, IsExternal, isReleasedWithJump);
                AllPieces.Add(n, SoundPiece);
            }
            // OpenAL soft loop points extension is disabled until a better way is found for handling smooth transitions with it.
            //SoftLoopPoints = SoundPiece.isSingle;
        }

        /// <summary>
        /// Delete wave data from cache if is no longer in use
        /// </summary>
        /// <param name="name">File name</param>
        /// <param name="isExternal">True if external sound</param>
        /// <param name="isReleasedWithJump">True if sound possibly be released with jump</param>
        public static void Sweep(string name, bool isExternal, bool isReleasedWithJump)
        {
            if (name == null || name == string.Empty)
                return;

            string key = GetKey(name, isExternal, isReleasedWithJump);
            if (AllPieces.ContainsKey(key) && AllPieces[key].RefCount < 1)
            {
                AllPieces[key].Dispose();
                AllPieces.Remove(key);
            }
        }

        /// <summary>
        /// Generate unique key for storing wave data in cache
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isExternal"></param>
        /// <param name="isReleasedWithJump"></param>
        /// <returns></returns>
        public static string GetKey(string name, bool isExternal, bool isReleasedWithJump)
        {
            string key = name;
            if (isReleasedWithJump)
                key += ".j";
            if (isExternal)
                key += ".x";
            return key;
        }

        /// <summary>
        /// Whether is close to exhausting while playing
        /// </summary>
        /// <param name="soundSourceID"></param>
        /// <param name="bufferID"></param>
        /// <returns></returns>
        public bool IsCheckpoint(int soundSourceID, int bufferID)
        {
            return SoundPiece.IsCheckpoint(soundSourceID, bufferID, Pitch);
        }

        public bool IsCheckpoint(int soundSourceID, float pitch)
        {
            Pitch = pitch;
            int bufferID;
            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFER, out bufferID);
            return IsCheckpoint(soundSourceID, bufferID);
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
                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_BUFFERS_QUEUED, out buffersQueued);
                if (buffersQueued < 2)
                    SoundPiece.Queue2(soundSourceID);
            }

            if (PlayMode == PlayMode.Release && SoundPiece.NextBuffer < 2)
                return false;

            return true;
        }

        /// <summary>
        /// Initializes the playing of the item, considering its PlayMode
        /// </summary>
        /// <param name="soundSourceID">ID of the AL sound source</param>
        public bool InitItemPlay(int soundSourceID)
        {
            bool needsFrequentUpdate = false;

            // Get out of AL_LOOP_POINTS_SOFT type playing
            int type;
            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_SOURCE_TYPE, out type);
            if (type == OpenAL.AL_STATIC)
            {
                int state;
                OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_SOURCE_STATE, out state);
                if (state != OpenAL.AL_PLAYING)
                {
                    OpenAL.alSourcei(soundSourceID, OpenAL.AL_BUFFER, OpenAL.AL_NONE);
                    type = OpenAL.AL_UNDETERMINED;
                }
            }

            // Put initial buffers into play
            switch (PlayMode)
            {
                case PlayMode.OneShot:
                case PlayMode.Loop:
                    {
                        if (type != OpenAL.AL_STATIC)
                        {
                            SoundPiece.QueueAll(soundSourceID);
                            PlayState = PlayState.Playing;
                        }
                        else
                            // We need to come back ASAP
                            needsFrequentUpdate = true;
                        break;
                    }
                case PlayMode.LoopRelease:
                    {
                        if (SoftLoopPoints && SoundPiece.isSingle)
                        {
                            // Utilizing AL_LOOP_POINTS_SOFT. We need to set a static buffer instead of queueing that.
                            int state;
                            OpenAL.alGetSourcei(soundSourceID, OpenAL.AL_SOURCE_STATE, out state);
                            if (state != OpenAL.AL_PLAYING)
                            {
                                OpenAL.alSourceStop(soundSourceID);
                                OpenAL.alSourcei(soundSourceID, OpenAL.AL_BUFFER, OpenAL.AL_NONE);
                                SoundPiece.SetBuffer(soundSourceID);
                                OpenAL.alSourcePlay(soundSourceID);
                                OpenAL.alSourcei(soundSourceID, OpenAL.AL_LOOPING, OpenAL.AL_TRUE);
                                PlayState = PlayState.Playing;
                            }
                            else
                                // We need to come back ASAP
                                needsFrequentUpdate = true;
                        }
                        else
                        {
                            if (type != OpenAL.AL_STATIC)
                            {
                                SoundPiece.NextBuffer = 0;
                                SoundPiece.Queue2(soundSourceID);
                                PlayState = PlayState.Playing;
                            }
                            else
                                // We need to come back ASAP
                                needsFrequentUpdate = true;
                        }
                        break;
                    }
                default:
                    {
                        PlayState = PlayState.NOP;
                        break;
                    }
            }
            return needsFrequentUpdate;
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
        private const int QUEUELENGHT = 16;

        /// <summary>
        /// ID generated automatically by OpenAL, when activated
        /// </summary>
        public int SoundSourceID = -1;
        bool Looping;
        float RolloffFactor = 1;

        private SoundItem[] SoundQueue = new SoundItem[QUEUELENGHT];
        /// <summary>
        /// Next command is to be inserted here in queue
        /// </summary>
        private int QueueHeader;
        /// <summary>
        /// Currently processing command in queue
        /// </summary>
        private int QueueTail;

        /// <summary>
        /// Whether needs active management, or let just OpenAL do the job
        /// </summary>
        public bool NeedsFrequentUpdate;

        /// <summary>
        /// Attached TrainCar
        /// </summary>
        private TrainCar Car;

        /// <summary>
        /// Whether world position should be ignored
        /// </summary>
        private bool Ignore3D;

        /// <summary>
        /// Constructs a new AL sound source
        /// </summary>
        /// <param name="isEnv">True if environment sound</param>
        /// <param name="rolloffFactor">The number indicating the fade speed by the distance</param>
        public ALSoundSource(bool isEnv, float rolloffFactor)
        {
            SoundSourceID = -1;
            SoundQueue[QueueTail].PlayState = PlayState.NOP;
            RolloffFactor = rolloffFactor;
        }

        private bool MustActivate;
        public static int ActiveCount;
        private static bool MustWarn = true;
        /// <summary>
        /// Tries allocating a new OpenAL SoundSourceID, warns if failed, and sets OpenAL attenuation parameters.
        /// Returns 1 if activation was successful, otherwise 0.
        /// </summary>
        private int TryActivate()
        {
            if (!MustActivate || SoundSourceID != -1 || !Active)
                return 0;

            OpenAL.alGenSources(1, out SoundSourceID);

            if (SoundSourceID == -1)
            {
                if (MustWarn)
                {
                    Trace.TraceWarning("Sound stream activation failed at number {0}", ActiveCount);
                    MustWarn = false;
                }
                return 0;
            }

            ActiveCount++;
            MustActivate = false;
            MustWarn = true;
            WasPlaying = false;
            StoppedAt = double.MaxValue;

            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_MAX_DISTANCE, SoundSource.MaxDistanceM);
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_REFERENCE_DISTANCE, SoundSource.ReferenceDistanceM);
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_MAX_GAIN, 1f);
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_ROLLOFF_FACTOR, RolloffFactor);
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_PITCH, PlaybackSpeed);
            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, Looping ? OpenAL.AL_TRUE : OpenAL.AL_FALSE);

            InitPosition();
            SetVolume();

            //if (OpenAL.HornEffectSlotID <= 0)
            //    OpenAL.CreateHornEffect();
            //
            //OpenAL.alSource3i(SoundSourceID, OpenAL.AL_AUXILIARY_SEND_FILTER, OpenAL.HornEffectSlotID, 0, OpenAL.AL_FILTER_NULL);

            return 1;
        }

        /// <summary>
        /// Set OpenAL gain
        /// </summary>
        private void SetVolume()
        {
            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_GAIN, Active ? Volume : 0);
        }

        /// <summary>
        /// Set whether to ignore 3D position of sound source
        /// </summary>
        public void InitPosition()
        {
            if (Ignore3D)
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

                if (Car != null && !Car.SoundSourceIDs.Contains(SoundSourceID))
                    Car.SoundSourceIDs.Add(SoundSourceID);
            }
        }

        /// <summary>
        /// Queries a new <see cref="SoundSourceID"/> from OpenAL, if one is not allocated yet.
        /// </summary>
        public void HardActivate(bool ignore3D, TrainCar car)
        {
            Ignore3D = ignore3D;
            Car = car;
            MustActivate = true;

            if (SoundSourceID == -1)
                HardCleanQueue();
        }

        /// <summary>
        /// Frees up the allocated <see cref="SoundSourceID"/>, and cleans the playing queue.
        /// </summary>
        public void HardDeactivate()
        {
            if (SoundSourceID != -1)
            {
                if (Car != null)
                    Car.SoundSourceIDs.Remove(SoundSourceID);

                Stop();
                OpenAL.alDeleteSources(1, ref SoundSourceID);
                SoundSourceID = -1;
                ActiveCount--;
            }

            if (SoundSourceID == -1)
                HardCleanQueue();
        }

        private bool _Active;
        public bool Active { get { return _Active; } set { _Active = value; SetVolume(); } }

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
                    SetVolume();
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
                        if (SoundSourceID != -1)
                            OpenAL.alSourcef(SoundSourceID, OpenAL.AL_PITCH, _PlaybackSpeed);
                    }
                }
            }
            get { return _PlaybackSpeed; }
        }

        public float SampleRate { get; private set; }
        /// <summary>
        /// Get predicted playing state, not just the copy of OpenAL's
        /// </summary>
        public bool isPlaying { get; private set; }
        private bool WasPlaying { get; set; }
        private double StoppedAt = double.MaxValue;

        /// <summary>
        /// Updates Items state and Queue
        /// </summary>
        public void Update()
        {
            lock (SoundQueue)
            {
                if (!WasPlaying && isPlaying)
                    WasPlaying = true;

                SkipProcessed();

                if (QueueHeader == QueueTail)
                {
                    NeedsFrequentUpdate = false;
                    XCheckVolumeAndState();
                    return;
                }

                if (SoundSourceID != -1)
                {
                    int p;
                    OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_BUFFERS_PROCESSED, out p);
                    while (p > 0)
                    {
                        OpenAL.alSourceUnqueueBuffer(SoundSourceID);
                        p--;
                    }
                }

                switch (SoundQueue[QueueTail % QUEUELENGHT].PlayState)
                {
                    case PlayState.Playing:
                        var justActivated = TryActivate();
                        switch (SoundQueue[QueueTail % QUEUELENGHT].PlayMode)
                        {
                            // Determine next action if available
                            case PlayMode.LoopRelease:
                            case PlayMode.Release:
                            case PlayMode.ReleaseWithJump:
                                if (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState == PlayState.New
                                    && (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.Release
                                    || SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
                                {
                                    SoundQueue[QueueTail % QUEUELENGHT].PlayMode = SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode;
                                }

                                if (SoundQueue[QueueTail % QUEUELENGHT].Update(SoundSourceID, _PlaybackSpeed))
                                {
                                    Start(); // Restart if buffers had been exhausted because of large update time
                                    NeedsFrequentUpdate = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.IsReleasedWithJump;
                                }
                                else
                                {
                                    LeaveLoop();
                                    SoundQueue[QueueTail % QUEUELENGHT].LeaveItemPlay(SoundSourceID);
                                    Start(); // Restart if buffers had been exhausted because of large update time
                                    NeedsFrequentUpdate = false; // Queued the last chunk, get rest
                                    isPlaying = false;
                                }

                                break;
                            case PlayMode.Loop:
                            case PlayMode.OneShot:
                                if (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState == PlayState.New
                                    && (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.Release
                                    || SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
                                {
                                    SoundQueue[QueueTail % QUEUELENGHT].PlayMode = SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayMode;
                                    SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState = PlayState.NOP;
                                    LeaveLoop();
                                    isPlaying = false;
                                }
                                else if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.Loop)
                                {
                                    // Unlike LoopRelease, which is being updated continuously, 
                                    // unattended Loop must be restarted explicitly after a reactivation.
                                    if (justActivated == 1)
                                        SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.New;

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
                                        isPlaying = true;
                                        NeedsFrequentUpdate = false; // Start unattended looping by OpenAL
                                    }
                                    else
                                    {
                                        LeaveLoop(); // Just in case. Wait one more cycle for our buffer,
                                        isPlaying = false;
                                        NeedsFrequentUpdate = true; // and watch carefully
                                    }
                                }
                                else if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.OneShot)
                                {
                                    NeedsFrequentUpdate = (SoundQueue[(QueueTail + 1) % QUEUELENGHT].PlayState != PlayState.NOP);
                                    int state;
                                    OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_SOURCE_STATE, out state);
                                    if (state != OpenAL.AL_PLAYING || SoundQueue[QueueTail % QUEUELENGHT].IsCheckpoint(SoundSourceID, _PlaybackSpeed))
                                    {
                                        SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                                        isPlaying = false;
                                    }
                                }
                                break;
                        }
                        break;
                    // Found a playable item, play it
                    case PlayState.New:
                        // Only if it is a Play command
                        if (SoundQueue[QueueTail % QUEUELENGHT].PlayMode != PlayMode.Release
                            && SoundQueue[QueueTail % QUEUELENGHT].PlayMode != PlayMode.ReleaseWithJump)
                        {
                            var justActivated_ = TryActivate();
                            int bufferID;
                            OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_BUFFER, out bufferID);

                            // If reactivated LoopRelease sound is already playing, then we are at a wrong place, 
                            // no need for reinitialization, just continue after 1st cue point
                            if (isPlaying && justActivated_ == 1 && SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.LoopRelease)
                            {
                                SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.Playing;
                            }
                            // Wait with initialization of a sound piece similar to the previous one, while that is still in queue.
                            // Otherwise we might end up with queueing the same buffers hundreds of times.
                            else if (!isPlaying || !SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.isMine(bufferID))
                            {
                                NeedsFrequentUpdate = SoundQueue[QueueTail % QUEUELENGHT].InitItemPlay(SoundSourceID);
                                var sampleRate = SampleRate;
                                SampleRate = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.Frequency;
                                if (sampleRate != SampleRate && SampleRate != 0)
                                    PlaybackSpeed *= sampleRate / SampleRate;

                                Start();
                                NeedsFrequentUpdate |= SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.IsReleasedWithJump;
                            }
                        }
                        // Otherwise mark as done
                        else
                        {
                            SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                            isPlaying = false;
                            NeedsFrequentUpdate = false;
                        }
                        break;
                    case PlayState.NOP:
                        NeedsFrequentUpdate = false;
                        LeaveLoop();
                        isPlaying = false;

                        break;
                }

                XCheckVolumeAndState();
            }
        }

        /// <summary>
        /// Clear processed commands from queue
        /// </summary>
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
                Stop();
                SoundQueue[QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                NeedsFrequentUpdate = false;
            }

            if (WasPlaying && !isPlaying || !Active)
            {
                int state;
                OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_SOURCE_STATE, out state);
                if (state != OpenAL.AL_PLAYING)
                {
                    if (StoppedAt > Program.Simulator.ClockTime)
                        StoppedAt = Program.Simulator.GameTime;
                    else if (StoppedAt < Program.Simulator.GameTime - 0.2)
                    {
                        StoppedAt = double.MaxValue;
                        HardDeactivate();
                        WasPlaying = false;
                        MustActivate = true;
                    }
                }
            }
        }

        /// <summary>
        /// Puts a command with filename into Play Queue. 
        /// Tries to optimize by Name, Mode
        /// </summary>
        /// <param name="Name">Name of the wave to play</param>
        /// <param name="Mode">Mode of the play</param>
        /// <param name="isExternal">Indicator of external sound</param>
        /// <param name="isReleasedWithJumpOrOneShotRepeated">Indicator if sound may be released with jump (LoopRelease), or is repeated command (OneShot)</param>
        public void Queue(string Name, PlayMode Mode, bool isExternal, bool isReleasedWithJumpOrOneShotRepeated)
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
                    else if (QueueHeader != QueueTail
                        && SoundQueue[(QueueHeader - 1) % QUEUELENGHT].SoundPiece.Name == Name
                        && SoundQueue[(QueueHeader - 1) % QUEUELENGHT].PlayMode == PlayMode.Loop
                        && Mode == PlayMode.Loop)
                    {
                        // Don't put into queue repeatedly
                    }
                    else
                    {
                        // Cannot optimize, put into Queue
                        SoundQueue[QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal, isReleasedWithJumpOrOneShotRepeated);
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayState = PlayState.New;
                        SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = Mode;
                        if (QueueHeader == QueueTail && SoundQueue[QueueTail % QUEUELENGHT].SoundPiece != null)
                            SampleRate = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.Frequency;

                        QueueHeader++;
                    }

                    return;
                }

                if (QueueHeader != QueueTail)
                {
                    PlayMode prevMode;
                    SoundItem prev;

                    for (int i = 1; i < 5; i++)
                    {

                       prev = SoundQueue[(QueueHeader - i) % QUEUELENGHT];

                        prevMode = prev.PlayMode;

                        // Ignore repeated commands
                        // In case we play OneShot, enable repeating same file only by defining it multiple times in sms, otherwise disable.
                        if (prevMode == Mode && (Mode != PlayMode.OneShot || isReleasedWithJumpOrOneShotRepeated)
                            && prev.SoundPiece != null && prev.SoundPiece.Name == Name)
                            return;
                        if (QueueHeader - i == QueueTail) break;
                    }

                    prev = SoundQueue[(QueueHeader - 1) % QUEUELENGHT];
                    prevMode = prev.PlayMode;

                    var optimized = false;

                    if (prev.PlayState == PlayState.New)
                    {
                        // Optimize play modes
                        switch (prev.PlayMode)
                        {
                            case PlayMode.Loop:
                                if (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump)
                                {
                                    prevMode = Mode;
                                }
                                break;
                            case PlayMode.LoopRelease:
                                if (prev.SoundPiece.Name == Name && Mode == PlayMode.Loop)
                                {
                                    prevMode = Mode;
                                }
                                else if (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump)
                                {
                                    // If interrupted, release it totally. Repeated looping sounds are "parked" with new state,
                                    // so a release command should completely eliminate them
                                    prevMode = Mode;
                                }
                                break;
                            case PlayMode.OneShot:
                                if (prev.SoundPiece.Name == Name && Mode == PlayMode.Loop)
                                {
                                    prevMode = Mode;
                                }
                                break;
                        }

                        if (prevMode != SoundQueue[(QueueHeader - 1) % QUEUELENGHT].PlayMode)
                        {
                            SoundQueue[(QueueHeader - 1) % QUEUELENGHT].PlayMode = prevMode;
                            optimized = true;
                        }
                    }

                    // If releasing, then release all older loops as well:
                    if (QueueHeader - 1 > QueueTail && (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump))
                    {
                        for (var i = QueueHeader - 1; i > QueueTail; i--)
                            if (SoundQueue[(i - 1) % QUEUELENGHT].PlayMode == PlayMode.Loop || SoundQueue[(i - 1) % QUEUELENGHT].PlayMode == PlayMode.LoopRelease)
                                SoundQueue[(i - 1) % QUEUELENGHT].PlayMode = Mode;
                    }
                    if (optimized)
                        return;
                }

                // Cannot optimize, put into Queue
                SoundQueue[QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal, Mode == PlayMode.LoopRelease && isReleasedWithJumpOrOneShotRepeated);
                SoundQueue[QueueHeader % QUEUELENGHT].PlayState = PlayState.New;
                SoundQueue[QueueHeader % QUEUELENGHT].PlayMode = Mode;
                // Need an initial sample rate value for frequency curve calculation
                if (QueueHeader == QueueTail && SoundQueue[QueueTail % QUEUELENGHT].SoundPiece != null)
                    SampleRate = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.Frequency;

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

        /// <summary>
        /// Immediately stops playback of all sounds in the queue and clears wave data
        /// </summary>
        public void ForceResetQueue()
        {
            for (int i = 0; i < QUEUELENGHT; i++)
            {
                SoundQueue[i].PlayState = PlayState.NOP;
                SoundQueue[i].SoundPiece = null;
            }

            QueueHeader = QueueTail = 0;
        }

        /// <summary>
        /// Start OpenAL playback
        /// </summary>
        private void Start()
        {
            int state;

            OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_SOURCE_STATE, out state);
            if (state != OpenAL.AL_PLAYING)
                OpenAL.alSourcePlay(SoundSourceID);
            isPlaying = true;
        }

        /// <summary>
        /// Stop OpenAL playback and flush buffers
        /// </summary>
        public void Stop()
        {
            OpenAL.alSourceStop(SoundSourceID);
            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_BUFFER, OpenAL.AL_NONE); 
            SkipProcessed();
            isPlaying = false;
        }

        /// <summary>
        /// Instruct OpenAL to enter looping playback mode
        /// </summary>
        private void EnterLoop()
        {
            if (Looping)
                return;

            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, OpenAL.AL_TRUE);
            Looping = true;
        }

        /// <summary>
        /// Instruct OpenAL to leave looping playback
        /// </summary>
        private void LeaveLoop()
        {
            OpenAL.alSourcei(SoundSourceID, OpenAL.AL_LOOPING, OpenAL.AL_FALSE);
            Looping = false;
        }

        private bool _MstsMonoTreatment;
        public bool MstsMonoTreatment
        {
            get
            {
                var soundPiece = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece;
                if (soundPiece != null)
                    _MstsMonoTreatment = soundPiece.MstsMonoTreatment;
                return _MstsMonoTreatment;
            }
        }

        private static bool _Muted;
        /// <summary>
        /// Sets OpenAL master gain to 100%
        /// </summary>
        public static void UnMuteAll()
        {
            if (_Muted)
            {
                OpenAL.alListenerf(OpenAL.AL_GAIN, 1);
                _Muted = false;
            }
        }

        /// <summary>
        /// Sets OpenAL master gain to zero
        /// </summary>
        public static void MuteAll()
        {
            if (!_Muted)
            {
                OpenAL.alListenerf(OpenAL.AL_GAIN, 0);
                _Muted = true;
            }
        }

        /// <summary>
        /// Collect data for Sound Debug Form
        /// </summary>
        /// <returns></returns>
        public string[] GetPlayingData()
        {
            string[] retval = new string[4];
            retval[0] = SoundSourceID.ToString();

            if (SoundQueue[QueueTail % QUEUELENGHT].SoundPiece != null)
            {
                retval[1] = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.Name.Split('\\').Last();
                retval[2] = SoundQueue[QueueTail % QUEUELENGHT].SoundPiece.NumCuePoints.ToString();
            }
            else
            {
                retval[1] = "(none)";
                retval[2] = "0";
            }

            if (SoundQueue[QueueTail % QUEUELENGHT].PlayState != PlayState.NOP)
            {
                retval[3] = String.Format("{0} {1}{2}", 
                    SoundQueue[QueueTail % QUEUELENGHT].PlayState,
                    SoundQueue[QueueTail % QUEUELENGHT].PlayMode,
                    SoundQueue[QueueTail % QUEUELENGHT].SoftLoopPoints && SoundQueue[QueueTail % QUEUELENGHT].PlayMode == PlayMode.LoopRelease ? "Soft" : "");
            }
            else
            {
                retval[3] = String.Format("Stopped {0}", SoundQueue[QueueTail % QUEUELENGHT].PlayMode);
            }

            retval[3] += " " + (QueueHeader - QueueTail).ToString();

            return retval;
        }

        public void Dispose()
        {
            if (SoundSourceID != -1)
            {
                OpenAL.alDeleteSources(1, ref SoundSourceID);
                ActiveCount--;
            }
        }
    }
}
