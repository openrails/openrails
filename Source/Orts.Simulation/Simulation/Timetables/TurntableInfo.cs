// COPYRIGHT 2014 by the Open Rails project.
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

// This code processes the Timetable definition and converts it into playable train information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Parsers.OR;
using ORTS.Common;

namespace Orts.Simulation.Timetables
{
    /// <summary>
    /// Class to collect pool details
    /// </summary>
    public class TurntableInfo : PoolInfo
    {
        //================================================================================================//
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulatorref"></param>
        public TurntableInfo(Simulator simulatorref) : base(simulatorref)
        {
        }

        //================================================================================================//
        /// <summary>
        /// Read pool files
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public Dictionary<string, TimetableTurntablePool> ProcessTurntables(string[] arguments, CancellationToken cancellation)
        {
            Dictionary<string, TimetableTurntablePool> turntables = new Dictionary<string, TimetableTurntablePool>();
            List<string> filenames;

            // Get filenames to process
            filenames = GetTurntableFilenames(arguments[0]);

            // Get file contents as strings
            Trace.Write("\n");
            foreach (string filePath in filenames)
            {
                // Get contents as strings
                Trace.Write("Turntable File : " + filePath + "\n");
                var turntableInfo = new TimetableReader(filePath);

                // Read lines from input until 'Name' definition is found
                int lineindex = 1;
                while (lineindex < turntableInfo.Strings.Count)
                {
                    switch (turntableInfo.Strings[lineindex][0].ToLower().Trim())
                    {
                        // Skip comment
                        case "#comment":
                            lineindex++;
                            break;

                        // Process name
                        // Do not increase lineindex as that is done in called method
                        case "#name":
                            TimetableTurntablePool newTurntable = new TimetableTurntablePool(turntableInfo, ref lineindex, simulator);
                            // Store if valid pool
                            if (!String.IsNullOrEmpty(newTurntable.PoolName))
                            {
                                if (turntables.ContainsKey(newTurntable.PoolName))
                                {
                                    Trace.TraceWarning("Duplicate turntable defined : " + newTurntable.PoolName);
                                }
                                else
                                {
                                    turntables.Add(newTurntable.PoolName, newTurntable);
                                }
                            }
                            break;

                        default:
                            if (!String.IsNullOrEmpty(turntableInfo.Strings[lineindex][0]))
                            {
                                Trace.TraceInformation("Invalid definition in file " + filePath + " at line " + lineindex + " : " +
                                    turntableInfo.Strings[lineindex][0].ToLower().Trim() + "\n");
                            }
                            lineindex++;
                            break;
                    }
                }
            }

            return turntables;
        }

        //================================================================================================//
        /// <summary>
        /// Get filenames of pools to process
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> GetTurntableFilenames(string filePath)
        {
            List<string> filenames = new List<string>();

            // Check type of timetable file - list or single
            string fileDirectory = Path.GetDirectoryName(filePath);

            foreach (var ORTurntableFile in Directory.GetFiles(fileDirectory, "*.turntable_or"))
            {
                filenames.Add(ORTurntableFile);
            }
            foreach (var ORTunrtableFile in Directory.GetFiles(fileDirectory, "*.turntable-or"))
            {
                filenames.Add(ORTunrtableFile);
            }

            return filenames;
        }
    }
}
