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
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Contributors
///     2009-12-06  Edward Keenan 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;


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
            if (!XNAIsInstalled())
            {
                MessageBox.Show("Could not find XNA 3.1\n\nPlease search www.microsoft.com for\nXNA 3.1 redistributable");
                return;
            }

            foreach (string filename in new string[] {
                        "ICSharpCode.SharpZipLib.dll",
                        "irrKlang.NET2.0.dll",
                        "reader.dll",
                        "Menu.exe",
                        "RunActivity.exe" })
            {
                if (!File.Exists(filename))
                {
                    MessageBox.Show("Could not find " + filename);
                    return;
                }
            }

            string menuFolder = Environment.CurrentDirectory;

            ProcessStartInfo objPSI = new ProcessStartInfo();
            objPSI.FileName = menuFolder + @"\menu.exe";
            objPSI.WindowStyle = ProcessWindowStyle.Normal; // or Hidden, Maximized or Normal

            // TODO ADD A CHECK FOR .NET 3.5

            Process.Start(objPSI);
        }

        static bool XNAIsInstalled()
        {
            try
            {
                // Restore retained settings
                RegistryKey RK = null;
                RegistryKey RK32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\XNA\\Framework\\v3.1");
                RegistryKey RK64 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\XNA\\Framework\\v3.1");
                if (RK32 != null)
                    RK = RK32;
                else if (RK64 != null)
                    RK = RK64;
                int installed = (int)RK.GetValue("Installed", 0);
                if (installed == 1)
                    return true;
            }
            catch (System.Exception)
            {
            }
            return false;
        }

    }
}
