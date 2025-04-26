// COPYRIGHT 2020 by the Open Rails project.
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
using ORTS.Scripting.Api;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedDieselPowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSDieselLocomotive DieselLocomotive => Locomotive as MSTSDieselLocomotive;
        public ScriptedTractionCutOffRelay TractionCutOffRelay { get; protected set; }
        protected DieselEngines DieselEngines => DieselLocomotive.DieselEngines;

        public override PowerSupplyType Type => PowerSupplyType.DieselElectric;
        public bool Activated = false;
        private DieselPowerSupply Script => AbstractScript as DieselPowerSupply;

        public float DieselEngineMinRpmForElectricTrainSupply { get; protected set; } = 0f;
        public float DieselEngineMinRpm;

        public ScriptedDieselPowerSupply(MSTSDieselLocomotive locomotive) :
            base(locomotive)
        {
            TractionCutOffRelay = new ScriptedTractionCutOffRelay(this);
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortstractioncutoffrelay":
                case "engine(ortstractioncutoffrelayclosingdelay":
                    TractionCutOffRelay.Parse(lowercasetoken, stf);
                    break;

                case "engine(ortselectrictrainsupply(dieselengineminrpm":
                    DieselEngineMinRpmForElectricTrainSupply = stf.ReadFloatBlock(STFReader.UNITS.None, 0f);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public override void Copy(IPowerSupply other)
        {
            base.Copy(other);

            if (other is ScriptedDieselPowerSupply scriptedOther)
            {
                TractionCutOffRelay.Copy(scriptedOther.TractionCutOffRelay);
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
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as DieselPowerSupply;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Script == null)
                {
                    AbstractScript = new DefaultDieselPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }

            TractionCutOffRelay.Initialize();
        }

        /// <summary>
        /// Initialization when simulation starts with moving train
        /// </summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            TractionCutOffRelay.InitializeMoving();
        }

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            TractionCutOffRelay.Save(outf);
        }

        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            TractionCutOffRelay.Restore(inf);
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            TractionCutOffRelay.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }
    }

    public class DefaultDieselPowerSupply : DieselPowerSupply
    {
        private Timer PowerOnTimer;
        private Timer AuxPowerOnTimer;

        /// <remarks>
        /// Used for the corresponding first engine on/off sound triggers.
        /// </remarks>
        private DieselEngineState PreviousFirstEngineState;
        /// <remarks>
        /// Used for the corresponding second engine on/off sound triggers.
        /// </remarks>
        private DieselEngineState PreviousSecondEngineState;

        private bool QuickPowerOn = false;

        public override void Initialize()
        {
            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());

            PreviousFirstEngineState = CurrentDieselEngineState(0);
            PreviousSecondEngineState = CurrentDieselEngineState(1);
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentDieselEnginesState())
            {
                case DieselEngineState.Stopped:
                case DieselEngineState.Stopping:
                case DieselEngineState.Starting:
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
                    break;

                case DieselEngineState.Running:
                    switch (CurrentTractionCutOffRelayState())
                    {
                        case TractionCutOffRelayState.Open:
                            // If traction cut-off relay is open, then it must be closed to finish the quick power-on sequence
                            if (QuickPowerOn)
                            {
                                QuickPowerOn = false;
                                SignalEventToTractionCutOffRelay(PowerSupplyEvent.CloseTractionCutOffRelay);
                            }

                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();

                            if (CurrentMainPowerSupplyState() == PowerSupplyState.PowerOn)
                            {
                                SignalEvent(Event.EnginePowerOff);
                                SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOff);
                            }
                            break;

                        case TractionCutOffRelayState.Closed:
                            // If traction cut-off relay is closed, quick power-on sequence has finished
                            QuickPowerOn = false;

                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();

                            if (PowerOnTimer.Triggered && CurrentMainPowerSupplyState() == PowerSupplyState.PowerOff)
                            {
                                SignalEvent(Event.EnginePowerOn);
                                SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOn);
                            }
                            break;
                    }

                    if (!AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Start();

                    if (AuxPowerOnTimer.Triggered && CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOff)
                    {
                        SignalEvent(Event.PowerConverterOn);
                        SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                    }
                    break;
            }

            // By default, on diesel locomotives, dynamic brake is available only if main power is available.
            SetCurrentDynamicBrakeAvailability(CurrentMainPowerSupplyState() == PowerSupplyState.PowerOn);

            if (ElectricTrainSupplyUnfitted())
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.Unavailable);
                DieselEngineMinRpm = 0;
            }
            else if (CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn
                    && ElectricTrainSupplySwitchOn())
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.PowerOn);
                DieselEngineMinRpm = DieselEngineMinRpmForElectricTrainSupply;
            }
            else
            {
                SetCurrentElectricTrainSupplyState(PowerSupplyState.PowerOff);
                DieselEngineMinRpm = 0;
            }

            UpdateSounds();
        }

        protected void UpdateSounds()
        {
            // First engine
            if ((PreviousFirstEngineState == DieselEngineState.Stopped
                || PreviousFirstEngineState == DieselEngineState.Stopping)
                && (CurrentDieselEngineState(0) == DieselEngineState.Starting
                || CurrentDieselEngineState(0) == DieselEngineState.Running))
            {
                SignalEvent(Event.EnginePowerOn);
            }
            else if ((PreviousFirstEngineState == DieselEngineState.Starting
                || PreviousFirstEngineState == DieselEngineState.Running)
                && (CurrentDieselEngineState(0) == DieselEngineState.Stopping
                || CurrentDieselEngineState(0) == DieselEngineState.Stopped))
            {
                SignalEvent(Event.EnginePowerOff);
            }
            PreviousFirstEngineState = CurrentDieselEngineState(0);

            // Second engine
            if ((PreviousSecondEngineState == DieselEngineState.Stopped
                || PreviousSecondEngineState == DieselEngineState.Stopping)
                && (CurrentDieselEngineState(1) == DieselEngineState.Starting
                || CurrentDieselEngineState(1) == DieselEngineState.Running))
            {
                SignalEvent(Event.SecondEnginePowerOn);
            }
            else if ((PreviousSecondEngineState == DieselEngineState.Starting
                || PreviousSecondEngineState == DieselEngineState.Running)
                && (CurrentDieselEngineState(1) == DieselEngineState.Stopping
                || CurrentDieselEngineState(1) == DieselEngineState.Stopped))
            {
                SignalEvent(Event.SecondEnginePowerOff);
            }
            PreviousSecondEngineState = CurrentDieselEngineState(1);
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    QuickPowerOn = true;
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToDieselEngines(PowerSupplyEvent.StartEngine);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOnElectricTrainSupply);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    QuickPowerOn = false;
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.SwitchOffElectricTrainSupply);
                    SignalEventToTractionCutOffRelay(PowerSupplyEvent.OpenTractionCutOffRelay);
                    SignalEventToDieselEngines(PowerSupplyEvent.StopEngine);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    break;

                case PowerSupplyEvent.TogglePlayerEngine:
                    switch (CurrentDieselEngineState(0))
                    {
                        case DieselEngineState.Stopped:
                        case DieselEngineState.Stopping:
                            SignalEventToDieselEngine(PowerSupplyEvent.StartEngine, 0);
                            Confirm(CabControl.PlayerDiesel, CabSetting.On);
                            break;

                        case DieselEngineState.Starting:
                            SignalEventToDieselEngine(PowerSupplyEvent.StopEngine, 0);
                            Confirm(CabControl.PlayerDiesel, CabSetting.Off);
                            break;

                        case DieselEngineState.Running:
                            if (ThrottlePercent() < 1)
                            {
                                SignalEventToDieselEngine(PowerSupplyEvent.StopEngine, 0);
                                Confirm(CabControl.PlayerDiesel, CabSetting.Off);
                            }
                            else
                            {
                                Confirm(CabControl.PlayerDiesel, CabSetting.Warn1);
                            }
                            break;
                    }
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
