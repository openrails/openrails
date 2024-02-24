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
using static ORTS.NotificationPage;
using File = System.IO.File;
using Font = System.Drawing.Font;

namespace ORTS
{
    class Notifications
    {
        string GetUserAgent()
        {
            return String.Format($"{Application.ProductName}/{VersionInfo.VersionOrBuild}");
        }

        public List<Notification> NotificationList = new List<Notification>();

        public Notifications() { }


        public void CheckNotifications()
        {
            NotificationList = GetNotifications(); // Make this a background task
        }

        public List<Notification> GetNotifications()
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
            var jsonInput = JsonConvert.DeserializeObject<JsonInput>(notificationsSerial, settings);

            return jsonInput.NotificationList;
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
        public List<Item> PrefixItemList { get; set; }
        public Met MetLists { get; set; }
        public List<Item> SuffixItemList { get; set; }
    }
    class Record : Item
    {
        public string Value { get; set; }
    }
    class Link : Item
    {
        public string Value { get; set; }
        public string Url { get; set; }
    }
    class Update : Item
    {
        public string Value { get; set; }
        public string Channel { get; set; }
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
    }
    class Contains : Criteria { }
    class NoLessThan : Criteria { }
    class NoMoreThan : Criteria { }
    class Criteria
    {
        // System Information "examples"
        public string DateTime { get; set; }    // "23/11/2023 12:40:45 (2023-11-23 12:40:45Z)"
        public string Application { get; set; } // "Open Rails U2023.11.23-0122 (X64)"
        public string Runtime { get; set; } // ".NET Framework 4.8.9181.0"
        public string System { get; set; }  // "Microsoft Windows 11 Home 10.0.22621 (X64; en-GB; en-GB,en-US,ja-JP)"
        public string Memory { get; set; }  // "32,592 MB"
        public string CPU { get; set; } // "12th Gen Intel(R) Core(TM) i7-1255U (GenuineIntel; 12 threads; 2,600 MHz)"
        public string GPU { get; set; } // "Intel(R) Iris(R) Xe Graphics (Intel Corporation; 1,024 MB)"
        public string Direct3D { get; set; }    // "12_1,12_0,11_1,11_0,10_1,10_0,9_3,9_2,9_1"
    }
    class Unmet
    {
        public List<Item> ItemList { get; set; }
    }
}
