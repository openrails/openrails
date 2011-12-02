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
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MSTS;

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
        float IdleExhaust = 10.0f;
        float MaxExhaust = 50.0f;
        float ExhaustDynamics = 1.5f;
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

		public MSTSDieselLocomotive(Simulator simulator, string wagFile, TrainCar previousCar)
            : base(simulator, wagFile, previousCar)
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
                case "engine(or_diesel(idleexhaust": IdleExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(or_diesel(maxexhaust": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(or_diesel(exhaustdynamics": ExhaustDynamics = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(or_diesel(exhaustcolor": ExhaustSteadyColor.PackedValue = stf.ReadHexBlock(Color.Gray.PackedValue); break;
                case "engine(or_diesel(exhausttransientcolor": ExhaustTransientColor.PackedValue = stf.ReadHexBlock(Color.Black.PackedValue); break;
                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.Diesel, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.Diesel, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Diesel, null); break;
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




            // Diesel locos have step controllers ; here to go around parse prblms
            HasStepCtrl = true;
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
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            // for example
            //if (inf.ReadBoolean()) SignalEvent(EventID.PantographUp);
            base.Restore(inf);
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
            //base.Update(elapsedClockSeconds );

            TrainBrakeController.Update(elapsedClockSeconds);
            if (EngineBrakeController != null)
                EngineBrakeController.Update(elapsedClockSeconds);

            if ((DynamicBrakeController != null) && (DynamicBrakePercent >= 0))
            {
                if (this.IsLeadLocomotive())
                    DynamicBrakePercent = DynamicBrakeController.Update(elapsedClockSeconds) * 100.0f;
                else
                    DynamicBrakeController.Update(elapsedClockSeconds);
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


            // TODO  this is a wild simplification for diesel electric
            float t = ThrottlePercent / 100f;
            float currentSpeedMpS = Math.Abs(SpeedMpS);

            ExhaustParticles = ((MaxExhaust - IdleExhaust) * t + IdleExhaust);
            if (ExhaustParticles < 5.0f)
                ExhaustParticles = 5.0f;

            if (EngineRPMderivation > 0.0f)
            {
                ExhaustParticles *= ExhaustDynamics * MaxExhaust;
                ExhaustColor = ExhaustTransientColor;
            }
            else
            {
                ExhaustColor = ExhaustSteadyColor;
                if (EngineRPMderivation < 0.0f)
                    ExhaustParticles = 3.0f;
            }
            if (PowerOn)
            {
                if (TractiveForceCurves == null)
                {
                    float maxForceN = MaxForceN * t;
                    float maxPowerW = MaxPowerW * (EngineRPM - IdleRPM) / (MaxRPM - IdleRPM);
                    if (maxForceN * WheelSpeedMpS > maxPowerW)
                        maxForceN = maxPowerW / WheelSpeedMpS;
                    if (currentSpeedMpS > MaxSpeedMpS)
                        maxForceN = 0;
                    MotiveForceN = maxForceN;
                }
                else
                {
                    MotiveForceN = TractiveForceCurves.Get(t, WheelSpeedMpS);
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
            }
            else
            {
                int carCount = 0;
                int controlEngine = -1;

                // When not LeadLocomotive; check if lead is in Neutral
                // if so this loco will have no motive force
                var LeadLocomotive = Simulator.Trains[0];

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

            //Force is filtered due to inductance
            FilteredMotiveForceN = CurrentFilter.Filter(MotiveForceN, elapsedClockSeconds);

            MotiveForceN = FilteredMotiveForceN;

            LimitMotiveForce(elapsedClockSeconds);

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

            if (MainResPressurePSI < CompressorRestartPressurePSI && !CompressorOn)
                SignalEvent(EventID.CompressorOn);
            else if (MainResPressurePSI > MaxMainResPressurePSI && CompressorOn)
                SignalEvent(EventID.CompressorOff);
            if (CompressorOn)
                MainResPressurePSI += elapsedClockSeconds * MainResChargingRatePSIpS;

            base.UpdateParent(elapsedClockSeconds);
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(EventID eventID)
        {
            switch (eventID)
            {
                // for example
                // case EventID.BellOn: Bell = true; break;
                // case EveantID.BellOff: Bell = false; break;
                default: break;
            }
            base.SignalEvent(eventID);
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
        Integrator revolutionsInt;

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

        public MSTSDieselLocomotiveViewer(Viewer3D viewer, MSTSDieselLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.
            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";

            foreach (KeyValuePair<string, List<ParticleEmitterDrawer>> pair in ParticleDrawers)
            {
                if (pair.Key == "Exhaust1")
                {
                    foreach (ParticleEmitterDrawer drawer in pair.Value)
                    {
                        drawer.SetTexture(SharedTextureManager.Get(viewer.RenderProcess.GraphicsDevice, dieselTexture));
                        drawer.SetEmissionRate(car.ExhaustParticles);
                    }
                }
            }
        }

        
        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {

            base.HandleUserInput(elapsedTime);
        }


        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (KeyValuePair<string, List<ParticleEmitterDrawer>> pair in ParticleDrawers)
            {
                if (pair.Key == "Exhaust1")
                {
                    foreach (ParticleEmitterDrawer drawer in pair.Value)
                    {
                        drawer.SetEmissionRate(((MSTSDieselLocomotive)this.Car).ExhaustParticles);
                        drawer.SetEmissionColor(((MSTSDieselLocomotive)this.Car).ExhaustColor);
                    }
                }
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
