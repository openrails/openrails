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
using Newtonsoft.Json.Linq;
using LibGit2Sharp;

namespace ORTS.Settings
{
    public class RouteSettings
    {
        public class Route
        {
            public string DateInstalled;

            public string DirectoryInstalledIn;
            public string Url;

            public long DownloadSize;
            public long InstallSize;

            public string Image;

            public string Description;

            public string AuthorName;
            public string AuthorUrl;

            public string Screenshot;

            public Route(string dateInstalled, string directoryInstalledIn, string url, 
                long downloadSize, long installSize, 
                string image, string description,
                string author, string authorUrl,
                string screenshot)
            { 
                DateInstalled = dateInstalled;
                DirectoryInstalledIn = directoryInstalledIn;
                Url = url;
                DownloadSize = downloadSize;
                InstallSize = installSize;
                Image = image;
                Description = description;
                AuthorName = author;
                AuthorUrl = authorUrl;
                Screenshot = screenshot;
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

            // only for debug purposes
            string definedContentJsonName = @"d:\content\routes.json";

            string definedContentJsonDirectoryName = Path.GetTempFileName();
            File.Delete(definedContentJsonDirectoryName);

            string githubUrl = "https://github.com/openrails/content.git";;

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TstLoadContentAndInstalled")) ||
                (Environment.GetEnvironmentVariable("TstLoadContentAndInstalled") != "1"))
            {
                try
                {
                    // normal non test behaviour, retrieve json file from github

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
                        string routeName = result["name"].ToString();
                        string url = result["url"].ToString();
                        long downloadSize = convertResultToLong(result, "downloadSize");
                        long installSize = convertResultToLong(result, "installSize");
                        string image = convertResultToString(result, "image");
                        string description = convertResultToString(result, "description");
                        JToken author = result["author"];
                        string authorName = convertResultToString(author, "name");
                        string authorUrl = convertResultToString(author, "url");
                        string screenshot = convertResultToString(result, "screenshot");

                        if (url.EndsWith(".git") || url.EndsWith(".zip"))
                        {
                            if (!Routes.ContainsKey(routeName))
                            {
                                Routes.Add(routeName, new RouteSettings.Route(
                                    "", "", url, downloadSize, installSize, image, description, authorName, authorUrl, screenshot));
                            }
                            else
                            {
                                update(Routes[routeName],
                                    url, downloadSize, installSize, image, description, authorName, authorUrl, screenshot);
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

        private void update(Route route,
            string url, 
            long downloadSize, long installSize, 
            string img, string description, 
            string authorName, string authorUrl, 
            string screenshot)
        {
            route.Url = url;
            route.DownloadSize = downloadSize;
            route.InstallSize = installSize;
            route.Image = img;
            route.Description = description;
            route.AuthorName = authorName;
            route.AuthorUrl = authorUrl;
            route.Screenshot = screenshot;
        } 

        private long convertResultToLong(JToken result, string fieldName)
        {
            if (result[fieldName] != null)
            {
                return (long)Convert.ToDouble(result[fieldName].ToString());
            }
            else
            {
                return 0;
            }
        }

        private string convertResultToString(JToken result, string fieldName)
        {
            if (result[fieldName] != null)
            {
                return result[fieldName].ToString();
            }
            else
            {
                return "";
            }
        }

        private void directoryDelete(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                // remove the read only flags, otherwise the Directory.delete does not work
                directoryRemoveReadOnlyFlags(directoryName);
                Directory.Delete(directoryName, true);
            }
        }

        private void directoryRemoveReadOnlyFlags(string directoryName)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                _ = new FileInfo(filename)
                {
                    IsReadOnly = false
                };
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
                // only save the installed routes
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
