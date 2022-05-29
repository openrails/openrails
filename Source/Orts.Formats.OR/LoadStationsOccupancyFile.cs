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
        public List<LoadStationOccupancy> LoadStationsOccupancy = new List<LoadStationOccupancy>();

        public LoadStationsOccupancyFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                    break;
                case "ContainerStationsOccupancy[].":
                    break;
                case "ContainerStationsOccupancy[].LoadStationID.":
                    LoadStationsOccupancy.Add(new ContainerStationOccupancy(item));
                    break;
                case "ContainerStationsOccupancy[].LoadData[].":
                    break;
                case "ContainerStationsOccupancy[].LoadData[].File":
                    var contStationOccupancy = LoadStationsOccupancy[LoadStationsOccupancy.Count - 1] as ContainerStationOccupancy;
                    contStationOccupancy.LoadData.Add(new LoadDataEntry(item));
                    contStationOccupancy.LoadData[contStationOccupancy.LoadData.Count - 1].ReadBlock(item);
                    break;
                default: return false;
            }
            return true;
        }
    }

    public class LoadStationOccupancy
    {
        public LoadStationID LoadStatID = new LoadStationID();

        public virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "wfile": LoadStatID.wfile = item.AsString(""); break;
                case "UiD": LoadStatID.UiD = item.AsInteger(0); break;
                default: return false;
            }
            return true;
        }
    }

    public struct LoadStationID
    {
        public string wfile;
        public int UiD;

        public LoadStationID(JsonReader json)
        {
            wfile = "";
            UiD = 0;
        }
    }

    public class ContainerStationOccupancy : LoadStationOccupancy
    {
        public List<LoadDataEntry> LoadData = new List<LoadDataEntry>();

        public ContainerStationOccupancy(JsonReader json)
        {

            json.ReadBlock(TryParse);
        }

        public override bool TryParse(JsonReader item)
        {
            // get values
            if (base.TryParse(item)) return true;
            return false;
        }
    }

    public class LoadDataEntry
    {
        public string FileName;
        public string FolderName;
        public int StackLocation;

        public LoadDataEntry(JsonReader json)
        {
            FileName = FolderName = "";
            StackLocation = 0;
        }

        public void ReadBlock(JsonReader json)
        {
            FileName = json.AsString("");
            json.ReadBlock(TryParse);
        }

        public bool TryParse(JsonReader item)
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
