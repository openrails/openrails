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
            using (STFReader inf = new STFReader(filePath, false))
                while (!inf.EOF)
                    switch (inf.ReadItem().ToLower())
                    {
                        case "service_definition": ReadServiceDefintionBlock(inf); break;
                        case "(": inf.SkipRestOfBlock(); break;
                    }
        }
        private void ReadServiceDefintionBlock(STFReader inf)
        {
            inf.MustMatch("(");
            while(!inf.EndOfBlock())
                switch (inf.ReadItem().ToLower())
                {
                    case "serial": Serial = inf.ReadIntBlock(STFReader.UNITS.None, null); break;
                    case "name": Name = inf.ReadItemBlock(null); break;
                    case "train_config": Train_Config = inf.ReadItemBlock(null); break;
                    case "pathid": PathID = inf.ReadItemBlock(null); break;
                    case "maxwheelacceleration": MaxWheelAcceleration = inf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                    case "efficiency": Efficiency = inf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                    case "(": inf.SkipRestOfBlock(); break;
                }
        }
	} // SRVFile
}

