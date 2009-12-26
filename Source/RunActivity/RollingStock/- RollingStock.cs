using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;

namespace ORTS
{
    public static class RollingStock
    {
        public static TrainCar Load(string wagFilePath)
        {
            WAGFile wagFile = SharedWAGFileManager.Get(wagFilePath);  
            TrainCar car;
            if (!wagFile.IsEngine)
                car = new TrainCar(wagFile);
            else
            {
                if (wagFile.Engine.Type == null)
                    throw new System.Exception(wagFilePath + "\r\n\r\nEngine type missing");

                switch (wagFile.Engine.Type.ToLower())
                {
                    case "steam": car = new SteamLocomotive(wagFile); break;
                    case "diesel": car = new DieselLocomotive(wagFile); break;
                    case "electric": car = new ElectricLocomotive(wagFile); break;
                    default: throw new System.Exception(wagFilePath + "\r\n\r\nUnknown engine type: " + wagFile.Engine.Type);
                }

                if (car.WagFile.Engine.CabView != null)
                {
                    string CVFFilePath = Path.GetDirectoryName(wagFilePath) + @"\CABVIEW\" + car.WagFile.Engine.CabView;
                    car.CVFFile = new CVFFile(CVFFilePath);
                }


            }
            return car;
        }

    }
}
