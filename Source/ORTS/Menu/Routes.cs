// COPYRIGHT 2011, 2012 by the Open Rails project.
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
using MSTS;

namespace ORTS.Menu
{
    public class Route
    {
        public readonly string Name;
        public readonly string Path;
        public readonly TRKFile TRKFile;

        public Route(string path, TRKFile trkFile)
        {
            Name = trkFile.Tr_RouteFile.Name;
            Path = path;
            TRKFile = trkFile;
        }

        public override string ToString()
        {
            return Name;
        }

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
                        var trkFile = new TRKFile(MSTSPath.GetTRKFileName(routeDirectory));
                        routes.Add(new Route(routeDirectory, trkFile));
                    }
                    catch { }
                }
            }
            return routes;
        }
    }
}
