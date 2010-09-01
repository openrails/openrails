/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;


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

		public static KeyboardState KeyboardState;
		public static MouseState MouseState;        
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;

        public static void Update()
        {
            LastKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
            LastMouseState = MouseState;
            MouseState = Mouse.GetState();
            if (LastKeyboardState != KeyboardState
                || LastMouseState.LeftButton != MouseState.LeftButton 
                || LastMouseState.RightButton != MouseState.RightButton
                || LastMouseState.MiddleButton != MouseState.MiddleButton )
                Changed = true;
        }

        public static void Handled()
        {
            Changed = false;
        }

        public static bool IsKeyDown(Keys key) { return KeyboardState.IsKeyDown(key); }
        public static bool IsKeyUp(Keys key) { return KeyboardState.IsKeyUp(key); }

        public static bool IsShiftDown() { return IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift); }

        public static bool IsAltKeyDown() { return IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt); }
		public static bool IsAltKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && IsAltKeyDown(); }

        public static bool IsCtrlKeyDown() { return IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl); }
        public static bool IsCtrlKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && IsCtrlKeyDown(); }

        public static bool IsPressed(Keys key) { return KeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key); }
        public static bool IsReleased(Keys key) { return KeyboardState.IsKeyUp(key) && LastKeyboardState.IsKeyDown(key); }

        public static bool IsAltPressed(Keys key) { return IsPressed(key) && IsAltKeyDown(); }

		public static bool IsMouseMoved() { return MouseState.X != LastMouseState.X || MouseState.Y != LastMouseState.Y; }

		public static bool IsMouseLeftButtonDown() { return MouseState.LeftButton == ButtonState.Pressed; }

		public static bool IsMouseLeftButtonPressed() { return MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released; }
		public static bool IsMouseLeftButtonReleased() { return MouseState.LeftButton == ButtonState.Released && LastMouseState.LeftButton == ButtonState.Pressed; }
	}
}
