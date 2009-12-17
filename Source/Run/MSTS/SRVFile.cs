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
			STFReader inf = new STFReader( filePath );
			try
			{
				while( !inf.EOF() )
				{
					inf.ReadToken();

					switch( inf.Tree )
					{
						case "Service_Definition(Serial":
							Serial = inf.ReadIntBlock();
							break;
						case "Service_Definition(Name":
							Name = inf.ReadStringBlock();
							break;
						case "Service_Definition(Train_Config":
							Train_Config = inf.ReadStringBlock();
							break;
						case "Service_Definition(PathID":
							PathID = inf.ReadStringBlock();
							break;
						case "Service_Definition(MaxWheelAcceleration":
							MaxWheelAcceleration = (float)inf.ReadDoubleBlock();
							break;
						case "Service_Definition(Efficiency":
							Efficiency = (float)inf.ReadDoubleBlock();
							break;
						case "Service_Definition(TimeTable":
                            inf.SkipBlock(); // todo complete parse
							break;
					}
				}
			}
			finally
			{
				inf.Close();
			}
		}


	} // SRVFile

}

