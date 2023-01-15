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
    public class OverlayLayer
    {
        public string name;
        public bool show;
    }

    public enum TypeOfPointOnApiMap {
        track,
        named,
        rest
    }

    public class PointOnApiMap
    {
        public LatLon latLon;
        public string color;
        public TypeOfPointOnApiMap typeOfPointOnApiMap;
        public string name;
    }

    public class LineOnApiMap
    {
        public LatLon latLonFrom;
        public LatLon latLonTo;
    }

    public class InfoApiMap
    {
        public string typeOfLocomotive;
        public string baseLayer;
        public OverlayLayer[] overlayLayer;

        public LinkedList<PointOnApiMap> pointOnApiMapList;
        public LinkedList<LineOnApiMap> lineOnApiMapList;

        public float latMin;
        public float latMax;
        public float lonMin;
        public float lonMax;

        public const string RegistrySection = "ApiMap";
        public const string RegistryKeyBaselayer = "baseLayer";
        public const string RegistrySectionLayers = "ApiMap\\Layers";

        public InfoApiMap(string powerSupplyName, string settingsFilePath, string registryKey) 
        {
            initTypeOfLocomotive(powerSupplyName);
            initLayers(settingsFilePath, registryKey);

            pointOnApiMapList = new LinkedList<PointOnApiMap>();
            lineOnApiMapList = new LinkedList<LineOnApiMap>();

            latMax = -999999f;
            latMin = +999999f;
            lonMax = -999999f;
            lonMin = +999999f;
        }

        private void initTypeOfLocomotive(string powerSupplyName)
        {
            string powerSupplyNameToLower = powerSupplyName.ToLower();
            if (powerSupplyNameToLower.Contains("steam"))
            {
                typeOfLocomotive = "steam";
            }
            else
            {
                if (powerSupplyNameToLower.Contains("diesel"))
                {
                    typeOfLocomotive = "diesel";
                }
                else
                {
                    typeOfLocomotive = "electric";
                }
            }
        }

        private void initLayers(string settingsFilePath, string registryKey)
        {
            var store = SettingsStore.GetSettingStore(settingsFilePath, registryKey, RegistrySection);
            baseLayer = (string)store.GetUserValue(RegistryKeyBaselayer, typeof(string));

            overlayLayer = null;
            store = SettingsStore.GetSettingStore(settingsFilePath, registryKey, RegistrySectionLayers);
            string[] names = store.GetUserNames();
            if (names.Length > 0)
            {
                overlayLayer = new OverlayLayer[names.Length];

                int index = 0;
                foreach (string name in names)
                {
                    overlayLayer[index] = new OverlayLayer
                    {
                        name = name,
                        show = (bool)store.GetUserValue(name, typeof(bool))
                    };
                    index++;
                }
            }
        }

        public static void storeLayerSetting(
            string settingsFilePath, string registryKey,
            string layerAction, string layerName)
        {
            if (layerAction == "baseLayerChange")
            {
                var store = SettingsStore.GetSettingStore(settingsFilePath, registryKey, InfoApiMap.RegistrySection);
                store.SetUserValue(InfoApiMap.RegistryKeyBaselayer, layerName);
            }
            else
            {
                var store = SettingsStore.GetSettingStore(settingsFilePath, registryKey, InfoApiMap.RegistrySectionLayers);
                if (layerAction == "overlayAdd")
                {
                    store.SetUserValue(layerName, (bool)true);
                }
                if (layerAction == "overlayRemove")
                {
                    store.SetUserValue(layerName, (bool)false);
                }
            }
        }

        public static LatLon convertToLatLon(int TileX, int TileZ, float X, float Y, float Z)
        {
            double latitude = 1f;
            double longitude = 1f;

            WorldLocation mstsLocation = new WorldLocation(TileX, TileZ, X, Y, Z);
            new WorldLatLon().ConvertWTC(TileX, TileZ, mstsLocation.Location, ref latitude, ref longitude);
            LatLon latLon = new LatLon(MathHelper.ToDegrees((float)latitude), MathHelper.ToDegrees((float)longitude));

            return latLon;
        }

        public void addToPointOnApiMap(
            int TileX, int TileZ, float X, float Y, float Z,
            string color, TypeOfPointOnApiMap typeOfPointOnApiMap, string name)
        {
            LatLon latLon = InfoApiMap.convertToLatLon(TileX, TileZ, X, Y, Z);

            addToPointOnApiMap(latLon,
                color, typeOfPointOnApiMap, name);
        }

        public void addToPointOnApiMap(
            LatLon latLon,
            string color, TypeOfPointOnApiMap typeOfPointOnApiMap, string name)
        {
            PointOnApiMap pointOnApiMap = new PointOnApiMap
            {
                latLon = latLon,
                color = color,
                typeOfPointOnApiMap = typeOfPointOnApiMap,
                name = name
            };

            if (pointOnApiMap.typeOfPointOnApiMap == TypeOfPointOnApiMap.named)
            {
                // named last is the list so that they get displayed on top
                pointOnApiMapList.AddLast(pointOnApiMap);
            }
            else
            {
                pointOnApiMapList.AddFirst(pointOnApiMap);
            }

            if (pointOnApiMap.latLon.Lat > latMax)
            {
                latMax = pointOnApiMap.latLon.Lat;
            }
            if (pointOnApiMap.latLon.Lat < latMin)
            {
                latMin = pointOnApiMap.latLon.Lat;
            }
            if (pointOnApiMap.latLon.Lon > lonMax)
            {
                lonMax = pointOnApiMap.latLon.Lon;
            }
            if (pointOnApiMap.latLon.Lon < lonMin)
            {
                lonMin = pointOnApiMap.latLon.Lon;
            }
        }

        public void addToLineOnApiMap(LatLon latLonFrom, LatLon latLongTo)
        {
            LineOnApiMap lineOnApiMap = new LineOnApiMap
            {
                latLonFrom = latLonFrom,
                latLonTo = latLongTo
            };
            lineOnApiMapList.AddLast(lineOnApiMap);
        }
    }
}
