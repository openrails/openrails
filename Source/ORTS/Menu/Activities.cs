// COPYRIGHT 2011, 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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