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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class DieselEngines : ISubSystem<DieselEngines>
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<DieselEngine> DEList = new List<DieselEngine>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return DEList.Count; } }

        public DieselEngineState State
        {
            get
            {
                DieselEngineState state = DieselEngineState.Stopped;

                foreach (DieselEngine dieselEngine in DEList)
                {
                    if (dieselEngine.State > state)
                        state = dieselEngine.State;
                }

                return state;
            }
        }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        protected readonly MSTSDieselLocomotive Locomotive;

        public MSTSGearBoxParams MSTSGearBoxParams = new MSTSGearBoxParams();

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public DieselEngines(MSTSDieselLocomotive loco)
        {
            Locomotive = loco;
        }

        public DieselEngine this[int i]
        {
            get { return DEList[i]; }
            set { DEList[i] = value; }
        }

        public void Add()
        {
            DEList.Add(new DieselEngine(Locomotive));
        }

        public void Add(DieselEngine de)
        {
            DEList.Add(de);
        }


        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">reference to the ENG file reader</param>
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsdieselengines":
                    stf.MustMatch("(");
                    int count = stf.ReadInt(0);
                    DEList.Clear(); // Remove any existing diesel engines to prevent errors
                    for (int i = 0; i < count; i++)
                    {
                        string setting = stf.ReadString().ToLower();
                        if (setting == "diesel")
                        {
                            DEList.Add(new DieselEngine(Locomotive));

                            DEList[i].Parse(stf);

                            // sets flag to indicate that a diesel eng prime mover code block has been defined by user, otherwise OR will define one through the next code section using "MSTS" values
                            DEList[i].DieselEngineConfigured = true;
                        }
                    }
                    break;
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
                    MSTSGearBoxParams.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(DieselEngines other)
        {
            DEList = new List<DieselEngine>();
            MSTSGearBoxParams.Copy(other.MSTSGearBoxParams);
            foreach (DieselEngine de in other.DEList)
            {
                DieselEngine dieselEngine = new DieselEngine(Locomotive);
                dieselEngine.Copy(de);

                DEList.Add(dieselEngine);
            }
        }

        public void Initialize()
        {
            foreach (DieselEngine de in DEList)
                de.Initialize();
        }

        public void InitializeMoving()
        {
            foreach (DieselEngine de in DEList)
                de.InitializeMoving();
        }

        public void EstablishParameters()
        {
            float totalTractionPower = 0;
            float totalETSPower = 0;

            foreach (DieselEngine de in DEList)
            {
                de.EstablishParameters();

                if (de.ProvidesTraction)
                    totalTractionPower += de.MaximumDieselPowerW;
                if (de.ProvidesETS)
                    totalETSPower += de.MaximumDieselPowerW;
            }

            // Can only determine some engine ratings after all parameters are established
            foreach (DieselEngine de in DEList)
                de.InitRailPower(totalTractionPower, totalETSPower);
        }

        /// <summary>
        /// Saves status of each auxiliary on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(DEList.Count);
            foreach (DieselEngine de in DEList)
                de.Save(outf);
        }

        /// <summary>
        /// Restores status of each auxiliary on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            if (DEList.Count == 0)
            {
                // If no diesel engines were saved, use BASIC configuration
                for (int i = 0; i < count; i++)
                    DEList.Add(new DieselEngine(Locomotive));

                EstablishParameters();
                Initialize();
            }
            foreach (DieselEngine de in DEList)
                de.Restore(inf);
        }

        /// <summary>
        /// A summary of instantaneous total power output of all the diesel engines
        /// </summary>
        public float OutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.OutputPowerW;
                return temp;
            }
        }

        /// <summary>
        /// A summary of instantaneous traction power output of all the diesel engines
        /// </summary>
        public float TractionPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.TractionPowerW;
                return temp;
            }
        }

        /// <summary>
        /// A summary of instantaneous auxiliary & ETS power output of all the diesel engines
        /// </summary>
        public float AuxiliaryPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.AuxiliaryPowerW;
                return temp;
            }
        }

        /// <summary>
        /// A summary of instantaneous power output available for traction of all the diesel engines
        /// </summary>
        public float AvailablePowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.ProvidesTraction ? de.AvailablePowerW : 0.0f;
                return temp;
            }
        }

        /// <summary>
        /// A summary of instantaneous power output available for ETS of all the diesel engines
        /// </summary>
        public float AvailableETSPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.ProvidesETS ? de.CurrentMaximumPowerW - de.AuxPowerTab[de.RealRPM] : 0.0f;
                return temp;
            }
        }

        /// <summary>
        /// A power-on indicator
        /// </summary>
        public bool PowerOn
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                    temp |= (de.State == DieselEngineState.Running) || (de.State == DieselEngineState.Starting);
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesel engines
        /// </summary>
        public float MaxPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.MaximumDieselPowerW;
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximum possible power of all the diesel engines at the current RPM
        /// </summary>
        public float MaxOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.CurrentMaximumPowerW;
                temp = Math.Max(temp, 0.0f);  // prevent -ve power
                return temp;
            }
        }

        /// <summary>
        /// Maximum rail output power for all deiesl engines
        /// </summary>
        public float MaximumRailOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.MaximumRailOutputPowerW;
                return temp;
            }
        }

        /// <summary>
        /// Total instantaneous fuel consumption of all diesel engines
        /// </summary>
        public float DieselFlowLpS
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                    temp += de.DieselFlowLpS;
                return temp;
            }
        }

        public bool HasGearBox
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                    temp |= (de.GearBox != null);
                return temp;
            }
        }

        /// <summary>
        /// Returns the tractive effort output of the gear box.
        /// </summary>
        public float TractiveForceN
        {
            get
            {
                float temp = 0;
                foreach (DieselEngine de in DEList)
                {
                    if (de.GearBox != null)
                    {
                        if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                            temp += de.GearBox.TractiveForceN;
                        else
                            temp += de.DemandedThrottlePercent * 0.01f * de.GearBox.TractiveForceN;
                    }
                }
                return temp;
            }
        }

        /// <summary>
        /// Updates all diesel engines
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (DieselEngine de in DEList)
                de.Update(elapsedClockSeconds);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            foreach (DieselEngine de in DEList)
                de.HandleEvent(evt);
        }

        public void HandleEvent(PowerSupplyEvent evt, int id)
        {
            if (id >= 0 && id < DEList.Count)
                DEList[id].HandleEvent(evt);
        }

        public List<DieselEngine>.Enumerator GetEnumerator()
        {
            return DEList.GetEnumerator();
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
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(eng.State)));

            if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                result.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), Simulator.Catalog.GetString(" "));  // Leave maximum power out
                foreach (var eng in DEList)
                {
                    result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.CurrentMaximumPowerW, Locomotive.IsMetric, false, false));
                }
            }
            else
            {
                result.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), FormatStrings.FormatPower(MaxOutputPowerW, Locomotive.IsMetric, false, false));
                foreach (var eng in DEList)
                    result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.CurrentMaximumPowerW, Locomotive.IsMetric, false, false));
            }

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Load"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F1}%", eng.LoadPercent);

            if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                foreach (var eng in DEList)
                {
                    var governorEnabled = eng.GovernorEnabled ? "???" : "";
                    result.AppendFormat("\t{0:F0} {2}{1}", eng.RealRPM, governorEnabled, FormatStrings.rpm);
                }
            }
            else
            {
                foreach (var eng in DEList)
                    result.AppendFormat("\t{0:F0} {1}", eng.RealRPM, FormatStrings.rpm);
            }

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Flow"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}/{1}", FormatStrings.FormatFuelVolume(pS.TopH(eng.DieselFlowLpS), Locomotive.IsMetric, Locomotive.IsUK), FormatStrings.h);

            result.Append("\t");
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatTemperature(eng.TemperatureDegC, Locomotive.IsMetric, false));

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Oil"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPressure(eng.DieselOilPressurePSI, PressureUnit.PSI, Locomotive.MainPressureUnit, true));

            return result.ToString();
        }

        public string GetDPStatus()
        {
            var result = new StringBuilder();
            var eng = DEList[0];
            result.AppendFormat("\t{0}", Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(eng.State)));
            result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.CurrentMaximumPowerW, Locomotive.IsMetric, false, false));
            result.AppendFormat("\t{0:F1}%", eng.LoadPercent);
            result.AppendFormat("\t{0:F0} {1}", eng.RealRPM, FormatStrings.rpm);
            result.AppendFormat("\t{0}", FormatStrings.FormatAirFlow(Locomotive.FilteredBrakePipeFlowM3pS, Locomotive.IsMetric));
            result.AppendFormat("\t{0}", FormatStrings.FormatTemperature(eng.TemperatureDegC, Locomotive.IsMetric, false));
            result.AppendFormat("\t{0}", FormatStrings.FormatPressure(eng.DieselOilPressurePSI, PressureUnit.PSI, Locomotive.MainPressureUnit, true));

            return result.ToString();
        }

        public int NumOfActiveEngines
        {
            get
            {
                int num = 0;
                foreach(DieselEngine eng in DEList)
                {
                    if (eng.State == DieselEngineState.Running)
                        num++;
                }
                return num;
            }
        }

        // This calculates the percent of running power. If the locomotive has two prime movers, and 
        // one is shut down then power will be reduced by the size of the prime mover
        public float RunningPowerFraction
        {
            get
            {
                float percent = 0;
                float totalMaxPower = MaxPowerW;
                foreach (DieselEngine eng in DEList)
                {
                    if (eng.State == DieselEngineState.Running)
                        percent += eng.CurrentMaximumPowerW / totalMaxPower;
                }
                return percent;
            }
        }
    }

    public class DieselEngine : ISubSystem<DieselEngine>
    {
        public enum Cooling
        {
            Undefined = -1,
            NoCooling = 0,
            Mechanical = 1,
            Hysteresis = 2,
            Proportional = 3
        }

        public int Id
        {
            get
            {
                return Locomotive.DieselEngines.DEList.IndexOf(this) + 1;
            }
        }

        #region Parameters and variables
        /// <summary>
        /// Actual change rate of the engine's RPM - useful for exhaust effects
        /// </summary>
        public float dRPM;
        /// <summary>
        /// Actual RPM of the engine
        /// </summary>
        public float RealRPM;

        /// <summary>
        /// RPM of the engine as defined by throttle setting
        /// </summary>
        public float RawRPM;

        /// <summary>
        /// RPM treshold when the engine starts to combust fuel
        /// </summary>
        public float StartingRPM = -1;

        /// <summary>
        /// RPM treshold when the engine is considered as succesfully started
        /// </summary>
        public float StartingConfirmationRPM = -1;

        /// <summary>
        /// The type of interpolation/rounding that should be used to define demanded engine RPM
        /// </summary>
        public Interpolator.RoundingMode SpeedControl = Interpolator.RoundingMode.Continuous;

        /// <summary>
        /// GearBox unit
        /// </summary>
        public GearBox GearBox;

        /// <summary>
        /// Parent locomotive
        /// </summary>
        public readonly MSTSDieselLocomotive Locomotive;

        protected MSTSGearBoxParams GearBoxParams => Locomotive.DieselEngines.MSTSGearBoxParams;

        protected Simulator Simulator => Locomotive.Simulator;

        /// <summary>
        /// Engine status
        /// </summary>
        public DieselEngineState State { get; protected set; } = DieselEngineState.Stopped;
        public bool PowerOn => State == DieselEngineState.Running || State == DieselEngineState.Starting;
        /// <summary>
        /// Type of engine cooling
        /// </summary>
        public Cooling EngineCooling = Cooling.Undefined;

        /// <summary>
        /// Holds in engine braking mode
        /// </summary>
        public bool engineBrakingLockout = false;

        /// <summary>
        /// The RPM controller tries to reach this value
        /// </summary>
        public float DemandedRPM;
        float throttleAcclerationFactor = 1.0f;

        /// <summary>
        /// Demanded throttle percent, usually taken from parent locomotive
        /// </summary>
        public float DemandedThrottlePercent { set { demandedThrottlePercent = value > 100f ? 100f : (value < 0 ? 0 : value); } get { return demandedThrottlePercent; } }
        float demandedThrottlePercent;
        /// <summary>
        /// Demanded dynamic brake percent, usually taken from parent locomotive
        /// Note: Negative value indicates dynamic brake is currently disabled
        /// </summary>
        public float DemandedDynamicsPercent { set { demandedDynamicsPercent = value > 100f ? 100f : (value < -1 ? -1 : value); } get { return demandedDynamicsPercent; } }
        float demandedDynamicsPercent;
        /// <summary>
        /// Idle RPM
        /// </summary>
        public float IdleRPM = -1;
        /// <summary>
        /// Maximal RPM
        /// </summary>
        public float MaxRPM = -1;
        /// <summary>
        /// Governor RPM - maximum speed before engine is shut down automatically
        /// </summary>
        public float GovernorRPM = -1;
        /// <summary>
        /// Difference between engine maximum and minimum RPM
        /// </summary>
        public float RPMRange;

        /// <summary>
        /// Change rate when accelerating the engine
        /// </summary>
        public float ChangeUpRPMpS = -1;
        /// <summary>
        /// Change rate when decelerating the engine
        /// </summary>
        public float ChangeDownRPMpS = -1;
        /// <summary>
        /// "Jerk" of the RPM when accelerating the engine
        /// </summary>
        public float RateOfChangeUpRPMpSS = -1;
        /// <summary>
        /// "Jerk" of the RPM when decelerating the engine
        /// </summary>
        public float RateOfChangeDownRPMpSS = -1;
        /// <summary>
        /// Engine mass moment of inertia in kg * m^2
        /// </summary>
        public float InertiaKgM2;

        /// <summary>
        /// Maximum overall rated power output of the diesel engine
        /// </summary>
        public float MaximumDieselPowerW = -1;
        /// <summary>
        /// Maximum power the diesel engine can currently output
        /// </summary>
        public float CurrentMaximumPowerW;
         /// <summary>
        /// Maximum power available to the rail
        /// </summary>
        public float MaximumRailOutputPowerW = -1;
        /// <summary>
        /// Real instantaneous total power output of the engine
        /// </summary>
        public float OutputPowerW;
        /// <summary>
        /// Instantaneous power output of the engine that's sent to the transmission
        /// </summary>
        public float TractionPowerW;
        /// <summary>
        /// Instantaneous power output of the engine that is consumed by auxiliary loads or ETS
        /// </summary>
        public float AuxiliaryPowerW;
        /// <summary>
        /// Instantaneous power capacity of the engine that's not consumed by auxiliary loads or ETS
        /// </summary>
        public float AvailablePowerW;

        /// <summary>
        /// Fuel consumed at idle, for reference
        /// </summary>
        public float DieselUsedPerHourAtIdleL = -1;
        /// <summary>
        /// Fuel consumed at full power, for reference
        /// </summary>
        public float DieselUsedPerHourAtMaxPowerL = -1;
        /// <summary>
        /// Current instantaneous fuel flow
        /// </summary>
        public float DieselFlowLpS;

        /// <summary>
        /// True if this engine provides power to the transmission
        /// </summary>
        public bool ProvidesTraction = true;
        /// <summary>
        /// True if this engine provides power to electric train supply
        /// </summary>
        public bool ProvidesETS = true;
        /// <summary>
        /// The proportion of total locomotive tractive power this engine currently provides
        /// </summary>
        public float TractionPowerProportion
        {
            get
            {
                if (ProvidesTraction)
                {
                    if (Locomotive.DieselEngines.Count > 1)
                    {
                        float availableTractionW = Locomotive.DieselEngines.AvailablePowerW;
                        if (availableTractionW > 0)
                            return AvailablePowerW / availableTractionW;
                        else
                            return 0;
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        /// <summary>
        /// The proportion of total locomotive ETS power this engine currently provides
        /// </summary>
        public float ETSPowerProportion
        {
            get
            {
                if (ProvidesETS)
                {
                    if (Locomotive.DieselEngines.Count > 1)
                    {
                        float availableETSW = Locomotive.DieselEngines.AvailableETSPowerW;
                        if (availableETSW > 0)
                            return (CurrentMaximumPowerW - AuxPowerTab[RealRPM]) / availableETSW;
                        else
                            return 0;
                    }
                    else
                    {
                        return 1.0f;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        /// <summary>
        /// Engine load table - Max output power vs. RPM
        /// </summary>
        public Interpolator DieselPowerTab;
        /// <summary>
        /// Auxiliary power table - Auxiliary power draw vs. RPM
        /// </summary>
        public Interpolator AuxPowerTab;
        /// <summary>
        /// Rail power table - Max rail output power vs. RPM
        /// </summary>
        public Interpolator RailPowerTab;
        /// <summary>
        /// Engine fuel consumption table operating at rated power - Fuel consumption vs. RPM
        /// </summary>
        public Interpolator DieselConsumptionTab;
        /// <summary>
        /// Engine throttle settings table - RPM vs. throttle settings
        /// </summary>
        public Interpolator ThrottleRPMTab;
        /// <summary>
        /// Engine dynamic brakes settings table - RPM vs. dynamic brakes settings
        /// </summary>
        public Interpolator DynamicsRPMTab;
        /// <summary>
        /// Engine throttle settings table w/ ETS - RPM vs. throttle settings when ETS is active
        /// </summary>
        public Interpolator ETSThrottleRPMTab;
        /// <summary>
        /// Engine dynamic brakes settings table w/ ETS - RPM vs. dynamic brakes settings when ETS is active
        /// </summary>
        public Interpolator ETSDynamicsRPMTab;
        /// <summary>
        /// Engine output torque table - Torque vs. RPM
        /// </summary>
        public Interpolator DieselTorqueTab;
        /// <summary>
        /// Friction torque table - Engine internal friction vs. RPM
        /// </summary>
        public Interpolator FrictionTorqueTab;

        /// <summary>
        /// Exhaust particle rate at idle RPM
        /// </summary>
        public float InitialExhaust = -1;
        /// <summary>
        /// Exhaust particle rate at max RPM
        /// </summary>
        public float MaxExhaust = -1;
        /// <summary>
        /// Current exhaust particle rate
        /// </summary>
        public float ExhaustParticles;
        /// <summary>
        /// Difference between maximum and minimum exhaust particle rate
        /// </summary>
        public float ExhaustRange;
        /// <summary>
        /// Exhaust particle lifespan at idle RPM
        /// </summary>
        public float InitialMagnitude = -1;
        /// <summary>
        /// Exhaust particle lifespan at max RPM
        /// </summary>
        public float MaxMagnitude = -1;
        /// <summary>
        /// Current exhaust particle lifespan
        /// </summary>
        public float ExhaustMagnitude;
        /// <summary>
        /// Difference between maximum and minimum exhaust particle lifespan
        /// </summary>
        public float MagnitudeRange;
        /// <summary>
        /// Multiplier for exhaust particle rate and lifespan when decreasing RPM
        /// </summary>
        public float ExhaustDecelReduction = 0.75f;
        /// <summary>
        /// Multiplier for exhaust particle rate and lifespan when increasing RPM
        /// </summary>
        public float ExhaustAccelIncrease = 2.0f;
        /// <summary>
        /// Current exhaust color
        /// </summary>
        public Color ExhaustColor;
        /// <summary>
        /// Exhaust color at steady state (no RPM change)
        /// </summary>
        public Color ExhaustSteadyColor = Color.Transparent;
        /// <summary>
        /// Exhaust color when accelerating the engine
        /// </summary>
        public Color ExhaustTransientColor = Color.Transparent;
        /// <summary>
        /// Exhaust color when decelerating the engine
        /// </summary>
        public Color ExhaustDecelColor = Color.Transparent;
        /// <summary>
        /// Exhaust color when compressor blown failure has triggered
        /// </summary>
        public Color ExhaustCompressorBlownColor = Color.Gray;

        public bool DieselEngineConfigured = false; // flag to indicate that the user has configured a diesel engine prime mover code block in the ENG file

        /// <summary>
        /// Current Engine oil pressure in PSI
        /// </summary>
        public float DieselOilPressurePSI
        {
            get
            {
                float k = (MaxOilPressurePSI - MinOilPressurePSI) / RPMRange;
                float res = MinOilPressurePSI + k * (RealRPM - IdleRPM) - dieseloilfailurePSI;
                if (res < 0f)
                    res = 0f;
                return res;
            }
        }

        /// <summary>
        /// Governor has activiated
        /// </summary>
        public bool GovernorEnabled = false;

        /// <summary>
        /// Geared Overspeed shutdown has activiated
        /// </summary>
        public bool GearOverspeedShutdownEnabled = false;

        /// <summary>
        /// Geared Underspeed shutdown has activiated
        /// </summary>
        public bool GearUnderspeedShutdownEnabled = false;

        /// <summary>
        /// Minimal oil pressure (at IdleRPM)
        /// </summary>
        public float MinOilPressurePSI = -1;
        /// <summary>
        /// Maximal oil pressure (at MaxRPM)
        /// </summary>
        public float MaxOilPressurePSI = -1;
        /// <summary>
        /// Oil failure/leakage is substracted from the DieselOilPressurePSI
        /// </summary>
        public float dieseloilfailurePSI = 0f;              //Intended to be implemented later
        /// <summary>
        /// Actual Engine temperature
        /// </summary>
        public float TemperatureDegC = 40f;
        /// <summary>
        /// Maximal engine temperature
        /// </summary>
        public float MaxTemperatureDegC = -1;
        /// <summary>
        /// Time constant to heat up from zero to 63% of MaxTemperature
        /// </summary>
        public float TempTimeConstantS = 720f;
        /// <summary>
        /// Optimal temperature of the diesel at rated power
        /// </summary>
        public float OptimalTemperatureDegC = 95f;
        /// <summary>
        /// Steady temperature when idling
        /// </summary>
        public float IdleTemperatureDegC = 75f;
        /// <summary>
        /// Hysteresis of the cooling regulator
        /// </summary>
        public float CoolingHystDegC = 5.0f;
        /// <summary>
        /// 0 to 1 value representing the amount of cooling currently used
        /// </summary>
        public float CoolingPower;

        /// <summary>
        /// Load of the engine (actual power output divided by maximum possible power output)
        /// expressed as percentage, normally 0%-100%
        /// </summary>
        public float LoadPercent
        {
            get
            {
                if (AvailablePowerW > 0)
                    return TractionPowerW / AvailablePowerW * 100.0f;
                else
                    return 0;
            }
        }
        /// <summary>
        /// The engine is connected to the gearbox
        /// </summary>
        public bool HasGearBox { get { return GearBox != null; } }
        #endregion

        public DieselEngine(MSTSDieselLocomotive locomotive)
        {
            Locomotive = locomotive;
        }

        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        public virtual void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            bool end = false;
            while (!end)
            {
                string lowercasetoken = stf.ReadItem().ToLower();
                switch (lowercasetoken)
                {
                    case "idlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "maxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "governorrpm": GovernorRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "startingrpm": StartingRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "startingconfirmrpm": StartingConfirmationRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "changeuprpmps": ChangeUpRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "changedownrpmps": ChangeDownRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "rateofchangeuprpmpss": RateOfChangeUpRPMpSS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "rateofchangedownrpmpss": RateOfChangeDownRPMpSS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "shaftinertia": InertiaKgM2 = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "speedcontrol": SpeedControl = (Interpolator.RoundingMode)Enum.Parse(typeof(Interpolator.RoundingMode), stf.ReadStringBlock(""), true); break;
                    case "maximalpower": MaximumDieselPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0); break;
                    case "providestractionpower": ProvidesTraction = stf.ReadBoolBlock(true); break;
                    case "providesetspower": ProvidesETS = stf.ReadBoolBlock(true); break;
                    case "idleexhaust": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "maxexhaust": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "idleexhaustmagnitude": InitialMagnitude = stf.ReadFloatBlock(STFReader.UNITS.Time, 0); break;
                    case "maxexhaustmagnitude": MaxMagnitude = stf.ReadFloatBlock(STFReader.UNITS.Time, 0); break;
                    case "exhaustdynamics": ExhaustAccelIncrease = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "exhaustdynamicsdown": ExhaustDecelReduction = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                    case "exhaustcolour":
                    case "exhaustcolor": ExhaustSteadyColor = stf.ReadColorBlock(Color.Gray); break;
                    case "exhausttransientcolour":
                    case "exhausttransientcolor": ExhaustTransientColor = stf.ReadColorBlock(Color.Black); break;
                    case "exhaustdecelcolour":
                    case "exhaustdecelcolor": ExhaustDecelColor = stf.ReadColorBlock(Color.WhiteSmoke); break;
                    case "dieselpowertab": DieselPowerTab = new Interpolator(stf); break;
                    case "auxiliarypowertab": AuxPowerTab = new Interpolator(stf); break;
                    case "idledieselconsumption": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Volume, 0); break;
                    case "dieselconsumptiontab": DieselConsumptionTab = new Interpolator(stf); break;
                    case "throttlerpmtab": ThrottleRPMTab = new Interpolator(stf); break;
                    case "dynamicsrpmtab": DynamicsRPMTab = new Interpolator(stf); break;
                    case "etsthrottlerpmtab": ETSThrottleRPMTab = new Interpolator(stf); break;
                    case "etsdynamicsrpmtab": ETSDynamicsRPMTab = new Interpolator(stf); break;
                    case "dieseltorquetab": DieselTorqueTab = new Interpolator(stf); break;
                    case "minoilpressure": MinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 0); break;
                    case "maxoilpressure": MaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 0); break;
                    case "maxtemperature": MaxTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 0); break;
                    case "cooling": EngineCooling = (Cooling)stf.ReadIntBlock((int)Cooling.Proportional); break;
                    case "temptimeconstant": TempTimeConstantS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0); break;
                    case "opttemperature": OptimalTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 95f); break;
                    case "idletemperature": IdleTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 75f); break;
                    default:
                        end = true;
                        break;
                }
            }
        }

        public void Copy(DieselEngine other)
        {
            IdleRPM = other.IdleRPM;
            MaxRPM = other.MaxRPM;
            GovernorRPM = other.GovernorRPM;
            StartingRPM = other.StartingRPM;
            StartingConfirmationRPM = other.StartingConfirmationRPM;
            ChangeUpRPMpS = other.ChangeUpRPMpS;
            ChangeDownRPMpS = other.ChangeDownRPMpS;
            RateOfChangeUpRPMpSS = other.RateOfChangeUpRPMpSS;
            RateOfChangeDownRPMpSS = other.RateOfChangeDownRPMpSS;
            InertiaKgM2 = other.InertiaKgM2;
            ProvidesTraction = other.ProvidesTraction;
            ProvidesETS = other.ProvidesETS;
            MaximumDieselPowerW = other.MaximumDieselPowerW;
            MaximumRailOutputPowerW = other.MaximumRailOutputPowerW;
            RailPowerTab = new Interpolator(other.RailPowerTab);
            DieselPowerTab = new Interpolator(other.DieselPowerTab);
            AuxPowerTab = new Interpolator(other.AuxPowerTab);
            DieselUsedPerHourAtIdleL = other.DieselUsedPerHourAtIdleL;
            DieselUsedPerHourAtMaxPowerL = other.DieselUsedPerHourAtMaxPowerL;
            DieselConsumptionTab = new Interpolator(other.DieselConsumptionTab);
            ThrottleRPMTab = new Interpolator(other.ThrottleRPMTab);
            DieselTorqueTab = new Interpolator(other.DieselTorqueTab);
            FrictionTorqueTab = new Interpolator(other.FrictionTorqueTab);
            // Following tables are optional, and may be null
            DynamicsRPMTab = other.DynamicsRPMTab != null ? new Interpolator(other.DynamicsRPMTab) : null;
            ETSThrottleRPMTab = other.ETSThrottleRPMTab != null ? new Interpolator(other.ETSThrottleRPMTab) : null;
            ETSDynamicsRPMTab = other.ETSDynamicsRPMTab != null ? new Interpolator(other.ETSDynamicsRPMTab) : null;
            InitialExhaust = other.InitialExhaust;
            InitialMagnitude = other.InitialMagnitude;
            MaxExhaust = other.MaxExhaust;
            MaxMagnitude = other.MaxMagnitude;
            ExhaustTransientColor = other.ExhaustTransientColor;
            ExhaustSteadyColor = other.ExhaustSteadyColor;
            ExhaustDecelColor = other.ExhaustDecelColor;
            MinOilPressurePSI = other.MinOilPressurePSI;
            MaxOilPressurePSI = other.MaxOilPressurePSI;
            ExhaustAccelIncrease = other.ExhaustAccelIncrease;
            ExhaustDecelReduction = other.ExhaustDecelReduction;
            EngineCooling = other.EngineCooling;
            TempTimeConstantS = other.TempTimeConstantS;
            IdleTemperatureDegC = other.IdleTemperatureDegC;
            OptimalTemperatureDegC = other.OptimalTemperatureDegC;
            MaxTemperatureDegC = other.MaxTemperatureDegC;
        }

        public void Initialize()
        {
            if (!Simulator.Settings.NoDieselEngineStart)
            {
                RealRPM = IdleRPM;
                RawRPM = RealRPM;
                State = DieselEngineState.Running;
            }
            RPMRange = MaxRPM - IdleRPM;
            MagnitudeRange = MaxMagnitude - InitialMagnitude;
            ExhaustRange = MaxExhaust - InitialExhaust;
            ExhaustSteadyColor.A = 10;
            ExhaustDecelColor.A = 10;
            TemperatureDegC = IdleTemperatureDegC;
            // Do not attach a gearbox to engines that do not provide traction
            if (GearBoxParams.IsInitialized && ProvidesTraction)
            {
                GearBox = new GearBox(this);
                GearBox.Initialize();
            }
        }

        public void InitializeMoving()
        {
            State = DieselEngineState.Running;
            TemperatureDegC = OptimalTemperatureDegC;

            DemandedThrottlePercent = Locomotive.ThrottlePercent;
            DemandedDynamicsPercent = Locomotive.DynamicBrakePercent;

            DemandedRPM = GetTargetRPM(float.PositiveInfinity);
            RealRPM = DemandedRPM;
            RawRPM = RealRPM;

            GearBox?.InitializeMoving();
        }

        public void Update(float elapsedClockSeconds)
        {
            if (Locomotive.DieselPowerSupply.MainPowerSupplyOn)
            {
                DemandedThrottlePercent = Locomotive.ThrottlePercent;
                DemandedDynamicsPercent = Locomotive.DynamicBrakePercent;
            }
            else
            {
                DemandedThrottlePercent = 0f;
                DemandedDynamicsPercent = 0f;
            }

            // Determine target engine RPM and update actual RPM accordingly, RPM may be changed further by other processes
            DemandedRPM = GetTargetRPM(elapsedClockSeconds);
            RealRPM = UpdateRPM(elapsedClockSeconds);

            RawRPM = RealRPM; // As RealRPM may sometimes change in the diesel mechanic configuration, this value used where the "actual" is required for calculation purposes.

            float normRPM = (RealRPM - IdleRPM) / RPMRange; // Normalized RPM, 0 at idle, 1 at max
            ExhaustParticles = InitialExhaust + (ExhaustRange * normRPM);
            ExhaustMagnitude = InitialMagnitude + (MagnitudeRange * normRPM);
            ExhaustColor = ExhaustSteadyColor;

            if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                if (State == DieselEngineState.Stopped && !HasGearBox)
                    RealRPM = 0;
                else if (HasGearBox && GearBox.IsClutchOn) // Geared engines can sometimes have the engine rotating whilst it is "stopped"
                    RealRPM = GearBox.ShaftRPM;

                if (HasGearBox)
                {
                    // links engine rpm and shaft rpm together when clutch is fully engaged
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        // When clutch is engaged then ERPM = SRPM, engine runs at train speed
                        if (RealRPM > IdleRPM && GearBox.IsClutchOn)
                            RealRPM = GearBox.ShaftRPM;

                        // prevent engine from stalling if engine speed falls below idle speed
                        var scoopActivationRPM = 1.05f * IdleRPM;
                        if (RealRPM <= IdleRPM && GearBox.ClutchType == TypesClutch.Fluid)
                        {
                            RealRPM = IdleRPM;
                            DemandedRPM = IdleRPM;
                            GearBox.clutchOn = false;
                        }
                        else if (RealRPM <= scoopActivationRPM && GearBox.ClutchType == TypesClutch.Scoop)
                        {
                            GearBox.clutchOn = false;
                        }

                        // Governor limits engine rpm
                        if (GovernorRPM != 0)
                        {
                            if ((RealRPM > MaxRPM || RealRPM < IdleRPM) && !GovernorEnabled)
                                GovernorEnabled = true;
                            else if (RealRPM > IdleRPM && RealRPM < MaxRPM && GovernorEnabled)
                                GovernorEnabled = false;
                        }
                    }

                    // If it is a geared locomotive, and rpm is greater then Max RPM, then output engine power should be reduced in HuD.
                    if (GovernorEnabled)
                    {
                        if (DemandedRPM > MaxRPM)
                        {
                            var excessRPM = DemandedRPM - MaxRPM;
                            RawRPM = MaxRPM - excessRPM;
                        }
                    }
                }
            }

            // Update power output
            // Use torque curves when operating in mechanical mode, power curves in govered mode (electric/hydraulic)
            if (HasGearBox && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                CurrentMaximumPowerW = DieselTorqueTab[RealRPM] * RPM.ToRadpS(RealRPM) * (1 - Locomotive.PowerReduction);
                // Consider rated power of gearbox
                if (GearBox.CurrentGear != null)
                    CurrentMaximumPowerW *= (GearBox.CurrentGear.TractiveForceatMaxSpeedN * GearBox.CurrentGear.MaxSpeedMpS) / (DieselTorqueTab[MaxRPM] * RPM.ToRadpS(MaxRPM));
                // Clamp power within bounds, this may produce out of bounds values
                CurrentMaximumPowerW = MathHelper.Clamp(CurrentMaximumPowerW, 0, MaximumDieselPowerW);
            }
            else
            {
                CurrentMaximumPowerW = MathHelper.Clamp(DieselPowerTab[RealRPM], 0.0f, MaximumDieselPowerW) * (1 - Locomotive.PowerReduction);
            }

            if (ProvidesETS && Locomotive.LocomotivePowerSupply != null)
                AuxiliaryPowerW = AuxPowerTab[RealRPM] + Locomotive.LocomotivePowerSupply.ElectricTrainSupplyPowerW * ETSPowerProportion;
            else
                AuxiliaryPowerW = AuxPowerTab[RealRPM];

            AvailablePowerW = State == DieselEngineState.Running ? CurrentMaximumPowerW - AuxiliaryPowerW : 0.0f;

            if (ProvidesTraction)
                TractionPowerW = Math.Max(Locomotive.LocomotiveAxles.DrivePowerW / Locomotive.TransmissionEfficiency * TractionPowerProportion, 0.0f);
            else
                TractionPowerW = 0.0f;

            OutputPowerW = State == DieselEngineState.Running ? TractionPowerW + AuxiliaryPowerW : 0.0f;

            OutputPowerW += InertiaKgM2 * RPM.ToRadpS(dRPM) * RPM.ToRadpS(RealRPM); // Power contributed by inertial torque

            if (State == DieselEngineState.Starting)
            {
                if ((RealRPM > (0.9f * StartingRPM)) && (RealRPM < StartingRPM))
                {
                    DemandedRPM = 1.1f * StartingConfirmationRPM;
                    ExhaustColor = ExhaustTransientColor;
                    ExhaustParticles = (MaxExhaust - InitialExhaust) / (0.5f * StartingRPM - StartingRPM) * (RealRPM - 0.5f * StartingRPM) + InitialExhaust;
                }
                if (RealRPM > StartingConfirmationRPM)
                    State = DieselEngineState.Running;
            }

            if ((State != DieselEngineState.Starting) && (RealRPM == 0f))
                State = DieselEngineState.Stopped;

            // fuel consumption will occur when engine is running above the starting rpm
            if (State == DieselEngineState.Stopped || ((State == DieselEngineState.Stopping || State == DieselEngineState.Starting) && RealRPM < StartingRPM))
            {
                ExhaustParticles = 0;
                DieselFlowLpS = 0;
            }
            else if (DieselConsumptionTab != null)
            {
                // Adjust diesel consumption per actual power output
                float frictionPower = FrictionTorqueTab[RealRPM] * RPM.ToRadpS(RealRPM);
                float powerRatio = (OutputPowerW + frictionPower) / (DieselPowerTab[RealRPM] + frictionPower);
                DieselFlowLpS = powerRatio * pS.FrompH(DieselConsumptionTab[RealRPM]);
                if (DieselFlowLpS < 0) // Interpolator may produce negative values in some configurations
                    DieselFlowLpS = 0;
            }

            if (Locomotive.PowerReduction == 1 && State != DieselEngineState.Stopped)     // Compressor blown, you get much smoke 
            {
                ExhaustColor = Color.WhiteSmoke;
                ExhaustParticles = 40f;
                ExhaustMagnitude = InitialMagnitude * 2;
            }

            UpdateCoolingSystem(elapsedClockSeconds);

            // Update the selected gear
            if (GearBox != null)
            {
                if (Locomotive.IsLeadLocomotive() || Locomotive.Train.HasControlCarWithGear)
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (Locomotive.GearBoxController.CurrentNotch > 0)
                            GearBox.NextGear = GearBox.Gears[Locomotive.GearBoxController.CurrentNotch - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                else
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (Locomotive.GearboxGearIndex > 0)

                            GearBox.NextGear = GearBox.Gears[Locomotive.GearboxGearIndex - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }

                GearBox.Update(elapsedClockSeconds);
            }
        }

        /// <summary>
        /// Determines the current RPM demanded by the engine governor, considering throttle setting, dynamic brake
        /// setting, ETS state, and custom behaviors.
        /// </summary>
        /// <param name="elapsedClockSeconds">Simulation time delta</param>
        /// <returns>The current target RPM value, limited between IdleRPM and MaxRPM</returns>
        public float GetTargetRPM(float elapsedClockSeconds)
        {
            float targetRPM = 0f;

            if (State == DieselEngineState.Running)
            {
                // Priority:
                // Use ETS tables if available and ETS is active
                // Use dynamic brake table if available, locomotive has dynamic braking, and dynamic braking is active
                // Else use throttle table
                bool etsEnabled = Locomotive.DieselPowerSupply?.ElectricTrainSupplyOn ?? false;
                bool dynamicBraking = demandedDynamicsPercent >= 0 && Locomotive.DynamicBrakeAvailable;

                if (etsEnabled && dynamicBraking && ETSDynamicsRPMTab != null)
                    targetRPM = ETSDynamicsRPMTab[demandedDynamicsPercent];
                else if (etsEnabled && ETSThrottleRPMTab != null)
                    targetRPM = ETSThrottleRPMTab[demandedThrottlePercent];
                else if (dynamicBraking && DynamicsRPMTab != null)
                    targetRPM = DynamicsRPMTab[demandedDynamicsPercent];
                else if (ThrottleRPMTab != null)
                    targetRPM = ThrottleRPMTab[demandedThrottlePercent];

                // Governor should never attempt to set an RPM outside the engine's rated min/max RPM
                targetRPM = MathHelper.Clamp(targetRPM, IdleRPM, MaxRPM);
            }

            // TODO: Add processing for custom engine RPM overrides

            if (GearBox != null)
                targetRPM = GetGearboxRPM(elapsedClockSeconds, targetRPM);

            return targetRPM;
        }

        /// <summary>
        /// Determines the current RPM demanded by the engine gearbox (assuming one is present),
        /// and updates some gearbox behavior
        /// </summary>
        /// <param name="elapsedClockSeconds">Simulation time delta</param>
        /// <param name="targetRPM">Current target RPM before updating gearbox</param>
        /// <returns>The current target RPM value as driven by the gearbox</returns>
        public float GetGearboxRPM(float elapsedClockSeconds, float targetRPM)
        {
            if (Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                if (GearBox.GearBoxOperation == GearBoxOperation.Automatic)
                {
                    if (RealRPM > 0)
                        GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                    else
                        GearBox.ClutchPercent = 100f;

                    // When clutch is engaged (true) engine rpm should follow wheel shaft speed
                    if (GearBox.IsClutchOn)
                        targetRPM = GearBox.ShaftRPM;
                }
                else
                {
                    if (GearBox.ManualGearChange && !GearBox.ManualGearBoxChangeOn) // Initially set gear change 
                    {
                        GearBox.ManualGearBoxChangeOn = true;
                    }
                    else if (GearBox.GearBoxType == TypesGearBox.B && GearBox.ManualGearBoxChangeOn && GearBox.ManualGearTimerS < GearBox.ManualGearTimerResetS)
                    {
                        GearBox.ManualGearTimerS += elapsedClockSeconds; // Increment timer
                    }
                    else if (GearBox.GearBoxType == TypesGearBox.B && GearBox.ManualGearBoxChangeOn && GearBox.ManualGearTimerS > GearBox.ManualGearTimerResetS)
                    {
                        // Reset gear change in preparation for the next gear change
                        GearBox.ManualGearBoxChangeOn = false;
                        GearBox.ManualGearChange = false;
                        GearBox.ManualGearTimerS = 0; // Reset timer
                    }

                    if (RealRPM > 0)
                        GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                    else
                        GearBox.ClutchPercent = 100f;

                    if (GearBox.CurrentGear != null && !GearBox.ManualGearBoxChangeOn)
                    {
                        // When clutch is engaged (true) engine rpm should follow wheel shaft speed
                        if (GearBox.IsClutchOn)
                        {
                            if (GearBox.ClutchType == TypesClutch.Friction)
                                targetRPM = GearBox.ShaftRPM;
                            else if (demandedThrottlePercent > 0)
                                targetRPM = GearBox.ShaftRPM;
                        }
                    }
                    else if (GearBox.ManualGearBoxChangeOn)
                    {
                        engineBrakingLockout = true;

                        // once engine speed is less then shaft speed reset gear change, or is at idle rpm, reset gear change
                        if ((RealRPM <= GearBox.ShaftRPM && GearBox.ShaftRPM < MaxRPM) || RealRPM == IdleRPM)
                        {
                            GearBox.ManualGearChange = false;
                            GearBox.ManualGearBoxChangeOn = false;
                        }
                    }
                }

                if (demandedThrottlePercent < GearBox.previousGearThrottleSetting)
                    GearBox.GearedThrottleDecrease = true;

                // Determine when freewheeling should occur
                if (GearBox.GearBoxFreeWheelFitted)
                {
                    if (GearBox.GearedThrottleDecrease && GearBox.ShaftRPM > ThrottleRPMTab[demandedThrottlePercent] || GearBox.ShaftRPM > GovernorRPM)
                    {
                        // GearBox.clutchOn = false;
                        GearBox.GearBoxFreeWheelEnabled = true;
                    }
                    else if (GearBox.ShaftRPM < ThrottleRPMTab[demandedThrottlePercent] && GearBox.ShaftRPM < GovernorRPM)
                    {
                        GearBox.GearBoxFreeWheelEnabled = false;
                        GearBox.GearedThrottleDecrease = false;
                    }
                }

                GearBox.previousGearThrottleSetting = demandedThrottlePercent;

                // Engine with no loading wll tend to speed up if throttle is open, similarly for situation where freewheeling is occurring
                // the following is an approximation to calculate rpm speed that motor can achieve when operating at no load - will increase until torque curve 
                // can no longer overcome auxiliary functions connected to engine
                if (GearBox.GearBoxFreeWheelEnabled || GearBox.CurrentGear == null)
                {
                    var tempthrottle = demandedThrottlePercent / 100.0f;

                    if (tempthrottle >= 0.5)
                        targetRPM = MaxRPM;
                    else if (tempthrottle < 0.5 && tempthrottle > 0)
                        targetRPM = (2.0f * tempthrottle * (MaxRPM - IdleRPM)) + IdleRPM;

                    throttleAcclerationFactor = (1.0f + tempthrottle) * 4.0f;
                }
                else if (!GearBox.IsClutchOn)
                {
                    // When clutch is slipping, engine rpm will increase initially quickly (whilst clutch under no load) until clutch starts to engage, and then slow down as clutch engages.
                    var tempClutchFraction = GearBox.ClutchPercent / 100.0f; // 100% = clutch slipping, 0% = clutch engaged
                    tempClutchFraction = MathHelper.Clamp(tempClutchFraction, 0.1f, 1.0f);  // maintain a value between 0.1 (never want throttle increase value to be zero) and 1.0
                    throttleAcclerationFactor = 1.0f + tempClutchFraction; // decreases as clutch engages, thus when clutch disengaged engine rpm change high, clutch engaged, engine rpm low

                    // Whilst clutch slipping use a similar approach as above to set RPM for "unloaded" engine.
                    var tempthrottle = demandedThrottlePercent / 100.0f;
                    if (tempthrottle >= 0.5)
                        targetRPM = MaxRPM;
                    else if (tempthrottle < 0.5 && tempthrottle > 0)
                        targetRPM = (2.0f * tempthrottle * (MaxRPM - IdleRPM)) + IdleRPM;
                }
                else
                {
                    // under "normal" circumstances
                    throttleAcclerationFactor = 1.0f;
                }

                // brakes engine when doing gear change
                // During a manual gear change brake engine shaft speed to match wheel shaft speed
                if (engineBrakingLockout && RealRPM > GearBox.ShaftRPM && RealRPM > IdleRPM)
                    targetRPM = IdleRPM;
                else if ((engineBrakingLockout && RealRPM < GearBox.ShaftRPM) || RealRPM <= IdleRPM || Locomotive.AbsSpeedMpS < 0.1f)
                    engineBrakingLockout = false;

                // Speeds engine rpm to simulate clutch starting to engage and pulling speed up as clutch slips for friction clutch
                var clutchEngagementBandwidthRPM = 10.0f;
                if (!GearBox.GearBoxFreeWheelEnabled && GearBox.CurrentGear != null && GearBox.ClutchType == TypesClutch.Friction && !GearBox.IsClutchOn &&
                    (GearBox.ShaftRPM < RealRPM - clutchEngagementBandwidthRPM || GearBox.ShaftRPM > RealRPM + clutchEngagementBandwidthRPM) && Locomotive.AbsSpeedMpS > 0.1 &&
                    !GearBox.ManualGearBoxChangeOn && demandedThrottlePercent == 0)
                    targetRPM = GearBox.ShaftRPM;

                // Simulate stalled engine if RPM decreases too far below IdleRPM
                if (RealRPM < 0.9f * IdleRPM && State == DieselEngineState.Running && GearBox.IsClutchOn)
                {
                    GearUnderspeedShutdownEnabled = true;
                    Trace.TraceInformation("Diesel Engine has stalled due to underspeed.");
                    HandleEvent(PowerSupplyEvent.StallEngine);
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Diesel Engine has stalled due to underspeed."));

                    if (GearBox.ClutchType == TypesClutch.Fluid || GearBox.ClutchType == TypesClutch.Scoop)
                        GearBox.clutchOn = false;
                }
                else if (Locomotive.AbsSpeedMpS < 0.05)
                {
                    GearUnderspeedShutdownEnabled = false;
                }

                // Simulate stalled engine if RPM increases too far and exceed the safe overrun speed, by stopping engine
                if (RealRPM > GovernorRPM && State == DieselEngineState.Running && GearBox.IsClutchOn)
                {
                    GearOverspeedShutdownEnabled = true;
                    Trace.TraceInformation("Diesel Engine has stalled due to overspeed.");
                    HandleEvent(PowerSupplyEvent.StallEngine);
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Diesel Engine has stalled due to overspeed."));

                    if (GearBox.ClutchType == TypesClutch.Fluid || GearBox.ClutchType == TypesClutch.Scoop)
                        GearBox.clutchOn = false;
                }
                else if (Locomotive.AbsSpeedMpS < 0.05 && State == DieselEngineState.Stopped)
                {
                    GearOverspeedShutdownEnabled = false;
                }

                // In event of over or underspeed shutdown of fluid or scoop coupling drive ERPM to 0.
                if ((GearOverspeedShutdownEnabled || GearUnderspeedShutdownEnabled) && (GearBox.ClutchType == TypesClutch.Fluid || GearBox.ClutchType == TypesClutch.Scoop))
                    targetRPM = 0;
            }
            else   // Legacy or MSTS Gearboxes
            {
                if (RealRPM > 0)
                    GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                else
                    GearBox.ClutchPercent = 100f;

                if (GearBox.CurrentGear != null)
                {
                    // Maintain Shaft RPM and Engine RPM equal when clutch is on
                    if (GearBox.IsClutchOn)
                        targetRPM = GearBox.ShaftRPM;
                }
            }

            return targetRPM;
        }

        /// <summary>
        /// Updates the actual RPM of the engine based on the current RPM, the current
        /// rate of change of RPM, and the target RPM value
        /// </summary>
        /// <param name="elapsedClockSeconds"></param>
        /// <returns></returns>
        public float UpdateRPM(float elapsedClockSeconds)
        {
            if (RealRPM < DemandedRPM)
            {
                float maxJerk = RateOfChangeUpRPMpSS * throttleAcclerationFactor;
                float maxInstantJerk = maxJerk * elapsedClockSeconds;

                // RPM increase exponentially decays, but clamped between 1% and 100% of the linear rate of change
                float targetAcceleration = MathHelper.Clamp((float)Math.Sqrt(2 * maxJerk * (DemandedRPM - RealRPM)), 0.01f * ChangeUpRPMpS, ChangeUpRPMpS);
                dRPM = MathHelper.Clamp(targetAcceleration, dRPM - maxInstantJerk * 1.25f, dRPM + maxInstantJerk);

                if (RealRPM + dRPM * elapsedClockSeconds > DemandedRPM)
                {
                    dRPM = (DemandedRPM - RealRPM) / elapsedClockSeconds;
                    return DemandedRPM;
                }
                else if (dRPM > 0.25f * ChangeUpRPMpS) // Only change particle emitter if RPM is still increasing substantially
                {
                    ExhaustParticles *= ExhaustAccelIncrease;
                    ExhaustMagnitude *= ExhaustAccelIncrease;
                    ExhaustColor = ExhaustTransientColor;
                }
            }
            else if (RealRPM > DemandedRPM)
            {
                float maxJerk = RateOfChangeDownRPMpSS * throttleAcclerationFactor;
                float maxInstantJerk = maxJerk * elapsedClockSeconds;

                // RPM decrease exponentially decays, but clamped between 1% and 100% of the linear rate of change
                float targetAcceleration = -MathHelper.Clamp((float)Math.Sqrt(2 * maxJerk * (RealRPM - DemandedRPM)), 0.01f * ChangeDownRPMpS, ChangeDownRPMpS);
                dRPM = MathHelper.Clamp(targetAcceleration, dRPM - maxInstantJerk, dRPM + maxInstantJerk * 1.25f);

                if (RealRPM + dRPM * elapsedClockSeconds < DemandedRPM)
                {
                    dRPM = (DemandedRPM - RealRPM) / elapsedClockSeconds;
                    return DemandedRPM;
                }
                else if (dRPM < -0.25f * ChangeDownRPMpS) // Only change particle emitter if RPM is still decreasing substantially
                {
                    ExhaustParticles *= ExhaustDecelReduction;
                    ExhaustMagnitude *= ExhaustDecelReduction;
                    ExhaustColor = ExhaustDecelColor;
                }
            }
            else
            {
                dRPM = 0;
                return DemandedRPM;
            }

            return Math.Max(RealRPM + dRPM * elapsedClockSeconds, 0);
        }

        /// <summary>
        /// Updates the engine cooling system and thermal simulation variables
        /// </summary>
        /// <param name="elapsedClockSeconds">Simulation time delta</param>
        public void UpdateCoolingSystem(float elapsedClockSeconds)
        {
            // Estimate heat using fuel consumption; more fuel used = more heat
            // Measured relative to maximum fuel consumption
            float currentHeatLoad = 0.0f;
            float idleHeatLoad = 0.0f;
            if (DieselUsedPerHourAtMaxPowerL > 0)
            {
                currentHeatLoad = pS.TopH(DieselFlowLpS) / DieselUsedPerHourAtMaxPowerL;
                idleHeatLoad = DieselUsedPerHourAtIdleL / DieselUsedPerHourAtMaxPowerL;
            }

            float thermalTimeDelta = (MaxTemperatureDegC - IdleTemperatureDegC) / TempTimeConstantS * elapsedClockSeconds;
            TemperatureDegC += thermalTimeDelta * currentHeatLoad;

            // FUTURE: Allow user to define entirely custom cooling systems, instead of the 4 canned types
            switch (EngineCooling)
            {
                case Cooling.NoCooling: // Passive cooling: Assume engine naturally radiates sufficient heat at MaxTemperature
                    CoolingPower = 1.0f;
                    break;
                case Cooling.Mechanical: // Mechanical cooling: Assume engine cooling is always on and varies with RPM
                    CoolingPower = MathHelper.Clamp((RealRPM - IdleRPM) / RPMRange, 0.0f, 1.0f);
                    break;
                case Cooling.Hysteresis: // Hysteresis cooling: Assume engine cooling is either fully on or fully off
                    if (CoolingPower != 1.0f && TemperatureDegC > OptimalTemperatureDegC)
                        CoolingPower = 1.0f;
                    else if (CoolingPower != 0.0f && TemperatureDegC < (OptimalTemperatureDegC - CoolingHystDegC))
                        CoolingPower = 0.0f;
                    break;
                default:
                case Cooling.Proportional: // Proportional cooling: Assume cooling power dynamically changes with temperature
                    float increasingCoolingDemand = (TemperatureDegC - OptimalTemperatureDegC) / CoolingHystDegC;
                    // As temperature increases from OptimalTemperature to OptimalTemperature + CoolingHyst, cooling increases from 0 to 1
                    // As temperature decreases from OptimalTemperature to OptimalTemperature - CoolingHyst, cooling decreases from 1 to 0
                    // In-between, cooling remains at whatever value it was previously, giving some hysteresis to prevent excessive cycling
                    CoolingPower = MathHelper.Clamp(MathHelper.Clamp(CoolingPower, increasingCoolingDemand, increasingCoolingDemand + 1.0f), 0.0f, 1.0f);
                    break;
            }

            float currentDeltaDegC = TemperatureDegC - Locomotive.CarOutsideTempC;
            // FUTURE: Allow user to define max safe ambient temperature, for now assume 40C / 104F
            float maxCoolingCoefficient = currentDeltaDegC / (MaxTemperatureDegC - 40.0f);
            // FUTURE: Allow user to define min safe ambient temperature, for now assume -20C / -4F
            float idleCoolingCoefficient = idleHeatLoad * currentDeltaDegC / (IdleTemperatureDegC - -20.0f);

            TemperatureDegC -= thermalTimeDelta * MathHelper.Lerp(idleCoolingCoefficient, maxCoolingCoefficient, CoolingPower);
            // Limit temperature within sensible bounds, engine failure due to extreme temperature is not modeled
            TemperatureDegC = MathHelper.Clamp(TemperatureDegC, IdleTemperatureDegC - 20.0f, MaxTemperatureDegC + 10.0f);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.StopEngine:
                    if (State != DieselEngineState.Stopped)
                    {
                        DemandedRPM = 0;
                        State = DieselEngineState.Stopping;
                        if (RealRPM <= 0)
                            State = DieselEngineState.Stopped;
                    }
                    break;
                case PowerSupplyEvent.StartEngine:
                    if (HasGearBox && Locomotive.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
                    {
                        if ((State == DieselEngineState.Stopped || State == DieselEngineState.Stopping) && GearBox.CurrentGear == null && Locomotive.Direction == Direction.N)
                        {
                            DemandedRPM = StartingRPM;
                            State = DieselEngineState.Starting;
                        }
                    }
                    else
                    {
                        if (State == DieselEngineState.Stopped || State == DieselEngineState.Stopping)
                        {
                            DemandedRPM = StartingRPM;
                            State = DieselEngineState.Starting;
                        }
                    }
                    break;
                case PowerSupplyEvent.StallEngine:
                    if (State == DieselEngineState.Running)
                    {
                        // If clutch is on when engine stalls, then maintain train speed on the engine
                        if (HasGearBox && GearBox.IsClutchOn)
                            DemandedRPM = GearBox.ShaftRPM;
                        else
                            DemandedRPM = 0;

                        State = DieselEngineState.Stopped;
                    }
                    break;
            }
        }

        public void Restore(BinaryReader inf)
        {
            State = (DieselEngineState)inf.ReadInt32();
            RealRPM = inf.ReadSingle();
            OutputPowerW = inf.ReadSingle();
            TemperatureDegC = inf.ReadSingle();
            CoolingPower = inf.ReadSingle();
            GovernorEnabled = inf.ReadBoolean();
            GearBox?.Restore(inf);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)State);
            outf.Write(RealRPM);
            outf.Write(OutputPowerW);
            outf.Write(TemperatureDegC);
            outf.Write(CoolingPower);
            outf.Write(GovernorEnabled);
            GearBox?.Save(outf);
        }

        /// <summary>
        /// Ensures all required diesel engine data is present, replacing missing values with data from the locomotive
        /// or from predefined defaults in order to ensure the engine can function. For MSTS locomotives, this
        /// creates an entire engine definition. For ORTS locomotives, this adds any missing data.
        /// Error code has been provided that will provide the user with an indication if a parameter was missing.
        /// </summary>
        public void EstablishParameters()
        {
            SetValueFromLoco(ref IdleRPM, ref Locomotive.IdleRPM, "IdleRPM", true, 300.0f);
            SetValueFromLoco(ref MaxRPM, ref Locomotive.MaxRPM, "MaxRPM", true, 600.0f);

            // Undertake a test to ensure that MaxRPM > IdleRPM by a factor of 1.5x
            if (!DieselEngineConfigured && MaxRPM / IdleRPM < 1.5)
            {
                MaxRPM = IdleRPM * 1.5f;
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("MaxRPM < IdleRPM x 1.5, set MaxRPM at arbitrary value = {0}", MaxRPM);
            }

            SetValueFromLoco(ref GovernorRPM, ref Locomotive.GovernorRPM, "GovernorRPM", true, MaxRPM * 1.309f);

            // Set RPM variables inside locomotive if still missing
            if (Locomotive.IdleRPM <= 0)
                Locomotive.IdleRPM = IdleRPM;
            if (Locomotive.MaxRPM <= 0)
                Locomotive.MaxRPM = MaxRPM;
            if (Locomotive.GovernorRPM <= 0)
                Locomotive.GovernorRPM = GovernorRPM;

            SetDefaultValue(ref StartingRPM, IdleRPM * 2.0f / 3.0f, "StartingRPM");
            SetDefaultValue(ref StartingConfirmationRPM, IdleRPM * 1.1f, "StartingConfirmRPM");

            SetValueFromLoco(ref InitialExhaust, ref Locomotive.InitialExhaust, "IdleExhaust");
            SetValueFromLoco(ref MaxExhaust, ref Locomotive.MaxExhaust, "MaxExhaust");

            SetValueFromLoco(ref InitialMagnitude, ref Locomotive.InitialMagnitude, "IdleExhaustMagnitude");
            SetValueFromLoco(ref MaxMagnitude, ref Locomotive.MaxMagnitude, "MaxExhaustMagnitude");

            if (ExhaustSteadyColor == Color.Transparent)
            {
                ExhaustSteadyColor = Locomotive.ExhaustSteadyColor;
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustColor not found in Diesel Engine Config, set to default value = {0}", ExhaustSteadyColor);
            }

            if (ExhaustTransientColor == Color.Transparent)
            {
                ExhaustTransientColor = Locomotive.ExhaustTransientColor;
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustTransientColor not found in Diesel Engine Config, set to default value = {0}", ExhaustTransientColor);
            }

            if (ExhaustDecelColor == Color.Transparent)
            {
                ExhaustDecelColor = Locomotive.ExhaustDecelColor;
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustDecelColor not found in Diesel Engine Config, set to default value = {0}", ExhaustDecelColor);
            }

            SetValueFromLoco(ref ChangeUpRPMpS, ref Locomotive.MaxRPMChangeRate, "ChangeUpRPMpS", true, 40.0f);
            SetValueFromLoco(ref ChangeDownRPMpS, ref Locomotive.MaxRPMChangeRate, "ChangeDownRPMpS", true, 40.0f);

            SetDefaultValue(ref RateOfChangeUpRPMpSS, ChangeUpRPMpS, "RateOfChangeUpRPMpS");
            SetDefaultValue(ref RateOfChangeDownRPMpSS, ChangeDownRPMpS, "RateOfChangeDownRPMpS");

            SetValueFromLoco(ref MinOilPressurePSI, ref Locomotive.DieselMinOilPressurePSI, "MinOilPressure", true, 40.0f);
            SetValueFromLoco(ref MaxOilPressurePSI, ref Locomotive.DieselMaxOilPressurePSI, "MaxOilPressure", true, 120.0f);

            SetValueFromLoco(ref MaxTemperatureDegC, ref Locomotive.DieselMaxTemperatureDegC, "MaxTemperature", true, 100.0f);

            if (EngineCooling == Cooling.Undefined)
            {
                EngineCooling = Locomotive.DieselEngineCooling;
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Cooling not found in Diesel Engine Config, set to default value = {0}", EngineCooling);
            }

            SetDefaultValue(ref TempTimeConstantS, 720f, "TempTimeConstant");

            if (ThrottleRPMTab == null)
            {
                ThrottleRPMTab = new Interpolator(new float[] { 0, 100 }, new float[] { IdleRPM, MaxRPM });
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ThrottleRPMTab not found in Diesel Engine Config, set to default values");
            }
            // Set rounding mode for RPM tables to the engine's rounding mode
            ThrottleRPMTab.XRounding = SpeedControl;
            if (DynamicsRPMTab != null)
                DynamicsRPMTab.XRounding = SpeedControl;
            if (ETSThrottleRPMTab != null)
                ETSThrottleRPMTab.XRounding = SpeedControl;
            if (ETSDynamicsRPMTab != null)
                ETSDynamicsRPMTab.XRounding = SpeedControl;

            SetValueFromLoco(ref MaximumDieselPowerW, ref Locomotive.MaximumDieselEnginePowerW, "MaximalPower", false, Locomotive.MaxPowerW);
            SetDefaultValue(ref MaximumDieselPowerW, 2500000, "MaxPower"); // If engine power still couldn't be found using Locomotive.MaxPower, assume 2.5 MW

            // If diesel power output curves not defined then set to "standard defaults" in ENG file
            // Set defaults for Torque and Power tables if both are not set.
            if (DieselTorqueTab == null & DieselPowerTab == null)
            {
                float[] torque = new float[] { 0.0f, 0.2f, 0.4f, 0.7f, 0.95f, 1f, 1f, 0.98f, 0.95f, 0.9f, 0.86f, 0.81f, 0.3f };
                float[] power = new float[torque.Length];
                float[] rpm = new float[torque.Length];

                rpm[0] = 0.0f;
                // Assumption: Engine achieves max power at exactly max RPM, and torque at this point is 81% of max torque
                float maxTorque = (MaximumDieselPowerW / RPM.ToRadpS(MaxRPM)) / 0.81f;

                int count = torque.Length;
                for (int i = 1; i < count - 1; i++)
                {
                    if (i == 1)
                        rpm[i] = IdleRPM;
                    else
                        rpm[i] = rpm[i - 1] + (MaxRPM - IdleRPM) / (count - 3);
                }
                rpm[count - 1] = MaxRPM * 1.35f;

                for (int i = 0; i < count; i++)
                {
                    torque[i] *= maxTorque;
                    power[i] = torque[i] * RPM.ToRadpS(rpm[i]);
                }

                DieselPowerTab = new Interpolator(rpm, power);
                DieselTorqueTab = new Interpolator(rpm, torque);

                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                {
                    if (DieselEngineConfigured)
                    {
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                    else
                    {
                        Trace.TraceInformation("DieselPowerTab constructed from default values (BASIC Config)");
                        Trace.TraceInformation("DieselTorqueTab constructed from default values (BASIC Config)");
                    }
                }
            }

            // Set defaults for Torque table if it is not set, using Power table.
            if (DieselTorqueTab == null && DieselPowerTab != null)
            {
                int points = DieselPowerTab.GetSize();
                float[] rpm = DieselPowerTab.X;
                float[] torque = new float[points];
                for (int i = 0; i < points; i++)
                {
                    torque[i] = DieselPowerTab[rpm[i]] / RPM.ToRadpS(rpm[i]);
                }
                DieselTorqueTab = new Interpolator(rpm, torque);

                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                {
                    if (DieselEngineConfigured)
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from other data");
                    else
                        Trace.TraceInformation("DieselTorqueTab constructed from other data (BASIC Config)");
                }
            }

            // Set defaults for Power table if it is not set, using Torque table.
            if (DieselPowerTab == null && DieselTorqueTab != null)
            {
                int points = DieselTorqueTab.GetSize();
                float[] rpm = DieselTorqueTab.X;
                float[] power = new float[points];
                for (int i = 0; i < points; i++)
                {
                    power[i] = DieselTorqueTab[rpm[i]] * RPM.ToRadpS(rpm[i]);
                }
                DieselPowerTab = new Interpolator(rpm, power);

                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                {
                    if (DieselEngineConfigured)
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from other data");
                    else
                        Trace.TraceInformation("DieselPowerTab constructed from other data (BASIC Config)");
                }
            }

            if (Locomotive.MaximumDieselEnginePowerW == 0 && DieselPowerTab != null)
            {
                Locomotive.MaximumDieselEnginePowerW = DieselPowerTab[MaxRPM];
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Maximum Diesel Engine Prime Mover Power set by DieselPowerTab {0} value", FormatStrings.FormatPower(DieselPowerTab[MaxRPM], Locomotive.IsMetric, false, false));
            }

            // Set idle fuel use reference if it is not set (for more accurate estimation of low-load fuel use)
            if (DieselUsedPerHourAtIdleL < 0)
            {
                if (DieselConsumptionTab != null)
                    DieselUsedPerHourAtIdleL = DieselConsumptionTab[IdleRPM];
                else
                    DieselUsedPerHourAtIdleL = Locomotive.DieselUsedPerHourAtIdleL;
            }

            // Set max fuel use reference if it is not set
            if (DieselUsedPerHourAtMaxPowerL < 0)
            {
                if (DieselConsumptionTab != null)
                    DieselUsedPerHourAtMaxPowerL = DieselConsumptionTab.MaxY();
                else
                    DieselUsedPerHourAtMaxPowerL = Locomotive.DieselUsedPerHourAtMaxPowerL;
            }
        }

        /// <summary>
        /// If required, determines the value of a diesel engine parameter based on the equivalent parameter from the locomotive,
        /// logging as needed to indicate if the parameter had a missing value. Optionally, a default value can be provided in
        /// case the parameter value is missing from the locomotive as well.
        /// </summary>
        /// <param name="engineVar">A reference to the diesel engine variable that needs to be checked for a validity</param>
        /// <param name="locoVar">A reference to the locomotive variable providing valid data for the <paramref name="engineVar"/></param>
        /// <param name="name">The name of the parameter the user should have entered to define this value, used for logging</param>
        /// <param name="logBasic">Should any logging output be sent for BASIC engines?</param>
        /// <param name="defaultValue">The (optional) value <paramref name="engineVar"/> should be set to if no value can be found</param>
        public void SetValueFromLoco(ref float engineVar, ref float locoVar, string name, bool logBasic = false, float defaultValue = -1)
        {
            // In a diesel engine, uninitialized floating point variables are set to -1
            if (engineVar < 0)
            {
                // Data is missing from the diesel engine, but data is present in the locomotive (or, we have no default to fall back to)
                // Use the value inside the locomotive for the engine
                if (locoVar > 0 || defaultValue < 0)
                {
                    engineVar = locoVar;
                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages) // Log messages if user has requested logging
                    {
                        if (DieselEngineConfigured) // Different logging for user-defined engines
                            Trace.TraceInformation("{0} not found in Diesel Engine Config{1}: set to default value = {2}", name, logBasic ? " (ADVANCED config)" : "", engineVar);
                        else if (logBasic) // Different logging for MSTS-defined engines
                            Trace.TraceInformation("{0} (BASIC config): set to default value = {1}", name, engineVar);
                    }
                }
                else // Data is missing from the diesel engine and from the locomotive, use the default value
                {
                    engineVar = defaultValue;
                    locoVar = engineVar;
                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages) // Log messages if user has requested logging
                    {
                        if (DieselEngineConfigured) // Different logging for user-defined engines
                            Trace.TraceInformation("{0} not found in Diesel Engine Config{1}: set to arbitrary value = {2}", name, logBasic ? " (ADVANCED config)" : "", engineVar);
                        else if (logBasic) // Different logging for MSTS-defined engines
                            Trace.TraceInformation("{0} (BASIC config): set to arbitrary value = {1}", name, engineVar);
                    }
                }
            }
        }

        /// <summary>
        /// Sets a diesel engine parameter to a given default value if that parameter has not been defined yet,
        /// logging as needed to indicate if the parameter had a missing value.
        /// </summary>
        /// <param name="engineVar">A reference to the diesel engine variable that needs to be checked for a validity</param>
        /// <param name="defaultValue">The value <paramref name="engineVar"/> should be set to if it has not been set yet</param>
        /// <param name="name">The name of the parameter the user should have entered to define this value, used for logging</param>
        /// <param name="logBasic">Should any logging output be sent for BASIC engines?</param>
        public void SetDefaultValue(ref float engineVar, float defaultValue, string name, bool logBasic = false)
        {
            // In a diesel engine, uninitialized floating point variables are set to -1
            if (engineVar < 0)
            {
                engineVar = defaultValue;
                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages) // Log messages if user has requested logging
                {
                    if (DieselEngineConfigured) // Different logging for user-defined engines
                        Trace.TraceInformation("{0} not found in Diesel Engine Config{1}: set to default value = {2}", name, logBasic ? " (ADVANCED config)" : "", engineVar);
                    else if (logBasic) // Different logging for MSTS-defined engines
                        Trace.TraceInformation("{0} (BASIC config): set to default value = {1}", name, engineVar);
                }
            }
        }


        /// <summary>
        /// Initializes diesel engine parameters that rely on knowing the performance of other diesel engines
        /// (if there are any others), particularly how much power at rail each engine is responsible for.
        /// </summary>
        /// <param name="totalTractionPower">Total power available for traction from all engines</param>
        /// <param name="totalETSPower">Total power available for electric train supply from all engines</param>
        public void InitRailPower(float totalTractionPower, float totalETSPower)
        {
            float maxTractionPowerProportion = MaximumDieselPowerW / totalTractionPower;
            float maxETSPowerProportion = MaximumDieselPowerW / totalETSPower;

            // Set rail power parameters if not already set
            (float, float)[] throttleRailPower; // Pairs of throttle settings and corresponding rail power
            if (Locomotive.TractiveForceCurves != null)
            {
                // Determine the max rail power applied from each tractive force curve
                int size = Locomotive.TractiveForceCurves.Size;
                throttleRailPower = new (float, float)[size];

                for (int i = 0; i < size; i++)
                {
                    float curveMaxPower = 0;
                    Interpolator forceCurve = Locomotive.TractiveForceCurves.Y[i];

                    for (int j = 0; j < forceCurve.GetSize(); j++)
                    {
                        float pointPower = forceCurve.X[j] * forceCurve.Y[j];

                        if (pointPower > curveMaxPower)
                            curveMaxPower = pointPower;
                    }
                    throttleRailPower[i] = (Locomotive.TractiveForceCurves.X[i], curveMaxPower * maxTractionPowerProportion);
                }
                // Set max rail power from force curves
                if (MaximumRailOutputPowerW < 0)
                {
                    MaximumRailOutputPowerW = throttleRailPower.Max(throttlePower => throttlePower.Item2);
                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Rail Output Power set by Diesel Traction Curves {0} value", FormatStrings.FormatPower(MaximumRailOutputPowerW, Locomotive.IsMetric, false, false));
                }
            }
            else
            {
                if (MaximumRailOutputPowerW < 0)
                {
                    if (Locomotive.MaxPowerW > 0) // set rail power to a default value on the basis that of the value specified in the MaxPowerW parameter
                        MaximumRailOutputPowerW = Locomotive.MaxPowerW;
                    else // Set rail power to a default value on the basis that it is about 80% of the prime mover output power
                        MaximumRailOutputPowerW = 0.8f * MaximumDieselPowerW;
                }

                // Without tractive force curves, power output at each throttle setting is MaxPower * throttle
                int size = ThrottleRPMTab.GetSize();
                throttleRailPower = new (float, float)[size];

                for (int i = 0; i < size; i++)
                {
                    // Throttle must be a 0-1 value for this calculation
                    float normThrottle = ThrottleRPMTab.X[i] / 100.0f;
                    throttleRailPower[i] = (normThrottle, MaximumRailOutputPowerW * normThrottle * maxTractionPowerProportion);
                }
            }

            // Set power in locomotive if it wasn't set (might be the case if tractive force curves were used)
            if (Locomotive.MaxPowerW <= 0)
                Locomotive.MaxPowerW = MaximumRailOutputPowerW;
            if (Locomotive.LocomotiveMaxRailOutputPowerW <= 0)
                Locomotive.LocomotiveMaxRailOutputPowerW = MaximumRailOutputPowerW;

            // Set rail power vs RPM from calculated rail powers
            if (RailPowerTab == null)
            {
                int size = throttleRailPower.Length;
                List<(float, float)> tempRailPowerPairs = new List<(float, float)>();

                for (int i = 0; i < size; i++)
                {
                    // Convert calculated throttle/power values to rpm/power values
                    tempRailPowerPairs.Add((ThrottleRPMTab[throttleRailPower[i].Item1 * 100.0f], throttleRailPower[i].Item2));
                }
                // Need to consider that RPM vs throttle table may be in an unusual order, or have duplicate values
                // Re-sort the table from lowest to highest RPM (RPM being Item1 in the tuple)
                tempRailPowerPairs.OrderBy(p => p.Item1);

                // Transfer the temporary rpm/power pairs to lists, removing duplicates in the process
                List<float> rpm = new List<float>();
                List<float> railPower = new List<float>();

                // Start from the first rpm/power pair
                rpm.Add(tempRailPowerPairs[0].Item1);
                railPower.Add(tempRailPowerPairs[0].Item2);

                for (int i = 1; i < size; i++)
                {
                    // If next RPM (Item1) value is the same as the last one, don't add it again
                    if (tempRailPowerPairs[i].Item1 <= rpm.Last())
                    {
                        // For duplicate RPM values, prefer the higher power value
                        if (tempRailPowerPairs[i].Item2 > railPower.Last())
                            railPower[railPower.Count - 1] = tempRailPowerPairs[i].Item2;
                    }
                    else
                    {
                        rpm.Add(tempRailPowerPairs[i].Item1);
                        railPower.Add(tempRailPowerPairs[i].Item2);
                    }
                }
                // Possible that only one value is added, interpolator needs at least 2
                if (rpm.Count < 2)
                {
                    rpm.Add(rpm[0] + 1.0f);
                    railPower.Add(railPower[0]);
                }

                RailPowerTab = new Interpolator(rpm.ToArray(), railPower.ToArray());
            }

            // Set auxiliary power vs RPM if not already set
            if (AuxPowerTab == null)
            {
                // Assume auxiliary power is rated engine power minus rail power
                float maxAuxPower = MaximumDieselPowerW - MaximumRailOutputPowerW / Locomotive.TransmissionEfficiency;

                if (maxAuxPower > 0)
                {
                    int size = DieselPowerTab.GetSize();

                    float[] rpm = DieselPowerTab.X;
                    float[] auxPower = new float[size];

                    for (int i = size - 1; i >= 0; i--)
                    {
                        float tempAux = DieselPowerTab[rpm[i]] - RailPowerTab[rpm[i]] / Locomotive.TransmissionEfficiency;
                        // Prevent nonsensical negative auxiliary draw, as well as nonsensically high auxiliary draw
                        auxPower[i] = MathHelper.Clamp(tempAux, 0.0f, maxAuxPower);
                        // Assume auxiliary draw is strictly increasing with engine RPM
                        if (auxPower[i] < maxAuxPower)
                            maxAuxPower = auxPower[i];
                    }

                    AuxPowerTab = new Interpolator(rpm, auxPower);

                    if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        if (DieselEngineConfigured)
                            Trace.TraceInformation("AuxiliaryPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from other data");
                        else
                            Trace.TraceInformation("AuxiliaryPowerTab constructed from other data (BASIC Config)");
                    }
                }
                else // Some engines are defined with engine power set to rail power, don't set aux power in that case
                {
                    AuxPowerTab = new Interpolator(new float[] { IdleRPM, MaxRPM }, new float[] { 0.0f, 0.0f });
                }
            }

            // Estimate engine friction losses (user shouldn't be entering friction data)
            if (FrictionTorqueTab == null)
            {
                // Estimate for friction torque (normalized to max friction torque, where "x" is actualRPM/maxRPM): 
                // y = 0.314 * x^2 + 0.118 * x + 0.568

                // Using fuel consumption, estimate friction at idle and at max RPM, then scale for other RPMs
                float idleFriction = 0.0f;
                float maxFriction = 0.0f;
                float idlePower = AuxPowerTab[IdleRPM];

                float rpmRatio = IdleRPM / MaxRPM;
                float frictionRatio = 0.314f * rpmRatio * rpmRatio + 0.118f * rpmRatio + 0.568f; // Ratio of idle:max friction torque

                // Some engines will apply power at idle RPM, which should be indicated by higher fuel use defined
                if (DieselConsumptionTab != null && DieselUsedPerHourAtIdleL < DieselConsumptionTab[IdleRPM] * 0.75f)
                {
                    // Estimate idle friction using two different levels of load at idle RPM
                    float loadedPower = DieselPowerTab[IdleRPM];
                    float loadedFuel = DieselConsumptionTab[IdleRPM];

                    // Assuming fuel efficiency (considering friction) is the same at both levels of load
                    // (idlePower + frictionPower) / idleFuel = (loadedPower + frictionPower) / loadedFuel
                    float idleFrictionPower = (loadedPower * DieselUsedPerHourAtIdleL - idlePower * loadedFuel) / (loadedFuel - DieselUsedPerHourAtIdleL);
                    idleFriction = Math.Max(idleFrictionPower / RPM.ToRadpS(IdleRPM), 0.0f);
                }    
                else
                {
                    // Estimate idle friction using two different RPMs
                    // Need to know how many times larger the max RPM friction is than at idle RPM
                    float frictionPowerRatio = 1.0f / (frictionRatio * rpmRatio);

                    // Assuming engine fuel efficiency is the same at idle and full power when accounting for friction
                    // (idlePower + frictionPower) / idleFuel = (MaxPower + frictionRatio * frictionPower) / loadedFuel
                    float idleFrictionPower = (MaximumDieselPowerW * DieselUsedPerHourAtIdleL - idlePower * DieselUsedPerHourAtMaxPowerL) / (DieselUsedPerHourAtMaxPowerL - frictionPowerRatio * DieselUsedPerHourAtIdleL);
                    idleFriction = Math.Max(idleFrictionPower / RPM.ToRadpS(IdleRPM), 0.0f);
                }

                maxFriction = idleFriction / frictionRatio;

                // Assemble table using idle friction and the estimated torque/rpm relationship
                int size = DieselTorqueTab.GetSize();
                float[] rpm = DieselTorqueTab.X;
                float[] friction = new float[size];

                if (idleFriction <= 0) // Couldn't find a realistic friction relationship, assume constant friction torque at all RPM
                {
                    float idleFrictionPower = (MaximumDieselPowerW * DieselUsedPerHourAtIdleL - idlePower * DieselUsedPerHourAtMaxPowerL) / (DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL);
                    idleFriction = Math.Max(idleFrictionPower / RPM.ToRadpS(IdleRPM), 0.0f);

                    for (int i = 0; i < size; i++)
                        friction[i] = idleFriction;
                }
                else // Could find a realistic relationship for friction vs RPM, use nonlinear friction torque relationship
                {
                    for (int i = 0; i < size; i++)
                    {
                        float normRPM = rpm[i] / MaxRPM;
                        friction[i] = 0.314f * normRPM * normRPM + 0.118f * normRPM + 0.568f;
                        friction[i] *= maxFriction;
                    }
                }

                FrictionTorqueTab = new Interpolator(rpm, friction);
            }

            // Estimate moment of inertia if not defined, using RPM change rates and friction
            if (InertiaKgM2 <= 0.0f)
            {
                // Assume moment of inertia is such that engine RPM decrease rate can be achieved entirely by friction
                float minFriction = FrictionTorqueTab.MinY();

                InertiaKgM2 = minFriction / RPM.ToRadpS(ChangeDownRPMpS);

                if (Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                {
                    if (DieselEngineConfigured)
                        Trace.TraceInformation("ShaftInertia not found in Diesel Engine Config (ADVANCED config): estimated value = {0}", InertiaKgM2);
                    else
                        Trace.TraceInformation("ShaftInertia (BASIC config): estimated value = {0}", InertiaKgM2);
                }
            }

            // If not defined, estimate fuel consumption at rated power from idle/max fuel use and power values
            if (DieselConsumptionTab == null)
            {
                int size = DieselPowerTab.GetSize();
                float[] rpm = DieselPowerTab.X;
                float[] fuel = new float[size];

                DieselPowerTab.MaxY(out float peakPowerRPM);
                // Assuming engine fuel efficiency is constant when accounting for friction
                float fuelPerPower = DieselUsedPerHourAtMaxPowerL / (MaximumDieselPowerW + FrictionTorqueTab[peakPowerRPM] * RPM.ToRadpS(peakPowerRPM));

                for (int i = 0; i < size; i++)
                {
                    float actualPower = DieselPowerTab[rpm[i]] + FrictionTorqueTab[rpm[i]] * RPM.ToRadpS(rpm[i]);
                    fuel[i] = fuelPerPower * actualPower;
                }

                DieselConsumptionTab = new Interpolator(rpm, fuel);
                if (DieselEngineConfigured && Locomotive.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("DieselConsumptionTab not found in Diesel Engine Config, set to default values");
            }
        }
    }
}
