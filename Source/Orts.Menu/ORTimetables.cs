// COPYRIGHT 2014 by the Open Rails project.
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

using GNU.Gettext;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using System;
using System.Collections.Generic;
using ORTS.Common;

namespace ORTS.Menu
{
    public class TimetableInfo
    {
        public readonly List<TimetableFileLite> ORTTList = new List<TimetableFileLite>();
        public readonly String Description;
        public readonly String fileName;

        // items set for use as parameters, taken from main menu
        public int Day;
        public int Season;
        public int Weather;
        public String WeatherFile;

        // note : file is read preliminary only, extracting description and train information
        // all other information is read only when activity is started

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        protected TimetableInfo(string filePath)
        {
            if (Vfs.FileExists(filePath))
            {
                try
                {
                    ORTTList.Add(new TimetableFileLite(filePath));
                    Description = ORTTList[0].Description;
                    fileName = filePath;
                }
                catch
                {
                    Description = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Description = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        protected TimetableInfo(String filePath, String directory)
        {
            if (Vfs.FileExists(filePath))
            {
                try
                {
                    TimetableGroupFileLite multiInfo = new TimetableGroupFileLite(filePath, directory);
                    ORTTList = multiInfo.ORTTInfo;
                    Description = multiInfo.Description;
                    fileName = filePath;
                }
                catch
                {
                    Description = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Description = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        public override string ToString()
        {
            return Description;
        }

        // get timetable information
        public static List<TimetableInfo> GetTimetableInfo(Route route)
        {
            var ORTTInfo = new List<TimetableInfo>();
            if (route != null)
            {
                var actdirectory = System.IO.Path.Combine(route.Path, "ACTIVITIES");
                var directory = System.IO.Path.Combine(actdirectory, "OPENRAILS");

                if (Vfs.DirectoryExists(directory))
                {
                    foreach (var ORTimetableFile in Vfs.GetFiles(directory, "*.timetable_or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORTimetableFile));
                        }
                        catch { }
                    }

                    foreach (var ORTimetableFile in Vfs.GetFiles(directory, "*.timetable-or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORTimetableFile));
                        }
                        catch { }
                    }

                    foreach (var ORMultitimetableFile in Vfs.GetFiles(directory, "*.timetablelist_or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORMultitimetableFile, directory));
                        }
                        catch { }
                    }

                    foreach (var ORMultitimetableFile in Vfs.GetFiles(directory, "*.timetablelist-or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORMultitimetableFile, directory));
                        }
                        catch { }
                    }
                }
            }
            return ORTTInfo;
        }
    }

    public class WeatherFileInfo
    {
        public string filedetails;

        public WeatherFileInfo(string filename)
        {
            filedetails = filename;
        }

        public override string ToString()
        {
            return System.IO.Path.GetFileName(filedetails);
        }

        public string GetFullName()
        {
            return filedetails;
        }

        // get weatherfiles
        public static List<WeatherFileInfo> GetTimetableWeatherFiles(Route route)
        {
            var weatherInfo = new List<WeatherFileInfo>();
            if (route != null)
            {
                var directory = System.IO.Path.Combine(route.Path, "WeatherFiles");

                if (Vfs.DirectoryExists(directory))
                {
                    foreach (var weatherFile in Vfs.GetFiles(directory, "*.weather-or"))
                    {
                        weatherInfo.Add(new WeatherFileInfo(weatherFile));
                    }

                }
            }
            return weatherInfo;
        }
    }
}
