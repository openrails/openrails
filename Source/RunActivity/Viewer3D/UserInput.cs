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
        public static bool Ready = false;  // flag UpdaterProcess that its time to handle keyboard input

        public static MouseState MouseState;        
        public static KeyboardState KeyboardState;   
        private static KeyboardState lastKeyboardState;
        private static MouseState lastMouseState;

        public static void Update()
        {
            lastKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
            lastMouseState = MouseState;
            MouseState = Mouse.GetState();
            if (lastKeyboardState != KeyboardState
                || lastMouseState.LeftButton != MouseState.LeftButton 
                || lastMouseState.RightButton != MouseState.RightButton
                || lastMouseState.MiddleButton != MouseState.MiddleButton )
                Ready = true;
        }

        public static void Handled()
        {
            Ready = false;
        }

        public static bool IsKeyDown(Keys key) { return KeyboardState.IsKeyDown(key); }

        public static bool IsShiftDown() { return IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift); }

        public static bool IsAltKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)); }

        public static bool IsCtrlKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && (IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl)); }

        public static bool IsCtrlKeyDown() { return (IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl)); }

        public static bool IsAltKeyDown() { return IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt); }

        public static bool IsPressed(Keys key) { return KeyboardState.IsKeyDown(key) && !lastKeyboardState.IsKeyDown(key); }

        public static bool IsAltPressed(Keys key) { return IsPressed(key) && (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)); }
    }

}
