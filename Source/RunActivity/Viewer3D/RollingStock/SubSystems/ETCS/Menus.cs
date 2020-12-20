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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orts.Viewer3D.RollingStock.SubSystems.ETCS
{
    public class MenuBar
    {
        public readonly List<DMIButton> Buttons = new List<DMIButton>();
        public MenuBar(DriverMachineInterface dmi)
        {
            /*var main = new DMITextButton("Main", "Main", true, () =>
            {
                var buts = new List<DMIButton>();
                buts.Add(new DMITextButton("Start", "Start", true, null, 153, 50, dmi));
                var driverid = new DMITextButton("Driver ID", "Driver ID", true, () =>
                {
                    var fields = new List<DMIDataEntryValue>();
                    var val = new DMIDataEntryValue();
                    val.Name = "Level";
                    val.Keyboard = new DMIKeyboard(DMIKeyboard.KeyboardType.Alphanumeric);
                    fields.Add(val);
                    var idwnd = new DataEntryWindow(new DMIDataEntryDefinition("Driver ID", fields, false, null, null), dmi);
                    dmi.ShowSubwindow(idwnd);
                }, 153, 50, dmi);
                driverid.Enabled = true;
                buts.Add(driverid);
                var traindata = new DMITextButton("Train data", "Train data", true, () =>
                {
                    var fields = new List<DMIDataEntryValue>();
                    var val = new DMIDataEntryValue();
                    val.Name = "Train category";
                    List<string> keys = new List<string>();
                    val.Keyboard = new DMIKeyboard(keys);
                    keys.Add("PASS 1");
                    keys.Add("PASS 2");
                    keys.Add("PASS 3");
                    keys.Add("TILT 1");
                    keys.Add("TILT 2");
                    keys.Add("TILT 3");
                    keys.Add("TILT 4");
                    keys.Add("TILT 5");
                    keys.Add("TILT 6");
                    keys.Add("TILT 7");
                    keys.Add("FP 1");
                    keys.Add("FP 2");
                    keys.Add("FP 3");
                    keys.Add("FP 4");
                    keys.Add("FG 1");
                    keys.Add("FG 2");
                    keys.Add("FG 3");
                    keys.Add("FG 4");
                    fields.Add(val);
                    val.Name = "Length (m)";
                    val.Keyboard = new DMIKeyboard(DMIKeyboard.KeyboardType.Numeric);
                    fields.Add(val);
                    val.Name = "Brake percentage";
                    val.Keyboard = new DMIKeyboard(DMIKeyboard.KeyboardType.Numeric);
                    fields.Add(val);
                    val.Name = "Maximum speed (km/h)";
                    val.Keyboard = new DMIKeyboard(DMIKeyboard.KeyboardType.Numeric);
                    fields.Add(val);
                    val.Name = "Airtight";
                    keys = new List<string>();
                    keys.Add("Yes");
                    keys.Add("No");
                    val.Keyboard = new DMIKeyboard(keys);
                    fields.Add(val);
                    val.Name = "Loading gauge";
                    keys = new List<string>();
                    keys.Add("G1");
                    keys.Add("GA");
                    keys.Add("GB");
                    keys.Add("GC");
                    keys.Add("Out of GC");
                    val.Keyboard = new DMIKeyboard(keys);
                    fields.Add(val);
                    var datawnd = new DataEntryWindow(new DMIDataEntryDefinition("Train data", fields, true, null, null), dmi);
                    dmi.ShowSubwindow(datawnd);
                }, 153, 50, dmi);
                traindata.Enabled = true;
                buts.Add(traindata);
                buts.Add(null);
                var level = new DMITextButton("Level", "Level", true, () =>
                {
                    var fields = new List<DMIDataEntryValue>();
                    var val = new DMIDataEntryValue();
                    val.Name = "Level";
                    List<string> keys = new List<string>();
                    keys.Add("Level 0");
                    keys.Add("Level 1");
                    keys.Add("Level 2");
                    keys.Add("Level 3");
                    keys.Add("SCMT");
                    keys.Add("LZB");
                    val.Keyboard = new DMIKeyboard(keys);
                    fields.Add(val);
                    var levwnd = new DataEntryWindow(new DMIDataEntryDefinition("Level", fields, false, null, null), dmi);
                    dmi.ShowSubwindow(levwnd);
                }, 153, 50, dmi);
                level.Enabled = true;
                buts.Add(level);
                var trn = new DMITextButton("Train running n.", "Train running number", true, () =>
                {
                    var fields = new List<DMIDataEntryValue>();
                    var val = new DMIDataEntryValue();
                    val.Name = "Train running number";
                    val.Keyboard = new DMIKeyboard(DMIKeyboard.KeyboardType.Numeric);
                    fields.Add(val);
                    var trnwnd = new DataEntryWindow(new DMIDataEntryDefinition("Train running number", fields, false, null, null), dmi);
                    dmi.ShowSubwindow(trnwnd);
                }, 153, 50, dmi);
                trn.Enabled = true;
                buts.Add(trn);
                var shunt = new DMITextButton("Shunting", "Shunting", true, null, 153, 50, dmi);
                shunt.DelayType = true;
                buts.Add(shunt);
                buts.Add(new DMITextButton("Non Leading", "Non Leading", true, null, 153, 50, dmi));
                buts.Add(new DMITextButton("Maintain Shunt.", "Maintain Shunting", true, null, 153, 50, dmi));
                buts.Add(new DMITextButton("Radio data", "Radio data", true, null, 153, 50, dmi));
                var wnd = new MenuWindow("Main", buts, dmi);
                dmi.ShowSubwindow(wnd);
            }, 60, 50, dmi);
            main.Enabled = true;
            Buttons.Add(main);
            var ov = new DMITextButton("Over-\nride", "Override", true, () =>
            {
                var buts = new List<DMIButtonDefinition>();
                buts.Add(new DMIButtonDefinition("EoA", false));
                var wnd = new MenuWindow(new DMIMenuWindowDefinition("Override", buts), dmi);
                dmi.ShowSubwindow(wnd);
            }, 60, 50, dmi);
            ov.Enabled = true;
            Buttons.Add(ov);
            Buttons.Add(new DMITextButton("Data\nview", "Data view", true, null, 60, 50, dmi));
            Buttons.Add(new DMITextButton("Spec", "Special", true, null, 60, 50, dmi));
            Buttons.Add(new DMIIconButton("SE_04.bmp", "SE_04.bmp", "Settings", true, null, 60, 50, dmi));*/
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
            if (status.ActiveSubwindow is DMIMenuWindowDefinition && status.ActiveSubwindow.WindowTitle == WindowTitle)
            {
                DMIMenuWindowDefinition menu = (DMIMenuWindowDefinition)status.ActiveSubwindow;
                for (int i=0; i<menu.Buttons.Count && i<Buttons.Count; i++)
                {
                    Buttons[i].Enabled = menu.Buttons[i].Enabled;
                }
            }
        }
    }
}
