// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace ORTS
{
	public class UserSettings
	{
		readonly string RegistryKey;

		#region User Settings

		// Please put all user settings in here as auto-properties. Public properties
		// of type 'string', 'int' and 'bool' are automatically loaded/saved.

		public int BrakePipeChargingRate { get; set; }
		public bool DataLogger { get; set; }
		public bool DynamicShadows { get; set; }
		public bool FullScreen { get; set; }
		public bool GraduatedRelease { get; set; }
		public bool MSTSBINSound { get; set; }
		public bool Precipitation { get; set; }
		public bool Profiling { get; set; }
        public int ProfilingFrameCount { get; set; }
        public int ShaderModel { get; set; }
		public bool ShadowAllShapes { get; set; }
		public bool ShadowMapBlur { get; set; }
		public int ShadowMapCount { get; set; }
		public int ShadowMapDistance { get; set; }
		public int ShadowMapResolution { get; set; }
        public bool ShowErrorDialogs { get; set; }
		public int SoundDetailLevel { get; set; }
		public bool TrainLights { get; set; }
		public bool VerticalSync { get; set; }
		public int ViewingDistance { get; set; }
		public bool WindowGlass { get; set; }
		public string WindowSize { get; set; }
		public bool Wire { get; set; }
		public int WorldObjectDensity { get; set; }

		#endregion

		public UserSettings(string registryKey, IEnumerable<string> options)
		{
			RegistryKey = registryKey;
			InitUserSettings();
			LoadUserSettings(options);
		}

		void InitUserSettings()
		{
			// Initialize defaults for all user settings here.
			BrakePipeChargingRate = 21;
            ProfilingFrameCount = 1000;
			ShadowMapBlur = true;
			ShadowMapCount = 4;
			ShadowMapResolution = 1024;
            ShowErrorDialogs = true;
			SoundDetailLevel = 5;
			ViewingDistance = 2000;
			WindowSize = "1024x768";
			WorldObjectDensity = 10;
		}

		void LoadUserSettings(IEnumerable<string> options)
		{
			// This special command-line option prevents the registry values from being used.
			var allowRegistryValues = !options.Contains("skip-user-settings", StringComparer.OrdinalIgnoreCase);
			// Pull apart the command-line options so we can find them by setting name.
			var optionsDictionary = new Dictionary<string, string>();
			foreach (var option in options)
			{
				var k = option.Split(new[] { '=', ':' }, 2)[0].ToLowerInvariant();
				var v = option.Contains('=') || option.Contains(':') ? option.Split(new[] { '=', ':' }, 2)[1].ToLowerInvariant() : "yes";
				optionsDictionary[k] = v;
			}

			RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey);
			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
			{
				// Get the default value.
				var defValue = property.GetValue(this, new object[0]);
				// Read in the registry option, if it exists.
				var regValue = allowRegistryValues && RK != null ? RK.GetValue(property.Name, null) : null;
				// Read in the command-line option, if it exists.
				var propertyNameLower = property.Name.ToLowerInvariant();
				var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

				// Map registry option for boolean types so 1 is true; everything else is false.
				if ((regValue != null) && (regValue is int) && (property.PropertyType == typeof(bool)))
					regValue = (int)regValue == 1;

				// Map command-line option for boolean types so true/yes/on/1 are all true; everything else is false.
				if ((optValue != null) && (property.PropertyType == typeof(bool)))
					optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

                // Parse command-line option for int types.
                else if ((optValue != null) && (property.PropertyType == typeof(int)))
                    optValue = int.Parse((string)optValue);

                var value = optValue != null ? optValue : regValue != null ? regValue : defValue;
				try
				{
					property.SetValue(this, value, new object[0]);
				}
				catch (ArgumentException)
				{
					Trace.TraceWarning("Unable to load {0} value from type {1}.", property.Name, value.GetType().FullName);
					value = defValue;
				}

				// Need to use object.Equals(object) here because values are boxed.
				Console.WriteLine("{0,-25} = {1,-10} {2}", property.Name, value, value.Equals(defValue) ? "" : optValue != null ? "(command-line)" : regValue != null ? "(registry)" : "");
			}
			if (RK != null)
				RK.Close();
		}
	}
}
