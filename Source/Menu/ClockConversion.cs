// COPYRIGHT 2017, 2018 by the Open Rails project.
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

using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace ORTS
{
    /// <summary>
    /// Manages conversion of shape files for animated clocks from STF (clocks.dat) to JSON (animated.clocks-or) formats.
    /// </summary>
    public static class ClockConversion
    {
        private static string ClocksDat = "openrails\\clocks.dat";
        private static string AnimatedClocksOr = "animated.clocks-or";

        /// <summary>
        /// If a source file is available and more recent than any destination file, then conversion is appropriate.
        /// </summary>
        /// <returns></returns>
        public static bool IsAppropriate(string routePath)
        {
            var fromPath = $"{routePath}\\{ClocksDat}";
            if (File.Exists(fromPath))
            {
                var toPath = $"{routePath}\\{AnimatedClocksOr}";
                if (File.Exists(toPath))
                {
                    var fromDate = File.GetLastWriteTime(fromPath);
                    var toDate = File.GetLastWriteTime(toPath);
                    return (fromDate > toDate);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///  Prompts to carry out conversion, carries it out using Contrib.DataConverter.exe and returns flag to continue.
        /// </summary>
        /// <param name="routePath"></param>
        /// <returns></returns>
        public static bool IsDone(string routePath)
        {
            var reply = MessageBox.Show(
                  "Animated clocks file \"clocksDat\" found."
                + "\n\nTo simulate animated clocks, allow Open Rails to create a file \"animatedClocksOr\" in folder"
                + $"\n{routePath}\""
                + "\n\notherwise continue with clocks not animated"
                , "Animated Clocks"
                , MessageBoxButtons.YesNo);

            if (reply == DialogResult.Yes)
            {
                var conversionMessage = "";
                var exitCode = ClockConversion.ConvertAnimatedClocks(routePath, ClocksDat, AnimatedClocksOr);
                if (exitCode == 0)
                {
                    conversionMessage
                        = $"File \"{routePath}\\{AnimatedClocksOr}\" created."
                        + "\n\nContinuing with clocks animated.";
                    MessageBox.Show(
                          conversionMessage
                        , "Animated Clocks"
                        , MessageBoxButtons.OK
                        , MessageBoxIcon.Information);

                    return true;
                }
                else
                {
                    conversionMessage
                        = $"Failed to create file \"{routePath}\\{AnimatedClocksOr}\"."
                        + $"\nExitCode = {exitCode}"
                        + $"\n\nFix problem or remove file {ClocksDat} and try again.";
                    MessageBox.Show(
                          conversionMessage
                        , "Animated Clocks"
                        , MessageBoxButtons.OK
                        , MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static int ConvertAnimatedClocks(string routePath, string from, string to)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "Contrib.DataConverter.exe",
                Arguments = $"/input \"{routePath}\\{from}\" /output \"{routePath}\\{to}\"",
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = Application.StartupPath,
                UseShellExecute = false
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
