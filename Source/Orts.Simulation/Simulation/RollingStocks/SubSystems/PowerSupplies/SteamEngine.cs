// COPYRIGHT 2013, 2014 by the Open Rails project.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using ORTS.Scripting.Api;
using static Orts.Simulation.RollingStocks.TrainCar;

namespace Orts.Simulation.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class SteamEngines : ISubSystem<SteamEngines>
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<SteamEngine> SEList = new List<SteamEngine>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return SEList.Count; } }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        protected readonly MSTSSteamLocomotive Locomotive;

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public SteamEngines(MSTSSteamLocomotive loco)
        {
            Locomotive = loco;
        }

        public SteamEngine this[int i]
        {
            get { return SEList[i]; }
            set { SEList[i] = value; }
        }

        public void Add()
        {
            SEList.Add(new SteamEngine(Locomotive));
        }

        public void Add(SteamEngine se)
        {
            SEList.Add(se);
        }


        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">reference to the ENG file reader</param>
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortssteamengines":
                    stf.MustMatch("(");
                    int count = stf.ReadInt(0);
                    for (int i = 0; i < count; i++)
                    {
                        string setting = stf.ReadString().ToLower();
                        if (setting == "steam")
                        {
                            SEList.Add(new SteamEngine(Locomotive));

                            SEList[i].Parse(stf);
                            SEList[i].Initialize();

                            // sets flag to indicate that a steam eng prime mover code block has been defined by user, otherwise OR will define one through the next code section using "MSTS" values
                            SEList[i].SteamEngineConfigured = true;
                        }

                        if ((!SEList[i].IsInitialized))
                        {
                            Trace.TraceInformation("Steam engine {0} model has some errors - loading MSTS format", i+1);
                            SEList[i].InitFromMSTS();
                            SEList[i].Initialize();
                        }
                    }
                    break;
            }
        }

        public void Copy(SteamEngines other)
        {
            SEList = new List<SteamEngine>();
            foreach (SteamEngine se in other.SEList)
            {
                SteamEngine steamEngine = new SteamEngine(Locomotive);
                steamEngine.Copy(se);

                SEList.Add(steamEngine);
            }
        }

        public void Initialize()
        {
            foreach (SteamEngine se in SEList)
            {
                se.Initialize();
            }
        }

        public void InitializeMoving()
        {
            foreach (SteamEngine se in SEList)
            {
                se.InitializeMoving();
            }
        }

        /// <summary>
        /// Saves status of each auxiliary on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(SEList.Count);
            foreach (SteamEngine se in SEList)
            {
                se.Save(outf);
            }
        }

        /// <summary>
        /// Restores status of each auxiliary on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            if (SEList.Count == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    SEList.Add(new SteamEngine(Locomotive));
                    SEList[i].InitFromMSTS();
                    SEList[i].Initialize();
                }

            }
            foreach (SteamEngine se in SEList)
                se.Restore(inf);
        }



        /// <summary>
        /// Updates each auxiliary on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (SteamEngine de in SEList)
            {
                de.Update(elapsedClockSeconds);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            foreach (SteamEngine de in SEList)
            {
                de.HandleEvent(evt);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            if (id >= 0 && id < SEList.Count)
            {
                SEList[id].HandleEvent(evt);
            }
        }

        public List<SteamEngine>.Enumerator GetEnumerator()
        {
            return SEList.GetEnumerator();
        }

        public static string SetDebugLabels()
        {
            var labels = new StringBuilder();
            var tabs = "\t";
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("Status"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("Load"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("RPM"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("Flow"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("Temperature"), tabs);
            labels.AppendFormat("{0}{1}", Simulator.Catalog.GetString("Oil Pressure"), tabs);
            return labels.ToString();
        }

        public string GetStatus()
        {
            var result = new StringBuilder();

            result.AppendFormat(Simulator.Catalog.GetString("Status"));

            return result.ToString();
        }

        public string GetDPStatus()
        {
            var result = new StringBuilder();
            var eng = SEList[0];

            return result.ToString();
        }

    }

    public class SteamEngine : ISubSystem<SteamEngine>
    {

        /// <summary>
        /// Number of Steam Cylinders
        /// </summary>
        public int NumberCylinders;

        /// <summary>
        /// Diameter of Steam Cylinders
        /// </summary>
        public float CylindersDiameterM;

        /// <summary>
        /// Steam Cylinders stroke
        /// </summary>
        public float CylindersStrokeM;

        /// <summary>
        /// Number of LP Steam Cylinders
        /// </summary>
        public int LPNumberCylinders;

        /// <summary>
        /// Diameter of LP Steam Cylinders
        /// </summary>
        public float LPCylindersDiameterM;

        /// <summary>
        /// LP Steam Cylinders stroke
        /// </summary>
        public float LPCylindersStrokeM;

        /// <summary>
        /// Starting Tractive Force for the engine
        /// </summary>
        public float absStartTractiveForceN;


        public enum AuxiliarySteamEngineTypes
        {
            Unknown,
            Booster,
            Adhesion,
            Rack,
        }

        public AuxiliarySteamEngineTypes AuxiliarySteamEngineType;

        /// <summary>
        /// Booster Cutoff - cutoff for booster cylinder
        /// </summary>
        public float BoosterCutoff;

        /// <summary>
        /// Booster Throttle cutoff position based upon the postion of the main engine reverser
        /// </summary>
        public float BoosterThrottleCutoff;

        /// <summary>
        /// Booster Gear Ratio
        /// </summary>
        public float BoosterGearRatio;

        /// <summary>
        /// Steam Engine real tractive force - this value may go above the average "calculated" TF value as the wheel rotates.
        /// </summary>
        public float RealTractiveForceN;

        /// <summary>
        /// Steam Engine average tractive force
        /// </summary>
        public float AverageTractiveForceN;

        /// <summary>
        /// Steam Engine display tractive force
        /// </summary>
        public float DisplayTractiveForceN;

        /// <summary>
        /// Steam Engine counter pressure barking force
        /// </summary>
        public float CylinderCounterPressureBrakeForceN;

        /// <summary>
        /// Steam Engine counter pressure barking force
        /// </summary>
        public bool CounterPressureBrakingFitted;

        /// <summary>
        /// Steam Engine counter pressure MEP
        /// </summary>
        public float CounterPressureMEP;

        /// <summary>
        /// Steam Engine maximum indicated horsepower
        /// </summary>
        public float MaxIndicatedHorsePowerHP;

        /// <summary>
        /// Steam Engine unbalanced mass on wheels - per side. Typically called Excess or overbalance of rods 
        /// </summary>
        public float ExcessRodBalanceLbs;

        /// <summary>
        /// Steam Engine unbalanced wheel warning. 
        /// </summary>
        public bool IsWheelHammerForceWarning;

        /// <summary>
        /// Steam Engine unbalanced large overload. 
        /// </summary>
        public bool IsWheelHammerForce;

        /// <summary>
        /// Steam Engine hammer force per wheel - excessive values of this could cause track deformities. 
        /// </summary>
        public float HammerForceLbs;

        /// <summary>
        /// Steam Engine drive wheel rev per second
        /// </summary>
        public float DriveWheelRevRpS;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogSteamChestPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogInitialPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogLPInitialPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogCutoffPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogLPCutoffPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogReleasePressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogLPReleasePressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogBackPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogLPBackPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogPreAdmissionPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LogPreCompressionPressurePSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float HPCylinderMEPPSI;

        /// <summary>
        /// HuD
        /// </summary>
        public float LPCylinderMEPPSI;

        /// <summary>
        /// Initial Pressure to HP cylinder @ start if stroke
        /// </summary>
        public float HPCompPressure_a_AtmPSI;

        /// <summary>
        /// Pressure at HP cylinder cutoff
        /// </summary>
        public float HPCompPressure_b_AtmPSI;

        /// <summary>
        /// Pressure in HP cylinder when steam completely released from the cylinder
        /// </summary>
        public float HPCompPressure_f_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_e_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_d_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_f_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_g_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_h_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_l_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_m_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_n_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float LPCompPressure_q_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_a_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_b_AtmPSI;

        /// <summary>
        /// Cylinder Pressures
        /// </summary>
        public float Pressure_c_AtmPSI;

        /// <summary>
        /// Initial Pressure to LP cylinder @ start if stroke
        /// </summary>
        public float LPPressure_a_AtmPSI;

        /// <summary>
        /// Pressure in combined HP & LP Cylinder pre-cutoff
        /// </summary>
        public float LPPressure_b_AtmPSI;

        /// <summary>
        /// Pressure in LP cylinder when steam release valve opens
        /// </summary>
        public float LPPressure_c_AtmPSI;

        /// <summary>
        /// Back pressure on LP cylinder
        /// </summary>
        public float LPPressure_d_AtmPSI;

        /// <summary>
        /// Pressure in LP cylinder when exhaust valve closes, and compression commences
        /// </summary>
        public float LPPressure_e_AtmPSI;

        /// <summary>
        /// Pressure in LP cylinder after compression occurs and steam admission starts
        /// </summary>
        public float LPPressure_f_AtmPSI;

        /// <summary>
        /// Mean Effective Pressure
        /// </summary>
        public float MeanEffectivePressurePSI;

        /// <summary>
        /// Steam usage per steam engine
        /// </summary>
        public float CylinderSteamUsageLBpS;

        /// <summary>
        /// Steam usage per steam engine per hour
        /// </summary>
        public float CylinderSteamUsageLBpH;

        /// <summary>
        /// Steam usage per steam engine steam cocks
        /// </summary>
        public float CylCockSteamUsageLBpS;

        /// <summary>
        /// Back pressure in cylinder
        /// </summary>
        public float CylinderBackPressurePSIG;

        /// <summary>
        /// Cylinder steam cocks atmospheric pressure usage per steam engine steam cocks
        /// </summary>
        public float CylinderCocksPressureAtmPSI;

        /// <summary>
        /// Indicated HP Horse Power for Compound
        /// </summary>
        public float HPIndicatedHorsePowerHP;

        /// <summary>
        /// Indicated LP Horse Power for Compound
        /// </summary>
        public float LPIndicatedHorsePowerHP;

        /// <summary>
        /// Indicated Horse Power for single expansion
        /// </summary>
        public float IndicatedHorsePowerHP;

        /// <summary>
        /// Speed of Piston
        /// </summary>
        public float PistonSpeedFtpMin;

        /// <summary>
        /// Calculated Factor of Adhesion
        /// </summary>
        public float CalculatedFactorOfAdhesion;

        /// <summary>
        /// Maximum Tractice Effort
        /// </summary>
        public float MaxTractiveEffortLbf;

        /// <summary>
        /// Static wheel force for engine
        /// </summary>
        public float SteamStaticWheelForce;

        // Values for Steam Cylinder events
        public enum SESteamLocomotiveValveGearTypes
        {
            Unknown,
            Walschaert_Inside,
            Walschaert_Outside,
            Stephenson_Inside,
            Stephenson_Outside,
        }

        public SESteamLocomotiveValveGearTypes SESteamLocomotiveValveGearType;


        public double SEValveEccentricRadiusM;   // Eccentric Radius (r) - typically half of total valve travel, say 2.25 inches for a 4.5 inch cylinder stroke
        public double SEValvePortWidthM;      // Valve travel (2r) - Eccentric Radius (r) (Typically half of total valve travel) = 2.25in
        public double SEValveMaximumLeadM;        // Lead (Pb) = 0.25in
        public double SEValveExhaustLapM;        // Exhaust Lap (e) (Usually 0 or very small value)
        public double SESteamLapM;        // Maximum Steam Lap at full cutoff (when piston at end of stroke)

        public float SECrankRadiusM;        // Crank radius (R) - Assume crank and rod lengths to give a 1:10 ratio - a reasonable av for steam locomotives?
        public float SEConnectRodLengthM; // Connecting Rod Length (L)
        public double SEEccentricRodLinkPinDistM;
        public double SEEccentricRodLengthM;

        public double SEValveLeadM;
        public float SEwireDrawingLocomotiveConstant;

        public double CutoffCrankAngleRad; // Crank angle at cutoff - used to determine cylinder events and steam usage
        public double AngleofAdvanceRad;
        public double ReleaseCylinderFraction;
        public double CompressionCylinderFraction;
        public double AdmissionCylinderFraction;
        public double ActualCutoffCylinderFraction;

        public double AdPortOpenM; // Distance the admission port is open in metres
        public double ExPortOpenM;
        public double FullValveTravelM;
        public float MEPWireDrawingFactor; // Factor to reduce MEP due to wire drawing - drop in pressure as steam flows through valve ports, etc. - typically around 0.85 for a saturated locomotive and 0.9 for a superheated locomotive

        public double SESteamChestVolumeM3;
        public double SERegulatorMaxAreaM2;

        public float SEKEffFactor;
        public float SESteamChestPressurePSI;

        public float HallIHP;
        public float HallMEP;

        public float SESteamCylinderConsumptionKgpS;


        public float NewCylinderCondensationFactor;
        public float SteamChestPressureReductionPSI;

        float SERodCoGM; // Centre of Gravity of the rods - used to calculate unbalanced forces

        // Assume cylinder clearance of 8% of the piston displacement for saturated locomotives and 9% for superheated locomotive -
        // default to saturated locomotive value
        float SECylinderClearancePC;

        float CylinderWork_ab_InLbs; // Work done during admission stage of cylinder
        float CylinderWork_bc_InLbs; // Work done during expansion stage of cylinder
        float CylinderWork_cd_InLbs;   // Work done during release stage of cylinder
        float CylinderWork_ef_InLbs; // Work done during compression stage of cylinder
        float CylinderWork_fa_InLbs; // Work done during PreAdmission stage of cylinder
        float CylinderWork_de_InLbs; // Work done during Exhaust stage of cylinder

        // Values for logging and displaying Steam pressure
        public float SELogInitialPressurePSI;
        public float SELogCutoffPressurePSI;
        public float SELogBackPressurePSI;
        public float SELogReleasePressurePSI;
        public float SELogSteamChestPressurePSI;
        public float SELogPreCompressionPressurePSI;
        public float SELogPreAdmissionPressurePSI;
        public float SEMeanEffectivePressurePSI;

        public enum SettingsFlags
        {
            NumberCylindersF = 0x0001,
            CylindersDiameterF = 0x0002,
            CylinderStrokeF = 0x0003,
            LPNumberCylindersF = 0x0004,
            LPCylindersDiameterF = 0x0005,
            LPCylinderStrokeF = 0x0006,
            AttachedAxleIdF = 0x0007,
            BoosterCutoffF = 0x0009,
            BoosterThrottleCutoffF = 0x0009,
            BoosterGearRatioF = 0x0010,
        }

        public int Id
        {
            get
            {
                return Locomotive.SteamEngines.SEList.IndexOf(this) + 1;
            }
        }



        /// <summary>
        /// Parent locomotive
        /// </summary>
        public readonly MSTSSteamLocomotive Locomotive;

        protected Simulator Simulator => Locomotive.Simulator;

        protected int AttachedAxleId;
        public Axle AttachedAxle => Locomotive.LocomotiveAxles[AttachedAxleId];

        SettingsFlags initLevel;          //level of initialization
        /// <summary>
        /// Initialization flag - is true when sufficient number of parameters is read succesfully
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                var initCheck = false;

                // Code duplicated from below - maybe able to get rid of it eventually
                if (AuxiliarySteamEngineType == AuxiliarySteamEngineTypes.Unknown)
                {
                    // Set to Adhesion as default if not defined by user, as this is the most common type of steam engine.
                    AuxiliarySteamEngineType = AuxiliarySteamEngineTypes.Adhesion;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Auxiliary Steam Engine Type: not found in Steam Engine Configuration: set to default value = {0}", AuxiliarySteamEngineType);

                }


                if (AuxiliarySteamEngineType == AuxiliarySteamEngineTypes.Booster)
                {
                    // Checks to see if a Booster engine has been correctly configured
                    if (initLevel == (SettingsFlags.NumberCylindersF | SettingsFlags.CylindersDiameterF | SettingsFlags.CylinderStrokeF | SettingsFlags.AttachedAxleIdF | SettingsFlags.BoosterCutoffF | SettingsFlags.BoosterGearRatioF | SettingsFlags.BoosterThrottleCutoffF))
                    {
                        initCheck = true;
                    }
                }
                else if (AuxiliarySteamEngineType != AuxiliarySteamEngineTypes.Booster)
                {
                    // Checks to see if a non-booster engine has been correctly defined
                    if (initLevel == (SettingsFlags.NumberCylindersF | SettingsFlags.CylindersDiameterF | SettingsFlags.CylinderStrokeF | SettingsFlags.AttachedAxleIdF))
                    {
                        initCheck = true;
                    }
                }
                else
                    initCheck = false;

                return initCheck;
            }
        }


        public SteamEngine(MSTSSteamLocomotive locomotive)
        {
            Locomotive = locomotive;
        }

        public bool SteamEngineConfigured = false; // flag to indicate that the user has configured a steam engine prime mover code block in the ENG file


        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        /// <param name="loco">Reference to the locomotive</param>
        public virtual void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
            {
                switch (stf.ReadItem().ToLower())
                {
                    case "numcylinders": NumberCylinders = stf.ReadIntBlock(null); initLevel |= SettingsFlags.NumberCylindersF; break;
                    case "cylinderstroke": CylindersStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); initLevel |= SettingsFlags.CylinderStrokeF; break;
                    case "cylinderdiameter": CylindersDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); initLevel |= SettingsFlags.CylindersDiameterF; break;
                    case "cylinderclearance": SECylinderClearancePC = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;


                    case "lpnumcylinders": LPNumberCylinders = stf.ReadIntBlock(null); initLevel |= SettingsFlags.LPNumberCylindersF; break;
                    case "lpcylinderstroke": LPCylindersStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); initLevel |= SettingsFlags.LPCylinderStrokeF; break;
                    case "lpcylinderdiameter": LPCylindersDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); initLevel |= SettingsFlags.LPCylindersDiameterF; break;
                    case "boostercutoff": BoosterCutoff = stf.ReadFloatBlock(STFReader.UNITS.None, null); initLevel |= SettingsFlags.BoosterCutoffF; break;
                    case "boosterthrottlecutoff": BoosterThrottleCutoff = stf.ReadFloatBlock(STFReader.UNITS.None, null); initLevel |= SettingsFlags.BoosterThrottleCutoffF; break;
                    case "boostergearratio": BoosterGearRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); initLevel |= SettingsFlags.BoosterGearRatioF; break;
                    case "attachedaxle": AttachedAxleId = stf.ReadIntBlock(null); initLevel |= SettingsFlags.AttachedAxleIdF; break;
                    case "counterpressurebraking":
                        CounterPressureBrakingFitted = stf.ReadBoolBlock(false);
                        break;
                    case "maxindicatedhorsepower":
                        MaxIndicatedHorsePowerHP = stf.ReadFloatBlock(STFReader.UNITS.Power, null);
                        MaxIndicatedHorsePowerHP = W.ToHp(MaxIndicatedHorsePowerHP);  // Convert input to HP for use internally in this module
                        break;
                    case "excessrodbalance":
                        var excess = stf.ReadFloatBlock(STFReader.UNITS.Mass, null);
                        ExcessRodBalanceLbs = Kg.ToLb(excess);  // Convert input to lbs for use internally in this module
                        break;
                    case "auxiliarysteamenginetype":
                        var auxiliarysteamengineType = stf.ReadStringBlock("");
                        try
                        {
                            AuxiliarySteamEngineType = (AuxiliarySteamEngineTypes)Enum.Parse(typeof(AuxiliarySteamEngineTypes), auxiliarysteamengineType);
                        }
                        catch
                        {
                            STFException.TraceWarning(stf, "Assumed unknown engine type " + auxiliarysteamengineType);
                        }
                        break;

                    case "engine(ortssteamlocomotivevalvegeartype":
                        stf.MustMatch("(");
                        var steamLocomotiveValveGearType = stf.ReadString();
                        try
                        {
                            SESteamLocomotiveValveGearType = (SESteamLocomotiveValveGearTypes)Enum.Parse(typeof(SESteamLocomotiveValveGearTypes), steamLocomotiveValveGearType);
                        }
                        catch
                        {
                            if (Simulator.Settings.VerboseConfigurationMessages)
                                STFException.TraceWarning(stf, "Assumed unknown valve gear type " + steamLocomotiveValveGearType);
                        }
                        break;
                    case "cylinderportwidth": SEValvePortWidthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "cylinderlead": SEValveMaximumLeadM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "cylinderlap": SESteamLapM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "eccentricrodlength": SEEccentricRodLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "eccentricrodpindistance": SEEccentricRodLinkPinDistM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "cylinderexhaustlap": SEValveExhaustLapM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "wiredrawlocomotiveconstant": SEwireDrawingLocomotiveConstant = stf.ReadIntBlock(null); break;
                    case "steamchestefficiencyfactor": SEKEffFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "steamchestvolume": SESteamChestVolumeM3 = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                    case "regulatormaximumarea": SERegulatorMaxAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                    case "connectingrodlength": SEConnectRodLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                    case "crankradius": SECrankRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;

                    case "(":
                        stf.SkipRestOfBlock();
                        break;

                }
            }
        }

        public void Copy(SteamEngine other)
        {
            NumberCylinders = other.NumberCylinders;
            CylindersStrokeM = other.CylindersStrokeM;
            CylindersDiameterM = other.CylindersDiameterM;
            LPNumberCylinders = other.LPNumberCylinders;
            LPCylindersStrokeM = other.LPCylindersStrokeM;
            LPCylindersDiameterM = other.LPCylindersDiameterM;
            BoosterCutoff = other.BoosterCutoff;
            AuxiliarySteamEngineType = other.AuxiliarySteamEngineType;
            CounterPressureBrakingFitted = other.CounterPressureBrakingFitted;
            MaxIndicatedHorsePowerHP = other.MaxIndicatedHorsePowerHP;
            ExcessRodBalanceLbs = other.ExcessRodBalanceLbs;
            BoosterThrottleCutoff = other.BoosterThrottleCutoff;
            BoosterGearRatio = other.BoosterGearRatio;
            AttachedAxleId = other.AttachedAxleId;
            SEValvePortWidthM = other.SEValvePortWidthM;
            SESteamLocomotiveValveGearType = other.SESteamLocomotiveValveGearType;
            SEValveMaximumLeadM = other.SEValveMaximumLeadM;
            SEwireDrawingLocomotiveConstant = other.SEwireDrawingLocomotiveConstant;
            SEValveExhaustLapM = other.SEValveExhaustLapM;
            SEEccentricRodLengthM = other.SEEccentricRodLengthM;
            SEEccentricRodLinkPinDistM = other.SEEccentricRodLinkPinDistM;
            SESteamLapM = other.SESteamLapM;
            SEConnectRodLengthM = other.SEConnectRodLengthM;
            SECrankRadiusM = other.SECrankRadiusM;
            SEKEffFactor = other.SEKEffFactor;
            SESteamChestVolumeM3 = other.SESteamChestVolumeM3;
            SERegulatorMaxAreaM2 = other.SERegulatorMaxAreaM2;
            SECylinderClearancePC = other.SECylinderClearancePC;
        }

        public void Initialize()
        {
            // Code duplicated from above - maybe able to get rid of it eventually
            if (AuxiliarySteamEngineType == AuxiliarySteamEngineTypes.Unknown)
            {
                // Set to Adhesion as default if not defined by user, as this is the most common type of steam engine.
                AuxiliarySteamEngineType = AuxiliarySteamEngineTypes.Adhesion;

                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Auxiliary Steam Engine Type: not found in Steam Engine Configuration: set to default value = {0}", AuxiliarySteamEngineType);

            }

            // Assign default information for steam cylinder valve gear
            if (SESteamLocomotiveValveGearType == SESteamLocomotiveValveGearTypes.Unknown && Locomotive.SteamLocomotiveValveGearType != MSTSSteamLocomotive.SteamLocomotiveValveGearTypes.Unknown)
            {
                int steamLocomotiveValveGearType = (int)Locomotive.SteamLocomotiveValveGearType;
                SESteamLocomotiveValveGearType = (SESteamLocomotiveValveGearTypes)steamLocomotiveValveGearType;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Locomotive Valve Gear Type: copied from ENG file and set to value of {0}", SESteamLocomotiveValveGearType);
            }
            else
            {
                SESteamLocomotiveValveGearType = SESteamLocomotiveValveGearTypes.Walschaert_Inside; // default value

                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Locomotive Valve Gear Type: not found in Steam Engine Configuration: set to Default value of {0}", SESteamLocomotiveValveGearType);
            }

            if (SEwireDrawingLocomotiveConstant == 0 && Locomotive.wireDrawingLocomotiveConstant != 0 && Id == 1)
            {
                SEwireDrawingLocomotiveConstant = Locomotive.wireDrawingLocomotiveConstant;

                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Wire Drawing Locomotive Constant: copied from ENG file and set to value of {0}", SEwireDrawingLocomotiveConstant);
            }
            else if (SEwireDrawingLocomotiveConstant == 0)
            {
                SEwireDrawingLocomotiveConstant = 145; // default value

                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Wire Drawing Locomotive Constant: not found in Steam Engine Configuration: set to Default value of {0}", SEwireDrawingLocomotiveConstant);
            }

            if (SEKEffFactor == 0 && Locomotive.KEffFactor != 0 && Id == 1)
            {
                SEKEffFactor = Locomotive.KEffFactor;

                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Chest Locomotive Constant: copied from ENG file and set to value of {0}", SEKEffFactor);
            }
            else if (SEKEffFactor == 0)
            {
                SEKEffFactor = 1.3f * (float)Math.Pow(10, -9); // default value

                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Chest Locomotive Constant: not found in Steam Engine Configuration: set to Default value of {0}", SEKEffFactor);
            }

            if (SEValvePortWidthM == 0 && Locomotive.ValvePortWidthM != 0 && Id == 1)
            {
                SEValvePortWidthM = Locomotive.ValvePortWidthM;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Cylinder Port Width: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValvePortWidthM, Locomotive.IsMetric));
            }
            else if (SEValvePortWidthM == 0)
            {
                SEValvePortWidthM = 0.05715f; // default value - 2.25 inches
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Cylinder Port Width: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValvePortWidthM, Locomotive.IsMetric));
            }

            if (SEValveMaximumLeadM == 0 && Locomotive.ValveMaximumLeadM != 0 && Id == 1)
            {
                SEValveMaximumLeadM = Locomotive.ValveMaximumLeadM;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Maximum Lead: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValveMaximumLeadM, Locomotive.IsMetric));
            }
            else if (SEValveMaximumLeadM == 0)
            {
                SEValveMaximumLeadM = 0.003175f; // default value - 0.125 inches
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Maximum Lead: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValveMaximumLeadM, Locomotive.IsMetric));
            }

            if (SEValveExhaustLapM == 0 && Locomotive.ValveExhaustLapM != 0 && Id == 1)
            {
                SEValveExhaustLapM = Locomotive.ValveExhaustLapM;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Exhaust Lap: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValveExhaustLapM, Locomotive.IsMetric));
            }
            else if (SEValveExhaustLapM == 0)
            {
                SEValveExhaustLapM = 0.0005f; // default value - 0.02 inches
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Exhaust Lap: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValveExhaustLapM, Locomotive.IsMetric));
            }

            if (SESteamLapM == 0 && Locomotive.SteamLapM != 0 && Id == 1)
            {
                SESteamLapM = Locomotive.SteamLapM;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Steam Lap: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SESteamLapM, Locomotive.IsMetric));
            }
            else if (SESteamLapM == 0)
            {
                SESteamLapM = 0.001f; // default value - 0.04 inches
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Steam Lap: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEValveExhaustLapM, Locomotive.IsMetric));
            }

            if (SEConnectRodLengthM == 0 && Locomotive.ConnectRodLengthM != 0 && Id == 1)
            {
                SEConnectRodLengthM = Locomotive.ConnectRodLengthM; // 10.8 ft
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Connecting Rod Length: copied from ENG file and set to value of {0}", FormatStrings.FormatDistanceDisplay((float)SEConnectRodLengthM, Locomotive.IsMetric));

            }
            else if (SEConnectRodLengthM == 0)
            {
                SEConnectRodLengthM = 3.29184f; // default value - 10.8 ft
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Connecting Rod Length: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatDistanceDisplay((float)SEConnectRodLengthM, Locomotive.IsMetric));
            }

            if (SECrankRadiusM == 0 && Locomotive.CrankRadiusM != 0 && Id == 1)
            {
                SECrankRadiusM = Locomotive.CrankRadiusM;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Crank Radius: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SECrankRadiusM, Locomotive.IsMetric));
            }
            else if (SECrankRadiusM == 0)
            {
                SECrankRadiusM = Me.FromFt(1.08f); // default value
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Crank Radius: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SECrankRadiusM, Locomotive.IsMetric));
            }

            if (SEEccentricRodLinkPinDistM == 0 && Locomotive.EccentricRodLinkPinDistM != 0 && Id == 1)
            {
                SEEccentricRodLinkPinDistM = Locomotive.EccentricRodLinkPinDistM; // half of cylinder stroke
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Eccentric Rod Link Pin Distance: copied from ENG file and set to value of {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEEccentricRodLinkPinDistM, Locomotive.IsMetric));
            }
            else if (SEEccentricRodLinkPinDistM == 0)
            {
                SEEccentricRodLinkPinDistM = CylindersStrokeM / 2; // default value - half of cylinder stroke
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Eccentric Rod Link Pin Distance: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEEccentricRodLinkPinDistM, Locomotive.IsMetric));
            }

            if (SEEccentricRodLengthM == 0 && Locomotive.EccentricRodLengthM != 0 && Id == 1)
            {
                SEEccentricRodLengthM = Locomotive.EccentricRodLengthM; // half of cylinder stroke
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Eccentric Rod Length: copied from ENG file and set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEEccentricRodLengthM, Locomotive.IsMetric));
            }
            else if (SEEccentricRodLengthM == 0)
            {
                SEEccentricRodLengthM = Me.FromFt(4.35f); // default value - half of cylinder stroke
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Valve Eccentric Rod Length: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatMillimeterDistanceDisplay((float)SEEccentricRodLengthM, Locomotive.IsMetric));
            }

            if (SECylinderClearancePC == 0 && Locomotive.CylinderClearancePC != 0 && Id == 1)
            {
                SECylinderClearancePC = Locomotive.CylinderClearancePC;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Cylinder Clearance: copied from ENG file and set to default value = {0}", SECylinderClearancePC);
            }
            else if (SECylinderClearancePC == 0)
            {
                SEEccentricRodLengthM = 0.08f; // Assume cylinder clearance of 8% of the piston displacement for saturated locomotives
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Cylinder Clearance: not found in Steam Engine Configuration: set to default value = {0}", SECylinderClearancePC);
            }

            if (SESteamChestVolumeM3 == 0 && Locomotive.SteamChestVolumeM3 != 0 && Id == 1)
            {
                SESteamChestVolumeM3 = Locomotive.SteamChestVolumeM3;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Chest Volume: copied from ENG file and set to value of {0}", FormatStrings.FormatSmallVolume((float)SESteamChestVolumeM3, Locomotive.IsMetric));
            }
            else if (SESteamChestVolumeM3 == 0)
            {
                SESteamChestVolumeM3 = Me3.FromFt3(2.0f); // default value - 2 cubic feet
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Steam Chest Volume: not found in Steam Engine Configuration: set to default value = {0}", FormatStrings.FormatSmallVolume((float)SESteamChestVolumeM3, Locomotive.IsMetric));
            }

            if (SERegulatorMaxAreaM2 == 0 && Locomotive.RegulatorMaxAreaM2 != 0 && Id == 1)
            {
                SERegulatorMaxAreaM2 = Locomotive.RegulatorMaxAreaM2;
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Regulator Maximum Area: copied from ENG file and set to value of {0}", FormatStrings.FormatArea((float)SERegulatorMaxAreaM2, Locomotive.IsMetric));
            }
            else if (SERegulatorMaxAreaM2 == 0)
            {
                SERegulatorMaxAreaM2 = Me2.FromIn2(20.7f);  // Regulator opening on Britannia  is 20.7in2 (133.5cm2) at full throttle.
                if (Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Regulator Maximum Area: copied from ENG file and set to value of {0}", FormatStrings.FormatArea((float)SERegulatorMaxAreaM2, Locomotive.IsMetric));
            }

            //   Locomotive.RodCoGM = 0.4f * SEConnectRodLengthM;   // 0.4 from crank end of rod

            //   RodCoGM = 0.4f * SEConnectRodLengthM;   // 0.4 from crank end of rod
            SERodCoGM = Me.FromFt(4.32f);

            // Temp feed back into MSTSSteamLocomotive.cs until all code refactored
            Locomotive.ConnectRodLengthM = SEConnectRodLengthM;
            Locomotive.CrankRadiusM = SECrankRadiusM;
            Locomotive.RodCoGM = SERodCoGM;
            Locomotive.CylinderClearancePC = SECylinderClearancePC;

        }

        public void InitializeMoving()
        {

        }

        public void Update(float elapsedClockSeconds)
        {

            CalculateSteamEvents();
            if (Locomotive.SteamEngineType != SteamEngineTypes.Compound)
            {
                CalculateSingleExpansionCylinderSteamIndicatorDiagram();
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {

        }

        public void Restore(BinaryReader inf)
        {

        }

        public void Save(BinaryWriter outf)
        {

        }

        /// <summary>
        /// Fix or define a steam engine code block. If the user has not defined a steam engine in the ENG file, then OR will use this section to create one.
        /// If the user has left a parameter out of the code, then OR uses this section to try and set the missing values to a default value.
        /// Error code has been provided that will provide the user with an indication if a parameter has been left out.
        /// </summary>
        public void InitFromMSTS()
        {
            if ((initLevel & SettingsFlags.NumberCylindersF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSNumCylinders != 0) // Advanced conf - Steam Eng block defined but no NumCylinders present
                {
                    NumberCylinders = Locomotive.MSTSNumCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of Cylinders: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", NumberCylinders);

                }
                else if (NumberCylinders == 0 && Locomotive.MSTSNumCylinders != 0)  // Basic conf - No ENG block defined, use the default "MSTS" value
                {
                    NumberCylinders = Locomotive.MSTSNumCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of Cylinders: set at MSTS default value (BASIC Config) = {0}", NumberCylinders);

                }
                else if (Locomotive.MSTSNumCylinders == 0) // No default "MSTS" value present, set to arbitary value
                {
                    NumberCylinders = 2;
                    Locomotive.MSTSNumCylinders = NumberCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of Cylinders: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", NumberCylinders);

                }
            }

            if ((initLevel & SettingsFlags.CylindersDiameterF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSCylinderDiameterM != 0) // Advanced conf - Steam Eng block defined but no CylinderDiameter present
                {
                    CylindersDiameterM = Locomotive.MSTSCylinderDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Diameter: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", CylindersDiameterM);

                }
                else if (CylindersDiameterM == 0 && Locomotive.MSTSCylinderDiameterM != 0)  // Basic conf - No steam ENG block defined, use the default "MSTS" value
                {
                    CylindersDiameterM = Locomotive.MSTSCylinderDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Diameter: set at MSTS default value (BASIC Config) = {0}", CylindersDiameterM);

                }
                else if (Locomotive.MSTSCylinderDiameterM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    CylindersDiameterM = 1;
                    Locomotive.MSTSCylinderDiameterM = CylindersDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Diameter: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", CylindersDiameterM);

                }
            }

            if ((initLevel & SettingsFlags.CylinderStrokeF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSCylinderStrokeM != 0) // Advanced conf - Steam Eng block defined but no CylinderStroke present
                {
                    CylindersStrokeM = Locomotive.MSTSCylinderStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Stroke: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", CylindersStrokeM);

                }
                else if (CylindersStrokeM == 0 && Locomotive.MSTSCylinderStrokeM != 0)  // Basic conf - No steam ENG block defined, use the default "MSTS" value
                {
                    CylindersStrokeM = Locomotive.MSTSCylinderStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Stroke: set at MSTS default value (BASIC Config) = {0}", CylindersStrokeM);

                }
                else if (Locomotive.MSTSCylinderStrokeM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    CylindersStrokeM = 1;
                    Locomotive.MSTSCylinderStrokeM = CylindersStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Cylinder Stroke: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", CylindersStrokeM);

                }
            }

            if ((initLevel & SettingsFlags.LPNumberCylindersF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSLPNumCylinders != 0) // Advanced conf - Steam Eng block defined but no NumCylinders present
                {
                    LPNumberCylinders = Locomotive.MSTSLPNumCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of LP Cylinders: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", LPNumberCylinders);

                }
                else if (LPNumberCylinders == 0 && Locomotive.MSTSLPNumCylinders != 0)  // Basic conf - No ENG block defined, use the default "MSTS" value
                {
                    LPNumberCylinders = Locomotive.MSTSLPNumCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of LP Cylinders: set at MSTS default value (BASIC Config) = {0}", LPNumberCylinders);

                }
                else if (Locomotive.MSTSLPNumCylinders == 0) // No default "MSTS" value present, set to arbitary value
                {
                    LPNumberCylinders = 2;
                    Locomotive.MSTSLPNumCylinders = LPNumberCylinders;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Number of LP Cylinders: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", LPNumberCylinders);

                }

            }


            if ((initLevel & SettingsFlags.LPCylindersDiameterF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSLPCylinderDiameterM != 0) // Advanced conf - Steam Eng block defined but no CylinderDiameter present
                {
                    LPCylindersDiameterM = Locomotive.MSTSLPCylinderDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Diameter: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", LPCylindersDiameterM);

                }
                else if (LPCylindersDiameterM == 0 && Locomotive.MSTSLPCylinderDiameterM != 0)  // Basic conf - No steam ENG block defined, use the default "MSTS" value
                {
                    LPCylindersDiameterM = Locomotive.MSTSLPCylinderDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Diameter: set at MSTS default value (BASIC Config) = {0}", LPCylindersDiameterM);

                }
                else if (Locomotive.MSTSLPCylinderDiameterM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    LPCylindersDiameterM = 1;
                    Locomotive.MSTSLPCylinderDiameterM = LPCylindersDiameterM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Diameter: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", LPCylindersDiameterM);

                }
            }

            if ((initLevel & SettingsFlags.LPCylinderStrokeF) == 0)
            {
                if (SteamEngineConfigured && Locomotive.MSTSLPCylinderStrokeM != 0) // Advanced conf - Steam Eng block defined but no CylinderStroke present
                {
                    LPCylindersStrokeM = Locomotive.MSTSLPCylinderStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Stroke: not found in Steam Engine Configuration (ADVANCED Config): set to default value = {0}", LPCylindersStrokeM);

                }
                else if (LPCylindersStrokeM == 0 && Locomotive.MSTSLPCylinderStrokeM != 0)  // Basic conf - No steam ENG block defined, use the default "MSTS" value
                {
                    LPCylindersStrokeM = Locomotive.MSTSLPCylinderStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Stroke: set at MSTS default value (BASIC Config) = {0}", LPCylindersStrokeM);

                }
                else if (Locomotive.MSTSLPNumCylinders == 0) // No default "MSTS" value present, set to arbitary value
                {
                    LPCylindersStrokeM = 1;
                    Locomotive.MSTSCylinderStrokeM = LPCylindersStrokeM;

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("LP Cylinder Stroke: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", LPCylindersStrokeM);

                }
            }

            if ((initLevel & SettingsFlags.BoosterCutoffF) == 0)
            {
                BoosterCutoff = 0.3f;
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Booster Cutoff: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", BoosterCutoff);
            }

            if ((initLevel & SettingsFlags.BoosterThrottleCutoffF) == 0)
            {
                BoosterThrottleCutoff = 0.3f;
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Booster Throttle Cutoff: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", BoosterCutoff);
            }

            if ((initLevel & SettingsFlags.BoosterGearRatioF) == 0)
            {
                BoosterGearRatio = 1.0f;
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Booster Gear Ratio: not found in Steam Engine Configuration (BASIC Config): set at arbitary value = {0}", BoosterGearRatio);
            }

        }

        public void InitDieselRailPowers(MSTSSteamLocomotive loco)
        {

        }


        private void CalculateSteamEvents()
        {

            // Calculate valve events based upon cutoff value - this is used to determine the timing of the opening and closing of the valves,
            // which in turn impacts the pressure and volume in the cylinder at different points in the cycle, and thus the mean effective
            // pressure and power output of the cylinder.

            // Valve events calculated using Zeuner Diagram
            // References - Valve-gears, Analysis by the Zeuner diagram : Spangler, H. W. -  https://archive.org/details/valvegearsanalys00spanrich
            // Zeuner Diagram by Charles Dockstader used as a reference source (Note - the release value seems to be incorrect ) - http://www.billp.org/Dockstader/ValveGear.html


            // Inputs required for Walschaert valve gear.
            // Cylinder stroke (S)
            // Connecting Rod Length (L)
            // Exhaust Lap (e) (Usually 0 or very small value)
            // Lead (l)

            // Additional Inputs required for Stephenson valve gear
            // Distance between eccentric rod pins (w)
            // Expansion Rod Link (ER)

            //  double ValveLeadM = 0;
            double MaxCutoffCrankAngleRad = 0;
            double MaxCutoffAngleofAdvanceRad = 0;
            double HalfTravelCutoffM = 0;
            double FullTravelMaxCutoffM = 0;
            double HalfTravelMaxCutoffM = 0;
            double StephensonLeadChangeM = 0;

            switch (SESteamLocomotiveValveGearType)
            {
                case SESteamLocomotiveValveGearTypes.Walschaert_Outside:
                // yi = Lap - req * sin (crank angle - angle of advance)
                case SESteamLocomotiveValveGearTypes.Walschaert_Inside:
                    // This is the default valve gear type
                    // Timing for steam events for Walschaert inside and outside valves should remain the same, as the valve events are determined by the
                    // crank angle at which the valve opens and closes, and this is determined by the eccentric radius and lead, which are the same
                    // for both inside and outside Walschaert valve gear. The only difference is that with inside valve gear, the eccentric rod
                    // is attached to the inside of the driving wheel, whereas with outside valve gear it is attached to the outside of the driving
                    // wheel. This means that with inside valve gear, the eccentric rod will be shorter than with outside valve gear, but this does
                    // not impact the timing of the valve events.
                    // The only variation will be the port opening :- yi = req * sin (crank angle + angle of advance) - Lap ????

                    SEValveLeadM = SEValveMaximumLeadM; // Lead is constant for Walschaert valve gear, and is equal to the maximum lead

                    CutoffCrankAngleRad = CalculateCrankAngle(Locomotive.cutoff);

                    MaxCutoffCrankAngleRad = CalculateCrankAngle(Locomotive.CutoffController.MaximumValue);

                    AngleofAdvanceRad = CalculateAngleOfAdvance(CutoffCrankAngleRad, SESteamLapM, SEValveLeadM);

                    MaxCutoffAngleofAdvanceRad = CalculateAngleOfAdvance(MaxCutoffCrankAngleRad, SESteamLapM, SEValveLeadM);

                    HalfTravelCutoffM = CalculateValveHalfTravel(SESteamLapM, SEValveLeadM, AngleofAdvanceRad);

                    FullValveTravelM = 2 * HalfTravelCutoffM;

                    HalfTravelMaxCutoffM = CalculateValveHalfTravel(SESteamLapM, SEValveLeadM, MaxCutoffAngleofAdvanceRad);

                    FullTravelMaxCutoffM = 2 * HalfTravelMaxCutoffM;

                    break;

                case SESteamLocomotiveValveGearTypes.Stephenson_Outside:
                case SESteamLocomotiveValveGearTypes.Stephenson_Inside:
                    // Following developed for Inside admission Stepehenson valve gear, but should be the same for outside admission, as the timing of the valve events is determined by the eccentric radius and lead, which are the same for both inside and outside Stephenson valve gear.
                    // In Stephenson valve lead is recalculated for every value of cutoff, as the lead is not constant, but varies with cutoff.
                    // The lead is calculated using the following formula:
                    // Lead = Lead Change + Max Lead
                    // Lead Change = (w^2 - y^2) / 2 * ER, where w = half-distance between eccentric rod pins, y = offset of die block of the link (0 at mid gear, and increases as cutoff increases (w)), ER = Expansion Rod Link

                    // y = w x (cutoff / max cutoff)                             

                    double EccentricRodLinkPinHalfDistM = SEEccentricRodLinkPinDistM / 2.0f; // convert to metres

                    double DieOffsetY = (Locomotive.cutoff / Locomotive.CutoffController.MaximumValue) * (EccentricRodLinkPinHalfDistM);

                    StephensonLeadChangeM = ((Math.Pow(EccentricRodLinkPinHalfDistM, 2) - Math.Pow(DieOffsetY, 2)) / (2 * SEEccentricRodLengthM));

                    SEValveLeadM = SEValveMaximumLeadM + StephensonLeadChangeM;

                    //                    Trace.TraceInformation("Lead {0}  Change {1} Base {2} cutoff {3}", ValveLeadM, StephensonLeadChangeM, ValveMaximumLeadM, cutoff);

                    CutoffCrankAngleRad = CalculateCrankAngle(Locomotive.cutoff);

                    MaxCutoffCrankAngleRad = CalculateCrankAngle(Locomotive.CutoffController.MaximumValue);

                    AngleofAdvanceRad = CalculateAngleOfAdvance(CutoffCrankAngleRad, SESteamLapM, SEValveLeadM);

                    MaxCutoffAngleofAdvanceRad = CalculateAngleOfAdvance(MaxCutoffCrankAngleRad, SESteamLapM, SEValveLeadM);

                    HalfTravelCutoffM = CalculateValveHalfTravel(SESteamLapM, SEValveLeadM, AngleofAdvanceRad);

                    FullValveTravelM = 2 * HalfTravelCutoffM;

                    HalfTravelMaxCutoffM = CalculateValveHalfTravel(SESteamLapM, SEValveLeadM, MaxCutoffAngleofAdvanceRad);

                    FullTravelMaxCutoffM = 2 * HalfTravelMaxCutoffM;

                    break;
            }

            // Eccentric Radius (r) (Half of total valve travel)
            // Admission crank Angle = arcsin (Steam Lap / Half Travel) - AngleofAdvance
            double AdmissionCrankAngleRad = Math.Asin(SESteamLapM / HalfTravelCutoffM) - AngleofAdvanceRad;

            // Actual cutoff crank Angle = 180 - arcsin (Steam Lap / Half Travel) - AngleofAdvance
            double ActualCutoffCrankAngleRad = Math.PI - Math.Asin(SESteamLapM / HalfTravelCutoffM) - AngleofAdvanceRad;

            // Release crank Angle = 180 - arcsin (-Exhaust Lap / Half Travel) - AngleofAdvance
            double ReleaseCrankAngleRad = (Math.PI - Math.Asin((-1 * SEValveExhaustLapM)) / HalfTravelCutoffM) - AngleofAdvanceRad;

            // Compression crank Angle = 360 + arcsin (-Exhaust Lap / Half Travel) - AngleofAdvance
            double CompressionCrankAngleRad = (2 * Math.PI) + Math.Asin((-1 * SEValveExhaustLapM) / HalfTravelCutoffM) - AngleofAdvanceRad;

            // Convert crank angles to linear travel
            // S = R (1 - cos(crank angle) + L (1 - SQRT ( 1 - (R/L sin(crank angle))^2)
            // To convert to a fraction of total stroke
            // Fraction = S / (2 * R)

            if (Locomotive.cutoff == 0)
            {
                AdmissionCylinderFraction = 0.0f;
                ReleaseCylinderFraction = 0.0f;
                CompressionCylinderFraction = 0.0f;
                ActualCutoffCylinderFraction = 0.0f;

            }
            else
            {
                // Admission
                float TempAdA = (float)(HalfTravelCutoffM * (1 - Math.Cos(AdmissionCrankAngleRad)));
                float TempAdB = (float)(Math.Sqrt(1 - Math.Pow(HalfTravelCutoffM / SEConnectRodLengthM * Math.Sin(AdmissionCrankAngleRad), 2)));
                float TravelAdmissionM = TempAdA + SEConnectRodLengthM * (1 - TempAdB);
                AdmissionCylinderFraction = TravelAdmissionM / (2 * HalfTravelCutoffM);

                // Cutoff
                float TempCutA = (float)(HalfTravelCutoffM * (1 - Math.Cos(ActualCutoffCrankAngleRad)));
                float TempCutB = (float)(Math.Sqrt(1 - Math.Pow(HalfTravelCutoffM / SEConnectRodLengthM * Math.Sin(ActualCutoffCrankAngleRad), 2)));
                float TravelCutoffM = TempCutA + SEConnectRodLengthM * (1 - TempCutB);
                ActualCutoffCylinderFraction = TravelCutoffM / (2 * HalfTravelCutoffM);

                // Release
                float TempRelA = (float)(HalfTravelCutoffM * (1 - Math.Cos(ReleaseCrankAngleRad)));
                float TempRelB = (float)(Math.Sqrt(1 - Math.Pow(HalfTravelCutoffM / SEConnectRodLengthM * Math.Sin(ReleaseCrankAngleRad), 2)));
                float TravelReleaseM = TempRelA + SEConnectRodLengthM * (1 - TempRelB);
                ReleaseCylinderFraction = TravelReleaseM / (2 * HalfTravelCutoffM);

                // Compression
                float TempCompA = (float)(HalfTravelCutoffM * (1 - Math.Cos(CompressionCrankAngleRad)));
                float TempCompB = (float)(Math.Sqrt(1 - Math.Pow(HalfTravelCutoffM / SEConnectRodLengthM * Math.Sin(CompressionCrankAngleRad), 2)));
                float TravelCompressionM = TempCompA + SEConnectRodLengthM * (1 - TempCompB);
                CompressionCylinderFraction = TravelCompressionM / (2 * HalfTravelCutoffM);

                // Inlet Port Opening = req * sin (crank angle + angle of advance) - Lap
                // Maximum inlet opening occurs when sin(crank angle + angle of advance) = 1, so maximum inlet opening = req - Lap
                AdPortOpenM = HalfTravelCutoffM - SESteamLapM;

                // Exhaust Port Opening = - req * sin (crank angle + angle of advance) - Exhaust Lap
                // Maximum exhaust opening occurs when sin(crank angle + angle of advance) = -1, so maximum exhaust opening = req - Exhaust Lap
                ExPortOpenM = HalfTravelCutoffM - SEValveExhaustLapM;

                if (ExPortOpenM > SEValvePortWidthM)
                {
                    ExPortOpenM = SEValvePortWidthM; // Limit exhaust port opening to maximum valve port width
                }

            }

            // Three elements impact the theoretical steam indicator diagram as follows:
            // i) Boiler to Steam Chest Pressure Drop - caused by the flow of steam from the boiler to the steam chest, and the resistance of the steam
            // passages. Hence the pressure in the steam chest is lower than the boiler pressure.
            // ii) Wire-drawing -  Steam Chest to Cylinder Pressure Drop - caused by the flow of steam from the steam chest to the cylinder, and the
            // resistance of the valve and port. Hence the pressure in the cylinder is lower than the steam chest pressure.
            // iii) Cylinder Condensation - caused by the condensation of steam in the cylinder, which reduces the effective pressure of the steam
            // in the cylinder, and increases the amount of steam used by the cylinder, as more steam is required to fill the cylinder to the same
            // pressure due to the condensation. Hence the effective pressure in the cylinder is lower than the pressure of the steam entering the
            // cylinder.
            //
            //  Hence PressureCylinder = PressureChest - (Chest 

            // i) Boiler to Steam Chest Pressure Drop
            // To calculate the pressure drop from the boiler to the steam chest, we can use the following formula based upon a representation of
            // steam flow and a k factor for different types of locomotives.
            // Steam Chest Pressure = Boiler Pressure - (kwot * Steam Flow^2), where kwot is a fixed constant which represents the resistance of
            // the steam passages at wide open throttle.
            // kwot is a locomotive specific constant which represents the resistance of the steam passages, and is based upon the design of the
            // locomotive, and in particular the size of the steam passages.
            // Closing the regulator will also create wire drawing as well as reducing the pressure, so we can adjust the kwot value by a throttle factor to account for this.
            // kwot = kwot full throttle / throttle^2, where kwot full throttle is the value of kwot at wide open throttle, and throttle is the
            // current throttle setting as a fraction of wide open throttle.
            // Typical values are as follows:
            // 

            double SEKFullFactor = SEKEffFactor / Math.Pow(Locomotive.throttle, 2);

            if (CylinderSteamUsageLBpH > 0)
            {
                SteamChestPressureReductionPSI = (float)Math.Pow(CylinderSteamUsageLBpH, 2) * (float)(SEKFullFactor);
            }
            else
            {
                SteamChestPressureReductionPSI = 0;
            }

            // ii) Wire-drawing -  Steam Chest to Cylinder Pressure Drop
            // To calculate the pressure at the moment of cut-off, we must account for the pressure drop(wire-drawing) as steam flows through the
            // restricted port opening.This is a dynamic flow problem where the cylinder pressure lags behind the steam chest pressure due to the
            // resistance of the valve.
            // Using the Alco / Cole ratio for flow through the valve, we can calculate the pressure drop across the valve at cut-off, and thus the
            // pressure in the cylinder at cut-off, which is used to determine the mean effective pressure of the cylinder.
            // Cutoff Pressure = Steam Chest Pressure * exp( - locomotive constant * Cutoff Port opening (in) / piston speed (ft/s))
            // Reference document -
            // http://users.fini.net/~bersano/english-anglais/The%20development%20of%20Locomotive%20Power%20at%20speed%20404.full%20-O.pdf
            //
            // Typical locomotive constants are as follows:
            // Saturated Steam (Pre- 1910) eg Deans Goods, 4-4-0 - 380 - 320, High resistance, small ports and heavy wiredrawing
            // Early Superheated Steam (1910s) eg GWR Saint, PRR K4s - 210 - 240, Significant pressure drop at high speed; shorter valve travel
            // Late Pre-War (1920s-30s) eg LMS Coronation, LNER A4 - 160-180, Good breathing, but often limited by smaller internal steam passages
            // Modern Standards (1940s+) eg BR Standard, Britannia - 145, Balanced high speed performance, standard long travel gear
            // Ultra-Modern / Streamlined eg Chapelon 242A1, N&W J-Class - 110 - 125, Minimum wiredrawing; large ports and long travel valves.

            var PistonSpeedFtS = (CylindersStrokeM * 2.0f * DriveWheelRevRpS) * Me.ToFt(1.0f); // Piston speed in ft/s

            MEPWireDrawingFactor = 0;

            if (PistonSpeedFtS > 0)
            {

                MEPWireDrawingFactor = (float)Math.Exp(PistonSpeedFtS / (-SEwireDrawingLocomotiveConstant * Me.ToIn((float)AdPortOpenM)));
            }
            else
            {
                MEPWireDrawingFactor = 1.0f;
            }

            // iii) Cylinder Condensation - caused by the condensation of steam in the cylinder, which reduces the effective pressure of the steam
            // To allow for steam condensation in a steam cylinder the "Missing Factor" is used to adjust MEP and Steam Consumption.
            // The following is based upon Professor Bill Hall's paper "Cylinder Condensation in Unsuperheated Steam Engines"
            // Missing Quantity (M) = Pressure & Material Factor (Ms) x Speed Factor (R) x Size Factor (S) x Configuration Factor (C)
            // Pressure and Material Factor (Ms) = 2.45 * P^-0.46 + 0.08, where P is the boiler gauge pressure (Extrapolation of Fig 3)
            // Speed Factor (R) = SQRT (500 / Wheel RPM) - accounts for the time that steam is in contact with cold cylinder walls.
            // Size Factor (S) = 2 / Cylinder Diameter (inches)
            // Configuration Factor (C) = Cbase + Cratio
            // Cbase = 1.62 * cutoff^2 - 2.05*cutoff + 1.63
            // Cratio = ((Cylinder Diameter (inches) / Stroke (inches)) - 0.5) * (2.5 * exp (-2.2 * cutoff))

            float PressureMaterialFactor = (float)((2.45f * Math.Pow(Locomotive.BoilerPressurePSI, -0.46f)) + 0.08f);
            float SpeedFactor = (float)Math.Sqrt(500.0f / (DriveWheelRevRpS * 60.0f)); // Convert wheel revs per second to revs per minute for this calculation
            float SizeFactor = 2.0f / Me.ToIn(CylindersDiameterM);
            float ConfigurationBaseFactor = (1.62f * (float)Math.Pow(Locomotive.cutoff, 2)) - (2.05f * Locomotive.cutoff) + 1.63f;
            float ConfigurationRatioFactor = ((Me.ToIn(CylindersDiameterM) / Me.ToIn(CylindersStrokeM)) - 0.5f) * (2.5f * (float)Math.Exp(-2.2f * Locomotive.cutoff));

            if (PistonSpeedFtS > 0)
            {
                NewCylinderCondensationFactor = PressureMaterialFactor * SpeedFactor * SizeFactor * (ConfigurationBaseFactor + ConfigurationRatioFactor);
            }
            else
            {
                NewCylinderCondensationFactor = 0;
            }

            // Steam consumption will be increased by this amount, and MEP will be decreased by the amount

            float SteamConsumptionCondensationIncreaseFactor = 1 + NewCylinderCondensationFactor; // This factor is used to increase steam consumption and decrease MEP to allow for cylinder condensation

            Run(DriveWheelRevRpS * 60, AdmissionCylinderFraction, ActualCutoffCylinderFraction, ReleaseCylinderFraction, CompressionCylinderFraction, 0.5);


        }


        // ============================================================
        // FULL Hall MASS-BASED STEAM SOLVER WITH:
        // - RK4 INTEGRATION
        // - REGULATOR WIRE DRAWING (NONLINEAR)
        // - STEAM PIPE RESTRICTION
        // - FULL 0–360° CYCLE
        // - STROKE-BASED EVENT REPORTING
        // - STABLE AT 0 RPM
        // ============================================================

        // =========================
        // CONSTANTS
        // =========================
        const double R_JpkgK = 461.5;
        const double GAMMA = 1.3;

        const double PSI_TO_PA = 6894.76;
        const double PA_TO_PSI = 1.0 / PSI_TO_PA;

        const double MIN_P = 1000.0;

        // =========================
        // FLOW COEFFICIENTS (FIX 1)
        // =========================
        const double Cd_port = 0.75;   // ?? NEW: realistic port discharge
        const double Cd_reg = 0.75;    // ?? NEW: regulator higher efficiency
        const double Cd_pipe = 0.85;     // ?? NEW: steam pipe flow coefficient

        // =========================
        // USER INPUTS
        // =========================

        public double ConRodLengthM;
        public double ClearanceFrac;

        //  public double BoilerPressurePSI;
        
        public double PortWidth_m = Me.FromIn(2.25f);          // 2.25 in;

        public double Throttle;

        // Superheat / condensation
        public bool HasSuperheater;
        public double Superheat_K = 420;           // set externally if desired
   
        bool InsideAdmission = false; // Set to true for Stephenson valve gear with inside admission, false for Walschaert and Stephenson with outside admission

        // =========================
        // RESULTS
        // =========================
        public double pAdmission_Pa, pCutoff_Pa, pRelease_Pa, pCompression_Pa, pTarget_Pa;
        double admissionStartFrac, cutoffFrac, releaseFrac, compressionFrac;

        public double SteamPerRevKgpS, SteamRate_lbhr;
        public double IHP_W, MEP_Pa;
        public double ChestPressure_PSI;

        double targetFrac = 0.5; // Target fraction of boiler pressure for iterative solver

        double mTotal_Kg;
        double pChestSum = 0;

        double pBack_Pa;
        double mBoiler;  // boiler → chest

        double lastTheta = 0;
        double lastYi = 0;
        double lastYo = 0;


        // =========================
        // HELPERS
        // =========================
        double ClampP(double p) => Math.Max(p, MIN_P);

        bool IsFinite(double x)
        {
            return !(double.IsNaN(x) || double.IsInfinity(x));
        }

        // =========================
        // STEAM PROPERTIES
        // =========================

        double Tsat_K(double P)
        {
            double bar = P / 1e5;
            return 104.87 * Math.Pow(bar, 0.1949) + 273.15;
        }

        double SteamTemp(double P)
        {
            double Ts = Tsat_K(P);
            return HasSuperheater ? Ts + Superheat_K : Ts;
        }

        // =========================
        // REGULATOR
        // =========================

        double CalculateRegulatorEffectiveArea(double throttle)
        {
            // NONLINEAR regulator characteristic (NEW)
            // Realistic: heavily restricted at small openings

            return SERegulatorMaxAreaM2 * Math.Pow(Math.Max(throttle, 0.0), 2.5);
        }

        // ============================================================
        // CYLINDER GEOMETRY
        // ============================================================

        double StrokeFraction(double theta)
        {
            double r = CylindersStrokeM / 2.0;
            double L = ConRodLengthM;

            double term = Math.Max(L * L - r * r * Math.Pow(Math.Sin(theta), 2), 1e-12);

            double x = r * (1 - Math.Cos(theta)) + (L - Math.Sqrt(term));
            return Math.Max(0, Math.Min(1, x / CylindersStrokeM));
        }

        double StrokeFracForward(double theta)
        {
            if (theta <= Math.PI)
                return 0.5 * (1.0 - Math.Cos(theta));   // forward stroke
            else
                return 0.5 * (1.0 - Math.Cos(2.0 * Math.PI - theta)); // mirror return stroke
        }

        double CylinderVolume(double frac)
        {
            double A = Math.PI * Math.Pow(CylindersDiameterM / 2, 2);
            double swept = A * CylindersStrokeM;
            return Math.Max(1e-6, swept * (frac + ClearanceFrac));
        }

        /// <summary>
        /// Calculates the derivative of cylinder volume with respect to crank angle theta.
        /// </summary>
        /// <param name="theta">Crank angle in radians.</param>
        /// <returns>The rate of change of cylinder volume with respect to theta.</returns>
        double dVdTheta(double theta)
        {
            double r = CylindersStrokeM / 2.0;
            double L = ConRodLengthM;

            double sinT = Math.Sin(theta);
            double cosT = Math.Cos(theta);

            double denom = Math.Sqrt(Math.Max(L * L - r * r * sinT * sinT, 1e-12));
            double dx = r * sinT + (r * r * sinT * cosT) / denom;

            double A = Math.PI * Math.Pow(CylindersDiameterM / 2, 2);
            return A * dx;
        }

        // ============================================================
        // HALL VALVE OPENING yi / yo
        // ============================================================

        double ComputeValveTravelRadius()
        {
            // Hall: rv = lap + max port opening
            return SESteamLapM + AdPortOpenM;
        }

        double ComputeAngleOfAdvanceRad(bool insideAdmission, double crankangle)
        {
            double lap = SESteamLapM;
            double lead = SEValveLeadM;
            double rv = ComputeValveTravelRadius();

            double ratio = (lap + lead) / Math.Max(rv, 1e-6);
            ratio = Math.Max(-1.0, Math.Min(1.0, ratio));

            double alpha = Math.Asin(ratio);

            // Inside admission (Stephenson reversed geometry)
            if (insideAdmission)
                alpha = Math.PI - alpha;

            return alpha;
        }

        double PortOpeningAdmission(double theta, double rv, double alpha)
        {
            double xv = rv * Math.Sin(theta + alpha);

            // Hall: opening occurs when displacement exceeds lap
            double yi = xv - SESteamLapM;

    //        Console.WriteLine($"PortAdmission - yi {Me.ToIn((float)yi)} in : CrankAngle {MathHelper.ToDegrees((float)theta)} deg : AoA {MathHelper.ToDegrees((float)alpha)} deg : xv {Me.ToIn((float)xv)} in : Lap {Me.ToIn((float)SESteamLapM)} in : RV {Me.ToIn((float)rv)} in : AdPortOpenM {Me.ToIn((float)AdPortOpenM)} in");

                
            return Math.Max(0.0, yi);
        }

        double PortOpeningExhaust(double theta, double rv, double alpha)
        {
            double xv = rv * Math.Sin(theta + alpha);

            double yo;

            if (!InsideAdmission)
            {
                
                yo = -xv - SEValveExhaustLapM;
            }
            else
            {
                yo = (xv + SEValveExhaustLapM);
            }

   //         Console.WriteLine($"Exhaust Port Opening - yo {Me.ToIn((float)yo)} in : CrankAngle {MathHelper.ToDegrees((float)theta)} deg : AoA {MathHelper.ToDegrees((float)alpha)} deg : XV {Me.ToIn((float)xv)} in : Valve Exhaust {Me.ToIn((float)SEValveExhaustLapM)} in : ExPortOpening {Me.ToIn((float)ExPortOpenM)} in");

            // yo > 0 → exhaust open
            // yo → 0 → closure point
            // yo = 0 → compression begins

            return Math.Max(0.0, yo);
        }

        // ============================================================
        // TRUE COMPRESSIBLE FLOW (HALL BASIS)
        // ============================================================

        double CriticalPressureRatio()
        {
            return Math.Pow(2.0 / (GAMMA + 1.0), GAMMA / (GAMMA - 1.0));
        }

        double MassFlow(double pup, double pdown, double T, double A)
        {
            if (A < 1e-10) return 0;

            pup = ClampP(pup);
            pdown = ClampP(pdown);

            if (pdown >= pup) return 0;

            double pr = pdown / pup;
            double crit = CriticalPressureRatio();

            double coeff = A * pup / Math.Sqrt(R_JpkgK * T);

            if (pr <= crit)
            {
                // CHOKED FLOW
                return coeff * Math.Sqrt(GAMMA) * Math.Pow(2.0 / (GAMMA + 1.0), (GAMMA + 1.0) / (2.0 * (GAMMA - 1.0)));
            }
            else
            {
                // SUBSONIC
                double term = Math.Pow(pr, 2.0 / GAMMA) - Math.Pow(pr, (GAMMA + 1.0) / GAMMA);
                return coeff * Math.Sqrt(2.0 * GAMMA / (GAMMA - 1.0) * term);
            }
        }

        // =========================
        // RK4 STEP (MASS-BASED)
        // =========================
        void RK4Step(
            ref double mCyl,
            ref double mChest,
            double V,
            double dVdt,
            double pBack,
            double Aadm,
            double Aexh,
            double Areg,
            double PipeArea,
            double BoilerP,
            double dt)
        {
            Func<double, double, (double dmCyl, double dmChest)> deriv =
            (mc, mch) =>
            {
                double Tcyl = SteamTemp(Math.Max(ClampP(mc * R_JpkgK * 400 / V), MIN_P));
                double Tch = SteamTemp(Math.Max(ClampP(mch * R_JpkgK * 400 / SESteamChestVolumeM3), MIN_P));

                //Use direct temperature model, not back-calculated pressure:
               // double Tcyl = HasSuperheater ? Superheat_K : Tsat_K(pCyl);
               // double Tch = HasSuperheater ? Superheat_K : Tsat_K(pCh);


                double pCyl = mc * R_JpkgK * Tcyl / V;
                double pCh = mch * R_JpkgK * Tch / SESteamChestVolumeM3;

                // Admission to cylinder
                double mIn = Cd_port * MassFlow(pCh, pCyl, Tch, Aadm); // chest → cylinder
                // Exhaust
                double mOut = Cd_port * MassFlow(pCyl, pBack, Tcyl, Aexh); // cylinder → exhaust
                // Regulator
                mBoiler = Cd_reg * MassFlow(BoilerP, pCh, Tch, Areg);

                // Pipe restriction
                double mPipe = Cd_pipe * MassFlow(pCh, pCyl, Tch, PipeArea);

                double dmC = mIn - mOut;
                double dmCh = mBoiler - mIn;

                return (dmC, dmCh);
            };

            var k1 = deriv(mCyl, mChest);
            var k2 = deriv(mCyl + 0.5 * dt * k1.dmCyl, mChest + 0.5 * dt * k1.dmChest);
            var k3 = deriv(mCyl + 0.5 * dt * k2.dmCyl, mChest + 0.5 * dt * k2.dmChest);
            var k4 = deriv(mCyl + dt * k3.dmCyl, mChest + dt * k3.dmChest);

            mCyl += dt / 6.0 * (k1.dmCyl + 2 * k2.dmCyl + 2 * k3.dmCyl + k4.dmCyl);
            mChest += dt / 6.0 * (k1.dmChest + 2 * k2.dmChest + 2 * k3.dmChest + k4.dmChest);

            mCyl = Math.Max(mCyl, 1e-8);
            mChest = Math.Max(mChest, 1e-6);
        }

        // ============================================================
        // MAIN SOLVER (HALL BALANCE)
        // ============================================================

        public void Run(double rpm, double admissionStart, double cutoff, double release, double compression, double targetFrac)
        {
            if (!HasSuperheater) Superheat_K = 0;

            double steps = 720;
            double omegaRadpS = Math.Max(2 * Math.PI * rpm / 60.0, 0.5); // angular velocity - ?? FIX: stable at 0 rpm
            double dTheta = 2 * Math.PI / steps;
            double dt = dTheta / omegaRadpS;


            double BoilerP = Locomotive.BoilerPressurePSI * PSI_TO_PA;
            double pCylinder = BoilerP;
            double pChest = BoilerP;

            double V0 = CylinderVolume(0.0);
            double Tcyl0 = SteamTemp(BoilerP);

            // ?? MASS STATE
            double mCyl = BoilerP * V0 / (R_JpkgK * Tcyl0);
            double mChest = BoilerP * SESteamChestVolumeM3 / (R_JpkgK * Tcyl0);

        //    Console.WriteLine($"Initial Mass - mCyl {mCyl} kg : mChest {mChest} kg ");

            double work = 0;
            mTotal_Kg = 0;
            Throttle = Locomotive.throttle;

            lastTheta = 0;
            lastYi = 0;
            lastYo = 0;
            pChestSum = 0;

            double prevP;

            double theta0 = 2.0 * Math.PI;

            double rv0 = ComputeValveTravelRadius();
            double AoA0 = ComputeAngleOfAdvanceRad(InsideAdmission, theta0);

            lastYi = PortOpeningAdmission(theta0, rv0, AoA0);
            lastYo = PortOpeningExhaust(theta0, rv0, AoA0);

            double V0_last = CylinderVolume(StrokeFraction(theta0));
            double Tcyl_last = SteamTemp(BoilerP);
            double lastP = BoilerP;

            prevP = lastP;

            pAdmission_Pa = pCutoff_Pa = pRelease_Pa = pCompression_Pa = pTarget_Pa = double.NaN;

            admissionStartFrac = cutoffFrac = releaseFrac = compressionFrac = double.NaN;

            // =========================
            // PIPE AREAS (NEW)
            // =========================
            double RegMaxArea = Me2.ToIn2((float)SERegulatorMaxAreaM2) * 0.00064516;

            // Typical pipe areas (GUIDELINE)
            double PipeArea = 0.6 * RegMaxArea;      // steam pipe
            
            for (int i = 0; i < steps; i++)
            {
                double CurrentCrankAngleRad = i * dTheta; // crank angle now 0 -> 360 degrees (2 * PI radians) over the course of the loop

                double frac = StrokeFraction(CurrentCrankAngleRad); // Return the fraction at the current crank angle, based upon the geometry of the cylinder and connecting rod
                double V = CylinderVolume(frac); // Return the cylinder volume at the current crank angle, based upon the geometry of the cylinder and connecting rod
                double dVdt = dVdTheta(CurrentCrankAngleRad) * omegaRadpS; // Return the rate of change of cylinder volume at the current crank angle, based upon the geometry of the cylinder and connecting rod, and the angular velocity

                double rv = ComputeValveTravelRadius();
                double AoA = ComputeAngleOfAdvanceRad(InsideAdmission, CurrentCrankAngleRad);

                double yi = PortOpeningAdmission(CurrentCrankAngleRad, rv, AoA);
                double yo = PortOpeningExhaust(CurrentCrankAngleRad, rv, AoA);

                double Aadm = Math.Min(yi * PortWidth_m, PortWidth_m * AdPortOpenM);
                double Aexh = Math.Min(yo * PortWidth_m, PortWidth_m * ExPortOpenM);

                double pBack = (5 + 0.03 * rpm) * PSI_TO_PA;

     //           Console.WriteLine($"yi {Me.ToIn((float)yi)} in : yo {Me.ToIn((float)yo)} in : CrankAngle {MathHelper.ToDegrees((float) CurrentCrankAngleRad)} deg : AoA {MathHelper.ToDegrees((float)AoA)} deg : PortWidth {Me.ToIn((float)PortWidth_m)} in : AdPortOpening {Me.ToIn((float)AdPortOpenM)} in : ExPortOpening {Me.ToIn((float)ExPortOpenM)} in");



                // =========================
                // FLOW PATH (NEW)
                // =========================

                // Regulator
                double Areg = CalculateRegulatorEffectiveArea(Throttle);
                

                // RK4 MASS UPDATE
                RK4Step(ref mCyl, ref mChest, V, dVdt, pBack, Aadm, Aexh, Areg, PipeArea, BoilerP, dt);

                //Use direct temperature model, not back-calculated pressure:
              //  double Tcyl = HasSuperheater ? Superheat_K : Tsat_K(pCyl);
             //   double Tch = HasSuperheater ? Superheat_K : Tsat_K(pCh);

                double Tcyl = SteamTemp(Math.Max(ClampP(mCyl * R_JpkgK * 400 / V), MIN_P));
                pCylinder = mCyl * R_JpkgK * Tcyl / V;

                double Tch = SteamTemp(Math.Max(ClampP(mChest * R_JpkgK * 400 / SESteamChestVolumeM3), MIN_P));
                pChest = mChest * R_JpkgK * Tch / SESteamChestVolumeM3;

           //     Console.WriteLine($"Steam Chest - I {i} : pChest {pChest * PA_TO_PSI} psi : mChest {mChest} kg : Tch {Tch} K : PChest Sum {pChestSum * PA_TO_PSI} psi");

                if (Aadm <= 0)
                {
                    pChest = BoilerP;
                    // No flow, so cylinder pressure cannot exceed steam chest pressure
                    pCylinder = Math.Min(pCylinder, pChest);
                }

                pBack_Pa = pBack;

                // WORK
                if (IsFinite(pCylinder))
                    work += pCylinder * dVdTheta(CurrentCrankAngleRad) * dTheta;

                // =========================
                // EVENT INTERPOLATION (FIXED)
                // =========================
                double interp(double last, double curr)
                {
                    if (Math.Abs(last - curr) < 1e-8) return 0;
                    return last / (last - curr);
                }

                if (lastYi <= 0 && yi > 0 && double.IsNaN(pAdmission_Pa))
                {
                    double f = interp(lastYi, yi);
                    pAdmission_Pa = prevP + f * (pCylinder - prevP);
                    admissionStartFrac = StrokeFracForward(CurrentCrankAngleRad);
                }

                if (lastYi > 0 && yi <= 0 && double.IsNaN(pCutoff_Pa))
                {
                    double f = interp(lastYi, yi);
                    pCutoff_Pa = prevP + f * (pCylinder - prevP);
                    cutoffFrac = StrokeFracForward(CurrentCrankAngleRad);
                }

                if (lastYo <= 0 && yo > 0 && double.IsNaN(pRelease_Pa))
                {
                    double f = interp(lastYo, yo);
                    pRelease_Pa = prevP + f * (pCylinder - prevP);
                    releaseFrac = StrokeFracForward(CurrentCrankAngleRad);
                }

                if (((lastYo > 0 && yo <= 0) || (lastYo == 0 && yo < 0)) && double.IsNaN(pCompression_Pa))
                {
                    double f = interp(lastYo, yo);
                    pCompression_Pa = prevP + f * (pCylinder - prevP);
                    compressionFrac = StrokeFracForward(CurrentCrankAngleRad);
                    //                 Console.WriteLine($"Compression Detection: Compression_Pa {pCompression_Pa * PA_TO_PSI:F2} psi: f {f} : prevP {prevP * PA_TO_PSI:F2} psi : pCylinder {pCylinder * PA_TO_PSI:F2} psi");
                }

                double targetTheta = targetFrac * Math.PI;

                if (double.IsNaN(pTarget_Pa) && CurrentCrankAngleRad >= targetTheta)
                {
                    pTarget_Pa = pCylinder;
                }

                prevP = pCylinder;
                lastYi = yi;
                lastYo = yo;

                mTotal_Kg += mBoiler * dt; // approximate flow tracking

                pChestSum += pChest;
            }

            // =========================
            // OUTPUTS
            // =========================
            SteamPerRevKgpS = mTotal_Kg * NumberCylinders * 2;
            SteamRate_lbhr = SteamPerRevKgpS * rpm * 60 * 2.20462;

            double pistonArea = Math.PI * Math.Pow(CylindersDiameterM / 2, 2);
            double sweptVolume = pistonArea * CylindersStrokeM;

            double cyclesPerSec = rpm / 60.0;

            IHP_W = work * cyclesPerSec * NumberCylinders * 2;
            MEP_Pa = work / sweptVolume;

            ChestPressure_PSI = (pChestSum / steps) * PA_TO_PSI;

            //       Console.WriteLine($"Chest PSI: {pChestSum * PA_TO_PSI} psi : Steps {steps}");

            // Note solver produces absolute pressures, so we convert to gauge pressure for reporting and comparison with other calculations

            Console.WriteLine($"Speed: {Locomotive.AbsSpeedMpS} mph : Cutoff {Locomotive.cutoff * 100} %");
            Console.WriteLine($"Boiler; {Locomotive.BoilerPressurePSI} psi, Steam Chest: {ChestPressure_PSI} psi");
            Console.WriteLine($"Admission Pressure: {admissionStartFrac * 100} %, {pAdmission_Pa * PA_TO_PSI:F2} psi");
            Console.WriteLine($"Cutoff Pressure: {cutoffFrac * 100} %, {pCutoff_Pa * PA_TO_PSI:F2} psi");
            Console.WriteLine($"Release Pressure: {releaseFrac * 100} %, {pRelease_Pa * PA_TO_PSI:F2} psi");
            Console.WriteLine($"Compression Pressure: {compressionFrac * 100} %, {pCompression_Pa * PA_TO_PSI:F2} psi");
            Console.WriteLine($"Back Pressure: {(float)pBack_Pa * PA_TO_PSI:F2} psi");
            Console.WriteLine($"Lead: {Me.ToIn((float)SEValveLeadM):F2} in");
            Console.WriteLine($"Lap: {Me.ToIn((float)SESteamLapM):F2} in");
            Console.WriteLine($"MaxAdmissionPortOpening: {Me.ToIn((float)AdPortOpenM):F4} in");
            Console.WriteLine($"MaxExhaustPortOpening: {Me.ToIn((float)ExPortOpenM):F4} in");
            Console.WriteLine($"SteamChestVolume: {Me3.ToFt3((float)SESteamChestVolumeM3):F3} ft3");
            Console.WriteLine($"PortWidth: {Me.ToIn((float)PortWidth_m):F3} in");
            Console.WriteLine($"Regulator Maximum Opening: {Me2.ToIn2((float)SERegulatorMaxAreaM2):F3} in2");

            Console.WriteLine($"Target Cylinder Pressure: {pTarget_Pa * PA_TO_PSI:F2} psi");

            Console.WriteLine($"Steam per rev: {SteamPerRevKgpS:F5} kg");
            Console.WriteLine($"Steam rate: {SteamRate_lbhr:F2} lb/hr");

            Console.WriteLine($"Theta: {lastTheta:F3}  yi: {lastYi:F5}  yo: {lastYo:F5}");
     //       Console.WriteLine($"Valve Phase (deg): {phi * 180.0 / Math.PI:F2}");

            Console.WriteLine();

            AdmissionCylinderFraction = admissionStartFrac;
            ActualCutoffCylinderFraction = cutoffFrac;
            ReleaseCylinderFraction = releaseFrac;
            CompressionCylinderFraction = compressionFrac;

            if (Locomotive.throttle < 0.01f)
            {
                SESteamChestPressurePSI = 0; // No steam flow, so no pressure in steam chest
                Pressure_b_AtmPSI = 0; // No steam flow, so no pressure in cylinder at cutoff
                Pressure_c_AtmPSI = 0; // No steam flow, so no pressure in cylinder at release
                Pressure_f_AtmPSI = 0; // No steam flow, so no pressure in cylinder at compression
                SESteamCylinderConsumptionKgpS = 0; // No steam flow, so no steam consumption in cylinder

            }
            else
            {
                SESteamChestPressurePSI = (float)(ChestPressure_PSI); // Convert to absolute pressure for use in other calculations
                Pressure_b_AtmPSI = (float)(pCutoff_Pa * PA_TO_PSI) + Locomotive.OneAtmospherePSI; // Convert to absolute pressure for use in other calculations
                Pressure_c_AtmPSI = (float)(pRelease_Pa * PA_TO_PSI) + Locomotive.OneAtmospherePSI; // Convert to absolute pressure for use in other calculations
                Pressure_f_AtmPSI = (float)(pCompression_Pa * PA_TO_PSI) + Locomotive.OneAtmospherePSI; // Convert to absolute pressure for use in other calculations
               //    Pressure_a_AtmPSI = (float)pAdmission + Locomotive.OneAtmospherePSI; // Convert to absolute pressure for use in other calculations
                
                SESteamCylinderConsumptionKgpS = (float)SteamPerRevKgpS; // Total steam consumption in kg/s
            }

            HallIHP = (float)IHP_W * 0.00134102f;
            HallMEP = (float)MEP_Pa * (float)PA_TO_PSI;


        }





        private void CalculateSingleExpansionCylinderSteamIndicatorDiagram()
        {
            // Principle source of reference for this section is - "Locomotive Operation - A Technical and Practical Analysis" by G. R. Henderson  - pg 128
            // https://archive.org/details/locomotiveoperat00hend/page/128/mode/2up

            // Calculate apparent volumes at various points in cylinder
            float CylinderVolumePoint_e = (float)CompressionCylinderFraction + SECylinderClearancePC;
            float CylinderVolumePoint_f = (float)AdmissionCylinderFraction + SECylinderClearancePC;

            // Note all pressures in absolute pressure for working on steam indicator diagram. MEP will be just a gauge pressure value as it is a differencial pressure calculated as an area off the indicator diagram
            // The pressures below are as calculated and referenced to the steam indicator diagram for single expansion locomotives by letters shown in brackets - see Coals to Newcastle website
            // Calculate Ratio of expansion, with cylinder clearance
            // R (ratio of Expansion) = (length of stroke to point of  exhaust + clearance) / (length of stroke to point of cut-off + clearance)
            // Expressed as a fraction of stroke R = (Exhaust point + c) / (cutoff + c)
            float SERatioOfExpansion_bc = ((float)ReleaseCylinderFraction + SECylinderClearancePC) / (Locomotive.cutoff + SECylinderClearancePC);
            // Absolute Mean Pressure = Ratio of Expansion

            //  SESteamChestPressurePSI = (Locomotive.throttle * (Locomotive.BoilerPressurePSI - SteamChestPressureReductionPSI)); // pressure in cylinder steam chest - allowance for pressure drop between boiler and steam chest

            SELogSteamChestPressurePSI = SESteamChestPressurePSI;  // Value for recording in log file
            SELogSteamChestPressurePSI = MathHelper.Clamp(SELogSteamChestPressurePSI, 0.00f, SELogSteamChestPressurePSI); // Clamp so that steam chest pressure does not go negative

            // Initial pressure will be decreased depending upon locomotive speed
            // This drop can be adjusted with a table in Eng File
            // (a) - Initial Pressure
            Pressure_a_AtmPSI = SESteamChestPressurePSI + Locomotive.OneAtmospherePSI; // This is the gauge pressure + atmospheric pressure to find the absolute pressure - pressure drop gas been allowed for as the steam goes into the cylinder through the opening in the steam chest port.

            SELogInitialPressurePSI = Pressure_a_AtmPSI - Locomotive.OneAtmospherePSI; // Value for log file & display
            SELogInitialPressurePSI = MathHelper.Clamp(SELogInitialPressurePSI, 0.00f, SELogInitialPressurePSI); // Clamp so that initial pressure does not go negative

            // (b) - Cutoff Pressure
            var CondensationPressureDropPSI = 1 - NewCylinderCondensationFactor; // Pressure drop due to cylinder condensation

            //    Pressure_b_AtmPSI = (Pressure_a_AtmPSI * MEPWireDrawingFactor) * CondensationPressureDropPSI;

            SELogCutoffPressurePSI = Pressure_b_AtmPSI - Locomotive.OneAtmospherePSI;   // Value for log file
            SELogCutoffPressurePSI = MathHelper.Clamp(SELogCutoffPressurePSI, 0.00f, SELogCutoffPressurePSI); // Clamp so that Cutoff pressure does not go negative

            // (c) - Release pressure 
            // Release pressure = Cutoff Pressure x Cylinder Volume (at cutoff point) / cylinder volume (at release)
            //  Pressure_c_AtmPSI = (Pressure_b_AtmPSI) * (Locomotive.cutoff + Locomotive.CylinderClearancePC) / ((float)ReleaseCylinderFraction + Locomotive.CylinderClearancePC);  // Check factor to calculate volume of cylinder for new volume at exhaust

            SELogReleasePressurePSI = Pressure_c_AtmPSI - Locomotive.OneAtmospherePSI;   // Value for log file
            SELogReleasePressurePSI = MathHelper.Clamp(SELogReleasePressurePSI, 0.00f, SELogReleasePressurePSI); // Clamp so that Release pressure does not go negative

            // (d) - Back Pressure 
            Pressure_d_AtmPSI = CylinderBackPressurePSIG + Locomotive.OneAtmospherePSI;

            if (Locomotive.throttle < 0.02f)
            {
                Pressure_a_AtmPSI = 0.0f;  // for sake of display zero pressure values if throttle is closed.
                Pressure_d_AtmPSI = 0.0f;
            }

            SELogBackPressurePSI = Pressure_d_AtmPSI - Locomotive.OneAtmospherePSI;  // Value for log file
            SELogBackPressurePSI = MathHelper.Clamp(SELogBackPressurePSI, 0.00f, SELogBackPressurePSI); // Clamp so that Back pressure does not go negative

            // (e) - Compression Pressure 
            // Calculate pre-compression pressure based upon back pressure being equal to it, as steam should be exhausting
            Pressure_e_AtmPSI = Pressure_d_AtmPSI;

            SELogPreCompressionPressurePSI = Pressure_e_AtmPSI - Locomotive.OneAtmospherePSI;   // Value for log file
            SELogPreCompressionPressurePSI = MathHelper.Clamp(SELogPreCompressionPressurePSI, 0.00f, SELogPreCompressionPressurePSI); // Clamp so that pre compression pressure does not go negative

            // (f) - Admission pressure 
            //   Pressure_f_AtmPSI = Pressure_e_AtmPSI * ((float)CompressionCylinderFraction + Locomotive.CylinderClearancePC) / ((float)AdmissionCylinderFraction + Locomotive.CylinderClearancePC);  // Check factor to calculate volume of

            SELogPreAdmissionPressurePSI = Pressure_f_AtmPSI - Locomotive.OneAtmospherePSI;   // Value for log file
            SELogPreAdmissionPressurePSI = MathHelper.Clamp(SELogPreAdmissionPressurePSI, 0.00f, SELogPreAdmissionPressurePSI); // Clamp so that pre admission pressure does not go negative

            // ****** Calculate Cylinder Work *******
            // In driving the wheels steam does work in the cylinders. The amount of work can be calculated by a typical steam indicator diagram
            // Mean Effective Pressure (work) = average positive pressures - average negative pressures
            // Average Positive pressures = admission + expansion + release
            // Average Negative pressures = exhaust + compression + pre-admission

            // Calculate Av Admission Work (inch pounds) between a) - b)
            // Av Admission work = Av (Initial Pressure + Cutoff Pressure) * length of Cylinder to cutoff
            // Mean Pressure
            float MeanPressure_ab_AtmPSI = ((Pressure_a_AtmPSI + Pressure_b_AtmPSI) / 2.0f);
            // Calculate volume between a - b
            float CylinderLength_ab_In = Me.ToIn(CylindersStrokeM * ((Locomotive.cutoff + Locomotive.CylinderClearancePC) - Locomotive.CylinderClearancePC));
            // Calculate work - a-b
            CylinderWork_ab_InLbs = MeanPressure_ab_AtmPSI * CylinderLength_ab_In;

            // Calculate Av Expansion Work (inch pounds) - between b) - c)
            // Av pressure during expansion = Cutoff pressure x log (ratio of expansion) / (ratio of expansion - 1.0) 
            // Av Expansion work = Av pressure during expansion * length of Cylinder during expansion
            // Mean Pressure
            float MeanPressure_bc_AtmPSI = Pressure_b_AtmPSI * ((float)Math.Log(SERatioOfExpansion_bc) / (SERatioOfExpansion_bc - 1.0f));
            // Calculate volume between b-c
            float CylinderLength_bc_In = Me.ToIn(CylindersStrokeM) * (((float)ReleaseCylinderFraction + Locomotive.CylinderClearancePC) - (Locomotive.cutoff + Locomotive.CylinderClearancePC));
            // Calculate work - b-c
            CylinderWork_bc_InLbs = MeanPressure_bc_AtmPSI * CylinderLength_bc_In;

            // Calculate Av Release work (inch pounds) - between c) - d)
            // Av Release work = Av pressure during release * length of Cylinder during release
            // Mean Pressure
            float MeanPressure_cd_AtmPSI = ((Pressure_c_AtmPSI + Pressure_d_AtmPSI) / 2.0f);
            // Calculate volume between c-d
            float CylinderLength_cd_In = Me.ToIn(CylindersStrokeM) * ((1.0f + Locomotive.CylinderClearancePC) - ((float)ReleaseCylinderFraction + Locomotive.CylinderClearancePC)); // Full cylinder length is 1.0

            // Calculate work - c-d             
            CylinderWork_cd_InLbs = MeanPressure_cd_AtmPSI * CylinderLength_cd_In;

            // Calculate Av Exhaust Work (inch pounds) - between d) - e)
            // Av Exhaust work = Av pressure during exhaust * length of Cylinder during exhaust stroke
            // Mean Pressure
            float MeanPressure_de_AtmPSI = ((Pressure_d_AtmPSI + Pressure_e_AtmPSI) / 2.0f);
            // Calculate volume between d-e
            float CylinderLength_de_In = Me.ToIn(CylindersStrokeM) * ((1.0f + Locomotive.CylinderClearancePC) - ((float)CompressionCylinderFraction + Locomotive.CylinderClearancePC)); // Full cylinder length is 1.0

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
            float CylinderLength_ef_In = Me.ToIn(CylindersStrokeM) * (CylinderVolumePoint_e - CylinderVolumePoint_f);
            // Calculate work - e-f
            CylinderWork_ef_InLbs = MeanPressure_ef_AtmPSI * CylinderLength_ef_In;

            // Calculate Av Pre-admission work (inch pounds) - between f) - a)
            // Av Pre-admission work = Av pressure during pre-admission * length of Cylinder during pre-admission stroke
            // Mean Pressure
            float MeanPressure_fa_AtmPSI = ((Pressure_a_AtmPSI + Pressure_f_AtmPSI) / 2.0f);
            // Calculate volume between f-a
            float CylinderLength_fa_In = (float)AdmissionCylinderFraction * Me.ToIn(CylindersStrokeM);
            // Calculate work - f-a
            CylinderWork_fa_InLbs = MeanPressure_fa_AtmPSI * CylinderLength_fa_In;

            // Calculate total work in cylinder
            float TotalWorkInLbs = CylinderWork_ab_InLbs + CylinderWork_bc_InLbs + CylinderWork_cd_InLbs - CylinderWork_de_InLbs - CylinderWork_ef_InLbs - CylinderWork_fa_InLbs;

            SEMeanEffectivePressurePSI = TotalWorkInLbs / Me.ToIn(CylindersStrokeM); // MEP doesn't need to be converted from Atm to gauge pressure as it is a differential pressure.
            SEMeanEffectivePressurePSI = MathHelper.Clamp(SEMeanEffectivePressurePSI, 0, Locomotive.MaxBoilerPressurePSI); // Make sure that Cylinder pressure does not go negative

            if (float.IsNaN(SEMeanEffectivePressurePSI) || Locomotive.throttle < 0.01)
            {
                SEMeanEffectivePressurePSI = 0;
            }

            //      Trace.TraceInformation("RPM {0}", pS.TopM(DriveWheelRevRpS));
  /*          
                   //     if (DriveWheelRevRpS >= 55.0 && DriveWheelRevRpS < 55.1 || DriveWheelRevRpS >= 110.0 && DriveWheelRevRpS < 110.1 || DriveWheelRevRpS >= 165.0 && DriveWheelRevRpS < 165.05 || DriveWheelRevRpS >= 220.0 && DriveWheelRevRpS < 220.05)
                        if (Locomotive.AbsSpeedMpS > 6 && Locomotive.AbsSpeedMpS <= 6.5f )
                        {
                            Trace.TraceInformation("***************************************** Single Expansion Steam Locomotive ***************************************************************");

                            Trace.TraceInformation("All pressures in Atmospheric Pressure (ie Added 14.5psi)");

                            Trace.TraceInformation("*********** Operating Conditions *********");

                            Trace.TraceInformation("Boiler Pressure {0}", Locomotive.BoilerPressurePSI);

                            Trace.TraceInformation("Throttle {0} Cutoff {1}  Revs {2} RelPt {3} Clear {4}", Locomotive.throttle, Locomotive.cutoff, pS.TopM(DriveWheelRevRpS), ReleaseCylinderFraction, SECylinderClearancePC);

                            Trace.TraceInformation("*********** Cylinder *********");

                            Trace.TraceInformation("Cylinder Pressures: a {0} b {1} c {2} d {3} e {4} f {5}", Pressure_a_AtmPSI, Pressure_b_AtmPSI, Pressure_c_AtmPSI, Pressure_d_AtmPSI, Pressure_e_AtmPSI, Pressure_f_AtmPSI);

                            Trace.TraceInformation("MeanPressure b-c (Expansion):MeanPressure b-c {0} ExpRatio {1} cutoff {2} Release {3}", MeanPressure_bc_AtmPSI, SERatioOfExpansion_bc, ActualCutoffCylinderFraction, ReleaseCylinderFraction);

                            Trace.TraceInformation("MeanPressure e-f (Compression): MeanPressure e-f {0} CompRatio {1} Vol_e {2} Vol_f {3}", MeanPressure_ef_AtmPSI, RatioOfCompression_ef, CylinderVolumePoint_e, CylinderVolumePoint_f);

                            Trace.TraceInformation("Cylinder Works: Total {0} === a-b {1} b-c {2} c-d {3} d-e {4} e-f {5} f-a {6}", TotalWorkInLbs, CylinderWork_ab_InLbs, CylinderWork_bc_InLbs, CylinderWork_cd_InLbs, CylinderWork_de_InLbs, CylinderWork_ef_InLbs, CylinderWork_fa_InLbs);

                            Trace.TraceInformation("MEP {0}", SEMeanEffectivePressurePSI);
                        }
            */

        }


        /// <summary>
        /// Find Crank Angle @ a certain cutoff point. 
        /// We will calculate the crank angle at the maximum cutoff point, and then use this to calculate the steam lap, which will be constant 
        /// for all cutoff points. We can then calculate the crank angle for any cutoff point using the same steam lap. Angularity Ratio = L/S, 
        /// where L is the connecting rod length, S is the cylinder stroke. This is used to calculate the crank angle at a given cutoff point, 
        /// based upon the distance from the crank centre to the piston at that cutoff point, which is in turn based upon the valve travel at 
        /// that cutoff point. The formula for calculating the crank angle is as follows:
        /// Crank Ang = ArcCos ( ( L/S + 0.5 - cutoff)^2 - (L/S)^2 + 0.25) / (L/S + 0.5 - cutoff) )
        /// </summary>
        public double CalculateCrankAngle(float cutoff)
        {
            var AngularityRatio = SEConnectRodLengthM / CylindersStrokeM;

            double numerator = Math.Pow((AngularityRatio + 0.5f - cutoff), 2) - Math.Pow(AngularityRatio, 2) + 0.25;

            double denominator = AngularityRatio + 0.5f - cutoff;

            double crankanglerad = Math.Acos(numerator / denominator);

            return crankanglerad;
        }

        /// <summary>
        /// Calculate Angle of Advance
        /// Angle of Advance = ArcTan ( Sin(CutoffCrankAngle) / ((Lap / (Lap + Lead)) - cos(CutoffCrankAngle)) )
        /// </summary>
        public double CalculateAngleOfAdvance(double crankangle, double steamlap, double valvelead)
        {
            double angleofadvancerad = Math.Atan(Math.Sin(crankangle) / ((steamlap / (steamlap + valvelead)) - Math.Cos(crankangle)));

            // should formula be (steamlap / (steamlap + 1))??

            return angleofadvancerad;
        }

        /// <summary>
        /// Calculate Equivalent Eccentric Radius (HalfTravel) 
        /// req = (Lap + Lead) / Sin(Angle of Advance)
        /// </summary>
        public double CalculateValveHalfTravel(double steamlap, double valvelead, double angleofadvancerad)
        {
            double halftravel = 0;
            // Calculate half travel at users cutoff point. Gives infinity values if angle of advance = 0 (ie cutoff =0)
            if (angleofadvancerad != 0)
            {
                halftravel = (steamlap + valvelead) / Math.Sin(angleofadvancerad);
                return halftravel;
            }
            else
            {
                return 0;
            }
        }


    }





    

}
