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
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Common;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.Timetables;
using Orts.Simulation.Signalling;
using ORTS;

namespace Orts.Parsers.OR
{
    public class PoolInfo
    {
        Simulator simulator;

        public PoolInfo (Simulator simulatorref)
        {
            simulator = simulatorref;
        }

        public Dictionary<string, TimetablePool> ProcessPools(string[] arguments, CancellationToken cancellation)
        {
            Dictionary<string, TimetablePool> pools = new Dictionary<string, TimetablePool>();
            List<string> filenames;

            // get filenames to process
            filenames = GetFilenames(arguments[0]);

            // get file contents as strings
            Trace.Write("\n");
            foreach (string filePath in filenames)
            {
                // get contents as strings
                Trace.Write("Pool File : " + filePath + "\n");
                var poolInfo = new TimetableReader(filePath);

                // read lines from input until 'Name' definition is found
                int lineindex = 1;
                while (lineindex < poolInfo.Strings.Count)
                {
                    switch (poolInfo.Strings[lineindex][0].ToLower().Trim())
                    {
                        // skip comment
                        case "#comment" :
                            lineindex++;
                            break;
                         
                        // process name
                        // do not increase lineindex as that is done in called method
                        case "#name" :
                            TimetablePool newPool = new TimetablePool(poolInfo, ref lineindex, simulator);
                            // store if valid pool
                            if (!String.IsNullOrEmpty(newPool.PoolName))
                            {
                                if (pools.ContainsKey(newPool.PoolName))
                                {
                                    Trace.TraceWarning("Duplicate pool defined : " + newPool.PoolName);
                                }
                                else
                                {
                                    pools.Add(newPool.PoolName, newPool);
                                }
                            }
                            break;

                        default :
                            if (!String.IsNullOrEmpty(poolInfo.Strings[lineindex][0]))
                            {
                                Trace.TraceInformation("Invalid definition in file " + filePath + " at line " + lineindex + " : " +
                                    poolInfo.Strings[lineindex][0].ToLower().Trim() + "\n");
                            }
                            lineindex++;
                            break;
                    }
                }
            }

            return (pools);
        }

        /// <summary>
        /// Get filenames of TTfiles to process
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> GetFilenames(string filePath)
        {
            List<string> filenames = new List<string>();

            // check type of timetable file - list or single
            string fileDirectory = Path.GetDirectoryName(filePath);

            foreach (var ORPoolFile in Directory.GetFiles(fileDirectory, "*.pool_or"))
            {
                filenames.Add(ORPoolFile);
            }

            return (filenames);
        }

    }
}

