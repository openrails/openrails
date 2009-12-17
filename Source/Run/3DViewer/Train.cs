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
        private Viewer Viewer;


        public TrainDrawer(Viewer viewer)
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

        public Dictionary<TrainCarSimulator, TrainCarViewer> LoadedCars = new Dictionary<TrainCarSimulator, TrainCarViewer>();

        public void Update(GameTime gameTime)
        {
            try
            {
                // TODO, reading camera's location should be locked to ensure its atomic
                WorldLocation viewerWorldLocation = new WorldLocation(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location);

                Dictionary<TrainCarSimulator, bool> carsInUse = new Dictionary<TrainCarSimulator, bool>();

                // list all cars in viewing range
                float removeDistance = Viewer.ViewingDistance * 1.5f;   // apply some hysteresis
                foreach( Train train in Viewer.Simulator.Trains )
                    foreach( TrainCarSimulator car in train.Cars )
                        if (ApproximateDistance(viewerWorldLocation, car.WorldPosition.WorldLocation) < removeDistance)
                            carsInUse.Add(car, true);

                // remove cars not in the list
                List<TrainCarSimulator> carsToRemove = new List<TrainCarSimulator>();
                foreach( TrainCarSimulator car in LoadedCars.Keys )
                    if( !carsInUse.ContainsKey( car ) )
                    {
                        Console.Write("c");
                        TrainCarViewer carViewer = LoadedCars[car];
                        Viewer.Components.Remove(carViewer);
                        carsToRemove.Add(car);
                        carViewer.Unload();
                        carViewer.Dispose(); 
                    }
                foreach (TrainCarSimulator car in carsToRemove)
                    LoadedCars.Remove(car);
                carsToRemove.Clear();


                // Add trains coming into range
                float addDistance = Viewer.ViewingDistance * 1.2f;
                foreach (Train train in Viewer.Simulator.Trains)
                    foreach (TrainCarSimulator car in train.Cars)
                        if (ApproximateDistance(viewerWorldLocation, car.WorldPosition.WorldLocation) < addDistance
                            && !LoadedCars.ContainsKey(car))
                        {
                            Console.Write("C");
                            TrainCarViewer carViewer;
                            if (car.GetType() == typeof(ElectricLocomotiveSimulator))
                                carViewer = new ElectricLocomotiveViewer(Viewer, (ElectricLocomotiveSimulator)car);
                            else if (car.GetType() == typeof(SteamLocomotivePhysics))
                                carViewer = new SteamLocomotiveViewer(Viewer, (SteamLocomotivePhysics)car);
                            else if (car.GetType() == typeof(DieselLocomotiveSimulator))
                                carViewer = new DieselLocomotiveViewer(Viewer, (DieselLocomotiveSimulator)car);
                            else
                                carViewer = new TrainCarViewer(Viewer, car);
                            Viewer.Components.Add(carViewer);
                            LoadedCars.Add(car, carViewer);
                        }
            }
            catch // we could throw a thread safety exception if the core process is adding or removing cars, in this case ignore it and try again later
            {
            }
        }
    }
}
