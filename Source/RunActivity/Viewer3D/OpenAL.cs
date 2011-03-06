using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace ORTS
{

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

        [SuppressUnmanagedCodeSecurity, DllImport("wfrd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool OpenWavFile(string FileName, [In, Out] int[]BufferIDs, [In, Out] int[]BufferLengths, int IsExternal);
        [SuppressUnmanagedCodeSecurity, DllImport("wfrd.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alSourceUnqueueBuffer(int SoundSourceID);

        [SuppressUnmanagedCodeSecurity, DllImport("alut.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alutInit(int[] argcp, string[] argv);
        public static int alutInit()
        {
            return alutInit(null, null);
        }
        [SuppressUnmanagedCodeSecurity, DllImport("alut.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alutExit();
        [SuppressUnmanagedCodeSecurity, DllImport("alut.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alutGetError();

        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetBufferi(int buffer, int attribute, out int val);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int alGetError();
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteBuffers(int number, [In] ref int buffer);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteSources(int number, [In] int[] sources);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteSources(int number, [In] ref int sources);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDistanceModel(int model);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenSources(int number, out int source);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetSourcei(int source, int attribute, out int val);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetSourcef(int source, int attribute, out float val);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListener3f(int attribute, float value1, float value2, float value3);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListenerfv(int attribute, [In] float[] values);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListenerf(int attribute, float value);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcePlay(int source);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceQueueBuffers(int source, int number, [In] ref int buffer);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcei(int source, int attribute, int val);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcef(int source, int attribute, float val);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSource3f(int source, int attribute, float value1, float value2, float value3);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceStop(int source);
        [SuppressUnmanagedCodeSecurity, DllImport("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceUnqueueBuffers(int source, int number, [In] IntPtr buffers);

    }
}
