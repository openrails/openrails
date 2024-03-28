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

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
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
        /// <summary>Boolean describing whether the keyboard and/or mouse state has been changed</summary>
        public static bool Changed;  // flag UpdaterProcess that its time to handle keyboard input
        //public static bool ComposingMessage;
        static KeyboardState KeyboardState;
        static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;

        /// <summary>Return the current x-location of the mouse pointer</summary>
        public static int MouseLocationX { get { return MouseState.X; } }
        /// <summary>Return the current y-location of the mouse pointer</summary>
        public static int MouseLocationY { get { return MouseState.Y; } }

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys key);

        /// <summary>
        /// Call this to update the mouse and keyboard states.
        /// </summary>
        public static void Update(TrackViewer trackViewer)
        {
            //if (MultiPlayer.MPManager.IsMultiPlayer() && MultiPlayer.MPManager.Instance().ComposingText) return;
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;
            // Make sure we have an "idle" (everything released) keyboard and mouse state if the window isn't active.
            KeyboardState = new KeyboardState(GetKeysWithPrintScreenFix(Keyboard.GetState()));
            MouseState = trackViewer.IsTrackViewerWindowActive ? Mouse.GetState(trackViewer.Window) : new MouseState(0, 0, LastMouseState.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
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
            foreach (TVUserCommands command in Enum.GetValues(typeof(TVUserCommands)))
            {
                if (TVUserInput.IsPressed(command))
                    Console.WriteLine("Pressed  {0} - {1}", command, TVInputSettings.Commands[(int)command]);
                if (TVUserInput.IsReleased(command))
                    Console.WriteLine("Released {0} - {1}", command, TVInputSettings.Commands[(int)command]);
            }
#endif
        }

        static Keys[] GetKeysWithPrintScreenFix(KeyboardState keyboardState)
        {
            // When running in fullscreen, Win32's GetKeyboardState (the API behind Keyboard.GetState()) never returns
            // the print screen key as being down. Something is eating it or something. So here we simply query that
            // key directly and forcibly add it to the list of pressed keys.
            List<Keys> keys = new List<Keys>(keyboardState.GetPressedKeys());
            if ((GetAsyncKeyState(Keys.PrintScreen) & 0x8000) != 0)
                keys.Add(Keys.PrintScreen);
            return keys.ToArray();
        }

        /// <summary>
        /// Legacy routine. Should be called when the inputs have been handled.
        /// </summary>
        public static void Handled()
        {
            Changed = false;
            //if (RDState != null)
            //    RDState.Handled();
        }

        /// <summary>
        /// return whether the key belonging to the given command is pressed since last update
        /// </summary>
        /// <param name="command">The command (a key-combination should have been defined for the command)</param>
        public static bool IsPressed(TVUserCommands command)
        {
            //if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsPressed(command))
            //    return true;
            ORTS.Settings.UserCommandInput setting = TVInputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState) && !setting.IsKeyDown(LastKeyboardState);
        }

        /// <summary>
        /// return whether the key belonging to the given command is released since last update
        /// </summary>
        /// <param name="command">The command (a key-combination should have been defined for the command)</param>
        public static bool IsReleased(TVUserCommands command)
        {
            //if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsReleased(command))
            //    return true;
            ORTS.Settings.UserCommandInput setting = TVInputSettings.Commands[(int)command];
            return !setting.IsKeyDown(KeyboardState) && setting.IsKeyDown(LastKeyboardState);
        }

        /// <summary>
        /// return whether the key belonging to the given command is down
        /// </summary>
        /// <param name="command">The command (a key-combination should have been defined for the command)</param>
        public static bool IsDown(TVUserCommands command)
        {
            //if (ComposingMessage == true) return false;
            //if (RDState != null && RDState.IsDown(command))
            //    return true;
            ORTS.Settings.UserCommandInput setting = TVInputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState);
        }

        ///<summary>Return whether the mouse has moved since last update</summary>
        public static bool IsMouseMoved() { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; }
        ///<summary>Return the amount of x-pixels the mouse moved</summary>
        public static int MouseMoveX() { return MouseState.X - LastMouseState.X; }
        ///<summary>Return the amount of y-pixels the mouse moved</summary>
        public static int MouseMoveY() { return MouseState.Y - LastMouseState.Y; }

        ///<summary>Return whether the mouse wheel has changed since the last update</summary>
        public static bool IsMouseWheelChanged() { return MouseState.ScrollWheelValue != LastMouseState.ScrollWheelValue; }
        ///<summary>Return the amount of change in mousewheel</summary>
        public static int MouseWheelChange() { return MouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue; }

        ///<summary>Return whether left mouse button is down</summary>
        public static bool IsMouseLeftButtonDown() { return MouseState.LeftButton == ButtonState.Pressed; }
        ///<summary>Return whether left mouse button has been pressed since last update</summary>
        public static bool IsMouseLeftButtonPressed() { return MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; }
        ///<summary>Return whether left mouse button has been released since last update</summary>
        public static bool IsMouseLeftButtonReleased() { return MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; }

        ///<summary>Return whether middle mouse button is down</summary>
        public static bool IsMouseMiddleButtonDown() { return MouseState.MiddleButton == ButtonState.Pressed; }
        ///<summary>Return whether middle mouse button has been pressed since last update</summary>
        public static bool IsMouseMiddleButtonPressed() { return MouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released; }
        ///<summary>Return whether middle mouse button has been released since last update</summary>
        public static bool IsMouseMiddleButtonReleased() { return MouseState.MiddleButton == ButtonState.Released && LastMouseState.MiddleButton == ButtonState.Pressed; }

        ///<summary>Return whether right mouse button is down</summary>
        public static bool IsMouseRightButtonDown() { return MouseState.RightButton == ButtonState.Pressed; }
        ///<summary>Return whether right mouse button has been pressed since last update</summary>
        public static bool IsMouseRightButtonPressed() { return MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released; }
        ///<summary>Return whether right mouse button has been released since last update</summary>
        public static bool IsMouseRightButtonReleased() { return MouseState.RightButton == ButtonState.Released && LastMouseState.RightButton == ButtonState.Pressed; }

        ///<summary>Return whether extra mouse button 1 is down</summary>
        public static bool IsMouseXButton1Down() { return MouseState.XButton1 == ButtonState.Pressed; }
        ///<summary>Return whether extra mouse button 1 has been pressed since last update</summary>
        public static bool IsMouseXButton1Pressed() { return MouseState.XButton1 == ButtonState.Pressed && LastMouseState.XButton1 == ButtonState.Released; }
        ///<summary>Return whether extra mouse button 1 has been released since last update</summary>
        public static bool IsMouseXButton1Released() { return MouseState.XButton1 == ButtonState.Released && LastMouseState.XButton1 == ButtonState.Pressed; }

        ///<summary>Return whether extra mouse button 2 is down</summary>
        public static bool IsMouseXButton2Down() { return MouseState.XButton2 == ButtonState.Pressed; }
        ///<summary>Return whether extra mouse button 2 has been pressed since last update</summary>
        public static bool IsMouseXButton2Pressed() { return MouseState.XButton2 == ButtonState.Pressed && LastMouseState.XButton2 == ButtonState.Released; }
        ///<summary>Return whether extra mouse button 2 has been released since last update</summary>
        public static bool IsMouseXButton2Released() { return MouseState.XButton2 == ButtonState.Released && LastMouseState.XButton2 == ButtonState.Pressed; }
    }
}
