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
		public double Wall { get; private set; }
		public double CPU { get; private set; }
		public double Wait { get { return Wall > CPU ? Wall - CPU : 0; } }
		public double SmoothedWall { get; private set; }
		public double SmoothedCPU { get; private set; }
		public double SmoothedWait { get { return SmoothedWall > SmoothedCPU ? SmoothedWall - SmoothedCPU : 0; } }
		readonly Stopwatch TimeTotal;
		readonly Stopwatch TimeRunning;
		TimeSpan TimeCPU;
		TimeSpan LastCPU;
		readonly ProcessThread ProcessThread;

		public Profiler(string name)
		{
			Name = name;
			TimeTotal = new Stopwatch();
			TimeRunning = new Stopwatch();
			foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
			{
				if (thread.Id == AppDomain.GetCurrentThreadId())
				{
					ProcessThread = thread;
					break;
				}
			}
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
			Wall = 100d * (double)TimeRunning.ElapsedMilliseconds / (double)TimeTotal.ElapsedMilliseconds;
			CPU = 100d * (double)TimeCPU.TotalMilliseconds / (double)TimeTotal.ElapsedMilliseconds;
			var rate = 1000d / TimeTotal.ElapsedMilliseconds;
			SmoothedWall = (SmoothedWall * (rate - 1) + Wall) / rate;
			SmoothedCPU = (SmoothedCPU * (rate - 1) + CPU) / rate;
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
