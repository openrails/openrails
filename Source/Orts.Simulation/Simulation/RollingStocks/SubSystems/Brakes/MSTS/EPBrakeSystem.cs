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

using System;
using System.Collections.Generic;
using ORTS.Common;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{

    public class EPBrakeSystem : AirTwinPipe
    {
        bool EPBrakeControlsBrakePipe;

        public EPBrakeSystem(TrainCar car)
            : base(car)
        {
            DebugType = "EP";
        }


        public override void Update(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = Car.Train.LeadLocomotive as MSTSLocomotive;

            // Only allow EP brake tokens to operate if car is connected to an EP system
            if (lead == null || !(lead.BrakeSystem is EPBrakeSystem) || Car.Train.BrakeLine4 == -1)
            {
                HoldingValve = ValveState.Release;
                base.Update(elapsedClockSeconds);
                return;
            }

            if (EPBrakeControlsBrakePipe)
            {
                if (Car.Train.BrakeLine4 >= 0)
                {
                    float targetPressurePSI = 0;
                    if (Car.Train.BrakeLine4 == 0)
                    {
                        targetPressurePSI = lead.TrainBrakeController.MaxPressurePSI;
                    }
                    else if (Car.Train.BrakeLine4 > 0)
                    {
                        float x = Math.Min(Car.Train.BrakeLine4, 1);
                        targetPressurePSI = lead.TrainBrakeController.MaxPressurePSI - lead.TrainBrakeController.MinReductionPSI * (1 - x) - lead.TrainBrakeController.FullServReductionPSI * x;
                    }
                    if (targetPressurePSI + 1 < BrakeLine1PressurePSI)
                    {
                        float dp = elapsedClockSeconds * MaxApplicationRatePSIpS / AuxCylVolumeRatio;
                        if (dp > BrakeLine1PressurePSI - targetPressurePSI)
                            dp = BrakeLine1PressurePSI - targetPressurePSI;
                        BrakeLine1PressurePSI -= dp;
                    }
                    else if (targetPressurePSI > BrakeLine1PressurePSI + 1 && BrakeLine2PressurePSI > targetPressurePSI && Car.Train.BrakeLine4 < 1)
                    {
                        float dp = elapsedClockSeconds * MaxReleaseRatePSIpS / AuxCylVolumeRatio;
                        if (dp > targetPressurePSI - BrakeLine1PressurePSI)
                            dp = targetPressurePSI - BrakeLine1PressurePSI;
                        BrakeLine1PressurePSI += dp;
                        BrakeLine2PressurePSI -= dp;
                    }
                }
                base.Update(elapsedClockSeconds);
                HoldingValve = ValveState.Release;
            }
            else
            {
                float demandedAutoCylPressurePSI = 0;
                if (BrakeLine3PressurePSI >= 1000f)
                {
                    HoldingValve = ValveState.Release;
                }
                else if (Car.Train.BrakeLine4 == -2) // Holding wire on
                {
                    HoldingValve = ValveState.Lap;
                }
                else if (Car.Train.BrakeLine4 == 0)
                {
                    HoldingValve = ValveState.Release;
                }
                else
                {
                    demandedAutoCylPressurePSI = Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * ServiceMaxCylPressurePSI;
                    if (TwoStageLowSpeedActive && demandedAutoCylPressurePSI > TwoStageLowPressurePSI) // Force EP system to respect two stage braking
                        demandedAutoCylPressurePSI = TwoStageLowPressurePSI;
                    HoldingValve = AutoCylPressurePSI <= demandedAutoCylPressurePSI ? ValveState.Lap : ValveState.Release;
                }
                
                base.Update(elapsedClockSeconds); // Allow processing of other valid tokens

                if (AutoCylPressurePSI < demandedAutoCylPressurePSI && !Car.WheelBrakeSlideProtectionActive)
                {
                    float dp = elapsedClockSeconds * ServiceApplicationRatePSIpS;
                    if (BrakeLine2PressurePSI - (dp * CylBrakeLineVolumeRatio) < AutoCylPressurePSI + dp)
                        dp = (BrakeLine2PressurePSI - AutoCylPressurePSI) / (1 + CylBrakeLineVolumeRatio);
                    if (dp > demandedAutoCylPressurePSI - AutoCylPressurePSI)
                        dp = demandedAutoCylPressurePSI - AutoCylPressurePSI;
                    BrakeLine2PressurePSI -= dp * CylBrakeLineVolumeRatio;
                    AutoCylPressurePSI += dp;
                }
            }
            
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch(lowercasetoken)
            {
                case "wagon(ortsepbrakecontrolsbrakepipe":
                    EPBrakeControlsBrakePipe = stf.ReadBoolBlock(false);
                    break;
                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }
        public override void InitializeFromCopy(BrakeSystem copy)
        {
            EPBrakeSystem thiscopy = (EPBrakeSystem)copy;
            EPBrakeControlsBrakePipe = thiscopy.EPBrakeControlsBrakePipe;
            base.InitializeFromCopy(copy);
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(Car.Train.HUDWagonBrakeCylinderPSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
            return s;
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            base.Initialize(handbrakeOn, maxPressurePSI, fullServPressurePSI, immediateRelease);
            AutoCylPressurePSI = Math.Max(AutoCylPressurePSI, Math.Min(Math.Max(Car.Train.BrakeLine4, 0), 1) * MaxCylPressurePSI);
            CylPressurePSI = ForceBrakeCylinderPressure(ref CylAirPSIM3, AutoCylPressurePSI);
        }
    }
}
