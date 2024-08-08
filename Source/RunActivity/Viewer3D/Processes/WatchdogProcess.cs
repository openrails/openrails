// COPYRIGHT 2014 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Orts.Common;
using Orts.Processes;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;

namespace Orts.Viewer3D.Processes
{
    /// <summary>
    /// A process which monitors other threads to check they're still running normally and reports errors if they're not.
    /// </summary>
    public class WatchdogProcess
    {
        readonly Profiler Profiler = new Profiler("Watchdog");
        readonly ProcessState State = new ProcessState("Watchdog");
        readonly Game Game;
        readonly Thread Thread;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        List<WatchdogToken> Tokens = new List<WatchdogToken>();

        public WatchdogProcess(Game game)
        {
            Game = game;
            Thread = new Thread(WatchdogThread);
        }

        public void Start()
        {
            Thread.Start();
        }

        public void Stop()
        {
            State.SignalTerminate();
        }

        /// <summary>
        /// Registers a new thread token for monitoring by the watchdog.
        /// </summary>
        /// <param name="token">The token representing the thread to start monitoring.</param>
        public void Register(WatchdogToken token)
        {
            // We must do this elaborate routine because:
            //   a) We cannot modify Tokens in-place (due to potentially being read from other threads at the same time)
            //   b) We cannot just assign directly (due to potentially being written from other threads at the same time)
            while (true)
            {
                var tokens = Tokens;
                var newTokens = new List<WatchdogToken>(tokens);
                newTokens.Add(token);
                if (tokens == Interlocked.CompareExchange(ref Tokens, newTokens, tokens))
                    break;
            }
        }

        /// <summary>
        /// Unregisters a thread token from monitoring by the watchdog.
        /// </summary>
        /// <param name="token">The token representing the thread to stop monitoring.</param>
        public void Unregister(WatchdogToken token)
        {
            // We must do this elaborate routine because:
            //   a) We cannot modify Tokens in-place (due to potentially being read from other threads at the same time)
            //   b) We cannot just assign directly (due to potentially being written from other threads at the same time)
            while (true)
            {
                var tokens = Tokens;
                var newTokens = new List<WatchdogToken>(tokens);
                newTokens.Remove(token);
                if (tokens == Interlocked.CompareExchange(ref Tokens, newTokens, tokens))
                    break;
            }
        }

        [ThreadName("Watchdog")]
        void WatchdogThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();

            while (true)
            {
                Thread.Sleep(1000);
                if (State.Terminated)
                    break;

                var tokens = Tokens;

                // Step each token first (which checks the state and captures stacks).
                foreach (var token in tokens)
                    token.Step();

                // Now see if any are waiting and any have hung.
                var waitTokens = new List<WatchdogToken>();
                var hungTokens = new List<WatchdogToken>();
                foreach (var token in tokens)
                    if (token.IsWaiting)
                        waitTokens.Add(token);
                    else if (!token.IsResponding)
                        hungTokens.Add(token);

                if (hungTokens.Count > 0)
                {
                    // Report every hung thread as a fatal error.
                    foreach (var token in hungTokens)
                        Trace.WriteLine(new FatalException(new ThreadHangException(token.Thread, token.Stacks)));

                    // Report every waiting thread as a warning (it might be relevant).
                    foreach (var token in waitTokens)
                        Trace.WriteLine(new ThreadWaitException(token.Thread, token.Stacks));

                    // Abandon ship!
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    else
                        Environment.Exit(1);
                }
            }
        }
    }

    /// <summary>
    /// A token which represents a single thread which can be monitored by the <see cref="WatchdogProcess"/> for responsiveness.
    /// </summary>
    [DebuggerDisplay("WatchdogToken({Thread.Name})")]
    public class WatchdogToken
    {
        const int MinimumLaxnessS = 5;
        const int MaximumLaxnessS = 10;

        internal readonly Thread Thread;

        /// <summary>
        /// Gets and sets a multiplier for how long a thread must stop responding before it is considered to have hung.
        /// </summary>
        public int SpecialDispensationFactor { get; set; }

        /// <summary>
        /// Returns the number of <see cref="Step"/> (scaled by <see cref="SpecialDispensationFactor"/>) since the thread last called <see cref="Ping"/>.
        /// </summary>
        internal int Counter { get { return _counter / SpecialDispensationFactor; } }

        /// <summary>
        /// Returns whether the thread this <see cref="WatchdogToken"/> represents is considered to be responding.
        /// </summary>
        internal bool IsResponding
        {
            get
            {
                return Counter < MaximumLaxnessS;
            }
        }

        /// <summary>
        /// Returns whether the thread this <see cref="WatchdogToken"/> represents is considered to be waiting (a subset of not responding).
        /// </summary>
        internal bool IsWaiting
        {
            get
            {
                return !IsResponding && StacksAreWaits();
            }
        }

        /// <summary>
        /// Returns the list of <see cref="StackTrace"/> that have been collected so far.
        /// </summary>
        internal List<StackTrace> Stacks { get; private set; }

        // THREAD SAFETY:
        //   This field is modified on multiple threads; all such writes must be performed in a thread-safe,
        //   lock-free way, e.g. with the Interlocked class.
        int _counter;

        /// <summary>
        /// Creates a new token for watching when a <see cref="Thread"/> for not responding.
        /// </summary>
        /// <param name="thread"></param>
        public WatchdogToken(Thread thread)
        {
            Thread = thread;
            SpecialDispensationFactor = 1;
            Stacks = new List<StackTrace>();
        }

        /// <summary>
        /// Calling this identifies that the thread is still making progress by resetting the <see cref="Counter"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method should be called (directly or indirectly) whenever a thread makes significant progress, such as
        /// when it is about to start processing the next file during loading. It must never be called inside a loop
        /// which is not guaranteed to exit (such as a while-true loop). It may be called within a loop that has fixed,
        /// known bounds (such as a typical for loop).
        /// </para>
        /// <para>
        /// The requirements on when this should be or not be called are important: failure to call this (directly or
        /// indirectly) during a long but guaranteed-to-terminate loop will cause unnecessary hang reports, and
        /// calling this during a potentially infinite loop will result in hangs that are not reported.
        /// </para>
        /// <para>
        /// When it doubt, and for processes which are expected to take considerable time, the
        /// <see cref="SpecialDispensationFactor"/> may be used to temporarily grant the thread more time to work
        /// before considering it to have stopped responding.
        /// </para>
        /// <example>
        /// This examples shows how to temporarily grant some long-running code extra time to complete without giving
        /// up on the ability to detect hangs.
        /// <code>
        /// void Test(WatchdogToken token) {
        ///     // Increase available working time.
        ///     token.SpecialDispensationFactor *= 10;
        ///     
        ///     // ... long-running process here ...
        ///     
        ///     // Reset available working time.
        ///     token.SpecialDispensationFactor /= 10;
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public void Ping()
        {
            Interlocked.Exchange(ref _counter, 0);
        }

        internal void Step()
        {
            Interlocked.Increment(ref _counter);

            // If we're past the minimum threshold for being interested, collect a stack trace. Otherwise, reset
            // the stacks (since the thread has recovered now).
            if (Counter > MinimumLaxnessS)
            {
                // We don't especially care if this fails, which seems to happen occasionally.
                try
                {
                    Stacks.Add(GetStackTrace());
                }
                catch (ThreadStateException)
                {
                }
            }
            else
            {
                Stacks.Clear();
            }
        }

        StackTrace GetStackTrace()
        {
#if NET5_0_OR_GREATER
            // TODO: https://github.com/microsoft/clrmd is likely the best option to reimplement this in .NET 5+
            throw new ThreadStateException("This is not implemented");
#else
            // Yes, I know this is deprecated. Sorry. This code needs to collect the stack trace from a *different*
            // thread and this seems to be the only option - three seperate, deprecated APIs. :(
#pragma warning disable 0618
            Thread.Suspend();
            try
            {
                return new StackTrace(Thread, true);
            }
            finally
            {
                Thread.Resume();
            }
#pragma warning restore 0618
#endif
        }

        bool StacksAreWaits()
        {
            foreach (var stack in Stacks)
            {
                if (stack.FrameCount < 1)
                    continue;
                var frame = stack.GetFrame(0);
                if (frame == null)
                    continue;
                var method = frame.GetMethod();
                if (method == null)
                    continue;
                if (method.Name.StartsWith("Wait"))
                    return true;
            }
            return false;
        }
    }

    class ThreadWatchdogException : Exception
    {
        public override string ToString()
        {
            return $"{GetType()}: {Message}{Environment.NewLine}{StackTrace}";
        }

        public override string StackTrace
        {
            get
            {
                return _stackTrace;
            }
        }

        string _stackTrace;

        internal ThreadWatchdogException(string message, List<StackTrace> stacks)
            : base(message)
        {
            // Figure out the common base of the stacks.
            var maximumDepth = stacks.Max(stack => stack.FrameCount);
            var commonStack = new List<string>(maximumDepth);
            for (var i = 0; i < maximumDepth; i++)
            {
                var frame = FormatStackFrame(stacks[0].GetFrame(stacks[0].FrameCount - i - 1));
                if (!stacks.All(stack => FormatStackFrame(stack.GetFrame(stack.FrameCount - i - 1)) == frame))
                    break;
                commonStack.Insert(0, frame);
            }
            _stackTrace = String.Join("", commonStack.ToArray());
        }

        /// <summary>
        /// Utility function which formats a single <see cref="StackFrame"/> in to a string as similarly as possible to <see cref="Exception"/>'s format.
        /// </summary>
        /// <param name="frame">The <see cref="StackFrame"/> to format in to a string.</param>
        /// <returns>The string containing the <see cref="StackFrame"/>'s function, argument names and (if available) source location.</returns>
        static string FormatStackFrame(StackFrame frame)
        {
            // Note: This code is meant to return the same string as Exception's formatting of a stack trace. Do not modify.
            var sb = new StringBuilder(255);
            var method = frame.GetMethod();
            if (method != null)
            {
                sb.Append("   at ");

                {
                    var type = method.DeclaringType;
                    if (type != null)
                    {
                        sb.Append(type.FullName.Replace('+', '.'));
                        sb.Append(".");
                    }
                }

                sb.Append(method.Name);

                if (method.IsGenericMethod)
                {
                    sb.Append("[");
                    var genericTypes = method.GetGenericArguments();
                    var first = true;
                    foreach (var genericType in genericTypes)
                    {
                        if (!first)
                            sb.Append(",");
                        sb.Append(genericType.Name);
                        first = false;
                    }
                    sb.Append("]");
                }

                {
                    sb.Append("(");
                    var parameters = method.GetParameters();
                    var first = true;
                    foreach (var parameter in parameters)
                    {
                        if (!first)
                            sb.Append(", ");
                        if (parameter.ParameterType == null)
                            sb.Append("<UnknownType>");
                        else
                            sb.Append(parameter.ParameterType.Name);
                        sb.Append(" ");
                        sb.Append(parameter.Name);
                        first = false;
                    }
                    sb.Append(")");
                }

                if (frame.GetILOffset() != -1)
                {
                    try
                    {
                        var fileName = frame.GetFileName();
                        if (fileName != null)
                        {
                            sb.Append(" in ");
                            sb.Append(fileName);
                            sb.Append(":line ");
                            sb.Append(frame.GetFileLineNumber());
                        }
                    }
                    catch (SecurityException)
                    {
                    }
                }

                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }

    class ThreadHangException : ThreadWatchdogException
    {
        public ThreadHangException(Thread thread, List<StackTrace> stacks)
            : base(String.Format("Thread '{0}' has hung; the consistent stack trace is shown below:", thread.Name), stacks)
        {
        }
    }

    class ThreadWaitException : ThreadWatchdogException
    {
        public ThreadWaitException(Thread thread, List<StackTrace> stacks)
            : base(String.Format("Thread '{0}' is waiting; the consistent stack trace is shown below:", thread.Name), stacks)
        {
        }
    }
}
