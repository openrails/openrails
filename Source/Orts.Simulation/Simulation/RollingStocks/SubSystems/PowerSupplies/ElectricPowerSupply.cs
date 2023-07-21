// COPYRIGHT 2021 by the Open Rails project.
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

using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedElectricPowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSElectricLocomotive ElectricLocomotive => Locomotive as MSTSElectricLocomotive;
        public Pantographs Pantographs => Locomotive.Pantographs;
        public ScriptedCircuitBreaker CircuitBreaker { get; protected set; }

        public override PowerSupplyType Type => PowerSupplyType.Electric;
        public bool Activated = false;
        private ElectricPowerSupply Script => AbstractScript as ElectricPowerSupply;

        public float LineVoltageV => (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
        public float PantographVoltageV { get; set; }
        public float FilterVoltageV { get; set; } = 0;

        public ScriptedElectricPowerSupply(MSTSLocomotive locomotive) :
            base(locomotive)
        {
            CircuitBreaker = new ScriptedCircuitBreaker(this);
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                    CircuitBreaker.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);

            if (other is ScriptedElectricPowerSupply scriptedOther)
            {
                CircuitBreaker.Copy(scriptedOther.CircuitBreaker);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(ElectricLocomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as ElectricPowerSupply;
                }
                if (Script == null)
                {
                    AbstractScript = new DefaultElectricPowerSupply();
                }

                AssignScriptFunctions();

                Script.Initialize();
                Activated = true;
            }

            CircuitBreaker.Initialize();
        }


        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            CircuitBreaker.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);

            base.Save(outf);
            CircuitBreaker.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();

            base.Restore(inf);
            CircuitBreaker.Restore(inf);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            CircuitBreaker.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }

        protected override void AssignScriptFunctions()
        {
            base.AssignScriptFunctions();

            // ElectricPowerSupply getters
            Script.CurrentPantographState = () => Pantographs.State;
            Script.CurrentCircuitBreakerState = () => CircuitBreaker.State;
            Script.CircuitBreakerDriverClosingOrder = () => CircuitBreaker.DriverClosingOrder;
            Script.CircuitBreakerDriverOpeningOrder = () => CircuitBreaker.DriverOpeningOrder;
            Script.CircuitBreakerDriverClosingAuthorization = () => CircuitBreaker.DriverClosingAuthorization;
            Script.PantographVoltageV = () => PantographVoltageV;
            Script.FilterVoltageV = () => FilterVoltageV;
            Script.LineVoltageV = () => LineVoltageV;

            // ElectricPowerSupply setters
            Script.SetPantographVoltageV = (value) => PantographVoltageV = value;
            Script.SetFilterVoltageV = (value) => FilterVoltageV = value;
            Script.SignalEventToCircuitBreaker = (evt) => CircuitBreaker.HandleEvent(evt);
        }
    }

    public class DefaultElectricPowerSupply : ElectricPowerSupply
    {
        private IIRFilter PantographFilter;
        private IIRFilter VoltageFilter;
        private Timer PowerOnTimer;
        private Timer AuxPowerOnTimer;

        private bool QuickPowerOn = false;

        public override void Initialize()
        {
            PantographFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            VoltageFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentPantographState())
            {
                case PantographState.Down:
                case PantographState.Lowering:
                case PantographState.Raising:
                    if (PowerOnTimer.Started)
                        PowerOnTimer.Stop();
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();

                    if (CurrentMainPowerSupplyState() == PowerSupplyState.PowerOn)
                    {
                        SignalEvent(Event.EnginePowerOff);
                        SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                    }
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    SetPantographVoltageV(PantographFilter.Filter(0.0f, elapsedClockSeconds));
                    SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                    break;

                case PantographState.Up:
                    SetPantographVoltageV(PantographFilter.Filter(LineVoltageV(), elapsedClockSeconds));

                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Open:
                            // If circuit breaker is open, then it must be closed to finish the quick power-on sequence
                            if (QuickPowerOn)
                            {
                                QuickPowerOn = false;
                                SignalEventToCircuitBreaker(PowerSupplyEvent.CloseCircuitBreaker);
                            }

                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();
                            if (AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Stop();

                            if (CurrentMainPowerSupplyState() == PowerSupplyState.PowerOn)
                            {
                                SignalEvent(Event.EnginePowerOff);
                                SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                            }
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                            break;

                        case CircuitBreakerState.Closed:
                            // If circuit breaker is closed, quick power-on sequence has finished
                            QuickPowerOn = false;

                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();
                            if (!AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Start();

                            if (PowerOnTimer.Triggered && CurrentMainPowerSupplyState() == PowerSupplyState.PowerOff)
                            {
                                SignalEvent(Event.EnginePowerOn);
                                SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOn);
                            }
                            SetCurrentAuxiliaryPowerSupplyState(AuxPowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(PantographVoltageV(), elapsedClockSeconds));
                            break;
                    }
                    break;
            }

            // By default, on electric locomotives, dynamic brake is always available (rheostatic brake is always available).
            SetCurrentDynamicBrakeAvailability(true);

            if (ElectricTrainSupplyUnfitted())
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.Unavailable);
            }
            else if (CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn
                    && ElectricTrainSupplySwitchOn())
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.PowerOn);
            }
            else
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.PowerOff);
            }
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                case PowerSupplyEvent.ForcedPowerOn:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOnElectricTrainSupply);
                    break;

                case PowerSupplyEvent.QuickPowerOnConditional:
                case PowerSupplyEvent.ForcedPowerOnConditional:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToPantographs(PowerSupplyEvent.RaisePantographConditional);
                    SignalEventToOtherTrainVehicles(PowerSupplyEvent.RaisePantographConditional);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOnElectricTrainSupply);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                case PowerSupplyEvent.ForcedPowerOff:
                    QuickPowerOn = false;
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOffElectricTrainSupply);
                    SignalEventToCircuitBreaker(PowerSupplyEvent.OpenCircuitBreaker);
                    SignalEventToPantographs(PowerSupplyEvent.LowerPantograph);
                    SignalEventToOtherTrainVehicles(PowerSupplyEvent.LowerPantograph);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    break;

                case PowerSupplyEvent.QuickPowerOffPantoUp:
                case PowerSupplyEvent.ForcedPowerOffPantoUp:
                    QuickPowerOn = false;
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOffElectricTrainSupply);
                    SignalEventToCircuitBreaker(PowerSupplyEvent.OpenCircuitBreaker);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    // raise pantographs as panto conditions might have changed
                    SignalEventToPantographs(PowerSupplyEvent.RaisePantographConditional);
                    SignalEventToOtherTrainVehicles(PowerSupplyEvent.RaisePantographConditional);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
