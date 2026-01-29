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
using ORTS.Scripting.Api;
using System.Linq;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;

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
        public ScriptedDieselPowerSupply DieselPowerSupply => PowerSupply as ScriptedDieselPowerSupply;

        public float IdleRPM;
        public float MaxRPM;
        public float GovernorRPM;
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
        public bool TractiveForcePowerLimited = false;

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

        public enum DieselTransmissionTypes
        {
            Legacy,
            Electric,
            Hydraulic,
            Mechanic,
            Hydromechanic,
        }
        public DieselTransmissionTypes DieselTransmissionType;

        float CalculatedMaxContinuousForceN;

        // diesel performance reporting
        public float DieselPerformanceTimeS = 0.0f; // Records the time since starting movement

    public DieselEngines DieselEngines;

        /// <summary>
        /// Used to accumulate a quantity that is not lost because of lack of precision when added to the Fuel level
        /// </summary>        
        float partialFuelConsumption = 0;

        private const float GearBoxControllerBoost = 1; // Slow boost to enable easy single gear up/down commands

        public MSTSDieselLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            DieselEngines = new DieselEngines(this);
            PowerSupply = new ScriptedDieselPowerSupply(this);
            RefillImmediately();
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowerondelay":
                case "engine(ortsauxpowerondelay":
                case "engine(ortspowersupply":
                case "engine(ortspowersupplyparameters":
                case "engine(ortstractioncutoffrelay":
                case "engine(ortstractioncutoffrelayclosingdelay":
                case "engine(ortsbattery":
                case "engine(ortsmasterkey(mode":
                case "engine(ortsmasterkey(delayoff":
                case "engine(ortsmasterkey(headlightcontrol":
                case "engine(ortselectrictrainsupply(mode":
                case "engine(ortselectrictrainsupply(dieselengineminrpm":
                    LocomotivePowerSupply.Parse(lowercasetoken, stf);
                    break;

                case "engine(dieselengineidlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsdieselenginegovernorrpm": GovernorRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                case "engine(dieselenginemaxrpmchangerate": MaxRPMChangeRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsdieselenginemaxpower": MaximumDieselEnginePowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, null); break;
                case "engine(effects(dieselspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(dieselsmokeeffectinitialsmokerate": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectinitialmagnitude": InitialMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxsmokerate": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxmagnitude": MaxMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                case "engine(ortstractiveforceispowerlimited": TractiveForcePowerLimited = stf.ReadBoolBlock(false); break;

                case "engine(ortsdieseltransmissiontype":
                    stf.MustMatch("(");
                    var transmissionType = stf.ReadString();
                    try
                    {
                        DieselTransmissionType = (DieselTransmissionTypes)Enum.Parse(typeof(DieselTransmissionTypes), transmissionType, true);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Skipped unknown diesel transmission type " + transmissionType);
                    }
                    break;
                case "engine(ortsdieselengines":
                case "engine(gearboxnumberofgears":
                case "engine(ortsreversegearboxindication":
                case "engine(gearboxdirectdrivegear":
                case "engine(ortsmainclutchtype":
                case "engine(ortsgearboxtype":
                case "engine(gearboxoperation":
                case "engine(gearboxenginebraking":
                case "engine(gearboxmaxspeedforgears":
                case "engine(gearboxmaxtractiveforceforgears":
                case "engine(ortsgearboxtractiveforceatspeed":
                case "engine(gearboxoverspeedpercentageforfailure":
                case "engine(gearboxbackloadforce":
                case "engine(gearboxcoastingforce":
                case "engine(gearboxupgearproportion":
                case "engine(gearboxdowngearproportion":
                case "engine(ortsgearboxfreewheel":
                    DieselEngines.Parse(lowercasetoken, stf);
                    break;

                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 120f); break;
                case "engine(ortsminoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 40f); break;
                case "engine(maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 0); break;
                case "engine(ortsdieselcooling": DieselEngineCooling = (DieselEngine.Cooling)stf.ReadInt((int)DieselEngine.Cooling.Proportional); break;
                default:
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

            // Create a diesel engine block if none exits, typically for a MSTS or BASIC configuration
            if (DieselEngines.Count == 0)
            {
                DieselEngines.Add(new DieselEngine(this));

                DieselEngines[0].InitFromMSTS();
                DieselEngines[0].Initialize();
            }


            // Check initialization of power values for diesel engines
            for (int i = 0; i < DieselEngines.Count; i++)
            {
                DieselEngines[i].InitDieselRailPowers(this);

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
                    float throttleSetting = 1.0f; // Must be at full throttle for these calculations
                    float StartingSpeedMpS = 0.1f; // Assumed starting speed for diesel - can't be zero otherwise error will occurr
                    MaxForceN = TractiveForceCurves.Get(throttleSetting, StartingSpeedMpS);

                    if (Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Force set to {0} value, calcuated from Tractive Force Tables", FormatStrings.FormatForce(MaxForceN, IsMetric));
                }
            }


            // Check force assumptions set for diesel   
            CalculatedMaxContinuousForceN = 0;
            float ThrottleSetting = 1.0f; // Must be at full throttle for these calculations
            if (TractiveForceCurves == null)  // Basic configuration - ie no force and Power tables, etc
            {
                CalculatedMaxContinuousForceN = ThrottleSetting * LocomotiveMaxRailOutputPowerW / SpeedOfMaxContinuousForceMpS;

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Diesel Force Settings (BASIC Config): Max Starting Force {0}, Calculated Max Continuous Force {1} @ speed of {2}", FormatStrings.FormatForce(MaxForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    Trace.TraceInformation("Diesel Power Settings (BASIC Config): Prime Mover {0}, Max Rail Output Power {1}", FormatStrings.FormatPower(MaximumDieselEnginePowerW, IsMetric, false, false), FormatStrings.FormatPower(LocomotiveMaxRailOutputPowerW, IsMetric, false, false));

                    if (MaxForceN < MaxContinuousForceN)
                    {
                        Trace.TraceInformation("!!!! Warning: Starting Tractive force {0} is less then Calculated Continuous force {1}, please check !!!!", FormatStrings.FormatForce(MaxForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    }
                }

            }
            else // Advanced configuration - 
            {
                float StartingSpeedMpS = 0.1f; // Assumed starting speed for diesel - can't be zero otherwise error will occurr
                float StartingForceN = TractiveForceCurves.Get(ThrottleSetting, StartingSpeedMpS);
                CalculatedMaxContinuousForceN = TractiveForceCurves.Get(ThrottleSetting, SpeedOfMaxContinuousForceMpS);
                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Diesel Force Settings (ADVANCED Config): Max Starting Force {0}, Calculated Max Continuous Force {1}, @ speed of {2}", FormatStrings.FormatForce(StartingForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    Trace.TraceInformation("Diesel Power Settings (ADVANCED Config): Prime Mover {0}, Max Rail Output Power {1} @ {2} rpm", FormatStrings.FormatPower(DieselEngines.MaxPowerW, IsMetric, false, false), FormatStrings.FormatPower(DieselEngines.MaximumRailOutputPowerW, IsMetric, false, false), MaxRPM);

                    if (StartingForceN < MaxContinuousForceN)
                    {
                        Trace.TraceInformation("!!!! Warning: Calculated Starting Tractive force {0} is less then Calculated Continuous force {1}, please check !!!!", FormatStrings.FormatForce(StartingForceN, IsMetric), FormatStrings.FormatForce(CalculatedMaxContinuousForceN, IsMetric), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                    }
                }
            }

            // Check that MaxPower value is realistic - Calculate power - metric - P = F x V
            float CalculatedContinuousPowerW = MaxContinuousForceN * SpeedOfMaxContinuousForceMpS;
            if (MaxPowerW < CalculatedContinuousPowerW && Simulator.Settings.VerboseConfigurationMessages)
            {
                Trace.TraceInformation("!!!! Warning: MaxPower {0} is less then continuous force calculated power {1} @ speed of {2}, please check !!!!", FormatStrings.FormatPower(MaxPowerW, IsMetric, false, false), FormatStrings.FormatPower(CalculatedContinuousPowerW, IsMetric, false, false), FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
            }

            if (!DieselEngines.HasGearBox)
            {
                // Check Adhesion values
                var calculatedmaximumpowerw = CalculatedMaxContinuousForceN * SpeedOfMaxContinuousForceMpS;
                var maxforcekN = MaxForceN / 1000.0f;
                var designadhesionzerospeed = maxforcekN / (Kg.ToTonne(DrvWheelWeightKg) * 10);
                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Zero Adhesion - zeroadhesion {0} maxForcekN {1} Driveweight {2}", designadhesionzerospeed, maxforcekN, DrvWheelWeightKg);
                }
                var calculatedmaxcontinuousforcekN = CalculatedMaxContinuousForceN / 1000.0f;
                var designadhesionmaxcontspeed = calculatedmaxcontinuousforcekN / (Kg.ToTonne(DrvWheelWeightKg) * 10);
                var zerospeed = 0;
                var configuredadhesionzerospeed = (Curtius_KnifflerA / (zerospeed + Curtius_KnifflerB) + Curtius_KnifflerC);
                var configuredadhesionmaxcontinuousspeed = (Curtius_KnifflerA / (SpeedOfMaxContinuousForceMpS + Curtius_KnifflerB) + Curtius_KnifflerC);
                var dropoffspeed = calculatedmaximumpowerw / (MaxForceN);
                var configuredadhesiondropoffspeed = (Curtius_KnifflerA / (dropoffspeed + Curtius_KnifflerB) + Curtius_KnifflerC);
                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Slip control system: {0}, Traction motor type: {1}", SlipControlSystem.ToString(), TractionMotorType.ToString()); // Slip control

                    Trace.TraceInformation("Apparent (Design) Adhesion: Zero - {0:N2} @ {1}, Max Continuous Speed - {2:N2} @ {3}, Drive Wheel Weight - {4}", designadhesionzerospeed, FormatStrings.FormatSpeedDisplay(zerospeed, IsMetric), designadhesionmaxcontspeed, FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric), FormatStrings.FormatMass(DrvWheelWeightKg, IsMetric));
                    Trace.TraceInformation("OR Calculated Adhesion Setting: Zero Speed - {0:N2} @ {1}, Dropoff Speed - {2:N2} @ {3}, Max Continuous Speed - {4:N2} @ {5}", configuredadhesionzerospeed, FormatStrings.FormatSpeedDisplay(zerospeed, IsMetric), configuredadhesiondropoffspeed, FormatStrings.FormatSpeedDisplay(dropoffspeed, IsMetric), configuredadhesionmaxcontinuousspeed, FormatStrings.FormatSpeedDisplay(SpeedOfMaxContinuousForceMpS, IsMetric));
                }
            }

            if (Simulator.Settings.VerboseConfigurationMessages)
            {
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
            GovernorRPM = locoCopy.GovernorRPM;
            MaxRPMChangeRate = locoCopy.MaxRPMChangeRate;
            MaximumDieselEnginePowerW = locoCopy.MaximumDieselEnginePowerW;
            PercentChangePerSec = locoCopy.PercentChangePerSec;
            LocomotiveMaxRailOutputPowerW = locoCopy.LocomotiveMaxRailOutputPowerW;
            DieselTransmissionType = locoCopy.DieselTransmissionType;

            EngineRPMderivation = locoCopy.EngineRPMderivation;
            EngineRPMold = locoCopy.EngineRPMold;

            TractiveForcePowerLimited = locoCopy.TractiveForcePowerLimited;

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

            DieselEngines.Copy(locoCopy.DieselEngines);
        }

        public override void Initialize(bool reinitialize = false)
        {
            DieselEngines.Initialize(reinitialize);

            if (DieselEngines[0].GearBox != null)
            {
                GearBoxController = new MSTSNotchController(DieselEngines[0].GearBox.NumOfGears + 1);
            }

            base.Initialize(reinitialize);

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

            if (Simulator.Settings.VerboseConfigurationMessages)
            {
                if (DieselEngines.HasGearBox)
                {
                    Trace.TraceInformation("==================================================== {0} has Gearbox =========================================================", LocomotiveName);
                    Trace.TraceInformation("Gearbox Type: {0}, Transmission Type: {1}, Number of Gears: {2}, Idle RpM: {3}, Max RpM: {4}, Gov RpM: {5}, GearBoxType: {6}, ClutchType: {7}, FreeWheel: {8}", DieselEngines[0].GearBox.GearBoxOperation, DieselTransmissionType, DieselEngines[0].GearBox.NumOfGears, DieselEngines[0].IdleRPM, DieselEngines[0].MaxRPM, DieselEngines[0].GovernorRPM, DieselEngines[0].GearBox.GearBoxType, DieselEngines[0].GearBox.ClutchType, DieselEngines[0].GearBox.GearBoxFreeWheelFitted);

                    Trace.TraceInformation("Gear\t Ratio\t Max Speed\t Max TE\t    Chg Up RpM\t Chg Dwn RpM\t Coast Force\t Back Force\t");

                    for (int i = 0; i < DieselEngines[0].GearBox.NumOfGears; i++)
                    {
                        Trace.TraceInformation("\t{0}\t\t\t {1:N2}\t\t{2:N2}\t\t{3:N2}\t\t\t{4}\t\t\t\t{5:N0}\t\t\t\t\t{6}\t\t\t{7}", i + 1, DieselEngines[0].GearBox.Gears[i].Ratio, FormatStrings.FormatSpeedDisplay(DieselEngines[0].GearBox.Gears[i].MaxSpeedMpS, IsMetric), FormatStrings.FormatForce(DieselEngines[0].GearBox.Gears[i].MaxTractiveForceN, IsMetric), DieselEngines[0].GearBox.Gears[i].ChangeUpSpeedRpM, DieselEngines[0].GearBox.Gears[i].ChangeDownSpeedRpM, FormatStrings.FormatForce(DieselEngines[0].GearBox.Gears[i].CoastingForceN, IsMetric), FormatStrings.FormatForce(DieselEngines[0].GearBox.Gears[i].BackLoadForceN, IsMetric));

                    }



                    var calculatedmaxcontinuousforcekN = DieselEngines[0].GearBox.Gears[0].MaxTractiveForceN / 1000.0f;
                    var designadhesionmaxcontspeed = calculatedmaxcontinuousforcekN / (Kg.ToTonne(DrvWheelWeightKg) * 10);

                    Trace.TraceInformation("Apparent (Design) Adhesion for Gear 1: {0:N2} @ {1}, Drive Wheel Weight - {2}", designadhesionmaxcontspeed, FormatStrings.FormatSpeedDisplay(DieselEngines[0].GearBox.Gears[0].MaxSpeedMpS, IsMetric), FormatStrings.FormatMass(DrvWheelWeightKg, IsMetric));

                    Trace.TraceInformation("===================================================================================================================\n\n");
                }
            }
            if (!Simulator.Settings.NoDieselEngineStart) SetPower(true);
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

            DieselEngines.InitializeMoving();

            if (DieselEngines[0].GearBox != null && GearBoxController != null)
            {
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
            DieselEngines.Update(elapsedClockSeconds);

            ExhaustParticles.Update(elapsedClockSeconds, DieselEngines[0].ExhaustParticles);
            ExhaustMagnitude.Update(elapsedClockSeconds, DieselEngines[0].ExhaustMagnitude);
            ExhaustColorR.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.R);
            ExhaustColorG.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.G);
            ExhaustColorB.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.B);

            base.Update(elapsedClockSeconds);

            // Calculate fuel consumption will occur unless diesel engine is stopped
            DieselFlowLps = DieselEngines.DieselFlowLps;
            partialFuelConsumption += DieselEngines.DieselFlowLps * elapsedClockSeconds;
            if (partialFuelConsumption >= 0.1)
            {
                DieselLevelL -= partialFuelConsumption;
                partialFuelConsumption = 0;
            }
            // stall engine if fuel runs out
            if (DieselLevelL <= 0.0f)
            {
                SignalEvent(Event.EnginePowerOff);
                DieselEngines.HandleEvent(PowerSupplyEvent.StopEngine);
            }

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
        /// This function updates periodically the states and physical variables of the locomotive's controllers.
        /// </summary>
        protected override void UpdateControllers(float elapsedClockSeconds)
        {
            base.UpdateControllers(elapsedClockSeconds);

            //Currently the ThrottlePercent is global to the entire train
            //So only the lead locomotive updates it, the others only updates the controller (actually useless)
            if (this.IsLeadLocomotive() || (RemoteControlGroup == -1))
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
        public override float GetAvailableTractionForceN(float t)
        {
            if (t <= 0) return 0;
            if (DieselEngines.HasGearBox && (TractiveForceCurves == null || DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic))
            {
                // TODO: provide more accurate values
                // Note: if this clause is fulfilled, this method is not used to calculate TractiveForceN, it just provides information for other subsystems about theoretical tractive force
                return base.GetAvailableTractionForceN(t);
            }
            float forceN = 0;
            float powerW = float.MaxValue;
            // If there is more then one diesel engine, and one or more engines is stopped, then the Fraction Power will give a fraction less then 1 depending upon power definitions of engines.
            float DieselEngineFractionPower = 1.0f;

            if (DieselEngines.Count > 1)
            {
                DieselEngineFractionPower = DieselEngines.RunningPowerFraction;
            }

            DieselEngineFractionPower = MathHelper.Clamp(DieselEngineFractionPower, 0.0f, 1.0f);  // Clamp decay within bounds


            if (!DieselEngines.HasGearBox && DieselPowerSupply.MaximumPowerW > 0)
            {
                powerW = DieselPowerSupply.MaximumPowerW * t;
            }
            // This section calculates the traction force of the locomotive as follows:
            // Basic configuration (no TF table) - uses P = F /speed  relationship - requires power and force parameters to be set in the ENG file. 
            // Advanced configuration (TF table) - use a user defined tractive force table
            if (TractiveForceCurves == null)
            {
                // This sets the maximum force of the locomotive, it will be adjusted down if it exceeds the max power of the locomotive.
                forceN = MaxForceN * (1 - PowerReduction) * t;

                // Maximum rail power is reduced by apparent throttle factor and the number of engines running (power ratio)
                powerW = Math.Min(powerW, LocomotiveMaxRailOutputPowerW * DieselEngineFractionPower * t);

                // If unloading speed is in ENG file, and locomotive speed is greater then unloading speed, and less then max speed, then apply a decay factor to the power/force
                if (UnloadingSpeedMpS != 0 && AbsTractionSpeedMpS > UnloadingSpeedMpS && AbsTractionSpeedMpS < MaxSpeedMpS && !WheelSlip)
                {
                    // use straight line curve to decay power to zero by 2 x unloading speed
                    float unloadingspeeddecay = 1.0f - (1.0f / UnloadingSpeedMpS) * (AbsTractionSpeedMpS - UnloadingSpeedMpS);
                    unloadingspeeddecay = MathHelper.Clamp(unloadingspeeddecay, 0.0f, 1.0f);  // Clamp decay within bounds
                    powerW *= unloadingspeeddecay;
                }
            }
            else
            {
                // Tractive force is read from Table using the apparent throttle setting, and then reduced by the number of engines running (power ratio)
                forceN = TractiveForceCurves.Get(t, AbsTractionSpeedMpS) * DieselEngineFractionPower * (1 - PowerReduction);
                if (forceN < 0 && !TractiveForceCurves.AcceptsNegativeValues())
                    forceN = 0;
            }
            if (forceN * AbsTractionSpeedMpS > powerW) forceN = powerW / AbsTractionSpeedMpS;
            return forceN;
        }
        protected override void UpdateTractionForce(float elapsedClockSeconds)
        {
            if (DieselEngines.HasGearBox && (TractiveForceCurves == null || DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic))
            {
                TractionForceN = DieselEngines.TractiveForceN;
                return;
            }
            base.UpdateTractionForce(elapsedClockSeconds);
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
                else if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Manual)
                {
                    DieselEngines[0].GearBox.ManualGearUp = true;
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
                else if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Manual)
                {
                    DieselEngines[0].GearBox.ManualGearDown = true;
                }
            }
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType.Type)
            {
                case CABViewControlTypes.GEARS:
                    if (DieselEngines.HasGearBox)
                        data = DieselEngines[0].GearBox.GearIndication;
                    break;

                case CABViewControlTypes.FUEL_GAUGE:
                    if (cvc.Units == CABViewControlUnits.GALLONS)
                        data = L.ToGUS(DieselLevelL);
                    else
                        data = DieselLevelL;
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_ORDER:
                    data = DieselPowerSupply.TractionCutOffRelay.DriverClosingOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_OPENING_ORDER:
                    data = DieselPowerSupply.TractionCutOffRelay.DriverOpeningOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_AUTHORIZATION:
                    data = DieselPowerSupply.TractionCutOffRelay.DriverClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_STATE:
                    switch (DieselPowerSupply.TractionCutOffRelay.State)
                    {
                        case TractionCutOffRelayState.Open:
                            data = 0;
                            break;
                        case TractionCutOffRelayState.Closing:
                            data = 1;
                            break;
                        case TractionCutOffRelayState.Closed:
                            data = 2;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_CLOSED:
                    switch (DieselPowerSupply.TractionCutOffRelay.State)
                    {
                        case TractionCutOffRelayState.Open:
                        case TractionCutOffRelayState.Closing:
                            data = 0;
                            break;
                        case TractionCutOffRelayState.Closed:
                            data = 1;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN:
                    switch (DieselPowerSupply.TractionCutOffRelay.State)
                    {
                        case TractionCutOffRelayState.Open:
                        case TractionCutOffRelayState.Closing:
                            data = 1;
                            break;
                        case TractionCutOffRelayState.Closed:
                            data = 0;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_AUTHORIZED:
                    data = DieselPowerSupply.TractionCutOffRelay.ClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN_AND_AUTHORIZED:
                    data = (DieselPowerSupply.TractionCutOffRelay.State < TractionCutOffRelayState.Closed && DieselPowerSupply.TractionCutOffRelay.ClosingAuthorization) ? 1 : 0;
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
                Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(DieselEngines[0].State)));
            if (DieselEngines.HasGearBox)
                status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Gear"),
                DieselEngines[0].GearBox.CurrentGearIndex < 0 ? Simulator.Catalog.GetParticularString("Gear", "N") : (DieselEngines[0].GearBox.GearIndication).ToString());
            status.AppendLine();
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Battery switch"),
                LocomotivePowerSupply.BatterySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Master key"),
                LocomotivePowerSupply.MasterKey.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Traction cut-off relay"),
                Simulator.Catalog.GetParticularString("TractionCutOffRelay", GetStringAttribute.GetPrettyName(DieselPowerSupply.TractionCutOffRelay.State)));
            status.AppendFormat("{0} = {1}\n",
                Simulator.Catalog.GetString("Electric train supply"),
                LocomotivePowerSupply.ElectricTrainSupplySwitch.On ? Simulator.Catalog.GetString("On") : Simulator.Catalog.GetString("Off"));
            status.AppendLine();
            status.AppendFormat("{0} = {1}",
                Simulator.Catalog.GetParticularString("PowerSupply", "Power"),
                Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(LocomotivePowerSupply.MainPowerSupplyState)));
            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());

            if (DieselEngines.HasGearBox && DieselTransmissionType == DieselTransmissionTypes.Mechanic)
            {
                    status.AppendFormat("\t{0} {1}-{2}", Simulator.Catalog.GetString("Gear"), DieselEngines[0].GearBox.CurrentGearIndex < 0 ? Simulator.Catalog.GetString("N") : (DieselEngines[0].GearBox.GearIndication).ToString(), DieselEngines[0].GearBox.GearBoxType);
            }
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
                   FormatStrings.FormatTemperature(C.FromF(SaturatedSteamHeatPressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]), IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
                   Simulator.Catalog.GetString("WaterLvl"),
                   FormatStrings.FormatFuelVolume(CurrentLocomotiveSteamHeatBoilerWaterCapacityL, IsMetric, IsUK),
                   Simulator.Catalog.GetString("Last:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(Train.LastCar.CarSteamHeatMainPipeSteamPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("Temp"),
                   FormatStrings.FormatTemperature(Train.LastCar.CarInsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("OutTemp"),
                   FormatStrings.FormatTemperature(CarOutsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("NetHt"),
                   Train.LastCar.CarNetHeatFlowRateW);
            }
            return status.ToString();
        }

        public string GetDPDebugStatus()
        {
            string throttle = "";
            if (ThrottlePercent > 0)
            {
                if (ThrottleController.NotchCount() > 3)
                    throttle = Simulator.Catalog.GetParticularString("Notch", "N") + MathHelper.Clamp(ThrottleController.GetNearestNotch(ThrottlePercent / 100f), 1, 8);
                else
                    throttle = string.Format("{0:F0}%", ThrottlePercent);
            }
            else if (DynamicBrakePercent > 0 && DynamicBrake)
            {
                if (RemoteControlGroup == 1)
                {
                    throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((Train.LeadLocomotive as MSTSLocomotive).DPDynamicBrakeController.CurrentNotch, 1, 8);
                }
                else
                {
                    // The clause here below leads to possible differences of one notch near the notch value, and therefore is commented
                    //               if (DynamicBrakeController.NotchCount() > 3)
                    //                   throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((DynamicBrakeController.GetNearestNotch(DynamicBrakePercent / 100f)), 1, 8);
                    //               else
                    throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((Train.LeadLocomotive as MSTSLocomotive).DPDynamicBrakeController.GetNotch(DynamicBrakePercent / 100f), 1, 8);
                }
            }
            else if (DynamicBrakePercent == 0 && !DynamicBrake)
                throttle = Simulator.Catalog.GetString("Setup");
            else
                throttle = Simulator.Catalog.GetParticularString("Notch", "Idle");
            if (DynamicBrakePercent >= 0)
                throttle += "???";

            var status = new StringBuilder();
            status.AppendFormat("{0}({1})\t", CarID, DPUnitID);
            status.AppendFormat("{0}\t", throttle);
            status.AppendFormat("{0} {1}\t", GetStringAttribute.GetPrettyName(Direction), Flipped ? Simulator.Catalog.GetString("(flipped)") : "");
            status.AppendFormat("{0}\t", IsLeadLocomotive() || RemoteControlGroup < 0 ? "———" : RemoteControlGroup == 0 ? Simulator.Catalog.GetString("Sync") : Simulator.Catalog.GetString("Async"));
            status.AppendFormat("{0}\t", FormatStrings.FormatFuelVolume(DieselLevelL, IsMetric, IsUK));
            status.AppendFormat("{0}{1}", FormatStrings.FormatForce(TractiveForceN, IsMetric), CouplerOverloaded ? "???" : "");
            status.Append(DieselEngines.GetDPStatus());

            return status.ToString();
        }

        public string GetDpuStatus(bool dataDpu, CABViewControlUnits loadUnits = CABViewControlUnits.NONE)// used by the TrainDpuInfo window
        {
            string throttle = "";
            if (ThrottlePercent > 0)
            {
                if (ThrottleController.NotchCount() > 3)
                    throttle = Simulator.Catalog.GetParticularString("Notch", "N") + MathHelper.Clamp(ThrottleController.GetNearestNotch(ThrottlePercent / 100f), 1, 8);
                else
                    throttle = string.Format("{0:F0}%", ThrottlePercent);
            }
            else if (DynamicBrakePercent > 0 && DynamicBrake)
            {
                if (RemoteControlGroup == 1)
                {
                    throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((Train.LeadLocomotive as MSTSLocomotive).DPDynamicBrakeController.CurrentNotch, 1,  8);
                }
                else
                {
                    // The clause here below leads to possible differences of one notch near the notch value, and therefore is commented
 //               if (DynamicBrakeController.NotchCount() > 3)
 //                   throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((DynamicBrakeController.GetNearestNotch(DynamicBrakePercent / 100f)), 1, 8);
 //               else
                    throttle = Simulator.Catalog.GetParticularString("Notch", "B") + MathHelper.Clamp((Train.LeadLocomotive as MSTSLocomotive).DPDynamicBrakeController.GetNotch(DynamicBrakePercent / 100f), 1, 8);
                }
            }
            else if (DynamicBrakePercent == 0 && !DynamicBrake)
                throttle = Simulator.Catalog.GetString("Setup");
            else
                throttle = Simulator.Catalog.GetParticularString("Notch", "Idle");
            if (DynamicBrakePercent >= 0)
                throttle += "???";

            var status = new StringBuilder();
            // ID
            status.AppendFormat("{0}({1})\t", CarID.Replace(" ", ""), DPUnitID);
            // Throttle
            status.AppendFormat("{0}\t", throttle);

            // Load
            var data = 0f;
            data = Math.Abs(TractiveForceN);
            if (DynamicBrakeForceN > 0)
            {
                data = -Math.Abs(DynamicBrakeForceN);
            }
            if (loadUnits == CABViewControlUnits.NONE)
                loadUnits = IsMetric ? CABViewControlUnits.AMPS : CABViewControlUnits.KILO_LBS;
            switch (loadUnits)
            {
                case CABViewControlUnits.AMPS:
                    if (ThrottlePercent >= 0 && DynamicBrakePercent == -1)
                    {
                        data = (data / MaxForceN) * MaxCurrentA;
                    }
                    if (ThrottlePercent == 0 && DynamicBrakePercent >= 0)
                    {
                        data = (data / MaxDynamicBrakeForceN) * DynamicBrakeMaxCurrentA;
                    }
                    status.AppendFormat("{0:F0} A", data);
                    break;

                case CABViewControlUnits.NEWTONS:
                    status.AppendFormat("{0:F0} N", data);
                    break;

                case CABViewControlUnits.KILO_NEWTONS:
                    data = data / 1000.0f;
                    status.AppendFormat("{0:F0} kN", data);
                    break;

                case CABViewControlUnits.LBS:
                    data = N.ToLbf(data);
                    status.AppendFormat("{0:F0} l", data);
                    break;

                case CABViewControlUnits.KILO_LBS:
                default:
                    data = N.ToLbf(data) * 0.001f;
                    status.AppendFormat("{0:F0} K", data);
                    break;
            }

            status.AppendFormat((data < 0 ? "???" : " ") + "\t");

            // BP
            var brakeInfoValue = brakeValue(Simulator.Catalog.GetString("BP"), Simulator.Catalog.GetString("Flow"));
            status.AppendFormat("{0:F0}\t", brakeInfoValue);
            // Air flow meter
            status.AppendFormat("{0:F0}\t", FormatStrings.FormatAirFlow(FilteredBrakePipeFlowM3pS, IsMetric));

            // Remote
            if (dataDpu)
            {
                status.AppendFormat("{0}\t", IsLeadLocomotive() || RemoteControlGroup < 0 ? "———" : RemoteControlGroup == 0 ? Simulator.Catalog.GetString("Sync") : Simulator.Catalog.GetString("Async"));
            }
            else
            {
                status.AppendFormat("{0}", IsLeadLocomotive() || RemoteControlGroup < 0 ? "———" : RemoteControlGroup == 0 ? Simulator.Catalog.GetString("Sync") : Simulator.Catalog.GetString("Async"));
            }

            if (dataDpu)
            {   // ER
                brakeInfoValue = brakeValue(Simulator.Catalog.GetString("EQ"), Simulator.Catalog.GetString("BC"));
                status.AppendFormat("{0:F0}\t", brakeInfoValue);

                // BC
                brakeInfoValue = Math.Round(BrakeSystem.GetCylPressurePSI()).ToString() + " psi";
                status.AppendFormat("{0:F0}\t", brakeInfoValue);

                // MR
                status.AppendFormat("{0:F0}", FormatStrings.FormatPressure(MainResPressurePSI, PressureUnit.PSI, (Simulator.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true));
            }
            return status.ToString();
        }

        string brakeValue(string tokenIni, string tokenEnd) // used by GetDpuStatus(bool dataHud)
        {
            string trainBrakeStatus = GetTrainBrakeStatus();
            var brakeInfoValue = "-";
            if (trainBrakeStatus.Contains(tokenIni) && trainBrakeStatus.Contains(tokenEnd))
            {
                var indexIni = trainBrakeStatus.IndexOf(tokenIni) + tokenIni.Length + 1;
                var indexEnd = trainBrakeStatus.IndexOf(tokenEnd) - indexIni;
                if (indexEnd > 0)// BP found before EOT
                    brakeInfoValue = trainBrakeStatus.Substring(indexIni, indexEnd).TrimEnd();
            }
            return brakeInfoValue;
        }

        public override string GetMultipleUnitsConfiguration()
        {
            if (Train == null)
                return base.GetMultipleUnitsConfiguration();
            var numberOfLocomotives = 0;
            var group = 0;
            var configuration = "";

            var dpUnitId = 0;
            var remoteControlGroup = 0;
            for (var i = 0; i < Train.Cars.Count; i++)
            {
                if (Train.Cars[i] is MSTSDieselLocomotive)
                {
                    if (dpUnitId != (dpUnitId = (Train.Cars[i] as MSTSLocomotive).DPUnitID))
                    {
                        configuration += string.Format("{0}{1}",
                            group,
                            remoteControlGroup != (remoteControlGroup = Train.Cars[i].RemoteControlGroup)
                                ? " | " : "\u2013"); // en-dash
                        group = 0;
                    }
                    group++;
                    numberOfLocomotives++;
                }
            }
            if (group > 0)
                configuration += string.Format("{0}", group);
            return numberOfLocomotives > 0 ? configuration : null;
        }

        private static string[] DebugLabels;
        private static int MaxNumberOfEngines;
        private static string[] DpuLabels;
        private static string[] DPULabels;

        private static void SetDebugLabels()
        {
            var labels = new StringBuilder();
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("ID"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Throttle"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetParticularString("NonSteam", "Reverser"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Remote"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Fuel"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Tractive Effort"));
            labels.Append(DieselEngines.SetDebugLabels());
            DebugLabels = labels.ToString().Split('\t');
        }

        private static void SetDPULabels(bool dpuFull, int numberOfEngines)
        {
            MaxNumberOfEngines = numberOfEngines;
            var labels = new StringBuilder();
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("ID"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Throttle"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Load"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("BP"));
            labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Flow"));
            if (!dpuFull)
            {
                labels.AppendFormat("{0}", Simulator.Catalog.GetString("Remote"));
                DpuLabels = labels.ToString().Split('\t');
            }
            if (dpuFull)
            {
                labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("Remote"));
                labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("ER"));
                labels.AppendFormat("{0}\t", Simulator.Catalog.GetString("BC"));
                labels.AppendFormat("{0}", Simulator.Catalog.GetString("MR"));
                DPULabels = labels.ToString().Split('\t');
            }
        }

        public static string GetDebugTableBase(int locomotivesInTrain)
        {
            if (DebugLabels == null)
                SetDebugLabels();

            string table = "";
            for (var i = 0; i < DebugLabels.Length; i++)
            {
                table += DebugLabels[i];
                for (var j = 0; j < locomotivesInTrain; j++)
                    table += "\t\t";
                table += "\n";
            }
             return table;
        }

        public static string GetDpuHeader(bool dpuVerticalFull, int locomotivesInTrain, int dpuMaxNumberOfEngines)
        {
            if (MaxNumberOfEngines != dpuMaxNumberOfEngines || dpuVerticalFull? DPULabels == null : DpuLabels == null)
                SetDPULabels(dpuVerticalFull , dpuMaxNumberOfEngines);
            string table = "";
            for (var i = 0; i < (dpuVerticalFull ? DPULabels.Length : DpuLabels.Length); i++)
            {
                table += dpuVerticalFull ? DPULabels[i] : DpuLabels[i];
                table += "\n";
            }
            table = table.TrimEnd('\n');
            return table;
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
            if (!LocomotivePowerSupply.MainPowerSupplyOn || !LocomotivePowerSupply.BatteryOn || !LocomotivePowerSupply.MasterKey.On)
            {
                LocomotivePowerSupply.HandleEvent(PowerSupplyEvent.QuickPowerOn);
            }
            foreach (DieselEngine de in DieselEngines)
            {
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
            if (DieselEngines.MSTSGearBoxParams.GearBoxMaxTractiveForceForGearsN.Count > 0)
            {
                if (ThrottleController != null && ThrottleController.MaximumValue > 1 && MaxForceN / DieselEngines.MSTSGearBoxParams.GearBoxMaxTractiveForceForGearsN[0] > 3)
                    // Tricky things have been made with this .eng file, see e.g Cravens 105; let's correct them
                {
                    for (int i = 0; i < DieselEngines.MSTSGearBoxParams.GearBoxMaxTractiveForceForGearsN.Count; i++)
                        DieselEngines.MSTSGearBoxParams.GearBoxMaxTractiveForceForGearsN[i] *= ThrottleController.MaximumValue;
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
                                if (control is CVCDiscrete && control.ControlType.Type == CABViewControlTypes.THROTTLE && (control as CVCDiscrete).Values.Count > 0 && (control as CVCDiscrete).Values[(control as CVCDiscrete).Values.Count - 1] > 1)
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
