// COPYRIGHT 2009, 2010 by the Open Rails project.
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
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
	/// <summary>
	/// Work with Service Files
	/// </summary>
	public class ServiceFile
	{
		public int Serial;
		public string Name;
		public string Train_Config;   // name of the consist file, no extension
		public string PathID;  // name of the path file, no extension
		public float MaxWheelAcceleration;
		public float Efficiency;
		public string TimeTableItem;
        public TimeTable TimeTable;

		/// <summary>
		/// Open a service file, 
		/// filePath includes full path and extension
		/// </summary>
		/// <param name="filePath"></param>
		public ServiceFile( string filePath )
		{
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("service_definition", ()=> { stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(null); }),
                        new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("train_config", ()=>{ Train_Config = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("pathid", ()=>{ PathID = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("maxwheelacceleration", ()=>{ MaxWheelAcceleration = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                        new STFReader.TokenProcessor("efficiency", ()=>{ Efficiency = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
                        new STFReader.TokenProcessor("timetable", ()=>{ TimeTable = new TimeTable(stf); }),
                    });}),
                });
        }

        // Used for explore in activity mode
        public ServiceFile()
        {
            Efficiency = 0.9f;
            TimeTable = new TimeTable();
        }
	} // SRVFile

    public class TimeTable
    {
        public float InitialSpeed;
 
        public TimeTable (STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("startingspeed", ()=>{ InitialSpeed = stf.ReadFloatBlock(STFReader.UNITS.Any, null); }),
            });
        }

        // Used for explore in activity mode
        public TimeTable()
        {

        }
    }
}

