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
using Orts.Formats.Msts;

namespace ORTS.Menu
{
    public class Activity
    {
        public readonly string Name;
        public readonly string ActivityID;
        public readonly string Description;
        public readonly string Briefing;
        public readonly StartTime StartTime = new StartTime(10, 0, 0);
        public readonly SeasonType Season = SeasonType.Summer;
        public readonly WeatherType Weather = WeatherType.Clear;
        public readonly Difficulty Difficulty = Difficulty.Easy;
        public readonly Duration Duration = new Duration(1, 0);
        public readonly Consist Consist = new Consist("unknown", null);
        public readonly Path Path = new Path("unknown");
        public readonly string FilePath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        protected Activity(string filePath, Folder folder, Route route)
        {
            if (filePath == null && this is DefaultExploreActivity)
            {
                Name = catalog.GetString("- Explore Route -");
            }
            else if (filePath == null && this is ExploreThroughActivity)
            {
                Name = catalog.GetString("+ Explore in Activity Mode +");
            }
            else if (File.Exists(filePath))
            {
                var showInList = true;
                try
                {
                    var actFile = new ActivityFile(filePath);
                    var srvFile = new ServiceFile(System.IO.Path.Combine(System.IO.Path.Combine(route.Path, "SERVICES"), actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name + ".srv"));
                    // ITR activities are excluded.
                    if (actFile.Tr_Activity.Tr_Activity_Header.RouteID.ToUpper() == route.RouteID.ToUpper())
                    {
                        Name = actFile.Tr_Activity.Tr_Activity_Header.Name.Trim();
                        if (actFile.Tr_Activity.Tr_Activity_Header.Mode == ActivityMode.IntroductoryTrainRide) Name = "Introductory Train Ride";
                        Description = actFile.Tr_Activity.Tr_Activity_Header.Description;
                        Briefing = actFile.Tr_Activity.Tr_Activity_Header.Briefing;
                        StartTime = actFile.Tr_Activity.Tr_Activity_Header.StartTime;
                        Season = actFile.Tr_Activity.Tr_Activity_Header.Season;
                        Weather = actFile.Tr_Activity.Tr_Activity_Header.Weather;
                        Difficulty = actFile.Tr_Activity.Tr_Activity_Header.Difficulty;
                        Duration = actFile.Tr_Activity.Tr_Activity_Header.Duration;
                        Consist = new Consist(System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS"), srvFile.Train_Config + ".con"), folder);
                        Path = new Path(System.IO.Path.Combine(System.IO.Path.Combine(route.Path, "PATHS"), srvFile.PathID + ".pat"));
                        if (!Path.IsPlayerPath)
                        {
                            // Not nice to throw an error now. Error was originally thrown by new Path(...);
                            throw new InvalidDataException("Not a player path");
                        }
                    }
                    else//Activity and route have different RouteID.
                        Name = "<" + catalog.GetString("Not same route:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (!showInList) throw new InvalidDataException(catalog.GetStringFmt("Activity '{0}' is excluded.", filePath));
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                if (string.IsNullOrEmpty(Description)) Description = null;
                if (string.IsNullOrEmpty(Briefing)) Briefing = null;
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Activity> GetActivities(Folder folder, Route route)
        {
            var activities = new List<Activity>();
            if (route != null)
            {
                activities.Add(new DefaultExploreActivity());
                activities.Add(new ExploreThroughActivity());
                var directory = System.IO.Path.Combine(route.Path, "ACTIVITIES");
                if (Directory.Exists(directory))
                {
                    foreach (var activityFile in Directory.GetFiles(directory, "*.act"))
                    {
                        try
                        {
                            activities.Add(new Activity(activityFile, folder, route));
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
        public new string StartTime;
        public new SeasonType Season = SeasonType.Summer;
        public new WeatherType Weather = WeatherType.Clear;
        public new Consist Consist = new Consist("unknown", null);
        public new Path Path = new Path("unknown");

        internal ExploreActivity()
            : base(null, null, null)
        {
        }
    }

    public class DefaultExploreActivity : ExploreActivity
    { }

    public class ExploreThroughActivity : ExploreActivity
    { }
}
