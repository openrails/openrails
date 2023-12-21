// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015, 2023 by the Open Rails project.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ORTS.Settings
{
    public class RouteSettings
    {
        public class Route
        {
            public string DateInstalled { get; set; }
            public string Url { get; set; }
            public string SubDirectory { get; set; }

            public Route(string dateInstalled, string url, string subDirectory)
            { 
                DateInstalled = dateInstalled;
                Url = url;
                SubDirectory = subDirectory;
            }
        }

        private ContentSettings Content;

        public IDictionary<string, Route> Routes = new Dictionary<string, Route>();

        public RouteSettings(ContentSettings content)
        {
            Content = content;
            Load();
        }

        public void Load() 
        {
            if (!string.IsNullOrWhiteSpace(Content.RouteJsonName))
            {
                if (File.Exists(Content.RouteJsonName))
                {
                    string json = File.ReadAllText(Content.RouteJsonName);
                    Routes = JsonConvert.DeserializeObject<IDictionary<string, Route>>(json);
                }
            }
        }

        public void Save()
        {
            IDictionary<string, Route> routes = new Dictionary<string, Route>();

            for (int index = 0; index < Routes.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(Routes.ElementAt(index).Value.DateInstalled))
                {
                    routes.Add(Routes.ElementAt(index));
                }
            }
            string json = JsonConvert.SerializeObject(routes, Formatting.Indented);
            File.WriteAllText(Content.RouteJsonName, json);
        }
    }
}
