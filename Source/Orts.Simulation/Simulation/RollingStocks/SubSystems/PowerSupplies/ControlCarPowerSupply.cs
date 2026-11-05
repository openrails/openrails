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
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedControlCarPowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSControlTrailerCar ControlTrailer => Locomotive as MSTSControlTrailerCar;

        public override PowerSupplyType Type => PowerSupplyType.ControlCar;

        private ControlCarPowerSupply Script => AbstractScript as ControlCarPowerSupply;

        public ScriptedControlCarPowerSupply(MSTSControlTrailerCar controlcar) :
        base(controlcar)
        {
            ElectricTrainSupplyDirection = PowerSupplyDirection.Sink;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Script == null)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    string[] pathArray = { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    AbstractScript = Simulator.ScriptManager.Load(pathArray, ScriptName) as ControlCarPowerSupply;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Script == null)
                {
                    AbstractScript = new DefaultControlCarPowerSupply();
                }

                AssignScriptFunctions();

                Script.AttachToHost(this);
                Script.Initialize();
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }
    }
    public class DefaultControlCarPowerSupply : ControlCarPowerSupply
    {
        private Timer AuxPowerOnTimer;
        PowerSupplyLoad AuxContinuousLoads = new PowerSupplyLoad(PowerSupplyLoadType.Other);
        public override void Initialize()
        {
            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
            AuxiliaryPowerSupplyLoads.Add(AuxContinuousLoads);
        }
        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            switch (CurrentElectricTrainSupplyState())
            {
                case PowerSupplyState.PowerOff:
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();
                    if (CurrentAuxiliaryPowerSupplyState() != PowerSupplyState.PowerOff)
                    {
                        SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOff);
                        SignalEvent(Event.PowerConverterOff);
                    }
                    break;

                case PowerSupplyState.PowerOn:
                    if (!AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Start();
                    switch (CurrentAuxiliaryPowerSupplyState())
                    {
                        case PowerSupplyState.PowerOff:
                            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOnOngoing);
                            break;
                        case PowerSupplyState.PowerOnOngoing:
                            if (AuxPowerOnTimer.Triggered)
                            {
                                SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);
                                SignalEvent(Event.PowerConverterOn);
                            }
                            break;
                    }
                    break;
            }
            AuxContinuousLoads.CurrentPowerW = CurrentAuxiliaryPowerSupplyState() == PowerSupplyState.PowerOn ? AuxContinuousPowerW() : 0;
            ElectricTrainSupplyPowerW = AuxiliaryPowerSupplyPowerW;
        }
        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.QuickPowerOn:
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOff);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOffMasterKey);
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                case PowerSupplyEvent.TogglePlayerEngine:
                    SignalEventToControlActiveLocomotive(evt);
                    break;

                default:
                    base.HandleEvent(evt);
                    break;
            }
        }
    }
}
