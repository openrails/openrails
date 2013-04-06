// COPYRIGHT 2013 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ORTS.Common
{
    /// <summary>
    /// Static class which provides version and build information about the whole game.
    /// </summary>
    public static class VersionInfo
    {
        static readonly string Revision = GetRevision("Revision.txt");
        public static readonly string Version = GetVersion("Version.txt");
        public static readonly string Build = GetBuild("OpenRails.exe", "Menu.exe", "RunActivity.exe");

        static string GetRevision(string fileName)
        {
            try
            {
                using (var f = new StreamReader(fileName))
                {
                    var revision = f.ReadLine().Trim();
                    if (revision.StartsWith("$Revision:") && revision.EndsWith("$") && !revision.Contains(" 000 "))
                        return revision.Substring(10, revision.Length - 11).Trim();
                }
            }
            catch
            {
            }
            return "";
        }

        static string GetVersion(string fileName)
        {
            try
            {
                using (var f = new StreamReader(fileName))
                {
                    var version = f.ReadLine();
                    if (!String.IsNullOrEmpty(Revision))
                        return version + "." + Revision;
                }
            }
            catch
            {
            }
            return "";
        }

        static string GetBuild(params string[] fileNames)
        {
            var builds = new Dictionary<TimeSpan, string>();
            foreach (var fileName in fileNames)
            {
                try
                {
                    var version = FileVersionInfo.GetVersionInfo(fileName);
                    builds.Add(new TimeSpan(version.ProductBuildPart, 0, 0, version.ProductPrivatePart * 2), version.ProductVersion);
                }
                catch
                {
                }
            }
            if (builds.Count > 0)
            {
                var datetime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var timespan = builds.Keys.OrderBy(ts => ts).Last();
                return String.Format("{0} ({1:u})", builds[timespan], datetime + timespan);
            }
            return "";
        }
    }
}
