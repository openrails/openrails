// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

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

namespace ORTS
{
    public static class UserInput
    {
        public static bool Changed = false;  // flag UpdaterProcess that its time to handle keyboard input
        public static bool ComposingMessage = false;
        public static KeyboardState KeyboardState;
        public static MouseState MouseState;
        static KeyboardState LastKeyboardState;
        static MouseState LastMouseState;
        public static Vector3 NearPoint;
        public static Vector3 FarPoint;

        public static RailDriverState RDState = null;

        [DllImport("user32.dll")]
        static extern int GetAsyncKeyState(Keys key);

        public static void Update(Viewer3D viewer)
        {
            if (MultiPlayer.MPManager.IsMultiPlayer() && MultiPlayer.MPManager.Instance().ComposingText) return;
            LastKeyboardState = KeyboardState;
            LastMouseState = MouseState;
            // Make sure we have an "idle" (everything released) keyboard and mouse state if the window isn't active.
            KeyboardState = viewer.RenderProcess.IsActive ? new KeyboardState(GetKeysWithPrintScreenFix(Keyboard.GetState())) : new KeyboardState();
            MouseState = viewer.RenderProcess.IsActive ? Mouse.GetState() : new MouseState(0, 0, LastMouseState.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            if (LastKeyboardState != KeyboardState && viewer.ComposeMessageWindow.Visible == true)
            {
                Changed = false;
                viewer.ComposeMessageWindow.AppendMessage(KeyboardState.GetPressedKeys(), LastKeyboardState.GetPressedKeys());

                return;
            }

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

                if (UserInput.IsPressed(UserCommands.DebugDumpKeymap))
                {
                    InputSettings.DumpToText("Keyboard.txt");
                    viewer.MessagesWindow.AddMessage("Keyboard command list saved to 'keyboard.txt'.", 10);
                    InputSettings.DumpToGraphic("Keyboard.png");
                    viewer.MessagesWindow.AddMessage("Keyboard map saved to 'keyboard.png'.", 10);
                }
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
            Changed = false;
            if (RDState != null)
                RDState.Handled();
        }

        public static bool IsPressed(UserCommands command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsPressed(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
            return setting.IsKeyDown(KeyboardState) && !setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsReleased(UserCommands command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsReleased(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
            return !setting.IsKeyDown(KeyboardState) && setting.IsKeyDown(LastKeyboardState);
        }

        public static bool IsDown(UserCommands command)
        {
            if (ComposingMessage == true) return false;
            if (RDState != null && RDState.IsDown(command))
                return true;
            var setting = InputSettings.Commands[(int)command];
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
    }
}
