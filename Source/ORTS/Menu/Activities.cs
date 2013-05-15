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

using System;
using System.Collections.Generic;
using System.IO;
using MSTS;

namespace ORTS.Menu
{
    public class Activity
    {
        public readonly string Name;
        public readonly string FilePath;
        public readonly ACTFile ACTFile;

        public Activity(string filePath, ACTFile actFile)
        {
            Name = actFile.Tr_Activity.Tr_Activity_Header.Name;
            FilePath = filePath;
            ACTFile = actFile;
        }

        public Activity(string name)
        {
            Name = name;
            FilePath = null;
            ACTFile = null;
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Activity> GetActivities(Route route)
        {
            var activities = new List<Activity>();
            if (route != null)
            {
                activities.Add(new ExploreActivity());
                var directory = System.IO.Path.Combine(route.Path, "ACTIVITIES");
                if (Directory.Exists(directory))
                {
                    foreach (var activityFile in Directory.GetFiles(directory, "*.act"))
                    {
                        if (System.IO.Path.GetFileName(activityFile).StartsWith("ITR_e1_s1_w1_t1", StringComparison.OrdinalIgnoreCase))
                            continue;
                        try
                        {
                            var actFile = new ACTFile(activityFile, true);
                            activities.Add(new Activity(activityFile, actFile));
                        }
                        catch { }
                    }
                }
            }
            return activities;
        }
    }

    public class ExploreActivity : Activity
    {
        public readonly Path Path;
        public readonly Consist Consist;
        public readonly int StartHour;
        public readonly int StartMinute;
        public readonly int Season;
        public readonly int Weather;

        public ExploreActivity(Path path, Consist consist, int season, int weather, int startHour, int startMinute)
            : base("- Explore Route -")
        {
            Path = path;
            Consist = consist;
            Season = season;
            Weather = weather;
            StartHour = startHour;
            StartMinute = startMinute;
        }

        public ExploreActivity()
            : this(null, null, 0, 0, 12, 0)
        {
        }
    }
}
