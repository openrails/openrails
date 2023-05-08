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

namespace Orts.Viewer3D.WebServices.SwitchPanel
{
    public class SwitchOnPanelStatus
    {
        public string Status = "";
        public string Color = "";
        public bool Blinking = false;

        public SwitchOnPanelStatus() { }

        public SwitchOnPanelStatus(string status, string color, bool blinking)
        {
            Status = status;
            Color = color;
            Blinking = blinking;
        }

        public override bool Equals(object obj)
        {
            return ((SwitchOnPanelStatus)obj).Status == Status &&
                ((SwitchOnPanelStatus)obj).Color == Color &&
                ((SwitchOnPanelStatus)obj).Blinking == Blinking;
        }

        public static void deepCopy(SwitchOnPanelStatus to, SwitchOnPanelStatus from)
        {
            to.Status = from.Status;
            to.Color = from.Color;
            to.Blinking = from.Blinking;
        }

        public override int GetHashCode()
        {
            var hashCode = -1070463442;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Status);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Color);
            hashCode = hashCode * -1521134295 + Blinking.GetHashCode();
            return hashCode;
        }
    }
}
