// COPYRIGHT 2014 by the Open Rails project.
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
using ORTS.Common;
using Orts.Parsers.Msts;
using System.Collections.Generic;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class SingleTransferPipe : AirSinglePipe
    {

        readonly static float OneAtmospherePSI = Bar.ToPSI(1);

        public SingleTransferPipe(TrainCar car)
            : base(car)
        {
            DebugType = "-";
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
            }
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            base.Initialize(handbrakeOn, 0, 0, true);
            AuxResPressurePSI = 0;
            EmergResPressurePSI = 0;
            RetainerPositions = 0;
            EmergencyReservoirPresent = false;
            // Calculate brake pipe size depending upon whether vacuum or air braked
            if (Car.CarBrakeSystemType == "vacuum_piped")
            {
                BrakePipeVolumeM3 = (0.050f * 0.050f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + Car.CarLengthM)); // Using (2") pipe
            }
            else // air braked by default
            {
                BrakePipeVolumeM3 = (0.032f * 0.032f * (float)Math.PI / 4f) * Math.Max(5.0f, (1 + Car.CarLengthM)); // Using DN32 (1-1/4") pipe
            }
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            SingleTransferPipe thiscopy = (SingleTransferPipe)copy;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            HandBrakePresent = thiscopy.HandBrakePresent;
        }

        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            // display differently depending upon whether vacuum or air braked system
            if (Car.CarBrakeSystemType == "vacuum_piped")
            {
                return $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, false)}";
            }
            else  // air braked by default
            {
                return $"{Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
            }
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            // display differently depending upon whether vacuum or air braked system
            if (Car.CarBrakeSystemType == "vacuum_piped")
            {
                var s = $" {Simulator.Catalog.GetString("V")} {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.InHg, PressureUnit.InHg, true)}";
                if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                    s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
                if (HandbrakePercent > 0)
                    s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
                return s;
            }
            else // air braked by default
            {
                var s = $"{Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], false)}";
                if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                    s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
                if (HandbrakePercent > 0)
                    s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
                return s;
            }
            
        }

        // This overides the information for each individual wagon in the extended HUD  
       public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            // display differently depending upon whether vacuum or air braked system
            if (Car.CarBrakeSystemType == "vacuum_piped")
            {       

                return new string[] {
                DebugType,
                string.Empty,
                FormatStrings.FormatPressure(Vac.FromPress(BrakeLine1PressurePSI), PressureUnit.InHg, PressureUnit.InHg, true),
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                HandbrakePercent > 0 ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                };
            }
            else  // air braked by default
            {

            return new string[] {
                DebugType,
                string.Empty,
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty, // Spacer because the state above needs 2 columns.
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
                };

           }
        }

        public override float GetCylPressurePSI()
        {
            return 0;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override void Update(float elapsedClockSeconds)
        {
            BleedOffValveOpen = false;

            float brakeShoeFriction = Car.GetBrakeShoeFrictionFactor();
            Car.HuDBrakeShoeFriction = Car.GetBrakeShoeFrictionCoefficientHuD();

            // Update anglecock opening. Anglecocks set to gradually open over 30 seconds, but close instantly.
            // Gradual opening prevents undesired emergency applications
            UpdateAngleCockState(AngleCockAOpen, ref AngleCockAOpenAmount, ref AngleCockAOpenTime);
            UpdateAngleCockState(AngleCockBOpen, ref AngleCockBOpenAmount, ref AngleCockBOpenTime);

            Car.BrakeRetardForceN = ( Car.MaxHandbrakeForceN * HandbrakePercent / 100) * brakeShoeFriction; // calculates value of force applied to wheel, independent of wheel skid
        }
    }
}
