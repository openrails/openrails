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
using Orts.Simulation.RollingStocks;

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchesOnPanel
    {
        private const int Cols = 10;
        private const int Rows = 4;

        public readonly SwitchOnPanel[,] SwitchesOnPanelArray = new SwitchOnPanel[Rows, Cols];
        public readonly SwitchOnPanel[,] PreviousSwitchesOnPanelArray = new SwitchOnPanel[Rows, Cols];
        private readonly Viewer Viewer;

        public SwitchesOnPanel(Viewer viewer)
        {
            Viewer = viewer;

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    SwitchesOnPanelArray[i, j] = new SwitchOnPanel(Viewer);
                    PreviousSwitchesOnPanelArray[i, j] = new SwitchOnPanel(Viewer);
                }
            }

            foreach (SwitchOnPanel switchOnPanel in SwitchesOnPanelArray)
                switchOnPanel.InitDefinitionEmpty();

            foreach (SwitchOnPanel switchOnPanel in SwitchesOnPanelArray)
                switchOnPanel.InitIs();
        }

        public void SetDefinitions(SwitchOnPanel[,] SwitchesOnPanelArray)
        {
            MSTSLocomotive locomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            foreach (SwitchOnPanel switchOnPanel in SwitchesOnPanelArray)
                switchOnPanel.InitDefinitionEmpty();

            switch (locomotive.EngineType)
            {
                case TrainCar.EngineTypes.Electric:
                    SwitchesOnPanelArray[0, 0].InitDefinition(UserCommand.ControlDoorLeft);
                    SwitchesOnPanelArray[0, 1].InitDefinition(UserCommand.ControlForwards);
                    SwitchesOnPanelArray[0, 2].InitDefinition(UserCommand.ControlLight);
                    SwitchesOnPanelArray[0, 3].InitDefinition(UserCommand.ControlEmergencyPushButton); 
                    SwitchesOnPanelArray[0, 4].InitDefinition(UserCommand.ControlAlerter);
                    SwitchesOnPanelArray[0, 5].InitDefinition(UserCommand.ControlSander);
                    SwitchesOnPanelArray[0, 6].InitDefinition(UserCommand.ControlWiper);
                    SwitchesOnPanelArray[0, 9].InitDefinition(UserCommand.ControlDoorRight);

                    SwitchesOnPanelArray[1, 0].InitDefinition(UserCommand.ControlBatterySwitchClose);
                    SwitchesOnPanelArray[1, 1].InitDefinition(UserCommand.ControlMasterKey);
                    SwitchesOnPanelArray[1, 2].InitDefinition(UserCommand.ControlPantograph1);
                    SwitchesOnPanelArray[1, 3].InitDefinition(UserCommand.ControlPantograph2);
                    SwitchesOnPanelArray[1, 4].InitDefinition(UserCommand.ControlCircuitBreakerClosingOrder);
                    SwitchesOnPanelArray[1, 5].InitDefinition(UserCommand.ControlHeadlightIncrease);
                    SwitchesOnPanelArray[1, 6].InitDefinition(UserCommand.ControlHandbrakeFull);
                    SwitchesOnPanelArray[1, 7].InitDefinition(UserCommand.ControlBrakeHoseConnect);
                    SwitchesOnPanelArray[1, 8].InitDefinition(UserCommand.ControlRetainersOn);
                    break;

                case TrainCar.EngineTypes.Diesel:
                    SwitchesOnPanelArray[0, 0].InitDefinition(UserCommand.ControlDoorLeft);
                    SwitchesOnPanelArray[0, 1].InitDefinition(UserCommand.ControlForwards);
                    SwitchesOnPanelArray[0, 2].InitDefinition(UserCommand.ControlGearUp);
                    SwitchesOnPanelArray[0, 3].InitDefinition(UserCommand.ControlLight);
                    SwitchesOnPanelArray[0, 4].InitDefinition(UserCommand.ControlEmergencyPushButton);
                    SwitchesOnPanelArray[0, 5].InitDefinition(UserCommand.ControlAlerter);
                    SwitchesOnPanelArray[0, 6].InitDefinition(UserCommand.ControlSander);
                    SwitchesOnPanelArray[0, 7].InitDefinition(UserCommand.ControlWiper);
                    SwitchesOnPanelArray[0, 9].InitDefinition(UserCommand.ControlDoorRight);

                    SwitchesOnPanelArray[1, 0].InitDefinition(UserCommand.ControlBatterySwitchClose);
                    SwitchesOnPanelArray[1, 1].InitDefinition(UserCommand.ControlMasterKey);
                    SwitchesOnPanelArray[1, 2].InitDefinition(UserCommand.ControlDieselPlayer);
                    SwitchesOnPanelArray[1, 3].InitDefinition(UserCommand.ControlDieselHelper);
                    SwitchesOnPanelArray[1, 4].InitDefinition(UserCommand.ControlTractionCutOffRelayClosingOrder);
                    SwitchesOnPanelArray[1, 5].InitDefinition(UserCommand.ControlHeadlightIncrease);
                    SwitchesOnPanelArray[1, 6].InitDefinition(UserCommand.ControlHandbrakeFull);
                    SwitchesOnPanelArray[1, 7].InitDefinition(UserCommand.ControlBrakeHoseConnect);
                    SwitchesOnPanelArray[1, 8].InitDefinition(UserCommand.ControlRetainersOn);
                    break;

                case TrainCar.EngineTypes.Steam:
                    SwitchesOnPanelArray[0, 0].InitDefinition(UserCommand.ControlDoorLeft);
                    SwitchesOnPanelArray[0, 1].InitDefinition(UserCommand.ControlForwards);
                    SwitchesOnPanelArray[0, 2].InitDefinition(UserCommand.ControlCylinderCocks);
                    SwitchesOnPanelArray[0, 3].InitDefinition(UserCommand.ControlLight);
                    SwitchesOnPanelArray[0, 4].InitDefinition(UserCommand.ControlEmergencyPushButton);
                    SwitchesOnPanelArray[0, 5].InitDefinition(UserCommand.ControlAlerter);
                    SwitchesOnPanelArray[0, 6].InitDefinition(UserCommand.ControlSander);
                    SwitchesOnPanelArray[0, 7].InitDefinition(UserCommand.ControlWiper);
                    SwitchesOnPanelArray[0, 9].InitDefinition(UserCommand.ControlDoorRight);

                    SwitchesOnPanelArray[1, 0].InitDefinition(UserCommand.ControlBatterySwitchClose);
                    SwitchesOnPanelArray[1, 1].InitDefinition(UserCommand.ControlMasterKey);
                    SwitchesOnPanelArray[1, 2].InitDefinition(UserCommand.ControlHeadlightIncrease);
                    SwitchesOnPanelArray[1, 3].InitDefinition(UserCommand.ControlHandbrakeFull);
                    SwitchesOnPanelArray[1, 4].InitDefinition(UserCommand.ControlBrakeHoseConnect);
                    SwitchesOnPanelArray[1, 5].InitDefinition(UserCommand.ControlRetainersOn);
                    break;

                case TrainCar.EngineTypes.Control:
                    // currently do not know what to do with this type as I have no example
                    SwitchesOnPanelArray[0, 0].InitDefinition(UserCommand.ControlDoorLeft);
                    SwitchesOnPanelArray[0, 9].InitDefinition(UserCommand.ControlDoorRight);
                    break;
            }

            SwitchesOnPanelArray[2, 0].InitDefinition(UserCommand.GameChangeCab);
            SwitchesOnPanelArray[2, 1].InitDefinition(UserCommand.GameSwitchManualMode);
            SwitchesOnPanelArray[2, 2].InitDefinition(UserCommand.GameAutopilotMode);
            SwitchesOnPanelArray[2, 3].InitDefinition(UserCommand.GameSwitchAhead);
            SwitchesOnPanelArray[2, 4].InitDefinition(UserCommand.GameSwitchBehind);
            SwitchesOnPanelArray[2, 5].InitDefinition(UserCommand.GameClearSignalForward);

            SwitchesOnPanelArray[3, 0].InitDefinition(UserCommand.GameMultiPlayerDispatcher);
            SwitchesOnPanelArray[3, 1].InitDefinition(UserCommand.DisplayTrackMonitorWindow);
            SwitchesOnPanelArray[3, 2].InitDefinition(UserCommand.DisplayTrainDrivingWindow);
            SwitchesOnPanelArray[3, 3].InitDefinition(UserCommand.DisplayNextStationWindow);
            SwitchesOnPanelArray[3, 4].InitDefinition(UserCommand.DisplaySwitchWindow);
            SwitchesOnPanelArray[3, 5].InitDefinition(UserCommand.DisplayTrainCarOperationsWindow);
            SwitchesOnPanelArray[3, 6].InitDefinition(UserCommand.DisplayTrainDpuWindow);
            SwitchesOnPanelArray[3, 7].InitDefinition(UserCommand.DisplayTrainListWindow);
            SwitchesOnPanelArray[3, 8].InitDefinition(UserCommand.DisplayEOTListWindow);
            SwitchesOnPanelArray[3, 9].InitDefinition(UserCommand.DisplayHUD);
        }

        private enum isTypeOfButtonAction
        {
            isPressed,
            isReleased
        }

        public void SetIsPressed(UserCommand userCommand)
        {
            setIs(userCommand, isTypeOfButtonAction.isPressed);
        }

        public void SetIsReleased(UserCommand userCommand)
        {
            setIs(userCommand, isTypeOfButtonAction.isReleased);
        }

        private void setIs(UserCommand userCommand, isTypeOfButtonAction isTypeOfButtonAction)
        {
            foreach (SwitchOnPanel switchOnPanel in SwitchesOnPanelArray)
            {
                for (int k = 0; k < switchOnPanel.Definition.NoOffButtons; k++)
                {
                    if (switchOnPanel.Definition.UserCommand[k] == userCommand)
                    {
                        if (isTypeOfButtonAction == isTypeOfButtonAction.isPressed)
                            switchOnPanel.IsPressed[k] = true;
                        if (isTypeOfButtonAction == isTypeOfButtonAction.isReleased)
                            switchOnPanel.IsReleased[k] = true;
                    }
                }
            }
        }

        public bool IsPressed(UserCommand userCommand)
        {
            if (Is(userCommand, isTypeOfButtonAction.isPressed))
            {
                switch (userCommand)
                {
                    case UserCommand.DisplayTrackMonitorWindow:
                        IsPressedDisplayTrackMonitorWindow();
                        return false;
                    case UserCommand.DisplayTrainDrivingWindow:
                        IsPressedDisplayTrainDrivingWindow();
                        return false;
                    case UserCommand.DisplayHUD:
                        IsPressedDisplayHUD();
                        return false;
                    case UserCommand.DisplayTrainDpuWindow:
                        IsPressedDisplayDpuWindow();
                        return false;
                    default:
                        return true;
                }
            } 
            else
            {
                return false;
            }
        }

        public bool IsReleased(UserCommand userCommand)
        {
            return Is(userCommand, isTypeOfButtonAction.isReleased);
        }

        private bool Is(UserCommand userCommand, isTypeOfButtonAction isTypeOfButtonAction)
        {
            foreach (SwitchOnPanel switchOnPanel in SwitchesOnPanelArray)
            {
                for (int k = 0; k < switchOnPanel.Definition.NoOffButtons; k++)
                {
                    if (switchOnPanel.Definition.UserCommand[k] == userCommand)
                    {
                        switch (isTypeOfButtonAction)
                        {
                            case isTypeOfButtonAction.isPressed:
                                if (switchOnPanel.IsPressed[k])
                                {
                                    switchOnPanel.IsPressed[k] = false;
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            case isTypeOfButtonAction.isReleased:
                                if (switchOnPanel.IsReleased[k])
                                {
                                    switchOnPanel.IsReleased[k] = false;
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
            return false;
        }

        private void IsPressedDisplayTrackMonitorWindow()
        {
            // handle display track monitor from switch panel:
            // cycle between: off --> static only --> all --> off

            if (!Viewer.TrackMonitorWindow.Visible)
            {
                // track monitor window not visible
                Viewer.TrackMonitorWindow.Visible = true;
                if (Viewer.TrackMonitorWindow.Monitor.Mode == Popups.TrackMonitor.DisplayMode.All)
                {
                    Viewer.TrackMonitorWindow.Monitor.CycleMode();
                }
            }
            else
            {
                // visible
                if (Viewer.TrackMonitorWindow.Monitor.Mode == Popups.TrackMonitor.DisplayMode.StaticOnly)
                {
                    Viewer.TrackMonitorWindow.Monitor.CycleMode();
                }
                else
                {
                    Viewer.TrackMonitorWindow.Visible = false;
                }
            }
        }

        private void IsPressedDisplayTrainDrivingWindow()
        {
            // handle display train driving info from switch panel:
            // cycle between: off --> normal text mode --> abreviated --> off

            if (!Viewer.TrainDrivingWindow.Visible)
            {
                // train driving info window not visible
                Viewer.TrainDrivingWindow.Visible = true;
                if (!Viewer.TrainDrivingWindow.normalTextMode)
                {
                    Viewer.TrainDrivingWindow.CycleMode();
                }
            }
            else
            {
                // visible
                if (Viewer.TrainDrivingWindow.normalTextMode)
                {
                    Viewer.TrainDrivingWindow.CycleMode();
                }
                else
                {
                    Viewer.TrainDrivingWindow.Visible = false;
                }
            }
        }

        private void IsPressedDisplayHUD()
        {
            if (!Viewer.HUDWindow.Visible)
            {
                // HUD not visible
                Viewer.HUDWindow.TextPage = 0;
                Viewer.HUDWindow.Visible = true;
            }
            else
            {
                // visible
                if (Viewer.HUDWindow.TextPage < Viewer.HUDWindow.TextPagesLength - 1)
                {
                    // next HUD page
                    Viewer.HUDWindow.TabAction();

                }
                else
                {
                    Viewer.HUDWindow.Visible = false;
                    Viewer.HUDWindow.TextPage = 0;
                }
            }
        }

        private void IsPressedDisplayDpuWindow()
        {
            if (!Viewer.TrainDpuWindow.Visible)
            {
                // DpuWindow not visible
                Viewer.TrainDpuWindow.normalTextMode = true;
                Viewer.TrainDpuWindow.normalVerticalMode = true;
                Viewer.TrainDpuWindow.Visible = true;
                Viewer.TrainDpuWindow.UpdateWindowSize();
            }
            else
            {
                // visible
                if ((Viewer.TrainDpuWindow.normalTextMode == true) && (Viewer.TrainDpuWindow.normalVerticalMode == true))
                {
                    Viewer.TrainDpuWindow.normalVerticalMode = false;
                    Viewer.TrainDpuWindow.UpdateWindowSize();
                    return;
                }

                if ((Viewer.TrainDpuWindow.normalTextMode == true) && (Viewer.TrainDpuWindow.normalVerticalMode == false))
                {
                    Viewer.TrainDpuWindow.normalTextMode = false;
                    Viewer.TrainDpuWindow.normalVerticalMode = true;
                    Viewer.TrainDpuWindow.UpdateWindowSize();
                    return;
                }

                if ((Viewer.TrainDpuWindow.normalTextMode == false) && (Viewer.TrainDpuWindow.normalVerticalMode == true))
                {
                    Viewer.TrainDpuWindow.normalVerticalMode = false;
                    Viewer.TrainDpuWindow.UpdateWindowSize();
                    return;
                }

                if ((Viewer.TrainDpuWindow.normalTextMode == false) && (Viewer.TrainDpuWindow.normalVerticalMode == false))
                {
                    Viewer.TrainDpuWindow.normalTextMode = true;
                    Viewer.TrainDpuWindow.normalVerticalMode = true;
                    Viewer.TrainDpuWindow.Visible = false;
                    Viewer.TrainDpuWindow.UpdateWindowSize();
                    return;
                }
            }
        }

        public bool IsChanged()
        {
            bool changed = false;

            SetDefinitions(SwitchesOnPanelArray);

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    SwitchOnPanel switchOnPanel = SwitchesOnPanelArray[i, j];
                    SwitchOnPanel previousSwitchOnPanel = PreviousSwitchesOnPanelArray[i, j];

                    if (!switchOnPanel.Definition.Equals(previousSwitchOnPanel.Definition))
                    {
                        SwitchOnPanelDefinition.DeepCopy(previousSwitchOnPanel.Definition, switchOnPanel.Definition);
                        // initIs just in case the amount of buttons has changed after a cab change
                        switchOnPanel.InitIs();
                        changed = true;
                    }

                    SwitchOnPanelStatus.getStatus(switchOnPanel.Definition.UserCommand[0], ref switchOnPanel.Status);
                    if (!switchOnPanel.Status.Equals(previousSwitchOnPanel.Status))
                    {
                        SwitchOnPanelStatus.DeepCopy(previousSwitchOnPanel.Status, switchOnPanel.Status);
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }

}
