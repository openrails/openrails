// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ORTS
{
    public class TrainDrawer
    {
        readonly Viewer3D Viewer;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        public Dictionary<TrainCar, TrainCarViewer> Cars = new Dictionary<TrainCar, TrainCarViewer>();
        public List<TrainCar> VisibleCars = new List<TrainCar>();
        TrainCar PlayerCar;

        public TrainDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            var visibleCars = VisibleCars;
            var cars = Cars;
            if (visibleCars.Any(c => !cars.ContainsKey(c)) || cars.Keys.Any(c => !visibleCars.Contains(c)))
            {
                var newCars = new Dictionary<TrainCar, TrainCarViewer>();
                foreach (var car in visibleCars)
                {
					try
					{
						if (cars.ContainsKey(car))
							newCars.Add(car, cars[car]);
						else
							newCars.Add(car, LoadCar(car));
					}
					catch (Exception) { }
                }
                Cars = newCars;
            }

            // Ensure the player locomotive has a cab view loaded and anything else they need.
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
                if (car.lightDrawer != null)
                    car.lightDrawer.Mark();
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
                    if (ApproximateDistance(Viewer.Camera.CameraWorldLocation, car.WorldPosition.WorldLocation) < removeDistance && car != Viewer.PlayerLocomotive)
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
                if (car.lightDrawer != null)
                    car.lightDrawer.PrepareFrame(frame, elapsedTime);
        }

        TrainCarViewer LoadCar(TrainCar car)
        {
            Trace.Write("C");
            var carViewer = car.GetViewer(Viewer);
            if (car.Lights != null)
                carViewer.lightDrawer = new LightDrawer(Viewer, car);
            return carViewer;
        }

        float ApproximateDistance(WorldLocation a, WorldLocation b)
        {
            var dx = a.Location.X - b.Location.X;
            var dz = a.Location.Z - b.Location.Z;
            dx += (a.TileX - b.TileX) * 2048;
            dz += (a.TileZ - b.TileZ) * 2048;
            return Math.Abs(dx) + Math.Abs(dz);
        }
    }
}
