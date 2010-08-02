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
			var running = TimeRunning.IsRunning;
			TimeTotal.Stop();
			TimeRunning.Stop();
			var rate = 1000d / TimeTotal.ElapsedMilliseconds;
			Wall = (Wall * (rate - 1) + 100d * (double)TimeRunning.ElapsedMilliseconds / (double)TimeTotal.ElapsedMilliseconds) / rate;
			CPU = (CPU * (rate - 1) + 100d * (double)TimeCPU.TotalMilliseconds / (double)TimeTotal.ElapsedMilliseconds) / rate;
			TimeTotal.Reset();
			TimeRunning.Reset();
			TimeCPU = TimeSpan.Zero;
			TimeTotal.Start();
			if (running) TimeRunning.Start();
			LastCPU = ProcessThread.TotalProcessorTime;
		}
	}
}
