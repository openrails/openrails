using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace ORTS
{
    class Profiler
    {
        public static List<Profiler> AllProfilers = new List<Profiler>();

        string name;
        public double elapsedTime;
        Stopwatch stopwatch;

        public Profiler(string name)
        {
            this.name = name;
            AllProfilers.Add(this);
        }

        public void Start()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            elapsedTime += stopwatch.Elapsed.TotalSeconds;
            elapsedTime -= 0.00035;
        }

        public void Print(double totalTime)
        {
            Trace.WriteLine(string.Format("{0}: {1:F2}%", name, elapsedTime * 100 / totalTime));
            elapsedTime = 0;
        }
    }

    struct ProfileMarker : IDisposable
    {
        public ProfileMarker(Profiler profiler)
        {
            this.profiler = profiler;
            profiler.Start();
        }

        public void Dispose()
        {
            profiler.Stop();
        }

        Profiler profiler;
    }
}
