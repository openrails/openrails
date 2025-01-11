using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using GNU.Gettext;
using ORTS.Common;
using ORTS.Common.Input;

namespace ORTS.Settings
{
    public enum RailDriverCalibrationSetting
    {
        [GetString("Reverser Neutral")] ReverserNeutral,
        [GetString("Reverser Full Reversed")] ReverserFullReversed,
        [GetString("Reverser Full Forward")] ReverserFullForward,
        [GetString("Throttle Idle")] ThrottleIdle,
        [GetString("Full Throttle")] ThrottleFull,
        [GetString("Dynamic Brake")] DynamicBrake,
        [GetString("Dynamic Brake Setup")] DynamicBrakeSetup,
        [GetString("Auto Brake Released")] AutoBrakeRelease,
        [GetString("Full Auto Brake ")] AutoBrakeFull,
        [GetString("Emergency Brake")] EmergencyBrake,
        [GetString("Independent Brake Released")] IndependentBrakeRelease,
        [GetString("Independent Brake Full")] IndependentBrakeFull,
        [GetString("Bail Off Disengaged (in Released position)")] BailOffDisengagedRelease,
        [GetString("Bail Off Engaged (in Released position)")] BailOffEngagedRelease,
        [GetString("Bail Off Disengaged (in Full position)")] BailOffDisengagedFull,
        [GetString("Bail Off Engaged (in Full position)")] BailOffEngagedFull,
        [GetString("Rotary Switch 1-Position 1(OFF)")] Rotary1Position1,
        [GetString("Rotary Switch 1-Position 2(SLOW)")] Rotary1Position2,
        [GetString("Rotary Switch 1-Position 3(FULL)")] Rotary1Position3,
        [GetString("Rotary Switch 2-Position 1(OFF)")] Rotary2Position1,
        [GetString("Rotary Switch 2-Position 2(DIM)")] Rotary2Position2,
        [GetString("Rotary Switch 2-Position 3(FULL)")] Rotary2Position3,
        [GetString("Reverse Reverser Direction")] ReverseReverser,
        [GetString("Reverse Throttle Direction")] ReverseThrottle,
        [GetString("Reverse Auto Brake Direction")] ReverseAutoBrake,
        [GetString("Reverse Independent Brake Direction")] ReverseIndependentBrake,
        [GetString("Full Range Throttle")] FullRangeThrottle,
        [GetString("Cut Off Delta")] CutOffDelta,
    }

    public class RailDriverSettings : SettingsBase
    {
        public static readonly string SectionName = "RailDriver";

        static readonly GettextResourceManager catalog = new GettextResourceManager("ORTS.Settings");
        private static readonly byte[] DefaultCalibrationSettings;
        private static readonly Dictionary<UserCommand, byte> DefaultUserCommands;

        private bool default0WhileSaving;

        public readonly byte[] UserCommands = new byte[Enum.GetNames(typeof(UserCommand)).Length];

        public readonly byte[] CalibrationSettings;

        static RailDriverSettings()
        {
            //default calibration settings from another developer's PC, they are as good as random numbers...
            DefaultCalibrationSettings = new byte[] { 225, 116, 60, 229, 176, 42, 119, 216, 79, 58, 213, 179, 30, 209, 109, 121, 73, 135, 180, 86, 145, 189, 0, 0, 0, 0, 0, 1 };
            DefaultUserCommands = new Dictionary<UserCommand, byte>();

            // top row of blue buttons left to right
            DefaultUserCommands.Add(UserCommand.GamePauseMenu, 0);                  // Btn 00 Default Legend Game Pause
            DefaultUserCommands.Add(UserCommand.GameSave, 1);                       // Btn 01 Default Legend Game Save
                                                                                    // Btn 02 Default Legend Control Gauges
            DefaultUserCommands.Add(UserCommand.DisplayTrackMonitorWindow, 3);      // Btn 03 Default Legend Track Monitor
                                                                                    // Btn 04 Default Legend Station/Siding Names
                                                                                    // Btn 05 Default Legend Car #
            DefaultUserCommands.Add(UserCommand.DisplaySwitchWindow, 6);            // Btn 06 Default Legend Switching Drive Aids
            DefaultUserCommands.Add(UserCommand.DisplayTrainCarOperationsWindow, 7);// Btn 07 Default Legend Train Car Operations
            DefaultUserCommands.Add(UserCommand.DisplayNextStationWindow, 8);       // Btn 08 Default Legend Next Station Window
                                                                                    // Btn 09 Default Legend Ops Notebook
                                                                                    // Btn 10 Default Legend Hide Drive Aids
            DefaultUserCommands.Add(UserCommand.DisplayCompassWindow, 11);          // Btn 11 Default Legend Compass Window
            DefaultUserCommands.Add(UserCommand.GameSwitchAhead, 12);         // Btn 12 Default Legend Switch Ahead
            DefaultUserCommands.Add(UserCommand.GameSwitchBehind, 13);        // Btn 13 Default Legend Switch Behind

            // bottom row of blue buttons left to right
            DefaultUserCommands.Add(UserCommand.GameExternalCabController, 14);     // Btn 14 Default Legend RailDriver Run/Stop
            DefaultUserCommands.Add(UserCommand.CameraToggleShowCab, 15);           // Btn 15 Default Legend Hide Cab Panel
            DefaultUserCommands.Add(UserCommand.CameraCab, 16);                     // Btn 16 Default Legend Frnt Cab View
            DefaultUserCommands.Add(UserCommand.CameraOutsideFront, 17);            // Btn 17 Default Legend Ext View 1
            DefaultUserCommands.Add(UserCommand.CameraOutsideRear, 18);             // Btn 18 Default Legend Ext.View 2
            DefaultUserCommands.Add(UserCommand.CameraCarPrevious, 19);             // Btn 19 Default Legend FrontCoupler
            DefaultUserCommands.Add(UserCommand.CameraCarNext, 20);                 // Btn 20 Default Legend Rear Coupler
            DefaultUserCommands.Add(UserCommand.CameraTrackside, 21);               // Btn 21 Default Legend Track View      
            DefaultUserCommands.Add(UserCommand.CameraPassenger, 22);               // Btn 22 Default Legend Passgr View      
            DefaultUserCommands.Add(UserCommand.CameraBrakeman, 23);                // Btn 23 Default Legend Coupler View
            DefaultUserCommands.Add(UserCommand.CameraFree, 24);                    // Btn 24 Default Legend Yard View
            DefaultUserCommands.Add(UserCommand.GameClearSignalForward, 25);        // Btn 25 Default Legend Request Pass
                                                                                    // Btn 26 Default Legend Load/Unload
                                                                                    // Btn 27 Default Legend OK

            // controls to right of blue buttons
            DefaultUserCommands.Add(UserCommand.CameraZoomIn, 28);
            DefaultUserCommands.Add(UserCommand.CameraZoomOut, 29);
            DefaultUserCommands.Add(UserCommand.CameraPanUp, 30);
            DefaultUserCommands.Add(UserCommand.CameraPanRight, 31);
            DefaultUserCommands.Add(UserCommand.CameraPanDown, 32);
            DefaultUserCommands.Add(UserCommand.CameraPanLeft, 33);

            // buttons on top left
            DefaultUserCommands.Add(UserCommand.ControlGearUp, 34);
            DefaultUserCommands.Add(UserCommand.ControlGearDown, 35);
            DefaultUserCommands.Add(UserCommand.ControlEmergencyPushButton, 36);
            //DefaultUserCommands.Add(UserCommand.ControlEmergencyPushButton, 37);
            DefaultUserCommands.Add(UserCommand.ControlAlerter, 38);
            DefaultUserCommands.Add(UserCommand.ControlSander, 39);
            DefaultUserCommands.Add(UserCommand.ControlPantograph1, 40);
            DefaultUserCommands.Add(UserCommand.ControlBellToggle, 41);
            DefaultUserCommands.Add(UserCommand.ControlHorn, 42);
            //DefaultUserCommands.Add(UserCommand.ControlHorn, 43);

        }

        /// <summary>
        /// Initializes a new instances of the <see cref="InputSettings"/> class with the specified options.
        /// </summary>
        /// <param name="options">The list of one-time options to override persisted settings, if any.</param>
        public RailDriverSettings(IEnumerable<string> options)
        : base(SettingsStore.GetSettingStore(SettingsBase.SettingsFilePath, SettingsBase.RegistryKey, SectionName))
        {
            CalibrationSettings = new byte[DefaultCalibrationSettings.Length];

            Load(options);
        }

        public override object GetDefaultValue(string name)
        {
            if (Enum.TryParse(name, true, out RailDriverCalibrationSetting calibrationSetting))
            {
                return default0WhileSaving ? 0 : DefaultCalibrationSettings[(int)calibrationSetting];
            }
            else if (Enum.TryParse(name, true, out UserCommand userCommand))
            {
                return GetDefaultValue(userCommand);
            }
            else
                throw new ArgumentOutOfRangeException($"Enum parameter {nameof(name)} not within expected range of either {nameof(RailDriverCalibrationSetting)} or {nameof(UserCommands)}");
        }

        public static byte GetDefaultValue(UserCommand command)
        {
            return DefaultUserCommands.TryGetValue(command, out byte value) ? value : byte.MaxValue;
        }

        public override void Reset()
        {
            //do not reset calibrations
            //foreach (RailDriverCalibrationSetting setting in EnumExtension.GetValues<RailDriverCalibrationSetting>())
            //    Reset(setting.ToString());

            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
                Reset(command.ToString());
        }

        public override void Save()
        {
            default0WhileSaving = true; //temporarily "disable" default calibration settings, so Calibration Settings are always getting written to SettingsStore
            foreach (RailDriverCalibrationSetting setting in Enum.GetValues(typeof(RailDriverCalibrationSetting)))
                Save(setting.ToString());

            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
                Save(command.ToString());

            default0WhileSaving = false;
        }

        public override void Save(string name)
        {
            Save(name, typeof(byte));
        }

        protected override object GetValue(string name)
        {
            if (Enum.TryParse(name, true, out RailDriverCalibrationSetting calibrationSetting))
            {
                return CalibrationSettings[(int)calibrationSetting];
            }
            else if (Enum.TryParse(name, true, out UserCommand userCommand))
            {
                return UserCommands[(int)userCommand];
            }
            else
                throw new ArgumentOutOfRangeException($"Enum parameter {nameof(name)} not within expected range of either {nameof(RailDriverCalibrationSetting)} or {nameof(UserCommands)}");
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (RailDriverCalibrationSetting setting in Enum.GetValues(typeof(RailDriverCalibrationSetting)))
                Load(optionsDictionary, setting.ToString(), typeof(byte));
            foreach (var command in Enum.GetValues(typeof(UserCommand)))
                Load(optionsDictionary, command.ToString(), typeof(byte));
        }

        protected override void SetValue(string name, object value)
        {
            if (Enum.TryParse(name, true, out RailDriverCalibrationSetting calibrationSetting))
            {
                if (!byte.TryParse(value?.ToString(), out byte result))
                    result = 0;
                CalibrationSettings[(int)calibrationSetting] = result;
            }
            else if (Enum.TryParse(name, true, out UserCommand userCommand))
            {
                UserCommands[(int)userCommand] = (byte)value;
            }
            else
                throw new ArgumentOutOfRangeException($"Enum parameter {nameof(name)} not within expected range of either {nameof(RailDriverCalibrationSetting)} or {nameof(UserCommands)}");
        }

        public string CheckForErrors(byte[] buttonSettings)
        {
            StringBuilder errors = new StringBuilder();

            var duplicates = buttonSettings.Select((value, index) => new { Index = index, Button = value }).
                Where(g => g.Button < 255).
                GroupBy(g => g.Button).
                Where(g => g.Count() > 1).
                OrderBy(g => g.Key);

            foreach (var duplicate in duplicates)
            {
                errors.Append(catalog.GetStringFmt("Button {0} is assigned to \r\n\t", duplicate.Key));
                foreach (var buttonMapping in duplicate)
                {
                    errors.Append($"\"{InputSettings.GetPrettyLocalizedName((UserCommand)(buttonMapping.Index))}\" and ");
                }
                errors.Remove(errors.Length - 5, 5);
                errors.AppendLine();
            }
            return errors.ToString();
        }

        public void DumpToText(string filePath)
        {
            var buttonMappings = UserCommands.
                Select((value, index) => new { Index = index, Button = value }).
                Where(button => button.Button < 255).
                OrderBy(button => button.Button);

            using (var writer = new StreamWriter(File.OpenWrite(filePath)))
            {
                writer.WriteLine("{0,-40}{1,-40}", "Command", "Button");
                writer.WriteLine(new string('=', 40 * 2));
                foreach (var buttonMapping in buttonMappings)
                    writer.WriteLine("{0,-40}{1,-40}", InputSettings.GetPrettyLocalizedName((UserCommand)(buttonMapping.Index)), buttonMapping.Button);
            }
        }

    }
}
