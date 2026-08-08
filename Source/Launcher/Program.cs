// COPYRIGHT 2009 - 2026 by the Open Rails project.
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
 * required dependencies and Open Rails files before launching the menu.
 *
 * .NET 6 checks for itself on launch
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Launcher
{
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

        static void CheckOR(List<string> missingFiles, string path)
        {
            foreach (var file in new[] {
                // Required libraries:
                "GNU.Gettext.dll",
                "GNU.Gettext.WinForms.dll",
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
    }
}
