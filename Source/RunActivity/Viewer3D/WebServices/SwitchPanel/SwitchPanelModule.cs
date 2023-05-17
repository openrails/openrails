// COPYRIGHT 2023 by the Open Rails project.
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
//

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using ORTS.Common.Input;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchPanelModule : WebSocketModule
    {
        private static SwitchPanelModule SwitchPanelModuleStatic;
        private static SwitchesOnPanel SwitchesOnPanelStatic;
        private static bool InitDone = false;
        private static int Connections = 0;

        public SwitchPanelModule(string url, Viewer viewer) :
            base(url, true)
        {
            AddProtocol("json");
            SwitchPanelModuleStatic = this;
            SwitchesOnPanelStatic = new SwitchesOnPanel(viewer);
            InitDone = true;
        }

        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            var text = Encoding.GetString(rxBuffer);
            var switchPanelEvent = JsonConvert.DeserializeObject<SwitchPanelEvent>(text);
            Trace.TraceInformation("Got message of type {0} {1}", switchPanelEvent.Type, switchPanelEvent.Data);

            int value;
            switch (switchPanelEvent.Type)
            {
                case "init":
                    SendAll("init");
                    break;
                case "buttonDown":
                    value = int.Parse(switchPanelEvent.Data.ToString());
                    SwitchesOnPanelStatic.setIsDown((UserCommand)Enum.ToObject(typeof(UserCommand), value));
                    break;
                case "buttonUp":
                    value = int.Parse(switchPanelEvent.Data.ToString());
                    SwitchesOnPanelStatic.setIsUp((UserCommand)Enum.ToObject(typeof(UserCommand), value));
                    break;
            }
            return Task.CompletedTask;
        }

        public static bool IsDown(UserCommand userCommand)
        {
            if ((Connections > 0) && InitDone)
                return SwitchesOnPanelStatic.IsDown(userCommand);
            else
                return false;
        }

        public static bool IsUp(UserCommand userCommand)
        {
            if ((Connections > 0) && InitDone)
                return SwitchesOnPanelStatic.IsUp(userCommand);
            else
                return false;
        }

        public static void SendSwitchPanelIfChanged()
        {
            if ((Connections > 0) && InitDone)
            {
                if (SwitchesOnPanelStatic.IsChanged())
                {
                    SwitchPanelModuleStatic.SendAll("update");
                }
            }
        }

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("client connected");
            Connections++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("client disconnected");
            Connections--;
            return Task.CompletedTask;
        }

        private void SendAll(string type)
        {
            _ = BroadcastEvent(new SwitchPanelEvent(type, SwitchesOnPanelStatic.SwitchesOnPanelArray));
        }

        public async Task BroadcastEvent(SwitchPanelEvent jsEvent)
        {
            try
            {
                var json = JsonConvert.SerializeObject(jsEvent);
                await BroadcastAsync(json).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                string error = e.ToString();
                Console.WriteLine("Json serialize error:" + error);
                throw;
            }
        }
    }

}
