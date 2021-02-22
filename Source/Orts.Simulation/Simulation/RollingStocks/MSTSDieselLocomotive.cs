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

//#define ALLOW_ORTS_SPECIFIC_ENG_PARAMETERS


using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using System.Diagnostics;
using System;
using System.IO;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a diesel locomotive
    /// </summary>
    public class MSTSDieselLocomotive : MSTSLocomotive
    {
        public float IdleRPM;
        public float MaxRPM;
        public float MaxRPMChangeRate;
        public float PercentChangePerSec = .2f;
        public float InitialExhaust;
        public float InitialMagnitude;
        public float MaxExhaust = 2.8f;
        public float MaxMagnitude = 1.5f;
        public float EngineRPMderivation;
        float EngineRPMold;
        float EngineRPMRatio; // used to compute Variable1 and Variable2
        public float MaximumDieselEnginePowerW;

        public MSTSNotchController FuelController = new MSTSNotchController(0, 1, 0.0025f);
        public float MaxDieselLevelL = 5000.0f;
        public float DieselLevelL
        {
            get { return FuelController.CurrentValue * MaxDieselLevelL; }
            set { FuelController.CurrentValue = value / MaxDieselLevelL; }
        }

        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        public float DieselUsedPerHourAtIdleL = 1.0f;
        public float DieselFlowLps;
        public float DieselWeightKgpL = 0.8508f; //per liter
        float InitialMassKg = 100000.0f;

        public float LocomotiveMaxRailOutputPowerW;

        public float EngineRPM;
        public SmoothedData ExhaustParticles = new SmoothedData(1);
        public SmoothedData ExhaustMagnitude = new SmoothedData(1);
        public SmoothedData ExhaustColorR = new SmoothedData(1);
        public SmoothedData ExhaustColorG = new SmoothedData(1);
        public SmoothedData ExhaustColorB = new SmoothedData(1);

        public float DieselOilPressurePSI = 0f;
        public float DieselMinOilPressurePSI;
        public float DieselMaxOilPressurePSI;
        public float DieselTemperatureDeg = 40f;
        public float DieselMaxTemperatureDeg;
        public DieselEngine.Cooling DieselEngineCooling = DieselEngine.Cooling.Proportional;

        public DieselEngines DieselEngines;

        public GearBox GearBox = new GearBox(); // this is the same instance present in the first engine of the locomotive; instead instances in other engines, if any, are copies

        /// <summary>
        /// Used to accumulate a quantity that is not lost because of lack of precision when added to the Fuel level
        /// </summary>        
        float partialFuelConsumption = 0;

        private const float GearBoxControllerBoost = 1; // Slow boost to enable easy single gear up/down commands

        public MSTSDieselLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerOn = true;
            RefillImmediately();
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
                case "engine(ortsdieselenginemaxpower": MaximumDieselEnginePowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, null); break;
                case "engine(effects(dieselspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(dieselsmokeeffectinitialsmokerate": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectinitialmagnitude": InitialMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxsmokerate": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxmagnitude": MaxMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsdieselengines": DieselEngines = new DieselEngines(this, stf); break;
                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 120f); break;
                case "engine(ortsminoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 40f); break;
                case "engine(maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 0); break;
                case "engine(ortsdieselcooling": DieselEngineCooling = (DieselEngine.Cooling)stf.ReadInt((int)DieselEngine.Cooling.Proportional); break;
                default:
                    GearBox.Parse(lowercasetoken, stf);
                    base.Parse(lowercasetoken, stf); break;
            }

            if (IdleRPM != 0 && MaxRPM != 0 && MaxRPMChangeRate != 0)
            {
                PercentChangePerSec = MaxRPMChangeRate / (MaxRPM - IdleRPM);
                EngineRPM = IdleRPM;
            }
        }

        public override void LoadFromWagFile(string wagFilePath)
        {
            base.LoadFromWagFile(wagFilePath);

            if (Simulator.Settings.VerboseConfigurationMessages)  // Display locomotivve name for verbose error messaging
            {
                Trace.TraceInformation("\n\n ================================================= {0} =================================================", LocomotiveName);
            }

            NormalizeParams();

            // Check to see if Speed of Max Tractive Force has been set - use ORTS value as first priority, if not use MSTS, last resort use an arbitary value.
            if (SpeedOfMaxContinuousForceMpS == 0)
            {
                if (MSTSSpeedOfMaxContinuousForceMpS != 0)
                {
                    SpeedOfMaxContinuousForceMpS = MSTSSpeedOfMaxContinuousForceMpS; // Use MSTS value if present

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Speed Of Max Continuous Force: set to default value {0}", FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));

                }
                else if (MaxPowerW != 0 && MaxContinuousForceN != 0)
                {
                    SpeedOfMaxContinuousForceMpS = MaxPowerW / MaxContinuousForceN;

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Speed Of Max Continuous Force: set to 'calculated' value {0}", FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));

                }
                else
                {
                    SpeedOfMaxContinuousForceMpS = 10.0f; // If not defined then set at an "arbitary" value of 22mph

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Speed Of Max Continuous Force: set to 'arbitary' value {0}", FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));

                }
            }

            if (DieselEngines == null)
                DieselEngines = new DieselEngines(this);

            // Create a diesel engine block if none exits, typically for a MSTS or BASIC configuration
            if (DieselEngines.Count == 0)
            {
                DieselEngines.Add(new DieselEngine());

                DieselEngines[0].InitFromMSTS(this);
                DieselEngines[0].Initialize(true);
            }


            // Check initialization of power values for diesel engines
            for (int i = 0; i < DieselEngines.Count; i++)
            {
                DieselEngines[i].InitDieselRailPowers(this);

            }

            if (GearBox != null && GearBox.IsInitialized)
            {
                GearBox.CopyFromMSTSParams(DieselEngines[0]);
                if (DieselEngines[0].GearBox == null)
                {
                    DieselEngines[0].GearBox = GearBox;
                    DieselEngines[0].GearBox.UseLocoGearBox(DieselEngines[0]);
                }
                for (int i = 1; i < DieselEngines.Count; i++)
                {
                    if (DieselEngines[i].GearBox == null)
                        DieselEngines[i].GearBox = new GearBox(GearBox, DieselEngines[i]);
                }

                if (GearBoxController == null)
                {
                    GearBoxController = new MSTSNotchController(GearBox.NumOfGears + 1);
                }
            }

            InitialMassKg = MassKG;

            // If traction force curves not set (BASIC configuration) then check that power values are set, otherwise locomotive will not move.
            if (TractiveForceCurves == null && LocomotiveMaxRailOutputPowerW == 0)
            {
                if (MaxPowerW != 0)
                {

                    LocomotiveMaxRailOutputPowerW = MaxPowerW;  // Set to default power value

                    if (Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("MaxRailOutputPower (BASIC Config): set to default value = {0}", FormatStrings.FormatPower(LocomotiveMaxRailOutputPowerW, IsMetric, false, false));
                    }
                }
                else
                {
                    LocomotiveMaxRailOutputPowerW = 2500000.0f; // If no default value then set to arbitary value

                    if (Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("MaxRailOutputPower (BASIC Config): set at arbitary value = {0}", FormatStrings.FormatPower(LocomotiveMaxRailOutputPowerW, IsMetric, false, false));
                    }

                }

                
                if (MaximumDieselEnginePowerW == 0)
                {
                    MaximumDieselEnginePowerW = LocomotiveMaxRailOutputPowerW;  // If no value set in ENG file, then set the Prime Mover power to same as RailOutputPower (typically the MaxPower value)

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Diesel Engine Prime Mover Power set the same as MaxRailOutputPower {0} value", FormatStrings.FormatPower(MaximumDieselEnginePowerW, IsMetric, false, false));

                }

            }

            // Check that maximum force value has been set
            if (MaxForceN == 0)
            {

                if (TractiveForceCurves == null)  // Basic configuration - ie no force and Power tables, etc
                {
                    float StartingSpeedMpS = 0.1f; // Assumed starting speed for diesel - can't be zero otherwise error will occurr
                    MaxForceN = LocomotiveMaxRailOutputPowerW / StartingSpeedMpS;

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Force set to {0} value, calculated from Rail Power Value.", FormatStrings.FormatForce(MaxForceN, IsMetric));
                }
                else
                {
                    float ThrottleSetting = 1.0f; // Must be at full throttle for these calculations
                    float StartingSpeedMpS = 0.1f; // Assumed starting speed for diesel - can't be zero otherwise error will occurr
                    float MaxForceN = TractiveForceCurves.Get(ThrottleSetting, StartingSpeedMpS);

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Force set to {0} value, calcuated from Tractive Force Tables", FormatStrings.FormatForce(MaxForceN, IsMetric));
                }


            }


            // Check force assumptions set for diesel
            if (Simulator.Settings.VerboseConfigurationMessages)
            {

                float ThrottleSetting = 1.0f; // Must be at full throttle for these calculations
                if (TractiveForceCurves == null)  // Basic configuration - ie no force and Power tables, etc
                {
                    float CalculatedMaxContinuousForceN = ThrottleSetting * LocomotiveMaxRailOutputPowerW / SpeedOfMaxContinuousForceMpS;
                    Trace.TraceInformation("Diesel Force Settings (BASIC Config): Max Starting Force {0}, Calculated Max Continuous Force {1} @ speed of {2}", FormatStrings.FormatForce(MaxForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    Trace.TraceInformation("Diesel Power Settings (BASIC Config): Prime Mover {0}, Max Rail Output Power {1}", FormatStrings.FormatPower(MaximumDieselEnginePowerW, IsMetric, false, false), FormatStrings.FormatPower(LocomotiveMaxRailOutputPowerW, IsMetric, false, false));

                    if (MaxForceN < MaxContinuousForceN)
                    {
                        Trace.TraceInformation("!!!! Warning: Starting Tractive force {0} is less then Calculated Continuous force {1}, please check !!!!", FormatStrings.FormatForce(MaxForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    }

                }
                else // Advanced configuration - 
                {
                    float StartingSpeedMpS = 0.1f; // Assumed starting speed for diesel - can't be zero otherwise error will occurr
                    float StartingForceN = TractiveForceCurves.Get(ThrottleSetting, StartingSpeedMpS);
                    float CalculatedMaxContinuousForceN = TractiveForceCurves.Get(ThrottleSetting, SpeedOfMaxContinuousForceMpS);
                    Trace.TraceInformation("Diesel Force Settings (ADVANCED Config): Max Starting Force {0} Calculated Max Continuous Force {1}, @ speed of {2}", FormatStrings.FormatForce(StartingForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    Trace.TraceInformation("Diesel Power Settings (ADVANCED Config): Prime Mover {0}, Max Rail Output Power {1} @ {2} rpm", FormatStrings.FormatPower(DieselEngines.MaxPowerW, IsMetric, false, false), FormatStrings.FormatPower(DieselEngines.MaximumRailOutputPowerW, IsMetric, false, false), MaxRPM);

                    if (StartingForceN < MaxContinuousForceN)
                    {
                        Trace.TraceInformation("!!!! Warning: Calculated Starting Tractive force {0} is less then Calculated Continuous force {1}, please check !!!!", FormatStrings.FormatForce(StartingForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    }
                }

                // Check that MaxPower value is realistic - Calculate power - metric - P = F x V
                float CalculatedContinuousPowerW = MaxContinuousForceN * SpeedOfMaxContinuousForceMpS;
                if (MaxPowerW < CalculatedContinuousPowerW)
                {
                    Trace.TraceInformation("!!!! Warning: MaxPower {0} is less then continuous force calculated power {1} @ speed of {2}, please check !!!!", FormatStrings.FormatPower(MaxPowerW, IsMetric, false, false), FormatStrings.FormatPower(CalculatedContinuousPowerW, IsMetric, false, false), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                }

                Trace.TraceInformation("===================================================================================================================\n\n");
            }

        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a locomotive already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            MSTSDieselLocomotive locoCopy = (MSTSDieselLocomotive)copy;
            EngineRPM = locoCopy.EngineRPM;
            IdleRPM = locoCopy.IdleRPM;
            MaxRPM = locoCopy.MaxRPM;
            MaxRPMChangeRate = locoCopy.MaxRPMChangeRate;
            MaximumDieselEnginePowerW = locoCopy.MaximumDieselEnginePowerW;
            PercentChangePerSec = locoCopy.PercentChangePerSec;
            LocomotiveMaxRailOutputPowerW = locoCopy.LocomotiveMaxRailOutputPowerW;

            EngineRPMderivation = locoCopy.EngineRPMderivation;
            EngineRPMold = locoCopy.EngineRPMold;

            MaxDieselLevelL = locoCopy.MaxDieselLevelL;
            DieselUsedPerHourAtMaxPowerL = locoCopy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = locoCopy.DieselUsedPerHourAtIdleL;

            DieselFlowLps = 0.0f;
            InitialMassKg = MassKG;

            if (this.CarID.StartsWith("0"))
                DieselLevelL = locoCopy.DieselLevelL;
            else
                DieselLevelL = locoCopy.MaxDieselLevelL;

            if (locoCopy.GearBoxController != null)
                GearBoxController = new MSTSNotchController(locoCopy.GearBoxController);

            DieselEngines = new DieselEngines(locoCopy.DieselEngines, this);
            if (DieselEngines[0].GearBox != null) GearBox = DieselEngines[0].GearBox;
            for (int i = 1; i < DieselEngines.Count; i++)
            {
                if (DieselEngines[i].GearBox == null && locoCopy.DieselEngines[i].GearBox != null)
                    DieselEngines[i].GearBox = new GearBox(GearBox, DieselEngines[i]);
            }
            foreach (DieselEngine de in DieselEngines)
            {
                de.Initialize(true);
            }
        }

        public override void Initialize()
        {
            if (GearBox != null && !GearBox.IsInitialized)
            {
                GearBox = null;
            }

            DieselEngines.Initialize(false);

            base.Initialize();

            // If DrvWheelWeight is not in ENG file, then calculate drivewheel weight freom FoA

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                DrvWheelWeightKg = MassKG; // set Drive wheel weight to total wagon mass if not in ENG file
                InitialDrvWheelWeightKg = MassKG; // // set Initial Drive wheel weight as well, as it is used as a reference
            }

            // Initialise water level in steam heat boiler
            if (CurrentLocomotiveSteamHeatBoilerWaterCapacityL == 0 && IsSteamHeatFitted)
            {
                if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = MaximumSteamHeatBoilerWaterTankCapacityL;
                }
                else
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = L.FromGUK(800.0f);
                }
            }
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
            outf.Write(CurrentLocomotiveSteamHeatBoilerWaterCapacityL);
            DieselEngines.Save(outf);
            ControllerFactory.Save(GearBoxController, outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            DieselLevelL = inf.ReadSingle();
            CurrentLocomotiveSteamHeatBoilerWaterCapacityL = inf.ReadSingle();
            DieselEngines.Restore(inf);
            ControllerFactory.Restore(GearBoxController, inf);
            
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions  when initial speed > 0 
        /// 

        public override void InitializeMoving()
        {
            base.InitializeMoving();
            WheelSpeedMpS = SpeedMpS;
            DynamicBrakePercent = -1;
            if (DieselEngines[0].GearBox != null && GearBoxController != null)
            {
                DieselEngines[0].GearBox.InitializeMoving();
                DieselEngines[0].InitializeMoving();
                if (IsLeadLocomotive())
                {
                    Train.MUGearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                    Train.AITrainGearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                }
                GearBoxController.CurrentNotch = Train.MUGearboxGearIndex;
                GearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                GearBoxController.SetValue((float)GearBoxController.CurrentNotch);
            }
            ThrottleController.SetValue(Train.MUThrottlePercent / 100);
        }


        /// <summary>
        /// This function updates periodically the states and physical variables of the locomotive's subsystems.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            // The following is not in the UpdateControllers function due to the fact that fuel level has to be calculated after the motive force calculation.
            FuelController.Update(elapsedClockSeconds);
            if (FuelController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.DieselFuel, CabSetting.Increase, FuelController.CurrentValue * 100);

            // Update water controller for steam boiler heating tank
            if (this.IsLeadLocomotive() && IsSteamHeatFitted)
            {
                WaterController.Update(elapsedClockSeconds);
                if (WaterController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeatBoilerWater, CabSetting.Increase, WaterController.CurrentValue * 100);
            }

        }


        /// <summary>
        /// This function updates periodically the states and physical variables of the locomotive's power supply.
        /// </summary>
        protected override void UpdatePowerSupply(float elapsedClockSeconds)
        {
            DieselEngines.Update(elapsedClockSeconds);

            ExhaustParticles.Update(elapsedClockSeconds, DieselEngines[0].ExhaustParticles);
            ExhaustMagnitude.Update(elapsedClockSeconds, DieselEngines[0].ExhaustMagnitude);
            ExhaustColorR.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.R);
            ExhaustColorG.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.G);
            ExhaustColorB.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.B);

            PowerOn = DieselEngines.PowerOn;
            AuxPowerOn = DieselEngines.PowerOn;
        }

        /// <summary>
        /// This function updates periodically the states and physical variables of the locomotive's controllers.
        /// </summary>
        protected override void UpdateControllers(float elapsedClockSeconds)
        {
            base.UpdateControllers(elapsedClockSeconds);

            //Currently the ThrottlePercent is global to the entire train
            //So only the lead locomotive updates it, the others only updates the controller (actually useless)
            if (this.IsLeadLocomotive() || (!AcceptMUSignals))
            {
                if (GearBoxController != null)
                {
                    GearboxGearIndex = (int)GearBoxController.UpdateAndSetBoost(elapsedClockSeconds, GearBoxControllerBoost);
                }
            }
            else
            {
                if (GearBoxController != null)
                {
                    GearBoxController.UpdateAndSetBoost(elapsedClockSeconds, GearBoxControllerBoost);
                }
            }
        }

        /// <summary>
        /// This function updates periodically the locomotive's motive force.
        /// </summary>
        protected override void UpdateMotiveForce(float elapsedClockSeconds, float t, float AbsSpeedMpS, float AbsWheelSpeedMpS)
        {
            // This section calculates the motive force of the locomotive as follows:
            // Basic configuration (no TF table) - uses P = F /speed  relationship - requires power and force parameters to be set in the ENG file. 
            // Advanced configuration (TF table) - use a user defined tractive force table
            // With Simple adhesion apart from correction for rail adhesion, there is no further variation to the motive force. 
            // With Advanced adhesion the raw motive force is fed into the advanced (axle) adhesion model, and is corrected for wheel slip and rail adhesion
            if (PowerOn)
            {
                // Appartent throttle setting is a reverse lookup of the throttletab vs rpm, hence motive force increase will be related to increase in rpm. The minimum of the two values
                // is checked to enable fast reduction in tractive force when decreasing the throttle. Typically it will take longer for the prime mover to decrease rpm then drop motive force.
                float LocomotiveApparentThrottleSetting = 0;

                if (IsPlayerTrain)
                {
                    LocomotiveApparentThrottleSetting = Math.Min(t, DieselEngines.ApparentThrottleSetting / 100.0f);
                }
                else // For AI trains, just use the throttle setting
                {
                    LocomotiveApparentThrottleSetting = t;
                }

                LocomotiveApparentThrottleSetting = MathHelper.Clamp(LocomotiveApparentThrottleSetting, 0.0f, 1.0f);  // Clamp decay within bounds

                // If there is more then one diesel engine, and one or more engines is stopped, then the Fraction Power will give a fraction less then 1 depending upon power definitions of engines.
                float DieselEngineFractionPower = 1.0f;

                if (DieselEngines.Count > 1)
                {
                    DieselEngineFractionPower = DieselEngines.RunningPowerFraction;
                }

                DieselEngineFractionPower = MathHelper.Clamp(DieselEngineFractionPower, 0.0f, 1.0f);  // Clamp decay within bounds

                if (TractiveForceCurves == null)
                {
                    // This sets the maximum force of the locomotive, it will be adjusted down if it exceeds the max power of the locomotive.
                    float maxForceN = Math.Min(t * MaxForceN * (1 - PowerReduction), AbsSpeedMpS == 0.0f ? (t * MaxForceN * (1 - PowerReduction)) : (t * LocomotiveMaxRailOutputPowerW / AbsSpeedMpS));

                    // Maximum rail power is reduced by apparent throttle factor and the number of engines running (power ratio)
                    float maxPowerW = LocomotiveMaxRailOutputPowerW * DieselEngineFractionPower * LocomotiveApparentThrottleSetting;

                    // If unloading speed is in ENG file, and locomotive speed is greater then unloading speed, and less then max speed, then apply a decay factor to the power/force
                    if (UnloadingSpeedMpS != 0 && AbsSpeedMpS > UnloadingSpeedMpS && AbsSpeedMpS < MaxSpeedMpS)
                    {
                        // use straight line curve to decay power to zero by 2 x unloading speed
                        float unloadingspeeddecay = 1.0f - (1.0f / UnloadingSpeedMpS) * (AbsSpeedMpS - UnloadingSpeedMpS);
                        unloadingspeeddecay = MathHelper.Clamp(unloadingspeeddecay, 0.0f, 1.0f);  // Clamp decay within bounds
                        maxPowerW *= unloadingspeeddecay;
                    }

                    if (DieselEngines.HasGearBox)
                    {
                        MotiveForceN = DieselEngines.MotiveForceN;
                    }
                    else
                    {
                        if (maxForceN * AbsSpeedMpS > maxPowerW)
                            maxForceN = maxPowerW / AbsSpeedMpS;

                        MotiveForceN = maxForceN;
                        // Motive force will be produced until power reaches zero, some locomotives had a overspeed monitor set at the maximum design speed
                    }
                }
                else
                {
                    // Tractive force is read from Table using the apparent throttle setting, and then reduced by the number of engines running (power ratio)
                    MotiveForceN = TractiveForceCurves.Get(LocomotiveApparentThrottleSetting, AbsSpeedMpS) * DieselEngineFractionPower * (1 - PowerReduction);
                    if (MotiveForceN < 0 && !TractiveForceCurves.AcceptsNegativeValues())
                        MotiveForceN = 0;
                }

                DieselFlowLps = DieselEngines.DieselFlowLps;
                partialFuelConsumption += DieselEngines.DieselFlowLps * elapsedClockSeconds;
                if (partialFuelConsumption >= 0.1)
                {
                    DieselLevelL -= partialFuelConsumption;
                    partialFuelConsumption = 0;
                }
                if (DieselLevelL <= 0.0f)
                {
                    PowerOn = false;
                    SignalEvent(Event.EnginePowerOff);
                    foreach (DieselEngine de in DieselEngines)
                    {
                        if (de.EngineStatus != DieselEngine.Status.Stopping || de.EngineStatus != DieselEngine.Status.Stopped)
                            de.Stop();
                    }
                }
            }

            if (MaxForceN > 0 && MaxContinuousForceN > 0 && PowerReduction < 1)
            {
                MotiveForceN *= 1 - (MaxForceN - MaxContinuousForceN) / (MaxForceN * MaxContinuousForceN) * AverageForceN * (1 - PowerReduction);
                float w = (ContinuousForceTimeFactor - elapsedClockSeconds) / ContinuousForceTimeFactor;
                if (w < 0)
                    w = 0;
                AverageForceN = w * AverageForceN + (1 - w) * MotiveForceN;
            }
        }

        /// <summary>
        /// This function updates periodically the locomotive's sound variables.
        /// </summary>
        protected override void UpdateSoundVariables(float elapsedClockSeconds)
        {
            EngineRPMRatio = (DieselEngines[0].RealRPM - DieselEngines[0].IdleRPM) / (DieselEngines[0].MaxRPM - DieselEngines[0].IdleRPM);

            Variable1 = ThrottlePercent / 100.0f;
            // else Variable1 = MotiveForceN / MaxForceN; // Gearbased, Variable1 proportional to motive force
            // allows for motor volume proportional to effort.

            // Refined Variable2 setting to graduate
            if (Variable2 != EngineRPMRatio)
            {
                // We must avoid Variable2 to run outside of [0, 1] range, even temporarily (because of multithreading)
                Variable2 = EngineRPMRatio < Variable2 ?
                    Math.Max(Math.Max(Variable2 - elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 0) :
                    Math.Min(Math.Min(Variable2 + elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 1);
            }

            EngineRPM = Variable2 * (MaxRPM - IdleRPM) + IdleRPM;

            if (DynamicBrakePercent > 0)
            {
                if (MaxDynamicBrakeForceN == 0)
                    Variable3 = DynamicBrakePercent / 100f;
                else
                    Variable3 = DynamicBrakeForceN / MaxDynamicBrakeForceN;
            }
            else
                Variable3 = 0;

            if (elapsedClockSeconds > 0.0f)
            {
                EngineRPMderivation = (EngineRPM - EngineRPMold) / elapsedClockSeconds;
                EngineRPMold = EngineRPM;
            }
        }

        public override void ChangeGearUp()
        {
            if (DieselEngines[0].GearBox != null)
            {
                if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Semiautomatic)
                {
                    DieselEngines[0].GearBox.AutoGearUp();
                    GearBoxController.SetValue((float)DieselEngines[0].GearBox.NextGearIndex);
                }
            }
        }

        public override void ChangeGearDown()
        {

            if (DieselEngines[0].GearBox != null)
            {
                if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Semiautomatic)
                {
                    DieselEngines[0].GearBox.AutoGearDown();
                    GearBoxController.SetValue((float)DieselEngines[0].GearBox.NextGearIndex);
                }
            }
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.GEARS:
                    if (DieselEngines.HasGearBox)
                        data = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                    break;
                case CABViewControlTypes.FUEL_GAUGE:
                    if (cvc.Units == CABViewControlUnits.GALLONS)
                        data = L.ToGUS(DieselLevelL);
                    else
                        data = DieselLevelL;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }

            return data;
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Engine"),
                Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(DieselEngines[0].EngineStatus)));

            if (DieselEngines.HasGearBox)
                status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Gear"), DieselEngines[0].GearBox.CurrentGearIndex < 0
                    ? Simulator.Catalog.GetParticularString("Gear", "N")
                    : (DieselEngines[0].GearBox.CurrentGearIndex + 1).ToString());

            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());

            if (DieselEngines.HasGearBox)
                status.AppendFormat("\t{0} {1}", Simulator.Catalog.GetString("Gear"), DieselEngines[0].GearBox.CurrentGearIndex);
            status.AppendFormat("\t{0} {1}\t\t{2}\n", 
                Simulator.Catalog.GetString("Fuel"), 
                FormatStrings.FormatFuelVolume(DieselLevelL, IsMetric, IsUK), DieselEngines.GetStatus());

            if (IsSteamHeatFitted && Train.PassengerCarsNumber > 0 && this.IsLeadLocomotive() && Train.CarSteamHeatOn)
            {
                // Only show steam heating HUD if fitted to locomotive and the train, has passenger cars attached, and is the lead locomotive
                // Display Steam Heat info
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}/{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18:N0}\n",
                   Simulator.Catalog.GetString("StHeat:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(CurrentSteamHeatPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("StTemp"),
                   FormatStrings.FormatTemperature(C.FromF(SteamHeatPressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]), IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
                   Simulator.Catalog.GetString("WaterLvl"),
                   FormatStrings.FormatFuelVolume(CurrentLocomotiveSteamHeatBoilerWaterCapacityL, IsMetric, IsUK),
                   Simulator.Catalog.GetString("Last:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(Train.LastCar.CarSteamHeatMainPipeSteamPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("Temp"),
                   FormatStrings.FormatTemperature(Train.LastCar.CarCurrentCarriageHeatTempC, IsMetric, false),
                   Simulator.Catalog.GetString("OutTemp"),
                   FormatStrings.FormatTemperature(Train.TrainOutsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("NetHt"),
                   Train.LastCar.DisplayTrainNetSteamHeatLossWpTime);
            }


            return status.ToString();
        }

        /// <summary>
        /// Catch the signal to start or stop the diesel
        /// </summary>
        public void StartStopDiesel()
        {
            if (!this.IsLeadLocomotive() && (this.ThrottlePercent == 0))
                PowerOn = !PowerOn;
        }

        public override void SetPower(bool ToState)
        {
            if (ToState)
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Start();
                SignalEvent(Event.EnginePowerOn);
            }
            else
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Stop();
                SignalEvent(Event.EnginePowerOff);
            }

            base.SetPower(ToState);
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            MSTSNotchController controller = null;
            if (type == (uint)PickupType.FuelDiesel) return FuelController;
            if (type == (uint)PickupType.FuelWater) return WaterController;
            return controller;
        }

        /// <summary>
        /// Sets step size for the fuel controller basing on pickup feed rate and engine fuel capacity
        /// </summary>
        /// <param name="type">Pickup</param>

        public override void SetStepSize(PickupObj matchPickup)
        {
            if (MaxDieselLevelL != 0)
                FuelController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / (MaxDieselLevelL * DieselWeightKgpL));
            if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                WaterController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / MaximumSteamHeatBoilerWaterTankCapacityL);
        }

        /// <summary>
        /// Sets coal and water supplies to full immediately.
        /// Provided in case route lacks pickup points for diesel oil.
        /// </summary>
        public override void RefillImmediately()
        {
            FuelController.CurrentValue = 1.0f;
            WaterController.CurrentValue = 1.0f;
        }

        /// <summary>
        /// Returns the fraction of diesel oil already in tank.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            if (pickupType == (uint)PickupType.FuelDiesel)
            {
                return FuelController.CurrentValue;
            }
            if (pickupType == (uint)PickupType.FuelWater)
            {
                return WaterController.CurrentValue;
            }
            return 0f;
        }

        /// <summary>
        /// Restores the type of gearbox, that was forced to
        /// automatic for AI trains
        /// </summary>
        public override void SwitchToPlayerControl()
        {
            foreach (DieselEngine de in DieselEngines)
            {
                if (de.GearBox != null)
                    de.GearBox.GearBoxOperation = de.GearBox.OriginalGearBoxOperation;
            }
            if (DieselEngines[0].GearBox != null && GearBoxController != null)
            {
                GearBoxController.CurrentNotch = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                GearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                GearBoxController.SetValue((float)GearBoxController.CurrentNotch);
            }

        }

        public override void SwitchToAutopilotControl()
        {
            SetDirection(Direction.Forward);
            foreach (DieselEngine de in DieselEngines)
            {
                if (de.EngineStatus != DieselEngine.Status.Running)
                    de.Initialize(true);
                if (de.GearBox != null)
                    de.GearBox.GearBoxOperation = GearBoxOperation.Automatic;
            }
            base.SwitchToAutopilotControl();
        }

        protected override void UpdateCarSteamHeat(float elapsedClockSeconds)
        {
            // Update Steam Heating System

            // TO DO - Add test to see if cars are coupled, if Light Engine, disable steam heating.

            if (IsSteamHeatFitted && this.IsLeadLocomotive())  // Only Update steam heating if train and locomotive fitted with steam heating
            {

                CurrentSteamHeatPressurePSI = SteamHeatController.CurrentValue * MaxSteamHeatPressurePSI;

                // Calculate steam boiler usage values
                // Don't turn steam heat on until pressure valve has been opened, water and fuel capacity also needs to be present, and steam boiler is not locked out
                if (CurrentSteamHeatPressurePSI > 0.1 && CurrentLocomotiveSteamHeatBoilerWaterCapacityL > 0 && DieselLevelL > 0 && !IsSteamHeatBoilerLockedOut)      
                {
                    // Set values for visible exhaust based upon setting of steam controller
                    HeatingSteamBoilerVolumeM3pS = 1.5f * SteamHeatController.CurrentValue;
                    HeatingSteamBoilerDurationS = 1.0f * SteamHeatController.CurrentValue;
                    Train.CarSteamHeatOn = true; // turn on steam effects on wagons

                    // Calculate fuel usage for steam heat boiler
                    float FuelUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerFuelUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    DieselLevelL -= FuelUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as fuel used.

                    // Calculate water usage for steam heat boiler
                    float WaterUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerWaterUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL -= WaterUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as water used.
                }
                else
                {
                    Train.CarSteamHeatOn = false; // turn on steam effects on wagons
                }
                

            }
        }

        public void TogglePlayerEngine()
        {
            if (ThrottlePercent < 1)
            {
                //                    PowerOn = !PowerOn;
                if (DieselEngines[0].EngineStatus == DieselEngine.Status.Stopped)
                {
                    DieselEngines[0].Start();
                    SignalEvent(Event.EnginePowerOn); // power on sound hook
                }
                if (DieselEngines[0].EngineStatus == DieselEngine.Status.Running)
                {
                    DieselEngines[0].Stop();
                    SignalEvent(Event.EnginePowerOff); // power off sound hook
                }
                Simulator.Confirmer.Confirm(CabControl.PlayerDiesel, DieselEngines.PowerOn ? CabSetting.On : CabSetting.Off);
            }
            else
            {
                Simulator.Confirmer.Warning(CabControl.PlayerDiesel, CabSetting.Warn1);
            }
        }

        //used by remote diesels to update their exhaust
        public void RemoteUpdate(float exhPart, float exhMag, float exhColorR, float exhColorG, float exhColorB)
        {
            ExhaustParticles.ForceSmoothValue(exhPart);
            ExhaustMagnitude.ForceSmoothValue(exhMag);
            ExhaustColorR.ForceSmoothValue(exhColorR);
            ExhaustColorG.ForceSmoothValue(exhColorG);
            ExhaustColorB.ForceSmoothValue(exhColorB);
        }


        //================================================================================================//
        /// <summary>
        /// The method copes with the strange parameters that some british gear-based DMUs have: throttle 
        /// values arrive up to 1000%, and conversely GearBoxMaxTractiveForceForGears are divided by 10.
        /// Apparently MSTS works well with such values. This method recognizes such case and corrects such values.
        /// </summary>
        protected void NormalizeParams()
        {
            // check for wrong GearBoxMaxTractiveForceForGears parameters
            if (GearBox != null && GearBox.mstsParams != null && GearBox.mstsParams.GearBoxMaxTractiveForceForGearsN.Count > 0)
            {
                if (ThrottleController != null && ThrottleController.MaximumValue > 1 && MaxForceN / GearBox.mstsParams.GearBoxMaxTractiveForceForGearsN[0] > 3)
                    // Tricky things have been made with this .eng file, see e.g Cravens 105; let's correct them
                {
                    for (int i = 0; i < GearBox.mstsParams.GearBoxMaxTractiveForceForGearsN.Count; i++)
                        GearBox.mstsParams.GearBoxMaxTractiveForceForGearsN[i] *= ThrottleController.MaximumValue;
                }
                ThrottleController.Normalize(ThrottleController.MaximumValue);
                // correct also .cvf files
                if (CabViewList.Count > 0)
                    foreach (var cabView in CabViewList)
                    {
                        if (cabView.CVFFile != null && cabView.CVFFile.CabViewControls != null && cabView.CVFFile.CabViewControls.Count > 0)
                        {
                            foreach ( var control in cabView.CVFFile.CabViewControls)
                            {
                                if (control is CVCDiscrete && control.ControlType == CABViewControlTypes.THROTTLE && (control as CVCDiscrete).Values.Count > 0 && (control as CVCDiscrete).Values[(control as CVCDiscrete).Values.Count - 1] > 1)
                                {
                                    var discreteControl = (CVCDiscrete)control;
                                    for (var i = 0; i < discreteControl.Values.Count; i++)
                                        discreteControl.Values[i] /= ThrottleController.MaximumValue;
                                    if (discreteControl.MaxValue > 0) discreteControl.MaxValue = discreteControl.Values[discreteControl.Values.Count - 1];
                                }
                            }
                        }
                    }
                ThrottleController.MaximumValue = 1;
            }
            // Check also for very low DieselEngineIdleRPM
            if (IdleRPM < 10) IdleRPM = Math.Max(150, MaxRPM / 10);
        }
    } // class DieselLocomotive
}
