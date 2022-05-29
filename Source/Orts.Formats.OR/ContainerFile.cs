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

    public class ContainerFile
    {
        public ContainerParameters ContainerParameters;


        public ContainerFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "Container":
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
        private int Index;


        public ContainerParameters(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        protected bool TryParse(JsonReader item)
        {

            // get values
            switch (item.Path)
            {
                case "Container.": break;
                case "Container.Name": Name = item.AsString(""); break;
                case "Container.Shape": ShapeFileName = item.AsString(ShapeFileName); break;
                case "Container.ContainerType": ContainerType = item.AsString("40ftHC"); break;
                case "Container.IntrinsicShapeOffset[]": 
                    switch (Index)
                    {
                        case 0:
                            IntrinsicShapeOffset.X = item.AsFloat(0.0f);
                            break;
                        case 1:
                            IntrinsicShapeOffset.Y = item.AsFloat(0.0f);
                            break;
                        case 2:
                            IntrinsicShapeOffset.Z = item.AsFloat(0.0f);
                            break;
                        default:
                            return false;
                    }
                    Index++;
                    break;
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
