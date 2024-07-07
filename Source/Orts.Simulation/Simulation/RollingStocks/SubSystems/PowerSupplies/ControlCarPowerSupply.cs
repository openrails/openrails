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

    public class ScriptedControlCarPowerSupply : ScriptedLocomotivePowerSupply, ISubSystem<ScriptedControlCarPowerSupply>
    {
        public MSTSControlTrailerCar ControlTrailer => Locomotive as MSTSControlTrailerCar;

        public override PowerSupplyType Type => PowerSupplyType.ControlCar;

        private ControlCarPowerSupply Script => AbstractScript as ControlCarPowerSupply;
        public ScriptedControlCarPowerSupply(MSTSControlTrailerCar controlcar) :
        base(controlcar)
        {

        }


        public void Copy(ScriptedControlCarPowerSupply other)
        {
            base.Copy(other);
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

        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();

            Script?.InitializeMoving();
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            Script?.Update(elapsedClockSeconds);
        }
    }
    public class DefaultControlCarPowerSupply : ControlCarPowerSupply
    {
        public override void Initialize()
        {

        }
        public override void Update(float elapsedClockSeconds)
        {
            SetCurrentBatteryState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentLowVoltagePowerSupplyState(BatterySwitchOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
            SetCurrentCabPowerSupplyState(BatterySwitchOn() && MasterKeyOn() ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
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
