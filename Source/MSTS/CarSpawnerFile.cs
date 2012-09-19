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

