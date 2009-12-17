/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.IO;
using MSTS;

namespace ORTS
{

///////////////////////////////////////////////////
///   SIMULATION BEHAVIOUR
///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>
    public class TrainCarSimulator
    {
        public WAGFile WagFile;
        public CVFFile CVFFile = null;

        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public bool Flipped = false; // the car is reversed in the consist
        public Train Train = null;  // the car is connected to this train
        public float DistanceM = 0.0f;  // running total of distance travelled - always positive, updated by train physics
        public float SpeedMpS = 0.0f; // meters pers second; updated by train physics, relative to direction of car  50mph = 22MpS

        public float MotiveForceN = 0.0f;   // ie motor and gravity in Newtons  - signed relative to direction of car - 
        public float FrictionForceN = 0.0f; // in Newtons ( kg.m/s^2 ) unsigned
        public float Variable1 = 0.0f;  // used to convey status to soundsource
        public float Variable2 = 0.0f;
        public float Variable3 = 0.0f;

        public List<CarEventHandler> EventHandlers = new List<CarEventHandler>();


        protected TrainCarSimulator(WAGFile wagFile)
        {
            WagFile = wagFile;
        }

        public virtual void HandleKeyboard(KeyboardInput keyboard, GameTime gametime)
        {
        }


        public virtual void Update(GameTime gameTime)
        {
            if (SpeedMpS < 0.1)
            {
                // Starting Friction 
                //
                //                      Above Freezing   Below Freezing
                //    Journal Bearing      25 lb/ton        35 lb/ton   (short ton)
                //     Roller Bearing       5 lb/ton        15 lb/ton
                //
                // [2009-10-25 from http://www.arema.org/publications/pgre/ ]

                float NpKG = 30f /* lb/ton */ * 4.84e-3f;  // convert lbs/short-ton to N/kg

                FrictionForceN = WagFile.Wagon.MassKG * NpKG;

                //FrictionForceN *= 2; // for better playability  // TODO why do we need this?
            }
            else
            {
                // Davis Formula for rolling friction
                float Asqft = 100f; // square feet cross sectional area
                float Wst = 30f / 4f; // short tons per axle weight of the car
                float Vmph = SpeedMpS * 0.000621371192f /* miles/M */ * 3600f /* sec/hr */; // convert speed to mph
                float N = 4; // number of axles
                float RlbPst; // resistance in lbs per ton

                // for friction bearings
                RlbPst = 1.3f + 29f / Wst + 0.045f * Vmph + 0.0005f * Asqft * Vmph * Vmph / (Wst * N);

                // for roller bearings
                // R = 0.6f + 20f / W + 0.01f * V + 0.07f * A * V * V / (W * N);

                float NpKG = RlbPst * 4.84e-3f;  // convert lbs/short-ton to N/kg

                FrictionForceN = WagFile.Wagon.MassKG * NpKG;

                //FrictionForceN *= 3; // for better playability  // TODO why do we need this
            }

            // Typical brake shoe force = 20,000 pounds or 89,000 newtons
            FrictionForceN += 89e3f * Train.TrainBrakePercent / 100f; 

            // TODO add static friction effect

            // TODO compute gravity as 'motive force'
        }

        public static TrainCarSimulator Create(string wagFilePath)
        {
            WAGFile wagFile = SharedWAGFileManager.Get(wagFilePath);  // TODO, look this up in database to avoid duplicate data
            TrainCarSimulator car;
            if (!wagFile.IsEngine)
                car = new TrainCarSimulator(wagFile);
            else 
            {
                if( wagFile.Engine.Type == null )
                    throw new System.Exception(wagFilePath + "\r\n\r\nEngine type missing" );

                switch (wagFile.Engine.Type.ToLower())
                {
                    case "steam": car = new SteamLocomotivePhysics(wagFile); break;
                    case "diesel": car = new DieselLocomotiveSimulator(wagFile); break;
                    case "electric": car = new ElectricLocomotiveSimulator(wagFile); break;
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

        public void CreateEvent(int eventID)
        {
            foreach (CarEventHandler eventHandler in EventHandlers)
                eventHandler.HandleCarEvent(eventID);
        }
    }


} // namespace ORTS
