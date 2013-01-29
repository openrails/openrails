// COPYRIGHT 2011, 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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