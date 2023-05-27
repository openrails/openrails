// COPYRIGHT 2013 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;

namespace ORTS.Common
{
    /// <summary>
    /// Static class which provides version and build information about the whole game.
    /// </summary>
    public static class VersionInfo
    {
        static readonly string ApplicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        /// <summary>Full version, e.g. stable: "1.4", testing: "T1.4-1-g1234567", unstable: "U2021.01.01-0000", local: ""</summary>
        public static readonly string Version = GetVersion("OpenRails.exe");

        /// <summary>Full build, e.g. "0.0.5223.24629 (2014-04-20 13:40:58Z)"</summary>
        public static readonly string Build = GetBuild("OpenRails.exe");

        /// <summary>Version, but if "", returns Build</summary>
        public static readonly string VersionOrBuild = GetVersionOrBuild();

        static string GetVersion(string fileName)
        {
            try
            {
                var version = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationPath, fileName));
                if (version.ProductVersion != version.FileVersion)
                    return version.ProductVersion;
            }
            catch
            {
            }
            return "";
        }

        static string GetBuild(string fileName)
        {
            var builds = new Dictionary<TimeSpan, string>();
            try
            {
                var version = FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationPath, fileName));
                var datetime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var timespan = new TimeSpan(version.FileBuildPart, 0, 0, version.FilePrivatePart * 2);
                return String.Format("{0} ({1:u})", version.FileVersion, datetime + timespan);
            }
            catch
            {
            }
            return "";
        }

        static string GetVersionOrBuild()
        {
            return Version.Length > 0 ? Version : Build;
        }

        /// <summary>
        /// Compares a version and build with the youngest version which failed to restore, to see if that version/build is likely to restore successfully
        /// </summary>
        /// <param name="version">version to test</param>
        /// <param name="build">build to test</param>
        /// <param name="youngestVersionFailedToRestore">youngest version that failed to restore</param>
        /// <returns>true or false when able to determine validity, null otherwise</returns>
        public static bool? GetValidity(string version, string build, string youngestVersionFailedToRestore)
        {
            // Validity rules:
            //  - Same version and build --> yes
            //  - Same non-empty version --> yes
            //  - Unable to parse save or program version --> maybe
            //  - Save version > setting --> maybe
            //  - Program version < setting --> maybe
            //  - Default --> no

            if (Version == version && Build == build) return true;
            if (Version.Length > 0 && version.Length > 0 && Version == version) return true;
            var saveVersion = ParseVersion(version);
            var programVersion = ParseVersion(Version);
            var settingVersion = ParseVersion(youngestVersionFailedToRestore);
            if (saveVersion.Major == 0 || programVersion.Major == 0) return null;
            if (saveVersion > settingVersion) return null;
            if (programVersion < settingVersion) return null;
            return false;
        }

        /// <summary>
        /// Converts many possible Open Rails versions into a standard Version struct
        /// </summary>
        /// <param name="version">text version to parse</param>
        public static Version ParseVersion(string version)
        {
            // Version numbers which we do parse:
            //  - 0.9.0.1648            --> 0.9
            //  - 1.3.1.4328            --> 1.3.1
            //  - T1.3.1-241-g6ff150c21 --> 1.3.1.241
            //  - X1.3.1-370-g7df5318c2 --> 1.3.1.370
            //  - 1.4                   --> 1.4
            //  - 1.4-rc1               --> 1.4
            //  - T1.4-2-g7db094316     --> 1.4.0.2
            // Version numbers which we do NOT parse:
            //  - U2019.07.25-2200
            //  - U2021.06.25-0406
            //  - X.1648
            //  - X1648

            if (version.StartsWith("T") || version.StartsWith("X")) version = version.Substring(1);

            var versionParts = version.Split('-');
            if (!System.Version.TryParse(versionParts[0], out var parsedVersion)) return new Version();

            var commits = 0;
            if (versionParts.Length > 1) int.TryParse(versionParts[1], out commits);
            // parsedVersion.Build will be -1 if the version only has major and minor, but we need the build number >= 0 here
            return new Version(parsedVersion.Major, parsedVersion.Minor, Math.Max(0, parsedVersion.Build), commits);
        }

        public static long GetVersionLong(Version version)
        {
            long number = 0;
            if (version.Major > 0) number += (long)(version.Major & 0xFFFF) << 48;
            if (version.Minor > 0) number += (long)(version.Minor & 0xFFFF) << 32;
            if (version.Build > 0) number += (version.Build & 0xFFFF) << 16;
            if (version.Revision > 0) number += (version.Revision & 0xFFFF);
            return number;
        }
    }
}
