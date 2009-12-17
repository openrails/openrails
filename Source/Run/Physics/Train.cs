/* TRAINS
 * 
 * Contains code to represent a train as a list of TrainCars and to handle the physics of moving
 * the train through the Track Database.
 * 
 * A train has:
 *  - a list of TrainCars 
 *  - a front and back position in the TDB ( represented by TDBTravellers )
 *  - speed
 *  - MU signals that are relayed from player locomtive to other locomotives and cars such as:
 *      - direction
 *      - throttle percent
 *      - brack percent  ( TODO, this should be changed to brake pipe pressure )
 *      
 *  Individual TrainCars provide information on friction and motive force they are generating.
 *  This is consolidated up by the train class into overall movement for the train.
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS;
using System.IO;
using Microsoft.Xna.Framework.Input;


namespace ORTS
{
    public class Train
    {
        public List<TrainCarSimulator> Cars = new List<TrainCarSimulator>();  // listed front to back
        public TrainCarSimulator FirstCar { get { return Cars[0]; } }
        public TrainCarSimulator LastCar { get { return Cars[Cars.Count - 1]; } }
        public TDBTraveller RearTDBTraveller;   // positioned at the back of the last car in the train
        public TDBTraveller FrontTDBTraveller; // positioned at the front of the train by CalculatePositionOfCars
        public PATTraveller PATTraveller = null;      // tracks where we are in the path file - this is the next waypoint
        public float SpeedMpS = 0.0f;  // meters per second +ve forward, -ve when backing

        // These signals pass through to all cars and locomotives on the train
        public bool TrainDirectionForward = true; //set by player locomotive to control MU'd locomotives
        public float TrainThrottlePercent = 0;  // set by player locomotive to control MU'd locomotives 
        public float TrainBrakePercent = 0;     // set by player locomotove to control entire train brakes
                                                // todo , make this air pressure

        /// <summary>
        /// Used by AI code to control train movement
        /// overrides all locomotive and train physics
        /// Note: while under AI control, locomotives
        /// will not get any keyboard commands.
        /// </summary>
        /// <param name="MpSS"></param>
        public void SetAccelleration(float MpSS)
        {
            // TODO finish this.
        }

        public void Update(GameTime gameTime)
        {
            float timeS = 0;
            if( gameTime != null )
                 timeS = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // some extremely simple 'physics'
            float TrainMotiveForceN = 0;     // newtons relative to forward train direction
            float TrainFrictionForceN  = 0;   // newtons always positive
            float TrainMassKG = 0;            // kg 
            foreach (TrainCarSimulator car in Cars)
            {
                car.Update(gameTime);
                TrainMotiveForceN += car.MotiveForceN;
                TrainFrictionForceN += car.FrictionForceN;
                TrainMassKG += car.WagFile.Wagon.MassKG;
            }

            // easier to calculate if TrainMotiveForceN is positive
            bool reversed = TrainMotiveForceN < 0;
            if( reversed ) TrainMotiveForceN *= -1;

            if( TrainMotiveForceN > TrainFrictionForceN )
            {
                TrainMotiveForceN -= TrainFrictionForceN;
                TrainFrictionForceN = 0;
            }
            else
            {
                TrainFrictionForceN -= TrainMotiveForceN;
                TrainMotiveForceN = 0;
            }
            if (reversed)
                TrainMotiveForceN *= -1;

            float accellerationMpS2 = TrainMotiveForceN / TrainMassKG;
            float decellerationMpS2 = TrainFrictionForceN / TrainMassKG;

            if (SpeedMpS >= 0)
            {
                SpeedMpS -= decellerationMpS2 * timeS;
                if (SpeedMpS < 0)
                    SpeedMpS = 0;
                SpeedMpS += accellerationMpS2 * timeS;
            }
            else
            {
                SpeedMpS += decellerationMpS2 * timeS;
                if (SpeedMpS > 0)
                    SpeedMpS = 0;
                SpeedMpS += accellerationMpS2 * timeS;
            }

            float distanceM = SpeedMpS * timeS;

            CalculatePositionOfCars( distanceM );

        }

        /// <summary>
        /// Cars have been added to the rear of the train, recalc the rearTDBtraveller
        /// </summary>
        /// <param name="distance"></param>
        public void RepositionRearTraveller()
        {

            TDBTraveller traveller = new TDBTraveller(FrontTDBTraveller);
            traveller.ReverseDirection();

            // process the cars first to last
            for (int i = 0; i < Cars.Count; ++i)
            {
                TrainCarSimulator car = Cars[i];
                car.SpeedMpS = SpeedMpS * ( car.Flipped ? -1: 1 );

                float bogieSpacing = car.WagFile.Wagon.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                // traveller is positioned at the front of the car
                // advance to the first bogie 
                traveller.Move((car.WagFile.Wagon.Length - bogieSpacing) / 2.0f);
                int tileX = traveller.TileX;
                int tileZ = traveller.TileZ;
                float x = traveller.X;
                float y = traveller.Y;
                float z = traveller.Z;
                traveller.Move(bogieSpacing);

                // normalize across tile boundaries
                while (tileX > traveller.TileX) { x += 2048; --tileX; }
                while (tileX < traveller.TileX) { x -= 2048; ++tileX; }
                while (tileZ > traveller.TileZ) { z += 2048; --tileZ; }
                while (tileZ < traveller.TileZ) { z -= 2048; ++tileZ; }


                // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                car.WorldPosition.XNAMatrix = Matrix.Identity;
                if (!car.Flipped)
                {
                    //  Rotate matrix 180' around Y axis.
                    car.WorldPosition.XNAMatrix.M11 = -1;
                    car.WorldPosition.XNAMatrix.M33 = -1;
                }
                car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                car.WorldPosition.TileX = traveller.TileX;
                car.WorldPosition.TileZ = traveller.TileZ;

                traveller.Move((car.WagFile.Wagon.Length - bogieSpacing) / 2.0f);  // Move to the rear of the car 
            }

            traveller.ReverseDirection();
           RearTDBTraveller = traveller;
        } // RepositionRearTraveller


        /// <summary>
        /// Distance is the signed distance the cars are moving.
        /// </summary>
        /// <param name="distance"></param>
        public void CalculatePositionOfCars( float distance )
        {
            // TODO Debug shows location of trian.tdbtraveller
            /*
            WorldPosition wm = new WorldPosition();
            wm.TileMatrix = Matrix.CreateTranslation(train.TDBTraveller.X, train.TDBTraveller.Y, -train.TDBTraveller.Z);
            wm.TileX = train.TDBTraveller.TileX;
            wm.TileZ = train.TDBTraveller.TileZ;
            TrainCars[0].WorldPosition = wm;
            return;
            */

            RearTDBTraveller.Move(distance);

            TDBTraveller traveller = new TDBTraveller(RearTDBTraveller);
            // The traveller location represents the back of the train.

            // process the cars last to first
            for (int i = Cars.Count - 1; i >= 0; --i)
            {
                TrainCarSimulator car = Cars[i];
                car.SpeedMpS = SpeedMpS * (car.Flipped ? -1 : 1 );
                car.DistanceM += Math.Abs(distance);

                float bogieSpacing = car.WagFile.Wagon.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                // traveller is positioned at the back of the car
                // advance to the first bogie 
                traveller.Move((car.WagFile.Wagon.Length - bogieSpacing) / 2.0f);
                int tileX = traveller.TileX;
                int tileZ = traveller.TileZ;
                float x = traveller.X;
                float y = traveller.Y;
                float z = traveller.Z;
                traveller.Move(bogieSpacing);

                // normalize across tile boundaries
                while (tileX > traveller.TileX) { x += 2048; --tileX; }
                while (tileX < traveller.TileX) { x -= 2048; ++tileX; }
                while (tileZ > traveller.TileZ) { z += 2048; --tileZ; }
                while (tileZ < traveller.TileZ) { z -= 2048; ++tileZ; }


                // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                car.WorldPosition.XNAMatrix = Matrix.Identity;
                if (car.Flipped)
                {
                    //  Rotate matrix 180' around Y axis.
                    car.WorldPosition.XNAMatrix.M11 = -1;
                    car.WorldPosition.XNAMatrix.M33 = -1;
                }
                car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                car.WorldPosition.TileX = traveller.TileX;
                car.WorldPosition.TileZ = traveller.TileZ;

                traveller.Move((car.WagFile.Wagon.Length - bogieSpacing) / 2.0f);  // Move to the front of the car 
            }

            FrontTDBTraveller = traveller;
        } // CalculatePositionOfCars

    }// class Train


}
