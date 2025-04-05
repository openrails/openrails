// COPYRIGHT 2014 by the Open Rails project.
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
using System.Globalization;
using System.Linq;
using System.Text;
using ORTS.Common;

namespace ORTS.Settings
{
    public class FolderSettings : SettingsBase
    {
        public static readonly string SectionName = "Folders";

        public readonly Dictionary<string, string> Folders;

        public FolderSettings(IEnumerable<string> options)
            : base(SettingsStore.GetSettingStore(SettingsBase.SettingsFilePath, SettingsBase.RegistryKey, SectionName))
        {
            Folders = new Dictionary<string, string>();
            Load(options);
        }

        public override object GetDefaultValue(string name)
        {
            return "";
        }

        protected override object GetValue(string name)
        {
            return Folders[name];
        }

        protected override void SetValue(string name, object value)
        {
            if ((string)value != "")
                Folders[name] = (string)value;
            else if (Folders.ContainsKey(name))
                Folders.Remove(name);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var name in SettingStore.GetUserNames())
                Load(optionsDictionary, name, typeof(string));
        }

        public override void Save()
        {
            foreach (var name in SettingStore.GetUserNames())
                if (!Folders.ContainsKey(name))
                    Reset(name);
            foreach (var name in Folders.Keys)
                Save(name);
        }

        public override void Save(string name)
        {
            Save(name, typeof(string));
        }

        public override void Reset()
        {
            foreach (var name in Folders.Keys)
                Reset(name);
        }
    }
}
