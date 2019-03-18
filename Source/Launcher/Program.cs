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

/* ORTS Launcher
 * 
 * This is the program that users execute start ORTS.  
 * Its purpose is to check for required dependancies 
 * before launching the rest of the ORTS executables.
 * 
 * This program must be compiled with a minimum of dependancies
 * so that it is gauranteed to run.  Ideally this should even
 * be a native program so it can check for .NET as well.
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ORTS
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
			var missing = new List<string>();
			CheckNetFx(missing);
			CheckXNA(missing);
			CheckOR(missing, path);
			if (missing.Count > 0)
			{
				MessageBox.Show(Application.ProductName + " requires the following:\n\n" + String.Join("\n", missing.ToArray()), Application.ProductName);
				return;
			}

			// Default menu
			var menu = "Menu.exe";
			var process = Process.Start(Path.Combine(path, menu));
            process.WaitForInputIdle();
		}

        static void CheckNetFx(List<string> missing)
        {
            using (var RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                if ((SafeReadKey(RK, "Install", 0) == 1) && (SafeReadKey(RK, "Release", 0) >= 461808))  //https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#find-net-framework-versions-45-and-later-with-code
                    return;
            missing.Add("Microsoft .NET Framework 4.7.2 or later");
        }

        static void CheckXNA(List<string> missing)
		{
			foreach (var key in new[] { @"SOFTWARE\Wow6432Node\Microsoft\XNA\Framework\v3.1", @"SOFTWARE\Microsoft\XNA\Framework\v3.1" })
			{
				using (var RK = Registry.LocalMachine.OpenSubKey(key))
					if (SafeReadKey(RK, "Installed", 0) == 1)
						return;
			}
			missing.Add("Microsoft XNA Framework 3.1");
		}

		static void CheckOR(List<string> missing, string path)
		{
			foreach (var file in new[] {
				// Required libraries:
				"GNU.Gettext.dll",
				"GNU.Gettext.WinForms.dll",
				"ICSharpCode.SharpZipLib.dll",
				"OpenAL32.dll",
				"PIEHidDotNet.dll",
				// Programs:
				"Menu.exe",
				"RunActivity.exe",
			})
			{
				if (!File.Exists(Path.Combine(path, file)))
					missing.Add("File '" + file + "'");
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
