// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team.

// This checks all keys for conflicts.
//#define CHECK_KEYMAP_DUPLICATES

// This logs the raw changes in input state.
//#define DEBUG_RAW_INPUT

// This logs the changes in input state, taking into account any corrections made by the code (e.g. swapped mouse buttons).
//#define DEBUG_INPUT

// This logs every UserCommandInput change from pressed to released.
//#define DEBUG_USER_INPUT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Input;
using Orts.Viewer3D.WebServices.SwitchPanel;
using ORTS.Common.Input;
using ORTS.Settings;
using Game = Orts.Viewer3D.Processes.Game;

namespace Orts.Viewer3D
{
    public static class UserInput
    {
        public static bool ComposingMessage;
        static KeyboardState KeyboardState;
        static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;
        static bool MouseButtonsSwapped;
        public static int MouseSpeedX;
        public static int MouseSpeedY;
        public static RailDriverState RDState;
        public static ExternalDeviceState WebDeviceState = new ExternalDeviceState();

        static InputSettings InputSettings;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys key);

        public static void Update(Game game)
        {
            if (Orts.MultiPlayer.MPManager.IsMultiPlayer() && Orts.MultiPlayer.MPManager.Instance().ComposingText) return;
            if (InputSettings == null) InputSettings = game.Settings.Input;
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;
            // Make sure we have an "idle" (everything released) keyboard and mouse state if the window isn't active.
            KeyboardState = game.IsActive ? new KeyboardState(GetKeysWithPrintScreenFix(Keyboard.GetState())) : new KeyboardState();
            MouseState = game.IsActive ? Mouse.GetState() : new MouseState(0, 0, LastMouseState.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            MouseButtonsSwapped = System.Windows.Forms.SystemInformation.MouseButtonsSwapped;

            MouseSpeedX = Math.Abs(MouseState.X - LastMouseState.X);
            MouseSpeedY = Math.Abs(MouseState.Y - LastMouseState.Y);

#if DEBUG_RAW_INPUT
            for (Keys key = 0; key <= Keys.OemClear; key++)
                if (LastKeyboardState[key] != KeyboardState[key])
                    Console.WriteLine("Keyboard {0} changed to {1}", key, KeyboardState[key]);
            if (LastMouseState.LeftButton != MouseState.LeftButton)
                Console.WriteLine("Mouse left button changed to {0}", MouseState.LeftButton);
            if (LastMouseState.MiddleButton != MouseState.MiddleButton)
                Console.WriteLine("Mouse middle button changed to {0}", MouseState.MiddleButton);
            if (LastMouseState.RightButton != MouseState.RightButton)
                Console.WriteLine("Mouse right button changed to {0}", MouseState.RightButton);
            if (LastMouseState.XButton1 != MouseState.XButton1)
                Console.WriteLine("Mouse X1 button changed to {0}", MouseState.XButton1);
            if (LastMouseState.XButton2 != MouseState.XButton2)
                Console.WriteLine("Mouse X2 button changed to {0}", MouseState.XButton2);
            if (LastMouseState.ScrollWheelValue != MouseState.ScrollWheelValue)
                Console.WriteLine("Mouse scrollwheel changed by {0}", MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue);
#endif
#if DEBUG_INPUT
            var newKeys = GetPressedKeys();
            var oldKeys = GetPreviousPressedKeys();
            foreach (var newKey in newKeys)
                if (!oldKeys.Contains(newKey))
                    Console.WriteLine("Keyboard {0} pressed", newKey);
            foreach (var oldKey in oldKeys)
                if (!newKeys.Contains(oldKey))
                    Console.WriteLine("Keyboard {0} released", oldKey);
            if (IsMouseLeftButtonPressed)
                Console.WriteLine("Mouse left button pressed");
            if (IsMouseLeftButtonReleased)
                Console.WriteLine("Mouse left button released");
            if (IsMouseMiddleButtonPressed)
                Console.WriteLine("Mouse middle button pressed");
            if (IsMouseMiddleButtonReleased)
                Console.WriteLine("Mouse middle button released");
            if (IsMouseRightButtonPressed)
                Console.WriteLine("Mouse right button pressed");
            if (IsMouseRightButtonReleased)
                Console.WriteLine("Mouse right button released");
            if (IsMouseWheelChanged)
                Console.WriteLine("Mouse scrollwheel changed by {0}", MouseWheelChange);
#endif
#if DEBUG_USER_INPUT
            foreach (UserCommand command in Enum.GetValues(typeof(UserCommand)))
            {
                if (UserInput.IsPressed(command))
                    Console.WriteLine("Pressed  {0} - {1}", command, InputSettings.Commands[(int)command]);
                if (UserInput.IsReleased(command))
                    Console.WriteLine("Released {0} - {1}", command, InputSettings.Commands[(int)command]);
            }
#endif
        }

        static Keys[] GetKeysWithPrintScreenFix(KeyboardState keyboardState)
        {
            // When running in fullscreen, Win32's GetKeyboardState (the API behind Keyboard.GetState()) never returns
            // the print screen key as being down. Something is eating it or something. So here we simply query that
            // key directly and forcibly add it to the list of pressed keys.
            var keys = new List<Keys>(keyboardState.GetPressedKeys());
            if ((GetAsyncKeyState(Keys.PrintScreen) & 0x8000) != 0)
                keys.Add(Keys.PrintScreen);
            return keys.ToArray();
        }

        public static void Handled()
        {
            RDState?.Handled();
            WebDeviceState?.Handled();
        }

        public static bool IsPressed(UserCommand command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsPressed(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
            return (setting.IsKeyDown(KeyboardState) && !setting.IsKeyDown(LastKeyboardState)) ||
                SwitchPanelModule.IsDown(command);
        }

        public static bool IsReleased(UserCommand command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsReleased(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
            return (!setting.IsKeyDown(KeyboardState) && setting.IsKeyDown(LastKeyboardState)) ||
                SwitchPanelModule.IsUp(command);
        }

        public static bool IsDown(UserCommand command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsDown(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState);
        }

        public static Keys[] GetPressedKeys() { return KeyboardState.GetPressedKeys(); }
        public static Keys[] GetPreviousPressedKeys() { return LastKeyboardState.GetPressedKeys(); }

        public static bool IsMouseMoved { get { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; } }
        public static int MouseMoveX { get { return MouseState.X - LastMouseState.X; } }
        public static int MouseMoveY { get { return MouseState.Y - LastMouseState.Y; } }
        public static bool MouseMovedUp {  get { return MouseState.Y < LastMouseState.Y; } }
        public static bool MouseMovedDown {  get { return MouseState.Y > LastMouseState.Y; } }
        public static bool MouseMovedLeft {  get { return MouseState.X < LastMouseState.X; } }
        public static bool MouseMovedRight {  get { return MouseState.X > LastMouseState.X; } }
        public static int MouseX { get { return MouseState.X; } }
        public static int MouseY { get { return MouseState.Y; } }

        public static bool IsMouseWheelChanged { get { return MouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue; } }
        public static int MouseWheelChange { get { return MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue; } }

        public static bool IsMouseLeftButtonDown { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Pressed : MouseState.LeftButton == ButtonState.Pressed; } }
        public static bool IsMouseLeftButtonPressed { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released : MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; } }
        public static bool IsMouseLeftButtonReleased { get { return MouseButtonsSwapped ? MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed : MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; } }

        public static bool IsMouseMiddleButtonDown { get { return MouseState.MiddleButton == ButtonState.Pressed; } }
        public static bool IsMouseMiddleButtonPressed { get { return MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released; } }
        public static bool IsMouseMiddleButtonReleased { get { return MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed; } }

        public static bool IsMouseRightButtonDown { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Pressed : MouseState.RightButton == ButtonState.Pressed; } }
        public static bool IsMouseRightButtonPressed { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released : MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released; } }
        public static bool IsMouseRightButtonReleased { get { return MouseButtonsSwapped ? MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed : MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed; } }
    }
}
