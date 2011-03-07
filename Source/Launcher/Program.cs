// COPYRIGHT 2009, 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;


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

			// Default menu, with override from Registry.
			var menu = "MenuWPF.exe";
			using (var RK = Registry.CurrentUser.OpenSubKey(@"Software\OpenRails\ORTS"))
			{
				var value = SafeReadKey(RK, "LauncherMenu", 0);
				if (value == 1)
					menu = "Menu.exe";
				if (value == 2)
					menu = "MenuWPF.exe";
			}
			Process.Start(Path.Combine(path, menu));
		}

		static void CheckNetFx(List<string> missing)
		{
			using (var RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5"))
				if ((SafeReadKey(RK, "Install", 0) == 1) && (SafeReadKey(RK, "SP", 0) >= 1))
					return;
			missing.Add("Microsoft .NET Framework 3.5 SP1");
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
				"ICSharpCode.SharpZipLib.dll",
				"irrKlang.NET2.0.dll",
				"PIEHidDotNet.dll",
				"Reader.dll",
				// Programs:
				"Menu.exe",
				"MenuWPF.exe",
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
