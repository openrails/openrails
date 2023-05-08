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

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchOnPanelDefinition
    {
        public int NoOffButtons = 0;
        public TypeOfButton Button = TypeOfButton.none;
        public UserCommand[] UserCommand = { ORTS.Common.Input.UserCommand.GamePauseMenu };
        public string Description = "";

        public SwitchOnPanelDefinition() { }

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

            return ((SwitchOnPanelDefinition)obj).Button == Button &&
                ((SwitchOnPanelDefinition)obj).Description == Description;
        }

        public static void deepCopy(SwitchOnPanelDefinition to, SwitchOnPanelDefinition from)
        {
            to.NoOffButtons = from.NoOffButtons;
            to.Button = from.Button;
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
            hashCode = hashCode * -1521134295 + NoOffButtons.GetHashCode();
            hashCode = hashCode * -1521134295 + Button.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<UserCommand[]>.Default.GetHashCode(UserCommand);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
            return hashCode;
        }
    }
}
