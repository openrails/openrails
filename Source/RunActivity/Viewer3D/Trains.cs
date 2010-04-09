/// COPYRIGHT 2010 by the Open Rails project.
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


namespace ORTS
{

    public class TrainDrawer
    {
        private Viewer3D Viewer;
        //public LightGlowDrawer lightGlowDrawer;

        /// THREAD SAFETY WARNING -
        public Dictionary<TrainCar, TrainCarViewer> LoadedCars = new Dictionary<TrainCar, TrainCarViewer>();   // is not written to by LoaderProcess
        public Dictionary<TrainCar, TrainCarViewer> UpdatedLoadedCars = null;  // is not read by UpdaterProcess
        public List<TrainCar> ViewableCars = new List<TrainCar>();


        public TrainDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
        }

        /// <summary>
        /// Get the viewer for this car.  If the car doesn't have a viewer, then load it.
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

        /// <summary>
        /// Executes in the UpdateProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            // fetch the list of carviewers that we generated with the last Load
            if (UpdatedLoadedCars == null)
            {
                // first pass, we don't have any yet
                UpdatedLoadedCars = new Dictionary<TrainCar, TrainCarViewer>(); 
            }
            else
            {
                Swap(ref LoadedCars, ref UpdatedLoadedCars);
            }
            // build a list of cars in viewing range for loader to ensure are loaded
            float removeDistance = Viewer.ViewingDistance * 1.5f;  
            ViewableCars.Clear();
            ViewableCars.Add(Viewer.PlayerLocomotiveViewer.Car);  // lets make sure its included even if its out of viewing range
            foreach (Train train in Viewer.Simulator.Trains)
                foreach (TrainCar car in train.Cars)
                {
                    if (ApproximateDistance(Viewer.Camera.WorldLocation, car.WorldPosition.WorldLocation) < removeDistance
                        && car != Viewer.PlayerLocomotiveViewer.Car)  // don't duplicate the player car
                        ViewableCars.Add(car);
                    //if (car.Lights != null)
                        //lightGlowDrawer = new LightGlowDrawer(Viewer, car);
                }
            // when LoadPrep returns, it launches Load in the background LoaderProcess thread
        }

        /// <summary>
        /// Executes in the LoaderProcess thread.
        /// </summary>
        public void Load(RenderProcess renderProcess)
        {
            UpdatedLoadedCars.Clear();
            foreach( TrainCar car in ViewableCars )
                if (LoadedCars.ContainsKey(car))
                {
                    UpdatedLoadedCars.Add(car, LoadedCars[car]);
                }
                else
                {
                    Console.Write("C");
                    TrainCarViewer carViewer = car.GetViewer(Viewer);
                    UpdatedLoadedCars.Add(car, carViewer);
                }
            // next time LoadPrep runs, it will fetch the UpdatedLoadedCars list of viewers.
        }

        /// <summary>
        /// Executes in the UpdateProcess thread.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            try
            {
                foreach (TrainCarViewer car in LoadedCars.Values)
                {
                    car.PrepareFrame(frame, elapsedTime);
                    //if(car.Car.Lights != null)
                        //lightGlowDrawer.PrepareFrame(frame, elapsedTime);
                }
            }
            catch( System.Exception error )  // possible thread safety violation - try again next time
            {
                Console.Error.WriteLine("Trains.PrepareFrame() \r\n   " + error.Message);
            }

        }

        public void Swap(ref Dictionary<TrainCar, TrainCarViewer> a, ref Dictionary<TrainCar, TrainCarViewer> b)
        {
            Dictionary<TrainCar, TrainCarViewer> temp = a;
            a = b;
            b = temp;
        }

        public float ApproximateDistance(WorldLocation a, WorldLocation b)
        {
            float dx = a.Location.X - b.Location.X;
            float dz = a.Location.Z - b.Location.Z;
            dx += (a.TileX - b.TileX) * 2048;
            dz += (a.TileZ - b.TileZ) * 2048;

            return Math.Abs(dx) + Math.Abs(dz);
        }

    }
}
