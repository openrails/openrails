// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015, 2023 by the Open Rails project.
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
using Newtonsoft.Json;
using ORTS.Common;
using static ORTS.Settings.ContentRouteSettings;

namespace ORTS.Settings
{
    public class ContentSettings : SettingsBase
    {
        #region User Settings
        public ContentRouteSettings ContentRouteSettings;
        #endregion

        public ContentSettings(IEnumerable<string> options)
        : base(SettingsStore.GetSettingStore(UserSettings.SettingsFilePath, UserSettings.RegistryKey, "ContentRoutes"))
        {
            ContentRouteSettings = new ContentRouteSettings();
            Load(options);
        }

        public override object GetDefaultValue(string name)
        {
            return "";
        }

        protected override object GetValue(string name)
        {
            return JsonConvert.SerializeObject(ContentRouteSettings.Routes[name], Formatting.Indented);
        }

        protected override void SetValue(string name, object value)
        {
            ContentRouteSettings.Routes[name] = JsonConvert.DeserializeObject<Route>((string)value);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var name in SettingStore.GetUserNames())
            {
                Load(optionsDictionary, name, typeof(string));
            }
        }

        public override void Save()
        {
            foreach (var name in ContentRouteSettings.Routes.Keys)
            {
                if (ContentRouteSettings.Routes.ContainsKey(name))
                {
                    if (ContentRouteSettings.Routes[name].Installed)
                    {
                        Save(name);
                    }
                }
            }

            foreach (var name in SettingStore.GetUserNames())
            {
                if (!ContentRouteSettings.Routes.ContainsKey(name) ||
                    (!ContentRouteSettings.Routes[name].Installed))
                {
                    var route = ContentRouteSettings.Routes[name];
                     // remove from registry
                    Reset(name);
                    // ContentRouteSettings.Routes.Add(name, route);
                }

            }

        }

        public override void Save(string name)
        {
            Save(name, typeof(string));
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
