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
        /// <summary>
        /// Load a consist file by name.
        /// </summary>
        /// <param name="basePath">The content directory.</param>
        /// <param name="name">The consist filename (without an extension) to search for.</param>
        /// <returns>The loaded consist.</returns>
        public static IConsist LoadFile(string basePath, string name)
        {
            string filePath = ConsistUtilities.ResolveConsist(basePath, name);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Could not locate consist: {name}");
            return LoadFile(filePath);
        }

        /// <summary>
        /// Load a consist file by path.
        /// </summary>
        /// <param name="filePath">The path to the consist.</param>
        /// <returns>The loaded consist.</returns>
        public static IConsist LoadFile(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".consist-or":
                    return Formats.OR.ConsistFile.LoadFrom(filePath);
                case ".con":
                    return new Formats.Msts.ConsistFile(filePath);
                default:
                    throw new InvalidDataException("Unknown consist format");
            }
        }

        /// <summary>
        /// Determine whether a consist should tilt.
        /// </summary>
        public static bool IsTilting(string name) => name.ToLower().Contains("tilted");

        /// <summary>
        /// Load the wagons of a consist into the simulator.
        /// </summary>
        /// <param name="consist">The consist file to load.</param>
        /// <param name="simulator">The game instance.</param>
        /// <param name="playerTrain">If set, errors that affect the first wagon are fatal.</param>
        /// <returns>The list of loaded <see cref="TrainCar"/>s.</returns>
        public static IEnumerable<TrainCar> LoadTrainCars(this IConsist consist, Simulator simulator, bool playerTrain = false)
        {
            UserSettings settings = simulator.Settings;
            bool first = true;
            IEnumerable<WagonReference> Iterator()
            {
                foreach (WagonReference wagonRef in consist.GetWagonList(simulator.BasePath, settings.Folders.Folders))
                {
                    yield return wagonRef;
                    first = false;
                }
            }
            foreach (WagonReference wagonRef in Iterator())
            {
                if (!File.Exists(wagonRef.FilePath))
                {
                    Trace.TraceWarning("Ignored missing wagon {0} in consist {1}", wagonRef.FilePath, consist.DisplayName);
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
