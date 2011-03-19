/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

// This checks all keys for conflicts.
//#define CHECK_KEYMAP_DUPLICATES

// This logs every UserCommandInput change from pressed to released.
//#define DEBUG_USER_INPUT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ORTS
{
    public static class UserInput
    {
        public static bool Changed = false;  // flag UpdaterProcess that its time to handle keyboard input

        public static UserCommandInput[] Commands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];

        public static KeyboardState KeyboardState;
        public static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;
        public static Vector3 NearPoint;
        public static Vector3 FarPoint;

        public static RailDriverState RDState = null;

        // Keyboard scancodes are basically constant; some keyboards have extra buttons (e.g. UK ones tend to have an
        // extra button next to Left Shift) or move one or two around (e.g. UK ones tend to move 0x2B down one row)
        // but generally this layout is right. Numeric keypad omitted as most keys are just duplicates of the main
        // keys (in two sets, based on Num Lock) and we don't use them. Scancodes are in hex.
        //
        // Break/Pause (0x11D) is handled specially and doesn't use the expect 0x45 scancode.
        //
        // [01 ]   [3B ][3C ][3D ][3E ]   [3F ][40 ][41 ][42 ]   [43 ][44 ][57 ][58 ]   [37 ][46 ][11D]
        // 
        // [29 ][02 ][03 ][04 ][05 ][06 ][07 ][08 ][09 ][0A ][0B ][0C ][0D ][0E     ]   [52 ][47 ][49 ]
        // [0F   ][10 ][11 ][12 ][13 ][14 ][15 ][16 ][17 ][18 ][19 ][1A ][1B ][2B   ]   [53 ][4F ][51 ]
        // [3A     ][1E ][1F ][20 ][21 ][22 ][23 ][24 ][25 ][26 ][27 ][28 ][1C      ]
        // [2A       ][2C ][2D ][2E ][2F ][30 ][31 ][32 ][33 ][34 ][35 ][36         ]        [48 ]
        // [1D   ][    ][38  ][39                          ][    ][    ][    ][1D   ]   [4B ][50 ][4D ]

        public static void Initialize()
        {
            Commands[(int)UserCommands.GameQuit] = new UserCommandKeyInput(0x01);
            Commands[(int)UserCommands.GameFullscreen] = new UserCommandKeyInput(0x1C, KeyModifiers.Alt);
            Commands[(int)UserCommands.GamePause] = new UserCommandKeyInput(0x11D);
            Commands[(int)UserCommands.GameSave] = new UserCommandKeyInput(0x3C);
            Commands[(int)UserCommands.GameSpeedUp] = new UserCommandKeyInput(0x49, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.GameSpeedDown] = new UserCommandKeyInput(0x51, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.GameSpeedReset] = new UserCommandKeyInput(0x47, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.GameOvercastIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Control);
            Commands[(int)UserCommands.GameOvercastDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Control);
            Commands[(int)UserCommands.GameClockForwards] = new UserCommandKeyInput(0x0D);
            Commands[(int)UserCommands.GameClockBackwards] = new UserCommandKeyInput(0x0C);
            Commands[(int)UserCommands.GameODS] = new UserCommandKeyInput(0x3F);
            Commands[(int)UserCommands.GameLogger] = new UserCommandKeyInput(0x58);
            Commands[(int)UserCommands.GameDebugKeys] = new UserCommandKeyInput(0x3B, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameDebugLockShadows] = new UserCommandKeyInput(0x1F, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameDebugLogRenderFrame] = new UserCommandKeyInput(0x58, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameDebugSignalling] = new UserCommandKeyInput(0x57, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameDebugWeatherChange] = new UserCommandKeyInput(0x19, KeyModifiers.Alt);
            Commands[(int)UserCommands.WindowTab] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.WindowHelp] = new UserCommandModifiableKeyInput(0x3B, Commands[(int)UserCommands.WindowTab]);
            Commands[(int)UserCommands.WindowTrackMonitor] = new UserCommandKeyInput(0x3E);
            Commands[(int)UserCommands.WindowSwitch] = new UserCommandKeyInput(0x42);
            Commands[(int)UserCommands.WindowTrainOperations] = new UserCommandKeyInput(0x43);
            Commands[(int)UserCommands.WindowNextStation] = new UserCommandKeyInput(0x44);
            Commands[(int)UserCommands.WindowCompass] = new UserCommandKeyInput(0x0B);
            Commands[(int)UserCommands.CameraCab] = new UserCommandKeyInput(0x02);
            Commands[(int)UserCommands.CameraOutsideFront] = new UserCommandKeyInput(0x03);
            Commands[(int)UserCommands.CameraOutsideRear] = new UserCommandKeyInput(0x04);
            Commands[(int)UserCommands.CameraTrackside] = new UserCommandKeyInput(0x05);
            Commands[(int)UserCommands.CameraPassenger] = new UserCommandKeyInput(0x06);
            Commands[(int)UserCommands.CameraBrakeman] = new UserCommandKeyInput(0x07);
            Commands[(int)UserCommands.CameraFree] = new UserCommandKeyInput(0x09);
            Commands[(int)UserCommands.CameraHeadOutForward] = new UserCommandKeyInput(0x47);
            Commands[(int)UserCommands.CameraHeadOutBackward] = new UserCommandKeyInput(0x4F);
            Commands[(int)UserCommands.CameraToggleShowCab] = new UserCommandKeyInput(0x02, KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraMoveFast] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraMoveSlow] = new UserCommandModifierInput(KeyModifiers.Control);
            Commands[(int)UserCommands.CameraPanLeft] = new UserCommandModifiableKeyInput(0x4B, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanRight] = new UserCommandModifiableKeyInput(0x4D, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanUp] = new UserCommandModifiableKeyInput(0x48, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanDown] = new UserCommandModifiableKeyInput(0x50, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanIn] = new UserCommandModifiableKeyInput(0x49, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanOut] = new UserCommandModifiableKeyInput(0x51, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateLeft] = new UserCommandModifiableKeyInput(0x4B, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateRight] = new UserCommandModifiableKeyInput(0x4D, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateUp] = new UserCommandModifiableKeyInput(0x48, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateDown] = new UserCommandModifiableKeyInput(0x50, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraCarNext] = new UserCommandKeyInput(0x49, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarPrevious] = new UserCommandKeyInput(0x51, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarFirst] = new UserCommandKeyInput(0x47, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarLast] = new UserCommandKeyInput(0x4F, KeyModifiers.Alt);
            Commands[(int)UserCommands.SwitchAhead] = new UserCommandKeyInput(0x22);
            Commands[(int)UserCommands.SwitchBehind] = new UserCommandKeyInput(0x22, KeyModifiers.Shift);
            Commands[(int)UserCommands.SwitchWithMouse] = new UserCommandModifierInput(KeyModifiers.Alt);
            Commands[(int)UserCommands.UncoupleWithMouse] = new UserCommandKeyInput(0x16);
            Commands[(int)UserCommands.LocomotiveSwitch] = new UserCommandKeyInput(0x12, KeyModifiers.Control);
            Commands[(int)UserCommands.LocomotiveFlip] = new UserCommandKeyInput(0x21, KeyModifiers.Shift | KeyModifiers.Control);
            Commands[(int)UserCommands.ResetSignal] = new UserCommandKeyInput(0x0F);
            Commands[(int)UserCommands.ControlForwards] = new UserCommandKeyInput(0x11);
            Commands[(int)UserCommands.ControlBackwards] = new UserCommandKeyInput(0x1F);
            Commands[(int)UserCommands.ControlReverserForward] = new UserCommandKeyInput(0x11);
            Commands[(int)UserCommands.ControlReverserBackwards] = new UserCommandKeyInput(0x1F);
            Commands[(int)UserCommands.ControlThrottleIncrease] = new UserCommandKeyInput(0x20);
            Commands[(int)UserCommands.ControlThrottleDecrease] = new UserCommandKeyInput(0x1E);
            Commands[(int)UserCommands.ControlTrainBrakeIncrease] = new UserCommandKeyInput(0x28);
            Commands[(int)UserCommands.ControlTrainBrakeDecrease] = new UserCommandKeyInput(0x27);
            Commands[(int)UserCommands.ControlEngineBrakeIncrease] = new UserCommandKeyInput(0x1B);
            Commands[(int)UserCommands.ControlEngineBrakeDecrease] = new UserCommandKeyInput(0x1A);
            Commands[(int)UserCommands.ControlDynamicBrakeIncrease] = new UserCommandKeyInput(0x34);
            Commands[(int)UserCommands.ControlDynamicBrakeDecrease] = new UserCommandKeyInput(0x33);
            Commands[(int)UserCommands.ControlBailOff] = new UserCommandKeyInput(0x35);
            Commands[(int)UserCommands.ControlInitializeBrakes] = new UserCommandKeyInput(0x35, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHandbrakeFull] = new UserCommandKeyInput(0x28, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHandbrakeNone] = new UserCommandKeyInput(0x27, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRetainersOn] = new UserCommandKeyInput(0x1B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRetainersOff] = new UserCommandKeyInput(0x1A, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlBrakeHoseConnect] = new UserCommandKeyInput(0x2B);
            Commands[(int)UserCommands.ControlBrakeHoseDisconnect] = new UserCommandKeyInput(0x2B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlEmergency] = new UserCommandKeyInput(0x0E);
            Commands[(int)UserCommands.ControlSander] = new UserCommandKeyInput(0x2D);
            Commands[(int)UserCommands.ControlWiper] = new UserCommandKeyInput(0x2F);
            Commands[(int)UserCommands.ControlHorn] = new UserCommandKeyInput(0x39);
            Commands[(int)UserCommands.ControlBell] = new UserCommandKeyInput(0x30);
            Commands[(int)UserCommands.ControlLight] = new UserCommandKeyInput(0x26);
            Commands[(int)UserCommands.ControlPantograph] = new UserCommandKeyInput(0x19);
            Commands[(int)UserCommands.ControlHeadlightIncrease] = new UserCommandKeyInput(0x23);
            Commands[(int)UserCommands.ControlHeadlightDecrease] = new UserCommandKeyInput(0x23, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDispatcherExtend] = new UserCommandKeyInput(0x0F, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDispatcherRelease] = new UserCommandKeyInput(0x0F, KeyModifiers.Shift | KeyModifiers.Control);
            Commands[(int)UserCommands.ControlInjector1Increase] = new UserCommandKeyInput(0x25);
            Commands[(int)UserCommands.ControlInjector1Decrease] = new UserCommandKeyInput(0x25, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1] = new UserCommandKeyInput(0x17);
            Commands[(int)UserCommands.ControlInjector2Increase] = new UserCommandKeyInput(0x26);
            Commands[(int)UserCommands.ControlInjector2Decrease] = new UserCommandKeyInput(0x26, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector2] = new UserCommandKeyInput(0x18);
            Commands[(int)UserCommands.ControlBlowerIncrease] = new UserCommandKeyInput(0x31);
            Commands[(int)UserCommands.ControlBlowerDecrease] = new UserCommandKeyInput(0x31, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDamperIncrease] = new UserCommandKeyInput(0x32);
            Commands[(int)UserCommands.ControlDamperDecrease] = new UserCommandKeyInput(0x32, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFiringRateIncrease] = new UserCommandKeyInput(0x13);
            Commands[(int)UserCommands.ControlFiringRateDecrease] = new UserCommandKeyInput(0x13, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFireShovelFull] = new UserCommandKeyInput(0x13, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlCylinderCocks] = new UserCommandKeyInput(0x2E);
            Commands[(int)UserCommands.ControlFiring] = new UserCommandKeyInput(0x21, KeyModifiers.Control);
#if CHECK_KEYMAP_DUPLICATES
            var firstUserCommand = Enum.GetValues(typeof(UserCommands)).Cast<UserCommands>().Min();
            var lastUserCommand = Enum.GetValues(typeof(UserCommands)).Cast<UserCommands>().Max();
            for (var outerCommand = firstUserCommand; outerCommand <= lastUserCommand; outerCommand++)
            {
                for (var innerCommand = outerCommand + 1; innerCommand <= lastUserCommand; innerCommand++)
                {
                    var outerCommandUniqueInputs = Commands[(int)outerCommand].UniqueInputs();
                    var innerCommandUniqueInputs = Commands[(int)innerCommand].UniqueInputs();
                    var sharedUniqueInputs = outerCommandUniqueInputs.Where(id => innerCommandUniqueInputs.Contains(id));
                    foreach (var uniqueInput in sharedUniqueInputs)
                        Trace.TraceInformation("Commands {0} and {1} conflict on input {2}.", outerCommand, innerCommand, uniqueInput);
                }
            }
#endif
        }

        public static void Update(Viewer3D viewer)
        {
            LastKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
            LastMouseState = MouseState;
            MouseState = Mouse.GetState();
            if (LastKeyboardState != KeyboardState
                || LastMouseState.LeftButton != MouseState.LeftButton
                || LastMouseState.RightButton != MouseState.RightButton
                || LastMouseState.MiddleButton != MouseState.MiddleButton)
            {
                Changed = true;
                if (MouseState.LeftButton == ButtonState.Pressed)
                {
                    Vector3 nearsource = new Vector3((float)MouseState.X, (float)MouseState.Y, 0f);
                    Vector3 farsource = new Vector3((float)MouseState.X, (float)MouseState.Y, 1f);
                    Matrix world = Matrix.CreateTranslation(0, 0, 0);
                    NearPoint = viewer.GraphicsDevice.Viewport.Unproject(nearsource, viewer.Camera.XNAProjection, viewer.Camera.XNAView, world);
                    FarPoint = viewer.GraphicsDevice.Viewport.Unproject(farsource, viewer.Camera.XNAProjection, viewer.Camera.XNAView, world);
                }

                if (UserInput.IsPressed(UserCommands.GameDebugKeys))
                {
                    Console.WriteLine();
                    Console.WriteLine("{0,-40}{1,-40}{2}", "Command", "Key", "Unique Inputs");
                    Console.WriteLine(new String('=', 40 * 3));
                    foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
                        Console.WriteLine("{0,-40}{1,-40}{2}", UserInput.FormatCommandName(command), Commands[(int)command], String.Join(", ", Commands[(int)command].UniqueInputs().OrderBy(s => s).ToArray()));
                    Console.WriteLine();
                }
            }
#if DEBUG_USER_INPUT
            foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
            {
                if (UserInput.IsPressed(command))
                    Console.WriteLine("Pressed  {0} - {1}", command, Commands[(int)command]);
                if (UserInput.IsReleased(command))
                    Console.WriteLine("Released {0} - {1}", command, Commands[(int)command]);
            }
#endif
        }

        public static void Handled()
        {
            Changed = false;
            if (RDState != null)
                RDState.Handled();
        }

        public static string FormatCommandName(UserCommands command)
        {
            var name = command.ToString();
            var nameU = name.ToUpperInvariant();
            for (var i = name.Length - 1; i > 0; i--)
            {
                if ((name[i] == nameU[i]) && (name[i - 1] != nameU[i - 1]))
                {
                    name = name.Insert(i, " ");
                    nameU = nameU.Insert(i, " ");
                }
            }
            return name;
        }

        public static bool IsPressed(UserCommands command)
        {
            if (RDState != null && RDState.IsPressed(command))
                return true;
            var setting = Commands[(int)command];
            return setting.IsKeyDown(KeyboardState) && !setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsReleased(UserCommands command)
        {
            if (RDState != null && RDState.IsReleased(command))
                return true;
            var setting = Commands[(int)command];
            return !setting.IsKeyDown(KeyboardState) && setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsDown(UserCommands command)
        {
            if (RDState != null && RDState.IsDown(command))
                return true;
            var setting = Commands[(int)command];
            return setting.IsKeyDown(KeyboardState);
        }

        public static bool IsMouseMoved() { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; }
        public static int MouseMoveX() { return MouseState.X - LastMouseState.X; }
        public static int MouseMoveY() { return MouseState.Y - LastMouseState.Y; }

        public static bool IsMouseLeftButtonDown() { return MouseState.LeftButton == ButtonState.Pressed; }
        public static bool IsMouseLeftButtonPressed() { return MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; }
        public static bool IsMouseLeftButtonReleased() { return MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; }

        public static bool IsMouseMiddleButtonDown() { return MouseState.MiddleButton == ButtonState.Pressed; }
        public static bool IsMouseMiddleButtonPressed() { return MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released; }
        public static bool IsMouseMiddleButtonReleased() { return MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed; }

        public static bool IsMouseRightButtonDown() { return MouseState.RightButton == ButtonState.Pressed; }
        public static bool IsMouseRightButtonPressed() { return MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released; }
        public static bool IsMouseRightButtonReleased() { return MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed; }
    }

    public enum UserCommands
    {
        GameQuit,
        GameFullscreen,
        GamePause,
        GameSave,
        GameSpeedUp,
        GameSpeedDown,
        GameSpeedReset,
        GameOvercastIncrease,
        GameOvercastDecrease,
        GameClockForwards,
        GameClockBackwards,
        GameODS,
        GameLogger,
        GameDebugKeys,
        GameDebugLockShadows,
        GameDebugLogRenderFrame,
        GameDebugSignalling,
        GameDebugWeatherChange,
        WindowTab,
        WindowHelp,
        WindowTrackMonitor,
        WindowSwitch,
        WindowTrainOperations,
        WindowNextStation,
        WindowCompass,
        CameraCab,
        CameraOutsideFront,
        CameraOutsideRear,
        CameraTrackside,
        CameraPassenger,
        CameraBrakeman,
        CameraFree,
        CameraHeadOutForward,
        CameraHeadOutBackward,
        CameraToggleShowCab,
        CameraMoveFast,
        CameraMoveSlow,
        CameraPanLeft,
        CameraPanRight,
        CameraPanUp,
        CameraPanDown,
        CameraPanIn,
        CameraPanOut,
        CameraRotateLeft,
        CameraRotateRight,
        CameraRotateUp,
        CameraRotateDown,
        CameraCarNext,
        CameraCarPrevious,
        CameraCarFirst,
        CameraCarLast,
        SwitchAhead,
        SwitchBehind,
        SwitchWithMouse,
        UncoupleWithMouse,
        LocomotiveSwitch,
        LocomotiveFlip,
        ResetSignal,
        ControlForwards,
        ControlBackwards,
        ControlReverserForward,
        ControlReverserBackwards,
        ControlThrottleIncrease,
        ControlThrottleDecrease,
        ControlTrainBrakeIncrease,
        ControlTrainBrakeDecrease,
        ControlEngineBrakeIncrease,
        ControlEngineBrakeDecrease,
        ControlDynamicBrakeIncrease,
        ControlDynamicBrakeDecrease,
        ControlBailOff,
        ControlInitializeBrakes,
        ControlHandbrakeFull,
        ControlHandbrakeNone,
        ControlRetainersOn,
        ControlRetainersOff,
        ControlBrakeHoseConnect,
        ControlBrakeHoseDisconnect,
        ControlEmergency,
        ControlSander,
        ControlWiper,
        ControlHorn,
        ControlBell,
        ControlLight,
        ControlPantograph,
        ControlHeadlightIncrease,
        ControlHeadlightDecrease,
        ControlDispatcherExtend,
        ControlDispatcherRelease,
        ControlInjector1Increase,
        ControlInjector1Decrease,
        ControlInjector1,
        ControlInjector2Increase,
        ControlInjector2Decrease,
        ControlInjector2,
        ControlBlowerIncrease,
        ControlBlowerDecrease,
        ControlDamperIncrease,
        ControlDamperDecrease,
        ControlFiringRateIncrease,
        ControlFiringRateDecrease,
        ControlFireShovelFull,
        ControlCylinderCocks,
        ControlFiring,
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }

    public abstract class UserCommandInput
    {
        public abstract bool IsKeyDown(KeyboardState keyboardState);

        public abstract IEnumerable<string> UniqueInputs();

        public override string ToString()
        {
            return "";
        }
    }

    public class UserCommandModifierInput : UserCommandInput
    {
        public readonly bool Shift;
        public readonly bool Control;
        public readonly bool Alt;

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

        protected bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return (!shift || keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) &&
                (!control || keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                (!alt || keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> UniqueInputs()
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

    public class UserCommandKeyInput : UserCommandInput
    {
        public readonly int ScanCode;
        public readonly bool Shift;
        public readonly bool Control;
        public readonly bool Alt;

        enum MapType
        {
             VirtualToCharacter = 2,
             VirtualToScan   = 0,
             VirtualToScanEx = 4,
             ScanToVirtual   = 1,
             ScanToVirtualEx = 3,
        }

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int code, MapType mapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetKeyNameText(int scanCode, [Out] string name, int nameLength);

        protected UserCommandKeyInput(int scancode, bool shift, bool control, bool alt)
        {
            ScanCode = scancode;
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public UserCommandKeyInput(int scancode)
            : this(scancode, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(int scancode, KeyModifiers modifiers)
            : this(scancode, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        protected Keys Key
        {
            get
            {
                var sc = ScanCode;
                if (ScanCode >= 0x0100)
                    sc = 0xE100 | (ScanCode & 0x7F);
                else if (ScanCode >= 0x0080)
                    sc = 0xE000 | (ScanCode & 0x7F);
                return (Keys)MapVirtualKey(sc, MapType.ScanToVirtualEx);
            }
        }

        protected bool IsKeyMatching(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        protected bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return ((keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) == shift) &&
                ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) == control) &&
                ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) == alt);
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> UniqueInputs()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift+");
            if (Control) key = key.Append("Control+");
            if (Alt) key = key.Append("Alt+");
            key.AppendFormat("0x{0:X2}", ScanCode);
            return new[] { key.ToString() };
        }

        public override string ToString()
        {
            var key = new StringBuilder();
            if (Shift) key.Append("Shift + ");
            if (Control) key.Append("Control + ");
            if (Alt) key.Append("Alt + ");

            var xnaName = Enum.GetName(typeof(Keys), Key);
            var keyName = new String('\0', 32);
            var keyNameLength = GetKeyNameText(ScanCode << 16, keyName, keyName.Length);
            keyName = keyName.Substring(0, keyNameLength);
            if (keyName.Length > 0)
            {
                // Pause is mapped to "Right Control" and GetKeyNameText prefers "NUM 9" to "PAGE UP" too so pick the
                // XNA key name in these cases.
                if ((ScanCode == 0x11D) || keyName.StartsWith("NUM ", StringComparison.OrdinalIgnoreCase) || keyName.StartsWith(xnaName, StringComparison.OrdinalIgnoreCase) || xnaName.StartsWith(keyName, StringComparison.OrdinalIgnoreCase))
                    key.Append(xnaName);
                else
                    key.Append(keyName);
            }
            else
            {
                // If we failed to convert the scan code to a name, show the scan code for debugging.
                key.AppendFormat(" [sc=0x{0:X2}]", ScanCode);
            }

            return key.ToString();
        }
    }

    public class UserCommandModifiableKeyInput : UserCommandKeyInput
    {
        public readonly bool IgnoreShift;
        public readonly bool IgnoreControl;
        public readonly bool IgnoreAlt;

        UserCommandModifiableKeyInput(int scanCode, bool shift, bool control, bool alt, bool ignoreShift, bool ignoreControl, bool ignoreAlt)
            : base(scanCode, shift, control, alt)
        {
            IgnoreShift = ignoreShift;
            IgnoreControl = ignoreControl;
            IgnoreAlt = ignoreAlt;
        }

        UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, IEnumerable<UserCommandModifierInput> combine)
            : this(scanCode, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0, combine.Any(c => c.Shift), combine.Any(c => c.Control), combine.Any(c => c.Alt))
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, params UserCommandInput[] combine)
            : this(scanCode, modifiers, combine.Cast<UserCommandModifierInput>())
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, params UserCommandInput[] combine)
            : this(scanCode, KeyModifiers.None, combine)
        {
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            var shiftState = IgnoreShift ? keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift) : Shift;
            var controlState = IgnoreControl ? keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl) : Control;
            var altState = IgnoreAlt ? keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt) : Alt;
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, shiftState, controlState, altState);
        }

        public override IEnumerable<string> UniqueInputs()
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
    }
}
