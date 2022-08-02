// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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
using ORTS.Common;
using ORTS.Scripting.Api;
using System.IO;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedDualModePowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSElectricLocomotive DualModeLocomotive => Locomotive as MSTSElectricLocomotive;
        public DieselEngines DieselEngines => null; // TODO : Add diesel engines when ORTSDualModeLocomotive is created
        public ScriptedCircuitBreaker CircuitBreaker { get; protected set; }
        public ScriptedTractionCutOffRelay TractionCutOffRelay { get; protected set; }
        public ScriptedVoltageSelector VoltageSelector { get; protected set; }
        public ScriptedPantographSelector PantographSelector { get; protected set; }
        public ScriptedPowerLimitationSelector PowerLimitationSelector { get; protected set; }

        public override PowerSupplyType Type => PowerSupplyType.DualMode;
        public bool Activated = false;
        private DualModePowerSupply Script => AbstractScript as DualModePowerSupply;

        public float LineVoltageV => (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
        public float PantographVoltageV { get; set; }
        public float FilterVoltageV { get; set; } = 0;

        public PowerSupplyMode Mode { get; set; } = PowerSupplyMode.None;

        public ScriptedDualModePowerSupply(MSTSElectricLocomotive locomotive) :
            base(locomotive)
        {
            CircuitBreaker = new ScriptedCircuitBreaker(this);
            TractionCutOffRelay = new ScriptedTractionCutOffRelay(this);
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

                case "engine(ortstractioncutoffrelay":
                case "engine(ortstractioncutoffrelayclosingdelay":
                    TractionCutOffRelay.Parse(lowercasetoken, stf);
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

            if (other is ScriptedDualModePowerSupply scriptedOther)
            {
                CircuitBreaker.Copy(scriptedOther.CircuitBreaker);
                TractionCutOffRelay.Copy(scriptedOther.TractionCutOffRelay);
                VoltageSelector.Copy(scriptedOther.VoltageSelector);
                PantographSelector.Copy(scriptedOther.PantographSelector);
                PowerLimitationSelector.Copy(scriptedOther.PowerLimitationSelector);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as DualModePowerSupply;
                }
                if (Script == null)
                {
                    AbstractScript = new DefaultDualModePowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }

            CircuitBreaker.Initialize();
            TractionCutOffRelay.Initialize();
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
            TractionCutOffRelay.InitializeMoving();
            VoltageSelector.InitializeMoving();
            PantographSelector.InitializeMoving();
            PowerLimitationSelector.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            CircuitBreaker.Save(outf);
            TractionCutOffRelay.Save(outf);
            VoltageSelector.Save(outf);
            PantographSelector.Save(outf);
            PowerLimitationSelector.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            CircuitBreaker.Restore(inf);
            TractionCutOffRelay.Restore(inf);
            VoltageSelector.Restore(inf);
            PantographSelector.Restore(inf);
            PowerLimitationSelector.Restore(inf);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            CircuitBreaker.Update(elapsedClockSeconds);
            TractionCutOffRelay.Update(elapsedClockSeconds);
            VoltageSelector.Update(elapsedClockSeconds);
            PantographSelector.Update( elapsedClockSeconds);
            PowerLimitationSelector.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }
    }

    public class DefaultDualModePowerSupply : DualModePowerSupply
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

                    SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                    SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                    PantographVoltageV = PantographFilter.Filter(0.0f, elapsedClockSeconds);
                    FilterVoltageV = VoltageFilter.Filter(0.0f, elapsedClockSeconds);
                    break;

                case PantographState.Up:
                    PantographVoltageV = PantographFilter.Filter(LineVoltageV, elapsedClockSeconds);

                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Open:
                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();
                            if (AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Stop();

                            SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                            FilterVoltageV = VoltageFilter.Filter(0.0f, elapsedClockSeconds);
                            break;

                        case CircuitBreakerState.Closed:
                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();
                            if (!AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Start();

                            SetCurrentMainPowerSupplyState(PowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryPowerSupplyState(AuxPowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            FilterVoltageV = VoltageFilter.Filter(PantographVoltageV, elapsedClockSeconds);
                            break;
                    }
                    break;
            }
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.CloseBatterySwitch);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOnElectricTrainSupply);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOffElectricTrainSupply);
                    SignalEventToCircuitBreaker(PowerSupplyEvent.OpenCircuitBreaker);
                    SignalEventToPantographs(PowerSupplyEvent.LowerPantograph);
                    SignalEventToOtherTrainVehicles(PowerSupplyEvent.LowerPantograph);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToBatterySwitch(PowerSupplyEvent.OpenBatterySwitch);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
