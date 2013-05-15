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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ORTS
{
    public partial class MultiplayerForm : Form
    {
		readonly UserSettings Settings;
        
        public MultiplayerForm(UserSettings settings)
        {
            InitializeComponent();

			Settings = settings;

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            textBoxUser.Text = Environment.UserName;


			this.textBoxUser.Text = Settings.Multiplayer_User;
			this.textBoxHost.Text = Settings.Multiplayer_Host;
			this.numericPort.Value = Settings.Multiplayer_Port;
			this.textMPUpdate.Text = ""+Settings.Multiplayer_UpdateInterval;
			this.showAvatar.Checked = Settings.ShowAvatar;
			this.avatarURL.Text = Settings.AvatarURL;
        }

        void textBoxUser_Validating(object sender, CancelEventArgs e)
        {
            if (textBoxUser.Text.Length < 4 || textBoxUser.Text.Length > 10 || textBoxUser.Text.Contains("\"") || textBoxUser.Text.Contains("\'") || textBoxUser.Text.Contains(" ") || Char.IsDigit(textBoxUser.Text, 0))
            {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                textBoxUser.Select(0, textBoxUser.Text.Length);

                // Set the ErrorProvider error with the text to display. 
                epUser.SetError(textBoxUser, "User name must be 4-10 characters long, cannot contain space, ' or \" and must not start with a digit.");
            }
            else
            {
                epUser.Clear();
            }
        }

        void textBoxHost_Validating(object sender, CancelEventArgs e)
        {
            // Regular expression matches 0.0.0.0 to 999.999.999.999
            var regex = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
            if (!regex.IsMatch(textBoxHost.Text))
            {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                textBoxHost.Select(0, textBoxHost.Text.Length);

                // Set the ErrorProvider error with the text to display. 
                epHost.SetError(textBoxHost, "IP should be of the form 127.0.0.1");
            }
            else
            {
                epHost.Clear();
            }
        }

        void numericPort_Validating(object sender, CancelEventArgs e)
        {
            if (numericPort.Value < 0 || numericPort.Value > 65535)
            {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                numericPort.Select(0, numericPort.Value.ToString().Length);

                // Set the ErrorProvider error with the text to display. 
                epPort.SetError(numericPort, "Port number must be an integer between 0 and 65535.");
            }
            else
            {
                epPort.Clear();
            }
        }

        void buttonOK_Click(object sender, EventArgs e)
        {
            // Retain settings for convenience
			Settings.Multiplayer_UpdateInterval = (int)double.Parse(textMPUpdate.Text);
			Settings.Multiplayer_User = textBoxUser.Text;
			Settings.Multiplayer_Host = textBoxHost.Text;
			Settings.Multiplayer_Port = (int)numericPort.Value;
			Settings.ShowAvatar = showAvatar.Checked;
			Settings.AvatarURL = avatarURL.Text;
        }
    }
}
