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
        private bool ThreadActive = false;
        WebServer webServer;

        public WebServerProcess(Game game)
        {
                Game = game;

                Thread = new Thread(WebServerThread);
                if (game.Settings.WebServer)
                {
                    ThreadActive = true;
                }
        }

        public void Start()
        {
            if (ThreadActive)
            {
                Thread.Start();
            }
        }

        public void Stop()
        {
            if (ThreadActive)
            {
            webServer.stop();
                State.SignalTerminate();
                Thread.Abort();
            }
        }
        public bool Finished
        {
            get
            {
                return State.Finished;
            }
        }

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        [ThreadName("WebServer")]
        void WebServerThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();
            int port = Game.Settings.WebServerPort;

            var myWebContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                System.Windows.Forms.Application.ExecutablePath),"Content\\Web");

            // 127.0.0.1 is a dummy, IPAddress.Any in WebServer.cs to accept any address
            // on the local Lan
            webServer = new WebServer("127.0.0.1", port, 1, myWebContentPath);
            webServer.Run();
        }
    }
}
