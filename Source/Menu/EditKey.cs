// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.
//  
// EDITKEY 
// 
// This is a form used during keyboard configuration.
//
// Its run as a modal dialog, permitting the user to enter a keystroke 
// along with an optional CTRL, ALT SHIFT combination.
// The dialog hooks the window's keyboard handler to ensure it sees
// all keystrokes.
//
// The dialog can exit with result OK, Cancel, or Ignore with the latter
// indicating that the user wishes to restore the original programmer's defaults.
//
// When the dialog is initialized with a ScanCode of 0, and an XNAKey of None,
// it indicates that the dialog should accept just CTRL, ALT, DEL combinations
// for use in modifier commands such as GameSwitchWithMouse.
//
// Settings are passed in and out with public fields:
//    ScanCode
//    XNAKey     ( see UserInput.cs for explanation )
//    Control
//    Alt
//    Shift



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XNA = Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ORTS
{
    public partial class EditKey : Form
    {
        public EditKey()
        {
            InitializeComponent();
        }

        public int ScanCode = 0;
        public XNA.Keys XNAKey = XNA.Keys.None;
        public bool Control = false;
        public bool Alt = false;
        public bool Shift = false;

        public static Color EditColor { get { return System.Drawing.Color.DarkKhaki; } }

        public bool IsModifier()
        {
            return ScanCode == 0 && XNAKey == XNA.Keys.None;
        }

        public void InitFrom(KeyPressControl control)
        {
            UserCommandInput userCommand = InputSettings.Commands[(int)control.eCommand];
            this.Width = control.Width;
            this.textBox.BackColor = EditColor;
            this.textBox.Location = new Point(0,1);
            this.textBox.Height = control.Height;
            this.textBox.Width = control.Width - this.buttonOK.Width - this.buttonCancel.Width - this.buttonDefault.Width  + 3;
            this.buttonOK.Location = new Point(this.textBox.Width - 1, -1);
            this.buttonOK.Height = control.Height;
            this.buttonCancel.Location = new Point(this.buttonOK.Location.X + this.buttonOK.Width -1 , -1);
            this.buttonCancel.Height = control.Height;
            this.buttonDefault.Location = new Point(this.buttonCancel.Location.X + this.buttonCancel.Width -1, -1);
            this.buttonDefault.Height = control.Height;
            userCommand.ToValue(out ScanCode, out XNAKey, out Control, out Alt, out Shift);
            this.textBox.Text = KeyToString();
        }

        public void InitFrom(UserCommandInput userCommand)
        {
            userCommand.ToValue(out ScanCode, out XNAKey, out Control, out Alt, out Shift);
            this.textBox.Text = KeyToString();
        }

        private string KeyToString()
        {
            return InputSettings.KeyAssignmentAsString(Control, Alt, Shift, ScanCode, XNAKey);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
        private void buttonDefault_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Ignore;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        private void EditKey_Load(object sender, EventArgs e)
        {
            this.textBox.Focus();
            HookKeyboard();
        }

        private void EditKey_Activated(object sender, EventArgs e)
        {
            this.textBox.Focus();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            return;
        }

        private bool shiftDown, altDown, ctrlDown;

        private void EditKey_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnhookKeyboard();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (this.textBox.Focused)
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    int vkCode = (int)(Marshal.ReadInt64(lParam) >> 32);
                    int flags = Marshal.ReadInt32(lParam);
                    if (vkCode == 55 && flags == 44)  // Printscreen returns an extended code
                        vkCode += 256;

                    bool modkey = false;  // true if processing CTRL ALT or SHIFT press
                    switch (vkCode)
                    {
                        case 29: ctrlDown = true; modkey = true; break;
                        case 56: altDown = true; modkey = true; break;
                        case 42:
                        case 54: shiftDown = true; modkey = true; break;
                        default: break;
                    }

                    // IsModifier is true if we are working on a key assignment that requires only CTRL ALT or SHIFT
                    if( (IsModifier() && modkey)
                        || (!IsModifier() && !modkey) )
                    {
                        Shift = shiftDown;
                        Control = ctrlDown;
                        Alt = altDown;
                        if( IsModifier() )
                            ScanCode = 0;
                        else
                            ScanCode = vkCode;
                        XNAKey = XNA.Keys.None;
                        this.textBox.Text = KeyToString();
                    }
                    return (IntPtr)1;   // disable further processing of this key
                }
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
                {
                    int vkCode = (int)(Marshal.ReadInt64(lParam) >> 32);
                    switch (vkCode)
                    {
                        case 29: ctrlDown = false; break;
                        case 56: altDown = false; break;
                        case 42:
                        case 54: shiftDown = false; break;
                        default: break;
                    }
                    return (IntPtr)1;   // disable further processing of this key
                }
            }
            else
            {
                shiftDown = altDown = ctrlDown = false;
            }
            return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }



        // Ref http://www.seesharpdot.net/?p=96

        private delegate IntPtr KeyboardProcedure(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProcedure lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        public static IntPtr keyboardHookId = IntPtr.Zero;

        public void HookKeyboard()
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                Debug.Assert(keyboardHookId == IntPtr.Zero);
                keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, GetModuleHandle(currentModule.ModuleName), 0);
            }
        }

        public void UnhookKeyboard()
        {
            if (keyboardHookId != IntPtr.Zero) 
            {
                UnhookWindowsHookEx(keyboardHookId);
                keyboardHookId = IntPtr.Zero;
            }
        }



    }
}
