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
	/// <summary>
	/// This class adds ability to detect a key click ( press and release )
	/// to the basic XNA keyboard handling functionality.
	/// And forms the starting point for a customizable input scenario
	/// where keyboard and mouse input is converted to symbolic commands and
	/// its these commands that the viewer class responds to.
	/// NOTE - I found the keyboard could only be read in the XNA Game Loop (RenderProcess) thread.
	/// </summary>
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

		public static void Initialize()
		{
			Commands[(int)UserCommands.GameQuit] = new UserCommandKeyInput(Keys.Escape);
			Commands[(int)UserCommands.GameFullscreen] = new UserCommandKeyInput(Keys.Enter, KeyModifiers.Alt);
			Commands[(int)UserCommands.GamePause] = new UserCommandKeyInput(Keys.Pause);
			Commands[(int)UserCommands.GameSave] = new UserCommandKeyInput(Keys.F2);
			Commands[(int)UserCommands.GameSpeedUp] = new UserCommandKeyInput(Keys.PageUp, KeyModifiers.Control | KeyModifiers.Alt);
			Commands[(int)UserCommands.GameSpeedDown] = new UserCommandKeyInput(Keys.PageDown, KeyModifiers.Control | KeyModifiers.Alt);
			Commands[(int)UserCommands.GameSpeedReset] = new UserCommandKeyInput(Keys.Home, KeyModifiers.Control | KeyModifiers.Alt);
			Commands[(int)UserCommands.GameOvercastIncrease] = new UserCommandKeyInput(Keys.OemPlus, KeyModifiers.Control);
			Commands[(int)UserCommands.GameOvercastDecrease] = new UserCommandKeyInput(Keys.OemMinus, KeyModifiers.Control);
			Commands[(int)UserCommands.GameClockForwards] = new UserCommandKeyInput(Keys.OemPlus);
			Commands[(int)UserCommands.GameClockBackwards] = new UserCommandKeyInput(Keys.OemMinus);
			Commands[(int)UserCommands.GameODS] = new UserCommandKeyInput(Keys.F5);
			Commands[(int)UserCommands.GameLogger] = new UserCommandKeyInput(Keys.F12);
			Commands[(int)UserCommands.GameDebugKeys] = new UserCommandKeyInput(Keys.F1, KeyModifiers.Alt);
			Commands[(int)UserCommands.GameDebugLockShadows] = new UserCommandKeyInput(Keys.S, KeyModifiers.Alt);
			Commands[(int)UserCommands.GameDebugLogRenderFrame] = new UserCommandKeyInput(Keys.F12, KeyModifiers.Alt);
			Commands[(int)UserCommands.GameDebugSignalling] = new UserCommandKeyInput(Keys.F11, KeyModifiers.Alt);
			Commands[(int)UserCommands.GameDebugWeatherChange] = new UserCommandKeyInput(Keys.P, KeyModifiers.Alt);
            Commands[(int)UserCommands.WindowTab] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.WindowHelp] = new UserCommandModifiableKeyInput(Keys.F1, Commands[(int)UserCommands.WindowTab]);
            Commands[(int)UserCommands.WindowTrackMonitor] = new UserCommandKeyInput(Keys.F4);
			Commands[(int)UserCommands.WindowSwitch] = new UserCommandKeyInput(Keys.F8);
			Commands[(int)UserCommands.WindowTrainOperations] = new UserCommandKeyInput(Keys.F9);
			Commands[(int)UserCommands.WindowNextStation] = new UserCommandKeyInput(Keys.F10);
			Commands[(int)UserCommands.WindowCompass] = new UserCommandKeyInput('0');
            Commands[(int)UserCommands.CameraCab] = new UserCommandKeyInput('1');
			Commands[(int)UserCommands.CameraOutsideFront] = new UserCommandKeyInput('2');
			Commands[(int)UserCommands.CameraOutsideRear] = new UserCommandKeyInput('3');
			Commands[(int)UserCommands.CameraTrackside] = new UserCommandKeyInput('4');
			Commands[(int)UserCommands.CameraPassenger] = new UserCommandKeyInput('5');
			Commands[(int)UserCommands.CameraBrakeman] = new UserCommandKeyInput('6');
			Commands[(int)UserCommands.CameraFree] = new UserCommandKeyInput('8');
            Commands[(int)UserCommands.CameraHeadOutForward] = new UserCommandKeyInput(Keys.Home);
            Commands[(int)UserCommands.CameraHeadOutBackward] = new UserCommandKeyInput(Keys.End);
            Commands[(int)UserCommands.CameraToggleShowCab] = new UserCommandKeyInput('1', KeyModifiers.Shift);
			Commands[(int)UserCommands.CameraMoveFast] = new UserCommandModifierInput(KeyModifiers.Shift);
			Commands[(int)UserCommands.CameraMoveSlow] = new UserCommandModifierInput(KeyModifiers.Control);
			Commands[(int)UserCommands.CameraPanLeft] = new UserCommandModifiableKeyInput(Keys.Left, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraPanRight] = new UserCommandModifiableKeyInput(Keys.Right, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraPanUp] = new UserCommandModifiableKeyInput(Keys.Up, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraPanDown] = new UserCommandModifiableKeyInput(Keys.Down, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraPanIn] = new UserCommandModifiableKeyInput(Keys.PageUp, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraPanOut] = new UserCommandModifiableKeyInput(Keys.PageDown, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraRotateLeft] = new UserCommandModifiableKeyInput(Keys.Left, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraRotateRight] = new UserCommandModifiableKeyInput(Keys.Right, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraRotateUp] = new UserCommandModifiableKeyInput(Keys.Up, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraRotateDown] = new UserCommandModifiableKeyInput(Keys.Down, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
			Commands[(int)UserCommands.CameraCarNext] = new UserCommandKeyInput(Keys.PageUp, KeyModifiers.Alt);
			Commands[(int)UserCommands.CameraCarPrevious] = new UserCommandKeyInput(Keys.PageDown, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarFirst] = new UserCommandKeyInput(Keys.Home, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarLast] = new UserCommandKeyInput(Keys.End, KeyModifiers.Alt);
			Commands[(int)UserCommands.SwitchAhead] = new UserCommandKeyInput('g');
			Commands[(int)UserCommands.SwitchBehind] = new UserCommandKeyInput('g', KeyModifiers.Shift);
			Commands[(int)UserCommands.SwitchWithMouse] = new UserCommandModifierInput(KeyModifiers.Alt);
			Commands[(int)UserCommands.UncoupleWithMouse] = new UserCommandKeyInput('u');
			Commands[(int)UserCommands.LocomotiveSwitch] = new UserCommandKeyInput('e', KeyModifiers.Control);
			Commands[(int)UserCommands.LocomotiveFlip] = new UserCommandKeyInput('f', KeyModifiers.Shift | KeyModifiers.Control);
			Commands[(int)UserCommands.ResetSignal] = new UserCommandKeyInput(Keys.Tab);
			Commands[(int)UserCommands.ControlForwards] = new UserCommandKeyInput('w');
			Commands[(int)UserCommands.ControlBackwards] = new UserCommandKeyInput('s');
			Commands[(int)UserCommands.ControlReverserForward] = new UserCommandKeyInput('w');
			Commands[(int)UserCommands.ControlReverserBackwards] = new UserCommandKeyInput('s');
			Commands[(int)UserCommands.ControlThrottleIncrease] = new UserCommandKeyInput('d');
			Commands[(int)UserCommands.ControlThrottleDecrease] = new UserCommandKeyInput('a');
			Commands[(int)UserCommands.ControlTrainBrakeIncrease] = new UserCommandKeyInput('\'');
			Commands[(int)UserCommands.ControlTrainBrakeDecrease] = new UserCommandKeyInput(';');
			Commands[(int)UserCommands.ControlEngineBrakeIncrease] = new UserCommandKeyInput(']');
			Commands[(int)UserCommands.ControlEngineBrakeDecrease] = new UserCommandKeyInput('[');
			Commands[(int)UserCommands.ControlDynamicBrakeIncrease] = new UserCommandKeyInput(',');
			Commands[(int)UserCommands.ControlDynamicBrakeDecrease] = new UserCommandKeyInput('.');
			Commands[(int)UserCommands.ControlBailOff] = new UserCommandKeyInput('/');
			Commands[(int)UserCommands.ControlInitializeBrakes] = new UserCommandKeyInput('?');
			Commands[(int)UserCommands.ControlHandbrakeFull] = new UserCommandKeyInput('\'', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlHandbrakeNone] = new UserCommandKeyInput(';', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlRetainersOn] = new UserCommandKeyInput(']', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlRetainersOff] = new UserCommandKeyInput('[', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlBrakeHoseConnect] = new UserCommandKeyInput('\\');
			Commands[(int)UserCommands.ControlBrakeHoseDisconnect] = new UserCommandKeyInput('\\', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlEmergency] = new UserCommandKeyInput(Keys.Back);
			Commands[(int)UserCommands.ControlSander] = new UserCommandKeyInput('x');
			Commands[(int)UserCommands.ControlWiper] = new UserCommandKeyInput('v');
			Commands[(int)UserCommands.ControlHorn] = new UserCommandKeyInput(' ');
			Commands[(int)UserCommands.ControlBell] = new UserCommandKeyInput('b');
			Commands[(int)UserCommands.ControlLight] = new UserCommandKeyInput('l');
			Commands[(int)UserCommands.ControlPantograph] = new UserCommandKeyInput('p');
			Commands[(int)UserCommands.ControlHeadlightIncrease] = new UserCommandKeyInput('h');
			Commands[(int)UserCommands.ControlHeadlightDecrease] = new UserCommandKeyInput('h', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlDispatcherExtend] = new UserCommandKeyInput(Keys.Tab, KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlDispatcherRelease] = new UserCommandKeyInput(Keys.Tab, KeyModifiers.Shift | KeyModifiers.Control);
            Commands[(int)UserCommands.ControlInjector1Increase] = new UserCommandKeyInput('k');
            Commands[(int)UserCommands.ControlInjector1Decrease] = new UserCommandKeyInput('k', KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1] = new UserCommandKeyInput('i');
            Commands[(int)UserCommands.ControlInjector2Increase] = new UserCommandKeyInput('l');
            Commands[(int)UserCommands.ControlInjector2Decrease] = new UserCommandKeyInput('l', KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector2] = new UserCommandKeyInput('o');
            Commands[(int)UserCommands.ControlBlowerIncrease] = new UserCommandKeyInput('n');
            Commands[(int)UserCommands.ControlBlowerDecrease] = new UserCommandKeyInput('n', KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDamperIncrease] = new UserCommandKeyInput('m');
            Commands[(int)UserCommands.ControlDamperDecrease] = new UserCommandKeyInput('m', KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFiringRateIncrease] = new UserCommandKeyInput('r');
            Commands[(int)UserCommands.ControlFiringRateDecrease] = new UserCommandKeyInput('r', KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFireShovelFull] = new UserCommandKeyInput('r', KeyModifiers.Control);
            Commands[(int)UserCommands.ControlCylinderCocks] = new UserCommandKeyInput('c');
            Commands[(int)UserCommands.ControlFiring] = new UserCommandKeyInput('f', KeyModifiers.Control);
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
		public readonly Keys Key;
		public readonly bool Shift;
		public readonly bool Control;
		public readonly bool Alt;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern short VkKeyScan(char ch);

		[DllImport("user32.dll")]
		static extern int MapVirtualKey(int uCode, int uMapType);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern int GetKeyNameText(int lParam, [Out] string lpString, int nSize);

		protected UserCommandKeyInput(Keys key, bool shift, bool control, bool alt)
		{
			if (key == Keys.None) throw new InvalidOperationException("No key specified for UserCommandInput(). VkKeyScan failure?");
			Key = key;
			Shift = shift;
			Control = control;
			Alt = alt;
		}

		public UserCommandKeyInput(Keys key, KeyModifiers modifiers)
			: this(key, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
		{
		}

		public UserCommandKeyInput(Keys key)
			: this(key, KeyModifiers.None)
		{
		}

		public UserCommandKeyInput(char key)
			: this((Keys)(VkKeyScan(key) & 0xFF), (KeyModifiers)((VkKeyScan(key) & 0xFF00) >> 8))
		{
		}

		public UserCommandKeyInput(char key, KeyModifiers modifiers)
			: this((Keys)(VkKeyScan(key) & 0xFF), (KeyModifiers)((VkKeyScan(key) & 0xFF00) >> 8) ^ modifiers)
		{
			var keyModifiers = (KeyModifiers)((VkKeyScan(key) & 0xFF00) >> 8);
			if ((modifiers & keyModifiers) != 0)
				Trace.TraceWarning(String.Format("UserCommandInput character '{0}' has conflicting modifier(s) {1}. Modifiers will be inverted from intended state.", key, modifiers & keyModifiers));
		}

		protected bool IsKeyMatching(KeyboardState keyboardState, Keys key)
		{
			return keyboardState.IsKeyDown(Key);
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
			key.Append(Key);
			return new[] { key.ToString() };
		}

		public override string ToString()
		{
			var key = new StringBuilder();
			if (Shift) key = key.Append("Shift + ");
			if (Control) key = key.Append("Control + ");
			if (Alt) key = key.Append("Alt + ");

			var xnaName = Enum.GetName(typeof(Keys), Key);
			var scanCode = MapVirtualKey((int)Key, 0) << 16;
			if (scanCode != 0)
			{
				var keyName = new String('\0', 32);
				var keyNameLength = GetKeyNameText(scanCode, keyName, keyName.Length);
				keyName = keyName.Substring(0, keyNameLength);
				if (keyName.Length > 0)
				{
					// GetKeyNameText prefers "NUM 9" to "PAGE UP" and so on. Sucks.
					if (keyName.StartsWith("NUM ") || keyName.StartsWith(xnaName, StringComparison.OrdinalIgnoreCase) || xnaName.StartsWith(keyName, StringComparison.OrdinalIgnoreCase))
						key.Append(xnaName);
					else
						key.Append(keyName);
				}
				else
				{
					// If we failed to convert the scan code to a name, include the scan code for debugging.
					key.Append(" [sc=");
					key.Append(scanCode);
					key.Append("]");
				}
			}
			else
			{
				key.Append(xnaName);
			}

			return key.ToString();
		}
	}

	public class UserCommandModifiableKeyInput : UserCommandKeyInput
	{
		public readonly bool IgnoreShift;
		public readonly bool IgnoreControl;
		public readonly bool IgnoreAlt;

		UserCommandModifiableKeyInput(Keys key, bool shift, bool control, bool alt, bool ignoreShift, bool ignoreControl, bool ignoreAlt)
			: base(key, shift, control, alt)
		{
			IgnoreShift = ignoreShift;
			IgnoreControl = ignoreControl;
			IgnoreAlt = ignoreAlt;
		}

		UserCommandModifiableKeyInput(Keys key, KeyModifiers modifiers, IEnumerable<UserCommandModifierInput> combine)
			: this(key, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0, combine.Any(c => c.Shift), combine.Any(c => c.Control), combine.Any(c => c.Alt))
		{
		}

		public UserCommandModifiableKeyInput(Keys key, KeyModifiers modifiers, params UserCommandInput[] combine)
			: this(key, modifiers, combine.Cast<UserCommandModifierInput>())
		{
		}

		public UserCommandModifiableKeyInput(Keys key, params UserCommandInput[] combine)
			: this(key, KeyModifiers.None, combine)
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
