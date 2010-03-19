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
        public List<TrainCar> Cars = new List<TrainCar>();  // listed front to back
        public TrainCar FirstCar { get { return Cars[0]; } }
        public TrainCar LastCar { get { return Cars[Cars.Count - 1]; } }
        public TDBTraveller RearTDBTraveller;   // positioned at the back of the last car in the train
        public TDBTraveller FrontTDBTraveller; // positioned at the front of the train by CalculatePositionOfCars
        public float SpeedMpS = 0.0f;  // meters per second +ve forward, -ve when backing
        public Train UncoupledFrom = null;  // train not to coupled back onto

        // These signals pass through to all cars and locomotives on the train
        public Direction MUDirection = Direction.Forward; //set by player locomotive to control MU'd locomotives
        public float MUThrottlePercent = 0;  // set by player locomotive to control MU'd locomotives 
        public float BrakeLine1PressurePSI = 90;     // set by player locomotive to control entire train brakes
        public float BrakeLine2PressurePSI = 0;     // extra line for dual line systems
        public float BrakeLine3PressurePSI = 0;     // extra line just in case

        // For AI control of the train
        public float AITrainBrakePercent
        {
            get { return aiBrakePercent; }
            set { aiBrakePercent = value; foreach (TrainCar car in Cars) car.BrakeSystem.AISetPercent(aiBrakePercent); }
        }
        private float aiBrakePercent = 0;
        public float AITrainThrottlePercent 
        { 
            get { return MUThrottlePercent; } 
            set{ MUThrottlePercent = value; } 
        }
        public bool AITrainDirectionForward 
        { 
            get { return MUDirection == Direction.Forward; } 
            set { MUDirection = value ? Direction.Forward : Direction.Reverse; } 
        }

        public Train()
        {
        }

        // restore game state
        public Train(BinaryReader inf)
        {
            RestoreCars( inf );
            SpeedMpS = inf.ReadSingle();
            MUDirection = (Direction)inf.ReadInt32();
            MUThrottlePercent = inf.ReadSingle();
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            aiBrakePercent = inf.ReadSingle();
            RearTDBTraveller = new TDBTraveller( inf );
            CalculatePositionOfCars(0);

        }

        // save game state
        public virtual void Save(BinaryWriter outf)
        {
            SaveCars( outf );
            outf.Write(SpeedMpS);
            outf.Write((int)MUDirection);
            outf.Write(MUThrottlePercent);
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(aiBrakePercent);
            RearTDBTraveller.Save(outf);
        }

        private void SaveCars(BinaryWriter outf)
        {
            outf.Write(Cars.Count);
            foreach (TrainCar car in Cars)
                RollingStock.Save(outf, car); 
        }

        private void RestoreCars(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            for (int i = 0; i < count; ++i)
                Cars.Add( RollingStock.Restore(inf, this));
        }

        /// <summary>
        /// Someone is sending an event notification to all cars on this train.
        /// ie doors open, pantograph up, lights on etc.
        /// </summary>
        public void SignalEvent(EventID eventID)
        {
            foreach (TrainCar car in Cars)
                car.SignalEvent(eventID);
        }


        public void Update( float elapsedClockSeconds )
        {
            PropagateBrakePressure();

            // some extremely simple 'physics'
            float TrainMotiveForceN = 0;     // newtons relative to forward train direction
            float TrainFrictionForceN  = 0;   // newtons always positive
            float TrainMassKG = 0;            // kg 
            foreach (TrainCar car in Cars)
            {
                car.Update(elapsedClockSeconds);
                TrainMotiveForceN += car.MotiveForceN * (car.Flipped ? -1.0f : 1.0f);
                TrainFrictionForceN += car.FrictionForceN;
                TrainMassKG += car.MassKG;
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
                SpeedMpS -= decellerationMpS2 * elapsedClockSeconds;
                if (SpeedMpS < 0)
                    SpeedMpS = 0;
                SpeedMpS += accellerationMpS2 * elapsedClockSeconds;
            }
            else
            {
                SpeedMpS += decellerationMpS2 * elapsedClockSeconds;
                if (SpeedMpS > 0)
                    SpeedMpS = 0;
                SpeedMpS += accellerationMpS2 * elapsedClockSeconds;
            }

            float distanceM = SpeedMpS * elapsedClockSeconds;

            CalculatePositionOfCars( distanceM );

        }

        private void PropagateBrakePressure()
        {
            // TODO , finish this
            foreach (TrainCar car in Cars)
            {
                car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
                car.BrakeSystem.BrakeLine2PressurePSI = BrakeLine2PressurePSI;
                car.BrakeSystem.BrakeLine3PressurePSI = BrakeLine3PressurePSI;
            }
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
                TrainCar car = Cars[i];
                car.SpeedMpS = SpeedMpS * ( car.Flipped ? -1: 1 );

                float bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                // traveller is positioned at the front of the car
                // advance to the first bogie 
                traveller.Move((car.Length - bogieSpacing) / 2.0f);
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

                traveller.Move((car.Length - bogieSpacing) / 2.0f);  // Move to the rear of the car 
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
            RearTDBTraveller.Move(distance);

            TDBTraveller traveller = new TDBTraveller(RearTDBTraveller);
            // The traveller location represents the back of the train.

            // process the cars last to first
            for (int i = Cars.Count - 1; i >= 0; --i)
            {
                TrainCar car = Cars[i];
                car.SpeedMpS = SpeedMpS * (car.Flipped ? -1 : 1 );
                car.DistanceM += Math.Abs(distance);

                float bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                // traveller is positioned at the back of the car
                // advance to the first bogie 
                traveller.Move((car.Length - bogieSpacing) / 2.0f);
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

                traveller.Move((car.Length - bogieSpacing) / 2.0f);  // Move to the front of the car 
            }

            FrontTDBTraveller = traveller;
        } // CalculatePositionOfCars

        //  Sets this train's speed so that momentum is conserved when otherTrain is coupled to it
        public void SetCoupleSpeed(Train otherTrain, float otherMult)
        {
            float kg1 = 0;
            foreach (TrainCar car in Cars)
                kg1+= car.MassKG;
            float kg2= 0;
            foreach (TrainCar car in otherTrain.Cars)
                kg2+= car.MassKG;
            SpeedMpS= (kg1*SpeedMpS+kg2*otherTrain.SpeedMpS*otherMult)/(kg1+kg2);
            otherTrain.SpeedMpS = SpeedMpS;
        }
    }// class Train


}
