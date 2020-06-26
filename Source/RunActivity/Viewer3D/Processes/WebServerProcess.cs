// COPYRIGHT 2020 by the Open Rails project.
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


using System.Threading;
using Orts.Viewer3D.WebServices;
using ORTS.Common;
using Orts.Processes;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace Orts.Viewer3D.Processes
{
    public class WebServerProcess
    {
        public readonly Profiler Profiler = new Profiler("WebServer");
        readonly ProcessState State = new ProcessState("WebServer");
        readonly Game Game;
        readonly Thread Thread;
        private readonly bool active;
        private readonly CancellationTokenSource stopServer = new CancellationTokenSource();

        public WebServerProcess(Game game)
        {
            Game = game;
            Thread = new Thread(WebServerThread);
            active = game.Settings.WebServer;
        }

        public void Start()
        {
            if (active)
                Thread.Start();
        }

        public void Stop()
        {
            if (active)
            {
                stopServer.Cancel();
                State.SignalTerminate();
                Thread.Abort();
            }
        }

        public bool Finished { get => State.Finished; }

        public void WaitTillFinished() => State.WaitTillFinished();

        [ThreadName("WebServer")]
        void WebServerThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();
            int port = Game.Settings.WebServerPort;

            var myWebContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
                System.Windows.Forms.Application.ExecutablePath),"Content\\Web");

            string url(string ip) => $"http://{ip}:{port}";
            var urls = new string[] { url("[::1]"), url("127.0.0.1"), url("localhost") };
            using (var server = WebServer.CreateWebServer(urls, myWebContentPath))
                server.RunAsync(stopServer.Token).Wait();
        }
    }
}
