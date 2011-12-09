// COPYRIGHT 2009, 2010 by the Open Rails project.
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
using System.Diagnostics;
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
		readonly ProcessThread ProcessThread;

		public Profiler(string name)
		{
			Name = name;
			Wall = new SmoothedData();
			CPU = new SmoothedData();
			Wait = new SmoothedData();
			TimeTotal = new Stopwatch();
			TimeRunning = new Stopwatch();
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
			TimeTotal.Start();
		}

		public void Start()
		{
			TimeRunning.Start();
			LastCPU = ProcessThread.TotalProcessorTime;
		}

		public void Stop()
		{
			TimeRunning.Stop();
			TimeCPU += ProcessThread.TotalProcessorTime - LastCPU;
		}

		public void Mark()
		{
			// Stop timers.
			var running = TimeRunning.IsRunning;
			TimeTotal.Stop();
			TimeRunning.Stop();
			// Calculate the Wall and CPU times from timers.
			Wall.Update(TimeTotal.ElapsedMilliseconds / 1000f, 100f * (float)TimeRunning.ElapsedMilliseconds / (float)TimeTotal.ElapsedMilliseconds);
			CPU.Update(TimeTotal.ElapsedMilliseconds / 1000f, 100f * (float)TimeCPU.TotalMilliseconds / (float)TimeTotal.ElapsedMilliseconds);
			Wait.Update(TimeTotal.ElapsedMilliseconds / 1000f, Math.Max(0, Wall.Value - CPU.Value));
			// Resume timers.
			TimeTotal.Reset();
			TimeRunning.Reset();
			TimeCPU = TimeSpan.Zero;
			TimeTotal.Start();
			if (running) TimeRunning.Start();
			LastCPU = ProcessThread.TotalProcessorTime;
		}
	}
}
