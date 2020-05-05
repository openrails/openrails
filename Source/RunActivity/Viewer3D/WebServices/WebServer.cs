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
//
// ===========================================================================================
//      Open Rails Web Server
//      The following files have been modified to accomodate the WebServer
//          Game.cs
//          HUDWindow.cs
//          WebServerProcess.cs
//          search for "WebServer" to find all occurrences
//
//      djr - 20171221
// ===========================================================================================

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using Orts.Simulation.Physics;

namespace Orts.Viewer3D.WebServices
{
    public static class WebServer
    {
        public static EmbedIO.WebServer CreateWebServer(string url, string path)
        {
            // Viewer is not yet initialized in the GameState object - wait until it is
            while (Program.Viewer == null)
                Thread.Sleep(1000);

            return new EmbedIO.WebServer(o => o
                    .WithUrlPrefix(url))
                .WithWebApi("/API", SerializationCallback, m => m
                    .WithController(() => new ORTSApiController(Program.Viewer)))
                .WithStaticFolder("/", path, true);
        }

        // We need to use the Newtonsoft serializer instead of the Swan one that EmbedIO defaults to -
        // for some reason, Swan won't serialize custom classes.
        private static async Task SerializationCallback(IHttpContext context, object data)
        {
            using (var text = context.OpenResponseText(new UTF8Encoding()))
            {
                await text.WriteAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }
    }

    internal class ORTSApiController : WebApiController
    {
        private readonly Viewer viewer;

        public ORTSApiController(Viewer viewer)
        {
            this.viewer = viewer;
        }


        // =======================================================================================
        // 		API to display the HUD Windows
        // =======================================================================================
        public class HudApiTable
        {
            public int nRows;
            public int nCols;
            public string[] values;
        }

        // -------------------------------------------------------------------------------------------
        public class HudApiArray
        {
            public int nTables;
            public HudApiTable commonTable;
            public HudApiTable extraTable;
        }


        // -------------------------------------------------------------------------------------------
        [Route(HttpVerbs.Post, "/HUD")]
        public HudApiArray ApiHUD([FormField] int pageno)
        {
            HudApiArray hudApiArray = new HudApiArray();
            hudApiArray.nTables = 1;

            hudApiArray.commonTable = ApiHUD_ProcessTable(0);
            if (pageno > 0)
            {
                hudApiArray.nTables = 2;
                hudApiArray.extraTable = ApiHUD_ProcessTable(pageno);
            }
            return hudApiArray;
        }

        // -------------------------------------------------------------------------------------------
        public HudApiTable ApiHUD_ProcessTable(int pageNo)
        {
            int nRows = 0;
            int nCols = 0;
            int nextCell = 0;

            Viewer3D.Popups.HUDWindow.TableData hudTable = viewer.HUDWindow.PrepareTable(pageNo);

            HudApiTable apiTable = new HudApiTable();

            apiTable.nRows = hudTable.Cells.GetLength(0);
            nRows = apiTable.nRows;
            apiTable.nCols = hudTable.Cells.GetLength(1);
            nCols = apiTable.nCols;
            apiTable.values = new string[nRows * nCols];

            try
            {
                for (int i = 0; i < nRows; ++i)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        apiTable.values[nextCell++] = hudTable.Cells[i, j];
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            return (apiTable);
        }


        // =======================================================================================
        // 		API for Sample Data
        // =======================================================================================

        public class Embedded
        {
            public string Str;
            public int Numb;
        }
        public class ApiSampleData
        {
            public int intData;
            public string strData;
            public DateTime dateData;
            public Embedded embedded;
            public string[] strArrayData;
        }

        // -------------------------------------------------------------------------------------------
        [Route(HttpVerbs.Post, "/APISAMPLE")]
        public ApiSampleData ApiSample()
        {
            ApiSampleData sampleData = new ApiSampleData();

            sampleData.intData = 576;
            sampleData.strData = "Sample String";
            sampleData.dateData = new DateTime(2018, 1, 1);

            sampleData.embedded = new Embedded();
            sampleData.embedded.Str = "Embeddded String";
            sampleData.embedded.Numb = 123;

            sampleData.strArrayData = new string[5];

            sampleData.strArrayData[0] = "First member";
            sampleData.strArrayData[1] = "Second member";
            sampleData.strArrayData[2] = "Third Member";
            sampleData.strArrayData[3] = "Forth member";
            sampleData.strArrayData[4] = "Fifth member";

            return (sampleData);
        }


        // =======================================================================================
        // 		API for Track Monitor Data
        // =======================================================================================

        // -------------------------------------------------------------------------------------------
        [Route(HttpVerbs.Post, "/TRACKMONITOR")]
        public Train.TrainInfo ApiTrackMonitor()
        {
            Train.TrainInfo trainInfo = viewer.PlayerTrain.GetTrainInfo();

            return (trainInfo);

        }


        // =======================================================================================
        // 		API for Train Info
        // =======================================================================================

        // -------------------------------------------------------------------------------------------
        [Route(HttpVerbs.Post, "/TRAININFO")]
        public Train.TrainInfo ApiTrainInfo()
        {
            Train.TrainInfo trainInfo = viewer.PlayerTrain.GetTrainInfo();

            return (trainInfo);
        }
    }
}