/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


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

		public static void Initialize()
		{
			Commands[(int)UserCommands.GameQuit] = new UserCommandInput(Keys.Escape);
			Commands[(int)UserCommands.GameFullscreen] = new UserCommandInput(Keys.Enter, KeyModifiers.Alt);
			Commands[(int)UserCommands.GamePause] = new UserCommandInput(Keys.Pause);
			Commands[(int)UserCommands.GameHelp] = new UserCommandInput(Keys.F1);
			Commands[(int)UserCommands.GameSave] = new UserCommandInput(Keys.F2);
			Commands[(int)UserCommands.GameSpeedUp] = new UserCommandInput(Keys.PageUp);
			Commands[(int)UserCommands.GameSpeedReset] = new UserCommandInput(Keys.PageDown);
			Commands[(int)UserCommands.GameOvercastIncrease] = new UserCommandInput(Keys.OemPlus, KeyModifiers.Control);
			Commands[(int)UserCommands.GameOvercastDecrease] = new UserCommandInput(Keys.OemMinus, KeyModifiers.Control);
			Commands[(int)UserCommands.GameClockForwards] = new UserCommandInput(Keys.OemPlus);
			Commands[(int)UserCommands.GameClockBackwards] = new UserCommandInput(Keys.OemMinus);
			Commands[(int)UserCommands.GameODS] = new UserCommandInput(Keys.F5);
			Commands[(int)UserCommands.GameLogger] = new UserCommandInput(Keys.F12);
			Commands[(int)UserCommands.WindowTrackMonitor] = new UserCommandInput(Keys.F4);
			Commands[(int)UserCommands.WindowSwitch] = new UserCommandInput(Keys.F8);
			Commands[(int)UserCommands.WindowTrainOperations] = new UserCommandInput(Keys.F9);
			Commands[(int)UserCommands.WindowNextStation] = new UserCommandInput(Keys.F10);
			Commands[(int)UserCommands.WindowCompass] = new UserCommandInput('0');
			Commands[(int)UserCommands.CameraCab] = new UserCommandInput('1');
			Commands[(int)UserCommands.CameraOutsideFront] = new UserCommandInput('2');
			Commands[(int)UserCommands.CameraOutsideRear] = new UserCommandInput('3');
			Commands[(int)UserCommands.CameraTrackside] = new UserCommandInput('4');
			Commands[(int)UserCommands.CameraPassenger] = new UserCommandInput('5');
			Commands[(int)UserCommands.CameraBrakeman] = new UserCommandInput('6');
			Commands[(int)UserCommands.CameraFree] = new UserCommandInput('8');
			Commands[(int)UserCommands.CameraHeadOutForwards] = new UserCommandInput(Keys.Up);
			Commands[(int)UserCommands.CameraHeadOutBackwards] = new UserCommandInput(Keys.Down);
			Commands[(int)UserCommands.CameraShowCab] = new UserCommandInput('1', KeyModifiers.Shift);
			Commands[(int)UserCommands.CameraAltitudeIncrease] = new UserCommandInput(Keys.Up, KeyModifiers.Control);
			Commands[(int)UserCommands.CameraAltitudeDecrease] = new UserCommandInput(Keys.Down, KeyModifiers.Control);
			Commands[(int)UserCommands.CameraCarNext] = new UserCommandInput(Keys.Left, KeyModifiers.Control);
			Commands[(int)UserCommands.CameraCarPrevious] = new UserCommandInput(Keys.Right, KeyModifiers.Control);
			Commands[(int)UserCommands.SwitchAhead] = new UserCommandInput('g');
			Commands[(int)UserCommands.SwitchBehind] = new UserCommandInput('g', KeyModifiers.Shift);
			Commands[(int)UserCommands.LocomotiveSwitch] = new UserCommandInput('e', KeyModifiers.Control);
			Commands[(int)UserCommands.LocomotiveFlip] = new UserCommandInput('f', KeyModifiers.Shift | KeyModifiers.Control);
			Commands[(int)UserCommands.ResetSignal] = new UserCommandInput(Keys.Tab);
			Commands[(int)UserCommands.ControlForwards] = new UserCommandInput('w');
			Commands[(int)UserCommands.ControlBackwards] = new UserCommandInput('s');
			Commands[(int)UserCommands.ControlReverserForward] = new UserCommandInput('w');
			Commands[(int)UserCommands.ControlReverserBackwards] = new UserCommandInput('s');
			Commands[(int)UserCommands.ControlThrottleIncrease] = new UserCommandInput('d');
			Commands[(int)UserCommands.ControlThrottleDecrease] = new UserCommandInput('a');
			Commands[(int)UserCommands.ControlTrainBrakeIncrease] = new UserCommandInput('\'');
			Commands[(int)UserCommands.ControlTrainBrakeDecrease] = new UserCommandInput(';');
			Commands[(int)UserCommands.ControlEngineBrakeIncrease] = new UserCommandInput(']');
			Commands[(int)UserCommands.ControlEngineBrakeDecrease] = new UserCommandInput('[');
			Commands[(int)UserCommands.ControlDynamicBrakeIncrease] = new UserCommandInput(',');
			Commands[(int)UserCommands.ControlDynamicBrakeDecrease] = new UserCommandInput('.');
			Commands[(int)UserCommands.ControlBailOff] = new UserCommandInput('/');
			Commands[(int)UserCommands.ControlInitializeBrakes] = new UserCommandInput('?');
			Commands[(int)UserCommands.ControlHandbrakeFull] = new UserCommandInput('\'', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlHandbrakeNone] = new UserCommandInput(';', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlRetainersOn] = new UserCommandInput(']');
			Commands[(int)UserCommands.ControlRetainersOff] = new UserCommandInput('[');
			Commands[(int)UserCommands.ControlBrakeHoseConnect] = new UserCommandInput('\\');
			Commands[(int)UserCommands.ControlBrakeHoseDisconnect] = new UserCommandInput('\\', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlEmergency] = new UserCommandInput(Keys.Back);
			Commands[(int)UserCommands.ControlSander] = new UserCommandInput('x');
			Commands[(int)UserCommands.ControlWiper] = new UserCommandInput('v');
			Commands[(int)UserCommands.ControlHorn] = new UserCommandInput(' ');
			Commands[(int)UserCommands.ControlBell] = new UserCommandInput('b');
			Commands[(int)UserCommands.ControlLight] = new UserCommandInput('l');
			Commands[(int)UserCommands.ControlPantograph] = new UserCommandInput('p');
			Commands[(int)UserCommands.ControlHeadlightIncrease] = new UserCommandInput('h');
			Commands[(int)UserCommands.ControlHeadlightDecrease] = new UserCommandInput('h', KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlDispatcherExtend] = new UserCommandInput(Keys.Tab, KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlDispatcherRelease] = new UserCommandInput(Keys.Tab, KeyModifiers.Shift | KeyModifiers.Control);
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
			}
		}

		public static void Handled()
		{
			Changed = false;
		}

		static bool IsKeyState(UserCommandInput setting)
		{
			return (KeyboardState.IsKeyDown(setting.Key)) &&
				((KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift)) == setting.Shift) &&
				((KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl)) == setting.Control) &&
				((KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt)) == setting.Alt);
		}

		static bool IsLastKeyState(UserCommandInput setting)
		{
			return (LastKeyboardState.IsKeyDown(setting.Key)) &&
				((LastKeyboardState.IsKeyDown(Keys.LeftShift) || LastKeyboardState.IsKeyDown(Keys.RightShift)) == setting.Shift) &&
				((LastKeyboardState.IsKeyDown(Keys.LeftControl) || LastKeyboardState.IsKeyDown(Keys.RightControl)) == setting.Control) &&
				((LastKeyboardState.IsKeyDown(Keys.LeftAlt) || LastKeyboardState.IsKeyDown(Keys.RightAlt)) == setting.Alt);
		}

		public static bool IsPressed(UserCommands command)
		{
			var setting = Commands[(int)command];
			return IsKeyState(setting) && !IsLastKeyState(setting);
		}

		public static bool IsReleased(UserCommands command)
		{
			var setting = Commands[(int)command];
			return !IsKeyState(setting) && IsLastKeyState(setting);
		}

		public static bool IsDown(UserCommands command)
		{
			var setting = Commands[(int)command];
			return IsKeyState(setting);
		}

		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsKeyDown(Keys key) { return KeyboardState.IsKeyDown(key); }
		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsKeyUp(Keys key) { return KeyboardState.IsKeyUp(key); }

		[Obsolete("UserInput.IsShiftDown() has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsShiftKeyDown() { return IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift); }

		[Obsolete("UserInput.IsAltKeyDown() has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsAltKeyDown() { return IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt); }
		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsAltKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && IsAltKeyDown(); }

		[Obsolete("UserInput.IsCtrlKeyDown() has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsControlKeyDown() { return IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl); }
		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsControlKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && IsControlKeyDown(); }

		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsPressed(Keys key) { return KeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key); }
		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsReleased(Keys key) { return KeyboardState.IsKeyUp(key) && LastKeyboardState.IsKeyDown(key); }

		[Obsolete("Using the enum Microsoft.Xna.Framework.Input.Keys with UserInput has been deprecated because it does not provide for users' keyboard layouts or customizable input keys. To respect the user's configuration, use the ORTS.UserInputCommands enum instead.")]
		public static bool IsAltPressed(Keys key) { return IsPressed(key) && IsAltKeyDown(); }

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
		GameHelp,
		GameSave,
		GameSpeedUp,
		GameSpeedReset,
		GameOvercastIncrease,
		GameOvercastDecrease,
		GameClockForwards,
		GameClockBackwards,
		GameODS,
		GameLogger,
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
		CameraHeadOutForwards,
		CameraHeadOutBackwards,
		CameraShowCab,
		CameraAltitudeIncrease,
		CameraAltitudeDecrease,
		CameraCarNext,
		CameraCarPrevious,
		SwitchAhead,
		SwitchBehind,
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
	}

	[Flags]
	public enum KeyModifiers
	{
		None = 0,
		Shift = 1,
		Control = 2,
		Alt = 4
	}

	public class UserCommandInput
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

		UserCommandInput(Keys key, bool shift, bool control, bool alt)
		{
			if (key == Keys.None) throw new InvalidOperationException("No key specified for UserCommandInput(). VkKeyScan failure?");
			Key = key;
			Shift = shift;
			Control = control;
			Alt = alt;
		}

		public UserCommandInput(Keys key, KeyModifiers modifiers)
			: this(key, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
		{
		}

		public UserCommandInput(Keys key)
			: this(key, KeyModifiers.None)
		{
		}

		public UserCommandInput(char key)
			: this((Keys)(VkKeyScan(key) & 0xFF), (KeyModifiers)((VkKeyScan(key) & 0xFF00) >> 8))
		{
		}

		public UserCommandInput(char key, KeyModifiers modifiers)
			: this(key)
		{
			var keyModifiers = (KeyModifiers)((VkKeyScan(key) & 0xFF00) >> 8);
			if ((modifiers & keyModifiers) != 0)
				Trace.TraceWarning(String.Format("UserCommandInput character '{0}' has conflicting modifier(s) {1}. Modifiers will be inverted from intended state.", key, modifiers & keyModifiers));
			if ((modifiers & KeyModifiers.Shift) != 0) Shift = !Shift;
			if ((modifiers & KeyModifiers.Control) != 0) Control = !Control;
			if ((modifiers & KeyModifiers.Alt) != 0) Alt = !Alt;
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
}
