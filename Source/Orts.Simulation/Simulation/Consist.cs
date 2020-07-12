// COPYRIGHT 2020 by the Open Rails project.
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

using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Simulation.Simulation
{
    public static class GenericConsist
    {
        public static IConsist LoadFile(string basePath, string name)
        {
            string filePath = LocateFile(basePath, name);
            if (filePath == null)
                throw new FileNotFoundException($"Could not locate consist: {name}");
            return LoadFile(filePath);
        }

        public static IConsist LoadFile(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".consist-or":
                    return Formats.OR.ConsistFile.LoadFrom(filePath);
                case ".con":
                    return new Formats.Msts.ConsistFile(filePath);
                default:
                    throw new InvalidOperationException("Invalid consist file type");
            }
        }

        public static string LocateFile(string basePath, string name)
        {
            string filePath = Path.Combine(basePath, "trains", "consists", name);
            string WithExtension(string ext) => Path.ChangeExtension(filePath, ext);
            if (File.Exists(WithExtension(".consist-or")))
                return WithExtension(".consist-or");
            else if (File.Exists(WithExtension(".con")))
                return WithExtension(".con");
            else
                throw new FileNotFoundException($"Consist not found: {name}");
        }

        public static bool IsTilting(string name) => name.ToLower().Contains("tilted");

        public static IEnumerable<TrainCar> LoadTrainCars(this IConsist consist, Simulator simulator, bool playerTrain = false)
        {
            UserSettings settings = simulator.Settings;
            bool first = true;
            IEnumerable<WagonSpecification> Iterator()
            {
                foreach (WagonSpecification wagonSpec in consist.GetWagonList(simulator.BasePath, settings.Folders.Folders))
                {
                    yield return wagonSpec;
                    first = false;
                }
            }
            foreach (WagonSpecification wagonSpec in Iterator())
            {
                if (!File.Exists(wagonSpec.FilePath))
                {
                    Trace.TraceWarning("Ignored missing wagon {0} in consist {1}", wagonSpec.FilePath, consist.Name);
                    continue;
                }

                TrainCar car;
                try
                {
                    car = RollingStock.Load(simulator, wagonSpec.FilePath);
                }
                catch (Exception error)
                {
                    var exception = new FileLoadException(wagonSpec.FilePath, error);
                    if (first && playerTrain) // First wagon is the player's loco and required, so issue a fatal error message
                        throw exception;
                    Trace.WriteLine(exception);
                    continue;
                }
                car.Flipped = wagonSpec.Flipped;
                car.UiD = wagonSpec.UiD;
                yield return car;
            }
        }
    }
}
