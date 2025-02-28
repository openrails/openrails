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
using ORTS.Common.Input;
using ORTS.Scripting.Api;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System;
using System.Diagnostics;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchOnPanelStatus
    {
        public string Status = "";
        public string Color = "";
        public bool Blinking = false;

        private static Viewer Viewer;
        private static List<UserCommand> ExceptionForCommand = new List<UserCommand>();

        public SwitchOnPanelStatus(Viewer viewer)
        {
            Viewer = viewer;
        }

        public SwitchOnPanelStatus(string status, string color, bool blinking)
        {
            Status = status;
            Color = color;
            Blinking = blinking;
        }

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
            switchOnPanelStatus.Status = Viewer.Catalog.GetParticularString("Door", GetStringAttribute.GetPrettyName(door));
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
            switchOnPanelStatus.Status = Viewer.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(locomotive.Pantographs.List[pantographIndex].State));
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

            switchOnPanelStatus.Color = locomotive.CabLightOn ? "lightyellow" : "";
            switchOnPanelStatus.Status = locomotive.CabLightOn ? Viewer.Catalog.GetString("On") : Viewer.Catalog.GetString("Off");
        }

        private static void getStatusControlDirection(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            switch (locomotive.Direction)
            {
                case ORTS.Common.Direction.Forward:
                    switchOnPanelStatus.Color = "lightgreen";
                    break;
                case ORTS.Common.Direction.Reverse:
                    switchOnPanelStatus.Color = "orange";
                    break;
            }
            switchOnPanelStatus.Status = Viewer.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(locomotive.Direction));
            if (locomotive.EngineType == TrainCar.EngineTypes.Steam)
            {
                switchOnPanelStatus.Status += " " + Math.Abs(Convert.ToInt32(locomotive.Train.MUReverserPercent)) + "%";
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

        private static void getStatusControlAlerterPushButton(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            if (locomotive.AlerterSnd)
            {
                switchOnPanelStatus.Status = "Alerter";
                switchOnPanelStatus.Color = "red";
                switchOnPanelStatus.Blinking = true;
            }
        }

        private static void getStatusGameControlMode(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            switch (Viewer.PlayerTrain.ControlMode)
            {
                case Train.TRAIN_CONTROL.AUTO_SIGNAL:
                    switchOnPanelStatus.Color = "lightgreen";
                    break;
                case Train.TRAIN_CONTROL.AUTO_NODE:
                    break;
                case Train.TRAIN_CONTROL.MANUAL:
                    switchOnPanelStatus.Color = "#FFCCCB"; // lightred
                    break;
                case Train.TRAIN_CONTROL.EXPLORER:
                    break;
                case Train.TRAIN_CONTROL.OUT_OF_CONTROL:
                    switchOnPanelStatus.Color = "red";
                    switchOnPanelStatus.Blinking = true;
                    break;
                case Train.TRAIN_CONTROL.INACTIVE:
                    break;
                case Train.TRAIN_CONTROL.TURNTABLE:
                    break;
                case Train.TRAIN_CONTROL.UNDEFINED:
                    break;
            }
            switchOnPanelStatus.Status = Viewer.Catalog.GetParticularString("TrainControl", GetStringAttribute.GetPrettyName(Viewer.PlayerTrain.ControlMode));
        }

        private static void getStatusGameAutopilotMode(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            var autopilot = (locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || locomotive.Train.Autopilot);
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
            switchOnPanelStatus.Status += Viewer.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(locomotive.ElectricPowerSupply.CircuitBreaker.State));
        }

        private static void getStatusTractionCutOffRelay(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSDieselLocomotive locomotive = Viewer.PlayerLocomotive as MSTSDieselLocomotive;

            switchOnPanelStatus.Status = "";
            switchOnPanelStatus.Color = "";

            string scriptName = locomotive.DieselPowerSupply.TractionCutOffRelay.ScriptName;
            if (scriptName == "Automatic")
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Always on") + " <br> ";
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
            switchOnPanelStatus.Status += Viewer.Catalog.GetParticularString("TractionCutOffRelay", GetStringAttribute.GetPrettyName(locomotive.DieselPowerSupply.TractionCutOffRelay.State));
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
            switchOnPanelStatus.Status = Viewer.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName((Locomotive as MSTSDieselLocomotive).DieselEngines.State));
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

        private static void getStatusHandbrake(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            Train train = Viewer.PlayerLocomotive.Train;

            int handBrakeCount = 0;
            int handBrakeOn = 0;

            for (int i = 0; i < train.Cars.Count; i++)
            {
                if ((train.Cars[i] as MSTSWagon).MSTSBrakeSystem.HandBrakePresent)
                {
                    handBrakeCount++;
                    if ((train.Cars[i] as MSTSWagon).GetTrainHandbrakeStatus())
                        handBrakeOn++;
                }
            }

            if ((handBrakeOn > 0) && (handBrakeCount != handBrakeOn))
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("On") + "/" + Viewer.Catalog.GetString("Off");
                switchOnPanelStatus.Color = "orange";
            } else
            {
                if (handBrakeOn > 0)
                {
                    switchOnPanelStatus.Status = Viewer.Catalog.GetString("Full");
                    switchOnPanelStatus.Color = "orange";
                }
                else
                {
                    if (handBrakeCount > 0)
                    {
                        switchOnPanelStatus.Status = Viewer.Catalog.GetString("Off");
                    }
                }
            }
        }

        private static void getStatusBrakehose(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            Train train = Viewer.PlayerLocomotive.Train;
            int brakeHoseConnectedCount = 0;
            int angleCockAOpenCount = 0;
            int angleCockBOpenCount = 0;

            for (int i = 0; i < train.Cars.Count; i++)
            {
                TrainCar car = train.Cars[i];
                if ((train.Cars[i].BrakeSystem.FrontBrakeHoseConnected) && (i > 0))
                    brakeHoseConnectedCount++;
                if ((train.Cars[i].BrakeSystem.AngleCockAOpen) && (i > 0))
                    angleCockAOpenCount++;
                if ((train.Cars[i].BrakeSystem.AngleCockBOpen) && (i < (train.Cars.Count - 1)))
                    angleCockBOpenCount++;
            }

            if ((brakeHoseConnectedCount == (train.Cars.Count - 1)) &&
                (angleCockAOpenCount == (train.Cars.Count - 1)) &&
                (angleCockBOpenCount == (train.Cars.Count - 1))) {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Connected");
                switchOnPanelStatus.Color = "lightgreen";
            }
        }

        private static void getStatusCylinderCocks(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            MSTSSteamLocomotive locomotive = Viewer.PlayerLocomotive as MSTSSteamLocomotive;

            bool cylinderCocksAreOpen = locomotive.CylinderCocksAreOpen;
            if (locomotive.CylinderCocksAreOpen)
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Open");
                switchOnPanelStatus.Color = "orange";
            }
            else
            {
                switchOnPanelStatus.Status = Viewer.Catalog.GetString("Closed");

            }
        }

        private static void getStatusRetainers(ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            Train train = Viewer.PlayerLocomotive.Train;

            if (train.RetainerSetting != Orts.Simulation.RollingStocks.SubSystems.Brakes.RetainerSetting.Exhaust)
            {
                switchOnPanelStatus.Status = train.RetainerPercent + "% " + Viewer.Catalog.GetString(GetStringAttribute.GetPrettyName(train.RetainerSetting));
                switchOnPanelStatus.Color = "orange";
            }
        }

        public static void getStatus(UserCommand userCommand, ref SwitchOnPanelStatus switchOnPanelStatus)
        {
            switchOnPanelStatus.Status = "";
            switchOnPanelStatus.Color = "";
            switchOnPanelStatus.Blinking = false;

            try
            {
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
                    case UserCommand.ControlAlerter:
                        getStatusControlAlerterPushButton(ref switchOnPanelStatus);
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
                    case UserCommand.ControlHandbrakeFull:
                        getStatusHandbrake(ref switchOnPanelStatus);
                        break;
                    case UserCommand.ControlBrakeHoseConnect:
                        getStatusBrakehose(ref switchOnPanelStatus);
                        break;
                    case UserCommand.ControlCylinderCocks:
                        getStatusCylinderCocks(ref switchOnPanelStatus);
                        break;
                    case UserCommand.ControlRetainersOn:
                        getStatusRetainers(ref switchOnPanelStatus);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionForCommand.Contains(userCommand))
                {
                    // exception not yet logged
                    ExceptionForCommand.Add(userCommand);

                    Trace.WriteLine("Error in Switch Panel function \"getStatus\" getting status for " + userCommand + ":");
                    Trace.WriteLine(ex);
                }
            }
        }

        public override bool Equals(object obj)
        {
            return ((SwitchOnPanelStatus)obj).Status == Status &&
                ((SwitchOnPanelStatus)obj).Color == Color &&
                ((SwitchOnPanelStatus)obj).Blinking == Blinking;
        }

        public static void DeepCopy(SwitchOnPanelStatus to, SwitchOnPanelStatus from)
        {
            to.Status = from.Status;
            to.Color = from.Color;
            to.Blinking = from.Blinking;
        }

        public override int GetHashCode()
        {
            var hashCode = -1070463442;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Status);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Color);
            hashCode = (hashCode * -1521134295) + Blinking.GetHashCode();
            return hashCode;
        }
    }
}
