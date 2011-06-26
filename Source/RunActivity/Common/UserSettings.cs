// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace ORTS
{
	public class UserSettings
	{
		readonly string RegistryKey;
        readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();

        enum Source
        {
            Default,
            CommandLine,
            Registry,
        }

		#region User Settings

		// Please put all user settings in here as auto-properties. Public properties
		// of type 'string', 'int' and 'bool' are automatically loaded/saved.

		public int BrakePipeChargingRate { get; set; }
		public bool DataLogger { get; set; }
		public bool DynamicShadows { get; set; }
		public bool FullScreen { get; set; }
		public bool GraduatedRelease { get; set; }
        public string LoggingFilename { get; set; }
        public string LoggingPath { get; set; }
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

        public int[] WindowPosition_Compass { get; set; }
        public int[] WindowPosition_DriverAid { get; set; }
        public int[] WindowPosition_Help { get; set; }
        public int[] WindowPosition_NextStation { get; set; }
        public int[] WindowPosition_Switch { get; set; }
        public int[] WindowPosition_TrackMonitor { get; set; }
        public int[] WindowPosition_TrainOperations { get; set; }

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
            LoggingPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            LoggingFilename = "OpenRailsLog.txt";
            ProfilingFrameCount = 1000;
            ShadowMapBlur = true;
            ShadowMapCount = 4;
            ShadowMapResolution = 1024;
            ShowErrorDialogs = true;
            SoundDetailLevel = 5;
            ViewingDistance = 2000;
            WindowSize = "1024x768";
            WorldObjectDensity = 10;

            WindowPosition_Compass = new[] { 50, 0 };
            WindowPosition_DriverAid = new[] { 100, 100 };
            WindowPosition_Help = new[] { 50, 50 };
            WindowPosition_NextStation = new[] { 0, 100 };
            WindowPosition_Switch = new[] { 0, 50 };
            WindowPosition_TrackMonitor = new[] { 100, 0 };
            WindowPosition_TrainOperations = new[] { 50, 50 };
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
				var defValue = property.GetValue(this, null);
				// Read in the registry option, if it exists.
				var regValue = allowRegistryValues && RK != null ? RK.GetValue(property.Name, null) : null;
				// Read in the command-line option, if it exists.
				var propertyNameLower = property.Name.ToLowerInvariant();
				var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

				// Map registry option for boolean types so 1 is true; everything else is false.
				if ((regValue != null) && (regValue is int) && (property.PropertyType == typeof(bool)))
					regValue = (int)regValue == 1;

                // Map registry option for int[] types.
                else if ((regValue != null) && (regValue is string) && (property.PropertyType == typeof(int[])))
                    regValue = ((string)regValue).Split(',').Select(s => int.Parse(s)).ToArray();

                // Parse command-line option for boolean types so true/yes/on/1 are all true; everything else is false.
				if ((optValue != null) && (property.PropertyType == typeof(bool)))
					optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

                // Parse command-line option for int types.
                else if ((optValue != null) && (property.PropertyType == typeof(int)))
                    optValue = int.Parse((string)optValue);

                // Parse command-line option for int[] types.
                else if ((optValue != null) && (property.PropertyType == typeof(int[])))
                    optValue = ((string)optValue).Split(',').Select(s => int.Parse(s.Trim())).ToArray();

                var value = optValue != null ? optValue : regValue != null ? regValue : defValue;
				try
				{
                    // int[] values must have the same number of items as default value.
                    if ((property.PropertyType == typeof(int[])) && (value != null) && ((int[])value).Length != ((int[])defValue).Length)
                        throw new ArgumentException();

					property.SetValue(this, value, new object[0]);
                    Sources.Add(property.Name, value.Equals(defValue) ? Source.Default : optValue != null ? Source.CommandLine : regValue != null ? Source.Registry : Source.Default);
				}
				catch (ArgumentException)
				{
					Trace.TraceWarning("Unable to load {0} value from type {1}.", property.Name, value.GetType().FullName);
					value = defValue;
                    Sources.Add(property.Name, Source.Default);
                }
            }
			if (RK != null)
				RK.Close();
		}

        public void Log()
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
            {
                var value = property.GetValue(this, null);
                var source = Sources[property.Name];
                if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
                else
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
            }
        }

        public void Save()
        {
            Save(null);
        }

        public void Save(string name)
        {
			RegistryKey RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey, true);
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
            {
                if ((name != null) && (property.Name != name))
                    continue;

                var value = property.GetValue(this, null);

                if (property.PropertyType == typeof(string))
                {
                    RK.SetValue(property.Name, value, RegistryValueKind.String);
                }
                else if (property.PropertyType == typeof(int))
                {
                    RK.SetValue(property.Name, value, RegistryValueKind.DWord);
                }
                else if (property.PropertyType == typeof(bool))
                {
                    RK.SetValue(property.Name, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                }
                else if (property.PropertyType == typeof(int[]))
                {
                    RK.SetValue(property.Name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
                }
            }
        }
    }
}
