// COPYRIGHT 2025 by the Open Rails project.
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

// Export the Settings in the Registry into an INI file, or vice versa.
// In order to support multiple installations with different settings,
// OpenRails supports using an INI file for the settings (instead of
// the registry entries that are shared by all installations).
// OpenRails creates a default INI file when the file exists, but has no
// settings. This tool exports the current settings from the registry to
// the INI file. It also allows the reverse.

// Important: UpdateSettings, part of UpdateManager has its own file,
//            Updater.ini.
//
// FUTURE: New settings classes that are not part of UserSettings need to
//         be added to the exporter. See FUTURE tag below.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.SettingsExporter
{
    class Program
    {
        static int Main(string[] args)
        {
            string fromArg = null; string toArg = null;

            #region Parse Args
            // parse arguments
            foreach (string arg in args)
            {
                if (arg.Equals("/h") || arg.Equals("/help")) { ShowHelp(); Environment.Exit(1); }
                else if (arg.StartsWith("/")) { Console.WriteLine("ERROR: Invalid option {0}.", arg); ShowHelp(); Environment.Exit(1); }
                else if (fromArg == null) { fromArg = arg; }
                else if (toArg == null) { toArg = arg; }
                else { Console.WriteLine("ERROR: extraneous argument {0}.", arg); ShowHelp(); Environment.Exit(1); }
            }
            if (String.IsNullOrEmpty(fromArg) || String.IsNullOrEmpty(toArg))
            { Console.WriteLine("ERROR: Missing from or to argument."); ShowHelp(); Environment.Exit(1); }
            else if (fromArg.Equals(toArg))
            { Console.WriteLine("ERROR: From {0} and to {1} must not be the same.", fromArg, toArg); ShowHelp(); Environment.Exit(1); }
            #endregion

            string loadFilePath = null; string loadRegistryKey = null;

            #region Determine From
            // determine where to load from
            if (fromArg.Equals("INI")) { loadFilePath = SettingsBase.DefaultSettingsFileName; }
            else if (fromArg.Equals("REG")) { loadRegistryKey = SettingsBase.DefaultRegistryKey; }
            else if (fromArg.EndsWith(".ini")) { loadFilePath = fromArg; }
            else { loadRegistryKey = fromArg; }

            // check that source exists
            if (!String.IsNullOrEmpty(loadFilePath))
            {
                var iniFilePath = Path.Combine(ApplicationInfo.ProcessDirectory, loadFilePath);
                if (!File.Exists(iniFilePath))
                {
                    Console.WriteLine("ERROR: INI file {0} to export from does not exist.", iniFilePath);
                    Environment.Exit(1);
                }
            }
            else if (!String.IsNullOrEmpty(loadRegistryKey))
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(loadRegistryKey))
                {
                    if (regKey == null)
                    {
                        Console.WriteLine("ERROR: Reg key {0} to export from does not exist.", loadRegistryKey);
                        Environment.Exit(1);
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR: No source to export from found");
                Environment.Exit(1);
            }
            #endregion

            // instantiate the static part of SettingsBase
            UserSettings userSettings;

            // then override
            SettingsBase.OverrideSettingsLocations(loadFilePath, loadRegistryKey);

            Console.WriteLine("Info: Loading from {0}.", SettingsBase.RegistryKey + SettingsBase.SettingsFilePath);

            // load the user settings and its sub-settings from the default location
            IEnumerable<string> options = Enumerable.Empty<string>();
            userSettings = new UserSettings(options);
            var updateState = new UpdateState();
            // FUTURE: add here when a new settings class is added (that is not handled by UserSettings)

            Console.WriteLine("Info: Successfully loaded settings from {0}.", userSettings.GetSettingsStoreName());

            string saveFilePath = null; string saveRegistryKey = null;

            #region Determine To
            // determine where to save to
            if (toArg.Equals("INI")) { saveFilePath = SettingsBase.DefaultSettingsFileName; }
            else if (toArg.Equals("REG")) { saveRegistryKey = SettingsBase.DefaultRegistryKey; }
            else if (toArg.Contains(".ini"))
            {
                // save to a custom ini file, do some basic verification
                saveFilePath = toArg;
                var dir = Path.GetDirectoryName(Path.Combine(ApplicationInfo.ProcessDirectory, saveFilePath));
                if (!Directory.Exists(dir)) { Console.WriteLine("ERROR: Directory {0} to save to does not exist.", dir); Environment.Exit(1); }
            }
            else if (toArg.StartsWith("SOFTWARE")) { saveRegistryKey = toArg; }
            else 
            { 
                Console.WriteLine("ERROR: Invalid destination {0}.", toArg);
                Console.WriteLine("ERROR: Registry key must start with \"SOFTWARE\", or INI path must include \".ini\".");
                Environment.Exit(1);
            }
            #endregion

            #region Backup
            if (!String.IsNullOrEmpty(saveFilePath))
            {
                // backup the file if it already exists
                string settingsFilePath = Path.Combine(ApplicationInfo.ProcessDirectory, saveFilePath);
                string backupFilePath = Path.Combine(ApplicationInfo.ProcessDirectory, saveFilePath + ".bak");
                if (File.Exists(settingsFilePath))
                {
                    File.Delete(backupFilePath);
                    File.Move(settingsFilePath, backupFilePath);
                    Console.WriteLine("Info: Backed up existing INI file as {0}.", backupFilePath);
                }

                // create an empty file (required by SettingsStore)
                using (File.Create(settingsFilePath)) { };

            }
            else if (!String.IsNullOrEmpty(saveRegistryKey))
            {
                // backup the registry key if it already exists
                using (var regKey = Registry.CurrentUser.OpenSubKey(saveRegistryKey))
                {
                    if (regKey != null)
                    {
                        using (var backupRegKey = Registry.CurrentUser.CreateSubKey(saveRegistryKey + "-Backup"))
                        { 
                            CopyRegistryKey(regKey, backupRegKey);
                            Console.WriteLine("Info: Backed up existing Registry key as {0}.", backupRegKey.Name);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR: No destination to export to.");
                Environment.Exit(1);
            }
            #endregion

            // change the settings store
            userSettings.ChangeSettingsStore(saveFilePath, saveRegistryKey, null);  // section is defined in SettingsStoreLocalIni
            updateState.ChangeSettingsStore(saveFilePath, saveRegistryKey, UpdateState.SectionName);
            // FUTURE: add here when a new settings class is added (that is not handled by UserSettings)

            Console.WriteLine("Info: Saving to {0}.", userSettings.GetSettingsStoreName());

            // save the settings
            userSettings.Save();
            updateState.Save();
            // FUTURE: add here when a new settings class is added (that is not handled by UserSettings)

            Console.WriteLine("Info: Successfully saved to {0}.", userSettings.GetSettingsStoreName());

            if (toArg.Equals("REG") || toArg.Equals(SettingsBase.DefaultRegistryKey))
            {
                Console.WriteLine();
                Console.WriteLine("To use the settings in the registry, manually delete the INI file in the OpenRails folder.");
                Console.WriteLine("  eg: {0}", Path.Combine(ApplicationInfo.ProcessDirectory, SettingsBase.DefaultSettingsFileName));
            }
 
            return 0;
        }

        static void ShowHelp()
        {
            string cmd = Path.GetFileNameWithoutExtension(ApplicationInfo.ProcessFile);
            Console.WriteLine();
            Console.WriteLine("Usage: {0} <from> <to>", cmd);
            Console.WriteLine("  <from>     Specify the source to load settings from. It may be:");
            Console.WriteLine("               INI  : use the default INI file {0}.", SettingsBase.DefaultSettingsFileName);
            Console.WriteLine("               REG  : use the default registry key {0}.", SettingsBase.DefaultRegistryKey);
            Console.WriteLine("               path : a specific INI file, relative to the OpenRails main folder. Must include \".ini\".");
            Console.WriteLine("               key  : a specific registry key, relative to the HKEY_CURRENT_USER key. Must start with \"SOFTWARE\".");
            Console.WriteLine("  <to>       Specify the destination to save the settings to. Similar to <from>.");
            Console.WriteLine("  /h, /help  Show this help.");
            Console.WriteLine();
            Console.WriteLine("This utility reads the Settings (Options) from one location, and exports them to another location.");
            Console.WriteLine("It creates a backup of any settings that will be overwritten. Example:");
            Console.WriteLine("  <installfolder>\\OpenRails.ini.bak");
            Console.WriteLine("  HKEY_CURRENT_USER\\SOFTWARE\\OpenRails\\ORTS-Backup");
            Console.WriteLine("This utility is intended to:");
            Console.WriteLine("- Create an INI file that has the same settings as what is in the registry.");
            Console.WriteLine("  Example: {0} REG INI", cmd);
            Console.WriteLine("- Copy the settings from an INI file back into the registry, so that other installations");
            Console.WriteLine("  use the same settings.");
            Console.WriteLine("  Example: {0} INI REG", cmd);
            Console.WriteLine("- Backup the settings, before making temporary changes (and restore afterwards).");
            Console.WriteLine("  Example: {0} REG SOFTWARE\\OpenRails-Saved\\ORTS", cmd);
            Console.WriteLine();
        }

        /// <summary>
        /// Recursively copy the values of a registry key to another registry key.
        /// </summary>
        /// <param name="fromKey">the key to copy from; must exist</param>
        /// <param name="toKey">the key to copy to; should not exist</param>
        static void CopyRegistryKey(RegistryKey fromKey, RegistryKey toKey)
        {
            // copy the values
            foreach (var name in fromKey.GetValueNames())
            {
                toKey.SetValue(name, fromKey.GetValue(name), fromKey.GetValueKind(name));
            }

            // copy the subkeys
            foreach (var name in fromKey.GetSubKeyNames())
            {
                using (var fromSubKey = fromKey.OpenSubKey(name))
                {
                    var toSubKey = toKey.CreateSubKey(name);
                    CopyRegistryKey(fromSubKey, toSubKey);
                }
            }
        }
    }
}
