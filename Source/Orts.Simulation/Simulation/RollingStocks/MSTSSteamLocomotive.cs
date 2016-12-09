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

// Steam debugging is off by default - uncomment the #define to turn on - provides printout in log at set speed for steam indicator pressures.
//#define DEBUG_LOCO_STEAM

// Burn debugging is off by default - uncomment the #define to turn on - provides visibility of burn related parameters on extended HUD.
//#define DEBUG_LOCO_BURN

// Steam usage debugging is off by default - uncomment the #define to turn on - provides visibility of steam usage related parameters on extended HUD. 
//#define DEBUG_LOCO_STEAM_USAGE

// Steam heating debugging is off by default - uncomment the #define to turn on - provides visibility of steam usage related parameters on extended HUD. 
// #define DEBUG_LOCO_STEAM_HEAT

// Steam heating debugging is off by default - uncomment the #define to turn on - provides visibility of steam usage related parameters on extended HUD. 
//#define DEBUG_LOCO_STEAM_HEAT_HUD

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

// Debug for Steam Ejector
//#define DEBUG_STEAM_EJECTOR


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
        public MSTSNotchController SteamHeatController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController Injector1Controller = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController Injector2Controller = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController BlowerController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController DamperController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FiringRateController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FireboxDoorController = new MSTSNotchController(0, 1, 0.1f);
        public MSTSNotchController FuelController = new MSTSNotchController(0, 1, 0.01f); // Could be coal, wood, oil or even peat !
        public MSTSNotchController WaterController = new MSTSNotchController(0, 1, 0.01f);
        public MSTSNotchController SmallEjectorController = new MSTSNotchController(0, 1, 0.1f);

        public bool Injector1IsOn;
        public bool Injector2IsOn;
        public bool CylinderCocksAreOpen;
        public bool CylinderCompoundOn;
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
        bool BoilerHeat = false;    // Boiler heat has exceeded total max possible heat in boiler
        bool ShovelAnyway = false; // Predicts when the AI fireman should be increasing the fire burn rate despite the heat in the boiler
        bool IsGrateLimit = false; // Grate limit of locomotive exceeded
        bool HasSuperheater = false;  // Flag to indicate whether locomotive is superheated steam type
        bool IsSuperSet = false;    // Flag to indicate whether superheating is reducing cylinder condenstation
        bool IsSaturated = false;     // Flag to indicate locomotive is saturated steam type
        bool safety2IsOn = false; // Safety valve #2 is on and opertaing
        bool safety3IsOn = false; // Safety valve #3 is on and opertaing
        bool safety4IsOn = false; // Safety valve #4 is on and opertaing
        bool IsFixGeared = false;
        bool IsSelectGeared = false;
        bool IsLocoSlip = false; 	   // locomotive is slipping

        // Aux Tender Parameters
        public bool AuxTenderMoveFlag = false; // Flag to indicate whether train has moved
        bool SteamIsAuxTenderCoupled = false;
        float TenderWaterChangePercent;       // Percentatge of water in tender
        public float CurrentAuxTenderWaterMassKG;
        float CurrentAuxTenderWaterVolumeUKG;
        float CombinedTenderWaterVolumeUKG;     // Combined value of water in tender and aux tender
        float PrevCombinedTenderWaterVolumeUKG;
        float PreviousTenderWaterVolumeUKG;

        // Carriage Steam Heating Parameters
        float MaxSteamHeatPressurePSI;    // Maximum Steam heating pressure
        float InsideTempC;                // Desired inside temperature for carriage steam heating
        float OutsideTempC;               // External ambient temeprature for carriage steam heating.
        float CurrentSteamHeatPressurePSI = 0.0f;   // Current pressure in steam heat system
        float CurrentTrainSteamHeatW;    // Current steam heat of air in train
        float CurrentCarriageHeatTempC;          // Set train carriage heat
        float SteamPipeHeatConvW;               // Heat radiated by steam pipe - convection
        float SteamHeatPipeRadW;                // Heat radiated by steam pipe - radiation
        float SteamPipeHeatW;               // Heat radiated by steam pipe - total
        float CurrentSteamHeatPipeTempC;                 // Temperature of steam in steam heat system based upon pressure setting
        float SpecificHeatCapcityAirKJpKgK = 1006.0f; // Specific Heat Capacity of Air
        float DensityAirKgpM3 = 1.247f;   // Density of air - use a av value
        float PipeHeatTransCoeffWpM2K = 22.0f;    // heat transmission coefficient for a steel pipe.
        float BoltzmanConstPipeWpM2 = 0.0000000567f; // Boltzman's Constant
        bool IsSteamInitial = true;        // To initialise steam heat
        bool IsSteamHeatFirstTime = true;  // Flag for first pass at steam heating.
        bool IsSteamHeatFitted = false;    // Is steam heating fitted to locomotive
        float CalculatedCarHeaterSteamUsageLBpS;  //
        float TotalTrainSteamHeatW;         // Total steam heat in train - based upon air volume
        float NetSteamHeatLossWpTime;        // Net Steam loss - Loss in Cars vs Steam Pipe Heat
        float DisplayNetSteamHeatLossWpTime;  // Display Net Steam loss - Loss in Cars vs Steam Pipe Heat
        bool IsSteamHeatExceeded = false;   // Flag to indicate when steam heat temp is exceeded
        bool IsSteamHeatLow = false;        // Flag to indicate when steam heat temp is low

        string SteamLocoType;     // Type of steam locomotive type

        float PulseTracker;
        int NextPulse = 1;

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
        float FuelBurnRateKGpS;
        float FuelFeedRateKGpS;
        float DesiredChange;     // Amount of change to increase fire mass, clamped to range 0.0 - 1.0
        public float CylinderSteamUsageLBpS;
        public float NewCylinderSteamUsageLBpS;
        public float BlowerSteamUsageLBpS;
        public float BoilerPressurePSI;     // Gauge pressure - what the engineer sees.

        public float EvaporationLBpS;          // steam generation rate
        public float FireMassKG;      // Mass of coal currently on grate area
        public float FireRatio;
        float FlueTempK = 775;      // Initial FlueTemp (best @ 475)
        float MaxFlueTempK;         // FlueTemp at full boiler performance
        public bool SafetyIsOn;
        public readonly SmoothedData SmokeColor = new SmoothedData(2);

        // eng file configuration parameters

        float MaxBoilerPressurePSI = 180f;  // maximum boiler pressure, safety valve setting
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
        float GrateLimitLBpFt2 = 150.0f;       // Max combustion rate of the grate, once this is reached, no more steam is produced.
        float MaxFuelBurnGrateKGpS;            // Maximum rate of fuel burnt depending upon grate limit
        float PreviousFireHeatTxfKW;    // Capture max FireHeat value before Grate limit is exceeded.
        float GrateCombustionRateLBpFt2;     // Grate combustion rate, ie how many lbs coal burnt per sq ft grate area.
        float ORTSMaxFiringRateKGpS;          // OR equivalent of above
        float DisplayMaxFiringRateKGpS;     // Display value of MaxFiringRate
        public float SafetyValveUsageLBpS;
        float BoilerHeatOutSVAIBTUpS;
        float SafetyValveDropPSI = 4.0f;      // Pressure drop before Safety valve turns off, normally around 4 psi - First safety valve normally operates between MaxBoilerPressure, and MaxBoilerPressure - 4, ie Max Boiler = 200, cutoff = 196.
        float EvaporationAreaM2;
        float SuperheatAreaM2 = 0.0f;      // Heating area of superheater
        float SuperheatKFactor = 11.7f;     // Factor used to calculate superheat temperature - guesstimate
        float SuperheatRefTempF;            // Superheat temperature in deg Fahrenheit, based upon the heating area.
        float SuperheatTempRatio;          // A ratio used to calculate the superheat temp - based on the ratio of superheat (using heat area) to "known" curve. 
        float CurrentSuperheatTempF;      // current value of superheating based upon boiler steam output
        float SuperheatVolumeRatio;   // Approximate ratio of Superheated steam to saturated steam at same pressure
        float FuelCalorificKJpKG = 33400;
        float ManBlowerMultiplier = 20.0f; // Blower Multipler for Manual firing
        float ShovelMassKG = 6;
        float HeatRatio = 0.001f;        // Ratio to control burn rate - based on ratio of heat in vs heat out
        float PressureRatio = 0.001f;    // Ratio to control burn rate - based upon boiler pressure
        float BurnRateRawKGpS;           // Raw combustion (burn) rate
        SmoothedData FuelRateStoker = new SmoothedData(15); // Stoker is more responsive and only takes x seconds to fully react to changing needs.
        SmoothedData FuelRate = new SmoothedData(45); // Automatic fireman takes x seconds to fully react to changing needs.
        SmoothedData BurnRateSmoothKGpS = new SmoothedData(120); // Changes in BurnRate take x seconds to fully react to changing needs.
        float FuelRateSmooth = 0.0f;     // Smoothed Fuel Rate
        public bool HasWaterScoop = false; // indicates whether loco + tender have a water scoop or not
        public float ScoopMinPickupSpeedMpS = 0.0f; // Minimum scoop pickup speed
        public float ScoopMaxPickupSpeedMpS = 200.0f; // Maximum scoop pickup speed
        public float ScoopResistanceN = 0.0f; // Scoop resistance
        public bool ScoopIsBroken = false; // becomes broken if activated where there is no trough
        public bool RefillingFromTrough = false; // refilling from through is ongoing

        // precomputed values
        float CylinderSweptVolumeFT3pFT;     // Volume of steam Cylinder
        float LPCylinderSweptVolumeFT3pFT;     // Volume of LP steam Cylinder
        float CylinderCondensationFactor;  // Cylinder compensation factor for condensation in cylinder due to cutoff
        float CylinderSpeedCondensationFactor;  // Cylinder compensation factor for condensation in cylinder due to speed
        float BlowerSteamUsageFactor;
        float InjectorFlowRateLBpS;    // Current injector flow rate - based upon current boiler pressure
        float MaxInjectorFlowRateLBpS = 0.0f;      // Maximum possible injector flow rate - based upon maximum boiler pressure
        Interpolator BackPressureIHPtoAtmPSI;             // back pressure in cylinders given usage
        Interpolator CylinderSteamDensityPSItoLBpFT3;   // steam density in cylinders given pressure (could be super heated)
        Interpolator SteamDensityPSItoLBpFT3;   // saturated steam density given pressure
        Interpolator WaterDensityPSItoLBpFT3;   // water density given pressure
        Interpolator SteamHeatPSItoBTUpLB;      // total heat in saturated steam given pressure
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
        Interpolator CylinderCondensationFactorSpeed;  // Table to find the cylinder condensation fraction Vs speed for the cylinder - saturated steam
        Interpolator SuperheatTempLimitXtoDegF;  // Table to find Super heat temp required to prevent cylinder condensation - Ref Elseco Superheater manual
        Interpolator SuperheatTempLbpHtoDegF;  // Table to find Super heat temp per lbs of steam to cylinder - from BTC Test Results for Std 8
        Interpolator InitialPressureDropRatioRpMtoX; // Allowance for wire-drawing - ie drop in initial pressure (cutoff) as speed increases
        Interpolator SteamChestPressureDropRatioRpMtoX; // Allowance for pressure drop in Steam chest pressure compared to Boiler Pressure

        Interpolator SteamEjectorSteamUsageLBpHtoPSI; // Steam consumption of steam ejector
        Interpolator SteamEjectorCapacityFactorIntoX; // Steam capacity factor for steam ejector

        Interpolator SaturatedSpeedFactorSpeedDropFtpMintoX; // Allowance for drop in TE for a saturated locomotive due to piston speed limitations
        Interpolator SuperheatedSpeedFactorSpeedDropFtpMintoX; // Allowance for drop in TE for a superheated locomotive due to piston speed limitations

        Interpolator NewBurnRateSteamToCoalLbspH; // Combustion rate of steam generated per hour to Dry Coal per hour

        Interpolator2D CutoffInitialPressureDropRatioUpper;  // Upper limit of the pressure drop from initial pressure to cut-off pressure
        Interpolator2D CutoffInitialPressureDropRatioLower;  // Lower limit of the pressure drop from initial pressure to cut-off pressure

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
        const float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        public float MaxTenderCoalMassKG;          // Maximum read from Eng File
        public float MaxTenderWaterMassKG;         // Maximum read from Eng file
        public float TenderCoalMassKG              // Decreased by firing and increased by refilling
        {
            get { return FuelController.CurrentValue * MaxTenderCoalMassKG; }
            set { FuelController.CurrentValue = value / MaxTenderCoalMassKG; }
        }
        public float TenderWaterVolumeUKG          // Decreased by running injectors and increased by refilling
        {
            get { return WaterController.CurrentValue * Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG; }
            set { WaterController.CurrentValue = value / (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG); }
        }
        float DamperBurnEffect;             // Effect of the Damper control
        float Injector1Fraction = 0.0f;     // Fraction (0-1) of injector 1 flow from Fireman controller or AI
        float Injector2Fraction = 0.0f;     // Fraction (0-1) of injector  of injector 2 flow from Fireman controller or AI
        float SafetyValveStartPSI = 0.1f;   // Set safety valve to just over max pressure - allows for safety valve not to operate in AI firing
        float InjectorBoilerInputLB = 0.0f; // Input into boiler from injectors
        const float WaterDensityAt100DegC1BarKGpM3 = 954.8f;


        // Steam Ejector
        float SteamEjectorSmallDiameterIn = 0.787402f; // Actual diameter of small ejector (Assume a small ejector of 20mm - Dreadnought)
        float EjectorBaseDiameterIn = 1.0f;         // Base reference diameter all values scalled from this value.
        float SteamEjectorSmallSetting;
        float SteamEjectorSmallBaseSteamUsageLbpS;
        float SmallEjectorCapacityFactor;
        float EjectorSmallSteamConsumptionLbpS;
        float EjectorLargeSteamConsumptionLbpS;
        float EjectorTotalSteamConsumptionLbpS;
        float SteamEjectorLargeBaseUsageSteamLbpS;
        float LargeEjectorCapacityFactor;
        float SteamEjectorLargeDiameterIn = 1.1811f;  // Actual diameter of large ejector (Assume a large ejector value of 30mm - Dreadnought )
        float SteamEjectorLargePressurePSI = 120.0f;

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
        float WaterTempIN;              // Input to water Temp Integrator.
        float WaterTempNewK;            // Boiler Water Temp (Kelvin) - for testing purposes
        float BkW_Diff;                 // Net Energy into boiler after steam loads taken.
        float WaterVolL;                // Actual volume of water in bolier (litres)
        float BoilerHeatOutBTUpS = 0.0f;// heat out of boiler in BTU
        float BoilerHeatInBTUpS = 0.0f; // heat into boiler in BTU
        float BoilerHeatExcess;         // Vlaue of excess boiler heat
        float InjCylEquivSizeIN;        // Calculate the equivalent cylinder size for purpose of sizing the injector.
        float InjectorSize;             // size of injector installed on boiler

        // Values from previous iteration to use in UpdateFiring() and show in HUD
        float PreviousBoilerHeatOutBTUpS = 0.0f;
        public float PreviousTotalSteamUsageLBpS;
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
        float SuperheaterSteamUsageFactor = 1.0f;       // Below 1.0, reduces steam usage due to superheater
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
        float CutoffPressureDropRatio;  // Ratio of Cutoff Pressure to Initial Pressure
        float CylinderPressureAtmPSI;
        float BackPressureAtmPSI;
        float InitialPressureAtmPSI;    // Initial Pressure to cylinder @ start if stroke
        float CutoffPressureAtmPSI;    // Pressure at cutoff
        float SteamChestPressurePSI;    // Pressure in steam chest - input to cylinder

        float CylinderAdmissionWorkInLbs; // Work done during steam admission into cylinder
        float CylinderExhaustOpenFactor; // Point on cylinder stroke when exhaust valve opens.
        float CylinderCompressionCloseFactor; // Point on cylinder stroke when compression valve closes - assumed reciporical of exhaust opens.
        float CylinderPreAdmissionOpenFactor = 0.05f; // Point on cylinder stroke when pre-admission valve opens
        float CylinderReleasePressureAtmPSI;       // Pressure when exhaust valve opens
        float CylinderPreCompressionPressureAtmPSI;       // Pressure when exhaust valve closes
        float CylinderPreAdmissionPressureAtmPSI;    // Pressure after compression occurs and steam admission starts
        float CylinderExpansionWorkInLbs; // Work done during expansion stage of cylinder
        float CylinderReleaseWorkInLbs;   // Work done during release stage of cylinder
        float CylinderCompressionWorkInLbs; // Work done during compression stage of cylinder
        float CylinderPreAdmissionWorkInLbs; // Work done during PreAdmission stage of cylinder
        float CylinderExhaustWorkInLbs; // Work done during Exhaust stage of cylinder

        // Compound Cylinder Information - HP Cylinder

        float HPCylinderInitialPressureAtmPSI;    // Initial Pressure to HP cylinder @ start if stroke
        float HPCylinderCutoffPressureAtmPSI;    // Pressure at HP cylinder cutoff
        float HPCylinderReleasePressureAtmPSI;       // Pressure in HP cylinder when steam release valve opens
        float HPCylinderReleasePressureRecvAtmPSI;   // Pressure in HP cylinder when steam release valve opens, and steam moves into steam passages which connect HP & LP together
        float HPCylinderExhaustPressureAtmPSI;       // Pressure in HP cylinder when steam completely released from the cylinder
        float HPCylinderPreCompressionPressureAtmPSI;       // Pressure when exhaust valve closes, and compression commences
        float HPCylinderPreAdmissionOpenPressureAtmPSI; //  Pre-Admission pressure prior to exhaust valve closing
        float HPCylinderBackPressureAtmPSI;     // Back pressure on HP cylinder
        float HPCylinderMEPAtmPSI;                 // Mean effective Pressure of HP Cylinder
        float HPCylinderClearancePC = 0.19f;    // Assume cylinder clearance of 19% of the piston displacement for HP cylinder
        float CompoundRecieverVolumePCHP = 0.3f; // Volume of receiver or passages between HP and LP cylinder as a fraction of the HP cylinder volume.
        float HPCylinderVolumeFactor = 1.0f;    // Represents the full volume of the HP steam cylinder    
        float LPCylinderVolumeFactor = 1.0f;    // Represents the full volume of the LP steam cylinder 

        // Compound Cylinder Information - LP Cylinder
        float LPCylinderInitialPressureAtmPSI;    // Initial Pressure to LP cylinder @ start if stroke
        float LPCylinderPreCutoffPressureAtmPSI; // Pressure in combined HP & LP Cylinder pre-cutoff
        float LPCylinderReleasePressureAtmPSI;   // Pressure in LP cylinder when steam release valve opens
        float LPCylinderPreCompressionPressureAtmPSI;       // Pressure in LP cylinder when exhaust valve closes, and compression commences
        float LPCylinderPreAdmissionPressureAtmPSI;    // Pressure in LP cylinder after compression occurs and steam admission starts
        float LPCylinderMEPAtmPSI;                     // Mean effective pressure of LP Cylinder
        float LPCylinderClearancePC = 0.066f;    // Assume cylinder clearance of 6.6% of the piston displacement for LP cylinder
        float LPCylinderBackPressureAtmPSI;     // Back pressure on LP cylinder

        // Simple locomotive cylinder information

        float MeanEffectivePressurePSI;         // Mean effective pressure
        float RatioOfExpansion;             // Ratio of expansion
        float CylinderClearancePC = 0.09f;    // Assume cylinder clearance of 8% of the piston displacement for saturated locomotives and 9% for superheated locomotive - default to saturated locomotive value
        float CylinderPortOpeningFactor;   // Model the size of the steam port opening in the cylinder - set to 0.085 as default, if no ENG file value added
        float CylinderPortOpeningUpper = 0.12f; // Set upper limit for Cylinder port opening
        float CylinderPortOpeningLower = 0.05f; // Set lower limit for Cylinder port opening
        float CylinderPistonShaftFt3;   // Volume taken up by the cylinder piston shaft
        float CylinderPistonShaftDiaIn = 3.5f; // Assume cylinder piston shaft to be 3.5 inches
        float CylinderPistonAreaFt2;    // Area of the piston in the cylinder (& HP Cylinder in case of Compound locomotive)
        float LPCylinderPistonAreaFt2;    // Area of the piston in the LP cylinder
        float CylinderClearanceSteamWeightLbs; // Weight of steam remaining in cylinder at "compression" (after exhaust valve closed)
        float CylinderCutoffSteamWeightLbs;   // Weight of steam remaining in cylinder at "compression" (after exhaust valve opens)
        float CylinderCutoffSteamVolumeFt3; // Volume of cylinder at steam release in cylinder
        float CylinderClearanceSteamVolumeFt3; // Volume in cylinder at start of steam compression
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

        float DrvWheelDiaM;     // Diameter of driver wheel
        float DrvWheelRevRpS;       // number of revolutions of the drive wheel per minute based upon speed.
        float PistonSpeedFtpMin;      // Piston speed of locomotive
        float IndicatedHorsePowerHP;   // Indicated Horse Power (IHP), theoretical power of the locomotive, it doesn't take into account the losses due to friction, etc. Typically output HP will be 70 - 90% of the IHP
        float DrawbarHorsePowerHP;  // Drawbar Horse Power  (DHP), maximum power available at the wheels.
        float DrawBarPullLbsF;      // Drawbar pull in lbf
        float BoilerEvapRateLbspFt2;  // Sets the evaporation rate for the boiler is used to multiple boiler evaporation area by - used as a player customisation factor.

        float MaxSpeedFactor;      // Max Speed factor - factor @ critical piston speed to reduce TE due to speed increase - American locomotive company
        float SpeedFactor;      // Current Speed factor - factor to reduce TE due to speed increase - American locomotive company
        float DisplaySpeedFactor;  // Value displayed in HUD

        public float MaxTractiveEffortLbf;     // Maximum theoritical tractive effort for locomotive
        float DisplayTractiveEffortLbsF; // Value of Tractive eefort to display in HUD
        float MaxCriticalSpeedTractiveEffortLbf;  // Maximum power value @ critical speed of piston
        float CurrentCriticalSpeedTractiveEffortLbf;  // Current power value @ current speed of piston
        float DisplayCriticalSpeedTractiveEffortLbf;  // Display power value @ speed of piston
        float StartTractiveEffortN = 0.0f;      // Record starting tractive effort
        float TractiveEffortLbsF;           // Current sim calculated tractive effort
        const float TractiveEffortFactor = 0.85f;  // factor for calculating Theoretical Tractive Effort

        float MaxLocoSpeedMpH;      // Speed of loco when max performance reached
        float MaxPistonSpeedFtpM;   // Piston speed @ max performance for the locomotive
        float MaxIndicatedHorsePowerHP; // IHP @ max performance for the locomotive
        float absSpeedMpS;

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
        float StartPistonForceLbf;    // Max force exerted by piston.
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
        float SpeedCrankCylinderPositionLeft;
        float SpeedCrankCylinderPositionMiddle;
        float SpeedCrankCylinderPositionRight;
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

        public float DrainpipeSteamVolumeM3pS;
        public float DrainpipeSteamVelocityMpS;
        public float Injector1SteamVolumeM3pS;
        public float Injector1SteamVelocityMpS;
        public float Injector2SteamVolumeM3pS;
        public float Injector2SteamVelocityMpS;
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
            WaterController.CurrentValue = TenderWaterVolumeUKG / (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG);
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
                case "engine(ortscylinderexhaustopen": CylinderExhaustOpenFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderportopening": CylinderPortOpeningFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(boilervolume": BoilerVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null); break;
                case "engine(maxboilerpressure": MaxBoilerPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(maxsteamheatingpressure": MaxSteamHeatPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(shovelcoalmass": ShovelMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtendercoalmass": MaxTenderCoalMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(maxtenderwatermass": MaxTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "engine(steamfiremanmaxpossiblefiringrate": MaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(steamfiremanismechanicalstoker": Stoker = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortssteamfiremanmaxpossiblefiringrate": ORTSMaxFiringRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null) / 2.2046f / 3600; break;
                case "engine(enginecontrollers(cutoff": CutoffController.Parse(stf); break;
                case "engine(enginecontrollers(steamheat": SteamHeatController.Parse(stf); break;
                case "engine(enginecontrollers(smallejector": SmallEjectorController.Parse(stf); break;
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
                case "engine(ortsboilerevaporationrate": BoilerEvapRateLbspFt2 = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderefficiencyrate": CylinderEfficiencyRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortscylinderinitialpressuredrop": InitialPressureDropRatioRpMtoX = new Interpolator(stf); break;
                case "engine(ortscylinderbackpressure": BackPressureIHPtoAtmPSI = new Interpolator(stf); break;
                case "engine(ortsburnrate": NewBurnRateSteamToCoalLbspH = new Interpolator(stf); break;
                case "engine(ortsboilerefficiency": BoilerEfficiencyGrateAreaLBpFT2toX = new Interpolator(stf); break;
                case "engine(ortssteamgearratio":
                    stf.MustMatch("(");
                    SteamGearRatioLow = stf.ReadFloat(STFReader.UNITS.None, null);
                    SteamGearRatioHigh = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "engine(ortssteammaxgearpistonrate": MaxSteamGearPistonRateFtpM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
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
                case "engine(enginecontrollers(waterscoop": HasWaterScoop = true; break;
                case "engine(steamwaterscoopminpickupspeed": ScoopMinPickupSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.SpeedDefaultMPH, 0.0f); break;
                case "engine(steamwaterscoopmaxpickupspeed": ScoopMaxPickupSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.SpeedDefaultMPH, 0.0f); break;
                case "engine(steamwaterscoopresistance": ScoopResistanceN = stf.ReadFloatBlock(STFReader.UNITS.Force, 0.0f); break;
                //Not used at the moment. Default unit of measure libs/s does not exist either
                //                case "engine(steamwaterpickuprate": ScoopPickupRateKGpS = stf.ReadFloatBlock(STFReader.UNITS.MassRateDefaultLBpH, null); break;
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
            MaxSteamHeatPressurePSI = locoCopy.MaxSteamHeatPressurePSI;
            ShovelMassKG = locoCopy.ShovelMassKG;
            MaxTenderCoalMassKG = locoCopy.MaxTenderCoalMassKG;
            MaxTenderWaterMassKG = locoCopy.MaxTenderWaterMassKG;
            MaxFiringRateKGpS = locoCopy.MaxFiringRateKGpS;
            Stoker = locoCopy.Stoker;
            ORTSMaxFiringRateKGpS = locoCopy.ORTSMaxFiringRateKGpS;
            CutoffController = (MSTSNotchController)locoCopy.CutoffController.Clone();
            SteamHeatController = (MSTSNotchController)locoCopy.SteamHeatController.Clone();
            Injector1Controller = (MSTSNotchController)locoCopy.Injector1Controller.Clone();
            Injector2Controller = (MSTSNotchController)locoCopy.Injector2Controller.Clone();
            BlowerController = (MSTSNotchController)locoCopy.BlowerController.Clone();
            DamperController = (MSTSNotchController)locoCopy.DamperController.Clone();
            FiringRateController = (MSTSNotchController)locoCopy.FiringRateController.Clone();
            FireboxDoorController = (MSTSNotchController)locoCopy.FireboxDoorController.Clone();
            SmallEjectorController = (MSTSNotchController)locoCopy.SmallEjectorController.Clone();
            GrateAreaM2 = locoCopy.GrateAreaM2;
            SuperheaterFactor = locoCopy.SuperheaterFactor;
            EvaporationAreaM2 = locoCopy.EvaporationAreaM2;
            SuperheatAreaM2 = locoCopy.SuperheatAreaM2;
            FuelCalorificKJpKG = locoCopy.FuelCalorificKJpKG;
            BoilerEvapRateLbspFt2 = locoCopy.BoilerEvapRateLbspFt2;
            CylinderEfficiencyRate = locoCopy.CylinderEfficiencyRate;
            InitialPressureDropRatioRpMtoX = new Interpolator(locoCopy.InitialPressureDropRatioRpMtoX);
            BackPressureIHPtoAtmPSI = new Interpolator(locoCopy.BackPressureIHPtoAtmPSI);
            NewBurnRateSteamToCoalLbspH = new Interpolator(locoCopy.NewBurnRateSteamToCoalLbspH);
            BoilerEfficiency = locoCopy.BoilerEfficiency;
            SteamGearRatioLow = locoCopy.SteamGearRatioLow;
            SteamGearRatioHigh = locoCopy.SteamGearRatioHigh;
            MaxSteamGearPistonRateFtpM = locoCopy.MaxSteamGearPistonRateFtpM;
            SteamEngineType = locoCopy.SteamEngineType;
            IsSaturated = locoCopy.IsSaturated;
            HasSuperheater = locoCopy.HasSuperheater;
            IsFixGeared = locoCopy.IsFixGeared;
            IsSelectGeared = locoCopy.IsSelectGeared;
            HasWaterScoop = locoCopy.HasWaterScoop;
            ScoopMinPickupSpeedMpS = locoCopy.ScoopMinPickupSpeedMpS;
            ScoopMaxPickupSpeedMpS = locoCopy.ScoopMaxPickupSpeedMpS;
            ScoopResistanceN = locoCopy.ScoopResistanceN;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(BoilerHeatOutBTUpS);
            outf.Write(BoilerHeatInBTUpS);
            outf.Write(TenderCoalMassKG);
            outf.Write(TenderWaterVolumeUKG);
            outf.Write(CylinderSteamUsageLBpS);
            outf.Write(BoilerHeatBTU);
            outf.Write(BoilerMassLB);
            outf.Write(BoilerPressurePSI);
            outf.Write(WaterTempNewK);
            outf.Write(EvaporationLBpS);
            outf.Write(CurrentCarriageHeatTempC);
            outf.Write(CurrentTrainSteamHeatW);
            outf.Write(FireMassKG);
            outf.Write(FlueTempK);
            outf.Write(SteamGearPosition);
            outf.Write(WaterFraction);
            outf.Write(ScoopIsBroken);
            ControllerFactory.Save(CutoffController, outf);
            ControllerFactory.Save(SteamHeatController, outf);
            ControllerFactory.Save(Injector1Controller, outf);
            ControllerFactory.Save(Injector2Controller, outf);
            ControllerFactory.Save(BlowerController, outf);
            ControllerFactory.Save(DamperController, outf);
            ControllerFactory.Save(FireboxDoorController, outf);
            ControllerFactory.Save(FiringRateController, outf);
            ControllerFactory.Save(SmallEjectorController, outf);
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
            TenderCoalMassKG = inf.ReadSingle();
            TenderWaterVolumeUKG = inf.ReadSingle();
            CylinderSteamUsageLBpS = inf.ReadSingle();
            BoilerHeatBTU = inf.ReadSingle();
            BoilerMassLB = inf.ReadSingle();
            BoilerPressurePSI = inf.ReadSingle();
            WaterTempNewK = inf.ReadSingle();
            WaterFraction = inf.ReadSingle();
            EvaporationLBpS = inf.ReadSingle();
            CurrentCarriageHeatTempC = inf.ReadSingle();
            CurrentTrainSteamHeatW = inf.ReadSingle();
            FireMassKG = inf.ReadSingle();
            FlueTempK = inf.ReadSingle();
            SteamGearPosition = inf.ReadSingle();
            ScoopIsBroken = inf.ReadBoolean();
            ControllerFactory.Restore(CutoffController, inf);
            ControllerFactory.Restore(SteamHeatController, inf);
            ControllerFactory.Restore(Injector1Controller, inf);
            ControllerFactory.Restore(Injector2Controller, inf);
            ControllerFactory.Restore(BlowerController, inf);
            ControllerFactory.Restore(DamperController, inf);
            ControllerFactory.Restore(FireboxDoorController, inf);
            ControllerFactory.Restore(FiringRateController, inf);
            ControllerFactory.Restore(SmallEjectorController, inf);
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
                DriverWheelRadiusM = 1;
            if (ZeroError(MaxBoilerPressurePSI, "MaxBoilerPressure"))
                MaxBoilerPressurePSI = 1;
            if (ZeroError(BoilerVolumeFT3, "BoilerVolume"))
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

            CylinderCondensationFractionX = SteamTable.CylinderCondensationFractionInterpolatorX();
            CylinderCondensationFactorSpeed = SteamTable.CylinderCondensationSimpleSpeedAdjRpMtoX();
            SuperheatTempLimitXtoDegF = SteamTable.SuperheatTempLimitInterpolatorXtoDegF();
            SuperheatTempLbpHtoDegF = SteamTable.SuperheatTempInterpolatorLbpHtoDegF();
            SteamChestPressureDropRatioRpMtoX = SteamTable.SteamChestPressureDropRatioInterpolatorRpMtoX();

            SteamEjectorSteamUsageLBpHtoPSI = SteamTable.EjectorSteamConsumptionLbspHtoPSI();
            SteamEjectorCapacityFactorIntoX = SteamTable.EjectorCapacityFactorIntoX();

            SaturatedSpeedFactorSpeedDropFtpMintoX = SteamTable.SaturatedSpeedFactorSpeedDropFtpMintoX();
            SuperheatedSpeedFactorSpeedDropFtpMintoX = SteamTable.SuperheatedSpeedFactorSpeedDropFtpMintoX();

            CutoffInitialPressureDropRatioUpper = SteamTable.CutoffInitialPressureUpper();
            CutoffInitialPressureDropRatioLower = SteamTable.CutoffInitialPressureLower();

            // Assign default steam table values if table not in ENG file
            if (BoilerEfficiencyGrateAreaLBpFT2toX == null)
            {
                BoilerEfficiencyGrateAreaLBpFT2toX = SteamTable.BoilerEfficiencyGrateAreaInterpolatorLbstoX();
                Trace.TraceInformation("BoilerEfficiencyGrateAreaLBpFT2toX - default information read from SteamTables");
            }

            // Assign default steam table values if table not in ENG file
            if (InitialPressureDropRatioRpMtoX == null)
            {
                InitialPressureDropRatioRpMtoX = SteamTable.InitialPressureDropRatioInterpolatorRpMtoX();
                Trace.TraceInformation("InitialPressureDropRatioRpMtoX - default information read from SteamTables");
            }

            // Assign default steam table values if table not in ENG file
            if (NewBurnRateSteamToCoalLbspH == null)
            {
                NewBurnRateSteamToCoalLbspH = SteamTable.NewBurnRateSteamToCoalLbspH();
                Trace.TraceInformation("BurnRateSteamToCoalLbspH - default information read from SteamTables");
            }

            // Check Cylinder efficiency rate to see if set - allows user to improve cylinder performance and reduce losses

            if (CylinderEfficiencyRate == 0)
            {
                CylinderEfficiencyRate = 1.0f; // If no cylinder efficiency rate in the ENG file set to mormal (1.0)
            }

            // Determine if Cylinder Port Opening  Factor has been set
            if (CylinderPortOpeningFactor == 0)
            {
                CylinderPortOpeningFactor = 0.085f; // Set as default if not specified
            }
            CylinderPortOpeningFactor = MathHelper.Clamp(CylinderPortOpeningFactor, 0.05f, 0.12f); // Clamp Cylinder Port Opening Factor to between 0.05 & 0.12 so that tables are not exceeded   

            // Initialise exhaust opening point on cylinder stroke, and its reciprocal compression close factor
            if (CylinderExhaustOpenFactor == 0)
            {
                CylinderExhaustOpenFactor = CutoffController.MaximumValue + 0.025f; // If no value in ENG file set to default value based upon maximum cutoff value
                CylinderCompressionCloseFactor = 1.0f - CylinderExhaustOpenFactor; // Point on cylinder stroke when compression valve closes - assumed reciporical of exhaust opens.
                if (CutoffController.MaximumValue > CylinderExhaustOpenFactor)
                {
                    Trace.TraceWarning("Maximum Cutoff {0} value is greater then CylinderExhaustOpenFactor {1}", CutoffController.MaximumValue, CylinderExhaustOpenFactor); // provide warning if exhaust port is likely to open before maximum allowed cutoff value is reached.
                }
            }
            else
            {
                if (CutoffController.MaximumValue > CylinderExhaustOpenFactor)
                {
                    CylinderExhaustOpenFactor = CutoffController.MaximumValue + 0.05f; // Ensure exhaust valve opening is always higher then specificed maximum cutoff value
                    Trace.TraceWarning("Maximum Cutoff {0} value is greater then CylinderExhaustOpenFactor {1}, automatically adjusted", CutoffController.MaximumValue, CylinderExhaustOpenFactor); // provide warning if exhaust port is likely to open before maximum allowed cutoff value is reached.
                }
                CylinderCompressionCloseFactor = 1.0f - CylinderExhaustOpenFactor; // Point on cylinder stroke when compression valve closes - assumed reciporical of exhaust opens.
            }
            CylinderExhaustOpenFactor = MathHelper.Clamp(CylinderExhaustOpenFactor, 0.5f, 0.95f); // Clamp Cylinder Exhaust Port Opening Factor to between 0.5 & 0.95 so that tables are not exceeded   

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
                    // Calculate maximum locomotive speed - based upon the number of revs for the drive shaft, geared to wheel shaft, and then circumference of drive wheel
                    // Max Geared speed = ((MaxPistonSpeedFt/m / Gear Ratio) x DrvWheelCircumference) / Feet in mile - miles per min
                    LowMaxGearedSpeedMpS = pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatio * MathHelper.Pi * DriverWheelRadiusM * 2.0f);
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2.0f * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
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
                        SteamGearRatioLow = 5.0f;
                        Trace.TraceWarning("SteamGearRatioLow not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }
                    if (SteamGearRatioHigh == 0)
                    {
                        SteamGearRatioHigh = 2.0f;
                        Trace.TraceWarning("SteamGearRatioHigh not found in ENG file, or doesn't appear to be a valid figure, and has been set to default value");
                    }

                    MotiveForceGearRatio = 0.0f; // assume in neutral gear as starting position
                    SteamGearRatio = 0.0f;   // assume in neutral gear as starting position
                    // Calculate maximum locomotive speed - based upon the number of revs for the drive shaft, geared to wheel shaft, and then circumference of drive wheel
                    // Max Geared speed = ((MaxPistonSpeed / Gear Ratio) x DrvWheelCircumference) / Feet in mile - miles per min
                    LowMaxGearedSpeedMpS = MpS.ToMpH(Me.FromFt(pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatioLow))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                    HighMaxGearedSpeedMpS = MpS.ToMpH(Me.FromFt(pS.FrompM(MaxSteamGearPistonRateFtpM / SteamGearRatioHigh))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                }
                else
                {
                    SteamLocoType = "Unknown Geared locomotive (default to non-gear)";
                    // Default to non-geared locomotive
                    MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                    SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                    MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
                }
            }
            else if (SteamEngineType == SteamEngineTypes.Simple)    // Simple locomotive
            {
                SteamLocoType = "Simple locomotive";
                MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;
            }
            else // Default to Simple Locomotive (Assumed Simple) shows up as "Unknown"
            {
                Trace.TraceWarning("Steam engine type parameter not formally defined. Simple locomotive has been assumed");
                SteamLocoType = "Not formally defined (assumed simple) locomotive.";
              //  SteamEngineType += "Simple";
                MotiveForceGearRatio = 1.0f;  // set gear ratio to default, as not a geared locomotive
                SteamGearRatio = 1.0f;     // set gear ratio to default, as not a geared locomotive
                MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio * CylinderEfficiencyRate;

            }


            // ******************  Test Boiler Type *********************  
            InitializeTenderWithCoal();
            InitializeTenderWithWater();

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
            float GrateLimitLBpFt2add = 162.0f;     // Alow burn rate to slightly exceed grate limit
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

            CylinderSteamUsageLBpS = 1.0f;  // Set to 1 to ensure that there are no divide by zero errors
            WaterFraction = 0.9f;  // Initialise boiler water level at 90%

            if (BoilerEvapRateLbspFt2 == 0) // If boiler evaporation rate is not in ENG file then set a default value
            {
                BoilerEvapRateLbspFt2 = 15.0f; // Default rate for evaporation rate. Assume a default rate of 15 lbs/sqft of evaporation area
            }
            BoilerEvapRateLbspFt2 = MathHelper.Clamp(BoilerEvapRateLbspFt2, 10.0f, 15.0f); // Clamp BoilerEvap Rate to between 10 & 15
            TheoreticalMaxSteamOutputLBpS = pS.FrompH(Me2.ToFt2(EvaporationAreaM2) * BoilerEvapRateLbspFt2); // set max boiler theoretical steam output

            float BoilerVolumeCheck = Me2.ToFt2(EvaporationAreaM2) / BoilerVolumeFT3;    //Calculate the Boiler Volume Check value.
            if (BoilerVolumeCheck > 15) // If boiler volume is not in ENG file or less then a viable figure (ie high ratio figure), then set to a default value
            {
                BoilerVolumeFT3 = Me2.ToFt2(EvaporationAreaM2) / 8.3f; // Default rate for evaporation rate. Assume a default ratio of evaporation area * 1/8.3
                Trace.TraceWarning("Boiler Volume not found in ENG file, or doesn't appear to be a valid figure, and has been set to {0} Ft^3", BoilerVolumeFT3); // Advise player that Boiler Volume is missing from or incorrect in ENG file
            }

            MaxBoilerHeatPressurePSI = MaxBoilerPressurePSI + SafetyValveStartPSI + 5.0f; // set locomotive maximum boiler pressure to calculate max heat, allow for safety valve + a bit
            MaxBoilerPressHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerHeatPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerHeatPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerHeatPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerHeatPressurePSI];  // calculate the maximum possible heat in the boiler, assuming safety valve and a small margin
            MaxBoilerHeatBTU = WaterFraction * BoilerVolumeFT3 * WaterDensityPSItoLBpFT3[MaxBoilerPressurePSI] * WaterHeatPSItoBTUpLB[MaxBoilerPressurePSI] + (1 - WaterFraction) * BoilerVolumeFT3 * SteamDensityPSItoLBpFT3[MaxBoilerPressurePSI] * SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI];  // calculate the maximum possible heat in the boiler

            MaxBoilerKW = Kg.FromLb(TheoreticalMaxSteamOutputLBpS) * W.ToKW(W.FromBTUpS(SteamHeatPSItoBTUpLB[MaxBoilerPressurePSI]));
            MaxFlueTempK = (MaxBoilerKW / (W.ToKW(BoilerHeatTransferCoeffWpM2K) * EvaporationAreaM2 * HeatMaterialThicknessFactor)) + baseTempK;

            // Determine if Superheater in use

            if (HasSuperheater)
            {
                SteamLocoType += " + Superheater";

                // Calculate superheat steam reference temperature based upon heating area of superheater
                // SuperTemp = (SuperHeatArea x HeatTransmissionCoeff * (MeanGasTemp - MeanSteamTemp)) / (SteamQuantity * MeanSpecificSteamHeat)
                // Formula has been simplified as follows: SuperTemp = (SuperHeatArea x FlueTempK x SFactor) / SteamQuantity
                // SFactor is a "loose reprentation" =  (HeatTransmissionCoeff / MeanSpecificSteamHeat) - Av figure calculate by comparing a number of "known" units for superheat.
                SuperheatRefTempF = (Me2.ToFt2(SuperheatAreaM2) * C.ToF(C.FromK(MaxFlueTempK)) * SuperheatKFactor) / pS.TopH(TheoreticalMaxSteamOutputLBpS);
                SuperheatTempRatio = SuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];    // calculate a ratio figure for known value against reference curve. 
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
                SuperheatRefTempF = 200.0f; // Assume a superheating temp of 250degF
                SuperheatTempRatio = SuperheatRefTempF / SuperheatTempLbpHtoDegF[pS.TopH(TheoreticalMaxSteamOutputLBpS)];
                SuperheatAreaM2 = Me2.FromFt2((SuperheatRefTempF * pS.TopH(TheoreticalMaxSteamOutputLBpS)) / (C.ToF(C.FromK(MaxFlueTempK)) * SuperheatKFactor)); // Back calculate Superheat area for display purposes only.
                CylinderClearancePC = 0.09f;
            }
            else // Default to saturated type of locomotive
            {
                SteamLocoType += " + Not formally defined (assumed saturated)";
                SuperheatRefTempF = 0.0f;
            }

            MaxBoilerOutputLBpH = pS.TopH(TheoreticalMaxSteamOutputLBpS);

            // Assign default steam table values if table not in ENG file 
            // Back pressure increases with the speed of the locomotive, as cylinder finds it harder to exhaust all the steam.

            if (BackPressureIHPtoAtmPSI == null)
            {
                if (HasSuperheater)
                {
                    BackPressureIHPtoAtmPSI = SteamTable.BackpressureSuperIHPtoPSI();
                    Trace.TraceInformation("BackPressureIHPtoAtmPSI (Superheated) - default information read from SteamTables");
                }
                else
                {
                    BackPressureIHPtoAtmPSI = SteamTable.BackpressureSatIHPtoPSI();
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
            if (SteamGearRatio == 0)
            {
                MaxLocoSpeedMpH = 0.0f;
            }
            else
            {
                MaxLocoSpeedMpH = MpS.ToMpH(Me.FromFt(pS.FrompM(MaxPistonSpeedFtpM / SteamGearRatio))) * 2.0f * MathHelper.Pi * DriverWheelRadiusM / (2.0f * CylinderStrokeM);
            }

            // If DrvWheelWeight is not in ENG file, then calculate from Factor of Adhesion(FoA) = DrvWheelWeight / Start (Max) Tractive Effort, assume FoA = 4.2

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                const float FactorofAdhesion = 4.2f; // Assume a typical factor of adhesion
                DrvWheelWeightKg = Kg.FromLb(FactorofAdhesion * MaxTractiveEffortLbf); // calculate Drive wheel weight if not in ENG file
                DrvWheelWeightKg = MathHelper.Clamp(DrvWheelWeightKg, 0.1f, MassKG); // Make sure adhesive weight does not exceed the weight of the locomotive
            }

            // Calculate factor of adhesion for display purposes

            CalculatedFactorofAdhesion = Kg.ToLb(DrvWheelWeightKg) / MaxTractiveEffortLbf;

            // Calculate "critical" power of locomotive to determine limit of max IHP
            MaxCriticalSpeedTractiveEffortLbf = (MaxTractiveEffortLbf * CylinderEfficiencyRate) * MaxSpeedFactor;
            DisplayCriticalSpeedTractiveEffortLbf = MaxCriticalSpeedTractiveEffortLbf;

            #endregion

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

            float MaxCombustionRateKgpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH(TheoreticalMaxSteamOutputLBpS)]));

            // Calculate the maximum boiler heat input based on the steam generation rate

            MaxBoilerHeatInBTUpS = W.ToBTUpS(W.FromKW(MaxCombustionRateKgpS * FuelCalorificKJpKG * BoilerEfficiencyGrateAreaLBpFT2toX[pS.TopH(Kg.ToLb(MaxCombustionRateKgpS)) / GrateAreaM2]));

            #region Initialise Locomotive in a Hot or Cold Start Condition

            // Initialise Steam Heating in Train

            // Checks to see if winter or autumn only?
            if (Simulator.Season == SeasonType.Winter)
            {
                // Winter temps
                InsideTempC = 15.5f;  // Assume a desired temperature of 60oF = 15.5oC
                OutsideTempC = -10.0f;
            }
            else if (Simulator.Season == SeasonType.Autumn || Simulator.Season == SeasonType.Spring)
            {
                // Sping / Autumn temps
                InsideTempC = 15.5f;  // Assume a desired temperature of 60oF = 15.5oC
                OutsideTempC = 0.0f;
            }
            else
            {
                // Summer temps
                InsideTempC = 15.5f;  // Assume a desired temperature of 60oF = 15.5oC
                OutsideTempC = 15.0f;
            }
            //         CarriageHeatTempC = InsideTempC; //Assume starting temp based upon season

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
                // Set steam heat to a hot value
                CurrentCarriageHeatTempC = InsideTempC;
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
                // Set steam heat to a cold value
                CurrentCarriageHeatTempC = OutsideTempC;
            }

            if (MaxSteamHeatPressurePSI == 0 || Simulator.Season == SeasonType.Summer)       // Check to see if steam heating is fitted to locomotive, if summer disable steam heating
            {
                IsSteamHeatFitted = false;
                CalculatedCarHeaterSteamUsageLBpS = 0.0f;       // Set to zero by default
            }
            else
            {
                IsSteamHeatFitted = true;
            }

#if DEBUG_LOCO_STEAM_HEAT

            Trace.TraceInformation("***************************************** DEBUG_LOCO_STEAM_HEAT (MSTSSteamLocomotive.cs)  #Initial ***************************************************************");
            Trace.TraceInformation("Inside Temp {0} Outside Temp {1} Current Car Temp {2} Current Train Steam Heat {3}", InsideTempC, OutsideTempC, CurrentCarriageHeatTempC, CurrentTrainSteamHeatW);
            Trace.TraceInformation("SteamHeat: Pressure {0} Fitted {1}", MaxSteamHeatPressurePSI, IsSteamHeatFitted);
#endif


            DamperFactorManual = TheoreticalMaxSteamOutputLBpS / SpeedEquivMpS; // Calculate a factor for damper control that will vary with speed.
            BlowerSteamUsageFactor = 0.04f * MaxBoilerOutputLBpH / 3600 / MaxBoilerPressurePSI;
            WaterTempNewK = C.ToK(C.FromF(PressureToTemperaturePSItoF[BoilerPressurePSI])); // Initialise new boiler pressure
            FireMassKG = IdealFireMassKG;
            if (MaxFiringRateKGpS == 0)
                MaxFiringRateKGpS = 180 * MaxBoilerOutputLBpH / 775 / 3600 / 2.2046f;

            if (ORTSMaxFiringRateKGpS != 0)
                MaxFiringRateKGpS = ORTSMaxFiringRateKGpS; // If OR value present then use it 

            // Initialise Mechanical Stoker if present
            if (Stoker == 1.0f)
            {
                StokerIsMechanical = true;
                MaxFiringRateKGpS = 2 * MaxFiringRateKGpS; // Temp allowance for mechanical stoker
            }
            MaxTheoreticalFiringRateKgpS = MaxFiringRateKGpS * 1.33f; // allow the fireman to overfuel for short periods of time 
            #endregion

            ApplyBoilerPressure();

            AuxPowerOn = true;
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
            UpdateSteamHeat(elapsedClockSeconds);
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


            StackSteamVelocityMpS.Update(elapsedClockSeconds, (float)Math.Sqrt(KPa.FromPSI(CylinderReleasePressureAtmPSI) * 1000 * 2 / WaterDensityAt100DegC1BarKGpM3));
            StackSteamVolumeM3pS = Kg.FromLb(CylinderSteamUsageLBpS + BlowerSteamUsageLBpS + RadiationSteamLossLBpS + CompSteamUsageLBpS + GeneratorSteamUsageLBpS) * SteamVaporSpecVolumeAt100DegC1BarM3pKG;
            float SmokeColorUnits = (RadiationSteamLossLBpS + CalculatedCarHeaterSteamUsageLBpS + BlowerBurnEffect + (SmokeColorDamper * SmokeColorFireMass)) / PreviousTotalSteamUsageLBpS - 0.2f;
            SmokeColor.Update(elapsedClockSeconds, MathHelper.Clamp(SmokeColorUnits, 0.25f, 1));

            //  Trace.TraceInformation("Smoke: Rad {0} CarHeat {1} Blower {2} Damper {3} Fire Mass {4} Prev {5} Total {6}", RadiationSteamLossLBpS, CalculatedCarHeaterSteamUsageLBpS, BlowerBurnEffect, SmokeColorDamper, SmokeColorFireMass, PreviousTotalSteamUsageLBpS, SmokeColorUnits);

            // Variable1 is proportional to angular speed, value of 10 means 1 rotation/second.
         //   var variable1 = (Simulator.UseAdvancedAdhesion && Train.IsPlayerDriven ? LocomotiveAxle.AxleSpeedMpS : SpeedMpS) / DriverWheelRadiusM / MathHelper.Pi * 5;
            var variable1 = WheelSpeedSlipMpS / DriverWheelRadiusM / MathHelper.Pi * 5;
            Variable1 = ThrottlePercent == 0 ? 0 : variable1;
            Variable2 = MathHelper.Clamp((CylinderPressureAtmPSI - OneAtmospherePSI) / BoilerPressurePSI * 100f, 0, 100);
            Variable3 = FuelRateSmooth * 100;

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
                if (SteamHeatController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeat, CabSetting.Increase, SteamHeatController.CurrentValue * 100);
                if (SteamHeatController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeat, CabSetting.Decrease, SteamHeatController.CurrentValue * 100);
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

                if (SmallEjectorController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Increase, SmallEjectorController.CurrentValue * 100);
                if (SmallEjectorController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Decrease, SmallEjectorController.CurrentValue * 100);
            
            }

            SteamHeatController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (SteamHeatController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeat, CabSetting.Increase, SteamHeatController.CurrentValue * 100);
                if (SteamHeatController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeat, CabSetting.Decrease, SteamHeatController.CurrentValue * 100);
            }

            SmallEjectorController.Update(elapsedClockSeconds);
            if (IsPlayerTrain)
            {
                if (SmallEjectorController.UpdateValue > 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Increase, SmallEjectorController.CurrentValue * 100);
                if (SmallEjectorController.UpdateValue < 0.0)
                    Simulator.Confirmer.UpdateWithPerCent(CabControl.SmallEjector, CabSetting.Decrease, SmallEjectorController.CurrentValue * 100);
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

            if (RefillingFromTrough && !IsOverTrough())
            {
                // Bad thing, scoop gets broken!
                Simulator.Confirmer.Message(ConfirmLevel.Error, Simulator.Catalog.GetString("Scoop broken because activated outside through"));
                WaterController.UpdateValue = 0.0f;
                RefillingFromTrough = false;
                SignalEvent(Event.WaterScoopUp);
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
            TenderCoalMassKG -= elapsedClockSeconds * FuelBurnRateKGpS; // Current Tender coal mass determined by burn rate.
            TenderCoalMassKG = MathHelper.Clamp(TenderCoalMassKG, 0, MaxTenderCoalMassKG); // Clamp value so that it doesn't go out of bounds
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

            #region Auxiliary Tender Operation

            // If aux tender is coupled then assume that both tender and aux tender will equalise at same % water level
            // Tender water level will be monitored, and aux tender adjusted based upon this level
            // Add the value of water in the auxiliary tender to the tender water.
            // If Aux tender is uncoupled then assume that the % water level is the same in both the tender and the aux tender before uncoupling. Therefore calculate tender water based upon controller value and max tender water value.
            if (Train.IsAuxTenderCoupled)
            {
                if (SteamIsAuxTenderCoupled == false)
                {
                    CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(CurrentAuxTenderWaterMassKG) / WaterLBpUKG);  // Adjust water volume due to aux tender being connected
                    SteamIsAuxTenderCoupled = true;
                    // If water levels are different in the tender compared to the aux tender, then equalise them
                    float MaxTotalCombinedWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) + (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG);
                    float CurrentTotalWaterVolumeUKG = CurrentAuxTenderWaterVolumeUKG + TenderWaterVolumeUKG;
                    float CurrentTotalWaterPercent = CurrentTotalWaterVolumeUKG / MaxTotalCombinedWaterVolumeUKG;
                    // Calculate new water volumes in both the tender and aux tender
                    CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) * CurrentTotalWaterPercent;
                    TenderWaterVolumeUKG = (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG) * CurrentTotalWaterPercent;
                }
            }
            else
            {
                if (SteamIsAuxTenderCoupled == true)  // When aux tender uncoupled adjust water in tender to remaining percentage.
                {
                    TenderWaterVolumeUKG = (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG) * WaterController.CurrentValue;  // Adjust water volume due to aux tender being uncoupled, adjust remaining tender water to whatever % value should be in tender
                    TenderWaterVolumeUKG = MathHelper.Clamp(TenderWaterVolumeUKG, 0, (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG)); // Clamp value so that it doesn't go out of bounds
                    CurrentAuxTenderWaterVolumeUKG = 0.0f;
                    SteamIsAuxTenderCoupled = false;
                }
            }

            // If refilling, as determined by increasing tender water level, then adjust aux tender water level at the same rate as the tender
            if (TenderWaterVolumeUKG > PreviousTenderWaterVolumeUKG)
            {
                CurrentAuxTenderWaterVolumeUKG = (Kg.ToLb(Train.MaxAuxTenderWaterMassKG * WaterController.CurrentValue) / WaterLBpUKG);  // Adjust water volume due to aux tender being connected
                CurrentAuxTenderWaterVolumeUKG = MathHelper.Clamp(CurrentAuxTenderWaterVolumeUKG, 0, (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG)); // Clamp value so that it doesn't go out of bounds
            }

            CombinedTenderWaterVolumeUKG = TenderWaterVolumeUKG + CurrentAuxTenderWaterVolumeUKG;
            CombinedTenderWaterVolumeUKG = MathHelper.Clamp(CombinedTenderWaterVolumeUKG, 0, ((Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG) + (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG))); // Clamp value so that it doesn't go out of bounds
            TenderWaterChangePercent = (InjectorBoilerInputLB / WaterLBpUKG) / (Kg.ToLb(MaxTenderWaterMassKG + Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG);  // Calculate the % change due to injector water usage
            TenderWaterVolumeUKG -= CombinedTenderWaterVolumeUKG * TenderWaterChangePercent;  // Adjust water usage in tender
            CurrentAuxTenderWaterVolumeUKG -= CombinedTenderWaterVolumeUKG * TenderWaterChangePercent; // Adjust water usage in aux tender
            PrevCombinedTenderWaterVolumeUKG = CombinedTenderWaterVolumeUKG;   // Store value for next iteration
            PreviousTenderWaterVolumeUKG = TenderWaterVolumeUKG;     // Store value for next iteration            


#if DEBUG_AUXTENDER

            Trace.TraceInformation("============================================= Aux Tender (MSTSSTeamLocomotive.cs) =========================================================");
         //   Trace.TraceInformation("Water Level Is set by act {0}", Simulator.WaterInitialIsSet);
            Trace.TraceInformation("Combined Tender Water {0}", CombinedTenderWaterVolumeUKG);
            Trace.TraceInformation("Tender Water {0} Max Tender Water {1}  Max Aux Tender {2}", TenderWaterVolumeUKG, (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG), (Kg.ToLb(Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG));
            Trace.TraceInformation(" Water Percent {0} AuxTenderCoupled {1} SteamAuxTenderCoupled {2}", TenderWaterChangePercent, Train.IsAuxTenderCoupled, SteamIsAuxTenderCoupled);
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
                BurnRateRawKGpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH((RadiationSteamLossLBpS + CalculatedCarHeaterSteamUsageLBpS) + BlowerBurnEffect + DamperBurnEffect)])); // Manual Firing - note steam usage due to safety valve, compressor and steam cock operation not included, as these are factored into firemans calculations, and will be adjusted for manually - Radiation loss divided by factor of 5.0 to reduce the base level - Manual fireman to compensate as appropriate.
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

                if (ShovelAnyway) // will force fire burn rate to increase even though boiler heat seems excessive - activated at full throttle, and on rising gradient
                {
                    // burnrate will be the radiation loss @ rest & then related to heat usage as a factor of the maximum boiler output - ignores total bolier heat to allow burn rate to increase if boiler heat usage is exceeding input
                    BurnRateRawKGpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH((BlowerBurnEffect + HeatRatio * FiringSteamUsageRateLBpS * PressureRatio))]));
                }
                else
                {
                    // burnrate will be the radiation loss @ rest & then related to heat usage as a factor of the maximum boiler output - normal operation - reduces burn rate if boiler heat is excessive.
                    BurnRateRawKGpS = pS.FrompH(Kg.FromLb(NewBurnRateSteamToCoalLbspH[pS.TopH((BlowerBurnEffect + HeatRatio * FiringSteamUsageRateLBpS * PressureRatio * BoilerHeatRatio * MaxBoilerHeatRatio))]));
                }

                //  Limit burn rate in AI fireman to within acceptable range of Fireman firing rate
                //        BurnRateRawKGpS = MathHelper.Clamp(BurnRateRawKGpS, 0.05f, MaxTheoreticalFiringRateKgpS); // Allow burnrate to max out at MaxTheoreticalFiringRateKgpS
                BurnRateRawKGpS = MathHelper.Clamp(BurnRateRawKGpS, 0.001f, MaxFuelBurnGrateKGpS); // Allow burnrate to max out at MaxTheoreticalFiringRateKgpS
            }

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

            FireRatio = FireMassKG / IdealFireMassKG;
            if (absSpeedMpS == 0)
                BurnRateRawKGpS *= FireRatio * 0.2f; // reduce background burnrate if stationary
            else if (FireRatio < 1.0f)  // maximise burnrate when FireMass = IdealFireMass, else allow a reduction in efficiency
                BurnRateRawKGpS *= FireRatio;
            else
                BurnRateRawKGpS *= 2 - FireRatio;

            // test for fusible plug
            if (FusiblePlugIsBlown)
            {
                BurnRateRawKGpS = 0.0f; // Drop fire due to melting of fusible plug and steam quenching fire, change later to allow graduate ramp down.
            }

            BurnRateSmoothKGpS.Update(elapsedClockSeconds, BurnRateRawKGpS);
            FuelBurnRateKGpS = BurnRateSmoothKGpS.SmoothedValue;
            //   FuelBurnRateKGpS = MathHelper.Clamp(FuelBurnRateKGpS, 0.0f, MaxFireMassKG); // clamp burnrate to maintain it within limits  MaxFuelBurnGrateKGpS
            FuelBurnRateKGpS = MathHelper.Clamp(FuelBurnRateKGpS, 0.0f, MaxFuelBurnGrateKGpS); // clamp burnrate to maintain it within limits
            #endregion

            #region Firing (feeding fuel) Rate of locomotive

            if (FiringIsManual)
            {
                FuelRateSmooth = CoalIsExhausted ? 0 : FiringRateController.CurrentValue;
                FuelFeedRateKGpS = MaxFiringRateKGpS * FuelRateSmooth;
            }
            else if (elapsedClockSeconds > 0.001 && MaxFiringRateKGpS > 0.001)
            {
                // Automatic fireman, ish.
                DesiredChange = MathHelper.Clamp(((IdealFireMassKG - FireMassKG) + FuelBurnRateKGpS) / MaxFiringRateKGpS, 0.001f, 1);
                if (StokerIsMechanical) // if a stoker is fitted expect a quicker response to fuel feeding
                {
                    FuelRateStoker.Update(elapsedClockSeconds, DesiredChange); // faster fuel feed rate for stoker    
                    FuelRateSmooth = CoalIsExhausted ? 0 : FuelRateStoker.SmoothedValue; // If tender coal is empty stop fuelrate (feeding coal onto fire). 
                }
                else
                {
                    FuelRate.Update(elapsedClockSeconds, DesiredChange); // slower fuel feed rate for fireman
                    FuelRateSmooth = CoalIsExhausted ? 0 : FuelRate.SmoothedValue; // If tender coal is empty stop fuelrate (feeding coal onto fire).
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
                        //  FuelBoostReset = false; // Reset boost timer
                        if (!StokerIsMechanical && IsPlayerTrain)  // Don't display message if stoker in operation
                        {
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("FireMass is back within limits. Your fireman will shovel as per normal."));
                        }
                    }
                }
                if (FuelBoost && !FuelBoostReset) // if fuel boost is still on, and hasn't been reset - needs further refinement as this shouldn't be able to be maintained indefinitely
                {
                    DisplayMaxFiringRateKGpS = MaxTheoreticalFiringRateKgpS; // Set display value with temporary higher shovelling level
                    FuelFeedRateKGpS = MaxTheoreticalFiringRateKgpS * FuelRateSmooth;  // At times of heavy burning allow AI fireman to overfuel
                    FuelBoostOnTimerS += elapsedClockSeconds; // Time how long to fuel boost for
                }
                else
                {
                    DisplayMaxFiringRateKGpS = MaxFiringRateKGpS; // Rest display max firing rate to new figure
                    FuelFeedRateKGpS = MaxFiringRateKGpS * FuelRateSmooth;
                }
            }
            // Calculate update to firemass as a result of adding fuel to the fire
            FireMassKG += elapsedClockSeconds * (FuelFeedRateKGpS - FuelBurnRateKGpS);
            FireMassKG = MathHelper.Clamp(FireMassKG, 0, MaxFireMassKG);
            GrateCombustionRateLBpFt2 = pS.TopH(Kg.ToLb(FuelBurnRateKGpS) / Me2.ToFt2(GrateAreaM2)); //coal burnt per sq ft grate area per hour
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

            #endregion

            if (FiringIsManual)
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

                if (BoilerHeatExcess > 1.075 && !ShovelAnyway)  // turn safety valves on if boiler heat is excessive, and fireman is not trying to raise steam for rising gradient
                {
                    SignalEvent(Event.SteamSafetyValveOn);
                    SafetyIsOn = true;
                }

                else if (BoilerHeatExcess < 1.02 || ShovelAnyway)  // turn safety vales off if boiler heat has returned to "normal", or fireman is trying to raise steam for rising gradient.
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
                }
                else
                {
                    SafetyValveUsageLBpS = 0.0f; // if safety valve closed, then zero discharge rate
                }

                #endregion

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
                    float BoilerHeatDiffValue = MaxBoilerHeatBTU * (MaxBoilerPressurePSI * 0.025f); // Set a differenntial to reset boilerheat flag - Allow a 2.5% drop in boiler pressure
                    if (BoilerHeatBTU < BoilerHeatDiffValue)
                    {
                        BoilerHeat = false;
                        BoilerHeatRatio = 1.0f;
                    }
                }
                BoilerHeatRatio = MathHelper.Clamp(BoilerHeatRatio, 0.001f, 1.0f); // Keep Boiler Heat ratio within bounds

                if (BoilerHeatBTU > MaxBoilerPressHeatBTU)  // Limit boiler heat further if heat excced the pressure that the safety valves are set to.
                {
                    float FactorPower = BoilerHeatBTU / (MaxBoilerPressHeatBTU - MaxBoilerHeatBTU);
                    MaxBoilerHeatRatio = MaxBoilerHeatBTU / (float)Math.Pow(BoilerHeatBTU, FactorPower);
                }
                else
                {
                    MaxBoilerHeatRatio = 1.0f;
                }
                MaxBoilerHeatRatio = MathHelper.Clamp(MaxBoilerHeatRatio, 0.001f, 1.0f); // Keep Max Boiler Heat ratio within bounds
            }

            // Limit Boiler heat input once grate limit is reached.
            if (GrateCombustionRateLBpFt2 > GrateLimitLBpFt2)
            {
                FireHeatTxfKW = PreviousFireHeatTxfKW; // if greater then grate limit don't allow any more heat txf
                if (!IsGrateLimit)  // Provide message to player that grate limit has been exceeded
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Grate limit exceeded - boiler heat rate cannot increase."));
                }
                IsGrateLimit = true;
            }
            else
            {
                //      FireHeatTxfKW = FuelBurnRateKGpS * FuelCalorificKJpKG * BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateKGpS)) / Me2.ToFt2(GrateAreaM2))] / (SpecificHeatCoalKJpKGpK * FireMassKG); // Current heat txf based on fire burning rate 
                FireHeatTxfKW = FuelCalorificKJpKG * FuelBurnRateKGpS;
                if (IsGrateLimit)  // Provide message to player that grate limit has now returned within limits
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Grate limit return to normal."));
                }
                IsGrateLimit = false;
            }

            PreviousFireHeatTxfKW = FireHeatTxfKW; // store the last value of FireHeatTxfKW

            BoilerHeatInBTUpS = W.ToBTUpS(W.FromKW(FireHeatTxfKW)) * BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateKGpS)) / Me2.ToFt2(GrateAreaM2))];
            BoilerHeatBTU += elapsedClockSeconds * W.ToBTUpS(W.FromKW(FireHeatTxfKW)) * BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateKGpS)) / Me2.ToFt2(GrateAreaM2))];

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
            WaterFraction = MathHelper.Clamp(WaterFraction, 0.0f, 1.01f); // set water fraction limits so that it doesn't go below zero or exceed full boiler volume

            // Update Boiler Heat based upon current Evaporation rate
            // Based on formula - BoilerCapacity (btu/h) = (SteamEnthalpy (btu/lb) - EnthalpyCondensate (btu/lb) ) x SteamEvaporated (lb/h) ?????
            // EnthalpyWater (btu/lb) = BoilerCapacity (btu/h) / SteamEvaporated (lb/h) + Enthalpysteam (btu/lb)  ?????

            BoilerHeatSmoothBTU.Update(elapsedClockSeconds, BoilerHeatBTU);

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
                BoilerPressurePSI = 0.50f; // Drop boiler pressure if fusible plug melts.
            }
            else
            {
                BoilerPressurePSI = SaturationPressureKtoPSI[WaterTempNewK]; // Gauge Pressure
            }

            if (!FiringIsManual)
            {
                if (BoilerHeat)
                {
                    PressureRatio = MathHelper.Clamp((MaxBoilerPressurePSI / BoilerPressurePSI), 0.001f, 0.99f); // Boiler pressure ratio to adjust burn rate, if maxboiler heat reached, then clamp ratio < 1.0
                }
                else
                {
                    PressureRatio = MathHelper.Clamp((MaxBoilerPressurePSI / BoilerPressurePSI), 0.001f, 1.5f); // Boiler pressure ratio to adjust burn rate
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
            DrvWheelRevRpS = absSpeedMpS / (2.0f * MathHelper.Pi * DriverWheelRadiusM);

            float DebugWheelRevs = pS.TopM(DrvWheelRevRpS);

            #region Calculation of Mean Effective Pressure of Cylinder using an Indicator Diagram type approach - Compound Locomotive - No receiver

            if (SteamEngineType == SteamEngineTypes.Compound)
            {

                // Define volume of cylinder at different points on cycle - the points align with points on indicator curve
                // Note: All LP cylinder values to be multiplied by Cylinder ratio to adjust volumes to same scale
                float HPCylinderVolumePoint_a = HPCylinderClearancePC;
                float HPCylinderVolumePoint_b = cutoff + HPCylinderClearancePC;
                float HPCylinderVolumePoint_d = CylinderExhaustOpenFactor + HPCylinderClearancePC;
                float HPCylinderVolumePoint_e = CylinderExhaustOpenFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP;
                float HPCylinderVolumePoint_eHPonly = CylinderExhaustOpenFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP; // Volume @ e only in HP Cylinder only
                float HPCylinderVolumePoint_f = HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP;
                float HPCylinderVolumePoint_fHPonly = HPCylinderVolumeFactor + HPCylinderClearancePC; // Volume @ f only in HP Cylinder only
                float LPCylinderVolumePoint_g = HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP + (LPCylinderClearancePC * CompoundCylinderRatio);
                float LPCylinderVolumePoint_h_pre = cutoff + HPCylinderClearancePC + CompoundRecieverVolumePCHP + ((LPCylinderClearancePC + cutoff) * CompoundCylinderRatio); // before cutoff
                float LPCylinderVolumePoint_h_LPpost = (cutoff + LPCylinderClearancePC) * CompoundCylinderRatio; // in LP Cylinder post cutoff
                float HPCylinderVolumePoint_h_HPpost = cutoff + HPCylinderClearancePC + CompoundRecieverVolumePCHP;   // in HP Cylinder + steam passages post cutoff
                float HPCylinderVolumePoint_hHPonly = cutoff + HPCylinderClearancePC;   // in HP Cylinder only post cutoff
                float HPCylinderVolumePoint_k_pre = (HPCylinderVolumeFactor - CylinderExhaustOpenFactor) + HPCylinderClearancePC + CompoundRecieverVolumePCHP;   // Before exhaust valve closure
                float HPCylinderVolumePoint_k_post = (HPCylinderVolumeFactor - CylinderExhaustOpenFactor) + HPCylinderClearancePC;   // after exhaust valve closure
                float LPCylinderVolumePoint_l = (CylinderExhaustOpenFactor + LPCylinderClearancePC) * CompoundCylinderRatio; // in LP Cylinder post cutoff
                float LPCylinderVolumePoint_n = ((LPCylinderVolumeFactor - CylinderExhaustOpenFactor) + LPCylinderClearancePC) * CompoundCylinderRatio; // in LP Cylinder @ Release
                float LPCylinderVolumePoint_m = (LPCylinderVolumeFactor + LPCylinderClearancePC) * CompoundCylinderRatio; // in LP Cylinder @ end of stroke
                float LPCylinderVolumePoint_a = LPCylinderClearancePC * CompoundCylinderRatio;

                SteamChestPressurePSI = (throttle * SteamChestPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

                if (CylinderCompoundOn)
                {
                    // ***** Simple mode *****

                    // Steam Indicator Diagram reference - (g) 
                    LPCylinderInitialPressureAtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // calculate value based upon setting of Cylinder port opening

                    float LPCutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;

                    // Steam Indicator Diagram reference - (h) 
                    LPCylinderPreCutoffPressureAtmPSI = LPCylinderInitialPressureAtmPSI * LPCutoffPressureDropRatio;

                    // Calculate work between g) - h) - Release Expansion  (LP Cylinder)              
                    float LPCylinderLengthCutoffExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_h_LPpost - LPCylinderVolumePoint_a);
                    // For purposes of work done only consider the change in LP cylinder between stroke commencement and cutoff
                    float LPMeanPressureCutoffWireAtmPSI = (LPCylinderInitialPressureAtmPSI + LPCylinderPreCutoffPressureAtmPSI) / 2.0f;  // Find the wire drawn pressure as an average, rather then as an expansion pressure
                    float LPCylinderCutoffExpansionWorkInLbs = LPMeanPressureCutoffWireAtmPSI * LPCylinderLengthCutoffExpansionIn;

                    // LP cylinder release pressure 
                    // Steam Indicator Diagram reference - (l) 
                    float LPVolumeRatioRelease = LPCylinderVolumePoint_h_LPpost / LPCylinderVolumePoint_l;
                    LPCylinderReleasePressureAtmPSI = LPCylinderPreCutoffPressureAtmPSI * LPVolumeRatioRelease;

                    // LP Cylinder back pressure will be decreased depending upon locomotive speed
                    // Steam Indicator Diagram reference - (m) 
                    LPCylinderBackPressureAtmPSI = BackPressureIHPtoAtmPSI[IndicatedHorsePowerHP];

                    // Calculate MEP for LP Cylinder
                    //Mean pressure & work between h) - l)
                    float LPExpansionRatioRelease = 1.0f / LPVolumeRatioRelease;
                    float LPMeanPressureReleaseAtmPSI = LPCylinderPreCutoffPressureAtmPSI * ((float)Math.Log(LPExpansionRatioRelease) / (LPExpansionRatioRelease - 1.0f));
                    // Calculate work between h) - l) - LP Cylinder only
                    float LPCylinderReleaseExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_l - LPCylinderVolumePoint_h_LPpost);
                    float LPCylinderReleaseWorkInLbs = LPMeanPressureReleaseAtmPSI * LPCylinderReleaseExpansionIn;

                    // Mean pressure & work between l) - m)
                    float LPMeanPressureExhaustAtmPSI = (LPCylinderReleasePressureAtmPSI + LPCylinderBackPressureAtmPSI) / 2.0f;
                    // Calculate work between l) - m) - LP Cylinder only
                    float LPCylinderExhaustExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_l);
                    float LPCylinderExhaustWorkInLbs = LPMeanPressureExhaustAtmPSI * LPCylinderExhaustExpansionIn;

                    // Calculate work between m) - n) - LP Cylinder only
                    float LPCylinderBackPressExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_n);
                    float LPCylinderBackPressureWorkInLbs = LPCylinderBackPressureAtmPSI * LPCylinderBackPressExpansionIn;

                    // Mean pressure & work between n) - q)
                    float LPCylinderVolumeRatioPreCompression = (LPCylinderVolumePoint_n) / LPCylinderVolumePoint_a;
                    float LPMeanPressurePreCompAtmPSI = LPCylinderBackPressureAtmPSI * LPCylinderVolumeRatioPreCompression * ((float)Math.Log(LPCylinderVolumeRatioPreCompression) / (LPCylinderVolumeRatioPreCompression - 1.0f));
                    // Calculate work between n) - q) - LP Cylinder only
                    float LPCylinderLengthPreCompExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_n - LPCylinderVolumePoint_a);
                    float LPCylinderPreCompExpansionWorkInLbs = LPMeanPressurePreCompAtmPSI * LPCylinderLengthPreCompExpansionIn;

                    // Mean pressure & work between q) - g)            
                    float LPMeanPressurePreAdmAtmPSI = (LPCylinderInitialPressureAtmPSI + LPCylinderPreAdmissionPressureAtmPSI) / 2.0f;
                    // Calculate work between q) - g) - LP Cylinder only
                    float LPCylinderLengthPreAdmExpansionIn = Me.ToIn(CylinderStrokeM) * LPCylinderVolumePoint_a;
                    float LPCylinderPreAdmExpansionWorkInLbs = LPMeanPressurePreAdmAtmPSI * LPCylinderLengthPreAdmExpansionIn;


                    // Calculate total Work in LP Cylinder
                    float TotalLPCylinderWorksInLbs = LPCylinderCutoffExpansionWorkInLbs + LPCylinderReleaseWorkInLbs + LPCylinderExhaustWorkInLbs - LPCylinderBackPressureWorkInLbs - LPCylinderPreCompExpansionWorkInLbs - LPCylinderPreAdmExpansionWorkInLbs;

                    LPCylinderMEPAtmPSI = TotalLPCylinderWorksInLbs / ((LPCylinderVolumeFactor * CompoundCylinderRatio) * Me.ToIn(CylinderStrokeM));

                    LPCylinderMEPAtmPSI = MathHelper.Clamp(LPCylinderMEPAtmPSI, 0.00f, LPCylinderMEPAtmPSI); // Clamp MEP so that LP MEP does not go negative

                    HPCylinderInitialPressureAtmPSI = 0.0f;  // for sake of display zero pressure values if compound is off.
                    HPCylinderBackPressureAtmPSI = 0.0f;
                    HPCylinderReleasePressureAtmPSI = 0.0f;
                    HPCylinderReleasePressureRecvAtmPSI = 0.0f;
                    HPCylinderExhaustPressureAtmPSI = 0.0f;
                    HPCylinderCutoffPressureAtmPSI = 0.0f;
                    HPCylinderPreCompressionPressureAtmPSI = 0.0f;
                    HPCylinderMEPAtmPSI = 0.0f;

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

                    // Initial pressure will be decreased depending upon locomotive speed
                    // Steam Indicator Diagram reference - (a) 
                    HPCylinderInitialPressureAtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // calculate value based upon setting of Cylinder port opening

                    float HPCutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;

                    // Cutoff pressure also drops with locomotive speed
                    // Steam Indicator Diagram reference - (b)
                    HPCylinderCutoffPressureAtmPSI = HPCylinderInitialPressureAtmPSI * HPCutoffPressureDropRatio;

                    // Mean pressure & work between a) - b) - Admission
                    float HPMeanPressureCuttoffAtmPSI = (HPCylinderInitialPressureAtmPSI + HPCylinderCutoffPressureAtmPSI) / 2.0f;
                    // Calculate work between a) - b)
                    float HPCylinderLengthAdmissionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_b - HPCylinderVolumePoint_a);
                    float HPCylinderAdmissionWorkInLbs = HPMeanPressureCuttoffAtmPSI * HPCylinderLengthAdmissionIn;

                    // Release pressure - occurs when the exhaust valve opens to release steam from the cylinder
                    // Steam Indicator Diagram reference - (d)
                    float HPVolumeRatioCuttoff = HPCylinderVolumePoint_b / HPCylinderVolumePoint_d;
                    HPCylinderReleasePressureAtmPSI = HPCylinderCutoffPressureAtmPSI * HPVolumeRatioCuttoff;  // Check factor to calculate volume of cylinder for new volume at exhaust

                    // Mean pressure & work between b) - d) - Cutoff Expansion - is after the cutoff and the first steam expansion, 
                    float HPExpansionRatioCutoff = 1.0f / HPVolumeRatioCuttoff; // Invert volume ratio to find Expansion ratio
                    float HPMeanPressureReleaseAtmPSI = HPCylinderCutoffPressureAtmPSI * ((float)Math.Log(HPExpansionRatioCutoff) / (HPExpansionRatioCutoff - 1.0f));
                    // Calculate work between b) - d)
                    float HPCylinderLengthCutoffExpansionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_d - HPCylinderVolumePoint_b);
                    float HPCylinderCutoffExpansionWorkInLbs = HPMeanPressureReleaseAtmPSI * HPCylinderLengthCutoffExpansionIn;

                    // Release pressure (with no receiver) is the pressure after the first steam expansion, and occurs as steam moves into the passageways between the HP and LP cylinder
                    // Steam Indicator Diagram reference - (e)
                    float HPVolumeRatioReceiver = (HPCylinderVolumePoint_d / HPCylinderVolumePoint_e);
                    // HPCylinderReleasePressureRecvAtmPSI = HPCylinderReleasePressureAtmPSI * HPExpansionRatioReceiver;
                    HPCylinderReleasePressureRecvAtmPSI = HPCylinderReleasePressureAtmPSI - 5.0f; // assume this relationship

                    // Exhaust pressure is the pressure after the second steam expansion, and occurs as all the steam is exhausted from the HP cylinder
                    // Steam Indicator Diagram reference - (f)
                    float HPVolumeRatioReleaseReceiver = (HPCylinderVolumePoint_e / HPCylinderVolumePoint_f);
                    HPCylinderExhaustPressureAtmPSI = HPCylinderReleasePressureRecvAtmPSI * HPVolumeRatioReleaseReceiver;

                    // Mean pressure & work between e) - f) - Release Expansion
                    float HPExpansionRatioReleaseReceiver = 1.0f / HPVolumeRatioReleaseReceiver; // Invert volume ratio to find Expansion ratio
                    float HPMeanPressureExhaustAtmPSI = HPCylinderReleasePressureRecvAtmPSI * ((float)Math.Log(HPExpansionRatioReleaseReceiver) / (HPExpansionRatioReleaseReceiver - 1.0f));
                    // Calculate work between e) - f) - Release Expansion
                    float HPCylinderLengthReleaseExpansionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_fHPonly - HPCylinderVolumePoint_d);
                    float HPCylinderReleaseExpansionWorkInLbs = HPMeanPressureExhaustAtmPSI * HPCylinderLengthReleaseExpansionIn;

                    // LP cylinder initial pressure will be mixture of the volume at exahust for HP cylinder and the volume of the LP clearance at the LP cylinder pre-admission pressure
                    // To calculate we need to calculate the LP Pre-Admission pressure @ q first
                    // LP Cylinder pre-admission pressure is the pressure after the second steam expansion, and occurs as the steam valves close in the LP Cylinder
                    // LP Cylinder compression pressure will be equal to back pressure - assume flat line.
                    // Steam Indicator Diagram reference - (n) 
                    LPCylinderPreCompressionPressureAtmPSI = LPCylinderBackPressureAtmPSI;

                    // Steam Indicator Diagram reference - (q)
                    float LPVolumeRatioCompression = LPCylinderVolumePoint_n / LPCylinderVolumePoint_a;
                    LPCylinderPreAdmissionPressureAtmPSI = LPCylinderPreCompressionPressureAtmPSI * LPVolumeRatioCompression;

                    // Steam Indicator Diagram reference - (g) 
                    LPCylinderInitialPressureAtmPSI = ((LPCylinderPreAdmissionPressureAtmPSI * (LPCylinderClearancePC * CompoundCylinderRatio)) + (HPCylinderExhaustPressureAtmPSI * (HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP))) / ((LPCylinderClearancePC * CompoundCylinderRatio) + (HPCylinderVolumeFactor + HPCylinderClearancePC + CompoundRecieverVolumePCHP));

                    // LP cylinder cutoff pressure - before LP Cylinder Cutoff - in this instance both the HP and LP are still interconnected
                    // Steam Indicator Diagram reference - (h) - pre cutoff in HP & LP Cylinder
                    float LPVolumeRatioPreCutoff = LPCylinderVolumePoint_g / LPCylinderVolumePoint_h_pre;
                    LPCylinderPreCutoffPressureAtmPSI = LPCylinderInitialPressureAtmPSI * LPVolumeRatioPreCutoff;

                    // Steam Indicator Diagram reference - (h) - In HP Cylinder post cutoff in LP Cylinder
                    // Pressure will have equalised in HP & LP Cylinder, so will be the same as the pressure pre-cutoff
                    HPCylinderPreCompressionPressureAtmPSI = LPCylinderPreCutoffPressureAtmPSI;

                    // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                    float LPCutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                    float LPCutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                    // calculate value based upon setting of Cylinder port opening

                    float LPCutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (LPCutoffDropUpper - LPCutoffDropLower)) + LPCutoffDropLower;


                    LPCylinderPreCutoffPressureAtmPSI *= LPCutoffPressureDropRatio;  // allow for wire drawing into LP cylinder

                    // Mean pressure & work between g) - h) - This curve is the admission curve for the LP and the Backpressure curve for the HP - it is an expansion curve
                    float LPExpansionRatioPreCutoff = LPCylinderVolumePoint_h_pre / LPCylinderVolumePoint_g;  // Invert volume ratio to find Expansion ratio
                    float LPMeanPressureCutoffAtmPSI = LPCylinderInitialPressureAtmPSI * ((float)Math.Log(LPExpansionRatioPreCutoff) / (LPExpansionRatioPreCutoff - 1.0f));

                    // Find negative pressures for HP Cylinder - upper half of g) - h) curve
                    HPCylinderBackPressureAtmPSI = LPMeanPressureCutoffAtmPSI; // HP Back pressure is the same as the mean admission pressure for the LP cylinder
                    // Calculate work between g) - h) - HP Cylinder only
                    float HPCylinderLengthExpansionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_b - HPCylinderVolumePoint_a); // Calculate negative work in HP cylinder only due to back pressure, ie backpressure in HP Only x volume of cylinder between commencement of LP stroke and cutoff
                    float HPCylinderBackPressureExpansionWorksInLbs = HPCylinderBackPressureAtmPSI * HPCylinderLengthExpansionIn;


                    // HP cylinder compression pressure
                    // Steam Indicator Diagram reference - (k) - before the valve closes
                    float HPExpansionRatioPreCompOpen = HPCylinderVolumePoint_h_HPpost / HPCylinderVolumePoint_k_pre;
                    HPCylinderPreAdmissionOpenPressureAtmPSI = HPCylinderPreCompressionPressureAtmPSI * HPExpansionRatioPreCompOpen;

                    // Find negative pressures in HP Cylinder
                    // Mean pressure & work between h) - k) - Compression Expansion
                    // Ratio of compression = stroke during compression = stroke @ start of compression / stroke and end of compression
                    float HPCylinderCompressionRatioPreCompression = HPCylinderVolumePoint_h_HPpost / HPCylinderVolumePoint_k_pre;
                    float HPMeanPressurePreCompAtmPSI = HPCylinderPreCompressionPressureAtmPSI * HPCylinderCompressionRatioPreCompression * ((float)Math.Log(HPCylinderCompressionRatioPreCompression) / (HPCylinderCompressionRatioPreCompression - 1.0f));
                    // Calculate work between h) - k) - HP Cylinder only
                    float HPCylinderLengthPreCompExpansionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_d - HPCylinderVolumePoint_b); // This volume is equivalent to the volume from LP cutoff to release
                    float HPCylinderPreCompExpansionWorkInLbs = HPMeanPressurePreCompAtmPSI * HPCylinderLengthPreCompExpansionIn;

                    // Find negative pressures in HP Cylinder
                    // Mean pressure & work between k) - a) - Admission Expansion
                    float HPMeanPressurePreAdmAtmPSI = (HPCylinderInitialPressureAtmPSI + HPCylinderPreAdmissionOpenPressureAtmPSI) / 2.0f;
                    // Calculate work between k) - a) - HP Cylinder only
                    float HPCylinderLengthPreAdmExpansionIn = Me.ToIn(CylinderStrokeM) * (HPCylinderVolumePoint_k_post - HPCylinderVolumePoint_a);
                    float HPCylinderPreAdmExpansionWorkInLbs = HPMeanPressurePreAdmAtmPSI * HPCylinderLengthPreAdmExpansionIn;

                    // Calculate total Work in HP Cylinder
                    float TotalHPCylinderWorksInLbs = HPCylinderAdmissionWorkInLbs + HPCylinderCutoffExpansionWorkInLbs + HPCylinderReleaseExpansionWorkInLbs - HPCylinderBackPressureExpansionWorksInLbs - HPCylinderPreCompExpansionWorkInLbs - HPCylinderPreAdmExpansionWorkInLbs;

                    // ***** Calculate pressures and work done in LP cylinder *****

                    // Calculate work between g) - h) - Release Expansion  (LP Cylinder)              
                    float LPCylinderLengthCutoffExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_h_LPpost - LPCylinderVolumePoint_a);
                    // For purposes of work done only consider the change in LP cylinder between stroke commencement and cutoff
                    float LPMeanPressureCutoffWireAtmPSI = (LPCylinderInitialPressureAtmPSI + LPCylinderPreCutoffPressureAtmPSI) / 2.0f;  // Find the wire drawn pressure as an average, rather then as an expansion pressure
                    float LPCylinderCutoffExpansionWorkInLbs = LPMeanPressureCutoffWireAtmPSI * LPCylinderLengthCutoffExpansionIn;

                    // LP cylinder release pressure 
                    // Steam Indicator Diagram reference - (l) 
                    float LPVolumeRatioRelease = LPCylinderVolumePoint_h_LPpost / LPCylinderVolumePoint_l;
                    LPCylinderReleasePressureAtmPSI = LPCylinderPreCutoffPressureAtmPSI * LPVolumeRatioRelease;

                    // LP Cylinder back pressure will be decreased depending upon locomotive speed
                    // Steam Indicator Diagram reference - (m) 
                    LPCylinderBackPressureAtmPSI = BackPressureIHPtoAtmPSI[IndicatedHorsePowerHP];

                    // Calculate MEP for LP Cylinder
                    //Mean pressure & work between h) - l)
                    float LPExpansionRatioRelease = 1.0f / LPVolumeRatioRelease;
                    float LPMeanPressureReleaseAtmPSI = LPCylinderPreCutoffPressureAtmPSI * ((float)Math.Log(LPExpansionRatioRelease) / (LPExpansionRatioRelease - 1.0f));
                    // Calculate work between h) - l) - LP Cylinder only
                    float LPCylinderReleaseExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_l - LPCylinderVolumePoint_h_LPpost);
                    float LPCylinderReleaseWorkInLbs = LPMeanPressureReleaseAtmPSI * LPCylinderReleaseExpansionIn;

                    // Mean pressure & work between l) - m)
                    float LPMeanPressureExhaustAtmPSI = (LPCylinderReleasePressureAtmPSI + LPCylinderBackPressureAtmPSI) / 2.0f;
                    // Calculate work between l) - m) - LP Cylinder only
                    float LPCylinderExhaustExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_l);
                    float LPCylinderExhaustWorkInLbs = LPMeanPressureExhaustAtmPSI * LPCylinderExhaustExpansionIn;

                    // Calculate work between m) - n) - LP Cylinder only
                    float LPCylinderBackPressExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_m - LPCylinderVolumePoint_n);
                    float LPCylinderBackPressureWorkInLbs = LPCylinderBackPressureAtmPSI * LPCylinderBackPressExpansionIn;

                    // Mean pressure & work between n) - q)
                    float LPCylinderVolumeRatioPreCompression = (LPCylinderVolumePoint_n) / LPCylinderVolumePoint_a;
                    float LPMeanPressurePreCompAtmPSI = LPCylinderBackPressureAtmPSI * LPCylinderVolumeRatioPreCompression * ((float)Math.Log(LPCylinderVolumeRatioPreCompression) / (LPCylinderVolumeRatioPreCompression - 1.0f));
                    // Calculate work between n) - q) - LP Cylinder only
                    float LPCylinderLengthPreCompExpansionIn = Me.ToIn(CylinderStrokeM) * (LPCylinderVolumePoint_n - LPCylinderVolumePoint_a);
                    float LPCylinderPreCompExpansionWorkInLbs = LPMeanPressurePreCompAtmPSI * LPCylinderLengthPreCompExpansionIn;

                    // Mean pressure & work between q) - g)            
                    float LPMeanPressurePreAdmAtmPSI = (LPCylinderInitialPressureAtmPSI + LPCylinderPreAdmissionPressureAtmPSI) / 2.0f;
                    // Calculate work between q) - g) - LP Cylinder only
                    float LPCylinderLengthPreAdmExpansionIn = Me.ToIn(CylinderStrokeM) * LPCylinderVolumePoint_a;
                    float LPCylinderPreAdmExpansionWorkInLbs = LPMeanPressurePreAdmAtmPSI * LPCylinderLengthPreAdmExpansionIn;


                    // Calculate total Work in LP Cylinder
                    float TotalLPCylinderWorksInLbs = LPCylinderCutoffExpansionWorkInLbs + LPCylinderReleaseWorkInLbs + LPCylinderExhaustWorkInLbs - LPCylinderBackPressureWorkInLbs - LPCylinderPreCompExpansionWorkInLbs - LPCylinderPreAdmExpansionWorkInLbs;

                    HPCylinderMEPAtmPSI = TotalHPCylinderWorksInLbs / (HPCylinderVolumeFactor * Me.ToIn(CylinderStrokeM));
                    LPCylinderMEPAtmPSI = TotalLPCylinderWorksInLbs / ((LPCylinderVolumeFactor * CompoundCylinderRatio) * Me.ToIn(CylinderStrokeM));


                    HPCylinderMEPAtmPSI = MathHelper.Clamp(HPCylinderMEPAtmPSI, 0.00f, HPCylinderMEPAtmPSI); // Clamp MEP so that HP MEP does not go negative
                    LPCylinderMEPAtmPSI = MathHelper.Clamp(LPCylinderMEPAtmPSI, 0.00f, LPCylinderMEPAtmPSI); // Clamp MEP so that LP MEP does not go negative

                    // Debug information

#if DEBUG_LOCO_STEAM
                    if (DebugWheelRevs >= 80.0 && DebugWheelRevs < 80.05 | DebugWheelRevs >= 160.0 && DebugWheelRevs < 160.05 | DebugWheelRevs >= 240.0 && DebugWheelRevs < 240.05 | DebugWheelRevs >= 320.0 && DebugWheelRevs < 320.05)
                    {
                        Trace.TraceInformation("***************************************** Compound Steam Locomotive ***************************************************************");

                        Trace.TraceInformation("*********** Operating Conditions *********");

                        Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2} RelPt {3} RecVol {4} CylRatio {5} HPClear {6} LPClear {7}", throttle, cutoff, pS.TopM(DrvWheelRevRpS), CylinderExhaustOpenFactor, CompoundRecieverVolumePCHP, CompoundCylinderRatio, HPCylinderClearancePC, LPCylinderClearancePC);

                        Trace.TraceInformation("*********** HP Cylinder *********");

                        Trace.TraceInformation("HP Cylinder Press: a {0} b {1} d {2} e {3} f {4} g {5} h {6} k {7} Back {8}", HPCylinderInitialPressureAtmPSI, HPCylinderCutoffPressureAtmPSI, HPCylinderReleasePressureAtmPSI, HPCylinderReleasePressureRecvAtmPSI, HPCylinderExhaustPressureAtmPSI, LPCylinderInitialPressureAtmPSI, HPCylinderPreCompressionPressureAtmPSI, HPCylinderPreAdmissionOpenPressureAtmPSI, HPCylinderBackPressureAtmPSI);

                        Trace.TraceInformation("Work Input: a - b: MeanPressCutoff {0} Cyl Len {1} b {2} a {3}", HPMeanPressureCuttoffAtmPSI, HPCylinderLengthAdmissionIn, HPCylinderVolumePoint_b, HPCylinderVolumePoint_a);

                        Trace.TraceInformation("Work Input: b - d: MeanPressRelease {0} Cyl Len {1} d {2} b {3} ", HPMeanPressureReleaseAtmPSI, HPCylinderLengthCutoffExpansionIn, HPCylinderVolumePoint_d, HPCylinderVolumePoint_b);

                        Trace.TraceInformation("Work Input: e - f: MeanPressExhaust {0} Cyl Len {1} f_HPonly {2} d {3} ", HPMeanPressureExhaustAtmPSI, HPCylinderLengthReleaseExpansionIn, HPCylinderVolumePoint_fHPonly, HPCylinderVolumePoint_d);

                        Trace.TraceInformation("MeanPressure: g-h: HPMeanPressPreComp {0} ExpRatio {1} h_pre {2} g {3} Input Press {4}", LPMeanPressureCutoffAtmPSI, LPExpansionRatioPreCutoff, LPCylinderVolumePoint_h_pre, LPCylinderVolumePoint_g, LPCylinderInitialPressureAtmPSI);

                        Trace.TraceInformation("Work Input: g - h: HPMeanBack {0} Cyl Len {1} b {2} a {3} ", HPCylinderBackPressureAtmPSI, HPCylinderLengthExpansionIn, HPCylinderVolumePoint_b, HPCylinderVolumePoint_a);

                        Trace.TraceInformation("MeanPressure: h-k: HPMeanPressPreComp {0} CompRatio {1} h_HPpost {2} k_pre {3} Input Press {4}", HPMeanPressurePreCompAtmPSI, HPCylinderCompressionRatioPreCompression, HPCylinderVolumePoint_h_HPpost, HPCylinderVolumePoint_k_pre, HPCylinderPreCompressionPressureAtmPSI);

                        Trace.TraceInformation("Work Input: h - k: HPMeanPressPreComp {0} Cyl Len {1} d {2} b {3} ", HPMeanPressurePreCompAtmPSI, HPCylinderLengthPreCompExpansionIn, HPCylinderVolumePoint_d, HPCylinderVolumePoint_b);

                        Trace.TraceInformation("Work Input: k - a: HPMeanPressPreAdmin {0} Cyl Len {1} k_post {2} a {3} ", HPMeanPressurePreAdmAtmPSI, HPCylinderLengthPreAdmExpansionIn, HPCylinderVolumePoint_k_post, HPCylinderVolumePoint_a);

                        Trace.TraceInformation("HP Works: Total {0} === a-b {1} b-d {2} e-f {3} g-h {4} h-k {5} k-a {6}", TotalHPCylinderWorksInLbs, HPCylinderAdmissionWorkInLbs, HPCylinderCutoffExpansionWorkInLbs, HPCylinderReleaseExpansionWorkInLbs, HPCylinderBackPressureExpansionWorksInLbs, HPCylinderPreCompExpansionWorkInLbs, HPCylinderPreAdmExpansionWorkInLbs);

                        Trace.TraceInformation("*********** LP Cylinder *********");

                        Trace.TraceInformation("LP Cylinder Press: g {0} h(pre) {1}  l {2} m {3} n {4} q {5}", LPCylinderInitialPressureAtmPSI, LPCylinderPreCutoffPressureAtmPSI, LPCylinderReleasePressureAtmPSI, LPCylinderBackPressureAtmPSI, LPCylinderPreCompressionPressureAtmPSI, LPCylinderPreAdmissionPressureAtmPSI);

                        Trace.TraceInformation("Press: h: PreCutoff {0} h_pre {1} g {2} InitPress {3}", LPCylinderPreCutoffPressureAtmPSI, LPCylinderVolumePoint_h_pre, LPCylinderVolumePoint_g, LPCylinderInitialPressureAtmPSI);

                        Trace.TraceInformation("Work Input: g - h: LPMeanBack {0} Cyl Len {1} h_postLP {2} a {3} ", LPMeanPressureCutoffAtmPSI, LPCylinderLengthCutoffExpansionIn, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("MeanPressure h-l: LPMeanPressRelease {0} ExpRatio {1} h_LPpost {2} l {3} PreCutoffPress {4}", LPMeanPressureReleaseAtmPSI, LPExpansionRatioRelease, LPCylinderVolumePoint_h_LPpost, LPCylinderVolumePoint_l, LPCylinderPreCutoffPressureAtmPSI);

                        Trace.TraceInformation("Work Input: h - l: LPMeanPressureRelease {0} Cyl Len {1} l {2} h_postLP {3} ", LPMeanPressureReleaseAtmPSI, LPCylinderReleaseExpansionIn, LPCylinderVolumePoint_l, LPCylinderVolumePoint_h_LPpost);

                        Trace.TraceInformation("Work Input: l - m: LPMeanPressureExhaust {0} Cyl Len {1} m {2} l {3} ", LPMeanPressureExhaustAtmPSI, LPCylinderExhaustExpansionIn, LPCylinderVolumePoint_m, LPCylinderVolumePoint_l);

                        Trace.TraceInformation("Work Input: m - n: LPMeanPressureBack {0} Cyl Len {1} m {2} n {3} ", LPCylinderBackPressureAtmPSI, LPCylinderBackPressExpansionIn, LPCylinderVolumePoint_m, LPCylinderVolumePoint_n);

                        Trace.TraceInformation("MeanPressure n-q: LPMeanPressPreComp {0} CompRatio {1} n {2} b {3} PreCompPress {4}", LPMeanPressurePreCompAtmPSI, LPCylinderVolumeRatioPreCompression, ((LPCylinderVolumeFactor - CylinderExhaustOpenFactor) + LPCylinderClearancePC), LPCylinderClearancePC, LPCylinderBackPressureAtmPSI);

                        Trace.TraceInformation("Work Input: n - q: LPMeanPressurePreComp {0} Cyl Len {1} n {2} a {3} ", LPMeanPressurePreCompAtmPSI, LPCylinderLengthPreCompExpansionIn, LPCylinderVolumePoint_n, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("Work Input: q - g: LPMeanPressurePreAdm {0} Cyl Len {1} a {2}", LPMeanPressurePreAdmAtmPSI, LPCylinderLengthPreAdmExpansionIn, LPCylinderVolumePoint_a);

                        Trace.TraceInformation("LP Works: Total {0} === g-h {1} h-l {2} l-m {3} m-n {4} n-q {5} q-g {6}", TotalLPCylinderWorksInLbs, LPCylinderCutoffExpansionWorkInLbs, LPCylinderReleaseWorkInLbs, LPCylinderExhaustWorkInLbs, LPCylinderBackPressureWorkInLbs, LPCylinderPreCompExpansionWorkInLbs, LPCylinderPreAdmExpansionWorkInLbs);

                        Trace.TraceInformation("*********** MEP *********");

                        Trace.TraceInformation("MEP: HP {0}  LP {1}", HPCylinderMEPAtmPSI, LPCylinderMEPAtmPSI);
                    }
#endif
                }

                if (throttle < 0.02f)
                {
                    HPCylinderInitialPressureAtmPSI = 0.0f;  // for sake of display zero pressure values if throttle is closed.
                    HPCylinderBackPressureAtmPSI = 0.0f;
                    HPCylinderReleasePressureAtmPSI = 0.0f;
                    HPCylinderReleasePressureRecvAtmPSI = 0.0f;
                    HPCylinderExhaustPressureAtmPSI = 0.0f;
                    HPCylinderCutoffPressureAtmPSI = 0.0f;
                    HPCylinderPreCompressionPressureAtmPSI = 0.0f;

                    LPCylinderInitialPressureAtmPSI = 0.0f;
                    LPCylinderPreCutoffPressureAtmPSI = 0.0f;
                    LPCylinderReleasePressureAtmPSI = 0.0f;
                    LPCylinderBackPressureAtmPSI = 0.0f;

                    HPCylinderMEPAtmPSI = 0.0f;
                    LPCylinderMEPAtmPSI = 0.0f;


                }
            }

            #endregion


            #region Calculation of Mean Effective Pressure of Cylinder using an Indicator Diagram type approach - Single Expansion

            if (SteamEngineType != SteamEngineTypes.Compound)
            {

                // Calculate apparent volumes at various points in cylinder
                float CylinderVolumePoint_e = CylinderCompressionCloseFactor + CylinderClearancePC;
                float CylinderVolumePoint_f = CylinderPreAdmissionOpenFactor + CylinderClearancePC;

                // Note all presurres in absolute for working on steam indicator diagram
                // The pressures below are as calculated and referenced to the steam indicator diagram for single expansion locomotives by letters shown in brackets - see Coals to Newcastle website
                // Calculate Ratio of expansion, with cylinder clearance
                // R (ratio of Expansion) = (length of stroke to point of  exhaust + clearance) / (length of stroke to point of cut-off + clearance)
                // Expressed as a fraction of stroke R = (Exhaust point + c) / (cutoff + c)
                RatioOfExpansion = (CylinderExhaustOpenFactor + CylinderClearancePC) / (cutoff + CylinderClearancePC);
                // Absolute Mean Pressure = Ratio of Expansion
                SteamChestPressurePSI = (throttle * SteamChestPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)] * BoilerPressurePSI); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

                // Initial pressure will be decreased depending upon locomotive speed
                // This drop can be adjusted with a table in Eng File
                // Steam Indicator Diagram reference - (a) 
                InitialPressureAtmPSI = ((throttle * BoilerPressurePSI) + OneAtmospherePSI) * InitialPressureDropRatioRpMtoX[pS.TopM(DrvWheelRevRpS)]; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

                // Steam Indicator Diagram reference - (d) 
                BackPressureAtmPSI = BackPressureIHPtoAtmPSI[IndicatedHorsePowerHP];

                if (throttle < 0.02f)
                {
                    InitialPressureAtmPSI = 0.0f;  // for sake of display zero pressure values if throttle is closed.
                    BackPressureAtmPSI = 0.0f;
                }


                // Calculate Cut-off Pressure drop - cutoff pressure drops as speed of locomotive increases.
                float CutoffDropUpper = CutoffInitialPressureDropRatioUpper.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - upper limit
                float CutoffDropLower = CutoffInitialPressureDropRatioLower.Get(pS.TopM(DrvWheelRevRpS), cutoff);  // Get Cutoff Pressure to Initial pressure drop - lower limit

                // calculate value based upon setting of Cylinder port opening

                CutoffPressureDropRatio = (((CylinderPortOpeningFactor - CylinderPortOpeningLower) / (CylinderPortOpeningUpper - CylinderPortOpeningLower)) * (CutoffDropUpper - CutoffDropLower)) + CutoffDropLower;

                // Steam Indicator Diagram reference - (b) 
                CutoffPressureAtmPSI = InitialPressureAtmPSI * CutoffPressureDropRatio;

                // In driving the wheels steam does work in the cylinders. The amount of work can be calculated by a typical steam indicator diagram
                // Mean Effective Pressure (work) = average positive pressures - average negative pressures
                // Average Positive pressures = admission + expansion + release
                // Average Negative pressures = exhaust + compression + pre-admission

                // Calculate Av Admission Work (inch pounds) between a) - b)
                // Av Admission work = Av (Initial Pressure + Cutoff Pressure) * length of Cylinder to cutoff
                float CylinderLengthAdmissionIn = Me.ToIn(CylinderStrokeM * ((cutoff + CylinderClearancePC) - CylinderClearancePC));
                CylinderAdmissionWorkInLbs = ((InitialPressureAtmPSI + CutoffPressureAtmPSI) / 2.0f) * CylinderLengthAdmissionIn;

                // Steam Indicator Diagram reference - (c) 
                // Release pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                CylinderReleasePressureAtmPSI = (CutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (CylinderExhaustOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust

                // Calculate Av Expansion Work (inch pounds) - between b) - c)
                // Av pressure during expansion = Cutoff pressure x log (ratio of expansion) / (ratio of expansion - 1.0) 
                // Av Expansion work = Av pressure during expansion * length of Cylinder during expansion
                float CylinderLengthExpansionIn = Me.ToIn(CylinderStrokeM) * ((CylinderExhaustOpenFactor + CylinderClearancePC) - (cutoff + CylinderClearancePC));
                float AverageExpansionPressureAtmPSI = CutoffPressureAtmPSI * ((float)Math.Log(RatioOfExpansion) / (RatioOfExpansion - 1.0f));
                CylinderExpansionWorkInLbs = AverageExpansionPressureAtmPSI * CylinderLengthExpansionIn;

                // Calculate Av Release work (inch pounds) - between c) - d)
                // Av Release work = Av pressure during release * length of Cylinder during release
                float CylinderLengthReleaseIn = Me.ToIn(CylinderStrokeM) * ((1.0f + CylinderClearancePC) - (CylinderExhaustOpenFactor + CylinderClearancePC)); // Full cylinder length is 1.0
                CylinderReleaseWorkInLbs = ((CylinderReleasePressureAtmPSI + BackPressureAtmPSI) / 2.0f) * CylinderLengthReleaseIn;

                // Calculate Av Exhaust Work (inch pounds) - between d) - e)
                // Av Exhaust work = Av pressure during exhaust * length of Cylinder during exhaust stroke
                CylinderExhaustWorkInLbs = BackPressureAtmPSI * Me.ToIn(CylinderStrokeM) * ((1.0f - CylinderCompressionCloseFactor) + CylinderClearancePC);

                // Steam Indicator Diagram reference - (e) 
                // Calculate pre-compression pressure based upon backpresure being equal to it, as steam should be exhausting
                CylinderPreCompressionPressureAtmPSI = (BackPressureAtmPSI);

                // Calculate Av Compression Work (inch pounds) - between e) - f)
                // Ratio of compression = stroke during compression = stroke @ start of compression / stroke and end of compression
                // Av compression pressure = PreCompression Pressure x Ratio of Compression x log (Ratio of Compression) / (Ratio of Compression - 1.0)
                // Av Exhaust work = Av pressure during compression * length of Cylinder during compression stroke
                float RatioOfCompression = (CylinderVolumePoint_e) / (CylinderVolumePoint_f);
                float CylinderLengthCompressionIn = Me.ToIn(CylinderStrokeM) * (CylinderVolumePoint_e - CylinderVolumePoint_f);
                float AverageCompressionPressureAtmPSI = CylinderPreCompressionPressureAtmPSI * RatioOfCompression * ((float)Math.Log(RatioOfCompression) / (RatioOfCompression - 1.0f));
                CylinderCompressionWorkInLbs = AverageCompressionPressureAtmPSI * CylinderLengthCompressionIn;

                // Steam Indicator Diagram reference - (f) 
                CylinderPreAdmissionPressureAtmPSI = CylinderPreCompressionPressureAtmPSI * (CylinderCompressionCloseFactor + CylinderClearancePC) / (CylinderPreAdmissionOpenFactor + CylinderClearancePC);  // Check factor to calculate volume of 

                // Calculate Av Pre-admission work (inch pounds) - between f) - a)
                // Av Pre-admission work = Av pressure during pre-admission * length of Cylinder during pre-admission stroke
                CylinderPreAdmissionWorkInLbs = ((InitialPressureAtmPSI + CylinderPreAdmissionPressureAtmPSI) / 2.0f) * CylinderPreAdmissionOpenFactor * Me.ToIn(CylinderStrokeM);

                // Calculate total work in cylinder
                float TotalPositiveWorkInLbs = CylinderAdmissionWorkInLbs + CylinderExpansionWorkInLbs + CylinderReleaseWorkInLbs - CylinderExhaustWorkInLbs - CylinderCompressionWorkInLbs - CylinderPreAdmissionWorkInLbs;

                MeanEffectivePressurePSI = TotalPositiveWorkInLbs / Me.ToIn(CylinderStrokeM);
                MeanEffectivePressurePSI = MathHelper.Clamp(MeanEffectivePressurePSI, 0, MaxBoilerPressurePSI + OneAtmospherePSI); // Make sure that Cylinder pressure does not go negative

#if DEBUG_LOCO_STEAM
                if (DebugWheelRevs >= 80.0 && DebugWheelRevs < 80.05 | DebugWheelRevs >= 160.0 && DebugWheelRevs < 160.05 | DebugWheelRevs >= 240.0 && DebugWheelRevs < 240.05 | DebugWheelRevs >= 320.0 && DebugWheelRevs < 320.05)
                {
                    Trace.TraceInformation("***************************************** Simple Steam Locomotive ***************************************************************");

                    Trace.TraceInformation("*********** Operating Conditions *********");

                    Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2} RelPt {3} Clear {4}", throttle, cutoff, pS.TopM(DrvWheelRevRpS), CylinderExhaustOpenFactor, CylinderClearancePC);

                    Trace.TraceInformation("*********** Cylinder *********");

                    Trace.TraceInformation("Cylinder Press: a {0} b {1} c {2} d {3} e {4} f {5}", InitialPressureAtmPSI, CutoffPressureAtmPSI, CylinderReleasePressureAtmPSI, BackPressureAtmPSI, CylinderPreCompressionPressureAtmPSI, CylinderPreAdmissionPressureAtmPSI);


                    Trace.TraceInformation("MeanPressure e-f: AvCompressionPressure {0} CompRatio {1} Vol_e {2} Vol_f {3} PreCompPress {4}", AverageCompressionPressureAtmPSI, RatioOfCompression, CylinderVolumePoint_e, CylinderVolumePoint_f, CylinderPreCompressionPressureAtmPSI);

                    //      Trace.TraceInformation("e - f: MeanPressPreComp {0} {1} d {2} b {3} ", HPMeanPressurePreCompAtmPSI, HPCylinderLengthPreCompExpansionIn, HPCylinderVolumePoint_d, HPCylinderVolumePoint_b);

                    Trace.TraceInformation("HP Works: Total {0} === a-b {1} b-c {2} c-d {3} d-e {4} e-f {5} f-a {6}", TotalPositiveWorkInLbs, CylinderAdmissionWorkInLbs, CylinderExpansionWorkInLbs, CylinderReleaseWorkInLbs, CylinderExhaustWorkInLbs, CylinderCompressionWorkInLbs, CylinderPreAdmissionWorkInLbs);
                }
#endif
            }

            #endregion
            // Determine if Superheater in use
            if (HasSuperheater)
            {
                CurrentSuperheatTempF = SuperheatTempLbpHtoDegF[pS.TopH(CylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate current superheat temp
                CurrentSuperheatTempF = MathHelper.Clamp(CurrentSuperheatTempF, 0.0f, SuperheatRefTempF); // make sure that superheat temp does not exceed max superheat temp or drop below zero
                float DifferenceSuperheatTeampF = CurrentSuperheatTempF - SuperheatTempLimitXtoDegF[cutoff]; // reduce superheat temp due to cylinder condensation
                SuperheatVolumeRatio = 1.0f + (0.0015f * DifferenceSuperheatTeampF); // Based on formula Vsup = Vsat ( 1 + 0.0015 Tsup) - Tsup temperature at superheated level
                // look ahead to see what impact superheat will have on cylinder usage
                float FutureCylinderSteamUsageLBpS = CylinderSteamUsageLBpS * 1.0f / SuperheatVolumeRatio; // Calculate potential future new cylinder steam usage
                float FutureSuperheatTempF = SuperheatTempLbpHtoDegF[pS.TopH(FutureCylinderSteamUsageLBpS)] * SuperheatTempRatio; // Calculate potential future new superheat temp

                float SuperheatTempThresholdXtoDegF = SuperheatTempLimitXtoDegF[cutoff] - 25.0f; // 10 deg bandwith reduction to reset superheat flag

                if (CurrentSuperheatTempF > SuperheatTempLimitXtoDegF[cutoff])
                {
                    IsSuperSet = true;    // Set to use superheat factor if above superheat temp threshold      
                }
                else if (FutureSuperheatTempF < SuperheatTempThresholdXtoDegF)
                {
                    IsSuperSet = false;    // Reset if superheat temp drops 
                }


                if (IsSuperSet)
                {
                    SuperheaterSteamUsageFactor = 1.0f; // set steam condensation to unity as no condensation occurs
                }
                else
                {
                    CylinderCondensationFactor = CylinderCondensationFractionX[cutoff];
                    CylinderSpeedCondensationFactor = CylinderCondensationFactorSpeed[pS.TopM(DrvWheelRevRpS)] / CylinderCondensationFactorSpeed[0.01f]; // Calculate compensating factor for condensation due to speed of locomotive
                    float CondensationFactorTemp = 1.0f + (CylinderCondensationFactor * CylinderSpeedCondensationFactor);  // Calculate correcting factor for steam use due to compensation
                    float TempCondensationFactor = CondensationFactorTemp - 1.0f;
                    float SuperHeatMultiplier = (1.0f - (CurrentSuperheatTempF / SuperheatTempLimitXtoDegF[cutoff])) * TempCondensationFactor;
                    SuperHeatMultiplier = MathHelper.Clamp(SuperHeatMultiplier, 0.0f, SuperHeatMultiplier);
                    float SuperHeatFactorFinal = 1.0f + SuperHeatMultiplier;
                    SuperheaterSteamUsageFactor = SuperHeatFactorFinal;
                }
            }
            else
            {
                CylinderCondensationFactor = CylinderCondensationFractionX[cutoff];
                CylinderSpeedCondensationFactor = CylinderCondensationFactorSpeed[pS.TopM(DrvWheelRevRpS)] / CylinderCondensationFactorSpeed[0.01f]; // Calculate compensating factor for condensation due to speed of locomotive
                float CondensationFactorTemp = 1.0f + (CylinderCondensationFactor * CylinderSpeedCondensationFactor);  // Calculate correcting factor for steam use due to compensation
                SuperheaterSteamUsageFactor = CondensationFactorTemp;
            }

            SuperheaterSteamUsageFactor = MathHelper.Clamp(SuperheaterSteamUsageFactor, 0.9f, SuperheaterSteamUsageFactor); // ensure factor does not go below 0.9, as this represents base steam consumption by the cylinders.

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
                CylinderPressureAtmPSI = CutoffPressureAtmPSI - (CutoffPressureAtmPSI * (1.0f - CylCockPressReduceFactor)); // Allow for pressure reduction due to Cylinder cocks being open.
            }
            else
            {
                CylinderPressureAtmPSI = CutoffPressureAtmPSI;
            }

            CylinderPressureAtmPSI = MathHelper.Clamp(CylinderPressureAtmPSI, 0, MaxBoilerPressurePSI + OneAtmospherePSI); // Make sure that Cylinder pressure does not go negative

            #region Calculation of Cylinder steam usage using an Indicator Diagram type approach
            // To calculate steam usage, Calculate amount of steam in cylinder 
            // Cylinder steam usage = steam volume (and weight) at start of release stage - steam remaining in cylinder after compression
            // This amount then should be corrected to allow for cylinder condensation in saturated locomotives or not in superheated locomotives



            if (SteamEngineType == SteamEngineTypes.Compound)
            {

                if (!CylinderCompoundOn) // compound mode
                // The steam in the HP @ Cutoff will give an indication of steam usage.
                {
                    float HPCylinderCutoffPressureGaugePSI = HPCylinderCutoffPressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                    float HPCylinderPreAdmissionPressureGaugePSI = HPCylinderPreAdmissionOpenPressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  
                    CylinderCutoffSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (cutoff + HPCylinderClearancePC); // Calculate volume of cylinder at start of release
                    CylinderCutoffSteamWeightLbs = CylinderCutoffSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[HPCylinderCutoffPressureGaugePSI]; // Weight of steam in Cylinder at release
                    CylinderClearanceSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (HPCylinderVolumeFactor - CylinderExhaustOpenFactor) + HPCylinderClearancePC; // volume of the clearance area + area of steam at pre-admission
                    // CylinderClearanceSteamWeightLbs = CylinderClearanceSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[HPCylinderPreAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                    CylinderClearanceSteamWeightLbs = 0.0f; // Requires proper steam tables at atmospheric - To Do
                    // For time being assume that compound locomotive doesn't experience cylinder condensation.
                    RawCylinderSteamWeightLbs = CylinderCutoffSteamWeightLbs - CylinderClearanceSteamWeightLbs;
                    RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * (RawCylinderSteamWeightLbs);
                    CalculatedCylinderSteamUsageLBpS = RawCalculatedCylinderSteamUsageLBpS;

                }
                else  // Simple mode
                // Steam at cutoff in LP will will give an indication of steam usage.
                {
                    float LPCylinderCutoffPressureGaugePSI = LPCylinderReleasePressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                    float LPCylinderPreAdmissionPressureGaugePSI = LPCylinderPreAdmissionPressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  
                    CylinderCutoffSteamVolumeFt3 = LPCylinderSweptVolumeFT3pFT * (CylinderExhaustOpenFactor + LPCylinderClearancePC); // Calculate volume of cylinder at start of release
                    CylinderCutoffSteamWeightLbs = CylinderCutoffSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[LPCylinderCutoffPressureGaugePSI]; // Weight of steam in Cylinder at release
                    CylinderClearanceSteamVolumeFt3 = LPCylinderSweptVolumeFT3pFT * ((LPCylinderVolumeFactor - CylinderExhaustOpenFactor) + LPCylinderClearancePC); // volume of the clearance area + area of steam at pre-admission
                    if (LPCylinderPreAdmissionPressureGaugePSI > 0.0) // need to consider steam density for pressures less then 0 gauge pressure - To Do
                    {
                        CylinderClearanceSteamWeightLbs = CylinderClearanceSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[LPCylinderPreAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                    }
                    else
                    {
                        CylinderClearanceSteamWeightLbs = 0.0f;
                    }
                    RawCylinderSteamWeightLbs = CylinderCutoffSteamWeightLbs - CylinderClearanceSteamWeightLbs;
                    RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * (RawCylinderSteamWeightLbs);
                    CalculatedCylinderSteamUsageLBpS = RawCalculatedCylinderSteamUsageLBpS * SuperheaterSteamUsageFactor;
                }
            }
            else // Calculate steam usage for simple and geared locomotives.
            {
                float CylinderCutoffPressureGaugePSI = CutoffPressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure
                float CylinderPreAdmissionPressureGaugePSI = CylinderPreAdmissionPressureAtmPSI - OneAtmospherePSI; // Convert to gauge pressure as steam tables are in gauge pressure  
                CylinderCutoffSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (cutoff + CylinderClearancePC); // Calculate volume of cylinder at start of release
                CylinderCutoffSteamWeightLbs = CylinderCutoffSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[CylinderCutoffPressureGaugePSI]; // Weight of steam in Cylinder at release
                CylinderClearanceSteamVolumeFt3 = CylinderSweptVolumeFT3pFT * (CylinderPreAdmissionOpenFactor + CylinderClearancePC); // volume of the clearance area + area of steam at pre-admission
                if (CylinderPreAdmissionPressureGaugePSI > 0.0) // need to consider steam density for pressures less then 0 gauge pressure - To Do
                {
                    CylinderClearanceSteamWeightLbs = CylinderClearanceSteamVolumeFt3 * CylinderSteamDensityPSItoLBpFT3[CylinderPreAdmissionPressureGaugePSI]; // Weight of total steam remaining in the cylinder
                }
                else
                {
                    CylinderClearanceSteamWeightLbs = 0.0f;
                }
                RawCylinderSteamWeightLbs = CylinderCutoffSteamWeightLbs - CylinderClearanceSteamWeightLbs;
                RawCalculatedCylinderSteamUsageLBpS = NumCylinders * DrvWheelRevRpS * CylStrokesPerCycle * (RawCylinderSteamWeightLbs);
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
        }

        private void UpdateMotion(float elapsedClockSeconds, float cutoff, float absSpeedMpS)
        {

            // This section updates the force calculations and maintains them at the current values.

            // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
            MaxIndicatedHorsePowerHP = MaxSpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;

            // Caculate the current piston speed - purely for display purposes at the moment 
            // Piston Speed (Ft p Min) = (Stroke length x 2) x (Ft in Mile x Train Speed (mph) / ( Circum of Drv Wheel x 60))
            PistonSpeedFtpMin = Me.ToFt(pS.TopM(CylinderStrokeM * 2.0f * DrvWheelRevRpS)) * SteamGearRatio;

            if (SteamEngineType == SteamEngineTypes.Compound)
            {
                if (!CylinderCompoundOn)
                {
                    // Calculate tractive effort if set to simple operation - to be checked
                    float LPSimpleTractiveEffortLbsF = CylinderEfficiencyRate * (0.7854f * LPCylinderMEPAtmPSI * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToFt(LPCylinderStrokeM)) / ((Me.ToFt(DriverWheelRadiusM * 2.0f)) * (float)Math.PI);
                    // There are two LP cylinders, and both ends need to be considered so the above value needs to multipled by 4 to get the total TE for the combined LP cylinders
                    LPSimpleTractiveEffortLbsF = 4.0f * LPSimpleTractiveEffortLbsF;

                    TractiveEffortLbsF = LPSimpleTractiveEffortLbsF;
                }
                else
                {
                    // Calculate tractive effort if set for compounding - tractive effort in each cylinder will need to be calculated
                    // From PRR test report - TE(for each cylinder - both ends of cylinder treated separately) = 0.7845 * (MEP * Cyl Dia^2 (ins) * Stroke (ft)) / Drv Wheel Cicum (ft)
                    // HP Cylinder
                    float HPTractiveEffortLbsF = CylinderEfficiencyRate * (0.7854f * HPCylinderMEPAtmPSI * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToFt(CylinderStrokeM)) / ((Me.ToFt(DriverWheelRadiusM * 2.0f)) * (float)Math.PI);
                    // There are two HP cylinders, and both ends need to be considered so the above value needs to multipled by 4 to get the total TE for the combined HP cylinders
                    HPTractiveEffortLbsF = 4.0f * HPTractiveEffortLbsF;

                    // LP Cylinder
                    float LPTractiveEffortLbsF = CylinderEfficiencyRate * (0.7854f * LPCylinderMEPAtmPSI * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToFt(LPCylinderStrokeM)) / ((Me.ToFt(DriverWheelRadiusM * 2.0f)) * (float)Math.PI);
                    // There are two LP cylinders, and both ends need to be considered so the above value needs to multipled by 4 to get the total TE for the combined LP cylinders
                    LPTractiveEffortLbsF = 4.0f * LPTractiveEffortLbsF;

                    TractiveEffortLbsF = (HPTractiveEffortLbsF + LPTractiveEffortLbsF);

                    // Calculate IHP
                    // IHP = (MEP x CylStroke(ft) x cylArea(sq in)) / 33000) - this is per cylinder - multiply by 4 for HP and LP to allow for each individual cylinder
                    float HPIndicatedHorsePowerHP = 4.0f * ((HPCylinderMEPAtmPSI * Me.ToFt(CylinderStrokeM) * Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * pS.TopM(DrvWheelRevRpS) / 33000.0f));

                    float LPIndicatedHorsePowerHP = 4.0f * ((LPCylinderMEPAtmPSI * Me.ToFt(CylinderStrokeM) * Me2.ToIn2(Me2.FromFt2(LPCylinderPistonAreaFt2)) * pS.TopM(DrvWheelRevRpS) / 33000.0f));

                    IndicatedHorsePowerHP = HPIndicatedHorsePowerHP + LPIndicatedHorsePowerHP;

                    float WheelRevs = pS.TopM(DrvWheelRevRpS);

#if DEBUG_LOCO_STEAM
                    if (WheelRevs >= 80.0 && WheelRevs < 80.05 | WheelRevs >= 160.0 && WheelRevs < 160.05 | WheelRevs >= 240.0 && WheelRevs < 240.05 | WheelRevs >= 320.0 && WheelRevs < 320.05)
                    {
                        Trace.TraceInformation("*********** Tractive Effort *********");

                        Trace.TraceInformation("HP Cylinder: {0} LP Cylinder: {1} Total {2}", HPTractiveEffortLbsF, LPTractiveEffortLbsF, TractiveEffortLbsF);

                        Trace.TraceInformation("*********** Indicated HorsePower *********");

                        Trace.TraceInformation("IHP LP Cylinder - MEP {0} Stroke {1} Area {2} Revs {3}", LPCylinderMEPAtmPSI, Me.ToFt(CylinderStrokeM), Me2.ToIn2(Me2.FromFt2(LPCylinderPistonAreaFt2)), pS.TopM(DrvWheelRevRpS));

                        Trace.TraceInformation("HP Cylinder: {0} LP Cylinder: {1} Total {2}", HPIndicatedHorsePowerHP, LPIndicatedHorsePowerHP, IndicatedHorsePowerHP);


                        Trace.TraceInformation("************************************************************************************************************************************************");
                    }
#endif

                }
            }
            else // if simple or geared locomotive calculate tractive effort
            {
                TractiveEffortLbsF = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2.0f * Me.ToIn(DriverWheelRadiusM))) * MeanEffectivePressurePSI * CylinderEfficiencyRate * MotiveForceGearRatio;

                // Calculate IHP
                // IHP = (MEP x CylStroke(ft) x cylArea(sq in) x No Strokes (/min)) / 33000) - this is per cylinder
                IndicatedHorsePowerHP = NumCylinders * MotiveForceGearRatio * ((MeanEffectivePressurePSI * Me.ToFt(CylinderStrokeM) * Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * pS.TopM(DrvWheelRevRpS) * CylStrokesPerCycle / 33000.0f));
            }

            TractiveEffortLbsF = MathHelper.Clamp(TractiveEffortLbsF, 0, TractiveEffortLbsF);
            DisplayTractiveEffortLbsF = TractiveEffortLbsF;

            DrawBarPullLbsF = -1.0f * N.ToLbf(CouplerForceU);
            DrawbarHorsePowerHP = -1.0f * (N.ToLbf(CouplerForceU) * Me.ToFt(absSpeedMpS)) / 550.0f;  // TE in this instance is a maximum, and not at the wheel???


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

            // On starting allow maximum motive force to be used
            if (absSpeedMpS < 1.0f && cutoff > 0.70f && throttle > 0.98f)
            {
                MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * MaxForceN;
            }

            if (absSpeedMpS == 0 && cutoff < 0.05f) // If the reverser is set too low then not sufficient steam is admitted to the steam cylinders, and hence insufficient Motive Force will produced to move the train.
                MotiveForceN = 0; 

            // Based upon max IHP, limit motive force.
            if (PistonSpeedFtpMin > MaxPistonSpeedFtpM || IndicatedHorsePowerHP > MaxIndicatedHorsePowerHP)
            {

                if (IndicatedHorsePowerHP >= MaxIndicatedHorsePowerHP)
                {
                    IndicatedHorsePowerHP = MaxIndicatedHorsePowerHP; // Set IHP to maximum value
                }
                // Calculate the speed factor for the locomotive, based upon piston speed limit  

                /// TODO - Add compound locomotive  ++++++++++++
                if (HasSuperheater)
                {
                    SpeedFactor = SuperheatedSpeedFactorSpeedDropFtpMintoX[PistonSpeedFtpMin];
                    // Calculate "critical" power of locomotive @ pistion speed
                    CurrentCriticalSpeedTractiveEffortLbf = (MaxTractiveEffortLbf * CylinderEfficiencyRate) * SpeedFactor;
                }
                else if (SteamEngineType == SteamEngineTypes.Geared)
                {
                    SpeedFactor = SaturatedSpeedFactorSpeedDropFtpMintoX[PistonSpeedFtpMin];   // Assume the same as saturated locomotive for time being.
                    // Calculate "critical" power of locomotive @ pistion speed
                    CurrentCriticalSpeedTractiveEffortLbf = (MaxTractiveEffortLbf * CylinderEfficiencyRate) * SpeedFactor * MotiveForceGearRatio;
                }
                else
                {
                    SpeedFactor = SaturatedSpeedFactorSpeedDropFtpMintoX[PistonSpeedFtpMin];
                    // Calculate "critical" power of locomotive @ pistion speed
                    CurrentCriticalSpeedTractiveEffortLbf = (MaxTractiveEffortLbf * CylinderEfficiencyRate) * SpeedFactor;
                }
                // Limit motive force once piston speed exceeds limit - natural limitation is achieved by MEP, however a cross check against the speed factor
                // (determined by Alco) will also be considered. Speed factor will be used if smaller then MEP calculated tractive effort. 
                // This allows a tapered effect as the locomotive force reaches a limit.
                if ((TractiveEffortLbsF * CylinderEfficiencyRate) > CurrentCriticalSpeedTractiveEffortLbf)
                {
                    MotiveForceN = (Direction == Direction.Forward ? 1 : -1) * N.FromLbf(CurrentCriticalSpeedTractiveEffortLbf);
                }

                // Only display value changes once piston limit exceeded
                if (PistonSpeedFtpMin > MaxPistonSpeedFtpM && CurrentCriticalSpeedTractiveEffortLbf < MaxCriticalSpeedTractiveEffortLbf)
                {
                    DisplayCriticalSpeedTractiveEffortLbf = CurrentCriticalSpeedTractiveEffortLbf;
                    DisplaySpeedFactor = SpeedFactor;
                }
            }

            #region - Steam Adhesion Model Input for Steam Locomotives

            // Based upon information presented in "Locomotive Operation - A Technical and Practical Analysis" by G. R. Henderson
            // At its simplest slip occurs when the wheel tangential force exceeds the static frictional force
            // Static frictional force = weight on the locomotive driving wheels * frictional co-efficient
            // Tangential force = Effective force (Interia + Piston force) * Tangential factor (sin (crank angle) + (crank radius / connecting rod length) * sin (crank angle) * cos (crank angle))
            // Typically tangential force will be greater at starting then when the locomotive is at speed, as interia and reduce steam pressure will decrease the value. 
            // Thus we will only consider slip impacts at start of the locomotive

            if (Simulator.UseAdvancedAdhesion && this == Simulator.PlayerLocomotive && this.Train.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING) // only set advanced wheel slip when advanced adhesion and is the player locomotive, AI locomotive will not work to this model. Don't use slip model when train is in auto pilot
            {
            float SlipCutoffPressureAtmPSI;
            float SlipCylinderReleasePressureAtmPSI;
            float SlipInitialPressureAtmPSI;
            

            // Starting tangential force - at starting piston force is based upon cutoff pressure  & interia = 0
            if (SteamEngineType == SteamEngineTypes.Compound)
            {
                if (!CylinderCompoundOn) // Compound Mode
                {
                    StartPistonForceLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * HPCylinderInitialPressureAtmPSI; // Piston force is equal to pressure in piston and piston area
                    SlipInitialPressureAtmPSI = HPCylinderInitialPressureAtmPSI;
                    SlipCutoffPressureAtmPSI = HPCylinderCutoffPressureAtmPSI;
                    SlipCylinderReleasePressureAtmPSI = HPCylinderExhaustPressureAtmPSI;
                }
                else  // Simple mode
                {
                    StartPistonForceLbf = Me2.ToIn2(Me2.FromFt2(LPCylinderPistonAreaFt2)) * LPCylinderInitialPressureAtmPSI; // Piston force is equal to pressure in piston and piston area
                    SlipInitialPressureAtmPSI = LPCylinderInitialPressureAtmPSI;
                    SlipCutoffPressureAtmPSI = LPCylinderPreCutoffPressureAtmPSI;
                    SlipCylinderReleasePressureAtmPSI = LPCylinderReleasePressureAtmPSI;
                }
            }
            else // simple locomotive
            {
                StartPistonForceLbf = Me2.ToIn2(Me2.FromFt2(CylinderPistonAreaFt2)) * InitialPressureAtmPSI; // Piston force is equal to pressure in piston and piston area
                SlipInitialPressureAtmPSI = InitialPressureAtmPSI;
                SlipCutoffPressureAtmPSI = CutoffPressureAtmPSI;
                SlipCylinderReleasePressureAtmPSI = CylinderReleasePressureAtmPSI;
            }

            // At starting, for 2 cylinder locomotive, maximum tangential force occurs at the following crank angles:
            // Backward - 45 deg & 135 deg, Forward - 135 deg & 45 deg. To calculate the maximum we only need to select one of these points
            // To calculate total tangential force we need to calculate the left and right hand side of the locomotive, LHS & RHS will be 90 deg apart

            if (NumCylinders == 3.0)
            {
               // Calculate values at start
                StartCrankAngleLeft = RadConvert * 30.0f;	// For 3 Cylinder locomotive, cranks are 120 deg apart, and maximum occurs @ 
                StartCrankAngleMiddle = RadConvert * 150.0f;	// 30, 150, 270 deg crank angles
                StartCrankAngleRight = RadConvert * 270.0f;
                StartTangentialCrankForceFactorLeft = (float)Math.Abs(((float)Math.Sin(StartCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleLeft) * (float)Math.Cos(StartCrankAngleLeft))));
                StartTangentialCrankForceFactorMiddle = (float)Math.Abs(((float)Math.Sin(StartCrankAngleMiddle) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleMiddle) * (float)Math.Cos(StartCrankAngleMiddle))));
                StartTangentialCrankForceFactorRight = (float)Math.Abs(((float)Math.Sin(StartCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleRight) * (float)Math.Cos(StartCrankAngleRight))));
                StartVerticalThrustForceMiddle = 0.0f;

                // Calculate values at speed
                SpeedCrankAngleLeft = RadConvert * 30.0f;	// For 3 Cylinder locomotive, cranks are 120 deg apart, and maximum occurs @ 
                SpeedCrankAngleMiddle = RadConvert * (30.0f + 120.0f + 120.0f);	// 30, 150, 270 deg crank angles
                SpeedCrankAngleRight = RadConvert * (30.0f + 120.0f);
                SpeedTangentialCrankForceFactorLeft = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleLeft) * (float)Math.Cos(SpeedCrankAngleLeft))));
                SpeedTangentialCrankForceFactorMiddle = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleMiddle) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleMiddle) * (float)Math.Cos(SpeedCrankAngleMiddle))));
                SpeedTangentialCrankForceFactorRight = (float)Math.Abs(((float)Math.Sin(SpeedCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleRight) * (float)Math.Cos(SpeedCrankAngleRight))));
                SpeedVerticalThrustForceMiddle = 0.0f;
                SpeedCrankCylinderPositionLeft = 30.0f / 180.0f;
                SpeedCrankCylinderPositionMiddle = ((30.0f + 120.0f + 120.0f) - 180.0f) / 180.0f;
                SpeedCrankCylinderPositionRight = (30.0f + 120.0f) / 180.0f;
            }
            else // if 2 cylinder
            {
                // Calculate values at start
                StartCrankAngleLeft = RadConvert * 45.0f;	// For 2 Cylinder locomotive, cranks are 90 deg apart, and maximum occurs @ 
                StartCrankAngleMiddle = RadConvert * 0.0f;
                StartCrankAngleRight = RadConvert * (45.0f + 90.0f);	// 315 & 45 deg crank angles
                StartTangentialCrankForceFactorLeft = ((float)Math.Sin(StartCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleLeft) * (float)Math.Cos(StartCrankAngleLeft)));
                StartTangentialCrankForceFactorRight = ((float)Math.Sin(StartCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(StartCrankAngleRight) * (float)Math.Cos(StartCrankAngleRight)));
                StartTangentialCrankForceFactorMiddle = 0.0f;

                // Calculate values at speed
                SpeedCrankAngleLeft = RadConvert * 45.0f;	// For 2 Cylinder locomotive, cranks are 90 deg apart, and maximum occurs @ 
                SpeedCrankAngleMiddle = 0.0f;	// 315 & 45 deg crank angles
                SpeedCrankAngleRight = RadConvert * (45.0f + 90.0f);
                SpeedTangentialCrankForceFactorLeft = ((float)Math.Sin(SpeedCrankAngleLeft) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleLeft) * (float)Math.Cos(SpeedCrankAngleLeft)));
                SpeedTangentialCrankForceFactorRight = ((float)Math.Sin(SpeedCrankAngleRight) + ((CrankRadiusFt / ConnectRodLengthFt) * (float)Math.Sin(SpeedCrankAngleRight) * (float)Math.Cos(SpeedCrankAngleRight)));
                SpeedVerticalThrustForceMiddle = 0.0f;
                SpeedCrankCylinderPositionLeft = 45.0f / 180.0f;
                SpeedCrankCylinderPositionMiddle = 0.0f;
                SpeedCrankCylinderPositionRight = (45.0f + 90.0f) / 180.0f;

            }

            // Calculate the starting force at the crank exerted on the drive wheel
            StartTangentialCrankWheelForceLbf = Math.Abs(StartPistonForceLbf * StartTangentialCrankForceFactorLeft) + Math.Abs(StartPistonForceLbf * StartTangentialCrankForceFactorMiddle) + Math.Abs(StartPistonForceLbf * StartTangentialCrankForceFactorRight);

     // Calculate cylinder presssure at "maximum" cranking value

            // Left hand crank position cylinder pressure
            if (cutoff > SpeedCrankCylinderPositionLeft )  // If cutoff is greater then crank position, then pressure will be before cutoff
            {
                CrankLeftCylinderPressure = (SlipCutoffPressureAtmPSI / cutoff) * SpeedCrankCylinderPositionLeft;
                CrankLeftCylinderPressure = MathHelper.Clamp(SlipCutoffPressureAtmPSI, 0, InitialPressureAtmPSI);
            }
            else // Pressure will be in the expansion section of the cylinder
            {
                // Crank pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                CrankLeftCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (SpeedCrankCylinderPositionLeft + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
            }

            // Right hand cranking position cylinder pressure
            if (CylinderExhaustOpenFactor > SpeedCrankCylinderPositionRight) // if exhaust opening is greating then cranking position, then pressure will be before release 
            {
                CrankRightCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (SpeedCrankCylinderPositionRight + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
            }
            else  // Pressure will be after release
            {
                CrankRightCylinderPressure = (SlipCylinderReleasePressureAtmPSI / CylinderExhaustOpenFactor) * SpeedCrankCylinderPositionRight;
            }

            if (NumCylinders == 3)
            {
                // Middle crank position cylinder pressure
                if (cutoff > SpeedCrankCylinderPositionLeft)  // If cutoff is greater then crank position, then pressure will be before cutoff
                {
                    CrankMiddleCylinderPressure = (SlipCutoffPressureAtmPSI / cutoff) * SpeedCrankCylinderPositionMiddle;
                    CrankMiddleCylinderPressure = MathHelper.Clamp(SlipCutoffPressureAtmPSI, 0, InitialPressureAtmPSI);
                }
                else // Pressure will be in the expansion section of the cylinder
                {
                    // Crank pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
                    CrankMiddleCylinderPressure = (SlipCutoffPressureAtmPSI) * (cutoff + CylinderClearancePC) / (SpeedCrankCylinderPositionMiddle + CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust
                }
            }
            else
            {

                CrankMiddleCylinderPressure = 0.0f;
            }
   // Calculate piston force for the relevant cylinder cranking positions
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

            SpeedTangentialCrankWheelForceLeftLbf = SpeedPistonForceLeftLbf + ReciprocatingInertiaForceLeft + ConnectRodInertiaForceLeft;
            SpeedTangentialCrankWheelForceMiddleLbf = SpeedPistonForceMiddleLbf + ReciprocatingInertiaForceMiddle + ConnectRodInertiaForceMiddle;
            SpeedTangentialCrankWheelForceRightLbf = SpeedPistonForceRightLbf + ReciprocatingInertiaForceRight + ConnectRodInertiaForceRight;

            if(NumCylinders == 2)
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
            
            if(NumCylinders == 2)
            {
                StartVerticalThrustForceMiddle = 0.0f;
            }   

            StartVerticalThrustForceLeft = StartPistonForceLbf * StartVerticalThrustFactorLeft;
            StartVerticalThrustForceMiddle = StartPistonForceLbf * StartVerticalThrustFactorMiddle;
            StartVerticalThrustForceRight = StartPistonForceLbf * StartVerticalThrustFactorRight;

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
            float ExcessBalanceWeightLb = (ConnectingRodWeightLb + ReciprocatingWeightLb) - ConnectingRodBalanceWeightLb -(Kg.ToLb(MassKG) / ExcessBalanceFactor);
            ExcessBalanceForceLeft = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleLeft) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
            ExcessBalanceForceMiddle = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleMiddle) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));
            ExcessBalanceForceRight = -1.603f * ExcessBalanceWeightLb * Me.ToIn(CylinderStrokeM) * (float)Math.Sin(SpeedCrankAngleRight) * ((MpS.ToMpH(absSpeedMpS) * MpS.ToMpH(absSpeedMpS)) / (Me.ToIn(DrvWheelDiaM) * Me.ToIn(DrvWheelDiaM)));

            if (NumCylinders == 2)
            {
                ExcessBalanceForceMiddle = 0.0f;
            }

            SpeedStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) + (SpeedVerticalThrustForceLeft + ExcessBalanceForceLeft) + (SpeedVerticalThrustForceMiddle + ExcessBalanceForceMiddle) + (SpeedVerticalThrustForceRight + ExcessBalanceForceRight)) * Train.LocomotiveCoefficientFriction;

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

            SteamDrvWheelWeightLbs = Kg.ToLb(DrvWheelWeightKg / LocoNumDrvWheels); // Calculate the weight per axle (used in MSTSLocomotive for friction calculatons)

            // Static Friction Force - adhesive factor increased by vertical thrust when travelling forward, and reduced by vertical thrust when travelling backwards

            if (Direction == Direction.Forward)
            {
                StartStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) + StartVerticalThrustForceLeft + StartVerticalThrustForceRight + StartVerticalThrustForceMiddle) * Train.LocomotiveCoefficientFriction;
            }
            else
            {
                StartStaticWheelFrictionForceLbf = (Kg.ToLb(DrvWheelWeightKg) - StartVerticalThrustForceLeft - StartVerticalThrustForceMiddle - StartVerticalThrustForceRight) * Train.LocomotiveCoefficientFriction;
            }

            if (absSpeedMpS < 1.0)  // For low speed use the starting values
            {
                SteamStaticWheelForce = StartStaticWheelFrictionForceLbf;
                SteamTangentialWheelForce = StartTangentialWheelTreadForceLbf;
            }
            else // for high speed use "running values"
            {
                SteamStaticWheelForce = SpeedStaticWheelFrictionForceLbf;
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
                    TotalWheelMomentofInertia *= LocoNumDrvWheels;
                    
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

                    if(FrictionWheelSpeedMpS > WheelSpeedMpS) // If slip speed is greater then normal forward speed use slip speed
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

      //      Trace.TraceInformation("Loco Speed - Wheelspeed {0} Slip {1} Train {2}", WheelSpeedMpS, WheelSpeedSlipMpS, SpeedMpS);

            #endregion

            // Derate when priming is occurring.
            if (BoilerIsPriming)
                MotiveForceN *= BoilerPrimingDeratingFactor;

            if( FusiblePlugIsBlown) // If fusible plug blows, then reduve motive force
            {
                MotiveForceN = 0.5f;
            }


            // Find the maximum TE for debug i.e. @ start and full throttle
            if (absSpeedMpS < 1.0)
            {
                if (MotiveForceN > StartTractiveEffortN && MotiveForceN < MaxForceN)
                {
                    StartTractiveEffortN = MotiveForceN; // update to new maximum TE
                }
            }
        }

        private void UpdateAuxiliaries(float elapsedClockSeconds, float absSpeedMpS)
        {
            // Only calculate compressor consumption if it is not a vacuum controlled steam engine
            if (!(BrakeSystem is Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS.VacuumSinglePipe))
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
            }
            else // Train is vacuum brake controlled, and steam ejector is used
            {

                // Calculate small steam ejector steam usage
                SteamEjectorSmallSetting = SmallEjectorController.CurrentValue;
                SteamEjectorSmallPressurePSI = BoilerPressurePSI * SteamEjectorSmallSetting;
                SteamEjectorSmallBaseSteamUsageLbpS = pS.FrompH(SteamEjectorSteamUsageLBpHtoPSI[SteamEjectorSmallPressurePSI]);

                SmallEjectorCapacityFactor = SteamEjectorCapacityFactorIntoX[SteamEjectorSmallDiameterIn];
                EjectorSmallSteamConsumptionLbpS = SteamEjectorSmallBaseSteamUsageLbpS * SmallEjectorCapacityFactor;

                if (SteamEjectorSmallSetting > 0.1f) // Test to see if small steam ejector is on
                {
                    SmallSteamEjectorIsOn = true;
                }
                else
                {
                    SmallSteamEjectorIsOn = false;
                }

         //       Trace.TraceInformation("Small: Press {0} Base Cons {1} Factor {2} Total Cons {3} Raw {4}", SteamEjectorSmallPressurePSI, SteamEjectorSmallBaseSteamLbpS, SmallEjectorCapacityFactor, EjectorSmallSteamConsumptionLbpS, SteamEjectorSteamUsageLBpHtoPSI[SteamEjectorSmallPressurePSI]); 

       //         Trace.TraceInformation("Vacuum Pump {0}", VacuumPumpFitted);
                // Calculate large steam ejector steam usage if no vacuum pump fitted, and the brake turns the large ejector on
                if (!VacuumPumpFitted && LargeSteamEjectorIsOn)
                {
                    SteamEjectorLargeBaseUsageSteamLbpS = pS.FrompH(SteamEjectorSteamUsageLBpHtoPSI[SteamEjectorLargePressurePSI]);

                    LargeEjectorCapacityFactor = SteamEjectorCapacityFactorIntoX[SteamEjectorLargeDiameterIn];
                    EjectorLargeSteamConsumptionLbpS = SteamEjectorLargeBaseUsageSteamLbpS * LargeEjectorCapacityFactor;
                }
                else
                {
                    EjectorLargeSteamConsumptionLbpS = 0.0f;
                }
                // Calculate Total steamconsumption for Ejectors
                EjectorTotalSteamConsumptionLbpS = EjectorSmallSteamConsumptionLbpS + EjectorLargeSteamConsumptionLbpS;

                BoilerMassLB -= elapsedClockSeconds * EjectorTotalSteamConsumptionLbpS; // Reduce boiler mass to reflect steam usage by compressor
                BoilerHeatBTU -= elapsedClockSeconds * EjectorTotalSteamConsumptionLbpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor
                BoilerHeatOutBTUpS += EjectorTotalSteamConsumptionLbpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by compressor
                TotalSteamUsageLBpS += EjectorTotalSteamConsumptionLbpS;
            }
            // Calculate cylinder cock steam Usage if turned on
            // The cock steam usage will be assumed equivalent to a steam orifice
            // Steam Flow (lb/hr) = 24.24 x Press(Cylinder + Atmosphere(psi)) x CockDia^2 (in) - this needs to be multiplied by Num Cyls
            if (CylinderCocksAreOpen == true)
            {
                if (throttle > 0.00 && absSpeedMpS > 0.1) // if regulator open & train moving
                {
                    CylCockSteamUsageLBpS = pS.FrompH(NumCylinders * (24.24f * (CylinderPressureAtmPSI) * CylCockDiaIN * CylCockDiaIN));
                    BoilerMassLB -= elapsedClockSeconds * CylCockSteamUsageLBpS; // Reduce boiler mass to reflect steam usage by cylinder steam cocks  
                    BoilerHeatBTU -= elapsedClockSeconds * CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    BoilerHeatOutBTUpS += CylCockSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks                
                    TotalSteamUsageLBpS += CylCockSteamUsageLBpS;
                    CylCockSteamUsageDisplayLBpS = CylCockSteamUsageLBpS;
                }
                else if (throttle > 0.00 && absSpeedMpS <= 0.1) // if regulator open and train stationary
                {
                    CylCockSteamUsageLBpS = 0.0f; // set usage to zero if regulator closed
                    CylCockSteamUsageStatLBpS = pS.FrompH(NumCylinders * (24.24f * (CutoffPressureAtmPSI) * CylCockDiaIN * CylCockDiaIN));
                    BoilerMassLB -= elapsedClockSeconds * CylCockSteamUsageStatLBpS; // Reduce boiler mass to reflect steam usage by cylinder steam cocks  
                    BoilerHeatBTU -= elapsedClockSeconds * CylCockSteamUsageStatLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks
                    BoilerHeatOutBTUpS += CylCockSteamUsageStatLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB);  // Reduce boiler Heat to reflect steam usage by cylinder steam cocks                
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
            if (InjCylEquivSizeIN <= 19.0)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector09FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 9mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector09FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 9mm Injector Flow rate 
                InjectorSize = 09.0f; // store size for display in HUD
            }
            else if (InjCylEquivSizeIN <= 24.0)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector10FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 10mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector10FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 10 mm Injector Flow rate 
                InjectorSize = 10.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 26.0)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector11FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 11mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector11FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 11 mm Injector Flow rate 
                InjectorSize = 11.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 28.0)
            {
                MaxInjectorFlowRateLBpS = pS.FrompM(Injector13FlowratePSItoUKGpM[MaxBoilerPressurePSI]) * WaterLBpUKG; // 13mm Injector maximum flow rate @ maximm boiler pressure
                InjectorFlowRateLBpS = pS.FrompM(Injector13FlowratePSItoUKGpM[BoilerPressurePSI]) * WaterLBpUKG; // 13 mm Injector Flow rate 
                InjectorSize = 13.0f; // store size for display in HUD                
            }
            else if (InjCylEquivSizeIN <= 30.0)
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
                    DamperBurnEffect = DamperController.CurrentValue * absSpeedMpS * DamperFactorManual; // Damper value for manual firing - related to damper setting and increased speed
                }
                DamperBurnEffect = MathHelper.Clamp(DamperBurnEffect, 0.0f, TheoreticalMaxSteamOutputLBpS); // set damper maximum to the max generation rate
            }
            #endregion

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

                // Put sound triggers in for the injectors in AI Fireman mode
                SignalEvent(Injector1IsOn ? Event.WaterInjector1On : Event.WaterInjector1Off); // hook for sound trigger
                SignalEvent(Injector2IsOn ? Event.WaterInjector2On : Event.WaterInjector2Off); // hook for sound trigger

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

                // Determine Heat Ratio - for calculating burn rate

                if (BoilerHeat && !ShovelAnyway) // If heat in boiler is going too high
                {
                    if (EvaporationLBpS > TotalSteamUsageLBpS)
                    {
                        HeatRatio = MathHelper.Clamp((((BoilerHeatOutBTUpS - BoilerHeatOutSVAIBTUpS) / BoilerHeatInBTUpS) * (TotalSteamUsageLBpS / EvaporationLBpS)), 0.001f, 1.0f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only, clamp < 1 if max boiler heat reached.
                    }
                    else
                    {
                        HeatRatio = MathHelper.Clamp((((BoilerHeatOutBTUpS - BoilerHeatOutSVAIBTUpS) / BoilerHeatInBTUpS) * (EvaporationLBpS / TotalSteamUsageLBpS)), 0.001f, 1.0f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only, clamp < 1 if max boiler heat reached.
                    }
                }
                else  // If heat in boiler is normal or low
                {
                    if (PressureRatio > 1.1) // If pressure drops by more then 10%, increase firing rate
                    {
                        float HeatFactor = (BoilerHeatOutBTUpS - BoilerHeatInBTUpS);
                        if (HeatFactor < 0)
                        {
                            HeatFactor *= -1.0f; // If negative convert to positive number
                        }
                        HeatRatio = MathHelper.Clamp((((BoilerHeatOutBTUpS - BoilerHeatOutSVAIBTUpS) * HeatFactor) / BoilerHeatInBTUpS), 0.001f, 1.6f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only
                    }
                    else // if boiler pressure is "normal"
                    {
                        HeatRatio = MathHelper.Clamp(((BoilerHeatOutBTUpS - BoilerHeatOutSVAIBTUpS) / BoilerHeatInBTUpS), 0.001f, 1.6f);  // Factor to determine how hard to drive burn rate, based on steam gen & usage rate, in AI mode only
                    }

                }

            }
            #endregion

        }

        private void UpdateSteamHeat(float elapsedClockSeconds)
        {
            // Update Steam Heating System

            // TO DO - Add test to see if cars are coupled, if Light Engine, disable steam heating.

            if (IsSteamHeatFitted && Train.TrainFittedSteamHeat)  // Only Update steam heating if train and locomotive fitted with steam heating, and is a passenger train
            {

                if (IsSteamHeatFirstTime)
                {
                    IsSteamHeatFirstTime = false;  // TrainCar and Train have not executed during first pass of steam locomotive, so ignore steam heating the first time
                }
                else
                {
                    // After first pass continue as normal

                    // Set default temperature values 
                    Train.TrainInsideTempC = InsideTempC;
                    Train.TrainOutsideTempC = OutsideTempC;
                    Train.TrainCurrentCarriageHeatTempC = CurrentCarriageHeatTempC; // Temp value

                    // Carriage temperature will be equal to heat input (from steam pipe) less heat losses through carriage walls, etc
                    // Calculate Heat in Train
                    TotalTrainSteamHeatW = SpecificHeatCapcityAirKJpKgK * DensityAirKgpM3 * Train.TrainHeatVolumeM3 * (InsideTempC - OutsideTempC);

                    if (IsSteamInitial)
                    {
                        CurrentTrainSteamHeatW = TotalTrainSteamHeatW;
                        IsSteamInitial = false;
                    }


                    // Calculate steam pipe heat energy
                    if (CurrentSteamHeatPressurePSI <= MaxSteamHeatPressurePSI)      // Don't let steam heat pressure exceed the maximum value
                    {
                        CurrentSteamHeatPressurePSI = SteamHeatController.CurrentValue * MaxSteamHeatPressurePSI;
                    }

                    CurrentSteamHeatPressurePSI = MathHelper.Clamp(CurrentSteamHeatPressurePSI, 0.0f, MaxSteamHeatPressurePSI);  // Clamp steam heat pressure within bounds

                    if (CurrentSteamHeatPressurePSI == 0.0)
                    {
                        CurrentSteamHeatPipeTempC = 0.0f;       // Reset values to zero if steam is not turned on.
                        SteamPipeHeatW = 0.0f;
                    }
                    else
                    {
                        CurrentSteamHeatPipeTempC = C.FromF(PressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]);
                        SteamPipeHeatConvW = (PipeHeatTransCoeffWpM2K * Train.TrainHeatPipeAreaM2 * (C.ToK(CurrentSteamHeatPipeTempC) - C.ToK(CurrentCarriageHeatTempC)));
                        float PipeTempAK = (float)Math.Pow(C.ToK(CurrentSteamHeatPipeTempC), 4.0f);
                        float PipeTempBK = (float)Math.Pow(C.ToK(CurrentCarriageHeatTempC), 4.0f);
                        SteamHeatPipeRadW = (BoltzmanConstPipeWpM2 * (PipeTempAK - PipeTempBK));
                        SteamPipeHeatW = SteamPipeHeatConvW + SteamHeatPipeRadW;   // heat generated by pipe per degree
                    }

                    // Calculate Net steam heat loss or gain
                    NetSteamHeatLossWpTime = SteamPipeHeatW - Train.TrainSteamHeatLossWpT;
                    DisplayNetSteamHeatLossWpTime = NetSteamHeatLossWpTime;


                    if (NetSteamHeatLossWpTime < 0)
                    {
                        NetSteamHeatLossWpTime = -1.0f * NetSteamHeatLossWpTime;
                        CurrentTrainSteamHeatW -= NetSteamHeatLossWpTime * elapsedClockSeconds;  // Losses per elapsed time
                    }
                    else
                    {
                        CurrentTrainSteamHeatW += NetSteamHeatLossWpTime * elapsedClockSeconds;  // Gains per elapsed time         
                    }

                    float MaximumHeatTempC = 30.0f;     // Allow heat to go to 86oF (30oC)

                    if (CurrentCarriageHeatTempC > OutsideTempC && CurrentCarriageHeatTempC <= MaximumHeatTempC && TotalTrainSteamHeatW > 0.0)
                    {
                        CurrentCarriageHeatTempC = (((InsideTempC - OutsideTempC) * CurrentTrainSteamHeatW) / TotalTrainSteamHeatW) + OutsideTempC;
                    }

                    // Test to see if steam heating temp has exceeded the comfortable heating value.

                    if (CurrentCarriageHeatTempC > InsideTempC)
                    {
                        if (!IsSteamHeatExceeded)
                        {
                            IsSteamHeatExceeded = true;
                            // Provide warning message if temperature is too hot
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Carriage temperature is too hot, the passengers are sweating."));
                        }
                    }
                    else if (CurrentCarriageHeatTempC < InsideTempC - 1.0f)
                    {

                        IsSteamHeatExceeded = false;        // Reset temperature warning
                    }

                    // Test to see if steam heating temp has dropped too low.

                    if (CurrentCarriageHeatTempC < 10.0f) // If temp below 50of (10oC) then alarm
                    {
                        if (!IsSteamHeatLow)
                        {
                            IsSteamHeatLow = true;
                            // Provide warning message if temperature is too hot
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Carriage temperature is too cold, the passengers are freezing."));
                        }
                    }
                    else if (CurrentCarriageHeatTempC > 13.0f)
                    {

                        IsSteamHeatLow = false;        // Reset temperature warning
                    }

                    float ConvertBtupLbtoKjpKg = 2.32599999962f;  // Conversion factor
                    // Calculate steam usage
                    // Only set up for saturated steam at this time - needs to also work for superheated steam
                    if (SteamPipeHeatW == 0.0 || CurrentSteamHeatPressurePSI == 0.0)
                    {
                        CalculatedCarHeaterSteamUsageLBpS = 0.0f;       // Zero steam usage if Steam pipe heat is zero
                    }
                    else
                    {
                        CalculatedCarHeaterSteamUsageLBpS = Kg.ToLb(W.ToKW(SteamPipeHeatW) / (SteamHeatPSItoBTUpLB[CurrentSteamHeatPressurePSI] * ConvertBtupLbtoKjpKg));
                    }

#if DEBUG_LOCO_STEAM_HEAT

                Trace.TraceInformation("***************************************** DEBUG_LOCO_STEAM_HEAT (MSTSSteamLocomotive.cs) #2 ***************************************************************");
                Trace.TraceInformation("Steam Contoller {0}", SteamHeatController.CurrentValue);
                Trace.TraceInformation("Steam Pipe Heat - Steam Heat Pressure {0} Steam Pipe Temp {1} Steam Pipe Heat Tot {2} Pipe Heat Conv {3} Pipe Heat {4} Train Steam Pipe Area {5}", CurrentSteamHeatPressurePSI, CurrentSteamHeatPipeTempC, SteamPipeHeatW, SteamPipeHeatConvW, SteamHeatPipeRadW, Train.TrainHeatPipeAreaM2);
                Trace.TraceInformation("Steam Usage - Usage {0} latent Heat {1}", CalculatedCarHeaterSteamUsageLBpS, (SteamHeatPSItoBTUpLB[CurrentSteamHeatPressurePSI] * ConvertBtupLbtoKjpKg));
                Trace.TraceInformation("Train Heat Loss {0} Net Train Heat Loss {1}", Train.TrainSteamHeatLossWpT, DisplayNetSteamHeatLossWpTime);
                Trace.TraceInformation("Total Train Heat {0} Inside Temp {1} Outside Temp {2} Train Volume {3}", TotalTrainSteamHeatW, InsideTempC, OutsideTempC, Train.TrainHeatVolumeM3);
                Trace.TraceInformation("Sec {0} Net Train Heat Loss {1} Current Train Steam Heat {2}", elapsedClockSeconds, NetSteamHeatLossWpTime, CurrentTrainSteamHeatW);

#endif

                    // Calculate impact of steam heat usage on locomotive
                    BoilerMassLB -= elapsedClockSeconds * CalculatedCarHeaterSteamUsageLBpS;
                    BoilerHeatBTU -= elapsedClockSeconds * CalculatedCarHeaterSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to steam heat usage
                    TotalSteamUsageLBpS += CalculatedCarHeaterSteamUsageLBpS;
                    BoilerHeatOutBTUpS += CalculatedCarHeaterSteamUsageLBpS * (BoilerSteamHeatBTUpLB - BoilerWaterHeatBTUpLB); // Heat loss due to safety valve                
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
                case CABViewControlTypes.ORTS_CYL_COMP:
                    data = CylinderCompoundOn ? 1 : 0;
                    break;
                case CABViewControlTypes.BLOWER:
                    data = BlowerController.CurrentValue;
                    break;
                case CABViewControlTypes.STEAM_HEAT:
                    data = SteamHeatController.CurrentValue;
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
                case CABViewControlTypes.SMALL_EJECTOR:
                    {
                        data = SmallEjectorController.CurrentValue;
                        break;
                    }
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }
            return data;
        }

        public override string GetStatus()
        {
            var boilerPressurePercent = BoilerPressurePSI / MaxBoilerPressurePSI;
            var boilerPressureSafety = boilerPressurePercent <= 0.25 ? "!!!" : boilerPressurePercent <= 0.5 ? "???" : "";
            var boilerWaterSafety = WaterFraction < WaterMinLevel || WaterFraction > WaterMaxLevel ? "!!!" : WaterFraction < WaterMinLevelSafe || WaterFraction > WaterMaxLevelSafe ? "???" : "";
            var coalPercent = TenderCoalMassKG / MaxTenderCoalMassKG;
            var waterPercent = TenderWaterVolumeUKG / (Kg.ToLb(MaxTenderWaterMassKG) / WaterLBpUKG);
            var fuelSafety = CoalIsExhausted || WaterIsExhausted ? "!!!" : coalPercent <= 0.105 || waterPercent <= 0.105 ? "???" : "";
            var status = new StringBuilder();

            if (IsFixGeared)
                status.AppendFormat("{0} = 1 ({1:F2})\n", Simulator.Catalog.GetString("Fixed gear"), SteamGearRatio);
            else if (IsSelectGeared)
                status.AppendFormat("{0} = {2} ({1:F2})\n", Simulator.Catalog.GetString("Gear"),
                    SteamGearRatio, SteamGearPosition == 0 ? Simulator.Catalog.GetParticularString("Gear", "N") : SteamGearPosition.ToString());

            status.AppendFormat("{0} = {1}/{2}\n", Simulator.Catalog.GetString("Steam usage"), FormatStrings.FormatMass(pS.TopH(Kg.FromLb(PreviousTotalSteamUsageLBpS)), MainPressureUnit != PressureUnit.PSI), FormatStrings.h);
            status.AppendFormat("{0}{2} = {1}{2}\n", Simulator.Catalog.GetString("Boiler pressure"), FormatStrings.FormatPressure(BoilerPressurePSI, PressureUnit.PSI, MainPressureUnit, true), boilerPressureSafety);
            status.AppendFormat("{0}{2} = {1:F0}% {3}{2}\n", Simulator.Catalog.GetString("Boiler water glass"), 100 * waterGlassPercent, boilerWaterSafety, FiringIsManual ? Simulator.Catalog.GetString("(safe range)") : "");

            if (FiringIsManual)
            {
                status.AppendFormat("{0}{3} = {2:F0}% {1}{3}\n", Simulator.Catalog.GetString("Boiler water level"), Simulator.Catalog.GetString("(absolute)"), WaterFraction * 100, boilerWaterSafety);
                if (IdealFireMassKG > 0)
                    status.AppendFormat("{0} = {1:F0}%\n", Simulator.Catalog.GetString("Fire mass"), FireMassKG / IdealFireMassKG * 100);
                else
                    status.AppendFormat("{0} = {1:F0}%\n", Simulator.Catalog.GetString("Fire ratio"), FireRatio * 100);
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

            status.AppendFormat("{0}\t{1}\t{4:N1}\t{2}\t{5:N2}\t{3}\t{6:N2}\n",
                Simulator.Catalog.GetString("Adj:"),
                Simulator.Catalog.GetString("CylEff"),
                Simulator.Catalog.GetString("CylExh"),
                Simulator.Catalog.GetString("PortOpen"),
                CylinderEfficiencyRate,
                CylinderExhaustOpenFactor,
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
                BoilerEfficiencyGrateAreaLBpFT2toX[(pS.TopH(Kg.ToLb(FuelBurnRateKGpS)) / Me2.ToFt2(GrateAreaM2))]);

            status.AppendFormat("{0}\t{1}\t{5}\t{2}\t{6}\t{3}\t{7}\t\t{4}\t{8}\n",
                Simulator.Catalog.GetString("Heat:"),
                Simulator.Catalog.GetString("In"),
                Simulator.Catalog.GetString("Out"),
                Simulator.Catalog.GetString("Stored"),
                Simulator.Catalog.GetString("Max"),
                FormatStrings.FormatPower(W.FromBTUpS(BoilerHeatInBTUpS), IsMetric, false, true),
                FormatStrings.FormatPower(W.FromBTUpS(PreviousBoilerHeatOutBTUpS), IsMetric, false, true),
                FormatStrings.FormatEnergy(W.FromBTUpS(BoilerHeatSmoothBTU.Value), IsMetric),
                FormatStrings.FormatEnergy(W.FromBTUpS(MaxBoilerHeatBTU), IsMetric));

            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\n",
                Simulator.Catalog.GetString("Temp:"),
                Simulator.Catalog.GetString("Flue"),
                FormatStrings.FormatTemperature(C.FromK(FlueTempK), IsMetric, false),
                Simulator.Catalog.GetString("Water"),
                FormatStrings.FormatTemperature(C.FromK(BoilerWaterTempK), IsMetric, false),
                Simulator.Catalog.GetString("MaxSupH"),
                FormatStrings.FormatTemperature(C.FromF(SuperheatRefTempF), IsMetric, false),
                Simulator.Catalog.GetString("CurSupH"),
                FormatStrings.FormatTemperature(C.FromF(CurrentSuperheatTempF), IsMetric, false));

            status.AppendFormat("\n\t\t === {0} === \t\t{1}/{2}\n",
                Simulator.Catalog.GetString("Steam Usage"),
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(PreviousTotalSteamUsageLBpS)), IsMetric),
                FormatStrings.h);

            if (!(BrakeSystem is Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS.VacuumSinglePipe))
            {
                // Display air compressor information
                status.AppendFormat("{0}\t{1}\t{10}/{21}\t{2}\t{11}/{21}\t{3}\t{12}/{21}\t{4}\t{13}/{21}\t{5}\t{14}/{21}\t{6}\t{15}/{21}\t{7}\t{16}/{21}\t{8}\t{17}/{21}\t{9}\t{18}/{21} ({19}x{20:N1}\")\n",
                    Simulator.Catalog.GetString("Usage:"),
                    Simulator.Catalog.GetString("Cyl"),
                    Simulator.Catalog.GetString("Blower"),
                    Simulator.Catalog.GetString("Radiation"),
                    Simulator.Catalog.GetString("Comprsr"),
                    Simulator.Catalog.GetString("SafetyV"),
                    Simulator.Catalog.GetString("CylCock"),
                    Simulator.Catalog.GetString("Genertr"),
                    Simulator.Catalog.GetString("Stoker"),
                    Simulator.Catalog.GetString("MaxSafe"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylinderSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowerSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(RadiationSteamLossLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CompSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(SafetyValveUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylCockSteamUsageDisplayLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(GeneratorSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(StokerSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(MaxSafetyValveDischargeLbspS)), IsMetric),
                    NumSafetyValves,
                    SafetyValveSizeIn,
                    FormatStrings.h);
            }
            else
            {
                // Display steam ejector information instead of air compressor
                status.AppendFormat("{0}\t{1}\t{10}/{21}\t{2}\t{11}/{21}\t{3}\t{12}/{21}\t{4}\t{13}/{21}\t{5}\t{14}/{21}\t{6}\t{15}/{21}\t{7}\t{16}/{21}\t{8}\t{17}/{21}\t{9}\t{18}/{21} ({19}x{20:N1}\")\n",
                    Simulator.Catalog.GetString("Usage:"),
                    Simulator.Catalog.GetString("Cyl"),
                    Simulator.Catalog.GetString("Blower"),
                    Simulator.Catalog.GetString("Radiation"),
                    Simulator.Catalog.GetString("Ejector"),
                    Simulator.Catalog.GetString("SafetyV"),
                    Simulator.Catalog.GetString("CylCock"),
                    Simulator.Catalog.GetString("Genertr"),
                    Simulator.Catalog.GetString("Stoker"),
                    Simulator.Catalog.GetString("MaxSafe"),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylinderSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(BlowerSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(RadiationSteamLossLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(EjectorTotalSteamConsumptionLbpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(SafetyValveUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CylCockSteamUsageDisplayLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(GeneratorSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(StokerSteamUsageLBpS)), IsMetric),
                    FormatStrings.FormatMass(pS.TopH(Kg.FromLb(MaxSafetyValveDischargeLbspS)), IsMetric),
                    NumSafetyValves,
                    SafetyValveSizeIn,
                    FormatStrings.h);

            }

            if (SteamEngineType == SteamEngineTypes.Compound)  // Display Steam Indicator Information for compound locomotive
            {

                // Display steam indicator pressures in HP cylinder
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\n",
                Simulator.Catalog.GetString("PressHP:"),
                Simulator.Catalog.GetString("Chest"),
                FormatStrings.FormatPressure(SteamChestPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Initial"),
                FormatStrings.FormatPressure(HPCylinderInitialPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Cutoff"),
                FormatStrings.FormatPressure(HPCylinderCutoffPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Rel"),
                FormatStrings.FormatPressure(HPCylinderReleasePressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("RelR"),
                FormatStrings.FormatPressure(HPCylinderReleasePressureRecvAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Exhaust"),
                FormatStrings.FormatPressure(HPCylinderExhaustPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Back"),
                FormatStrings.FormatPressure(HPCylinderBackPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("PreComp"),
                FormatStrings.FormatPressure(HPCylinderPreCompressionPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("MEP"),
                FormatStrings.FormatPressure(HPCylinderMEPAtmPSI, PressureUnit.PSI, MainPressureUnit, true));

                // Display steam indicator pressures in LP cylinder
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\n",
                Simulator.Catalog.GetString("PressLP:"),
                Simulator.Catalog.GetString("Chest"),
                FormatStrings.FormatPressure(SteamChestPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Initial"),
                FormatStrings.FormatPressure(LPCylinderInitialPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Cutoff"),
                FormatStrings.FormatPressure(LPCylinderPreCutoffPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Rel"),
                FormatStrings.FormatPressure(LPCylinderReleasePressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("Back"),
                FormatStrings.FormatPressure(LPCylinderBackPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("PreComp"),
                FormatStrings.FormatPressure(LPCylinderPreCompressionPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("PreAdm"),
                FormatStrings.FormatPressure(LPCylinderPreAdmissionPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                Simulator.Catalog.GetString("MEP"),
                FormatStrings.FormatPressure(LPCylinderMEPAtmPSI, PressureUnit.PSI, MainPressureUnit, true));

            }
            else  // Display Steam Indicator Information for single expansion locomotive
            {

                status.AppendFormat("{0}\t{1}\t{9}\t{2}\t{10}\t{3}\t{11}\t{4}\t{12}\t{5}\t{13}\t{6}\t{14}\t{7}\t{15}\t{8}\t{16}\n",
                Simulator.Catalog.GetString("Press:"),
                    Simulator.Catalog.GetString("Chest"),
                    Simulator.Catalog.GetString("Initial"),
                    Simulator.Catalog.GetString("Cutoff"),
                    Simulator.Catalog.GetString("Rel"),
                    Simulator.Catalog.GetString("Back"),
                    Simulator.Catalog.GetString("PreComp"),
                    Simulator.Catalog.GetString("PreAdm"),
                    Simulator.Catalog.GetString("MEP"),
                    FormatStrings.FormatPressure(SteamChestPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(InitialPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(CutoffPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(CylinderReleasePressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(BackPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(CylinderPreCompressionPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(CylinderPreAdmissionPressureAtmPSI, PressureUnit.PSI, MainPressureUnit, true),
                    FormatStrings.FormatPressure(MeanEffectivePressurePSI, PressureUnit.PSI, MainPressureUnit, true));
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
            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\t{13}\t{14:N2}\t{15}\t{16:N2}\t{17}\t{18:N2}\t{19}\t{20:N2}\t{21}\t{22:N2}\n",
                "DbgUse:",
                "SwpVol",
                CylinderSweptVolumeFT3pFT,
                "RelVol",
                CylinderCutoffSteamVolumeFt3,
                "CompVol",
                CylinderClearanceSteamVolumeFt3,
                "RawSt",
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(RawCalculatedCylinderSteamUsageLBpS)), IsMetric),
                "CalcSt",
                FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCylinderSteamUsageLBpS)), IsMetric),
                "ClrWt",
                CylinderClearanceSteamWeightLbs,
                "CutWt",
                CylinderCutoffSteamWeightLbs,
                "TotWt",
                RawCylinderSteamWeightLbs,
                "SupFact",
                SuperheaterSteamUsageFactor,
                "CondFact",
                CylinderCondensationFactor,
                "CondSp",
                CylinderSpeedCondensationFactor);
#endif

            if (IsSteamHeatFitted && Train.TrainFittedSteamHeat)  // Only show steam heating HUD if fitted to locomotive and the train
            {
                // Display Steam Heat info
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}/{9}\t{10}\t{11:N0}\n",
                   Simulator.Catalog.GetString("StHeat:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(CurrentSteamHeatPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("TrTemp"),
                   FormatStrings.FormatTemperature(CurrentCarriageHeatTempC, IsMetric, false),
                   Simulator.Catalog.GetString("StTemp"),
                   FormatStrings.FormatTemperature(CurrentSteamHeatPipeTempC, IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
                   Simulator.Catalog.GetString("NetHt"),
                   DisplayNetSteamHeatLossWpTime);
            }

#if DEBUG_LOCO_STEAM_HEAT_HUD
            status.AppendFormat("\n{0}\t{1}\t{2:N0}\t{3}\t{4:N0}\t{5}\t{6:N0}\t{7}\t{8:N0}\t{9}\t{10:N0}\t{11}\t{12}\n",
                Simulator.Catalog.GetString("StHtDB:"),
                Simulator.Catalog.GetString("TotHt"),
                TotalTrainSteamHeatW,
                Simulator.Catalog.GetString("NetHt"),
                DisplayNetSteamHeatLossWpTime,
                Simulator.Catalog.GetString("PipHt"),
                SteamPipeHeatW,
                Simulator.Catalog.GetString("CarHt"),
                Train.TrainSteamHeatLossWpT,
                Simulator.Catalog.GetString("CurrHt"),
                CurrentTrainSteamHeatW,
                Simulator.Catalog.GetString("Cont"),
                SteamHeatController.CurrentValue);
#endif

            status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Fireman"));
            status.AppendFormat("{0}\t{1}\t{7}\t\t{2}\t{8}\t\t{3}\t{9}/{13}\t\t{4}\t{10}/{13}\t\t{5}\t{11}/{13}\t\t{6}\t{12}/{14}{13}\n",
                Simulator.Catalog.GetString("Fire:"),
                Simulator.Catalog.GetString("Ideal"),
                Simulator.Catalog.GetString("Actual"),
                Simulator.Catalog.GetString("MaxFireR"),
                Simulator.Catalog.GetString("FeedRate"),
                Simulator.Catalog.GetString("BurnRate"),
                Simulator.Catalog.GetString("Combust"),
                FormatStrings.FormatMass(IdealFireMassKG, IsMetric),
                FormatStrings.FormatMass(FireMassKG, IsMetric),
                FormatStrings.FormatMass(pS.TopH(DisplayMaxFiringRateKGpS), IsMetric),
                FormatStrings.FormatMass(pS.TopH(FuelFeedRateKGpS), IsMetric),
                FormatStrings.FormatMass(pS.TopH(FuelBurnRateKGpS), IsMetric),
                FormatStrings.FormatMass(Kg.FromLb(GrateCombustionRateLBpFt2), IsMetric),
                FormatStrings.h,
                IsMetric ? FormatStrings.m2 : FormatStrings.ft2);

#if DEBUG_LOCO_BURN
            status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8:N2}\t{9}\t{10:N2}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\n",
                "DbgBurn:",
                "BoilHeat",
                BoilerHeat ? "Yes" : "No",
                "H/R",
                HeatRatio,
                "BoilH/R",
                BoilerHeatRatio,
                "MBoilH/R",
                MaxBoilerHeatRatio,
                "PrRatio",
                PressureRatio,
                "FireHeat",
                FireHeatTxfKW,
                "RawBurn",
                FormatStrings.FormatMass(pS.TopH(BurnRateRawKGpS), IsMetric),
                "SuperSet",
                IsSuperSet,
                "MaxFuel",
                FormatStrings.FormatMass(pS.TopH(MaxFuelBurnGrateKGpS), IsMetric),
                "BstReset",
                FuelBoostReset ? "Yes" : "No",
                "ShAny",
                ShovelAnyway);
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
                status.AppendFormat("{0}\t{1}\t{2}\t{3:N0}%\t{4}\t{5}\t\t{6:N0}%\t{7}\t{8}\t\t{9}\t{10}\n",
                    Simulator.Catalog.GetString("Tender:"),
                    Simulator.Catalog.GetString("Coal"),
                    FormatStrings.FormatMass(TenderCoalMassKG, IsMetric),
                    TenderCoalMassKG / MaxTenderCoalMassKG * 100,
                    Simulator.Catalog.GetString("Water(C)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CombinedTenderWaterVolumeUKG), IsMetric, IsUK),
                    CombinedTenderWaterVolumeUKG / (Kg.ToLb(MaxTenderWaterMassKG + Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) * 100,
                    Simulator.Catalog.GetString("Water(T)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(TenderWaterVolumeUKG), IsMetric, IsUK),
                    Simulator.Catalog.GetString("Water(A)"),
                    FormatStrings.FormatFuelVolume(L.FromGUK(CurrentAuxTenderWaterVolumeUKG), IsMetric, IsUK));
            }
            else
            {
                status.AppendFormat("{0}\t{1}\t{3}\t{4:N0}%\t{2}\t{5}\t\t{6:N0}%\n",
                    Simulator.Catalog.GetString("Tender:"),
                    Simulator.Catalog.GetString("Coal"),
                    Simulator.Catalog.GetString("Water"),
                    FormatStrings.FormatMass(TenderCoalMassKG, IsMetric),
                    TenderCoalMassKG / MaxTenderCoalMassKG * 100,
                    FormatStrings.FormatFuelVolume(L.FromGUK(CombinedTenderWaterVolumeUKG), IsMetric, IsUK),
                    CombinedTenderWaterVolumeUKG / (Kg.ToLb(MaxTenderWaterMassKG + Train.MaxAuxTenderWaterMassKG) / WaterLBpUKG) * 100);
            }

            status.AppendFormat("{0}\t{1}\t{2}\t\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\n",
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
                IsGrateLimit ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"));

            status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Performance"));
            status.AppendFormat("{0}\t{1}\t{4}\t{2}\t{5}\t{3}\t{6}\n",
                Simulator.Catalog.GetString("Power:"),
                Simulator.Catalog.GetString("MaxInd"),
                Simulator.Catalog.GetString("Ind"),
                Simulator.Catalog.GetString("Drawbar"),
                FormatStrings.FormatPower(W.FromHp(MaxIndicatedHorsePowerHP), IsMetric, false, false),
                FormatStrings.FormatPower(W.FromHp(IndicatedHorsePowerHP), IsMetric, false, false),
                FormatStrings.FormatPower(W.FromHp(DrawbarHorsePowerHP), IsMetric, false, false));

            status.AppendFormat("{0}\t{1}\t{7}\t{2}\t{8}\t{3}\t{9}\t{4}\t{10}\t{5}\t{11}\t{6} {12}\n",
                     Simulator.Catalog.GetString("Force:"),
                     Simulator.Catalog.GetString("TheorTE"),
                     Simulator.Catalog.GetString("StartTE"),
                     Simulator.Catalog.GetString("TE"),
                     Simulator.Catalog.GetString("Draw"),
                     Simulator.Catalog.GetString("CritSpTE"),
                     Simulator.Catalog.GetString("CritSpeed"),
                     FormatStrings.FormatForce(N.FromLbf(MaxTractiveEffortLbf), IsMetric),
                     FormatStrings.FormatForce(StartTractiveEffortN, IsMetric),
                     FormatStrings.FormatForce(N.FromLbf(DisplayTractiveEffortLbsF), IsMetric),
                     FormatStrings.FormatForce(N.FromLbf(DrawBarPullLbsF), IsMetric),
                     FormatStrings.FormatForce(N.FromLbf(DisplayCriticalSpeedTractiveEffortLbf), IsMetric),
                     FormatStrings.FormatSpeedDisplay(MpS.FromMpH(MaxLocoSpeedMpH), IsMetric));

            status.AppendFormat("{0}\t{1}\t{5:N0} {9}/{10}\t\t{2}\t{6:N3}\t{3}\t{7:N0} {11}\t{4} {8:N2}\n",
                Simulator.Catalog.GetString("Move:"),
                Simulator.Catalog.GetString("Piston"),
                Simulator.Catalog.GetString("SpdFact"),
                Simulator.Catalog.GetString("DrvWhl"),
                Simulator.Catalog.GetString("MF-Gear"),
                IsMetric ? Me.FromFt(PistonSpeedFtpMin) : PistonSpeedFtpMin,
                DisplaySpeedFactor,
                pS.TopM(DrvWheelRevRpS),
                MotiveForceGearRatio,
                IsMetric ? FormatStrings.m : FormatStrings.ft,
                FormatStrings.min,
                FormatStrings.rpm);

            status.AppendFormat("\n{0}\t{1}\t{2}\t{3}\t{4:N2}\t{5}\t{6:N2}\n",
                Simulator.Catalog.GetString("Sand:"),
                Simulator.Catalog.GetString("S/Use"),
                TrackSanderSandConsumptionFt3pH,
                Simulator.Catalog.GetString("S/Box"),
                TrackSandBoxCapacityFt3,
                Simulator.Catalog.GetString("M/Press"),
                MainResPressurePSI
                );

            if (Simulator.UseAdvancedAdhesion && SteamEngineType != SteamEngineTypes.Geared) // Only display slip monitor if advanced adhesion used
            {
                status.AppendFormat("\n\t\t === {0} === \n", Simulator.Catalog.GetString("Slip Monitor"));
                status.AppendFormat("{0}\t{1}\t{2:N0}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:N2}\t{13}\t{14}\t{15:N2}\t{16}\t{17}\t{18:N1}\n",
                    Simulator.Catalog.GetString("Slip:"),
                    Simulator.Catalog.GetString("MForceN"),
                    FormatStrings.FormatForce(MotiveForceN, IsMetric),
                    Simulator.Catalog.GetString("Piston"),
                    FormatStrings.FormatForce(N.FromLbf(StartPistonForceLbf), IsMetric),
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

#if DEBUG_STEAM_EJECTOR

            status.AppendFormat("\n\t\t === {0} === \t\t{1}/{2}\n",
            Simulator.Catalog.GetString("Steam Ejector"),
            pS.TopH(EjectorTotalSteamConsumptionLbpS),
            FormatStrings.h);

            status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\n",
            Simulator.Catalog.GetString("Small:"),
            Simulator.Catalog.GetString("Size"),
            SteamEjectorSmallDiameterIn,
            Simulator.Catalog.GetString("Press"),
            SteamEjectorSmallPressurePSI,
            Simulator.Catalog.GetString("Cons"),
            pS.TopH(EjectorSmallSteamConsumptionLbpS));

            if (!VacuumPumpFitted) // only display large ejector if vacuum pump fitted.
            {
                status.AppendFormat("{0}\t{1}\t{2:N2}\t{3}\t{4:N2}\t{5}\t{6:N2}\t{7}\t{8}\n",
                Simulator.Catalog.GetString("Large:"),
                Simulator.Catalog.GetString("Size"),
                SteamEjectorLargeDiameterIn,
                Simulator.Catalog.GetString("Press"),
                SteamEjectorLargePressurePSI,
                Simulator.Catalog.GetString("Cons"),
                pS.TopH(EjectorLargeSteamConsumptionLbpS),
                Simulator.Catalog.GetString("Lg Ej"),
                LargeSteamEjectorIsOn);
            }

#endif

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
                            // Re -initialise the following for the new gear setting - set to zero as in neutral speed
                            MotiveForceGearRatio = 0.0f;
                            MaxLocoSpeedMpH = 0.0f;
                            SteamGearRatio = 0.0f;
                            MaxTractiveEffortLbf = 0.0f;
                            MaxIndicatedHorsePowerHP = 0.0f;

                        }
                        else if (SteamGearPosition == 1.0)
                        {
                            // Re -initialise the following for the new gear setting
                            MotiveForceGearRatio = SteamGearRatioLow;
                            MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                            SteamGearRatio = SteamGearRatioLow;

                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio;

                            // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                            MaxIndicatedHorsePowerHP = MaxSpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;
                        }
                        else if (SteamGearPosition == 2.0)
                        {
                            // Re -initialise the following for the new gear setting
                            MotiveForceGearRatio = SteamGearRatioHigh;
                            MaxLocoSpeedMpH = MpS.ToMpH(HighMaxGearedSpeedMpS);
                            SteamGearRatio = SteamGearRatioHigh;

                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio;

                            // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                            MaxIndicatedHorsePowerHP = MaxSpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;
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
                            MotiveForceGearRatio = SteamGearRatioLow;
                            MaxLocoSpeedMpH = MpS.ToMpH(LowMaxGearedSpeedMpS);
                            SteamGearRatio = SteamGearRatioLow;
                            MaxTractiveEffortLbf = (NumCylinders / 2.0f) * (Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM) / (2 * Me.ToIn(DriverWheelRadiusM))) * MaxBoilerPressurePSI * TractiveEffortFactor * MotiveForceGearRatio;

                            // Max IHP = (Max TE x Speed) / 375.0, use a factor of 0.85 to calculate max TE
                            MaxIndicatedHorsePowerHP = MaxSpeedFactor * (MaxTractiveEffortLbf * MaxLocoSpeedMpH) / 375.0f;

                        }
                        else if (SteamGearPosition == 0.0)
                        {
                            // Re -initialise the following for the new gear setting - set to zero as in neutral speed
                            MotiveForceGearRatio = 0.0f;
                            MaxLocoSpeedMpH = 0.0f;
                            SteamGearRatio = 0.0f;
                            MaxTractiveEffortLbf = 0.0f;
                            MaxIndicatedHorsePowerHP = 0.0f;
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

        //Steam Heat Controller

        #region Steam heating controller

        public void StartSteamHeatIncrease(float? target)
        {
            SteamHeatController.CommandStartTime = Simulator.ClockTime;
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamHeat, CabSetting.Increase, SteamHeatController.CurrentValue * 100);
            SteamHeatController.StartIncrease(target);
            SignalEvent(Event.SteamHeatChange);
        }

        public void StopSteamHeatIncrease()
        {
            SteamHeatController.StopIncrease();
            new ContinuousSteamHeatCommand(Simulator.Log, 1, true, SteamHeatController.CurrentValue, SteamHeatController.CommandStartTime);
        }

        public void StartSteamHeatDecrease(float? target)
        {
            if (IsPlayerTrain)
                Simulator.Confirmer.ConfirmWithPerCent(CabControl.SteamHeat, CabSetting.Decrease, SteamHeatController.CurrentValue * 100);
            SteamHeatController.StartDecrease(target);
            SignalEvent(Event.SteamHeatChange);
        }

        public void StopSteamHeatDecrease()
        {
            SteamHeatController.StopDecrease();
            if (IsPlayerTrain)
                new ContinuousSteamHeatCommand(Simulator.Log, 1, false, SteamHeatController.CurrentValue, SteamHeatController.CommandStartTime);
        }

        public void SteamHeatChangeTo(bool increase, float? target)
        {
            if (increase)
            {
                if (target > SteamHeatController.CurrentValue)
                {
                    StartSteamHeatIncrease(target);
                }
            }
            else
            {
                if (target < SteamHeatController.CurrentValue)
                {
                    StartSteamHeatDecrease(target);
                }
            }
        }

        public void SetSteamHeatValue(float value)
        {
            var controller = SteamHeatController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                new ContinuousSteamHeatCommand(Simulator.Log, 1, change > 0, controller.CurrentValue, Simulator.GameTime);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeat, oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease, controller.CurrentValue * 100);
        }

        #endregion

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
                if (!CylinderCompoundOn)
                {
                    // Calculate maximum tractive effort if set for compounding
                    MaxTractiveEffortLbf = CylinderEfficiencyRate * (1.6f * MaxBoilerPressurePSI * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderDiameterM) * Me.ToIn(LPCylinderStrokeM)) / ((CompoundCylinderRatio + 1.0f) * (Me.ToIn(DriverWheelRadiusM * 2.0f)));
                }
                else
                {
                    // Calculate maximum tractive effort if set to simple operation
                    MaxTractiveEffortLbf = CylinderEfficiencyRate * (1.6f * MaxBoilerPressurePSI * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderDiameterM) * Me.ToIn(CylinderStrokeM)) / (Me.ToIn(DriverWheelRadiusM * 2.0f));
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
            if (type == (uint)PickupType.FuelCoal) return FuelController;
            if (type == (uint)PickupType.FuelWater) return WaterController;
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

        public void GetLocoInfo(ref float CC, ref float BC, ref float DC, ref float FC, ref float I1, ref float I2, ref float SH, ref float SE)
        {
            CC = CutoffController.CurrentValue;
            BC = BlowerController.CurrentValue;
            DC = DamperController.CurrentValue;
            FC = FiringRateController.CurrentValue;
            I1 = Injector1Controller.CurrentValue;
            I2 = Injector2Controller.CurrentValue;
            SH = SteamHeatController.CurrentValue;
            SE = SmallEjectorController.CurrentValue;
        }

        public void SetLocoInfo(float CC, float BC, float DC, float FC, float I1, float I2, float SH, float SE)
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
            SteamHeatController.CurrentValue = SH;
            SteamHeatController.UpdateValue = 0.0f;
            SmallEjectorController.CurrentValue = SE;
            SmallEjectorController.UpdateValue = 0.0f;
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
            if (Train.MUDirection == Direction.Forward)
            {
                Train.MUReverserPercent = 100;
            }
            else if (Train.MUDirection == Direction.Reverse)
            {
                Train.MUReverserPercent = -100;
            }
            base.SwitchToAutopilotControl();
        }

    } // class SteamLocomotive
}
