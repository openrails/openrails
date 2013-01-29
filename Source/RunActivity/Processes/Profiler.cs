// COPYRIGHT 2009, 2010 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ORTS
{
    public class Profiler
    {
        public readonly string Name;
        public SmoothedData Wall { get; private set; }
        public SmoothedData CPU { get; private set; }
        public SmoothedData Wait { get; private set; }
        readonly Stopwatch TimeTotal;
        readonly Stopwatch TimeRunning;
        TimeSpan TimeCPU;
        TimeSpan LastCPU;
        ProcessThread ProcessThread;
#if DEBUG_THREAD_PERFORMANCE
        StreamWriter DebugFileStream;
#endif

        public Profiler(string name)
        {
            Name = name;
            Wall = new SmoothedData();
            CPU = new SmoothedData();
            Wait = new SmoothedData();
            TimeTotal = new Stopwatch();
            TimeRunning = new Stopwatch();
            TimeTotal.Start();
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream = new StreamWriter(File.OpenWrite("debug_thread_" + name.ToLowerInvariant() + "_profiler.csv"));
            DebugFileStream.Write("Time,Event\n");
#endif
        }

        public void SetThread()
        {
            // This is so that you can identify threads from debuggers like Visual Studio.
            try
            {
                Thread.CurrentThread.Name = Name + " Process";
            }
            catch { }

            // This is so that you can identify threads from programs like Process Monitor. The call
            // should always fail but will appear in Process Monitor's log against the correct thread.
            try
            {
                File.ReadAllBytes(@"DEBUG\THREAD\" + Name + " Process");
            }
            catch { }

#pragma warning disable 618 // Although obsolete GetCurrentThreadId() is required to link to ProcessThread
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                if (thread.Id == AppDomain.GetCurrentThreadId())
                {
                    ProcessThread = thread;
                    break;
                }
            }
#pragma warning restore 618
        }

        public void Start()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},+\n", DateTime.Now.Ticks);
#endif
            TimeRunning.Start();
            if (ProcessThread != null)
                LastCPU = ProcessThread.TotalProcessorTime;
        }

        public void Stop()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},-\n", DateTime.Now.Ticks);
#endif
            TimeRunning.Stop();
            if (ProcessThread != null)
                TimeCPU += ProcessThread.TotalProcessorTime - LastCPU;
        }

        public void Mark()
        {
            // Collect timing data from the timers while they're running and reset them.
            var running = TimeRunning.IsRunning;
            var timeTotal = (float)TimeTotal.ElapsedMilliseconds;
            var timeRunning = (float)TimeRunning.ElapsedMilliseconds;
            var timeCPU = (float)TimeCPU.TotalMilliseconds;

            TimeTotal.Reset();
            TimeTotal.Start();
            TimeRunning.Reset();
            if (running) TimeRunning.Start();
            TimeCPU = TimeSpan.Zero;

            // Calculate the Wall and CPU times from timer data.
            Wall.Update(timeTotal / 1000, 100f * timeRunning / timeTotal);
            CPU.Update(timeTotal / 1000, 100f * timeCPU / timeTotal);
            Wait.Update(timeTotal / 1000, Math.Max(0, (timeRunning - timeCPU) / timeTotal));
        }
    }
}
