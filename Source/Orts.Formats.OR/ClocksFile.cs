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

using Orts.Parsers.Msts;
using Orts.Parsers.OR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Formats.OR
{
    /// <summary>
    /// Reads animated.clocks-or and parses it
    /// </summary>
    public class ClocksFile
    {
        /// <summary>
        /// Contains list of valid shape files for clocks
        /// </summary>
        public List<ClockShape> ClockShapeList = new List<ClockShape>();

        private string ShapePath = null;
        private ClockShape ClockShape = null;
        private const string invalid = "invalid";
        private const string incomplete = "incomplete";

        /// <summary>
        /// Reads JSON file, parsing valid data into ClockShapeList and logging errors.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="clockShapeList"></param>
        /// <param name="shapePath"></param>
        public ClocksFile(string fileName, List<ClockShape> clockShapeList, string shapePath)
        {
            ShapePath = shapePath;
            ClockShapeList = clockShapeList;
            JsonReader.ReadFile(fileName, TryParse);

            // Filter out objects with essentials properties that are either incomplete or invalid.
            var BadProperties = invalid + incomplete;
            ClockShapeList = ClockShapeList
                .Where(r => (BadProperties.Contains(r.Name) || BadProperties.Contains(r.ClockType)) == false)
                .ToList();
        }

        /// <summary>
        /// Parses next item from JSON data, populating a ClockShape and issuing error messages.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual bool TryParse(JsonReader item)
        {
            var stringValue = "";
            switch (item.Path)
            {
                case "":
                    // Ignore these items.
                    break;

                case "[].":
                    // Create an object with properties which are initially incomplete, so omissions can be detected and the object rejected later.
                    ClockShape = new ClockShape(incomplete, incomplete);
                    ClockShapeList.Add(ClockShape);
                    break;
                
                case "[].Name":
                    // Parse the property with default value as invalid, so errors can be detected and the object rejected later.
                    stringValue = item.AsString(invalid);
                    var path = ShapePath + stringValue;
                    ClockShape.Name = path;
                    if (stringValue == invalid)
                        return false;
                    if (File.Exists(path) == false)
                        Trace.TraceWarning($"Non-existent shape file {path} referenced in animated.clocks-or");
                    break;

                case "[].ClockType":
                    stringValue = item.AsString(invalid);
                    ClockShape.ClockType = stringValue;
                    if (stringValue == invalid)
                        return false;
                    if (stringValue != "analog")
                        Trace.TraceWarning($"ClockType \"{stringValue}\" found, but \"analog\" expected");
                    break;

                default:
                    Trace.TraceWarning($"Unexpected entry \"{item.Path}\" found"); 
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Holds a reference to a shape file and a clocktype.
    /// Can parse an STF element to populate the object.
    /// </summary>
    public class ClockShape
    {
        public string Name { get; set; }
        public string ClockType { get; set; }

        public ClockShape(string name, string clockType)
        {
            this.Name = name;
            this.ClockType = clockType;
        }

        public ClockShape(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            ClockType = stf.ReadString();
            stf.SkipRestOfBlock();
        }
    }

    #region STF file openrails\clocks.dat. Used by Contrib.DataConverter
    public class ClockList
    {
        public List<string> ShapeNames; //clock shape names
        public List<string> ClockType;  //second parameter of the ClockItem is the OR-ClockType -> analog, digital
        public string ListName;

        public ClockList(List<ClockShape> clockDataItems, string listName)
        {
            ShapeNames = new List<string>();
            ClockType = new List<string>();
            ListName = listName;
            int i = 0;
            foreach (ClockShape data in clockDataItems)
            {
                ShapeNames.Add(data.Name);
                ClockType.Add(data.ClockType);
                i++;
            }
        }
    }

    public class ClockBlock
    {
        public ClockBlock(STFReader stf, string shapePath, List<ClockList> clockLists, string listName)
        {
            var clockDataItems = new List<ClockShape>();
            var count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("clockitem", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra ClockItem");
                        else
                        {
                            var dataItem = new ClockShape(stf, shapePath);
                            if (File.Exists(shapePath + dataItem.Name))
                                clockDataItems.Add(dataItem);
                            else
                                STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem.Name));
                        }
                    }),
                });
            if (count > 0)
                STFException.TraceWarning(stf, count + " missing ClockItem(s)");
            ClockList clockList = new ClockList(clockDataItems, listName);
            clockLists.Add(clockList);
        }
    }
    #endregion
}