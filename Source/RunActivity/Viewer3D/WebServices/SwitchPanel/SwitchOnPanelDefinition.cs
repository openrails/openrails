// COPYRIGHT 2023 by the Open Rails project.
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
//

using System.Collections.Generic;
using Orts.Simulation.RollingStocks;
using ORTS.Common.Input;
using ORTS.Common;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchOnPanelDefinition
    {
        private readonly Viewer Viewer;

        public int NoOffButtons = 0;
        public UserCommand[] UserCommand = { ORTS.Common.Input.UserCommand.GamePauseMenu };
        public string Description = "";

        public SwitchOnPanelDefinition(Viewer viewer)
        {
            Viewer = viewer;
        }

        public void init(UserCommand userCommand)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switch (userCommand)
            {
                case ORTS.Common.Input.UserCommand.ControlBatterySwitchClose:
                    switch (locomotive.LocomotivePowerSupply.BatterySwitch.Mode)
                    {
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.AlwaysOn:
                            init0(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose, GetParticularString("SwitchPanel", "Battery switch"));
                            break;
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.Switch:
                            init1(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose, GetParticularString("SwitchPanel", "Battery switch"));
                            break;
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.PushButtons:
                            init2(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose, ORTS.Common.Input.UserCommand.ControlBatterySwitchOpen, GetParticularString("SwitchPanel", "Battery switch"));
                            break;
                    }
                    break;

                case ORTS.Common.Input.UserCommand.ControlMasterKey:
                    if (locomotive.LocomotivePowerSupply.MasterKey.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.MasterKey.ModeType.AlwaysOn)
                        init0(ORTS.Common.Input.UserCommand.ControlMasterKey, GetParticularString("SwitchPanel", "Master key"));
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlMasterKey, GetParticularString("SwitchPanel", "Master key"));
                    break;

                case ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder:
                    if ((locomotive as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.ScriptName == "Automatic")
                        init0(ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder, GetParticularString("SwitchPanel", "Circuit breaker"));
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder, GetParticularString("SwitchPanel", "Circuit breaker"));
                    break;

                case ORTS.Common.Input.UserCommand.ControlGearUp:
                    if ((locomotive as MSTSDieselLocomotive).DieselEngines.HasGearBox)
                        init2(ORTS.Common.Input.UserCommand.ControlGearUp, ORTS.Common.Input.UserCommand.ControlGearDown, GetParticularString("SwitchPanel", "Gear"));
                    else
                        init0(ORTS.Common.Input.UserCommand.ControlGearUp, GetParticularString("SwitchPanel", "Gear"));
                    break;

                case ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder:
                    if ((locomotive as MSTSDieselLocomotive).DieselPowerSupply.TractionCutOffRelay.ScriptName == "Automatic")
                        init0(ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder, GetParticularString("SwitchPanel", "Traction cut-off"));
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder, GetParticularString("SwitchPanel", "Traction cut-off"));
                    break;

                case ORTS.Common.Input.UserCommand.DisplayNextStationWindow:
                    Orts.Simulation.Activity act = Viewer.Simulator.ActivityRun;
                    if ((act != null) && (act.EventList.Count) > 0)
                        init1(ORTS.Common.Input.UserCommand.DisplayNextStationWindow, GetParticularStringAttribute.GetParticularPrettyName(userCommand, "SwitchPanel"));
                    else
                        init0(ORTS.Common.Input.UserCommand.DisplayNextStationWindow, GetParticularStringAttribute.GetParticularPrettyName(userCommand, "SwitchPanel"));
                    break;

                case ORTS.Common.Input.UserCommand.ControlHeadlightIncrease:
                    init2(ORTS.Common.Input.UserCommand.ControlHeadlightIncrease, ORTS.Common.Input.UserCommand.ControlHeadlightDecrease, GetParticularString("SwitchPanel", "Front Light"));
                    break;

                case ORTS.Common.Input.UserCommand.ControlForwards:
                    init2(ORTS.Common.Input.UserCommand.ControlForwards, ORTS.Common.Input.UserCommand.ControlBackwards, GetParticularString("SwitchPanel", "Direction"));
                    break;

                default:
                    string description = determineDescription(userCommand);

                    init1(userCommand, description);
                    break;
            }
        }

        public void initEmpty()
        {
            init0();
        }

        private string determineDescription(UserCommand userCommand)
        {
            string description = GetParticularStringAttribute.GetParticularPrettyName(userCommand, "SwitchPanel");

            description = capitalize(Viewer.Catalog.GetParticularString("SwitchPanel", description));

            return description;
        }

        private string GetParticularString(string context, string description)
        {
            return capitalize(Viewer.Catalog.GetParticularString(context, description));
        }

        private string capitalize(string value)
        {
            if (value.Length > 0)
            {
                return char.ToUpper(value[0]) + value.Substring(1);
            } 
            else
            {
                return "";
            }
        }

        // almost empty non functioning button
        private void init0(UserCommand userCommand = ORTS.Common.Input.UserCommand.GamePauseMenu, string description = "")
        {
            NoOffButtons = 0;
            UserCommand = new UserCommand[] { userCommand };
            Description = description;
        }

        // 1 button
        private void init1(UserCommand userCommand, string description)
        {
            NoOffButtons = 1;
            UserCommand = new UserCommand[] { userCommand };
            Description = description;
        }

        // 2 buttons
        private void init2(UserCommand userCommandTop, UserCommand userCommandBottom, string description)
        {
            NoOffButtons = 2;
            UserCommand = new UserCommand[] { userCommandTop, userCommandBottom };
            Description = description;
        }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            if (((SwitchOnPanelDefinition)obj).NoOffButtons != this.NoOffButtons)
            {
                return false;
            }

            if (NoOffButtons > 0)
            {
                for (int i = 0; i < NoOffButtons; i++)
                {
                    if (((SwitchOnPanelDefinition)obj).UserCommand[i] != UserCommand[i])
                    {
                        return false;
                    }
                }
            }

            return ((SwitchOnPanelDefinition)obj).Description == Description;
        }

        public static void deepCopy(SwitchOnPanelDefinition to, SwitchOnPanelDefinition from)
        {
            to.NoOffButtons = from.NoOffButtons;
            to.Description = from.Description;

            to.UserCommand = new UserCommand[from.NoOffButtons];
            for (int i = 0; i < from.NoOffButtons; i++)
            {
                to.UserCommand[i] = from.UserCommand[i];
            }
        }

        public override int GetHashCode()
        {
            var hashCode = 1410623761;
            hashCode = (hashCode * -1521134295) + NoOffButtons.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<UserCommand[]>.Default.GetHashCode(UserCommand);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Description);
            return hashCode;
        }
    }
}
