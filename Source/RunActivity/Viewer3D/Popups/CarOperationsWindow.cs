// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using System;

namespace Orts.Viewer3D.Popups
{
    public class CarOperationsWindow : Window
    {
        readonly Viewer Viewer;

        public int CarPosition
        {
            set;
            get;
        }

        public CarOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 19, Window.DecorationSize.Y + owner.TextFontDefault.Height * 11 + ControlLayout.SeparatorSize * 10, Viewer.Catalog.GetString("Car Operation Menu"))
        {
            Viewer = owner.Viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label ID, buttonHandbrake, buttonTogglePower, buttonToggleMU, buttonToggleBatterySwitch, buttonToggleElectricTrainSupplyCable, buttonToggleBrakeHose, buttonToggleAngleCockA, buttonToggleAngleCockB, buttonToggleBleedOffValve, buttonClose;

            TrainCar trainCar = Viewer.PlayerTrain.Cars[CarPosition];
            BrakeSystem brakeSystem = (trainCar as MSTSWagon).BrakeSystem;

            var vbox = base.Layout(layout).AddLayoutVertical();
            vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID") + "  " + (CarPosition >= Viewer.PlayerTrain.Cars.Count? " " :Viewer.PlayerTrain.Cars[CarPosition].CarID), LabelAlignment.Center));
            ID.Color = Color.Red;
            vbox.AddHorizontalSeparator();

            string buttonHandbrakeText = "";
            if ((trainCar as MSTSWagon).GetTrainHandbrakeStatus())
                buttonHandbrakeText = Viewer.Catalog.GetString("Unset Handbrake");
            else
                buttonHandbrakeText = Viewer.Catalog.GetString("Set Handbrake");
            vbox.Add(buttonHandbrake = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, buttonHandbrakeText, LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            vbox.Add(buttonTogglePower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Power"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleMU = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle MU Connection"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleBatterySwitch = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Battery Switch"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleElectricTrainSupplyCable = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Electric Train Supply Connection"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            string buttonToggleBrakeHoseText = "";
            if (brakeSystem.FrontBrakeHoseConnected)
                buttonToggleBrakeHoseText = Viewer.Catalog.GetString("Disconnect Brake Hose");
            else
                buttonToggleBrakeHoseText = Viewer.Catalog.GetString("Connect Brake Hose");
            vbox.Add(buttonToggleBrakeHose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, buttonToggleBrakeHoseText, LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            string buttonToggleAngleCockAText = "";
            if (brakeSystem.AngleCockAOpen)
                buttonToggleAngleCockAText = Viewer.Catalog.GetString("Close Front Angle Cock");
            else
                buttonToggleAngleCockAText = Viewer.Catalog.GetString("Open Front Angle Cock");
            vbox.Add(buttonToggleAngleCockA = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, buttonToggleAngleCockAText, LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            string buttonToggleAngleCockBText = "";
            if (brakeSystem.AngleCockBOpen)
                buttonToggleAngleCockBText = Viewer.Catalog.GetString("Close Rear Angle Cock");
            else
                buttonToggleAngleCockBText = Viewer.Catalog.GetString("Open Rear Angle Cock");
            vbox.Add(buttonToggleAngleCockB = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, buttonToggleAngleCockBText, LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            string buttonToggleBleedOffValveAText = "";
            if (brakeSystem.BleedOffValveOpen)
                buttonToggleBleedOffValveAText = Viewer.Catalog.GetString("Close Bleed Off Valve");
            else
                buttonToggleBleedOffValveAText = Viewer.Catalog.GetString("Open Bleed Off Valve");
            vbox.Add(buttonToggleBleedOffValve = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, buttonToggleBleedOffValveAText, LabelAlignment.Center));
            vbox.AddHorizontalSeparator();

            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));

            if ((trainCar as MSTSWagon).HandBrakePresent)
                buttonHandbrake.Click += new Action<Control, Point>(buttonHandbrake_Click);
            else
                buttonHandbrake.Color = Color.Gray;
            buttonTogglePower.Click += new Action<Control, Point>(buttonTogglePower_Click);
            buttonToggleMU.Click += new Action<Control, Point>(buttonToggleMU_Click);
            buttonToggleBatterySwitch.Click += new Action<Control, Point>(buttonToggleBatterySwitch_Click);
            buttonToggleElectricTrainSupplyCable.Click += new Action<Control, Point>(buttonToggleElectricTrainSupplyCable_Click);

            if (CarPosition > 0)
                buttonToggleBrakeHose.Click += new Action<Control, Point>(buttonToggleBrakeHose_Click);
            else
                buttonToggleBrakeHose.Color = Color.Gray;

            buttonToggleAngleCockA.Click += new Action<Control, Point>(buttonToggleAngleCockA_Click);
            buttonToggleAngleCockB.Click += new Action<Control, Point>(buttonToggleAngleCockB_Click);
            buttonToggleBleedOffValve.Click += new Action<Control, Point>(buttonToggleBleedOffValve_Click);
            buttonClose.Click += new Action<Control, Point>(buttonClose_Click);

            return vbox;
        }

        void buttonClose_Click(Control arg1, Point arg2)
        {
            Visible = false;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            if (updateFull)
            {
                Layout();
            }
            base.PrepareFrame(elapsedTime, updateFull);
        }

        void buttonHandbrake_Click(Control arg1, Point arg2)
        {
            new WagonHandbrakeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus());
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
        }

        void buttonTogglePower_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
            {
                MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                new PowerCommand(Viewer.Log, locomotive, !locomotive.LocomotivePowerSupply.MainPowerSupplyOn);
                if (locomotive.LocomotivePowerSupply.MainPowerSupplyOn)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));
        }

        void buttonToggleMU_Click(Control arg1, Point arg2)
        {

            if ((Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                || (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive))
            {
                MSTSLocomotive locomotive = Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive;

                new ToggleMUCommand(Viewer.Log, locomotive, locomotive.RemoteControlGroup < 0);
                if (locomotive.RemoteControlGroup >= 0)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No MU command for this type of car!"));
        }

        void buttonToggleBatterySwitch_Click(Control arg1, Point arg2)
        {
            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSWagon wagon
                && wagon.PowerSupply is IPowerSupply)
            {
                new ToggleBatterySwitchCommand(Viewer.Log, wagon, !wagon.PowerSupply.BatterySwitch.On);
                if (wagon.PowerSupply.BatterySwitch.On)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch off battery command sent"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switch on battery command sent"));
            }
        }

        void buttonToggleElectricTrainSupplyCable_Click(Control arg1, Point arg2)
        {
            MSTSWagon wagon = Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon;

            if (wagon.PowerSupply != null)
            {
                new ConnectElectricTrainSupplyCableCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !wagon.PowerSupply.FrontElectricTrainSupplyCableConnected);
                if (wagon.PowerSupply.FrontElectricTrainSupplyCableConnected)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable connected"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front ETS cable disconnected"));
            }
            else
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("This car doesn't have an ETS system"));
            }
        }

        void buttonToggleBrakeHose_Click(Control arg1, Point arg2)
        {
            new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
        }

        void buttonToggleAngleCockA_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockACommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
        }

        void buttonToggleAngleCockB_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockBCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
        }

        void buttonToggleBleedOffValve_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new ToggleBleedOffValveCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
        }
    }
}
