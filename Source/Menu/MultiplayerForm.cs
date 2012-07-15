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
        public MultiplayerForm()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            textBoxUser.Text = Environment.UserName;

            // Restore retained settings
            using (var RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey))
            {
                if (RK != null)
                {
                    this.textBoxUser.Text = (string)RK.GetValue("Multiplayer_User", textBoxUser.Text);
                    this.textBoxHost.Text = (string)RK.GetValue("Multiplayer_Host", textBoxHost.Text);
                    this.numericPort.Value = (int)RK.GetValue("Multiplayer_Port", (int)numericPort.Value);
                }
            }
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
            using (var RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey))
            {
                RK.SetValue("Multiplayer_User", textBoxUser.Text);
                RK.SetValue("Multiplayer_Host", textBoxHost.Text);
                RK.SetValue("Multiplayer_Port", (int)numericPort.Value);
            }
        }
    }
}
