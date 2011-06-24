// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
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
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using System.Diagnostics;

namespace ORTS
{
    /// <summary>
    /// Wrapper class for the externals of library OpenAL
    /// </summary>
    public class OpenAL
    {
        public const int AL_BUFFER = 0x1009;
        public const int AL_BUFFERS_PROCESSED = 0x1016;
        public const int AL_PLAYING = 0x1012;
        public const int AL_SOURCE_STATE = 0x1010;
        public const int AL_LOOPING = 0x1007;
        public const int AL_GAIN = 0x100a;
        public const int AL_VELOCITY = 0x1006;
        public const int AL_DISTANCE_MODEL = 0xd000;
        public const int AL_INVERSE_DISTANCE = 0xd001;
        public const int AL_INVERSE_DISTANCE_CLAMPED = 0xd002;
        public const int AL_LINEAR_DISTANCE = 0xd003;
        public const int AL_LINEAR_DISTANCE_CLAMPED = 0xd004;
        public const int AL_EXPONENT_DISTANCE = 0xd005;
        public const int AL_EXPONENT_DISTANCE_CLAMPED = 0xd006;
        public const int AL_MAX_DISTANCE = 0x1023;
        public const int AL_REFERENCE_DISTANCE = 0x1020;
        public const int AL_ROLLOFF_FACTOR = 0x1021;
        public const int AL_PITCH = 0x1003;
        public const int AL_POSITION = 0x1004;
        public const int AL_DIRECTION = 0x1005;
        public const int AL_FREQUENCY = 0x2001;
        public const int AL_BITS = 0x2002;
        public const int AL_CHANNELS = 0x2003;
        public const int AL_BYTE_OFFSET = 0x1026;
        public const int AL_MIN_GAIN = 0x100d;
        public const int AL_MAX_GAIN = 0x100e;

        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern string AlInitialize(string devName);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetBufferi(int buffer, int attribute, out int val);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alGetError();
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteBuffers(int number, [In] ref int buffer);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteSources(int number, [In] int[] sources);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteSources(int number, [In] ref int sources);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDistanceModel(int model);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenSources(int number, out int source);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetSourcei(int source, int attribute, out int val);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetSourcef(int source, int attribute, out float val);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListener3f(int attribute, float value1, float value2, float value3);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListenerfv(int attribute, [In] float[] values);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListenerf(int attribute, float value);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcePlay(int source);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceQueueBuffers(int source, int number, [In] ref int buffer);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcei(int source, int attribute, int val);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcef(int source, int attribute, float val);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSource3f(int source, int attribute, float value1, float value2, float value3);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceStop(int source);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceUnqueueBuffers(int source, int number, ref int buffers);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alGetEnumValue(string enumName);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenBuffers(int number, out int buffer);
        [SuppressUnmanagedCodeSecurity, DllImport("wrap_oal.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alBufferData(int buffer, int format, [In] byte[] data, int size, int frequency);

        public static int alSourceUnqueueBuffer(int SoundSourceID)
        {
            int bufid = 0;
            OpenAL.alSourceUnqueueBuffers(SoundSourceID, 1, ref bufid);
            return bufid;
        }
    }

    /// <summary>
    /// WAVEFILEHEADER binary structure
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack=1)]
    public struct WAVEFILEHEADER
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] szRIFF;
        [FieldOffset(4), MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint ulRIFFSize;
        [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] szWAVE;
    }

    /// <summary>
    /// RIFFCHUNK binary structure
    /// </summary>
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Pack = 1)]
    public struct RIFFCHUNK
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] szChunkName;
        [FieldOffset(4), MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint ulChunkSize;
    }

    /// <summary>
    /// WAVEFORMATEX binary structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    /// <summary>
    /// WAVEFORMATEXTENSIBLE binary structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAVEFORMATEXTENSIBLE
    {
        public WAVEFORMATEX Format;
        public ushort wValidBitsPerSample;
        public uint dwChannelMask;
        public Guid SubFormat;
    }

    /// <summary>
    /// CUECHUNK binary structure
    /// Describes the CUE chunk list of a wave file
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct CUECHUNK
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] szChunkName;
        [FieldOffset(4), MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint ulChunkSize;
        [FieldOffset(8), MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint ulNumCuePts;
    }

    /// <summary>
    /// CUEPT binary structure
    /// Describes one CUE point in CUE list
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CUEPT
    {
        public uint ulID;
        public uint ulPlayPos;
        public uint ulRiffID;
        public uint ulChunkStart;
        public uint ulBlockStart;
        public uint ulByteStart;
    }

    public enum WAVEFORMATTYPE
    {
        WT_UNKNOWN,
        WT_PCM,
        WT_EXT
    }

    /// <summary>
    /// Helper class to load wave files
    /// </summary>
    public class WaveFileData
    {
        // Constants from C header files
        private const ushort WAVE_FORMAT_PCM                = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE         = 0xFFFE;

        private const ushort SPEAKER_FRONT_LEFT             = 0x1;
        private const ushort SPEAKER_FRONT_RIGHT            = 0x2;
        private const ushort SPEAKER_FRONT_CENTER           = 0x4;
        private const ushort SPEAKER_LOW_FREQUENCY          = 0x8;
        private const ushort SPEAKER_BACK_LEFT              = 0x10;
        private const ushort SPEAKER_BACK_RIGHT             = 0x20;
        private const ushort SPEAKER_FRONT_LEFT_OF_CENTER   = 0x40;
        private const ushort SPEAKER_FRONT_RIGHT_OF_CENTER  = 0x80;
        private const ushort SPEAKER_BACK_CENTER            = 0x100;
        private const ushort SPEAKER_SIDE_LEFT              = 0x200;
        private const ushort SPEAKER_SIDE_RIGHT             = 0x400;
        private const ushort SPEAKER_TOP_CENTER             = 0x800;
        private const ushort SPEAKER_TOP_FRONT_LEFT         = 0x1000;
        private const ushort SPEAKER_TOP_FRONT_CENTER       = 0x2000;
        private const ushort SPEAKER_TOP_FRONT_RIGHT        = 0x4000;
        private const ushort SPEAKER_TOP_BACK_LEFT          = 0x8000;
        
        // General info about current wave file
        public bool isKnownType;
        public WAVEFORMATEXTENSIBLE wfEXT;
        public WAVEFORMATTYPE wtType = new WAVEFORMATTYPE();

        public uint ulDataSize;
        public uint ulDataOffset;

        public ushort nChannels;
        public uint nSamplesPerSec;
        public ushort nBitsPerSample;

        public uint ulFormat;
        public uint ulFirstCue;
        public uint ulLastCue;
        public FileStream pFile;

        public WaveFileData()
        {
            pFile = null;
            isKnownType = false;
            wtType = WAVEFORMATTYPE.WT_UNKNOWN;

            ulFormat = 0;
            ulDataSize = 0;
            ulDataOffset = 0;

            nChannels = 0;
            nSamplesPerSec = 0;
            nBitsPerSample = 0;

            ulFirstCue = 0xFFFFFFFF;
            ulLastCue = 0xFFFFFFFF;
        }

        public void Dispose()
        {
            if (pFile != null)
                pFile.Close();

            pFile = null;
        }

        /// <summary>
        /// Tries to read and parse a binary wave file
        /// </summary>
        /// <param name="n">Name of the file</param>
        /// <returns>True if success</returns>
        private bool ParseWAV(string n)
        {
            pFile = File.Open(n, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (pFile == null)
                return false;

            // Read Wave file header
            WAVEFILEHEADER waveFileHeader = new WAVEFILEHEADER();
            {
                GetNextStructureValue<WAVEFILEHEADER>(pFile, out waveFileHeader, - 1);
                // Check if wave file
                string hdr = new string(waveFileHeader.szRIFF);
                if (hdr != "RIFF" && hdr != "WAVE")
                    return false;

                // Read chunks : fmt, data, cue
                RIFFCHUNK riffChunk = new RIFFCHUNK();
                {
                    while (GetNextStructureValue<RIFFCHUNK>(pFile, out riffChunk, -1))
                    {
                        // Format chunk
                        hdr = new string (riffChunk.szChunkName);
                        if (hdr == "fmt ")
                        {
                            WAVEFORMATEXTENSIBLE waveFmt = new WAVEFORMATEXTENSIBLE();
                            if (riffChunk.ulChunkSize <= Marshal.SizeOf(waveFmt))
                            {
                                GetNextStructureValue<WAVEFORMATEXTENSIBLE>(pFile, out waveFmt, (int)riffChunk.ulChunkSize);

                                // Determine if this is a WAVEFORMATEX or WAVEFORMATEXTENSIBLE wave file
                                if (waveFmt.Format.wFormatTag == WAVE_FORMAT_PCM)
                                {
                                    isKnownType = true;
                                    wtType =  WAVEFORMATTYPE.WT_PCM;
                                    waveFmt.wValidBitsPerSample = waveFmt.Format.wBitsPerSample;
                                }
                                else if (waveFmt.Format.wFormatTag == WAVE_FORMAT_EXTENSIBLE)
                                {
                                    isKnownType = true;
                                    wtType = WAVEFORMATTYPE.WT_EXT;
                                }

                                wfEXT = waveFmt;
                                nBitsPerSample = waveFmt.Format.wBitsPerSample;
                                nChannels = waveFmt.Format.nChannels;
                                nSamplesPerSec = waveFmt.Format.nSamplesPerSec;
                            }
                            // Unexpected length
                            else
                            {
                                pFile.Seek(riffChunk.ulChunkSize, SeekOrigin.Current);
                            }
                        }
                        // Data chunk
                        else if (hdr == "data")
                        {
                            ulDataSize = riffChunk.ulChunkSize;
                            ulDataOffset = (uint)pFile.Position;
                            pFile.Seek(riffChunk.ulChunkSize, SeekOrigin.Current);
                        }
                        // CUE points
                        else if (hdr == "cue ")
                        {
                            // Seek back and read CUE header
                            pFile.Seek(Marshal.SizeOf(riffChunk) * -1, SeekOrigin.Current);
                            CUECHUNK cueChunk;
                            GetNextStructureValue<CUECHUNK>(pFile, out cueChunk, -1);
                            {
                                CUEPT cuePt;
                                uint pos;
                                // Read all CUE points
                                for (uint i = 0; i < cueChunk.ulNumCuePts; i++)
                                {
                                    if (GetNextStructureValue<CUEPT>(pFile, out cuePt, -1))
                                    {
                                        pos = 0;
                                        pos += cuePt.ulChunkStart;
                                        pos += cuePt.ulBlockStart;
                                        pos += cuePt.ulByteStart;

                                        // Use only the first and last points
                                        if (ulFirstCue == 0xFFFFFFFF)
                                            ulFirstCue = pos;
                                        else
                                            ulLastCue = pos;
                                    }
                                }
                            }
                        }
                        else // skip the unknown chunks
                        {
                            pFile.Seek(riffChunk.ulChunkSize, SeekOrigin.Current);
                        }

                        // Ensure that we are correctly aligned for next chunk
                        if ((riffChunk.ulChunkSize & 1) == 1)
                            pFile.Seek(1, SeekOrigin.Current);
                    } //get next chunk

                    // If no data found
                    if (ulDataSize == 0 || ulDataOffset == 0)
                        return false;

                    if (ulFirstCue > ulLastCue)
                    {
                        uint tmp = ulFirstCue;
                        ulFirstCue = ulLastCue;
                        ulLastCue = tmp;
                    }

                    return isKnownType;
                }
            }
        }

        /// <summary>
        /// Gets the wave file's correspondig AL format number
        /// </summary>
        /// <param name="pulFormat">Place to put the format number</param>
        /// <returns>True if success</returns>
        private bool GetALFormat(ref int pulFormat)
        {
            pulFormat = 0;

            if (wtType == WAVEFORMATTYPE.WT_PCM)
            {
                if (wfEXT.Format.nChannels == 1)
                {
                    switch (wfEXT.Format.wBitsPerSample)
                    {
                        case 4:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO_IMA4");
                            break;
                        case 8:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO8");
                            break;
                        case 16:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO16");
                            break;
                    }
                }
                else if (wfEXT.Format.nChannels == 2)
                {
                    switch (wfEXT.Format.wBitsPerSample)
                    {
                        case 4:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO_IMA4");
                            break;
                        case 8:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO8");
                            break;
                        case 16:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO16");
                            break;
                    }
                }
                else if ((wfEXT.Format.nChannels == 4) && (wfEXT.Format.wBitsPerSample == 16))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_QUAD16");
            }
            else if (wtType == WAVEFORMATTYPE.WT_EXT)
            {
                if ((wfEXT.Format.nChannels == 1) && ((wfEXT.dwChannelMask == SPEAKER_FRONT_CENTER) || (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT)) || (wfEXT.dwChannelMask == 0)))
                {
                    switch (wfEXT.Format.wBitsPerSample)
                    {
                        case 4:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO_IMA4");
                            break;
                        case 8:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO8");
                            break;
                        case 16:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_MONO16");
                            break;
                    }
                }
                else if ((wfEXT.Format.nChannels == 2) && (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT)))
                {
                    switch (wfEXT.Format.wBitsPerSample)
                    {
                        case 4:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO_IMA4");
                            break;
                        case 8:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO8");
                            break;
                        case 16:
                            pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_STEREO16");
                            break;
                    }
                }
                else if ((wfEXT.Format.nChannels == 2) && (wfEXT.Format.wBitsPerSample == 16) && (wfEXT.dwChannelMask == (SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_REAR16");
                else if ((wfEXT.Format.nChannels == 4) && (wfEXT.Format.wBitsPerSample == 16) && (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_QUAD16");
                else if ((wfEXT.Format.nChannels == 6) && (wfEXT.Format.wBitsPerSample == 16) && (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_51CHN16");
                else if ((wfEXT.Format.nChannels == 7) && (wfEXT.Format.wBitsPerSample == 16) && (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT | SPEAKER_BACK_CENTER)))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_61CHN16");
                else if ((wfEXT.Format.nChannels == 8) && (wfEXT.Format.wBitsPerSample == 16) && (wfEXT.dwChannelMask == (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT | SPEAKER_SIDE_LEFT | SPEAKER_SIDE_RIGHT)))
                    pulFormat = OpenAL.alGetEnumValue("AL_FORMAT_71CHN16");
            }

            return pulFormat != 0;
        }

        /// <summary>
        /// Reads the wave contents of a wave file
        /// </summary>
        /// <param name="ToMono">True if must convert to mono before return</param>
        /// <returns>Read wave data</returns>
        private byte[] ReadData(bool ToMono)
        {
            byte[] buffer = null;
            if (pFile == null || ulDataOffset == 0 || ulDataSize == 0)
            {
                return buffer;
            }

            buffer = new byte[ulDataSize];
            if (buffer == null)
            {
                return buffer;
            }

            pFile.Seek(ulDataOffset, SeekOrigin.Begin);
            int size = (int)ulDataSize;
            if (pFile.Read(buffer, 0, size) != size)
            {
                buffer = null;
            }

            if (ToMono)
            {
                byte[] newbuffer = ConvertToMono(buffer);
                buffer = newbuffer;
            }

            return buffer;
        }

        /// <summary>
        /// Converts the read wave buffer to mono
        /// </summary>
        /// <param name="buffer">Buffer to convert</param>
        /// <returns>The converted buffer</returns>
        private byte[] ConvertToMono(byte[] buffer)
        {
            if (wfEXT.Format.nChannels == 1)
                return buffer;

            int pos = 0;
            int len = (int)ulDataSize / 2;

            byte[] retval = new byte[len];

            if (wfEXT.Format.wBitsPerSample == 8)
            {
                byte newval;

                while (pos < len)
                {
                    newval = buffer[pos * 2];
                    retval[pos] = newval;
                    pos++;
                }
            }
            else
            {
                MemoryStream sms = new MemoryStream(buffer);
                BinaryReader srd = new BinaryReader(sms);
                MemoryStream dms = new MemoryStream(retval);
                BinaryWriter drw = new BinaryWriter(dms);
                ushort newval;

                len /= 2;
                while (pos < len)
                {
                    newval = srd.ReadUInt16();
                    newval = srd.ReadUInt16();
                    drw.Write(newval);
                    pos++;
                }

                drw.Flush();
                drw.Close();
                dms.Flush();
                dms.Close();
                srd.Close();
                sms.Close();
            }

            wfEXT.Format.nChannels = 1;
            ulDataSize = (uint)retval.Length;
            if (ulFirstCue != 0xFFFFFFFF)
                ulFirstCue /= 2;
            if (ulLastCue != 0xFFFFFFFF)
                ulLastCue /= 2;

            return retval;
        }

        /// <summary>
        /// Opens, reads the given wave file. 
        /// Also creates the AL buffers and fills them with data
        /// </summary>
        /// <param name="Name">Name of the wave file to read</param>
        /// <param name="BufferIDs">Array of the buffer IDs to place</param>
        /// <param name="BufferLens">Array of the length data to place</param>
        /// <param name="ToMono">Indicates if the wave must be converted to mono</param>
        /// <returns>True if success</returns>
        public bool OpenWavFile(string Name, ref int[] BufferIDs, ref int[] BufferLens, bool ToMono)
        {
            WaveFileData wfi = new WaveFileData();
            int fmt = -1;

            if (!wfi.ParseWAV(Name))
            {
                return false;
            }

            if (wfi.ulDataSize == 0 || ((int)wfi.ulDataSize) == -1)
            {
                Trace.TraceError("Wave file {0} has invalid length, could not read.", Name);
                return false;
            }

            byte[] buffer = wfi.ReadData(ToMono);
            if (buffer == null)
            {
                return false;
            }

            if (!wfi.GetALFormat(ref fmt))
            {
                return false;
            }

            if (buffer.Length != wfi.ulDataSize)
            {
                Trace.TraceWarning("Wave file {0} has invalid length, File buffer length:{1}, File length from header:{2}; using buffer length for further operations.", 
                    Name, buffer.Length, wfi.ulDataSize);
                wfi.ulDataSize = (uint)buffer.Length;
            }

            if ((int)wfi.ulFirstCue != -1 && (int)wfi.ulLastCue != -1)
            {
                uint adjPos1 = wfi.ulFirstCue * wfi.nBitsPerSample / 8 * wfi.nChannels;
                uint adjPos2 = wfi.ulLastCue * wfi.nBitsPerSample / 8 * wfi.nChannels;

                if (adjPos1 > wfi.ulDataSize || adjPos2 > wfi.ulDataSize)
                {
                    Trace.TraceWarning("Wave file {0} has invalid CUE data, Length: {1}, CUE1: {2}, CUE2: {3}, BitsPerSample: {4}, Channels: {5}; falling back to single buffer.",
                        Name, wfi.ulDataSize, adjPos1, adjPos2, wfi.nBitsPerSample, wfi.nChannels);
                    wfi.ulFirstCue = 0xFFFFFFFF;
                    wfi.ulLastCue = 0xFFFFFFFF;
                }
            }

            if (wfi.ulFirstCue == 0xFFFFFFFF || wfi.ulLastCue == 0xFFFFFFFF)
            {
                BufferLens[0] = (int)wfi.ulDataSize;

                if (BufferLens[0] > 0)
                    OpenAL.alGenBuffers(1, out BufferIDs[0]);
                else
                    BufferIDs[0] = 0;

                {
                    if (BufferLens[0] > 0)
                        OpenAL.alBufferData(BufferIDs[0], fmt, buffer, (int)wfi.ulDataSize, (int)wfi.nSamplesPerSec);
                }

                BufferIDs[1] = 0;
                BufferIDs[2] = 0;
            }
            else
            {
                uint adjPos1 = wfi.ulFirstCue * wfi.nBitsPerSample / 8 * wfi.nChannels;
                uint adjPos2 = wfi.ulLastCue * wfi.nBitsPerSample / 8 * wfi.nChannels;

                BufferLens[0] = (int)adjPos1;
                BufferLens[1] = (int)adjPos2 - (int)adjPos1;
                BufferLens[2] = (int)wfi.ulDataSize - (int)adjPos2;

                if (BufferLens[0] > 0)
                    OpenAL.alGenBuffers(1, out BufferIDs[0]);
                else
                    BufferIDs[0] = 0;

                if (BufferLens[1] > 0)
                    OpenAL.alGenBuffers(1, out BufferIDs[1]);
                else
                    BufferIDs[1] = 0;

                if (BufferLens[2] > 0)
                    OpenAL.alGenBuffers(1, out BufferIDs[2]);
                else
                    BufferIDs[2] = 0;

                if (BufferLens[0] > 0)
                    OpenAL.alBufferData(BufferIDs[0], fmt, buffer, (int)adjPos1, (int)wfi.nSamplesPerSec);

                if (BufferLens[1] > 0)
                {
                    OpenAL.alBufferData(BufferIDs[1], fmt, GetFromArray(buffer, (int)adjPos1, BufferLens[1]), BufferLens[1], (int)wfi.nSamplesPerSec);
                }

                if (BufferLens[2] > 0)
                {
                    OpenAL.alBufferData(BufferIDs[2], fmt, GetFromArray(buffer, (int)adjPos2, BufferLens[2]), BufferLens[2], (int)wfi.nSamplesPerSec);
                }
            }

            return true;
        }

        /// <summary>
        /// Extracts an array of bytes from an another array of bytes
        /// </summary>
        /// <param name="buffer">Initial buffer</param>
        /// <param name="offset">Offset from copy</param>
        /// <param name="len">Number of bytes to copy</param>
        /// <returns>New buffer with the extracted data</returns>
        private byte[] GetFromArray(byte[] buffer, int offset, int len)
        {
            byte[] retval = new byte[len];
            Buffer.BlockCopy(buffer, offset, retval, 0, len);
            return retval;
        }
        
        /// <summary>
        /// Reads a given structure from a FileStream
        /// </summary>
        /// <typeparam name="T">Type to read, must be able to Marshal to native</typeparam>
        /// <param name="fs">FileStream from read</param>
        /// <param name="retval">The filled structure</param>
        /// <param name="len">The bytes to read, -1 if the structure size must be filled</param>
        /// <returns>True if success</returns>
        public bool GetNextStructureValue<T>(FileStream fs, out T retval, int len)
        {
            byte[] buffer;
            retval = default(T);
            if (len == -1)
            {
                buffer = new byte[Marshal.SizeOf(retval.GetType())];
            }
            else
            {
                buffer = new byte[len];
            }

            try
            {
                if (fs.Read(buffer, 0, buffer.Length) != buffer.Length)
                    return false;

                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                retval = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), retval.GetType());
                handle.Free();
                return true;
            }
            catch 
            {
                return false;
            }
        } 
    }
}

