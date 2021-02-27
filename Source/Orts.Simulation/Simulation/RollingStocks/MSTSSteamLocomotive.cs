// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// Burn debugging is off by default - uncomment the #define to turn on - provides visibility of burn related parameters for AI Fireman on extended HUD.
//#define DEBUG_LOCO_BURN_AI

// Compound Indicator Pressures are off by default - uncomment the #define to turn on - provides visibility of calculations for MEP.
//#define DEBUG_LOCO_STEAM_COMPOUND_HP_MEP

// Compound Indicator Pressures are off by default - uncomment the #define to turn on - provides visibility of calculations for MEP.
//#define DEBUG_LOCO_STEAM_COMPOUND_LP_MEP

// Burn debugging is off by default - uncomment the #define to turn on - provides visibility of calculations for MEP.
//#define DEBUG_LOCO_STEAM_MEP

// Steam usage debugging is off by default - uncomment the #define to turn on - provides visibility of steam usage related parameters on extended HUD. 
//#define DEBUG_LOCO_STEAM_USAGE

// Debug for Auxiliary Tender
//#define DEBUG_AUXTENDER

// Debug for Steam Effects
//#define DEBUG_STEAM_EFFECTS

// Debug for Steam Slip
//#define DEBUG_STEAM_SLIP

// Debug for Steam Slip HUD
//#define DEBUG_STEAM_SLIP_HUD

// Debug for Sound Variables
//#define DEBUG_STEAM_SOUND_VARIABLES

// Debug for Steam Performance Data @ 5mph increments
//#define DEBUG_STEAM_PERFORMANCE

// Debug for Steam Cylinder Events
//#define DEBUG_STEAM_CYLINDER_EVENTS

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
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using ORTS.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a steam locomotive
    /// </summary>
    public class MSTSSteamLocomotive : MSTSLocomotive
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
        public MSTSNotchController SmallEjectorController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController LargeEjectorController = new MSTSNotchController(0, 1, 0.1f);

        public bool Injector1IsOn;
        bool Injector1SoundIsOn = false;
        public bool Injector2IsOn;
        bool Injector2SoundIsOn = false;
        public bool CylinderCocksAreOpen;
        public bool BlowdownValveOpen;
        public bool CylinderCompoundOn;  // Flag to indicate whether compound locomotive is in compound or simple mode of operation - simple = true (ie bypass valve is open)
        bool FiringIsManual;
        bool BlowerIsOn = false;
        bool BoilerIsPriming = false;
        bool WaterIsExhausted = false;
        bool CoalIsExhausted = false;
        bool FireIsExhausted = false;
        bool FuelBoost = false;
        bool FuelBoostReset = false;
        bool StokerIsMechanical = false;
        bool HotStart; // Determine whether locomotive is started in hot or cold state - selectable option in Options TAB
        bool FullBoilerHeat = false;    // Boiler heat has exceeded max possible heat in boiler (max operating steam pressure)
        bool FullMaxPressBoilerHeat = false; // Boiler heat has exceed the max total possible heat in boiler (max safety valve pressure)
        bool ShovelAnyway = false; // Predicts when the AI fireman should be increasing the fire burn rate despite the heat in the boiler
        /// <summary>
        /// Grate limit of locomotive exceedeed?
        /// </summary>
        public bool IsGrateLimit { get; protected set; } = false;
        bool HasSuperheater = false;  // Flag to indicate whether locomotive is superheated steam type
        bool IsSuperSet = false;    // Flag to indicate whether superheating is reducing cylinder condenstation
        bool IsSaturated = false;     // Flag to indicate locomotive is saturated steam type
        bool safety2IsOn = false; // Safety valve #2 is on and opertaing
        bool safety3IsOn = false; // Safety valve #3 is on and opertaing
        bool safety4IsOn = false; // Safety valve #4 is on and opertaing
        bool IsFixGeared = false;
        bool IsSelectGeared = false;
        bool IsLocoSlip = false; 	   // locomotive is slipping
        bool IsCritTELimit = false; // Flag to advise if critical TE is exceeded
        bool ISBoilerLimited = false;  // Flag to indicate that Boiler is limiting factor with the locomotive power
        bool SetFireOn = false; // Flag to set the AI fire to on for starting of locomotive
        bool SetFireOff = false; // Flag to set the AI fire to off for locomotive when approaching a stop 
        bool SetFireReset = false; // Flag if AI fire has been reset, ie no overrides in place
        bool AIFireOverride = false; // Flag to show ai fire has has been overriden
        bool InjectorLockedOut = false; // Flag to lock injectors from changing within a fixed period of time

        // Aux Tender Parameters
        public bool AuxTenderMoveFlag = false; // Flag to indicate whether train has moved
        bool SteamIsAuxTenderCoupled = false;
        float TenderWaterPercent;       // Percentage of water in tender
        public float WaterConsumptionLbpS;
        public float CurrentAuxTenderWaterMassKG;
        public float CurrentAuxTenderWaterVolumeUKG;
        public float CurrentLocoTenderWaterVolumeUKG;
        float PrevCombinedTenderWaterVolumeUKG;
        float PreviousTenderWaterVolumeUKG;
        public float MaxLocoTenderWaterMassKG = 1;         // Maximum read from Eng file - this value must be non-zero, if not defined in ENG file, can cause NaN errors
        float RestoredMaxTotalCombinedWaterVolumeUKG; // Values to restore after game save
        float RestoredCombinedTenderWaterVolumeUKG;     // Values to restore after game save

        // Tender

        public bool HasTenderCoupled = true;

        float BlowdownSteamUsageLBpS;
        float BlowdownValveSizeDiaIn2;

        string SteamLocoType;     // Type of steam locomotive type

        float PulseTracker;
        int NextPulse = 1;

        // state variables
        SmoothedData BoilerHeatSmoothBTU = new SmoothedData(240); // total heat in water and steam in boiler - lb/s * SteamHeat(BTU/lb)
        float BoilerHeatSmoothedBTU;
        float PreviousBoilerHeatSmoothedBTU;
        float BoilerHeatBTU;        // total heat in water and steam in boiler - lb/s * SteamHeat(BTU/lb)
        float MaxBoilerHeatBTU;   // Boiler heat at max rated output and pressure, etc
        float MaxBoilerSafetyPressHeatBTU;  // Boiler heat at boiler pressure for operation of safety valves
        float MaxBoilerHeatSafetyPressurePSI; // Boiler Pressure for calculating max boiler pressure, includes safety valve pressure
        float BoilerStartkW;        // calculate starting boilerkW
        float MaxBoilerHeatInBTUpS = 0.1f; // Remember the BoilerHeat value equivalent to Max Boiler Heat

        float baseStartTempK;     // Starting water temp
        float StartBoilerHeatBTU;
        public float BoilerMassLB;         // current total mass of water and steam in boiler (changes as boiler usage changes)
        bool RestoredGame = false; // Flag to indicate that game is being restored. This will stop some values from being "initialised", as this will overwrite restored values.

        float BoilerKW;                 // power of boiler
        float MaxBoilerKW;              // power of boiler at full performance
        float MaxBoilerOutputHP;           // Horsepower output of boiler
        float BoilerSteamHeatBTUpLB;    // Steam Heat based on current boiler pressure
        float BoilerWaterHeatBTUpLB;    // Water Heat based on current boiler pressure
        float BoilerSteamDensityLBpFT3; // Steam Density based on current boiler pressure
        float BoilerWaterDensityLBpFT3; // Water Density based on current boiler pressure
        float BoilerWaterTempK;
        public float FuelBurnRateSmoothedKGpS;
        float FuelFeedRateKGpS;
        float DesiredChange;     // Amount of change to increase fire mass, clamped to range 0.0 - 1.0
        public float CylinderSteamUsageLBpS;
        public float NewCylinderSteamUsageLBpS;
        public float BlowerSteamUsageLBpS;
        public float EvaporationLBpS;          // steam generation rate
        public float FireMassKG;      // Mass of coal currently on grate area
        public float FireRatio;     // Ratio of actual firemass to ideal firemass
        float MaxFiringRateLbpH; // Max coal burnt when steam evaporation (production) rate is maximum
        float TempFireHeatLossPercent;
        float FireHeatLossPercent;  // Percentage loss of heat due to too much or too little air for combustion
        float FlueTempK = 775;      // Initial FlueTemp (best @ 475)
        float MaxFlueTempK;         // FlueTemp at full boiler performance
        public bool SafetyIsOn;
        public readonly SmoothedData SmokeColor = new SmoothedData(2);

        // eng file configuration parameters

        float BoilerVolumeFT3;      // total space in boiler that can hold water and steam
        int NumCylinders = 2;       // Number of Cylinders
        float CylinderStrokeM;      // High pressure cylinders
        float CylinderDiameterM;    // High pressure cylinders
        int LPNumCylinders = 2;       // Number of LP Cylinders
        float LPCylinderStrokeM;      // Low pressure cylinders
        float LPCylinderDiameterM;    // Low pressure cylinders
        float CompoundCylinderRatio;    // Compound locomotive - ratio of low pressure to high pressure cylinder
        float MaxBoilerOutputLBpH;  // maximum boiler steam generation rate
        float IdealFireMassKG;      // Target fire mass
        float MaxFireMassKG;        // Max possible fire mass
        float MaxFiringRateKGpS;              // Max rate at which fireman or stoker can can feed coal into fire
        /// <summary>
        /// Max combustion rate of the grate; once this is reached, no more steam is produced.
        /// </summary>
        public float GrateLimitLBpFt2 { get; protected set; } = 150.0f;
        float MaxFuelBurnGrateKGpS;            // Maximum rate of fuel burnt depending upon grate limit
        /// <summary>
        /// Grate combustion rate, i.e. how many lbs coal burnt per sq ft grate area.
        /// </summary>
        public float GrateCombustionRateLBpFt2 { get; protected set; }
        float ORTSMaxFiringRateKGpS;          // OR equivalent of above
        float DisplayMaxFiringRateKGpS;     // Display value of MaxFiringRate
        public float SafetyValveUsageLBpS;
        float SafetyValveBoilerHeatOutBTUpS; // Heat removed by blowing of safety valves.
        float BoilerHeatOutSVAIBTUpS;
        float SafetyValveDropPSI = 4.0f;      // Pressure drop before Safety valve turns off, normally around 4 psi - First safety valve normally operates between MaxBoilerPressure, and MaxBoilerPressure - 4, ie Max Boiler = 200, cutoff = 196.
        float EvaporationAreaM2;
        float SuperheatAreaM2 = 0.0f;      // Heating area of superheater
        float SuperheatKFactor = 15000.0f;     // Factor used to calculate superheat temperature - guesstimate
        float MaxSuperheatRefTempF;            // Maximum Superheat temperature in deg Fahrenheit, based upon the heating area.
        float SuperheatTempRatio;          // A ratio used to calculate the superheat temp - based on the ratio of superheat (using heat area) to "known" curve. 
        public float CurrentSuperheatTempF;      // current value of superheating based upon boiler steam output
        float SuperheatVolumeRatio;   // Approximate ratio of Superheated steam to saturated steam at same pressure
        float SuperheatCutoffPressureFactor; // Factor to adjust cutoff pressure for superheat locomotives, defaults to 55.0, user defineable
        float FuelCalorificKJpKG = 33400;
        float ManBlowerMultiplier = 20.0f; // Blower Multipler for Manual firing
        float ShovelMassKG = 6;
        float FiringSteamUsageRateLBpS;   // rate if excessive usage
        float FullBoilerHeatRatio = 1.0f;   // Boiler heat ratio, if boiler heat exceeds, normal boiler pressure boiler heat
        float MaxBoilerHeatRatio = 1.0f;   // Max Boiler heat ratio, if boiler heat exceeds, safety boiler pressure boiler heat
        float AIFiremanBurnFactor = 1.0f;  // Factor by which to adjust burning (hence heat rate), combination of PressureRatio * BoilerHeatRatio * MaxBoilerHeatRatio
        float AIFiremanBurnFactorExceed = 1.0f;  // Factor by which to adjust burning (hence heat rate) for excessive shoveling, combination of PressureRatio * BoilerHeatRatio * MaxBoilerHeatRatio
        float HeatRatio = 0.001f;        // Ratio to control burn rate - based on ratio of heat in vs heat out
        float PressureRatio = 0.001f;    // Ratio to control burn rate - based upon boiler pressure
        float BurnRateRawKGpS;           // Raw combustion (burn) rate
        float MaxCombustionRateKgpS;
        SmoothedData FuelRateStoker = new SmoothedData(15); // Stoker is more responsive and only takes x seconds to fully react to changing needs.
        SmoothedData FuelRate = new SmoothedData(45); // Automatic fireman takes x seconds to fully react to changing needs.
        SmoothedData BurnRateSmoothKGpS = new SmoothedData(150); // Changes in BurnRate take x seconds to fully react to changing needs - models increase and decrease in heat.
        float FuelRateSmoothed = 0.0f;     // Smoothed Fuel Rate

        // steam performance reporting
        public float SteamPerformanceTimeS = 0.0f; // Records the time since starting movement
        public float CumulativeWaterConsumptionLbs = 0.0f;
        public float CumulativeCylinderSteamConsumptionLbs = 0.0f;
        float CummulativeTotalSteamConsumptionLbs = 0.0f;
        public static float DbfEvalCumulativeWaterConsumptionLbs;//DebriefEval

        int LocoIndex;
        public float LocoTenderFrictionForceN; // Combined friction of locomotive and tender
        public float TotalFrictionForceN;
        public float TrainLoadKg;
        public float LocomotiveCouplerForceN;


        // precomputed values
        float CylinderSweptVolumeFT3pFT;     // Volume of steam Cylinder
        float LPCylinderSweptVolumeFT3pFT;     // Volume of LP steam Cylinder
        float CylinderCondensationFactor;  // Cylinder compensation factor for condensation in cylinder due to cutoff
        float BlowerSteamUsageFactor;
        float InjectorLockOutResetTimeS = 15.0f; // Time to reset the injector lock out time - time to prevent change of injectors
        float InjectorLockOutTimeS = 0.0f; // Current lock out time - reset after Reset Time exceeded 
        float InjectorFlowRateLBpS;    // Current injector flow rate - based upon current boiler pressure
        float MaxInjectorFlowRateLBpS = 0.0f;      // Maximum possible injector flow rate - based upon maximum boiler pressure
        Interpolator BackPressureIHPtoPSI;             // back pressure in cylinders given usage
        Interpolator CylinderSteamDensityPSItoLBpFT3;   // steam density in cylinders given pressure (could be super heated)
        Interpolator WaterDensityPSItoLBpFT3;   // water density given pressure
        Interpolator WaterHeatPSItoBTUpLB;      // total heat in water given pressure
        Interpolator HeatToPressureBTUpLBtoPSI; // pressure given total heat in water (inverse of WaterHeat)
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

        Interpolator SaturatedSpeedFactorSpeedDropFtpMintoX; // Allowance for drop in TE for a saturated locomotive due to piston speed limitations
        Interpolator SuperheatedSpeedFactorSpeedDropFtpMintoX; // Allowance for drop in TE for a superheated locomotive due to piston speed limitations

        Interpolator NewBurnRateSteamToCoalLbspH; // Combustion rate of steam generated per hour to Dry Coal per hour

        Interpolator2D CutoffInitialPressureDropRatioUpper;  // Upper limit of the pressure drop from initial pressure to cut-off pressure
        Interpolator2D CutoffInitialPressureDropRatioLower;  // Lower limit of the pressure drop from initial pressure to cut-off pressure

        Interpolator CylinderExhausttoCutoff;  // Fraction of cylinder travel to exhaust
        Interpolator CylinderCompressiontoCutoff;  // Fraction of cylinder travel to Compression
        Interpolator CylinderAdmissiontoCutoff;  // Fraction of cylinder travel to Admission

        // Heat Radiation Parameters
        float KcInsulation;   // Insulated section of Boiler - Coefficient of thermal conductivity -  BBTU / sq.ft. / hr / l in / °F.
        float KcUninsulation = 1.67f;   // Uninsulated section of Boiler (Steel only) - Coefficient of thermal conductivity -  BBTU / sq.ft. / hr / l in / °F.
        float BoilerSurfaceAreaFt2;
        float FractionBoilerAreaInsulated;
        float BoilerHeatRadiationLossBTU; // Heat loss of boiler (hourly value)
               
        #region Additional steam properties
        const float SpecificHeatCoalKJpKGpK = 1.26f; // specific heat of coal - kJ/kg/K
        const float SteamVaporSpecVolumeAt100DegC1BarM3pKG = 1.696f;
        float WaterHeatBTUpFT3;             // Water heat in btu/ft3
        bool FusiblePlugIsBlown = false;    // Fusible plug blown, due to lack of water in the boiler
        bool LocoIsOilBurner = false;       // Used to identify if loco is oil burner
        float GrateAreaM2;                  // Grate Area in SqM
        float IdealFireDepthIN = 7.0f;      // Assume standard coal coverage of grate = 7 inches.
        float FuelDensityKGpM3 = 864.5f;    // Anthracite Coal : 50 - 58 (lb/ft3), 800 - 929 (kg/m3)
        float DamperFactorManual = 1.0f;    // factor to control draft through fire when locomotive is running in Manual mode
        public float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        public float MaxTenderCoalMassKG = 1;          // Maximum read from Eng File -  - this value must be non-zero, if not defined in ENG file, can cause NaN errors
        public float TenderCoalMassKG              // Decreased by firing and increased by refilling
        {
            get { return FuelController.CurrentValue * MaxTenderCoalMassKG; }
            set { FuelController.CurrentValue = value / MaxTenderCoalMassKG; }
        }

        float DamperBurnEffect;             // Effect of the Damper control Used in manual firing)
        float Injector1Fraction = 0.0f;     // Fraction (0-1) of injector 1 flow from Fireman controller or AI
        float Injector2Fraction = 0.0f;     // Fraction (0-1) of injector  of injector 2 flow from Fireman controller or AI
        float SafetyValveStartPSI = 0.1f;   // Set safety valve to just over max pressure - allows for safety valve not to operate in AI firing
        float InjectorBoilerInputLB = 0.0f; // Input into boiler from injectors
        const float WaterDensityAt100DegC1BarKGpM3 = 954.8f;


        // Steam Ejector
        float TempEjectorSmallSteamConsumptionLbpS;
        float TempEjectorLargeSteamConsumptionLbpS;
        float EjectorTotalSteamConsumptionLbpS;
        public float VacuumPumpOutputFt3pM;

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
        float WaterFraction;        // fraction of boiler volume occupied by water
        float WaterMinLevel = 0.7f;         // min level before we blow the fusible plug
        float WaterMinLevelSafe = 0.75f;    // min level which you would normally want for safety
        float WaterMaxLevel = 0.91f;        // max level above which we start priming
        float WaterMaxLevelSafe = 0.90f;    // max level below which we stop priming
        float WaterGlassMaxLevel = 0.89f;   // max height of water gauge as a fraction of boiler level
        float WaterGlassMinLevel = 0.73f;   // min height of water gauge as a fraction of boiler level
        float WaterGlassLengthIN = 8.0f;    // nominal length of water gauge
        float WaterGlassLevelIN;            // Water glass level in inches
        float waterGlassPercent;            // Water glass level in percent
        float MEPFactor = 0.7f;             // Factor to determine the MEP
        float GrateAreaDesignFactor = 500.0f;   // Design factor for determining Grate Area
        float EvapAreaDesignFactor = 10.0f;     // Design factor for determining Evaporation Area

        float SpecificHeatWaterKJpKGpC; // Specific Heat Capacity of water in boiler (from Interpolator table) kJ/kG per deg C
        float WaterTempInK;              // Input to water Temp Integrator.
        float WaterTempNewK;            // Boiler Water Temp (Kelvin) - for testing purposes
        float BkW_Diff;                 // Net Energy into boiler after steam loads taken.
        float WaterVolL;                // Actual volume of water in bolier (litres)
        float BoilerHeatOutBTUpS = 0.0f;// heat out of boiler in BTU
        /// <summary>
        /// Heat into boiler in BTU
        /// </summary>
        public float BoilerHeatInBTUpS { get; protected set; } = 0.0f;
        float BoilerHeatExcess;         // Vlaue of excess boiler heat
        float InjCylEquivSizeIN;        // Calculate the equivalent cylinder size for purpose of sizing the injector.
        float InjectorSize;             // size of injector installed on boiler

        // Values from previous iteration to use in UpdateFiring() and show in HUD
        public float PreviousBoilerHeatOutBTUpS { get; protected set; } = 0.0f;
        public float PreviousTotalSteamUsageLBpS { get; protected set; }
        float Injector1WaterDelTempF = 65f;   // Injector 1 water delivery temperature - F
        float Injector2WaterDelTempF = 65f;   // Injector 1 water delivery temperature - F
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

        float SuperheaterFactor = 1.0f;               // Currently 2 values respected: 0.0 for no superheat (default), > 1.0 for typical superheat
        public float SuperheaterSteamUsageFactor = 1.0f;       // Below 1.0, reduces steam usage due to superheater
        float Stoker = 0.0f;                // Currently 2 values respected: 0.0 for no mechanical stoker (default), = 1.0 for typical mechanical stoker
        float StokerMaxUsage = 0.01f;       // Max steam usage of stoker - 1% of max boiler output
        float StokerMinUsage = 0.005f;      // Min Steam usage - just to keep motor ticking over - 0.5% of max boiler output
        float StokerSteamUsageLBpS;         // Current steam usage of stoker
        float MaxTheoreticalFiringRateKgpS;     // Max firing rate that fireman can sustain for short periods
        float FuelBoostOnTimerS = 0.01f;    // Timer to allow fuel boosting for a short while
        float FuelBoostResetTimerS = 0.01f; // Timer to rest fuel boosting for a while
        float TimeFuelBoostOnS = 300.0f;    // Time to allow fuel boosting to go on for 
        float TimeFuelBoostResetS = 1200.0f;// Time to wait before next fuel boost
        float throttle;
        float SpeedEquivMpS = 27.0f;          // Equvalent speed of 60mph in mps (27m/s) - used for damper control

        // Cylinder related parameters
        public float CutoffPressureDropRatio;  // Ratio of Cutoff Pressure to Initial Pressure
        public float LPCutoffPressureDropRatio; // Ratio of Cutoff Pressure to Initial Pressure - LP Cylinder
        float CylinderCocksPressureAtmPSI; // Pressure in cylinder (impacted by cylinder cocks).
        float Pressure_d_AtmPSI;
        float Pressure_a_AtmPSI;    // Initial Pressure to cylinder @ start if stroke
        float Pressure_b_AtmPSI;    // Pressure at cutoff
        float SteamChestPressurePSI;    // Pressure in steam chest - input to cylinder

        float CylinderWork_ab_InLbs; // Work done during steam admission into cylinder
        float CylinderExhaustOpenFactor; // Point on cylinder stroke when exhaust valve opens.
        float CylinderCompressionCloseFactor; // Point on cylinder stroke when compression valve closes - assumed reciporical of exhaust opens.
        float CylinderAdmissionOpenFactor = 0.05f; // Point on cylinder stroke when pre-admission valve opens
        float Pressure_c_AtmPSI;       // Pressure when exhaust valve opens
        float Pressure_e_AtmPSI;       // Pressure when exhaust valve closes
        float Pressure_f_AtmPSI;    // Pressure after compression occurs and steam admission starts
        float CylinderWork_bc_InLbs; // Work done during expansion stage of cylinder
        float CylinderWork_cd_InLbs;   // Work done during release stage of cylinder
        float CylinderWork_ef_InLbs; // Work done during compression stage of cylinder
        float CylinderWork_fa_InLbs; // Work done during PreAdmission stage of cylinder
        float CylinderWork_de_InLbs; // Work done during Exhaust stage of cylinder

        // Values for logging and displaying Steam pressure
        public float LogInitialPressurePSI;
        public float LogCutoffPressurePSI;
        public float LogBackPressurePSI;
        public float LogReleasePressurePSI;
        public float LogSteamChestPressurePSI;

        // Values for Steam Cylinder events
        // Commented out as never used
        //float ValveTravel = 10.8268f;
        //float ValveLead = 0.275591f;
        //float ValveExhLap = 0.708661f;
        //float ValveSteamLap;
        //double ValveAdvanceAngleDeg;

        public float LogLPInitialPressurePSI;
        public float LogLPCutoffPressurePSI;
        public float LogLPBackPressurePSI;
        public float LogLPReleasePressurePSI;
        public float LogLPSteamChestPressurePSI;

        public bool LogIsCompoundLoco = false;

        float LogPreCompressionPressurePSI;
        float LogPreAdmissionPressurePSI;

        // Compound Cylinder Information - HP Cylinder - Compound Operation

        float HPCompPressure_a_AtmPSI;    // Initial Pressure to HP cylinder @ start if stroke
        float HPCompPressure_b_AtmPSI;    // Pressure at HP cylinder cutoff
        float HPCompPressure_d_AtmPSI;       // Pressure in HP cylinder when steam release valve opens
        float HPCompPressure_e_AtmPSI;   // Pressure in HP cylinder when steam release valve opens, and steam moves into steam passages which connect HP & LP together
        float HPCompPressure_f_AtmPSI;       // Pressure in HP cylinder when steam completely released from the cylinder
        float HPCompPressure_h_AtmPSI;       // Pressure when exhaust valve closes, and compression commences
        float HPCompPressure_k_AtmPSI; //  Pre-Admission pressure prior to exhaust valve closing
        float HPCompPressure_u_AtmPSI;  // Admission pressure
        float HPCompMeanPressure_gh_AtmPSI;     // Back pressure on HP cylinder
        public float HPCylinderMEPPSI;                 // Mean effective Pressure of HP Cylinder
        float HPCylinderClearancePC = 0.19f;    // Assume cylinder clearance of 19% of the piston displacement for HP cylinder
        float CompoundRecieverVolumePCHP = 0.3f; // Volume of receiver or passages between HP and LP cylinder as a fraction of the HP cylinder volume.
        float HPCylinderVolumeFactor = 1.0f;    // Represents the full volume of the HP steam cylinder    
        float LPCylinderVolumeFactor = 1.0f;    // Represents the full volume of the LP steam cylinder 
        float HPIndicatedHorsePowerHP;
        float LPIndicatedHorsePowerHP;

        // Compound Cylinder Information - LP Cylinder - Simple Operation
        float LPPressure_a_AtmPSI;    // Initial Pressure to LP cylinder @ start if stroke
        float LPPressure_b_AtmPSI; // Pressure in combined HP & LP Cylinder pre-cutoff
        float LPPressure_c_AtmPSI;   // Pressure in LP cylinder when steam release valve opens
        float LPPressure_d_AtmPSI;     // Back pressure on LP cylinder
        float LPPressure_e_AtmPSI;       // Pressure in LP cylinder when exhaust valve closes, and compression commences
        float LPPressure_f_AtmPSI;    // Pressure in LP cylinder after compression occurs and steam admission starts
        public float LPCylinderMEPPSI;                     // Mean effective pressure of LP Cylinder
        float LPCylinderClearancePC = 0.066f;    // Assume cylinder clearance of 6.6% of the piston displacement for LP cylinder

        // Simple locomotive cylinder information
        public float MeanEffectivePressurePSI;         // Mean effective pressure
        float RatioOfExpansion_bc;             // Ratio of expansion
        float CylinderClearancePC = 0.09f;    // Assume cylinder clearance of 8% of the piston displacement for saturated locomotives and 9% for superheated locomotive - default to saturated locomotive value
        float CylinderPortOpeningFactor;   // Model the size of the steam port opening in the cylinder - set to 0.085 as default, if no ENG file value added
        float CylinderPortOpeningUpper = 0.12f; // Set upper limit for Cylinder port opening
        float CylinderPortOpeningLower = 0.05f; // Set lower limit for Cylinder port opening
        float CylinderPistonShaftFt3;   // Volume taken up by the cylinder piston shaft
        float CylinderPistonShaftDiaIn = 3.5f; // Assume cylinder piston shaft to be 3.5 inches
        float CylinderPistonAreaFt2;    // Area of the piston in the cylinder (& HP Cylinder in case of Compound locomotive)
        float LPCylinderPistonAreaFt2;    // Area of the piston in the LP cylinder
        float CylinderAdmissionSteamWeightLbs; // Weight of steam remaining in cylinder at "admission" (when admission valve opens)
        float CylinderReleaseSteamWeightLbs;   // Weight of steam in cylinder at "release" (exhaust valve opens)
        float CylinderReleaseSteamVolumeFt3; // Volume of cylinder at steam release in cylinder
        float CylinderAdmissionSteamVolumeFt3; // Volume in cylinder at start of steam compression
        float RawCylinderSteamWeightLbs;    // 
        float RawCalculatedCylinderSteamUsageLBpS;  // Steam usage before superheat or cylinder condensation compensation
        float CalculatedCylinderSteamUsageLBpS; // Steam usage calculated from steam indicator diagram

        const int CylStrokesPerCycle = 2;  // each cylinder does 2 strokes for every wheel rotation, within each stroke
        float CylinderEfficiencyRate = 1.0f; // Factor to vary the output power of the cylinder without changing steam usage - used as a player customisation factor.
        public float CylCockSteamUsageLBpS = 0.0f; // Cylinder Cock Steam Usage if locomotive moving
        public float CylCockSteamUsageStatLBpS = 0.0f; // Cylinder Cock Steam Usage if locomotive stationary
        public float CylCockSteamUsageDisplayLBpS = 0.0f; // Cylinder Cock Steam Usage for display and effects
        float CylCockDiaIN = 0.5f;          // Steam Cylinder Cock orifice size
        float CylCockPressReduceFactor;     // Factor to reduce cylinder pressure by if cocks open
        float CylCockBoilerHeatOutBTUpS;  // Amount of heat taken out by use of cylinder cocks

        float DrvWheelDiaM;     // Diameter of driver wheel
        float DrvWheelRevRpS;       // number of revolutions of the drive wheel per minute based upon speed.
        float PistonSpeedFtpMin;      // Piston speed of locomotive
        public float IndicatedHorsePowerHP;   // Indicated Horse Power (IHP), theoretical power of the locomotive, it doesn't take into account the losses due to friction, etc. Typically output HP will be 70 - 90% of the IHP
        public float DrawbarHorsePowerHP;  // Drawbar Horse Power  (DHP), maximum power available at the wheels.
        public float DrawBarPullLbsF;      // Drawbar pull in lbf
        float BoilerEvapRateLbspFt2;  // Sets the evaporation rate for the boiler is used to multiple boiler evaporation area by - used as a player customisation factor.

        float MaxSpeedFactor;      // Max Speed factor - factor @ critical piston speed to reduce TE due to speed increase - American locomotive company
        float DisplaySpeedFactor;  // Value displayed in HUD

        public float MaxTractiveEffortLbf;     // Maximum theoritical tractive effort for locomotive
        public float DisplayMaxTractiveEffortLbf;     // HuD display value of maximum theoritical tractive effort for locomotive

        float DisplayTractiveEffortLbsF; // Value of Tractive effort to display in HUD
        float MaxCriticalSpeedTractiveEffortLbf;  // Maximum power value @ critical speed of piston
        float DisplayCriticalSpeedTractiveEffortLbf;  // Display power value @ speed of piston
        float absStartTractiveEffortN = 0.0f;      // Record starting tractive effort
        float TractiveEffortLbsF;           // Current sim calculated tractive effort
        const float TractiveEffortFactor = 0.85f;  // factor for calculating Theoretical Tractive Effort for non-geared locomotives
        float GearedTractiveEffortFactor = 0.7f;  // factor for calculating Theoretical Tractive Effort for geared locomotives
        float NeutralGearedDavisAN; // Davis A value adjusted for neutral gearing
        const float DavisMechanicalResistanceFactor = 20.0f;
        float GearedRetainedDavisAN; // Remembers the Davis A value

        float MaxLocoSpeedMpH;      // Speed of loco when max performance reached
        float DisplayMaxLocoSpeedMpH;      // Display value of speed of loco when max performance reached
        float MaxPistonSpeedFtpM;   // Piston speed @ max performance for the locomotive
        float MaxIndicatedHorsePowerHP; // IHP @ max performance for the locomotive
        float DisplayMaxIndicatedHorsePowerHP; // Display value for HUD of IHP @ max performance for the geared locomotive
        float RetainedGearedMaxMaxIndicatedHorsePowerHP; // Retrains maximum IHP value for steam locomotives.
        float absSpeedMpS;
        float CombFrictionN;  // Temporary parameter to store combined friction values of locomotive and tender
        float CombGravityN;   // Temporary parameter to store combined Gravity values of locomotive and tender
        float CombTunnelN;    // Temporary parameter to store combined Tunnel values of locomotive and tender
        float CombCurveN;     // Temporary parameter to store combined Curve values of locomotive and tender
        float CombWindN;     // Temporary parameter to store combined Curve values of locomotive and tender

        float cutoff;
        float NumSafetyValves;  // Number of safety valves fitted to locomotive - typically 1 to 4
        float SafetyValveSizeIn;    // Size of the safety value - all will be the same size.
        float SafetyValveSizeDiaIn2; // Area of the safety valve - impacts steam discharge rate - is the space when the valve lifts
        float MaxSafetyValveDischargeLbspS; // Steam discharge rate of all safety valves combined.
        float SafetyValveUsage1LBpS; // Usage rate for safety valve #1
        float SafetyValveUsage2LBpS; // Usage rate for safety valve #2
        float SafetyValveUsage3LBpS; // Usage rate for safety valve #3
        float SafetyValveUsage4LBpS; // Usage rate for safety valve #4
        float MaxSteamGearPistonRateFtpM;   // Max piston rate for a geared locomotive, such as a Shay
        float SteamGearRatio;   // Gear ratio for a geared locomotive, such as a Shay  
        float SteamGearRatioLow;   // Gear ratio for a geared locomotive, such as a Shay
        float SteamGearRatioHigh;   // Gear ratio for a two speed geared locomotive, such as a Climax
        float LowMaxGearedSpeedMpS;  // Max speed of the geared locomotive - Low Gear
        float HighMaxGearedSpeedMpS; // Max speed of the geared locomotive - High Gear
        float MotiveForceGearRatio; // mulitplication factor to be used in calculating motive force etc, when a geared locomotive.
        float SteamGearPosition = 0.0f; // Position of Gears if set

        // Rotative Force and adhesion

        float CalculatedFactorofAdhesion; // Calculated factor of adhesion
        float StartTangentialCrankWheelForceLbf; 		// Tangential force on wheel - at start
        float SpeedTotalTangCrankWheelForceLbf; 		// Tangential force on wheel - at speed
        float StartStaticWheelFrictionForceLbf;  // Static force on wheel due to adhesion
        float SpeedStaticWheelFrictionForceLbf;  // Static force on wheel  - at speed
        float StartPistonForceLeftLbf;    // Max force exerted by piston.
        float StartPistonForceMiddleLbf;
        float StartPistonForceRightLbf;
        float StartTangentialWheelTreadForceLbf; // Tangential force at the wheel tread.
        float SpeedTangentialWheelTreadForceLbf;
        float ReciprocatingWeightLb = 580.0f;  // Weight of reciprocating parts of the rod driving gears
        float ConnectingRodWeightLb = 600.0f;  // Weignt of connecting rod
        float ConnectingRodBalanceWeightLb = 300.0f; // Balance weight for connecting rods
        float ExcessBalanceFactor = 400.0f;  // Factor to be included in excess balance formula
        float CrankRadiusFt = 1.08f;        // Assume crank and rod lengths to give a 1:10 ratio - a reasonable av for steam locomotives?
        float ConnectRodLengthFt = 10.8f;
        float RodCoGFt = 3.0f;
        float CrankLeftCylinderPressure;
        float CrankMiddleCylinderPressure;
        float CrankRightCylinderPressure;
        float RadConvert = (float)Math.PI / 180.0f;  // Conversion of degs to radians
        float StartCrankAngleLeft;
        float StartCrankAngleRight;
        float StartCrankAngleMiddle;
        float StartTangentialCrankForceFactorLeft;
        float StartTangentialCrankForceFactorMiddle = 0.0f;
        float StartTangentialCrankForceFactorRight;
        float SpeedTangentialCrankForceFactorLeft;
        float SpeedTangentialCrankForceFactorMiddle;
        float SpeedTangentialCrankForceFactorRight;
        float SpeedPistonForceLeftLbf;
        float SpeedPistonForceMiddleLbf;
        float SpeedPistonForceRightLbf;
        float SpeedTangentialCrankWheelForceLeftLbf;
        float SpeedTangentialCrankWheelForceMiddleLbf;
        float SpeedTangentialCrankWheelForceRightLbf;
        float StartVerticalThrustFactorLeft;
        float StartVerticalThrustFactorMiddle;
        float StartVerticalThrustFactorRight;
        float SpeedVerticalThrustFactorLeft;
        float SpeedVerticalThrustFactorMiddle;
        float SpeedVerticalThrustFactorRight;
        float StartVerticalThrustForceMiddle;
        float StartVerticalThrustForceLeft;
        float StartVerticalThrustForceRight;
        float SpeedVerticalThrustForceLeft;
        float SpeedVerticalThrustForceMiddle;
        float SpeedVerticalThrustForceRight;
        float SpeedCrankAngleLeft;
        float SpeedCrankAngleRight;
        float SpeedCrankAngleMiddle;
        float CrankCylinderPositionLeft;
        float CrankCylinderPositionMiddle;
        float CrankCylinderPositionRight;
        float ExcessBalanceForceLeft;
        float ExcessBalanceForceMiddle;
        float ExcessBalanceForceRight;
        float FrictionWheelSpeedMpS; // Tangential speed of wheel rim
        float PrevFrictionWheelSpeedMpS; // Previous tangential speed of wheel rim

        #endregion

        #region Variables for visual effects (steam, smoke)

        public readonly SmoothedData StackSteamVelocityMpS = new SmoothedData(2);
        public float StackSteamVolumeM3pS;
        public float Cylinders1SteamVelocityMpS;
        public float Cylinders1SteamVolumeM3pS;
        public float Cylinders2SteamVelocityMpS;
        public float Cylinders2SteamVolumeM3pS;
        public float SafetyValvesSteamVelocityMpS;
        public float SafetyValvesSteamVolumeM3pS;

        public float BlowdownSteamVolumeM3pS;
        public float BlowdownSteamVelocityMpS;
        public float BlowdownParticleDurationS = 3.0f;

        public float DrainpipeSteamVolumeM3pS;
        public float DrainpipeSteamVelocityMpS;
        public float Injector1SteamVolumeM3pS;
        public float Injector1SteamVelocityMpS;
        public float Injector2SteamVolumeM3pS;
        public float Injector2SteamVelocityMpS;

        public float SmallEjectorSteamVolumeM3pS;
        public float SmallEjectorSteamVelocityMpS;
        public float SmallEjectorParticleDurationS = 3.0f;

        public float LargeEjectorSteamVolumeM3pS;
        public float LargeEjectorSteamVelocityMpS;
        public float LargeEjectorParticleDurationS = 3.0f;

        public float CompressorSteamVolumeM3pS;
        public float CompressorSteamVelocityMpS;
        public float GeneratorSteamVolumeM3pS;
        public float GeneratorSteamVelocityMpS;
        public float WhistleSteamVolumeM3pS;
        public float WhistleSteamVelocityMpS;
        float CylinderCockTimerS = 0.0f;
        float CylinderCockOpenTimeS = 0.0f;
        bool CylinderCock1On = true;
        bool CylinderCock2On = false;
        public bool Cylinder2SteamEffects = false;
        public bool GeneratorSteamEffects = false;
        public float CompressorParticleDurationS = 3.0f;
        public float Cylinder1ParticleDurationS = 3.0f;
        public float Cylinder2ParticleDurationS = 3.0f;
        public float WhistleParticleDurationS = 3.0f;
        public float SafetyValvesParticleDurationS = 3.0f;
        public float DrainpipeParticleDurationS = 3.0f;
        public float Injector1ParticleDurationS = 3.0f;
        public float Injector2ParticleDurationS = 3.0f;
        public float GeneratorParticleDurationS = 3.0f;

        #endregion

        public MSTSSteamLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            RefillTenderWithCoal();
            RefillTenderWithWater();
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

        /// <summary>
        /// Adjusts the fuel controller to initial coal mass.
        /// </summary>
        public void InitializeTenderWithCoal()
        {
            FuelController.CurrentValue = TenderCoalMassKG / MaxTenderCoalMassKG;
        }

        /// <summary>
        /// Adjusts the water controller to initial water volume.
        /// </summary>  
        public void InitializeTenderWithWater()
        {
            WaterController.CurrentValue = CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;
        }

        private bool ZeroError(float v, string name)
        {
            if (v > 0)
                return false;
            Trace.TraceWarning("Steam engine value {1} must be defined and greater than zero in {0}", WagFilePath, name);
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
                case "engine(lpnumcylinders": LPNumCylinders = stf.ReadIntBlock(null); break;
                case "engine(lpcylinderstroke": LPCylinderStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(lpcylinderdiameter": LPCylinderDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(ortscylinderportopening": CylinderPortOpeningFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(boilervolume": BoilerVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null); break;
                case "engine(maxboilerpressure": MaxBoilerPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(ortsmaxsuperheattemperature": MaxSuperheatRefTempF = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;  // New input and conversion units to be added for temperature
                case "engine(ortsmaxindicatedhorsepower": MaxIndicatedHorsePowerHP = stf.ReadFloatBlock(STFReader.UNITS.Power, null);
                    MaxIndicatedHorsePowerHP = W.ToHp(MaxIndicatedHorsePowerHP);  // Convert input to HP for use internally in this module
                    break;
                case "engine(vacuumbrakeslargeejectorusagerate": EjectorLargeSteamConsumptionLbpS = pS.FrompH(stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null)); break;
                case "engine(vacuumbrakessmallejectorusagerate": EjectorSmallSteamConsumptionLbpS = pS.FrompH(stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null)); break;
                case "engine(ortssuperheatcutoffpressurefactor": SuperheatCutoffPressureFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(shovelcoalmass": ShovelMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtendercoalmass": MaxTenderCoalMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtenderwatermass": MaxLocoTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(steamfiremanmaxpossiblefiringrate": MaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(steamfiremanismechanicalstoker": Stoker = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssteamfiremanmaxpossiblefiringrate": ORTSMaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(enginecontrollers(cutoff": CutoffController.Parse(stf); break;
                case "engine(enginecontrollers(ortssmallejector": SmallEjectorController.Parse(stf); SmallEjectorControllerFitted = true; break;
                case "engine(enginecontrollers(ortslargeejector": LargeEjectorController.Parse(stf); LargeEjectorControllerFitted = true; break;
                case "engine(enginecontrollers(injector1water": Injector1Controller.Parse(stf); break;
                case "engine(enginecontrollers(injector2water": Injector2Controller.Parse(stf); break;
                case "engine(enginecontrollers(blower": BlowerController.Parse(stf); break;
                case "engine(enginecontrollers(dampersfront": DamperController.Parse(stf); break;
                case "engine(enginecontrollers(shovel": FiringRateController.Parse(stf); break;
                case "engine(enginecontrollers(firedoor": FireboxDoorController.Parse(stf); break;
                case "engine(effects(steamspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(ortsgratearea": GrateAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(superheater": SuperheaterFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(istenderrequired": IsTenderRequired = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsevaporationarea": EvaporationAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(ortsboilersurfacearea": BoilerSurfaceAreaFt2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(ortsfractionboilerinsulated": FractionBoilerAreaInsulated = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsheatcoefficientinsulation": KcInsulation = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssuperheatarea": SuperheatAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "engine(ortsfuelcalorific": FuelCalorificKJpKG = stf.ReadFloatBlock(STFReader.UNITS.EnergyDensity, null); break;
                case "engine(ortsboilerevaporationrate": BoilerEvapRateLbspFt2 = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderefficiencyrate": CylinderEfficiencyRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderinitialpressuredrop": InitialPressureDropRatioRpMtoX = new Interpolator(stf); break;
                case "engine(ortscylinderbackpressure": BackPressureIHPtoPSI = new Interpolator(stf); break;
                case "engine(ortsburnrate": NewBurnRateSteamToCoalLbspH = new Interpolator(stf); break;
                case "engine(ortsboilerefficiency": BoilerEfficiencyGrateAreaLBpFT2toX = new Interpolator(stf); break;
                case "engine(ortscylindereventexhaust": CylinderExhausttoCutoff = new Interpolator(stf); break;
                case "engine(ortscylindereventcompression": CylinderCompressiontoCutoff = new Interpolator(stf); break;
                case "engine(ortscylindereventadmission": CylinderAdmissiontoCutoff = new Interpolator(stf); break;
                case "engine(ortssteamgearratio":
                    stf.MustMatch("(");
                    SteamGearRatioLow = stf.ReadFloat(STFReader.UNITS.None, null);
                    SteamGearRatioHigh = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "engine(ortssteammaxgearpistonrate": MaxSteamGearPistonRateFtpM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsgearedtractiveeffortfactor": GearedTractiveEffortFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssteamlocomotivetype":
                    stf.MustMatch("(");
                    var steamengineType = stf.ReadString();
                    try
                    {
                        SteamEngineType = (SteamEngineTypes)Enum.Parse(typeof(SteamEngineTypes), steamengineType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown engine type " + steamengineType);
                    }
                    break;
                case "engine(ortssteamboilertype":
                    stf.MustMatch("(");
                    string typeString1 = stf.ReadString();
                    IsSaturated = String.Compare(typeString1, "Saturated") == 0;
                    HasSuperheater = String.Compare(typeString1, "Superheated") == 0;
                    break;
                case "engine(ortssteamgeartype":
                    stf.MustMatch("(");
                    string typeString2 = stf.ReadString();
                    IsFixGeared = String.Compare(typeString2, "Fixed") == 0;
                    IsSelectGeared = String.Compare(typeString2, "Select") == 0;
                    break;

                default: base.Parse(lowercasetoken, stf); break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            MSTSSteamLocomotive locoCopy = (MSTSSteamLocomotive)copy;
            NumCylinders = locoCopy.NumCylinders;
            CylinderStrokeM = locoCopy.CylinderStrokeM;
            CylinderDiameterM = locoCopy.CylinderDiameterM;
            LPNumCylinders = locoCopy.LPNumCylinders;
            LPCylinderStrokeM = locoCopy.LPCylinderStrokeM;
            LPCylinderDiameterM = locoCopy.LPCylinderDiameterM;
            CylinderExhaustOpenFactor = locoCopy.CylinderExhaustOpenFactor;
            CylinderPortOpeningFactor = locoCopy.CylinderPortOpeningFactor;
            BoilerVolumeFT3 = locoCopy.BoilerVolumeFT3;
            MaxBoilerPressurePSI = locoCopy.MaxBoilerPressurePSI;
            MaxSuperheatRefTempF = locoCopy.MaxSuperheatRefTempF;
            MaxIndicatedHorsePowerHP = locoCopy.MaxIndicatedHorsePowerHP;
            SuperheatCutoffPressureFactor = locoCopy.SuperheatCutoffPressureFactor;
            EjectorSmallSteamConsumptionLbpS = locoCopy.EjectorSmallSteamConsumptionLbpS;
            EjectorLargeSteamConsumptionLbpS = locoCopy.EjectorLargeSteamConsumptionLbpS;
            ShovelMassKG = locoCopy.ShovelMassKG;
            GearedTractiveEffortFactor = locoCopy.GearedTractiveEffortFactor;
            MaxTenderCoalMassKG = locoCopy.MaxTenderCoalMassKG;
            MaxLocoTenderWaterMassKG = locoCopy.MaxLocoTenderWaterMassKG;
            MaxFiringRateKGpS = locoCopy.MaxFiringRateKGpS;
            Stoker = locoCopy.Stoker;
            ORTSMaxFiringRateKGpS = locoCopy.ORTSMaxFiringRateKGpS;
            CutoffController = (MSTSNotchController)locoCopy.CutoffController.Clone();
            Injector1Controller = (MSTSNotchController)locoCopy.Injector1Controller.Clone();
            Injector2Controller = (MSTSNotchController)locoCopy.Injector2Controller.Clone();
            BlowerController = (MSTSNotchController)locoCopy.BlowerController.Clone();
            DamperController = (MSTSNotchController)locoCopy.DamperController.Clone();
            FiringRateController = (MSTSNotchController)locoCopy.FiringRateController.Clone();
            FireboxDoorController = (MSTSNotchController)locoCopy.FireboxDoorController.Clone();
            SmallEjectorController = (MSTSNotchController)locoCopy.SmallEjectorController.Clone();
            LargeEjectorController = (MSTSNotchController)locoCopy.LargeEjectorController.Clone();
            GrateAreaM2 = locoCopy.GrateAreaM2;
            SuperheaterFactor = locoCopy.SuperheaterFactor;
            EvaporationAreaM2 = locoCopy.EvaporationAreaM2;
            BoilerSurfaceAreaFt2 = locoCopy.BoilerSurfaceAreaFt2;
            FractionBoilerAreaInsulated = locoCopy.FractionBoilerAreaInsulated;
            KcInsulation = locoCopy.KcInsulation;
            SuperheatAreaM2 = locoCopy.SuperheatAreaM2;
            FuelCalorificKJpKG = locoCopy.FuelCalorificKJpKG;
            BoilerEvapRateLbspFt2 = locoCopy.BoilerEvapRateLbspFt2;
            CylinderEfficiencyRate = locoCopy.CylinderEfficiencyRate;
            InitialPressureDropRatioRpMtoX = new Interpolator(locoCopy.InitialPressureDropRatioRpMtoX);
            BackPressureIHPtoPSI = new Interpolator(locoCopy.BackPressureIHPtoPSI);
            NewBurnRateSteamToCoalLbspH = new Interpolator(locoCopy.NewBurnRateSteamToCoalLbspH);
            BoilerEfficiency = locoCopy.BoilerEfficiency;
            SteamGearRatioLow = locoCopy.SteamGearRatioLow;
            SteamGearRatioHigh = locoCopy.SteamGearRatioHigh;
            MaxSteamGearPistonRateFtpM = locoCopy.MaxSteamGearPistonRateFtpM;
            SteamEngineType = locoCopy.SteamEngineType;
            IsSaturated = locoCopy.IsSaturated;
            IsTenderRequired = locoCopy.IsTenderRequired;
            HasSuperheater = locoCopy.HasSuperheater;
            IsFixGeared = locoCopy.IsFixGeared;
            IsSelectGeared = locoCopy.IsSelectGeared;
            LargeEjectorControllerFitted = locoCopy.LargeEjectorControllerFitted;
            CylinderExhausttoCutoff = locoCopy.CylinderExhausttoCutoff;
            CylinderCompressiontoCutoff = locoCopy.CylinderCompressiontoCutoff;
            CylinderAdmissiontoCutoff = locoCopy.CylinderAdmissiontoCutoff;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(RestoredGame);
            outf.Write(BoilerHeatOutBTUpS);
            outf.Write(BoilerHeatInBTUpS);
            outf.Write(PreviousBoilerHeatOutBTUpS);
            outf.Write(PreviousBoilerHeatSmoothedBTU);
            outf.Write(BurnRateRawKGpS);
            outf.Write(TenderCoalMassKG);
            outf.Write(RestoredMaxTotalCombinedWaterVolumeUKG);
            outf.Write(RestoredCombinedTenderWaterVolumeUKG);
            outf.Write(CumulativeWaterConsumptionLbs);
            outf.Write(CurrentAuxTenderWaterVolumeUKG);
            outf.Write(CurrentLocoTenderWaterVolumeUKG);
            outf.Write(PreviousTenderWaterVolumeUKG);
            outf.Write(SteamIsAuxTenderCoupled);
            outf.Write(CylinderSteamUsageLBpS);
            outf.Write(BoilerHeatBTU);
            outf.Write(BoilerMassLB);
            outf.Write(BoilerPressurePSI);
            outf.Write(CoalIsExhausted);
            outf.Write(WaterIsExhausted);
            outf.Write(FireIsExhausted);
            outf.Write(FuelBoost);
            outf.Write(FuelBoostOnTimerS);
            outf.Write(FuelBoostResetTimerS);
            outf.Write(FuelBoostReset);
            outf.Write(Injector1IsOn);
            outf.Write(Injector1Fraction);
            outf.Write(Injector2IsOn);
            outf.Write(Injector2Fraction);
            outf.Write(InjectorLockedOut);
            outf.Write(InjectorLockOutTimeS);
            outf.Write(InjectorLockOutResetTimeS);
            outf.Write(WaterTempNewK);
            outf.Write(BkW_Diff);
            outf.Write(WaterFraction);
            outf.Write(BoilerSteamHeatBTUpLB);
            outf.Write(BoilerWaterHeatBTUpLB);
            outf.Write(BoilerWaterDensityLBpFT3);
            outf.Write(BoilerSteamDensityLBpFT3);
            outf.Write(EvaporationLBpS);
            outf.Write(FireMassKG);
            outf.Write(FlueTempK);
            outf.Write(SteamGearPosition);
            ControllerFactory.Save(CutoffController, outf);
            ControllerFactory.Save(Injector1Controller, outf);
            ControllerFactory.Save(Injector2Controller, outf);
            ControllerFactory.Save(BlowerController, outf);
            ControllerFactory.Save(DamperController, outf);
            ControllerFactory.Save(FireboxDoorController, outf);
            ControllerFactory.Save(FiringRateController, outf);
            ControllerFactory.Save(SmallEjectorController, outf);
            ControllerFactory.Save(LargeEjectorController, outf);
            outf.Write(FuelBurnRateSmoothedKGpS);
            outf.Write(BoilerHeatSmoothedBTU);
            outf.Write(FuelRateSmoothed);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            RestoredGame = inf.ReadBoolean();
            BoilerHeatOutBTUpS = inf.ReadSingle();
            BoilerHeatInBTUpS = inf.ReadSingle();
            PreviousBoilerHeatOutBTUpS = inf.ReadSingle();
            PreviousBoilerHeatSmoothedBTU = inf.ReadSingle();
            BurnRateRawKGpS = inf.ReadSingle();
            TenderCoalMassKG = inf.ReadSingle();
            RestoredMaxTotalCombinedWaterVolumeUKG = inf.ReadSingle();
            RestoredCombinedTenderWaterVolumeUKG = inf.ReadSingle();
            CumulativeWaterConsumptionLbs = inf.ReadSingle();
            CurrentAuxTenderWaterVolumeUKG = inf.ReadSingle();
            CurrentLocoTenderWaterVolumeUKG = inf.ReadSingle();
            PreviousTenderWaterVolumeUKG = inf.ReadSingle();
            SteamIsAuxTenderCoupled = inf.ReadBoolean();
            CylinderSteamUsageLBpS = inf.ReadSingle();
            BoilerHeatBTU = inf.ReadSingle();
            BoilerMassLB = inf.ReadSingle();
            BoilerPressurePSI = inf.ReadSingle();
            CoalIsExhausted = inf.ReadBoolean();
            WaterIsExhausted = inf.ReadBoolean();
            FireIsExhausted = inf.ReadBoolean();
            FuelBoost = inf.ReadBoolean();
            FuelBoostOnTimerS = inf.ReadSingle();
            FuelBoostResetTimerS = inf.ReadSingle();
            FuelBoostReset = inf.ReadBoolean();
            Injector1IsOn = inf.ReadBoolean();
            Injector1Fraction = inf.ReadSingle();
            Injector2IsOn = inf.ReadBoolean();
            Injector2Fraction = inf.ReadSingle();
            InjectorLockedOut = inf.ReadBoolean();
            InjectorLockOutTimeS = inf.ReadSingle();
            InjectorLockOutResetTimeS = inf.ReadSingle();
            WaterTempNewK = inf.ReadSingle();
            BkW_Diff = inf.ReadSingle();
            WaterFraction = inf.ReadSingle();
            BoilerSteamHeatBTUpLB = inf.ReadSingle();
            BoilerWaterHeatBTUpLB = inf.ReadSingle();
            BoilerWaterDensityLBpFT3 = inf.ReadSingle();
            BoilerSteamDensityLBpFT3 = inf.ReadSingle();
            EvaporationLBpS = inf.ReadSingle();
            FireMassKG = inf.ReadSingle();
            FlueTempK = inf.ReadSingle();
            SteamGearPosition = inf.ReadSingle();
            ControllerFactory.Restore(CutoffController, inf);
            ControllerFactory.Restore(Injector1Controller, inf);
            ControllerFactory.Restore(Injector2Controller, inf);
            ControllerFactory.Restore(BlowerController, inf);
            ControllerFactory.Restore(DamperController, inf);
            ControllerFactory.Restore(FireboxDoorController, inf);
            ControllerFactory.Restore(FiringRateController, inf);
            ControllerFactory.Restore(SmallEjectorController, inf);
            ControllerFactory.Restore(LargeEjectorController, inf);
            FuelBurnRateSmoothedKGpS = inf.ReadSingle();
            BurnRateSmoothKGpS.ForceSmoothValue(FuelBurnRateSmoothedKGpS);
            BoilerHeatSmoothedBTU = inf.ReadSingle();
            BoilerHeatSmoothBTU.ForceSmoothValue(BoilerHeatSmoothedBTU);
            FuelRateSmoothed = inf.ReadSingle();
            FuelRate.ForceSmoothValue(FuelRateSmoothed);
            base.Restore(inf);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (NumCylinders < 0 && ZeroError(NumCylinders, "NumCylinders"))
                NumCylinders = 0;
            if (ZeroError(CylinderDiameterM, "CylinderDiammeter"))
                CylinderDiameterM = 1;
            if (ZeroError(CylinderStrokeM, "CylinderStroke"))
                CylinderStrokeM = 1;
            if (ZeroError(DriverWheelRadiusM, "WheelRadius"))
                DriverWheelRadiusM = Me.FromIn(30.0f); // Wheel radius of loco drive wheels can be anywhere from about 10" to 40"
            if (ZeroError(MaxBoilerPressurePSI, "MaxBoilerPressure"))
                MaxBoilerPressurePSI = 1;
            if (ZeroError(BoilerVolumeFT3, "BoilerVolume"))
                BoilerVolumeFT3 = 1;

            // For light locomotives reduce the weight of the various connecting rods, as the default values are for larger locomotives. This will reduce slip on small locomotives
            // It is not believed that the weight reduction on the connecting rods is linear with the weight of the locmotive. However this requires futher research, and this section is a 
            // work around until any further research is undertaken
            // "The following code provides a simple 2-step adjustment, as not enough information is currently available for a more flexible one."
            if (MassKG < Kg.FromTUS(10))
            {
                const float reductionfactor = 0.2f;
                ReciprocatingWeightLb = 580.0f * reductionfactor;  // Weight of reciprocating parts of the rod driving gears
                ConnectingRodWeightLb = 600.0f * reductionfactor;  // Weignt of connecting rod
                ConnectingRodBalanceWeightLb = 300.0f * reductionfactor; // Balance weight for connecting rods
            }
            else if (MassKG < Kg.FromTUS(20)) 
            {
                const float reductionfactor = 0.3f;
                ReciprocatingWeightLb = 580.0f * reductionfactor;  // Weight of reciprocating parts of the rod driving gears
                ConnectingRodWeightLb = 600.0f * reductionfactor;  // Weignt of connecting rod
                ConnectingRodBalanceWeightLb = 300.0f * reductionfactor; // Balance weight for connecting rods
            }

            #region Initialise additional steam properties

            WaterDensityPSItoLBpFT3 = SteamTable.WaterDensityInterpolatorPSItoLBpFT3();
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

            CylinderCondensationFractionX = SteamTable.CylinderCondensationFractionInterpolatorX();
            SuperheatTempLimitXtoDegF = SteamTable.SuperheatTempLimitInterpolatorXtoDegF();
            SuperheatTempLbpHtoDegF = SteamTable.SuperheatTempInterpolatorLbpHtoDegF();

            SaturatedSpeedFactorSpeedDropFtpMintoX = SteamTable.SaturatedSpeedFactorSpeedDropFtpMintoX();
            SuperheatedSpeedFactorSpeedDropFtpMintoX = SteamTable.SuperheatedSpeedFactorSpeedDropFtpMintoX();

            CutoffInitialPressureDropRatioUpper = SteamTable.CutoffInitialPressureUpper();
            CutoffInitialPressureDropRatioLower = SteamTable.CutoffInitialPressureLower();

            // Assign default steam table values if cylinder event is not in ENG file
            if (CylinderExhausttoCutoff == null)
            {
                CylinderExhausttoCutoff = SteamTable.CylinderEventExhausttoCutoff();
                // Trace.TraceInformation("Default values used for CylinderExhausttoCutoff");
            }


            if (CylinderCompressiontoCutoff == null)
            {
                CylinderCompressiontoCutoff = SteamTable.CylinderEventCompressiontoCutoff();
                // Trace.TraceInformation("Default values used for CylinderCompressiontoCutoff");
            }

            if (CylinderAdmissiontoCutoff == null)
            {
                CylinderAdmissiontoCutoff = SteamTable.CylinderEventAdmissiontoCutoff();
            }

            // Assign default steam table values if table not in ENG file
            if (BoilerEfficiencyGrateAreaLBpFT2toX == null)
            {
                if (HasSuperheater)
                {
                    BoilerEfficiencyGrateAreaLBpFT2toX = SteamTable.SuperBoilerEfficiencyGrateAreaInterpolatorLbstoX();
                    Trace.TraceInformation("BoilerEfficiencyGrateAreaLBpFT2toX (Superheated) - default information read from SteamTables");

                }
                else
                {
                    BoilerEfficiencyGrateAreaLBpFT2toX = SteamTable.SatBoilerEfficiencyGrateAreaInterpolatorLbstoX();
                    Trace.TraceInformation("BoilerEfficiencyGrateAreaLBpFT2toX (Saturated) - default information read from SteamTables");
                }

            }

            // Calculate Grate Limit
            // Rule of thumb indicates that Grate limit occurs when the Boiler Efficiency is equal to 50% of the BE at zero firing rate
            // http://5at.co.uk/index.php/definitions/terrms-and-definitions/grate-limit.html
            // The following calculations are based upon the assumption that the BE curve is actually modelled by a straight line.
            // Calculate BE gradient
            float BEGradient = (BoilerEfficiencyGrateAreaLBpFT2toX[100] - BoilerEfficiencyGrateAreaLBpFT2toX[0]) / (100.0f - 0.0f);
            GrateLimitLBpFt2 = ((BoilerEfficiencyGrateAreaLBpFT2toX[0] / 2.0f) - BoilerEfficiencyGrateAreaLBpFT2toX[0]) / BEGradient;

            // Check Cylinder efficiency rate to see if set - allows user to improve cylinder performance and reduce losses
            if (CylinderEfficiencyRate == 0)
            {
                CylinderEfficiencyRate = 1.0f; // If no cylinder efficiency rate in the ENG file set to nominal (1.0)
            }

            // Assign value for superheat Initial pressure factor if not set in ENG file
            if (SuperheatCutoffPressureFactor == 0)
            {
                SuperheatCutoffPressureFactor = 40.0f; // If no factor in the ENG file set to nominal value (40.0)
            }

            // Determine if Cylinder Port Opening  Factor has been set
            if (CylinderPortOpeningFactor == 0)
            {
                CylinderPortOpeningFactor = 0.085f; // Set as default if not specified
            }
            CylinderPortOpeningFactor = MathHelper.Clamp(CylinderPortOpeningFactor, 0.05f, 0.12f); // Clamp Cylinder Port Opening Factor to between 0.05 & 0.12 so that tables are not exceeded   

            DrvWheelDiaM = DriverWheelRadiusM * 2.0f;

            // Test to see if gear type set
            bool IsGearAssumed = false;
            if (IsFixGeared || IsSelectGeared) // If a gear type has been selected, but gear type not set in steamenginetype, then set assumption
            {
                if (SteamEngineType != SteamEngineTypes.Geared)
                {
                    IsGearAssumed = true;
                    Trace.TraceWarning("Geared locomotive parameter not defined. Geared locomotive has been assumed");
                }
            }

            // Initialise vacuum brake small ejector steam consumption, read from ENG file if user input
            if (EjectorSmallSteamConsumptionLbpS == 0)
            {
                EjectorSmallSteamConsumptionLbpS = pS.FrompH(300.0f); // Use a value of 300 lb/hr as default
            }

            // Initialise vacuum brake large ejector steam consumption, read from ENG file if user input
            if (EjectorLargeSteamConsumptionLbpS == 0)
            {
                EjectorLargeSteamConsumptionLbpS = pS.FrompH(650.0f); // Based upon Gresham publication - steam consumption for 20mm ejector is 650lbs/hr or 0.180555 lb/s
            }

            // Assign value for boiler surface area if not set in ENG file
            if (BoilerSurfaceAreaFt2 == 0)
            {
                BoilerSurfaceAreaFt2 = Me2.ToFt2(16.0f * GrateAreaM2); // Rough approximation - based upon empirical graphing
            }

            // Assign value for boiler heat loss coefficient if not set in ENG file
            if (KcInsulation == 0)
            {
                KcInsulation = 0.4f; // Rough approximation - based upon empirical graphing
            }

            // Assign value for boiler heat loss insulation fraction if not set in ENG file
            if (FractionBoilerAreaInsulated == 0)
            {
                FractionBoilerAreaInsulated = 0.86f; // Rough approximation - based upon empirical graphing
            }

            // ******************  Test Locomotive and Gearing type *********************** 

            if (SteamEngineType == SteamEngineTypes.Compound)
            {
                //  Initialise Compound locomotive
                SteamLocoType = "Compound locomotive";

                // Model current based upon a four cylinder, balanced compound, type Vauclain, as built by Baldwin, with no receiver between the HP and LP cylinder
                // Set to compound operation intially
                CompoundCylinderRatio = (LPCylinderDiameterM * LPCylinderDiameterM) / (CylinderDiameterM * CylinderDiameterM);
                MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                MaxTractiveEffortLbf = CylinderEfficiencyRate * (1.6f * MaxBoilerPressurePSI * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderStrokeM)) / ((CompoundCylinderRatio + 1.0f) * (Me.ToIn(DriverWheelRadiusM * 2.0f)));
                DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                LogIsCompoundLoco = true;  // Set logging to true for compound locomotive

            }
            else if (SteamEngineType == SteamEngineTypes.Geared)
            {
                if (IsFixGeared)
                {
                    // Advise if gearing is assumed
                    if (IsGearAssumed)
                    {
                        SteamLocoType = "Not formally defined (assumed Fixed Geared) locomotive";
                    }
                    else
                    {
                        SteamLocoType = "Fixed Geared locomotive";
                    }

                    // Check for ENG file values
                    if (MaxSteamGearPistonRateFtpM == 0)
                    {
                        MaxSteamGearPistonRateFtpM = 700.0f; // Assume same value as standard steam locomotive
                        Trace.TraceWarning("MaxSteamGearPistonRateRpM not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    if (SteamGearRatioLow == 0)
                    {
                        SteamGearRatioLow = 5.0f;
                        Trace.TraceWarning("SteamGearRatioLow not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    MotiveForceGearRatio = SteamGearRatioLow;
                    SteamGearRatio = SteamGearRatioLow;
                    SteamGearPosition = 1.0f; // set starting gear position 
                    // Calculate maximum locomotive speed - based upon the number of revs for the drive shaft, geared to wheel shaft, and then circumference of drive wheel
                    // Max Geared speed = ((MaxPistonSpeedFt/m / Gear Ratio) x DrvWheelCircumference) / Feet in mile - miles per min
                    LowMaxGearedSpeedMpS = (Me.FromFt(pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatioLow))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2.0f * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * GearedTractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                    MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                    DisplayMaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                    DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                }
                else if (IsSelectGeared)
                {
                    // Advise if gearing is assumed
                    if (IsGearAssumed)
                    {
                        SteamLocoType = "Not formally defined (assumed Selectable Geared) locomotive";
                    }
                    else
                    {
                        SteamLocoType = "Selectable Geared locomotive";
                    }

                    // Check for ENG file values
                    if (MaxSteamGearPistonRateFtpM == 0)
                    {
                        MaxSteamGearPistonRateFtpM = 500.0f;
                        Trace.TraceWarning("MaxSteamGearPistonRateRpM not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    if (SteamGearRatioLow == 0)
                    {
                        SteamGearRatioLow = 9.0f;
                        Trace.TraceWarning("SteamGearRatioLow not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    if (SteamGearRatioHigh == 0)
                    {
                        SteamGearRatioHigh = 4.5f;
                        Trace.TraceWarning("SteamGearRatioHigh not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    // Adjust resistance for neutral gearing
                    GearedRetainedDavisAN = DavisAN; // remember davis a value for later
                    NeutralGearedDavisAN = DavisAN; // Initialise neutral gear value
                    float TempDavisAAmount = N.FromLbf((DavisMechanicalResistanceFactor * Kg.ToTUS(DrvWheelWeightKg))); // Based upon the Davis formula for steam locomotive resistance
                    if (TempDavisAAmount > 0.5 * DavisAN)
                    {
                        TempDavisAAmount = DavisAN * 0.5f; // If calculated mechanical resistance is greater then then 50% of the DavisA amount then set to an arbitary value of 50%.
                    }
                    NeutralGearedDavisAN -= TempDavisAAmount; // Reduces locomotive resistance when in neutral gear, as mechanical resistance decreases

                    MotiveForceGearRatio = SteamGearRatioLow; // assume in low gear as starting position (for purposes of initialising locomotive and HUD correctly
                    SteamGearRatio = SteamGearRatioLow;   // assume in low gear as starting position
                    SteamGearPosition = 1.0f; // assume in low gear as starting position 
                    // Calculate maximum locomotive speed - based upon the number of revs for the drive shaft, geared to wheel shaft, and then circumference of drive wheel
                    // Max Geared speed = ((MaxPistonSpeed / Gear Ratio) x DrvWheelCircumference) / Feet in mile - miles per min
                    LowMaxGearedSpeedMpS = (Me.FromFt(pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatioLow))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                    HighMaxGearedSpeedMpS = (Me.FromFt(pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatioHigh))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * GearedTractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                    MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                    DisplayMaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                    DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                }
                else
                {
                    SteamLocoType = "Unknown Geared locomotive (default to non-gear)";
                    // Default to non-geared locomotive
                    MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                    SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                    DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                }
            }
            else if (SteamEngineType == SteamEngineTypes.Simple)    // Simple locomotive
            {
                SteamLocoType = "Simple locomotive";
                MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
            }
            else // Default to Simple Locomotive (Assumed Simple) shows up as "Unknown"
            {
                Trace.TraceWarning("Steam engine type parameter not formally defined. Simple locomotive has been assumed");
                SteamLocoType = "Not formally defined (assumed simple) locomotive.";
                //  SteamEngineType += "Simple";
                MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
            }

            // ******************  Test Boiler Type ********************* 


            MaxTotalCombinedWaterVolumeUKG = (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG); // Initialise loco with tender water only - will be updated as appropriate

            if (RestoredCombinedTenderWaterVolumeUKG > 1.0)// Check to see if this is a restored game -(assumed so if Restored >0), then set water controller values based upon saved values
            {
                CombinedTenderWaterVolumeUKG = RestoredCombinedTenderWaterVolumeUKG;
                MaxTotalCombinedWaterVolumeUKG = RestoredMaxTotalCombinedWaterVolumeUKG;
            }

            InitializeTenderWithWater();

            InitializeTenderWithCoal();

            // Assign default steam table values if table not in ENG file
            if (InitialPressureDropRatioRpMtoX == null)
            {
                if (HasSuperheater)
                {
                    InitialPressureDropRatioRpMtoX = SteamTable.SuperInitialPressureDropRatioInterpolatorRpMtoX();
                    Trace.TraceInformation("InitialPressureDropRatioRpMtoX (Superheated) - default information read from SteamTables");
                }
                else
                {
                    InitialPressureDropRatioRpMtoX = SteamTable.SatInitialPressureDropRatioInterpolatorRpMtoX();
                    Trace.TraceInformation("InitialPressureDropRatioRpMtoX (Saturated) - default information read from SteamTables");
                }

            }

            // Computed Values
            // Read alternative OR Value for calculation of Ideal Fire Mass
            if (GrateAreaM2 == 0)  // Calculate Grate Area if not present in ENG file
            {
                float MinGrateAreaSizeSqFt = 6.0f;
                GrateAreaM2 = Me2.FromFt2(((NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) * MEPFactor * MaxBoilerPressurePSI) / (Me.ToIn(DriverWheelRadiusM * 2.0f) * GrateAreaDesignFactor));
                GrateAreaM2 = MathHelper.Clamp(GrateAreaM2, Me2.FromFt2(MinGrateAreaSizeSqFt), GrateAreaM2); // Clamp grate area to a minimum value of 6 sq ft
                IdealFireMassKG = GrateAreaM2 * Me.FromIn(IdealFireDepthIN) * FuelDensityKGpM3;
                Trace.TraceWarning("Grate Area not found in ENG file and has been set to {0} m^2", GrateAreaM2); // Advise player that Grate Area is missing from ENG file
            }
            else
                if (LocoIsOilBurner)
                IdealFireMassKG = GrateAreaM2 * 720.0f * 0.08333f * 0.02382f * 1.293f;  // Check this formula as conversion factors maybe incorrect, also grate area is now in SqM
            else
                IdealFireMassKG = GrateAreaM2 * Me.FromIn(IdealFireDepthIN) * FuelDensityKGpM3;

            // Calculate the maximum fuel burn rate based upon grate area and limit
            float GrateLimitLBpFt2add = GrateLimitLBpFt2 * 1.10f;     // Alow burn rate to slightly exceed grate limit (by 10%)
            MaxFuelBurnGrateKGpS = pS.FrompH(Kg.FromLb(Me2.ToFt2(GrateAreaM2) * GrateLimitLBpFt2add));

            if (MaxFireMassKG == 0) // If not specified, assume twice as much as ideal. 
                // Scale FIREBOX control to show FireMassKG as fraction of MaxFireMassKG.
                MaxFireMassKG = 2 * IdealFireMassKG;

            float baseTempK = C.ToK(C.FromF(PressureToTemperaturePSItoF[MaxBoilerPressurePSI]));
            if (EvaporationAreaM2 == 0)        // If evaporation Area is not in ENG file then synthesize a value
            {
                float MinEvaporationAreaM2 = 100.0f;
                EvaporationAreaM2 = Me2.FromFt2(((NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) * MEPFactor * MaxBoilerPressurePSI) / (Me.ToIn(DriverWheelRadiusM * 2.0f) * EvapAreaDesignFactor));
                EvaporationAreaM2 = MathHelper.Clamp(EvaporationAreaM2, Me2.FromFt2(MinEvaporationAreaM2), EvaporationAreaM2); // Clamp evaporation area to a minimum value of 6 sq ft, so that NaN values don't occur.
                Trace.TraceWarning("Evaporation Area not found in ENG file and has been set to {0} m^2", EvaporationAreaM2); // Advise player that Evaporation Area is missing from ENG file
            }

            if (!RestoredGame)  // If this is not a restored game, then initialise these values
            {
                CylinderSteamUsageLBpS = 1.0f;  // Set to 1 to ensure that there are no divide by zero errors
                WaterFraction = 0.9f;  // Initialise boiler water level at 90%
            }
            float MaxWaterFraction = 0.9f; // Initialise the max water fraction when the boiler starts
            if (BoilerEvapRateLbspFt2 == 0) // If boiler evaporation rate is not in ENG file then set a default value
            {
                BoilerEvapRateLbspFt2 = 15.0f; // Default rate for evaporation rate. Assume a default rate of 15 lbs/sqft of evaporation area
            }
            BoilerEvapRateLbspFt2 = MathHelper.Clamp(BoilerEvapRateLbspFt2, 7.5f, 30.0f); // Clamp BoilerEvap Rate to between 7.5 & 30 - some modern locomotives can go as high as 30, but majority are around 15.
            TheoreticalMaxSteamOutputLBpS = pS.FrompH(Me2.ToFt2(EvaporationAreaM2) * BoilerEvapRateLbspFt2); // set max boiler theoretical steam output

            float BoilerVolumeCheck = Me2.ToFt2(EvaporationAreaM2) / BoilerVolumeFT3;    //Calculate the Boiler Volume Check value.
            if (BoilerVolumeCheck > 15) // If boiler volume is not in ENG file or less then a viable figure (ie high ratio figure), then set to a default value
            {
                BoilerVolumeFT3 = Me2.ToFt2(EvaporationAreaM2) / 8.3f; // Default rate for evaporation rate. Assume a default ratio of evaporation area * 1/8.3
                // Advise player that Boiler Volume is missing from or incorrect in ENG file
                Trace.TraceWarning("Boiler Volume not found in ENG file, or doesn't appear to be a valid figure, and has been set to {0} Ft^3", BoilerVolumeFT3);
            }

            MaxBoilerHeatSafetyPressurePSI = MaxBoilerPressurePSI + SafetyValveStartPSI + 6.0f; // set locomotive maximum boiler pressure to calculate max heat, allow for safety valve + a bit
            MaxBoilerSafetyPressHeatBTU = MaxWaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerHeatSafetyPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerHeatSafetyPressurePSI] + (1 - MaxWaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerHeatSafetyPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerHeatSafetyPressurePSI];  // calculate the maximum possible heat in the boiler, assuming safety valve and a small margin
            MaxBoilerHeatBTU = MaxWaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerPressurePSI] + (1 - MaxWaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI];  // calculate the maximum possible heat in the boiler

            MaxBoilerKW = Kg.FromLb(TheoreticalMaxSteamOutputLBpS) * W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI]));
            MaxFlueTempK = (MaxBoilerKW / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 * HeatMaterialThicknessFactor)) + baseTempK;

            MaxBoilerOutputLBpH = pS.TopH(TheoreticalMaxSteamOutputLBpS);

            // Determine if Superheater in use
            if (HasSuperheater)
            {
                SteamLocoType += " + Superheater";

                if (MaxSuperheatRefTempF == 0) // If Max superheat temp is not set in ENG file, then set a default.
                {
                    // Calculate maximum superheat steam reference temperature based upon heating area of superheater - from Superheat Engineering Data by Elesco
                    // SuperTemp = (SuperHeatArea x HeatTransmissionCoeff * (MeanGasTemp - MeanSteamTemp)) / (SteamQuantity * MeanSpecificSteamHeat)
                    // Formula has been simplified as follows: SuperTemp = (SuperHeatArea x SFactor) / SteamQuantity
                    // SFactor is a "loose reprentation" =  (HeatTransmissionCoeff / MeanSpecificSteamHeat) - Av figure calculate by comparing a number of "known" units for superheat.
                    MaxSuperheatRefTempF = (Me2.ToFt2(SuperheatAreaM2) * SuperheatKFactor) / pS.TopH(TheoreticalMaxSteamOutputLBpS);
                }
                SuperheatTempRatio = MaxSuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];    // calculate a ratio figure for known value against reference curve.
                CylinderClearancePC = 0.09f;
            }
            else if (IsSaturated)
            {
                SteamLocoType += " + Saturated";
            }
            else if (SuperheatAreaM2 == 0 && SuperheaterFactor > 1.0) // check if MSTS value, then set superheating
            {
                SteamLocoType += " + Not formally defined (assumed superheated)";
                Trace.TraceWarning("Steam boiler type parameter not formally defined. Superheated locomotive has been assumed.");

                HasSuperheater = true;
                MaxSuperheatRefTempF = 250.0f; // Assume a superheating temp of 250degF
                SuperheatTempRatio = MaxSuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];
                // Reverse calculate Superheat area.
                SuperheatAreaM2 = Me2.FromFt2((MaxSuperheatRefTempF * pS.TopH(TheoreticalMaxSteamOutputLBpS)) / (SuperheatKFactor));
                // Advise player that Superheat Area is missing from ENG file
                Trace.TraceWarning("Superheat Area not found in ENG file, and has been set to {0} Ft^2", Me2.ToFt2(SuperheatAreaM2));
                CylinderClearancePC = 0.09f;
            }
            else // Default to saturated type of locomotive
            {
                SteamLocoType += " + Not formally defined (assumed saturated)";
                MaxSuperheatRefTempF = 0.0f;
                CylinderClearancePC = 0.08f;
            }

            // Assign default steam table values if table not in ENG file 
            // Back pressure increases with the speed of the locomotive, as cylinder finds it harder to exhaust all the steam.

            if (BackPressureIHPtoPSI == null)
            {
                if (HasSuperheater)
                {
                    BackPressureIHPtoPSI = SteamTable.BackpressureSuperIHPtoPSI();
                    Trace.TraceInformation("BackPressureIHPtoAtmPSI (Superheated) - default information read from SteamTables");
                }
                else
                {
                    BackPressureIHPtoPSI = SteamTable.BackpressureSatIHPtoPSI();
                    Trace.TraceInformation("BackPressureIHPtoAtmPSI (Saturated) - default information read from SteamTables");
                }
            }

            // Determine whether to start locomotive in Hot or Cold State
            HotStart = Simulator.Settings.HotStart;

            // Calculate maximum power of the locomotive, based upon the maximum IHP
            // Maximum IHP will occur at different (piston) speed for saturated locomotives and superheated based upon the wheel revolution. Typically saturated locomotive produce maximum power @ a piston speed of 700 ft/min , and superheated will occur @ 1000ft/min
            // Set values for piston speed

            if (HasSuperheater)
            {
                MaxPistonSpeedFtpM = 1000.0f; // if superheated locomotive
                MaxSpeedFactor = SuperheatedSpeedFactorSpeedDropFtpMintoX[MaxPistonSpeedFtpM];
                DisplaySpeedFactor = MaxSpeedFactor;
            }
            else if (SteamEngineType == SteamEngineTypes.Geared)
            {
                MaxPistonSpeedFtpM = MaxSteamGearPistonRateFtpM;  // if geared locomotive
                MaxSpeedFactor = SaturatedSpeedFactorSpeedDropFtpMintoX[MaxPistonSpeedFtpM];   // Assume the same as saturated locomotive for time being.
                DisplaySpeedFactor = MaxSpeedFactor;
            }
            else
            {
                MaxPistonSpeedFtpM = 700.0f;  // if saturated locomotive
                MaxSpeedFactor = SaturatedSpeedFactorSpeedDropFtpMintoX[MaxPistonSpeedFtpM];
                DisplaySpeedFactor = MaxSpeedFactor;
            }

            // Calculate max velocity of the locomotive based upon above piston speed
            if (SteamEngineType != SteamEngineTypes.Geared)
            {
                MaxLocoSpeedMpH = MpS.ToMpH(Me.FromFt(pS.FrompM(MaxPistonSpeedFtpM / SteamGearRatio))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                DisplayMaxLocoSpeedMpH = MaxLocoSpeedMpH;
            }

            // Assign default steam table values if table not in ENG file
            if (NewBurnRateSteamToCoalLbspH == null)
            {
                // This will calculate a default burnrate curve based upon the fuel calorific, Max Steam Output, and Boiler Efficiency
                // Firing Rate = (Max Evap / BE) x (Steam Btu/lb @ pressure / Fuel Calorific)

                float BoilerEfficiencyBurnRate = (BoilerEfficiencyGrateAreaLBpFT2toX[0.0f] / 2.0f);
                MaxFiringRateLbpH = (pS.TopH(TheoreticalMaxSteamOutputLBpS) / BoilerEfficiencyBurnRate) * (SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI] / KJpKg.ToBTUpLb(FuelCalorificKJpKG));

                // Create a new default burnrate curve locomotive based upon default information
                float[] TempSteamOutputRate = new float[]
                    {
                               0.0f, pS.TopH(TheoreticalMaxSteamOutputLBpS), pS.TopH(TheoreticalMaxSteamOutputLBpS * 1.1f), pS.TopH(TheoreticalMaxSteamOutputLBpS * 1.2f), pS.TopH(TheoreticalMaxSteamOutputLBpS * 1.3f), pS.TopH(TheoreticalMaxSteamOutputLBpS * 1.4f)
                    };

                float[] TempCoalFiringRate = new float[]
                    {
                                0.0f, MaxFiringRateLbpH, (MaxFiringRateLbpH * 1.5f), (MaxFiringRateLbpH * 2.0f), (MaxFiringRateLbpH * 3.0f), (MaxFiringRateLbpH * 4.0f)
                    };

                NewBurnRateSteamToCoalLbspH = new Interpolator(TempSteamOutputRate, TempCoalFiringRate);
            }
            else // If user provided burn curve calculate the Max Firing Rate
            {
                MaxFiringRateLbpH = NewBurnRateSteamToCoalLbspH[pS.TopH(TheoreticalMaxSteamOutputLBpS)];
            }

            // Calculate Boiler output horsepower - based upon information in Johnsons book - The Steam Locomotive - pg 150
            // In essence the cylinder power and boiler power are calculated, and the smaller of the two are used. Thus in some instances the boiler power may be the limiting factor rather then the cylinder power.
            // BoilerOutputHP = Boiler Evap / Steam per ihp-hour. These values will be rounded for calculation purposes
            float SteamperIHPh = 0.0f;
            if (HasSuperheater)
            {
                if (MaxSuperheatRefTempF >= 350.0f)
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 16.53f; // For 350 deg superheated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 16.1f; // For 350 deg superheated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 15.825f; // For 350 deg superheated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 15.625f; // For 350 deg superheated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 17.01f; // For For 350 deg superheated locomotive with boiler pressure less then 200
                    }

                }
                else if (MaxSuperheatRefTempF >= 300.0f)
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 17.41f; // For 300 deg superheated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 16.925f; // For 300 deg superheated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 16.675f; // For 300 deg superheated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 16.5f; // For 300 deg superheated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 17.91f; // For For 300 deg superheated locomotive with boiler pressure less then 200
                    }
                }
                else if (MaxSuperheatRefTempF >= 250.0f)
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 18.325f; // For 250 deg superheated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 17.8125f; // For 250 deg superheated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 17.575f; // For 250 deg superheated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 17.3875f; // For 250 deg superheated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 19.05f; // For For 250 deg superheated locomotive with boiler pressure less then 200
                    }
                }
                else if (MaxSuperheatRefTempF > 200.0f)
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 19.3f; // For 200 deg superheated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 18.725f; // For 200 deg superheated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 18.5f; // For 200 deg superheated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 18.3f; // For 200 deg superheated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 20.1f; // For For 200 deg superheated locomotive with boiler pressure less then 200
                    }
                }
                else if (MaxSuperheatRefTempF >= 150.0f)
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 20.6f; // For 150 deg superheated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 19.95f; // For 150 deg superheated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 19.55f; // For 150 deg superheated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 19.3f; // For 150 deg superheated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 21.375f; // For For 150 deg superheated locomotive with boiler pressure less then 200
                    }
                }
                else  // Assume the same as a saturated locomotive
                {
                    if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                    {
                        SteamperIHPh = 27.5f; // For saturated locomotive with boiler pressure between 200 and 249
                    }
                    else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                    {
                        SteamperIHPh = 26.6f; // For saturated locomotive with default boiler pressure between 250 and 299
                    }
                    else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                    {
                        SteamperIHPh = 26.05f; // For saturated locomotive with default boiler pressure between 300 and 349 
                    }
                    else if (MaxBoilerPressurePSI >= 350)
                    {
                        SteamperIHPh = 25.7f; // For saturated locomotive with default boiler pressure between 350 and 399 
                    }
                    else
                    {
                        SteamperIHPh = 28.425f; // For saturated locomotive with boiler pressure less then 200
                    }
                }

            }
            else
            {
                if (MaxBoilerPressurePSI >= 200 && MaxBoilerPressurePSI < 249)
                {
                    SteamperIHPh = 27.5f; // For saturated locomotive with boiler pressure between 200 and 249
                }
                else if (MaxBoilerPressurePSI >= 250 && MaxBoilerPressurePSI < 299)
                {
                    SteamperIHPh = 26.6f; // For saturated locomotive with default boiler pressure between 250 and 299
                }
                else if (MaxBoilerPressurePSI >= 300 && MaxBoilerPressurePSI < 349)
                {
                    SteamperIHPh = 26.05f; // For saturated locomotive with default boiler pressure between 300 and 349 
                }
                else if (MaxBoilerPressurePSI >= 350)
                {
                    SteamperIHPh = 25.7f; // For saturated locomotive with default boiler pressure between 350 and 399 
                }
                else
                {
                    SteamperIHPh = 28.425f; // For saturated locomotive with boiler pressure less then 200
                }
            }

            MaxBoilerOutputHP = MaxBoilerOutputLBpH / SteamperIHPh; // Calculate boiler output power

            // if MaxIHP is set in ENG file, and is a geared (mainly selectable model) locomotive then retain a copy of the original value
            if (MaxIndicatedHorsePowerHP != 0) 
            {
                RetainedGearedMaxMaxIndicatedHorsePowerHP = MaxIndicatedHorsePowerHP;
            }

            if (MaxIndicatedHorsePowerHP == 0) // if MaxIHP is not set in ENG file, then set a default
            {
                // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                MaxIndicatedHorsePowerHP = MaxSpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;  // To be checked what MaxTractive Effort is for the purposes of this formula.

                // Check to see if MaxIHP is in fact limited by the boiler
                if (MaxIndicatedHorsePowerHP > MaxBoilerOutputHP)
                {
                    MaxIndicatedHorsePowerHP = MaxBoilerOutputHP; // Set maxIHp to limit set by boiler
                    ISBoilerLimited = true;
                }
                else
                {
                    ISBoilerLimited = false;
                }
            }
                       
            DisplayMaxIndicatedHorsePowerHP = MaxIndicatedHorsePowerHP;

            // If DrvWheelWeight is not in ENG file, then calculate from Factor of Adhesion(FoA) = DrvWheelWeight / Start (Max) Tractive Effort, assume FoA = 4.2

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                const float FactorofAdhesion = 4.2f; // Assume a typical factor of adhesion
                DrvWheelWeightKg = Kg.FromLb(FactorofAdhesion * MaxTractiveEffortLbf); // calculate Drive wheel weight if not in ENG file
                DrvWheelWeightKg = MathHelper.Clamp(DrvWheelWeightKg, 0.1f, MassKG); // Make sure adhesive weight does not exceed the weight of the locomotive
                InitialDrvWheelWeightKg = DrvWheelWeightKg; // Initialise the Initial Drive wheel weight the same as starting value
            }

            // Calculate factor of adhesion for display purposes

            CalculatedFactorofAdhesion = Kg.ToLb(DrvWheelWeightKg) / MaxTractiveEffortLbf;

            // Calculate "critical" power of locomotive to determine limit of max IHP
            MaxCriticalSpeedTractiveEffortLbf = (MaxTractiveEffortLbf * CylinderEfficiencyRate) * MaxSpeedFactor;
            DisplayCriticalSpeedTractiveEffortLbf = MaxCriticalSpeedTractiveEffortLbf;

            // Cylinder Steam Usage = Cylinder Volume * Cutoff * No of Cylinder Strokes (based on loco speed, ie, distance travelled in period / Circumference of Drive Wheels)
            // SweptVolumeToTravelRatioFT3pFT is used to calculate the Cylinder Steam Usage Rate (see below)
            // SweptVolumeToTravelRatioFT3pFT = strokes_per_cycle * no_of_cylinders * pi*CylRad^2 * stroke_length / 2*pi*WheelRad
            // "pi"s cancel out
            // Cylinder Steam Usage	= SweptVolumeToTravelRatioFT3pFT x cutoff x {(speed x (SteamDensity (CylPress) - SteamDensity (CylBackPress)) 
            // lbs/s                = ft3/ft                                  x   ft/s  x  lbs/ft3

            // Cylinder piston shaft volume needs to be calculated and deducted from sweptvolume - assume diameter of the cylinder minus one-half of the piston-rod area. Let us assume that the latter is 3 square inches
            CylinderPistonShaftFt3 = Me2.ToFt2(Me2.FromIn2(((float)Math.PI * (CylinderPistonShaftDiaIn / 2.0f) * (CylinderPistonShaftDiaIn / 2.0f)) / 2.0f));
            CylinderPistonAreaFt2 = Me2.ToFt2(MathHelper.Pi * CylinderDiameterM * CylinderDiameterM / 4.0f);
            LPCylinderPistonAreaFt2 = Me2.ToFt2(MathHelper.Pi * LPCylinderDiameterM * LPCylinderDiameterM / 4.0f);
            CylinderSweptVolumeFT3pFT = ((CylinderPistonAreaFt2 * Me.ToFt(CylinderStrokeM)) - CylinderPistonShaftFt3);
            LPCylinderSweptVolumeFT3pFT = ((LPCylinderPistonAreaFt2 * Me.ToFt(LPCylinderStrokeM)) - CylinderPistonShaftFt3);

            MaxCombustionRateKgpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH(TheoreticalMaxSteamOutputLBpS)]));

            // Calculate the maximum boiler heat input based on the steam generation rate

            MaxBoilerHeatInBTUpS = W.ToBTUpS(W.FromKW(MaxCombustionRateKgpS * FuelCalorificKJpKG * BoilerEfficiencyGrateAreaLBpFT2toX[pS.TopH(Kg.ToLb(MaxCombustionRateKgpS)) / GrateAreaM2]));

            // Initialise Locomotive in a Hot or Cold Start Condition

            if (!RestoredGame) // Only initialise the following values if game is not being restored.
            {
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

                WaterTempNewK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI])); // Initialise new boiler pressure
                FireMassKG = IdealFireMassKG;

                BoilerSteamHeatBTUpLB = SteamHeatPSItoBTUpLB[BoilerPressurePSI];
                BoilerWaterHeatBTUpLB = WaterHeatPSItoBTUpLB[BoilerPressurePSI];
                BoilerSteamDensityLBpFT3 = SteamDensityPSItoLBpFT3[BoilerPressurePSI];
                BoilerWaterDensityLBpFT3 = WaterDensityPSItoLBpFT3[BoilerPressurePSI];

            }
            DamperFactorManual = TheoreticalMaxSteamOutputLBpS / SpeedEquivMpS; // Calculate a factor for damper control that will vary with speed.
            BlowerSteamUsageFactor = 0.04f * MaxBoilerOutputLBpH / 3600 / MaxBoilerPressurePSI;


            if (ORTSMaxFiringRateKGpS != 0)
                MaxFiringRateKGpS = ORTSMaxFiringRateKGpS; // If OR value present then use it 

            if (MaxFiringRateKGpS == 0)  // If no value for Firing rate then estimate a rate.
            {
                if (Stoker == 1.0f)
                {
                    MaxFiringRateKGpS = pS.FrompH(Kg.FromLb(14400.0f)); // Assume mecxhanical stocker can feed at a rate of 14,400 lb/hr

                }
                else
                {
                    MaxFiringRateKGpS = 180 * MaxBoilerOutputLBpH / 775 / 3600 / 2.2046f;
                }

            }

            // Initialise Mechanical Stoker if present
            if (Stoker == 1.0f)
            {
                StokerIsMechanical = true;
            }
            else // AI fireman
            {
                MaxTheoreticalFiringRateKgpS = MaxFiringRateKGpS * 1.33f; // allow the fireman to overfuel for short periods of time 
            }

            ApplyBoilerPressure();

            AuxPowerOn = true;

            if (Simulator.Settings.DataLogSteamPerformance)
            {
                Trace.TraceInformation("============================================= Steam Locomotive Performance - Locomotive Details =========================================================");
                Trace.TraceInformation("Version - {0}", VersionInfo.VersionOrBuild);
                Trace.TraceInformation("Locomotive Name - {0}", LocomotiveName);
                Trace.TraceInformation("Steam Locomotive Type - {0}", SteamLocoType);

                Trace.TraceInformation("**************** General ****************");
                Trace.TraceInformation("WheelRadius {0:N2} ft, NumWheels {1}, DriveWheelWeight {2:N1} t-uk", Me.ToFt(DriverWheelRadiusM), LocoNumDrvAxles, Kg.ToTUK(DrvWheelWeightKg));

                Trace.TraceInformation("**************** Boiler ****************");
                Trace.TraceInformation("Boiler Volume {0:N1} cu ft, Evap Area {1:N1} sq ft, Superheat Area {2:N1} sq ft, Max Superheat Temp {3:N1} F, Max Boiler Pressure {4:N1} psi", BoilerVolumeFT3, Me2.ToFt2(EvaporationAreaM2), Me2.ToFt2(SuperheatAreaM2), MaxSuperheatRefTempF, MaxBoilerPressurePSI);
                Trace.TraceInformation("Boiler Evap Rate {0} , Max Boiler Output {1} lbs/h", BoilerEvapRateLbspFt2, MaxBoilerOutputLBpH);

                Trace.TraceInformation("**************** Cylinder ****************");
                Trace.TraceInformation("Num {0}, Stroke {1:N1} in, Diameter {2:N1} in, Efficiency {3:N1}, MaxIHP {4:N1}", NumCylinders, Me.ToIn(CylinderStrokeM), Me.ToIn(CylinderDiameterM), CylinderEfficiencyRate, MaxIndicatedHorsePowerHP);
                Trace.TraceInformation("Port Opening {0}, Exhaust Point {1}, InitialSuperheatFactor {2}", CylinderPortOpeningFactor, CylinderExhaustOpenFactor, SuperheatCutoffPressureFactor);

                Trace.TraceInformation("**************** Fire ****************");
                Trace.TraceInformation("Grate - Area {0:N1} sq ft, Limit {1:N1} lb/sq ft", Me2.ToFt2(GrateAreaM2), GrateLimitLBpFt2);
                Trace.TraceInformation("Fuel - Calorific {0} btu/lb, Max Firing Rate {1} lbs/h Max Coal Load {2} lbs", KJpKg.ToBTUpLb(FuelCalorificKJpKG), Kg.ToLb(pS.TopH(MaxFiringRateKGpS)), Kg.ToLb(MaxTenderCoalMassKG));

                Trace.TraceInformation("========================================================================================================================================================");

            }
            RestoredGame = true; // Set flag for restored game indication
            #endregion
        }

        /// <summary>
        /// Sets controller settings from other engine for cab switch
        /// </summary>
        /// <param name="other"></param>
        public override void CopyControllerSettings(TrainCar other)
        {
            base.CopyControllerSettings(other);
            if (CutoffController != null)
                CutoffController.SetValue(Train.MUReverserPercent / 100);
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
            CutoffController.SetValue(Train.MUReverserPercent / 100);
            ThrottleController.SetValue(Train.MUThrottlePercent / 100);
            HotStart = true;
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
            base.Update(elapsedClockSeconds);
            UpdateFX(elapsedClockSeconds);

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
            throttle = ThrottlePercent / 100;
            cutoff = Math.Abs(Train.MUReverserPercent / 100);
            if (cutoff > CutoffController.MaximumValue) // Maximum value set in cutoff controller in ENG file
                cutoff = CutoffController.MaximumValue;   // limit to maximum value set in ENG file for locomotive
            float absSpeedMpS = Math.Abs(Train.SpeedMpS);
            if (absSpeedMpS > 2 && (Train.MUReverserPercent == 100 || Train.MUReverserPercent == -100))
            {   // AI cutoff adjustment logic, also used for steam MU'd with non-steam
                cutoff = throttle * CutoffController.MaximumValue * 2 / absSpeedMpS;
                float min = 0.2f;  // Figure originally set with ForceFactor2 table - not sure the significance at this time.
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
            UpdateMotiveForce(elapsedClockSeconds, 0, 0, 0);
            UpdateAuxiliaries(elapsedClockSeconds, absSpeedMpS);
            #endregion

            #region adjust state
            UpdateWaterGauge();
            UpdateInjectors(elapsedClockSeconds);
            UpdateFiring(absSpeedMpS);
            #endregion

        }

        /// <summary>
        /// Update variables related to audiovisual effects (sound, steam)
        /// </summary>
        private void UpdateFX(float elapsedClockSeconds)
        {
            // This section updates the various steam effects for the steam locomotive. It uses the particle drawer which has the following inputs.
            // Stack - steam velocity, steam volume, particle duration, colour, whislts all other effects use these inputs only, non-Stack - steam velocity, steam volume, particle duration
            // The steam effects have been adjust based upon their "look and feel", as the particle drawer has a number of different multipliers in it.
            // Steam Velocity - increasing this value increases how far the steam jets out of the orifice, steam volume adjust volume, particle duration adjusts the "decay' time of the steam
            // The duration time is reduced with speed to reduce the steam trail behind the locomotive when running at speed.
            // Any of the steam effects can be disabled by not defining them in the ENG file, and thus they will not be displayed in the viewer.

            // Cylinder steam cock effects
            if (Cylinder2SteamEffects) // For MSTS locomotives with one cyldinder cock ignore calculation of cock opening times.
            {
                CylinderCockOpenTimeS = 0.5f * 1.0f / DrvWheelRevRpS;  // Calculate how long cylinder cocks open  @ speed = Time (sec) / (Drv Wheel RpS ) - assume two cylinder strokes per rev, ie each cock will only be open for 1/2 rev
                CylinderCockTimerS += elapsedClockSeconds;
                if (CylinderCockTimerS > CylinderCockOpenTimeS)
                {
                    if (CylinderCock1On)
                    {
                        CylinderCock1On = false;
                        CylinderCock2On = true;
                        CylinderCockTimerS = 0.0f;  // Reset timer
                    }
                    else if (CylinderCock2On)
                    {
                        CylinderCock1On = true;
                        CylinderCock2On = false;
                        CylinderCockTimerS = 0.0f;  // Reset timer

                    }

                }
            }

            float SteamEffectsFactor = MathHelper.Clamp(BoilerPressurePSI / MaxBoilerPressurePSI, 0.1f, 1.0f);  // Factor to allow for drops in boiler pressure reducing steam effects

            // Bernoulli formula for future reference - steam velocity = SQRT ( 2 * dynamic pressure (pascals) / fluid density)
            Cylinders1SteamVelocityMpS = 100.0f;
            Cylinders2SteamVelocityMpS = 100.0f;
            Cylinders1SteamVolumeM3pS = (CylinderCock1On && CylinderCocksAreOpen && throttle > 0.0 && CylCockSteamUsageDisplayLBpS > 0.0 ? (10.0f * SteamEffectsFactor) : 0.0f);
            Cylinders2SteamVolumeM3pS = (CylinderCock2On && CylinderCocksAreOpen && throttle > 0.0 && CylCockSteamUsageDisplayLBpS > 0.0 ? (10.0f * SteamEffectsFactor) : 0.0f);
            Cylinder1ParticleDurationS = 1.0f;
            Cylinder2ParticleDurationS = 1.0f;

            // Blowdown Steam Effects
            BlowdownSteamVolumeM3pS = (BlowdownValveOpen && BlowdownSteamUsageLBpS > 0.0 ? (10.0f * SteamEffectsFactor) : 0.0f);
            BlowdownSteamVelocityMpS = 350.0f;
            BlowdownParticleDurationS = 2.0f;

            // Drainpipe Steam Effects
            DrainpipeSteamVolumeM3pS = 0.0f;  // Turn Drainpipe permanently "off"
            DrainpipeSteamVelocityMpS = 0.0f;
            DrainpipeParticleDurationS = 1.0f;

            // Generator Steam Effects
            GeneratorSteamVelocityMpS = 50.0f;
            GeneratorSteamVolumeM3pS = 4.0f * SteamEffectsFactor;
            GeneratorParticleDurationS = 1.0f;
            GeneratorParticleDurationS = MathHelper.Clamp(GeneratorParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);

            // Injector Steam Effects
            Injector1SteamVolumeM3pS = (Injector1IsOn ? (5.0f * SteamEffectsFactor) : 0);
            Injector1SteamVelocityMpS = 10.0f;
            Injector1ParticleDurationS = 1.0f;
            Injector1ParticleDurationS = MathHelper.Clamp(Injector1ParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);

            Injector2SteamVolumeM3pS = (Injector2IsOn ? (5.0f * SteamEffectsFactor) : 0);
            Injector2SteamVelocityMpS = 10.0f;
            Injector2ParticleDurationS = 1.0f;
            Injector2ParticleDurationS = MathHelper.Clamp(Injector2ParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);

            // Ejector Steam Effects
            SmallEjectorSteamVolumeM3pS = (SmallSteamEjectorIsOn ? (1.5f * SteamEffectsFactor) : 0);
            SmallEjectorSteamVelocityMpS = 10.0f;
            SmallEjectorParticleDurationS = 1.0f;
            SmallEjectorParticleDurationS = MathHelper.Clamp(SmallEjectorParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);

            LargeEjectorSteamVolumeM3pS = (LargeSteamEjectorIsOn ? (1.5f * SteamEffectsFactor) : 0);
            LargeEjectorSteamVelocityMpS = 10.0f;
            LargeEjectorParticleDurationS = 1.0f;
            LargeEjectorParticleDurationS = MathHelper.Clamp(LargeEjectorParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);

            // Compressor Steam Effects
            // Only show compressor steam effects if it is not a vacuum controlled steam engine
            if (!(BrakeSystem is Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS.VacuumSinglePipe))
            {
                CompressorSteamVelocityMpS = 10.0f;
                CompressorSteamVolumeM3pS = (CompressorIsOn ? (1.5f * SteamEffectsFactor) : 0);
                CompressorParticleDurationS = 1.0f;
                CompressorParticleDurationS = MathHelper.Clamp(CompressorParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 1.0f);
            }

            // Whistle Steam Effects
            WhistleSteamVelocityMpS = 10.0f;
            WhistleSteamVolumeM3pS = (Horn ? (5.0f * SteamEffectsFactor) : 0);
            WhistleParticleDurationS = 3.0f;
            WhistleParticleDurationS = MathHelper.Clamp(WhistleParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 3.0f);

            // Safety Valves Steam Effects

            SafetyValvesSteamVelocityMpS = (float)Math.Sqrt(KPa.FromPSI(MaxBoilerPressurePSI) * 1000 * 2 / WaterDensityAt100DegC1BarKGpM3);
            //SafetyValvesSteamVolumeM3pS = SafetyIsOn ? Kg.FromLb(SafetyValveUsageLBpS) * SteamVaporSpecVolumeAt100DegC1BarM3pKG : 0;
            SafetyValvesSteamVolumeM3pS = SafetyIsOn ? 5.0f : 0;
            SafetyValvesParticleDurationS = 3.0f;
            SafetyValvesParticleDurationS = MathHelper.Clamp(SafetyValvesParticleDurationS / (absSpeedMpS / 4.0f), 0.1f, 3.0f);

            // Smoke Stack Smoke Effects
            // Colur for smoke is determined by the amount of air flowing through the fire (ie damper ).

            // Damper - to determine smoke color
            float SmokeColorDamper = 0.0f;
            if (FiringIsManual)
            {
                SmokeColorDamper = DamperBurnEffect; // set to the damper burn effect as set for manual firing
            }
            else
            {
                SmokeColorDamper = absSpeedMpS * DamperFactorManual; // Damper value for manual firing - related to increased speed, and airflow through fire
            }

            SmokeColorDamper = MathHelper.Clamp(SmokeColorDamper, 0.0f, TheoreticalMaxSteamOutputLBpS); // set damper maximum to the max generation rate

            // Fire mass
            //TODO - Review and check
            float SmokeColorFireMass = (FireMassKG / IdealFireMassKG); // As firemass exceeds the ideal mass the fire becomes 'blocked', when firemass is < ideal then fire burns more freely.
            SmokeColorFireMass = (1.0f / SmokeColorFireMass) * (1.0f / SmokeColorFireMass) * (1.0f / SmokeColorFireMass); // Inverse the firemass value, then cube it to make it a bit more significant

            StackSteamVelocityMpS.Update(elapsedClockSeconds, (float)Math.Sqrt(KPa.FromPSI(Pressure_c_AtmPSI) * 1000 * 2 / WaterDensityAt100DegC1BarKGpM3));
            StackSteamVolumeM3pS = Kg.FromLb(CylinderSteamUsageLBpS + BlowerSteamUsageLBpS + RadiationSteamLossLBpS + CompSteamUsageLBpS + GeneratorSteamUsageLBpS) * SteamVaporSpecVolumeAt100DegC1BarM3pKG;
            float SmokeColorUnits = (RadiationSteamLossLBpS + CalculatedCarHeaterSteamUsageLBpS + BlowerBurnEffect + (SmokeColorDamper * SmokeColorFireMass)) / PreviousTotalSteamUsageLBpS - 0.2f;
            SmokeColor.Update(elapsedClockSeconds, MathHelper.Clamp(SmokeColorUnits, 0.25f, 1));

            // Variable1 is proportional to angular speed, value of 10 means 1 rotation/second.
            var variable1 = Math.Abs(WheelSpeedSlipMpS / DriverWheelRadiusM / MathHelper.Pi * 5);
            Variable1 = ThrottlePercent == 0 ? 0 : variable1;
            Variable2 = MathHelper.Clamp((CylinderCocksPressureAtmPSI - OneAtmospherePSI) / BoilerPressurePSI * 100f, 0, 100);
            Variable3 = FuelRateSmoothed * 100;

            const int rotations = 2;
            const int fullLoop = 10 * rotations;
            int numPulses = NumCylinders * 2 * rotations;

            var dPulseTracker = Variable1 / fullLoop * numPulses * elapsedClockSeconds;
            PulseTracker += dPulseTracker;

            if (PulseTracker > (float)NextPulse - dPulseTracker / 2)
            {
                SignalEvent((Event)((int)Event.SteamPulse1 + NextPulse - 1));
                PulseTracker %= numPulses;
                NextPulse %= numPulses;
                NextPulse++;
            }
        }

        protected override void UpdateControllers(float elapsedClockSeconds)
        {
            base.UpdateControllers(elapsedClockSeconds);

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
            if (IsPlayerTrain)
            {
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
            }

            if (SmallEjectorControllerFitted)
            {
                SmallEjectorController.Update(elapsedClockSeconds);
                if (IsPlayerTrain)
                {
                    if (SmallEjectorController.UpdateValue > 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Increase, SmallEjectorController.CurrentValue * 100);
                    if (SmallEjectorController.UpdateValue < 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Decrease, SmallEjectorController.CurrentValue * 100);
                }
            }

            if (LargeEjectorControllerFitted)
            {
                LargeEjectorController.Update(elapsedClockSeconds);
                if (IsPlayerTrain)
                {
                    if (LargeEjectorController.UpdateValue > 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.LargeEjector, CabSetting.Increase, LargeEjectorController.CurrentValue * 100);
                    if (LargeEjectorController.UpdateValue < 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.LargeEjector, CabSetting.Decrease, LargeEjectorController.CurrentValue * 100);
                }
            }

            Injector1Controller.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (Injector1Controller.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector1, CabSetting.Increase, Injector1Controller.CurrentValue * 100);
                if (Injector1Controller.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector1, CabSetting.Decrease, Injector1Controller.CurrentValue * 100);
            }
            Injector2Controller.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (Injector2Controller.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector2, CabSetting.Increase, Injector2Controller.CurrentValue * 100);
                if (Injector2Controller.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector2, CabSetting.Decrease, Injector2Controller.CurrentValue * 100);
            }

            BlowerController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (BlowerController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Increase, BlowerController.CurrentValue * 100);
                if (BlowerController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, CabSetting.Decrease, BlowerController.CurrentValue * 100);
            }

            DamperController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (DamperController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Increase, DamperController.CurrentValue * 100);
                if (DamperController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, CabSetting.Decrease, DamperController.CurrentValue * 100);
            }
            FiringRateController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (FiringRateController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Increase, FiringRateController.CurrentValue * 100);
                if (FiringRateController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.FiringRate, CabSetting.Decrease, FiringRateController.CurrentValue * 100);
            }

            var oldFireboxDoorValue = FireboxDoorController.CurrentValue;
            if (IsPlayerTrain)
            {
                FireboxDoorController.Update(elapsedClockSeconds);
                if (FireboxDoorController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Increase, FireboxDoorController.CurrentValue * 100);
                if (FireboxDoorController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, CabSetting.Decrease, FireboxDoorController.CurrentValue * 100);
                if (oldFireboxDoorValue == 0 && FireboxDoorController.CurrentValue > 0)
                    SignalEvent(Event.FireboxDoorOpen);
                else if (oldFireboxDoorValue > 0 && FireboxDoorController.CurrentValue == 0)
                    SignalEvent(Event.FireboxDoorClose);
            }

            FuelController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (FuelController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.TenderCoal, CabSetting.Increase, FuelController.CurrentValue * 100);
            }

            WaterController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (WaterController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.TenderWater, CabSetting.Increase, WaterController.CurrentValue * 100);
            }
        }

        private void UpdateTender(float elapsedClockSeconds)
        {
            TenderWaterLevelFraction = CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;
            float TempCylinderSteamUsageLbpS = CylinderSteamUsageLBpS;
            // Limit Cylinder steam usage to the maximum boiler evaporation rate, lower limit is for when the locomotive is at rest and "no steam" is being used by cylinder, ensures some coal is used.
            TempCylinderSteamUsageLbpS = MathHelper.Clamp(TempCylinderSteamUsageLbpS, 0.1f, TheoreticalMaxSteamOutputLBpS);

            if (HasTenderCoupled) // If a tender is coupled then coal is available
            {
                TenderCoalMassKG -= elapsedClockSeconds * pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH(TempCylinderSteamUsageLbpS)])); // Current Tender coal mass determined by burn rate.
                TenderCoalMassKG = MathHelper.Clamp(TenderCoalMassKG, 0, MaxTenderCoalMassKG); // Clamp value so that it doesn't go out of bounds
            }
            else // if no tender coupled then check whether a tender is required
            {
                if (IsTenderRequired == 1.0)  // Tender is required
                {
                    TenderCoalMassKG = 0.0f; // Set tender coal to zero (none available)
                }
                else  // Tender is not required (ie tank locomotive) - therefore coal will be carried on the locomotive
                {
                    TenderCoalMassKG -= elapsedClockSeconds * pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH(TempCylinderSteamUsageLbpS)])); // Current Tender coal mass determined by burn rate.
                    TenderCoalMassKG = MathHelper.Clamp(TenderCoalMassKG, 0, MaxTenderCoalMassKG); // Clamp value so that it doesn't go out of bounds
                }
            }

            if (TenderCoalMassKG < 1.0)
            {
                if (!CoalIsExhausted)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Tender coal supply is empty. Your loco will fail."));
                }
                CoalIsExhausted = true;
            }
            else
            {
                CoalIsExhausted = false;
            }

            #region Auxiliary Water Tender Operation

            // If aux tender is coupled then assume that both tender and aux tender will equalise at same % water level
            // Tender water level will be monitored, and aux tender adjusted based upon this level
            // Add the value of water in the auxiliary tender to the tender water.
            // If Aux tender is uncoupled then assume that the % water level is the same in both the tender and the aux tender before uncoupling. Therefore calculate tender water based upon controller value and max tender water value.
            if (Train.IsAuxTenderCoupled)
            {
                if (SteamIsAuxTenderCoupled == false)
                {
                    CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG);  // Adjust water volume due to aux tender being connected
                    SteamIsAuxTenderCoupled = true;
                    // If water levels are different in the tender compared to the aux tender, then equalise them
                    MaxTotalCombinedWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) + (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG);
                    float CurrentTotalWaterVolumeUKG = CurrentAuxTenderWaterVolumeUKG + CombinedTenderWaterVolumeUKG;
                    float CurrentTotalWaterPercent = CurrentTotalWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;
                    // Calculate new water volumes in both the tender and aux tender
                    CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) * CurrentTotalWaterPercent;
                    CurrentLocoTenderWaterVolumeUKG = (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG) * CurrentTotalWaterPercent;
                }
            }
            else
            {
                if (SteamIsAuxTenderCoupled == true)  // When aux tender uncoupled adjust water in tender to remaining percentage.
                {
                    MaxTotalCombinedWaterVolumeUKG = Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG;
                    CombinedTenderWaterVolumeUKG = CurrentLocoTenderWaterVolumeUKG;  // Adjust water volume due to aux tender being uncoupled, adjust remaining tender water to whatever % value should be in tender
                    CombinedTenderWaterVolumeUKG = MathHelper.Clamp(CombinedTenderWaterVolumeUKG, 0, (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG)); // Clamp value so that it doesn't go out of bounds
                    CurrentAuxTenderWaterVolumeUKG = 0.0f;
                    SteamIsAuxTenderCoupled = false;
                }
            }

            // If refilling, as determined by increasing tender water level, then adjust aux tender water level at the same rate as the tender
            if (CombinedTenderWaterVolumeUKG > PreviousTenderWaterVolumeUKG)
            {
                CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG * WaterController.CurrentValue) / WaterLBpUKG);  // Adjust water volume due to aux tender being connected
                CurrentAuxTenderWaterVolumeUKG = MathHelper.Clamp(CurrentAuxTenderWaterVolumeUKG, 0, (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG)); // Clamp value so that it doesn't go out of bounds
            }

            if (HasTenderCoupled) // If a tender is coupled then water is available
            {
                CombinedTenderWaterVolumeUKG -= InjectorBoilerInputLB / WaterLBpUKG;  // Adjust water usage in tender
            }
            else // if no tender coupled then check whether a tender is required
            {
                if (IsTenderRequired == 1.0)  // Tender is required
                {
                    CombinedTenderWaterVolumeUKG = 0.0f;
                }
                else  // Tender is not required (ie tank locomotive) - therefore water will be carried on the locomotive (and possibly on aux tender)
                {
                    CombinedTenderWaterVolumeUKG -= InjectorBoilerInputLB / WaterLBpUKG;  // Adjust water usage in tender
                }
            }

            TenderWaterPercent = CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;  // Calculate the current % of water in tender
            RestoredMaxTotalCombinedWaterVolumeUKG = MaxTotalCombinedWaterVolumeUKG;
            RestoredCombinedTenderWaterVolumeUKG = CombinedTenderWaterVolumeUKG;
            CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) * TenderWaterPercent; // Adjust water level in aux tender
            CurrentLocoTenderWaterVolumeUKG = (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG) * TenderWaterPercent; // Adjust water level in locomotive tender
            PrevCombinedTenderWaterVolumeUKG = CombinedTenderWaterVolumeUKG;   // Store value for next iteration
            PreviousTenderWaterVolumeUKG = CombinedTenderWaterVolumeUKG;     // Store value for next iteration
            WaterConsumptionLbpS = InjectorBoilerInputLB / elapsedClockSeconds; // water consumption
            WaterConsumptionLbpS = MathHelper.Clamp(WaterConsumptionLbpS, 0, WaterConsumptionLbpS);
            CumulativeWaterConsumptionLbs += InjectorBoilerInputLB;
            if (CumulativeWaterConsumptionLbs > 0) DbfEvalCumulativeWaterConsumptionLbs = CumulativeWaterConsumptionLbs;//DebriefEval

#if DEBUG_AUXTENDER

            Trace.TraceInformation("============================================= Aux Tender (MSTSSTeamLocomotive.cs) =========================================================");
         //   Trace.TraceInformation("Water Level Is set by act {0}", Simulator.WaterInitialIsSet);
            Trace.TraceInformation("Combined Tender Water {0} Max Combined {1}", CombinedTenderWaterVolumeUKG, MaxTotalCombinedWaterVolumeUKG);
            Trace.TraceInformation("Tender Water {0} Max Tender Water {1}  Max Aux Tender {2}", CurrentLocoTenderWaterVolumeUKG, (Kg.ToLb(MaxLocoTenderWaterMassKG) / WaterLBpUKG), (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG));
            Trace.TraceInformation(" Water Percent {0} AuxTenderCoupled {1} SteamAuxTenderCoupled {2}", TenderWaterPercent, Train.IsAuxTenderCoupled, SteamIsAuxTenderCoupled);
            Trace.TraceInformation("Water Controller Current Value {0} Previous Value {1}", WaterController.CurrentValue, PreviousTenderWaterVolumeUKG);
#endif
            if (absSpeedMpS > 0.5) // Indicates train has moved, and therefore game started
            {
                AuxTenderMoveFlag = true;
            }

            #endregion

            if (CombinedTenderWaterVolumeUKG < 1.0)
            {
                if (!WaterIsExhausted && IsPlayerTrain)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Tender water supply is empty. Your loco will fail."));
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

            // Determine heat loss values that should not be considered when firing - ie safety valves and cylinder cocks - as these are mechanisms to control heat and we don't want to increase firing to cover these items
            float BoilerHeatExceptionsBtupS = SafetyValveBoilerHeatOutBTUpS + CylCockBoilerHeatOutBTUpS;

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
            #region Combsution (burn) rate for locomotive
            // Adjust burn rates for firing in either manual or AI mode
            if (FiringIsManual)
            {
                // Manual Firing - a small burning effect is maintained by the Radiation Steam Loss. The Blower is designed to be used when stationary, or if required when regulator is closed
                // The exhaust steam from the cylinders drives the draught through the firebox, the damper is used to reduce (or increase) the draft as required.
                BurnRateRawKGpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH((RadiationSteamLossLBpS + CalculatedCarHeaterSteamUsageLBpS) + BlowerBurnEffect + DamperBurnEffect)]));
            }
            else // ***********  AI Fireman *****************
            {

                if (PreviousTotalSteamUsageLBpS > TheoreticalMaxSteamOutputLBpS)
                {
                    FiringSteamUsageRateLBpS = TheoreticalMaxSteamOutputLBpS; // hold usage rate if steam usage rate exceeds boiler max output
                }
                else
                {
                    FiringSteamUsageRateLBpS = PreviousTotalSteamUsageLBpS; // Current steam usage rate
                }

                AIFiremanBurnFactorExceed = HeatRatio * PressureRatio;  // Firing rate for AI fireman if firemass drops, and fireman needs to exceed normal capacity
                AIFiremanBurnFactor = HeatRatio * PressureRatio * FullBoilerHeatRatio * MaxBoilerHeatRatio; // Firing rate factor under normal conditions
                float AIFiremanStartingBurnFactor = 4.0f;

                if (ShovelAnyway && BoilerHeatBTU < MaxBoilerHeatBTU) // will force fire burn rate to increase even though boiler heat seems excessive
                {
                    // burnrate will be the radiation loss @ rest & then related to heat usage as a factor of the maximum boiler output
                    // ignores total bolier heat to allow burn rate to increase if boiler heat usage is exceeding input
                    BurnRateRawKGpS = (W.ToKW(W.FromBTUpS(PreviousBoilerHeatOutBTUpS - BoilerHeatExceptionsBtupS)) / FuelCalorificKJpKG) * AIFiremanBurnFactorExceed;
                }
                else
                {

                    if (BoilerHeatBTU > MaxBoilerHeatBTU && BoilerHeatBTU <= MaxBoilerSafetyPressHeatBTU && throttle > 0.1 && cutoff > 0.1 && BoilerHeatInBTUpS < PreviousBoilerHeatOutBTUpS && FullMaxPressBoilerHeat)
                    // This allows for situation where boiler heat has gone beyond safety valve heat, and is reducing, but steam will be required shortly so don't allow fire to go too low
                    {
                        // Burn Rate rate if Boiler Heat is too high
                        BurnRateRawKGpS = (W.ToKW(W.FromBTUpS(PreviousBoilerHeatOutBTUpS - BoilerHeatExceptionsBtupS)) / FuelCalorificKJpKG) * AIFiremanStartingBurnFactor; // Calculate the amount of coal that should be burnt based upon heat used by boiler
                    }
                    else
                    {
                        // Burn Rate rate if Boiler Heat is "normal"
                        BurnRateRawKGpS = (W.ToKW(W.FromBTUpS(PreviousBoilerHeatOutBTUpS - BoilerHeatExceptionsBtupS)) / FuelCalorificKJpKG) * AIFiremanBurnFactor;
                    }
                }

                // AIFireOverride flag set to challenge driver if boiler AI fireman is overriden - ie steam safety valves will be set and blow if pressure is excessive
                if (SetFireOn || SetFireOff) // indicate that AI fireman override is in use
                {
                    AIFireOverride = true; // Set whenever SetFireOn or SetFireOff are selected
                }
                else if (BoilerPressurePSI < MaxBoilerPressurePSI && BoilerHeatSmoothedBTU < MaxBoilerHeatBTU && BoilerHeatBTU < MaxBoilerSafetyPressHeatBTU)
                {
                    AIFireOverride = false; // Reset if pressure and heat back to "normal"
                }

                if (SetFireReset)  // Check FireReset Override command - resets fireoff and fireon override
                {
                    SetFireOff = false;
                    SetFireOn = false;
                    SetFireReset = false;
                }

                // Check FireOff Override command - allows player to force fire low in preparation for a station stop
                if (SetFireOff)
                {
                    if (BoilerPressurePSI < MaxBoilerPressurePSI - 20.0f || BoilerHeatSmoothedBTU < 0.90f || (absSpeedMpS < 0.01f && throttle < 0.01f))
                    {
                        SetFireOff = false; // Disable FireOff if bolierpressure too low
                    }

                    BurnRateRawKGpS = 0.0035f;
                }

                // Check FireOn Override command - allows player to force the fire up in preparation for a station departure
                if (SetFireOn)
                {
                    if ((BoilerHeatSmoothedBTU > 0.995f * MaxBoilerHeatBTU && absSpeedMpS > 10.0f) || BoilerPressurePSI > MaxBoilerPressurePSI || absSpeedMpS <= 10.0f && (BoilerHeatSmoothedBTU > MaxBoilerHeatBTU || BoilerHeatBTU > 1.1f * MaxBoilerSafetyPressHeatBTU))
                    {
                        SetFireOn = false; // Disable FireOn if bolierpressure and boilerheat back to "normal"
                    }
                    BurnRateRawKGpS = 0.9f * pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH(TheoreticalMaxSteamOutputLBpS)])); // AI fire on goes to approx 100% of fire needed to maintain full boiler steam generation
                }
            }

            float MinimumBurnRateKGpS = 0.0012626f; // Set minimum burn rate @ 10lb/hr
            BurnRateRawKGpS = MathHelper.Clamp(BurnRateRawKGpS, MinimumBurnRateKGpS, BurnRateRawKGpS); // ensure burnrate never goes to zero, unless the fire drops to an unacceptable level, or a fusible plug blows

            FuelFeedRateKGpS = BurnRateRawKGpS;
            float MinimumFireLevelfactor = 0.05f; // factor representing the how low firemass has got compared to ideal firemass
            if (FireMassKG / IdealFireMassKG < MinimumFireLevelfactor) // If fire level drops too far 
            {
                BurnRateRawKGpS = 0.0f; // If fire is no longer effective set burn rate to zero, change later to allow graduate ramp down
                if (!FireIsExhausted)
                {
                    if (IsPlayerTrain)
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Fire has dropped too far. Your loco will fail."));
                    FireIsExhausted = true; // fire has run out of fuel.
                }
            }

            // Typically If the fire is too thick the air cannot pass through it. If the fire is too thin, excessive air passes through the firebed and holes
            // will be formed.In both cases the firebox temperature will be considerably reduced.
            // The information provided on pg 26 of BRITISH TRANSPORT COMMISSION - Handbook for Railway Steam Locomotive Enginemen - 
            // https://archive.org/details/HandbookForRailwaySteamLocomotiveEnginemen  has been used to model this aspect. Two formula have been developed from this information.
            // The % over or under the ideal value will be assumed to be the change in air combustion volume
            // Calculate the current firemass to the ideal firemass required for this type of locomotive
            FireRatio = FireMassKG / IdealFireMassKG;

            float TempFireHeatRatio = 1.0f; // Initially set ratio equal to 1.0

            if (FireRatio < 1.0f)
            {
                // If the coal mass drops below the ideal assume that "too much air" will be applied to the fire
                TempFireHeatRatio -= FireRatio; // Calculate air volume away from ideal mass
                TempFireHeatLossPercent = (0.0058f * TempFireHeatRatio * TempFireHeatRatio + 0.035f * TempFireHeatRatio); // Convert to a multiplier between 0 and 1
                TempFireHeatLossPercent = MathHelper.Clamp(TempFireHeatLossPercent, 0.0f, 1.0f); // Prevent % from being a negative number
            }
            else if (FireRatio > 1.0)
            {
                // If coal mass greater then ideal, assume too little air will get through the fire
                TempFireHeatRatio -= (FireRatio - 1.0f); // Calculate air volume away from ideal mass - must be a number between 0 and 1
                TempFireHeatLossPercent = (0.0434f * TempFireHeatRatio * TempFireHeatRatio - 0.1276f * TempFireHeatRatio);  // Convert to a multiplier between 0 and 1
                TempFireHeatLossPercent = MathHelper.Clamp(TempFireHeatLossPercent, 0.0f, 1.0f); // Prevent % from being a negative number
            }
            else
            {
                // If FireRatio is equal to 1.0, then ideal state has been reached
                FireHeatLossPercent = 1.0f;
            }

            FireHeatLossPercent = 1.0f - TempFireHeatLossPercent;
            FireHeatLossPercent = MathHelper.Clamp(FireHeatLossPercent, 0.0f, FireHeatLossPercent); // Prevent % from being a negative number

            // test for fusible plug
            if (FusiblePlugIsBlown)
            {
                BurnRateRawKGpS = 0.0f; // Drop fire due to melting of fusible plug and steam quenching fire, change later to allow graduate ramp down.
            }

            BurnRateSmoothKGpS.Update(elapsedClockSeconds, BurnRateRawKGpS);
            FuelBurnRateSmoothedKGpS = BurnRateSmoothKGpS.SmoothedValue;
            FuelBurnRateSmoothedKGpS = MathHelper.Clamp(FuelBurnRateSmoothedKGpS, 0.0f, MaxFuelBurnGrateKGpS); // clamp burnrate to max fuel that can be burnt within grate limit
            #endregion

            #region Firing (feeding fuel) Rate of locomotive

            if (FiringIsManual)
            {
                FuelRateSmoothed = CoalIsExhausted ? 0 : FiringRateController.CurrentValue;
                FuelFeedRateKGpS = MaxFiringRateKGpS * FuelRateSmoothed;
            }
            else if (elapsedClockSeconds > 0.001 && MaxFiringRateKGpS > 0.001)
            {
                // Automatic fireman, ish.
                DesiredChange = MathHelper.Clamp(((IdealFireMassKG - FireMassKG) + FuelBurnRateSmoothedKGpS) / MaxFiringRateKGpS, 0.001f, 1);
                if (StokerIsMechanical) // if a stoker is fitted expect a quicker response to fuel feeding
                {
                    FuelRateStoker.Update(elapsedClockSeconds, DesiredChange); // faster fuel feed rate for stoker    
                    FuelRateSmoothed = CoalIsExhausted ? 0 : FuelRateStoker.SmoothedValue; // If tender coal is empty stop fuelrate (feeding coal onto fire). 
                }
                else
                {
                    FuelRate.Update(elapsedClockSeconds, DesiredChange); // slower fuel feed rate for fireman
                    FuelRateSmoothed = CoalIsExhausted ? 0 : FuelRate.SmoothedValue; // If tender coal is empty stop fuelrate (feeding coal onto fire).
                }

                float CurrentFireLevelfactor = 0.95f; // factor representing the how low firemass has got compared to ideal firemass  
                if ((FireMassKG / IdealFireMassKG) < CurrentFireLevelfactor) // if firemass is falling too low shovel harder - set @ 85% - needs further refinement as this shouldn't be able to be maintained indefinitely
                {
                    if (FuelBoostOnTimerS < TimeFuelBoostOnS) // If fuel boost timer still has time available allow fuel boost
                    {
                        FuelBoostResetTimerS = 0.01f;     // Reset fuel reset (time out) timer to allow stop boosting for a period of time.
                        if (!FuelBoost)
                        {
                            FuelBoost = true; // boost shoveling 
                            if (!StokerIsMechanical && IsPlayerTrain)  // Don't display message if stoker in operation
                            {
                                Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("FireMass is getting low. Your fireman will shovel faster, but don't wear him out."));
                            }
                        }
                    }
                }
                else if (FireMassKG >= IdealFireMassKG) // If firemass has returned to normal - turn boost off
                {
                    if (FuelBoost)
                    {
                        FuelBoost = false; // disable boost shoveling 
                        if (!StokerIsMechanical && IsPlayerTrain)  // Don't display message if stoker in operation
                        {
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("FireMass is back within limits. Your fireman will shovel as per normal."));
                        }
                    }
                }
                if (FuelBoost && !FuelBoostReset) // if fuel boost is still on, and hasn't been reset - needs further refinement as this shouldn't be able to be maintained indefinitely
                {
                    DisplayMaxFiringRateKGpS = MaxTheoreticalFiringRateKgpS; // Set display value with temporary higher shovelling level
                    FuelFeedRateKGpS = MaxTheoreticalFiringRateKgpS * FuelRateSmoothed;  // At times of heavy burning allow AI fireman to overfuel
                    FuelBoostOnTimerS += elapsedClockSeconds; // Time how long to fuel boost for
                }
                else
                {
                    DisplayMaxFiringRateKGpS = MaxFiringRateKGpS; // Rest display max firing rate to new figure
                    FuelFeedRateKGpS = MaxFiringRateKGpS * FuelRateSmoothed;
                }
            }
            // Calculate update to firemass as a result of adding fuel to the fire
            FireMassKG += elapsedClockSeconds * (FuelFeedRateKGpS - FuelBurnRateSmoothedKGpS);
            FireMassKG = MathHelper.Clamp(FireMassKG, 0, MaxFireMassKG);
            GrateCombustionRateLBpFt2 = pS.TopH(Kg.ToLb(FuelBurnRateSmoothedKGpS) / Me2.ToFt2(GrateAreaM2)); //coal burnt per sq ft grate area per hour
            // Time Fuel Boost reset timer if all time has been used up on boost timer
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
            #endregion
        }

        private void UpdateBoiler(float elapsedClockSeconds)
        {
            absSpeedMpS = Math.Abs(Train.SpeedMpS);

            #region Safety valves - determine number and size

            // Determine number and size of safety valves
            // Reference: Ashton's POP Safety valves catalogue
            // To calculate size use - Total diam of safety valve = 0.036 x ( H / (L x P), where H = heat area of boiler sq ft (not including superheater), L = valve lift (assume 0.1 in for Ashton valves), P = Abs pressure psi (gauge pressure + atmospheric)

            const float ValveSizeCalculationFactor = 0.036f;
            const float ValveLiftIn = 0.1f;
            float ValveSizeTotalDiaIn = ValveSizeCalculationFactor * (Me2.ToFt2(EvaporationAreaM2) / (ValveLiftIn * (MaxBoilerPressurePSI + OneAtmospherePSI)));

            ValveSizeTotalDiaIn += 1.0f; // Add safety margin to align with Ashton size selection table

            // There will always be at least two safety valves to allow for a possible failure. There may be up to four fitted to a locomotive depending upon the size of the heating area. Therefore allow for combinations of 2x, 3x or 4x.
            // Common valve sizes are 2.5", 3", 3.5" and 4".

            // Test for 2x combinations
            float TestValveSizeTotalDiaIn = ValveSizeTotalDiaIn / 2.0f;
            if (TestValveSizeTotalDiaIn <= 4.0f)
            {
                NumSafetyValves = 2.0f;   // Assume that there are 2 safety valves
                if (TestValveSizeTotalDiaIn <= 2.5)
                {
                    SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
                }
                if (TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
                {
                    SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
                }
                if (TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
                {
                    SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
                }
                if (TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
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
                    if (TestValveSizeTotalDiaIn <= 2.5)
                    {
                        SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
                    }
                    if (TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
                    {
                        SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
                    }
                    if (TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
                    {
                        SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
                    }
                    if (TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
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
                        if (TestValveSizeTotalDiaIn <= 2.5)
                        {
                            SafetyValveSizeIn = 2.5f; // Safety valve is a 2.5" diameter unit
                        }
                        if (TestValveSizeTotalDiaIn > 2.5 && TestValveSizeTotalDiaIn <= 3.0)
                        {
                            SafetyValveSizeIn = 3.0f; // Safety valve is a 3.0" diameter unit
                        }
                        if (TestValveSizeTotalDiaIn > 3.0 && TestValveSizeTotalDiaIn <= 3.5)
                        {
                            SafetyValveSizeIn = 3.5f; // Safety valve is a 3.5" diameter unit
                        }
                        if (TestValveSizeTotalDiaIn > 3.5 && TestValveSizeTotalDiaIn <= 4.0)
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
            // Set "valve area" of safety valve, based on reverse engineered values of steam, valve area is determined by lift and the gap created 
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
            float SafetyValveOpen1Psi = MaxBoilerPressurePSI + SafetyValveStartPSI;
            float SafetyValveClose1Psi = MaxBoilerPressurePSI - 4.0f;
            float SafetyValveOpen2Psi = MaxBoilerPressurePSI + 2.0f;
            float SafetyValveClose2Psi = MaxBoilerPressurePSI - 3.0f;
            float SafetyValveOpen3Psi = MaxBoilerPressurePSI + 4.0f;
            float SafetyValveClose3Psi = MaxBoilerPressurePSI - 2.0f;
            float SafetyValveOpen4Psi = MaxBoilerPressurePSI + 6.0f;
            float SafetyValveClose4Psi = MaxBoilerPressurePSI - 1.0f;

            #endregion

            if (FiringIsManual) // Operate safety valves if manual firing or AI Fire Override is active
            {

                #region Safety Valve - Manual Firing

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
                    SafetyValveBoilerHeatOutBTUpS = SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
                }
                else
                {
                    SafetyValveUsageLBpS = 0.0f;
                }

                #endregion

            }

            else
            {
                #region Safety Valve - AI Firing
                // turn safety valves on if boiler heat is excessive, and fireman is not trying to raise steam for rising gradient by using the AI fire override
                if (AIFireOverride && BoilerPressurePSI > MaxBoilerPressurePSI + SafetyValveStartPSI)
                {
                    SignalEvent(Event.SteamSafetyValveOn);
                    SafetyIsOn = true;
                }

                // turn safety vales off if boiler heat has returned to "normal", fitreman is no longer in override mode
                else if (!AIFireOverride && BoilerPressurePSI < MaxBoilerPressurePSI - SafetyValveDropPSI)
                {
                    SignalEvent(Event.SteamSafetyValveOff);
                    SafetyIsOn = false;
                }

                if (SafetyIsOn)
                {
                    SafetyValveUsageLBpS = MaxSafetyValveDischargeLbspS;   // For the AI fireman use the maximum possible safety valve steam volume
                    BoilerMassLB -= elapsedClockSeconds * SafetyValveUsageLBpS;
                    BoilerHeatBTU -= elapsedClockSeconds * SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve
                    TotalSteamUsageLBpS += SafetyValveUsageLBpS;
                    BoilerHeatOutBTUpS += SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve
                    BoilerHeatOutSVAIBTUpS = SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Use this value to adjust the burn rate in AI mode if safety valves operate, main usage value used for display values
                    SafetyValveBoilerHeatOutBTUpS = SafetyValveUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
                }
                else
                {
                    SafetyValveUsageLBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }

                #endregion

            }

            // Update details for blowdown vlave
            if (BlowdownValveOpen)
            {
                // Use Napier formula to calculate steam discharge rate through safety valve, ie Discharge (lb/s) = (Valve area * Abs Pressure) / 70
                // 
                const float BlowdownValveDischargeFactor = 70.0f;

                // Find area of pipe - assume 1.5" dia pressure pipe
                BlowdownValveSizeDiaIn2 = (float)Math.PI * (1.5f / 2.0f) * (1.5f / 2.0f);

                BlowdownSteamUsageLBpS = (BlowdownValveSizeDiaIn2 * (MaxBoilerPressurePSI + OneAtmospherePSI)) / BlowdownValveDischargeFactor;

                BoilerMassLB -= elapsedClockSeconds * BlowdownSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by blower  
                BoilerHeatBTU -= elapsedClockSeconds * BlowdownSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by blower
                BoilerHeatOutBTUpS += BlowdownSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by blower
                TotalSteamUsageLBpS += BlowdownSteamUsageLBpS;
            }
            else
            {
                BlowdownSteamUsageLBpS = 0; // Turn off Hud view
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

            if (!FiringIsManual)
            {
                // FullBoilerHeatRatio - provides a multiplication factor which attempts to reduce the firing rate of the locomotive in AI Firing if the boiler Heat
                // exceeds the heat in the boiler when the boiler is at full operating pressure
                if (BoilerHeatBTU > MaxBoilerHeatBTU && BoilerHeatInBTUpS > (PreviousBoilerHeatOutBTUpS * 0.90f)) // Limit boiler heat to max value for the boiler
                {
                    float FullBoilerHeatRatioMaxRise = -1.0f;
                    float FullBoilerHeatRatioMaxRun = MaxBoilerSafetyPressHeatBTU - MaxBoilerHeatBTU;
                    float FullBoilerHeatRatioGrad = FullBoilerHeatRatioMaxRise / FullBoilerHeatRatioMaxRun;

                    if (BoilerHeatBTU <= MaxBoilerHeatBTU)
                    // If boiler heat is "normal" then set HeatRatio to 1.0
                    {
                        FullBoilerHeat = false;
                        FullBoilerHeatRatio = 1.0f; // if boiler pressure close to normal set pressure ratio to normal
                        FullMaxPressBoilerHeat = false; // Rest flag
                    }
                    else if (BoilerHeatBTU > MaxBoilerHeatBTU && BoilerHeatBTU <= MaxBoilerSafetyPressHeatBTU && !FullMaxPressBoilerHeat && BoilerHeatSmoothedBTU > (MaxBoilerHeatBTU * 0.995))
                    // If boiler heat exceeds full boiler heat, but is less then the MaxPressHeat, and is not coming down from having exceeded the safety valve heat then set to variable value which tries to reduce the heat level
                    {
                        FullBoilerHeatRatio = (FullBoilerHeatRatioGrad * (BoilerHeatBTU - MaxBoilerHeatBTU)) + 1.0f;
                        FullBoilerHeat = true;

                    }
                    else if (BoilerHeatBTU > MaxBoilerSafetyPressHeatBTU)
                    // if heat level has exceeded the safety valve heat
                    {
                        FullBoilerHeatRatio = 0.1f;
                        FullMaxPressBoilerHeat = true;  // Set flag to indicate that heat has exceeded the safety valve heat
                    }
                }
                else
                {
                    FullBoilerHeat = false;
                    FullBoilerHeatRatio = 1.0f; // if boiler pressure back to normal, set pressure ratio to normal
                }

                if (BoilerHeatBTU < MaxBoilerHeatBTU || BoilerHeatInBTUpS > PreviousBoilerHeatOutBTUpS)
                {
                    FullMaxPressBoilerHeat = false; // Reset flag
                }

                // MaxBoilerHeatRatio - provides a multiplication factor which attempts to reduce the firing rate of the locomotive in AI Firing if the boiler Heat
                // exceeds the heat in the boiler when the boiler is at the pressure of the safety valves.
                // Set Heat Ratio if boiler heat excceds the maximum boiler limit
                if (BoilerHeatBTU > MaxBoilerSafetyPressHeatBTU)  // Limit boiler heat further if heat excced the pressure that the safety valves are set to.
                {

                    if (BoilerHeatBTU > MaxBoilerSafetyPressHeatBTU * 1.1)
                    {
                        MaxBoilerHeatRatio = 0.01f;
                    }
                    else if (BoilerHeatInBTUpS > PreviousBoilerHeatOutBTUpS)
                    {
                        float FactorPower = BoilerHeatBTU / (MaxBoilerSafetyPressHeatBTU - MaxBoilerHeatBTU);
                        MaxBoilerHeatRatio = MaxBoilerHeatBTU / (float)Math.Pow(BoilerHeatBTU, FactorPower);
                    }
                    else
                    {
                        MaxBoilerHeatRatio = 1.0f;
                    }

                }
                else
                {
                    MaxBoilerHeatRatio = 1.0f;
                }
                MaxBoilerHeatRatio = MathHelper.Clamp(MaxBoilerHeatRatio, 0.001f, 1.0f); // Keep Max Boiler Heat ratio within bounds
            }

            // Calculate the amount of heat produced by the fire - this is naturally limited by the Grate Limit (see above), and also by the combustion oxygen
            FireHeatTxfKW = FuelCalorificKJpKG * FuelBurnRateSmoothedKGpS * FireHeatLossPercent;

            // Provide information message only to user if grate limit is exceeded.
            if (GrateCombustionRateLBpFt2 > GrateLimitLBpFt2)
            {
                if (!IsGrateLimit)  // Provide message to player that grate limit has been exceeded
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Grate limit exceeded - boiler heat rate cannot increase."));
                }
                IsGrateLimit = true;
            }
            else
            {
                if (IsGrateLimit)  // Provide message to player that grate limit has now returned within limits
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Grate limit return to normal."));
                }
                IsGrateLimit = false;
            }

            BoilerHeatInBTUpS = W.ToBTUpS(W.FromKW(FireHeatTxfKW) * BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateSmoothedKGpS)) / Me2.ToFt2(GrateAreaM2))]);
            BoilerHeatBTU += elapsedClockSeconds * W.ToBTUpS(W.FromKW(FireHeatTxfKW) * BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateSmoothedKGpS)) / Me2.ToFt2(GrateAreaM2))]);

            // This section calculates heat radiation loss from the boiler. This section is based upon the description provided in "The Thermal Insulation of the Steam Locomotive" (Paper 501) published in the
            // which was published in the March 1, 1951 Journal of the Institution of Locomotive Engineers.
            // In basic terms,  Heat loss = Kc * A * dT, where Kc = heat transfer coefficient, A = heat transfer area, and dT = difference in temperature, ie (Boiler Temp - Ambient Temp)
            
            // Calculate the temp differential
            float TemperatureDifferentialF = 0;
            
            TemperatureDifferentialF = C.ToF(C.FromK(BoilerWaterTempK) - CarOutsideTempC);

            // As locomotive moves, the Kc value will increase as more heat loss occurs with greater speed.
            // This section calculates the variation of Kc with speed, and is based upon the information provided here - https://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html
            // In short Kc = 10.45 - v + 10 v1/2 - where v = speed in m/s. This formula flattens out above 20m/s, so this will be used as the maximum figure, and a fraction determined from it.
            // It is only valid at lower speeds, ie 2m/s (5mph) so there is no change in Kc below this value

            float LowSpeedMpS = 2.0f;
            float HighSpeedMpS = 20.0f;
            float KcMinSpeed = 10.45f - LowSpeedMpS + (10.0f * (float)Math.Pow(LowSpeedMpS, 0.5)); // Minimum speed of 2m/s
            float KcMaxSpeed = 10.45f - HighSpeedMpS + (10.0f * (float)Math.Pow(HighSpeedMpS, 0.5)); // Maximum speed of 20m/s
            float KcActualSpeed = 10.45f - absSpeedMpS + (10.0f * (float)Math.Pow(absSpeedMpS, 0.5));
            float KcMovementFraction = 0;

            if (absSpeedMpS > 2 && absSpeedMpS < 20.0f)
            {
                KcMovementFraction = KcActualSpeed / KcMinSpeed; // Calculate fraction only between 2 and 20
            }
            else if (absSpeedMpS < 2)
            {
                KcMovementFraction = 1.0f; // If speed less then 2m/s then set fracftion to give stationary Kc value 
            }
            else
            {
                KcMovementFraction = KcMaxSpeed / KcMinSpeed; // Calculate constant fraction over 20m/s
            }
            
            //            Trace.TraceInformation("Fraction - {0} Speed {1} MinSpeed {2} Actual {3}", KcMovementFraction, absSpeedMpS, KcMinSpeed, KcActualSpeed);
            
            // Calculate radiation loss - has two elements, insulated and uninsulated
            float UninsulatedBoilerHeatRadiationLossBTU = BoilerSurfaceAreaFt2 * (1.0f - FractionBoilerAreaInsulated) * KcMovementFraction * KcUninsulation * TemperatureDifferentialF;
            float InsulatedBoilerHeatRadiationLossBTU = BoilerSurfaceAreaFt2 * FractionBoilerAreaInsulated * KcMovementFraction * KcInsulation * TemperatureDifferentialF;
            BoilerHeatRadiationLossBTU = UninsulatedBoilerHeatRadiationLossBTU + InsulatedBoilerHeatRadiationLossBTU;
            
            //            Trace.TraceInformation("Heat Loss - {0} Area {1} TempDiff {2} Speed {3} MoveFraction {4}", BoilerHeatRadiationLossBTU, BoilerSurfaceAreaFt2, TemperatureDifferentialF, absSpeedMpS, KcMovementFraction);
            
            // Temporary calculation to maintain smoke stack and minimum coal feed in AI firing - could be changed
            RadiationSteamLossLBpS = pS.FrompH(BoilerHeatRadiationLossBTU) / (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
            
                     //   Trace.TraceInformation("RadLoss {0}", RadiationSteamLossLBpS);
            
            BoilerHeatBTU -= elapsedClockSeconds * pS.FrompH(BoilerHeatRadiationLossBTU);
            BoilerHeatOutBTUpS += pS.FrompH(BoilerHeatRadiationLossBTU);

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
            WaterFraction = MathHelper.Clamp(WaterFraction, 0.0f, 1.01f); // set water fraction limits so that it doesn't go below zero or exceed full boiler volume

            // Update Boiler Heat based upon current Evaporation rate
            // Based on formula - BoilerCapacity (btu/h) = (SteamEnthalpy (btu/lb) - EnthalpyCondensate (btu/lb) ) x SteamEvaporated (lb/h) ?????
            // EnthalpyWater (btu/lb) = BoilerCapacity (btu/h) / SteamEvaporated (lb/h) + Enthalpysteam (btu/lb)  ?????

            BoilerHeatSmoothBTU.Update(elapsedClockSeconds, BoilerHeatBTU);

            BoilerHeatSmoothedBTU = MathHelper.Clamp(BoilerHeatSmoothBTU.SmoothedValue, 0.0f, (MaxBoilerSafetyPressHeatBTU * 1.05f));

            WaterHeatBTUpFT3 = (BoilerHeatSmoothedBTU / BoilerVolumeFT3 - (1 - WaterFraction) * BoilerSteamDensityLBpFT3 * BoilerSteamHeatBTUpLB) / (WaterFraction * BoilerWaterDensityLBpFT3);

            #region Boiler Pressure calculation
            // works on the principle that boiler pressure will go up or down based on the change in water temperature, which is impacted by the heat gain or loss to the boiler
            WaterVolL = WaterFraction * BoilerVolumeFT3 * 28.31f;   // idealy should be equal to water flow in and out. 1ft3 = 28.31 litres of water
            // Calculate difference in boiler rating, ie heat in - heat out - 1 BTU = 0.0002931 kWh, divide by 3600????
            if (PreviousBoilerHeatSmoothedBTU != 0.0)
            {
                BkW_Diff = (pS.TopH((BoilerHeatSmoothedBTU - PreviousBoilerHeatSmoothedBTU)) * 0.0002931f);            // Calculate difference in boiler rating, ie heat in - heat out - 1 BTU = 0.0002931 kWh, divide by 3600????
            }
            SpecificHeatWaterKJpKGpC = SpecificHeatKtoKJpKGpK[WaterTempNewK] * WaterVolL;  // Spec Heat = kj/kg, litres = kgs of water
            WaterTempInK = BkW_Diff / SpecificHeatWaterKJpKGpC;   // calculate water temp variation
            WaterTempNewK += WaterTempInK; // Calculate new water temp
            WaterTempNewK = MathHelper.Clamp(WaterTempNewK, 274.0f, 496.0f);

            PreviousBoilerHeatSmoothedBTU = BoilerHeatSmoothedBTU;

            if (FusiblePlugIsBlown)
            {
                BoilerPressurePSI = 0.50f; // Drop boiler pressure if fusible plug melts.
            }
            else
            {
                BoilerPressurePSI = SaturationPressureKtoPSI[WaterTempNewK]; // Gauge Pressure
            }

            if (!FiringIsManual)
            {
                //PressureRatio - provides a multiplication factor which attempts to increase the firing rate of the locomotive in AI Firing if the boiler pressure
                // drops below the normal operating pressure
                if (BoilerHeatBTU > MaxBoilerHeatBTU || (BoilerHeatInBTUpS > (PreviousBoilerHeatOutBTUpS * 1.05f)))  // Cap pressure ratio if boiler heat is excessive or steam consumption exceeds production
                {
                    PressureRatio = 1.0f;
                }
                else
                {
                    // The pressure ratio forces the fire to burn harder if the pressure drops below the maximum boiler pressure - 2psi.

                    float PressureRatioMaxRise = 1.75f;
                    float PressureRatioMaxRun = 20.0f;
                    float PressureRatioGrad = PressureRatioMaxRise / PressureRatioMaxRun;
                    if (BoilerPressurePSI > (MaxBoilerPressurePSI - 2.0f) && BoilerPressurePSI <= MaxBoilerPressurePSI)
                    {
                        PressureRatio = 1.0f; // if boiler pressure close to normal set pressure ratio to normal
                    }
                    else if (BoilerPressurePSI > (MaxBoilerPressurePSI - PressureRatioMaxRun) && BoilerPressurePSI <= (MaxBoilerPressurePSI))
                    {
                        PressureRatio = PressureRatioGrad * (MaxBoilerPressurePSI - BoilerPressurePSI) + 1.0f;
                    }
                    else if (BoilerPressurePSI <= (MaxBoilerPressurePSI - PressureRatioMaxRun))
                    {
                        PressureRatio = PressureRatioMaxRise + 1.0f;
                    }
                    PressureRatio = MathHelper.Clamp(PressureRatio, 0.001f, (PressureRatioMaxRise + 1.0f)); // Boiler pressure ratio to adjust burn rate
                }

            }
            #endregion

            // Cap Boiler pressure under certain circumstances

            // Ai Fireman
            if (!FiringIsManual && BoilerPressurePSI > MaxBoilerPressurePSI) // For AI fireman stop excessive pressure
            {
                if (!AIFireOverride)
                {
                    BoilerPressurePSI = MaxBoilerPressurePSI;  // Check for AI firing
                }
            }

            // Manual Fireman - clamp pressure at pressure just over top safety valve

            BoilerPressurePSI = MathHelper.Clamp(BoilerPressurePSI, 0.000f, (MaxBoilerPressurePSI + 7.0f)); // Clamp Boiler pressure to maximum safety valve pressure

            ApplyBoilerPressure();

            // Calculate cummulative steam consumption
            CummulativeTotalSteamConsumptionLbs += PreviousTotalSteamUsageLBpS * elapsedClockSeconds;
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
            DrvWheelRevRpS = absSpeedMpS / (2.0f * MathHelper.Pi * DriverWheelRadiusM);

            // To calculate TE and steam consumption OR calculates the ideal steam cylinder indicator (pressure / volume) diagram.
            // Parameter names used through out this section are based upon the indicator diagrams found at the following location:
            // Single expansion locomotive - http://www.coalstonewcastle.com.au/physics/steam-intro/#indicator_simple
            // Compound locomotive - http://www.coalstonewcastle.com.au/physics/steam-intro/#indicator_compound
            // The following parameter naming convention has been used to identify the relevant points within the cylinder, and the corresponding volumes and pressures.
            // Pressure - LPPressure_a_AtmPSI - the pressure that occurs at point a in the LP cylinder on the relevant indicator diagram (in Atmospheric pressure)
            // Mean Pressure - HPMeanPressure_ab_AtmPSI - the mean pressure between points a & b in the HP cylinder 
            // Volume - HPCylinderVolumePoint_d - the volume in the HP cylinder at point d 

            // Initialise values used in this module


#if DEBUG_STEAM_CYLINDER_EVENTS

            // THIS CODE IS NOT FULLY OPERATIONAL AT THIS TIME

            float ValveHalfTravel = ValveTravel / 2.0f;

            // Valve events calculated using Zeuner Diagram
            // References - Valve-gears, Analysis by the Zeuner diagram : Spangler, H. W. -  https://archive.org/details/valvegearsanalys00spanrich
            // Zeuner Diagram by Charles Dockstader used as a reference source (Note - the release value seems to be incorrect ) - http://www.billp.org/Dockstader/ValveGear.html

            if (cutoff != 0) // If cutoff is > 0, then calculate valve events
            {
                // Calculate cutoff point on axis axis, and then the relevant point on the valve trael circle
                float ValveCutOffAxis = cutoff * ValveTravel;
                float ValveCutOffOrigin = ValveCutOffAxis - ValveHalfTravel;
                float ValveCutOffAxisAng = (float)Math.Acos(ValveCutOffOrigin / ValveHalfTravel);
                float ValvePointX0 = ValveHalfTravel * (float)Math.Cos(ValveCutOffAxisAng);
                float ValvePointY0 = ValveHalfTravel * (float)Math.Sin(ValveCutOffAxisAng);

                float ValvePointX1 = -ValveHalfTravel;
                float ValvePointY1 = 0.0f;

                float DistanceP0P1 = (float)Math.Sqrt((float)Math.Pow((ValvePointX0 - ValvePointX1), 2) + (float)Math.Pow((ValvePointY0 - ValvePointY1), 2));
                float DistanceLead = ValveLead;
                float DistanceP0P3 = (float)Math.Sqrt((float)Math.Pow(DistanceP0P1, 2) - (float)Math.Pow(DistanceLead, 2));

                float A = ((float)Math.Pow(DistanceP0P3, 2) - (float)Math.Pow(DistanceLead, 2) + (float)Math.Pow(DistanceP0P1, 2)) / (2.0f * DistanceP0P1);
                float H = (float)Math.Pow(DistanceP0P3, 2) - (float)Math.Pow(A, 2);
                float B = ((float)Math.Pow(DistanceLead, 2) - (float)Math.Pow(DistanceP0P3, 2) + (float)Math.Pow(DistanceP0P1, 2)) / (2.0f * DistanceP0P1);

                float ValvePointX2 = ((DistanceP0P1 - A) / DistanceP0P1) * (float)Math.Abs(ValvePointX0 - ValvePointX1) + ValvePointX1;
                float ValvePointY2 = ((DistanceP0P1 - A) / DistanceP0P1) * (float)Math.Abs(ValvePointY0 - ValvePointY1) + ValvePointY1;

                float ValvePointX3 = ValvePointX2 - (H * (ValvePointY1 - ValvePointY0)) / DistanceP0P1;
                float ValvePointY3 = ValvePointY2 + (H * (ValvePointX1 - ValvePointX0)) / DistanceP0P1;

                float ValveGradientAdminCutoff = (ValvePointY0 - ValvePointY3) / (ValvePointX0 - ValvePointX3);
                float ValveAdvanceAngleRadians = (float)Math.Atan(ValveGradientAdminCutoff);
                ValveAdvanceAngleDeg = MathHelper.ToDegrees(ValveAdvanceAngleRadians);

                //   Trace.TraceInformation("Check - Grad {0} ATAN {1} Deg {2}", ValveGradientAdminCutoff, ValveAdvanceAngleRadians, ValveAdvanceAngle);
                //   Trace.TraceInformation("Valve - Angle {0} x0 {1} y1{2}", ValveCutOffAxisAng, ValveCutOffPointX0, ValveCutOffPointY0);
            }
            else
            {
                ValveAdvanceAngleDeg = 0.0f;
            }
            
#endif

            // Set Cylinder Events according to cutoff value selected

            CylinderExhaustOpenFactor = CylinderExhausttoCutoff[cutoff];
            CylinderCompressionCloseFactor = CylinderCompressiontoCutoff[cutoff];
            CylinderAdmissionOpenFactor = CylinderAdmissiontoCutoff[cutoff];

            float DebugWheelRevs = pS.TopM(DrvWheelRevRpS);

            #region Calculation of Mean Effective Pressure of Cylinder using an Indicator Diagram type approach - Compound Locomotive - No receiver

            // Prinipal reference for compound locomotives: Compound Locomotives by Arthur Tannatt Woods - https://archive.org/stream/compoundlocomoti00woodrich#page/n5/mode/2up

            if (SteamEngineType == SteamEngineTypes.Compound)
            {

                CylinderCompressionCloseFactor = 1.0f - CylinderExhausttoCutoff[cutoff];  // In case of Vuclain locomotive Compression in each of the cylinders is inverse of release (cylinders aligned)

                // Define volume of cylinder at different points on cycle - the points align with points on indicator curve
                // Note: All LP cylinder values to be multiplied by Cylinder ratio to adjust volumes to same scale
                float HPCylinderVolumePoint_a = HPCylinderClearancePC;
                float HPCylinderVolumePoint_b = cutoff + HPCylinderClearancePC;
                float HPCylinderVolumePoint_d = CylinderExhaustOpenFactor + HPCylinderClearancePC;
                float HPCylinderVolumePoint_e = CylinderExhaustOpenFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP;
                float HPCylinderVolumePoint_f = HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP;
                float HPCylinderVolumePoint_fHPonly = HPCylinderVolumeFactor + HPCylinderClearancePC; // Volume @ f only in HP Cylinder only
                float LPCylinderVolumePoint_g = HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP + (LPCylinderClearancePC * CompoundCylinderRatio);
                float LPCylinderVolumePoint_h_pre = cutoff + HPCylinderClearancePC + CompoundRecieverVolumePCHP + ((LPCylinderClearancePC + cutoff) * CompoundCylinderRatio); // before cutoff
                float LPCylinderVolumePoint_h_LPpost = cutoff + LPCylinderClearancePC; // in LP Cylinder post cutoff
                float HPCylinderVolumePoint_h_HPpost = cutoff + HPCylinderClearancePC + CompoundRecieverVolumePCHP;   // in HP Cylinder + steam passages post cutoff
                float HPCylinderVolumePoint_hHPonly = cutoff + HPCylinderClearancePC;   // in HP Cylinder only post cutoff
                float HPCylinderVolumePoint_k_pre = (CylinderCompressionCloseFactor) + HPCylinderClearancePC + CompoundRecieverVolumePCHP;   // Before exhaust valve closure
                float HPCylinderVolumePoint_k_post = (CylinderCompressionCloseFactor) + HPCylinderClearancePC;   // after exhaust valve closure
                float LPCylinderVolumePoint_l = (CylinderExhaustOpenFactor + LPCylinderClearancePC); // in LP Cylinder post cutoff
                float LPCylinderVolumePoint_n = (CylinderCompressionCloseFactor + LPCylinderClearancePC); // in LP Cylinder @ Release
                float LPCylinderVolumePoint_m = (LPCylinderVolumeFactor + LPCylinderClearancePC); // in LP Cylinder @ end of stroke
                float LPCylinderVolumePoint_a = LPCylinderClearancePC;
                float LPCylinderVolumePoint_q = (CylinderAdmissionOpenFactor) + LPCylinderClearancePC;
                float HPCylinderVolumePoint_u = (CylinderAdmissionOpenFactor) + HPCylinderClearancePC;

                SteamChestPressurePSI = (throttle * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

                LogSteamChestPressurePSI = SteamChestPressurePSI;  // Value for recording in log file
                LogSteamChestPressurePSI = MathHelper.Clamp(LogSteamChestPressurePSI, 0.00f, LogSteamChestPressurePSI); // Clamp so that steam chest pressure does not go negative

                if (CylinderCompoundOn)
                {
                    // ***** Simple mode *****
                    // Compound bypass valve open - puts locomotive into simple (single expansion) mode - boiler steam is fed to both steam cylinders at the same time. 
                    // Thus both the HP and LP act as single expansion cylinder operating in parallel. Both HP and LP will acts as single expansion cylinders
                    // HP Cylinder parameters do not have a prefix

                    // (a) - Initial Pressure (For LP equates to point g)
                    // LP Cylinder
                    LPPressure_a_AtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                    LogLPInitialPressurePSI = LPPressure_a_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPInitialPressurePSI = MathHelper.Clamp(LogLPInitialPressurePSI, 0.00f, LogLPInitialPressurePSI); // Clamp so that LP Initial pressure does not go negative

                    // HP Cylinder

                    Pressure_a_AtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                    LogInitialPressurePSI = Pressure_a_AtmPSI - OneAtmospherePSI; // Value for log file & display
                    LogInitialPressurePSI = MathHelper.Clamp(LogInitialPressurePSI, 0.00f, LogInitialPressurePSI); // Clamp so that initial pressure does not go negative

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // calculate value based upon setting of Cylinder port opening - as steam goes into both in parallel - both will suffer similar condensation

                    CutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;
                    LPCutoffPressureDropRatio = CutoffPressureDropRatio;

                    if (HasSuperheater) // If locomotive is superheated then cutoff pressure drop will be different.
                    {
                        float DrvWheelRevpM = pS.TopM(DrvWheelRevRpS);
                        LPCutoffPressureDropRatio = (1.0f - ((1 / SuperheatCutoffPressureFactor) * (float)Math.Sqrt(pS.TopM(DrvWheelRevRpS))));
                        CutoffPressureDropRatio = (1.0f - ((1 / SuperheatCutoffPressureFactor) * (float)Math.Sqrt(pS.TopM(DrvWheelRevRpS))));
                    }

                    // (b) - Cutoff Pressure (For LP equates to point h)
                    // LP Cylinder
                    LPPressure_b_AtmPSI = LPPressure_a_AtmPSI * LPCutoffPressureDropRatio;

                    LogLPCutoffPressurePSI = LPPressure_b_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPCutoffPressurePSI = MathHelper.Clamp(LogLPCutoffPressurePSI, 0.00f, LogLPCutoffPressurePSI); // Clamp so that LP Cutoff pressure does not go negative

                    // HP Cylinder
                    Pressure_b_AtmPSI = Pressure_a_AtmPSI * CutoffPressureDropRatio;

                    LogCutoffPressurePSI = Pressure_b_AtmPSI - OneAtmospherePSI;   // Value for log file
                    LogCutoffPressurePSI = MathHelper.Clamp(LogCutoffPressurePSI, 0.00f, LogCutoffPressurePSI); // Clamp so that Cutoff pressure does not go negative

                    // (c) - Release Pressure (For LP equates to point l)
                    // LP Cylinder
                    float LPVolumeRatioRelease = LPCylinderVolumePoint_h_LPpost / LPCylinderVolumePoint_l;
                    LPPressure_c_AtmPSI = LPPressure_b_AtmPSI * LPVolumeRatioRelease;

                    LogLPReleasePressurePSI = LPPressure_c_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPReleasePressurePSI = MathHelper.Clamp(LogLPReleasePressurePSI, 0.00f, LogLPReleasePressurePSI); // Clamp so that LP Release pressure does not go negative

                    // HP Cylinder
                    Pressure_c_AtmPSI = (Pressure_b_AtmPSI) * (cutoff + CylinderClearancePC) / (CylinderExhaustOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust

                    LogReleasePressurePSI = Pressure_c_AtmPSI - OneAtmospherePSI;   // Value for log file
                    LogReleasePressurePSI = MathHelper.Clamp(LogReleasePressurePSI, 0.00f, LogReleasePressurePSI); // Clamp so that Release pressure does not go negative

                    // (d) - Exhaust (Back) Pressure (For LP equates to point m)
                    // LP Cylinder
                    // Cylinder back pressure will be decreased depending upon locomotive speed
                    LPPressure_d_AtmPSI = BackPressureIHPtoPSI[IndicatedHorsePowerHP] + OneAtmospherePSI;

                    LogLPBackPressurePSI = LPPressure_d_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPBackPressurePSI = MathHelper.Clamp(LogLPBackPressurePSI, 0.00f, LogLPBackPressurePSI); // Clamp so that LP Back pressure does not go negative

                    // HP Cylinder

                    Pressure_d_AtmPSI = BackPressureIHPtoPSI[IndicatedHorsePowerHP] + OneAtmospherePSI;

                    LogBackPressurePSI = Pressure_d_AtmPSI - OneAtmospherePSI;  // Value for log file
                    LogBackPressurePSI = MathHelper.Clamp(LogBackPressurePSI, 0.00f, LogBackPressurePSI); // Clamp so that Back pressure does not go negative

                    // (e) - Compression Pressure (For LP equates to point n)
                    // LP Cylinder
                    LPPressure_e_AtmPSI = LPPressure_d_AtmPSI;

                    // HP Cylinder
                    Pressure_e_AtmPSI = Pressure_d_AtmPSI;

                    // (f) - Compression Pressure (For LP equates to point q)
                    // LP Cylinder
                    float LPVolumeRatioCompression = LPCylinderVolumePoint_q / LPCylinderVolumePoint_n;
                    LPPressure_f_AtmPSI = LPPressure_e_AtmPSI * LPVolumeRatioCompression;

                    // HP Cylinder
                    Pressure_f_AtmPSI = Pressure_e_AtmPSI * (CylinderCompressionCloseFactor + CylinderClearancePC) / (CylinderAdmissionOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of 

                    LogPreAdmissionPressurePSI = Pressure_f_AtmPSI - OneAtmospherePSI;   // Value for log file
                    LogPreAdmissionPressurePSI = MathHelper.Clamp(LogPreAdmissionPressurePSI, 0.00f, LogPreAdmissionPressurePSI); // Clamp so that pre admission pressure does not go negative

                    // ***** Calculate MEP for LP Cylinder *****

                    // Calculate work between a) - b) - Admission (LP Cylinder g - h)              
                    // Calculate Mean Pressure
                    float LPMeanPressure_ab_AtmPSI = (LPPressure_a_AtmPSI + LPPressure_b_AtmPSI) / 2.0f;
                    // Calculate volume between a-b
                    float LPCylinderLength_ab_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_h_LPpost - LPCylinderVolumePoint_a);
                    // Calculate work a-b
                    float LPCylinderWork_ab_InLbs = LPMeanPressure_ab_AtmPSI * LPCylinderLength_ab_In;

                    //Calculate work between b) - c) - Cutoff  (LP Cylinder h - l)
                    // Calculate Mean Pressure
                    float LPExpansionRatio_bc = 1.0f / LPVolumeRatioRelease;
                    float LPMeanPressure_bc_AtmPSI = LPPressure_b_AtmPSI * ((float)Math.Log(LPExpansionRatio_bc) / (LPExpansionRatio_bc - 1.0f));
                    // Calculate volume between b-c                    
                    float LPCylinderLength_bc_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_l - LPCylinderVolumePoint_h_LPpost);
                    // Calculate work b-c
                    float LPCylinderWork_bc_InLbs = LPMeanPressure_bc_AtmPSI * LPCylinderLength_bc_In;

                    //Calculate work between c) - d) - Release  (LP Cylinder l - m)
                    // Mean pressure & work between c) - d) - Cutoff Expansion  (LP Cylinder l - m)
                    // Calculate Mean Pressure
                    float LPMeanPressure_cd_AtmPSI = (LPPressure_c_AtmPSI + LPPressure_d_AtmPSI) / 2.0f;
                    // Calculate volume between c-d
                    float LPCylinderLength_cd_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_l);
                    // Calculate work between c-d
                    float LPCylinderWork_cd_InLbs = LPMeanPressure_cd_AtmPSI * LPCylinderLength_cd_In;

                    //Calculate work between d) - e) - Exhaust  (LP Cylinder m - n)
                    // Calculate Mean Pressure
                    float LPMeanPressure_de_AtmPSI = (LPPressure_e_AtmPSI + LPPressure_d_AtmPSI) / 2.0f; // Average pressure
                    // Calculate volume between d-e
                    float LPCylinderLength_de_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_n);
                    // Calculate work between d-e
                    float LPCylinderWork_de_InLbs = LPPressure_d_AtmPSI * LPCylinderLength_de_In;

                    //Calculate work between e) - f) - Compression  (LP Cylinder n - q )
                    // Calculate Mean Pressure
                    float LPCompressionRatio_ef = (LPCylinderVolumePoint_n) / LPCylinderVolumePoint_q;
                    float LPMeanPressure_ef_AtmPSI = LPPressure_d_AtmPSI * LPCompressionRatio_ef * ((float)Math.Log(LPCompressionRatio_ef) / (LPCompressionRatio_ef - 1.0f));
                    // Calculate volume between e-f
                    float LPCylinderLength_ef_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_n - LPCylinderVolumePoint_q);
                    // Calculate work between e-f
                    float LPCylinderWork_ef_InLbs = LPMeanPressure_ef_AtmPSI * LPCylinderLength_ef_In;

                    //Calculate work between f) - a) - Compression  (LP Cylinder q - g )
                    // Calculate Mean Pressure
                    float LPMeanPressure_af_AtmPSI = (LPPressure_a_AtmPSI + LPPressure_f_AtmPSI) / 2.0f;
                    // Calculate volume between f-a
                    float LPCylinderLength_fa_In = Me.ToIn(LPCylinderStrokeM) * (LPCylinderVolumePoint_q - LPCylinderVolumePoint_a);
                    // Calculate work between f-a
                    float LPCylinderWork_fa_InLbs = LPMeanPressure_af_AtmPSI * LPCylinderLength_fa_In;

                    // Calculate total Work in LP Cylinder
                    float TotalLPCylinderWorksInLbs = LPCylinderWork_ab_InLbs + LPCylinderWork_bc_InLbs + LPCylinderWork_cd_InLbs - LPCylinderWork_de_InLbs - LPCylinderWork_ef_InLbs - LPCylinderWork_fa_InLbs;

                    LPCylinderMEPPSI = TotalLPCylinderWorksInLbs / Me.ToIn(LPCylinderStrokeM);
                    LPCylinderMEPPSI = MathHelper.Clamp(LPCylinderMEPPSI, 0.00f, LPCylinderMEPPSI); // Clamp MEP so that LP MEP does not go negative

                    // ***** Calculate MEP for HP Cylinder *****

                    // Calculate Av Admission Work (inch pounds) between a) - b)
                    // Av Admission work = Av (Initial Pressure + Cutoff Pressure) * length of Cylinder to cutoff
                    // Mean Pressure
                    float MeanPressure_ab_AtmPSI = ((Pressure_a_AtmPSI + Pressure_b_AtmPSI) / 2.0f);
                    // Calculate volume between a -b
                    float CylinderLength_ab_In = Me.ToIn(CylinderStrokeM * ((cutoff + CylinderClearancePC) - CylinderClearancePC));
                    // Calculate work - a-b
                    CylinderWork_ab_InLbs = MeanPressure_ab_AtmPSI * CylinderLength_ab_In;

                    // Calculate Av Expansion Work (inch pounds) - between b) - c)
                    // Av pressure during expansion = Cutoff pressure x log (ratio of expansion) / (ratio of expansion - 1.0) 
                    // Av Expansion work = Av pressure during expansion * length of Cylinder during expansion
                    // Mean Pressure
                    float RatioOfExpansion_bc = HPCylinderVolumePoint_d / HPCylinderVolumePoint_b;
                    float MeanPressure_bc_AtmPSI = Pressure_b_AtmPSI * ((float)Math.Log(RatioOfExpansion_bc) / (RatioOfExpansion_bc - 1.0f));
                    // Calculate volume between b-c
                    float CylinderLength_bc_In = Me.ToIn(CylinderStrokeM) * ((CylinderExhaustOpenFactor + CylinderClearancePC) - (cutoff + CylinderClearancePC));
                    // Calculate work - b-c
                    CylinderWork_bc_InLbs = MeanPressure_bc_AtmPSI * CylinderLength_bc_In;

                    // Calculate Av Release work (inch pounds) - between c) - d)
                    // Av Release work = Av pressure during release * length of Cylinder during release
                    // Mean Pressure
                    float MeanPressure_cd_AtmPSI = ((Pressure_c_AtmPSI + Pressure_d_AtmPSI) / 2.0f);
                    // Calculate volume between c-d
                    float CylinderLength_cd_In = Me.ToIn(CylinderStrokeM) * ((1.0f + CylinderClearancePC) - (CylinderExhaustOpenFactor + CylinderClearancePC)); // Full cylinder length is 1.0
                    // Calculate work - c-d             
                    CylinderWork_cd_InLbs = MeanPressure_cd_AtmPSI * CylinderLength_cd_In;

                    // Calculate Av Exhaust Work (inch pounds) - between d) - e)
                    // Av Exhaust work = Av pressure during exhaust * length of Cylinder during exhaust stroke
                    // Mean Pressure
                    float MeanPressure_de_AtmPSI = ((Pressure_d_AtmPSI + Pressure_e_AtmPSI) / 2.0f);
                    // Calculate volume between d-e
                    float CylinderLength_de_In = Me.ToIn(CylinderStrokeM) * ((HPCylinderVolumeFactor + HPCylinderClearancePC) - HPCylinderVolumePoint_k_post);
                    // Calculate work - d-e
                    CylinderWork_de_InLbs = MeanPressure_de_AtmPSI * CylinderLength_de_In;

                    // Calculate Av Compression Work (inch pounds) - between e) - f)
                    // Ratio of compression = stroke during compression = stroke @ start of compression / stroke and end of compression
                    // Av compression pressure = PreCompression Pressure x Ratio of Compression x log (Ratio of Compression) / (Ratio of Compression - 1.0)
                    // Av Exhaust work = Av pressure during compression * length of Cylinder during compression stroke
                    // Mean Pressure
                    float RatioOfCompression_ef = (HPCylinderVolumePoint_k_post) / (HPCylinderVolumePoint_u);
                    float MeanPressure_ef_AtmPSI = Pressure_e_AtmPSI * RatioOfCompression_ef * ((float)Math.Log(RatioOfCompression_ef) / (RatioOfCompression_ef - 1.0f));
                    // Calculate volume between e-f
                    float CylinderLength_ef_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_k_post - HPCylinderVolumePoint_u);
                    // Calculate work - e-f
                    CylinderWork_ef_InLbs = MeanPressure_ef_AtmPSI * CylinderLength_ef_In;

                    // Calculate Av Pre-admission work (inch pounds) - between f) - a)
                    // Av Pre-admission work = Av pressure during pre-admission * length of Cylinder during pre-admission stroke
                    // Mean Pressure
                    float MeanPressure_fa_AtmPSI = ((Pressure_a_AtmPSI + Pressure_f_AtmPSI) / 2.0f);
                    // Calculate volume between f-a
                    float CylinderLength_fa_In = CylinderAdmissionOpenFactor * Me.ToIn(CylinderStrokeM);
                    // Calculate work - f-a
                    CylinderWork_fa_InLbs = MeanPressure_fa_AtmPSI * CylinderLength_fa_In;

                    // Calculate total work in cylinder
                    float TotalHPCylinderWorkInLbs = CylinderWork_ab_InLbs + CylinderWork_bc_InLbs + CylinderWork_cd_InLbs - CylinderWork_de_InLbs - CylinderWork_ef_InLbs - CylinderWork_fa_InLbs;

                    HPCylinderMEPPSI = TotalHPCylinderWorkInLbs / Me.ToIn(CylinderStrokeM);
                    HPCylinderMEPPSI = MathHelper.Clamp(HPCylinderMEPPSI, 0.00f, HPCylinderMEPPSI); // Clamp MEP so that LP MEP does not go negative

                    MeanEffectivePressurePSI = HPCylinderMEPPSI + LPCylinderMEPPSI; // Calculate Total MEP

#if DEBUG_LOCO_STEAM_COMPOUND_LP_MEP
                    if (DebugWheelRevs >= 40.0 && DebugWheelRevs < 40.05 | DebugWheelRevs >= 80.0 && DebugWheelRevs < 80.05 | DebugWheelRevs >= 160.0 && DebugWheelRevs < 160.05 | DebugWheelRevs >= 240.0 && DebugWheelRevs < 240.05 | DebugWheelRevs >= 320.0 && DebugWheelRevs < 320.05)
                    {
                        Trace.TraceInformation("***************************************** Compound Steam Locomotive ***************************************************************");

                        Trace.TraceInformation("*********** Single Expansion *********");

                        Trace.TraceInformation("*********** Operating Conditions *********");

                        Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2:N1} RecVol {3} CylRatio {4} HPClear {5} LPClear {6}", throttle, cutoff, pS.TopM(DrvWheelRevRpS), CompoundRecieverVolumePCHP, CompoundCylinderRatio, HPCylinderClearancePC, LPCylinderClearancePC);

                        Trace.TraceInformation("Cylinder Events - Cutoff {0:N3} Release {1:N3} Compression {2:N3} Admission {3:N3}", cutoff, CylinderExhaustOpenFactor, CylinderCompressionCloseFactor, CylinderAdmissionOpenFactor);

                        Trace.TraceInformation("*********** LP Cylinder *********");

                        Trace.TraceInformation("LP Cylinder Press: a {0} b {1}  c {2} d {3} e {4} f {5}", LPPressure_a_AtmPSI, LPPressure_b_AtmPSI, LPPressure_c_AtmPSI, LPPressure_d_AtmPSI, LPPressure_e_AtmPSI, LPPressure_f_AtmPSI);

                        Trace.TraceInformation("Press: b: b {0} h_pre {1} g {2} InitPress {3}", LPPressure_b_AtmPSI, LPCylinderVolumePoint_h_pre, LPCylinderVolumePoint_g, LPPressure_a_AtmPSI);

                        Trace.TraceInformation("Work Input: a - b: LPMeanBack {0} Cyl Len {1} h_postLP {2} a {3} ", LPMeanPressure_ab_AtmPSI, LPCylinderLength_ab_In, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("MeanPressure b-c: LPMeanPressRelease {0} ExpRatio {1} h_LPpost {2} l {3} PreCutoffPress {4}", LPMeanPressure_bc_AtmPSI, LPExpansionRatio_bc, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_l, LPPressure_b_AtmPSI);

                        Trace.TraceInformation("Work Input: b - c: LPMeanPressureRelease {0} Cyl Len {1} l {2} h_postLP {3} ", LPMeanPressure_bc_AtmPSI, LPCylinderLength_bc_In, LPCylinderVolumePoint_l, LPCylinderVolumePoint_h_LPpost);

                        Trace.TraceInformation("Work Input: c - d: LPMeanPressureExhaust {0} Cyl Len {1} m {2} l {3} ", LPMeanPressure_cd_AtmPSI, LPCylinderLength_cd_In, LPCylinderVolumePoint_m, LPCylinderVolumePoint_l);

                        Trace.TraceInformation("Work Input: d - e: LPMeanPressureBack {0} Cyl Len {1} m {2} n {3} ", LPPressure_d_AtmPSI, LPCylinderLength_de_In, LPCylinderVolumePoint_m, LPCylinderVolumePoint_n);

                        Trace.TraceInformation("MeanPressure e - f: LPMeanPressPreComp {0} CompRatio {1} n {2} b {3} PreCompPress {4}", LPMeanPressure_ef_AtmPSI, LPCompressionRatio_ef, ((LPCylinderVolumeFactor - CylinderExhaustOpenFactor) + LPCylinderClearancePC), LPCylinderClearancePC, LPPressure_d_AtmPSI);

                        Trace.TraceInformation("Work Input: e - f: LPMeanPressurePreComp {0} Cyl Len {1} n {2} a {3} ", LPMeanPressure_ef_AtmPSI, LPCylinderLength_ef_In, LPCylinderVolumePoint_n, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("Work Input: f - a: LPMeanPressurePreAdm {0} Cyl Len {1} a {2}", LPMeanPressure_af_AtmPSI, LPCylinderLength_fa_In, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("LP Works: Total {0} === a-b {1} b-c {2} c-d {3} d-e {4} e-f {5} f-a {6}", TotalLPCylinderWorksInLbs, LPCylinderWork_ab_InLbs, LPCylinderWork_bc_InLbs, LPCylinderWork_cd_InLbs, LPCylinderWork_de_InLbs, LPCylinderWork_ef_InLbs, LPCylinderWork_fa_InLbs);

                        Trace.TraceInformation("*********** HP Cylinder *********");

                        Trace.TraceInformation("Cylinder Pressures: a {0} b {1} c {2} d {3} e {4} f {5}", Pressure_a_AtmPSI, Pressure_b_AtmPSI, Pressure_c_AtmPSI, Pressure_d_AtmPSI, Pressure_e_AtmPSI, Pressure_f_AtmPSI);

                        Trace.TraceInformation("MeanPressure Expansion (b-c): MeanPressure b-c {0} ExpRatio {1} cutoff {2} Release Event {3}", MeanPressure_bc_AtmPSI, RatioOfExpansion_bc, cutoff, CylinderExhaustOpenFactor);

                        Trace.TraceInformation("MeanPressure Compression (e-f): MeanPressure e-f {0} CompRatio {1} Vol_e {2} Vol_f {3}", MeanPressure_ef_AtmPSI, RatioOfCompression_ef, HPCylinderVolumePoint_k_post, HPCylinderVolumePoint_u);

                        Trace.TraceInformation("Cylinder Works: Total {0} === a-b {1} b-c {2} c-d {3} d-e {4} e-f {5} f-a {6}", TotalHPCylinderWorkInLbs, CylinderWork_ab_InLbs, CylinderWork_bc_InLbs, CylinderWork_cd_InLbs, CylinderWork_de_InLbs, CylinderWork_ef_InLbs, CylinderWork_fa_InLbs);

                        Trace.TraceInformation("*********** MEP *********");

                        Trace.TraceInformation("MEP: HP {0}  LP {1}", HPCylinderMEPPSI, LPCylinderMEPPSI);
                    }
#endif
                }
                else
                {

                    // ***** Compound mode *****

                    // For calculation of the steam indicator cycle, three values are calculated as follows:
                    // a) Pressure at various points around on the cycle - these values are calculated using volumes including high, low cylinders and interconnecting passages
                    // b) Mean pressures for various sections on the steam indicator cycle
                    // c) Work for various sections of the indicator cyle - volume values in this instance will only be the relevant cylinder values, as this is the only place that work is done in.
                    // Note all pressures in absolute pressure for working on steam indicator diagram
                    // The pressures below are as calculated and referenced to the steam indicator diagram for Compound Locomototives by letters shown in brackets - without receivers - see Coals to Newcastle website - physics section
                    // Two process are followed her, firstly all the pressures are calculated using the full volume ratios of the HP & LP cylinder, as well as an allowance for the connecting passages between the cylinders. 
                    // The second process calculates the work done in the cylinders, and in this case only the volumes of either the HP or LP cylinder are used.

                    SteamChestPressurePSI = (throttle * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

                    LogSteamChestPressurePSI = SteamChestPressurePSI;  // Value for recording in log file
                    LogSteamChestPressurePSI = MathHelper.Clamp(LogSteamChestPressurePSI, 0.00f, LogSteamChestPressurePSI); // Clamp so that steam chest pressure does not go negative

                    // (a) - Initial pressure
                    // Initial pressure will be decreased depending upon locomotive speed
                    HPCompPressure_a_AtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                    LogInitialPressurePSI = HPCompPressure_a_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogInitialPressurePSI = MathHelper.Clamp(LogInitialPressurePSI, 0.00f, LogInitialPressurePSI); // Clamp so that HP Initial pressure does not go negative

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // calculate value based upon setting of Cylinder port opening

                    CutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;

                    if (HasSuperheater) // If locomotive is superheated then cutoff pressure drop will be different.
                    {
                        float DrvWheelRevpM = pS.TopM(DrvWheelRevRpS);
                        CutoffPressureDropRatio = (1.0f - ((1 / SuperheatCutoffPressureFactor) * (float)Math.Sqrt(pS.TopM(DrvWheelRevRpS))));
                    }

                    // (b) - Cutoff pressure
                    // Cutoff pressure also drops with locomotive speed
                    HPCompPressure_b_AtmPSI = HPCompPressure_a_AtmPSI * CutoffPressureDropRatio;

                    LogCutoffPressurePSI = HPCompPressure_b_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogCutoffPressurePSI = MathHelper.Clamp(LogCutoffPressurePSI, 0.00f, LogCutoffPressurePSI); // Clamp so that HP Cutoff pressure does not go negative

                    // (d) - Release pressure
                    // Release pressure - occurs when the exhaust valve opens to release steam from the cylinder
                    float HPCompVolumeRatio_bd = HPCylinderVolumePoint_b / HPCylinderVolumePoint_d;
                    HPCompPressure_d_AtmPSI = HPCompPressure_b_AtmPSI * HPCompVolumeRatio_bd;  // Check factor to calculate volume of cylinder for new volume at exhaust

                    LogReleasePressurePSI = HPCompPressure_d_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogReleasePressurePSI = MathHelper.Clamp(LogReleasePressurePSI, 0.00f, LogReleasePressurePSI); // Clamp so that HP Release pressure does not go negative


                    // (e) - Release pressure (with Steam passages in circuit
                    // Release pressure (with no receiver) is the pressure after the first steam expansion, and occurs as steam moves into the passageways between the HP and LP cylinder
                    float HPCompVolumeRatio_de = (HPCylinderVolumePoint_d / HPCylinderVolumePoint_e);
                    // HPCylinderReleasePressureRecvAtmPSI = HPCylinderReleasePressureAtmPSI * HPExpansionRatioReceiver;
                    HPCompPressure_e_AtmPSI = HPCompPressure_d_AtmPSI - 5.0f; // assume this relationship


                    // (f) - HP Exhaust pressure
                    // Exhaust pressure is the pressure after the second steam expansion, and occurs as all the steam is exhausted from the HP cylinder
                    float HPCompVolumeRatio_ef = (HPCylinderVolumePoint_e / HPCylinderVolumePoint_f);
                    HPCompPressure_f_AtmPSI = HPCompPressure_e_AtmPSI * HPCompVolumeRatio_ef;

                    // LP cylinder initial pressure (g) will be mixture of the volume at exahust for HP cylinder and the volume of the LP clearance at the LP cylinder pre-admission pressure
                    // Pg = (Pq x Vq + Pf x Vf) / (Vq = Vf)
                    // To calculate we need to calculate the LP Pre-Admission pressure @ q first
                    // LP Cylinder pre-admission pressure is the pressure after the second steam expansion, and occurs as the steam valves close in the LP Cylinder
                    // LP Cylinder compression pressure will be equal to back pressure - assume flat line.

                    // (m) - LP exhaust pressure  
                    // LP Cylinder back pressure will be increased depending upon locomotive speed
                    float LPCompPressure_m_AtmPSI = BackPressureIHPtoPSI[IndicatedHorsePowerHP] + OneAtmospherePSI;

                    LogLPBackPressurePSI = LPCompPressure_m_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPBackPressurePSI = MathHelper.Clamp(LogLPBackPressurePSI, 0.00f, LogLPBackPressurePSI); // Clamp so that LP Back pressure does not go negative

                    // (n) - LP Compression Pressure 
                    float LPCompPressure_n_AtmPSI = LPCompPressure_m_AtmPSI;

                    // (q) - LP Admission close 
                    float LPCompVolumeRatio_nq = LPCylinderVolumePoint_n / LPCylinderVolumePoint_q;
                    float LPCompPressure_q_AtmPSI = LPCompPressure_n_AtmPSI * LPCompVolumeRatio_nq;

                    // (g) - LP Initial Pressure  
                    float LPCompPressure_g_AtmPSI = ((LPPressure_f_AtmPSI * (LPCylinderVolumePoint_q * CompoundCylinderRatio)) + (HPCompPressure_f_AtmPSI * HPCylinderVolumePoint_f)) / ((LPCylinderVolumePoint_q * CompoundCylinderRatio) + HPCylinderVolumePoint_f);

                    LogLPInitialPressurePSI = LPCompPressure_g_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPInitialPressurePSI = MathHelper.Clamp(LogLPInitialPressurePSI, 0.00f, LogLPInitialPressurePSI); // Clamp so that LP Initial pressure does not go negative

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float LPCutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float LPCutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // (h) - pre cutoff in HP & LP Cylinder
                    // LP cylinder cutoff pressure - before LP Cylinder Cutoff - in this instance both the HP and LP are still interconnected
                    float LPCompVolumeRatio_ghpre = LPCylinderVolumePoint_g / LPCylinderVolumePoint_h_pre;

                    // calculate pressure drop value based upon setting of Cylinder port opening (allows for condensaton into cylinder
                    LPCutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (LPCutoffDropUpper - LPCutoffDropLower)) + LPCutoffDropLower;

                    float LPCompPressure_h_AtmPSI = LPCompPressure_g_AtmPSI * LPCutoffPressureDropRatio; // allow for wire drawing into LP cylinder

                    LogLPCutoffPressurePSI = LPCompPressure_h_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPCutoffPressurePSI = MathHelper.Clamp(LogLPCutoffPressurePSI, 0.00f, LogLPCutoffPressurePSI); // Clamp so that LP Cutoff pressure does not go negative

                    // (h) - In HP Cylinder post cutoff in LP Cylinder
                    // Pressure will have equalised in HP & LP Cylinder, so will be the same as the pressure pre-cutoff
                    HPCompPressure_h_AtmPSI = LPCompPressure_h_AtmPSI;

                    // (l) - Release pressure
                    // LP cylinder release pressure
                    float LPCompVolumeRatio_hl = LPCylinderVolumePoint_h_LPpost / LPCylinderVolumePoint_l;
                    float LPCompPressure_l_AtmPSI = LPCompPressure_h_AtmPSI * LPCompVolumeRatio_hl;

                    LogLPReleasePressurePSI = LPCompPressure_l_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogLPReleasePressurePSI = MathHelper.Clamp(LogLPReleasePressurePSI, 0.00f, LogLPReleasePressurePSI); // Clamp so that LP Release pressure does not go negative

                    // (k) - HP Cylinder Compression Pressure - before the valve closes 
                    // HP cylinder compression pressure
                    float HPCompVolumeRatio_hk = HPCylinderVolumePoint_h_HPpost / HPCylinderVolumePoint_k_pre;
                    HPCompPressure_k_AtmPSI = HPCompPressure_h_AtmPSI * HPCompVolumeRatio_hk;

                    // (u) - HP Cylinder before the admission valve closes 
                    // HP cylinder admission pressure
                    //  float HPCompVolumeRatio_ku = HPCylinderVolumePoint_k_pre / HPCylinderVolumePoint_u;
                    float HPCompVolumeRatio_ku = HPCylinderVolumePoint_k_post / HPCylinderVolumePoint_u;
                    HPCompPressure_u_AtmPSI = HPCompPressure_k_AtmPSI * HPCompVolumeRatio_ku;
                    HPCompPressure_u_AtmPSI = MathHelper.Clamp(HPCompPressure_u_AtmPSI, 0.00f, MaxBoilerPressurePSI + 50.0f); // pressure does not go excessively positive

                    // ***** Calculate mean pressures and work done in HP cylinder *****

                    // Mean pressure between a) - b) - Admission
                    float HPCompMeanPressure_ab_AtmPSI = (HPCompPressure_a_AtmPSI + HPCompPressure_b_AtmPSI) / 2.0f;  // Average pressure between initial and cutoff pressure
                    // Calculate positive work between a) - b)
                    float HPCompCylinderLength_ab_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_b - HPCylinderVolumePoint_a);
                    float HPCompWork_ab_InLbs = HPCompMeanPressure_ab_AtmPSI * HPCompCylinderLength_ab_In;

                    // Mean pressure between b) - d) - Cutoff Expansion - is after the cutoff and the first steam expansion, 
                    float HPCompExpansionRatio_bd = 1.0f / HPCompVolumeRatio_bd; // Invert volume ratio to find Expansion ratio
                    float HPCompMeanPressure_bd_AtmPSI = HPCompPressure_b_AtmPSI * ((float)Math.Log(HPCompExpansionRatio_bd) / (HPCompExpansionRatio_bd - 1.0f));
                    // Calculate positive work between b) - d)
                    float HPCompCylinderLength_bd_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_d - HPCylinderVolumePoint_b);
                    float HPCompWork_bd_InLbs = HPCompMeanPressure_bd_AtmPSI * HPCompCylinderLength_bd_In;

                    // Mean pressure e) - f) - Release Expansion
                    float HPCompExpansionRatio_ef = 1.0f / HPCompVolumeRatio_ef; // Invert volume ratio to find Expansion ratio
                    float HPCompMeanPressure_ef_AtmPSI = HPCompPressure_e_AtmPSI * ((float)Math.Log(HPCompExpansionRatio_ef) / (HPCompExpansionRatio_ef - 1.0f));
                    // Calculate positive work between e) - f) - Release Expansion
                    float HPCompCylinderLength_ef_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_fHPonly - HPCylinderVolumePoint_d);
                    float HPCompWork_ef_InLbs = HPCompMeanPressure_ef_AtmPSI * HPCompCylinderLength_ef_In;

                    // Find negative pressures in HP Cylinder

                    // Mean pressure g) - h) - This curve is the admission curve for the LP and the Backpressure curve for the HP - it is an expansion curve 
                    // (This section needs to be here because of the next calculation HPCylinderBackPressureAtmPSI)
                    float LPCompExpansionRatio_gh = 1.0f / LPCompVolumeRatio_ghpre;  // Invert volume ratio to find Expansion ratio
                                                                                     //        float LPCompMeanPressure_gh_AtmPSI = LPCompPressure_g_AtmPSI * ((float)Math.Log(LPCompExpansionRatio_gh) / (LPCompExpansionRatio_gh - 1.0f));
                    float LPCompMeanPressure_gh_AtmPSI = (LPCompPressure_g_AtmPSI + LPCompPressure_h_AtmPSI) / 2.0f;
                    // Find negative pressures for HP Cylinder - upper half of g) - h) curve
                    HPCompMeanPressure_gh_AtmPSI = LPCompMeanPressure_gh_AtmPSI; // Mean HP Back pressure is the same as the mean admission pressure for the LP cylinder

                    LogBackPressurePSI = HPCompMeanPressure_gh_AtmPSI - OneAtmospherePSI;  // Value for recording in log file
                    LogBackPressurePSI = MathHelper.Clamp(LogBackPressurePSI, 0.00f, LogBackPressurePSI); // Clamp so that HP Release pressure does not go negative

                    // Calculate negative work between g) - h) - HP Cylinder only
                    float HPCompCylinderLength_gh_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_b - HPCylinderVolumePoint_a); // Calculate negative work in HP cylinder only due to back pressure, ie backpressure in HP Only x volume of cylinder between commencement of LP stroke and cutoff
                    float HPCompWorks_gh_InLbs = HPCompMeanPressure_gh_AtmPSI * HPCompCylinderLength_gh_In;

                    // Mean pressure between h) - k) - Compression Expansion
                    // Ratio of compression = stroke during compression = stroke @ start of compression / stroke and end of compression
                    float HPCompCompressionRatio_hk = HPCylinderVolumePoint_h_HPpost / HPCylinderVolumePoint_k_pre;
                    float HPCompMeanPressure_hk_AtmPSI = HPCompPressure_h_AtmPSI * HPCompCompressionRatio_hk * ((float)Math.Log(HPCompCompressionRatio_hk) / (HPCompCompressionRatio_hk - 1.0f));
                    // Calculate negative work between h) - k) - HP Cylinder only
                    float HPCompCylinderLength_hk_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_d - HPCylinderVolumePoint_b); // This volume is equivalent to the volume from LP cutoff to release
                    float HPCompWork_hk_InLbs = HPCompMeanPressure_hk_AtmPSI * HPCompCylinderLength_hk_In;

                    // Mean pressure & work between k) - u) - Compression #2 Expansion
                    float HPCompMeanPressure_ku_AtmPSI = (HPCompPressure_u_AtmPSI + HPCompPressure_k_AtmPSI) / 2.0f;
                    // Calculate negative work between k) - u) - HP Cylinder only
                    float HPCompCylinderLength_ku_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_k_post - HPCylinderVolumePoint_u);
                    float HPCompWork_ku_InLbs = HPCompMeanPressure_ku_AtmPSI * HPCompCylinderLength_ku_In;

                    // Mean pressure & work between u) - a) - Admission Expansion
                    float HPCompMeanPressure_ua_AtmPSI = (HPCompPressure_a_AtmPSI + HPCompPressure_k_AtmPSI) / 2.0f;
                    // Calculate negative work between u) - a) - HP Cylinder only
                    float HPCompCylinderLength_ua_In = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_u - HPCylinderVolumePoint_a);
                    float HPCompWork_ua_InLbs = HPCompMeanPressure_ku_AtmPSI * HPCompCylinderLength_ua_In;

                    // Calculate total Work in HP Cylinder
                    float TotalHPCylinderWorksInLbs = HPCompWork_ab_InLbs + HPCompWork_bd_InLbs + HPCompWork_ef_InLbs - HPCompWorks_gh_InLbs - HPCompWork_hk_InLbs - HPCompWork_ku_InLbs - HPCompWork_ua_InLbs;

                    // ***** Calculate pressures and work done in LP cylinder *****

                    // Calculate work between g) - h) - Release Expansion  (LP Cylinder)              
                    float LPCompCylinderLength_gh_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_h_LPpost - LPCylinderClearancePC);
                    // For purposes of work done only consider the change in LP cylinder between stroke commencement and cutoff
                    float LPCompWork_gh_InLbs = LPCompMeanPressure_gh_AtmPSI * LPCompCylinderLength_gh_In;

                    // Calculate MEP for LP Cylinder
                    //Mean pressure & work between h) - l)
                    float LPCompExpansionRatio_hl = 1.0f / LPCompVolumeRatio_hl;
                    float LPCompMeanPressure_hl_AtmPSI = LPCompPressure_h_AtmPSI * ((float)Math.Log(LPCompExpansionRatio_hl) / (LPCompExpansionRatio_hl - 1.0f));
                    // Calculate work between h) - l) - LP Cylinder only
                    float LPCompCylinderLength_hl_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_l - LPCylinderVolumePoint_h_LPpost);
                    float LPCompWork_hl_InLbs = LPCompMeanPressure_hl_AtmPSI * LPCompCylinderLength_hl_In;

                    // Mean pressure & work between l) - m)
                    float LPCompMeanPressure_lm_AtmPSI = (LPCompPressure_l_AtmPSI + LPCompPressure_m_AtmPSI) / 2.0f;
                    // Calculate work between l) - m) - LP Cylinder only
                    float LPCompCylinderLength_lm_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_l);
                    float LPCompWork_lm_InLbs = LPCompMeanPressure_lm_AtmPSI * LPCompCylinderLength_lm_In;

                    // Calculate work between m) - n) - LP Cylinder only
                    // Mean Pressure
                    float LPCompMeanPressure_mn_AtmPSI = (LPCompPressure_m_AtmPSI + LPCompPressure_n_AtmPSI) / 2.0f;
                    float LPCompCylinderLength_mn_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_n);
                    float LPCompWork_mn_InLbs = LPCompMeanPressure_mn_AtmPSI * LPCompCylinderLength_mn_In;

                    // Mean pressure & work between n) - q)
                    float LPCompCompressionRatio_nq = (LPCylinderVolumePoint_n) / LPCylinderVolumePoint_q;
                    float LPCompMeanPressure_nq_AtmPSI = LPCompPressure_n_AtmPSI * LPCompCompressionRatio_nq * ((float)Math.Log(LPCompCompressionRatio_nq) / (LPCompCompressionRatio_nq - 1.0f));
                    // Calculate work between n) - q) - LP Cylinder only
                    float LPCompCylinderLength_nq_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_n - LPCylinderVolumePoint_q);
                    float LPCompWork_nq_InLbs = LPCompMeanPressure_nq_AtmPSI * LPCompCylinderLength_nq_In;

                    // Mean pressure & work between q) - g)            
                    float LPCompMeanPressure_qg_AtmPSI = (LPCompPressure_g_AtmPSI + LPCompPressure_q_AtmPSI) / 2.0f;
                    // Calculate work between q) - g) - LP Cylinder only
                    float LPCompCylinderLength_qg_In = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_q - LPCylinderVolumePoint_a);
                    float LPCompWork_qg_InLbs = LPCompMeanPressure_qg_AtmPSI * LPCompCylinderLength_qg_In;

                    // Calculate total Work in LP Cylinder
                    float TotalLPCylinderWorksInLbs = LPCompWork_gh_InLbs + LPCompWork_hl_InLbs + LPCompWork_lm_InLbs - LPCompWork_mn_InLbs - LPCompWork_nq_InLbs - LPCompWork_qg_InLbs;

                    HPCylinderMEPPSI = TotalHPCylinderWorksInLbs / Me.ToIn(CylinderStrokeM);
                    LPCylinderMEPPSI = TotalLPCylinderWorksInLbs / Me.ToIn(LPCylinderStrokeM);

                    HPCylinderMEPPSI = MathHelper.Clamp(HPCylinderMEPPSI, 0.00f, HPCylinderMEPPSI); // Clamp MEP so that HP MEP does not go negative
                    LPCylinderMEPPSI = MathHelper.Clamp(LPCylinderMEPPSI, 0.00f, LPCylinderMEPPSI); // Clamp MEP so that LP MEP does not go negative

                    if (throttle < 0.02f)
                    {
                        HPCompPressure_a_AtmPSI = 0.0f;  // for sake of display zero pressure values if throttle is closed.
                        HPCompMeanPressure_gh_AtmPSI = 0.0f;
                        HPCompPressure_d_AtmPSI = 0.0f;
                        HPCompPressure_e_AtmPSI = 0.0f;
                        HPCompPressure_f_AtmPSI = 0.0f;
                        HPCompPressure_b_AtmPSI = 0.0f;
                        HPCompPressure_h_AtmPSI = 0.0f;

                        LPCompPressure_g_AtmPSI = 0.0f;
                        LPCompPressure_h_AtmPSI = 0.0f;
                        LPCompPressure_l_AtmPSI = 0.0f;
                        LPCompPressure_n_AtmPSI = 0.0f;

                        HPCylinderMEPPSI = 0.0f;
                        LPCylinderMEPPSI = 0.0f;
                    }


                    // Debug information

#if DEBUG_LOCO_STEAM_COMPOUND_HP_MEP
                    if (DebugWheelRevs >= 40.0 && DebugWheelRevs < 40.05 | DebugWheelRevs >= 80.0 && DebugWheelRevs < 80.05 | DebugWheelRevs >= 160.0 && DebugWheelRevs < 160.05 | DebugWheelRevs >= 240.0 && DebugWheelRevs < 240.05 | DebugWheelRevs >= 320.0 && DebugWheelRevs < 320.05)
                    {
                        Trace.TraceInformation("***************************************** Compound Steam Locomotive ***************************************************************");

                        Trace.TraceInformation("*********** Operating Conditions *********");

                        Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2:N1} RecVol {3} CylRatio {4} HPClear {5} LPClear {6}", throttle, cutoff, pS.TopM(DrvWheelRevRpS), CompoundRecieverVolumePCHP, CompoundCylinderRatio, HPCylinderClearancePC, LPCylinderClearancePC);

                        Trace.TraceInformation("Cylinder Events - Cutoff {0:N3} Release {1:N3} Compression {2:N3} Admission {3:N3}", cutoff, CylinderExhaustOpenFactor, CylinderCompressionCloseFactor, CylinderAdmissionOpenFactor);

                        Trace.TraceInformation("*********** HP Cylinder *********");

                        Trace.TraceInformation("HP Cylinder Press: a {0:N1} b {1:N1} d {2:N1} e {3:N1} f {4:N1} g {5:N1} h {6:N1} k {7:N1}  u {8:N1} Back {9:N1}", HPCompPressure_a_AtmPSI, HPCompPressure_b_AtmPSI, HPCompPressure_d_AtmPSI, HPCompPressure_e_AtmPSI, HPCompPressure_f_AtmPSI, LPCompPressure_g_AtmPSI, HPCompPressure_h_AtmPSI, HPCompPressure_k_AtmPSI, HPCompPressure_u_AtmPSI, HPCompMeanPressure_gh_AtmPSI);

                        Trace.TraceInformation("Work Input: a - b: Mean Pressure {0} Cyl Len {1} b {2} a {3}", HPCompMeanPressure_ab_AtmPSI, HPCompCylinderLength_ab_In, HPCylinderVolumePoint_b, HPCylinderVolumePoint_a);

                        Trace.TraceInformation("Work Input: b - d: Mean Pressure {0} Exp Ratio {1} Cyl Len {2} d {3} b {4} ", HPCompMeanPressure_bd_AtmPSI, HPCompExpansionRatio_bd, HPCompCylinderLength_bd_In, HPCylinderVolumePoint_d, HPCylinderVolumePoint_b);

                        Trace.TraceInformation("Work Input: e - f: Mean Pressure {0} Cyl Len {1} f_HPonly {2} d {3} ", HPCompMeanPressure_ef_AtmPSI, HPCompCylinderLength_ef_In, HPCylinderVolumePoint_fHPonly, HPCylinderVolumePoint_d);

                        Trace.TraceInformation("MeanPressure: g - h: Mean Pressure {0} ExpRatio {1} Cyl Len  {2} h_pre {3} g {4}", HPCompMeanPressure_gh_AtmPSI, LPCompExpansionRatio_gh, HPCompCylinderLength_gh_In, LPCylinderVolumePoint_h_pre, LPCylinderVolumePoint_g);

                        Trace.TraceInformation("Work Input: g - h: Mean Pressure {0} Cyl Len {1} b {2} a {3} ", HPCompMeanPressure_gh_AtmPSI, HPCompCylinderLength_gh_In, HPCylinderVolumePoint_b, HPCylinderVolumePoint_a);

                        Trace.TraceInformation("MeanPressure: h-k: Mean Pressure {0} CompRatio {1} h_HPpost {2} k_pre {3}", HPCompMeanPressure_hk_AtmPSI, HPCompCompressionRatio_hk, HPCylinderVolumePoint_h_HPpost, HPCylinderVolumePoint_k_pre);

                        Trace.TraceInformation("Work Input: h - k: Mean Pressure {0} Cyl Len {1} d {2} b {3} ", HPCompMeanPressure_hk_AtmPSI, HPCompCylinderLength_hk_In, HPCylinderVolumePoint_d, HPCylinderVolumePoint_b);

                        Trace.TraceInformation("Work Input: k - u: Mean Pressure {0} Cyl Len {1} k_post {2} u {3} ", HPCompMeanPressure_ku_AtmPSI, HPCompCylinderLength_ku_In, HPCylinderVolumePoint_k_post, HPCylinderVolumePoint_u);

                        Trace.TraceInformation("Work Input: u - a: Mean Pressure {0} Cyl Len {1} u {2} a {3} ", HPCompMeanPressure_ua_AtmPSI, HPCompCylinderLength_ua_In, HPCylinderVolumePoint_u, HPCylinderClearancePC);

                        Trace.TraceInformation("HP Works: Total {0} === a-b {1} b-d {2} e-f {3} g-h {4} h-k {5} k-u {6} u-a {7}", TotalHPCylinderWorksInLbs, HPCompWork_ab_InLbs, HPCompWork_bd_InLbs, HPCompWork_ef_InLbs, HPCompWorks_gh_InLbs, HPCompWork_hk_InLbs, HPCompWork_ku_InLbs, HPCompWork_ua_InLbs);

                        Trace.TraceInformation("*********** LP Cylinder *********");

                        Trace.TraceInformation("LP Cylinder Press: g {0} h(pre) {1}  l {2} m {3} n {4} q {5}", LPCompPressure_g_AtmPSI, LPCompPressure_h_AtmPSI, LPCompPressure_l_AtmPSI, LPCompPressure_m_AtmPSI, LPCompPressure_n_AtmPSI, LPCompPressure_q_AtmPSI);

                        Trace.TraceInformation("Press: h: PreCutoff {0} h_pre {1} g {2} InitPress {3}", LPCompPressure_h_AtmPSI, LPCylinderVolumePoint_h_pre, LPCylinderVolumePoint_g, LPCompPressure_g_AtmPSI);

                        Trace.TraceInformation("Work Input: g - h: Mean Pressure {0} Cyl Len {1} h_postLP {2} a {3} ", LPCompMeanPressure_gh_AtmPSI, LPCompCylinderLength_gh_In, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("MeanPressure h - l: Mean Pressure {0} ExpRatio {1} h_LPpost {2} l {3}", LPCompMeanPressure_hl_AtmPSI, LPCompExpansionRatio_hl, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_l);

                        Trace.TraceInformation("Work Input: h - l: Mean Pressure {0} Cyl Len {1} l {2} h_postLP {3} ", LPCompMeanPressure_hl_AtmPSI, LPCompCylinderLength_hl_In, LPCylinderVolumePoint_l, LPCylinderVolumePoint_h_LPpost);

                        Trace.TraceInformation("Work Input: l - m: Mean Pressure {0} Cyl Len {1} m {2} l {3} ", LPCompMeanPressure_lm_AtmPSI, LPCompCylinderLength_lm_In, LPCylinderVolumePoint_m, LPCylinderVolumePoint_l);

                        Trace.TraceInformation("Work Input: m - n: Mean Pressure {0} Cyl Len {1} m {2} n {3} ", LPCompMeanPressure_mn_AtmPSI, LPCompCylinderLength_mn_In, LPCylinderVolumePoint_m, LPCylinderVolumePoint_n);

                        Trace.TraceInformation("MeanPressure n - q: Mean Pressure {0} CompRatio {1} n {2} b {3}", LPCompMeanPressure_nq_AtmPSI, LPCompCompressionRatio_nq, ((LPCylinderVolumeFactor - CylinderExhaustOpenFactor) + LPCylinderClearancePC), LPCylinderClearancePC);

                        Trace.TraceInformation("Work Input: n - q: Mean Pressure {0} Cyl Len {1} n {2} a {3} ", LPCompMeanPressure_nq_AtmPSI, LPCompCylinderLength_nq_In, LPCylinderVolumePoint_n, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("Work Input: q - g: Mean Pressure {0} Cyl Len {1} a {2}", LPCompMeanPressure_qg_AtmPSI, LPCompCylinderLength_qg_In, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("LP Works: Total {0} === g-h {1} h-l {2} l-m {3} m-n {4} n-q {5} q-g {6}", TotalLPCylinderWorksInLbs, LPCompWork_gh_InLbs, LPCompWork_hl_InLbs, LPCompWork_lm_InLbs, LPCompWork_mn_InLbs, LPCompWork_nq_InLbs, LPCompWork_qg_InLbs);

                        Trace.TraceInformation("*********** MEP *********");

                        Trace.TraceInformation("MEP: HP {0}  LP {1}", HPCylinderMEPPSI, LPCylinderMEPPSI);
                    }
#endif
                }

                MeanEffectivePressurePSI = HPCylinderMEPPSI + LPCylinderMEPPSI; // Calculate Total MEP
            }

            #endregion


            #region Calculation of Mean Effective Pressure of Cylinder using an Indicator Diagram type approach - Single Expansion

            // Principle source of reference for this section is - LOCOMOTIVE OPERATION - A TECHNICAL AND PRACTICAL ANALYSIS BY G. R. HENDERSON  - pg 128

            if (SteamEngineType != SteamEngineTypes.Compound)
            {

                // Calculate apparent volumes at various points in cylinder
                float CylinderVolumePoint_e = CylinderCompressionCloseFactor + CylinderClearancePC;
                float CylinderVolumePoint_f = CylinderAdmissionOpenFactor + CylinderClearancePC;

                // Note all pressures in absolute pressure for working on steam indicator diagram. MEP will be just a gauge pressure value as it is a differencial pressure calculated as an area off the indicator diagram
                // The pressures below are as calculated and referenced to the steam indicator diagram for single expansion locomotives by letters shown in brackets - see Coals to Newcastle website
                // Calculate Ratio of expansion, with cylinder clearance
                // R (ratio of Expansion) = (length of stroke to point of  exhaust + clearance) / (length of stroke to point of cut-off + clearance)
                // Expressed as a fraction of stroke R = (Exhaust point + c) / (cutoff + c)
                RatioOfExpansion_bc = (CylinderExhaustOpenFactor + CylinderClearancePC) / (cutoff + CylinderClearancePC);
                // Absolute Mean Pressure = Ratio of Expansion

                SteamChestPressurePSI = (throttle * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS * MotiveForceGearRatio)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

                LogSteamChestPressurePSI = SteamChestPressurePSI;  // Value for recording in log file
                LogSteamChestPressurePSI = MathHelper.Clamp(LogSteamChestPressurePSI, 0.00f, LogSteamChestPressurePSI); // Clamp so that steam chest pressure does not go negative

                // Initial pressure will be decreased depending upon locomotive speed
                // This drop can be adjusted with a table in Eng File
                // (a) - Initial Pressure
                Pressure_a_AtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS * MotiveForceGearRatio)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                LogInitialPressurePSI = Pressure_a_AtmPSI - OneAtmospherePSI; // Value for log file & display
                LogInitialPressurePSI = MathHelper.Clamp(LogInitialPressurePSI, 0.00f, LogInitialPressurePSI); // Clamp so that initial pressure does not go negative

                // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS * MotiveForceGearRatio), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS * MotiveForceGearRatio), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                // calculate value based upon setting of Cylinder port opening

                CutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;

                if (HasSuperheater) // If locomotive is superheated then cutoff pressure drop will be different.
                {
                    float DrvWheelRevpM = pS.TopM(DrvWheelRevRpS);
                    CutoffPressureDropRatio = (1.0f - ((1 / SuperheatCutoffPressureFactor) * (float)Math.Sqrt(pS.TopM(DrvWheelRevRpS * MotiveForceGearRatio))));
                }


                // (b) - Cutoff Pressure
                Pressure_b_AtmPSI = Pressure_a_AtmPSI * CutoffPressureDropRatio;

                LogCutoffPressurePSI = Pressure_b_AtmPSI - OneAtmospherePSI;   // Value for log file
                LogCutoffPressurePSI = MathHelper.Clamp(LogCutoffPressurePSI, 0.00f, LogCutoffPressurePSI); // Clamp so that Cutoff pressure does not go negative

                // (c) - Release pressure 
                // Release pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                Pressure_c_AtmPSI = (Pressure_b_AtmPSI) * (cutoff + CylinderClearancePC) / (CylinderExhaustOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust

                LogReleasePressurePSI = Pressure_c_AtmPSI - OneAtmospherePSI;   // Value for log file
                LogReleasePressurePSI = MathHelper.Clamp(LogReleasePressurePSI, 0.00f, LogReleasePressurePSI); // Clamp so that Release pressure does not go negative


                // (d) - Back Pressure 
                Pressure_d_AtmPSI = BackPressureIHPtoPSI[IndicatedHorsePowerHP] + OneAtmospherePSI;

                if (throttle < 0.02f)
                {
                    Pressure_a_AtmPSI = 0.0f;  // for sake of display zero pressure values if throttle is closed.
                    Pressure_d_AtmPSI = 0.0f;
                }

                LogBackPressurePSI = Pressure_d_AtmPSI - OneAtmospherePSI;  // Value for log file
                LogBackPressurePSI = MathHelper.Clamp(LogBackPressurePSI, 0.00f, LogBackPressurePSI); // Clamp so that Back pressure does not go negative

                // (e) - Compression Pressure 
                // Calculate pre-compression pressure based upon back pressure being equal to it, as steam should be exhausting
                Pressure_e_AtmPSI = Pressure_d_AtmPSI;

                LogPreCompressionPressurePSI = Pressure_e_AtmPSI - OneAtmospherePSI;   // Value for log file
                LogPreCompressionPressurePSI = MathHelper.Clamp(LogPreCompressionPressurePSI, 0.00f, LogPreCompressionPressurePSI); // Clamp so that pre compression pressure does not go negative

                // (f) - Admission pressure 
                Pressure_f_AtmPSI = Pressure_e_AtmPSI * (CylinderCompressionCloseFactor + CylinderClearancePC) / (CylinderAdmissionOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of 

                LogPreAdmissionPressurePSI = Pressure_f_AtmPSI - OneAtmospherePSI;   // Value for log file
                LogPreAdmissionPressurePSI = MathHelper.Clamp(LogPreAdmissionPressurePSI, 0.00f, LogPreAdmissionPressurePSI); // Clamp so that pre admission pressure does not go negative

                // ****** Calculate Cylinder Work *******
                // In driving the wheels steam does work in the cylinders. The amount of work can be calculated by a typical steam indicator diagram
                // Mean Effective Pressure (work) = average positive pressures - average negative pressures
                // Average Positive pressures = admission + expansion + release
                // Average Negative pressures = exhaust + compression + pre-admission

                // Calculate Av Admission Work (inch pounds) between a) - b)
                // Av Admission work = Av (Initial Pressure + Cutoff Pressure) * length of Cylinder to cutoff
                // Mean Pressure
                float MeanPressure_ab_AtmPSI = ((Pressure_a_AtmPSI + Pressure_b_AtmPSI) / 2.0f);
                // Calculate volume between a -b
                float CylinderLength_ab_In = Me.ToIn(CylinderStrokeM * ((cutoff + CylinderClearancePC) - CylinderClearancePC));
                // Calculate work - a-b
                CylinderWork_ab_InLbs = MeanPressure_ab_AtmPSI * CylinderLength_ab_In;

                // Calculate Av Expansion Work (inch pounds) - between b) - c)
                // Av pressure during expansion = Cutoff pressure x log (ratio of expansion) / (ratio of expansion - 1.0) 
                // Av Expansion work = Av pressure during expansion * length of Cylinder during expansion
                // Mean Pressure
                float MeanPressure_bc_AtmPSI = Pressure_b_AtmPSI * ((float)Math.Log(RatioOfExpansion_bc) / (RatioOfExpansion_bc - 1.0f));
                // Calculate volume between b-c
                float CylinderLength_bc_In = Me.ToIn(CylinderStrokeM) * ((CylinderExhaustOpenFactor + CylinderClearancePC) - (cutoff + CylinderClearancePC));
                // Calculate work - b-c
                CylinderWork_bc_InLbs = MeanPressure_bc_AtmPSI * CylinderLength_bc_In;

                // Calculate Av Release work (inch pounds) - between c) - d)
                // Av Release work = Av pressure during release * length of Cylinder during release
                // Mean Pressure
                float MeanPressure_cd_AtmPSI = ((Pressure_c_AtmPSI + Pressure_d_AtmPSI) / 2.0f);
                // Calculate volume between c-d
                float CylinderLength_cd_In = Me.ToIn(CylinderStrokeM) * ((1.0f + CylinderClearancePC) - (CylinderExhaustOpenFactor + CylinderClearancePC)); // Full cylinder length is 1.0
                // Calculate work - c-d             
                CylinderWork_cd_InLbs = MeanPressure_cd_AtmPSI * CylinderLength_cd_In;

                // Calculate Av Exhaust Work (inch pounds) - between d) - e)
                // Av Exhaust work = Av pressure during exhaust * length of Cylinder during exhaust stroke
                // Mean Pressure
                float MeanPressure_de_AtmPSI = ((Pressure_d_AtmPSI + Pressure_e_AtmPSI) / 2.0f);
                // Calculate volume between d-e
                float CylinderLength_de_In = Me.ToIn(CylinderStrokeM) * ((1.0f + CylinderClearancePC) - (CylinderCompressionCloseFactor + CylinderClearancePC)); // Full cylinder length is 1.0
                // Calculate work - d-e
                CylinderWork_de_InLbs = MeanPressure_de_AtmPSI * CylinderLength_de_In;

                // Calculate Av Compression Work (inch pounds) - between e) - f)
                // Ratio of compression = stroke during compression = stroke @ start of compression / stroke and end of compression
                // Av compression pressure = PreCompression Pressure x Ratio of Compression x log (Ratio of Compression) / (Ratio of Compression - 1.0)
                // Av Exhaust work = Av pressure during compression * length of Cylinder during compression stroke
                // Mean Pressure
                float RatioOfCompression_ef = (CylinderVolumePoint_e) / (CylinderVolumePoint_f);
                float MeanPressure_ef_AtmPSI = Pressure_e_AtmPSI * RatioOfCompression_ef * ((float)Math.Log(RatioOfCompression_ef) / (RatioOfCompression_ef - 1.0f));
                // Calculate volume between e-f
                float CylinderLength_ef_In = Me.ToIn(CylinderStrokeM) * (CylinderVolumePoint_e - CylinderVolumePoint_f);
                // Calculate work - e-f
                CylinderWork_ef_InLbs = MeanPressure_ef_AtmPSI * CylinderLength_ef_In;

                // Calculate Av Pre-admission work (inch pounds) - between f) - a)
                // Av Pre-admission work = Av pressure during pre-admission * length of Cylinder during pre-admission stroke
                // Mean Pressure
                float MeanPressure_fa_AtmPSI = ((Pressure_a_AtmPSI + Pressure_f_AtmPSI) / 2.0f);
                // Calculate volume between f-a
                float CylinderLength_fa_In = CylinderAdmissionOpenFactor * Me.ToIn(CylinderStrokeM);
                // Calculate work - f-a
                CylinderWork_fa_InLbs = MeanPressure_fa_AtmPSI * CylinderLength_fa_In;

                // Calculate total work in cylinder
                float TotalWorkInLbs = CylinderWork_ab_InLbs + CylinderWork_bc_InLbs + CylinderWork_cd_InLbs - CylinderWork_de_InLbs - CylinderWork_ef_InLbs - CylinderWork_fa_InLbs;

                MeanEffectivePressurePSI = TotalWorkInLbs / Me.ToIn(CylinderStrokeM); // MEP doen't need to be converted from Atm to gauge pressure as it is a differential pressure.
                MeanEffectivePressurePSI = MathHelper.Clamp(MeanEffectivePressurePSI, 0, MaxBoilerPressurePSI); // Make sure that Cylinder pressure does not go negative

#if DEBUG_LOCO_STEAM_MEP
                if (DebugWheelRevs >= 55.0 && DebugWheelRevs < 55.1 | DebugWheelRevs >= 110.0 && DebugWheelRevs < 110.1 | DebugWheelRevs >= 165.0 && DebugWheelRevs < 165.05 | DebugWheelRevs >= 220.0 && DebugWheelRevs < 220.05)
                 {
                     Trace.TraceInformation("***************************************** Single Expansion Steam Locomotive ***************************************************************");
 
                    Trace.TraceInformation("All pressures in Atmospheric Pressure (ie Added 14.5psi)");

                     Trace.TraceInformation("*********** Operating Conditions *********");
 
                    Trace.TraceInformation("Boiler Pressure {0} Initial/Cutoff Factor {1}", BoilerPressurePSI, CutoffPressureDropRatio);

                     Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2} RelPt {3} Clear {4}", throttle, cutoff, pS.TopM(DrvWheelRevRpS), CylinderExhaustOpenFactor, CylinderClearancePC);
 
                     Trace.TraceInformation("*********** Cylinder *********");
 
                    Trace.TraceInformation("Cylinder Pressures: a {0} b {1} c {2} d {3} e {4} f {5}", Pressure_a_AtmPSI, Pressure_b_AtmPSI , Pressure_c_AtmPSI , Pressure_d_AtmPSI , Pressure_e_AtmPSI , Pressure_f_AtmPSI);
 
                    Trace.TraceInformation("MeanPressure b-c (Expansion):MeanPressure b-c {0} ExpRatio {1} cutoff {2} Release {3}", MeanPressure_bc_AtmPSI, RatioOfExpansion_bc, cutoff, CylinderExhaustOpenFactor);
 
                    Trace.TraceInformation("MeanPressure e-f (Compression): MeanPressure e-f {0} CompRatio {1} Vol_e {2} Vol_f {3}", MeanPressure_ef_AtmPSI, RatioOfCompression_ef , CylinderVolumePoint_e, CylinderVolumePoint_f);
 
                    Trace.TraceInformation("Cylinder Works: Total {0} === a-b {1} b-c {2} c-d {3} d-e {4} e-f {5} f-a {6}", TotalWorkInLbs, CylinderWork_ab_InLbs, CylinderWork_bc_InLbs, CylinderWork_cd_InLbs, CylinderWork_de_InLbs, CylinderWork_ef_InLbs, CylinderWork_fa_InLbs);

                    Trace.TraceInformation("MEP {0}", MeanEffectivePressurePSI);
                 }
#endif

            }

            #endregion
            // Determine if Superheater in use
            if (HasSuperheater)
            {
                CurrentSuperheatTempF = SuperheatTempLbpHtoDegF[pS.TopH(CylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate current superheat temp
                CurrentSuperheatTempF = MathHelper.Clamp(CurrentSuperheatTempF, 0.0f, MaxSuperheatRefTempF); // make sure that superheat temp does not exceed max superheat temp or drop below zero
                float CylinderCondensationSpeedFactor = 1.0f - 0.00214f * pS.TopM(DrvWheelRevRpS); // This provides a speed related factor which reduces the amount of superheating required to overcome 
                // initial condensation, ie allows for condensation reduction as more steam goes through the cylinder as speed increases and the cylinder gets hotter
                CylinderCondensationSpeedFactor = MathHelper.Clamp(CylinderCondensationSpeedFactor, 0.25f, 1.0f); // make sure that speed factor does not go out of bounds
                float DifferenceSuperheatTeampF = CurrentSuperheatTempF - (SuperheatTempLimitXtoDegF[cutoff] * CylinderCondensationSpeedFactor); // reduce superheat temp due to cylinder condensation
                SuperheatVolumeRatio = 1.0f + (0.0015f * DifferenceSuperheatTeampF); // Based on formula Vsup = Vsat ( 1 + 0.0015 Tsup) - Tsup temperature at superheated level
                // look ahead to see what impact superheat will have on cylinder usage
                float FutureCylinderSteamUsageLBpS = CylinderSteamUsageLBpS * 1.0f / SuperheatVolumeRatio; // Calculate potential future new cylinder steam usage
                float FutureSuperheatTempF = SuperheatTempLbpHtoDegF[pS.TopH(FutureCylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate potential future new superheat temp

                float SuperheatTempThresholdXtoDegF = SuperheatTempLimitXtoDegF[cutoff] - 25.0f; // 10 deg bandwith reduction to reset superheat flag

                if (CurrentSuperheatTempF > SuperheatTempLimitXtoDegF[cutoff] * CylinderCondensationSpeedFactor)
                {
                    IsSuperSet = true;    // Set to use superheat factor if above superheat temp threshold      
                }
                else if (FutureSuperheatTempF < SuperheatTempThresholdXtoDegF * CylinderCondensationSpeedFactor)
                {
                    IsSuperSet = false;    // Reset if superheat temp drops 
                }


                if (IsSuperSet)
                {
                    SuperheaterSteamUsageFactor = 1.0f / SuperheatVolumeRatio; // set steam usage based upon the volume of superheated steam
                }
                else // Superheated locomotive, but superheat temp limit has not been reached.
                {
                    CylinderCondensationFactor = CylinderCondensationFractionX[cutoff];

                    float CondensationFactorTemp = 1.0f + (CylinderCondensationFactor);  // Calculate correcting factor for steam use due to compensation
                    float TempCondensationFactor = CondensationFactorTemp - 1.0f;
                    float SuperHeatMultiplier = (1.0f - (CurrentSuperheatTempF / SuperheatTempLimitXtoDegF[cutoff])) * TempCondensationFactor;
                    SuperHeatMultiplier = MathHelper.Clamp(SuperHeatMultiplier, 0.0f, SuperHeatMultiplier);
                    float SuperHeatFactorFinal = 1.0f + SuperHeatMultiplier;
                    SuperheaterSteamUsageFactor = SuperHeatFactorFinal;
                    SuperheaterSteamUsageFactor = MathHelper.Clamp(SuperheaterSteamUsageFactor, 0.0f, 1.0f); // In saturated mode steam usage should not be reduced
                }
            }
            else // Saturated steam locomotive
            {
                CylinderCondensationFactor = CylinderCondensationFractionX[cutoff];
                float CondensationFactorTemp = 1.0f + (CylinderCondensationFactor);  // Calculate correcting factor for steam use due to compensation
                SuperheaterSteamUsageFactor = CondensationFactorTemp;
                //      SuperheaterSteamUsageFactor = 1.0f; // Steam input to cylinder, but loses effectiveness. In saturated mode steam usage should not be reduced
            }

            SuperheaterSteamUsageFactor = MathHelper.Clamp(SuperheaterSteamUsageFactor, 0.60f, SuperheaterSteamUsageFactor); // ensure factor does not go below 0.6, as this represents base steam consumption by the cylinders.

            // mean pressure during stroke = ((absolute mean pressure + (clearance + cylstroke)) - (initial pressure + clearance)) / cylstroke
            // Mean effective pressure = cylinderpressure - backpressure

            // Cylinder pressure also reduced by steam vented through cylinder cocks.
            CylCockPressReduceFactor = 1.0f;

            if (CylinderCocksAreOpen) // Don't apply steam cocks derate until Cylinder steam usage starts to work
            {
                if (HasSuperheater) // Superheated locomotive
                {
                    CylCockPressReduceFactor = ((CylinderSteamUsageLBpS / SuperheaterSteamUsageFactor) / ((CylinderSteamUsageLBpS / SuperheaterSteamUsageFactor) + CylCockSteamUsageLBpS)); // For superheated locomotives temp convert back to a saturated comparison for calculation of steam cock reduction factor.
                }
                else // Simple locomotive
                {
                    CylCockPressReduceFactor = (CylinderSteamUsageLBpS / (CylinderSteamUsageLBpS + CylCockSteamUsageLBpS)); // Saturated steam locomotive
                }

                if (SteamEngineType == SteamEngineTypes.Compound)
                {
                    if (CylinderCompoundOn)  // Compound bypass valve open - simple mode for compound locomotive 
                    {
                        CylinderCocksPressureAtmPSI = LPPressure_b_AtmPSI - (LPPressure_b_AtmPSI * (1.0f - CylCockPressReduceFactor)); // Allow for pressure reduction due to Cylinder cocks being open.
                    }
                    else // Compound mode for compound locomotive
                    {
                        CylinderCocksPressureAtmPSI = HPCompPressure_b_AtmPSI - (HPCompPressure_b_AtmPSI * (1.0f - CylCockPressReduceFactor)); // Allow for pressure reduction due to Cylinder cocks being open.
                    }
                }
                else // Simple locomotive
                {
                    CylinderCocksPressureAtmPSI = Pressure_b_AtmPSI - (Pressure_b_AtmPSI * (1.0f - CylCockPressReduceFactor)); // Allow for pressure reduction due to Cylinder cocks being open.
                }
            }
            else // Cylinder cocks closed, put back to normal
            {
                if (SteamEngineType == SteamEngineTypes.Compound)
                {
                    if (CylinderCompoundOn)  // simple mode for compound locomotive 
                    {
                        CylinderCocksPressureAtmPSI = LPPressure_b_AtmPSI;
                    }
                    else // Compound mode for compound locomotive
                    {
                        CylinderCocksPressureAtmPSI = HPCompPressure_b_AtmPSI;
                    }
                }
                else // Simple locomotive
                {
                    CylinderCocksPressureAtmPSI = Pressure_b_AtmPSI;
                }
            }

            CylinderCocksPressureAtmPSI = MathHelper.Clamp(CylinderCocksPressureAtmPSI, 0, MaxBoilerPressurePSI + OneAtmospherePSI); // Make sure that Cylinder pressure does not go negative

            #region Calculation of Cylinder steam usage using an Indicator Diagram type approach
            // Reference - Indicator Practice and Steam-Engine Economy by Frank Hemenway - pg 65
            // To calculate steam usage, Calculate amount of steam in cylinder 
            // Cylinder steam usage = steam volume (and weight) at start of release stage - steam remaining in cylinder after compression (when admission valve opens)
            // This amount then should be corrected to allow for cylinder condensation in saturated locomotives or not in superheated locomotives

            if (SteamEngineType == SteamEngineTypes.Compound)
            {

                if (!CylinderCompoundOn) // cylinder bypass value closed - in compound mode
                // The steam in the HP @ Cutoff will give an indication of steam usage.
                {
                    float HPCylinderReleasePressureGaugePSI = HPCompPressure_d_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                    float HPCylinderAdmissionPressureGaugePSI = HPCompPressure_k_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  
                    CylinderReleaseSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (CylinderExhaustOpenFactor + HPCylinderClearancePC); // Calculate volume of cylinder at start of release
                    CylinderReleaseSteamWeightLbs = CylinderReleaseSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[HPCylinderReleasePressureGaugePSI]; // Weight of steam in Cylinder at release
                    CylinderAdmissionSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (CylinderCompressionCloseFactor + HPCylinderClearancePC); // volume of the clearance area + area of steam at pre-admission
                    if (HPCylinderAdmissionPressureGaugePSI > 0.0) // need to consider steam density for pressures less then 0 gauge pressure - To Do
                    {
                        CylinderAdmissionSteamWeightLbs = CylinderAdmissionSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[HPCylinderAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                    }
                    else
                    {
                        CylinderAdmissionSteamWeightLbs = 0.0f;
                    }
                    // For time being assume that compound locomotive doesn't experience cylinder condensation.
                    RawCylinderSteamWeightLbs = CylinderReleaseSteamWeightLbs - CylinderAdmissionSteamWeightLbs;
                    RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * RawCylinderSteamWeightLbs;
                    CalculatedCylinderSteamUsageLBpS = RawCalculatedCylinderSteamUsageLBpS * SuperheaterSteamUsageFactor;

                }
                else  // Simple mode
                // Steam at cutoff in LP will will give an indication of steam usage.
                {
                    float LPCylinderReleasePressureGaugePSI = LPPressure_c_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                    float LPCylinderAdmissionPressureGaugePSI = LPPressure_f_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  
                    CylinderReleaseSteamVolumeFt3 = LPCylinderSweptVolumeFT3pFT * (CylinderExhaustOpenFactor + LPCylinderClearancePC); // Calculate volume of cylinder at release
                    CylinderReleaseSteamWeightLbs = CylinderReleaseSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[LPCylinderReleasePressureGaugePSI]; // Weight of steam in Cylinder at release
                    CylinderAdmissionSteamVolumeFt3 = LPCylinderSweptVolumeFT3pFT * (CylinderAdmissionOpenFactor + LPCylinderClearancePC); // volume of the clearance area + area of steam at admission
                    if (LPCylinderAdmissionPressureGaugePSI > 0.0) // need to consider steam density for pressures less then 0 gauge pressure - To Do
                    {
                        CylinderAdmissionSteamWeightLbs = CylinderAdmissionSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[LPCylinderAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                    }
                    else
                    {
                        CylinderAdmissionSteamWeightLbs = 0.0f;
                    }
                    RawCylinderSteamWeightLbs = CylinderReleaseSteamWeightLbs - CylinderAdmissionSteamWeightLbs;
                    RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * RawCylinderSteamWeightLbs;
                    CalculatedCylinderSteamUsageLBpS = RawCalculatedCylinderSteamUsageLBpS * SuperheaterSteamUsageFactor;
                }
            }
            else // Calculate steam usage for simple and geared locomotives.
            {
                float CylinderReleasePressureGaugePSI = Pressure_c_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                CylinderReleasePressureGaugePSI = MathHelper.Clamp(CylinderReleasePressureGaugePSI, 0.0f, BoilerPressurePSI);
                CylinderReleaseSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (CylinderExhaustOpenFactor + CylinderClearancePC); // Calculate volume of cylinder at release
                CylinderReleaseSteamWeightLbs = CylinderReleaseSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[CylinderReleasePressureGaugePSI]; // Weight of steam in Cylinder at release

                float CylinderAdmissionPressureGaugePSI = Pressure_f_AtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  (back pressure)
                CylinderAdmissionPressureGaugePSI = MathHelper.Clamp(CylinderAdmissionPressureGaugePSI, 0.0f, BoilerPressurePSI);
                CylinderAdmissionSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (CylinderAdmissionOpenFactor + CylinderClearancePC); // volume of the clearance area + volume of steam at admission

                if (CylinderAdmissionPressureGaugePSI > 0.0) // need to consider steam density for pressures less then 0 gauge pressure - To Do
                {
                    CylinderAdmissionSteamWeightLbs = CylinderAdmissionSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[CylinderAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                }
                else
                {
                    CylinderAdmissionSteamWeightLbs = 0.0f;
                }
                RawCylinderSteamWeightLbs = CylinderReleaseSteamWeightLbs - CylinderAdmissionSteamWeightLbs;

                // Calculate steam usage based upon how many piston strokes happen for each revolution of the wheels. 
                // Geared locomotives will have to take into account gearing ratio. 
                RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * MotiveForceGearRatio * CylStrokesPerCycle * RawCylinderSteamWeightLbs;
                CalculatedCylinderSteamUsageLBpS = RawCalculatedCylinderSteamUsageLBpS * SuperheaterSteamUsageFactor;
            }

            #endregion

            if (throttle < 0.01 && absSpeedMpS > 0.1 || FusiblePlugIsBlown) // If locomotive moving and throttle set to close, then reduce steam usage, alternatively if fusible plug is blown.
            {
                CalculatedCylinderSteamUsageLBpS = 0.0001f; // Set steam usage to a small value if throttle is closed
            }

            // usage calculated as moving average to minimize chance of oscillation.
            // Decrease steam usage by SuperheaterUsage factor to model superheater - very crude model - to be improved upon
            CylinderSteamUsageLBpS = (0.6f * CylinderSteamUsageLBpS + 0.4f * CalculatedCylinderSteamUsageLBpS);

            BoilerMassLB -= elapsedClockSeconds * CylinderSteamUsageLBpS; //  Boiler mass will be reduced by cylinder steam usage
            BoilerHeatBTU -= elapsedClockSeconds * CylinderSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); //  Boiler Heat will be reduced by heat required to replace the cylinder steam usage, ie create steam from hot water. 
            TotalSteamUsageLBpS += CylinderSteamUsageLBpS;
            BoilerHeatOutBTUpS += CylinderSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);
            CumulativeCylinderSteamConsumptionLbs += CylinderSteamUsageLBpS * elapsedClockSeconds;

        }

        private void UpdateMotion(float elapsedClockSeconds, float cutoff, float absSpeedMpS)
        {

            // This section updates the force calculations and maintains them at the current values.

            // Caculate the current piston speed - purely for display purposes at the moment 
            // Piston Speed (Ft p Min) = (Stroke length x 2) x (Ft in Mile x Train Speed (mph) / ( Circum of Drv Wheel x 60))
            PistonSpeedFtpMin = Me.ToFt(pS.TopM(CylinderStrokeM * 2.0f * DrvWheelRevRpS)) * SteamGearRatio;

            if (SteamEngineType == SteamEngineTypes.Compound)
            {
                // Calculate tractive effort if set for compounding - tractive effort in each cylinder will need to be calculated

                // HP Cylinder

                float HPTractiveEffortLbsF = (NumCylinders / 2.0f) * MotiveForceGearRatio * ((HPCylinderMEPPSI * CylinderEfficiencyRate) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2.0f * Me.ToIn(DriverWheelRadiusM)));

                // LP Cylinder

                float LPTractiveEffortLbsF = (LPNumCylinders / 2.0f) * MotiveForceGearRatio * ((LPCylinderMEPPSI * CylinderEfficiencyRate) * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderStrokeM)) / (2.0f * Me.ToIn(DriverWheelRadiusM));

                TractiveEffortLbsF = (HPTractiveEffortLbsF + LPTractiveEffortLbsF);
                TractiveEffortLbsF = MathHelper.Clamp(TractiveEffortLbsF, 0.0f, MaxTractiveEffortLbf); // Ensure tractive effort never exceeds starting TE

                // Calculate IHP
                // IHP = (MEP x Speed (mph)) / 375.0) - this is per cylinder

                HPIndicatedHorsePowerHP = (HPTractiveEffortLbsF * pS.TopH(Me.ToMi(absSpeedMpS))) / 375.0f;
                LPIndicatedHorsePowerHP = (LPTractiveEffortLbsF * pS.TopH(Me.ToMi(absSpeedMpS))) / 375.0f;

                float WheelRevs = pS.TopM(DrvWheelRevRpS);
                IndicatedHorsePowerHP = HPIndicatedHorsePowerHP + LPIndicatedHorsePowerHP;
                IndicatedHorsePowerHP = MathHelper.Clamp(IndicatedHorsePowerHP, 0, IndicatedHorsePowerHP);
            }
            else // if simple or geared locomotive calculate tractive effort
            {

                // If the steam piston is exceeding the maximum design piston rate then decrease efficiency of mep
                if (SteamEngineType == SteamEngineTypes.Geared && PistonSpeedFtpMin > MaxSteamGearPistonRateFtpM)
                {
                    // use straight line curve to decay mep to zero by 2 x piston speed
                    float pistonforcedecay = 1.0f - (1.0f / MaxSteamGearPistonRateFtpM) * (PistonSpeedFtpMin - MaxSteamGearPistonRateFtpM);
                    pistonforcedecay = MathHelper.Clamp(pistonforcedecay, 0.0f, 1.0f);  // Clamp decay within bounds

                    MeanEffectivePressurePSI *= pistonforcedecay; // Decrease mep once piston critical speed is exceeded
                }

                TractiveEffortLbsF = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2.0f * Me.ToIn(DriverWheelRadiusM))) * (MeanEffectivePressurePSI * CylinderEfficiencyRate) * MotiveForceGearRatio;

                // Force tractive effort to zero if throttle is closed, or if a geared steam locomotive in neutral gear. MEP calculation is not allowing it to go to zero
                if (throttle < 0.001 || (SteamEngineType == SteamEngineTypes.Geared && SteamGearPosition == 0))
                {
                    TractiveEffortLbsF = 0.0f; 
                }
                TractiveEffortLbsF = MathHelper.Clamp(TractiveEffortLbsF, 0, TractiveEffortLbsF);
                DisplayTractiveEffortLbsF = TractiveEffortLbsF;

                // Calculate IHP
                // IHP = (MEP x CylStroke(ft) x cylArea(sq in) x No Strokes (/min)) / 33000) - this is per cylinder

                IndicatedHorsePowerHP = (TractiveEffortLbsF * pS.TopH(Me.ToMi(absSpeedMpS))) / 375.0f;
                IndicatedHorsePowerHP = MathHelper.Clamp(IndicatedHorsePowerHP, 0, IndicatedHorsePowerHP);
            }

            // Calculate the elapse time for the steam performance monitoring
            if (Simulator.Settings.DataLogSteamPerformance)
            {
                if (SpeedMpS > 0.05)
                {
                    SteamPerformanceTimeS += elapsedClockSeconds;
                }
                else if (SpeedMpS < 0.04)
                {
                    SteamPerformanceTimeS = 0.0f;   // set time to zero if loco stops
                }
            }

            // Calculate friction values and load variables for train
            TotalFrictionForceN = 0.0f;
            LocomotiveCouplerForceN = 0.0f;
            TrainLoadKg = 0.0f;
            for (int i = 0; i < Train.Cars.Count; i++)  // Doesn't included the locomotive or tender
                if (Train.Cars[i].SpeedMpS > 0)
                {
                    if (Train.Cars[i].WagonType != WagonTypes.Engine && Train.Cars[i].WagonType != WagonTypes.Tender)
                    {
                        TotalFrictionForceN += Train.Cars[i].FrictionForceN;
                        TrainLoadKg += Train.Cars[i].MassKG;
                    }
                    if ((Train.Cars[i].WagonType == WagonTypes.Engine || Train.Cars[i].WagonType == WagonTypes.Tender) && i < 2)
                    {
                        LocomotiveCouplerForceN = -1.0f * Train.Cars[i].CouplerForceU;
                    }

                }


            // Reset frictional forces of the locomotive
            CombFrictionN = 0;
            CombGravityN = 0;
            CombTunnelN = 0;
            CombCurveN = 0;
            CombWindN = 0;


            LocoIndex = 0;
            for (int i = 0; i < Train.Cars.Count; i++)  // Doesn't included the locomotive or tender
            {
                if (Train.Cars[i] == this)
                    LocoIndex = i;
            }

            // Find frictional forces of the locomotive
            CombFrictionN = Train.Cars[LocoIndex].FrictionForceN;
            CombGravityN -= Train.Cars[LocoIndex].GravityForceN;
            CombTunnelN = Train.Cars[LocoIndex].TunnelForceN;
            CombCurveN = Train.Cars[LocoIndex].CurveForceN;
            CombWindN = Train.Cars[LocoIndex].WindForceN;

            if (HasTenderCoupled)
            {
                if (LocoIndex < Train.Cars.Count - 1) // Room for a tender in the train
                {
                    // Find frictional forces of tender and add them to the locomotive
                    CombFrictionN += Train.Cars[LocoIndex + 1].FrictionForceN;
                    CombGravityN -= Train.Cars[LocoIndex + 1].GravityForceN; // Gravity forces have negative values on rising grade
                    CombTunnelN += Train.Cars[LocoIndex + 1].TunnelForceN;
                    CombCurveN += Train.Cars[LocoIndex + 1].CurveForceN;
                    CombWindN += Train.Cars[LocoIndex + 1].WindForceN;
                }

            }

            LocoTenderFrictionForceN = CombFrictionN + CombGravityN + CombTunnelN + CombCurveN + CombWindN;  // Combined frictional forces of the locomotive and tender

            MotiveForceSmoothedN.Update(elapsedClockSeconds, MotiveForceN);
            if (float.IsNaN(MotiveForceN))
                MotiveForceN = 0;
            switch (this.Train.TrainType)
            {
                case Train.TRAINTYPE.AI:
                case Train.TRAINTYPE.AI_PLAYERHOSTING:
                case Train.TRAINTYPE.STATIC:
                case Train.TRAINTYPE.INTENDED_PLAYER:
                    break;
                case Train.TRAINTYPE.PLAYER:
                case Train.TRAINTYPE.AI_PLAYERDRIVEN:
                case Train.TRAINTYPE.REMOTE:
                    AdvancedAdhesion(elapsedClockSeconds);
                    break;
                default:
                    break;
            }

        }

        protected override void UpdateMotiveForce(float elapsedClockSeconds, float t, float AbsSpeedMpS, float AbsWheelSpeedMpS)
        {
            // Pass force and power information to MSTSLocomotive file by overriding corresponding method there

            // Set Max Power equal to max IHP
            MaxPowerW = W.FromHp(MaxIndicatedHorsePowerHP);

            // Set maximum force for the locomotive
            MaxForceN = N.FromLbf(MaxTractiveEffortLbf * CylinderEfficiencyRate);

            // Set Max Velocity of locomotive
            MaxSpeedMpS = Me.FromMi(pS.FrompH(MaxLocoSpeedMpH)); // Note this is not the true max velocity of the locomotive, but  the speed at which max HP is reached

            // Set "current" motive force based upon the throttle, cylinders, steam pressure, etc	
            MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * N.FromLbf(TractiveEffortLbsF);

            // On starting allow maximum motive force to be used, unless gear is in neutral (normally only geared locomotive will be zero).
            if (absSpeedMpS < 1.0f && cutoff > 0.70f && throttle > 0.98f && MotiveForceGearRatio != 0)
            {
                MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * MaxForceN;
            }

            if (absSpeedMpS == 0 && cutoff < 0.05f) // If the reverser is set too low then not sufficient steam is admitted to the steam cylinders, and hence insufficient Motive Force will produced to move the train.
                MotiveForceN = 0;

            // Based upon max IHP, limit motive force.

            if (IndicatedHorsePowerHP >= MaxIndicatedHorsePowerHP)
            {
                MotiveForceN = N.FromLbf((MaxIndicatedHorsePowerHP * 375.0f) / pS.TopH(Me.ToMi(SpeedMpS)));
                IndicatedHorsePowerHP = MaxIndicatedHorsePowerHP; // Set IHP to maximum value
                IsCritTELimit = true; // Flag if limiting TE
            }
            else
            {
                IsCritTELimit = false; // Reset flag if limiting TE
            }

            DrawBarPullLbsF = N.ToLbf(Math.Abs(MotiveForceN) - LocoTenderFrictionForceN); // Locomotive drawbar pull is equal to motive force of locomotive (+ tender) - friction forces of locomotive (+ tender)
            DrawBarPullLbsF = MathHelper.Clamp(DrawBarPullLbsF, 0, DrawBarPullLbsF); // clamp value so it doesn't go negative

            DrawbarHorsePowerHP = (DrawBarPullLbsF * MpS.ToMpH(absSpeedMpS)) / 375.0f;  // TE in this instance is a maximum, and not at the wheel???
            DrawbarHorsePowerHP = MathHelper.Clamp(DrawbarHorsePowerHP, 0, DrawbarHorsePowerHP); // clamp value so it doesn't go negative

            #region - Steam Adhesion Model Input for Steam Locomotives

            // Based upon information presented in "Locomotive Operation - A Technical and Practical Analysis" by G. R. Henderson - https://archive.org/details/locomotiveoperat00hend
            // At its simplest slip occurs when the wheel tangential force exceeds the static frictional force
            // Static frictional force = weight on the locomotive driving wheels * frictional co-efficient
            // Tangential force = Effective force (Interia + Piston force) * Tangential factor (sin (crank angle) + (crank radius / connecting rod length) * sin (crank angle) * cos (crank angle))
            // Typically tangential force will be greater at starting then when the locomotive is at speed, as interia and reduce steam pressure will decrease the value. 
            // By default this model uses information based upon a "NYC 4-4-2 locomotive", for smaller locomotives this data is changed in the OR initialisation phase.

            if (Simulator.UseAdvancedAdhesion && this == Simulator.PlayerLocomotive && this.Train.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING) // only set advanced wheel slip when advanced adhesion and is the player locomotive, AI locomotive will not work to this model. Don't use slip model when train is in auto pilot
            {
                float SlipCutoffPressureAtmPSI;
                float SlipCylinderReleasePressureAtmPSI;
                float SlipInitialPressureAtmPSI;


                // Starting tangential force - at starting piston force is based upon cutoff pressure  & interia = 0
                if (SteamEngineType == SteamEngineTypes.Compound)
                {
                    if (!CylinderCompoundOn) // Bypass Valve closed - in Compound Mode
                    {
                        StartPistonForceLeftLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * HPCompPressure_a_AtmPSI; // Piston force is equal to pressure in piston and piston area
                        SlipInitialPressureAtmPSI = HPCompPressure_a_AtmPSI;
                        SlipCutoffPressureAtmPSI = HPCompPressure_b_AtmPSI;
                        SlipCylinderReleasePressureAtmPSI = HPCompPressure_f_AtmPSI;
                    }
                    else  // Simple mode
                    {
                        StartPistonForceLeftLbf = Me2.ToIn2(Me2.FromFt2(LPCylinderPistonAreaFt2)) * LPPressure_a_AtmPSI; // Piston force is equal to pressure in piston and piston area
                        SlipInitialPressureAtmPSI = LPPressure_a_AtmPSI;
                        SlipCutoffPressureAtmPSI = LPPressure_b_AtmPSI;
                        SlipCylinderReleasePressureAtmPSI = LPPressure_c_AtmPSI;
                    }
                }
                else // simple locomotive
                {
                    StartPistonForceLeftLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * Pressure_a_AtmPSI; // Piston force is equal to pressure in piston and piston area
                    SlipInitialPressureAtmPSI = Pressure_a_AtmPSI;
                    SlipCutoffPressureAtmPSI = Pressure_b_AtmPSI;
                    SlipCylinderReleasePressureAtmPSI = Pressure_c_AtmPSI;
                }

                // At starting, for 2 cylinder locomotive, maximum tangential force occurs at the following crank angles:
                // Backward - 45 deg & 135 deg, Forward - 135 deg & 45 deg. To calculate the maximum we only need to select one of these points
                // To calculate total tangential force we need to calculate the left and right hand side of the locomotive, LHS & RHS will be 90 deg apart

                if (NumCylinders == 3.0)
                {
                    // Calculate values at start
                    StartCrankAngleLeft = RadConvert * 30.0f;   // For 3 Cylinder locomotive, cranks are 120 deg apart, and maximum occurs @ 
                    StartCrankAngleMiddle = RadConvert * 150.0f;    // 30, 150, 270 deg crank angles
                    StartCrankAngleRight = RadConvert * 270.0f;
                    StartTangentialCrankForceFactorLeft = (float)Math.Abs(((float)Math.Sin(StartCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleLeft) * (float)Math.Cos(StartCrankAngleLeft))));
                    StartTangentialCrankForceFactorMiddle = (float)Math.Abs(((float)Math.Sin(StartCrankAngleMiddle) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleMiddle) * (float)Math.Cos(StartCrankAngleMiddle))));
                    StartTangentialCrankForceFactorRight = (float)Math.Abs(((float)Math.Sin(StartCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleRight) * (float)Math.Cos(StartCrankAngleRight))));
                    StartVerticalThrustForceMiddle = 0.0f;

                    // Calculate values at speed
                    SpeedCrankAngleLeft = RadConvert * 30.0f;   // For 3 Cylinder locomotive, cranks are 120 deg apart, and maximum occurs @ 
                    SpeedCrankAngleMiddle = RadConvert * (30.0f + 120.0f + 120.0f); // 30, 150, 270 deg crank angles
                    SpeedCrankAngleRight = RadConvert * (30.0f + 120.0f);
                    SpeedTangentialCrankForceFactorLeft = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleLeft) * (float)Math.Cos(SpeedCrankAngleLeft))));
                    SpeedTangentialCrankForceFactorMiddle = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleMiddle) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleMiddle) * (float)Math.Cos(SpeedCrankAngleMiddle))));
                    SpeedTangentialCrankForceFactorRight = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleRight) * (float)Math.Cos(SpeedCrankAngleRight))));
                    SpeedVerticalThrustForceMiddle = 0.0f;
                    CrankCylinderPositionLeft = 30.0f / 180.0f;
                    CrankCylinderPositionMiddle = ((30.0f + 120.0f + 120.0f) - 180.0f) / 180.0f;
                    CrankCylinderPositionRight = (30.0f + 120.0f) / 180.0f;
                }
                else // if 2 cylinder
                {
                    // Calculate values at start
                    StartCrankAngleLeft = RadConvert * 45.0f;   // For 2 Cylinder locomotive, cranks are 90 deg apart, and maximum occurs @ 
                    StartCrankAngleMiddle = RadConvert * 0.0f;
                    StartCrankAngleRight = RadConvert * (45.0f + 90.0f);    // 315 & 45 deg crank angles
                    StartTangentialCrankForceFactorLeft = ((float)Math.Sin(StartCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleLeft) * (float)Math.Cos(StartCrankAngleLeft)));
                    StartTangentialCrankForceFactorRight = ((float)Math.Sin(StartCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleRight) * (float)Math.Cos(StartCrankAngleRight)));
                    StartTangentialCrankForceFactorMiddle = 0.0f;

                    // Calculate values at speed
                    SpeedCrankAngleLeft = RadConvert * 45.0f;   // For 2 Cylinder locomotive, cranks are 90 deg apart, and maximum occurs @ 
                    SpeedCrankAngleMiddle = 0.0f;   // 315 & 45 deg crank angles
                    SpeedCrankAngleRight = RadConvert * (45.0f + 90.0f);
                    SpeedTangentialCrankForceFactorLeft = ((float)Math.Sin(SpeedCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleLeft) * (float)Math.Cos(SpeedCrankAngleLeft)));
                    SpeedTangentialCrankForceFactorRight = ((float)Math.Sin(SpeedCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleRight) * (float)Math.Cos(SpeedCrankAngleRight)));
                    SpeedVerticalThrustForceMiddle = 0.0f;
                    CrankCylinderPositionLeft = 45.0f / 180.0f;
                    CrankCylinderPositionMiddle = 0.0f;
                    CrankCylinderPositionRight = (45.0f + 90.0f) / 180.0f;

                }



                // Calculate cylinder presssure at "maximum" cranking value
                // If cutoff hasn't reached point in piston movement, then pressure will be less

                // Left hand crank position cylinder pressure
                if (cutoff > CrankCylinderPositionLeft)  // If cutoff is greater then crank position, then pressure will be before cutoff
                {
                    CrankLeftCylinderPressure = (SlipCutoffPressureAtmPSI / cutoff) * CrankCylinderPositionLeft;
                    CrankLeftCylinderPressure = MathHelper.Clamp(SlipCutoffPressureAtmPSI, 0, Pressure_a_AtmPSI);
                }
                else // Pressure will be in the expansion section of the cylinder
                {
                    // Crank pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                    CrankLeftCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (CrankCylinderPositionLeft + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
                }

                // Right hand cranking position cylinder pressure
                if (CylinderExhaustOpenFactor > CrankCylinderPositionRight) // if exhaust opening is greating then cranking position, then pressure will be before release 
                {
                    CrankRightCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (CrankCylinderPositionRight + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
                }
                else  // Pressure will be after release
                {
                    CrankRightCylinderPressure = (SlipCylinderReleasePressureAtmPSI / CylinderExhaustOpenFactor) * CrankCylinderPositionRight;
                }

                if (NumCylinders == 3)
                {
                    // Middle crank position cylinder pressure
                    if (cutoff > CrankCylinderPositionLeft)  // If cutoff is greater then crank position, then pressure will be before cutoff
                    {
                        CrankMiddleCylinderPressure = (SlipCutoffPressureAtmPSI / cutoff) * CrankCylinderPositionMiddle;
                        CrankMiddleCylinderPressure = MathHelper.Clamp(SlipCutoffPressureAtmPSI, 0, Pressure_a_AtmPSI);
                    }
                    else // Pressure will be in the expansion section of the cylinder
                    {
                        // Crank pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                        CrankMiddleCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (CrankCylinderPositionMiddle + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
                    }
                }
                else
                {

                    CrankMiddleCylinderPressure = 0.0f;
                }

                // Calculate piston force for the relevant cylinder cranking positions
                StartPistonForceLeftLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankLeftCylinderPressure;
                StartPistonForceMiddleLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankMiddleCylinderPressure;
                StartPistonForceRightLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankRightCylinderPressure;

                SpeedPistonForceLeftLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankLeftCylinderPressure;
                SpeedPistonForceMiddleLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankMiddleCylinderPressure;
                SpeedPistonForceRightLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * CrankRightCylinderPressure;

                // Calculate the inertia of the reciprocating weights and the connecting rod
                float ReciprocatingInertiaFactorLeft = -1.603f * ((float)Math.Cos(StartCrankAngleLeft)) + ((CrankRadiusFt / ConnectRodLengthFt) * ((float)Math.Cos(2.0f * StartCrankAngleLeft)));
                float ReciprocatingInertiaForceLeft = ReciprocatingInertiaFactorLeft * ReciprocatingWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                float ReciprocatingInertiaFactorMiddle = -1.603f * ((float)Math.Cos(StartCrankAngleMiddle)) + ((CrankRadiusFt / ConnectRodLengthFt) * ((float)Math.Cos(2.0f * StartCrankAngleMiddle)));
                float ReciprocatingInertiaForceMiddle = ReciprocatingInertiaFactorMiddle * ReciprocatingWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                float ReciprocatingInertiaFactorRight = -1.603f * ((float)Math.Cos(StartCrankAngleRight)) + ((CrankRadiusFt / ConnectRodLengthFt) * ((float)Math.Cos(2.0f * StartCrankAngleRight)));
                float ReciprocatingInertiaForceRight = ReciprocatingInertiaFactorRight * ReciprocatingWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));

                float ConnectRodInertiaFactorLeft = -1.603f * ((float)Math.Cos(StartCrankAngleLeft)) + (((CrankRadiusFt * RodCoGFt) / (ConnectRodLengthFt * ConnectRodLengthFt)) * ((float)Math.Cos(2.0f * StartCrankAngleLeft)));
                float ConnectRodInertiaForceLeft = ConnectRodInertiaFactorLeft * ConnectingRodWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                float ConnectRodInertiaFactorMiddle = -1.603f * ((float)Math.Cos(StartCrankAngleMiddle)) + (((CrankRadiusFt * RodCoGFt) / (ConnectRodLengthFt * ConnectRodLengthFt)) * ((float)Math.Cos(2.0f * StartCrankAngleMiddle)));
                float ConnectRodInertiaForceMiddle = ConnectRodInertiaFactorMiddle * ConnectingRodWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                float ConnectRodInertiaFactorRight = -1.603f * ((float)Math.Cos(StartCrankAngleRight)) + (((CrankRadiusFt * RodCoGFt) / (ConnectRodLengthFt * ConnectRodLengthFt)) * ((float)Math.Cos(2.0f * StartCrankAngleRight)));
                float ConnectRodInertiaForceRight = ConnectRodInertiaFactorRight * ConnectingRodWeightLb * Me.ToIn(CylinderStrokeM) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));

                if (cutoff == 0 || throttle == 0)
                {
                    // Zero starting force if cutoff is 0
                    StartTangentialCrankWheelForceLbf = 0.0f;

                    SpeedTangentialCrankWheelForceLeftLbf = 0.0f;
                    SpeedTangentialCrankWheelForceMiddleLbf = 0.0f;
                    SpeedTangentialCrankWheelForceRightLbf = 0.0f;

                }
                else
                {
                    // Calculate the starting force at the crank exerted on the drive wheel
                    StartTangentialCrankWheelForceLbf = Math.Abs(StartPistonForceLeftLbf * StartTangentialCrankForceFactorLeft) + Math.Abs(StartPistonForceMiddleLbf * StartTangentialCrankForceFactorMiddle) + Math.Abs(StartPistonForceRightLbf * StartTangentialCrankForceFactorRight);

                    SpeedTangentialCrankWheelForceLeftLbf = SpeedPistonForceLeftLbf + ReciprocatingInertiaForceLeft + ConnectRodInertiaForceLeft;
                    SpeedTangentialCrankWheelForceMiddleLbf = SpeedPistonForceMiddleLbf + ReciprocatingInertiaForceMiddle + ConnectRodInertiaForceMiddle;
                    SpeedTangentialCrankWheelForceRightLbf = SpeedPistonForceRightLbf + ReciprocatingInertiaForceRight + ConnectRodInertiaForceRight;
                }

                if (NumCylinders == 2)
                {
                    ReciprocatingInertiaFactorMiddle = 0.0f;
                    ReciprocatingInertiaForceMiddle = 0.0f;
                    ConnectRodInertiaFactorMiddle = 0.0f;
                    ConnectRodInertiaForceMiddle = 0.0f;
                    SpeedTangentialCrankWheelForceMiddleLbf = 0.0f;
                }

                if (NumCylinders == 3.0)
                {
                    SpeedTotalTangCrankWheelForceLbf = (SpeedTangentialCrankWheelForceLeftLbf * SpeedTangentialCrankForceFactorLeft) + (SpeedTangentialCrankWheelForceMiddleLbf * SpeedTangentialCrankForceFactorMiddle) + (SpeedTangentialCrankWheelForceRightLbf * SpeedTangentialCrankForceFactorRight);
                }
                else
                {
                    SpeedTotalTangCrankWheelForceLbf = (SpeedTangentialCrankWheelForceLeftLbf * SpeedTangentialCrankForceFactorLeft) + (SpeedTangentialCrankWheelForceRightLbf * SpeedTangentialCrankForceFactorRight);
                }

                /// Calculation of Adhesion Friction Force @ Start
                /// Vertical thrust of the connecting rod will reduce or increase the effect of the adhesive weight of the locomotive
                /// Vert Thrust = Piston Force * 3/4 * r/l * sin(crank angle)
                StartVerticalThrustFactorLeft = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleLeft);
                StartVerticalThrustFactorMiddle = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleMiddle);
                StartVerticalThrustFactorRight = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleRight);

                if (NumCylinders == 2)
                {
                    StartVerticalThrustForceMiddle = 0.0f;
                }

                StartVerticalThrustForceLeft = StartPistonForceLeftLbf * StartVerticalThrustFactorLeft;
                StartVerticalThrustForceMiddle = StartPistonForceLeftLbf * StartVerticalThrustFactorMiddle;
                StartVerticalThrustForceRight = StartPistonForceLeftLbf * StartVerticalThrustFactorRight;

                /// Calculation of Adhesion Friction Force @ Speed
                /// Vertical thrust of the connecting rod will reduce or increase the effect of the adhesive weight of the locomotive
                /// Vert Thrust = Piston Force * 3/4 * r/l * sin(crank angle)
                SpeedVerticalThrustFactorLeft = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleLeft);
                SpeedVerticalThrustFactorMiddle = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleMiddle);
                SpeedVerticalThrustFactorRight = 3.0f / 4.0f * (CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleRight);

                if (NumCylinders == 2)
                {
                    SpeedVerticalThrustForceMiddle = 0.0f;
                }

                SpeedVerticalThrustForceLeft = SpeedTangentialCrankWheelForceLeftLbf * SpeedVerticalThrustFactorLeft;
                SpeedVerticalThrustForceMiddle = SpeedTangentialCrankWheelForceMiddleLbf * SpeedVerticalThrustFactorMiddle;
                SpeedVerticalThrustForceRight = SpeedTangentialCrankWheelForceRightLbf * SpeedVerticalThrustFactorRight;

                // Calculate Excess Balance
                float ExcessBalanceWeightLb = (ConnectingRodWeightLb + ReciprocatingWeightLb) - ConnectingRodBalanceWeightLb - (Kg.ToLb(MassKG) / ExcessBalanceFactor);
                ExcessBalanceForceLeft = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleLeft) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                ExcessBalanceForceMiddle = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleMiddle) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
                ExcessBalanceForceRight = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleRight) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));

                if (NumCylinders == 2)
                {
                    ExcessBalanceForceMiddle = 0.0f;
                }

                //    SpeedStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) + (SpeedVerticalThrustForceLeft + ExcessBalanceForceLeft) + (SpeedVerticalThrustForceMiddle + ExcessBalanceForceMiddle) + (SpeedVerticalThrustForceRight + ExcessBalanceForceRight)) * Train.LocomotiveCoefficientFriction;

                SpeedStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) + (SpeedVerticalThrustForceLeft + ExcessBalanceForceLeft) + (SpeedVerticalThrustForceMiddle + ExcessBalanceForceMiddle) + (SpeedVerticalThrustForceRight + ExcessBalanceForceRight)) * 0.33f;
                // Calculate internal resistance - IR = 3.8 * diameter of cylinder^2 * stroke * dia of drivers (all in inches)
                float InternalResistance = 3.8f * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (Me.ToIn(DrvWheelDiaM));

                // To convert the force at the crank to the force at wheel tread = Crank Force * Cylinder Stroke / Diameter of Drive Wheel (inches) - internal friction should be deducted from this as well.
                StartTangentialWheelTreadForceLbf = (StartTangentialCrankWheelForceLbf * Me.ToIn(CylinderStrokeM) / (Me.ToIn(DrvWheelDiaM))) - InternalResistance;
                StartTangentialWheelTreadForceLbf = MathHelper.Clamp(StartTangentialWheelTreadForceLbf, 0, StartTangentialWheelTreadForceLbf);  // Make sure force does not go negative

                SpeedTangentialWheelTreadForceLbf = (SpeedTotalTangCrankWheelForceLbf * Me.ToIn(CylinderStrokeM) / (Me.ToIn(DrvWheelDiaM))) - InternalResistance;
                SpeedTangentialWheelTreadForceLbf = MathHelper.Clamp(SpeedTangentialWheelTreadForceLbf, 0, SpeedTangentialWheelTreadForceLbf);  // Make sure force does not go negative

                // Determine weather conditions and friction coeff
                // Typical coefficients of friction taken from TrainCar Coefficients of friction as base, and altered as appropriate for steam locomotives.
                // Sand ----  40% increase of friction coeff., sand on wet rails, tends to make adhesion as good as dry rails.
                // Dry, wght per wheel > 10,000lbs   == 0.35
                // Dry, wght per wheel < 10,000lbs   == 0.25

                SteamDrvWheelWeightLbs = Kg.ToLb(DrvWheelWeightKg / LocoNumDrvAxles); // Calculate the weight per axle (used in MSTSLocomotive for friction calculatons)

                // Static Friction Force - adhesive factor increased by vertical thrust when travelling forward, and reduced by vertical thrust when travelling backwards

                if (Direction == Direction.Forward)
                {
                    StartStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) + StartVerticalThrustForceLeft + StartVerticalThrustForceRight + StartVerticalThrustForceMiddle) * Train.LocomotiveCoefficientFriction;
                }
                else
                {
                    StartStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) - StartVerticalThrustForceLeft - StartVerticalThrustForceMiddle - StartVerticalThrustForceRight) * Train.LocomotiveCoefficientFriction;
                }

                // Transition between Starting slip calculations, and slip at speed. Incremental values applied between 1 and 10mph.

                if (absSpeedMpS < 0.45)  // For low speed use the starting values
                {
                    SteamStaticWheelForce = StartStaticWheelFrictionForceLbf;
                    SteamTangentialWheelForce = StartTangentialWheelTreadForceLbf;
                }
                else if (absSpeedMpS > 4.5) // for high speed use "running values"
                {
                    SteamStaticWheelForce = SpeedStaticWheelFrictionForceLbf;
                    SteamTangentialWheelForce = SpeedTangentialWheelTreadForceLbf;
                }
                else
                {
                    // incremental straight line used model static value between 1 and 10mph
                    float LineGrad = (SpeedStaticWheelFrictionForceLbf - StartStaticWheelFrictionForceLbf) / (4.5f - 0.5f);
                    SteamStaticWheelForce = LineGrad * absSpeedMpS + StartStaticWheelFrictionForceLbf;

                    SteamTangentialWheelForce = SpeedTangentialWheelTreadForceLbf;

                }

                SteamStaticWheelForce = MathHelper.Clamp(SteamStaticWheelForce, 0.1f, SteamStaticWheelForce);  // Ensure static wheelforce never goes negative - as this will induce wheel slip incorrectly

                // Test if wheel forces are high enough to induce a slip. Set slip flag if slip occuring 
                if (!IsLocoSlip)
                {
                    if (SteamTangentialWheelForce > SteamStaticWheelForce)
                    {
                        IsLocoSlip = true; 	// locomotive is slipping
                    }
                }
                else if (IsLocoSlip)
                {
                    if (SteamTangentialWheelForce < SteamStaticWheelForce)
                    {
                        IsLocoSlip = false; 	// locomotive is not slipping
                        PrevFrictionWheelSpeedMpS = 0.0f; // Reset previous wheel slip speed to zero
                        FrictionWheelSpeedMpS = 0.0f;
                    }
                }
                else
                {
                    IsLocoSlip = false; 	// locomotive is not slipping
                    PrevFrictionWheelSpeedMpS = 0.0f; // Reset previous wheel slip speed to zero
                    FrictionWheelSpeedMpS = 0.0f;
                }

                // If locomotive slip is occuring, set parameters to reduce motive force (pulling power), and set wheel rotational speed for wheel viewers
                if (IsLocoSlip)
                {

                    // This next section caluclates the turning speed for the wheel if slip occurs. It is based upon the force applied to the wheel and the moment of inertia for the wheel
                    // A Generic wheel profile is used, so results may not be applicable to all locomotive, but should provide a "reasonable" guestimation
                    // Generic wheel assumptions are - 80 inch drive wheels ( 2.032 metre), a pair of drive wheels weighs approx 6,000lbs, axle weighs 1,000 lbs, and has a diameter of 8 inches.
                    // Moment of Inertia (Wheel and axle) = (Mass x Radius) / 2.0
                    float WheelRadiusAssumptM = Me.FromIn(80.0f / 2.0f);
                    float WheelWeightKG = Kg.FromLb(6000.0f);
                    float AxleWeighKG = Kg.FromLb(1000.0f);
                    float AxleRadiusM = Me.FromIn(8.0f / 2.0f);
                    float WheelMomentInertia = (WheelWeightKG * WheelRadiusAssumptM * WheelRadiusAssumptM) / 2.0f;
                    float AxleMomentInertia = (WheelWeightKG * AxleRadiusM * AxleRadiusM) / 2.0f;
                    float TotalWheelMomentofInertia = WheelMomentInertia + AxleMomentInertia; // Total MoI for generic wheel

                    // The moment of inertia will be adjusted up or down compared to the size of the wheel on the player locomotive compared to the Generic wheel                
                    TotalWheelMomentofInertia *= DriverWheelRadiusM / WheelRadiusAssumptM;

                    // The moment of inertia needs to be increased by the number of wheel sets
                    TotalWheelMomentofInertia *= LocoNumDrvAxles;

                    // the inertia of the coupling rods can also be added
                    // Assume rods weigh approx 1500 lbs
                    // // MoI = rod weight x stroke radius (ie stroke / 2)
                    float RodWeightKG = Kg.FromLb(1500.0f);
                    // ???? For both compound and simple??????
                    float RodStrokeM = CylinderStrokeM / 2.0f;
                    float RodMomentInertia = RodWeightKG * RodStrokeM * RodStrokeM;

                    float TotalMomentInertia = TotalWheelMomentofInertia + RodMomentInertia;

                    // angular acceleration = (sum of forces * wheel radius) / moment of inertia
                    float AngAccRadpS2 = (N.FromLbf(SteamTangentialWheelForce - SteamStaticWheelForce) * DriverWheelRadiusM) / TotalMomentInertia;
                    // tangential acceleration = angular acceleration * wheel radius
                    // tangential speed = angular acceleration * time
                    PrevFrictionWheelSpeedMpS = FrictionWheelSpeedMpS; // Save current value of wheelspeed
                    // Speed = current velocity + acceleration * time
                    FrictionWheelSpeedMpS += (AngAccRadpS2 * DriverWheelRadiusM * elapsedClockSeconds);  // increase wheel speed whilever wheel accelerating
                    FrictionWheelSpeedMpS = MathHelper.Clamp(FrictionWheelSpeedMpS, 0.0f, 62.58f);  // Clamp wheel speed at maximum of 140mph (62.58 m/s)

                    WheelSlip = true;  // Set wheel slip if locomotive is slipping
                    WheelSpeedMpS = SpeedMpS;

                    if (FrictionWheelSpeedMpS > WheelSpeedMpS) // If slip speed is greater then normal forward speed use slip speed
                    {
                        WheelSpeedSlipMpS = (Direction == Direction.Forward ? 1 : -1) * FrictionWheelSpeedMpS;
                    }
                    else // use normal wheel speed
                    {
                        WheelSpeedSlipMpS = (Direction == Direction.Forward ? 1 : -1) * WheelSpeedMpS;
                    }

                    MotiveForceN *= Train.LocomotiveCoefficientFriction;  // Reduce locomotive tractive force to stop it moving forward
                }
                else
                {
                    WheelSlip = false;
                    WheelSpeedMpS = SpeedMpS;
                    WheelSpeedSlipMpS = SpeedMpS;
                }

#if DEBUG_STEAM_SLIP

                if (absSpeedMpS > 17.85 && absSpeedMpS < 17.9)  // only print debug @ 40mph
                {
                Trace.TraceInformation("========================== Debug Slip in MSTSSteamLocomotive.cs ==========================================");
                Trace.TraceInformation("Speed {0} Cutoff {1}", MpS.ToMpH(absSpeedMpS), cutoff);
                Trace.TraceInformation("==== Rotational Force ====");
                Trace.TraceInformation("Crank Pressure (speed): Left {0}  Middle {1}  Right {2}", CrankLeftCylinderPressure, CrankMiddleCylinderPressure, CrankRightCylinderPressure);
                Trace.TraceInformation("Cylinder Force (speed): Left {0}  Middle {1}  Right {2}", SpeedPistonForceLeftLbf, SpeedPistonForceMiddleLbf, SpeedPistonForceRightLbf);

                Trace.TraceInformation("Tang Factor (speed): Left {0}  Middle {1}  Right {2}", SpeedTangentialCrankForceFactorLeft, SpeedTangentialCrankForceFactorMiddle, SpeedTangentialCrankForceFactorRight);

                Trace.TraceInformation("Inertia Factor (speed) - Recip: Left {0}  Middle {1}  Right {2}", ReciprocatingInertiaFactorLeft, ReciprocatingInertiaFactorMiddle, ReciprocatingInertiaFactorRight);
                Trace.TraceInformation("Inertia Force (speed) - Recip: Left {0}  Middle {1}  Right {2}", ReciprocatingInertiaForceLeft, ReciprocatingInertiaForceMiddle, ReciprocatingInertiaForceRight);

                //        Trace.TraceInformation("Factor {0} Weight {1} Stroke {2}, Speed {3} Wheel {4}", ReciprocatingInertiaFactorLeft, ReciprocatingWeightLb, Me.ToIn(CylinderStrokeM), MpS.ToMpH(absSpeedMpS), Me.ToIn(DrvWheelDiaM));

                Trace.TraceInformation("Inertia Factor (speed) - ConRod: Left {0}  Middle {1}  Right {2}", ConnectRodInertiaFactorLeft, ConnectRodInertiaFactorMiddle, ConnectRodInertiaFactorRight);
                Trace.TraceInformation("Inertia Force (speed) - ConRod: Left {0}  Middle {1}  Right {2}", ConnectRodInertiaForceLeft, ConnectRodInertiaForceMiddle, ConnectRodInertiaForceRight);

                Trace.TraceInformation("Effective Total Force (speed): Left {0}  Middle {1}  Right {2}", SpeedTangentialCrankWheelForceLeftLbf, SpeedTangentialCrankWheelForceMiddleLbf, SpeedTangentialCrankWheelForceRightLbf);
                Trace.TraceInformation("Total Rotational Force (speed): Total {0}", SpeedTangentialWheelTreadForceLbf);

                Trace.TraceInformation("==== Adhesive Force ====");

                Trace.TraceInformation("ExcessBalance {0} Adhesive Wt {1}, Loco Friction {2}", ExcessBalanceWeightLb, Kg.ToLb(DrvWheelWeightKg), Train.LocomotiveCoefficientFriction);

                Trace.TraceInformation("Vert Thrust (speed): Left {0} Middle {1} Right {2}", SpeedVerticalThrustForceLeft, SpeedVerticalThrustForceMiddle, SpeedVerticalThrustForceRight);

                Trace.TraceInformation("Excess Balance (speed): Left {0} Middle {1} Right {2}", ExcessBalanceForceLeft, ExcessBalanceForceMiddle, ExcessBalanceForceRight);

                Trace.TraceInformation("Static Force (speed): {0}", SpeedStaticWheelFrictionForceLbf);
                }

#endif

            }
            else // Set wheel speed if "simple" friction is used
            {
                WheelSlip = false;
                WheelSpeedMpS = SpeedMpS;
                WheelSpeedSlipMpS = SpeedMpS;
            }

            #endregion

            // Derate when priming is occurring.
            if (BoilerIsPriming)
                MotiveForceN *= BoilerPrimingDeratingFactor;

            if (FusiblePlugIsBlown) // If fusible plug blows, then reduve motive force
            {
                MotiveForceN = 0.5f;
            }


            // Find the maximum TE for debug i.e. @ start and full throttle
            if (absSpeedMpS < 1.0)
            {
                if (Math.Abs(MotiveForceN) > absStartTractiveEffortN && Math.Abs(MotiveForceN) < MaxForceN)
                {
                    absStartTractiveEffortN = Math.Abs(MotiveForceN); // update to new maximum TE
                }
            }
        }

        private void UpdateAuxiliaries(float elapsedClockSeconds, float absSpeedMpS)
        {
            // Only calculate compressor consumption if it is not a vacuum controlled steam engine
            if (!(BrakeSystem is Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS.VacuumSinglePipe))
            {
                // Air brake system
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
            }
            else // Train is vacuum brake controlled, and steam ejector and vacuum pump are possibly used
            {

                /// <summary>
                /// Update small ejector vacuum rate and steam usage.
                /// </summary>
                /// 
                // Calculate small steam ejector steam usage
                SteamEjectorSmallSetting = SmallEjectorController.CurrentValue;
                SteamEjectorSmallPressurePSI = BoilerPressurePSI * SteamEjectorSmallSetting;
                // Steam consumption for small ejector is assumed to be @ 120 psi steam pressure, therefore pressure will vary up and down from this reference figure.
                float TempSteamPressure = SteamEjectorSmallPressurePSI / 120.0f;

                TempEjectorSmallSteamConsumptionLbpS = EjectorSmallSteamConsumptionLbpS * TempSteamPressure;
                // calculate small ejector fraction (maximum of the ratio of steam consumption for small and large ejector of train pipe charging rate - 
                //assumes consumption rates have been set correctly to relative sizes) to be used in vacuum brakes 
                // Ejector charging rate (vacuum output) will reach a maximum at the value of VacuumBrakesMinBoilerPressureMaxVacuum. Values of up to 
                // maximum output (1.0) are possible depending upon th steam setting of the small ejector. After maximum is reached the ejector output starts to decrease.
                // two straight line graphs are used to calculate rising and falling output either side of the maximum vacuum point. These straigh lines are based upon BR ejector test reports
                // Curves are lower - y = 1.7735x - 0.6122 and upper - y = -0.1179x + 1.1063
                if (SteamEjectorSmallPressurePSI < MaxVaccuumMaxPressurePSI)
                {
                    SmallEjectorFeedFraction = ((1.6122f * (SteamEjectorSmallPressurePSI / MaxVaccuumMaxPressurePSI) - 0.6122f)) * (EjectorSmallSteamConsumptionLbpS / (EjectorLargeSteamConsumptionLbpS + EjectorSmallSteamConsumptionLbpS));
                }
                else
                {
                    //  The fraction is dropped slightly as pressure increases to simulate decrease in vacuum evacuation as ejector pressure increases above the kneepoint of curve
                    SmallEjectorFeedFraction = ((1.1063f - (0.1063f * (SteamEjectorSmallPressurePSI / MaxVaccuumMaxPressurePSI)))) * EjectorSmallSteamConsumptionLbpS / (EjectorLargeSteamConsumptionLbpS + EjectorSmallSteamConsumptionLbpS);
                }

                SmallEjectorFeedFraction = MathHelper.Clamp(SmallEjectorFeedFraction, 0.0f, 1.0f); // Keep within bounds
                SmallEjectorBrakePipeChargingRatePSIorInHgpS = SmallEjectorFeedFraction * BrakePipeChargingRatePSIorInHgpS; // Rate used in the vacuum brakes

                // Calculate small steam ejector steam usage, when the small ejector turns on
                if (SmallSteamEjectorIsOn)
                {
                    TempEjectorSmallSteamConsumptionLbpS = EjectorSmallSteamConsumptionLbpS;
                }
                else
                {
                    TempEjectorSmallSteamConsumptionLbpS = 0.0f;
                }

                /// <summary>
                /// Update large ejector vacuum rate and steam usage.
                /// </summary>

                // Calculate Large steam ejector steam usage
                SteamEjectorLargeSetting = LargeEjectorController.CurrentValue;
                SteamEjectorLargePressurePSI = BoilerPressurePSI * SteamEjectorLargeSetting;
                // Steam consumption for large ejector is assumed to be @ 120 psi steam pressure, therefore pressure will vary up and down from this reference figure.
                float TempLargeSteamPressure = SteamEjectorLargePressurePSI / 120.0f;
                TempEjectorLargeSteamConsumptionLbpS = EjectorLargeSteamConsumptionLbpS * TempLargeSteamPressure;

                // Large ejector will suffer performance efficiency impacts if boiler steam pressure falls below max vacuum point.
                if (SteamEjectorLargeSetting == 0)
                {
                    LargeEjectorFeedFraction = 0;
                }
                else if (BoilerPressurePSI < MaxVaccuumMaxPressurePSI)
                {
                    LargeEjectorFeedFraction = ((1.6122f * (SteamEjectorLargePressurePSI / MaxVaccuumMaxPressurePSI) - 0.6122f)) * EjectorLargeSteamConsumptionLbpS / (EjectorLargeSteamConsumptionLbpS + EjectorSmallSteamConsumptionLbpS);
                }
                else
                {
                    //  The fraction is dropped slightly as pressure increases to simulate decrease in vacuum evacuation as ejector pressure increases above the kneepoint of curve
                    LargeEjectorFeedFraction = ((1.1063f - (0.1063f * (SteamEjectorLargePressurePSI / MaxVaccuumMaxPressurePSI)))) * EjectorLargeSteamConsumptionLbpS / (EjectorLargeSteamConsumptionLbpS + EjectorSmallSteamConsumptionLbpS);
                }

                // If simple brake controls chosen, then "automatically" set the large ejector value
                if (Simulator.Settings.SimpleControlPhysics || !LargeEjectorControllerFitted)
                {

                    //  Provided BP is greater then max vacuum pressure large ejector will operate at full efficiency
                    LargeEjectorFeedFraction = 1.0f * EjectorLargeSteamConsumptionLbpS / (EjectorLargeSteamConsumptionLbpS + EjectorSmallSteamConsumptionLbpS);

                }

                LargeEjectorFeedFraction = MathHelper.Clamp(LargeEjectorFeedFraction, 0.0f, 1.0f); // Keep within bounds
                LargeEjectorBrakePipeChargingRatePSIorInHgpS = LargeEjectorFeedFraction * BrakePipeChargingRatePSIorInHgpS;

                // Calculate large steam ejector steam usage, when the large ejector turns on
                if (LargeSteamEjectorIsOn)
                {
                    TempEjectorLargeSteamConsumptionLbpS = EjectorLargeSteamConsumptionLbpS;
                }
                else
                {
                    TempEjectorLargeSteamConsumptionLbpS = 0.0f;
                }
                // Calculate Total steamconsumption for Ejectors
                EjectorTotalSteamConsumptionLbpS = TempEjectorSmallSteamConsumptionLbpS + TempEjectorLargeSteamConsumptionLbpS;

                BoilerMassLB -= elapsedClockSeconds * EjectorTotalSteamConsumptionLbpS; // Reduce boiler mass to reflect steam usage by compressor
                BoilerHeatBTU -= elapsedClockSeconds * EjectorTotalSteamConsumptionLbpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor
                BoilerHeatOutBTUpS += EjectorTotalSteamConsumptionLbpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor
                TotalSteamUsageLBpS += EjectorTotalSteamConsumptionLbpS;

                if (VacuumPumpFitted)
                {
                    // Vacuum pump calculations
                    // Assume 5in dia vacuum pump. Forward and backward stroke evacuates air
                    VacuumPumpOutputFt3pM = Me3.ToFt3(Me3.FromIn3((Me.ToIn(CylinderStrokeM) * 2.5f * 2.5f))) * (float)Math.PI * 1.9f * pS.TopM(DrvWheelRevRpS);
                    VacuumPumpChargingRateInHgpS = (VacuumPumpOutputFt3pM / 138.0f) * 0.344f; // This is based upon a ratio from RM ejector - 0.344InHGpS to evacuate 138ft3 in a minute
                    if (AbsSpeedMpS < 0.1) // Stop vacuum pump if locomotive speed is nearly stationary - acts as a check to control elsewhere
                    {
                        VacuumPumpOperating = false;
                        VacuumPumpChargingRateInHgpS = 0.0f;
                    }
                }

            }

            // Calculate cylinder cock steam Usage if turned on
            // The cock steam usage will be assumed equivalent to a steam orifice
            // Steam Flow (lb/hr) = 24.24 x Press(Cylinder + Atmosphere(psi)) x CockDia^2 (in) - this needs to be multiplied by Num Cyls
            if (CylinderCocksAreOpen == true)
            {
                if (throttle > 0.00 && absSpeedMpS > 0.1) // if regulator open & train moving
                {
                    CylCockSteamUsageLBpS = pS.FrompH(NumCylinders * (24.24f * (CylinderCocksPressureAtmPSI) * CylCockDiaIN * CylCockDiaIN));
                    BoilerMassLB -= elapsedClockSeconds * CylCockSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by cylinder steam cocks  
                    BoilerHeatBTU -= elapsedClockSeconds * CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    BoilerHeatOutBTUpS += CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks                
                    CylCockBoilerHeatOutBTUpS = CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    TotalSteamUsageLBpS += CylCockSteamUsageLBpS;
                    CylCockSteamUsageDisplayLBpS = CylCockSteamUsageLBpS;
                }
                else if (throttle > 0.00 && absSpeedMpS <= 0.1) // if regulator open and train stationary
                {
                    CylCockSteamUsageLBpS = 0.0f; // set usage to zero if regulator closed
                    CylCockSteamUsageStatLBpS = pS.FrompH(NumCylinders * (24.24f * (Pressure_b_AtmPSI) * CylCockDiaIN * CylCockDiaIN));
                    BoilerMassLB -= elapsedClockSeconds * CylCockSteamUsageStatLBpS; // Reduce boiler mass to reflect steam usage by cylinder steam cocks  
                    BoilerHeatBTU -= elapsedClockSeconds * CylCockSteamUsageStatLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    BoilerHeatOutBTUpS += CylCockSteamUsageStatLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks                
                    CylCockBoilerHeatOutBTUpS = CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    TotalSteamUsageLBpS += CylCockSteamUsageStatLBpS;
                    CylCockSteamUsageDisplayLBpS = CylCockSteamUsageStatLBpS;
                }
            }
            else
            {
                CylCockSteamUsageLBpS = 0.0f;       // set steam usage to zero if turned off
                CylCockSteamUsageDisplayLBpS = CylCockSteamUsageLBpS;
            }

            // Calculate Generator steam Usage if turned on
            // Assume generator kW = 350W for D50 Class locomotive
            if (GeneratorSteamEffects) // If Generator steam effects not present then assume no generator is fitted to locomotive
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
                GeneratorSteamUsageLBpS = 0.0f; // No generator fitted to locomotive
            }
            if (StokerIsMechanical)
            {
                StokerSteamUsageLBpS = pS.FrompH(MaxBoilerOutputLBpH) * (StokerMinUsage + (StokerMaxUsage - StokerMinUsage) * FuelFeedRateKGpS / MaxFiringRateKGpS);  // Caluculate current steam usage based on fuel feed rates
                BoilerMassLB -= elapsedClockSeconds * StokerSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by mechanical stoker  
                BoilerHeatBTU -= elapsedClockSeconds * StokerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by mechanical stoker
                BoilerHeatOutBTUpS += StokerSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by mecahnical stoker
                TotalSteamUsageLBpS += StokerSteamUsageLBpS;
            }
            else
            {
                StokerSteamUsageLBpS = 0.0f;
            }
            // Other Aux device usage??
        }

        private void UpdateWaterGauge()
        {
            WaterGlassLevelIN = ((WaterFraction - WaterGlassMinLevel) / (WaterGlassMaxLevel - WaterGlassMinLevel)) * WaterGlassLengthIN;
            WaterGlassLevelIN = MathHelper.Clamp(WaterGlassLevelIN, 0, WaterGlassLengthIN);

            waterGlassPercent = (WaterFraction - WaterMinLevel) / (WaterMaxLevel - WaterMinLevel);
            waterGlassPercent = MathHelper.Clamp(waterGlassPercent, 0.0f, 1.0f);

            if (WaterFraction < WaterMinLevel)  // Blow fusible plugs if absolute boiler water drops below 70%
            {
                if (!FusiblePlugIsBlown)
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Water level dropped too far. Plug has fused and loco has failed."));
                FusiblePlugIsBlown = true; // if water level has dropped, then fusible plug will blow , see "water model"
            }
            // Check for priming            
            if (WaterFraction >= WaterMaxLevel) // Priming occurs if water level exceeds 91%
            {
                if (!BoilerIsPriming)
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Boiler overfull and priming."));
                BoilerIsPriming = true;
            }
            else if (WaterFraction < WaterMaxLevelSafe)
            {
                if (BoilerIsPriming)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Boiler no longer priming."));
                BoilerIsPriming = false;
            }
        }

        private void UpdateInjectors(float elapsedClockSeconds)
        {
            #region Calculate Injector size

            // Calculate size of injectors to suit cylinder size.
            InjCylEquivSizeIN = (NumCylinders / 2.0f) * Me.ToIn(CylinderDiameterM);

            // Based on equiv cyl size determine correct size injector
            if (InjCylEquivSizeIN <= 19.0 && (2.0f * (pS.TopH(pS.FrompM(Injector09FlowratePSItoUKGpM[MaxBoilerPressurePSI])) * WaterLBpUKG)) > MaxBoilerOutputLBpH)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector09FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 9mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector09FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 9mm Injector Flow rate 
                InjectorSize = 09.0f; // store size for display in HUD
            }
            else if (InjCylEquivSizeIN <= 24.0 && (2.0f * (pS.TopH(pS.FrompM(Injector10FlowratePSItoUKGpM[MaxBoilerPressurePSI])) * WaterLBpUKG)) > MaxBoilerOutputLBpH)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector10FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 10mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector10FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 10 mm Injector Flow rate 
                InjectorSize = 10.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 26.0 && (2.0f * (pS.TopH(pS.FrompM(Injector11FlowratePSItoUKGpM[MaxBoilerPressurePSI])) * WaterLBpUKG)) > MaxBoilerOutputLBpH)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector11FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 11mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector11FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 11 mm Injector Flow rate 
                InjectorSize = 11.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 28.0 && (2.0f * (pS.TopH(pS.FrompM(Injector13FlowratePSItoUKGpM[MaxBoilerPressurePSI])) * WaterLBpUKG)) > MaxBoilerOutputLBpH)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector13FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 13mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector13FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 13 mm Injector Flow rate 
                InjectorSize = 13.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 30.0 && (2.0f * (pS.TopH(pS.FrompM(Injector14FlowratePSItoUKGpM[MaxBoilerPressurePSI])) * WaterLBpUKG)) > MaxBoilerOutputLBpH)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector14FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 14mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector14FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 14 mm Injector Flow rate 
                InjectorSize = 14.0f; // store size for display in HUD                
            }
            else
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector15FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 15mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector15FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 15 mm Injector Flow rate 
                InjectorSize = 15.0f; // store size for display in HUD                
            }
            #endregion

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
                    MaxInject1SteamUsedLbpS = InjWaterFedSteamPressureFtoPSI[BoilerPressurePSI];  // Maximum amount of steam used at actual boiler pressure
                    ActInject1SteamUsedLbpS = (Injector1Fraction * InjectorFlowRateLBpS) / MaxInject1SteamUsedLbpS; // Lbs of steam injected into boiler to inject water.

                    // Calculate heat loss for steam injection
                    Inject1SteamHeatLossBTU = ActInject1SteamUsedLbpS * (BoilerSteamHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector1WaterTempPressurePSI]); // Calculate heat loss for injection steam, ie steam heat to water delivery temperature

                    // Calculate heat loss for water injected
                    // Loss of boiler heat due to water injection - loss is the diff between steam and water Heat
                    Inject1WaterHeatLossBTU = Injector1Fraction * InjectorFlowRateLBpS * (BoilerWaterHeatBTUpLB - WaterHeatPSItoBTUpLB[Injector1WaterTempPressurePSI]);

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

            // Update injector lockout timer
            if (Injector1IsOn || Injector2IsOn)
            {
                if (InjectorLockedOut)
                {
                    InjectorLockOutTimeS += elapsedClockSeconds;
                }
                if (InjectorLockOutTimeS > InjectorLockOutResetTimeS)
                {
                    InjectorLockedOut = false;
                    InjectorLockOutTimeS = 0.0f;

                }
            }
        }

        private void UpdateFiring(float absSpeedMpS)
        {

            if (FiringIsManual)

            #region Manual Fireman
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

                // Damper - need to be calculated in AI fireman case too, to determine smoke color
                if (absSpeedMpS < 1.0f)    // locomotive is stationary then damper will have no effect
                {
                    DamperBurnEffect = 0.0f;
                }
                else
                {
                    // The damper burn effect is created by the cylinder exhaust steam, and the opening state of the damper. 1.2 included as a small increase to compensate for calculation losses
                    DamperBurnEffect = DamperController.CurrentValue * CylinderSteamUsageLBpS * 1.2f;
                }
                DamperBurnEffect = MathHelper.Clamp(DamperBurnEffect, 0.0f, TheoreticalMaxSteamOutputLBpS * 1.5f); // set damper maximum to the max generation rate
            }
            #endregion

            else

            #region AI Fireman
            {
                // Injectors
                // Injectors normally not on when stationary?
                // Injector water delivery heat decreases with the capacity of the injectors, ideally one injector would be used as appropriate to match steam consumption. @nd one only used if required.
                if (WaterGlassLevelIN > 7.99)        // turn injectors off if water level in boiler greater then 8.0, to stop cycling
                {
                    Injector1IsOn = false;
                    Injector1Fraction = 0.0f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    StopInjector1Sound();
                    StopInjector2Sound();
                }
                else if (WaterGlassLevelIN <= 7.0 && WaterGlassLevelIN > 6.875 && !InjectorLockedOut)  // turn injector 1 on 20% if water level in boiler drops below 7.0
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.1f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.875 && WaterGlassLevelIN > 6.75 && !InjectorLockedOut)  // turn injector 1 on 20% if water level in boiler drops below 7.0
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.2f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.75 && WaterGlassLevelIN > 6.675 && !InjectorLockedOut)  // turn injector 1 on 20% if water level in boiler drops below 7.0
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.3f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.675 && WaterGlassLevelIN > 6.5 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.4f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.5 && WaterGlassLevelIN > 6.375 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.5f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.375 && WaterGlassLevelIN > 6.25 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.6f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.25 && WaterGlassLevelIN > 6.125 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.7f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.125 && WaterGlassLevelIN > 6.0 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.8f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 6.0 && WaterGlassLevelIN > 5.875 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 0.9f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (WaterGlassLevelIN <= 5.875 && WaterGlassLevelIN > 5.75 && !InjectorLockedOut)
                {
                    Injector1IsOn = true;
                    Injector1Fraction = 1.0f;
                    Injector2IsOn = false;
                    Injector2Fraction = 0.0f;
                    InjectorLockedOut = true;
                    PlayInjector1SoundIfStarting();
                }
                else if (BoilerPressurePSI > (MaxBoilerPressurePSI - 100.0))  // If boiler pressure is not too low then turn on injector 2
                {
                    if (WaterGlassLevelIN <= 5.75 && WaterGlassLevelIN > 5.675 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.1f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.675 && WaterGlassLevelIN > 5.5 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.2f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.5 && WaterGlassLevelIN > 5.325 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.3f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.325 && WaterGlassLevelIN > 5.25 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.4f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.25 && WaterGlassLevelIN > 5.125 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.5f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.125 && WaterGlassLevelIN > 5.0 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.6f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 5.0 && WaterGlassLevelIN > 4.875 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.7f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 4.875 && WaterGlassLevelIN > 4.75 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.8f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 4.75 && WaterGlassLevelIN > 4.625 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 0.9f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                    else if (WaterGlassLevelIN <= 4.625 && WaterGlassLevelIN > 4.5 && !InjectorLockedOut)
                    {
                        Injector2IsOn = true;
                        Injector2Fraction = 1.0f;
                        InjectorLockedOut = true;
                        PlayInjector2SoundIfStarting();
                    }
                }

                float BoilerHeatCheck = BoilerHeatOutBTUpS / BoilerHeatInBTUpS;
                BoilerHeatExcess = BoilerHeatBTU / MaxBoilerHeatBTU;

                // Determine if AI fireman should shovel coal despite the fact that boiler heat has exceeded max boiler heat - provides a crude "look ahead" capability. 
                // Example - boiler heat excessive, and train faces heavy climb up grade, fire burn rate still needs to increase, despite the fact that AI code normally will try and reduce burn rate.
                if (throttle > 0.98 && CurrentElevationPercent < -0.3 && BoilerHeatCheck > 1.25 && BoilerHeatExcess < 1.07)
                {
                    ShovelAnyway = true; // Fireman should be increasing fire burn rate despite high boiler heat
                }
                else
                {
                    ShovelAnyway = false; // Fireman should not be increasing fire burn rate if high boiler heat
                }

                // HeatRatio - provides a multiplication factor which attempts to increase the firing rate of the locomotive in AI Firing if the boiler Heat
                // drops below the heat in the boiler when the boiler is at normal operating pressure

                // Determine Heat Ratio - for calculating burn rate
                if (FullBoilerHeat && !ShovelAnyway) // If heat in boiler is going too high, ie has exceeded maximum heat
                {
                    HeatRatio = 1.0f;

                }
                else  // If heat in boiler is normal or low
                {
                    // Use a straight line correlation to vary the HeatRatio as the heat fluctuates compared to the max heat required in the boiler
                    float HeatRatioMaxRise = 5.0f;
                    float HeatRatioMaxRun = 10.0f; // 10% of boilerheat
                    float HeatRatioGrad = HeatRatioMaxRise / HeatRatioMaxRun;
                    if (BoilerHeatBTU > (MaxBoilerHeatBTU) && BoilerHeatBTU <= MaxBoilerHeatBTU || (BoilerHeatInBTUpS > (PreviousBoilerHeatOutBTUpS * 1.05f))) // If boiler heat within 0.5% of max then set HeatRatio to 1.0
                    {
                        HeatRatio = 1.0f; // if boiler pressure close to normal set pressure ratio to normal
                    }
                    else if (BoilerHeatBTU > (MaxBoilerHeatBTU * 0.90f) && BoilerHeatBTU <= (MaxBoilerHeatBTU)) // If boiler heat between 90 and 98% of Max then set to variable value
                    {
                        HeatRatio = (HeatRatioGrad * ((MaxBoilerHeatBTU - BoilerHeatBTU) / MaxBoilerHeatBTU) * 100.0f) + 1.0f;

                    }
                    else if (BoilerHeatBTU <= (MaxBoilerHeatBTU * 0.90f))
                    {
                        HeatRatio = HeatRatioMaxRise + 1.0f;
                    }
                    else if (BoilerHeatBTU > MaxBoilerHeatBTU)
                    {
                        HeatRatio = 1.0f;
                    }
                    else
                    {
                        HeatRatio = 1.0f;
                    }
                    HeatRatio = MathHelper.Clamp(HeatRatio, 0.001f, (HeatRatioMaxRise + 1.0f)); // Boiler pressure ratio to adjust burn rate
                }
            }
            #endregion
        }

        /// <summary>
        /// Turn on the injector 1 sound only when the injector starts.
        /// </summary>
        private void PlayInjector1SoundIfStarting()
        {
            if (!Injector1SoundIsOn)
            {
                Injector1SoundIsOn = true;
                SignalEvent(Event.WaterInjector1On);
            }
        }

        /// <summary>
        /// Turn on the injector 2 sound only when the injector starts.
        /// </summary>
        private void PlayInjector2SoundIfStarting()
        {
            if (!Injector2SoundIsOn)
            {
                Injector2SoundIsOn = true;
                SignalEvent(Event.WaterInjector2On);
            }
        }

        /// <summary>
        /// Turn off the injector 1 sound only when the injector stops.
        /// </summary>
        private void StopInjector1Sound()
        {
            if (Injector1SoundIsOn)
            {
                Injector1SoundIsOn = false;
                SignalEvent(Event.WaterInjector1Off);
            }
        }

        /// <summary>
        /// Turn off the injector 2 sound only when the injector stops.
        /// </summary>
        private void StopInjector2Sound()
        {
            if (Injector2SoundIsOn)
            {
                Injector2SoundIsOn = false;
                SignalEvent(Event.WaterInjector2Off);
            }
        }


        protected override void UpdateCarSteamHeat(float elapsedClockSeconds)
        {
            // Update Steam Heating System

            if (IsSteamHeatFitted && this.IsLeadLocomotive())  // Only Update steam heating if train and locomotive fitted with steam heating, and is a passenger train
            {
                CurrentSteamHeatPressurePSI = SteamHeatController.CurrentValue * MaxSteamHeatPressurePSI;

                if (CurrentSteamHeatPressurePSI > BoilerPressurePSI)
                {
                    CurrentSteamHeatPressurePSI = BoilerPressurePSI; // If boiler pressure drops, then make sure that steam heating pressure cannot be greater then boiler pressure.
                }

                // TO DO - Add test to see if cars are coupled, if Light Engine, disable steam heating.

                if (CurrentSteamHeatPressurePSI > 0.1)  // Only Update steam heating if train and locomotive fitted with steam heating, and is a passenger train
                {
                    Train.CarSteamHeatOn = true; // turn on steam effects on wagons

                    // Calculate impact of steam heat usage on locomotive
                    BoilerMassLB -= elapsedClockSeconds * CalculatedCarHeaterSteamUsageLBpS;
                    BoilerHeatBTU -= elapsedClockSeconds * CalculatedCarHeaterSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to steam heat usage
                    TotalSteamUsageLBpS += CalculatedCarHeaterSteamUsageLBpS;
                    BoilerHeatOutBTUpS += CalculatedCarHeaterSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve                

                }
                else
                {
                    Train.CarSteamHeatOn = false; // turn off steam effects on wagons
                }
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
                    data = waterGlassPercent; // Shows the level in the water glass
                    break;
                case CABViewControlTypes.TENDER_WATER:
                    data = CombinedTenderWaterVolumeUKG; // Looks like default locomotives need an absolute UK gallons value
                    break;
                case CABViewControlTypes.STEAM_PR:
                    data = ConvertFromPSI(cvc, BoilerPressurePSI);
                    break;
                case CABViewControlTypes.STEAMCHEST_PR:
                    data = ConvertFromPSI(cvc, SteamChestPressurePSI);
                    break;
                case CABViewControlTypes.STEAMHEAT_PRESSURE:
                    data = ConvertFromPSI(cvc, CurrentSteamHeatPressurePSI);
                    break;
                case CABViewControlTypes.CUTOFF:
                case CABViewControlTypes.REVERSER_PLATE:
                    data = Train.MUReverserPercent / 100f;
                    break;
                case CABViewControlTypes.CYL_COCKS:
                    data = CylinderCocksAreOpen ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_CYL_COMP:
                    data = CylinderCompoundOn ? 1 : 0;
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
                case CABViewControlTypes.ORTS_BLOWDOWN_VALVE:
                    data = BlowdownValveOpen ? 1 : 0;
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
                case CABViewControlTypes.SMALL_EJECTOR:
                    {
                        data = SmallEjectorController.CurrentValue;
                        break;
                    }
               case CABViewControlTypes.ORTS_LARGE_EJECTOR:
                    {
                        data = LargeEjectorController.CurrentValue;
                        break;
                    }
                case CABViewControlTypes.FUEL_GAUGE:
                    if (cvc.Units == CABViewControlUnits.LBS)
                        data = Kg.ToLb(TenderCoalMassKG);
                    else
                        data = TenderCoalMassKG;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }
            return data;
        }

        public override string GetStatus()
        {
            // Set variable to change text colour as appropriate to flag different degrees of warning
            var boilerPressurePercent = BoilerPressurePSI / MaxBoilerPressurePSI;
            var boilerPressureSafety = "";
            if (FiringIsManual || AIFireOverride)
            {
                boilerPressureSafety = boilerPressurePercent <= 0.25 || boilerPressurePercent > 1.0 ? "!!!" : boilerPressurePercent <= 0.5 || boilerPressurePercent > 0.985 ? "???" : "";
            }
            else
            {
                boilerPressureSafety = boilerPressurePercent <= 0.25 ? "!!!" : boilerPressurePercent <= 0.5 ? "???" : "";
            }
            var boilerWaterSafety = WaterFraction < WaterMinLevel || WaterFraction > WaterMaxLevel ? "!!!" : WaterFraction < WaterMinLevelSafe || WaterFraction > WaterMaxLevelSafe ? "???" : "";
            var coalPercent = TenderCoalMassKG / MaxTenderCoalMassKG;
            var waterPercent = CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;
            var fuelSafety = CoalIsExhausted || WaterIsExhausted ? "!!!" : coalPercent <= 0.105 || waterPercent <= 0.105 ? "???" : "";
            var steamusagesafety = PreviousTotalSteamUsageLBpS > EvaporationLBpS ? "!!!" : PreviousTotalSteamUsageLBpS > EvaporationLBpS * 0.95f ? "???" : "";

            var status = new StringBuilder();

            if (IsFixGeared)
                status.AppendFormat("{0} = 1 ({1:F2})\n", Simulator.Catalog.GetString("Fixed gear"), SteamGearRatio);
            else if (IsSelectGeared)
                status.AppendFormat("{0} = {2} ({1:F2})\n", Simulator.Catalog.GetString("Gear"),
                    SteamGearRatio, SteamGearPosition == 0 ? Simulator.Catalog.GetParticularString("Gear", "N") : SteamGearPosition.ToString());
            status.AppendFormat("{0}{2} = {1}/{3}{2}\n", Simulator.Catalog.GetString("Steam usage"), FormatStrings.FormatMass(pS.TopH(Kg.FromLb(PreviousTotalSteamUsageLBpS)), MainPressureUnit != PressureUnit.PSI), steamusagesafety, FormatStrings.h);
            status.AppendFormat("{0}{2} = {1}{2}\n", Simulator.Catalog.GetString("Boiler pressure"), FormatStrings.FormatPressure(BoilerPressurePSI, PressureUnit.PSI, MainPressureUnit, true), boilerPressureSafety);
            status.AppendFormat("{0}{2} = {1:F0}% {3}{2}\n", Simulator.Catalog.GetString("Boiler water glass"), 100 * waterGlassPercent, boilerWaterSafety, FiringIsManual ? Simulator.Catalog.GetString("(safe range)") : "");

            if (FiringIsManual)
            {
                status.AppendFormat("{0}{3} = {2:F0}% {1}{3}\n", Simulator.Catalog.GetString("Boiler water level"), Simulator.Catalog.GetString("(absolute)"), WaterFraction * 100, boilerWaterSafety);
                if (IdealFireMassKG > 0)
                    status.AppendFormat("{0} = {1:F0}%\n", Simulator.Catalog.GetString("Fire mass"), FireMassKG / IdealFireMassKG * 100);
                else
                    status.AppendFormat("{0} = {1:F0}%\n", Simulator.Catalog.GetString("Fire Heat Loss"), FireHeatLossPercent * 100);
            }

            status.AppendFormat("{0}{5} = {3:F0}% {1}, {4:F0}% {2}{5}\n", Simulator.Catalog.GetString("Fuel levels"), Simulator.Catalog.GetString("coal"), Simulator.Catalog.GetString("water"), 100 * coalPercent, 100 * waterPercent, fuelSafety);

            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());

            status.AppendFormat("\n\n\t\t === {0} === \t\t\n", Simulator.Catalog.GetString("Key Inputs"));

            status.AppendFormat("{0}\t\t{1}\n", Simulator.Catalog.GetString("Locomotive Type:"),
                SteamLocoType);

            status.AppendFormat("{0}\t{1}\t{6}\t{2}\t{7}\t{3}\t{8}\t{4}\t{9}\t{5}\t{10}\n",
                Simulator.Catalog.GetString("Input:"),
                Simulator.Catalog.GetString("Evap"),
                Simulator.Catalog.GetString("Grate"),
                Simulator.Catalog.GetString("Boiler"),
                Simulator.Catalog.GetString("SuperHr"),
                Simulator.Catalog.GetString("FuelCal"),
                FormatStrings.FormatArea(EvaporationAreaM2, IsMetric),
                FormatStrings.FormatArea(GrateAreaM2, IsMetric),
                FormatStrings.FormatVolume(Me3.FromFt3(BoilerVolumeFT3), IsMetric),
                FormatStrings.FormatArea(SuperheatAreaM2, IsMetric),
                FormatStrings.FormatEnergyDensityByMass(FuelCalorificKJpKG, IsMetric));

            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N3}\n",
                Simulator.Catalog.GetString("Adj:"),
                Simulator.Catalog.GetString("CylEff"),
                CylinderEfficiencyRate,
                Simulator.Catalog.GetString("PortOpen"),
                CylinderPortOpeningFactor);

            status.AppendFormat("\n\t\t === {0} === \t\t{1}/{2}\n",
                Simulator.Catalog.GetString("Steam Production"),
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(EvaporationLBpS)), IsMetric),
                FormatStrings.h);

            status.AppendFormat("{0}\t{1}\t{5}\t{2}\t{6}\t{3}\t{7}/{8}\t\t{4}\t{9:N2}\n",
                Simulator.Catalog.GetString("Boiler:"),
                Simulator.Catalog.GetParticularString("HUD", "Power"),
                Simulator.Catalog.GetString("Mass"),
                Simulator.Catalog.GetString("MaxOutp"),
                Simulator.Catalog.GetString("BoilerEff"),
                FormatStrings.FormatPower(W.FromKW(BoilerKW), IsMetric, true, false),
                FormatStrings.FormatMass(Kg.FromLb(BoilerMassLB), IsMetric),
                FormatStrings.FormatMass(Kg.FromLb(MaxBoilerOutputLBpH), IsMetric),
                FormatStrings.h,
                BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateSmoothedKGpS)) / Me2.ToFt2(GrateAreaM2))]);

            status.AppendFormat("{0}\t{1}\t{2}\t\t{3}\t{4}\t\t{5}\t{6}\t\t{7}\t{8}\t\t{9}\t{10}\t\t{11}\t{12}\t\t{13}\t{14}\n",
                Simulator.Catalog.GetString("Heat:"),
                Simulator.Catalog.GetString("In"),
                FormatStrings.FormatPower(W.FromBTUpS(BoilerHeatInBTUpS), IsMetric, false, true),
                Simulator.Catalog.GetString("Out"),
                FormatStrings.FormatPower(W.FromBTUpS(PreviousBoilerHeatOutBTUpS), IsMetric, false, true),
                Simulator.Catalog.GetString("Rad"),
                FormatStrings.FormatPower(W.FromBTUpS(pS.FrompH(BoilerHeatRadiationLossBTU)), IsMetric, false, true),
                Simulator.Catalog.GetString("Stored"),
                FormatStrings.FormatEnergy(W.FromBTUpS(BoilerHeatSmoothedBTU), IsMetric),
                Simulator.Catalog.GetString("Max"),
                FormatStrings.FormatEnergy(W.FromBTUpS(MaxBoilerHeatBTU), IsMetric),
                Simulator.Catalog.GetString("Safety"),
                FormatStrings.FormatEnergy(W.FromBTUpS(MaxBoilerSafetyPressHeatBTU), IsMetric),
                Simulator.Catalog.GetString("Raw"),
                FormatStrings.FormatEnergy(W.FromBTUpS(BoilerHeatBTU), IsMetric)
                );

            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                Simulator.Catalog.GetString("Temp:"),
                Simulator.Catalog.GetString("Flue"),
                FormatStrings.FormatTemperature(C.FromK(FlueTempK), IsMetric, false),
                Simulator.Catalog.GetString("Water"),
                FormatStrings.FormatTemperature(C.FromK(BoilerWaterTempK), IsMetric, false),
                Simulator.Catalog.GetString("MaxSupH"),
                FormatStrings.FormatTemperature(C.FromF(MaxSuperheatRefTempF), IsMetric, false),
                Simulator.Catalog.GetString("CurSupH"),
                FormatStrings.FormatTemperature(C.FromF(CurrentSuperheatTempF), IsMetric, false));

            status.AppendFormat("\n\t\t === {0} === \t\t{1}/{2}\n",
                Simulator.Catalog.GetString("Steam Usage"),
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(PreviousTotalSteamUsageLBpS)), IsMetric),
                FormatStrings.h);

            if (!(BrakeSystem is Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS.VacuumSinglePipe))
            {
                // Display air compressor information
                status.AppendFormat("{0}\t{1}\t{2}/{21}\t{3}\t{4}/{21}\t{5}\t{6}/{21}\t{7}\t{8}/{21}\t{9}\t{10}/{21}\t{11}\t{12}/{21}\t{13}\t{14}/{21}\t{15}\t{16}/{21}\t{17}\t{18}/{21} ({19}x{20:N1}\")\n",
                    Simulator.Catalog.GetString("Usage:"),
                    Simulator.Catalog.GetString("Cyl"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylinderSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Blower"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowerSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Comprsr"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CompSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("SafetyV"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(SafetyValveUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("CylCock"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylCockSteamUsageDisplayLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Genertr"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(GeneratorSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Stoker"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(StokerSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("BlowD"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowdownSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("MaxSafe"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(MaxSafetyValveDischargeLbspS)), IsMetric),
                    NumSafetyValves,
                    SafetyValveSizeIn,
                    FormatStrings.h);
            }
            else
            {
                // Display steam ejector information instead of air compressor
                status.AppendFormat("{0}\t{1}\t{2}/{21}\t{3}\t{4}/{21}\t{5}\t{6}/{21}\t{7}\t{8}/{21}\t{9}\t{10}/{21}\t{11}\t{12}/{21}\t{13}\t{14}/{21}\t{15}\t{16}/{21}\t{17}\t{18}/{21} ({19}x{20:N1}\")\n",
                    Simulator.Catalog.GetString("Usage:"),
                    Simulator.Catalog.GetString("Cyl"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylinderSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Blower"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowerSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Ejector"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(EjectorTotalSteamConsumptionLbpS)), IsMetric),
                    Simulator.Catalog.GetString("SafetyV"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(SafetyValveUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("CylCock"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylCockSteamUsageDisplayLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Genertr"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(GeneratorSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("Stoker"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(StokerSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("BlowD"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowdownSteamUsageLBpS)), IsMetric),
                    Simulator.Catalog.GetString("MaxSafe"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(MaxSafetyValveDischargeLbspS)), IsMetric),
                    NumSafetyValves,
                    SafetyValveSizeIn,
                    FormatStrings.h);
            }


#if DEBUG_STEAM_CYLINDER_EVENTS

            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8:N2}\t{9}\t{10:N2}\t{11}\t{12:N2}\n",
            Simulator.Catalog.GetString("CylValve:"),
            Simulator.Catalog.GetString("Travel"),
            ValveTravel,
            Simulator.Catalog.GetString("Cutoff"),
            cutoff * 100,
            Simulator.Catalog.GetString("Lead"),
            ValveLead,
            Simulator.Catalog.GetString("ExhLap"),
            ValveExhLap,
            Simulator.Catalog.GetString("StLap"),
            ValveSteamLap,
            Simulator.Catalog.GetString("AdvAng"),
            ValveAdvanceAngleDeg
              );
            
#endif


            // Display steam cylinder events
            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8:N3}\n",
            Simulator.Catalog.GetString("CylEvts:"),
            Simulator.Catalog.GetString("Cutoff"),
            cutoff * 100,
            Simulator.Catalog.GetString("CylExh"),
            CylinderExhaustOpenFactor * 100,
            Simulator.Catalog.GetString("CylComp"),
            CylinderCompressionCloseFactor * 100,
            Simulator.Catalog.GetString("CyAdmis"),
            CylinderAdmissionOpenFactor * 100
                             );

            if (SteamEngineType == SteamEngineTypes.Compound)  // Display Steam Indicator Information for compound locomotive
            {

                // Display steam indicator pressures in HP cylinder
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\n",
                Simulator.Catalog.GetString("PressHP:"),
                Simulator.Catalog.GetString("Chest"),
                FormatStrings.FormatPressure(LogSteamChestPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Initial"),
                FormatStrings.FormatPressure(LogInitialPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Cutoff"),
                FormatStrings.FormatPressure(LogCutoffPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Rel"),
                FormatStrings.FormatPressure(LogReleasePressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Back"),
                FormatStrings.FormatPressure(LogBackPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("MEP"),
                FormatStrings.FormatPressure(HPCylinderMEPPSI, PressureUnit.PSI, MainPressureUnit, true)
                );

                // Display steam indicator pressures in LP cylinder
                status.AppendFormat("{0}\t\t\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\n",
                Simulator.Catalog.GetString("PressLP:"),
                Simulator.Catalog.GetString("Initial"),
                FormatStrings.FormatPressure(LogLPInitialPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Cutoff"),
                FormatStrings.FormatPressure(LogLPCutoffPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Rel"),
                FormatStrings.FormatPressure(LogLPReleasePressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Back"),
                FormatStrings.FormatPressure(LogLPBackPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("MEP"),
                FormatStrings.FormatPressure(LPCylinderMEPPSI, PressureUnit.PSI, MainPressureUnit, true)
                );

            }
            else  // Display Steam Indicator Information for single expansion locomotive
            {

                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\n",
                Simulator.Catalog.GetString("Press:"),
                    Simulator.Catalog.GetString("Chest"),
                    FormatStrings.FormatPressure(LogSteamChestPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("Initial"),
                    FormatStrings.FormatPressure(LogInitialPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("Cutoff"),
                    FormatStrings.FormatPressure(LogCutoffPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("Rel"),
                    FormatStrings.FormatPressure(LogReleasePressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("Back"),
                    FormatStrings.FormatPressure(LogBackPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("MEP"),
                    FormatStrings.FormatPressure(MeanEffectivePressurePSI, PressureUnit.PSI, MainPressureUnit, true)
                    );
            }

            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                Simulator.Catalog.GetString("Status:"),
                Simulator.Catalog.GetString("Safety"),
                SafetyIsOn ? Simulator.Catalog.GetString("Open") : Simulator.Catalog.GetString("Closed"),
                Simulator.Catalog.GetString("Plug"),
                FusiblePlugIsBlown ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("Prime"),
                BoilerIsPriming ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("Comp"),
                CylinderCompoundOn ? Simulator.Catalog.GetString("Off") : Simulator.Catalog.GetString("On")
                );

#if DEBUG_LOCO_STEAM_USAGE
            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\t{13}\t{14:N2}\t{15}\t{16:N2}\t{17}\t{18:N2}\t{19}\t{20:N2}\t{21}\t{22}\n",
                "DbgUse:",
                "SwpVol",
                CylinderSweptVolumeFT3pFT,
                "CutoffVol",
                CylinderReleaseSteamVolumeFt3,
                "CompVol",
                CylinderAdmissionSteamVolumeFt3,
                "RawSt",
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(RawCalculatedCylinderSteamUsageLBpS)), IsMetric),
                "CalcSt",
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCylinderSteamUsageLBpS)), IsMetric),
                "ClrWt",
                CylinderAdmissionSteamWeightLbs,
                "CutWt",
                CylinderReleaseSteamWeightLbs,
                "TotWt",
                RawCylinderSteamWeightLbs,
                "SupFact",
                SuperheaterSteamUsageFactor,
                "CondFact",
                CylinderCondensationFactor,
                "SuperSet",
                IsSuperSet);
#endif

            if (IsSteamHeatFitted && Train.PassengerCarsNumber > 0 && this.IsLeadLocomotive() && Train.CarSteamHeatOn)
            {
                // Only show steam heating HUD if fitted to locomotive and the train, has passenger cars attached, and is the lead locomotive, and steam heat valve is on.
                // Display Steam Heat info
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}/{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16:N0}\n",
                   Simulator.Catalog.GetString("StHeat:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(CurrentSteamHeatPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("StTemp"),
                   FormatStrings.FormatTemperature(C.FromF(SteamHeatPressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]), IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
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

            status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Fireman"));
            status.AppendFormat("{0}\t{1}\t{2}\t\t{3}\t{4}\t\t{5}\t{6:N0}/{13}\t\t{7}\t{8:N0}/{13}\t\t{9}\t{10:N0}/{13}\t\t{11}\t{12}/{14}{13}\t{15}\t{16}/{18}{17}\t\t{19}\t{20:N0}\n",
                Simulator.Catalog.GetString("Fire:"),
                Simulator.Catalog.GetString("Ideal"),
                FormatStrings.FormatMass(IdealFireMassKG, IsMetric),
                Simulator.Catalog.GetString("Actual"),
                FormatStrings.FormatMass(FireMassKG, IsMetric),
                Simulator.Catalog.GetString("MaxFireR"),
                FormatStrings.FormatMass(pS.TopH(DisplayMaxFiringRateKGpS), IsMetric),
                Simulator.Catalog.GetString("FeedRate"),
                FormatStrings.FormatMass(pS.TopH(FuelFeedRateKGpS), IsMetric),
                Simulator.Catalog.GetString("BurnRate"),
                FormatStrings.FormatMass(pS.TopH(FuelBurnRateSmoothedKGpS), IsMetric),
                Simulator.Catalog.GetString("Combust"),
                FormatStrings.FormatMass(Kg.FromLb(GrateCombustionRateLBpFt2), IsMetric),
                FormatStrings.h,
                IsMetric ? FormatStrings.m2 : FormatStrings.ft2,
                Simulator.Catalog.GetString("GrLimit"),
                FormatStrings.FormatMass(Kg.FromLb(GrateLimitLBpFt2), IsMetric),
                FormatStrings.h,
                IsMetric ? FormatStrings.m2 : FormatStrings.ft2,
                Simulator.Catalog.GetString("MaxBurn"),
                MaxFiringRateLbpH
                );

#if DEBUG_LOCO_BURN_AI
            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8:N2}\t{9}\t{10:N2}\t{11}\t{12:N0}\t{13}\t{14:N0}\t{15}\t{16:N0}\t{17}\t{18:N0}\t{19}\t{20:N0}\t{21}\t{22}\t{23}\t{24}\t{25}\t{26}\n {27}\t{28}\t{29:N3}\n",
                "DbgBurn1:",
                "BoilHeat",
                FullBoilerHeat ? "Yes" : "No",
                "H/R",
                HeatRatio,
                "BoilH/R",
                FullBoilerHeatRatio,
                "MBoilH/R",
                MaxBoilerHeatRatio,
                "PrRatio",
                PressureRatio,
                "FireHeat",
                FireHeatTxfKW,
                "RawBurn",
                pS.TopH(Kg.ToLb( BurnRateRawKGpS)),
                "RawHeat",
                BoilerHeatBTU,
                "SuperSet",
                IsSuperSet,
                "GratLmt",
                GrateLimitLBpFt2,
                "MaxFuel",
                pS.TopH(Kg.ToLb( MaxFuelBurnGrateKGpS)),
                "BstReset",
                FuelBoostReset ? "Yes" : "No",
                "ShAny",
                ShovelAnyway,
                "DbgBurn2:",
                "FHLoss",
                FireHeatLossPercent);
#endif

            status.AppendFormat("{0}\t{1}\t{6}/{12}\t\t({7:N0} {13})\t{2}\t{8}/{12}\t\t{3}\t{9}\t\t{4}\t{10}/{12}\t\t{5}\t{11}\n",
                Simulator.Catalog.GetString("Injector:"),
                Simulator.Catalog.GetString("Max"),
                Simulator.Catalog.GetString("Inj1"),
                Simulator.Catalog.GetString("Temp1"),
                Simulator.Catalog.GetString("Inj2"),
                Simulator.Catalog.GetString("Temp2"),
                FormatStrings.FormatFuelVolume(pS.TopH(L.FromGUK(MaxInjectorFlowRateLBpS / WaterLBpUKG)), IsMetric, IsUK),
                InjectorSize,
                FormatStrings.FormatFuelVolume(Injector1Fraction * pS.TopH(L.FromGUK(InjectorFlowRateLBpS / WaterLBpUKG)), IsMetric, IsUK),
                FormatStrings.FormatTemperature(C.FromF(Injector1WaterDelTempF), IsMetric, false),
                FormatStrings.FormatFuelVolume(Injector2Fraction * pS.TopH(L.FromGUK(InjectorFlowRateLBpS / WaterLBpUKG)), IsMetric, IsUK),
                FormatStrings.FormatTemperature(C.FromF(Injector2WaterDelTempF), IsMetric, false),
                FormatStrings.h,
                FormatStrings.mm);

            if (SteamIsAuxTenderCoupled)
            {
                status.AppendFormat("{0}\t{1}\t{2}\t{3:N0}%\t{4}\t{5}\t\t{6:N0}%\t{7}\t{8}\t\t{9}\t{10}\t\t{11}\t{12:N0}\t{13}\t{14:N0}\n",
                    Simulator.Catalog.GetString("Tender:"),
                    Simulator.Catalog.GetString("Coal"),
                    FormatStrings.FormatMass(TenderCoalMassKG, IsMetric),
                    TenderCoalMassKG / MaxTenderCoalMassKG * 100,
                    Simulator.Catalog.GetString("Water(C)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CombinedTenderWaterVolumeUKG), IsMetric, IsUK),
                    CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG * 100,
                    Simulator.Catalog.GetString("Water(T)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CurrentLocoTenderWaterVolumeUKG), IsMetric, IsUK),
                    Simulator.Catalog.GetString("Water(A)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CurrentAuxTenderWaterVolumeUKG), IsMetric, IsUK),
                    Simulator.Catalog.GetString("Steam"),
                    FormatStrings.FormatMass(Kg.FromLb(CumulativeCylinderSteamConsumptionLbs), IsMetric),
                    Simulator.Catalog.GetString("TotSteam"),
                    FormatStrings.FormatMass(Kg.FromLb(CummulativeTotalSteamConsumptionLbs), IsMetric)
                    );
            }
            else
            {
                status.AppendFormat("{0}\t{1}\t{2}\t{3:N0}%\t{4}\t{5}\t\t{6:N0}%\t{7}\t{8:N0}\t{9}\t\t{10:N0}\n",
                    Simulator.Catalog.GetString("Tender:"),
                    Simulator.Catalog.GetString("Coal"),
                    FormatStrings.FormatMass(TenderCoalMassKG, IsMetric),
                    TenderCoalMassKG / MaxTenderCoalMassKG * 100,
                    Simulator.Catalog.GetString("Water"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CombinedTenderWaterVolumeUKG), IsMetric, IsUK),
                    CombinedTenderWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG * 100,
                    Simulator.Catalog.GetString("Steam"),
                    FormatStrings.FormatMass(Kg.FromLb(CumulativeCylinderSteamConsumptionLbs), IsMetric),
                    Simulator.Catalog.GetString("TotSteam"),
                    FormatStrings.FormatMass(Kg.FromLb(CummulativeTotalSteamConsumptionLbs), IsMetric)
                    );
            }

            status.AppendFormat("{0}\t{1}\t{2}\t\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\n",
                Simulator.Catalog.GetString("Status:"),
                Simulator.Catalog.GetString("CoalOut"),
                CoalIsExhausted ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("WaterOut"),
                WaterIsExhausted ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("FireOut"),
                FireIsExhausted ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("Stoker"),
                StokerIsMechanical ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("Boost"),
                FuelBoost ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("GrLimit"),
                IsGrateLimit ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("FireOn"),
                SetFireOn ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("FireOff"),
                SetFireOff ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                Simulator.Catalog.GetString("AIOR"),
                AIFireOverride ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No")
                );

            if (SteamEngineType == SteamEngineTypes.Geared)
            {
                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Performance"));
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                    Simulator.Catalog.GetString("Power:"),
                    Simulator.Catalog.GetString("MaxInd"),
                    FormatStrings.FormatPower(W.FromHp(DisplayMaxIndicatedHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("Ind"),
                    FormatStrings.FormatPower(W.FromHp(IndicatedHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("Drawbar"),
                    FormatStrings.FormatPower(W.FromHp(DrawbarHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("BlrLmt"),
                    ISBoilerLimited ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"));

                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\n",
                         Simulator.Catalog.GetString("Force:"),
                         Simulator.Catalog.GetString("TheorTE"),
                         FormatStrings.FormatForce(N.FromLbf(DisplayMaxTractiveEffortLbf), IsMetric),
                         Simulator.Catalog.GetString("StartTE"),
                         FormatStrings.FormatForce(absStartTractiveEffortN, IsMetric),
                         Simulator.Catalog.GetString("TE"),
                         FormatStrings.FormatForce(N.FromLbf(DisplayTractiveEffortLbsF), IsMetric),
                         Simulator.Catalog.GetString("Draw"),
                         FormatStrings.FormatForce(N.FromLbf(DrawBarPullLbsF), IsMetric),
                         Simulator.Catalog.GetString("CritSpeed"),
                         FormatStrings.FormatSpeedDisplay(MpS.FromMpH(DisplayMaxLocoSpeedMpH), IsMetric),
                         Simulator.Catalog.GetString("SpdLmt"),
                         IsCritTELimit ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"));

                status.AppendFormat("{0}\t{1}\t{2:N0} {7}/{8}\t\t{3}\t{4:N0} {9}\t{5} {6:N2}\t\t{10}\t{11}\n",
                    Simulator.Catalog.GetString("Move:"),
                    Simulator.Catalog.GetString("Piston"),
                    IsMetric ? Me.FromFt(PistonSpeedFtpMin) : PistonSpeedFtpMin,
                    Simulator.Catalog.GetString("DrvWhl"),
                    pS.TopM(DrvWheelRevRpS),
                    Simulator.Catalog.GetString("MF-Gear"),
                    MotiveForceGearRatio,
                    IsMetric ? FormatStrings.m : FormatStrings.ft,
                    FormatStrings.min,
                    FormatStrings.rpm,
                    Simulator.Catalog.GetString("Max-SpdF"),
                    DisplaySpeedFactor

                    );
            }
            else
            {
                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Performance"));
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                    Simulator.Catalog.GetString("Power:"),
                    Simulator.Catalog.GetString("MaxInd"),
                    FormatStrings.FormatPower(W.FromHp(MaxIndicatedHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("Ind"),
                    FormatStrings.FormatPower(W.FromHp(IndicatedHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("Drawbar"),
                    FormatStrings.FormatPower(W.FromHp(DrawbarHorsePowerHP), IsMetric, false, false),
                    Simulator.Catalog.GetString("BlrLmt"),
                    ISBoilerLimited ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"));

                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\n",
                         Simulator.Catalog.GetString("Force:"),
                         Simulator.Catalog.GetString("TheorTE"),
                         FormatStrings.FormatForce(N.FromLbf(MaxTractiveEffortLbf), IsMetric),
                         Simulator.Catalog.GetString("StartTE"),
                         FormatStrings.FormatForce(absStartTractiveEffortN, IsMetric),
                         Simulator.Catalog.GetString("TE"),
                         FormatStrings.FormatForce(N.FromLbf(DisplayTractiveEffortLbsF), IsMetric),
                         Simulator.Catalog.GetString("Draw"),
                         FormatStrings.FormatForce(N.FromLbf(DrawBarPullLbsF), IsMetric),
                         Simulator.Catalog.GetString("CritSpeed"),
                         FormatStrings.FormatSpeedDisplay(MpS.FromMpH(MaxLocoSpeedMpH), IsMetric),
                         Simulator.Catalog.GetString("SpdLmt"),
                         IsCritTELimit ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"));

                status.AppendFormat("{0}\t{1}\t{2:N0} {7}/{8}\t\t{3}\t{4:N0} {9}\t{5} {6:N2}\t\t{10}\t{11}\n",
                    Simulator.Catalog.GetString("Move:"),
                    Simulator.Catalog.GetString("Piston"),
                    IsMetric ? Me.FromFt(PistonSpeedFtpMin) : PistonSpeedFtpMin,
                    Simulator.Catalog.GetString("DrvWhl"),
                    pS.TopM(DrvWheelRevRpS),
                    Simulator.Catalog.GetString("MF-Gear"),
                    MotiveForceGearRatio,
                    IsMetric ? FormatStrings.m : FormatStrings.ft,
                    FormatStrings.min,
                    FormatStrings.rpm,
                    Simulator.Catalog.GetString("Max-SpdF"),
                    DisplaySpeedFactor

                    );
            }

            if (Simulator.UseAdvancedAdhesion && SteamEngineType != SteamEngineTypes.Geared) // Only display slip monitor if advanced adhesion used
            {
                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Slip Monitor"));
                status.AppendFormat("{0}\t{1}\t{2:N0}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\t{13}\t{14}\t{15:N2}\t{16}\t{17}\t{18:N1}\n",
                    Simulator.Catalog.GetString("Slip:"),
                    Simulator.Catalog.GetString("MForceN"),
                    FormatStrings.FormatForce(MotiveForceN, IsMetric),
                    Simulator.Catalog.GetString("Piston"),
                    FormatStrings.FormatForce(N.FromLbf(StartPistonForceLeftLbf), IsMetric),
                    Simulator.Catalog.GetString("Tang(c)"),
                    FormatStrings.FormatForce(N.FromLbf(StartTangentialCrankWheelForceLbf), IsMetric),
                    Simulator.Catalog.GetString("Tang(t)"),
                    FormatStrings.FormatForce(N.FromLbf(SteamTangentialWheelForce), IsMetric),
                    Simulator.Catalog.GetString("Static"),
                    FormatStrings.FormatForce(N.FromLbf(SteamStaticWheelForce), IsMetric),
                    Simulator.Catalog.GetString("Coeff"),
                    Train.LocomotiveCoefficientFriction,
                    Simulator.Catalog.GetString("Slip"),
                    IsLocoSlip ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                    Simulator.Catalog.GetString("WheelM"),
                    FormatStrings.FormatMass(Kg.FromLb(SteamDrvWheelWeightLbs), IsMetric),
                    Simulator.Catalog.GetString("FoA"),
                    CalculatedFactorofAdhesion);

                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4:N2}\t{5}\t{6:N2}\n",
                Simulator.Catalog.GetString("Sand:"),
                Simulator.Catalog.GetString("S/Use"),
                FormatStrings.FormatVolume(TrackSanderSandConsumptionM3pS, IsMetric),
                Simulator.Catalog.GetString("S/Box"),
                FormatStrings.FormatVolume(CurrentTrackSandBoxCapacityM3, IsMetric),
                Simulator.Catalog.GetString("M/Press"),
                MainResPressurePSI);
            }

#if DEBUG_STEAM_EFFECTS
            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:N2}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\t{13}\t{14:N2}\t{15}\t{16:N2}\t{17}\t{18:N2}\t{19}\t{20:N2}\n",
                "StEff#1:",
                "Cyl1Vel",
                Cylinders1SteamVelocityMpS,
                "Cyl1Vol",
                Cylinders1SteamVolumeM3pS,
                "Cyl1Dur",
                Cylinder1ParticleDurationS,
                "Cyl2Vel",
                Cylinders2SteamVelocityMpS,
                "Cyl2Vol",
                Cylinders2SteamVolumeM3pS,
                "Cyl2Dur",
                Cylinder2ParticleDurationS,
                "CockTime",
                CylinderCockOpenTimeS,
                "SVVel",
                SafetyValvesSteamVelocityMpS,
                "SVVol",
                SafetyValvesSteamVolumeM3pS,
                "SVDur",
                SafetyValvesParticleDurationS
                );

            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6:N2}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\n",
                "StEff#2:",
                "CompVel",
                CompressorSteamVelocityMpS,
                "CompVol",
                CompressorSteamVolumeM3pS,
                "CompDur",
                CompressorParticleDurationS,
                "Inj1Vel",
                Injector1SteamVelocityMpS,
                "Inj1Vol",
                Injector1SteamVolumeM3pS,
                "Inj1Dur",
                Injector1ParticleDurationS
                );

#endif

#if DEBUG_STEAM_SLIP_HUD

                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Slip Debug"));
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\n",
                    Simulator.Catalog.GetString("Start:"),
                    Simulator.Catalog.GetString("CyPressL"),
                    FormatStrings.FormatPressure(CrankLeftCylinderPressure, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("CyPressR"),
                    FormatStrings.FormatPressure(CrankRightCylinderPressure, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("Tang(c)"),
                    FormatStrings.FormatForce(N.FromLbf(StartTangentialCrankWheelForceLbf), IsMetric),
                    Simulator.Catalog.GetString("Tang(t)"),
                    FormatStrings.FormatForce(N.FromLbf(StartTangentialWheelTreadForceLbf), IsMetric),
                    Simulator.Catalog.GetString("Static"),
                    FormatStrings.FormatForce(N.FromLbf(StartStaticWheelFrictionForceLbf), IsMetric)
                );

                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\n",
                  Simulator.Catalog.GetString("Speed:"),
                  Simulator.Catalog.GetString("CyPressL"),
                  FormatStrings.FormatPressure(CrankLeftCylinderPressure, PressureUnit.PSI, MainPressureUnit, true),
                  Simulator.Catalog.GetString("CyPressR"),
                  FormatStrings.FormatPressure(CrankRightCylinderPressure, PressureUnit.PSI, MainPressureUnit, true),
                  Simulator.Catalog.GetString("Tang(c)"),
                  FormatStrings.FormatForce(N.FromLbf(SpeedTotalTangCrankWheelForceLbf), IsMetric),
                  Simulator.Catalog.GetString("Tang(t)"),
                  FormatStrings.FormatForce(N.FromLbf(SpeedTangentialWheelTreadForceLbf), IsMetric),
                  Simulator.Catalog.GetString("Static"),
                  FormatStrings.FormatForce(N.FromLbf(SpeedStaticWheelFrictionForceLbf), IsMetric)
                );

                status.AppendFormat("{0}\t{1}\t{2}\n",
                      Simulator.Catalog.GetString("Wheel:"),
                      Simulator.Catalog.GetString("TangSp"),
                FrictionWheelSpeedMpS
                );


#endif


#if DEBUG_STEAM_SOUND_VARIABLES

            status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Sound Variables"));
            status.AppendFormat("{0}\t{1:N2}\t{2}\t{3:N2}\t{4}\t{5:N2}\n",
              Simulator.Catalog.GetString("V1:"),
              Variable1,
              Simulator.Catalog.GetString("V2:"),
              Variable2,
              Simulator.Catalog.GetString("V3:"),
              Variable3
              );

#endif

            // If vacuum braked display information on ejectors
            if ((BrakeSystem is VacuumSinglePipe))
            {

                status.AppendFormat("\n\t\t\t === {0} === \t\t{1}/{2}\n",
                Simulator.Catalog.GetString("Ejector / Vacuum Pump"),
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(EjectorTotalSteamConsumptionLbpS)), IsMetric),
                FormatStrings.h);

                status.AppendFormat("\t{0}\t{1}\t{2:N2}\t{3}\t{4:N2}/{5}\t{6}\t{7:N2}\t{8}\t{9}",
                Simulator.Catalog.GetString("Large:"),
                Simulator.Catalog.GetString("Press"),
                FormatStrings.FormatPressure(SteamEjectorLargePressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("StCons"),
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(TempEjectorLargeSteamConsumptionLbpS)), IsMetric),
                FormatStrings.h,
                Simulator.Catalog.GetString("Rate"),
                LargeEjectorBrakePipeChargingRatePSIorInHgpS,
                Simulator.Catalog.GetString("Lg Ej"),
                LargeSteamEjectorIsOn ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No")
                );

                if (SmallEjectorControllerFitted) // only display small ejector if fitted.
                {
                    status.AppendFormat("\t{0}\t{1}\t{2:N2}\t{3}\t{4:N2}/{5}\t{6}\t{7:N2}\t{8}\t{9}",
                    Simulator.Catalog.GetString("Small:"),
                    Simulator.Catalog.GetString("Press"),
                    FormatStrings.FormatPressure(SteamEjectorSmallPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    Simulator.Catalog.GetString("StCons"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(TempEjectorSmallSteamConsumptionLbpS)), IsMetric),
                    FormatStrings.h,
                    Simulator.Catalog.GetString("Rate"),
                    SmallEjectorBrakePipeChargingRatePSIorInHgpS,
                    Simulator.Catalog.GetString("Sm Ej"),
                    SmallSteamEjectorIsOn ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No")
                    );
                }

                if (VacuumPumpFitted) // only display vacuum pump if fitted.
                {
                    status.AppendFormat("\t{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6}",
                    Simulator.Catalog.GetString("Vac:"),
                    Simulator.Catalog.GetString("Out"),
                    VacuumPumpOutputFt3pM,
                    Simulator.Catalog.GetString("Rate"),
                    VacuumPumpChargingRateInHgpS,
                    Simulator.Catalog.GetString("Pump"),
                    VacuumPumpOperating ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No")
                    );
                }

                status.AppendFormat("\t{0}\t{1:N2}\t{2}\t{3:N2}\n",
                    Simulator.Catalog.GetString("Leak:"),
                    TrainBrakePipeLeakPSIorInHgpS,
                    Simulator.Catalog.GetString("Net:"),
                    HUDNetBPLossGainPSI
                    );


            }

            // If a water scoop fitted display relevant debug info for the player train only
            if (IsPlayerTrain && HasWaterScoop)
            {
                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Water Scoop"));
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\n",
                Simulator.Catalog.GetString("Fill:"),
               Simulator.Catalog.GetString("ScDwn"),
               IsWaterScoopDown ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
               Simulator.Catalog.GetString("ScBrk"),
               ScoopIsBroken ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
               Simulator.Catalog.GetString("Min"),
               FormatStrings.FormatSpeedDisplay(WaterScoopMinSpeedMpS, IsMetric),
               Simulator.Catalog.GetString("WaVel"),
               FormatStrings.FormatSpeedDisplay(WaterScoopVelocityMpS, IsMetric),
               Simulator.Catalog.GetString("Drag"),
               FormatStrings.FormatForce(WaterScoopDragForceN, IsMetric),
               Simulator.Catalog.GetString("WaterU"),
               FormatStrings.FormatFuelVolume(WaterScoopedQuantityLpS, IsMetric, IsUK),
               Simulator.Catalog.GetString("Input"),
               FormatStrings.FormatFuelVolume(WaterScoopInputAmountL, IsMetric, IsUK),
               Simulator.Catalog.GetString("Total"),
               FormatStrings.FormatFuelVolume(WaterScoopTotalWaterL, IsMetric, IsUK)

               );
            }

            return status.ToString();
        } 

// Gear Box

public void SteamStartGearBoxIncrease()
        {
            if (IsSelectGeared)
            {
                if (throttle == 0)   // only change gears if throttle is at zero
                {
                    if (SteamGearPosition < 2.0f) // Maximum number of gears is two
                    {
                        SteamGearPosition += 1.0f;
                        Simulator.Confirmer.ConfirmWithPerCent(CabControl.GearBox, CabSetting.Increase, SteamGearPosition);
                        if (SteamGearPosition == 0.0)
                        {
                            // Re-initialise the following for the new gear setting - set to zero as in neutral speed
                            DavisAN = NeutralGearedDavisAN; // Reduces locomotive resistance when in neutral gear, as mechanical resistance decreases
                            MotiveForceGearRatio = 0.0f;
                            DisplayMaxLocoSpeedMpH = 0.0f;
                            SteamGearRatio = 0.0f;
                            DisplayMaxTractiveEffortLbf = 0.0f;
                            DisplayMaxIndicatedHorsePowerHP = 0.0f;
                            DrawbarHorsePowerHP = 0.0f;
                            DrawBarPullLbsF = 0.0f;
                            SignalEvent(Event.SteamGearLeverToggle);
                            SignalEvent(Event.GearPosition0);
                        }
                        else if (SteamGearPosition == 1.0)
                        {
                            // Re -initialise the following for the new gear setting
                            DavisAN = GearedRetainedDavisAN; // reset resistance to geared value
                            MotiveForceGearRatio = SteamGearRatioLow;
                            MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                            DisplayMaxLocoSpeedMpH = MaxLocoSpeedMpH;
                            SteamGearRatio = SteamGearRatioLow;
                            SignalEvent(Event.SteamGearLeverToggle);
                            SignalEvent(Event.GearPosition1);

                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * GearedTractiveEffortFactor * MotiveForceGearRatio;
                            DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;

                            // If value set in ENG file then hold to this value, otherwise calculate the value
                            if (RetainedGearedMaxMaxIndicatedHorsePowerHP != 0)
                            {
                                MaxIndicatedHorsePowerHP = RetainedGearedMaxMaxIndicatedHorsePowerHP;
                            }
                            else
                            {
                                // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                                MaxIndicatedHorsePowerHP = MaxSpeedFactor * ((MaxTractiveEffortLbf) * MaxLocoSpeedMpH) / 375.0f;
                            }

                            // Check to see if MaxIHP is in fact limited by the boiler
                            if (MaxIndicatedHorsePowerHP > MaxBoilerOutputHP)
                            {
                                MaxIndicatedHorsePowerHP = MaxBoilerOutputHP; // Set maxIHp to limit set by boiler
                                ISBoilerLimited = true;
                            }
                            else
                            {
                                ISBoilerLimited = false;
                            }

                            DisplayMaxIndicatedHorsePowerHP = MaxIndicatedHorsePowerHP;
                        }
                        else if (SteamGearPosition == 2.0)
                        {
                            // Re -initialise the following for the new gear setting
                            MotiveForceGearRatio = SteamGearRatioHigh;
                            MaxLocoSpeedMpH = MpS.ToMpH(HighMaxGearedSpeedMpS);
                            DisplayMaxLocoSpeedMpH = MaxLocoSpeedMpH;
                            SteamGearRatio = SteamGearRatioHigh;
                            SignalEvent(Event.SteamGearLeverToggle);
                            SignalEvent(Event.GearPosition2);

                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * GearedTractiveEffortFactor * MotiveForceGearRatio;
                            DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;

                            // If value set in ENG file then hold to this value, otherwise calculate the value
                            if (RetainedGearedMaxMaxIndicatedHorsePowerHP != 0)
                            {
                                MaxIndicatedHorsePowerHP = RetainedGearedMaxMaxIndicatedHorsePowerHP;
                            }
                            else
                            {
                                // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                                MaxIndicatedHorsePowerHP = MaxSpeedFactor * ((MaxTractiveEffortLbf) * MaxLocoSpeedMpH) / 375.0f;
                            }

                            // Check to see if MaxIHP is in fact limited by the boiler
                            if (MaxIndicatedHorsePowerHP > MaxBoilerOutputHP)
                            {
                                MaxIndicatedHorsePowerHP = MaxBoilerOutputHP; // Set maxIHp to limit set by boiler
                                ISBoilerLimited = true;
                            }
                            else
                            {
                                ISBoilerLimited = false;
                            }

                            DisplayMaxIndicatedHorsePowerHP = MaxIndicatedHorsePowerHP;
                        }
                    }
                }
                else
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Gears can't be changed unless throttle is at zero."));

                }
            }
        }

        public void SteamStopGearBoxIncrease()
        {

        }

        public void SteamStartGearBoxDecrease()
        {
            if (IsSelectGeared)
            {
                if (throttle == 0)  // only change gears if throttle is at zero
                {
                    if (SteamGearPosition > 0.0f) // Gear number can't go below zero
                    {
                        SteamGearPosition -= 1.0f;
                        Simulator.Confirmer.ConfirmWithPerCent(CabControl.GearBox, CabSetting.Increase, SteamGearPosition);
                        if (SteamGearPosition == 1.0)
                        {

                            // Re -initialise the following for the new gear setting
                            DavisAN = GearedRetainedDavisAN; // reset resistance to geared value
                            MotiveForceGearRatio = SteamGearRatioLow;
                            MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                            DisplayMaxLocoSpeedMpH = MaxLocoSpeedMpH;
                            SteamGearRatio = SteamGearRatioLow;
                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * GearedTractiveEffortFactor * MotiveForceGearRatio;
                            DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                            SignalEvent(Event.SteamGearLeverToggle);
                            SignalEvent(Event.GearPosition1);

                            // If value set in ENG file then hold to this value, otherwise calculate the value
                            if (RetainedGearedMaxMaxIndicatedHorsePowerHP != 0)
                            {
                                MaxIndicatedHorsePowerHP = RetainedGearedMaxMaxIndicatedHorsePowerHP;
                            }
                            else
                            {
                                // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                                MaxIndicatedHorsePowerHP = MaxSpeedFactor * ((MaxTractiveEffortLbf) * MaxLocoSpeedMpH) / 375.0f;
                            }

                            // Check to see if MaxIHP is in fact limited by the boiler
                            if (MaxIndicatedHorsePowerHP > MaxBoilerOutputHP)
                            {
                                MaxIndicatedHorsePowerHP = MaxBoilerOutputHP; // Set maxIHp to limit set by boiler
                                ISBoilerLimited = true;
                            }
                            else
                            {
                                ISBoilerLimited = false;
                            }

                            DisplayMaxIndicatedHorsePowerHP = MaxIndicatedHorsePowerHP;

                        }
                        else if (SteamGearPosition == 0.0)
                        {
                            // Re -initialise the following for the new gear setting - set to zero as in neutral speed
                            DavisAN = NeutralGearedDavisAN; // Reduces locomotive resistance when in neutral gear, as mechanical resistance decreases
                            MotiveForceGearRatio = 0.0f;
                            DisplayMaxLocoSpeedMpH = 0.0f;
                            SteamGearRatio = 0.0f;
                            DisplayMaxTractiveEffortLbf = 0.0f;
                            DisplayMaxIndicatedHorsePowerHP = 0.0f;
                            DrawbarHorsePowerHP = 0.0f;
                            DrawBarPullLbsF = 0.0f;
                            SignalEvent(Event.SteamGearLeverToggle);
                            SignalEvent(Event.GearPosition0);
                        }
                    }
                }
                else
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Gears can't be changed unless throttle is at zero."));

                }
            }
        }

        public void SteamStopGearBoxDecrease()
        {

        }

        //Small Ejector Controller

        #region Small Ejector controller

        public void StartSmallEjectorIncrease(float? target)
        {
            SmallEjectorController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.SmallEjector, CabSetting.Increase, SmallEjectorController.CurrentValue * 100);
            SmallEjectorController.StartIncrease(target);
            SignalEvent(Event.SmallEjectorChange);
        }

        public void StopSmallEjectorIncrease()
        {
            SmallEjectorController.StopIncrease();
            new ContinuousSmallEjectorCommand(Simulator.Log, 1, true, SmallEjectorController.CurrentValue, SmallEjectorController.CommandStartTime);
        }

        public void StartSmallEjectorDecrease(float? target)
        {
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.SmallEjector, CabSetting.Decrease, SmallEjectorController.CurrentValue * 100);
            SmallEjectorController.StartDecrease(target);
            SignalEvent(Event.SmallEjectorChange);
        }

        public void StopSmallEjectorDecrease()
        {
            SmallEjectorController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousSmallEjectorCommand(Simulator.Log, 1, false, SmallEjectorController.CurrentValue, SmallEjectorController.CommandStartTime);
        }

        public void SmallEjectorChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > SmallEjectorController.CurrentValue)
                {
                    StartSmallEjectorIncrease(target);
                }
            }
            else
            {
                if (target < SmallEjectorController.CurrentValue)
                {
                    StartSmallEjectorDecrease(target);
                }
            }
        }

        public void SetSmallEjectorValue(float value)
        {
            var controller = SmallEjectorController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousSmallEjectorCommand(Simulator.Log, 1, change > 0, controller.CurrentValue, Simulator.GameTime);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        #endregion

        //Define Large Ejector Controller

        #region Large Ejector controller

        public void StartLargeEjectorIncrease(float? target)
        {
            LargeEjectorController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.LargeEjector, CabSetting.Increase, LargeEjectorController.CurrentValue* 100);
            LargeEjectorController.StartIncrease(target);
            SignalEvent(Event.LargeEjectorChange);
        }

        public void StopLargeEjectorIncrease()
        {
            LargeEjectorController.StopIncrease();
            new ContinuousLargeEjectorCommand(Simulator.Log, 1, true, LargeEjectorController.CurrentValue, LargeEjectorController.CommandStartTime);
        }

        public void StartLargeEjectorDecrease(float? target)
        {
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.LargeEjector, CabSetting.Decrease, LargeEjectorController.CurrentValue* 100);
            LargeEjectorController.StartDecrease(target);
            SignalEvent(Event.LargeEjectorChange);
        }

        public void StopLargeEjectorDecrease()
        {
            LargeEjectorController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousLargeEjectorCommand(Simulator.Log, 1, false, LargeEjectorController.CurrentValue, LargeEjectorController.CommandStartTime);
        }

        public void LargeEjectorChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > SmallEjectorController.CurrentValue)
                {
                    StartLargeEjectorIncrease(target);
                }
            }
            else
            {
                if (target<LargeEjectorController.CurrentValue)
                {
                    StartLargeEjectorDecrease(target);
                }
            }
        }

        public void SetLargeEjectorValue(float value)
        {
            var controller = LargeEjectorController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousLargeEjectorCommand(Simulator.Log, 1, change > 0, controller.CurrentValue, Simulator.GameTime);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.LargeEjector, oldValue<controller.IntermediateValue? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue* 100);
        }

        #endregion

        public override void StartReverseIncrease(float? target)
        {
            CutoffController.StartIncrease(target);
            CutoffController.CommandStartTime = Simulator.ClockTime;
            switch (Direction)
            {
                case Direction.Reverse: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.Off); break;
                case Direction.N: Simulator.Confirmer.Confirm(CabControl.SteamLocomotiveReverser, CabSetting.Neutral); break;
                case Direction.Forward: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.On); break;
            }
            SignalEvent(Event.ReverserChange);
        }

        public void StopReverseIncrease()
        {
            CutoffController.StopIncrease();
            new ContinuousReverserCommand(Simulator.Log, true, CutoffController.CurrentValue, CutoffController.CommandStartTime);
        }

        public override void StartReverseDecrease(float? target)
        {
            CutoffController.StartDecrease(target);
            CutoffController.CommandStartTime = Simulator.ClockTime;
            switch (Direction)
            {
                case Direction.Reverse: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.Off); break;
                case Direction.N: Simulator.Confirmer.Confirm(CabControl.SteamLocomotiveReverser, CabSetting.Neutral); break;
                case Direction.Forward: Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamLocomotiveReverser, Math.Abs(Train.MUReverserPercent), CabSetting.On); break;
            }
            SignalEvent(Event.ReverserChange);
        }

        public void StopReverseDecrease()
        {
            CutoffController.StopDecrease();
            new ContinuousReverserCommand(Simulator.Log, false, CutoffController.CurrentValue, CutoffController.CommandStartTime);
        }

        public void ReverserChangeTo(bool isForward, float? target)
        {
            if (isForward)
            {
                if (target > CutoffController.CurrentValue)
                {
                    StartReverseIncrease(target);
                }
            }
            else
            {
                if (target < CutoffController.CurrentValue)
                {
                    StartReverseDecrease(target);
                }
            }
        }

        public void SetCutoffValue(float value)
        {
            var controller = CutoffController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousReverserCommand(Simulator.Log, change > 0, controller.CurrentValue, Simulator.GameTime);
                SignalEvent(Event.ReverserChange);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamLocomotiveReverser, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void SetCutoffPercent(float percent)
        {
            Train.MUReverserPercent = CutoffController.SetPercent(percent);
            Direction = Train.MUReverserPercent >= 0 ? Direction.Forward : Direction.Reverse;
        }

        public void StartInjector1Increase(float? target)
        {
            Injector1Controller.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Injector1, CabSetting.Increase, Injector1Controller.CurrentValue * 100);
            Injector1Controller.StartIncrease(target);
        }

        public void StopInjector1Increase()
        {
            Injector1Controller.StopIncrease();
            new ContinuousInjectorCommand(Simulator.Log, 1, true, Injector1Controller.CurrentValue, Injector1Controller.CommandStartTime);
        }

        public void StartInjector1Decrease(float? target)
        {
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Injector1, CabSetting.Decrease, Injector1Controller.CurrentValue * 100);
            Injector1Controller.StartDecrease(target);
            Injector1Controller.CommandStartTime = Simulator.ClockTime;
        }

        public void StopInjector1Decrease()
        {
            Injector1Controller.StopDecrease();
            new ContinuousInjectorCommand(Simulator.Log, 1, false, Injector1Controller.CurrentValue, Injector1Controller.CommandStartTime);
        }

        public void Injector1ChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > Injector1Controller.CurrentValue)
                {
                    StartInjector1Increase(target);
                }
            }
            else
            {
                if (target < Injector1Controller.CurrentValue)
                {
                    StartInjector1Decrease(target);
                }
            }
        }

        public void SetInjector1Value(float value)
        {
            var controller = Injector1Controller;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousInjectorCommand(Simulator.Log, 1, change > 0, controller.CurrentValue, Simulator.GameTime);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector1, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void StartInjector2Increase(float? target)
        {
            Injector2Controller.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Injector2, CabSetting.Increase, Injector2Controller.CurrentValue * 100);
            Injector2Controller.StartIncrease(target);
        }

        public void StopInjector2Increase()
        {
            Injector2Controller.StopIncrease();
            new ContinuousInjectorCommand(Simulator.Log, 2, true, Injector2Controller.CurrentValue, Injector2Controller.CommandStartTime);
        }

        public void StartInjector2Decrease(float? target)
        {
            Injector2Controller.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Injector2, CabSetting.Decrease, Injector2Controller.CurrentValue * 100);
            Injector2Controller.StartDecrease(target);
        }

        public void StopInjector2Decrease()
        {
            Injector2Controller.StopDecrease();
            new ContinuousInjectorCommand(Simulator.Log, 2, false, Injector2Controller.CurrentValue, Injector2Controller.CommandStartTime);
        }

        public void Injector2ChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > Injector2Controller.CurrentValue)
                {
                    StartInjector2Increase(target);
                }
            }
            else
            {
                if (target < Injector2Controller.CurrentValue)
                {
                    StartInjector2Decrease(target);
                }
            }
        }

        public void SetInjector2Value(float value)
        {
            var controller = Injector2Controller;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousInjectorCommand(Simulator.Log, 2, change > 0, controller.CurrentValue, Simulator.GameTime);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Injector2, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void StartBlowerIncrease(float? target)
        {
            BlowerController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Blower, CabSetting.Increase, BlowerController.CurrentValue * 100);
            BlowerController.StartIncrease(target);
            SignalEvent(Event.BlowerChange);
        }
        public void StopBlowerIncrease()
        {
            BlowerController.StopIncrease();
            if (IsPlayerTrain)
                new ContinuousBlowerCommand(Simulator.Log, true, BlowerController.CurrentValue, BlowerController.CommandStartTime);
        }
        public void StartBlowerDecrease(float? target)
        {
            BlowerController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Blower, CabSetting.Decrease, BlowerController.CurrentValue * 100);
            BlowerController.StartDecrease(target);
            SignalEvent(Event.BlowerChange);
        }
        public void StopBlowerDecrease()
        {
            BlowerController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousBlowerCommand(Simulator.Log, false, BlowerController.CurrentValue, BlowerController.CommandStartTime);
        }

        public void BlowerChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > BlowerController.CurrentValue)
                {
                    StartBlowerIncrease(target);
                }
            }
            else
            {
                if (target < BlowerController.CurrentValue)
                {
                    StartBlowerDecrease(target);
                }
            }
        }

        public void SetBlowerValue(float value)
        {
            var controller = BlowerController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousBlowerCommand(Simulator.Log, change > 0, controller.CurrentValue, Simulator.GameTime);
                SignalEvent(Event.BlowerChange);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Blower, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void StartDamperIncrease(float? target)
        {
            DamperController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Damper, CabSetting.Increase, DamperController.CurrentValue * 100);
            DamperController.StartIncrease(target);
            SignalEvent(Event.DamperChange);
        }
        public void StopDamperIncrease()
        {
            DamperController.StopIncrease();
            if (IsPlayerTrain)
                new ContinuousDamperCommand(Simulator.Log, true, DamperController.CurrentValue, DamperController.CommandStartTime);
        }
        public void StartDamperDecrease(float? target)
        {
            DamperController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.Damper, CabSetting.Decrease, DamperController.CurrentValue * 100);
            DamperController.StartDecrease(target);
            SignalEvent(Event.DamperChange);
        }
        public void StopDamperDecrease()
        {
            DamperController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousDamperCommand(Simulator.Log, false, DamperController.CurrentValue, DamperController.CommandStartTime);
        }

        public void DamperChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > DamperController.CurrentValue)
                {
                    StartDamperIncrease(target);
                }
            }
            else
            {
                if (target < DamperController.CurrentValue)
                {
                    StartDamperDecrease(target);
                }
            }
        }

        public void SetDamperValue(float value)
        {
            var controller = DamperController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousDamperCommand(Simulator.Log, change > 0, controller.CurrentValue, Simulator.GameTime);
                SignalEvent(Event.DamperChange);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.Damper, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void StartFireboxDoorIncrease(float? target)
        {
            FireboxDoorController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.FireboxDoor, CabSetting.Increase, FireboxDoorController.CurrentValue * 100);
            FireboxDoorController.StartIncrease(target);
            SignalEvent(Event.FireboxDoorChange);
        }
        public void StopFireboxDoorIncrease()
        {
            FireboxDoorController.StopIncrease();
            if (IsPlayerTrain)
                new ContinuousFireboxDoorCommand(Simulator.Log, true, FireboxDoorController.CurrentValue, FireboxDoorController.CommandStartTime);
        }
        public void StartFireboxDoorDecrease(float? target)
        {
            FireboxDoorController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.FireboxDoor, CabSetting.Decrease, FireboxDoorController.CurrentValue * 100);
            FireboxDoorController.StartDecrease(target);
            SignalEvent(Event.FireboxDoorChange);
        }
        public void StopFireboxDoorDecrease()
        {
            FireboxDoorController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousFireboxDoorCommand(Simulator.Log, false, FireboxDoorController.CurrentValue, FireboxDoorController.CommandStartTime);
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

        public void SetFireboxDoorValue(float value)
        {
            var controller = FireboxDoorController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousFireboxDoorCommand(Simulator.Log, change > 0, controller.CurrentValue, Simulator.GameTime);
                SignalEvent(Event.FireboxDoorChange);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.FireboxDoor, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        public void StartFiringRateIncrease(float? target)
        {
            FiringRateController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.FiringRate, FiringRateController.CurrentValue * 100);
            FiringRateController.StartIncrease(target);
        }
        public void StopFiringRateIncrease()
        {
            FiringRateController.StopIncrease();
            if (IsPlayerTrain)
                new ContinuousFiringRateCommand(Simulator.Log, true, FiringRateController.CurrentValue, FiringRateController.CommandStartTime);
        }
        public void StartFiringRateDecrease(float? target)
        {
            FiringRateController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.FiringRate, FiringRateController.CurrentValue * 100);
            FiringRateController.StartDecrease(target);
        }
        public void StopFiringRateDecrease()
        {
            FiringRateController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousFiringRateCommand(Simulator.Log, false, FiringRateController.CurrentValue, FiringRateController.CommandStartTime);
        }

        public void FiringRateChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > FiringRateController.CurrentValue)
                {
                    StartFiringRateIncrease(target);
                }
            }
            else
            {
                if (target < FiringRateController.CurrentValue)
                {
                    StartFiringRateDecrease(target);
                }
            }
        }

        public void FireShovelfull()
        {
            FireMassKG += ShovelMassKG;
            if (IsPlayerTrain)
                Simulator.Confirmer.Confirm(CabControl.FireShovelfull, CabSetting.On);
            // Make a black puff of smoke
            SmokeColor.Update(1, 0);
        }

public void ToggleCylinderCocks()
        {
            CylinderCocksAreOpen = !CylinderCocksAreOpen;
            SignalEvent(Event.CylinderCocksToggle);
            if (CylinderCocksAreOpen)
                SignalEvent(Event.CylinderCocksOpen);
            else
                SignalEvent(Event.CylinderCocksClose);

            if (IsPlayerTrain)
                Simulator.Confirmer.Confirm(CabControl.CylinderCocks, CylinderCocksAreOpen ? CabSetting.On : CabSetting.Off);
        }

        public void ToggleCylinderCompound()
        {

            if (SteamEngineType == SteamEngineTypes.Compound)  // only use this control if a compound locomotive
            {
                CylinderCompoundOn = !CylinderCompoundOn;
                SignalEvent(Event.CylinderCompoundToggle);
                if (IsPlayerTrain)
                {
                    Simulator.Confirmer.Confirm(CabControl.CylinderCompound, CylinderCompoundOn ? CabSetting.On : CabSetting.Off);
                }
                if (!CylinderCompoundOn) // Compound bypass valve closed - operating in compound mode
                {
                    // Calculate maximum tractive effort if set for compounding
                    MaxTractiveEffortLbf = CylinderEfficiencyRate * (1.6f * MaxBoilerPressurePSI * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderStrokeM)) / ((CompoundCylinderRatio + 1.0f) * (Me.ToIn(DriverWheelRadiusM * 2.0f)));
                    DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                }
                else // Compound bypass valve opened - operating in simple mode
                {
                    // Calculate maximum tractive effort if set to simple operation
                    MaxTractiveEffortLbf = CylinderEfficiencyRate * (1.6f * MaxBoilerPressurePSI * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) / (Me.ToIn(DriverWheelRadiusM * 2.0f));
                    DisplayMaxTractiveEffortLbf = MaxTractiveEffortLbf;
                }
            }
        }

        public void ToggleInjector1()
        {
            if (!FiringIsManual)
                return;
            Injector1IsOn = !Injector1IsOn;
            SignalEvent(Injector1IsOn ? Event.WaterInjector1On : Event.WaterInjector1Off); // hook for sound trigger
            if (IsPlayerTrain)
                Simulator.Confirmer.Confirm(CabControl.Injector1, Injector1IsOn ? CabSetting.On : CabSetting.Off);
        }

        public void ToggleInjector2()
        {
            if (!FiringIsManual)
                return;
            Injector2IsOn = !Injector2IsOn;
            SignalEvent(Injector2IsOn ? Event.WaterInjector2On : Event.WaterInjector2Off); // hook for sound trigger
            if (IsPlayerTrain)
                Simulator.Confirmer.Confirm(CabControl.Injector2, Injector2IsOn ? CabSetting.On : CabSetting.Off);
        }

        public void ToggleBlowdownValve()
        {
            BlowdownValveOpen = !BlowdownValveOpen;
            SignalEvent(Event.BlowdownValveToggle);
            if (BlowdownValveOpen)
                SignalEvent(Event.BoilerBlowdownOn);
            else
                SignalEvent(Event.BoilerBlowdownOff);

            if (IsPlayerTrain)
                Simulator.Confirmer.Confirm(CabControl.BlowdownValve, BlowdownValveOpen? CabSetting.On : CabSetting.Off);
        }

    public void ToggleManualFiring()
        {
            FiringIsManual = !FiringIsManual;
            if (FiringIsManual)
                SignalEvent(Event.AIFiremanSoundOff);
            else
                SignalEvent(Event.AIFiremanSoundOn);
        }

        public void AIFireOn()
        {
            SetFireOn = true;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("AI Fireman has started adding fuel to fire"));
            SetFireOff = false;
        }

        public void AIFireOff()
        {
            SetFireOff = true;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("AI Fireman has stopped adding fuel to fire"));
            SetFireOn = false;
        }

        public void AIFireReset()
        {
            SetFireReset = true;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("AI Fireman has been reset"));
            SetFireOff = false;
            SetFireOn = false;
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            if (type == (uint)PickupType.FuelCoal) return FuelController;
            if (type == (uint)PickupType.FuelWater) return WaterController;
            return null;
        }

        /// <summary>
        /// Sets step size for the fuel controller basing on pickup feed rate and engine fuel capacity
        /// </summary>
        /// <param name="type">Pickup</param>
        public override void SetStepSize(PickupObj matchPickup)
        {
            uint type = matchPickup.PickupType;
            if (type == (uint)PickupType.FuelCoal && MaxTenderCoalMassKG != 0)
                FuelController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / MaxTenderCoalMassKG);
            else if (type == (uint)PickupType.FuelWater && MaxLocoTenderWaterMassKG != 0)
                WaterController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / MaxLocoTenderWaterMassKG); 
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
            if (pickupType == (uint)PickupType.FuelWater)
            {
                return WaterController.CurrentValue;
            }
            if (pickupType == (uint)PickupType.FuelCoal)
            {
                return FuelController.CurrentValue;
            }
            return 0f;
        }

        public void GetLocoInfo(ref float CC, ref float BC, ref float DC, ref float FC, ref float I1, ref float I2, ref float SE, ref float LE)
        {
            CC = CutoffController.CurrentValue;
            BC = BlowerController.CurrentValue;
            DC = DamperController.CurrentValue;
            FC = FiringRateController.CurrentValue;
            I1 = Injector1Controller.CurrentValue;
            I2 = Injector2Controller.CurrentValue;
            SE = SmallEjectorController.CurrentValue;
            LE = LargeEjectorController.CurrentValue;
        }

        public void SetLocoInfo(float CC, float BC, float DC, float FC, float I1, float I2, float SE, float LE)
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
            SmallEjectorController.CurrentValue = SE;
            SmallEjectorController.UpdateValue = 0.0f;
            LargeEjectorController.CurrentValue = LE;
            LargeEjectorController.UpdateValue = 0.0f;
        }

        public override void SwitchToPlayerControl()
        {
            if (Train.MUReverserPercent == 100)
            {
                Train.MUReverserPercent = 25;
                if ((Flipped ^ UsingRearCab)) CutoffController.SetValue(-0.25f);
                else CutoffController.SetValue(0.25f);

            }
            else if (Train.MUReverserPercent == -100)
            {
                Train.MUReverserPercent = -25;
                if ((Flipped ^ UsingRearCab)) CutoffController.SetValue(0.25f);
                else CutoffController.SetValue(-0.25f);

            }
            base.SwitchToPlayerControl();
        }

        public override void SwitchToAutopilotControl()
        {
            if (Train.MUDirection != Direction.Forward) SignalEvent(Event.ReverserChange);
            Train.MUDirection = Direction.Forward;
            Train.MUReverserPercent = 100;
            base.SwitchToAutopilotControl();
        }

    } // class SteamLocomotive
}
