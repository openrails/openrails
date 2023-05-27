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
using System.Collections.Generic;
using Orts.Parsers.OR;
using ORTS.Common;

namespace Orts.Formats.OR
{
    /// <summary>
    ///
    /// class ORWeatherFile
    /// </summary>

    public class LoadStationsPopulationFile
    {
        public List<ContainerStationPopulation> LoadStationsPopulation = new List<ContainerStationPopulation>();

        public LoadStationsPopulationFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "ContainerStationsPopulation[]":
                    // Ignore these items.
                    break;
                case "ContainerStationsPopulation[].":
                    LoadStationsPopulation.Add(new ContainerStationPopulation(item));
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

    public class ContainerStationPopulation
    {
        public LoadStationID LoadStatID;
        public List<LoadDataEntry> LoadData = new List<LoadDataEntry>();

        public ContainerStationPopulation(JsonReader json)
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
        public LoadState LoadState;

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
                case "LoadState": Enum.TryParse(item.AsString(""), out LoadState); break;
                default: return false;
            }
            return true;
        }
    }
}
