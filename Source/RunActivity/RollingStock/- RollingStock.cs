using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using MSTS;

namespace ORTS
{
    public static class RollingStock
    {
        public static TrainCar Load(Simulator simulator, string wagFilePath, TrainCar previousCar)
        {
            GenericWAGFile wagFile = SharedGenericWAGFileManager.Get(wagFilePath);  
            TrainCar car;
            if( wagFile.OpenRails != null 
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
                    return car;
                }
                catch (Exception error)
                {
                    Trace.WriteLine(error);
                    // on error, fall through and try loading without the custom dll
                }
            }
            if (!wagFile.IsEngine)
            {   
                // its an ordinary MSTS wagon
                car = new MSTSWagon(simulator, wagFilePath, previousCar);
            }
            else
            {   
                // its an ordinary MSTS engine of some type.
                if (wagFile.Engine.Type == null)
                    throw new InvalidDataException(wagFilePath + "\r\n\r\nEngine type missing");

                switch (wagFile.Engine.Type.ToLower())
                {
                        // TODO complete parsing of proper car types
                    case "electric": car = new MSTSElectricLocomotive(simulator, wagFilePath, previousCar); break;
                    case "steam": car = new MSTSSteamLocomotive(simulator, wagFilePath, previousCar); break;
                    case "diesel": car = new MSTSDieselLocomotive(simulator, wagFilePath, previousCar); break;
					default: throw new InvalidDataException(wagFilePath + "\r\n\r\nUnknown engine type: " + wagFile.Engine.Type);
                }
            }
            return car;
        }

        public static void Save(BinaryWriter outf, TrainCar car)
        {
            MSTSWagon wagon = (MSTSWagon)car;   // extend this when we introduce other types of rolling stock
            outf.Write(wagon.WagFilePath);
            wagon.Save(outf);
        }

		public static TrainCar Restore(Simulator simulator, BinaryReader inf, Train train, TrainCar previousCar)
        {
            TrainCar car = Load(simulator, inf.ReadString(), previousCar);
            car.Train = train;
            car.Restore(inf);
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
            public EngineClass Engine = null;
            public OpenRailsData OpenRails = null;

            public GenericWAGFile(string filenamewithpath)
            {
                WagFile(filenamewithpath);
            }

            public void WagFile(string filenamewithpath)
            {
                using (STFReader stf = new STFReader(filenamewithpath, false))
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("engine", ()=>{ Engine = new EngineClass(stf); }),
                        new STFReader.TokenProcessor("_openrails", ()=>{ OpenRails = new OpenRailsData(stf); }),
                    });
            }

            public class EngineClass
            {
                public string Type = null;

                public EngineClass(STFReader stf)
                {
                    stf.MustMatch("(");
                    stf.ReadString();
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("type", ()=>{ Type = stf.ReadStringBlock(null); }),
                    });
                }
            } // class WAGFile.Engine

            public class OpenRailsData
            {
                public string DLL = null;

                public OpenRailsData(STFReader stf)
                {
                    stf.MustMatch("(");
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("dll", ()=>{ DLL = stf.ReadStringBlock(null); }),
                    });
                }
            } // class WAGFile.Engine

        }// class WAGFile


    }
}
