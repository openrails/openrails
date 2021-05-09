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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;
using ORTS.Scripting.Api.ETCS;
using System.Collections.Generic;

namespace Orts.Viewer3D.RollingStock.SubSystems.ETCS
{
    public class MenuBar
    {
        public readonly IList<DMIButton> Buttons = new List<DMIButton>();
        public MenuBar(DriverMachineInterface dmi)
        {
            // Interface with TCS to be defined in the future
        }
    }
    public class MenuWindow : DMISubwindow
    {
        readonly List<DMIButton> Buttons = new List<DMIButton>();
        public MenuWindow(DMIMenuWindowDefinition definition, DriverMachineInterface dmi) : base(definition.WindowTitle, false, dmi)
        {
            int i = 0;
            foreach (var buttondef in definition.Buttons)
            {
                DMIButton b;
                if (buttondef is DMITextButtonDefinition)
                {
                    b = new DMITextButton((buttondef as DMITextButtonDefinition).Label, buttondef.ConfirmerCaption, buttondef.Enabled, () => { /* TODO */ }, 153, 50, dmi);
                }
                else if (buttondef is DMIIconButtonDefinition)
                {
                    b = new DMIIconButton((buttondef as DMIIconButtonDefinition).EnabledIconName, (buttondef as DMIIconButtonDefinition).DisabledIconName, buttondef.ConfirmerCaption, buttondef.Enabled, () => { /* TODO */ }, 153, 50, dmi);
                }
                else
                {
                    i++;
                    continue;
                }
                Buttons.Add(b);
                AddToLayout(b, new Point(i % 2 * 153, 50 + 50 * (i / 2)));
                i++;
            }
        }
        public MenuWindow(string title, List<DMIButton> buttons, DriverMachineInterface dmi) : base(title, false, dmi)
        {
            int i = 0;
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    Buttons.Add(button);
                    AddToLayout(button, new Point(i % 2 * 153, 50 + 50 * (i / 2)));
                }
                i++;
            }
            Visible = true;
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            base.PrepareFrame(status);
            // TODO: TCS menu interface to be defined
        }
    }
}
