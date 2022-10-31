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

using System;
using System.Collections.Generic;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Processed external device data sent to UserInput class
    /// </summary>
    public class ExternalDeviceState
    {
        public ExternalDeviceCabControl Direction = new ExternalDeviceCabControl();      // -100 (reverse) to 100 (forward)
        public ExternalDeviceCabControl Throttle = new ExternalDeviceCabControl();       // 0 to 100
        public ExternalDeviceCabControl DynamicBrake = new ExternalDeviceCabControl();   // 0 to 100 if active otherwise less than 0
        public ExternalDeviceCabControl TrainBrake = new ExternalDeviceCabControl();     // 0 (release) to 100 (CS), does not include emergency
        public ExternalDeviceCabControl EngineBrake = new ExternalDeviceCabControl();    // 0 to 100
        public ExternalDeviceCabControl Lights = new ExternalDeviceCabControl();                  // lights rotary, 1 off, 2 dim, 3 full
        public Dictionary<(CabViewControlType,int), ExternalDeviceCabControl> CabControls;
        public Dictionary<UserCommand, ExternalDeviceButton> Buttons;
        public ExternalDeviceState()
        {
            Buttons = new Dictionary<UserCommand, ExternalDeviceButton>();
            CabControls = new Dictionary<(CabViewControlType,int), ExternalDeviceCabControl>();
        }

        public virtual void Handled()
        {
            Direction.Changed = false;
            Throttle.Changed = false;
            DynamicBrake.Changed = false;
            TrainBrake.Changed = false;
            EngineBrake.Changed = false;
            Lights.Changed = false;
            foreach (var button in Buttons.Values)
            {
                button.Changed = false;
            }
            foreach (var control in CabControls.Values)
            {
                control.Changed = false;
            }
        }

        public bool IsPressed(UserCommand command)
		{
            return Buttons.TryGetValue(command, out var button) && button.IsPressed;
		}

		public bool IsReleased(UserCommand command)
		{
            return Buttons.TryGetValue(command, out var button) && button.IsReleased;
		}

		public bool IsDown(UserCommand command)
		{
            return Buttons.TryGetValue(command, out var button) && button.IsDown;
		}
    }
    public class ExternalDeviceButton
    {
        bool isDown;
        public bool IsDown
        {
            get
            {
                return isDown;
            }
            set
            {
                if (isDown != value)
                {
                    isDown = value;
                    Changed = true;
                }
            }
        }
        public bool IsPressed { get { return IsDown && Changed; } }
        public bool IsReleased { get { return !IsDown && Changed; } }
        public bool Changed;
    }
    public class ExternalDeviceCabControl
    {
        float value;
        public bool Changed;
        public float Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    Changed = true;
                }
            }
        }
    }
}