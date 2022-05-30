// COPYRIGHT 2017, 2018 by the Open Rails project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Parsers.OR;

namespace Orts.Formats.OR
{
    /// <summary>
    ///
    /// class ORWeatherFile
    /// </summary>

    public class LoadStationsOccupancyFile
    {
        public List<ContainerStationOccupancy> LoadStationsOccupancy = new List<ContainerStationOccupancy>();

        public LoadStationsOccupancyFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "ContainerStationsOccupancy[]":
                    // Ignore these items.
                    break;
                case "ContainerStationsOccupancy[].":
                    LoadStationsOccupancy.Add(new ContainerStationOccupancy(item));
                    break;
                default: return false;
            }
            return true;
        }
    }

    public class LoadStationID
    {
        public string wfile;
        public int UiD;

        public LoadStationID(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "wfile": wfile = item.AsString(""); break;
                case "UiD": UiD = item.AsInteger(0); break;
                default: return false;
            }
            return true;
        }
    }

    public class ContainerStationOccupancy
    {
        public LoadStationID LoadStatID;
        public List<LoadDataEntry> LoadData = new List<LoadDataEntry>();

        public ContainerStationOccupancy(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "LoadData[]":
                    // Ignore these items.
                    break;
                case "LoadStationID.":
                    LoadStatID = new LoadStationID(item);
                    break;
                case "LoadData[].":
                    LoadData.Add(new LoadDataEntry(item));
                    break;
                default: return false;
            }
            return true;
        }
    }

    public class LoadDataEntry
    {
        public string FileName;
        public string FolderName;
        public int StackLocation;

        public LoadDataEntry(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "File": FileName = item.AsString(""); break;
                case "Folder": FolderName = item.AsString(""); break;
                case "StackLocation": StackLocation = item.AsInteger(0); break;
                default: return false;
            }
            return true;
        }
    }
}
