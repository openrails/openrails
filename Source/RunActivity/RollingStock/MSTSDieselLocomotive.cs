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

/* DIESEL LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer.  The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
// needed for Debug

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a diesel locomotive
    /// </summary>
    public class MSTSDieselLocomotive : MSTSLocomotive
    {
        float IdleRPM = 0;
        float MaxRPM = 0;
        float MaxRPMChangeRate = 0;
        float PercentChangePerSec = .2f;
        public float IdleExhaust = 0.0f;
        public float InitialExhaust = 0.0f;
        float ExhaustMagnitude = 4.0f;
        float MaxExhaust = 50.0f;
        public float ExhaustDynamics = 4.0f;
        float EngineRPMderivation = 0.0f;
        float EngineRPMold = 0.0f;

        public float MaxDieselLevelL = 5000.0f;
        float DieselUsedPerHourAtMaxPowerL = 1.0f;
        float DieselUsedPerHourAtIdleL = 1.0f;
        public float DieselLevelL = 5000.0f;
        float DieselFlowLps = 0.0f;
        float DieselWeightKgpL = 0.8f; //per liter
        float InitialMassKg = 100000.0f;

        public float EngineRPM = 0.0f;
        public float ExhaustParticles = 10.0f;
        public Color ExhaustColor = Color.Gray;
        Color ExhaustSteadyColor = Color.Gray;
        Color ExhaustTransientColor = Color.Black;
        Color ExhaustDecelColor = Color.TransparentWhite;

        public MSTSDieselLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerOn = true;
            InitialMassKg = MassKG;
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(dieselengineidlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpmchangerate": MaxRPMChangeRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                case "engine(effects(dieselspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(dieselsmokeeffectinitialsmokerate": IdleExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectinitialmagnitude": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxsmokerate": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxmagnitude": ExhaustMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(or_diesel(exhaustcolor": ExhaustSteadyColor.PackedValue = stf.ReadHexBlock(Color.Gray.PackedValue); break;
                case "engine(or_diesel(exhausttransientcolor": ExhaustTransientColor.PackedValue = stf.ReadHexBlock(Color.Black.PackedValue); break;
                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.LiquidVolume, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.LiquidVolume, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.LiquidVolume, null); break;
                // for example
                //case "engine(sound": CabSoundFileName = stf.ReadStringBlock(); break;
                //case "engine(cabview": CVFFileName = stf.ReadStringBlock(); break;
                default: base.Parse(lowercasetoken, stf); break;
            }

            if (IdleRPM != 0 && MaxRPM != 0 && MaxRPMChangeRate != 0)
            {
                PercentChangePerSec = MaxRPMChangeRate / (MaxRPM - IdleRPM);
                EngineRPM = IdleRPM;
            }

            if (MaxDieselLevelL != DieselLevelL)
                DieselLevelL = MaxDieselLevelL;
        }


        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void InitializeFromCopy(MSTSWagon copy)
        {
            MSTSDieselLocomotive locoCopy = (MSTSDieselLocomotive)copy;
            IdleRPM = locoCopy.IdleRPM;
            MaxRPM = locoCopy.MaxRPM;
            MaxRPMChangeRate = locoCopy.MaxRPMChangeRate;
            PercentChangePerSec = locoCopy.PercentChangePerSec;
            IdleExhaust = locoCopy.IdleExhaust;
            MaxExhaust = locoCopy.MaxExhaust;
            ExhaustDynamics = locoCopy.ExhaustDynamics;
            EngineRPMderivation = locoCopy.EngineRPMderivation;
            EngineRPMold = locoCopy.EngineRPMold;

            MaxDieselLevelL = locoCopy.MaxDieselLevelL;
            DieselUsedPerHourAtMaxPowerL = locoCopy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = locoCopy.DieselUsedPerHourAtIdleL;
            if (this.CarID.StartsWith("0"))
                DieselLevelL = locoCopy.DieselLevelL;
            else
                DieselLevelL = locoCopy.MaxDieselLevelL;
            DieselFlowLps = 0.0f;
            InitialMassKg = MassKG;

            EngineRPM = locoCopy.EngineRPM;
            ExhaustParticles = locoCopy.ExhaustParticles;
            ExhaustSteadyColor = locoCopy.ExhaustSteadyColor;
            ExhaustTransientColor = locoCopy.ExhaustTransientColor;
            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            // for example
            // outf.Write(Pan);
            base.Save(outf);
            outf.Write(DieselLevelL);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            DieselLevelL = inf.ReadSingle();
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            return new MSTSDieselLocomotiveViewer(viewer, this);
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            TrainBrakeController.Update(elapsedClockSeconds);
            if( TrainBrakeController.UpdateValue > 0.0 ) {
                Simulator.Confirmer.Update( CabControl.TrainBrake, CabSetting.Increase, GetTrainBrakeStatus() );
            }
            if( TrainBrakeController.UpdateValue < 0.0 ) {
                Simulator.Confirmer.Update( CabControl.TrainBrake, CabSetting.Decrease, GetTrainBrakeStatus() );
            }

            if( EngineBrakeController != null ) {
                EngineBrakeController.Update( elapsedClockSeconds );
                if( EngineBrakeController.UpdateValue > 0.0 ) {
                    Simulator.Confirmer.Update( CabControl.EngineBrake, CabSetting.Increase, GetEngineBrakeStatus() );
                }
                if( EngineBrakeController.UpdateValue < 0.0 ) {
                    Simulator.Confirmer.Update( CabControl.EngineBrake, CabSetting.Decrease, GetEngineBrakeStatus() );
                }
            }

            if ((DynamicBrakeController != null) && (DynamicBrakePercent >= 0))
            {
                if (!DynamicBrake)
                {
                    if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                    {
                        DynamicBrake = true; // Engage
                        if (IsLeadLocomotive())
                            Simulator.Confirmer.ConfirmWithPerCent(CabControl.DynamicBrake, DynamicBrakeController.CurrentValue * 100);
                    }
                    else if (IsLeadLocomotive())
                        Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
                }
                else if (this.IsLeadLocomotive())
                    DynamicBrakePercent = DynamicBrakeController.Update(elapsedClockSeconds) * 100.0f;
                else
                    DynamicBrakeController.Update(elapsedClockSeconds);
            }
            else if ((DynamicBrakeController != null) && (DynamicBrakePercent < 0) && (DynamicBrake))
            {
                if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                {
                    DynamicBrake = false; // Disengage
                    if (IsLeadLocomotive())
                        Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.Off);
                }
                else if (IsLeadLocomotive())
                    Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
            }

            //Currently the ThrottlePercent is global to the entire train
            //So only the lead locomotive updates it, the others only updates the controller (actually useless)
            if (this.IsLeadLocomotive())
            {
                ThrottlePercent = ThrottleController.Update(elapsedClockSeconds) * 100.0f;
            }
            else
            {
                ThrottleController.Update(elapsedClockSeconds);
            }

#if INDIVIDUAL_CONTROL
			//this train is remote controlled, with mine as a helper, so I need to send the controlling information, but not the force.
			if (MultiPlayer.MPManager.IsMultiPlayer() && this.Train.TrainType == Train.TRAINTYPE.REMOTE && this == Program.Simulator.PlayerLocomotive)
			{
				//cannot control train brake as it is the remote's job to do so
				if ((EngineBrakeController != null && EngineBrakeController.UpdateValue != 0.0) || (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0) || ThrottleController.UpdateValue != 0.0)
				{
					controlUpdated = true;
				}
				ThrottlePercent = ThrottleController.Update(elapsedClockSeconds) * 100.0f;
				return; //done, will go back and send the message to the remote train controller
			}

			if (MultiPlayer.MPManager.IsMultiPlayer() && this.notificationReceived == true)
			{
				ThrottlePercent = ThrottleController.CurrentValue * 100.0f;
				this.notificationReceived = false;
			}
#endif
			
			// TODO  this is a wild simplification for diesel electric
            float e = (EngineRPM - IdleRPM) / (MaxRPM - IdleRPM); //
            float t = ThrottlePercent / 100f;
            float currentSpeedMpS = Math.Abs(SpeedMpS);
            float currentWheelSpeedMpS = Math.Abs(WheelSpeedMpS);

            //Initial smoke, when locomotive is started:

            ExhaustColor = ExhaustSteadyColor;
            
            if (EngineRPM == IdleRPM)
            {
                ExhaustParticles = IdleExhaust;
                ExhaustDynamics = InitialExhaust;
                ExhaustColor = ExhaustSteadyColor;
            }
            else if (EngineRPMderivation > 0.0f)
            {
                ExhaustParticles = IdleExhaust + (e * MaxExhaust);
                ExhaustDynamics = InitialExhaust + (e * ExhaustMagnitude);
                ExhaustColor = ExhaustTransientColor;               
            }
            else if (EngineRPMderivation < 0.0f)
            {
                    ExhaustParticles = IdleExhaust + (e * MaxExhaust);
                    ExhaustDynamics = InitialExhaust + (e * ExhaustMagnitude);
                if (t == 0f)
                {
                    ExhaustColor = ExhaustDecelColor;
                }
                else
                {
                    ExhaustColor = ExhaustSteadyColor;
                }
            }
            if (PowerOn)
            {
                if (TractiveForceCurves == null)
                {
                    float maxForceN = MaxForceN * t;
                    float maxPowerW = MaxPowerW * t * t;
                    if (!this.Simulator.UseAdvancedAdhesion)
                        currentWheelSpeedMpS = currentSpeedMpS;
                    if (maxForceN * currentWheelSpeedMpS > maxPowerW)
                        maxForceN = maxPowerW / currentWheelSpeedMpS;
                    //if (currentSpeedMpS > MaxSpeedMpS)
                    //    maxForceN = 0;
                    if (currentSpeedMpS > MaxSpeedMpS -0.05f)
                        maxForceN = 20 * (MaxSpeedMpS - currentSpeedMpS) * maxForceN;
                    if (currentSpeedMpS > (MaxSpeedMpS))
                        maxForceN = 0;
                    MotiveForceN = maxForceN;
                }
                else
                {
                    MotiveForceN = TractiveForceCurves.Get(t, currentWheelSpeedMpS);
                    if (MotiveForceN < 0)
                        MotiveForceN = 0;
                }
                if (t == 0)
                    DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                else
                    DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * t + DieselUsedPerHourAtIdleL) / 3600.0f;

                DieselLevelL -= DieselFlowLps * elapsedClockSeconds;
                if (DieselLevelL <= 0.0f)
                    PowerOn = false;
                MassKG = InitialMassKg - MaxDieselLevelL * DieselWeightKgpL + DieselLevelL * DieselWeightKgpL;
            }


            if (MaxForceN > 0 && MaxContinuousForceN > 0)
            {
                MotiveForceN *= 1 - (MaxForceN - MaxContinuousForceN) / (MaxForceN * MaxContinuousForceN) * AverageForceN;
                float w = (ContinuousForceTimeFactor - elapsedClockSeconds) / ContinuousForceTimeFactor;
                if (w < 0)
                    w = 0;
                AverageForceN = w * AverageForceN + (1 - w) * MotiveForceN;
            }

#if !NEW_SIGNALLING
            if (this.IsLeadLocomotive())
            {
                switch (Direction)
                {
                    case Direction.Forward:
                        //MotiveForceN *= 1;     //Not necessary
                        break;
                    case Direction.Reverse:
                        MotiveForceN *= -1;
                        break;
                    case Direction.N:
                    default:
                        MotiveForceN *= 0;
                        break;
                }
                ConfirmWheelslip();
            }
            else
            {
                int carCount = 0;
                int controlEngine = -1;

                // When not LeadLocomotive; check if lead is in Neutral
                // if so this loco will have no motive force
				var LeadLocomotive = Simulator.PlayerLocomotive.Train;

                foreach (TrainCar car in LeadLocomotive.Cars)
                {
                    if (car.IsDriveable)
                        if (controlEngine == -1)
                        {
                            controlEngine = carCount;
                            if (car.Direction == Direction.N)
                                MotiveForceN *= 0;
                            else
                            {
                                switch (Direction)
                                {
                                    case Direction.Forward:
                                        MotiveForceN *= 1;     //Not necessary
                                        break;
                                    case Direction.Reverse:
                                        MotiveForceN *= -1;
                                        break;
                                    case Direction.N:
                                    default:
                                        MotiveForceN *= 0;
                                        break;
                                }
                            }
                        }
                    break;
                } // foreach
            } // end when not lead loco
#else

            if (Train.TrainType == Train.TRAINTYPE.PLAYER)
            {
                if (this.IsLeadLocomotive())
                {
                    switch (Direction)
                    {
                        case Direction.Forward:
                            //MotiveForceN *= 1;     //Not necessary
                            break;
                        case Direction.Reverse:
                            MotiveForceN *= -1;
                            break;
                        case Direction.N:
                        default:
                            MotiveForceN *= 0;
                            break;
                    }
                    ConfirmWheelslip();
                }
                else
                {
                    // When not LeadLocomotive; check if lead is in Neutral
                    // if so this loco will have no motive force

                    var LeadLocomotive = Simulator.PlayerLocomotive;

                    if (LeadLocomotive == null) { }
                    else if (LeadLocomotive.Direction == Direction.N)
                        MotiveForceN *= 0;
                    else
                    {
                        switch (Direction)
                        {
                            case Direction.Forward:
                                MotiveForceN *= 1;     //Not necessary
                                break;
                            case Direction.Reverse:
                                MotiveForceN *= -1;
                                break;
                            case Direction.N:
                            default:
                                MotiveForceN *= 0;
                                break;
                        }
                    }
                } // end when not lead loco
            }// end player locomotive

            else // for AI locomotives
            {
                switch (Direction)
                {
                    case Direction.Reverse:
                        MotiveForceN *= -1;
                        break;
                    default:
                        break;
                }
            }// end AI locomotive
#endif

            // Variable1 is wheel rotation in m/sec for steam locomotives
            //Variable2 = Math.Abs(MotiveForceN) / MaxForceN;   // force generated

            if (PowerOn)
                Variable1 = ThrottlePercent / 100f;   // throttle setting
            else
            {
                Variable1 = -IdleRPM/(MaxRPM - IdleRPM);
                ExhaustParticles = 0;

            }
            //Variable2 = Math.Abs(WheelSpeedMpS);

            if (DynamicBrakePercent > 0 && DynamicBrakeForceCurves != null)
            {
                float f = DynamicBrakeForceCurves.Get(.01f * DynamicBrakePercent, currentSpeedMpS);
                if (f > 0)
                    MotiveForceN -= (SpeedMpS > 0 ? 1 : -1) * f;
            }
            Variable3 = 0;
            if ( DynamicBrakePercent > 0)
                Variable3 -= MotiveForceN / MaxDynamicBrakeForceN;
            switch (this.Train.TrainType)
            {
                case Train.TRAINTYPE.AI:
                    if (!PowerOn)
                        PowerOn = true;
                    //LimitMotiveForce(elapsedClockSeconds);    //calls the advanced physics
                    LimitMotiveForce();                         //let's call the basic physics instead for now
                    WheelSpeedMpS = Flipped ? -currentSpeedMpS : currentSpeedMpS;            //make the wheels go round
                    break;
                case Train.TRAINTYPE.STATIC:
                    break;
                case Train.TRAINTYPE.PLAYER:
                case Train.TRAINTYPE.REMOTE:
                    // For notched throttle controls (e.g. Dash 9 found on Marias Pass) UpdateValue is always 0.0
                    if (ThrottleController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.Throttle,
                            ThrottleController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            ThrottleController.CurrentValue * 100);
                    }
                    if (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.DynamicBrake,
                            DynamicBrakeController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            DynamicBrakeController.CurrentValue * 100);
                    }

                    //Force is filtered due to inductance
                    FilteredMotiveForceN = CurrentFilter.Filter(MotiveForceN, elapsedClockSeconds);

                    MotiveForceN = FilteredMotiveForceN;

                    LimitMotiveForce(elapsedClockSeconds);

                    if (WheelslipCausesThrottleDown && WheelSlip)
                        ThrottleController.SetValue(0.0f);
                    break;
                default:
                    break;

            }

            

            // Refined Variable2 setting to graduate
            if (Variable2 != Variable1)
            {
                // Calculated value
                float addition = PercentChangePerSec;
                bool neg = false;

                if (Variable1 < Variable2)
                {
                    addition *= -1;
                    neg = true;
                }

                addition *= elapsedClockSeconds;

                Variable2 += addition;

                if ((neg && Variable2 < Variable1) || (!neg && Variable2 > Variable1))
                    Variable2 = Variable1;
            }

            EngineRPM = Variable2 * (MaxRPM - IdleRPM) + IdleRPM;

            if (elapsedClockSeconds > 0.0f)
            {
                EngineRPMderivation = (EngineRPM - EngineRPMold)/elapsedClockSeconds;
                EngineRPMold = EngineRPM;
            }

            if ((MainResPressurePSI < CompressorRestartPressurePSI) && (!CompressorOn) && (PowerOn))
                SignalEvent(Event.CompressorOn);
            else if (MainResPressurePSI > MaxMainResPressurePSI && CompressorOn)
                SignalEvent(Event.CompressorOff);
            if ((CompressorOn)&&(PowerOn))
                MainResPressurePSI += elapsedClockSeconds * MainResChargingRatePSIpS;

            base.UpdateParent(elapsedClockSeconds); // Calls the Update() method in the parent class MSTSLocomotive which calls Update() on its parent MSTSWagon which calls ...
        }

        public override string GetStatus()
        {
            var result = new StringBuilder();
            result.AppendFormat("Diesel engine = {0}\n", PowerOn ? "On" : "Off");
            result.AppendFormat("Diesel RPM = {0:F0}\n", EngineRPM);
            result.AppendFormat("Diesel level = {0:F0} L ({1:F0} gal)\n", DieselLevelL, DieselLevelL / 3.785f);
            result.AppendFormat("Diesel flow = {0:F1} L/h ({1:F1} gal/h)", DieselFlowLps * 3600.0f, DieselFlowLps * 3600.0f / 3.785f);
            return result.ToString();
        }

        /// <summary>
        /// Catch the signal to start or stop the diesel
        /// </summary>
        public void StartStopDiesel()
        {
            if (!this.IsLeadLocomotive()&&(this.ThrottlePercent == 0))
                PowerOn = !PowerOn;
        }

    } // class DieselLocomotive

    public class DieselEngine
    {
        public float RPM = 0;
        public float DieselEngineMaxRPM = 1000;
        public float DieselEngineIdleRPM = 200;
        public float DieselEngineMaxRPMChangeRateRPMps = 100.0f;
        public float DieselEngineMaxRPMChangeRateRPMpss = 10.0f;

        public float TemperatureC = 0;
        public float OptTemperatureC = 90;
        public float MaxTemperatureC = 120;
        public float TempTimeConstant = 1;

        public float PressurePSI = 0.0f;
        public float MaxOilPressurePSI = 150.0f;
        
        public float MaxDieselLevelL = 1000;
        public float DieselUsedPerHourAtMaxPower = 10;
        public float DieselUsedPerHourAtIdle = 1;
        public float DieselUsedInTransient = 1.5f;
        public float vyhrevnostkWhpKg = 11.61f;
        Interpolator DieselConsumption;

        public float PowerW { get; set; }
        public float MaxPowerW = 1000000;
        public float DemPowerPer = 0;
        //public float OutPowerW = 0;
        public float CoolingPowerW { get; set; }

        Integrator temperatureInt;

        public Color SmokeColor;
        public float ExhaustParticles;

        public DieselEngine()
        {
            temperatureInt = new Integrator();
            DieselConsumption = new Interpolator(2);
            DieselConsumption[DieselEngineIdleRPM] = DieselUsedPerHourAtIdle;
            DieselConsumption[DieselEngineMaxRPM] = DieselUsedPerHourAtMaxPower;
        }

        public void Update(float elapsedClockSeconds)
        {
            if(TemperatureC > (MaxTemperatureC - 10))
                CoolingPowerW = PowerW;
            if(TemperatureC < (MaxTemperatureC - 20))
                CoolingPowerW = 0;

            TemperatureC = temperatureInt.Integrate(elapsedClockSeconds, (PowerW - CoolingPowerW) / TempTimeConstant);

        }



    }


    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special Diesel loco animation to the basic LocomotiveViewer class
    /// </summary>
    class MSTSDieselLocomotiveViewer : MSTSLocomotiveViewer
    {
        MSTSDieselLocomotive DieselLocomotive { get { return (MSTSDieselLocomotive)Car; } }
        List<ParticleEmitterDrawer> Exhaust = new List<ParticleEmitterDrawer>();

        public MSTSDieselLocomotiveViewer(Viewer3D viewer, MSTSDieselLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.
            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";

            foreach (var drawers in from drawer in ParticleDrawers
                                    where drawer.Key.ToLowerInvariant().StartsWith("exhaust")
                                    select drawer.Value)
            {
                Exhaust.AddRange(drawers);
            }
            foreach (var drawer in Exhaust)
            {
                drawer.SetTexture(viewer.TextureManager.Get(dieselTexture));
                drawer.SetEmissionRate(car.ExhaustParticles);
                drawer.SetParticleDuration(car.ExhaustDynamics);
            }
        }

        
        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if( UserInput.IsPressed( UserCommands.ControlDieselPlayer ) ) {
                if( DieselLocomotive.ThrottlePercent < 1 ) {
                    DieselLocomotive.PowerOn = !DieselLocomotive.PowerOn;
                    Viewer.Simulator.Confirmer.Confirm( CabControl.PlayerDiesel, DieselLocomotive.PowerOn ? CabSetting.On : CabSetting.Off );
                } else {
                    Viewer.Simulator.Confirmer.Warning( CabControl.PlayerDiesel, CabSetting.Warn1 );
                }
            }
            if (UserInput.IsPressed(UserCommands.ControlDieselHelper))
            {
                bool powerOn = false;
                int helperLocos = 0;
                foreach( TrainCar traincar in DieselLocomotive.Train.Cars )
                {
                    if( traincar.GetType() == typeof( MSTSDieselLocomotive ) ) {
                        ((MSTSDieselLocomotive)traincar).StartStopDiesel();
                        powerOn = ((MSTSDieselLocomotive)traincar).PowerOn;
                        helperLocos++;
                    }
                }
                // One confirmation however many helper locomotives
                // <CJComment> Couldn't make one confirmation per loco work correctly :-( </CJComment>
                if( helperLocos > 0 ) {
                    Viewer.Simulator.Confirmer.Confirm( CabControl.HelperDiesel, powerOn ? CabSetting.On : CabSetting.Off );
                }
            }
            base.HandleUserInput(elapsedTime);
        }


        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (var drawer in Exhaust)
            {
                drawer.SetEmissionRate(((MSTSDieselLocomotive)this.Car).ExhaustParticles);
                drawer.SetEmissionColor(((MSTSDieselLocomotive)this.Car).ExhaustColor);
                drawer.SetParticleDuration(((MSTSDieselLocomotive)this.Car).ExhaustDynamics);
            }
            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }

    } // class MSTSDieselLocomotiveViewer

}
