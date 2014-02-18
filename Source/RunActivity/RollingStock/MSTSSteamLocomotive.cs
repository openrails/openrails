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

/* STEAM LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer. The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
using Microsoft.Xna.Framework;
using ORTS.Viewer3D;  // for MathHelper

namespace ORTS
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a steam locomotive
    /// </summary>
    public class MSTSSteamLocomotive: MSTSLocomotive
    {
        //Configure a default cutoff controller
        //If none is specified, this will be used, otherwise those values will be overwritten
        public MSTSNotchController CutoffController = new MSTSNotchController(-0.9f, 0.9f, 0.1f);
        public MSTSNotchController Injector1Controller = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController Injector2Controller = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController BlowerController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController DamperController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FiringRateController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FireboxDoorController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FuelController = new MSTSNotchController(0, 1, 0.01f); // Could be coal, wood, oil or even peat !
        public MSTSNotchController WaterController = new MSTSNotchController(0, 1, 0.01f);

        bool Injector1IsOn;
        bool Injector2IsOn;
        public bool CylinderCocksAreOpen;
        bool FiringIsManual;
        bool BlowerIsOn = false;
        bool BoilerIsPriming = false;
        bool WaterIsExhausted = false;
        bool CoalIsExhausted = false;
        bool FireIsExhausted = false;
        bool FuelBoost = false;
        bool FuelBoostReset = false;
        bool StokerIsMechanical = false;
        bool HotStart = true;
        bool BoilerHeat = false;
        bool HasSuperheater = false;
        bool safety2IsOn = false; // Safety valve #2 is on and opertaing
        bool safety3IsOn = false; // Safety valve #3 is on and opertaing
        bool safety4IsOn = false; // Safety valve #4 is on and opertaing
        bool IsGearedSteamLoco = false; // Indicates that it is a geared locomotive
        bool IsGearedSpeedExcess = false; // Flag indicating that geared locomotive speed has been exceeded 

        // state variables
        float BoilerHeatBTU;        // total heat in water and steam in boiler - lb/s * SteamHeat(BTU/lb)
        float MaxBoilerHeatBTU;   // Boiler heat at max output and pressure, etc
        float MaxBoilerHeatPressurePSI; // Boiler Pressure for calculating max boiler pressure, includes safety valve pressure
        float PreviousBoilerHeatInBTU; // Hold previous boiler heat value
        float BoilerStartkW;        // calculate starting boilerkW
        float MaxBoilerHeatInBTUpS = 0.1f; // Remember the BoilerHeat value equivalent to Max Boiler Heat
        float MaxBoilerPressHeatBTU;  // Boiler heat at max boiler pressure
        float baseStartTempK;     // Starting water temp
        float StartBoilerHeatBTU;
        float FiringSteamUsageRateLBpS;   // rate if excessive usage
        float BoilerHeatRatio = 1.0f;   // Boiler heat ratio, if boiler heat exceeds, normal boiler pressure boiler heat
        float MaxBoilerHeatRatio = 1.0f;   // Max Boiler heat ratio, if boiler heat exceeds, safety boiler pressure boiler heat
        SmoothedData BoilerHeatSmoothBTU = new SmoothedData(60);       // total heat in water and steam in boiler - lb/s * SteamHeat(BTU/lb)
        float BoilerMassLB;         // total mass of water and steam in boiler

        float BoilerKW;                 // power of boiler
        float MaxBoilerKW;              // power of boiler at full performance
        float BoilerSteamHeatBTUpLB;    // Steam Heat based on current boiler pressure
        float BoilerWaterHeatBTUpLB;    // Water Heat based on current boiler pressure
        float BoilerSteamDensityLBpFT3; // Steam Density based on current boiler pressure
        float BoilerWaterDensityLBpFT3; // Water Density based on current boiler pressure
        float BoilerWaterTempK; 
        float FuelBurnRateLBpS;
        float FuelFeedRateLBpS;
        float DesiredChange;     // Amount of change to increase fire mass, clamped to range 0.0 - 1.0
        public float CylinderSteamUsageLBpS;
        public float BlowerSteamUsageLBpS;
        public float BoilerPressurePSI;
 
        float WaterFraction;        // fraction of boiler volume occupied by water
        public float EvaporationLBpS;          // steam generation rate
        public float FireMassKG;      // Mass of coal currently on grate area
        public float FireRatio;
        float FlueTempK = 775;      // Initial FlueTemp (best @ 475)
        float MaxFlueTempK;         // FlueTemp at full boiler performance
        public bool SafetyIsOn;
        public readonly SmoothedData Smoke = new SmoothedData(15);

        // eng file configuration parameters
        float MaxBoilerPressurePSI = 180f;  // maximum boiler pressure, safety valve setting
        float BoilerVolumeFT3;      // total space in boiler that can hold water and steam
        int NumCylinders = 2;
        float CylinderStrokeM;
        float CylinderDiameterM;
        float MaxBoilerOutputLBpH;  // maximum boiler steam generation rate
        float ExhaustLimitLBpH;     // steam usage rate that causes increased back pressure
        public float BasicSteamUsageLBpS;  // steam used for auxiliary stuff - ie loco at rest
        float IdealFireMassKG;      // Target fire mass
        float MaxFireMassKG;        // Max possible fire mass
        float MaxFiringRateKGpS;              // Max rate at which fireman or stoker can can feed coal into fire
        float GrateLimitLBpS = 140.0f;       // Max combustion rate of the grate, once this is reached, no more steam is produced.
        float PreviousFireHeatTxfKW;    // Capture max FireHeat value before Grate limit is exceeded.
        float GrateCombustionRateLBpFt2;     // Grate combustion rate, ie how many lbs coal burnt per sq ft grate area.
        float ORTSMaxFiringRateKGpS;          // OR equivalent of above
        public float SafetyValveUsageLBpS;
        float SafetyValveDropPSI = 4.0f;      // Pressure drop before Safety valve turns off, normally around 4 psi - First safety valve normally operates between MaxBoilerPressure, and MaxBoilerPressure - 4, ie Max Boiler = 200, cutoff = 196.
        float EvaporationAreaM2;
        float SuperheatAreaM2 = 0.0f;      // Heating area of superheater
        float SuperheatKFactor = 11.7f;     // Factor used to calculate superheat temperature - guesstimate
        float SuperheatRefTempF;            // Superheat temperature in deg Fahrenheit, based upon the heating area.
        float SuperheatTempRatio;          // A ratio used to calculate the superheat - based on the ratio of superheat (using heat area) to "known" curve. 
        float CurrentSuperheatTeampF;      // current value of superheating based upon boiler steam output
        float SuperheatVolumeRatio;   // Approximate ratio of Superheated steam to saturated steam at same pressure
        float FuelCalorificKJpKG = 33400;
        float ManBlowerMultiplier = 20.0f; // Blower Multipler for Manual firing
        float ShovelMassKG = 6;
        float BurnRateMultiplier = 1.0f; // Used to vary the rate at which fuels burns at - used as a player customisation factor.
        float HeatRatio = 0.001f;        // Ratio to control burn rate - based on ratio of heat in vs heat out
        float PressureRatio = 0.01f;    // Ratio to control burn rate - based upon boiler pressure
        float BurnRateRawLBpS;           // Raw burnrate
        SmoothedData FuelRateStokerLBpS = new SmoothedData(30); // Stoker is more responsive and only takes x seconds to fully react to changing needs.
        SmoothedData FuelRate = new SmoothedData(90); // Automatic fireman takes x seconds to fully react to changing needs.
        SmoothedData BurnRateSmoothLBpS = new SmoothedData(300); // Changes in BurnRate take x seconds to fully react to changing needs.
        float FuelRateSmoothLBpS = 0.0f;     // Smoothed Fuel Rate
        
        // precomputed values
        float CylinderSweptVolumeFT3pFT;     // Volume of steam Cylinder
        float BlowerSteamUsageFactor;
        float InjectorFlowRateLBpS;
        Interpolator ForceFactor1Npcutoff;  // negative pressure part of tractive force given cutoff
        Interpolator ForceFactor2Npcutoff;  // positive pressure part of tractive force given cutoff
        Interpolator CylinderPressureDropLBpStoPSI;     // pressure drop from throttle to cylinders given usage
        Interpolator BackPressureLBpStoPSI;             // back pressure in cylinders given usage
        Interpolator CylinderSteamDensityPSItoLBpFT3;   // steam density in cylinders given pressure (could be super heated)
        Interpolator SteamDensityPSItoLBpFT3;   // saturated steam density given pressure
        Interpolator WaterDensityPSItoLBpFT3;   // water density given pressure
        Interpolator SteamHeatPSItoBTUpLB;      // total heat in saturated steam given pressure
        Interpolator WaterHeatPSItoBTUpLB;      // total heat in water given pressure
        Interpolator HeatToPressureBTUpLBtoPSI; // pressure given total heat in water (inverse of WaterHeat)
        Interpolator BurnRateLBpStoKGpS;        // fuel burn rate given steam usage - units in kg/s
        Interpolator PressureToTemperaturePSItoF;
        Interpolator InjDelWaterTempMinPressureFtoPSI; // Injector Delivery Water Temp - Minimum Capacity
        Interpolator InjDelWaterTempMaxPressureFtoPSI; // Injector Delivery Water Temp - Maximum Capacity
        Interpolator InjWaterFedSteamPressureFtoPSI; // Injector Water Lbs of water per lb steam used
        Interpolator InjCapMinFactorX; // Injector Water Table to determin min capacity - max/min
        Interpolator Injector09FlowratePSItoUKGpM;  // Flowrate of 09mm injector in gpm based on boiler pressure        
        Interpolator Injector10FlowratePSItoUKGpM;  // Flowrate of 10mm injector in gpm based on boiler pressure
        Interpolator Injector11FlowratePSItoUKGpM;  // Flowrate of 11mm injector in gpm based on boiler pressure
        Interpolator Injector13FlowratePSItoUKGpM;  // Flowrate of 13mm injector in gpm based on boiler pressure 
        Interpolator Injector14FlowratePSItoUKGpM;  // Flowrate of 14mm injector in gpm based on boiler pressure         
        Interpolator Injector15FlowratePSItoUKGpM;  // Flowrate of 15mm injector in gpm based on boiler pressure                       
        Interpolator SpecificHeatKtoKJpKGpK;        // table for specific heat capacity of water at temp of water
        Interpolator SaturationPressureKtoPSI;      // Saturated pressure of steam (psi) @ water temperature (K)
        Interpolator BoilerEfficiencyGrateAreaLBpFT2toX;      //  Table to determine boiler efficiency based upon lbs of coal per sq ft of Grate Area
        Interpolator BoilerEfficiency;  // boiler efficiency given steam usage
        Interpolator WaterTempFtoPSI;  // Table to convert water temp to pressure
        Interpolator CylinderCondensationFractionX;  // Table to find the cylinder condensation fraction per cutoff for the cylinder - saturated steam
        Interpolator SuperheatTempLimitXtoDegF;  // Table to find Super heat temp required to prevent cylinder condensation - Ref Elseco Superheater manual
        Interpolator SuperheatTempLbpHtoDegF;  // Table to find Super heat temp per lbs of steam to cylinder - from BTC Test Results for Std 8
        Interpolator InitialPressureDropRatioRpMtoX; // Allowance for wire-drawing - ie drop in initial pressure (cutoff) as speed increases
        Interpolator CutoffPressureDropRatioRpMtoX; // Allowance for pressure drop in Cut-off pressure compared to Initial Pressure - NB only curve for 50% cutoff done
        Interpolator SteamChestPressureDropRatioRpMtoX; // Allowance for pressure drop in Steam chest pressure compared to Boiler Pressure
        
        float CylinderPressurePSI;
        float BackPressurePSI;

  #region Additional steam properties
        const float SpecificHeatCoalKJpKGpK = 1.26f; // specific heat of coal - Kj/kg Kelvin
        float WaterHeatBTUpFT3;             // Water heat in btu/ft3
        bool FusiblePlugIsBlown = false;    // Fusible plug blown, due to lack of water in the boiler
        bool LocoIsOilBurner = false;       // Used to identify if loco is oil burner
        float GrateAreaM2;                  // Grate Area in SqM
        float IdealFireDepthIN = 7.0f;      // Assume standard coal coverage of grate = 7 inches.
        float FuelDensityKGpM3 = 864.5f;    // Anthracite Coal : 50 - 58 (lb/ft3), 800 - 929 (kg/m3)
        float DamperFactorManual = 1.0f;    // factor to control draft through fire when locomotive is running in Manual mode
        const float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        float MaxTenderCoalMassKG;          // Maximum read from Eng File
        float MaxTenderWaterMassKG;         // Maximum read from Eng file
        float TenderCoalMassLB              // Decreased by firing and increased by refilling
        {
            get { return FuelController.CurrentValue * Kg.ToLb(MaxTenderCoalMassKG); }
            set { FuelController.CurrentValue = value / Kg.ToLb(MaxTenderCoalMassKG); }
        }
        float TenderWaterVolumeUKG          // Decreased by running injectors and increased by refilling
        {
            get { return WaterController.CurrentValue * Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG; }
            set { WaterController.CurrentValue = value / (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG); }
        }
        float DamperBurnEffect;             // Effect of the Damper control
        float Injector1Fraction = 0.0f;     // Fraction (0-1) of injector 1 flow from Fireman controller or AI
        float Injector2Fraction = 0.0f;     // Fraction (0-1) of injector  of injector 2 flow from Fireman controller or AI
        float SafetyValveStartPSI = 0.1f;   // Set safety valve to just over max pressure - allows for safety valve not to operate in AI firing
        float InjectorBoilerInputLB = 0.0f; // Input into boiler from injectors
        public float CylCockSteamUsageLBpS = 0.0f; // Cylinder Cock Steam Usage
        float CylCockDiaIN = 0.5f;          // Steam Cylinder Cock orifice size
        float CylCockPressReduceFactor;     // Factor to reduce cylinder pressure by if cocks open

        // Air Compressor Characteristics - assume 9.5in x 10in Compressor operating at 120 strokes per min.          
        float CompCylDiaIN = 9.5f;
        float CompCylStrokeIN = 10.0f;
        float CompStrokespM = 120.0f;
        float CompSteamUsageLBpS = 0.0f;
        const float BTUpHtoKJpS = 0.000293071f;     // Convert BTU/s to Kj/s
        float BoilerHeatTransferCoeffWpM2K = 45.0f; // Heat Transfer of locomotive boiler 45 Wm2K
        float TotalSteamUsageLBpS;                  // Running total for complete current steam usage
        float GeneratorSteamUsageLBpS = 1.0f;       // Generator Steam Usage
        float RadiationSteamLossLBpS = 2.5f;        // Steam loss due to radiation losses
        float BlowerBurnEffect;                     // Effect of Blower on burning rate
        float FlueTempDiffK;                        // Current difference in flue temp at current firing and steam usage rates.
        float FireHeatTxfKW;                        // Current heat generated by the locomotive fire
        float HeatMaterialThicknessFactor = 1.0f;   // Material thickness for convection heat transfer
        float TheoreticalMaxSteamOutputLBpS;        // Max boiler output based upon Output = EvapArea x 15 ( lbs steam per evap area)

        // Water model - locomotive boilers require water level to be maintained above the firebox crown sheet
        // This model is a crude representation of a water gauge based on a generic boiler and 8" water gauge
        // Based on a scaled drawing following water fraction levels have been used - crown sheet = 0.7, min water level = 0.73, max water level = 0.89
        float WaterGlassMaxLevel = 0.89f;   // max height of water gauge as a fraction of boiler level
        float WaterGlassMinLevel = 0.73f;   // min height of water gauge as a fraction of boiler level
        float WaterGlassLengthIN = 8.0f;    // nominal length of water gauge
        float WaterGlassLevelIN;            // Water glass level in inches
        float MEPFactor = 0.7f;             // Factor to determine the MEP
        float GrateAreaDesignFactor = 500.0f;   // Design factor for determining Grate Area
        float EvapAreaDesignFactor = 10.0f;     // Design factor for determining Evaporation Area

        float SpecificHeatWaterKJpKGpC; // Specific Heat Capacity of water in boiler (from Interpolator table) kJ/kG per deg C
        float WaterTempIN;              // Input to water Temp Integrator.
        float WaterTempNewK;            // Boiler Water Temp (Kelvin) - for testing purposes
        float BkW_Diff;                 // Net Energy into boiler after steam loads taken.
        float WaterVolL;                // Actual volume of water in bolier (litres)
        float BoilerHeatOutBTUpS = 0.0f;// heat out of boiler in BTU
        float BoilerHeatInBTUpS = 0.0f; // heat into boiler in BTU
        float InjCylEquivSizeIN;        // Calculate the equivalent cylinder size for purpose of sizing the injector.
        float InjectorSize;             // size of injector installed on boiler

        // Values from previous iteration to use in UpdateFiring() and show in HUD
        float PreviousBoilerHeatOutBTUpS = 0.0f;
        public float PreviousTotalSteamUsageLBpS;
        float Injector1WaterDelTempF;   // Injector 1 water delivery temperature - F
        float Injector2WaterDelTempF;   // Injector 1 water delivery temperature - F
        float Injector1TempFraction;    // Find the fraction above the min temp of water delivery
        float Injector2TempFraction;    // Find the fraction above the min temp of water delivery
        float Injector1WaterTempPressurePSI;  // Pressure equivalent of water delivery temp
        float Injector2WaterTempPressurePSI;  // Pressure equivalent of water delivery temp
        float MaxInject1SteamUsedLbpS;  // Max steam injected into boiler when injector operating at full value - Injector 1
        float MaxInject2SteamUsedLbpS;  // Max steam injected into boiler when injector operating at full value - Injector 2
        float ActInject1SteamUsedLbpS;  // Act steam injected into boiler when injector operating at current value - Injector 1
        float ActInject2SteamUsedLbpS;  // Act steam injected into boiler when injector operating at current value - Injector 2   
        float Inject1SteamHeatLossBTU;  // heat loss due to steam usage from boiler for injector operation - Injector 1     
        float Inject2SteamHeatLossBTU;  // heat loss due to steam usage from boiler for injector operation - Injector 2
        float Inject1WaterHeatLossBTU;  // heat loss due to water injected into the boiler for injector operation - Injector 1   
        float Inject2WaterHeatLossBTU;  // heat loss due to water injected into the boiler for injector operation - Injector 1                        

        // Derating factors for motive force 
        float BoilerPrimingDeratingFactor = 0.1f;   // Factor if boiler is priming
        float OneAtmospherePSI = 14.696f;      // Atmospheric Pressure
        
        float StartTractiveEffortN = 0.0f;      // Record starting tractive effort
        float SuperheaterFactor = 1.0f;               // Currently 2 values respected: 1.0 for no superheat (default), > 1.0 for typical superheat
        float SuperheaterSteamUsageFactor = 1.0f;       // Below 1.0, reduces steam usage due to superheater
        float Stoker = 0.0f;                // Currently 2 values respected: 0.0 for no mechanical stoker (default), = 1.0 for typical mechanical stoker
        float StokerMaxUsage = 0.01f;       // Max steam usage of stoker - 1% of max boiler output
        float StokerMinUsage = 0.005f;      // Min Steam usage - just to keep motor ticking over - 0.5% of max boiler output
        float StokerSteamUsageLBpS;         // Current steam usage of stoker
        const float BoilerKWtoBHP = 0.101942f;  // Convert Boiler kW to Boiler HP, note different to HP.
        float MaxTheoreticalFiringRateKgpS;     // Max firing rate that fireman can sustain for short periods
        float FuelBoostOnTimerS = 0.01f;    // Timer to allow fuel boosting for a short while
        float FuelBoostResetTimerS = 0.01f; // Timer to rest fuel boosting for a while
        float TimeFuelBoostOnS = 300.0f;    // Time to allow fuel boosting to go on for 
        float TimeFuelBoostResetS = 1800.0f;// Time to wait before next fuel boost
        float throttle;
        float SpeedEquivMpS = 27.0f;          // Equvalent speed of 60mph in mps (27m/s) - used for damper control
        float MeanEffectivePressurePSI;         // Mean effective pressure
        float RatioOfExpansion;             // Ratio of expansion
        float CylinderClearancePC = 0.1f;    // Assume a cylinder clearance of 10% of the piston displacement
        float CylinderCompressionPC = 0.5f; // Compression occurs at % - 50% assumes 0.5 left
        float CylinderPistonShaftFt3;   // Volume taken up by the cylinder piston shaft
        float CylinderPistonShaftDiaIn = 3.5f; // Assume cylinder piston shaft to be 3.5 inches
        float CylinderPistonAreaFt2;    // Area of the piston in the cylinder
        float CylinderExhaustPressurePSI;  // Pressure in Cylinder at the end of the stroke.
        float CylinderPressureVolumeCutoffFactor; // calculation of c =PV @ cutoff
        float CylinderCompressionPressurePSI;   // Compression Pressure in cylinder
        float CylinderExhaustPC = 0.85f;     // Point at which the cylinder exhausts
        float MeanPressureStrokePSI;
        float MeanBackPressurePSI;         // Back pressure allowing for compression and clearance
        float SteamChestPressurePSI;    // Pressure in steam chest - input to cylinder
        float InitialPressurePSI;
        float TractiveEffortLbsF;
        const int CylStrokesPerCycle = 2;  // each cylinder does 2 strokes for every wheel rotation, within each stroke
        float DrvWheelRevRpS;       // number of revolutions of the drive wheel per minute based upon speed.
        float PistonSpeedFtpM;      // Piston speed of locomotive
        const float FeetinMile = 5280.0f;   // Feet in a mile
        float IndicatedHorsePowerHP;   // Indicated Horse Power (IHP), theoretical power of the locomotive, it doesn't take into account the losses due to friction, etc. Typically output HP will be 70 - 90% of the IHP
        float DrawbarHorsePowerHP;  // Drawbar Horse Power  (DHP), maximum power available at the wheels.
        float DrawBarPullLbsF;      // Drawbar pull in lbf
        float BoilerEvapRateLbspFt2;  // Sets the evaporation rate for the boiler is used to multiple boiler evaporation area by - used as a player customisation factor.
        float CylinderEfficiencyRate = 1.0f; // Factor to vary the output power of the cylinder without changing steam usage - used as a player customisation factor.
        float SpeedFactor;      // Speed factor - factor to reduce TE due to speed increase - American locomotive company
        public float MaxTractiveEffortLbf;     // Maximum tractive effort for locomotive
        float MaxLocoSpeedMpH;      // Speed of loco when max performance reached
        float MaxPistonSpeedFtpM;   // Piston speed @ max performance for the locomotive
        float MaxIndicatedHorsePowerHP; // IHP @ max performance for the locomotive
        float absSpeedMpS;
        float t;
        float CriticalSpeedTractiveEffortLbf;  // Speed at which the piston speed reaches it maximum recommended value
        float currentSpeedMpS;
        float currentWheelSpeedMpS;
        float maxForceN;
        float maxPowerW;
        float cutoff;
        public float DrvWheelWeightKg; // weight on locomotive drive wheel, includes drag factor
        float NumSafetyValves;  // Number of safety valves fitted to locomotive - typically 1 to 4
        float SafetyValveSizeIn;    // Size of the safety value - all will be the same size.
        float SafetyValveSizeDiaIn2; // Area of the safety valve - impacts steam discharge rate - is the space when the valve lifts
        float MaxSafetyValveDischargeLbspS; // Steam discharge rate of all safety valves combined.
        float SafetyValveUsage1LBpS; // Usage rate for safety valve #1
        float SafetyValveUsage2LBpS; // Usage rate for safety valve #2
        float SafetyValveUsage3LBpS; // Usage rate for safety valve #3
        float SafetyValveUsage4LBpS; // Usage rate for safety valve #4
        float MaxSteamGearPistonRateRpM;   // Max piston rate for a geared locomotive, such as a Shay
        float SteamGearRatio = 0.0f;   // Gear ratio for a geared locomotive, such as a Shay  
        float MaxGearedSpeedMpS;  // Max speed of the geared locomotive 
        float MotiveForceGearRatio; // mulitplication factor to be used in calculating motive force etc, when a geared locomotive.    

  #endregion 

        public MSTSSteamLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            if (NumCylinders < 0 && ZeroError(NumCylinders, "NumCylinders", wagFile))
                NumCylinders = 0;
            if (ZeroError(CylinderDiameterM, "CylinderDiammeter", wagFile))
                CylinderDiameterM= 1;
            if (ZeroError(CylinderStrokeM, "CylinderStroke", wagFile))
                CylinderStrokeM= 1;
            if (ZeroError(DriverWheelRadiusM, "WheelRadius", wagFile))
                DriverWheelRadiusM= 1;
            if (ZeroError(MaxBoilerPressurePSI, "MaxBoilerPressure", wagFile))
                MaxBoilerPressurePSI= 1;
            if (ZeroError(MaxBoilerOutputLBpH, "MaxBoilerOutput", wagFile))
                MaxBoilerOutputLBpH= 1;
            if (ZeroError(ExhaustLimitLBpH, "ExhaustLimit", wagFile))
                ExhaustLimitLBpH = MaxBoilerOutputLBpH;
            if (ZeroError(BoilerVolumeFT3, "BoilerVolume", wagFile))
                BoilerVolumeFT3 = 1;

        #region Initialise additional steam properties

            SteamDensityPSItoLBpFT3 = SteamTable.SteamDensityInterpolatorPSItoLBpFT3();
            WaterDensityPSItoLBpFT3 = SteamTable.WaterDensityInterpolatorPSItoLBpFT3();
            SteamHeatPSItoBTUpLB = SteamTable.SteamHeatInterpolatorPSItoBTUpLB();
            WaterHeatPSItoBTUpLB = SteamTable.WaterHeatInterpolatorPSItoBTUpLB();
            CylinderSteamDensityPSItoLBpFT3 = SteamTable.SteamDensityInterpolatorPSItoLBpFT3();
            HeatToPressureBTUpLBtoPSI = SteamTable.WaterHeatToPressureInterpolatorBTUpLBtoPSI();
            PressureToTemperaturePSItoF = SteamTable.PressureToTemperatureInterpolatorPSItoF();
            Injector09FlowratePSItoUKGpM = SteamTable.Injector09FlowrateInterpolatorPSItoUKGpM();
            Injector10FlowratePSItoUKGpM = SteamTable.Injector10FlowrateInterpolatorPSItoUKGpM();
            Injector11FlowratePSItoUKGpM = SteamTable.Injector11FlowrateInterpolatorPSItoUKGpM();
            Injector13FlowratePSItoUKGpM = SteamTable.Injector13FlowrateInterpolatorPSItoUKGpM();
            Injector14FlowratePSItoUKGpM = SteamTable.Injector14FlowrateInterpolatorPSItoUKGpM();            
            Injector15FlowratePSItoUKGpM = SteamTable.Injector15FlowrateInterpolatorPSItoUKGpM();            
            InjDelWaterTempMinPressureFtoPSI = SteamTable.InjDelWaterTempMinPressureInterpolatorFtoPSI();
            InjDelWaterTempMaxPressureFtoPSI = SteamTable.InjDelWaterTempMaxPressureInterpolatorFtoPSI();
            InjWaterFedSteamPressureFtoPSI = SteamTable.InjWaterFedSteamPressureInterpolatorFtoPSI();
            InjCapMinFactorX = SteamTable.InjCapMinFactorInterpolatorX();
            WaterTempFtoPSI = SteamTable.TemperatureToPressureInterpolatorFtoPSI();
            SpecificHeatKtoKJpKGpK = SteamTable.SpecificHeatInterpolatorKtoKJpKGpK();
            SaturationPressureKtoPSI = SteamTable.SaturationPressureInterpolatorKtoPSI();
            BoilerEfficiencyGrateAreaLBpFT2toX = SteamTable.BoilerEfficiencyGrateAreaInterpolatorLbstoX();
            CylinderCondensationFractionX = SteamTable.CylinderCondensationFractionInterpolatorX();
            SuperheatTempLimitXtoDegF = SteamTable.SuperheatTempLimitInterpolatorXtoDegF();
            SuperheatTempLbpHtoDegF = SteamTable.SuperheatTempInterpolatorLbpHtoDegF();
            InitialPressureDropRatioRpMtoX = SteamTable.InitialPressureDropRatioInterpolatorRpMtoX();
            CutoffPressureDropRatioRpMtoX = SteamTable.CutoffPressureDropRatioInterpolatorRpMtoX();
            SteamChestPressureDropRatioRpMtoX = SteamTable.SteamChestPressureDropRatioInterpolatorRpMtoX();
          
			RefillTenderWithCoal();
            RefillTenderWithWater();

            // Computed Values
            // Read alternative OR Value for calculation of Ideal Fire Mass
            if (GrateAreaM2 == 0)  // Calculate Grate Area if not present in ENG file
            {
                float MinGrateAreaSizeSqFt = 6.0f;
                GrateAreaM2 = Me2.FromFt2(((NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) * MEPFactor * MaxBoilerPressurePSI) / (Me.ToIn(DriverWheelRadiusM * 2.0f) * GrateAreaDesignFactor));
                GrateAreaM2 = MathHelper.Clamp(GrateAreaM2, Me2.FromFt2(MinGrateAreaSizeSqFt), GrateAreaM2); // Clamp gratearea to a minimum value of 6 sq ft
                IdealFireMassKG = GrateAreaM2 * Me.FromIn(IdealFireDepthIN) * FuelDensityKGpM3;
            }
            else
                if (LocoIsOilBurner)
                    IdealFireMassKG = GrateAreaM2 * 720.0f * 0.08333f * 0.02382f * 1.293f;  // Check this formula as conversion factors maybe incorrect, also grate area is now in SqM
                else
                    IdealFireMassKG = GrateAreaM2 * Me.FromIn(IdealFireDepthIN) * FuelDensityKGpM3;
            if (MaxFireMassKG == 0) // If not specified, assume twice as much as ideal. 
                                    // Scale FIREBOX control to show FireMassKG as fraction of MaxFireMassKG.
                MaxFireMassKG = 2 * IdealFireMassKG;

            float baseTempK = C.ToK(C.FromF(PressureToTemperaturePSItoF[MaxBoilerPressurePSI]));
            if (EvaporationAreaM2 == 0)        // If evaporation Area is not in ENG file then synthesize a value
                EvaporationAreaM2 = Me2.FromFt2(((NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) * MEPFactor * MaxBoilerPressurePSI) / (Me.ToIn(DriverWheelRadiusM * 2.0f) * EvapAreaDesignFactor));


            CylinderSteamUsageLBpS = 1.0f;  // Set to 1 to ensure that there are no divide by zero errors
            WaterFraction = 0.9f;

            if (BoilerEvapRateLbspFt2 == 0) // If boiler evaporation rate is not in ENG file then set a default value
            {
                BoilerEvapRateLbspFt2 = 15.0f; // Default rate for evaporation rate. Assume a default rate of 15 lbs/sqft of evaporation area
            }
            BoilerEvapRateLbspFt2 = MathHelper.Clamp(BoilerEvapRateLbspFt2, 10.0f, 15.0f); // Clamp BoilerEvap Rate to between 10 & 15
            TheoreticalMaxSteamOutputLBpS = pS.FrompH(Me2.ToFt2(EvaporationAreaM2) * BoilerEvapRateLbspFt2 ); // set max boiler theoretical steam output

            MaxBoilerHeatPressurePSI = MaxBoilerPressurePSI + SafetyValveStartPSI + 5.0f; // set locomotive maximum boiler pressure to calculate max heat, allow for safety valve + a bit
            MaxBoilerPressHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerHeatPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerHeatPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerHeatPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerHeatPressurePSI];  // calculate the maximum possible heat in the boiler, assuming safety valve and a small margin
            MaxBoilerHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI];  // calculate the maximum possible heat in the boiler

            MaxBoilerKW = Kg.FromLb(TheoreticalMaxSteamOutputLBpS) * W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI]));
            MaxFlueTempK = (MaxBoilerKW / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 * HeatMaterialThicknessFactor)) + baseTempK;

            // Determine if Superheater in use
            if (SuperheatAreaM2 == 0) // If super heating area not specified
            {
                if (SuperheaterFactor > 1.0) // check if MSTS value, then set superheating
                {
                    HasSuperheater = true;
                    SuperheatRefTempF = 200.0f; // Assume a superheating temp of 250degF
                    SuperheatTempRatio = SuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];
                }
                else
                {
                    HasSuperheater = false;
                    SuperheatRefTempF = 0.0f;
                }
            }
            else  // if OR value set then calculate
            {
                
                HasSuperheater = true;

                // Calculate superheat steam reference temperature based upon heating area of superheater
                // SuperTemp = (SuperHeatArea x HeatTransmissionCoeff * (MeanGasTemp - MeanSteamTemp)) / (SteamQuantity * MeanSpecificSteamHeat)
                // Formula has been simplified as follows: SuperTemp = (SuperHeatArea x FlueTempK x SFactor) / SteamQuantity
                // SFactor is a "loose reprentation" =  (HeatTransmissionCoeff / MeanSpecificSteamHeat) - Av figure calculate by comparing a number of "known" units for superheat.
                SuperheatRefTempF = (Me2.ToFt2(SuperheatAreaM2) * C.ToF(C.FromK(MaxFlueTempK)) * SuperheatKFactor) / pS.TopH(TheoreticalMaxSteamOutputLBpS);
                SuperheatTempRatio = SuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];    // calculate a ratio figure for known value against reference curve.      
             
            }
            
            // Determine whether it is a geared locomotive
            if (SteamGearRatio > 1.0)
            {
            IsGearedSteamLoco = true;    // set flag for geared locomotive
            MotiveForceGearRatio = SteamGearRatio;
            // Calculate maximum locomotive speed - based upon the number of revs for the drive shaft, geared to wheel shaft, and then circumference of drive wheel
            // Max Geared speed = ((MaxPistonSpeed / Gear Ratio) x DrvWheelCircumference) / Feet in mile - miles per min
            MaxGearedSpeedMpS =   pS.FrompM(Me.FromMi(((MaxSteamGearPistonRateRpM / SteamGearRatio) * (float)Math.PI * Me.ToFt(DriverWheelRadiusM) * 2.0f) / FeetinMile));  
            }
            else
            {
            IsGearedSteamLoco = false;    // set flag for non-geared locomotive
            MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
            }            
                    
        #endregion

            // Cylinder Steam Usage = Cylinder Volume * Cutoff * No of Cylinder Strokes (based on loco speed, ie, distance travelled in period / Circumference of Drive Wheels)
            // SweptVolumeToTravelRatioFT3pFT is used to calculate the Cylinder Steam Usage Rate (see below)
            // SweptVolumeToTravelRatioFT3pFT = strokes_per_cycle * no_of_cylinders * pi*CylRad^2 * stroke_length / 2*pi*WheelRad
            // "pi"s cancel out
            
            // Cylinder piston shaft volume needs to be calculated and deducted from sweptvolume - assume diameter of the cylinder minus one-half of the piston-rod area. Let us assume that the latter is 3 square inches
            CylinderPistonShaftFt3 = Me2.ToFt2(Me2.FromIn2(((float)Math.PI * (CylinderPistonShaftDiaIn / 2.0f) * (CylinderPistonShaftDiaIn / 2.0f)) / 2.0f));
            CylinderPistonAreaFt2 = (float)Math.PI * (Me.ToFt(CylinderDiameterM / 2.0f) * Me.ToFt(CylinderDiameterM / 2.0f)); 
            CylinderSweptVolumeFT3pFT = ((CylinderPistonAreaFt2 * Me.ToFt(CylinderStrokeM)) - CylinderPistonShaftFt3);
            
            // Cylinder Steam Usage	= SweptVolumeToTravelRatioFT3pFT x cutoff x {(speed x (SteamDensity (CylPress) - SteamDensity (CylBackPress)) 
            // lbs/s                = ft3/ft                                  x   ft/s  x  lbs/ft3

            // The next two tables are the average over a full wheel rotation calculated using numeric integration
            // they depend on valve geometry and main rod length etc
            if (ForceFactor1Npcutoff == null)
            {
                ForceFactor1Npcutoff = new Interpolator(11);
                ForceFactor1Npcutoff[.200f] = -.428043f;
                ForceFactor1Npcutoff[.265f] = -.453624f;
                ForceFactor1Npcutoff[.330f] = -.479480f;
                ForceFactor1Npcutoff[.395f] = -.502123f;
                ForceFactor1Npcutoff[.460f] = -.519346f;
                ForceFactor1Npcutoff[.525f] = -.535572f;
                ForceFactor1Npcutoff[.590f] = -.550099f;
                ForceFactor1Npcutoff[.655f] = -.564719f;
                ForceFactor1Npcutoff[.720f] = -.579431f;
                ForceFactor1Npcutoff[.785f] = -.593737f;
                ForceFactor1Npcutoff[.850f] = -.607703f;
                ForceFactor1Npcutoff.ScaleY(4.4482f * (float)Math.PI / 4 * 39.372f * 39.372f * NumCylinders * CylinderDiameterM * CylinderDiameterM * CylinderStrokeM / (2 * DriverWheelRadiusM)); // Original formula
            }
            if (ForceFactor2Npcutoff == null)
            {
                ForceFactor2Npcutoff = new Interpolator(11);
                ForceFactor2Npcutoff[.200f] = .371714f;
                ForceFactor2Npcutoff[.265f] = .429217f;
                ForceFactor2Npcutoff[.330f] = .476195f;
                ForceFactor2Npcutoff[.395f] = .512149f;
                ForceFactor2Npcutoff[.460f] = .536852f;
                ForceFactor2Npcutoff[.525f] = .554344f;
                ForceFactor2Npcutoff[.590f] = .565618f;
                ForceFactor2Npcutoff[.655f] = .573383f;
                ForceFactor2Npcutoff[.720f] = .579257f;
                ForceFactor2Npcutoff[.785f] = .584714f;
                ForceFactor2Npcutoff[.850f] = .591967f;
                ForceFactor2Npcutoff.ScaleY(4.4482f * (float)Math.PI / 4 * 39.372f * 39.372f * NumCylinders * CylinderDiameterM * CylinderDiameterM * CylinderStrokeM / (2 * DriverWheelRadiusM)); // original Formula
            }
            if (CylinderPressureDropLBpStoPSI == null)
            {   // this table is not based on measurements
                CylinderPressureDropLBpStoPSI = new Interpolator(5);
                CylinderPressureDropLBpStoPSI[0] = 0;
                CylinderPressureDropLBpStoPSI[.2f] = 0;
                CylinderPressureDropLBpStoPSI[.5f] = 2;
                CylinderPressureDropLBpStoPSI[1] = 10;
                CylinderPressureDropLBpStoPSI[2] = 20;
                CylinderPressureDropLBpStoPSI.ScaleX(ExhaustLimitLBpH);
                CylinderPressureDropLBpStoPSI.ScaleX(1 / 3600f);
            }
            if (BackPressureLBpStoPSI == null)
            {   // this table is not based on measurements
                BackPressureLBpStoPSI = new Interpolator(3);
                BackPressureLBpStoPSI[0] = 0;
                BackPressureLBpStoPSI[1] = 10;
                BackPressureLBpStoPSI[1.2f] = 30;
                BackPressureLBpStoPSI.ScaleX(TheoreticalMaxSteamOutputLBpS);

            }

            // This is to model falling boiler efficiency as the combustion increases, based on a "crude" model, to be REDONE?
            if (BoilerEfficiency == null)
            {
                BoilerEfficiency = new Interpolator(4);
                BoilerEfficiency[0] = .82f;
                BoilerEfficiency[(1 - .82f) / .35f] = .82f;
                BoilerEfficiency[(1 - .4f) / .35f] = .4f;
                BoilerEfficiency[1 / .35f] = .4f;
            }

            // Based on the EvapArea, this section calculates the maximum boiler output in lbs/s, and also calculates the theoretical burn rate in kg/s to support it.
            // BurnRate creates a table with an x-axis of steam production in lb/s, and a y-axis calculating the coal burnt to support this production rate in lb/s.
            BurnRateLBpStoKGpS = new Interpolator(27);
            for (int i = 0; i < 27; i++)
            {
                float x = .1f * i;
                float y = x;
                if (y < .02)
                    y = .02f;
                else if (y > 2.5f)
                    y = 2.5f;
                BurnRateLBpStoKGpS[x] = y / BoilerEfficiency[x]; // To increase burnrate to compensate for a loss of energy, ie this is amount of coal that would need to compensate for the boiler efficiency
            }
            float sy = (MaxFlueTempK - baseTempK) * W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 / HeatMaterialThicknessFactor; // Boiler kWs
            float sx = sy / (W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI])));  // BoilerkW / (SteamHeat- in kJ)?
            BurnRateLBpStoKGpS.ScaleX(sx);  // Steam in lbs
            BurnRateLBpStoKGpS.ScaleY(sy / FuelCalorificKJpKG); // Original Formula - FuelBurnt KG = BoilerkW / FuelCalorific - Convert to equivalent kgs of coal
            BoilerEfficiency.ScaleX(sx); // Boiler Efficiency x axis - Steam in lbs
            MaxBoilerOutputLBpH = Kg.ToLb(pS.TopH(sx));
            BurnRateLBpStoKGpS.ScaleY(BurnRateMultiplier);

            // Calculate the maximum boiler heat input based on the steam generation rate
            MaxBoilerHeatInBTUpS = Kg.ToLb(BurnRateLBpStoKGpS[TheoreticalMaxSteamOutputLBpS]) * KJpKg.ToBTUpLb(FuelCalorificKJpKG) * BoilerEfficiency[TheoreticalMaxSteamOutputLBpS];

        #region Initialise Locomotive in a Hot or Cold Start Condition

            if (HotStart)
            {
                // Hot Start - set so that FlueTemp is at maximum, boilerpressure slightly below max
                BoilerPressurePSI = MaxBoilerPressurePSI - 5.0f;
                baseStartTempK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI]));
                BoilerStartkW = Kg.FromLb((BoilerPressurePSI / MaxBoilerPressurePSI) * TheoreticalMaxSteamOutputLBpS) * W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[BoilerPressurePSI])); // Given pressure is slightly less then max, this figure should be slightly less, ie reduce TheoreticalMaxSteamOutputLBpS, for the time being assume a ratio of bp to MaxBP
                FlueTempK = (BoilerStartkW / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 * HeatMaterialThicknessFactor)) + baseStartTempK;
                BoilerMassLB = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[BoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[BoilerPressurePSI];
                BoilerHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[BoilerPressurePSI] * WaterHeatPSItoBTUpLB[BoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[BoilerPressurePSI] * SteamHeatPSItoBTUpLB[BoilerPressurePSI];
                StartBoilerHeatBTU = BoilerHeatBTU;
            }
            else
            {
                // Cold Start - as per current
                BoilerPressurePSI = MaxBoilerPressurePSI * 0.66f; // Allow for cold start - start at 66% of max boiler pressure - check pressure value given heat in boiler????
                baseStartTempK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI]));
                BoilerStartkW = Kg.FromLb((BoilerPressurePSI / MaxBoilerPressurePSI) * TheoreticalMaxSteamOutputLBpS) * W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[BoilerPressurePSI]));
                FlueTempK = (BoilerStartkW / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 * HeatMaterialThicknessFactor)) + baseStartTempK;
                BoilerMassLB = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[BoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[BoilerPressurePSI];
                BoilerHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[BoilerPressurePSI] * WaterHeatPSItoBTUpLB[BoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[BoilerPressurePSI] * SteamHeatPSItoBTUpLB[BoilerPressurePSI];
            }

            DamperFactorManual = TheoreticalMaxSteamOutputLBpS / SpeedEquivMpS; // Calculate a factor for damper control that will vary with speed.
            BlowerSteamUsageFactor = .04f * MaxBoilerOutputLBpH / 3600 / MaxBoilerPressurePSI;
            WaterTempNewK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI])); // Initialise new boiler pressure
            FireMassKG = IdealFireMassKG;
            if (MaxFiringRateKGpS == 0)
                MaxFiringRateKGpS = 180 * MaxBoilerOutputLBpH / 775 / 3600 / 2.2046f;
                
            if (ORTSMaxFiringRateKGpS != 0)
                MaxFiringRateKGpS = ORTSMaxFiringRateKGpS; // If OR value present then use it 

            // Initialise Mechanical Stoker if present
            if ( Stoker == 1.0f)
            {
                StokerIsMechanical = true;
                MaxFiringRateKGpS = 2 * MaxFiringRateKGpS; // Temp allowance for mechanical stoker
            }
            MaxTheoreticalFiringRateKgpS = MaxFiringRateKGpS * 1.2f; // allow the fireman to overfuel for short periods of time 
        #endregion

            ApplyBoilerPressure();
        }

        /// <summary>
        /// Sets the coal level to maximum.
        /// </summary>
        public void RefillTenderWithCoal()
        {
            FuelController.CurrentValue = 1.0f;
        }

        /// <summary>
        /// Sets the water level to maximum.
        /// </summary>
        public void RefillTenderWithWater()
        {
            WaterController.CurrentValue = 1.0f;
        } 
       
        public static bool ZeroError(float v, string name, string wagFile)
        {
            if (v > 0)
                return false;
            Trace.TraceWarning("Steam engine value {1} must be defined and greater than zero in {0}", wagFile, name);
            return true;
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
				case "engine(numcylinders": NumCylinders = stf.ReadIntBlock(null); break;
                case "engine(cylinderstroke": CylinderStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(cylinderdiameter": CylinderDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(boilervolume": BoilerVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null); break;
                case "engine(maxboilerpressure": MaxBoilerPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(maxboileroutput": MaxBoilerOutputLBpH = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null); break;
                case "engine(exhaustlimit": ExhaustLimitLBpH = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null); break;
                case "engine(basicsteamusage": BasicSteamUsageLBpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 3600; break;
                case "engine(safetyvalvessteamusage": SafetyValveUsageLBpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 3600; break;
                case "engine(safetyvalvepressuredifference": SafetyValveDropPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(idealfiremass": IdealFireMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(shovelcoalmass": ShovelMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtendercoalmass": MaxTenderCoalMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtenderwatermass": MaxTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(steamfiremanmaxpossiblefiringrate": MaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(steamfiremanismechanicalstoker": Stoker = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssteamfiremanmaxpossiblefiringrate": ORTSMaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(enginecontrollers(cutoff": CutoffController.Parse(stf); break;
                case "engine(enginecontrollers(injector1water": Injector1Controller.Parse(stf); break;
                case "engine(enginecontrollers(injector2water": Injector2Controller.Parse(stf); break;
                case "engine(enginecontrollers(blower": BlowerController.Parse(stf); break;
                case "engine(enginecontrollers(dampersfront": DamperController.Parse(stf); break;
                case "engine(enginecontrollers(shovel": FiringRateController.Parse(stf); break;
                case "engine(enginecontrollers(firedoor": FireboxDoorController.Parse(stf); break;
                case "engine(effects(steamspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(ortsgratearea": GrateAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(superheater": SuperheaterFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsevaporationarea": EvaporationAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(ortssuperheatarea": SuperheatAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(ortsfuelcalorific": FuelCalorificKJpKG = stf.ReadFloatBlock(STFReader.UNITS.EnergyDensity, null); break;
                case "engine(ortsburnratemultiplier": BurnRateMultiplier = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsboilerevaporationrate": BoilerEvapRateLbspFt2 = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderefficiencyrate": CylinderEfficiencyRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsforcefactor1": ForceFactor1Npcutoff = new Interpolator(stf); break;
                case "engine(ortsforcefactor2": ForceFactor2Npcutoff = new Interpolator(stf); break;
                case "engine(ortscylinderpressuredrop": CylinderPressureDropLBpStoPSI = new Interpolator(stf); break;
                case "engine(ortsbackpressure": BackPressureLBpStoPSI = new Interpolator(stf); break;
                case "engine(ortsburnrate": BurnRateLBpStoKGpS = new Interpolator(stf); break;
                case "engine(ortsboilerefficiency": BoilerEfficiency = new Interpolator(stf); break;
                case "engine(ortsdrivewheelweight": DrvWheelWeightKg = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(ortssteamgearratio": SteamGearRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssteammaxgearpistonrate": MaxSteamGearPistonRateRpM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                default: base.Parse(lowercasetoken, stf); break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void InitializeFromCopy(MSTSWagon copy)
        {
            MSTSSteamLocomotive locoCopy = (MSTSSteamLocomotive)copy;
            NumCylinders = locoCopy.NumCylinders;
            CylinderStrokeM = locoCopy.CylinderStrokeM;
            CylinderDiameterM = locoCopy.CylinderDiameterM;
            BoilerVolumeFT3 = locoCopy.BoilerVolumeFT3;
            MaxBoilerPressurePSI = locoCopy.MaxBoilerPressurePSI; 
            MaxBoilerOutputLBpH = locoCopy.MaxBoilerOutputLBpH;
            ExhaustLimitLBpH = locoCopy.ExhaustLimitLBpH;
            BasicSteamUsageLBpS = locoCopy.BasicSteamUsageLBpS;
            SafetyValveUsageLBpS = locoCopy.SafetyValveUsageLBpS;
            SafetyValveDropPSI = locoCopy.SafetyValveDropPSI;
            IdealFireMassKG = locoCopy.IdealFireMassKG;
            ShovelMassKG = locoCopy.ShovelMassKG;
            MaxTenderCoalMassKG = locoCopy.MaxTenderCoalMassKG;
            MaxTenderWaterMassKG = locoCopy.MaxTenderWaterMassKG;
            ORTSMaxFiringRateKGpS = locoCopy.ORTSMaxFiringRateKGpS;
            Stoker = locoCopy.Stoker;
            MaxFiringRateKGpS = locoCopy.MaxFiringRateKGpS;
            CutoffController = (MSTSNotchController)locoCopy.CutoffController.Clone();
            Injector1Controller = (MSTSNotchController)locoCopy.Injector1Controller.Clone();
            Injector2Controller = (MSTSNotchController)locoCopy.Injector2Controller.Clone();
            BlowerController = (MSTSNotchController)locoCopy.BlowerController.Clone();
            DamperController = (MSTSNotchController)locoCopy.DamperController.Clone();
            FiringRateController = (MSTSNotchController)locoCopy.FiringRateController.Clone();
            FireboxDoorController = (MSTSNotchController)locoCopy.FireboxDoorController.Clone();
            GrateAreaM2 = locoCopy.GrateAreaM2;
            SuperheaterFactor = locoCopy.SuperheaterFactor;
            EvaporationAreaM2 = locoCopy.EvaporationAreaM2;
            SuperheatAreaM2 = locoCopy.SuperheatAreaM2;
            FuelCalorificKJpKG = locoCopy.FuelCalorificKJpKG;
            BurnRateMultiplier = locoCopy.BurnRateMultiplier;
            BoilerEvapRateLbspFt2 = locoCopy.BoilerEvapRateLbspFt2;
            CylinderEfficiencyRate = locoCopy.CylinderEfficiencyRate;
            ForceFactor1Npcutoff = new Interpolator(locoCopy.ForceFactor1Npcutoff);
            ForceFactor2Npcutoff = new Interpolator(locoCopy.ForceFactor2Npcutoff);
            CylinderPressureDropLBpStoPSI = new Interpolator(locoCopy.CylinderPressureDropLBpStoPSI);
            BackPressureLBpStoPSI = new Interpolator(locoCopy.BackPressureLBpStoPSI);
            BurnRateLBpStoKGpS = new Interpolator(locoCopy.BurnRateLBpStoKGpS);
            BoilerEfficiency = locoCopy.BoilerEfficiency;
            DrvWheelWeightKg = locoCopy.DrvWheelWeightKg;
            SteamGearRatio = locoCopy.SteamGearRatio;
            MaxSteamGearPistonRateRpM = locoCopy.MaxSteamGearPistonRateRpM;
            base.InitializeFromCopy(copy);  // each derived level initializes its own variables
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(BoilerHeatOutBTUpS);
            outf.Write(BoilerHeatInBTUpS); 
            outf.Write(TenderCoalMassLB);
            outf.Write(TenderWaterVolumeUKG);
            outf.Write(CylinderSteamUsageLBpS);
            outf.Write(BoilerHeatBTU);
            outf.Write(BoilerMassLB);
            outf.Write(BoilerPressurePSI);
            outf.Write(WaterFraction);
            outf.Write(EvaporationLBpS);
            outf.Write(FireMassKG);
            outf.Write(FlueTempK);
            ControllerFactory.Save(CutoffController, outf);
            ControllerFactory.Save(Injector1Controller, outf);
            ControllerFactory.Save(Injector2Controller, outf);
            ControllerFactory.Save(BlowerController, outf);
            ControllerFactory.Save(DamperController, outf);
            ControllerFactory.Save(FireboxDoorController, outf);
            ControllerFactory.Save(FiringRateController, outf);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
		public override void Restore(BinaryReader inf)
        {
            BoilerHeatOutBTUpS = inf.ReadSingle();
            BoilerHeatInBTUpS = inf.ReadSingle(); 
            TenderCoalMassLB = inf.ReadSingle();
            TenderWaterVolumeUKG = inf.ReadSingle();
            CylinderSteamUsageLBpS = inf.ReadSingle();
            BoilerHeatBTU = inf.ReadSingle();
            BoilerMassLB = inf.ReadSingle();
            BoilerPressurePSI = inf.ReadSingle();
            WaterFraction = inf.ReadSingle();
            EvaporationLBpS = inf.ReadSingle();
            FireMassKG = inf.ReadSingle();
            FlueTempK = inf.ReadSingle();
            CutoffController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            Injector1Controller = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            Injector2Controller = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            BlowerController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            DamperController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            FireboxDoorController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            FiringRateController = (MSTSNotchController)ControllerFactory.Restore(Simulator, inf);
            base.Restore(inf);
        }

        /// <summary>
        /// Create a viewer for this locomotive.   Viewers are only attached
        /// while the locomotive is in viewing range.
        /// </summary>
        public override TrainCarViewer GetViewer(Viewer viewer)
        {
            return new MSTSSteamLocomotiveViewer( viewer, this );
        }
        /// <summary>
        /// Sets controler settings from other engine for cab switch
        /// </summary>
        /// <param name="other"></param>
        public override void CopyControllerSettings(TrainCar other)
        {
            base.CopyControllerSettings(other);
            if (CutoffController != null)
                CutoffController.SetValue(Train.MUReverserPercent / 100);
        }
 
        // +++++++++++++++++++++ Main Simulation - Start ++++++++++++++++++++++++++++++++
        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            PowerOn = true;
            UpdateControllers(elapsedClockSeconds);
            base.Update(elapsedClockSeconds);

#if INDIVIDUAL_CONTROL
			//this train is remote controlled, with mine as a helper, so I need to send the controlling information, but not the force.
			if (MultiPlayer.MPManager.IsMultiPlayer() && this.Train.TrainType == Train.TRAINTYPE.REMOTE && this == Program.Simulator.PlayerLocomotive)
			{
				if (CutoffController.UpdateValue != 0.0 || BlowerController.UpdateValue != 0.0 || DamperController.UpdateValue != 0.0 || FiringRateController.UpdateValue != 0.0 || Injector1Controller.UpdateValue != 0.0 || Injector2Controller.UpdateValue != 0.0)
				{
					controlUpdated = true;
				}
				Train.MUReverserPercent = CutoffController.Update(elapsedClockSeconds) * 100.0f;
				if (Train.MUReverserPercent >= 0)
					Train.MUDirection = Direction.Forward;
				else
					Train.MUDirection = Direction.Reverse;
				return; //done, will go back and send the message to the remote train controller
			}

			if (MultiPlayer.MPManager.IsMultiPlayer() && this.notificationReceived == true)
			{
				Train.MUReverserPercent = CutoffController.CurrentValue * 100.0f;
				if (Train.MUReverserPercent >= 0)
					Train.MUDirection = Direction.Forward;
				else
					Train.MUDirection = Direction.Reverse;
			}
#endif 
            Variable1 = (Simulator.UseAdvancedAdhesion ? LocomotiveAxle.AxleSpeedMpS : SpeedMpS) / DriverWheelRadiusM; // Unit is [rad/s]. Value of 6.28 means 1 rotation/second.
            Variable2 = Math.Min(CylinderPressurePSI / MaxBoilerPressurePSI * 100f, 100f);
            Variable3 = FiringIsManual ? FiringRateController.CurrentValue * 100 : FuelRate.SmoothedValue * 100;

            throttle = ThrottlePercent / 100;
            cutoff = Math.Abs(Train.MUReverserPercent / 100);
            if (cutoff > ForceFactor2Npcutoff.MaxX())
                cutoff = ForceFactor2Npcutoff.MaxX();
            float absSpeedMpS = Math.Abs(Train.SpeedMpS);
            if (absSpeedMpS > 2 && (Train.MUReverserPercent == 100 || Train.MUReverserPercent == -100))
            {   // AI cutoff adjustment logic, also used for steam MU'd with non-steam
                cutoff = throttle * ForceFactor2Npcutoff.MaxX() * 2 / absSpeedMpS;
                float min = ForceFactor2Npcutoff.MinX();
                if (cutoff < min)
                {
                    throttle = cutoff / min;
                    cutoff = min;
                }
                else
                    throttle = 1;
            }
            #region transfer energy
            UpdateTender(elapsedClockSeconds);
            UpdateFirebox(elapsedClockSeconds, absSpeedMpS);
            UpdateBoiler(elapsedClockSeconds);
            UpdateCylinders(elapsedClockSeconds, throttle, cutoff, absSpeedMpS);
            UpdateMotion(elapsedClockSeconds, cutoff, absSpeedMpS);
            UpdateMotiveForce(elapsedClockSeconds, t, currentSpeedMpS, currentWheelSpeedMpS);
            UpdateAuxiliaries(elapsedClockSeconds, absSpeedMpS);
            #endregion

            #region adjust state
            UpdateWaterGauge();
            UpdateInjectors(elapsedClockSeconds);
            UpdateFiring(absSpeedMpS);
            #endregion
        }

        private void UpdateControllers(float elapsedClockSeconds)
        {
            if (this.IsLeadLocomotive())
            {
                Train.MUReverserPercent = CutoffController.Update(elapsedClockSeconds) * 100.0f;
                Direction = Train.MUReverserPercent >= 0 ? Direction.Forward : Direction.Reverse;
            }
            else
                CutoffController.Update(elapsedClockSeconds);

            if (CutoffController.UpdateValue != 0.0)
                // On a steam locomotive, the Reverser is the same as the Cut Off Control.
                switch (Direction)
                {
                    case Direction.Reverse: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.Off); break;
                    case Direction.N: Simulator.Confirmer.Confirm(CabControl.SteamLocomotiveReverser, CabSetting.Neutral); break;
                    case Direction.Forward: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.On); break;
                }
            if (BlowerController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Increase, BlowerController.CurrentValue * 100);
            if (BlowerController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Decrease, BlowerController.CurrentValue * 100);
            if (DamperController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Increase, DamperController.CurrentValue * 100);
            if (DamperController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Decrease, DamperController.CurrentValue * 100);
            if (FireboxDoorController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Increase, FireboxDoorController.CurrentValue * 100);
            if (FireboxDoorController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Decrease, FireboxDoorController.CurrentValue * 100);
            if (FiringRateController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Increase, FiringRateController.CurrentValue * 100);
            if (FiringRateController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Decrease, FiringRateController.CurrentValue * 100);

            Injector1Controller.Update(elapsedClockSeconds);
            if (Injector1Controller.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector1, CabSetting.Increase, Injector1Controller.CurrentValue * 100);
            if (Injector1Controller.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector1, CabSetting.Decrease, Injector1Controller.CurrentValue * 100);
            Injector2Controller.Update(elapsedClockSeconds);
            if (Injector2Controller.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector2, CabSetting.Increase, Injector2Controller.CurrentValue * 100);
            if (Injector2Controller.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector2, CabSetting.Decrease, Injector2Controller.CurrentValue * 100);

            BlowerController.Update(elapsedClockSeconds);
            if (BlowerController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Increase, BlowerController.CurrentValue * 100);
            if (BlowerController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Decrease, BlowerController.CurrentValue * 100);

            DamperController.Update(elapsedClockSeconds);
            if (DamperController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Increase, DamperController.CurrentValue * 100);
            if (DamperController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Decrease, DamperController.CurrentValue * 100);
            FiringRateController.Update(elapsedClockSeconds);
            if (FiringRateController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Increase, FiringRateController.CurrentValue * 100);
            if (FiringRateController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Decrease, FiringRateController.CurrentValue * 100);

            var oldFireboxDoorValue = FireboxDoorController.CurrentValue;
            FireboxDoorController.Update(elapsedClockSeconds);
            if (FireboxDoorController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Increase, FireboxDoorController.CurrentValue * 100);
            if (FireboxDoorController.UpdateValue < 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Decrease, FireboxDoorController.CurrentValue * 100);
            if (oldFireboxDoorValue == 0 && FireboxDoorController.CurrentValue > 0)
                SignalEvent(Event.FireboxDoorOpen);
            else if (oldFireboxDoorValue > 0 && FireboxDoorController.CurrentValue == 0)
                SignalEvent(Event.FireboxDoorClose);

            FuelController.Update(elapsedClockSeconds);
            if (FuelController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.TenderCoal, CabSetting.Increase, FuelController.CurrentValue * 100);
            WaterController.Update(elapsedClockSeconds);
            if (WaterController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.TenderWater, CabSetting.Increase, WaterController.CurrentValue * 100);
        }

        private void UpdateTender(float elapsedClockSeconds)
        {
            TenderCoalMassLB -= elapsedClockSeconds * FuelBurnRateLBpS; // Current Tender coal mass determined by burn rate.
            TenderCoalMassLB = MathHelper.Clamp(TenderCoalMassLB, 0, Kg.ToLb(MaxTenderCoalMassKG)); // Clamp value so that it doesn't go out of bounds
            if (TenderCoalMassLB < 1.0)
            {
                if (!CoalIsExhausted)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Tender coal supply is empty. Your loco will fail.");
                }
                CoalIsExhausted = true;
            }
            else
            {
                CoalIsExhausted = false;
            }
            TenderWaterVolumeUKG -= InjectorBoilerInputLB / WaterLBpUKG; // Current water volume determined by injector input rate
            TenderWaterVolumeUKG = MathHelper.Clamp(TenderWaterVolumeUKG, 0, (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG)); // Clamp value so that it doesn't go out of bounds
            if (TenderWaterVolumeUKG < 1.0)
            {
                if (!WaterIsExhausted)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Tender water supply is empty. Your loco will fail.");
                }
                WaterIsExhausted = true;
            }
            else
            {
                WaterIsExhausted = false;
            }
        }

        private void UpdateFirebox(float elapsedClockSeconds, float absSpeedMpS)
        {
            if (!FiringIsManual && !HotStart)  // if loco is started cold, and is not moving then the blower may be needed to heat loco up.
            {
                if (absSpeedMpS < 1.0f)    // locomotive is stationary then blower can heat fire
                {
                    BlowerIsOn = true;  // turn blower on if being used
                    BlowerSteamUsageLBpS = BlowerSteamUsageFactor * BlowerController.CurrentValue * BoilerPressurePSI;
                    BlowerBurnEffect = ManBlowerMultiplier * BlowerSteamUsageFactor * BlowerController.CurrentValue * BoilerPressurePSI;
                }
                else
                {
                    BlowerBurnEffect = 0.0f;
                    BlowerIsOn = false;
                }
            }
            // Adjust burn rates for firing in either manual or AI mode
            if (FiringIsManual)
            {
                BurnRateRawLBpS = Kg.ToLb(BurnRateLBpStoKGpS[(RadiationSteamLossLBpS) + BlowerBurnEffect + DamperBurnEffect]); // Manual Firing - note steam usage due to safety valve, compressor and steam cock operation not included, as these are factored into firemans calculations, and will be adjusted for manually - Radiation loss divided by factor of 5.0 to reduce the base level - Manual fireman to compensate as appropriate.
            }
            else
            {
                if (PreviousTotalSteamUsageLBpS > TheoreticalMaxSteamOutputLBpS)
                {
                    FiringSteamUsageRateLBpS = TheoreticalMaxSteamOutputLBpS; // hold usage rate if steam usage rate exceeds boiler max output
                }
                else
                {
                    FiringSteamUsageRateLBpS = PreviousTotalSteamUsageLBpS;
                }
                // burnrate will be the radiation loss @ rest & then related to heat usage as a factor of the maximum boiler output
                BurnRateRawLBpS = Kg.ToLb(BurnRateLBpStoKGpS[(BlowerBurnEffect + HeatRatio * FiringSteamUsageRateLBpS * PressureRatio * BoilerHeatRatio * MaxBoilerHeatRatio)]); // Original

                //  Limit burn rate in AI fireman to within acceptable range of Fireman firing rate
                BurnRateRawLBpS = MathHelper.Clamp(BurnRateRawLBpS, 0.05f, Kg.ToLb(MaxFiringRateKGpS) * 1.2f); // Allow burnrate to max out at 1.2 x max firing rate
            }

            FuelFeedRateLBpS = BurnRateRawLBpS;
            if (FireMassKG < 25) // If fire level drops too far 
            {
                BurnRateRawLBpS = 0.0f; // If fire is no longer effective set burn rate to zero, change later to allow graduate ramp down
                if (!FireIsExhausted)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Fire has dropped too far. Your loco will fail.");
                    FireIsExhausted = true; // fire has run out of fuel.
                }
            }
            if (FusiblePlugIsBlown)
            {
            BurnRateRawLBpS = 0.0f; // Drop fire due to melting of fusible plug and steam quenching fire, change later to allow graduate ramp down.
            }
            
            FireRatio = FireMassKG / IdealFireMassKG;
            if (absSpeedMpS == 0)
            {
                BurnRateRawLBpS *= FireRatio * 0.2f; // reduce background burnrate if stationary
                // <CJComment> Correct version commented out. Needs fixing. </CJComment>
                //BurnRateSmoothLBpS.Update(elapsedClockSeconds, BurnRateRawLBpS);
                BurnRateSmoothLBpS.Update(0.1f, BurnRateRawLBpS); // Smooth the burn rate
                FuelBurnRateLBpS = BurnRateSmoothLBpS.SmoothedValue;
            }
            else if (FireRatio < 1.0f)  // maximise burnrate when FireMass = IdealFireMass, else allow a reduction in efficiency
            {
                BurnRateRawLBpS *= FireRatio;
                // <CJComment> Correct version commented out. Needs fixing. </CJComment>
                //BurnRateSmoothLBpS.Update(elapsedClockSeconds, BurnRateRawLBpS);
                BurnRateSmoothLBpS.Update(0.1f, BurnRateRawLBpS); // Smooth the burn rate
                FuelBurnRateLBpS = BurnRateSmoothLBpS.SmoothedValue;
            }
            else
            {
                BurnRateRawLBpS *= 2 - FireRatio;
                // <CJComment> Correct version commented out. Needs fixing. </CJComment>
                //BurnRateSmoothLBpS.Update(elapsedClockSeconds, BurnRateRawLBpS); // Smooth the burn rate
                BurnRateSmoothLBpS.Update(0.1f, BurnRateRawLBpS); // Smooth the burn rate
                FuelBurnRateLBpS = BurnRateSmoothLBpS.SmoothedValue;
            }
            FuelBurnRateLBpS = MathHelper.Clamp(FuelBurnRateLBpS, 0, MaxFireMassKG); // clamp burnrate to maintain it within limits

            if (FiringIsManual)
            {
                // If tender coal is empty stop fuelrate (feeding coal onto fire).  
                if (CoalIsExhausted)
                {
                    FuelFeedRateLBpS = 0.0f; // set fuel rate to zero if tender empty
                    DesiredChange = 0.0f;
                    FireMassKG -= elapsedClockSeconds * Kg.FromLb(FuelBurnRateLBpS); // Firemass will only decrease if tender coal is empty
                }
                else
                {
                    FuelFeedRateLBpS = Kg.ToLb(MaxFiringRateKGpS) * FiringRateController.CurrentValue;
                    FireMassKG += elapsedClockSeconds * (MaxFiringRateKGpS * FiringRateController.CurrentValue - Kg.FromLb(FuelBurnRateLBpS));
                }
            }
            else if (elapsedClockSeconds > 0.001 && MaxFiringRateKGpS > 0.001)
            {
                // Automatic fireman, ish.
                DesiredChange = MathHelper.Clamp(((IdealFireMassKG - FireMassKG) + Kg.FromLb(FuelBurnRateLBpS)) / MaxFiringRateKGpS, 0.001f, 1);
                if (StokerIsMechanical) // if a stoker is fitted expect a quicker response to fuel feeding
                {
                    FuelRateStokerLBpS.Update(elapsedClockSeconds, DesiredChange); // faster fuel feed rate for stoker    
                    FuelRateSmoothLBpS = FuelRateStokerLBpS.SmoothedValue;
                }
                else
                {
                    FuelRate.Update(elapsedClockSeconds, DesiredChange); // slower fuel feed rate for fireman
                    FuelRateSmoothLBpS = FuelRate.SmoothedValue;
                }
                // If tender coal is empty stop fuelrate (feeding coal onto fire).  
                if ((IdealFireMassKG - FireMassKG) > 20.0) // if firemass is falling too low shovel harder - needs further refinement as this shouldn't be able to be maintained indefinitely
                {
                    if (FuelBoostOnTimerS < TimeFuelBoostOnS) // If fuel boost timer still has time available allow fuel boost
                    {
                        FuelBoostResetTimerS = 0.01f;     // Reset fuel reset (time out) timer to allow stop boosting for a period of time.
                        if (!FuelBoost)
                        {
                            FuelBoost = true; // boost shoveling 
                            if (!StokerIsMechanical)  // Don't display message if stoker in operation
                            {
                                Simulator.Confirmer.Message(ConfirmLevel.Warning, "FireMass is getting low. Your fireman will shovel faster, but don't wear him out.");
                            }
                        }
                    }
                }
                else if ((IdealFireMassKG - FireMassKG) < 1.0)
                {
                    FuelBoost = false;
                    if (FuelBoost)
                    {
                        FuelBoost = false; // disable boost shoveling 
                        if (!StokerIsMechanical)  // Don't display message if stoker in operation
                        {
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, "FireMass is back within limits. Your fireman will shovel as per normal.");
                        }
                    }
                }
                if (CoalIsExhausted)
                {
                    FuelFeedRateLBpS = 0.0f; // set fuel rate to zero if tender empty
                    FireMassKG -= elapsedClockSeconds * Kg.FromLb(FuelBurnRateLBpS); // Firemass will only decrease if tender coal is empty
                }
                else
                {
                    if (FuelBoost && !FuelBoostReset) // if firemass is falling too low, shovel harder - needs further refinement as this shouldn't be able to be maintained indefinitely
                    {
                        FuelFeedRateLBpS = Kg.ToLb(MaxTheoreticalFiringRateKgpS) * FuelRateSmoothLBpS;  // At times of heavy burning allow AI fireman to overfuel
                        FireMassKG += elapsedClockSeconds * (MaxTheoreticalFiringRateKgpS * FuelRateSmoothLBpS - Kg.FromLb(FuelBurnRateLBpS));
                        FuelBoostOnTimerS += elapsedClockSeconds; // Time how long to fuel boost for
                    }
                    else
                    {
                        FuelFeedRateLBpS = Kg.ToLb(MaxFiringRateKGpS) * FuelRateSmoothLBpS;
                        FireMassKG += elapsedClockSeconds * (MaxFiringRateKGpS * FuelRateSmoothLBpS - Kg.FromLb(FuelBurnRateLBpS));
                    }
                }                  
            }
            FireMassKG = MathHelper.Clamp(FireMassKG, 0, MaxFireMassKG);
            GrateCombustionRateLBpFt2 = FuelBurnRateLBpS / Me2.ToFt2(GrateAreaM2); //coal burnt per sq ft grate area
            // Time Fuel Boost reset time if all time has been used up on boost timer
            if (FuelBoostOnTimerS >= TimeFuelBoostOnS)
            {
                FuelBoostResetTimerS += elapsedClockSeconds; // Time how long to wait for next fuel boost
                FuelBoostReset = true;
            }
            if (FuelBoostResetTimerS > TimeFuelBoostResetS)
            {
                FuelBoostOnTimerS = 0.01f;     // Reset fuel boost timer to allow another boost if required.
                FuelBoostReset = false;
            }
            Smoke.Update(elapsedClockSeconds, FuelFeedRateLBpS / FuelBurnRateLBpS);
        }

        private void UpdateBoiler(float elapsedClockSeconds)
        {
            absSpeedMpS = Math.Abs(Train.SpeedMpS);
            
            // Determine number and size of safety valves
            // Reference: Ashton's POP Safety valves catalogue
            // To calculate size use - Total diam of safety valve = 0.036 x ( H / (L x P), where H = heat area of boiler sq ft (not including superheater), L = valve lift (assume 0.1 in for Ashton valves), P = Abs pressure psi (gauge pressure + atmospheric)
            
            const float ValveSizeCalculationFactor = 0.036f;
            const float ValveLiftIn = 0.1f;
            float ValveSizeTotalDiaIn = ValveSizeCalculationFactor * ( Me2.ToFt2(EvaporationAreaM2) / (ValveLiftIn * (MaxBoilerPressurePSI + OneAtmospherePSI)));
              
            ValveSizeTotalDiaIn += 1.0f; // Add safety margin to align with Ashton size selection table
            
            // There will always be at least two safety valves to allow for a possible failure. There may be up to four fitted to a locomotive depending upon the size of the heating area. Therefore allow for combinations of 2x, 3x or 4x.
            // Common valve sizes are 2.5", 3", 3.5" and 4".
            
            // Test for 2x combinations
            float TestValveSizeTotalDiaIn = ValveSizeTotalDiaIn / 2.0f;
            if (TestValveSizeTotalDiaIn <= 4.0f)
            {
            NumSafetyValves = 2.0f;   // Assume that there are 2 safety valves
            if ( TestValveSizeTotalDiaIn <= 2.5)
            {
            SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
            {
            SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
            {
            SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
            {
            SafetyValveSizeIn = 4.0f; // Safety valve is a 4.0" diameter unit
            }
            }
            else
            {
            TestValveSizeTotalDiaIn = ValveSizeTotalDiaIn / 3.0f;
            // Test for 3x combinations
            if (TestValveSizeTotalDiaIn <= 4.0f)
            {
            NumSafetyValves = 3.0f;   // Assume that there are 3 safety valves
            if ( TestValveSizeTotalDiaIn <= 2.5)
            {
            SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
            {
            SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
            {
            SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
            {
            SafetyValveSizeIn = 4.0f; // Safety valve is a 4.0" diameter unit
            }
            }
            else
            {
            TestValveSizeTotalDiaIn = ValveSizeTotalDiaIn / 4.0f;
            // Test for 4x combinations
            if (TestValveSizeTotalDiaIn <= 4.0f)
            {
            NumSafetyValves = 4.0f;   // Assume that there are 4 safety valves
            if ( TestValveSizeTotalDiaIn <= 2.5)
            {
            SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
            {
            SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
            {
            SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
            }
            if ( TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
            {
            SafetyValveSizeIn = 4.0f; // Safety valve is a 4.0" diameter unit
            }
            }
            else
            {
            // Else set at maximum default value
            NumSafetyValves = 4.0f;   // Assume that there are 4 safety valves
            SafetyValveSizeIn = 4.0f; // Safety valve is a 4.0" diameter unit
            }
            }
            }
            
            // Steam Discharge Rates
            // Use Napier formula to calculate steam discharge rate through safety valve, ie Discharge (lb/s) = (Valve area * Abs Pressure) / 70
            // Set "valve area" of safety valve, based on reverse enginnered values of steam, valve area is determined by lift and the gap created 
            const float SafetyValveDischargeFactor = 70.0f;
            if (SafetyValveSizeIn == 2.5f)
            {
                SafetyValveSizeDiaIn2 = 0.610369021f;
            }
            else
            {
                if (SafetyValveSizeIn == 3.0f)
                {
                    SafetyValveSizeDiaIn2 = 0.799264656f;
                }
                else
                {
                    if (SafetyValveSizeIn == 3.5f)
                    {
                        SafetyValveSizeDiaIn2 = 0.932672199f;
                    }
                    else
                    {
                        if (SafetyValveSizeIn == 4.0f)
                        {
                            SafetyValveSizeDiaIn2 = 0.977534912f;
                        }
                    }
                }
            }
            
            // For display purposes calculate the maximum steam discharge with all safety valves open
            MaxSafetyValveDischargeLbspS = NumSafetyValves * (SafetyValveSizeDiaIn2 * (MaxBoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor;
            // Set open pressure and close pressures for safety valves.
            float SafetyValveOpen1Psi = MaxBoilerPressurePSI;
            float SafetyValveClose1Psi = MaxBoilerPressurePSI - 4.0f;
            float SafetyValveOpen2Psi = MaxBoilerPressurePSI + 2.0f;
            float SafetyValveClose2Psi = MaxBoilerPressurePSI - 3.0f;
            float SafetyValveOpen3Psi = MaxBoilerPressurePSI + 4.0f;
            float SafetyValveClose3Psi = MaxBoilerPressurePSI - 2.0f;
            float SafetyValveOpen4Psi = MaxBoilerPressurePSI + 6.0f;
            float SafetyValveClose4Psi = MaxBoilerPressurePSI - 1.0f;
            
            // Safety Valve
            if (BoilerPressurePSI > MaxBoilerPressurePSI + SafetyValveStartPSI)
            {
                if (!SafetyIsOn)
                {
                    SignalEvent(Event.SteamSafetyValveOn);
                    SafetyIsOn = true;
                }
            }
            else if (BoilerPressurePSI < MaxBoilerPressurePSI - SafetyValveDropPSI)
            {
                if (SafetyIsOn)
                {
                    SignalEvent(Event.SteamSafetyValveOff);
                    SafetyIsOn = false;
                    SafetyValveUsage1LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
            }
            if (SafetyIsOn)
            {
               // Determine how many safety valves are in operation and set Safety Valve discharge rate
                SafetyValveUsageLBpS = 0.0f;  // Set to zero initially
                
                // Calculate rate for safety valve 1
                SafetyValveUsage1LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is above open value then set rate
                               
                // Calculate rate for safety valve 2
                if (BoilerPressurePSI > SafetyValveOpen2Psi)
                {
                safety2IsOn = true; // turn safey 2 on
                SafetyValveUsage2LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is above open value then set rate
                }
                else
                {
                if (BoilerPressurePSI < SafetyValveClose1Psi)
                {
                safety2IsOn = false; // turn safey 2 off
                SafetyValveUsage2LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                else
                {
                if (safety2IsOn)
                {
                SafetyValveUsage2LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is between open and close values, set rate
                }
                else
                {
                SafetyValveUsage2LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                }
                }
                
                
                // Calculate rate for safety valve 3
                if (BoilerPressurePSI > SafetyValveOpen3Psi)
                {
                safety3IsOn = true; // turn safey 3 on
                SafetyValveUsage3LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is above open value then set rate
                }
                else
                {
                if (BoilerPressurePSI < SafetyValveClose3Psi)
                {
                safety3IsOn = false; // turn safey 3 off
                SafetyValveUsage3LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                else
                {
                if (safety3IsOn)
                {
                SafetyValveUsage3LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is between open and close values, set rate
                }
                else
                {
                SafetyValveUsage3LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                }
                }
                
                
                // Calculate rate for safety valve 4
                if (BoilerPressurePSI > SafetyValveOpen4Psi)
                {
                safety4IsOn = true; // turn safey 4 on
                SafetyValveUsage4LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is above open value then set rate
                }
                else
                {
                if (BoilerPressurePSI < SafetyValveClose4Psi)
                {
                safety4IsOn = false; // turn safey 4 off
                SafetyValveUsage4LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                else
                {
                if (safety4IsOn)
                {
                SafetyValveUsage4LBpS = (SafetyValveSizeDiaIn2 * (BoilerPressurePSI + OneAtmospherePSI)) / SafetyValveDischargeFactor; // If safety valve is between open and close values, set rate
                }
                else
                {
                SafetyValveUsage4LBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }
                }
                }
                
                
                SafetyValveUsageLBpS = SafetyValveUsage1LBpS + SafetyValveUsage2LBpS + SafetyValveUsage3LBpS + SafetyValveUsage4LBpS;   // Sum all the safety valve discharge rates together
                BoilerMassLB -= elapsedClockSeconds * SafetyValveUsageLBpS;
                BoilerHeatBTU -= elapsedClockSeconds * SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve
                TotalSteamUsageLBpS += SafetyValveUsageLBpS;
                BoilerHeatOutBTUpS += SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve
            }
            else
            {
                SafetyValveUsageLBpS = 0.0f;
            }

            // Adjust blower impacts on heat and boiler mass
            if (BlowerIsOn)
            {
                BoilerMassLB -= elapsedClockSeconds * BlowerSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by blower  
                BoilerHeatBTU -= elapsedClockSeconds * BlowerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by blower
                BoilerHeatOutBTUpS += BlowerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by blower
                TotalSteamUsageLBpS += BlowerSteamUsageLBpS;
            }
            BoilerWaterTempK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI]));

            if (FlueTempK < BoilerWaterTempK)
            {
                FlueTempK = BoilerWaterTempK + 10.0f;  // Ensure that flue temp is greater then water temp, so that you don't get negative steam production
            }
            // Heat transferred per unit time (W or J/s) = (Heat Txf Coeff (W/m2K) * Heat Txf Area (m2) * Temp Difference (K)) / Material Thickness - in this instance the material thickness is a means of increasing the boiler output - convection heat formula.
            // Heat transfer Coefficient for Locomotive Boiler = 45.0 Wm^2K            
            BoilerKW = (FlueTempK - BoilerWaterTempK) * W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 / HeatMaterialThicknessFactor;
            if (GrateCombustionRateLBpFt2 > GrateLimitLBpS)
            {
                FireHeatTxfKW = PreviousFireHeatTxfKW; // if greater then grate limit don't allow any more heat txf
            }
            else
            {
                FireHeatTxfKW = Kg.FromLb(FuelBurnRateLBpS) * FuelCalorificKJpKG * BoilerEfficiencyGrateAreaLBpFT2toX[FuelBurnRateLBpS] / (SpecificHeatCoalKJpKGpK * FireMassKG); // Current heat txf based on fire burning rate  
            }

            PreviousFireHeatTxfKW = FireHeatTxfKW; // store the last value of FireHeatTxfKW
            
            FlueTempDiffK = ((BoilerHeatInBTUpS - BoilerHeatOutBTUpS) * BTUpHtoKJpS) / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2); // calculate current FlueTempK difference, based upon heat input due to firing - heat taken out by boiler

            FlueTempK += elapsedClockSeconds * FlueTempDiffK; // Calculate increase or decrease in Flue Temp

            FlueTempK = MathHelper.Clamp(FlueTempK, 0, 3000.0f);    // Maximum firebox temp in Penn document = 1514 K.

            if (FusiblePlugIsBlown)
            {
                EvaporationLBpS = 0.0333f;   // if fusible plug is blown drop steam output of boiler.
            }
            else
            {
                // Steam Output (kg/h) = ( Boiler Rating (kW) * 3600 s/h ) / Energy added kJ/kg, Energy added = energy (at Boiler Pressure - Feedwater energy)
                // Allow a small increase if superheater is installed
                EvaporationLBpS = Kg.ToLb(BoilerKW / W.ToKW(W.FromBTUpS(BoilerSteamHeatBTUpLB)));  // convert kW,  1kW = 0.94781712 BTU/s - fudge factor required - 1.1
            }

            // Cap Steam Generation rate if excessive
            //EvaporationLBpS = MathHelper.Clamp(EvaporationLBpS, 0, TheoreticalMaxSteamOutputLBpS); // If steam generation is too high, then cap at max theoretical rule of thumb

            PreviousBoilerHeatInBTU = BoilerHeatBTU;

            if (!FiringIsManual)
            {

                if (BoilerHeatBTU > MaxBoilerHeatBTU) // Limit boiler heat to max value for the boiler
                {
                    BoilerHeat = true;
                    const float BoilerHeatFactor = 1.025f; // Increasing this factor will change the burn rate once boiler heat has been reached
                    float FactorPower = BoilerHeatBTU / (MaxBoilerHeatBTU / BoilerHeatFactor); 
                    BoilerHeatRatio = MaxBoilerHeatBTU / (float)Math.Pow(BoilerHeatBTU, FactorPower);
                }
                else
                {
                    BoilerHeat = false;
                    BoilerHeatRatio = 1.0f;
                }
                BoilerHeatRatio = MathHelper.Clamp(BoilerHeatRatio, 0.0f, 1.0f); // Keep Boiler Heat ratio within bounds
                if (BoilerHeatBTU > MaxBoilerPressHeatBTU)
                {
                    float FactorPower = BoilerHeatBTU / (MaxBoilerPressHeatBTU - MaxBoilerHeatBTU);
                    MaxBoilerHeatRatio = MaxBoilerHeatBTU / (float)Math.Pow(BoilerHeatBTU, FactorPower); 
                }
                else
                {
                    MaxBoilerHeatRatio = 1.0f;
                }
                MaxBoilerHeatRatio = MathHelper.Clamp(MaxBoilerHeatRatio, 0.0f, 1.0f); // Keep Max Boiler Heat ratio within bounds
            }

            BoilerHeatInBTUpS = FuelBurnRateLBpS * KJpKg.ToBTUpLb(FuelCalorificKJpKG) * BoilerEfficiencyGrateAreaLBpFT2toX[FuelBurnRateLBpS];
            BoilerHeatBTU += elapsedClockSeconds * FuelBurnRateLBpS * KJpKg.ToBTUpLb(FuelCalorificKJpKG) * BoilerEfficiencyGrateAreaLBpFT2toX[FuelBurnRateLBpS];

            // Basic steam radiation losses 
            RadiationSteamLossLBpS = pS.FrompM((absSpeedMpS == 0.0f) ?
                3.04f : // lb/min at rest 
                5.29f); // lb/min moving
            BoilerMassLB -= elapsedClockSeconds * RadiationSteamLossLBpS;
            BoilerHeatBTU -= elapsedClockSeconds * RadiationSteamLossLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
            TotalSteamUsageLBpS += RadiationSteamLossLBpS;
            BoilerHeatOutBTUpS += RadiationSteamLossLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);

            // Recalculate the fraction of the boiler containing water (the rest contains saturated steam)
            // The derivation of the WaterFraction equation is not obvious, but starts from:
            // Mb = Mw + Ms, Vb = Vw + Vs and (Vb - Vw)/Vs = 1 where Mb is mass in boiler, Vw is the volume of water etc. and Vw/Vb is the WaterFraction.
            // We can say:
            //                Mw = Mb - Ms
            //                Mw = Mb - Ms x (Vb - Vw)/Vs
            //      Mw - MsVw/Vs = Mb - MsVb/Vs
            // Vw(Mw/Vw - Ms/Vs) = Vb(Mb/Vb - Ms/Vs)
            //             Vw/Vb = (Mb/Vb - Ms/Vs)/(Mw/Vw - Ms/Vs)
            // If density Dx = Mx/Vx, we can write:
            //             Vw/Vb = (Mb/Vb - Ds)/Dw - Ds)
            WaterFraction = ((BoilerMassLB / BoilerVolumeFT3) - BoilerSteamDensityLBpFT3) / (BoilerWaterDensityLBpFT3 - BoilerSteamDensityLBpFT3);
            
            // Update Boiler Heat based upon current Evaporation rate
            // Based on formula - BoilerCapacity (btu/h) = (SteamEnthalpy (btu/lb) - EnthalpyCondensate (btu/lb) ) x SteamEvaporated (lb/h) ?????
            // EnthalpyWater (btu/lb) = BoilerCapacity (btu/h) / SteamEvaporated (lb/h) + Enthalpysteam (btu/lb)  ?????

            //<CJComment> Correct statement commented out. Needs sorting. </CJComment>
            //BoilerHeatSmoothBTU.Update(elapsedClockSeconds, BoilerHeatBTU);
            BoilerHeatSmoothBTU.Update(0.1f, BoilerHeatBTU);

            WaterHeatBTUpFT3 = (BoilerHeatSmoothBTU.Value / BoilerVolumeFT3 - (1 - WaterFraction) * BoilerSteamDensityLBpFT3 * BoilerSteamHeatBTUpLB) / (WaterFraction * BoilerWaterDensityLBpFT3);

            #region Boiler Pressure calculation
            // works on the principle that boiler pressure will go up or down based on the change in water temperature, which is impacted by the heat gain or loss to the boiler
            WaterVolL = WaterFraction * BoilerVolumeFT3 * 28.31f;   // idealy should be equal to water flow in and out. 1ft3 = 28.31 litres of water
            BkW_Diff = (((BoilerHeatInBTUpS - BoilerHeatOutBTUpS) * 3600) * 0.0002931f);            // Calculate difference in boiler rating, ie heat in - heat out - 1 BTU = 0.0002931 kWh, divide by 3600????
            SpecificHeatWaterKJpKGpC = SpecificHeatKtoKJpKGpK[WaterTempNewK] * WaterVolL;  // Spec Heat = kj/kg, litres = kgs of water
            WaterTempIN = BkW_Diff / SpecificHeatWaterKJpKGpC;   // calculate water temp variation
            WaterTempNewK += elapsedClockSeconds * WaterTempIN; // Calculate new water temp
            WaterTempNewK = MathHelper.Clamp(WaterTempNewK, 274.0f, 496.0f);
            if (FusiblePlugIsBlown)
            {
            BoilerPressurePSI = 5.0f; // Drop boiler pressure if fusible plug melts.
            }
            else
            {
            BoilerPressurePSI = SaturationPressureKtoPSI[WaterTempNewK]; // Gauge Pressure
            }
            
            if (!FiringIsManual)
            {
                if (BoilerHeat)
                {
                    PressureRatio = MathHelper.Clamp((MaxBoilerPressurePSI / BoilerPressurePSI), 0.01f, 0.99f); // Boiler pressure ratio to adjust burn rate, if maxboiler heat reached, then clamp ratio < 1.0
                }
                else
                {
                    PressureRatio = MathHelper.Clamp((MaxBoilerPressurePSI / BoilerPressurePSI), 0.01f, 1.2f); // Boiler pressure ratio to adjust burn rate
                }
            }
            #endregion

            if (!FiringIsManual && BoilerPressurePSI > MaxBoilerPressurePSI) // For AI fireman stop excessive pressure
            {
                BoilerPressurePSI = MaxBoilerPressurePSI;  // Check for AI firing
            }
            if (FiringIsManual && BoilerPressurePSI > MaxBoilerPressurePSI + 10) // For manual fireman stop excessive pressure
            {
                BoilerPressurePSI = MaxBoilerPressurePSI + 10.0f;  // Check for manual firing
            }

            ApplyBoilerPressure();
        }

        private void ApplyBoilerPressure()
        {
            BoilerSteamHeatBTUpLB = SteamHeatPSItoBTUpLB[BoilerPressurePSI];
            BoilerWaterHeatBTUpLB = WaterHeatPSItoBTUpLB[BoilerPressurePSI];
            BoilerSteamDensityLBpFT3 = SteamDensityPSItoLBpFT3[BoilerPressurePSI];
            BoilerWaterDensityLBpFT3 = WaterDensityPSItoLBpFT3[BoilerPressurePSI];

            // Save values for use in UpdateFiring() and HUD
            PreviousBoilerHeatOutBTUpS = BoilerHeatOutBTUpS;
            PreviousTotalSteamUsageLBpS = TotalSteamUsageLBpS;
            // Reset for next pass
            BoilerHeatOutBTUpS = 0.0f;
            TotalSteamUsageLBpS = 0.0f;
        }

        private void UpdateCylinders(float elapsedClockSeconds, float throttle, float cutoff, float absSpeedMpS)
        {
            // Calculate speed of locomotive in wheel rpm - used to determine changes in performance based upon speed.
            DrvWheelRevRpS = Me.ToFt(absSpeedMpS) / (2.0f * (float)Math.PI * Me.ToFt(DriverWheelRadiusM));
            // Determine if Superheater in use
            if (HasSuperheater)
            {
                CurrentSuperheatTeampF = SuperheatTempLbpHtoDegF[pS.TopH(CylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate current superheat temp
                float DifferenceSuperheatTeampF = CurrentSuperheatTeampF - SuperheatTempLimitXtoDegF[cutoff]; // reduce superheat temp due to cylinder condensation
                SuperheatVolumeRatio = 1.0f + (0.0015f * DifferenceSuperheatTeampF); // Based on formula Vsup = Vsat ( 1 + 0.0015 Tsup) - Tsup temperature at superheated level
                // look ahead to see what impact superheat will have on cylinder usage
                float FutureCylinderSteamUsageLBpS = CylinderSteamUsageLBpS * 1.0f / SuperheatVolumeRatio; // Calculate potential future new cylinder steam usage
                float FutureSuperheatTeampF = SuperheatTempLbpHtoDegF[pS.TopH(FutureCylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate potential future new superheat temp
                

                if (CurrentSuperheatTeampF > SuperheatTempLimitXtoDegF[cutoff])
                {
                  if (FutureSuperheatTeampF < SuperheatTempLimitXtoDegF[cutoff])
                  {
                  SuperheaterSteamUsageFactor = 1.0f; // Superheating has minimal impact as all heat has been lost in the steam, so no steam reduction is achieved, but condensation has stopped
                  }
                  else
                  {
                  SuperheaterSteamUsageFactor = 1.0f / SuperheatVolumeRatio; // set steam reduction based on Superheat Volume Ratio
                  }
                }
                else
                {
                    SuperheaterSteamUsageFactor = 1.0f + CylinderCondensationFractionX[cutoff]; // calculate steam usage factor for superheated steam locomotive when superheat tem is not high enough to stop condensation 
                }
            }
            else
            {
                SuperheaterSteamUsageFactor = 1.0f + CylinderCondensationFractionX[cutoff]; // calculate steam usage factor for saturated steam locomotive according to cylinder condensation fraction 
            }
             // Calculate Ratio of expansion, with cylinder clearance
            // R (ratio of Expansion) = (length of stroke + clearance) / (length of stroke to point of cut-on + clearance)
            // Expressed as a fraction of stroke R = (1 + c) / (cutoff + c)
            RatioOfExpansion = (1.0f + CylinderClearancePC) / (cutoff + CylinderClearancePC);
            // Absolute Mean Pressure = Ratio of Expansion
            SteamChestPressurePSI = (throttle * SteamChestPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest
            // Initial pressure will be decreased depending upon locomotive speed
                InitialPressurePSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.
           
            
            MeanPressureStrokePSI = InitialPressurePSI * (cutoff + ((cutoff + CylinderClearancePC) * (float)Math.Log(RatioOfExpansion)));
                    
           // mean pressure during stroke = ((absolute mean pressure + (clearance + cylstroke)) - (initial pressure + clearance)) / cylstroke
           // Mean effective pressure = cylpressure - backpressure
           // Cylinder pressure also reduced by steam vented through cylinder cocks.
            CylCockPressReduceFactor = 1.0f;

            if (CylinderCocksAreOpen) // Don't apply steam cocks derate until Cylinder steam usage starts to work
            {
                if (HasSuperheater)
                {
                    CylCockPressReduceFactor = ((CylinderSteamUsageLBpS / SuperheaterSteamUsageFactor) / ((CylinderSteamUsageLBpS / SuperheaterSteamUsageFactor) + CylCockSteamUsageLBpS)); // For superheated locomotives temp convert back to a saturated comparison for calculation of steam cock reduction factor.
                }
                else
                {
                    CylCockPressReduceFactor = (CylinderSteamUsageLBpS / (CylinderSteamUsageLBpS + CylCockSteamUsageLBpS)); // Saturated steam locomotive
                }
                CylinderPressurePSI = MeanPressureStrokePSI - (MeanEffectivePressurePSI * (1.0f - CylCockPressReduceFactor)); // Allow for pressure reduction due to Cylinder cocks being open.
            }
            else
            {
                    CylinderPressurePSI = MeanPressureStrokePSI;
            }

            CylinderPressurePSI = MathHelper.Clamp(CylinderPressurePSI, 0, MaxBoilerPressurePSI + OneAtmospherePSI); // Make sure that Cylinder pressure does not go negative
            BackPressurePSI = BackPressureLBpStoPSI[CylinderSteamUsageLBpS - CylCockSteamUsageLBpS];
            
            MeanBackPressurePSI = (BackPressurePSI + OneAtmospherePSI) * ((1.0f - CylinderCompressionPC) + ((CylinderCompressionPC + CylinderClearancePC) * (float)Math.Log((CylinderCompressionPC + CylinderClearancePC) / CylinderClearancePC)));
            MeanEffectivePressurePSI = (MeanPressureStrokePSI - MeanBackPressurePSI) * CutoffPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)];
            MeanEffectivePressurePSI = MathHelper.Clamp(MeanEffectivePressurePSI, 0, MaxBoilerPressurePSI); // Make sure that Cylinder pressure does not go negative
            // Calculate PV const at cutoff, and then the terminal pressure at the end of cylinder stroke.
            CylinderPressureVolumeCutoffFactor = InitialPressurePSI * cutoff * CylinderStrokeM; // Pressure doesn't need to be in absolute, as steam density figures appear to be in gauge pressure.
            CylinderExhaustPressurePSI = CylinderPressureVolumeCutoffFactor / (CylinderStrokeM * CylinderExhaustPC);
            // Cylinder steam usage = (volume of steam in cylinder @ cutoff value (MEP)) * number of cylinder strokes based on speed - assume 2 stroke per wheel rev per cylinder. Note CylinderSteamDensity is in lbs/ft3
            // Calculate Cylinder compression pressure, and increases inversley proportional to drop in initial pressure, and with speed of locomotive
            // CylinderCompressionPressurePSI = 0.25f * InitialPressurePSI * (1.0f / (CylinderCompressionPressureFactor * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]));
            float CylinderCompressionPressureFactor = 0.2f; // factor to increase cpmnpresion pressure by as lcomotive goes faster
            CylinderCompressionPressurePSI = 0.1f * InitialPressurePSI * (1.0f / (CylinderCompressionPressureFactor * CutoffPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]));

            float CalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * ((CylinderSweptVolumeFT3pFT * CylinderSteamDensityPSItoLBpFT3[CylinderExhaustPressurePSI]) - (2.0f * CylinderClearancePC * CylinderSweptVolumeFT3pFT * CylinderSteamDensityPSItoLBpFT3[CylinderCompressionPressurePSI])) * SuperheaterSteamUsageFactor;

            if (throttle < 0.01 && absSpeedMpS > 0.1) // If locomotive moving and throttle set to close, then reduce steam usage.
            {
                CalculatedCylinderSteamUsageLBpS = 0.3f; // Set steam usage to a small value if throttle is closed
            }
                                
            // usage calculated as moving average to minimize chance of oscillation.
            // Decrease steam usage by SuperheaterUsage factor to model superheater - very crude model - to be improved upon
            CylinderSteamUsageLBpS = (0.6f * CylinderSteamUsageLBpS + 0.4f * CalculatedCylinderSteamUsageLBpS);

            BoilerMassLB -= elapsedClockSeconds * CylinderSteamUsageLBpS; //  Boiler mass will be reduced by cylinder steam usage
            BoilerHeatBTU -= elapsedClockSeconds * CylinderSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); //  Boiler Heat will be reduced by heat required to replace the cylinder steam usage, ie create steam from hot water. 
            TotalSteamUsageLBpS += CylinderSteamUsageLBpS;
            BoilerHeatOutBTUpS += CylinderSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
        }

        private void UpdateMotion(float elapsedClockSeconds, float cutoff, float absSpeedMpS)
        {
           // Caculate the piston speed
           // Piston Speed (Ft p Min) = (Stroke length x 2) x (Ft in Mile x Train Speed (mph) / ( Circum of Drv Wheel x 60))
            PistonSpeedFtpM = (2.0f * Me.ToFt(CylinderStrokeM)) * ((FeetinMile * MpS.ToMpH(absSpeedMpS)) / ((2.0f * (float)Math.PI * Me.ToFt(DriverWheelRadiusM)) * 60.0f) );
            CylinderEfficiencyRate = MathHelper.Clamp(CylinderEfficiencyRate, 0.6f, 1.2f); // Clamp Cylinder Efficiency Rate to between 0.6 & 1.2
            TractiveEffortLbsF = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MeanEffectivePressurePSI * CylinderEfficiencyRate;
            TractiveEffortLbsF = MathHelper.Clamp(TractiveEffortLbsF, 0, TractiveEffortLbsF);
                      
            // Calculate IHP
            // IHP = (MEP x CylStroke(ft) x cylArea(sq in) x No Strokes (/min)) / 33000) - this is per cylinder
            IndicatedHorsePowerHP = NumCylinders * ((MeanEffectivePressurePSI * Me.ToFt(CylinderStrokeM) *  Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * pS.TopM(DrvWheelRevRpS) * CylStrokesPerCycle / 33000.0f));
       
            // DHP = (Tractive Effort x velocity) / 550.0 - velocity in ft-sec
            // DrawbarHorsePowerHP = (TractiveEffortLbsF * Me.ToFt(absSpeedMpS)) / 550.0f;  // TE in this instance is a maximum, and not at the wheel???

            DrawBarPullLbsF = -1.0f * N.ToLbf(CouplerForceU);
            DrawbarHorsePowerHP = -1.0f * (N.ToLbf(CouplerForceU) * Me.ToFt(absSpeedMpS)) / 550.0f;  // TE in this instance is a maximum, and not at the wheel???


            if (IsGearedSteamLoco)
            {
                if (absSpeedMpS > MaxGearedSpeedMpS)
                {
                    if (!IsGearedSpeedExcess)
                        IsGearedSpeedExcess = true;     // set excess speed flag
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "You are exceeding the maximum recommended speed. Continued operation at this speed may cause damage.");
                }
                else
                {
                    if (absSpeedMpS < MaxGearedSpeedMpS - 1.0)
                        IsGearedSpeedExcess = false;     // reset excess speed flag
                }
                
            }


            MotiveForceSmoothedN.Update(elapsedClockSeconds, MotiveForceN);
            if (float.IsNaN(MotiveForceN))
                MotiveForceN = 0;
            switch (this.Train.TrainType)
            {
                case Train.TRAINTYPE.AI:
                case Train.TRAINTYPE.STATIC:
                    break;
                case Train.TRAINTYPE.PLAYER:
                case Train.TRAINTYPE.REMOTE:
                    LimitMotiveForce(elapsedClockSeconds);
                    break;
                default:
                    break;
            }

            if (absSpeedMpS == 0 && cutoff < 0.3f)
                MotiveForceN = 0;   // valves assumed to be closed

        }

        protected override void UpdateMotiveForce(float elapsedClockSeconds, float t, float currentSpeedMpS, float currentWheelSpeedMpS)
        {
            // Pass force and power information to MSTSLocomotive file by overriding corresponding method there
         
            // Calculate maximum power of the locomotive, based upon the maximum IHP
            // Maximum IHP will occur at different (piston) speed for saturated locomotives and superheated based upon the wheel revolution. Typically saturated locomotive produce maximum power @ a piston speed of 700 ft/min , and superheated will occur @ 1000ft/min
            // Set values for piston speed

	        if ( HasSuperheater)
	        {
	            MaxPistonSpeedFtpM = 1000.0f; // if superheated locomotive
                SpeedFactor = 0.445f;
	        }
	        else
	        {
	            MaxPistonSpeedFtpM = 700.0f;  // if saturated locomotive
                SpeedFactor = 0.412f;
	        }

           // Calculate max velocity of the locomotive based upon above piston speed

            MaxLocoSpeedMpH = MaxPistonSpeedFtpM * ((2.0f * (float)Math.PI * Me.ToFt(DriverWheelRadiusM)) * 60.0f) / (FeetinMile * (2.0f * Me.ToFt(CylinderStrokeM)));

            const float TractiveEffortFactor = 0.85f;
            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor;

            // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE

            MaxIndicatedHorsePowerHP = SpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;
            
            // Set Max Power equal to max IHP
            MaxPowerW = W.FromHp(MaxIndicatedHorsePowerHP);
             
            // Set "current" motive force based upon the throttle, clinders, steam pressure, etc	
            MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * N.FromLbf(TractiveEffortLbsF * MotiveForceGearRatio);

            // Set maximum force for the locomotive
            MaxForceN = N.FromLbf(MaxTractiveEffortLbf * CylinderEfficiencyRate * MotiveForceGearRatio);

            // On starting allow maximum motive force to be used
            if (absSpeedMpS < 1.0f && cutoff > 0.70f && throttle > 0.98f)
            {
                MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * N.FromLbf(MaxForceN);
            }
            // If "critical" speed of locomotive is reached, limit max IHP
            CriticalSpeedTractiveEffortLbf = (MaxIndicatedHorsePowerHP * 375.0f) / MaxLocoSpeedMpH;
            // Based upon max IHP, limit motive force.
            if (absSpeedMpS > pS.FrompH(Me.FromMi(MaxLocoSpeedMpH)))
            {
                IndicatedHorsePowerHP = MaxIndicatedHorsePowerHP; // Set IHP to maximum value
                if(TractiveEffortLbsF > CriticalSpeedTractiveEffortLbf)
                {
                    MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * N.FromLbf(CriticalSpeedTractiveEffortLbf * CylinderEfficiencyRate);
                }
            }
            // Derate when priming is occurring.
            if (BoilerIsPriming)
                MotiveForceN *= BoilerPrimingDeratingFactor;
            // Find the maximum TE for debug i.e. @ start and full throttle
            if (absSpeedMpS < 2.0)
            {
                if (MotiveForceN > StartTractiveEffortN)
                {
                    StartTractiveEffortN = MotiveForceN; // update to new maximum TE
                }
            }
        }

        protected override float GetSteamLocoMechFrictN()
        {
            // Calculate steam locomotive mechanical friction value, ie 20 (or 98.0667 metric) x DrvWheelWeight x Valve Factor, Assume VF = 1
            // If DrvWheelWeight is not in ENG file, then calculate from Factor of Adhesion(FoA) = DrvWheelWeight / Start (Max) Tractive Effort, assume FoA = 4.0

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                const float FactorofAdhesion = 4.0f; // Assume a typical factor of adhesion
                DrvWheelWeightKg = Kg.FromLb(FactorofAdhesion * MaxTractiveEffortLbf); // calculate Drive wheel weight if not in ENG file
            }  
              
            const float MetricTonneFromKg = 1000.0f;    // Conversion factor to convert from kg to tonnes
            return 98.0667f * (DrvWheelWeightKg / MetricTonneFromKg);
        }

        private void UpdateAuxiliaries(float elapsedClockSeconds, float absSpeedMpS)
        {
            // Calculate Air Compressor steam Usage if turned on
            if (CompressorIsOn)
            {
                CompSteamUsageLBpS = Me3.ToFt3(Me3.FromIn3((float)Math.PI * (CompCylDiaIN / 2.0f) * (CompCylDiaIN / 2.0f) * CompCylStrokeIN * pS.FrompM(CompStrokespM))) * SteamDensityPSItoLBpFT3[BoilerPressurePSI];   // Calculate Compressor steam usage - equivalent to volume of compressor steam cylinder * steam denisty * cylinder strokes
                BoilerMassLB -= elapsedClockSeconds * CompSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by compressor
                BoilerHeatBTU -= elapsedClockSeconds * CompSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor
                BoilerHeatOutBTUpS += CompSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor

                TotalSteamUsageLBpS += CompSteamUsageLBpS;
            }
            else
            {
                CompSteamUsageLBpS = 0.0f;    // Set steam usage to zero if compressor is turned off
            }
            // Calculate cylinder cock steam Usage if turned on
            // The cock steam usage will be assumed equivalent to a steam orifice
            // Steam Flow (lb/hr) = 24.24 x Press(Cylinder + Atmosphere(psi)) x CockDia^2 (in) - this needs to be multiplied by Num Cyls
            if (CylinderCocksAreOpen == true)
            {
                if (throttle > 0.02) // if regulator open
                {
                    CylCockSteamUsageLBpS = pS.FrompH(NumCylinders * (24.24f * (CylinderPressurePSI + OneAtmospherePSI) * CylCockDiaIN * CylCockDiaIN));
                    BoilerMassLB -= elapsedClockSeconds * CylCockSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by cylinder steam cocks  
                    BoilerHeatBTU -= elapsedClockSeconds * CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    BoilerHeatOutBTUpS += CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks                
                    TotalSteamUsageLBpS += CylCockSteamUsageLBpS;
                }
                else
                {
                    CylCockSteamUsageLBpS = 0.0f; // set usage to zero if regulator closed
                }
            }
            else
            {
                CylCockSteamUsageLBpS = 0.0f;       // set steam usage to zero if turned off
            }
            //<CJComment> What if there is no electricity generator? </CJComment>
            // Calculate Generator steam Usage if turned on
            // Assume generator kW = 350W for D50 Class locomotive
            if (absSpeedMpS > 2.0f) //  Turn generator on if moving
            {
                GeneratorSteamUsageLBpS = 0.0291666f; // Assume 105lb/hr steam usage for 500W generator
            //   GeneratorSteamUsageLbpS = (GeneratorSizekW * SteamkwToBTUpS) / steamHeatCurrentBTUpLb; // calculate Generator steam usage
                BoilerMassLB -= elapsedClockSeconds * GeneratorSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by generator  
                BoilerHeatBTU -= elapsedClockSeconds * GeneratorSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by generator
                BoilerHeatOutBTUpS += GeneratorSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by generator
                TotalSteamUsageLBpS += GeneratorSteamUsageLBpS;
            }
            else
            {
                GeneratorSteamUsageLBpS = 0.0f;
            }
            if (StokerIsMechanical)
            {
                StokerSteamUsageLBpS = pS.FrompH(MaxBoilerOutputLBpH) * (StokerMinUsage + (((StokerMaxUsage - StokerMinUsage) / Kg.ToLb(MaxFiringRateKGpS)) * FuelFeedRateLBpS));  // Caluculate current steam usage based on fuel feed rates
                BoilerMassLB -= elapsedClockSeconds * StokerSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by mechanical stoker  
                BoilerHeatBTU -= elapsedClockSeconds * StokerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by mechanical stoker
                BoilerHeatOutBTUpS += StokerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by mecahnical stoker
                TotalSteamUsageLBpS += StokerSteamUsageLBpS;
            }
            // Other Aux device usage??
        }

        private void UpdateWaterGauge()
        {
            WaterGlassLevelIN = ((WaterFraction - WaterGlassMinLevel) / (WaterGlassMaxLevel - WaterGlassMinLevel)) * WaterGlassLengthIN;
            WaterGlassLevelIN = MathHelper.Clamp(WaterGlassLevelIN, 0, WaterGlassLengthIN);

            if (WaterFraction < 0.7f)
            {
                if (!FusiblePlugIsBlown)
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Water level dropped too far. Plug has fused and loco has failed.");
                FusiblePlugIsBlown = true; // if water level has dropped, then fusible plug will blow , see "water model"
            }
            // Check for priming            
            if (WaterFraction >= 0.91f)
            {
                if (!BoilerIsPriming)
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Boiler overfull and priming.");
                BoilerIsPriming = true;
            }
            else if (WaterFraction < 0.90f)
            {
                if (BoilerIsPriming)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, "Boiler no longer priming.");
                BoilerIsPriming = false;
            }
        }

        private void UpdateInjectors(float elapsedClockSeconds)
        {
            // Calculate size of injectors to suit cylinder size.
            InjCylEquivSizeIN = (NumCylinders / 2.0f) * Me.ToIn(CylinderDiameterM);

            // Based on equiv cyl size determine correct size injector
            if (InjCylEquivSizeIN <= 19.0)
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector09FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 9mm Injector Flow rate 
                InjectorSize = 09.0f; // store size for display in HUD
            }
            else if (InjCylEquivSizeIN <= 24.0)
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector10FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 10 mm Injector Flow rate 
                InjectorSize = 10.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 26.0)
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector11FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 11 mm Injector Flow rate 
                InjectorSize = 11.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 28.0)
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector13FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 13 mm Injector Flow rate 
                InjectorSize = 13.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 30.0)
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector14FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 14 mm Injector Flow rate 
                InjectorSize = 14.0f; // store size for display in HUD                
            }
            else
            {
                InjectorFlowRateLBpS = pS.FrompM(Injector15FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 15 mm Injector Flow rate 
                InjectorSize = 15.0f; // store size for display in HUD                
            }
            if (WaterIsExhausted)
            {
                InjectorFlowRateLBpS = 0.0f; // If the tender water is empty, stop flow into boiler
            }

            InjectorBoilerInputLB = 0; // Used by UpdateTender() later in the cycle
            if (WaterIsExhausted)
            {
                // don't fill boiler with injectors
            }
            else
            {
                // Injectors to fill boiler   
                if (Injector1IsOn)
                {
                    // Calculate Injector 1 delivery water temp
                    if (Injector1Fraction < InjCapMinFactorX[BoilerPressurePSI])
                    {
                        Injector1WaterDelTempF = InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI]; // set water delivery temp to minimum value
                    }
                    else
                    {
                        Injector1TempFraction = (Injector1Fraction - InjCapMinFactorX[BoilerPressurePSI]) / (1 - InjCapMinFactorX[MaxBoilerPressurePSI]); // Find the fraction above minimum value
                        Injector1WaterDelTempF = InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI] - ((InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI] - InjDelWaterTempMaxPressureFtoPSI[BoilerPressurePSI]) * Injector1TempFraction);
                        Injector1WaterDelTempF = MathHelper.Clamp(Injector1WaterDelTempF, 65.0f, 500.0f);
                    }

                    Injector1WaterTempPressurePSI = WaterTempFtoPSI[Injector1WaterDelTempF]; // calculate the pressure of the delivery water

                    // Calculate amount of steam used to inject water
                    MaxInject1SteamUsedLbpS = InjWaterFedSteamPressureFtoPSI[BoilerPressurePSI];  // Maximum amount of steam used at boiler pressure
                    ActInject1SteamUsedLbpS = (Injector1Fraction * InjectorFlowRateLBpS) / MaxInject1SteamUsedLbpS; // Lbs of steam injected into boiler to inject water.

                    // Calculate heat loss for steam injection
                    Inject1SteamHeatLossBTU = ActInject1SteamUsedLbpS * (BoilerSteamHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector1WaterTempPressurePSI]); // Calculate heat loss for injection steam, ie steam heat to water delivery temperature

                    // Calculate heat loss for water injected
                    Inject1WaterHeatLossBTU = Injector1Fraction * InjectorFlowRateLBpS * (BoilerWaterHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector1WaterTempPressurePSI]); // Loss of boiler heat due to water injection - loss is the diff between steam and water Heat

                    // calculate Water steam heat based on injector water delivery temp
                    BoilerMassLB += elapsedClockSeconds * Injector1Fraction * InjectorFlowRateLBpS;   // Boiler Mass increase by Injector 1
                    BoilerHeatBTU -= elapsedClockSeconds * (Inject1WaterHeatLossBTU + Inject1SteamHeatLossBTU); // Total loss of boiler heat due to water injection - inject steam and water Heat   
                    InjectorBoilerInputLB += (elapsedClockSeconds * Injector1Fraction * InjectorFlowRateLBpS); // Keep track of water flow into boilers from Injector 1
                    BoilerHeatOutBTUpS += (Inject1WaterHeatLossBTU + Inject1SteamHeatLossBTU); // Total loss of boiler heat due to water injection - inject steam and water Heat
                }
                if (Injector2IsOn)
                {
                    // Calculate Injector 2 delivery water temp
                    if (Injector2Fraction < InjCapMinFactorX[BoilerPressurePSI])
                    {
                        Injector2WaterDelTempF = InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI]; // set water delivery temp to minimum value
                    }
                    else
                    {
                        Injector2TempFraction = (Injector2Fraction - InjCapMinFactorX[BoilerPressurePSI]) / (1 - InjCapMinFactorX[MaxBoilerPressurePSI]); // Find the fraction above minimum value
                        Injector2WaterDelTempF = InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI] - ((InjDelWaterTempMinPressureFtoPSI[BoilerPressurePSI] - InjDelWaterTempMaxPressureFtoPSI[BoilerPressurePSI]) * Injector2TempFraction);
                        Injector2WaterDelTempF = MathHelper.Clamp(Injector2WaterDelTempF, 65.0f, 500.0f);
                    }
                    Injector2WaterTempPressurePSI = WaterTempFtoPSI[Injector2WaterDelTempF]; // calculate the pressure of the delivery water

                    // Calculate amount of steam used to inject water
                    MaxInject2SteamUsedLbpS = InjWaterFedSteamPressureFtoPSI[BoilerPressurePSI];  // Maximum amount of steam used at boiler pressure
                    ActInject2SteamUsedLbpS = (Injector2Fraction * InjectorFlowRateLBpS) / MaxInject2SteamUsedLbpS; // Lbs of steam injected into boiler to inject water.

                    // Calculate heat loss for steam injection
                    Inject2SteamHeatLossBTU = ActInject2SteamUsedLbpS * (BoilerSteamHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector2WaterTempPressurePSI]); // Calculate heat loss for injection steam, ie steam heat to water delivery temperature

                    // Calculate heat loss for water injected
                    Inject2WaterHeatLossBTU = Injector2Fraction * InjectorFlowRateLBpS * (BoilerWaterHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector2WaterTempPressurePSI]); // Loss of boiler heat due to water injection - loss is the diff between steam and water Heat

                    // calculate Water steam heat based on injector water delivery temp
                    BoilerMassLB += elapsedClockSeconds * Injector2Fraction * InjectorFlowRateLBpS;   // Boiler Mass increase by Injector 1
                    BoilerHeatBTU -= elapsedClockSeconds * (Inject2WaterHeatLossBTU + Inject2SteamHeatLossBTU); // Total loss of boiler heat due to water injection - inject steam and water Heat   
                    InjectorBoilerInputLB += (elapsedClockSeconds * Injector2Fraction * InjectorFlowRateLBpS); // Keep track of water flow into boilers from Injector 1
                    BoilerHeatOutBTUpS += (Inject2WaterHeatLossBTU + Inject2SteamHeatLossBTU); // Total loss of boiler heat due to water injection - inject steam and water Heat
                }
            }
        }

        private void UpdateFiring(float absSpeedMpS)
        {
            if (FiringIsManual)
            {
                // Test to see if blower has been manually activiated.
                if (BlowerController.CurrentValue > 0.0f)
                {
                    BlowerIsOn = true;  // turn blower on if being used
                    BlowerSteamUsageLBpS = BlowerSteamUsageFactor * BlowerController.CurrentValue * BoilerPressurePSI;
                    BlowerBurnEffect = ManBlowerMultiplier * BlowerSteamUsageFactor * BlowerController.CurrentValue * BoilerPressurePSI;
                }
                else
                {
                    BlowerIsOn = false;  // turn blower off if not being used
                    BlowerSteamUsageLBpS = 0.0f;
                    BlowerBurnEffect = 0.0f;
                }
                if (Injector1IsOn)
                {
                    Injector1Fraction = Injector1Controller.CurrentValue;
                }
                if (Injector2IsOn)
                {
                    Injector2Fraction = Injector2Controller.CurrentValue;
                }
                // Damper
                if (absSpeedMpS < 1.0f)    // locomotive is stationary then damper will have no effect
                {
                    DamperBurnEffect = 0.0f;
                }
                else
                {
                    DamperBurnEffect = DamperController.CurrentValue * absSpeedMpS * DamperFactorManual; // Damper value for manual firing - related to damper setting and increased speed
                }
                DamperBurnEffect = MathHelper.Clamp(DamperBurnEffect, 0.0f, TheoreticalMaxSteamOutputLBpS); // set damper maximum to the max generation rate
            }
            else

            #region AI Fireman
            {
                // Injectors
                // Injectors normally not on when stationary?
                // Injector water delivery heat decreases with the capacity of the injectors, therefore cycle injectors on evenly across both.
                if (WaterGlassLevelIN > 7.99)        // turn injectors off if water level in boiler greater then 8.0, to stop cycling
                {
                    Injector1IsOn = false;
                    Injector1Fraction = 0.0f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                }
                else if (WaterGlassLevelIN <= 7.0 & WaterGlassLevelIN > 6.75)  // turn injector 1 on 20% if water level in boiler drops below 7.0
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.2f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                }
                else if (WaterGlassLevelIN <= 6.75 & WaterGlassLevelIN > 6.5) 
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.2f;
                    Injector2IsOn = true;
                    Injector2Fraction = 0.2f;
                }
                else if (WaterGlassLevelIN <= 6.5 & WaterGlassLevelIN > 6.25)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.4f;
                    Injector2IsOn = true;
                    Injector2Fraction = 0.2f;
                }
                else if (WaterGlassLevelIN <= 6.25 & WaterGlassLevelIN > 6.0)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.4f;
                    Injector2IsOn = true;
                    Injector2Fraction = 0.4f;
                }
                else if (WaterGlassLevelIN <= 6.0 & WaterGlassLevelIN > 5.75)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.6f;
                    Injector2IsOn = true;
                    Injector2Fraction = 0.4f;
                }
                else if (BoilerPressurePSI > (MaxBoilerPressurePSI - 10.0))  // If boiler pressure is not too low then turn on injector 2
                {
                    if (WaterGlassLevelIN <= 5.75 & WaterGlassLevelIN > 5.5)
                    {
                        Injector1IsOn = true;
                        Injector1Fraction = 0.6f;
                        Injector2IsOn = true;
                        Injector2Fraction = 0.6f;
                    }
                    else if (WaterGlassLevelIN <= 5.5 & WaterGlassLevelIN > 5.25)
                    {
                        Injector1IsOn = true;
                        Injector1Fraction = 0.8f;
                        Injector2IsOn = true;
                        Injector2Fraction = 0.6f;
                    }
                    else if (WaterGlassLevelIN <= 5.25 & WaterGlassLevelIN > 5.0)
                    {
                        Injector1IsOn = true;
                        Injector1Fraction = 0.8f;
                        Injector2IsOn = true;
                        Injector2Fraction = 0.8f;
                    }
                    else if (WaterGlassLevelIN <= 5.0 & WaterGlassLevelIN > 4.75)
                    {
                        Injector1IsOn = true;
                        Injector1Fraction = 1.0f;
                        Injector2IsOn = true;
                        Injector2Fraction = 0.8f;
                    }
                    else if (WaterGlassLevelIN <= 4.75 & WaterGlassLevelIN > 4.5)
                    {
                        Injector1IsOn = true;
                        Injector1Fraction = 1.0f;
                        Injector2IsOn = true;
                        Injector2Fraction = 1.0f;
                    }
                }
            }
            #endregion
            // Determine Heat Ratio - for calculating burn rate

            if (BoilerHeat)
            {
                if (EvaporationLBpS > TotalSteamUsageLBpS)
                {
                    HeatRatio = MathHelper.Clamp(((BoilerHeatOutBTUpS / BoilerHeatInBTUpS) * (TotalSteamUsageLBpS / EvaporationLBpS)), 0.1f, 1.0f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only, clamp < 1 if max boiler heat reached.
                }
                else
                {
                    HeatRatio = MathHelper.Clamp(((BoilerHeatOutBTUpS / BoilerHeatInBTUpS) * (EvaporationLBpS / TotalSteamUsageLBpS)), 0.1f, 1.0f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only, clamp < 1 if max boiler heat reached.
                }
            }
            else
            {
                HeatRatio = MathHelper.Clamp((BoilerHeatOutBTUpS / BoilerHeatInBTUpS), 0.1f, 1.3f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only
            }
        }

    // +++++++++++++++ Main Simulation - End +++++++++++++++++++++

        public override float GetDataOf(CabViewControl cvc)
        {
            float data;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.WHISTLE:
                    data = Horn ? 1 : 0;
                    break;
                case CABViewControlTypes.REGULATOR:
                    data = ThrottlePercent / 100f;
                    break;
                case CABViewControlTypes.BOILER_WATER:
                    data = WaterFraction;
                    break;
                case CABViewControlTypes.TENDER_WATER:
                    data = TenderWaterVolumeUKG; // Looks like default locomotives need an absolute UK gallons value
                    break;
                case CABViewControlTypes.STEAM_PR:
                    data = ConvertFromPSI(cvc, BoilerPressurePSI);
                    break;
                case CABViewControlTypes.STEAMCHEST_PR:
                    data = ConvertFromPSI(cvc, SteamChestPressurePSI);
                    break;
                case CABViewControlTypes.CUTOFF:
                case CABViewControlTypes.REVERSER_PLATE:
                    data = Train.MUReverserPercent / 100f;
                    break;
                case CABViewControlTypes.CYL_COCKS:
                    data = CylinderCocksAreOpen ? 1 : 0;
                    break;
                case CABViewControlTypes.BLOWER:
                    data = BlowerController.CurrentValue;
                    break;
                case CABViewControlTypes.DAMPERS_FRONT:
                    data = DamperController.CurrentValue;
                    break;
                case CABViewControlTypes.FIREBOX:
                    data = FireMassKG / MaxFireMassKG;
                    break;
                case CABViewControlTypes.FIREHOLE:
                    data = FireboxDoorController.CurrentValue;
                    break;
                case CABViewControlTypes.WATER_INJECTOR1:
                    data = Injector1Controller.CurrentValue;
                    break;
                case CABViewControlTypes.WATER_INJECTOR2:
                    data = Injector2Controller.CurrentValue;
                    break;
                case CABViewControlTypes.STEAM_INJ1:
                    data = Injector1IsOn ? 1 : 0;
                    break;
                case CABViewControlTypes.STEAM_INJ2:
                    data = Injector2IsOn ? 1 : 0;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }
            return data;
        }

        public override string GetStatus()
        {
            var evap = pS.TopH(EvaporationLBpS);
            var usage = pS.TopH(PreviousTotalSteamUsageLBpS);
            
			var result = new StringBuilder();
            result.AppendFormat("Boiler pressure = {0:F1} PSI\nSteam = +{1:F0} lb/h -{2:F0} lb/h ({3:F0} %)\nWater Gauge = {4:F1} in", BoilerPressurePSI, evap, usage, Smoke.SmoothedValue * 100, WaterGlassLevelIN);
            if (FiringIsManual)
            {
                result.AppendFormat("\nWater level = {0:F0} %", WaterFraction * 100);
                if (IdealFireMassKG > 0)
                    result.AppendFormat("\nFire mass = {0:F0} %", FireMassKG / IdealFireMassKG * 100);
                else
                    result.AppendFormat("\nFire ratio = {0:F0} %", FireRatio * 100);
                result.Append("\nInjectors =");
                if (Injector1IsOn)
                    result.AppendFormat(" {0:F0} %", Injector1Controller.CurrentValue*100);
                else
                    result.Append(" Off");
                if (Injector2IsOn)
                    result.AppendFormat(" {0:F0} %", Injector2Controller.CurrentValue * 100);
                else
                    result.Append(" Off");
                result.AppendFormat("\nBlower = {0:F0} %", BlowerController.CurrentValue * 100);
                result.AppendFormat("\nDamper = {0:F0} %", DamperController.CurrentValue * 100);
                result.AppendFormat("\nFiring rate = {0:F0} %", FiringRateController.CurrentValue * 100);
            }
            return result.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("Car {0}\t{2} {1}\t{3:F0}%\t{4:F0}m/s\t{5:F0}kW\t{6:F0}kN\t{7}\t{8}\n", UiD, Flipped ? "(flip)" : "", Direction == Direction.Forward ? "Fwd" : Direction == Direction.Reverse ? "Rev" : "N", ThrottlePercent, SpeedMpS, MotiveForceN * SpeedMpS / 1000, MotiveForceN / 1000, WheelSlip ? "Slipping" : "", CouplerOverloaded ? "Coupler overloaded" : "");

            status.AppendFormat("\n\t\t === Key Inputs === \t\t{0:N0} lb/h\n",
            pS.TopH(EvaporationLBpS));
            status.AppendFormat("Input:\tEvap\t{0:N0} ft^2\tGrate\t{1:N0} ft^2\tBoil.\t{2:N0} ft^3\tSup\t{3:N0} ft^2\tFuel Cal.\t{4:N0} btu/lb\t\tG. Ratio\t{5:N2}\n",
                Me2.ToFt2(EvaporationAreaM2),
                Me2.ToFt2(GrateAreaM2),
                BoilerVolumeFT3,
                Me2.ToFt2(SuperheatAreaM2),
                KJpKg.ToBTUpLb(FuelCalorificKJpKG),
                SteamGearRatio);

            status.AppendFormat("\n\t\t === Steam Production === \t\t{0:N0} lb/h\n",
            pS.TopH(EvaporationLBpS));
            status.AppendFormat("Boiler:\tPower\t{0:N0} bhp\tMass\t{1:N0} lb\tOut.\t{2:N0} lb/h\t\tBoiler Eff\t{3:N2}\n",
                BoilerKW * BoilerKWtoBHP,
                BoilerMassLB,
                MaxBoilerOutputLBpH,
                BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(FuelBurnRateLBpS) / Me2.ToFt2(GrateAreaM2))]);
            status.AppendFormat("Heat:\tIn\t{0:N0} btu\tOut\t{1:N0} btu\tSteam\t{2:N0} btu/lb\t\tWater\t{3:N0} btu/lb\tand\t{4:N0} btu/ft^3\t\tHeat\t{5:N0} btu\t\tMax\t{6:N0} btu\n",
                BoilerHeatInBTUpS,
                PreviousBoilerHeatOutBTUpS,
                BoilerSteamHeatBTUpLB,
                BoilerWaterHeatBTUpLB,
                WaterHeatBTUpFT3,
                BoilerHeatSmoothBTU.Value,
                MaxBoilerHeatBTU);
            status.AppendFormat("Temp.:\tFlue\t{0:N0} F\tWater\t{1:N0} F\tS Ratio\t{2:N2}\t\tMaxSuper {3:N0} F\t\tCurSuper {4:N0} F",
                C.ToF(C.FromK(FlueTempK)),
                C.ToF(C.FromK(BoilerWaterTempK)),
                SuperheatVolumeRatio,
                SuperheatRefTempF,
                CurrentSuperheatTeampF);

            status.AppendFormat("\n\t\t === Steam Usage === \t\t{0:N0} lb/h\n",
                pS.TopH(PreviousTotalSteamUsageLBpS));
            status.AppendFormat("Usage.:\tCyl.\t{0:N0} lb/h\tBlower\t{1:N0} lb/h\tRad.\t{2:N0} lb/h\tComp.\t{3:N0} lb/h\tSafety\t{4:N0} lb/h\tCock\t{5:N0} lb/h\tGen.\t{6:N0} lb/h\tStoke\t{7:N0} lb/h\n",
            pS.TopH(CylinderSteamUsageLBpS),
            pS.TopH(BlowerSteamUsageLBpS),
            pS.TopH(RadiationSteamLossLBpS),
            pS.TopH(CompSteamUsageLBpS),
            pS.TopH(SafetyValveUsageLBpS),
            pS.TopH(CylCockSteamUsageLBpS),
            pS.TopH(GeneratorSteamUsageLBpS),
            pS.TopH(StokerSteamUsageLBpS));
            status.AppendFormat("Press.:\tChest\t{0:N0} psi\tBack\t{1:N0} psi\tMEP\t{2:N0} psi\tComp\t{3:N0} psi\tExhaust\t{4:N0} psi\tSup Fact\t{5:N2}\tMax Safe\t{6:N0} lb/h ({7} x {8:N1})\n",
            SteamChestPressurePSI,
            BackPressurePSI,
            MeanEffectivePressurePSI,
            CylinderCompressionPressurePSI,
            CylinderExhaustPressurePSI,
            SuperheaterSteamUsageFactor,
            pS.TopH(MaxSafetyValveDischargeLbspS),
            NumSafetyValves,
            SafetyValveSizeIn);
            status.AppendFormat("Status.:\tSafety\t{0}\tPlug\t{1}\tPrime\t{2}\tBoil. Heat\t{3}\tSuper\t{4}\tGear\t{5}",
                SafetyIsOn,
                FusiblePlugIsBlown,
                BoilerIsPriming,
                BoilerHeat,
                HasSuperheater,
                IsGearedSteamLoco);

            status.AppendFormat("\n\t === Fireman === \n");
            status.AppendFormat("Fire:\tIdeal\t{0:N0} lb\t\tFire\t{1:N0} lb\t\tMax Fire\t{2:N0} lb/h\t\tFuel\t{3:N0} lb/h\t\tBurn\t{4:N0} lb/h\t\tComb\t{5:N1} lbs/ft2\n",
                Kg.ToLb(IdealFireMassKG),
                Kg.ToLb(FireMassKG),
                pS.TopH(Kg.ToLb(MaxFiringRateKGpS)),
                pS.TopH(FuelFeedRateLBpS),
                pS.TopH(FuelBurnRateLBpS),
                (pS.TopH(GrateCombustionRateLBpFt2)));
            status.AppendFormat("Injector:\tMax\t{0:N0} gal(uk)/h\t\t({1:N0}mm)\tInj. 1\t{2:N0} gal(uk)/h\t\ttemp\t{3:N0} F\t\tInj. 2\t{4:N0} gal(uk)/h\t\ttemp 2\t{5:N0} F\n",
                pS.TopH(InjectorFlowRateLBpS) / WaterLBpUKG,
                InjectorSize,
                Injector1Fraction * pS.TopH(InjectorFlowRateLBpS) / WaterLBpUKG,
                Injector1WaterDelTempF,
                Injector2Fraction * pS.TopH(InjectorFlowRateLBpS) / WaterLBpUKG,
                Injector2WaterDelTempF);
            status.AppendFormat("Tender:\tCoal\t{0:N0} lb\t{1:N0} %\tWater\t{2:N0} gal(uk)\t\t{3:F0} %\n",
                TenderCoalMassLB,
                (TenderCoalMassLB / Kg.ToLb(MaxTenderCoalMassKG)) * 100,
                TenderWaterVolumeUKG,
                (TenderWaterVolumeUKG / (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG)) * 100);
            status.AppendFormat("Status.:\tCoalOut\t{0}\t\tWaterOut\t{1}\tFireOut\t{2}\tStoker\t{3}\tBoost\t{4}",
                CoalIsExhausted,
                WaterIsExhausted,
                FireIsExhausted,
                StokerIsMechanical,
                FuelBoost);

            status.AppendFormat("\n\t\t === Performance === \n");
            status.AppendFormat("Power:\tMaxIHP\t{0:N0} hp\tIHP\t{1:N0} hp\tDHP\t{2:N0} hp\n",
                MaxIndicatedHorsePowerHP,
                IndicatedHorsePowerHP,
                DrawbarHorsePowerHP);
            status.AppendFormat("Force:\tMax TE\t{0:N0}\tStart TE\t{1:N0} lbf\tTE\t{2:N0} lbf\tDraw\t{3:N0} lbf\tCritic\t{4:N0} lbf\n",
                MaxTractiveEffortLbf,
                N.ToLbf(StartTractiveEffortN),
                TractiveEffortLbsF,
                DrawBarPullLbsF,
                CriticalSpeedTractiveEffortLbf);

            status.AppendFormat("Move:\tPiston\t{0:N0}ft/m\tDrv\t{1:N0} rpm\tGear Sp\t{2:N0} mph\n",
                PistonSpeedFtpM,
                pS.TopM(DrvWheelRevRpS),
                MpS.ToMpH(MaxGearedSpeedMpS));

            return status.ToString();
        }

        public override void StartReverseIncrease( float? target ) {
            CutoffController.StartIncrease( target );
            CutoffController.CommandStartTime = Simulator.ClockTime;
            switch (Direction)
            {
                case Direction.Reverse: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.Off); break;
                case Direction.N: Simulator.Confirmer.Confirm(CabControl.SteamLocomotiveReverser, CabSetting.Neutral); break;
                case Direction.Forward: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.On); break;
            }
            SignalEvent(Event.ReverserChange);
        }

        public void StopReverseIncrease() {
            CutoffController.StopIncrease();
        }

        public override void StartReverseDecrease( float? target ) {
            CutoffController.StartDecrease( target );
            CutoffController.CommandStartTime = Simulator.ClockTime;
            switch (Direction)
            {
                case Direction.Reverse: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.Off); break;
                case Direction.N: Simulator.Confirmer.Confirm(CabControl.SteamLocomotiveReverser, CabSetting.Neutral); break;
                case Direction.Forward: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.On); break;
            }
            SignalEvent(Event.ReverserChange);
        }

        public void StopReverseDecrease() {
            CutoffController.StopDecrease();
        }

        public void ReverserChangeTo( bool isForward, float? target ) {
            if( isForward ) {
                if( target > CutoffController.CurrentValue ) {
                    StartReverseIncrease( target );
                }
            } else {
                if( target < CutoffController.CurrentValue ) {
                    StartReverseDecrease( target );
                }
            }
        }

        public void SetCutoffPercent(float percent)
        {
            Train.MUReverserPercent = CutoffController.SetRDPercent(percent);
            Direction = Train.MUReverserPercent >= 0 ? Direction.Forward : Direction.Reverse;
        }

        public void StartInjector1Increase( float? target ) {
            Injector1Controller.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Injector1, CabSetting.Increase, Injector1Controller.CurrentValue * 100 );
            Injector1Controller.StartIncrease( target );
        }

        public void StopInjector1Increase() {
            Injector1Controller.StopIncrease();
        }

        public void StartInjector1Decrease( float? target ) {
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Injector1, CabSetting.Decrease, Injector1Controller.CurrentValue * 100 );
            Injector1Controller.StartDecrease( target );
            Injector1Controller.CommandStartTime = Simulator.ClockTime;
        }

        public void StopInjector1Decrease() {
            Injector1Controller.StopDecrease();
        }
        
        public void ToggleInjector1()
        {
            if (!FiringIsManual)
                return;
            Injector1IsOn = !Injector1IsOn;
            SignalEvent(Injector1IsOn ? Event.SteamEjector1On : Event.SteamEjector1Off); // hook for sound trigger
            Simulator.Confirmer.Confirm(CabControl.Injector1, Injector1IsOn ? CabSetting.On : CabSetting.Off);
        }

        public void StartInjector2Increase( float? target ) {
            Injector2Controller.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Injector2, CabSetting.Increase, Injector2Controller.CurrentValue * 100 );
            Injector2Controller.StartIncrease( target );
        }

        public void StopInjector2Increase() {
            Injector2Controller.StopIncrease();
        }

        public void StartInjector2Decrease( float? target ) {
            Injector2Controller.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Injector2, CabSetting.Decrease, Injector2Controller.CurrentValue * 100 );
            Injector2Controller.StartDecrease( target );
        }

        public void StopInjector2Decrease() {
            Injector2Controller.StopDecrease();
        }

        public void ToggleInjector2()
        {
            if (!FiringIsManual)
                return;
            Injector2IsOn = !Injector2IsOn;
            SignalEvent(Injector2IsOn ? Event.SteamEjector2On : Event.SteamEjector2Off); // hook for sound trigger
            Simulator.Confirmer.Confirm( CabControl.Injector2, Injector2IsOn ? CabSetting.On : CabSetting.Off );
        }

        public void Injector1ChangeTo( bool increase, float? target ) {
            if( increase ) {
                if( target > Injector1Controller.CurrentValue ) {
                    StartInjector1Increase( target );
                }
            } else {
                if( target < Injector1Controller.CurrentValue ) {
                    StartInjector1Decrease( target );
                }
            }
        }

        public void Injector2ChangeTo( bool increase, float? target ) {
            if( increase ) {
                if( target > Injector2Controller.CurrentValue ) {
                    StartInjector2Increase( target );
                }
            } else {
                if( target < Injector2Controller.CurrentValue ) {
                    StartInjector2Decrease( target );
                }
            }
        }

        public void StartBlowerIncrease( float? target ) {
            BlowerController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Blower, CabSetting.Increase, BlowerController.CurrentValue * 100 );
            BlowerController.StartIncrease( target );
            SignalEvent(Event.BlowerChange);
        }
        public void StopBlowerIncrease() {
            BlowerController.StopIncrease();
        }
        public void StartBlowerDecrease( float? target ) {
            BlowerController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Blower, CabSetting.Decrease, BlowerController.CurrentValue * 100 );
            BlowerController.StartDecrease( target );
            SignalEvent(Event.BlowerChange);
        }
        public void StopBlowerDecrease() {
            BlowerController.StopDecrease();
        }

        public void BlowerChangeTo( bool increase, float? target ) {
            if( increase ) {
                if( target > BlowerController.CurrentValue ) {
                    StartBlowerIncrease( target );
                }
            } else {
                if( target < BlowerController.CurrentValue ) {
                    StartBlowerDecrease( target );
                }
            }
        }

        public void StartDamperIncrease( float? target ) {
            DamperController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Damper, CabSetting.Increase, DamperController.CurrentValue * 100 );
            DamperController.StartIncrease( target );
            SignalEvent(Event.DamperChange);
        }
        public void StopDamperIncrease() {
            DamperController.StopIncrease();
        }
        public void StartDamperDecrease( float? target ) {
            DamperController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.Damper, CabSetting.Decrease, DamperController.CurrentValue * 100 );
            DamperController.StartDecrease( target );
            SignalEvent(Event.DamperChange);
        }
        public void StopDamperDecrease() {
            DamperController.StopDecrease();
        }

        public void DamperChangeTo( bool increase, float? target ) {
            if( increase ) {
                if( target > DamperController.CurrentValue ) {
                    StartDamperIncrease( target );
                }
            } else {
                if( target < DamperController.CurrentValue ) {
                    StartDamperDecrease( target );
                }
            }
        }

        public void StartFireboxDoorIncrease(float? target)
        {
            FireboxDoorController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent(CabControl.FireboxDoor, CabSetting.Increase, FireboxDoorController.CurrentValue * 100);
            FireboxDoorController.StartIncrease(target);
            SignalEvent(Event.FireboxDoorChange);
        }
        public void StopFireboxDoorIncrease()
        {
            FireboxDoorController.StopIncrease();
        }
        public void StartFireboxDoorDecrease(float? target)
        {
            FireboxDoorController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent(CabControl.FireboxDoor, CabSetting.Decrease, FireboxDoorController.CurrentValue * 100);
            FireboxDoorController.StartDecrease(target);
            SignalEvent(Event.FireboxDoorChange);
        }
        public void StopFireboxDoorDecrease()
        {
            FireboxDoorController.StopDecrease();
        }

        public void FireboxDoorChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > FireboxDoorController.CurrentValue)
                {
                    StartFireboxDoorIncrease(target);
                }
            }
            else
            {
                if (target < FireboxDoorController.CurrentValue)
                {
                    StartFireboxDoorDecrease(target);
                }
            }
        }

        public void StartFiringRateIncrease( float? target ) {
            FiringRateController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.FiringRate, FiringRateController.CurrentValue * 100 );
            FiringRateController.StartIncrease( target );
        }
        public void StopFiringRateIncrease() {
            FiringRateController.StopIncrease();
        }
        public void StartFiringRateDecrease( float? target ) {
            FiringRateController.CommandStartTime = Simulator.ClockTime;
            Simulator.Confirmer.ConfirmWithPerCent( CabControl.FiringRate, FiringRateController.CurrentValue * 100 );
            FiringRateController.StartDecrease( target );
        }
        public void StopFiringRateDecrease() {
            FiringRateController.StopDecrease();
        }

        public void FiringRateChangeTo( bool increase, float? target ) {
            if( increase ) {
                if( target > FiringRateController.CurrentValue ) {
                    StartFiringRateIncrease( target );
                }
            } else {
                if( target < FiringRateController.CurrentValue ) {
                    StartFiringRateDecrease( target );
                }
            }
        }

        public void FireShovelfull()
        {
            FireMassKG+= ShovelMassKG;
            Simulator.Confirmer.Confirm( CabControl.FireShovelfull, CabSetting.On );
        }

        public void ToggleCylinderCocks()
        {
            CylinderCocksAreOpen = !CylinderCocksAreOpen;
            SignalEvent(Event.CylinderCocksToggle);
            Simulator.Confirmer.Confirm(CabControl.CylinderCocks, CylinderCocksAreOpen ? CabSetting.On : CabSetting.Off);
        }

        public void ToggleManualFiring()
        {
            FiringIsManual = !FiringIsManual;
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            MSTSNotchController controller;
            if (type == (uint)MSTSLocomotiveViewer.PickupType.FuelCoal) return FuelController;
            if (type == (uint)MSTSLocomotiveViewer.PickupType.FuelWater) return WaterController;
            return null;
        }

        /// <summary>
        /// Sets coal and water supplies to full immediately.
        /// Provided in case route lacks pickup points for coal and especially water.
        /// </summary>
        public override void RefillImmediately()
        {
            RefillTenderWithCoal();
            RefillTenderWithWater();
        }

        /// <summary>
        /// Returns the fraction of coal or water already in tender.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            if (pickupType == (uint)MSTSLocomotiveViewer.PickupType.FuelWater)
            {
                return WaterController.CurrentValue;
            }
            if (pickupType == (uint)MSTSLocomotiveViewer.PickupType.FuelCoal)
            {
                return FuelController.CurrentValue;
            }
            return 0f;
        }

		public void GetLocoInfo(ref float CC, ref float BC, ref float DC, ref float FC, ref float I1, ref float I2)
		{
			CC = CutoffController.CurrentValue;
			BC = BlowerController.CurrentValue;
			DC = DamperController.CurrentValue;
			FC = FiringRateController.CurrentValue;
			I1 = Injector1Controller.CurrentValue;
			I2 = Injector2Controller.CurrentValue;
		}

		public void SetLocoInfo(float CC, float BC, float DC, float FC, float I1, float I2)
		{
			CutoffController.CurrentValue = CC;
			CutoffController.UpdateValue = 0.0f;
			BlowerController.CurrentValue = BC;
			BlowerController.UpdateValue = 0.0f;
			DamperController.CurrentValue = DC;
			DamperController.UpdateValue = 0.0f;
			FiringRateController.CurrentValue = FC;
			FiringRateController.UpdateValue = 0.0f;
			Injector1Controller.CurrentValue = I1;
			Injector1Controller.UpdateValue = 0.0f;
			Injector2Controller.CurrentValue = I2;
			Injector2Controller.UpdateValue = 0.0f;
		}

    } // class SteamLocomotive

    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds any special steam loco animation to the basic LocomotiveViewer class
    /// </summary>
    class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {
        const float LBToKG = 0.45359237f;
        const float SteamVaporDensityAt100DegC1BarM3pKG = 1.694f;
        float Throttlepercent;
        float Burn_Rate;
        float Steam_Rate;
        float Color_Value;
        float Pulse_Rate = 1.0f;
        float pulse = 0.25f;
        float steamcolor = 1.0f;
        float old_Distance_Travelled = 0.0f;

        MSTSSteamLocomotive SteamLocomotive { get{ return (MSTSSteamLocomotive)Car;}}
        List<ParticleEmitterViewer> Cylinders = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Drainpipe = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SafetyValves = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Stack = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Whistle = new List<ParticleEmitterViewer>();

        public MSTSSteamLocomotiveViewer(Viewer viewer, MSTSSteamLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.
            string steamTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\smokemain.ace";

            foreach (var emitter in ParticleDrawers)
            {
                if (emitter.Key.ToLowerInvariant() == "cylindersfx")
                    Cylinders.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "drainpipefx")
                    Drainpipe.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "safetyvalvesfx")
                    SafetyValves.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "stackfx")
                    Stack.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "whistlefx")
                    Whistle.AddRange(emitter.Value);
                foreach (var drawer in emitter.Value)
                    drawer.Initialize(viewer.TextureManager.Get(steamTexture));
            }
        }

        /// <summary>
        /// Overrides the base method as steam locomotives have continuous reverser controls and so
        /// lacks the throttle interlock and warning in other locomotives. 
        /// </summary>
        protected override void ReverserControlForwards() {
            SteamLocomotive.StartReverseIncrease( null );
        }

        /// <summary>
        /// Overrides the base method as steam locomotives have continuous reverser controls and so
        /// lacks the throttle interlock and warning in other locomotives. 
        /// </summary>
        protected override void ReverserControlBackwards() {
            SteamLocomotive.StartReverseDecrease( null );
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Note: UserInput.IsReleased( UserCommands.ControlReverserForward/Backwards ) are given here but
            // UserInput.IsPressed( UserCommands.ControlReverserForward/Backwards ) are handled in base class MSTSLocomotive.
            if( UserInput.IsReleased( UserCommands.ControlForwards ) ) {
                SteamLocomotive.StopReverseIncrease();
                new ContinuousReverserCommand( Viewer.Log, true, SteamLocomotive.CutoffController.CurrentValue, SteamLocomotive.CutoffController.CommandStartTime );
            } else if( UserInput.IsReleased( UserCommands.ControlBackwards ) ) {
                SteamLocomotive.StopReverseDecrease();
                new ContinuousReverserCommand( Viewer.Log, false, SteamLocomotive.CutoffController.CurrentValue, SteamLocomotive.CutoffController.CommandStartTime );
            }
            if( UserInput.IsPressed( UserCommands.ControlInjector1Increase ) ) {
                SteamLocomotive.StartInjector1Increase( null );
            } else if( UserInput.IsReleased( UserCommands.ControlInjector1Increase ) ) {
                SteamLocomotive.StopInjector1Increase();
                new ContinuousInjectorCommand( Viewer.Log, 1, true, SteamLocomotive.Injector1Controller.CurrentValue, SteamLocomotive.Injector1Controller.CommandStartTime );
            }
            else if( UserInput.IsPressed( UserCommands.ControlInjector1Decrease ) )
                SteamLocomotive.StartInjector1Decrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlInjector1Decrease ) ) {
                SteamLocomotive.StopInjector1Decrease();
                new ContinuousInjectorCommand( Viewer.Log, 1, false, SteamLocomotive.Injector1Controller.CurrentValue, SteamLocomotive.Injector1Controller.CommandStartTime );
            }
            if( UserInput.IsPressed( UserCommands.ControlInjector1 ) )
                new ToggleInjectorCommand( Viewer.Log, 1 );

            if (UserInput.IsPressed(UserCommands.ControlInjector2Increase))
                SteamLocomotive.StartInjector2Increase( null );
            else if( UserInput.IsReleased( UserCommands.ControlInjector2Increase ) ) {
                SteamLocomotive.StopInjector2Increase();
                new ContinuousInjectorCommand( Viewer.Log, 2, true, SteamLocomotive.Injector2Controller.CurrentValue, SteamLocomotive.Injector2Controller.CommandStartTime );
            } else if( UserInput.IsPressed( UserCommands.ControlInjector2Decrease ) )
                SteamLocomotive.StartInjector2Decrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlInjector2Decrease ) ) {
                SteamLocomotive.StopInjector2Decrease();
                new ContinuousInjectorCommand( Viewer.Log, 2, false, SteamLocomotive.Injector2Controller.CurrentValue, SteamLocomotive.Injector2Controller.CommandStartTime );
            }
            if( UserInput.IsPressed( UserCommands.ControlInjector2 ) )
                new ToggleInjectorCommand( Viewer.Log, 2 );

            if (UserInput.IsPressed(UserCommands.ControlBlowerIncrease))
                SteamLocomotive.StartBlowerIncrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlBlowerIncrease ) ) {
                SteamLocomotive.StopBlowerIncrease();
                new ContinuousBlowerCommand( Viewer.Log, true, SteamLocomotive.BlowerController.CurrentValue, SteamLocomotive.BlowerController.CommandStartTime );
            } else if( UserInput.IsPressed( UserCommands.ControlBlowerDecrease ) )
                SteamLocomotive.StartBlowerDecrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlBlowerDecrease ) ) {
                SteamLocomotive.StopBlowerDecrease();
                new ContinuousBlowerCommand( Viewer.Log, false, SteamLocomotive.BlowerController.CurrentValue, SteamLocomotive.BlowerController.CommandStartTime );
            }
            if (UserInput.IsPressed(UserCommands.ControlDamperIncrease))
                SteamLocomotive.StartDamperIncrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlDamperIncrease ) ) {
                SteamLocomotive.StopDamperIncrease();
                new ContinuousDamperCommand( Viewer.Log, true, SteamLocomotive.DamperController.CurrentValue, SteamLocomotive.DamperController.CommandStartTime );
            } else if( UserInput.IsPressed( UserCommands.ControlDamperDecrease ) )
                SteamLocomotive.StartDamperDecrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlDamperDecrease ) ) {
                SteamLocomotive.StopDamperDecrease();
                new ContinuousDamperCommand( Viewer.Log, false, SteamLocomotive.DamperController.CurrentValue, SteamLocomotive.DamperController.CommandStartTime );
            }
            if (UserInput.IsPressed(UserCommands.ControlFireboxOpen))
                SteamLocomotive.StartFireboxDoorIncrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFireboxOpen)) {
                SteamLocomotive.StopFireboxDoorIncrease();
                new ContinuousFireboxDoorCommand(Viewer.Log, true, SteamLocomotive.FireboxDoorController.CurrentValue, SteamLocomotive.FireboxDoorController.CommandStartTime);
            } else if (UserInput.IsPressed(UserCommands.ControlFireboxClose))
                SteamLocomotive.StartFireboxDoorDecrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFireboxClose)) {
                SteamLocomotive.StopFireboxDoorDecrease();
                new ContinuousFireboxDoorCommand(Viewer.Log, false, SteamLocomotive.FireboxDoorController.CurrentValue, SteamLocomotive.FireboxDoorController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlFiringRateIncrease))
                SteamLocomotive.StartFiringRateIncrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlFiringRateIncrease ) ) {
                SteamLocomotive.StopFiringRateIncrease();
                new ContinuousFiringRateCommand( Viewer.Log, true, SteamLocomotive.FiringRateController.CurrentValue, SteamLocomotive.FiringRateController.CommandStartTime );
            } else if( UserInput.IsPressed( UserCommands.ControlFiringRateDecrease ) )
                SteamLocomotive.StartFiringRateDecrease( null );
            else if( UserInput.IsReleased( UserCommands.ControlFiringRateDecrease ) ) {
                SteamLocomotive.StopFiringRateDecrease();
                new ContinuousFiringRateCommand( Viewer.Log, false, SteamLocomotive.FiringRateController.CurrentValue, SteamLocomotive.FiringRateController.CommandStartTime );
            }
            if( UserInput.IsPressed( UserCommands.ControlFireShovelFull ) )
                new FireShovelfullCommand( Viewer.Log );
            if( UserInput.IsPressed( UserCommands.ControlCylinderCocks ) )
                new ToggleCylinderCocksCommand( Viewer.Log );
            if( UserInput.IsPressed( UserCommands.ControlFiring ) )
                new ToggleManualFiringCommand( Viewer.Log );

            if (UserInput.RDState != null && UserInput.RDState.Changed)
                SteamLocomotive.SetCutoffPercent(UserInput.RDState.DirectionPercent);

            base.HandleUserInput(elapsedTime);

#if DEBUG_DUMP_STEAM_POWER_CURVE
            // For power curve tests
            if (Viewer.Settings.DataLogger
                && !Viewer.Settings.DataLogPerformanceeous
                && !Viewer.Settings.DataLogPhysics
                && !Viewer.Settings.DataLogMisc)
            {
                var loco = SteamLocomotive;
                // If we're using more steam than the boiler can make ...
                if (loco.PreviousTotalSteamUsageLBpS > loco.EvaporationLBpS)
                {
                    // Reduce the cut-off gradually as far as 15%
                    if (loco.CutoffController.CurrentValue > 0.15)
                    {
                        float? target = MathHelper.Clamp(loco.CutoffController.CurrentValue - 0.01f, 0.15f, 0.75f);
                        loco.StartReverseDecrease(target);
                    }
                    else
                    {
                        // Reduce the throttle also
                        float? target = SteamLocomotive.ThrottleController.CurrentValue - 0.01f;
                        loco.StartThrottleDecrease(target);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var car = Car as MSTSSteamLocomotive;
            var steamUsageLBpS = car.CylinderSteamUsageLBpS + car.BlowerSteamUsageLBpS + car.BasicSteamUsageLBpS + (car.SafetyIsOn ? car.SafetyValveUsageLBpS : 0);
            var cockSteamUsageLBps = car.CylCockSteamUsageLBpS;
            var safetySteamUsageLBps = car.SafetyValveUsageLBpS;
            // TODO: Expected assignment:
            //var steamVolumeM3pS = Kg.FromLb(steamUsageLBpS) * SteamVaporDensityAt100DegC1BarM3pKG;
            var steamVolumeM3pS = Kg.FromLb(steamUsageLBpS) * SteamVaporDensityAt100DegC1BarM3pKG;
            var cocksVolumeM3pS = Kg.FromLb(cockSteamUsageLBps) * SteamVaporDensityAt100DegC1BarM3pKG;
            var safetyVolumeM3pS = Kg.FromLb(safetySteamUsageLBps) * SteamVaporDensityAt100DegC1BarM3pKG;

            foreach (var drawer in Cylinders)
                drawer.SetOutput(car.CylinderCocksAreOpen ? cocksVolumeM3pS : 0);

            foreach (var drawer in Drainpipe)
                drawer.SetOutput(0);

            foreach (var drawer in SafetyValves)
                drawer.SetOutput(car.SafetyIsOn ? safetyVolumeM3pS : 0);
            
            foreach (var drawer in Stack)
            {

                Throttlepercent = Math.Max(car.ThrottlePercent / 10f, 0f);

                Pulse_Rate = (MathHelper.Pi * SteamLocomotive.DriverWheelRadiusM);

                if (car.Direction == Direction.Forward)
                {
                    if (pulse == 0.25f)
                        if (Viewer.PlayerTrain.DistanceTravelledM > old_Distance_Travelled + (Pulse_Rate / 4))
                        {
                            pulse = 1.0f;
                        }
                    if (pulse == 1.0f)
                        if (Viewer.PlayerTrain.DistanceTravelledM > old_Distance_Travelled + Pulse_Rate)
                        {
                            pulse = 0.25f;
                            old_Distance_Travelled = Viewer.PlayerTrain.DistanceTravelledM;
                        }
                }
                if (car.Direction == Direction.Reverse)
                {
                    if (pulse == 0.25f)
                        if (Viewer.PlayerTrain.DistanceTravelledM < old_Distance_Travelled - (Pulse_Rate / 4))
                        {
                            pulse = 1.0f;
                        }
                    if (pulse == 1.0f)
                        if (Viewer.PlayerTrain.DistanceTravelledM < old_Distance_Travelled - Pulse_Rate)
                        {
                            pulse = 0.25f;
                            old_Distance_Travelled = Viewer.PlayerTrain.DistanceTravelledM;
                        }
                }
                Color_Value = (steamVolumeM3pS * .10f) + (car.Smoke.SmoothedValue / 2) / 256 * 100f;

                drawer.SetOutput((steamVolumeM3pS * pulse) + car.FireRatio, (Throttlepercent + car.FireRatio), (new Color(Color_Value, Color_Value, Color_Value)));
               
            }

            foreach (var drawer in Whistle)
                drawer.SetOutput(car.Horn ? 1 : 0);

            base.PrepareFrame(frame, elapsedTime);
        }
         
        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }


    } // class SteamLocomotiveViewer

}
