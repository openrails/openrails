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

using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ORTS.ContentManager.Models
{
    public class Service
    {
        public readonly string Name;
        public readonly string ID;
        public readonly DateTime StartTime;
        public readonly string Consist;
        public readonly string Path;

        public readonly IEnumerable<Stop> Stops;

        public Service(Content content)
        {
            Debug.Assert(content.Type == ContentType.Service);
            if (System.IO.Path.GetExtension(content.PathName).Equals(".srv", StringComparison.OrdinalIgnoreCase))
            {
                var file = new SRVFile(content.PathName);
                Name = file.Name;
                Consist = file.Train_Config;
                Path = file.PathID;

                Debug.Assert(content is ContentMSTSService);
                var msts = content as ContentMSTSService;
                var actFile = new ACTFile(content.Parent.PathName);
                if (msts.IsPlayer)
                {
                    var activityTraffic = actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Player_Traffic_Definition;

                    ID = "0";
                    StartTime = MSTSTimeToDateTime(activityTraffic.Time);
                    Stops = from stop in activityTraffic.Player_Traffic_List
                            select new Stop(0, stop.PlatformStartID, stop.DistanceDownPath, MSTSTimeToDateTime(stop.ArrivalTime), MSTSTimeToDateTime(stop.DepartTime));
                }
                else
                {
                    var trfFile = new TRFFile(msts.TrafficPathName);
                    var activityService = (from service in actFile.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList
                                           where service.Name == content.Name
                                           select service).First();
                    var trafficService = (from service in trfFile.TrafficDefinition.TrafficItems
                                          where service.Service_Definition == content.Name
                                          select service).First();

                    ID = activityService.UiD.ToString();
                    StartTime = MSTSTimeToDateTime(activityService.Time);
                    Stops = trafficService.TrafficDetails.Zip(activityService.ServiceList, (tt, stop) => new Stop(0, stop.PlatformStartID, stop.DistanceDownPath, MSTSTimeToDateTime(tt.ArrivalTime), MSTSTimeToDateTime(tt.DepartTime)));
                }
            }
        }

        /// <summary>
        /// Convert <see cref="ACTFile"/> arrival and departure times in to normalized times.
        /// </summary>
        DateTime MSTSTimeToDateTime(DateTime mstsPlayerTime)
        {
            return mstsPlayerTime.AddYears(1999);
        }

        /// <summary>
        /// Convert <see cref="TRFFile"/> arrival and departure times in to normalized times.
        /// </summary>
        DateTime MSTSTimeToDateTime(int mstsAITime)
        {
            return new DateTime(2000, 1, 1).AddSeconds(mstsAITime);
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

    public static class ServiceExtensions
    {
        // TODO: This prototype is taken from .NET 4.0 and should be removed when we upgrade.
        /// <summary>
        /// Applies a specified function to the corresponding elements of two sequences, producing a sequence of the results.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="first">The first input sequence.</param>
        /// <param name="second">The second input sequence.</param>
        /// <param name="resultSelector">A function that specifies how to combine the corresponding elements of the two sequences.</param>
        /// <returns>An <see cref="IEnumerable<T>"/> that contains elements of the two input sequences, combined by <paramref name="resultSelector"/>.</returns>
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            var f = first.GetEnumerator();
            var s = second.GetEnumerator();
            while (f.MoveNext() && s.MoveNext())
                yield return resultSelector(f.Current, s.Current);
        }
    }
}
