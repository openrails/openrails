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
//      Based on an idea by Dan Reynolds (HighAspect) - 2017-12-21
// ===========================================================================================

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using Orts.Simulation.Physics;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orts.Viewer3D.WebServices
{
    public static class WebServer
    {
        public static EmbedIO.WebServer CreateWebServer(string url, string path) => CreateWebServer(new string[] { url }, path);

        public static EmbedIO.WebServer CreateWebServer(string[] urls, string path)
        {
            // Viewer is not yet initialized in the GameState object - wait until it is
            while (Program.Viewer == null)
                Thread.Sleep(1000);

            return new EmbedIO.WebServer(o => o
                    .WithUrlPrefixes(urls))
                .WithWebApi("/API", SerializationCallback, m => m
                    .WithController(() => new ORTSApiController(Program.Viewer)))
                .WithStaticFolder("/", path, true);
        }

        // We need to use the Newtonsoft serializer instead of the Swan one that EmbedIO defaults to -
        // for some reason, Swan won't serialize custom classes.
        private static async Task SerializationCallback(IHttpContext context, object data)
        {
            using (var text = context.OpenResponseText(new UTF8Encoding()))
                await text.WriteAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }

    internal class ORTSApiController : WebApiController
    { 
        private readonly Viewer Viewer;

        public ORTSApiController(Viewer viewer)
        {
            Viewer = viewer;
        }


        #region /API/APISAMPLE
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

        // Call from JavaScript is case-sensitive, with /API prefix, e.g:
        //   hr.open("GET", "/API/APISAMPLE", true);
        [Route(HttpVerbs.Get, "/APISAMPLE")]
        public ApiSampleData ApiSample()
        {
            var sampleData = new ApiSampleData();

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

            return sampleData;
        }
        #endregion


        #region /API/HUD
        public class HudApiTable
        {
            public int nRows;
            public int nCols;
            public string[] values;
        }

        public class HudApiArray
        {
            public int nTables;
            public HudApiTable commonTable;
            public HudApiTable extraTable;
        }

        [Route(HttpVerbs.Get, "/HUD/{pageNo}")]
        // Example URL where pageNo = 3: 
        //   "http://localhost:2150/API/HUD/3" returns data in JSON
        // Call from JavaScript is case-sensitive, with /API prefix, e.g:
        //   hr.open("GET", "/API/HUD" + pageNo, true);
        // The name of this method is not significant.
        public HudApiArray ApiHUD(int pageNo)
        {
            var hudApiArray = new HudApiArray();
            hudApiArray.nTables = 1;

            hudApiArray.commonTable = ApiHUD_ProcessTable(0);
            if (pageNo > 0)
            {
                hudApiArray.nTables = 2;
                hudApiArray.extraTable = ApiHUD_ProcessTable(pageNo);
            }
            return hudApiArray;
        }

        public HudApiTable ApiHUD_ProcessTable(int pageNo)
        {
            Popups.HUDWindow.TableData hudTable = Viewer.HUDWindow.PrepareTable(pageNo);

            var apiTable = new HudApiTable();

            apiTable.nRows = hudTable.Cells.GetLength(0);
            var nRows = apiTable.nRows;
            apiTable.nCols = hudTable.Cells.GetLength(1);
            var nCols = apiTable.nCols;
            apiTable.values = new string[nRows * nCols];

            int nextCell = 0;
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    apiTable.values[nextCell++] = hudTable.Cells[i, j];

            return (apiTable);
        }
        #endregion


        #region /API/TRACKMONITOR
        [Route(HttpVerbs.Get, "/TRACKMONITOR")]
        public Train.TrainInfo TrackMonitor()
        {
            return Viewer.PlayerTrain.GetTrainInfo();
        }
        #endregion
    }
}