﻿// COPYRIGHT 2018 by the Open Rails project.
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
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;

namespace Orts.Formats.OR
{
    public class ExtClockFile
    {
        /// <summary>
        /// Reading STF file openrails\clocks.dat
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="shapePath"></param>
        /// <param name="clockLists"></param>
        public ExtClockFile(string filePath, string shapePath, List<ClockList> clockLists)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var clockBlock = new ClockBlock(stf, shapePath, clockLists, "Default");
            }
        }

        /// <summary>
        /// Reading JSON file animated.clocks-or
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="clockLists"></param>
        /// <param name="shapePath"></param>
        public ExtClockFile(string filePath, List<ClockList> clockLists, string shapePath)
        {
            var ClocksFile = new ClocksFile(filePath, shapePath, clockLists);
            clockLists = ClocksFile.ClockListList;
        }
    }

    public class ClockList
    {
        public List<string> shapeNames; //clock shape names
        public List<string> clockType;  //second parameter of the ClockItem is the OR-ClockType -> analog, digital
        public string ListName;
        //public ClockList(List<ClockItemData> clockDataItems, string listName)
        //{
        //    shapeNames = new List<string>();
        //    clockType = new List<string>();
        //    ListName = listName;
        //    int i = 0;
        //    foreach (ClockItemData data in clockDataItems)
        //    {
        //        shapeNames[i] = data.name;
        //        clockType[i] = data.clockType;
        //        i++;
        //    }
        //}
        public ClockList(List<ClockItemData> clockDataItems, string listName)
        {
            shapeNames = new List<string>();
            clockType = new List<string>();
            ListName = listName;
            int i = 0;
            foreach (ClockItemData data in clockDataItems)
            {
                shapeNames.Add(data.name);
                clockType.Add(data.clockType);
                i++;
            }
        }

        public ClockList()
        {
            shapeNames = new List<string>();
            clockType = new List<string>();
        }
    }

    public class ClockBlock
    {
        public ClockBlock(STFReader stf, string shapePath, List<ClockList> clockLists, string listName)
        {
            var clockDataItems = new List<ClockItemData>();
            var count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("clockitem", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra ClockItem");
                        else
                        {
                            var dataItem = new ClockItemData(stf, shapePath);
                            if (File.Exists(dataItem.name))
                                clockDataItems.Add(dataItem);
                            else
                                STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem.name));
                        }
                    }),
                });
            if (count > 0)
                STFException.TraceWarning(stf, count + " missing ClockItem(s)");
            ClockList clockList = new ClockList(clockDataItems, listName);
            clockLists.Add(clockList);
        }
    }

    public class ClockItemData
    {
        public string name = "incomplete";                                    //sFile of OR-Clock
        public string clockType = "incomplete";                               //Type of OR-Clock -> analog, digital

        public ClockItemData(string name, string clockType)
        {
            this.name = name;
            this.clockType = clockType;
        }

        public ClockItemData()
        {
        }

        public ClockItemData(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            name = shapePath + stf.ReadString();
            clockType = stf.ReadString();
            stf.SkipRestOfBlock();
        }
    }
}
