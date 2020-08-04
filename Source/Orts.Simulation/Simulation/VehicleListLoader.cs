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
using ORTS.Content;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Simulation.Simulation
{
    public static class VehicleListLoader
    {
        /// <summary>
        /// Load a vehicle list by name.
        /// </summary>
        /// <param name="basePath">The content directory.</param>
        /// <param name="name">The vehicle list filename (without an extension) to search for.</param>
        /// <returns>The loaded vehicle list.</returns>
        public static IVehicleList LoadFile(string basePath, string name)
        {
            string filePath = VehicleListUtilities.ResolveVehicleList(basePath, name);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Could not locate vehicle list: {name}");
            return LoadFile(filePath);
        }

        /// <summary>
        /// Load a vehicle list file by path.
        /// </summary>
        /// <param name="filePath">The path to the vehicle list.</param>
        /// <returns>The loaded vehicle list.</returns>
        public static IVehicleList LoadFile(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".train-or":
                    return Formats.OR.TrainFile.LoadFrom(filePath);
                case ".con":
                    return new Formats.Msts.ConsistFile(filePath);
                default:
                    throw new InvalidDataException("Unknown vehicle list format");
            }
        }

        /// <summary>
        /// Load the wagons of a vehicle list into the simulator.
        /// </summary>
        /// <param name="vehicleList">The vehicle list file to load.</param>
        /// <param name="simulator">The game instance.</param>
        /// <param name="flip">If set, reverse the formation.</param>
        /// <param name="playerTrain">If set, errors that affect the first wagon are fatal.</param>
        /// <param name="preference">Request a formation with a particular lead locomotive, identified by a filesystem path.</param>
        /// <returns>The list of loaded <see cref="TrainCar"/>s.</returns>
        public static IEnumerable<TrainCar> LoadCars(this IVehicleList vehicleList, Simulator simulator, bool flip = false, bool playerTrain = false, PreferredLocomotive preference = null)
        {
            UserSettings settings = simulator.Settings;
            bool first = true;
            IEnumerable<WagonReference> Iterator()
            {
                IEnumerable<WagonReference> list;
                if (flip)
                    list = vehicleList.GetReverseWagonList(simulator.BasePath, settings.Folders.Folders, preference);
                else
                    list = vehicleList.GetForwardWagonList(simulator.BasePath, settings.Folders.Folders, preference);
                foreach (WagonReference wagonRef in list)
                {
                    yield return wagonRef;
                    first = false;
                }
            }
            foreach (WagonReference wagonRef in Iterator())
            {
                if (!File.Exists(wagonRef.FilePath))
                {
                    Trace.TraceWarning("Ignored missing wagon {0} in train {1}", wagonRef.FilePath, vehicleList.DisplayName);
                    continue;
                }

                TrainCar car;
                try
                {
                    car = RollingStock.Load(simulator, wagonRef.FilePath);
                }
                catch (Exception error)
                {
                    var exception = new FileLoadException(wagonRef.FilePath, error);
                    if (first && playerTrain) // First wagon is the player's loco and required, so issue a fatal error message
                        throw exception;
                    Trace.WriteLine(exception);
                    continue;
                }
                car.Flipped = wagonRef.Flipped;
                car.UiD = wagonRef.UiD;
                yield return car;
            }
        }
    }
}
