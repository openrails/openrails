/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;

namespace MSTS
{
	/// <summary>
	/// Work with Service Files
	/// </summary>
	public class SRVFile
	{
		public int Serial;
		public string Name;
		public string Train_Config;   // name of the consist file, no extension
		public string PathID;  // name of the path file, no extension
		public float MaxWheelAcceleration;
		public float Efficiency;
		public string TimeTableItem;

		/// <summary>
		/// Open a service file, 
		/// filePath includes full path and extension
		/// </summary>
		/// <param name="filePath"></param>
		public SRVFile( string filePath )
		{
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("service_definition", ()=> { stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(STFReader.UNITS.None, null); }),
                        new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("train_config", ()=>{ Train_Config = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("pathid", ()=>{ PathID = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("maxwheelacceleration", ()=>{ MaxWheelAcceleration = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                        new STFReader.TokenProcessor("efficiency", ()=>{ Efficiency = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                    });}),
                });
        }
	} // SRVFile
}

