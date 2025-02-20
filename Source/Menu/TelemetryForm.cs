// COPYRIGHT 2009 - 2024 by the Open Rails project.
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
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Settings;

namespace Menu
{
    public partial class TelemetryForm : Form
    {
        readonly TelemetryManager Manager;

        public TelemetryForm(TelemetryManager manager)
        {
            InitializeComponent();  // Needed so that setting StartPosition = CenterParent is respected.

            GettextResourceManager catalog = new GettextResourceManager("Menu");
            Localizer.Localize(this, catalog);

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            // Bold version of the font
            title1.Font = title2.Font = title3.Font = new Font(Font, FontStyle.Bold);

            Manager = manager;
            comboBoxSystem.SelectedIndex = (int)Manager.GetState(TelemetryType.System);
        }

        private void comboBoxSystem_SelectedIndexChanged(object sender, EventArgs e)
        {
            Manager.SetState(TelemetryType.System, (TelemetryState)comboBoxSystem.SelectedIndex);
        }

        private void linkLabelPreviewSystem_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("cmd", "/k Contrib.DataCollector /system");
        }

        private void linkLabelServer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Manager.Settings.ServerURL,
                UseShellExecute = true,
            });
        }
    }
}
