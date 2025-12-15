// COPYRIGHT 2022 by the Open Rails project.
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

using System.Diagnostics;
using System.IO;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    /// <summary>
    /// Basic power supply class for steam locomotives
    /// For electrical systems powered by battery
    /// </summary>
    public class ScriptedSteamPowerSupply : ScriptedLocomotivePowerSupply
    {
        public MSTSSteamLocomotive SteamLocomotive => Locomotive as MSTSSteamLocomotive;
        public override PowerSupplyType Type => PowerSupplyType.Steam;
        public ScriptedSteamPowerSupply(MSTSSteamLocomotive locomotive) : base(locomotive)
        {
            ElectricTrainSupplySwitch = new ElectricTrainSupplySwitch(Locomotive, ElectricTrainSupplySwitch.ModeType.Unfitted);
        }
        public override void Initialize()
        {
            base.Initialize();

            if (AbstractScript == null)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    Trace.TraceWarning("Skipped custom power supply script, not available for steam locomotives.");
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (AbstractScript == null)
                {
                    AbstractScript = new DefaultSteamPowerSupply();
                }

                AssignScriptFunctions();

                AbstractScript.AttachToHost(this);
                AbstractScript.Initialize();
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            AbstractScript?.Update(elapsedClockSeconds);
        }
    }
    public class DefaultSteamPowerSupply : LocomotivePowerSupply
    {
        public override void Initialize()
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);

            SetCurrentMainPowerSupplyState(PowerSupplyState.PowerOn);
            SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState.PowerOn);

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
                    SignalEventToBatterySwitch(PowerSupplyEvent.QuickPowerOn);
                    SignalEventToMasterKey(PowerSupplyEvent.TurnOnMasterKey);
                    SignalEventToPantograph(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToOtherTrainVehiclesWithId(PowerSupplyEvent.RaisePantograph, 1);
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.QuickPowerOn);
                    break;

                case PowerSupplyEvent.QuickPowerOff:
                    SignalEventToElectricTrainSupplySwitch(PowerSupplyEvent.QuickPowerOff);
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
