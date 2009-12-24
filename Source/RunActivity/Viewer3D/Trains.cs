/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Threading;
using MSTS;

/// The Terrain consists of TerrainTiles 2km square each subdivided 16 x 16 into TerrainPatch's
/// The TerrainTile class

namespace ORTS
{

    public class TrainDrawer
    {
        private Viewer3D Viewer;


        public TrainDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
        }

        public float ApproximateDistance( WorldLocation a, WorldLocation b )
        {
            float dx = a.Location.X - b.Location.X;
            float dz = a.Location.Z - b.Location.Z;
            dx += (a.TileX - b.TileX) * 2048;
            dz += (a.TileZ - b.TileZ) * 2048;

            return Math.Abs(dx) + Math.Abs(dz);
        }

        /// THREAD SAFETY WARNING - only LoaderPocess can write to or change this array.
        public Dictionary<TrainCar, TrainCarViewer> LoadedCars = new Dictionary<TrainCar, TrainCarViewer>();

        WorldLocation viewerWorldLocation;

        public void LoadPrep()
        {
            // TODO, buffer in all train locations as well to prevent issues
            viewerWorldLocation = new WorldLocation(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location);
        }

        /// <summary>
        /// Get the viewer for this car.  If the car doesn't have a viewer, then load it.
        /// THREAD SAFETY WARNING - use inside LoaderProcess, or when Loader Process is idle
        /// </summary>
        /// <param name="car"></param>
        /// <returns></returns>
        public TrainCarViewer GetViewer(TrainCar car)
        {
            if (LoadedCars.ContainsKey(car))
                return LoadedCars[car];

            Console.Write("C");
            TrainCarViewer carViewer = car.GetViewer(Viewer);
            LoadedCars.Add(car, carViewer);
            return carViewer;
        }

        public void Load(RenderProcess renderProcess)
        {

            Dictionary<TrainCar, bool> carsInUse = new Dictionary<TrainCar, bool>();

            // list all cars in viewing range
            float removeDistance = Viewer.ViewingDistance * 1.5f;   // apply some hysteresis
            foreach (Train train in Viewer.Simulator.Trains)
                foreach (TrainCar car in train.Cars)
                    if (ApproximateDistance(viewerWorldLocation, car.WorldPosition.WorldLocation) < removeDistance)
                        carsInUse.Add(car, true);

            // include the player's locomotive - we can't unload the viewer no matter how far away it is from the loco
            // THREAD SAFETY WARNING - wat if UpdateProcess switches the player to a different loco?
            {
                TrainCarViewer carViewer = Viewer.PlayerLocomotiveViewer;
                if (!carsInUse.ContainsKey(carViewer.Car))
                    carsInUse.Add(carViewer.Car, true);
            }

            // remove cars not in the list
            List<TrainCar> carsToRemove = new List<TrainCar>();
            foreach (TrainCar car in LoadedCars.Keys)
                if (!carsInUse.ContainsKey(car))
                {
                    Console.Write("c");
                    TrainCarViewer carViewer = LoadedCars[car];
                    carsToRemove.Add(car);
                    carViewer.Unload();
                }
            foreach (TrainCar car in carsToRemove)
                // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
                LoadedCars.Remove(car);
            carsToRemove.Clear();


            // Add trains coming into range
            float addDistance = Viewer.ViewingDistance * 1.2f;
            foreach (Train train in Viewer.Simulator.Trains)
                foreach (TrainCar car in train.Cars)
                    if (ApproximateDistance(viewerWorldLocation, car.WorldPosition.WorldLocation) < addDistance
                        && !LoadedCars.ContainsKey(car))
                    {
                        Console.Write("C");
                        TrainCarViewer carViewer = car.GetViewer(Viewer);
                        // THREAD SAFETY WARNING - UpdateProcess could read this array at any time
                        LoadedCars.Add(car, carViewer);
                    }
        }

        public void Update(GameTime gameTime)
        {
            try
            {
                // THREAD SAFETY WARNING - LoaderProcess could write to this array or change the size at any time
                foreach (TrainCarViewer car in LoadedCars.Values)
                    car.Update(gameTime);
            }
            catch  // thread safety violation - try again next time
            {
            }
        }

        public void PrepareFrame(RenderFrame frame, GameTime gameTime)
        {
            try
            {
                foreach (TrainCarViewer car in LoadedCars.Values)
                    car.PrepareFrame(frame, gameTime);
            }
            catch  // thread safety violation - try again next time
            {
            }

        }
    }
}
