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
 *      - brake percent  ( TODO, this should be changed to brake pipe pressure )
 *      
 *  Individual TrainCars provide information on friction and motive force they are generating.
 *  This is consolidated by the train class into overall movement for the train.
 * 
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;


namespace ORTS
{
    public class Train
    {
        // public static Signals Signals;
        public List<TrainCar> Cars = new List<TrainCar>();  // listed front to back
        public TrainCar FirstCar { get { return Cars[0]; } }
        public TrainCar LastCar { get { return Cars[Cars.Count - 1]; } }
        public TDBTraveller RearTDBTraveller;   // positioned at the back of the last car in the train
        public TDBTraveller FrontTDBTraveller; // positioned at the front of the train by CalculatePositionOfCars
        public float SpeedMpS = 0.0f;  // meters per second +ve forward, -ve when backing
        public Train UncoupledFrom = null;  // train not to coupled back onto
        public float TotalCouplerSlackM = 0;
        public float MaximumCouplerForceN = 0;
        public int NPull = 0;
        public int NPush = 0;
        private int LeadLocomotiveIndex = -1;

        // These signals pass through to all cars and locomotives on the train
        public Direction MUDirection = Direction.Forward; //set by player locomotive to control MU'd locomotives
        public float MUThrottlePercent = 0;  // set by player locomotive to control MU'd locomotives
        public float MUReverserPercent = 100;  // steam engine direction/cutoff control for MU'd locomotives
        public float MUDynamicBrakePercent = -1;  // dynamic brake control for MU'd locomotives, <0 for off
        public float BrakeLine1PressurePSI = 90;     // set by player locomotive to control entire train brakes
        public float BrakeLine2PressurePSI = 0;     // extra line for dual line systems
        public float BrakeLine3PressurePSI = 0;     // extra line just in case
        public float BrakeLine4PressurePSI = 0;     // extra line just in case
        public RetainerSetting RetainerSetting = RetainerSetting.Exhaust;
        public int RetainerPercent = 100;

        private Signal nextSignal = new Signal(null, -1);
        public float distanceToSignal = 0.1f;
        public TrackMonitorSignalAspect TMaspect = TrackMonitorSignalAspect.None;

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
            set { MUDirection = value ? Direction.Forward : Direction.Reverse; MUReverserPercent = value ? 100 : -100; } 
        }
        public TrainCar LeadLocomotive
        {
            get { return LeadLocomotiveIndex >= 0 ? Cars[LeadLocomotiveIndex] : null; }
            set
            {
                LeadLocomotiveIndex = -1;
                for (int i = 0; i < Cars.Count; i++)
                    if (value == Cars[i] && value.IsDriveable)
                    {
                        LeadLocomotiveIndex = i;
                        //MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                        //if (lead.EngineBrakeController != null)
                        //    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
                    }
                if (LeadLocomotiveIndex < 0)
                    foreach (TrainCar car in Cars)
                        car.BrakeSystem.BrakeLine1PressurePSI = -1;
            }
        }

        public Train()
        {
        }


        public void InitSignals(Simulator simulator)
        {
            nextSignal = simulator.Signals.FindNearestSignal(FrontTDBTraveller);
            distanceToSignal = nextSignal.DistanceToSignal(FrontTDBTraveller);
        }

        //
        //  This method is invoked whenever the train direction has changed or 'G' key pressed 
        //
        public void ResetSignal(Simulator simulator)
        {
            nextSignal = simulator.Signals.FindNearestSignal(FrontTDBTraveller);
            nextSignal.TrackStateChanged();
            distanceToSignal = nextSignal.DistanceToSignal(FrontTDBTraveller);
        }

        // Sets the Lead locomotive to the next in the consist
        public void LeadNextLocomotive()
        {
            // First driveable
            int firstLead = -1;
            // Next driveale to the current
            int nextLead = -1;
            // Count of driveable locos
            int coud = 0;

            for (int i = 0; i < Cars.Count; i++)
            {
                if (Cars[i].IsDriveable)
                {
                    // Count the driveables
                    coud++;

                    // Get the first driveable
                    if (firstLead == -1)
                        firstLead = i;

                    // If later than current select the next
                    if (LeadLocomotiveIndex < i && nextLead == -1)
                    {
                        nextLead = i;
                    }
                }
            }

            // If found one after the current
            if (nextLead != -1)
                LeadLocomotiveIndex = nextLead;
            // If not, and have more than one, set the first
            else if (coud > 1)
                LeadLocomotiveIndex = firstLead;
        }

        // restore game state
        public Train(BinaryReader inf)
        {
            RestoreCars( inf );
            SpeedMpS = inf.ReadSingle();
            MUDirection = (Direction)inf.ReadInt32();
            MUThrottlePercent = inf.ReadSingle();
            MUDynamicBrakePercent = inf.ReadSingle();
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakeLine4PressurePSI = inf.ReadSingle();
            aiBrakePercent = inf.ReadSingle();
            LeadLocomotiveIndex = inf.ReadInt32();
            RetainerSetting = (RetainerSetting)inf.ReadInt32();
            RetainerPercent = inf.ReadInt32();
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
            outf.Write(MUDynamicBrakePercent);
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakeLine4PressurePSI);
            outf.Write(aiBrakePercent);
            outf.Write(LeadLocomotiveIndex);
            outf.Write((int)RetainerSetting);
            outf.Write(RetainerPercent);
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
                Cars.Add( RollingStock.Restore(inf, this, i == 0 ? null : Cars[i - 1]));
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
            PropagateBrakePressure(elapsedClockSeconds);

            foreach (TrainCar car in Cars)
            {
                car.MotiveForceN = 0;
                car.Update(elapsedClockSeconds);
                //Console.WriteLine("update {0} {1} {2} {3} {4}", car.SpeedMpS, car.MotiveForceN, car.GravityForceN, car.FrictionForceN, car.BrakeSystem.GetStatus());
                car.TotalForceN= car.MotiveForceN + car.GravityForceN;
                if (car.Flipped)
                {
                    car.TotalForceN = -car.TotalForceN;
                    car.SpeedMpS = -car.SpeedMpS;
                }
            }

            AddCouplerImpuseForces();
            ComputeCouplerForces();
            UpdateCarSpeeds(elapsedClockSeconds);
            UpdateCouplerSlack(elapsedClockSeconds);

            float distanceM = LastCar.SpeedMpS * elapsedClockSeconds;

            SpeedMpS = 0;
            foreach (TrainCar car1 in Cars)
            {
                SpeedMpS += car1.SpeedMpS;
                if (car1.Flipped)
                    car1.SpeedMpS = -car1.SpeedMpS;
            }
            SpeedMpS /= Cars.Count;

            CalculatePositionOfCars( distanceM );

            //End-of-route detection
            if (IsEndOfRoute(MUDirection))// FrontTDBTraveller.Direction))
            {
                Stop();

                // TODO - Collision detection: If a train hits an object, there should be a
                //        realistic response.  This includes a train impacting a bumper/buffer.
                //        It's possible that collision detection will occur BEFORE end-of-
                //        route detection and will obsolete this test in this location.
                //        However, the case of an unterminated section should be kept in mind.
            }

            //
            //  Update the distance to and aspect of next signal
            //
            float dist = nextSignal.DistanceToSignal(FrontTDBTraveller);
            if (dist <= 0.0f)
            {
                nextSignal.NextSignal();
                dist = nextSignal.DistanceToSignal(FrontTDBTraveller);
            }
            distanceToSignal = dist;
            TMaspect = nextSignal.GetMonitorAspect();
        } // end Update

        /// <summary>
        /// Returns true if (forward == 1) and front of train on TrEndNode
        /// or if (forward == 0) and rear of train on TrEndNode.
        /// </summary>
        private bool IsEndOfRoute(Direction forward)
        {
            // This test detects that a TrEndNode has been encountered.  This can occur if a 
            // train tries to continue through a bumper (buffer).  It can also occur if it
            // progresses beyond an unterminated section (no bumper).

            // Using FrontTDBTraveller if moving forward or RearTDBTraveller if moving backwards
            TDBTraveller t = (forward == Direction.Forward) ? FrontTDBTraveller : RearTDBTraveller;
            if (t.TN.TrEndNode == null) return false;
            else return true; // Signal end-of-route
        } // end IsEndOfRoute

        /// <summary>
        /// Stops the train ASAP
        /// </summary>
        private void Stop()
        {
            // End of route: Stop ASAP
            SpeedMpS = 0; // Abrupt stop
            //SpeedMpS = -0.85f * SpeedMpS;  // Gives a bounce
            foreach (TrainCar stopping in Cars)
            {
                stopping.SpeedMpS = SpeedMpS;
            }
            AITrainThrottlePercent = 0;
            AITrainBrakePercent = 100;
            // The following does not seem to be essential.  It is commented out because
            // we don't want emergency brake set if we just touch a bumper.  ...WaltN
            //if (LeadLocomotive != null) ((MSTSLocomotive)LeadLocomotive).SetEmergency();
        } // end Stop

        public void InitializeBrakes()
        {
            if (SpeedMpS != 0)
                return;
            float maxPressurePSI = 90;
            if (LeadLocomotiveIndex >= 0)
            {
                MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                if (lead.TrainBrakeController != null)
                {
                    lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, 1000, ref BrakeLine4PressurePSI);
                    maxPressurePSI = lead.TrainBrakeController.GetMaxPressurePSI();
                    BrakeLine1PressurePSI = MathHelper.Max(BrakeLine1PressurePSI, maxPressurePSI - lead.TrainBrakeController.GetFullServReductionPSI());
                }
                if (lead.EngineBrakeController != null)
                    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
            }
            else
            {
                BrakeLine1PressurePSI = BrakeLine3PressurePSI = BrakeLine4PressurePSI = 0;
            }
            BrakeLine2PressurePSI = maxPressurePSI;
            //Console.WriteLine("init {0} {1} {2}", BrakeLine1PressurePSI, BrakeLine2PressurePSI, BrakeLine3PressurePSI);
            foreach (TrainCar car in Cars)
            {
                car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
                car.BrakeSystem.BrakeLine2PressurePSI = BrakeLine2PressurePSI;
                car.BrakeSystem.BrakeLine3PressurePSI = 0;
                car.BrakeSystem.Initialize(LeadLocomotiveIndex < 0, maxPressurePSI);
                if (LeadLocomotiveIndex < 0)
                    car.BrakeSystem.BrakeLine1PressurePSI = -1;
            }
        }
        public void SetHandbrakePercent(float percent)
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            foreach (TrainCar car in Cars)
                car.BrakeSystem.SetHandbrakePercent(percent);
        }
        public void ConnectBrakeHoses()
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            foreach (TrainCar car in Cars)
                car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
        }
        public void DisconnectBrakes()
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            int first = -1;
            int last = -1;
            FindLeadLocomotives(ref first, ref last);
            for (int i = 0; i < Cars.Count; i++)
            {
                if (first <= i && i <= last)
                    continue;
                TrainCar car = Cars[i];
                car.BrakeSystem.BrakeLine1PressurePSI = 0;
                car.BrakeSystem.BrakeLine2PressurePSI = 0;
                car.BrakeSystem.BrakeLine3PressurePSI = 0;
                car.BrakeSystem.Initialize(false, 0);
                car.BrakeSystem.BrakeLine1PressurePSI = -1;
            }
        }
        public void SetRetainers(bool increase)
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            if (!increase)
            {
                RetainerSetting = RetainerSetting.Exhaust;
                RetainerPercent = 100;
            }
            else if (RetainerPercent < 100)
                RetainerPercent*= 2;
            else if ( RetainerSetting != RetainerSetting.SlowDirect)
            {
                RetainerPercent = 25;
                switch (RetainerSetting)
                {
                    case RetainerSetting.Exhaust: RetainerSetting = RetainerSetting.LowPressure; break;
                    case RetainerSetting.LowPressure: RetainerSetting = RetainerSetting.HighPressure; break;
                    case RetainerSetting.HighPressure: RetainerSetting = RetainerSetting.SlowDirect; break;
                }
            }
            int first = -1;
            int last = -1;
            FindLeadLocomotives(ref first, ref last);
            int step = 100 / RetainerPercent;
            for (int i = 0; i < Cars.Count; i++)
            {
                int j = Cars.Count - 1 - i;
                if (j <= last)
                    break;
                Cars[j].BrakeSystem.SetRetainer(i % step == 0 ? RetainerSetting : RetainerSetting.Exhaust);
                //Console.WriteLine("setretainer {0} {1}", j + 1, i % step);
            }
        }
        private void FindLeadLocomotives(ref int first, ref int last)
        {
            first = last = -1;
            if (LeadLocomotiveIndex >= 0)
            {
                for (int i = LeadLocomotiveIndex; i < Cars.Count && Cars[i].IsDriveable; i++)
                    last = i;
                for (int i = LeadLocomotiveIndex; i >= 0 && Cars[i].IsDriveable; i--)
                    first = i;
            }
        }

        private void PropagateBrakePressure(float elapsedClockSeconds)
        {
            if (LeadLocomotiveIndex >= 0)
            {
                MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                if (lead.TrainBrakeController != null)
                    lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, elapsedClockSeconds, ref BrakeLine4PressurePSI);
                if (lead.EngineBrakeController != null)
                    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, elapsedClockSeconds);
                if (lead.BrakePipeChargingRatePSIpS < 1000)
                {
                    float serviceTimeFactor = lead.BrakeServiceTimeFactorS;
                    if (lead.TrainBrakeController != null && lead.TrainBrakeController.GetIsEmergency())
                        serviceTimeFactor = lead.BrakeEmergencyTimeFactorS;
                    int nSteps = (int)(elapsedClockSeconds * 2 / lead.BrakePipeTimeFactorS + 1);
                    float dt = elapsedClockSeconds / nSteps;
                    for (int i = 0; i < nSteps; i++)
                    {
                        if (lead.BrakeSystem.BrakeLine1PressurePSI < BrakeLine1PressurePSI)
                        {
                            float dp = dt * lead.BrakePipeChargingRatePSIpS;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > BrakeLine1PressurePSI)
                                dp = BrakeLine1PressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > lead.MainResPressurePSI)
                                dp = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;
                            if (dp < 0)
                                dp = 0;
                            lead.BrakeSystem.BrakeLine1PressurePSI += dp;
                            lead.MainResPressurePSI -= dp * lead.BrakeSystem.BrakePipeVolumeFT3 / lead.MainResVolumeFT3;
                        }
                        else if (lead.BrakeSystem.BrakeLine1PressurePSI > BrakeLine1PressurePSI)
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 - dt / serviceTimeFactor);
                        TrainCar car0 = Cars[0];
                        float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                        foreach (TrainCar car in Cars)
                        {
                            float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                            if (p0 >= 0 && p1 >= 0)
                            {
                                float dp = dt * (p1 - p0) / lead.BrakePipeTimeFactorS;
                                car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                                car0.BrakeSystem.BrakeLine1PressurePSI += dp;
                            }
                            p0 = p1;
                            car0 = car;
                        }
                    }
                }
                else
                {
                    foreach (TrainCar car in Cars)
                        if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                            car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
                }
                bool twoPipes = lead.BrakeSystem.GetType() == typeof(AirTwinPipe) || lead.BrakeSystem.GetType() == typeof(EPBrakeSystem);
                int first = -1;
                int last = -1;
                FindLeadLocomotives(ref first, ref last);
                float sumpv = 0;
                float sumv = 0;
                for (int i = 0; i < Cars.Count; i++)
                {
                    BrakeSystem brakeSystem = Cars[i].BrakeSystem;
                    if (brakeSystem.BrakeLine1PressurePSI < 0)
                        continue;
                    if (i < first || i > last)
                    {
                        brakeSystem.BrakeLine3PressurePSI = 0;
                        if (twoPipes)
                        {
                            sumv += brakeSystem.BrakePipeVolumeFT3;
                            sumpv += brakeSystem.BrakePipeVolumeFT3 * brakeSystem.BrakeLine2PressurePSI;
                        }
                    }
                    else
                    {
                        float p = brakeSystem.BrakeLine3PressurePSI;
                        if (p > 1000)
                            p -= 1000;
                        AirSinglePipe.ValveState prevState = lead.EngineBrakeState;
                        if (p < BrakeLine3PressurePSI)
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS / (last - first + 1);
                            if (p + dp > BrakeLine3PressurePSI)
                                dp = BrakeLine3PressurePSI - p;
                            p += dp;
                            lead.EngineBrakeState = AirSinglePipe.ValveState.Apply;
                        }
                        else if (p > BrakeLine3PressurePSI)
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS / (last - first + 1);
                            if (p - dp < BrakeLine3PressurePSI)
                                dp = p - BrakeLine3PressurePSI;
                            p -= dp;
                            lead.EngineBrakeState = AirSinglePipe.ValveState.Release;
                        }
                        else
                            lead.EngineBrakeState = AirSinglePipe.ValveState.Lap;
                        if (lead.EngineBrakeState != prevState)
                            switch (lead.EngineBrakeState)
                            {
                                case AirSinglePipe.ValveState.Release: lead.SignalEvent(EventID.EngineBrakeRelease); break;
                                case AirSinglePipe.ValveState.Apply: lead.SignalEvent(EventID.EngineBrakeApply); break;
                            }
                        if (lead.BailOff || (lead.DynamicBrakeAutoBailOff && MUDynamicBrakePercent>0))
                            p += 1000;
                        brakeSystem.BrakeLine3PressurePSI = p;
                        sumv += brakeSystem.BrakePipeVolumeFT3;
                        sumpv += brakeSystem.BrakePipeVolumeFT3 * brakeSystem.BrakeLine2PressurePSI;
                        MSTSLocomotive eng = (MSTSLocomotive)Cars[i];
                        sumv += eng.MainResVolumeFT3;
                        sumpv += eng.MainResVolumeFT3 * eng.MainResPressurePSI;
                    }
                }
                if (sumv > 0)
                    sumpv /= sumv;
                BrakeLine2PressurePSI = sumpv;
                for (int i = 0; i < Cars.Count; i++)
                {
                    if (Cars[i].BrakeSystem.BrakeLine1PressurePSI < 0)
                        continue;
                    if (i < first || i > last)
                    {
                        Cars[i].BrakeSystem.BrakeLine2PressurePSI = twoPipes ? sumpv : 0;
                    }
                    else
                    {
                        Cars[i].BrakeSystem.BrakeLine2PressurePSI = sumpv;
                        MSTSLocomotive eng = (MSTSLocomotive)Cars[i];
                        eng.MainResPressurePSI = sumpv;
                    }
                }
            }
            else
            {
                foreach (TrainCar car in Cars)
                {
                    if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                        continue;
                    car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
                    car.BrakeSystem.BrakeLine2PressurePSI = BrakeLine2PressurePSI;
                    car.BrakeSystem.BrakeLine3PressurePSI = 0;
                }
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

                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, false);
                    if (i < Cars.Count - 1)
                        traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
                    continue;
                }

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

                traveller.Move((car.Length - bogieSpacing) / 2.0f);
                if (i < Cars.Count - 1)
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
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
            TrackNode tn = RearTDBTraveller.TN;
            RearTDBTraveller.Move(distance);
            if (distance < 0 && tn != RearTDBTraveller.TN)
                AlignTrailingPointSwitch(tn, RearTDBTraveller.TN);

            TDBTraveller traveller = new TDBTraveller(RearTDBTraveller);
            // The traveller location represents the back of the train.

            // process the cars last to first
            for (int i = Cars.Count - 1; i >= 0; --i)
            {
                TrainCar car = Cars[i];

                if (i < Cars.Count - 1)
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());

                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, true);
                    continue;
                }

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
                //Console.WriteLine("{0}", car.WorldPosition.XNAMatrix.ToString());

                traveller.Move((car.Length - bogieSpacing) / 2.0f);  // Move to the front of the car 
            }

            if (distance > 0 && traveller.TN != FrontTDBTraveller.TN)
                AlignTrailingPointSwitch(FrontTDBTraveller.TN, traveller.TN);
            FrontTDBTraveller = traveller;
        } // CalculatePositionOfCars

        // aligns a trailing point switch that was just moved over to match the track the train is on
        public void AlignTrailingPointSwitch(TrackNode from, TrackNode to)
        {
            if (from.TrJunctionNode != null)
                return;
            TrackNode sw = null;
            if (to.TrJunctionNode != null)
                sw = to;
            else
            {
                foreach (TrPin fp in from.TrPins)
                {
                    foreach (TrPin tp in to.TrPins)
                    {
                        if (fp.Link == tp.Link)
                        {
                            sw = Program.Simulator.TDB.TrackDB.TrackNodes[fp.Link];
                            break;
                        }
                    }
                    if (sw != null)
                        break;
                }
            }
            if (sw == null || sw.TrJunctionNode == null)
                return;
            for (int i = 1; i < sw.TrPins.Length; i++)
            {
                if (from == Program.Simulator.TDB.TrackDB.TrackNodes[sw.TrPins[i].Link])
                {
                    sw.TrJunctionNode.SelectedRoute = i - 1;
                    return;
                }
            }
        }

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
            foreach (TrainCar car1 in Cars)
                car1.SpeedMpS = car1.Flipped ? -SpeedMpS : SpeedMpS;
            foreach (TrainCar car2 in otherTrain.Cars)
                car2.SpeedMpS = car2.Flipped ? -SpeedMpS : SpeedMpS;
        }

        // setups of the left hand side of the coupler force solving equations
        void SetupCouplerForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car= Cars[i];
                car.CouplerForceB = 1 / car.MassKG;
                car.CouplerForceA = -car.CouplerForceB;
                car.CouplerForceC = -1 / Cars[i + 1].MassKG;
                car.CouplerForceB -= car.CouplerForceC;
            }
        }

        // solves coupler force equations
        void SolveCouplerForceEquations()
        {
            float b = Cars[0].CouplerForceB;
            Cars[0].CouplerForceU = Cars[0].CouplerForceR / b;
            for (int i = 1; i < Cars.Count - 1; i++)
            {
                Cars[i].CouplerForceG = Cars[i - 1].CouplerForceC / b;
                b = Cars[i].CouplerForceB - Cars[i].CouplerForceA * Cars[i].CouplerForceG;
                Cars[i].CouplerForceU = (Cars[i].CouplerForceR - Cars[i].CouplerForceA * Cars[i - 1].CouplerForceU) / b;
            }
            for (int i = Cars.Count - 3; i >= 0; i--)
                Cars[i].CouplerForceU -= Cars[i + 1].CouplerForceG * Cars[i + 1].CouplerForceU;
        }

        // removes equations if forces don't match faces in contact
        // returns true if a change is made
        bool FixCouplerForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
                    continue;
                float maxs1 = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM < maxs1 || car.CouplerForceU > 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
                    continue;
                float maxs1 = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM > -maxs1 || car.CouplerForceU < 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            return false;
        }

        // changes the coupler force equation for car to make the corresponding force equal to forceN
        void SetCouplerForce(TrainCar car, float forceN)
        {
            car.CouplerForceA = car.CouplerForceC = 0;
            car.CouplerForceB = 1;
            car.CouplerForceR = forceN;
            //Console.WriteLine("setf {0} {1}", forceN, car.CouplerForceU);
        }

        // removes equations if forces don't match faces in contact
        // returns true if a change is made
        bool FixCouplerImpulseForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
                    continue;
                if (car.CouplerSlackM < car.CouplerSlack2M || car.CouplerForceU > 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
                    continue;
                if (car.CouplerSlackM > -car.CouplerSlack2M || car.CouplerForceU < 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            return false;
        }

        // computes and applies coupler impulse forces which force speeds to match when no relative movement is possible
        void AddCouplerImpuseForces()
        {
            if (Cars.Count < 2)
                return;
            SetupCouplerForceEquations();
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                float max = car.CouplerSlack2M;
                if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
                {
                    car.CouplerForceB = 1;
                    car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
                }
                else
                    car.CouplerForceR = Cars[i + 1].SpeedMpS - car.SpeedMpS;
            }
            do
                SolveCouplerForceEquations();
            while (FixCouplerImpulseForceEquations());
            MaximumCouplerForceN = 0;
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                Cars[i].SpeedMpS += Cars[i].CouplerForceU / Cars[i].MassKG;
                Cars[i + 1].SpeedMpS -= Cars[i].CouplerForceU / Cars[i + 1].MassKG;
                //if (Cars[i].CouplerForceU != 0)
                //    Console.WriteLine("impulse {0} {1} {2} {3} {4}", i, Cars[i].CouplerForceU, Cars[i].CouplerSlackM, Cars[i].SpeedMpS, Cars[i+1].SpeedMpS);
                //if (MaximumCouplerForceN < Math.Abs(Cars[i].CouplerForceU))
                //    MaximumCouplerForceN = Math.Abs(Cars[i].CouplerForceU);
            }
        }

        // computes coupler acceleration balancing forces
        void ComputeCouplerForces()
        {
            for (int i = 0; i < Cars.Count; i++)
                if (Cars[i].SpeedMpS > 0)
                    Cars[i].TotalForceN -= Cars[i].FrictionForceN;
                else if (Cars[i].SpeedMpS < 0)
                    Cars[i].TotalForceN += Cars[i].FrictionForceN;
            if (Cars.Count < 2)
                return;
            SetupCouplerForceEquations();
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                float max = car.GetMaximumCouplerSlack1M();
                if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
                {
                    car.CouplerForceB = 1;
                    car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
                }
                else
                    car.CouplerForceR = Cars[i + 1].TotalForceN / Cars[i + 1].MassKG - car.TotalForceN / car.MassKG;
            }
            do
                SolveCouplerForceEquations();
            while (FixCouplerForceEquations());
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                //Console.WriteLine("cforce {0} {1} {2}", i, car.CouplerForceU, car.SpeedMpS);
                car.TotalForceN += car.CouplerForceU;
                Cars[i + 1].TotalForceN -= car.CouplerForceU;
                if (MaximumCouplerForceN < Math.Abs(car.CouplerForceU))
                    MaximumCouplerForceN = Math.Abs(car.CouplerForceU);
                float maxs = car.GetMaximumCouplerSlack2M();
                if (car.CouplerForceU > 0)
                {
                    float f = -(car.CouplerSlackM + car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
                    if (car.CouplerSlackM > -maxs && f > car.CouplerForceU)
                        car.CouplerSlack2M = -car.CouplerSlackM;
                    else
                        car.CouplerSlack2M = maxs;
                }
                else if (car.CouplerForceU == 0)
                    car.CouplerSlack2M = maxs;
                else
                {
                    float f = (car.CouplerSlackM - car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
                    if (car.CouplerSlackM < maxs && f > car.CouplerForceU)
                        car.CouplerSlack2M = car.CouplerSlackM;
                    else
                        car.CouplerSlack2M = maxs;
                }
                //Console.WriteLine("{0} {1} {2}", car.CouplerSlackM, car.CouplerSlack2M, car.CouplerForceU);
            }
        }
        void UpdateCarSpeeds(float elapsedTime)
        {
            int n = 0;
            foreach (TrainCar car in Cars)
            {
                //Console.WriteLine("updatespeed {0} {1} {2} {3}", car.SpeedMpS, car.TotalForceN, car.MassKG, car.FrictionForceN);
                if (car.SpeedMpS > 0)
                {
                    car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
                    if (car.SpeedMpS < 0)
                        car.SpeedMpS = 0;
                }
                else if (car.SpeedMpS < 0)
                {
                    car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
                    if (car.SpeedMpS > 0)
                        car.SpeedMpS = 0;
                }
                else
                    n++;
            }
            if (n == 0)
                return;
            // start cars moving forward
            for (int i = 0; i < Cars.Count; i++)
            {
                TrainCar car = Cars[i];
                if (car.SpeedMpS != 0 || car.TotalForceN <= car.FrictionForceN)
                    continue;
                int j = i;
                float f = 0;
                float m = 0;
                for (; ; )
                {
                    f += car.TotalForceN - car.FrictionForceN;
                    m += car.MassKG;
                    if (j == Cars.Count - 1 || car.CouplerSlackM < car.GetMaximumCouplerSlack2M())
                        break;
                    j++;
                    car = Cars[j];
                }
                if (f > 0)
                {
                    for (int k = i; k <= j; k++)
                        Cars[k].SpeedMpS = f / m * elapsedTime;
                    n -= j - i + 1;
                }
            }
            if (n == 0)
                return;
            // start cars moving backward
            for (int i = Cars.Count - 1; i >= 0 ; i--)
            {
                TrainCar car = Cars[i];
                if (car.SpeedMpS != 0 || car.TotalForceN > -car.FrictionForceN)
                    continue;
                int j = i;
                float f = 0;
                float m = 0;
                for (; ; )
                {
                    f += car.TotalForceN + car.FrictionForceN;
                    m += car.MassKG;
                    if (j == 0 || car.CouplerSlackM > -car.GetMaximumCouplerSlack2M())
                        break;
                    j--;
                    car = Cars[j];
                }
                if (f < 0)
                {
                    for (int k = j; k <= i; k++)
                        Cars[k].SpeedMpS = f / m * elapsedTime;
                }
            }
        }
        void UpdateCouplerSlack(float elapsedTime)
        {
            TotalCouplerSlackM = 0;
            NPull = NPush = 0;
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                car.CouplerSlackM += (car.SpeedMpS - Cars[i + 1].SpeedMpS) * elapsedTime;
                float max = car.GetMaximumCouplerSlack2M();
                if (car.CouplerSlackM < -max)
                    car.CouplerSlackM = -max;
                else if (car.CouplerSlackM > max)
                    car.CouplerSlackM = max;
                TotalCouplerSlackM += car.CouplerSlackM;
                max = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM >= max)
                    NPull++;
                else if (car.CouplerSlackM <= -max)
                    NPush++;
                //Console.WriteLine("slack {0} {1} {2}", i, car.CouplerSlackM, car.CouplerSlack2M);
            }
            foreach (TrainCar car in Cars)
                car.DistanceM += Math.Abs(car.SpeedMpS * elapsedTime);
        }
    }// class Train


}
