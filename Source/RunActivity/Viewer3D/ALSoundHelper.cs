using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public enum PlayMode
    {
        OneShot,
        HalfShot,
        Loop,
        LoopRelease,
        Release,
        ReleaseWithJump
    };

    public enum PlayState
    {
        NOP,
        New,
        Playing,
        Stopping
    }

    public class SoundPiece : IDisposable
    {
        public string Name { get; private set; }
        public int Frequency { get; private set; }
        public int BitsPerSample { get; private set; }
        public int Channels { get; private set; }
        public bool IsExternal {get; private set; }

        private int[] BufferIDs = new int[] { 0, 0, 0 };
        private int[] BufferLens = new int[] { 0, 0, 0 };

        private int _CheckFactor = 8192;

        private bool _isValid;
        private bool _isSingle;

        public SoundPiece(string Name, int IsExternal)
        {
            this.Name = Name;
            this.IsExternal = IsExternal == 1;
            if (!OpenAL.OpenWavFile(Name, BufferIDs, BufferLens, IsExternal))
            {
                BufferIDs[0] = 0;
                BufferIDs[1] = 0;
                BufferIDs[2] = 0;
                _isValid = false;
            }
            else
            {
                _isValid = true;
                _isSingle = BufferIDs[1] == 0 && BufferIDs[2] == 0;

                int tmp;

                int bid = BufferIDs[0];
                if (bid == 0)
                    bid = BufferIDs[1];
                if (bid == 0)
                    bid = BufferIDs[2];

                OpenAL.alGetBufferi(bid, OpenAL.AL_FREQUENCY, out tmp);
                Frequency = tmp;

                OpenAL.alGetBufferi(bid, OpenAL.AL_BITS, out tmp);
                BitsPerSample = tmp;

                OpenAL.alGetBufferi(bid, OpenAL.AL_CHANNELS, out tmp);
                Channels = tmp;

                _CheckFactor *= Channels;
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
                if (isSingle)
                    return BufferLens[0];
                else
                    return BufferLens[0] + BufferLens[1] + BufferLens[2];
            }
        }

        public bool isLast(int bufferID)
        {
            if (_isSingle)
                return true;

            if (BufferIDs[2] == 0)
                if (BufferIDs[1] == 0)
                    return bufferID == BufferIDs[0];
                else
                    return bufferID == BufferIDs[1];
            else
                return bufferID == BufferIDs[2];
        }

        public bool isSecond(int bufferID)
        {
            if (BufferIDs[1] == 0)
                return bufferID == BufferIDs[0];
            else
                return bufferID == BufferIDs[1];
        }

        public void QueueAll(int SoundSourceID)
        {
            if (BufferIDs[0] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[0]);
            }

            if (BufferIDs[1] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[1]);
            }

            if (BufferIDs[2] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[2]);
            }
        }

        public void Queue12(int SoundSourceID)
        {
            if (BufferIDs[0] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[0]);
            }

            if (BufferIDs[1] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[1]);
            }

        }

        public void Queue2(int SoundSourceID)
        {
            if (BufferIDs[1] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[1]);
            }
        }

        public void Queue3(int SoundSourceID)
        {
            if (BufferIDs[2] != 0)
            {
                OpenAL.alSourceQueueBuffers(SoundSourceID, 1, ref BufferIDs[2]);
            }
        }

        public bool isCheckpoint(int SoundSourceID, int BufferID, float Pitch)
        {
            return isUnder(SoundSourceID, BufferID, (int)((float)_CheckFactor * Pitch));
        }

        private bool isUnder(int SoundSourceID, int BufferID, int len)
        {
            int pos;
            OpenAL.alGetSourcei(SoundSourceID, OpenAL.AL_BYTE_OFFSET, out pos);
            int bid = -1;
            if (BufferID == BufferIDs[0])
                bid = 0;
            else if (BufferID == BufferIDs[1])
                bid = 1;
            else if (BufferID == BufferIDs[2])
                bid = 2;

            if (bid == -1)
                return false;

            if (BufferLens[bid] < len)
                return true;

            //if (pos == 0)
            //    OpenAL.alSourcePlay(SoundSourceID);

            bool retval = pos > BufferLens[bid] - len && pos < BufferLens[bid];

            return retval;
        }

        public void Dispose()
        {
            if (BufferIDs[0] != 0) OpenAL.alDeleteBuffers(1, ref BufferIDs[0]);
            if (BufferIDs[1] != 0) OpenAL.alDeleteBuffers(1, ref BufferIDs[1]);
            if (BufferIDs[2] != 0) OpenAL.alDeleteBuffers(1, ref BufferIDs[2]);
        }
    }

    public struct SoundItem
    {
        public SoundPiece SoundPiece;
        public PlayMode PlayMode;
        public PlayState PlayState;
        private bool _hasCheckpoint;
        public float _Pitch;

        private static Dictionary<string, SoundPiece> _AllPieces = new Dictionary<string, SoundPiece>();
        public static void DisposeAll()
        {
            foreach (SoundPiece sp in _AllPieces.Values)
            {
                sp.Dispose();
            }

            _AllPieces.Clear();
        }

        public void SetPiece(string Name, int IsExternal)
        {
            string n = Name;
            if (IsExternal == 1)
                n += ".x";

            if (_AllPieces.ContainsKey(n))
            {
                SoundPiece = _AllPieces[n];
            }
            else
            {
                SoundPiece = new SoundPiece(Name, IsExternal);
                _AllPieces.Add(n, SoundPiece);
            }
        }

        public void InitItemPlay(int SoundSourceID)
        {
            _hasCheckpoint = false;
            // Put initial buffers into play
            switch (PlayMode)
            {
                case PlayMode.OneShot:
                    {
                        SoundPiece.QueueAll(SoundSourceID);
                        PlayState = PlayState.Playing;
                        break;
                    }
                case PlayMode.Loop:
                    {
                        SoundPiece.QueueAll(SoundSourceID);

                        PlayState = PlayState.Playing;
                        break;
                    }
                case PlayMode.LoopRelease:
                case PlayMode.HalfShot:
                    {
                        if (SoundPiece.isSingle)
                        {
                            SoundPiece.QueueAll(SoundSourceID);
                        }
                        else
                        {
                            SoundPiece.Queue12(SoundSourceID);
                        }
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

        private bool isCheckpoint(int SoundSourceID, int bufferID, bool isProcessed)
        {
            bool isCheckpoint = SoundPiece.isCheckpoint(SoundSourceID, bufferID, _Pitch);
            if (!isCheckpoint)
            {
                _hasCheckpoint = false;
                return false;
            }

            if (_hasCheckpoint)
            {
                return false;
            }

            _hasCheckpoint = isCheckpoint;
            return _hasCheckpoint;
        }

        public PlayState Update(int SoundSourceID, int bufferID, bool isProcessed, float Pitch)
        {
            // If previously it has marked Stopping, it is finished now
            if (PlayState == PlayState.Stopping && bufferID == 0)
            {
                PlayState = PlayState.NOP;
                _hasCheckpoint = false;
                return PlayState;
            }

            _Pitch = Pitch;

            switch (PlayMode)
            {
                case PlayMode.Loop:
                    {
                        if (isProcessed)
                            _hasCheckpoint = false;

                        // Check if playing already, if not, start
                        //   It is the case of the depleted buffers
                        
                        if (bufferID == 0)
                        {
                            SoundPiece.QueueAll(SoundSourceID);
                            OpenAL.alSourcePlay(SoundSourceID);
                        }
                        else
                        
                        {
                            if (!isCheckpoint(SoundSourceID, bufferID, isProcessed))
                                return PlayState;

                            if (SoundPiece.isLast(bufferID))
                            {
                                SoundPiece.QueueAll(SoundSourceID);
                            }
                        }

                        break;
                    }
                case PlayMode.LoopRelease:
                    {
                        if (isProcessed)
                            _hasCheckpoint = false;

                        // Check if playing already, if not, start
                        //   It is the case of the depleted buffers
                        
                        if (bufferID == 0)
                        {
                            SoundPiece.Queue2(SoundSourceID);
                            OpenAL.alSourcePlay(SoundSourceID);
                        }
                        else
                        
                        {
                            if (!isCheckpoint(SoundSourceID, bufferID, isProcessed))
                                return PlayState;

                            if (SoundPiece.isSecond(bufferID))
                            {
                                SoundPiece.Queue2(SoundSourceID);
                            }
                        }
                        
                        break;
                    }
                case PlayMode.Release:
                    {
                        if (isProcessed)
                            _hasCheckpoint = false;

                        if (bufferID == 0)
                        {
                            PlayState = PlayState.NOP;
                        }
                        else
                        {
                            if (!isCheckpoint(SoundSourceID, bufferID, isProcessed))
                                return PlayState;

                            if ((SoundPiece.isSingle && SoundPiece.isLast(bufferID)) ||
                                (!SoundPiece.isSingle && SoundPiece.isSecond(bufferID)))
                            {
                                PlayState = PlayState.NOP;
                            }
                        }
                        break;
                    }
                case PlayMode.ReleaseWithJump:
                    {
                        if (bufferID == 0)
                        {
                            PlayState = PlayState.NOP;
                        }
                        else
                        {
                            if (!isCheckpoint(SoundSourceID, bufferID, isProcessed))
                                return PlayState;

                            if (SoundPiece.isSecond(bufferID))
                            {
                                SoundPiece.Queue3(SoundSourceID);
                            }
                            else if (SoundPiece.isLast(bufferID))
                            {
                                PlayState = PlayState.NOP;
                            }
                        }
                        break;
                    }
                case PlayMode.OneShot:
                    {
                        if (bufferID == 0)
                        {
                            PlayState = PlayState.NOP;
                        }
                        else
                        {
                            if (!isCheckpoint(SoundSourceID, bufferID, isProcessed))
                                return PlayState;

                            if (SoundPiece.isLast(bufferID))
                            {
                                PlayState = PlayState.NOP;
                            }
                        }
                        break;
                    }
                case PlayMode.HalfShot:
                    {
                        if (bufferID == 0)
                        {
                            PlayState = PlayState.NOP;
                        }
                        else
                        {
                            if (!isProcessed)
                                return PlayState;

                            if (SoundPiece.isSingle || SoundPiece.isSecond(bufferID))
                            {
                                PlayState = PlayState.NOP;
                            }
                        }
                        break;
                    }
            }

            return PlayState;
        }
    }

    public class ALSoundSource : IDisposable
    {
        private static IntPtr OpenAlDevice = IntPtr.Zero;
        private static IntPtr OpenAlContext = IntPtr.Zero;
        private static int refCount = 0;

        private const int QUEUELENGHT = 16;

        float[] ListenerPosition = new float[] { 0.0f, 0.0f, 0.0f };
        float[] ListenerVelocity = new float[] { 0.0f, 0.0f, 0.0f };
        float[] ListenerOrientation = new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f };

        int _SoundSourceID = -1;
        Guid _SSGuid;
        bool _isEnv = false;
        bool _isLinearRolloff = false;

        private SoundItem[] _Queue = new SoundItem[QUEUELENGHT];
        private int _QueueHeader = 0;
        private int _QueueTail = 0;

        private static void Initialize()
        {
            if (refCount == 0)
            {
                try
                {
                    int r  = OpenAL.alutInit();
                    int err = OpenAL.alutGetError();
                }
                catch (Exception e)
                {
                    throw (e);
                }
            }

            refCount++;
        }

        private static void Uninitialize()
        {
            refCount--;
            if (refCount == 0)
            {
                SoundItem.DisposeAll();
                OpenAL.alutExit();
            }
        }

        public ALSoundSource(bool isEnv, bool isLinearRolloff)
        {
            Initialize();
            _SoundSourceID = -1;
            _Queue[_QueueTail].PlayState = PlayState.NOP;
            _SSGuid = Guid.NewGuid();
            _isEnv = isEnv;
            _isLinearRolloff = isLinearRolloff;
        }

        private bool _nxtUpdate = false;
        private bool _MustActivate = false;


        private static int _ActiveCount = 0;
        private static bool _MustWarn = true;
        private void TryActivate()
        {
            if (_MustActivate)
            {
                if (_SoundSourceID == -1)
                {
                    OpenAL.alGenSources(1, out _SoundSourceID);
                    // Reset state
                    if (_SoundSourceID != -1)
                    {
                        _ActiveCount++;
                        _MustActivate = false;
                        _MustWarn = true;

                        // Experimenting
                        /*
                        if (_isLinearRolloff)
                        {
                            OpenAL.alDistanceModel(OpenAL.AL_LINEAR_DISTANCE);
                            OpenAL.alSourcef(_SoundSourceID, OpenAL.AL_ROLLOFF_FACTOR, .5f);
                        }
                        else
                        {
                            OpenAL.alDistanceModel(OpenAL.AL_EXPONENT_DISTANCE_CLAMPED);
                            OpenAL.alSourcef(_SoundSourceID, OpenAL.AL_ROLLOFF_FACTOR, 2f);
                        }
                        */

                        if (_Active)
                            SetVolume(_Volume);
                        else
                            SetVolume(0);

                        OpenAL.alSourcef(_SoundSourceID, OpenAL.AL_PITCH, _PlaybackSpeed);
                    }
                    else if (_MustWarn)
                    {
                        Console.Write("\r\nSound source activation failed at number: {0}", _ActiveCount);
                        _MustWarn = false;
                    }
                }
            }
        }

        private void SetVolume(float volume)
        {
            bool bred = false;
            if (_Queue[_QueueTail % QUEUELENGHT].SoundPiece != null && !_Queue[_QueueTail % QUEUELENGHT].SoundPiece.IsExternal)
            {
                volume *= .8f;
                bred = true;
            }
            OpenAL.alSourcef(_SoundSourceID, OpenAL.AL_GAIN, volume);
        }

        public void SetPosition(float x, float y, float z)
        {
            OpenAL.alSource3f(_SoundSourceID, OpenAL.AL_POSITION, x / 10, y / 10, z / 10);
        }

        public void SetVelocity(float x, float y, float z)
        {
            OpenAL.alSource3f(_SoundSourceID, OpenAL.AL_VELOCITY, x , y , z );
        }

        public bool HardActive
        {
            get
            {
                return _SoundSourceID != -1;
            }
            set
            {
                if (_SoundSourceID == -1 && value)
                {
                    _MustActivate = true;
                    TryActivate();
                }
                else if (_SoundSourceID != -1 && !value)
                {
                    Stop();
                    OpenAL.alDeleteSources(1, ref _SoundSourceID);
                    _SoundSourceID = -1;
                    _ActiveCount--;
                }

                if (_SoundSourceID == -1)
                    HardCleanQueue();
            }
        }

        private bool _Active = true;
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
                        OpenAL.alSourcef(_SoundSourceID, OpenAL.AL_PITCH, _PlaybackSpeed);
                    }
                }
            }
        }

        public float SampleRate { get; private set; }
        public bool isPlaying { get; private set; }

        // Updates Items state and Queue
        public void Update()
        {
            if (_SoundSourceID == -1)
            {
                TryActivate();

                if (_nxtUpdate)
                {
                    _nxtUpdate = false;
                }
                else
                    return;
            }
            
            // First check queue if items available
            XCheckQueue();

            XCheckVolumeAndState();

            // Find the number of processed buffers
            // if processed, unqueue that
            int p = BuffersProcessed();

            bool isBufferProcessed = p != 0;
            while (p > 0)
            {
                int rb = OpenAL.alSourceUnqueueBuffer(_SoundSourceID);
                p--;
            }

            int b;
            OpenAL.alGetSourcei(_SoundSourceID, OpenAL.AL_BUFFER, out b);

            // Signal playing state
            isPlaying = b != 0;

            // If the current item is finishing, check Queue again to play new items
            // They won't get played while an another Item is playing
            if (_Queue[_QueueTail % QUEUELENGHT].PlayState != PlayState.NOP)
            {
                _Queue[_QueueTail % QUEUELENGHT].Update(_SoundSourceID, b, isBufferProcessed, _PlaybackSpeed);
                XCheckQueue();
            }
        }

        // Skip processed commands
        private void SkipProcessed()
        {
            while (_Queue[_QueueTail % QUEUELENGHT].PlayState == PlayState.NOP && _QueueHeader != _QueueTail)
                _QueueTail++;
        }

        // (Cross)checks the Queue for playing
        private void XCheckQueue()
        {
            SkipProcessed();

            if (_QueueHeader == _QueueTail)
                return;
            
            // Get Current and Next item
            SoundItem cur = _Queue[_QueueTail % QUEUELENGHT];
            SoundItem nxt = _Queue[(_QueueTail + 1) % QUEUELENGHT];

            switch (cur.PlayState)
            {
                // Found a playable item, play it
                case PlayState.New:
                    {
                        // Only if it is a Play command
                        if (cur.PlayMode != PlayMode.Release && cur.PlayMode != PlayMode.ReleaseWithJump)
                        {
                            _Queue[_QueueTail % QUEUELENGHT].InitItemPlay(_SoundSourceID);
                            SampleRate = _Queue[_QueueTail % QUEUELENGHT].SoundPiece.Frequency;
                            //if (_Active) SetVolume(_Volume);
                            Start();
                        }
                        // Otherwise mark as done
                        else
                        {
                            _Queue[_QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                        }
                        break;
                    }
                // Current is Playing
                case PlayState.Playing:
                    {
                        switch (cur.PlayMode)
                        {
                            // Determine next action if available
                            case PlayMode.Loop:
                            case PlayMode.LoopRelease:
                                {
                                    if (nxt.PlayState == PlayState.New && (
                                        nxt.PlayMode == PlayMode.Release || nxt.PlayMode == PlayMode.ReleaseWithJump))
                                    {
                                        _Queue[_QueueTail % QUEUELENGHT].PlayMode = nxt.PlayMode;
                                        _Queue[(_QueueTail + 1) % QUEUELENGHT].PlayState = PlayState.NOP;
                                    }

                                    break;
                                }
                            // Or optimize out unnecessary commands
                            case PlayMode.OneShot:
                            case PlayMode.HalfShot:
                                {
                                    if (nxt.PlayState == PlayState.New &&
                                        nxt.PlayMode == PlayMode.Release || nxt.PlayMode == PlayMode.ReleaseWithJump)
                                    {
                                        _Queue[(_QueueTail + 1) % QUEUELENGHT].PlayState = PlayState.NOP;
                                    }

                                    break;
                                }
                        }
                        break;
                    }
                case PlayState.NOP:
                    {
                        break;
                    }
            }
        }

        private void XCheckVolumeAndState()
        {
            if (_Volume == 0 && (
                _Queue[_QueueTail % QUEUELENGHT].PlayMode == PlayMode.Release ||
                _Queue[_QueueTail % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump))
            {
                _Queue[_QueueTail % QUEUELENGHT].PlayState = PlayState.NOP;
                Stop();
            }
        }

        // Simply puts a command with filename into Play Queue
        public void Queue(string Name, PlayMode Mode, int isExternal)
        {
            if (_SoundSourceID == -1)
            {
                if (Mode == PlayMode.Release || Mode == PlayMode.ReleaseWithJump)
                {
                    _QueueHeader = _QueueTail;
                    _Queue[_QueueHeader % QUEUELENGHT].PlayState = PlayState.NOP;
                }
                else if (Mode == PlayMode.Loop || Mode == PlayMode.LoopRelease)
                {
                    // Cannot optimize, put into Queue
                    _Queue[_QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal);
                    _Queue[_QueueHeader % QUEUELENGHT].PlayState = PlayState.New;

                    // If single, it can't LoopReleased, play one shot instead
                    if (_Queue[_QueueHeader % QUEUELENGHT].SoundPiece.isSingle && Mode == PlayMode.LoopRelease)
                        _Queue[_QueueHeader % QUEUELENGHT].PlayMode = PlayMode.Loop;
                    else
                        _Queue[_QueueHeader % QUEUELENGHT].PlayMode = Mode;

                    _QueueHeader++;
                }

                return;
            }
            
            if (_QueueHeader != _QueueTail)
            {
                SoundItem prev = _Queue[(_QueueHeader - 1) % QUEUELENGHT];

                PlayMode prevMode = prev.PlayMode;

                // Optimize out command
                if (prevMode == Mode)
                    return;

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
                        // LoopRelease
                        case PlayMode.LoopRelease:
                            {
                                // If merely Released, it is a Half Shot
                                if (Mode == PlayMode.Release)
                                {
                                    prevMode = PlayMode.HalfShot;
                                }
                                // If With jump, it is a OneShot
                                else if (Mode == PlayMode.ReleaseWithJump)
                                {
                                    prevMode = PlayMode.OneShot;
                                }
                                break;
                            }
                        // Previously optimized to Half Shot
                        case PlayMode.HalfShot:
                            {
                                // Go back to Loop or to OneShot
                                if ( (prev.SoundPiece.Name == Name) &&
                                    (Mode == PlayMode.LoopRelease || Mode == PlayMode.Loop || Mode == PlayMode.OneShot) )
                                {
                                    prevMode = Mode;
                                }
                                break;
                            }
                        // Previously optimized to OneShot
                        case PlayMode.OneShot:
                            {
                                // Go back to Loop or Half Shot
                                if ( (prev.SoundPiece.Name == Name) &&
                                    (Mode == PlayMode.LoopRelease || Mode == PlayMode.Loop || Mode == PlayMode.HalfShot) )
                                {
                                    prevMode = Mode;
                                }
                                break;
                            }
                    }

                    if (prevMode != _Queue[(_QueueHeader - 1) % QUEUELENGHT].PlayMode)
                    {
                        _Queue[(_QueueHeader - 1) % QUEUELENGHT].PlayMode = prevMode;
                        return;
                    }
                }
            }

            // Cannot optimize, put into Queue
            _Queue[_QueueHeader % QUEUELENGHT].SetPiece(Name, isExternal);
            _Queue[_QueueHeader % QUEUELENGHT].PlayState = PlayState.New;

            // If single, it can't LoopReleased, play one shot instead
            if (_Queue[_QueueHeader % QUEUELENGHT].SoundPiece.isSingle && Mode == PlayMode.LoopRelease)
                _Queue[_QueueHeader % QUEUELENGHT].PlayMode = PlayMode.Loop;
            else
                _Queue[_QueueHeader % QUEUELENGHT].PlayMode = Mode;

            _QueueHeader++;
        }

        private void HardCleanQueue()
        {
            if (_QueueHeader == _QueueTail)
                return;

            int h = _QueueHeader;
            while (h >= _QueueTail)
            {
                if (_Queue[h % QUEUELENGHT].PlayState == PlayState.NOP)
                {
                    h--;
                    continue;
                }

                if ((_Queue[h % QUEUELENGHT].PlayMode == PlayMode.Loop ||
                    _Queue[h % QUEUELENGHT].PlayMode == PlayMode.LoopRelease ||
                    (_Queue[h % QUEUELENGHT].PlayMode == PlayMode.OneShot && _Queue[h % QUEUELENGHT].SoundPiece.Length > 50000)
                    ) &&
                    (_Queue[h % QUEUELENGHT].PlayState == PlayState.New ||
                    _Queue[h % QUEUELENGHT].PlayState == PlayState.Playing))
                    break;

                if (_Queue[h % QUEUELENGHT].PlayMode == PlayMode.Release ||
                    _Queue[h % QUEUELENGHT].PlayMode == PlayMode.ReleaseWithJump)
                {
                    h = _QueueTail - 1;
                }

                h--;
            }

            if (h >= _QueueTail)
            {
                int i;
                for (i = h-1; i >= _QueueTail; i--)
                {
                    _Queue[i % QUEUELENGHT].PlayState = PlayState.NOP;
                }

                for (i = _QueueHeader; i > h; i--)
                {
                    _Queue[i % QUEUELENGHT].PlayState = PlayState.NOP;
                }

                _Queue[h % QUEUELENGHT].PlayState = PlayState.New;
                _Queue[(h + 1) % QUEUELENGHT].PlayState = PlayState.NOP;

                _QueueHeader = h + 1;
                _QueueTail = h;
            }
            else
            {
                for (int i = _QueueTail; i <= _QueueHeader; i++)
                    _Queue[i % QUEUELENGHT].PlayState = PlayState.NOP;

                _QueueHeader = _QueueTail;
            }
        }

        private int BuffersProcessed()
        {
            int pnum;
            OpenAL.alGetSourcei(_SoundSourceID, OpenAL.AL_BUFFERS_PROCESSED, out pnum);
            return pnum;
        }

        private void Start()
        {
            int state;
            
            OpenAL.alGetSourcei(_SoundSourceID, OpenAL.AL_SOURCE_STATE, out state);
            if (state != OpenAL.AL_PLAYING)
                OpenAL.alSourcePlay(_SoundSourceID);
        }

        public void Stop()
        {
            OpenAL.alSourceStop(_SoundSourceID);
            SkipProcessed();
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
            if (_SoundSourceID != -1) OpenAL.alDeleteSources(1, ref _SoundSourceID);
            Uninitialize();
        }
    }
}
