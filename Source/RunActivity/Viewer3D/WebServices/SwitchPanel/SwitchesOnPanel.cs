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

using ORTS.Common.Input;
using ORTS.Scripting.Api;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchesOnPanel
    {
        private const int Cols = 10;
        private const int Rows = 4;

        private static readonly SwitchOnPanel[,] SwitchesOnPanelArray = new SwitchOnPanel[Rows, Cols];
        private static readonly SwitchOnPanel[,] PreviousSwitchesOnPanelArray = new SwitchOnPanel[Rows, Cols];
        private static Viewer Viewer;

        public SwitchesOnPanel(Viewer viewer)
        {
            Viewer = viewer;


            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    SwitchesOnPanelArray[i, j] = new SwitchOnPanel();
                    PreviousSwitchesOnPanelArray[i, j] = new SwitchOnPanel();
                }
            }

            setDefinitionsEmpty(SwitchesOnPanelArray);

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    SwitchesOnPanelArray[i, j].initIs();
                }
            }
        }

        #region privateInitControl

        private static void initEmpty(SwitchOnPanel switchOnPanel) { switchOnPanel.init0(); }

        private static void initControlAlerter(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlAlerter, "reset", TypeOfButton.push); }

        private static void initControlCabinLight(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlLight, "cabin light"); }

        private static void initControlDirection(SwitchOnPanel switchOnPanel) { switchOnPanel.init2(UserCommand.ControlForwards, UserCommand.ControlBackwards, "direction"); }

        private static void initControlDisplayTrackMonitorWindow(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.DisplayTrackMonitorWindow, "Track Monitor"); }

        private static void initControlDisplayTrainDrivingWindow(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.DisplayTrainDrivingWindow, "Train Driving"); }

        private static void initControlDisplayNextStationWindow(SwitchOnPanel switchOnPanel) 
        {
            Orts.Simulation.Activity act = Viewer.Simulator.ActivityRun;
            if ((act != null) && (act.EventList.Count) > 0)
            {
                switchOnPanel.init1(UserCommand.DisplayNextStationWindow, "Activity Monitor");
            }
            else
            {
                switchOnPanel.init0(UserCommand.DisplayNextStationWindow, "Activity Monitor");
            }
        }

        private static void initControlDisplayHUD(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.DisplayHUD, "HUD"); }

        private static void initControlDoorLeft(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlDoorLeft, "left door"); }

        private static void initControlDoorRight(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlDoorRight, "right door"); }

        private static void initControlEmergencyPushButton(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlEmergencyPushButton, "EMERGENCY"); }

        private static void initControlGear(SwitchOnPanel switchOnPanel)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if ((locomotive as MSTSDieselLocomotive).DieselEngines.HasGearBox)
            {
                switchOnPanel.init2(UserCommand.ControlGearUp, UserCommand.ControlGearDown, "gear");
            }
            else
            {
                switchOnPanel.init0(UserCommand.ControlGearUp, "gear");
            }
        }

        private static void initControlHeadLight(SwitchOnPanel switchOnPanel) { switchOnPanel.init2(UserCommand.ControlHeadlightIncrease, UserCommand.ControlHeadlightDecrease, "front light"); }

        private static void initControlMultiPlayerDispatcher(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.GameMultiPlayerDispatcher, "Map"); }

        private static void initControlPantograph1(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlPantograph1, "panto 1"); }

        private static void initControlPantograph2(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlPantograph2, "panto 2"); }

        private static void initControlDieselPlayer(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlDieselPlayer, "engine"); }

        private static void initControlDieselHelper(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlDieselHelper, "helper engine"); }

        private static void initControlSander(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlSander, "sander", TypeOfButton.push); }

        private static void initGameChangeCab(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.GameChangeCab, "change cab"); }

        private static void initGameSwitchManualMode(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.GameSwitchManualMode, "signal mode"); }

        private static void initGameAutopilotMode(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.GameAutopilotMode, "autopilot"); }

        private static void initControlWiper(SwitchOnPanel switchOnPanel) { switchOnPanel.init1(UserCommand.ControlWiper, "wiper"); }

        private static void initControlBatterySwitch(SwitchOnPanel switchOnPanel)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            switch (locomotive.LocomotivePowerSupply.BatterySwitch.Mode)
            {
                case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.AlwaysOn:
                    switchOnPanel.init0(UserCommand.ControlBatterySwitchClose, "battery switch");
                    break;
                case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.Switch:
                    switchOnPanel.init1(UserCommand.ControlBatterySwitchClose, "battery switch");
                    break;
                case Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.PushButtons:
                    switchOnPanel.init2(UserCommand.ControlBatterySwitchClose, UserCommand.ControlBatterySwitchOpen, "battery switch", TypeOfButton.push);
                    break;
            }
        }

        private static void initControlMasterKey(SwitchOnPanel switchOnPanel)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            if (locomotive.LocomotivePowerSupply.MasterKey.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.MasterKey.ModeType.AlwaysOn)
            {
                switchOnPanel.init0(UserCommand.ControlMasterKey, "master key");
            }
            else
            {
                switchOnPanel.init1(UserCommand.ControlMasterKey, "master key");
            }
        }

        private static void initControlCircuitBreaker(SwitchOnPanel switchOnPanel) 
        {
            MSTSElectricLocomotive locomotive = Viewer.PlayerLocomotive as MSTSElectricLocomotive;
            string scriptName = locomotive.ElectricPowerSupply.CircuitBreaker.ScriptName;
            if (scriptName == "Automatic")
            {
                switchOnPanel.init0(UserCommand.ControlCircuitBreakerClosingOrder, "circuit breaker");
            }
            else
            {
                switchOnPanel.init1(UserCommand.ControlCircuitBreakerClosingOrder, "circuit breaker");
            }
        }

        private static void initControlTractionCutOffRelay(SwitchOnPanel switchOnPanel)
        {
            MSTSDieselLocomotive locomotive = Viewer.PlayerLocomotive as MSTSDieselLocomotive;
            string scriptName = locomotive.DieselPowerSupply.TractionCutOffRelay.ScriptName;
            if (scriptName == "Automatic")
            {
                switchOnPanel.init0(UserCommand.ControlTractionCutOffRelayClosingOrder, "traction cut-off");
            }
            else
            {
                switchOnPanel.init1(UserCommand.ControlTractionCutOffRelayClosingOrder, "traction cut-off");
            }
        }

        public static void setDefinitionsEmpty(SwitchOnPanel[,] SwitchesOnPanelArray)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    initEmpty(SwitchesOnPanelArray[i, j]);
                }
            }
        }

        #endregion

        public static void setDefinitions(SwitchOnPanel[,] SwitchesOnPanelArray)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            setDefinitionsEmpty(SwitchesOnPanelArray);

            switch (locomotive.EngineType)
            {
                case TrainCar.EngineTypes.Electric:
                    initControlDoorLeft(SwitchesOnPanelArray[0, 0]);
                    initControlDirection(SwitchesOnPanelArray[0, 1]);
                    initControlCabinLight(SwitchesOnPanelArray[0, 2]);
                    initControlEmergencyPushButton(SwitchesOnPanelArray[0, 3]);
                    initControlAlerter(SwitchesOnPanelArray[0, 4]);
                    initControlSander(SwitchesOnPanelArray[0, 5]);
                    initControlWiper(SwitchesOnPanelArray[0, 6]);
                    initControlDoorRight(SwitchesOnPanelArray[0, 9]);

                    initControlBatterySwitch(SwitchesOnPanelArray[1, 0]);
                    initControlMasterKey(SwitchesOnPanelArray[1, 1]);
                    initControlPantograph1(SwitchesOnPanelArray[1, 2]);
                    initControlPantograph2(SwitchesOnPanelArray[1, 3]);
                    initControlCircuitBreaker(SwitchesOnPanelArray[1, 4]);
                    initControlHeadLight(SwitchesOnPanelArray[1, 5]);

                    initGameChangeCab(SwitchesOnPanelArray[2, 0]);
                    initGameSwitchManualMode(SwitchesOnPanelArray[2, 1]);
                    initGameAutopilotMode(SwitchesOnPanelArray[2, 2]);

                    initControlMultiPlayerDispatcher(SwitchesOnPanelArray[3, 0]);
                    initControlDisplayTrackMonitorWindow(SwitchesOnPanelArray[3, 1]);
                    initControlDisplayTrainDrivingWindow(SwitchesOnPanelArray[3, 2]);
                    initControlDisplayNextStationWindow(SwitchesOnPanelArray[3, 3]);
                    initControlDisplayHUD(SwitchesOnPanelArray[3, 9]);
                    break;
                case TrainCar.EngineTypes.Diesel:
                    initControlDoorLeft(SwitchesOnPanelArray[0, 0]);
                    initControlDirection(SwitchesOnPanelArray[0, 1]);
                    initControlGear(SwitchesOnPanelArray[0, 2]);
                    initControlCabinLight(SwitchesOnPanelArray[0, 3]);
                    initControlEmergencyPushButton(SwitchesOnPanelArray[0, 4]);
                    initControlAlerter(SwitchesOnPanelArray[0, 5]);
                    initControlSander(SwitchesOnPanelArray[0, 6]);
                    initControlWiper(SwitchesOnPanelArray[0, 6]);
                    initControlDoorRight(SwitchesOnPanelArray[0, 9]);

                    initControlBatterySwitch(SwitchesOnPanelArray[1, 0]);
                    initControlMasterKey(SwitchesOnPanelArray[1, 1]);
                    initControlDieselPlayer(SwitchesOnPanelArray[1, 2]);
                    initControlDieselHelper(SwitchesOnPanelArray[1, 3]);
                    initControlTractionCutOffRelay(SwitchesOnPanelArray[1, 4]);
                    initControlHeadLight(SwitchesOnPanelArray[1, 5]);
                    
                    initGameChangeCab(SwitchesOnPanelArray[2, 0]);
                    initGameSwitchManualMode(SwitchesOnPanelArray[2, 1]);
                    initGameAutopilotMode(SwitchesOnPanelArray[2, 2]);

                    initControlMultiPlayerDispatcher(SwitchesOnPanelArray[3, 0]);
                    initControlDisplayTrackMonitorWindow(SwitchesOnPanelArray[3, 1]);
                    initControlDisplayTrainDrivingWindow(SwitchesOnPanelArray[3, 2]);
                    initControlDisplayNextStationWindow(SwitchesOnPanelArray[3, 3]);
                    initControlDisplayHUD(SwitchesOnPanelArray[3, 9]);
                    break;
                case TrainCar.EngineTypes.Steam:
                    initControlDoorLeft(SwitchesOnPanelArray[0, 0]);
                    initControlDoorRight(SwitchesOnPanelArray[0, 9]);
                    initControlMultiPlayerDispatcher(SwitchesOnPanelArray[3, 0]);
                    initControlDisplayTrackMonitorWindow(SwitchesOnPanelArray[3, 1]);
                    initControlDisplayTrainDrivingWindow(SwitchesOnPanelArray[3, 2]);
                    initControlDisplayNextStationWindow(SwitchesOnPanelArray[3, 3]);
                    initControlDisplayHUD(SwitchesOnPanelArray[3, 9]);
                    break;
                case TrainCar.EngineTypes.Control:
                    // currently do not know what to do with this type

                    initControlMultiPlayerDispatcher(SwitchesOnPanelArray[3, 0]);
                    initControlDisplayTrackMonitorWindow(SwitchesOnPanelArray[3, 1]);
                    initControlDisplayTrainDrivingWindow(SwitchesOnPanelArray[3, 2]);
                    initControlDisplayNextStationWindow(SwitchesOnPanelArray[3, 3]);
                    initControlDisplayHUD(SwitchesOnPanelArray[3, 9]);
                    break;
            }
        }

        public static SwitchOnPanel[,] GetSwitchesOnPanelArray()
        {
            return SwitchesOnPanelArray;
        }

        public static SwitchOnPanel[,] getPreviousSwitchesOnPanelArray()
        {
            return PreviousSwitchesOnPanelArray;
        }

        private enum isTypeOfButtonAction
        {
            pressed,
            down,
            up
        }

        public static void setIsPressed(UserCommand userCommand)
        {
            setIs(userCommand, isTypeOfButtonAction.pressed);
        }

        public static void setIsDown(UserCommand userCommand)
        {
            setIs(userCommand, isTypeOfButtonAction.down);
        }

        public static void setIsUp(UserCommand userCommand)
        {
            setIs(userCommand, isTypeOfButtonAction.up);
        }

        private static void setIs(UserCommand userCommand, isTypeOfButtonAction isTypeOfButtonAction)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    for (int k = 0; k < SwitchesOnPanelArray[i, j].Definition.NoOffButtons; k++)
                    {
                        if (SwitchesOnPanelArray[i, j].Definition.UserCommand[k] == userCommand)
                        {
                            if (isTypeOfButtonAction == isTypeOfButtonAction.pressed)
                                SwitchesOnPanelArray[i, j].IsPressed[k] = true;
                            if (isTypeOfButtonAction == isTypeOfButtonAction.down)
                                SwitchesOnPanelArray[i, j].IsDown[k] = true;
                            if (isTypeOfButtonAction == isTypeOfButtonAction.up)
                                SwitchesOnPanelArray[i, j].IsUp[k] = true;
                        }
                    }
                }
            }
        }

        public static bool IsPressed(UserCommand userCommand)
        {
            return Is(userCommand, isTypeOfButtonAction.pressed);
        }

        public static bool IsDown(UserCommand userCommand)
        {
            return Is(userCommand, isTypeOfButtonAction.down);
        }

        public static bool IsUp(UserCommand userCommand)
        {
            return Is(userCommand, isTypeOfButtonAction.up);
        }

        private static bool Is(UserCommand userCommand, isTypeOfButtonAction isTypeOfButtonAction)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    if (SwitchesOnPanelArray[i, j] != null)
                    {
                        for (int k = 0; k < SwitchesOnPanelArray[i, j].Definition.NoOffButtons; k++)
                        {
                            if (SwitchesOnPanelArray[i, j].Definition.UserCommand[k] == userCommand)
                            {
                                if (isTypeOfButtonAction == isTypeOfButtonAction.pressed)
                                {
                                    if (SwitchesOnPanelArray[i, j].IsPressed[k])
                                    {
                                        SwitchesOnPanelArray[i, j].IsPressed[k] = false;
                                        return true;
                                    } 
                                    else
                                    {
                                        return false;
                                    }
                                }
                                    
                                if (isTypeOfButtonAction == isTypeOfButtonAction.down)
                                {
                                    if (SwitchesOnPanelArray[i, j].IsDown[k])
                                    {
                                        SwitchesOnPanelArray[i, j].IsDown[k] = false;
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                if (isTypeOfButtonAction == isTypeOfButtonAction.up)
                                {
                                    if (SwitchesOnPanelArray[i, j].IsUp[k])
                                    {
                                        SwitchesOnPanelArray[i, j].IsUp[k] = false;
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        #region privateGetStatus

        private static void getStatusDoors(UserCommand userCommand, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            DoorState door;
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            bool flipped = locomotive.GetCabFlipped() ^ locomotive.Flipped;

            if (userCommand == UserCommand.ControlDoorLeft)
                door = flipped ? locomotive.Doors.RightDoor.State : locomotive.Doors.LeftDoor.State;
            else
                door = flipped ? locomotive.Doors.LeftDoor.State : locomotive.Doors.RightDoor.State;

            if (door == DoorState.Open)
            {
                switchOnPanelStatus.Color = locomotive.AbsSpeedMpS > 0.1f ? "red" : "orange";
                switchOnPanelStatus.Blinking = locomotive.AbsSpeedMpS > 0.1f;
            }
            if ((door == DoorState.Opening) || (door == DoorState.Closing))
            {
                switchOnPanelStatus.Color = locomotive.AbsSpeedMpS > 0.1f ? "red" : "darkorange";
                switchOnPanelStatus.Blinking = true;
            }
            if (door == DoorState.Closed)
            {
                switchOnPanelStatus.Color = "";
                switchOnPanelStatus.Blinking = false;
            }
            switchOnPanelStatus.Status = door.ToString();
        }

        private static void getStatusControlPantograph(UserCommand userCommand, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            int pantographIndex = (int)userCommand - (int)UserCommand.ControlPantograph1;

            if (locomotive.Pantographs.List[pantographIndex].State == PantographState.Up)
            {
                switchOnPanelStatus.Color = "lightblue";
            }
            if ((locomotive.Pantographs.List[pantographIndex].State == PantographState.Raising) ||
                (locomotive.Pantographs.List[pantographIndex].State == PantographState.Lowering))
            {
                switchOnPanelStatus.Color = "lightblue";
                switchOnPanelStatus.Blinking = true;
            }
            switchOnPanelStatus.Status = locomotive.Pantographs.List[pantographIndex].State.ToString();
        }

        private static void getStatusControlHeadlight(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switch (locomotive.Headlight)
            {
                case 0:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Off");
                    break;
                case 1:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Dim");
                    switchOnPanelStatus.Color = "lightyellow";
                    break;
                case 2:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Bright");
                    switchOnPanelStatus.Color = "lightblue";
                    break;
            }
        }

        private static void getStatusControlCablight(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switchOnPanelStatus.Status = locomotive.CabLightOn ? Viewer.Catalog.GetString("On") : Viewer.Catalog.GetString("Off");
            switchOnPanelStatus.Color = locomotive.CabLightOn ? "lightyellow" : "";
        }

        private static void getStatusControlDirection(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switch (locomotive.Direction)
            {
                case ORTS.Common.Direction.Forward:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Forward");
                    switchOnPanelStatus.Color = "lightgreen";
                    break;
                case ORTS.Common.Direction.Reverse:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Backward");
                    switchOnPanelStatus.Color = "orange";
                    break;
                case ORTS.Common.Direction.N:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Neutral");
                    break;
            }
        }

        private static void getStatusControlSander(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.Sander)
                switchOnPanelStatus.Color = "yellow";
        }

        private static void getStatusControlWiper(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.Wiper)
                switchOnPanelStatus.Color = "blue";
        }

        private static void getStatusControlEmergencyPushButton(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.EmergencyButtonPressed)
            {
                switchOnPanelStatus.Status = "SET";
                switchOnPanelStatus.Color = "red";
                switchOnPanelStatus.Blinking = true;
            }
            else
            {
                switchOnPanelStatus.Color = "#FFCCCB"; // lightred
            }
        }

        private static void getStatusGameControlMode(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            switch (Viewer.PlayerTrain.ControlMode)
            {
                case Train.TRAIN_CONTROL.AUTO_SIGNAL:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Auto Signal");
                    switchOnPanelStatus.Color = "lightgreen";
                    break;
                case Train.TRAIN_CONTROL.AUTO_NODE:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Node");
                    break;
                case Train.TRAIN_CONTROL.MANUAL:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Manual");
                    switchOnPanelStatus.Color = "#FFCCCB"; // lightred
                    break;
                case Train.TRAIN_CONTROL.EXPLORER:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Explorer");
                    break;
                case Train.TRAIN_CONTROL.OUT_OF_CONTROL:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("OutOfControl");
                    switchOnPanelStatus.Color = "red";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case Train.TRAIN_CONTROL.INACTIVE:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Inactive");
                    break;
                case Train.TRAIN_CONTROL.TURNTABLE:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Turntable");
                    break;
                case Train.TRAIN_CONTROL.UNDEFINED:
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Unknown");
                    break;
            }
        }

        private static void getStatusGameAutopilotMode(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            var autopilot = (locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING);
            if (autopilot)
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("On");
                switchOnPanelStatus.Color = "lightgreen";
            }
            else
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Off");
            }
        }

        private static void getStatusMasterKey(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.LocomotivePowerSupply.MasterKey.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.MasterKey.ModeType.AlwaysOn)
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Always On");
                switchOnPanelStatus.Color = "lightgray";
            }
            else
            {
                if (locomotive.LocomotivePowerSupply.MasterKey.On)
                {
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("On");
                    switchOnPanelStatus.Color = "lightblue";
                }
                else
                {
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Off");
                }
            }
        }

        private static void getStatusBatterySwitch(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.LocomotivePowerSupply.BatterySwitch.Mode == Simulation.RollingStocks.SubSystems.PowerSupplies.BatterySwitch.ModeType.AlwaysOn)
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Always On");
                switchOnPanelStatus.Color = "lightgray";
            }
            else
            {
                if (locomotive.LocomotivePowerSupply.BatterySwitch.On)
                {
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("On");
                    switchOnPanelStatus.Color = "lightblue";
                }
                else
                {
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Off");
                }
            }
        }

        private static void getStatusCircuitBreaker(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSElectricLocomotive locomotive = Viewer.PlayerLocomotive as MSTSElectricLocomotive;

            switchOnPanelStatus.Status = "";
            switchOnPanelStatus.Color = "";

            string scriptName = locomotive.ElectricPowerSupply.CircuitBreaker.ScriptName;
            if (scriptName == "Automatic")
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Automatic") + " <br> ";
                switchOnPanelStatus.Color = "lightgray";
            }

            switch (locomotive.ElectricPowerSupply.CircuitBreaker.State)
            {
                case CircuitBreakerState.Closing:
                    if (switchOnPanelStatus.Color == "")
                        switchOnPanelStatus.Color = "lightblue";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case CircuitBreakerState.Closed:
                    if (switchOnPanelStatus.Color == "")
                        switchOnPanelStatus.Color = "lightblue";
                    break;
            }
            switchOnPanelStatus.Status += locomotive.ElectricPowerSupply.CircuitBreaker.State.ToString();
        }

        private static void getStatusTractionCutOffRelay(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSDieselLocomotive locomotive = Viewer.PlayerLocomotive as MSTSDieselLocomotive;

            switchOnPanelStatus.Status = "";
            switchOnPanelStatus.Color = "";

            string scriptName = locomotive.DieselPowerSupply.TractionCutOffRelay.ScriptName;
            if (scriptName == "Automatic")
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Automatic") + " <br> ";
                switchOnPanelStatus.Color = "lightgray";
            }

            switch (locomotive.DieselPowerSupply.TractionCutOffRelay.State)
            {
                case TractionCutOffRelayState.Closing:
                    if (switchOnPanelStatus.Color == "")
                        switchOnPanelStatus.Color = "lightblue";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case TractionCutOffRelayState.Closed:
                    if (switchOnPanelStatus.Color == "")
                        switchOnPanelStatus.Color = "lightblue";
                    break;
            }
            switchOnPanelStatus.Status += locomotive.DieselPowerSupply.TractionCutOffRelay.State.ToString();
        }

        private static void getStatusDieselEnginePlayerHelper(MSTSDieselLocomotive Locomotive, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            switch (Locomotive.DieselEngines.State)
            {
                case DieselEngineState.Stopped:
                    break;
                case DieselEngineState.Starting:
                    switchOnPanelStatus.Color = "lightblue";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case DieselEngineState.Running:
                    switchOnPanelStatus.Color = "lightblue";
                    break;
                case DieselEngineState.Stopping:
                    switchOnPanelStatus.Color = "lightblue";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case DieselEngineState.Unavailable:
                    switchOnPanelStatus.Color = "lightgray";
                    break;
            }
            switchOnPanelStatus.Status = (Locomotive as MSTSDieselLocomotive).DieselEngines.State.ToString();
        }

        private static void getStatusDieselEngine(UserCommand userCommand, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            Train train = (Viewer.PlayerLocomotive as MSTSLocomotive).Train;

            int count = 0;
            foreach (TrainCar trainCar in train.Cars)
            {
                if (trainCar.GetType() == typeof(MSTSDieselLocomotive))
                {
                    count++;
                    MSTSDieselLocomotive locomotive = trainCar as MSTSDieselLocomotive;
                    if ((count == 1) && (userCommand == UserCommand.ControlDieselPlayer))
                    {
                        getStatusDieselEnginePlayerHelper(locomotive, ref switchOnPanelStatus);
                    }
                    if ((count == 2) && (userCommand == UserCommand.ControlDieselHelper))
                    {
                        getStatusDieselEnginePlayerHelper(locomotive, ref switchOnPanelStatus);
                    }
                }
            }
        }

        #endregion

        public static void getStatus(UserCommand userCommand, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            switchOnPanelStatus.Status = "";
            switchOnPanelStatus.Color = "";
            switchOnPanelStatus.Blinking = false;

            switch (userCommand)
            {
                case UserCommand.ControlDoorLeft:
                case UserCommand.ControlDoorRight:
                    getStatusDoors(userCommand, ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlPantograph1:
                case UserCommand.ControlPantograph2:
                case UserCommand.ControlPantograph3:
                case UserCommand.ControlPantograph4:
                    getStatusControlPantograph(userCommand, ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlHeadlightIncrease:
                case UserCommand.ControlHeadlightDecrease:
                    getStatusControlHeadlight(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlLight:
                    getStatusControlCablight(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlBackwards:
                case UserCommand.ControlForwards:
                    getStatusControlDirection(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlSander:
                    getStatusControlSander(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlWiper:
                    getStatusControlWiper(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlEmergencyPushButton:
                    getStatusControlEmergencyPushButton(ref switchOnPanelStatus);
                    break;
                case UserCommand.GameSwitchManualMode:
                    getStatusGameControlMode(ref switchOnPanelStatus);
                    break;
                case UserCommand.GameAutopilotMode:
                    getStatusGameAutopilotMode(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlMasterKey:
                    getStatusMasterKey(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlBatterySwitchClose:
                    getStatusBatterySwitch(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlCircuitBreakerClosingOrder:
                    getStatusCircuitBreaker(ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlDieselPlayer:
                    getStatusDieselEngine(UserCommand.ControlDieselPlayer, ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlDieselHelper:
                    getStatusDieselEngine(UserCommand.ControlDieselHelper, ref switchOnPanelStatus);
                    break;
                case UserCommand.ControlTractionCutOffRelayClosingOrder:
                    getStatusTractionCutOffRelay(ref switchOnPanelStatus);
                    break;
            }
        }
    }

}
