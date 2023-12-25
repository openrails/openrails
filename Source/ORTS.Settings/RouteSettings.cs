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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LibGit2Sharp;
using static ORTS.Settings.RouteSettings;
using System.Windows.Forms;

namespace ORTS.Settings
{
    public class RouteSettings
    {
        public class Route
        {
            public string DateInstalled { get; set; }
            public string Url { get; set; }

            public Route(string dateInstalled, string url)
            { 
                DateInstalled = dateInstalled;
                Url = url;
            }
        }

        private readonly ContentSettings Content;

        public IDictionary<string, Route> Routes { get; private set; }

        public RouteSettings(ContentSettings content)
        {
            Content = content;
            Routes = new Dictionary<string, Route>();

            Load();
        }

        public void Load() 
        {
            // left empty
        }

        public void LoadContentAndInstalled()
        {
            if (!string.IsNullOrWhiteSpace(Content.RouteJsonName))
            {
                if (File.Exists(Content.RouteJsonName))
                {
                    try
                    {
                        string json = File.ReadAllText(Content.RouteJsonName);
                        Routes = JsonConvert.DeserializeObject<IDictionary<string, Route>>(json);
                    }
                    catch (Exception error)
                    {
                        throw new Exception("Error during reading " + Content.RouteJsonName + ": " + error.Message, error);
                    }
                }
            }

            string definedContentJsonName = @"d:\content\routes.json";
            string definedContentJsonDirectoryName = Path.Combine(UserSettings.UserDataFolder, "ContentJson");
            string githubUrl = "https://github.com/openrails/content.git";

            if (Environment.GetEnvironmentVariable("TstLoadContentAndInstalled") == null)
            {
                try
                {
                    // normal non test behaviour, retrieve json file from github

                    directoryDelete(definedContentJsonDirectoryName);

                    Repository.Clone(githubUrl, definedContentJsonDirectoryName);

                    definedContentJsonName = Path.Combine(definedContentJsonDirectoryName, "routes.json");
                }
                catch (Exception error) 
                { 
                    throw new Exception("Error during retrieving routes.json from \"" + githubUrl + "\":" + error.Message, error); 
                }  
            }

            if (File.Exists(definedContentJsonName))
            {
                try
                {
                    var json = File.ReadAllText(definedContentJsonName);

                    IList<JToken> results = JsonConvert.DeserializeObject<JToken>(json) as IList<JToken>;
                    foreach (JToken result in results)
                    {
                        if (result["url"].ToString().EndsWith(".git"))
                        {
                            string routeName = result["name"].ToString();
                            if (!Routes.ContainsKey(routeName))
                            {
                                Routes.Add(routeName, new RouteSettings.Route("", result["url"].ToString()));
                            }
                        }
                    }

                    directoryDelete(definedContentJsonDirectoryName);
                }
                catch (Exception error)
                {
                    throw new Exception("Error during reading \"" +  definedContentJsonName + "\": " + error.Message, error);
                }
            }

            return;
        }

        private void directoryDelete(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                directoryRemoveReadOnlyFlags(directoryName);
                Directory.Delete(directoryName, true);
            }
        }

        private void directoryRemoveReadOnlyFlags(string directoryName)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                FileInfo file = new FileInfo(filename);
                file.IsReadOnly = false;
            }
            foreach (string subDirectoryName in Directory.GetDirectories(directoryName))
            {
                directoryRemoveReadOnlyFlags(subDirectoryName);
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
