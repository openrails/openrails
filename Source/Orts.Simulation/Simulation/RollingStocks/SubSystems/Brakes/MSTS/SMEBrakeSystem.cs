// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
using System;
using System.Collections.Generic;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{

    // Detailed description of the operation of a SME brake system can be found in:  "Air brakes, an up-to-date treatise on the Westinghouse air brake as designed for passenger and 
    // freight service and for electric cars" by Ludy, Llewellyn V., 1875- [from old catalog]; American Technical Society
    // https://archive.org/details/airbrakesuptodat00ludy/page/174/mode/2up?q=%22SME+brake%22
    
    public class SMEBrakeSystem : AirTwinPipe
    {
        public SMEBrakeSystem(TrainCar car)
            : base(car)
        {
            DebugType = "SME";
        }

        public override void Update(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            float demandedAutoCylPressurePSI = 0;

            // Only allow SME brake tokens to operate if car is connected to an SME system
            if (lead == null || !(lead.BrakeSystem is SMEBrakeSystem) || Car.Train.BrakeLine4 < 0)
            {
                HoldingValve = ValveState.Release;
                base.Update(elapsedClockSeconds);
                return;
            }

            // process valid SME brake tokens

            if (BrakeLine3PressurePSI >= 1000f)
            {
                HoldingValve = ValveState.Release;
            }
            else
            {
                demandedAutoCylPressurePSI = Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * MaxTripleValveCylPressurePSI;
                HoldingValve = AutoCylPressurePSI <= demandedAutoCylPressurePSI ? ValveState.Lap : ValveState.Release;
            }
            
            base.Update(elapsedClockSeconds); // Allow processing of other valid tokens


            if (AutoCylPressurePSI < demandedAutoCylPressurePSI && !Car.WheelBrakeSlideProtectionActive)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (BrakeLine2PressurePSI - AutoCylPressurePSI) / (1 + AuxBrakeLineVolumeRatio / AuxCylVolumeRatio);
                if (dp > demandedAutoCylPressurePSI - AutoCylPressurePSI)
                    dp = demandedAutoCylPressurePSI - AutoCylPressurePSI;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;
            }
            
        }

        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return new string[] {
                DebugType,
                string.Format("{0}{1}",FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true), (Car as MSTSWagon).WheelBrakeSlideProtectionActive ? "???" : ""),
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true),
                EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.EmergencyReservoir], true) : string.Empty,
                TwoPipes ? FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.MainPipe], true) : string.Empty,
                BrakeValve == BrakeValveType.Distributor ? FormatStrings.FormatPressure(ControlResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true) : string.Empty,
                RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                string.Empty, // Spacer because the state above needs 2 columns.
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
            };
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
            return s;
        }
    }
}
