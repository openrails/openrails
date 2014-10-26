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

using MSTS.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ORTS.ContentManager.Formats
{
    public class Service
    {
        public readonly string Consist;
        public readonly string Path;

        public readonly IEnumerable<Stop> Stops;

        Service(Content content)
        {
            Debug.Assert(content.Type == ContentType.Service);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".srv", StringComparison.OrdinalIgnoreCase))
            {
                var file = new SRVFile(content.PathName);
                Consist = file.Train_Config;
                Path = file.PathID;
            }
        }

        internal Service(Content content, Player_Service_Definition playerService)
            : this(content.Get(playerService.Name, ContentType.Service))
        {
            Stops = from stop in playerService.Player_Traffic_Definition.Player_Traffic_List
                    select new Stop(0, stop.PlatformStartID, stop.DistanceDownPath, stop.ArrivalTime, stop.DepartTime);
        }

        internal Service(Content content, Service_Definition service)
            : this(content.Get(service.Name, ContentType.Service))
        {
            Stops = from stop in service.ServiceList
                    select new Stop(0, stop.PlatformStartID, stop.DistanceDownPath, DateTime.MinValue, DateTime.MinValue);
        }

        public class Stop
        {
            public readonly int StationID;
            public readonly int PlatformID;
            public readonly float Distance;
            public readonly DateTime ArrivalTime;
            public readonly DateTime DepartureTime;

            internal Stop(int stationID, int platformID, float distance, DateTime arrivalTime, DateTime departureTime)
            {
                StationID = stationID;
                PlatformID = platformID;
                Distance = distance;
                ArrivalTime = arrivalTime;
                DepartureTime = departureTime;
            }
        }
    }
}
