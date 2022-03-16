﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

    public class EPBrakeSystem : AirTwinPipe
    {
        public EPBrakeSystem(TrainCar car)
            : base(car)
        {
            DebugType = "EP";
        }

        public override void Update(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            float demandedAutoCylPressurePSI = 0;

            // Only allow EP brake tokens to operate if car is connected to an EP system
            if (lead == null || !(lead.BrakeSystem is EPBrakeSystem))
            {
                HoldingValve = ValveState.Release;
                base.Update(elapsedClockSeconds);
                return;
            }

            // process valid EP brake tokens

            if (BrakeLine3PressurePSI >= 1000f || Car.Train.BrakeLine4 < 0)
            {
                HoldingValve = ValveState.Release;
            }
            else if (Car.Train.BrakeLine4 == 0)
            {
                HoldingValve = ValveState.Lap;
            }
            else
            {
                demandedAutoCylPressurePSI = Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * MaxCylPressurePSI;
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

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
            return s;
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            base.Initialize(handbrakeOn, maxPressurePSI, fullServPressurePSI, immediateRelease);
            AutoCylPressurePSI = Math.Max(AutoCylPressurePSI, Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * MaxCylPressurePSI);
        }
    }
}
