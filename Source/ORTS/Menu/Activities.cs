// COPYRIGHT 2011 by the Open Rails project.
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
        public readonly string FileName;
        public readonly ACTFile ACTFile;

        public Activity(string name, string fileName, ACTFile actFile)
        {
            Name = name;
            FileName = fileName;
            ACTFile = actFile;
        }

        public static List<Activity> GetActivities(Route route)
        {
            var activities = new List<Activity>();
            if (route != null)
            {
                activities.Add(new ExploreActivity());
                var directory = Path.Combine(route.Path, "ACTIVITIES");
                if (Directory.Exists(directory))
                {
                    foreach (var activityFile in Directory.GetFiles(directory, "*.act"))
                    {
                        if (Path.GetFileName(activityFile).StartsWith("ITR_e1_s1_w1_t1", StringComparison.OrdinalIgnoreCase))
                            continue;
                        try
                        {
                            var actFile = new ACTFile(activityFile, true);
                            activities.Add(new Activity(actFile.Tr_Activity.Tr_Activity_Header.Name, activityFile, actFile));
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
        public readonly string Path;
        public readonly string Consist;
        public readonly int StartHour;
        public readonly int StartMinute;
        public readonly int Season;
        public readonly int Weather;

        public ExploreActivity(string path, string consist, int season, int weather, int startHour, int startMinute)
            : base("- Explore Route -", null, null)
        {
            Path = path;
            Consist = consist;
            Season = season;
            Weather = weather;
            StartHour = startHour;
            StartMinute = startMinute;
        }

        public ExploreActivity()
            : this("", "", 0, 0, 12, 0)
        {
        }
    }
}