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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MSTS;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;

namespace LibAE
{

    public class ConsistInfo
    {
        [JsonProperty("ConsistName")]
        public string consistName;
        [JsonProperty("ConsistPath")]
        string consistPath;

        public ConsistInfo (string name, string path)
        {
            consistName = name;
            consistPath = path;
        }
    }

    [Serializable()]
    public class ActivityInfo
    {
        [JsonIgnore]
        public List<string> routePaths { get; set; }
        [JsonIgnore]
        public List<ConsistInfo> trainConsists { get; set; }
        [JsonProperty("ActivityName")]
        public string ActivityName { get; set; }
        [JsonProperty("RoutePath")]
        public string RoutePath { get; set; }
        [JsonProperty("Description")]
        public string ActivityDescr { get; set; }
        [JsonProperty("FileName")]
        public string FileName { get; set; }
        [JsonProperty("ItemWidget")]
        public List<globalItem> ActItemWidget { get; set; }


        public ActivityInfo()
        {
            routePaths = new List<string>();
            trainConsists = new List<ConsistInfo>();
            ActItemWidget = new List<globalItem>();

        }

        public void config (List<string> routes)
        {
            foreach (string routeParent in routes)
            {
                if (!Directory.Exists(routeParent))
                {
                    continue;
                }
                string[] subdirectoryEntries = Directory.GetDirectories(routeParent);
                foreach (string route in subdirectoryEntries)
                {
                    string[] files = Directory.GetFileSystemEntries(route, "*.trk");
                    if (files.Count() == 1)
                    {
                        routePaths.Add(route);
                    }
                }
                string consistPath = Path.Combine(routeParent, "../TRAINS/CONSISTS");
                subdirectoryEntries = Directory.GetFileSystemEntries(consistPath, "*.con");
                foreach (string consist in subdirectoryEntries)
                {
                    string fullPathConsist = Path.GetFullPath(consist);
                    CONFile consistName = new CONFile(fullPathConsist);
                    ConsistInfo conInfo = new ConsistInfo(consistName.ToString(), fullPathConsist);
                    trainConsists.Add(conInfo); 
                }
            }
        }

        public void AddActWidget(globalItem item)
        {
            if (item.GetType() == typeof(ActStartItem))
            {
                foreach (var action in ActItemWidget)
                {
                    if (action.GetType() == typeof(ActStartItem))
                    {
                        return;
                    }
                }
            }
            else if (item.GetType() == typeof(ActStopItem))
            {
                bool StartFound = false;
                foreach (var action in ActItemWidget)
                {
                    if (action.GetType() == typeof(ActStopItem))
                    {
                        return;
                    }
                    if (action.GetType() == typeof(ActStartItem))
                    {
                        StartFound = true;
                    }
                }
                if (!StartFound)
                    return;
            }
            else if (item.GetType() == typeof(ActWaitItem))
            {
                bool StartFound = false;
                foreach (var action in ActItemWidget)
                {
                    if (action.GetType() == typeof(ActStartItem))
                    {
                        StartFound = true;
                    }
                }
                if (!StartFound)
                    return;
            }
            ActItemWidget.Add(item);
        }

        public bool saveActivity(string activityName)
        {
            FileName = Path.GetFileName(activityName);
            //RoutePath = Path.GetDirectoryName(activityName);

            return saveActivity();
        }

        public bool saveActivity()
        {
            if (FileName == null || FileName.Length <= 0)
                return false;
            string activityPath = Path.Combine(RoutePath, "activities");
            string completeFileName = Path.Combine(activityPath, FileName);

            JsonSerializer serializer = new JsonSerializer();
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            serializer.TypeNameHandling = TypeNameHandling.All;
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter wr = new StreamWriter(completeFileName))
            {
                using (JsonWriter writer = new JsonTextWriter(wr))
                {
                    serializer.Serialize(writer, this);
                }
            }
            return true;
        }

        static public ActivityInfo loadActivity(string fileName)
        {
            ActivityInfo p;

            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(fileName))
                {
                    ActivityInfo info = JsonConvert.DeserializeObject<ActivityInfo>((string)sr.ReadToEnd(), new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                    p = info;
                }

            }
            catch (IOException e)
            {
                fileName += ".act.json";
                p = new ActivityInfo();
                p.FileName = Path.GetFileName(fileName);
                p.RoutePath = Path.GetDirectoryName(fileName);
                DirectoryInfo parent = Directory.GetParent(p.RoutePath);
                p.RoutePath = parent.FullName;
                parent = Directory.GetParent(p.RoutePath);
                p.RoutePath = parent.FullName;
                p.ActivityName = "";
                p.ActivityDescr = "";
            }
            return p;
        }
    }
}
