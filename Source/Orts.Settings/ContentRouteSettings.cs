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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LibGit2Sharp;

namespace ORTS.Settings
{
    public class ContentRouteSettings
    {
        public enum DownloadType
        {
            github,
            zip,
            none
        }

        public class Start
        {
            public string Route;
            public string Activity;
            public string Locomotive;
            public string Consist;
            public string StartingAt;
            public string HeadingTo;
            public string Time;
            public string Season;
            public string Weather;
        }

        public class Route
        {
            public bool Installed;
            public DateTime DateInstalled;
            public string DirectoryInstalledIn;
            public string ContentName;
            public string ContentDirectory;

            public string Url;
            public long DownloadSize;
            public long InstallSize;
            public string Image;
            public string Description;
            public string AuthorName;
            public string AuthorUrl;
            public string Screenshot;

            public Start Start;

            public Route(
                string url, 
                long downloadSize, long installSize, 
                string image, string description,
                string author, string authorUrl,
                string screenshot,
                Start start)
            {
                Installed = false;
                DateInstalled = DateTime.MinValue;
                DirectoryInstalledIn = "";
                ContentName = "";
                ContentDirectory = "";
                Url = url;
                DownloadSize = downloadSize;
                InstallSize = installSize;
                Image = image;
                Description = description;
                AuthorName = author;
                AuthorUrl = authorUrl;
                Screenshot = screenshot;
                Start = start;
            }

            public DownloadType getDownloadType ()
            {
                if (Url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    return DownloadType.github;
                }
                if (Url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return DownloadType.zip;
                }

                return DownloadType.none;
            }
        }

        public IDictionary<string, Route> Routes { get; private set; }

        // RouteSettings are presented to the user in the menu DownloadContent form 
        // 
        // lines are a mix from:
        // - routes already downloaded, stored in registry Computer\HKEY_CURRENT_USER\SOFTWARE\OpenRails\ORTS\ContentRoutes
        // - routes which can be downloaded, stored in GitHub "https://github.com/openrails/content.git" file routes.json
        //
        // where above the word route is mentioned, "Installation profile" is ment as can be found in OR's Main menu

        public ContentRouteSettings()
        {
            Routes = new Dictionary<string, Route>();
        }

        public void LoadContent(ref string errorMsg)
        {
            // only for debug purposes
            string definedContentJsonName = @"c:\content\routes.json";

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
                    errorMsg = error.Message;
                }  
            }

            if (File.Exists(definedContentJsonName))
            {
                try
                {
                    var json = File.ReadAllText(definedContentJsonName);

                    IList<JToken> results = JsonConvert.DeserializeObject<JToken>(json) as IList<JToken>;
                    foreach (JToken resultToken in results)
                    {
                        string routeName = resultToken["name"].ToString();
                        string url = resultToken["url"].ToString();
                        long downloadSize = convertResultToLong(resultToken, "downloadSize");
                        long installSize = convertResultToLong(resultToken, "installSize");
                        string image = convertResultToString(resultToken, "image");
                        string description = convertResultToString(resultToken, "description");
                        JToken authorToken = resultToken["author"];
                        string authorName = convertResultToString(authorToken, "name");
                        string authorUrl = convertResultToString(authorToken, "url");
                        string screenshot = convertResultToString(resultToken, "screenshot");

                        JToken startToken = resultToken["start"];
                        Start start = new Start
                        {
                            Route = convertResultToString(startToken, "route"),
                            Activity = convertResultToString(startToken,"activity"),
                            Locomotive = convertResultToString(startToken, "locomotive"),
                            Consist = convertResultToString(startToken, "consist"),
                            StartingAt = convertResultToString(startToken, "startingat"),
                            HeadingTo = convertResultToString(startToken, "headingto"),
                            Time = convertResultToString(startToken, "time"),
                            Season = convertResultToString(startToken, "season"),
                            Weather = convertResultToString(startToken, "weather")
                        };

                        if (url.EndsWith(".git") || url.EndsWith(".zip"))
                        {
                            if (!Routes.ContainsKey(routeName))
                            {
                                Routes.Add(routeName, new ContentRouteSettings.Route(
                                    url, downloadSize, installSize, image, description, authorName, authorUrl, screenshot, start));
                            }
                            else
                            {
                                update(Routes[routeName],
                                    url, downloadSize, installSize, image, description, authorName, authorUrl, screenshot, start);
                            }
                        }
                    }

                    bool doingTheSumOfTheFileBytes = false;
                    directoryDelete(definedContentJsonDirectoryName, ref doingTheSumOfTheFileBytes);
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
            string screenshot,
            Start start)
        {
            route.Url = url;
            route.DownloadSize = downloadSize;
            route.InstallSize = installSize;
            route.Image = img;
            route.Description = description;
            route.AuthorName = authorName;
            route.AuthorUrl = authorUrl;
            route.Screenshot = screenshot;
            route.Start = start;
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
            if ((result != null) &&(result[fieldName] != null))
            {
                return result[fieldName].ToString();
            }
            else
            {
                return "";
            }
        }

        public static void directoryDelete(string directoryName, ref bool doingTheSumOfTheFileBytes)
        {
            if (Directory.Exists(directoryName))
            {
                // remove the read only flags,
                // otherwise the Directory.delete does not work in case read only files exists
                directoryRemoveReadOnlyFlagsAndDeleteFile(directoryName, ref doingTheSumOfTheFileBytes);
                Directory.Delete(directoryName, true);
            }
        }

        private static void directoryRemoveReadOnlyFlagsAndDeleteFile(string directoryName, ref bool doingTheSumOfTheFileBytes)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                while (doingTheSumOfTheFileBytes)
                {
                    // stop deleting file while summing in progress,
                    // sum is for feedback to the user
                    System.Threading.Thread.Sleep(10); 
                }

                _ = new FileInfo(filename)
                {
                    IsReadOnly = false
                };
                File.Delete(filename);
            }
            foreach (string subDirectoryName in Directory.GetDirectories(directoryName))
            {
                directoryRemoveReadOnlyFlagsAndDeleteFile(subDirectoryName, ref doingTheSumOfTheFileBytes);
            }
        }
    }
}
