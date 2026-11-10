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
    /// class ContainerFile
    /// </summary>

    public class ContainerFile
    {
        public ContainerParameters ContainerParameters;


        public ContainerFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "Container":
                    // Ignore these items.
                    break;
                case "Container.":
                    ContainerParameters = new ContainerParameters(item);
                    break;
                default: return false;
            }
            return true;
        }
    }


    public class ContainerParameters
    {
        public string Name;
        public string ShapeFileName;
        public string ContainerType;  
        public Vector3 IntrinsicShapeOffset = new Vector3(0f, 1.17f, 0f);
        public float EmptyMassKG = -1;
        public float MaxMassWhenLoadedKG = -1;

        public ContainerParameters(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "Name": Name = item.AsString(""); break;
                case "Shape": ShapeFileName = item.AsString(ShapeFileName); break;
                case "ContainerType": ContainerType = item.AsString("40ftHC"); break;
                case "IntrinsicShapeOffset[]": IntrinsicShapeOffset = item.AsVector3(Vector3.Zero); break;
                case "EmptyMassKG": EmptyMassKG = item.AsFloat(-1); break;
                case "MaxMassWhenLoadedKG": MaxMassWhenLoadedKG = item.AsFloat(-1); break;
                default: return false;
            }
            return true;
        }

        // restore
        public ContainerParameters(BinaryReader inf)
        {
 //           Overcast = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
//            outf.Write(Overcast);
        }
    }
}
