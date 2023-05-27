// COPYRIGHT 2014 by the Open Rails project.
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

using System.Collections.Generic;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Work with Traffic Files
    /// </summary>
    public class TrafficFile
    {
        public Traffic_Traffic_Definition TrafficDefinition;

        public TrafficFile(string filePath)
        {
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("traffic_definition", ()=>{ TrafficDefinition = new Traffic_Traffic_Definition(stf); }),
                });
        }
    }

    /// <summary>
    /// Parses Traffic Definitions in Traffic File
    /// </summary>
    public class Traffic_Traffic_Definition
    {
        public string Name;
        public int Serial;
        public List<Traffic_Service_Definition> TrafficItems = new List<Traffic_Service_Definition>();

        public Traffic_Traffic_Definition(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.MustMatch("serial");
            Serial = stf.ReadIntBlock(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("service_definition", ()=>{ TrafficItems.Add(new Traffic_Service_Definition(stf)); }),
            });
        }
    }

    /// <summary>
    /// Parses Traffic Definition Items in Traffic Definitions in Traffic File
    /// </summary>
    public class Traffic_Service_Definition
    {
        public string Service_Definition;
        public int Time;
        public List<Traffic_Traffic_Item> TrafficDetails = new List<Traffic_Traffic_Item>();

        public Traffic_Service_Definition()
        {
        }

        public Traffic_Service_Definition(STFReader stf)
        {
            var arrivalTime = 0;
            var departTime = 0;
            var skipCount = 0;
            var distanceDownPath = 0F;
            var platformStartID = 0;

            stf.MustMatch("(");
            Service_Definition = stf.ReadString();
            Time = stf.ReadInt(null);   // Cannot use stt.ReadFloat(STFReader.UNITS.Time, null) as number will be followed by "arrivaltime"
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("arrivaltime", ()=>{ arrivalTime = (int)stf.ReadFloatBlock(STFReader.UNITS.Time, null); }),
                new STFReader.TokenProcessor("departtime", ()=>{ departTime = (int)stf.ReadFloatBlock(STFReader.UNITS.Time, null); }),
                new STFReader.TokenProcessor("skipcount", ()=>{ skipCount = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("distancedownpath", ()=>{ distanceDownPath = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("platformstartid", ()=>{ platformStartID = stf.ReadIntBlock(null);
                    TrafficDetails.Add(new Traffic_Traffic_Item(arrivalTime, departTime, skipCount, distanceDownPath, platformStartID));
                }),
            });
        }
        // This is used to convert the player data taken from the .act file into a traffic service definition for autopilot mode
        public Traffic_Service_Definition(string service_Definition, Player_Traffic_Definition player_Traffic_Definition)
        {
            var arrivalTime = 0;
            var departTime = 0;
            var skipCount = 0;
            var distanceDownPath = 0F;
            var platformStartID = 0;

            Service_Definition = service_Definition;
            Time = player_Traffic_Definition.Time;
            foreach (Player_Traffic_Item player_Traffic_Item in player_Traffic_Definition.Player_Traffic_List)
            {
                arrivalTime = (int)player_Traffic_Item.ArrivalTime.TimeOfDay.TotalSeconds;
                departTime = (int)player_Traffic_Item.DepartTime.TimeOfDay.TotalSeconds;
                distanceDownPath = player_Traffic_Item.DistanceDownPath;
                platformStartID = player_Traffic_Item.PlatformStartID;
                TrafficDetails.Add(new Traffic_Traffic_Item(arrivalTime, departTime, skipCount, distanceDownPath, platformStartID));
            }
        }

    }

    public class Traffic_Traffic_Item
    {
        public int ArrivalTime;
        public int DepartTime;
        public int SkipCount;
        public float DistanceDownPath;
        public int PlatformStartID;

        public Traffic_Traffic_Item()
        {
        }

        public Traffic_Traffic_Item(int arrivalTime, int departTime, int skipCount, float distanceDownPath, int platformStartID)
        {
            ArrivalTime = arrivalTime;
            DepartTime = departTime;
            SkipCount = skipCount;
            DistanceDownPath = distanceDownPath;
            PlatformStartID = platformStartID;
        }
    }
}
