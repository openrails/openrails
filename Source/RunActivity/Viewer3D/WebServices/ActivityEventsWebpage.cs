// COPYRIGHT 2026 by the Open Rails project.
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
using System.IO;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Newtonsoft.Json;

namespace Orts.Viewer3D.WebServices
{
    public class ActivityEventsWebpage : WebSocketModule
    {
        private readonly Viewer Viewer;

        private int ConnectionCnt = 0;
        private bool InitHandled = false;

        private bool BeepToBeDone = false;
        private readonly SoundPlayer SoundPlayerBeep;

        private string HeaderPrev = "";
        private string TextPrev = "";

        public class EventsSend
        {
            [JsonProperty("Type")]
            public string Type;
            [JsonProperty("Header")]
            public string Header;
            [JsonProperty("Text")]
            public string Text;
            [JsonProperty("BeepTranslated")]
            public string BeepTranslated;
            [JsonProperty("BeepHelpTranslated")]
            public string BeepHelpTranslated;

            public EventsSend(string type, string header, string text, string beepTranslated, string beepHelpTranslated)
            {
                Type = type;
                Header = header;
                Text = text;
                BeepTranslated = beepTranslated;
                BeepHelpTranslated = beepHelpTranslated;
            }
        }

        public class EventsReceived
        {
            public EventsReceived(string type, object data)
            {
                Type = type;
                Data = data;
            }

            [JsonProperty("type")]
            public string Type { get; private set; }

            [JsonProperty("data")]
            public object Data { get; private set; }
        }

        public ActivityEventsWebpage(string url, Viewer viewer) :
            base(url, true)
        {
            Viewer = viewer;
            AddProtocol("json");

            SoundPlayerBeep = new SoundPlayer(Path.Combine(Viewer.ContentPath, "Beep.wav"));
            SoundPlayerBeep.LoadAsync();
        }

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("ActivityEventsWebpage, client connected");
            ConnectionCnt++;
            InitHandled = false;

            HeaderPrev = "";
            TextPrev = "";

            handleSendInit();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            Trace.TraceInformation("ActivityEventsWebpage, client disconnected");
            ConnectionCnt--;

            return Task.CompletedTask;
        }

        public bool isConnectionOpen => ConnectionCnt > 0;
        public bool isInitHandled => InitHandled;

        /// <inheritdoc />
        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult result)
        {
            var text = Encoding.GetString(rxBuffer);
            var eventsReceived = JsonConvert.DeserializeObject<EventsReceived>(text);

            if (eventsReceived.Type.Equals("beep"))
            {
                BeepToBeDone = (bool)eventsReceived.Data;
            }

            InitHandled = true;

            return Task.CompletedTask;
        }

        public async Task BroadcastEvent(EventsSend EventsSend)
        {
            try
            {
                string jsonSend = JsonConvert.SerializeObject(EventsSend);
                await BroadcastAsync(jsonSend).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.TraceInformation(
                    "ActivityEventsWebpage.BroadcastEvent, Json serialize or Broadcast error:");
                Trace.WriteLine(e);
            }
        }

        private void handleSend(string type, string header, string text, string beepTranslated, string beepHelpTranslated)
        {
            if (!(header.Equals(HeaderPrev) && text.Equals(TextPrev)))
            {
                HeaderPrev = header;
                TextPrev = text;

                EventsSend eventsSend =
                    new EventsSend(
                        type,
                        header,
                        text,
                        beepTranslated,
                        beepHelpTranslated);

                _ = BroadcastEvent(eventsSend);

                if (BeepToBeDone)
                {
                    SoundPlayerBeep.Play();
                }
            }
        }

        private void handleSendInit()
        {
            if (isConnectionOpen)
            {
                if (Viewer.Simulator.Activity != null &&
                    Viewer.Simulator.Activity.Tr_Activity != null &&
                    Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header != null &&
                    Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing.Length > 0)
                {
                    var header = Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header;
                    handleSend("init", 
                        Viewer.Catalog.GetString("Activity") + ": " + header.Name, 
                        header.Description + "\n\n" +
                        Viewer.Catalog.GetString("Activity Briefing") + ":\n\n" + header.Briefing,
                        Viewer.Catalog.GetString("Beep"), Viewer.Catalog.GetString("When checked 'beep' to be heard when a new Activity Event message appears"));
                }
                else
                {
                    handleSend("init", Viewer.Catalog.GetString("Open Rails not started for a specific Actvity"), "Hence this page will be left empty", "", "");
                }
            }
        }

        public void handleSendActivityEvent(string header, string text)
        {
            if (isConnectionOpen)
            {
                if (Viewer.Simulator.Activity != null &&
                    Viewer.Simulator.Activity.Tr_Activity != null &&
                    Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header != null &&
                    Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_Header.Briefing.Length > 0)
                {
                    handleSend("event", header, text, "", "");
                }
            }
        }
    }
}
