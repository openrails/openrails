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


using EmbedIO.Net;
using System.Threading;
using Orts.Viewer3D.WebServices;
using ORTS.Common;
using Orts.Processes;
using System.IO;
using System.Windows.Forms;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace Orts.Viewer3D.Processes
{
    public class WebServerProcess
    {
        public readonly Profiler Profiler = new Profiler("WebServer");
        private readonly ProcessState State = new ProcessState("WebServer");
        private readonly Game Game;
        private readonly Thread Thread;
        private readonly CancellationTokenSource StopServer = new CancellationTokenSource();

        public WebServerProcess(Game game)
        {
            Game = game;
            Thread = new Thread(WebServerThread);
        }

        public void Start()
        {
            State.SignalStart();
            Thread.Start();
        }

        public void Stop()
        {
            StopServer.Cancel();
            State.SignalTerminate();
        }

        [ThreadName("WebServer")]
        void WebServerThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();
            if (!Game.Settings.WebServer)
                return;

            string myWebContentPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Content\\Web");
            EndPointManager.UseIpv6 = true;
            using (EmbedIO.WebServer server = WebServer.CreateWebServer($"http://*:{Game.Settings.WebServerPort}", myWebContentPath))
                server.RunAsync(StopServer.Token).Wait();
        }
    }
}
