// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Updater;
using SharpDX.Direct3D9;
using static System.Net.WebRequestMethods;
using static Orts.Formats.Msts.TileName;
using static ORTS.Common.SystemInfo;
using static ORTS.NotificationPage;
using File = System.IO.File;
using Font = System.Drawing.Font;

namespace ORTS
{
    class Notifications
    {
        public List<Notification> NotificationList = new List<Notification>();
        public List<Check> CheckList = new List<Check>();
        public UpdateManager UpdateManager;
        public bool Available { get; set; } = true;

        public Notifications() { }


        public void CheckNotifications(UpdateManager updateManager)
        {
            var notifications = GetNotifications(); // Make this a background task
            Available = (updateManager.LastCheckError is System.Net.WebException) == false;
            NotificationList = notifications.NotificationList;
            CheckList = notifications.CheckList;
            UpdateManager = updateManager;
        }

        public Notifications GetNotifications()
        {
            //// Fetch the update URL (adding ?force=true if forced) and cache the update/error.
            //var client = new WebClient()
            //{
            //    CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
            //    Encoding = Encoding.UTF8,
            //};
            //client.Headers[HttpRequestHeader.UserAgent] = GetUserAgent();
            //var notificationsUri = new Uri("");
            //var notificationsData = client.DownloadString(notificationsUri);
            //var notifications = JsonConvert.DeserializeObject<NotificationList>(notificationsData);


            // Input from string
            //string json = @"
            //{
            //    'notificationList':
            //        [
            //            {
            //                'date': '{{release_date}}',
            //                'title': 'Update is available',
            //                'prefixItemList': 
            //                    [
            //                        {
            //                            '$type':'ORTS.Record, Menu',
            //                            'label': 'Update mode',
            //                            'indent': 140,
            //                            'value': 'Stable'
            //                        },
            //                        {
            //                            '$type':'ORTS.Record, Menu',
            //                            'label': 'Installed version',
            //                            'indent': 140,
            //                            'value': '{{installed_version}}'
            //                        },
            //                        {
            //                            '$type':'ORTS.Record, Menu',
            //                            'label': 'New version available',
            //                            'indent': 140,
            //                            'value': '{{new_version}}'
            //                        },
            //                        {
            //                            '$type':'ORTS.Link, Menu',
            //                            'label': 'What_s new',
            //                            'indent': 140,
            //                            'value': 'Find out on-line what_s new in this version.',
            //                            'url': 'https://www.openrails.org/discover/latest_stable_version'
            //                        }
            //                    ],
            //                'metLists':
            //                    { 
            //                        'itemList': 
            //                            [
            //                                {
            //                                    '$type':'ORTS.Update, Menu',
            //                                    'channel': 'testing',
            //                                    'label': 'Install',
            //                                    'indent': 140,
            //                                    'value': 'Install the new version'
            //                                }
            //                            ],
            //                        'checkIdList':
            //                            [
            //                                {
            //                                    'id': 'not_updated'
            //                                }
            //                            ]
            //                    },
            //                'suffixItemList': []
            //            }
            //        ],
            //    'CheckList': 
            //        [
            //            {
            //                'id': 'not_updated',
            //                'includesAnyOf': [],
            //                'excludesAllOf': 
            //                    [
            //                        { 
            //                            '$type':'ORTS.Contains, Menu',
            //                            'application': '{{installed_version}}'
            //                        }
            //                    ]
            //            }
            //        ]
            //}";

            //JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            //var notifications = JsonConvert.DeserializeObject<JsonInput>(json, settings);


            // Write to file
            // serialize formatted JSON to a file
            //string jsonOutput = JsonConvert.SerializeObject(notifications, Formatting.Indented);
            //File.WriteAllText(@"c:\_tmp\notifications.json", jsonOutput);


            // Input from file
            var filename = @"c:\_tmp\notifications.json";

            // read file into a string and deserialize JSON to a type
            var notificationsSerial = File.ReadAllText(filename);

            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jsonInput = JsonConvert.DeserializeObject<Notifications>(notificationsSerial, settings);

            return jsonInput;
        }

        public void ReplaceParameters()
        {
            foreach(var n in NotificationList)
            {
                n.Title = ReplaceParameter(n.Title);
                n.Date = ReplaceParameter(n.Date);
                n.PrefixItemList?.ForEach(item => ReplaceItemParameter(item));
                n.MetLists?.ItemList?.ForEach(item => ReplaceItemParameter(item));
                n.SuffixItemList?.ForEach(item => ReplaceItemParameter(item));
            }
            foreach (var c in CheckList)
            {
                c.ExcludesAllOf?.ForEach(criteria =>  ReplaceCriteriaParameter(criteria));
                c.IncludesAnyOf?.ForEach(criteria => ReplaceCriteriaParameter(criteria));
            }
        }

        private void ReplaceItemParameter(Item item)
        {
            if (item is Record record)
                record.Value = ReplaceParameter(record.Value);
            if (item is Link link)
                link.Value = ReplaceParameter(link.Value);
            if (item is Update update)
                update.Value = ReplaceParameter(update.Value);
        }

        private void ReplaceCriteriaParameter(Criteria criteria)
        {
            criteria.Value = ReplaceParameter(criteria.Value);
        }

        private string ReplaceParameter(string field)
        {
            if (field.Contains("{{") == false)
                return field;

            field = field.TrimStart(' ', '{');
            field = field.TrimEnd('}', ' ');

            switch (field.ToLower())
            {
                case "update_mode":
                    field = (UpdateManager.ChannelName == "")
                        ? "none"
                        : UpdateManager.ChannelName;
                    break;
                case "new_version":
                    field = (UpdateManager.LastUpdate == null || UpdateManager.ChannelName == "")
                        ? "none"
                        : UpdateManager.LastUpdate.Version;
                    break;
                case "release_date":
                    field = (UpdateManager.LastUpdate == null) 
                        ? "none" 
                        : $"{UpdateManager.LastUpdate.Date:dd-MMM-yy}";
                    break;
                case "installed_version":
                    field = SystemInfo.Application.Version;
                    break;
                case "runtime":
                    field = SystemInfo.Runtime.ToString();
                    break;
                case "system":
                    field = SystemInfo.OperatingSystem.ToString();
                    break;
                case "memory":
                    field = SystemInfo.Direct3DFeatureLevels.ToString();
                    break;
                case "cpu":
                    field = "";
                    foreach (var cpu in SystemInfo.CPUs)
                    {
                        field += $", {cpu.Name}";
                    }
                    break;
                case "gpu":
                    field = "";
                    foreach (var gpu in SystemInfo.GPUs)
                    {
                        field += $", {gpu.Name}";
                    }
                    break;
                case "direct3d":
                    field = string.Join(",", SystemInfo.Direct3DFeatureLevels);
                    break;
                default:
                    break;
            }

            return field;
        }

        /// <summary>
        /// Drop any notifications for the channel not selected
        /// </summary>
        /// <param name="updateManager"></param>
        public void DropUnusedUpdateNotifications(UpdateManager updateManager)
        {
            var updateModeSetting = updateManager.ChannelName.ToLower();
            foreach(var n in NotificationList)
            {
                if (n.UpdateMode == null)   // Skip notifications which are not updates
                    continue;

                var lowerUpdateMode = n.UpdateMode.ToLower();

                // If setting == "none", then keep just one update notification
                if (updateModeSetting == "" && lowerUpdateMode == "stable")
                    continue;

                // Mark unused updates for deletion
                n.ToDelete = lowerUpdateMode != updateModeSetting;
            }
            NotificationList.RemoveAll(n => n.ToDelete);
        }
    }

    class JsonInput
    {
        public List<Notification> NotificationList { get; set; }
        public List<Check> CheckList { get; set; }
    }

    class Notification
    {
        public string Date { get; set; }
        public string Title { get; set; }
        public string UpdateMode { get; set; }
        public List<Item> PrefixItemList { get; set; }
        public Met MetLists { get; set; }
        public List<Item> SuffixItemList { get; set; }
        public bool ToDelete { get; set; } = false; // So we can mark items for deletion and then delete in single statement.
    }
    class Record : Item
    {
        public string Value { get; set; }
    }
    class Text : Item
    {
        public string Color { get; set; } = "black";
    }
    class Heading : Item
    {
        public string Color { get; set; } = "red";
    }
    class Link : Item
    {
        public string Value { get; set; }
        public string Url { get; set; }
    }
    class Update : Item
    {
        public string Value { get; set; }
        public string UpdateMode { get; set; }
    }
    class Item
    {
        public string Label { get; set; }
        public int Indent { get; set; } = 140;
    }
    class Met
    {
        public List<Item> ItemList { get; set; }
        public List<CheckId> CheckIdList { get; set; }
    }
    class CheckId
    {
        public string Id { get; set; }
    }
    class Check
    {
        public string Id { get; set; }
        public List<Criteria> IncludesAnyOf { get; set; }
        public List<Criteria> ExcludesAllOf { get; set; }
        public List<Item> UnmetItemList { get; set; }
        public bool IsChecked { get; set; } = false;
        public bool IsMet { get; set; } = false;
    }
    class Contains : Criteria { }
    class NoLessThan : Criteria { }
    class NoMoreThan : Criteria { }
    class Criteria
    {
        // System Information "examples"
        public string Name { get; set; }    // installed_version, runtime, system, memory, cpu, gpu, direct3d
        public string Value { get; set; }   // {{new_version}}, {{10_0}}
    }
}
