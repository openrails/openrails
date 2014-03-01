// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// This file is part of ORTS TrackViewer. But it is mainly a copy of RunActivity\Viewer3D\UserInput.cs
// modified for the limited 2D functionality needed here.

// This checks all keys for conflicts.
//#define CHECK_KEYMAP_DUPLICATES

// This logs the raw changes in input state.
//#define DEBUG_RAW_INPUT

// This logs every UserCommandInput change from pressed to released.
//#define DEBUG_USER_INPUT

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

namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// Defining all the methods to deal with user input via mouse or keys.
    /// The main method is Update, which reads and stores the current state
    /// A number of other methods can be used to check whether a key/mouse is pressed or simply down.
    /// </summary>
    public static class TVUserInput
    {
        public static bool Changed = false;  // flag UpdaterProcess that its time to handle keyboard input
        public static bool ComposingMessage = false;
        public static KeyboardState KeyboardState;
        public static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;
        

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys key);

        public static void Update()
        {
            //if (MultiPlayer.MPManager.IsMultiPlayer() && MultiPlayer.MPManager.Instance().ComposingText) return;
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;
            // Make sure we have an "idle" (everything released) keyboard and mouse state if the window isn't active.
            KeyboardState = new KeyboardState(GetKeysWithPrintScreenFix(Keyboard.GetState()));
            MouseState = Mouse.GetState();
            /* this part might be needed for message composing
            if (LastKeyboardState != KeyboardState  && ComposingMessage)
            {
                Changed = false;
                //viewer.ComposeMessageWindow.AppendMessage(KeyboardState.GetPressedKeys(), LastKeyboardState.GetPressedKeys());

                return;
            }
            */

            if (LastKeyboardState != KeyboardState
                || LastMouseState.LeftButton != MouseState.LeftButton
                || LastMouseState.RightButton != MouseState.RightButton
                || LastMouseState.MiddleButton != MouseState.MiddleButton
                || LastMouseState.X != MouseState.X
                || LastMouseState.Y != MouseState.Y
                || LastMouseState.ScrollWheelValue != MouseState.ScrollWheelValue)
            {
                Changed = true;
             
            }
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
#if DEBUG_USER_INPUT
            foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
            {
                if (TVUserInputut.IsPressed(command))
                    Console.WriteLine("Pressed  {0} - {1}", command, TVInputSettings.Commands[(int)command]);
                if (TVUserInputut.IsReleased(command))
                    Console.WriteLine("Released {0} - {1}", command, TVInputSettings.Commands[(int)command]);
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
            Changed = false;
            //if (RDState != null)
            //    RDState.Handled();
        }

        public static bool IsPressed(TVUserCommands command)
        {
            if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsPressed(command))
            //    return true;
            var setting = TVInputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState) && !setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsReleased(TVUserCommands command)
        {
            if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsReleased(command))
            //    return true;
            var setting = TVInputSettings.Commands[(int)command];
            return !setting.IsKeyDown(KeyboardState) && setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsDown(TVUserCommands command)
        {
            if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsDown(command))
            //    return true;
            var setting = TVInputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState);
        }

        public static bool IsMouseMoved() { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; }
        public static int MouseMoveX() { return MouseState.X - LastMouseState.X; }
        public static int MouseMoveY() { return MouseState.Y - LastMouseState.Y; }

        public static bool IsMouseWheelChanged() { return MouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue; }
        public static int MouseWheelChange() { return MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue; }

        public static bool IsMouseLeftButtonDown() { return MouseState.LeftButton == ButtonState.Pressed; }
        public static bool IsMouseLeftButtonPressed() { return MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; }
        public static bool IsMouseLeftButtonReleased() { return MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; }

        public static bool IsMouseMiddleButtonDown() { return MouseState.MiddleButton == ButtonState.Pressed; }
        public static bool IsMouseMiddleButtonPressed() { return MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released; }
        public static bool IsMouseMiddleButtonReleased() { return MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed; }

        public static bool IsMouseRightButtonDown() { return MouseState.RightButton == ButtonState.Pressed; }
        public static bool IsMouseRightButtonPressed() { return MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released; }
        public static bool IsMouseRightButtonReleased() { return MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed; }

        public static bool IsMouseXButton1Down() { return MouseState.XButton1 == ButtonState.Pressed; }
        public static bool IsMouseXButton1Pressed() { return MouseState.XButton1 == ButtonState.Pressed && LastMouseState.XButton1 == ButtonState.Released; }
        public static bool IsMouseXButton1Released() { return MouseState.XButton1 == ButtonState.Released && LastMouseState.XButton1 == ButtonState.Pressed; }

        public static bool IsMouseXButton2Down() { return MouseState.XButton2 == ButtonState.Pressed; }
        public static bool IsMouseXButton2Pressed() { return MouseState.XButton2 == ButtonState.Pressed && LastMouseState.XButton2 == ButtonState.Released; }
        public static bool IsMouseXButton2Released() { return MouseState.XButton2 == ButtonState.Released && LastMouseState.XButton2 == ButtonState.Pressed; }
    }
}
