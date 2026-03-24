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

using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks;
using ORTS.Scripting.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Orts.Simulation.RollingStocks.TrainCar;
using ORTS.Common;

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

        }

        public void InitializeMoving()
        {

        }

        public void Update(float elapsedClockSeconds)
        {


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

    }

}
