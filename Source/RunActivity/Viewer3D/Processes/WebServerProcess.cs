// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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


using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Orts.Viewer3D;
using Orts.Viewer3D.WebServices;
using ORTS.Common;
using ORTS.Settings;
using Orts.Processes;

namespace Orts.Viewer3D.Processes
{
    public class WebServerProcess
    {
        public readonly Profiler Profiler = new Profiler("WebServer");
        readonly ProcessState State = new ProcessState("WebServer");
        readonly Game Game;
        readonly Thread Thread;
        // readonly WatchdogToken WatchdogToken;
        // readonly CancellationTokenSource CancellationTokenSource;

        WebServer webServer;

        public WebServerProcess(Game game)
        {
            Game = game;
            Thread = new Thread(WebServerThread);
            //    WatchdogToken = new WatchdogToken(Thread);
            //    WatchdogToken.SpecialDispensationFactor = 6;    // ???
            //    CancellationTokenSource = new CancellationTokenSource(WatchdogToken.Ping);
        }

        public void Start()
        {
            Thread.Start();
        }

        public void Stop()
        {
            //public Socket ServerSocket = null;
            //Socket ServerSocket.Stop();
            webServer.stop();

            // Game.WatchdogProcess.Unregister(WatchdogToken);
            // CancellationTokenSource.Cancel();
            State.SignalTerminate();
            Thread.Abort();
        }

        public bool Finished
        {
            get
            {
                return State.Finished;
            }
        }

        /// <summary>
        /// Returns a token (copyable object) which can be queried for the cancellation (termination) of the loader.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All loading code should periodically (e.g. between loading each file) check the token and exit as soon
        /// as it is cancelled (<see cref="CancellationToken.IsCancellationRequested"/>).
        /// </para>
        /// <para>
        /// Reading <see cref="CancellationToken.IsCancellationRequested"/> causes the <see cref="WatchdogToken"/> to
        /// be pinged, informing the <see cref="WatchdogProcess"/> that the loader is still responsive. Therefore the
        /// remarks about the <see cref="WatchdogToken.Ping()"/> method apply to the token regarding when it should
        /// and should not be used.
        /// </para>
        /// </remarks>
        //public CancellationToken CancellationToken
        //{
        //    get
        //    {
        //        return CancellationTokenSource.Token;
        //    }
        //}

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        [ThreadName("WebServer")]
        void WebServerThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();

            // //////////////////////////////////////////////////////////////////


            var myWebContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath),
                "Content\\Web");

            // 127.0.0.1 is a dummy, IPAddress.Any in WebServer.cs to accept any address
            // on the local Lan
            webServer = new WebServer("127.0.0.1", 2150, 1, myWebContentPath);

            webServer.Run();
        }
    }
}
