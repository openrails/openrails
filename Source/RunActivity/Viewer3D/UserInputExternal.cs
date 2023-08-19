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
        public Dictionary<(CabViewControlType,int), ExternalDeviceCabControl> CabControls;
        public Dictionary<UserCommand, ExternalDeviceButton> Commands;
        public ExternalDeviceState()
        {
            Commands = new Dictionary<UserCommand, ExternalDeviceButton>();
            CabControls = new Dictionary<(CabViewControlType,int), ExternalDeviceCabControl>();
        }

        public virtual void Handled()
        {
            foreach (var button in Commands.Values)
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
            return Commands.TryGetValue(command, out var button) && button.IsPressed;
		}

		public bool IsReleased(UserCommand command)
		{
            return Commands.TryGetValue(command, out var button) && button.IsReleased;
		}

		public bool IsDown(UserCommand command)
		{
            return Commands.TryGetValue(command, out var button) && button.IsDown;
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