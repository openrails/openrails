// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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

using System.Collections.Generic;
using System.IO;
using GNU.Gettext;
using MSTS;
using Orts.Formats.Msts;
using ORTS.Common;

namespace ORTS.Menu
{
    public class Route
    {
        public readonly string Name;
        public readonly string RouteID;
        public readonly string Description;
        public readonly string Path;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        Route(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
				var trkFilePath = MSTSPath.GetTRKFileName(path);
                try
                {
					var trkFile = new RouteFile(trkFilePath);
                    Name = trkFile.Tr_RouteFile.Name.Trim();
                    RouteID = trkFile.Tr_RouteFile.RouteID;
                    Description = trkFile.Tr_RouteFile.Description.Trim();
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileName(path) + ">";
                }
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(path) + ">";
                if (string.IsNullOrEmpty(Description)) Description = null;
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileName(path) + ">";
            }
            Path = path;
        }

        public override string ToString()
        {
            return Name;
        }

        // FIXME: Not needed, just left here for the TestingForm
        public static List<Route> GetRoutes(Folder folder)
        {
            var routes = new List<Route>();
            var directory = System.IO.Path.Combine(folder.Path, "ROUTES");
            if (Directory.Exists(directory))
            {
                foreach (var routeDirectory in Directory.GetDirectories(directory))
                {
                    try
                    {
                        routes.Add(new Route(routeDirectory));
                    }
                    catch { }
                }
            }
            return routes;
        }
        
        public static List<Route> GetRoutes()
        {
            var routes = new List<Route>();
            var directory = System.IO.Path.Combine(Vfs.MstsBasePath, "ROUTES");
            if (Vfs.DirectoryExists(directory))
            {
                foreach (var routeDirectory in Vfs.GetDirectories(directory))
                {
                    try
                    {
                        routes.Add(new Route(routeDirectory));
                    }
                    catch { }
                }
            }
            return routes;
        }
    }
}
