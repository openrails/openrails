/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 



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
			List<CarSpwanerItemData> spawnerDataItems = new List<CarSpwanerItemData>();
			int realCount = 0;
			using (STFReader stf = new STFReader(filePath, false))
			{
				int count = stf.ReadInt(STFReader.UNITS.None, null);
				
				stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("carspawneritem", ()=>{ spawnerDataItems.Add(new CarSpwanerItemData(stf, shapePath)); realCount++;}),
                });
				if (count != realCount)
					STFException.TraceError(stf, "Count mismatch.");
			}

			shapeNames = new string[realCount];
			distanceFrom = new float[realCount];
			int i = 0;
			foreach (CarSpwanerItemData data in spawnerDataItems) {
				shapeNames[i] = data.name;
				distanceFrom[i] = data.dist;
				i++;
			}
		}

		public class CarSpwanerItemData
		{
			public string name;
			public float dist;

			public CarSpwanerItemData(STFReader stf, string shapePath)
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

