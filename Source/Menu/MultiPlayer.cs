using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ORTS {
    public partial class MultiPlayer : Form {
        MainForm parentForm;

        public MultiPlayer( MainForm parentForm ) {
            this.parentForm = parentForm;
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.
            // set defaults
            tUsername.Text = parentForm.Username;
            tIP.Text = parentForm.IP;
            tPortNo.Text = parentForm.PortNo.ToString();
            rbServer.Checked = ! parentForm.IsClient;
            rbClient.Checked = parentForm.IsClient;
            tIP.Enabled = rbClient.Checked;
            tIP.ReadOnly = !rbClient.Checked;
        }

        // Radio buttons are mutually exclusive so need event handler for only one of the pair.
        private void rbClient_CheckedChanged( object sender, EventArgs e ) {
            parentForm.IsClient = rbClient.Checked;
            // This text box is disabled once Server has been selected.
            tIP.Enabled = rbClient.Checked;
            tIP.ReadOnly = !rbClient.Checked;
        }

        private void bClose_Click( object sender, EventArgs e ) {
            this.Close();
        }

        private void tUsername_Validating( object sender, CancelEventArgs e ) {
            if( tUsername.Text.Length < 4 
            || tUsername.Text.Length > 10
            || tUsername.Text.Contains("\"")
            || tUsername.Text.Contains( "\'" )
            || tUsername.Text.Contains( " " )
            || (tUsername.Text.Length > 0 && Char.IsDigit(tUsername.Text, 0) ) ) {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                tUsername.Select( 0, tUsername.Text.Length );

                // Set the ErrorProvider error with the text to display. 
                epUsername.SetError( tUsername, "Username must be 4-10 characters long, cannot contain space, ' or \" and may not start with a digit." );
            } else {
                parentForm.Username = tUsername.Text;
                epUsername.Clear();
            }
        }

        private void tIP_Validating( object sender, CancelEventArgs e ) {
            // Regular expression matches 0.0.0.0 to 999.999.999.999
            Regex regex = new Regex( @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$" );
            if( ! regex.IsMatch(tIP.Text) ) {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                tIP.Select( 0, tIP.Text.Length );

                // Set the ErrorProvider error with the text to display. 
                epIP.SetError( tIP, "IP should be of the form 127.0.0.1" );
            } else {
                parentForm.IP = tIP.Text;
                epIP.Clear();
            }
        }

        private void tPortNo_Validating( object sender, CancelEventArgs e ) {
            // 0 to 65535
            try {
                parentForm.PortNo = Convert.ToInt32( tPortNo.Text );
                epPortNo.Clear();
            } catch {
                // Cancel the event and select the text to be corrected by the user.
                e.Cancel = true;
                tPortNo.Select( 0, tPortNo.Text.Length );

                // Set the ErrorProvider error with the text to display. 
                epPortNo.SetError( tPortNo, "Port no. must be an integer 0 to 65535." );
            }
        }
    }
}
