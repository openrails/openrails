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
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class DieselEngines : IEnumerable
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<DieselEngine> DEList = new List<DieselEngine>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return DEList.Count; } }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        public readonly MSTSDieselLocomotive Locomotive;

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public DieselEngines(MSTSDieselLocomotive loco)
        {
            Locomotive = loco;
        }

        /// <summary>
        /// constructor from copy
        /// </summary>
        public DieselEngines(DieselEngines copy, MSTSDieselLocomotive loco)
        {
            DEList = new List<DieselEngine>();
            foreach (DieselEngine de in copy.DEList)
            {
                DEList.Add(new DieselEngine(de, loco));
            }
            Locomotive = loco;
        }

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive, based on stf reader parameters 
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        /// <param name="stf">Reference to the ENG file reader</param>
        public DieselEngines(MSTSDieselLocomotive loco, STFReader stf)
        {
            Locomotive = loco;
            Parse(stf, loco);

        }


        public DieselEngine this[int i]
        {
            get { return DEList[i]; }
            set { DEList[i] = value; }
        }

        public void Add()
        {
            DEList.Add(new DieselEngine());
        }

        public void Add(DieselEngine de)
        {
            DEList.Add(de);
        }


        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">eference to the ENG file reader</param>
        public void Parse(STFReader stf, MSTSDieselLocomotive loco)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(0);
            for (int i = 0; i < count; i++)
            {
                string setting = stf.ReadString().ToLower();
                if (setting == "diesel")
                {
                    DEList.Add(new DieselEngine());

                    DEList[i].Parse(stf, loco);
                    DEList[i].Initialize(true);
                }
                
                if ((!DEList[i].IsInitialized))
                {
                    STFException.TraceWarning(stf, "Diesel engine model has some errors - loading MSTS format");
                    DEList[i].InitFromMSTS((MSTSDieselLocomotive)Locomotive);
                    DEList[i].Initialize(true);
                }
            }
        }

        public void Initialize(bool start)
        {
            foreach (DieselEngine de in DEList)
                de.Initialize(start);
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
                for (int i = 0; i < count; i++)
                {
                    DEList.Add(new DieselEngine());
                    DEList[i].InitFromMSTS((MSTSDieselLocomotive)Locomotive);
                    DEList[i].Initialize(true);
                }
                
            }
            foreach (DieselEngine de in DEList)
                de.Restore(inf);
        }

        /// <summary>
        /// A summary of power of all the diesels
        /// </summary>
        public float PowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.OutputPowerW;
                }
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
                {
                    temp |= (de.EngineStatus == DieselEngine.Status.Running) || (de.EngineStatus == DieselEngine.Status.Starting);
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaximalPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaxOutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of fuel flow of all the auxiliaries
        /// </summary>
        public float DieselFlowLps
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.DieselFlowLps;
                }
                return temp;
            }
        }

        public bool HasGearBox
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                {
                    temp |= (de.GearBox != null);
                }
                return temp;
            }
        }

        public float MotiveForceN
        {
            get
            {
                float temp = 0;
                foreach (DieselEngine de in DEList)
                {
                    if(de.GearBox != null)
                        temp += (de.DemandedThrottlePercent * 0.01f * de.GearBox.MotiveForceN);
                }
                return temp;
            }
        }

        /// <summary>
        /// Updates each auxiliary on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (DieselEngine de in DEList)
            {
                de.Update(elapsedClockSeconds);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public DieselEnum GetEnumerator()
        {
            return new DieselEnum(DEList.ToArray());
        }

        public string GetStatus()
        {
            var result = new StringBuilder();

            result.AppendFormat(Simulator.Catalog.GetString("Status"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(eng.EngineStatus)));

            result.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), FormatStrings.FormatPower(MaxOutputPowerW, Locomotive.IsMetric, false, false));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.MaxOutputPowerW, Locomotive.IsMetric, false, false));

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Load"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F1}%", eng.LoadPercent);

            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F0} {1}", eng.RealRPM, FormatStrings.rpm);

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Flow"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}/{1}", FormatStrings.FormatFuelVolume(pS.TopH(eng.DieselFlowLps), Locomotive.IsMetric, Locomotive.IsUK), FormatStrings.h);

            result.Append("\t");
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatTemperature(eng.DieselTemperatureDeg, Locomotive.IsMetric, false));

            result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Oil"));
            foreach (var eng in DEList)
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
                    if (eng.EngineStatus == DieselEngine.Status.Running)
                        num++;
                }
                return num;
            }
        }

    }

    public class DieselEnum : IEnumerator
    {
        public DieselEngine[] deList;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public DieselEnum(DieselEngine[] list)
        {
            deList = list;
        }

        public bool MoveNext()
        {
            position++;
            return (position < deList.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public DieselEngine Current
        {
            get
            {
                try
                {
                    return deList[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

    public class DieselEngine
    {
        public enum Status
        {
            [GetParticularString("Engine", "Stopped")] Stopped = 0,
            [GetParticularString("Engine", "Starting")] Starting = 1,
            [GetParticularString("Engine", "Running")] Running = 2,
            [GetParticularString("Engine", "Stopping")] Stopping = 3
        }

        public enum Cooling
        {
            NoCooling = 0,
            Mechanical = 1,
            Hysteresis = 2,
            Proportional = 3
        }

        public enum SettingsFlags
        {
            IdleRPM              = 0x0001,
            MaxRPM               = 0x0002,
            StartingRPM          = 0x0004,
            StartingConfirmRPM   = 0x0008,
            ChangeUpRPMpS        = 0x0010,
            ChangeDownRPMpS      = 0x0020,
            RateOfChangeUpRPMpSS = 0x0040,
            RateOfChangeDownRPMpSS = 0x0080,
            MaximalPowerW        = 0x0100,
            IdleExhaust          = 0x0200,
            MaxExhaust           = 0x0400,
            ExhaustDynamics      = 0x0800,
            ExhaustColor         = 0x1000,
            ExhaustTransientColor = 0x2000,
            DieselPowerTab       = 0x4000,
            DieselConsumptionTab = 0x8000,
            ThrottleRPMTab       = 0x10000,
            DieselTorqueTab      = 0x20000,
            MinOilPressure       = 0x40000,
            MaxOilPressure       = 0x80000,
            MaxTemperature       = 0x100000,
            Cooling              = 0x200000,
            TempTimeConstant     = 0x400000,
            OptTemperature       = 0x800000,
            IdleTemperature      = 0x1000000
        }

        public DieselEngine()
        {
        }

        public DieselEngine(DieselEngine copy, MSTSDieselLocomotive loco)
        {
            IdleRPM = copy.IdleRPM;
            MaxRPM = copy.MaxRPM;
            StartingRPM = copy.StartingRPM;
            StartingConfirmationRPM = copy.StartingConfirmationRPM;
            ChangeUpRPMpS = copy.ChangeUpRPMpS;
            ChangeDownRPMpS = copy.ChangeDownRPMpS;
            RateOfChangeUpRPMpSS = copy.RateOfChangeUpRPMpSS;
            RateOfChangeDownRPMpSS = copy.RateOfChangeDownRPMpSS;
            MaximalPowerW = copy.MaximalPowerW;
            initLevel = copy.initLevel;
            DieselPowerTab = new Interpolator(copy.DieselPowerTab);
            DieselConsumptionTab = new Interpolator(copy.DieselConsumptionTab);
            ThrottleRPMTab = new Interpolator(copy.ThrottleRPMTab);
            if (copy.DieselTorqueTab != null) DieselTorqueTab = new Interpolator(copy.DieselTorqueTab);
            DieselUsedPerHourAtMaxPowerL = copy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = copy.DieselUsedPerHourAtIdleL;
            InitialExhaust = copy.InitialExhaust;
            InitialMagnitude = copy.InitialMagnitude;
            MaxExhaust = copy.MaxExhaust;
            MaxMagnitude = copy.MaxMagnitude;
            ExhaustParticles = copy.ExhaustParticles;
            ExhaustColor = copy.ExhaustColor;
            ExhaustSteadyColor = copy.ExhaustSteadyColor;
            ExhaustTransientColor = copy.ExhaustTransientColor;
            ExhaustDecelColor = copy.ExhaustDecelColor;
            DieselMaxOilPressurePSI = copy.DieselMaxOilPressurePSI;
            DieselMinOilPressurePSI = copy.DieselMinOilPressurePSI;
            DieselMaxTemperatureDeg = copy.DieselMaxTemperatureDeg;

            if (copy.GearBox != null)
            {
                GearBox = new GearBox(copy.GearBox, this);
            }
            locomotive = loco;
        }

        #region Parameters and variables
        float dRPM;
        /// <summary>
        /// Actual change rate of the engine's RPM - useful for exhaust effects
        /// </summary>
        public float EngineRPMchangeRPMpS { get { return dRPM; } }
        /// <summary>
        /// Actual RPM of the engine
        /// </summary>
        public float RealRPM;

        /// <summary>
        /// RPM treshold when the engine starts to combust fuel
        /// </summary>
        public float StartingRPM;

        /// <summary>
        /// RPM treshold when the engine is considered as succesfully started
        /// </summary>
        public float StartingConfirmationRPM;

        /// <summary>
        /// GearBox unit
        /// </summary>
        public GearBox GearBox;

        /// <summary>
        /// Parent locomotive
        /// </summary>
        public MSTSDieselLocomotive locomotive;

        SettingsFlags initLevel;          //level of initialization
        /// <summary>
        /// Initialization flag - is true when sufficient number of parameters is read succesfully
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                if (initLevel == (SettingsFlags.IdleRPM | SettingsFlags.MaxRPM | SettingsFlags.StartingRPM | SettingsFlags.StartingConfirmRPM | SettingsFlags.ChangeUpRPMpS | SettingsFlags.ChangeDownRPMpS
                    | SettingsFlags.RateOfChangeUpRPMpSS | SettingsFlags.RateOfChangeDownRPMpSS | SettingsFlags.MaximalPowerW | SettingsFlags.IdleExhaust | SettingsFlags.MaxExhaust
                    | SettingsFlags.ExhaustDynamics | SettingsFlags.ExhaustColor | SettingsFlags.ExhaustTransientColor | SettingsFlags.DieselPowerTab | SettingsFlags.DieselConsumptionTab | SettingsFlags.ThrottleRPMTab
                    | SettingsFlags.DieselTorqueTab | SettingsFlags.MinOilPressure | SettingsFlags.MaxOilPressure | SettingsFlags.MaxTemperature | SettingsFlags.Cooling
                    | SettingsFlags.TempTimeConstant | SettingsFlags.OptTemperature | SettingsFlags.IdleTemperature))

                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Engine status
        /// </summary>
        public Status EngineStatus = Status.Stopped;
        /// <summary>
        /// Type of engine cooling
        /// </summary>
        public Cooling EngineCooling = Cooling.Proportional;

        /// <summary>
        /// The RPM controller tries to reach this value
        /// </summary>
        public float DemandedRPM;           
        float demandedThrottlePercent;
        /// <summary>
        /// Demanded throttle percent, usually token from parent locomotive
        /// </summary>
        public float DemandedThrottlePercent { set { demandedThrottlePercent = value > 100f ? 100f : (value < 0 ? 0 : value); } get { return demandedThrottlePercent; } }
        /// <summary>
        /// Idle RPM
        /// </summary>
        public float IdleRPM;
        /// <summary>
        /// Maximal RPM
        /// </summary>
        public float MaxRPM;
        /// <summary>
        /// RPM change rate from ENG file
        /// </summary>
        public float RPMRange;
        /// <summary>
        /// Change rate when accelerating the engine
        /// </summary>
        public float ChangeUpRPMpS;
        /// <summary>
        /// Change rate when decelerating the engine
        /// </summary>
        public float ChangeDownRPMpS;
        /// <summary>
        /// "Jerk" of the RPM when accelerating the engine
        /// </summary>
        public float RateOfChangeUpRPMpSS;
        /// <summary>
        /// "Jerk" of the RPM when decelerating the engine
        /// </summary>
        public float RateOfChangeDownRPMpSS;

        /// <summary>
        /// Power limit of the engine from ENG file
        /// </summary>
        public float MaximalPowerW;
        /// <summary>
        /// Actual power limit depending on the actual RPM
        /// </summary>
        public float MaxOutputPowerW;
        /// <summary>
        /// Real power output of the engine
        /// </summary>
        public float OutputPowerW;
        /// <summary>
        /// Relative output power to the MaximalPowerW
        /// </summary>
        public float ThrottlePercent { get { return OutputPowerW / MaximalPowerW * 100f; } }
        /// <summary>
        /// Fuel consumed at max power
        /// </summary>
        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        /// <summary>
        /// Fuel consumed at idle
        /// </summary>
        public float DieselUsedPerHourAtIdleL = 1.0f;
        /// <summary>
        /// Current fuel flow
        /// </summary>
        public float DieselFlowLps;
        /// <summary>
        /// Engine load table - Max output power vs. RPM
        /// </summary>
        public Interpolator DieselPowerTab;
        /// <summary>
        /// Engine consumption table - Consumption vs. RPM
        /// </summary>
        public Interpolator DieselConsumptionTab;
        /// <summary>
        /// Engine throttle settings table - RPM vs. throttle settings
        /// </summary>
        public Interpolator ThrottleRPMTab;
        /// <summary>
        /// Engine output torque table - Torque vs. RPM
        /// </summary>
        public Interpolator DieselTorqueTab;
         /// <summary>
        /// Current exhaust number of particles
        /// </summary>
        public float ExhaustParticles = 10.0f;
        /// <summary>
        /// Current exhaust color
        /// </summary>
        public Color ExhaustColor;
        /// <summary>
        /// Exhaust color at steady state (no RPM change)
        /// </summary>
        public Color ExhaustSteadyColor = Color.Gray;
        /// <summary>
        /// Exhaust color when accelerating the engine
        /// </summary>
        public Color ExhaustTransientColor = Color.Black;
        /// <summary>
        /// Exhaust color when decelerating the engine
        /// </summary>
        public Color ExhaustDecelColor = Color.WhiteSmoke;

        public Color ExhaustCompressorBlownColor = Color.Gray;

        public float InitialMagnitude = 1.5f;        
        public float MaxMagnitude = 1.5f;
        public float MagnitudeRange;
        public float ExhaustMagnitude = 1.5f;   

        public float InitialExhaust = 0.7f;
        public float MaxExhaust = 2.8f;
        public float ExhaustRange;

        public float ExhaustDecelReduction = 0.75f; //Represents the percentage that exhaust will be reduced while engine is decreasing RPMs.
        public float ExhaustAccelIncrease = 2.0f; //Represents the percentage that exhaust will be increased while engine is increasing RPMs.

        /// <summary>
        /// Current Engine oil pressure in PSI
        /// </summary>
        public float DieselOilPressurePSI
        {
            get
            {
                float k = (DieselMaxOilPressurePSI - DieselMinOilPressurePSI)/(MaxRPM - IdleRPM);
                float q = DieselMaxOilPressurePSI - k * MaxRPM;
                float res = k * RealRPM + q - dieseloilfailurePSI;
                if (res < 0f)
                    res = 0f;
                return res;
            }
        }
        /// <summary>
        /// Minimal oil pressure at IdleRPM
        /// </summary>
        public float DieselMinOilPressurePSI = 40f;
        /// <summary>
        /// Maximal oil pressure at MaxRPM
        /// </summary>
        public float DieselMaxOilPressurePSI = 120f;
        /// <summary>
        /// Oil failure/leakage is substracted from the DieselOilPressurePSI
        /// </summary>
        public float dieseloilfailurePSI = 0f;              //Intended to be implemented later
        /// <summary>
        /// Actual Engine temperature
        /// </summary>
        public float DieselTemperatureDeg = 40f;
        /// <summary>
        /// Maximal engine temperature
        /// </summary>
        public float DieselMaxTemperatureDeg = 100.0f;
        /// <summary>
        /// Time constant to heat up from zero to 63% of MaxTemperature
        /// </summary>
        public float DieselTempTimeConstantSec = 720f;
        /// <summary>
        /// Optimal temperature of the diesel at rated power
        /// </summary>
        public float DieselOptimalTemperatureDegC = 95f;
        /// <summary>
        /// Steady temperature when idling
        /// </summary>
        public float DieselIdleTemperatureDegC = 75f;
        /// <summary>
        /// Hysteresis of the cooling regulator
        /// </summary>
        public float DieselTempCoolingHyst = 20f;
        /// <summary>
        /// Cooling system indicator
        /// </summary>
        public bool DieselTempCoolingRunning = false;

        /// <summary>
        /// Load of the engine
        /// </summary>
        public float LoadPercent
        {
            get
            {
                return (MaxOutputPowerW <= 0f ? 0f : (OutputPowerW * 100f / MaxOutputPowerW)) ;
            }
        }
        /// <summary>
        /// The engine is connected to the gearbox
        /// </summary>
        public bool HasGearBox { get { return GearBox != null; } }
        #endregion

        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        /// <param name="loco">Reference to the locomotive</param>
        public virtual void Parse(STFReader stf, MSTSDieselLocomotive loco)
        {
            locomotive = loco;
            stf.MustMatch("(");
            bool end = false;
            while (!end)
            {
                string lowercasetoken = stf.ReadItem().ToLower();
                switch (lowercasetoken)
                {
                    case "idlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.IdleRPM; break;
                    case "maxrpm":          MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0);initLevel |= SettingsFlags.MaxRPM; break;
                    case "startingrpm": StartingRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.StartingRPM; break;
                    case "startingconfirmrpm": StartingConfirmationRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.StartingConfirmRPM; break;
                    case "changeuprpmps": ChangeUpRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.ChangeUpRPMpS; break;
                    case "changedownrpmps": ChangeDownRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.ChangeDownRPMpS; break;
                    case "rateofchangeuprpmpss": RateOfChangeUpRPMpSS = stf.ReadFloatBlock(STFReader.UNITS.None, 0);initLevel |= SettingsFlags.RateOfChangeUpRPMpSS; break;
                    case "rateofchangedownrpmpss": RateOfChangeDownRPMpSS = stf.ReadFloatBlock(STFReader.UNITS.None, 0);initLevel |= SettingsFlags.RateOfChangeDownRPMpSS; break;
                    case "maximalpower":   MaximalPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0);initLevel |= SettingsFlags.MaximalPowerW; break;
                    case "idleexhaust":     InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.IdleExhaust; break;
                    case "maxexhaust":      MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0);initLevel |= SettingsFlags.MaxExhaust; break;
                    case "exhaustdynamics": ExhaustAccelIncrease = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustdynamicsdown": ExhaustDecelReduction = stf.ReadFloatBlock(STFReader.UNITS.None, null); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustcolor":
                        // Color byte order changed in XNA 4 from BGRA to RGBA
                        ExhaustSteadyColor.PackedValue = stf.ReadHexBlock(Color.Gray.PackedValue);
                        var tempSR = ExhaustSteadyColor.R;
                        ExhaustSteadyColor.R = ExhaustSteadyColor.B;
                        ExhaustSteadyColor.B = tempSR;
                        initLevel |= SettingsFlags.ExhaustColor;
                        break;
                    case "exhausttransientcolor":
                        ExhaustTransientColor.PackedValue = stf.ReadHexBlock(Color.Black.PackedValue);
                        var tempTR = ExhaustTransientColor.R;
                        ExhaustTransientColor.R = ExhaustTransientColor.B;
                        ExhaustTransientColor.B = tempTR;
                        initLevel |= SettingsFlags.ExhaustTransientColor;
                        break;
                    case "dieselpowertab": DieselPowerTab = new Interpolator(stf);initLevel |= SettingsFlags.DieselPowerTab; break;
                    case "dieselconsumptiontab": DieselConsumptionTab = new Interpolator(stf);initLevel |= SettingsFlags.DieselConsumptionTab; break;
                    case "throttlerpmtab": ThrottleRPMTab = new Interpolator(stf); initLevel |= SettingsFlags.ThrottleRPMTab; break;
                    case "dieseltorquetab": DieselTorqueTab = new Interpolator(stf); initLevel |= SettingsFlags.DieselTorqueTab; break;
                    case "minoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 40f); initLevel |= SettingsFlags.MinOilPressure; break;
                    case "maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 120f); initLevel |= SettingsFlags.MaxOilPressure; break;
                    case "maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 100f); initLevel |= SettingsFlags.MaxTemperature; break;
                    case "cooling": EngineCooling = (Cooling)stf.ReadIntBlock((int)Cooling.Proportional); initLevel |= SettingsFlags.Cooling; break ; //ReadInt changed to ReadIntBlock
                    case "temptimeconstant": DieselTempTimeConstantSec = stf.ReadFloatBlock(STFReader.UNITS.Time, 720f); initLevel |= SettingsFlags.TempTimeConstant; break;
                    case "opttemperature": DieselOptimalTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 95f); initLevel |= SettingsFlags.OptTemperature; break;
                    case "idletemperature": DieselIdleTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 75f); initLevel |= SettingsFlags.IdleTemperature; break;
                    default:
                        end = true;
                        break;
                }
            }
        }

        public void Initialize(bool start)
        {
            if (start)
            {
                RealRPM = IdleRPM;
                EngineStatus = Status.Running;
            }
            RPMRange = MaxRPM - IdleRPM;
            MagnitudeRange = MaxMagnitude - InitialMagnitude;
            ExhaustRange = MaxExhaust - InitialExhaust;
            ExhaustSteadyColor.A = 10;
            ExhaustDecelColor.A = 10;
        }


        public void Update(float elapsedClockSeconds)
        {
            if (EngineStatus == DieselEngine.Status.Running)
                DemandedThrottlePercent = locomotive.ThrottlePercent;
            else
                DemandedThrottlePercent = 0f;

            if (locomotive.Direction == Direction.Reverse)
                locomotive.PrevMotiveForceN *= -1f;

            if ((EngineStatus == DieselEngine.Status.Running) && (locomotive.ThrottlePercent > 0))
            {
                OutputPowerW = (locomotive.PrevMotiveForceN > 0 ? locomotive.PrevMotiveForceN * locomotive.AbsSpeedMpS : 0) / locomotive.DieselEngines.NumOfActiveEngines;
            }
            else
            {
                OutputPowerW = 0.0f;
            }

            if ((ThrottleRPMTab != null) && (EngineStatus == Status.Running))
            {
                DemandedRPM = ThrottleRPMTab[demandedThrottlePercent];
            }

            if (GearBox != null)
            {
                if (RealRPM > 0)
                    GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                else
                    GearBox.ClutchPercent = 100f;
                
                if (GearBox.CurrentGear != null)
                {
                    if (GearBox.IsClutchOn)
                        DemandedRPM = GearBox.ShaftRPM;
                }
            }

            if ( RealRPM == IdleRPM )
            {
                ExhaustParticles = InitialExhaust;
                ExhaustMagnitude = InitialMagnitude;
                ExhaustColor = ExhaustSteadyColor;
            }
            if (RealRPM < DemandedRPM)
            {
                dRPM = (float)Math.Min(Math.Sqrt(2 * RateOfChangeUpRPMpSS * (DemandedRPM - RealRPM)), ChangeUpRPMpS);
                if ( dRPM > 1.0f ) //The forumula above generates a floating point error that we have to compensate for so we can't actually test for zero.
                {
                    ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange ))) * ExhaustAccelIncrease;
                    ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                    ExhaustColor = ExhaustTransientColor; 
                }
                else
                {
                    dRPM = 0;
                    ExhaustParticles = InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange));
                    ExhaustMagnitude = InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange));
                    ExhaustColor = ExhaustSteadyColor;
                }
            }
            else if (RealRPM > (DemandedRPM))
                {
                    dRPM = (float)Math.Max(-Math.Sqrt(2 * RateOfChangeDownRPMpSS * (RealRPM - DemandedRPM)), -ChangeDownRPMpS);
                    ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustColor = ExhaustDecelColor;

                }

            if ((OutputPowerW > (1.1f * MaxOutputPowerW)) && (EngineStatus == Status.Running))
                dRPM = (MaxOutputPowerW - OutputPowerW) / MaximalPowerW * 0.01f * RateOfChangeDownRPMpSS;

            RealRPM = Math.Max(RealRPM + dRPM * elapsedClockSeconds, 0);

            if (DieselPowerTab != null)
            {
                MaxOutputPowerW = (DieselPowerTab[RealRPM] <= MaximalPowerW * (1 - locomotive.PowerReduction) ? DieselPowerTab[RealRPM] * (1 - locomotive.PowerReduction) : MaximalPowerW) * (1 - locomotive.PowerReduction);
                MaxOutputPowerW = MaxOutputPowerW < 0f ? 0f : MaxOutputPowerW;
            }
             else
            {
                MaxOutputPowerW = (RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * MaximalPowerW * (1 - locomotive.PowerReduction);
            }

            if (EngineStatus == Status.Starting)
            {
                if ((RealRPM > (0.9f * StartingRPM)) && (RealRPM < StartingRPM))
                {
                    DemandedRPM = 1.1f * StartingConfirmationRPM;
                    ExhaustColor = ExhaustTransientColor;
                    ExhaustParticles = (MaxExhaust - InitialExhaust) / (0.5f * StartingRPM - StartingRPM) * (RealRPM - 0.5f * StartingRPM) + InitialExhaust;
                }
                if ((RealRPM > StartingConfirmationRPM))// && (RealRPM < 0.9f * IdleRPM))
                    EngineStatus = Status.Running;
            }

            if ((EngineStatus != Status.Starting) && (RealRPM == 0f))
                EngineStatus = Status.Stopped;

            if ((EngineStatus == Status.Stopped) || (EngineStatus == Status.Stopping) || ((EngineStatus == Status.Starting) && (RealRPM < StartingRPM)))
            {
                ExhaustParticles = 0;
                DieselFlowLps = 0;
            }
            else
            {
                if (DieselConsumptionTab != null)
                {
                         DieselFlowLps = DieselConsumptionTab[RealRPM] / 3600.0f;
                }
                else
                {
                    if (ThrottlePercent == 0)
                        DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                    else
                        DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * ThrottlePercent / 100f + DieselUsedPerHourAtIdleL) / 3600.0f;
                }
            }

            if (ExhaustParticles > 100f)
                ExhaustParticles = 100f;

            if (locomotive.PowerReduction == 1 && EngineStatus != Status.Stopped)     // Compressor blown, you get much smoke 
            {
                ExhaustColor = Color.WhiteSmoke;
                ExhaustParticles = 40f;
                ExhaustMagnitude = InitialMagnitude * 2;
            }

            DieselTemperatureDeg += elapsedClockSeconds * (DieselMaxTemperatureDeg - DieselTemperatureDeg) / DieselTempTimeConstantSec;
            switch(EngineCooling)
            {
                case Cooling.NoCooling:
                    DieselTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.01f * (95f - 60f) + 60f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    DieselTempCoolingRunning = false;
                    break;
                case Cooling.Mechanical:
                    DieselTemperatureDeg += elapsedClockSeconds * ((RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * 95f + 60f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    DieselTempCoolingRunning = true;
                    break;
                case Cooling.Hysteresis:
                    if(DieselTemperatureDeg > DieselMaxTemperatureDeg)
                        DieselTempCoolingRunning = true;
                    if(DieselTemperatureDeg < (DieselMaxTemperatureDeg - DieselTempCoolingHyst))
                        DieselTempCoolingRunning = false;

                    if(DieselTempCoolingRunning)
                        DieselTemperatureDeg += elapsedClockSeconds * (DieselMaxTemperatureDeg - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    else
                        DieselTemperatureDeg -= elapsedClockSeconds * (DieselMaxTemperatureDeg - 2f * DieselTempCoolingHyst - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    break;
                default:
                case Cooling.Proportional:
                    float cooling = (95f - DieselTemperatureDeg) * 0.01f;
                    cooling = cooling < 0f ? 0 : cooling;
                    if (DieselTemperatureDeg >= (80f))
                        DieselTempCoolingRunning = true;
                    if(DieselTemperatureDeg < (80f - DieselTempCoolingHyst))
                        DieselTempCoolingRunning = false;

                    if (!DieselTempCoolingRunning)
                        cooling = 0f;

                    DieselTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.01f * 95f - DieselTemperatureDeg) / DieselTempTimeConstantSec;
                    if (DieselTemperatureDeg > DieselMaxTemperatureDeg - DieselTempCoolingHyst)
                        DieselTemperatureDeg = DieselMaxTemperatureDeg - DieselTempCoolingHyst;
                    break;
            }
            if(DieselTemperatureDeg < 40f)
                DieselTemperatureDeg = 40f;

            if (GearBox != null)
            {
                if ((locomotive.IsLeadLocomotive()))
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (locomotive.GearBoxController.CurrentNotch > 0)
                            GearBox.NextGear = GearBox.Gears[locomotive.GearBoxController.CurrentNotch - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                else
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (locomotive.GearboxGearIndex > 0)
                            GearBox.NextGear = GearBox.Gears[locomotive.GearboxGearIndex - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                if (GearBox.CurrentGear == null)
                    OutputPowerW = 0f;

                GearBox.Update(elapsedClockSeconds);
            }
        }

        public Status Start()
        {
            switch (EngineStatus)
            {
                case Status.Stopped:
                case Status.Stopping:
                    DemandedRPM = StartingRPM;
                    EngineStatus = Status.Starting;
                    break;
                default:
                    break;
            }
            return EngineStatus;
        }

        public Status Stop()
        {
            if (EngineStatus != Status.Stopped)
            {
                DemandedRPM = 0;
                EngineStatus = Status.Stopping;
                if (RealRPM <= 0)
                    EngineStatus = Status.Stopped;
            }
            return EngineStatus;
        }

        public void Restore(BinaryReader inf)
        {
            EngineStatus = (Status)inf.ReadInt32();
            RealRPM = inf.ReadSingle();
            OutputPowerW = inf.ReadSingle();
            DieselTemperatureDeg = inf.ReadSingle();

            Boolean gearSaved    = inf.ReadBoolean();  // read boolean which indicates gear data was saved

            if (((MSTSDieselLocomotive)locomotive).GearBox != null)
            {
                if (!((MSTSDieselLocomotive)locomotive).GearBox.IsInitialized || !gearSaved)
                    GearBox = null;
                else
                {
                    GearBox.Restore(inf);
                }
            }

        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)EngineStatus);
            outf.Write(RealRPM);
            outf.Write(OutputPowerW);
            outf.Write(DieselTemperatureDeg);
            if (GearBox != null)
            {
                outf.Write(true);
                GearBox.Save(outf);
            }
            else
            {
                outf.Write(false);
            }
        }

        public void InitializeMoving()
        {
            EngineStatus = Status.Running;
        }

        public void InitFromMSTS(MSTSDieselLocomotive loco)
        {
            if((initLevel & SettingsFlags.IdleRPM) == 0) IdleRPM = loco.IdleRPM;
            if ((initLevel & SettingsFlags.MaxRPM) == 0) MaxRPM = loco.MaxRPM;
            InitialMagnitude = loco.InitialMagnitude;
            MaxMagnitude = loco.MaxMagnitude;
            if ((initLevel & SettingsFlags.MaxExhaust) == 0) MaxExhaust = loco.MaxExhaust;
            if ((initLevel & SettingsFlags.ExhaustColor) == 0) ExhaustSteadyColor = loco.ExhaustSteadyColor;
            ExhaustDecelColor = loco.ExhaustDecelColor;
            if ((initLevel & SettingsFlags.ExhaustTransientColor) == 0) ExhaustTransientColor = loco.ExhaustTransientColor;
            if ((initLevel & SettingsFlags.StartingRPM) == 0) StartingRPM = loco.IdleRPM * 2.0f / 3.0f;
            if ((initLevel & SettingsFlags.StartingConfirmRPM) == 0) StartingConfirmationRPM = loco.IdleRPM * 1.1f;
            if ((initLevel & SettingsFlags.ChangeUpRPMpS) == 0) ChangeUpRPMpS = loco.MaxRPMChangeRate;
            if ((initLevel & SettingsFlags.ChangeDownRPMpS) == 0) ChangeDownRPMpS = loco.MaxRPMChangeRate;
            if ((initLevel & SettingsFlags.RateOfChangeUpRPMpSS) == 0) RateOfChangeUpRPMpSS = ChangeUpRPMpS;
            if ((initLevel & SettingsFlags.RateOfChangeDownRPMpSS) == 0) RateOfChangeDownRPMpSS = ChangeDownRPMpS;
            if ((initLevel & SettingsFlags.MaximalPowerW) == 0) MaximalPowerW = loco.MaxPowerW;
            if ((initLevel & SettingsFlags.MaxOilPressure) == 0) DieselMaxOilPressurePSI = loco.DieselMaxOilPressurePSI;
            if ((initLevel & SettingsFlags.MinOilPressure) == 0) DieselMinOilPressurePSI = loco.DieselMinOilPressurePSI;
            if ((initLevel & SettingsFlags.MaxTemperature) == 0) DieselMaxTemperatureDeg = loco.DieselMaxTemperatureDeg;
            if ((initLevel & SettingsFlags.Cooling) == 0) EngineCooling = loco.DieselEngineCooling;
            if ((initLevel & SettingsFlags.TempTimeConstant) == 0) DieselTempTimeConstantSec = 720f;
            //DieselPowerTab = new Interpolator(new float[] { diesel.IdleRPM, diesel.IdleRPM * 1.01f, diesel.MaxRPM * 0.5f, diesel.MaxRPM }, new float[] { diesel.MaxPowerW * 0.05f, diesel.MaxRPM * 0.1f, diesel.MaxRPM * 0.5f, diesel.MaxPowerW });
            //DieselPowerTab = new Interpolator(new float[] { diesel.IdleRPM, diesel.MaxRPM }, new float[] { diesel.MaxPowerW * 0.05f, diesel.MaxPowerW });
            if ((initLevel & SettingsFlags.DieselConsumptionTab) == 0) 
                DieselConsumptionTab = new Interpolator(new float[] { loco.IdleRPM, loco.MaxRPM }, new float[] { loco.DieselUsedPerHourAtIdleL, loco.DieselUsedPerHourAtMaxPowerL });
            if ((initLevel & SettingsFlags.ThrottleRPMTab) == 0) 
                ThrottleRPMTab = new Interpolator(new float[] { 0, 100 }, new float[] { loco.IdleRPM, loco.MaxRPM });

            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                int count = 11;
                float[] rpm = new float[count + 1];
                float[] power = new float[] { 0.02034f, 0.09302f, 0.36628f, 0.60756f, 0.69767f, 0.81395f, 0.93023f, 0.9686f, 0.99418f, 0.99418f, 1f, 0.5f };
                float[] torque = new float[] { 0.05f, 0.2f, 0.7f, 0.95f, 1f, 1f, 0.98f, 0.95f, 0.9f, 0.86f, 0.81f, 0.3f };

                for (int i = 0; i < count; i++)
                {
                    if (i == 0)
                        rpm[i] = loco.IdleRPM;
                    else
                        rpm[i] = rpm[i - 1] + (loco.MaxRPM - loco.IdleRPM) / (count - 1);
                    power[i] *= MaximalPowerW;
                    torque[i] *= MaximalPowerW / (MaxRPM * 2f * 3.1415f / 60f) / 0.81f;
                }
                rpm[count] = loco.MaxRPM * 1.5f;
                DieselPowerTab = new Interpolator(rpm, power);
                //DieselPowerTab.test("PowerTab", count);
                DieselTorqueTab = new Interpolator(rpm, torque);
                //DieselTorqueTab.test("TorqueTab", count);
            }

            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == SettingsFlags.DieselPowerTab))
            {
                float[] rpm = new float[DieselPowerTab.GetSize()];
                float[] torque = new float[DieselPowerTab.GetSize()];
                for (int i = 0; i < DieselPowerTab.GetSize(); i++)
                {
                    rpm[i] = IdleRPM + (float)i * (MaxRPM - IdleRPM) / (float)DieselPowerTab.GetSize();
                    torque[i] = DieselPowerTab[rpm[i]] / (rpm[i] * 2f * 3.1415f / 60f);
                }
            }

            if (((initLevel & SettingsFlags.DieselTorqueTab) == SettingsFlags.DieselTorqueTab) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                float[] rpm = new float[DieselPowerTab.GetSize()];
                float[] power = new float[DieselPowerTab.GetSize()];
                for (int i = 0; i < DieselPowerTab.GetSize(); i++)
                {
                    rpm[i] = IdleRPM + (float)i * (MaxRPM - IdleRPM) / (float)DieselPowerTab.GetSize();
                    power[i] = DieselPowerTab[rpm[i]] * rpm[i] * 2f * 3.1415f / 60f;
                }
            }
            
            InitialExhaust = loco.InitialExhaust;
            MaxExhaust = loco.MaxExhaust;
            locomotive = loco;
        }

    }
}
