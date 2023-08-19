// COPYRIGHT 2016 by the Open Rails project.
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

using Newtonsoft.Json;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Orts.DataConverter
{
    class ClockConverter : IDataConverter
    {
        public ClockConverter()
        {
        }

        public void ShowConversions()
        {
            //                "1234567890123456789012345678901234567890123456789012345678901234567890123456789"
            //                "               Input  Output              Description"
            Console.WriteLine("openrails/clocks.dat  animated.clocks-or  Converts from STF to JSON");
        }

        /// <summary>
        /// Not clear what happens if conversion fails.
        /// </summary>
        /// <param name="conversion"></param>
        /// <returns></returns>
        public bool DoConversion(DataConversion conversion)
        {
            // We can convert from .dat files.
            if (conversion.Input.EndsWith(@"openrails\clocks.dat") == false)
            {
                return false;
            }
            // We can convert to .clocks-or files.
            if (conversion.Output.Any(r => r.EndsWith("clocks-or")) == false
                || conversion.Output.Count != 1)
            {
                return false;
            }
            if (File.Exists(conversion.Input) == false)
            {
                throw new FileNotFoundException("", conversion.Input);
            }

            var clockLists = new List<ClockList>();
            try
            {
                var inFilePath = Path.GetFullPath(conversion.Input);

                // Shorten the path to get the route
                var routeLength = inFilePath.Length - @"openrails\clocks.dat".Length;
                var routePath = inFilePath.Substring(0, routeLength);

                clockLists = ParseInput(conversion, routePath);
            }
            catch (Orts.Parsers.Msts.STFException e)
            {
                Console.WriteLine("Parse error in " + conversion.Input + e.Message);
                throw; // re-throw exception without losing the stack trace
            }

            var jso = new List<ClockShape>();

            try
            {
                GenerateOutput(conversion, clockLists, jso);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error generating " + conversion.Output + e.Message);
                throw; // re-throw exception without losing the stack trace
            }

            return true;
        }

        private static List<ClockList> ParseInput(DataConversion conversion, string routePath)
        {
            var clockLists = new List<ClockList>();
            {
                using (STFReader stf = new STFReader(conversion.Input, false))
                {
                    var clockBlock = new ClockBlock(stf, routePath + @"shapes\", clockLists, "Default");
                }
            }
            return clockLists;
        }

        private static void GenerateOutput(DataConversion conversion, List<ClockList> clockLists, List<ClockShape> jso)
        {
            var clockList = clockLists.First();
            for (var i = 0; i < clockList.ClockType.Count(); i++)
            {
                var item = new ClockShape(clockList.ShapeNames[i], clockList.ClockType[i]);
                jso.Add(item);
            }

            File.WriteAllText(conversion.Output.First(), JsonConvert.SerializeObject(jso, Formatting.Indented));
        }
    }
}
