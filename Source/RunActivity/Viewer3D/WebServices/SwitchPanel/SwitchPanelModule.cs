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
        private static SwitchPanelModule SwitchPanelModuleLocal;
        private static bool InitDone = false;
        private static Viewer Viewer;
        private static int Connections = 0;

        public SwitchPanelModule(string url, Viewer viewer) :
            base(url, true)
        {
            AddProtocol("json");
            SwitchPanelModuleLocal = this;
            _ = new SwitchesOnPanel(viewer);
            Viewer = viewer;
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
                case "buttonClick":
                    value = int.Parse(switchPanelEvent.Data.ToString());
                    SwitchesOnPanel.setIsPressed((UserCommand)Enum.ToObject(typeof(UserCommand), value));
                    break;
                case "buttonDown":
                    value = int.Parse(switchPanelEvent.Data.ToString());
                    SwitchesOnPanel.setIsDown((UserCommand)Enum.ToObject(typeof(UserCommand), value));
                    break;
                case "buttonUp":
                    value = int.Parse(switchPanelEvent.Data.ToString());
                    SwitchesOnPanel.setIsUp((UserCommand)Enum.ToObject(typeof(UserCommand), value));
                    break;
            }
            return Task.CompletedTask;
        }

        private static void IsPressedDisplayTrackMonitorWindow()
        {
            // handle display track monitor from switch panel:
            // cycle between: off --> static only --> all --> off

            if (!Viewer.TrackMonitorWindow.Visible)
            {
                // track monitor window not visible
                Viewer.TrackMonitorWindow.Visible = true;
                if (Viewer.TrackMonitorWindow.Monitor.Mode == Popups.TrackMonitor.DisplayMode.All)
                {
                    Viewer.TrackMonitorWindow.Monitor.CycleMode();
                }
            }
            else
            {
                // visible
                if (Viewer.TrackMonitorWindow.Monitor.Mode == Popups.TrackMonitor.DisplayMode.StaticOnly)
                {
                    Viewer.TrackMonitorWindow.Monitor.CycleMode();
                }
                else
                {
                    Viewer.TrackMonitorWindow.Visible = false;
                }
            }
        }

        private static void IsPressedDisplayTrainDrivingWindow()
        {
            // handle display train driving info from switch panel:
            // cycle between: off --> normal text mode --> abreviated --> off

            if (!Viewer.TrainDrivingWindow.Visible)
            {
                // train driving info window not visible
                Viewer.TrainDrivingWindow.Visible = true;
                if (!Viewer.TrainDrivingWindow.normalTextMode)
                {
                    Viewer.TrainDrivingWindow.CycleMode();
                }
            }
            else
            {
                // visible
                if (Viewer.TrainDrivingWindow.normalTextMode)
                {
                    Viewer.TrainDrivingWindow.CycleMode();
                }
                else
                {
                    Viewer.TrainDrivingWindow.Visible = false;
                }
            }
        }

        private static void IsPressedDisplayHUD()
        {
            if (!Viewer.HUDWindow.Visible)
            {
                // HUD not visible
                Viewer.HUDWindow.TextPage = 0;
                Viewer.HUDWindow.Visible = true;
            }
            else
            {
                // visible
                if (Viewer.HUDWindow.TextPage < Viewer.HUDWindow.TextPagesLength - 1)
                {
                    // next HUD page
                    Viewer.HUDWindow.TabAction();

                }
                else
                {
                    Viewer.HUDWindow.Visible = false;
                    Viewer.HUDWindow.TextPage = 0;
                }
            }
        }

        public static bool IsPressed(UserCommand userCommand)
        {
            bool toBeReturned = false;

            if ((Connections > 0) && InitDone)
            {
                if (SwitchesOnPanel.IsPressed(userCommand))
                {
                    switch (userCommand)
                    {
                        case UserCommand.DisplayTrackMonitorWindow:
                            IsPressedDisplayTrackMonitorWindow();
                            break;
                        case UserCommand.DisplayTrainDrivingWindow:
                            IsPressedDisplayTrainDrivingWindow();
                            break;
                        case UserCommand.DisplayHUD:
                            IsPressedDisplayHUD();
                            break;
                        default:
                            toBeReturned = true;
                            break;
                    }
                }

            }
            return toBeReturned;
        }

        public static bool IsDown(UserCommand userCommand)
        {
            if ((Connections > 0) && InitDone)
            {
                if (SwitchesOnPanel.IsDown(userCommand))
                    return true;
            }
            return false;
        }

        public static bool IsUp(UserCommand userCommand)
        {
            if ((Connections > 0) && InitDone)
            {
                if (SwitchesOnPanel.IsUp(userCommand))
                    return true;
            }
            return false;
        }

        public static void SendSwitchPanelIfChanged()
        {
            if ((Connections > 0) && InitDone)
            {
                int rows = SwitchesOnPanel.GetSwitchesOnPanelArray().GetLength(0);
                int cols = SwitchesOnPanel.GetSwitchesOnPanelArray().GetLength(1);
                bool changed = false;

                SwitchesOnPanel.setDefinitions(SwitchesOnPanel.GetSwitchesOnPanelArray());

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (!SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Definition.Equals(SwitchesOnPanel.getPreviousSwitchesOnPanelArray()[i, j].Definition))
                        {
                            SwitchOnPanelDefinition.deepCopy(
                                SwitchesOnPanel.getPreviousSwitchesOnPanelArray()[i, j].Definition,
                                SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Definition);
                            changed = true;
                        }

                        SwitchesOnPanel.getStatus(SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Definition.UserCommand[0],
                            ref SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Status);
                        if (!SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Status.Equals(SwitchesOnPanel.getPreviousSwitchesOnPanelArray()[i, j].Status))
                        {
                            SwitchOnPanelStatus.deepCopy(
                                SwitchesOnPanel.getPreviousSwitchesOnPanelArray()[i, j].Status,
                                SwitchesOnPanel.GetSwitchesOnPanelArray()[i, j].Status);
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    SwitchPanelModuleLocal.SendAll("buttonClick");
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
            _ = BroadcastEvent(new SwitchPanelEvent(type, SwitchesOnPanel.GetSwitchesOnPanelArray()));
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
