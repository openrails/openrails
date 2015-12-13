// COPYRIGHT 2012, 2014 by the Open Rails project.
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

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ORTS.Settings;
using Xna = Microsoft.Xna.Framework.Input;

namespace ORTS
{
    /// <summary>
    /// A form used to edit keyboard input settings, in combination with <see cref="KeyInputControl"/>.
    /// </summary>
    /// <remarks>
    /// <para>This form is opened as a modal dialog by <see cref="KeyInputControl"/> and is not intended to be directly used by other code.</para>
    /// <para>The form hooks the keyboard input (using a low-level hook) and captures all input whilst focused. The captured input is translated in to the appropriate form for the keyboard input setting being modified (i.e. modifier only, or key + modifiers). Only scan codes are used, never virtual keys.</para>
    /// <para>The <see cref="DialogResult"/> indicates the user's response: <see cref="DialogResult.OK"/> for "accept", <see cref="DialogResult.Cancel"/> for "cancel" and <see cref="DialogResult.Ignore"/> for "reset to default".</para>
    /// </remarks>
    public partial class KeyInputEditControl : Form
    {
        readonly UserCommandInput LiveInput;
        readonly bool IsModifier;

        int ScanCode;
        Xna.Keys VirtualKey;
        bool Shift;
        bool Control;
        bool Alt;

        public string PersistentDescriptor
        {
            get
            {
                return String.Format("{0},{1},{2},{3},{4}", ScanCode, (int)VirtualKey, Shift ? 1 : 0, Control ? 1 : 0, Alt ? 1 : 0);
            }
        }

        public KeyInputEditControl(KeyInputControl control)
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            // Use a lambda here so we can capture the 'control' variable
            // only for as long as needed to set our location/size.
            Load += (object sender, EventArgs e) =>
            {
                Location = control.Parent.PointToScreen(control.Location);
                Size = control.Size;
                textBox.Focus();
                HookKeyboard();
            };

            FormClosed += (object sender, FormClosedEventArgs e) =>
            {
                UnhookKeyboard();
            };

            LiveInput = control.UserInput;
            IsModifier = LiveInput.IsModifier;
            var parts = LiveInput.PersistentDescriptor.Split(',');
            if (parts.Length >= 5)
            {
                ScanCode = int.Parse(parts[0]);
                VirtualKey = (Xna.Keys)int.Parse(parts[1]);
                Shift = parts[2] != "0";
                Control = parts[3] != "0";
                Alt = parts[4] != "0";
            }
            UpdateText();
        }

        void UpdateText()
        {
            LiveInput.PersistentDescriptor = PersistentDescriptor;
            textBox.Text = LiveInput.ToString();
        }

        KeyboardProcedure CurrentKeyboardProcedure;
        bool CurrentShift, CurrentControl, CurrentAlt;

        IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (textBox.Focused)
            {
                if (nCode >= 0)
                {
                    var virtualKeyCode = (Xna.Keys)Marshal.ReadInt32(lParam);
                    var scanCode = (int)(Marshal.ReadInt64(lParam) >> 32);

                    switch ((int)wParam)
                    {
                        case WM_KEYDOWN:
                        case WM_SYSKEYDOWN:
                            // Print-screen needs an extended code, so fiddle things a bit.
                            if (virtualKeyCode == Xna.Keys.PrintScreen && scanCode == 0x37)
                                scanCode += 256;

                            // True if the virtual key code is for a modifier (shift, control, alt).
                            var isModifier = false;
                            switch (scanCode)
                            {
                                case 0x2A:
                                case 0x36: CurrentShift = true; isModifier = true; break;
                                case 0x1D: CurrentControl = true; isModifier = true; break;
                                case 0x38: CurrentAlt = true; isModifier = true; break;
                                default: break;
                            }

                            if (!(IsModifier ^ isModifier))
                            {
                                ScanCode = IsModifier ? 0 : scanCode;
                                VirtualKey = Xna.Keys.None;
                                Shift = CurrentShift;
                                Control = CurrentControl;
                                Alt = CurrentAlt;
                                UpdateText();
                            }

                            // Return 1 to disable further processing of this key.
                            return (IntPtr)1;
                        case WM_KEYUP:
                        case WM_SYSKEYUP:
                            switch (scanCode)
                            {
                                case 0x2A:
                                case 0x36: CurrentShift = false; break;
                                case 0x1D: CurrentControl = false; break;
                                case 0x38: CurrentAlt = false; break;
                                default: break;
                            }

                            // Return 1 to disable further processing of this key.
                            return (IntPtr)1;
                    }
                }
            }
            else
            {
                CurrentShift = CurrentControl = CurrentAlt = false;
            }
            return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }



        // Ref http://www.seesharpdot.net/?p=96

        private delegate IntPtr KeyboardProcedure(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProcedure lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        public static IntPtr keyboardHookId = IntPtr.Zero;

        public void HookKeyboard()
        {
            CurrentKeyboardProcedure = HookCallback;
            using (var currentProcess = Process.GetCurrentProcess())
            {
                using (var currentModule = currentProcess.MainModule)
                {
                    Debug.Assert(keyboardHookId == IntPtr.Zero);
                    keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, CurrentKeyboardProcedure, GetModuleHandle(currentModule.ModuleName), 0);
                }
            }
        }

        void UnhookKeyboard()
        {
            if (keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyboardHookId);
                keyboardHookId = IntPtr.Zero;
                CurrentKeyboardProcedure = null;
            }
        }
    }
}
