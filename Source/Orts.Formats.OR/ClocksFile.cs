// COPYRIGHT 2021 by the Open Rails project.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Parsers.OR;

namespace Orts.Formats.OR
{
    /// <summary>
    /// Reads animated.clocks-or and parses it
    /// </summary>
    public class ClocksFile
    {
        public List<ClockShape> ClockShapeList = new List<ClockShape>();
        private string ShapePath = null;
        private ClockShape ClockShape = null;

        public ClocksFile(string fileName, string shapePath, List<ClockShape> clockShapeList)
        {
            ShapePath = shapePath;
            ClockShapeList = clockShapeList;
            JsonReader.ReadFile(fileName, TryParse);
        }

        protected virtual bool TryParse(JsonReader item)
        {
            var invalid = "invalid";
            var stringValue = "";
            switch (item.Path)
            {
                case "":
                    // Ignore these items.
                    break;

                case "ClockShapeList[].":
                    ClockShape = new ClockShape();
                    ClockShapeList.Add(ClockShape);
                    break;
                
                case "ClockShapeList[].name":
                    stringValue = item.AsString(invalid);
                    var path = ShapePath + stringValue;
                    ClockShape.name = path;
                    if (stringValue == invalid)
                        return false;
                    if (File.Exists(path) == false)
                        Trace.TraceWarning($"Non-existent shape file referenced {path}");                    
                    break;

                case "ClockShapeList[].clockType":
                    stringValue = item.AsString(invalid);
                    ClockShape.clockType = stringValue;
                    if (stringValue == invalid)
                        return false;
                    if (stringValue != "analog")
                        Trace.TraceWarning($"ClockType \"{stringValue}\" found, but \"analog\" expected");
                    break;

                default: return false;
            }
            return true;
        }
    }
}