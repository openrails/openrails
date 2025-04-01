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
using Orts.Simulation.Physics;

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

        public void Init(UserCommand userCommand)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switch (userCommand)
            {
                case ORTS.Common.Input.UserCommand.ControlBatterySwitchClose:
                    switch (locomotive.LocomotivePowerSupply.BatterySwitch.Mode)
                    {
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.AlwaysOn:
                            init0(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose);
                            break;
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.Switch:
                            init1(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose);
                            break;
                        case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.PushButtons:
                            init2(ORTS.Common.Input.UserCommand.ControlBatterySwitchClose, ORTS.Common.Input.UserCommand.ControlBatterySwitchOpen);
                            break;
                    }
                    break;

                case ORTS.Common.Input.UserCommand.ControlMasterKey:
                    if (locomotive.LocomotivePowerSupply.MasterKey.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.MasterKey.ModeType.AlwaysOn)
                        init0(ORTS.Common.Input.UserCommand.ControlMasterKey);
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlMasterKey);
                    break;

                case ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder:
                    if ((locomotive as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.ScriptName == "Automatic")
                        init0(ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder);
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder);
                    break;

                case ORTS.Common.Input.UserCommand.ControlGearUp:
                    if ((locomotive as MSTSDieselLocomotive).DieselEngines.HasGearBox)
                        init2(ORTS.Common.Input.UserCommand.ControlGearUp, ORTS.Common.Input.UserCommand.ControlGearDown);
                    else
                        init0(ORTS.Common.Input.UserCommand.ControlGearUp);
                    break;

                case ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder:
                    if ((locomotive as MSTSDieselLocomotive).DieselPowerSupply.TractionCutOffRelay.ScriptName == "Automatic")
                        init0(ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder);
                    else
                        init1(ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder);
                    break;

                case ORTS.Common.Input.UserCommand.ControlHeadlightIncrease:
                    init2(ORTS.Common.Input.UserCommand.ControlHeadlightIncrease, ORTS.Common.Input.UserCommand.ControlHeadlightDecrease);
                    break;

                case ORTS.Common.Input.UserCommand.ControlForwards:
                    init2(ORTS.Common.Input.UserCommand.ControlForwards, ORTS.Common.Input.UserCommand.ControlBackwards);
                    break;

                case ORTS.Common.Input.UserCommand.ControlHandbrakeFull:
                    Train train = Viewer.PlayerLocomotive.Train;

                    int handBrakeCount = 0;

                    for (int i = 0; i < train.Cars.Count; i++)
                    {
                        if ((train.Cars[i] as MSTSWagon).MSTSBrakeSystem.HandBrakePresent)
                            handBrakeCount++;
                    }

                    if (handBrakeCount > 0)
                        init2(ORTS.Common.Input.UserCommand.ControlHandbrakeFull, ORTS.Common.Input.UserCommand.ControlHandbrakeNone);
                    else
                        init0(ORTS.Common.Input.UserCommand.ControlHandbrakeFull);
                    break;

                case ORTS.Common.Input.UserCommand.ControlBrakeHoseConnect:
                    init2(ORTS.Common.Input.UserCommand.ControlBrakeHoseConnect, ORTS.Common.Input.UserCommand.ControlBrakeHoseDisconnect);
                    break;

                case ORTS.Common.Input.UserCommand.ControlRetainersOn:
                    init2(ORTS.Common.Input.UserCommand.ControlRetainersOn, ORTS.Common.Input.UserCommand.ControlRetainersOff);
                    break;

                default:
                    init1(userCommand);
                    break;
            }
        }

        // empty button
        public void InitEmpty()
        {
            NoOffButtons = 0;
            UserCommand = new UserCommand[] { ORTS.Common.Input.UserCommand.GamePauseMenu };
            Description = "";
        }

        // almost empty non functioning button
        private void init0(UserCommand userCommand)
        {
            NoOffButtons = 0;
            UserCommand = new UserCommand[] { userCommand };
            Description = determineDescription(userCommand);
        }

        // 1 button
        private void init1(UserCommand userCommand)
        {
            NoOffButtons = 1;
            UserCommand = new UserCommand[] { userCommand };
            Description = determineDescription(userCommand);
        }

        // 2 buttons
        private void init2(UserCommand userCommandTop, UserCommand userCommandBottom)
        {
            NoOffButtons = 2;
            UserCommand = new UserCommand[] { userCommandTop, userCommandBottom };
            Description = determineDescription(userCommandTop);
        }

        private string determineDescription(UserCommand userCommand)
        {
            string description = "";

            /*
             * this code could be simplified, however the GetParticularString is necessary to make translations possible
             */
            switch (userCommand)
            {
                case ORTS.Common.Input.UserCommand.GameSwitchAhead:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Switch Ahead");
                    break;
                case ORTS.Common.Input.UserCommand.GameSwitchBehind:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Switch Behind");
                    break;
                case ORTS.Common.Input.UserCommand.GameChangeCab:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Change Cab");
                    break;
                case ORTS.Common.Input.UserCommand.GameMultiPlayerDispatcher:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Map");
                    break;
                case ORTS.Common.Input.UserCommand.GameSwitchManualMode:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Switch Manual");
                    break;
                case ORTS.Common.Input.UserCommand.GameClearSignalForward:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Clear Signal Forward");
                    break;
                case ORTS.Common.Input.UserCommand.GameAutopilotMode:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Autopilot");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayTrackMonitorWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Track Monitor");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayHUD:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "HUD");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayTrainDrivingWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Train Driving");
                    break;
                case ORTS.Common.Input.UserCommand.DisplaySwitchWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Switch");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayTrainCarOperationsWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Train Operations");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayTrainDpuWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Train Dpu");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayNextStationWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Next Station");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayTrainListWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Train List");
                    break;
                case ORTS.Common.Input.UserCommand.DisplayEOTListWindow:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "EOT List");
                    break;
                case ORTS.Common.Input.UserCommand.ControlForwards:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Direction");
                    break;
                case ORTS.Common.Input.UserCommand.ControlGearUp:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Gear");
                    break;
                case ORTS.Common.Input.UserCommand.ControlHandbrakeFull:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Handbrake");
                    break;
                case ORTS.Common.Input.UserCommand.ControlBrakeHoseConnect:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Brake hose");
                    break;
                case ORTS.Common.Input.UserCommand.ControlAlerter:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Alerter");
                    break;
                case ORTS.Common.Input.UserCommand.ControlEmergencyPushButton:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Emergency");
                    break;
                case ORTS.Common.Input.UserCommand.ControlSander:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Sander");
                    break;
                case ORTS.Common.Input.UserCommand.ControlWiper:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Wiper");
                    break;
                case ORTS.Common.Input.UserCommand.ControlDoorLeft:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Door Left");
                    break;
                case ORTS.Common.Input.UserCommand.ControlDoorRight:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Door Right");
                    break;
                case ORTS.Common.Input.UserCommand.ControlLight:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Light");
                    break;
                case ORTS.Common.Input.UserCommand.ControlPantograph1:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Pantograph 1");
                    break;
                case ORTS.Common.Input.UserCommand.ControlPantograph2:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Pantograph 2");
                    break;
                case ORTS.Common.Input.UserCommand.ControlBatterySwitchClose:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Battery Switch");
                    break;
                case ORTS.Common.Input.UserCommand.ControlMasterKey:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Master Key");
                    break;
                case ORTS.Common.Input.UserCommand.ControlCircuitBreakerClosingOrder:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Circuit Breaker");
                    break;
                case ORTS.Common.Input.UserCommand.ControlTractionCutOffRelayClosingOrder:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Traction Cut-Off");
                    break;
                case ORTS.Common.Input.UserCommand.ControlDieselPlayer:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Diesel Player");
                    break;
                case ORTS.Common.Input.UserCommand.ControlDieselHelper:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Diesel Helper");
                    break;
                case ORTS.Common.Input.UserCommand.ControlHeadlightIncrease:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Front light");
                    break;
                case ORTS.Common.Input.UserCommand.ControlCylinderCocks:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Cyl Cocks");
                    break;
                case ORTS.Common.Input.UserCommand.ControlRetainersOn:
                    description = Viewer.Catalog.GetParticularString("SwitchPanel", "Retainers");
                    break;
            }

            return description;
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

        public static void DeepCopy(SwitchOnPanelDefinition to, SwitchOnPanelDefinition from)
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
