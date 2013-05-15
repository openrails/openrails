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
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;


namespace MSTS
{

	public class CarSpawnerFile
	{
		public string[] shapeNames; //car shape names
		public float[] distanceFrom; // the second parameter of the CarSpwanerItem

		public CarSpawnerFile(string filePath, string shapePath)
		{
			List<CarSpawnerItemData> spawnerDataItems = new List<CarSpawnerItemData>();
			using (STFReader stf = new STFReader(filePath, false))
			{
				var count = stf.ReadInt(STFReader.UNITS.None, null);
				stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("carspawneritem", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra CarSpawnerItem");
                        else
                            spawnerDataItems.Add(new CarSpawnerItemData(stf, shapePath));
                    }),
                });
				if (count > 0)
                    STFException.TraceWarning(stf, count + " missing CarSpawnerItem(s)");
			}

            shapeNames = new string[spawnerDataItems.Count];
            distanceFrom = new float[spawnerDataItems.Count];
			int i = 0;
			foreach (CarSpawnerItemData data in spawnerDataItems) {
				shapeNames[i] = data.name;
				distanceFrom[i] = data.dist;
				i++;
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
				name = shapePath+stf.ReadString();
				dist = stf.ReadFloat(STFReader.UNITS.Distance, null);
				stf.SkipRestOfBlock();
			}
		} // TrackType

	} // class CVFFile
} // namespace MSTS

