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
    public static class GenericTrain
    {
        /// <summary>
        /// Load a train file by name.
        /// </summary>
        /// <param name="basePath">The content directory.</param>
        /// <param name="name">The train filename (without an extension) to search for.</param>
        /// <returns>The loaded train.</returns>
        public static ITrainFile LoadFile(string basePath, string name)
        {
            string filePath = TrainFileUtilities.ResolveTrainFile(basePath, name);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Could not locate train: {name}");
            return LoadFile(filePath);
        }

        /// <summary>
        /// Load a train file by path.
        /// </summary>
        /// <param name="filePath">The path to the train.</param>
        /// <returns>The loaded train.</returns>
        public static ITrainFile LoadFile(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".train-or":
                    return Formats.OR.TrainFile.LoadFrom(filePath);
                case ".con":
                    return new Formats.Msts.ConsistFile(filePath);
                default:
                    throw new InvalidDataException("Unknown train format");
            }
        }

        /// <summary>
        /// Determine whether a train should tilt.
        /// </summary>
        public static bool IsTilting(string name) => name.ToLower().Contains("tilted");

        /// <summary>
        /// Load the wagons of a train into the simulator.
        /// </summary>
        /// <param name="train">The train file to load.</param>
        /// <param name="simulator">The game instance.</param>
        /// <param name="flip">If set, reverse the train.</param>
        /// <param name="playerTrain">If set, errors that affect the first wagon are fatal.</param>
        /// <param name="preference">Request a formation with a particular lead locomotive, identified by a filesystem path.</param>
        /// <returns>The list of loaded <see cref="TrainCar"/>s.</returns>
        public static IEnumerable<TrainCar> LoadCars(this ITrainFile train, Simulator simulator, bool flip = false, bool playerTrain = false, PreferredLocomotive preference = null)
        {
            UserSettings settings = simulator.Settings;
            bool first = true;
            IEnumerable<WagonReference> Iterator()
            {
                IEnumerable<WagonReference> list;
                if (flip)
                    list = train.GetReverseWagonList(simulator.BasePath, settings.Folders.Folders, preference);
                else
                    list = train.GetForwardWagonList(simulator.BasePath, settings.Folders.Folders, preference);
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
                    Trace.TraceWarning("Ignored missing wagon {0} in train {1}", wagonRef.FilePath, train.DisplayName);
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
