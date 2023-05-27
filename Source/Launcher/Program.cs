// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* Open Rails Launcher
 *
 * This is the program which users launch. Its purpose is to check for the
 * required dependencies before launching the menu, so it is designed to
 * run on clean versions of Windows 7 and up
 *
 * Dependencies checked for:
 *   - Microsoft .NET Framework 4.7.2
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ORTS
{
    internal struct DependencyHint
    {
        public string Name;
        public string Url;
        public string Text;
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            // Check for any missing components.
            var path = Path.GetDirectoryName(Application.ExecutablePath);
            List<DependencyHint> missingDependencies = new List<DependencyHint>();

            CheckNetFx(missingDependencies);

            if (missingDependencies.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (var item in missingDependencies)
                    builder.AppendLine(item.Name);

                if (MessageBox.Show($"{Application.ProductName} requires the following:\n\n{builder.ToString()}" +
                    "\nWhen you click OK, we will guide you to download the required software.\n" +
                    (missingDependencies.Count > 1 ? "If there are multiple items missing, you need to repeat this process until all dependencies are resolved.\n" : string.Empty) +
                    "Click Cancel to quit.",
                    Application.ProductName, MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    DownloadDependency(missingDependencies[0]);
                }
                return;
            }

            List<string> missingORFiles = new List<string>();
            CheckOR(missingORFiles, path);
            if (missingORFiles.Count > 0)
            {
                MessageBox.Show($"{Application.ProductName} is missing the following:\n\n{string.Join("\n", missingORFiles.ToArray())}\n\nPlease re-install the software.", Application.ProductName);
                return;
            }
            // Default menu
            var process = Process.Start(Path.Combine(path, "Menu.exe"));
            process.WaitForInputIdle();
        }

        private static void DownloadDependency(DependencyHint dependency)
        {
            Clipboard.SetText(dependency.Url);
            MessageBox.Show($"{dependency.Text} \n\nWhen you click OK, we will try to open a browser window pointing to the URL. " +
                "You can also open a browser window yourself now and paste the URL from clipboard (Ctrl + V).", dependency.Name);
            Process.Start(dependency.Url);
        }

        static void CheckNetFx(List<DependencyHint> missingDependencies)
        {
            using (var RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                if ((SafeReadKey(RK, "Install", 0) == 1) && (SafeReadKey(RK, "Release", 0) >= 461808))  //https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#find-net-framework-versions-45-and-later-with-code
                    return;

            missingDependencies.Add(new DependencyHint()
            {
                Name = ("Microsoft .NET Framework 4.7.2 or later"),
                Text = "Please go to\n https://support.microsoft.com/en-us/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows \nto download the installation package " +
                "for Microsoft .NET Framework 4.7.2 and install the software.",
                Url = "https://support.microsoft.com/en-us/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows"
            });
        }

        static void CheckOR(List<string> missingFiles, string path)
        {
            foreach (var file in new[] {
                // Required libraries:
                "GNU.Gettext.dll",
                "GNU.Gettext.WinForms.dll",
                "ICSharpCode.SharpZipLib.dll",
                "DotNetZip.dll",
                @"Native/X86/OpenAL32.dll",
                @"Native/X64/OpenAL32.dll",
                // Programs:
                "Menu.exe",
                "RunActivity.exe",
            })
            {
                if (!File.Exists(Path.Combine(path, file)))
                    missingFiles.Add($"File '{file}'");
            }
        }

        static int SafeReadKey(RegistryKey key, string name, int defaultValue)
        {
            try
            {
                return (int)key.GetValue(name, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}