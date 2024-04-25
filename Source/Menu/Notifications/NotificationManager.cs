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
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Updater;
using static ORTS.Common.SystemInfo;
using static ORTS.NotificationPage;

namespace ORTS
{
    class NotificationManager
    {
        public bool ArePagesVisible = false;
        // New notifications are those with a date after the NotificationsReadDate.
        // Notifications are listed in reverse date order, with the newest one at the front.
        // We don't track the reading of each notification but set the NewNotificationCount = 0 after the last of the new ones has been read.
        public int NewPageCount = 1;
        public int LastPageViewed = 0;
        public int Index = 0;

        public Notifications Notifications;
        public List<NotificationPage> PageList = new List<NotificationPage>();
        private Exception Error;

        private MainForm MainForm; // Needed so we can add controls to the NotificationPage
        private UpdateManager UpdateManager;
        private UserSettings Settings;

        public NotificationManager(MainForm mainForm, UpdateManager updateManager, UserSettings settings) 
        { 
            MainForm = mainForm;
            this.UpdateManager = updateManager;
            this.Settings = settings;
        }

        // Make this a background task
        public void CheckNotifications()
        {
            try
            {
                Error = null;
                Notifications = GetNotifications();
                DropUnusedUpdateNotifications();
                ReplaceParameters();

                PopulatePageList();
                ArePagesVisible = false;
            }
            catch (WebException ex)
            {
                Error = ex;
            }
        }

        public Notifications GetNotifications()
        {
            String notificationsSerial;
            // To support testing of a new menu.json file, GetNotifications tests for a local file test.json first and uses that if present.

            var filename = @"menu_test.json";
            if (System.IO.File.Exists(filename))
            {
                // Input from local file into a string
                notificationsSerial = System.IO.File.ReadAllText(filename);
            }
            else
            {
                // Input from remote file into a string
                notificationsSerial = GetRemoteJson();
            }

            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jsonInput = JsonConvert.DeserializeObject<Notifications>(notificationsSerial, jsonSettings);

            return jsonInput;
        }

        /// <summary>
        /// Fetch the Notifications from     https://static.openrails.org/api/notifications/menu.json 
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

            return client.DownloadString(new Uri("https://wepp.co.uk/openrails/notifications/menu.json"));
        }

        public void PopulatePageList()
        {
            SetUpdateNotificationPage();

            var NotificationPage = PageList.LastOrDefault();
            new NTextControl(NotificationPage, "").Add();
            new NTextControl(NotificationPage, "(Toggle icon to hide NotificationPages.)").Add();
        }

        /// <summary>
        /// Ultimately there will be a list of notifications downloaded from     https://static.openrails.org/api/notifications/menu.json .
        /// Until then, there is a single notification announcing either that a new update is available or the installation is up to date.
        /// </summary>
        void SetUpdateNotificationPage()
        {
            MainForm.UpdateNotificationPageAlert();
            PageList.Clear();
            var page = MainForm.CreateNotificationPage(Notifications);

            if (UpdateManager.LastCheckError != null || Error != null)
            {
                NewPageCount = 0;
                var message = (UpdateManager.LastCheckError != null)
                    ? UpdateManager.LastCheckError.Message
                    : Error.Message;

                // Reports notifications are not available.
                var channelName = UpdateManager.ChannelName == "" ? "None" : UpdateManager.ChannelName;
                var today = DateTime.Now.Date;
                new NTitleControl(page, 1, 1, $"{today:dd-MMM-yy}", "Notifications are not available").Add();
                new NRecordControl(page, "Update mode", 140, channelName).Add();
                new NRecordControl(page, "Installed version", 140, VersionInfo.VersionOrBuild).Add();

                new NHeadingControl(page, "Notifications are not available", "red").Add();
                new NTextControl(page, $"Error: {message}").Add();
                new NTextControl(page, "Is your Internet connected?").Add();

                new NRetryControl(page, "Retry", 140, "Try again to fetch notifications", MainForm).Add();
                PageList.Add(page);
                return;
            }

            NewPageCount = 1;
            var list = Notifications.NotificationList;
            var n = list[Index];

            new NTitleControl(page, Index + 1, list.Count, n.Date, n.Title).Add();

            foreach (var item in n.PrefixItemList)
            {
                AddItemToPage(page, item);
            }

            // Check constraints if there is a MetList.
            // If a check fails then its UnmetList is added to the page, otherwise the MetList is added.
            if (n.MetLists != null || n.MetLists.ItemList.Count == 0)
            {
                CheckConstraints(page, n);
            }            

            foreach (var item in n.SuffixItemList)
            {
                AddItemToPage(page, item);
            }
            PageList.Add(page);
        }

        /// <summary>
        /// CheckConstraints() implements ExcludesAnyOf() AND ExcludesAllOf() AND IncludesAnyOf() AND IncludesAllOf(), but all parts are optional.
        /// Consider first the Excludes:
        ///   ExcludesAnyOf() Unmet if A == a OR B == b OR ...
        ///   ExcludesAllOf() Unmet if A == a AND B == b AND ...
        /// and then the Includes:
        ///   IncludesAnyOf() Met if D == d OR E == e OR ...
        ///   IncludesAllOf() Met if D == d AND E == e OR ...
        /// </summary>
        /// <param name="page"></param>
        /// <param name="n"></param>
        private void CheckConstraints(NotificationPage page, Notification n)
        {
            foreach (var nc in n.MetLists.CheckIdList)
            {
                // Find the matching check
                var c = Notifications.CheckList.Where(check => check.Id == nc.Id).FirstOrDefault();
                if (c != null)
                {
                    // Check the Excludes constraints first
                    if (c.ExcludesAllOf != null)    // ExcludesAllOf is optional
                    {
                        var checkFailed = CheckMissingMatch(c, c.ExcludesAllOf);
                        if (checkFailed != null)
                        {
                            foreach (var item in checkFailed.UnmetItemList)
                            {
                                AddItemToPage(page, item);
                            }
                            return;
                        }
                    }
                    if ( c.ExcludesAnyOf?.Count > 0)    // ExcludesAnyOf is optional
                    {
                        if (CheckAnyMatch(c, c.ExcludesAnyOf) != null)
                        {
                            foreach (var item in c.UnmetItemList)
                            {
                                AddItemToPage(page, item);
                            }
                            return;
                        }
                    }

                    // Check the Includes constraints last
                    if (c.IncludesAllOf?.Count > 0)    // IncludesAllOf is optional
                    {
                        var checkFailed = CheckMissingMatch(c, c.IncludesAllOf);
                        if (checkFailed != null)
                        {
                            foreach (var item in checkFailed.UnmetItemList)
                            {
                                AddItemToPage(page, item);
                            }
                            return;
                        }
                    }
                    if (c.IncludesAnyOf?.Count > 0) // IncludesAnyOf is optional
                    {
                        if (CheckAnyMatch(c, c.IncludesAnyOf) == null)
                        {
                            foreach (var item in c.UnmetItemList)
                            {
                                AddItemToPage(page, item);
                            }
                            return;
                        }
                    }
                }
            }
            foreach (var item in n.MetLists.ItemList)
            {
                AddItemToPage(page, item);
            }
        }

        private void AddItemToPage(NotificationPage page, Item item)
        {
            if (item is Record record)
            {
                new NRecordControl(page, item.Label, item.Indent, record.Value).Add();
            }
            else if (item is Link link)
            {
                new NLinkControl(page, item.Label, item.Indent, link.Value, MainForm, link.Url).Add();
            }
            else if (item is Update update)
            {
                new NUpdateControl(page, item.Label, item.Indent, update.Value, MainForm).Add();
            }
            else if (item is Heading heading)
            {
                new NHeadingControl(page, item.Label, heading.Color).Add();
            }
            else if (item is Item item2)
            {
                new NTextControl(page, item.Label).Add();
            }
        }

        /// <summary>
        /// Returns null or the first check that matches.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        private Check CheckAnyMatch(Check check, List<Criteria> criteriaList)
        {
            foreach (var c in criteriaList)
            {
                if (c is Contains) // Other criteria might be added such as NoLessThan and NoMoreThan.
                {
                    var matchedCheck = CheckContains(check, c);
                    if (matchedCheck != null)
                        return matchedCheck;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns null or the first check that does not match.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        private Check CheckMissingMatch(Check check, List<Criteria> criteriaList)
        {
            foreach (var c in criteriaList)
            {
                if (c is Contains) // Other criteria might be added such as NoLessThan and NoMoreThan.
                {
                    if (CheckContains(check, c) == null)
                    {
                        return check;
                    }
                }
            }
            return null;
        }

        private Check CheckContains(Check check, Criteria c)
        {
            switch (c.Name)
            {
                case "direct3d":
                    if (SystemInfo.Direct3DFeatureLevels.Contains(c.Value, StringComparer.OrdinalIgnoreCase))
                        return check;
                    break;
                case "installed_version":
                    if (SystemInfo.Application.Version.IndexOf(c.Value, StringComparison.OrdinalIgnoreCase) > -1)
                        return check;
                    break;
                case "system":
                    var os = SystemInfo.OperatingSystem;
                    var system = $"{os.Name} {os.Version} {os.Architecture} {os.Language} {os.Languages ?? new string[0]}";
                    if (system.IndexOf(c.Value, StringComparison.OrdinalIgnoreCase) > -1)
                        return check;
                    break;
                default:
                    if (GetSetting(c.Name).IndexOf(c.Value, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        return check;
                    }
                    if (c.Name.IndexOf(c.Value, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        return check;
                    }
                    return null;    // Any check that is not recognised is skipped.
            }
            return null;
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


        /// <summary>
        /// Drop any notifications for the channel not selected
        /// </summary>
        /// <param name="updateManager"></param>
        public void DropUnusedUpdateNotifications()
        {
            var updateModeSetting = UpdateManager.ChannelName.ToLower();
            foreach (var n in Notifications.NotificationList)
            {
                if (n.UpdateMode == null)   // Skip notifications which are not updates
                    continue;

                var lowerUpdateMode = n.UpdateMode.ToLower();

                // If setting == "none", then keep just one update notification, e.g. the stable one
                if (updateModeSetting == "" && lowerUpdateMode == "stable")
                    continue;

                // Mark unused updates for deletion outside loop
                n.ToDelete = lowerUpdateMode != updateModeSetting;
            }
            Notifications.NotificationList.RemoveAll(n => n.ToDelete);
        }

        public void ReplaceParameters()
        {
            foreach (var n in Notifications.NotificationList)
            {
                n.Title = ReplaceParameter(n.Title);
                n.Date = ReplaceParameter(n.Date);
                n.PrefixItemList?.ForEach(item => ReplaceItemParameter(item));
                n.MetLists?.ItemList?.ForEach(item => ReplaceItemParameter(item));
                n.SuffixItemList?.ForEach(item => ReplaceItemParameter(item));
            }
            foreach (var c in Notifications.CheckList)
            {
                c.ExcludesAllOf?.ForEach(criteria => ReplaceCriteriaParameter(criteria));
                c.IncludesAllOf?.ForEach(criteria => ReplaceCriteriaParameter(criteria));
                c.ExcludesAnyOf?.ForEach(criteria => ReplaceCriteriaParameter(criteria));
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
            if (field.Contains("}}") == false)
                return field;

            var parameterArray = field.Split('{', '}'); // 5 elements: prefix, "", target, "", suffix
            var target = parameterArray[2].ToLower();
            var replacement = parameterArray[2]; // Default is original text

            switch (target)
            {
                case "update_mode":
                    replacement = UpdateManager.ChannelName == ""
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
                        : $"{UpdateManager.LastUpdate.Date:dd-MMM-yy}";
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
                default:
                    var propertyValue = GetSetting(field);
                    replacement = (propertyValue == "") 
                        ? field     // strings that are not recognised are not replaced.
                        : propertyValue;
                    break;
            }

            return parameterArray[0] + replacement + parameterArray[4];
        }
    }
}
