// COPYRIGHT 2012, 2013, 2014 by the Open Rails project.
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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ORTS.Common;
using GNU.Gettext;

namespace ORTS.Settings
{
    /// <summary>
    /// Specifies game commands.
    /// </summary>
    /// <remarks>
    /// <para>The ordering and naming of these commands is important. They are listed in the UI in the order they are defined in the code, and the first word of each command is the "group" to which it belongs.</para>
    /// </remarks>
    public enum UserCommands
    {
        [GetString("Game Pause Menu")] GamePauseMenu,
        [GetString("Game Save")] GameSave,
        [GetString("Game Quit")] GameQuit,
        [GetString("Game Pause")] GamePause,
        [GetString("Game Screenshot")] GameScreenshot,
        [GetString("Game Fullscreen")] GameFullscreen,
        [GetString("Game Switch Ahead")] GameSwitchAhead,
        [GetString("Game Switch Behind")] GameSwitchBehind,
        [GetString("Game Switch Picked")] GameSwitchPicked,
        [GetString("Game Signal Picked")] GameSignalPicked,
        [GetString("Game Switch With Mouse")] GameSwitchWithMouse,
        [GetString("Game Uncouple With Mouse")] GameUncoupleWithMouse,
        [GetString("Game Change Cab")] GameChangeCab,
        [GetString("Game Request Control")] GameRequestControl,
        [GetString("Game Multi Player Dispatcher")] GameMultiPlayerDispatcher,
        [GetString("Game Multi Player Texting")] GameMultiPlayerTexting,
        [GetString("Game Switch Manual Mode")] GameSwitchManualMode,
        [GetString("Game Clear Signal Forward")] GameClearSignalForward,
        [GetString("Game Clear Signal Backward")] GameClearSignalBackward,
        [GetString("Game Reset Signal Forward")] GameResetSignalForward,
        [GetString("Game Reset Signal Backward")] GameResetSignalBackward,
        [GetString("Game Autopilot Mode")] GameAutopilotMode,
        [GetString("Game Suspend Old Player")] GameSuspendOldPlayer,

        [GetString("Display Next Window Tab")] DisplayNextWindowTab,
        [GetString("Display Help Window")] DisplayHelpWindow,
        [GetString("Display Track Monitor Window")] DisplayTrackMonitorWindow,
        [GetString("Display HUD")] DisplayHUD,
        [GetString("Display Car Labels")] DisplayCarLabels,
        [GetString("Display Station Labels")] DisplayStationLabels,
        [GetString("Display Switch Window")] DisplaySwitchWindow,
        [GetString("Display Train Operations Window")] DisplayTrainOperationsWindow,
        [GetString("Display Next Station Window")] DisplayNextStationWindow,
        [GetString("Display Compass Window")] DisplayCompassWindow,
        [GetString("Display Basic HUD Toggle")] DisplayBasicHUDToggle,
        [GetString("Display Train List Window")] DisplayTrainListWindow,

        [GetString("Debug Speed Up")] DebugSpeedUp,
        [GetString("Debug Speed Down")] DebugSpeedDown,
        [GetString("Debug Speed Reset")] DebugSpeedReset,
        [GetString("Debug Overcast Increase")] DebugOvercastIncrease,
        [GetString("Debug Overcast Decrease")] DebugOvercastDecrease,
        [GetString("Debug Fog Increase")] DebugFogIncrease,
        [GetString("Debug Fog Decrease")] DebugFogDecrease,
        [GetString("Debug Precipitation Increase")] DebugPrecipitationIncrease,
        [GetString("Debug Precipitation Decrease")] DebugPrecipitationDecrease,
        [GetString("Debug Precipitation Liquidity Increase")] DebugPrecipitationLiquidityIncrease,
        [GetString("Debug Precipitation Liquidity Decrease")] DebugPrecipitationLiquidityDecrease,
        [GetString("Debug Weather Change")] DebugWeatherChange,
        [GetString("Debug Clock Forwards")] DebugClockForwards,
        [GetString("Debug Clock Backwards")] DebugClockBackwards,
        [GetString("Debug Logger")] DebugLogger,
        [GetString("Debug Lock Shadows")] DebugLockShadows,
        [GetString("Debug Dump Keyboard Map")] DebugDumpKeymap,
        [GetString("Debug Log Render Frame")] DebugLogRenderFrame,
        [GetString("Debug Tracks")] DebugTracks,
        [GetString("Debug Signalling")] DebugSignalling,
        [GetString("Debug Reset Wheel Slip")] DebugResetWheelSlip,
        [GetString("Debug Toggle Advanced Adhesion")] DebugToggleAdvancedAdhesion,
        [GetString("Debug Sound Form")] DebugSoundForm,
        [GetString("Debug Physics Form")] DebugPhysicsForm,

        [GetString("Camera Cab")] CameraCab,
        [GetString("Camera Change Passenger Viewpoint")] CameraChangePassengerViewPoint,
        [GetString("Camera 3D Cab")] CameraThreeDimensionalCab,
        [GetString("Camera Toggle Show Cab")] CameraToggleShowCab,
        [GetString("Camera Head Out Forward")] CameraHeadOutForward,
        [GetString("Camera Head Out Backward")] CameraHeadOutBackward,
        [GetString("Camera Outside Front")] CameraOutsideFront,
        [GetString("Camera Outside Rear")] CameraOutsideRear,
        [GetString("Camera Trackside")] CameraTrackside,
        [GetString("Camera SpecialTracksidePoint")] CameraSpecialTracksidePoint,
        [GetString("Camera Passenger")] CameraPassenger,
        [GetString("Camera Brakeman")] CameraBrakeman,
        [GetString("Camera Free")] CameraFree,
        [GetString("Camera Previous Free")] CameraPreviousFree,
        [GetString("Camera Reset")] CameraReset,
        [GetString("Camera Move Fast")] CameraMoveFast,
        [GetString("Camera Move Slow")] CameraMoveSlow,
        [GetString("Camera Pan (Rotate) Left")] CameraPanLeft,
        [GetString("Camera Pan (Rotate) Right")] CameraPanRight,
        [GetString("Camera Pan (Rotate) Up")] CameraPanUp,
        [GetString("Camera Pan (Rotate) Down")] CameraPanDown,
        [GetString("Camera Zoom In (Move Z)")] CameraZoomIn,
        [GetString("Camera Zoom Out (Move Z)")] CameraZoomOut,
        [GetString("Camera Rotate (Pan) Left")] CameraRotateLeft,
        [GetString("Camera Rotate (Pan) Right")] CameraRotateRight,
        [GetString("Camera Rotate (Pan) Up")] CameraRotateUp,
        [GetString("Camera Rotate (Pan) Down")] CameraRotateDown,
        [GetString("Camera Car Next")] CameraCarNext,
        [GetString("Camera Car Previous")] CameraCarPrevious,
        [GetString("Camera Car First")] CameraCarFirst,
        [GetString("Camera Car Last")] CameraCarLast,
        [GetString("Camera Jumping Trains")] CameraJumpingTrains,
        [GetString("Camera Jump Back Player")] CameraJumpBackPlayer,
        [GetString("Camera Jump See Switch")] CameraJumpSeeSwitch,
        [GetString("Camera Vibrate")] CameraVibrate,
        [GetString("Camera Scroll Right")] CameraScrollRight,
        [GetString("Camera Scroll Left")] CameraScrollLeft,

        [GetString("Control Forwards")] ControlForwards,
        [GetString("Control Backwards")] ControlBackwards,
        [GetString("Control Throttle Increase")] ControlThrottleIncrease,
        [GetString("Control Throttle Decrease")] ControlThrottleDecrease,
        [GetString("Control Throttle Zero")] ControlThrottleZero,
        [GetString("Control Gear Up")] ControlGearUp,
        [GetString("Control Gear Down")] ControlGearDown,
        [GetString("Control Train Brake Increase")] ControlTrainBrakeIncrease,
        [GetString("Control Train Brake Decrease")] ControlTrainBrakeDecrease,
        [GetString("Control Train Brake Zero")] ControlTrainBrakeZero,
        [GetString("Control Engine Brake Increase")] ControlEngineBrakeIncrease,
        [GetString("Control Engine Brake Decrease")] ControlEngineBrakeDecrease,
        [GetString("Control Dynamic Brake Increase")] ControlDynamicBrakeIncrease,
        [GetString("Control Dynamic Brake Decrease")] ControlDynamicBrakeDecrease,
        [GetString("Control Bail Off")] ControlBailOff,
        [GetString("Control Initialize Brakes")] ControlInitializeBrakes,
        [GetString("Control Handbrake Full")] ControlHandbrakeFull,
        [GetString("Control Handbrake None")] ControlHandbrakeNone,
        [GetString("Control Odometer Show/Hide")] ControlOdoMeterShowHide,
        [GetString("Control Odometer Reset")] ControlOdoMeterReset,
        [GetString("Control Odometer Direction")] ControlOdoMeterDirection,
        [GetString("Control Retainers On")] ControlRetainersOn,
        [GetString("Control Retainers Off")] ControlRetainersOff,
        [GetString("Control Brake Hose Connect")] ControlBrakeHoseConnect,
        [GetString("Control Brake Hose Disconnect")] ControlBrakeHoseDisconnect,
        [GetString("Control Alerter")] ControlAlerter,
        [GetString("Control Emergency Push Button")] ControlEmergencyPushButton,
        [GetString("Control Sander")] ControlSander,
        [GetString("Control Sander Toggle")] ControlSanderToggle,
        [GetString("Control Wiper")] ControlWiper,
        [GetString("Control Horn")] ControlHorn,
        [GetString("Control Bell")] ControlBell,
        [GetString("Control Bell Toggle")] ControlBellToggle,
        [GetString("Control Door Left")] ControlDoorLeft,
        [GetString("Control Door Right")] ControlDoorRight,
        [GetString("Control Mirror")] ControlMirror,
        [GetString("Control Light")] ControlLight,
        [GetString("Control Pantograph 1")] ControlPantograph1,
        [GetString("Control Pantograph 2")] ControlPantograph2,
        [GetString("Control Circuit Breaker Closing Order")] ControlCircuitBreakerClosingOrder,
        [GetString("Control Circuit Breaker Opening Order")] ControlCircuitBreakerOpeningOrder,
        [GetString("Control Circuit Breaker Closing Authorization")] ControlCircuitBreakerClosingAuthorization,
        [GetString("Control Diesel Player")] ControlDieselPlayer,
        [GetString("Control Diesel Helper")] ControlDieselHelper,
        [GetString("Control Headlight Increase")] ControlHeadlightIncrease,
        [GetString("Control Headlight Decrease")] ControlHeadlightDecrease,
        [GetString("Control Injector 1 Increase")] ControlInjector1Increase,
        [GetString("Control Injector 1 Decrease")] ControlInjector1Decrease,
        [GetString("Control Injector 1")] ControlInjector1,
        [GetString("Control Injector 2 Increase")] ControlInjector2Increase,
        [GetString("Control Injector 2 Decrease")] ControlInjector2Decrease,
        [GetString("Control Injector 2")] ControlInjector2,
        [GetString("Control Blower Increase")] ControlBlowerIncrease,
        [GetString("Control Blower Decrease")] ControlBlowerDecrease,
        [GetString("Control Steam Heat Increase")] ControlSteamHeatIncrease,
        [GetString("Control Steam Heat Decrease")] ControlSteamHeatDecrease,
        [GetString("Control Damper Increase")] ControlDamperIncrease,
        [GetString("Control Damper Decrease")] ControlDamperDecrease,
        [GetString("Control Firebox Open")] ControlFireboxOpen,
        [GetString("Control Firebox Close")] ControlFireboxClose,
        [GetString("Control Firing Rate Increase")] ControlFiringRateIncrease,
        [GetString("Control Firing Rate Decrease")] ControlFiringRateDecrease,
        [GetString("Control Fire Shovel Full")] ControlFireShovelFull,
        [GetString("Control Cylinder Cocks")] ControlCylinderCocks,
        [GetString("Control Small Ejector Increase")] ControlSmallEjectorIncrease,
        [GetString("Control Small Ejector Decrease")] ControlSmallEjectorDecrease,
        [GetString("Control Cylinder Compound")] ControlCylinderCompound,
        [GetString("Control Firing")] ControlFiring,
        [GetString("Control Refill")] ControlRefill,
        [GetString("Control TroughRefill")] ControlTroughRefill,
        [GetString("Control ImmediateRefill")]ControlImmediateRefill,
        [GetString("Control Turntable Clockwise")]ControlTurntableClockwise,
        [GetString("Control Turntable Counterclockwise")]ControlTurntableCounterclockwise,
        [GetString("Control Cab Radio")] ControlCabRadio,
        [GetString("Control AI Fire On")] ControlAIFireOn,
        [GetString("Control AI Fire Off")] ControlAIFireOff,
        [GetString("Control AI Fire Reset")] ControlAIFireReset,
    }

    /// <summary>
    /// Specifies the keyboard modifiers for <see cref="UserCommands"/>.
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }

    /// <summary>
    /// Loads, stores and manages keyboard input settings for all available <see cref="UserCommands"/>.
    /// </summary>
    /// <remarks>
    /// <para>Keyboard input is processed by associating specific combinations of keys (either scan codes or virtual keys) and modifiers with each <see cref="UserCommands"/>.</para>
    /// <para>There are three kinds of <see cref="UserCommands"/>, each using a different <see cref="UserCommandInput"/>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="UserCommandModifierInput"/> represents a specific combination of keyboard modifiers (Shift, Control and Alt). E.g. Shift.</description></item>
    /// <item><description><see cref="UserCommandKeyInput"/> represents a key (scan code or virtual key) and a specific combination of keyboard modifiers. E.g. Alt-F4.</description></item>
    /// <item><description><see cref="UserCommandModifiableKeyInput"/> represents a key (scan code or virtual key), a specific combination of keyboard modifiers and a set of keyboard modifiers to ignore. E.g. Up Arrow (+ Shift) (+ Control).</description></item>
    /// </list>
    /// <para>Keyboard input is identified in two distinct ways:</para>
    /// <list>
    /// <item><term>Scan code</term><description>A scan code represents a specific location on the physical keyboard, irrespective of the user's locale, keyboard layout and other enviromental settings. For this reason, this is the preferred way to refer to the "main" area of the keyboard - this area varies significantly by locale and usually it is the physical location that matters.</description></item>
    /// <item><term>Virtual key</term><description>A virtual key represents a logical key on the keyboard, irrespective of where it might be located. For keys outside the "main" area, this is much the same as scan codes and is preferred when refering to logical keys like "Up Arrow".</description></item>
    /// </list>
    /// </remarks>
    public class InputSettings : SettingsBase
    {
        static GettextResourceManager catalog = new GettextResourceManager("ORTS.Settings");

        public static readonly UserCommandInput[] DefaultCommands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];
        public readonly UserCommandInput[] Commands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];

        static InputSettings()
        {
            InitializeCommands(DefaultCommands);
        }

        /// <summary>
        /// Initializes a new instances of the <see cref="InputSettings"/> class with the specified options.
        /// </summary>
        /// <param name="options">The list of one-time options to override persisted settings, if any.</param>
        public InputSettings(IEnumerable<string> options)
        : base(SettingsStore.GetSettingStore(UserSettings.SettingsFilePath, UserSettings.RegistryKey, "Keys"))
        {
            InitializeCommands(Commands);
            Load(options);
        }

        UserCommands GetCommand(string name)
        {
            return (UserCommands)Enum.Parse(typeof(UserCommands), name);
        }

        UserCommands[] GetCommands()
        {
            return (UserCommands[])Enum.GetValues(typeof(UserCommands));
        }

        public override object GetDefaultValue(string name)
        {
            return DefaultCommands[(int)GetCommand(name)].PersistentDescriptor;
        }

        protected override object GetValue(string name)
        {
            return Commands[(int)GetCommand(name)].PersistentDescriptor;
        }

        protected override void SetValue(string name, object value)
        {
            Commands[(int)GetCommand(name)].PersistentDescriptor = (string)value;
        }

        protected override void Load(bool allowUserSettings, Dictionary<string, string> optionsDictionary)
        {
            foreach (var command in GetCommands())
                Load(allowUserSettings, optionsDictionary, command.ToString(), typeof(string));
        }

        public override void Save()
        {
            foreach (var command in GetCommands())
                Save(command.ToString());
        }

        public override void Save(string name)
        {
            Save(name, typeof(string));
        }

        public override void Reset()
        {
            foreach (var command in GetCommands())
                Reset(command.ToString());
        }

        #region External APIs
        enum MapVirtualKeyType
        {
            VirtualToCharacter = 2,
            VirtualToScan = 0,
            VirtualToScanEx = 4,
            ScanToVirtual = 1,
            ScanToVirtualEx = 3,
        }

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int code, MapVirtualKeyType type);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetKeyNameText(int scanCode, [Out] string name, int nameLength);
        #endregion

        // Keyboard scancodes are basically constant; some keyboards have extra buttons (e.g. UK ones tend to have an
        // extra button next to Left Shift) or move one or two around (e.g. UK ones tend to move 0x2B down one row)
        // but generally this layout is right. Numeric keypad omitted as most keys are just duplicates of the main
        // keys (in two sets, based on Num Lock) and we don't use them. Scancodes are in hex.
        //
        // Break/Pause (0x11D) is handled specially and doesn't use the expect 0x45 scancode.
        //
        public static readonly string[] KeyboardLayout = new[] {
            "[01 ]   [3B ][3C ][3D ][3E ]   [3F ][40 ][41 ][42 ]   [43 ][44 ][57 ][58 ]   [37 ][46 ][11D]",
            "                                                                                            ",
            "[29 ][02 ][03 ][04 ][05 ][06 ][07 ][08 ][09 ][0A ][0B ][0C ][0D ][0E     ]   [52 ][47 ][49 ]",
            "[0F   ][10 ][11 ][12 ][13 ][14 ][15 ][16 ][17 ][18 ][19 ][1A ][1B ][2B   ]   [53 ][4F ][51 ]",
            "[3A     ][1E ][1F ][20 ][21 ][22 ][23 ][24 ][25 ][26 ][27 ][28 ][1C      ]                  ",
            "[2A       ][2C ][2D ][2E ][2F ][30 ][31 ][32 ][33 ][34 ][35 ][36         ]        [48 ]     ",
            "[1D   ][    ][38  ][39                          ][    ][    ][    ][1D   ]   [4B ][50 ][4D ]",
        };

        public static void DrawKeyboardMap(Action<Rectangle> drawRow, Action<Rectangle, int, string> drawKey)
        {
            for (var y = 0; y < KeyboardLayout.Length; y++)
            {
                var keyboardLine = KeyboardLayout[y];
                if (drawRow != null)
                    drawRow(new Rectangle(0, y, keyboardLine.Length, 1));

                var x = keyboardLine.IndexOf('[');
                while (x != -1)
                {
                    var x2 = keyboardLine.IndexOf(']', x);

                    var scanCodeString = keyboardLine.Substring(x + 1, 3).Trim();
                    var keyScanCode = scanCodeString.Length > 0 ? int.Parse(scanCodeString, NumberStyles.HexNumber) : 0;

                    var keyName = GetScanCodeKeyName(keyScanCode);
                    // Only allow F-keys to show >1 character names. The rest we'll remove for now.
                    if ((keyName.Length > 1) && !new[] { 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x57, 0x58 }.Contains(keyScanCode))
                        keyName = "";

                    if (drawKey != null)
                        drawKey(new Rectangle(x, y, x2 - x + 1, 1), keyScanCode, keyName);

                    x = keyboardLine.IndexOf('[', x2);
                }
            }
        }

        IEnumerable<UserCommands> GetScanCodeCommands(int scanCode)
        {
            return GetCommands().Where(uc => (Commands[(int)uc] is UserCommandKeyInput) && ((Commands[(int)uc] as UserCommandKeyInput).ScanCode == scanCode));
        }

        public Color GetScanCodeColor(int scanCode)
        {
            // These should be placed in order of priority - the first found match is used.
            var prefixesToColors = new List<KeyValuePair<string, Color>>()
            {
                new KeyValuePair<string, Color>("ControlReverser", Color.DarkGreen),
                new KeyValuePair<string, Color>("ControlThrottle", Color.DarkGreen),
                new KeyValuePair<string, Color>("ControlTrainBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlEngineBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlDynamicBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlBrakeHose", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlEmergency", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlBailOff", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlInitializeBrakes", Color.DarkRed),
                new KeyValuePair<string, Color>("Control", Color.DarkBlue),
                new KeyValuePair<string, Color>("Camera", Color.Orange),
                new KeyValuePair<string, Color>("Display", Color.DarkGoldenrod),
                //new KeyValuePair<string, Color>("Game", Color.Blue),
                new KeyValuePair<string, Color>("", Color.Gray),
            };

            foreach (var prefixToColor in prefixesToColors)
                foreach (var command in GetScanCodeCommands(scanCode))
                    if (command.ToString().StartsWith(prefixToColor.Key))
                        return prefixToColor.Value;

            return Color.TransparentBlack;
        }

        public void DumpToText(string filePath)
        {
            using (var writer = new StreamWriter(File.OpenWrite(filePath)))
            {
                writer.WriteLine("{0,-40}{1,-40}{2}", "Command", "Key", "Unique Inputs");
                writer.WriteLine(new String('=', 40 * 3));
                foreach (var command in GetCommands())
                    writer.WriteLine("{0,-40}{1,-40}{2}", GetPrettyCommandName(command), Commands[(int)command], String.Join(", ", Commands[(int)command].GetUniqueInputs().OrderBy(s => s).ToArray()));
            }
        }

        public void DumpToGraphic(string filePath)
        {
            var keyWidth = 50;
            var keyHeight = 4 * keyWidth;
            var keySpacing = 5;
            var keyFontLabel = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, keyHeight * 0.33f, System.Drawing.GraphicsUnit.Pixel);
            var keyFontCommand = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, keyHeight * 0.22f, System.Drawing.GraphicsUnit.Pixel);
            var keyboardLayoutBitmap = new System.Drawing.Bitmap(KeyboardLayout[0].Length * keyWidth, KeyboardLayout.Length * keyHeight);
            using (var g = System.Drawing.Graphics.FromImage(keyboardLayoutBitmap))
            {
                DrawKeyboardMap(null, (keyBox, keyScanCode, keyName) =>
                {
                    var keyCommands = GetScanCodeCommands(keyScanCode);
                    var keyCommandNames = String.Join("\n", keyCommands.Select(c => String.Join(" ", GetPrettyCommandName(c).Split(' ').Skip(1).ToArray())).ToArray());

                    var keyColor = GetScanCodeColor(keyScanCode);
                    var keyTextColor = System.Drawing.Brushes.Black;
                    if (keyColor == Color.TransparentBlack)
                    {
                        keyColor = Color.White;
                    }
                    else
                    {
                        keyColor.R += (byte)((255 - keyColor.R) * 2 / 3);
                        keyColor.G += (byte)((255 - keyColor.G) * 2 / 3);
                        keyColor.B += (byte)((255 - keyColor.B) * 2 / 3);
                    }

                    Scale(ref keyBox, keyWidth, keyHeight);
                    keyBox.Inflate(-keySpacing, -keySpacing);

                    g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)keyColor.PackedValue)), keyBox.Left, keyBox.Top, keyBox.Width, keyBox.Height);
                    g.DrawRectangle(System.Drawing.Pens.Black, keyBox.Left, keyBox.Top, keyBox.Width, keyBox.Height);
                    g.DrawString(keyName, keyFontLabel, keyTextColor, keyBox.Right - g.MeasureString(keyName, keyFontLabel).Width + keySpacing, keyBox.Top - 3 * keySpacing);
                    g.DrawString(keyCommandNames, keyFontCommand, keyTextColor, keyBox.Left, keyBox.Bottom - keyCommands.Count() * keyFontCommand.Height);
                });
            }
            keyboardLayoutBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public static void Scale(ref Rectangle rectangle, int scaleX, int scaleY)
        {
            rectangle.X *= scaleX;
            rectangle.Y *= scaleY;
            rectangle.Width *= scaleX;
            rectangle.Height *= scaleY;
        }

        #region Default Input Settings
        static void InitializeCommands(UserCommandInput[] Commands)
        {
            // All UserCommandModifierInput commands go here.
            Commands[(int)UserCommands.GameSwitchWithMouse] = new UserCommandModifierInput(KeyModifiers.Alt);
            Commands[(int)UserCommands.DisplayNextWindowTab] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraMoveFast] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.GameSuspendOldPlayer] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraMoveSlow] = new UserCommandModifierInput(KeyModifiers.Control);

            // Everything else goes here, sorted alphabetically please (and grouped by first word of name).
            Commands[(int)UserCommands.CameraBrakeman] = new UserCommandKeyInput(0x07);
			Commands[(int)UserCommands.CameraCab] = new UserCommandKeyInput(0x02);
			Commands[(int)UserCommands.CameraThreeDimensionalCab] = new UserCommandKeyInput(0x02, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarFirst] = new UserCommandKeyInput(0x47, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarLast] = new UserCommandKeyInput(0x4F, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarNext] = new UserCommandKeyInput(0x49, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarPrevious] = new UserCommandKeyInput(0x51, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraFree] = new UserCommandKeyInput(0x09);
            Commands[(int)UserCommands.CameraHeadOutBackward] = new UserCommandKeyInput(0x4F);
            Commands[(int)UserCommands.CameraHeadOutForward] = new UserCommandKeyInput(0x47);
            Commands[(int)UserCommands.CameraJumpBackPlayer] = new UserCommandKeyInput(0x0A);
            Commands[(int)UserCommands.CameraJumpingTrains] = new UserCommandKeyInput(0x0A, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraJumpSeeSwitch] = new UserCommandKeyInput(0x22, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraOutsideFront] = new UserCommandKeyInput(0x03);
            Commands[(int)UserCommands.CameraOutsideRear] = new UserCommandKeyInput(0x04);
            Commands[(int)UserCommands.CameraPanDown] = new UserCommandModifiableKeyInput(0x50, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanLeft] = new UserCommandModifiableKeyInput(0x4B, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanRight] = new UserCommandModifiableKeyInput(0x4D, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanUp] = new UserCommandModifiableKeyInput(0x48, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPassenger] = new UserCommandKeyInput(0x06);
            Commands[(int)UserCommands.CameraPreviousFree] = new UserCommandKeyInput(0x09, KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraReset] = new UserCommandKeyInput(0x09, KeyModifiers.Control);
            Commands[(int)UserCommands.CameraRotateDown] = new UserCommandModifiableKeyInput(0x50, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateLeft] = new UserCommandModifiableKeyInput(0x4B, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateRight] = new UserCommandModifiableKeyInput(0x4D, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateUp] = new UserCommandModifiableKeyInput(0x48, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraScrollLeft] = new UserCommandModifiableKeyInput(0x4B, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraScrollRight] = new UserCommandModifiableKeyInput(0x4D, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraChangePassengerViewPoint] = new UserCommandKeyInput(0x06, KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraToggleShowCab] = new UserCommandKeyInput(0x02, KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraTrackside] = new UserCommandKeyInput(0x05);
            Commands[(int)UserCommands.CameraSpecialTracksidePoint] = new UserCommandKeyInput(0x05, KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraVibrate] = new UserCommandKeyInput(0x2F, KeyModifiers.Control);
            Commands[(int)UserCommands.CameraZoomIn] = new UserCommandModifiableKeyInput(0x49, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraZoomOut] = new UserCommandModifiableKeyInput(0x51, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);

            Commands[(int)UserCommands.ControlAlerter] = new UserCommandKeyInput(0x2C);
            Commands[(int)UserCommands.ControlBackwards] = new UserCommandKeyInput(0x1F);
            Commands[(int)UserCommands.ControlBailOff] = new UserCommandKeyInput(0x35);
            Commands[(int)UserCommands.ControlBell] = new UserCommandKeyInput(0x30);
            Commands[(int)UserCommands.ControlBellToggle] = new UserCommandKeyInput(0x30, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlBlowerDecrease] = new UserCommandKeyInput(0x31, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlBlowerIncrease] = new UserCommandKeyInput(0x31);
            Commands[(int)UserCommands.ControlSteamHeatDecrease] = new UserCommandKeyInput(0x20, KeyModifiers.Alt);
            Commands[(int)UserCommands.ControlSteamHeatIncrease] = new UserCommandKeyInput(0x16, KeyModifiers.Alt);
            Commands[(int)UserCommands.ControlBrakeHoseConnect] = new UserCommandKeyInput(0x2B);
            Commands[(int)UserCommands.ControlBrakeHoseDisconnect] = new UserCommandKeyInput(0x2B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlCabRadio] = new UserCommandKeyInput(0x13, KeyModifiers.Alt);
            Commands[(int)UserCommands.ControlCircuitBreakerClosingOrder] = new UserCommandKeyInput(0x18);
            Commands[(int)UserCommands.ControlCircuitBreakerOpeningOrder] = new UserCommandKeyInput(0x17);
            Commands[(int)UserCommands.ControlCircuitBreakerClosingAuthorization] = new UserCommandKeyInput(0x18, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlCylinderCocks] = new UserCommandKeyInput(0x2E);
            Commands[(int)UserCommands.ControlSmallEjectorIncrease] = new UserCommandKeyInput(0x24);
            Commands[(int)UserCommands.ControlSmallEjectorDecrease] = new UserCommandKeyInput(0x24, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlCylinderCompound] = new UserCommandKeyInput(0x19);
            Commands[(int)UserCommands.ControlDamperDecrease] = new UserCommandKeyInput(0x32, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDamperIncrease] = new UserCommandKeyInput(0x32);
            Commands[(int)UserCommands.ControlDieselHelper] = new UserCommandKeyInput(0x15, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlDieselPlayer] = new UserCommandKeyInput(0x15, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDoorLeft] = new UserCommandKeyInput(0x10);
            Commands[(int)UserCommands.ControlDoorRight] = new UserCommandKeyInput(0x10, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDynamicBrakeDecrease] = new UserCommandKeyInput(0x33);
            Commands[(int)UserCommands.ControlDynamicBrakeIncrease] = new UserCommandKeyInput(0x34);
            Commands[(int)UserCommands.ControlEmergencyPushButton] = new UserCommandKeyInput(0x0E);
            Commands[(int)UserCommands.ControlEngineBrakeDecrease] = new UserCommandKeyInput(0x1A);
            Commands[(int)UserCommands.ControlEngineBrakeIncrease] = new UserCommandKeyInput(0x1B);
            Commands[(int)UserCommands.ControlFireboxClose] = new UserCommandKeyInput(0x21, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFireboxOpen] = new UserCommandKeyInput(0x21);
            Commands[(int)UserCommands.ControlFireShovelFull] = new UserCommandKeyInput(0x13, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlFiring] = new UserCommandKeyInput(0x21, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlFiringRateDecrease] = new UserCommandKeyInput(0x13, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFiringRateIncrease] = new UserCommandKeyInput(0x13);
            Commands[(int)UserCommands.ControlForwards] = new UserCommandKeyInput(0x11);
            Commands[(int)UserCommands.ControlGearDown] = new UserCommandKeyInput(0x12, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlGearUp] = new UserCommandKeyInput(0x12);
            Commands[(int)UserCommands.ControlHandbrakeFull] = new UserCommandKeyInput(0x28, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHandbrakeNone] = new UserCommandKeyInput(0x27, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHeadlightDecrease] = new UserCommandKeyInput(0x23, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHeadlightIncrease] = new UserCommandKeyInput(0x23);
            Commands[(int)UserCommands.ControlHorn] = new UserCommandKeyInput(0x39);
            Commands[(int)UserCommands.ControlImmediateRefill] = new UserCommandKeyInput(0x14, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInitializeBrakes] = new UserCommandKeyInput(0x35, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1] = new UserCommandKeyInput(0x17);
            Commands[(int)UserCommands.ControlInjector1Decrease] = new UserCommandKeyInput(0x25, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1Increase] = new UserCommandKeyInput(0x25);
            Commands[(int)UserCommands.ControlInjector2] = new UserCommandKeyInput(0x18);
            Commands[(int)UserCommands.ControlInjector2Decrease] = new UserCommandKeyInput(0x26, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector2Increase] = new UserCommandKeyInput(0x26);
            Commands[(int)UserCommands.ControlLight] = new UserCommandKeyInput(0x26);
            Commands[(int)UserCommands.ControlMirror] = new UserCommandKeyInput(0x2F, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlPantograph1] = new UserCommandKeyInput(0x19);
            Commands[(int)UserCommands.ControlPantograph2] = new UserCommandKeyInput(0x19, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlOdoMeterShowHide] = new UserCommandKeyInput(0x2C, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlOdoMeterReset] = new UserCommandKeyInput(0x2C, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlOdoMeterDirection] = new UserCommandKeyInput(0x2C, KeyModifiers.Control | KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRefill] = new UserCommandKeyInput(0x14);
            Commands[(int)UserCommands.ControlRetainersOff] = new UserCommandKeyInput(0x1A, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRetainersOn] = new UserCommandKeyInput(0x1B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlSander] = new UserCommandKeyInput(0x2D);
            Commands[(int)UserCommands.ControlSanderToggle] = new UserCommandKeyInput(0x2D, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlThrottleDecrease] = new UserCommandKeyInput(0x1E);
            Commands[(int)UserCommands.ControlThrottleIncrease] = new UserCommandKeyInput(0x20);
            Commands[(int)UserCommands.ControlThrottleZero] = new UserCommandKeyInput(0x1E, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlTrainBrakeDecrease] = new UserCommandKeyInput(0x27);
            Commands[(int)UserCommands.ControlTrainBrakeIncrease] = new UserCommandKeyInput(0x28);
            Commands[(int)UserCommands.ControlTrainBrakeZero] = new UserCommandKeyInput(0x27, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlTroughRefill] = new UserCommandKeyInput(0x15);
            Commands[(int)UserCommands.ControlTurntableClockwise] = new UserCommandKeyInput(0x2E, KeyModifiers.Alt);
            Commands[(int)UserCommands.ControlTurntableCounterclockwise] = new UserCommandKeyInput(0x2E, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlAIFireOn] = new UserCommandKeyInput(0x23, KeyModifiers.Alt);
            Commands[(int)UserCommands.ControlAIFireOff] = new UserCommandKeyInput(0x23, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlAIFireReset] = new UserCommandKeyInput(0x23, KeyModifiers.Control | KeyModifiers.Alt);

            Commands[(int)UserCommands.ControlWiper] = new UserCommandKeyInput(0x2F);

            Commands[(int)UserCommands.DebugClockBackwards] = new UserCommandKeyInput(0x0C);
            Commands[(int)UserCommands.DebugClockForwards] = new UserCommandKeyInput(0x0D);
            Commands[(int)UserCommands.DebugDumpKeymap] = new UserCommandKeyInput(0x3B, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugFogDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Shift);
            Commands[(int)UserCommands.DebugFogIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Shift);
            Commands[(int)UserCommands.DebugLockShadows] = new UserCommandKeyInput(0x1F, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugLogger] = new UserCommandKeyInput(0x58);
            Commands[(int)UserCommands.DebugLogRenderFrame] = new UserCommandKeyInput(0x58, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugOvercastDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugOvercastIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugPhysicsForm] = new UserCommandKeyInput(0x3D, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugPrecipitationDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugPrecipitationIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugPrecipitationLiquidityDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugPrecipitationLiquidityIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugResetWheelSlip] = new UserCommandKeyInput(0x2D, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSignalling] = new UserCommandKeyInput(0x57, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSoundForm] = new UserCommandKeyInput(0x1F, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSpeedDown] = new UserCommandKeyInput(0x51, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSpeedReset] = new UserCommandKeyInput(0x47, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSpeedUp] = new UserCommandKeyInput(0x49, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugToggleAdvancedAdhesion] = new UserCommandKeyInput(0x2D, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugTracks] = new UserCommandKeyInput(0x40, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugWeatherChange] = new UserCommandKeyInput(0x19, KeyModifiers.Alt);

            Commands[(int)UserCommands.DisplayTrainListWindow] = new UserCommandKeyInput(0x43, KeyModifiers.Alt);
            Commands[(int)UserCommands.DisplayBasicHUDToggle] = new UserCommandKeyInput(0x3F, KeyModifiers.Alt);
            Commands[(int)UserCommands.DisplayCarLabels] = new UserCommandModifiableKeyInput(0x41, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplayCompassWindow] = new UserCommandKeyInput(0x0B);
            Commands[(int)UserCommands.DisplayHelpWindow] = new UserCommandModifiableKeyInput(0x3B, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplayHUD] = new UserCommandModifiableKeyInput(0x3F, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplayNextStationWindow] = new UserCommandKeyInput(0x44);
            Commands[(int)UserCommands.DisplayStationLabels] = new UserCommandModifiableKeyInput(0x40, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplaySwitchWindow] = new UserCommandKeyInput(0x42);
            Commands[(int)UserCommands.DisplayTrackMonitorWindow] = new UserCommandKeyInput(0x3E);
            Commands[(int)UserCommands.DisplayTrainOperationsWindow] = new UserCommandKeyInput(0x43);

            Commands[(int)UserCommands.GameAutopilotMode] = new UserCommandKeyInput(0x1E, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameChangeCab] = new UserCommandKeyInput(0x12, KeyModifiers.Control);
            Commands[(int)UserCommands.GameClearSignalBackward] = new UserCommandKeyInput(0x0F, KeyModifiers.Shift);
            Commands[(int)UserCommands.GameClearSignalForward] = new UserCommandKeyInput(0x0F);
            Commands[(int)UserCommands.GameFullscreen] = new UserCommandKeyInput(0x1C, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameMultiPlayerDispatcher] = new UserCommandKeyInput(0x0A, KeyModifiers.Control);
            Commands[(int)UserCommands.GameMultiPlayerTexting] = new UserCommandKeyInput(0x14, KeyModifiers.Control);
            Commands[(int)UserCommands.GamePause] = new UserCommandKeyInput(Keys.Pause);
            Commands[(int)UserCommands.GamePauseMenu] = new UserCommandKeyInput(0x01);
            Commands[(int)UserCommands.GameQuit] = new UserCommandKeyInput(0x3E, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameRequestControl] = new UserCommandKeyInput(0x12, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameResetSignalBackward] = new UserCommandKeyInput(0x0F, KeyModifiers.Control | KeyModifiers.Shift);
            Commands[(int)UserCommands.GameResetSignalForward] = new UserCommandKeyInput(0x0F, KeyModifiers.Control);
            Commands[(int)UserCommands.GameSave] = new UserCommandKeyInput(0x3C);
            Commands[(int)UserCommands.GameScreenshot] = new UserCommandKeyInput(Keys.PrintScreen);
            Commands[(int)UserCommands.GameSignalPicked] = new UserCommandKeyInput(0x22, KeyModifiers.Control);
            Commands[(int)UserCommands.GameSwitchAhead] = new UserCommandKeyInput(0x22);
            Commands[(int)UserCommands.GameSwitchBehind] = new UserCommandKeyInput(0x22, KeyModifiers.Shift);
            Commands[(int)UserCommands.GameSwitchManualMode] = new UserCommandKeyInput(0x32, KeyModifiers.Control);
            Commands[(int)UserCommands.GameSwitchPicked] = new UserCommandKeyInput(0x22, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameUncoupleWithMouse] = new UserCommandKeyInput(0x16);
        }
        #endregion

        bool IsModifier(UserCommands command)
        {
            return Commands[(int)command].GetType() == typeof(UserCommandModifierInput);
        }

        public string CheckForErrors()
        {
            // Make sure all modifiable input commands are synchronized first.
            foreach (var command in Commands)
                if (command is UserCommandModifiableKeyInput)
                    (command as UserCommandModifiableKeyInput).SynchronizeCombine();

            var errors = new List<String>();

            // Check for commands which both require a particular modifier, and ignore it.
            foreach (var command in GetCommands())
            {
                var input = Commands[(int)command];
                var modInput = input as UserCommandModifiableKeyInput;
                if (modInput != null)
                {
                    if (modInput.Shift && modInput.IgnoreShift)
                        errors.Add(catalog.GetStringFmt("{0} requires and is modified by Shift", GetPrettyLocalizedName(command)));
                    if (modInput.Control && modInput.IgnoreControl)
                        errors.Add(catalog.GetStringFmt("{0} requires and is modified by Control", GetPrettyLocalizedName(command)));
                    if (modInput.Alt && modInput.IgnoreAlt)
                        errors.Add(catalog.GetStringFmt("{0} requires and is modified by Alt", GetPrettyLocalizedName(command)));
                }
            }

            // Check for two commands assigned to the same key
            var firstCommand = GetCommands().Min();
            var lastCommand = GetCommands().Max();
            for (var command1 = firstCommand; command1 <= lastCommand; command1++)
            {
                var input1 = Commands[(int)command1];

                // Modifier inputs don't matter as they don't represent any key.
                if (input1 is UserCommandModifierInput)
                    continue;

                for (var command2 = command1 + 1; command2 <= lastCommand; command2++)
                {
                    var input2 = Commands[(int)command2];

                    // Modifier inputs don't matter as they don't represent any key.
                    if (input2 is UserCommandModifierInput)
                        continue;

                    // Ignore problems when both inputs are on defaults. (This protects the user somewhat but leaves developers in the dark.)
                    if (input1.PersistentDescriptor == InputSettings.DefaultCommands[(int)command1].PersistentDescriptor && input2.PersistentDescriptor == InputSettings.DefaultCommands[(int)command2].PersistentDescriptor)
                        continue;

                    var unique1 = input1.GetUniqueInputs();
                    var unique2 = input2.GetUniqueInputs();
                    var sharedUnique = unique1.Where(id => unique2.Contains(id));
                    foreach (var uniqueInput in sharedUnique)
                        errors.Add(catalog.GetStringFmt("{0} and {1} both match {2}", GetPrettyLocalizedName(command1), GetPrettyLocalizedName(command2), GetPrettyUniqueInput(uniqueInput)));
                }
            }

            return String.Join("\n", errors.ToArray());
        }

        public static string GetPrettyLocalizedName(Enum value)
        {
            return catalog.GetString(GetStringAttribute.GetPrettyName(value));
        }

        public static string GetPrettyCommandName(UserCommands command)
        {
            var name = command.ToString();
            var nameU = name.ToUpperInvariant();
            var nameL = name.ToLowerInvariant();
            for (var i = name.Length - 1; i > 0; i--)
            {
                if (((name[i - 1] != nameU[i - 1]) && (name[i] == nameU[i])) ||
                    (name[i - 1] == nameL[i - 1]) && (name[i] != nameL[i]))
                {
                    name = name.Insert(i, " ");
                    nameL = nameL.Insert(i, " ");
                }
            }
            return name;
        }

        public static string GetPrettyUniqueInput(string uniqueInput)
        {
            var parts = uniqueInput.Split('+');
            if (parts[parts.Length - 1].StartsWith("0x"))
            {
                var key = int.Parse(parts[parts.Length - 1].Substring(2), NumberStyles.AllowHexSpecifier);
                parts[parts.Length - 1] = GetScanCodeKeyName(key);
            }
            return String.Join(" + ", parts);
        }

        public static Keys GetScanCodeKeys(int scanCode)
        {
            var sc = scanCode;
            if (scanCode >= 0x0100)
                sc = 0xE100 | (scanCode & 0x7F);
            else if (scanCode >= 0x0080)
                sc = 0xE000 | (scanCode & 0x7F);
            return (Keys)MapVirtualKey(sc, MapVirtualKeyType.ScanToVirtualEx);
        }

        public static string GetScanCodeKeyName(int scanCode)
        {
            var xnaName = Enum.GetName(typeof(Keys), GetScanCodeKeys(scanCode));
            var keyName = new String('\0', 32);
            var keyNameLength = GetKeyNameText(scanCode << 16, keyName, keyName.Length);
            keyName = keyName.Substring(0, keyNameLength);

            if (keyName.Length > 0)
            {
                // Pick the XNA key name because:
                //   Pause (0x11D) is mapped to "Right Control".
                //   GetKeyNameText prefers "NUM 9" to "PAGE UP".
                if (!String.IsNullOrEmpty(xnaName) && ((scanCode == 0x11D) || keyName.StartsWith("NUM ", StringComparison.OrdinalIgnoreCase) || keyName.StartsWith(xnaName, StringComparison.OrdinalIgnoreCase) || xnaName.StartsWith(keyName, StringComparison.OrdinalIgnoreCase)))
                    return xnaName;

                return keyName;
            }

            // If we failed to convert the scan code to a name, show the scan code for debugging.
            return String.Format(" [sc=0x{0:X2}]", scanCode);
        }
    }

    /// <summary>
    /// Represents a single user-triggerable keyboard input command.
    /// </summary>
    public abstract class UserCommandInput
    {
        public abstract string PersistentDescriptor { get; set; }

        public virtual bool IsModifier { get { return false; } }

        public abstract bool IsKeyDown(KeyboardState keyboardState);

        public abstract IEnumerable<string> GetUniqueInputs();

        public override string ToString()
        {
            return "";
        }
    }

    /// <summary>
    /// Stores a specific combination of keyboard modifiers for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandModifierInput : UserCommandInput
    {
        public bool Shift { get; private set; }
        public bool Control { get; private set; }
        public bool Alt { get; private set; }

        protected UserCommandModifierInput(bool shift, bool control, bool alt)
        {
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public UserCommandModifierInput(KeyModifiers modifiers)
        : this((modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        protected static bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return (!shift || keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) &&
                (!control || keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                (!alt || keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        }

        public override string PersistentDescriptor
        {
            get
            {
                return String.Format("0,0,{0},{1},{2}", Shift ? 1 : 0, Control ? 1 : 0, Alt ? 1 : 0);
            }
            set
            {
                var parts = value.Split(',');
                if (parts.Length >= 5)
                {
                    Shift = parts[2] != "0";
                    Control = parts[3] != "0";
                    Alt = parts[4] != "0";
                }
            }
        }

        public override bool IsModifier { get { return true; } }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift+");
            if (Control) key = key.Append("Control+");
            if (Alt) key = key.Append("Alt+");
            if (key.Length > 0) key.Length -= 1;
            return new[] { key.ToString() };
        }

        public override string ToString()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift + ");
            if (Control) key = key.Append("Control + ");
            if (Alt) key = key.Append("Alt + ");
            if (key.Length > 0) key.Length -= 3;
            return key.ToString();
        }
    }

    /// <summary>
    /// Stores a key and specific combination of keyboard modifiers for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandKeyInput : UserCommandInput
    {
        public int ScanCode { get; private set; }
        public Keys VirtualKey { get; private set; }
        public bool Shift { get; private set; }
        public bool Control { get; private set; }
        public bool Alt { get; private set; }

        protected UserCommandKeyInput(int scanCode, Keys virtualKey, bool shift, bool control, bool alt)
        {
            Debug.Assert((scanCode >= 1 && scanCode <= 127) || (virtualKey != Keys.None), "Scan code for keyboard input is outside the allowed range of 1-127.");
            ScanCode = scanCode;
            VirtualKey = virtualKey;
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public UserCommandKeyInput(int scancode)
        : this(scancode, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(Keys virtualKey)
        : this(virtualKey, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(int scancode, KeyModifiers modifiers)
        : this(scancode, Keys.None, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        public UserCommandKeyInput(Keys virtualKey, KeyModifiers modifiers)
        : this(0, virtualKey, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        protected Keys Key
        {
            get
            {
                return VirtualKey == Keys.None ? InputSettings.GetScanCodeKeys(ScanCode) : VirtualKey;
            }
        }

        protected static bool IsKeyMatching(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        protected static bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return ((keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) == shift) &&
                ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) == control) &&
                ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) == alt);
        }

        public override string PersistentDescriptor
        {
            get
            {
                return String.Format("{0},{1},{2},{3},{4}", ScanCode, (int)VirtualKey, Shift ? 1 : 0, Control ? 1 : 0, Alt ? 1 : 0);
            }
            set
            {
                var parts = value.Split(',');
                if (parts.Length >= 5)
                {
                    ScanCode = int.Parse(parts[0]);
                    VirtualKey = (Keys)int.Parse(parts[1]);
                    Shift = parts[2] != "0";
                    Control = parts[3] != "0";
                    Alt = parts[4] != "0";
                }
            }
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift+");
            if (Control) key = key.Append("Control+");
            if (Alt) key = key.Append("Alt+");
            if (VirtualKey == Keys.None)
                key.AppendFormat("0x{0:X2}", ScanCode);
            else
                key.Append(VirtualKey);
            return new[] { key.ToString() };
        }

        public override string ToString()
        {
            var key = new StringBuilder();
            if (Shift) key.Append("Shift + ");
            if (Control) key.Append("Control + ");
            if (Alt) key.Append("Alt + ");
            if (VirtualKey == Keys.None)
                key.Append(InputSettings.GetScanCodeKeyName(ScanCode));
            else
                key.Append(VirtualKey);
            return key.ToString();
        }
    }

    /// <summary>
    /// Stores a key, specific combination of keyboard modifiers and a set of keyboard modifiers to ignore for comparison with a <see cref="KeyboardState"/>.
    /// </summary>
    public class UserCommandModifiableKeyInput : UserCommandKeyInput
    {
        public bool IgnoreShift { get; private set; }
        public bool IgnoreControl { get; private set; }
        public bool IgnoreAlt { get; private set; }

        UserCommandModifierInput[] Combine;

        UserCommandModifiableKeyInput(int scanCode, Keys virtualKey, KeyModifiers modifiers, IEnumerable<UserCommandInput> combine)
            : base(scanCode, virtualKey, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
            Combine = combine.Cast<UserCommandModifierInput>().ToArray();
            SynchronizeCombine();
        }

        public UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, params UserCommandInput[] combine)
            : this(scanCode, Keys.None, modifiers, combine)
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, params UserCommandInput[] combine)
            : this(scanCode, KeyModifiers.None, combine)
        {
        }

        public override string PersistentDescriptor
        {
            get
            {
                return String.Format("{0},{1},{2},{3}", base.PersistentDescriptor, IgnoreShift ? 1 : 0, IgnoreControl ? 1 : 0, IgnoreAlt ? 1 : 0);
            }
            set
            {
                base.PersistentDescriptor = value;
                var parts = value.Split(',');
                if (parts.Length >= 8)
                {
                    IgnoreShift = parts[5] != "0";
                    IgnoreControl = parts[6] != "0";
                    IgnoreAlt = parts[7] != "0";
                }
            }
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            var shiftState = IgnoreShift ? keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift) : Shift;
            var controlState = IgnoreControl ? keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl) : Control;
            var altState = IgnoreAlt ? keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt) : Alt;
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, shiftState, controlState, altState);
        }

        public override IEnumerable<string> GetUniqueInputs()
        {
            IEnumerable<string> inputs = new[] { Key.ToString() };

            // This must result in the output being Shift+Control+Alt+key.

            if (IgnoreAlt)
                inputs = inputs.SelectMany(i => new[] { i, "Alt+" + i });
            else if (Alt)
                inputs = inputs.Select(i => "Alt+" + i);

            if (IgnoreControl)
                inputs = inputs.SelectMany(i => new[] { i, "Control+" + i });
            else if (Control)
                inputs = inputs.Select(i => "Control+" + i);

            if (IgnoreShift)
                inputs = inputs.SelectMany(i => new[] { i, "Shift+" + i });
            else if (Shift)
                inputs = inputs.Select(i => "Shift+" + i);

            return inputs;
        }

        public override string ToString()
        {
            var key = new StringBuilder(base.ToString());
            if (IgnoreShift) key.Append(" (+ Shift)");
            if (IgnoreControl) key.Append(" (+ Control)");
            if (IgnoreAlt) key.Append(" (+ Alt)");
            return key.ToString();
        }

        public void SynchronizeCombine()
        {
            IgnoreShift = Combine.Any(c => c.Shift);
            IgnoreControl = Combine.Any(c => c.Control);
            IgnoreAlt = Combine.Any(c => c.Alt);
        }
    }
}
