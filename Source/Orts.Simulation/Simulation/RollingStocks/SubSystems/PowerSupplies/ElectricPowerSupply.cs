﻿// COPYRIGHT 2021 by the Open Rails project.
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
using Orts.Simulation.RollingStocks.SubSystems.Controllers;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedElectricPowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSElectricLocomotive ElectricLocomotive => Locomotive as MSTSElectricLocomotive;
        public ScriptedCircuitBreaker CircuitBreaker { get; protected set; }
        public ScriptedVoltageSelector VoltageSelector { get; protected set; }
        public ScriptedPantographSelector PantographSelector { get; protected set; }
        public ScriptedPowerLimitationSelector PowerLimitationSelector { get; protected set; }

        public override PowerSupplyType Type => PowerSupplyType.Electric;
        private ElectricPowerSupply Script => AbstractScript as ElectricPowerSupply;

        public float LineVoltageV => (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
        public float PantographVoltageV { get; set; }
        public float PantographVoltageVAC { get; set; }
        public float PantographVoltageVDC { get; set; }
        public float FilterVoltageV { get; set; } = 0;

        public ScriptedElectricPowerSupply(MSTSLocomotive locomotive) :
            base(locomotive)
        {
            CircuitBreaker = new ScriptedCircuitBreaker(this);
            VoltageSelector = new ScriptedVoltageSelector(this);
            PantographSelector = new ScriptedPantographSelector(this);
            PowerLimitationSelector = new ScriptedPowerLimitationSelector(this);
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                    CircuitBreaker.Parse(lowercasetoken, stf);
                    break;

                case "engine(ortsvoltageselector":
                    VoltageSelector.Parse(stf);
                    break;

                case "engine(ortspantographselector":
                    PantographSelector.Parse(stf);
                    break;

                case "engine(ortspowerlimitationselector":
                    PowerLimitationSelector.Parse(stf);
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
                VoltageSelector.Copy(scriptedOther.VoltageSelector);
                PantographSelector.Copy(scriptedOther.PantographSelector);
                PowerLimitationSelector.Copy(scriptedOther.PowerLimitationSelector);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Script == null)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    string[] pathArray = { Path.Combine(Path.GetDirectoryName(ElectricLocomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as ElectricPowerSupply;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Script == null)
                {
                    AbstractScript = new DefaultElectricPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
            }

            CircuitBreaker.Initialize();
            VoltageSelector.Initialize();
            PantographSelector.Initialize();
            PowerLimitationSelector.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            CircuitBreaker.InitializeMoving();
            VoltageSelector.InitializeMoving();
            PantographSelector.InitializeMoving();
            PowerLimitationSelector.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            CircuitBreaker.Save(outf);
            VoltageSelector.Save(outf);
            PantographSelector.Save(outf);
            PowerLimitationSelector.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            CircuitBreaker.Restore(inf);
            VoltageSelector.Restore(inf);
            PantographSelector.Restore(inf);
            PowerLimitationSelector.Restore(inf);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            CircuitBreaker.Update(elapsedClockSeconds);
            VoltageSelector.Update(elapsedClockSeconds);
            PantographSelector.Update(elapsedClockSeconds);
            PowerLimitationSelector.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
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
                    if (CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn)
                    {
                        SignalEvent(Event.PowerConverterOff);
                        SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    }
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
                                SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOn);
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
                            if (CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn)
                            {
                                SignalEvent(Event.PowerConverterOff);
                                SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                            }
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
                            if (AuxPowerOnTimer.Triggered && CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOff)
                            {
                                SignalEvent(Event.PowerConverterOn);
                                SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                            }
                            SetFilterVoltageV(VoltageFilter.Filter(PantographVoltageV(), elapsedClockSeconds));
                            break;
                    }
                    break;
            }

            if (PowerLimitationSelector.Position != null)
            {
                MaximumPowerW = PowerLimitationSelector.Position.PowerW;
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
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOnElectricTrainSupply);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOffElectricTrainSupply);
                    SignalEventToCircuitBreaker(PowerSupplyEvent.QuickPowerOff);
                    SignalEventToPantographs(PowerSupplyEvent.LowerPantograph);
                    SignalEventToOtherTrainVehicles(PowerSupplyEvent.LowerPantograph);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
