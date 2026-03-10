// COPYRIGHT 2009, 2010, 2012, 2013 by the Open Rails project.
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

using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Orts.Simulation.RollingStocks
{
    public static class RollingStock
    {
        public static TrainCar Load(Simulator simulator, Train train, string wagFilePath, bool initialize = true)
        {
            GenericWAGFile wagFile = SharedGenericWAGFileManager.Get(wagFilePath);
            TrainCar car;
            if (wagFile.OpenRails != null
               && wagFile.OpenRails.DLL != null)
            {  // wag file specifies an external DLL
                try
                {
                    // TODO search the path list
                    string wagFolder = Path.GetDirectoryName(wagFilePath);
                    string dllPath = ORTSPaths.FindTrainCarPlugin(wagFolder, wagFile.OpenRails.DLL);
                    Assembly customDLL = Assembly.LoadFrom(dllPath);
                    object[] args = new object[] { wagFilePath };
                    car = (TrainCar)customDLL.CreateInstance("ORTS.CustomCar", true, BindingFlags.CreateInstance, null, args, null, null);

                    car.Train = train;
                    train.Cars.Add(car);

                    return car;
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(wagFile.OpenRails.DLL, error));
                    // on error, fall through and try loading without the custom dll
                }
            }

            if (!wagFile.IsEngine)
            {
                if (wagFilePath.ToLower().Contains("orts_eot"))
                    car = new EOT(simulator, wagFilePath);
                else
                    // its an ordinary MSTS wagon
                    car = new MSTSWagon(simulator, wagFilePath);
            }
            else
            {
                // its an ordinary MSTS engine of some type.
                if (wagFile.Engine.Type == null)
                    throw new InvalidDataException(wagFilePath + "\r\n\r\nEngine type missing");

                switch (wagFile.Engine.Type.ToLower())
                {
                    // TODO complete parsing of proper car types
                    case "electric": car = new MSTSElectricLocomotive(simulator, wagFilePath); break;
                    case "steam": car = new MSTSSteamLocomotive(simulator, wagFilePath); break;
                    case "diesel": car = new MSTSDieselLocomotive(simulator, wagFilePath); break;
                    case "control": car = new MSTSControlTrailerCar(simulator, wagFilePath); break;
                    default: throw new InvalidDataException(wagFilePath + "\r\n\r\nUnknown engine type: " + wagFile.Engine.Type);
                }
            }

            car.Train = train;

            if (car is MSTSWagon wagon)
            {
                wagon.Load();

                if (initialize)
                {
                    wagon.Initialize();
                }
            }

            // Loading is complete, the car can be added to the train
            train.Cars.Add(car);

            return car;
        }

        /// <summary>
        /// Utility class to avoid loading multiple copies of the same file.
        /// </summary>
        public class SharedGenericWAGFileManager
        {
            private static Dictionary<string, GenericWAGFile> SharedWAGFiles = new Dictionary<string, GenericWAGFile>();

            public static GenericWAGFile Get(string path)
            {
                if (!SharedWAGFiles.ContainsKey(path))
                {
                    GenericWAGFile wagFile = new GenericWAGFile(path);
                    SharedWAGFiles.Add(path, wagFile);
                    return wagFile;
                }
                else
                {
                    return SharedWAGFiles[path];
                }
            }
        }

        /// <summary>
        /// This is an abbreviated parse to determine where to direct the file.
        /// </summary>
        public class GenericWAGFile
        {
            public bool IsEngine { get { return Engine != null; } }
            public EngineClass Engine;
            public OpenRailsData OpenRails;

            public GenericWAGFile(string filenamewithpath)
            {
                WagFile(filenamewithpath);
            }

            public void WagFile(string filenamewithpath)
            {
                using (STFReader stf = ConsistGenerator.IsWagonRecognized(filenamewithpath)
                    ? new STFReader(ConsistGenerator.GetWagon(filenamewithpath), filenamewithpath, System.Text.Encoding.UTF8, false)
                    : new STFReader(filenamewithpath, false))
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("engine", ()=>{ Engine = new EngineClass(stf); }),
                        new STFReader.TokenProcessor("_openrails", ()=>{ OpenRails = new OpenRailsData(stf); }),
                    });
            }

            public class EngineClass
            {
                public string Type;

                public EngineClass(STFReader stf)
                {
                    stf.MustMatch("(");
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("type", ()=>{ Type = stf.ReadStringBlock(null); }),
                    });
                }
            } // class WAGFile.Engine

            public class OpenRailsData
            {
                public string DLL;

                public OpenRailsData(STFReader stf)
                {
                    stf.MustMatch("(");
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("ortsdll", ()=>{ DLL = stf.ReadStringBlock(null); }),
                    });
                }
            } // class WAGFile.Engine

        }// class WAGFile


    }
}
