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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using ORTS.Common;

namespace ORTS.Settings
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

    public class UserSettings : SettingsBase
    {
        public static readonly string RegistryKey;        // ie @"SOFTWARE\OpenRails\ORTS"
        public static readonly string SettingsFilePath;   // ie @"C:\Program Files\Open Rails\OpenRails.ini"
        public static readonly string UserDataFolder;     // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails"
        public static readonly string DeletedSaveFolder;  // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails\Deleted Saves"
        public static readonly string SavePackFolder;     // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails\Save Packs"

        static UserSettings()
        {
            // Only one of these is allowed; if the INI file exists, we use that, otherwise we use the registry.
            RegistryKey = "SOFTWARE\\OpenRails\\ORTS";
            SettingsFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "OpenRails.ini");
            if (File.Exists(SettingsFilePath))
                RegistryKey = null;
            else
                SettingsFilePath = null;

            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            // TODO: If using INI file, move these to application directory as well.
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);
            DeletedSaveFolder = Path.Combine(UserDataFolder, "Deleted Saves");
            SavePackFolder = Path.Combine(UserDataFolder, "Save Packs");
        }

        readonly Dictionary<string, object> CustomDefaultValues = new Dictionary<string, object>();

        #region Menu_Selection enum
        public enum Menu_SelectionIndex
        {
            // Base items
            Folder = 0,
            Route = 1,
            // Activity mode items
            Activity = 2,
            Locomotive = 3,
            Consist = 4,
            Path = 5,
            Time = 6,
            // Timetable mode items
            TimetableSet = 2,
            Timetable = 3,
            Train = 4,
            Day = 5,
            // Shared items
            Season = 7,
            Weather = 8,
        }
        #endregion

        /// <summary>
        /// Specifies an anti-aliasing method. Currently, Monogame's MSAA is the only supported method.
        /// </summary>
        public enum AntiAliasingMethod
        {
            /// <summary>
            /// No antialiasing
            /// </summary>
            None = 1,
            /// <summary>
            /// 2x multisampling
            /// </summary>
            MSAA2x = 2,
            /// <summary>
            /// 4x multisampling
            /// </summary>
            MSAA4x = 3,
            /// <summary>
            /// 8x multisampling
            /// </summary>
            MSAA8x = 4,
            /// <summary>
            /// 16x multisampling
            /// </summary>
            MSAA16x = 5,
            /// <summary>
            /// 32x multisampling
            /// </summary>
            MSAA32x = 6,
        }

        public enum DirectXFeature
        {
            Level /* Default value which gets replaced with what is supported */,
            Level9_1,
            Level9_3,
            Level10_0,
        }

        #region User Settings

        // Please put all user settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        // Main menu settings:
        [Default(true)]
        public bool Logging { get; set; }
        [Default("")]
        public string Multiplayer_User { get; set; }
        [Default("127.0.0.1")]
        public string Multiplayer_Host { get; set; }
        [Default(30000)]
        public int Multiplayer_Port { get; set; }
        [Default(true)]
        public bool IsModeActivity { get; set; } // false indicates Timetable mode

        // General settings:
        [Default(false)]
        public bool Alerter { get; set; }
        [Default(true)]
        public bool AlerterDisableExternal { get; set; }
        [Default(true)]
        public bool SpeedControl { get; set; }
        [Default(false)]
        public bool GraduatedRelease { get; set; }
        [Default(false)]
        public bool RetainersOnAllCars { get; set; }
        [Default(true)]
        public bool UseLargeAddressAware { get; set; }
        [Default(21)]
        public int BrakePipeChargingRate { get; set; }
        [Default("Automatic")]
        public String PressureUnit { get; set; }
        [Default("Automatic")]
        public String Units { get; set; }
        [Default(false)]
        public bool DisableTCSScripts { get; set; }

        // Audio settings:
        [Default(100)]
        public int SoundVolumePercent { get; set; }
        [Default(5)]
        public int SoundDetailLevel { get; set; }
        [Default(50)]
        public int ExternalSoundPassThruPercent { get; set; } // higher = louder sound

        // Video settings:
        [Default(true)]
        public bool DynamicShadows { get; set; }
        [Default(false)]
        public bool ShadowAllShapes { get; set; }
        [Default(true)]
        public bool ModelInstancing { get; set; }
        [Default(true)]
        public bool Wire { get; set; }
        [Default(true)]
        public bool VerticalSync { get; set; }
        [Default(2000)]
        public int ViewingDistance { get; set; }
        [Default(true)]
        public bool DistantMountains { get; set; }
        [Default(40000)]
        public int DistantMountainsViewingDistance { get; set; }
        [Default(true)]
        public bool LODViewingExtension { get; set; }
        [Default(45)] // MSTS uses 60 FOV horizontally, on 4:3 displays this is 45 FOV vertically (what OR uses).
        public int ViewingFOV { get; set; }
        [Default(49)]
        public int WorldObjectDensity { get; set; }
        [Default(20)]
        public int DayAmbientLight { get; set; }
        [Default(AntiAliasingMethod.MSAA2x)]
        public int AntiAliasing { get; set; }

        // Simulation settings:

        [Default(false)]
        public bool SimpleControlPhysics { get; set; }
        [Default(true)]
        public bool UseAdvancedAdhesion { get; set; }
        [Default(false)]
        public bool BreakCouplers { get; set; }
        [Default(false)]
        public bool CurveSpeedDependent { get; set; }
        [Default(false)]
        public bool TunnelResistanceDependent { get; set; }
        [Default(false)]
        public bool WindResistanceDependent { get; set; }
        [Default(true)]
        public bool HotStart { get; set; }
        [Default(false)]
        public bool NoDieselEngineStart { get; set; }
        [Default(true)]
        public bool ElectricHotStart { get; set; }

        // Data logger settings:
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
        [Default(false)]
        public bool DataLogSteamPerformance { get; set; }
        [Default(false)]
        public bool VerboseConfigurationMessages { get; set; }

        // Evaluation settings:
        [Default(false)]
        public bool DataLogTrainSpeed { get; set; }
        [Default(10)]
        public int DataLogTSInterval { get; set; }
        //Time, Train Speed, Max Speed, Signal Aspect, Elevation, Direction, Distance Travelled, Control Mode, Throttle, Brake, Dyn Brake, Gear
        [Default(new[] { 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
        public int[] DataLogTSContents { get; set; }
        [Default(false)]
        public bool DataLogStationStops { get; set; }


        // Timetable settings:
        [Default(true)]
        public bool TTUseRestartDelays { get; set; }
        [Default(true)]
        public bool TTCreateTrainOnPoolUnderflow { get; set; }
        [Default(false)]
        public bool TTOutputTimetableTrainInfo { get; set; }
        [Default(false)]
        public bool TTOutputTimetableFullEvaluation { get; set; }

        //System settings
        [Default("")]
        public String Language { get; set; }
        // Updater settings are saved only in "Updater.ini".
        [Default(true)]
        public bool FullScreen { get; set; }
        [Default("1024x768")]
        public string WindowSize { get; set; }
        [Default(false)]
        public bool OutOfFocus { get; set; }
        [Default(false)]
        public bool WindowGlass { get; set; }
        [Default(0)]
        public int SuppressConfirmations { get; set; }
        [Default(2150)]
        public int WebServerPort { get; set; }
        [Default(false)]
        public bool PerformanceTuner { get; set; }
        [Default(60)]
        public int PerformanceTunerTarget { get; set; }

        // Experimental settings:
        [Default(0)]
        public int UseSuperElevation { get; set; }
        [Default(50)]
        public int SuperElevationMinLen { get; set; }
        [Default(1435)]
        public int SuperElevationGauge { get; set; }
        [Default(0)]
        public int LODBias { get; set; }
        [Default(true)]
        public bool SuppressShapeWarnings { get; set; }
        [Default(false)]
        public bool DoubleWire { get; set; }
        [Default(false)]
        public bool AuxActionEnabled { get; set; }
        [Default(false)]
        public bool UseLocationPassingPaths { get; set; }
        [Default(false)]
        public bool UseMSTSEnv { get; set; }
        [Default(false)]
        public bool SignalLightGlow { get; set; }
        [Default(100)]
        public int AdhesionFactor { get; set; }
        [Default(10)]
        public int AdhesionFactorChange { get; set; }
        [Default(false)]
        public bool AdhesionProportionalToWeather { get; set; }
        [Default(false)]
        public bool NoForcedRedAtStationStops { get; set; }
        [Default(false)]
        public bool CorrectQuestionableBrakingParams { get; set; }
        [Default(false)]
        public bool OpenDoorsInAITrains { get; set; }
        [Default(0)]
        public int ActRandomizationLevel { get; set; }
        [Default(0)]
        public int ActWeatherRandomizationLevel { get; set; }

        // Hidden settings:
        [Default(0)]
        public int CarVibratingLevel { get; set; }
        [Default("OpenRailsLog.txt")]
        public string LoggingFilename { get; set; }
        [Default("OpenRailsEvaluation.txt")]
        public string EvaluationFilename { get; set; }//
        [Default("")] // If left as "", OR will use the user's desktop folder
        public string LoggingPath { get; set; }
        [Default("")]
        public string ScreenshotPath { get; set; }
        [Default("")]
        public string DirectXFeatureLevel
        {
            get => DirectXFeatureEnum.ToString().Replace("Level", "");
            set => DirectXFeatureEnum = (DirectXFeature)Enum.Parse(typeof(DirectXFeature), "Level" + value);
        }
        DirectXFeature DirectXFeatureEnum;
        public bool IsDirectXFeatureLevelIncluded(DirectXFeature level) => level <= DirectXFeatureEnum;
        [Default(true)]
        public bool ShadowMapBlur { get; set; }
        [Default(4)]
        public int ShadowMapCount { get; set; }
        [Default(0)]
        public int ShadowMapDistance { get; set; }
        [Default(1024)]
        public int ShadowMapResolution { get; set; }
        [Default(10)]
        public int Multiplayer_UpdateInterval { get; set; }
        [Default("http://openrails.org/images/support-logos.jpg")]
        public string AvatarURL { get; set; }
        [Default(false)]
        public bool ShowAvatar { get; set; }
        [Default("0.0")] // Do not offer to restore/resume any saves this version or older. Updated whenever a younger save fails to restore.
        public string YoungestVersionFailedToRestore { get; set; }

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
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_MultiPlayer { get; set; }
        [Default(new[] { 0, 100 })]
        public int[] WindowPosition_NextStation { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_Quit { get; set; }
        [Default(new[] { 0, 50 })]
        public int[] WindowPosition_Switch { get; set; }
        [Default(new[] { 100, 0 })]
        public int[] WindowPosition_TrackMonitor { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_TrainDriving { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_TrainOperations { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_TrainDpu { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_CarOperations { get; set; }
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_ComposeMessage { get; set; }
        [Default(new[] { 100, 0 })]
        public int[] WindowPosition_TrainList { get; set; }

        // Menu-game communication settings:
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerClient { get; set; }
        [Default(false)]
        [DoNotSave]
        public bool MultiplayerServer { get; set; }

        // In-game settings:
        [Default(false)]
        public bool Letterbox2DCab { get; set; }
        [Default(true)]
        public bool Use3DCab { get; set; }
        [Default(0x7)] // OSDLocations.DisplayState.Auto
        public int OSDLocationsState { get; set; }
        [Default(0x1)] // OSDCars.DisplayState.Trains
        public int OSDCarsState { get; set; }
        [Default(0)] // TrackMonitor.DisplayMode.All
        public int TrackMonitorDisplayMode { get; set; }

        #endregion

        public FolderSettings Folders { get; private set; }
        public InputSettings Input { get; private set; }
        public RailDriverSettings RailDriver { get; private set; }

        public UserSettings(IEnumerable<string> options)
            : base(SettingsStore.GetSettingStore(SettingsFilePath, RegistryKey, null))
        {
            CustomDefaultValues["LoggingPath"] = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CustomDefaultValues["ScreenshotPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.ProductName);
            CustomDefaultValues["Multiplayer_User"] = Environment.UserName;
            Load(options);
            Folders = new FolderSettings(options);
            Input = new InputSettings(options);
            RailDriver = new RailDriverSettings(options);
        }

        /// <summary>
        /// Get a saving property from this instance by name.
        /// </summary>
        public SavingProperty<T> GetSavingProperty<T>(string name)
        {
            var property = GetProperty(name);
            if (property == null)
                return null;
            else
                return new SavingProperty<T>(this, property, AllowUserSettings);
        }

        public override object GetDefaultValue(string name)
        {
            var property = GetType().GetProperty(name);

            if (CustomDefaultValues.ContainsKey(property.Name))
                return CustomDefaultValues[property.Name];

            if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
                return (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;

            throw new InvalidDataException(String.Format("UserSetting {0} has no default value.", property.Name));
        }

        public string GetCacheFilePath(string type, string key)
        {
            var hasher = new MD5CryptoServiceProvider();
            hasher.ComputeHash(Encoding.Default.GetBytes(key));
            var hash = String.Join("", hasher.Hash.Select(h => h.ToString("x2")));

            var directory = Path.Combine(UserSettings.UserDataFolder, "Cache", type);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, hash + ".cache-or");
        }

        PropertyInfo GetProperty(string name)
        {
            return GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        PropertyInfo[] GetProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(pi => pi.Name != "Folders" && pi.Name != "Input" && pi.Name != "RailDriver").ToArray();
        }

        protected override object GetValue(string name)
        {
            return GetProperty(name).GetValue(this, null);
        }

        protected override void SetValue(string name, object value)
        {
            GetProperty(name).SetValue(this, value, null);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var property in GetProperties())
                Load(optionsDictionary, property.Name, property.PropertyType);
        }

        public override void Save()
        {
            foreach (var property in GetProperties())
                if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                {
                    Console.WriteLine(property.Name, property.PropertyType);
                    Save(property.Name, property.PropertyType);
                }

            Folders.Save();
            Input.Save();
            RailDriver.Save();
        }

        public override void Save(string name)
        {
            var property = GetProperty(name);
            if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                Save(property.Name, property.PropertyType);
        }

        public override void Reset()
        {
            foreach (var property in GetProperties())
                Reset(property.Name);
        }

        public void Log()
        {
            foreach (var property in GetProperties().OrderBy(p => p.Name))
            {
                var value = property.GetValue(this, null);
                var source = Sources[property.Name] == Source.CommandLine ? "(command-line)" : Sources[property.Name] == Source.User ? "(user set)" : "";
                if (property.PropertyType == typeof(string[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((string[])value).Select(v => v.ToString()).ToArray()), source);
                else if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source);
                else
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source);
            }
        }
    }

    /// <summary>
    /// A wrapper for a UserSettings property that saves any new values immediately.
    /// </summary>
    /// <typeparam name="T">Cast values to this type.</typeparam>
    public class SavingProperty<T>
    {
        private readonly UserSettings Settings;
        private readonly PropertyInfo Property;
        private readonly bool DoSave;

        internal SavingProperty(UserSettings settings, PropertyInfo property, bool allowSave = true)
        {
            Settings = settings;
            Property = property;
            DoSave = allowSave;
        }

        /// <summary>
        /// Get or set the current value of this property.
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// Get the current value of this property.
        /// </summary>
        public T GetValue()
            => Property.GetValue(Settings) is T cast ? cast : default;

        /// <summary>
        /// Set the current value of this property.
        /// </summary>
        public void SetValue(T value)
        {
            if (!GetValue().Equals(value))
            {
                Property.SetValue(Settings, value);
                if (DoSave)
                    Settings.Save(Property.Name);
            }
        }
    }
}
