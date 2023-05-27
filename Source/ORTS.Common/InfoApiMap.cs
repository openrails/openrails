// COPYRIGHT 2013 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using ORTS.Common;

namespace Orts.Common
{
    public enum TypeOfPointOnApiMap
    {
        Track,
        Named,
        Rest
    }

    public class PointOnApiMap
    {
        public LatLon LatLon;
        public string Color;
        public TypeOfPointOnApiMap TypeOfPointOnApiMap;
        public string Name;
    }

    public class LineOnApiMap
    {
        public LatLon LatLonFrom;
        public LatLon LatLonTo;
    }

    public class InfoApiMap
    {
        public string TypeOfLocomotive;

        public LinkedList<PointOnApiMap> PointOnApiMapList;
        public LinkedList<LineOnApiMap> LineOnApiMapList;

        public float LatMin;
        public float LatMax;
        public float LonMin;
        public float LonMax;

        public InfoApiMap(string powerSupplyName)
        {
            InitTypeOfLocomotive(powerSupplyName);

            PointOnApiMapList = new LinkedList<PointOnApiMap>();
            LineOnApiMapList = new LinkedList<LineOnApiMap>();

            LatMax = -999999f;
            LatMin = +999999f;
            LonMax = -999999f;
            LonMin = +999999f;
        }

        private void InitTypeOfLocomotive(string powerSupplyName)
        {
            string powerSupplyNameToLower = powerSupplyName.ToLower();
            if (powerSupplyNameToLower.Contains("steam"))
            {
                TypeOfLocomotive = "steam";
            }
            else
            {
                if (powerSupplyNameToLower.Contains("diesel"))
                {
                    TypeOfLocomotive = "diesel";
                }
                else
                {
                    TypeOfLocomotive = "electric";
                }
            }
        }

        public static LatLon ConvertToLatLon(int tileX, int tileZ, float x, float y, float z)
        {
            double latitude = 1f;
            double longitude = 1f;

            WorldLocation mstsLocation = new WorldLocation(tileX, tileZ, x, y, z);
            new WorldLatLon().ConvertWTC(tileX, tileZ, mstsLocation.Location, ref latitude, ref longitude);
            LatLon latLon = new LatLon(MathHelper.ToDegrees((float)latitude), MathHelper.ToDegrees((float)longitude));

            return latLon;
        }

        public void AddToPointOnApiMap(
            int tileX, int tileZ, float x, float y, float z,
            string Color, TypeOfPointOnApiMap typeOfPointOnApiMap, string name)
        {
            LatLon latLon = InfoApiMap.ConvertToLatLon(tileX, tileZ, x, y, z);

            AddToPointOnApiMap(latLon,
                Color, typeOfPointOnApiMap, name);
        }

        public void AddToPointOnApiMap(
            LatLon latLon,
            string color, TypeOfPointOnApiMap typeOfPointOnApiMap, string name)
        {
            PointOnApiMap pointOnApiMap = new PointOnApiMap
            {
                LatLon = latLon,
                Color = color,
                TypeOfPointOnApiMap = typeOfPointOnApiMap,
                Name = name
            };

            if (pointOnApiMap.TypeOfPointOnApiMap == TypeOfPointOnApiMap.Named)
            {
                // named last is the list so that they get displayed on top
                PointOnApiMapList.AddLast(pointOnApiMap);
            }
            else
            {
                PointOnApiMapList.AddFirst(pointOnApiMap);
            }

            if (pointOnApiMap.LatLon.Lat > LatMax)
            {
                LatMax = pointOnApiMap.LatLon.Lat;
            }
            if (pointOnApiMap.LatLon.Lat < LatMin)
            {
                LatMin = pointOnApiMap.LatLon.Lat;
            }
            if (pointOnApiMap.LatLon.Lon > LonMax)
            {
                LonMax = pointOnApiMap.LatLon.Lon;
            }
            if (pointOnApiMap.LatLon.Lon < LonMin)
            {
                LonMin = pointOnApiMap.LatLon.Lon;
            }
        }

        public void AddToLineOnApiMap(LatLon latLonFrom, LatLon latLongTo)
        {
            LineOnApiMap lineOnApiMap = new LineOnApiMap
            {
                LatLonFrom = latLonFrom,
                LatLonTo = latLongTo
            };
            LineOnApiMapList.AddLast(lineOnApiMap);
        }
    }
}
