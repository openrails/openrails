using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ORTS
{
	public class LoaderProcess
	{
		public readonly bool Threaded;
		public readonly Profiler Profiler = new Profiler("Loader");
		public bool Slow;
		readonly Viewer3D Viewer;
		readonly Thread Thread;
		readonly ProcessState State;

		public LoaderProcess(Viewer3D viewer)
		{
			Threaded = true;
			Viewer = viewer;
			if (Threaded)
			{
				State = new ProcessState();
				Thread = new Thread(LoadLoop);
			}
		}

		public void Run()
		{
			if (Threaded)
				Thread.Start();
		}

		public void Stop()
		{
			if (Threaded)
				Thread.Abort();
		}

		public bool Finished
		{
			get
			{
				// Non-threaded updater is always "finished".
				return !Threaded || State.Finished;
			}
		}

		void LoadLoop()
		{
			Thread.CurrentThread.Name = "Loader Process";

			while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
			{
				// Wait for a new Update() command
				State.WaitTillStarted();

				try
				{
					Update();
				}
				catch (Exception error)
				{
					if (!(error is ThreadAbortException))
					{
						// Unblock anyone waiting for us, report error and die.
						State.SignalFinish();
						Viewer.ProcessReportError(error);
						// Finally unblock any process that may have started us, while the message was showing
						State.SignalFinish();
						return;
					}
				}

				// Signal finished so RenderProcess can start drawing
				State.SignalFinish();
			}
		}

		public const double UpdatePeriod = 0.1;       // 10 times per second 
		public double LastUpdate = 0;          // last time we were upated

		public void StartUpdate()
		{
			Slow = !Finished;
			// the loader will often fall behind, in that case let it finish
			// before issueing a new command.
			if (Slow)
				return;

			Viewer.LoadPrep();

			State.SignalStart();

			LastUpdate = Program.RealTime;
		}

		public void Update()
		{
			Profiler.Start();

			try
			{
				Viewer.Load(Viewer.RenderProcess);  // complete scan and load as necessary
			}
			finally
			{
				Profiler.Stop();
			}
		}
	} // LoaderProcess
}
