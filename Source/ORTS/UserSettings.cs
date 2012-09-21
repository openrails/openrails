// COPYRIGHT 2010, 2011, 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ORTS
{
    public class DefaultAttribute : Attribute
    {
        public readonly object Value;
        public DefaultAttribute(object value)
        {
            Value = value;
        }
    }

    public class DoNotSaveAttribute : Attribute
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
    public class UserSettings
    {
        readonly string RegistryKey;
        readonly Dictionary<string, object> CustomDefaultValues = new Dictionary<string, object>();
        readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();

        public enum Source
        {
            Default,
            CommandLine,
            Registry,
        }

        #region User Settings

        // Please put all user settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        // General settings.
        [Default(false)]
        public bool Alerter { get; set; }
        [Default(21)]
        public int BrakePipeChargingRate { get; set; }
        [Default( 0 )]
        public int Cab2DStretch { get; set; }
        [Default(false)]
        public bool DataLogger { get; set; }
        [Default(false)]
        public bool DynamicShadows { get; set; }
        [Default(false)]
        public bool FullScreen { get; set; }
        [Default(false)]
        public bool GraduatedRelease { get; set; }
        [Default(true)]
        public bool Logging { get; set; }
        [Default("OpenRailsLog.txt")]
        public string LoggingFilename { get; set; }
        [Default("")]
        public string LoggingPath { get; set; }
        [Default(false)]
        public bool MSTSBINSound { get; set; }
        [Default(false)]
        public bool Precipitation { get; set; }
        [Default(false)]
        public bool Profiling { get; set; }
        [Default(0)]
        public int ProfilingFrameCount { get; set; }
        [Default(0)]
        public int ProfilingTime { get; set; }
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
        [Default(true)]
        public bool ShowErrorDialogs { get; set; }
        [Default(5)]
        public int SoundDetailLevel { get; set; }
        [Default(false)]
        public bool SuppressConfirmations { get; set; }
        [Default(false)]
        public bool TrainLights { get; set; }
        [Default(false)]
        public bool VerticalSync { get; set; }
        [Default(2000)]
        public int ViewingDistance { get; set; }
        [Default(45)] // MSTS uses 60 FOV horizontally, on 4:3 displays this is 45 FOV vertically (what OR uses).
        public int ViewingFOV { get; set; }
        [Default(false)]
        public bool WindowGlass { get; set; }
        [Default("1024x768")]
        public string WindowSize { get; set; }
        [Default(false)]
        public bool Wire { get; set; }
        [Default(10)]
        public int WorldObjectDensity { get; set; }
        [Default(false)]
        public bool ViewDispatcher { get; set; }

        [Default(new string[0])]
        public string[] Menu_Selection { get; set; }

        // These two are command-line only flags to start multiplayer modes.
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerClient { get; set; }
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerServer { get; set; }

        // Multiplayer settings.
        [Default(false)]
        public bool Multiplayer { get; set; }
        [Default("")]
        public string Multiplayer_User { get; set; }
        [Default("127.0.0.1")]
        public string Multiplayer_Host { get; set; }
        [Default(30000)]
        public int Multiplayer_Port { get; set; }
        [Default(10)]
        public int Multiplayer_UpdateInterval { get; set; }
		[Default("http://openrails.org/images/support-logos.jpg")]
		public string AvatarURL { get; set; }
		[Default(false)]
		public bool ShowAvatar { get; set; }

        // Physics options
        [Default(true)]
        public bool UseAdvancedAdhesion { get; set; }
        [Default(false)]
        public bool BreakCouplers { get; set; }

        // Window position settings.
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
		public int[] WindowPosition_ComposeMessage { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of <c>UserSettings</c> with a given <paramref name="registryKey"/> and list of <paramref name="options"/> overrides.
        /// </summary>
        /// <param name="registryKey">Registry key under <c>HKEY_CURRENT_USER</c> to load and save the settings.</param>
        /// <param name="options">List of all the setting overrides from the command-line.</param>
        public UserSettings(string registryKey, IEnumerable<string> options)
        {
            RegistryKey = registryKey;
            InitUserSettings();
            LoadUserSettings(options);
        }

        void InitUserSettings()
        {
            CustomDefaultValues["LoggingPath"] = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CustomDefaultValues["ScreenshotPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.ProductName);
            CustomDefaultValues["Multiplayer_User"] = Environment.UserName;
        }

        /// <summary>
        /// Merges the settings from the defaults, the saved values, and the <paramref name="options"/> passed in.
        /// </summary>
        /// <param name="options">A list of options to override the default and saved values, specified in "name=value" or "name:value" format.</param>
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

            using (var RK = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                // All public instance properties are settings. Go through them all.
                foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
                {
                    // Get the default value.
                    object defValue = null;
                    if (CustomDefaultValues.ContainsKey(property.Name))
                        defValue = CustomDefaultValues[property.Name];
                    else if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
                        defValue = (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;
                    else
                        throw new InvalidDataException(String.Format("UserSetting property {0} has no default value.", property.Name));
                    // Read in the registry option, if it exists.
                    var regValue = allowRegistryValues && RK != null ? RK.GetValue(property.Name, null) : null;
                    // Read in the command-line option, if it exists into optValue.
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

                    // Parse command-line option for string[] types.
                    else if ((optValue != null) && (property.PropertyType == typeof(string[])))
                        optValue = ((string)optValue).Split(',').Select(s => s.Trim()).ToArray();

                    // Parse command-line option for int[] types.
                    else if ((optValue != null) && (property.PropertyType == typeof(int[])))
                        optValue = ((string)optValue).Split(',').Select(s => int.Parse(s.Trim())).ToArray();

                    // We now have defValue, regValue, optValue containing the default, persisted and override values
                    // for the setting. regValue and optValue are null if they are not found/specified.
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
                        Trace.TraceWarning("Unable to load {0} value from type {1}", property.Name, value.GetType().FullName);
                        value = defValue;
                        Sources.Add(property.Name, Source.Default);
                    }
                }
            }
        }

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
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((string[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
                else if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
                else 
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
            }
        }

        /// <summary>
        /// Saves all peristent settings.
        /// </summary>
        public void Save()
        {
            Save(null);
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
            using (var RK = Registry.CurrentUser.CreateSubKey(RegistryKey))
            {
                var values = RK.GetValueNames();
                foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
                {
                    if ((name != null) && (property.Name != name))
                        continue;

                    if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length > 0)
                        continue;

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
                        if (values.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                            RK.DeleteValue(property.Name);
                    }
                    else if (property.PropertyType == typeof(string))
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
                    else if (property.PropertyType == typeof(string[]))
                    {
                        RK.SetValue(property.Name, (string[])value, RegistryValueKind.MultiString);
                    }
                    else if (property.PropertyType == typeof(int[]))
                    {
                        RK.SetValue(property.Name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
                    }
                }
            }
        }
    }
}