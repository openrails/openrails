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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ActivityEditor.Internat;
using ActivityEditor.Preference;
using ORTS.Settings;

namespace ActivityEditor
{
    static class Program
    {
        public static IntalMngr intlMngr;
        public static string[] Arguments;
        public static string RegistryKey;     // ie @"SOFTWARE\OpenRails\ActivityEditor"
        public static string UserDataFolder;  // ie @"F:\Users\Wayne\AppData\Roaming\Open Rails"
        public static ActEditor actEditor;
        public static AEPreference aePreference { get; set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);
            RegistryKey = "SOFTWARE\\OpenRails\\ActivityEditor";

            if (File.Exists(@"F:\temp\AE.txt"))
            {
                File.Delete(@"F:\temp\AE.txt");
            }
#if WITH_DEBUG
            File.AppendAllText(@"F:\temp\AE.txt",
                "In ActivityEditor\n");
#endif

            var options = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")));
            UserSettings settings = new UserSettings(options);   //   GetSettings(options);
            intlMngr = new IntalMngr(settings);

            Init(settings, args);
        }

        static void Init(UserSettings settings, string[] args)
        {
            Init(settings, args, "");
        }

        static void Init(UserSettings settings, string[] args, string mode)
        {
            Console.WriteLine(mode.Length > 0 ? "Mode       = {0} {1}" : "Mode       = {1}", mode, args.Length == 1 ? "Activity" : "Explore");
            if (args.Length == 1)
            {
                Console.WriteLine("Activity   = {0}", args[0]);
            }
            else if (args.Length == 3)
            {
                Console.WriteLine("Activity   = {0}", args[0]);
            }
            else if (args.Length == 4)
            {
                Console.WriteLine("Activity   = {0}", args[0]);
            }
            else if (args.Length > 0)
            {
                Console.WriteLine("Path       = {0}", args[0]);
                Console.WriteLine("Consist    = {0}", args[1]);
                Console.WriteLine("Time       = {0}", args[2]);
                Console.WriteLine("Season     = {0}", args[3]);
                Console.WriteLine("Weather    = {0}", args[4]);
            }
            aePreference = AEPreference.loadXml();
            aePreference.CompleteSettings(settings);
            Arguments = args;
            //  PseudoSim va gérer l'ensemble du système
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            actEditor = new ActEditor();
            aePreference.ActEditor = actEditor;
            Application.Run(actEditor);
            Program.aePreference.orConfig.SaveConfig();


            //Simulator = new PseudoSim(settings);
            //Simulator.Start();

        }
        static UserSettings GetSettings(IEnumerable<string> options)
        {
            return new UserSettings(options);
        }


    }
}
