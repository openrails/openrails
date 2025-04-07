// COPYRIGHT 2012, 2013, 2014 by the Open Rails project.
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
using GNU.Gettext;
using Orts.Formats.Msts;
using System;
using ORTS.Common;

namespace ORTS.Menu
{
    /// <summary>
    /// Representation of the metadata of a path, where the path is coded in a .pat file. So not the full .pat file,
    /// but just basic information to be used in menus etc.
    /// </summary>
    public class Path
    {
        /// <summary>Name of the path</summary>
        public readonly string Name;
        /// <summary>Start location of the path</summary>
        public readonly string Start;
        /// <summary>Destination location of the path</summary>
        public readonly string End;
        /// <summary>Full filename of the underlying .pat file</summary>
        public readonly string FilePath;
        /// <summary>Is the path a player path or not</summary>
        public readonly bool IsPlayerPath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        /// <summary>
        /// Constructor. This will try to have the requested .pat file parsed for its metadata
        /// </summary>
        /// <param name="filePath">The full name of the .pat file</param>
        internal Path(string filePath)
        {
            if (Vfs.FileExists(filePath))
            {
                try
                {
                    var patFile = new PathFile(filePath);
                    this.IsPlayerPath = patFile.IsPlayerPath;
                    Name = patFile.Name.Trim();
                    Start = patFile.Start.Trim();
                    End = patFile.End.Trim();
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                if (string.IsNullOrEmpty(Start)) Start = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                if (string.IsNullOrEmpty(End)) End = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            else
            {
                Name = Start = End = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            this.FilePath = filePath;
        }

        /// <summary>
        /// A path will be identified by its destination
        /// </summary>
        public override string ToString()
        {
            return End;
        }

        /// <summary>
        /// Return a list of paths that belong to the given route.
        /// </summary>
        /// <param name="route">The Route for which the paths need to be found</param>
        /// <param name="includeNonPlayerPaths">Selects whether non-player paths are included or not</param>
        public static List<Path> GetPaths(Route route, bool includeNonPlayerPaths)
        {
            var paths = new List<Path>();
            var directory = System.IO.Path.Combine(route.Path, "PATHS");
            if (Vfs.DirectoryExists(directory))
            {
                foreach (var file in Vfs.GetFiles(directory, "*.pat"))
                {
                    Path path = null;
                    try
                    {
                        path = new Path(file);
                    }
                    catch { }

                    bool pathShouldBeIncluded = includeNonPlayerPaths || path.IsPlayerPath;
                    if (pathShouldBeIncluded)
                    {
                        // Suppress the 7 broken paths shipped with MSTS
                        //
                        // MSTS ships with 7 unfinished paths, which cannot be used as they reference tracks that do not exist.
                        // MSTS checks for "broken path" before running the simulator and doesn't offer them in the list.
                        // ORTS checks for "broken path" when the simulator runs and does offer them in the list.
                        // The first activity in Marias Pass is "Explore Longhale" which leads to a "Broken Path" message.
                        // The message then confuses users new to ORTS who have just installed it along with MSTS,
                        // see https://bugs.launchpad.net/or/+bug/1345172 and https://bugs.launchpad.net/or/+bug/128547
                        if (!file.EndsWith("ROUTES/USA1/PATHS/aftstrm(traffic03).pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA1/PATHS/aftstrmtraffic01.pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA1/PATHS/aiphwne2.pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA1/PATHS/aiwnphex.pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA1/PATHS/blizzard(traffic).pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA2/PATHS/longhale.pat", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith("ROUTES/USA2/PATHS/long-haul west (blizzard).pat", StringComparison.OrdinalIgnoreCase)
                            )
                        {
                            paths.Add(path);
                        }
                    }
                }
            }
            return paths;
        }

        /// <summary>
        /// Get a path from a certain route with given name.
        /// </summary>
        /// <param name="route">The Route for which the paths need to be found</param>
        /// <param name="name">The (file) name of the path, without directory, any extension allowed</param>
        /// <param name="allowNonPlayerPath">Are non-player paths allowed?</param>
        /// <returns>The path that has been found and is allowed, or null</returns>
        public static Path GetPath(Route route, string name, bool allowNonPlayerPath)
        {
            Path path;
            string directory = System.IO.Path.Combine(route.Path, "PATHS");
            string file = System.IO.Path.Combine(directory, System.IO.Path.ChangeExtension(name, "pat"));
            try
            {
                path = new Path(file);
            }
            catch {
                path = null;
            }

            bool pathIsAllowed = allowNonPlayerPath || path.IsPlayerPath;
            if (!pathIsAllowed)
            {
                path = null;
            }

            return path;
        }

        /// <summary>
        /// Additional information strings about the metadata
        /// </summary>
        /// <returns>array of strings with the user-readable information</returns>
        public string[] ToInfo()
        {
            string[] infoString = new string[] {
                catalog.GetStringFmt("Start at: {0}", Start),
                catalog.GetStringFmt("Heading to: {0}", End),
            };

            return (infoString);
        }
    }
}
