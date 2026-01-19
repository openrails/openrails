// COPYRIGHT 2022 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Orts.Processes;
using ORTS.Common;

namespace Orts.Viewer3D.Processes
{
    /// <summary>
    /// The host process is used to collect and provide details about the
    /// host environment and the application's consumption of resources,
    /// such as CPU, GPU, and memory usage.
    /// </summary>
    public class HostProcess
    {
        public int ProcessorCount { get; } = System.Environment.ProcessorCount;
        public float CLRMemoryAllocatedBytesPerSec { get; private set; }
        public float CPUMemoryPrivate { get; private set; }
        public float CPUMemoryWorkingSet { get; private set; }
        public float CPUMemoryWorkingSetPrivate { get; private set; }
        public float CPUMemoryVirtual { get; private set; }
        public ulong CPUMemoryVirtualLimit { get; private set; }
        public float GPUMemoryCommitted { get; private set; }
        public float GPUMemoryDedicated { get; private set; }
        public float GPUMemoryShared { get; private set; }

        readonly PerformanceCounterCategory CounterDotNetClrMemory = new PerformanceCounterCategory(".NET CLR Memory");
        readonly PerformanceCounterCategory CounterProcess = new PerformanceCounterCategory("Process");
        readonly PerformanceCounterCategory CounterGpuProcessMemory = new PerformanceCounterCategory("GPU Process Memory");

        CounterSample CLRMemoryAllocatedBytesPerSecSample;
        CounterSample CPUMemoryPrivateSample;
        CounterSample CPUMemoryWorkingSetSample;
        CounterSample CPUMemoryWorkingSetPrivateSample;
        CounterSample CPUMemoryVirtualSample;
        CounterSample[] GPUMemoryCommittedSamples = new CounterSample[1];
        CounterSample[] GPUMemoryDedicatedSamples = new CounterSample[1];
        CounterSample[] GPUMemorySharedSamples = new CounterSample[1];

        readonly Profiler Profiler = new Profiler("Host");
        readonly ProcessState State = new ProcessState("Host");
        readonly Game Game;
        readonly Thread Thread;

        private const int SleepTime = 10000;

        public HostProcess(Game game)
        {
            Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");

            Game = game;
            Thread = new Thread(HostThread);
        }

        public void Start()
        {
            Thread.Start();
        }

        public void Stop()
        {
            State.SignalTerminate();
        }

        [ThreadName("Host")]
        void HostThread()
        {
            Profiler.SetThread();

            var memoryStatus = new MEMORYSTATUSEX { Size = 64 };
            GlobalMemoryStatusEx(memoryStatus);
            CPUMemoryVirtualLimit = Math.Min(memoryStatus.TotalVirtual, memoryStatus.TotalPhysical);

            while (true)
            {
                State.Sleep(SleepTime);
                if (State.Terminated)
                    break;
                if (!DoHost())
                    return;
            }
        }

        [CallOnThread("Host")]
        bool DoHost()
        {
            if (Debugger.IsAttached)
            {
                Host();
            }
            else
            {
                try
                {
                    Host();
                }
                catch (Exception error)
                {
                    // We ignore all errors because the data is non-critical
                    // Trace.WriteLine(error);
                    Game.ProcessReportError(error);
                }
            }
            return true;
        }

        [CallOnThread("Host")]
        void Host()
        {
            Profiler.Start();
            try
            {
                var processId = Process.GetCurrentProcess().Id;

                var dotNetClrMemory = GetInstanceSamples(CounterDotNetClrMemory, "Process ID", processId, "Allocated Bytes/sec");
                CLRMemoryAllocatedBytesPerSec = GetValue(ref CLRMemoryAllocatedBytesPerSecSample, dotNetClrMemory[0]);

                var process = GetInstanceSamples(CounterProcess, "ID Process", processId, "Private Bytes", "Working Set", "Working Set - Private", "Virtual Bytes");
                CPUMemoryPrivate = GetValue(ref CPUMemoryPrivateSample, process[0]);
                CPUMemoryWorkingSet = GetValue(ref CPUMemoryWorkingSetSample, process[1]);
                CPUMemoryWorkingSetPrivate = GetValue(ref CPUMemoryWorkingSetPrivateSample, process[2]);
                CPUMemoryVirtual = GetValue(ref CPUMemoryVirtualSample, process[3]);

                var gpuProcessMemory = GetInstancesSamples(CounterGpuProcessMemory, $"pid_{processId}_", "Total Committed", "Dedicated Usage", "Shared Usage");
                GPUMemoryCommitted = GetSumValue(ref GPUMemoryCommittedSamples, gpuProcessMemory[0]);
                GPUMemoryDedicated = GetSumValue(ref GPUMemoryDedicatedSamples, gpuProcessMemory[1]);
                GPUMemoryShared = GetSumValue(ref GPUMemorySharedSamples, gpuProcessMemory[2]);
            }
            finally
            {
                Profiler.Stop();
            }
        }

        IList<CounterSample> GetInstanceSamples(PerformanceCounterCategory category, string key, long value, params string[] counterNames)
        {
            try
            {
                var categoryData = category.ReadCategory();
                var index = categoryData[key].Values.Cast<InstanceData>().ToList().FindIndex(a => a.RawValue == value);
                return counterNames.Select(name => categoryData.Contains(name) ? categoryData[name].Values.Cast<InstanceData>().ElementAt(index).Sample : CounterSample.Empty).ToList();
            }
            catch
            {
                return counterNames.Select(name => CounterSample.Empty).ToList();
            }
        }

        IList<CounterSample[]> GetInstancesSamples(PerformanceCounterCategory category, string instancePrefix, params string[] counterNames)
        {
            try
            {
                var categoryData = category.ReadCategory();
                return counterNames.Select(name => categoryData.Contains(name) ? categoryData[name].Values.Cast<InstanceData>().Where(id => id.InstanceName.StartsWith(instancePrefix)).Select(id => id.Sample).ToArray() : new[] { CounterSample.Empty }).ToList();
            }
            catch
            {
                return counterNames.Select(name => new[] { CounterSample.Empty }).ToList();
            }
        }

        float GetValue(ref CounterSample counterSample, CounterSample nextCounterSample)
        {
            try
            {
                return CounterSample.Calculate(counterSample, nextCounterSample);
            }
            catch
            {
                return 0;
            }
            finally
            {
                counterSample = nextCounterSample;
            }
        }

        float GetSumValue(ref CounterSample[] counterSamples, CounterSample[] nextCounterSamples)
        {
            try
            {
                var counterSample = new CounterSample(counterSamples.Sum(cs => cs.RawValue), counterSamples[0].BaseValue, counterSamples[0].CounterFrequency, counterSamples[0].SystemFrequency, counterSamples[0].TimeStamp, counterSamples[0].TimeStamp100nSec, counterSamples[0].CounterType, counterSamples[0].CounterTimeStamp);
                var nextCounterSample = new CounterSample(nextCounterSamples.Sum(cs => cs.RawValue), nextCounterSamples[0].BaseValue, nextCounterSamples[0].CounterFrequency, nextCounterSamples[0].SystemFrequency, nextCounterSamples[0].TimeStamp, nextCounterSamples[0].TimeStamp100nSec, nextCounterSamples[0].CounterType, nextCounterSamples[0].CounterTimeStamp);
                return CounterSample.Calculate(counterSample, nextCounterSample);
            }
            catch
            {
                return 0;
            }
            finally
            {
                counterSamples = nextCounterSamples;
            }
        }

        #region Native code

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public class MEMORYSTATUSEX
        {
            public uint Size;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

        #endregion
    }
}
