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
using System.IO;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace ORTS
{

    public class ScriptedElectricPowerSupply
    {
        private readonly MSTSElectricLocomotive Locomotive;
        private readonly Simulator Simulator;
        private readonly Pantographs Pantographs;
        public ScriptedCircuitBreaker CircuitBreaker;

        public bool Activated = false;
        private string ScriptName = "Default";
        private ElectricPowerSupply Script;

        private PowerSupplyState state;
        public PowerSupplyState State
        {
            get
            {
                return state;
            }

            private set
            {
                if (state != value)
                {
                    state = value;

                    switch (state)
                    {
                        case PowerSupplyState.PowerOff:
                            Locomotive.SignalEvent(Event.EnginePowerOff);
                            break;

                        case PowerSupplyState.PowerOn:
                            Locomotive.SignalEvent(Event.EnginePowerOn);
                            break;
                    }

                    Locomotive.PowerOn = PowerOn;
                }
            }
        }
        public bool PowerOn
        {
            get
            {
                return State == PowerSupplyState.PowerOn;
            }
        }

        private PowerSupplyState auxiliaryState;
        public PowerSupplyState AuxiliaryState
        {
            get
            {
                return auxiliaryState;
            }

            private set
            {
                if (auxiliaryState != value)
                {
                    auxiliaryState = value;

                    if (Locomotive.Train != null && Locomotive.IsLeadLocomotive())
                    {
                        foreach (TrainCar car in Locomotive.Train.Cars)
                        {
                            MSTSWagon wagon = car as MSTSWagon;

                            if (wagon != null)
                            {
                                wagon.AuxPowerOn = AuxPowerOn;
                            }
                        }
                    }
                }
            }
        }
        public bool AuxPowerOn
        {
            get
            {
                return auxiliaryState == PowerSupplyState.PowerOn;
            }
        }

        public bool RouteElectrified
        {
            get
            {
                return Simulator.TRK.Tr_RouteFile.Electrified || Simulator.Settings.OverrideNonElectrifiedRoutes;
            }
        }

        public float FilterVoltageV { get; set; }
        private float PowerOnDelayS = 0;
        private float AuxPowerOnDelayS = 0;

        public ScriptedElectricPowerSupply(MSTSElectricLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = locomotive.Simulator;
            Pantographs = locomotive.Pantographs;

            State = PowerSupplyState.PowerOff;
            AuxiliaryState = PowerSupplyState.PowerOff;
            FilterVoltageV = 0;

            CircuitBreaker = new ScriptedCircuitBreaker(Locomotive);
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowerondelay":
                    PowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortsauxpowerondelay":
                    AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                    break;

                case "engine(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                    CircuitBreaker.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(ScriptedElectricPowerSupply other)
        {
            ScriptName = other.ScriptName;
            
            State = other.State;
            AuxiliaryState = other.AuxiliaryState;

            PowerOnDelayS = other.PowerOnDelayS;
            AuxPowerOnDelayS = other.AuxPowerOnDelayS;

            CircuitBreaker.Copy(other.CircuitBreaker);
        }

        public void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();

            State = (PowerSupplyState) Enum.Parse(typeof(PowerSupplyState), inf.ReadString());
            AuxiliaryState = (PowerSupplyState)Enum.Parse(typeof(PowerSupplyState), inf.ReadString());

            PowerOnDelayS = inf.ReadSingle();
            AuxPowerOnDelayS = inf.ReadSingle();

            CircuitBreaker.Restore(inf);
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as ElectricPowerSupply;
                }
                if (Script == null)
                {
                    Script = new DefaultElectricPowerSupply() as ElectricPowerSupply;
                }

                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.CurrentState = () => State;
                Script.CurrentAuxiliaryState = () => AuxiliaryState;
                Script.CurrentPantographState = () => Pantographs.State;
                Script.CurrentCircuitBreakerState = () => CircuitBreaker.State;
                Script.FilterVoltageV = () => FilterVoltageV;
                Script.LineVoltageV = () => (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
                Script.PowerOnDelayS = () => PowerOnDelayS;
                Script.AuxPowerOnDelayS = () => AuxPowerOnDelayS;

                Script.SetCurrentState = (value) => State = value;
                Script.SetCurrentAuxiliaryState = (value) => AuxiliaryState = value;
                Script.SetFilterVoltageV = (value) => FilterVoltageV = value;

                Script.Initialize();
                Activated = true;
            }

            Pantographs.Initialize();
            CircuitBreaker.Initialize();
        }


        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public void InitializeMoving()
        {
            State = PowerSupplyState.PowerOn;
            AuxiliaryState = PowerSupplyState.PowerOn;
            CircuitBreaker.InitializeMoving();
        }

        public void Update(float elapsedClockSeconds)
        {
            CircuitBreaker.Update(elapsedClockSeconds);

            if (Script != null)
                Script.Update(elapsedClockSeconds);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            CircuitBreaker.HandleEvent(evt);
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);

            outf.Write(State.ToString());
            outf.Write(AuxiliaryState.ToString());

            outf.Write(PowerOnDelayS);
            outf.Write(AuxPowerOnDelayS);

            CircuitBreaker.Save(outf);
        }
    }

    public class DefaultElectricPowerSupply : ElectricPowerSupply
    {
        private IIRFilter VoltageFilter;
        private Timer PowerOnTimer;
        private Timer AuxPowerOnTimer;

        public override void Initialize()
        {
            VoltageFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
        }

        public override void Update(float elapsedClockSeconds)
        {
            switch (CurrentPantographState())
            {
                case PantographState.Down:
                case PantographState.Lowering:
                case PantographState.Raising:
                    if (PowerOnTimer.Started)
                        PowerOnTimer.Stop();
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();

                    SetCurrentState(PowerSupplyState.PowerOff);
                    SetCurrentAuxiliaryState(PowerSupplyState.PowerOff);
                    SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                    break;

                case PantographState.Up:
                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Open:
                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();
                            if (AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Stop();

                            SetCurrentState(PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryState(PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                            break;

                        case CircuitBreakerState.Closed:
                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();
                            if (!AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Start();

                            SetCurrentState(PowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryState(AuxPowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(LineVoltageV(), elapsedClockSeconds));
                            break;
                    }
                    break;
            }
        }
    }
}
