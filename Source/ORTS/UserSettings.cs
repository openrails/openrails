// COPYRIGHT 2012, 2013 by the Open Rails project.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ORTS
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultAttribute : Attribute
    {
        public readonly object Value;
        public DefaultAttribute(object value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DoNotSaveAttribute : Attribute
    {
    }

    /// <summary>
    /// Loads, stores and saves user configuration settings.
    /// </summary>
    /// <remarks>
    /// <para>Every public, instance property on this class is considered a setting which can be loaded and saved. They can
    /// be of types <c>string</c>, <c>int</c>, <c>bool</c>, <c>string[]</c> or <c>int[]</c>.</para>
    /// <para>Settings are saved into the registry, under a key provided to <c>UserSettings.UserSettings</c>.</para>
    /// <para>Command-line overriding of options is possible using the formats "-name=value" and "/name:value".</para>
    /// </remarks>
    public abstract class UserSettings
    {
		public enum Source
		{
			Default,
			CommandLine,
			User, // FIXME: Not registry!
		}

		protected readonly Dictionary<string, object> CustomDefaultValues = new Dictionary<string, object>();
		protected readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();

		#region User Settings

        // Please put all user settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        // Main menu settings:
        [Default(true)]
        public bool Logging { get; set; }
        [Default(false)]
        public bool FullScreen { get; set; }
        [Default("")]
        public string Multiplayer_User { get; set; }
        [Default("127.0.0.1")]
        public string Multiplayer_Host { get; set; }
        [Default(30000)]
        public int Multiplayer_Port { get; set; }

        // General settings:
        [Default(false)]
        public bool Alerter { get; set; }
        [Default(false)]
        public bool GraduatedRelease { get; set; }
        [Default(21)]
        public int BrakePipeChargingRate { get; set; }
        [Default(false)]
        public bool SuppressConfirmations { get; set; }
        [Default(false)]
        public bool ViewDispatcher { get; set; }

        // Audio settings:
        [Default(5)]
        public int SoundDetailLevel { get; set; }
        [Default(false)]
        public bool MSTSBINSound { get; set; }
		[Default(100)]
		public int SoundVolumePercent { get; set; }

        // Video settings:
        [Default(10)]
        public int WorldObjectDensity { get; set; }
        [Default("1024x768")]
        public string WindowSize { get; set; }
        [Default(false)]
        public bool TrainLights { get; set; }
        [Default(false)]
        public bool Precipitation { get; set; }
        [Default(false)]
        public bool Wire { get; set; }
        [Default(false)]
        public bool DynamicShadows { get; set; }
        [Default(false)]
        public bool WindowGlass { get; set; }
        [Default(45)] // MSTS uses 60 FOV horizontally, on 4:3 displays this is 45 FOV vertically (what OR uses).
        public int ViewingFOV { get; set; }
        [Default(0)]
        public int Cab2DStretch { get; set; }
        [Default(2000)]
        public int ViewingDistance { get; set; }

        // Simulation settings:
        [Default(true)]
        public bool UseAdvancedAdhesion { get; set; }
        [Default(10)]
        public int AdhesionMovingAverageFilterSize { get; set; }
        [Default(false)]
        public bool BreakCouplers { get; set; }
        [Default(false)]
        public bool OverrideNonElectrifiedRoutes { get; set; }

        // Experimental settings for super-elevation:
        [Default(0)]
        public int UseSuperElevation { get; set; }
        [Default(50)]
        public int SuperElevationMinLen { get; set; }
        [Default(1435)]
        public int SuperElevationGauge { get; set; }

        // Experimental settings for distant mountains:
        [Default(false)]
        public bool DistantMountains { get; set; }
        [Default(40000)]
        public int DistantMountainsViewingDistance { get; set; }

        // Experimental settings for LOD extension:
        [Default(false)]
        public bool LODViewingExtention { get; set; }

        // Experimental settings for auto-tuning performance:
        [Default(false)]
        public bool PerformanceTuner { get; set; }
        [Default(60)]
        public int PerformanceTunerTarget { get; set; }

        // Experimental settings for overhead wire:
        [Default(false)]
        public bool DoubleWire { get; set; }

        // Experimental settings for loading stuttering:
        [Default(0)]
        public int LoadingDelay { get; set; }
        
        // Data Logger settings:
        [Default("comma")]
        public string DataLoggerSeparator { set; get; }
        [Default("route")]
        public string DataLogSpeedUnits { get; set; }
        [Default(false)]
        public bool DataLogStart { get; set; }
        [Default(true)]
        public bool DataLogPerformance { get; set; }
        [Default(false)]
        public bool DataLogPhysics { get; set; }
        [Default(false)]
        public bool DataLogMisc { get; set; }

        // Hidden settings:
        [Default(0)]
        public int CarVibratingLevel { get; set; }
        [Default("OpenRailsLog.txt")]
        public string LoggingFilename { get; set; }
        [Default("")] // If left as "", OR will use the user's desktop folder
        public string LoggingPath { get; set; }
        [Default("")]
        public string ScreenshotPath { get; set; }
        [Default(0)]
        public int ShaderModel { get; set; }
        [Default(false)]
        public bool ShadowAllShapes { get; set; }
        [Default(true)]
        public bool ShadowMapBlur { get; set; }
        [Default(4)]
        public int ShadowMapCount { get; set; }
        [Default(0)]
        public int ShadowMapDistance { get; set; }
        [Default(1024)]
        public int ShadowMapResolution { get; set; }
        [Default(false)]
        public bool VerticalSync { get; set; }
        [Default(10)]
        public int Multiplayer_UpdateInterval { get; set; }
        [Default("http://openrails.org/images/support-logos.jpg")]
        public string AvatarURL { get; set; }
        [Default(false)]
        public bool ShowAvatar { get; set; }

        // Internal settings:
        [Default(false)]
        public bool DataLogger { get; set; }
        [Default(false)]
        public bool Profiling { get; set; }
        [Default(0)]
        public int ProfilingFrameCount { get; set; }
        [Default(0)]
        public int ProfilingTime { get; set; }
        [Default(0)]
        public int ReplayPauseBeforeEndS { get; set; }
        [Default(true)]
        public bool ReplayPauseBeforeEnd { get; set; }
        [Default(true)]
        public bool ShowErrorDialogs { get; set; }
        [Default(new string[0])]
        public string[] Menu_Selection { get; set; }
        [Default(false)]
        public bool Multiplayer { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_Activity { get; set; }
        [Default(new[] { 50, 0 })]
        public int[] WindowPosition_Compass { get; set; }
        [Default(new[] { 100, 100 })]
        public int[] WindowPosition_DriverAid { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_Help { get; set; }
        [Default(new[] { 0, 100 })]
        public int[] WindowPosition_NextStation { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_Quit { get; set; }
        [Default(new[] { 0, 50 })]
        public int[] WindowPosition_Switch { get; set; }
        [Default(new[] { 100, 0 })]
        public int[] WindowPosition_TrackMonitor { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_TrainOperations { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_CarOperations { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_ComposeMessage { get; set; }

        // Menu-game communication settings:
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerClient { get; set; }
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerServer { get; set; }
        
        #endregion

		protected UserSettings()
		{
			CustomDefaultValues["LoggingPath"] = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			CustomDefaultValues["ScreenshotPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.ProductName);
			CustomDefaultValues["Multiplayer_User"] = Environment.UserName;
		}

		/// <summary>
		/// Gets the default value of a specific setting.
		/// </summary>
		/// <param name="name">The name of the setting to fetch the default value of.</param>
		/// <returns>The default value of the setting.</returns>
		public object GetDefaultValue(string name)
		{
			var property = GetType().GetProperty(name);

			if (CustomDefaultValues.ContainsKey(property.Name))
				return CustomDefaultValues[property.Name];

			if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
				return (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;

			throw new InvalidDataException(String.Format("UserSetting property {0} has no default value.", property.Name));
		}

		protected abstract object GetUserValue(string name);

		protected abstract void SetUserValue(string name, string value);

		protected abstract void SetUserValue(string name, int value);

		protected abstract void SetUserValue(string name, bool value);

		protected abstract void SetUserValue(string name, string[] value);

		protected abstract void SetUserValue(string name, int[] value);

		protected abstract void DeleteUserValue(string name);

		/// <summary>
		/// Writes out all settings, their current value, and where it was loaded from to the console.
		/// </summary>
		public void Log()
		{
			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
			{
				var value = property.GetValue(this, null);
				var source = Sources[property.Name];
				if (property.PropertyType == typeof(string[]))
					Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((string[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.User ? "(registry)" : "");
				else if (property.PropertyType == typeof(int[]))
					Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.User ? "(registry)" : "");
				else
					Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source == Source.CommandLine ? "(command-line)" : source == Source.User ? "(user set)" : "");
			}
		}

		/// <summary>
		/// Saves all peristent settings.
		/// </summary>
		public void Save()
		{
			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
				Save(property);
		}

		/// <summary>
		/// Saves only a single persistent setting.
		/// </summary>
		/// <remarks>
		/// Used, eg, by Popups.Window to save their location.
		/// </remarks>
		/// <param name="name">Name of the setting to save.</param>
		public void Save(string name)
		{
			Save(GetType().GetProperty(name));
		}

		/// <summary>
		/// Merges the settings from the defaults, the saved values, and the <paramref name="options"/> passed in.
		/// </summary>
		/// <param name="options">A list of options to override the default and saved values, specified in "name=value" or "name:value" format.</param>
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

			// All public instance properties are settings. Go through them all.
			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
				Load(allowUserSettings, optionsDictionary, property);
		}

		void Load(bool allowUserSettings, Dictionary<string, string> optionsDictionary, PropertyInfo property)
		{
			// Get the default value.
			var defValue = GetDefaultValue(property.Name);

			// Read in the registry option, if it exists.
			var userValue = allowUserSettings ? GetUserValue(property.Name) : null;

			// Read in the command-line option, if it exists into optValue.
			var propertyNameLower = property.Name.ToLowerInvariant();
			var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

			// Map registry option for boolean types so 1 is true; everything else is false.
			if ((userValue != null) && (userValue is int) && (property.PropertyType == typeof(bool)))
				userValue = (int)userValue == 1;

			// Map registry option for int[] types.
			else if ((userValue != null) && (userValue is string) && (property.PropertyType == typeof(int[])))
				userValue = ((string)userValue).Split(',').Select(s => int.Parse(s)).ToArray();

			// Parse command-line option for boolean types so true/yes/on/1 are all true; everything else is false.
			if ((optValue != null) && (property.PropertyType == typeof(bool)))
				optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

			// Parse command-line option for int types.
			else if ((optValue != null) && (property.PropertyType == typeof(int)))
				optValue = int.Parse((string)optValue);

			// Parse command-line option for string[] types.
			else if ((optValue != null) && (property.PropertyType == typeof(string[])))
				optValue = ((string)optValue).Split(',').Select(s => s.Trim()).ToArray();

			// Parse command-line option for int[] types.
			else if ((optValue != null) && (property.PropertyType == typeof(int[])))
				optValue = ((string)optValue).Split(',').Select(s => int.Parse(s.Trim())).ToArray();

			// We now have defValue, regValue, optValue containing the default, persisted and override values
			// for the setting. regValue and optValue are null if they are not found/specified.
			var value = optValue != null ? optValue : userValue != null ? userValue : defValue;
			try
			{
				// int[] values must have the same number of items as default value.
				if ((property.PropertyType == typeof(int[])) && (value != null) && ((int[])value).Length != ((int[])defValue).Length)
					throw new ArgumentException();

				property.SetValue(this, value, new object[0]);
				Sources.Add(property.Name, value.Equals(defValue) ? Source.Default : optValue != null ? Source.CommandLine : userValue != null ? Source.User : Source.Default);
			}
			catch (ArgumentException)
			{
				Trace.TraceWarning("Unable to load {0} value from type {1}", property.Name, value.GetType().FullName);
				value = defValue;
				Sources.Add(property.Name, Source.Default);
			}
		}

		void Save(PropertyInfo property)
		{
			if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length > 0)
				return;

			object defValue = null;
			if (CustomDefaultValues.ContainsKey(property.Name))
				defValue = CustomDefaultValues[property.Name];
			else if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
				defValue = (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;
			else
				throw new InvalidDataException(String.Format("UserSetting property {0} has no default value.", property.Name));

			var value = property.GetValue(this, null);

			if (defValue.Equals(value)
				|| (property.PropertyType == typeof(string[]) && String.Join(",", (string[])defValue) == String.Join(",", (string[])value))
				|| (property.PropertyType == typeof(int[]) && String.Join(",", ((int[])defValue).Select(v => v.ToString()).ToArray()) == String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray())))
			{
				DeleteUserValue(property.Name);
			}
			else if (property.PropertyType == typeof(string))
			{
				SetUserValue(property.Name, (string)value);
			}
			else if (property.PropertyType == typeof(int))
			{
				SetUserValue(property.Name, (int)value);
			}
			else if (property.PropertyType == typeof(bool))
			{
				SetUserValue(property.Name, (bool)value);
			}
			else if (property.PropertyType == typeof(string[]))
			{
				SetUserValue(property.Name, (string[])value);
			}
			else if (property.PropertyType == typeof(int[]))
			{
				SetUserValue(property.Name, (int[])value);
			}
		}

		/// <summary>
		/// Initializes a new instance of a subclass of <c>UserSettings</c> with a given <paramref name="registryKey"/> and <paramref name="filePath"/> and list of <paramref name="options"/> overrides.
		/// </summary>
		/// <param name="registryKey">Registry key under <c>HKEY_CURRENT_USER</c> to load and save the settings.</param>
		/// <param name="filePath">File path from which to load and save the settings.</param>
		/// <param name="options">List of all the setting overrides from the command-line.</param>
		public static UserSettings GetSettings(string registryKey, string filePath, IEnumerable<string> options)
		{
			if (File.Exists(filePath))
				return new UserSettingsLocalIni(filePath, options);
			return new UserSettingsRegistry(registryKey, options);
		}
	}

	public class UserSettingsRegistry : UserSettings
	{
        readonly string RegistryKey;
		readonly RegistryKey Key;

		internal UserSettingsRegistry(string registryKey, IEnumerable<string> options)
        {
            RegistryKey = registryKey;
			Key = Registry.CurrentUser.CreateSubKey(RegistryKey);
			Load(options);
        }

		protected override object GetUserValue(string name)
		{
			return Key.GetValue(name);
		}

		protected override void DeleteUserValue(string name)
		{
			Key.DeleteValue(name, false);
		}

		protected override void SetUserValue(string name, string value)
		{
			Key.SetValue(name, value, RegistryValueKind.String);
		}

		protected override void SetUserValue(string name, int value)
		{
			Key.SetValue(name, value, RegistryValueKind.DWord);
		}

		protected override void SetUserValue(string name, bool value)
		{
			Key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
		}

		protected override void SetUserValue(string name, string[] value)
		{
			Key.SetValue(name, value, RegistryValueKind.MultiString);
		}

		protected override void SetUserValue(string name, int[] value)
		{
			Key.SetValue(name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
		}
    }

	public class UserSettingsLocalIni : UserSettings
	{
		const string SectionName = "ORTS";

		readonly string FilePath;

		internal UserSettingsLocalIni(string filePath, IEnumerable<string> options)
		{
			FilePath = filePath;
			Load(options);
		}

		protected override object GetUserValue(string name)
		{
			var buffer = new String('\0', 256);
			var length = NativeMethods.GetPrivateProfileString(SectionName, name, null, buffer, buffer.Length, FilePath);
			if (length == 0)
				return null;
			buffer = buffer.Substring(0, length);
			var value = buffer.Split(':');
			if (value.Length < 2)
			{
				Trace.TraceWarning("Setting {0} contains invalid value {1}", name, buffer);
				return null;
			}
			var valueFull = String.Join(":", value.Skip(1).ToArray());
			switch (value[0])
			{
				case "string":
					return valueFull;
				case "int":
					return int.Parse(valueFull, CultureInfo.InvariantCulture);
				case "bool":
					return valueFull.Equals("true", StringComparison.InvariantCultureIgnoreCase);
				case "string[]":
					return valueFull.Split(',');
				case "int[]":
					return valueFull.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
				default:
					Trace.TraceWarning("Setting {0} contains invalid value {1}", name, buffer);
					return null;
			}
		}

		protected override void DeleteUserValue(string name)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, null, FilePath);
		}

		protected override void SetUserValue(string name, string value)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, "string:" + value, FilePath);
		}

		protected override void SetUserValue(string name, int value)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, "int:" + value.ToString("R", CultureInfo.InvariantCulture), FilePath);
		}

		protected override void SetUserValue(string name, bool value)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, "bool:" + (value ? "true" : "false"), FilePath);
		}

		protected override void SetUserValue(string name, string[] value)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, "string[]:" + String.Join(",", value), FilePath);
		}

		protected override void SetUserValue(string name, int[] value)
		{
			NativeMethods.WritePrivateProfileString(SectionName, name, "int[]:" + String.Join(",", ((int[])value).Select(v => v.ToString("R", CultureInfo.InvariantCulture)).ToArray()), FilePath);
		}
	}

	internal class NativeMethods
	{
		[DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GetPrivateProfileString(string sectionName, string keyName, string defaultValue, string value, int size, string fileName);

		[DllImport("KERNEL32.DLL", EntryPoint = "WritePrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int WritePrivateProfileString(string sectionName, string keyName, string value, string fileName);
	}
}
