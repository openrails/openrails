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
        private readonly Viewer Viewer;

        public SwitchOnPanelDefinition Definition;
        public SwitchOnPanelStatus Status;

        public bool[] IsPressed = new bool[] { false };
        public bool[] IsReleased = new bool[] { false };

        public SwitchOnPanel(Viewer viewer)
        {
            Viewer = viewer;
            Definition = new SwitchOnPanelDefinition(Viewer);
            Status = new SwitchOnPanelStatus(Viewer);
        }

        public void InitDefinition(UserCommand userCommand)
        {
            Definition.Init(userCommand);
        }

        public void InitDefinitionEmpty()
        {
            Definition.InitEmpty();
        }

        public void InitIs()
        {
            if (Definition.NoOffButtons == 1)
            {
                IsPressed = new bool[] { false };
                IsReleased = new bool[] { false };
            }
            if (Definition.NoOffButtons == 2)
            {
                IsPressed = new bool[] { false, false };
                IsReleased = new bool[] { false, false };
            }
        }
    }
}
