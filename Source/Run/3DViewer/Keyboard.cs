/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;


namespace ORTS
{
    /// <summary>
    /// This class adds ability to detect a key click ( press and release )
    /// to the basic XNA keyboard handling functionality.
    /// </summary>
    public class KeyboardInput
    {
        public KeyboardState KeyboardState;

        private KeyboardState lastKeyboardState;

        public void SetKeyboardState(KeyboardState newState)
        {
            lastKeyboardState = KeyboardState;
            KeyboardState = newState;
        }

        public bool IsKeyDown(Keys key) { return KeyboardState.IsKeyDown(key); }

        public bool IsShiftDown() { return IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift); }

        public bool IsAltKeyDown(Keys key) { return KeyboardState.IsKeyDown(key) && (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)); }

        public bool IsAltKeyDown() { return IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt); }

        public bool IsPressed(Keys key) { return KeyboardState.IsKeyDown(key) && !lastKeyboardState.IsKeyDown(key); }

        public bool IsAltPressed(Keys key) { return IsPressed(key) && (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)); }
    }

}
