// COPYRIGHT 2009, 2010, 2013 by the Open Rails project.
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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace MSTS
{
    // TODO: Replace this with a full-on ORTS.Content.ContentManager.
    /// <summary>
    /// Utility functions to access the various directories in an MSTS install.
    /// </summary>
    public class MSTSPath
    {

        //TODO - make all msts classes use this.
        public static string DefaultLocation;   // MSTS default path.

        /// <summary>
        /// Returns the base path of the MSTS installation
        /// </summary>
        /// <returns>no trailing \</returns>
        public static string Base()
        /* Throws
         *		System.Exception( "Can't find MSTS" );
         */
        {

			if (DefaultLocation == null)
			{
				DefaultLocation = "c:\\program files\\microsoft games\\train simulator";

				RegistryKey RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft Games\Train Simulator\1.0");
				if (RK == null)
					RK = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Train Simulator\1.0");
				if (RK != null)
					DefaultLocation = (string)RK.GetValue("Path", DefaultLocation);

				// Verify installation at this location
				if (!Directory.Exists(DefaultLocation))
					throw new FileNotFoundException("MSTS directory '" + DefaultLocation + "' does not exist.", DefaultLocation);
			}

            return DefaultLocation;
        }  //


        /// <summary>
        /// Returns the route folder with out trailing \
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public static string RouteFolder(string route)
        {
            return Base() + "\\ROUTES\\" + route;
        }

        public static string ConsistFolder()
        {
            return Base() + "\\TRAINS\\CONSISTS";
        }

        public static string TrainsetFolder()
        {
            return Base() + "\\TRAINS\\TRAINSET";
        }

        public static string GlobalSoundFolder()
        {
            return Base() + "\\SOUND";
        }

        public static string GetActivityFolder(string routeFolderName)
        {
            return RouteFolder(routeFolderName) + "\\ACTIVITIES";
        }

        public static string GetTRKFileName(string routeFolderPath)
        {
            if (!Directory.Exists(routeFolderPath))
                throw new DirectoryNotFoundException(routeFolderPath);
            var trkFileNames = Directory.GetFiles(routeFolderPath, "*.trk");
            if (trkFileNames.Length == 0)
                throw new FileNotFoundException("TRK file not found in '" + routeFolderPath + "'.", Path.Combine(routeFolderPath, Path.GetFileName(routeFolderPath)));
            return trkFileNames[0];
        }

        /// <summary>
        /// Given a soundfile reference in a wag or eng file, return the path the sound file
        /// </summary>
        /// <param name="wagfilename"></param>
        /// <param name="soundfile"></param>
        /// <returns></returns>
        public static string TrainSoundPath(string wagfilename, string soundfile)
        {
            string trainsetSoundPath = Path.GetDirectoryName(wagfilename) + @"\SOUND\" + soundfile;
            string globalSoundPath = MSTSPath.GlobalSoundFolder() + @"\" + soundfile;

            return File.Exists(trainsetSoundPath) ? trainsetSoundPath : globalSoundPath;
        }


        /// <summary>
        /// Given a soundfile reference in a cvf file, return the path to the sound file
        /// </summary>
        public static string SMSSoundPath(string smsfilename, string soundfile)
        {
            string smsSoundPath = Path.GetDirectoryName(smsfilename) + @"\" + soundfile;
            string globalSoundPath = MSTSPath.GlobalSoundFolder() + @"\" + soundfile;

            return File.Exists(smsSoundPath) ? smsSoundPath : globalSoundPath;
        }

        public static string TITFilePath(string route)
        {
            return RouteFolder(route) + "\\" + route + ".TIT";
        }

        public static string GetConPath(string conName)
        {
            return Base() + @"\TRAINS\CONSISTS\" + conName + ".con";
        }

        public static string GetSrvPath(string srvName, string routeFolderPath)
        {
            return routeFolderPath + @"\SERVICES\" + srvName + ".srv";
        }

        public static string GetTrfPath(string trfName, string routeFolderPath)
        {
            return routeFolderPath + @"\TRAFFIC\" + trfName + ".trf";
        }

        public static string GetPatPath(string patName, string routeFolderPath)
        {
            return routeFolderPath + @"\PATHS\" + patName + ".pat";
        }

        public static string GetWagPath(string name, string folder)
        {
            return Base() + @"\TRAINS\TRAINSET\" + folder + @"\" + name + ".wag";
        }

        public static string GetEngPath(string name, string folder)
        {
            return Base() + @"\TRAINS\TRAINSET\" + folder + @"\" + name + ".eng";
        }

    } // class MSTSPath
}
