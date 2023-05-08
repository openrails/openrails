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

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchOnPanel
    {
        public SwitchOnPanelDefinition Definition = new SwitchOnPanelDefinition();
        public SwitchOnPanelStatus Status = new SwitchOnPanelStatus();

        public bool[] IsPressed = new bool[] { false };
        public bool[] IsDown = new bool[] { false };
        public bool[] IsUp = new bool[] { false };

        // almost empty non functioning button
        public void init0(UserCommand userCommand = ORTS.Common.Input.UserCommand.GamePauseMenu, string description = "")
        {
            Definition.NoOffButtons = 0;
            Definition.Button = TypeOfButton.none;
            Definition.UserCommand = new UserCommand[] { userCommand };
            Definition.Description = description;
        }

        // 1 button
        public void init1(UserCommand userCommand, string description, TypeOfButton typeOfButton = TypeOfButton.click)
        {
            Definition.NoOffButtons = 1;
            Definition.Button = typeOfButton;
            Definition.UserCommand = new UserCommand[] { userCommand };
            Definition.Description = description;
        }

        // 2 buttons
        public void init2(UserCommand userCommandTop, UserCommand userCommandBottom, string description, TypeOfButton typeOfButton = TypeOfButton.click)
        {
            Definition.NoOffButtons = 2;
            Definition.Button = typeOfButton;
            Definition.UserCommand = new UserCommand[] { userCommandTop, userCommandBottom };
            Definition.Description = description;
        }

        public void initIs()
        {
            if (Definition.NoOffButtons == 1)
            {
                IsPressed = new bool[] { false };
                IsDown = new bool[] { false };
                IsUp = new bool[] { false };
            }
            if (Definition.NoOffButtons == 2)
            {
                IsPressed = new bool[] { false, false };
                IsDown = new bool[] { false, false };
                IsUp = new bool[] { false, false };
            }
        }
    }
}
