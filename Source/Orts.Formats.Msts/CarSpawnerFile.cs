// COPYRIGHT 2011, 2012 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using ORTS.Common;


namespace Orts.Formats.Msts
{
    public class CarSpawnerList
    {
        public string[] shapeNames; //car shape names
        public float[] distanceFrom; // the second parameter of the CarSpawnerItem
        public string ListName;
        public bool IgnoreXRotation = false; // true for humans
        public CarSpawnerList(List<CarSpawnerItemData> spawnerDataItems, string listName, bool ignoreXRotation)
        {
            IgnoreXRotation = ignoreXRotation;
            shapeNames = new string[spawnerDataItems.Count];
            distanceFrom = new float[spawnerDataItems.Count];
            ListName = listName;
            int i = 0;
            foreach (CarSpawnerItemData data in spawnerDataItems)
            {
                shapeNames[i] = data.name;
                distanceFrom[i] = data.dist;
                i++;
            }
        }
    }

    public class CarSpawnerBlock
    {
        private bool IgnoreXRotation = false;
        public CarSpawnerBlock(STFReader stf, string shapePath, List<CarSpawnerList> carSpawnerLists, string listName)
        {
            var spawnerDataItems = new List<CarSpawnerItemData>();
            {
                var count = stf.ReadInt(null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("ignorexrotation", ()=>{IgnoreXRotation = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("carspawneritem", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra CarSpawnerItem");
                        else
                        {
                            var dataItem = new CarSpawnerItemData(stf, shapePath);
                            if (Vfs.FileExists(dataItem.name))
                                spawnerDataItems.Add(dataItem);
                            else
                                STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem.name));
                        }
                    }),
                });
                if (count > 0)
                    STFException.TraceWarning(stf, count + " missing CarSpawnerItem(s)");
            }

            CarSpawnerList carSpawnerList = new CarSpawnerList(spawnerDataItems, listName, IgnoreXRotation);
            carSpawnerLists.Add(carSpawnerList);
        }

    }

    public class CarSpawnerItemData
    {
        public string name;
        public float dist;

        public CarSpawnerItemData(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            //pre fit in the shape path so no need to do it again and again later
            name = shapePath + stf.ReadString();
            dist = stf.ReadFloat(STFReader.UNITS.Distance, null);
            stf.SkipRestOfBlock();
        }
    } 

	public class CarSpawnerFile
	{
		public CarSpawnerFile(string filePath, string shapePath, List<CarSpawnerList> carSpawnerLists)
		{
			using (STFReader stf = new STFReader(filePath, false))
			{
                var carSpawnerBlock = new CarSpawnerBlock(stf, shapePath, carSpawnerLists, "Default");
			}
		}
	}
}

