// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GNU.Gettext;
using GNU.Gettext.WinForms;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Updater;

namespace Updater
{
    public partial class UpdaterProgress : Form
    {
        readonly UserSettings Settings;
        readonly GettextResourceManager catalog = new GettextResourceManager("Updater");

        string BasePath;
        string LauncherPath;
        bool ShouldRelaunchApplication;

        public UpdaterProgress()
        {
            InitializeComponent();

            // Windows 2000 and XP should use 8.25pt Tahoma, while Windows
            // Vista and later should use 9pt "Segoe UI". We'll use the
            // Message Box font to allow for user-customizations, though.
            Font = SystemFonts.MessageBoxFont;

            Settings = new UserSettings(new string[0]);
            LoadLanguage();

            BasePath = Path.GetDirectoryName(Application.ExecutablePath);
            LauncherPath = UpdateManager.GetMainExecutable(BasePath, Application.ProductName);
        }

        void LoadLanguage()
        {
            if (Settings.Language.Length > 0)
            {
                try
                {
                    CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Language);
                }
                catch { }
            }

            Localizer.Localize(this, catalog);
        }

        void UpdaterProgress_Load(object sender, EventArgs e)
        {
            // If /RELAUNCH=1 is set, we're expected to re-launch the main application when we're done.
            ShouldRelaunchApplication = Environment.GetCommandLineArgs().Any(a => a == UpdateManager.RelaunchCommandLine + "1");

            // If /ELEVATE=1 is set, we're an elevation wrapper used to preserve the integrity level of the caller.
            var needsElevation = Environment.GetCommandLineArgs().Any(a => a == UpdateManager.ElevationCommandLine + "1");

            // Run everything in a new thread so the UI is responsive and visible.
            new Thread(needsElevation ? (ThreadStart)ElevationThread : (ThreadStart)UpdaterThread).Start();
        }

        void ElevationThread()
        {
            // Remove both the /RELAUNCH= and /ELEVATE= command-line flags from the child process - it should not do either.
            var processInfo = new ProcessStartInfo(Application.ExecutablePath, String.Join(" ", Environment.GetCommandLineArgs().Skip(1).Where(a => !a.StartsWith(UpdateManager.RelaunchCommandLine) && !a.StartsWith(UpdateManager.ElevationCommandLine)).ToArray()));
            processInfo.Verb = "runas";

            var process = Process.Start(processInfo);
            // We would like to use WaitForInputIdle() here but it is unreliable across elevation.
            if (!IsDisposed)
                Invoke((Action)Hide);
            process.WaitForExit();

            RelaunchApplication();

            Environment.Exit(0);
        }

        void UpdaterThread()
        {
            // We wait for any processes identified by /WAITPID=<pid> to exit before starting up so that the updater
            // will not try and apply an update whilst the previous instance is still lingering.
            var waitPids = Environment.GetCommandLineArgs().Where(a => a.StartsWith(UpdateManager.WaitProcessIdCommandLine));
            foreach (var waitPid in waitPids)
            {
                try
                {
                    var process = Process.GetProcessById(int.Parse(waitPid.Substring(9)));
                    while (!process.HasExited)
                        process.WaitForExit(100);
                    process.Close();
                }
                catch (ArgumentException)
                {
                    // ArgumentException occurs if we try and GetProcessById with an ID that has already exited.
                }
            }

            // Update manager is needed early to apply any updates before we show UI.
            var updateManager = new UpdateManager(BasePath, Application.ProductName, VersionInfo.VersionOrBuild);
            updateManager.ApplyProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Invoke((Action)(() =>
                {
                    progressBarUpdater.Value = e.ProgressPercentage;
                }));
            };

            var channelName = Enumerable.FirstOrDefault(Environment.GetCommandLineArgs(), a => a.StartsWith(UpdateManager.ChannelCommandLine));
            if (channelName != null && channelName.Length > UpdateManager.ChannelCommandLine.Length)
                updateManager.SetChannel(channelName.Substring(UpdateManager.ChannelCommandLine.Length));

            updateManager.Check();
            if (updateManager.LastCheckError != null)
            {
                if (!IsDisposed)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show("Error: " + updateManager.LastCheckError, Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                Application.Exit();
                return;
            }

            updateManager.Apply();
            if (updateManager.LastUpdateError != null)
            {
                if (!IsDisposed)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show("Error: " + updateManager.LastUpdateError, Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                Application.Exit();
                return;
            }

            RelaunchApplication();

            Environment.Exit(0);
        }

        void UpdaterProgress_FormClosed(object sender, FormClosedEventArgs e)
        {
            RelaunchApplication();
        }

        void RelaunchApplication()
        {
            if (ShouldRelaunchApplication)
            {
                var process = Process.Start(LauncherPath);
                process.WaitForExit();
            }
        }
    }
}
