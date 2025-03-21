// COPYRIGHT 2009 - 2024 by the Open Rails project.
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
using ORTS.Common;

namespace ORTS.Settings
{
    public class TelemetrySettings : PropertySettingsBase
    {
        public static readonly string SectionName = "Telemetry";

        [Default(0)]
        public int RandomNumber1000 { get; set; }
        [Default("https://telemetry.openrails.org")]
        public string ServerURL { get; set; }
        public DateTime StateSystem { get; set; }

        public TelemetrySettings()
            : base(SettingsStore.GetSettingStore(UserSettings.SettingsFilePath, UserSettings.RegistryKey, SectionName))
        {
            Load(new string[0]);
        }

        public override object GetDefaultValue(string name)
        {
            if (name.StartsWith("State")) return TelemetryManager.StateUndecided;
            return base.GetDefaultValue(name);
        }
    }
}
