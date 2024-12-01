// COPYRIGHT 2013, 2014 by the Open Rails project.
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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ORTS.Common
{
    /// <summary>
    /// Base class for supporting settings (either from user, commandline, default, ...)
    /// </summary>
	public abstract class SettingsBase
	{
        public static string RegistryKey { get; protected set; }        // ie @"SOFTWARE\OpenRails\ORTS"
        public static string SettingsFilePath { get; protected set; }   // ie @"C:\Program Files\Open Rails\OpenRails.ini"

        static SettingsBase()
        {
            // Only one of these is allowed; if the INI file exists, we use that, otherwise we use the registry.
            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";
            SettingsFilePath = Path.Combine(ApplicationInfo.ProcessDirectory, "OpenRails.ini");
            if (File.Exists(SettingsFilePath))
                RegistryKey = null;
            else
                SettingsFilePath = null;
        }

        /// <summary>
        /// Enumeration of the various sources for settings
        /// </summary>
		protected enum Source
		{
            /// <summary>Setting is a default setting</summary>
			Default,
            /// <summary>Setting comes from the command line</summary>
            CommandLine,
            /// <summary>Setting comes from user (so stored between runs)</summary>
            User,
		}

        /// <summary>The store of the settings</summary>
        protected SettingsStore SettingStore { get; private set; }

        /// <summary>Translates name of a setting to its source</summary>
        protected readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();

        /// <summary>
        /// True when the user settings store is in use and will be read from and written to.
        /// </summary>
        protected bool AllowUserSettings { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">The store for the settings</param>
		protected SettingsBase(SettingsStore settings)
		{
			SettingStore = settings;
		}

        /// <summary>
        /// Get the default value of a setting
        /// </summary>
        /// <param name="name">The name of the setting</param>
		public abstract object GetDefaultValue(string name);

        /// <summary>
        /// Get the current value of a setting
        /// </summary>
        /// <param name="name">The name of the setting</param>
		protected abstract object GetValue(string name);

        /// <summary>
        /// set the current value of a setting
        /// </summary>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value of the setting</param>
		protected abstract void SetValue(string name, object value);

        /// <summary>
        /// Load all settings, possibly partly from the given options
        /// </summary>
        /// <param name="optionsDictionary">???</param>
		protected abstract void Load(Dictionary<string, string> optionsDictionary);

        /// <summary>
        /// Save all settings to the store
        /// </summary>
		public abstract void Save();

        /// <summary>
        /// Save a setting to the store. Since type is not known, this is abstract.
        /// </summary>
        /// <param name="name">name of the setting</param>
		public abstract void Save(string name);

        /// <summary>
        /// Reset all values to their default
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Load settings from the options
        /// </summary>
        /// <param name="options">???</param>
		protected void Load(IEnumerable<string> options)
		{
			// This special command-line option prevents the registry values from being used.
			AllowUserSettings = !options.Contains("skip-user-settings", StringComparer.OrdinalIgnoreCase);

			// Pull apart the command-line options so we can find them by setting name.
			var optionsDictionary = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var k = option.Split(new[] { '=', ':' }, 2)[0].ToLowerInvariant();
				var v = option.Contains('=') || option.Contains(':') ? option.Split(new[] { '=', ':' }, 2)[1].ToLowerInvariant() : "yes";
				optionsDictionary[k] = v;
			}

			Load(optionsDictionary);
		}

        /// <summary>
        /// Load a single value from the store, once type of the setting is known
        /// </summary>
        /// <param name="optionsDictionary">???</param>
        /// <param name="name">name of the setting</param>
        /// <param name="type">type of the setting</param>
		protected void Load(Dictionary<string, string> optionsDictionary, string name, Type type)
		{
			// Get the default value.
			var defValue = GetDefaultValue(name);

			// Read in the user setting, if it exists.
			var userValue = AllowUserSettings ? SettingStore.GetUserValue(name, type) : null;

			// Read in the command-line option, if it exists into optValue.
			var propertyNameLower = name.ToLowerInvariant();
			var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

            // Parse command-line options...

			if ((optValue != null) && (type == typeof(bool)))
                // Option for boolean types so true/yes/on/1 are all true; everything else is false.
                optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

			else if ((optValue != null) && (type == typeof(int)))
                // Option for int types.
                optValue = int.Parse((string)optValue);

			else if ((optValue != null) && (type == typeof(string[])))
                // Option for string[] types.
                optValue = ((string)optValue).Split(',').Select(s => s.Trim()).ToArray();

			else if ((optValue != null) && (type == typeof(int[])))
                // Option for int[] types.
                optValue = ((string)optValue).Split(',').Select(s => int.Parse(s.Trim())).ToArray();

			// We now have defValue, regValue, optValue containing the default, persisted and override values
			// for the setting. regValue and optValue are null if they are not found/specified.
			var value = optValue != null ? optValue : userValue != null ? userValue : defValue;
			try
			{
				// int[] values must have the same number of items as default value.
				if ((type == typeof(int[])) && (value != null) && ((int[])value).Length != ((int[])defValue).Length)
					throw new ArgumentException();

				SetValue(name, value);
				Sources.Add(name, value.Equals(defValue) ? Source.Default : optValue != null ? Source.CommandLine : userValue != null ? Source.User : Source.Default);
			}
			catch (ArgumentException)
			{
				Trace.TraceWarning("Unable to load {0} value from type {1}", name, value.GetType().FullName);
				value = defValue;
				Sources.Add(name, Source.Default);
			}
		}

        /// <summary>
        /// Save a setting to the store, if name and especially type are known
        /// </summary>
        /// <param name="name">name of the setting</param>
        /// <param name="type">type of the setting</param>
		protected void Save(string name, Type type)
		{
			var defValue = GetDefaultValue(name);
			var value = GetValue(name);

			if (defValue.Equals(value)
				|| (type == typeof(string[]) && String.Join(",", (string[])defValue) == String.Join(",", (string[])value))
				|| (type == typeof(int[]) && String.Join(",", ((int[])defValue).Select(v => v.ToString()).ToArray()) == String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray())))
			{
				SettingStore.DeleteUserValue(name);
			}
			else if (type == typeof(string))
			{
				SettingStore.SetUserValue(name, (string)value);
			}
			else if (type == typeof(int))
			{
				SettingStore.SetUserValue(name, (int)value);
			}
            else if (type == typeof(byte))
            {
                SettingStore.SetUserValue(name, (byte)value);
            }
            else if (type == typeof(bool))
            {
                SettingStore.SetUserValue(name, (bool)value);
            }
            else if (type == typeof(DateTime))
            {
                SettingStore.SetUserValue(name, (DateTime)value);
            }
            else if (type == typeof(TimeSpan))
            {
                SettingStore.SetUserValue(name, (TimeSpan)value);
            }
            else if (type == typeof(string[]))
			{
				SettingStore.SetUserValue(name, (string[])value);
			}
			else if (type == typeof(int[]))
			{
				SettingStore.SetUserValue(name, (int[])value);
			}
		}

        /// <summary>
        /// Reset a single setting to its default
        /// </summary>
        /// <param name="name">name of the setting</param>
        protected void Reset(string name)
        {
            SetValue(name, GetDefaultValue(name));
            SettingStore.DeleteUserValue(name);
        }
	}
}
