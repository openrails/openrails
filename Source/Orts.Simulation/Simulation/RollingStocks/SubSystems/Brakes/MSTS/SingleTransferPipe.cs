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

using ORTS.Common;
using System.Collections.Generic;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class SingleTransferPipe : AirSinglePipe
    {
        public SingleTransferPipe(TrainCar car)
            : base(car)
        {
            DebugType = "-";
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            base.Initialize(handbrakeOn, 0, 0, true);
            AuxResPressurePSI = 0;
            EmergResPressurePSI = 0;
            (Car as MSTSWagon).RetainerPositions = 0;
            (Car as MSTSWagon).EmergencyReservoirPresent = false;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            SingleTransferPipe thiscopy = (SingleTransferPipe)copy;
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
        }

        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], false));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus(units);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
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
            Car.BrakeRetardForceN = (MaxHandbrakeForceN * HandbrakePercent / 100) * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid
            if (Car.BrakeSkid) // Test to see if wheels are skiding to excessive brake force
            {
                Car.BrakeForceN = (MaxHandbrakeForceN * HandbrakePercent / 100) * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = (MaxHandbrakeForceN * HandbrakePercent / 100) * Car.BrakeShoeCoefficientFrictionAdjFactor; // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }
        
        }
    }
}
