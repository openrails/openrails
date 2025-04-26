// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D
{
    public class TrainDrawer
    {
        readonly Viewer Viewer;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        public Dictionary<TrainCar, TrainCarViewer> Cars = new Dictionary<TrainCar, TrainCarViewer>();
        List<TrainCar> VisibleCars = new List<TrainCar>();
        TrainCar PlayerCar;

        public TrainDrawer(Viewer viewer)
        {
            Viewer = viewer;
            Viewer.Simulator.QueryCarViewerLoaded += Simulator_QueryCarViewerLoaded;
        }

        void Simulator_QueryCarViewerLoaded(object sender, Simulator.QueryCarViewerLoadedEventArgs e)
        {
            var cars = Cars;
            if (cars.ContainsKey(e.Car))
                e.Loaded = true;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            var cancellation = Viewer.LoaderProcess.CancellationToken;
            var visibleCars = VisibleCars;
            var cars = Cars;
            if (visibleCars.Any(c => !cars.ContainsKey(c)) || cars.Keys.Any(c => !visibleCars.Contains(c)))
            {
                var newCars = new Dictionary<TrainCar, TrainCarViewer>();
                foreach (var car in visibleCars)
                {
                    if (cancellation.IsCancellationRequested)
                        break;
                    try
					{
						if (cars.ContainsKey(car))
							newCars.Add(car, cars[car]);
						else
							newCars.Add(car, LoadCar(car));
					}
					catch (Exception error) 
                    {
                        Trace.WriteLine(new FileLoadException(car.WagFilePath, error));
                    }
                }
                Cars = newCars;
				//for those cars not visible now, will unload them (to remove attached sound)
				foreach (var car in cars)
				{
					if (!visibleCars.Contains(car.Key))
					{
						car.Value.Unload();
					}
				}
			}

            // Ensure the player locomotive has a cab view loaded and anything else they need.
            cars = Cars;
            if (PlayerCar != null && cars.ContainsKey(PlayerCar))
                cars[PlayerCar].LoadForPlayer();
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            var cars = Cars;
            foreach (var car in cars.Values)
            {
                car.Mark();
                car.lightDrawer?.Mark();
            }
            CABTextureManager.Mark(Viewer);
        }

        [CallOnThread("Updater")]
        public TrainCarViewer GetViewer(TrainCar car)
        {
            var cars = Cars;
            if (cars.ContainsKey(car))
                return cars[car];
            var newCars = new Dictionary<TrainCar, TrainCarViewer>(cars);
            newCars.Add(car, LoadCar(car));
            // This will actually race against the loader's Load() call above, but that's okay since the TrainCar
            // we're given here is always the player's locomotive - specifically included in LoadPrep() below.
            Cars = newCars;
            return newCars[car];
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            var visibleCars = new List<TrainCar>();
            var removeDistance = Viewer.Settings.ViewingDistance * 1.5f;
            visibleCars.Add(Viewer.PlayerLocomotive);
            foreach (var train in Viewer.Simulator.Trains)
                foreach (var car in train.Cars)
                    if (WorldLocation.ApproximateDistance(Viewer.Camera.CameraWorldLocation, car.WorldPosition.WorldLocation) < removeDistance && car != Viewer.PlayerLocomotive)
                        visibleCars.Add(car);
            VisibleCars = visibleCars;
            PlayerCar = Viewer.Simulator.PlayerLocomotive;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var cars = Cars;
            foreach (var car in cars.Values)
                car.PrepareFrame(frame, elapsedTime);
            // Do the lights separately for proper alpha sorting
            foreach (var car in cars.Values)
                car.lightDrawer?.PrepareFrame(frame, elapsedTime);
        }

        TrainCarViewer LoadCar(TrainCar car)
        {
            TrainCarViewer carViewer =
                car is MSTSDieselLocomotive ? new MSTSDieselLocomotiveViewer(Viewer, car as MSTSDieselLocomotive) :
                car is MSTSElectricLocomotive ? new MSTSElectricLocomotiveViewer(Viewer, car as MSTSElectricLocomotive) :
                car is MSTSSteamLocomotive ? new MSTSSteamLocomotiveViewer(Viewer, car as MSTSSteamLocomotive) :
                car is MSTSControlTrailerCar ? new MSTSControlTrailerCarViewer(Viewer, car as MSTSControlTrailerCar) :
                car is MSTSLocomotive ? new MSTSLocomotiveViewer(Viewer, car as MSTSLocomotive) :
                car is MSTSWagon ? new MSTSWagonViewer(Viewer, car as MSTSWagon) :
                null;
            if (car.Lights != null) // Don't make a light viewer when there are no lights
                carViewer.lightDrawer = new LightViewer(Viewer, car, carViewer);
            return carViewer;
        }
    }
}
