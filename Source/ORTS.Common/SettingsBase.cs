// COPYRIGHT 2013 by the Open Rails project.
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
using System.Diagnostics;
using System.Linq;

namespace ORTS.Common
{
	public abstract class SettingsBase
	{
		public enum Source
		{
			Default,
			CommandLine,
			User,
		}

		protected readonly SettingStore SettingStore;
		protected readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();

		public SettingsBase(SettingStore settings)
		{
			SettingStore = settings;
		}

		public abstract object GetDefaultValue(string name);
		protected abstract object GetValue(string name);
		protected abstract void SetValue(string name, object value);
		protected abstract void Load(bool allowUserSettings, Dictionary<string, string> optionsDictionary);
		public abstract void Save();
		public abstract void Save(string name);
        public abstract void Reset();

		protected void Load(IEnumerable<string> options)
		{
			// This special command-line option prevents the registry values from being used.
			var allowUserSettings = !options.Contains("skip-user-settings", StringComparer.OrdinalIgnoreCase);

			// Pull apart the command-line options so we can find them by setting name.
			var optionsDictionary = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var k = option.Split(new[] { '=', ':' }, 2)[0].ToLowerInvariant();
				var v = option.Contains('=') || option.Contains(':') ? option.Split(new[] { '=', ':' }, 2)[1].ToLowerInvariant() : "yes";
				optionsDictionary[k] = v;
			}

			Load(allowUserSettings, optionsDictionary);
		}

		protected void Load(bool allowUserSettings, Dictionary<string, string> optionsDictionary, string name, Type type)
		{
			// Get the default value.
			var defValue = GetDefaultValue(name);

			// Read in the registry option, if it exists.
			var userValue = allowUserSettings ? SettingStore.GetUserValue(name) : null;

			// Read in the command-line option, if it exists into optValue.
			var propertyNameLower = name.ToLowerInvariant();
			var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

			// Map registry option for boolean types so 1 is true; everything else is false.
			if ((userValue != null) && (userValue is int) && (type == typeof(bool)))
				userValue = (int)userValue == 1;

			// Map registry option for int[] types.
			else if ((userValue != null) && (userValue is string) && (type == typeof(int[])))
				userValue = ((string)userValue).Split(',').Select(s => int.Parse(s)).ToArray();

			// Parse command-line option for boolean types so true/yes/on/1 are all true; everything else is false.
			if ((optValue != null) && (type == typeof(bool)))
				optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

			// Parse command-line option for int types.
			else if ((optValue != null) && (type == typeof(int)))
				optValue = int.Parse((string)optValue);

			// Parse command-line option for string[] types.
			else if ((optValue != null) && (type == typeof(string[])))
				optValue = ((string)optValue).Split(',').Select(s => s.Trim()).ToArray();

			// Parse command-line option for int[] types.
			else if ((optValue != null) && (type == typeof(int[])))
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
			else if (type == typeof(bool))
			{
				SettingStore.SetUserValue(name, (bool)value);
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

        protected void Reset(string name)
        {
            SetValue(name, GetDefaultValue(name));
            SettingStore.DeleteUserValue(name);
        }
	}
}
