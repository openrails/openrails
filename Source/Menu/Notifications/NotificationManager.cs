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
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Updater;
using static ORTS.Common.SystemInfo;
using static ORTS.NotificationPage;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;

// Notifications are read only once as a background task at start into NotificationList.
// Every time the notifications page is re-visited, the position is discarded and first page is shown.
// Every time the notifications page is re-visited, the options may have changed, so
// the visibility of each notification in NotificationList is re-assessed. Also its date.
// Every time the notifications page is re-visited or the page is incremented up or down,
// the current notification is re-assessed and the panel is re-loaded with items for the current page.

namespace ORTS
{
    public class NotificationManager
    {
        public class PageTracking
        {
            // Notifications are listed in reverse date order, with the newest one at the front.
            public string LastViewDate { get; set; } // This is the date when notifications were last viewed and is saved in yyyy-MM-dd format as a hidden UserSetting.
            public int Count { get; set; }  // New notifications are those with a date after the LastViewDate.
            public int Viewed { get; set; } // Viewing always starts at the first, newest notification. The user sees (Count - Viewed) 
        }

        public Notifications Notifications; // An object defined by the JSON schema
        public int CurrentNotificationNo = 0;
        public bool ArePagesVisible = false;

        private Exception Error;
        private Dictionary<string, string> ParameterDictionary;

        private bool Log = false;
        private const string LogFile = "notifications_trial_log.txt";

        private readonly MainForm MainForm; // Needed so we can add controls to the NotificationPage
        private readonly UpdateManager UpdateManager;
        private readonly UserSettings Settings;
        private readonly Panel Panel;

        public NotificationPage Page { get; private set; }
        public Image PreviousImage { get; private set; }
        public Image NextImage { get; private set; }
        public Image FirstImage { get; private set; }
        public Image LastImage { get; private set; }
        public PageTracking NewPages { get; private set; }

        public NotificationManager(MainForm mainForm, UpdateManager updateManager, UserSettings settings, Panel panel
            , Image previousImage
            , Image nextImage
            , Image firstImage
            , Image lastImage
            ) 
        { 
            MainForm = mainForm;
            this.UpdateManager = updateManager;
            this.Settings = settings;
            Panel = panel;
            PreviousImage = previousImage;
            NextImage = nextImage;
            FirstImage = firstImage;
            LastImage = lastImage;
            NewPages = new PageTracking();
        }

        //TODO Make this a background task
        public void CheckNotifications()
        {
            try
            {
                Error = null;
                ArePagesVisible = false;
                CurrentNotificationNo = 0;
                Notifications = GetNotifications();
                ParameterDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                // To support testing, add any overriding values to the ValueDictionary
                GetOverrideParameters()?.ParameterValueList.ForEach(i => ParameterDictionary.Add(i.Parameter, i.Value));
                LogOverrideParameters();

                ReplaceParameters();
                LogParameters();

                Notifications.NotificationList = IncludeValid(Notifications.NotificationList);
                Notifications.NotificationList = SortByDate(Notifications.NotificationList);
            }
            catch (WebException ex)
            {
                Error = ex;
            }
        }

        public Notifications GetNotifications()
        {
            string notificationsSerial;
            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications() tests first for a local file notifications_trial.json
            // and uses that if present, else it uses the remote file.

            var filename = @"notifications_trial.json";
            if (System.IO.File.Exists(filename))
            {
                // Input from local file into a string
                notificationsSerial = System.IO.File.ReadAllText(filename);

                // Turn on logging
                Log = true;
            }
            else
            {
                // Input from remote file into a string
                notificationsSerial = GetRemoteJson();
            }

            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jsonInput = JsonConvert.DeserializeObject<Notifications>(notificationsSerial, jsonSettings);

            NewPages.Count = 0;
            NewPages.Viewed = 0;
            NewPages.LastViewDate = Settings.LastViewNotificationDate;
            if (NewPages.LastViewDate == "") NewPages.LastViewDate = "2024-01-01"; // Date of this code - i.e. before Notifications went public

            //TODO Cache jsonInput

            return jsonInput;
        }

        /// <summary>
        /// Fetch the Notifications from https://static.openrails.org/api/notifications/menu.json 
        /// This file is copied hourly from Github openrails/notifications/
        /// </summary>
        private string GetRemoteJson()
        {
            var client = new WebClient()
            {
                CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache),
                Encoding = Encoding.UTF8,
            };
            // Helpful to supply server with data for its log file.
            client.Headers[HttpRequestHeader.UserAgent] = $"{System.Windows.Forms.Application.ProductName}/{VersionInfo.VersionOrBuild}";

            return client.DownloadString(new Uri("https://wepp.co.uk/openrails/notifications2/menu.json"));
            //return client.DownloadString(new Uri("https://static.openrails.org/api/notifications/menu.json"));
        }

        private List<Notification> IncludeValid(List<Notification> list)
        {
            NewPages.Count = 0;

            var filteredList = new List<Notification>();
            foreach (var n in list)
            {
                if (AreNotificationChecksMet(n))
                {
                    if (n.Date == "none") 
                        n.Date = " none"; // UpdateChannel = "none" found; push this to end of the list
                    else
                    {
                        if (String.Compare(NewPages.LastViewDate, n.Date) == -1) NewPages.Count++;
                    }
                    filteredList.Insert(0, n); // Add to head of list to provide a basic pre-sort.
                }
            }
            return filteredList;
        }

        private List<Notification> SortByDate(List<Notification> list)
        {
            return list.OrderByDescending(n => n.Date).ToList();
        }

        /// <summary>
        /// Adds details of the current notifications to the panel
        /// </summary>
        public void PopulatePage()
        {
            Settings.LastViewNotificationDate = $"{DateTime.Today:yyyy-MM-dd}";    
            Settings.Save("LastViewNotificationDate");  // Saves the date on any viewing of notifications
            Page = new NotificationPage(MainForm, Panel, this); 

            if (UpdateManager.LastCheckError != null || Error != null)
            {
                NewPages.Count = 0;
                var message = (UpdateManager.LastCheckError != null)
                    ? UpdateManager.LastCheckError.Message
                    : Error.Message;

                // Reports notifications are not available.
                var channelName = UpdateManager.ChannelName == "" ? "None" : UpdateManager.ChannelName;
                var today = DateTime.Now.Date;
                Page.NDetailList.Add(new NTitleControl(Page, 1, 1, $"{today:dd-MMM-yy}", "Notifications are not available"));
                Page.NDetailList.Add(new NRecordControl(Page, "Update mode", 140, channelName));
                Page.NDetailList.Add(new NRecordControl(Page, "Installed version", 140, VersionInfo.VersionOrBuild));

                Page.NDetailList.Add(new NHeadingControl(Page, "Notifications are not available", "red"));
                Page.NDetailList.Add(new NTextControl(Page, $"Error: {message}"));
                Page.NDetailList.Add(new NTextControl(Page, "Is your Internet connected?"));

                Page.NDetailList.Add(new NRetryControl(Page, "Retry", 140, "Try again to fetch notifications", MainForm));
            }
            else
            {
                var list = Notifications.NotificationList;
                var n = list[CurrentNotificationNo];
                LogNotification(n);

                Page.NDetailList.Add(new NTitleControl(Page, CurrentNotificationNo + 1, list.Count, n.Date, n.Title));

                // Check constraints for each item
                foreach (var item in n.ItemList)
                {
                    if (AreItemChecksMet(item)) AddItemToPage(Page, item);
                }
            }

            Page.NDetailList.Add(new NTextControl(Page, ""));
            Page.NDetailList.Add(new NTextControl(Page, "(Toggle icon to hide these notifications.)"));
        }

        #region Process Criteria
        private bool AreNotificationChecksMet(Notification notification)
        {
            if (notification.IncludeIf != null || notification.IncludeIfNot != null)
            {
                AppendToLog($"Title: {notification.Title}");
            }
            if (notification.IncludeIf != null)
            {
                foreach (var checkName in notification.IncludeIf)
                {
                    // Include if A=true AND B=true AND ...
                    if (IsCheckMet(checkName) == false) return false;
                }
            }
            if (notification.IncludeIfNot != null)
            {
                foreach (var checkName in notification.IncludeIfNot)
                {
                    // Include if C=false AND D=false AND ...
                    if (IsCheckMet(checkName) == true) return false;
                }
            }

            return true;
        }

        private bool AreItemChecksMet(Item item)
        {
            if (item.IncludeIf != null || item.IncludeIfNot != null)
            {
                AppendToLog($"Label: {item.Label}");
            }
            if (item.IncludeIf != null)
            {
                foreach (var checkName in item.IncludeIf)
                {
                    // Include if A=true AND B=true AND ...
                    if (IsCheckMet(checkName) == false) return false;
                }
            }
            if (item.IncludeIfNot != null)
            {
                foreach (var checkName in item.IncludeIfNot)
                {
                    // Include if C=false AND D=false AND ...
                    if (IsCheckMet(checkName) == true) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Could cache these checks to improve performance.
        /// </summary>
        /// <param name="checkname"></param>
        /// <returns></returns>
        private bool IsCheckMet(string checkname)
        {
            foreach (var check in Notifications.CheckList)
            {
                if (check.Id == checkname)
                {
                    if (CheckAnyMatch(check.AnyOfList)) return true;
                }
            }
            return false;
        }

        private bool CheckAnyMatch(List<AnyOf> anyOfList)
        {
            foreach (var anyOf in anyOfList)
            {
                if (CheckAllMatch(anyOf.AllOfList)) return true; 
            }
            return false;
        }

        private bool CheckAllMatch(List<Criteria> criteriaList)
        {
            foreach (var c in criteriaList)
            {
                if (c is Contains) // other criteria might be added such as NoLessThan and NoMoreThan.
                {
                    if (CheckContains(c, true) == false) return false;
                }
                if (c is NotContains)
                {
                    if (CheckContains(c, false) == false) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if a match is found and sense = true. For NotContains, use sense = false.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="sense"></param>
        /// <returns></returns>
        private bool CheckContains(Criteria criteria, bool sense)
        {
            // If Property was a parameter, then use the expansion
            var content = ParameterDictionary.ContainsKey(criteria.Property)
                ? ParameterDictionary[criteria.Property]
                : criteria.Property;

            var result = content.IndexOf(criteria.Value, StringComparison.OrdinalIgnoreCase) > -1 == sense;
            LogCheckContains(criteria.Value, sense, content, result);
            return result;
        }
        #endregion

        private void AddItemToPage(NotificationPage page, Item item)
        {
            if (item is Record record)
            {
                Page.NDetailList.Add(new NRecordControl(page, item.Label, item.Indent, record.Value));
            }
            else if (item is Link link)
            {
                var url = GetUrl(link);
                if (string.IsNullOrEmpty(url) == false)
                {
                    Page.NDetailList.Add(new NLinkControl(page, item.Label, item.Indent, link.Value, MainForm, url));
                }
            }
            else if (item is Update update)
            {
                Page.NDetailList.Add(new NUpdateControl(page, item.Label, item.Indent, update.Value, MainForm));
            }
            else if (item is Heading heading)
            {
                Page.NDetailList.Add(new NHeadingControl(page, item.Label, heading.Color));
            }
            else if (item is Text text)
            {
                Page.NDetailList.Add(new NTextControl(page, item.Label, text.Color));
            }
            else
            {
                Page.NDetailList.Add(new NTextControl(page, item.Label));
            }
        }

        private string GetUrl(Link link)
        {
            var url = link.Url;
            if (string.IsNullOrEmpty(url))
            {
                switch (UpdateManager.ChannelName.ToLower())
                {
                    case "stable":
                    case "": // Channel None
                        url = link.StableUrl;
                        break;
                    case "testing":
                        url = link.TestingUrl;
                        break;
                    case "unstable":
                        url = link.UnstableUrl;
                        break;
                }
            }

            return url;
        }

        public void ReplaceParameters()
        {
            foreach (var n in Notifications.NotificationList)
            {
                n.Title = ReplaceParameter(n.Title);
                n.Date = ReplaceParameter(n.Date);
                n.ItemList?.ForEach(item => ReplaceItemParameter(item));
            }
            foreach (var list in Notifications.CheckList)
            {
                foreach(var c in list?.AnyOfList)
                {
                    c?.AllOfList.ForEach(criteria => ReplaceCriteriaPropertyParameter(criteria));
                    c?.AllOfList.ForEach(criteria => ReplaceCriteriaValueParameter(criteria));
                }
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

        /// <summary>
        /// If Property is a parameter, remove {{..}} and add it and its replacement to the dictionary.
        /// </summary>
        /// <param name="criteria"></param>
        private void ReplaceCriteriaPropertyParameter(Criteria criteria)
        {
            if (ContainsParameter(criteria.Property))
            {
                criteria.Property = ReplaceParameter(criteria.Property);
            }
        }

        private void ReplaceCriteriaValueParameter(Criteria criteria)
        {
            criteria.Value = ReplaceParameter(criteria.Value);
        }

        private string ReplaceParameter(string field)
        {
            if (ContainsParameter(field) == false) return field;

            var parameterArray = field.Split('{', '}'); // 5 elements: prefix, "", target, "", suffix
            var target = parameterArray[2];
            var lowerCaseTarget = parameterArray[2].ToLower();
            string replacement;

            // If found in dictionary, then use that else extract it from program
            if (ParameterDictionary.ContainsKey(lowerCaseTarget))
            {
                replacement = ParameterDictionary[lowerCaseTarget];
            }
            else
            {
                switch (lowerCaseTarget)
                {
                    // Using "none" instead of "" so that records are readable.
                    case "update_mode":
                        replacement = (UpdateManager.ChannelName == "")
                            ? "none"
                            : UpdateManager.ChannelName;
                        break;
                    case "latest_version":
                        replacement = UpdateManager.LastUpdate == null
                            || UpdateManager.ChannelName == ""
                            ? "none"
                            : UpdateManager.LastUpdate.Version;
                        break;
                    case "release_date":
                        replacement = UpdateManager.LastUpdate == null
                            ? "none"
                            : $"{UpdateManager.LastUpdate.Date:yyyy-MM-dd}";
                        break;

                    case "installed_version":
                        replacement = SystemInfo.Application.Version;
                        break;
                    case "runtime":
                        replacement = Runtime.ToString();
                        break;
                    case "system":
                        replacement = SystemInfo.OperatingSystem.ToString();
                        break;
                    case "memory":
                        replacement = Direct3DFeatureLevels.ToString();
                        break;
                    case "cpu":
                        replacement = "";
                        foreach (var cpu in CPUs)
                        {
                            replacement += $", {cpu.Name}";
                        }
                        break;
                    case "gpu":
                        replacement = "";
                        foreach (var gpu in GPUs)
                        {
                            replacement += $", {gpu.Name}";
                        }
                        break;
                    case "direct3d":
                        replacement = string.Join(",", Direct3DFeatureLevels);
                        break;

                    case "installed_routes":
                        replacement = GetInstalledRoutes();
                        break;

                    default:
                        var propertyValue = GetSetting(target);
                        replacement = (propertyValue == "")
                            ? field     // strings that are not recognised are not replaced.
                            : propertyValue.ToLower().Replace("false", "off").Replace("true", "on");
                        break;
                }
                ParameterDictionary.Add(lowerCaseTarget, replacement);
            }

            return parameterArray[0] + replacement + parameterArray[4];
        }

        private bool ContainsParameter(string field)
        {
            if (field.Contains("{{") == false) return false;
            if (field.Contains("}}") == false) return false;
            return true;
        }

        /// <summary>
        /// Gets the property value of a UserSetting. The setting must match case as in "Setting.SimpleControlPhysics".
        /// </summary>
        /// <param name="settingText"></param>
        /// <returns></returns>
        string GetSetting(string settingText)
        {
            var nameArray = settingText.Split('.'); // 2 elements: "Settings, "<property>", e.g. "SimpleControlPhysics" 
            if (nameArray[0] == "Settings" && nameArray.Length == 2)
            {
                return Settings.GetType().GetProperty(nameArray[1])?.GetValue(Settings).ToString() ?? "";
            }
            return "";
        }

        private string GetInstalledRoutes()
        {
            var installedRouteList = "";
            var contentDictionary = Settings.Folders.Folders;
            foreach (var folder in contentDictionary)
            {
                var path = folder.Value + @"\ROUTES\";
                if (Directory.Exists(path))
                {
                    foreach (var routePath in Directory.GetDirectories(path))
                    {
                        // Extract the last folder in the path - the route folder name, e.g. "SCE"
                        var routeName = System.IO.Path.GetFileName(routePath).ToLower();
                        installedRouteList += routeName + ",";
                    }
                }
            }
            return installedRouteList;
        }

        public OverrideParameterList GetOverrideParameters()
        {
            // To support testing of a new remote notifications.json file before it is published,
            // GetNotifications tests first for a local file notifications_override_values.json
            // and uses that if present to override the current program values, else it extracts these from the program.

            var filename = @"notifications_trial_parameters.json";
            if (System.IO.File.Exists(filename))
            {
                // Input from local file into a string
                var overrideParametersSerial = System.IO.File.ReadAllText(filename);

                var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                var jsonInput = JsonConvert.DeserializeObject<OverrideParameterList>(overrideParametersSerial, jsonSettings);

                return jsonInput;
            }
            else
                return null;
        }

        public void ChangePage(int step)
        {
            CurrentNotificationNo += step;
            if (step > 0
                && CurrentNotificationNo > NewPages.Viewed   // and this is a new unviewed page
                && CurrentNotificationNo <= NewPages.Count) // and there are still new unviewed pages to be viewed
            {
                NewPages.Viewed++;
            }
            MainForm.ShowNotificationPages();
        }

        #region Logging
        public void LogOverrideParameters()
        {
            if (Log == false) return;

            if (File.Exists(LogFile)) File.Delete(LogFile);

            using (StreamWriter sw = File.CreateText(LogFile))
            {
                sw.WriteLine("Parameters overridden:");
                foreach (var p in ParameterDictionary)
                {
                    sw.WriteLine($"{p.Key} = {p.Value}");
                }
                sw.WriteLine();
            }
        }

        public void LogParameters()
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile))
            {
                sw.WriteLine("Parameters used:");
                foreach (var p in ParameterDictionary)
                {
                    sw.WriteLine($"{p.Key} = {p.Value}");
                }
                sw.WriteLine();
            }
        }

        public void LogNotification(Notification n)
        {
            AppendToLog($"\r\nNotification: {n.Title}");
        }
        public void LogChecks(string checkName)
        {
            AppendToLog($"CheckId: {checkName}");
        }
        public void LogCheckContains(string value, bool sense, string content, bool result)
        {
            var negation = sense ? "" : "NOT ";
            AppendToLog($"Check: {result} = '{value}' {negation}contained in '{content}'");
        }

        public void AppendToLog(string record)
        {
            if (Log == false) return;

            using (StreamWriter sw = File.AppendText(LogFile))
            {
                sw.WriteLine(record);
            }
        }
        #endregion
    }
}
