// COPYRIGHT 2012 by the Open Rails project.
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

// KEYPRESSCONTROL
//
// A windows control used in the options menu to set up keyboard configuration.
// This control displays the keystroke combination assigned to a specific game command.
// And it provides a means to edit it using the EditKey form.
//
// It takes its input directly from the InputSettings.Commands list, 
// and writes any changes back to the same location, 
// updating InputSettings.Sources if needed.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using XNA = Microsoft.Xna.Framework.Input;



namespace ORTS
{
    public partial class KeyPressControl : TextBox
    {
        public KeyPressControl()
        {
            InitializeComponent();
        }

        public UserCommands eCommand;
        public UserCommandInput DefaultKeyCombo;

        public void InitFrom( UserCommands ecommand, UserCommandInput defaultKeyCombo )
        {
            eCommand = ecommand;
            DefaultKeyCombo = defaultKeyCombo;
            UserCommandInput keyCombo = InputSettings.Commands[(int)ecommand];
            Text = keyCombo.ToEditString();
            RefreshColor();
        }

        // True if the current key combo matches the default key combo
        private bool MatchesDefaults()
        {
            int scan1, scan2;
            XNA.Keys vkey1, vkey2;
            bool ctrl1, ctrl2;
            bool alt1, alt2;
            bool shift1, shift2;

            UserCommandInput currentKeyCombo = InputSettings.Commands[(int)eCommand];
            DefaultKeyCombo.ToValue(out scan1, out vkey1, out ctrl1, out alt1, out shift1);
            currentKeyCombo.ToValue(out scan2, out vkey2, out ctrl2, out alt2, out shift2);

            // Note for purposes of hilighting changed keys, won't don't hilite changes in the 
            // 'ignore keys' used by modifiable commands.  The modifier will hilite as changed,
            // but we don't that to cause the commands they modify to show changed.

            return scan1 == scan2 && vkey1 == vkey2 && ctrl1 == ctrl2 && alt1 == alt2 && shift1 == shift2;
        }

        private void RefreshColor()
        {
            if (MatchesDefaults() )
                BackColor = SystemColors.Window;
            else
                BackColor = EditKey.EditColor;
        }

        private void KeyPressControl_Click(object sender, EventArgs e)
        {
            var editKey = new EditKey();
            editKey.Location = Parent.PointToScreen(Location);
            editKey.InitFrom(this);
            DialogResult result = editKey.ShowDialog(this);
            if ( result == DialogResult.OK)
            {
                // User pressed OK, now write new values to the InputSettings structure
                UserCommandInput keyCombo = InputSettings.Commands[(int)eCommand];
                keyCombo.SetFromValues(editKey.ScanCode, editKey.XNAKey, editKey.Control, editKey.Alt, editKey.Shift);
                Text = keyCombo.ToEditString();
                if( MatchesDefaults() )
                    InputSettings.Sources[(int)eCommand] = UserSettings.Source.Default;
                else
                    InputSettings.Sources[(int)eCommand] = UserSettings.Source.Registry;
                RefreshColor();
            }
            else if ( result == DialogResult.Ignore)
            {
                // User pressed 'D' for defaults
                int scan; XNA.Keys vkey; bool ctrl; bool alt; bool shift;
                DefaultKeyCombo.ToValue(out scan, out vkey, out ctrl, out alt, out shift);
                UserCommandInput keyCombo = InputSettings.Commands[(int)eCommand];
                keyCombo.SetFromValues(scan, vkey, ctrl, alt, shift);
                Text = keyCombo.ToEditString();
                InputSettings.Sources[(int)eCommand] = UserSettings.Source.Default;
                RefreshColor();
            }
            Parent.Focus();
        }
    }
}
